using E3Core.Data;
using E3Core.Settings;
using E3Core.Utility;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static E3Core.Processors.BuffCheck;

namespace E3Core.Processors
{
    public static class Pets
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
		[ExposedData("Pets", "PetMaxShrink")]
		private static bool _petMaxShrink = false;
        private static Int32 _petMaxShrinkID = 0;
        private static Int64 _nextPetCheck = 0;
        private static Int64 _nextPetCheckInterval = 1000;
		[ExposedData("Pets", "PetShrinkSpells")]
		private static List<string> _petShrinkSpells = new List<string>() { "Diminutive Companion", "Gemstone of Dark Flame", "Symbol of Ancient Summoning", "Tiny Companion",  };
		private static ISpawns _spawns = E3.Spawns;

		//Multi-Pet: one pet per [Pets] Pet Spell= line, keyed by the spell name. Me.Pet only ever points at one of them.
		private static Dictionary<string, Int32> _multiPetIDs = new Dictionary<string, Int32>(StringComparer.OrdinalIgnoreCase);
		//name each line's own summon produced. Pet names are unique per player on these servers, so this finds
		//the same pet again after a zone gives it a new spawn ID
		private static Dictionary<string, string> _multiPetNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static Dictionary<string, Int64> _multiPetSummonBlockedUntil = new Dictionary<string, Int64>(StringComparer.OrdinalIgnoreCase);
		private static string _multiPetSummonPending = null;
		private static Int64 _multiPetSummonPendingAt = 0;
		//_multiPetNames is saved next to the bot ini as <botini>_pet.ini so it survives an e3/MQ restart
		private static string _multiPetNamesFile = String.Empty;
		private const string _multiPetNamesSection = "Pet Names";

        [SubSystemInit]
        public static void Pets_Init()
        {
            RegisterEvents();
        }

        private static void RegisterEvents()
        {
			//server refused a Multi-Pet summon because that line already has a pet we did not recognise
			EventProcessor.RegisterEvent("MultiPetSummonRefused", @".*(cannot have more than one pet|already have a pet).*", (x) =>
			{
				if (!E3.CharacterSettings.Pet_MultiPet) return;
				OnMultiPetSummonRefused();
			});
        }

        public static void Reset()
        {
            _petMaxShrink = false;
            _petMaxShrinkID = 0;

            //try out hold if on enc, then after 3 sec try ghold
			if (E3.CurrentClass == Class.Enchanter)
            {
				MQ.Cmd("/squelch /pet hold on");
				MQ.Cmd("/timed 30 /squelch /pet ghold on");
			}
            else
            {
				MQ.Cmd("/timed 30 /squelch /pet ghold on");
			}
			if (E3.CharacterSettings.Pet_MultiPet)
			{
				MQ.Cmd("/timed 35 /say #petcmd ghold on all");
			}

		}

		/// <summary>
		/// Sends a pet command to Me.Pet, and with Multi-Pet on also to every pet we own
		/// through the server's #petcmd (servers where you can have several pets out at once).
		/// #petcmd works on the current target, so callers should already have the mob targeted.
		/// </summary>
		/// <param name="petCmd">the normal /pet command, e.g. "/pet back off"</param>
		/// <param name="multiPetVerb">the #petcmd verb, e.g. "back"</param>
		/// <param name="delayInDeciSeconds">optional /timed delay</param>
		public static void PetCommand(string petCmd, string multiPetVerb, Int32 delayInDeciSeconds = 0)
		{
			string timed = delayInDeciSeconds > 0 ? $"/timed {delayInDeciSeconds} " : String.Empty;
			if (MQ.Query<Int32>("${Me.Pet.ID}") > 0)
			{
				MQ.Cmd(timed + petCmd);
			}
			if (E3.CharacterSettings.Pet_MultiPet)
			{
				//has to go out as chat (/say), which is where the server picks up # commands
				MQ.Cmd($"{timed}/say #petcmd {multiPetVerb} all");
			}
		}

        [AdvSettingInvoke]
        public static void Check_Pets()
        {
            if (E3.IsInvis) return;
            if (Basics.AmIDead()) return;
            if (!e3util.ShouldCheck(ref _nextPetCheck, _nextPetCheckInterval)) return;

            Int32 petId = MQ.Query<Int32>("${Me.Pet.ID}");

			if (E3.CharacterSettings.Pet_MultiPet)
			{
				Check_MultiPets(petId);
				return;
			}

            if (petId > 0)
            {
                CheckPetHeal(petId);
                CheckPetShrink(petId);
                CheckPetBuffs();
            }

			if (Basics.InCombat() && !E3.CharacterSettings.Pet_SummonCombat)
			{
				return;
			}
			if (petId<1)
			{	
				CheckPetSummon(ref petId);
			}
		}

        public static void removeBuffsIfNecessary(List<Spell> buffs) {
            foreach (var buff in buffs) {
                if (!String.IsNullOrWhiteSpace(buff.Ifs))
                {
                    if (!Casting.Ifs(buff.Ifs))
                    {
                        continue;
                    }
                }
                //var buffIndex = MQ.Query<int>($"${{Me.Pet.Buff[{buff.SpellName}]}}");
                if (Casting.TryGetPetBuffDuration(buff.SpellID,out var _))
                {
                    MQ.Cmd($"/removebuff -pet {buff.SpellName}");
                }
            }
        }

        public static void CheckPetBuffs()
        {
            Pets.removeBuffsIfNecessary(E3.CharacterSettings.BlockedPetBuffs);
        }

        private static void CheckPetSummon(ref Int32 petID)
        {
            if (petID == 0 && E3.CharacterSettings.PetSpell.Count > 0)
            {
                bool castPet = false;
                //we have no pet, do we have a pet configurd to summon?               
                foreach (var spell in E3.CharacterSettings.PetSpell)
                {
					if (!spell.Enabled) continue;
					if (!String.IsNullOrWhiteSpace(spell.Ifs))
                    {
                        if (!Casting.Ifs(spell))
                        {
                            continue;
                        }
                    }

                    if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
                    {
                        Casting.Cast(0, spell);
                        castPet = true;
                        break;
                    }
                }
                if(castPet)
                {
                    //wait for the pet to appear
                    MQ.Delay(1000);
                    petID = MQ.Query<Int32>("${Me.Pet.ID}");
                    if (petID > 0)
                    {
						MQ.Cmd("/squelch /pet hold on");
						MQ.Cmd("/squelch /pet ghold on");
                    }
					if (E3.CharacterSettings.Pet_MultiPet)
					{
						MQ.Cmd("/say #petcmd ghold on all");
					}
                }
              
            }
        }

		/// <summary>
		/// Waits out a pet summon cast, then gives the pet a moment to appear.
		/// </summary>
		private static void WaitForPetCast(Spell petSpell)
		{
			//bards come back from Cast as soon as the cast starts (song handling), and the next
			//pet check would /stopsong and recast it, so wait the cast out before looking for the pet
			Int64 castDoneBy = Core.StopWatch.ElapsedMilliseconds + (Int64)petSpell.MyCastTime + 3000;
			while (Casting.IsCasting() && Core.StopWatch.ElapsedMilliseconds < castDoneBy)
			{
				MQ.Delay(100);
			}
			//wait for the pet to appear
			MQ.Delay(1000);
		}

		/// <summary>
		/// Multi-Pet version of the pet check: keeps one pet per Pet Spell line, heals whichever pet is lowest,
		/// and summons any line whose pet is missing. Shrink and blocked buffs still only work on Me.Pet.
		/// </summary>
		private static void Check_MultiPets(Int32 petId)
		{
			LoadMultiPetNames();
			List<Spawn> myPets = GetMyLivingPets();
			UpdateMultiPetTracking(myPets);

			if (petId > 0)
			{
				CheckPetShrink(petId);
				CheckPetBuffs();
			}

			//heal the lowest of the pets we track (plus Me.Pet, which is always one of ours)
			Spawn lowestPet = null;
			foreach (var pet in myPets)
			{
				if (pet.ID != petId && !_multiPetIDs.ContainsValue(pet.ID)) continue;
				if (lowestPet == null || pet.PctHps < lowestPet.PctHps) lowestPet = pet;
			}
			if (lowestPet != null)
			{
				CheckPetHeal(lowestPet.ID, (Int32)lowestPet.PctHps);
			}

			if (Basics.InCombat() && !E3.CharacterSettings.Pet_SummonCombat)
			{
				return;
			}
			CheckMultiPetSummon(myPets);
		}

		/// <summary>
		/// Spawn IDs of the pets Multi-Pet tracks, one per Pet Spell line.
		/// </summary>
		public static List<Int32> GetMultiPetIDs()
		{
			return _multiPetIDs.Values.ToList();
		}

		/// <summary>
		/// Every living pet whose master is us. Some extra pets show up with no master at all, so a pet
		/// with no master whose name one of our summons gave us counts too.
		/// </summary>
		private static List<Spawn> GetMyLivingPets()
		{
			List<Spawn> pets = new List<Spawn>();
			foreach (var s in _spawns.Get())
			{
				if (s.Dead || s.ID == E3.CurrentId) continue;
				if (s.TypeDesc != "Pet") continue;
				if (s.MasterID == E3.CurrentId || (s.MasterID == 0 && IsLearnedPetName(s.CleanName)))
				{
					pets.Add(s);
				}
			}
			return pets;
		}
		private static bool IsLearnedPetName(string name)
		{
			return _multiPetNames.Values.Any(x => String.Equals(x, name, StringComparison.OrdinalIgnoreCase));
		}
		/// <summary>
		/// All pet spawns near us, anyone's, so a summon can spot the new one even when it has no master.
		/// </summary>
		private static HashSet<Int32> GetNearbyPetIDs()
		{
			HashSet<Int32> ids = new HashSet<Int32>();
			foreach (var s in _spawns.Get())
			{
				if (s.TypeDesc == "Pet" && s.Distance < 100) ids.Add(s.ID);
			}
			return ids;
		}

		/// <summary>
		/// Drops pets that died or left, finds known pets again by name (zoning changes their IDs),
		/// and out of combat hands any pet nobody tracks to a Pet Spell line that has none.
		/// </summary>
		private static void UpdateMultiPetTracking(List<Spawn> myPets)
		{
			foreach (var key in _multiPetIDs.Keys.ToList())
			{
				if (!myPets.Any(x => x.ID == _multiPetIDs[key])) _multiPetIDs.Remove(key);
			}

			foreach (var spell in E3.CharacterSettings.PetSpell)
			{
				if (_multiPetIDs.ContainsKey(spell.SpellName)) continue;
				if (!_multiPetNames.TryGetValue(spell.SpellName, out var petName)) continue;
				var pet = myPets.FirstOrDefault(x => !_multiPetIDs.ContainsValue(x.ID) && String.Equals(x.CleanName, petName, StringComparison.OrdinalIgnoreCase));
				if (pet != null) _multiPetIDs[spell.SpellName] = pet.ID;
			}

			//a summon whose pet showed up after CheckMultiPetSummon stopped looking still gets it, and learns its name
			if (_multiPetSummonPending != null && Core.StopWatch.ElapsedMilliseconds - _multiPetSummonPendingAt < 15000)
			{
				var pet = myPets.FirstOrDefault(x => !_multiPetIDs.ContainsValue(x.ID) && !IsLearnedPetName(x.CleanName));
				if (pet != null)
				{
					TrackMultiPet(_multiPetSummonPending, pet);
					_multiPetSummonPending = null;
				}
			}

			//we don't know which line these came from (e3 restarted with pets out), so it is a best guess.
			//if it is wrong the server refuses that line's next summon and OnMultiPetSummonRefused sorts it out.
			//not in combat, so a swarm pet doesn't get mistaken for one of ours.
			if (Basics.InCombat()) return;
			foreach (var spell in E3.CharacterSettings.PetSpell)
			{
				if (!spell.Enabled) continue;
				if (_multiPetIDs.ContainsKey(spell.SpellName)) continue;
				var pet = myPets.FirstOrDefault(x => !_multiPetIDs.ContainsValue(x.ID) && !IsLearnedPetName(x.CleanName));
				if (pet == null) break;
				_multiPetIDs[spell.SpellName] = pet.ID;
			}
		}

		/// <summary>
		/// Summons the first Pet Spell line that has no pet. One summon per check.
		/// </summary>
		private static void CheckMultiPetSummon(List<Spawn> myPets)
		{
			foreach (var spell in E3.CharacterSettings.PetSpell)
			{
				if (!spell.Enabled) continue;
				if (_multiPetIDs.ContainsKey(spell.SpellName)) continue;
				if (_multiPetSummonBlockedUntil.TryGetValue(spell.SpellName, out var blockedUntil) && Core.StopWatch.ElapsedMilliseconds < blockedUntil) continue;
				if (!String.IsNullOrWhiteSpace(spell.Ifs))
				{
					if (!Casting.Ifs(spell))
					{
						continue;
					}
				}
				if (!(Casting.CheckMana(spell) && Casting.CheckReady(spell))) continue;

				HashSet<Int32> petsBefore = GetNearbyPetIDs();
				foreach (var pet in myPets) petsBefore.Add(pet.ID);
				_multiPetSummonPending = spell.SpellName;
				_multiPetSummonPendingAt = Core.StopWatch.ElapsedMilliseconds;
				Casting.Cast(0, spell);
				WaitForPetCast(spell);

				_spawns.RefreshList();
				var newPet = GetMyLivingPets().FirstOrDefault(x => !petsBefore.Contains(x.ID) && !_multiPetIDs.ContainsValue(x.ID));
				if (newPet == null)
				{
					//master could not be resolved: a brand new pet right next to us that nobody else owns
					newPet = _spawns.Get().FirstOrDefault(x => x.TypeDesc == "Pet" && !x.Dead && x.MasterID == 0 && x.Distance < 40 && !petsBefore.Contains(x.ID) && !_multiPetIDs.ContainsValue(x.ID));
				}
				if (newPet != null)
				{
					_multiPetSummonPending = null;
					TrackMultiPet(spell.SpellName, newPet);
					MQ.Cmd("/say #petcmd ghold on all");
				}
				return;
			}
		}

		private static void TrackMultiPet(string spellName, Spawn pet)
		{
			foreach (var key in _multiPetIDs.Where(x => x.Value == pet.ID).Select(x => x.Key).ToList())
			{
				_multiPetIDs.Remove(key);
			}
			_multiPetIDs[spellName] = pet.ID;
			if (_multiPetNames.TryGetValue(spellName, out var knownName) && String.Equals(knownName, pet.CleanName, StringComparison.OrdinalIgnoreCase)) return;
			//a name belongs to one line
			foreach (var key in _multiPetNames.Where(x => String.Equals(x.Value, pet.CleanName, StringComparison.OrdinalIgnoreCase)).Select(x => x.Key).ToList())
			{
				_multiPetNames.Remove(key);
			}
			_multiPetNames[spellName] = pet.CleanName;
			SaveMultiPetNames();
		}

		/// <summary>
		/// Loads the pet names learned from earlier summons. Re-reads when the bot ini changes (different character or set).
		/// </summary>
		private static void LoadMultiPetNames()
		{
			string fileName = E3.CharacterSettings._fileName.Replace(".ini", "_pet.ini");
			if (fileName == _multiPetNamesFile) return;
			_multiPetNamesFile = fileName;
			_multiPetNames.Clear();
			_multiPetIDs.Clear();
			if (!File.Exists(fileName)) return;
			try
			{
				IniData data = e3util.CreateIniParser().ReadFile(fileName);
				var section = data.Sections[_multiPetNamesSection];
				if (section == null) return;
				foreach (var key in section)
				{
					if (!String.IsNullOrWhiteSpace(key.Value)) _multiPetNames[key.KeyName] = key.Value;
				}
			}
			catch (Exception ex)
			{
				_log.Write($"Could not read {fileName}: {ex.Message}", Logging.LogLevels.Error);
			}
		}

		private static void SaveMultiPetNames()
		{
			if (String.IsNullOrWhiteSpace(_multiPetNamesFile)) return;
			try
			{
				IniData data = new IniData();
				data.Sections.AddSection(_multiPetNamesSection);
				var section = data.Sections[_multiPetNamesSection];
				foreach (var pair in _multiPetNames)
				{
					section.AddKey(pair.Key, pair.Value);
				}
				e3util.CreateIniParser().WriteFile(_multiPetNamesFile, data);
			}
			catch (Exception ex)
			{
				_log.Write($"Could not save {_multiPetNamesFile}: {ex.Message}", Logging.LogLevels.Error);
			}
		}

		/// <summary>
		/// The server says the line we just summoned already has a pet. Give it the pet of another line that never
		/// confirmed it by its own summon (a best guess from UpdateMultiPetTracking), otherwise stop trying for a minute.
		/// </summary>
		private static void OnMultiPetSummonRefused()
		{
			string spellName = _multiPetSummonPending;
			_multiPetSummonPending = null;
			if (spellName == null || Core.StopWatch.ElapsedMilliseconds - _multiPetSummonPendingAt > 30000) return;

			_multiPetSummonBlockedUntil[spellName] = Core.StopWatch.ElapsedMilliseconds + 60000;
			foreach (var pair in _multiPetIDs.ToList())
			{
				if (String.Equals(pair.Key, spellName, StringComparison.OrdinalIgnoreCase)) continue;
				if (_multiPetNames.ContainsKey(pair.Key)) continue;
				if (_spawns.TryByID(pair.Value, out var pet))
				{
					TrackMultiPet(spellName, pet);
					E3.Bots.Broadcast($"\ayServer says {spellName} already has a pet, now tracking {pet.CleanName} as the {spellName} pet.");
					return;
				}
			}
			E3.Bots.Broadcast($"\ayServer says {spellName} already has a pet but none was found, pausing that summon for 60s.");
		}

        private static void CheckPetHeal(Int32 petID)
        {
			CheckPetHeal(petID, MQ.Query<Int32>("${Me.Pet.PctHPs}"));
		}
        private static void CheckPetHeal(Int32 petID, Int32 pctHps)
        {
            Int32 pctMendHps = E3.CharacterSettings.Pet_MendPercent;
            Int32 currentTarget = MQ.Query<Int32>("${Target.ID}");
            if (pctHps < pctMendHps && MQ.Query<bool>("${Me.AltAbilityReady[Replenish Companion]}"))
            {
                //Replenish Companion only works on Me.Pet. On multi-pet servers selecting a pet makes it Me.Pet.
                //only swap targets when the AA is ready, and put the target back after, so a pet sitting under
                //Mend % in combat doesn't pull us off the mob every check
                if (E3.CharacterSettings.Pet_MultiPet && petID != MQ.Query<Int32>("${Me.Pet.ID}"))
                {
                    Casting.TrueTarget(petID);
                    MQ.Delay(1000, () => MQ.Query<Int32>("${Me.Pet.ID}") == petID);
                }
                if (petID == MQ.Query<Int32>("${Me.Pet.ID}"))
                {
                    Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue("Replenish Companion", out s))
                    {
                        s = new Spell("Replenish Companion");
                    }
                    if (s.SpellID > 0)
                    {
                        Casting.Cast(petID, s);
                        e3util.PutOriginalTargetBackIfNeeded(currentTarget);
                        return;
                    }
                }
                e3util.PutOriginalTargetBackIfNeeded(currentTarget);
            }
            foreach (var spell in E3.CharacterSettings.PetHeals)
            {
				if (!spell.Enabled) continue;

				if (!String.IsNullOrWhiteSpace(spell.Ifs))
				{
					if (!Casting.Ifs(spell))
					{
                        continue;
					}
				}

				if (pctHps <= spell.HealPct)
                {
                    if (Casting.CheckMana(spell) && Casting.CheckReady(spell))
                    {
                        Casting.Cast(petID, spell);
                        e3util.PutOriginalTargetBackIfNeeded(currentTarget);
                        return;
                    }
                }
            }
        }

        private static void CheckPetShrink(Int32 petID)
        {
            if (!E3.CharacterSettings.Pet_AutoShrink) return;
            if (petID != _petMaxShrinkID)
            {
                _petMaxShrinkID = petID;
                _petMaxShrink = false;
            }
            double petHeight = MQ.Query<double>("${Me.Pet.Height}");


            if (_petMaxShrink) return;

            if (petHeight > 1 && petHeight != 0)
            {
                foreach (var spellName in _petShrinkSpells)
                {
					Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue(spellName, out s))
                    {
                        s = new Spell(spellName);
                    }
                    if (s.SpellID > 0 && s.CastType != CastingType.None)
                    {
                        Casting.Cast(petID, s);
                        MQ.Delay(300);
                        double newPetHeight = MQ.Query<double>("${Me.Pet.Height}");

                        if (newPetHeight == petHeight)
                        {
                            _petMaxShrink = true;
                            _petMaxShrinkID = petID;
                        }

                        return;
                    }
                }
            }
        }
    }
}

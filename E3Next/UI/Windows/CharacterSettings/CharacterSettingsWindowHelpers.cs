using E3Core.Data;
using E3Core.Processors;
using E3Core.Server;
using E3Core.Utility;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Windows.Forms.AxHost;
using window = E3Core.UI.Windows.CharacterSettings.CharacterSettingsWindow;

namespace E3Core.UI.Windows.CharacterSettings
{
	public class CharacterSettingsWindowHelpers
	{
		public static object _dataLock = new object();

		public static Logging _log = E3.Log;
		private static IMQ MQ = E3.MQ;
		private static ISpawns _spawns = E3.Spawns;

		public static Dictionary<string, SpellData> _spellCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_discCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_aaCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
		_skillCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase), // guess we are not showing much from skills :P 
		_itemCatalogLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);

		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _catalog_Spells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_AA = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Disc = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Skills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
		_catalog_Items = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

		public static readonly Dictionary<string, (float r, float g, float b, float a)> RichTextColorMapping = new Dictionary<string, (float, float, float, float)>(StringComparer.OrdinalIgnoreCase)
		{
			["color=gold"] = (0.95f, 0.85f, 0.35f, 1.0f),
			["color=yellow"] = (1.0f, 0.92f, 0.23f, 1.0f),
			["color=orange"] = (1.0f, 0.6f, 0.2f, 1.0f),
			["color=red"] = (0.9f, 0.3f, 0.3f, 1.0f),
			["color=green"] = (0.3f, 0.9f, 0.5f, 1.0f),
			["color=blue"] = (0.35f, 0.6f, 0.95f, 1.0f),
			["color=teal"] = (0.3f, 0.85f, 0.85f, 1.0f),
			["color=purple"] = (0.75f, 0.55f, 0.95f, 1.0f),
			["color=white"] = (0.95f, 0.95f, 0.95f, 1.0f),
			["color=gray"] = (0.6f, 0.6f, 0.6f, 1.0f),
			["color=silver"] = (0.8f, 0.8f, 0.85f, 1.0f)
		};


		public static readonly string[] _spellKeyOutputOrder = new[]
		{
			"Gem", "Ifs", "CheckFor", "CastIF", "HealPct", "HealthMax", "Zone", "MinSick",
			"BeforeSpell", "AfterSpell", "BeforeEvent", "AfterEvent", "MinMana", "MaxMana", "MinEnd",
			"MinDurationBeforeRecast", "MaxTries", "Reagent", "CastType", "PctAggro", "Delay", "RecastDelay",
			"AfterEventDelay", "AfterSpellDelay", "BeforeEventDelay", "BeforeSpellDelay", "AfterCastDelay",
			"AfterCastCompletedDelay", "SongRefreshTime", "StackRequestItem", "StackRequestTargets",
			"StackCheckInterval", "StackRecastDelay", "MinHP", "MinHPTotal", "GiveUpTimer", "TriggerSpell",
			"MinAggro", "MaxAggro",
			"ExcludedClasses", "ExcludedNames"
		};
		public static readonly string[] _spellFlagOutputOrder = new[]
		{
			"NoInterrupt", "IgnoreStackRules", "NoTarget", "NoAggro", "NoBurn", "Rotate",
			"NoMidSongCast", "GoM", "AllowSpellSwap", "NoEarlyRecast", "NoStack", "Debug", "IsDoT", "IsDebuff"
		};
		public static readonly HashSet<string> _spellKnownKeys = new HashSet<string>(_spellKeyOutputOrder, StringComparer.OrdinalIgnoreCase);
		public static readonly HashSet<string> _spellKnownFlags = new HashSet<string>(_spellFlagOutputOrder, StringComparer.OrdinalIgnoreCase);
		public static readonly HashSet<string> _customKeySections = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Burn", "CommandSets", "Ifs", "Events", "E3BotsPublishData (key/value)", "EventLoopTiming", "EventRegMatches" };
		public static readonly Dictionary<string, List<string>> _stringCollectionSections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) 
		{ { "Startup Commands",new List<string>() }, {"CommandSets", new List<string>() }, {"Zoning Commands", new List<string>() },
		{ "Manastone",new List<string>(){ "ExceptionMQQuery","ExceptionZone"} },{ "Heals",new List<string>(){ "Tank","Important Bot","Pet Owner"} },{"E3ChatChannelsToJoin",new List<string>() }
		};

		public static readonly Dictionary<string, List<string>> _singleEntryKeys = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) 
		{ { "Buffs", new List<string>(){"Aura" } }, { "Heals", new List<string>(){ "Who to Heal","Who to HoT" } }, {"Ifs",new List<string>() }, {"Events",new List<string>() },{"E3BotsPublishData",new List<string>() }
		,{"E3BotsPublishData (key/value)",new List<string>()},{"EventLoopTiming",new List<string>() },{"EventRegMatches",new List<string>() }
		};

		public static readonly Dictionary<string, string> _spellKeyAliasMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{"AfterCast", "AfterSpell"},
			{"BeforeCast", "BeforeSpell"},
			{"DelayAfterCast", "AfterCastCompletedDelay"},
			{"AfterCastCompletedDelay", "AfterCastCompletedDelay"},
			{"MinHpTotal", "MinHPTotal"},
			{"MinHp", "MinHP"}
		};
	
		public static readonly string[] _spellCastTypeOptions = new[] { "Spell", "AA", "Disc", "Ability", "Item", "None" };
		public static void RebuildSectionsOrderIfNeeded()
		{
			var state = window._state.GetState<State_MainWindow>();

			// Rebuild sections order when ini path changes
			string activeIniPath = GetActiveSettingsPath() ?? string.Empty;
			if (!string.Equals(activeIniPath, state.LastIniPath, StringComparison.OrdinalIgnoreCase))
			{
				state.LastIniPath = activeIniPath;
				state.SelectedSection = string.Empty;
				state.SelectedKey = string.Empty;
				state.SelectedValueIndex = -1;
				BuildConfigSectionOrder();
				// Auto-load catalogs on ini switch without blocking UI
				RequestCatalogUpdate();
			}
		}
		public static void ResetCatalogs()
		{
			_catalog_Spells.Clear();
			_catalog_AA.Clear();
			_catalog_Disc.Clear();
			_catalog_Skills.Clear();
			_catalog_Items.Clear();
		}
		public static void RequestCatalogUpdate()
		{
			var gemstate = window._state.GetState<State_CatalogGems>();
			window._state.State_CatalogReady = false;
			ResetCatalogs();
			window._state.State_CatalogLoadRequested = true;
			window._state.Status_CatalogRequest = "Queued catalog load...";
			gemstate.Source = "Refreshing...";
		}
		public static SectionData GetCurrentSectionData()
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();

			if (mainWindowState.CurrentINIData != null)
			{
				var data = mainWindowState.CurrentINIData;
				var sec = data.Sections.GetSectionData(mainWindowState.SelectedSection);
				return sec;
			}
			return null;
		}

		public static Int32 GetIconFromIniString(string value)
		{
			Int32 indexOfSlash = value.IndexOf('/');

			string spellName = value;
			if (indexOfSlash != -1)
			{
				spellName = value.Substring(0, indexOfSlash);
			}
			Int32 iconID = 0;
			if (_spellCatalogLookup.TryGetValue(spellName, out var tspell))
			{
				iconID = tspell.SpellIcon;
			}
			else if (_aaCatalogLookup.TryGetValue(spellName, out var taa))
			{
				iconID = taa.SpellIcon;
			}
			else if (_itemCatalogLookup.TryGetValue(spellName, out var titem))
			{
				iconID = titem.SpellIcon;

			}
			else if (_discCatalogLookup.TryGetValue(spellName, out var tdisc))
			{
				iconID = tdisc.SpellIcon;
			}

			return iconID;

		}

		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.Category ?? string.Empty;
				string sub = s.Subcategory ?? string.Empty;
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}

				var newSpell = new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = Convert.ToDouble(s.MyCastTimeInSeconds),
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				};

				if (String.IsNullOrWhiteSpace(newSpell.CastName)) newSpell.CastName = newSpell.Name;

				l.Add(newSpell);
			}
			foreach (var submap in dest.Values)
			{
				foreach (var l in submap.Values)
				{
					l.Sort((a, b) => b.Level.CompareTo(a.Level));
				}
			}
			return dest;
		}
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.Category ?? string.Empty;
				string sub = s.Subcategory ?? string.Empty;
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}

				var newSpell = new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = s.MyCastTimeInSeconds,
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				};
				if (String.IsNullOrWhiteSpace(newSpell.CastName)) newSpell.CastName = newSpell.Name;
				l.Add(newSpell);



			}
			foreach (var submap in dest.Values)
			{
				foreach (var l in submap.Values)
				{
					l.Sort((a, b) => b.Level.CompareTo(a.Level));
				}
			}
			return dest;
		}
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingSkillsCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			var cat = "Skill"; var sub = "Basic";
			var submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
			dest[cat] = submap;
			var list = new List<E3Spell>();
			submap[sub] = list;
			foreach (var s in data)
			{
				if (s == null) continue;
				var newSpell = new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					CastType = s.CastType.ToString(),
					Description = s.Description ?? string.Empty,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				};
				if (String.IsNullOrWhiteSpace(newSpell.CastName)) newSpell.CastName = newSpell.Name;
				list.Add(newSpell);
			}
			list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			return dest;
		}
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeLoadingItemsCatalog(List<Spell> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.CastName ?? string.Empty; // item name
				string sub = s.SpellName ?? string.Empty; // click spell
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.CastName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = Convert.ToDouble(s.MyCastTimeInSeconds),
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			return dest;
		}
		// Organize items like e3config: first key = CastName (item), subkey = SpellName, and list entries by item (CastName)
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeItemsCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			foreach (var s in data)
			{
				if (s == null) continue;
				string cat = s.CastName ?? string.Empty; // item name
				string sub = s.SpellName ?? string.Empty; // click spell
				if (!dest.TryGetValue(cat, out var submap))
				{
					submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
					dest.Add(cat, submap);
				}
				if (!submap.TryGetValue(sub, out var l))
				{
					l = new List<E3Spell>();
					submap.Add(sub, l);
				}
				l.Add(new E3Spell
				{
					Name = s.CastName ?? string.Empty,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					CastName = s.CastName ?? string.Empty,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					Mana = s.Mana,
					CastTime = s.MyCastTimeInSeconds,
					Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
					Range = s.MyRange,
					Description = s.Description ?? string.Empty,
					ResistType = s.ResistType ?? string.Empty,
					ResistAdj = s.ResistAdj,
					CastType = s.CastType.ToString(),
					SpellGem = s.SpellGem,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			return dest;
		}


		// Organize skills like e3config: force into Skill/Basic and list by spell name
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeSkillsCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
		{
			var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
			var cat = "Skill"; var sub = "Basic";
			var submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
			dest[cat] = submap;
			var list = new List<E3Spell>();
			submap[sub] = list;
			foreach (var s in data)
			{
				if (s == null) continue;
				list.Add(new E3Spell
				{
					Name = s.SpellName ?? string.Empty,
					CastName = s.CastName ?? s.SpellName,
					Category = cat,
					Subcategory = sub,
					Level = s.Level,
					TargetType = s.TargetType ?? string.Empty,
					SpellType = s.SpellType ?? string.Empty,
					CastType = s.CastType.ToString(),
					Description = s.Description ?? string.Empty,
					SpellEffects = s.SpellEffects != null
						? s.SpellEffects.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()).ToList()
						: new List<string>(),
					SpellIcon = s.SpellIcon
				});
			}
			list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
			return dest;
		}

		public static Dictionary<string, SpellData> ConvertSpellsToSpellDataLookup(List<Spell> spells)
		{
			Dictionary<string, SpellData> returnValue = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
			foreach (var spell in spells)
			{
				var tspell = spell.ToProto();
				if (!returnValue.ContainsKey(tspell.CastName))
				{
					returnValue.Add(tspell.CastName, tspell);
				}
			}
			return returnValue;
		}
		public static Dictionary<string, SpellData> ConvertToSpellDataLookup(Google.Protobuf.Collections.RepeatedField<SpellData> spells)
		{
			Dictionary<string, SpellData> returnValue = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
			foreach (var spell in spells)
			{
				if (!returnValue.ContainsKey(spell.CastName))
				{
					returnValue.Add(spell.CastName, spell);
				}

			}
			return returnValue;
		}
		public static string GenerateUniqueKey(KeyDataCollection keys, string baseName)
		{
			string unique = baseName;
			int idx = 1;
			while (keys.ContainsKey(unique))
			{
				unique = $"{baseName} ({idx})";
				idx++;
				if (idx > 1000)
				{
					return null;
				}
			}
			return unique;
		}
		public static void RefreshEditableSpellState(bool force = false)
		{


			var mainWindowState = window._state.GetState<State_MainWindow>();
			var spellEditorState = window._state.GetState<State_SpellEditor>();

			if (force) { mainWindowState.Currently_EditableSpell = null; }

			//check if this has changed from what we were before
			if (String.IsNullOrWhiteSpace(mainWindowState.SelectedSection))
			{
				mainWindowState.Signature_CurrentEditedSpell = String.Empty;
				mainWindowState.Currently_EditableSpell = null;
				return;

			}
			if (String.IsNullOrWhiteSpace(mainWindowState.SelectedKey))
			{
				mainWindowState.Signature_CurrentEditedSpell = String.Empty;
				mainWindowState.Currently_EditableSpell = null;
				return;

			}
			if (mainWindowState.SelectedValueIndex == -1)
			{
				mainWindowState.Signature_CurrentEditedSpell = String.Empty;
				mainWindowState.Currently_EditableSpell = null;
				return;

			}
			//lets get the actual entry
			var kd = GetCurrentEditedSpellKeyData();
			if (kd == null) return;

			var rawValue = kd.ValueList[mainWindowState.SelectedValueIndex];

			string entryLabel = $"[{mainWindowState.SelectedSection}] {mainWindowState.SelectedKey} entry #{mainWindowState.SelectedValueIndex + 1}";

			if (!String.Equals(mainWindowState.Signature_CurrentEditedSpell, entryLabel) || mainWindowState.Currently_EditableSpell == null)
			{
				mainWindowState.Signature_CurrentEditedSpell = entryLabel;
				mainWindowState.Currently_EditableSpell = new Spell(rawValue, mainWindowState.CurrentINIData, false);
				spellEditorState.Reset();
			}


		}
		public static KeyData GetCurrentEditedSpellKeyData()
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();

			var data = mainWindowState.CurrentINIData;
			if (data == null) return null;
			var sectionData = data.Sections.GetSectionData(mainWindowState.SelectedSection);
			if (sectionData == null) return null;
			var kd = sectionData.Keys.GetKeyData(mainWindowState.SelectedKey);
			if (kd == null) return null;
			if (kd.ValueList.Count <= mainWindowState.SelectedValueIndex) return null;

			return kd;
		}

		public static void SaveActiveIniData()
		{
			try
			{
				string currentPath = GetCurrentCharacterIniPath();
				string selectedPath = GetActiveSettingsPath();
				var pd = GetActiveCharacterIniData();
				if (string.IsNullOrEmpty(selectedPath) || pd == null) return;

				var parser = E3Core.Utility.e3util.CreateIniParser();
				parser.WriteFile(selectedPath, pd);
				var state = window._state.GetState<State_MainWindow>();
				state.ConfigIsDirty = false;
				_log.Write($"Saved changes to {Path.GetFileName(selectedPath)}");
			}
			catch (Exception ex)
			{
				_log.Write($"Failed to save: {ex.Message}", Logging.LogLevels.Error);
			}
		}

		static Dictionary<string, List<String>> _KeyOptionsLookup = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
			{"Assist Type (Melee/Ranged/Off)", new List<string>() { "Melee","AutoAttack","Ranged","AutoFire","Off" } },
			{"Melee Stick Point", new List<string>() { "Front","Behind","BehindOnce","Pin","!Front" } }

		};

		static List<String> _KeyOptionsOnOff = new List<string>() { "On", "Off" };
		// Attempt to derive an explicit set of allowed options from the key label, e.g.
		// "Assist Type (Melee/Ranged/Off)" => ["Melee","Ranged","Off"]
		public static bool TryGetValidOptionsForKey(string keyLabel, out List<string> options)
		{
			if (_KeyOptionsLookup.TryGetValue(keyLabel, out var result))
			{
				options = result;
				return true;
			}
			options = null;
			return false;
		}
		public static bool IsStringConfigKey(string key)
		{
			if (E3.CharacterSettings.SettingsReflectionStringTypes.Contains(key))
			{
				return true;
			}
			return false;
		}

		public static bool IsIntergerConfigKey(string key)
		{
			if (E3.CharacterSettings.SettingsReflectionIntTypes.Contains(key))
			{

				return true;
			}
			return false;
		}
		public static bool IsBooleanConfigKey(string key)
		{
			if (E3.CharacterSettings.SettingsReflectionBoolTypes.Contains(key))
			{

				return true;
			}
			return false;
		}
		public static bool IsActiveIniBard()
		{
			try
			{
				var chardata = GetActiveCharacterIniData();
				if (chardata?.Sections != null)
				{
					if (chardata.Sections.ContainsSection("Bard")) return true;
				}

				string path = GetActiveSettingsPath() ?? string.Empty;
				if (string.IsNullOrEmpty(path)) return string.Equals(E3.CurrentClass.ToString(), "Bard", StringComparison.OrdinalIgnoreCase);

				string file = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
				var parts = file.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2)
				{
					string cls = parts.Last();
					if (string.Equals(cls, "Bard", StringComparison.OrdinalIgnoreCase)) return true;
				}

				return string.Equals(E3.CurrentClass.ToString(), "Bard", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return string.Equals(E3.CurrentClass.ToString(), "Bard", StringComparison.OrdinalIgnoreCase);
			}
		}
		public static string FormatMsSmart(int ms)
		{
			if (ms <= 0) return string.Empty;
			double totalSec = ms / 1000.0;
			if (totalSec < 60.0)
			{
				return totalSec < 10 ? totalSec.ToString("0.##") + "s" : totalSec.ToString("0.#") + "s";
			}
			int m = (int)(totalSec / 60.0);
			double rs = totalSec - m * 60;
			if (rs < 0.5) return m.ToString() + "m";
			return m.ToString() + "m " + rs.ToString("0.#") + "s";
		}
		public static void ChangeSelectedCharacter(string filename)
		{
			var state = window._state.GetState<State_MainWindow>();

			_log.Write($"Selecting other:{filename}", Logging.LogLevels.Debug);
			var parser = e3util.CreateIniParser();
			var pd = parser.ReadFile(filename);
			state.CurrentINIFileNameFull = filename;
			state.CurrentINIData = pd;
			state.SelectedCharacterSection = string.Empty;
			state.SignatureOfSelectedKeyValue = String.Empty;

			// Trigger catalog reload for the selected peer
			RequestCatalogUpdate();
		}
		public static string AppendIfToken(string value, string ifKey)
		{
			string v = value ?? string.Empty;
			// We support both legacy "Ifs|" and preferred "/Ifs|" tokens when extending,
			// but we always write using "/Ifs|" going forward.
			const string tokenPreferred = "/Ifs|";
			const string tokenLegacy = "Ifs|";
			int posSlash = v.IndexOf(tokenPreferred, StringComparison.OrdinalIgnoreCase);
			int posLegacy = v.IndexOf(tokenLegacy, StringComparison.OrdinalIgnoreCase);
			int pos = posSlash >= 0 ? posSlash : posLegacy;
			int tokenLen = posSlash >= 0 ? tokenPreferred.Length : tokenLegacy.Length;

			if (pos < 0)
			{
				// No Ifs present; append preferred token with NO leading separator
				if (v.Length == 0) return tokenPreferred + ifKey;
				return v + tokenPreferred + ifKey;
			}

			// Extend existing Ifs list; rebuild using preferred token
			int start = pos + tokenLen;
			int end = v.IndexOf('|', start);
			string head = v.Substring(0, pos) + tokenPreferred; // normalize token
			string rest = end >= 0 ? v.Substring(end) : string.Empty;
			string list = end >= 0 ? v.Substring(start, end - start) : v.Substring(start);
			var items = list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(x => x.Trim())
							.Where(x => x.Length > 0)
							.ToList();
			if (!items.Contains(ifKey, StringComparer.OrdinalIgnoreCase)) items.Add(ifKey);
			string rebuilt = head + string.Join(",", items) + rest;
			return rebuilt;
		}
		public static string FormatSecondsSmart(double seconds)
		{
			if (seconds <= 0) return string.Empty;
			if (seconds < 1.0)
			{
				return seconds.ToString("0.###") + " s";
			}
			if (seconds < 60.0)
			{
				return seconds < 10.0 ? seconds.ToString("0.##") + " s" : seconds.ToString("0.#") + " s";
			}
			int minutes = (int)(seconds / 60.0);
			double remainder = seconds - minutes * 60.0;
			if (remainder < 0.5)
			{
				return minutes + "m";
			}
			return minutes + "m " + remainder.ToString("0.#") + "s";
		}

		private static readonly Regex _inlineNumberRegex = new Regex(@"\b\d{4,}\b", RegexOptions.Compiled);

		public static string FormatWithSeparators(long value)
		{
			return value.ToString("N0", CultureInfo.InvariantCulture);
		}

		public static string FormatInlineNumbers(string input)
		{
			if (string.IsNullOrEmpty(input)) return input;
			return _inlineNumberRegex.Replace(input, m =>
			{
				if (long.TryParse(m.Value, out var numeric))
				{
					return FormatWithSeparators(numeric);
				}
				return m.Value;
			});
		}

		public static bool IsIniForOnlineToon(string iniPath, ConcurrentDictionary<string, string> onlineToons)
		{
			if (onlineToons == null || onlineToons.Count == 0) return false;
			string file = Path.GetFileNameWithoutExtension(iniPath) ?? string.Empty;
			if (string.IsNullOrEmpty(file)) return false;
			int underscore = file.IndexOf('_');
			string toon = underscore > 0 ? file.Substring(0, underscore) : file;
			return onlineToons.ContainsKey(toon);
		}
		static List<String> _catalogRefreshKeyTypes = new List<string>() { "Spells", "AAs", "Discs", "Skills", "Items" };
		static Int64 _numberofMillisecondsBeforeCatalogNeedsRefresh = 30000;

		public static void ProcessBackground_UpdateRemotePlayer(string targetToon)
		{
			//put lower case as zeromq is case sensitive
			targetToon = targetToon.ToLower();

			//have to make a network call and wait for a response. 
			System.Threading.Tasks.Task.Run(() =>
			{
				try
				{
					var gemState = window._state.GetState<State_CatalogGems>();

					//pre-create the new lookups
					SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>
					mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
					Dictionary<string, SpellData>
					spellLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					aaLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					discLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					skillLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase),
					itemLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);


					_log.WriteDelayed($"Fetching data (remote)", Logging.LogLevels.Debug);

					//_state.Status_CatalogRequest = $"Loading catalogs from {targetToon}...";
					bool peerSuccess = true;


					// Send request: CatalogReq-<Toon>

					//do we have that toons data already in memory or is it too old?


					bool needDataRefresh = false;

					Int64 dataMustBeNewerThan = Core.StopWatch.ElapsedMilliseconds - _numberofMillisecondsBeforeCatalogNeedsRefresh;
					foreach (var key in _catalogRefreshKeyTypes)
					{
						string topicKey = $"CatalogResp-{E3.CurrentName}-{key}";
						_log.WriteDelayed($"Checking for catalog key for target toon:{targetToon} key:{topicKey}", Logging.LogLevels.Debug);

						if (NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(targetToon, out var topics)
							&& topics.TryGetValue(topicKey, out var entry))
						{
							//if we called within the last 60 seconds, just use the old data
							if (entry.LastUpdate < dataMustBeNewerThan)
							{
								_log.WriteDelayed($"Catalog key found but too old, asking for refresh: old:{entry.LastUpdate} vs  new:{dataMustBeNewerThan}", Logging.LogLevels.Debug);

								needDataRefresh = true;
								dataMustBeNewerThan = 0;
								break;
							}
						}
						else
						{
							_log.WriteDelayed($"Catalog key not found, asking for refresh", Logging.LogLevels.Debug);

							needDataRefresh = true;
							dataMustBeNewerThan = 0;
							break;
						}
					}



					if (needDataRefresh)
					{
						PubServer.AddTopicMessage($"CatalogReq-{targetToon}", "");
					}

					if (TryFetchPeerSpellDataListPub(targetToon, "Spells", out var ps, dataMustBeNewerThan))
					{
						mapSpells = OrganizeCatalog(ps);
						spellLookup = ConvertToSpellDataLookup(ps);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "AAs", out var pa, dataMustBeNewerThan))
					{
						mapAAs = OrganizeCatalog(pa);
						aaLookup = ConvertToSpellDataLookup(ps);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "Discs", out var pd, dataMustBeNewerThan))
					{
						mapDiscs = OrganizeCatalog(pd);
						discLookup = ConvertToSpellDataLookup(pd);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "Skills", out var pk, dataMustBeNewerThan))
					{
						mapSkills = OrganizeSkillsCatalog(pk);
						skillLookup = ConvertToSpellDataLookup(pk);
					}
					if (TryFetchPeerSpellDataListPub(targetToon, "Items", out var pi, dataMustBeNewerThan))
					{
						mapItems = OrganizeItemsCatalog(pi);
						itemLookup = ConvertToSpellDataLookup(pi);
					}

					// Also try to fetch gem data
					if (peerSuccess && TryFetchPeerGemData(targetToon, out var gemData))
					{
						lock (_dataLock)
						{
							gemState.Gems = gemData;
							window._state.State_GemsAvailable = true;
						}
					}
					else
					{
						lock (_dataLock)
						{
							window._state.State_GemsAvailable = false;
						}
					}

					// If any peer fetch failed, fallback to local
					if (!peerSuccess)
					{
						window._state.Status_CatalogRequest = "Peer catalog fetch failed; using local.";
						gemState.Source = "Local (fallback)";
					}
					else
					{
						gemState.Source = $"Remote ({targetToon})";
					}
					_log.WriteDelayed($"Fetching data (remote) Complete!", Logging.LogLevels.Debug);


					lock (_dataLock)
					{
						// Publish atomically
						_catalog_Spells = mapSpells;
						_spellCatalogLookup = spellLookup;
						_catalog_AA = mapAAs;
						_aaCatalogLookup = aaLookup;
						_catalog_Disc = mapDiscs;
						_discCatalogLookup = discLookup;
						_catalog_Skills = mapSkills;
						_skillCatalogLookup = skillLookup;
						_catalog_Items = mapItems;
						_itemCatalogLookup = itemLookup;

						_catalogLookups = new[]
						{
							(_catalog_Spells, "Spell"),
							(_catalog_AA, "AA"),
							(_catalog_Disc, "Disc"),
							(_catalog_Skills, "Skill"),
							(_catalog_Items, "Item")
							};
						window._state.State_CatalogReady = true;
						window._state.Status_CatalogRequest = "Catalogs loaded.";

					}

				}
				catch (Exception ex)
				{
					window._state.Status_CatalogRequest = "Catalog load failed: " + (ex.Message ?? "error");
				}
				finally
				{
					window._state.State_CatalogLoading = false;
					window._state.State_CatalogLoadRequested = false;
				}
			});
		}
		public static E3Spell FindSpellItemAAByName(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;

			// Search all catalog types for an exact match
			foreach (var (catalog, type) in _catalogLookups)
			{
				foreach (var categoryKvp in catalog)
				{
					foreach (var subCategoryKvp in categoryKvp.Value)
					{
						var match = subCategoryKvp.Value.FirstOrDefault(spell =>
							string.Equals(spell.Name, name, StringComparison.OrdinalIgnoreCase));
						if (match != null)
						{
							// Set the cast type if not already set
							if (string.IsNullOrEmpty(match.CastType)) match.CastType = type;
							return match;
						}
					}
				}
			}
			return null;
		}

		public static int GetLocalSpellIconIndex(string spellName)
		{
			if (string.IsNullOrEmpty(spellName)) return -1;

			try
			{
				// Use the catalog lookups if they're available
				var spellInfo = FindSpellItemAAByName(spellName);
				if (spellInfo != null && spellInfo.SpellIcon >= 0)
				{
					return spellInfo.SpellIcon;
				}

				// Fallback: Query MQ directly for spell icon
				int iconIndex = E3.MQ.Query<int>($"${{Spell[{spellName}].SpellIcon}}");
				return iconIndex > 0 ? iconIndex : -1;
			}
			catch
			{
				return -1;
			}
		}

		public static void UpdateLocalSpellGemDataViaLocal()
		{
			var gemState = window._state.GetState<State_CatalogGems>();

			try
			{
				var localGems = new string[12];
				var localGemIcons = new int[12];

				for (int gem = 1; gem <= 12; gem++)
				{
					try
					{
						string spellName = MQ.Query<string>($"${{Me.Gem[{gem}]}}");
						Int32 spellID = MQ.Query<Int32>($"${{Me.Gem[{gem}].ID}}");
						localGems[gem - 1] = spellID.ToString();

						// Get spell icon index if we have a valid spell
						if (!string.IsNullOrEmpty(spellName) && !spellName.Equals("NULL", StringComparison.OrdinalIgnoreCase))
						{
							localGemIcons[gem - 1] = GetLocalSpellIconIndex(spellName);
						}
						else
						{
							localGemIcons[gem - 1] = -1;
						}
					}
					catch
					{
						localGems[gem - 1] = "ERROR";
						localGemIcons[gem - 1] = -1;
					}
				}
				lock (_dataLock)
				{
					gemState.Gems = localGems;
					gemState.GemIcons = localGemIcons;
					window._state.State_GemsAvailable = true;
				}
			}
			catch (Exception ex)
			{
				_log.WriteDelayed($"Fetching data Error: {ex.Message}", Logging.LogLevels.Debug);
				window._state.State_GemsAvailable = false;
			}
		}
		public static void ProcessBackground_UpdateLocalPlayer()
		{
			var gemState = window._state.GetState<State_CatalogGems>();

			SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
					mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();

			window._state.Status_CatalogRequest = "Loading catalogs (local)...";
			gemState.Source = "Local";

			_log.WriteDelayed($"Fetching data (local)", Logging.LogLevels.Debug);

			var spellList = e3util.ListAllBookSpells();
			mapSpells = OrganizeLoadingCatalog(spellList);
			var spellLookup = ConvertSpellsToSpellDataLookup(spellList);

			var aaList = e3util.ListAllActiveAA();
			mapAAs = OrganizeLoadingCatalog(aaList);
			var aaLookup = ConvertSpellsToSpellDataLookup(aaList);

			var discList = e3util.ListAllDiscData();
			mapDiscs = OrganizeLoadingCatalog(discList);
			var discLookup = ConvertSpellsToSpellDataLookup(discList);

			var skillList = e3util.ListAllActiveSkills();
			mapSkills = OrganizeLoadingSkillsCatalog(skillList);
			var skillLookup =ConvertSpellsToSpellDataLookup(skillList);

			var itemList = e3util.ListAllItemWithClickyData();
			mapItems = OrganizeLoadingItemsCatalog(itemList);
			var itemLookup = ConvertSpellsToSpellDataLookup(itemList);

			// Also collect local gem data with spell icon indices
			UpdateLocalSpellGemDataViaLocal();

			_log.WriteDelayed($"Fetching data (local) Complete!", Logging.LogLevels.Debug);

			lock (_dataLock)
			{
				// Publish atomically
				_catalog_Spells = mapSpells;
				_spellCatalogLookup = spellLookup;

				_catalog_AA = mapAAs;
				_aaCatalogLookup = aaLookup;

				_catalog_Disc = mapDiscs;
				_discCatalogLookup = discLookup;

				_catalog_Skills = mapSkills;
				_skillCatalogLookup = skillLookup;

				_catalog_Items = mapItems;
				_itemCatalogLookup = itemLookup;

				_catalogLookups = new[]
				{
					(_catalog_Spells, "Spell"),
					(_catalog_AA, "AA"),
					(_catalog_Disc, "Disc"),
					(_catalog_Skills, "Skill"),
					(_catalog_Items, "Item")
				};
				window._state.State_CatalogReady = true;
				window._state.Status_CatalogRequest = "Catalogs loaded.";
				window._state.State_CatalogLoading = false;
				window._state.State_CatalogLoadRequested = false;

			}
		}
		public static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> GetCatalogByType(AddType t)
		{
			lock (_dataLock)
			{
				switch (t)
				{
					case AddType.AAs: return _catalog_AA;
					case AddType.Discs: return _catalog_Disc;
					case AddType.Skills: return _catalog_Skills;
					case AddType.Items: return _catalog_Items;
					case AddType.Spells:
					default: return _catalog_Spells;
				}
			}
		}
		public static (SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>, string)[] _catalogLookups = new[]
		{
			(_catalog_Spells, "Spell"),
			(_catalog_AA, "AA"),
			(_catalog_Disc, "Disc"),
			(_catalog_Skills, "Skill"),
			(_catalog_Items, "Item")
		};
		public static bool TryFetchPeerSpellDataListPub(string toon, string listKey, out Google.Protobuf.Collections.RepeatedField<SpellData> data, Int64 dataMustBeOlderThan)
		{
			data = new Google.Protobuf.Collections.RepeatedField<SpellData>();
			//topics are stored in toon specific keys
			string topic = $"CatalogResp-{E3.CurrentName}-{listKey}";
			// Poll SharedDataClient.TopicUpdates for up to ~2s
			long end = Core.StopWatch.ElapsedMilliseconds + 4000;

			_log.WriteDelayed($"Trying to fetch data with key:{topic}", Logging.LogLevels.Debug);


			while (Core.StopWatch.ElapsedMilliseconds < end)
			{
				if (NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
					&& topics.TryGetValue(topic, out var entry))
				{
					if (entry.LastUpdate < dataMustBeOlderThan) continue;
					_log.WriteDelayed($"Data found with key:{topic}", Logging.LogLevels.Debug);

					string payload = entry.Data;
					int first = payload.IndexOf(':');
					int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
					string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
					byte[] bytes = Convert.FromBase64String(b64);
					var list = SpellDataList.Parser.ParseFrom(bytes);
					data = list.Data;
					return true;
				}
				System.Threading.Thread.Sleep(25);
			}
			_log.WriteDelayed($"Data NOT FOUND with key:{topic}", Logging.LogLevels.Debug);

			return false;
		}
		public static bool TryFetchPeerGemData(string toon, out string[] gemData)
		{
			var gemState = window._state.GetState<State_CatalogGems>();

			gemData = new string[12];
			try
			{
				_log.WriteDelayed($"Tryign to fetch gem data for:{toon}", Logging.LogLevels.Debug);

				if (string.IsNullOrEmpty(toon)) return false;

				string topic = $"CatalogResp-{E3.CurrentName}-Gems";
				// Poll SharedDataClient.TopicUpdates for gem data
				long end = Core.StopWatch.ElapsedMilliseconds + 2000; // 2 second timeout
				while (Core.StopWatch.ElapsedMilliseconds < end)
				{
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
						&& topics.TryGetValue(topic, out var entry))
					{
						string payload = entry.Data;
						ParseGemDataWithIcons(payload, out gemData, out gemState.GemIcons);
						return true;
					}

					// Also check if data came back under current name
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
						&& topics2.TryGetValue(topic, out var entry2))
					{
						string payload = entry2.Data;
						ParseGemDataWithIcons(payload, out gemData, out gemState.GemIcons);
						return true;
					}

					System.Threading.Thread.Sleep(25);
				}
			}
			catch { }

			// Fill with ERROR if failed
			for (int i = 0; i < 12; i++)
			{
				gemData[i] = "ERROR";
				gemState.GemIcons[i] = -1;
			}
			return false;
		}

		// Helper method to parse gem data with icon indices from pipe-separated format
		public static void ParseGemDataWithIcons(string payload, out string[] gemNames, out int[] gemIcons)
		{
			gemNames = new string[12];
			gemIcons = new int[12];
			_log.WriteDelayed($"Parsing gem data with payload:{payload}", Logging.LogLevels.Debug);

			try
			{
				// Parse pipe-separated gem data: "SpellName:IconIndex|SpellName:IconIndex|..."
				var gems = payload.Split('|');
				int count = Math.Min(gems.Length, 12);

				for (int i = 0; i < count; i++)
				{
					string gemEntry = gems[i] ?? "NULL:-1";
					string[] parts = gemEntry.Split(':');

					if (parts.Length >= 2)
					{
						gemNames[i] = parts[0] ?? "NULL";
						if (int.TryParse(parts[1], out int iconIndex))
						{
							gemIcons[i] = iconIndex;
						}
						else
						{
							gemIcons[i] = -1;
						}
					}
					else
					{
						// Fallback for old format without icons
						gemNames[i] = gemEntry ?? "NULL";
						gemIcons[i] = -1;
					}
				}

				// Fill remaining slots if needed
				for (int i = count; i < 12; i++)
				{
					gemNames[i] = "NULL";
					gemIcons[i] = -1;
				}
			}
			catch
			{
				// Error case - fill with defaults
				for (int i = 0; i < 12; i++)
				{
					gemNames[i] = "ERROR";
					gemIcons[i] = -1;
				}
			}
		}
		public static string GetActiveSettingsPath()
		{
			switch (window._activeSettingsTab)
			{
				case SettingsTab.General:
					if (E3.GeneralSettings != null && !string.IsNullOrEmpty(E3.GeneralSettings._fileLastModifiedFileName))
						return E3.GeneralSettings._fileLastModifiedFileName;
					return E3Core.Settings.BaseSettings.GetSettingsFilePath("General Settings.ini");
				case SettingsTab.Advanced:
					var adv = E3Core.Settings.BaseSettings.GetSettingsFilePath("Advanced Settings.ini");
					if (!string.IsNullOrEmpty(E3Core.Settings.BaseSettings.CurrentSet)) adv = adv.Replace(".ini", "_" + E3Core.Settings.BaseSettings.CurrentSet + ".ini");
					return adv;
				case SettingsTab.Character:
				default:
					var state = window._state.GetState<State_MainWindow>();
					if (string.IsNullOrEmpty(state.CurrentINIFileNameFull))
					{
						var currentPath = GetCurrentCharacterIniPath();
						state.CurrentINIFileNameFull = currentPath;
					}
					return state.CurrentINIFileNameFull;
			}
		}

		public static string GetSelectedIniOwnerName()
		{
			try
			{
				string path = GetActiveSettingsPath();
				if (string.IsNullOrEmpty(path)) return E3.CurrentName;
				string file = Path.GetFileNameWithoutExtension(path);
				int us = file.IndexOf('_');
				if (us > 0) return file.Substring(0, us);
				return file;
			}
			catch { return E3.CurrentName; }
		}
		private static ConcurrentDictionary<string, string> _onlineToonsCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private static Int64 _onlineToonsLastUpdate = 0;
		private static Int64 _onlineToonsLastsUpdateInterval = 3000;
		public static ConcurrentDictionary<string, string> GetOnlineToonNames()
		{

			if (!e3util.ShouldCheck(ref _onlineToonsLastUpdate, _onlineToonsLastsUpdateInterval))
			{
				lock (_onlineToonsCache)
				{
					return _onlineToonsCache;

				}
			}
			lock (_onlineToonsCache)
			{
				_onlineToonsCache.Clear();

				try
				{
					var connected = E3Core.Server.NetMQServer.SharedDataClient?.UsersConnectedTo?.Keys;
					if (connected != null)
					{
						foreach (var name in connected)
						{
							if (!string.IsNullOrEmpty(name)) _onlineToonsCache.TryAdd(name, name);
						}
					}
				}
				catch { }

				if (!string.IsNullOrEmpty(E3.CurrentName)) _onlineToonsCache.TryAdd(E3.CurrentName, E3.CurrentName);


				return _onlineToonsCache;
			}

		}
		private static Int64 _iniFileScanInterval = 3000;
		private static Int64 _iniFileScanTimeStamp = 0;
		public static void ScanCharIniFilesIfNeeded()
		{
			if (!e3util.ShouldCheck(ref _iniFileScanTimeStamp, _iniFileScanInterval))
			{
				return;
			}
			var dir = Settings.BaseSettings.GetBotPath();// Path.GetDirectoryName(curPath);
			var server = E3.ServerName ?? string.Empty;
			var pattern = "*_*" + server + ".ini";
			var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
			if (files == null || files.Length == 0)
				files = Directory.GetFiles(dir, "*.ini", SearchOption.TopDirectoryOnly);
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);

			var mainWindowState = window._state.GetState<State_MainWindow>();
			mainWindowState.IniFilesFromDisk = files;
		}
		public static string GetCurrentCharacterIniPath()
		{
			string returnValue = E3.CharacterSettings._fileName;
			if (E3.CharacterSettings != null && !string.IsNullOrEmpty(E3.CharacterSettings._fileName))
			{
				return returnValue;

			}
			var name = E3.CurrentName ?? string.Empty;
			var server = E3.ServerName ?? string.Empty;
			var klass = E3.CurrentClass.ToString();
			returnValue = E3Core.Settings.BaseSettings.GetBoTFilePath(name, server, klass);
			return returnValue;
		}
		public static bool TryGetIniPathForToon(string toon, out string path)
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();
			var allPlayerState = window._state.GetState<State_AllPlayers>();
			path = null;
			if (string.IsNullOrWhiteSpace(toon)) return false;

			// Keep our list of ini files fresh
			ScanCharIniFilesIfNeeded();

			// Current character is easy
			if (!string.IsNullOrEmpty(E3.CurrentName) &&
				string.Equals(E3.CurrentName, toon, StringComparison.OrdinalIgnoreCase))
			{
				path = GetCurrentCharacterIniPath();
				return !string.IsNullOrEmpty(path);
			}

			if (mainWindowState.IniFilesFromDisk == null || mainWindowState.IniFilesFromDisk.Length == 0) return false;

			// Optional: prefer matches that also contain server in the filename
			allPlayerState.ServerByToon.TryGetValue(toon, out var serverHint);
			serverHint = serverHint ?? string.Empty;

			// Gather candidates: filename starts with "<Toon>_" or equals "<Toon>.ini"
			var candidates = new List<string>();
			foreach (var f in mainWindowState.IniFilesFromDisk)
			{
				var name = System.IO.Path.GetFileName(f);
				if (name.StartsWith(toon + "_", StringComparison.OrdinalIgnoreCase) ||
					name.Equals(toon + ".ini", StringComparison.OrdinalIgnoreCase))
				{
					candidates.Add(f);
				}
			}

			if (candidates.Count == 0) return false;

			// Prefer one that mentions the server (common pattern: Toon_Server_Class.ini)
			if (!string.IsNullOrEmpty(serverHint))
			{
				var withServer = candidates.FirstOrDefault(f =>
					System.IO.Path.GetFileName(f).IndexOf("_" + serverHint + "_", StringComparison.OrdinalIgnoreCase) >= 0);
				if (!string.IsNullOrEmpty(withServer))
				{
					path = withServer;
					return true;
				}
			}

			// Fallback: first candidate
			path = candidates[0];
			return true;
		}

		public static bool TrySaveIniValueForToon(string toon, string section, string key, string newValue, out string error)
		{
			error = null;
			if (!TryGetIniPathForToon(toon, out var iniPath))
			{
				error = $"Could not resolve ini path for '{toon}'.";
				return false;
			}

			try
			{
				var parser = E3Core.Utility.e3util.CreateIniParser();       // you already use this elsewhere
				var data = parser.ReadFile(iniPath);                         // IniParser.Model.IniData
				if (!data.Sections.ContainsSection(section))
					data.Sections.AddSection(section);
				data[section][key] = newValue ?? string.Empty;               // simplest way to set a value
				parser.WriteFile(iniPath, data);                             // persist to disk

				return true;
			}
			catch (Exception ex)
			{
				error = ex.Message;
				return false;
			}
		}

		// Reads a single INI value for a toon. Returns true if the toon/path exists and read succeeded.
		public  static bool TryReadIniValueForToon(string toon, string section, string key, out string value)
		{
			value = string.Empty;
			try
			{
				if (!TryGetIniPathForToon(toon, out var iniPath))
					return false;

				var parser = E3Core.Utility.e3util.CreateIniParser();
				var data = parser.ReadFile(iniPath);
				if (!data.Sections.ContainsSection(section))
					return true; // file exists but section missing -> empty

				value = data[section][key] ?? string.Empty;
				return true;
			}
			catch
			{
				value = string.Empty;
				return false;
			}
		}
		public static List<string> ScanInventoryForType(string key)
		{
			var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (string.IsNullOrWhiteSpace(key)) return found.ToList();
			string target = key.Trim();

			// Scan a generous set of inventory indices and their bag contents
			for (int inv = 1; inv <= 40; inv++)
			{
				try
				{
					bool present = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}]}}");
					if (!present) continue;

					// top-level item type
					string t = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Type}}") ?? string.Empty;
					if (!string.IsNullOrEmpty(t) && t.Equals(target, StringComparison.OrdinalIgnoreCase))
					{
						string name = E3.MQ.Query<string>($"${{Me.Inventory[{inv}]}}") ?? string.Empty;
						if (!string.IsNullOrEmpty(name)) found.Add(name);
					}

					// bag contents if container
					int slots = E3.MQ.Query<int>($"${{Me.Inventory[{inv}].Container}}");
					if (slots <= 0) continue;
					for (int i = 1; i <= slots; i++)
					{
						try
						{
							bool ipresent = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}].Item[{i}]}}");
							if (!ipresent) continue;
							string it = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}].Type}}") ?? string.Empty;
							if (!string.IsNullOrEmpty(it) && it.Equals(target, StringComparison.OrdinalIgnoreCase))
							{
								string iname = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}]}}") ?? string.Empty;
								if (!string.IsNullOrEmpty(iname)) found.Add(iname);
							}
						}
						catch { }
					}
				}
				catch { }
			}

			return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
		}
		public static IniData GetActiveCharacterIniData()
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();

			return mainWindowState.CurrentINIData;
		}
		static List<String> _configSectionOrderDefault = new List<string>() { "Misc", "Assist Settings", "Nukes", "Debuffs", "DoTs on Assist", "DoTs on Command", "Heals", "Buffs", "Melee Abilities", "Burn", "CommandSets", "Pets", "Ifs" };
		static List<String> _configSectionOrderNecro = new List<string>() { "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
		static List<String> _configSectionOrderSK = new List<string>() { "Nukes", "Assist Settings", "Buffs", "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs" };
		static List<String> _configSectionOrderBard = new List<string>() { "Bard", "Melee Abilities", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };

		public static void BuildConfigSectionOrder()
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();
			var pd = GetActiveCharacterIniData();
			if (pd?.Sections == null) return;

			// Class-prioritized defaults similar to e3config
			var cls = E3.CurrentClass;
			List<String> currentOrder = _configSectionOrderDefault;
			//if (cls.ToString().Equals("Bard", StringComparison.OrdinalIgnoreCase))
			//{
			//	currentOrder = _configSectionOrderBard;
			//}
			//else if (cls.ToString().Equals("Necromancer", StringComparison.OrdinalIgnoreCase))
			//{
			//	currentOrder = _configSectionOrderNecro;
			//}
			//else if (cls.ToString().Equals("Shadowknight", StringComparison.OrdinalIgnoreCase))
			//{
			//	currentOrder = _configSectionOrderSK;
			//}
			mainWindowState.SectionsOrdered.Clear();
			// Seed ordered list with defaults that exist in the INI
			foreach (var d in currentOrder)
			{
				if (pd.Sections.ContainsSection(d)) mainWindowState.SectionsOrdered.Add(d);
			}
			// Append any remaining sections not included yet
			foreach (SectionData s in pd.Sections)
			{
				if (!mainWindowState.SectionsOrdered.Contains(s.SectionName, StringComparer.OrdinalIgnoreCase))
					mainWindowState.SectionsOrdered.Add(s.SectionName);
			}

			if (mainWindowState.SectionsOrdered.Count > 0)
			{
				if (string.IsNullOrEmpty(mainWindowState.SelectedSection) || !mainWindowState.SectionsOrdered.Contains(mainWindowState.SelectedSection, StringComparer.OrdinalIgnoreCase))
				{
					mainWindowState.SelectedSection = mainWindowState.SectionsOrdered[0];
					var section = pd.Sections.GetSectionData(mainWindowState.SelectedSection);
					mainWindowState.SelectedKey = section?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
					mainWindowState.SelectedValueIndex = -1;
				}
			}
		}
		// Background worker tick invoked from E3.Process(): handle catalog loads and icon system
		public static void ProcessBackgroundWork()
		{
			if (window._state.State_CatalogLoadRequested && !window._state.State_CatalogLoading)
			{

				window._state.State_CatalogLoading = true;
				_log.WriteDelayed("Making background request", Logging.LogLevels.Debug);


				_log.WriteDelayed("Tryign to fetch data for user", Logging.LogLevels.Debug);

				// Always fetch via RouterServer, same as e3config
				string targetToon = GetSelectedIniOwnerName();

				_log.WriteDelayed($"Target tooon: {targetToon}", Logging.LogLevels.Debug);

				bool isLocal = string.IsNullOrEmpty(targetToon) || targetToon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);

				_log.WriteDelayed($"Are they local?: {isLocal}", Logging.LogLevels.Debug);

				try
				{
					if (isLocal)
					{
						ProcessBackground_UpdateLocalPlayer();
					}
					else
					{
						if (GetOnlineToonNames().ContainsKey(targetToon))
						{
							ProcessBackground_UpdateRemotePlayer(targetToon);
						}
					}

				}
				finally
				{
					window._state.State_CatalogLoading = false;
					window._state.State_CatalogLoadRequested = false;
				}
			}
			var foodDrinkState = window._state.GetState<State_FoodDrink>();
			// Food/Drink inventory scan (local or remote peer) — non-blocking
			if (foodDrinkState.ScanRequested && !foodDrinkState.Pending)
			{
				foodDrinkState.ScanRequested = false;
				try
				{
					string owner = GetSelectedIniOwnerName();
					bool isLocal = string.IsNullOrEmpty(owner) || owner.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);
					if (!isLocal)
					{
						// Start remote request and mark pending; actual receive handled below
						E3Core.Server.PubServer.AddTopicMessage($"InvReq-{owner}", foodDrinkState.Key);
						foodDrinkState.Pending = true;
						foodDrinkState.PendingToon = owner;
						foodDrinkState.PendingType = foodDrinkState.Key;
						foodDrinkState.TimeoutAt = Core.StopWatch.ElapsedMilliseconds + 2000;
						foodDrinkState.Status = $"Scanning {foodDrinkState.Key} on {owner}...";
					}
					else
					{
						var list = ScanInventoryForType(foodDrinkState.Key);
						foodDrinkState.Candidates = list ?? new List<string>();
						foodDrinkState.Status = foodDrinkState.Candidates.Count == 0 ? "No matches found in inventory." : $"Found {foodDrinkState.Candidates.Count} items.";
					}
				}
				catch (Exception ex)
				{
					foodDrinkState.Status = "Scan failed: " + (ex.Message ?? "error");
				}
			}
			// Remote response polling — checked each tick without blocking
			if (foodDrinkState.Pending)
			{
				try
				{
					string toon = foodDrinkState.PendingToon;
					string type = foodDrinkState.PendingType;
					string topic = $"InvResp-{E3.CurrentName}-{type}";
					// Prefer remote publisher bucket
					if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
						&& topics.TryGetValue(topic, out var entry))
					{
						string payload = entry.Data ?? string.Empty;
						int first = payload.IndexOf(':');
						int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
						string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
						try
						{
							var bytes = Convert.FromBase64String(b64);
							var joined = Encoding.UTF8.GetString(bytes);
							foodDrinkState.Candidates = (joined ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
								.Select(s => s.Trim())
								.Where(s => s.Length > 0)
								.Distinct(StringComparer.OrdinalIgnoreCase)
								.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
								.ToList();
						}
						catch
						{
							foodDrinkState.Candidates = new List<string>();
						}
						foodDrinkState.Status = foodDrinkState.Candidates.Count == 0 ? $"No {type} found on {toon}." : $"Found {foodDrinkState.Candidates.Count} items on {toon}.";
						foodDrinkState.Pending = false;
					}
					else if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
							 && topics2.TryGetValue(topic, out var entry2))
					{
						string payload = entry2.Data ?? string.Empty;
						int first = payload.IndexOf(':');
						int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
						string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
						try
						{
							var bytes = Convert.FromBase64String(b64);
							var joined = Encoding.UTF8.GetString(bytes);
							foodDrinkState.Candidates = (joined ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
								.Select(s => s.Trim())
								.Where(s => s.Length > 0)
								.Distinct(StringComparer.OrdinalIgnoreCase)
								.OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
								.ToList();
						}
						catch
						{
							foodDrinkState.Candidates = new List<string>();
						}
						foodDrinkState.Status = foodDrinkState.Candidates.Count == 0 ? $"No {type} found on {toon}." : $"Found {foodDrinkState.Candidates.Count} items on {toon}.";
						foodDrinkState.Pending = false;
					}
					else if (Core.StopWatch.ElapsedMilliseconds >= foodDrinkState.TimeoutAt)
					{
						foodDrinkState.Status = $"Remote {type} scan timed out for {toon}.";
						foodDrinkState.Candidates = new List<string>();
						foodDrinkState.Pending = false;
					}
				}
				catch
				{
					foodDrinkState.Status = "Remote scan error.";
					foodDrinkState.Candidates = new List<string>();
					foodDrinkState.Pending = false;
				}
			}

			var allPlayerState = window._state.GetState<State_AllPlayers>();
			if (window._state.Request_AllplayersRefresh && !allPlayerState.Refreshing)
			{
				var mainWindowState = window._state.GetState<State_MainWindow>();
		
				window._state.Request_AllplayersRefresh = false; // consume the pending request before we start
				allPlayerState.Refreshing = true;
				allPlayerState.ReqSection = mainWindowState.SelectedSection;
				allPlayerState.ReqKey = mainWindowState.SelectedKey;

				System.Threading.Tasks.Task.Run(() =>
				{
					try
					{

						allPlayerState.Status = "Refreshing...";

						var newRows = new List<KeyValuePair<string, string>>();
						string section = allPlayerState.ReqSection;
						string key = allPlayerState.ReqKey;

						foreach (var toon in GetOnlineToonNames().Keys)
						{
							string value = string.Empty;

							// First, try reading directly from the toon's local INI (if present on this machine)
							bool gotLocal = TryReadIniValueForToon(toon, section, key, out value);

							// If we didn't get a value locally and it's a remote toon, request from peer
							if (!gotLocal && !toon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
							{
								string requestTopic = $"ConfigValueReq-{toon}";
								string payload = $"{section}:{key}";
								E3Core.Server.PubServer.AddTopicMessage(requestTopic, payload);

								string responseTopic = $"ConfigValueResp-{E3.CurrentName}-{section}:{key}";
								long end = Core.StopWatch.ElapsedMilliseconds + 2000;
								bool found = false;
								while (Core.StopWatch.ElapsedMilliseconds < end)
								{
									if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics) &&
										topics.TryGetValue(responseTopic, out var entry))
									{
										value = entry.Data;
										found = true;
										break;
									}
									if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2) &&
										topics2.TryGetValue(responseTopic, out var entry2))
									{
										value = entry2.Data;
										found = true;
										break;
									}
									System.Threading.Thread.Sleep(25);
								}
								if (!found) value = "<timeout>";
							}

							newRows.Add(new KeyValuePair<string, string>(toon, value));
						}

						lock (allPlayerState.DataLock)
						{
							allPlayerState.Data_Rows = newRows;
							allPlayerState.Data_Edit = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							foreach (var row in newRows)
							{
								var toonKey = row.Key ?? string.Empty;
								allPlayerState.Data_Edit[toonKey] = row.Value ?? string.Empty;
							}
						}
						allPlayerState.LastUpdatedAt = Core.StopWatch.ElapsedMilliseconds;
					}
					catch (Exception ex)
					{
						allPlayerState.Status = "Refresh failed: " + ex.Message;
					}
					finally
					{
						allPlayerState.Refreshing = false;
					}
				});
			}
		}

		public static bool AddToActiveIni(string sectionName ,string key, string value)
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();
			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData(sectionName);
				if (section == null)
				{
					pd.Sections.AddSection(sectionName);
					section = pd.Sections.GetSectionData(sectionName);
				}
				if (section == null) return false;
				string baseKey = key ?? string.Empty;
				if (string.IsNullOrWhiteSpace(baseKey)) return false;
				string unique = baseKey;
				int idx = 1;
				while (section.Keys.ContainsKey(unique)) { unique = baseKey + " (" + idx.ToString() + ")"; idx++; if (idx > 1000) break; }
				if (!section.Keys.ContainsKey(unique))
				{
					section.Keys.AddKey(unique, value ?? string.Empty);
					mainWindowState.ConfigIsDirty = true;
					mainWindowState.SelectedSection = sectionName;
					mainWindowState.SelectedKey = unique;
					mainWindowState.SelectedValueIndex = -1;
					return true;
				}
				return false;
			}
			catch { return false; }
		}
		public static bool AddIfToActiveIni(string key, string value)
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();
			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData("Ifs");
				if (section == null)
				{
					pd.Sections.AddSection("Ifs");
					section = pd.Sections.GetSectionData("Ifs");
				}
				if (section == null) return false;
				string baseKey = key ?? string.Empty;
				if (string.IsNullOrWhiteSpace(baseKey)) return false;
				string unique = baseKey;
				int idx = 1;
				while (section.Keys.ContainsKey(unique)) { unique = baseKey + " (" + idx.ToString() + ")"; idx++; if (idx > 1000) break; }
				if (!section.Keys.ContainsKey(unique))
				{
					section.Keys.AddKey(unique, value ?? string.Empty);
					mainWindowState.ConfigIsDirty = true;
					mainWindowState.SelectedSection = "Ifs";
					mainWindowState.SelectedKey = unique;
					mainWindowState.SelectedValueIndex = -1;
					return true;
				}
				return false;
			}
			catch { return false; }
		}

		public static bool AddBurnToActiveIni(string key, string value)
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();

			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData("Burn");
				if (section == null)
				{
					pd.Sections.AddSection("Burn");
					section = pd.Sections.GetSectionData("Burn");
				}
				if (section == null) return false;
				string baseKey = key ?? string.Empty;
				if (string.IsNullOrWhiteSpace(baseKey)) return false;
				string unique = baseKey;
				int idx = 1;
				while (section.Keys.ContainsKey(unique)) { unique = baseKey + " (" + idx.ToString() + ")"; idx++; if (idx > 1000) break; }
				if (!section.Keys.ContainsKey(unique))
				{
					section.Keys.AddKey(unique, value ?? string.Empty);
					mainWindowState.ConfigIsDirty = true;
					mainWindowState.SelectedSection = "Burn";
					mainWindowState.SelectedKey = unique;
					mainWindowState.SelectedValueIndex = -1;
					return true;
				}
				return false;
			}
			catch { return false; }
		}
		
		public static bool DeleteKeyFromActiveIni(string sectionName, string keyName)
		{
			var mainWindowState = window._state.GetState<State_MainWindow>();

			try
			{
				var pd = GetActiveCharacterIniData();
				if (pd == null) return false;
				var section = pd.Sections.GetSectionData(sectionName ?? string.Empty);
				if (section == null || section.Keys == null) return false;
				if (!section.Keys.ContainsKey(keyName)) return false;
				section.Keys.RemoveKey(keyName);
				mainWindowState.ConfigIsDirty = true;
				mainWindowState.SelectedValueIndex = -1;
				// Pick a new selected key if any remain
				var nextKey = section.Keys.FirstOrDefault()?.KeyName ?? string.Empty;
				mainWindowState.SelectedKey = nextKey ?? string.Empty;
				return true;
			}
			catch { return false; }
		}

	}
}

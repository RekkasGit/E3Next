using E3Core.Classes;
using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace E3Core.Processors
{
    /// <summary>
    /// A catch all for ancillary commands and functions.
    /// </summary>
    public static class Basics
    {
        public static SavedGroupDataFile SavedGroupData = new SavedGroupDataFile();
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        public static bool IsPaused = false;
        public static List<int> GroupMembers = new List<int>();
        private static long _nextGroupCheck = 0;
        private static long _nextGroupCheckInterval = 1000;
        private static long _nextResourceCheck = 0;
        private static long _nextResourceCheckInterval = 1000;
        private static long _nextAutoMedCheck = 0;
        private static long _nextAutoMedCheckInterval = 1000;
        private static long _nextFoodCheck = 0;
        private static long _nextFoodCheckInterval = 1000;
        private static long _nextCursorCheck = 0;
        private static long _nextCursorCheckInterval = 1000;
        private static long _nextBoxCheck = 0;
        private static long _nextBoxCheckInterval = 10000;
        private static long _nextForageCheck = 0;
        private static long _nextForageCheckInterval = 10000;
        private static DateTime? _cursorOccupiedSince;
        private static TimeSpan _cursorOccupiedTime;
        private static TimeSpan _cursorOccupiedThreshold = new TimeSpan(0, 0, 0, 30);
        private static Int32 _cusrorPreviousID;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }

        /// <summary>
        /// Registers the events.
        /// </summary>
        public static void RegisterEvents()
        {



            EventProcessor.RegisterEvent("InviteToGroup", "(.+) invites you to join a group.", (x) =>
            {
                MQ.Cmd("/invite");
                MQ.Delay(300);
            });
            EventProcessor.RegisterEvent("InviteToRaid", "(.+) invites you to join a raid.", (x) =>
            {
                MQ.Delay(500);
                MQ.Cmd("/raidaccept");
            });

            EventProcessor.RegisterEvent("InviteToDZ", "(.+) tells you, 'dzadd'", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    MQ.Cmd($"/dzadd {x.match.Groups[1].Value}");
                }
            });

            EventProcessor.RegisterEvent("TellRelay", "(.+) tells you, '(.+)'", (x) =>
            {
                if (!E3.GeneralSettings.RelayTells) return;
                if (x.match.Groups.Count > 2)
                {
                    string name = x.match.Groups[1].Value;
                    Int32 petID = MQ.Query<Int32>("${Me.Pet.ID");
                    foreach (var spawn in _spawns.Get())
                    {
                        if(spawn.CleanName==name && spawn.TypeDesc=="NPC")
                        {
                            return;
                        }
                        if (petID > 0)
                        {
                            if (petID == spawn.ID)
                            {
                                return;
                            }
                        }


                    }
                    E3.Bots.Broadcast($"\agTell from: \ap{name}\ag, message: \ao'{x.match.Groups[2].Value}'");
                }
            });

            EventProcessor.RegisterEvent("Zoned", @"You have entered (.+)\.", (x) =>
            {
                //means we have zoned.
                _spawns.RefreshList();//make sure we get a new refresh of this zone.
                Loot.Reset();
                Movement.ResetKeepFollow();
                Assist.Reset();
                Pets.Reset();
                Nukes.Reset();
                Zoning.Zoned(MQ.Query<Int32>("${Zone.ID}"));
            });
            EventProcessor.RegisterEvent("Summoned", @"You have been summoned!", (x) =>
            {


                //coth, or summoned by a mob?
                ///a Tae Ew warder says 'You will not evade me, Alara!' 
                ///You have been summoned!
                if (Assist.AllowControl) return; //this is our driver and most likely a tank, ignore this.

                _spawns.RefreshList();//make sure we get a new refresh of this zone.
                //check to see if your target is on top of you, if so... well, good luck!
                Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                if(_spawns.TryByID(targetID, out var spawn))
                {
                    if(spawn.Distance<5 && spawn.Aggressive && spawn.TypeDesc=="NPC")
                    {
                        //oh dear, the mob is in your face, best of luck.
                        if(Movement.AnchorTarget>0)
                        {
                            Movement.MoveToAnchor();
                        }
                    }
                    else
                    {
                        //you have been cothed, reset stuff
                        Movement.Reset();
                        Assist.Reset();
                    }
                }
            });
            //
            EventProcessor.RegisterEvent("AskedForRaidInvite", "(.+) tells you, 'raidadd'", (x) =>
            {
                if (x.match.Groups.Count > 1)
                {
                    string  user = x.match.Groups[1].Value;

                    //need to be in the same zone
                    if(_spawns.TryByName(user,out var s))
                    {
                        MQ.Cmd($"/raidinvite {user}");
                    }
                }
            });
            EventProcessor.RegisterCommand("/e3settings", (x) =>
            {
                if(x.args.Count>0)
                {
                    if(x.args[0].Equals("createdefault", StringComparison.OrdinalIgnoreCase))
                    {
                        string newFileName = BaseSettings.GetSettingsFilePath("General Settings_default.ini");
                        if(System.IO.File.Exists(newFileName))
                        {
                            System.IO.File.Delete(newFileName);
                        }
                        E3.GeneralSettings.CreateSettings(newFileName);
                    }
                }

            });
            EventProcessor.RegisterCommand("/dropinvis", (x) =>
            {
                E3.Bots.BroadcastCommandToGroup("/makemevisible",x);
                MQ.Cmd("/makemevisible");
            });
            EventProcessor.RegisterCommand("/droplev", (x) =>
            {
                E3.Bots.BroadcastCommandToGroup("/removelev",x);
                MQ.Cmd("/removelev");
            });
            EventProcessor.RegisterCommand("/shutdown", (x) =>
            {
                MQ.Write("Isussing shutdown, setting process to false.");
                Core.IsProcessing = false;
                System.Threading.Thread.MemoryBarrier();

            });
            EventProcessor.RegisterCommand("/e3reload", (x) =>
            {
                E3.Bots.Broadcast("\aoReloading settings files...");

                if(x.args.Count>0)
                {
                    BaseSettings.CurrentSet = x.args[0].ToUpper();    
                }
                else
                {
                    BaseSettings.CurrentSet = String.Empty;
                }

                E3.CharacterSettings = new CharacterSettings();
                E3.AdvancedSettings = new AdvancedSettings();
                E3.GeneralSettings = new GeneralSettings();
                Rez.Reset();
                Loot.Reset();
                Assist.Reset();
                E3.Bots.Broadcast("\aoComplete!");

            });
           
            EventProcessor.RegisterCommand("/debug", (x) =>
            {

                var traceLevel = Logging.LogLevels.Trace;

                if(x.args.Count>0)
                {
                    if(String.Equals(x.args[0],"notrace", StringComparison.OrdinalIgnoreCase))
                    {
                        traceLevel = Logging.LogLevels.None;
                    }

                }

                if (Logging.MinLogLevelTolog == Logging.LogLevels.Error)
                {
                    Logging.MinLogLevelTolog = Logging.LogLevels.Debug;
                    Logging.TraceLogLevel = traceLevel;
                    MainProcessor.ProcessDelay = 1000;
                    _log.Write("Debug has been turned on:");
                }
                else
                {
                    _log.Write("Debug has been turned off.");
                    Logging.MinLogLevelTolog = Logging.LogLevels.Error;
                    Logging.TraceLogLevel = Logging.LogLevels.None;
                    MainProcessor.ProcessDelay = E3.ProcessDelay;
                }
            });

            EventProcessor.RegisterCommand("/pizza", (x) =>
            {
                if (E3.CurrentName == "Reek")
                {
                    System.Diagnostics.Process.Start("https://ordering.orders2.me/menu/pontillos-pizzeria-hudson-ridge");
                }
                else
                {
                    System.Diagnostics.Process.Start("https://www.dominos.com/en/restaurants?type=Delivery");
                }
            });

            if(E3.ServerName=="Lazarus")
            {
                EventProcessor.RegisterCommand("/baz", (x) =>
                {

                    System.Diagnostics.Process.Start("https://www.lazaruseq.com/Magelo/index.php?page=bazaar");

                });
            }
        
            EventProcessor.RegisterCommand("/yes", (x) =>
            {
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommandToGroup("/yes all",x);
                }
                e3util.ClickYesNo(true);
            });
            EventProcessor.RegisterCommand("/no", (x) =>
            {
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommandToGroup("/no all",x);
                }
                e3util.ClickYesNo(false);
            });

          
            EventProcessor.RegisterCommand("/reportaa", (x) =>
            {
                List<string> validReportChannels = new List<string>() { "/g", "/gu", "/say", "/rsay","/gsay", "/rs" };

                string channel = "/gu";
                if(x.args.Count>0 && validReportChannels.Contains(x.args[0], StringComparer.OrdinalIgnoreCase))
                {

                    channel = x.args[0];
                }
                MQ.Cmd($"{channel} AA Spent-Available: ${{Me.AAPointsSpent}}-${{Me.AAPoints}}");
                E3.Bots.BroadcastCommand($"{channel} AA Spent-Available: ${{Me.AAPointsSpent}}-${{Me.AAPoints}}", true);

            });

            EventProcessor.RegisterCommand("/bark", (x) =>
            {
                //rebuild the bark message, and do a /say
                if (x.args.Count > 0)
                {
                    int targetid = MQ.Query<int>("${Target.ID}");
                    if (targetid > 0)
                    {
                        Spawn s;
                        if (_spawns.TryByID(targetid, out s))
                        {
                            e3util.TryMoveToLoc(s.X, s.Y,s.Z);
                            System.Text.StringBuilder sb = new StringBuilder();
                            bool first = true;
                            foreach (string arg in x.args)
                            {
                                if (!first) sb.Append(" ");
                                sb.Append(arg);
                                first = false;
                            }
                            string message = sb.ToString();
                            E3.Bots.BroadcastCommandToGroup($"/bark-send {targetid} \"{message}\"",x);
                            for (int i = 0; i < 5; i++)
                            {
                                MQ.Cmd($"/say {message}");
                                MQ.Delay(1500);
                                if (EventProcessor.EventList["Zoned"].queuedEvents.Count > 0)
                                {
                                    //means we have zoned and can stop
                                    break;
                                }
                            }
                        }
                    }
                }
            });

            EventProcessor.RegisterCommand("/bark-send", (x) =>
            {
                if (x.args.Count > 1)
                {
                    int targetid;
                    if (int.TryParse(x.args[0], out targetid))
                    {
                        if (targetid > 0)
                        {
                            Spawn s;
                            if (_spawns.TryByID(targetid, out s))
                            {
                                Casting.TrueTarget(targetid);
                                MQ.Delay(100);
                                e3util.TryMoveToLoc(s.X, s.Y,s.Z);

                                string message = x.args[1];
                                int currentZone = Zoning.CurrentZone.Id;
                                for (int i = 0; i < 5; i++)
                                {
                                    MQ.Cmd($"/say {message}",1000);
                                    
                                    int tzone = MQ.Query<int>("${Zone.ID}");
                                    if (tzone != currentZone)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            });

            EventProcessor.RegisterCommand("/evac", (x) =>
            {
                if (E3.CurrentClass == Class.Druid || E3.CurrentClass == Class.Wizard)
                {
                    //someone told us to gate

                    if (E3.CharacterSettings.CasterEvacs.Count > 0)
                    {
                        foreach (var spell in E3.CharacterSettings.CasterEvacs)
                        {
                            if (!Casting.SpellInCooldown(spell))
                            {
                                if (Casting.CheckReady(spell) && Casting.CheckMana(spell))
                                {
                                    Casting.Cast(0, spell);
                                    return;
                                }
                            }
                        }
                    }

                    Spell s;
                    if (!Spell.LoadedSpellsByName.TryGetValue("Exodus", out s))
                    {
                        s = new Spell("Exodus");
                    }
                    if (Casting.CheckReady(s))
                    {
                        Casting.Cast(0, s);
                    }
                    else
                    {
                        //lets try and do evac spell?
                        string spellToCheck = string.Empty;
                        if (E3.CurrentClass == Class.Wizard)
                        {
                            spellToCheck = "Evacuate";
                        }
                        else if (E3.CurrentClass == Class.Druid)
                        {
                            spellToCheck = "Succor";
                        }

                        if (spellToCheck != string.Empty && MQ.Query<bool>($"${{Me.Book[{spellToCheck}]}}"))
                        {

                            if (!Spell.LoadedSpellsByName.TryGetValue(spellToCheck, out s))
                            {
                                s = new Spell(spellToCheck);
                            }
                            if (Casting.CheckReady(s) && Casting.CheckMana(s))
                            {
                                Casting.Cast(0, s);
                            }
                        }
                    }
                }
                else
                {
                    E3.Bots.BroadcastCommandToGroup("/evac me");
                }
            });

            EventProcessor.RegisterCommand("/e3p", (x) =>
            {
                //swap them

                if (x.args.Count > 0)
                {
                    if (x.args[0].Equals("off", StringComparison.OrdinalIgnoreCase))
                    {
                        if (IsPaused)
                        {
                            IsPaused = false;
                            E3.Bots.Broadcast("\agRunning E3 again!");
                        }
                    }
                    else if (x.args[0].Equals("on", StringComparison.OrdinalIgnoreCase))
                    {
                        if(!IsPaused)
                        {
                            IsPaused = true;
                            E3.Bots.Broadcast("\arPAUSING E3!");

                        }
                    }
                }
                else
                {
                    IsPaused = IsPaused ? false : true;
                    if (IsPaused) E3.Bots.Broadcast("\arPAUSING E3!");
                    if (!IsPaused) E3.Bots.Broadcast("\agRunning E3 again!");

                }

            });

            EventProcessor.RegisterCommand("/savegroup", (x) =>
            {
                var args = x.args;
                if (args.Count == 0)
                    return;

                MQ.Write($"\agCreating new saved group by the name of {args[0]}");
                SavedGroupData.SaveData(args[0]);
                MQ.Write($"\agSuccessfully created {args[0]}");
            });

            EventProcessor.RegisterCommand("/group", (x) =>
            {
                var args = x.args;
                if (args.Count == 0)
                    return;

                var server = MQ.Query<string>("${MacroQuest.Server}");
                var groupKey = server + "_" + args[0];
                var savedGroups = SavedGroupData.GetData();
                if (!savedGroups.TryGetValue(groupKey, out var groupMembers))
                {
                    MQ.Write($"\arNo group with the name of {args[0]} found in Saved Groups.ini. Use /savegroup groupName to create one");
                    return;
                }
                MQ.Cmd("/disband");
                MQ.Cmd("/raiddisband");
                E3.Bots.BroadcastCommand("/raiddisband");
                E3.Bots.BroadcastCommand("/disband");

                MQ.Delay(1500);
                if (MQ.Query<int>("${Group}") > 0)
                {
                    MQ.Delay(2000, "!${Group}");
                }

                foreach (var member in groupMembers)
                {
                    MQ.Cmd($"/invite {member}");
                }
            });

            EventProcessor.RegisterCommand("/wiki", x =>
            {
                if (x.args.Count == 0)
                {
                    Process.Start(new ProcessStartInfo { FileName = "https://github.com/RekkasGit/E3Next/wiki", UseShellExecute = true });
                }
                else if (string.Equals(x.args[0], "Laz", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo { FileName = "https://www.lazaruseq.com/Wiki/index.php/Main_Page", UseShellExecute = true });
                }
            });
        }

        /// <summary>
        /// Refreshes the group member cache.
        /// </summary>
        public static void RefreshGroupMembers()
        {
            if (!e3util.ShouldCheck(ref _nextGroupCheck, _nextGroupCheckInterval)) return;

            int groupCount = MQ.Query<int>("${Group}");
            groupCount++;
            GroupMembers.Clear();
            //refresh group members.
            
            for (int i = 0; i < groupCount; i++)
            {
                int id = MQ.Query<int>($"${{Group.Member[{i}].ID}}");
                if(id>0)
                {
                    GroupMembers.Add(id);
                }
            }
        }

        /// <summary>
        /// Am I dead?
        /// </summary>
        /// <returns>Returns a bool indicating whether or not you're dead.</returns>
        public static bool AmIDead()
        {
            //scan through our inventory looking for a container.
            for (int i = 1; i <= 10; i++)
            {
                bool SlotExists = MQ.Query<bool>($"${{Me.Inventory[pack{i}]}}");
                if (SlotExists)
                {
                    return false;
                }
            }
            if(MQ.Query<Int32>("${Me.Inventory[Chest].ID}")>0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Am I in combat?
        /// </summary>
        /// <returns>Returns a bool indicating whether or not you're in combat</returns>
        public static bool InCombat()
        {
            bool inCombat = Assist.IsAssisting || MQ.Query<bool>("${Me.Combat}") || MQ.Query<bool>("${Me.CombatState.Equal[Combat]}");
            return inCombat;
        }
        public static bool InGameCombat()
        {
            bool inCombat =  MQ.Query<bool>("${Me.CombatState.Equal[Combat]}");
            return inCombat;
        }
        /// <summary>
        /// Checks the mana resources, and does actions to regenerate mana during combat.
        /// </summary>
        [ClassInvoke(Data.Class.ManaUsers)]
        public static void CheckManaResources()
        {
            if (!e3util.ShouldCheck(ref _nextResourceCheck, _nextResourceCheckInterval)) return;

            using (_log.Trace())
            {
                if (E3.IsInvis) return;
                if (Basics.AmIDead()) return;
                

                int pctMana = MQ.Query<int>("${Me.PctMana}");
                var pctHps = MQ.Query<int>("${Me.PctHPs}");
                int currentHps = MQ.Query<int>("${Me.CurrentHPs}");

                if (E3.CurrentClass == Data.Class.Enchanter)
                {
                    bool manaDrawBuff = MQ.Query<bool>("${Bool[${Me.Buff[Mana Draw]}]}") || MQ.Query<bool>("${Bool[${Me.Song[Mana Draw]}]}");
                    if (manaDrawBuff)
                    {
                        if (pctMana > 50)
                        {
                            return;
                        }
                    }
                }

                if (E3.CurrentClass == Data.Class.Necromancer)
                {
                    bool deathBloom = MQ.Query<bool>("${Bool[${Me.Buff[Death Bloom]}]}") || MQ.Query<bool>("${Bool[${Me.Song[Death Bloom]}]}");
                    if (deathBloom)
                    {
                        return;
                    }
                }

                if (E3.CurrentClass == Data.Class.Shaman)
                {
                    bool canniReady = MQ.Query<bool>("${Me.AltAbilityReady[Cannibalization]}");

                    if (canniReady && currentHps > 7000 && MQ.Query<double>("${Math.Calc[${Me.MaxMana} - ${Me.CurrentMana}]}") > 4500)
                    {
                        Spell s;
                        if (!Spell.LoadedSpellsByName.TryGetValue("Cannibalization", out s))
                        {
                            s = new Spell("Cannibalization");
                        }
                        if (s.CastType != CastType.None)
                        {
                            Casting.Cast(0, s);
                            return;
                        }
                    }

                    //don't canni if we are moving/following
                    if (!(Movement.IsMoving() || Movement.Following))
                    {
                        var canniSpell = E3.CharacterSettings.CanniSpell;
                        if (canniSpell != null && E3.CharacterSettings.AutoCanni)
                        {
                            if (Casting.CheckReady(canniSpell))
                            {
                                pctHps = MQ.Query<int>("${Me.PctHPs}");
                                var hpThresholdDefined = canniSpell.MinHP > 0;
                                var manaThresholdDefined = canniSpell.MaxMana > 0;
                                bool castCanniSpell = false;
                                bool hpThresholdMet = false;
                                bool manaThresholdMet = false;

                                if (hpThresholdDefined)
                                {
                                    if (pctHps > canniSpell.MinHP)
                                    {
                                        hpThresholdMet = true;
                                    }
                                }

                                if (manaThresholdDefined)
                                {
                                    if (pctMana < canniSpell.MaxMana)
                                    {
                                        manaThresholdMet = true;
                                    }
                                }

                                if (hpThresholdDefined && manaThresholdDefined)
                                {
                                    castCanniSpell = hpThresholdMet && manaThresholdMet;
                                }
                                else if (hpThresholdDefined && !manaThresholdDefined)
                                {
                                    castCanniSpell = hpThresholdMet;
                                }
                                else if (manaThresholdDefined && !hpThresholdDefined)
                                {
                                    castCanniSpell = manaThresholdMet;
                                }
                                else
                                {
                                    castCanniSpell = true;
                                }

                                if (castCanniSpell)
                                {
                                    Casting.Cast(0, canniSpell);
                                }
                            }
                        }
                    }
                    
                }

                if (MQ.Query<bool>("${Me.ItemReady[Summoned: Large Modulation Shard]}"))
                {
                    if (MQ.Query<double>("${Math.Calc[${Me.MaxMana} - ${Me.CurrentMana}]}") > 3500 && currentHps > 6000)
                    {
                        Spell s;
                        if (!Spell.LoadedSpellsByName.TryGetValue("Summoned: Large Modulation Shard", out s))
                        {
                            s = new Spell("Summoned: Large Modulation Shard");
                        }
                        if (s.CastType != CastType.None)
                        {
                            Casting.Cast(0, s);
                            return;
                        }
                    }
                }
                if (MQ.Query<bool>("${Me.ItemReady[Azure Mind Crystal III]}"))
                {
                    if (MQ.Query<double>("${Math.Calc[${Me.MaxMana} - ${Me.CurrentMana}]}") > 3500)
                    {
                        Spell s;
                        if (!Spell.LoadedSpellsByName.TryGetValue("Azure Mind Crystal III", out s))
                        {
                            s = new Spell("Azure Mind Crystal III");
                        }
                        if (s.CastType != CastType.None)
                        {
                            Casting.Cast(0, s);
                            return;
                        }
                    }
                }

                if (E3.CurrentClass == Data.Class.Necromancer && pctMana < 50 && E3.CurrentInCombat)
                {
                    bool deathBloomReady = MQ.Query<bool>("${Me.AltAbilityReady[Death Bloom]}");
                    if (deathBloomReady && currentHps > 8000)
                    {
                        Spell s;
                        if (!Spell.LoadedSpellsByName.TryGetValue("Death Bloom", out s))
                        {
                            s = new Spell("Death Bloom");
                        }
                        if (s.CastType != CastType.None)
                        {
                            Casting.Cast(0, s);
                            return;
                        }
                    }
                }
                if (E3.CurrentClass == Data.Class.Cleric && pctMana < 30 && E3.CurrentInCombat)
                {
                    bool miracleReady = MQ.Query<bool>("${Me.AltAbilityReady[Quiet Miracle]}");
                    if (miracleReady)
                    {
                        Spell s;
                        if (!Spell.LoadedSpellsByName.TryGetValue("Quiet Miracle", out s))
                        {
                            s = new Spell("Quiet Miracle");
                        }
                        if (s.CastType != CastType.None)
                        {
                            Casting.Cast(E3.CurrentId, s);
                            return;
                        }
                    }
                }
                if (E3.CurrentClass == Data.Class.Wizard && pctMana < 40 && E3.CurrentInCombat)
                {
                    bool harvestReady = MQ.Query<bool>("${Me.AltAbilityReady[Harvest of Druzzil]}");
                    if (harvestReady)
                    {
                        Spell s;
                        if (!Spell.LoadedSpellsByName.TryGetValue("Harvest of Druzzil", out s))
                        {
                            s = new Spell("Harvest of Druzzil");
                        }
                        if (s.CastType != CastType.None)
                        {
                            Casting.Cast(0, s);
                            return;
                        }
                    }
                }
                if (E3.CurrentClass == Data.Class.Enchanter && pctMana < 50 && E3.CurrentInCombat)
                {
                    bool manaDrawReady = MQ.Query<bool>("${Me.AltAbilityReady[Mana Draw]}");
                    if (manaDrawReady)
                    {
                        Spell s;
                        if (!Spell.LoadedSpellsByName.TryGetValue("Mana Draw", out s))
                        {
                            s = new Spell("Mana Draw");
                        }
                        if (s.CastType != CastType.None)
                        {
                            Casting.Cast(0, s);
                            return;
                        }
                    }
                }

               

                //manastone code
                int minMana = E3.GeneralSettings.ManaStone_InCombatMinMana;
                int minHP = E3.GeneralSettings.ManaStone_MinHP;
                int maxMana = E3.GeneralSettings.ManaStone_InCombatMaxMana;
                int maxLoop = E3.GeneralSettings.ManaStone_NumberOfLoops;
                int totalClicksToTry =E3.GeneralSettings.ManaStone_NumerOfClicksPerLoop;
                int delayBetweenClicks = E3.GeneralSettings.ManaStone_DelayBetweenLoops;
                //Int32 minManaToTryAndHeal = 1000;
                bool manastone_UseInCombat = E3.GeneralSettings.ManaStone_EnabledInCombat;

                if (!InCombat())
                {
                    minMana = E3.GeneralSettings.ManaStone_OutOfCombatMinMana;
                    maxMana = E3.GeneralSettings.ManaStone_OutOfCombatMaxMana;
                }

                if(E3.CharacterSettings.Manastone_OverrideGeneralSettings)
                {
                    minMana = E3.CharacterSettings.ManaStone_InCombatMinMana;
                    minHP = E3.CharacterSettings.ManaStone_MinHP;
                    maxMana = E3.CharacterSettings.ManaStone_InCombatMaxMana;
                    maxLoop = E3.CharacterSettings.ManaStone_NumberOfLoops;
                    totalClicksToTry = E3.CharacterSettings.ManaStone_NumerOfClicksPerLoop;
                    delayBetweenClicks = E3.CharacterSettings.ManaStone_DelayBetweenLoops;
                    //Int32 minManaToTryAndHeal = 1000;

                    if (!InCombat())
                    {
                        minMana = E3.CharacterSettings.ManaStone_OutOfCombatMinMana;
                        maxMana = E3.CharacterSettings.ManaStone_OutOfCombatMaxMana;
                    }
                    manastone_UseInCombat = E3.CharacterSettings.ManaStone_EnabledInCombat;
                }

                if (!manastone_UseInCombat)
                {
                    if (InCombat()) return;
                }

                if (pctMana > minMana) return;
                pctHps = MQ.Query<int>("${Me.PctHPs}");
                if (pctHps < minHP) return;
                //no manastone in pok
                bool pok = MQ.Query<bool>("${Zone.ShortName.Equal[poknowledge]}");
                if (pok) return;

                bool hasManaStone = MQ.Query<bool>("${Bool[${FindItem[=Manastone]}]}");
                
                string manastoneName = "Manastone";
                if(!hasManaStone)
                {
                    hasManaStone = MQ.Query<bool>("${Bool[${FindItem[=Apocryphal Manastone]}]}");
                    if(hasManaStone) manastoneName = "Apocryphal Manastone";
                    if(!hasManaStone)
                    {
                        hasManaStone = MQ.Query<bool>("${Bool[${FindItem[=Rose Colored Manastone]}]}");
                        if (hasManaStone) manastoneName = "Rose Colored Manastone";
                    }
                }
                bool amIStanding = MQ.Query<bool>("${Me.Standing}");

                if (hasManaStone && amIStanding)
                {
                    string manastoneCommand = $"/useitem \"{manastoneName}\"";
                    e3util.YieldToEQ();
                    if (MQ.Query<bool>("${Me.Invis}")) return;

                    MQ.Write("\agUsing Manastone...");
                    pctHps = MQ.Query<int>("${Me.PctHPs}");
                    pctMana = MQ.Query<int>("${Me.PctMana}");
                    int currentLoop = 0;
                    while (pctHps > minHP && pctMana < maxMana)
                    {
                        currentLoop++;
                        int currentMana = MQ.Query<int>("${Me.CurrentMana}");

                        for (int i = 0; i < totalClicksToTry; i++)
                        {
                            MQ.Cmd(manastoneCommand);
                        }
                        //allow mq to have the commands sent to the server
                        MQ.Delay(delayBetweenClicks);

                        if (MQ.Query<bool>("${Me.Invis}")) return;
                        if ((E3.CurrentClass & Class.Priest) == E3.CurrentClass && Basics.InCombat())
                        {
                            if (Heals.SomeoneNeedsHealing(currentMana, pctMana))
                            {
                                return;
                            }
                        }
                        if (currentLoop > maxLoop)
                        {
                            return;
                        }

                        pctHps = MQ.Query<int>("${Me.PctHPs}");
                        pctMana = MQ.Query<int>("${Me.PctMana}");
                    }
                }
            }
        }

        /// <summary>
        /// Do I need to med?
        /// </summary>
        [ClassInvoke(Data.Class.All)]
        public static void CheckAutoMed()
        {
            if (!e3util.ShouldCheck(ref _nextAutoMedCheck, _nextAutoMedCheckInterval)) return;
            int autoMedPct = E3.GeneralSettings.General_AutoMedBreakPctMana;
            if (autoMedPct == 0) return;
            if (!E3.CharacterSettings.Misc_AutoMedBreak) return;
            using (_log.Trace())
            {
                bool onMount = MQ.Query<bool>("${Me.Mount.ID}");

                
                if (onMount|| Assist.IsAssisting ) return;

                if(!Movement.StandingStillForTimePeriod())
                {
                    if (Movement.Following || Movement.IsMoving()) return;
                }

                bool amIStanding = MQ.Query<bool>("${Me.Standing}");
                string combatState = MQ.Query<string>("${Me.CombatState}");
                if (amIStanding && autoMedPct > 0 && combatState!="COMBAT")
                {
                    int pctMana = MQ.Query<int>("${Me.PctMana}");
                    int pctEndurance = MQ.Query<int>("${Me.PctEndurance}");

                    if (pctMana < autoMedPct && (E3.CurrentClass & Class.ManaUsers) == E3.CurrentClass)
                    {
                        MQ.Cmd("/sit");
                        return;
                    }

                    if (pctEndurance < autoMedPct)
                    {
                        MQ.Cmd("/sit");
                    }
                }
            }
        }

        /// <summary>
        /// Checks hunger and thirst levels, and eats the configured food and drink in order to save stat food.
        /// </summary>
        [ClassInvoke(Class.All)]
        public static void CheckFood()
        {
            if (!e3util.ShouldCheck(ref _nextFoodCheck, _nextFoodCheckInterval)) return;

            if (!E3.CharacterSettings.Misc_AutoFoodEnabled) return;
            using (_log.Trace())
            {
                var toEat = E3.CharacterSettings.Misc_AutoFood;
                var toDrink = E3.CharacterSettings.Misc_AutoDrink;

                if (MQ.Query<bool>($"${{FindItem[{toEat}].ID}}") && MQ.Query<int>("${Me.Hunger}") < 4500)
                {
                    MQ.Cmd($"/useitem \"{toEat}\"");
                }

                if (MQ.Query<bool>($"${{FindItem[{toDrink}].ID}}") && MQ.Query<int>("${Me.Thirst}") < 4500)
                {
                    MQ.Cmd($"/useitem \"{toDrink}\"");
                }
            }
        }

        /// <summary>
        /// Uses box of misfit prizes on if necessary.
        /// </summary>
        [ClassInvoke(Class.All)]
        public static void CheckBox()
        {
            if (!E3.GeneralSettings.AutoMisfitBox) return;
            if (InCombat()) return;
            if (!e3util.ShouldCheck(ref _nextBoxCheck, _nextBoxCheckInterval)) return;

            var box = "Box of Misfit Prizes";
            if (!MQ.Query<bool>($"${{FindItem[={box}]}}")) return;
            if (!MQ.Query<bool>($"${{FindItem[={box}].NoDrop}}")) return;
            if (!MQ.Query<bool>($"${{Me.ItemReady[={box}]}}")) return;
            if (MQ.Query<bool>("${Cursor.ID}"))
            {
                e3util.ClearCursor();
            }

            var ammoItem = MQ.Query<string>("${Me.Inventory[ammo]}");
            if (!string.Equals(ammoItem, box, StringComparison.OrdinalIgnoreCase))
            {
                MQ.Cmd($"/exchange \"{box}\" ammo");
            }

            Casting.Cast(0, new Spell(box));
            MQ.Delay(6000);
            var boxId = MQ.Query<int>($"${{NearestSpawn[radius 20 {box}].ID}}");
            MQ.Delay(100);
            if (!Casting.TrueTarget(boxId))
            {
                MQ.Write("\arWhere box?");
                e3util.Exchange("ammo", ammoItem);
                return;
            }

            MQ.Delay(100);
            MQ.Cmd("/open", 500);
            _spawns.RefreshList();
            boxId = MQ.Query<int>($"${{NearestSpawn[corpse radius 20 {box}].ID}}");
            var boxSpawn = _spawns.Get().FirstOrDefault(f => f.ID == boxId);
            if (!Casting.TrueTarget(boxSpawn?.ID ?? 0))
            {
                MQ.Write("\arWhere box?");
                e3util.Exchange("ammo", ammoItem);
                return;
            }

            if (boxSpawn != null)
            {
                Loot.LootCorpse(boxSpawn, true);
                MQ.Cmd("/nomodkey /notify LootWnd DoneButton leftmouseup");
            }
            else
            {
                MQ.Write("\arUnable to find spawn for box in spawn cache; skipping looting");
            }

            if (!string.Equals(ammoItem, box))
            {
                e3util.Exchange("ammo", ammoItem);
            }
        }

        [ClassInvoke(Class.All)]
        public static void CheckForage()
        {
            if (!E3.CharacterSettings.Misc_AutoForage) return;
            if (!e3util.ShouldCheck(ref _nextForageCheck, _nextForageCheckInterval)) return;

            bool forageReady = MQ.Query<bool>("${Me.AbilityReady[Forage]}");

            if(forageReady)
            {
                MQ.Write("\agAuto Foraging....");
                MQ.Cmd("/doability forage");
                MQ.Delay(2000, "${Bool[${Cursor.ID}]}");
                MQ.Delay(500);
                bool cursorItem = MQ.Query<bool>("${Bool[${Cursor.ID}]}");
                if(cursorItem)
                {

                    //auto delete stuff on cursor that is configured to do so
                    string autoinvItem = MQ.Query<string>("${Cursor}");
                    if (E3.CharacterSettings.Cursor_Delete.Contains(autoinvItem, StringComparer.OrdinalIgnoreCase))
                    {
                        //configured to delete this item.
                        MQ.Cmd("/destroy");
                        if (autoinvItem != "NULL")
                        {
                            E3.Bots.Broadcast($"\agAutoDestroy\aw:\ao{autoinvItem}");
                        }
                        MQ.Delay(300);
                        return;
                    }
                    else
                    {

                        e3util.ClearCursor();

                    }

                }
            }
         
        }




        /// <summary>
        /// Checks the cursor and clears it if necessary.
        /// </summary>
        [ClassInvoke(Class.All)]
        public static void CheckCursor()
        {
            if (!e3util.ShouldCheck(ref _nextCursorCheck, _nextCursorCheckInterval)) return;
            using (_log.Trace())
            {
                bool itemOnCursor = MQ.Query<bool>("${Bool[${Cursor.ID}]}");
                
                if (itemOnCursor)
                {
                    //auto delete stuff on cursor that is configured to do so
                    string autoinvItem = MQ.Query<string>("${Cursor}");
                    if (E3.CharacterSettings.Cursor_Delete.Contains(autoinvItem, StringComparer.OrdinalIgnoreCase))
                    {
                        //configured to delete this item.
                        MQ.Cmd("/destroy");
                        if (autoinvItem != "NULL")
                        {
                            E3.Bots.Broadcast($"\agAutoDestroy\aw:\ao{autoinvItem}");
                        }
                        MQ.Delay(300);
                        return;
                    }

                    Int32 itemID = MQ.Query<Int32>("${Cursor.ID}");

                    if (_cursorOccupiedSince == null || itemID!=_cusrorPreviousID)
                    {
                        _cursorOccupiedSince = DateTime.Now;
                        _cusrorPreviousID = itemID;
                    }

                   
                    if (!e3util.IsManualControl() || Basics.InCombat())
                    {
                        bool regenItem = MQ.Query<bool>("${Cursor.Name.Equal[Azure Mind Crystal III]}") || MQ.Query<bool>("${Cursor.Name.Equal[Summoned: Large Modulation Shard]}") || MQ.Query<bool>("${Cursor.Name.Equal[Sanguine Mind Crystal III]}");

                        if (regenItem)
                        {
                            int charges = MQ.Query<int>("${Cursor.Charges}");
                            if (charges == 3)
                            {
                                e3util.ClearCursor();
                                _cursorOccupiedSince = null;
                            }
                        }
                        else
                        {
                            bool orb = MQ.Query<bool>("${Cursor.Name.Equal[Molten orb]}") || MQ.Query<bool>("${Cursor.Name.Equal[Lava orb]}");
                            if (orb)
                            {
                                int charges = MQ.Query<int>("${Cursor.Charges}");
                                if (charges == 10)
                                {
                                    e3util.ClearCursor();
                                    _cursorOccupiedSince = null;
                                }
                            }
                            else
                            {
                                _cursorOccupiedTime = DateTime.Now - _cursorOccupiedSince.GetValueOrDefault();
                                // if there's a thing on our cursor for > 30 seconds, inventory it
                                if (_cursorOccupiedTime > _cursorOccupiedThreshold)
                                {
                                    e3util.ClearCursor();
                                    _cursorOccupiedSince = null;
                                }
                            }
                        }
                    }
                    
                }
                else
                {
                    _cursorOccupiedSince = null;
                    _cusrorPreviousID = -1;
                }
            }
        }
    }
}

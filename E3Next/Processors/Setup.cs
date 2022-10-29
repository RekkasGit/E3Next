using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Setup
    {

        static public Boolean _reloadOnLoot = false;
        static public string _missingSpellItem = string.Empty;
        static public Int32 _numInventorySlots = 10;
        static public Int32 _previousSpellGemThatWasCast = -1;
        public const string _e3Version = "1.0";
        public static Boolean _Debug = true;
        public const string _macroData_Ini = @"e3 Macro Inis\e3 Data.ini";
        public static string _generalSettings_Ini = @"e3 Macro Inis\General Settings.ini";
        public static string _advancedSettings_Ini = @"e3 Macro Inis\Advanced Settingse.ini";
        public static string _character_Ini = @"e3 Bot Inis\{CharacterName}_{ServerName}.ini";
        
        public static string _serverNameForIni = "PEQTGC"; //project eq, the grand creation, where e3 was born i believe.
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;

        public static Boolean Init()
        {
            using (_log.Trace())
            {
            
                //lets init server name
                //laz server specific,otherwise default
                if (MQ.Query<bool>($"${{MacroQuest.Server.Equal[Project Lazarus]}}"))
                {
                    _serverNameForIni = "Lazarus";
                }
                

                MQ.Write($"Loading nE³xt v{_e3Version}...");

                InitPlugins();
                InitServerNameForIni();
                //ValidateIniFiles();
                //LoadOrCreateCharacterSettings();
                InitSubSystems();
                return true;
            }

        }
        private static void InitSubSystems()
        {
            Casting.Init();
            Basics.Init();
            Assist.Init();
            DebuffDot.Init();
            Burns.Init();
            Loot.Init();
            WaitForRez.Init();
            Sell.Init();
            NowCast.Init();
            Pets.Init();
            Alerts.Init();
            BuffCheck.Init();
            BegForBuffs.Init();
            Cures.Init();
            GiveMe.Init();
            Movement.Init();
            Inventory.Init();
        }
        private static void LoadOrCreateCharacterSettings()
        {
            using (_log.Trace())
            {
                //TODO: Remove all INI functionality out of MQ
                string botIniVersion = MQ.Query<string>($"${{ini[{_macroData_Ini},${{Me.CleanName}}-{_serverNameForIni},Bot_Ini version]}}");
                bool versionSame = String.Equals(botIniVersion, _e3Version, StringComparison.OrdinalIgnoreCase);
                bool botIniExists = MQ.Query<bool>($"${{ini[{_character_Ini}]}}");

                if (!versionSame || !botIniExists)
                {
                    //make new character setting
                    MQ.Cmd("/echo Creating ${Me.CleanName}'${If[${Me.CleanName.Right[1].Equal[s]},,s]} settings file...");
                }
            }
        }
        private static void InitServerNameForIni()
        {   //TODO: Remove all INI functionality out of MQ
            using (_log.Trace())
            {
                if (!MQ.Query<Boolean>($"${{Ini[{_macroData_Ini}].Length}}"))
                {
                    MQ.Write("Welcome to e3 next! preforming first time setup...");
                    MakeMacroDataInis();
                }
            }
        }
        private static void ValidateIniFiles()
        {
            using (_log.Trace())
            {
                //TODO: Remove all INI reads from MQ and just do it in C#
                if (!MQ.Query<bool>($"${{ini[${_macroData_Ini},File Paths,General Settings].Length}}"))
                {
                    MQ.Write("ERROR: Could not find designated file path for [General Settings].  Please review review settings in [${MacroData_Ini} > File Paths].");
                    MQ.Cmd("/beep");
                    E3.Shutdown();
                }
                _generalSettings_Ini = MQ.Query<string>($"${{ini[${_macroData_Ini},File Paths,General Settings]}}");

                if (!MQ.Query<bool>($"${{ini[${_macroData_Ini},File Paths,Advanced Settings].Length}}"))
                {
                    MQ.Write($"ERROR: Could not find designated file path for [Advanced Settings].  Please review review settings in [${_macroData_Ini} > File Paths].");
                    MQ.Cmd("/beep");
                    E3.Shutdown();
                }
                _advancedSettings_Ini = MQ.Query<string>($"${{ini[${_macroData_Ini},File Paths,Advanced Settings]}}");

                if (!MQ.Query<bool>($"${{ini[${_macroData_Ini},File Paths,Bot Settings].Length}}"))
                {
                    MQ.Write($"ERROR: Could not find designated file path for  [Bot Settings].  Please review review settings in [${_macroData_Ini} > File Paths].");
                    MQ.Cmd("/beep");
                    E3.Shutdown();
                }

                _character_Ini = MQ.Query<string>($"${{ini[${_macroData_Ini},File Paths,Bot Settings]}}") + MQ.Query<string>("${Me.CleanName}") + $"_{_serverNameForIni}.ini";

            }
        }

        private static void MakeMacroDataInis()
        {
            return;
            //using (_log.Trace())
            //{
            //    //TODO:come back later and do this ourselves
            //    MQ.Cmd("/ini \"e3 Macro Inis\\e3 Data.ini\" \"e3 Build\" \"Version\"");
            //    MQ.Cmd("/ini \"e3 Macro Inis\\e3 Data.ini\" \"File Paths\" \"Bot Settings\" \"e3 Bot Inis\"");
            //    MQ.Cmd("/ini \"e3 Macro Inis\\e3 Data.ini\" \"File Paths\" \"General Settings\" \"e3 Macro Inis\\General Settings.ini\"");
            //    MQ.Cmd("/ini \"e3 Macro Inis\\e3 Data.ini\" \"File Paths\" \"Advanced Settings\" \"e3 Macro Inis\\Advanced Settings.ini\"");
            //}
        }
        private static void InitPlugins()
        {
            using (_log.Trace())
            {

                if (!MQ.Query<bool>("${Plugin[MQ2EQBC]}"))
                {
                    MQ.Write("Plugin MQ2EQBC is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2EQBC");
                    MQ.Delay(1000);

                    Int32 counter = 0;
                    while (!MQ.Query<bool>("${EQBC}"))
                    {
                        if (counter > 10)
                        {
                            MQ.Write("***WARNING*** Could not load MQ2EQBC, macro functionality may be limited.");
                            break;
                        }
                        counter++;
                        MQ.Delay(1000);
                    }
                }
                bool EQBC = MQ.Query<bool>("${EQBC}");
                MQ.Write("Checking for EQBC...result:" + EQBC);
                if (EQBC)
                {
                    bool bcConnect = MQ.Query<bool>("${EQBC.Connected}");
                    MQ.Write("EQBC Found! checking connection...result:" + bcConnect);
                    if (!bcConnect)
                    {
                        MQ.Write("Not connected, trying to connect!");
                        MQ.Cmd("/bccmd connect");

                        if (!MQ.Delay(5000, "${EQBC.Connected}"))
                        {
                            MQ.Write("***WARNING*** Could not connect to EQBCS! Please open EQBCS and try again.  Macro functionality may be limited...");
                        }
                    }
                }
                if (!MQ.Query<bool>("${Plugin[MQ2NetBots]}"))
                {
                    MQ.Write("Plugin MQ2NetBots is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2NetBots");
                    if (!MQ.Delay(3000, "${NetBots}"))
                    {
                        MQ.Write("***WARNING*** Could not load MQ2NetBots! Macro functionality may be limited.");
                    }
                }
                if (MQ.Query<bool>("${NetBots}"))
                {
                    MQ.Cmd("/squelch /netbots on grab=on send=on");
                }
                if (!MQ.Query<bool>($"${{Plugin[MQ2AdvPath].Name.Length}}"))
                {
                    MQ.Write("Plugin MQ2AdvPath is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2AdvPath");
                    if (!MQ.Delay(3000, "${AdvPath}"))
                    {
                        MQ.Write("***WARNING*** Could not load MQ2AdvPath. Please ensure you're using a copy of MQ2 which was compiled with the MQ2AdvPath plugin and try again.");
                    }

                }
                if (!MQ.Query<bool>("${Plugin[MQ2MoveUtils]}"))
                {
                    MQ.Write("Plugin MQ2MoveUtils is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2MoveUtils");
                    if (!MQ.Delay(3000, "${Stick.Status}"))
                    {
                        MQ.Write("***WARNING*** Could not load MQ2MoveUtils! Macro functionality may be limited.");
                    }
                    if (!MQ.Query<bool>("${Stick.Status}"))
                    {
                        if (!MQ.Query<bool>("${AdvPath}"))
                        {
                            MQ.Write("Follow and Assist stick DISABLED.");
                        }
                        else
                        {
                            MQ.Write("Follow and Assist stick DISABLED. Follow restricted to NetBots.");
                        }
                    }
                }
                if (!MQ.Query<bool>("${Plugin[MQ2Exchange]}"))
                {
                    MQ.Write("Plugin MQ2Exchange is not loaded, attempting to resolve...");
                    MQ.Cmd("/plugin MQ2Exchange");
                    if (!MQ.Delay(3000, "${Plugin[MQ2Exchange]}"))
                    {
                        MQ.Write("***WARNING*** Could not load MQ2Exchange! Macro functionality may be limited. Item swapping is DISABLED.");
                    }
                }
                if (!MQ.Query<bool>($"${{Plugin[mq2itemdisplay]}}"))
                {
                    MQ.Cmd("/plugin MQ2ItemDisplay");

                }
                if (!MQ.Query<bool>($"${{Plugin[MQ2LinkDB]}}"))
                {
                    MQ.Cmd("/plugin MQ2LinkDB");
                    MQ.Cmd("/link /scan off");
                }

            }
        }


    }
}

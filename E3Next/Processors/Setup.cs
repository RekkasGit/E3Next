using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public class SubSystemInitAttribute : Attribute
    {
    }
    public static class Setup
    {

        static public Boolean _reloadOnLoot = false;
        static public string _missingSpellItem = string.Empty;
        static public Int32 _numInventorySlots = 10;
        static public Int32 _previousSpellGemThatWasCast = -1;
        public const string _e3Version = "1.0.29-beta";
        public static Boolean _debug = true;
        public const string _macroData_Ini = @"e3 Macro Inis\e3 Data.ini";
        public static string _generalSettings_Ini = @"e3 Macro Inis\General Settings.ini";
        public static string _advancedSettings_Ini = @"e3 Macro Inis\Advanced Settingse.ini";
        public static string _character_Ini = @"e3 Bot Inis\{CharacterName}_{ServerName}.ini";

        public static string _serverNameForIni = "PEQTGC"; //project eq, the grand creation, where legacy e3 was born i believe.
        public static Logging _log = E3.Log;
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
                InitSubSystems();
                return true;
            }

        }
        private static void InitSubSystems()
        {
            var methods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(x => x.GetTypes())
            .Where(x => x.IsClass)
            .SelectMany(x => x.GetMethods())
            .Where(x => x.GetCustomAttributes(typeof(SubSystemInitAttribute), false).FirstOrDefault() != null); // returns only methods that have the InvokeAttribute

            foreach (var foundMethod in methods) // iterate through all found methods
            {
                //these are static don't need to create an instance
                var func = (Action)foundMethod.CreateDelegate(typeof(Action));
                func.Invoke();
            }
        }
        private static void InitPlugins()
        {
            using (_log.Trace())
            {

                if(E3.Bots is Bots)
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

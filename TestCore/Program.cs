using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TestCore
{
    class Program
    {
        //get-childitem *_PEQTGC.ini | rename-item -newname {$_.name -replace '_PEQTGC.ini','_Lazarus.ini' }
        static void Main(string[] args)
        {

            ///1 AFK
            ///1 Aggressive
            ///1 Anonymous
            ///4 Blind
            ///4 BodyID
            ///4 BodyNameLength
            ///N BodyName
            ///1 Buyer
            ///4 Class.ID //uint_32t
            ///4 CleanUpNameLength
            ///N CleanUpName
            /////4 ConColorID //do the string part in C#
            ///4 CurrentEndurance
            ///4 CurrentHPs
            ///4 CurrentMana
            ///1 Dead
            ///4 DisplayNameLength
            ///N DisplayName
            /////4 Distance //Do this on the C# size, method GetDistance()
            ///1 Ducking
            ///1 Feigning 
            /////1 Fleeing //looks expensive, skip
            ///4 GenderID .// do string matching in c# male, female, neuter, uknown. 
            ///1 GM
            ///4 GuildID
            ///4 Heading.Degrees
            ///4 Height //keep as float
            ///4 ID
            ///1 Invs //1,2,3,4 values
            ///1 IsSummoned
            ///4 Level
            /////1 LineOfSight //this is really expensive should be an indiviual call not on EVERYTHING
            ///1 LinkDead
            ///4 Look
            /////4 Mark //looks expensive
            ///4 MasterID
            ///4 MaxEndurance
            ///4 MaxRanage //keep as float?
            ///4 MaxRangeTo //keep as float?
            ///1 Mount //true/false
            ///1 Moving
            ///2 NameLength
            ///N Name
            ///1 Named
            ///8 PctHps
            ///8 PctMana
            ///4 PetID
            ///4 PlayerState
            ///4 RaceID
            ///4 raceNameLength
            ///N raceName
            ///1 Roleplaying
            ///1 Sitting
            ///1 Sneaking
            ///1 Standing
            ///1 Stunned
            ///2 SuffixLength
            ///N Suffix
            ///1 Targetable
            ///4 TargetOfTargetID
            ///1 Trader
            ///2 TypeLength
            ///N Type
            ///1 Underwater
            ///4 X
            ///4 Y
            ///4 Z


            var mq = new MoqMQ();
            MonoCore.Core.mqInstance = mq;
            MonoCore.Logging.MQ = MonoCore.Core.mqInstance;

            //E3Core.Settings.AdvancedSettings asettings = new E3Core.Settings.AdvancedSettings();
            // E3Core.Settings.GeneralSettings gsettings = new E3Core.Settings.GeneralSettings();
            //E3Core.Settings.CharacterSettings settings = new E3Core.Settings.CharacterSettings();

            MonoCore.Core.OnInit();
           
            while (true)
            {
                MonoCore.Core.OnIncomingChat("Rekken tells the group, 'nowCast Dawnstrike targetid=88'");

                MonoCore.Core.OnPulse();
                System.Threading.Thread.Sleep(100);
            }


            //string result = settings.ToString();
            //Console.WriteLine(result);



        }


        public class MoqMQ : IMQ
        {
            public bool AddCommand(string query)
            {
                Console.WriteLine("AddCommand:" + query);
                return true;
            }

            public void Broadcast(string query)
            {
                Console.WriteLine("Broadcast:" + query);
            }

            public void ClearCommands()
            {
                throw new NotImplementedException();
            }

            public void Cmd(string query, bool delayed = false)
            {
                Console.WriteLine("CMD:"+query);
               //do work
            }
            public void Cmd(string query,Int32 delay, bool delayed = false)
            {
                Console.WriteLine("CMD:" + query);
                //do work
            }
            public void Delay(int value)
            {
              

                //lets tell core that it can continue
                Core.CoreResetEvent.Set();
                //we are now going to wait on the core
                MainProcessor.ProcessResetEvent.Wait();
                MainProcessor.ProcessResetEvent.Reset();
               
            }

            public bool Delay(int maxTimeToWait, string Condition)
            {
                return true;
            }

            public bool Delay(int maxTimeToWait, Func<bool> methodToCheck)
            {
                return true;
            }

            public T Query<T>(string query)
            {

                if (typeof(T) == typeof(Int32))
                {
                   

                }
                else if (typeof(T) == typeof(Boolean))
                {
                   


                }
                else if (typeof(T) == typeof(string))
                {
                    if(query=="${MacroQuest.Path[macros]}") return (T)(object)@"G:\EQ\e3Test\Macros";
                    if(query=="${MacroQuest.Path[config]}") return (T)(object)@"G:\EQ\e3Test\config";
                    if(query=="${MacroQuest.Server}") return (T)(object)@"Project Lazarus"; ;
                    if(query=="${Me.Class}")return (T)(object)"Druid";
                    if(query=="${Me.CleanName}") return (T)(object)"Shadowvine";
                    if(query=="${EverQuest.GameState}") return (T)(object)"INGAME";

                    return (T)(object)String.Empty;
                }
                else if (typeof(T) == typeof(decimal))
                {
                  
                }
                else if (typeof(T) == typeof(Int64))
                {
                   
                }


                return default(T);

            }

            public void RemoveCommand(string commandName)
            {
                Console.Write("RemoveCommand:" + commandName);
            }

            public void TraceEnd(string methodName)
            {
                Console.WriteLine("traceend:" + methodName);
            }

            public void TraceStart(string methodName)
            {
                Console.WriteLine("tracestart:"+methodName);
            }

            public void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
            {
                Console.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] {query}");
            }

            public bool FeatureEnabled(MQFeature feature)
            {
                return true;
            }

			public string GetFocusedWindowName()
			{
                return "NULL";
			}

			public void WriteDelay(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
			{
				Console.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] {query}");
			}
		}

    }
}

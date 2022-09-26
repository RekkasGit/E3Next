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



            MonoCore.Core.mqInstance = new MoqMQ();
            MonoCore.Logging.MQ = MonoCore.Core.mqInstance;

            //E3Core.Settings.AdvancedSettings asettings = new E3Core.Settings.AdvancedSettings();
            // E3Core.Settings.GeneralSettings gsettings = new E3Core.Settings.GeneralSettings();
            //E3Core.Settings.CharacterSettings settings = new E3Core.Settings.CharacterSettings();

            MonoCore.Core.OnInit();

            while(true)
            {

                MonoCore.Core.OnIncomingChat("Pyra tells the group, 'SWARM-Host of the Elements'");

                E3Core.Processors.E3.Process();
                MonoCore.EventProcessor.ProcessEventsInQueues();
                System.Threading.Thread.Sleep(1000);
            }


            //string result = settings.ToString();
            //Console.WriteLine(result);



        }


        public class MoqMQ : MonoCore.IMQ
        {
            public void Broadcast(string query)
            {
                Console.WriteLine("Broadcast:" + query);
            }

            public void Cmd(string query)
            {
                Console.WriteLine("CMD:"+query);
               //do work
            }

            public void Delay(int value)
            {
               
            }

            public bool Delay(int maxTimeToWait, string Condition)
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

            public void TraceEnd(string methodName)
            {
                Console.WriteLine("traceend:" + methodName);
            }

            public void TraceStart(string methodName)
            {
                Console.WriteLine("tracestart:"+methodName);
            }

            public void Write(string query, string colorcode = "", [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0)
            {
                Console.WriteLine($"[{System.DateTime.Now.ToString("HH:mm:ss")}] {colorcode}{query}");
            }
        }

    }
}

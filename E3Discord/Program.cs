using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace E3Discord
{
    public class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler();
        private static EventHandler _handler;
        private static Random _random = new Random();

		/// <summary>
		/// Note you will have to setup Privileged gateway intents so that getting members of a discord channel work. 
		/// basically just allow
		/// 
		/// Presence intent
		/// server member intent
		/// message content intent
		/// </summary>


        static void Main()
        {
            var args = Environment.GetCommandLineArgs();
            DiscordMessager.Init(args);

            var timer = new System.Timers.Timer(60 * 60 * 1000); //one hour in milliseconds
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Start();

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
			//FactOrJoke();
			DiscordMessager.SendMessageToDiscord("Discord bot now active.");
			while (true)
            {
                var task = Task.Run(() => DiscordMessager.PollDiscord());
                if (task.Wait(TimeSpan.FromSeconds(60))) // if it didn't return in 60 seconds, the bot's everquest client likely went disconnected. kill this process
                { 
                    task.GetAwaiter().GetResult(); 
                }
                else
                {
                    DiscordMessager.SendMessageToDiscord("Disconnected because Discordbot dc'ed :sob:");
                    Environment.Exit(0);
                }

                Thread.Sleep(1000);
            }
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //FactOrJoke();
        }

        private static void FactOrJoke()
        {
            if (_random.Next(2) == 1)
            {
                DiscordMessager.TellAJoke();
            }
            else
            {
                DiscordMessager.GetAFact();
            }
        }

        private static bool Handler()
        {
            DiscordMessager.SendMessageToDiscord("Disconnected because app died or was closed :sob:");
            Environment.Exit(0);

            return true;
        }
    }
}

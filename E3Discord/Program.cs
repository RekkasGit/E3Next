using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace E3Discord
{
    public class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler();
        static EventHandler _handler;

        static void Main()
        {
            var args = Environment.GetCommandLineArgs();
            DiscordMessager.Init(args);

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            while (true)
            {
                var task = Task.Run(() => DiscordMessager.PollDiscord());
                if (task.Wait(TimeSpan.FromSeconds(60))) // if it didn't return in 60 seconds, the bot likely went offline. kill this process
                { 
                    task.GetAwaiter().GetResult(); 
                }
                else
                {
                    DiscordMessager.SendMessageToDiscord("Disconnected because Discordbot dc'ed :sob:");
                    Environment.Exit(0);
                }

                //DiscordMessager.PollDiscord();
                Thread.Sleep(1000);
            }
        }

        private static bool Handler()
        {
            DiscordMessager.SendMessageToDiscord("Disconnected because Chadbot app died or was closed :sob:");
            Environment.Exit(0);

            return true;
        }
    }
}

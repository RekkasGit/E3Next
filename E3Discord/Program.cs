using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

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
                try
                {
                    Process.GetProcessById(int.Parse(args[7]));
                }
                // kill the chatbot process if the parent process is gone
                catch (ArgumentException)
                {
                    DiscordMessager.SendMessageToDiscord("Chatbot Disconnected :sob:");
                    DiscordMessager.SendMessageToGame("Chatbot Disconnected");
                    Environment.Exit(0);
                }

                DiscordMessager.PollDiscord();
                Thread.Sleep(1000);
            }
        }

        private static bool Handler()
        {
            DiscordMessager.SendMessageToDiscord("Chatbot Disconnected :sob:");
            DiscordMessager.SendMessageToGame("Chatbot Disconnected");
            Environment.Exit(0);

            return true;
        }
    }
}

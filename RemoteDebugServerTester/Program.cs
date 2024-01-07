using MonoCore;

namespace RemoteDebugServerTester
{
    internal class Program
    {
        static void Main(string[] args)
        {



            Core.OnInit();

            while (true)
            {
                Core.OnPulse();
                System.Threading.Thread.Sleep(50);
            }


        }
    }
}

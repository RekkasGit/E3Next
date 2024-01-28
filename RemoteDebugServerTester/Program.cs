using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteDebugServerTester
{
	internal class Program
	{
		static void Main(string[] args)
		{



			Core.OnInit();
			
			while(true)
			{
				Core.OnPulse();
				System.Threading.Thread.Sleep(50);
			}


		}
	}
}

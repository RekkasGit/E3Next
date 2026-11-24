using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Template.Utility
{
	internal class MyCodeUtil
	{
		public static bool ShouldCheck(ref Int64 nextCheck, Int64 nextCheckInterval)
		{
			if (Core.StopWatch.ElapsedMilliseconds < nextCheck)
			{
				return false;
			}
			else
			{
				nextCheck = Core.StopWatch.ElapsedMilliseconds + nextCheckInterval;
				return true;
			}
		}
	}
}

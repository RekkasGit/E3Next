using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
	public class CharacterBuffs : IDisposable
	{

		public Dictionary<Int32, Int64> BuffDurations = new Dictionary<int, Int64>();
		public Int64 LastUpdate= 0;
		private CharacterBuffs() { }

		public static CharacterBuffs Aquire()
		{
			CharacterBuffs obj;
			if (!StaticObjectPool.TryPop<CharacterBuffs>(out obj))
			{
				obj = new CharacterBuffs();
			}

			return obj;
		}
		public void Dispose()
		{
			BuffDurations.Clear();
			LastUpdate = 0;
			StaticObjectPool.Push(this);
		}
		~CharacterBuffs()
		{
			//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
			//if this is called, it will cause the domain to hang in the GC when shuttind down
			//This is only here to warn you

		}
	
    }
}

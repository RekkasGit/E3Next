using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
	public class CorpsesLooted : IDisposable
	{

		public List<Int32> CorpseIDs = new List<int>();
		public Int64 LastUpdate = 0;
		private CorpsesLooted() { }

		public static CorpsesLooted Aquire()
		{
			CorpsesLooted obj;
			if (!StaticObjectPool.TryPop<CorpsesLooted>(out obj))
			{
				obj = new CorpsesLooted();
			}

			return obj;
		}
		public void Dispose()
		{
			CorpseIDs.Clear();
			LastUpdate = 0;
			StaticObjectPool.Push(this);
		}
		~CorpsesLooted()
		{
			//DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
			//if this is called, it will cause the domain to hang in the GC when shuttind down
			//This is only here to warn you

		}

	}
}

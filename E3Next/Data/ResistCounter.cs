using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public class ResistCounter : IDisposable
    {

        public Int32 _mobID;
        public Dictionary<Int32, Int32> SpellCounters = new Dictionary<int, int>();

        public static ResistCounter Aquire()
        {
            ResistCounter obj;
            if (!StaticObjectPool.TryPop<ResistCounter>(out obj))
            {
                obj = new ResistCounter();
            }

            return obj;
        }
        public void Dispose()
        {
            _mobID = 0;
            SpellCounters.Clear();

            StaticObjectPool.Push(this);
        }
        ~ResistCounter()
        {
            //DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
            //if this is called, it will cause the domain to hang in the GC when shuttind down
            //This is only here to warn you

        }
    }
}

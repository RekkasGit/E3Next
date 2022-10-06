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
        public Dictionary<Int32, Int32> _spellCounters = new Dictionary<int, int>();

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
            _spellCounters.Clear();

            StaticObjectPool.Push(this);
        }
    }
}

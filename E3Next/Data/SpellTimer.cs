using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public class SpellTimer
    {
        public Int32 _mobID;
        public Dictionary<Int32, Int64> _timestamps = new Dictionary<Int32, Int64>();

        public static SpellTimer Aquire()
        {
            SpellTimer obj;
            if (!StaticObjectPool.TryPop<SpellTimer>(out obj))
            {
                obj = new SpellTimer();
            }

            return obj;
        }
        public void Dispose()
        {
            _mobID = 0;
            _timestamps.Clear();

            StaticObjectPool.Push(this);
        }
    }
}

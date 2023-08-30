using MonoCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public class SpellTimer
    {
        public Int32 MobID;
        public Dictionary<Int32, Int64> Timestamps = new Dictionary<Int32, Int64>();
		public Dictionary<Int32, Int64> TimestampBySpellDuration = new Dictionary<Int32, Int64>();
		public Dictionary<Int32, Int64> Lockedtimestamps = new Dictionary<Int32, Int64>();

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
            MobID = 0;
            Timestamps.Clear();
            TimestampBySpellDuration.Clear();

			Lockedtimestamps.Clear();
            StaticObjectPool.Push(this);
        }
        ~SpellTimer()
        {
            //DO NOT CALL DISPOSE FROM THE FINALIZER! This should only ever be used in using statements
            //if this is called, it will cause the domain to hang in the GC when shuttind down
            //This is only here to warn you

        }

    }
}

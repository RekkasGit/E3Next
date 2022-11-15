using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public abstract class BaseProcessor
    {
        protected static Logging _log = E3.Log;
        protected static IMQ MQ = E3.Mq;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [SubSystemInit]
        protected virtual void Init()
        {
            RegisterEvents();
        }

        protected virtual void RegisterEvents()
        {

        }
    }
}

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
        protected static Logging _log = Core._log;
        protected static IMQ MQ = Core.mqInstance;
    }
}

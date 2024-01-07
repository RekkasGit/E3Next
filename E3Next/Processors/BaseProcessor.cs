using MonoCore;

namespace E3Core.Processors
{
    public abstract class BaseProcessor
    {
        protected static Logging _log = E3.Log;
        protected static IMQ MQ = E3.MQ;
    }
}

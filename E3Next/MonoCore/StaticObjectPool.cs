using System.Collections.Generic;

/// <summary>
/// Version 0.1
/// This file, is the 'core', or the mediator between you and MQ2Mono
/// MQ2Mono is just a simple C++ plugin to MQ, which exposes the
/// OnInit
/// OnPulse
/// OnIncomingChat
/// etc
/// Methods from MQ event framework, and meshes it in such a way to allow you to write rather straight forward C# code.
///
/// Included in this is a Logging/trace framework, Event Proecssor, MQ command object, etc
///
/// Your class is included in here with a simple .Process Method. This methoid will be called once every OnPulse from the plugin, or basically every frame of the EQ client.
/// All you code should *NOT* be in this file.
/// </summary>
namespace E3Core.MonoCore
{
    /// <summary>
    /// Used for object pooling objects to be reused
    /// </summary>
    public static class StaticObjectPool
    {
        private static class Pool<T>
        {
            private static readonly Stack<T> pool = new Stack<T>();

            public static void InnerPush(T obj)
            {
                lock (pool)
                {
                    pool.Push(obj);
                }
            }

            public static bool InnerTryPop(out T obj)
            {
                lock (pool)
                {
                    if (pool.Count > 0)
                    {
                        obj = pool.Pop();
                        return true;
                    }
                }
                obj = default;
                return false;
            }
        }

        public static void Push<T>(T obj)
        {
            Pool<T>.InnerPush(obj);
        }

        public static bool TryPop<T>(out T obj)
        {
            return Pool<T>.InnerTryPop(out obj);
        }

        public static T PopOrDefault<T>()
        {
            TryPop(out T ret);
            return ret;
        }

        public static T PopOrNew<T>()
            where T : new()
        {
            return TryPop(out T ret) ? ret : new T();
        }
    }
}
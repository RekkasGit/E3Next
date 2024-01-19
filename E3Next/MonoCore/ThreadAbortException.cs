using System;

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
    //used to abort the main C# thread so that it can finish up and exist
    //try statemnts that catch expections need to exclude this error. 
    public class ThreadAbortException : Exception
    {
        public ThreadAbortException()
        {
        }

        public ThreadAbortException(string message)
            : base(message)
        {
        }

        public ThreadAbortException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
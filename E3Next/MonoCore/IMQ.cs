using System;
using System.Runtime.CompilerServices;

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
    public interface IMQ
    {
        T Query<T>(string query);
        void Cmd(string query, bool delayed = false);
        void Cmd(string query, int delay, bool delayed = false);
        void Write(string query, [CallerMemberName] string memberName = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int lineNumber = 0);
        void TraceStart(string methodName);
        void TraceEnd(string methodName);
        void Delay(int value);
        bool Delay(int maxTimeToWait, string Condition);
        bool Delay(int maxTimeToWait, Func<Boolean> methodToCheck);
        //void Broadcast(string query);
        bool AddCommand(string query);
        void ClearCommands();
        void RemoveCommand(string commandName);

        bool FeatureEnabled(MQFeature feature);
        string GetFocusedWindowName();
    }
}
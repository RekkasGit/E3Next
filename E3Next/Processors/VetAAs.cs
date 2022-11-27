using E3Core.Data;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using IniParser;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class VetAAs
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;

        [SubSystemInit]
        public static void Init()
        {
            VeteranAAs();
        }
        /// <summary>
        /// The veteran aa commands.
        /// /armor - uses Armor of Experience
        /// /intensity - uses Intensity of the Resolute
        /// /infusion - uses Infusion of the Faithful
        /// /staunch - uses Staunch Recovery
        /// /servant - uses Steadfast Servant
        /// /expedient - uses Expedient Recovery
        /// /lesson - uses Lesson of the Devoted
        /// /throne - uses Throne of Heroes
        /// /jester - uses Chaotic Jester
        /// </summary>
        public static void VeteranAAs()
        {
            EventProcessor.RegisterCommand("/armor", (x) =>
            {
                VetAA("Armor of Experience", "/armor", x.args.Count);
            });
            EventProcessor.RegisterCommand("/intensity", (x) =>
            {
                VetAA("Intensity of the Resolute", "/intensity", x.args.Count);
            });
            EventProcessor.RegisterCommand("/infusion", (x) =>
            {
                VetAA("Infusion of the Faithful", "/infusion", x.args.Count);
            });
            EventProcessor.RegisterCommand("/staunch", (x) =>
            {
                VetAA("Staunch Recovery", "/staunch", x.args.Count);
            });
            EventProcessor.RegisterCommand("/servant", (x) =>
            {
                VetAA("Steadfast Servant", "/servant", x.args.Count);
            });
            EventProcessor.RegisterCommand("/expedient", (x) =>
            {
                VetAA("Expedient Recovery", "/expedient", x.args.Count);
            });
            EventProcessor.RegisterCommand("/lesson", (x) =>
            {
                VetAA("Lesson of the Devoted", "/lesson", x.args.Count);
            });
            EventProcessor.RegisterCommand("/throne", (x) =>
            {
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommandToGroup("/throne me");
                }
                MQ.Cmd("/interrupt");
                MQ.Delay(500);
                MQ.Cmd("/alt act 511");
                MQ.Delay(500);
                if (E3.CurrentClass == Class.Bard)
                {
                    MQ.Delay(17000);

                }
                else
                {
                    MQ.Delay(20000, Casting.IsNotCasting);
                }
            });
            EventProcessor.RegisterCommand("/jester", (x) =>
            {
                VetAA("Chaotic Jester", "/jester", x.args.Count);
            });
        }

        private static void VetAA(string vetAASpell, string command, Int32 argCount)
        {
            Spell s;
            if (!Spell._loadedSpellsByName.TryGetValue(vetAASpell, out s))
            {
                s = new Spell(vetAASpell);
            }
            if (argCount == 0)
            {
                if (Casting.CheckReady(s))
                {
                    Casting.Cast(0, s);
                }
                E3.Bots.BroadcastCommandToGroup($"{command} all");
            }
            else
            {

                if (Casting.CheckReady(s))
                {
                    //this is to deal with vet aa with bards not showing a cast window.
                    //force a stop of any song, do the cast, wait for it to end, then continue back on your way.
                    if (E3.CurrentClass == Class.Bard)
                    {
                        MQ.Cmd("/stopsong");
                        MQ.Delay(0);
                    }
                    Casting.Cast(0, s);


                }
            }
        }
    }
}

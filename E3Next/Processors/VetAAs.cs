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
        public static void VetAAs_Init()
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
                VetAA("Armor of Experience", "/armor", x);
            });
            EventProcessor.RegisterCommand("/intensity", (x) =>
            {
                VetAA("Intensity of the Resolute", "/intensity", x);
            });
            EventProcessor.RegisterCommand("/infusion", (x) =>
            {
                VetAA("Infusion of the Faithful", "/infusion", x);
            });
            EventProcessor.RegisterCommand("/staunch", (x) =>
            {
                VetAA("Staunch Recovery", "/staunch", x);
            });
            EventProcessor.RegisterCommand("/servant", (x) =>
            {
                VetAA("Steadfast Servant", "/servant", x);
            });
            EventProcessor.RegisterCommand("/expedient", (x) =>
            {
                VetAA("Expedient Recovery", "/expedient", x);
            });
            EventProcessor.RegisterCommand("/lesson", (x) =>
            {
                VetAA("Lesson of the Devoted", "/lesson", x);
            });
            EventProcessor.RegisterCommand("/throne", (x) =>
            {
                if (x.args.Count == 0)
                {
                    E3.Bots.BroadcastCommandToGroup("/throne me",x);
                }
                Casting.Interrupt();
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
			EventProcessor.RegisterCommand("/origin", (x) =>
			{
				if (x.args.Count == 0)
				{
					E3.Bots.BroadcastCommandToGroup("/origin me", x);
				}
				Casting.Interrupt();
				MQ.Delay(500);
				MQ.Cmd("/alt act 331");
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
                VetAA("Chaotic Jester", "/jester", x);
            });
        }

        private static void VetAA(string vetAASpell, string command,EventProcessor.CommandMatch x)
        {
            Spell s;
            if (!Spell.LoadedSpellsByName.TryGetValue(vetAASpell, out s))
            {
                s = new Spell(vetAASpell);
            }
            if (x.args.Count == 0)
            {
                if (Casting.CheckReady(s))
                {
                    Casting.Cast(0, s);
                }
                E3.Bots.BroadcastCommandToGroup($"{command} all",x);
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
                       
                    }
                    Casting.Cast(0, s);


                }
            }
        }
    }
}

using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Assist
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        private static bool use_FULLBurns;
        private static bool use_QUICKBurns;
        private static bool use_EPICBurns;
        private static bool use_LONGBurns;
        private static bool use_Swarms;
        private static Dictionary<Int32, Int32> _resistCounters = new Dictionary<int, int>();

        public static Boolean _isAssisting = false;
        public static Int32 _assistTargetID = 0;
        public static Int32 _assistStickDistance = 10;
        public static string _epicWeaponName=String.Empty;
        public static string _anguishBPName=String.Empty;


        public static void Init()
        {
            RegisterEvents();
            RegisterEpicAndAnguishBP();
            E3._bots.SetupAliases();
        }

        public static void AssistOff()
        {


            if (MQ.Query<bool>("${Window[CastingWindow].Open}")) MQ.Cmd("/interrupt");
            if (MQ.Query<bool>("${Me.Combat}")) MQ.Cmd("/attack off");
            if (MQ.Query<bool>("${Me.AutoFire}")) MQ.Cmd("/autofire off");
            if (MQ.Query<Int32>("${Me.Pet.ID}") >0) MQ.Cmd("/squelch /pet back off");
            MQ.Delay(500, "${Bool[!${Me.Combat} && !${Me.AutoFire}]}");
            _isAssisting = false;
            _assistTargetID = 0;
            if (MQ.Query<bool>("${Stick.Status.Equal[ON]}")) MQ.Cmd("/squelch /stick off");

            use_FULLBurns=false;
            use_QUICKBurns=false;
            use_EPICBurns=false;
            use_LONGBurns=false;
            use_Swarms=false;
            _resistCounters.Clear();
            //issue follow
            if(Basics._following)
            {
                Basics.AcquireFollow();
            }
        }
        public static void AssistOn(Int32 mobID)
        {
            if (mobID == 0)
            {
                //something wrong with the assist, kickout
                MQ.Broadcast("Cannot assist, improper mobid");
                return;
            }
            Spawn s;
            if(_spawns.TryByID(mobID,out s))
            {
               
                if(s.TypeDesc == "Corpse")
                {
                    MQ.Broadcast("Cannot assist, a corpse");
                    return;
                }
                if(!(s.TypeDesc=="NPC"|| s.TypeDesc=="Pet"))
                {
                    MQ.Broadcast("Cannot assist, not a NPC or Pet");
                    return;
                }
               
                if (s.Distance3D > E3._generalSettings.Assists_MaxEngagedDistance)
                {
                     MQ.Broadcast($"{s.CleanName} is too far away.");
                    return;
                }

                if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    MQ.Cmd("/stand");
                }


                Spawn folTarget;

                if(_spawns.TryByID(Basics._followTargetID,out folTarget))
                {
                    if (Basics._following && folTarget.Distance3D > 100 && MQ.Query<bool>("${Me.Moving}"))
                    {
                        //using a delay in awhile loop, use query for realtime info
                        while (MQ.Query<bool>("${Me.Moving}") && MQ.Query<Decimal>($"${{Spawn[id {Basics._followTargetID}].Distance3D}}") > 100)
                        {
                            MQ.Delay(100);
                            //wait us to get close to our follow target and then we can engage
                        }
                    }
                }

                if (MQ.Query<bool>("${Stick.Active}")) MQ.Cmd("/squelch /stick off");
                if (MQ.Query<bool>("${AdvPath.Following}")) MQ.Cmd("/squelch /afollow off ");

                _isAssisting = true;
                _assistTargetID = s.ID;

                if(MQ.Query<Int32>("${Target.ID}")!=_assistTargetID)
                {
                    Casting.TrueTarget(_assistTargetID);
                }
                MQ.Cmd($"/face fast id {_assistTargetID}");
                
                if(MQ.Query<Int32>("${Me.Pet.ID}")>0)
                {
                    MQ.Cmd($"/pet attack {_assistTargetID}");
                }




            }
           
        }


        private static void RegisterEpicAndAnguishBP()
        {
            foreach (string name in _epicList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    _epicWeaponName = name;
                }
            }

            foreach (string name in _anguishBPList)
            {
                if (MQ.Query<Int32>($"${{FindItemCount[={name}]}}") > 0)
                {
                    _anguishBPName = name;
                }
            }

        }
        private static void RegisterEvents()
        {

            EventProcessor.RegisterCommand("/assistme", (x) =>
            {
                Int32 mobid;
                if (x.args.Count > 0)
                {
                    if(Int32.TryParse(x.args[1],out mobid))
                    {
                        AssistOn(mobid);
                    }
                }
                else
                {
                    Int32 targetID = MQ.Query<Int32>("${Target.ID}");
                    if (targetID > 0)
                    {
                        //we are telling people to follow us
                        E3._bots.BroadcastCommandToOthers($"/assistme {targetID}");
                    }
                    else
                    {
                        MQ.Write("\aNEED A TARGET TO ASSIST");
                    }
                }
            });

        }

        private static List<string> _anguishBPList = new List<string>() { 
            "Bladewhisper Chain Vest of Journeys",
            "Farseeker's Plate Chestguard of Harmony",
            "Wrathbringer's Chain Chestguard of the Vindicator",
            "Savagesoul Jerkin of the Wilds",
            "Glyphwielder's Tunic of the Summoner",
            "Whispering Tunic of Shadows",
            "Ritualchanter's Tunic of the Ancestors"};

        private static List<string> _epicList = new List<string>() { 
            "Prismatic Dragon Blade",
            "Blade of Vesagran",
            "Raging Taelosian Alloy Axe",
            "Vengeful Taelosian Blood Axe",
            "Staff of Living Brambles",
            "Staff of Everliving Brambles",
            "Fistwraps of Celestial Discipline",
            "Transcended Fistwraps of Immortality",
            "Redemption",
            "Nightbane, Sword of the Valiant",
            "Heartwood Blade",
            "Heartwood Infused Bow",
            "Aurora, the Heartwood Blade",
            "Aurora, the Heartwood Bow",
            "Fatestealer",
            "Nightshade, Blade of Entropy",
            "Innoruuk's Voice",
            "Innoruuk's Dark Blessing",
            "Crafted Talisman of Fates",
            "Blessed Spiritstaff of the Heyokah",
            "Staff of Prismatic Power",
            "Staff of Phenomenal Power",
            "Soulwhisper",
            "Deathwhisper"};
    }
}

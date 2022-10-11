using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class WaitForRez
    {

        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;
        public static bool _waitingOnRez = true;
        public static void Init()
        {
            RegisterEvents();
        }


        public static void Process()
        {


            if (_waitingOnRez)
            {
                //check for dialog box
                if (MQ.Query<bool>("${Window[ConfirmationDialogBox].Open}"))
                {
                    //check if its a valid confirmation box
                    string message = MQ.Query<string>("${Window[ConfirmationDialogBox].Child[cd_textoutput].Text}");
                    if (!message.Contains("percent)"))
                    {
                        MQ.Cmd("/nomodkey /notify ConfirmationDialogBox No_Button leftmouseup");
                        return; //not a rez dialog box, do not accept.
                    }
                    MQ.Cmd("/nomodkey /notify ConfirmationDialogBox Yes_Button leftmouseup");
                    MQ.Delay(2000);//start zone
                    //zone may to happen
                    MQ.Delay(30000, "${Spawn[${Me}'s].ID}");

                    if (!MQ.Query<bool>("${Spawn[${Me}'s].ID}"))
                    {
                        //something went rong kick out.
                        return;
                    }

                    //okay, we are rezed, and now need to loot our corpse. 
                    //it should be the closest so we will use spawn as it will grab the closest first.
                    Int32 corpseID = MQ.Query<Int32>("${Spawn[${Me}'s].ID}");
                    Casting.TrueTarget(corpseID);

                    //check if its rezable.
                    if (!CanRez())
                    {
                        tryLootAgain:
                        MQ.Cmd("/corpse");
                        MQ.Delay(1000);
                        MQ.Cmd("/loot");
                        MQ.Delay(1000);
                        MQ.Cmd("/notify LootWnd LootAllButton leftmouseup");
                        MQ.Delay(20000, "!${Window[LootWnd].Open}");

                        if (MQ.Query<bool>("${Window[LootWnd].Open}"))
                        {
                            _waitingOnRez = false;
                            MQ.Cmd("/beep");
                            E3._bots.Broadcast("\agWaitForRez:\arERROR! \atLoot Window stuck open, please help.");
                            MQ.Delay(1000);
                            return;

                        }
                        //maybe there is another corpse? lets find out.
                        //get all spawns within a 100 distance
                        //make sure we have the most up to date zones spawns, takes about 1-2ms so no real harm forcing the issue.
                        _spawns.RefreshList();
                        string corpseName = E3._currentName + "'s corpse";
                        foreach (var spawn in _spawns.Get())
                        {
                            if (spawn.CleanName.StartsWith(corpseName))
                            {
                                if (spawn.Distance < 100)
                                {
                                    Casting.TrueTarget(spawn.ID);
                                    MQ.Delay(500);
                                    if (!CanRez())
                                    {
                                        goto tryLootAgain;
                                    }
                                }
                            }
                        }
                        _waitingOnRez = false;
                        E3._bots.Broadcast("\atReady to die again!");
                    }

                }
            }
        }

        private static bool CanRez()
        {
            MQ.Cmd("/consider");
            MQ.Delay(2000);
            //check for the event.
            if(HasEventItem("CanRez"))
            {
                //its rezable
                return true;
            }
            else if(HasEventItem("CanNotRez"))
            {
                return false;
            }
            else
            {
                //dunno, error?
                E3._bots.Broadcast("\agWaitForRez:\arERROR! \atUnsure if we can loot, assuming no.");
                return true;
            }
        }
        private static bool HasEventItem(string name)
        {
            if (EventProcessor._eventList[name].queuedEvents.Count > 0)
            {
                while (EventProcessor._eventList[name].queuedEvents.Count > 0)
                {
                    EventProcessor.EventMatch e;
                    EventProcessor._eventList[name].queuedEvents.TryDequeue(out e);
                }

                return true;
            }
            return false;
        }
        private static void RegisterEvents()
        {


            EventProcessor.RegisterEvent("YourDead", "You died.", (x) =>
            {
                Assist.AssistOff();
                _waitingOnRez = true;
            });

            EventProcessor.RegisterEvent("CanRez", "This corpse can be resurrected.", (x) =>
            {
              
            });
            EventProcessor.RegisterEvent("CanNotRez", "This corpse cannot be resurrected.", (x) =>
            {

            });
            EventProcessor.RegisterEvent("WaitForRez", "(.+) tells you, 'Wait4Rez'", (x) =>
            {
                _waitingOnRez = true;
            });
            EventProcessor.RegisterCommand("/WaitRez", (x) =>
            {
                if(x.args.Count>0)
                {
                    //is it us?
                    if(x.args[0].Equals(E3._currentName, StringComparison.OrdinalIgnoreCase))
                    {
                        //its us! lets wait
                        _waitingOnRez = true;
                    }
                }

            });
            EventProcessor.RegisterCommand("/TellRez", (x) =>
            {
                if (x.args.Count > 0)
                {
                    E3._bots.BroadcastCommandToOthers($"/WaitRez {x.args[0]}");
                }
            });
        }

    }
}

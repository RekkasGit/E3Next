using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Alerts
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;


        private static void RegisterEvents()
        {

            #region AnguishMask
            //Anguish mask swap
            string pattern = ".+You feel a gaze of deadly power focusing on you.+";
            EventProcessor.RegisterEvent("AnguishMask", pattern, (x) => {


                if (!MQ.Query<bool>("${Bool[${FindItem[=Mirrored Mask]}]}"))
                {
                    MQ.Broadcast("I don't have a mirrored mask, I dun messed up.");
                    MQ.Cmd("/beep");
                    return;
                }
                else
                {
                    Int32 itemSlot = MQ.Query<Int32>("${FindItem[=Mirrored Mask].ItemSlot}");
                    if (itemSlot >= 23)
                    {
                        //TODO: SWAP Inventory Items
                    }
                }


                if (MQ.Query<bool>("${Me.Inventory[face].Name.Equal[Mirrored Mask]}"))
                {
                    Data.Spell mirroredMask = new Data.Spell("Mirrored Mask");
                    Casting.Cast(0, mirroredMask);
                    MQ.Delay(1000);
                    if (!MQ.Query<bool>("${Bool[${Me.Song[Reflective Skin]}]"))
                    {
                        //try again.
                        Casting.Cast(0, mirroredMask);
                    }
                }

            });
            #endregion
            #region PoTaticsStampeed
            pattern = ".+You hear the pounding of hooves.+";
            EventProcessor.RegisterEvent("PoT_STAMPEDE", pattern, (x) => {

                if (MQ.Query<bool>("${Zone.ShortName.Equal[potactics]}"))
                {
                    MQ.Cmd("/gsay STAMPEDE");

                }
            });
            #endregion

            #region CharacterFlag
            pattern = "You receive a character flag.+";
            EventProcessor.RegisterEvent("CharacterFlag", pattern, (x) => {
                MQ.Broadcast("I have recieved a characer flag!");
            });

            #endregion

        }
    }
}

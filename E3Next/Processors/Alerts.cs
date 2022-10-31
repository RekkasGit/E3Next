using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    /// <summary>
    /// Scans incoming chat messages for trigger phrases to act on
    /// </summary>
    public static class Alerts
    {
        private static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
        }
        private static void RegisterEvents()
        {

            #region AnguishMask
            //Anguish mask swap
            string pattern = @"You feel a gaze of deadly power focusing on you\.";
            EventProcessor.RegisterEvent("AnguishMask", pattern, (x) => {

                string currentFace = MQ.Query<string>("${Me.Inventory[face].Name}");
               
                if (!MQ.Query<bool>("${Bool[${FindItem[=Mirrored Mask]}]}"))
                {
                    E3._bots.Broadcast("I don't have a mirrored mask, I dun messed up.");
                    MQ.Cmd("/beep");
                    return;
                }
                else
                {
                   
                    if(currentFace!= "Mirrored Mask")
                    {
                        MQ.Cmd("/exchange \"mirrored mask\" face");
                        MQ.Delay(100);
                    }
                }



                if (MQ.Query<bool>("${Me.Inventory[face].Name.Equal[Mirrored Mask]}"))
                {
                    Data.Spell mirroredMask = new Data.Spell("Mirrored Mask");
                    Casting.Cast(0, mirroredMask);
                    MQ.Delay(1000);
                    if (!MQ.Query<bool>("${Bool[${Me.Song[Reflective Skin]}]}"))
                    {
                        //try again.
                        Casting.Cast(0, mirroredMask);
                        MQ.Delay(1000);
                    }
                    //put your old mask back on
                    MQ.Cmd($"/exchange \"{currentFace}\" face");
                    MQ.Delay(100);
                }

            });
            #endregion

            #region PoTaticsStampeed
            pattern = "You hear the pounding of hooves.";
            EventProcessor.RegisterEvent("PoT_STAMPEDE", pattern, (x) => {

                if (MQ.Query<bool>("${Zone.ShortName.Equal[potactics]}"))
                {
                    MQ.Cmd("/gsay STAMPEDE");

                }
            });
            #endregion

            #region CharacterFlag
            pattern = "You receive a character flag.";
            EventProcessor.RegisterEvent("CharacterFlag", pattern, (x) => {
                E3._bots.Broadcast("I have recieved a characer flag!");
            });

            #endregion

        }
    }
}

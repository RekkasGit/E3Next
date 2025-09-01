using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E3Core.Utility;
using E3Core.Data;
using static System.Collections.Specialized.BitVector32;

namespace E3Core.Processors
{
    /// <summary>
    /// Scans incoming chat messages for trigger phrases to act on
    /// </summary>
    public static class Alerts
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
	
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        [SubSystemInit]
        public static void Alerts_Init()
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
                    E3.Bots.Broadcast("I don't have a mirrored mask, I dun messed up.");
                    E3.Bots.BroadcastCommand("/popup ${Me} doesn't have a mirrored mask.");
                    e3util.Beep();
                    return;
                }
                else
                {
                    if(currentFace!= "Mirrored Mask")
                    {
                        MQ.Cmd("/exchange \"mirrored mask\" face");
                        MQ.Delay(500);
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
                E3.Bots.Broadcast("I have recieved a character flag!");
            });

            #endregion

            #region Ducking

            pattern = "From the corner of your eye, you notice a Kyv taking aim at your head. You should duck\\.";
            EventProcessor.RegisterEvent("YouShouldDuck", pattern, (x) => {

                if(!MQ.Query<bool>("${Me.Ducking}"))
                {
                    MQ.Cmd("/nomodkey /keypress duck");
                    E3.Bots.Broadcast("/ar Ducking to avoid arrow.");
                }
            });
            pattern = "An arrow narrowly misses you\\.";
            EventProcessor.RegisterEvent("YouShouldStand", pattern, (x) => {

                if (MQ.Query<bool>("${Me.Ducking}"))
                {
                    MQ.Cmd("/nomodkey /keypress duck");
                    E3.Bots.Broadcast("/ag Avoided arrow standing up!");
                }

            });
			#endregion
			#region Ture_Warning
			pattern = "roars with fury as it surveys its attackers";
			EventProcessor.RegisterEvent("Ture_warning", pattern, (x) => {
				{

					if (E3.CurrentName == MQ.Query<string>("${Raid.Leader}"))
					{
						MQ.Cmd($"/rsay AE Rampage INC 5 seconds.");
					}
				}

			});
			#endregion

			#region Ture_Ramp_Start
			pattern = "eyes roll into its head as it goes into a frenzy";
			EventProcessor.RegisterEvent("Ture_Ramp_Start", pattern, (x) => {
				{

					if (E3.CurrentName == MQ.Query<string>("${Raid.Leader}"))
					{
						MQ.Cmd($"/rsay -+- 10k AE Rampage Started -+-");
					}
				}

			});
			#endregion

			#region Ture_Ramp_End
			pattern = "calms and regains its focus";
			EventProcessor.RegisterEvent("Ture_Ramp_End", pattern, (x) => {
				{

					if (E3.CurrentName == MQ.Query<string>("${Raid.Leader}"))
					{
						MQ.Cmd($"/rsay -+- Boss Safe - AE Rampage ended -+-");
					}
				}

			});
			#endregion

			#region Keldovan_Power
			pattern = "Keldovan the Harrier regains his combat stance";
			EventProcessor.RegisterEvent("Keldovan_Power", pattern, (x) => {
				{

					if (E3.CurrentName == MQ.Query<string>("${Raid.Leader}"))
					{
						MQ.Cmd($"/rsay -+- Keldovan has regained a power - KILL A DOG -+-");
					}
				}

			});
			#endregion
			#region Uqua
			pattern = "The (.+) must unlock the door to the next room\\.";
            EventProcessor.RegisterEvent("AlertUquaChamberKey", pattern, (x) => {

                if(x.match.Groups.Count>1)
                {
                    string classValue = x.match.Groups[1].Value;
                    MQ.Cmd($"/rsay >>^<< The {classValue} unlocks the door >>^<<");
                    MQ.Cmd($"/g >>^<< The {classValue} unlocks the door >>^<<");
                    E3.Bots.Broadcast($"\ar >>^<< \agThe \ap{classValue}\ag unlocks the door \ar>>^<<");
                    MQ.Cmd($"/popup \ar>>^<< The {classValue} unlocks the door >>^<<");
                }


            });

            #endregion

			
            pattern = $@"(.+) YOU for ([0-9]+) points of damage. \(Rampage\)";
            EventProcessor.RegisterEvent("RampageDamage", pattern, (x) => {
				
				if (E3.CharacterSettings.Alerts_RampageMessages)
				{
					if (x.match.Groups.Count > 2)
					{
						Int32 aggroPct = MQ.Query<Int32>("${Me.PctAggro}");
						if (aggroPct < 100)
						{
							string mobname = x.match.Groups[1].Value;
							string damage = x.match.Groups[2].Value;
							E3.Bots.Broadcast($"\arRAMPAGE\aw for \ar{damage}\aw damage from \ag{mobname}");
						}
					}
				}
				
            });

			//if not a tank, lets broadcast out that we are taking damage
			if ((E3.CurrentClass & Class.Tank) != E3.CurrentClass)
			{
				//You have taken 7840 points of damage.
				pattern = $@"\.  You have taken ([0-9]+) points of damage.";
				EventProcessor.RegisterEvent("NormalDamageToYou", pattern, (x) => {

					if (E3.CharacterSettings.Alerts_DamageMessages)
					{
						if (x.match.Groups.Count > 1)
						{
							string damage = x.match.Groups[1].Value;
							E3.Bots.Broadcast($"\arDAMAGE!\aw for \ar{damage}");
						}

					}

					
				});
				//You have taken 7840 points of damage.
				pattern = $@"You have taken ([0-9]+) damage from (.+) by (.+)";
				EventProcessor.RegisterEvent("NormalDamageToYou2", pattern, (x) => {
					if (E3.CharacterSettings.Alerts_DamageMessages)
					{
						if (x.match.Groups.Count > 3)
						{
							string damage = x.match.Groups[1].Value;
							string mobname = x.match.Groups[2].Value;
							string by = x.match.Groups[3].Value;
							E3.Bots.Broadcast($"\arDAMAGE!\aw for \ar{damage}\aw damage from \ag{mobname} \awby {by}");
						}
					}
					
				});
				pattern = $@"^(.+) YOU for ([0-9]+) points of damage\.$";
				EventProcessor.RegisterEvent("NormalDamageToYou3", pattern, (x) => {

					if (E3.CharacterSettings.Alerts_DamageMessages)
					{
						if (x.match.Groups.Count > 2)
						{
							string damage = x.match.Groups[2].Value;
							string mobname = x.match.Groups[1].Value;
							E3.Bots.Broadcast($"\arDAMAGE!\aw for \ar{damage}\aw damage from \ag{mobname}");
						}
					}
				});
			}
			pattern = @"(.+) spell has been reflected by (.+)\.";
			EventProcessor.RegisterEvent("ReflectSpell", pattern, (x) => {

				if(E3.CharacterSettings.Alerts_ReflectMessages)
				{
					if (x.match.Groups.Count > 2)
					{
						string mobname = x.match.Groups[1].Value;
						string personName = x.match.Groups[2].Value;
						if (E3.CurrentName == personName)
						{
							MQ.Cmd($"/g I have reflected {mobname} spell!");
						}
					}
				}
				
			});
			pattern = @"You abandon your preparations to camp\.";
			EventProcessor.RegisterEvent("PauseForCampUndo", pattern, (x) => {

				
				if (!Basics.IsPaused) return;
				Basics.Pause(false);
			});
			pattern = @"It will take you about 30 seconds to prepare your camp\.";
			EventProcessor.RegisterEvent("PauseForCamp30", pattern, (x) => {

				if (!E3.CharacterSettings.CPU_Camping_PauseAt30Seconds)
				{
					return;
				}
                Basics.Pause(true);
			});
			pattern = @"It will take about 20 more seconds to prepare your camp\.";
			EventProcessor.RegisterEvent("PauseForCamp20", pattern, (x) => {


				if (!E3.CharacterSettings.CPU_Camping_PauseAt20Seconds)
				{
					return;
				}
                if (Basics.IsPaused) return;
				Basics.Pause(true);
			});

			pattern = @"It will take about 5 more seconds to prepare your camp\.";
			EventProcessor.RegisterEvent("ShutdownForCamp", pattern, (x) => {
			
                if(!E3.CharacterSettings.CPU_Camping_ShutdownAt5Seconds)
                {
                    return;
                }

                if (Core._MQ2MonoVersion >= 0.22m)
				{
					MQ.Cmd("/shutdown", true);
				}
				
			});

            if(e3util.IsEQLive())
            {
				pattern = @"You gain party experience";
				EventProcessor.RegisterEvent("YouGainEXPParty", pattern, (x) => {

					E3.Bots.Broadcast(x.eventString + $" Total:{MQ.Query<string>("${Me.PctExp}")}%");

				});
				pattern = @"You gain experience";
				EventProcessor.RegisterEvent("YouGainEXP", pattern, (x) => {

					E3.Bots.Broadcast(x.eventString + $" Total:{MQ.Query<string>("${Me.PctExp}")}%");

				});
				pattern = @"(.+) has asked you to join the shared task";
				EventProcessor.RegisterEvent("GuildAddTask", pattern, (x) => {
					if (!E3.CharacterSettings.Misc_AutoJoinTasks) return;
					if (x.match.Groups.Count > 1)
					{
						string person = x.match.Groups[1].Value;
						//need to fill out GuildList.txt for it to work for guild members not in zone.
						if (e3util.InMyGuild(person))
						{
							
							MQ.Delay(7000);
							e3util.ClickYesNo(true);

						}
						else
						{
							E3.Bots.Broadcast($@"{person} tried to invite me to a task, but not in my guild or was in guild but not in zone and not in \e3 Macro Inis\guildlist.txt");
						}
					}
				});
				EventProcessor.RegisterCommand("/e3autojointasks", (x) =>
				{
					
					e3util.ToggleBooleanSetting(ref E3.CharacterSettings.Misc_AutoJoinTasks, "Auto Join Tasks", x.args);
					

				});
				

			}
		}
		
    }
}

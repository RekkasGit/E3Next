using E3Core.Classes;
using E3Core.Server;
using E3Core.Settings;
using E3Core.Settings.FeatureSettings;
using E3Core.Utility;
using MonoCore;
using NetMQ;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace E3Core.Processors
{
    /// <summary>
    /// The main file from which all other processing is called.
    /// </summary>
    public static class E3
	{
		public static bool _amIDead = false;
		/// <summary>
		/// The main processing loop. Things are broken up to keep this loop small and understandable.
		/// </summary>
		public static void Process()
		{
			_amIDead = Basics.AmIDead();
			if (!ShouldRun())
			{
				return;
			}
			//Init is here to make sure we only Init while InGame, as some queries will fail if not in game
			if (!IsInit) { Init(); }
			var sw = new Stopwatch();
			sw.Start();
			//auto 5 min gc check
			CheckGC();

			//did someone send us a command? lets process it. 
			ProcessExternalCommands();

			//update all states, important.
			StateUpdates();
			RefreshCaches();

			//don't eat stat food even if paused!
			Basics.CheckFood();
			//kickout after updates if paused
			if (IsPaused()) return;
			//stunned, no sense in processing
			if (MQ.Query<bool>("${Me.Stunned}")) return;
			if (MQ.Query<Int32>("${Me.CurrentHPs}") < 1) return; //we are dead
			if (MQ.Query<bool>("${Me.Feigning}") && E3.CharacterSettings.IfFDStayDown) return;


			//global action taken key, used by adv settings
			//if true, adv settings will stop processing for this loop.
			ActionTaken = false;
			BeforeAdvancedSettingsCalls();

			if (!_amIDead)
			{
				if (!ActionTaken)
				{
					//All the advanced Ini stuff here
					AdvancedSettingsCalls();
				}
				AfterAdvancedSettingsCalls();

				//attribute class calls
				ClassMethodCalls();
			}
			else
			{
				//follow/rez/etc
				ClassMethodCalls();
			}

			
            //final cleanup/actions after the main loop has done processing
            FinalCalls();
        }
	
		private static void BeforeAdvancedSettingsCalls()
		{
			if (PctHPs < 98)
			{
				Heals.Check_LifeSupport();
			}
			//nowcast before all.
			EventProcessor.ProcessEventsInQueues("/nowcast");
			EventProcessor.ProcessEventsInQueues("/backoff");
			EventProcessor.ProcessEventsInQueues("/assistme");

			if (!_amIDead)
			{
				//use burns if able, this is high as some heals need burns as well
				Burns.UseBurns();
				//do the basics first
				//first and formost, do healing checks
				if ((CurrentClass & Data.Class.Priest) == CurrentClass)
				{
					ActionTaken = false;
					Heals.Check_Heals();
					Basics.CheckManaResources();
					if (ActionTaken) return; //we did a heal, kick out as we may need to do another heal.
				}
				//instant buffs have their own shouldcheck, need it snappy so check quickly.
				BuffCheck.BuffInstant(E3.CharacterSettings.InstantBuffs);
				Assist.Process();
			}
			Rez.Process();

		}
		private static void AdvancedSettingsCalls()
		{
			using (Log.Trace("AdvMethodCalls"))
			{
				//rembmer check_heals is auto inserted, should probably just pull out here
				List<string> _methodsToInvokeAsStrings;
				if (AdvancedSettings.ClassMethodsAsStrings.TryGetValue(CurrentShortClassString, out _methodsToInvokeAsStrings))
				{
					foreach (var methodName in _methodsToInvokeAsStrings)
					{
						Burns.UseBurns();
						//if an action was taken, start over
						if (ActionTaken)
						{
							break;
						}
						Action methodToInvoke;
						if (AdvancedSettings.MethodLookup.TryGetValue(methodName, out methodToInvoke))
						{
							methodToInvoke.Invoke();
						}
						//check backoff
						//check nowcast
						EventProcessor.ProcessEventsInQueues("/nowcast");
						EventProcessor.ProcessEventsInQueues("/backoff");
					}
				}
			}
		}
		
		private static void AfterAdvancedSettingsCalls()
		{
			EventProcessor.ProcessEventsInQueues("/backoff");
			EventProcessor.ProcessEventsInQueues("/assistme");

			Assist.Process();

			//process any requests commands from the UI.
			PubClient.ProcessRequests();

			//bard song player
			if (E3.CurrentClass == Data.Class.Bard)
			{
				Bard.Check_AutoMez();
				Bard.check_BardSongs();
			}
		}
	
		private static void ClassMethodCalls()
		{

			//class attribute method calls, call them all!
			//in case any of them change the target, put it back after called
			Int32 orgTargetID = MQ.Query<Int32>("${Target.ID}");

			//using (Log.Trace("ClassMethodCalls"))
			{

				//lets do our class methods, this is last because of bards
				foreach (var kvp in AdvancedSettings.ClassMethodLookup)
				{
					Burns.UseBurns();
					//using (Log.Trace($"ClassMethodCalls-{kvp.Key}-Main"))
					{
						kvp.Value.Invoke();
					}
					EventProcessor.ProcessEventsInQueues("/nowcast");
					EventProcessor.ProcessEventsInQueues("/backoff");
					EventProcessor.ProcessEventsInQueues("/assistme");
				}
				e3util.PutOriginalTargetBackIfNeeded(orgTargetID);
			}


		}
		private static void FinalCalls()
		{


			if(!_amIDead)
			{
				using (Log.Trace("LootProcessing"))
				{
					Loot.Process();
				}
				//instant buffs have their own shouldcheck, need it snappy so check quickly.
				BuffCheck.BuffInstant(E3.CharacterSettings.InstantBuffs);

			}

			//were modifications made to the settings files?
			CheckModifiedSettings();
		}
		private static void CheckModifiedSettings()
        {
            if (!e3util.ShouldCheck(ref _nextReloadSettingsCheck, _nextReloadSettingsInterval)) return;


			if (GlobalIfs.ShouldReload())
			{
				E3.Bots.Broadcast("\aoAuto-Reloading Global Ifs/Character settings settings file...");
				E3.GlobalIfs = new GlobalIfs();
				CharacterSettings = new CharacterSettings();
				Loot.Reset();
				GiveMe.Reset();
				Burns.Reset();
				Bard.RestartMelody();
				Setup.GetExposedDataMappedToDictionary();
				E3.Bots.Broadcast("\aoComplete!");
			}
			else if (CharacterSettings.ShouldReload())
            {
                E3.Bots.Broadcast("\aoAuto-Reloading Character settings file...");
                CharacterSettings = new CharacterSettings();
                Loot.Reset();
                GiveMe.Reset();
				Burns.Reset();
				Bard.RestartMelody();
				Setup.GetExposedDataMappedToDictionary();
				E3.Bots.Broadcast("\aoComplete!");
               
            }

            if (GlobalCursorDelete.ShouldReload())
            {
                E3.Bots.Broadcast("\aoAuto-Reloading Global Cursor Delete/Character settings settings file...");
                E3.GlobalCursorDelete = new GlobalCursorDelete();
                CharacterSettings = new CharacterSettings();
                Loot.Reset();
                GiveMe.Reset();
				Burns.Reset();
				Bard.RestartMelody();
				Setup.GetExposedDataMappedToDictionary();
				E3.Bots.Broadcast("\aoComplete!");
            }

            if (GeneralSettings.ShouldReload())
            {
                E3.Bots.Broadcast("\aoAuto-Reloading General settings file...");
                GeneralSettings = new GeneralSettings();
                Loot.Reset();
                E3.Bots.Broadcast("\aoComplete!");
            }
            if (Zoning.TributeDataFile.ShouldReload())
            {
                E3.Bots.Broadcast("\aoAuto-Reloading Tribute settings file...");
                Zoning.TributeDataFile.LoadData();
                Zoning.TributeDataFile.ToggleTribute();
                E3.Bots.Broadcast("\aoComplete!");
            }
			if (Loot.LootStackableSettings.ShouldReload())
			{
				E3.Bots.Broadcast("\aoAuto-Reloading Loot Stackable Settings...");
				Loot.LootStackableSettings.LoadData();
				E3.Bots.Broadcast("\aoComplete!");
			}
		}
       
		public static bool IsPaused()
        {
            EventProcessor.ProcessEventsInQueues("/e3p");

            if (Basics.IsPaused)
            {
                return true;
            }
            return false;
        }
		/// <summary>
		/// This is used during thigns like casting, while we are delaying a ton
		/// so that we can keep things 'up to date' such as hp, invs status, pet name, etc. 
		/// 
		/// </summary>
		/// 
		private static Int64 _nextStateUpdateCheckTime = 0;
		//needs to be fast to be able to show a new buff has landed
		private static Int64 _nextBuffUpdateCheckTime = 0;
		private static Int64 _nextSlowUpdateCheckTime = 0;
		private static Int64 _nextMiscUpdateCheckTime = 0;
		private static Int64 _MiscUpdateCheckRate = 100;
	
		//qick hack to prevent calling state update... while in state updates. 
		public static bool InStateUpdate = false;

		public static void StateUpdates_Counters()
		{
			
			PubServer.AddTopicMessage("${Me.TotalCounters}", MQ.Query<string>("${Debuff.Count}"));
			PubServer.AddTopicMessage("${Me.CountersPoison}", MQ.Query<string>("${Debuff.Poisoned}"));
			PubServer.AddTopicMessage("${Me.CountersDisease}", MQ.Query<string>("${Debuff.Diseased}"));
			PubServer.AddTopicMessage("${Me.CountersCurse}", MQ.Query<string>("${Debuff.Cursed}"));
			PubServer.AddTopicMessage("${Me.CountersCorrupted}", MQ.Query<string>("${Debuff.Corrupted}"));
			
		}
		public static void StateUpdates_Misc()
		{
			PubServer.AddTopicMessage("${InCombat}", CurrentInCombat.ToString());
			PubServer.AddTopicMessage("${EQ.CurrentFocusedWindowName}", MQ.GetFocusedWindowName());
			//PubServer.AddTopicMessage("${EQ.CurrentHoveredWindowName}", MQ.GetHoverWindowName());
			PubServer.AddTopicMessage("${Me.CurrentTargetID}", MQ.Query<string>("${Target.ID}"));
			PubServer.AddTopicMessage("${Me.ZoneID}", MQ.Query<string>("${Zone.ID}"));
			PubServer.AddTopicMessage("${Me.Instance}", MQ.Query<string>("${Me.Instance}"));

		}
		public static void StateUpdates_Stats()
		{
			PubServer.AddTopicMessage("${Me.PctMana}", MQ.Query<string>("${Me.PctMana}"));
			PubServer.AddTopicMessage("${Me.PctEndurance}", MQ.Query<string>("${Me.PctEndurance}"));
			PubServer.AddTopicMessage("${Me.PctHPs}", PctHPs.ToString());
			PubServer.AddTopicMessage("${Me.CurrentHPs}", MQ.Query<string>("${Me.CurrentHPs}"));
			PubServer.AddTopicMessage("${Me.CurrentMana}", MQ.Query<string>("${Me.CurrentMana}"));
			PubServer.AddTopicMessage("${Me.CurrentEndurance}", MQ.Query<string>("${Me.CurrentEndurance}"));
		}
		public static void StateUpdates_BuffInformation()
		{
			PubServer.AddTopicMessage("${Me.BuffInfo}", e3util.GenerateBuffInfoForPubSub());
			PubServer.AddTopicMessage("${Me.PetBuffInfo}", e3util.GeneratePetBuffInfoForPubSub());
		}
		public static void StateUpdates_AAInformation()
		{
			PubServer.AddTopicMessage("${Me.AAPoints}", MQ.Query<string>("${Me.AAPoints}"));
			PubServer.AddTopicMessage("${Me.AAPointsAssigned}", MQ.Query<string>("${Me.AAPointsAssigned}"));
			PubServer.AddTopicMessage("${Me.AAPointsSpent}", MQ.Query<string>("${Me.AAPointsSpent}"));
			PubServer.AddTopicMessage("${Me.AAPointsTotal}", MQ.Query<string>("${Me.AAPointsTotal}"));
		}
		public static void ProcessExternalCommands()
		{
			NetMQServer.SharedDataClient.ProcessCommands(); //recieving data
			e3util.ProcessE3BCCommands(); //send out data we may have queued up for /e3bc commands
			RouterServer.ProcessRequests();//process any tlo request from the UI, or anything really.
			//process any commands we need to process from the UI
			PubClient.ProcessRequests();
		}
		public static void StateUpdates()
        {

			try
			{
				//this is important so that we do not get caught up in recursion during a Delay as delay can call this. 
				InStateUpdate = true;
				PctHPs = MQ.Query<int>("${Me.PctHPs}");
				IsInvis = MQ.Query<bool>("${Me.Invis}");
				CurrentId = MQ.Query<int>("${Me.ID}");
				CurrentInCombat = Basics.InCombat();

				//hp, mana, counters, etc, should send out quickly, but no more than say 50 milliseconds
				if (e3util.ShouldCheck(ref _nextStateUpdateCheckTime, E3.CharacterSettings.CPU_PublishStateDataInMS))
				{
					StateUpdates_Stats();
				}
				//other stuff not quite so quickly
				if (e3util.ShouldCheck(ref _nextMiscUpdateCheckTime, _MiscUpdateCheckRate))
				{
					StateUpdates_Misc();
				}
				//expensive only send out once per second?
				if (e3util.ShouldCheck(ref _nextBuffUpdateCheckTime, E3.CharacterSettings.CPU_PublishBuffDataInMS))
				{
					StateUpdates_BuffInformation();
					StateUpdates_Counters();
				}
				
				//not horribly important stuff, can just be sent out whever, currently once per second
				if (e3util.ShouldCheck(ref _nextSlowUpdateCheckTime, E3.CharacterSettings.CPU_PublishSlowDataInMS))
				{
					StateUpdates_AAInformation();
					//lets query the data we are configured to send out extra
					if (E3.CharacterSettings.E3BotsPublishData.Count > 0)
					{
						foreach (var pair in E3.CharacterSettings.E3BotsPublishData)
						{
							//to parse out custom values
							string valueToCheck = Casting.Ifs_Results(pair.Value);
							string resultvalue= MQ.Query<string>(valueToCheck);
							PubServer.AddTopicMessage(pair.Key, resultvalue);
						}
					}
					string nameOfPet = MQ.Query<string>("${Me.Pet.CleanName}");
					if (nameOfPet != "NULL")
					{
						//set the pet name
						CurrentPetName = nameOfPet;
						PubServer.AddTopicMessage("${Me.Pet.CleanName}", CurrentPetName);
					}
					string nameOfMerc = MQ.Query<string>("${Mercenary.CleanName}");
					if (nameOfMerc != "NULL")
					{
						//set the pet name
						CurrentMercName = nameOfMerc;
						PubServer.AddTopicMessage("${Mercenary.CleanName}", CurrentMercName);
					}
					E3.IsMoving = MQ.Query<bool>("${Me.Moving}");
					if (E3.IsMoving)
					{
						LastMovementTimeStamp = Core.StopWatch.ElapsedMilliseconds;
					}

					if (MQ.Query<bool>("${MoveUtils.GM}"))
					{
						if (e3util.IsEQEMU())
						{
							MQ.Cmd("/squelch /stick imsafe");
						}
						Bots.Broadcast("GM Safe kicked in, on live issue /stick imsafe.  you may need to reissue /followme or /assiston");
					}
				}

			
			}
            finally
            {
                InStateUpdate = false;
            }
			
           
        }
        private static void RefreshCaches()
        {
            Casting.RefreshGemCache();
            Basics.RefreshGroupMembers();
        }
		public static void ReInit()
		{
			string classValue = e3util.ClassNameFix(MQ.Query<string>("${Me.Class}"));
			Enum.TryParse(classValue, out CurrentClass);
			CurrentLongClassString = CurrentClass.ToString();
			CurrentShortClassString = Data.EQClasses.ClassLongToShort[CurrentLongClassString];
			if(e3util.IsEQLive())
			{
				e3util.MobMaxDebuffSlots = 200;
			}
		}
        private static void Init()
        {

            if (!IsInit)
            {
                MQ.ClearCommands();
                AsyncIO.ForceDotNet.Force();
				if (e3util.IsEQLive())
				{
					e3util.MobMaxDebuffSlots = 200;
					e3util.XtargetMax = 20;
				}
				Logging.TraceLogLevel = Logging.LogLevels.None; //log level we are currently at
                Logging.MinLogLevelTolog = Logging.LogLevels.Error; //log levels have integers assoicatd to them. you can set this to Error to only log errors. 
                Logging.DefaultLogLevel = Logging.LogLevels.Debug; //the default if a level is not passed into the _log.write statement. useful to hide/show things.
                MainProcessor.ApplicationName = "E3"; //application name, used in some outputs
                MonoCore.MQ.MaxMillisecondsToWork = 40;
                //max event count for each registered event before spilling over.
                EventProcessor.EventLimiterPerRegisteredEvent = 20;
                CurrentName = MQ.Query<string>("${Me.CleanName}");

                CurrentId = MQ.Query<int>("${Me.ID}");
                //do first to get class information
                

                CurrentName = MQ.Query<string>("${Me.CleanName}");
                ServerName = e3util.FormatServerName(MQ.Query<string>("${MacroQuest.Server}"));
                //deal with the Shadow Knight class issue.
                string classValue =e3util.ClassNameFix(MQ.Query<string>("${Me.Class}"));
                Enum.TryParse(classValue, out CurrentClass);
                CurrentLongClassString = CurrentClass.ToString();
                CurrentShortClassString = Data.EQClasses.ClassLongToShort[CurrentLongClassString];

                //Init the settings
                GeneralSettings = new Settings.GeneralSettings();

				NetMQServer.Init();
				if (Bots == null)
				{
                   	Bots = new SharedDataBots();

                    //if ("DANNET".Equals(E3.GeneralSettings.General_NetworkMethod, StringComparison.OrdinalIgnoreCase) && Core._MQ2MonoVersion > 0.20m)
                    //{
                    //    Bots = new DanBots();
                    //}
                    //else
                    //{
                    //    Bots = new Bots();
                    //}
                }
				GlobalIfs = new GlobalIfs();
				GlobalCursorDelete = new GlobalCursorDelete();
				CharacterSettings = new Settings.CharacterSettings();
                AdvancedSettings = new Settings.AdvancedSettings();
				
				//setup is done after the settings are setup.
				//as there is an order dependecy
				Setup.Init();
                IsInit = true;
                MonoCore.Spawns.RefreshTimePeriodInMS = 500;
			}


        }
      
        private static bool ShouldRun()
        {

            if (IsBadState)
            {
                return false;
            }

            return true;
        }
        public static void Shutdown()
        {
            IsBadState = true;
            AdvancedSettings.Reset();
            CharacterSettings = null;
            GeneralSettings = null;
            AdvancedSettings = null;
            Spawns.EmptyLists();
           

        }
        //test to see if we need to GC every 5 min to maintain proper memory profile
        private static void CheckGC()
        {
            if(_lastGCCollect==0)
            {
                _lastGCCollect = Core.StopWatch.ElapsedMilliseconds;
            }

            if(Core.StopWatch.ElapsedMilliseconds - _lastGCCollect> 300000)
            {
                //GC collect every 5 min
                GC.Collect();
                _lastGCCollect = Core.StopWatch.ElapsedMilliseconds;
            }
        }

        public static bool ActionTaken = false;
        public static bool Following = false;
        public static long StartTimeStamp;
		[ExposedData("Core", "IsInit")]
		public static bool IsInit = false;
		public static bool IsBadState = false;
        public static IMQ MQ = Core.mqInstance;
        public static Logging Log = Core.logInstance;
        public static Settings.CharacterSettings CharacterSettings = null;
		public static Settings.FeatureSettings.GlobalIfs GlobalIfs = null;
		public static Settings.FeatureSettings.GlobalCursorDelete GlobalCursorDelete = null;
		public static Settings.GeneralSettings GeneralSettings = null;
        public static Settings.AdvancedSettings AdvancedSettings = null;
        public static IBots Bots = null;
        public static string CurrentName;
        public static Data.Class CurrentClass;
        public static string ServerName;
        public static string CurrentPetName = String.Empty;
		public static string CurrentMercName = String.Empty;
        public static bool CurrentInCombat = false;
        public static int CurrentId;
		[ExposedData("Core", "LastMovementTimeStamp")]
        public static Int64 LastMovementTimeStamp;
        public static string CurrentLongClassString;
        public static string CurrentShortClassString;
		public static System.Random Random = new System.Random();
		public static int PctHPs;
        public static ISpawns Spawns = Core.spawnInstance;
        public static bool IsInvis;
		public static bool IsMoving;

		private static Int64 _nextReloadSettingsCheck = 0;
        private static Int64 _nextReloadSettingsInterval = 2000;
        private static Int64 _lastGCCollect = 0;
        public volatile static bool NetMQ_PubServerThradRun = true;
		public volatile static bool NetMQ_SharedDataServerThreadRun = true;
		public volatile static bool NetMQ_RouterServerThradRun = true;
		public volatile static bool NetMQ_PubClientThradRun = true;

		public static MQBuild MQBuildVersion = MQBuild.EMU;


	}
	public enum MQBuild
	{
		Live=1,
		Test=2,
		Beta=3,
		EMU=4
	}
}

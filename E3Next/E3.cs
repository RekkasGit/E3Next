using E3Core.Classes;
using E3Core.Data;
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
using System.Linq;
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
		/// <summary>
		/// The main processing loop. Things are broken up to keep this loop small and understandable.
		/// </summary>
		public static void Process()
        {

            if (!ShouldRun())
            {
                return;
            }
			//Init is here to make sure we only Init while InGame, as some queries will fail if not in game
			if (!IsInit) { Init(); }

			//auto 5 min gc check
			CheckGC();
	
			//update all states, important.
			StateUpdates();
			RefreshCaches();

			//kickout after updates if paused
			if (IsPaused()) return;

			//global action taken key, used by adv settings
			//if true, adv settings will stop processing for this loop.
            ActionTaken = false;
         
			
            BeforeAdvancedSettingsCalls();
			if (!ActionTaken)
			{
				//All the advanced Ini stuff here
				AdvancedSettingsCalls();
			}
            AfterAdvancedSettingsCalls();
			
			//attribute class calls
			ClassMethodCalls();

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

			Rez.Process();
			if (Basics.AmIDead()) return;
			Assist.Process();

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
						//using (Log.Trace($"{methodName}-Burns"))
						{
							Burns.UseBurns();

						}

						//using (Log.Trace($"{methodName}-Main"))
						{
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
						}

						//check backoff
						//check nowcast
						//using (Log.Trace($"{methodName}-CheckQueues"))
						{
							EventProcessor.ProcessEventsInQueues("/nowcast");
							EventProcessor.ProcessEventsInQueues("/backoff");

						}

					}
				}
			}
		}
		
		private static void AfterAdvancedSettingsCalls()
		{
			EventProcessor.ProcessEventsInQueues("/backoff");
			Assist.Process();

			//process any requests commands from the UI.
			PubClient.ProcessRequests();

			//bard song player
			if (E3.CurrentClass == Data.Class.Bard)
			{
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
				}
				e3util.PutOriginalTargetBackIfNeeded(orgTargetID);
			}


		}
		private static void FinalCalls()
		{
			using (Log.Trace("LootProcessing"))
			{
				Loot.Process();
			}
			//instant buffs have their own shouldcheck, need it snappy so check quickly.
			BuffCheck.BuffInstant(E3.CharacterSettings.InstantBuffs);

			//were modifications made to the settings files?
			CheckModifiedSettings();
		}
		private static void CheckModifiedSettings()
        {
            if (!e3util.ShouldCheck(ref _nextReloadSettingsCheck, _nextReloadSettingsInterval)) return;

            if (CharacterSettings.ShouldReload())
            {
                E3.Bots.Broadcast("\aoAuto-Reloading Character settings file...");
                CharacterSettings = new CharacterSettings();
                Loot.Reset();
                GiveMe.Reset();
				Bard.RestartMelody();
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
		private static Int64 _nextStateUpdateTimeInterval = 50;

		private static Int64 _nextBuffUpdateCheckTime = 0;
		private static Int64 _nextBuffUpdateTimeInterval = 1000;


        //qick hack to prevent calling state update... while in state updates. 
        public static bool InStateUpdate = false;
		public static void StateUpdates()
        {
           
            try
            {
                InStateUpdate = true;
				NetMQServer.SharedDataClient.ProcessCommands(); //recieving data
				NetMQServer.SharedDataClient.ProcessE3BCCommands();//sending out data

				if (e3util.ShouldCheck(ref _nextBuffUpdateCheckTime, _nextBuffUpdateTimeInterval))
				{
					PubServer.AddTopicMessage("${Me.BuffInfo}", e3util.GenerateBuffInfoForPubSub());
					PubServer.AddTopicMessage("${Me.PetBuffInfo}", e3util.GeneratePetBuffInfoForPubSub());
				}

				if (!e3util.ShouldCheck(ref _nextStateUpdateCheckTime, _nextStateUpdateTimeInterval)) return;
				PctHPs = MQ.Query<int>("${Me.PctHPs}");
				//cure counters
				PubServer.AddTopicMessage("${Me.TotalCounters}", MQ.Query<string>("${Debuff.Count}"));
				PubServer.AddTopicMessage("${Me.CountersPoison}", MQ.Query<string>("${Debuff.Poisoned}"));
				PubServer.AddTopicMessage("${Me.CountersDisease}", MQ.Query<string>("${Debuff.Diseased}"));
				PubServer.AddTopicMessage("${Me.CountersCurse}", MQ.Query<string>("${Debuff.Cursed}"));
				PubServer.AddTopicMessage("${Me.CountersCorrupted}", MQ.Query<string>("${Debuff.Corrupted}"));
				//end cure counters
				PubServer.AddTopicMessage("${Me.PctMana}", MQ.Query<string>("${Me.PctMana}"));
				PubServer.AddTopicMessage("${Me.PctEndurance}", MQ.Query<string>("${Me.PctEndurance}"));
				PubServer.AddTopicMessage("${Me.PctHPs}", PctHPs.ToString());
				PubServer.AddTopicMessage("${Me.CurrentHPs}", MQ.Query<string>("${Me.CurrentHPs}"));
				PubServer.AddTopicMessage("${Me.CurrentMana}", MQ.Query<string>("${Me.CurrentMana}"));
				PubServer.AddTopicMessage("${Me.CurrentEndurance}", MQ.Query<string>("${Me.CurrentEndurance}"));
				PubServer.AddTopicMessage("${Me.Name}", E3.CurrentName);
				PubServer.AddTopicMessage("${Me.TargetName}", MQ.Query<string>("${Target.Name}"));
				PubServer.AddTopicMessage("${Me.AAPoints}", MQ.Query<string>("${Me.AAPoints}"));
				PubServer.AddTopicMessage("${Me.Casting}", MQ.Query<string>("${Me.Casting}"));

				//TopicUpdates
				//get bot network useres
				Int32 count = 1;
				foreach(var pair in NetMQServer.SharedDataClient.TopicUpdates)
				{
					PubServer.AddTopicMessage($"${{E3Bot{count}.Name}}",pair.Key);
					PubServer.AddTopicMessage($"${{E3Bot{count}.Target}}", E3.Bots.Query(pair.Key, "${Me.TargetName}"));
					PubServer.AddTopicMessage($"${{E3Bot{count}.Casting}}", E3.Bots.Query(pair.Key, "${Me.Casting}"));
					PubServer.AddTopicMessage($"${{E3Bot{count}.AAPoints}}", E3.Bots.Query(pair.Key, "${Me.AAPoints}"));
					PubServer.AddTopicMessage($"${{E3Bot{count}.Casting}}", E3.Bots.Query(pair.Key, "${Me.Casting}"));

					count++;
				}

				IsInvis = MQ.Query<bool>("${Me.Invis}");
			
				CurrentId = MQ.Query<int>("${Me.ID}");
				CurrentInCombat = Basics.InCombat();
				PubServer.AddTopicMessage("${InCombat}", CurrentInCombat.ToString());
				PubServer.AddTopicMessage("${EQ.CurrentFocusedWindowName}", MQ.GetFocusedWindowName());

				string nameOfPet = MQ.Query<string>("${Me.Pet.CleanName}");
				if (nameOfPet != "NULL")
				{
					//set the pet name
					CurrentPetName = nameOfPet;
					PubServer.AddTopicMessage("${Me.Pet.CleanName}", CurrentPetName);
				}

				bool IsMoving = MQ.Query<bool>("${Me.Moving}");
				if (IsMoving)
				{
					LastMovementTimeStamp = Core.StopWatch.ElapsedMilliseconds;
				}
				if (MQ.Query<bool>("${MoveUtils.GM}"))
				{
					MQ.Cmd("/squelch /stick imsafe");
					Bots.Broadcast("GM Safe kicked in, issued /stick imsafe.  you may need to reissue /followme or /assiston");
				}

				//process any tlo request from the UI, or anything really.
				RouterServer.ProcessRequests();
				//process any commands we need to process from the UI
				PubClient.ProcessRequests();
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
        private static void Init()
        {

            if (!IsInit)
            {
                MQ.ClearCommands();
                AsyncIO.ForceDotNet.Force();

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
                CurrentShortClassString = Data.Classes.ClassLongToShort[CurrentLongClassString];

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
        public static bool IsInit = false;
        public static bool IsBadState = false;
        public static IMQ MQ = Core.mqInstance;
        public static Logging Log = Core.logInstance;
        public static Settings.CharacterSettings CharacterSettings = null;
        public static Settings.GeneralSettings GeneralSettings = null;
        public static Settings.AdvancedSettings AdvancedSettings = null;
        public static IBots Bots = null;
        public static string CurrentName;
        public static Data.Class CurrentClass;
        public static string ServerName;
        public static string CurrentPetName = String.Empty;
        public static bool CurrentInCombat = false;
        public static int CurrentId;
        public static Int64 LastMovementTimeStamp;
        public static string CurrentLongClassString;
        public static string CurrentShortClassString;

		public static int PctHPs;
        public static ISpawns Spawns = Core.spawnInstance;
        public static bool IsInvis;
        private static Int64 _nextReloadSettingsCheck = 0;
        private static Int64 _nextReloadSettingsInterval = 2000;
        private static Int64 _lastGCCollect = 0;
        public volatile static bool NetMQ_PubServerThradRun = true;
		public volatile static bool NetMQ_SharedDataServerThradRun = true;
		public volatile static bool NetMQ_RouterServerThradRun = true;
		public volatile static bool NetMQ_PubClientThradRun = true;



	}
}

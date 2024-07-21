﻿using E3Core.Data;
using E3Core.Server;
using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using static MonoCore.EventProcessor;

namespace E3Core.Processors
{
    public interface IBots
    {
       
     //   Boolean InZone(string Name);
        Int32 PctHealth(string name);
        List<string> BotsConnected();
        Boolean HasShortBuff(string name, Int32 buffid);
        void BroadcastCommand(string command, bool noparse = false, CommandMatch match = null);
        void BroadcastCommandToGroup(string command, CommandMatch match=null, bool noparse = false);
        void BroadcastCommandToPerson(string person, string command, bool noparse = false);
        void Broadcast(string message, bool noparse = false);
        List<Int32> BuffList(string name);
        List<Int32> PetBuffList(string name);
        Int32 BaseDebuffCounters(string name);
        Int32 BaseDiseasedCounters(string name);
		Int32 BaseCorruptedCounters(string name);
		Int32 BasePoisonedCounters(string name);
        Int32 BaseCursedCounters(string name);
        bool IsMyBot(string name);
        void Trade(string name);
		string Query(string name, string query);
		CharacterBuffs GetBuffInformation(string name);
    }
  

    public class SharedDataBots : IBots
    {
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static Dictionary<string, CharacterBuffs> _characterBuffs = new Dictionary<string, CharacterBuffs>();
        private static Dictionary<string, CharacterBuffs> _petBuffs = new Dictionary<string, CharacterBuffs>();
		private static System.Text.StringBuilder _stringBuilder = new System.Text.StringBuilder();
		[ExposedData("Bots", "GlobalAllEnabled")]
		private static bool GlobalAllEnabled = false;
		[ExposedData("Bots", "NetworkingPathsTolookAt")]
		List<string> _pathsTolookAt = new List<string>();
		Task _autoRegisrationTask;
        public SharedDataBots()
        {
            string settingsFilePath = BaseSettings.GetSettingsFilePath("");

            if(!settingsFilePath.EndsWith(@"\"))
            {
                settingsFilePath += @"\";
            }

            settingsFilePath+=@"SharedData\";

            if(!Directory.Exists(settingsFilePath))
            {
                Directory.CreateDirectory(settingsFilePath);
            }
            _pathsTolookAt.Add(settingsFilePath);
           
            //add other paths that have been configured to look at
            foreach(var path in E3.GeneralSettings.General_E3NetworkAddPathToMonitor)
            {
                _pathsTolookAt.Add(path);
            }

			_autoRegisrationTask = Task.Factory.StartNew(() => { AutoRegisterUsers(_pathsTolookAt); }, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

			//had to be registered in this order so that you don't get wildcard matches happening first in mq
			//there is no 'exact match' in the MQ command linked list
			//smallest to largest
			EventProcessor.RegisterCommand("/e3GlobalBroadcast", (x) =>
			{
				GlobalAllEnabled = !GlobalAllEnabled;
				Broadcast($"\agSetting Global Broadcast to {GlobalAllEnabled}");

			});
			EventProcessor.RegisterCommand("/e3bc", (x) =>
			{
				if (x.args.Count > 0)
				{

					string message = e3util.ArgsToCommand(x.args);
					if (message.StartsWith(@"/"))
                    {
                        BroadcastCommand(message,true,x);
                    }
                    else
                    {
						Broadcast(message,true);

					}
				}
			});
			EventProcessor.RegisterCommand("/e3bca", (x) =>
			{
				if (x.args.Count > 0)
				{

					string message = e3util.ArgsToCommand(x.args);
					if (message.StartsWith(@"/"))
					{
						BroadcastCommand(message, true,x);
					}
					else
					{
						Broadcast(message, true);

					}
				}
			});
			EventProcessor.RegisterCommand("/e3bcz", (x) =>
			{
				if (x.args.Count > 0)
				{

					string message = e3util.ArgsToCommand(x.args);
					if (message.StartsWith(@"/"))
					{
						BroadcastCommandAllZoneNotMe(message, true,x);
					}
					else
					{
						BroadcastZone(message, true);

					}
				}
			});
			EventProcessor.RegisterCommand("/e3bcg", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandToGroup(command,x,true);

				}
				
			});
			EventProcessor.RegisterCommand("/e3bcgz", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandToGroupZone(command, x, true);

				}

			});
			EventProcessor.RegisterCommand("/e3bcchannel", (x) =>
			{
				if (x.args.Count > 1)
				{
					string channel = x.args[0];
					x.args.RemoveAt(0);
					string command = e3util.ArgsToCommand(x.args);
					if (x.filters.Count > 0)
					{
						command += " \"" + e3util.ArgsToCommand(x.filters) + "\"";
					}

					BroadcastCommandToChannel(channel, command, true);
				}
			});
			EventProcessor.RegisterCommand("/e3bct", (x) =>
			{
				if (x.args.Count > 1)
				{
					string person = x.args[0];
					x.args.RemoveAt(0);
					string command = e3util.ArgsToCommand(x.args);
					if(x.filters.Count > 0)
					{
						command += " \""+ e3util.ArgsToCommand(x.filters)+"\"";
					}

					BroadcastCommandToPerson(person, command,true);
				}
			});
			EventProcessor.RegisterCommand("/e3bcga", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandToGroupAll(command,x,true);
		            
                }
			});
			EventProcessor.RegisterCommand("/e3bcgza", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandToGroupAllZone(command, x, true);

				}
			});
			EventProcessor.RegisterCommand("/e3bcaa", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
			        BroadcastCommandAll(command,true,x);

				}
			});
			EventProcessor.RegisterCommand("/e3bcza", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandAllZone(command, true,x);

				}
			});
			EventProcessor.RegisterCommand("/e3bcr", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandRaidNotMe(command, true, x);

				}
			});
			EventProcessor.RegisterCommand("/e3bcrz", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandToRaidNotMeZone(command, x,true);

				}
			});
			EventProcessor.RegisterCommand("/e3bcra", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandRaid(command, true,x);

				}
			});
			EventProcessor.RegisterCommand("/e3bcraz", (x) =>
			{
				if (x.args.Count > 0)
				{
					string command = e3util.ArgsToCommand(x.args);
					BroadcastCommandRaidZone(command, x,true);

				}
			});

		}
		public CharacterBuffs GetBuffInformation(string name)
		{
			UpdateBuffInfoUserInfo(name, name, "${Me.BuffInfo}", _characterBuffs);

			if (_characterBuffs.TryGetValue(name, out var buff)) return buff;

			return null;
		}

		
        private void AutoRegisterUsers(List<string> settingsPaths)
        {
			///we can have two states here, the normal user_server_pubsubport.txt
			///and the "proxy_pubsubport.txt". If the proxy_pubsubport exists, we use that and only that.
            string searchPattern = $"*_{E3.ServerName}_pubsubport.txt";
			bool inProxyState = false;
			string proxyFileFullName = string.Empty;
			DateTime proxyFileLastUpdateTime = DateTime.MinValue; 
			while (Core.IsProcessing)
            {
				//currently have proxy file, to just keep the thread checking for updates
				if(inProxyState)
				{
					System.Threading.Thread.Sleep(500);

					if(!File.Exists(proxyFileFullName))
					{    //proxy file poofed, will need to restart proxy
						continue;
					}
					//need to check for proxy file update
					DateTime lastUpdate = System.IO.File.GetLastWriteTime(proxyFileFullName);

					if(lastUpdate>proxyFileLastUpdateTime)
					{
						inProxyState = false;
					
					}

					continue;
				}
				//check to see if there are any proxy setups, if so do them first
				foreach (var path in settingsPaths)
				{
					string tpath = path;
					if (!tpath.EndsWith(@"\"))
					{
						tpath += @"\";
					}
					if (File.Exists($@"{tpath}proxy_pubsubport.txt"))
					{

						//we are in proxy mode, set proxy state and kick out

						NetMQServer.SharedDataClient.RegisterUser("proxy", tpath, true);
						inProxyState = true;
						proxyFileFullName = $@"{tpath}proxy_pubsubport.txt";
						proxyFileLastUpdateTime = System.IO.File.GetLastWriteTime(proxyFileFullName);
						break;

					}
				}
				if (inProxyState) continue;
				foreach (var path in settingsPaths)
				{
					string tpath = path;
					if (!tpath.EndsWith(@"\"))
					{
						tpath += @"\";
					}
					
					try
                    {
						//look for files that start with $"{user}_{E3.ServerName}_pubsubport.txt"
						string[] fileNames = System.IO.Directory.GetFiles(path, searchPattern);
						foreach (string file in fileNames)
						{
							//D:\\EQ\\E3_ROF2_MQ2Next\\Config\\e3 Macro Inis\\Rekken_Lazarus_pubsubport.txt
							Int32 currentIndex = file.LastIndexOf(@"\") + 1;
							Int32 indexOfUnderline = file.IndexOf('_', currentIndex);
							string name = file.Substring(currentIndex, indexOfUnderline - currentIndex);

							if (!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
							{
								NetMQServer.SharedDataClient.RegisterUser(name, path);
							}
						}

					}
					catch (Exception ex)
                    {
                        _log.WriteDelayed($"Auto Reg user eror for path:{path} message:{ex.ToString()}"); ;
						System.Threading.Thread.Sleep(1000);
					}
				}
     			System.Threading.Thread.Sleep(500);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">name of user</param>
        /// <param name="charBuffKeyName">name of the character to use, could be pet or user</param>
        /// <param name="topicKey">topic key to use</param>
        private void UpdateBuffInfoUserInfo(string name, string charBuffKeyName, string topicKey, Dictionary<string, CharacterBuffs> buffCollection)
        {
            var userTopics = NetMQServer.SharedDataClient.TopicUpdates[name];
            lock (userTopics[topicKey])
            {

                //we don't have it in our memeory, so lets add it
                if (!buffCollection.ContainsKey(charBuffKeyName))
                {
                    var buffInfo = CharacterBuffs.Aquire();
                    e3util.BuffInfoToDictonary(userTopics[topicKey].Data, buffInfo.BuffDurations);
                    buffInfo.LastUpdate = userTopics[topicKey].LastUpdate;
                    buffCollection.Add(charBuffKeyName, buffInfo);
                }
                //do we have updated information that is newer than what we already have?
                if (userTopics[topicKey].LastUpdate > buffCollection[charBuffKeyName].LastUpdate)
                {
                    //new info, lets update!
                    var buffInfo = buffCollection[charBuffKeyName];
                    e3util.BuffInfoToDictonary(userTopics[topicKey].Data, buffInfo.BuffDurations);
                    buffInfo.LastUpdate = userTopics[topicKey].LastUpdate;
                    
                }
            }
        }

        private int DebuffCounterFunction(string name,string key, Dictionary<string, SharedNumericDataInt32> collection)
		{
            //register the user to get their buff data if its not already there
			if (!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
			{
				//don't have the data yet
				return 0; //dunno just say 0
				
			}
			var userTopics = NetMQServer.SharedDataClient.TopicUpdates[name];
			//check to see if it has been filled out yet.
			string keyToUse = key;
			if (!userTopics.ContainsKey(keyToUse))
			{
				//don't have the data yet kick out and assume everything is ok.
				return 0;//dunno just say 0
			}
			var entry = userTopics[keyToUse];
			if (!collection.ContainsKey(name))
			{
				collection.Add(name, new SharedNumericDataInt32 { Data = 0 });
			}
			var sharedInfo = collection[name];
            lock(entry)
            {
				if (entry.LastUpdate > sharedInfo.LastUpdate)
				{
					if (Int32.TryParse(entry.Data, out var result))
					{

						sharedInfo.Data = result;
						sharedInfo.LastUpdate = entry.LastUpdate;
					}
				}
			}
			
			return sharedInfo.Data;
		}

		Dictionary<string, SharedNumericDataInt32> _debuffCurseCounterCollection = new Dictionary<string, SharedNumericDataInt32>();
		public int BaseCursedCounters(string name)
        {
            return DebuffCounterFunction(name, "${Me.CountersCurse}", _debuffCurseCounterCollection);
		}

		Dictionary<string, SharedNumericDataInt32> _debuffTotalCounterCollection = new Dictionary<string, SharedNumericDataInt32>();
		public int BaseDebuffCounters(string name)
        {
			return DebuffCounterFunction(name, "${Me.TotalCounters}", _debuffTotalCounterCollection);
		}

		Dictionary<string, SharedNumericDataInt32> _debuffDiseaseCounterCollection = new Dictionary<string, SharedNumericDataInt32>();
		public int BaseDiseasedCounters(string name)
        {
			return DebuffCounterFunction(name, "${Me.CountersDisease}", _debuffDiseaseCounterCollection);

		}

		Dictionary<string, SharedNumericDataInt32> _debuffPoisonCounterCollection = new Dictionary<string, SharedNumericDataInt32>();
		public int BasePoisonedCounters(string name)
        {
			return DebuffCounterFunction(name, "${Me.CountersPoison}", _debuffPoisonCounterCollection);

		}
		Dictionary<string, SharedNumericDataInt32> _debuffCorruptedCounterCollection = new Dictionary<string, SharedNumericDataInt32>();
		public int BaseCorruptedCounters(string name)
		{
			return DebuffCounterFunction(name, "${Me.CountersCorrupted}", _debuffCorruptedCounterCollection);

		}
		private Int32 _botsConnectedCount = 0;
		private List<string> _botsConnectedCache = new List<string>();
		public List<string> BotsConnected()
        {
			if(NetMQServer.SharedDataClient.TopicUpdates.Keys.Count!=_botsConnectedCount)
			{
				//need to udpate
				_botsConnectedCount = NetMQServer.SharedDataClient.TopicUpdates.Keys.Count;
				_botsConnectedCache = NetMQServer.SharedDataClient.TopicUpdates.Keys.ToList();
			}
			return _botsConnectedCache;
		}

        public void Broadcast(string message, bool noparse = false)
        {
			//have to parse out all the MQ macro information
			if (!noparse)
			{
				message = MQ.Query<string>(message);
			}
			PubServer.AddTopicMessage("BroadCastMessage", $"{E3.CurrentName}:{message}");
		}
		public void BroadcastZone(string message, bool noparse = false)
		{
			//have to parse out all the MQ macro information
			if (!noparse)
			{
				message = MQ.Query<string>(message);
			}
			PubServer.AddTopicMessage("BroadCastMessageZone", $"{E3.CurrentName}:{message}");
			//	MQ.Write($"\ar<\ay{E3.CurrentName}\ar> \aw{message}");
		}
		public void BroadcastCommand(string command, bool noparse = false, CommandMatch match = null)
        {
			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
                command = _stringBuilder.ToString();
			}
            if(!noparse)
            {
				command = MQ.Query<string>(command);
			}

			PubServer.AddTopicMessage("OnCommand-AllExceptMe", $"{E3.CurrentName}:{noparse}:{command}");
			MQ.Write($"\ap{E3.CurrentName} => \ayAll: \ag{command}");
		}
		public void BroadcastCommandAll(string command, bool noparse = false, CommandMatch match = null)
		{
			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-All", $"{E3.CurrentName}:{noparse}:{command}");
		}
		public void BroadcastCommandRaid(string command, bool noparse = false, CommandMatch match = null)
		{
			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-Raid", $"{E3.CurrentName}:{noparse}:{command}");
		}
		public void BroadcastCommandRaidNotMe(string command, bool noparse = false, CommandMatch match = null)
		{
			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-RaidNotMe", $"{E3.CurrentName}:{noparse}:{command}");
			MQ.Write($"\ap{E3.CurrentName} => \ayRaid All: \ag{command}");
		}

		public void BroadcastCommandRaidZone(string command, CommandMatch match = null, bool noparse = false)
		{	

			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-RaidZone", $"{E3.CurrentName}:{noparse}:{command}");

		}
		public void BroadcastCommandToRaidNotMeZone(string command, CommandMatch match = null, bool noparse = false)
		{
			

			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-RaidZoneNotMe", $"{E3.CurrentName}:{noparse}:{command}");

		}
		public void BroadcastCommandAllZone(string command, bool noparse = false, CommandMatch match = null)
		{
			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-AllZone", $"{E3.CurrentName}:{noparse}:{command}");
		}
		public void BroadcastCommandAllZoneNotMe(string command, bool noparse = false, CommandMatch match = null)
		{
			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-AllExceptMeZone", $"{E3.CurrentName}:{noparse}:{command}");
			MQ.Write($"\ap{E3.CurrentName} => \ayGroup All: \ag{command}");
		}
		public void BroadcastCommandToGroup(string command, CommandMatch match = null, bool noparse = false)
        {
			bool hasAllFlag = false;

			if (match != null)
			{
				hasAllFlag = match.hasAllFlag;
			}
			if (GlobalAllEnabled)
			{
				hasAllFlag = GlobalAllEnabled;
			}

            if(hasAllFlag)
            {
                BroadcastCommand(command,noparse,match);
                return;
            }

			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-Group", $"{E3.CurrentName}:{noparse}:{command}");
			MQ.Write($"\ap{E3.CurrentName} => \ayGroup: \ag{command}");
		}
		public void BroadcastCommandToGroupZone(string command, CommandMatch match = null, bool noparse = false)
		{
			bool hasAllFlag = false;

			if (match != null)
			{
				hasAllFlag = match.hasAllFlag;
			}
			if (GlobalAllEnabled)
			{
				hasAllFlag = GlobalAllEnabled;
			}

			if (hasAllFlag)
			{
				BroadcastCommand(command, noparse, match);
				return;
			}

			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-GroupZone", $"{E3.CurrentName}:{noparse}:{command}");
			MQ.Write($"\ap{E3.CurrentName} => \ayGroup Zone : \ag{command}");
		}
		public void BroadcastCommandToGroupAll(string command, CommandMatch match = null, bool noparse = false)
		{
			bool hasAllFlag = false;

			if (match != null)
			{
				hasAllFlag = match.hasAllFlag;
			}
			if (GlobalAllEnabled)
			{
				hasAllFlag = GlobalAllEnabled;
			}

			if (hasAllFlag)
			{
				BroadcastCommand(command, noparse, match);
				return;
			}

			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-GroupAll", $"{E3.CurrentName}:{noparse}:{command}");
		}
		public void BroadcastCommandToGroupAllZone(string command, CommandMatch match = null, bool noparse = false)
		{
			bool hasAllFlag = false;

			if (match != null)
			{
				hasAllFlag = match.hasAllFlag;
			}
			if (GlobalAllEnabled)
			{
				hasAllFlag = GlobalAllEnabled;
			}

			if (hasAllFlag)
			{
				BroadcastCommand(command, noparse, match);
				return;
			}

			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_stringBuilder.Clear();
				_stringBuilder.Append($"{command}");
				foreach (var filter in match.filters)
				{
					_stringBuilder.Append($" \"{filter}\"");
				}
				command = _stringBuilder.ToString();
			}
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage("OnCommand-GroupAllZone", $"{E3.CurrentName}:{noparse}:{command}");
			
		}
		public void BroadcastCommandToChannel(string channel, string command, bool noparse = false)
		{
			if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
			PubServer.AddTopicMessage($"${{DataChannel.{channel}}}", $"{E3.CurrentName}:{false}:{command}");
			MQ.Write($"\ap{E3.CurrentName} => \ay{channel} : \ag{command}");

		}
		public void BroadcastCommandToPerson(string person, string command, bool noparse = false)
		{
            person = e3util.FirstCharToUpper(person);
            if (!noparse)
			{
				command = MQ.Query<string>(command);
			}
     		PubServer.AddTopicMessage("OnCommand-" + person, $"{E3.CurrentName}:{false}:{command}");
			MQ.Write($"\ap{E3.CurrentName} => \ay{person} : \ag{command}");

		}
		List<int> _buffListReturnValue = new List<int>();

        public List<int> BuffList(string name)
        {
            _buffListReturnValue.Clear();
			//register the user to get their buff data if its not already there
			if (!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
			{
                //couldn't register, no file avilable assume they are not online yet
                return _buffListReturnValue;
               
            }
            var userTopics = NetMQServer.SharedDataClient.TopicUpdates[name];
            //check to see if it has been filled out yet.
            string topicKey = "${Me.BuffInfo}";
            if (!userTopics.ContainsKey(topicKey))
            {
                //don't have the data yet kick out with empty list as we simply don't know.
                return _buffListReturnValue;
            }
            //we have the data, lets check for updates
            //the double name is because the 2nd name could be the pet name! its called in PetBuffList
            UpdateBuffInfoUserInfo(name, name, topicKey, _characterBuffs);
            //done with updates, now lets check the data.
            _buffListReturnValue.AddRange(_characterBuffs[name].BuffDurations.Keys);

            return _buffListReturnValue;

        }

        public bool HasShortBuff(string name, int buffid)
        {
            _buffListReturnValue.Clear();
            //register the user to get their buff data if its not already there
            if (!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
            {
                //don't have data yet
                return false;
                
            }
            var userTopics = NetMQServer.SharedDataClient.TopicUpdates[name];
            //check to see if it has been filled out yet.
            string keyToUse = "${Me.BuffInfo}";
            if (!userTopics.ContainsKey(keyToUse))
            {
                //don't have the data yet kick out and assume everything is ok.
                return false;
            }
            //we have the data, lets check on it. 
            UpdateBuffInfoUserInfo(name, name, keyToUse, _characterBuffs);
            //done with updates, now lets check the data.
            return _characterBuffs[name].BuffDurations.ContainsKey(buffid);

        }
        HashSet<string> _isMyBotCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public bool IsMyBot(string name)
        {
            if(NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
            {
                return true;
            }
            return false;
	    }

        Dictionary<string, SharedNumericDataInt32> _pctHealthCollection = new Dictionary<string, SharedNumericDataInt32>();
		public int PctHealth(string name)
		{
			//register the user to get their buff data if its not already there
			if (!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
			{
				return 100; //dunno just say full health
			}
			var userTopics = NetMQServer.SharedDataClient.TopicUpdates[name];
			//check to see if it has been filled out yet.
			string keyToUse = "${Me.PctHPs}";
			if (!userTopics.ContainsKey(keyToUse))
			{
				//don't have the data yet kick out and assume everything is ok.
				return 100;//dunno just say full health
			}
            var entry = userTopics[keyToUse];
            if(!_pctHealthCollection.ContainsKey(name))
            {
                _pctHealthCollection.Add(name, new SharedNumericDataInt32 { Data=100});
			}
            var sharedInfo = _pctHealthCollection[name];
            lock(entry)
            {
				if (entry.LastUpdate > sharedInfo.LastUpdate)
				{
					if (Int32.TryParse(entry.Data, out var result))
					{

						sharedInfo.Data = result;
						sharedInfo.LastUpdate = entry.LastUpdate;
					}
				}
			}
			
            return sharedInfo.Data;
		}
        public string Query(string name,string query)
        {
			//register the user to get their buff data if its not already there
			if (!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
			{
				return "NULL"; //dunno
			}
			var userTopics = NetMQServer.SharedDataClient.TopicUpdates[name];
			//check to see if it has been filled out yet.
			string keyToUse = query;
			if (!userTopics.ContainsKey(keyToUse))
			{
				return "NULL"; //dunno
			}
			var entry = userTopics[keyToUse];
			lock (entry)
			{
                return entry.Data;
			}
		}

		public List<int> PetBuffList(string name)
		{
			_buffListReturnValue.Clear();
			//register the user to get their buff data if its not already there
			if (!NetMQServer.SharedDataClient.TopicUpdates.ContainsKey(name))
			{  //no data assume not online yet.
				return _buffListReturnValue;
			}
			var userTopics = NetMQServer.SharedDataClient.TopicUpdates[name];
			//check to see if it has been filled out yet.
			string topicKey = "${Me.PetBuffInfo}";
			if (!userTopics.ContainsKey(topicKey))
			{
				//don't have the data yet kick out with empty list as we simply don't know.
				return _buffListReturnValue;
			}
			//we have the data, lets check for updates
			//the double name is because the 2nd name could be the pet name! its called in PetBuffList
			UpdateBuffInfoUserInfo(name, name, topicKey,_petBuffs);
			//done with updates, now lets check the data.
			_buffListReturnValue.AddRange(_petBuffs[name].BuffDurations.Keys);

			return _buffListReturnValue;
		}

		public void Trade(string name)
		{
			MQ.Cmd("/notify TradeWnd TRDW_Trade_Button leftmouseup", 250);
            string command = "/notify TradeWnd TRDW_Trade_Button leftmouseup";
			PubServer.AddTopicMessage("OnCommand-" + name, $"{E3.CurrentName}:{false}:{command}");
		}
        class SharedNumericDataInt32
        {
            public Int32 Data { get; set; }
            public Int64 LastUpdate { get; set; }
        }

	}
	/*
	public class Bots : IBots
	{
		public static string _lastSuccesfulCast = String.Empty;
		public static Logging _log = E3.Log;
		private static IMQ MQ = E3.MQ;

		private string _connectedBotsString = string.Empty;
		private List<string> _connectedBots = new List<string>();
		private static Dictionary<string, List<Int32>> _buffListCollection = new Dictionary<string, List<int>>();
		private static Dictionary<string, Int64> _buffListCollectionTimeStamps = new Dictionary<string, long>();
		private static Int64 _nextBuffCheck = 0;
		private static Int64 _nextBuffRefreshTimeInterval = 1000;

		private static Dictionary<string, List<Int32>> _petBuffListCollection = new Dictionary<string, List<int>>();
		private static Dictionary<string, Int64> _petBuffListCollectionTimeStamps = new Dictionary<string, long>();
		private static Int64 _nextPetBuffCheck = 0;
		private static Int64 _nextPetBuffRefreshTimeInterval = 1000;
		private static bool GlobalAllEnabled = false;

		public Bots()
		{
			EventProcessor.RegisterCommand("/e3GlobalBroadcast", (x) =>
			{
				GlobalAllEnabled = !GlobalAllEnabled;
				Broadcast($"\agSetting Global Broadcast to {GlobalAllEnabled}");

			});
		}

		private static string GetGroupCommand()
		{
			if (E3.GeneralSettings.General_BroadCast_Default == Settings.DefaultBroadcast.All)
			{
				return "/bca";
			}
			else if (E3.GeneralSettings.General_BroadCast_Default == Settings.DefaultBroadcast.AllInZoneOrRaid)
			{
				return "/bca";
			}

			return "/bcg";
		}
		private static StringBuilder _strinbBuilder = new StringBuilder();
		public void BroadcastCommandToGroup(string query, CommandMatch match = null, bool noparse = false)
		{

			bool hasAllFlag = false;

			if (match != null)
			{
				hasAllFlag = match.hasAllFlag;
			}
			if (GlobalAllEnabled)
			{
				hasAllFlag = GlobalAllEnabled;
			}

			string noparseCommand = string.Empty;
			if (noparse)
			{
				//space after is required
				noparseCommand = "/noparse ";
			}

			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_strinbBuilder.Clear();
				if (hasAllFlag)
				{
					_strinbBuilder.Append($"{noparseCommand}/bca /{query}");
				}
				else
				{
					_strinbBuilder.Append($"{GetGroupCommand()} /{query}");
				}

				foreach (var filter in match.filters)
				{
					_strinbBuilder.Append($" \"{filter}\"");
				}
				MQ.Cmd(_strinbBuilder.ToString());
			}
			else
			{
				if (hasAllFlag)
				{
					MQ.Cmd($"{noparseCommand}/bca /{query}");
				}
				else
				{
					MQ.Cmd($"{noparseCommand}{GetGroupCommand()} /{query}");
				}

			}
		}
		public void BroadcastCommandToPerson(string person, string command, bool noparse = false)
		{
			person = e3util.FirstCharToUpper(person);
			MQ.Cmd($"/bct {person} /{command}");
		}
		public void BroadcastCommand(string command, bool noparse = false, CommandMatch match = null)
		{
			if (match != null && match.filters.Count > 0)
			{
				//need to pass over the filters if they exist
				_strinbBuilder.Clear();
				_strinbBuilder.Append($"/bca /{command}");
				foreach (var filter in match.filters)
				{
					_strinbBuilder.Append($" \"{filter}\"");
				}
				if (noparse)
				{
					MQ.Cmd($"/noparse {_strinbBuilder.ToString()}");
				}
				else
				{
					MQ.Cmd(_strinbBuilder.ToString());
				}
			}
			else if (noparse)
			{
				MQ.Cmd($"/noparse /bca /{command}");
			}
			else
			{
				MQ.Cmd($"/bca /{command}");
			}
		}
		public Boolean InZone(string name)
		{
			return MQ.Query<bool>($"${{NetBots[{name}].InZone}}");

		}
		public Int32 PctHealth(string name)
		{
			return MQ.Query<Int32>($"${{NetBots[{name}].PctHPs}}");
		}

		public List<string> BotsConnected()
		{


			string currentConnectedBots = MQ.Query<string>("${NetBots.Client}");

			if (currentConnectedBots == "NULL" || string.IsNullOrEmpty(currentConnectedBots))
			{
				//error?
				if (_connectedBots.Count > 0)
				{
					_connectedBots = new List<string>();

				}
				return _connectedBots;
			}

			if (currentConnectedBots == _connectedBotsString)
			{
				//no chnage, return the current list
				return _connectedBots;
			}
			else
			{
				//its different, update
				_connectedBots = e3util.StringsToList(currentConnectedBots, ' ');
				return _connectedBots;
			}
		}

		public bool HasShortBuff(string name, Int32 buffid)
		{
			return BuffList(name).Contains(buffid);
		}

		public List<int> BuffList(string name)
		{

			List<Int32> buffList;
			bool alreadyExisted = true;
			if (!_buffListCollection.TryGetValue(name, out buffList))
			{
				alreadyExisted = false;
				buffList = new List<int>();
				_buffListCollection.Add(name, buffList);
				_buffListCollectionTimeStamps.Add(name, 0);
			}

			if (!e3util.ShouldCheck(ref _nextBuffCheck, _nextBuffRefreshTimeInterval) && alreadyExisted) return _buffListCollection[name];

			//refresh all lists of all people

			foreach (var kvp in _buffListCollection)
			{
				if (kvp.Key.Contains("\""))
				{
					//ignore pets with quotes. 
					continue;
				}
				string listString = string.Empty;

				listString = MQ.Query<string>($"${{NetBots[{kvp.Key}].Buff}}");
				_buffListCollection[kvp.Key].Clear();
				if (listString != "NULL")
				{
					e3util.StringsToNumbers(listString, ' ', _buffListCollection[kvp.Key]);
					listString = MQ.Query<string>($"${{NetBots[{kvp.Key}].ShortBuff}}");
					if (listString != "NULL")
					{
						e3util.StringsToNumbers(listString, ' ', _buffListCollection[kvp.Key]);
					}
				}

			}
			return _buffListCollection[name];

		}
		public List<int> PetBuffList(string name)
		{

			List<Int32> buffList;
			bool alreadyExisted = true;
			if (!_petBuffListCollection.TryGetValue(name, out buffList))
			{
				alreadyExisted = false;
				buffList = new List<int>();
				_petBuffListCollection.Add(name, buffList);
				_petBuffListCollectionTimeStamps.Add(name, 0);
			}

			if (!e3util.ShouldCheck(ref _nextPetBuffCheck, _nextPetBuffRefreshTimeInterval) && alreadyExisted) return _petBuffListCollection[name];

			//refresh all lists of all people

			foreach (var kvp in _petBuffListCollection)
			{
				if (kvp.Key.Contains("\""))
				{
					//ignore pets with quotes. 
					continue;
				}
				string listString = string.Empty;

				listString = MQ.Query<string>($"${{NetBots[{kvp.Key}].PetBuff}}");
				_petBuffListCollection[kvp.Key].Clear();
				if (listString != "NULL")
				{
					e3util.StringsToNumbers(listString, ' ', _petBuffListCollection[kvp.Key]);
				}

			}
			return _petBuffListCollection[name];

		}

		public void Broadcast(string message, bool noparse = false)
		{
			if (noparse)
			{
				MQ.Cmd($"/noparse /bc {message}");
			}
			else
			{
				MQ.Cmd($"/bc {message}");
			}

		}

		//NOTE* these are the counters on the original spell , not the CURRENT counter value.
		//can't currently get counter totals on EMu due to a but in MQ
		//per Dannuic
		//There are more spells (especially on emu) that can be cured that don't use counters, you should be checking SPA (which is what ${Me.Diseased} does)

		public int BaseDebuffCounters(string name)
		{
			Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Counters}}");
			return counters;
		}

		public int BaseDiseasedCounters(string name)
		{
			Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Diseased}}");
			return counters;
		}

		public int BasePoisonedCounters(string name)
		{
			Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Poisoned}}");
			return counters;
		}

		public int BaseCursedCounters(string name)
		{
			Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Cursed}}");
			return counters;
		}

		public bool IsMyBot(string name)
		{
			return BotsConnected().Contains(name);
		}

		public void Trade(string name)
		{
			MQ.Cmd("/notify TradeWnd TRDW_Trade_Button leftmouseup", 250);
			MQ.Cmd($"/bct {name} //notify TradeWnd TRDW_Trade_Button leftmouseup");
		}

		public int BaseCorruptedCounters(string name)
		{
			throw new NotImplementedException();
		}
	}

	public class DanBots : IBots
    {
        private string _connectedBotsString = string.Empty;
        private List<string> _connectedBots = new List<string>();

        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static Dictionary<string, List<Int32>> _buffListCollection = new Dictionary<string, List<int>>();
        private static Dictionary<string, Int64> _buffListCollectionTimeStamps = new Dictionary<string, long>();
        private static Int64 _nextBuffCheck = 0;
        private static Int64 _nextBuffRefreshTimeInterval = 1000;

        private static Dictionary<string, List<Int32>> _petBuffListCollection = new Dictionary<string, List<int>>();
        private static Dictionary<string, Int64> _petBuffListCollectionTimeStamps = new Dictionary<string, long>();
        private static Int64 _nextPetBuffCheck = 0;
        private static Int64 _nextPetBuffRefreshTimeInterval = 1000;

        private static StringBuilder _strinbBuilder = new StringBuilder();
        private static HashSet<string> _registeredObservations = new HashSet<string>();
         private static string GetGroupCommand()
        {
            if (E3.GeneralSettings.General_BroadCast_Default == Settings.DefaultBroadcast.All)
            {
                return "/dge";
            }
            else if (E3.GeneralSettings.General_BroadCast_Default == Settings.DefaultBroadcast.AllInZoneOrRaid)
            {
                return "/dgre";
            }

            return "/dgge";
        }
        public DanBots()
        {
            //set the observe delay to be a bit faster
            MQ.Cmd("/dnet observedelay 200");
            EventProcessor.RegisterCommand("/e3danfix", (x) =>
            {
                Broadcast("\agRe-Registering all observers");
                ReRegisterObservations();
            });

        }
        public List<string> BotsConnected()
        {
            //project lazarus_alara|project lazarus_hordester|project lazarus_kusara|project lazarus_rekken|project lazarus_shadowvine|project lazarus_yona|
             string playerList = MQ.Query<string>("${DanNet.Peers}");

            if(playerList==_connectedBotsString)
            {
                //no change just return what we have
                return _connectedBots;
            }
            _connectedBotsString = playerList;
            if (playerList.EndsWith("|"))
            {
                playerList = playerList.Substring(0, playerList.Length - 1);
            }
            if (playerList != "NULL")
            {
                _connectedBots.Clear();
                List<string> tmpList = e3util.StringsToList(playerList, '|');

                foreach (var value in tmpList)
                {
                    //last index of 
                    Int32 indexOfUnderScore = value.LastIndexOf('_');
                    if (indexOfUnderScore == -1)
                    {
                        string newName = e3util.FirstCharToUpper(value);
						_connectedBots.Add(newName);
						continue;
                    }
                    if(indexOfUnderScore+1==value.Length)
                    {
                        //nothing after the underscore, skip
                        continue;
                    }
                    string newStr = value.Substring(indexOfUnderScore+1, value.Length - indexOfUnderScore-1);
                    newStr= char.ToUpper(newStr[0]) + newStr.Substring(1);
                    _connectedBots.Add(newStr);
                }
            }
            return _connectedBots;
        }

        public void Broadcast(string message, bool noparse = false)
        {
            if(noparse)
            {
				MQ.Cmd($"/dgt {message}");
			}
            else
            {
				MQ.Cmd($"/noparse /dgt {message}");
			}
           
        }

        public void BroadcastCommand(string command, bool noparse = false, CommandMatch match = null)
        {

            if (match != null && match.filters.Count > 0)
            {
                //need to pass over the filters if they exist
                _strinbBuilder.Clear();
                _strinbBuilder.Append($"{GetGroupCommand()} {command}");
                foreach (var filter in match.filters)
                {
                    _strinbBuilder.Append($" \"{filter}\"");
                }
                if (noparse)
                {
                    MQ.Cmd($"/noparse {_strinbBuilder.ToString()}");
                }
                else
                {
                    MQ.Cmd(_strinbBuilder.ToString());
                }
            }
            else if (noparse)
            {
                MQ.Cmd($"/noparse {GetGroupCommand()} {command}");
            }
            else
            {
                MQ.Cmd($"{GetGroupCommand()} {command}");
            }

        }

        public void BroadcastCommandToGroup(string query, CommandMatch match = null,bool noparse =false)
        {
            string noparseCommand = string.Empty;
            if (noparse)
            {
                //space after is required
                noparseCommand = "/noparse ";
            }
            if (match != null && match.filters.Count > 0)
            {
                //need to pass over the filters if they exist
                _strinbBuilder.Clear();
                _strinbBuilder.Append($"{noparseCommand}{GetGroupCommand()} {query}");
                foreach (var filter in match.filters)
                {
                    _strinbBuilder.Append($" \"{filter}\"");
                }
                MQ.Cmd(_strinbBuilder.ToString());
            }
            else
            {
                MQ.Cmd($"{noparseCommand}{GetGroupCommand()} {query}");
            }
            
        }

        public void BroadcastCommandToPerson(string person, string command,bool noparse = false)
        {
            MQ.Cmd($"/dex {person} {command}");
        }

        private void RegisterBuffSlots(string name)
        {
            for(Int32 i=1;i<= e3util.MaxBuffSlots; i++)
            {
                RegisterObserve(name, $"Me.Buff[{i}].Spell.ID");
            }
            for (Int32 i = 1; i <= e3util.MaxSongSlots; i++)
            {
                RegisterObserve(name, $"Me.Song[{i}].Spell.ID");
            }
        }
        public List<int> BuffList(string name)
        {

			List<Int32> buffList;
			bool alreadyExisted = true;
			if (!_buffListCollection.TryGetValue(name, out buffList))
			{
				alreadyExisted = false;
				buffList = new List<int>();
				_buffListCollection.Add(name, buffList);
				_buffListCollectionTimeStamps.Add(name, 0);
			}

			if (!e3util.ShouldCheck(ref _nextBuffCheck, _nextBuffRefreshTimeInterval) && alreadyExisted) return _buffListCollection[name];

			//refresh all lists of all people
			foreach (var kvp in _buffListCollection)
			{
				if (!_buffListObservers.Contains(kvp.Key))
				{
					RegisterObserve(kvp.Key, "MonoBuffInfo.Buffs");
					RegisterObserve(kvp.Key, "MonoBuffInfo.ShortBuffs");
					_buffListObservers.Add(kvp.Key);
					MQ.Delay(200); //need time for the observer to startup
				}
				if (kvp.Key.Contains("\""))
				{
					//ignore pets with quotes. 
					continue;
				}
				string listString = MQ.Query<String>($"${{DanNet[{kvp.Key}].O[\"MonoBuffInfo.Buffs\"]}}");
				_buffListCollection[kvp.Key].Clear();
				if (listString!="NULL" && !String.IsNullOrWhiteSpace(listString))
				{
					e3util.StringsToNumbers(listString, ':', _buffListCollection[kvp.Key]);
					listString = MQ.Query<String>($"${{DanNet[{kvp.Key}].O[\"MonoBuffInfo.ShortBuffs\"]}}");
					if (listString != "NULL" && !String.IsNullOrWhiteSpace(listString))
					{
						e3util.StringsToNumbers(listString, ':', _buffListCollection[kvp.Key]);
					}
				}
			}
			return _buffListCollection[name];
		}
        public List<int> PetBuffList(string name)
        {
            List<Int32> buffList;
            bool alreadyExisted = true;
            if (!_petBuffListCollection.TryGetValue(name, out buffList))
            {
                alreadyExisted = false;
                buffList = new List<int>();
                _petBuffListCollection.Add(name, buffList);
                _petBuffListCollectionTimeStamps.Add(name, 0);
            }

            if (!e3util.ShouldCheck(ref _nextPetBuffCheck, _nextPetBuffRefreshTimeInterval) && alreadyExisted) return _petBuffListCollection[name];

            //refresh all lists of all people

            foreach (var kvp in _petBuffListCollection)
            {
				if (!_petBuffListObservers.Contains(kvp.Key))
				{
					RegisterObserve(kvp.Key, "MonoBuffInfo.PetBuffs");
					_petBuffListObservers.Add(kvp.Key);
					MQ.Delay(200); //need time for the observer to startup
				}
				if (kvp.Key.Contains("\""))
                {
                    //ignore pets with quotes. 
                    continue;
                }
				string listString = MQ.Query<String>($"${{DanNet[{kvp.Key}].O[\"MonoBuffInfo.PetBuffs\"]}}");
				if (listString != "NULL" && !String.IsNullOrWhiteSpace(listString))
				{
					e3util.StringsToNumbers(listString, ':', _petBuffListCollection[kvp.Key]);
				}
            }
            return _petBuffListCollection[name];
            
        }
        private HashSet<String> _curseCountersObservers = new HashSet<string>();

		//NOTE* these are the counters on the original spell , not the CURRENT counter value.
		//can't currently get counter totals on EMu due to a but in MQ
        //per Dannuic
		//There are more spells (especially on emu) that can be cured that don't use counters, you should be checking SPA (which is what ${Me.Diseased} does)
		public int BaseCursedCounters(string name)
        {
            if (!_curseCountersObservers.Contains(name))
            {
                RegisterObserve(name, "Me.CountersCurse");
                _curseCountersObservers.Add(name);
                MQ.Delay(100);
            }
            Int32 counters = MQ.Query<Int32>($"${{DanNet[{name}].O[\"Me.CountersCurse\"]}}");
            return counters;
     
        }
        private HashSet<String> _debuffCountersObservers = new HashSet<string>();
        public int BaseDebuffCounters(string name)
        {
            if (!_debuffCountersObservers.Contains(name))
            {
                RegisterObserve(name, "Me.TotalCounters");
                _debuffCountersObservers.Add(name);
                MQ.Delay(100);
            }
            Int32 counters = MQ.Query<Int32>($"${{DanNet[{name}].O[\"Me.TotalCounters\"]}}");
            return counters;
           
        }
        private HashSet<String> _diseaseCountersObservers = new HashSet<string>();

        public int BaseDiseasedCounters(string name)
        {
            if (!_diseaseCountersObservers.Contains(name))
            {
                RegisterObserve(name, "Me.CountersDisease");
                _diseaseCountersObservers.Add(name);
                MQ.Delay(100);
            }
            Int32 counters = MQ.Query<Int32>($"${{DanNet[{name}].O[\"Me.CountersDisease\"]}}");
            return counters;
      
        }
        private HashSet<String> _poisonCountersObservers = new HashSet<string>();

        public int BasePoisonedCounters(string name)
        {
            if (!_poisonCountersObservers.Contains(name))
            {
                RegisterObserve(name, "Me.CountersPoison");
                _poisonCountersObservers.Add(name);
                MQ.Delay(100);
            }
            Int32 counters = MQ.Query<Int32>($"${{DanNet[{name}].O[\"Me.CountersPoison\"]}}");
            return counters;
        }
        public bool HasShortBuff(string name, Int32 buffid)
        {
            return BuffList(name).Contains(buffid);
        }
        private HashSet<String> _inZoneObservers = new HashSet<string>();

        public Boolean InZone(string name)
        {
            MQ.Cmd($"/dquery {name} -q Zone.ID",100);
            Int32 zoneid = MQ.Query<Int32>("${DanNet.Q}");

            if (zoneid==Zoning.CurrentZone.Id)
            {
                return true;
            }
            return false;
        }
        private HashSet<String> _pctHealthObservers = new HashSet<string>();
        public Int32 PctHealth(string name)
        {
            if(!_pctHealthObservers.Contains(name))
            {
                RegisterObserve(name, "Me.PctHPs");
                _pctHealthObservers.Add(name);
                MQ.Delay(100);
            }
            Int32 pctHealth = MQ.Query<Int32>($"${{DanNet[{name}].O[\"Me.PctHPs\"]}}");
            return pctHealth;
        }

		private HashSet<String> _buffListObservers = new HashSet<string>();
		private HashSet<String> _petBuffListObservers = new HashSet<string>();
		private void RegisterObserve(string name, string query)
        {
            string observeCommand = $"/dobserve {name} -q {query}";
            MQ.Cmd(observeCommand);
            if(!_registeredObservations.Contains(observeCommand))
            {
                _registeredObservations.Add(observeCommand);
            }
        }
        private void ReRegisterObservations()
        {
            foreach(var command in _registeredObservations)
            {
                MQ.Cmd(command);
            }
        }

        public bool IsMyBot(string name)
        {
            return BotsConnected().Contains(name);
        }

        public void Trade(string name)
        {
            MQ.Cmd("/notify TradeWnd TRDW_Trade_Button leftmouseup", 250);
            MQ.Cmd($"/dex {name} /notify TradeWnd TRDW_Trade_Button leftmouseup");
        }

		public int BaseCorruptedCounters(string name)
		{
			throw new NotImplementedException();
		}
	}
	*/
}

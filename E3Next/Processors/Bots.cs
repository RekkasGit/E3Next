using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static MonoCore.EventProcessor;

namespace E3Core.Processors
{
    public interface IBots
    {
       
        Boolean InZone(string Name);
        Int32 PctHealth(string name);
        List<string> BotsConnected();
        Boolean HasShortBuff(string name, Int32 buffid);
        void BroadcastCommand(string command, bool noparse = false, CommandMatch match = null);
        void BroadcastCommandToGroup(string command, CommandMatch match=null, bool noparse = false);
        void BroadcastCommandToPerson(string person, string command);
        void Broadcast(string message);
        List<Int32> BuffList(string name);
        List<Int32> PetBuffList(string name);
        Int32 BaseDebuffCounters(string name);
        Int32 BaseDiseasedCounters(string name);
        Int32 BasePoisonedCounters(string name);
        Int32 BaseCursedCounters(string name);
        bool IsMyBot(string name);
        void Trade(string name);
    }
    public class Bots: IBots
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
                Broadcast($"\agSetting Global Boradcast to {GlobalAllEnabled}");
				
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

            if(match!=null)
            {
                hasAllFlag = match.hasAllFlag;
            }
            if(GlobalAllEnabled)
            {
                hasAllFlag = GlobalAllEnabled;
            }

            string noparseCommand = string.Empty;
            if(noparse)
            {
                //space after is required
                noparseCommand = "/noparse ";
            }

            if(match!=null && match.filters.Count>0)
            {
                //need to pass over the filters if they exist
                _strinbBuilder.Clear();
                if(hasAllFlag)
                {
                    _strinbBuilder.Append($"{noparseCommand}/bca /{query}");
                }
                else
                {
                    _strinbBuilder.Append($"{GetGroupCommand()} /{query}");
                }
                
                foreach(var filter in match.filters)
                {
                    _strinbBuilder.Append($" \"{filter}\"");
                }
                MQ.Cmd(_strinbBuilder.ToString());
            }
            else
            {
                if(hasAllFlag)
                {
                    MQ.Cmd($"{noparseCommand}/bca /{query}");
                }
                else
                {
                    MQ.Cmd($"{noparseCommand}{GetGroupCommand()} /{query}");
                }
                
            }
        }
        public void BroadcastCommandToPerson(string person, string command)
        {
            person = e3util.FirstCharToUpper(person);
			MQ.Cmd($"/bct {person} /{command}");
        }
        public void BroadcastCommand(string command,bool noparse = false, CommandMatch match = null)
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

            if(currentConnectedBots=="NULL" || string.IsNullOrEmpty(currentConnectedBots))
            {
                //error?
                if(_connectedBots.Count>0)
                {
                    _connectedBots = new List<string>();

                }
                return _connectedBots;
            }

            if(currentConnectedBots==_connectedBotsString)
            {
                //no chnage, return the current list
                return _connectedBots;
            }
            else
            {
                //its different, update
                _connectedBots =e3util .StringsToList(currentConnectedBots, ' ');
                return _connectedBots;
            }
        }

        public bool HasShortBuff(string name, Int32 buffid)
        {
            return BuffList(name).Contains(buffid);
        }

        public  List<int> BuffList(string name)
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

            foreach(var kvp in _buffListCollection)
            {
                if(kvp.Key.Contains("\""))
                {
                    //ignore pets with quotes. 
                    continue;
                }
                string listString = string.Empty;
                
                listString=MQ.Query<string>($"${{NetBots[{kvp.Key}].Buff}}");
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

        public void Broadcast(string message)
        {
            MQ.Cmd($"/bc {message}");
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
    }

    public class DanBots : IBots
    {
        private string _connectedBotsString = string.Empty;
        private List<string> _connectedBots = new List<string>();
        private Int32 _maxBuffSlots = 37;
        private Int32 _maxSongSlots = 25;
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
                    if (indexOfUnderScore == -1) continue;
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

        public void Broadcast(string message)
        {
            MQ.Cmd($"/dgt {message}");
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

        public void BroadcastCommandToPerson(string person, string command)
        {
            MQ.Cmd($"/dex {person} {command}");
        }

        private void RegisterBuffSlots(string name)
        {
            for(Int32 i=1;i<= _maxBuffSlots; i++)
            {
                RegisterObserve(name, $"Me.Buff[{i}].Spell.ID");
            }
            for (Int32 i = 1; i <= _maxSongSlots; i++)
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

                //register this persons buff slots
                RegisterBuffSlots(name);
            }
            if (!e3util.ShouldCheck(ref _nextBuffCheck, _nextBuffRefreshTimeInterval) && alreadyExisted) return _buffListCollection[name];
            //refresh all lists of all people
            foreach (var kvp in _buffListCollection)
            {
                _buffListCollection[kvp.Key].Clear();
                for (Int32 i=1;i<= _maxBuffSlots; i++)
                {
                   Int32 spellid= MQ.Query<Int32>($"${{DanNet[{kvp.Key}].O[\"Me.Buff[{i}].Spell.ID\"]}}");
                    if(spellid>0)
                    {
                        _buffListCollection[kvp.Key].Add(spellid);
                    }
                }
                for (Int32 i = 1; i <= _maxSongSlots; i++)
                {
                    Int32 spellid = MQ.Query<Int32>($"${{DanNet[{kvp.Key}].O[\"Me.Song[{i}].Spell.ID\"]}}");
                    if (spellid > 0)
                    {
                        _buffListCollection[kvp.Key].Add(spellid);
                    }
                }
            }
            return _buffListCollection[name];   //need to register and get a buff list.
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
                for (Int32 i = 1; i <= _maxBuffSlots; i++)
                {
                    Int32 spellid = MQ.Query<Int32>($"${{DanNet[{kvp.Key}].O[\"Me.Pet.Buff[{i}].Spell.ID\"]}}");
                    if (spellid > 0)
                    {
                        _petBuffListCollection[kvp.Key].Add(spellid);
                    }
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
            MQ.Cmd($"/dquery {name} -q Zone.ID");
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
    }
}

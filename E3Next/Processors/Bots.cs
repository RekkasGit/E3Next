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
        void BroadcastCommandToGroup(string command, CommandMatch match=null);
        void BroadcastCommandToPerson(string person, string command);
        void Broadcast(string message);
        List<Int32> BuffList(string name);
        Int32 DebuffCounters(string name);
        Int32 DiseasedCounters(string name);
        Int32 PoisonedCounters(string name);
        Int32 CursedCounters(string name);
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
        private static StringBuilder _strinbBuilder = new StringBuilder();
        public void BroadcastCommandToGroup(string query, CommandMatch match = null)
        {
            if(match!=null && match.filters.Count>0)
            {
                //need to pass over the filters if they exist
                _strinbBuilder.Clear();
                _strinbBuilder.Append($"/bcg /{query}");
                foreach(var filter in match.filters)
                {
                    _strinbBuilder.Append($" \"{filter}\"");
                }
                MQ.Cmd(_strinbBuilder.ToString());
            }
            else
            {
                MQ.Cmd($"/bcg /{query}");
            }
        }
        public void BroadcastCommandToPerson(string person, string command)
        {
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

            if(currentConnectedBots=="NULL")
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
                string listString = MQ.Query<string>($"${{NetBots[{kvp.Key}].Buff}}");
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
        

        public void Broadcast(string message)
        {
            MQ.Cmd($"/bc {message}");
        }

        public int DebuffCounters(string name)
        {
            Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Counters}}");
            return counters;
        }

        public int DiseasedCounters(string name)
        {
            Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Diseased}}");
            return counters;
        }

        public int PoisonedCounters(string name)
        {
            Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Poisoned}}");
            return counters;
        }

        public int CursedCounters(string name)
        {
            Int32 counters = MQ.Query<Int32>($"${{NetBots[{name}].Cursed}}");
            return counters;
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
        private static StringBuilder _strinbBuilder = new StringBuilder();
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
            MQ.Cmd($"/dga {message}");
        }

        public void BroadcastCommand(string command, bool noparse = false, CommandMatch match = null)
        {

            if (match != null && match.filters.Count > 0)
            {
                //need to pass over the filters if they exist
                _strinbBuilder.Clear();
                _strinbBuilder.Append($"/dgae /{command}");
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
                MQ.Cmd($"/noparse /dgae /{command}");
            }
            else
            {
                MQ.Cmd($"/dgae /{command}");
            }

        }

        public void BroadcastCommandToGroup(string query, CommandMatch match = null)
        {
            if (match != null && match.filters.Count > 0)
            {
                //need to pass over the filters if they exist
                _strinbBuilder.Clear();
                _strinbBuilder.Append($"/dgge {query}");
                foreach (var filter in match.filters)
                {
                    _strinbBuilder.Append($" \"{filter}\"");
                }
                MQ.Cmd(_strinbBuilder.ToString());
            }
            else
            {
                MQ.Cmd($"/dgge {query}");
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
                MQ.Cmd($"/dobserve {name} -q Me.Buff[{i}].Spell.ID");
            }
            for (Int32 i = 1; i <= _maxSongSlots; i++)
            {
                MQ.Cmd($"/dobserve {name} -q Me.Song[{i}].Spell.ID");
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
                   Int32 spellid= MQ.Query<Int32>($"${{DanNet[{name}].O[\"Me.Buff[{i}].Spell.ID\"]}}");
                    if(spellid>0)
                    {
                        _buffListCollection[kvp.Key].Add(spellid);
                    }
                }
                for (Int32 i = 1; i <= _maxSongSlots; i++)
                {
                    Int32 spellid = MQ.Query<Int32>($"${{DanNet[{name}].O[\"Me.Song[{i}].Spell.ID\"]}}");
                    if (spellid > 0)
                    {
                        _buffListCollection[kvp.Key].Add(spellid);
                    }
                }
            }
            return _buffListCollection[name];   //need to register and get a buff list.
        }

        public int CursedCounters(string name)
        {
            MQ.Cmd("/dquery {name} -q Me.CountersCurse");
            Int32 counters = MQ.Query<Int32>("${DanNet.Q}");
            return counters;

        }

        public int DebuffCounters(string name)
        {
            MQ.Cmd("/dquery {name} -q Me.TotalCounters");
            Int32 counters = MQ.Query<Int32>("${DanNet.Q}");
            return counters;
           
        }

        public int DiseasedCounters(string name)
        {
            MQ.Cmd("/dquery {name} -q Me.CountersDisease");
            Int32 counters = MQ.Query<Int32>("${DanNet.Q}");
            return counters;
        }

        public bool HasShortBuff(string name, Int32 buffid)
        {
            return BuffList(name).Contains(buffid);
        }

        public Boolean InZone(string name)
        {
            MQ.Cmd("/dquery {name} -q Zone.ID");
            Int32 zoneid = MQ.Query<Int32>("${DanNet.Q}");

            if(zoneid==Zoning.CurrentZone.Id)
            {
                return true;
            }
            return false;

        }
        public Int32 PctHealth(string name)
        {
            return MQ.Query<Int32>($"/dquery {name} -q Me.PctHPs");
        }

        public int PoisonedCounters(string name)
        {
            MQ.Cmd("/dquery {name} -q Me.CountersPoison");
            Int32 counters = MQ.Query<Int32>("${DanNet.Q}");
            return counters;
        }

     
    }

   
}

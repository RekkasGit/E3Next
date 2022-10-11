using E3Core.Utility;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public interface IBots
    {
        Boolean InZone(string Name);
        Int32 PctHealth(string name);
        List<string> BotsConnected();
        Boolean HasShortBuff(string name, Int64 buffid);
        void BroadcastCommandToOthers(string command);
        void Broadcast(string message);
        List<Int32> BuffList(string name);


    }
    public class Bots: IBots
    {
        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;

        private string netbotConnectionString = string.Empty;
        private List<string> _connectedBots = new List<string>();
        private static Dictionary<string, List<Int32>> _buffListCollection = new Dictionary<string, List<int>>();
        private static Dictionary<string, Int64> _buffListCollectionTimeStamps = new Dictionary<string, long>();
        private static Int64 _nextBuffCheck = 0;
        private static Int64 _nextBuffRefreshTimeInterval = 1000;
        public void BroadcastCommandToOthers(string query)
        {
            MQ.Cmd($"/bcg /{query}");
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

            if(currentConnectedBots==netbotConnectionString)
            {
                //no chnage, return the current list
                return _connectedBots;
            }
            else
            {
                //its different, update
                _connectedBots = currentConnectedBots.Split(' ').ToList();
                return _connectedBots;
            }
        }

        public bool HasShortBuff(string name, Int64 buffid)
        {
            string buffidAsString = buffid.ToString();
            string buffList = MQ.Query<string>($"${{NetBots[{name}].ShortBuff}}");

            if(buffList.Contains(buffidAsString))
            {
                if(buffList.Contains(" "+buffidAsString+ " "))
                {
                    return true;
                }
                if(buffList.StartsWith(buffidAsString + " "))
                {
                    return true;
                }
                if(buffList.EndsWith(" "+buffidAsString))
                {
                    return true;
                }
            }
            return false;
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

            if (!e3util.ShouldCheck(ref _nextBuffCheck, _nextBuffRefreshTimeInterval) && alreadyExisted) return buffList;

            //refresh all lists of all people

            foreach(var kvp in _buffListCollection)
            {
                string listString = MQ.Query<string>($"${{NetBots[{kvp.Key}].Buff}}");
                _buffListCollection[kvp.Key].Clear();
                if (listString != "NULL")
                {  
                    StrignsToNumbers(listString, ' ', _buffListCollection[kvp.Key]);
                    listString = MQ.Query<string>($"${{NetBots[{kvp.Key}].ShortBuff}}");
                    if (listString != "NULL")
                    {
                        StrignsToNumbers(listString, ' ', _buffListCollection[kvp.Key]);
                    }
                }
            }
            return _buffListCollection[name];

        }
        private static void StrignsToNumbers(string s, char delim, List<Int32> list)
        {
            List<int> result = list;
            int start = 0;
            int end = 0;
            foreach (char x in s)
            {
                if (x == delim || end == s.Length - 1)
                {
                    if (end == s.Length - 1)
                        end++;
                    result.Add(int.Parse(s.Substring(start, end - start)));
                    start = end + 1;
                }
                end++;
            }
            
        }

        public void Broadcast(string message)
        {
            MQ.Cmd($"/bc {message}");
        }
    }

    public class DanBots : IBots
    {
        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;

        public List<string> BotsConnected()
        {
            throw new NotImplementedException();
        }

        public void Broadcast(string message)
        {
            throw new NotImplementedException();
        }

        public void BroadcastCommandToOthers(string query)
        {
            throw new NotImplementedException();
        }

        public List<int> BuffList(string name)
        {
            throw new NotImplementedException();
        }

        public bool HasShortBuff(string name, Int64 buffid)
        {
            throw new NotImplementedException();
        }

        public Boolean InZone(string name)
        {
            return false;
           // return MQ.Query<bool>($"${{NetBots[{name}].InZone}}");


        }
        public Int32 PctHealth(string name)
        {
            return 0;
        }

    }

   
}

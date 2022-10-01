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
        Boolean HasBuff(string name, Int64 buffid);
    }
    public class Bots: IBots
    {
        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;

        private string netbotConnectionString = string.Empty;
        private List<string> _connectedBots = new List<string>();
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

        public bool HasBuff(string name, Int64 buffid)
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

        public bool HasBuff(string name, Int64 buffid)
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

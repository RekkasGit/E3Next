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
        void BroadcastCommandToOthers(string query);
        void SetupAliases();

    }
    public class Bots: IBots
    {
        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;

        private string netbotConnectionString = string.Empty;
        private List<string> _connectedBots = new List<string>();
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

        public void SetupAliases()
        {
            MQ.Cmd("/noparse /squelch /alias /AssistOn /bc Assist on ${Target.ID}");
            MQ.Cmd("/noparse /squelch /alias /AssistMe /bc Assist on ${Target.ID}");
            MQ.Cmd("/squelch /alias /BackOff /bc Back off");
            MQ.Cmd("/squelch /alias /debuff /bc Debuffs on ${Target.ID}");
            MQ.Cmd("/squelch /alias /debuffson /bc Debuffs on ${Target.ID}");
            MQ.Cmd("/squelch /alias /debuffsoff /bc End Debuffs");
            MQ.Cmd("/squelch /alias /dot /bc DoTs on ${Target.ID}");
            MQ.Cmd("/squelch /alias /dotson /bc DoTs on ${Target.ID}");
            MQ.Cmd("/squelch /alias /dotsoff /bc End DoTs");
            MQ.Cmd("/squelch /alias /targetaeon /bc targetae on");
            MQ.Cmd("/squelch /alias /targetaeoff /bc targetae off");
            MQ.Cmd("/squelch /alias /pbaeon /bc pbae on");
            MQ.Cmd("/squelch /alias /pbaeoff /bc pbae off");
            MQ.Cmd("/squelch /alias /aeon /bc allae on");
            MQ.Cmd("/squelch /alias /aeoff /bc allae off");
            MQ.Cmd("/squelch /alias /pvpOff /bc //varset TogglePvP FALSE");
            MQ.Cmd("/squelch /alias /pvpOn /bc //varset TogglePvP TRUE");
            MQ.Cmd("/squelch /alias /SwarmPets /bc Swarm pets on ${Target.ID}");
            MQ.Cmd("/squelch /alias /EpicBurns /bc Epic burn");
            MQ.Cmd("/squelch /alias /QuickBurns /bc Quick burn");
            MQ.Cmd("/squelch /alias /LongBurns /bc Long burn");
            MQ.Cmd("/squelch /alias /FullBurns /bc Full burn");
            MQ.Cmd("/squelch /alias /taunton /varset doTaunt 1");
            MQ.Cmd("/squelch /alias /tauntoff /varset doTaunt 0");
            MQ.Cmd("/squelch /alias /AssistType /bc //varset Assist Type");
            MQ.Cmd("/squelch /alias /ns /bc SpellSet");
            MQ.Cmd("/squelch /alias /ss /bc SpellSet");
            MQ.Cmd("/squelch /alias /combatmode /bc combatmode");
            MQ.Cmd("/squelch /alias /cm /bc combatmode");
            MQ.Cmd("/squelch /alias /Lesson /bc VetAA Lesson of the Devoted");
            MQ.Cmd("/squelch /alias /Infusion /bc VetAA Infusion of the Faithful");
            MQ.Cmd("/squelch /alias /Staunch /bc VetAA Staunch Recovery");
            MQ.Cmd("/squelch /alias /Servant /bc VetAA Steadfast Servant");
            MQ.Cmd("/squelch /alias /Intensity /bc VetAA Intensity of the Resolute");
            MQ.Cmd("/squelch /alias /Armor /bc VetAA Armor of Experience");
            MQ.Cmd("/squelch /alias /Expedient /bc VetAA Expedient Recovery");
            MQ.Cmd("/squelch /alias /Throne /bc VetAA Throne of Heroes");
            MQ.Cmd("/squelch /alias /Jester /bc VetAA Chaotic Jester");

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

        public void BroadcastCommandToOthers(string query)
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

        public void SetupAliases()
        {
            throw new NotImplementedException();
        }
    }

   
}

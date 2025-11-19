using E3Core.Data;
using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3NextConfigEditor.Client
{
	public class Bots : IBots
	{
		public int BaseCorruptedCounters(string name)
		{
			return 0;
		}

		public int BaseCursedCounters(string name)
		{
			return 0;
		}

		public int BaseDebuffCounters(string name)
		{
			return 0;
		}

		public int BaseDiseasedCounters(string name)
		{
			return 0;
		}

		public int BasePoisonedCounters(string name)
		{
			return 0;
		}

		public List<string> BotsConnected()
		{
			return new List<string>();
		}

		public List<string> BotsInCombat()
		{
			return new List<string>();
		}

		public void Broadcast(string message, bool noparse = false)
		{
			
		}

		public void BroadcastCommand(string command, bool noparse = false, EventProcessor.CommandMatch match = null)
		{
			
		}

		public void BroadcastCommandAllZone(string command, bool noparse = false, EventProcessor.CommandMatch match = null)
		{
			
		}

		public void BroadcastCommandToGroup(string command, EventProcessor.CommandMatch match = null, bool noparse = false)
		{
			
		}

		public void BroadcastCommandToGroupZone(string command, EventProcessor.CommandMatch match = null, bool noparse = false)
		{
			
		}

		public void BroadcastCommandToPerson(string person, string command, bool noparse = false)
		{
			
		}

		public List<int> BuffList(string name)
		{
			return new List<int>();
		}

		public CharacterBuffs GetBuffInformation(string name)
		{
			throw new NotImplementedException();
		}

		public bool HasShortBuff(string name, int buffid)
		{
			return false;
		}

		public bool InCombat(string name)
		{
			return false;
		}

		public bool IsMyBot(string name)
		{
			return false;
		}

		public int PctHealth(string name)
		{
			return 100;
		}

		public int PctMana(string name)
		{
			return 100;
		}

		public List<int> PetBuffList(string name)
		{
			return new List<int>();
		}

		public string Query(string name, string query)
		{
			return string.Empty;
		}

		public void Trade(string name)
		{
			throw new NotImplementedException();
		}
	}
}

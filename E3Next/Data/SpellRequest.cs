using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public class SpellRequest : Spell
    {
        public SpellRequest(string spellName, IniData parsedData = null):base(spellName, parsedData)
        {

        }

        public Int64 LastRequestTimeStamp;
    }
}

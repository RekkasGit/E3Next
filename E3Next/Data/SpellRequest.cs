using IniParser.Model;

using System;

namespace E3Core.Data
{
    public class SpellRequest : Spell
    {
        public SpellRequest(string spellName, IniData parsedData = null) : base(spellName, parsedData)
        {

        }

        public Int64 LastRequestTimeStamp;
    }
}

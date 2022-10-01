using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public enum Class
    {
        Warrior = 0x1,
        Cleric = 0x2,
        Paladin = 0x4,
        Ranger = 0x8,
        Shadowknight = 0x10,
        Druid = 0x20,
        Monk = 0x40,
        Bard = 0x80,
        Rogue = 0x100,
        Shaman = 0x200,
        Necromancer = 0x400,
        Wizard = 0x800,
        Mage = 0x1000,
        Enchanter = 0x2000,
        Beastlord = 0x4000,
        Berserker = 0x8000,
        Mercenary = 0x10000,
        Tank = Warrior | Paladin | Shadowknight,
        Priest = Cleric | Druid | Shaman,
        Caster = Wizard | Mage | Enchanter | Necromancer,
        Melee = Beastlord | Berserker | Bard | Rogue | Ranger | Monk,
        PureMelee=Warrior|Rogue|Berserker,
        All = Tank | Priest | Caster | Melee,
        PetClass=Shadowknight|Druid|Necromancer|Mage|Enchanter|Beastlord,
        HealHybrid= Paladin|Ranger|Beastlord
    }

    public static class Classes
    {


        static Classes()
        {
            foreach(var pair in _classLongToShort)
            {
                _classShortToLong.Add(pair.Value, pair.Key);
            }
        
        }

    public static List<string> _classShortNames = new List<string>() { "WAR",
                                                                            "PAL",
                                                                            "RNG",
                                                                            "SHD",
                                                                            "DRU",
                                                                            "MNK",
                                                                            "BRD",
                                                                            "ROG",
                                                                            "SHM",
                                                                            "NEC",
                                                                            "WIZ",
                                                                            "MAG",
                                                                            "ENC",
                                                                            "BST",
                                                                            "BER",
                                                                            "MER"};

        public static Dictionary<string, string> _classLongToShort = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {{"Warrior","WAR"},
                                                                                                        {"Cleric","CLR"},
                                                                                                        {"Paladin","PAL"},
                                                                                                        {"Ranger","RNG"},
                                                                                                        {"Shadowknight","SHD" },
                                                                                                        {"Druid","DRU"},
                                                                                                        {"Monk","MNK"},
                                                                                                        {"Bard","BRD"},
                                                                                                        {"Rogue","ROG"},
                                                                                                        {"Shaman","SHM"},
                                                                                                        {"Necromancer","NEC"},
                                                                                                        {"Wizard","WIZ"},
                                                                                                        {"Mage","MAG"},
                                                                                                        {"Enchanter","ENC" },
                                                                                                        {"Beastlord","BST"},
                                                                                                        {"Berserker","BER"}};


        public static Dictionary<string, string> _classShortToLong = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); 

    }
}

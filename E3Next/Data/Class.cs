using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    [Flags]
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
        Magician = 0x1000,
        Enchanter = 0x2000,
        Beastlord = 0x4000,
        Berserker = 0x8000,
        Mercenary = 0x10000,
        Tank = Warrior | Paladin | Shadowknight,
        Priest = Cleric | Druid | Shaman,
        Caster = Wizard | Magician | Enchanter | Necromancer,
        Melee = Beastlord | Berserker | Bard | Rogue | Ranger | Monk,
        PureMelee = Warrior | Rogue | Berserker | Monk,
        All = Tank | Priest | Caster | Melee,
        PetClass = Shadowknight | Druid | Necromancer | Magician | Enchanter | Beastlord | Shaman,
        HealHybrid = Paladin | Ranger | Beastlord,
        FeignDeathClass = Necromancer | Shadowknight | Monk,
        ManaUsers = Caster | Priest | HealHybrid | Shadowknight,
        Ranged = Caster | Ranger,
        Charmer = Enchanter|Druid|Necromancer
    }

    public static class Classes
    {


        static Classes()
        {
            foreach(var pair in ClassLongToShort)
            {
                _classShortToLong.Add(pair.Value, pair.Key);
            }
        
        }

    public static List<string> ClassShortNames = new List<string>() { "WAR",
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
                                                                            "MER",
                                                                            "CLR"};

        public static Dictionary<string, string> ClassLongToShort = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {{"Warrior","WAR"},
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
                                                                                                        {"Magician","MAG"},
                                                                                                        {"Enchanter","ENC" },
                                                                                                        {"Beastlord","BST"},
                                                                                                        {"Berserker","BER"}};


        public static Dictionary<string, string> _classShortToLong = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); 

    }
}

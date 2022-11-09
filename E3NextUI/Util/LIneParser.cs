using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace E3NextUI.Util
{
    public static class LineParser
    {

        //store the entire battle data?

        //1 million data entries should be neough for anything, about 3.8 megs per
        public static List<Int32> _yourDamage = new List<int>(100000);
        public static List<Int64> _yourDamageTime = new List<Int64>(1000000);

        public static List<Int32> _yourPetDamage = new List<int>(1000000);
        public static List<Int64> _yourPetDamageTime = new List<Int64>(1000000);

        public static List<Int32> _yourDamageShieldDamage = new List<int>(1000000);
        public static List<Int64> _yourDamageShieldDamageTime = new List<Int64>(1000000);

        public static List<Int32> _damageToYou = new List<int>(1000000);
        public static List<Int64> _damageToYouTime = new List<Int64>(1000000);

        public static List<Int32> _healingToYou = new List<int>(1000000);
        public static List<Int64> _healingToYouTime = new List<Int64>(1000000);

        public static List<Int32> _healingByYou = new List<int>(1000000);
        public static List<Int64> _healingByYouTime = new List<Int64>(1000000);

        public static Int64 _lastCombatCheck = 0;
        public static bool _currentlyCombat = false;
        public static object _objectLock = new object();
        public static string PetName=String.Empty;

        public static void Reset()
        {
            lock(_objectLock)
            {
                _yourDamage.Clear();
                _yourDamageTime.Clear();
                _yourPetDamage.Clear();
                _yourPetDamageTime.Clear();
                _yourDamageShieldDamage.Clear();
                _yourDamageShieldDamageTime.Clear();
                _damageToYou.Clear();
                _damageToYouTime.Clear();
                _healingToYou.Clear();
                _healingToYouTime.Clear();
                _healingByYou.Clear();
                _healingByYouTime.Clear();
            }
        
        }
        public static void SetPetName(string petName)
        {
            if (String.IsNullOrWhiteSpace(PetName) && petName!="NULL")
            {
                PetName = petName;
                _yourPetMelee = new System.Text.RegularExpressions.Regex($"{PetName} .+ for ([0-9]+) points of damage.");
            }
            else
            {
                if(petName!="NULL")
                {
                    if (!PetName.Equals(petName))
                    {
                        PetName = petName;
                        _yourPetMelee = new System.Text.RegularExpressions.Regex($"{PetName} .+ for ([0-9]+) points of damage.");
                    }

                }

            }
        }
        public static void SetCombatState(bool inCombat)
        {
            if (inCombat && _currentlyCombat == false)
            {
                _currentlyCombat = true;
                //reset our collections
                Reset();
                return;
            }

            _currentlyCombat = inCombat;
        }
        public static void ParseLine(string line)
        { 
            lock(_objectLock)
            { 
                //E3UI._stopWatch;
                if (TryUpdateCollection(line, _yourdmg, _yourDamage, _yourDamageTime)) return;
                if (TryUpdateCollection(line, _yourdot, _yourDamage, _yourDamageTime)) return;
                if (TryUpdateCollection(line, _yourspellDmg, _yourDamage, _yourDamageTime)) return;
               
                if (!String.IsNullOrWhiteSpace(PetName))
                {
                    if (TryUpdateCollection(line, _yourPetMelee, _yourPetDamage, _yourPetDamageTime)) return;
                    if (TryUpdateCollection(line, _yourPetProcDmg, _yourPetDamage, _yourPetDamageTime)) return;
                }
                if (TryUpdateCollection(line, _yourswarmDmg, _yourPetDamage, _yourPetDamageTime)) return;
                if (TryUpdateCollection(line, _meleeDmgToYou, _damageToYou, _damageToYouTime)) return;
                if (TryUpdateCollection(line, _dotDmgToYou, _damageToYou, _damageToYouTime)) return;
                if (TryUpdateCollection(line, _spellDmgToYou, _damageToYou, _damageToYouTime)) return;
                if (TryUpdateCollection(line, _damageshieldByYou, _yourDamageShieldDamage, _yourDamageShieldDamageTime)) return;
                if (TryUpdateCollection(line, _healingYou, _healingToYou, _healingToYouTime)) return;
                if (TryUpdateCollection(line, _selfHeals, _healingToYou, _healingToYouTime)) return;
                if (TryUpdateCollection(line, _healingByYouRegex, _healingByYou, _healingByYouTime)) return;

            }
        }
        private static bool TryUpdateCollection(string line,Regex reg, List<Int32> collection, List<Int64> timeCollection)
        {
            var match = reg.Match(line);
            if (match.Success)
            {

                if (Int32.TryParse(match.Groups[1].Value, out Int32 value))
                {
                    collection.Add(value);
                    timeCollection.Add(E3UI._stopWatch.ElapsedMilliseconds);
                    return true;
                }
            }
            return false;
        }
        //Reaper hits a Tae Ew disciple for 131 points of damage.


        //damage done by you
        static System.Text.RegularExpressions.Regex _yourdmg = new System.Text.RegularExpressions.Regex("You .+ for ([0-9]+) points of damage.");
        static System.Text.RegularExpressions.Regex _yourdot = new System.Text.RegularExpressions.Regex("taken ([0-9]+) damage from your");
        static System.Text.RegularExpressions.Regex _yourspellDmg = new System.Text.RegularExpressions.Regex($"{E3UI._playerName} hit .+ for ([0-9]+) points of");
       
        //proc dmg by pet
        static System.Text.RegularExpressions.Regex _yourPetProcDmg = new System.Text.RegularExpressions.Regex(".+ was hit by non-melee for ([0-9]+) points of damage\\.");
        static System.Text.RegularExpressions.Regex _yourPetMelee = new System.Text.RegularExpressions.Regex($"{PetName} .+ for ([0-9]+) points of damage.");
        static System.Text.RegularExpressions.Regex _yourswarmDmg = new System.Text.RegularExpressions.Regex($"{E3UI._playerName}`s pet hits .+ for ([0-9]+) points of");

        //damage to you
        static System.Text.RegularExpressions.Regex _meleeDmgToYou = new System.Text.RegularExpressions.Regex(".+ YOU for ([0-9]+) points of damage\\.");
        static System.Text.RegularExpressions.Regex _dotDmgToYou = new System.Text.RegularExpressions.Regex("You have taken ([0-9]+) damage");
        static System.Text.RegularExpressions.Regex _spellDmgToYou = new System.Text.RegularExpressions.Regex("You have taken ([0-9]+) points of damage");

        //damage shield
        static System.Text.RegularExpressions.Regex _damageshieldByYou = new System.Text.RegularExpressions.Regex(".+ was hit by non-melee for ([0-9]+) points of damage\\.");
        //healing

        static System.Text.RegularExpressions.Regex _healingYou = new System.Text.RegularExpressions.Regex(".+ has healed you for ([0-9]+) points\\.");
        static System.Text.RegularExpressions.Regex _selfHeals = new System.Text.RegularExpressions.Regex("You have been healed for ([0-9]+) hit points");
        static System.Text.RegularExpressions.Regex _healingByYouRegex = new System.Text.RegularExpressions.Regex("You have healed .+ for ([0-9]+) points\\.");

    }
}

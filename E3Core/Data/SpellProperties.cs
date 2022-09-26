using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Data
{
    public static class SpellProperties
    {

        static List<string> _spellPropMapping = new List<string>();
        static Dictionary<string, Int32> _spellPropIndex = new Dictionary<string, int>();
       
        static SpellProperties()
        {
            
            _spellPropMapping.Add("CastName");
            _spellPropMapping.Add("CastType");
            _spellPropMapping.Add("TargetType");
            _spellPropMapping.Add("SpellGem");
            _spellPropMapping.Add("SubToRun");
            _spellPropMapping.Add("GiveUpTimer");
            _spellPropMapping.Add("MaxTries");
            _spellPropMapping.Add("CheckFor");
            _spellPropMapping.Add("Duration");
            _spellPropMapping.Add("RecastTime");
            _spellPropMapping.Add("RecoveryTime");
            _spellPropMapping.Add("MyCastTime");
            _spellPropMapping.Add("MyRange");
            _spellPropMapping.Add("Mana");
            _spellPropMapping.Add("MinMana");
            _spellPropMapping.Add("MaxMana");
            _spellPropMapping.Add("MinHP");
            _spellPropMapping.Add("HealPct");
            _spellPropMapping.Add("Reagent");
            _spellPropMapping.Add("ItemMustEquip");
            _spellPropMapping.Add("SpellName");
            _spellPropMapping.Add("NoBurn");
            _spellPropMapping.Add("NoAggro");
            _spellPropMapping.Add("Mode");
            _spellPropMapping.Add("Rotate");
            _spellPropMapping.Add("Delay");
            _spellPropMapping.Add("CastID");
            _spellPropMapping.Add("MinEnd");
            _spellPropMapping.Add("CastInvis");
            _spellPropMapping.Add("SpellType");
            _spellPropMapping.Add("CastTarget");
            _spellPropMapping.Add("GiftOfMana");
            _spellPropMapping.Add("CheckForID");
            _spellPropMapping.Add("SpellID");
            _spellPropMapping.Add("PctAggro");
            _spellPropMapping.Add("Zone");
            _spellPropMapping.Add("MinSick");
            _spellPropMapping.Add("AllowSpellSwap");
            _spellPropMapping.Add("NoEarlyRecast");
            _spellPropMapping.Add("NoStack");
            _spellPropMapping.Add("TriggerSpell");
            _spellPropMapping.Add("BeforeSpell");
            _spellPropMapping.Add("AfterSpell");
            _spellPropMapping.Add("NoInterrupt");
            _spellPropMapping.Add("AfterEvent");
            _spellPropMapping.Add("BeforeEvent");
            _spellPropMapping.Add("CastIF");

            _spellPropIndex.Add("iCastName", 1);
            _spellPropIndex.Add("iCastType", 2);
            _spellPropIndex.Add("iTargetType", 3);
            _spellPropIndex.Add("iSpellGem", 4);
            _spellPropIndex.Add("iSubToRun", 5);
            _spellPropIndex.Add("iGiveUpTimer", 6);
            _spellPropIndex.Add("iMaxTries", 7);
            _spellPropIndex.Add("iCheckFor", 8);
            _spellPropIndex.Add("iDuration", 9);
            _spellPropIndex.Add("iRecastTime", 10);
            _spellPropIndex.Add("iRecoveryTime", 11);
            _spellPropIndex.Add("iMyCastTime", 12);
            _spellPropIndex.Add("iMyRange", 13);
            _spellPropIndex.Add("iMana", 14);
            _spellPropIndex.Add("iMinMana", 15);
            _spellPropIndex.Add("iMaxMana", 16);
            _spellPropIndex.Add("iMinHP", 17);
            _spellPropIndex.Add("iHealPct", 18);
            _spellPropIndex.Add("iReagent", 19);
            _spellPropIndex.Add("iItemMustEquip", 20);
            _spellPropIndex.Add("iSpellName", 21);
            _spellPropIndex.Add("iNoBurn", 22);
            _spellPropIndex.Add("iNoAggro", 23);
            _spellPropIndex.Add("iMode", 24);
            _spellPropIndex.Add("iRotate", 25);
            _spellPropIndex.Add("iDelay", 26);
            _spellPropIndex.Add("iCastID", 27);
            _spellPropIndex.Add("iMinEnd", 28);
            _spellPropIndex.Add("iCastInvis", 29);
            _spellPropIndex.Add("iSpellType", 30);
            _spellPropIndex.Add("iCastTarget", 31);
            _spellPropIndex.Add("iGiftOfMana", 32);
            _spellPropIndex.Add("iCheckForID", 33);
            _spellPropIndex.Add("iSpellID", 34);
            _spellPropIndex.Add("iPctAggro", 35);
            _spellPropIndex.Add("iZone", 36);
            _spellPropIndex.Add("iMinSick", 37);
            _spellPropIndex.Add("iAllowSpellSwap", 38);
            _spellPropIndex.Add("iNoEarlyRecast", 39);
            _spellPropIndex.Add("iNoStack", 40);
            _spellPropIndex.Add("iTriggerSpell", 41);
            _spellPropIndex.Add("iIfs", 42);
            _spellPropIndex.Add("iBeforeSpell", 43);
            _spellPropIndex.Add("iAfterSpell", 44);
            _spellPropIndex.Add("iNoInterrupt", 45);
            _spellPropIndex.Add("iAfterEvent", 46);
            _spellPropIndex.Add("iBeforeEvent", 47);
            _spellPropIndex.Add("iCastIf", 48);

        }


    }
}

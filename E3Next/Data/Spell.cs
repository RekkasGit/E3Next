using E3Core.Processors;
using E3Core.Utility;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace E3Core.Data
{


    public enum CastType
    {
        AA,
        Spell,
        Disc,
        Ability,
        Item,
        None
    }

    public class Spell
    {
        public static Dictionary<Int32, Data.Spell> _loadedSpells = new Dictionary<int, Spell>();
        public static Dictionary<string, Data.Spell> LoadedSpellsByName = new Dictionary<string, Spell>();
        public static IMQ MQ = E3.MQ;


        public Spell(string spellName, IniData parsedData = null)
        {

            SpellName = spellName; //what the thing actually casts
            CastName = spellName;//required to send command
            InitName = spellName;

            Parse(parsedData);



            QueryMQ();
            if (!_loadedSpells.ContainsKey(this.SpellID))
            {
                //sometimes an item can have the same spellid of a spell. prevent duplicates. 
                //should deal with this later tho, and make it off maybe castID
                if (!LoadedSpellsByName.ContainsKey(this.SpellName))
                {
                    LoadedSpellsByName.Add(this.SpellName, this);
                    _loadedSpells.Add(this.SpellID, this);
                }
            }
        }

        void Parse(IniData parsedData)
        {

            if (SpellName.Contains("/"))
            {

                string[] splitList = SpellName.Split('/');
                SpellName = splitList[0];
                CastName = SpellName;
                Int32 counter = 0;
                foreach (var value in splitList)
                {
                    //skip the 1st one
                    if (counter == 0)
                    {
                        counter++;
                        continue;
                    }

                    if (value.StartsWith("Gem|", StringComparison.OrdinalIgnoreCase))
                    {
                        SpellGem = GetArgument<Int32>(value);
                    }
                    else if (value.Equals("NoInterrupt", StringComparison.OrdinalIgnoreCase))
                    {
                        NoInterrupt = true;
                    }
                    else if (value.StartsWith("AfterSpell|", StringComparison.OrdinalIgnoreCase))
                    {
                        AfterSpell = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("AfterCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        AfterSpell = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("BeforeSpell|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeSpell = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("BeforeCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeSpell = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("GiveUpTimer|", StringComparison.OrdinalIgnoreCase))
                    {
                        GiveUpTimer = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("GiveUpTimer|", StringComparison.OrdinalIgnoreCase))
                    {
                        MaxTries = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("CheckFor|", StringComparison.OrdinalIgnoreCase))
                    {
                        CheckFor = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("CastIf|", StringComparison.OrdinalIgnoreCase))
                    {
                        CastIF = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("MinMana|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinMana = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("MaxMana|", StringComparison.OrdinalIgnoreCase))
                    {
                        MaxMana = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("MinHP|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinHP = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("HealPct|", StringComparison.OrdinalIgnoreCase))
                    {
                        HealPct = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("HealPct|", StringComparison.OrdinalIgnoreCase))
                    {
                        HealPct = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("Reagent|", StringComparison.OrdinalIgnoreCase))
                    {
                        Reagent = GetArgument<String>(value);
                    }
                    else if (value.Equals("NoBurn", StringComparison.OrdinalIgnoreCase))
                    {
                        NoBurn = true;
                    }
                    else if (value.Equals("NoAggro", StringComparison.OrdinalIgnoreCase))
                    {
                        NoAggro = true;
                    }
                    else if (value.Equals("Rotate", StringComparison.OrdinalIgnoreCase))
                    {
                        Rotate = true;
                    }
                    else if (value.StartsWith("Delay|", StringComparison.OrdinalIgnoreCase))
                    {
                        string tvalue = value;
                        bool isMinute = false;
                        if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                        {
                            tvalue = tvalue.Substring(0, value.Length - 1);
                        }
                        else if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                        {
                            isMinute = true;
                            tvalue = tvalue.Substring(0, value.Length - 1);
                        }

                        Delay = GetArgument<Int32>(tvalue);
                        if (isMinute)
                        {
                            Delay = Delay * 60;
                        }

                    }
                    else if (value.Equals("GoM", StringComparison.OrdinalIgnoreCase))
                    {
                        GiftOfMana = true;
                    }
                    else if (value.StartsWith("PctAggro|", StringComparison.OrdinalIgnoreCase))
                    {
                        PctAggro = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("Zone|", StringComparison.OrdinalIgnoreCase))
                    {
                        Zone = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("MinSick|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinSick = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("MinEnd|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinEnd = GetArgument<Int32>(value);
                    }
                    else if (value.Equals("AllowSpellSwap", StringComparison.OrdinalIgnoreCase))
                    {
                        GiftOfMana = true;
                    }
                    else if (value.Equals("NoEarlyRecast", StringComparison.OrdinalIgnoreCase))
                    {
                        NoEarlyRecast = true;
                    }
                    else if (value.Equals("NoStack", StringComparison.OrdinalIgnoreCase))
                    {
                        NoStack = true;
                    }
                    else if (value.StartsWith("TriggerSpell|", StringComparison.OrdinalIgnoreCase))
                    {
                        TriggerSpell = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("Ifs|", StringComparison.OrdinalIgnoreCase))
                    {
                        string ifKey = GetArgument<string>(value);
                        var section = parsedData.Sections["Ifs"];
                        if (section != null)
                        {
                            var keyData = section[ifKey];
                            if (!String.IsNullOrWhiteSpace(keyData))
                            {
                                Ifs = keyData;
                            }
                        }
                    }
                    else if (value.StartsWith("AfterEvent|", StringComparison.OrdinalIgnoreCase))
                    {
                        string ifKey = GetArgument<string>(value);
                        var section = parsedData.Sections["Events"];
                        if (section != null)
                        {
                            var keyData = section[ifKey];
                            if (!String.IsNullOrWhiteSpace(keyData))
                            {
                                AfterEvent = keyData;
                            }
                        }
                    }
                    else if (value.StartsWith("BeforeEvent|", StringComparison.OrdinalIgnoreCase))
                    {
                        string ifKey = GetArgument<string>(value);
                        var section = parsedData.Sections["Events"];
                        if (section != null)
                        {
                            var keyData = section[ifKey];
                            if (!String.IsNullOrWhiteSpace(keyData))
                            {
                                BeforeEvent = keyData;
                            }
                        }
                    }
                    else
                    {
                        //doesn't match anything, so we assume its the target for the 1st one
                        if (String.IsNullOrWhiteSpace(CastTarget) && !value.Contains("|"))
                        {
                            CastTarget = e3util.FirstCharToUpper(value);
                        }

                    }
                }

            }

        }
        public static T GetArgument<T>(string query)
        {
            Int32 indexOfPipe = query.IndexOf('|') + 1;
            string input = query.Substring(indexOfPipe, query.Length - indexOfPipe);

            if (typeof(T) == typeof(Int32))
            {

                Int32 value;
                if (Int32.TryParse(input, out value))
                {
                    return (T)(object)value;
                }

            }
            else if (typeof(T) == typeof(Boolean))
            {
                Boolean booleanValue;
                if (Boolean.TryParse(input, out booleanValue))
                {
                    return (T)(object)booleanValue;
                }
                if (input == "NULL")
                {
                    return (T)(object)false;
                }
                Int32 intValue;
                if (Int32.TryParse(input, out intValue))
                {
                    if (intValue > 0)
                    {
                        return (T)(object)true;
                    }
                    return (T)(object)false;
                }
                if (string.IsNullOrWhiteSpace(input))
                {
                    return (T)(object)false;
                }

                return (T)(object)true;


            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)input;
            }
            else if (typeof(T) == typeof(decimal))
            {
                Decimal value;
                if (Decimal.TryParse(input, out value))
                {
                    return (T)(object)value;
                }
            }
            else if (typeof(T) == typeof(Int64))
            {
                Int64 value;
                if (Int64.TryParse(input, out value))
                {
                    return (T)(object)value;
                }
            }


            return default(T);
        }
        void QueryMQ()
        {

            if (MQ.Query<bool>($"${{Me.AltAbility[{CastName}].Spell}}"))
            {
                CastType = CastType.AA;
            }
            else if (MQ.Query<bool>($"${{Me.Book[{CastName}]}}"))
            {
                CastType = CastType.Spell;
                SpellInBook = true;
            }
            else if (MQ.Query<bool>($"${{Me.CombatAbility[{CastName}]}}"))
            {
                CastType = CastType.Disc;
            }
            else if (MQ.Query<bool>($"${{Me.Ability[{CastName}]}}"))
            {
                CastType = CastType.Ability;
            }
            else if (MQ.Query<bool>($"${{FindItem[={CastName}]}}"))
            {

                CastType = CastType.Item;
            }
            else if (MQ.Query<bool>($"${{Spell[{CastName}]}}"))
            {
                //final check to see if its a spell, that maybe a mob casts?
                CastType = CastType.Spell;
            }
            else
            {
                //bad spell/item/etc
                CastType = CastType.None;
            }



            if (CastType == CastType.Item)
            {
                Int32 invSlot;
                Int32 bagSlot;
                //check if this is an itemID

                Int32 itemID = -1;


                if (Int32.TryParse(CastName, out itemID))
                {
                    invSlot = MQ.Query<Int32>($"${{FindItem[{CastName}].ItemSlot}}");
                    bagSlot = MQ.Query<Int32>($"${{FindItem[{CastName}].ItemSlot2}}");
                }
                else
                {
                    invSlot = MQ.Query<Int32>($"${{FindItem[={CastName}].ItemSlot}}");
                    bagSlot = MQ.Query<Int32>($"${{FindItem[={CastName}].ItemSlot2}}");

                }

                if (bagSlot == -1)
                {
                    //Means this is not in a bag and in the root inventory, OR we are wearing it
                    TargetType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell.TargetType}}");
                    Duration = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.Duration}}");
                    DurationTotalSeconds = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.Duration.TotalSeconds}}");
                    RecastTime = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.RecastTime}}");
                    RecoveryTime = MQ.Query<Decimal>($"${{Me.Inventory[{invSlot}].Spell.RecoveryTime}}");
                    MyCastTime = MQ.Query<Decimal>($"${{Me.Inventory[{invSlot}].Spell.CastTime}}");

                    double AERange = MQ.Query<double>($"${{Me.Inventory[{invSlot}].Spell.AERange}}");
                    MyRange = AERange;
                    if (MyRange == 0)
                    {
                        MyRange = MQ.Query<double>($"${{Me.Inventory[{invSlot}].Spell.MyRange}}"); ;
                    }

                    string et = MQ.Query<String>($"${{Me.Inventory[{invSlot}].EffectType}}");

                    if (et.Equals("Click Worn", StringComparison.OrdinalIgnoreCase))
                    {
                        ItemMustEquip = true;
                    }

                    SpellName = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell}}");
                    SpellID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.ID}}");
                    CastID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].ID}}");
                    SpellType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell.SpellType}}");
                }
                else
                {
                    //1 index vs 0 index
                    bagSlot += 1;
                    TargetType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.TargetType}}");
                    Duration = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.Duration}}");
                    DurationTotalSeconds = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.Duration.TotalSeconds}}");

                    RecastTime = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.RecastTime}}");
                    RecoveryTime = MQ.Query<Decimal>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.RecoveryTime}}");
                    MyCastTime = MQ.Query<Decimal>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].CastTime}}");

                    double AERange = MQ.Query<double>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.AERange}}");
                    MyRange = AERange;
                    if (MyRange == 0)
                    {
                        MyRange = MQ.Query<double>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.MyRange}}"); ;
                    }

                    string et = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].EffectType}}");
                    if (et.Equals("Click Worn", StringComparison.OrdinalIgnoreCase))
                    {
                        ItemMustEquip = true;
                    }

                    SpellName = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell}}");
                    SpellID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.ID}}");
                    CastID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].ID}}");
                    SpellType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.SpellType}}");

                }

            }
            else if (CastType == CastType.AA)
            {
                TargetType = MQ.Query<String>($"${{Me.AltAbility[{CastName}].Spell.TargetType}}");
                Duration = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].Spell.Duration}}");
                DurationTotalSeconds = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].Spell.Duration.TotalSeconds}}");

                RecastTime = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].ReuseTime}}");
                RecoveryTime = MQ.Query<Decimal>($"${{Me.AltAbility[{CastName}].Spell.RecoveryTime}}");
                MyCastTime = MQ.Query<Decimal>($"${{Me.AltAbility[{CastName}].Spell.MyCastTime}}");

                Double AERange = MQ.Query<Double>($"${{Me.AltAbility[{CastName}].Spell.AERange}}");
                MyRange = MQ.Query<double>($"${{Me.AltAbility[{CastName}].Spell.MyRange}}");
                SpellType = MQ.Query<String>($"${{Spell[{CastName}].SpellType}}");


                if (SpellType.Equals("Detrimental", StringComparison.OrdinalIgnoreCase))
                {

                    if (AERange > 0)
                    {
                        if (MyRange == 0)
                        {
                            //set MyRange to AE range for spells that don't have a MyRange like PBAE nukes
                            MyRange = AERange;
                        }
                    }

                }
                else
                {
                    //if the buff/heal has an AERange value, set MyRange to AE Range because otherwise the spell won't land on the target
                    if (AERange > 0)
                    {
                        MyRange = AERange;
                    }

                }
                SpellName = MQ.Query<String>($"${{Me.AltAbility[{CastName}].Spell}}");
                SpellID = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].Spell.ID}}");
                CastID = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].ID}}");
            }
            else if (CastType == CastType.Spell)
            {

                TargetType = MQ.Query<String>($"${{Spell[{CastName}].TargetType}}");
                Duration = MQ.Query<Int32>($"${{Spell[{CastName}].Duration}}");
                DurationTotalSeconds = MQ.Query<Int32>($"${{Spell[{CastName}].Duration.TotalSeconds}}");

                RecastTime = MQ.Query<Int32>($"${{Spell[{CastName}].RecastTime}}");
                RecoveryTime = MQ.Query<Decimal>($"${{Spell[{CastName}].RecoveryTime}}");
                MyCastTime = MQ.Query<Decimal>($"${{Spell[{CastName}].MyCastTime}}");

                Double AERange = MQ.Query<Double>($"${{Spell[{CastName}].AERange}}");
                MyRange = MQ.Query<double>($"${{Spell[{CastName}].MyRange}}");
                SpellType = MQ.Query<String>($"${{Spell[{CastName}].SpellType}}");

                if (SpellType.Equals("Detrimental", StringComparison.OrdinalIgnoreCase))
                {

                    if (AERange > 0)
                    {
                        if (MyRange == 0)
                        {
                            //set MyRange to AE range for spells that don't have a MyRange like PBAE nukes
                            MyRange = AERange;
                        }
                    }

                }
                else
                {
                    //if the buff/heal has an AERange value, set MyRange to AE Range because otherwise the spell won't land on the target
                    if (AERange > 0)
                    {
                        MyRange = AERange;
                    }

                }
                Mana = MQ.Query<Int32>($"${{Spell[{CastName}].Mana}}");
                SpellName = CastName;
                SpellID = MQ.Query<Int32>($"${{Spell[{CastName}].ID}}");
                CastID = SpellID;
            }
            else if (CastType == CastType.Disc)
            {
                TargetType = MQ.Query<String>($"${{Spell[{CastName}].TargetType}}");
                Duration = MQ.Query<Int32>($"${{Spell[{CastName}].Duration}}");
                DurationTotalSeconds = MQ.Query<Int32>($"${{Spell[{CastName}].Duration.TotalSeconds}}");
                EnduranceCost = MQ.Query<Int32>($"${{Spell[{CastName}].EnduranceCost}}");
                Double AERange = MQ.Query<Double>($"${{Spell[{CastName}].AERange}}");
                MyRange = AERange;
                if (MyRange == 0)
                {
                    MyRange = MQ.Query<double>($"${{Spell[{CastName}].MyRange}}");
                }
                SpellName = CastName;
                SpellID = MQ.Query<Int32>($"${{Spell[{CastName}].ID}}");
                CastID = SpellID;
                SpellType = MQ.Query<String>($"${{Spell[{CastName}].SpellType}}");

            }
            else if (CastType == CastType.Ability)
            {
                //nothing to update here
            }

            if (!String.IsNullOrWhiteSpace(CheckFor))
            {
                if (MQ.Query<bool>($"${{Bool[${{AltAbility[{CheckFor}].Spell}}]}}"))
                {
                    CheckForID = MQ.Query<Int32>($"${{AltAbility[{CheckFor}].Spell.ID}}");
                }
                else if (MQ.Query<bool>($"${{Bool[${{Spell[{CheckFor}].ID}}]}}"))
                {
                    CheckForID = MQ.Query<Int32>($"${{Spell[{CheckFor}].ID}}");
                }
            }

        }
        //override public String ToString()
        //{

        //    string returnString = $"spell:{SpellName} spellid:{SpellID} castid: {CastID} mana:{Mana} spelltype:{SpellType} targettype:{TargetType} Duration:{Duration} RecastTime:{RecastTime} RecoveryTime:{RecoveryTime} MyCastTime:{MyCastTime} MyRange:{MyRange} MustEquip:{ItemMustEquip} ";

        //    return returnString;
        //}
        public String SpellName = String.Empty;//the spell's name. If the item clicks, this is the spell it casts
        public String CastName = String.Empty;//this can be the item, spell, aa, disc. What is required to cast it. 
        public CastType CastType;
        public String TargetType = String.Empty;
        public Int32 SpellGem;
        public Int32 GiveUpTimer;
        public Int32 MaxTries = 5;
        public String CheckFor = String.Empty;
        public Int32 Duration;
        public Int32 DurationTotalSeconds;
        public Int32 RecastTime;
        public Decimal RecoveryTime;
        private Decimal myCastTime;
        public decimal MyCastTime
        {
            get { return myCastTime; }
            set
            {
                myCastTime = value;
                if (CastType != CastType.Ability)
                {
                    MyCastTimeInSeconds = value / 1000;
                }
            }
        }
        public decimal MyCastTimeInSeconds = 0;
        public Double MyRange;
        public Int32 Mana;
        public Int32 MinMana;
        public Int32 MaxMana;
        public Int32 MinHP;
        public Int32 HealPct;
        public String Reagent = String.Empty;
        public Boolean ItemMustEquip;
        public Boolean NoBurn;
        public Boolean NoAggro;
        public Int32 Mode;
        public Boolean Rotate;
        public Int32 EnduranceCost;
        public Int32 Delay;
        public Int32 CastID;
        public Int32 MinEnd;
        public Boolean CastInvis;
        public String SpellType = String.Empty;
        public String CastTarget = String.Empty;
        public Boolean GiftOfMana;
        public Int32 CheckForID;
        public Int32 SpellID;
        public Int32 PctAggro;
        public String Zone = "All";
        public Int32 MinSick = 2;
        public Boolean AllowSpellSwap;
        public Boolean NoEarlyRecast;
        public Boolean NoStack;
        public String TriggerSpell = String.Empty;
        public String BeforeSpell = String.Empty;
        public Data.Spell BeforeSpellData;
        public String AfterSpell = String.Empty;
        public Data.Spell AfterSpellData;
        public Boolean NoInterrupt;
        public String AfterEvent = String.Empty;
        public String BeforeEvent = String.Empty;
        public String CastIF = String.Empty;
        public string Ifs = String.Empty;
        public string InitName = String.Empty;
        public bool ReagentOutOfStock = false;
        public bool SpellInBook = false;



        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.GetType().Name);
            sb.AppendLine();
            sb.AppendLine("==============");
            foreach (FieldInfo property in this.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public |
                                                                    BindingFlags.Instance | BindingFlags.Static))
            {
                var value = property.GetValue(this);
                sb.Append(property.Name);
                sb.Append(": ");
                sb.Append(value);
                sb.Append(System.Environment.NewLine);
               
            }
            return sb.ToString();
        }
    }
}

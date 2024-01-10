using E3Core.Processors;
using E3Core.Utility;

using IniParser.Model;

using MonoCore;

using System;
using System.Collections.Generic;
using System.Linq;
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
        public static readonly IMQ MQ = E3.MQ;

        private static readonly Dictionary<string, int> _spellIDLookup = new Dictionary<string, int>();

        public static readonly Dictionary<string, Spell> LoadedSpellsByName = new Dictionary<string, Spell>();
        public static readonly Dictionary<int, Spell> _loadedSpells = new Dictionary<int, Spell>();

        public static int SpellIDLookup(string spellName)
        {
            if (_spellIDLookup.TryGetValue(spellName, out var result))
            {
                return result;
            }
            int spellID = MQ.Query<int>($"${{Spell[{spellName}].ID}}");
            if (spellID > 0)
            {
                _spellIDLookup.Add(spellName, spellID);
            }
            return spellID;
        }

        public static T GetArgument<T>(string query)
        {
            int indexOfPipe = query.IndexOf('|') + 1;
            string input = query.Substring(indexOfPipe, query.Length - indexOfPipe);

            if (typeof(T) == typeof(int))
            {
                if (int.TryParse(input, out int value))
                {
                    return (T)(object)value;
                }
            }
            else if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(input, out bool booleanValue))
                {
                    return (T)(object)booleanValue;
                }
                if (input == "NULL")
                {
                    return (T)(object)false;
                }
                if (int.TryParse(input, out int intValue))
                {
                    return intValue > 0 ? (T)(object)true : (T)(object)false;
                }
                return string.IsNullOrWhiteSpace(input) ? (T)(object)false : (T)(object)true;
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)input;
            }
            else if (typeof(T) == typeof(decimal))
            {
                if (decimal.TryParse(input, out decimal value))
                {
                    return (T)(object)value;
                }
            }
            else if (typeof(T) == typeof(long))
            {
                if (long.TryParse(input, out long value))
                {
                    return (T)(object)value;
                }
            }

            return default;
        }

        public string SpellName { get; set; } = string.Empty;//the spell's name. If the item clicks, this is the spell it casts
        public string CastName { get; set; } = string.Empty;//this can be the item, spell, aa, disc. What is required to cast it. 
        public CastType CastType { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public int SpellGem { get; set; }
        public int GiveUpTimer { get; set; }
        public int MaxTries { get; set; } = 5;
        public Dictionary<string, int> CheckForCollection = new Dictionary<string, int>();
        public int Duration { get; set; }
        public int DurationTotalSeconds { get; set; }
        public int RecastTime { get; set; }
        public decimal RecoveryTime { get; set; }
        private decimal myCastTime;
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
        public decimal MyCastTimeInSeconds { get; set; }
        public double MyRange { get; set; }
        public int Mana { get; set; }
        public int MinMana { get; set; }
        public int MaxMana { get; set; }
        public int MinHP { get; set; }
        public int HealPct { get; set; }
        public bool Debug { get; set; }
        public string Reagent { get; set; } = string.Empty;
        public bool ItemMustEquip { get; set; }
        public bool NoBurn { get; set; }
        public bool NoAggro { get; set; }
        public int Mode { get; set; }
        public bool Rotate { get; set; }
        public int EnduranceCost { get; set; }
        public int Delay { get; set; }
        public int DelayAfterCast { get; set; }

        public int CastID { get; set; }
        public int MinEnd { get; set; }
        public bool CastInvis { get; set; }
        public string SpellType { get; set; } = string.Empty;
        public string CastTarget { get; set; } = string.Empty;
        public List<string> StackRequestTargets = new List<string>();
        public long StackIntervalCheck { get; set; } = 10000;
        public long StackIntervalNextCheck = 0;
        public long StackRecastDelay { get; set; } = 0;
        public string StackRequestItem { get; set; } = string.Empty;
        public readonly Dictionary<string, long> StackSpellCooldown = new Dictionary<string, long>();
        public bool GiftOfMana { get; set; }
        public int SpellID { get; set; }
        public int PctAggro { get; set; }
        public string Zone { get; set; } = "All";
        public int MinSick { get; set; } = 2;
        public bool AllowSpellSwap { get; set; }
        public bool NoEarlyRecast { get; set; }
        public bool NoStack { get; set; }
        public string TriggerSpell { get; set; } = string.Empty;
        public string BeforeSpell { get; set; } = string.Empty;
        public Spell BeforeSpellData { get; set; }
        public string AfterSpell { get; set; } = string.Empty;
        public Spell AfterSpellData { get; set; }
        public bool NoInterrupt { get; set; }
        public string AfterEvent { get; set; } = string.Empty;
        public string BeforeEvent { get; set; } = string.Empty;
        public string CastIF { get; set; } = string.Empty;
        public string Ifs { get; set; } = string.Empty;
        public string InitName { get; set; } = string.Empty;
        public bool ReagentOutOfStock { get; set; }
        public bool SpellInBook { get; set; }
        public bool NoMidSongCast { get; set; }
        public long MinDurationBeforeRecast { get; set; }
        public long LastUpdateCheckFromTopicUpdate { get; set; }
        public bool IsShortBuff { get; set; }
        public int HealthMax { get; set; } = 100;

        // Used for item clicks, where the click effect is from an aug.
        //  This option lets E3Next resolve the correct spell effect.
        //  Augs are 1-based, so < 1 means ignore
        public int AugSlot { get; set; } = 0;

        // Indicates cast routines should use /useitem instead of /casting
        //  Work-around for casting not always working
        public bool UseItem { get; set; }

        // Used to specify a recast time when its not queryable from MQ2 
        //  and the lookup dictionary doesn't work.
        public int ItemRecast { get; set; }

        public Spell(string spellName, IniData parsedData = null)
        {
            SpellName = spellName; //what the thing actually casts
            CastName = spellName;//required to send command
            InitName = spellName;

            Parse(parsedData);

            QueryMQ();

            if (this.SpellID > 0 && !_loadedSpells.ContainsKey(this.SpellID))
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

        private void Parse(IniData parsedData)
        {
            if (SpellName.Contains("/"))
            {
                string[] splitList = SpellName.Split('/');
                SpellName = splitList[0];
                CastName = SpellName;
                int counter = 0;
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
                        SpellGem = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("AugSlot|", StringComparison.OrdinalIgnoreCase))
                    {
                        AugSlot = GetArgument<int>(value);
                        if (AugSlot < 0)
                        {
                            E3.Bots.Broadcast($"Spell parameters for {CastName} have requested an invalid AugSlot {AugSlot}. Ignoring AugSlot.");
                            e3util.Beep();
                            AugSlot = 0;
                        }
                    }
                    else if (value.Equals("UseItem", StringComparison.OrdinalIgnoreCase))
                    {
                        UseItem = true;
                    }
                    else if (value.Equals("NoInterrupt", StringComparison.OrdinalIgnoreCase))
                    {
                        NoInterrupt = true;
                    }
                    else if (value.Equals("ItemRecast", StringComparison.OrdinalIgnoreCase))
                    {
                        // Assume milliseconds unless s or m suffix specified
                        string tvalue = value;
                        int multiplier = 1;

                        if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase))
                        {
                            tvalue = tvalue.Substring(0, value.Length - 1);
                            multiplier = 1000; // 1000ms per sec
                        }
                        else if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase))
                        {
                            tvalue = tvalue.Substring(0, value.Length - 1);
                            multiplier = 1000 * 60; // 1000ms per sec, 60s / min
                        }

                        ItemRecast = GetArgument<int>(tvalue);
                        ItemRecast *= multiplier;

                        // TODO - Validate recast time, must be >= 0
                        // TODO - Refactor "Delay" type values into uniform func
                    }
                    else if (value.Equals("Debug", StringComparison.OrdinalIgnoreCase))
                    {
                        Debug = true;
                    }
                    else if (value.Equals("HealthMax|", StringComparison.OrdinalIgnoreCase))
                    {
                        HealthMax = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("AfterSpell|", StringComparison.OrdinalIgnoreCase))
                    {
                        AfterSpell = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("StackRequestTargets|", StringComparison.OrdinalIgnoreCase))
                    {
                        string targetString = GetArgument<string>(value);
                        string[] targets = targetString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var target in targets)
                        {
                            StackRequestTargets.Add(e3util.FirstCharToUpper(target.Trim()));
                        }
                        //StackIntervalCheck
                    }
                    else if (value.StartsWith("StackCheckInterval|", StringComparison.OrdinalIgnoreCase))
                    {
                        StackIntervalCheck = GetArgument<long>(value);
                        StackIntervalCheck *= 1000;
                    }
                    else if (value.StartsWith("StackRecastDelay|", StringComparison.OrdinalIgnoreCase))
                    {
                        StackRecastDelay = GetArgument<long>(value);
                        StackRecastDelay *= 1000;
                    }
                    else if (value.StartsWith("StackRequestItem|", StringComparison.OrdinalIgnoreCase))
                    {
                        StackRequestItem = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("AfterCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        AfterSpell = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("BeforeSpell|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeSpell = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("MinDurationBeforeRecast|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinDurationBeforeRecast = GetArgument<long>(value) * 1000;
                    }
                    else if (value.StartsWith("BeforeCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeSpell = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("GiveUpTimer|", StringComparison.OrdinalIgnoreCase))
                    {
                        GiveUpTimer = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("MaxTries|", StringComparison.OrdinalIgnoreCase))
                    {
                        MaxTries = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("CheckFor|", StringComparison.OrdinalIgnoreCase))
                    {
                        string checkFors = GetArgument<string>(value);
                        string[] checkForItems = checkFors.Split(',');

                        foreach (var checkFor in checkForItems)
                        {
                            if (!CheckForCollection.ContainsKey(checkFor.Trim()))
                            {
                                CheckForCollection.Add(checkFor.Trim(), 0);
                            }
                        }
                    }
                    else if (value.StartsWith("CastIf|", StringComparison.OrdinalIgnoreCase))
                    {
                        CastIF = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("MinMana|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinMana = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("MaxMana|", StringComparison.OrdinalIgnoreCase))
                    {
                        MaxMana = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("MinHP|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinHP = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("HealPct|", StringComparison.OrdinalIgnoreCase))
                    {
                        HealPct = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("Reagent|", StringComparison.OrdinalIgnoreCase))
                    {
                        Reagent = GetArgument<string>(value);
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
                    else if (value.Equals("NoMidSongCast", StringComparison.OrdinalIgnoreCase))
                    {
                        NoMidSongCast = true;
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

                        Delay = GetArgument<int>(tvalue);
                        if (isMinute)
                        {
                            Delay *= 60;
                        }
                    }
                    else if (value.StartsWith("DelayAfterCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        DelayAfterCast = GetArgument<int>(value);
                    }
                    else if (value.Equals("GoM", StringComparison.OrdinalIgnoreCase))
                    {
                        GiftOfMana = true;
                    }
                    else if (value.StartsWith("PctAggro|", StringComparison.OrdinalIgnoreCase))
                    {
                        PctAggro = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("Zone|", StringComparison.OrdinalIgnoreCase))
                    {
                        Zone = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("MinSick|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinSick = GetArgument<int>(value);
                    }
                    else if (value.StartsWith("MinEnd|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinEnd = GetArgument<int>(value);
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
                        TriggerSpell = GetArgument<string>(value);
                    }
                    else if (value.StartsWith("Ifs|", StringComparison.OrdinalIgnoreCase))
                    {
                        string ifKey = GetArgument<string>(value);
                        var section = parsedData.Sections["Ifs"];
                        if (section != null)
                        {
                            var keys = ifKey.Split(','); // Splitting based on comma
                            foreach (var key in keys)
                            {
                                var keyData = section[key];
                                if (!string.IsNullOrWhiteSpace(keyData))
                                {
                                    Ifs = string.IsNullOrWhiteSpace(Ifs) ? keyData : Ifs + " && " + keyData;
                                }
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
                            if (!string.IsNullOrWhiteSpace(keyData))
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
                            if (!string.IsNullOrWhiteSpace(keyData))
                            {
                                BeforeEvent = keyData;
                            }
                        }
                    }
                    else
                    {
                        //doesn't match anything, so we assume its the target for the 1st one
                        if (string.IsNullOrWhiteSpace(CastTarget) && !value.Contains("|"))
                        {
                            CastTarget = e3util.FirstCharToUpper(value);
                        }
                    }
                }
            }
        }


        private void QueryMQ()
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
            else if (MQ.Query<bool>($"${{Me.Ability[{CastName}]}}") || string.Compare("Slam", CastName, true) == 0)
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
                int invSlot;
                int bagSlot;
                //check if this is an itemID

                int itemID = -1;

                if (int.TryParse(CastName, out itemID))
                {
                    invSlot = MQ.Query<int>($"${{FindItem[{CastName}].ItemSlot}}");
                    bagSlot = MQ.Query<int>($"${{FindItem[{CastName}].ItemSlot2}}");
                }
                else
                {
                    invSlot = MQ.Query<int>($"${{FindItem[={CastName}].ItemSlot}}");
                    bagSlot = MQ.Query<int>($"${{FindItem[={CastName}].ItemSlot2}}");
                }

                string queryRoot = $"${{Me.Inventory[{invSlot}]";
                if (AugSlot > 0)
                {
                    // AugSlot is 1-based
                    int augSlots = MQ.Query<int>($"{queryRoot}.Augs}}");
                    if (AugSlot <= augSlots)
                    {
                        queryRoot += $".Item[{AugSlot}].Clicky";
                    }
                    else
                    {
                        E3.Bots.Broadcast($"Spell parameters for {CastName} have requested an AugSlot {AugSlot} that is higher than item's available slots. Ignoring AugSlot.");
                        e3util.Beep();
                        AugSlot = 0;
                    }
                }

                if (bagSlot == -1)
                {
                    //Means this is not in a bag and in the root inventory, OR we are wearing it
                    TargetType = MQ.Query<string>($"{queryRoot}.Spell.TargetType}}");
                    Duration = MQ.Query<int>($"{queryRoot}.Spell.Duration}}");
                    DurationTotalSeconds = MQ.Query<int>($"{queryRoot}.Spell.Duration.TotalSeconds}}");
                    RecastTime = MQ.Query<int>($"{queryRoot}.Spell.RecastTime}}");
                    RecoveryTime = MQ.Query<decimal>($"{queryRoot}.Spell.RecoveryTime}}");
                    MyCastTime = MQ.Query<decimal>($"{queryRoot}.Spell.CastTime}}");

                    double AERange = MQ.Query<double>($"{queryRoot}.Spell.AERange}}");
                    MyRange = AERange;
                    if (MyRange == 0)
                    {
                        MyRange = MQ.Query<double>($"{queryRoot}.Spell.MyRange}}");
                    }

                    string et = MQ.Query<string>($"{queryRoot}.EffectType}}");

                    if (et.Equals("Click Worn", StringComparison.OrdinalIgnoreCase))
                    {
                        ItemMustEquip = true;
                    }

                    SpellName = MQ.Query<string>($"{queryRoot}.Spell}}");
                    SpellID = MQ.Query<int>($"{queryRoot}.Spell.ID}}");
                    CastID = MQ.Query<int>($"${{Me.Inventory[{invSlot}].ID}}"); // Item Id
                    SpellType = MQ.Query<string>($"{queryRoot}.Spell.SpellType}}");
                    IsShortBuff = MQ.Query<bool>($"{queryRoot}.Spell.DurationWindow}}");
                }
                else
                {
                    //1 index vs 0 index
                    bagSlot += 1;

                    queryRoot += $".Item[{bagSlot}]";

                    TargetType = MQ.Query<string>($"{queryRoot}.Spell.TargetType}}");
                    Duration = MQ.Query<int>($"{queryRoot}.Spell.Duration}}");
                    DurationTotalSeconds = MQ.Query<int>($"{queryRoot}.Spell.Duration.TotalSeconds}}");

                    RecastTime = MQ.Query<int>($"{queryRoot}.Spell.RecastTime}}");
                    RecoveryTime = MQ.Query<decimal>($"{queryRoot}.Spell.RecoveryTime}}");
                    MyCastTime = MQ.Query<decimal>($"{queryRoot}.CastTime}}");

                    double AERange = MQ.Query<double>($"{queryRoot}.Spell.AERange}}");
                    MyRange = AERange;
                    if (MyRange == 0)
                    {
                        MyRange = MQ.Query<double>($"{queryRoot}.Spell.MyRange}}");
                    }

                    string et = MQ.Query<string>($"{queryRoot}.EffectType}}");
                    if (et.Equals("Click Worn", StringComparison.OrdinalIgnoreCase))
                    {
                        ItemMustEquip = true;
                    }

                    SpellName = MQ.Query<string>($"{queryRoot}.Spell}}");
                    SpellID = MQ.Query<int>($"{queryRoot}.Spell.ID}}");
                    CastID = MQ.Query<int>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].ID}}"); // Item ID
                    SpellType = MQ.Query<string>($"{queryRoot}.Spell.SpellType}}");
                    IsShortBuff = MQ.Query<bool>($"{queryRoot}.Spell.DurationWindow}}");
                }
            }
            else if (CastType == CastType.AA)
            {
                TargetType = MQ.Query<string>($"${{Me.AltAbility[{CastName}].Spell.TargetType}}");
                Duration = MQ.Query<int>($"${{Me.AltAbility[{CastName}].Spell.Duration}}");
                DurationTotalSeconds = MQ.Query<int>($"${{Me.AltAbility[{CastName}].Spell.Duration.TotalSeconds}}");

                RecastTime = MQ.Query<int>($"${{Me.AltAbility[{CastName}].ReuseTime}}");
                RecoveryTime = MQ.Query<decimal>($"${{Me.AltAbility[{CastName}].Spell.RecoveryTime}}");
                MyCastTime = MQ.Query<decimal>($"${{Me.AltAbility[{CastName}].Spell.MyCastTime}}");

                double AERange = MQ.Query<double>($"${{Me.AltAbility[{CastName}].Spell.AERange}}");
                MyRange = MQ.Query<double>($"${{Me.AltAbility[{CastName}].Spell.MyRange}}");
                SpellType = MQ.Query<string>($"${{Spell[{CastName}].SpellType}}");

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
                int tmpMana = MQ.Query<int>($"${{Me.AltAbility[{CastName}].Spell.Mana}}");
                if (tmpMana > 0)
                {
                    Mana = tmpMana;
                }
                SpellName = MQ.Query<string>($"${{Me.AltAbility[{CastName}].Spell}}");
                SpellID = MQ.Query<int>($"${{Me.AltAbility[{CastName}].Spell.ID}}");
                CastID = MQ.Query<int>($"${{Me.AltAbility[{CastName}].ID}}");
                IsShortBuff = MQ.Query<bool>($"${{Me.AltAbility[{CastName}].Spell.DurationWindow}}");
            }
            else if (CastType == CastType.Spell)
            {
                if (SpellInBook)
                {
                    string bookNumber = MQ.Query<string>($"${{Me.Book[{CastName}]}}");

                    TargetType = MQ.Query<string>($"${{Me.Book[{bookNumber}].TargetType}}");
                    Duration = MQ.Query<int>($"${{Me.Book[{bookNumber}].Duration}}");
                    DurationTotalSeconds = MQ.Query<int>($"${{Me.Book[{bookNumber}].Duration.TotalSeconds}}");

                    RecastTime = MQ.Query<int>($"${{Me.Book[{bookNumber}].RecastTime}}");
                    RecoveryTime = MQ.Query<decimal>($"${{Me.Book[{bookNumber}].RecoveryTime}}");
                    MyCastTime = MQ.Query<decimal>($"${{Me.Book[{bookNumber}].MyCastTime}}");

                    double AERange = MQ.Query<double>($"${{Me.Book[{bookNumber}].AERange}}");
                    MyRange = MQ.Query<double>($"${{Me.Book[{bookNumber}].MyRange}}");
                    SpellType = MQ.Query<string>($"${{Me.Book[{bookNumber}].SpellType}}");
                    IsShortBuff = MQ.Query<bool>($"${{Me.Book[{bookNumber}].DurationWindow}}");

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
                    Mana = MQ.Query<int>($"${{Me.Book[{bookNumber}].Mana}}");
                    SpellName = CastName;
                    SpellID = MQ.Query<int>($"${{Me.Book[{bookNumber}].ID}}");
                    CastID = SpellID;
                }
                else
                {
                    TargetType = MQ.Query<string>($"${{Spell[{CastName}].TargetType}}");
                    Duration = MQ.Query<int>($"${{Spell[{CastName}].Duration}}");
                    DurationTotalSeconds = MQ.Query<int>($"${{Spell[{CastName}].Duration.TotalSeconds}}");

                    RecastTime = MQ.Query<int>($"${{Spell[{CastName}].RecastTime}}");
                    RecoveryTime = MQ.Query<decimal>($"${{Spell[{CastName}].RecoveryTime}}");
                    MyCastTime = MQ.Query<decimal>($"${{Spell[{CastName}].MyCastTime}}");

                    double AERange = MQ.Query<double>($"${{Spell[{CastName}].AERange}}");
                    MyRange = MQ.Query<double>($"${{Spell[{CastName}].MyRange}}");
                    SpellType = MQ.Query<string>($"${{Spell[{CastName}].SpellType}}");
                    IsShortBuff = MQ.Query<bool>($"${{Spell[{CastName}].DurationWindow}}");

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
                    Mana = MQ.Query<int>($"${{Spell[{CastName}].Mana}}");
                    SpellName = CastName;
                    SpellID = MQ.Query<int>($"${{Spell[{CastName}].ID}}");
                    CastID = SpellID;
                }
            }
            else if (CastType == CastType.Disc)
            {
                TargetType = MQ.Query<string>($"${{Spell[{CastName}].TargetType}}");
                Duration = MQ.Query<int>($"${{Spell[{CastName}].Duration}}");
                DurationTotalSeconds = MQ.Query<int>($"${{Spell[{CastName}].Duration.TotalSeconds}}");
                EnduranceCost = MQ.Query<int>($"${{Spell[{CastName}].EnduranceCost}}");
                double AERange = MQ.Query<double>($"${{Spell[{CastName}].AERange}}");
                MyRange = AERange;
                if (MyRange == 0)
                {
                    MyRange = MQ.Query<double>($"${{Spell[{CastName}].MyRange}}");
                }
                SpellName = CastName;
                SpellID = MQ.Query<int>($"${{Spell[{CastName}].ID}}");
                CastID = SpellID;
                SpellType = MQ.Query<string>($"${{Spell[{CastName}].SpellType}}");
                IsShortBuff = MQ.Query<bool>($"${{Spell[{CastName}].DurationWindow}}");
            }
            else if (CastType == CastType.Ability)
            {
                //nothing to update here
            }

            foreach (string key in CheckForCollection.Keys.ToList())
            {
                int tcID = 0;
                if (MQ.Query<bool>($"${{Bool[${{AltAbility[{key}].Spell}}]}}"))
                {
                    tcID = MQ.Query<int>($"${{AltAbility[{key}].Spell.ID}}");
                }
                else if (MQ.Query<bool>($"${{Bool[${{Spell[{key}].ID}}]}}"))
                {
                    tcID = MQ.Query<int>($"${{Spell[{key}].ID}}");
                }
                CheckForCollection[key] = tcID;
            }
        }

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
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        //override public String ToString()
        //{

        //    string returnString = $"spell:{SpellName} spellid:{SpellID} castid: {CastID} mana:{Mana} spelltype:{SpellType} targettype:{TargetType} Duration:{Duration} RecastTime:{RecastTime} RecoveryTime:{RecoveryTime} MyCastTime:{MyCastTime} MyRange:{MyRange} MustEquip:{ItemMustEquip} ";

        //    return returnString;
        //}
    }
}

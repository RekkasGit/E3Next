using E3Core.Processors;
using E3Core.Utility;
using IniParser.Model;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static Dictionary<Int32, Data.Spell> _loadedSpells = new Dictionary<int, Spell>();
        public static Dictionary<string, Data.Spell> LoadedSpellsByName = new Dictionary<string, Spell>();
        public static Dictionary<string, Data.Spell> LoadedSpellByConfigEntry = new Dictionary<string, Data.Spell>();
		static Dictionary<string, Int32> _spellIDLookup = new Dictionary<string, Int32>();
		public static IMQ MQ = E3.MQ;


        public static Int32 SpellIDLookup(string spellName)
        {
            if(_spellIDLookup.TryGetValue(spellName, out var result))
            {
                return result;
			}
            Int32 spellID = MQ.Query<Int32>($"${{Spell[{spellName}].ID}}");
            if(spellID>0)
            {
				_spellIDLookup.Add(spellName, spellID);
			}
			return spellID;

		}
        //only used for seralization
        public Spell()
        {

        }
        public Spell(string spellName, IniData parsedData = null)
        {

            if(!LoadedSpellByConfigEntry.ContainsKey(spellName))
            {
                LoadedSpellByConfigEntry.Add(spellName, this);
            }

            SpellName = spellName; //what the thing actually casts
            CastName = spellName;//required to send command
            InitName = spellName;

            Parse(parsedData);



            QueryMQ();
            if (this.SpellID>0 && !_loadedSpells.ContainsKey(this.SpellID))
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
					else if (value.Equals("IsDoT", StringComparison.OrdinalIgnoreCase))
					{
						IsDoT = true;
					}
					else if (value.Equals("IsDebuff", StringComparison.OrdinalIgnoreCase))
					{
						IsDebuff = true;
					}
					else if (value.Equals("Debug", StringComparison.OrdinalIgnoreCase))
					{
						Debug = true;
					}
					else if (value.Equals("IgnoreStackRules", StringComparison.OrdinalIgnoreCase))
					{
						IgnoreStackRules = true;
					}
					else if (value.StartsWith("HealthMax|", StringComparison.OrdinalIgnoreCase))
					{
						HealthMax = GetArgument<Int32>(value);
					}
					else if (value.StartsWith("AfterSpell|", StringComparison.OrdinalIgnoreCase))
                    {
                        AfterSpell = GetArgument<String>(value);
                    }
					else if (value.StartsWith("StackRequestTargets|", StringComparison.OrdinalIgnoreCase))
					{
                        string targetString = GetArgument<String>(value);
                        string[] targets = targetString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach(var target in targets)
                        {
                            StackRequestTargets.Add(e3util.FirstCharToUpper(target.Trim()));
                        }
						//StackIntervalCheck
					}
					else if (value.StartsWith("StackCheckInterval|", StringComparison.OrdinalIgnoreCase))
					{
						StackIntervalCheck = GetArgument<Int64>(value);
                        StackIntervalCheck *= 1000;
					}
					else if (value.StartsWith("StackRecastDelay|", StringComparison.OrdinalIgnoreCase))
					{
						StackRecastDelay = GetArgument<Int64>(value);
						StackRecastDelay *= 1000;
					}
					else if (value.StartsWith("StackRequestItem|", StringComparison.OrdinalIgnoreCase))
					{
						StackRequestItem = GetArgument<String>(value);
					}
					else if (value.StartsWith("AfterCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        AfterSpell = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("BeforeSpell|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeSpell = GetArgument<String>(value);
                    }
					else if (value.StartsWith("MinDurationBeforeRecast|", StringComparison.OrdinalIgnoreCase))
					{
						MinDurationBeforeRecast = GetArgument<Int64>(value) *1000;
					}
					else if (value.StartsWith("BeforeCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeSpell = GetArgument<String>(value);
                    }
                    else if (value.StartsWith("GiveUpTimer|", StringComparison.OrdinalIgnoreCase))
                    {
                        GiveUpTimer = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("MaxTries|", StringComparison.OrdinalIgnoreCase))
                    {
                        MaxTries = GetArgument<Int32>(value);
                    }
                    else if (value.StartsWith("CheckFor|", StringComparison.OrdinalIgnoreCase))
					{
                        string checkFors = GetArgument<String>(value);
                        string[] checkForItems = checkFors.Split(',');

                        foreach(var checkFor in checkForItems)
                        {
                            if(!CheckForCollection.ContainsKey(checkFor.Trim()))
                            {
								CheckForCollection.Add(checkFor.Trim(), 0);
							}
                        }
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
                    else if (value.Equals("NoTarget", StringComparison.OrdinalIgnoreCase))
                    {
                        NoTarget = true;
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

                        Delay = GetArgument<Int32>(tvalue);
                        if (isMinute)
                        {
                            Delay = Delay * 60;
                        }

                    }
					else if (value.StartsWith("DelayAfterCast|", StringComparison.OrdinalIgnoreCase))
					{
						
						DelayAfterCast = GetArgument<Int32>(value);
						
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
                            var keys = ifKey.Split(','); // Splitting based on comma
                            foreach (var key in keys)
                            {
                                var keyData = section[key];
                                if (!String.IsNullOrWhiteSpace(keyData))
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
            else if (MQ.Query<bool>($"${{Me.Ability[{CastName}]}}")|| String.Compare("Slam",CastName,true)==0)
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
                    SpellIcon = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.SpellIcon}}");
					SpellType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell.SpellType}}");
					IsShortBuff = MQ.Query<bool>($"${{Me.Inventory[{invSlot}].Spell.DurationWindow}}");
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
                    SpellIcon = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.SpellIcon}}");
					SpellType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.SpellType}}");
					IsShortBuff = MQ.Query<bool>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.DurationWindow}}");
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
				SpellIcon = MQ.Query<Int32>($"${{Spell[{CastName}].SpellIcon}}");

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
                int tmpMana = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].Spell.Mana}}");
                if(tmpMana>0)
                {
                    Mana = tmpMana;
                }
                SpellName = MQ.Query<String>($"${{Me.AltAbility[{CastName}].Spell}}");
                SpellID = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].Spell.ID}}");
                CastID = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].ID}}");
				IsShortBuff = MQ.Query<bool>($"${{Me.AltAbility[{CastName}].Spell.DurationWindow}}");
                Category = MQ.Query<String>($"${{Me.AltAbility[{CastName}].Spell.Category}}");
                Subcategory = MQ.Query<String>($"${{Me.AltAbility[{CastName}].Spell.Subcategory}}");
			}
			else if (CastType == CastType.Spell)
            {
              
                if(SpellInBook)
                {
                    string bookNumber = MQ.Query<string>($"${{Me.Book[{CastName}]}}");

                    TargetType = MQ.Query<String>($"${{Me.Book[{bookNumber}].TargetType}}");
                    Duration = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].Duration}}");
                    DurationTotalSeconds = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].Duration.TotalSeconds}}");

                    RecastTime = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].RecastTime}}");
                    RecoveryTime = MQ.Query<Decimal>($"${{Me.Book[{bookNumber}].RecoveryTime}}");
                    MyCastTime = MQ.Query<Decimal>($"${{Me.Book[{bookNumber}].MyCastTime}}");

                    Double AERange = MQ.Query<Double>($"${{Me.Book[{bookNumber}].AERange}}");
                    MyRange = MQ.Query<double>($"${{Me.Book[{bookNumber}].MyRange}}");
                    SpellType = MQ.Query<String>($"${{Me.Book[{bookNumber}].SpellType}}");
                    IsShortBuff = MQ.Query<bool>($"${{Me.Book[{bookNumber}].DurationWindow}}");
                    Subcategory = MQ.Query<string>($"${{Me.Book[{bookNumber}].Subcategory}}");
                    Category = MQ.Query<string>($"${{Me.Book[{bookNumber}].Category}}");
                   

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
                        if (AERange > 0 && Subcategory != "Calm")
                        {
                            MyRange = AERange;
                        }

                    }
                    Mana = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].Mana}}");
                    SpellName = CastName;
                    SpellID = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].ID}}");
                    CastID = SpellID;
                    SpellIcon = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].SpellIcon}}");
					Level = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].Level}}");
				}
                else
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
                    IsShortBuff = MQ.Query<bool>($"${{Spell[{CastName}].DurationWindow}}");
                    Subcategory = MQ.Query<string>($"${{Spell[{CastName}].Subcategory}}");
                    Category = MQ.Query<string>($"${{Spell[{CastName}].Category}}");
                    SpellIcon = MQ.Query<Int32>($"${{Spell[{CastName}].SpellIcon}}");
                    Level = MQ.Query<Int32>($"${{Spell[{CastName}].Level}}");
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
                        if (AERange > 0 && Subcategory != "Calm")
                        {
                            MyRange = AERange;
                        }

                    }
                    Mana = MQ.Query<Int32>($"${{Spell[{CastName}].Mana}}");
                    SpellName = CastName;
                    SpellID = MQ.Query<Int32>($"${{Spell[{CastName}].ID}}");
                    CastID = SpellID;

                }

                
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
				IsShortBuff = MQ.Query<bool>($"${{Spell[{CastName}].DurationWindow}}");
                SpellIcon = MQ.Query<Int32>($"${{Spell[{CastName}].SpellIcon}}");


			}
			else if (CastType == CastType.Ability)
            {
                //nothing to update here
            }

            foreach(string key in CheckForCollection.Keys.ToList())
            {
                Int32 tcID = 0;
				if (MQ.Query<bool>($"${{Bool[${{AltAbility[{key}].Spell}}]}}"))
				{
					tcID = MQ.Query<Int32>($"${{AltAbility[{key}].Spell.ID}}");
				}
				else if (MQ.Query<bool>($"${{Bool[${{Spell[{key}].ID}}]}}"))
				{
					tcID = MQ.Query<Int32>($"${{Spell[{key}].ID}}");
				}
                CheckForCollection[key] = tcID;
            }

        }
        //override public String ToString()
        //{

        //    string returnString = $"spell:{SpellName} spellid:{SpellID} castid: {CastID} mana:{Mana} spelltype:{SpellType} targettype:{TargetType} Duration:{Duration} RecastTime:{RecastTime} RecoveryTime:{RecoveryTime} MyCastTime:{MyCastTime} MyRange:{MyRange} MustEquip:{ItemMustEquip} ";

        //    return returnString;
        //}
        public String Subcategory = String.Empty;
        public String Category = String.Empty;
        public String SpellName = String.Empty;//the spell's name. If the item clicks, this is the spell it casts
        public String CastName = String.Empty;//this can be the item, spell, aa, disc. What is required to cast it. 
        public CastType CastType;
        public String TargetType = String.Empty;
        public Int32 SpellGem;
        public Int32 GiveUpTimer;
        public Int32 MaxTries = 5;
        public Dictionary<string, Int32> CheckForCollection = new Dictionary<string, int>();
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
        public bool Debug;
        public String Reagent = String.Empty;
        public Boolean ItemMustEquip;
        public Boolean NoBurn;
        public Boolean NoTarget;
        public Boolean NoAggro;
        public Int32 Mode;
        public Boolean Rotate;
        public Int32 EnduranceCost;
        public Int32 Delay;
        public Int32 DelayAfterCast = 0;
        public Int32 CastID;
        public Int32 MinEnd;
        public Boolean CastInvis;
        public String SpellType = String.Empty;
        public String CastTarget = String.Empty;
        public List<string> StackRequestTargets = new List<string>();
        public Int64 StackIntervalCheck = 10000;
        public Int64 StackIntervalNextCheck = 0;
        public Int64 StackRecastDelay = 0;
        public string StackRequestItem = String.Empty;
		public Dictionary<string, Int64> StackSpellCooldown = new Dictionary<string, long>();
		public Boolean GiftOfMana;
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
        public Int32 SpellIcon = 0;
        public bool NoMidSongCast = false;
        public Int64 MinDurationBeforeRecast = 0;
        public Int64 LastUpdateCheckFromTopicUpdate = 0;
        public bool IsShortBuff = false;
        public Int32 HealthMax = 100;
        public bool IgnoreStackRules = false;
        public bool IsDebuff = false;
        public bool IsDoT = false;
        public Int32 Level = 255;

        public SpellData ToProto()
        {

            SpellData r = new SpellData();
            r.AfterEvent = this.AfterEvent;
            r.AfterSpell = this.AfterSpell;
            r.AllowSpellSwap = this.AllowSpellSwap;
            r.BeforeEvent = this.BeforeEvent;
            r.BeforeSpell = this.BeforeSpell;
            r.CastID = this.CastID;
            r.CastIF = this.CastIF;
            r.CastInvis = this.CastInvis;
            r.CastName = this.CastName;
            r.CastTarget = this.CastTarget;
            r.CastType = (SpellData.Types.CastingType)this.CastType;
            r.Category = this.Category;
            r.Debug = this.Debug;
            r.Delay = this.Delay;
            r.DelayAfterCast = this.DelayAfterCast;
            r.Duration = this.Duration;
            r.DurationTotalSeconds = this.DurationTotalSeconds;
            r.EnduranceCost = this.EnduranceCost;
            r.GiftOfMana = this.GiftOfMana;
            r.GiveUpTimer = this.GiveUpTimer;
            r.HealPct = this.HealPct;
            r.HealthMax = this.HealthMax;
            r.Ifs=  this.Ifs;
            r.IgnoreStackRules = this.IgnoreStackRules;
            r.InitName = this.InitName;
            r.IsDebuff = this.IsDebuff;
            r.IsDoT= this.IsDoT;
            r.IsShortBuff = this.IsShortBuff;
            r.ItemMustEquip = this.ItemMustEquip;
            r.Mana= this.Mana;
            r.MaxMana= this.MaxMana;
            r.MaxTries = this.MaxTries;
            r.MinDurationBeforeRecast = this.MinDurationBeforeRecast;
            r.MinEnd = this.MinEnd;
            r.MinHP = this.MinHP;
            r.MinMana = this.MinMana;
            r.MinSick = this.MinSick;
            r.Mode = this.Mode;
            r.MyCastTime = (double)this.MyCastTime;
            r.MyCastTimeInSeconds = (double)this.MyCastTimeInSeconds;
            r.MyRange= this.MyRange; 
            r.NoAggro = this.NoAggro;
            r.NoBurn = this.NoBurn;
            r.NoEarlyRecast = this.NoEarlyRecast;
            r.NoInterrupt = this.NoInterrupt;
            r.NoMidSongCast = this.NoMidSongCast;
            r.NoStack = this.NoStack;
            r.NoTarget = this.NoTarget;
            r.PctAggro = this.PctAggro;
            r.Reagent = this.Reagent;
            r.ReagentOutOfStock = this.ReagentOutOfStock;
            r.RecastTime= this.RecastTime;
            r.RecoveryTime = (double)this.RecoveryTime;
            r.Rotate = this.Rotate;
            r.SpellGem = this.SpellGem;
            r.SpellIcon = this.SpellIcon;
            r.SpellID = this.SpellID;
            r.SpellInBook = this.SpellInBook;
            r.SpellName = this.SpellName;
            r.SpellType = this.SpellType;
            r.StackRecastDelay = this.StackRecastDelay;
            r.StackRequestItem = this.StackRequestItem;
            r.StackRequestTargets.AddRange(this.StackRequestTargets);
            r.Subcategory = this.Subcategory;
            r.TargetType = this.TargetType;
            r.TriggerSpell =this.TriggerSpell;
            r.Zone = this.Zone;
            r.Level = this.Level;
            return r;

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
                sb.Append(System.Environment.NewLine);
               
            }
            return sb.ToString();
        }
    }
}

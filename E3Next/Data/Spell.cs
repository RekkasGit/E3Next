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


	public enum CastingType
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

		//these can be set to use these lookup spells, mainly used for the config editor so that we don't have to query MQ in a chatty fashion
		public static Dictionary<string, SpellData> SpellDataLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<string, SpellData> AltDataLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<string, SpellData> DiscDataLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);
		public static Dictionary<string, SpellData> ItemDataLookup = new Dictionary<string, SpellData>(StringComparer.OrdinalIgnoreCase);

		static Dictionary<string, Int32> _spellIDLookup = new Dictionary<string, Int32>();
		public static IMQ MQ = E3.MQ;
        //mainly to deal with temp items that you might not have but specified in your ini
        public bool Initialized = false;
        

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
					else if(value.StartsWith("CastType|", StringComparison.OrdinalIgnoreCase))
					{
						string castTypeAsString = GetArgument<String>(value);
						Enum.TryParse<CastingType>(castTypeAsString, true, out this.CastTypeOverride);
					}
					else if (value.Equals("Debug", StringComparison.OrdinalIgnoreCase))
					{
						Debug = true;
					}
					else if (value.Equals("IgnoreStackRules", StringComparison.OrdinalIgnoreCase))
					{
						IgnoreStackRules = true;
					}
					else if (value.Equals("IgnoreStackRules", StringComparison.OrdinalIgnoreCase))
					{
						IgnoreStackRules = true;
					}
					else if (value.StartsWith("SongRefreshTime|", StringComparison.OrdinalIgnoreCase))
					{
						SongRefreshTime = GetArgument<Int32>(value);
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
					else if (value.StartsWith("BeforeCast|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeSpell = GetArgument<String>(value);
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
					else if (value.StartsWith("ExcludedClasses|", StringComparison.OrdinalIgnoreCase))
					{
						string excludeClassesString = GetArgument<String>(value);
						string[] excludeClasses = excludeClassesString.Split(',');

						foreach (var eclass in excludeClasses)
						{
							if (!ExcludedClasses.Contains(eclass.Trim()))
							{
								ExcludedClasses.Add(eclass.Trim());
							}
						}
					}
					else if (value.StartsWith("ExcludedNames|", StringComparison.OrdinalIgnoreCase))
					{
						string excludeNamesString = GetArgument<String>(value);
						string[] excludeNames = excludeNamesString.Split(',');

						foreach (var ename in excludeNames)
						{
							if (!ExcludedNames.Contains(ename.Trim()))
							{
								ExcludedNames.Add(ename.Trim());
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
					else if (value.StartsWith("MinHPTotal|", StringComparison.OrdinalIgnoreCase))
					{
						//mainly for shaman canni AA, should probably put it for all spell checks
						MinHPTotal = GetArgument<Int32>(value);
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
					else if (value.Equals("Disabled", StringComparison.OrdinalIgnoreCase))
					{
						Enabled = false;
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
					else if (value.StartsWith("RecastDelay|", StringComparison.OrdinalIgnoreCase))
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

						RecastDelay = GetArgument<Int32>(tvalue);
						if (isMinute)
						{
							RecastDelay = RecastDelay * 60;
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
					else if (value.StartsWith("DelayAfterCast|", StringComparison.OrdinalIgnoreCase))
					{
						AfterCastCompletedDelay = GetArgument<Int32>(value);
					}
					else if (value.StartsWith("AfterCastCompletedDelay|", StringComparison.OrdinalIgnoreCase))
					{

						AfterCastCompletedDelay = GetArgument<Int32>(value);

					}
					else if (value.StartsWith("AfterEventDelay|", StringComparison.OrdinalIgnoreCase))
					{
						AfterEventDelay = GetArgument<Int32>(value);
					}
					else if (value.StartsWith("BeforeEventDelay|", StringComparison.OrdinalIgnoreCase))
					{
						BeforeEventDelay = GetArgument<Int32>(value);
					}
					else if (value.StartsWith("AfterSpellDelay|", StringComparison.OrdinalIgnoreCase))
					{
						AfterSpellDelay = GetArgument<Int32>(value);
					}
					else if (value.StartsWith("BeforeSpellDelay|", StringComparison.OrdinalIgnoreCase))
					{
						BeforeSpellDelay = GetArgument<Int32>(value);
					}
					else if (value.StartsWith("AfterCastDelay|", StringComparison.OrdinalIgnoreCase))
					{
						AfterCastDelay = GetArgument<Int32>(value);
					}
					else if (value.StartsWith("MinEnd|", StringComparison.OrdinalIgnoreCase))
                    {
                        MinEnd = GetArgument<Int32>(value);
                    }
                    else if (value.Equals("AllowSpellSwap", StringComparison.OrdinalIgnoreCase))
                    {
                        AllowSpellSwap = true;
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
                    else if (parsedData!=null && value.StartsWith("Ifs|", StringComparison.OrdinalIgnoreCase))
                    {
                        IfsKeys = GetArgument<string>(value);
                        var section = parsedData.Sections["Ifs"];
                        if (section != null)
                        {
                            var keys = IfsKeys.Split(','); // Splitting based on comma
                            foreach (var key in keys)
                            {
                                var keyData = section[key];
                                if (!String.IsNullOrWhiteSpace(keyData))
                                {
                                    Ifs = string.IsNullOrWhiteSpace(Ifs) ? keyData : Ifs + " && " + keyData;
                                }
								else
								{
									//check the global ifs
									if(E3.GlobalIfs.Ifs.ContainsKey(key))
									{
										Ifs = string.IsNullOrWhiteSpace(Ifs) ? E3.GlobalIfs.Ifs[key] : Ifs + " && " + E3.GlobalIfs.Ifs[key];
									}
								}
                            }
                        }
                    }
                    else if (parsedData != null && value.StartsWith("AfterEvent|", StringComparison.OrdinalIgnoreCase))
                    {
                        AfterEventKeys = GetArgument<string>(value);
                        var section = parsedData.Sections["Events"];
                        if (section != null)
                        {
                            var keyData = section[AfterEventKeys];
                            if (!String.IsNullOrWhiteSpace(keyData))
                            {
                                AfterEvent = keyData;
                            }
                        }
                    }
                    else if (parsedData != null && value.StartsWith("BeforeEvent|", StringComparison.OrdinalIgnoreCase))
                    {
                        BeforeEventKeys = GetArgument<string>(value);
                        var section = parsedData.Sections["Events"];
                        if (section != null)
                        {
                            var keyData = section[BeforeEventKeys];
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
        //mainly to deal with temporary items
        public bool ReInit()
        {
            if(!Initialized)
            {
				QueryMQ();
         	}
			return Initialized;
        }
        void QueryMQ()
        {
            Initialized = true;
			if (CastTypeOverride == CastingType.None)
			{
				if (MQ.Query<bool>($"${{Me.AltAbility[{CastName}].Spell}}"))
				{
					CastType = CastingType.AA;
				}
				else if (MQ.Query<bool>($"${{Me.Book[{CastName}]}}"))
				{
					CastType = CastingType.Spell;
					SpellInBook = true;
				}
				else if (MQ.Query<bool>($"${{Me.CombatAbility[{CastName}]}}"))
				{
					CastType = CastingType.Disc;
				}
				else if (MQ.Query<bool>($"${{Me.Ability[{CastName}]}}") || String.Compare("Slam", CastName, true) == 0)
				{
					CastType = CastingType.Ability;
				}
				else if (MQ.Query<bool>($"${{FindItem[={CastName}]}}"))
				{

					CastType = CastingType.Item;
				}
				else if (MQ.Query<bool>($"${{Spell[{CastName}]}}"))
				{
					//final check to see if its a spell, that maybe a mob casts?
					CastType = CastingType.Spell;
				}
				else
				{
					//bad spell/item/etc
					CastType = CastingType.None;
				}
			}
			else
			{
				CastType = CastTypeOverride;
			}
           



            if (CastType == CastingType.Item)
            {
                Int32 invSlot;
                Int32 bagSlot;
                //check if this is an itemID

				//we already have this data populated, just kick out
                //used in the config editor to limit the number of calls.
				if(ItemDataLookup.ContainsKey(CastName))
				{
					var SpellData = ItemDataLookup[CastName];
					Spell.TransferSpellData(SpellData, this);
					goto gotoCheckCollectionPopulation;
				}
				
                Int32 itemID = -1;



                bool itemFound = MQ.Query<bool>($"${{FindItem[{CastName}]}}");

                if (!itemFound )
                {
                    //didn't find the item, cannot get information on it kick out
                    Initialized = false;
                    return;
                }


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
					MyRange = MQ.Query<Double>($"${{Me.Inventory[{invSlot}].Spell.MyRange}}");
					Description = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell.Description}}");
                    ResistType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell.ResistType}}");
					ResistAdj = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.ResistAdj}}");
					SpellType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell.SpellType}}");
					double AERange = MQ.Query<double>($"${{Me.Inventory[{invSlot}].Spell.AERange}}");
					Subcategory = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell.Subcategory}}");
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

					string et = MQ.Query<String>($"${{Me.Inventory[{invSlot}].EffectType}}");

                    if (et.Equals("Click Worn", StringComparison.OrdinalIgnoreCase))
                    {
                        ItemMustEquip = true;
                    }

                    SpellName = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Spell}}");
                    SpellID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.ID}}");
                    CastID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].ID}}");
                    SpellIcon = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Spell.SpellIcon}}");
				
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
					MyRange = MQ.Query<Double>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.MyRange}}");
					Description = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.Description}}");
					ResistType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.ResistType}}");
					ResistAdj = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.ResistAdj}}");
					SpellType = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.SpellType}}");

					double AERange = MQ.Query<double>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.AERange}}");
					Subcategory = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.Subcategory}}");
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

					SpellName = MQ.Query<String>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell}}");
                    SpellID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.ID}}");
                    CastID = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].ID}}");
                    SpellIcon = MQ.Query<Int32>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.SpellIcon}}");
					IsShortBuff = MQ.Query<bool>($"${{Me.Inventory[{invSlot}].Item[{bagSlot}].Spell.DurationWindow}}");
				}

            }
            else if (CastType == CastingType.AA)
            {
				//we already have this data populated, just kick out
				if (AltDataLookup.ContainsKey(CastName))
				{
					var SpellData = AltDataLookup[CastName];
					Spell.TransferSpellData(SpellData, this);

					goto gotoCheckCollectionPopulation;
				}
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
                Description = MQ.Query<String>($"${{Spell[{CastName}].Description}}");
				ResistType = MQ.Query<String>($"${{Me.AltAbility[{CastName}].Spell.ResistType}}");
				ResistAdj = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].Spell.ResistAdj}}");
				AAID = MQ.Query<Int32>($"${{Me.AltAbility[{CastName}].ID}}");

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
			else if (CastType == CastingType.Spell)
            {
				
				if (SpellInBook)
                {
					//we already have this data populated, just kick out
					if (SpellDataLookup.ContainsKey(CastName))
					{
						var SpellData = SpellDataLookup[CastName];
						Spell.TransferSpellData(SpellData, this);
						goto gotoCheckCollectionPopulation;
					}
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
                    Description= MQ.Query<string>($"${{Me.Book[{bookNumber}].Description}}");
					ResistType = MQ.Query<String>($"${{Me.Book[{bookNumber}].ResistType}}");
					ResistAdj = MQ.Query<Int32>($"${{Me.Book[{bookNumber}].ResistAdj}}");

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
                    Description = MQ.Query<string>($"${{Spell[{CastName}].Description}}");
					ResistType = MQ.Query<String>($"${{Spell[{CastName}].ResistType}}");
					ResistAdj = MQ.Query<Int32>($"${{Spell[{CastName}].ResistAdj}}");


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
            else if (CastType == CastingType.Disc)
            {
				//we already have this data populated, just kick out
				if (DiscDataLookup.ContainsKey(CastName))
				{
					var SpellData = DiscDataLookup[CastName];
					Spell.TransferSpellData(SpellData, this);
					goto gotoCheckCollectionPopulation;
				}
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
                Description = MQ.Query<String>($"${{Spell[{CastName}].Description}}");
				ResistType = MQ.Query<String>($"${{Spell[{CastName}].ResistType}}");
				ResistAdj = MQ.Query<Int32>($"${{Spell[{CastName}].ResistAdj}}");
                Level = MQ.Query<Int32>($"${{Spell[{CastName}].Level}}");
				Subcategory = MQ.Query<string>($"${{Spell[{CastName}].Subcategory}}");
				Category = MQ.Query<string>($"${{Spell[{CastName}].Category}}");

			}
			else if (CastType == CastingType.Ability)
            {
                //nothing to update here
            }
			gotoCheckCollectionPopulation:
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
		public CastingType CastType = CastingType.None;
		public CastingType CastTypeOverride = CastingType.None;
        public String TargetType = String.Empty;
        public Int32 SpellGem;
        public Int32 GiveUpTimer;
        private const Int32 MaxTriesDefault = 5;
        public Int32 MaxTries = MaxTriesDefault;
        public Dictionary<string, Int32> CheckForCollection = new Dictionary<string, int>();
		public HashSet<string> ExcludedClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		public HashSet<string> ExcludedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                if (CastType != CastingType.Ability)
                {
                    MyCastTimeInSeconds = value / 1000;
                }
            }
        }
		public Int32 AAID = 0;
        public decimal MyCastTimeInSeconds = 0;
        public Double MyRange;
        public Int32 Mana;
        public Int32 MinMana;
        public Int32 MaxMana;
        public Int32 MinHP;
        public Int32 MinHPTotal;
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
        public Int32 RecastDelay;
        public Int64 LastCastTimeStamp;
        public Int64 LastAssistTimeStampForCast;
        public Int32 AfterCastCompletedDelay = 0;
        public Int32 CastID;
        public Int32 MinEnd;
        public Boolean CastInvis;
        public String SpellType = String.Empty;
        public String CastTarget = String.Empty;
        public List<string> StackRequestTargets = new List<string>();
		public static Int64 _stackIntervalCheckDefault = 10000;
        public Int64 StackIntervalCheck = _stackIntervalCheckDefault;
        public Int64 StackIntervalNextCheck = 0;
        public Int64 StackRecastDelay = 0;
        public string StackRequestItem = String.Empty;
		public Dictionary<string, Int64> StackSpellCooldown = new Dictionary<string, long>();
		public Boolean GiftOfMana;
        public Int32 SpellID;
        public Int32 PctAggro;
        public String Zone = "All";
        private const Int32 MinSickDefault = 2;
        public Int32 MinSick = MinSickDefault;
        public Boolean AllowSpellSwap;
        public Boolean NoEarlyRecast;
        public Boolean NoStack;
        public String TriggerSpell = String.Empty;
        public String BeforeSpell = String.Empty;
        public Data.Spell BeforeSpellData;
        public String AfterSpell = String.Empty;
        public Data.Spell AfterSpellData;
        public Boolean NoInterrupt;
		public Int32 SongRefreshTime = 18;

		public Int32 AfterEventDelay = 0;
		public Int32 BeforeEventDelay = 0;
		public Int32 AfterSpellDelay = 0;
		public Int32 BeforeSpellDelay = 0;
		public Int32 AfterCastDelay = 0;

		public String AfterEvent = String.Empty;
		public String BeforeEvent = String.Empty;
		
		public String CastIF = String.Empty;
        public string Ifs = String.Empty;
        public string IfsKeys = String.Empty;
        public string AfterEventKeys = String.Empty;
        public string BeforeEventKeys = String.Empty;
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
		public bool IsBuff = false;
        public Int32 Level = 255;
        public string Description  = String.Empty;
        public Int32 ResistAdj = 0;
        public string ResistType = String.Empty;
		public bool Enabled = true;
		public List<String> SpellEffects = new List<string>();

		//.\protoc --csharp_out=.\ SpellData.proto
		//add field to this class, you need to update the proto file as well.
		public static Spell FromProto(SpellData source, Spell dest = null)
        {
			Spell r;
			if(dest==null)
			{
				r = new Spell();
			}
			else
			{
				r = dest;
			}
			r.SongRefreshTime = source.SongRefreshTime;
			r.AfterEvent = source.AfterEvent;
            r.AfterEventKeys = source.AfterEventKeys;
			r.AfterSpell = source.AfterSpell;
			r.AllowSpellSwap = source.AllowSpellSwap;
			r.BeforeEvent = source.BeforeEvent;
            r.BeforeEventKeys = source.BeforeEventKeys;
			r.BeforeSpell = source.BeforeSpell;
			r.CastID = source.CastID;
			r.CastIF = source.CastIF;
			r.CastInvis = source.CastInvis;
			r.CastName = source.CastName;
			r.CastTarget = source.CastTarget;
			r.CastType = (CastingType)source.CastType;
			r.Category = source.Category;
			r.Debug = source.Debug;
			r.Delay = source.Delay;
            r.RecastDelay = source.RecastDelay;
			r.AfterCastCompletedDelay = source.AfterCastCompletedDelay;
			r.Duration = source.Duration;
			r.DurationTotalSeconds = source.DurationTotalSeconds;
			r.EnduranceCost = source.EnduranceCost;
			r.GiftOfMana = source.GiftOfMana;
			r.GiveUpTimer = source.GiveUpTimer;
			r.HealPct = source.HealPct;
			r.HealthMax = source.HealthMax;
			r.Ifs = source.Ifs;
			r.IfsKeys = source.IfsKeys;
			r.IgnoreStackRules = source.IgnoreStackRules;
			r.InitName = source.InitName;
			r.IsDebuff = source.IsDebuff;
			r.IsDoT = source.IsDoT;
			r.IsBuff = source.IsBuff;
			r.IsShortBuff = source.IsShortBuff;
			r.ItemMustEquip = source.ItemMustEquip;
			r.Mana = source.Mana;
			r.MaxMana = source.MaxMana;
			r.MaxTries = source.MaxTries;
			r.MinDurationBeforeRecast = source.MinDurationBeforeRecast;
			r.MinEnd = source.MinEnd;
			r.MinHP = source.MinHP;
            r.MinHPTotal = source.MinHPTotal;
			r.MinMana = source.MinMana;
			r.MinSick = source.MinSick;
			r.Mode = source.Mode;
			r.MyCastTime = (Decimal)source.MyCastTime;
			r.MyCastTimeInSeconds = (Decimal)source.MyCastTimeInSeconds;
			r.MyRange = source.MyRange;
			r.NoAggro = source.NoAggro;
			r.NoBurn = source.NoBurn;
			r.NoEarlyRecast = source.NoEarlyRecast;
			r.NoInterrupt = source.NoInterrupt;
			r.NoMidSongCast = source.NoMidSongCast;
			r.NoStack = source.NoStack;
			r.NoTarget = source.NoTarget;
			r.PctAggro = source.PctAggro;
			r.Reagent = source.Reagent;
			r.ReagentOutOfStock = source.ReagentOutOfStock;
			r.RecastTime = source.RecastTime;
			r.RecoveryTime = (Decimal)source.RecoveryTime;
			r.Rotate = source.Rotate;
			r.SpellGem = source.SpellGem;
			r.SpellIcon = source.SpellIcon;
			r.SpellID = source.SpellID;
			r.SpellInBook = source.SpellInBook;
			r.SpellName = source.SpellName;
			r.SpellType = source.SpellType;
			r.StackRecastDelay = source.StackRecastDelay;
			r.StackRequestItem = source.StackRequestItem;
			r.StackRequestTargets.AddRange(source.StackRequestTargets);
			r.Subcategory = source.Subcategory;
			r.TargetType = source.TargetType;
			r.TriggerSpell = source.TriggerSpell;
			r.Zone = source.Zone;
			r.Level = source.Level;
			r.Description = source.Description;
			r.ResistType = source.ResistType;
			r.ResistAdj = source.ResistAdj;
			r.CastTypeOverride = (CastingType)source.CastTypeOverride;
			foreach(var entry in source.CheckForCollection)
			{
				if(!r.CheckForCollection.ContainsKey(entry))
				{
					r.CheckForCollection.Add(entry,0);
				}
			}
			foreach (var entry in source.ExcludedClasses)
			{
				if (!r.ExcludedClasses.Contains(entry))
				{
					r.ExcludedClasses.Add(entry);
				}
			}
			foreach (var entry in source.ExcludedNames)
			{
				if (!r.ExcludedNames.Contains(entry))
				{
					r.ExcludedNames.Add(entry);
				}
			}
			r.Enabled = source.Enabled;

			foreach(var entry in source.SpellEffects)
			{
				r.SpellEffects.Add(entry);
			}
			r.AfterEventDelay = source.AfterEventDelay;
			r.BeforeEventDelay = source.BeforeEventDelay;
			r.BeforeSpellDelay = source.BeforeSpellDelay;
			r.AfterCastDelay = source.AfterCastDelay;
			r.AfterSpellDelay = source.AfterSpellDelay;

			return r;
		}
		public static void TransferSpellData(SpellData source, Spell dest)
		{
			Spell r;
			if (dest == null)
			{
				r = new Spell();
			}
			else
			{
				r = dest;
			}
			r.SongRefreshTime = source.SongRefreshTime;
			r.CastID = source.CastID;
			r.CastName = source.CastName;
			r.CastType = (CastingType)source.CastType;
			r.Category = source.Category;
			
			r.Duration = source.Duration;
			r.DurationTotalSeconds = source.DurationTotalSeconds;
			r.EnduranceCost = source.EnduranceCost;
			r.InitName = source.InitName;
			r.ItemMustEquip = source.ItemMustEquip;
			r.Mana = source.Mana;
			r.MyCastTime = (Decimal)source.MyCastTime;
			r.MyCastTimeInSeconds = (Decimal)source.MyCastTimeInSeconds;
			r.MyRange = source.MyRange;
			r.NoAggro = source.NoAggro;
			r.RecastTime = source.RecastTime;
			r.RecoveryTime = (Decimal)source.RecoveryTime;
			r.SpellIcon = source.SpellIcon;
			r.SpellID = source.SpellID;
			r.SpellInBook = source.SpellInBook;
			r.SpellName = source.SpellName;
			r.SpellType = source.SpellType;
			r.Subcategory = source.Subcategory;
			r.TargetType = source.TargetType;
			r.Level = source.Level;
			r.Description = source.Description;
			r.ResistType = source.ResistType;
			r.ResistAdj = source.ResistAdj;
			r.CastTypeOverride = (CastingType)source.CastTypeOverride;
			
			foreach (var entry in source.SpellEffects)
			{
				r.SpellEffects.Add(entry);
			}
		}
		public SpellData ToProto()
        {

            SpellData r = new SpellData();
			r.SongRefreshTime = this.SongRefreshTime;
			r.AfterEvent = this.AfterEvent;
            r.AfterEventKeys = this.AfterEventKeys;
            r.AfterSpell = this.AfterSpell;
            r.AllowSpellSwap = this.AllowSpellSwap;
            r.BeforeEvent = this.BeforeEvent;
            r.BeforeEventKeys = this.BeforeEventKeys;
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
            r.RecastDelay = this.RecastDelay;
            r.AfterCastCompletedDelay = this.AfterCastCompletedDelay;
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
			r.IsBuff = this.IsBuff;
            r.IsShortBuff = this.IsShortBuff;
            r.ItemMustEquip = this.ItemMustEquip;
            r.Mana= this.Mana;
            r.MaxMana= this.MaxMana;
            r.MaxTries = this.MaxTries;
            r.MinDurationBeforeRecast = this.MinDurationBeforeRecast;
            r.MinEnd = this.MinEnd;
            r.MinHP = this.MinHP;
            r.MinHPTotal = this.MinHPTotal;
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
            r.Description = this.Description;
            r.ResistType = this.ResistType;
            r.ResistAdj = this.ResistAdj;
			r.CastTypeOverride = (SpellData.Types.CastingType)this.CastTypeOverride;
			r.IfsKeys = IfsKeys;
			r.CheckForCollection.AddRange(CheckForCollection.Keys.ToList());
            r.ExcludedClasses.AddRange(ExcludedClasses.ToList());
			r.ExcludedNames.AddRange(ExcludedNames.ToList());

			r.Enabled = Enabled;
			foreach (var entry in SpellEffects)
			{
				r.SpellEffects.Add(entry);
			}
			r.AfterEventDelay = AfterEventDelay;
			r.BeforeEventDelay = BeforeEventDelay;
			r.BeforeSpellDelay =BeforeSpellDelay;
			r.AfterCastDelay = AfterCastDelay;
			r.AfterSpellDelay = AfterSpellDelay;
			return r;

        }
		public void TransferFlags(Spell d)
		{
			d.IfsKeys = IfsKeys;
			d.SpellGem = SpellGem;
			d.Zone = Zone;
			d.MinSick = MinSick;
			d.CheckForCollection =CheckForCollection.ToDictionary(entry => entry.Key,  entry => entry.Value);
            d.ExcludedClasses = ExcludedClasses.ToHashSet();
            d.ExcludedNames = ExcludedNames.ToHashSet();
			d.HealPct = HealPct;
			d.NoInterrupt = NoInterrupt;
			d.AfterSpell = AfterSpell;
			d.BeforeSpell = BeforeSpell;
			d.MinMana = MinMana;
			d.MaxMana = MaxMana;
			d.IgnoreStackRules = IgnoreStackRules;
			d.HealthMax = HealthMax;
			d.MinDurationBeforeRecast = MinDurationBeforeRecast;
			d.MaxTries = MaxTries;
			d.CastIF = CastIF;
			d.MinEnd = MinEnd;
			d.AfterEvent = AfterEvent;
			d.BeforeEvent = BeforeEvent;
			d.Reagent = Reagent;
			d.Enabled = Enabled;
            d.CastTarget = CastTarget;
			d.AfterEventDelay = AfterEventDelay;
			d.BeforeEventDelay = BeforeEventDelay;
			d.BeforeSpellDelay = BeforeSpellDelay;
			d.AfterCastDelay = AfterCastDelay;
			d.AfterSpellDelay = AfterSpellDelay;
			d.SongRefreshTime = SongRefreshTime;
		}

        public string ToConfigEntry()
        {
			//This is C#'s ternary conditional operator
            //its condition if true do 1st, else 2nd. 
			//in this case, if ifskeys is null or empty, set to string empty
            //else use /Ifs|{IfsKeys}
            string t_Ifs = (String.IsNullOrWhiteSpace(this.IfsKeys)) ? String.Empty : $"/Ifs|{IfsKeys}";
            string t_Zone = (Zone=="All") ? String.Empty :  $"/Zone|{Zone}";
			string t_MinSick = (MinSick == MinSickDefault) ? String.Empty : t_MinSick = $"/MinSick|{MinSick}";
	        string t_checkFor = (CheckForCollection.Count == 0) ? String.Empty: t_checkFor = "/CheckFor|" + String.Join(",", CheckForCollection.Keys.ToList());
            string t_excludeClasses = (ExcludedClasses.Count == 0) ? String.Empty : t_excludeClasses = "/ExcludedClasses|" + String.Join(",", ExcludedClasses.ToList());
			string t_excludeNames = (ExcludedNames.Count == 0) ? String.Empty : t_excludeNames = "/ExcludedNames|" + String.Join(",", ExcludedNames.ToList());

			string t_healPct = (HealPct == 0) ?String.Empty :  $"/HealPct|{HealPct}";
            string t_noInterrupt = (!NoInterrupt) ? String.Empty :$"/NoInterrupt";
		    string t_AfterSpell = (String.IsNullOrWhiteSpace(this.AfterSpell)) ?String.Empty : t_AfterSpell = $"/AfterSpell|{AfterSpell}";
			string t_BeforeSpell = (String.IsNullOrWhiteSpace(this.BeforeSpell)) ? String.Empty : t_BeforeSpell = $"/BeforeSpell|{BeforeSpell}";
            string t_minMana = (MinMana==0) ?String.Empty: $"/MinMana|{MinMana}";
			string t_maxMana = (MaxMana == 0) ? String.Empty : $"/MaxMana|{MaxMana}";
			string t_ignoreStackRules = (!IgnoreStackRules) ? String.Empty : $"/IgnoreStackRules";
			string t_healthMax = (HealthMax == 100) ? String.Empty : $"/HealthMax|{HealthMax}";
			string t_MinDurationBeforeRecast = (MinDurationBeforeRecast == 0) ? String.Empty : $"/MinDurationBeforeRecast|{MinDurationBeforeRecast/1000}";
			string t_MaxTries = (MaxTries == MaxTriesDefault) ? String.Empty : $"/MaxTries|{MaxTries}";
			string t_CastIF = (String.IsNullOrWhiteSpace(this.CastIF)) ? String.Empty : $"/CastIF|{CastIF}";
			string t_MinEnd = (MinEnd == 0) ? String.Empty : $"/MinEnd|{MinEnd}";
			string t_AfterEvent = (String.IsNullOrWhiteSpace(this.AfterEventKeys)) ? String.Empty : $"/AfterEvent|{AfterEventKeys}";
			string t_BeforeEvent = (String.IsNullOrWhiteSpace(this.BeforeEventKeys)) ? String.Empty : $"/BeforeEvent|{BeforeEventKeys}";
			string t_Reagent = (String.IsNullOrWhiteSpace(this.Reagent)) ? String.Empty : $"/Reagent|{Reagent}";
			string t_CastTypeOverride = (this.CastTypeOverride== CastingType.None) ? String.Empty : $"/CastType|{CastTypeOverride.ToString()}";
			string t_GemNumber = (this.SpellGem == 0) ? String.Empty : $"/Gem|{SpellGem}";
			string t_Enabled = (Enabled == true) ? String.Empty : $"/Disabled";
			string t_CastTarget = (String.IsNullOrWhiteSpace(this.CastTarget) || this.IsBuff==false) ? String.Empty : $"/{CastTarget}";
			string t_PctAggro = (PctAggro == 0) ? String.Empty : $"/PctAggro|{PctAggro}";
            string t_Delay = (Delay == 0) ? String.Empty : $"/Delay|{Delay}s";
            string t_RecastDelay = (RecastDelay == 0) ? String.Empty : $"/RecastDelay|{RecastDelay}s";
			string t_NoTarget = NoTarget == false ? String.Empty : $"/NoTarget";
			string t_NoAggro = NoAggro == false ? String.Empty : $"/NoAggro";

			string t_AfterEventDelay = AfterEventDelay == 0 ? String.Empty : $"/AfterEventDelay|{AfterEventDelay}";
			string t_AfterSpellDelay = AfterSpellDelay == 0 ? String.Empty : $"/AfterEventDelay|{AfterSpellDelay}";
			string t_BeforeEventDelay = BeforeEventDelay == 0 ? String.Empty : $"/BeforeEventDelay|{BeforeEventDelay}";
			string t_BeforeSpellDelay = BeforeSpellDelay == 0 ? String.Empty : $"/BeforeEventDelay|{BeforeSpellDelay}";
			string t_AfterCastDelay = AfterCastDelay == 0 ? String.Empty : $"/AfterCastDelay|{AfterCastDelay}";
			string t_AfterCastCompletedDelay= AfterCastCompletedDelay == 0 ? String.Empty : $"/AfterCastCompletedDelay|{AfterCastCompletedDelay}";
			string t_SongRefreshTime = SongRefreshTime == 18 ? String.Empty : $"/SongRefreshTime|{SongRefreshTime}";
			string t_StackRequestItem = (String.IsNullOrWhiteSpace(this.StackRequestItem)) ? String.Empty : $"/StackRequestItem|{StackRequestItem}";
			string t_StackRequestTargets = (StackRequestTargets.Count==0) ? String.Empty : $"/StackRequestTargets|{String.Join(",",StackRequestTargets)}";
			string t_StackCheckInterval = StackIntervalCheck == _stackIntervalCheckDefault ? String.Empty : $"/StackCheckInterval|{StackIntervalCheck/10000}";
			string t_StackRecastDelay = StackRecastDelay == 0 ? String.Empty : $"/StackRecastDelay|{StackRecastDelay/1000}";

			string returnValue = $"{CastName}{t_CastTarget}{t_GemNumber}{t_Ifs}{t_checkFor}{t_CastIF}{t_healPct}{t_healthMax}{t_noInterrupt}{t_Zone}{t_MinSick}{t_BeforeSpell}{t_AfterSpell}{t_BeforeEvent}{t_AfterEvent}{t_minMana}{t_maxMana}{t_MinEnd}{t_ignoreStackRules}{t_MinDurationBeforeRecast}{t_MaxTries}{t_Reagent}{t_CastTypeOverride}{t_PctAggro}{t_Delay}{t_RecastDelay}{t_NoTarget}{t_NoAggro}{t_AfterEventDelay}{t_AfterSpellDelay}{t_BeforeEventDelay}{t_BeforeSpellDelay}{t_AfterCastDelay}{t_AfterCastCompletedDelay}{t_SongRefreshTime}{t_StackRequestItem}{t_StackRequestTargets}{t_StackCheckInterval}{t_StackRecastDelay}{t_excludeClasses}{t_excludeNames}{t_Enabled}";
			return returnValue;

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

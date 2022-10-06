using E3Core.Data;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public static class Casting
    {

        public static string _lastSuccesfulCast = String.Empty;
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        public static Dictionary<Int32, Int64> _gemRecastLockForMem = new Dictionary<int, long>();
        public static Dictionary<Int32, ResistCounter> _resistCounters = new Dictionary<Int32, ResistCounter>();
        public static Dictionary<Int32, Int32> _currentSpellGems = new Dictionary<int, int>();
        public static Int64 _currentSpellGemsLastRefresh = 0;


        public static void ResetResistCounters()
        {
            //put them back in their object pools
            foreach (var kvp in Casting._resistCounters)
            {
                kvp.Value.Dispose();
            }
            _resistCounters.Clear();
            
        }
        public static void Init()
        {
            RegisterEventsCasting();
            RefreshGemCache();
        }

        public static void RefreshGemCache()
        {
            if((E3._currentClass & Class.PureMelee) ==E3._currentClass)
            {
                //class doesn't have spells
                return;
            }

            if(Core._stopWatch.ElapsedMilliseconds <_currentSpellGemsLastRefresh)
            {
                return;
            }
            _currentSpellGemsLastRefresh = Core._stopWatch.ElapsedMilliseconds + 2000;
            //need to get all the spellgems setup
            for (int i = 1; i < 13; i++)
            {
                Int32 spellID = MQ.Query<Int32>($"${{Me.Gem[{i}].ID}}");
                if (!_currentSpellGems.ContainsKey(i))
                {
                    _currentSpellGems.Add(i, spellID);
                }
                _currentSpellGems[i] = spellID;
            }
        }
        static void RegisterEventsCasting()
        {
            _log.Write("Regitering nowCast events....");
            List<String> r = new List<string>();
            r.Add("(.+) tells the group, 'nowCast (.+) targetid=(.+)'");
            r.Add("(.+) tells the says, 'nowCast (.+) targetid=(.+)'");
            EventProcessor.RegisterEvent("nowCastEvent", r, (x) => {
                _log.Write($"Processing {x.eventName}");
                
                string user = string.Empty;
                string spellName = String.Empty;
                Int32 targetid = 0;
                if (x.match.Groups.Count > 3)
                {
                    user = x.match.Groups[1].Value;
                    spellName = x.match.Groups[2].Value;
                    Int32.TryParse(x.match.Groups[3].Value, out targetid);

                }
                _log.Write($"{ x.eventName}:{ user} asked to cast the spell:{spellName}");

                Data.Spell spell = new Data.Spell(spellName);
                CastReturn returnValue = Cast(targetid, spell);

                _log.Write($"{ x.eventName}: {spellName} result?: {returnValue.ToString()}");

            });



        }
        public static CastReturn Cast(int targetID, Data.Spell spell, Func<bool> interruptCheck=null)
        {

            //TODO: Add bard logic back in
            using (_log.Trace())
            {
               

                if(targetID < 1)
                {
                    MQ.Write($"Invalid targetId for Casting. {targetID}");
                }
               
                String targetName =String.Empty;
                //targets of 0 means keep current target
                if (targetID > 0) 
                {
                    targetName = MQ.Query<string>($"${{Spawn[id {targetID}].CleanName}}");
                }
                else
                {
                    targetName = MQ.Query<string>($"${{Spawn[id ${{Target.ID}}].CleanName}}");
                }
                _log.Write($"TargetName:{targetName}");
                //why we should not cast.. for whatever reason.
                #region validation checks
                if (MQ.Query<bool>("${Me.Invis}"))
                {

                    E3._actionTaken = true;

                    _log.Write($"SkipCast-Invis ${spell.CastName} {targetName} : {targetID}");
                    return CastReturn.CAST_INVIS;

                }

                if (!String.IsNullOrWhiteSpace(spell.Reagent))
                {
                    _log.Write($"Checking for reagent required for spell cast:{targetName} value:{spell.Reagent}");
                    //spell requires a regent, lets check if we have it.
                    Int32 itemCount = MQ.Query<Int32>($"${{FindItemCount[={spell.Reagent}]}}");
                    if (itemCount < 1)
                    {
                        spell.ReagentOutOfStock = true;
                        _log.Write($"Cannot cast [{spell.CastName}], I do not have any [{spell.Reagent}], removing this spell from array. Restock and Reload Macro", Logging.LogLevels.Error);
                        MQ.Cmd("/beep");
                        return CastReturn.CAST_REAGENT;
                    }
                    else
                    {
                        _log.Write($"Reagent found!");

                    }

                }

                _log.Write("Checking for zoning...");
                if (E3._zoneID != MQ.Query<Int32>("${Zone.ID}"))
                {
                    _log.Write("Currently zoning, delaying for 1second");
                    //we are zoning, we need to chill for a bit.
                    MQ.Delay(1000);
                    return CastReturn.CAST_ZONING;
                }

                _log.Write("Checking for Feigning....");
                if (MQ.Query<bool>("${Me.Feigning}"))
                {
                    MQ.Broadcast($"skipping [{spell.CastName}] , i am feigned.");
                    MQ.Delay(200);
                    return CastReturn.CAST_FEIGN;
                }
                _log.Write("Checking for Open spell book....");
                if (MQ.Query<bool>("${Window[SpellBookWnd].Open}"))
                {
                    E3._actionTaken = true;
                    MQ.Broadcast($"skipping [{spell.CastName}] , spellbook is open.");
                    MQ.Delay(200);
                    return CastReturn.CAST_SPELLBOOKOPEN;
                }
                _log.Write("Checking for Open corpse....");
                if (MQ.Query<bool>("${Corpse.Open}"))
                {
                    E3._actionTaken = true;
                    MQ.Broadcast($"skipping [{spell.CastName}] , I have a corpse open.");
                    MQ.Delay(200);
                    return CastReturn.CAST_SPELLBOOKOPEN;
                }
                _log.Write("Checking for LoS for non beneficial...");
                if (!spell.SpellType.Equals("Beneficial"))
                {
                    _log.Write("Checking for LoS for non disc and not self...");
                    if (!spell.CastType.Equals("Disc") && spell.TargetType.Equals("Self"))
                    {
                        _log.Write("Checking for LoS for non PB AE and Self...");
                        if (!(spell.TargetType.Equals("PB AE") || spell.TargetType.Equals("Self")))
                        {
                            _log.Write("Checking for LoS if target has LoS...");
                            if (!MQ.Query<bool>($"${{Spawn[id {targetID}].LineOfSight}}"))
                            {
                                _log.Write($"I cannot see {targetName}");
                                MQ.Write($"SkipCast-LoS {spell.CastName} ${spell.CastID} {targetName} {targetID}");
                                return CastReturn.CAST_CANNOTSEE;

                            }
                        }
                    }
                }
                #endregion
                //now to get the target
                _log.Write("Checking to see if we need to aquire a target for non self /pbaoe");
                if (spell.TargetType != "PB AE" && spell.TargetType != "Self")
                {
                    TrueTarget(targetID);
                }
                _log.Write($"Checking BeforeEvent...");
                if (!String.IsNullOrWhiteSpace(spell.BeforeEvent))
                {
                    _log.Write($"Doing BeforeEvent:{spell.BeforeEvent}");
                    MQ.Cmd($"/docommand {spell.BeforeEvent}");
                }

                _log.Write($"Checking BeforeSpell...");
                if (!String.IsNullOrWhiteSpace(spell.BeforeSpell))
                {
                    if (spell.BeforeSpellData == null)
                    {
                        spell.BeforeSpellData = new Data.Spell(spell.BeforeSpell);
                    }
                    _log.Write($"Doing BeforeSpell:{spell.BeforeSpell}");
                    Casting.Cast(targetID, spell.BeforeSpellData);
                }

                //remove item from cursor before casting
                _log.Write($"Checking for item on cursor...");
                if (MQ.Query<bool>("${Cursor.ID}"))
                {
                    MQ.Write("Issuing auto inventory on ${Cursor} for spell: ${pendingCast}");
                    MQ.Cmd("/autoinventory");
                }


                //From here, we actually start casting the spell. 
                _log.Write($"Checking for spell type to run logic...");
                if (spell.CastType == Data.CastType.Disc)
                {
                    _log.Write($"Doing disc based logic checks...");
                    if (MQ.Query<bool>("${Me.ActiveDisc.ID}") && spell.TargetType.Equals("Self"))
                    {
                        return CastReturn.CAST_ACTIVEDISC;

                    }
                    else
                    {
                        //activate disc!
                        TrueTarget(targetID);
                        E3._actionTaken = true;
                        _log.Write($"Issuing Disc command:{spell.CastName}");
                        MQ.Cmd($"/disc {spell.CastName}");
                        MQ.Delay(300);
                        return CastReturn.CAST_SUCCESS;
                    }

                }
                else if (spell.CastType == Data.CastType.Ability && MQ.Query<bool>($"${{Me.AbilityReady[{spell.CastName}]}}"))
                {
                    _log.Write($"Doing Ability based logic checks...");
                    //to deal with a slam bug
                    if (spell.CastName.Equals("Slam"))
                    {
                        _log.Write($"Doing Ability:Slam based logic checks...");
                        if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FirstAbilityButton].Text.Equal[Slam]}"))
                        {

                            MQ.Cmd("/doability 1");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SecondAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 2");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_ThirdAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 3");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 4");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 5");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FifthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 5");
                        }
                        else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SixthAbilityButton].Text.Equal[Slam]}"))
                        {
                            MQ.Cmd("/doability 6");
                        }
                    }
                    else
                    {
                        MQ.Cmd($"/doability \"{spell.CastName}\"");
                    }

                    MQ.Delay(300, $"${{Me.AbilityReady[{spell.CastName}]}}");


                }
                else
                {
                    //Spell, AA, Items
                    _log.Write($"Doing Spell based logic checks...");
                    //Stop following for spell/item/aa with a cast time > 0 MyCastTime, unless im a bard
                    //anything under 300 is insta cast
                    if (spell.MyCastTime > 300 && E3._currentClass != Data.Class.Bard)
                    {
                        if (MQ.Query<bool>("${Stick.Status.Equal[on]}")) MQ.Cmd("/squelch /stick pause");
                        if (MQ.Query<bool>("${AdvPath.Following}") && E3._following) MQ.Cmd("/squelch /afollow off");
                        if (MQ.Query<bool>("${MoveTo.Moving}") && E3._following) MQ.Cmd("/moveto off");

                        MQ.Delay(300, "${Bool[!${Me.Moving}]}");

                    }

                    //TODO: SWAP ITEM

                    E3._actionTaken = true;

                    if (spell.CastType == Data.CastType.Item)
                    {

                        //TODO: Recast logic

                    }

                    startCast:
                    if (E3._currentClass == Data.Class.Bard && MQ.Query<bool>($"${{Bool[${{Me.Book[{spell.CastName}]}}]}}"))
                    {
                      
                        MQ.Cmd($"/cast \"{spell.CastName}\"");
                        MQ.Delay(500, "${Window[CastingWindow].Open}");
                        if (!MQ.Query<bool>("${Window[CastingWindow].Open}"))
                        {
                            MQ.Write("Issuing stopcast as cast window isn't open");
                            MQ.Cmd("/stopcast");
                            MQ.Delay(100);
                            MQ.Cmd($"/cast \"{spell.CastName}\"");
                            //wait for spell cast
                            MQ.Delay(300);

                        }

                        return CastReturn.CAST_SUCCESS;
                    }
                    else
                    {
                        _log.Write($"Doing Spell:TargetType based logic checks...");
                        if (spell.TargetType.Equals("Self"))
                        {
                            if (spell.CastType == Data.CastType.Spell)
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.SpellGem}\"");
                            }
                            else
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.CastType.ToString()}\"");
                            }
                        }
                        else
                        {
                            if (spell.CastType == Data.CastType.Spell)
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.SpellGem}\" \"-targetid|{targetID}\"");
                            }
                            else
                            {
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.CastType.ToString()}\" \"-targetid|{targetID}\"");
                            }
                        }
                        //wait for spell cast
                        MQ.Delay(300);
                    }
                    if(WaitForPossibleFizzle(spell))
                    {
                        //do we have the mana to recast the spell if fizzle? if so, do it
                        if(checkMana(spell))
                        {
                            MQ.Delay(100);
                            goto startCast;
                            //we fizzled, recast spell?

                        }
                    }

                }

                while(MQ.Query<bool>("${Window[CastingWindow].Open}")) 
                {
                    //means that we didn't fizzle and are now casting the spell

                    //TODO: Interrupt logic
                    if (interruptCheck != null && interruptCheck())
                    {
                        MQ.Cmd("/interrupt");
                        E3._actionTaken = true;
                        return CastReturn.CAST_INTERRUPTFORHEAL;
                    }
                    MQ.Delay(100);

                }
                //spell is either complete or interrupted
                //need to wait for a period to get our results. 
                MQ.Delay(300);
                string castResult = MQ.Query<string>("${Cast.Result}");
                CastReturn returnValue = CastReturn.CAST_INTERRUPTED;
                Enum.TryParse<CastReturn>(castResult, out returnValue);

                if (returnValue == CastReturn.CAST_SUCCESS)
                {
                    _lastSuccesfulCast = spell.CastName;
                    //clear the counter for this pell on this mob?
                    if (_resistCounters.ContainsKey(targetID))
                    {
                        _resistCounters[targetID].Dispose();
                        _resistCounters.Remove(targetID);

                    }
                }
                else if (returnValue == CastReturn.CAST_RESIST)
                {
                    if (!_resistCounters.ContainsKey(targetID))
                    {
                        ResistCounter tresist = ResistCounter.Aquire();
                        _resistCounters.Add(targetID, tresist);
                    }
                    ResistCounter resist = _resistCounters[targetID];
                    if (!resist._spellCounters.ContainsKey(spell.SpellID))
                    {
                        resist._spellCounters.Add(spell.SpellID, 0);
                    }
                    resist._spellCounters[spell.SpellID]++;
                    
                }
                else if (returnValue==CastReturn.CAST_TAKEHOLD)
                {
                    //TODO: Add timers
                }
                else if(returnValue == CastReturn.CAST_IMMUNE)
                {
                    //TODO: deal with immunity
                }


                //is an after spell configured? lets do that now.
                _log.Write($"Checking AfterSpell...");
                if (!String.IsNullOrWhiteSpace(spell.AfterSpell))
                {
                    
                    if (spell.AfterSpellData == null)
                    {
                        spell.AfterSpellData = new Data.Spell(spell.AfterSpell);
                    }
                    //Wait for GCD if spell

                    _log.Write($"Doing AfterSpell:{spell.AfterSpell}");
                    if(CheckReady(spell))
                    {
                        Casting.Cast(targetID, spell.AfterSpellData);
                    }
                }
                //after event, after all things are done               
                _log.Write($"Checking AfterEvent...");
                if (!String.IsNullOrWhiteSpace(spell.AfterEvent))
                {
                    _log.Write($"Doing AfterEvent:{spell.AfterEvent}");
                    MQ.Cmd($"/docommand {spell.AfterEvent}");
                }

                if (Basics._following && Assist._isAssisting) Basics.AcquireFollow();

                //TODO: bard resume twist

                MQ.Write("Cast return value:" + returnValue);
                return returnValue;
                
            }


        }
      
        private static bool MemorizeSpell(Data.Spell spell)
        {
            //if no spell gem is set, set it.
            if (spell.SpellGem == 0)
            {
                spell.SpellGem = E3._generalSettings.Casting_DefaultSpellGem;
            }
            Int32 spellID;
            if (_currentSpellGems.TryGetValue(spell.SpellGem, out spellID))
            {
                if(spell.SpellID==spellID)
                {
                    //already memed, exit.
                    return true;
                }
            }

            //memorize may fail if there is a gem "Lockout" time period, where
            //We JUST memed a spell so its protected to be used for a period of its recast time.
            if (_gemRecastLockForMem.ContainsKey(spell.SpellGem))
            {
                //there is a spell lock possibly on this gem, check
                if(_gemRecastLockForMem[spell.SpellGem]> Core._stopWatch.ElapsedMilliseconds)
                {
                    //this is still locked, return false
                    return false;
                }
            }
            MQ.Cmd($"/memorize \"{spell.SpellName}\" {spell.SpellGem}");
            MQ.Delay(1000);
            MQ.Delay(5000, "!${Window[SpellBookWnd].Open}");

            //make double sure the collectio has this spell gem. maybe purchased AA for new slots?
            if (!_gemRecastLockForMem.ContainsKey(spell.SpellGem))
            {
                _gemRecastLockForMem.Add(spell.SpellGem, 0);
            }
            _gemRecastLockForMem[spell.SpellGem] = Core._stopWatch.ElapsedMilliseconds + spell.RecastTime + 1000;

            //update spellgem collection
            if(!_currentSpellGems.ContainsKey(spell.SpellGem))
            {
                _currentSpellGems.Add(spell.SpellGem, spell.SpellID);
            }
            _currentSpellGems[spell.SpellGem] = spell.SpellID;


            return true;
        }

        public static Boolean checkMana(Data.Spell spell)
        {

            Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
            Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
            if (currentMana>spell.Mana)
            {
                if(pctMana> spell.MinMana)
                {   if(spell.MaxMana > 0)
                    {
                        if (pctMana < spell.MaxMana)
                        {
                            return false;
                        }
                    }
                    return true;
                   
                }
            }
            return false;
        }
        public static Boolean CheckReady(Data.Spell spell)
        {

            //do we need to memorize it?
            if(!MemorizeSpell(spell))
            {
                return false;
            }

            _log.Write($"CheckReady on {spell.CastName}");

            if(E3._currentClass== Data.Class.Bard && !MQ.Query<bool>("${Twist.Twisting}"))
            {
                while(MQ.Query<bool>("${Window[CastingWindow].Open}"))
                {
                    MQ.Delay(100);
                }
            } 
            else if(MQ.Query<bool>("${Window[CastingWindow].Open}"))
            {
                //we are already casting a spell, user overrride? 
                return false;
            }

            bool returnValue = false;
            if(spell.CastType== Data.CastType.Spell)
            {
                recheckCooldown:

                //deal with grouped spells should put in a method if it gets bigger than these two
                if(spell.CastName== "Focused Hail of Arrows" || spell.CastName== "Hail of Arrows")
                {
                    if (MQ.Query<bool>("${Bool[${Me.Gem[Focused Hail of Arrows]}]}") && MQ.Query<bool>("${Bool[${Me.Gem[Hail of Arrows]}]}"))
                    {
                        returnValue = true;
                        if (!MQ.Query<bool>("${Me.SpellReady[Focused Hail of Arrows]}")) { returnValue = false; }
                        if (!MQ.Query<bool>("${Me.SpellReady[Hail of Arrows]}")) { returnValue = false; }

                        return returnValue;
                    }
                }
                if (spell.CastName == "Mana Flare" || spell.CastName == "Mana Recursion")
                {
                    if (MQ.Query<bool>("${Bool[${Me.Gem[Mana Flare]}]}") && MQ.Query<bool>("${Bool[${Me.Gem[Mana Recursion]}]}"))
                    {
                        returnValue = true;
                        if (!MQ.Query<bool>("${Me.SpellReady[Mana Flare]}")) { returnValue = false; }
                        if (!MQ.Query<bool>("${Me.SpellReady[Mana Recursion]}")) { returnValue = false; }

                        return returnValue;
                    }
                }

                _log.Write($"Checking if spell is ready on {spell.CastName}");

                if (MQ.Query<bool>($"${{Me.SpellReady[{spell.CastName}]}}"))
                {
                    _log.Write($"CheckReady Success! on {spell.CastName}");

                    returnValue = true;
                    return returnValue;
                }
                _log.Write($"Checking if spell are on cooldown for {spell.CastName}");

                if (MQ.Query<bool>("${Me.SpellInCooldown}") && MQ.Query<bool>($"${{Bool[${{Me.Gem[{spell.CastName}]}}]}}"))
                {
                    _log.Write("Spells in cooldown, redoing check.");
                    MQ.Delay(100);
                    goto recheckCooldown;
                }


            }
            else if(spell.CastType == Data.CastType.Item)
            {
                if(MQ.Query<bool>($"${{Me.ItemReady[{spell.CastName}]}}"))
                {
                    return true;
                }
            }
            else if (spell.CastType == Data.CastType.AA)
            {
                if (MQ.Query<bool>($"${{Me.AltAbilityReady[{spell.CastName}]}}"))
                {
                    return true;
                }
            }
            else if (spell.CastType == Data.CastType.Disc)
            {
                //bug with thiefs eyes, always return true
                if (spell.SpellID == 8001) return true;

                if (MQ.Query<bool>($"${{Me.CombatAbilityReady[{spell.CastName}]}}"))
                {
                    return true;
                }
            }
            else if (spell.CastType == Data.CastType.Ability)
            {
                if (MQ.Query<bool>($"${{Me.AbilityReady[{spell.CastName}]}}"))
                {
                    return true;
                }
            }

            return returnValue;
        }
        private static bool WaitForPossibleFizzle(Data.Spell spell)
        {

            if (spell.MyCastTime > 500)
            {
                Int32 counter = 0;
                while (!MQ.Query<bool>("${Window[CastingWindow].Open}") && counter < 20)
                {
                    if (MQ.Query<bool>("${Cast.Result.Equal[CAST_FIZZLE]}"))
                    {
                        //window not open yet, but we have a fizzle set..breaking
                        return true;
                    }
                    MQ.Delay(100);
                    counter++;
                }
            }
            return false;
        }
  
        public static bool TrueTarget(Int32 targetID)
        {

            //0 means don't change target
            if (targetID == 0) return false;

            _log.Write("Trying to Aquire target");

            if (MQ.Query<Int32>("${Target.ID}") == targetID) return true;
            
            //now to get the target
            if (MQ.Query<Int32>($"${{SpawnCount[id {targetID}]}}") > 0)
            {
                MQ.Cmd($"/target id {targetID}");
                MQ.Delay(300, $"${{Target.ID}}=={targetID}");
                if (MQ.Query<Int32>("${Target.ID}") == targetID) return true;
                return false;
            }
            else
            {   
                MQ.Write("TrueTarget has no spawncount");
                return false;
            }

        }
        
    }
   

    /*
    | CAST_CANCELLED       | Spell was cancelled by ducking (either manually or because mob died) |
    | CAST_CANNOTSEE       | You can't see your target                                            |
    | CAST_IMMUNE          | Target is immune to this spell                                       |
    | CAST_INTERRUPTED     | Casting was interrupted and exceeded the given time limit            |
    | CAST_INVIS           | You were invis, and noInvis is set to true                           |
    | CAST_NOTARGET        | You don't have a target selected for this spell                      |
    | CAST_NOTMEMMED       | Spell is not memmed and you gem to mem was not specified             |
    | CAST_NOTREADY        | AA ability or spell is not ready yet                                 |
    | CAST_OUTOFMANA       | You don't have enough mana for this spell!                           |
    | CAST_OUTOFRANGE      | Target is out of range                                               |
    | CAST_RESIST          | Your spell was resisted!                                             |
    | CAST_SUCCESS         | Your spell was cast successfully! (yay)                              |
    | CAST_UNKNOWN         | Spell/Item/Ability was not found                                     |
    | CAST_COLLAPSE        | Gate Collapsed                                                       |
    | CAST_TAKEHOLD        | Spell not hold                                                       |
    | CAST_FIZZLE          | Spell Fizzle                                                         |
    | CAST_INVISIBLE       | NOT Casting Invis                                                    |
    | CAST_RECOVER	       | Spell not Recovered yet!                                             |
    | CAST_STUNNED	       | Stunned                                                              |
    | CAST_STANDIG	       | Not Standing                                                         |
    | CAST_DISTRACTED      | To Distracted ( spell book open )                                    |
    | CAST_COMPONENTS| Missing Component													      |
     
     */
    public enum CastReturn
    {
        CAST_CANCELLED,
        CAST_CANNOTSEE,
        CAST_IMMUNE,
        CAST_INTERRUPTED,
        CAST_INVIS,
        CAST_NOTARGET,
        CAST_NOTMEMMED,
        CAST_NOTREADY,
        CAST_OUTOFMANA,
        CAST_OUTOFRANGE,
        CAST_RESIST,
        CAST_SUCCESS,
        CAST_UNKNOWN,
        CAST_COLLAPSE,
        CAST_TAKEHOLD,
        CAST_FIZZLE,
        CAST_INVISIBLE,
        CAST_RECOVER,
        CAST_STUNNED,
        CAST_STANDIG,
        CAST_DISTRACTED,
        CAST_COMPONENTS,
        CAST_REAGENT,
        CAST_ZONING,
        CAST_FEIGN,
        CAST_SPELLBOOKOPEN,
        CAST_ACTIVEDISC,
        CAST_INTERRUPTFORHEAL

    }
}

using E3Core.Data;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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
        private static ISpawns _spawns = E3._spawns;


        public static CastReturn Cast(int targetID, Data.Spell spell, Func<Int32, Int32, bool> interruptCheck = null, bool isNowCast = false)
        {


            //bard can cast insta cast items while singing, they be special.
            if (E3._currentClass == Class.Bard && spell.MyCastTime <= 500 && spell.CastType == CastType.Item)
            {
                //instant cast item, can cast while singing
            }
            else if (E3._currentClass == Class.Bard && spell.CastType == CastType.Spell)
            {
                Sing(targetID, spell);
                MQ.Delay((int)spell.MyCastTime);
                return CastReturn.CAST_SUCCESS;
            }
            else
            {
                //block on waiting for the spell window to close
                while (IsCasting())
                {
                    if (!isNowCast && EventProcessor._commandList.ContainsKey("/nowcast") && EventProcessor._commandList["/nowcast"].queuedEvents.Count > 0)
                    {
                        //we have a nowcast ready to be processed
                        MQ.Cmd("/interrupt");
                        MQ.Delay(0);
                        return CastReturn.CAST_INTERRUPTED;
                    }
                    EventProcessor.ProcessEventsInQueues("/backoff");
                    EventProcessor.ProcessEventsInQueues("/followme");
                    if (Assist._assistTargetID == 0)
                    {
                        return CastReturn.CAST_INTERRUPTED;
                    }
                    MQ.Delay(50);
                }
            }


            CastReturn returnValue = CastReturn.CAST_RESIST;
            //TODO: Add bard logic back in
            //using (_log.Trace())
            {

                if (MQ.Query<bool>("${Cursor.ID}"))
                {
                    MQ.Cmd("/autoinv");
                }

                if (targetID == 0)
                {
                    //means don't change current target
                    targetID = MQ.Query<Int32>("${Target.ID}");
                    if (targetID == 0)
                    {
                        targetID = MQ.Query<Int32>("${Me.ID}");
                    }
                }

                if (targetID < 1)
                {
                    if (!(spell.TargetType == "Self" || spell.TargetType == "Group v1"))
                    {
                        MQ.Write($"Invalid targetId for Casting. {targetID}");
                        E3._actionTaken = true;
                        return CastReturn.CAST_NOTARGET;
                    }
                }

                Spawn s;
                if (_spawns.TryByID(targetID, out s))
                {

                    String targetName = String.Empty;
                    //targets of 0 means keep current target
                    if (targetID > 0)
                    {
                        targetName = s.CleanName;
                    }
                    else
                    {
                        targetName = MQ.Query<string>($"${{Spawn[id ${{Target.ID}}].CleanName}}");
                    }
                    _log.Write($"TargetName:{targetName}");
                    //why we should not cast.. for whatever reason.
                    #region validation checks
                    if (!isNowCast && MQ.Query<bool>("${Me.Invis}"))
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
                        E3._bots.Broadcast($"skipping [{spell.CastName}] , i am feigned.");
                        MQ.Delay(200);
                        return CastReturn.CAST_FEIGN;
                    }
                    _log.Write("Checking for Open spell book....");
                    if (MQ.Query<bool>("${Window[SpellBookWnd].Open}"))
                    {
                        E3._actionTaken = true;
                        E3._bots.Broadcast($"skipping [{spell.CastName}] , spellbook is open.");
                        MQ.Delay(200);
                        return CastReturn.CAST_SPELLBOOKOPEN;
                    }
                    _log.Write("Checking for Open corpse....");
                    if (MQ.Query<bool>("${Corpse.Open}"))
                    {
                        E3._actionTaken = true;
                        E3._bots.Broadcast($"skipping [{spell.CastName}] , I have a corpse open.");
                        MQ.Delay(200);
                        return CastReturn.CAST_CORPSEOPEN;
                    }
                    _log.Write("Checking for LoS for non beneficial...");
                    if (!spell.SpellType.Equals("Beneficial"))
                    {
                        _log.Write("Checking for LoS for non disc and not self...");
                        if (!(spell.CastType.Equals("Disc") && spell.TargetType.Equals("Self")))
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
                    _log.Write("Checking BeforeEvent...");
                    if (!String.IsNullOrWhiteSpace(spell.BeforeEvent))
                    {
                        _log.Write($"Doing BeforeEvent:{spell.BeforeEvent}");
                        MQ.Cmd($"/docommand {spell.BeforeEvent}");
                    }

                    _log.Write("Checking BeforeSpell...");
                    if (!String.IsNullOrWhiteSpace(spell.BeforeSpell))
                    {
                        if (spell.BeforeSpellData == null)
                        {
                            spell.BeforeSpellData = new Data.Spell(spell.BeforeSpell);
                        }
                        //Wait for GCD if spell

                        _log.Write("Doing AfterSpell:{spell.AfterSpell}");
                        if (CheckReady(spell.BeforeSpellData) && CheckMana(spell.BeforeSpellData))
                        {
                            Casting.Cast(targetID, spell.BeforeSpellData);
                        }
                        _log.Write($"Doing BeforeSpell:{spell.BeforeSpell}");

                    }

                    //remove item from cursor before casting
                    _log.Write("Checking for item on cursor...");
                    if (MQ.Query<bool>("${Cursor.ID}"))
                    {
                        MQ.Write("Issuing auto inventory on ${Cursor} for spell: ${pendingCast}");
                        MQ.Cmd("/autoinventory");
                    }


                    //From here, we actually start casting the spell. 
                    _log.Write("Checking for spell type to run logic...");
                    if (spell.CastType == Data.CastType.Disc)
                    {
                        _log.Write("Doing disc based logic checks...");
                        if (MQ.Query<bool>("${Me.ActiveDisc.ID}") && spell.TargetType.Equals("Self"))
                        {
                            return CastReturn.CAST_ACTIVEDISC;

                        }
                        else
                        {
                            //activate disc!
                            TrueTarget(targetID);
                            E3._actionTaken = true;
                            _log.Write("Issuing Disc command:{spell.CastName}");
                            MQ.Cmd($"/disc {spell.CastName}");
                            MQ.Delay(300);
                            returnValue = CastReturn.CAST_SUCCESS;
                            goto startCasting;
                        }

                    }
                    else if (spell.CastType == Data.CastType.Ability && MQ.Query<bool>($"${{Me.AbilityReady[{spell.CastName}]}}"))
                    {
                        _log.Write("Doing Ability based logic checks...");
                        //to deal with a slam bug
                        if (spell.CastName.Equals("Slam"))
                        {
                            _log.Write("Doing Ability:Slam based logic checks...");
                            if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FirstAbilityButton].Text.Equal[Slam]}"))
                            {
                                MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                                MQ.Cmd("/doability 1");
                            }
                            else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SecondAbilityButton].Text.Equal[Slam]}"))
                            {
                                MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                                MQ.Cmd("/doability 2");
                            }
                            else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_ThirdAbilityButton].Text.Equal[Slam]}"))
                            {
                                MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                                MQ.Cmd("/doability 3");
                            }
                            else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
                            {
                                MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                                MQ.Cmd("/doability 4");
                            }
                            else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FourthAbilityButton].Text.Equal[Slam]}"))
                            {
                                MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                                MQ.Cmd("/doability 5");
                            }
                            else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_FifthAbilityButton].Text.Equal[Slam]}"))
                            {
                                MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                                MQ.Cmd("/doability 5");
                            }
                            else if (MQ.Query<bool>("${Window[ActionsAbilitiesPage].Child[AAP_SixthAbilityButton].Text.Equal[Slam]}"))
                            {
                                MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                                MQ.Cmd("/doability 6");
                            }
                        }
                        else
                        {
                            MQ.Write($"\ag{spell.CastName} \am{targetName} \ao{targetID}");
                            MQ.Cmd($"/doability \"{spell.CastName}\"");
                        }

                        MQ.Delay(300, $"${{Me.AbilityReady[{spell.CastName}]}}");


                    }
                    else
                    {
                        //Spell, AA, Items
                        _log.Write("Doing Spell based logic checks...");


                        //TODO: SWAP ITEM

                        if (spell.MyCastTime > 300)
                        {

                            if (MQ.Query<bool>("${Stick.Status.Equal[on]}")) MQ.Cmd("/squelch /stick pause");
                            if (MQ.Query<bool>("${AdvPath.Following}") && E3._following) MQ.Cmd("/squelch /afollow off");
                            if (MQ.Query<bool>("${MoveTo.Moving}") && E3._following) MQ.Cmd("/moveto off");
                            bool navActive = MQ.Query<bool>("${Navigation.Active}");
                            if (navActive)
                            {
                                MQ.Cmd("/nav stop log=error");
                            }
                            MQ.Delay(300, "${Bool[!${Me.Moving}]}");

                        }

                        _log.Write("Doing Spell:TargetType based logic checks...");
                        if (spell.TargetType.Equals("Self"))
                        {
                            if (spell.CastType == Data.CastType.Spell)
                            {
                                MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");

                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.SpellGem}\"");
                                if (spell.MyCastTime > 500)
                                {
                                    MQ.Delay(1000);
                                }
                            }
                            else
                            {
                                if (spell.CastType == CastType.AA)
                                {
                                    MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");

                                    MQ.Cmd($"/casting \"{spell.CastName}|alt\"");
                                    if (spell.MyCastTime > 500)
                                    {
                                        MQ.Delay(1000);
                                    }
                                }
                                else
                                {
                                    MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");

                                    //else its an item
                                    MQ.Cmd($"/casting \"{spell.CastName}|{spell.CastType.ToString()}\"");
                                    if (spell.MyCastTime > 500)
                                    {
                                        MQ.Delay(1000);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (spell.CastType == Data.CastType.Spell)
                            {
                                MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");
                                MQ.Cmd($"/casting \"{spell.CastName}|{spell.SpellGem}\" \"-targetid|{targetID}\"");
                                if (spell.MyCastTime > 500)
                                {
                                    MQ.Delay(1000);
                                }
                            }
                            else
                            {
                                MQ.Write($"\ag{spell.CastName} \at{spell.SpellID} \am{targetName} \ao{targetID} \aw({spell.MyCastTime / 1000}sec)");
                                if (spell.CastType == CastType.AA)
                                {
                                    MQ.Cmd($"/casting \"{spell.CastName}|alt\" \"-targetid|{targetID}\"");
                                    if (spell.MyCastTime > 500)
                                    {
                                        MQ.Delay(1000);
                                    }
                                }
                                else
                                {
                                    //else its an item
                                    MQ.Cmd($"/casting \"{spell.CastName}|item\" \"-targetid|{targetID}\"");
                                    if (spell.MyCastTime > 500)
                                    {
                                        MQ.Delay(1000);
                                    }
                                }
                            }
                        }
                    }

                    startCasting:
                    //needed for heal interrupt check
                    Int32 currentMana = 0;
                    Int32 pctMana = 0;
                    if (interruptCheck != null)
                    {
                        currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
                        pctMana = MQ.Query<Int32>("${Me.PctMana}");
                    }
                    while (IsCasting())
                    {
                        //means that we didn't fizzle and are now casting the spell
                        if (!spell.NoInterrupt)
                        {
                            if (interruptCheck != null && interruptCheck(currentMana, pctMana))
                            {
                                MQ.Cmd("/interrupt");
                                MQ.Delay(0);
                                E3._actionTaken = true;
                                return CastReturn.CAST_INTERRUPTFORHEAL;
                            }
                            //check to see if there is a nowcast queued up, if so we need to kickout.
                            if (!isNowCast && EventProcessor._commandList.ContainsKey("/nowcast") && EventProcessor._commandList["/nowcast"].queuedEvents.Count > 0)
                            {
                                //we have a nowcast ready to be processed
                                MQ.Cmd("/interrupt");
                                MQ.Delay(0);
                                return CastReturn.CAST_INTERRUPTED;
                            }
                            //check if we need to process any events,if healing tho, ignore. 
                            if (spell.SpellType.Equals("Detrimental"))
                            {
                                EventProcessor.ProcessEventsInQueues("/backoff");
                                EventProcessor.ProcessEventsInQueues("/followme");

                                if (Assist._assistTargetID == 0)
                                {
                                    return CastReturn.CAST_INTERRUPTED;
                                }

                            }
                        }


                        MQ.Delay(50);

                    }

                    MQ.Delay(2000, "!${Cast.Status.Find[C]}");

                    returnValue = CheckForReist(spell);

                    if (returnValue == CastReturn.CAST_SUCCESS)
                    {
                        _lastSuccesfulCast = spell.CastName;
                        //clear the spell counter for this pell on this mob?
                        if (_resistCounters.ContainsKey(targetID))
                        {
                            if(_resistCounters[targetID]._spellCounters.ContainsKey(spell.SpellID))
                            {
                                _resistCounters[targetID]._spellCounters[spell.SpellID] = 0;
                            }
                        }
                    }
                    else if (returnValue == CastReturn.CAST_RESIST || returnValue == CastReturn.CAST_TAKEHOLD)
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
                    else if (returnValue== CastReturn.CAST_IMMUNE)
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
                        resist._spellCounters[spell.SpellID]=99;
                    }
                   

                    //MQ.Write($"{spell.CastName} Result:{returnValue.ToString()}");

                    //is an after spell configured? lets do that now.
                    _log.Write("Checking AfterSpell...");
                    if (!String.IsNullOrWhiteSpace(spell.AfterSpell))
                    {

                        if (spell.AfterSpellData == null)
                        {
                            spell.AfterSpellData = new Data.Spell(spell.AfterSpell);
                        }
                        //Wait for GCD if spell

                        _log.Write("Doing AfterSpell:{spell.AfterSpell}");
                        if (CheckReady(spell.AfterSpellData) && CheckMana(spell.AfterSpellData))
                        {
                            Casting.Cast(targetID, spell.AfterSpellData);
                        }
                    }
                    //after event, after all things are done               
                    _log.Write("Checking AfterEvent...");
                    if (!String.IsNullOrWhiteSpace(spell.AfterEvent))
                    {
                        _log.Write($"Doing AfterEvent:{spell.AfterEvent}");
                        MQ.Cmd($"/docommand {spell.AfterEvent}");
                    }
                    //if (Basics._following && Assist._isAssisting) Basics.AcquireFollow();

                    //TODO: bard resume twist

                    E3._actionTaken = true;
                    //clear out the queues for the resist counters as they may have a few that lagged behind.
                    ClearResistChecks();
                    return returnValue;

                }
                MQ.Write($"\arInvalid targetId for Casting. {targetID}");

                E3._actionTaken = true;
                return CastReturn.CAST_NOTARGET;
            }

        }

        public static void Sing(Int32 targetid, Data.Spell spell)
        {
            if (E3._currentClass != Data.Class.Bard) return;
            //Stop following for spell/item/aa with a cast time > 0 MyCastTime, unless im a bard
            //anything under 300 is insta cast

            if (targetid > 0)
            {
                TrueTarget(targetid);
            }

            if (spell.CastType == CastType.Spell)
            {
                //if (MQ.Query<bool>($"${{Bool[${{Me.Book[{spell.CastName}]}}]}}"))
                {
                    MQ.Cmd("/stopsong");
                    MQ.Delay(0);
                    MQ.Cmd($"/cast \"{spell.CastName}\"");
                    MQ.Delay(500, IsCasting);
                    if (!IsCasting())
                    {
                        MQ.Write("Issuing stopcast as cast window isn't open");
                        MQ.Cmd("/stopsong");
                        MQ.Delay(100);
                        MQ.Cmd($"/cast \"{spell.CastName}\"");
                        //wait for spell cast window
                        if (spell.MyCastTime > 500)
                        {
                            MQ.Delay(1000);
                        }
                    }
                }
            }
            else if (spell.CastType == CastType.Item)
            {
                if (spell.MyCastTime > 500)
                {
                    MQ.Cmd("/stopsong");
                    MQ.Delay(100);

                }
                MQ.Cmd($"/useitem \"{spell.CastName}\"");
                MQ.Delay(300);
            }
            else if (spell.CastType == CastType.AA)
            {
                if (spell.MyCastTime > 500)
                {
                    MQ.Cmd("/stopsong");
                    MQ.Delay(100);
                }
                MQ.Cmd($"/casting \"{spell.CastName}\" alt");
                MQ.Delay(300);
            }

        }
        public static bool IsSpellMemed(string spellName)
        {
            foreach(Int32 spellid in _currentSpellGems.Values)
            {
                if(spellid>0)
                {
                    string spellGemName = MQ.Query<string>($"${{Spell[{spellid}]}}");

                    if(spellGemName.Equals(spellName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;

                    }
                }
            }
            return false;
        }

        public static bool MemorizeSpell(Data.Spell spell)
        {
            if (spell.CastType != CastType.Spell)
            {
                return true;
            }
            //if no spell gem is set, set it.
            if (spell.SpellGem == 0)
            {
                spell.SpellGem = E3._generalSettings.Casting_DefaultSpellGem;
            }
            foreach(var spellid in _currentSpellGems.Values)
            {
                if (spellid == spell.SpellID && spellid != 0)
                {
                    return true;
                }
            }

            Int32 spellID;
            if (_currentSpellGems.TryGetValue(spell.SpellGem, out spellID))
            {
                if (spell.SpellID == spellID)
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
                if (_gemRecastLockForMem[spell.SpellGem] > Core._stopWatch.ElapsedMilliseconds)
                {
                    //this is still locked, return false
                    return false;
                }
            }
            MQ.Write($"\aySpell not memed, meming \ag{spell.SpellName} \ayin \awGEM:{spell.SpellGem}");
            MQ.Cmd($"/memorize \"{spell.SpellName}\" {spell.SpellGem}");
            MQ.Delay(2000);
            MQ.Delay(5000, "!${Window[SpellBookWnd].Open}");
            MQ.Delay(3000, $"${{Me.SpellReady[${{Me.Gem[{spell.SpellGem}].Name}}]}}");
        
            //make double sure the collectio has this spell gem. maybe purchased AA for new slots?
            if (!_gemRecastLockForMem.ContainsKey(spell.SpellGem))
            {
                _gemRecastLockForMem.Add(spell.SpellGem, 0);
            }
            _gemRecastLockForMem[spell.SpellGem] = Core._stopWatch.ElapsedMilliseconds + spell.RecastTime + 2000;

            //update spellgem collection
            if (!_currentSpellGems.ContainsKey(spell.SpellGem))
            {
                _currentSpellGems.Add(spell.SpellGem, spell.SpellID);
            }
            _currentSpellGems[spell.SpellGem] = spell.SpellID;
      
            return true;
        }

        public static Boolean CheckMana(Data.Spell spell)
        {

            Int32 currentMana = MQ.Query<Int32>("${Me.CurrentMana}");
            Int32 pctMana = MQ.Query<Int32>("${Me.PctMana}");
            if (currentMana >= spell.Mana)
            {
                if (pctMana >= spell.MinMana)
                {
                    if (spell.MaxMana > 0)
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
        public static Boolean IsCasting()
        {
            if(MQ.Query<bool>("${Window[CastingWindow].Open}"))
            {
                //MQ.Delay(0);
                return true;
            }
           
            return false;
        }
        public static Boolean IsNotCasting()
        {
            return !IsCasting();
        }
        public static Boolean InGlobalCooldown()
        {
        if (MQ.Query<bool>("${Me.SpellReady[${Me.Gem[1].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[3].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[5].Name}]}") || MQ.Query<bool>("${Me.SpellReady[${Me.Gem[7].Name}]}"))
            {
                return false;
            }
            return true;
        }
        public static Boolean CheckReady(Data.Spell spell)
        {
            if (spell.CastType == CastType.None) return false;
            //do we need to memorize it?
            if (!MemorizeSpell(spell))
            {
                return false;
            }

            _log.Write($"CheckReady on {spell.CastName}");

            if (E3._currentClass == Data.Class.Bard && !MQ.Query<bool>("${Twist.Twisting}"))
            {
                while (IsCasting())
                {
                    MQ.Delay(20);
                }
            }
            while (IsCasting())
            {

                MQ.Delay(20);
            }

            bool returnValue = false;
            if (spell.CastType == Data.CastType.Spell)
            {
                recheckCooldown:

                //deal with grouped spells should put in a method if it gets bigger than these two
                if (spell.CastName == "Focused Hail of Arrows" || spell.CastName == "Hail of Arrows")
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

                //if (MQ.Query<bool>("${Me.SpellInCooldown}") && MQ.Query<bool>($"${{Bool[${{Me.Gem[{spell.CastName}]}}]}}"))
                if (InGlobalCooldown())
                {
                    _log.Write("Spells in cooldown, redoing check.");
                    MQ.Delay(20);
                    goto recheckCooldown;
                }


            }
            else if (spell.CastType == Data.CastType.Item)
            {
                if (MQ.Query<bool>($"${{Me.ItemReady[{spell.CastName}]}}"))
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
        public static bool InRange(Int32 targetId, Data.Spell spell)
        {
            Spawn s;
            if (_spawns.TryByID(targetId, out s))
            {
                double targetDistance = s.Distance;
                if (targetDistance <= spell.MyRange)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool TrueTarget(Int32 targetID, bool allowClear = false)
        {

            //0 means don't change target
            if (allowClear && targetID == 0)
            {
                MQ.Cmd("/keypress esc");
                MQ.Delay(200);
                return true;
            }
            else
            {
                if (targetID == 0) return false;

            }

            _log.Write("Trying to Aquire target");

            if (MQ.Query<Int32>("${Target.ID}") == targetID) return true;

            //now to get the target
            if (MQ.Query<Int32>($"${{SpawnCount[id {targetID}]}}") > 0)
            {
                MQ.Cmd($"/target id {targetID}");
                MQ.Delay(600, $"${{Target.ID}}=={targetID}");
                if (MQ.Query<Int32>("${Target.ID}") == targetID) return true;
                return false;
            }
            else
            {
                MQ.Write("TrueTarget has no spawncount");
                return false;
            }

        }

        public static void ResetResistCounters()
        {
            //put them back in their object pools
            foreach (var kvp in Casting._resistCounters)
            {
                kvp.Value.Dispose();
            }
            _resistCounters.Clear();

        }
        [SubSystemInit]
        public static void Init()
        {
            RegisterEventsCasting();
            RegisterEventsCastResults();
            RefreshGemCache();
        }

        public static void RefreshGemCache()
        {
            if ((E3._currentClass & Class.PureMelee) == E3._currentClass)
            {
                //class doesn't have spells
                return;
            }

            if (Core._stopWatch.ElapsedMilliseconds < _currentSpellGemsLastRefresh)
            {
                return;
            }
            _currentSpellGemsLastRefresh = Core._stopWatch.ElapsedMilliseconds + 2000;
            //need to get all the spellgems setup

            for (int i = 1; i < 13; i++)
            {
                Int32 spellID = MQ.Query<Int32>($"${{Me.Gem[{i}].ID}}");
             
                string spellName = MQ.Query<string>($"${{Me.Gem[{i}]}}");
                if (!_currentSpellGems.ContainsKey(i))
                {
                    _currentSpellGems.Add(i, spellID);
                }
                _currentSpellGems[i] = spellID;
               
            }
        }
        static void RegisterEventsCasting()
        {


        }

        public static void ClearResistChecks()
        {
            MQ.Delay(100);
            Double endtime = 0;
            CheckForResistByName("CAST_TAKEHOLD", endtime);
            CheckForResistByName("CAST_RESIST", endtime);
            CheckForResistByName("CAST_FIZZLE", endtime);
            CheckForResistByName("CAST_IMMUNE", endtime);
        }
        public static CastReturn CheckForReist(Data.Spell spell)
        {
            //it takes time to wait for a spell resist, up to 2-400 millieconds.
            //basically 0 or is non determential to not resist, mostly nukes/spells
            //what this buys is is a much faster nuke/heal cycle, at the expense of checking for their resist status
            //tho debuffs/dots/buffs its more important as we have to keep track of timers, so we will pay the cost of waiting for resist checks.
            if (spell.Duration == 0)
            {
                return CastReturn.CAST_SUCCESS;
            }

            Double endtime = Core._stopWatch.Elapsed.TotalMilliseconds + 500;
            while (endtime > Core._stopWatch.Elapsed.TotalMilliseconds)
            {

                //string result = MQ.Query<string>("${Cast.Result}");

                //if (result != "CAST_SUCCESS")
                //{
                //    CastReturn r = CastReturn.CAST_INTERRUPTED;
                //    Enum.TryParse<CastReturn>(result, out r);
                //    return r;
                //}
                //frankly sometimes mq2cast is bad about getting events. do it ourselves as well
                if (CheckForResistByName("CAST_TAKEHOLD", endtime)) return CastReturn.CAST_TAKEHOLD;
                if (CheckForResistByName("CAST_RESIST", endtime)) return CastReturn.CAST_RESIST;
                if (CheckForResistByName("CAST_FIZZLE", endtime)) return CastReturn.CAST_FIZZLE;
                if (CheckForResistByName("CAST_IMMUNE", endtime)) return CastReturn.CAST_IMMUNE;
                //if (CheckForResistByName("CAST_COLLAPSE", endtime)) return CastReturn.CAST_COLLAPSE;
                //if (CheckForResistByName("CAST_CANNOTSEE", endtime)) return CastReturn.CAST_NOTARGET;
                //if (CheckForResistByName("CAST_COMPONENTS", endtime)) return CastReturn.CAST_COMPONENTS;
                //if (CheckForResistByName("CAST_DISTRACTED", endtime)) return CastReturn.CAST_DISTRACTED;
                //if (CheckForResistByName("CAST_INTERRUPTED", endtime)) return CastReturn.CAST_INTERRUPTED;
                //if (CheckForResistByName("CAST_NOTARGET", endtime)) return CastReturn.CAST_NOTARGET;
                //if (CheckForResistByName("CAST_OUTDOORS", endtime)) return CastReturn.CAST_DISTRACTED;
                MQ.Delay(100);

            }
            //assume success at this point.
            return CastReturn.CAST_SUCCESS;
        }
        public static Int64 TimeLeftOnMySpell(Data.Spell spell)
        {

            for (Int32 i = 1; i < 57; i++)
            {
                Int32 buffID = MQ.Query<Int32>($"${{Target.Buff[{i}].ID}}");

                if (spell.SpellID == buffID)
                {
                    //check if its mine
                    string casterName = MQ.Query<string>($"${{Target.Buff[{i}].Caster}}");
                    if (E3._currentName == casterName)
                    {
                        //its my spell!
                        Int64 millisecondsLeft = MQ.Query<Int64>($"${{Target.BuffDuration[{i}]}}");
                        return millisecondsLeft;
                    }
                }
            }
            return 0;
        }
        public static Int64 TimeLeftOnTargetBuff(Data.Spell spell)
        {
            Int64 millisecondsLeft = MQ.Query<Int64>($"${{Target.Buff[{spell.SpellName}].Duration}}");

            if(millisecondsLeft==0)
            {
                bool spellExists = MQ.Query<bool>($"${{Spell[{spell.SpellName}]}}");
                //doing this as -1 is a default 'bad' value for NULL, but in here a neg duration means perma.
                if(spellExists)
                {
                    //check to see if its a perm buff
                    Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
                    if (duration < 0)
                    {
                        millisecondsLeft = Int32.MaxValue;
                    }
                }
            }
            return millisecondsLeft;
        }
        public static Int64 TimeLeftOnMyPetBuff(Data.Spell spell)
        {

            Int64 millisecondsLeft = 0;
            bool buffExists = MQ.Query<bool>($"${{Me.Pet.Buff[{spell.SpellName}]}}");
            if (buffExists)
            {
                millisecondsLeft = MQ.Query<Int64>($"${{Me.Pet.Buff[{spell.SpellName}].Duration}}");
                if (millisecondsLeft == 0)
                {
                    //check if perma spell
                    Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
                    if (duration < 0)
                    {
                        millisecondsLeft = Int32.MaxValue;
                    }
                }
            }
            return millisecondsLeft;
        }
        public static Int64 TimeLeftOnMyBuff(Data.Spell spell)
        {
            Int64 millisecondsLeft = 0;
            bool buffExists = MQ.Query<bool>($"${{Me.Buff[{spell.SpellName}]}}");
            if(buffExists)
            {
                millisecondsLeft = MQ.Query<Int64>($"${{Me.Buff[{spell.SpellName}].Duration}}");
                if (millisecondsLeft == 0)
                {
                    //check if perma spell
                    Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
                    if (duration < 0)
                    {
                        millisecondsLeft = Int32.MaxValue;
                    }
                }
            }
            else
            {
                buffExists = MQ.Query<bool>($"${{Me.Song[{spell.SpellName}]}}");
                if(buffExists)
                {
                    millisecondsLeft = MQ.Query<Int64>($"${{Me.Song[{spell.SpellName}].Duration}}");
                    if (millisecondsLeft == 0)
                    {
                        //check if perma spell
                        Int32 duration = MQ.Query<Int32>($"${{Spell[{spell.SpellName}].Duration}}");
                        if (duration < 0)
                        {
                            millisecondsLeft = Int32.MaxValue;
                        }
                    }
                }
               
            }
  
            return millisecondsLeft;
        }
        private static bool CheckForResistByName(string name, Double time)
        {
            if (EventProcessor._eventList[name].queuedEvents.Count > 0)
            {
                while (EventProcessor._eventList[name].queuedEvents.Count > 0)
                {
                    EventProcessor.EventMatch e;
                    EventProcessor._eventList[name].queuedEvents.TryDequeue(out e);
                }
                // MQ.Write($"*********Total Time for Internal result: {500 - (time - Core._stopWatch.Elapsed.TotalMilliseconds)} ms");

                return true;
            }
            return false;
        }
        static void RegisterEventsCastResults()
        {
            List<String> r = new List<string>();
            //r.Add("Your gate is too unstable, and collapses.*");
            //EventProcessor.RegisterEvent("CAST_COLLAPSE", r, (x) => {
            //    //not doing anything, casting code will remove this from the collection if it detects
            //    //so this will never be called.
            //});

            //r = new List<string>();
            //r.Add("You cannot see your target.");
            //EventProcessor.RegisterEvent("CAST_CANNOTSEE", r, (x) => {
            //});

            //r = new List<string>();
            //r.Add("You need to play a.+ for this song.");
            //EventProcessor.RegisterEvent("CAST_COMPONENTS", r, (x) => {
            //});

            //r = new List<string>();
            //r.Add("You are too distracted to cast a spell now.");
            //r.Add("You can't cast spells while invulnerable.");
            //r.Add("You *CANNOT* cast spells, you have been silenced.");
            //EventProcessor.RegisterEvent("CAST_DISTRACTED", r, (x) => {
            //});

            r = new List<string>();
            r.Add("Your target has no mana to affect.");
            r.Add("Your target looks unaffected.");
            r.Add("Your target is immune to changes in its attack speed.");
            r.Add("Your target is immune to changes in its run speed.");
            r.Add("Your target is immune to snare spells.");
            r.Add("Your target cannot be mesmerized.");
            r.Add("Your target looks unaffected.");
            EventProcessor.RegisterEvent("CAST_IMMUNE", r, (x) =>
            {
            });


            //r = new List<string>();
            //r.Add("Your .+ is interrupted.");
            //r.Add("Your spell is interrupted.");
            //r.Add("Your casting has been interrupted.");
            //EventProcessor.RegisterEvent("CAST_INTERRUPTED", r, (x) => {
            //});

            r = new List<string>();
            r.Add("Your spell fizzles.");
            r.Add("Your .+ spell fizzles.");
            r.Add(@"You miss a note, bringing your song to a close\.");
            EventProcessor.RegisterEvent("CAST_FIZZLE", r, (x) =>
            {
            });

            //r = new List<string>();
            //r.Add("You must first select a target for this spell.");
            //r.Add("This spell only works on.*");
            //r.Add("You must first target a group member.");
            //EventProcessor.RegisterEvent("CAST_NOTARGET", r, (x) => {
            //});

            //r = new List<string>();
            //r.Add("This spell does not work here.");
            //r.Add("You can only cast this spell in the outdoors.");
            //r.Add("You can not summon a mount here.");
            //r.Add("You must have both the Horse Models and your current Luclin Character Model enabled to summon a mount.");
            //EventProcessor.RegisterEvent("CAST_OUTDOORS", r, (x) => {
            //});

            r = new List<string>();
            r.Add(@"Your target resisted the .+ spell\.");
            //TODO deal with live vs non live
            //r.Add(".+ resisted your .+\!"); //for live?
            //r.Add(".+ avoided your .+!"); //for live?
            EventProcessor.RegisterEvent("CAST_RESIST", r, (x) =>
            {
            });

            //r = new List<string>();
            //r.Add("You can't cast spells while stunned.");
            ////TODO deal with live vs non live
            ////r.Add(".+ resisted your .+\!"); //for live?
            ////r.Add(".+ avoided your .+!"); //for live?
            //EventProcessor.RegisterEvent("CAST_STUNNED", r, (x) => {
            //});

            //r = new List<string>();
            //r.Add("You can't cast spells while stunned.");
            ////TODO deal with live vs non live
            ////r.Add(".+ resisted your .+\!"); //for live?
            ////r.Add(".+ avoided your .+!"); //for live?
            //EventProcessor.RegisterEvent("CAST_STUNNED", r, (x) => {
            //});

            r = new List<string>();
            r.Add(@" spell did not take hold. \(Blocked by");
            r.Add(@" did not take hold on .+ \(Blocked by");
            r.Add(@"Your spell did not take hold\.");
            r.Add(@"Your spell would not have taken hold\.");
            r.Add(@"Your spell is too powerful for your intended target\.");
            EventProcessor.RegisterEvent("CAST_TAKEHOLD", r, (x) =>
            {
            });


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
        CAST_INTERRUPTFORHEAL,
        CAST_CORPSEOPEN

    }

}

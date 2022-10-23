using E3Core.Processors;
using E3Core.Settings;
using System;
using E3Core.Classes;
using E3Core.Data;
using E3Core.Utility;
using MonoCore;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace E3Core.Classes
{
    public static class Necromancer
    {
        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        private static Int64 _nextAggroCheck = 0;
        private static Int64 _nextAggroRefreshTimeInterval = 1000;
        private static Int32 _maxAggroCap = 90;
        [AdvSettingInvoke]
        public static void Check_NecroAggro()
        {
            if (!e3util.ShouldCheck(ref _nextAggroCheck, _nextAggroRefreshTimeInterval)) return;

            Int32 currentAggro = 0;
            Int32 tempMaxAggro = 0;

            for (Int32 i = 1; i <= 13; i++)
            {
                bool autoHater = MQ.Query<bool>($"${{Me.XTarget[{i}].TargetType.Equal[Auto Hater]}}");
                if (autoHater) continue;
                Int32 mobId = MQ.Query<Int32>($"${{Me.XTarget[{i}].ID}}");
                if (mobId > 0)
                {
                     Spawn s;
                    if (_spawns.TryByID(mobId, out s))
                    {
                        if (s.Aggressive)
                        {
                            currentAggro = MQ.Query<Int32>($"${{Me.XTarget[{i}].PctAggro}}");
                            if(tempMaxAggro<currentAggro)
                            {
                                tempMaxAggro = currentAggro;
                            }
                        }
                    }
                }
            }
            if(tempMaxAggro>_maxAggroCap && !MQ.Query<bool>("${Bool[${Me.Song[Gathering Dusk]}]}"))
            {
                
                Spell s;
                if(!Spell._loadedSpellsByName.TryGetValue("Improved Death Peace",out s))
                {
                    s = new Spell("Improved Death Peace");
                }
                if(Casting.CheckReady(s) && Casting.CheckMana(s))
                {
                    Casting.Cast(0, s);
                    MQ.Cmd("/stand");
                    return;
                }
                if (!Spell._loadedSpellsByName.TryGetValue("Death Peace", out s))
                {
                    s = new Spell("Death Peace");
                }
                if (Casting.CheckReady(s) && Casting.CheckMana(s))
                {
                    Casting.Cast(0, s);
                    MQ.Cmd("/stand");
                    return;
                }

            } 
            else if(tempMaxAggro>_maxAggroCap && !MQ.Query<bool>("${Bool[${Me.Song[Harmshield]}]}"))
            {

                Spell s;

                if (!Spell._loadedSpellsByName.TryGetValue("Embalmer's Carapace", out s))
                {
                    s = new Spell("Embalmer's Carapace");
                }
                if (Casting.CheckReady(s) && Casting.CheckMana(s))
                {
                    Casting.Cast(0, s);
                    return;
                }

                if (!Spell._loadedSpellsByName.TryGetValue("Harmshield", out s))
                {
                    s = new Spell("Harmshield");
                }
                if (Casting.CheckReady(s) && Casting.CheckMana(s))
                {
                    Casting.Cast(0, s);
                    return;
                }
               

            }



        }

    }
}

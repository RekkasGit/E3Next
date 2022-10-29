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
    public static class Shaman
    {

        public static Logging _log = E3._log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3._spawns;

        private static Int64 _nextAggroCheck = 0;
        private static Int64 _nextAggroRefreshTimeInterval = 1000;
        private static Int64 _nextTotemCheck = 0;
        private static Int64 _nextTotemRefreshTimeInterval = 1000;

        private static Int32 _maxAggroCap = 90;

        [AdvSettingInvoke]
        public static void Check_ShamanAggro()
        {

            if (!e3util.ShouldCheck(ref _nextAggroCheck, _nextAggroRefreshTimeInterval)) return;

            BuffCheck.DropBuff("Inconspicuous Totem");

            Int32 currentAggro = 0;
            Int32 tempMaxAggro = 0;

            for (Int32 i = 1; i <= 13; i++)
            {
                bool autoHater = MQ.Query<bool>($"${{Me.XTarget[{i}].TargetType.Equal[Auto Hater]}}");
                if (!autoHater) continue;
                Int32 mobId = MQ.Query<Int32>($"${{Me.XTarget[{i}].ID}}");
                if (mobId > 0)
                {
                    Spawn s;
                    if (_spawns.TryByID(mobId, out s))
                    {
                        if (s.Aggressive)
                        {
                            currentAggro = MQ.Query<Int32>($"${{Me.XTarget[{i}].PctAggro}}");
                            if (tempMaxAggro < currentAggro)
                            {
                                tempMaxAggro = currentAggro;
                            }
                        }
                    }
                }
            }
            if (tempMaxAggro > _maxAggroCap)
            {

                Spell s;
                if (!Spell._loadedSpellsByName.TryGetValue("Inconspicuous Totem", out s))
                {
                    s = new Spell("Inconspicuous Totem");
                }
                if (Casting.CheckReady(s) && Casting.CheckMana(s))
                {
                    Casting.Cast(0, s);
                    MQ.Delay(400);
                    BuffCheck.DropBuff("Inconspicuous Totem");
                    return;
                }
               
            }
            

        }

        [AdvSettingInvoke]
        public static void Check_MalosTotem()
        {
            if (!e3util.ShouldCheck(ref _nextTotemCheck, _nextTotemRefreshTimeInterval)) return;
            if(Movement._anchorTarget>0)
            {
                bool idolUp = MQ.Query<bool>("${Bool[${Spawn[Spirit Idol]}]}");
            
                if (!idolUp)
                {
                    Spell s;
                    if (!Spell._loadedSpellsByName.TryGetValue("Idol of Malos", out s))
                    {
                        s = new Spell("Idol of Malos");
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
}

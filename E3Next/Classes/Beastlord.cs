using E3Core.Processors;
using E3Core.Settings;
using E3Core.Utility;

using MonoCore;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the beastlord class
    /// </summary>
    public static class Beastlord
    {
        private static Logging _log = E3.Log;
        private static IMQ MQ = E3.MQ;
        private static ISpawns _spawns = E3.Spawns;
        private static long _nextParagonCheck = 0;
        private static long _nextParagonCheckInterval = 1000;
        private static long _nextFocusedParagonCheck = 0;
        private static long _nextFocusedParagonCheckInterval = 1000;

        [ClassInvoke(Data.Class.Beastlord)]
        public static void CheckParagon()
        {
            if (E3.IsInvis) return;
            if (!e3util.ShouldCheck(ref _nextParagonCheck, _nextParagonCheckInterval)) return;
            if (!Casting.CheckReady(E3.CharacterSettings.ParagonSpell)) return;
            if (!MQ.Query<bool>("${Group}")) return;
            if (MQ.Query<int>("${Raid.Members}") > 0) return;
            if (E3.CharacterSettings.AutoParagon)
            {
                if (MQ.Query<int>($"${{Group.LowMana[{E3.CharacterSettings.ParagonManaPct}]}}") >= 2)
                {
                    Casting.Cast(E3.CurrentId, E3.CharacterSettings.ParagonSpell);
                }
            }
        }

        [ClassInvoke(Data.Class.Beastlord)]
        public static void CheckFocusedParagon()
        {
            if (E3.IsInvis) return;
            if (!e3util.ShouldCheck(ref _nextFocusedParagonCheck, _nextFocusedParagonCheckInterval)) return;
            if (E3.CharacterSettings.AutoFocusedParagon)
            {
                foreach (var character in E3.CharacterSettings.FocusedParagonCharacters)
                {
                    MQ.Cmd($"/dobserve {character} -q Me.PctMana");
                    if (_spawns.TryByName(character, out var characterSpawn))
                    {
                        if (!Casting.CheckReady(E3.CharacterSettings.FocusedParagonSpell)) return;
                        var pctMana = MQ.Query<int>($"${{DanNet[{character}].Observe[Me.PctMana]}}");
                        if (pctMana < E3.CharacterSettings.FocusedParagonManaPct)
                        {
                            if (characterSpawn.Distance < E3.CharacterSettings.FocusedParagonSpell.MyRange)
                                Casting.Cast(characterSpawn.ID, E3.CharacterSettings.FocusedParagonSpell);
                        }
                    }
                }
            }
        }
    }
}

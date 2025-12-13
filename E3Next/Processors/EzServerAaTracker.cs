using System.Globalization;
using MonoCore;

namespace E3Core.Processors
{
    public static class EzServerAaTracker
    {
        private const string UnspentPattern = @"Unspent AA:\s*([\d,]+)";
        private const string AbilityPointsPattern = @"You now have\s+([\d,]+)\s+ability point\(s\)";

        [SubSystemInit]
        public static void Init()
        {
            EventProcessor.RegisterEvent("E3_EZ_UnspentAA", UnspentPattern, HandleAaUpdate);
            EventProcessor.RegisterEvent("E3_EZ_AbilityPoints", AbilityPointsPattern, HandleAaUpdate);
        }

        private static void HandleAaUpdate(EventProcessor.EventMatch match)
        {
            if (!E3.IsEzServer)
            {
                return;
            }

            if (match?.match == null || match.match.Groups.Count <= 1)
            {
                return;
            }

            var capturedValue = match.match.Groups[1].Value ?? string.Empty;
            if (TryParseAaValue(capturedValue, out var aaPoints))
            {
                E3.UpdateEzServerAaPoints(aaPoints);
            }
        }

        private static bool TryParseAaValue(string text, out int aaPoints)
        {
            aaPoints = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var normalized = text.Replace(",", string.Empty);
            return int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out aaPoints);
        }
    }
}

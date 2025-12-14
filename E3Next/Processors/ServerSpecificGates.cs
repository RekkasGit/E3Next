using System;

namespace E3Core.Processors
{
    public enum ServerId
    {
        Unknown = 0,
        EqMight,
        Lazarus,
        EzServer
    }

    public static class ServerSpecificGates
    {
        private static readonly ServerId[] _priorityOrder =
        {
            ServerId.EqMight,
            ServerId.Lazarus,
            ServerId.EzServer
        };

        public static bool IsServer(ServerId serverId)
        {
            switch (serverId)
            {
                case ServerId.EqMight:
                    return MatchesMacroQuestServerName("EQ_Might");
                case ServerId.Lazarus:
                    return MatchesMacroQuestServerName("Lazarus");
                case ServerId.EzServer:
                    return MatchesMacroQuestServerName("EZ_(Linux)_x4_Exp")
                           || MatchesEverQuestServerName("EZ (Linux) x4 Exp");
                default:
                    return false;
            }
        }

        public static ServerId GetActiveServer()
        {
            foreach (var server in _priorityOrder)
            {
                if (IsServer(server))
                {
                    return server;
                }
            }

            return ServerId.Unknown;
        }

        private static bool MatchesMacroQuestServerName(string expected)
        {
            if (string.IsNullOrWhiteSpace(expected)) return false;
            var current = E3.ServerName ?? string.Empty;
            return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesEverQuestServerName(string expected)
        {
            if (string.IsNullOrWhiteSpace(expected)) return false;
            var current = E3.EverQuestServerName ?? string.Empty;
            return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}

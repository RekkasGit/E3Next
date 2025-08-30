#include "E3Plus.h"
#include <string>
#include "Config.h"
#include "E3Plus_UI.h"

// Heals module
namespace Heals { void Tick(); void HandleCommand(const char* szLine); void Status(); }

PreSetup("MQ2E3Plus");
PLUGIN_VERSION(0.1);

bool g_enabled = false;
uint64_t g_lastTick = 0;
int g_tickIntervalMs = 25;

static void PrintHelp()
{
    WriteChatf("[E3Plus] Usage:");
    WriteChatf("  /e3plus on|off|status|tick <ms>");
    WriteChatf("  /e3plus heal on|off|status");
    WriteChatf("  /e3plus heal life \"Spell Name\" <pct>");
    WriteChatf("  /e3plus heal single \"Spell Name\" <pct>");
    WriteChatf("  /e3plus heal group \"Spell Name\" <pct> <minInjured>");
    WriteChatf("  /e3plus ui");
}

void E3PlusCommand(PlayerClient* pChar, const char* szLine)
{
    char arg1[MAX_STRING] = {0};
    GetArg(arg1, szLine, 1);

    if (_stricmp(arg1, "heal") == 0)
    {
        Heals::HandleCommand(szLine);
        return;
    }

    if (_stricmp(arg1, "ui") == 0)
    {
        E3PlusUI::Toggle();
        return;
    }

    if (_stricmp(arg1, "on") == 0)
    {
        g_enabled = true;
        E3PlusCfg::g_core.enabled = true;
        WriteChatf("[E3Plus] Enabled");
        return;
    }

    if (_stricmp(arg1, "off") == 0)
    {
        g_enabled = false;
        E3PlusCfg::g_core.enabled = false;
        WriteChatf("[E3Plus] Disabled");
        return;
    }

    if (_stricmp(arg1, "status") == 0)
    {
        WriteChatf("[E3Plus] %s, tick=%dms", g_enabled ? "Enabled" : "Disabled", g_tickIntervalMs);
        Heals::Status();
        return;
    }

    if (_stricmp(arg1, "tick") == 0)
    {
        char arg2[MAX_STRING] = {0};
        GetArg(arg2, szLine, 2);
        if (arg2[0])
        {
            int v = atoi(arg2);
            if (v < 1) v = 1;
            if (v > 250) v = 250;
            g_tickIntervalMs = v;
            E3PlusCfg::g_core.tickIntervalMs = v;
            WriteChatf("[E3Plus] Set tick interval to %d ms", g_tickIntervalMs);
        }
        else
        {
            WriteChatf("[E3Plus] Current tick interval: %d ms", g_tickIntervalMs);
        }
        return;
    }

    PrintHelp();
}

static inline uint64_t NowMs()
{
    return MQGetTickCount64();
}

void E3Plus_Tick()
{
    if (!g_enabled) return;
    if (GetGameState() != GAMESTATE_INGAME) return;

    uint64_t now = NowMs();
    if (now - g_lastTick < static_cast<uint64_t>(g_tickIntervalMs)) return;
    g_lastTick = now;

    // Placeholder for future ported logic from E3Next.Process()
    // Keep this extremely light per frame until features are ported.

    // Heals
    Heals::Tick();
}

PLUGIN_API void InitializePlugin()
{
    // Load config
    E3PlusCfg::LoadFromIni();

    // Apply core config on startup
    g_enabled = E3PlusCfg::g_core.enabled;
    g_tickIntervalMs = E3PlusCfg::g_core.tickIntervalMs;

    AddCommand("/e3plus", E3PlusCommand);
    WriteChatf("[E3Plus] Loaded");
}

PLUGIN_API void ShutdownPlugin()
{
    RemoveCommand("/e3plus");
    WriteChatf("[E3Plus] Unloaded");
}

PLUGIN_API void OnPulse()
{
    E3Plus_Tick();
}

PLUGIN_API void OnUpdateImGui()
{
    if (GetGameState() != GAMESTATE_INGAME) return;
    // Keep core settings in sync live
    g_enabled = E3PlusCfg::g_core.enabled;
    g_tickIntervalMs = E3PlusCfg::g_core.tickIntervalMs;
    if (E3PlusUI::IsVisible()) {
        E3PlusUI::Render();
    }
}

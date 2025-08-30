#include "Config.h"
#include <windows.h>
#include <filesystem>
#include <mq/Plugin.h>

namespace fs = std::filesystem;

namespace E3PlusCfg {

CoreConfig g_core;
HealsConfig g_heals;

static std::string g_iniPath;

std::string IniPath()
{
    if (!g_iniPath.empty()) return g_iniPath;
    // Place ini next to MacroQuest executable (working directory)
    g_iniPath = "MQ2E3Plus.ini";
    return g_iniPath;
}

static std::string ReadString(const char* section, const char* key, const char* def)
{
    char buf[1024] = {0};
    GetPrivateProfileStringA(section, key, def, buf, (DWORD)sizeof(buf), IniPath().c_str());
    return std::string(buf);
}

static int ReadInt(const char* section, const char* key, int def)
{
    return (int)GetPrivateProfileIntA(section, key, def, IniPath().c_str());
}

static void WriteString(const char* section, const char* key, const std::string& val)
{
    WritePrivateProfileStringA(section, key, val.c_str(), IniPath().c_str());
}

static void WriteInt(const char* section, const char* key, int val)
{
    WritePrivateProfileStringA(section, key, std::to_string(val).c_str(), IniPath().c_str());
}

void LoadFromIni()
{
    // Core
    g_core.enabled = ReadInt("Core", "Enabled", 0) != 0;
    g_core.tickIntervalMs = ReadInt("Core", "TickInterval", 25);

    // Heals
    g_heals.enabled = ReadInt("Heals", "Enabled", 1) != 0;
    g_heals.lifeSpell = ReadString("Heals", "LifeSpell", "");
    g_heals.lifePct = ReadInt("Heals", "LifePct", 50);
    g_heals.singleSpell = ReadString("Heals", "SingleSpell", "");
    g_heals.singlePct = ReadInt("Heals", "SinglePct", 75);
    g_heals.groupSpell = ReadString("Heals", "GroupSpell", "");
    g_heals.groupPct = ReadInt("Heals", "GroupPct", 75);
    g_heals.groupMin = ReadInt("Heals", "GroupMin", 3);
}

void SaveToIni()
{
    // Core
    WriteInt("Core", "Enabled", g_core.enabled ? 1 : 0);
    WriteInt("Core", "TickInterval", g_core.tickIntervalMs);

    // Heals
    WriteInt("Heals", "Enabled", g_heals.enabled ? 1 : 0);
    WriteString("Heals", "LifeSpell", g_heals.lifeSpell);
    WriteInt("Heals", "LifePct", g_heals.lifePct);
    WriteString("Heals", "SingleSpell", g_heals.singleSpell);
    WriteInt("Heals", "SinglePct", g_heals.singlePct);
    WriteString("Heals", "GroupSpell", g_heals.groupSpell);
    WriteInt("Heals", "GroupPct", g_heals.groupPct);
    WriteInt("Heals", "GroupMin", g_heals.groupMin);
}

}


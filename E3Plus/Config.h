#pragma once

#include <string>

namespace E3PlusCfg {

struct CoreConfig {
    bool enabled = false;
    int tickIntervalMs = 25;
};

struct HealsConfig {
    bool enabled = true;
    std::string lifeSpell;
    int lifePct = 50;
    std::string singleSpell;
    int singlePct = 75;
    std::string groupSpell;
    int groupPct = 75;
    int groupMin = 3;
};

extern CoreConfig g_core;
extern HealsConfig g_heals;

std::string IniPath();
void LoadFromIni();
void SaveToIni();

}


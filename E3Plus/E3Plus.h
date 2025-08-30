#pragma once

#include <mq/Plugin.h>
#include "Heals.h"
#include <string>

extern bool g_enabled;
extern uint64_t g_lastTick;
extern int g_tickIntervalMs;

void E3PlusCommand(PlayerClient* pChar, const char* szLine);
void E3Plus_Tick();

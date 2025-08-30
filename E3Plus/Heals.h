#pragma once

#include <mq/Plugin.h>

namespace Heals {
    void Tick();
    void HandleCommand(const char* szLine);
    void Status();
}


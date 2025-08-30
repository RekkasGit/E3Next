#include "E3Plus_UI.h"
#include "Config.h"
#include "Heals.h"
#include <imgui.h>
#include <string>

using namespace E3PlusCfg;

namespace E3PlusUI {

static bool s_visible = false;

void Toggle() { s_visible = !s_visible; }
bool IsVisible() { return s_visible; }

static void InputText(const char* label, std::string& value, float width = 250.0f)
{
    ImGui::PushItemWidth(width);
    char buf[256] = {0};
    strncpy_s(buf, value.c_str(), sizeof(buf) - 1);
    if (ImGui::InputText(label, buf, sizeof(buf))) {
        value = buf;
    }
    ImGui::PopItemWidth();
}

void Render()
{
    if (!s_visible) return;

    if (ImGui::Begin("E3Plus", &s_visible))
    {
        if (ImGui::CollapsingHeader("Core", ImGuiTreeNodeFlags_DefaultOpen))
        {
            ImGui::Checkbox("Enabled", &g_core.enabled);
            ImGui::SameLine();
            ImGui::SetNextItemWidth(100);
            ImGui::InputInt("Tick (ms)", &g_core.tickIntervalMs);
            if (g_core.tickIntervalMs < 1) g_core.tickIntervalMs = 1;
            if (g_core.tickIntervalMs > 250) g_core.tickIntervalMs = 250;
        }

        if (ImGui::CollapsingHeader("Heals", ImGuiTreeNodeFlags_DefaultOpen))
        {
            ImGui::Checkbox("Heals Enabled", &g_heals.enabled);
            
            ImGui::SeparatorText("Life Support");
            InputText("Life Spell", g_heals.lifeSpell);
            ImGui::SetNextItemWidth(100);
            ImGui::InputInt("Life %", &g_heals.lifePct);
            if (g_heals.lifePct < 1) g_heals.lifePct = 1;
            if (g_heals.lifePct > 99) g_heals.lifePct = 99;

            ImGui::SeparatorText("Single Target Heal");
            InputText("Single Spell", g_heals.singleSpell);
            ImGui::SetNextItemWidth(100);
            ImGui::InputInt("Single %", &g_heals.singlePct);
            if (g_heals.singlePct < 1) g_heals.singlePct = 1;
            if (g_heals.singlePct > 99) g_heals.singlePct = 99;

            ImGui::SeparatorText("Group Heal");
            InputText("Group Spell", g_heals.groupSpell);
            ImGui::SetNextItemWidth(100);
            ImGui::InputInt("Group %", &g_heals.groupPct);
            if (g_heals.groupPct < 1) g_heals.groupPct = 1;
            if (g_heals.groupPct > 99) g_heals.groupPct = 99;
            ImGui::SameLine();
            ImGui::SetNextItemWidth(100);
            ImGui::InputInt("Min Injured", &g_heals.groupMin);
            if (g_heals.groupMin < 1) g_heals.groupMin = 1;
        }

        if (ImGui::Button("Load from INI")) {
            LoadFromIni();
        }
        ImGui::SameLine();
        if (ImGui::Button("Save to INI")) {
            SaveToIni();
        }
        ImGui::SameLine();
        if (ImGui::Button("Close")) {
            s_visible = false;
        }
    }
    ImGui::End();
}

}


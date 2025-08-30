#include "Heals.h"
#include <string>
#include "Config.h"
#include <algorithm>

// Simple TLO parsing helpers using MQ's macro parser
static std::string QueryTLO(const char* expr)
{
    char buffer[MAX_STRING] = {0};
    // Use parser v2 for consistent behavior (like MQ2Mono does)
    auto old = std::exchange(gParserVersion, 2);
    strncpy_s(buffer, expr, MAX_STRING);
    if (!ParseMacroData(buffer, sizeof(buffer))) buffer[0] = 0;
    gParserVersion = old;
    return std::string(buffer);
}

static int QueryInt(const char* expr, int def = 0)
{
    std::string s = QueryTLO(expr);
    if (s.empty() || _stricmp(s.c_str(), "NULL") == 0) return def;
    return atoi(s.c_str());
}

static bool QueryBool(const char* expr, bool def = false)
{
    std::string s = QueryTLO(expr);
    if (s.empty() || _stricmp(s.c_str(), "NULL") == 0) return def;
    if (_stricmp(s.c_str(), "TRUE") == 0 || s == "1") return true;
    if (_stricmp(s.c_str(), "FALSE") == 0 || s == "0") return false;
    return def;
}

static void DoCmd(const std::string& cmd, bool delayed = false)
{
    HideDoCommand(pLocalPlayer, cmd.c_str(), delayed);
}

namespace Heals {
    static bool s_enabled = true;
    static std::string s_lifeSpell;
    static int s_lifePct = 50;
    static std::string s_singleSpell;
    static int s_singlePct = 75;
    static std::string s_groupSpell;
    static int s_groupPct = 75;
    static int s_groupMin = 3;
    static uint64_t s_lastCastTs = 0;
    static int s_castCooldownMs = 500; // basic anti-spam

    static inline uint64_t NowMs() { return MQGetTickCount64(); }

    static bool CanCast()
    {
        if (QueryBool("${Me.Casting}", false)) return false;
        uint64_t now = NowMs();
        if (now - s_lastCastTs < (uint64_t)s_castCooldownMs) return false;
        return true;
    }

    static bool SpellReady(const std::string& name)
    {
        if (name.empty()) return false;
        char expr[512] = {0};
        // Many MQ setups support Me.SpellReady["name"], returns TRUE when ready
        _snprintf_s(expr, _TRUNCATE, "${Me.SpellReady[\"%s\"]}", name.c_str());
        return QueryBool(expr, true); // default true to avoid blocking on unknown
    }

    static void CastOnSelf(const std::string& name)
    {
        if (name.empty()) return;
        // Target self, then cast by name
        DoCmd("/squelch /target id ${Me.ID}");
        DoCmd(std::string("/cast \"") + name + "\"");
        s_lastCastTs = NowMs();
    }

    static bool CheckLifeSupport()
    {
        if (!s_enabled || s_lifeSpell.empty()) return false;
        int pctHPs = QueryInt("${Me.PctHPs}", 100);
        if (pctHPs <= s_lifePct && CanCast() && SpellReady(s_lifeSpell)) {
            CastOnSelf(s_lifeSpell);
            return true;
        }
        return false;
    }

    static bool CheckGroupHeal()
    {
        if (!s_enabled || s_groupSpell.empty()) return false;
        char expr[128];
        _snprintf_s(expr, _TRUNCATE, "${Group.Injured[%d]}", s_groupPct);
        int injured = QueryInt(expr, 0);
        if (injured >= s_groupMin && CanCast() && SpellReady(s_groupSpell)) {
            // Most group heals are self-cast centered; ensure self target is ok
            CastOnSelf(s_groupSpell);
            return true;
        }
        return false;
    }

    static bool CheckSingleTarget()
    {
        if (!s_enabled || s_singleSpell.empty()) return false;
        int members = QueryInt("${Group.Members}", 0);
        int bestPct = 101;
        std::string bestName;

        for (int i = 0; i <= members; ++i) {
            char nameExpr[64];
            _snprintf_s(nameExpr, _TRUNCATE, "${Group.Member[%d].Name}", i);
            std::string name = QueryTLO(nameExpr);
            if (name.empty() || _stricmp(name.c_str(), "NULL") == 0) continue;

            char hpExpr[96];
            _snprintf_s(hpExpr, _TRUNCATE, "${Group.Member[%d].Spawn.PctHPs}", i);
            int pct = QueryInt(hpExpr, 100);
            if (pct == 0) continue; // dead or unknown
            char distExpr[96];
            _snprintf_s(distExpr, _TRUNCATE, "${Group.Member[%d].Spawn.Distance}", i);
            int dist = QueryInt(distExpr, 9999);
            if (dist > 0 && dist <= 200 && pct < bestPct) {
                bestPct = pct;
                bestName = name;
            }
        }

        if (!bestName.empty() && bestPct <= s_singlePct && CanCast() && SpellReady(s_singleSpell)) {
            // Target then cast
            DoCmd(std::string("/squelch /target ") + bestName);
            DoCmd(std::string("/cast \"") + s_singleSpell + "\"");
            s_lastCastTs = NowMs();
            return true;
        }
        return false;
    }

    void Tick()
    {
        if (GetGameState() != GAMESTATE_INGAME) return;
        // Skip while invis like E3Next
        if (QueryBool("${Me.Invis}", false)) return;

        // Pull config live from INI state to allow live editing via UI/commands
        s_enabled = E3PlusCfg::g_heals.enabled;
        s_lifeSpell = E3PlusCfg::g_heals.lifeSpell;
        s_lifePct = E3PlusCfg::g_heals.lifePct;
        s_singleSpell = E3PlusCfg::g_heals.singleSpell;
        s_singlePct = E3PlusCfg::g_heals.singlePct;
        s_groupSpell = E3PlusCfg::g_heals.groupSpell;
        s_groupPct = E3PlusCfg::g_heals.groupPct;
        s_groupMin = E3PlusCfg::g_heals.groupMin;

        // Priority: life support > group > single
        if (CheckLifeSupport()) return;
        if (CheckGroupHeal()) return;
        (void)CheckSingleTarget();
    }

    static void PrintStatusLine(const char* label, const std::string& spell, int pct, int extra = -1)
    {
        if (!spell.empty()) {
            if (extra >= 0)
                WriteChatf("[E3Plus] %-10s: %s @%d%% (extra=%d)", label, spell.c_str(), pct, extra);
            else
                WriteChatf("[E3Plus] %-10s: %s @%d%%", label, spell.c_str(), pct);
        } else {
            WriteChatf("[E3Plus] %-10s: <disabled>", label);
        }
    }

    void Status()
    {
        // Show current config state (from shared config)
        WriteChatf("[E3Plus] Heals: %s", E3PlusCfg::g_heals.enabled ? "Enabled" : "Disabled");
        PrintStatusLine("Life", E3PlusCfg::g_heals.lifeSpell, E3PlusCfg::g_heals.lifePct);
        PrintStatusLine("Single", E3PlusCfg::g_heals.singleSpell, E3PlusCfg::g_heals.singlePct);
        PrintStatusLine("Group", E3PlusCfg::g_heals.groupSpell, E3PlusCfg::g_heals.groupPct, E3PlusCfg::g_heals.groupMin);
    }

    void HandleCommand(const char* szLine)
    {
        char sub[MAX_STRING] = {0};
        GetArg(sub, szLine, 2); // first arg is 'heal'

        if (_stricmp(sub, "on") == 0) { s_enabled = true; E3PlusCfg::g_heals.enabled = true; WriteChatf("[E3Plus] Heals enabled"); return; }
        if (_stricmp(sub, "off") == 0) { s_enabled = false; E3PlusCfg::g_heals.enabled = false; WriteChatf("[E3Plus] Heals disabled"); return; }
        if (_stricmp(sub, "status") == 0) { Status(); return; }

        if (_stricmp(sub, "life") == 0) {
            char spell[MAX_STRING] = {0};
            char pctStr[MAX_STRING] = {0};
            GetArg(spell, szLine, 3, true, false, true, '"'); // quoted spell name
            GetArg(pctStr, szLine, 4);
            if (spell[0] && pctStr[0]) {
                s_lifeSpell = spell; E3PlusCfg::g_heals.lifeSpell = spell;
                s_lifePct = std::clamp(atoi(pctStr), 1, 99); E3PlusCfg::g_heals.lifePct = s_lifePct;
                WriteChatf("[E3Plus] Life support: '%s' @%d%%", s_lifeSpell.c_str(), s_lifePct);
            } else {
                WriteChatf("[E3Plus] Usage: /e3plus heal life \"Spell Name\" <pct>");
            }
            return;
        }

        if (_stricmp(sub, "single") == 0) {
            char spell[MAX_STRING] = {0};
            char pctStr[MAX_STRING] = {0};
            GetArg(spell, szLine, 3, true, false, true, '"');
            GetArg(pctStr, szLine, 4);
            if (spell[0] && pctStr[0]) {
                s_singleSpell = spell; E3PlusCfg::g_heals.singleSpell = spell;
                s_singlePct = std::clamp(atoi(pctStr), 1, 99); E3PlusCfg::g_heals.singlePct = s_singlePct;
                WriteChatf("[E3Plus] Single heal: '%s' @%d%%", s_singleSpell.c_str(), s_singlePct);
            } else {
                WriteChatf("[E3Plus] Usage: /e3plus heal single \"Spell Name\" <pct>");
            }
            return;
        }

        if (_stricmp(sub, "group") == 0) {
            char spell[MAX_STRING] = {0};
            char pctStr[MAX_STRING] = {0};
            char minStr[MAX_STRING] = {0};
            GetArg(spell, szLine, 3, true, false, true, '"');
            GetArg(pctStr, szLine, 4);
            GetArg(minStr, szLine, 5);
            if (spell[0] && pctStr[0] && minStr[0]) {
                s_groupSpell = spell; E3PlusCfg::g_heals.groupSpell = spell;
                s_groupPct = std::clamp(atoi(pctStr), 1, 99); E3PlusCfg::g_heals.groupPct = s_groupPct;
                s_groupMin = std::max(1, atoi(minStr)); E3PlusCfg::g_heals.groupMin = s_groupMin;
                WriteChatf("[E3Plus] Group heal: '%s' @%d%% min=%d", s_groupSpell.c_str(), s_groupPct, s_groupMin);
            } else {
                WriteChatf("[E3Plus] Usage: /e3plus heal group \"Spell Name\" <pct> <minInjured>");
            }
            return;
        }

        WriteChatf("[E3Plus] Unknown heal subcommand. Try: on|off|status|life|single|group");
    }
}

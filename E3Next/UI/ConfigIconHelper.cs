using System;
using System.Collections.Generic;

namespace E3Next.UI
{
    /// <summary>
    /// Helper class for mapping E3Next configuration sections and keys to appropriate Font Awesome icons.
    /// Provides intelligent icon selection based on section names and key patterns.
    /// </summary>
    public static class ConfigIconHelper
    {
        // Small container for key icon pattern
        private class KeyIconPattern
        {
            public Func<string, bool> Pattern;
            public string Icon;
            public KeyIconPattern(Func<string, bool> pattern, string icon)
            {
                Pattern = pattern; Icon = icon;
            }
        }

        private static bool ContainsCI(string text, string value)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(value)) return false;
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Section name to icon mappings
        private static readonly Dictionary<string, string> SectionIcons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Core sections (mix MD and FA)
            { "Misc", FontAwesome.Cogs },
            { "Assist Settings", FontAwesome.CrossHairs },
            { "General", FontAwesome.Cog },
            { "Advanced", FontAwesome.SlidersH },
            { "Settings", FontAwesome.Cog },

            // Specific requests
            // Fallback to ASCII labels if icon fonts are unavailable
            { "AutoMed", "[AM]" },
            { "Display", "[DSP]" },
            { "Life Support", "[HEAL]" },
            { "Rez", "[REZ]" },
            { "Bando Buff", "[SWAP]" },

            // Additional requested mappings
            { "Rampage Actions", "[ADJ]" },
            { "Cursor Delete", "[DEL]" },
            { "Gimme", "[SWAP]" },
            { "Events", "[EVT]" },
            { "EventLoop", "[EVT]" },
            { "EventLoopTiming", "[EVT]" },
            { "EventRegMatches", "[EVT]" },
            { "Report", "[EVT]" },
            { "CPU", "[CPU]" },
            { "Startup", "[EVT]" },
            { "Zoning Commands", "[EVT]" },
            { "E3BotsPublish", "[EVT]" },
            { "E3ChatChannels", "[EVT]" },
            { "Alerts", "[EVT]" },

            // Combat sections
            { "Nukes", FontAwesome.Bolt },
            { "Debuffs", FontAwesome.Minus },
            { "DoTs on Assist", FontAwesome.Clock },
            { "DoTs on Command", FontAwesome.Hourglass },
            { "Heals", FontAwesome.Heart },
            { "Buffs", FontAwesome.Plus },
            { "Melee Abilities", FontAwesome.Sword },
            { "Burn", FontAwesome.Fire },
            { "CommandSets", FontAwesome.List },
            { "Pets", FontAwesome.Paw },
            { "Ifs", FontAwesome.Question },

            // Class-specific sections
            { "Bard", FontAwesome.Music },
            { "Necromancer", FontAwesome.Skull },
            { "Shadowknight", FontAwesome.UserShield },
            { "Paladin", FontAwesome.Shield },
            { "Warrior", FontAwesome.Fist },
            { "Cleric", FontAwesome.Heart },
            { "Druid", FontAwesome.Leaf },
            { "Shaman", FontAwesome.Skull },
            { "Wizard", FontAwesome.Magic },
            { "Magician", FontAwesome.Magic },
            { "Enchanter", FontAwesome.Gem },
            { "Ranger", FontAwesome.Eye },
            { "Rogue", FontAwesome.EyeSlash },
            { "Monk", FontAwesome.Fist },
            { "Berserker", FontAwesome.Fire },
            { "Beastlord", FontAwesome.Paw },

            // Melody sections (Bard)
            { "Melody", FontAwesome.Music },

            // Hunt sections
            { "Hunt", FontAwesome.Search },
            { "Navigation", FontAwesome.Compass },

            // Item sections
            { "Items", FontAwesome.Box },
            { "Inventory", FontAwesome.Archive },
            { "Equipment", FontAwesome.Shield },

            // Zone/Location sections
            { "Zones", FontAwesome.Map },
            { "Locations", FontAwesome.MapMarker },
        };

        // Key patterns to icon mappings (checked in order)
        private static readonly List<KeyIconPattern> KeyPatterns = new List<KeyIconPattern>
        {
            // Status/Toggle keys
            new KeyIconPattern(key => ContainsCI(key, "Enable"), FontAwesome.Check),
            new KeyIconPattern(key => key != null && key.StartsWith("Use ", StringComparison.OrdinalIgnoreCase), FontAwesome.Play),
            new KeyIconPattern(key => ContainsCI(key, "On") && ContainsCI(key, "Off"), FontAwesome.Pause),
            
            // Healing related
            new KeyIconPattern(key => ContainsCI(key, "Heal"), FontAwesome.Heart),
            new KeyIconPattern(key => string.Equals(key, "Tank", StringComparison.OrdinalIgnoreCase), FontAwesome.UserShield),
            new KeyIconPattern(key => ContainsCI(key, "Important"), FontAwesome.Crown),
            new KeyIconPattern(key => ContainsCI(key, "Health"), FontAwesome.Heart),
            new KeyIconPattern(key => ContainsCI(key, "Mana"), FontAwesome.Zap),
            new KeyIconPattern(key => ContainsCI(key, "Endurance"), FontAwesome.Running),
            
            // Buffs and Debuffs
            new KeyIconPattern(key => ContainsCI(key, "Buff"), FontAwesome.ArrowUp),
            new KeyIconPattern(key => ContainsCI(key, "Debuff") || ContainsCI(key, "Debuf"), FontAwesome.ArrowDown),
            new KeyIconPattern(key => ContainsCI(key, "Aura"), FontAwesome.Star),
            new KeyIconPattern(key => ContainsCI(key, "Song"), FontAwesome.Music),
            
            // Combat related
            new KeyIconPattern(key => ContainsCI(key, "Nuke"), FontAwesome.Bolt),
            new KeyIconPattern(key => ContainsCI(key, "DoT") || ContainsCI(key, "Damage over Time"), FontAwesome.Clock),
            new KeyIconPattern(key => ContainsCI(key, "Melee"), FontAwesome.Sword),
            new KeyIconPattern(key => ContainsCI(key, "Spell"), FontAwesome.Magic),
            new KeyIconPattern(key => ContainsCI(key, "Ability"), FontAwesome.Star),
            new KeyIconPattern(key => ContainsCI(key, "Combat"), FontAwesome.CrossHairs),
            new KeyIconPattern(key => ContainsCI(key, "Attack"), FontAwesome.Sword),
            new KeyIconPattern(key => ContainsCI(key, "Defense") || ContainsCI(key, "Defensive"), FontAwesome.Shield),
            new KeyIconPattern(key => ContainsCI(key, "Taunt"), FontAwesome.Bullseye),
            
            // Pet related
            new KeyIconPattern(key => ContainsCI(key, "Pet"), FontAwesome.Paw),
            new KeyIconPattern(key => ContainsCI(key, "Summon"), FontAwesome.Dragon),
            new KeyIconPattern(key => ContainsCI(key, "Minion"), FontAwesome.Spider),
            
            // Food and drink
            new KeyIconPattern(key => string.Equals(key, "Food", StringComparison.OrdinalIgnoreCase), FontAwesome.BirthdayCake),
            new KeyIconPattern(key => string.Equals(key, "Drink", StringComparison.OrdinalIgnoreCase), FontAwesome.Coffee),
            new KeyIconPattern(key => ContainsCI(key, "Potion"), FontAwesome.Flask),
            
            // Positioning and movement
            new KeyIconPattern(key => ContainsCI(key, "Position"), FontAwesome.LocationArrow),
            new KeyIconPattern(key => ContainsCI(key, "Location"), FontAwesome.MapMarker),
            new KeyIconPattern(key => ContainsCI(key, "Distance"), FontAwesome.ArrowsAlt),
            new KeyIconPattern(key => ContainsCI(key, "Range"), FontAwesome.Compass),
            new KeyIconPattern(key => ContainsCI(key, "Radius"), FontAwesome.Bullseye),
            new KeyIconPattern(key => ContainsCI(key, "Camp"), FontAwesome.MapMarker),
            new KeyIconPattern(key => ContainsCI(key, "Move"), FontAwesome.Running),
            new KeyIconPattern(key => ContainsCI(key, "Nav"), FontAwesome.Compass),
            
            // Assist and targeting
            new KeyIconPattern(key => ContainsCI(key, "Assist"), FontAwesome.CrossHairs),
            new KeyIconPattern(key => ContainsCI(key, "Target"), FontAwesome.Bullseye),
            new KeyIconPattern(key => ContainsCI(key, "Hunt"), FontAwesome.Search),
            new KeyIconPattern(key => ContainsCI(key, "Pull"), FontAwesome.Running),
            new KeyIconPattern(key => ContainsCI(key, "Ignore"), FontAwesome.EyeSlash),
            new KeyIconPattern(key => ContainsCI(key, "Watch"), FontAwesome.Eye),
            
            // Timing and delays
            new KeyIconPattern(key => ContainsCI(key, "Delay"), FontAwesome.Clock),
            new KeyIconPattern(key => ContainsCI(key, "Timer"), FontAwesome.Hourglass),
            new KeyIconPattern(key => ContainsCI(key, "Cooldown"), FontAwesome.Refresh),
            new KeyIconPattern(key => ContainsCI(key, "Recast"), FontAwesome.Repeat),
            new KeyIconPattern(key => ContainsCI(key, "Duration"), FontAwesome.Clock),
            new KeyIconPattern(key => ContainsCI(key, "Time"), FontAwesome.Clock),
            
            // Percentages and thresholds
            new KeyIconPattern(key => ContainsCI(key, "Pct") || (key != null && key.IndexOf("%", StringComparison.Ordinal) >= 0), FontAwesome.Balance),
            new KeyIconPattern(key => ContainsCI(key, "Threshold"), FontAwesome.SlidersH),
            new KeyIconPattern(key => ContainsCI(key, "Level"), FontAwesome.Star),
            new KeyIconPattern(key => ContainsCI(key, "Priority"), FontAwesome.Crown),
            
            // Items and equipment
            new KeyIconPattern(key => ContainsCI(key, "Item"), FontAwesome.Box),
            new KeyIconPattern(key => ContainsCI(key, "Equipment"), FontAwesome.Archive),
            new KeyIconPattern(key => ContainsCI(key, "Bag"), FontAwesome.Archive),
            new KeyIconPattern(key => ContainsCI(key, "Inventory"), FontAwesome.Box),
            
            // Conditions and logic
            new KeyIconPattern(key => ContainsCI(key, "If") && !ContainsCI(key, "Buff"), FontAwesome.Question),
            new KeyIconPattern(key => ContainsCI(key, "Condition"), FontAwesome.Question),
            new KeyIconPattern(key => ContainsCI(key, "Check"), FontAwesome.CheckCircle),
            new KeyIconPattern(key => ContainsCI(key, "Verify"), FontAwesome.CheckCircle),
            
            // Communication and chat
            new KeyIconPattern(key => ContainsCI(key, "Tell"), FontAwesome.Microphone),
            new KeyIconPattern(key => ContainsCI(key, "Say"), FontAwesome.VolumeUp),
            new KeyIconPattern(key => ContainsCI(key, "Chat"), FontAwesome.VolumeUp),
            new KeyIconPattern(key => ContainsCI(key, "Report"), FontAwesome.Info),
            
            // Special abilities and disciplines
            new KeyIconPattern(key => ContainsCI(key, "Disc") && !ContainsCI(key, "Discipline"), FontAwesome.Star),
            new KeyIconPattern(key => ContainsCI(key, "Discipline"), FontAwesome.Star),
            new KeyIconPattern(key => ContainsCI(key, "AA"), FontAwesome.Gem),
            new KeyIconPattern(key => ContainsCI(key, "Alt") && ContainsCI(key, "Ability"), FontAwesome.Gem),
            
            // Zone and instance
            new KeyIconPattern(key => ContainsCI(key, "Zone"), FontAwesome.Map),
            new KeyIconPattern(key => ContainsCI(key, "Instance"), FontAwesome.Mountain),
            new KeyIconPattern(key => ContainsCI(key, "Raid"), FontAwesome.Crown),
            new KeyIconPattern(key => ContainsCI(key, "Group"), FontAwesome.UserShield),
            
            // Misc patterns
            new KeyIconPattern(key => ContainsCI(key, "Auto"), FontAwesome.Play),
            new KeyIconPattern(key => ContainsCI(key, "Manual"), FontAwesome.Edit),
            new KeyIconPattern(key => ContainsCI(key, "Random"), FontAwesome.Random),
            new KeyIconPattern(key => ContainsCI(key, "Always"), FontAwesome.Infinity),
            new KeyIconPattern(key => ContainsCI(key, "Never"), FontAwesome.Times),
            new KeyIconPattern(key => ContainsCI(key, "Max"), FontAwesome.ArrowUp),
            new KeyIconPattern(key => ContainsCI(key, "Min"), FontAwesome.ArrowDown),
        };

        /// <summary>
        /// Gets the appropriate Font Awesome icon for a configuration section.
        /// </summary>
        /// <param name="sectionName">The name of the configuration section</param>
        /// <returns>The Font Awesome icon Unicode string, or a default folder icon if no specific mapping is found</returns>
        public static string GetSectionIcon(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                return FontAwesome.Folder;

            // Check for exact section name matches first
            if (SectionIcons.TryGetValue(sectionName, out string icon))
                return icon;

            // Check for partial matches (e.g., "Something Melody" -> Music icon)
            foreach (var kvp in SectionIcons)
            {
                if (ContainsCI(sectionName, kvp.Key))
                    return kvp.Value;
            }

            // Default icon for unknown sections
            return FontAwesome.Folder;
        }

        /// <summary>
        /// Gets the appropriate Font Awesome icon for a configuration key.
        /// </summary>
        /// <param name="keyName">The name of the configuration key</param>
        /// <param name="sectionName">The section this key belongs to (optional, used for context)</param>
        /// <returns>The Font Awesome icon Unicode string, or a default file icon if no specific mapping is found</returns>
        public static string GetKeyIcon(string keyName, string sectionName = null)
        {
            if (string.IsNullOrEmpty(keyName))
                return FontAwesome.File;

            // Check key patterns in order of specificity
            for (int i = 0; i < KeyPatterns.Count; i++)
            {
                var kp = KeyPatterns[i];
                if (kp.Pattern != null && kp.Pattern(keyName))
                    return kp.Icon;
            }

            // If we have section context, try to infer from section type
            if (!string.IsNullOrEmpty(sectionName))
            {
                string sectionIcon = GetSectionIcon(sectionName);
                
                // For some sections, use a related but different icon for keys
                if (sectionIcon == FontAwesome.Heart) // Heals section
                    return FontAwesome.Plus; // Healing action
                if (sectionIcon == FontAwesome.Music) // Bard/Melody section
                    return FontAwesome.VolumeUp; // Song/Sound
                if (sectionIcon == FontAwesome.Paw) // Pet section
                    return FontAwesome.Star; // Pet ability
                if (sectionIcon == FontAwesome.CrossHairs) // Assist section
                    return FontAwesome.Bullseye; // Targeting setting
            }

            // Default icon for unknown keys
            return FontAwesome.File;
        }

        /// <summary>
        /// Determines if a section should be displayed as expanded by default based on its importance.
        /// </summary>
        /// <param name="sectionName">The name of the section</param>
        /// <returns>True if the section should be expanded by default</returns>
        public static bool ShouldExpandByDefault(string sectionName)
        {
            if (string.IsNullOrEmpty(sectionName))
                return false;

            // High-priority sections that should be expanded
            var importantSections = new[]
            {
                "Misc",
                "Assist Settings", 
                "General",
                "Heals"
            };

            return Array.Exists(importantSections, s => 
                string.Equals(s, sectionName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
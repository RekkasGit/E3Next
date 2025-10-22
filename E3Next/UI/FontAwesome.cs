using System;

namespace E3Next.UI
{
    /// <summary>
    /// Font Awesome icon constants for use in E3Next ImGui interface.
    /// Contains Unicode values for Font Awesome icons in solid style.
    /// 
    /// To use these icons in ImGui text rendering:
    /// - Ensure Font Awesome font is loaded in your ImGui context
    /// - Use the constants like: $"{FontAwesome.Heart} Health Settings"
    /// </summary>
    public static class FontAwesome
    {
        // Common interface icons
        public const string Cog = "\uf013";                    // Settings/Configuration
        public const string Cogs = "\uf085";                   // Multiple settings
        public const string Wrench = "\uf0ad";                 // Tools/Utilities
        public const string SlidersH = "\uf1de";               // Controls/Adjustment
        public const string List = "\uf03a";                   // List view
        public const string ListAlt = "\uf022";                // Alternative list
        public const string Bars = "\uf0c9";                   // Menu/Categories
        public const string Folder = "\uf07b";                 // Section/Category
        public const string FolderOpen = "\uf07c";             // Expanded section
        public const string File = "\uf15b";                   // Single item/key
        public const string FileAlt = "\uf15c";                // Alternative file
        
        // Gaming/Combat related (prefer FA Free glyphs)
        public const string Sword = "\uf0fb";                  // Medkit as melee/ability proxy (FA Free)
        public const string Magic = "\uf0d0";                  // Spellcasting
        public const string Dragon = "\uf0d0";                 // Alias to Magic (fallback)
        public const string Bolt = "\uf0e7";                   // Lightning/Energy
        public const string Fire = "\uf06d";                   // Fire spells/damage
        public const string Skull = "\uf54c";                  // Death/Necromancy
        public const string CrossHairs = "\uf05b";             // Targeting/Assist
        public const string Bullseye = "\uf140";               // Precise targeting
        public const string Search = "\uf002";                 // Hunt/Search
        public const string Eye = "\uf06e";                    // Vision/Detection
        public const string EyeSlash = "\uf070";               // Hidden/Ignore
        public const string Ghost = "\uf54c";                  // Alias to Skull (fallback)
        
        // Character/Class related
        public const string Shield = "\uf132";                 // Defense
        public const string UserShield = "\uf132";             // Alias to Shield (fallback)
        public const string Heart = "\uf004";                  // Health/Healing
        public const string Plus = "\uf067";                   // Healing/Buffs
        public const string Minus = "\uf068";                  // Debuffs/Nerfs
        public const string ArrowUp = "\uf062";                // Buffs/Enhancement
        public const string ArrowDown = "\uf063";              // Debuffs/Reduction
        public const string Star = "\uf005";                   // Special abilities
        public const string Fist = "\uf255";                   // Hand-to-hand (Free: hand-rock)
        public const string Crown = "\uf521";                  // Leadership/Important
        public const string Gem = "\uf3a5";                    // Valuable/Rare
        
        // Action/Ability types
        public const string Zap = "\uf0e7";                    // Instant abilities
        public const string Clock = "\uf017";                  // Over-time effects
        public const string Hourglass = "\uf254";              // Duration effects
        public const string Refresh = "\uf021";                // Cooldowns/Recast
        public const string Repeat = "\uf01e";                 // Repeating actions
        public const string Play = "\uf04b";                   // Activation
        public const string Pause = "\uf04c";                  // Deactivation
        public const string Stop = "\uf04d";                   // Stop action
        
        // Pet/Minion related (use general symbols that exist in Free)
        public const string Paw = "\uf1b0";                    // Pet abilities
        public const string Spider = "\uf1b0";                 // Alias to Paw (fallback)
        
        // Music/Bard related (Free)
        public const string Music = "\uf001";                  // Music/Songs
        public const string VolumeUp = "\uf028";               // Loud/Amplified
        public const string Microphone = "\uf130";             // Voice/Singing
        
        // Status/State indicators
        public const string Check = "\uf00c";                  // Enabled/Active
        public const string Times = "\uf00d";                  // Disabled/Off
        public const string CheckCircle = "\uf058";            // Success/Good
        public const string TimesCircle = "\uf057";            // Error/Bad
        public const string ExclamationTriangle = "\uf071";    // Warning
        public const string Info = "\uf129";                   // Information
        public const string Question = "\uf128";               // Unknown/Help
        
        // Movement/Position (Free)
        public const string ArrowsAlt = "\uf0b2";              // Movement/Position
        public const string LocationArrow = "\uf124";          // GPS/Location
        public const string Running = "\uf124";                // Alias to LocationArrow (fallback)
        public const string Compass = "\uf14e";                // Navigation
        public const string Map = "\uf279";                    // Area/Zone (solid may require Pro; fallback acceptable)
        public const string MapMarker = "\uf041";              // Specific location
        
        // Items/Equipment (prefer Free)
        public const string Archive = "\uf187";                // Storage/Bags
        public const string Coffee = "\uf0f4";                 // Drinks/Potions
        public const string Flask = "\uf0c3";                  // Potions/Alchemy
        public const string Box = "\uf187";                    // Use archive as box proxy
        public const string Utensils = "\uf2e7";               // Food/Consumables
        public const string BirthdayCake = "\uf1fd";           // Food/Consumables (cake)
        
        // Interface/UI
        public const string ChevronRight = "\uf054";           // Expand/Next
        public const string ChevronDown = "\uf078";            // Collapse/Down
        public const string CaretRight = "\uf0da";             // Tree expand
        public const string CaretDown = "\uf0d7";              // Tree collapse
        public const string Ellipsis = "\uf141";               // More options
        public const string Edit = "\uf044";                   // Edit/Modify
        public const string Save = "\uf0c7";                   // Save changes
        public const string Trash = "\uf1f8";                  // Delete/Remove
        
        // Miscellaneous (prefer Free)
        public const string Random = "\uf074";                 // Random/Variable
        public const string Atom = "\uf5d2";                   // Science/Magic
        public const string Infinity = "\uf534";               // Unlimited/Endless
        public const string Balance = "\uf24e";                // Balance/Equilibrium
        public const string Sun = "\uf185";                    // Light/Day
        public const string Moon = "\uf186";                   // Dark/Night
        public const string Leaf = "\uf06c";                   // Nature/Druid
        public const string Tree = "\uf1bb";                   // Nature/Growth
        public const string Mountain = "\uf1bb";               // Alias to Tree (fallback)
        public const string Water = "\uf043";                  // Tint (water proxy)
    }
}
# Font Awesome Icon Integration for E3Next GUI

This enhancement adds Font Awesome icons to the left column (Sections & Keys) of the E3Next configuration interface (`/e3imgui`) to improve visual clarity and user experience.

## Features Added

### 1. Font Awesome Icon Constants (`FontAwesome.cs`)
- Comprehensive set of Unicode constants for Font Awesome solid icons
- Gaming, combat, and utility-specific icons appropriate for EverQuest automation
- Easy to extend with additional icons as needed

### 2. Smart Icon Mapping (`ConfigIconHelper.cs`)
- **Section Icons**: Maps configuration sections to appropriate icons
  - `Heals` → ❤️ Heart icon
  - `Nukes` → ⚡ Bolt icon  
  - `Bard` → 🎵 Music icon
  - `Pets` → 🐾 Paw icon
  - And many more...

- **Key Icons**: Intelligently maps configuration keys based on patterns
  - `Tank` → 🛡️ Shield icon
  - `Enable*` → ✅ Check icon
  - `*Heal*` → ❤️ Heart icon
  - `*Buff*` → ⬆️ Arrow up icon
  - Contextual icons based on parent section

- **Smart Expansion**: Important sections auto-expand by default
  - `Misc`, `Assist Settings`, `General`, `Heals`

### 3. Enhanced UI Rendering
- Icons appear alongside section and key names in tree view
- Maintains existing functionality while adding visual enhancement
- Icons help users quickly identify configuration categories

## Implementation Details

### Files Modified
- `UI/E3ImGui.cs`: Updated left column rendering to include icons
- `E3Next.csproj`: Added new files to compilation

### Files Added
- `UI/FontAwesome.cs`: Icon constant definitions
- `UI/ConfigIconHelper.cs`: Icon mapping logic

### Code Changes
```csharp
// Section rendering with icon
string sectionIcon = ConfigIconHelper.GetSectionIcon(sec);
string sectionLabel = $"{sectionIcon} {sec}##section_{sec}";
bool nodeOpen = imgui_TreeNodeEx(sectionLabel, treeFlags);

// Key rendering with icon  
string keyIcon = ConfigIconHelper.GetKeyIcon(key, sec);
string keyLabel = $"  {keyIcon} {key}";
if (imgui_Selectable(keyLabel, keySelected)) { ... }
```

## Prerequisites

For Font Awesome icons to display properly, the ImGui context must have the Font Awesome font loaded. This typically requires:

1. **Font Loading**: Font Awesome .ttf file loaded in ImGui font atlas
2. **Font Selection**: Proper font selection when rendering icon text
3. **Fallback**: If font is not available, icons will display as Unicode placeholders

## Icon Categories

### Combat & Spells
- ⚡ **Bolt**: Nukes, instant damage
- 🔥 **Fire**: Burn abilities, fire spells  
- ❄️ **Snowflake**: Ice/cold spells
- 🕒 **Clock**: DoT spells, timers
- 💀 **Skull**: Death/necromancy spells

### Character Management  
- ❤️ **Heart**: Healing, health management
- ➕ **Plus**: Buffs, beneficial effects
- ➖ **Minus**: Debuffs, harmful effects
- 🛡️ **Shield**: Defense, tanking
- ⚔️ **Sword**: Melee combat, attacks

### Interface & Controls
- ⚙️ **Cog**: Settings, configuration
- 📁 **Folder**: Sections, categories
- 📄 **File**: Individual keys, items
- ✅ **Check**: Enabled options
- ❌ **Times**: Disabled options

## Usage Examples

The system automatically assigns appropriate icons based on naming patterns:

```
🔧 Misc
├── ⚙️ Enable Setting
├── ✅ Auto Something  
└── 📄 Other Key

❤️ Heals  
├── 🛡️ Tank
├── 👑 Important Bot
└── ➕ Group Heal Pct

⚡ Nukes
├── 🔥 Fire Nuke
├── ❄️ Ice Nuke  
└── ⚡ Lightning Bolt

🎵 Bard
├── 🔊 Combat Song
├── 🎵 Travel Song
└── 🔊 Mana Song
```

## Extending the System

### Adding New Icons
Add constants to `FontAwesome.cs`:
```csharp
public const string NewIcon = "\uf123"; // Font Awesome hex code
```

### Adding Section Mappings
Update `SectionIcons` dictionary in `ConfigIconHelper.cs`:
```csharp
{ "New Section", FontAwesome.NewIcon },
```

### Adding Key Patterns
Add patterns to `KeyPatterns` list in `ConfigIconHelper.cs`:
```csharp
(key => key.Contains("NewPattern", StringComparison.OrdinalIgnoreCase), FontAwesome.NewIcon),
```

## Testing Checklist

- [ ] Icons display correctly in left column tree view
- [ ] Section expand/collapse still works
- [ ] Key selection still works  
- [ ] Important sections auto-expand
- [ ] Icon assignments look logical and helpful
- [ ] No performance impact on UI rendering
- [ ] Fallback behavior if Font Awesome not loaded

## Benefits

1. **Visual Clarity**: Quick identification of configuration categories
2. **Better UX**: Reduced cognitive load when navigating settings
3. **Professional Look**: Modern, polished interface appearance
4. **Contextual Cues**: Icons provide hints about setting purposes
5. **Extensible**: Easy to add new icons and mappings

## Future Enhancements

- Add color coding for different icon categories
- Include icon tooltips with additional information
- Support for different Font Awesome styles (regular, brands)
- Dynamic icon updates based on setting states
- User customization of icon assignments
# UI Settings Persistence

## Overview
The E3Next ImGui interface now saves theme and rounding preferences to a persistent INI file. Settings are automatically loaded on startup and saved whenever changes are made.

## UI Settings File Location
The UI settings are saved to:
```
{MacroQuest Config Folder}\e3 Macro Inis\UI Settings.ini
```

Example path:
```
C:\MacroQuest\config\e3 Macro Inis\UI Settings.ini
```

## File Format
```ini
[UI Theme]
Current Theme=DarkTeal
Rounding=8.0
```

## Supported Themes
- **DarkTeal** (Default) - Original E3Next theme with teal accents
- **DarkBlue** - Dark theme with blue accents
- **DarkPurple** - Dark theme with purple accents  
- **DarkOrange** - Dark theme with orange accents
- **DarkGreen** - Dark theme with green accents
- **Pink** - Dark theme with pink accents (Barbie theme)
- **Sunset** - Dark theme with sunset colors (orange/pink blend)

## Rounding Values
- **Range**: 0.0 to 16.0 pixels
- **Default**: 8.0 pixels
- **Precision**: Stored to 1 decimal place

## Implementation Details

### Automatic Loading
- Settings are loaded during ImGui initialization
- Occurs once when `/e3imgui` is first opened
- Fallback to defaults if file doesn't exist or is corrupted

### Automatic Saving
- **Theme changes**: Saved immediately when theme is selected
- **Rounding changes**: Saved immediately when +/- buttons are used
- No manual save required

### Error Handling
- Missing file: Creates default settings file automatically
- Corrupted file: Falls back to default values and logs error
- Invalid theme: Falls back to DarkTeal theme
- Invalid rounding: Clamped to valid range (0.0-16.0)

### File Creation
The file is automatically created with default values:
```csharp
// Default settings on first run
public string UI_Theme = "DarkTeal";
public float UI_Rounding = 8.0f;
```

### Logging
Settings operations are logged:
```
[INFO] Loaded UI Settings: Theme=DarkTeal, Rounding=8.0
[INFO] Saved UI Settings to C:\MacroQuest\config\e3 Macro Inis\UI Settings.ini
[ERROR] Failed to load UI Settings, using defaults: File not found
```

## User Experience

### First Launch
1. User opens `/e3imgui` for the first time
2. Default theme (DarkTeal) and rounding (8.0px) are applied
3. Settings file is created with defaults
4. User can immediately customize appearance

### Subsequent Launches  
1. User opens `/e3imgui`
2. Saved theme and rounding are applied automatically
3. Interface appears exactly as user last configured it
4. Changes persist across game sessions

### Customization
1. User clicks "Theme" button to open Theme Settings
2. Select different theme - saved immediately
3. Adjust rounding with +/- buttons - saved immediately  
4. Settings persist automatically without manual save

## Benefits
- **Persistent preferences**: Settings survive game restarts
- **Immediate feedback**: Changes are saved as soon as they're made
- **No manual save needed**: Automatic persistence
- **Error resilient**: Graceful fallback to defaults
- **Per-character support**: Can have different settings per character set
- **Familiar location**: Uses same folder as other E3 settings

## Technical Notes
- Uses existing E3 settings infrastructure (BaseSettings class)
- Follows same pattern as GeneralSettings.ini and other setting files
- Supports character sets (appends `_{set}` to filename if CurrentSet is specified)
- Thread-safe saving during UI operations
- Minimal performance impact (only saves when changes are made)
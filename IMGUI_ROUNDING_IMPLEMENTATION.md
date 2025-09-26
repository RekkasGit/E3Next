# ImGui Rounding Implementation for E3Next

## Overview
This document describes the implementation of rounded UI elements for the E3Next ImGui interface. The implementation adds customizable rounding to windows, buttons, child windows, popups, scrollbars, and other UI elements.

## Features Added

### 1. Core Functionality
- **Default rounding**: 8.0px for a modern, polished look
- **Customizable rounding**: Adjustable from 0.0px (square) to 16.0px (heavily rounded)
- **Proportional rounding**: Different UI elements use appropriate rounding ratios
- **Theme integration**: Rounding applies to all existing themes

### 2. UI Controls
- **Theme Settings Modal**: New rounding control section
- **Granular adjustment**: +/- 0.5px and +/- 1.0px buttons
- **Reset button**: Quick return to default 8.0px
- **Live preview**: Shows rounded button and child window

### 3. Rounding Ratios
- **Windows**: 100% of base rounding (`_uiRounding`)
- **Child windows**: 75% of base rounding (`_uiRounding * 0.75f`)
- **Frames** (buttons, inputs): 50% of base rounding (`_uiRounding * 0.5f`)
- **Popups/Modals**: 75% of base rounding (`_uiRounding * 0.75f`)
- **Scrollbars**: 100% of base rounding (`_uiRounding`)
- **Grab handles**: 50% of base rounding (`_uiRounding * 0.5f`)
- **Tabs**: 50% of base rounding (`_uiRounding * 0.5f`)

## Files Modified

### Core.cs
- Added `ImGuiStyleVar` enum with all style variable constants
- Added ImGui style variable function declarations:
  - `imgui_PushStyleVarFloat(int styleVar, float value)`
  - `imgui_PushStyleVarVec2(int styleVar, float x, float y)`
  - `imgui_PopStyleVar(int count)`
  - `imgui_GetStyleVarFloat(int styleVar)`
  - `imgui_GetStyleVarVec2(int styleVar, out float x, out float y)`

### E3ImGui.cs
- Added rounding settings variables
- Added `PushRoundedStyle()` and `PopRoundedStyle()` helper functions
- Modified `PushCurrentTheme()` to include rounding
- Modified `PopCurrentTheme()` to clean up rounding styles
- Enhanced theme settings modal with rounding controls and preview

## Required MQ2Mono Implementation

For this functionality to work, MQ2Mono must implement the following native functions:

```cpp
// Push a float style variable (for rounding values)
void imgui_PushStyleVarFloat(int styleVar, float value);

// Push a Vec2 style variable (for padding, spacing, etc.)
void imgui_PushStyleVarVec2(int styleVar, float x, float y);

// Pop one or more style variables
void imgui_PopStyleVar(int count);

// Optional: Get current style variable values
float imgui_GetStyleVarFloat(int styleVar);
void imgui_GetStyleVarVec2(int styleVar, float* x, float* y);
```

## Usage

### For End Users
1. Open `/e3imgui` command to show the E3Next Config window
2. Click the "Theme" button to open Theme Settings
3. Use the rounding controls to adjust UI roundness:
   - `--` and `++` buttons: Adjust by 1.0px
   - `-` and `+` buttons: Adjust by 0.5px
   - `Reset` button: Return to default 8.0px
4. See live preview of the rounding effect
5. Close the Theme Settings modal to apply changes

### For Developers
The rounding system is automatically applied to all ImGui elements when themes are pushed. The system uses a proportional approach where different UI elements get appropriate rounding amounts relative to the base setting.

## Benefits
- **Modern appearance**: Rounded corners provide a contemporary, polished look
- **Consistency**: All UI elements follow the same rounding scheme
- **Customization**: Users can adjust rounding to their preference
- **Performance**: Minimal overhead using ImGui's built-in style system
- **Integration**: Works seamlessly with existing theme system

## Testing
Once MQ2Mono implements the required functions:
1. Load E3Next and open `/e3imgui`
2. Try different rounding values (0, 4, 8, 12, 16)
3. Test with all available themes
4. Verify that all UI elements show appropriate rounding
5. Check that the preview in Theme Settings works correctly

## Future Enhancements
- Save/load rounding preferences to INI files
- Per-theme rounding settings
- Additional style variable controls (padding, spacing, etc.)
- Slider control when MQ2Mono implements ImGui sliders
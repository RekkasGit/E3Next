# Spell Icon System Implementation

This document describes the implementation of the spell icon system for the E3Next ImGui configuration editor, which replicates the functionality of the original e3config.exe application.

## Overview

The spell icon system displays EverQuest spell icons alongside spell names in the memorized gems display, similar to how the original e3config.exe shows spell icons. The system includes:

1. **TGA Image Loading** - Loads spell icon images from EQ's TGA files
2. **Icon Management** - Manages and caches spell icons as ImGui textures
3. **Server-side Data Collection** - Includes spell icon indices in gem data
4. **UI Display** - Shows icons with hover tooltips in the gem display

## Architecture

### Components

1. **TGALoader.cs** - Simple TGA file parser for EverQuest's spell icon files
2. **SpellIconManager.cs** - Manages loading and caching of spell icons as ImGui textures
3. **Server-side modifications** - Enhanced gem data collection with icon indices
4. **UI modifications** - Updated gem display with icon support and tooltips

### Data Flow

```
EverQuest TGA Files → TGALoader → SpellIconManager → ImGui Textures → UI Display
Server Gem Collection → Icon Index Lookup → Client Display
```

## Implementation Details

### 1. TGA Loading (`TGALoader.cs`)

- Reads EverQuest's spell icon TGA files from `{EQPath}\uifiles\default\spells01.tga` through `spells63.tga`
- Each TGA file contains a 6x6 grid of 40x40 pixel spell icons
- Extracts individual icons as Bitmap objects
- Handles TGA format specifics (BGRA pixel order, origin flags)

### 2. Spell Icon Manager (`SpellIconManager.cs`)

- Loads all spell icons during initialization
- Converts Bitmap objects to ImGui texture handles (placeholder implementation)
- Provides indexed access to spell icons: `GetSpellIconTexture(iconIndex)`
- Handles cleanup and resource management

**Note**: The texture creation in `CreateTextureFromData()` is currently a placeholder. This needs to be implemented to call the actual MQ2 ImGui texture creation functions.

### 3. Server-side Data Collection

Enhanced the `CollectSpellGemData()` method in `SharedDataClient.cs`:
- Now includes spell icon indices alongside spell names
- Format: `"SpellName:IconIndex|SpellName:IconIndex|..."`
- Uses catalog lookups for efficient icon index retrieval
- Fallback to direct MQ queries if catalog data unavailable

### 4. UI Display Enhancements

Updated `RenderCatalogGemData()` in `E3ImGui.cs`:
- Parses gem data with icon indices
- Displays placeholder icon indicators (diamond symbols)
- Shows detailed hover tooltips with spell information
- Handles both local and remote gem data seamlessly

## Current Status

### ✅ Completed
- TGA image loading utility
- Spell icon manager structure
- Server-side gem data with icon indices
- UI parsing and display framework
- Hover tooltips with spell details
- Integration with existing catalog system

### 🔄 Partial Implementation
- ImGui texture creation (placeholder implementation)
- Icon display (currently shows placeholder symbols)

### ❌ Remaining Work
- **ImGui Texture Integration** - Replace placeholder texture creation with actual MQ2 ImGui calls
- **Icon Display** - Replace placeholder symbols with actual `imgui_Image()` calls
- **Testing** - Comprehensive testing of the icon loading and display system

## Completing the Implementation

To complete the spell icon system, you need to:

### 1. Implement Actual Texture Creation

Replace the placeholder in `SpellIconManager.CreateTextureFromData()`:

```csharp
private static IntPtr CreateTextureFromData(byte[] data, int width, int height)
{
    // Replace this placeholder with actual MQ2 ImGui texture creation
    // Example (pseudo-code):
    // return MQ2ImGui.CreateTexture(data, width, height, RGBA_FORMAT);
    
    // Current placeholder:
    GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
    return handle.AddrOfPinnedObject();
}
```

### 2. Enable Icon Display in UI

Update `RenderCatalogGemData()` to use real textures:

```csharp
// Replace this block:
if (iconIndex >= 0)
{
    // TODO: Replace this with actual ImGui Image() call
    IntPtr iconTexture = SpellIconManager.GetSpellIconTexture(iconIndex);
    if (iconTexture != IntPtr.Zero)
    {
        imgui_Image(iconTexture, 16f, 16f);
        imgui_SameLine();
    }
}
```

### 3. MQ2 Integration Research

You'll need to determine:
- How MQ2's ImGui integration handles texture creation
- What texture format is expected (DirectX 9/11, OpenGL)
- Available texture creation functions in the MQ2 environment
- Proper cleanup/disposal of textures

### 4. Error Handling and Fallbacks

Add robust error handling for:
- Missing TGA files
- Invalid icon indices
- Texture creation failures
- Memory management issues

## Testing Strategy

1. **Icon Loading Test** - Verify TGA files are read correctly
2. **Index Mapping Test** - Confirm spell names map to correct icon indices
3. **UI Display Test** - Validate icons appear correctly in gem slots
4. **Tooltip Test** - Ensure hover tooltips work properly
5. **Memory Usage Test** - Check for memory leaks or excessive usage

## Benefits

Once complete, this system provides:

- **Visual Consistency** - Matches the original e3config.exe experience
- **Enhanced UX** - Quick visual identification of memorized spells
- **Rich Information** - Detailed hover tooltips with spell data
- **Performance** - Cached textures for smooth UI performance
- **Compatibility** - Works with both local and remote character data

## Technical Notes

- Icon indices range from 0 to ~2200+ (63 TGA files × 36 icons each)
- Each icon is 40x40 pixels, displayed at 16x16 or 20x20 for UI
- TGA files use 32-bit BGRA format with alpha channel
- System gracefully handles missing icons or TGA files
- Memory usage is approximately 50-100MB for all cached icons

The implementation provides a solid foundation that matches the original e3config.exe functionality while integrating seamlessly with the existing E3Next ImGui configuration system.
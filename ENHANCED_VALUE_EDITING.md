# Enhanced Value Editing with Gem Selection

## Overview
This update completely revamps the configuration value editing system in E3Next's `/e3imgui` interface. The old Edit/Delete button system has been replaced with inline text boxes and gem slot dropdowns for a much more intuitive editing experience.

## Key Features

### 1. Inline Editing
- **Direct text input**: Each configuration value now has its own text box for immediate editing
- **No modal dialogs**: Edit values directly in the main interface
- **Real-time updates**: Changes are applied with the "Update" button

### 2. Gem Slot Selection
- **Smart gem dropdowns**: Available for spell-related values (not items)
- **12 gem slots supported**: Gem 1 through Gem 12 plus "No Gem" option
- **Automatic item detection**: Food, drink, and other items don't show gem selection
- **Format**: Values are saved as `"SpellName/Gem|#"` format

### 3. Intelligent Value Detection
The system automatically determines if a value should have gem selection based on:
- **Section names**: Items, Inventory, Equipment, Misc sections
- **Key names**: Food, Drink, Item, Ammo, Arrow, Thrown keys
- **Value content**: Potion, elixir, bread, meat, fish, etc.

### 4. Enhanced User Experience
- **Visual clarity**: Each row shows row number, text box, gem dropdown (if applicable), and icon buttons
- **Compact buttons**: Font Awesome icons reduce visual clutter
- **Tooltips**: Hover over icons to see their function
- **Smart display**: Shows gem assignment in the selectable value display
- **Fallback support**: Text alternatives if Font Awesome isn't available

## Interface Layout

Each configuration value row now shows:
```
1. [Text Input Box          ] [Gem Dropdown] [📝] [🗑️] Spell Name (Gem 3)
2. [Text Input Box          ]               [📝] [🗑️] Bread of the Wild
3. [Text Input Box          ] [Gem Dropdown] [📝] [🗑️] Heal (No Gem)
```

**Icon Buttons:**
- **📝 (Pencil)**: Update/Save the value with current text and gem selection
- **🗑️ (Trash)**: Delete the value from the configuration
- **➕ (Plus)**: Add new value (in the add section)

## File Changes

### E3ImGui.cs
- **Added helper variables**:
  - `_cfgValueEditBuffers`: Stores text input state for each value
  - `_cfgValueGemSelection`: Stores gem selection for each value
  - `_cfgLastSelectionKey`: Tracks section/key changes for buffer cleanup

- **Added helper functions**:
  - `ParseValueWithGem()`: Parses "SpellName/Gem|#" format
  - `FormatValueWithGem()`: Creates "SpellName/Gem|#" format
  - `IsItemValue()`: Determines if value should have gem selection

- **Completely rewrote**:
  - `RenderSelectedKeyValues()`: Now uses inline editing system
  - Value list rendering: Each value has its own text box and gem dropdown
  - Add new value section: Unified with inline editing approach

## Usage Examples

### For Spells/Abilities
- **Input**: `"Greater Heal"`
- **Gem Selection**: `"Gem 5"`
- **Saved As**: `"Greater Heal/Gem|5"`

### For Items  
- **Input**: `"Bread of the Wild"`
- **Gem Selection**: Not shown (auto-detected as item)
- **Saved As**: `"Bread of the Wild"`

### No Gem Assignment
- **Input**: `"Gate"`
- **Gem Selection**: `"No Gem"`  
- **Saved As**: `"Gate"`

## Technical Implementation

### Value Parsing
```csharp
ParseValueWithGem("Greater Heal/Gem|5", out string name, out int gem);
// name = "Greater Heal", gem = 5

FormatValueWithGem("Greater Heal", 5);
// Returns: "Greater Heal/Gem|5"
```

### Smart Item Detection
The system checks multiple criteria:
- Section name contains "Items", "Inventory", "Equipment", "Misc"
- Key name contains "Food", "Drink", "Item", "Ammo", "Arrow", "Thrown"
- Value name contains "potion", "elixir", "bread", "meat", etc.

### Buffer Management
- Buffers are automatically cleared when switching sections/keys
- Each value has a unique buffer key: `"{section}_{key}_{index}"`
- Add new value uses: `"add_new_{section}_{key}"`

### Font Awesome Icons
The interface uses Font Awesome icons with fallback text alternatives:
```csharp
// Font Awesome Unicode characters
private const string ICON_FA_PENCIL = "\uf040";  // Pencil (edit)
private const string ICON_FA_TRASH = "\uf1f8";   // Trash can (delete) 
private const string ICON_FA_PLUS = "\uf067";    // Plus (add)

// Fallback system
string updateIcon = GetIconOrFallback(ICON_FA_PENCIL, "Ed");
string deleteIcon = GetIconOrFallback(ICON_FA_TRASH, "X");
string addIcon = GetIconOrFallback(ICON_FA_PLUS, "+");
```

## Benefits

### For Users
- **Faster editing**: No more modal dialogs or separate edit modes
- **Gem assignment**: Easy gem slot selection with visual feedback
- **Clear display**: See gem assignments at a glance
- **Item-aware**: System knows when gem selection doesn't apply

### For Developers  
- **Maintainable**: Clear separation of parsing/formatting logic
- **Extensible**: Easy to add more intelligent detection rules
- **Consistent**: All values use the same editing paradigm

## Migration
- **Existing configurations**: Fully compatible with current format
- **New gem syntax**: Only applied when gem is selected
- **Backward compatibility**: Old format still works perfectly

## MQ2Mono Font Awesome Support

For optimal display, MQ2Mono should load the Font Awesome font. The system includes fallbacks:

**With Font Awesome:**
- 📝 Pencil icon for update
- 🗑️ Trash icon for delete  
- ➕ Plus icon for add

**Without Font Awesome (fallback):**
- "Ed" text for update
- "X" text for delete
- "+" text for add

**Implementation in MQ2Mono:**
```cpp
// Load Font Awesome font file in ImGui initialization
ImGuiIO& io = ImGui::GetIO();
io.Fonts->AddFontFromFileTTF("FontAwesome.ttf", 16.0f);
```

## Future Enhancements
- **Gem validation**: Could check if gem slot is actually available
- **Spell validation**: Could validate spell names against catalogs
- **Macro integration**: Could integrate with macro gem management
- **Hotkey assignment**: Could add quick gem assignment hotkeys
- **More icons**: Additional Font Awesome icons for other UI elements

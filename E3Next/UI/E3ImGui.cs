using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Reflection;
using IniParser.Model;
using E3Core.Processors;
using NetMQ;
using NetMQ.Sockets;
using Google.Protobuf;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace MonoCore
{
    // /e3imgui UI extracted into dedicated partial class file
    public static partial class Core
    {
        private sealed class ThemeDefinition
        {
            public string Name { get; }
            public Dictionary<ImGuiCol, float[]> Colors { get; }

            public ThemeDefinition(string name, IDictionary<ImGuiCol, float[]> colors)
            {
                Name = name;
                Colors = new Dictionary<ImGuiCol, float[]>(colors.Count);
                foreach (var kvp in colors)
                {
                    Colors[kvp.Key] = (float[])kvp.Value.Clone();
                }
            }
        }

        private readonly struct ThemeColor
        {
            public ImGuiCol Color { get; }
            public float R { get; }
            public float G { get; }
            public float B { get; }
            public float A { get; }

            public ThemeColor(ImGuiCol color, float r, float g, float b, float a)
            {
                Color = color;
                R = r;
                G = g;
                B = b;
                A = a;
            }
        }

        private readonly struct ThemeColorEditorEntry
        {
            public ImGuiCol Color { get; }
            public string Label { get; }

            public ThemeColorEditorEntry(ImGuiCol color, string label)
            {
                Color = color;
                Label = label;
            }
        }

        private static readonly ImGuiCol[] _themeColorOrder = new[]
        {
            ImGuiCol.WindowBg,
            ImGuiCol.ChildBg,
            ImGuiCol.FrameBg,
            ImGuiCol.FrameBgHovered,
            ImGuiCol.FrameBgActive,
            ImGuiCol.Button,
            ImGuiCol.ButtonHovered,
            ImGuiCol.ButtonActive,
            ImGuiCol.Header,
            ImGuiCol.HeaderHovered,
            ImGuiCol.HeaderActive,
            ImGuiCol.Tab,
            ImGuiCol.TabHovered,
            ImGuiCol.TabActive,
            ImGuiCol.TabUnfocused,
            ImGuiCol.TabUnfocusedActive,
            ImGuiCol.SliderGrab,
            ImGuiCol.SliderGrabActive,
            ImGuiCol.CheckMark,
            ImGuiCol.TitleBg,
            ImGuiCol.TitleBgActive,
            ImGuiCol.Separator,
            ImGuiCol.SeparatorHovered,
            ImGuiCol.SeparatorActive,
            ImGuiCol.ScrollbarGrab,
            ImGuiCol.ScrollbarGrabHovered,
            ImGuiCol.ScrollbarGrabActive
        };

        private static readonly ThemeColor[] _themeBaseColors = new[]
        {
            new ThemeColor(ImGuiCol.WindowBg, 0.13f, 0.13f, 0.14f, 1.0f),
            new ThemeColor(ImGuiCol.ChildBg, 0.11f, 0.11f, 0.12f, 1.0f),
            new ThemeColor(ImGuiCol.FrameBg, 0.17f, 0.18f, 0.20f, 1.0f),
            new ThemeColor(ImGuiCol.FrameBgHovered, 0.20f, 0.21f, 0.23f, 1.0f),
            new ThemeColor(ImGuiCol.FrameBgActive, 0.19f, 0.20f, 0.22f, 1.0f),
            new ThemeColor(ImGuiCol.Button, 0.13f, 0.55f, 0.53f, 1.0f),
            new ThemeColor(ImGuiCol.ButtonHovered, 0.17f, 0.66f, 0.64f, 1.0f),
            new ThemeColor(ImGuiCol.ButtonActive, 0.12f, 0.48f, 0.47f, 1.0f),
            new ThemeColor(ImGuiCol.Header, 0.12f, 0.50f, 0.49f, 0.55f),
            new ThemeColor(ImGuiCol.HeaderHovered, 0.16f, 0.62f, 0.60f, 0.80f),
            new ThemeColor(ImGuiCol.HeaderActive, 0.12f, 0.50f, 0.49f, 1.00f),
            new ThemeColor(ImGuiCol.Tab, 0.11f, 0.48f, 0.46f, 1.0f),
            new ThemeColor(ImGuiCol.TabHovered, 0.16f, 0.62f, 0.60f, 1.0f),
            new ThemeColor(ImGuiCol.TabActive, 0.13f, 0.55f, 0.53f, 1.0f),
            new ThemeColor(ImGuiCol.TabUnfocused, 0.09f, 0.09f, 0.10f, 1.0f),
            new ThemeColor(ImGuiCol.TabUnfocusedActive, 0.11f, 0.11f, 0.12f, 1.0f),
            new ThemeColor(ImGuiCol.SliderGrab, 0.29f, 0.79f, 0.76f, 1.0f),
            new ThemeColor(ImGuiCol.SliderGrabActive, 0.36f, 0.86f, 0.80f, 1.0f),
            new ThemeColor(ImGuiCol.CheckMark, 0.36f, 0.86f, 0.80f, 1.0f),
            new ThemeColor(ImGuiCol.TitleBg, 0.10f, 0.10f, 0.11f, 1.0f),
            new ThemeColor(ImGuiCol.TitleBgActive, 0.12f, 0.12f, 0.14f, 1.0f),
            new ThemeColor(ImGuiCol.Separator, 0.25f, 0.27f, 0.30f, 1.0f),
            new ThemeColor(ImGuiCol.SeparatorHovered, 0.30f, 0.33f, 0.36f, 1.0f),
            new ThemeColor(ImGuiCol.SeparatorActive, 0.21f, 0.60f, 0.60f, 1.0f),
            new ThemeColor(ImGuiCol.ScrollbarGrab, 0.28f, 0.30f, 0.32f, 1.0f),
            new ThemeColor(ImGuiCol.ScrollbarGrabHovered, 0.32f, 0.34f, 0.36f, 1.0f),
            new ThemeColor(ImGuiCol.ScrollbarGrabActive, 0.36f, 0.38f, 0.40f, 1.0f)
        };

        private static readonly Dictionary<ImGuiCol, float[]> _themeDefaultColorMap = BuildColorMap(_themeBaseColors);

        private static readonly ThemeDefinition[] _themePresets = new[]
        {
            CreateTheme("E3 Dark Teal"),
            CreateTheme("Midnight Violet",
                new ThemeColor(ImGuiCol.WindowBg, 0.12f, 0.10f, 0.16f, 1.0f),
                new ThemeColor(ImGuiCol.ChildBg, 0.10f, 0.08f, 0.14f, 1.0f),
                new ThemeColor(ImGuiCol.Button, 0.44f, 0.25f, 0.66f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonHovered, 0.52f, 0.30f, 0.76f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonActive, 0.38f, 0.19f, 0.60f, 1.0f),
                new ThemeColor(ImGuiCol.Header, 0.42f, 0.29f, 0.64f, 0.55f),
                new ThemeColor(ImGuiCol.HeaderHovered, 0.52f, 0.34f, 0.78f, 0.80f),
                new ThemeColor(ImGuiCol.HeaderActive, 0.52f, 0.34f, 0.78f, 1.00f),
                new ThemeColor(ImGuiCol.Tab, 0.28f, 0.18f, 0.46f, 1.0f),
                new ThemeColor(ImGuiCol.TabHovered, 0.50f, 0.32f, 0.74f, 1.0f),
                new ThemeColor(ImGuiCol.TabActive, 0.42f, 0.27f, 0.66f, 1.0f),
                new ThemeColor(ImGuiCol.CheckMark, 0.86f, 0.72f, 0.96f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrab, 0.70f, 0.44f, 0.94f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrabActive, 0.83f, 0.53f, 1.00f, 1.0f),
                new ThemeColor(ImGuiCol.SeparatorActive, 0.62f, 0.44f, 0.95f, 1.0f)),
            CreateTheme("Gunmetal",
                new ThemeColor(ImGuiCol.WindowBg, 0.15f, 0.15f, 0.16f, 1.0f),
                new ThemeColor(ImGuiCol.ChildBg, 0.13f, 0.13f, 0.14f, 1.0f),
                new ThemeColor(ImGuiCol.Button, 0.53f, 0.58f, 0.63f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonHovered, 0.63f, 0.68f, 0.73f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonActive, 0.44f, 0.49f, 0.54f, 1.0f),
                new ThemeColor(ImGuiCol.Header, 0.37f, 0.43f, 0.49f, 0.65f),
                new ThemeColor(ImGuiCol.HeaderHovered, 0.47f, 0.53f, 0.59f, 0.85f),
                new ThemeColor(ImGuiCol.HeaderActive, 0.53f, 0.58f, 0.64f, 1.00f),
                new ThemeColor(ImGuiCol.Tab, 0.30f, 0.34f, 0.39f, 1.0f),
                new ThemeColor(ImGuiCol.TabHovered, 0.46f, 0.51f, 0.57f, 1.0f),
                new ThemeColor(ImGuiCol.TabActive, 0.40f, 0.45f, 0.50f, 1.0f),
                new ThemeColor(ImGuiCol.CheckMark, 0.88f, 0.91f, 0.95f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrab, 0.69f, 0.74f, 0.78f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrabActive, 0.80f, 0.84f, 0.88f, 1.0f),
                new ThemeColor(ImGuiCol.SeparatorActive, 0.53f, 0.60f, 0.68f, 1.0f)),
            CreateTheme("Sunset Ember",
                new ThemeColor(ImGuiCol.WindowBg, 0.14f, 0.10f, 0.08f, 1.0f),
                new ThemeColor(ImGuiCol.ChildBg, 0.12f, 0.09f, 0.07f, 1.0f),
                new ThemeColor(ImGuiCol.Button, 0.78f, 0.35f, 0.18f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonHovered, 0.87f, 0.42f, 0.23f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonActive, 0.67f, 0.30f, 0.15f, 1.0f),
                new ThemeColor(ImGuiCol.Header, 0.77f, 0.37f, 0.20f, 0.60f),
                new ThemeColor(ImGuiCol.HeaderHovered, 0.90f, 0.46f, 0.26f, 0.90f),
                new ThemeColor(ImGuiCol.HeaderActive, 0.92f, 0.50f, 0.28f, 1.00f),
                new ThemeColor(ImGuiCol.Tab, 0.45f, 0.22f, 0.16f, 1.0f),
                new ThemeColor(ImGuiCol.TabHovered, 0.70f, 0.36f, 0.22f, 1.0f),
                new ThemeColor(ImGuiCol.TabActive, 0.60f, 0.30f, 0.19f, 1.0f),
                new ThemeColor(ImGuiCol.CheckMark, 0.97f, 0.71f, 0.48f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrab, 0.95f, 0.62f, 0.37f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrabActive, 0.99f, 0.73f, 0.43f, 1.0f),
                new ThemeColor(ImGuiCol.SeparatorActive, 0.84f, 0.42f, 0.23f, 1.0f)),
            CreateTheme("Barbie Pink",
                new ThemeColor(ImGuiCol.WindowBg, 0.18f, 0.12f, 0.16f, 1.0f),
                new ThemeColor(ImGuiCol.ChildBg, 0.15f, 0.10f, 0.14f, 1.0f),
                new ThemeColor(ImGuiCol.FrameBg, 0.25f, 0.15f, 0.22f, 1.0f),
                new ThemeColor(ImGuiCol.FrameBgHovered, 0.30f, 0.18f, 0.26f, 1.0f),
                new ThemeColor(ImGuiCol.FrameBgActive, 0.28f, 0.16f, 0.24f, 1.0f),
                new ThemeColor(ImGuiCol.Button, 0.90f, 0.30f, 0.70f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonHovered, 0.95f, 0.40f, 0.78f, 1.0f),
                new ThemeColor(ImGuiCol.ButtonActive, 0.85f, 0.25f, 0.65f, 1.0f),
                new ThemeColor(ImGuiCol.Header, 0.88f, 0.35f, 0.75f, 0.65f),
                new ThemeColor(ImGuiCol.HeaderHovered, 0.92f, 0.45f, 0.82f, 0.85f),
                new ThemeColor(ImGuiCol.HeaderActive, 0.95f, 0.50f, 0.85f, 1.00f),
                new ThemeColor(ImGuiCol.Tab, 0.70f, 0.25f, 0.58f, 1.0f),
                new ThemeColor(ImGuiCol.TabHovered, 0.88f, 0.38f, 0.75f, 1.0f),
                new ThemeColor(ImGuiCol.TabActive, 0.85f, 0.32f, 0.70f, 1.0f),
                new ThemeColor(ImGuiCol.TabUnfocused, 0.40f, 0.15f, 0.30f, 1.0f),
                new ThemeColor(ImGuiCol.TabUnfocusedActive, 0.50f, 0.18f, 0.38f, 1.0f),
                new ThemeColor(ImGuiCol.CheckMark, 1.00f, 0.70f, 0.95f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrab, 0.95f, 0.50f, 0.85f, 1.0f),
                new ThemeColor(ImGuiCol.SliderGrabActive, 1.00f, 0.60f, 0.92f, 1.0f),
                new ThemeColor(ImGuiCol.TitleBg, 0.22f, 0.12f, 0.18f, 1.0f),
                new ThemeColor(ImGuiCol.TitleBgActive, 0.28f, 0.15f, 0.22f, 1.0f),
                new ThemeColor(ImGuiCol.Separator, 0.55f, 0.25f, 0.45f, 1.0f),
                new ThemeColor(ImGuiCol.SeparatorHovered, 0.65f, 0.35f, 0.55f, 1.0f),
                new ThemeColor(ImGuiCol.SeparatorActive, 0.85f, 0.45f, 0.75f, 1.0f),
                new ThemeColor(ImGuiCol.ScrollbarGrab, 0.60f, 0.25f, 0.50f, 1.0f),
                new ThemeColor(ImGuiCol.ScrollbarGrabHovered, 0.70f, 0.35f, 0.60f, 1.0f),
                new ThemeColor(ImGuiCol.ScrollbarGrabActive, 0.80f, 0.45f, 0.70f, 1.0f))
        };

        private static readonly Dictionary<ImGuiCol, float[]> _activeThemeColors = new Dictionary<ImGuiCol, float[]>(_themeColorOrder.Length);
        private static ThemeDefinition _currentThemePreset = null;
        private static bool _themeInitialized = false;
        private static bool _themeDirty = false;
        private static bool _themeWindowOpen = false;
        private static bool _showDonateModal = false;
        private static readonly string _themeWindowTitle = "E3 Theme Options";

        private static readonly (string Header, ThemeColorEditorEntry[] Entries)[] _themeEditorSections = new[]
        {
            ("Backgrounds", new[]
            {
                new ThemeColorEditorEntry(ImGuiCol.WindowBg, "Window Background"),
                new ThemeColorEditorEntry(ImGuiCol.ChildBg, "Child Background"),
                new ThemeColorEditorEntry(ImGuiCol.TitleBg, "Title Background"),
                new ThemeColorEditorEntry(ImGuiCol.TitleBgActive, "Title Active Background")
            }),
            ("Frames & Inputs", new[]
            {
                new ThemeColorEditorEntry(ImGuiCol.FrameBg, "Frame Background"),
                new ThemeColorEditorEntry(ImGuiCol.FrameBgHovered, "Frame Hovered"),
                new ThemeColorEditorEntry(ImGuiCol.FrameBgActive, "Frame Active"),
                new ThemeColorEditorEntry(ImGuiCol.SliderGrab, "Slider Grab"),
                new ThemeColorEditorEntry(ImGuiCol.SliderGrabActive, "Slider Grab Active"),
                new ThemeColorEditorEntry(ImGuiCol.CheckMark, "Check Mark")
            }),
            ("Buttons & Tabs", new[]
            {
                new ThemeColorEditorEntry(ImGuiCol.Button, "Button"),
                new ThemeColorEditorEntry(ImGuiCol.ButtonHovered, "Button Hovered"),
                new ThemeColorEditorEntry(ImGuiCol.ButtonActive, "Button Active"),
                new ThemeColorEditorEntry(ImGuiCol.Tab, "Tab"),
                new ThemeColorEditorEntry(ImGuiCol.TabHovered, "Tab Hovered"),
                new ThemeColorEditorEntry(ImGuiCol.TabActive, "Tab Active"),
                new ThemeColorEditorEntry(ImGuiCol.TabUnfocused, "Tab Unfocused"),
                new ThemeColorEditorEntry(ImGuiCol.TabUnfocusedActive, "Tab Unfocused Active")
            }),
            ("Headers & Separators", new[]
            {
                new ThemeColorEditorEntry(ImGuiCol.Header, "Header"),
                new ThemeColorEditorEntry(ImGuiCol.HeaderHovered, "Header Hovered"),
                new ThemeColorEditorEntry(ImGuiCol.HeaderActive, "Header Active"),
                new ThemeColorEditorEntry(ImGuiCol.Separator, "Separator"),
                new ThemeColorEditorEntry(ImGuiCol.SeparatorHovered, "Separator Hovered"),
                new ThemeColorEditorEntry(ImGuiCol.SeparatorActive, "Separator Active")
            }),
            ("Scrollbars", new[]
            {
                new ThemeColorEditorEntry(ImGuiCol.ScrollbarGrab, "Scrollbar"),
                new ThemeColorEditorEntry(ImGuiCol.ScrollbarGrabHovered, "Scrollbar Hovered"),
                new ThemeColorEditorEntry(ImGuiCol.ScrollbarGrabActive, "Scrollbar Active")
            })
        };

        private static Dictionary<ImGuiCol, float[]> BuildColorMap(IEnumerable<ThemeColor> colors)
        {
            var dict = new Dictionary<ImGuiCol, float[]>();
            foreach (var color in colors)
            {
                dict[color.Color] = new[] { color.R, color.G, color.B, color.A };
            }
            return dict;
        }

        private static ThemeDefinition CreateTheme(string name, params ThemeColor[] overrides)
        {
            var map = new Dictionary<ImGuiCol, float[]>(_themeColorOrder.Length);
            foreach (var col in _themeColorOrder)
            {
                var baseColor = _themeDefaultColorMap[col];
                map[col] = (float[])baseColor.Clone();
            }

            foreach (var oc in overrides)
            {
                map[oc.Color] = new[] { oc.R, oc.G, oc.B, oc.A };
            }

            return new ThemeDefinition(name, map);
        }

        private static void EnsureThemeInitialized()
        {
            if (_themeInitialized)
            {
                return;
            }

            // Try to load the saved theme preference
            string savedThemeName = E3.CharacterSettings.Misc_UITheme;
            _currentThemePreset = _themePresets.FirstOrDefault(t => t.Name.Equals(savedThemeName, StringComparison.OrdinalIgnoreCase)) ?? _themePresets[0];
            ApplyThemePreset(_currentThemePreset);
            _themeInitialized = true;
        }

        private static void ApplyThemePreset(ThemeDefinition preset)
        {
            _activeThemeColors.Clear();
            foreach (var col in _themeColorOrder)
            {
                if (!preset.Colors.TryGetValue(col, out var values))
                {
                    values = _themeDefaultColorMap[col];
                }

                _activeThemeColors[col] = (float[])values.Clone();
            }

            _currentThemePreset = preset;
            _themeDirty = false;
        }

        private static void PushE3Theme()
        {
            EnsureThemeInitialized();
            foreach (var col in _themeColorOrder)
            {
                var values = _activeThemeColors[col];
                imgui_PushStyleColor((int)col, values[0], values[1], values[2], values[3]);
            }
        }
        private static void PopE3Theme()
        {
            imgui_PopStyleColor(_themeColorOrder.Length);
        }

        private static bool RenderThemeColorEditor(ThemeColorEditorEntry entry)
        {
            if (!_activeThemeColors.TryGetValue(entry.Color, out var values))
            {
                return false;
            }

            bool changed = false;
            imgui_Text(entry.Label);
            imgui_SameLine();
            imgui_TextColored(values[0], values[1], values[2], values[3], "###");

            double r = values[0];
            imgui_SetNextItemWidth(160f);
            if (imgui_SliderDouble($"R##{entry.Color}", ref r, 0.0, 1.0, "%.2f"))
            {
                values[0] = (float)r;
                changed = true;
            }

            double g = values[1];
            imgui_SetNextItemWidth(160f);
            if (imgui_SliderDouble($"G##{entry.Color}", ref g, 0.0, 1.0, "%.2f"))
            {
                values[1] = (float)g;
                changed = true;
            }

            double b = values[2];
            imgui_SetNextItemWidth(160f);
            if (imgui_SliderDouble($"B##{entry.Color}", ref b, 0.0, 1.0, "%.2f"))
            {
                values[2] = (float)b;
                changed = true;
            }

            double a = values[3];
            imgui_SetNextItemWidth(160f);
            if (imgui_SliderDouble($"A##{entry.Color}", ref a, 0.0, 1.0, "%.2f"))
            {
                values[3] = (float)a;
                changed = true;
            }

            return changed;
        }

        private static void RenderThemeModal()
        {
            EnsureThemeInitialized();

            PushE3Theme();
            int flags = (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse | ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoDocking);
            bool open = imgui_Begin(_themeWindowTitle, flags);
            if (open)
            {
                imgui_Text("Adjust the active UI theme or switch to a preset.");

                string presetLabel = _themeDirty ? $"{_currentThemePreset.Name} (modified)" : _currentThemePreset.Name;
                if (_comboAvailable)
                {
                    bool opened = BeginComboSafe("Theme Preset", presetLabel);
                    if (opened)
                    {
                        foreach (var preset in _themePresets)
                        {
                            bool selected = ReferenceEquals(preset, _currentThemePreset) && !_themeDirty;
                            if (imgui_Selectable(preset.Name, selected))
                            {
                                ApplyThemePreset(preset);
                                // Save the selected theme to character settings
                                E3.CharacterSettings.Misc_UITheme = preset.Name;
                                E3.CharacterSettings.SaveData();
                            }
                        }
                        EndComboSafe();
                    }
                }
                else
                {
                    imgui_Text("Theme presets unavailable (combo unsupported).");
                }

                imgui_SameLine();
                if (imgui_Button("Close"))
                {
                    _themeWindowOpen = false;
                }

                if (_themeDirty)
                {
                    imgui_SameLine();
                    if (imgui_Button("Revert"))
                    {
                        ApplyThemePreset(_currentThemePreset);
                    }
                }

                imgui_Separator();

                foreach (var section in _themeEditorSections)
                {
                    if (imgui_CollapsingHeader(section.Header, 0))
                    {
                        foreach (var entry in section.Entries)
                        {
                            if (RenderThemeColorEditor(entry))
                            {
                                _themeDirty = true;
                            }
                            imgui_Separator();
                        }
                    }
                }
            }

            imgui_End();
            PopE3Theme();

            if (!open)
            {
                _themeWindowOpen = false;
            }
        }

        private static void RenderHeaderControls()
        {
            const float controlColumnWidth = 55f;

            if (imgui_BeginTable("E3HeaderControlsTable", 4, (int)ImGuiTableFlags.ImGuiTableFlags_SizingStretchSame, 0f))
            {
                imgui_TableSetupColumn("Fill", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 1f);
                imgui_TableSetupColumn("DonateButton", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, controlColumnWidth);
                imgui_TableSetupColumn("ThemeButton", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, controlColumnWidth);
                imgui_TableSetupColumn("CloseButton", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthFixed, controlColumnWidth);
                imgui_TableNextRow();

                if (imgui_TableNextColumn())
                {
                    // Empty stretch column to push buttons to the right edge
                }

                if (imgui_TableNextColumn())
                {
                    if (imgui_Button("Donate"))
                    {
                        _showDonateModal = true;
                    }
                }

                if (imgui_TableNextColumn())
                {
                    if (imgui_Button("Theme"))
                    {
                        _themeWindowOpen = !_themeWindowOpen;
                    }
                }

                if (imgui_TableNextColumn())
                {
                    if (imgui_Button("Close"))
                    {
                        _themeWindowOpen = false;
                        imgui_Begin_OpenFlagSet(_e3ImGuiWindow, false);
                    }
                }

                imgui_EndTable();
            }
        }

        private static void OpenUrl(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                E3.Log.Write($"Failed to open URL: {url} - {ex.Message}", Logging.LogLevels.Error);
            }
        }

        private static void RenderDonateModal()
        {
            imgui_Begin_OpenFlagSet("Support E3", true);
            bool open = imgui_Begin("Support E3", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
            if (open)
            {
                imgui_TextColored(0.9f, 0.9f, 0.6f, 1.0f, "Hi, Ty for thinking of donating!\nIf you wish to donate, please use friends and family.");
                imgui_Separator();
                
                // Buttons centered horizontally
                float avail = imgui_GetContentRegionAvailX();
                float yesW = 60f;
                float noW = 60f;
                float spacing = 8f;
                float total = yesW + spacing + noW;
                if (total < avail)
                {
                    imgui_SameLineEx((avail - total) / 2f, 0f);
                }
                if (imgui_Button("Yes"))
                {
                    OpenUrl("https://www.paypal.com/paypalme/RekkaSoftware");
                    _showDonateModal = false;
                }
                imgui_SameLine();
                if (imgui_Button("No"))
                {
                    _showDonateModal = false;
                }
            }
            imgui_End();
            if (!open)
            {
                _showDonateModal = false;
            }
        }
        // Config UI toggle: "/e3imgui".
        private static readonly string _e3ImGuiWindow = "E3Next Config";
        private static bool _imguiInitDone = false;
        private static bool _imguiContextReady = false;
        // Hunt window state
        private static readonly string _e3HuntWindow = "E3Next Hunt";
        private static bool _huntWindowOpen = false;
        private static bool _huntWindowMinimized = false;
        private static string _huntRadiusBuf = string.Empty;
        private static string _huntZRadiusBuf = string.Empty;
        private static string _huntPullBuf = string.Empty;
        private static string _huntIgnoreBuf = string.Empty;
        private static string _huntPullMethod = string.Empty;
        private static string _huntPullSpell = string.Empty;
        private static string _huntPullItem = string.Empty;
        private static string _huntPullAA = string.Empty;
        private static string _huntPullDisc = string.Empty;
        // Hunt debug window state
        private static readonly string _e3HuntDebugWindow = "E3Next Hunt Debug";
        private static bool _huntDebugWindowOpen = false;
        private static int _huntDebugLines = 200;
        private static string _huntDebugCopyStatus = string.Empty;
        private static bool TryCopyToClipboard(string text)
        {
            try
            {
                // Try common clipboard helpers if MQ2Mono exposes them
                var t = typeof(MonoCore.Core);
                var m = t.GetMethod("imgui_SetClipboardText", BindingFlags.Public | BindingFlags.Static);
                if (m != null)
                {
                    m.Invoke(null, new object[] { text ?? string.Empty });
                    return true;
                }
                m = t.GetMethod("imgui_LogTextToClipboard", BindingFlags.Public | BindingFlags.Static);
                if (m != null)
                {
                    m.Invoke(null, new object[] { text ?? string.Empty });
                    return true;
                }
            }
            catch { }
            return false;
        }
        
        // Static UI data to completely avoid MQ queries during rendering
        private static List<string> _uiIgnoreList = new List<string>();
        private static string _uiCurrentZone = "Unknown";
        private static bool _uiDataNeedsRefresh = true;

        // Queue to apply UI-driven changes safely on the processing loop
        public static ConcurrentQueue<Action> UIApplyQueue = new ConcurrentQueue<Action>();
        public static void EnqueueUI(Action a)
        {
            if (a != null) UIApplyQueue.Enqueue(a);
        }

        // Settings viewer state/cache
        private enum SettingsTab { Character, General, Advanced }
        private static SettingsTab _activeSettingsTab = SettingsTab.Character;
        private static string _activeSettingsFilePath = string.Empty;
        private static string[] _activeSettingsFileLines = Array.Empty<string>();
        private static long _nextIniRefreshAtMs = 0;
        private static string _selectedCharacterSection = string.Empty;
        private static Dictionary<string, string> _charIniEdits = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static List<string> _cfgSectionsOrdered = new List<string>();
        private static string _cfg_LastIniPath = string.Empty;
        private static string _cfgSelectedSection = string.Empty;
        private static string _cfgSelectedKey = string.Empty; // subsection/key
        private static int _cfgSelectedValueIndex = -1;
        private static bool _cfg_Dirty = false;
        // Inline edit helpers
        private static int _cfgInlineEditIndex = -1;
        private static string _cfgInlineEditBuffer = string.Empty;
        
        // Collapsible section state tracking
        private static Dictionary<string, bool> _cfgSectionExpanded = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static string _cfgNewKeyBuffer = string.Empty;

        // "All Players" key view state/cache
        private static bool _cfgAllPlayersView = false; // aggregated view
        private static string _cfgAllPlayersSig = string.Empty; // section::key
        private static long _cfgAllPlayersNextRefreshAtMs = 0;
        private static List<System.Collections.Generic.KeyValuePair<string, string>> _cfgAllPlayersRows = new List<System.Collections.Generic.KeyValuePair<string, string>>();
        private static Dictionary<string, string> _cfgAllPlayersServerByToon = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> _cfgAllPlayersIsRemote = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, string> _cfgAllPlayersEditBuf = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cfgAllPlayersLock = new object();
        private static System.Threading.Tasks.Task _cfgAllPlayersWorkerTask = null;
        private static bool _cfgAllPlayersRefreshRequested = false;
        private static bool _cfgAllPlayersRefreshing = false;
        private static string _cfgAllPlayersReqSection = string.Empty;
        private static string _cfgAllPlayersReqKey = string.Empty;
        private static long _cfgAllPlayersLastUpdatedAt = 0;
        private static int _cfgAllPlayersRefreshIntervalMs = 5000;
        private static string _cfgAllPlayersStatus = string.Empty;

        // Character .ini selection state
        private static string _selectedCharIniPath = string.Empty; // defaults to current character
        private static IniData _selectedCharIniParsedData = null;  // parsed data for non-current selection
        private static string[] _charIniFiles = Array.Empty<string>();
        private static long _nextIniFileScanAtMs = 0;
        // Dropdown support (feature-detect combo availability to avoid crashes on older MQ2Mono)
        private static bool _comboAvailable = true;

        
        public static void OnUpdateImGui()
        {
            try
            {
                // Early exit if ImGui functions aren't available
                if (!_imguiContextReady && _imguiInitDone)
                {
                    return; // ImGui failed to initialize, skip rendering
                }
                
                // Initialize window visibility once (default hidden)
                if (!_imguiInitDone)
                {
                    try 
                    { 
                        imgui_Begin_OpenFlagSet(_e3ImGuiWindow, false);
                        _imguiContextReady = true; // Mark as ready after first successful ImGui call
                        E3.Log.Write("ImGui initialized successfully", Logging.LogLevels.Info);
                    }
                    catch (Exception ex)
                    {
                        E3.Log.Write($"ImGui initialization failed: {ex.Message}", Logging.LogLevels.Error);
                        _imguiContextReady = false; // Mark as failed
                        _imguiInitDone = true;
                        return; // Exit early to prevent further ImGui calls
                    }
                    _imguiInitDone = true;
                }

            // Only render if ImGui is available and ready
            if (_imguiContextReady && imgui_Begin_OpenFlagGet(_e3ImGuiWindow))
            {
                // Apply active E3 theme colors
                PushE3Theme();
                imgui_Begin(_e3ImGuiWindow, (int)ImGuiWindowFlags.ImGuiWindowFlags_None);

                // Header with better styling
                if (imgui_BeginTable("E3HeaderTable", 2, (int)ImGuiTableFlags.ImGuiTableFlags_SizingStretchSame, 0f))
                {
                    imgui_TableSetupColumn("Info", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 1f);
                    imgui_TableSetupColumn("Controls", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0f);
                    imgui_TableNextRow();

                    if (imgui_TableNextColumn())
                    {
                        imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"nE³xt v{E3Core.Processors.Setup._e3Version} | Build {E3Core.Processors.Setup._buildDate}");
                    }

                    if (imgui_TableNextColumn())
                    {
                        RenderHeaderControls();
                    }

                    imgui_EndTable();
                }

                    imgui_Separator();

                // Character INI selector (used by Config Editor)
                RenderCharacterIniSelector();

                imgui_Separator();
                
                // All Players View toggle with better styling
                imgui_Text("View Mode:");
                imgui_SameLine();
                if (imgui_Button(_cfgAllPlayersView ? "Switch to Character View" : "Switch to All Players View"))
                {
                    _cfgAllPlayersView = !_cfgAllPlayersView;
                }
                imgui_SameLine();
                imgui_TextColored(0.3f, 0.8f, 0.3f, 1.0f, _cfgAllPlayersView ? "All Players Mode" : "Character Mode");

                imgui_Separator();

                if (_cfgAllPlayersView)
                {
                    string currentSig = $"{_cfgSelectedSection}::{_cfgSelectedKey}";
                    if (!string.Equals(currentSig, _cfgAllPlayersSig, StringComparison.OrdinalIgnoreCase))
                    {
                        _cfgAllPlayersSig = currentSig;
                        _cfgAllPlayersRefreshRequested = true;
                    }
                }

                // Config Editor only
                if (_cfgAllPlayersView)
                {
                    RenderAllPlayersView();
                }
                else
                {
                    RenderConfigEditor();
                }


                imgui_End();
                PopE3Theme();
            }

            if (_imguiContextReady && _themeWindowOpen)
            {
                RenderThemeModal();
            }

            if (_imguiContextReady && _showDonateModal)
            {
                RenderDonateModal();
            }

            // Render Hunt window if toggled
            if (_imguiContextReady && _huntWindowOpen)
            {
                if (_huntWindowMinimized)
                {
                    // Floating UI - clean and compact
                    int windowFlags = (int)(ImGuiWindowFlags.ImGuiWindowFlags_NoDecoration | 
                                           ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | 
                                           ImGuiWindowFlags.ImGuiWindowFlags_NoFocusOnAppearing | 
                                           ImGuiWindowFlags.ImGuiWindowFlags_NoNav);
                    
                    // Semi-transparent background for subtle appearance
                    imgui_SetNextWindowBgAlpha(0.9f);
                    
                    // Match E3 theme for the float window
                    PushE3Theme();
                    bool open = imgui_Begin("E3Hunt Float", windowFlags);
                    if (open)
                    {
                        // Get current status for color coding
                        bool isActive = E3Core.Processors.Hunt.Enabled && E3Core.Processors.Hunt.Go;
                        bool enabled = E3Core.Processors.Hunt.Enabled;
                        
                        // Functional status line with key info
                        string status = E3Core.Processors.Hunt.Status ?? "Idle";
                        string targetName = E3Core.Processors.Hunt.TargetName;
                        int radius = E3Core.Processors.Hunt.Radius;
                        
                        // Show current target if we have one
                        if (!string.IsNullOrEmpty(targetName) && E3Core.Processors.Hunt.TargetID > 0)
                        {
                            imgui_TextColored(0.9f, 0.6f, 0.6f, 1.0f, $"Target: {targetName}");
                        }
                        else
                        {
                            imgui_TextColored(0.7f, 0.7f, 0.9f, 1.0f, $"Status: {status}");
                        }
                        
                        // Quick info line
                        imgui_TextColored(0.8f, 0.8f, 0.8f, 1.0f, $"Range: {radius}  State: {(isActive ? "Active" : enabled ? "Ready" : "Off")}");
                        
                        imgui_Separator();
                        
                        // Power toggle
                        imgui_PushStyleColor(21, enabled ? 0.2f : 0.6f, enabled ? 0.8f : 0.3f, enabled ? 0.2f : 0.3f, 1.0f);
                        if (imgui_ButtonEx(enabled ? "ON" : "OFF", 40, 25))
                        {
                            bool now = !enabled;
                            EnqueueUI(() => E3Core.Processors.Hunt.Enabled = now);
                        }
                        imgui_PopStyleColor();
                        
                        imgui_SameLine();
                        
                        // Go/Stop with proper sizing for PAUSE text
                        bool go = E3Core.Processors.Hunt.Go;
                        imgui_PushStyleColor(21, go ? 0.1f : 0.7f, go ? 0.7f : 0.5f, go ? 0.1f : 0.1f, 1.0f);
                        if (imgui_ButtonEx(go ? "GO" : "STOP", 50, 25))
                        {
                            bool now = !go;
                            EnqueueUI(() => E3Core.Processors.Hunt.Go = now);
                        }
                        imgui_PopStyleColor();

                        imgui_SameLine();
                        
                        // Expand with better text
                        imgui_PushStyleColor(21, 0.3f, 0.5f, 0.8f, 1.0f);
                        if (imgui_ButtonEx("More", 40, 25))
                        {
                            _huntWindowMinimized = false;
                        }
                        imgui_PopStyleColor();

                        // Right-click anywhere on window to expand
                        if (imgui_IsWindowHovered() && imgui_IsMouseClicked(1))
                        {
                            _huntWindowMinimized = false;
                        }
                    }
                    imgui_End();
                    PopE3Theme();
                }
                else
                {
                    // Full window - original layout
                    imgui_SetNextWindowSizeConstraints(450, 300, 800, 600);
                    PushE3Theme();
                    bool open = imgui_Begin(_e3HuntWindow, (int)ImGuiWindowFlags.ImGuiWindowFlags_None);
                    if (open)
                    {
                        // Enhanced header with improved visual hierarchy
                        if (imgui_BeginTable("##hunt_header", 3, 0, 0))
                        {
                            imgui_TableSetupColumn("Title", 0, 0);
                            imgui_TableSetupColumn("Status", 0, 0);
                            imgui_TableSetupColumn("Controls", 0, 0);
                            
                            imgui_TableNextColumn();
                            // Title with enhanced styling
                            imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "E3Next Hunt Mode");
                            
                            imgui_TableNextColumn();
                            // Enhanced status display
                            string status = E3Core.Processors.Hunt.Status ?? "Unknown";
                            bool isActive = E3Core.Processors.Hunt.Enabled && E3Core.Processors.Hunt.Go;
                            bool enabled = E3Core.Processors.Hunt.Enabled;
                            
                            // State indicator block
                            string stateBlock = isActive ? "[ACTIVE]" : (enabled ? "[READY]" : "[OFF]");
                            if (isActive && status.Contains("Scanning"))
                            {
                                imgui_TextColored(0.3f, 1.0f, 0.3f, 1.0f, stateBlock);
                                imgui_SameLine();
                                imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, $" {status}");
                            }
                            else if (isActive)
                            {
                                imgui_TextColored(0.9f, 0.9f, 0.3f, 1.0f, stateBlock);
                                imgui_SameLine();
                                imgui_TextColored(0.9f, 0.8f, 0.6f, 1.0f, $" {status}");
                            }
                            else if (enabled)
                            {
                                imgui_TextColored(0.8f, 0.6f, 0.3f, 1.0f, stateBlock);
                                imgui_SameLine();
                                imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, $" {status}");
                            }
                            else
                            {
                                imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, stateBlock);
                                imgui_SameLine();
                                imgui_TextColored(0.5f, 0.5f, 0.5f, 1.0f, $" {status}");
                            }
                            
                            imgui_TableNextColumn();
                            // Minimize button with better styling
                            imgui_PushStyleColor(21, 0.2f, 0.4f, 0.7f, 1.0f); // Blue button
                            if (imgui_Button("Float"))
                            {
                                _huntWindowMinimized = true;
                            }
                            imgui_PopStyleColor();
                            
                            imgui_EndTable();
                        }
                    
                    imgui_Separator();

                    // Enhanced tab bar with visual styling
                    imgui_PushStyleColor(33, 0.15f, 0.25f, 0.4f, 1.0f); // Tab background
                    imgui_PushStyleColor(34, 0.3f, 0.5f, 0.8f, 1.0f);   // Tab active
                    imgui_PushStyleColor(35, 0.2f, 0.4f, 0.6f, 1.0f);   // Tab hovered
                    
                    if (imgui_BeginTabBar("##hunt_tabs"))
                    {
                        // Main Controls Tab
                        if (imgui_BeginTabItem("Controls"))
                        {
                            // Quick action controls
                            imgui_PushStyleColor(21, 0.2f, 0.6f, 0.2f, 1.0f);
                            if (imgui_Button("Start Hunting"))
                            {
                                EnqueueUI(() => {
                                    E3Core.Processors.Hunt.Enabled = true;
                                    E3Core.Processors.Hunt.Go = true;
                                });
                            }
                            imgui_PopStyleColor();
                            
                            imgui_SameLine();
                            imgui_PushStyleColor(21, 0.6f, 0.6f, 0.2f, 1.0f);
                            if (imgui_Button("Pause Hunt"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.Go = false);
                            }
                            imgui_PopStyleColor();
                            
                            imgui_SameLine();
                            imgui_PushStyleColor(21, 0.6f, 0.2f, 0.2f, 1.0f);
                            if (imgui_Button("Stop Hunt"))
                            {
                                EnqueueUI(() => {
                                    E3Core.Processors.Hunt.Go = false;
                                    E3Core.Processors.Hunt.Enabled = false;
                                });
                            }
                            imgui_PopStyleColor();
                            
                            imgui_Separator();
                            
                            // Control toggles in a table
                            if (imgui_BeginTable("##hunt_controls", 2, 0, 0))
                            {
                                imgui_TableSetupColumn("Control", 0, 150);
                                imgui_TableSetupColumn("Setting", 0, 200);
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Hunt Mode:");
                                imgui_TableNextColumn();
                                bool enabled = E3Core.Processors.Hunt.Enabled;
                                bool newEnabled = imgui_Checkbox("Enabled", enabled);
                                if (newEnabled != enabled)
                                {
                                    bool now = newEnabled;
                                    EnqueueUI(() => E3Core.Processors.Hunt.Enabled = now);
                                }
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Hunt Status:");
                                imgui_TableNextColumn();
                                bool go = E3Core.Processors.Hunt.Go;
                                bool newGo = imgui_Checkbox("Go/Pause", go);
                                if (newGo != go)
                                {
                                    bool now = newGo;
                                    EnqueueUI(() => E3Core.Processors.Hunt.Go = now);
                                }
                                
                                imgui_EndTable();
                            }
                            
                            imgui_Separator();
                            
                            // Range settings with enhanced header
                            imgui_Separator();
                            imgui_PushStyleColor(23, 0.2f, 0.3f, 0.5f, 0.3f); // Header background
                            imgui_TextColored(0.9f, 1.0f, 1.0f, 1.0f, "Search Range Settings");
                            imgui_PopStyleColor();
                            
                            // Quick range buttons
                            int currentRadius = E3Core.Processors.Hunt.Radius;
                            imgui_Text($"Current Radius: {currentRadius}");
                            
                            if (imgui_Button("100"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.Radius = 100);
                                _huntRadiusBuf = "100";
                            }
                            imgui_SameLine();
                            if (imgui_Button("250"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.Radius = 250);
                                _huntRadiusBuf = "250";
                            }
                            imgui_SameLine();
                            if (imgui_Button("500"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.Radius = 500);
                                _huntRadiusBuf = "500";
                            }
                            imgui_SameLine();
                            if (imgui_Button("1000"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.Radius = 1000);
                                _huntRadiusBuf = "1000";
                            }
                            
                            if (imgui_BeginTable("##hunt_range", 2, 0, 0))
                            {
                                imgui_TableSetupColumn("Parameter", 0, 100);
                                imgui_TableSetupColumn("Value", 0, 150);
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Custom Radius:");
                                imgui_TableNextColumn();
                                imgui_SetNextItemWidth(100);
                                _huntRadiusBuf = string.IsNullOrEmpty(_huntRadiusBuf) ? E3Core.Processors.Hunt.Radius.ToString() : _huntRadiusBuf;
                                if (imgui_InputText("##radius", _huntRadiusBuf))
                                {
                                    _huntRadiusBuf = imgui_InputText_Get("##radius");
                                    if (int.TryParse(_huntRadiusBuf, out var r)) EnqueueUI(() => E3Core.Processors.Hunt.Radius = Math.Max(10, r));
                                }
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Z-Radius:");
                                imgui_TableNextColumn();
                                imgui_SetNextItemWidth(100);
                                _huntZRadiusBuf = string.IsNullOrEmpty(_huntZRadiusBuf) ? E3Core.Processors.Hunt.ZRadius.ToString() : _huntZRadiusBuf;
                                if (imgui_InputText("##zradius", _huntZRadiusBuf))
                                {
                                    _huntZRadiusBuf = imgui_InputText_Get("##zradius");
                                    if (int.TryParse(_huntZRadiusBuf, out var zr)) EnqueueUI(() => E3Core.Processors.Hunt.ZRadius = Math.Max(10, zr));
                                }
                                
                                imgui_EndTable();
                            }
                            
                            imgui_Separator();
                            
                            // Camp settings with enhanced header
                            imgui_Separator();
                            imgui_PushStyleColor(23, 0.2f, 0.5f, 0.3f, 0.3f); // Green header background
                            imgui_TextColored(0.9f, 1.0f, 0.9f, 1.0f, "Camp Settings");
                            imgui_PopStyleColor();
                            bool camp = E3Core.Processors.Hunt.CampOn;
                            bool newCamp = imgui_Checkbox("Camp Mode", camp);
                            if (newCamp != camp)
                            {
                                bool now = newCamp;
                                EnqueueUI(() => E3Core.Processors.Hunt.CampOn = now);
                            }
                            imgui_SameLine();
                            if (imgui_Button("Set Camp Here"))
                            {
                                EnqueueUI(() => {
                                    E3Core.Processors.Hunt.CampOn = true;
                                    E3Core.Processors.Hunt.CampX = E3.MQ.Query<int>("${Me.X}");
                                    E3Core.Processors.Hunt.CampY = E3.MQ.Query<int>("${Me.Y}");
                                    E3Core.Processors.Hunt.CampZ = E3.MQ.Query<int>("${Me.Z}");
                                });
                            }
                            
                            if (camp)
                            {
                                imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, $"Camp Location: {E3Core.Processors.Hunt.CampX}, {E3Core.Processors.Hunt.CampY}, {E3Core.Processors.Hunt.CampZ}");
                            }
                            
                            imgui_EndTabItem();
                        }

                        // Filters Tab
                        if (imgui_BeginTabItem("Filters"))
                        {
                            // Enhanced filter section header
                            imgui_PushStyleColor(23, 0.5f, 0.2f, 0.3f, 0.3f); // Reddish header background
                            imgui_TextColored(1.0f, 0.9f, 0.9f, 1.0f, "Target Filters");
                            imgui_PopStyleColor();
                            imgui_TextWrapped("Use | to separate multiple filter terms. 'ALL' means no filter, 'NONE' means no exclusions.");
                            
                            // Quick filter presets
                            imgui_Text("Quick Presets:");
                            if (imgui_Button("All Mobs"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.PullFilters = "ALL");
                                _huntPullBuf = "ALL";
                            }
                            imgui_SameLine();
                            if (imgui_Button("Animals Only"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.PullFilters = "wolf|bear|spider|rat|bat|snake");
                                _huntPullBuf = "wolf|bear|spider|rat|bat|snake";
                            }
                            imgui_SameLine();
                            if (imgui_Button("Undead Only"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.PullFilters = "skeleton|zombie|spirit|ghost|wraith");
                                _huntPullBuf = "skeleton|zombie|spirit|ghost|wraith";
                            }
                            
                            imgui_Text("Pull Filters (Include):");
                            imgui_SetNextItemWidth(-1);
                            _huntPullBuf = string.IsNullOrEmpty(_huntPullBuf) ? (E3Core.Processors.Hunt.PullFilters ?? string.Empty) : _huntPullBuf;
                            if (imgui_InputText("##pull_filters", _huntPullBuf))
                            {
                                _huntPullBuf = imgui_InputText_Get("##pull_filters");
                                EnqueueUI(() => E3Core.Processors.Hunt.PullFilters = _huntPullBuf);
                            }

                            imgui_Text("Ignore Filters (Exclude):");
                            
                            // Quick ignore presets
                            if (imgui_Button("Clear Ignores"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.IgnoreFilters = "NONE");
                                _huntIgnoreBuf = "NONE";
                            }
                            imgui_SameLine();
                            if (imgui_Button("Ignore Guards"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.IgnoreFilters = "guard|merchant|banker|guildmaster");
                                _huntIgnoreBuf = "guard|merchant|banker|guildmaster";
                            }
                            imgui_SameLine();
                            if (imgui_Button("Ignore NPCs"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.IgnoreFilters = "merchant|banker|guildmaster|trainer|vendor");
                                _huntIgnoreBuf = "merchant|banker|guildmaster|trainer|vendor";
                            }
                            
                            imgui_SetNextItemWidth(-1);
                            _huntIgnoreBuf = string.IsNullOrEmpty(_huntIgnoreBuf) ? (E3Core.Processors.Hunt.IgnoreFilters ?? string.Empty) : _huntIgnoreBuf;
                            if (imgui_InputText("##ignore_filters", _huntIgnoreBuf))
                            {
                                _huntIgnoreBuf = imgui_InputText_Get("##ignore_filters");
                                EnqueueUI(() => E3Core.Processors.Hunt.IgnoreFilters = _huntIgnoreBuf);
                            }
                            
                            // Enhanced ignore button with warning styling
                            imgui_PushStyleColor(21, 0.7f, 0.3f, 0.1f, 1.0f); // Orange warning button
                            if (imgui_Button("Ignore Current Target"))
                            {
                                EnqueueUI(() => {
                                    var nm = E3.MQ.Query<string>("${Target.CleanName}");
                                    if (!string.IsNullOrWhiteSpace(nm) && !string.Equals(nm, "NULL", StringComparison.OrdinalIgnoreCase))
                                    {
                                        E3Core.Processors.Hunt.AddIgnoreName(nm);
                                    }
                                });
                                // Mark for refresh after adding
                                _uiDataNeedsRefresh = true;
                            }
                            imgui_PopStyleColor();

                            imgui_Separator();
                            imgui_TextColored(0.8f, 0.85f, 0.95f, 1.0f, $"Permanently Ignored Mobs - Zone: {_uiCurrentZone}");
                            
                            // Refresh button for manual data refresh
                            imgui_SameLine();
                            if (imgui_Button("Refresh##ignore_refresh"))
                            {
                                _uiDataNeedsRefresh = true;
                                RefreshUIData();
                            }
                            
                            // Auto-refresh data if needed (but not during rendering)
                            if (_uiDataNeedsRefresh)
                            {
                                RefreshUIData();
                            }
                            
                            if (imgui_BeginChild("##hunt_ignore_list", -1, 150, true))
                            {
                                // Use static UI data - no MQ queries during rendering
                                if (_uiIgnoreList.Count == 0)
                                {
                                    imgui_TextColored(0.5f, 0.5f, 0.5f, 1.0f, "No mobs in ignore list for current zone");
                                }
                                else
                                {
                                    // Create table with 3 columns: #, Name, Delete
                                    if (imgui_BeginTable("##ignore_table", 3, 0, 0))
                                    {
                                        imgui_TableSetupColumn("#", 0, 5);
                                        imgui_TableSetupColumn("Name", 0, 65);
                                        imgui_TableSetupColumn("Delete", 0, 10);
                                        imgui_TableHeadersRow();
                                        
                                        for (int i = 0; i < _uiIgnoreList.Count; i++)
                                        {
                                            imgui_TableNextRow();
                                            
                                            // Column 1: Row number
                                            imgui_TableNextColumn();
                                            imgui_Text((i + 1).ToString());
                                            
                                            // Column 2: Mob name
                                            imgui_TableNextColumn();
                                            imgui_Text(_uiIgnoreList[i]);
                                            
                                            // Column 3: Delete button
                                            imgui_TableNextColumn();
                                            if (imgui_Button($"Del##{i}"))
                                            {
                                                string mobToRemove = _uiIgnoreList[i];
                                                EnqueueUI(() => {
                                                    E3Core.Processors.Hunt.RemoveIgnoreName(mobToRemove);
                                                });
                                                // Mark for refresh after deletion
                                                _uiDataNeedsRefresh = true;
                                            }
                                        }
                                        imgui_EndTable();
                                    }
                                }
                                imgui_EndChild();
                            }
                            
                            imgui_EndTabItem();
                        }

                        // Pull Config Tab
                        if (imgui_BeginTabItem("Pull Config"))
                        {
                            imgui_TextColored(0.9f, 0.8f, 0.6f, 1.0f, "Pull Configuration");
                            
                            // Pull method combo
                            imgui_Text("Pull Method:");
                            imgui_SetNextItemWidth(200);
                            string currentMethod = E3Core.Processors.Hunt.PullMethod ?? "None";
                            _huntPullMethod = string.IsNullOrEmpty(_huntPullMethod) ? currentMethod : _huntPullMethod;
                            
                            string[] pullMethods = { "None", "Ranged", "Spell", "Item", "AA", "Disc", "Attack", "Melee" };
                            string previewMethod = _huntPullMethod;
                            if (imgui_BeginCombo("##pull_method", previewMethod, 0))
                            {
                                foreach (var method in pullMethods)
                                {
                                    if (imgui_Selectable(method, method == _huntPullMethod))
                                    {
                                        _huntPullMethod = method;
                                        EnqueueUI(() => { E3Core.Processors.Hunt.PullMethod = method; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                                    }
                                }
                                imgui_EndCombo();
                            }
                            
                            // Method-specific settings
                            if (!string.Equals(_huntPullMethod, "None", StringComparison.OrdinalIgnoreCase) && 
                                !string.Equals(_huntPullMethod, "Ranged", StringComparison.OrdinalIgnoreCase))
                            {
                                imgui_Separator();
                                
                                if (string.Equals(_huntPullMethod, "Spell", StringComparison.OrdinalIgnoreCase))
                                {
                                    imgui_Text("Spell Name:");
                                    imgui_SetNextItemWidth(-1);
                                    _huntPullSpell = string.IsNullOrEmpty(_huntPullSpell) ? (E3Core.Processors.Hunt.PullSpell ?? string.Empty) : _huntPullSpell;
                                    if (imgui_InputText("##pull_spell", _huntPullSpell))
                                    {
                                        _huntPullSpell = imgui_InputText_Get("##pull_spell");
                                        EnqueueUI(() => { E3Core.Processors.Hunt.PullSpell = _huntPullSpell; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                                    }
                                }
                                else if (string.Equals(_huntPullMethod, "Item", StringComparison.OrdinalIgnoreCase))
                                {
                                    imgui_Text("Item Name:");
                                    imgui_SetNextItemWidth(-1);
                                    _huntPullItem = string.IsNullOrEmpty(_huntPullItem) ? (E3Core.Processors.Hunt.PullItem ?? string.Empty) : _huntPullItem;
                                    if (imgui_InputText("##pull_item", _huntPullItem))
                                    {
                                        _huntPullItem = imgui_InputText_Get("##pull_item");
                                        EnqueueUI(() => { E3Core.Processors.Hunt.PullItem = _huntPullItem; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                                    }
                                }
                                else if (string.Equals(_huntPullMethod, "AA", StringComparison.OrdinalIgnoreCase))
                                {
                                    imgui_Text("AA Name:");
                                    imgui_SetNextItemWidth(-1);
                                    _huntPullAA = string.IsNullOrEmpty(_huntPullAA) ? (E3Core.Processors.Hunt.PullAA ?? string.Empty) : _huntPullAA;
                                    if (imgui_InputText("##pull_aa", _huntPullAA))
                                    {
                                        _huntPullAA = imgui_InputText_Get("##pull_aa");
                                        EnqueueUI(() => { E3Core.Processors.Hunt.PullAA = _huntPullAA; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                                    }
                                }
                                else if (string.Equals(_huntPullMethod, "Disc", StringComparison.OrdinalIgnoreCase))
                                {
                                    imgui_Text("Discipline Name:");
                                    imgui_SetNextItemWidth(-1);
                                    _huntPullDisc = string.IsNullOrEmpty(_huntPullDisc) ? (E3Core.Processors.Hunt.PullDisc ?? string.Empty) : _huntPullDisc;
                                    if (imgui_InputText("##pull_disc", _huntPullDisc))
                                    {
                                        _huntPullDisc = imgui_InputText_Get("##pull_disc");
                                        EnqueueUI(() => { E3Core.Processors.Hunt.PullDisc = _huntPullDisc; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                                    }
                                }
                            }
                            
                            imgui_Separator();
                            bool aa = E3Core.Processors.Hunt.AutoAssistAtMelee;
                            bool newAA = imgui_Checkbox("Auto Assist at Melee Range", aa);
                            if (newAA != aa)
                            {
                                bool now = newAA;
                                EnqueueUI(() => E3Core.Processors.Hunt.AutoAssistAtMelee = now);
                            }

                            // Advanced pull tuning similar to rgmercs
                            imgui_Separator();
                            imgui_TextColored(0.9f, 0.9f, 0.7f, 1.0f, "Advanced Tuning");

                            // MaxPathRange
                            int mpr = E3Core.Processors.Hunt.MaxPathRange;
                            imgui_Text("Max Path Range (0 disables):");
                            imgui_SetNextItemWidth(220);
                            if (imgui_SliderInt("##hunt_mpr", ref mpr, 0, 10000))
                            {
                                int v = Math.Max(0, mpr);
                                EnqueueUI(() => { E3Core.Processors.Hunt.MaxPathRange = v; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                            }

                            // PullIgnoreTimeSec
                            int pits = E3Core.Processors.Hunt.PullIgnoreTimeSec;
                            imgui_Text("Pull Attempt Timeout (sec):");
                            imgui_SetNextItemWidth(220);
                            if (imgui_SliderInt("##hunt_pits", ref pits, 5, 60))
                            {
                                int v = Math.Max(0, pits);
                                EnqueueUI(() => { E3Core.Processors.Hunt.PullIgnoreTimeSec = v; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                            }

                            // TempIgnoreDurationSec
                            int tids = E3Core.Processors.Hunt.TempIgnoreDurationSec;
                            imgui_Text("Temp Ignore Duration (sec):");
                            imgui_SetNextItemWidth(220);
                            if (imgui_SliderInt("##hunt_tids", ref tids, 15, 120))
                            {
                                int v = Math.Max(1, tids);
                                EnqueueUI(() => { E3Core.Processors.Hunt.TempIgnoreDurationSec = v; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                            }

                            // RangedApproachFactor
                            double raf = E3Core.Processors.Hunt.RangedApproachFactor;
                            imgui_Text("Ranged Approach Factor (0.3 - 0.9):");
                            imgui_SetNextItemWidth(220);
                            if (imgui_SliderDouble("##hunt_raf", ref raf, 0.3, 0.9, "%.2f"))
                            {
                                double v = Math.Max(0.3, Math.Min(0.9, raf));
                                EnqueueUI(() => { E3Core.Processors.Hunt.RangedApproachFactor = v; E3Core.Processors.Hunt.SaveHuntPullSettings(); });
                            }

                            imgui_EndTabItem();
                        }

                        // Targets Tab
                        if (imgui_BeginTabItem("Targets"))
                        {
                            // Enhanced candidates header
                            imgui_PushStyleColor(23, 0.2f, 0.4f, 0.2f, 0.3f); // Green header background
                            imgui_TextColored(0.9f, 1.0f, 0.9f, 1.0f, "Hunt Candidates (Last Scan)");
                            imgui_PopStyleColor();
                            
                            // Target management controls
                            imgui_PushStyleColor(21, 0.2f, 0.6f, 0.2f, 1.0f); // Green action button
                            if (imgui_Button("Target Nearest"))
                            {
                                EnqueueUI(() => {
                                    var availableTargets = E3Core.Processors.Hunt.GetCandidatesSnapshot();
                                    if (availableTargets.Count > 0)
                                    {
                                        var nearest = availableTargets.OrderBy(c => c.distance).FirstOrDefault();
                                        if (nearest.id > 0)
                                        {
                                            E3Core.Processors.Hunt.ForceSetTarget(nearest.id);
                                        }
                                    }
                                });
                            }
                            imgui_PopStyleColor();
                            
                            imgui_SameLine();
                            imgui_PushStyleColor(21, 0.6f, 0.4f, 0.1f, 1.0f); // Orange utility button
                            if (imgui_Button("Clear Temp Ignores"))
                            {
                                EnqueueUI(() => E3Core.Processors.Hunt.ClearTempIgnores());
                            }
                            imgui_PopStyleColor();
                            
                            imgui_SameLine();
                            imgui_PushStyleColor(21, 0.6f, 0.2f, 0.2f, 1.0f); // Red button
                            if (imgui_Button("Stop Current Target"))
                            {
                                EnqueueUI(() => {
                                    E3Core.Processors.Hunt.TargetID = 0;
                                    E3Core.Processors.Hunt.TargetName = string.Empty;
                                });
                            }
                            imgui_PopStyleColor();

                            var candidates = E3Core.Processors.Hunt.GetCandidatesSnapshot();
                            if (candidates == null || candidates.Count == 0)
                            {
                                imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, "No candidates seen yet.");
                            }
                            else if (imgui_BeginChild("HuntTargetsChild", 0, 0, true))
                            {
                                float outerW = imgui_GetContentRegionAvailX();
                                float colNum = 40f;
                                float colLvl = 50f;
                                float colDist = 60f;
                                float colPath = 60f;
                                float colLoc = 180f;
                                float colActions = 140f;
                                float colName = Math.Max(160f, outerW - (colNum + colLvl + colDist + colPath + colLoc + colActions));

                                if (imgui_BeginTable("##hunt_candidates", 6, 0, outerW))
                                {
                                    imgui_TableSetupColumn("#", 0, colNum);
                                    imgui_TableSetupColumn("Name", 0, colName);
                                    imgui_TableSetupColumn("Lvl", 0, colLvl);
                                    imgui_TableSetupColumn("Dist", 0, colDist);
                                    imgui_TableSetupColumn("Path", 0, colPath);
                                    imgui_TableSetupColumn("Actions", 0, colActions);
                                    imgui_TableHeadersRow();

                                    for (int i = 0; i < candidates.Count; i++)
                                    {
                                        var c = candidates[i];
                                        imgui_TableNextRow();
                                        imgui_TableNextColumn(); imgui_Text((i + 1).ToString());
                                        imgui_TableNextColumn();
                                        // Con color styling similar to rgmercs
                                        var con = (c.con ?? string.Empty).ToUpperInvariant();
                                        float r=1f,g=1f,b=1f;
                                        if (con.Contains("GREY")) { r=.6f; g=.6f; b=.6f; }
                                        else if (con.Contains("GREEN")) { r=.2f; g=.9f; b=.2f; }
                                        else if (con.Contains("LIGHT") && con.Contains("BLUE")) { r=.4f; g=.7f; b=1f; }
                                        else if (con.Contains("BLUE")) { r=.2f; g=.5f; b=1f; }
                                        else if (con.Contains("WHITE")) { r=1f; g=1f; b=1f; }
                                        else if (con.Contains("YELLOW")) { r=1f; g=.9f; b=.2f; }
                                        else if (con.Contains("RED")) { r=1f; g=.2f; b=.2f; }
                                        imgui_TextColored(r,g,b,1.0f, c.name ?? string.Empty);
                                        imgui_TableNextColumn(); imgui_Text(c.level.ToString());
                                        imgui_TableNextColumn(); imgui_Text(string.Format("{0:0.0}", c.distance));
                                        imgui_TableNextColumn(); imgui_Text(c.pathLen > 0 ? string.Format("{0:0}", c.pathLen) : "-");
                                        imgui_TableNextColumn();
                                        // Target button with green styling
                                        imgui_PushStyleColor(21, 0.2f, 0.7f, 0.2f, 1.0f); // Green target button
                                        if (imgui_Button($"Target##{i}"))
                                        {
                                            int tid = c.id;
                                            EnqueueUI(() => E3Core.Processors.Hunt.ForceSetTarget(tid));
                                        }
                                        imgui_PopStyleColor();
                                        
                                        imgui_SameLine();
                                        
                                        // Ignore button with red styling
                                        imgui_PushStyleColor(21, 0.7f, 0.2f, 0.2f, 1.0f); // Red ignore button
                                        if (imgui_Button($"Ignore##{i}"))
                                        {
                                            string nm = c.name ?? string.Empty;
                                            if (!string.IsNullOrEmpty(nm))
                                            {
                                                EnqueueUI(() => E3Core.Processors.Hunt.AddIgnoreName(nm));
                                                _uiDataNeedsRefresh = true;
                                            }
                                        }
                                        imgui_PopStyleColor();
                                    }
                                    imgui_EndTable();
                                }
                                imgui_EndChild();
                            }

                            imgui_EndTabItem();
                        }

                        // Status Tab
                        if (imgui_BeginTabItem("Status"))
                        {
                            // Enhanced status header
                            imgui_PushStyleColor(23, 0.3f, 0.2f, 0.5f, 0.3f); // Purple header background
                            imgui_TextColored(1.0f, 0.9f, 1.0f, 1.0f, "Current Status");
                            imgui_PopStyleColor();
                            
                            if (imgui_BeginTable("##hunt_status", 2, 0, 0))
                            {
                                imgui_TableSetupColumn("Property", 0, 120);
                                imgui_TableSetupColumn("Value", 0, 200);
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Hunt State:");
                                imgui_TableNextColumn();
                                imgui_Text(E3Core.Processors.HuntStateMachine.CurrentState.ToString());

                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("State Reason:");
                                imgui_TableNextColumn();
                                imgui_Text(E3Core.Processors.HuntStateMachine.StateReason ?? "");

                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("State Elapsed:");
                                imgui_TableNextColumn();
                                var ms = E3Core.Processors.HuntStateMachine.StateElapsedMs;
                                imgui_Text(string.Format("{0:0.0}s", ms/1000.0));

                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Hunt Status:");
                                imgui_TableNextColumn();
                                imgui_Text(E3Core.Processors.Hunt.Status ?? "Unknown");
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Enabled:");
                                imgui_TableNextColumn();
                                imgui_TextColored(E3Core.Processors.Hunt.Enabled ? 0.2f : 0.6f, E3Core.Processors.Hunt.Enabled ? 0.8f : 0.6f, E3Core.Processors.Hunt.Enabled ? 0.2f : 0.6f, 1.0f, 
                                    E3Core.Processors.Hunt.Enabled ? "Yes" : "No");
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Active:");
                                imgui_TableNextColumn();
                                imgui_TextColored(E3Core.Processors.Hunt.Go ? 0.2f : 0.6f, E3Core.Processors.Hunt.Go ? 0.8f : 0.6f, E3Core.Processors.Hunt.Go ? 0.2f : 0.6f, 1.0f, 
                                    E3Core.Processors.Hunt.Go ? "Yes" : "Paused");
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Search Radius:");
                                imgui_TableNextColumn();
                                imgui_Text($"{E3Core.Processors.Hunt.Radius} units");
                                
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Z-Radius:");
                                imgui_TableNextColumn();
                                imgui_Text($"{E3Core.Processors.Hunt.ZRadius} units");

                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Next Scan In:");
                                imgui_TableNextColumn();
                                imgui_Text(string.Format("{0} ms", E3Core.Processors.Hunt.GetMsUntilNextScan()));

                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Next Nav In:");
                                imgui_TableNextColumn();
                                imgui_Text(string.Format("{0} ms", E3Core.Processors.Hunt.GetMsUntilNextNav()));

                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Next Pull In:");
                                imgui_TableNextColumn();
                                imgui_Text(string.Format("{0} ms", E3Core.Processors.Hunt.GetMsUntilNextPull()));

                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_Text("Nav Owned:");
                                imgui_TableNextColumn();
                                imgui_Text(E3Core.Processors.HuntStateMachine.IsNavigationOwned ? "Yes" : "No");
                                
                                int tid = E3Core.Processors.Hunt.TargetID;
                                if (tid > 0)
                                {
                                    imgui_TableNextRow();
                                    imgui_TableNextColumn();
                                    imgui_Text("Current Target:");
                                    imgui_TableNextColumn();
                                    var tname = E3Core.Processors.Hunt.TargetName ?? string.Empty;
                                    imgui_TextColored(0.8f, 0.3f, 0.3f, 1.0f, $"{tname} (ID: {tid})");
                                }
                                
                                imgui_EndTable();
                            }
                            
                            imgui_EndTabItem();
                        }
                        
                        imgui_EndTabBar();
                    }
                    
                    // Clean up tab styling (pop 3 colors)
                    imgui_PopStyleColor(3); // Tab background, active, hovered

                    // Bottom controls with enhanced styling
                    imgui_Separator();
                    imgui_PushStyleColor(21, 0.5f, 0.2f, 0.2f, 1.0f); // Red close button
                    if (imgui_RightAlignButton("Close"))
                    {
                        _huntWindowOpen = false;
                    }
                    imgui_PopStyleColor();
                    }
                    imgui_End();
                    PopE3Theme();
                }
            }
            
            // Render Hunt Debug window if toggled
            if (_imguiContextReady && _huntDebugWindowOpen)
            {
                int windowFlags = (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
                bool open = imgui_Begin(_e3HuntDebugWindow, windowFlags);
                if (open)
                {
                    imgui_TextColored(0.9f, 0.8f, 0.2f, 1.0f, "Hunt Debug");
                    imgui_SameLine();
                    if (imgui_Button("Clear")) { EnqueueUI(() => E3Core.Processors.Hunt.ClearDebugLog()); }
                    imgui_SameLine();
                    if (imgui_Button("Copy All"))
                    {
                        try
                        {
                            var lines = E3Core.Processors.Hunt.GetDebugLogSnapshot(_huntDebugLines);
                            var sb = new StringBuilder(lines.Count * 64);
                            long baseTs = lines.Count > 0 ? lines[0].ts : 0;
                            for (int i = 0; i < lines.Count; i++)
                            {
                                double dt = baseTs > 0 ? (lines[i].ts - baseTs) / 1000.0 : 0;
                                sb.AppendFormat("+{0:0.000}s  {1}\n", dt, lines[i].msg ?? string.Empty);
                            }
                            string text = sb.ToString();
                            if (TryCopyToClipboard(text))
                            {
                                _huntDebugCopyStatus = "Copied to clipboard.";
                            }
                            else
                            {
                                // Fallback: dump to temp file
                                string tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                    $"E3Next_HuntDebug_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                                System.IO.File.WriteAllText(tmp, text ?? string.Empty);
                                _huntDebugCopyStatus = $"Saved to {tmp}";
                            }
                        }
                        catch (Exception ex)
                        {
                            _huntDebugCopyStatus = $"Copy failed: {ex.Message}";
                        }
                    }
                    imgui_SameLine();
                    if (imgui_Button("Close")) { _huntDebugWindowOpen = false; }
                    imgui_Separator();

                    // Summary
                    var state = E3Core.Processors.HuntStateMachine.CurrentState.ToString();
                    var reason = E3Core.Processors.HuntStateMachine.StateReason ?? string.Empty;
                    var status = E3Core.Processors.Hunt.Status ?? string.Empty;
                    int tid = E3Core.Processors.Hunt.TargetID;
                    string tname = E3Core.Processors.Hunt.TargetName ?? string.Empty;
                    imgui_TextColored(0.75f, 0.85f, 0.95f, 1.0f, $"State: {state}  |  Reason: {reason}");
                    imgui_TextColored(0.70f, 0.90f, 0.70f, 1.0f, $"Status: {status}");
                    if (tid > 0) imgui_TextColored(0.90f, 0.60f, 0.60f, 1.0f, $"Target: {tname} (ID: {tid})");
                    else imgui_TextColored(0.60f, 0.60f, 0.60f, 1.0f, "Target: None");
                    if (!string.IsNullOrEmpty(_huntDebugCopyStatus))
                    {
                        imgui_TextColored(0.6f, 0.8f, 0.6f, 1.0f, _huntDebugCopyStatus);
                    }
                    imgui_Separator();

                    if (imgui_BeginChild("##hunt_debug_scroll", 0, 280, true))
                    {
                        var lines = E3Core.Processors.Hunt.GetDebugLogSnapshot(_huntDebugLines);
                        if (lines.Count == 0)
                        {
                            imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, "No debug entries yet.");
                        }
                        else
                        {
                            long baseTs = lines[0].ts;
                            for (int i = 0; i < lines.Count; i++)
                            {
                                double dt = (lines[i].ts - baseTs) / 1000.0;
                                imgui_Text(string.Format("+{0:0.000}s  {1}", dt, lines[i].msg ?? string.Empty));
                            }
                        }
                        imgui_EndChild();
                    }
                }
                imgui_End();
            }
            }
            catch (Exception ex)
            {
                E3.Log.Write($"OnUpdateImGui error: {ex.Message}", Logging.LogLevels.Error);
            }
        }

        public static void ToggleImGuiWindow()
        {
            if (!_imguiContextReady)
            {
                E3.Log.Write("ImGui not available. Make sure MQ2Mono is loaded: /plugin MQ2Mono", Logging.LogLevels.Info);
                return;
            }
            
            try
            {
                bool open = imgui_Begin_OpenFlagGet(_e3ImGuiWindow);
                imgui_Begin_OpenFlagSet(_e3ImGuiWindow, !open);
                E3.Log.Write($"E3 ImGui window {(!open ? "opened" : "closed")}", Logging.LogLevels.Info);
            }
            catch (Exception ex)
            {
                E3.Log.Write($"ImGui error: {ex.Message}", Logging.LogLevels.Error);
                _imguiContextReady = false; // Mark as failed for future calls
            }
        }

        private static void RefreshUIData()
        {
            try
            {
                // Only refresh once per call to prevent loops
                _uiDataNeedsRefresh = false;

                // Use cached, UI-safe accessors to avoid MQ queries during rendering
                _uiCurrentZone = E3Core.Processors.Hunt.GetCurrentZoneCached();
                _uiIgnoreList = E3Core.Processors.Hunt.GetIgnoreListSnapshotCached();
            }
            catch (Exception ex)
            {
                E3.Log.Write($"Error refreshing UI data: {ex.Message}", Logging.LogLevels.Error);
                _uiCurrentZone = "Unknown";
                _uiIgnoreList = new List<string>();
            }
        }

        public static void ToggleImGuiHuntWindow()
        {
            if (!_imguiContextReady)
            {
                E3.Log.Write("ImGui not available. Load MQ2Mono: /plugin MQ2Mono", Logging.LogLevels.Info);
                return;
            }
            _huntWindowOpen = !_huntWindowOpen;
            if (_huntWindowOpen)
            {
                // Start in minimized mode by default
                _huntWindowMinimized = true;
                // refresh text buffers from current values
                _huntRadiusBuf = E3Core.Processors.Hunt.Radius.ToString();
                _huntZRadiusBuf = E3Core.Processors.Hunt.ZRadius.ToString();
                _huntPullBuf = E3Core.Processors.Hunt.PullFilters ?? string.Empty;
                _huntIgnoreBuf = E3Core.Processors.Hunt.IgnoreFilters ?? string.Empty;
                _huntPullMethod = E3Core.Processors.Hunt.PullMethod ?? "None";
                _huntPullSpell = E3Core.Processors.Hunt.PullSpell ?? string.Empty;
                _huntPullItem = E3Core.Processors.Hunt.PullItem ?? string.Empty;
                _huntPullAA = E3Core.Processors.Hunt.PullAA ?? string.Empty;
                _huntPullDisc = E3Core.Processors.Hunt.PullDisc ?? string.Empty;
                
                // Mark UI data for refresh when window opens
                _uiDataNeedsRefresh = true;
            }
        }

        public static void ToggleImGuiHuntWindowMinimized()
        {
            if (!_imguiContextReady || !_huntWindowOpen)
            {
                return;
            }
            _huntWindowMinimized = !_huntWindowMinimized;
        }

        public static void ToggleImGuiHuntDebugWindow()
        {
            if (!_imguiContextReady)
            {
                E3.Log.Write("ImGui not available. Load MQ2Mono: /plugin MQ2Mono", Logging.LogLevels.Info);
                return;
            }
            _huntDebugWindowOpen = !_huntDebugWindowOpen;
        }

        private static void RefreshSettingsViewIfNeeded()
        {
            try
            {
                if (Core.StopWatch.ElapsedMilliseconds < _nextIniRefreshAtMs) return;
                _nextIniRefreshAtMs = Core.StopWatch.ElapsedMilliseconds + 1000; // 1s throttle

                string path = GetActiveSettingsPath();
                if (!string.Equals(path, _activeSettingsFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    _activeSettingsFilePath = path;
                    _activeSettingsFileLines = Array.Empty<string>();
                }
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    _activeSettingsFileLines = System.IO.File.ReadAllLines(path);
                }
                else
                {
                    _activeSettingsFileLines = new[] { "Settings file not found.", path ?? string.Empty };
                }
            }
            catch
            {
                _activeSettingsFileLines = new[] { "Error reading settings file." };
            }
        }

        private static string GetActiveSettingsPath()
        {
            switch (_activeSettingsTab)
            {
                case SettingsTab.General:
                    if (E3.GeneralSettings != null && !string.IsNullOrEmpty(E3.GeneralSettings._fileLastModifiedFileName))
                        return E3.GeneralSettings._fileLastModifiedFileName;
                    return E3Core.Settings.BaseSettings.GetSettingsFilePath("General Settings.ini");
                case SettingsTab.Advanced:
                    var adv = E3Core.Settings.BaseSettings.GetSettingsFilePath("Advanced Settings.ini");
                    if (!string.IsNullOrEmpty(E3Core.Settings.BaseSettings.CurrentSet)) adv = adv.Replace(".ini", "_" + E3Core.Settings.BaseSettings.CurrentSet + ".ini");
                    return adv;
                case SettingsTab.Character:
                default:
                    var currentPath = GetCurrentCharacterIniPath();
                    if (string.IsNullOrEmpty(_selectedCharIniPath))
                        _selectedCharIniPath = currentPath;
                    return _selectedCharIniPath;
            }
        }

        // Resolves a toon’s ini path by scanning known .ini files and preferring a match
        // that includes the server name if we have it.
        private static bool TryGetIniPathForToon(string toon, out string path)
        {
            path = null;
            if (string.IsNullOrWhiteSpace(toon)) return false;

            // Keep our list of ini files fresh
            ScanCharIniFilesIfNeeded();

            // Current character is easy
            if (!string.IsNullOrEmpty(E3.CurrentName) &&
                string.Equals(E3.CurrentName, toon, StringComparison.OrdinalIgnoreCase))
            {
                path = GetCurrentCharacterIniPath();
                return !string.IsNullOrEmpty(path);
            }

            if (_charIniFiles == null || _charIniFiles.Length == 0) return false;

            // Optional: prefer matches that also contain server in the filename
            _cfgAllPlayersServerByToon.TryGetValue(toon, out var serverHint);
            serverHint = serverHint ?? string.Empty;

            // Gather candidates: filename starts with "<Toon>_" or equals "<Toon>.ini"
            var candidates = new List<string>();
            foreach (var f in _charIniFiles)
            {
                var name = System.IO.Path.GetFileName(f);
                if (name.StartsWith(toon + "_", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals(toon + ".ini", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(f);
                }
            }

            if (candidates.Count == 0) return false;

            // Prefer one that mentions the server (common pattern: Toon_Server_Class.ini)
            if (!string.IsNullOrEmpty(serverHint))
            {
                var withServer = candidates.FirstOrDefault(f =>
                    System.IO.Path.GetFileName(f).IndexOf("_" + serverHint + "_", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!string.IsNullOrEmpty(withServer))
                {
                    path = withServer;
                    return true;
                }
            }

            // Fallback: first candidate
            path = candidates[0];
            return true;
        }

        // Reads, updates, and writes a single INI value for a toon.
        private static bool TrySaveIniValueForToon(string toon, string section, string key, string newValue, out string error)
        {
            error = null;
            if (!TryGetIniPathForToon(toon, out var iniPath))
            {
                error = $"Could not resolve ini path for '{toon}'.";
                return false;
            }

            try
            {
                var parser = E3Core.Utility.e3util.CreateIniParser();       // you already use this elsewhere
                var data = parser.ReadFile(iniPath);                         // IniParser.Model.IniData
                if (!data.Sections.ContainsSection(section))
                    data.Sections.AddSection(section);
                data[section][key] = newValue ?? string.Empty;               // simplest way to set a value
                parser.WriteFile(iniPath, data);                             // persist to disk

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // Reads a single INI value for a toon. Returns true if the toon/path exists and read succeeded.
        private static bool TryReadIniValueForToon(string toon, string section, string key, out string value)
        {
            value = string.Empty;
            try
            {
                if (!TryGetIniPathForToon(toon, out var iniPath))
                    return false;

                var parser = E3Core.Utility.e3util.CreateIniParser();
                var data = parser.ReadFile(iniPath);
                if (!data.Sections.ContainsSection(section))
                    return true; // file exists but section missing -> empty

                value = data[section][key] ?? string.Empty;
                return true;
            }
            catch
            {
                value = string.Empty;
                return false;
            }
        }


        private static string GetCurrentCharacterIniPath()
        {
            if (E3.CharacterSettings != null && !string.IsNullOrEmpty(E3.CharacterSettings._fileName))
                return E3.CharacterSettings._fileName;
            var name = E3.CurrentName ?? string.Empty;
            var server = E3.ServerName ?? string.Empty;
            var klass = E3.CurrentClass.ToString();
            return E3Core.Settings.BaseSettings.GetBoTFilePath(name, server, klass);
        }

        private static void ScanCharIniFilesIfNeeded()
        {
            try
            {
                if (Core.StopWatch.ElapsedMilliseconds < _nextIniFileScanAtMs) return;
                _nextIniFileScanAtMs = Core.StopWatch.ElapsedMilliseconds + 3000; // 3s throttle

                var curPath = GetCurrentCharacterIniPath();
                if (string.IsNullOrEmpty(curPath) || !File.Exists(curPath)) return;
                var dir = Path.GetDirectoryName(curPath);
                var server = E3.ServerName ?? string.Empty;
                var pattern = "*_*" + server + ".ini";
                var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
                if (files == null || files.Length == 0)
                    files = Directory.GetFiles(dir, "*.ini", SearchOption.TopDirectoryOnly);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                _charIniFiles = files;
            }
            catch { }
        }

        private static IniData GetActiveCharacterIniData()
        {
            var currentPath = GetCurrentCharacterIniPath();
            if (string.Equals(_selectedCharIniPath, currentPath, StringComparison.OrdinalIgnoreCase))
                return E3.CharacterSettings?.ParsedData;
            return _selectedCharIniParsedData;
        }

        private static void RenderCharacterIniSelector()
        {
            ScanCharIniFilesIfNeeded();

            var currentPath = GetCurrentCharacterIniPath();
            string currentDisplay = Path.GetFileName(currentPath);
            string selName = Path.GetFileName(_selectedCharIniPath ?? currentPath);
            if (string.IsNullOrEmpty(selName)) selName = currentDisplay;
                        
            bool opened = _comboAvailable && BeginComboSafe("Select Character", selName);
            if (opened)
            {
                if (!string.IsNullOrEmpty(currentPath))
                {
                    bool sel = string.Equals(_selectedCharIniPath, currentPath, StringComparison.OrdinalIgnoreCase);
                    if (imgui_Selectable($"Current: {currentDisplay}", sel))
                    {
                        _selectedCharIniPath = currentPath;
                        _selectedCharIniParsedData = null; // use live current
                        _nextIniRefreshAtMs = 0;
                        
                        // Trigger catalog reload for the selected peer
                        _cfg_CatalogsReady = false;
                        _cfgSpells.Clear();
                        _cfgAAs.Clear();
                        _cfgDiscs.Clear();
                        _cfgSkills.Clear();
                        _cfgItems.Clear();
                        _cfg_CatalogLoadRequested = true;
                        _cfg_CatalogStatus = "Queued catalog load...";
                    }
                }

                imgui_Separator();
                imgui_TextColored(0.85f, 0.65f, 0.40f, 1.0f, "Other Characters:");
                
                foreach (var f in _charIniFiles)
                {
                    if (string.Equals(f, currentPath, StringComparison.OrdinalIgnoreCase)) continue;
                    string name = Path.GetFileName(f);
                    bool sel = string.Equals(_selectedCharIniPath, f, StringComparison.OrdinalIgnoreCase);
                    if (imgui_Selectable($"{name}", sel))
                    {
                        try
                        {
                            var parser = E3Core.Utility.e3util.CreateIniParser();
                            var pd = parser.ReadFile(f);
                            _selectedCharIniPath = f;
                            _selectedCharIniParsedData = pd;
                            _selectedCharacterSection = string.Empty;
                            _charIniEdits.Clear();
                            _cfgAllPlayersSig = string.Empty; // force refresh
                            _nextIniRefreshAtMs = 0;
                            
                            // Trigger catalog reload for the selected peer
                            _cfg_CatalogsReady = false;
                            _cfgSpells.Clear();
                            _cfgAAs.Clear();
                            _cfgDiscs.Clear();
                            _cfgSkills.Clear();
                            _cfgItems.Clear();
                            _cfg_CatalogLoadRequested = true;
                            _cfg_CatalogStatus = "Queued catalog load...";
                        }
                        catch { }
                    }
                }
                imgui_EndCombo();
            }

            imgui_SameLine();
            
            // Save button with better styling
            if (imgui_Button(_cfg_Dirty ? "Save Changes*" : "Save Changes"))
            {
                SaveActiveIniData();
            }
            imgui_SameLine();
            imgui_TextColored(0.6f, 0.6f, 0.6f, 1.0f, _cfg_Dirty ? "Unsaved changes" : "All changes saved");
            
            imgui_Separator();
        }

        // Safe combo wrapper for older MQ2Mono
        private static bool BeginComboSafe(string label, string preview)
        {
            try
            {
                return imgui_BeginCombo(label, preview, 0);
            }
            catch
            {
                _comboAvailable = false;
                return false;
            }
        }
        private static void EndComboSafe()
        {
            try { imgui_EndCombo(); } catch { }
        }

        private static bool _cfg_Inited = false;
        private static void EnsureConfigEditorInit()
        {
            if (_cfg_Inited) return;
            _cfg_Inited = true;
            BuildConfigSectionOrder();
        }
        private static void BuildConfigSectionOrder()
        {
            _cfgSectionsOrdered.Clear();
            var pd = GetActiveCharacterIniData();
            if (pd?.Sections == null) return;

            // Class-prioritized defaults similar to e3config
            var defaults = new List<string>() { "Misc", "Assist Settings", "Nukes", "Debuffs", "DoTs on Assist", "DoTs on Command", "Heals", "Buffs", "Melee Abilities", "Burn", "CommandSets", "Pets", "Ifs" };
            try
            {
                var cls = E3.CurrentClass;
                if (cls.ToString().Equals("Bard", StringComparison.OrdinalIgnoreCase))
                {
                    defaults = new List<string>() { "Bard", "Melee Abilities", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
                }
                
                // Check if we're viewing a Bard config (local or remote) by looking for melody sections
                bool isBardConfig = pd.Sections.Any(s => s.SectionName.EndsWith(" Melody", StringComparison.OrdinalIgnoreCase));
                if (isBardConfig)
                {
                    // If current class is already bard, we've set defaults above, otherwise set bard defaults
                    if (!cls.ToString().Equals("Bard", StringComparison.OrdinalIgnoreCase))
                    {
                        defaults = new List<string>() { "Bard", "Melee Abilities", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
                    }
                    
                    // Add bard melody sections dynamically - they follow the pattern "MelodyName Melody"
                    var tempDefaults = new List<string>(defaults);
                    var melodySections = pd.Sections
                        .Where(s => s.SectionName.EndsWith(" Melody", StringComparison.OrdinalIgnoreCase))
                        .Select(s => s.SectionName)
                        .OrderBy(s => s)
                        .ToList();
                    
                    // Insert melody sections after "Bard" section
                    for (int i = 0; i < melodySections.Count; i++)
                    {
                        tempDefaults.Insert(1 + i, melodySections[i]);
                    }
                    defaults = tempDefaults;
                }
                else if (cls.ToString().Equals("Necromancer", StringComparison.OrdinalIgnoreCase))
                {
                    defaults = new List<string>() { "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs", "Assist Settings", "Buffs" };
                }
                else if (cls.ToString().Equals("Shadowknight", StringComparison.OrdinalIgnoreCase))
                {
                    defaults = new List<string>() { "Nukes", "Assist Settings", "Buffs", "DoTs on Assist", "DoTs on Command", "Debuffs", "Pets", "Burn", "CommandSets", "Ifs" };
                }
            }
            catch { }

            // Seed ordered list with defaults that exist in the INI
            foreach (var d in defaults)
            {
                if (pd.Sections.ContainsSection(d)) _cfgSectionsOrdered.Add(d);
            }
            // Append any remaining sections not included yet
            foreach (SectionData s in pd.Sections)
            {
                if (!_cfgSectionsOrdered.Contains(s.SectionName, StringComparer.OrdinalIgnoreCase))
                    _cfgSectionsOrdered.Add(s.SectionName);
            }

            if (_cfgSectionsOrdered.Count > 0)
            {
                if (string.IsNullOrEmpty(_cfgSelectedSection) || !_cfgSectionsOrdered.Contains(_cfgSelectedSection, StringComparer.OrdinalIgnoreCase))
                {
                    _cfgSelectedSection = _cfgSectionsOrdered[0];
                    var section = pd.Sections.GetSectionData(_cfgSelectedSection);
                    _cfgSelectedKey = section?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
                    _cfgSelectedValueIndex = -1;
                }
            }
        }

        // Ifs sample import helpers and modal
        private static string ResolveSampleIfsPath()
        {
            var dirs = new List<string>();
            try
            {
                string cfg = GetActiveSettingsPath();
                if (!string.IsNullOrEmpty(cfg))
                {
                    var dir = Path.GetDirectoryName(cfg);
                    if (!string.IsNullOrEmpty(dir)) dirs.Add(dir);
                }
            }
            catch { }
            try
            {
                string botIni = GetCurrentCharacterIniPath();
                if (!string.IsNullOrEmpty(botIni))
                {
                    var botDir = Path.GetDirectoryName(botIni);
                    if (!string.IsNullOrEmpty(botDir)) dirs.Add(botDir);
                }
            }
            catch { }
            try { dirs.Add(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty); } catch { }
            dirs.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? string.Empty, "E3Next"));
            dirs.Add(Directory.GetCurrentDirectory());
            dirs.Add(Path.Combine(Directory.GetCurrentDirectory(), "E3Next"));

            string[] names = new[] { "sample ifs", "sample ifs.txt", "Sample Ifs.txt", "sample_ifs.txt" };
            foreach (var d in dirs)
            {
                if (string.IsNullOrEmpty(d)) continue;
                foreach (var n in names)
                {
                    try
                    {
                        var p = Path.Combine(d, n);
                        if (File.Exists(p)) return p;
                    }
                    catch { }
                }
                try
                {
                    foreach (var f in Directory.EnumerateFiles(d, "*", SearchOption.TopDirectoryOnly))
                    {
                        string fn = Path.GetFileNameWithoutExtension(f) ?? string.Empty;
                        if (fn.Equals("sample ifs", StringComparison.OrdinalIgnoreCase)) return f;
                    }
                }
                catch { }
            }
            return string.Empty;
        }

        private static void LoadSampleIfsForModal()
        {
            _cfgIfSampleLines.Clear();
            _cfgIfSampleStatus = string.Empty;
            try
            {
                string sample = ResolveSampleIfsPath();
                if (string.IsNullOrEmpty(sample)) { _cfgIfSampleStatus = "Sample file not found."; return; }
                _cfgIfSampleStatus = "Loaded: " + Path.GetFileName(sample);
                int added = 0;
                foreach (var raw in File.ReadAllLines(sample))
                {
                    var line = (raw ?? string.Empty).Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("#") || line.StartsWith(";")) continue;
                    string key = string.Empty; string val = string.Empty;
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        key = (line.Substring(0, eq).Trim());
                        val = (line.Substring(eq + 1).Trim());
                    }
                    else
                    {
                        int colon = line.IndexOf(':');
                        int dash = line.IndexOf('-');
                        int pos = -1;
                        if (colon > 0) pos = colon; else if (dash > 0) pos = dash;
                        if (pos > 0)
                        {
                            key = line.Substring(0, pos).Trim();
                            val = line.Substring(pos + 1).Trim();
                        }
                        else
                        {
                            key = line;
                            val = string.Empty;
                        }
                    }
                    if (!string.IsNullOrEmpty(key))
                    {
                        _cfgIfSampleLines.Add(new KeyValuePair<string, string>(key, val));
                        added++;
                    }
                }
                if (added == 0) _cfgIfSampleStatus = "No entries found in sample file.";
                if (_cfgIfSampleLines.Count == 0) _cfgIfSampleStatus = "No entries found in sample file.";
            }
            catch (Exception ex)
            {
                _cfgIfSampleStatus = "Error reading sample IFs: " + (ex.Message ?? "error");
            }
        }

        private static bool AddIfToActiveIni(string key, string value)
        {
            try
            {
                var pd = GetActiveCharacterIniData();
                if (pd == null) return false;
                var section = pd.Sections.GetSectionData("Ifs");
                if (section == null)
                {
                    pd.Sections.AddSection("Ifs");
                    section = pd.Sections.GetSectionData("Ifs");
                }
                if (section == null) return false;
                string baseKey = key ?? string.Empty;
                if (string.IsNullOrWhiteSpace(baseKey)) return false;
                string unique = baseKey;
                int idx = 1;
                while (section.Keys.ContainsKey(unique)) { unique = baseKey + " (" + idx.ToString() + ")"; idx++; if (idx > 1000) break; }
                if (!section.Keys.ContainsKey(unique))
                {
                    section.Keys.AddKey(unique, value ?? string.Empty);
                    _cfg_Dirty = true;
                    _cfgSelectedSection = "Ifs";
                    _cfgSelectedKey = unique;
                    _cfgSelectedValueIndex = -1;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        private static void RenderIfsSampleModal()
        {
            imgui_Begin_OpenFlagSet("Sample If's", true);
            bool _open_ifs = imgui_Begin("Sample If's", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
            if (_open_ifs)
            {
                if (!string.IsNullOrEmpty(_cfgIfSampleStatus)) imgui_Text(_cfgIfSampleStatus);
                float h = 300f; float w = 640f;
                if (imgui_BeginChild("IfsSampleList", w, h, true))
                {
                    for (int i = 0; i < _cfgIfSampleLines.Count; i++)
                    {
                        var kv = _cfgIfSampleLines[i];
                        string display = string.IsNullOrEmpty(kv.Value) ? kv.Key : (kv.Key + " = " + kv.Value);
                        if (imgui_Selectable($"{display}##IF_{i}", false))
                        {
                            AddIfToActiveIni(kv.Key, kv.Value);
                        }
                    }
                }
                imgui_EndChild();
                imgui_SameLine();
                if (imgui_Button("Import All"))
                {
                    int cnt = 0;
                    for (int i = 0; i < _cfgIfSampleLines.Count; i++) { var kv = _cfgIfSampleLines[i]; if (AddIfToActiveIni(kv.Key, kv.Value)) cnt++; }
                    _cfgIfSampleStatus = cnt > 0 ? ($"Imported {cnt} If(s)") : "No new If's to import.";
                }
                imgui_SameLine();
                if (imgui_Button("Close")) _cfgShowIfSampleModal = false;
            }
            imgui_End();
            if (!_open_ifs) _cfgShowIfSampleModal = false;
        }

        private static void RenderAllPlayersView()
        {
            imgui_Text("All Players View");
            imgui_Separator();

            var pd = GetActiveCharacterIniData();
            if (pd == null || pd.Sections == null || string.IsNullOrEmpty(_cfgSelectedSection) || string.IsNullOrEmpty(_cfgSelectedKey))
            {
                imgui_Text("Select a section and key in the Config Editor first.");
                return;
            }

            

            imgui_Text($"Viewing: [{_cfgSelectedSection}] -> [{_cfgSelectedKey}]");
            imgui_SameLine();
            if (imgui_Button("Refresh")) _cfgAllPlayersRefreshRequested = true;

            imgui_Separator();

            if (imgui_BeginChild("AllPlayersList", 0, 0, true))
            {
                float outerW = Math.Max(720f, imgui_GetContentRegionAvailX()); // keep it roomy
                                                                               // Columns: Toon | Server | Remote | Value (editable) | Actions
                if (imgui_BeginTable("AllPlayersTable", 4, 0, outerW))
                {
                    imgui_TableSetupColumn("Toon", 0, 180f);
                    imgui_TableSetupColumn("Remote", 0, 80f);
                    imgui_TableSetupColumn("Value", 0, Math.Max(260f, outerW - (180f + 80f + 120f))); // leave room for Save
                    imgui_TableSetupColumn("Actions", 0, 100f);
                    imgui_TableHeadersRow();

                    lock (_cfgAllPlayersLock)
                    {
                        foreach (var row in _cfgAllPlayersRows)
                        {
                            string toon = row.Key ?? string.Empty;
                            string server = _cfgAllPlayersServerByToon.TryGetValue(toon, out var sv) ? (sv ?? string.Empty) : string.Empty;
                            bool isRemote = _cfgAllPlayersIsRemote.Contains(toon);

                            if (!_cfgAllPlayersEditBuf.ContainsKey(toon))
                                _cfgAllPlayersEditBuf[toon] = row.Value ?? string.Empty;

                            imgui_TableNextRow();

                            // Toon
                            imgui_TableNextColumn();
                            imgui_Text(toon);

                            // Remote
                            imgui_TableNextColumn();
                            imgui_Text(isRemote ? "Yes" : "No");

                            // Value (editable)
                            imgui_TableNextColumn();
                            string currentValue = _cfgAllPlayersEditBuf[toon];
                            bool isBool = string.Equals(currentValue, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(currentValue, "false", StringComparison.OrdinalIgnoreCase)
                                || string.Equals(currentValue, "on", StringComparison.OrdinalIgnoreCase) || string.Equals(currentValue, "off", StringComparison.OrdinalIgnoreCase);

                            if (isBool)
                            {
                                if (BeginComboSafe($"##value_{toon}", currentValue))
                                {
                                    if (imgui_Selectable("True", string.Equals(currentValue, "True", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        _cfgAllPlayersEditBuf[toon] = "True";
                                    }
                                    if (imgui_Selectable("False", string.Equals(currentValue, "False", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        _cfgAllPlayersEditBuf[toon] = "False";
                                    }
                                    if (imgui_Selectable("On", string.Equals(currentValue, "On", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        _cfgAllPlayersEditBuf[toon] = "On";
                                    }
                                    if (imgui_Selectable("Off", string.Equals(currentValue, "Off", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        _cfgAllPlayersEditBuf[toon] = "Off";
                                    }
                                    EndComboSafe();
                                }
                            }
                            else
                            {
                                string inputId = $"##edit_{toon}";
                                if (imgui_InputText(inputId, currentValue))
                                {
                                    _cfgAllPlayersEditBuf[toon] = imgui_InputText_Get(inputId) ?? string.Empty;
                                }
                            }

                            // Actions
                            imgui_TableNextColumn();
                            if (imgui_Button($"Save##{row.Key}"))
                            {
                                string newValue = _cfgAllPlayersEditBuf[row.Key] ?? string.Empty;

                                if (TrySaveIniValueForToon(row.Key, _cfgSelectedSection, _cfgSelectedKey, newValue, out var err))
                                {
                                    E3.MQ.Write($"Saved [{_cfgSelectedSection}] {_cfgSelectedKey} for {row.Key}.");
                                }
                                else
                                {
                                    E3.MQ.Write($"Save failed for {row.Key}: {err}");
                                }
                            }
                        }
                    }

                    imgui_EndTable();
                }
            }
            imgui_EndChild();
        }


        // Spell icon system state
        private static bool _cfg_IconSystemInitialized = false;
        
        // Catalogs and Add modal state
        private static bool _cfg_CatalogsReady = false;
        private static bool _cfg_CatalogLoadRequested = false;
        private static bool _cfg_CatalogLoading = false;
        private static string _cfg_CatalogStatus = string.Empty;
        private static string _cfg_CatalogSource = "Unknown"; // "Local", "Remote (ToonName)", or "Unknown"
        // Memorized gem data from catalog responses with spell icon support
        private static string[] _cfg_CatalogGems = new string[12]; // Gem data from catalog response
        private static int[] _cfg_CatalogGemIcons = new int[12]; // Spell icon indices for gems
        private static bool _cfg_GemsAvailable = false; // Whether we have gem data
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> _cfgItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
        private class E3Spell
        {
            public string Name;
            public string Category;
            public string Subcategory;
            public int Level;
            public string CastName;
            public string TargetType;
            public string SpellType;
            public int Mana;
            public double CastTime;
            public int Recast;
            public double Range;
            public string Description;
            public string ResistType;
            public int ResistAdj;
            public string CastType; // AA/Spell/Disc/Ability/Item/None
            public int SpellIcon = -1; // Spell icon index for display
            public override string ToString() => Name;
        }
        private static E3Spell _cfgCatalogInfoSpell = null;
        private static bool _cfgShowSpellInfoModal = false;
        private static E3Spell _cfgSpellInfoSpell = null;

        private enum AddType { Spells, AAs, Discs, Skills, Items }
        private static bool _cfgShowAddModal = false;
        private static AddType _cfgAddType = AddType.Spells;
        private static string _cfgAddCategory = string.Empty;
        private static string _cfgAddSubcategory = string.Empty;
        private static string _cfgAddFilter = string.Empty;

        // Food/Drink picker state
        private static bool _cfgShowFoodDrinkModal = false;
        private static string _cfgFoodDrinkKey = string.Empty; // "Food" or "Drink"
        private static string _cfgFoodDrinkStatus = string.Empty;
        private static List<string> _cfgFoodDrinkCandidates = new List<string>();
        private static bool _cfgFoodDrinkScanRequested = false;
        // Toon picker (Heals: Tank / Important Bot)
        private static bool _cfgShowToonPickerModal = false;
        private static string _cfgToonPickerStatus = string.Empty;
        private static List<string> _cfgToonCandidates = new List<string>();
        // Append If modal state
        private static bool _cfgShowIfAppendModal = false;
        private static int _cfgIfAppendRow = -1;
        private static List<string> _cfgIfAppendCandidates = new List<string>();
        private static string _cfgIfAppendStatus = string.Empty;
        // Ifs import (sample) modal state
        private static bool _cfgShowIfSampleModal = false;
        private static List<System.Collections.Generic.KeyValuePair<string, string>> _cfgIfSampleLines = new List<System.Collections.Generic.KeyValuePair<string, string>>();
        private static string _cfgIfSampleStatus = string.Empty;
        // Ifs: add-new helper input buffers
        private static string _cfgIfNewKey = string.Empty;
        private static string _cfgIfNewValue = string.Empty;
        // Remote fetch state (non-blocking)
        private static bool _cfgFoodDrinkPending = false;
        private static string _cfgFoodDrinkPendingToon = string.Empty;
        private static string _cfgFoodDrinkPendingType = string.Empty;
        private static long _cfgFoodDrinkTimeoutAt = 0;

        private static bool IsBooleanConfigKey(string key, KeyData kd)
        {
            if (kd == null) return false;
            // Heuristic: keys that are explicitly On/Off
            var v = (kd.Value ?? string.Empty).Trim();
            if (string.Equals(v, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "Off", StringComparison.OrdinalIgnoreCase))
                return true;
            // Common patterns
            if (key.IndexOf("Enable", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (key.IndexOf("Use ", StringComparison.OrdinalIgnoreCase) == 0) return true;
            return false;
        }

        // Attempt to derive an explicit set of allowed options from the key label, e.g.
        // "Assist Type (Melee/Ranged/Off)" => ["Melee","Ranged","Off"]
        private static bool TryGetKeyOptions(string keyLabel, out List<string> options)
        {
            options = null;
            if (string.IsNullOrEmpty(keyLabel)) return false;
            int i = keyLabel.IndexOf('(');
            int j = keyLabel.IndexOf(')');
            if (i < 0 || j <= i) return false;
            var inside = keyLabel.Substring(i + 1, j - i - 1).Trim();
            // Only treat as options if slash-delimited list exists
            if (inside.IndexOf('/') < 0) return false;
            var parts = inside.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(x => x.Trim())
                              .Where(x => !string.IsNullOrEmpty(x))
                              .ToList();
            if (parts.Count <= 1) return false;
            // Heuristic: ignore numeric unit hints like "(in milliseconds)" or "(Pct)" or "(1+)"
            bool looksNumericHint = parts.Any(p => p.Any(char.IsDigit)) || parts.Any(p => p.Equals("Pct", StringComparison.OrdinalIgnoreCase)) || parts.Any(p => p.IndexOf("millisecond", StringComparison.OrdinalIgnoreCase) >= 0);
            if (looksNumericHint) return false;
            options = parts;
            return true;
        }


        private static void RenderConfigEditor()
        {
            EnsureConfigEditorInit();

            var pd = GetActiveCharacterIniData();
            if (pd == null || pd.Sections == null)
            {
                imgui_TextColored(1.0f, 0.8f, 0.8f, 1.0f, "No character INI loaded.");
                return;
            }

            // Catalog status / loader with better styling
            if (!_cfg_CatalogsReady)
            {
                imgui_TextColored(1.0f, 0.9f, 0.3f, 1.0f, "Catalog Status");
                
                if (_cfg_CatalogLoading)
                {
                    imgui_Text(string.IsNullOrEmpty(_cfg_CatalogStatus) ? "Loading catalogs..." : _cfg_CatalogStatus);
                }
                else
                {
                    imgui_Text(string.IsNullOrEmpty(_cfg_CatalogStatus) ? "Catalogs not loaded" : _cfg_CatalogStatus);
                    imgui_SameLine();
                    if (imgui_Button("Load Catalogs"))
                    {
                        _cfg_CatalogLoadRequested = true;
                        _cfg_CatalogStatus = "Queued catalog load...";
                    }
                }
                imgui_Separator();
            }

            // Rebuild sections order when ini path changes
            string activeIniPath = GetActiveSettingsPath() ?? string.Empty;
            if (!string.Equals(activeIniPath, _cfg_LastIniPath, StringComparison.OrdinalIgnoreCase))
            {
                _cfg_LastIniPath = activeIniPath;
                _cfgSelectedSection = string.Empty;
                _cfgSelectedKey = string.Empty;
                _cfgSelectedValueIndex = -1;
                BuildConfigSectionOrder();
                // Auto-load catalogs on ini switch without blocking UI
                _cfg_CatalogsReady = false;
                _cfgSpells.Clear();
                _cfgAAs.Clear();
                _cfgDiscs.Clear();
                _cfgSkills.Clear();
                _cfgItems.Clear();
                _cfg_CatalogLoadRequested = true;
                _cfg_CatalogStatus = "Queued catalog load...";
            }

            // Use ImGui Table for responsive 3-column layout
            float availY = imgui_GetContentRegionAvailY();
            
            if (imgui_BeginTable("ConfigEditorTable", 3, 
                (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | 
                     ImGuiTableFlags.ImGuiTableFlags_Resizable | 
                     ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp), 
                imgui_GetContentRegionAvailX()))
            {
                // Set up columns with initial proportions
                imgui_TableSetupColumn("Sections & Keys", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0.35f);
                imgui_TableSetupColumn("Values", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0.35f);
                imgui_TableSetupColumn("Tools & Info", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 0.30f);
                imgui_TableHeadersRow();

                imgui_TableNextRow();
                
                // Column 1: Sections and Keys (with TreeNodes)
                if (imgui_TableNextColumn())
                {
                    if (imgui_BeginChild("SectionsTree", 0, Math.Max(200f, availY * 0.75f), false))
                    {
                        // Use a 1-column table with RowBg to get built-in alternating backgrounds
                        int tableFlags = (int)(ImGuiTableFlags.ImGuiTableFlags_RowBg | ImGuiTableFlags.ImGuiTableFlags_SizingStretchProp);
                        if (imgui_BeginTable("SectionsTreeTable", 1, tableFlags, imgui_GetContentRegionAvailX()))
                        {
                            imgui_TableSetupColumn("Section", 0, 0);
                            foreach (var sec in _cfgSectionsOrdered)
                            {
                                var secData = pd.Sections.GetSectionData(sec);
                                if (secData?.Keys == null) continue;

                                // Initialize expanded state defaults
                                if (!_cfgSectionExpanded.ContainsKey(sec))
                                {
                                    _cfgSectionExpanded[sec] = false;
                                }

                                // Section row
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                int treeFlags = (int)ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_SpanAvailWidth;
                                if (_cfgSectionExpanded[sec])
                                    treeFlags |= (int)ImGuiTreeNodeFlags.ImGuiTreeNodeFlags_DefaultOpen;
                                string sectionLabel = $"{sec}##section_{sec}";
                                bool nodeOpen = imgui_TreeNodeEx(sectionLabel, treeFlags);
                                if (imgui_IsItemHovered() && imgui_IsMouseClicked(0))
                                {
                                    _cfgSelectedSection = sec;
                                    _cfgSelectedKey = string.Empty;
                                }
                                _cfgSectionExpanded[sec] = nodeOpen;

                                if (nodeOpen)
                                {
                                    // Key rows
                                    var keys = secData.Keys.Select(k => k.KeyName).ToArray();
                                    foreach (var key in keys)
                                    {
                                        imgui_TableNextRow();
                                        imgui_TableNextColumn();
                                        bool keySelected = string.Equals(_cfgSelectedSection, sec, StringComparison.OrdinalIgnoreCase) &&
                                                           string.Equals(_cfgSelectedKey, key, StringComparison.OrdinalIgnoreCase);
                                        string keyLabel = $"  {key}"; // simple indent under section
                                        if (imgui_Selectable(keyLabel, keySelected))
                                        {
                                            _cfgSelectedSection = sec;
                                            _cfgSelectedKey = key;
                                            _cfgSelectedValueIndex = -1;
                                        }
                                    }
                                    imgui_TreePop();
                                }
                            }
                            imgui_EndTable();
                        }
                    }
                    imgui_EndChild();
                }
                
                // Column 2: Values
                if (imgui_TableNextColumn())
                {
                    var selectedSection = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
                    if (imgui_BeginChild("ValuesPanel", 0, Math.Max(200f, availY * 0.75f), false))
                    {
                        if (selectedSection == null)
                        {
                            imgui_Text("No section selected.");
                        }
                        else if (selectedSection.Keys == null || selectedSection.Keys.Count() == 0)
                        {
                            // Empty section: allow creating a new key directly here
                            imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, $"[{_cfgSelectedSection}] (empty)");
                            imgui_Separator();
                            imgui_Text("Create a new key:");
                            imgui_SameLine();
                            imgui_SetNextItemWidth(220f);
                            if (imgui_InputText("##new_key_name", _cfgNewKeyBuffer))
                            {
                                _cfgNewKeyBuffer = imgui_InputText_Get("##new_key_name") ?? string.Empty;
                            }
                            imgui_SameLine();
                            if (imgui_Button("Add Key"))
                            {
                                string newKey = (_cfgNewKeyBuffer ?? string.Empty).Trim();
                                if (newKey.Length > 0 && !selectedSection.Keys.ContainsKey(newKey))
                                {
                                    selectedSection.Keys.AddKey(newKey, string.Empty);
                                    _cfgSelectedKey = newKey;
                                    _cfgNewKeyBuffer = string.Empty;
                                    _cfgInlineEditIndex = -1;
                                    // On next frame the normal values editor will show for the new key
                                }
                            }
                        }
                        else if (string.IsNullOrEmpty(_cfgSelectedKey))
                        {
                            // Section has keys, but no key selected yet: keep values panel empty
                            imgui_Text("Select a configuration key from the left panel.");
                        }
                        else
                        {
                            RenderSelectedKeyValues(selectedSection);
                        }
                    }
                    imgui_EndChild();
                }
                
                // Column 3: Tools and Info
                if (imgui_TableNextColumn())
                {
                    var selectedSection = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
                    if (imgui_BeginChild("ToolsPanel", 0, Math.Max(200f, availY * 0.75f), false))
                    {
                        RenderConfigurationTools(selectedSection);
                    }
                    imgui_EndChild();
                }
                
                imgui_EndTable();
            }
            
            // Display memorized spells if available from catalog data (safe)
            RenderCatalogGemData();
        }
        
        
        // Safe gem display using catalog data (no TLO queries from UI thread)
        private static void RenderCatalogGemData()
        {
            if (!_cfg_GemsAvailable || _cfg_CatalogGems == null) return;
            
            try
            {
                imgui_Separator();
                
                // Show header with source info
                string sourceText = _cfg_CatalogSource.StartsWith("Remote") ? "Memorized Spells" : "Currently Memorized Spells";
                imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, sourceText);
                
                if (_cfg_CatalogSource.StartsWith("Remote"))
                {
                    imgui_SameLine();
                    imgui_TextColored(0.7f, 1.0f, 0.7f, 1.0f, $"({_cfg_CatalogSource.Replace("Remote (", "").Replace(")", "")})")
;
                }
                
                // Use horizontal table for gem display
                if (imgui_BeginTable("CatalogGems", 12, (int)(ImGuiTableFlags.ImGuiTableFlags_Borders | ImGuiTableFlags.ImGuiTableFlags_SizingStretchSame), imgui_GetContentRegionAvailX()))
                {
                    // Column headers
                    for (int gem = 1; gem <= 12; gem++)
                    {
                        imgui_TableSetupColumn($"Gem {gem}", (int)ImGuiTableColumnFlags.ImGuiTableColumnFlags_WidthStretch, 1.0f);
                    }
                    imgui_TableHeadersRow();
                    
                    imgui_TableNextRow();
                    
                    // Display gem data from catalog
                    for (int gem = 0; gem < 12; gem++)
                    {
                        imgui_TableNextColumn();
                        
                        string spellName = _cfg_CatalogGems[gem];
                        
                        if (!string.IsNullOrEmpty(spellName) && !spellName.Equals("NULL", StringComparison.OrdinalIgnoreCase) && !spellName.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
                        {
                            // Get spell icon index for this gem
                            int iconIndex = (_cfg_CatalogGemIcons != null && gem < _cfg_CatalogGemIcons.Length) ? _cfg_CatalogGemIcons[gem] : -1;
                            
                            // Display spell icon using native EQ texture
                            if (iconIndex >= 0)
                            {
                                imgui_DrawSpellIconByIconIndex(iconIndex, 40.0f);
                            }
                            
                            // Try to find spell info for color coding
                            if (_cfg_CatalogsReady)
                            {
                                var spellInfo = FindSpellItemAAByName(spellName);
                                if (spellInfo != null && spellInfo.Level > 0)
                                {
                                    // Color code by spell level
                                    float r = 0.9f, g = 0.9f, b = 0.9f;
                                    if (spellInfo.Level <= 10) { r = 0.7f; g = 1.0f; b = 0.7f; }
                                    else if (spellInfo.Level <= 50) { r = 0.9f; g = 0.9f; b = 0.7f; }
                                    else if (spellInfo.Level <= 85) { r = 1.0f; g = 0.8f; b = 0.6f; }
                                    else { r = 1.0f; g = 0.7f; b = 0.7f; }
                                    
                                    // Only show details in tooltip (no inline name)
                                    if (imgui_IsItemHovered())
                                    {
                                        imgui_BeginTooltip();
                                        imgui_Text($"Spell: {spellName}");
                                        imgui_Text($"Level: {spellInfo.Level}");
                                        if (iconIndex >= 0)
                                            imgui_Text($"Icon: {iconIndex}");
                                        if (!string.IsNullOrEmpty(spellInfo.Description))
                                        {
                                            imgui_Separator();
                                            imgui_TextWrapped(spellInfo.Description);
                                        }
                                        imgui_EndTooltip();
                                    }
                                    
                                }
                                else
                                {
                                    // Only show details in tooltip (no inline name)
                                    
                                    // Add basic hover tooltip
                                    if (imgui_IsItemHovered())
                                    {
                                        imgui_BeginTooltip();
                                        imgui_Text($"Spell: {spellName}");
                                        if (iconIndex >= 0)
                                            imgui_Text($"Icon: {iconIndex}");
                                        imgui_EndTooltip();
                                    }
                                }
                            }
                            else
                            {
                                imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, spellName);
                                
                                // Add basic hover tooltip
                                if (imgui_IsItemHovered())
                                {
                                    imgui_BeginTooltip();
                                    imgui_Text($"Spell: {spellName}");
                                    if (iconIndex >= 0)
                                        imgui_Text($"Icon: {iconIndex}");
                                    imgui_EndTooltip();
                                }
                            }
                        }
                        else if (spellName == "ERROR")
                        {
                            imgui_TextColored(0.8f, 0.4f, 0.4f, 1.0f, "(error)");
                        }
                        else
                        {
                            imgui_TextColored(0.5f, 0.5f, 0.5f, 1.0f, "(empty)");
                        }
                    }
                    
                    imgui_EndTable();
                }
            }
            catch (Exception ex)
            {
                imgui_TextColored(0.8f, 0.4f, 0.4f, 1.0f, $"Error displaying gems: {ex.Message}");
            }
        }
        
        // Helper method to render values for the selected key
        private static void RenderSelectedKeyValues(SectionData selectedSection)
        {
            var kd = selectedSection.Keys.GetKeyData(_cfgSelectedKey ?? string.Empty);
            string raw = kd?.Value ?? string.Empty;
            var parts = GetValues(kd);
            
            // Title row with better styling
            imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"[{_cfgSelectedSection}] {_cfgSelectedKey}");
            imgui_Separator();

            
            if (parts.Count == 0)
            {
                imgui_Text("(No values)");
                imgui_Separator();
            }

            // Enumerated options derived from key label e.g. "(Melee/Ranged/Off)"
            if (TryGetKeyOptions(_cfgSelectedKey, out var enumOpts))
            {
                string current = (raw ?? string.Empty).Trim();
                string display = current.Length == 0 ? "(unset)" : current;
                if (BeginComboSafe("Value", display))
                {
                    foreach (var opt in enumOpts)
                    {
                        bool sel = string.Equals(current, opt, StringComparison.OrdinalIgnoreCase);
                        if (imgui_Selectable(opt, sel))
                        {
                            string chosen = opt;
                            var pdAct = GetActiveCharacterIniData();
                            var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                            if (selSec != null && selSec.Keys.ContainsKey(_cfgSelectedKey))
                            {
                                var kdata = selSec.Keys.GetKeyData(_cfgSelectedKey);
                                if (kdata != null)
                                {
                                    WriteValues(kdata, new List<string> { chosen });
                                }
                            }
                        }
                    }
                    EndComboSafe();
                }
                imgui_Separator();
            }
            // Boolean fast toggle support → dropdown selector with better styling
            else if (IsBooleanConfigKey(_cfgSelectedKey, kd))
            {
                string current = (raw ?? string.Empty).Trim();
                // Derive allowed options from base E3 conventions
                List<string> baseOpts;
                var keyLabel = _cfgSelectedKey ?? string.Empty;
                bool mentionsOnOff = keyLabel.IndexOf("(On/Off)", StringComparison.OrdinalIgnoreCase) >= 0
                                     || keyLabel.IndexOf("On/Off", StringComparison.OrdinalIgnoreCase) >= 0
                                     || keyLabel.IndexOf("Enable", StringComparison.OrdinalIgnoreCase) >= 0
                                     || keyLabel.StartsWith("Use ", StringComparison.OrdinalIgnoreCase);
                if (string.Equals(current, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(current, "False", StringComparison.OrdinalIgnoreCase))
                {
                    // Preserve True/False style if that's what's used
                    baseOpts = new List<string> { "True", "False" };
                }
                else if (mentionsOnOff || string.Equals(current, "On", StringComparison.OrdinalIgnoreCase) || string.Equals(current, "Off", StringComparison.OrdinalIgnoreCase))
                {
                    baseOpts = new List<string> { "On", "Off" };
                }
                else
                {
                    // Default to On/Off per E3 defaults
                    baseOpts = new List<string> { "On", "Off" };
                }

                string display = current.Length == 0 ? "(unset)" : current;
                if (BeginComboSafe("Value", display))
                {
                    foreach (var opt in baseOpts)
                    {
                        bool sel = string.Equals(current, opt, StringComparison.OrdinalIgnoreCase);
                        if (imgui_Selectable(opt, sel))
                        {
                            string chosen = opt;
                            var pdAct = GetActiveCharacterIniData();
                            var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                            if (selSec != null && selSec.Keys.ContainsKey(_cfgSelectedKey))
                            {
                                var kdata = selSec.Keys.GetKeyData(_cfgSelectedKey);
                                if (kdata != null)
                                {
                                    WriteValues(kdata, new List<string> { chosen });
                                }
                            }
                        }
                    }
                    EndComboSafe();
                }
                imgui_Separator();
            }

            // Values list with improved styling
            bool listChanged = false;
            imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Configuration Values");
            
            for (int i = 0; i < parts.Count; i++)
            {
                string v = parts[i];
                bool editing = (_cfgInlineEditIndex == i);
                // Create a unique ID for this item that doesn't depend on its position in the list
                string itemUid = $"{_cfgSelectedSection}_{_cfgSelectedKey}_{i}_{(v ?? string.Empty).GetHashCode()}";
                
                if (!editing)
                {
                    // Row with better styling and alignment
                    imgui_Text($"{i + 1}.");
                    imgui_SameLine();
                    
                    // Edit button
                    if (imgui_Button($"Edit##edit_{itemUid}"))
                    {
                        _cfgInlineEditIndex = i;
                        _cfgInlineEditBuffer = v;
                    }
                    imgui_SameLine();
                    
                    // Delete button
                    if (imgui_Button($"Delete##delete_{itemUid}"))
                    {
                        int idx = i;
                        var pdAct = GetActiveCharacterIniData();
                        var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                        var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
                        if (key != null)
                        {
                            var vals = GetValues(key);
                            if (idx >= 0 && idx < vals.Count)
                            {
                                vals.RemoveAt(idx);
                                WriteValues(key, vals);
                                listChanged = true;
                            }
                        }
                        // continue to render items; parts refresh handled below
                    }
                    imgui_SameLine();
                    
                    // Make value selectable to show info in right panel
                    bool isSelected = (_cfgSelectedValueIndex == i);
                    if (imgui_Selectable($"{v}##select_{itemUid}", isSelected))
                    {
                        _cfgSelectedValueIndex = i;
                    }
                }
                else
                {
                    // Edit mode with better styling
                    imgui_Text($"* {i + 1}.");
                    imgui_SameLine();
                    
                    imgui_SetNextItemWidth(200f);
                    if (imgui_InputText($"##edit_text_{itemUid}", _cfgInlineEditBuffer))
                    {
                        _cfgInlineEditBuffer = imgui_InputText_Get($"##edit_text_{itemUid}");
                    }
                    imgui_SameLine();
                    
                    if (imgui_Button($"Save##save_{itemUid}"))
                    {
                        string newText = _cfgInlineEditBuffer ?? string.Empty;
                        int idx = i;
                        var pdAct = GetActiveCharacterIniData();
                        var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                        var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
                        if (key != null)
                        {
                            var vals = GetValues(key);
                            if (idx >= 0 && idx < vals.Count)
                            {
                                vals[idx] = newText;
                                WriteValues(key, vals);
                                listChanged = true;
                            }
                        }
                        _cfgInlineEditIndex = -1;
                        _cfgInlineEditBuffer = string.Empty;
                        // continue to render items; parts refresh handled below
                    }
                    imgui_SameLine();
                    
                    if (imgui_Button($"Cancel##cancel_{itemUid}"))
                    {
                        _cfgInlineEditIndex = -1;
                        _cfgInlineEditBuffer = string.Empty;
                    }
                }

                // If a change was made, we need to refresh the parts list for subsequent iterations
                if (listChanged)
                {
                    // Re-get the values after modification
                    var updatedKd = selectedSection.Keys.GetKeyData(_cfgSelectedKey ?? string.Empty);
                    parts = GetValues(updatedKd);
                    listChanged = false; // Reset the flag
                    // Clear selection since list changed
                    _cfgSelectedValueIndex = -1;
                    // Adjust the loop counter since we've removed an item
                    i--;
                }
            }
            
            // Handle adding a new manual entry (if we're in add mode)
            if (_cfgInlineEditIndex >= parts.Count)
            {
                imgui_Separator();
                imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Add New Value");
                
                imgui_Text($"+ {parts.Count + 1}.");
                imgui_SameLine();
                
                imgui_SetNextItemWidth(200f);
                if (imgui_InputText($"##add_new_manual", _cfgInlineEditBuffer))
                {
                    _cfgInlineEditBuffer = imgui_InputText_Get($"##add_new_manual");
                }
                imgui_SameLine();
                
                if (imgui_Button($"Add##add_manual"))
                {
                    string newText = _cfgInlineEditBuffer ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(newText))
                    {
                        var pdAct = GetActiveCharacterIniData();
                        var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                        var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
                        if (key != null)
                        {
                            var vals = GetValues(key);
                            vals.Add(newText.Trim());
                            WriteValues(key, vals);
                        }
                    }
                    _cfgInlineEditIndex = -1;
                    _cfgInlineEditBuffer = string.Empty;
                }
                imgui_SameLine();
                
                if (imgui_Button($"Cancel##cancel_manual"))
                {
                    _cfgInlineEditIndex = -1;
                    _cfgInlineEditBuffer = string.Empty;
                }
            }
            // Add new value button (only show when not editing)
            else if (!listChanged && _cfgInlineEditIndex == -1)
            {
                imgui_Separator();
                imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Add New Values");
                
                if (imgui_Button("Add Manual"))
                {
                    _cfgInlineEditIndex = parts.Count;
                    _cfgInlineEditBuffer = string.Empty;
                }
                imgui_SameLine();
                if (imgui_Button("Add From Catalog"))
                {
                    _cfgShowAddModal = true;
                }
            }
        }
        
        // Helper method to render configuration tools panel
        private static void RenderConfigurationTools(SectionData selectedSection)
        {
            if (selectedSection == null || string.IsNullOrEmpty(_cfgSelectedKey))
            {
                imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, "Select a configuration key to see available tools.");
                return;
            }
            
            imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Configuration Tools");
            imgui_Separator();
            
            
            // Special section buttons
            bool isHeals = string.Equals(_cfgSelectedSection, "Heals", StringComparison.OrdinalIgnoreCase);
            bool isTankKey = string.Equals(_cfgSelectedKey, "Tank", StringComparison.OrdinalIgnoreCase);
            bool isImpKey = string.Equals(_cfgSelectedKey, "Important Bot", StringComparison.OrdinalIgnoreCase);
            
            if (isHeals && (isTankKey || isImpKey))
            {
                if (imgui_Button("Pick Toons"))
                {
                    try
                    {
                        var keys = E3Core.Server.NetMQServer.SharedDataClient?.UsersConnectedTo?.Keys?.ToList() ?? new List<string>();
                        keys.Sort(StringComparer.OrdinalIgnoreCase);
                        _cfgToonCandidates = keys;
                        _cfgToonPickerStatus = keys.Count == 0 ? "No connected toons detected." : $"{keys.Count} connected.";
                    }
                    catch { _cfgToonCandidates = new List<string>(); _cfgToonPickerStatus = "Error loading toons."; }
                    _cfgShowToonPickerModal = true;
                }
            }
            
            if (_cfgSelectedKey.Equals("Food", StringComparison.OrdinalIgnoreCase) || _cfgSelectedKey.Equals("Drink", StringComparison.OrdinalIgnoreCase))
            {
                if (imgui_Button("Pick From Inventory"))
                {
                    // Reset scan state so results don't carry over between Food/Drink
                    _cfgFoodDrinkKey = _cfgSelectedKey; // "Food" or "Drink"
                    _cfgFoodDrinkStatus = string.Empty;
                    _cfgFoodDrinkCandidates.Clear();
                    _cfgFoodDrinkScanRequested = true; // auto-trigger scan for new kind
                    _cfgShowFoodDrinkModal = true;
                }
            }
            
            // Ifs sample import button (only when editing the Ifs section)
            if (string.Equals(_cfgSelectedSection, "Ifs", StringComparison.OrdinalIgnoreCase))
            {
                // Add New If (top-level key under [Ifs])
                imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Add New If");
                imgui_Text("Name:");
                imgui_SameLine();
                imgui_SetNextItemWidth(200f);
                if (imgui_InputText("##ifs_new_key", _cfgIfNewKey))
                {
                    _cfgIfNewKey = imgui_InputText_Get("##ifs_new_key") ?? string.Empty;
                }
                imgui_Text("Value:");
                imgui_SameLine();
                imgui_SetNextItemWidth(260f);
                if (imgui_InputText("##ifs_new_value", _cfgIfNewValue))
                {
                    _cfgIfNewValue = imgui_InputText_Get("##ifs_new_value") ?? string.Empty;
                }
                imgui_SameLine();
                if (imgui_Button("Add"))
                {
                    var key = (_cfgIfNewKey ?? string.Empty).Trim();
                    var val = _cfgIfNewValue ?? string.Empty;
                    if (key.Length > 0)
                    {
                        if (AddIfToActiveIni(key, val))
                        {
                            _cfgIfNewKey = string.Empty;
                            _cfgIfNewValue = string.Empty;
                        }
                    }
                }
                imgui_Separator();

                if (imgui_Button("Sample If's"))
                {
                    try { LoadSampleIfsForModal(); _cfgShowIfSampleModal = true; }
                    catch (Exception ex) { _cfgIfSampleStatus = "Load failed: " + (ex.Message ?? "error"); _cfgShowIfSampleModal = true; }
                }
            }
            
            // Add our HealPct+Gem helper for heal-related keys
            if (IsHealingKey(_cfgSelectedSection, _cfgSelectedKey))
            {
                imgui_Separator();
                imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Heals Helper: Add with HealPct + Gem");
                RenderHealPctSuffixInput("tools", selectedSection, 220f);
            }
            
            imgui_Separator();
            
            // Display selected value information
            if (_cfgSelectedValueIndex >= 0)
            {
                var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                var values = GetValues(kd);
                if (_cfgSelectedValueIndex < values.Count)
                {
                    string selectedValue = values[_cfgSelectedValueIndex];
                    imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Selected Value Info");
                    imgui_Text($"Value: {selectedValue}");
                    
                    // Try to find spell/item/AA information
                    if (_cfg_CatalogsReady)
                    {
                        var spellInfo = FindSpellItemAAByName(selectedValue);
                        if (spellInfo != null)
                        {
                            imgui_Separator();
                            
                            // Display spell/item/AA details using a compact table
                            if (imgui_BeginTable("SelectedValueInfo", 2, 0, imgui_GetContentRegionAvailX()))
                            {
                                imgui_TableSetupColumn("Property", 0, 80f);
                                imgui_TableSetupColumn("Value", 0, imgui_GetContentRegionAvailX() - 100f);
                                
                                // Type
                                imgui_TableNextRow();
                                imgui_TableNextColumn();
                                imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Type:");
                                imgui_TableNextColumn();
                                imgui_Text(spellInfo.CastType ?? "Unknown");
                                
                                // Level (if applicable)
                                if (spellInfo.Level > 0)
                                {
                                    imgui_TableNextRow();
                                    imgui_TableNextColumn();
                                    imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Level:");
                                    imgui_TableNextColumn();
                                    imgui_Text(spellInfo.Level.ToString());
                                }
                                
                                // Mana (if applicable)
                                if (spellInfo.Mana > 0)
                                {
                                    imgui_TableNextRow();
                                    imgui_TableNextColumn();
                                    imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Mana:");
                                    imgui_TableNextColumn();
                                    imgui_Text(spellInfo.Mana.ToString());
                                }
                                
                                // Cast Time (if applicable)
                                if (spellInfo.CastTime > 0)
                                {
                                    imgui_TableNextRow();
                                    imgui_TableNextColumn();
                                    imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Cast Time:");
                                    imgui_TableNextColumn();
                                    imgui_Text($"{spellInfo.CastTime:0.00}s");
                                }
                                
                                // Target (if applicable)
                                if (!string.IsNullOrEmpty(spellInfo.TargetType))
                                {
                                    imgui_TableNextRow();
                                    imgui_TableNextColumn();
                                    imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Target:");
                                    imgui_TableNextColumn();
                                    imgui_Text(spellInfo.TargetType);
                                }
                                
                                imgui_EndTable();
                            }
                            
                            // Description (if available)
                            if (!string.IsNullOrEmpty(spellInfo.Description))
                            {
                                imgui_Separator();
                                imgui_TextColored(0.75f, 0.85f, 1.0f, 1.0f, "Description:");
                                imgui_Text(spellInfo.Description);
                            }
                        }
                        else
                        {
                            imgui_TextColored(0.8f, 0.8f, 0.6f, 1.0f, "(No catalog info found)");
                        }
                    }
                    else
                    {
                        imgui_TextColored(0.8f, 0.8f, 0.6f, 1.0f, "(Catalogs not loaded)");
                    }
                    
                    imgui_Separator();
                }
            }
            
            imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Configuration Info");
            imgui_Text($"Section: {_cfgSelectedSection}");
            imgui_Text($"Key: {_cfgSelectedKey}");
            
            // Show modals
            if (_cfgShowAddModal)
            {
                RenderAddFromCatalogModal(GetActiveCharacterIniData(), selectedSection);
            }
            if (_cfgShowFoodDrinkModal)
            {
                RenderFoodDrinkPicker(selectedSection);
            }
            if (_cfgShowToonPickerModal)
            {
                RenderToonPickerModal(selectedSection);
            }
            if (_cfgShowSpellInfoModal)
            {
                RenderSpellInfoModal();
            }
            if (_cfgShowIfAppendModal)
            {
                RenderIfAppendModal(selectedSection);
            }
            if (_cfgShowIfSampleModal)
            {
                RenderIfsSampleModal();
            }
        }
        
        // Helper to determine if a key is healing-related
        private static bool IsHealingKey(string section, string key)
        {
            if (string.IsNullOrEmpty(section) || string.IsNullOrEmpty(key)) return false;
            
            var healingSections = new[] { "Heals", "Tank", "Important Bot" };
            return healingSections.Any(s => string.Equals(section, s, StringComparison.OrdinalIgnoreCase));
        }

        // Save out active ini data (current or selected)
        private static void SaveActiveIniData()
        {
            try
            {
                string currentPath = GetCurrentCharacterIniPath();
                string selectedPath = GetActiveSettingsPath();
                var pd = GetActiveCharacterIniData();
                if (string.IsNullOrEmpty(selectedPath) || pd == null) return;

                var parser = E3Core.Utility.e3util.CreateIniParser();
                parser.WriteFile(selectedPath, pd);
                _cfg_Dirty = false;
                _nextIniRefreshAtMs = 0;
                E3.MQ.Write($"Saved changes to {Path.GetFileName(selectedPath)}");
            }
            catch (Exception ex)
            {
                E3.MQ.Write($"Failed to save: {ex.Message}");
            }
        }

        // Background worker tick invoked from E3.Process(): handle catalog loads and icon system
        public static void ProcessBackgroundWork()
        {
            // Initialize spell icon system if not already done
            if (!_cfg_IconSystemInitialized)
            {
                try
                {
                    string eqPath = E3.MQ.Query<string>("${EverQuest.Path}");
                    if (!string.IsNullOrEmpty(eqPath))
                    {
                        E3Next.UI.SpellIconManager.Initialize(eqPath);
                        _cfg_IconSystemInitialized = true;
                        E3Core.Utility.e3util._log.Write("Spell icon system initialized.");
                    }
                }
                catch (Exception ex)
                {
                    E3Core.Utility.e3util._log.Write($"Failed to initialize spell icon system: {ex.Message}");
                }
            }
            
            if (_cfg_CatalogLoadRequested && !_cfg_CatalogLoading)
            {
                _cfg_CatalogLoading = true;
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        // Always fetch via RouterServer, same as e3config
                        string targetToon = GetSelectedIniOwnerName();
                        bool isLocal = string.IsNullOrEmpty(targetToon) || targetToon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);
                        SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
                            mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
                            mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
                            mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(),
                            mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
                        
                        // Always try to fetch from peer first if it's not the current toon
                        if (!isLocal)
                        {
                            _cfg_CatalogStatus = $"Loading catalogs from {targetToon}...";
                            bool peerSuccess = true;
                            
                            peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Spells", out var ps);
                            if (peerSuccess) mapSpells = OrganizeCatalog(ps); else mapSpells = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
                            
                            peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "AAs", out var pa);
                            if (peerSuccess) mapAAs = OrganizeCatalog(pa); else mapAAs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
                            
                            peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Discs", out var pd);
                            if (peerSuccess) mapDiscs = OrganizeCatalog(pd); else mapDiscs = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
                            
                            peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Skills", out var pk);
                            if (peerSuccess) mapSkills = OrganizeSkillsCatalog(pk); else mapSkills = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
                            
                            peerSuccess &= TryFetchPeerSpellDataListPub(targetToon, "Items", out var pi);
                            if (peerSuccess) mapItems = OrganizeItemsCatalog(pi); else mapItems = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>();
                            
                            // Also try to fetch gem data
                            if (peerSuccess && TryFetchPeerGemData(targetToon, out var gemData))
                            {
                                _cfg_CatalogGems = gemData;
                                _cfg_GemsAvailable = true;
                            }
                            else
                            {
                                _cfg_GemsAvailable = false;
                            }
                            
                            // If any peer fetch failed, fallback to local
                            if (!peerSuccess)
                            {
                                _cfg_CatalogStatus = "Peer catalog fetch failed; using local.";
                                _cfg_CatalogSource = "Local (fallback)";
                                isLocal = true;
                            }
                            else
                            {
                                _cfg_CatalogSource = $"Remote ({targetToon})";
                            }
                        }
                        
                        if (isLocal)
                        {
                            _cfg_CatalogStatus = "Loading catalogs (local)...";
                            _cfg_CatalogSource = "Local";
                            mapSpells = OrganizeCatalog(FetchSpellDataList("${E3.SpellBook.ListAll}"));
                            mapAAs = OrganizeCatalog(FetchSpellDataList("${E3.AA.ListAll}"));
                            mapDiscs = OrganizeCatalog(FetchSpellDataList("${E3.Discs.ListAll}"));
                            mapSkills = OrganizeSkillsCatalog(FetchSpellDataList("${E3.Skills.ListAll}"));
                            mapItems = OrganizeItemsCatalog(FetchSpellDataList("${E3.ItemsWithSpells.ListAll}"));
                            
                            // Also collect local gem data with spell icon indices
                            try
                            {
                                var localGems = new string[12];
                                var localGemIcons = new int[12];
                                
                                for (int gem = 1; gem <= 12; gem++)
                                {
                                    try
                                    {
                                        string spellName = E3.MQ.Query<string>($"${{Me.Gem[{gem}]}}");
                                        localGems[gem - 1] = spellName ?? "NULL";
                                        
                                        // Get spell icon index if we have a valid spell
                                        if (!string.IsNullOrEmpty(spellName) && !spellName.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                                        {
                                            localGemIcons[gem - 1] = GetLocalSpellIconIndex(spellName);
                                        }
                                        else
                                        {
                                            localGemIcons[gem - 1] = -1;
                                        }
                                    }
                                    catch
                                    {
                                        localGems[gem - 1] = "ERROR";
                                        localGemIcons[gem - 1] = -1;
                                    }
                                }
                                _cfg_CatalogGems = localGems;
                                _cfg_CatalogGemIcons = localGemIcons;
                                _cfg_GemsAvailable = true;
                            }
                            catch
                            {
                                _cfg_GemsAvailable = false;
                            }
                        }

                        // Publish atomically
                        _cfgSpells = mapSpells;
                        _cfgAAs = mapAAs;
                        _cfgDiscs = mapDiscs;
                        _cfgSkills = mapSkills;
                        _cfgItems = mapItems;
                        _cfg_CatalogsReady = true;
                        _cfg_CatalogStatus = "Catalogs loaded.";
                    }
                    catch (Exception ex)
                    {
                        _cfg_CatalogStatus = "Catalog load failed: " + (ex.Message ?? "error");
                    }
                    finally
                    {
                        _cfg_CatalogLoadRequested = false;
                        _cfg_CatalogLoading = false;
                    }
                });
            }
            // Food/Drink inventory scan (local or remote peer) — non-blocking
            if (_cfgFoodDrinkScanRequested && !_cfgFoodDrinkPending)
            {
                _cfgFoodDrinkScanRequested = false;
                try
                {
                    string owner = GetSelectedIniOwnerName();
                    bool isLocal = string.IsNullOrEmpty(owner) || owner.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase);
                    if (!isLocal)
                    {
                        // Start remote request and mark pending; actual receive handled below
                        E3Core.Server.PubServer.AddTopicMessage($"InvReq-{owner}", _cfgFoodDrinkKey);
                        _cfgFoodDrinkPending = true;
                        _cfgFoodDrinkPendingToon = owner;
                        _cfgFoodDrinkPendingType = _cfgFoodDrinkKey;
                        _cfgFoodDrinkTimeoutAt = Core.StopWatch.ElapsedMilliseconds + 2000;
                        _cfgFoodDrinkStatus = $"Scanning {_cfgFoodDrinkKey} on {owner}...";
                    }
                    else
                    {
                        var list = ScanInventoryForType(_cfgFoodDrinkKey);
                        _cfgFoodDrinkCandidates = list ?? new List<string>();
                        _cfgFoodDrinkStatus = _cfgFoodDrinkCandidates.Count == 0 ? "No matches found in inventory." : $"Found {_cfgFoodDrinkCandidates.Count} items.";
                    }
                }
                catch (Exception ex)
                {
                    _cfgFoodDrinkStatus = "Scan failed: " + (ex.Message ?? "error");
                }
            }
            // Remote response polling — checked each tick without blocking
            if (_cfgFoodDrinkPending)
            {
                try
                {
                    string toon = _cfgFoodDrinkPendingToon;
                    string type = _cfgFoodDrinkPendingType;
                    string topic = $"InvResp-{E3.CurrentName}-{type}";
                    // Prefer remote publisher bucket
                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
                        && topics.TryGetValue(topic, out var entry))
                    {
                        string payload = entry.Data ?? string.Empty;
                        int first = payload.IndexOf(':');
                        int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
                        string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
                        try
                        {
                            var bytes = Convert.FromBase64String(b64);
                            var joined = Encoding.UTF8.GetString(bytes);
                            _cfgFoodDrinkCandidates = (joined ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => s.Length > 0)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                                .ToList();
                        }
                        catch
                        {
                            _cfgFoodDrinkCandidates = new List<string>();
                        }
                        _cfgFoodDrinkStatus = _cfgFoodDrinkCandidates.Count == 0 ? $"No {type} found on {toon}." : $"Found {_cfgFoodDrinkCandidates.Count} items on {toon}.";
                        _cfgFoodDrinkPending = false;
                    }
                    else if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
                             && topics2.TryGetValue(topic, out var entry2))
                    {
                        string payload = entry2.Data ?? string.Empty;
                        int first = payload.IndexOf(':');
                        int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
                        string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
                        try
                        {
                            var bytes = Convert.FromBase64String(b64);
                            var joined = Encoding.UTF8.GetString(bytes);
                            _cfgFoodDrinkCandidates = (joined ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => s.Length > 0)
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                                .ToList();
                        }
                        catch
                        {
                            _cfgFoodDrinkCandidates = new List<string>();
                        }
                        _cfgFoodDrinkStatus = _cfgFoodDrinkCandidates.Count == 0 ? $"No {type} found on {toon}." : $"Found {_cfgFoodDrinkCandidates.Count} items on {toon}.";
                        _cfgFoodDrinkPending = false;
                    }
                    else if (Core.StopWatch.ElapsedMilliseconds >= _cfgFoodDrinkTimeoutAt)
                    {
                        _cfgFoodDrinkStatus = $"Remote {type} scan timed out for {toon}.";
                        _cfgFoodDrinkCandidates = new List<string>();
                        _cfgFoodDrinkPending = false;
                    }
                }
                catch
                {
                    _cfgFoodDrinkStatus = "Remote scan error.";
                    _cfgFoodDrinkCandidates = new List<string>();
                    _cfgFoodDrinkPending = false;
                }
            }

            if (_cfgAllPlayersRefreshRequested && !_cfgAllPlayersRefreshing)
            {
                _cfgAllPlayersRefreshing = true;
                _cfgAllPlayersReqSection = _cfgSelectedSection;
                _cfgAllPlayersReqKey = _cfgSelectedKey;

                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _cfgAllPlayersStatus = "Refreshing...";
                        var connectedToons = E3Core.Server.NetMQServer.SharedDataClient?.UsersConnectedTo?.Keys?.ToList() ?? new List<string>();
                        if (!connectedToons.Contains(E3.CurrentName, StringComparer.OrdinalIgnoreCase))
                        {
                            connectedToons.Add(E3.CurrentName);
                        }

                        var newRows = new List<KeyValuePair<string, string>>();
                        string section = _cfgAllPlayersReqSection;
                        string key = _cfgAllPlayersReqKey;

                        foreach (var toon in connectedToons)
                        {
                            string value = string.Empty;

                            // First, try reading directly from the toon's local INI (if present on this machine)
                            bool gotLocal = TryReadIniValueForToon(toon, section, key, out value);

                            // If we didn't get a value locally and it's a remote toon, request from peer
                            if (!gotLocal && !toon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                            {
                                string requestTopic = $"ConfigValueReq-{toon}";
                                string payload = $"{section}:{key}";
                                E3Core.Server.PubServer.AddTopicMessage(requestTopic, payload);

                                string responseTopic = $"ConfigValueResp-{E3.CurrentName}-{section}:{key}";
                                long end = Core.StopWatch.ElapsedMilliseconds + 2000;
                                bool found = false;
                                while (Core.StopWatch.ElapsedMilliseconds < end)
                                {
                                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics) &&
                                        topics.TryGetValue(responseTopic, out var entry))
                                    {
                                        value = entry.Data;
                                        found = true;
                                        break;
                                    }
                                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2) &&
                                        topics2.TryGetValue(responseTopic, out var entry2))
                                    {
                                        value = entry2.Data;
                                        found = true;
                                        break;
                                    }
                                    System.Threading.Thread.Sleep(25);
                                }
                                if (!found) value = "<timeout>";
                            }

                            newRows.Add(new KeyValuePair<string, string>(toon, value));
                        }

                        lock (_cfgAllPlayersLock)
                        {
                            _cfgAllPlayersRows = newRows;
                        }
                        _cfgAllPlayersLastUpdatedAt = Core.StopWatch.ElapsedMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        _cfgAllPlayersStatus = "Refresh failed: " + ex.Message;
                    }
                    finally
                    {
                        _cfgAllPlayersRefreshing = false;
                        _cfgAllPlayersRefreshRequested = false;
                    }
                });
            }
        }

        // Fetch SpellDataList via local RouterServer (same process), like e3config
        private static Google.Protobuf.Collections.RepeatedField<SpellData> FetchSpellDataList(string query)
        {
            try
            {
                using (var sock = new NetMQ.Sockets.DealerSocket())
                {
                    sock.Options.Identity = Guid.NewGuid().ToByteArray();
                    sock.Options.SendHighWatermark = 50000;
                    sock.Options.ReceiveHighWatermark = 50000;
                    sock.Connect("tcp://127.0.0.1:" + E3Core.Server.NetMQServer.RouterPort.ToString());

                    // Empty frame
                    var msg = new NetMQ.Msg();
                    msg.InitEmpty(); sock.TrySend(ref msg, new TimeSpan(0, 0, 5), true); msg.Close();

                    // Payload frame: 4-byte cmd, 4-byte len, bytes(query)
                    var data = Encoding.Default.GetBytes(query ?? string.Empty);
                    msg.InitPool(data.Length + 8);
                    unsafe
                    {
                        fixed (byte* dest = msg.Data)
                        fixed (byte* src = data)
                        {
                            byte* p = dest;
                            *(int*)p = 1; p += 4;
                            *(int*)p = data.Length; p += 4;
                            System.Buffer.MemoryCopy(src, p, data.Length, data.Length);
                        }
                    }
                    sock.TrySend(ref msg, new TimeSpan(0, 0, 5), false); msg.Close();

                    // Receive empty frame
                    msg.InitEmpty(); sock.TryReceive(ref msg, new TimeSpan(0, 0, 5)); msg.Close();
                    // Receive data frame
                    msg.InitEmpty(); sock.TryReceive(ref msg, new TimeSpan(0, 0, 10));
                    byte[] bytes = new byte[msg.Size];
                    Buffer.BlockCopy(msg.Data, 0, bytes, 0, msg.Size);
                    msg.Close();

                    var list = SpellDataList.Parser.ParseFrom(bytes);
                    return list.Data;
                }
            }
            catch
            {
                return new Google.Protobuf.Collections.RepeatedField<SpellData>();
            }
        }

        // Router-based direct fetch (kept for future), currently unused
        private static bool TryFetchPeerSpellDataList(string toon, string query, out Google.Protobuf.Collections.RepeatedField<SpellData> data)
        {
            data = new Google.Protobuf.Collections.RepeatedField<SpellData>();
            return false;
        }

        // Fetch gem data from peer catalog response (now includes spell icon indices)
        private static bool TryFetchPeerGemData(string toon, out string[] gemData)
        {
            gemData = new string[12];
            try
            {
                if (string.IsNullOrEmpty(toon)) return false;
                
                string topic = $"CatalogResp-{E3.CurrentName}-Gems";
                // Poll SharedDataClient.TopicUpdates for gem data
                long end = Core.StopWatch.ElapsedMilliseconds + 2000; // 2 second timeout
                while (Core.StopWatch.ElapsedMilliseconds < end)
                {
                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
                        && topics.TryGetValue(topic, out var entry))
                    {
                        string payload = entry.Data;
                        ParseGemDataWithIcons(payload, out gemData, out _cfg_CatalogGemIcons);
                        return true;
                    }
                    
                    // Also check if data came back under current name
                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
                        && topics2.TryGetValue(topic, out var entry2))
                    {
                        string payload = entry2.Data;
                        ParseGemDataWithIcons(payload, out gemData, out _cfg_CatalogGemIcons);
                        return true;
                    }
                    
                    System.Threading.Thread.Sleep(25);
                }
            }
            catch { }
            
            // Fill with ERROR if failed
            for (int i = 0; i < 12; i++)
            {
                gemData[i] = "ERROR";
                _cfg_CatalogGemIcons[i] = -1;
            }
            return false;
        }
        
        // Helper method to parse gem data with icon indices from pipe-separated format
        private static void ParseGemDataWithIcons(string payload, out string[] gemNames, out int[] gemIcons)
        {
            gemNames = new string[12];
            gemIcons = new int[12];
            
            try
            {
                // Parse pipe-separated gem data: "SpellName:IconIndex|SpellName:IconIndex|..."
                var gems = payload.Split('|');
                int count = Math.Min(gems.Length, 12);
                
                for (int i = 0; i < count; i++)
                {
                    string gemEntry = gems[i] ?? "NULL:-1";
                    string[] parts = gemEntry.Split(':');
                    
                    if (parts.Length >= 2)
                    {
                        gemNames[i] = parts[0] ?? "NULL";
                        if (int.TryParse(parts[1], out int iconIndex))
                        {
                            gemIcons[i] = iconIndex;
                        }
                        else
                        {
                            gemIcons[i] = -1;
                        }
                    }
                    else
                    {
                        // Fallback for old format without icons
                        gemNames[i] = gemEntry ?? "NULL";
                        gemIcons[i] = -1;
                    }
                }
                
                // Fill remaining slots if needed
                for (int i = count; i < 12; i++)
                {
                    gemNames[i] = "NULL";
                    gemIcons[i] = -1;
                }
            }
            catch
            {
                // Error case - fill with defaults
                for (int i = 0; i < 12; i++)
                {
                    gemNames[i] = "ERROR";
                    gemIcons[i] = -1;
                }
            }
        }
        
        // Helper method to get spell icon index for local spells
        private static int GetLocalSpellIconIndex(string spellName)
        {
            if (string.IsNullOrEmpty(spellName)) return -1;
            
            try
            {
                // Use the catalog lookups if they're available
                var spellInfo = FindSpellItemAAByName(spellName);
                if (spellInfo != null && spellInfo.SpellIcon >= 0)
                {
                    return spellInfo.SpellIcon;
                }
                
                // Fallback: Query MQ directly for spell icon
                int iconIndex = E3.MQ.Query<int>($"${{Spell[{spellName}].SpellIcon}}");
                return iconIndex > 0 ? iconIndex : -1;
            }
            catch
            {
                return -1;
            }
        }
        
        // PubSub relay approach: request peer to publish SpellDataList as base64 on response topic
        private static bool TryFetchPeerSpellDataListPub(string toon, string listKey, out Google.Protobuf.Collections.RepeatedField<SpellData> data)
        {
            data = new Google.Protobuf.Collections.RepeatedField<SpellData>();
            try
            {
                if (string.IsNullOrEmpty(toon)) return false;
                // Send request: CatalogReq-<Toon>
                E3Core.Server.PubServer.AddTopicMessage($"CatalogReq-{toon}", listKey);
                string topic = $"CatalogResp-{E3.CurrentName}-{listKey}";
                // Poll SharedDataClient.TopicUpdates for up to ~2s
                long end = Core.StopWatch.ElapsedMilliseconds + 4000;
                while (Core.StopWatch.ElapsedMilliseconds < end)
                {
                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics)
                        && topics.TryGetValue(topic, out var entry))
                    {
                        string payload = entry.Data;
                        int first = payload.IndexOf(':');
                        int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
                        string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
                        byte[] bytes = Convert.FromBase64String(b64);
                        var list = SpellDataList.Parser.ParseFrom(bytes);
                        data = list.Data;
                        return true;
                    }
                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(E3.CurrentName, out var topics2)
                        && topics2.TryGetValue(topic, out var entry2))
                    {
                        string payload = entry2.Data;
                        int first = payload.IndexOf(':');
                        int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
                        string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
                        byte[] bytes = Convert.FromBase64String(b64);
                        var list = SpellDataList.Parser.ParseFrom(bytes);
                        data = list.Data;
                        return true;
                    }
                    System.Threading.Thread.Sleep(25);
                }
            }
            catch { }
            return false;
        }

        private static string GetSelectedIniOwnerName()
        {
            try
            {
                string path = GetActiveSettingsPath();
                if (string.IsNullOrEmpty(path)) return E3.CurrentName;
                string file = Path.GetFileNameWithoutExtension(path);
                int us = file.IndexOf('_');
                if (us > 0) return file.Substring(0, us);
                return E3.CurrentName;
            }
            catch { return E3.CurrentName; }
        }

        // Organize from SpellData (protobuf) into the UI catalog structure
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
        {
            var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in data)
            {
                if (s == null) continue;
                string cat = s.Category ?? string.Empty;
                string sub = s.Subcategory ?? string.Empty;
                if (!dest.TryGetValue(cat, out var submap))
                {
                    submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
                    dest.Add(cat, submap);
                }
                if (!submap.TryGetValue(sub, out var l))
                {
                    l = new List<E3Spell>();
                    submap.Add(sub, l);
                }
                l.Add(new E3Spell {
                    Name = s.SpellName ?? string.Empty,
                    Category = cat,
                    Subcategory = sub,
                    Level = s.Level,
                    CastName = s.CastName ?? string.Empty,
                    TargetType = s.TargetType ?? string.Empty,
                    SpellType = s.SpellType ?? string.Empty,
                    Mana = s.Mana,
                    CastTime = s.MyCastTimeInSeconds,
                    Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
                    Range = s.MyRange,
                    Description = s.Description ?? string.Empty,
                    ResistType = s.ResistType ?? string.Empty,
                    ResistAdj = s.ResistAdj,
                    CastType = s.CastType.ToString(),
                    SpellIcon = s.SpellIcon
                });
            }
            foreach (var submap in dest.Values)
            {
                foreach (var l in submap.Values)
                {
                    l.Sort((a, b) => b.Level.CompareTo(a.Level));
                }
            }
            return dest;
        }

        // Organize skills like e3config: force into Skill/Basic and list by spell name
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeSkillsCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
        {
            var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
            var cat = "Skill"; var sub = "Basic";
            var submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
            dest[cat] = submap;
            var list = new List<E3Spell>();
            submap[sub] = list;
            foreach (var s in data)
            {
                if (s == null) continue;
                list.Add(new E3Spell {
                    Name = s.SpellName ?? string.Empty,
                    Category = cat,
                    Subcategory = sub,
                    Level = s.Level,
                    TargetType = s.TargetType ?? string.Empty,
                    SpellType = s.SpellType ?? string.Empty,
                    CastType = s.CastType.ToString(),
                    Description = s.Description ?? string.Empty,
                    SpellIcon = s.SpellIcon
                });
            }
            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return dest;
        }

        // Organize items like e3config: first key = CastName (item), subkey = SpellName, and list entries by item (CastName)
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeItemsCatalog(Google.Protobuf.Collections.RepeatedField<SpellData> data)
        {
            var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in data)
            {
                if (s == null) continue;
                string cat = s.CastName ?? string.Empty; // item name
                string sub = s.SpellName ?? string.Empty; // click spell
                if (!dest.TryGetValue(cat, out var submap))
                {
                    submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
                    dest.Add(cat, submap);
                }
                if (!submap.TryGetValue(sub, out var l))
                {
                    l = new List<E3Spell>();
                    submap.Add(sub, l);
                }
                l.Add(new E3Spell {
                    Name = s.CastName ?? string.Empty,
                    Category = cat,
                    Subcategory = sub,
                    Level = s.Level,
                    CastName = s.CastName ?? string.Empty,
                    TargetType = s.TargetType ?? string.Empty,
                    SpellType = s.SpellType ?? string.Empty,
                    Mana = s.Mana,
                    CastTime = s.MyCastTimeInSeconds,
                    Recast = s.RecastTime != 0 ? s.RecastTime : s.RecastDelay,
                    Range = s.MyRange,
                    Description = s.Description ?? string.Empty,
                    ResistType = s.ResistType ?? string.Empty,
                    ResistAdj = s.ResistAdj,
                    CastType = s.CastType.ToString(),
                    SpellIcon = s.SpellIcon
                });
            }
            return dest;
        }

        // Helpers to organize catalog data
        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> OrganizeCatalog(List<E3Core.Data.Spell> list)
        {
            var dest = new SortedDictionary<string, SortedDictionary<string, List<E3Spell>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in list)
            {
                if (s == null) continue;
                string cat = s.Category ?? string.Empty;
                string sub = s.Subcategory ?? string.Empty;
                if (!dest.TryGetValue(cat, out var submap))
                {
                    submap = new SortedDictionary<string, List<E3Spell>>(StringComparer.OrdinalIgnoreCase);
                    dest.Add(cat, submap);
                }
                if (!submap.TryGetValue(sub, out var l))
                {
                    l = new List<E3Spell>();
                    submap.Add(sub, l);
                }
                l.Add(new E3Spell { Name = s.SpellName ?? string.Empty, Category = cat, Subcategory = sub, Level = s.Level, SpellIcon = s.SpellIcon });
            }
            foreach (var submap in dest.Values)
            {
                foreach (var l in submap.Values)
                {
                    l.Sort((a, b) => b.Level.CompareTo(a.Level));
                }
            }
            return dest;
        }

        // Options for appending HealPct/Gem when adding from catalog in Heals
        private static bool _cfgAddAppendHealGem = true;
        private static string _cfgAddHealPct = "80";
        private static string _cfgAddGem = "1";

        private static void RenderAddFromCatalogModal(IniData pd, SectionData selectedSection)
        {
            imgui_Begin_OpenFlagSet("Add From Catalog", true);
            bool _open_Add = imgui_Begin("Add From Catalog", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
            if (_open_Add)
            {
                float totalW = Math.Max(880f, imgui_GetContentRegionAvailX());
                float listH = Math.Max(420f, imgui_GetContentRegionAvailY() * 0.8f);
                float thirdW = Math.Max(220f, totalW / 3.0f - 8.0f);

                // Header: type + filter + catalog source
                imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, "Add From Catalog");
                imgui_SameLine();
                if (imgui_BeginCombo("##type", _cfgAddType.ToString(), 0))
                {
                    foreach (AddType t in Enum.GetValues(typeof(AddType)))
                    {
                        bool sel = t == _cfgAddType;
                        if (imgui_Selectable(t.ToString(), sel)) _cfgAddType = t;
                    }
                    EndComboSafe();
                }
                imgui_SameLine();
                imgui_Text("Filter:");
                imgui_SameLine();
                if (imgui_InputText("##filter", _cfgAddFilter ?? string.Empty))
                    _cfgAddFilter = imgui_InputText_Get("##filter") ?? string.Empty;
                
                // Catalog source info and refresh button
                imgui_Separator();
                imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Catalog Source:");
                imgui_SameLine();
                
                // Color code the source based on type
                if (_cfg_CatalogSource.StartsWith("Remote"))
                    imgui_TextColored(0.7f, 1.0f, 0.7f, 1.0f, _cfg_CatalogSource); // Green for remote
                else if (_cfg_CatalogSource.StartsWith("Local (fallback)"))
                    imgui_TextColored(1.0f, 0.8f, 0.4f, 1.0f, _cfg_CatalogSource); // Orange for fallback
                else if (_cfg_CatalogSource.StartsWith("Local"))
                    imgui_TextColored(0.8f, 0.8f, 1.0f, 1.0f, _cfg_CatalogSource); // Light blue for local
                else
                    imgui_TextColored(0.8f, 0.8f, 0.8f, 1.0f, _cfg_CatalogSource); // Gray for unknown
                
                imgui_SameLine();
                if (imgui_Button("Refresh Catalog"))
                {
                    // Trigger catalog refresh
                    _cfg_CatalogsReady = false;
                    _cfgSpells.Clear();
                    _cfgAAs.Clear();
                    _cfgDiscs.Clear();
                    _cfgSkills.Clear();
                    _cfgItems.Clear();
                    _cfg_CatalogLoadRequested = true;
                    _cfg_CatalogStatus = "Queued catalog refresh...";
                    _cfg_CatalogSource = "Refreshing...";
                }
                
                // Show catalog status if loading
                if (_cfg_CatalogLoading)
                {
                    imgui_SameLine();
                    imgui_TextColored(0.9f, 0.9f, 0.4f, 1.0f, _cfg_CatalogStatus.Replace("Loading catalogs", "Loading"));
                }
                
                imgui_Separator();

                // If editing Heals keys, show append options
                if (IsHealingKey(_cfgSelectedSection, _cfgSelectedKey))
                {
                    imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Append options (Heals)");
                    bool prev = _cfgAddAppendHealGem;
                    _cfgAddAppendHealGem = imgui_Checkbox("Append HealPct + Gem", _cfgAddAppendHealGem);
                    imgui_SameLine();
                    imgui_Text("HealPct:"); imgui_SameLine();
                    imgui_SetNextItemWidth(45f);
                    if (imgui_InputText("##add_healpct", _cfgAddHealPct)) _cfgAddHealPct = imgui_InputText_Get("##add_healpct");
                    imgui_SameLine();
                    imgui_Text("Gem:"); imgui_SameLine();
                    imgui_SetNextItemWidth(35f);
                    if (imgui_InputText("##add_gem", _cfgAddGem)) _cfgAddGem = imgui_InputText_Get("##add_gem");
                    imgui_Separator();
                }

                // Resolve the catalog for the chosen type
                var src = GetCatalogByType(_cfgAddType);

                // -------- LEFT: Top-level categories --------
                if (imgui_BeginChild("TopLevelCats", thirdW, listH, true))
                {
                    imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Categories");
                    var cats = src.Keys.ToList();
                    cats.Sort(StringComparer.OrdinalIgnoreCase);
                    int ci = 0;
                    foreach (var c in cats)
                    {
                        bool sel = string.Equals(_cfgAddCategory, c, StringComparison.OrdinalIgnoreCase);
                        string id = $"{c}##Cat_{_cfgAddType}_{ci}";
                        if (imgui_Selectable(id, sel))
                        {
                            _cfgAddCategory = c;
                            _cfgAddSubcategory = string.Empty; // reset mid level on cat change
                        }
                        ci++;
                    }
                }
                imgui_EndChild();

                imgui_SameLine();

                // -------- MIDDLE: Subcategories for selected category --------
                if (imgui_BeginChild("SubCats", thirdW, listH, true))
                {
                    imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Subcategories");
                    if (!string.IsNullOrEmpty(_cfgAddCategory) && src.TryGetValue(_cfgAddCategory, out var submap))
                    {
                        var subs = submap.Keys.ToList();
                        subs.Sort(StringComparer.OrdinalIgnoreCase);
                        int si = 0;
                        foreach (var sc in subs)
                        {
                            bool sel = string.Equals(_cfgAddSubcategory, sc, StringComparison.OrdinalIgnoreCase);
                            string id = $"{sc}##Sub_{_cfgAddType}_{_cfgAddCategory}_{si}";
                            if (imgui_Selectable(id, sel)) _cfgAddSubcategory = sc;
                            si++;
                        }
                    }
                    else
                    {
                        imgui_Text("Select a category.");
                    }
                }
                imgui_EndChild();

                imgui_SameLine();

                // -------- RIGHT: Entries (with Add / Info) --------
                if (imgui_BeginChild("EntryList", thirdW, listH, true))
                {
                    imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Entries");

                    IEnumerable<E3Spell> entries = Enumerable.Empty<E3Spell>();
                    if (!string.IsNullOrEmpty(_cfgAddCategory) && src.TryGetValue(_cfgAddCategory, out var submap2))
                    {
                        if (!string.IsNullOrEmpty(_cfgAddSubcategory) && submap2.TryGetValue(_cfgAddSubcategory, out var l))
                            entries = l;
                        else
                            entries = submap2.Values.SelectMany(x => x);
                    }

                    string filter = (_cfgAddFilter ?? string.Empty).Trim();
                    if (filter.Length > 0)
                        entries = entries.Where(e => e.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

                    // stable ordering
                    entries = entries.OrderByDescending(e => e.Level)
                                     .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);

                    int i = 0;
                    foreach (var e in entries)
                    {
                        string uid = $"{_cfgAddType}_{_cfgAddCategory}_{_cfgAddSubcategory}_{i}";

                        // Add
                        if (imgui_Button($"Add##add_{uid}"))
                        {
                            var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                            if (kd != null)
                            {
                                var vals = GetValues(kd);
                                string v = (e.Name ?? string.Empty).Trim();

                                // If we're in a Heals key and the option is enabled, append HealPct/Gem for spells
                                if (IsHealingKey(_cfgSelectedSection, _cfgSelectedKey) && _cfgAddAppendHealGem && _cfgAddType == AddType.Spells)
                                {
                                    int hpct = 80; int gem = 1;
                                    if (!int.TryParse(_cfgAddHealPct?.Trim(), out hpct)) hpct = 80;
                                    hpct = Math.Max(1, Math.Min(99, hpct));
                                    if (!int.TryParse(_cfgAddGem?.Trim(), out gem)) gem = 1;
                                    gem = Math.Max(1, Math.Min(12, gem));
                                    v = $"{v}/HealPct|{hpct}/Gem|{gem}";
                                }

                                // Prevent duplicate base spell entries (compare by leading token before '/')
                                if (!vals.Any(x => (x ?? string.Empty).Split('/')[0].Equals((e.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)))
                                {
                                    vals.Add(v);
                                    WriteValues(kd, vals);
                                }
                            }
                        }
                        imgui_SameLine();

                        // Info
                        if (imgui_Button($"Info##info_{uid}"))
                        {
                            _cfgSpellInfoSpell = e;   // <- use the field that exists in your file
                            _cfgShowSpellInfoModal = true;
                        }
                        imgui_SameLine();

                        // Row text: show level + name (no ToDisplayString needed)
                        imgui_Text($"[{e.Level}] {e.Name}");
                        i++;
                    }

                    if (i == 0) imgui_Text("No entries found");
                }
                imgui_EndChild();

                imgui_Separator();
                // One-click bulk add of the currently visible entries
                if (imgui_Button("Add All Visible"))
                {
                    TryAddVisibleEntriesToSelectedKey(selectedSection);
                }
                imgui_SameLine();
                if (imgui_Button("Close")) { _cfgShowAddModal = false; }
            }
            imgui_End();

            // If user clicked the X, reflect that in our show flag
            if (!_open_Add || !imgui_Begin_OpenFlagGet("Add From Catalog")) _cfgShowAddModal = false;
        }

        private static SortedDictionary<string, SortedDictionary<string, List<E3Spell>>> GetCatalogByType(AddType t)
        {
            switch (t)
            {
                case AddType.AAs: return _cfgAAs;
                case AddType.Discs: return _cfgDiscs;
                case AddType.Skills: return _cfgSkills;
                case AddType.Items: return _cfgItems;
                case AddType.Spells:
                default: return _cfgSpells;
            }
        }
        
        // Search all catalogs for a spell/item/AA by name
        private static E3Spell FindSpellItemAAByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            // Search all catalog types for an exact match
            var catalogs = new[] 
            {
                (_cfgSpells, "Spell"),
                (_cfgAAs, "AA"),
                (_cfgDiscs, "Disc"), 
                (_cfgSkills, "Skill"),
                (_cfgItems, "Item")
            };
            
            foreach (var (catalog, type) in catalogs)
            {
                foreach (var categoryKvp in catalog)
                {
                    foreach (var subCategoryKvp in categoryKvp.Value)
                    {
                        var match = subCategoryKvp.Value.FirstOrDefault(spell => 
                            string.Equals(spell.Name, name, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            // Set the cast type if not already set
                            if (string.IsNullOrEmpty(match.CastType)) match.CastType = type;
                            return match;
                        }
                    }
                }
            }
            
            return null;
        }

        private static void TryAddVisibleEntriesToSelectedKey(SectionData selectedSection)
        {
            if (selectedSection == null || string.IsNullOrEmpty(_cfgSelectedKey)) return;
            var kd = selectedSection.Keys?.GetKeyData(_cfgSelectedKey);
            if (kd == null) return;
            var values = GetValues(kd);

            var src = GetCatalogByType(_cfgAddType);
            IEnumerable<E3Spell> entries = Enumerable.Empty<E3Spell>();
            if (!string.IsNullOrEmpty(_cfgAddCategory) && src.TryGetValue(_cfgAddCategory, out var submap2))
            {
                if (!string.IsNullOrEmpty(_cfgAddSubcategory) && submap2.TryGetValue(_cfgAddSubcategory, out var l))
                    entries = l;
                else
                    entries = submap2.Values.SelectMany(x => x);
            }
            string filter = (_cfgAddFilter ?? string.Empty).Trim();
            if (filter.Length > 0) entries = entries.Where(e => e.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            foreach (var e in entries)
            {
                string toAdd = (e.Name ?? string.Empty).Trim();
                if (!values.Contains(toAdd, StringComparer.OrdinalIgnoreCase)) values.Add(toAdd);
            }
            WriteValues(kd, values);
        }

        private static List<string> GetValues(KeyData kd)
        {
            var vals = new List<string>();
            try
            {
                if (kd.ValueList != null && kd.ValueList.Count > 0)
                {
                    foreach (var v in kd.ValueList) vals.Add(v ?? string.Empty);
                }
                else if (!string.IsNullOrEmpty(kd.Value))
                {
                    // Support pipe-delimited storage if present
                    var parts = (kd.Value ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());
                    foreach (var p in parts) vals.Add(p);
                }
            }
            catch { }
            return vals;
        }

        private static void WriteValues(KeyData kd, List<string> values)
        {
            try
            {
                if (kd == null) return;
                // Preserve exact row semantics: one value per row, including empties
                if (kd.ValueList != null)
                {
                    kd.ValueList.Clear();
                    foreach (var v in values) kd.ValueList.Add(v ?? string.Empty);
                }
                // Do NOT set kd.Value here; in our Ini parser, setting Value appends to ValueList.
                _cfg_Dirty = true;
            }
            catch { }
        }

        // Inventory scanning for Food/Drink using MQ TLOs (non-blocking via ProcessBackgroundWork trigger)
        private static void RenderFoodDrinkPicker(SectionData selectedSection)
        {
            // Use proper modal behavior
            imgui_Begin_OpenFlagSet("Pick From Inventory##modal", true);
            bool modalOpen = imgui_Begin("Pick From Inventory##modal", (int)(ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize | ImGuiWindowFlags.ImGuiWindowFlags_NoCollapse));
            
            if (modalOpen)
            {
                // Header with better styling
                imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"Pick {_cfgFoodDrinkKey} from inventory");
                imgui_Separator();
                
                // Status and scan button
                if (string.IsNullOrEmpty(_cfgFoodDrinkStatus))
                {
                    if (imgui_Button("Scan Inventory"))
                    {
                        _cfgFoodDrinkStatus = "Scanning...";
                        _cfgFoodDrinkScanRequested = true;
                    }
                    imgui_Text("Click above to scan your inventory.");
                }
                else
                {
                    imgui_TextColored(0.7f, 0.9f, 0.7f, 1.0f, _cfgFoodDrinkStatus);
                }
                
                imgui_Separator();

                // Results list with better sizing
                if (_cfgFoodDrinkCandidates.Count > 0)
                {
                    imgui_TextColored(0.8f, 0.9f, 1.0f, 1.0f, "Found items (click to select):");
                    
                    // Use responsive sizing for the list
                    float listHeight = Math.Min(400f, Math.Max(150f, _cfgFoodDrinkCandidates.Count * 20f + 40f));
                    float listWidth = Math.Max(300f, imgui_GetContentRegionAvailX() * 0.9f);
                    
                    if (imgui_BeginChild("FoodDrinkList", listWidth, listHeight, true))
                    {
                        for (int i = 0; i < _cfgFoodDrinkCandidates.Count; i++)
                        {
                            var item = _cfgFoodDrinkCandidates[i];
                            if (imgui_Selectable($"{item}##item_{i}", false))
                            {
                                // Apply selection
                                var pdAct = GetActiveCharacterIniData();
                                var secData = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                                var keyData = secData?.Keys.GetKeyData(_cfgSelectedKey);
                                if (keyData != null)
                                {
                                    var vals = GetValues(keyData);
                                    // Replace first value or add if empty
                                    if (vals.Count == 0) vals.Add(item);
                                    else vals[0] = item;
                                    WriteValues(keyData, vals);
                                }
                                _cfgShowFoodDrinkModal = false;
                                break; // Exit loop after selection
                            }
                        }
                        imgui_EndTable();
                    }
                }
                else if (!string.IsNullOrEmpty(_cfgFoodDrinkStatus) && !_cfgFoodDrinkStatus.Contains("Scanning"))
                {
                    imgui_TextColored(0.9f, 0.7f, 0.7f, 1.0f, "No matching items found.");
                }

                imgui_Separator();
                
                // Action buttons
                if (_cfgFoodDrinkCandidates.Count > 0)
                {
                    if (imgui_Button("Rescan"))
                    {
                        _cfgFoodDrinkStatus = "Scanning...";
                        _cfgFoodDrinkCandidates.Clear();
                        _cfgFoodDrinkScanRequested = true;
                    }
                    imgui_SameLine();
                }
                
                if (imgui_Button("Close"))
                {
                    _cfgShowFoodDrinkModal = false;
                }
            }
            
            imgui_End();
            
            // Handle window close via X button
            if (!modalOpen)
            {
                _cfgShowFoodDrinkModal = false;
            }
        }

        // Toon picker modal for Heals section (Tank / Important Bot)
        private static void RenderToonPickerModal(SectionData selectedSection)
        {
            imgui_Begin_OpenFlagSet("Pick Toons", true);
            bool _open_toon = imgui_Begin("Pick Toons", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
            if (_open_toon)
            {
                if (!string.IsNullOrEmpty(_cfgToonPickerStatus)) imgui_Text(_cfgToonPickerStatus);
                float h = 300f; float w = 420f;
                if (imgui_BeginChild("ToonList", w, h, true))
                {
                    var list = _cfgToonCandidates ?? new List<string>();
                    var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                    var current = kd != null ? GetValues(kd) : new List<string>();
                    int i = 0;
                    foreach (var name in list)
                    {
                        string label = $"{name}##toon_{i}";
                        bool already = current.Contains(name, StringComparer.OrdinalIgnoreCase);
                        if (imgui_Selectable(label, false))
                        {
                            if (kd != null)
                            {
                                var vals = GetValues(kd);
                                if (!vals.Contains(name, StringComparer.OrdinalIgnoreCase))
                                {
                                    vals.Add(name);
                                    WriteValues(kd, vals);
                                }
                            }
                        }
                        if (already) { imgui_SameLine(); imgui_Text("(added)"); }
                        i++;
                    }
                }
                imgui_EndChild();
                if (imgui_Button("Close")) _cfgShowToonPickerModal = false;
            }
            imgui_End();
            if (!_open_toon) _cfgShowToonPickerModal = false;
        }

        // Spell Info modal (read-only details) using real ImGui tables + colored labels
        private static void RenderSpellInfoModal()
        {
            var s = _cfgSpellInfoSpell;
            if (s == null) { _cfgShowSpellInfoModal = false; return; }
            imgui_Begin_OpenFlagSet("Spell Info", true);
            try
            {
                if (Core._MQ2MonoVersion >= 0.34m)
                {
                    // Keep the modal contained within a reasonable width/height
                    imgui_SetNextWindowSizeConstraints(380f, 0f, 640f, 1000f);
                }
            }
            catch { }
            bool _open_info = imgui_Begin("Spell Information", 0);
            if (_open_info)
            {
                // Header with better styling
                imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"{s.Name ?? string.Empty}");
                imgui_Separator();

                // Build table rows (label, value) with better formatting
                var rows = new List<KeyValuePair<string, string>>();
                rows.Add(new KeyValuePair<string, string>("Type", s.CastType ?? string.Empty));
                rows.Add(new KeyValuePair<string, string>("Level", s.Level > 0 ? s.Level.ToString() : string.Empty));
                rows.Add(new KeyValuePair<string, string>("Mana", s.Mana > 0 ? s.Mana.ToString() : string.Empty));
                rows.Add(new KeyValuePair<string, string>("Cast Time", s.CastTime > 0 ? $"{s.CastTime:0.00}s" : string.Empty));
                rows.Add(new KeyValuePair<string, string>("Recast", s.Recast > 0 ? FormatMsSmart(s.Recast) : string.Empty));
                rows.Add(new KeyValuePair<string, string>("Range", s.Range > 0 ? s.Range.ToString("0") : string.Empty));
                rows.Add(new KeyValuePair<string, string>("Target", s.TargetType ?? string.Empty));
                rows.Add(new KeyValuePair<string, string>("School", s.SpellType ?? string.Empty));
                rows.Add(new KeyValuePair<string, string>("Resist", !string.IsNullOrEmpty(s.ResistType) ? ($"{s.ResistType} {(s.ResistAdj != 0 ? "(" + s.ResistAdj.ToString() + ")" : string.Empty)}") : string.Empty));
                // Filter out empty values to avoid rendering blank rows
                rows = rows.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToList();

                float width = Math.Min(520f, imgui_GetContentRegionAvailX());
                if (imgui_BeginTable("SpellInfoTable", 2, 0, width))
                {
                    imgui_TableSetupColumn("Property", 0, 140f);
                    imgui_TableSetupColumn("Value", 0, Math.Max(260f, width - 160f));
                    imgui_TableHeadersRow();

                    foreach (var kv in rows)
                    {
                        imgui_TableNextRow();
                        imgui_TableNextColumn();
                        // Colored label (soft yellow)
                        imgui_TextColored(0.95f, 0.85f, 0.35f, 1f, kv.Key);
                        imgui_TableNextColumn();
                        imgui_Text(kv.Value);
                    }
                    imgui_EndTable();
                }

                if (!string.IsNullOrEmpty(s.Description))
                {
                    imgui_Separator();
                    imgui_TextColored(0.75f, 0.85f, 1.0f, 1f, "Description:");
                    try
                    {
                        if (Core._MQ2MonoVersion >= 0.34m)
                        {
                            imgui_TextWrapped(s.Description);
                        }
                        else
                        {
                            // Fallback to manual wrapping on older MQ2Mono
                            float totalWidth = Math.Min(520f, imgui_GetContentRegionAvailX());
                            float valueColWidth = Math.Max(260f, totalWidth - 160f);
                            int maxChars = Math.Max(30, (int)Math.Floor(valueColWidth / 8f));
                            string wrapped = WrapTextByChars(s.Description, maxChars);
                            imgui_Text(wrapped);
                        }
                    }
                    catch { imgui_Text(s.Description); }
                }

                imgui_Separator();
                if (imgui_Button("Close")) { _cfgShowSpellInfoModal = false; _cfgSpellInfoSpell = null; }
            }
            imgui_End();
            if (!_open_info) { _cfgShowSpellInfoModal = false; _cfgSpellInfoSpell = null; }
        }

        // Helper: format milliseconds as seconds, or minutes+seconds over 60s
        private static string FormatMsSmart(int ms)
        {
            if (ms <= 0) return string.Empty;
            double totalSec = ms / 1000.0;
            if (totalSec < 60.0)
            {
                return totalSec < 10 ? totalSec.ToString("0.##") + "s" : totalSec.ToString("0.#") + "s";
            }
            int m = (int)(totalSec / 60.0);
            double rs = totalSec - m * 60;
            if (rs < 0.5) return m.ToString() + "m";
            return m.ToString() + "m " + rs.ToString("0.#") + "s";
        }

        // Simple word-wrapping helper based on maximum characters per line.
        // Inserts line breaks at spaces where possible; falls back to hard breaks for long words.
        private static string WrapTextByChars(string text, int maxCharsPerLine)
        {
            if (string.IsNullOrEmpty(text) || maxCharsPerLine <= 0) return text ?? string.Empty;
            var sbAll = new System.Text.StringBuilder(text.Length + 16);
            var paragraphs = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (int p = 0; p < paragraphs.Length; p++)
            {
                string para = paragraphs[p];
                var sb = new System.Text.StringBuilder(para.Length + 8);
                int lineLen = 0;
                foreach (var word in para.Split(' '))
                {
                    if (word.Length + (lineLen == 0 ? 0 : 1) > maxCharsPerLine)
                    {
                        if (lineLen > 0)
                        {
                            sb.Append('\n');
                            lineLen = 0;
                        }
                        int idx = 0;
                        while (idx < word.Length)
                        {
                            int take = Math.Min(maxCharsPerLine, word.Length - idx);
                            sb.Append(word, idx, take);
                            idx += take;
                            if (idx < word.Length) sb.Append('\n');
                        }
                        lineLen = word.Length % maxCharsPerLine;
                    }
                    else
                    {
                        if (lineLen != 0) { sb.Append(' '); lineLen++; }
                        sb.Append(word);
                        lineLen += word.Length;
                    }
                }
                sbAll.Append(sb.ToString());
                if (p < paragraphs.Length - 1) sbAll.Append('\n');
            }
            return sbAll.ToString();
        }

        // Append If modal: choose an If key to append to a specific row value
        private static void RenderIfAppendModal(SectionData selectedSection)
        {
            imgui_Begin_OpenFlagSet("Append If", true);
            bool _open_if = imgui_Begin("Append If", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
            if (_open_if)
            {
                if (!string.IsNullOrEmpty(_cfgIfAppendStatus)) imgui_Text(_cfgIfAppendStatus);
                float h = 300f; float w = 520f;
                if (imgui_BeginChild("IfList", w, h, true))
                {
                    var list = _cfgIfAppendCandidates ?? new List<string>();
                    int i = 0;
                    foreach (var key in list)
                    {
                        string label = $"{key}##ifkey_{i}";
                        if (imgui_Selectable(label, false))
                        {
                            try
                            {
                                var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                                if (kd != null && _cfgIfAppendRow >= 0)
                                {
                                    var vals = GetValues(kd);
                                    if (_cfgIfAppendRow < vals.Count)
                                    {
                                        string updated = AppendIfToken(vals[_cfgIfAppendRow] ?? string.Empty, key);
                                        vals[_cfgIfAppendRow] = updated;
                                        WriteValues(kd, vals);
                                    }
                                }
                            }
                            catch { }
                            _cfgShowIfAppendModal = false;
                            break;
                        }
                        i++;
                    }
                }
                imgui_EndChild();
                if (imgui_Button("Close")) _cfgShowIfAppendModal = false;
            }
            imgui_End();
            if (!_open_if) _cfgShowIfAppendModal = false;
        }

        // Helper to append or extend an Ifs| key list token in a config value
        private static string AppendIfToken(string value, string ifKey)
        {
            string v = value ?? string.Empty;
            // We support both legacy "Ifs|" and preferred "/Ifs|" tokens when extending,
            // but we always write using "/Ifs|" going forward.
            const string tokenPreferred = "/Ifs|";
            const string tokenLegacy = "Ifs|";
            int posSlash = v.IndexOf(tokenPreferred, StringComparison.OrdinalIgnoreCase);
            int posLegacy = v.IndexOf(tokenLegacy, StringComparison.OrdinalIgnoreCase);
            int pos = posSlash >= 0 ? posSlash : posLegacy;
            int tokenLen = posSlash >= 0 ? tokenPreferred.Length : tokenLegacy.Length;

            if (pos < 0)
            {
                // No Ifs present; append preferred token with NO leading separator
                if (v.Length == 0) return tokenPreferred + ifKey;
                return v + tokenPreferred + ifKey;
            }

            // Extend existing Ifs list; rebuild using preferred token
            int start = pos + tokenLen;
            int end = v.IndexOf('|', start);
            string head = v.Substring(0, pos) + tokenPreferred; // normalize token
            string rest = end >= 0 ? v.Substring(end) : string.Empty;
            string list = end >= 0 ? v.Substring(start, end - start) : v.Substring(start);
            var items = list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(x => x.Trim())
                            .Where(x => x.Length > 0)
                            .ToList();
            if (!items.Contains(ifKey, StringComparer.OrdinalIgnoreCase)) items.Add(ifKey);
            string rebuilt = head + string.Join(",", items) + rest;
            return rebuilt;
        }

        // Inventory helper that uses MQ TLOs to scan for Food/Drink items
        private static List<string> ScanInventoryForType(string key)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(key)) return found.ToList();
            string target = key.Trim();

            // Scan a generous set of inventory indices and their bag contents
            for (int inv = 1; inv <= 40; inv++)
            {
                try
                {
                    bool present = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}]}}" );
                    if (!present) continue;

                    // top-level item type
                    string t = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Type}}" ) ?? string.Empty;
                    if (!string.IsNullOrEmpty(t) && t.Equals(target, StringComparison.OrdinalIgnoreCase))
                    {
                        string name = E3.MQ.Query<string>($"${{Me.Inventory[{inv}]}}" ) ?? string.Empty;
                        if (!string.IsNullOrEmpty(name)) found.Add(name);
                    }

                    // bag contents if container
                    int slots = E3.MQ.Query<int>($"${{Me.Inventory[{inv}].Container}}" );
                    if (slots <= 0) continue;
                    for (int i = 1; i <= slots; i++)
                    {
                        try
                        {
                            bool ipresent = E3.MQ.Query<bool>($"${{Me.Inventory[{inv}].Item[{i}]}}" );
                            if (!ipresent) continue;
                            string it = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}].Type}}" ) ?? string.Empty;
                            if (!string.IsNullOrEmpty(it) && it.Equals(target, StringComparison.OrdinalIgnoreCase))
                            {
                                string iname = E3.MQ.Query<string>($"${{Me.Inventory[{inv}].Item[{i}]}}" ) ?? string.Empty;
                                if (!string.IsNullOrEmpty(iname)) found.Add(iname);
                            }
                        }
                        catch { }
                    }
                }
                catch { }
            }

            return found.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
        }

        // HealPct + Gem suffix input helper state
        private static string _cfgHealPctInputBuffer = string.Empty;
        private static string _cfgHealPctThreshold = "80";
        private static string _cfgHealGemIndex = "1";
        private static AddType _cfgHealPctCatalogType = AddType.Spells;
        private static string _cfgHealPctSelectedSpell = string.Empty;
        private static string _cfgHealPctFilter = string.Empty;
        
        /// <summary>
        /// Renders inputs to append '/HealPct|##' and '/Gem|#' to a spell name and add it to the current key.
        /// Supports manual entry or selection from the catalog.
        /// </summary>
        private static bool RenderHealPctSuffixInput(string id, SectionData selectedSection, float width = 0f)
        {
            bool spellAdded = false;
            
            // Manual entry row
            if (width > 0) imgui_SetNextItemWidth(width);
            if (imgui_InputText($"##healPctSpell_{id}", _cfgHealPctInputBuffer))
            {
                _cfgHealPctInputBuffer = imgui_InputText_Get($"##healPctSpell_{id}");
            }
            imgui_SameLine();
            
            imgui_Text("HealPct:");
            imgui_SameLine();
            imgui_SetNextItemWidth(45f);
            if (imgui_InputText($"##healPctThreshold_{id}", _cfgHealPctThreshold))
            {
                _cfgHealPctThreshold = imgui_InputText_Get($"##healPctThreshold_{id}");
            }
            imgui_SameLine();
            imgui_Text("Gem:");
            imgui_SameLine();
            imgui_SetNextItemWidth(35f);
            if (imgui_InputText($"##healGemIdx_{id}", _cfgHealGemIndex))
            {
                _cfgHealGemIndex = imgui_InputText_Get($"##healGemIdx_{id}");
            }
            imgui_SameLine();
            
            imgui_PushStyleColor(21, 0.2f, 0.7f, 0.2f, 1.0f);
            bool addManualClicked = imgui_Button($"Add##healpct_{id}");
            imgui_PopStyleColor();
            
            // Catalog selection row
            imgui_TextColored(0.7f, 0.8f, 0.9f, 1.0f, "Or pick from catalog:");
            
            // Type selection
            imgui_SetNextItemWidth(90f);
            if (imgui_BeginCombo($"##healPctType_{id}", _cfgHealPctCatalogType.ToString(), 0))
            {
                foreach (AddType t in Enum.GetValues(typeof(AddType)))
                {
                    bool sel = t == _cfgHealPctCatalogType;
                    if (imgui_Selectable(t.ToString(), sel)) _cfgHealPctCatalogType = t;
                }
                EndComboSafe();
            }
            imgui_SameLine();
            
            // Filter
            imgui_SetNextItemWidth(120f);
            if (imgui_InputText($"##healPctFilter_{id}", _cfgHealPctFilter))
            {
                _cfgHealPctFilter = imgui_InputText_Get($"##healPctFilter_{id}");
            }
            imgui_SameLine();
            
            // Spell selection dropdown - show filtered spells from selected catalog
            var catalog = GetCatalogByType(_cfgHealPctCatalogType);
            var allSpells = catalog.Values.SelectMany(submap => submap.Values.SelectMany(spells => spells));
            
            string filter = _cfgHealPctFilter?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(filter))
            {
                allSpells = allSpells.Where(s => s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            
            var sortedSpells = allSpells.OrderByDescending(s => s.Level).ThenBy(s => s.Name).Take(50).ToList();
            string previewText = string.IsNullOrEmpty(_cfgHealPctSelectedSpell) ? "Select spell..." : _cfgHealPctSelectedSpell;
            
            imgui_SetNextItemWidth(200f);
            if (imgui_BeginCombo($"##healPctSpellSelect_{id}", previewText, 0))
            {
                foreach (var spell in sortedSpells)
                {
                    bool sel = spell.Name.Equals(_cfgHealPctSelectedSpell, StringComparison.OrdinalIgnoreCase);
                    string displayText = $"[{spell.Level}] {spell.Name}";
                    if (imgui_Selectable(displayText, sel))
                    {
                        _cfgHealPctSelectedSpell = spell.Name;
                    }
                }
                EndComboSafe();
            }
            imgui_SameLine();
            
            // Add from catalog button
            imgui_PushStyleColor(21, 0.2f, 0.6f, 0.8f, 1.0f); // Blue button
            bool addCatalogClicked = imgui_Button($"Add From Catalog##{id}");
            imgui_PopStyleColor();
            
            // Normalized and bounded inputs
            int hpct = 80;
            if (!int.TryParse(_cfgHealPctThreshold?.Trim(), out hpct)) hpct = 80;
            hpct = Math.Max(1, Math.Min(99, hpct));
            int gem = 1;
            if (!int.TryParse(_cfgHealGemIndex?.Trim(), out gem)) gem = 1;
            gem = Math.Max(1, Math.Min(12, gem));
            
            // Handle manual entry
            if (addManualClicked && !string.IsNullOrWhiteSpace(_cfgHealPctInputBuffer))
            {
                string spellName = _cfgHealPctInputBuffer.Trim();
                string spellWithSuffix = $"{spellName}/HealPct|{hpct}/Gem|{gem}";
                if (TryAddSpellToSelectedKey(selectedSection, spellWithSuffix))
                {
                    _cfgHealPctInputBuffer = string.Empty;
                    spellAdded = true;
                }
            }
            
            // Handle catalog selection
            if (addCatalogClicked && !string.IsNullOrWhiteSpace(_cfgHealPctSelectedSpell))
            {
                string spellWithSuffix = $"{_cfgHealPctSelectedSpell}/HealPct|{hpct}/Gem|{gem}";
                if (TryAddSpellToSelectedKey(selectedSection, spellWithSuffix))
                {
                    _cfgHealPctSelectedSpell = string.Empty;
                    spellAdded = true;
                }
            }
            
            return spellAdded;
        }
        
        /// <summary>
        /// Helper to add a spell to the currently selected configuration key
        /// </summary>
        private static bool TryAddSpellToSelectedKey(SectionData selectedSection, string spellWithSuffix)
        {
            if (selectedSection != null)
            {
                var keyData = selectedSection.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                if (keyData != null)
                {
                    var values = GetValues(keyData);
                    // Check if the exact spell (without suffix) already exists
                    string spellName = spellWithSuffix.Split('/')[0];
                    bool alreadyExists = values.Any(v => v.Split('/')[0].Equals(spellName, StringComparison.OrdinalIgnoreCase));
                    
                    if (!alreadyExists)
                    {
                        values.Add(spellWithSuffix);
                        WriteValues(keyData, values);
                        return true;
                    }
                }
            }
            return false;
        }

        #region Spell Icon Rendering
        
        /// <summary>
        /// Renders a spell icon with the specified size and optional tooltip
        /// </summary>
        /// <param name="spellId">The EQ spell ID</param>
        /// <param name="size">Size of the icon (both width and height)</param>
        /// <param name="showTooltip">Whether to show a tooltip on hover</param>
        /// <returns>True if the icon was clicked</returns>
        public static bool RenderSpellIcon(int spellId, float size = 40.0f, bool showTooltip = true)
        {
            if (spellId <= 0)
            {
                // Render a placeholder for invalid spell ID
                RenderSpellIconPlaceholder(size);
                return false;
            }

            // Draw natively by spell id
            imgui_DrawSpellIconBySpellID(spellId, size);

            bool clicked = imgui_IsItemHovered() && imgui_IsMouseClicked(0);

            if (showTooltip && imgui_IsItemHovered())
            {
                RenderSpellTooltip(spellId, GetSpellIconIndex(spellId));
            }

            return clicked;
        }

        /// <summary>
        /// Renders a spell icon by its icon index
        /// </summary>
        /// <param name="iconIndex">The spell icon index (0-based)</param>
        /// <param name="size">Size of the icon</param>
        /// <param name="showTooltip">Whether to show tooltip on hover</param>
        /// <param name="spellId">Optional spell ID for tooltip info</param>
        /// <returns>True if the icon was clicked</returns>
        public static bool RenderSpellIconByIndex(int iconIndex, float size = 40.0f, bool showTooltip = true, int spellId = 0)
        {
            try
            {
                if (!E3Next.UI.SpellIconManager.IsReady())
                {
                    // Try to initialize if not ready
                    InitializeSpellIcons();
                    if (!E3Next.UI.SpellIconManager.IsReady())
                    {
                        RenderSpellIconPlaceholder(size);
                        return false;
                    }
                }

                // Draw via native EQ texture animation wrapper
                imgui_DrawSpellIconByIconIndex(iconIndex, size);

                // Compute click based on hover state
                bool clicked = imgui_IsItemHovered() && imgui_IsMouseClicked(0);

                // Show tooltip on hover if enabled
                if (showTooltip && imgui_IsItemHovered())
                {
                    RenderSpellTooltip(spellId, iconIndex);
                }

                return clicked;
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error rendering spell icon {iconIndex}: {ex.Message}");
                RenderSpellIconPlaceholder(size);
                return false;
            }
        }

        /// <summary>
        /// Renders a clickable image button
        /// </summary>
        private static bool RenderIconButton(IntPtr textureId, float size)
        {
            // Use the ImGui Image function to render the texture
            imgui_Image(textureId, size, size);
            
            // Check if the image was clicked
            return imgui_IsItemHovered() && imgui_IsMouseClicked(0); // Left mouse button
        }

        /// <summary>
        /// Renders a placeholder when spell icon is not available
        /// </summary>
        private static void RenderSpellIconPlaceholder(float size)
        {
            // Draw a simple colored rectangle as placeholder
            imgui_PushStyleColor((int)ImGuiCol.Button, 0.3f, 0.3f, 0.3f, 1.0f);
            imgui_ButtonEx("?", size, size);
            imgui_PopStyleColor(1);

            if (imgui_IsItemHovered())
            {
                imgui_BeginTooltip();
                imgui_TextWrapped("Spell icon not available");
                imgui_EndTooltip();
            }
        }

        /// <summary>
        /// Renders a tooltip with spell information
        /// </summary>
        private static void RenderSpellTooltip(int spellId, int iconIndex)
        {
            imgui_BeginTooltip();
            
            if (spellId > 0)
            {
                try
                {
                    // Get spell information from MQ
                    string spellName = mq_ParseTLO($"${{Spell[{spellId}].Name}}");
                    string spellLevel = mq_ParseTLO($"${{Spell[{spellId}].Level}}");
                    string spellDescription = mq_ParseTLO($"${{Spell[{spellId}].Description}}");
                    
                    if (spellName != "NULL" && !string.IsNullOrEmpty(spellName))
                    {
                        imgui_TextColored(0.9f, 0.9f, 0.3f, 1.0f, spellName);
                        
                        if (spellLevel != "NULL" && !string.IsNullOrEmpty(spellLevel))
                        {
                            imgui_Text($"Level: {spellLevel}");
                        }
                        
                        if (spellDescription != "NULL" && !string.IsNullOrEmpty(spellDescription) && spellDescription.Length > 0)
                        {
                            imgui_Separator();
                            imgui_PushTextWrapPos(300.0f); // Wrap at 300 pixels
                            imgui_TextWrapped(spellDescription);
                            imgui_PopTextWrapPos();
                        }
                    }
                    else
                    {
                        imgui_Text($"Spell ID: {spellId}");
                    }
                }
                catch (Exception ex)
                {
                    imgui_Text($"Spell ID: {spellId}");
                    imgui_Text($"Error: {ex.Message}");
                }
            }
            else
            {
                imgui_Text($"Icon Index: {iconIndex}");
            }
            
            imgui_EndTooltip();
        }

        /// <summary>
        /// Gets the spell icon index for a given spell ID
        /// This needs to be implemented based on EQ's spell data mapping
        /// </summary>
        private static int GetSpellIconIndex(int spellId)
        {
            try
            {
                // Query MQ for the spell's icon ID
                string iconIdStr = mq_ParseTLO($"${{Spell[{spellId}].SpellIcon}}");
                if (iconIdStr != "NULL" && int.TryParse(iconIdStr, out int iconId))
                {
                    // EQ's spell icon IDs need to be converted to 0-based indices
                    // This conversion may need adjustment based on how EQ stores icon IDs
                    return Math.Max(0, iconId - 1); // Convert to 0-based index
                }
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error getting spell icon index for spell {spellId}: {ex.Message}");
            }
            
            return 0; // Default to first icon
        }

        /// <summary>
        /// Initializes the spell icon system if not already done
        /// </summary>
        private static void InitializeSpellIcons()
        {
            try
            {
                if (!E3Next.UI.SpellIconManager.IsReady())
                {
                    // Try to get EQ directory from MQ
                    string eqDir = mq_ParseTLO("${EverQuest.Path}");
                    if (eqDir != "NULL" && !string.IsNullOrEmpty(eqDir))
                    {
                        E3Next.UI.SpellIconManager.Initialize(eqDir);
                    }
                    else
                    {
                        E3Core.Utility.e3util._log.Write("Could not determine EQ directory for spell icon initialization");
                    }
                }
            }
            catch (Exception ex)
            {
                E3Core.Utility.e3util._log.Write($"Error initializing spell icons: {ex.Message}");
            }
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using IniParser.Model;
using E3Core.Processors;
using NetMQ;
using NetMQ.Sockets;
using Google.Protobuf;

namespace MonoCore
{
    // /e3imgui UI extracted into dedicated partial class file
    public static partial class Core
    {
        // Config UI toggle: "/e3imgui".
        private static readonly string _e3ImGuiWindow = "E3Next Config";
        private static bool _imguiInitDone = false;
        private static bool _imguiContextReady = false;

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
                
                imgui_Begin(_e3ImGuiWindow, (int)ImGuiWindowFlags.ImGuiWindowFlags_None);

                // Header with better styling
                imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"nE³xt v{E3Core.Processors.Setup._e3Version} | Build {E3Core.Processors.Setup._buildDate}");
                
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
            
            // Section header
            imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Character Configuration");
            
            imgui_SameLine();
            imgui_TextColored(0.7f, 0.7f, 0.7f, 1.0f, $"({Path.GetFileNameWithoutExtension(currentDisplay)})");
            
            imgui_Separator();
            
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
                imgui_Text("Other Characters:");
                
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
            
            imgui_Separator();
            
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


        // Catalogs and Add modal state
        private static bool _cfg_CatalogsReady = false;
        private static bool _cfg_CatalogLoadRequested = false;
        private static bool _cfg_CatalogLoading = false;
        private static string _cfg_CatalogStatus = string.Empty;
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

            float availY = imgui_GetContentRegionAvailY();
            float leftW = 220f;
            
            // Left pane: sections with better styling
            imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Main Ini Sections");
            imgui_SameLine(245);
            imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Section Topics");

            if (imgui_BeginChild("Cfg_Left", leftW, Math.Max(120f, availY * 0.8f), true))
            {
                foreach (var sec in _cfgSectionsOrdered)
                {
                    bool sel = string.Equals(_cfgSelectedSection, sec, StringComparison.OrdinalIgnoreCase);
                    // Add emoji to section names for better visual distinction
                    string sectionDisplay = sec;
                    if (imgui_Selectable(sectionDisplay, sel))
                    {
                        _cfgSelectedSection = sec;
                        var secData = pd.Sections.GetSectionData(sec);
                        _cfgSelectedKey = secData?.Keys?.FirstOrDefault()?.KeyName ?? string.Empty;
                        _cfgSelectedValueIndex = -1;
                    }
                }
            }
            imgui_EndChild();

            imgui_SameLine();

            // Middle pane: keys of selected section
            float midW = 240f;
            var selectedSection = pd.Sections.GetSectionData(_cfgSelectedSection ?? string.Empty);
            if (imgui_BeginChild("Cfg_Middle", midW, Math.Max(120f, availY * 0.8f), true))
            {
                if (selectedSection == null)
                {
                    imgui_Text("Select a section.");
                }
                else
                {
                    KeyDataCollection keysEnum = (selectedSection != null && selectedSection.Keys != null)
                        ? selectedSection.Keys
                        : new KeyDataCollection();
                    string[] keys = keysEnum.Select(k => k.KeyName).ToArray();
                    if (keys.Length == 0)
                    {
                        imgui_Text("(No keys)");
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(_cfgSelectedKey) || !keys.Contains(_cfgSelectedKey, StringComparer.OrdinalIgnoreCase))
                        {
                            _cfgSelectedKey = keys[0];
                            _cfgSelectedValueIndex = -1;
                        }
                        
                        foreach (var k in keys)
                        {
                            bool sel = string.Equals(_cfgSelectedKey, k, StringComparison.OrdinalIgnoreCase);
                            if (imgui_Selectable(k, sel))
                            {
                                _cfgSelectedKey = k;
                                _cfgSelectedValueIndex = -1;
                            }
                        }
                    }
                }
            }
            imgui_EndChild();

            imgui_SameLine();

            // Right pane: values for selected key
            float rightW = Math.Max(600f, imgui_GetContentRegionAvailX() - leftW - midW - 20f);
            if (imgui_BeginChild("Cfg_Right", rightW, Math.Max(120f, availY * 0.8f), true))
            {
                if (selectedSection == null)
                {
                    imgui_Text("No section selected.");
                }
                else
                {
                    var kd = selectedSection.Keys.GetKeyData(_cfgSelectedKey ?? string.Empty);
                    string raw = kd?.Value ?? string.Empty;
                    var parts = GetValues(kd);
                    if (parts.Count == 0)
                    {
                        imgui_Text("(No values)");
                        imgui_Separator();
                    }

                    // Title row with better styling
                    imgui_TextColored(0.95f, 0.85f, 0.35f, 1.0f, $"[{_cfgSelectedSection}] {_cfgSelectedKey}");
                    imgui_Separator();

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

                        string display = current.Length == 0 ? "(unset)" : (string.Equals(current, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(current, "On", StringComparison.OrdinalIgnoreCase) ? "" + current : "" + current);
                        if (BeginComboSafe("Value", display))
                        {
                            foreach (var opt in baseOpts)
                            {
                                bool sel = string.Equals(current, opt, StringComparison.OrdinalIgnoreCase);
                                string optDisplay = string.Equals(opt, "True", StringComparison.OrdinalIgnoreCase) || string.Equals(opt, "On", StringComparison.OrdinalIgnoreCase) ? "" + opt : "" + opt;
                                if (imgui_Selectable(optDisplay, sel))
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
                            
                            // Edit button with icon
                            if (imgui_Button($"Edit##edit_{itemUid}"))
                            {
                                _cfgInlineEditIndex = i;
                                _cfgInlineEditBuffer = v;
                            }
                            imgui_SameLine();
                            
                            // Delete button with icon
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
                            
                            // Append If button for rows that support Ifs (typically spell-like entries)
                            if (imgui_Button($"If+##if_{itemUid}"))
                            {
                                try
                                {
                                    _cfgIfAppendRow = i;
                                    // Collect IF keys from current INI [Ifs] and GlobalIfs
                                    var list = new List<string>();
                                    try
                                    {
                                        var ini = GetActiveCharacterIniData();
                                        var secIf = ini?.Sections?.GetSectionData("Ifs");
                                        if (secIf?.Keys != null)
                                        {
                                            foreach (var k in secIf.Keys) if (!string.IsNullOrEmpty(k.KeyName)) list.Add(k.KeyName);
                                        }
                                    }
                                    catch { }
                                    try
                                    {
                                        if (E3.GlobalIfs?.Ifs != null)
                                        {
                                            foreach (var k in E3.GlobalIfs.Ifs.Keys) if (!string.IsNullOrEmpty(k)) list.Add(k);
                                        }
                                    }
                                    catch { }
                                    list = list.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
                                    _cfgIfAppendCandidates = list;
                                    _cfgIfAppendStatus = list.Count == 0 ? "No Ifs found in [Ifs] or Global Ifs." : $"{list.Count} If(s) found.";
                                }
                                catch { _cfgIfAppendCandidates = new List<string>(); _cfgIfAppendStatus = "Error loading Ifs."; }
                                _cfgShowIfAppendModal = true;
                            }
                            imgui_SameLine();
                            
                            // Value display with better formatting
                            imgui_TextColored(0.9f, 0.9f, 0.9f, 1.0f, v);
                        }
                        else
                        {
                            // Edit mode with better styling
                            imgui_Text($"🔹 {i + 1}.");
                            imgui_SameLine();
                            
                            imgui_SetNextItemWidth(300f);
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

                        // Context menu: right-click to Edit/Delete or Stop Editing
                        // Open context menu on right-click only (1 = ImGuiMouseButton_Right)
                        if (imgui_BeginPopupContextItem($"##context_{itemUid}", 1))
                        {
                            if (!editing)
                            {
                                if (imgui_MenuItem("Edit"))
                                {
                                    _cfgInlineEditIndex = i;
                                    _cfgInlineEditBuffer = v;
                                }
                            }
                            else
                            {
                                if (imgui_MenuItem("Stop Editing"))
                                {
                                    _cfgInlineEditIndex = -1;
                                    _cfgInlineEditBuffer = string.Empty;
                                }
                            }
                            if (imgui_MenuItem("Delete"))
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
                                imgui_EndPopup();
                                // Don't break here, continue to render all items
                            }
                            imgui_EndPopup();
                        }
                        
                        // If a change was made, we need to refresh the parts list for subsequent iterations
                        if (listChanged)
                        {
                            // Re-get the values after modification
                            var updatedKd = selectedSection.Keys.GetKeyData(_cfgSelectedKey ?? string.Empty);
                            parts = GetValues(updatedKd);
                            listChanged = false; // Reset the flag
                            // Adjust the loop counter since we've removed an item
                            i--;
                        }
                    }

                    // New-row inline editor when adding at end
                    if (!listChanged && _cfgInlineEditIndex >= 0 && _cfgInlineEditIndex == parts.Count)
                    {
                        // Render input for a new value (not yet persisted)
                        if (imgui_InputText($"##edit_new_{_cfgSelectedSection}_{_cfgSelectedKey}", _cfgInlineEditBuffer))
                        {
                            _cfgInlineEditBuffer = imgui_InputText_Get($"##edit_new_{_cfgSelectedSection}_{_cfgSelectedKey}");
                        }
                        imgui_SameLine();
                        if (imgui_Button("Save##new"))
                        {
                            string newText = _cfgInlineEditBuffer ?? string.Empty;
                            var pdAct = GetActiveCharacterIniData();
                            var selSec = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                            var key = selSec?.Keys.GetKeyData(_cfgSelectedKey);
                            if (key != null)
                            {
                                var vals = GetValues(key);
                                // Only add non-empty values to avoid accidental blank rows
                                if (!string.IsNullOrWhiteSpace(newText))
                                {
                                    vals.Add(newText);
                                    WriteValues(key, vals);
                                    listChanged = true;
                                }
                            }
                            _cfgInlineEditIndex = -1;
                            _cfgInlineEditBuffer = string.Empty;
                        }
                        imgui_SameLine();
                        if (imgui_Button("Cancel##new"))
                        {
                            _cfgInlineEditIndex = -1;
                            _cfgInlineEditBuffer = string.Empty;
                        }
                    }

                    // Add new value with improved styling
                    if (!listChanged && _cfgInlineEditIndex == -1)
                    {
                        imgui_Separator();
                        imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Add New Values");
                        
                        imgui_SameLine();
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
                        
                        // Special section buttons with icons
                        imgui_Separator();
                        
                        // Heals: Tank / Important Bot → pick from connected toons
                        bool isHeals = string.Equals(_cfgSelectedSection, "Heals", StringComparison.OrdinalIgnoreCase);
                        bool isTankKey = string.Equals(_cfgSelectedKey, "Tank", StringComparison.OrdinalIgnoreCase);
                        bool isImpKey = string.Equals(_cfgSelectedKey, "Important Bot", StringComparison.OrdinalIgnoreCase);
                        if (isHeals && (isTankKey || isImpKey))
                        {
                            if (imgui_Button("👥 Pick Toons"))
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
                            imgui_SameLine();
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
                            imgui_SameLine();
                            if (imgui_Button("Sample If's"))
                            {
                                try { LoadSampleIfsForModal(); _cfgShowIfSampleModal = true; }
                                catch (Exception ex) { _cfgIfSampleStatus = "Load failed: " + (ex.Message ?? "error"); _cfgShowIfSampleModal = true; }
                            }
                        }
                    }
                }
            }
            imgui_EndChild();

            if (_cfgShowAddModal)
            {
                RenderAddFromCatalogModal(pd, selectedSection);
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

        // Background worker tick invoked from E3.Process(): handle catalog loads
        public static void ProcessBackgroundWork()
        {
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
                            
                            // If any peer fetch failed, fallback to local
                            if (!peerSuccess)
                            {
                                _cfg_CatalogStatus = "Peer catalog fetch failed; using local.";
                                isLocal = true;
                            }
                        }
                        
                        if (isLocal)
                        {
                            _cfg_CatalogStatus = "Loading catalogs (local)...";
                            mapSpells = OrganizeCatalog(FetchSpellDataList("${E3.SpellBook.ListAll}"));
                            mapAAs = OrganizeCatalog(FetchSpellDataList("${E3.AA.ListAll}"));
                            mapDiscs = OrganizeCatalog(FetchSpellDataList("${E3.Discs.ListAll}"));
                            mapSkills = OrganizeSkillsCatalog(FetchSpellDataList("${E3.Skills.ListAll}"));
                            mapItems = OrganizeItemsCatalog(FetchSpellDataList("${E3.ItemsWithSpells.ListAll}"));
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
                            string value = "";
                            if (toon.Equals(E3.CurrentName, StringComparison.OrdinalIgnoreCase))
                            {
                                // Get local value
                                var pd = GetActiveCharacterIniData();
                                value = pd?.Sections?.GetSectionData(section)?.Keys?.GetKeyData(key)?.Value ?? "";
                            }
                            else
                            {
                                // Request from peer
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
                long end = Core.StopWatch.ElapsedMilliseconds + 2000;
                while (Core.StopWatch.ElapsedMilliseconds < end)
                {
                    if (E3Core.Server.NetMQServer.SharedDataClient.TopicUpdates.TryGetValue(toon, out var topics))
                    {
                        if (topics.TryGetValue(topic, out var entry))
                        {
                            string payload = entry.Data;
                            // entry.Data includes "user:server:message"; peel off prefix
                            int first = payload.IndexOf(':');
                            int second = first >= 0 ? payload.IndexOf(':', first + 1) : -1;
                            string b64 = (second > 0 && second + 1 < payload.Length) ? payload.Substring(second + 1) : payload;
                            byte[] bytes = Convert.FromBase64String(b64);
                            var list = SpellDataList.Parser.ParseFrom(bytes);
                            data = list.Data;
                            return true;
                        }
                    }
                    System.Threading.Thread.Sleep(25);
                }
            }
            catch { }
            return false;
        }

        // Note: Remote Food/Drink inventory now uses non-blocking pending state above.

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
                    CastType = s.CastType.ToString()
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
                    Description = s.Description ?? string.Empty
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
                    CastType = s.CastType.ToString()
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
                l.Add(new E3Spell { Name = s.SpellName ?? string.Empty, Category = cat, Subcategory = sub, Level = s.Level });
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

        private static void RenderAddFromCatalogModal(IniData pd, SectionData selectedSection)
        {
            float totalW = Math.Max(880f, imgui_GetContentRegionAvailX());
            float listH = Math.Max(420f, imgui_GetContentRegionAvailY() * 0.8f);
            // Ensure window open flag is set when we intend to show it (allows reopening after X)
            imgui_Begin_OpenFlagSet("Add From Catalog", true);
            bool _open_Add = imgui_Begin("Add From Catalog", (int)ImGuiWindowFlags.ImGuiWindowFlags_None);
            if (_open_Add)
            {
                // Type tabs with better styling
                imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Select Spell Type");
                
                imgui_SameLine(); 
                if (imgui_Button(_cfgAddType == AddType.Spells ? "> Spells" : "Spells")) _cfgAddType = AddType.Spells;
                imgui_SameLine(); 
                if (imgui_Button(_cfgAddType == AddType.AAs ? "> AAs" : "AAs")) _cfgAddType = AddType.AAs;
                imgui_SameLine(); 
                if (imgui_Button(_cfgAddType == AddType.Discs ? "> Discs" : "Discs")) _cfgAddType = AddType.Discs;
                imgui_SameLine(); 
                if (imgui_Button(_cfgAddType == AddType.Skills ? "> Skills" : "Skills")) _cfgAddType = AddType.Skills;
                imgui_SameLine(); 
                if (imgui_Button(_cfgAddType == AddType.Items ? "> Items" : "Items")) _cfgAddType = AddType.Items;
                imgui_Separator();

                // Filter with better styling
                imgui_TextColored(0.8f, 0.9f, 0.95f, 1.0f, "Filter Spells");
                
                imgui_SameLine();
                if (imgui_InputText("Filter", _cfgAddFilter))
                    _cfgAddFilter = imgui_InputText_Get("Filter") ?? string.Empty;

                // Source map and columns
                var src = GetCatalogByType(_cfgAddType);
                var cats = src.Keys.ToList();
                cats.Sort(StringComparer.OrdinalIgnoreCase);
                float leftW = Math.Max(200f, totalW * 0.25f);
                float midW = Math.Max(200f, totalW * 0.25f);
                float thirdW = Math.Max(360f, totalW - leftW - midW - 24f);

                // Categories column
                imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Categories");
                
                if (imgui_BeginChild("CatList", leftW, listH, true))
                {
                    int ci = 0;
                    foreach (var c in cats)
                    {
                        bool sel = string.Equals(_cfgAddCategory, c, StringComparison.OrdinalIgnoreCase);
                        string label = $"{c}##Cat_{_cfgAddType}_{ci}";
                        if (imgui_Selectable(label, sel)) { _cfgAddCategory = c; _cfgAddSubcategory = string.Empty; }
                        ci++;
                    }
                }
                imgui_EndChild();

                imgui_SameLine();

                // Subcategories column
                imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Subcategories");
                
                if (imgui_BeginChild("SubList", midW, listH, true))
                {
                    if (!string.IsNullOrEmpty(_cfgAddCategory) && src.TryGetValue(_cfgAddCategory, out var submap))
                    {
                        var subs = submap.Keys.ToList(); subs.Sort(StringComparer.OrdinalIgnoreCase);
                        int si = 0;
                        foreach (var sc in subs)
                        {
                            bool sel = string.Equals(_cfgAddSubcategory, sc, StringComparison.OrdinalIgnoreCase);
                            string label = $"{sc}##Sub_{_cfgAddType}_{_cfgAddCategory}_{si}";
                            if (imgui_Selectable(label, sel)) { _cfgAddSubcategory = sc; }
                            si++;
                        }
                    }
                }
                imgui_EndChild();

                imgui_SameLine();

                // Entries column
                imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Spell Entries");
                
                if (imgui_BeginChild("EntryList", thirdW, listH, true))
                {
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
                    // Sort globally by Level (desc), then Name to stabilize order
                    entries = entries
                        .OrderByDescending(e => e.Level)
                        .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase);
                    int i = 0;
                    foreach (var e in entries)
                    {
                        // Row controls: Add | Info | Name (non-clickable text)
                        string uid = $"{_cfgAddType}_{_cfgAddCategory}_{_cfgAddSubcategory}_{i}";
                        
                        imgui_SameLine();
                        if (imgui_Button($"➕ Add##add_{uid}"))
                        {
                            var kd = selectedSection?.Keys?.GetKeyData(_cfgSelectedKey ?? string.Empty);
                            if (kd != null)
                            {
                                var vals = GetValues(kd);
                                string toAdd = (e.Name ?? string.Empty).Trim();
                                if (!vals.Contains(toAdd, StringComparer.OrdinalIgnoreCase))
                                {
                                    vals.Add(toAdd);
                                    WriteValues(kd, vals);
                                }
                            }
                        }
                        imgui_SameLine();
                        if (imgui_Button($"ℹ️ Info##info_{uid}"))
                        {
                            _cfgSpellInfoSpell = e;
                            _cfgShowSpellInfoModal = true;
                        }
                        imgui_SameLine();
                        
                        // Display spell info with level
                        string spellDisplay = $"📦 [{e.Level}] {e.Name}";
                        imgui_Text(spellDisplay);
                        i++;
                    }
                    if (i == 0) imgui_Text("❌ No entries found");
                }
                imgui_EndChild();

                // Footer actions with better styling
                imgui_Separator();
                imgui_TextColored(0.9f, 0.95f, 1.0f, 1.0f, "Quick Actions");
                
                imgui_SameLine();
                if (imgui_Button("📦 Add All Visible"))
                {
                    TryAddVisibleEntriesToSelectedKey(selectedSection);
                }
                imgui_SameLine();
                if (imgui_Button("❌ Close")) { _cfgShowAddModal = false; }
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
            imgui_Begin_OpenFlagSet("Pick From Inventory", true);
            bool _open_fd = imgui_Begin("Pick From Inventory", (int)ImGuiWindowFlags.ImGuiWindowFlags_None);
            if (_open_fd)
            {
                imgui_Text($"Pick {_cfgFoodDrinkKey} from inventory");
                imgui_Separator();
                if (string.IsNullOrEmpty(_cfgFoodDrinkStatus))
                {
                    if (imgui_Button("Scan Inventory"))
                    {
                        _cfgFoodDrinkStatus = "Scanning...";
                        _cfgFoodDrinkScanRequested = true;
                    }
                }
                else
                {
                    imgui_Text(_cfgFoodDrinkStatus);
                }

                if (_cfgFoodDrinkCandidates.Count > 0)
                {
                    if (imgui_BeginChild("FoodDrinkList", Math.Max(200f, imgui_GetContentRegionAvailX()), Math.Max(300f, imgui_GetContentRegionAvailY() * 0.7f), true))
                    {
                        foreach (var item in _cfgFoodDrinkCandidates)
                        {
                            if (imgui_Selectable(item, false))
                            {
                                string picked = item;
                                var pdAct = GetActiveCharacterIniData();
                                var secData = pdAct.Sections.GetSectionData(_cfgSelectedSection);
                                var key = secData?.Keys.GetKeyData(_cfgSelectedKey);
                                if (key != null)
                                {
                                    var vals = GetValues(key);
                                    // Replace first value or add if empty
                                    if (vals.Count == 0) vals.Add(picked);
                                    else vals[0] = picked;
                                    WriteValues(key, vals);
                                }
                                _cfgShowFoodDrinkModal = false;
                            }
                        }
                    }
                    imgui_EndChild();
                }

                if (imgui_Button("Close"))
                {
                    _cfgShowFoodDrinkModal = false;
                }
                imgui_End();
            }
            if (!_open_fd) _cfgShowFoodDrinkModal = false;
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
            bool _open_info = imgui_Begin("Spell Information", (int)ImGuiWindowFlags.ImGuiWindowFlags_AlwaysAutoResize);
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

                float width = Math.Max(520f, imgui_GetContentRegionAvailX());
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
                    imgui_Text(s.Description);
                }

                imgui_Separator();
                if (imgui_Button("❌ Close")) { _cfgShowSpellInfoModal = false; _cfgSpellInfoSpell = null; }
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
        private static bool IsLikelyFood(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("bread") || n.Contains("meat") || n.Contains("pie") || n.Contains("cake") || n.Contains("cookie") || n.Contains("muffin") || n.Contains("stew") || n.Contains("cheese") || n.Contains("tart") || n.Contains("sausage") || n.Contains("soup") || n.Contains("steak");
        }
        private static bool IsLikelyDrink(string name)
        {
            string n = name.ToLowerInvariant();
            return n.Contains("water") || n.Contains("milk") || n.Contains("wine") || n.Contains("ale") || n.Contains("beer") || n.Contains("tea") || n.Contains("juice") || n.Contains("elixir") || n.Contains("nectar") || n.Contains("brew");
        }
    }
}

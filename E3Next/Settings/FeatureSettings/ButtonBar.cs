using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;

namespace E3Core.Settings.FeatureSettings
{
    public class ButtonBar : BaseSettings, IBaseSettings
    {
        private string _filename = string.Empty;

        public class ButtonDef
        {
            public string Label;
            public string Command;
        }

        public class ButtonSet
        {
            public string Name;
            public List<ButtonDef> Buttons = new List<ButtonDef>();
        }

        // Default flat list (back-compat). Prefer Sets for multi-tab.
        public List<ButtonDef> Buttons = new List<ButtonDef>();
        public List<ButtonSet> Sets = new List<ButtonSet>();

        public class WindowDef
        {
            public string Id;
            public List<string> Sets = new List<string>();
            public bool Visible;
            public bool Locked;
            public bool HideTitleBar;
            public bool Compact;
            public bool AutoResize = true;
        }

        // Key: "Server_Character"; Value: list of windows
        public Dictionary<string, List<WindowDef>> WindowsByChar = new Dictionary<string, List<WindowDef>>(StringComparer.OrdinalIgnoreCase);

        public ButtonBar()
        {
            LoadData();
        }

        public void LoadData()
        {
            _filename = GetSettingsFilePath("E3 Buttons.ini");
            if (!string.IsNullOrEmpty(CurrentSet))
            {
                _filename = _filename.Replace(".ini", "_" + CurrentSet + ".ini");
            }

            IniData parsedData;
            var fileIniData = E3Core.Utility.e3util.CreateIniParser();

            try
            {
                if (!System.IO.File.Exists(_filename))
                {
                    if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                    {
                        System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                    }
                    parsedData = CreateSettings(_filename);
                }
                else
                {
                    parsedData = fileIniData.ReadFile(_filename);
                }
            }
            catch (Exception)
            {
                // Handle potential race creating file on multi-character startup
                System.Threading.Thread.Sleep(1000);
                if (!System.IO.File.Exists(_filename))
                {
                    if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                    {
                        System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                    }
                    parsedData = CreateSettings(_filename);
                }
                else
                {
                    parsedData = fileIniData.ReadFile(_filename);
                }
            }

            _fileLastModifiedFileName = _filename;
            _fileLastModified = System.IO.File.GetLastWriteTime(_filename);

            Buttons.Clear();
            Sets.Clear();
            WindowsByChar.Clear();

            // Back-compat default set from [Buttons]
            var section = parsedData.Sections["Buttons"];
            if (section != null)
            {
                var s = new ButtonSet { Name = "Default" };
                foreach (var key in section)
                {
                    string label = key.KeyName?.Trim() ?? string.Empty;
                    string command = ExtractFirstNonEmpty(key);
                    if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(command))
                    {
                        var bd = new ButtonDef { Label = label, Command = command };
                        Buttons.Add(bd);
                        s.Buttons.Add(bd);
                    }
                }
                if (s.Buttons.Count > 0) Sets.Add(s);
            }

            // Load any sections named "Set:Name" or "Set Name"
            foreach (var sec in parsedData.Sections)
            {
                var name = sec.SectionName?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(name)) continue;
                string setName = null;
                if (name.StartsWith("Set:", StringComparison.OrdinalIgnoreCase)) setName = name.Substring(4).Trim();
                else if (name.StartsWith("Set ", StringComparison.OrdinalIgnoreCase)) setName = name.Substring(4).Trim();
                if (string.IsNullOrEmpty(setName)) continue;

                var set = new ButtonSet { Name = setName };
                foreach (var key in sec.Keys)
                {
                    string label = key.KeyName?.Trim() ?? string.Empty;
                    string command = ExtractFirstNonEmpty(key);
                    if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(command))
                    {
                        set.Buttons.Add(new ButtonDef { Label = label, Command = command });
                    }
                }
                if (set.Buttons.Count > 0) Sets.Add(set);
            }

            // Character-specific window layout: [Character:Server_Char]
            try
            {
                string server = E3Core.Processors.E3.ServerName ?? string.Empty;
                string name = E3Core.Processors.E3.CurrentName ?? string.Empty;
                string charKey = $"{server}_{name}";
                var charSec = parsedData.Sections[$"Character:{charKey}"];
                if (charSec != null)
                {
                    int count = 1;
                    var cntKey = charSec.GetKeyData("WindowCount");
                    if (cntKey != null && int.TryParse(ExtractFirstNonEmpty(cntKey), out var c) && c > 0 && c < 10) count = c;
                    var windows = new List<WindowDef>();
                    for (int i = 1; i <= count; i++)
                    {
                        var w = new WindowDef { Id = i.ToString() };
                        w.Visible = ParseBool(charSec.GetKeyData($"Window{i}Visible"), false);
                        w.Locked = ParseBool(charSec.GetKeyData($"Window{i}Locked"), false);
                        w.HideTitleBar = ParseBool(charSec.GetKeyData($"Window{i}HideTitleBar"), false);
                        w.Compact = ParseBool(charSec.GetKeyData($"Window{i}Compact"), false);
                        w.AutoResize = ParseBool(charSec.GetKeyData($"Window{i}AutoResize"), true);

                        var setsKey = charSec.GetKeyData($"Window{i}Sets");
                        var setsCsv = ExtractFirstNonEmpty(setsKey);
                        if (!string.IsNullOrWhiteSpace(setsCsv))
                        {
                            foreach (var sName in setsCsv.Split(new char[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var sn = sName.Trim();
                                if (sn.Length > 0) w.Sets.Add(sn);
                            }
                        }
                        // default: use all sets if none specified
                        if (w.Sets.Count == 0)
                        {
                            foreach (var s in Sets) w.Sets.Add(s.Name);
                        }
                        windows.Add(w);
                    }
                    WindowsByChar[charKey] = windows;
                }
            }
            catch { }
        }

        public IniData CreateSettings(string filename)
        {
            var parser = E3Core.Utility.e3util.CreateIniParser();
            var newFile = new IniData();

            newFile.Sections.AddSection("Buttons");
            var section = newFile.Sections.GetSectionData("Buttons");
            // Intentionally create empty; user can add labels -> commands
            // Also add an example set to mirror ButtonMaster style
            newFile.Sections.AddSection("Set:Primary");
            var setP = newFile.Sections.GetSectionData("Set:Primary");
            setP.Keys.AddKey("Burn (all)", "/bcaa //burn");
            setP.Keys.AddKey("Pause (all)", "/bcaa //multi ; /twist off ; /mqp on");
            newFile.Sections.AddSection("Set:Movement");
            var setM = newFile.Sections.GetSectionData("Set:Movement");
            setM.Keys.AddKey("Nav Target (bca)", "/bca //nav id ${Target.ID}");

            // Add a starter character layout for the current character
            try
            {
                string server = E3Core.Processors.E3.ServerName ?? string.Empty;
                string name = E3Core.Processors.E3.CurrentName ?? string.Empty;
                string charKey = $"Character:{server}_{name}";
                newFile.Sections.AddSection(charKey);
                var charSec = newFile.Sections.GetSectionData(charKey);
                charSec.Keys.AddKey("WindowCount", "1");
                charSec.Keys.AddKey("Window1Visible", "On");
                charSec.Keys.AddKey("Window1Locked", "Off");
                charSec.Keys.AddKey("Window1HideTitleBar", "Off");
                charSec.Keys.AddKey("Window1Compact", "Off");
                charSec.Keys.AddKey("Window1AutoResize", "On");
                charSec.Keys.AddKey("Window1Sets", "Primary,Movement");
            }
            catch { }

            if (!System.IO.File.Exists(filename))
            {
                if (!System.IO.Directory.Exists(_configFolder + _settingsFolder))
                {
                    System.IO.Directory.CreateDirectory(_configFolder + _settingsFolder);
                }
                _log.Write($"Creating new E3 Buttons file:{filename}");
                parser.WriteFile(filename, newFile);
            }
            else
            {
                var fileIniData = E3Core.Utility.e3util.CreateIniParser();
                return fileIniData.ReadFile(filename);
            }

            return newFile;
        }

        private static string ExtractFirstNonEmpty(KeyData key)
        {
            if (key == null) return string.Empty;
            if (key.ValueList != null && key.ValueList.Count > 0)
            {
                foreach (var v in key.ValueList)
                {
                    if (!string.IsNullOrWhiteSpace(v)) return v;
                }
            }
            return key.Value ?? string.Empty;
        }

        private static bool ParseBool(KeyData key, bool def)
        {
            string v = ExtractFirstNonEmpty(key);
            if (string.IsNullOrWhiteSpace(v)) return def;
            if (v.Equals("on", StringComparison.OrdinalIgnoreCase) || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("1")) return true;
            if (v.Equals("off", StringComparison.OrdinalIgnoreCase) || v.Equals("false", StringComparison.OrdinalIgnoreCase) || v.Equals("0")) return false;
            return def;
        }

        public void Save()
        {
            try
            {
                var parser = E3Core.Utility.e3util.CreateIniParser();
                var ini = new IniData();

                // Write sets
                foreach (var set in Sets)
                {
                    if (string.IsNullOrWhiteSpace(set.Name)) continue;
                    var secName = $"Set:{set.Name}";
                    ini.Sections.AddSection(secName);
                    var sec = ini.Sections.GetSectionData(secName);
                    foreach (var b in set.Buttons)
                    {
                        if (string.IsNullOrWhiteSpace(b?.Label)) continue;
                        var val = (b.Command ?? string.Empty).Replace("\r", string.Empty).Replace("\n", "\\n");
                        sec.Keys.AddKey(b.Label, val);
                    }
                }

                // Write character windows (current character only)
                string server = E3Core.Processors.E3.ServerName ?? string.Empty;
                string name = E3Core.Processors.E3.CurrentName ?? string.Empty;
                string charKey = $"{server}_{name}";
                if (WindowsByChar.TryGetValue(charKey, out var windows) && windows != null && windows.Count > 0)
                {
                    var secName = $"Character:{charKey}";
                    ini.Sections.AddSection(secName);
                    var sec = ini.Sections.GetSectionData(secName);
                    sec.Keys.AddKey("WindowCount", windows.Count.ToString());
                    for (int i = 0; i < windows.Count; i++)
                    {
                        var w = windows[i];
                        int idx = i + 1;
                        sec.Keys.AddKey($"Window{idx}Visible", w.Visible ? "On" : "Off");
                        sec.Keys.AddKey($"Window{idx}Locked", w.Locked ? "On" : "Off");
                        sec.Keys.AddKey($"Window{idx}HideTitleBar", w.HideTitleBar ? "On" : "Off");
                        sec.Keys.AddKey($"Window{idx}Compact", w.Compact ? "On" : "Off");
                        sec.Keys.AddKey($"Window{idx}AutoResize", w.AutoResize ? "On" : "Off");
                        if (w.Sets != null && w.Sets.Count > 0)
                        {
                            sec.Keys.AddKey($"Window{idx}Sets", string.Join(",", w.Sets));
                        }
                    }
                }

                // Ensure directory exists
                var dir = System.IO.Path.GetDirectoryName(_filename);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);

                parser.WriteFile(_filename, ini);

                _fileLastModifiedFileName = _filename;
                _fileLastModified = System.IO.File.GetLastWriteTime(_filename);
            }
            catch (Exception ex)
            {
                E3Core.Processors.E3.MQ.Write($"Error saving E3 Buttons.ini: {ex.Message}");
            }
        }
    }
}

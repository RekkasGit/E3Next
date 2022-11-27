using E3Core.Utility;
using IniParser.Model;
using IniParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using E3Core.Processors;
using MonoCore;

namespace E3Core.Settings.FeatureSettings
{
    public class TributeDataFile : BaseSettings
    {
        private Dictionary<string, bool> _tributeData;

        public TributeDataFile()
        {
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/tribute", x =>
            {
                if (x.args.Count == 0)
                {
                    return;
                }

                var useTribute = string.Equals(x.args[0], "On", StringComparison.OrdinalIgnoreCase);
                SaveData(Zoning.CurrentZone.ShortName, useTribute);
                ToggleTribute();
            });
        }

        public void LoadData()
        {
            string fileName = GetSettingsFilePath("Tribute Settings.ini");
            FileIniDataParser fileIniData = e3util.CreateIniParser();
            var parsedData = fileIniData.ReadFile(fileName);
            _tributeData = parsedData.Sections.SelectMany(s => s.Keys).Where(w => !w.KeyName.Contains("is for"))
                .ToDictionary(k => k.KeyName, v => string.Equals(v.Value, "On", StringComparison.OrdinalIgnoreCase));
            _fileLastModifiedFileName = fileName;
            _fileLastModified = System.IO.File.GetLastWriteTime(fileName);
        }

        public void ToggleTribute()
        {
            var tributeActive = MQ.Query<bool>("${Me.TributeActive}");
            if (_tributeData.TryGetValue(Zoning.CurrentZone.ShortName, out var useTribute))
            {
                if (useTribute && !tributeActive)
                {
                    if (MQ.Query<int>("${Me.CurrentFavor}") > 500)
                    {
                        SetTribute();
                    }
                    else
                    {
                        E3.Bots.Broadcast($"\arTribute is on for {Zoning.CurrentZone.Name}, but i don't have enough favor to turn it on :(");
                    }
                }
                else if (!useTribute && tributeActive)
                {
                    SetTribute();
                }
            }
            else
            {
                SaveData(Zoning.CurrentZone.ShortName, false);
            }
        }

        private void SetTribute()
        {
            MQ.Cmd("/keypress TOGGLE_TRIBUTEBENEFITWIN");
            MQ.Cmd("/notify TBW_PersonalPage TBWP_ActivateButton leftmouseup");
            MQ.Cmd("/keypress TOGGLE_TRIBUTEBENEFITWIN");
            var tributeActive = MQ.Query<bool>("${Me.TributeActive}");
            if (tributeActive)
            {
                MQ.Cmd("/echo I've entered a tribute zone; turning tribute on");
            }
            else
            {
                MQ.Cmd("/echo Not a tribute zone; turning tribute off");
            }
        }

        public void SaveData(string zoneName, bool useTribute)
        {
            string fileName = GetSettingsFilePath("Tribute Settings.ini");
            FileIniDataParser fileIniData = e3util.CreateIniParser();
            fileIniData.Parser.Configuration.AllowDuplicateKeys = false;
            fileIniData.Parser.Configuration.OverrideDuplicateKeys = false;
            var parsedData = fileIniData.ReadFile(fileName);
            var section = parsedData.Sections.GetSectionData(zoneName.Substring(0, 1));
            if (section != null)
            {
                section.Keys.RemoveKey(zoneName);
                section.Keys.AddKey(zoneName, useTribute ? "On" : "Off");
            }
            else
            {
                parsedData.Sections.AddSection(zoneName.Substring(0, 1));
                parsedData.Sections[zoneName.Substring(0, 1)].AddKey(zoneName, useTribute ? "On" : "Off");
            }
            fileIniData.WriteFile(fileName, parsedData);
            LoadData();
        }
    }
}

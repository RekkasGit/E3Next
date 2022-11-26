using E3Core.Utility;
using IniParser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E3Core.Settings.FeatureSettings
{
    public class SavedGroupDataFile : BaseSettings
    {
        private Dictionary<string, string[]> _savedGroups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public SavedGroupDataFile()
        {
            LoadData();
        }

        public void LoadData()
        {
            var savedGroupFilePath = GetSettingsFilePath("Saved Groups.ini");
            var parser = e3util.CreateIniParser();
            var parsedData = parser.ReadFile(savedGroupFilePath);
            _savedGroups = parsedData.Sections.ToDictionary(k => k.SectionName, v => v.Keys.Select(s => s.Value).ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string[]> GetData()
        {
            return _savedGroups;
        }

        public void SaveData(string groupName)
        {
            var server = MQ.Query<string>("${MacroQuest.Server}");
            var groupKey = server + "_" + groupName;
            var groupMemberCount = MQ.Query<int>("${Group.Members}");
            var savedGroupFilePath = GetSettingsFilePath("Saved Groups.ini");
            var parser = new FileIniDataParser();
            parser.Parser.Configuration.CaseInsensitive = true;
            var groupData = parser.ReadFile(savedGroupFilePath);
            groupData.Sections.RemoveSection(groupKey);
            groupData.Sections.AddSection(groupKey);
            for (int i = 1; i <= groupMemberCount; i++)
            {
                var member = MQ.Query<string>($"${{Group.Member[{i}]}}");
                groupData[groupKey].AddKey($"GroupMember#{i}", member);
            }
            parser.WriteFile(savedGroupFilePath, groupData);

            LoadData();
        }
    }
}

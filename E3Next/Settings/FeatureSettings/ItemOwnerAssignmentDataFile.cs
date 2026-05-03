using E3Core.Processors;
using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace E3Core.Settings.FeatureSettings
{
    public static class ItemOwnerAssignmentDataFile
    {
        public static readonly Dictionary<string, string> ItemOwners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static readonly object SyncRoot = new object();
        public static void LoadData()
        {
            lock (SyncRoot)
            {
                ItemOwners.Clear();

                string fileNameFullPath = BaseSettings.GetSettingsFilePath(GetFileName());
                if (!File.Exists(fileNameFullPath))
                {
                    EnsureFolderExists();
                    File.CreateText(fileNameFullPath).Dispose();
                    return;
                }

                FileIniDataParser parser = e3util.CreateIniParser();
                IniData parsedData = parser.ReadFile(fileNameFullPath);

                foreach (var section in parsedData.Sections)
                {
                    foreach (var key in section.Keys)
                    {
                        string itemName = key.KeyName?.Trim();
                        string ownerName = key.Value?.Trim();
                        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(ownerName))
                            continue;

                        ItemOwners[itemName] = ownerName;
                    }
                }
            }
        }

        public static string GetOwner(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return string.Empty;

            lock (SyncRoot)
            {
                return ItemOwners.TryGetValue(itemName.Trim(), out var owner) ? owner : string.Empty;
            }
        }

        public static Dictionary<string, string> GetAllOwners()
        {
            lock (SyncRoot)
            {
                return new Dictionary<string, string>(ItemOwners, StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void SetOwner(string itemName, string ownerName)
        {
            itemName = itemName?.Trim();
            ownerName = ownerName?.Trim();
            if (string.IsNullOrWhiteSpace(itemName))
                return;

            lock (SyncRoot)
            {
                if (string.IsNullOrWhiteSpace(ownerName))
                    ItemOwners.Remove(itemName);
                else
                    ItemOwners[itemName] = ownerName;

                SaveDataInternal();
            }
        }

        public static void ClearOwner(string itemName)
        {
            SetOwner(itemName, string.Empty);
        }

        private static void EnsureFolderExists()
        {
            string fileNameFullPath = BaseSettings.GetSettingsFilePath(GetFileName());
            string folder = Path.GetDirectoryName(fileNameFullPath);
            if (!string.IsNullOrWhiteSpace(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        private static void SaveDataInternal()
        {
            EnsureFolderExists();

            FileIniDataParser parser = e3util.CreateIniParser();
            IniData newFile = new IniData();

            foreach (char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_")
            {
                newFile.Sections.AddSection(c.ToString());
            }

            foreach (var kvp in ItemOwners.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                string sectionName = GetSectionName(kvp.Key);
                newFile.Sections.GetSectionData(sectionName).Keys.AddKey(kvp.Key, kvp.Value);
            }

            string fileNameFullPath = BaseSettings.GetSettingsFilePath(GetFileName());
            File.Delete(fileNameFullPath);
            parser.WriteFile(fileNameFullPath, newFile);
        }

        private static string GetSectionName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return "_";

            char first = char.ToUpperInvariant(itemName.Trim()[0]);
            if (first >= 'A' && first <= 'Z')
                return first.ToString();
            if (first >= '0' && first <= '9')
                return first.ToString();
            return "_";
        }

        private static string GetFileName()
        {
            return $"Item Owner Assignments_{E3.ServerName}.ini";
        }
    }
}

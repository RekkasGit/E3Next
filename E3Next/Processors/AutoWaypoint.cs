using E3Core.Settings;
using E3Core.Utility;
using MonoCore;
using E3Core.Data;
using System;
using IniParser;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser.Model;
using System.Collections;
using E3Core.Processors;

namespace E3Core.Processors
{

    public class Coordinate
    {
        public Double X;
        public Double Y;
        public Double Z;
    }


    public enum InstructionType
    {
        Move,
        Pause,
        ClearTargets
    }

    public class Instruction 
    {
        public InstructionType type;
        public object value;
    }

    public class ZoneWaypoint
    {
        public string name;
        public List<Instruction> instructions;
    }

    public static class AutoWaypoint
    {
        private static bool Enabled;
        private static ZoneWaypoint currentZone;
        private static int currentStep;
        private static Dictionary<string, ZoneWaypoint> zoneWaypoints;
        private static IMQ MQ = E3.MQ;
        private static string kLoggingPrefix = "AutoNav";
        private static string kAutoNavSettingsFilename = "AutoNav Settings.ini";
        [SubSystemInit]
        public static void Init()
        {
            RegisterEvents();
            CreateWaypoints();
        }
        private static void RegisterEvents()
        {
            EventProcessor.RegisterCommand("/autowaypoint", x => startRunWaypoints());
        }

        private static void CreateWaypoints()
        {
            zoneWaypoints = new Dictionary<string, ZoneWaypoint>();
            IniData autoNavData = ReadAutoNavIni();
            var enumerator = autoNavData.Sections.GetEnumerator();
            while (enumerator.MoveNext()) {
                LoadSettingsINISection(enumerator.Current);
            }
        }

        [ClassInvoke(Data.Class.All)]
        public static void CheckAutoWaypoint()
        {
            if (!Enabled) return;
            bool navActive = MQ.Query<bool>("${Navigation.Active}");
            if (navActive) return;
            if (ClearXTargets.Enabled) return;
            runWaypoints();
        }   

        private static void startRunWaypoints()
        {
            /// Don't use Zoning.CurrentZone in case the event handler it hasn't processed the zoning event yet.
            string zoneName = MQ.Query<string>("${Zone.ShortName}");
           if (zoneWaypoints.TryGetValue(zoneName, out currentZone)) {

            } else {
                MQ.Write($"{kLoggingPrefix} could not find a zone with the shortname matching {zoneName}.");
                return;
            }
            Enabled = true;
            currentStep = -1; 
            runWaypoints();
        }
        private static void runWaypoints()
        {
            currentStep += 1;
            if (currentStep >= currentZone.instructions.Count()) {
                Enabled = false;
                return;
            }
            Instruction curInstruction = currentZone.instructions[currentStep];
            switch (curInstruction.type) {
                case InstructionType.Move:
                    {
                        Coordinate coord = (Coordinate)curInstruction.value;
                        e3util.TryMoveToLoc(coord.X, coord.Y, coord.Z);
                        break;
                    }
                case InstructionType.ClearTargets:
                    {


                        ClearXTargets.MobToAttack = 0;
                        ClearXTargets.Enabled = true;
                        break;
                    }
                case InstructionType.Pause:
                    {
                        int delayAmount = (int)curInstruction.value;
                        MQ.Delay(delayAmount);
                        break;
                    }
                }        
        }

        private static IniData ReadAutoNavIni()
        {
            FileIniDataParser fileIniData = e3util.CreateIniParser();
            string filename = BaseSettings.GetSettingsFilePath(kAutoNavSettingsFilename);
            IniData parsedData = new IniData();
            if (System.IO.File.Exists(filename))
            {
                parsedData = fileIniData.ReadFile(filename);
            }
            return parsedData;
        }

        private static void LoadSettingsINISection(SectionData section)
        {
            if (zoneWaypoints.ContainsKey(section.SectionName))
            {
                MQ.Write($"{kLoggingPrefix} duplicate section key found: ${section.SectionName}");
                return;
            }

            List<Instruction> instructions = new List<Instruction>();

            foreach (KeyData curKey in section.Keys)
            {
                foreach (string value in curKey.ValueList) 
                {
                    Instruction curInstruction;
                    string[] instructionSplit = value.Split('|');
                    if (instructionSplit.Length != 2) return;
                    string instructionName = instructionSplit[0];
                    string instructionValue = instructionSplit[1];
                    if (instructionName.Equals("Move"))
                    {
                        string[] valueSplit = instructionValue.Split(',');
                        if (valueSplit.Count() != 3) continue;
                        double xVal;
                        double yVal;
                        double zVal;
                        Double.TryParse(valueSplit[0], out xVal);
                        Double.TryParse(valueSplit[1], out yVal);
                        Double.TryParse(valueSplit[2], out zVal);
                        Coordinate curCoord = new Coordinate() { X = xVal, Y = yVal, Z = zVal };
                        curInstruction = new Instruction() { type = InstructionType.Move, value = curCoord };
                    }
                    else if (instructionName.Equals("Pause"))
                    {
                        if (Int32.TryParse(instructionValue, out int pauseDuration))
                        {
                            curInstruction = new Instruction() { type = InstructionType.Pause, value = pauseDuration };
                        }
                        else
                        {
                            MQ.Write($"{kLoggingPrefix} invalid pause duration in section {section.SectionName}.");
                            continue;
                        }
                    }
                    else if (instructionName.Equals("ClearTargets"))
                    {
                        curInstruction = new Instruction() { type = InstructionType.ClearTargets, value = 0 };
                    }
                    else
                    {
                        MQ.Write($"{kLoggingPrefix} unexpected key read in section {section.SectionName}.");
                        continue;
                    }
                    instructions.Add(curInstruction);
                }

            }
            ZoneWaypoint zone = new ZoneWaypoint() {name = section.SectionName, instructions = instructions};
            zoneWaypoints.Add(section.SectionName, zone);
        }
    }
}

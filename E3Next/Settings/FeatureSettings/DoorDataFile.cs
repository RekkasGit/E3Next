using E3Core.Utility;
using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Settings.FeatureSettings
{
    public class DoorDataFile:BaseSettings
    {
        IniData _doorData;
        public void LoadData()
        {
            string fileName = GetSettingsFilePath(@"doors.ini");


            FileIniDataParser fileIniData = e3util.CreateIniParser();
            _doorData = fileIniData.ReadFile(fileName);

        }

        public Int32 ClosestDoorID()
        {

            string zoneshortname = MQ.Query<string>("${Zone.ShortName}");
           
            var section = _doorData.Sections[zoneshortname];
            if(section!=null)
            {
                Int32 closestID = -1;
                double closeDistance = 99999;
                for(Int32 i=1;i<= section.Count;i++)
                {
                    var keyData = section.GetKeyData(zoneshortname + "#" + i);
                    if(keyData!=null)
                    {
                        Int32 indexOfComma = keyData.Value.IndexOf(",");
                        if(indexOfComma>0)
                        {
                            string doorIDString = keyData.Value.Substring(0, indexOfComma);

                            Int32 doorID;
                            if(Int32.TryParse(doorIDString,out doorID))
                            {
                                MQ.Cmd($"/squelch /doortarget id {doorID}");
                                double currentDistance = MQ.Query<Double>("${DoorTarget.Distance}");
                                //bad id returned
                                if (currentDistance == -1) continue;



                                if (currentDistance < closeDistance)
                                {
                                    closeDistance = currentDistance;
                                    closestID = doorID;
                                    if(closeDistance<50)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
               
                return closestID;

            }
            return 0;
        }
    }
}

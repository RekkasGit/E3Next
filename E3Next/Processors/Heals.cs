using E3Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Processors
{
    public class Heals:BaseProcessor
    {

        [AdvSettingInvoke]
        public static void Check_Heals() 
        { 
            using (_log.Trace())
            {
                
            }
           
        }

    }
}

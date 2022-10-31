using E3Core.Processors;
using E3Core.Settings;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E3Core.Classes
{
    /// <summary>
    /// Properties and methods specific to the magician class
    /// </summary>
    public static class Magician
    {
        private static IMQ MQ = E3.MQ;

        /// <summary>
        /// Checks pets for items and re-equips if necessary.
        /// </summary>
        [ClassInvoke(Data.Class.Magician)]
        public static void CheckPetItems()
        {
            var primary = MQ.Query<int>("${Me.Pet.Primary}");
            if (primary == 0)
            {

            }
        }
    }
}

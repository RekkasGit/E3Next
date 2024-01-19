using System;
using System.Collections.Generic;

/// <summary>
/// Version 0.1
/// This file, is the 'core', or the mediator between you and MQ2Mono
/// MQ2Mono is just a simple C++ plugin to MQ, which exposes the
/// OnInit
/// OnPulse
/// OnIncomingChat
/// etc
/// Methods from MQ event framework, and meshes it in such a way to allow you to write rather straight forward C# code.
///
/// Included in this is a Logging/trace framework, Event Proecssor, MQ command object, etc
///
/// Your class is included in here with a simple .Process Method. This methoid will be called once every OnPulse from the plugin, or basically every frame of the EQ client.
/// All you code should *NOT* be in this file.
/// </summary>
namespace E3Core.MonoCore
{
    /// <summary>
    /// the actual object pooled spawn object that can be used in scripts.
    /// </summary>
    public class Spawn : IDisposable
    {
        private readonly byte[] _data = new byte[1024];
        private int _length = 0;

        public bool isDirty { get; set; }

        public static Spawn Aquire()
        {
            if (!StaticObjectPool.TryPop(out Spawn obj))
            {
                obj = new Spawn();
            }

            return obj;
        }

        static readonly Dictionary<string, string> _stringLookup = new Dictionary<string, string>();

        public void Init(byte[] data, int length)
        {
            isDirty = true;
            //used for remote debug, to send the representastion of the data over.
            Buffer.BlockCopy(data, 0, _data, 0, length);
            _length = length;
            //end of remote debug

            int cb = 0;
            ID = BitConverter.ToInt32(data, cb);
            cb += 4;
            AFK = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Aggressive = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Anonymous = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Blind = BitConverter.ToInt32(data, cb);
            cb += 4;
            BodyTypeID = BitConverter.ToInt32(data, cb);
            cb += 4;
            //bodytype desc
            int slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            //to prevent GC from chruning from destroying long lived string, keep a small collection of them
            //change to byte key based dictionary for even better?
            string tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out BodyTypeDesc))
            {
                _stringLookup.Add(tstring, tstring);
                BodyTypeDesc = tstring;
            }
            cb += slength;
            Buyer = BitConverter.ToBoolean(data, cb);
            cb += 1;
            ClassID = BitConverter.ToInt32(data, cb);
            cb += 4;
            //cleanname
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out CleanName))
            {
                _stringLookup.Add(tstring, tstring);
                CleanName = tstring;
            }
            cb += slength;
            ConColorID = BitConverter.ToInt32(data, cb);
            cb += 4;
            CurrentEndurnace = BitConverter.ToInt32(data, cb);
            cb += 4;
            CurrentHPs = BitConverter.ToInt32(data, cb);
            cb += 4;
            CurrentMana = BitConverter.ToInt32(data, cb);
            cb += 4;
            Dead = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //displayname
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out DisplayName))
            {
                _stringLookup.Add(tstring, tstring);
                DisplayName = tstring;
            }
            cb += slength;
            Ducking = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Feigning = BitConverter.ToBoolean(data, cb);
            cb += 1;
            GenderID = BitConverter.ToInt32(data, cb);
            cb += 4;
            GM = BitConverter.ToBoolean(data, cb);
            cb += 1;
            GuildID = BitConverter.ToInt32(data, cb);
            cb += 4;
            Heading = BitConverter.ToSingle(data, cb);
            cb += 4;
            Height = BitConverter.ToSingle(data, cb);
            cb += 4;

            Invis = BitConverter.ToBoolean(data, cb);
            cb += 1;
            IsSummoned = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Level = BitConverter.ToInt32(data, cb);
            cb += 4;
            Levitate = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Linkdead = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Look = BitConverter.ToSingle(data, cb);
            cb += 4;
            MasterID = BitConverter.ToInt32(data, cb);
            cb += 4;
            MaxEndurance = BitConverter.ToInt32(data, cb);
            cb += 4;
            MaxRange = BitConverter.ToSingle(data, cb);
            cb += 4;
            MaxRangeTo = BitConverter.ToSingle(data, cb);
            cb += 4;
            Mount = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Moving = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //name
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out Name))
            {
                _stringLookup.Add(tstring, tstring);
                Name = tstring;
            }
            cb += slength;
            Named = BitConverter.ToBoolean(data, cb);
            cb += 1;
            PctHps = BitConverter.ToInt32(data, cb);
            cb += 4;
            PctMana = BitConverter.ToInt32(data, cb);
            cb += 4;
            PetID = BitConverter.ToInt32(data, cb);
            cb += 4;
            PlayerState = BitConverter.ToInt32(data, cb);
            cb += 4;
            RaceID = BitConverter.ToInt32(data, cb);
            cb += 4;
            //RaceName
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out RaceName))
            {
                _stringLookup.Add(tstring, tstring);
                RaceName = tstring;
            }
            cb += slength;
            RolePlaying = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Sitting = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Sneaking = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Standing = BitConverter.ToBoolean(data, cb);
            cb += 1;
            Stunned = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //Suffix
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out Suffix))
            {
                _stringLookup.Add(tstring, tstring);
                Suffix = tstring;
            }
            cb += slength;
            Targetable = BitConverter.ToBoolean(data, cb);
            cb += 1;
            TargetOfTargetID = BitConverter.ToInt32(data, cb);
            cb += 4;
            Trader = BitConverter.ToBoolean(data, cb);
            cb += 1;
            //TypeDesc
            slength = BitConverter.ToInt32(data, cb);
            cb += 4;
            tstring = System.Text.Encoding.ASCII.GetString(data, cb, slength);
            if (!_stringLookup.TryGetValue(tstring, out TypeDesc))
            {
                _stringLookup.Add(tstring, tstring);
                TypeDesc = tstring;
            }
            cb += slength;
            Underwater = BitConverter.ToBoolean(data, cb);
            cb += 1;
            X = BitConverter.ToSingle(data, cb);
            cb += 4;
            Y = BitConverter.ToSingle(data, cb);
            cb += 4;
            Z = BitConverter.ToSingle(data, cb);
            cb += 4;
            playerX = BitConverter.ToSingle(data, cb);
            cb += 4;
            playerY = BitConverter.ToSingle(data, cb);
            cb += 4;
            playerZ = BitConverter.ToSingle(data, cb);
            cb += 4;
            DeityID = BitConverter.ToInt32(data, cb);
            cb += 4;
        }

        public int DeityID { get; private set; }
        public float playerZ { get; private set; }
        public float playerY { get; private set; }
        public float playerX { get; private set; }
        public float Z { get; private set; }
        public float Y { get; private set; }
        public float X { get; private set; }
        public bool Underwater { get; private set; }
        public string TypeDesc = String.Empty;
        public bool Trader { get; private set; }
        public int TargetOfTargetID { get; private set; }
        public bool Targetable { get; private set; }
        public string Suffix;
        public bool Stunned { get; private set; }
        public bool Standing { get; private set; }
        public bool Sneaking { get; private set; }
        public bool Sitting { get; private set; }
        public bool RolePlaying { get; private set; }
        public string RaceName; 
        public int RaceID { get; private set; }
        public int PlayerState { get; private set; }
        public int PetID { get; private set; }
        public int PctMana { get; private set; }
        public int PctHps { get; private set; }
        public bool Named { get; private set; }
        public string Name = String.Empty;
        public bool Moving { get; private set; }
        public bool Mount { get; private set; }
        public float MaxRangeTo { get; private set; }
        public float MaxRange { get; private set; }
        public int MaxEndurance { get; private set; }
        public int MasterID{ get; private set; }
        public float Look{ get; private set; }
        public bool Linkdead{ get; private set; }
        public bool Levitate{ get; private set; }
        public int Level{ get; private set; }
        public bool IsSummoned{ get; private set; }
        public bool Invis{ get; private set; }
        public int ID{ get; private set; }
        public float Height{ get; private set; }
        public float Heading{ get; private set; }
        public int GuildID{ get; private set; }
        public bool GM{ get; private set; }
        public int GenderID{ get; private set; }
        public string Gender
        {
            get
            {
                return GetGender(GenderID);
            }
        }

        public bool Feigning { get; private set; }
        public bool Ducking { get; private set; }
        public string DisplayName = string.Empty;
        public bool Dead { get; private set; }
        public int CurrentMana { get; private set; }
        public int CurrentHPs { get; private set; }
        public int CurrentEndurnace { get; private set; }
        public int ConColorID { get; private set; }
        public string ConColor
        {
            get
            {
                return GetConColor(ConColorID);
            }
        }
        public string CleanName = String.Empty;
        public int ClassID { get; private set; }
        public string ClassName
        {
            get
            {
                return ClassIDToName(ClassID);
            }
        }
        public string ClassShortName
        {
            get
            {
                return ClassIDToShortName(ClassID);
            }
        }
        public bool Anonymous { get; private set; }
        public bool AFK { get; private set; }
        public bool Aggressive { get; private set; }
        public int Blind { get; private set; }
        public int BodyTypeID { get; private set; }
        public string BodyTypeDesc = String.Empty;
        public bool Buyer { get; private set; }
        public double Distance3D
        {
            get
            {
                return GetDistance3D();
            }
        }
        public double Distance
        {
            get
            {
                return GetDistance();
            }
        }
        private string GetConColor(int ConColorID)
        {
            switch (ConColorID)
            {
                case 0x06:
                    return "GREY";
                case 0x02:
                    return "GREEN";
                case 0x12:
                    return "LIGHT BLUE";
                case 0x04:
                    return "BLUE";
                case 0x0a:
                    return "WHITE";
                case 0x0f:
                    return "YELLOW";
                case 0x0d:
                    return "RED";
                default:
                    return "RED";
            }
        }
        private string GetGender(int genderID)
        {
            switch (genderID)
            {
                case 0:
                    return "male";
                case 1:
                    return "female";
                case 2:
                    return "neuter";
                case 3:
                    return "unknown";
            }
            return String.Empty;
        }

        private double GetDistance3D()
        {
            double dx = playerX - X;
            double dy = playerY - Y;
            double dz = playerZ - Z;

            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private double GetDistance()
        {
            double dx = X - playerX;
            double dy = Y - playerY;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private string ClassIDToShortName(int classID)
        {
            switch (classID)
            {
                case 1:
                    return "WAR";
                case 2:
                    return "CLR";
                case 3:
                    return "PAL";
                case 4:
                    return "RNG";
                case 5:
                    return "SHD";
                case 6:
                    return "DRU";
                case 7:
                    return "MNK";
                case 8:
                    return "BRD";
                case 9:
                    return "ROG";
                case 10:
                    return "SHM";
                case 11:
                    return "NEC";
                case 12:
                    return "WIZ";
                case 13:
                    return "MAG";
                case 14:
                    return "ENC";
                case 15:
                    return "BST";
                case 16:
                    return "BER";
            }
            return String.Empty;
        }

        private string ClassIDToName(int ClassID)
        {
            switch (ClassID)
            {
                case 1:
                    return "Warrior";
                case 2:
                    return "Cleric";
                case 3:
                    return "Paladin";
                case 4:
                    return "Ranger";
                case 5:
                    return "Shadowknight";
                case 6:
                    return "Druid";
                case 7:
                    return "Monk";
                case 8:
                    return "Bard";
                case 9:
                    return "Rogue";
                case 10:
                    return "Shaman";
                case 11:
                    return "Necromancer";
                case 12:
                    return "Wizard";
                case 13:
                    return "Mage";
                case 14:
                    return "Enchanter";
                case 15:
                    return "Beastlord";
                case 16:
                    return "Berserker";
            }

            return String.Empty;
        }

        public void Dispose()
        {
            StaticObjectPool.Push(this);
        }
    }
}
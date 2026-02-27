using System;

namespace E3Core.Classes
{
    public class MemoryStats
    {
        public string CharacterName { get; set; }
        public double CSharpMemoryMB { get; set; }
        public double EQCommitSizeMB { get; set; }
        public DateTime Timestamp { get; set; }

        public MemoryStats()
        {
            Timestamp = DateTime.Now;
        }

        public MemoryStats(string characterName, double cSharpMemoryMB, double eqCommitSizeMB)
        {
            CharacterName = characterName;
            CSharpMemoryMB = cSharpMemoryMB;
            EQCommitSizeMB = eqCommitSizeMB;
            Timestamp = DateTime.Now;
        }
    }
}

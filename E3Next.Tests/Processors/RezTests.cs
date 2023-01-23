using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using E3Core.Data;
using E3Core.Processors;
using E3Core.Settings;
using MonoCore;
using Moq;
using Xunit;

namespace E3Next.Tests.Processors
{
    [CollectionDefinition("Rez Tests", DisableParallelization = true)]
    public class RezTests
    {
        public class MQMock : Mock<IMQ>, IDisposable
        {
            public MQMock(string workingDirectory)
            {
                WorkingDirectory = workingDirectory;
            }
            
            public string WorkingDirectory { get; private set; }

            // Cleanup any temp files created for testing
            public void Dispose()
            {
                Directory.Delete(WorkingDirectory, true);
            }
        }
        
        public class SpellArgs
        {
            public string Name { get; set; }
            public bool Ready { get; set; }
            public bool InInventory { get; set; }
            public bool Learned { get; set; }
            public CastType Type { get; set; }
            public string EffectType { get; set; }
            public string SpellType { get; set; }
            
            public int InvSlot { get; set; }
            public int BagSlot { get; set; }

        }
        
        // So many possible MQ expressions in this call tree...
        private static void SetupSpellQueries(Mock<IMQ> mq, SpellArgs args)
        {
            switch (args.Type)
            {
                case CastType.Item:
                    mq.Setup(m => m.Query<bool>($"${{FindItem[={args.Name}]}}"))
                        .Returns(args.Type == CastType.Item && args.Ready && args.InInventory);
                    mq.Setup(m => m.Query<int>($"${{FindItem[={args.Name}].ItemSlot}}"))
                        .Returns(args.InvSlot);
                    mq.Setup(m => m.Query<int>($"${{FindItem[={args.Name}].ItemSlot2}}"))
                        .Returns(args.BagSlot);
                    mq.Setup(m => m.Query<string>($"${{Me.Inventory[{args.InvSlot}].EffectType}}"))
                        .Returns(args.EffectType);
                    mq.Setup(m => m.Query<string>($"${{Me.Inventory[{args.InvSlot}].Item[{args.BagSlot}].EffectType}}"))
                        .Returns(args.EffectType);
                    mq.Setup(m => m.Query<bool>($"${{Me.ItemReady[{args.Name}]}}"))
                        .Returns(args.Ready);
                    break;
                case CastType.AA:
                    mq.Setup(m => m.Query<bool>($"${{Me.AltAbility[{args.Name}].Spell}}"))
                        .Returns(args.Type == CastType.AA && args.Learned);
                    mq.Setup(m => m.Query<bool>($"${{Me.AltAbility[{args.Name}]}}"))
                        .Returns(args.Type == CastType.AA && args.Ready && args.Learned);
                    mq.Setup(m => m.Query<bool>($"${{Spell[{args.Name}]}}"))
                        .Returns(args.Type == CastType.AA && args.Learned);
                    mq.Setup(m => m.Query<string>($"${{Spell[{args.Name}].SpellType}}"))
                        .Returns(args.SpellType);
                    mq.Setup(m => m.Query<bool>($"${{Me.AltAbilityReady[{args.Name}]}}"))
                        .Returns(args.Ready);
                    break;
                case CastType.Spell:
                    mq.Setup(m => m.Query<bool>($"${{Me.Book[{args.Name}]}}"))
                        .Returns(args.Type == CastType.Spell && args.Ready && args.Learned);
                    mq.Setup(m => m.Query<bool>($"${{Spell[{args.Name}]}}"))
                        .Returns(args.Type == CastType.Spell && args.Learned);
                    mq.Setup(m => m.Query<string>($"${{Spell[{args.Name}].SpellType}}"))
                        .Returns(args.SpellType);
                    mq.Setup(m => m.Query<bool>($"${{Me.SpellReady[{args.Name}]}}"))
                        .Returns(args.Ready);

                    break;
            }
        }

        private static MQMock GetMQMock()
        {
            var workingDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(workingDir);

            var macrosDir = Path.Combine(workingDir, "macros");
            Directory.CreateDirectory(macrosDir);
            
            var configDir = Path.Combine(workingDir, "config");
            Directory.CreateDirectory(configDir);
            
            var mock = new MQMock(workingDir);
            mock.Setup(m => m.Query<string>("${MacroQuest.Path[macros]}"))
                .Returns(macrosDir);
        
            mock.Setup(m => m.Query<string>("${MacroQuest.Path[config]}"))
                .Returns(configDir);

            // Explicit handling for Divine Rez since it is hardcoded
            SetupSpellQueries(mock, new SpellArgs
            {
                Name = "Divine Resurrection",
                Type = CastType.AA
            });
            
            return mock;
        }
        
        private SpellArgs[] GetTestingSpellArgs()
        {
            return new[]
            {
                new SpellArgs
                {
                    Name = "Rez Doodad",
                    Type = CastType.Item,
                    Learned = false,
                    InInventory = false,
                    InvSlot = -1,
                    BagSlot = -1,
                    Ready = true,
                    SpellType = "Beneficial",
                    EffectType = "Click Inventory"
                },
                new SpellArgs{
                    Name = "Some AA",
                    Type = CastType.AA,
                    Learned = false,
                    InInventory = false,
                    InvSlot = -1,
                    BagSlot = -1,
                    Ready = false,
                    SpellType = "Beneficial",
                    EffectType = null
                },
                new SpellArgs{
                    Name = "Spell On CD",
                    Type = CastType.Spell,
                    Learned = false,
                    InInventory = false,
                    InvSlot = -1,
                    BagSlot = -1,
                    Ready = false,
                    SpellType = "Beneficial",
                    EffectType = null
                },
                new SpellArgs{
                    Name = "Spell Off CD",
                    Type = CastType.Spell,
                    Learned = false,
                    InInventory = false,
                    InvSlot = -1,
                    BagSlot = -1,
                    Ready = true,
                    SpellType = "Beneficial",
                    EffectType = null
                }
            };
        }

        private static void Setup(IMQ mqInstance)
        {
            E3.MQ = mqInstance;
            E3.Log = new Logging(mqInstance);
                
            Spell.MQ = mqInstance;
            // HACK: We are keeping a private static reference to a public static property for both E3.MQ and E3.Spawns, this is really asking
            // for trouble with initialization order down the line. For now just force set the value via reflection for testing purposes.
            typeof(Rez).GetField("MQ", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, mqInstance);
            typeof(Casting).GetField("MQ", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, mqInstance);

            // Setup rez spell list
            E3.GeneralSettings = new GeneralSettings(false);
                
            E3.CharacterSettings = new CharacterSettings(false);
        }

        [Fact]
        public void GetAvailableRezSpell_ExcludesSpellsOnCD()
        {
            var mqMock = GetMQMock();
            try
            {
                Setup(mqMock.Object);
                var args = GetTestingSpellArgs();
                foreach (var arg in args)
                {
                    arg.Name += "__";
                }
                
                E3.CharacterSettings.RezSpells = new List<string>(args.Select(a => a.Name).ToList());
                
                // Know both "Spell on CD and Spell off CD" but on cd is ...on CD
                args[2].Learned = true;
                args[2].Ready = false;
                
                args[3].Learned = true;
                args[3].Ready = true;

                SetupSpellQueries(mqMock, args[0]);
                SetupSpellQueries(mqMock, args[1]);
                SetupSpellQueries(mqMock, args[2]);
                SetupSpellQueries(mqMock, args[3]);

                var result = Rez.GetAvailableRezSpell();
                Assert.Equal(E3.CharacterSettings.RezSpells[3], result.SpellName);
            }
            finally
            {
                mqMock.Dispose();
            }
        }

        [Fact]
        public void GetAvailableRezSpell_UsesItemsIfInInventory()
        {
            var mqMock = GetMQMock();
            try
            {
                Setup(mqMock.Object);
                var args = GetTestingSpellArgs();
                E3.CharacterSettings.RezSpells = new List<string>(args.Select(a => a.Name).ToList());

                args[0].InInventory = true;
                args[1].BagSlot = -1;
                args[1].InvSlot = 1;

                SetupSpellQueries(mqMock, args[0]);
                SetupSpellQueries(mqMock, args[1]);
                SetupSpellQueries(mqMock, args[2]);
                SetupSpellQueries(mqMock, args[3]);

                var result = Rez.GetAvailableRezSpell();
                Assert.Equal(E3.CharacterSettings.RezSpells[0], result.CastName);
            }
            finally
            {
                mqMock.Dispose();
            }
        }
        
        [Fact]
        public void GetAvailableRezSpell_UsesItemsInBags()
        {
            var mqMock = GetMQMock();
            try
            {
                Setup(mqMock.Object);
                var args = GetTestingSpellArgs();
                E3.CharacterSettings.RezSpells = new List<string>(args.Select(a => a.Name).ToList());

                args[0].InInventory = true;
                args[1].BagSlot = 1;
                args[1].InvSlot = 1;

                SetupSpellQueries(mqMock, args[0]);
                SetupSpellQueries(mqMock, args[1]);
                SetupSpellQueries(mqMock, args[2]);
                SetupSpellQueries(mqMock, args[3]);

                var result = Rez.GetAvailableRezSpell();
                Assert.Equal(E3.CharacterSettings.RezSpells[0], result.CastName);
            }
            finally
            {
                mqMock.Dispose();
            }
        }
        
        [Fact]
        public void GetAvailableRezSpell_UsesAA()
        {
            var mqMock = GetMQMock();
            try
            {
                Setup(mqMock.Object);
                var args = GetTestingSpellArgs();
                E3.CharacterSettings.RezSpells = new List<string>(args.Select(a => a.Name).ToList());

                args[1].Learned = true;
                args[1].Ready = true;

                SetupSpellQueries(mqMock, args[0]);
                SetupSpellQueries(mqMock, args[1]);
                SetupSpellQueries(mqMock, args[2]);
                SetupSpellQueries(mqMock, args[3]);

                var result = Rez.GetAvailableRezSpell();
                Assert.Equal(E3.CharacterSettings.RezSpells[1], result.CastName);
            }
            finally
            {
                mqMock.Dispose();
            }
        }
    }
}
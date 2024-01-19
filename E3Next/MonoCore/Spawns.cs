using System;
using System.Collections.Generic;
using System.Threading;

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
    /// Used to download spawns from MQ in a quick manner to be used in scripts.
    /// </summary>
    public class Spawns : ISpawns
    {
        //special list so we can get rid of the non dirty values
        private static List<Spawn> _tmpSpawnList = new List<Spawn>();

        public static List<Spawn> _spawns = new List<Spawn>(2048);
        private static readonly Dictionary<string, Spawn> _spawnsByName = new Dictionary<string, Spawn>(2048, StringComparer.OrdinalIgnoreCase);
        public static readonly Dictionary<int, Spawn> SpawnsByID = new Dictionary<int, Spawn>(2048);

        public static long RefreshTimePeriodInMS { get; set; } = 1000;
        private static long _lastRefesh = 0;

        public void AddSpace(Spawn spawn)
        {
            _spawns.Add(spawn);
        }

        public bool TryByID(int id, out Spawn s)
        {
            RefreshListIfNeeded();
            return SpawnsByID.TryGetValue(id, out s);
        }

        public bool TryByName(string name, out Spawn s)
        {
            RefreshListIfNeeded();
            return _spawnsByName.TryGetValue(name, out s);
        }

        public int GetIDByName(string name)
        {
            RefreshListIfNeeded();
            return _spawnsByName.TryGetValue(name, out Spawn returnValue) ? returnValue.ID : 0;
        }

        public bool Contains(string name)
        {
            RefreshListIfNeeded();
            return _spawnsByName.ContainsKey(name);
        }

        public bool Contains(int id)
        {
            RefreshListIfNeeded();
            return SpawnsByID.ContainsKey(id);
        }

        public IEnumerable<Spawn> Get()
        {
            RefreshListIfNeeded();
            return _spawns;
        }

        private void RefreshListIfNeeded()
        {
            if (_spawns.Count == 0 || Core.StopWatch.ElapsedMilliseconds - _lastRefesh > RefreshTimePeriodInMS)
            {
                RefreshList();
            }
        }

        /// <summary>
        /// warning, only do this during shutdown.
        /// </summary>
        public void EmptyLists()
        {
            _spawnsByName.Clear();
            SpawnsByID.Clear();
            _spawns.Clear();
        }

        public void RefreshList()
        {
            //need to mark everything not dirty so we know what get spawns gets us.
            foreach (var spawn in _spawns)
            {
                spawn.isDirty = false;
            }

            //request new spawns!
            Core.mq_GetSpawns();

            //spawns has new/updated data, get rid of the non dirty stuff.
            //can use the other dictionaries to help
            _spawnsByName.Clear();
            SpawnsByID.Clear();
            foreach (var spawn in _spawns)
            {
                if (spawn.isDirty)
                {
                    _tmpSpawnList.Add(spawn);
                    if (spawn.TypeDesc == "PC")
                    {
                        if (!_spawnsByName.ContainsKey(spawn.Name))
                        {
                            _spawnsByName.Add(spawn.Name, spawn);
                        }
                    }
                    SpawnsByID.Add(spawn.ID, spawn);
                }
                else
                {
                    spawn.Dispose();
                }
            }

            _spawns.Clear();

            //swap the collections
            _tmpSpawnList = Interlocked.Exchange(ref _spawns, _tmpSpawnList);

            //clear the dictionaries and rebuild.
            //_spawns should have fresh data now!
            _lastRefesh = Core.StopWatch.ElapsedMilliseconds;
        }
    }
}
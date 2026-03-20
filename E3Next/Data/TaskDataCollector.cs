using E3Core.Processors;
using Google.Protobuf;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E3Core.Data
{
    public static class TaskDataCollector
    {
        public const int MaxTaskSlots = 30;
        public const int MaxObjectiveSlots = 20;

        public static List<TaskSnapshot> Capture(IMQ mq, bool allowDelays = true)
        {
            var results = new List<TaskSnapshot>();
            if (mq == null) return results;

            for (int slot = 1; slot <= MaxTaskSlots; slot++)
            {
                var task = CaptureTask(mq, slot, allowDelays);
                if (task != null)
                {
                    results.Add(task);
                }
            }

            return results;
        }

        public static string SerializeForWire(IEnumerable<TaskSnapshot> tasks)
        {
            if (tasks == null) return string.Empty;

            var taskList = new TaskDataList();
            foreach (var task in tasks)
            {
                taskList.Data.Add(task.ToProto());
            }

            return Convert.ToBase64String(taskList.ToByteArray());
        }

        public static List<TaskWireSummary> DeserializeFromWire(string payload)
        {
            var results = new List<TaskWireSummary>();
            if (string.IsNullOrWhiteSpace(payload)) return results;

            try
            {
                var taskList = new TaskDataList();
                taskList.MergeFrom(ByteString.FromBase64(payload));

                foreach (var task in taskList.Data)
                {
                    results.Add(task.ToWireSummary());
                }
            }
            catch
            {
            }

            return results;
        }

        private static TaskSnapshot CaptureTask(IMQ mq, int slot, bool allowDelays)
        {
            string title = SafeQueryString(mq, $"${{Task[{slot}].Title}}", allowDelays);
            if (string.IsNullOrWhiteSpace(title) || string.Equals(title, "NULL", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var task = new TaskSnapshot
            {
                Slot = slot,
                Title = title,
                Type = SafeQueryString(mq, $"${{Task[{slot}].Type}}", allowDelays),
                ActiveStep = SafeQueryString(mq, $"${{Task[{slot}].Step.Instruction}}", allowDelays),
                MemberCount = SafeQuery<int>(mq, $"${{Task[{slot}].Members}}", allowDelays),
                Leader = SafeQueryString(mq, $"${{Task[{slot}].Leader.Name}}", allowDelays)
            };

            long timerSeconds = SafeQuery<long>(mq, $"${{Task[{slot}].Timer.TotalSeconds}}", allowDelays);
            if (timerSeconds > 0)
            {
                task.TimerSeconds = timerSeconds;
                task.TimerDisplay = SafeQueryString(mq, $"${{Task[{slot}].Timer.TimeDHM}}", allowDelays);
            }

            CaptureObjectives(mq, allowDelays, slot, task);
            return task;
        }

        private static void CaptureObjectives(IMQ mq, bool allowDelays, int slot, TaskSnapshot task)
        {
            for (int objectiveIndex = 1; objectiveIndex <= MaxObjectiveSlots; objectiveIndex++)
            {
                string instruction = SafeQueryString(mq, $"${{Task[{slot}].Objective[{objectiveIndex}].Instruction}}", allowDelays);
                if (string.IsNullOrWhiteSpace(instruction) || string.Equals(instruction, "NULL", StringComparison.OrdinalIgnoreCase))
                {
                    if (task.Objectives.Count > 0)
                    {
                        break;
                    }

                    continue;
                }

                var snapshot = new TaskObjectiveSnapshot
                {
                    Index = objectiveIndex,
                    Instruction = instruction,
                    Status = SafeQueryString(mq, $"${{Task[{slot}].Objective[{objectiveIndex}].Status}}", allowDelays),
                    Zone = SafeQueryString(mq, $"${{Task[{slot}].Objective[{objectiveIndex}].Zone}}", allowDelays),
                    Optional = SafeQuery<bool>(mq, $"${{Task[{slot}].Objective[{objectiveIndex}].Optional}}", allowDelays)
                };

                task.Objectives.Add(snapshot);
            }
        }

        private static T SafeQuery<T>(IMQ mq, string query, bool allowDelays)
        {
            try
            {
                return mq.Query<T>(query);
            }
            catch (ThreadAbort)
            {
                throw;
            }
            catch
            {
                return default;
            }
        }

        private static string SafeQueryString(IMQ mq, string query, bool allowDelays)
        {
            var result = SafeQuery<string>(mq, query, allowDelays);
            return string.IsNullOrEmpty(result) ? string.Empty : result.Trim();
        }
    }

    public static class TaskSnapshotExtensions
    {
        public static TaskData ToProto(this TaskSnapshot snapshot)
        {
            var taskData = new TaskData
            {
                Slot = snapshot.Slot,
                Title = snapshot.Title ?? string.Empty,
                ActiveStep = snapshot.ActiveStep ?? string.Empty,
                CompletedObjectives = snapshot.CompletedObjectives,
                TotalObjectives = snapshot.TotalObjectives,
                Type = snapshot.Type ?? string.Empty,
                IsComplete = snapshot.IsComplete,
                TimerDisplay = snapshot.TimerDisplay ?? string.Empty
            };

            foreach (var obj in snapshot.Objectives)
            {
                taskData.Objectives.Add(new TaskObjective
                {
                    Index = obj.Index,
                    Instruction = obj.Instruction ?? string.Empty,
                    Status = obj.Status ?? string.Empty,
                    Zone = obj.Zone ?? string.Empty,
                    Optional = obj.Optional
                });
            }

            return taskData;
        }
    }

    public static class TaskDataExtensions
    {
        public static TaskWireSummary ToWireSummary(this TaskData data)
        {
            var summary = new TaskWireSummary
            {
                Slot = data.Slot,
                Title = data.Title,
                ActiveStep = data.ActiveStep,
                CompletedObjectives = data.CompletedObjectives,
                TotalObjectives = data.TotalObjectives,
                Type = data.Type,
                IsComplete = data.IsComplete,
                TimerDisplay = data.TimerDisplay
            };

            foreach (var obj in data.Objectives)
            {
                summary.Objectives.Add(new TaskWireObjective
                {
                    Index = obj.Index,
                    Instruction = obj.Instruction,
                    Status = obj.Status,
                    Zone = obj.Zone,
                    Optional = obj.Optional
                });
            }

            return summary;
        }
    }

    public class TaskSnapshot
    {
        public int Slot { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string ActiveStep { get; set; } = string.Empty;
        public string TimerDisplay { get; set; } = string.Empty;
        public long TimerSeconds { get; set; }
        public int MemberCount { get; set; }
        public string Leader { get; set; } = string.Empty;
        public List<TaskObjectiveSnapshot> Objectives { get; } = new List<TaskObjectiveSnapshot>();

        public bool IsComplete => Objectives.Count > 0 && Objectives.TrueForAll(o => o.IsComplete);
        public int CompletedObjectives => Objectives.Count(o => o.IsComplete);
        public int TotalObjectives => Objectives.Count;
    }

    public class TaskObjectiveSnapshot
    {
        public int Index { get; set; }
        public string Instruction { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public bool Optional { get; set; }

        public bool IsComplete => string.Equals(Status, "Done", StringComparison.OrdinalIgnoreCase);
    }

    public class TaskWireSummary
    {
        public int Slot { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ActiveStep { get; set; } = string.Empty;
        public int CompletedObjectives { get; set; }
        public int TotalObjectives { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool IsComplete { get; set; }
        public string TimerDisplay { get; set; } = string.Empty;
        public List<TaskWireObjective> Objectives { get; } = new List<TaskWireObjective>();
    }

    public class TaskWireObjective
    {
        public int Index { get; set; }
        public string Instruction { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public bool Optional { get; set; }

        public bool IsComplete => string.Equals(Status, "Done", StringComparison.OrdinalIgnoreCase);
    }
}

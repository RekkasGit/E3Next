using E3Core.Processors;
using MonoCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E3Core.Data
{
    public static class TaskDataCollector
    {
        private const char WireFieldSeparator = '|';
        private const char WireEntrySeparator = ';';
        private const char WireObjectiveSeparator = '~';
        private const char WireObjectiveFieldSeparator = '^';

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

            var builder = new StringBuilder();
            foreach (var task in tasks)
            {
                if (builder.Length > 0)
                {
                    builder.Append(WireEntrySeparator);
                }

                builder.Append(task.Slot);
                builder.Append(WireFieldSeparator).Append(Encode(task.Title));
                builder.Append(WireFieldSeparator).Append(Encode(task.ActiveStep));
                builder.Append(WireFieldSeparator).Append(task.CompletedObjectives);
                builder.Append(WireFieldSeparator).Append(task.TotalObjectives);
                builder.Append(WireFieldSeparator).Append(Encode(task.Type));
                builder.Append(WireFieldSeparator).Append(task.IsComplete ? 1 : 0);
                builder.Append(WireFieldSeparator).Append(Encode(task.TimerDisplay));

                // Serialize objectives
                builder.Append(WireFieldSeparator);
                for (int i = 0; i < task.Objectives.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(WireObjectiveSeparator);
                    }

                    var obj = task.Objectives[i];
                    builder.Append(obj.Index);
                    builder.Append(WireObjectiveFieldSeparator).Append(Encode(obj.Instruction));
                    builder.Append(WireObjectiveFieldSeparator).Append(Encode(obj.Status));
                    builder.Append(WireObjectiveFieldSeparator).Append(Encode(obj.Zone));
                    builder.Append(WireObjectiveFieldSeparator).Append(obj.Optional ? 1 : 0);
                }
            }

            return builder.ToString();
        }

        public static List<TaskWireSummary> DeserializeFromWire(string payload)
        {
            var results = new List<TaskWireSummary>();
            if (string.IsNullOrWhiteSpace(payload)) return results;

            var entries = payload.Split(WireEntrySeparator);
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry)) continue;

                var fields = entry.Split(WireFieldSeparator);
                if (fields.Length < 8) continue;

                var summary = new TaskWireSummary
                {
                    Slot = ParseInt(fields[0]),
                    Title = Decode(fields[1]),
                    ActiveStep = Decode(fields[2]),
                    CompletedObjectives = ParseInt(fields[3]),
                    TotalObjectives = ParseInt(fields[4]),
                    Type = Decode(fields[5]),
                    IsComplete = ParseInt(fields[6]) == 1,
                    TimerDisplay = Decode(fields[7])
                };

                // Parse objectives if present (field index 8)
                if (fields.Length > 8 && !string.IsNullOrEmpty(fields[8]))
                {
                    var objectiveEntries = fields[8].Split(WireObjectiveSeparator);
                    foreach (var objEntry in objectiveEntries)
                    {
                        if (string.IsNullOrWhiteSpace(objEntry)) continue;

                        var objFields = objEntry.Split(WireObjectiveFieldSeparator);
                        if (objFields.Length < 5) continue;

                        summary.Objectives.Add(new TaskWireObjective
                        {
                            Index = ParseInt(objFields[0]),
                            Instruction = Decode(objFields[1]),
                            Status = Decode(objFields[2]),
                            Zone = Decode(objFields[3]),
                            Optional = ParseInt(objFields[4]) == 1
                        });
                    }
                }

                results.Add(summary);
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
                return mq.Query<T>(query, allowDelays);
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

        private static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static int ParseInt(string value)
        {
            if (int.TryParse(value, out var result))
            {
                return result;
            }

            return 0;
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

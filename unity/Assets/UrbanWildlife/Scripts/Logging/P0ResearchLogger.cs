using System;
using System.IO;
using UnityEngine;
using UrbanWildlife.Animals;
using UrbanWildlife.Cycle;
using UrbanWildlife.Humans;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Logging
{
    [RequireComponent(typeof(LayoutPacketReader), typeof(P0ConstraintManager), typeof(P0CycleController))]
    public sealed class P0ResearchLogger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Path relative to the Unity Assets folder, or an absolute path.")]
        private string outputRoot = "../../data/raw/research-logs";

        private LayoutPacketReader reader;
        private P0ConstraintManager constraints;
        private P0CycleController cycle;
        private P0HumanSimulation humans;
        private P0AnimalSimulation animals;
        private ResearchLogWriter writer;
        private string activeSessionId;

        public string CurrentJsonlPath => writer?.JsonlPath ?? string.Empty;
        public string CurrentCsvPath => writer?.CsvPath ?? string.Empty;

        private void Awake()
        {
            reader = GetComponent<LayoutPacketReader>();
            constraints = GetComponent<P0ConstraintManager>();
            cycle = GetComponent<P0CycleController>();
            humans = GetComponent<P0HumanSimulation>();
            animals = GetComponent<P0AnimalSimulation>();
        }

        private void OnEnable()
        {
            if (reader == null)
            {
                reader = GetComponent<LayoutPacketReader>();
            }
            if (constraints == null)
            {
                constraints = GetComponent<P0ConstraintManager>();
            }
            if (cycle == null)
            {
                cycle = GetComponent<P0CycleController>();
            }
            if (humans == null)
            {
                humans = GetComponent<P0HumanSimulation>();
            }
            if (animals == null)
            {
                animals = GetComponent<P0AnimalSimulation>();
            }

            reader.LayoutAccepted += OnLayoutAccepted;
            constraints.ConstraintsEvaluated += OnConstraintsEvaluated;
            cycle.PhaseChanged += OnPhaseChanged;
            if (animals != null)
            {
                animals.CycleMemoryRecorded += OnCycleMemoryRecorded;
            }
        }

        private void OnDisable()
        {
            if (reader != null)
            {
                reader.LayoutAccepted -= OnLayoutAccepted;
            }
            if (constraints != null)
            {
                constraints.ConstraintsEvaluated -= OnConstraintsEvaluated;
            }
            if (cycle != null)
            {
                cycle.PhaseChanged -= OnPhaseChanged;
            }
            if (animals != null)
            {
                animals.CycleMemoryRecorded -= OnCycleMemoryRecorded;
            }
        }

        private void OnCycleMemoryRecorded(P0CycleMemorySnapshot snapshot)
        {
            if (reader.LatestPacket == null)
            {
                return;
            }

            EnsureWriter(reader.LatestPacket.session_id);
            WriteRecord(
                "cycle_memory_recorded",
                cycle.Mechanics.MemoryEffectSummary(),
                constraints.LatestResult);
        }

        private void OnConstraintsEvaluated(P0ConstraintResult result)
        {
            LayoutPacket packet = reader.LatestPacket;
            if (packet == null)
            {
                return;
            }

            EnsureWriter(packet.session_id);
            WriteRecord("constraint_evaluated", "P0 planning constraints evaluated", result);
        }

        private void OnLayoutAccepted(LayoutPacket packet)
        {
            EnsureWriter(packet.session_id);
            WriteRecord(
                "layout_confirmed",
                $"tokens={packet.tokens.Length};path_points={packet.path.point_count};capture={packet.capture.mode}",
                null);
        }

        private void OnPhaseChanged(P0Phase phase)
        {
            if (reader.LatestPacket == null)
            {
                return;
            }

            EnsureWriter(reader.LatestPacket.session_id);
            string note = phase == P0Phase.Observe
                ? "Run outcome snapshot"
                : phase == P0Phase.Complete
                    ? "Three-cycle session complete"
                    : string.Empty;
            WriteRecord("phase_changed", note, constraints.LatestResult);
        }

        private void EnsureWriter(string sessionId)
        {
            string normalized = string.IsNullOrWhiteSpace(sessionId) ? "unnamed-session" : sessionId;
            if (writer != null && activeSessionId == normalized)
            {
                return;
            }

            activeSessionId = normalized;
            writer = new ResearchLogWriter(ResolvePath(outputRoot), activeSessionId);
        }

        private void WriteRecord(string eventType, string note, P0ConstraintResult result)
        {
            LayoutPacket packet = reader.LatestPacket;
            P0CycleMemorySnapshot memory = cycle.Mechanics.LastMemory;
            ResearchLogRecord record = new ResearchLogRecord
            {
                timestamp_utc = DateTimeOffset.UtcNow.ToString("O"),
                session_id = activeSessionId,
                cycle_index = cycle.CycleIndex,
                cycle_scenario_id = cycle.CurrentProfile.ScenarioId,
                cycle_title = cycle.CurrentProfile.Title,
                event_type = eventType,
                phase = cycle.Phase.ToString(),
                predicted_top_feeder = P0CycleMechanics.FeederLabel(cycle.Mechanics.PredictedFeeder),
                predicted_conflict_area = P0CycleMechanics.ConflictAreaLabel(cycle.Mechanics.PredictedConflictArea),
                memory_source_cycle_index = memory?.SourceCycleIndex,
                remembered_human_trips = memory?.HumanTrips,
                remembered_avoidance_events = memory?.AvoidanceEvents,
                remembered_pigeon_food_token_id = RememberedTokenId(
                    memory?.PigeonPreferredFoodTokenId),
                remembered_squirrel_food_token_id = RememberedTokenId(
                    memory?.SquirrelPreferredFoodTokenId),
                remembered_fox_food_token_id = RememberedTokenId(
                    memory?.FoxPreferredFoodTokenId),
                carried_squirrel_familiarity = memory == null
                    ? (float?)null
                    : P0CycleMechanics.CarriedSquirrelFamiliarity(memory),
                memory_caution_multiplier = memory == null
                    ? (float?)null
                    : P0CycleMechanics.MemoryCautionMultiplier(memory),
                layout_timestamp_ms = packet?.timestamp_ms,
                human_connected = result?.human_connected,
                animal_reachable = result?.animal_reachable,
                food_hotspot_valid = result?.food_hotspot_valid,
                changes_used = result?.changes_used,
                changes_allowed = result?.changes_allowed,
                human_trips = humans?.CompletedTrips ?? 0,
                pigeon_count = animals?.PigeonCount ?? 0,
                squirrel_count = animals?.SquirrelCount ?? 0,
                fox_count = animals?.FoxCount ?? 0,
                pigeon_feed_events = animals?.PigeonFeedEvents ?? 0,
                squirrel_feed_events = animals?.SquirrelFeedEvents ?? 0,
                fox_feed_events = animals?.FoxFeedEvents ?? 0,
                animal_avoidance_events = animals?.AvoidanceEvents ?? 0,
                note = note,
            };
            try
            {
                writer.Append(record);
            }
            catch (IOException exception)
            {
                Debug.LogWarning($"Research log write failed: {exception.Message}", this);
            }
            catch (UnauthorizedAccessException exception)
            {
                Debug.LogWarning($"Research log write failed: {exception.Message}", this);
            }
        }

        private static string ResolvePath(string configuredPath)
        {
            return Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(Application.dataPath, configuredPath));
        }

        private static int? RememberedTokenId(int? tokenId)
        {
            return tokenId.HasValue && tokenId.Value >= 0 ? tokenId : null;
        }
    }
}

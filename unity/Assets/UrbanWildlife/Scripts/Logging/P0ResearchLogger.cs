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

            reader.LayoutAccepted += OnLayoutAccepted;
            constraints.ConstraintsEvaluated += OnConstraintsEvaluated;
            cycle.PhaseChanged += OnPhaseChanged;
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
            ResearchLogRecord record = new ResearchLogRecord
            {
                timestamp_utc = DateTimeOffset.UtcNow.ToString("O"),
                session_id = activeSessionId,
                cycle_index = cycle.CycleIndex,
                event_type = eventType,
                phase = cycle.Phase.ToString(),
                layout_timestamp_ms = packet?.timestamp_ms,
                human_connected = result?.human_connected,
                animal_reachable = result?.animal_reachable,
                food_hotspot_valid = result?.food_hotspot_valid,
                changes_used = result?.changes_used,
                changes_allowed = result?.changes_allowed,
                human_trips = humans?.CompletedTrips ?? 0,
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
    }
}

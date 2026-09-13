using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Ecology;
using UrbanWildlife.Environmental;
using UrbanWildlife.Mobility;
using UrbanWildlife.Strategy;

namespace UrbanWildlife.Reporting
{
    public sealed class CityObservationTracker
    {
        private readonly List<CityTracePoint> traces = new List<CityTracePoint>();
        private readonly List<CityFeedEntry> feed = new List<CityFeedEntry>();
        private readonly Dictionary<string, float[]> lastTracePositions =
            new Dictionary<string, float[]>();
        private readonly HashSet<string> observedEventKeys = new HashSet<string>();
        private readonly int maximumTracePoints;
        private readonly float minimumSampleDistanceNorm;
        private CityBalanceScore phaseStartBalance;
        private CityDevelopmentPhase reportPhase;
        private bool previousOverflow;
        private int feedSequence = 1;

        public CityObservationTracker(
            int maximumTracePoints = 6000,
            float minimumSampleDistanceNorm = 0.006f)
        {
            if (maximumTracePoints < 100 || minimumSampleDistanceNorm <= 0f)
            {
                throw new ArgumentException("City observation sampling configuration is invalid.");
            }
            this.maximumTracePoints = maximumTracePoints;
            this.minimumSampleDistanceNorm = minimumSampleDistanceNorm;
            Snapshot = new CityObservationSnapshot();
        }

        public CityObservationSnapshot Snapshot { get; private set; }

        public void ResetView(
            CityMobilityPlan mobility,
            CityEnvironmentSnapshot environment,
            CityWildlifeSnapshot wildlife,
            CityStrategySnapshot strategy)
        {
            if (mobility == null || environment == null || wildlife == null || strategy == null)
            {
                throw new ArgumentException("A complete city snapshot is required to reset observations.");
            }
            traces.Clear();
            feed.Clear();
            lastTracePositions.Clear();
            observedEventKeys.Clear();
            previousOverflow = environment.overflow_active;
            foreach (CityDestinationLoad load in mobility.destination_loads.Where(item =>
                         item.crowd_penalty > 0f))
            {
                observedEventKeys.Add(CrowdingKey(wildlife.cycle_index, load.building_id));
            }
            foreach (CityWildlifeOutcome outcome in wildlife.permanent_outcomes)
            {
                observedEventKeys.Add(WildlifeOutcomeKey(outcome));
            }
            foreach (CityStrategyProject project in strategy.projects.Where(item =>
                         item.state == CityStrategyProjectState.Complete))
            {
                observedEventKeys.Add(ProjectKey(project.id));
            }
            foreach (var dimension in BalanceDimensions(strategy).Where(item => item.Item2 < 35f))
            {
                observedEventKeys.Add(BalanceKey(
                    wildlife.cycle_index,
                    strategy.phase,
                    dimension.Item1));
            }
            Snapshot = new CityObservationSnapshot();
            BeginPhase(strategy);
        }

        public void BeginPhase(CityStrategySnapshot strategy)
        {
            if (strategy == null)
            {
                throw new ArgumentNullException(nameof(strategy));
            }
            reportPhase = strategy.phase;
            phaseStartBalance = CloneBalance(strategy.balance);
        }

        public void Capture(
            float deltaSeconds,
            CityMobilitySimulation mobility,
            CityEnvironmentSnapshot environment,
            CityWildlifeSnapshot wildlife,
            CityStrategySnapshot strategy)
        {
            if (deltaSeconds < 0f || mobility == null || environment == null ||
                wildlife == null || strategy == null)
            {
                throw new ArgumentException("A complete city observation frame is required.");
            }
            Snapshot.elapsed_seconds += deltaSeconds;
            if (phaseStartBalance == null || reportPhase != strategy.phase)
            {
                BeginPhase(strategy);
            }

            foreach (CityTripAgent agent in mobility.Agents.Where(item =>
                         item.State != CityTripMotionState.Pending &&
                         item.State != CityTripMotionState.Complete))
            {
                AddTrace(agent.Trip.agent_id, CityTraceLayer.Human, CityTraceMark.Footprint,
                    agent.PositionNorm, wildlife.cycle_index, strategy.phase);
            }
            foreach (CityVehicleAgent vehicle in mobility.ActiveVehicleAgents)
            {
                AddTrace(vehicle.id, CityTraceLayer.Human, CityTraceMark.VehicleTyre,
                    vehicle.position_norm, wildlife.cycle_index, strategy.phase);
            }
            foreach (CityWildlifeAgent agent in wildlife.agents.Where(item =>
                         item.life_state == CityWildlifeLifeState.Active))
            {
                AddTrace(agent.id, CityTraceLayer.Animal, TraceMark(agent.species),
                    agent.position_norm, wildlife.cycle_index, strategy.phase);
            }

            ObserveOverflow(environment, wildlife.cycle_index, strategy.phase);
            ObserveCrowding(mobility.Plan, wildlife.cycle_index, strategy.phase);
            ObserveWildlifeOutcomes(wildlife, strategy.phase);
            ObserveProjects(strategy, wildlife.cycle_index);
            ObserveBalance(strategy, wildlife.cycle_index);
            RefreshSnapshot();
        }

        public CityTracePoint[] Trace(CityTraceDisplayMode mode)
        {
            switch (mode)
            {
                case CityTraceDisplayMode.HumanTrace:
                    return traces.Where(point => point.layer == CityTraceLayer.Human).ToArray();
                case CityTraceDisplayMode.AnimalTrace:
                    return traces.Where(point => point.layer == CityTraceLayer.Animal).ToArray();
                default:
                    return traces.ToArray();
            }
        }

        public CityPhaseReport BuildPhaseReport(
            CityStrategySnapshot strategy,
            CityWildlifeSnapshot wildlife)
        {
            if (strategy == null || wildlife == null)
            {
                throw new ArgumentNullException(strategy == null ? nameof(strategy) : nameof(wildlife));
            }
            CityBalanceScore before = phaseStartBalance ?? CloneBalance(strategy.balance);
            CityBalanceScore after = CloneBalance(strategy.balance);
            CityFeedEntry[] phaseFeed = feed.Where(item => item.phase == reportPhase).ToArray();
            return new CityPhaseReport
            {
                phase = reportPhase,
                before = before,
                after = after,
                change = Difference(after, before),
                human_trace_points = traces.Count(point =>
                    point.phase == reportPhase && point.layer == CityTraceLayer.Human),
                animal_trace_points = traces.Count(point =>
                    point.phase == reportPhase && point.layer == CityTraceLayer.Animal),
                feeding_events = phaseFeed.Count(item => item.type == CityFeedEventType.Feeding),
                migration_events = phaseFeed.Count(item =>
                    item.type == CityFeedEventType.MigrationIn ||
                    item.type == CityFeedEventType.MigrationOut),
                roadkill_events = phaseFeed.Count(item => item.type == CityFeedEventType.Roadkill),
                overflow_events = phaseFeed.Count(item => item.type == CityFeedEventType.WasteOverflow),
                city_feed = phaseFeed,
            };
        }

        public CityResearchLogRecord BuildResearchRecord(
            string sessionId,
            CityEnvironmentSnapshot environment,
            CityWildlifeSnapshot wildlife,
            CityStrategySnapshot strategy,
            string eventType = "city_snapshot",
            string note = null)
        {
            int migrationEvents = wildlife.permanent_outcomes.Count(outcome =>
                outcome.type == CityWildlifeOutcomeType.MigratedIn ||
                outcome.type == CityWildlifeOutcomeType.MigratedOut);
            return new CityResearchLogRecord
            {
                timestamp_utc = DateTimeOffset.UtcNow.ToString("O"),
                session_id = sessionId,
                event_type = eventType,
                cycle_index = wildlife.cycle_index,
                phase = strategy.phase.ToString(),
                time_block = strategy.time_block.ToString(),
                development_points = strategy.development_points,
                city_balance_total = strategy.balance.total,
                development = strategy.balance.development,
                accessibility = strategy.balance.accessibility,
                waste_management = strategy.balance.waste_management,
                habitat_connectivity = strategy.balance.habitat_connectivity,
                wildlife_safety = strategy.balance.wildlife_safety,
                waste_demand = environment.waste_demand,
                waste_capacity = environment.waste_capacity,
                overflow_active = environment.overflow_active,
                active_wildlife = wildlife.active_count,
                feeding_events = wildlife.feeding_events,
                migration_events = migrationEvents,
                roadkill_events = wildlife.roadkill_events,
                human_trace_points = Trace(CityTraceDisplayMode.HumanTrace).Length,
                animal_trace_points = Trace(CityTraceDisplayMode.AnimalTrace).Length,
                note = note,
            };
        }

        private void AddTrace(
            string agentId,
            CityTraceLayer layer,
            CityTraceMark mark,
            float[] position,
            int cycleIndex,
            CityDevelopmentPhase phase)
        {
            if (traces.Count >= maximumTracePoints || position?.Length != 2)
            {
                return;
            }
            if (lastTracePositions.TryGetValue(agentId, out float[] previous) &&
                Vector2.Distance(ToVector(previous), ToVector(position)) < minimumSampleDistanceNorm)
            {
                return;
            }
            traces.Add(new CityTracePoint
            {
                agent_id = agentId,
                layer = layer,
                mark = mark,
                position_norm = ClonePoint(position),
                elapsed_seconds = Snapshot.elapsed_seconds,
                cycle_index = cycleIndex,
                phase = phase,
            });
            lastTracePositions[agentId] = ClonePoint(position);
        }

        private void ObserveOverflow(
            CityEnvironmentSnapshot environment,
            int cycleIndex,
            CityDevelopmentPhase phase)
        {
            if (environment.overflow_active && !previousOverflow)
            {
                AddFeed(CityFeedEventType.WasteOverflow, "waste-system", null,
                    "Waste capacity exceeded long enough to create litter hotspots.", cycleIndex, phase);
            }
            previousOverflow = environment.overflow_active;
        }

        private void ObserveCrowding(
            CityMobilityPlan plan,
            int cycleIndex,
            CityDevelopmentPhase phase)
        {
            foreach (CityDestinationLoad load in plan.destination_loads.Where(item => item.crowd_penalty > 0f))
            {
                AddFeedOnce(CrowdingKey(cycleIndex, load.building_id),
                    CityFeedEventType.Crowding, load.building_id, null,
                    $"{load.building_id} exceeded comfortable destination capacity.", cycleIndex, phase);
            }
        }

        private void ObserveWildlifeOutcomes(
            CityWildlifeSnapshot wildlife,
            CityDevelopmentPhase phase)
        {
            foreach (CityWildlifeOutcome outcome in wildlife.permanent_outcomes)
            {
                string key = WildlifeOutcomeKey(outcome);
                CityFeedEventType type;
                switch (outcome.type)
                {
                    case CityWildlifeOutcomeType.Fed: type = CityFeedEventType.Feeding; break;
                    case CityWildlifeOutcomeType.MigratedIn: type = CityFeedEventType.MigrationIn; break;
                    case CityWildlifeOutcomeType.MigratedOut: type = CityFeedEventType.MigrationOut; break;
                    case CityWildlifeOutcomeType.Roadkill: type = CityFeedEventType.Roadkill; break;
                    default: continue;
                }
                AddFeedOnce(key, type, outcome.agent_id, outcome.cause_id,
                    MessageFor(outcome), outcome.cycle_index, phase);
            }
        }

        private void ObserveProjects(CityStrategySnapshot strategy, int cycleIndex)
        {
            foreach (CityStrategyProject project in strategy.projects.Where(item =>
                         item.state == CityStrategyProjectState.Complete))
            {
                AddFeedOnce(ProjectKey(project.id), CityFeedEventType.ProjectCompleted,
                    project.target_id, project.id,
                    $"{project.action_type} completed after {project.total_time_blocks} time block(s).",
                    cycleIndex, strategy.phase);
            }
        }

        private void ObserveBalance(CityStrategySnapshot strategy, int cycleIndex)
        {
            foreach (var dimension in BalanceDimensions(strategy).Where(item => item.Item2 < 35f))
            {
                AddFeedOnce(BalanceKey(cycleIndex, strategy.phase, dimension.Item1),
                    CityFeedEventType.BalanceWarning, dimension.Item1, null,
                    $"{dimension.Item1} fell below the 35-point warning line.",
                    cycleIndex, strategy.phase);
            }
        }

        private void AddFeedOnce(
            string key,
            CityFeedEventType type,
            string subjectId,
            string causeId,
            string message,
            int cycleIndex,
            CityDevelopmentPhase phase)
        {
            if (observedEventKeys.Add(key))
            {
                AddFeed(type, subjectId, causeId, message, cycleIndex, phase);
            }
        }

        private void AddFeed(
            CityFeedEventType type,
            string subjectId,
            string causeId,
            string message,
            int cycleIndex,
            CityDevelopmentPhase phase)
        {
            feed.Add(new CityFeedEntry
            {
                id = $"city-feed-{feedSequence++:0000}",
                type = type,
                subject_id = subjectId,
                cause_id = causeId,
                message = message,
                elapsed_seconds = Snapshot.elapsed_seconds,
                cycle_index = cycleIndex,
                phase = phase,
            });
        }

        private void RefreshSnapshot()
        {
            Snapshot.trace_points = traces.ToArray();
            Snapshot.city_feed = feed.ToArray();
        }

        private static CityTraceMark TraceMark(CityWildlifeSpecies species)
        {
            switch (species)
            {
                case CityWildlifeSpecies.Pigeon: return CityTraceMark.BirdTrack;
                case CityWildlifeSpecies.GreySquirrel: return CityTraceMark.SmallPaw;
                case CityWildlifeSpecies.Fox: return CityTraceMark.FoxPaw;
                default: return CityTraceMark.HedgehogTrack;
            }
        }

        private static (string, float)[] BalanceDimensions(CityStrategySnapshot strategy)
        {
            return new[]
            {
                ("Development", strategy.balance.development),
                ("Accessibility", strategy.balance.accessibility),
                ("Waste management", strategy.balance.waste_management),
                ("Habitat connectivity", strategy.balance.habitat_connectivity),
                ("Wildlife safety", strategy.balance.wildlife_safety),
            };
        }

        private static string CrowdingKey(int cycleIndex, string buildingId)
        {
            return $"crowding:{cycleIndex}:{buildingId}";
        }

        private static string WildlifeOutcomeKey(CityWildlifeOutcome outcome)
        {
            return $"wildlife:{outcome.cycle_index}:{outcome.type}:{outcome.agent_id}:{outcome.cause_id}";
        }

        private static string ProjectKey(string projectId)
        {
            return $"project:{projectId}";
        }

        private static string BalanceKey(
            int cycleIndex,
            CityDevelopmentPhase phase,
            string dimension)
        {
            return $"balance:{cycleIndex}:{phase}:{dimension}";
        }

        private static string MessageFor(CityWildlifeOutcome outcome)
        {
            switch (outcome.type)
            {
                case CityWildlifeOutcomeType.Fed:
                    return $"{outcome.agent_id} fed at {outcome.location_id}.";
                case CityWildlifeOutcomeType.MigratedIn:
                    return $"{outcome.agent_id} entered the district near {outcome.location_id}.";
                case CityWildlifeOutcomeType.MigratedOut:
                    return $"{outcome.agent_id} left after sustained low habitat utility.";
                default:
                    return $"{outcome.agent_id} was struck by {outcome.cause_id}.";
            }
        }

        private static CityBalanceScore Difference(CityBalanceScore after, CityBalanceScore before)
        {
            return new CityBalanceScore
            {
                development = after.development - before.development,
                accessibility = after.accessibility - before.accessibility,
                waste_management = after.waste_management - before.waste_management,
                habitat_connectivity = after.habitat_connectivity - before.habitat_connectivity,
                wildlife_safety = after.wildlife_safety - before.wildlife_safety,
                total = after.total - before.total,
            };
        }

        private static CityBalanceScore CloneBalance(CityBalanceScore source)
        {
            return source == null
                ? new CityBalanceScore()
                : new CityBalanceScore
                {
                    development = source.development,
                    accessibility = source.accessibility,
                    waste_management = source.waste_management,
                    habitat_connectivity = source.habitat_connectivity,
                    wildlife_safety = source.wildlife_safety,
                    total = source.total,
                };
        }

        private static Vector2 ToVector(float[] point) => new Vector2(point[0], point[1]);
        private static float[] ClonePoint(float[] point) => (float[])point.Clone();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.City;
using UrbanWildlife.Ecology;
using UrbanWildlife.Environmental;

namespace UrbanWildlife.Strategy
{
    public sealed class CityStrategySimulation
    {
        private readonly CityState city;
        private readonly CityStrategyConfiguration configuration;
        private readonly List<CityStrategyProject> projects = new List<CityStrategyProject>();
        private int nextProjectSequence = 1;
        private int blocksInPhase;

        public CityStrategySimulation(
            CityState city,
            CityStrategyConfiguration configuration = null)
        {
            CityStateValidationResult validation = CityStateValidator.Validate(city);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.Summary, nameof(city));
            }
            this.city = city;
            this.configuration = configuration ?? new CityStrategyConfiguration();
            if (this.configuration.starting_development_points < 0 ||
                this.configuration.phase_development_point_grant < 0 ||
                this.configuration.time_blocks_per_phase <= 0 ||
                this.configuration.time_block_seconds <= 0f)
            {
                throw new ArgumentException("City strategy configuration is invalid.", nameof(configuration));
            }
            DevelopmentPoints = this.configuration.starting_development_points;
            Phase = CityDevelopmentPhase.PopulationGrowth;
            TimeBlock = CityTimeBlock.Quiet;
            RefreshSnapshot(null, null);
        }

        public CityDevelopmentPhase Phase { get; private set; }
        public CityTimeBlock TimeBlock { get; private set; }
        public int DevelopmentPoints { get; private set; }
        public float BlockElapsedSeconds { get; private set; }
        public int CompletedPhaseCount { get; private set; }
        public CityStrategySnapshot Snapshot { get; private set; }
        public static float[] AllowedTimeScales => new[] { 0f, 1f, 2f };

        public bool TryQueueProject(
            CityStrategyActionType actionType,
            string targetId,
            out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(targetId))
            {
                error = "A project target is required.";
                return false;
            }
            if (projects.Any(project => project.state == CityStrategyProjectState.Active &&
                                        project.target_id == targetId))
            {
                error = $"{targetId} already has an active project.";
                return false;
            }
            if (!TargetMatchesAction(actionType, targetId, out error))
            {
                return false;
            }
            (int cost, int duration, float disturbance, float obstruction) = CostFor(actionType);
            if (DevelopmentPoints < cost)
            {
                error = $"{actionType} requires {cost} DP but only {DevelopmentPoints} remain.";
                return false;
            }
            DevelopmentPoints -= cost;
            MarkProjectStarted(actionType, targetId);
            projects.Add(new CityStrategyProject
            {
                id = $"project-{nextProjectSequence++:000}",
                action_type = actionType,
                state = CityStrategyProjectState.Active,
                target_id = targetId,
                dp_cost = cost,
                total_time_blocks = duration,
                remaining_time_blocks = duration,
                construction_disturbance = disturbance,
                access_obstruction = obstruction,
            });
            RefreshSnapshot(null, null);
            return true;
        }

        public void Tick(
            float deltaSeconds,
            CityEnvironmentSnapshot environment,
            CityWildlifeSnapshot wildlife)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }
            BlockElapsedSeconds += deltaSeconds;
            while (BlockElapsedSeconds >= configuration.time_block_seconds)
            {
                BlockElapsedSeconds -= configuration.time_block_seconds;
                AdvanceTimeBlock();
            }
            RefreshSnapshot(environment, wildlife);
        }

        public void AdvanceTimeBlock()
        {
            foreach (CityStrategyProject project in projects.Where(item =>
                         item.state == CityStrategyProjectState.Active).ToArray())
            {
                project.remaining_time_blocks = Math.Max(0, project.remaining_time_blocks - 1);
                if (project.remaining_time_blocks == 0)
                {
                    CompleteProject(project);
                }
            }

            TimeBlock = (CityTimeBlock)(((int)TimeBlock + 1) % 4);
            blocksInPhase += 1;
            if (blocksInPhase >= configuration.time_blocks_per_phase)
            {
                blocksInPhase = 0;
                if (Phase != CityDevelopmentPhase.Redevelopment)
                {
                    Phase = (CityDevelopmentPhase)((int)Phase + 1);
                    CompletedPhaseCount += 1;
                    DevelopmentPoints += configuration.phase_development_point_grant;
                }
            }
            city.revision += 1;
            RefreshSnapshot(null, null);
        }

        public void UpdateFeedback(
            CityEnvironmentSnapshot environment,
            CityWildlifeSnapshot wildlife)
        {
            RefreshSnapshot(environment, wildlife);
        }

        private bool TargetMatchesAction(
            CityStrategyActionType actionType,
            string targetId,
            out string error)
        {
            error = null;
            CityConstructionState? state = TargetState(actionType, targetId);
            if (!state.HasValue)
            {
                error = $"No compatible target '{targetId}' exists for {actionType}.";
                return false;
            }
            if (actionType == CityStrategyActionType.Demolish)
            {
                if (state != CityConstructionState.DemolitionProposed)
                {
                    error = $"{targetId} must be approved for demolition first.";
                    return false;
                }
                if (IsLastNaturalFoodPatch(targetId))
                {
                    error = "The last Natural Food patch cannot be demolished.";
                    return false;
                }
                return true;
            }
            if (state != CityConstructionState.Proposed)
            {
                error = $"{targetId} must be Proposed before construction starts.";
                return false;
            }
            return true;
        }

        private CityConstructionState? TargetState(CityStrategyActionType actionType, string targetId)
        {
            switch (actionType)
            {
                case CityStrategyActionType.ConstructBuilding:
                    return city.buildings.FirstOrDefault(item => item.id == targetId)?.construction_state;
                case CityStrategyActionType.ConstructGreenPatch:
                    return city.green_patches.FirstOrDefault(item => item.id == targetId)?.construction_state;
                case CityStrategyActionType.ConstructRoad:
                    return city.vehicle_roads.FirstOrDefault(item => item.id == targetId)?.construction_state;
                case CityStrategyActionType.ConstructFootpath:
                    return city.pedestrian_links.FirstOrDefault(item => item.id == targetId)?.construction_state;
                case CityStrategyActionType.InstallBench:
                    return city.amenities.FirstOrDefault(item =>
                        item.id == targetId && item.type == CityAmenityType.Bench)?.construction_state;
                case CityStrategyActionType.InstallBin:
                    return city.amenities.FirstOrDefault(item =>
                        item.id == targetId && item.type == CityAmenityType.Bin)?.construction_state;
                default:
                    return FindAnyTargetState(targetId);
            }
        }

        private CityConstructionState? FindAnyTargetState(string targetId)
        {
            CityBuilding building = city.buildings.FirstOrDefault(item => item.id == targetId);
            if (building != null) return building.construction_state;
            CityGreenPatch patch = city.green_patches.FirstOrDefault(item => item.id == targetId);
            if (patch != null) return patch.construction_state;
            CityVehicleRoad road = city.vehicle_roads.FirstOrDefault(item => item.id == targetId);
            if (road != null) return road.construction_state;
            CityPedestrianLink link = city.pedestrian_links.FirstOrDefault(item => item.id == targetId);
            if (link != null) return link.construction_state;
            CityAmenity amenity = city.amenities.FirstOrDefault(item => item.id == targetId);
            return amenity?.construction_state;
        }

        private void MarkProjectStarted(CityStrategyActionType actionType, string targetId)
        {
            CityConstructionState next = actionType == CityStrategyActionType.Demolish
                ? CityConstructionState.Demolishing
                : CityConstructionState.UnderConstruction;
            SetTargetState(targetId, next);
        }

        private void CompleteProject(CityStrategyProject project)
        {
            if (project.action_type == CityStrategyActionType.Demolish)
            {
                RemoveTarget(project.target_id);
            }
            else
            {
                SetTargetState(project.target_id, CityConstructionState.Existing);
            }
            project.state = CityStrategyProjectState.Complete;
        }

        private void SetTargetState(string targetId, CityConstructionState state)
        {
            CityBuilding building = city.buildings.FirstOrDefault(item => item.id == targetId);
            if (building != null) { building.construction_state = state; return; }
            CityGreenPatch patch = city.green_patches.FirstOrDefault(item => item.id == targetId);
            if (patch != null) { patch.construction_state = state; return; }
            CityVehicleRoad road = city.vehicle_roads.FirstOrDefault(item => item.id == targetId);
            if (road != null) { road.construction_state = state; return; }
            CityPedestrianLink link = city.pedestrian_links.FirstOrDefault(item => item.id == targetId);
            if (link != null) { link.construction_state = state; return; }
            CityAmenity amenity = city.amenities.FirstOrDefault(item => item.id == targetId);
            if (amenity != null) amenity.construction_state = state;
        }

        private void RemoveTarget(string targetId)
        {
            city.buildings = city.buildings.Where(item => item.id != targetId).ToArray();
            city.green_patches = city.green_patches.Where(item => item.id != targetId).ToArray();
            city.vehicle_roads = city.vehicle_roads.Where(item => item.id != targetId).ToArray();
            city.pedestrian_links = city.pedestrian_links.Where(item => item.id != targetId).ToArray();
            city.amenities = city.amenities.Where(item => item.id != targetId).ToArray();
            city.waste.nodes = city.waste.nodes.Where(item =>
                item.source_building_id != targetId && item.id != $"waste-{targetId}").ToArray();

            foreach (CityBuilding building in city.buildings)
            {
                building.vehicle_road_ids = building.vehicle_road_ids.Where(id => id != targetId).ToArray();
                building.pedestrian_link_ids = building.pedestrian_link_ids.Where(id => id != targetId).ToArray();
            }
            foreach (CityGreenPatch patch in city.green_patches)
            {
                patch.connected_patch_ids = patch.connected_patch_ids.Where(id => id != targetId).ToArray();
            }
            foreach (CityVehicleRoad road in city.vehicle_roads)
            {
                road.connected_building_ids = road.connected_building_ids.Where(id => id != targetId).ToArray();
                road.connected_road_ids = road.connected_road_ids.Where(id => id != targetId).ToArray();
            }
            foreach (CityPedestrianLink link in city.pedestrian_links)
            {
                link.connected_building_ids = link.connected_building_ids.Where(id => id != targetId).ToArray();
                link.connected_link_ids = link.connected_link_ids.Where(id => id != targetId).ToArray();
            }
        }

        private bool IsLastNaturalFoodPatch(string targetId)
        {
            CityGreenPatch target = city.green_patches.FirstOrDefault(item => item.id == targetId);
            return target != null && target.natural_food_value > 0f &&
                   city.green_patches.Count(item => item.id != targetId &&
                                                    item.natural_food_value > 0f) == 0;
        }

        private void RefreshSnapshot(
            CityEnvironmentSnapshot environment,
            CityWildlifeSnapshot wildlife)
        {
            Snapshot = new CityStrategySnapshot
            {
                phase = Phase,
                time_block = TimeBlock,
                development_points = DevelopmentPoints,
                block_elapsed_seconds = BlockElapsedSeconds,
                completed_phase_count = CompletedPhaseCount,
                active_construction_disturbance = projects
                    .Where(project => project.state == CityStrategyProjectState.Active)
                    .Sum(project => project.construction_disturbance),
                active_access_obstruction = projects
                    .Where(project => project.state == CityStrategyProjectState.Active)
                    .Sum(project => project.access_obstruction),
                projects = projects.ToArray(),
                balance = CalculateBalance(environment, wildlife),
            };
        }

        private CityBalanceScore CalculateBalance(
            CityEnvironmentSnapshot environment,
            CityWildlifeSnapshot wildlife)
        {
            CityBuilding[] existingBuildings = city.buildings.Where(item =>
                item.construction_state == CityConstructionState.Existing).ToArray();
            float development = Mathf.Clamp01(existingBuildings.Length / 10f);
            float accessible = existingBuildings.Length == 0
                ? 0f
                : existingBuildings.Count(building =>
                    (!building.requires_pedestrian_access || building.pedestrian_link_ids.Length > 0) &&
                    (!building.requires_vehicle_access || building.vehicle_road_ids.Length > 0)) /
                  (float)existingBuildings.Length;
            float waste = environment == null
                ? 0.5f
                : Mathf.Clamp01(1f - Mathf.Max(0f, environment.waste_utilisation - 0.65f) / 0.75f -
                                    (environment.overflow_active ? 0.25f : 0f));
            CityGreenPatch[] existingPatches = city.green_patches.Where(item =>
                item.construction_state == CityConstructionState.Existing).ToArray();
            float connectedShare = existingPatches.Length == 0
                ? 0f
                : existingPatches.Count(patch => patch.connected_patch_ids.Length > 0) /
                  (float)existingPatches.Length;
            float habitat = existingPatches.Length == 0
                ? 0f
                : Mathf.Clamp01(0.55f * connectedShare +
                                0.45f * existingPatches.Average(patch =>
                                    (patch.shelter_value + patch.natural_food_value) * 0.5f));
            float roadkillPenalty = wildlife == null || wildlife.agents.Length == 0
                ? 0f
                : wildlife.roadkill_events / (float)wildlife.agents.Length;
            float safety = environment == null
                ? 0.5f
                : Mathf.Clamp01(1f - 0.55f * environment.average_disturbance -
                                    0.85f * roadkillPenalty);
            return new CityBalanceScore
            {
                development = development * 100f,
                accessibility = accessible * 100f,
                waste_management = waste * 100f,
                habitat_connectivity = habitat * 100f,
                wildlife_safety = safety * 100f,
                total = (development + accessible + waste + habitat + safety) * 20f,
            };
        }

        private static (int cost, int duration, float disturbance, float obstruction) CostFor(
            CityStrategyActionType actionType)
        {
            switch (actionType)
            {
                case CityStrategyActionType.ConstructBuilding: return (5, 2, 0.65f, 0.55f);
                case CityStrategyActionType.ConstructGreenPatch: return (4, 2, 0.28f, 0.18f);
                case CityStrategyActionType.ConstructRoad: return (3, 2, 0.72f, 0.68f);
                case CityStrategyActionType.ConstructFootpath: return (2, 1, 0.32f, 0.34f);
                case CityStrategyActionType.InstallBench: return (1, 1, 0.12f, 0.08f);
                case CityStrategyActionType.InstallBin: return (1, 1, 0.12f, 0.08f);
                default: return (2, 1, 0.58f, 0.48f);
            }
        }
    }
}

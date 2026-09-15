using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.City;
using UrbanWildlife.Environmental;
using UrbanWildlife.Mobility;

namespace UrbanWildlife.Ecology
{
    public sealed class CityWildlifeSimulation
    {
        private const float Epsilon = 0.0001f;
        private readonly CityState city;
        private readonly CityWildlifeConfiguration configuration;
        private readonly List<CityWildlifeAgent> agents = new List<CityWildlifeAgent>();
        private readonly List<CityWildlifeOutcome> outcomes = new List<CityWildlifeOutcome>();
        private readonly Dictionary<string, float> feedingCooldowns = new Dictionary<string, float>();
        private readonly Dictionary<CityWildlifeSpecies, CitySpeciesProfile> profiles;

        public CityWildlifeSimulation(
            CityState city,
            CityWildlifeConfiguration configuration = null,
            int pigeonCount = 8,
            int squirrelCount = 4,
            int foxCount = 2,
            int hedgehogCount = 1)
        {
            CityStateValidationResult validation = CityStateValidator.Validate(city);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.Summary, nameof(city));
            }
            this.city = city;
            this.configuration = configuration ?? new CityWildlifeConfiguration();
            ValidateConfiguration(this.configuration);
            profiles = CreateProfiles().ToDictionary(profile => profile.species);
            SpawnInitial(CityWildlifeSpecies.Pigeon, pigeonCount);
            SpawnInitial(CityWildlifeSpecies.GreySquirrel, squirrelCount);
            SpawnInitial(CityWildlifeSpecies.Fox, foxCount);
            SpawnInitial(CityWildlifeSpecies.Hedgehog, hedgehogCount);
            RefreshSnapshot();
        }

        public int CycleIndex { get; private set; }
        public CityWildlifeSnapshot Snapshot { get; private set; }
        public CitySpeciesProfile Profile(CityWildlifeSpecies species) => profiles[species];

        public void Tick(
            float deltaSeconds,
            CityEnvironmentSnapshot environment,
            IEnumerable<CityVehicleAgent> vehicles)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }
            if (environment == null)
            {
                throw new ArgumentNullException(nameof(environment));
            }

            CityVehicleAgent[] activeVehicles = (vehicles ?? Array.Empty<CityVehicleAgent>())
                .Where(vehicle => vehicle?.position_norm?.Length == 2)
                .ToArray();
            foreach (CityWildlifeAgent agent in agents.Where(item =>
                         item.life_state == CityWildlifeLifeState.Active).ToArray())
            {
                if (DetectRoadkill(agent, activeVehicles))
                {
                    continue;
                }
                SelectTarget(agent, environment);
                if (agent.target_utility < configuration.migration_utility_threshold)
                {
                    agent.low_utility_elapsed_seconds += deltaSeconds;
                    if (agent.low_utility_elapsed_seconds >= configuration.migration_grace_seconds)
                    {
                        agent.life_state = CityWildlifeLifeState.Migrated;
                        RecordOutcome(CityWildlifeOutcomeType.MigratedOut, agent, agent.target_patch_id, null);
                        continue;
                    }
                }
                else
                {
                    agent.low_utility_elapsed_seconds = 0f;
                }
                MoveTowardTarget(agent, deltaSeconds, environment);
                UpdateFeeding(agent, deltaSeconds, environment);
            }
            RefreshSnapshot();
        }

        public bool TryAddImmigrant(
            CityWildlifeSpecies species,
            CityEnvironmentSnapshot environment,
            float minimumUtility = 0.45f)
        {
            CityPatchPressure best = BestPatch(species, null, environment, out float utility);
            if (best == null || utility < minimumUtility)
            {
                return false;
            }
            int sequence = agents.Count(agent => agent.species == species) + 1;
            CityWildlifeAgent immigrant = CreateAgent(
                species,
                sequence,
                OffsetSpawnPosition(species, sequence - 1, best.position_norm));
            immigrant.target_patch_id = best.patch_id;
            immigrant.target_utility = utility;
            agents.Add(immigrant);
            RecordOutcome(CityWildlifeOutcomeType.MigratedIn, immigrant, best.patch_id, null);
            RefreshSnapshot();
            return true;
        }

        public void AdvanceCycle()
        {
            CycleIndex += 1;
            foreach (CityWildlifeAgent agent in agents)
            {
                agent.memories = (agent.memories ?? Array.Empty<CityWildlifeMemory>())
                    .Select(memory =>
                    {
                        memory.age_cycles += 1;
                        memory.strength = Mathf.Max(
                            0f,
                            memory.strength - (memory.negative
                                ? configuration.negative_memory_decay_per_cycle
                                : configuration.positive_memory_decay_per_cycle));
                        return memory;
                    })
                    .Where(memory => memory.strength > Epsilon)
                    .ToArray();
            }
            RefreshSnapshot();
        }

        public void Remember(
            CityWildlifeAgent agent,
            CityWildlifeMemoryType type,
            string locationId,
            float[] positionNorm,
            float strength,
            bool negative)
        {
            if (agent == null || !agents.Contains(agent))
            {
                throw new ArgumentException("Memory owner is not part of this simulation.", nameof(agent));
            }
            CityWildlifeMemory[] existing = agent.memories ?? Array.Empty<CityWildlifeMemory>();
            List<CityWildlifeMemory> memories = existing
                .Where(memory => memory != null &&
                                 !(memory.type == type && memory.location_id == locationId))
                .ToList();
            memories.Add(new CityWildlifeMemory
            {
                type = type,
                location_id = locationId,
                position_norm = ClonePoint(positionNorm),
                strength = Mathf.Clamp01(strength),
                negative = negative,
                age_cycles = 0,
            });
            agent.memories = memories
                .OrderByDescending(memory => memory.strength + (memory.negative ? 0.08f : 0f))
                .Take(configuration.memory_limit)
                .ToArray();
        }

        private void SpawnInitial(CityWildlifeSpecies species, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            float[][] spawnSites = PreferredInitialSpawnSites(species);
            if (spawnSites.Length == 0 && count > 0)
            {
                throw new InvalidOperationException("Wildlife requires at least one usable spawn site.");
            }
            for (int index = 0; index < count; index += 1)
            {
                int spreadIndex = (int)Math.Floor(
                    (index + 0.5f) * spawnSites.Length / Math.Max(1f, count));
                int speciesPhase = (int)species * Math.Max(1, spawnSites.Length / 7);
                float[] centre = spawnSites[(spreadIndex + speciesPhase) % spawnSites.Length];
                agents.Add(CreateAgent(
                    species,
                    index + 1,
                    OffsetSpawnPosition(species, index, centre)));
            }
        }

        private float[][] PreferredInitialSpawnSites(CityWildlifeSpecies species)
        {
            CityGreenPatch[] operational = OperationalPatches().ToArray();
            if (species == CityWildlifeSpecies.Pigeon)
            {
                List<float[]> sites = new List<float[]>();
                HashSet<string> occupiedCellIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (CityGridCell cell in (city.planning_grid?.cells ?? Array.Empty<CityGridCell>())
                             .Where(cell => cell != null &&
                                            PigeonCanLandOn(cell.current_cover) &&
                                            CityGridResolver.IsUnitPoint(cell.center_norm))
                             .OrderBy(cell => cell.row)
                             .ThenBy(cell => cell.col)
                             .ThenBy(cell => cell.id, StringComparer.Ordinal))
                {
                    sites.Add(ClonePoint(cell.center_norm));
                    occupiedCellIds.Add(cell.id);
                }
                foreach (CityGreenPatch patch in operational
                             .OrderByDescending(patch => patch.type == CityGreenPatchType.OpenGrass)
                             .ThenBy(patch => patch.id, StringComparer.Ordinal))
                {
                    if (!string.IsNullOrWhiteSpace(patch.planning_cell_id) &&
                        occupiedCellIds.Contains(patch.planning_cell_id))
                    {
                        continue;
                    }
                    float[] centre = PolygonCentroid(patch.polygon_norm);
                    if (!sites.Any(site => SamePoint(site, centre)))
                    {
                        sites.Add(centre);
                    }
                }
                return sites.ToArray();
            }

            bool woodlandSpecies = species == CityWildlifeSpecies.GreySquirrel ||
                                   species == CityWildlifeSpecies.Fox ||
                                   species == CityWildlifeSpecies.Hedgehog;
            if (woodlandSpecies)
            {
                CityGreenPatch[] woodland = operational
                    .Where(patch => patch.type == CityGreenPatchType.Woodland)
                    .OrderBy(patch => PolygonCentroid(patch.polygon_norm)[1])
                    .ThenBy(patch => PolygonCentroid(patch.polygon_norm)[0])
                    .ThenBy(patch => patch.id, StringComparer.Ordinal)
                    .ToArray();
                return (woodland.Length > 0 ? woodland : operational)
                    .Select(patch => PolygonCentroid(patch.polygon_norm))
                    .ToArray();
            }
            return operational
                .OrderByDescending(patch => patch.type == CityGreenPatchType.OpenGrass)
                .ThenBy(patch => PolygonCentroid(patch.polygon_norm)[1])
                .ThenBy(patch => PolygonCentroid(patch.polygon_norm)[0])
                .ThenBy(patch => patch.id, StringComparer.Ordinal)
                .Select(patch => PolygonCentroid(patch.polygon_norm))
                .ToArray();
        }

        private static bool PigeonCanLandOn(CityLandCover cover)
        {
            return cover == CityLandCover.PublicGreen ||
                   cover == CityLandCover.CivicPlaza ||
                   cover == CityLandCover.OpenLand;
        }

        private static float[] SpawnOffset(CityWildlifeSpecies species, int index)
        {
            const float speciesOffset = 0.005f;
            float individualOffset = ((index % 3) - 1) * 0.001f;
            switch (species)
            {
                case CityWildlifeSpecies.Pigeon:
                    return new[] { -speciesOffset + individualOffset, -speciesOffset - individualOffset };
                case CityWildlifeSpecies.GreySquirrel:
                    return new[] { speciesOffset + individualOffset, -speciesOffset - individualOffset };
                case CityWildlifeSpecies.Fox:
                    return new[] { speciesOffset + individualOffset, speciesOffset - individualOffset };
                default:
                    return new[] { -speciesOffset + individualOffset, speciesOffset - individualOffset };
            }
        }

        private static float[] OffsetSpawnPosition(
            CityWildlifeSpecies species,
            int index,
            float[] centre)
        {
            float[] offset = SpawnOffset(species, index);
            return new[]
            {
                Mathf.Clamp01(centre[0] + offset[0]),
                Mathf.Clamp01(centre[1] + offset[1]),
            };
        }

        private CityWildlifeAgent CreateAgent(
            CityWildlifeSpecies species,
            int sequence,
            float[] position)
        {
            string prefix = species == CityWildlifeSpecies.GreySquirrel
                ? "squirrel"
                : species.ToString().ToLowerInvariant();
            return new CityWildlifeAgent
            {
                id = $"{prefix}-{sequence:00}",
                species = species,
                life_state = CityWildlifeLifeState.Active,
                position_norm = ClonePoint(position),
            };
        }

        private void SelectTarget(CityWildlifeAgent agent, CityEnvironmentSnapshot environment)
        {
            CityPatchPressure best = BestPatch(agent.species, agent, environment, out float utility);
            agent.target_patch_id = best?.patch_id;
            agent.target_utility = best == null ? float.NegativeInfinity : utility;
        }

        private CityPatchPressure BestPatch(
            CityWildlifeSpecies species,
            CityWildlifeAgent agent,
            CityEnvironmentSnapshot environment,
            out float bestUtility)
        {
            bestUtility = float.NegativeInfinity;
            CityPatchPressure best = null;
            foreach (CityPatchPressure patch in environment.patch_pressures ?? Array.Empty<CityPatchPressure>())
            {
                if (patch == null || patch.position_norm?.Length != 2)
                {
                    continue;
                }
                float utility = CalculateUtility(species, agent, patch);
                if (utility > bestUtility + Epsilon ||
                    (Mathf.Abs(utility - bestUtility) <= Epsilon &&
                     string.CompareOrdinal(patch.patch_id, best?.patch_id) < 0))
                {
                    best = patch;
                    bestUtility = utility;
                }
            }
            return best;
        }

        private float CalculateUtility(
            CityWildlifeSpecies species,
            CityWildlifeAgent agent,
            CityPatchPressure patch)
        {
            CitySpeciesProfile profile = profiles[species];
            float travel = agent == null ? 0f : DistanceUnits(agent.position_norm, patch.position_norm) /
                                                   Mathf.Max(city.bounds.width_units, city.bounds.height_units);
            float positive = MemoryAt(agent, patch.patch_id, false);
            float negative = MemoryAt(agent, patch.patch_id, true);
            return patch.natural_food * profile.natural_food_weight +
                   patch.anthropogenic_food * profile.anthropogenic_food_weight +
                   patch.shelter * profile.shelter_weight +
                   positive * profile.positive_memory_weight -
                   patch.human_disturbance * profile.disturbance_weight -
                   patch.traffic_exposure * profile.traffic_weight -
                   negative * profile.negative_memory_weight -
                   travel * profile.travel_cost_weight;
        }

        private static float MemoryAt(CityWildlifeAgent agent, string patchId, bool negative)
        {
            if (agent == null)
            {
                return 0f;
            }
            return (agent.memories ?? Array.Empty<CityWildlifeMemory>())
                .Where(memory => memory != null && memory.location_id == patchId &&
                                 memory.negative == negative)
                .Select(memory => memory.strength)
                .DefaultIfEmpty(0f)
                .Max();
        }

        private void MoveTowardTarget(
            CityWildlifeAgent agent,
            float deltaSeconds,
            CityEnvironmentSnapshot environment)
        {
            CityPatchPressure target = (environment.patch_pressures ?? Array.Empty<CityPatchPressure>())
                .FirstOrDefault(patch => patch.patch_id == agent.target_patch_id);
            if (target == null)
            {
                return;
            }
            float distance = DistanceUnits(agent.position_norm, target.position_norm);
            if (distance <= configuration.target_reached_units || deltaSeconds <= 0f)
            {
                return;
            }
            float amount = Mathf.Min(1f, profiles[agent.species].speed_units_per_second * deltaSeconds / distance);
            float[] candidate = new[]
            {
                Mathf.Lerp(agent.position_norm[0], target.position_norm[0], amount),
                Mathf.Lerp(agent.position_norm[1], target.position_norm[1], amount),
            };
            if (!IsPathBlocked(agent.species, agent.position_norm, candidate))
            {
                agent.position_norm = candidate;
                return;
            }

            // Deterministic axis detour keeps ground agents out of blocked cells and footprints.
            float[] horizontal = new[] { candidate[0], agent.position_norm[1] };
            float[] vertical = new[] { agent.position_norm[0], candidate[1] };
            if (!IsPathBlocked(agent.species, agent.position_norm, horizontal))
            {
                agent.position_norm = horizontal;
            }
            else if (!IsPathBlocked(agent.species, agent.position_norm, vertical))
            {
                agent.position_norm = vertical;
            }
            else
            {
                Remember(agent, CityWildlifeMemoryType.Threat, agent.target_patch_id,
                    target.position_norm, 0.55f, true);
                RecordOutcome(CityWildlifeOutcomeType.AvoidedDisturbance, agent,
                    agent.target_patch_id, "blocked-route");
            }
        }

        private void UpdateFeeding(
            CityWildlifeAgent agent,
            float deltaSeconds,
            CityEnvironmentSnapshot environment)
        {
            feedingCooldowns.TryGetValue(agent.id, out float cooldown);
            cooldown = Mathf.Max(0f, cooldown - deltaSeconds);
            CityPatchPressure target = (environment.patch_pressures ?? Array.Empty<CityPatchPressure>())
                .FirstOrDefault(patch => patch.patch_id == agent.target_patch_id);
            if (target != null && cooldown <= Epsilon &&
                DistanceUnits(agent.position_norm, target.position_norm) <= configuration.target_reached_units &&
                target.natural_food + target.anthropogenic_food > Epsilon)
            {
                bool humanFood = target.anthropogenic_food > target.natural_food;
                agent.feed_events += 1;
                feedingCooldowns[agent.id] = configuration.feed_interval_seconds;
                Remember(agent,
                    humanFood ? CityWildlifeMemoryType.HumanFood : CityWildlifeMemoryType.Resource,
                    target.patch_id,
                    target.position_norm,
                    0.8f,
                    false);
                RecordOutcome(CityWildlifeOutcomeType.Fed, agent, target.patch_id,
                    humanFood ? "anthropogenic-food" : "natural-food");
            }
            else
            {
                feedingCooldowns[agent.id] = cooldown;
            }
        }

        private bool DetectRoadkill(CityWildlifeAgent agent, IEnumerable<CityVehicleAgent> vehicles)
        {
            CitySpeciesProfile profile = profiles[agent.species];
            if (!profile.ground_collision_vulnerable)
            {
                return false;
            }
            CityVehicleAgent collision = vehicles.FirstOrDefault(vehicle =>
                DistanceUnits(agent.position_norm, vehicle.position_norm) <= profile.collision_radius_units);
            if (collision == null)
            {
                return false;
            }
            agent.life_state = CityWildlifeLifeState.Dead;
            Remember(agent, CityWildlifeMemoryType.Traffic, "collision", agent.position_norm, 1f, true);
            RecordOutcome(CityWildlifeOutcomeType.Roadkill, agent, agent.target_patch_id, collision.id);
            return true;
        }

        private bool IsPathBlocked(
            CityWildlifeSpecies species,
            float[] start,
            float[] end)
        {
            if (end[0] < 0.01f || end[0] > 0.95f || end[1] < 0.01f || end[1] > 0.99f)
            {
                return true;
            }

            // Pigeons may fly over obstacles, but they cannot finish a step on one.
            if (species == CityWildlifeSpecies.Pigeon)
            {
                if (CityGridResolver.TryGetContainingCell(
                        city.planning_grid,
                        end,
                        out CityGridCell landingCell) &&
                    (landingCell.current_cover == CityLandCover.Water ||
                     landingCell.current_cover == CityLandCover.Building))
                {
                    return true;
                }
                foreach (CityBuilding building in city.buildings ?? Array.Empty<CityBuilding>())
                {
                    if (building == null || building.construction_state != CityConstructionState.Existing)
                    {
                        continue;
                    }
                    float halfWidth = building.footprint_units[0] / city.bounds.width_units * 0.55f;
                    float halfHeight = building.footprint_units[1] / city.bounds.height_units * 0.55f;
                    if (Mathf.Abs(end[0] - building.position_norm[0]) <= halfWidth &&
                        Mathf.Abs(end[1] - building.position_norm[1]) <= halfHeight)
                    {
                        return true;
                    }
                }
                return false;
            }

            foreach (CityGridCell cell in city.planning_grid?.cells ?? Array.Empty<CityGridCell>())
            {
                if (cell == null ||
                    (cell.current_cover != CityLandCover.Water &&
                     cell.current_cover != CityLandCover.Building) ||
                    !CityGridResolver.IsUnitPoint(cell.center_norm) ||
                    !CityGridResolver.IsUnitSize(cell.size_norm))
                {
                    continue;
                }
                if (SegmentIntersectsRectangle(
                        start,
                        end,
                        cell.center_norm[0],
                        cell.center_norm[1],
                        cell.size_norm[0] * 0.5f,
                        cell.size_norm[1] * 0.5f))
                {
                    return true;
                }
            }

            foreach (CityBuilding building in city.buildings ?? Array.Empty<CityBuilding>())
            {
                if (building == null || building.construction_state != CityConstructionState.Existing)
                {
                    continue;
                }
                float halfWidth = building.footprint_units[0] / city.bounds.width_units * 0.55f;
                float halfHeight = building.footprint_units[1] / city.bounds.height_units * 0.55f;
                if (SegmentIntersectsRectangle(
                        start,
                        end,
                        building.position_norm[0],
                        building.position_norm[1],
                        halfWidth,
                        halfHeight))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SegmentIntersectsRectangle(
            float[] start,
            float[] end,
            float centreX,
            float centreY,
            float halfWidth,
            float halfHeight)
        {
            float minimum = 0f;
            float maximum = 1f;
            return ClipSegmentAxis(
                       start[0],
                       end[0] - start[0],
                       centreX - halfWidth,
                       centreX + halfWidth,
                       ref minimum,
                       ref maximum) &&
                   ClipSegmentAxis(
                       start[1],
                       end[1] - start[1],
                       centreY - halfHeight,
                       centreY + halfHeight,
                       ref minimum,
                       ref maximum);
        }

        private static bool ClipSegmentAxis(
            float start,
            float delta,
            float lower,
            float upper,
            ref float minimum,
            ref float maximum)
        {
            if (Mathf.Abs(delta) <= Epsilon)
            {
                return start >= lower - Epsilon && start <= upper + Epsilon;
            }
            float first = (lower - start) / delta;
            float second = (upper - start) / delta;
            if (first > second)
            {
                float swap = first;
                first = second;
                second = swap;
            }
            minimum = Mathf.Max(minimum, first);
            maximum = Mathf.Min(maximum, second);
            return minimum <= maximum + Epsilon;
        }

        private void RecordOutcome(
            CityWildlifeOutcomeType type,
            CityWildlifeAgent agent,
            string locationId,
            string causeId)
        {
            outcomes.Add(new CityWildlifeOutcome
            {
                type = type,
                agent_id = agent.id,
                species = agent.species,
                location_id = locationId,
                cause_id = causeId,
                cycle_index = CycleIndex,
                position_norm = ClonePoint(agent.position_norm),
            });
        }

        private void RefreshSnapshot()
        {
            Snapshot = new CityWildlifeSnapshot
            {
                cycle_index = CycleIndex,
                active_count = agents.Count(agent => agent.life_state == CityWildlifeLifeState.Active),
                migrated_count = agents.Count(agent => agent.life_state == CityWildlifeLifeState.Migrated),
                dead_count = agents.Count(agent => agent.life_state == CityWildlifeLifeState.Dead),
                feeding_events = agents.Sum(agent => agent.feed_events),
                roadkill_events = outcomes.Count(outcome => outcome.type == CityWildlifeOutcomeType.Roadkill),
                agents = agents.ToArray(),
                permanent_outcomes = outcomes.ToArray(),
            };
        }

        private IEnumerable<CityGreenPatch> OperationalPatches()
        {
            return (city.green_patches ?? Array.Empty<CityGreenPatch>())
                .Where(patch => patch != null && patch.construction_state == CityConstructionState.Existing);
        }

        private float DistanceUnits(float[] first, float[] second)
        {
            float x = (first[0] - second[0]) * city.bounds.width_units;
            float y = (first[1] - second[1]) * city.bounds.height_units;
            return Mathf.Sqrt(x * x + y * y);
        }

        private static float[] PolygonCentroid(float[][] points)
        {
            float[][] valid = (points ?? Array.Empty<float[]>())
                .Where(point => point != null && point.Length == 2)
                .ToArray();
            return valid.Length == 0
                ? new[] { 0.5f, 0.5f }
                : new[] { valid.Average(point => point[0]), valid.Average(point => point[1]) };
        }

        private static float[] ClonePoint(float[] point)
        {
            return point == null ? null : (float[])point.Clone();
        }

        private static bool SamePoint(float[] first, float[] second)
        {
            return first != null && second != null && first.Length == 2 && second.Length == 2 &&
                   Mathf.Abs(first[0] - second[0]) <= Epsilon &&
                   Mathf.Abs(first[1] - second[1]) <= Epsilon;
        }

        private static void ValidateConfiguration(CityWildlifeConfiguration configuration)
        {
            if (configuration.memory_limit < 6 || configuration.memory_limit > 10 ||
                configuration.positive_memory_decay_per_cycle < 0f ||
                configuration.negative_memory_decay_per_cycle < 0f ||
                configuration.negative_memory_decay_per_cycle >=
                configuration.positive_memory_decay_per_cycle ||
                configuration.migration_grace_seconds < 0f ||
                configuration.target_reached_units <= 0f ||
                configuration.feed_interval_seconds <= 0f)
            {
                throw new ArgumentException("City wildlife configuration is invalid.", nameof(configuration));
            }
        }

        private static IEnumerable<CitySpeciesProfile> CreateProfiles()
        {
            yield return new CitySpeciesProfile
            {
                species = CityWildlifeSpecies.Pigeon,
                natural_food_weight = 0.55f,
                anthropogenic_food_weight = 1.15f,
                shelter_weight = 0.25f,
                disturbance_weight = 0.28f,
                traffic_weight = 0.18f,
                travel_cost_weight = 0.22f,
                positive_memory_weight = 0.55f,
                negative_memory_weight = 0.42f,
                speed_units_per_second = 1.4f,
                collision_radius_units = 0.42f,
                ground_collision_vulnerable = false,
            };
            yield return new CitySpeciesProfile
            {
                species = CityWildlifeSpecies.GreySquirrel,
                natural_food_weight = 0.95f,
                anthropogenic_food_weight = 0.62f,
                shelter_weight = 0.95f,
                disturbance_weight = 0.85f,
                traffic_weight = 0.78f,
                travel_cost_weight = 0.34f,
                positive_memory_weight = 0.85f,
                negative_memory_weight = 0.92f,
                speed_units_per_second = 1.1f,
                collision_radius_units = 0.38f,
                ground_collision_vulnerable = true,
            };
            yield return new CitySpeciesProfile
            {
                species = CityWildlifeSpecies.Fox,
                natural_food_weight = 0.72f,
                anthropogenic_food_weight = 0.78f,
                shelter_weight = 1.05f,
                disturbance_weight = 1.0f,
                traffic_weight = 0.90f,
                travel_cost_weight = 0.24f,
                positive_memory_weight = 0.72f,
                negative_memory_weight = 1.0f,
                speed_units_per_second = 1.25f,
                collision_radius_units = 0.55f,
                ground_collision_vulnerable = true,
            };
            yield return new CitySpeciesProfile
            {
                species = CityWildlifeSpecies.Hedgehog,
                natural_food_weight = 0.82f,
                anthropogenic_food_weight = 0.35f,
                shelter_weight = 1.20f,
                disturbance_weight = 1.15f,
                traffic_weight = 1.30f,
                travel_cost_weight = 0.48f,
                positive_memory_weight = 0.62f,
                negative_memory_weight = 1.18f,
                speed_units_per_second = 0.55f,
                collision_radius_units = 0.34f,
                ground_collision_vulnerable = true,
            };
        }
    }
}

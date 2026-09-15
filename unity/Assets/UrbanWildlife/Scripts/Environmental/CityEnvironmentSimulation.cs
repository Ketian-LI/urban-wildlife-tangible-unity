using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.City;

namespace UrbanWildlife.Environmental
{
    public sealed class CityEnvironmentSimulation
    {
        private const float Epsilon = 0.0001f;
        private readonly CityState city;
        private readonly CityEnvironmentConfiguration configuration;

        public CityEnvironmentSimulation(
            CityState city,
            CityEnvironmentConfiguration configuration = null)
        {
            CityStateValidationResult validation = CityStateValidator.Validate(city);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.Summary, nameof(city));
            }
            this.city = city;
            this.configuration = configuration ?? new CityEnvironmentConfiguration();
            if (this.configuration.overflow_grace_seconds < 0f ||
                this.configuration.building_influence_radius_units <= 0f ||
                this.configuration.road_influence_radius_units <= 0f ||
                this.configuration.litter_influence_radius_units <= 0f ||
                this.configuration.overflow_food_multiplier < 0f)
            {
                throw new ArgumentException("City environment configuration is invalid.", nameof(configuration));
            }
            SynchronizeWasteInfrastructure();
            Tick(0f, 1f);
        }

        public CityEnvironmentSnapshot Snapshot { get; private set; }

        public void Tick(float deltaSeconds, float activityMultiplier = 1f)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }
            if (activityMultiplier < 0f || float.IsNaN(activityMultiplier) ||
                float.IsInfinity(activityMultiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(activityMultiplier));
            }

            SynchronizeWasteInfrastructure();
            CityWasteNode[] nodes = city.waste.nodes ?? Array.Empty<CityWasteNode>();
            CityWasteNode[] outputs = nodes
                .Where(node => node != null && node.type == CityWasteNodeType.BuildingOutput &&
                               BuildingIsOperational(node.source_building_id))
                .ToArray();
            CityWasteNode[] bins = nodes
                .Where(node => node != null && node.type == CityWasteNodeType.Bin)
                .ToArray();

            float demand = outputs.Sum(node => node.generation_rate) * activityMultiplier;
            float capacity = bins.Sum(node => node.capacity);
            bool pressureExists = demand > capacity + Epsilon;
            city.waste.overflow_elapsed_seconds = pressureExists
                ? city.waste.overflow_elapsed_seconds + deltaSeconds
                : 0f;
            city.waste.overflow_active = pressureExists &&
                                         city.waste.overflow_elapsed_seconds >=
                                         configuration.overflow_grace_seconds;
            city.waste.total_demand = demand;
            city.waste.total_capacity = capacity;

            float utilisation = capacity <= Epsilon ? (demand > 0f ? 1f : 0f) : demand / capacity;
            foreach (CityWasteNode bin in bins)
            {
                bin.fill_ratio = Mathf.Clamp01(utilisation);
            }
            foreach (CityWasteNode output in outputs)
            {
                output.fill_ratio = 0f;
            }

            city.waste.litter_hotspots = city.waste.overflow_active
                ? CreateLitterHotspots(outputs, demand, capacity)
                : Array.Empty<CityLitterHotspot>();
            Snapshot = BuildSnapshot(activityMultiplier, utilisation);
        }

        private void SynchronizeWasteInfrastructure()
        {
            city.waste ??= new CityWasteSystem();
            List<CityWasteNode> nodes = (city.waste.nodes ?? Array.Empty<CityWasteNode>())
                .Where(node => node != null && node.type == CityWasteNodeType.BuildingOutput)
                .ToList();
            HashSet<string> outputBuildings = new HashSet<string>(
                nodes.Select(node => node.source_building_id ?? string.Empty));
            foreach (CityBuilding building in city.buildings ?? Array.Empty<CityBuilding>())
            {
                if (building == null || outputBuildings.Contains(building.id))
                {
                    continue;
                }
                nodes.Add(new CityWasteNode
                {
                    id = $"waste-{building.id}",
                    type = CityWasteNodeType.BuildingOutput,
                    source_building_id = building.id,
                    position_norm = ClonePoint(building.position_norm),
                    generation_rate = building.waste_output,
                    capacity = 0f,
                    fill_ratio = 0f,
                });
            }

            foreach (CityAmenity amenity in ActiveAmenities(CityAmenityType.Bin))
            {
                nodes.Add(new CityWasteNode
                {
                    id = $"waste-{amenity.id}",
                    type = CityWasteNodeType.Bin,
                    source_building_id = null,
                    position_norm = ClonePoint(amenity.position_norm),
                    generation_rate = 0f,
                    capacity = amenity.waste_capacity,
                    fill_ratio = 0f,
                });
            }
            city.waste.nodes = nodes.ToArray();
        }

        private CityLitterHotspot[] CreateLitterHotspots(
            CityWasteNode[] outputs,
            float demand,
            float capacity)
        {
            float overflowShare = demand <= Epsilon
                ? 0f
                : Mathf.Clamp01((demand - capacity) / demand);
            float maxGeneration = Mathf.Max(Epsilon, outputs.Select(node => node.generation_rate).DefaultIfEmpty(0f).Max());
            return outputs.Select(node => new CityLitterHotspot
            {
                id = $"litter-{node.source_building_id}",
                source_id = node.source_building_id,
                position_norm = ClonePoint(node.position_norm),
                intensity = Mathf.Clamp01(overflowShare * (0.35f + 0.65f * node.generation_rate / maxGeneration)),
            }).ToArray();
        }

        private CityEnvironmentSnapshot BuildSnapshot(float activityMultiplier, float utilisation)
        {
            List<CityFoodSource> foodSources = new List<CityFoodSource>();
            foreach (CityGreenPatch patch in OperationalPatches())
            {
                if (patch.natural_food_value <= Epsilon)
                {
                    continue;
                }
                // Keep natural-food supply proportional to habitat area. The lower floor is
                // deliberately below one 5 x 5 planning cell so grid refinement cannot multiply
                // the city's total food merely by splitting one habitat into more records.
                float sizeFactor = Mathf.Clamp(patch.patch_size_units / 300f, 0.05f, 1.5f);
                foodSources.Add(new CityFoodSource
                {
                    id = $"food-natural-{patch.id}",
                    kind = CityFoodSourceKind.Natural,
                    source_id = patch.id,
                    position_norm = PolygonCentroid(patch.polygon_norm),
                    availability = patch.natural_food_value * sizeFactor,
                    caused_by_overflow = false,
                });
            }

            foreach (CityBuilding building in OperationalBuildings())
            {
                float reduction = BinExposureReduction(building.position_norm);
                float availability = building.anthropogenic_food_output * activityMultiplier * (1f - reduction);
                if (availability <= Epsilon)
                {
                    continue;
                }
                foodSources.Add(new CityFoodSource
                {
                    id = $"food-human-{building.id}",
                    kind = CityFoodSourceKind.Anthropogenic,
                    source_id = building.id,
                    position_norm = ClonePoint(building.position_norm),
                    availability = availability,
                    caused_by_overflow = false,
                });
            }

            foreach (CityLitterHotspot hotspot in city.waste.litter_hotspots ?? Array.Empty<CityLitterHotspot>())
            {
                foodSources.Add(new CityFoodSource
                {
                    id = $"food-{hotspot.id}",
                    kind = CityFoodSourceKind.Anthropogenic,
                    source_id = hotspot.id,
                    position_norm = ClonePoint(hotspot.position_norm),
                    availability = hotspot.intensity * configuration.overflow_food_multiplier,
                    caused_by_overflow = true,
                });
            }

            CityPatchPressure[] pressures = OperationalPatches()
                .Select(patch => BuildPatchPressure(patch, foodSources, activityMultiplier))
                .ToArray();
            return new CityEnvironmentSnapshot
            {
                natural_food_total = foodSources
                    .Where(source => source.kind == CityFoodSourceKind.Natural)
                    .Sum(source => source.availability),
                anthropogenic_food_total = foodSources
                    .Where(source => source.kind == CityFoodSourceKind.Anthropogenic)
                    .Sum(source => source.availability),
                average_disturbance = pressures.Length == 0
                    ? 0f
                    : pressures.Average(pressure => pressure.human_disturbance),
                waste_demand = city.waste.total_demand,
                waste_capacity = city.waste.total_capacity,
                waste_utilisation = utilisation,
                overflow_active = city.waste.overflow_active,
                overflow_elapsed_seconds = city.waste.overflow_elapsed_seconds,
                food_sources = foodSources.ToArray(),
                patch_pressures = pressures,
            };
        }

        private CityPatchPressure BuildPatchPressure(
            CityGreenPatch patch,
            IReadOnlyCollection<CityFoodSource> foodSources,
            float activityMultiplier)
        {
            float[] centre = PolygonCentroid(patch.polygon_norm);
            float buildingPressure = OperationalBuildings().Sum(building =>
                building.disturbance_output * activityMultiplier *
                Influence(centre, building.position_norm, configuration.building_influence_radius_units));
            float benchPressure = ActiveAmenities(CityAmenityType.Bench).Sum(bench =>
                bench.human_activity_weight * Influence(centre, bench.position_norm, bench.service_radius_units));
            float traffic = (city.vehicle_roads ?? Array.Empty<CityVehicleRoad>())
                .Where(RoadIsOperational)
                .Sum(road => road.traffic_load * PolylineInfluence(
                    centre,
                    road.points_norm,
                    configuration.road_influence_radius_units));
            float litter = (city.waste.litter_hotspots ?? Array.Empty<CityLitterHotspot>())
                .Where(hotspot => hotspot != null)
                .Sum(hotspot => hotspot.intensity * Influence(
                    centre,
                    hotspot.position_norm,
                    configuration.litter_influence_radius_units));
            float anthropogenicFood = foodSources
                .Where(source => source.kind == CityFoodSourceKind.Anthropogenic)
                .Sum(source => source.availability * Influence(
                    centre,
                    source.position_norm,
                    configuration.building_influence_radius_units));
            return new CityPatchPressure
            {
                patch_id = patch.id,
                position_norm = centre,
                natural_food = patch.natural_food_value,
                anthropogenic_food = anthropogenicFood,
                shelter = patch.shelter_value,
                human_disturbance = Mathf.Clamp01(
                    patch.human_disturbance + 0.32f * buildingPressure +
                    0.24f * benchPressure + 0.28f * traffic + 0.22f * litter),
                traffic_exposure = Mathf.Clamp01(traffic),
                litter_pressure = Mathf.Clamp01(litter),
            };
        }

        private float BinExposureReduction(float[] position)
        {
            return ActiveAmenities(CityAmenityType.Bin)
                .Select(bin => bin.food_exposure_reduction *
                               Influence(position, bin.position_norm, bin.service_radius_units))
                .DefaultIfEmpty(0f)
                .Max();
        }

        private IEnumerable<CityBuilding> OperationalBuildings()
        {
            return (city.buildings ?? Array.Empty<CityBuilding>())
                .Where(building => building != null &&
                                   building.construction_state == CityConstructionState.Existing);
        }

        private IEnumerable<CityGreenPatch> OperationalPatches()
        {
            return (city.green_patches ?? Array.Empty<CityGreenPatch>())
                .Where(patch => patch != null && patch.construction_state == CityConstructionState.Existing);
        }

        private IEnumerable<CityAmenity> ActiveAmenities(CityAmenityType type)
        {
            return (city.amenities ?? Array.Empty<CityAmenity>())
                .Where(amenity => amenity != null && amenity.type == type &&
                                  amenity.construction_state == CityConstructionState.Existing);
        }

        private bool BuildingIsOperational(string buildingId)
        {
            return OperationalBuildings().Any(building => building.id == buildingId);
        }

        private static bool RoadIsOperational(CityVehicleRoad road)
        {
            return road != null && road.construction_state == CityConstructionState.Existing;
        }

        private float Influence(float[] from, float[] to, float radiusUnits)
        {
            float distance = DistanceUnits(from, to);
            return 1f - Mathf.Clamp01(distance / Mathf.Max(Epsilon, radiusUnits));
        }

        private float PolylineInfluence(float[] position, float[][] points, float radiusUnits)
        {
            float nearest = (points ?? Array.Empty<float[]>())
                .Where(point => point != null && point.Length == 2)
                .Select(point => DistanceUnits(position, point))
                .DefaultIfEmpty(float.PositiveInfinity)
                .Min();
            return float.IsInfinity(nearest)
                ? 0f
                : 1f - Mathf.Clamp01(nearest / Mathf.Max(Epsilon, radiusUnits));
        }

        private float DistanceUnits(float[] a, float[] b)
        {
            float dx = (a[0] - b[0]) * city.bounds.width_units;
            float dy = (a[1] - b[1]) * city.bounds.height_units;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static float[] PolygonCentroid(float[][] points)
        {
            float[][] valid = (points ?? Array.Empty<float[]>())
                .Where(point => point != null && point.Length == 2)
                .ToArray();
            if (valid.Length == 0)
            {
                return new[] { 0.5f, 0.5f };
            }
            return new[]
            {
                valid.Average(point => point[0]),
                valid.Average(point => point[1]),
            };
        }

        private static float[] ClonePoint(float[] point)
        {
            return point == null ? null : (float[])point.Clone();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlife.City
{
    public sealed class CityStateValidationResult
    {
        public CityStateValidationResult(IEnumerable<string> errors)
        {
            Errors = errors == null ? Array.Empty<string>() : errors.ToArray();
        }

        public string[] Errors { get; }
        public bool IsValid => Errors.Length == 0;
        public string Summary => IsValid ? "City state is valid." : string.Join("; ", Errors);
    }

    public static class CityStateValidator
    {
        public static CityStateValidationResult Validate(CityState state)
        {
            List<string> errors = new List<string>();
            if (state == null)
            {
                errors.Add("City state is missing.");
                return new CityStateValidationResult(errors);
            }

            if (state.schema_version != CityStateContract.SchemaVersion)
            {
                errors.Add($"Unsupported city schema version '{state.schema_version}'.");
            }
            if (string.IsNullOrWhiteSpace(state.city_id))
            {
                errors.Add("City ID is required.");
            }
            if (string.IsNullOrWhiteSpace(state.source_contract))
            {
                errors.Add("City source contract is required.");
            }
            if (state.revision < 0)
            {
                errors.Add("City revision cannot be negative.");
            }
            if (state.bounds == null || state.bounds.origin != "top_left" ||
                !Positive(state.bounds.width_units) || !Positive(state.bounds.height_units))
            {
                errors.Add("City bounds must use top_left origin and positive finite dimensions.");
            }

            CityBuilding[] buildings = state.buildings ?? Array.Empty<CityBuilding>();
            CityGreenPatch[] patches = state.green_patches ?? Array.Empty<CityGreenPatch>();
            CityVehicleRoad[] roads = state.vehicle_roads ?? Array.Empty<CityVehicleRoad>();
            CityPedestrianLink[] links = state.pedestrian_links ?? Array.Empty<CityPedestrianLink>();
            ValidateUniqueIds(buildings.Select(item => item?.id), "building", errors);
            ValidateUniqueIds(patches.Select(item => item?.id), "green patch", errors);
            ValidateUniqueIds(roads.Select(item => item?.id), "vehicle road", errors);
            ValidateUniqueIds(links.Select(item => item?.id), "pedestrian link", errors);

            HashSet<string> buildingIds = new HashSet<string>(
                buildings.Where(item => item != null && !string.IsNullOrWhiteSpace(item.id))
                    .Select(item => item.id));
            HashSet<string> patchIds = new HashSet<string>(
                patches.Where(item => item != null && !string.IsNullOrWhiteSpace(item.id))
                    .Select(item => item.id));
            HashSet<string> roadIds = new HashSet<string>(
                roads.Where(item => item != null && !string.IsNullOrWhiteSpace(item.id))
                    .Select(item => item.id));
            HashSet<string> linkIds = new HashSet<string>(
                links.Where(item => item != null && !string.IsNullOrWhiteSpace(item.id))
                    .Select(item => item.id));
            foreach (CityBuilding building in buildings.Where(item => item != null))
            {
                ValidatePoint(building.position_norm, $"Building {building.id} position", errors);
                if (building.footprint_units == null || building.footprint_units.Length != 2 ||
                    building.footprint_units.Any(value => !Positive(value)))
                {
                    errors.Add($"Building {building.id} footprint must contain two positive dimensions.");
                }
                if (building.housing_capacity < 0 || building.comfortable_capacity < 0 ||
                    !NonNegative(building.human_origin_rate) ||
                    !NonNegative(building.human_destination_weight) ||
                    !NonNegative(building.vehicle_demand) || !NonNegative(building.waste_output) ||
                    !NonNegative(building.anthropogenic_food_output) ||
                    !NonNegative(building.disturbance_output))
                {
                    errors.Add($"Building {building.id} contains a negative or non-finite city pressure value.");
                }
                ValidateReferences(
                    building.vehicle_road_ids,
                    roadIds,
                    $"Building {building.id}",
                    "vehicle road",
                    errors);
                ValidateReferences(
                    building.pedestrian_link_ids,
                    linkIds,
                    $"Building {building.id}",
                    "pedestrian link",
                    errors);
            }

            foreach (CityGreenPatch patch in patches.Where(item => item != null))
            {
                ValidatePolyline(patch.polygon_norm, 3, $"Green patch {patch.id} polygon", errors);
                if (!UnitValue(patch.shelter_value) || !UnitValue(patch.natural_food_value) ||
                    !UnitValue(patch.human_disturbance) || !Positive(patch.patch_size_units))
                {
                    errors.Add($"Green patch {patch.id} has invalid ecological field values.");
                }
                ValidateReferences(
                    patch.connected_patch_ids,
                    patchIds,
                    $"Green patch {patch.id}",
                    "green patch",
                    errors);
            }
            if (!patches.Any(item => item != null && item.natural_food_value > 0f))
            {
                errors.Add("At least one green patch must retain Natural Food.");
            }

            foreach (CityVehicleRoad road in roads.Where(item => item != null))
            {
                ValidatePolyline(road.points_norm, 2, $"Vehicle road {road.id}", errors);
                if (!Positive(road.width_units) || !Positive(road.speed_units_per_second) ||
                    !UnitValue(road.traffic_load))
                {
                    errors.Add($"Vehicle road {road.id} has invalid width, speed or traffic load.");
                }
                ValidateReferences(
                    road.connected_building_ids,
                    buildingIds,
                    $"Vehicle road {road.id}",
                    "building",
                    errors);
                ValidateReferences(
                    road.connected_road_ids,
                    roadIds,
                    $"Vehicle road {road.id}",
                    "vehicle road",
                    errors);
                if ((road.connected_road_ids ?? Array.Empty<string>()).Contains(road.id))
                {
                    errors.Add($"Vehicle road {road.id} cannot connect to itself.");
                }
            }

            foreach (CityPedestrianLink link in links.Where(item => item != null))
            {
                ValidatePolyline(link.points_norm, 2, $"Pedestrian link {link.id}", errors);
                if (!Positive(link.width_units))
                {
                    errors.Add($"Pedestrian link {link.id} width must be positive.");
                }
                ValidateReferences(
                    link.connected_building_ids,
                    buildingIds,
                    $"Pedestrian link {link.id}",
                    "building",
                    errors);
                ValidateReferences(
                    link.connected_link_ids,
                    linkIds,
                    $"Pedestrian link {link.id}",
                    "pedestrian link",
                    errors);
                if ((link.connected_link_ids ?? Array.Empty<string>()).Contains(link.id))
                {
                    errors.Add($"Pedestrian link {link.id} cannot connect to itself.");
                }
            }

            ValidateWaste(state.waste, buildingIds, errors);
            return new CityStateValidationResult(errors);
        }

        private static void ValidateWaste(
            CityWasteSystem waste,
            HashSet<string> buildingIds,
            List<string> errors)
        {
            if (waste == null)
            {
                errors.Add("Waste system is required.");
                return;
            }
            if (!NonNegative(waste.total_demand) || !NonNegative(waste.total_capacity) ||
                !NonNegative(waste.overflow_elapsed_seconds))
            {
                errors.Add("Waste totals must be finite and non-negative.");
            }

            CityWasteNode[] nodes = waste.nodes ?? Array.Empty<CityWasteNode>();
            ValidateUniqueIds(nodes.Select(item => item?.id), "waste node", errors);
            foreach (CityWasteNode node in nodes.Where(item => item != null))
            {
                ValidatePoint(node.position_norm, $"Waste node {node.id} position", errors);
                if (!NonNegative(node.generation_rate) || !NonNegative(node.capacity) ||
                    !UnitValue(node.fill_ratio))
                {
                    errors.Add($"Waste node {node.id} has invalid values.");
                }
                if (node.type == CityWasteNodeType.BuildingOutput &&
                    !buildingIds.Contains(node.source_building_id ?? string.Empty))
                {
                    errors.Add($"Waste node {node.id} references an unknown building.");
                }
            }

            CityLitterHotspot[] hotspots = waste.litter_hotspots ?? Array.Empty<CityLitterHotspot>();
            ValidateUniqueIds(hotspots.Select(item => item?.id), "litter hotspot", errors);
            foreach (CityLitterHotspot hotspot in hotspots.Where(item => item != null))
            {
                ValidatePoint(hotspot.position_norm, $"Litter hotspot {hotspot.id} position", errors);
                if (!UnitValue(hotspot.intensity))
                {
                    errors.Add($"Litter hotspot {hotspot.id} intensity must be between zero and one.");
                }
            }
        }

        private static void ValidateUniqueIds(
            IEnumerable<string> ids,
            string label,
            List<string> errors)
        {
            string[] values = ids.ToArray();
            if (values.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"Every {label} requires an ID.");
            }
            string[] duplicates = values.Where(value => !string.IsNullOrWhiteSpace(value))
                .GroupBy(value => value)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicates.Length > 0)
            {
                errors.Add($"Duplicate {label} IDs: {string.Join(",", duplicates)}.");
            }
        }

        private static void ValidateReferences(
            IEnumerable<string> references,
            HashSet<string> validIds,
            string owner,
            string targetLabel,
            List<string> errors)
        {
            foreach (string reference in references ?? Array.Empty<string>())
            {
                if (!validIds.Contains(reference))
                {
                    errors.Add($"{owner} references unknown {targetLabel} {reference}.");
                }
            }
        }

        private static void ValidatePolyline(
            float[][] points,
            int minimumPoints,
            string label,
            List<string> errors)
        {
            if (points == null || points.Length < minimumPoints)
            {
                errors.Add($"{label} requires at least {minimumPoints} points.");
                return;
            }
            for (int index = 0; index < points.Length; index += 1)
            {
                ValidatePoint(points[index], $"{label} point {index}", errors);
            }
        }

        private static void ValidatePoint(float[] point, string label, List<string> errors)
        {
            if (point == null || point.Length != 2 || !UnitValue(point[0]) || !UnitValue(point[1]))
            {
                errors.Add($"{label} must contain two normalized coordinates.");
            }
        }

        private static bool Positive(float value)
        {
            return value > 0f && IsFinite(value);
        }

        private static bool NonNegative(float value)
        {
            return value >= 0f && IsFinite(value);
        }

        private static bool UnitValue(float value)
        {
            return value >= 0f && value <= 1f && IsFinite(value);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

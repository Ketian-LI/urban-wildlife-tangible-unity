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
            CityAmenity[] amenities = state.amenities ?? Array.Empty<CityAmenity>();
            ValidateUniqueIds(buildings.Select(item => item?.id), "building", errors);
            ValidateUniqueIds(patches.Select(item => item?.id), "green patch", errors);
            ValidateUniqueIds(roads.Select(item => item?.id), "vehicle road", errors);
            ValidateUniqueIds(links.Select(item => item?.id), "pedestrian link", errors);
            ValidateUniqueIds(amenities.Select(item => item?.id), "amenity", errors);

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
            ValidatePlanningGrid(state, buildings, patches, buildingIds, patchIds, errors);
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

            foreach (CityAmenity amenity in amenities.Where(item => item != null))
            {
                ValidatePoint(amenity.position_norm, $"Amenity {amenity.id} position", errors);
                if (!Positive(amenity.service_radius_units) ||
                    !UnitValue(amenity.human_activity_weight) ||
                    !NonNegative(amenity.waste_capacity) ||
                    !UnitValue(amenity.food_exposure_reduction))
                {
                    errors.Add($"Amenity {amenity.id} has invalid service or pressure values.");
                }
                if (amenity.type == CityAmenityType.Bin && amenity.waste_capacity <= 0f)
                {
                    errors.Add($"Bin {amenity.id} must provide positive waste capacity.");
                }
                if (amenity.type == CityAmenityType.Bench && amenity.waste_capacity > 0f)
                {
                    errors.Add($"Bench {amenity.id} cannot provide waste capacity.");
                }
            }

            ValidateWaste(state.waste, buildingIds, errors);
            return new CityStateValidationResult(errors);
        }

        private static void ValidatePlanningGrid(
            CityState state,
            CityBuilding[] buildings,
            CityGreenPatch[] patches,
            HashSet<string> buildingIds,
            HashSet<string> patchIds,
            List<string> errors)
        {
            CityPlanningGrid grid = state.planning_grid;
            if (grid == null)
            {
                // The planning grid was introduced after schema 0.1 states were already in use.
                return;
            }

            bool expectedDimensions = grid.cols == CityPlanningGrid.DefaultColumns &&
                                      grid.rows == CityPlanningGrid.DefaultRows;
            if (!expectedDimensions)
            {
                errors.Add(
                    $"Planning grid must be {CityPlanningGrid.DefaultColumns} columns by " +
                    $"{CityPlanningGrid.DefaultRows} rows.");
            }

            if (grid.cells == null)
            {
                errors.Add("Planning grid cells are required.");
                return;
            }
            if (grid.cells.Length != CityPlanningGrid.DefaultCellCount)
            {
                errors.Add(
                    $"Planning grid must contain exactly {CityPlanningGrid.DefaultCellCount} cells.");
            }

            ValidateUniqueIds(grid.cells.Select(cell => cell?.id), "planning grid cell", errors);
            Dictionary<string, CityGridCell> cellsById = new Dictionary<string, CityGridCell>(
                StringComparer.Ordinal);
            HashSet<string> coordinates = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> occupiedPatchIds = new HashSet<string>(StringComparer.Ordinal);
            int buildableCellCount = 0;

            foreach (CityGridCell cell in grid.cells)
            {
                if (cell == null)
                {
                    errors.Add("Planning grid cannot contain a null cell.");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(cell.id) && !cellsById.ContainsKey(cell.id))
                {
                    cellsById.Add(cell.id, cell);
                }

                bool coordinateInRange = cell.row >= 0 && cell.row < grid.rows &&
                                         cell.col >= 0 && cell.col < grid.cols;
                if (!coordinateInRange)
                {
                    errors.Add($"Planning grid cell {cell.id} has an out-of-range row or column.");
                }
                else if (!coordinates.Add($"{cell.row}:{cell.col}"))
                {
                    errors.Add(
                        $"Planning grid contains more than one cell at row {cell.row}, column {cell.col}.");
                }

                if (!CityGridResolver.IsUnitPoint(cell.center_norm) ||
                    !CityGridResolver.IsUnitSize(cell.size_norm))
                {
                    errors.Add($"Planning grid cell {cell.id} has invalid normalized geometry.");
                }
                else
                {
                    float halfWidth = cell.size_norm[0] * 0.5f;
                    float halfHeight = cell.size_norm[1] * 0.5f;
                    if (cell.center_norm[0] - halfWidth < -CityGridResolver.GeometryTolerance ||
                        cell.center_norm[0] + halfWidth > 1f + CityGridResolver.GeometryTolerance ||
                        cell.center_norm[1] - halfHeight < -CityGridResolver.GeometryTolerance ||
                        cell.center_norm[1] + halfHeight > 1f + CityGridResolver.GeometryTolerance)
                    {
                        errors.Add($"Planning grid cell {cell.id} extends outside normalized city bounds.");
                    }

                    if (coordinateInRange && grid.cols > 0 && grid.rows > 0)
                    {
                        float[] expectedCenter = CityGridResolver.ExpectedCenterNorm(
                            grid,
                            cell.row,
                            cell.col);
                        float[] expectedSize = CityGridResolver.ExpectedSizeNorm(grid);
                        if (!CityGridResolver.Approximately(cell.center_norm[0], expectedCenter[0]) ||
                            !CityGridResolver.Approximately(cell.center_norm[1], expectedCenter[1]) ||
                            !CityGridResolver.Approximately(cell.size_norm[0], expectedSize[0]) ||
                            !CityGridResolver.Approximately(cell.size_norm[1], expectedSize[1]))
                        {
                            errors.Add(
                                $"Planning grid cell {cell.id} geometry does not match its row and column.");
                        }
                    }
                }

                if (!Enum.IsDefined(typeof(CityLandCover), cell.baseline_cover) ||
                    !Enum.IsDefined(typeof(CityLandCover), cell.current_cover))
                {
                    errors.Add($"Planning grid cell {cell.id} has an unknown land cover.");
                }
                if (cell.fixed_feature && cell.buildable)
                {
                    errors.Add($"Fixed planning grid cell {cell.id} cannot be buildable.");
                }
                if (cell.buildable && !cell.fixed_feature)
                {
                    buildableCellCount += 1;
                }
                if ((cell.baseline_cover == CityLandCover.Woodland ||
                     cell.current_cover == CityLandCover.Woodland) && !cell.was_woodland)
                {
                    errors.Add($"Woodland planning grid cell {cell.id} must retain woodland history.");
                }
                if (cell.last_changed_revision < 0 ||
                    cell.last_changed_revision > state.revision)
                {
                    errors.Add(
                        $"Planning grid cell {cell.id} has an invalid last changed revision.");
                }

                ValidateGridCellOccupancy(
                    cell,
                    buildings,
                    patches,
                    buildingIds,
                    patchIds,
                    occupiedPatchIds,
                    errors);
            }

            if (expectedDimensions && coordinates.Count != CityPlanningGrid.DefaultCellCount)
            {
                errors.Add(
                    $"Planning grid must contain each of the {CityPlanningGrid.DefaultCellCount} " +
                    "row and column coordinates once.");
            }
            if (grid.max_active_player_buildings < 0 ||
                grid.max_active_player_buildings > buildableCellCount)
            {
                errors.Add(
                    "Planning grid maximum active player buildings must fit within its buildable cells.");
            }

            foreach (CityBuilding building in buildings.Where(item =>
                         item != null && !string.IsNullOrWhiteSpace(item.planning_cell_id)))
            {
                string[] occupiedCellIds = BuildingCellIds(building);
                if (!cellsById.TryGetValue(building.planning_cell_id, out CityGridCell anchorCell))
                {
                    errors.Add(
                        $"Building {building.id} references unknown planning cell " +
                        $"{building.planning_cell_id}.");
                }
                else if (anchorCell.occupant_id != building.id)
                {
                    errors.Add(
                        $"Building {building.id} and planning cell {anchorCell.id} do not reference each other.");
                }
                if (occupiedCellIds.Length == 0)
                {
                    errors.Add($"Building {building.id} must occupy at least one planning cell.");
                }
                if (occupiedCellIds.Distinct(StringComparer.Ordinal).Count() != occupiedCellIds.Length)
                {
                    errors.Add($"Building {building.id} repeats a planning cell in its footprint.");
                }
                CityGridCell[] expectedFootprint = CityGridResolver.GetBuildingFootprintCellsAtPosition(
                    grid,
                    state.bounds,
                    building,
                    out string footprintError);
                if (!string.IsNullOrEmpty(footprintError))
                {
                    errors.Add($"Building {building.id} has an invalid grid footprint: {footprintError}");
                }
                else if (!new HashSet<string>(
                             expectedFootprint.Select(cell => cell.id),
                             StringComparer.Ordinal).SetEquals(occupiedCellIds))
                {
                    errors.Add(
                        $"Building {building.id} occupied cells do not match its size and rotation.");
                }
                foreach (string occupiedCellId in occupiedCellIds)
                {
                    if (!cellsById.TryGetValue(occupiedCellId, out CityGridCell footprintCell))
                    {
                        errors.Add(
                            $"Building {building.id} references unknown footprint cell {occupiedCellId}.");
                    }
                    else if (footprintCell.occupant_id != building.id)
                    {
                        errors.Add(
                            $"Building {building.id} and footprint cell {footprintCell.id} do not reference each other.");
                    }
                }
            }

            foreach (CityGreenPatch patch in patches.Where(item =>
                         item != null && !string.IsNullOrWhiteSpace(item.planning_cell_id)))
            {
                if (!cellsById.TryGetValue(patch.planning_cell_id, out CityGridCell cell))
                {
                    errors.Add(
                        $"Green patch {patch.id} references unknown planning cell " +
                        $"{patch.planning_cell_id}.");
                }
                else if (cell.habitat_patch_id != patch.id)
                {
                    errors.Add(
                        $"Green patch {patch.id} and planning cell {cell.id} do not reference each other.");
                }
            }
        }

        private static void ValidateGridCellOccupancy(
            CityGridCell cell,
            CityBuilding[] buildings,
            CityGreenPatch[] patches,
            HashSet<string> buildingIds,
            HashSet<string> patchIds,
            HashSet<string> occupiedPatchIds,
            List<string> errors)
        {
            bool hasBuilding = !string.IsNullOrWhiteSpace(cell.occupant_id);
            bool hasPatch = !string.IsNullOrWhiteSpace(cell.habitat_patch_id);
            if (hasBuilding && hasPatch)
            {
                errors.Add($"Planning grid cell {cell.id} cannot contain a building and habitat together.");
            }
            if (cell.current_cover == CityLandCover.Building && !hasBuilding)
            {
                errors.Add($"Building planning grid cell {cell.id} must reference its occupant.");
            }
            if (hasBuilding)
            {
                if (cell.current_cover != CityLandCover.Building)
                {
                    errors.Add(
                        $"Occupied planning grid cell {cell.id} must use Building land cover.");
                }
                if (!buildingIds.Contains(cell.occupant_id))
                {
                    errors.Add(
                        $"Planning grid cell {cell.id} references unknown building {cell.occupant_id}.");
                }
                CityBuilding building = buildings.FirstOrDefault(item =>
                    item != null && item.id == cell.occupant_id);
                if (building != null && !BuildingCellIds(building).Contains(cell.id))
                {
                    errors.Add(
                        $"Planning grid cell {cell.id} and building {building.id} do not reference each other.");
                }
            }

            if (hasPatch)
            {
                if (!CityGridResolver.IsHabitatCover(cell.current_cover))
                {
                    errors.Add(
                        $"Habitat planning grid cell {cell.id} has incompatible land cover.");
                }
                if (!patchIds.Contains(cell.habitat_patch_id))
                {
                    errors.Add(
                        $"Planning grid cell {cell.id} references unknown green patch " +
                        $"{cell.habitat_patch_id}.");
                }
                if (!occupiedPatchIds.Add(cell.habitat_patch_id))
                {
                    errors.Add(
                        $"Green patch {cell.habitat_patch_id} occupies more than one planning grid cell.");
                }
                CityGreenPatch patch = patches.FirstOrDefault(item =>
                    item != null && item.id == cell.habitat_patch_id);
                if (patch != null)
                {
                    if (patch.planning_cell_id != cell.id)
                    {
                        errors.Add(
                            $"Planning grid cell {cell.id} and green patch {patch.id} do not reference each other.");
                    }
                    CityLandCover expectedCover = GreenPatchCover(patch);
                    if (cell.current_cover != expectedCover)
                    {
                        errors.Add(
                            $"Planning grid cell {cell.id} cover does not match green patch {patch.id}.");
                    }
                }
            }
        }

        private static string[] BuildingCellIds(CityBuilding building)
        {
            if (building == null)
            {
                return Array.Empty<string>();
            }
            if (building.planning_cell_ids != null && building.planning_cell_ids.Length > 0)
            {
                return building.planning_cell_ids
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToArray();
            }
            return string.IsNullOrWhiteSpace(building.planning_cell_id)
                ? Array.Empty<string>()
                : new[] { building.planning_cell_id };
        }

        private static CityLandCover GreenPatchCover(CityGreenPatch patch)
        {
            switch (patch.type)
            {
                case CityGreenPatchType.Woodland:
                    return CityLandCover.Woodland;
                case CityGreenPatchType.ShrubGarden:
                    return CityLandCover.ShrubGarden;
                default:
                    return patch.public_park ? CityLandCover.PublicGreen : CityLandCover.OpenLand;
            }
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

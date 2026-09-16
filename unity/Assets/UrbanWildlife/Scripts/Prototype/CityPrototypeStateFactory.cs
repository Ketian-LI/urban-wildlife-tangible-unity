using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;
using UrbanWildlife.Construction;
using UrbanWildlife.Networks;

namespace UrbanWildlife.Prototype
{
    public static class CityPrototypeStateFactory
    {
        public static CityState Create()
        {
            CityBounds bounds = new CityBounds
            {
                origin = "top_left",
                width_units = 90f,
                height_units = 60f,
            };
            CityBuilding[] buildings =
            {
                // The opening state is deliberately sparse. The denser reference is the
                // player's destination, not pre-authored scenery.
                Building("detached-garden", CityBuildingType.DetachedHouse, 110, 0.277778f, 0.25f, 12, 0.35f, 0.15f, 4, 0.45f),
                Building("detached-east", CityBuildingType.DetachedHouse, 111, 0.82f, 0.25f, 12, 0.35f, 0.15f, 4, 0.45f),
            };
            CityPlanningGrid planningGrid = CreatePlanningGrid(buildings, bounds);

            float[][] mainRoadPoints = SmoothPoints(
                (0.00f, 0.295f),
                (0.08f, 0.300f),
                (0.16f, 0.350f),
                (0.23f, 0.395f),
                (0.31f, 0.350f),
                (0.43f, 0.405f),
                (0.55f, 0.410f),
                (0.66f, 0.365f),
                (0.73f, 0.380f),
                (0.82f, 0.395f),
                (0.91f, 0.357f),
                (1.00f, 0.357f));
            float[][] westLoopPoints = SmoothPoints(
                (0.31f, 0.350f),
                (0.30f, 0.265f),
                (0.27f, 0.190f),
                (0.20f, 0.165f),
                (0.14f, 0.230f),
                (0.12f, 0.310f),
                (0.17f, 0.375f),
                (0.23f, 0.395f));
            float[][] eastLoopPoints = SmoothPoints(
                (0.73f, 0.380f),
                (0.74f, 0.285f),
                (0.77f, 0.175f),
                (0.83f, 0.135f),
                (0.89f, 0.185f),
                (0.91f, 0.270f),
                (0.87f, 0.345f),
                (0.82f, 0.395f));
            float[][] southArcPoints = SmoothPoints(
                (0.43f, 0.405f),
                (0.41f, 0.545f),
                (0.45f, 0.665f),
                (0.57f, 0.710f),
                (0.68f, 0.700f),
                (0.76f, 0.610f),
                (0.82f, 0.395f));

            CityVehicleRoad[] mainRoads =
            {
                new CityVehicleRoad
                {
                    id = "vehicle-main-street",
                    role = CityVehicleRoadRole.Main,
                    route_option = CityRoadRouteOption.Existing,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.ExistingMap,
                    points_norm = mainRoadPoints,
                    width_units = 1.1f,
                    speed_units_per_second = 7f,
                    traffic_load = 0.46f,
                    connected_building_ids = Array.Empty<string>(),
                    connected_road_ids = new[]
                    {
                        "vehicle-local-west-loop",
                        "vehicle-local-east-loop",
                        "vehicle-local-south-arc",
                    },
                },
                ExistingLocalRoad(
                    "vehicle-local-west-loop",
                    westLoopPoints,
                    "vehicle-main-street",
                    "detached-garden"),
                ExistingLocalRoad(
                    "vehicle-local-east-loop",
                    eastLoopPoints,
                    "vehicle-main-street",
                    "detached-east"),
                ExistingLocalRoad(
                    "vehicle-local-south-arc",
                    southArcPoints,
                    "vehicle-main-street"),
            };

            CityPedestrianLink[] mainLinks =
            {
                ExistingSidewalk(
                    "pedestrian-park-spine",
                    mainRoadPoints,
                    bounds,
                    -1.05f,
                    new[]
                    {
                        "pedestrian-local-west-loop",
                        "pedestrian-local-east-loop",
                        "pedestrian-local-south-arc",
                    }),
                ExistingSidewalk(
                    "pedestrian-local-west-loop",
                    westLoopPoints,
                    bounds,
                    -0.58f,
                    new[] { "pedestrian-park-spine" },
                    "detached-garden"),
                ExistingSidewalk(
                    "pedestrian-local-east-loop",
                    eastLoopPoints,
                    bounds,
                    0.58f,
                    new[] { "pedestrian-park-spine" },
                    "detached-east"),
                ExistingSidewalk(
                    "pedestrian-local-south-arc",
                    southArcPoints,
                    bounds,
                    0.58f,
                    new[] { "pedestrian-park-spine" }),
            };

            List<CityVehicleRoad> roads = new List<CityVehicleRoad>(mainRoads);
            List<CityPedestrianLink> links = new List<CityPedestrianLink>(mainLinks);
            foreach (CityBuilding building in buildings)
            {
                string roadNetwork = building.id == "detached-garden"
                    ? "vehicle-local-west-loop"
                    : "vehicle-local-east-loop";
                string linkNetwork = building.id == "detached-garden"
                    ? "pedestrian-local-west-loop"
                    : "pedestrian-local-east-loop";
                CityVehicleRoad targetRoad = roads.Single(road => road.id == roadNetwork);
                CityPedestrianLink targetSidewalk = links.Single(link => link.id == linkNetwork);
                string roadId = $"road-access-{building.id}";
                string linkId = $"walk-access-{building.id}";
                float[] roadJoin = ClosestPointOnPolyline(
                    building.position_norm,
                    targetRoad.points_norm,
                    bounds);
                float[] buildingEdge = BuildingEdgeToward(building, roadJoin, bounds);
                float[][] accessRoadPoints = SmoothPoints(
                    (buildingEdge[0], buildingEdge[1]),
                    ((buildingEdge[0] + roadJoin[0]) * 0.5f,
                     (buildingEdge[1] + roadJoin[1]) * 0.5f),
                    (roadJoin[0], roadJoin[1]));
                roads.Add(new CityVehicleRoad
                {
                    id = roadId,
                    role = CityVehicleRoadRole.BuildingAccess,
                    route_option = CityRoadRouteOption.ExistingNetwork,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.AutoGenerated,
                    points_norm = accessRoadPoints,
                    width_units = 1.05f,
                    speed_units_per_second = 4.5f,
                    traffic_load = building.vehicle_demand * 0.5f,
                    connected_building_ids = new[] { building.id },
                    connected_road_ids = new[] { roadNetwork },
                });
                links.Add(new CityPedestrianLink
                {
                    id = linkId,
                    type = CityPedestrianLinkType.BasicBuildingAccess,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.AutoGenerated,
                    points_norm = OpeningAccessSidewalk(
                        accessRoadPoints,
                        targetSidewalk.points_norm,
                        bounds,
                        building.id),
                    width_units = 0.70f,
                    step_free_accessible = true,
                    connected_building_ids = new[] { building.id },
                    connected_link_ids = new[] { linkNetwork },
                });
                building.vehicle_road_ids = new[] { roadId };
                building.pedestrian_link_ids = new[] { linkId };
            }

            CityGreenPatch[] patches = CreateGreenPatches(planningGrid, bounds);

            CityAmenity[] amenities = Array.Empty<CityAmenity>();

            CityWasteNode[] wasteNodes = buildings.Select(building => new CityWasteNode
            {
                id = $"waste-{building.id}",
                type = CityWasteNodeType.BuildingOutput,
                source_building_id = building.id,
                position_norm = (float[])building.position_norm.Clone(),
                generation_rate = building.waste_output,
                capacity = 0f,
                fill_ratio = 0f,
            }).ToArray();

            CityState city = new CityState
            {
                city_id = "city-prototype-v01",
                revision = 5,
                source_contract = "city-prototype-demo-0.1",
                bounds = bounds,
                planning_grid = planningGrid,
                buildings = buildings,
                green_patches = patches,
                vehicle_roads = roads.ToArray(),
                pedestrian_links = links.ToArray(),
                amenities = amenities,
                waste = new CityWasteSystem
                {
                    total_demand = buildings.Sum(building => building.waste_output),
                    total_capacity = 0f,
                    overflow_elapsed_seconds = 0f,
                    overflow_active = false,
                    nodes = wasteNodes,
                    litter_hotspots = Array.Empty<CityLitterHotspot>(),
                },
            };
            CityStateValidationResult validation = CityStateValidator.Validate(city);
            if (!validation.IsValid)
            {
                throw new InvalidOperationException($"Prototype city is invalid: {validation.Summary}");
            }
            return city;
        }

        private static CityBuilding Building(
            string id,
            CityBuildingType type,
            int tokenId,
            float x,
            float y,
            int housing,
            float origin,
            float destination,
            int capacity,
            float vehicleDemand)
        {
            bool commercial = type == CityBuildingType.Commercial;
            bool community = type == CityBuildingType.CommunityFacility;
            return new CityBuilding
            {
                id = id,
                source_token_id = tokenId,
                type = type,
                construction_state = CityConstructionState.Existing,
                position_norm = new[] { x, y },
                rotation_deg = 0f,
                footprint_units = CityConstructionFactory.ReferenceFootprintUnits(type),
                housing_capacity = housing,
                human_origin_rate = origin,
                human_destination_weight = destination,
                comfortable_capacity = capacity,
                vehicle_demand = vehicleDemand,
                waste_output = commercial ? 0.9f : community ? 0.55f : housing > 20 ? 0.75f : 0.3f,
                anthropogenic_food_output = commercial ? 0.85f : community ? 0.3f : 0.2f,
                disturbance_output = commercial ? 0.85f : community ? 0.65f : housing > 20 ? 0.8f : 0.35f,
                requires_vehicle_access = true,
                requires_pedestrian_access = true,
            };
        }

        private static CityPlanningGrid CreatePlanningGrid(
            IEnumerable<CityBuilding> buildings,
            CityBounds bounds)
        {
            CityPlanningGrid grid = new CityPlanningGrid
            {
                cols = CityPlanningGrid.DefaultColumns,
                rows = CityPlanningGrid.DefaultRows,
                // Desktop play can grow into the dense late-stage reference city.
                // Camera play remains naturally bounded by the physical Token inventory.
                max_active_player_buildings = 72,
                cells = Enumerable.Range(0, CityPlanningGrid.DefaultRows)
                    .SelectMany(row => Enumerable.Range(0, CityPlanningGrid.DefaultColumns)
                        .Select(col => CreateGridCell(row, col)))
                    .ToArray(),
            };

            foreach (CityBuilding building in buildings ?? Array.Empty<CityBuilding>())
            {
                if (!CityGridResolver.TryOccupyWithBuilding(
                        grid,
                        bounds,
                        building,
                        0,
                        out string error))
                {
                    throw new InvalidOperationException(
                        $"Could not place baseline building {building.id} on the planning grid: {error}");
                }
            }
            return grid;
        }

        private static CityGridCell CreateGridCell(int row, int col)
        {
            CityLandCover cover = BaselineCover(row, col);
            bool fixedFeature = cover == CityLandCover.PublicGreen ||
                                cover == CityLandCover.Water ||
                                cover == CityLandCover.CivicPlaza;
            string habitatId = cover == CityLandCover.Woodland ||
                               cover == CityLandCover.PublicGreen
                ? HabitatPatchId(row, col, cover)
                : null;
            return new CityGridCell
            {
                id = CellId(row, col),
                row = row,
                col = col,
                center_norm = new[]
                {
                    (col + 0.5f) / CityPlanningGrid.DefaultColumns,
                    (row + 0.5f) / CityPlanningGrid.DefaultRows,
                },
                size_norm = new[]
                {
                    1f / CityPlanningGrid.DefaultColumns,
                    1f / CityPlanningGrid.DefaultRows,
                },
                baseline_cover = cover,
                current_cover = cover,
                buildable = !fixedFeature,
                fixed_feature = fixedFeature,
                occupant_id = null,
                habitat_patch_id = habitatId,
                was_woodland = cover == CityLandCover.Woodland,
                last_changed_revision = 0,
            };
        }

        private static CityLandCover BaselineCover(int row, int col)
        {
            // Preserve the approved organic woodland/open-land zoning while subdividing each
            // former 10 x 10 planning area into four 5 x 5 ecological/occupancy cells.
            int zoneRow = row / 2;
            int zoneCol = col / 2;
            bool woodland = zoneRow == 0 || zoneRow == 5 ||
                            (zoneRow == 1 && (zoneCol <= 2 || zoneCol == 4 || zoneCol >= 6)) ||
                            (zoneRow == 2 && (zoneCol <= 1 || zoneCol == 4 || zoneCol >= 7)) ||
                            (zoneRow == 3 && (zoneCol <= 1 || zoneCol >= 7)) ||
                            (zoneRow == 4 && (zoneCol <= 3 || zoneCol >= 7));
            if (woodland)
            {
                return CityLandCover.Woodland;
            }
            return CityLandCover.OpenLand;
        }

        private static CityGreenPatch[] CreateGreenPatches(
            CityPlanningGrid grid,
            CityBounds bounds)
        {
            CityGridCell[] habitatCells = grid.cells
                .Where(cell => cell != null &&
                               !string.IsNullOrWhiteSpace(cell.habitat_patch_id))
                .OrderBy(cell => cell.row)
                .ThenBy(cell => cell.col)
                .ToArray();
            HashSet<string> habitatCellIds = new HashSet<string>(
                habitatCells.Select(cell => cell.id));
            return habitatCells.Select(cell =>
            {
                bool woodland = cell.current_cover == CityLandCover.Woodland;
                string[] connected = CityGridResolver.GetFourNeighbours(grid, cell.id)
                    .Where(neighbour => habitatCellIds.Contains(neighbour.id))
                    .Select(neighbour => neighbour.habitat_patch_id)
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToArray();
                return new CityGreenPatch
                {
                    id = cell.habitat_patch_id,
                    source_token_id = -1,
                    planning_cell_id = cell.id,
                    type = woodland ? CityGreenPatchType.Woodland : CityGreenPatchType.OpenGrass,
                    construction_state = CityConstructionState.Existing,
                    public_park = !woodland,
                    polygon_norm = CityGridResolver.PolygonFor(cell),
                    shelter_value = woodland ? 0.88f : 0.30f,
                    natural_food_value = woodland ? 0.80f : 0.28f,
                    human_disturbance = woodland ? 0.12f : 0.46f,
                    patch_size_units = cell.size_norm[0] * bounds.width_units *
                                       cell.size_norm[1] * bounds.height_units,
                    connected_patch_ids = connected,
                };
            }).ToArray();
        }

        private static string HabitatPatchId(int row, int col, CityLandCover cover)
        {
            string prefix = cover == CityLandCover.Woodland ? "woodland" : "public-green";
            return $"{prefix}-cell-{CellId(row, col).ToLowerInvariant()}";
        }

        private static string CellId(int row, int col)
        {
            return $"{(char)('A' + row)}{col + 1}";
        }

        private static CityAmenity Amenity(
            string id,
            CityAmenityType type,
            float x,
            float y,
            float radius,
            float activity,
            float capacity,
            float exposureReduction)
        {
            return new CityAmenity
            {
                id = id,
                type = type,
                construction_state = CityConstructionState.Existing,
                position_norm = new[] { x, y },
                service_radius_units = radius,
                human_activity_weight = activity,
                waste_capacity = capacity,
                food_exposure_reduction = exposureReduction,
            };
        }

        private static CityVehicleRoad ExistingLocalRoad(
            string id,
            float[][] points,
            string mainRoadId,
            params string[] buildingIds)
        {
            return new CityVehicleRoad
            {
                id = id,
                role = CityVehicleRoadRole.Local,
                route_option = CityRoadRouteOption.Existing,
                construction_state = CityConstructionState.Existing,
                source = CityNetworkSource.ExistingMap,
                points_norm = points,
                width_units = 0.90f,
                speed_units_per_second = 5f,
                traffic_load = 0.24f,
                connected_building_ids = buildingIds ?? Array.Empty<string>(),
                connected_road_ids = new[] { mainRoadId },
            };
        }

        private static CityPedestrianLink ExistingSidewalk(
            string id,
            float[][] roadPoints,
            CityBounds bounds,
            float offsetUnits,
            string[] connectedLinkIds,
            params string[] buildingIds)
        {
            return new CityPedestrianLink
            {
                id = id,
                type = CityPedestrianLinkType.ExistingNetwork,
                construction_state = CityConstructionState.Existing,
                source = CityNetworkSource.ExistingMap,
                points_norm = CityRoadCandidateGenerator.OffsetPolyline(
                    roadPoints,
                    bounds,
                    offsetUnits),
                width_units = 0.55f,
                step_free_accessible = true,
                connected_building_ids = buildingIds ?? Array.Empty<string>(),
                connected_link_ids = connectedLinkIds ?? Array.Empty<string>(),
            };
        }

        private static float[] BuildingEdgeToward(
            CityBuilding building,
            float[] target,
            CityBounds bounds)
        {
            float centreX = building.position_norm[0] * bounds.width_units;
            float centreY = building.position_norm[1] * bounds.height_units;
            float deltaX = target[0] * bounds.width_units - centreX;
            float deltaY = target[1] * bounds.height_units - centreY;
            float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (length <= 0.0001f)
            {
                return (float[])building.position_norm.Clone();
            }
            float directionX = deltaX / length;
            float directionY = deltaY / length;
            float halfWidth = building.footprint_units[0] * 0.5f;
            float halfDepth = building.footprint_units[1] * 0.5f;
            float distanceToWidth = Math.Abs(directionX) <= 0.0001f
                ? float.MaxValue
                : halfWidth / Math.Abs(directionX);
            float distanceToDepth = Math.Abs(directionY) <= 0.0001f
                ? float.MaxValue
                : halfDepth / Math.Abs(directionY);
            float edgeDistance = Math.Min(distanceToWidth, distanceToDepth);
            return new[]
            {
                (centreX + directionX * edgeDistance) / bounds.width_units,
                (centreY + directionY * edgeDistance) / bounds.height_units,
            };
        }

        private static float[] ClosestPointOnPolyline(
            float[] point,
            float[][] polyline,
            CityBounds bounds)
        {
            float[] best = (float[])polyline[0].Clone();
            float bestDistance = float.PositiveInfinity;
            for (int index = 1; index < polyline.Length; index += 1)
            {
                float[] start = polyline[index - 1];
                float[] end = polyline[index];
                float segmentX = (end[0] - start[0]) * bounds.width_units;
                float segmentY = (end[1] - start[1]) * bounds.height_units;
                float pointX = (point[0] - start[0]) * bounds.width_units;
                float pointY = (point[1] - start[1]) * bounds.height_units;
                float denominator = segmentX * segmentX + segmentY * segmentY;
                float t = denominator <= 0.000001f
                    ? 0f
                    : Math.Max(0f, Math.Min(1f,
                        (pointX * segmentX + pointY * segmentY) / denominator));
                float[] candidate =
                {
                    start[0] + (end[0] - start[0]) * t,
                    start[1] + (end[1] - start[1]) * t,
                };
                float deltaX = (point[0] - candidate[0]) * bounds.width_units;
                float deltaY = (point[1] - candidate[1]) * bounds.height_units;
                float distance = deltaX * deltaX + deltaY * deltaY;
                if (distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static float[][] Points(params (float x, float y)[] points)
        {
            return points.Select(point => new[] { point.x, point.y }).ToArray();
        }

        private static float[][] SmoothPoints(params (float x, float y)[] controlPoints)
        {
            const int SamplesPerSegment = 4;
            List<float[]> points = new List<float[]>();
            for (int index = 0; index < controlPoints.Length - 1; index += 1)
            {
                (float x, float y) p0 = controlPoints[Math.Max(0, index - 1)];
                (float x, float y) p1 = controlPoints[index];
                (float x, float y) p2 = controlPoints[index + 1];
                (float x, float y) p3 = controlPoints[Math.Min(controlPoints.Length - 1, index + 2)];
                for (int sample = 0; sample < SamplesPerSegment; sample += 1)
                {
                    float t = sample / (float)SamplesPerSegment;
                    float t2 = t * t;
                    float t3 = t2 * t;
                    points.Add(new[]
                    {
                        0.5f * ((2f * p1.x) + (-p0.x + p2.x) * t +
                                (2f * p0.x - 5f * p1.x + 4f * p2.x - p3.x) * t2 +
                                (-p0.x + 3f * p1.x - 3f * p2.x + p3.x) * t3),
                        0.5f * ((2f * p1.y) + (-p0.y + p2.y) * t +
                                (2f * p0.y - 5f * p1.y + 4f * p2.y - p3.y) * t2 +
                                (-p0.y + 3f * p1.y - 3f * p2.y + p3.y) * t3),
                    });
                }
            }
            (float x, float y) last = controlPoints[controlPoints.Length - 1];
            points.Add(new[] { last.x, last.y });
            return points.ToArray();
        }

        private static float[][] OpeningAccessSidewalk(
            float[][] accessRoadPoints,
            float[][] networkSidewalkPoints,
            CityBounds bounds,
            string buildingId)
        {
            float side = string.CompareOrdinal(buildingId, "detached-east") <= 0
                ? -0.98f
                : 0.98f;
            float[][] points = CityRoadCandidateGenerator.OffsetPolyline(
                accessRoadPoints,
                bounds,
                side);
            float[] last = points[points.Length - 1];
            float[] join = ClosestPointOnPolyline(last, networkSidewalkPoints, bounds);
            return points.Concat(new[] { join }).ToArray();
        }
    }
}

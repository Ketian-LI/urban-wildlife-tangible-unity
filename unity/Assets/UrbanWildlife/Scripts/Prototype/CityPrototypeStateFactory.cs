using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;

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
                Building("apartment-west", CityBuildingType.Apartment, 100, 0.083333f, 0.125f, 60, 0.85f, 0.25f, 8, 0.80f),
                Building("apartment-court", CityBuildingType.Apartment, 101, 0.416667f, 0.125f, 60, 0.85f, 0.25f, 8, 0.80f),
                Building("detached-garden", CityBuildingType.DetachedHouse, 110, 0.583333f, 0.875f, 12, 0.35f, 0.15f, 4, 0.45f),
                Building("market-hall", CityBuildingType.Commercial, 120, 0.75f, 0.125f, 0, 0.05f, 0.90f, 16, 0.70f),
                Building("corner-shops", CityBuildingType.Commercial, 121, 0.916667f, 0.875f, 0, 0.05f, 0.82f, 12, 0.65f),
                Building("community-centre", CityBuildingType.CommunityFacility, 130, 0.916667f, 0.375f, 0, 0.05f, 0.72f, 24, 0.50f),
            };
            CityPlanningGrid planningGrid = CreatePlanningGrid(buildings, bounds);

            CityVehicleRoad[] mainRoads =
            {
                new CityVehicleRoad
                {
                    id = "vehicle-main-street",
                    role = CityVehicleRoadRole.Main,
                    route_option = CityRoadRouteOption.Existing,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.ExistingMap,
                    points_norm = Points((0.04f, 0.53f), (0.16f, 0.51f), (0.29f, 0.53f), (0.43f, 0.50f), (0.57f, 0.53f), (0.73f, 0.50f), (0.87f, 0.52f), (0.96f, 0.54f)),
                    width_units = 4.2f,
                    speed_units_per_second = 7f,
                    traffic_load = 0.46f,
                    connected_building_ids = buildings.Select(building => building.id).ToArray(),
                    connected_road_ids = new[] { "vehicle-neighbourhood-lane" },
                },
                new CityVehicleRoad
                {
                    id = "vehicle-neighbourhood-lane",
                    role = CityVehicleRoadRole.Local,
                    route_option = CityRoadRouteOption.Existing,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.ExistingMap,
                    points_norm = Points((0.50f, 0.52f), (0.55f, 0.62f), (0.68f, 0.79f), (0.94f, 0.79f)),
                    width_units = 3.2f,
                    speed_units_per_second = 5f,
                    traffic_load = 0.24f,
                    connected_building_ids = new[] { "detached-garden", "corner-shops" },
                    connected_road_ids = new[] { "vehicle-main-street" },
                },
            };

            CityPedestrianLink[] mainLinks =
            {
                new CityPedestrianLink
                {
                    id = "pedestrian-park-spine",
                    type = CityPedestrianLinkType.ExistingNetwork,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.ExistingMap,
                    points_norm = Points((0.04f, 0.46f), (0.18f, 0.47f), (0.33f, 0.45f), (0.48f, 0.47f), (0.63f, 0.45f), (0.79f, 0.47f), (0.96f, 0.46f)),
                    width_units = 1.8f,
                    step_free_accessible = true,
                    connected_building_ids = buildings.Select(building => building.id).ToArray(),
                    connected_link_ids = new[] { "pedestrian-garden-branch" },
                },
                new CityPedestrianLink
                {
                    id = "pedestrian-garden-branch",
                    type = CityPedestrianLinkType.ExistingNetwork,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.ExistingMap,
                    points_norm = Points((0.48f, 0.47f), (0.52f, 0.67f), (0.63f, 0.82f), (0.91f, 0.84f)),
                    width_units = 1.5f,
                    step_free_accessible = true,
                    connected_building_ids = new[] { "detached-garden", "corner-shops" },
                    connected_link_ids = new[] { "pedestrian-park-spine" },
                },
            };

            List<CityVehicleRoad> roads = new List<CityVehicleRoad>(mainRoads);
            List<CityPedestrianLink> links = new List<CityPedestrianLink>(mainLinks);
            foreach (CityBuilding building in buildings)
            {
                string roadNetwork = building.position_norm[1] > 0.62f
                    ? "vehicle-neighbourhood-lane"
                    : "vehicle-main-street";
                string linkNetwork = building.position_norm[1] > 0.62f
                    ? "pedestrian-garden-branch"
                    : "pedestrian-park-spine";
                string roadId = $"road-access-{building.id}";
                string linkId = $"walk-access-{building.id}";
                float roadY = roadNetwork == "vehicle-main-street" ? 0.53f : 0.79f;
                float linkY = linkNetwork == "pedestrian-park-spine" ? 0.46f : 0.84f;
                roads.Add(new CityVehicleRoad
                {
                    id = roadId,
                    role = CityVehicleRoadRole.BuildingAccess,
                    route_option = CityRoadRouteOption.ExistingNetwork,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.AutoGenerated,
                    points_norm = Points(
                        (building.position_norm[0], building.position_norm[1]),
                        (building.position_norm[0], roadY)),
                    width_units = 2.6f,
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
                    points_norm = Points(
                        (building.position_norm[0], building.position_norm[1]),
                        (building.position_norm[0], linkY)),
                    width_units = 1.2f,
                    step_free_accessible = true,
                    connected_building_ids = new[] { building.id },
                    connected_link_ids = new[] { linkNetwork },
                });
                building.vehicle_road_ids = new[] { roadId };
                building.pedestrian_link_ids = new[] { linkId };
            }

            CityGreenPatch[] patches = CreateGreenPatches(planningGrid, bounds);

            CityAmenity[] amenities =
            {
                Amenity("bench-park-01", CityAmenityType.Bench, 0.583333f, 0.375f, 10f, 0.55f, 0f, 0f),
                Amenity("bin-main-01", CityAmenityType.Bin, 0.31f, 0.50f, 15f, 0.08f, 2.35f, 0.68f),
                Amenity("bin-east-01", CityAmenityType.Bin, 0.76f, 0.52f, 15f, 0.08f, 2.25f, 0.68f),
            };

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
                revision = 4,
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
                footprint_units = type == CityBuildingType.DetachedHouse
                    ? new[] { 10f, 9f }
                    : new[] { 9f, community ? 8f : 7f },
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
                cols = 6,
                rows = 4,
                max_active_player_buildings = 9,
                cells = Enumerable.Range(0, 4)
                    .SelectMany(row => Enumerable.Range(0, 6)
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
                center_norm = new[] { (col + 0.5f) / 6f, (row + 0.5f) / 4f },
                size_norm = new[] { 1f / 6f, 1f / 4f },
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
            // Two uneven edge groves frame a readable central planning meadow.
            // The logical grid remains regular for camera snapping; only the landscape
            // composition is deliberately asymmetrical.
            bool westWoodland = (row == 0 && col == 1) ||
                                (row == 2 && col == 0) ||
                                (row == 3 && col <= 1);
            bool eastWoodland = (row == 0 && col == 5) ||
                                (row == 2 && col >= 4);
            if (westWoodland || eastWoodland)
            {
                return CityLandCover.Woodland;
            }
            if (row == 1 && col == 3)
            {
                return CityLandCover.PublicGreen;
            }
            if (row == 1 && col == 1)
            {
                return CityLandCover.CivicPlaza;
            }
            // The pond sits against the western habitat edge rather than on the
            // outer board border, leaving a calmer buildable foreground.
            if (row == 2 && col == 1)
            {
                return CityLandCover.Water;
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

        private static float[][] Points(params (float x, float y)[] points)
        {
            return points.Select(point => new[] { point.x, point.y }).ToArray();
        }
    }
}

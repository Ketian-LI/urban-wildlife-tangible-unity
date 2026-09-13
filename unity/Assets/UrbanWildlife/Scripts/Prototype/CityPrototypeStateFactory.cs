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
            CityBuilding[] buildings =
            {
                Building("apartment-west", CityBuildingType.Apartment, 100, 0.16f, 0.18f, 60, 0.85f, 0.25f, 8, 0.80f),
                Building("apartment-court", CityBuildingType.Apartment, 101, 0.34f, 0.18f, 60, 0.85f, 0.25f, 8, 0.80f),
                Building("detached-garden", CityBuildingType.DetachedHouse, 110, 0.57f, 0.80f, 12, 0.35f, 0.15f, 4, 0.45f),
                Building("market-hall", CityBuildingType.Commercial, 120, 0.72f, 0.18f, 0, 0.05f, 0.90f, 16, 0.70f),
                Building("corner-shops", CityBuildingType.Commercial, 121, 0.84f, 0.78f, 0, 0.05f, 0.82f, 12, 0.65f),
                Building("community-centre", CityBuildingType.CommunityFacility, 130, 0.87f, 0.48f, 0, 0.05f, 0.72f, 24, 0.50f),
            };

            CityVehicleRoad[] mainRoads =
            {
                new CityVehicleRoad
                {
                    id = "vehicle-main-street",
                    role = CityVehicleRoadRole.Main,
                    route_option = CityRoadRouteOption.Existing,
                    construction_state = CityConstructionState.Existing,
                    source = CityNetworkSource.ExistingMap,
                    points_norm = Points((0.04f, 0.42f), (0.28f, 0.40f), (0.50f, 0.42f), (0.73f, 0.39f), (0.96f, 0.43f)),
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
                    points_norm = Points((0.50f, 0.42f), (0.58f, 0.61f), (0.71f, 0.72f), (0.94f, 0.72f)),
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
                    points_norm = Points((0.04f, 0.53f), (0.25f, 0.55f), (0.48f, 0.51f), (0.70f, 0.54f), (0.96f, 0.51f)),
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
                    points_norm = Points((0.48f, 0.51f), (0.51f, 0.67f), (0.62f, 0.76f), (0.91f, 0.79f)),
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
                float roadY = roadNetwork == "vehicle-main-street" ? 0.42f : 0.72f;
                float linkY = linkNetwork == "pedestrian-park-spine" ? 0.53f : 0.78f;
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

            CityGreenPatch[] patches =
            {
                Patch("public-park", CityGreenPatchType.OpenGrass, true, 0.12f, 0.48f, 0.46f, 0.86f, 0.25f, 0.22f, 0.48f),
                Patch("woodland-west", CityGreenPatchType.Woodland, false, 0.06f, 0.57f, 0.24f, 0.92f, 0.88f, 0.82f, 0.12f),
                Patch("woodland-east", CityGreenPatchType.Woodland, false, 0.72f, 0.55f, 0.94f, 0.94f, 0.90f, 0.78f, 0.14f),
                Patch("rain-garden", CityGreenPatchType.ShrubGarden, false, 0.34f, 0.60f, 0.49f, 0.88f, 0.62f, 0.58f, 0.18f),
            };

            CityAmenity[] amenities =
            {
                Amenity("bench-park-01", CityAmenityType.Bench, 0.30f, 0.67f, 10f, 0.55f, 0f, 0f),
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
                bounds = new CityBounds { origin = "top_left", width_units = 90f, height_units = 60f },
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

        private static CityGreenPatch Patch(
            string id,
            CityGreenPatchType type,
            bool publicPark,
            float minX,
            float minY,
            float maxX,
            float maxY,
            float shelter,
            float food,
            float disturbance)
        {
            return new CityGreenPatch
            {
                id = id,
                source_token_id = -1,
                type = type,
                construction_state = CityConstructionState.Existing,
                public_park = publicPark,
                polygon_norm = Points((minX, minY), (maxX, minY), (maxX, maxY), (minX, maxY)),
                shelter_value = shelter,
                natural_food_value = food,
                human_disturbance = disturbance,
                patch_size_units = (maxX - minX) * 90f * (maxY - minY) * 60f,
                connected_patch_ids = Array.Empty<string>(),
            };
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

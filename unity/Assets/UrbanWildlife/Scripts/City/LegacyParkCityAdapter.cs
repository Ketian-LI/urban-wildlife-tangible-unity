using System;
using System.Linq;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.City
{
    public static class LegacyParkCityAdapter
    {
        public const string SourceContract = "legacy-p0-park-layout-0.1";

        public static CityState Create(P0Scenario scenario, LayoutPacket packet)
        {
            if (scenario?.board == null || scenario.baseline_layout == null ||
                packet?.tokens == null || packet.path?.points_norm == null)
            {
                throw new ArgumentException("A complete legacy scenario and confirmed layout are required.");
            }

            LayoutToken[] activityTokens = packet.tokens
                .Where(token => token.type == "food_hotspot")
                .OrderBy(token => token.id)
                .ToArray();
            LayoutToken[] woodlandTokens = packet.tokens
                .Where(token => token.type == "woodland")
                .OrderBy(token => token.id)
                .ToArray();
            CityBuilding[] buildings = activityTokens
                .Select((token, index) => CreateActivityBuilding(token, index))
                .ToArray();
            string[] buildingIds = buildings.Select(building => building.id).ToArray();

            CityGreenPatch[] woodlandPatches = woodlandTokens
                .Select(token => CreateWoodlandPatch(token, scenario.board))
                .ToArray();
            CityGreenPatch publicPark = CreateLegacyPublicPark();
            CityGreenPatch[] patches = woodlandPatches.Concat(new[] { publicPark }).ToArray();

            float[][] route = ClonePoints(packet.path.points_norm);
            CityVehicleRoad road = new CityVehicleRoad
            {
                id = "legacy-main-road",
                role = CityVehicleRoadRole.Main,
                route_option = CityRoadRouteOption.Existing,
                construction_state = CityConstructionState.Existing,
                source = CityNetworkSource.LegacyAdapter,
                points_norm = ClonePoints(route),
                width_units = 3.5f,
                speed_units_per_second = 7f,
                traffic_load = 0f,
                connected_building_ids = buildingIds,
            };
            CityPedestrianLink pedestrian = new CityPedestrianLink
            {
                id = "legacy-pedestrian-route",
                type = CityPedestrianLinkType.ExistingNetwork,
                construction_state = CityConstructionState.Existing,
                source = CityNetworkSource.LegacyAdapter,
                points_norm = ClonePoints(route),
                width_units = 1.5f,
                step_free_accessible = true,
                connected_building_ids = buildingIds,
            };

            CityWasteNode[] wasteNodes = buildings.Select(building => new CityWasteNode
            {
                id = $"waste-source-{building.source_token_id}",
                type = CityWasteNodeType.BuildingOutput,
                source_building_id = building.id,
                position_norm = (float[])building.position_norm.Clone(),
                generation_rate = building.waste_output,
                capacity = 0f,
                fill_ratio = 0f,
            }).ToArray();
            float totalDemand = buildings.Sum(building => building.waste_output);

            return new CityState
            {
                city_id = $"legacy-{scenario.scenario_id.ToLowerInvariant()}",
                revision = 0,
                source_contract = SourceContract,
                bounds = new CityBounds
                {
                    origin = scenario.board.origin,
                    width_units = scenario.board.width_cm,
                    height_units = scenario.board.height_cm,
                },
                buildings = buildings,
                green_patches = patches,
                vehicle_roads = new[] { road },
                pedestrian_links = new[] { pedestrian },
                amenities = Array.Empty<CityAmenity>(),
                waste = new CityWasteSystem
                {
                    total_demand = totalDemand,
                    total_capacity = 0f,
                    overflow_elapsed_seconds = 0f,
                    overflow_active = false,
                    nodes = wasteNodes,
                    litter_hotspots = Array.Empty<CityLitterHotspot>(),
                },
            };
        }

        private static CityBuilding CreateActivityBuilding(LayoutToken token, int index)
        {
            bool commercial = index == 1;
            return new CityBuilding
            {
                id = $"legacy-activity-{token.id}",
                source_token_id = token.id,
                type = commercial ? CityBuildingType.Commercial : CityBuildingType.CommunityFacility,
                construction_state = CityConstructionState.Existing,
                position_norm = new[] { token.x_norm, token.y_norm },
                rotation_deg = token.angle_deg,
                footprint_units = commercial ? new[] { 7f, 6f } : new[] { 6f, 6f },
                housing_capacity = 0,
                human_origin_rate = 0f,
                human_destination_weight = commercial ? 0.9f : 0.6f,
                comfortable_capacity = commercial ? 10 : 8,
                vehicle_demand = commercial ? 0.65f : 0.3f,
                waste_output = commercial ? 0.85f : 0.35f,
                anthropogenic_food_output = commercial ? 0.9f : 0.4f,
                disturbance_output = commercial ? 0.7f : 0.45f,
                requires_vehicle_access = true,
                requires_pedestrian_access = true,
                vehicle_road_ids = new[] { "legacy-main-road" },
                pedestrian_link_ids = new[] { "legacy-pedestrian-route" },
            };
        }

        private static CityGreenPatch CreateWoodlandPatch(LayoutToken token, ScenarioBoard board)
        {
            float xRadius = Math.Min(0.065f, 6f / board.width_cm);
            float yRadius = Math.Min(0.085f, 6f / board.height_cm);
            return new CityGreenPatch
            {
                id = $"legacy-woodland-{token.id}",
                source_token_id = token.id,
                type = CityGreenPatchType.Woodland,
                construction_state = CityConstructionState.Existing,
                public_park = false,
                polygon_norm = Diamond(token.x_norm, token.y_norm, xRadius, yRadius),
                shelter_value = 0.85f,
                natural_food_value = 0.8f,
                human_disturbance = 0.15f,
                patch_size_units = 72f,
                connected_patch_ids = Array.Empty<string>(),
            };
        }

        private static CityGreenPatch CreateLegacyPublicPark()
        {
            return new CityGreenPatch
            {
                id = "legacy-public-open-grass",
                source_token_id = -1,
                type = CityGreenPatchType.OpenGrass,
                construction_state = CityConstructionState.Existing,
                public_park = true,
                polygon_norm = new[]
                {
                    new[] { 0.08f, 0.1f },
                    new[] { 0.92f, 0.1f },
                    new[] { 0.92f, 0.9f },
                    new[] { 0.08f, 0.9f },
                },
                shelter_value = 0.2f,
                natural_food_value = 0.25f,
                human_disturbance = 0.55f,
                patch_size_units = 2800f,
                connected_patch_ids = Array.Empty<string>(),
            };
        }

        private static float[][] Diamond(float x, float y, float xRadius, float yRadius)
        {
            return new[]
            {
                new[] { Clamp01(x), Clamp01(y - yRadius) },
                new[] { Clamp01(x + xRadius), Clamp01(y) },
                new[] { Clamp01(x), Clamp01(y + yRadius) },
                new[] { Clamp01(x - xRadius), Clamp01(y) },
            };
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }

        private static float[][] ClonePoints(float[][] points)
        {
            return points.Select(point => (float[])point.Clone()).ToArray();
        }
    }
}

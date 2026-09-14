using System;
using UrbanWildlife.City;
using UrbanWildlife.Input;

namespace UrbanWildlife.Construction
{
    public static class CityConstructionFactory
    {
        public static string ObjectId(CityTokenState token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }
            return token.type == CityPhysicalTokenType.GreenIntervention
                ? $"green-intervention-token-{token.id}"
                : $"building-token-{token.id}";
        }

        public static CityBuilding CreateBuilding(
            CityTokenState token,
            CityConstructionState constructionState)
        {
            if (token == null || token.type == CityPhysicalTokenType.GreenIntervention)
            {
                throw new ArgumentException("A building Token is required.");
            }

            CityBuilding building = BaseBuilding(token, constructionState);
            switch (token.type)
            {
                case CityPhysicalTokenType.Apartment:
                    building.type = CityBuildingType.Apartment;
                    building.footprint_units = new[] { 20f, 20f };
                    building.housing_capacity = 60;
                    building.human_origin_rate = 0.85f;
                    building.human_destination_weight = 0.25f;
                    building.comfortable_capacity = 8;
                    building.vehicle_demand = 0.8f;
                    building.waste_output = 0.75f;
                    building.anthropogenic_food_output = 0.35f;
                    building.disturbance_output = 0.8f;
                    break;
                case CityPhysicalTokenType.DetachedHouse:
                    building.type = CityBuildingType.DetachedHouse;
                    building.footprint_units = new[] { 10f, 10f };
                    building.housing_capacity = 12;
                    building.human_origin_rate = 0.35f;
                    building.human_destination_weight = 0.15f;
                    building.comfortable_capacity = 4;
                    building.vehicle_demand = 0.45f;
                    building.waste_output = 0.3f;
                    building.anthropogenic_food_output = 0.2f;
                    building.disturbance_output = 0.35f;
                    break;
                case CityPhysicalTokenType.Commercial:
                    building.type = CityBuildingType.Commercial;
                    building.footprint_units = new[] { 20f, 10f };
                    building.housing_capacity = 0;
                    building.human_origin_rate = 0.05f;
                    building.human_destination_weight = 0.9f;
                    building.comfortable_capacity = 16;
                    building.vehicle_demand = 0.7f;
                    building.waste_output = 0.9f;
                    building.anthropogenic_food_output = 0.85f;
                    building.disturbance_output = 0.85f;
                    break;
                case CityPhysicalTokenType.CommunityFacility:
                    building.type = CityBuildingType.CommunityFacility;
                    building.footprint_units = new[] { 20f, 10f };
                    building.housing_capacity = 0;
                    building.human_origin_rate = 0.05f;
                    building.human_destination_weight = 0.7f;
                    building.comfortable_capacity = 24;
                    building.vehicle_demand = 0.5f;
                    building.waste_output = 0.55f;
                    building.anthropogenic_food_output = 0.3f;
                    building.disturbance_output = 0.65f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(token), token.type, "Unsupported building Token.");
            }
            return building;
        }

        public static CityGreenPatch CreateGreenIntervention(
            CityTokenState token,
            CityBounds bounds,
            CityConstructionState constructionState)
        {
            if (token == null || token.type != CityPhysicalTokenType.GreenIntervention)
            {
                throw new ArgumentException("A Green Intervention Token is required.");
            }
            if (bounds == null || bounds.width_units <= 0f || bounds.height_units <= 0f)
            {
                throw new ArgumentException("Positive city bounds are required.");
            }

            float halfWidthNorm = 3f / bounds.width_units;
            float halfHeightNorm = 3f / bounds.height_units;
            return new CityGreenPatch
            {
                id = ObjectId(token),
                source_token_id = token.id,
                type = CityGreenPatchType.ShrubGarden,
                construction_state = constructionState,
                public_park = false,
                polygon_norm = new[]
                {
                    new[] { token.x_norm, token.y_norm - halfHeightNorm },
                    new[] { token.x_norm + halfWidthNorm, token.y_norm },
                    new[] { token.x_norm, token.y_norm + halfHeightNorm },
                    new[] { token.x_norm - halfWidthNorm, token.y_norm },
                },
                shelter_value = 0.55f,
                natural_food_value = 0.5f,
                human_disturbance = 0.2f,
                patch_size_units = 18f,
                connected_patch_ids = Array.Empty<string>(),
            };
        }

        public static CityWasteNode CreateBuildingWasteNode(CityBuilding building)
        {
            if (building == null)
            {
                throw new ArgumentNullException(nameof(building));
            }
            return new CityWasteNode
            {
                id = $"waste-source-{building.id}",
                type = CityWasteNodeType.BuildingOutput,
                source_building_id = building.id,
                position_norm = (float[])building.position_norm.Clone(),
                generation_rate = building.waste_output,
                capacity = 0f,
                fill_ratio = 0f,
            };
        }

        private static CityBuilding BaseBuilding(
            CityTokenState token,
            CityConstructionState constructionState)
        {
            return new CityBuilding
            {
                id = ObjectId(token),
                source_token_id = token.id,
                construction_state = constructionState,
                position_norm = new[] { token.x_norm, token.y_norm },
                rotation_deg = token.rotation_deg,
                planning_cell_ids = Array.Empty<string>(),
                requires_vehicle_access = true,
                requires_pedestrian_access = true,
                vehicle_road_ids = Array.Empty<string>(),
                pedestrian_link_ids = Array.Empty<string>(),
            };
        }
    }
}

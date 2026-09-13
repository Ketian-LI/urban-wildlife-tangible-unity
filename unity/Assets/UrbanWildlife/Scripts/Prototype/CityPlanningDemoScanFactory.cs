using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;
using UrbanWildlife.Input;

namespace UrbanWildlife.Prototype
{
    public static class CityPlanningDemoScanFactory
    {
        public static CityTokenState[] ConfirmedTokensFrom(CityState city)
        {
            return (city?.buildings ?? Array.Empty<CityBuilding>())
                .Where(building => building != null &&
                                   CityTokenInventory.TryGetExpectedType(
                                       building.source_token_id,
                                       out CityPhysicalTokenType _))
                .Select(building => new CityTokenState
                {
                    id = building.source_token_id,
                    type = TokenType(building.type),
                    x_norm = building.position_norm[0],
                    y_norm = building.position_norm[1],
                    rotation_deg = building.rotation_deg,
                    confidence = 1f,
                })
                .OrderBy(token => token.id)
                .ToArray();
        }

        public static CityTokenScanPacket CreateElectronicSample(CityState city, long timestampMs)
        {
            List<CityTokenState> tokens = ConfirmedTokensFrom(city).ToList();
            AddIfAvailable(tokens, new CityTokenState
            {
                id = 102,
                type = CityPhysicalTokenType.Apartment,
                x_norm = 0.50f,
                y_norm = 0.20f,
                rotation_deg = 0f,
                confidence = 0.99f,
            });
            AddIfAvailable(tokens, new CityTokenState
            {
                id = 140,
                type = CityPhysicalTokenType.GreenIntervention,
                x_norm = 0.58f,
                y_norm = 0.64f,
                rotation_deg = 0f,
                confidence = 0.99f,
            });
            DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
            return new CityTokenScanPacket
            {
                schema_version = CityTokenScanContract.SchemaVersion,
                packet_type = CityTokenScanContract.PacketType,
                timestamp_ms = timestampMs,
                timestamp_utc = time.UtcDateTime.ToString("O"),
                scan_id = $"electronic-preview-{timestampMs}",
                session_id = "unity-city-prototype",
                development_phase = 1,
                calibration_id = "electronic-screen-test",
                coordinate_system = new LayoutCoordinateSystem
                {
                    origin = "top_left",
                    x_axis = "right",
                    y_axis = "down",
                    range = new[] { 0f, 1f },
                    unity_plane_mapping = "x_norm -> Unity X; y_norm -> Unity Z",
                },
                capture = new LayoutCapture
                {
                    mode = "electronic_sample",
                    stable = true,
                    note = "Temporary electronic substitute for the overhead physical board.",
                },
                recognition = new CityTokenRecognition
                {
                    marker_backend = "aruco",
                    marker_family = "DICT_4X4_1000",
                    token_config_version = "city-tokens-0.1",
                },
                token_states = tokens.OrderBy(token => token.id).ToArray(),
            };
        }

        private static void AddIfAvailable(List<CityTokenState> tokens, CityTokenState candidate)
        {
            if (tokens.All(token => token.id != candidate.id))
            {
                tokens.Add(candidate);
            }
        }

        private static CityPhysicalTokenType TokenType(CityBuildingType type)
        {
            switch (type)
            {
                case CityBuildingType.Apartment: return CityPhysicalTokenType.Apartment;
                case CityBuildingType.DetachedHouse: return CityPhysicalTokenType.DetachedHouse;
                case CityBuildingType.Commercial: return CityPhysicalTokenType.Commercial;
                default: return CityPhysicalTokenType.CommunityFacility;
            }
        }
    }
}

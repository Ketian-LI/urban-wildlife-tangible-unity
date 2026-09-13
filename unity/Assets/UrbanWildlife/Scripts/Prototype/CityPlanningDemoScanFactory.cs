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
            CityPlanningGrid grid = city?.planning_grid;
            CityGridCell[] availableCells = grid == null
                ? null
                : (grid.cells ?? Array.Empty<CityGridCell>())
                    .Where(IsAvailablePlanningCell)
                    .OrderBy(cell => cell.row)
                    .ThenBy(cell => cell.col)
                    .ToArray();
            int nextCellIndex = 0;
            AddElectronicSampleIfAvailable(
                tokens,
                102,
                CityPhysicalTokenType.Apartment,
                availableCells,
                ref nextCellIndex,
                0.25f,
                0.125f);
            AddElectronicSampleIfAvailable(
                tokens,
                140,
                CityPhysicalTokenType.GreenIntervention,
                availableCells,
                ref nextCellIndex,
                0.25f,
                0.375f);
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

        private static void AddElectronicSampleIfAvailable(
            List<CityTokenState> tokens,
            int id,
            CityPhysicalTokenType type,
            CityGridCell[] availableCells,
            ref int nextCellIndex,
            float fallbackX,
            float fallbackY)
        {
            if (tokens.Any(token => token.id == id))
            {
                return;
            }

            float x = fallbackX;
            float y = fallbackY;
            if (availableCells != null)
            {
                if (nextCellIndex >= availableCells.Length)
                {
                    return;
                }
                CityGridCell cell = availableCells[nextCellIndex];
                nextCellIndex += 1;
                x = cell.center_norm[0];
                y = cell.center_norm[1];
            }

            tokens.Add(new CityTokenState
            {
                id = id,
                type = type,
                x_norm = x,
                y_norm = y,
                rotation_deg = 0f,
                confidence = 0.99f,
            });
        }

        private static bool IsAvailablePlanningCell(CityGridCell cell)
        {
            return cell != null &&
                   cell.buildable &&
                   !cell.fixed_feature &&
                   string.IsNullOrWhiteSpace(cell.occupant_id) &&
                   string.IsNullOrWhiteSpace(cell.habitat_patch_id) &&
                   cell.center_norm != null &&
                   cell.center_norm.Length >= 2;
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

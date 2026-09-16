using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;
using UrbanWildlife.Construction;
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
                    x_norm = TokenAnchorPosition(city, building)[0],
                    y_norm = TokenAnchorPosition(city, building)[1],
                    rotation_deg = building.rotation_deg,
                    confidence = 1f,
                })
                .OrderBy(token => token.id)
                .ToArray();
        }

        private static float[] TokenAnchorPosition(CityState city, CityBuilding building)
        {
            if (building?.position_norm != null && building.position_norm.Length >= 2)
            {
                return new[] { building.position_norm[0], building.position_norm[1] };
            }

            CityGridCell anchor = CityGridResolver.GetCell(
                city?.planning_grid,
                building?.planning_cell_id);
            return anchor?.center_norm ?? new[] { 0f, 0f };
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
                112,
                CityPhysicalTokenType.DetachedHouse,
                availableCells,
                grid,
                city?.bounds,
                city?.buildings,
                city?.vehicle_roads,
                ref nextCellIndex,
                0.25f,
                0.125f);
            AddElectronicSampleIfAvailable(
                tokens,
                140,
                CityPhysicalTokenType.GreenIntervention,
                availableCells,
                grid,
                city?.bounds,
                city?.buildings,
                city?.vehicle_roads,
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

        public static bool TryCreateDesktopPlacementScan(
            CityState city,
            CityPhysicalTokenType type,
            float xNorm,
            float yNorm,
            long timestampMs,
            out CityTokenScanPacket scan,
            out string error)
        {
            scan = null;
            error = string.Empty;
            if (city == null)
            {
                error = "The current city is unavailable.";
                return false;
            }
            if (type == CityPhysicalTokenType.GreenIntervention)
            {
                error = "Desktop building placement expects a building type.";
                return false;
            }
            if (xNorm < 0f || xNorm > 1f || yNorm < 0f || yNorm > 1f)
            {
                error = "Choose a position inside the map.";
                return false;
            }

            List<CityTokenState> tokens = ConfirmedTokensFrom(city).ToList();
            HashSet<int> usedIds = new HashSet<int>(tokens.Select(token => token.id));
            int tokenId = CityTokenInventory.IdsFor(type)
                .FirstOrDefault(id => !usedIds.Contains(id));
            if (tokenId == 0)
            {
                tokenId = CityTokenInventory.NextDesktopVirtualId(type, usedIds);
                if (tokenId == 0)
                {
                    error = $"The desktop city has reached its {type} building limit.";
                    return false;
                }
            }

            tokens.Add(new CityTokenState
            {
                id = tokenId,
                type = type,
                x_norm = xNorm,
                y_norm = yNorm,
                rotation_deg = 0f,
                confidence = 1f,
            });
            DateTimeOffset time = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs);
            scan = new CityTokenScanPacket
            {
                schema_version = CityTokenScanContract.SchemaVersion,
                packet_type = CityTokenScanContract.PacketType,
                timestamp_ms = timestampMs,
                timestamp_utc = time.UtcDateTime.ToString("O"),
                scan_id = $"desktop-placement-{timestampMs}",
                session_id = "unity-city-desktop",
                development_phase = 1,
                calibration_id = "desktop-pointer",
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
                    mode = "desktop_pointer",
                    stable = true,
                    note = "Mouse placement in the camera-free Unity edition.",
                },
                recognition = new CityTokenRecognition
                {
                    marker_backend = "desktop_pointer",
                    marker_family = "none",
                    token_config_version = "city-tokens-0.1",
                },
                token_states = tokens.OrderBy(token => token.id).ToArray(),
            };
            return true;
        }

        private static void AddElectronicSampleIfAvailable(
            List<CityTokenState> tokens,
            int id,
            CityPhysicalTokenType type,
            CityGridCell[] availableCells,
            CityPlanningGrid grid,
            CityBounds bounds,
            IEnumerable<CityBuilding> existingBuildings,
            IEnumerable<CityVehicleRoad> roads,
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
                CityGridCell cell = null;
                while (nextCellIndex < availableCells.Length && cell == null)
                {
                    CityGridCell candidate = availableCells[nextCellIndex];
                    nextCellIndex += 1;
                    if (type == CityPhysicalTokenType.GreenIntervention ||
                        CanFitBuilding(
                            type,
                            id,
                            candidate,
                            grid,
                            bounds,
                            existingBuildings,
                            roads))
                    {
                        cell = candidate;
                    }
                }
                if (cell == null)
                {
                    return;
                }
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

        private static bool CanFitBuilding(
            CityPhysicalTokenType type,
            int id,
            CityGridCell anchor,
            CityPlanningGrid grid,
            CityBounds bounds,
            IEnumerable<CityBuilding> existingBuildings,
            IEnumerable<CityVehicleRoad> roads)
        {
            if (anchor == null || grid == null || bounds == null)
            {
                return false;
            }
            CityTokenState token = new CityTokenState
            {
                id = id,
                type = type,
                x_norm = anchor.center_norm[0],
                y_norm = anchor.center_norm[1],
                rotation_deg = 0f,
                confidence = 1f,
            };
            CityBuilding building = CityConstructionFactory.CreateBuilding(
                token,
                CityConstructionState.Proposed);
            CityGridCell[] footprint = CityGridResolver.GetBuildingFootprintCellsAtPosition(
                grid,
                bounds,
                building,
                out string error);
            return string.IsNullOrEmpty(error) && footprint.Length > 0 &&
                   footprint.All(cell => cell.buildable && !cell.fixed_feature &&
                                          string.IsNullOrWhiteSpace(cell.occupant_id)) &&
                   string.IsNullOrEmpty(CityConstructionPlacementRules.ValidateBuilding(
                       building,
                       bounds,
                       existingBuildings,
                       Array.Empty<CityBuilding>(),
                       roads));
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

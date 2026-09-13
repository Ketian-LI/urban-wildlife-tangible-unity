using System;
using System.Collections.Generic;
using System.Linq;

namespace UrbanWildlife.City
{
    public static class CityGridResolver
    {
        public const float DefaultSnapRadiusUnits = 5f;
        public const float GeometryTolerance = 0.0001f;

        public static bool TryResolveNearestCell(
            CityPlanningGrid grid,
            CityBounds bounds,
            float[] positionNorm,
            out CityGridCell cell,
            float snapRadiusUnits = DefaultSnapRadiusUnits)
        {
            cell = null;
            if (grid?.cells == null || bounds == null ||
                !PositiveFinite(bounds.width_units) || !PositiveFinite(bounds.height_units) ||
                !IsUnitPoint(positionNorm) || !NonNegativeFinite(snapRadiusUnits))
            {
                return false;
            }

            CityGridCell nearest = grid.cells
                .Where(candidate => candidate != null && IsUnitPoint(candidate.center_norm))
                .OrderBy(candidate => DistanceUnits(positionNorm, candidate.center_norm, bounds))
                .ThenBy(candidate => candidate.id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (nearest == null ||
                DistanceUnits(positionNorm, nearest.center_norm, bounds) >
                snapRadiusUnits + GeometryTolerance)
            {
                return false;
            }

            cell = nearest;
            return true;
        }

        public static CityGridCell GetCell(CityPlanningGrid grid, string cellId)
        {
            if (grid?.cells == null || string.IsNullOrWhiteSpace(cellId))
            {
                return null;
            }
            return grid.cells.FirstOrDefault(cell => cell != null && cell.id == cellId);
        }

        public static bool TryGetContainingCell(
            CityPlanningGrid grid,
            float[] positionNorm,
            out CityGridCell cell)
        {
            cell = null;
            if (grid?.cells == null || !IsUnitPoint(positionNorm))
            {
                return false;
            }

            cell = grid.cells
                .Where(candidate => candidate != null &&
                                    IsUnitPoint(candidate.center_norm) &&
                                    IsUnitSize(candidate.size_norm))
                .FirstOrDefault(candidate =>
                    Math.Abs(positionNorm[0] - candidate.center_norm[0]) <=
                    candidate.size_norm[0] * 0.5f + GeometryTolerance &&
                    Math.Abs(positionNorm[1] - candidate.center_norm[1]) <=
                    candidate.size_norm[1] * 0.5f + GeometryTolerance);
            return cell != null;
        }

        public static CityGridCell[] GetFourNeighbours(CityPlanningGrid grid, string cellId)
        {
            CityGridCell origin = GetCell(grid, cellId);
            if (origin == null)
            {
                return Array.Empty<CityGridCell>();
            }
            return grid.cells
                .Where(cell => cell != null &&
                               Math.Abs(cell.row - origin.row) + Math.Abs(cell.col - origin.col) == 1)
                .OrderBy(cell => cell.row)
                .ThenBy(cell => cell.col)
                .ToArray();
        }

        public static CityPlanningGrid DeepClone(CityPlanningGrid grid)
        {
            if (grid == null)
            {
                return null;
            }
            return new CityPlanningGrid
            {
                cols = grid.cols,
                rows = grid.rows,
                max_active_player_buildings = grid.max_active_player_buildings,
                cells = (grid.cells ?? Array.Empty<CityGridCell>())
                    .Select(CloneCell)
                    .ToArray(),
            };
        }

        public static bool TryOccupyWithBuilding(
            CityPlanningGrid grid,
            CityBounds bounds,
            CityBuilding building,
            int revision,
            out string error)
        {
            error = string.Empty;
            if (building == null)
            {
                error = "Building is missing.";
                return false;
            }
            if (!TryResolveAssignedCell(
                    grid,
                    bounds,
                    building.planning_cell_id,
                    building.position_norm,
                    out CityGridCell cell,
                    out error))
            {
                return false;
            }
            if (!cell.buildable || cell.fixed_feature)
            {
                error = $"Grid cell {cell.id} does not allow building placement.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(cell.occupant_id) && cell.occupant_id != building.id)
            {
                error = $"Grid cell {cell.id} is already occupied by {cell.occupant_id}.";
                return false;
            }

            cell.was_woodland = cell.was_woodland ||
                                cell.baseline_cover == CityLandCover.Woodland ||
                                cell.current_cover == CityLandCover.Woodland;
            cell.current_cover = CityLandCover.Building;
            cell.occupant_id = building.id;
            cell.habitat_patch_id = null;
            cell.last_changed_revision = revision;
            building.planning_cell_id = cell.id;
            building.position_norm = ClonePoint(cell.center_norm);
            return true;
        }

        public static bool TryOccupyWithGreenPatch(
            CityPlanningGrid grid,
            CityBounds bounds,
            CityGreenPatch patch,
            int revision,
            out string error)
        {
            error = string.Empty;
            if (patch == null)
            {
                error = "Green patch is missing.";
                return false;
            }
            float[] position = PolygonCentroid(patch.polygon_norm);
            if (!TryResolveAssignedCell(
                    grid,
                    bounds,
                    patch.planning_cell_id,
                    position,
                    out CityGridCell cell,
                    out error))
            {
                return false;
            }
            if (!cell.buildable || cell.fixed_feature)
            {
                error = $"Grid cell {cell.id} does not allow green intervention placement.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(cell.occupant_id))
            {
                error = $"Grid cell {cell.id} is occupied by building {cell.occupant_id}.";
                return false;
            }
            if (cell.fixed_feature && cell.habitat_patch_id != patch.id)
            {
                error = $"Fixed grid cell {cell.id} cannot be replaced by a green patch.";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(cell.habitat_patch_id) &&
                cell.habitat_patch_id != patch.id)
            {
                error = $"Grid cell {cell.id} already contains habitat {cell.habitat_patch_id}.";
                return false;
            }

            CityLandCover cover = CoverFor(patch);
            cell.was_woodland = cell.was_woodland ||
                                cell.baseline_cover == CityLandCover.Woodland ||
                                cell.current_cover == CityLandCover.Woodland ||
                                cover == CityLandCover.Woodland;
            cell.current_cover = cover;
            cell.occupant_id = null;
            cell.habitat_patch_id = patch.id;
            cell.last_changed_revision = revision;
            patch.planning_cell_id = cell.id;
            patch.polygon_norm = PolygonFor(cell);
            patch.patch_size_units = cell.size_norm[0] * bounds.width_units *
                                     cell.size_norm[1] * bounds.height_units;
            return true;
        }

        public static bool TryReleaseCell(
            CityPlanningGrid grid,
            string cellId,
            int revision,
            out string error)
        {
            error = string.Empty;
            CityGridCell cell = GetCell(grid, cellId);
            if (cell == null)
            {
                error = $"Grid cell '{cellId}' does not exist.";
                return false;
            }
            if (cell.fixed_feature)
            {
                error = $"Fixed grid cell {cell.id} cannot be released.";
                return false;
            }

            cell.was_woodland = cell.was_woodland ||
                                cell.baseline_cover == CityLandCover.Woodland ||
                                cell.current_cover == CityLandCover.Woodland;
            cell.current_cover = cell.was_woodland
                ? CityLandCover.Disturbed
                : CityLandCover.OpenLand;
            cell.occupant_id = null;
            cell.habitat_patch_id = null;
            cell.last_changed_revision = revision;
            return true;
        }

        public static float[] ExpectedCenterNorm(CityPlanningGrid grid, int row, int col)
        {
            if (grid == null || grid.cols <= 0 || grid.rows <= 0)
            {
                return null;
            }
            return new[]
            {
                (col + 0.5f) / grid.cols,
                (row + 0.5f) / grid.rows,
            };
        }

        public static float[] ExpectedSizeNorm(CityPlanningGrid grid)
        {
            if (grid == null || grid.cols <= 0 || grid.rows <= 0)
            {
                return null;
            }
            return new[] { 1f / grid.cols, 1f / grid.rows };
        }

        public static float[][] PolygonFor(CityGridCell cell)
        {
            if (cell == null || !IsUnitPoint(cell.center_norm) || !IsUnitSize(cell.size_norm))
            {
                return Array.Empty<float[]>();
            }
            float halfWidth = cell.size_norm[0] * 0.5f;
            float halfHeight = cell.size_norm[1] * 0.5f;
            return new[]
            {
                new[] { cell.center_norm[0] - halfWidth, cell.center_norm[1] - halfHeight },
                new[] { cell.center_norm[0] + halfWidth, cell.center_norm[1] - halfHeight },
                new[] { cell.center_norm[0] + halfWidth, cell.center_norm[1] + halfHeight },
                new[] { cell.center_norm[0] - halfWidth, cell.center_norm[1] + halfHeight },
            };
        }

        public static bool IsUnitPoint(float[] point)
        {
            return point != null && point.Length == 2 &&
                   UnitFinite(point[0]) && UnitFinite(point[1]);
        }

        public static bool IsUnitSize(float[] size)
        {
            return size != null && size.Length == 2 &&
                   PositiveFinite(size[0]) && size[0] <= 1f &&
                   PositiveFinite(size[1]) && size[1] <= 1f;
        }

        public static bool Approximately(float first, float second)
        {
            return Math.Abs(first - second) <= GeometryTolerance;
        }

        public static bool IsHabitatCover(CityLandCover cover)
        {
            return cover == CityLandCover.Woodland ||
                   cover == CityLandCover.PublicGreen ||
                   cover == CityLandCover.ShrubGarden ||
                   cover == CityLandCover.Recovering;
        }

        private static bool TryResolveAssignedCell(
            CityPlanningGrid grid,
            CityBounds bounds,
            string assignedCellId,
            float[] positionNorm,
            out CityGridCell cell,
            out string error)
        {
            error = string.Empty;
            if (!string.IsNullOrWhiteSpace(assignedCellId))
            {
                cell = GetCell(grid, assignedCellId);
                if (cell != null)
                {
                    return true;
                }
                error = $"Grid cell '{assignedCellId}' does not exist.";
                return false;
            }
            if (TryResolveNearestCell(grid, bounds, positionNorm, out cell))
            {
                return true;
            }
            error = "Position is not within the grid snap radius of a planning cell.";
            return false;
        }

        private static CityGridCell CloneCell(CityGridCell cell)
        {
            if (cell == null)
            {
                return null;
            }
            return new CityGridCell
            {
                id = cell.id,
                row = cell.row,
                col = cell.col,
                center_norm = ClonePoint(cell.center_norm),
                size_norm = ClonePoint(cell.size_norm),
                baseline_cover = cell.baseline_cover,
                current_cover = cell.current_cover,
                buildable = cell.buildable,
                fixed_feature = cell.fixed_feature,
                occupant_id = cell.occupant_id,
                habitat_patch_id = cell.habitat_patch_id,
                was_woodland = cell.was_woodland,
                last_changed_revision = cell.last_changed_revision,
            };
        }

        private static CityLandCover CoverFor(CityGreenPatch patch)
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

        private static float[] PolygonCentroid(IEnumerable<float[]> points)
        {
            float[][] valid = (points ?? Array.Empty<float[]>())
                .Where(IsUnitPoint)
                .ToArray();
            return valid.Length == 0
                ? null
                : new[]
                {
                    valid.Average(point => point[0]),
                    valid.Average(point => point[1]),
                };
        }

        private static float DistanceUnits(float[] first, float[] second, CityBounds bounds)
        {
            float x = (first[0] - second[0]) * bounds.width_units;
            float y = (first[1] - second[1]) * bounds.height_units;
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static bool UnitFinite(float value)
        {
            return value >= 0f && value <= 1f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool PositiveFinite(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool NonNegativeFinite(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float[] ClonePoint(float[] point)
        {
            return point == null ? null : (float[])point.Clone();
        }
    }
}

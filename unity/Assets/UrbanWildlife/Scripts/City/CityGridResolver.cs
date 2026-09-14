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
            CityGridCell[] footprintCells = BuildingFootprintCells(
                grid,
                bounds,
                building,
                cell,
                out error);
            if (footprintCells.Length == 0)
            {
                return false;
            }
            return OccupyBuildingCells(
                building,
                cell,
                footprintCells,
                revision,
                true,
                out error);
        }

        public static bool TryOccupyWithBuildingAtPosition(
            CityPlanningGrid grid,
            CityBounds bounds,
            CityBuilding building,
            int revision,
            out string error)
        {
            CityGridCell[] footprintCells = GetBuildingFootprintCellsAtPosition(
                grid,
                bounds,
                building,
                out error);
            if (footprintCells.Length == 0)
            {
                return false;
            }
            CityGridCell anchor = footprintCells.FirstOrDefault(candidate =>
                PointInsideCell(building.position_norm, candidate));
            if (anchor == null && !TryResolveNearestCell(
                    grid,
                    bounds,
                    building.position_norm,
                    out anchor))
            {
                error = "Building centre is outside the planning surface.";
                return false;
            }
            return OccupyBuildingCells(
                building,
                anchor,
                footprintCells,
                revision,
                false,
                out error);
        }

        public static CityGridCell[] GetBuildingFootprintCells(
            CityPlanningGrid grid,
            CityBounds bounds,
            CityBuilding building,
            out string error)
        {
            error = string.Empty;
            if (building == null)
            {
                error = "Building is missing.";
                return Array.Empty<CityGridCell>();
            }
            if (!TryResolveAssignedCell(
                    grid,
                    bounds,
                    building.planning_cell_id,
                    building.position_norm,
                    out CityGridCell anchor,
                    out error))
            {
                return Array.Empty<CityGridCell>();
            }
            return BuildingFootprintCells(grid, bounds, building, anchor, out error);
        }

        public static CityGridCell[] GetBuildingFootprintCellsAtPosition(
            CityPlanningGrid grid,
            CityBounds bounds,
            CityBuilding building,
            out string error)
        {
            error = string.Empty;
            if (grid?.cells == null || bounds == null ||
                !PositiveFinite(bounds.width_units) || !PositiveFinite(bounds.height_units) ||
                building == null || !IsUnitPoint(building.position_norm) ||
                building.footprint_units == null || building.footprint_units.Length != 2 ||
                !PositiveFinite(building.footprint_units[0]) ||
                !PositiveFinite(building.footprint_units[1]))
            {
                error = "Building footprint or planning surface is incomplete.";
                return Array.Empty<CityGridCell>();
            }

            float centreX = building.position_norm[0] * bounds.width_units;
            float centreY = building.position_norm[1] * bounds.height_units;
            float radians = building.rotation_deg * (float)Math.PI / 180f;
            float cosine = (float)Math.Cos(radians);
            float sine = (float)Math.Sin(radians);
            float halfWidth = building.footprint_units[0] * 0.5f;
            float halfHeight = building.footprint_units[1] * 0.5f;
            float envelopeHalfX = Math.Abs(cosine) * halfWidth + Math.Abs(sine) * halfHeight;
            float envelopeHalfY = Math.Abs(sine) * halfWidth + Math.Abs(cosine) * halfHeight;
            if (centreX - envelopeHalfX < -GeometryTolerance ||
                centreX + envelopeHalfX > bounds.width_units + GeometryTolerance ||
                centreY - envelopeHalfY < -GeometryTolerance ||
                centreY + envelopeHalfY > bounds.height_units + GeometryTolerance)
            {
                error = "Building footprint extends beyond the planning surface.";
                return Array.Empty<CityGridCell>();
            }

            CityGridCell[] cells = grid.cells
                .Where(cell => cell != null && IsUnitPoint(cell.center_norm) &&
                               IsUnitSize(cell.size_norm) &&
                               RotatedFootprintOverlapsCell(
                                   centreX,
                                   centreY,
                                   halfWidth,
                                   halfHeight,
                                   cosine,
                                   sine,
                                   cell,
                                   bounds))
                .OrderBy(cell => cell.row)
                .ThenBy(cell => cell.col)
                .ToArray();
            if (cells.Length == 0)
            {
                error = "Building footprint does not overlap the planning surface.";
            }
            return cells;
        }

        public static string[] ClearWoodlandAlongRoads(
            CityPlanningGrid grid,
            CityBounds bounds,
            IEnumerable<CityVehicleRoad> roads,
            int revision)
        {
            if (grid?.cells == null || bounds == null ||
                !PositiveFinite(bounds.width_units) || !PositiveFinite(bounds.height_units))
            {
                return Array.Empty<string>();
            }

            CityVehicleRoad[] activeRoads = (roads ?? Array.Empty<CityVehicleRoad>())
                .Where(road => road?.points_norm != null && road.points_norm.Length >= 2)
                .ToArray();
            HashSet<string> removedPatchIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (CityGridCell cell in grid.cells.Where(candidate =>
                         candidate != null &&
                         candidate.current_cover == CityLandCover.Woodland))
            {
                if (!activeRoads.Any(road => RoadOverlapsCell(road, cell, bounds)))
                {
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(cell.habitat_patch_id))
                {
                    removedPatchIds.Add(cell.habitat_patch_id);
                }
                cell.was_woodland = true;
                cell.current_cover = CityLandCover.OpenLand;
                cell.habitat_patch_id = null;
                cell.last_changed_revision = revision;
            }
            return removedPatchIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
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

            CityGridCell[] releasedCells = string.IsNullOrWhiteSpace(cell.occupant_id)
                ? new[] { cell }
                : grid.cells.Where(candidate => candidate != null &&
                                                candidate.occupant_id == cell.occupant_id)
                    .ToArray();
            if (releasedCells.Any(candidate => candidate.fixed_feature))
            {
                error = "A fixed planning feature cannot be released.";
                return false;
            }
            foreach (CityGridCell releasedCell in releasedCells)
            {
                releasedCell.was_woodland = releasedCell.was_woodland ||
                                             releasedCell.baseline_cover == CityLandCover.Woodland ||
                                             releasedCell.current_cover == CityLandCover.Woodland;
                releasedCell.current_cover = releasedCell.was_woodland
                    ? CityLandCover.Disturbed
                    : CityLandCover.OpenLand;
                releasedCell.occupant_id = null;
                releasedCell.habitat_patch_id = null;
                releasedCell.last_changed_revision = revision;
            }
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

        private static bool OccupyBuildingCells(
            CityBuilding building,
            CityGridCell anchor,
            CityGridCell[] footprintCells,
            int revision,
            bool snapPositionToCells,
            out string error)
        {
            error = string.Empty;
            CityGridCell unavailable = footprintCells.FirstOrDefault(candidate =>
                !candidate.buildable || candidate.fixed_feature);
            if (unavailable != null)
            {
                error = $"Grid cell {unavailable.id} does not allow building placement.";
                return false;
            }
            CityGridCell occupied = footprintCells.FirstOrDefault(candidate =>
                !string.IsNullOrWhiteSpace(candidate.occupant_id) &&
                candidate.occupant_id != building.id);
            if (occupied != null)
            {
                error = $"Grid cell {occupied.id} is already occupied by {occupied.occupant_id}.";
                return false;
            }

            foreach (CityGridCell footprintCell in footprintCells)
            {
                footprintCell.was_woodland = footprintCell.was_woodland ||
                                              footprintCell.baseline_cover == CityLandCover.Woodland ||
                                              footprintCell.current_cover == CityLandCover.Woodland;
                footprintCell.current_cover = CityLandCover.Building;
                footprintCell.occupant_id = building.id;
                footprintCell.habitat_patch_id = null;
                footprintCell.last_changed_revision = revision;
            }
            building.planning_cell_id = anchor.id;
            building.planning_cell_ids = footprintCells.Select(candidate => candidate.id).ToArray();
            if (snapPositionToCells)
            {
                building.position_norm = new[]
                {
                    footprintCells.Average(candidate => candidate.center_norm[0]),
                    footprintCells.Average(candidate => candidate.center_norm[1]),
                };
            }
            return true;
        }

        private static bool PointInsideCell(float[] point, CityGridCell cell)
        {
            return IsUnitPoint(point) && cell != null &&
                   Math.Abs(point[0] - cell.center_norm[0]) <=
                   cell.size_norm[0] * 0.5f + GeometryTolerance &&
                   Math.Abs(point[1] - cell.center_norm[1]) <=
                   cell.size_norm[1] * 0.5f + GeometryTolerance;
        }

        private static bool RotatedFootprintOverlapsCell(
            float centreX,
            float centreY,
            float halfWidth,
            float halfHeight,
            float cosine,
            float sine,
            CityGridCell cell,
            CityBounds bounds)
        {
            float cellCentreX = cell.center_norm[0] * bounds.width_units;
            float cellCentreY = cell.center_norm[1] * bounds.height_units;
            float cellHalfWidth = cell.size_norm[0] * bounds.width_units * 0.5f;
            float cellHalfHeight = cell.size_norm[1] * bounds.height_units * 0.5f;
            float deltaX = cellCentreX - centreX;
            float deltaY = cellCentreY - centreY;
            float localX = deltaX * cosine + deltaY * sine;
            float localY = -deltaX * sine + deltaY * cosine;

            float cellRadiusOnBuildingX = cellHalfWidth * Math.Abs(cosine) +
                                          cellHalfHeight * Math.Abs(sine);
            if (Math.Abs(localX) >= halfWidth + cellRadiusOnBuildingX - GeometryTolerance)
            {
                return false;
            }
            float cellRadiusOnBuildingY = cellHalfWidth * Math.Abs(sine) +
                                          cellHalfHeight * Math.Abs(cosine);
            if (Math.Abs(localY) >= halfHeight + cellRadiusOnBuildingY - GeometryTolerance)
            {
                return false;
            }

            float buildingRadiusOnWorldX = halfWidth * Math.Abs(cosine) +
                                           halfHeight * Math.Abs(sine);
            if (Math.Abs(deltaX) >= cellHalfWidth + buildingRadiusOnWorldX - GeometryTolerance)
            {
                return false;
            }
            float buildingRadiusOnWorldY = halfWidth * Math.Abs(sine) +
                                           halfHeight * Math.Abs(cosine);
            return Math.Abs(deltaY) < cellHalfHeight + buildingRadiusOnWorldY - GeometryTolerance;
        }

        private static bool RoadOverlapsCell(
            CityVehicleRoad road,
            CityGridCell cell,
            CityBounds bounds)
        {
            float padding = Math.Max(0f, road.width_units * 0.5f);
            float halfWidth = cell.size_norm[0] * bounds.width_units * 0.5f + padding;
            float halfHeight = cell.size_norm[1] * bounds.height_units * 0.5f + padding;
            float centreX = cell.center_norm[0] * bounds.width_units;
            float centreY = cell.center_norm[1] * bounds.height_units;
            float minX = centreX - halfWidth;
            float maxX = centreX + halfWidth;
            float minY = centreY - halfHeight;
            float maxY = centreY + halfHeight;
            for (int index = 1; index < road.points_norm.Length; index += 1)
            {
                float[] first = road.points_norm[index - 1];
                float[] second = road.points_norm[index];
                if (!IsUnitPoint(first) || !IsUnitPoint(second))
                {
                    continue;
                }
                if (SegmentIntersectsRectangle(
                        first[0] * bounds.width_units,
                        first[1] * bounds.height_units,
                        second[0] * bounds.width_units,
                        second[1] * bounds.height_units,
                        minX,
                        maxX,
                        minY,
                        maxY))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool SegmentIntersectsRectangle(
            float firstX,
            float firstY,
            float secondX,
            float secondY,
            float minX,
            float maxX,
            float minY,
            float maxY)
        {
            float deltaX = secondX - firstX;
            float deltaY = secondY - firstY;
            float enter = 0f;
            float exit = 1f;
            return Clip(-deltaX, firstX - minX, ref enter, ref exit) &&
                   Clip(deltaX, maxX - firstX, ref enter, ref exit) &&
                   Clip(-deltaY, firstY - minY, ref enter, ref exit) &&
                   Clip(deltaY, maxY - firstY, ref enter, ref exit);
        }

        private static bool Clip(float direction, float distance, ref float enter, ref float exit)
        {
            if (Math.Abs(direction) <= GeometryTolerance)
            {
                return distance >= 0f;
            }
            float ratio = distance / direction;
            if (direction < 0f)
            {
                if (ratio > exit) return false;
                if (ratio > enter) enter = ratio;
            }
            else
            {
                if (ratio < enter) return false;
                if (ratio < exit) exit = ratio;
            }
            return true;
        }

        private static CityGridCell[] BuildingFootprintCells(
            CityPlanningGrid grid,
            CityBounds bounds,
            CityBuilding building,
            CityGridCell anchor,
            out string error)
        {
            error = string.Empty;
            if (grid?.cells == null || bounds == null || anchor == null ||
                building.footprint_units == null || building.footprint_units.Length != 2 ||
                !PositiveFinite(building.footprint_units[0]) ||
                !PositiveFinite(building.footprint_units[1]))
            {
                error = "Building footprint or planning grid is incomplete.";
                return Array.Empty<CityGridCell>();
            }

            float cellWidth = bounds.width_units / grid.cols;
            float cellHeight = bounds.height_units / grid.rows;
            bool quarterTurn = IsQuarterTurn(building.rotation_deg);
            float footprintWidth = quarterTurn
                ? building.footprint_units[1]
                : building.footprint_units[0];
            float footprintHeight = quarterTurn
                ? building.footprint_units[0]
                : building.footprint_units[1];
            int spanColumns = Math.Max(1, (int)Math.Ceiling(
                footprintWidth / cellWidth - GeometryTolerance));
            int spanRows = Math.Max(1, (int)Math.Ceiling(
                footprintHeight / cellHeight - GeometryTolerance));
            if (anchor.col + spanColumns > grid.cols || anchor.row + spanRows > grid.rows)
            {
                error =
                    $"The {spanColumns}×{spanRows} building footprint extends beyond the planning grid.";
                return Array.Empty<CityGridCell>();
            }

            List<CityGridCell> cells = new List<CityGridCell>();
            for (int row = anchor.row; row < anchor.row + spanRows; row += 1)
            {
                for (int col = anchor.col; col < anchor.col + spanColumns; col += 1)
                {
                    CityGridCell cell = grid.cells.FirstOrDefault(candidate =>
                        candidate != null && candidate.row == row && candidate.col == col);
                    if (cell == null)
                    {
                        error = $"Planning grid is missing the cell at row {row}, column {col}.";
                        return Array.Empty<CityGridCell>();
                    }
                    cells.Add(cell);
                }
            }
            return cells.ToArray();
        }

        private static bool IsQuarterTurn(float rotationDeg)
        {
            double radians = rotationDeg * Math.PI / 180d;
            return Math.Abs(Math.Sin(radians)) > Math.Abs(Math.Cos(radians));
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

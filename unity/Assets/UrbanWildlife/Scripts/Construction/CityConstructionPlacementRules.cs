using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Construction
{
    public static class CityConstructionPlacementRules
    {
        public const float RoadPlacementClearanceUnits = 0.35f;

        public static string ValidateBuilding(
            CityBuilding candidate,
            CityBounds bounds,
            IEnumerable<CityBuilding> existingBuildings,
            IEnumerable<CityBuilding> otherProposals)
        {
            return ValidateBuilding(
                candidate,
                bounds,
                existingBuildings,
                otherProposals,
                Array.Empty<CityVehicleRoad>());
        }

        public static string ValidateBuilding(
            CityBuilding candidate,
            CityBounds bounds,
            IEnumerable<CityBuilding> existingBuildings,
            IEnumerable<CityBuilding> otherProposals,
            IEnumerable<CityVehicleRoad> roads)
        {
            if (candidate == null || bounds == null)
            {
                return "Building placement data is incomplete.";
            }
            if (candidate.position_norm == null || candidate.position_norm.Length != 2 ||
                candidate.footprint_units == null || candidate.footprint_units.Length != 2 ||
                candidate.position_norm.Any(value => value < 0f || value > 1f ||
                                                     float.IsNaN(value) || float.IsInfinity(value)) ||
                candidate.footprint_units.Any(value => value <= 0f ||
                                                      float.IsNaN(value) || float.IsInfinity(value)))
            {
                return "Building placement data is incomplete.";
            }

            Footprint candidateFootprint = Footprint.From(candidate, bounds);
            if (!candidateFootprint.IsInside(bounds))
            {
                return "Building footprint extends beyond the city boundary.";
            }

            IEnumerable<CityBuilding> occupied =
                (existingBuildings ?? Array.Empty<CityBuilding>())
                .Concat(otherProposals ?? Array.Empty<CityBuilding>());
            CityBuilding overlap = occupied.FirstOrDefault(building =>
                building != null && Footprint.From(building, bounds).Overlaps(candidateFootprint));
            if (overlap != null)
            {
                return $"Building footprint overlaps {overlap.id}.";
            }

            CityVehicleRoad roadOverlap = (roads ?? Array.Empty<CityVehicleRoad>())
                .Where(road => road != null &&
                               road.construction_state != CityConstructionState.Demolishing)
                .FirstOrDefault(road => candidateFootprint.IntersectsRoad(road, bounds));
            return roadOverlap == null
                ? string.Empty
                : $"Building footprint overlaps road {roadOverlap.id}.";
        }

        public static string ValidateGreenIntervention(CityGreenPatch candidate)
        {
            if (candidate?.polygon_norm == null || candidate.polygon_norm.Length < 3)
            {
                return "Green Intervention footprint is incomplete.";
            }
            bool outside = candidate.polygon_norm.Any(point =>
                point == null || point.Length != 2 || point[0] < 0f || point[0] > 1f ||
                point[1] < 0f || point[1] > 1f);
            return outside
                ? "Green Intervention footprint extends beyond the city boundary."
                : string.Empty;
        }

        private struct Footprint
        {
            public float MinX;
            public float MaxX;
            public float MinY;
            public float MaxY;

            public static Footprint From(CityBuilding building, CityBounds bounds)
            {
                float x = building.position_norm[0] * bounds.width_units;
                float y = building.position_norm[1] * bounds.height_units;
                float width = building.footprint_units[0];
                float height = building.footprint_units[1];
                double radians = building.rotation_deg * Math.PI / 180d;
                float halfX = (float)(Math.Abs(Math.Cos(radians)) * width / 2f +
                                      Math.Abs(Math.Sin(radians)) * height / 2f);
                float halfY = (float)(Math.Abs(Math.Sin(radians)) * width / 2f +
                                      Math.Abs(Math.Cos(radians)) * height / 2f);
                return new Footprint
                {
                    MinX = x - halfX,
                    MaxX = x + halfX,
                    MinY = y - halfY,
                    MaxY = y + halfY,
                };
            }

            public bool IsInside(CityBounds bounds)
            {
                return MinX >= 0f && MaxX <= bounds.width_units &&
                       MinY >= 0f && MaxY <= bounds.height_units;
            }

            public bool Overlaps(Footprint other)
            {
                const float edgeContactToleranceUnits = 0.001f;
                return MinX < other.MaxX - edgeContactToleranceUnits &&
                       MaxX > other.MinX + edgeContactToleranceUnits &&
                       MinY < other.MaxY - edgeContactToleranceUnits &&
                       MaxY > other.MinY + edgeContactToleranceUnits;
            }

            public bool IntersectsRoad(CityVehicleRoad road, CityBounds bounds)
            {
                if (road?.points_norm == null || road.points_norm.Length < 2)
                {
                    return false;
                }

                float padding = Math.Max(0f, road.width_units * 0.5f) +
                                RoadPlacementClearanceUnits;
                float minX = MinX - padding;
                float maxX = MaxX + padding;
                float minY = MinY - padding;
                float maxY = MaxY + padding;
                for (int index = 1; index < road.points_norm.Length; index += 1)
                {
                    float[] first = road.points_norm[index - 1];
                    float[] second = road.points_norm[index];
                    if (!ValidPoint(first) || !ValidPoint(second))
                    {
                        continue;
                    }
                    float firstX = first[0] * bounds.width_units;
                    float firstY = first[1] * bounds.height_units;
                    float secondX = second[0] * bounds.width_units;
                    float secondY = second[1] * bounds.height_units;
                    if (SegmentIntersectsRectangle(
                            firstX,
                            firstY,
                            secondX,
                            secondY,
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

            private static bool ValidPoint(float[] point)
            {
                return point != null && point.Length == 2 &&
                       !float.IsNaN(point[0]) && !float.IsInfinity(point[0]) &&
                       !float.IsNaN(point[1]) && !float.IsInfinity(point[1]);
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
                if (PointInside(firstX, firstY, minX, maxX, minY, maxY) ||
                    PointInside(secondX, secondY, minX, maxX, minY, maxY))
                {
                    return true;
                }
                return SegmentsIntersect(firstX, firstY, secondX, secondY, minX, minY, maxX, minY) ||
                       SegmentsIntersect(firstX, firstY, secondX, secondY, maxX, minY, maxX, maxY) ||
                       SegmentsIntersect(firstX, firstY, secondX, secondY, maxX, maxY, minX, maxY) ||
                       SegmentsIntersect(firstX, firstY, secondX, secondY, minX, maxY, minX, minY);
            }

            private static bool PointInside(
                float x,
                float y,
                float minX,
                float maxX,
                float minY,
                float maxY)
            {
                return x >= minX && x <= maxX && y >= minY && y <= maxY;
            }

            private static bool SegmentsIntersect(
                float ax,
                float ay,
                float bx,
                float by,
                float cx,
                float cy,
                float dx,
                float dy)
            {
                const float tolerance = 0.0001f;
                float first = Cross(ax, ay, bx, by, cx, cy);
                float second = Cross(ax, ay, bx, by, dx, dy);
                float third = Cross(cx, cy, dx, dy, ax, ay);
                float fourth = Cross(cx, cy, dx, dy, bx, by);
                if (((first > tolerance && second < -tolerance) ||
                     (first < -tolerance && second > tolerance)) &&
                    ((third > tolerance && fourth < -tolerance) ||
                     (third < -tolerance && fourth > tolerance)))
                {
                    return true;
                }
                return Math.Abs(first) <= tolerance && OnSegment(ax, ay, bx, by, cx, cy) ||
                       Math.Abs(second) <= tolerance && OnSegment(ax, ay, bx, by, dx, dy) ||
                       Math.Abs(third) <= tolerance && OnSegment(cx, cy, dx, dy, ax, ay) ||
                       Math.Abs(fourth) <= tolerance && OnSegment(cx, cy, dx, dy, bx, by);
            }

            private static float Cross(
                float ax,
                float ay,
                float bx,
                float by,
                float px,
                float py)
            {
                return (bx - ax) * (py - ay) - (by - ay) * (px - ax);
            }

            private static bool OnSegment(
                float ax,
                float ay,
                float bx,
                float by,
                float px,
                float py)
            {
                const float tolerance = 0.0001f;
                return px >= Math.Min(ax, bx) - tolerance &&
                       px <= Math.Max(ax, bx) + tolerance &&
                       py >= Math.Min(ay, by) - tolerance &&
                       py <= Math.Max(ay, by) + tolerance;
            }
        }
    }
}

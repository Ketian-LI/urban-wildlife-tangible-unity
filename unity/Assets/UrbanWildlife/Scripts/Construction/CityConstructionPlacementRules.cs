using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Construction
{
    public static class CityConstructionPlacementRules
    {
        public static string ValidateBuilding(
            CityBuilding candidate,
            CityBounds bounds,
            IEnumerable<CityBuilding> existingBuildings,
            IEnumerable<CityBuilding> otherProposals)
        {
            if (candidate == null || bounds == null)
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
            return overlap == null
                ? string.Empty
                : $"Building footprint overlaps {overlap.id}.";
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
        }
    }
}

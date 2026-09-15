using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Networks
{
    public static class CityRoadCandidateGenerator
    {
        private const float LowImpactDetourNorm = 0.1f;

        public static CityRoadChoiceSet Generate(CityState city, CityBuilding building)
        {
            ValidateInputs(city, building);
            CityVehicleRoad[] connectionRoads = ConnectionRoads(city.vehicle_roads);
            NetworkConnection closest = ClosestConnection(
                building.position_norm,
                connectionRoads,
                city.bounds,
                false);
            NetworkConnection endpoint = ClosestConnection(
                building.position_norm,
                connectionRoads,
                city.bounds,
                true);

            float[][] directPoints = SmoothAccessRoute(
                building,
                closest.Point,
                city.bounds);
            float[][] existingNetworkPoints = SmoothAccessRoute(
                building,
                endpoint.Point,
                city.bounds);
            float[][] lowImpactPoints = LowImpactRoute(
                BuildingAccessPoint(building, closest.Point, city.bounds),
                closest.Point,
                city.green_patches,
                city.bounds);

            CityRoadCandidate direct = CreateCandidate(
                building,
                closest.RoadId,
                CityRoadRouteOption.Direct,
                directPoints,
                city,
                1f,
                "Shortest access; may cut through Woodland or Shrub habitat.");
            CityRoadCandidate existingNetwork = CreateCandidate(
                building,
                endpoint.RoadId,
                CityRoadRouteOption.ExistingNetwork,
                existingNetworkPoints,
                city,
                0.85f,
                "Reuses an existing network node; usually moderate length and pressure.");
            CityRoadCandidate lowImpact = CreateCandidate(
                building,
                closest.RoadId,
                CityRoadRouteOption.LowImpact,
                lowImpactPoints,
                city,
                0.65f,
                "Detours around high-value green patches; longer but reduces fragmentation pressure.");

            return new CityRoadChoiceSet
            {
                building_id = building.id,
                candidates = new[] { direct, existingNetwork, lowImpact },
            };
        }

        private static CityVehicleRoad[] ConnectionRoads(
            IEnumerable<CityVehicleRoad> roads)
        {
            CityVehicleRoad[] active = (roads ?? Array.Empty<CityVehicleRoad>())
                .Where(road => road?.points_norm != null && road.points_norm.Length >= 2 &&
                               road.construction_state != CityConstructionState.Demolishing)
                .ToArray();
            CityVehicleRoad[] main = active
                .Where(road => road.role == CityVehicleRoadRole.Main)
                .ToArray();
            if (main.Length > 0)
            {
                return main;
            }
            CityVehicleRoad[] established = active
                .Where(road => road.role != CityVehicleRoadRole.BuildingAccess)
                .ToArray();
            return established.Length > 0 ? established : active;
        }

        private static float[][] SmoothAccessRoute(
            CityBuilding building,
            float[] end,
            CityBounds bounds)
        {
            float[] start = BuildingAccessPoint(building, end, bounds);
            float startX = start[0] * bounds.width_units;
            float startY = start[1] * bounds.height_units;
            float endX = end[0] * bounds.width_units;
            float endY = end[1] * bounds.height_units;
            float deltaX = endX - startX;
            float deltaY = endY - startY;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (distance <= 0.001f)
            {
                return new[] { start, ClonePoint(end) };
            }

            float directionX = deltaX / distance;
            float directionY = deltaY / distance;
            float perpendicularX = -directionY;
            float perpendicularY = directionX;
            float handle = Math.Min(7f, distance * 0.32f);
            float bendSign = StableHash(building.id) % 2 == 0 ? 1f : -1f;
            float bend = Math.Min(2.4f, distance * 0.10f) * bendSign;
            float controlOneX = startX + directionX * handle + perpendicularX * bend;
            float controlOneY = startY + directionY * handle + perpendicularY * bend;
            float controlTwoX = endX - directionX * handle + perpendicularX * bend * 0.55f;
            float controlTwoY = endY - directionY * handle + perpendicularY * bend * 0.55f;

            const int segmentCount = 10;
            return Enumerable.Range(0, segmentCount + 1)
                .Select(index =>
                {
                    float t = index / (float)segmentCount;
                    float inverse = 1f - t;
                    float x = inverse * inverse * inverse * startX +
                              3f * inverse * inverse * t * controlOneX +
                              3f * inverse * t * t * controlTwoX +
                              t * t * t * endX;
                    float y = inverse * inverse * inverse * startY +
                              3f * inverse * inverse * t * controlOneY +
                              3f * inverse * t * t * controlTwoY +
                              t * t * t * endY;
                    return new[]
                    {
                        Clamp(x / bounds.width_units, 0f, 1f),
                        Clamp(y / bounds.height_units, 0f, 1f),
                    };
                })
                .ToArray();
        }

        private static float[] BuildingAccessPoint(
            CityBuilding building,
            float[] target,
            CityBounds bounds)
        {
            if (building.footprint_units == null || building.footprint_units.Length != 2)
            {
                return ClonePoint(building.position_norm);
            }
            float centreX = building.position_norm[0] * bounds.width_units;
            float centreY = building.position_norm[1] * bounds.height_units;
            float targetX = target[0] * bounds.width_units;
            float targetY = target[1] * bounds.height_units;
            float deltaX = targetX - centreX;
            float deltaY = targetY - centreY;
            float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (length <= 0.001f)
            {
                return ClonePoint(building.position_norm);
            }

            float directionX = deltaX / length;
            float directionY = deltaY / length;
            double radians = building.rotation_deg * Math.PI / 180d;
            float cosine = (float)Math.Cos(radians);
            float sine = (float)Math.Sin(radians);
            float localX = directionX * cosine + directionY * sine;
            float localY = -directionX * sine + directionY * cosine;
            float halfWidth = building.footprint_units[0] * 0.5f;
            float halfHeight = building.footprint_units[1] * 0.5f;
            float widthDistance = Math.Abs(localX) <= 0.0001f
                ? float.MaxValue
                : halfWidth / Math.Abs(localX);
            float heightDistance = Math.Abs(localY) <= 0.0001f
                ? float.MaxValue
                : halfHeight / Math.Abs(localY);
            float edgeDistance = Math.Min(widthDistance, heightDistance);
            return new[]
            {
                Clamp((centreX + directionX * edgeDistance) / bounds.width_units, 0f, 1f),
                Clamp((centreY + directionY * edgeDistance) / bounds.height_units, 0f, 1f),
            };
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                int hash = 17;
                foreach (char character in value ?? string.Empty)
                {
                    hash = hash * 31 + character;
                }
                return hash & int.MaxValue;
            }
        }

        public static CityPedestrianLink CreateBasicPedestrianAccess(
            CityState city,
            CityBuilding building)
        {
            ValidateInputs(city, building);
            NetworkConnection connection = ClosestConnection(
                building.position_norm,
                city.pedestrian_links,
                city.bounds,
                false);
            return new CityPedestrianLink
            {
                id = $"pedestrian-access-{building.id}",
                type = CityPedestrianLinkType.BasicBuildingAccess,
                construction_state = CityConstructionState.Proposed,
                source = CityNetworkSource.AutoGenerated,
                points_norm = new[]
                {
                    ClonePoint(building.position_norm),
                    ClonePoint(connection.Point),
                },
                width_units = 0.70f,
                step_free_accessible = true,
                connected_building_ids = new[] { building.id },
                connected_link_ids = new[] { connection.RoadId },
            };
        }

        private static CityRoadCandidate CreateCandidate(
            CityBuilding building,
            string connectionRoadId,
            CityRoadRouteOption option,
            float[][] points,
            CityState city,
            float trafficFactor,
            string summary)
        {
            return new CityRoadCandidate
            {
                id = $"road-candidate-{building.id}-{OptionSlug(option)}",
                building_id = building.id,
                connection_road_id = connectionRoadId,
                route_option = option,
                points_norm = ClonePoints(points),
                estimated_length_units = PolylineLength(points, city.bounds),
                estimated_green_impact_units = EstimateGreenImpact(
                    points,
                    city.green_patches,
                    city.bounds),
                estimated_traffic_pressure = Clamp01(building.vehicle_demand * trafficFactor),
                tradeoff_summary = summary,
            };
        }

        private static float[][] LowImpactRoute(
            float[] start,
            float[] end,
            IEnumerable<CityGreenPatch> patches,
            CityBounds bounds)
        {
            float midX = (start[0] + end[0]) * 0.5f;
            float midY = (start[1] + end[1]) * 0.5f;
            float deltaX = end[0] - start[0];
            float deltaY = end[1] - start[1];
            float length = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
            float perpendicularX = length <= 0.0001f ? 0f : -deltaY / length;
            float perpendicularY = length <= 0.0001f ? 1f : deltaX / length;

            float[][] first =
            {
                ClonePoint(start),
                new[]
                {
                    Clamp(midX + perpendicularX * LowImpactDetourNorm, 0.02f, 0.98f),
                    Clamp(midY + perpendicularY * LowImpactDetourNorm, 0.02f, 0.98f),
                },
                ClonePoint(end),
            };
            float[][] second =
            {
                ClonePoint(start),
                new[]
                {
                    Clamp(midX - perpendicularX * LowImpactDetourNorm, 0.02f, 0.98f),
                    Clamp(midY - perpendicularY * LowImpactDetourNorm, 0.02f, 0.98f),
                },
                ClonePoint(end),
            };
            float firstImpact = EstimateGreenImpact(first, patches, bounds);
            float secondImpact = EstimateGreenImpact(second, patches, bounds);
            if (Math.Abs(firstImpact - secondImpact) > 0.001f)
            {
                return firstImpact < secondImpact ? first : second;
            }
            return PolylineLength(first, bounds) <= PolylineLength(second, bounds)
                ? first
                : second;
        }

        private static NetworkConnection ClosestConnection(
            float[] source,
            IEnumerable<CityVehicleRoad> roads,
            CityBounds bounds,
            bool endpointsOnly)
        {
            IEnumerable<NetworkPolyline> polylines = (roads ?? Array.Empty<CityVehicleRoad>())
                .Where(road => road?.points_norm != null && road.points_norm.Length >= 2)
                .Select(road => new NetworkPolyline(road.id, road.points_norm));
            return ClosestConnection(source, polylines, bounds, endpointsOnly);
        }

        private static NetworkConnection ClosestConnection(
            float[] source,
            IEnumerable<CityPedestrianLink> links,
            CityBounds bounds,
            bool endpointsOnly)
        {
            IEnumerable<NetworkPolyline> polylines = (links ?? Array.Empty<CityPedestrianLink>())
                .Where(link => link?.points_norm != null && link.points_norm.Length >= 2)
                .Select(link => new NetworkPolyline(link.id, link.points_norm));
            return ClosestConnection(source, polylines, bounds, endpointsOnly);
        }

        private static NetworkConnection ClosestConnection(
            float[] source,
            IEnumerable<NetworkPolyline> polylines,
            CityBounds bounds,
            bool endpointsOnly)
        {
            NetworkConnection best = null;
            foreach (NetworkPolyline polyline in polylines)
            {
                if (endpointsOnly)
                {
                    float[][] endpoints =
                    {
                        polyline.Points[0],
                        polyline.Points[polyline.Points.Length - 1],
                    };
                    foreach (float[] endpoint in endpoints)
                    {
                        best = Better(best, polyline.Id, endpoint, source, bounds);
                    }
                    continue;
                }

                for (int index = 1; index < polyline.Points.Length; index += 1)
                {
                    float[] projected = ClosestPointOnSegment(
                        source,
                        polyline.Points[index - 1],
                        polyline.Points[index],
                        bounds);
                    best = Better(best, polyline.Id, projected, source, bounds);
                }
            }
            if (best == null)
            {
                throw new InvalidOperationException("An existing network is required before access routes can be generated.");
            }
            return best;
        }

        private static NetworkConnection Better(
            NetworkConnection current,
            string roadId,
            float[] point,
            float[] source,
            CityBounds bounds)
        {
            float distance = Distance(source, point, bounds);
            if (current == null || distance < current.DistanceUnits)
            {
                return new NetworkConnection(roadId, ClonePoint(point), distance);
            }
            return current;
        }

        private static float[] ClosestPointOnSegment(
            float[] point,
            float[] first,
            float[] second,
            CityBounds bounds)
        {
            float px = point[0] * bounds.width_units;
            float py = point[1] * bounds.height_units;
            float ax = first[0] * bounds.width_units;
            float ay = first[1] * bounds.height_units;
            float bx = second[0] * bounds.width_units;
            float by = second[1] * bounds.height_units;
            float dx = bx - ax;
            float dy = by - ay;
            float denominator = dx * dx + dy * dy;
            float t = denominator <= 0.0001f
                ? 0f
                : Clamp(((px - ax) * dx + (py - ay) * dy) / denominator, 0f, 1f);
            return new[]
            {
                (ax + dx * t) / bounds.width_units,
                (ay + dy * t) / bounds.height_units,
            };
        }

        private static float EstimateGreenImpact(
            float[][] points,
            IEnumerable<CityGreenPatch> patches,
            CityBounds bounds)
        {
            CityGreenPatch[] sensitivePatches = (patches ?? Array.Empty<CityGreenPatch>())
                .Where(patch => patch != null &&
                                (patch.type == CityGreenPatchType.Woodland ||
                                 patch.type == CityGreenPatchType.ShrubGarden))
                .ToArray();
            float impact = 0f;
            for (int index = 1; index < points.Length; index += 1)
            {
                float segmentLength = Distance(points[index - 1], points[index], bounds);
                const int sampleCount = 16;
                int inside = 0;
                for (int sample = 0; sample <= sampleCount; sample += 1)
                {
                    float t = sample / (float)sampleCount;
                    float[] point =
                    {
                        Lerp(points[index - 1][0], points[index][0], t),
                        Lerp(points[index - 1][1], points[index][1], t),
                    };
                    if (sensitivePatches.Any(patch => PointInPolygon(point, patch.polygon_norm)))
                    {
                        inside += 1;
                    }
                }
                impact += segmentLength * inside / (sampleCount + 1f);
            }
            return impact;
        }

        private static bool PointInPolygon(float[] point, float[][] polygon)
        {
            if (polygon == null || polygon.Length < 3)
            {
                return false;
            }
            bool inside = false;
            int previous = polygon.Length - 1;
            for (int current = 0; current < polygon.Length; current += 1)
            {
                float[] a = polygon[current];
                float[] b = polygon[previous];
                bool crosses = (a[1] > point[1]) != (b[1] > point[1]) &&
                               point[0] < (b[0] - a[0]) * (point[1] - a[1]) /
                               (b[1] - a[1]) + a[0];
                if (crosses)
                {
                    inside = !inside;
                }
                previous = current;
            }
            return inside;
        }

        private static float PolylineLength(float[][] points, CityBounds bounds)
        {
            float length = 0f;
            for (int index = 1; index < points.Length; index += 1)
            {
                length += Distance(points[index - 1], points[index], bounds);
            }
            return length;
        }

        private static float Distance(float[] first, float[] second, CityBounds bounds)
        {
            float x = (first[0] - second[0]) * bounds.width_units;
            float y = (first[1] - second[1]) * bounds.height_units;
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static void ValidateInputs(CityState city, CityBuilding building)
        {
            if (city?.bounds == null || building?.position_norm == null ||
                building.position_norm.Length != 2)
            {
                throw new ArgumentException("A valid city and building are required for network planning.");
            }
        }

        private static string OptionSlug(CityRoadRouteOption option)
        {
            switch (option)
            {
                case CityRoadRouteOption.Direct:
                    return "direct";
                case CityRoadRouteOption.ExistingNetwork:
                    return "existing-network";
                case CityRoadRouteOption.LowImpact:
                    return "low-impact";
                default:
                    throw new ArgumentOutOfRangeException(nameof(option), option, "Unsupported road option.");
            }
        }

        private static float[][] ClonePoints(IEnumerable<float[]> points)
        {
            return points.Select(ClonePoint).ToArray();
        }

        private static float[] ClonePoint(float[] point)
        {
            return (float[])point.Clone();
        }

        private static float Lerp(float first, float second, float t)
        {
            return first + (second - first) * t;
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private sealed class NetworkPolyline
        {
            public NetworkPolyline(string id, float[][] points)
            {
                Id = id;
                Points = points;
            }

            public string Id { get; }
            public float[][] Points { get; }
        }

        private sealed class NetworkConnection
        {
            public NetworkConnection(string roadId, float[] point, float distanceUnits)
            {
                RoadId = roadId;
                Point = point;
                DistanceUnits = distanceUnits;
            }

            public string RoadId { get; }
            public float[] Point { get; }
            public float DistanceUnits { get; }
        }
    }
}

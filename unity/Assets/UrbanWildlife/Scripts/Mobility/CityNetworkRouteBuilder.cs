using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Mobility
{
    public static class CityNetworkRouteBuilder
    {
        public static float[][] Build(
            CityState city,
            CityBuilding origin,
            CityBuilding destination,
            CityTravelMode mode)
        {
            if (city?.bounds == null || origin == null || destination == null)
            {
                throw new ArgumentException("A city, origin and destination are required.");
            }

            Dictionary<string, NetworkLine> network = mode == CityTravelMode.Drive
                ? VehicleNetwork(city)
                : PedestrianNetwork(city);
            string[] originReferences = mode == CityTravelMode.Drive
                ? origin.vehicle_road_ids
                : origin.pedestrian_link_ids;
            string[] destinationReferences = mode == CityTravelMode.Drive
                ? destination.vehicle_road_ids
                : destination.pedestrian_link_ids;
            NetworkLine originLine = FirstReferenced(network, originReferences, origin.id, mode);
            NetworkLine destinationLine = FirstReferenced(
                network,
                destinationReferences,
                destination.id,
                mode);

            List<float[]> route = new List<float[]> { ClonePoint(origin.position_norm) };
            AppendOriented(route, originLine.Points, city.bounds);

            string originConnectorId = originLine.ConnectedLineIds.FirstOrDefault() ?? originLine.Id;
            string destinationConnectorId = destinationLine.ConnectedLineIds.FirstOrDefault() ??
                                            destinationLine.Id;
            if (originConnectorId == destinationConnectorId &&
                network.TryGetValue(originConnectorId, out NetworkLine sharedConnector))
            {
                float[] destinationJoin = EndpointNearestPolyline(
                    destinationLine.Points,
                    sharedConnector.Points,
                    city.bounds);
                AppendBetweenNearestProjections(
                    route,
                    sharedConnector.Points,
                    route[route.Count - 1],
                    destinationJoin,
                    city.bounds);
            }
            else
            {
                if (network.TryGetValue(originConnectorId, out NetworkLine originConnector))
                {
                    AppendOriented(route, originConnector.Points, city.bounds);
                }
                if (destinationConnectorId != originConnectorId &&
                    network.TryGetValue(destinationConnectorId, out NetworkLine destinationConnector))
                {
                    AppendOriented(route, destinationConnector.Points, city.bounds);
                }
            }

            bool destinationLineAlreadyAdded = destinationLine.Id == originLine.Id ||
                                               destinationLine.Id == originConnectorId ||
                                               destinationLine.Id == destinationConnectorId;
            if (!destinationLineAlreadyAdded)
            {
                AppendOriented(route, destinationLine.Points, city.bounds);
            }
            AppendDistinct(route, destination.position_norm);
            float[][] result = route.ToArray();
            if (result.Length < 2)
            {
                throw new InvalidOperationException("Network route contains fewer than two points.");
            }
            return result;
        }

        public static float[][] BuildExternalDrive(
            CityState city,
            CityBuilding origin,
            float outsideMarginNorm = 0.06f)
        {
            return BuildExternal(city, origin, CityTravelMode.Drive, outsideMarginNorm);
        }

        public static float[][] BuildExternalWalk(
            CityState city,
            CityBuilding origin,
            float outsideMarginNorm = 0.035f)
        {
            return BuildExternal(city, origin, CityTravelMode.Walk, outsideMarginNorm);
        }

        private static float[][] BuildExternal(
            CityState city,
            CityBuilding origin,
            CityTravelMode mode,
            float outsideMarginNorm)
        {
            if (city?.bounds == null || origin == null)
            {
                throw new ArgumentException("A city and origin are required.");
            }
            if (outsideMarginNorm <= 0f || outsideMarginNorm > 0.25f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outsideMarginNorm),
                    "The outside gateway margin must stay between 0 and 0.25.");
            }

            Dictionary<string, NetworkLine> network = mode == CityTravelMode.Drive
                ? VehicleNetwork(city)
                : PedestrianNetwork(city);
            string[] references = mode == CityTravelMode.Drive
                ? origin.vehicle_road_ids
                : origin.pedestrian_link_ids;
            NetworkLine current = FirstReferenced(
                network,
                references,
                origin.id,
                mode);
            List<float[]> route = new List<float[]> { ClonePoint(origin.position_norm) };
            AppendOriented(route, current.Points, city.bounds);

            HashSet<string> visited = new HashSet<string> { current.Id };
            for (int depth = 0; depth < network.Count && !TouchesMapEdge(current.Points); depth += 1)
            {
                NetworkLine next = current.ConnectedLineIds
                    .Where(id => !visited.Contains(id) && network.ContainsKey(id))
                    .Select(id => network[id])
                    .OrderBy(line => BoundaryDistance(line.Points))
                    .ThenBy(line => DistanceToNearestEndpoint(
                        route[route.Count - 1],
                        line.Points,
                        city.bounds))
                    .FirstOrDefault();
                if (next == null)
                {
                    break;
                }
                if (TouchesMapEdge(next.Points))
                {
                    AppendFromNearestProjectionToBoundary(route, next.Points, city.bounds);
                }
                else
                {
                    AppendOriented(route, next.Points, city.bounds);
                }
                current = next;
                visited.Add(current.Id);
            }

            AppendDistinct(
                route,
                OutsideGatewayFrom(route[route.Count - 1], outsideMarginNorm));
            return route.ToArray();
        }

        public static float Length(float[][] points, CityBounds bounds)
        {
            if (points == null || points.Length < 2 || bounds == null)
            {
                return 0f;
            }
            float length = 0f;
            for (int index = 1; index < points.Length; index += 1)
            {
                length += Distance(points[index - 1], points[index], bounds);
            }
            return length;
        }

        private static Dictionary<string, NetworkLine> VehicleNetwork(CityState city)
        {
            return (city.vehicle_roads ?? Array.Empty<CityVehicleRoad>())
                .Where(road => road?.construction_state == CityConstructionState.Existing &&
                               road.points_norm != null && road.points_norm.Length >= 2)
                .ToDictionary(
                    road => road.id,
                    road => new NetworkLine(road.id, road.points_norm, road.connected_road_ids));
        }

        private static Dictionary<string, NetworkLine> PedestrianNetwork(CityState city)
        {
            return (city.pedestrian_links ?? Array.Empty<CityPedestrianLink>())
                .Where(link => link?.construction_state == CityConstructionState.Existing &&
                               link.points_norm != null && link.points_norm.Length >= 2)
                .ToDictionary(
                    link => link.id,
                    link => new NetworkLine(link.id, link.points_norm, link.connected_link_ids));
        }

        private static NetworkLine FirstReferenced(
            IReadOnlyDictionary<string, NetworkLine> network,
            IEnumerable<string> references,
            string buildingId,
            CityTravelMode mode)
        {
            foreach (string reference in references ?? Array.Empty<string>())
            {
                if (network.TryGetValue(reference, out NetworkLine line))
                {
                    return line;
                }
            }
            throw new InvalidOperationException(
                $"Building {buildingId} has no valid {mode} network reference.");
        }

        private static void AppendOriented(
            List<float[]> route,
            float[][] points,
            CityBounds bounds)
        {
            float[] current = route[route.Count - 1];
            bool reverse = Distance(current, points[points.Length - 1], bounds) <
                           Distance(current, points[0], bounds);
            if (reverse)
            {
                for (int index = points.Length - 1; index >= 0; index -= 1)
                {
                    AppendDistinct(route, points[index]);
                }
            }
            else
            {
                foreach (float[] point in points)
                {
                    AppendDistinct(route, point);
                }
            }
        }

        private static void AppendDistinct(List<float[]> route, float[] point)
        {
            if (route.Count > 0)
            {
                float[] previous = route[route.Count - 1];
                if (Math.Abs(previous[0] - point[0]) <= 0.0001f &&
                    Math.Abs(previous[1] - point[1]) <= 0.0001f)
                {
                    return;
                }
            }
            route.Add(ClonePoint(point));
        }

        private static void AppendFromNearestProjectionToBoundary(
            List<float[]> route,
            float[][] points,
            CityBounds bounds)
        {
            float[] current = route[route.Count - 1];
            PolylineProjection projection = ProjectPointOntoPolyline(current, points, bounds);
            AppendDistinct(route, projection.Point);

            float distanceToStart = DistanceAlongPolylineToStart(
                points,
                projection.SegmentIndex,
                projection.Point,
                bounds);
            float distanceToEnd = DistanceAlongPolylineToEnd(
                points,
                projection.SegmentIndex,
                projection.Point,
                bounds);
            bool startTouchesBoundary = PointTouchesMapEdge(points[0]);
            bool endTouchesBoundary = PointTouchesMapEdge(points[points.Length - 1]);
            bool towardStart = startTouchesBoundary &&
                               (!endTouchesBoundary || distanceToStart <= distanceToEnd);
            if (towardStart)
            {
                for (int index = projection.SegmentIndex; index >= 0; index -= 1)
                {
                    AppendDistinct(route, points[index]);
                }
                return;
            }
            for (int index = projection.SegmentIndex + 1; index < points.Length; index += 1)
            {
                AppendDistinct(route, points[index]);
            }
        }

        private static void AppendBetweenNearestProjections(
            List<float[]> route,
            float[][] points,
            float[] startReference,
            float[] endReference,
            CityBounds bounds)
        {
            PolylineProjection start = ProjectPointOntoPolyline(startReference, points, bounds);
            PolylineProjection end = ProjectPointOntoPolyline(endReference, points, bounds);
            AppendDistinct(route, start.Point);
            float startPosition = start.SegmentIndex + start.SegmentT;
            float endPosition = end.SegmentIndex + end.SegmentT;
            if (startPosition <= endPosition)
            {
                for (int index = start.SegmentIndex + 1; index <= end.SegmentIndex; index += 1)
                {
                    AppendDistinct(route, points[index]);
                }
            }
            else
            {
                for (int index = start.SegmentIndex; index > end.SegmentIndex; index -= 1)
                {
                    AppendDistinct(route, points[index]);
                }
            }
            AppendDistinct(route, end.Point);
        }

        private static float[] EndpointNearestPolyline(
            float[][] candidateLine,
            float[][] targetPolyline,
            CityBounds bounds)
        {
            float[] first = candidateLine[0];
            float[] last = candidateLine[candidateLine.Length - 1];
            return ProjectPointOntoPolyline(first, targetPolyline, bounds).Distance <=
                   ProjectPointOntoPolyline(last, targetPolyline, bounds).Distance
                ? first
                : last;
        }

        private static PolylineProjection ProjectPointOntoPolyline(
            float[] point,
            float[][] polyline,
            CityBounds bounds)
        {
            PolylineProjection closest = new PolylineProjection
            {
                SegmentIndex = 0,
                SegmentT = 0f,
                Point = ClonePoint(polyline[0]),
                Distance = float.PositiveInfinity,
            };
            for (int index = 0; index < polyline.Length - 1; index += 1)
            {
                float[] start = polyline[index];
                float[] end = polyline[index + 1];
                float dx = (end[0] - start[0]) * bounds.width_units;
                float dy = (end[1] - start[1]) * bounds.height_units;
                float px = (point[0] - start[0]) * bounds.width_units;
                float py = (point[1] - start[1]) * bounds.height_units;
                float denominator = dx * dx + dy * dy;
                float t = denominator <= 0.000001f
                    ? 0f
                    : Math.Max(0f, Math.Min(1f, (px * dx + py * dy) / denominator));
                float[] projected =
                {
                    start[0] + (end[0] - start[0]) * t,
                    start[1] + (end[1] - start[1]) * t,
                };
                float distance = Distance(point, projected, bounds);
                if (distance < closest.Distance)
                {
                    closest.SegmentIndex = index;
                    closest.SegmentT = t;
                    closest.Point = projected;
                    closest.Distance = distance;
                }
            }
            return closest;
        }

        private static float DistanceAlongPolylineToStart(
            float[][] points,
            int segmentIndex,
            float[] projection,
            CityBounds bounds)
        {
            float distance = Distance(projection, points[segmentIndex], bounds);
            for (int index = segmentIndex; index > 0; index -= 1)
            {
                distance += Distance(points[index], points[index - 1], bounds);
            }
            return distance;
        }

        private static float DistanceAlongPolylineToEnd(
            float[][] points,
            int segmentIndex,
            float[] projection,
            CityBounds bounds)
        {
            float distance = Distance(projection, points[segmentIndex + 1], bounds);
            for (int index = segmentIndex + 1; index < points.Length - 1; index += 1)
            {
                distance += Distance(points[index], points[index + 1], bounds);
            }
            return distance;
        }

        private static bool TouchesMapEdge(float[][] points)
        {
            return BoundaryDistance(points) <= 0.055f;
        }

        private static bool PointTouchesMapEdge(float[] point)
        {
            return Math.Min(
                Math.Min(point[0], 1f - point[0]),
                Math.Min(point[1], 1f - point[1])) <= 0.055f;
        }

        private static float BoundaryDistance(float[][] points)
        {
            return points
                .Where(point => point != null && point.Length >= 2)
                .Select(point => Math.Min(
                    Math.Min(point[0], 1f - point[0]),
                    Math.Min(point[1], 1f - point[1])))
                .DefaultIfEmpty(float.PositiveInfinity)
                .Min();
        }

        private static float DistanceToNearestEndpoint(
            float[] point,
            float[][] line,
            CityBounds bounds)
        {
            return Math.Min(
                Distance(point, line[0], bounds),
                Distance(point, line[line.Length - 1], bounds));
        }

        private static float[] OutsideGatewayFrom(float[] point, float margin)
        {
            float left = point[0];
            float right = 1f - point[0];
            float top = point[1];
            float bottom = 1f - point[1];
            float closest = Math.Min(Math.Min(left, right), Math.Min(top, bottom));
            if (closest == left)
            {
                return new[] { -margin, point[1] };
            }
            if (closest == right)
            {
                return new[] { 1f + margin, point[1] };
            }
            if (closest == top)
            {
                return new[] { point[0], -margin };
            }
            return new[] { point[0], 1f + margin };
        }

        private static float Distance(float[] first, float[] second, CityBounds bounds)
        {
            float x = (first[0] - second[0]) * bounds.width_units;
            float y = (first[1] - second[1]) * bounds.height_units;
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static float[] ClonePoint(float[] point)
        {
            return (float[])point.Clone();
        }

        private sealed class NetworkLine
        {
            public NetworkLine(string id, float[][] points, IEnumerable<string> connectedLineIds)
            {
                Id = id;
                Points = points.Select(ClonePoint).ToArray();
                ConnectedLineIds = (connectedLineIds ?? Array.Empty<string>()).ToArray();
            }

            public string Id { get; }
            public float[][] Points { get; }
            public string[] ConnectedLineIds { get; }
        }

        private struct PolylineProjection
        {
            public int SegmentIndex;
            public float SegmentT;
            public float[] Point;
            public float Distance;
        }
    }
}

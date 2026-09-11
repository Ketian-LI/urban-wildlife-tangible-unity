using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Animals
{
    public enum AnimalMovementObstacleKind
    {
        Pond,
        Woodland,
    }

    public readonly struct AnimalMovementObstacle
    {
        public AnimalMovementObstacle(
            AnimalMovementObstacleKind kind,
            Vector2 centre,
            Vector2 halfExtents)
        {
            Kind = kind;
            Centre = centre;
            HalfExtents = new Vector2(
                Mathf.Max(0.01f, halfExtents.x),
                Mathf.Max(0.01f, halfExtents.y));
        }

        public AnimalMovementObstacleKind Kind { get; }
        public Vector2 Centre { get; }
        public Vector2 HalfExtents { get; }

        public bool Contains(Vector2 point, float clearance)
        {
            Vector2 radii = ExpandedRadii(clearance);
            Vector2 local = point - Centre;
            float normalizedDistance =
                local.x * local.x / (radii.x * radii.x) +
                local.y * local.y / (radii.y * radii.y);
            return normalizedDistance < 0.999f;
        }

        public Vector2 ExpandedRadii(float clearance)
        {
            float safeClearance = Mathf.Max(0f, clearance);
            return HalfExtents + Vector2.one * safeClearance;
        }
    }

    public static class AnimalObstacleAvoidance
    {
        public const float WoodlandDisplayWidth = 1.55f;
        public const float WoodlandDisplayHeight = 1.03f;
        public const float PondArtworkCollisionScale = 1.38f;
        private const float RouteNodeExpansion = 1.13f;
        private const int RouteNodesPerObstacle = 12;

        public static AnimalMovementObstacle[] BuildObstacles(LayoutPacket packet, P0Scenario scenario)
        {
            if (packet?.tokens == null || scenario?.board == null)
            {
                return Array.Empty<AnimalMovementObstacle>();
            }

            List<AnimalMovementObstacle> obstacles = new List<AnimalMovementObstacle>();
            ScenarioRegion pond = scenario.fixed_regions?.pond;
            if (pond?.center_norm != null && pond.center_norm.Length >= 2 &&
                pond.bounding_size_cm != null && pond.bounding_size_cm.Length >= 2)
            {
                float scale = scenario.animal_simulation?.unity_units_per_cm ?? 0.1f;
                obstacles.Add(new AnimalMovementObstacle(
                    AnimalMovementObstacleKind.Pond,
                    NormalizedToLocal(
                        pond.center_norm[0],
                        pond.center_norm[1],
                        scenario,
                        scale),
                    new Vector2(
                        pond.bounding_size_cm[0] * scale * 0.5f * PondArtworkCollisionScale,
                        pond.bounding_size_cm[1] * scale * 0.5f * PondArtworkCollisionScale)));
            }

            float layoutScale = scenario.animal_simulation?.unity_units_per_cm ?? 0.1f;
            obstacles.AddRange(packet.tokens
                .Where(token => token.type == "woodland")
                .Select(token => new AnimalMovementObstacle(
                    AnimalMovementObstacleKind.Woodland,
                    NormalizedToLocal(token.x_norm, token.y_norm, scenario, layoutScale),
                    new Vector2(WoodlandDisplayWidth * 0.5f, WoodlandDisplayHeight * 0.5f))));
            return obstacles.ToArray();
        }

        public static float ClearanceFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return 0.10f;
                case AnimalSpecies.Squirrel:
                    return 0.15f;
                default:
                    return 0.26f;
            }
        }

        public static Vector2 WoodlandAccessPoint(
            Vector2 woodlandCentre,
            Vector2 approachFrom,
            AnimalSpecies species)
        {
            AnimalMovementObstacle woodland = new AnimalMovementObstacle(
                AnimalMovementObstacleKind.Woodland,
                woodlandCentre,
                new Vector2(WoodlandDisplayWidth * 0.5f, WoodlandDisplayHeight * 0.5f));
            return ProjectOutside(approachFrom, woodlandCentre, woodland, ClearanceFor(species));
        }

        public static Vector2 HabitatAccessPoint(
            Vector2 woodlandCentre,
            Vector2 approachFrom,
            AnimalSpecies species,
            AnimalMovementObstacle[] obstacles,
            Vector2 boardHalfExtents)
        {
            float clearance = ClearanceFor(species);
            AnimalMovementObstacle woodland = new AnimalMovementObstacle(
                AnimalMovementObstacleKind.Woodland,
                woodlandCentre,
                new Vector2(WoodlandDisplayWidth * 0.5f, WoodlandDisplayHeight * 0.5f));
            Vector2 radii = woodland.ExpandedRadii(clearance) * 1.035f;
            Vector2 best = WoodlandAccessPoint(woodlandCentre, approachFrom, species);
            float bestScore = float.PositiveInfinity;
            const int candidateCount = 24;
            for (int index = 0; index < candidateCount; index += 1)
            {
                float angle = index * Mathf.PI * 2f / candidateCount;
                Vector2 candidate = woodlandCentre + new Vector2(
                    Mathf.Cos(angle) * radii.x,
                    Mathf.Sin(angle) * radii.y);
                if (!InsideBoard(candidate, boardHalfExtents))
                {
                    continue;
                }

                bool blockedByOtherObstacle = obstacles != null && Array.Exists(
                    obstacles,
                    obstacle =>
                        !(obstacle.Kind == AnimalMovementObstacleKind.Woodland &&
                          Vector2.Distance(obstacle.Centre, woodlandCentre) < 0.01f) &&
                        obstacle.Contains(candidate, clearance));
                if (blockedByOtherObstacle)
                {
                    continue;
                }

                float score = Vector2.Distance(candidate, approachFrom) + index * 0.0001f;
                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }
            return ClampToBoard(best, boardHalfExtents);
        }

        public static Vector2 ResolveAccessibleTarget(
            Vector2 current,
            Vector2 requestedTarget,
            AnimalMovementObstacle[] obstacles,
            float clearance,
            Vector2 boardHalfExtents)
        {
            Vector2 target = ClampToBoard(requestedTarget, boardHalfExtents);
            if (obstacles == null)
            {
                return target;
            }

            foreach (AnimalMovementObstacle obstacle in obstacles)
            {
                if (obstacle.Contains(target, clearance))
                {
                    target = ProjectOutside(current, target, obstacle, clearance);
                    target = ClampToBoard(target, boardHalfExtents);
                }
            }
            return target;
        }

        public static Vector2 NextWaypoint(
            Vector2 current,
            Vector2 target,
            AnimalMovementObstacle[] obstacles,
            float clearance,
            Vector2 boardHalfExtents)
        {
            if (obstacles == null || obstacles.Length == 0 ||
                SegmentIsClear(current, target, obstacles, clearance))
            {
                return ClampToBoard(target, boardHalfExtents);
            }

            List<Vector2> nodes = new List<Vector2> { current, target };
            foreach (AnimalMovementObstacle obstacle in obstacles)
            {
                Vector2 radii = obstacle.ExpandedRadii(clearance) * RouteNodeExpansion;
                for (int index = 0; index < RouteNodesPerObstacle; index += 1)
                {
                    float angle = index * Mathf.PI * 2f / RouteNodesPerObstacle;
                    Vector2 node = obstacle.Centre + new Vector2(
                        Mathf.Cos(angle) * radii.x,
                        Mathf.Sin(angle) * radii.y);
                    if (InsideBoard(node, boardHalfExtents))
                    {
                        nodes.Add(node);
                    }
                }
            }

            int count = nodes.Count;
            float[] distance = Enumerable.Repeat(float.PositiveInfinity, count).ToArray();
            int[] previous = Enumerable.Repeat(-1, count).ToArray();
            bool[] visited = new bool[count];
            distance[0] = 0f;
            for (int iteration = 0; iteration < count; iteration += 1)
            {
                int currentIndex = -1;
                float shortest = float.PositiveInfinity;
                for (int index = 0; index < count; index += 1)
                {
                    if (!visited[index] && distance[index] < shortest)
                    {
                        shortest = distance[index];
                        currentIndex = index;
                    }
                }
                if (currentIndex < 0 || currentIndex == 1)
                {
                    break;
                }

                visited[currentIndex] = true;
                for (int candidate = 0; candidate < count; candidate += 1)
                {
                    if (candidate == currentIndex || visited[candidate] ||
                        !SegmentIsClear(nodes[currentIndex], nodes[candidate], obstacles, clearance))
                    {
                        continue;
                    }
                    float candidateDistance = distance[currentIndex] +
                        Vector2.Distance(nodes[currentIndex], nodes[candidate]);
                    if (candidateDistance + 0.0001f < distance[candidate])
                    {
                        distance[candidate] = candidateDistance;
                        previous[candidate] = currentIndex;
                    }
                }
            }

            if (previous[1] < 0)
            {
                return FallbackWaypoint(current, target, obstacles, clearance, boardHalfExtents);
            }

            int nextIndex = 1;
            while (previous[nextIndex] > 0)
            {
                nextIndex = previous[nextIndex];
            }
            return nodes[nextIndex];
        }

        public static bool SegmentIsClear(
            Vector2 start,
            Vector2 end,
            AnimalMovementObstacle[] obstacles,
            float clearance)
        {
            if (obstacles == null)
            {
                return true;
            }

            foreach (AnimalMovementObstacle obstacle in obstacles)
            {
                Vector2 radii = obstacle.ExpandedRadii(clearance);
                Vector2 normalizedStart = new Vector2(
                    (start.x - obstacle.Centre.x) / radii.x,
                    (start.y - obstacle.Centre.y) / radii.y);
                Vector2 normalizedEnd = new Vector2(
                    (end.x - obstacle.Centre.x) / radii.x,
                    (end.y - obstacle.Centre.y) / radii.y);
                Vector2 segment = normalizedEnd - normalizedStart;
                float denominator = segment.sqrMagnitude;
                float t = denominator <= 0.000001f
                    ? 0f
                    : Mathf.Clamp01(-Vector2.Dot(normalizedStart, segment) / denominator);
                Vector2 closest = normalizedStart + segment * t;
                if (closest.sqrMagnitude < 0.999f)
                {
                    return false;
                }
            }
            return true;
        }

        private static Vector2 FallbackWaypoint(
            Vector2 current,
            Vector2 target,
            AnimalMovementObstacle[] obstacles,
            float clearance,
            Vector2 boardHalfExtents)
        {
            foreach (AnimalMovementObstacle obstacle in obstacles)
            {
                if (SegmentIsClear(current, target, new[] { obstacle }, clearance))
                {
                    continue;
                }
                Vector2 radii = obstacle.ExpandedRadii(clearance) * RouteNodeExpansion;
                Vector2 above = obstacle.Centre + new Vector2(0f, radii.y);
                Vector2 below = obstacle.Centre - new Vector2(0f, radii.y);
                Vector2 chosen = Vector2.Distance(current, above) + Vector2.Distance(above, target) <=
                    Vector2.Distance(current, below) + Vector2.Distance(below, target)
                    ? above
                    : below;
                return ClampToBoard(chosen, boardHalfExtents);
            }
            return ClampToBoard(target, boardHalfExtents);
        }

        private static Vector2 ProjectOutside(
            Vector2 approachFrom,
            Vector2 point,
            AnimalMovementObstacle obstacle,
            float clearance)
        {
            Vector2 direction = approachFrom - obstacle.Centre;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = point - obstacle.Centre;
            }
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.right;
            }
            direction.Normalize();
            Vector2 radii = obstacle.ExpandedRadii(clearance);
            float inverseScale = Mathf.Sqrt(
                direction.x * direction.x / (radii.x * radii.x) +
                direction.y * direction.y / (radii.y * radii.y));
            float distance = inverseScale <= 0.0001f ? radii.x : 1f / inverseScale;
            return obstacle.Centre + direction * distance * 1.025f;
        }

        private static Vector2 ClampToBoard(Vector2 point, Vector2 boardHalfExtents)
        {
            if (boardHalfExtents.x <= 0f || boardHalfExtents.y <= 0f)
            {
                return point;
            }
            return new Vector2(
                Mathf.Clamp(point.x, -boardHalfExtents.x, boardHalfExtents.x),
                Mathf.Clamp(point.y, -boardHalfExtents.y, boardHalfExtents.y));
        }

        private static bool InsideBoard(Vector2 point, Vector2 boardHalfExtents)
        {
            return boardHalfExtents.x <= 0f || boardHalfExtents.y <= 0f ||
                (Mathf.Abs(point.x) <= boardHalfExtents.x && Mathf.Abs(point.y) <= boardHalfExtents.y);
        }

        private static Vector2 NormalizedToLocal(
            float xNorm,
            float yNorm,
            P0Scenario scenario,
            float scale)
        {
            return new Vector2(
                (xNorm - 0.5f) * scenario.board.width_cm * scale,
                (0.5f - yNorm) * scenario.board.height_cm * scale);
        }
    }
}

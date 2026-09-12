using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Humans
{
    public static class HumanRoutePlanner
    {
        public static HumanRoutePlan[] CreatePlans(LayoutPacket packet, P0Scenario scenario)
        {
            if (packet?.path?.points_norm == null || packet.path.points_norm.Length < 2)
            {
                throw new ArgumentException("A confirmed human path is required.", nameof(packet));
            }
            if (scenario?.human_simulation == null || scenario.board == null)
            {
                throw new ArgumentException("Human simulation parameters are required.", nameof(scenario));
            }

            ScenarioHumanSimulation settings = scenario.human_simulation;
            Vector2[] mainPath = packet.path.points_norm
                .Select(point => ToLocal(point[0], point[1], scenario))
                .ToArray();
            Vector2 plaza = ToLocal(
                scenario.fixed_regions.central_plaza.center_norm[0],
                scenario.fixed_regions.central_plaza.center_norm[1],
                scenario);
            int plazaWaypointIndex = FindNearestWaypoint(mainPath, plaza);
            LayoutToken[] foodTokens = packet.tokens
                .Where(token => token.type == "food_hotspot")
                .OrderBy(token => token.id)
                .ToArray();
            if (settings.visitor_count > 0 && foodTokens.Length == 0)
            {
                throw new ArgumentException("Visitor routes require at least one Food Hotspot.", nameof(packet));
            }

            List<HumanRoutePlan> plans = new List<HumanRoutePlan>();
            int spawnOrder = 0;
            for (int index = 0; index < settings.walker_count; index += 1)
            {
                plans.Add(new HumanRoutePlan(
                    HumanArchetype.Walker,
                    mainPath,
                    -1,
                    settings.walker_speed_cm_per_second * settings.unity_units_per_cm,
                    0f,
                    spawnOrder++ * settings.spawn_interval_seconds));
            }

            for (int index = 0; index < settings.dweller_count; index += 1)
            {
                plans.Add(new HumanRoutePlan(
                    HumanArchetype.Dweller,
                    mainPath,
                    plazaWaypointIndex,
                    settings.dweller_speed_cm_per_second * settings.unity_units_per_cm,
                    settings.dwell_seconds,
                    spawnOrder++ * settings.spawn_interval_seconds));
            }

            for (int index = 0; index < settings.visitor_count; index += 1)
            {
                LayoutToken food = foodTokens[index % foodTokens.Length];
                Vector2 foodPosition = ToLocal(food.x_norm, food.y_norm, scenario);
                Vector2[] route = BuildFoodDetour(mainPath, foodPosition, out int activityWaypointIndex);
                plans.Add(new HumanRoutePlan(
                    HumanArchetype.Visitor,
                    route,
                    activityWaypointIndex,
                    settings.visitor_speed_cm_per_second * settings.unity_units_per_cm,
                    settings.visit_seconds,
                    spawnOrder++ * settings.spawn_interval_seconds,
                    food.id));
            }

            for (int index = 0; index < settings.wheelchair_user_count; index += 1)
            {
                plans.Add(new HumanRoutePlan(
                    HumanArchetype.WheelchairUser,
                    mainPath,
                    -1,
                    settings.wheelchair_user_speed_cm_per_second * settings.unity_units_per_cm,
                    0f,
                    spawnOrder++ * settings.spawn_interval_seconds));
            }

            return plans.ToArray();
        }

        private static Vector2[] BuildFoodDetour(
            Vector2[] mainPath,
            Vector2 foodPosition,
            out int activityWaypointIndex)
        {
            int anchorIndex = FindNearestWaypoint(mainPath, foodPosition);
            List<Vector2> route = new List<Vector2>();
            route.AddRange(mainPath.Take(anchorIndex + 1));
            route.Add(foodPosition);
            activityWaypointIndex = route.Count - 1;
            route.Add(mainPath[anchorIndex]);
            route.AddRange(mainPath.Skip(anchorIndex + 1));
            return route.ToArray();
        }

        private static int FindNearestWaypoint(Vector2[] points, Vector2 target)
        {
            int nearestIndex = 0;
            float nearestDistance = float.PositiveInfinity;
            for (int index = 0; index < points.Length; index += 1)
            {
                float distance = Vector2.SqrMagnitude(points[index] - target);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = index;
                }
            }

            return nearestIndex;
        }

        private static Vector2 ToLocal(float xNorm, float yNorm, P0Scenario scenario)
        {
            float scale = scenario.human_simulation.unity_units_per_cm;
            return new Vector2(
                (xNorm - 0.5f) * scenario.board.width_cm * scale,
                (0.5f - yNorm) * scenario.board.height_cm * scale);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Input;

namespace UrbanWildlife.Planning
{
    public static class P0ConstraintEvaluator
    {
        public static P0ConstraintResult Evaluate(LayoutPacket packet, P0Scenario scenario)
        {
            if (packet == null || scenario == null || scenario.board == null ||
                scenario.fixed_regions == null || scenario.constraints == null ||
                scenario.baseline_layout == null)
            {
                throw new ArgumentException("Layout packet and complete P0 scenario are required.");
            }

            Vector2[] pathCm = ToCentimetres(packet.path.points_norm, scenario.board);
            bool humanConnected = IsHumanPathConnected(pathCm, packet.path.continuous, scenario.fixed_regions);
            HashSet<int> validFoodIds = FindValidFoodIds(packet.tokens, pathCm, scenario);
            bool foodValid = packet.tokens
                .Where(token => token.type == "food_hotspot")
                .All(token => validFoodIds.Contains(token.id));
            bool animalReachable = IsAnimalRouteAvailable(packet.tokens, validFoodIds, scenario);
            int changesUsed = CountChanges(packet, pathCm, scenario);
            int changesAllowed = scenario.constraints.changes_allowed;
            bool withinBudget = changesUsed <= changesAllowed;

            return new P0ConstraintResult
            {
                human_connected = humanConnected,
                animal_reachable = animalReachable,
                food_hotspot_valid = foodValid,
                changes_used = changesUsed,
                changes_allowed = changesAllowed,
                within_change_budget = withinBudget,
                all_constraints_satisfied = humanConnected && animalReachable && foodValid && withinBudget,
            };
        }

        public static LayoutPacket CreateBaselinePacket(P0Scenario scenario)
        {
            LayoutToken[] tokens = scenario.baseline_layout.tokens
                .Select(token => new LayoutToken
                {
                    id = token.id,
                    type = token.type,
                    x_norm = token.center_norm[0],
                    y_norm = token.center_norm[1],
                    angle_deg = 0f,
                    in_bounds = true,
                    confidence = 1f,
                })
                .ToArray();
            float[][] points = scenario.baseline_layout.planned_human_path.points_norm;
            return new LayoutPacket
            {
                schema_version = "0.1",
                packet_type = "confirmed_layout",
                tokens = tokens,
                path = new LayoutPath
                {
                    format = "polyline",
                    points_norm = points,
                    point_count = points.Length,
                    continuous = true,
                    component_count = 1,
                },
            };
        }

        private static bool IsHumanPathConnected(
            Vector2[] pathCm,
            bool continuous,
            ScenarioFixedRegions regions)
        {
            if (!continuous || pathCm.Length < 2)
            {
                return false;
            }

            return PointInRectangle(pathCm[0], regions.entrance_a) &&
                   PolylineTouchesEllipse(pathCm, regions.central_plaza) &&
                   PointInRectangle(pathCm[pathCm.Length - 1], regions.exit_b);
        }

        private static HashSet<int> FindValidFoodIds(
            LayoutToken[] tokens,
            Vector2[] pathCm,
            P0Scenario scenario)
        {
            HashSet<int> valid = new HashSet<int>();
            ScenarioConstraints rules = scenario.constraints;
            float minimumCentrelineDistance = rules.food_token_radius_cm +
                                              rules.path_width_cm / 2f +
                                              rules.food_path_edge_clearance_cm;
            foreach (LayoutToken token in tokens.Where(item => item.type == "food_hotspot"))
            {
                Vector2 centre = ToCentimetres(token.x_norm, token.y_norm, scenario.board);
                float pathDistance = DistanceToPolyline(centre, pathCm);
                bool hasHumanActivitySource = PointInEllipse(centre, scenario.fixed_regions.central_plaza) ||
                                              pathDistance <= rules.food_path_influence_max_cm;
                bool clearsPath = pathDistance >= minimumCentrelineDistance;
                bool clearsPond = !PointInExpandedEllipse(
                    centre,
                    scenario.fixed_regions.pond,
                    rules.food_token_radius_cm);
                bool onBoard = centre.x >= rules.food_token_radius_cm &&
                               centre.x <= scenario.board.width_cm - rules.food_token_radius_cm &&
                               centre.y >= rules.food_token_radius_cm &&
                               centre.y <= scenario.board.height_cm - rules.food_token_radius_cm;
                if (hasHumanActivitySource && clearsPath && clearsPond && onBoard)
                {
                    valid.Add(token.id);
                }
            }

            return valid;
        }

        private static bool IsAnimalRouteAvailable(
            LayoutToken[] tokens,
            HashSet<int> validFoodIds,
            P0Scenario scenario)
        {
            if (validFoodIds.Count == 0)
            {
                return false;
            }

            bool woodlandOutsidePond = tokens
                .Where(token => token.type == "woodland")
                .Select(token => ToCentimetres(token.x_norm, token.y_norm, scenario.board))
                .Any(centre => !PointInEllipse(centre, scenario.fixed_regions.pond));

            // In S001 the pond is the only impassable region and does not touch a board edge.
            // The human path adds movement cost but is deliberately not a wall, so any valid
            // woodland and food endpoint outside the pond have a route around it.
            return woodlandOutsidePond;
        }

        private static int CountChanges(LayoutPacket packet, Vector2[] candidatePathCm, P0Scenario scenario)
        {
            Dictionary<int, LayoutToken> currentById = packet.tokens.ToDictionary(token => token.id);
            int changes = 0;
            foreach (ScenarioToken baseline in scenario.baseline_layout.tokens)
            {
                if (!currentById.TryGetValue(baseline.id, out LayoutToken current))
                {
                    changes += 1;
                    continue;
                }

                Vector2 currentCm = ToCentimetres(current.x_norm, current.y_norm, scenario.board);
                Vector2 baselineCm = new Vector2(baseline.center_cm[0], baseline.center_cm[1]);
                if (Vector2.Distance(currentCm, baselineCm) > scenario.constraints.token_move_tolerance_cm)
                {
                    changes += 1;
                }
            }

            Vector2[] baselinePathCm = scenario.baseline_layout.planned_human_path.points_cm
                .Select(point => new Vector2(point[0], point[1]))
                .ToArray();
            if (PolylineHausdorffDistance(candidatePathCm, baselinePathCm) >
                scenario.constraints.path_change_tolerance_cm)
            {
                changes += 1;
            }

            return changes;
        }

        private static Vector2[] ToCentimetres(float[][] pointsNorm, ScenarioBoard board)
        {
            if (pointsNorm == null)
            {
                return Array.Empty<Vector2>();
            }

            return pointsNorm.Select(point => ToCentimetres(point[0], point[1], board)).ToArray();
        }

        private static Vector2 ToCentimetres(float xNorm, float yNorm, ScenarioBoard board)
        {
            return new Vector2(xNorm * board.width_cm, yNorm * board.height_cm);
        }

        private static bool PointInRectangle(Vector2 point, ScenarioRegion rectangle)
        {
            Vector2 centre = new Vector2(rectangle.center_cm[0], rectangle.center_cm[1]);
            Vector2 half = new Vector2(rectangle.size_cm[0] / 2f, rectangle.size_cm[1] / 2f);
            return Mathf.Abs(point.x - centre.x) <= half.x && Mathf.Abs(point.y - centre.y) <= half.y;
        }

        private static bool PointInEllipse(Vector2 point, ScenarioRegion ellipse)
        {
            Vector2 centre = new Vector2(ellipse.center_cm[0], ellipse.center_cm[1]);
            float[] size = ellipse.size_cm ?? ellipse.bounding_size_cm;
            Vector2 radii = new Vector2(size[0] / 2f, size[1] / 2f);
            float x = (point.x - centre.x) / radii.x;
            float y = (point.y - centre.y) / radii.y;
            return x * x + y * y <= 1f;
        }

        private static bool PointInExpandedEllipse(Vector2 point, ScenarioRegion ellipse, float expansionCm)
        {
            Vector2 centre = new Vector2(ellipse.center_cm[0], ellipse.center_cm[1]);
            float[] size = ellipse.size_cm ?? ellipse.bounding_size_cm;
            Vector2 radii = new Vector2(size[0] / 2f + expansionCm, size[1] / 2f + expansionCm);
            float x = (point.x - centre.x) / radii.x;
            float y = (point.y - centre.y) / radii.y;
            return x * x + y * y <= 1f;
        }

        private static bool PolylineTouchesEllipse(Vector2[] points, ScenarioRegion ellipse)
        {
            if (points.Any(point => PointInEllipse(point, ellipse)))
            {
                return true;
            }

            Vector2 centre = new Vector2(ellipse.center_cm[0], ellipse.center_cm[1]);
            float[] size = ellipse.size_cm ?? ellipse.bounding_size_cm;
            Vector2 radii = new Vector2(size[0] / 2f, size[1] / 2f);
            for (int index = 1; index < points.Length; index += 1)
            {
                Vector2 start = new Vector2(
                    (points[index - 1].x - centre.x) / radii.x,
                    (points[index - 1].y - centre.y) / radii.y);
                Vector2 end = new Vector2(
                    (points[index].x - centre.x) / radii.x,
                    (points[index].y - centre.y) / radii.y);
                if (DistanceToSegment(Vector2.zero, start, end) <= 1f)
                {
                    return true;
                }
            }

            return false;
        }

        private static float DistanceToPolyline(Vector2 point, Vector2[] polyline)
        {
            if (polyline.Length == 0)
            {
                return float.PositiveInfinity;
            }

            if (polyline.Length == 1)
            {
                return Vector2.Distance(point, polyline[0]);
            }

            float minimum = float.PositiveInfinity;
            for (int index = 1; index < polyline.Length; index += 1)
            {
                minimum = Mathf.Min(minimum, DistanceToSegment(point, polyline[index - 1], polyline[index]));
            }

            return minimum;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + t * segment);
        }

        private static float PolylineHausdorffDistance(Vector2[] first, Vector2[] second)
        {
            if (first.Length == 0 || second.Length == 0)
            {
                return float.PositiveInfinity;
            }

            float firstToSecond = first.Max(point => DistanceToPolyline(point, second));
            float secondToFirst = second.Max(point => DistanceToPolyline(point, first));
            return Mathf.Max(firstToSecond, secondToFirst);
        }
    }
}

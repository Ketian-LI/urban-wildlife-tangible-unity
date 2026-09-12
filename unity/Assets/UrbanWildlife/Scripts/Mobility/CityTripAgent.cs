using System;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Mobility
{
    public sealed class CityTripAgent
    {
        public const float WalkSpeedUnitsPerSecond = 1.4f;
        public const float DriveSpeedUnitsPerSecond = 7f;

        private readonly CityBounds bounds;
        private float[][] activeRoute;
        private int segmentIndex;
        private float segmentProgress;
        private float dwellRemaining;

        public CityTripAgent(CityRepresentativeTrip trip, CityBounds cityBounds)
        {
            if (trip?.route_points_norm == null || trip.route_points_norm.Length < 2 ||
                cityBounds == null)
            {
                throw new ArgumentException("A valid representative trip and city bounds are required.");
            }
            Trip = trip;
            bounds = cityBounds;
            activeRoute = ClonePoints(trip.route_points_norm);
            PositionNorm = ClonePoint(activeRoute[0]);
            State = CityTripMotionState.Pending;
        }

        public CityRepresentativeTrip Trip { get; }
        public CityTripMotionState State { get; private set; }
        public float[] PositionNorm { get; private set; }
        public float RouteProgress01 { get; private set; }
        public float SpeedUnitsPerSecond => Trip.mode == CityTravelMode.Drive
            ? DriveSpeedUnitsPerSecond
            : WalkSpeedUnitsPerSecond;

        public void Start()
        {
            if (State != CityTripMotionState.Pending)
            {
                return;
            }
            State = CityTripMotionState.Outbound;
            RouteProgress01 = 0f;
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }
            float remaining = deltaSeconds;
            int safety = 0;
            while (remaining > 0.0001f && safety < 128)
            {
                safety += 1;
                if (State == CityTripMotionState.Pending || State == CityTripMotionState.Complete)
                {
                    return;
                }
                if (State == CityTripMotionState.Dwelling)
                {
                    float consumed = Math.Min(remaining, dwellRemaining);
                    dwellRemaining -= consumed;
                    remaining -= consumed;
                    if (dwellRemaining <= 0.0001f)
                    {
                        BeginReturn();
                    }
                    continue;
                }
                remaining = MoveAlongRoute(remaining);
            }
        }

        public CityVehicleAgent VehicleSnapshot()
        {
            if (Trip.mode != CityTravelMode.Drive ||
                State == CityTripMotionState.Pending || State == CityTripMotionState.Complete)
            {
                return null;
            }
            return new CityVehicleAgent
            {
                id = $"vehicle-{Trip.id}",
                source_trip_id = Trip.id,
                state = State,
                position_norm = ClonePoint(PositionNorm),
                route_progress_01 = RouteProgress01,
                speed_units_per_second = DriveSpeedUnitsPerSecond,
            };
        }

        private float MoveAlongRoute(float availableSeconds)
        {
            if (segmentIndex >= activeRoute.Length - 1)
            {
                FinishLeg();
                return availableSeconds;
            }
            float[] start = activeRoute[segmentIndex];
            float[] end = activeRoute[segmentIndex + 1];
            float segmentLength = Distance(start, end);
            if (segmentLength <= 0.0001f)
            {
                segmentIndex += 1;
                segmentProgress = 0f;
                PositionNorm = ClonePoint(end);
                return availableSeconds;
            }

            float distanceRemaining = segmentLength * (1f - segmentProgress);
            float secondsNeeded = distanceRemaining / SpeedUnitsPerSecond;
            if (availableSeconds + 0.0001f >= secondsNeeded)
            {
                PositionNorm = ClonePoint(end);
                segmentIndex += 1;
                segmentProgress = 0f;
                UpdateProgress();
                if (segmentIndex >= activeRoute.Length - 1)
                {
                    FinishLeg();
                }
                return Math.Max(0f, availableSeconds - secondsNeeded);
            }

            float distanceMoved = availableSeconds * SpeedUnitsPerSecond;
            segmentProgress += distanceMoved / segmentLength;
            PositionNorm = new[]
            {
                Lerp(start[0], end[0], segmentProgress),
                Lerp(start[1], end[1], segmentProgress),
            };
            UpdateProgress();
            return 0f;
        }

        private void FinishLeg()
        {
            if (State == CityTripMotionState.Outbound)
            {
                State = CityTripMotionState.Dwelling;
                dwellRemaining = Math.Max(0f, Trip.dwell_seconds);
                RouteProgress01 = 1f;
                if (dwellRemaining <= 0f)
                {
                    BeginReturn();
                }
                return;
            }
            if (State == CityTripMotionState.Returning)
            {
                State = CityTripMotionState.Complete;
                RouteProgress01 = 1f;
            }
        }

        private void BeginReturn()
        {
            activeRoute = activeRoute.Reverse().Select(ClonePoint).ToArray();
            segmentIndex = 0;
            segmentProgress = 0f;
            State = CityTripMotionState.Returning;
            RouteProgress01 = 0f;
        }

        private void UpdateProgress()
        {
            float completed = 0f;
            for (int index = 1; index <= segmentIndex && index < activeRoute.Length; index += 1)
            {
                completed += Distance(activeRoute[index - 1], activeRoute[index]);
            }
            if (segmentIndex < activeRoute.Length - 1)
            {
                completed += Distance(activeRoute[segmentIndex], activeRoute[segmentIndex + 1]) *
                             segmentProgress;
            }
            float total = CityNetworkRouteBuilder.Length(activeRoute, bounds);
            RouteProgress01 = total <= 0.0001f ? 1f : Math.Min(1f, completed / total);
        }

        private float Distance(float[] first, float[] second)
        {
            float x = (first[0] - second[0]) * bounds.width_units;
            float y = (first[1] - second[1]) * bounds.height_units;
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static float Lerp(float first, float second, float t)
        {
            return first + (second - first) * t;
        }

        private static float[][] ClonePoints(float[][] points)
        {
            return points.Select(ClonePoint).ToArray();
        }

        private static float[] ClonePoint(float[] point)
        {
            return (float[])point.Clone();
        }
    }
}

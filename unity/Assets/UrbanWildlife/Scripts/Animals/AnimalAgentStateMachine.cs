using System;
using UnityEngine;

namespace UrbanWildlife.Animals
{
    public enum AnimalSpecies
    {
        Pigeon,
        Squirrel,
        Fox,
    }

    public enum AnimalActivityState
    {
        Resting,
        Wandering,
        Patrolling,
        DetectingFood,
        ApproachingFood,
        Feeding,
        Staying,
        AvoidingHumans,
        Retreating,
    }

    public sealed class AnimalAgentConfig
    {
        public AnimalSpecies Species { get; set; }
        public float SpeedUnitsPerSecond { get; set; }
        public float DetectionRadiusUnits { get; set; }
        public float DisturbanceRadiusUnits { get; set; }
        public float FeedSeconds { get; set; }
        public float PostFeedStaySeconds { get; set; }
        public float RestSeconds { get; set; }
        public bool AvoidsHumans { get; set; }
        public float InitialFamiliarity { get; set; }
        public float FamiliarityGainPerFeed { get; set; }
        public float MaximumFamiliarity { get; set; }
    }

    public readonly struct AnimalPerception
    {
        public AnimalPerception(
            bool hasFood,
            Vector2 foodPosition,
            bool hasShelter,
            Vector2 shelterPosition,
            float nearestHumanDistance)
            : this(
                hasFood,
                foodPosition,
                hasShelter,
                shelterPosition,
                nearestHumanDistance,
                Array.Empty<AnimalMovementObstacle>(),
                Vector2.zero)
        {
        }

        public AnimalPerception(
            bool hasFood,
            Vector2 foodPosition,
            bool hasShelter,
            Vector2 shelterPosition,
            float nearestHumanDistance,
            AnimalMovementObstacle[] movementObstacles,
            Vector2 boardHalfExtents)
        {
            HasFood = hasFood;
            FoodPosition = foodPosition;
            HasShelter = hasShelter;
            ShelterPosition = shelterPosition;
            NearestHumanDistance = nearestHumanDistance;
            MovementObstacles = movementObstacles ?? Array.Empty<AnimalMovementObstacle>();
            BoardHalfExtents = boardHalfExtents;
        }

        public bool HasFood { get; }
        public Vector2 FoodPosition { get; }
        public bool HasShelter { get; }
        public Vector2 ShelterPosition { get; }
        public float NearestHumanDistance { get; }
        public AnimalMovementObstacle[] MovementObstacles { get; }
        public Vector2 BoardHalfExtents { get; }
    }

    public sealed class AnimalSpawnPlan
    {
        public AnimalSpawnPlan(
            AnimalAgentConfig config,
            Vector2 startPosition,
            Vector2 foodPosition,
            Vector2 shelterPosition,
            int foodTokenId,
            int woodlandTokenId)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            StartPosition = startPosition;
            FoodPosition = foodPosition;
            ShelterPosition = shelterPosition;
            FoodTokenId = foodTokenId;
            WoodlandTokenId = woodlandTokenId;
        }

        public AnimalAgentConfig Config { get; }
        public Vector2 StartPosition { get; }
        public Vector2 FoodPosition { get; }
        public Vector2 ShelterPosition { get; }
        public int FoodTokenId { get; }
        public int WoodlandTokenId { get; }
    }

    public sealed class AnimalAgentStateMachine
    {
        private readonly AnimalAgentConfig config;
        private float stateTimer;
        private Vector2 navigationTarget;
        private Vector2 navigationWaypoint;
        private bool hasNavigationTarget;
        private bool hasNavigationWaypoint;

        public AnimalAgentStateMachine(AnimalSpawnPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }
            if (plan.Config.SpeedUnitsPerSecond <= 0f || plan.Config.FeedSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(plan));
            }

            config = plan.Config;
            Position = plan.StartPosition;
            Familiarity = Mathf.Clamp(plan.Config.InitialFamiliarity, 0f, plan.Config.MaximumFamiliarity);
            State = config.Species == AnimalSpecies.Pigeon
                ? AnimalActivityState.Wandering
                : AnimalActivityState.Resting;
            stateTimer = config.Species == AnimalSpecies.Pigeon ? 0f : config.RestSeconds;
        }

        public AnimalSpecies Species => config.Species;
        public AnimalActivityState State { get; private set; }
        public Vector2 Position { get; private set; }
        public int FeedEvents { get; private set; }
        public int AvoidanceEvents { get; private set; }
        public float Familiarity { get; private set; }

        public void Tick(float deltaSeconds, AnimalPerception perception)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (ShouldAvoidHumans(perception) &&
                State != AnimalActivityState.AvoidingHumans &&
                State != AnimalActivityState.Retreating)
            {
                AvoidanceEvents += 1;
                SetState(AnimalActivityState.AvoidingHumans, 0.75f);
            }

            switch (State)
            {
                case AnimalActivityState.Resting:
                    if (ConsumeTimer(deltaSeconds))
                    {
                        SetState(
                            Species == AnimalSpecies.Fox
                                ? AnimalActivityState.Patrolling
                                : AnimalActivityState.Wandering);
                    }
                    break;
                case AnimalActivityState.Wandering:
                case AnimalActivityState.Patrolling:
                    if (perception.HasFood &&
                        Vector2.Distance(Position, perception.FoodPosition) <= config.DetectionRadiusUnits)
                    {
                        SetState(AnimalActivityState.DetectingFood, 0.5f);
                    }
                    break;
                case AnimalActivityState.DetectingFood:
                    if (!perception.HasFood)
                    {
                        ReturnToSearch();
                    }
                    else if (ConsumeTimer(deltaSeconds))
                    {
                        SetState(AnimalActivityState.ApproachingFood);
                    }
                    break;
                case AnimalActivityState.ApproachingFood:
                    if (!perception.HasFood)
                    {
                        ReturnToSearch();
                    }
                    else if (MoveTowards(perception.FoodPosition, deltaSeconds, perception))
                    {
                        SetState(AnimalActivityState.Feeding, config.FeedSeconds);
                    }
                    break;
                case AnimalActivityState.Feeding:
                    if (ConsumeTimer(deltaSeconds))
                    {
                        FeedEvents += 1;
                        if (Species == AnimalSpecies.Squirrel)
                        {
                            Familiarity = Mathf.Min(
                                config.MaximumFamiliarity,
                                Familiarity + config.FamiliarityGainPerFeed);
                        }

                        SetState(
                            Species == AnimalSpecies.Pigeon
                                ? AnimalActivityState.Staying
                                : AnimalActivityState.Retreating,
                            Species == AnimalSpecies.Pigeon ? config.PostFeedStaySeconds : 0f);
                    }
                    break;
                case AnimalActivityState.Staying:
                    if (ConsumeTimer(deltaSeconds))
                    {
                        SetState(AnimalActivityState.Wandering);
                    }
                    break;
                case AnimalActivityState.AvoidingHumans:
                    if (ConsumeTimer(deltaSeconds))
                    {
                        SetState(AnimalActivityState.Retreating);
                    }
                    break;
                case AnimalActivityState.Retreating:
                    if (!perception.HasShelter || MoveTowards(perception.ShelterPosition, deltaSeconds, perception))
                    {
                        SetState(AnimalActivityState.Resting, config.RestSeconds);
                    }
                    break;
            }
        }

        private bool ShouldAvoidHumans(AnimalPerception perception)
        {
            if (!config.AvoidsHumans)
            {
                return false;
            }

            float effectiveRadius = config.DisturbanceRadiusUnits;
            if (Species == AnimalSpecies.Squirrel)
            {
                effectiveRadius *= 1f - 0.35f * Familiarity;
            }
            return perception.NearestHumanDistance < effectiveRadius;
        }

        private bool MoveTowards(Vector2 target, float deltaSeconds, AnimalPerception perception)
        {
            float clearance = AnimalObstacleAvoidance.ClearanceFor(Species);
            Vector2 accessibleTarget = AnimalObstacleAvoidance.ResolveAccessibleTarget(
                Position,
                target,
                perception.MovementObstacles,
                clearance,
                perception.BoardHalfExtents);
            if (!hasNavigationTarget || Vector2.Distance(navigationTarget, accessibleTarget) > 0.02f)
            {
                navigationTarget = accessibleTarget;
                hasNavigationTarget = true;
                hasNavigationWaypoint = false;
            }

            if (!hasNavigationWaypoint || Vector2.Distance(Position, navigationWaypoint) <= 0.015f)
            {
                navigationWaypoint = AnimalObstacleAvoidance.NextWaypoint(
                    Position,
                    navigationTarget,
                    perception.MovementObstacles,
                    clearance,
                    perception.BoardHalfExtents);
                hasNavigationWaypoint = Vector2.Distance(navigationWaypoint, navigationTarget) > 0.015f;
            }

            Vector2 stepTarget = hasNavigationWaypoint ? navigationWaypoint : navigationTarget;
            Position = Vector2.MoveTowards(Position, stepTarget, config.SpeedUnitsPerSecond * deltaSeconds);
            if (hasNavigationWaypoint && Vector2.Distance(Position, navigationWaypoint) <= 0.001f)
            {
                hasNavigationWaypoint = false;
            }
            return !hasNavigationWaypoint && Vector2.Distance(Position, navigationTarget) <= 0.001f;
        }

        private bool ConsumeTimer(float deltaSeconds)
        {
            stateTimer = Mathf.Max(0f, stateTimer - deltaSeconds);
            return stateTimer <= 0f;
        }

        private void ReturnToSearch()
        {
            SetState(
                Species == AnimalSpecies.Fox
                    ? AnimalActivityState.Patrolling
                    : AnimalActivityState.Wandering);
        }

        private void SetState(AnimalActivityState state, float timer = 0f)
        {
            State = state;
            stateTimer = Mathf.Max(0f, timer);
            if (state != AnimalActivityState.ApproachingFood && state != AnimalActivityState.Retreating)
            {
                hasNavigationTarget = false;
                hasNavigationWaypoint = false;
            }
        }
    }
}

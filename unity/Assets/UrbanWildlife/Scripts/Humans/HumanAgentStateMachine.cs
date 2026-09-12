using System;
using UnityEngine;

namespace UrbanWildlife.Humans
{
    public enum HumanArchetype
    {
        Walker,
        Dweller,
        Visitor,
        WheelchairUser,
    }

    public enum HumanActivityState
    {
        WaitingToEnter,
        Moving,
        Dwelling,
        Visiting,
        Finished,
    }

    public sealed class HumanRoutePlan
    {
        public HumanRoutePlan(
            HumanArchetype archetype,
            Vector2[] waypoints,
            int activityWaypointIndex,
            float speedUnitsPerSecond,
            float activitySeconds,
            float startDelaySeconds,
            int sourceTokenId = -1)
        {
            if (waypoints == null || waypoints.Length < 2)
            {
                throw new ArgumentException("A human route requires at least two waypoints.", nameof(waypoints));
            }
            if (speedUnitsPerSecond <= 0f || activitySeconds < 0f || startDelaySeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speedUnitsPerSecond));
            }
            if (activityWaypointIndex >= waypoints.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(activityWaypointIndex));
            }

            Archetype = archetype;
            Waypoints = (Vector2[])waypoints.Clone();
            ActivityWaypointIndex = activityWaypointIndex;
            SpeedUnitsPerSecond = speedUnitsPerSecond;
            ActivitySeconds = activitySeconds;
            StartDelaySeconds = startDelaySeconds;
            SourceTokenId = sourceTokenId;
        }

        public HumanArchetype Archetype { get; }
        public Vector2[] Waypoints { get; }
        public int ActivityWaypointIndex { get; }
        public float SpeedUnitsPerSecond { get; }
        public float ActivitySeconds { get; }
        public float StartDelaySeconds { get; }
        public int SourceTokenId { get; }
    }

    public sealed class HumanAgentStateMachine
    {
        private readonly HumanRoutePlan plan;
        private int targetWaypointIndex;
        private float remainingDelay;
        private float remainingActivity;
        private bool activityCompleted;

        public HumanAgentStateMachine(HumanRoutePlan plan)
        {
            this.plan = plan ?? throw new ArgumentNullException(nameof(plan));
            Position = plan.Waypoints[0];
            targetWaypointIndex = 1;
            remainingDelay = plan.StartDelaySeconds;
            State = remainingDelay > 0f ? HumanActivityState.WaitingToEnter : HumanActivityState.Moving;
        }

        public HumanArchetype Archetype => plan.Archetype;
        public HumanActivityState State { get; private set; }
        public Vector2 Position { get; private set; }
        public int SourceTokenId => plan.SourceTokenId;
        public bool IsFinished => State == HumanActivityState.Finished;

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || IsFinished)
            {
                return;
            }

            float remainingTick = deltaSeconds;
            int transitionGuard = 0;
            while (remainingTick > 0f && !IsFinished && transitionGuard < 100)
            {
                transitionGuard += 1;
                if (State == HumanActivityState.WaitingToEnter)
                {
                    if (remainingTick < remainingDelay)
                    {
                        remainingDelay -= remainingTick;
                        return;
                    }

                    remainingTick -= remainingDelay;
                    remainingDelay = 0f;
                    State = HumanActivityState.Moving;
                    continue;
                }

                if (State == HumanActivityState.Dwelling || State == HumanActivityState.Visiting)
                {
                    if (remainingTick < remainingActivity)
                    {
                        remainingActivity -= remainingTick;
                        return;
                    }

                    remainingTick -= remainingActivity;
                    remainingActivity = 0f;
                    activityCompleted = true;
                    AdvanceAfterWaypoint();
                    continue;
                }

                Vector2 target = plan.Waypoints[targetWaypointIndex];
                float distance = Vector2.Distance(Position, target);
                float travelTime = distance / plan.SpeedUnitsPerSecond;
                if (travelTime > remainingTick)
                {
                    Position = Vector2.MoveTowards(
                        Position,
                        target,
                        plan.SpeedUnitsPerSecond * remainingTick);
                    return;
                }

                Position = target;
                remainingTick -= travelTime;
                if (!activityCompleted &&
                    targetWaypointIndex == plan.ActivityWaypointIndex &&
                    plan.ActivitySeconds > 0f)
                {
                    remainingActivity = plan.ActivitySeconds;
                    State = plan.Archetype == HumanArchetype.Dweller
                        ? HumanActivityState.Dwelling
                        : HumanActivityState.Visiting;
                }
                else
                {
                    AdvanceAfterWaypoint();
                }
            }
        }

        private void AdvanceAfterWaypoint()
        {
            if (targetWaypointIndex >= plan.Waypoints.Length - 1)
            {
                State = HumanActivityState.Finished;
                return;
            }

            targetWaypointIndex += 1;
            State = HumanActivityState.Moving;
        }
    }
}

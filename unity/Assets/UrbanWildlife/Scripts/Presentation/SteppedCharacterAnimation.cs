using UnityEngine;

namespace UrbanWildlife.Presentation
{
    public enum CharacterAnimationAction
    {
        Idle,
        Turning,
        Walking,
        Feeding,
        Sitting,
        Rising,
    }

    public readonly struct CharacterPose
    {
        public CharacterPose(
            int frameIndex,
            int frameCount,
            float widthScale,
            float heightScale,
            float swayDegrees,
            float lift)
        {
            FrameIndex = frameIndex;
            FrameCount = frameCount;
            WidthScale = widthScale;
            HeightScale = heightScale;
            SwayDegrees = swayDegrees;
            Lift = lift;
        }

        public int FrameIndex { get; }
        public int FrameCount { get; }
        public float WidthScale { get; }
        public float HeightScale { get; }
        public float SwayDegrees { get; }
        public float Lift { get; }
    }

    public static class SteppedCharacterAnimation
    {
        public const float FramesPerSecond = 8f;
        public const int IdleFrameCount = 3;
        public const int TurnFrameCount = 3;
        public const int WalkFrameCount = 6;
        public const int FeedFrameCount = 6;
        public const int SitFrameCount = 4;
        public const int RiseFrameCount = 4;
        public const float TurnSeconds = TurnFrameCount / FramesPerSecond;
        public const float RiseSeconds = RiseFrameCount / FramesPerSecond;

        private static readonly float[] IdleWidth = { 1f, 1.003f, 1f };
        private static readonly float[] IdleHeight = { 1f, 1.008f, 1.002f };
        private static readonly float[] IdleSway = { -0.25f, 0.3f, 0f };

        private static readonly float[] TurnWidth = { 1f, 0.2f, 1f };
        private static readonly float[] TurnHeight = { 1f, 1.025f, 1f };
        private static readonly float[] TurnSway = { 0f, 4.5f, 0f };

        private static readonly float[] WalkWidth = { 1f, 0.994f, 1f, 1f, 0.994f, 1f };
        private static readonly float[] WalkHeight = { 1f, 1.012f, 1.02f, 1f, 1.012f, 1.02f };
        private static readonly float[] WalkSway = { -0.7f, 0f, 0.7f, 0.7f, 0f, -0.7f };
        private static readonly float[] WalkLift = { 0f, 0.002f, 0.004f, 0f, 0.002f, 0.004f };

        private static readonly float[] FeedWidth = { 1f, 1.015f, 1.035f, 1.035f, 1.015f, 1f };
        private static readonly float[] FeedHeight = { 1f, 0.96f, 0.89f, 0.89f, 0.96f, 1f };
        private static readonly float[] FeedSway = { 0f, 1.5f, 3.5f, 3.5f, 1.5f, 0f };

        private static readonly float[] SitWidth = { 1f, 1.025f, 1.055f, 1.075f };
        private static readonly float[] SitHeight = { 1f, 0.94f, 0.85f, 0.79f };
        private static readonly float[] SitSway = { 0f, 1f, 1.8f, 0.5f };

        public static int FrameCountFor(CharacterAnimationAction action)
        {
            switch (action)
            {
                case CharacterAnimationAction.Turning:
                    return TurnFrameCount;
                case CharacterAnimationAction.Walking:
                    return WalkFrameCount;
                case CharacterAnimationAction.Feeding:
                    return FeedFrameCount;
                case CharacterAnimationAction.Sitting:
                    return SitFrameCount;
                case CharacterAnimationAction.Rising:
                    return RiseFrameCount;
                default:
                    return IdleFrameCount;
            }
        }

        public static int FrameIndexFor(
            CharacterAnimationAction action,
            float elapsedSeconds,
            float offsetSeconds = 0f)
        {
            int count = FrameCountFor(action);
            int raw = Mathf.Max(0, Mathf.FloorToInt((elapsedSeconds + offsetSeconds) * FramesPerSecond));
            if (IsLooping(action))
            {
                return raw % count;
            }
            return Mathf.Min(count - 1, raw);
        }

        public static CharacterPose Sample(
            CharacterAnimationAction action,
            float elapsedSeconds,
            float offsetSeconds = 0f)
        {
            int frame = FrameIndexFor(action, elapsedSeconds, offsetSeconds);
            switch (action)
            {
                case CharacterAnimationAction.Turning:
                    return Pose(frame, TurnFrameCount, TurnWidth, TurnHeight, TurnSway);
                case CharacterAnimationAction.Walking:
                    return Pose(frame, WalkFrameCount, WalkWidth, WalkHeight, WalkSway, WalkLift);
                case CharacterAnimationAction.Feeding:
                    return Pose(frame, FeedFrameCount, FeedWidth, FeedHeight, FeedSway);
                case CharacterAnimationAction.Sitting:
                    return Pose(frame, SitFrameCount, SitWidth, SitHeight, SitSway);
                case CharacterAnimationAction.Rising:
                    int reversed = RiseFrameCount - 1 - frame;
                    return Pose(reversed, RiseFrameCount, SitWidth, SitHeight, SitSway);
                default:
                    return Pose(frame, IdleFrameCount, IdleWidth, IdleHeight, IdleSway);
            }
        }

        public static bool UseAlternateArtwork(CharacterAnimationAction action, int frameIndex)
        {
            if (action == CharacterAnimationAction.Walking)
            {
                return frameIndex >= 3;
            }
            if (action == CharacterAnimationAction.Feeding)
            {
                return frameIndex == 2 || frameIndex == 3;
            }
            return false;
        }

        public static bool HasTurnPassedMidpoint(float elapsedSeconds)
        {
            return elapsedSeconds >= TurnSeconds * 0.5f;
        }

        private static bool IsLooping(CharacterAnimationAction action)
        {
            return action == CharacterAnimationAction.Idle ||
                action == CharacterAnimationAction.Walking ||
                action == CharacterAnimationAction.Feeding;
        }

        private static CharacterPose Pose(
            int frame,
            int frameCount,
            float[] widths,
            float[] heights,
            float[] sways,
            float[] lifts = null)
        {
            return new CharacterPose(
                frame,
                frameCount,
                widths[frame],
                heights[frame],
                sways[frame],
                lifts == null ? 0f : lifts[frame]);
        }
    }
}

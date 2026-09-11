using System;

namespace UrbanWildlife.Cycle
{
    public enum P0PredictedFeeder
    {
        None,
        Pigeon,
        Squirrel,
        Fox,
    }

    public enum P0PredictedConflictArea
    {
        None,
        MainRoute,
        ActivityNode,
        WoodlandEdge,
    }

    public sealed class P0CycleProfile
    {
        public P0CycleProfile(
            string scenarioId,
            string title,
            string brief,
            int pigeonCount,
            int squirrelCount,
            int foxCount)
        {
            ScenarioId = scenarioId;
            Title = title;
            Brief = brief;
            PigeonCount = pigeonCount;
            SquirrelCount = squirrelCount;
            FoxCount = foxCount;
        }

        public string ScenarioId { get; }
        public string Title { get; }
        public string Brief { get; }
        public int PigeonCount { get; }
        public int SquirrelCount { get; }
        public int FoxCount { get; }
        public int AnimalCount => PigeonCount + SquirrelCount + FoxCount;
    }

    public sealed class P0CycleMechanics
    {
        public P0CycleMechanics()
        {
            BeginCycle(0);
        }

        public int CycleIndex { get; private set; }
        public P0CycleProfile CurrentProfile { get; private set; }
        public P0PredictedFeeder PredictedFeeder { get; private set; }
        public P0PredictedConflictArea PredictedConflictArea { get; private set; }
        public bool PredictionReady =>
            PredictedFeeder != P0PredictedFeeder.None &&
            PredictedConflictArea != P0PredictedConflictArea.None;

        public static P0CycleProfile ProfileForCycle(int cycleIndex)
        {
            switch (cycleIndex)
            {
                case 0:
                    return new P0CycleProfile(
                        "S001-C1",
                        "Repair the park",
                        "Restore public access while keeping a low-disturbance route between woodland and activity nodes.",
                        7,
                        3,
                        1);
                case 1:
                    return new P0CycleProfile(
                        "S001-C2",
                        "Weekend picnic pressure",
                        "More visitors increase food opportunity and disturbance. Re-plan without blocking normal movement.",
                        9,
                        4,
                        1);
                default:
                    return new P0CycleProfile(
                        "S001-C3",
                        "Dusk final proposal",
                        "Test the final layout as human activity falls and two foxes begin using the park edge.",
                        8,
                        3,
                        2);
            }
        }

        public void BeginCycle(int cycleIndex)
        {
            CycleIndex = Math.Max(0, cycleIndex);
            CurrentProfile = ProfileForCycle(CycleIndex);
            PredictedFeeder = P0PredictedFeeder.None;
            PredictedConflictArea = P0PredictedConflictArea.None;
        }

        public bool SetPredictedFeeder(P0PredictedFeeder prediction)
        {
            if (prediction == P0PredictedFeeder.None)
            {
                return false;
            }

            PredictedFeeder = prediction;
            return true;
        }

        public bool SetPredictedConflictArea(P0PredictedConflictArea prediction)
        {
            if (prediction == P0PredictedConflictArea.None)
            {
                return false;
            }

            PredictedConflictArea = prediction;
            return true;
        }

        public string PredictionSummary()
        {
            if (!PredictionReady)
            {
                return "Prediction incomplete";
            }

            return $"Most feeding: {FeederLabel(PredictedFeeder)} · Most pressure: {ConflictAreaLabel(PredictedConflictArea)}";
        }

        public string BuildObservationSummary(
            int pigeonFeedEvents,
            int squirrelFeedEvents,
            int foxFeedEvents,
            int avoidanceEvents)
        {
            string actual = ActualTopFeeder(pigeonFeedEvents, squirrelFeedEvents, foxFeedEvents);
            string comparison = PredictionReady
                ? string.Equals(actual, FeederLabel(PredictedFeeder), StringComparison.Ordinal)
                    ? "prediction matched"
                    : actual == "Mixed"
                        ? "outcome was mixed"
                        : "prediction differed"
                : "no prediction recorded";
            return
                $"Observed feeds — Pigeon {pigeonFeedEvents}, Squirrel {squirrelFeedEvents}, Fox {foxFeedEvents}. " +
                $"Most feeding: {actual} ({comparison}). Avoidance events: {avoidanceEvents}.";
        }

        public string BuildCausalExplanation(
            int pigeonFeedEvents,
            int squirrelFeedEvents,
            int foxFeedEvents,
            int avoidanceEvents)
        {
            int totalFeeds = pigeonFeedEvents + squirrelFeedEvents + foxFeedEvents;
            if (avoidanceEvents > 0)
            {
                return
                    $"Human proximity interrupted cautious animals {avoidanceEvents} time(s). " +
                    "In the next Plan, compare activity-node distance from woodland edges without weakening public access.";
            }

            if (totalFeeds == 0)
            {
                return
                    "No animal reached a feeding opportunity in this run. Check route reachability and the spacing between woodland and activity nodes.";
            }

            return
                "Animals reached feeding opportunities without a recorded avoidance response. Compare this with completed human trips before keeping the layout.";
        }

        public static string FeederLabel(P0PredictedFeeder prediction)
        {
            return prediction == P0PredictedFeeder.None ? "Not set" : prediction.ToString();
        }

        public static string ConflictAreaLabel(P0PredictedConflictArea prediction)
        {
            switch (prediction)
            {
                case P0PredictedConflictArea.MainRoute:
                    return "Main route";
                case P0PredictedConflictArea.ActivityNode:
                    return "Activity node";
                case P0PredictedConflictArea.WoodlandEdge:
                    return "Woodland edge";
                default:
                    return "Not set";
            }
        }

        private static string ActualTopFeeder(int pigeon, int squirrel, int fox)
        {
            int maximum = Math.Max(pigeon, Math.Max(squirrel, fox));
            int matches = (pigeon == maximum ? 1 : 0) +
                (squirrel == maximum ? 1 : 0) +
                (fox == maximum ? 1 : 0);
            if (matches != 1)
            {
                return "Mixed";
            }

            if (pigeon == maximum)
            {
                return "Pigeon";
            }
            return squirrel == maximum ? "Squirrel" : "Fox";
        }
    }
}

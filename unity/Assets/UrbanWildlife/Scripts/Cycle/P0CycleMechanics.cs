using System;
using System.Collections.Generic;

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

    public sealed class P0CycleScore
    {
        private P0CycleScore(
            int humanAccessScore,
            int pigeonFeedingScore,
            int squirrelFeedingScore,
            int foxFeedingScore)
        {
            HumanAccessScore = humanAccessScore;
            PigeonFeedingScore = pigeonFeedingScore;
            SquirrelFeedingScore = squirrelFeedingScore;
            FoxFeedingScore = foxFeedingScore;
        }

        public const string FormulaVersion = "P0_SCORE_V0.1";
        public const int HumanAccessMaximum = 40;
        public const int SpeciesFeedingMaximum = 20;

        public int HumanAccessScore { get; }
        public int PigeonFeedingScore { get; }
        public int SquirrelFeedingScore { get; }
        public int FoxFeedingScore { get; }
        public int TotalScore =>
            HumanAccessScore + PigeonFeedingScore + SquirrelFeedingScore + FoxFeedingScore;

        public static P0CycleScore Calculate(
            int successfulHumans,
            int humanCount,
            int fedPigeons,
            int pigeonCount,
            int fedSquirrels,
            int squirrelCount,
            int fedFoxes,
            int foxCount)
        {
            return new P0CycleScore(
                ProportionalScore(successfulHumans, humanCount, HumanAccessMaximum),
                ProportionalScore(fedPigeons, pigeonCount, SpeciesFeedingMaximum),
                ProportionalScore(fedSquirrels, squirrelCount, SpeciesFeedingMaximum),
                ProportionalScore(fedFoxes, foxCount, SpeciesFeedingMaximum));
        }

        public string Summary()
        {
            return
                $"TOTAL {TotalScore}/100 · Human access {HumanAccessScore}/40 · " +
                $"Pigeon {PigeonFeedingScore}/20 · Squirrel {SquirrelFeedingScore}/20 · Fox {FoxFeedingScore}/20";
        }

        private static int ProportionalScore(int successfulAgents, int agentCount, int maximum)
        {
            if (agentCount <= 0)
            {
                return 0;
            }

            float ratio = Math.Min(Math.Max(0, successfulAgents), agentCount) / (float)agentCount;
            return (int)Math.Round(maximum * ratio, MidpointRounding.AwayFromZero);
        }
    }

    public sealed class P0CycleMemorySnapshot
    {
        public P0CycleMemorySnapshot(
            int sourceCycleIndex,
            int humanTrips,
            int pigeonFeedEvents,
            int squirrelFeedEvents,
            int foxFeedEvents,
            int avoidanceEvents,
            int pigeonPreferredFoodTokenId,
            int squirrelPreferredFoodTokenId,
            int foxPreferredFoodTokenId,
            float squirrelFamiliarity,
            P0CycleScore score = null,
            float humanTraceDistanceUnits = 0f,
            float animalTraceDistanceUnits = 0f)
        {
            SourceCycleIndex = sourceCycleIndex;
            HumanTrips = Math.Max(0, humanTrips);
            PigeonFeedEvents = Math.Max(0, pigeonFeedEvents);
            SquirrelFeedEvents = Math.Max(0, squirrelFeedEvents);
            FoxFeedEvents = Math.Max(0, foxFeedEvents);
            AvoidanceEvents = Math.Max(0, avoidanceEvents);
            PigeonPreferredFoodTokenId = pigeonPreferredFoodTokenId;
            SquirrelPreferredFoodTokenId = squirrelPreferredFoodTokenId;
            FoxPreferredFoodTokenId = foxPreferredFoodTokenId;
            SquirrelFamiliarity = Math.Max(0f, squirrelFamiliarity);
            Score = score;
            HumanTraceDistanceUnits = Math.Max(0f, humanTraceDistanceUnits);
            AnimalTraceDistanceUnits = Math.Max(0f, animalTraceDistanceUnits);
        }

        public int SourceCycleIndex { get; }
        public int HumanTrips { get; }
        public int PigeonFeedEvents { get; }
        public int SquirrelFeedEvents { get; }
        public int FoxFeedEvents { get; }
        public int AvoidanceEvents { get; }
        public int PigeonPreferredFoodTokenId { get; }
        public int SquirrelPreferredFoodTokenId { get; }
        public int FoxPreferredFoodTokenId { get; }
        public float SquirrelFamiliarity { get; }
        public P0CycleScore Score { get; }
        public float HumanTraceDistanceUnits { get; }
        public float AnimalTraceDistanceUnits { get; }
    }

    public sealed class P0CycleMechanics
    {
        private readonly List<P0CycleMemorySnapshot> memoryHistory =
            new List<P0CycleMemorySnapshot>();

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
        public P0CycleMemorySnapshot LastMemory =>
            memoryHistory.Count == 0 ? null : memoryHistory[memoryHistory.Count - 1];
        public int MemoryCount => memoryHistory.Count;

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

        public void ResetSession()
        {
            memoryHistory.Clear();
            BeginCycle(0);
        }

        public bool RecordOutcome(P0CycleMemorySnapshot snapshot)
        {
            if (snapshot == null || snapshot.SourceCycleIndex != CycleIndex)
            {
                return false;
            }

            if (LastMemory != null && LastMemory.SourceCycleIndex == snapshot.SourceCycleIndex)
            {
                memoryHistory[memoryHistory.Count - 1] = snapshot;
            }
            else
            {
                memoryHistory.Add(snapshot);
            }
            return true;
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

        public string MemorySummary()
        {
            P0CycleMemorySnapshot memory = LastMemory;
            if (memory == null)
            {
                return "No previous-cycle memory";
            }

            string scoreText = memory.Score == null
                ? string.Empty
                : $" Planning score {memory.Score.TotalScore}/100.";
            string traceText =
                $" Traces: human {memory.HumanTraceDistanceUnits * 10f:0} cm, " +
                $"animal {memory.AnimalTraceDistanceUnits * 10f:0} cm.";
            return
                $"Cycle {memory.SourceCycleIndex + 1}: people completed {memory.HumanTrips} trips; " +
                $"feeds P{memory.PigeonFeedEvents}/S{memory.SquirrelFeedEvents}/F{memory.FoxFeedEvents}; " +
                $"avoidance {memory.AvoidanceEvents}. Remembered nodes: " +
                $"P{TokenLabel(memory.PigeonPreferredFoodTokenId)}, " +
                $"S{TokenLabel(memory.SquirrelPreferredFoodTokenId)}, " +
                $"F{TokenLabel(memory.FoxPreferredFoodTokenId)}.{scoreText}{traceText}";
        }

        public string MemoryEffectSummary()
        {
            P0CycleMemorySnapshot memory = LastMemory;
            if (memory == null)
            {
                return "The first cycle starts without learned site preference.";
            }

            int cautionPercent = MemoryCautionPercent(memory.AvoidanceEvents);
            return
                "At least half of each returning species will revisit its remembered activity node when one exists. " +
                $"Squirrel familiarity carries forward at 80%; cautious-species response distance is +{cautionPercent}%.";
        }

        public static float CarriedSquirrelFamiliarity(P0CycleMemorySnapshot memory)
        {
            return memory == null ? 0f : memory.SquirrelFamiliarity * 0.8f;
        }

        public static float MemoryCautionMultiplier(P0CycleMemorySnapshot memory)
        {
            return memory == null
                ? 1f
                : 1f + MemoryCautionPercent(memory.AvoidanceEvents) / 100f;
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

        private static int MemoryCautionPercent(int avoidanceEvents)
        {
            return Math.Min(25, Math.Max(0, avoidanceEvents) * 2);
        }

        private static string TokenLabel(int tokenId)
        {
            return tokenId < 0 ? "—" : tokenId.ToString();
        }
    }
}

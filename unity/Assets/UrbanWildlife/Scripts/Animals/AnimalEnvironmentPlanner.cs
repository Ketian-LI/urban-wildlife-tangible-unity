using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Cycle;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Animals
{
    public static class AnimalEnvironmentPlanner
    {
        public static AnimalSpawnPlan[] CreatePlans(LayoutPacket packet, P0Scenario scenario)
        {
            return CreatePlans(packet, scenario, 1, 1, 1);
        }

        public static AnimalSpawnPlan[] CreatePlans(
            LayoutPacket packet,
            P0Scenario scenario,
            int pigeonCount,
            int squirrelCount,
            int foxCount)
        {
            return CreatePlans(
                packet,
                scenario,
                pigeonCount,
                squirrelCount,
                foxCount,
                null);
        }

        public static AnimalSpawnPlan[] CreatePlans(
            LayoutPacket packet,
            P0Scenario scenario,
            int pigeonCount,
            int squirrelCount,
            int foxCount,
            P0CycleMemorySnapshot memory)
        {
            if (packet?.tokens == null || scenario?.animal_simulation == null || scenario.board == null)
            {
                throw new ArgumentException("A confirmed layout and animal simulation parameters are required.");
            }

            LayoutToken[] foods = packet.tokens
                .Where(token => token.type == "food_hotspot")
                .OrderBy(token => token.id)
                .ToArray();
            LayoutToken[] woodlands = packet.tokens
                .Where(token => token.type == "woodland")
                .OrderBy(token => token.id)
                .ToArray();
            if (foods.Length < 3 || woodlands.Length < 2)
            {
                throw new ArgumentException("P0 animal plans require three Food Hotspots and two Woodlands.");
            }
            if (pigeonCount < 0 || squirrelCount < 0 || foxCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pigeonCount), "Animal counts cannot be negative.");
            }

            ScenarioAnimalSimulation settings = scenario.animal_simulation;
            AnimalMovementObstacle[] obstacles = AnimalObstacleAvoidance.BuildObstacles(packet, scenario);
            Vector2 boardHalfExtents = new Vector2(
                scenario.board.width_cm * settings.unity_units_per_cm * 0.5f - 0.28f,
                scenario.board.height_cm * settings.unity_units_per_cm * 0.5f - 0.28f);
            Vector2 pigeonStart = new Vector2(
                0f,
                scenario.board.height_cm * settings.unity_units_per_cm * 0.42f);
            List<AnimalSpawnPlan> plans = new List<AnimalSpawnPlan>();
            for (int index = 0; index < pigeonCount; index += 1)
            {
                LayoutToken food = SelectFood(
                    foods,
                    index,
                    pigeonCount,
                    0,
                    memory?.PigeonPreferredFoodTokenId ?? -1);
                Vector2 offset = ScatterOffset(index, pigeonCount, 0.10f);
                plans.Add(new AnimalSpawnPlan(
                    CreateConfig(AnimalSpecies.Pigeon, settings, memory),
                    pigeonStart + offset,
                    ToLocal(food, scenario, settings.unity_units_per_cm),
                    pigeonStart + offset,
                    food.id,
                    -1));
            }

            for (int index = 0; index < squirrelCount; index += 1)
            {
                LayoutToken woodland = woodlands[index % woodlands.Length];
                LayoutToken food = SelectFood(
                    foods,
                    index,
                    squirrelCount,
                    1,
                    memory?.SquirrelPreferredFoodTokenId ?? -1);
                Vector2 foodPosition = ToLocal(food, scenario, settings.unity_units_per_cm);
                Vector2 woodlandCentre = ToLocal(woodland, scenario, settings.unity_units_per_cm);
                Vector2 shelter = AnimalObstacleAvoidance.HabitatAccessPoint(
                    woodlandCentre,
                    foodPosition + ScatterOffset(index, squirrelCount, 0.08f),
                    AnimalSpecies.Squirrel,
                    obstacles,
                    boardHalfExtents);
                plans.Add(new AnimalSpawnPlan(
                    CreateConfig(AnimalSpecies.Squirrel, settings, memory),
                    shelter,
                    foodPosition,
                    shelter,
                    food.id,
                    woodland.id));
            }

            for (int index = 0; index < foxCount; index += 1)
            {
                LayoutToken woodland = woodlands[(index + 1) % woodlands.Length];
                LayoutToken food = SelectFood(
                    foods,
                    index,
                    foxCount,
                    2,
                    memory?.FoxPreferredFoodTokenId ?? -1);
                Vector2 foodPosition = ToLocal(food, scenario, settings.unity_units_per_cm);
                Vector2 woodlandCentre = ToLocal(woodland, scenario, settings.unity_units_per_cm);
                Vector2 shelter = AnimalObstacleAvoidance.HabitatAccessPoint(
                    woodlandCentre,
                    foodPosition + ScatterOffset(index, foxCount, 0.12f),
                    AnimalSpecies.Fox,
                    obstacles,
                    boardHalfExtents);
                plans.Add(new AnimalSpawnPlan(
                    CreateConfig(AnimalSpecies.Fox, settings, memory),
                    shelter,
                    foodPosition,
                    shelter,
                    food.id,
                    woodland.id));
            }
            return plans.ToArray();
        }

        private static Vector2 ScatterOffset(int index, int count, float spacing)
        {
            if (count <= 1)
            {
                return Vector2.zero;
            }

            float angle = index * 137.5f * Mathf.Deg2Rad;
            float radius = spacing * (1f + index / 4f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        private static LayoutToken SelectFood(
            LayoutToken[] foods,
            int index,
            int speciesCount,
            int defaultOffset,
            int preferredFoodTokenId)
        {
            LayoutToken preferred = Array.Find(foods, food => food.id == preferredFoodTokenId);
            int returningCount = (speciesCount + 1) / 2;
            if (preferred != null && index < returningCount)
            {
                return preferred;
            }

            return foods[(index + defaultOffset) % foods.Length];
        }

        private static AnimalAgentConfig CreateConfig(
            AnimalSpecies species,
            ScenarioAnimalSimulation settings,
            P0CycleMemorySnapshot memory)
        {
            float scale = settings.unity_units_per_cm;
            float cautionMultiplier = P0CycleMechanics.MemoryCautionMultiplier(memory);
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return new AnimalAgentConfig
                    {
                        Species = species,
                        SpeedUnitsPerSecond = settings.pigeon_speed_cm_per_second * scale,
                        DetectionRadiusUnits = settings.pigeon_food_detection_cm * scale,
                        DisturbanceRadiusUnits = 0f,
                        FeedSeconds = settings.pigeon_feed_seconds,
                        PostFeedStaySeconds = settings.pigeon_stay_seconds,
                        RestSeconds = 0f,
                        AvoidsHumans = false,
                    };
                case AnimalSpecies.Squirrel:
                    return new AnimalAgentConfig
                    {
                        Species = species,
                        SpeedUnitsPerSecond = settings.squirrel_speed_cm_per_second * scale,
                        DetectionRadiusUnits = settings.squirrel_food_detection_cm * scale,
                        DisturbanceRadiusUnits = settings.squirrel_disturbance_cm * scale * cautionMultiplier,
                        FeedSeconds = settings.squirrel_feed_seconds,
                        RestSeconds = settings.squirrel_rest_seconds,
                        AvoidsHumans = true,
                        InitialFamiliarity = Mathf.Clamp(
                            Mathf.Max(
                                settings.squirrel_initial_familiarity,
                                P0CycleMechanics.CarriedSquirrelFamiliarity(memory)),
                            0f,
                            settings.squirrel_maximum_familiarity),
                        FamiliarityGainPerFeed = settings.squirrel_familiarity_gain_per_feed,
                        MaximumFamiliarity = settings.squirrel_maximum_familiarity,
                    };
                default:
                    return new AnimalAgentConfig
                    {
                        Species = species,
                        SpeedUnitsPerSecond = settings.fox_speed_cm_per_second * scale,
                        DetectionRadiusUnits = settings.fox_food_detection_cm * scale,
                        DisturbanceRadiusUnits = settings.fox_disturbance_cm * scale * cautionMultiplier,
                        FeedSeconds = settings.fox_feed_seconds,
                        RestSeconds = settings.fox_rest_seconds,
                        AvoidsHumans = true,
                    };
            }
        }

        private static Vector2 ToLocal(LayoutToken token, P0Scenario scenario, float scale)
        {
            return new Vector2(
                (token.x_norm - 0.5f) * scenario.board.width_cm * scale,
                (0.5f - token.y_norm) * scenario.board.height_cm * scale);
        }
    }
}

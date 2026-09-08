using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Animals
{
    public static class AnimalEnvironmentPlanner
    {
        public static AnimalSpawnPlan[] CreatePlans(LayoutPacket packet, P0Scenario scenario)
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

            ScenarioAnimalSimulation settings = scenario.animal_simulation;
            Vector2 pigeonStart = new Vector2(
                0f,
                scenario.board.height_cm * settings.unity_units_per_cm * 0.42f);
            Vector2 squirrelShelter = ToLocal(woodlands[0], scenario, settings.unity_units_per_cm);
            Vector2 foxShelter = ToLocal(woodlands[1], scenario, settings.unity_units_per_cm);
            List<AnimalSpawnPlan> plans = new List<AnimalSpawnPlan>
            {
                new AnimalSpawnPlan(
                    CreateConfig(AnimalSpecies.Pigeon, settings),
                    pigeonStart,
                    ToLocal(foods[0], scenario, settings.unity_units_per_cm),
                    pigeonStart,
                    foods[0].id,
                    -1),
                new AnimalSpawnPlan(
                    CreateConfig(AnimalSpecies.Squirrel, settings),
                    squirrelShelter,
                    ToLocal(foods[1], scenario, settings.unity_units_per_cm),
                    squirrelShelter,
                    foods[1].id,
                    woodlands[0].id),
                new AnimalSpawnPlan(
                    CreateConfig(AnimalSpecies.Fox, settings),
                    foxShelter,
                    ToLocal(foods[2], scenario, settings.unity_units_per_cm),
                    foxShelter,
                    foods[2].id,
                    woodlands[1].id),
            };
            return plans.ToArray();
        }

        private static AnimalAgentConfig CreateConfig(
            AnimalSpecies species,
            ScenarioAnimalSimulation settings)
        {
            float scale = settings.unity_units_per_cm;
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
                        DisturbanceRadiusUnits = settings.squirrel_disturbance_cm * scale,
                        FeedSeconds = settings.squirrel_feed_seconds,
                        RestSeconds = settings.squirrel_rest_seconds,
                        AvoidsHumans = true,
                        InitialFamiliarity = settings.squirrel_initial_familiarity,
                        FamiliarityGainPerFeed = settings.squirrel_familiarity_gain_per_feed,
                        MaximumFamiliarity = settings.squirrel_maximum_familiarity,
                    };
                default:
                    return new AnimalAgentConfig
                    {
                        Species = species,
                        SpeedUnitsPerSecond = settings.fox_speed_cm_per_second * scale,
                        DetectionRadiusUnits = settings.fox_food_detection_cm * scale,
                        DisturbanceRadiusUnits = settings.fox_disturbance_cm * scale,
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

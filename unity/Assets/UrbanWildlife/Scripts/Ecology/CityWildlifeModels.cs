using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UrbanWildlife.Ecology
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityWildlifeSpecies
    {
        Pigeon,
        GreySquirrel,
        Fox,
        Hedgehog,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityWildlifeLifeState
    {
        Active,
        Migrated,
        Dead,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityWildlifeMemoryType
    {
        Resource,
        HumanFood,
        Threat,
        Traffic,
        Shelter,
        Crossing,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityWildlifeOutcomeType
    {
        Fed,
        AvoidedDisturbance,
        MigratedOut,
        MigratedIn,
        Roadkill,
    }

    [Serializable]
    public sealed class CitySpeciesProfile
    {
        public CityWildlifeSpecies species;
        public float natural_food_weight;
        public float anthropogenic_food_weight;
        public float shelter_weight;
        public float disturbance_weight;
        public float traffic_weight;
        public float travel_cost_weight;
        public float positive_memory_weight;
        public float negative_memory_weight;
        public float speed_units_per_second;
        public float collision_radius_units;
        public bool ground_collision_vulnerable;
    }

    [Serializable]
    public sealed class CityWildlifeMemory
    {
        public CityWildlifeMemoryType type;
        public string location_id;
        public float[] position_norm;
        public float strength;
        public bool negative;
        public int age_cycles;
    }

    [Serializable]
    public sealed class CityWildlifeAgent
    {
        public string id;
        public CityWildlifeSpecies species;
        public CityWildlifeLifeState life_state;
        public float[] position_norm;
        public string target_patch_id;
        public float target_utility;
        public float low_utility_elapsed_seconds;
        public int feed_events;
        public CityWildlifeMemory[] memories = Array.Empty<CityWildlifeMemory>();
    }

    [Serializable]
    public sealed class CityWildlifeOutcome
    {
        public CityWildlifeOutcomeType type;
        public string agent_id;
        public CityWildlifeSpecies species;
        public string location_id;
        public string cause_id;
        public int cycle_index;
        public float[] position_norm;
    }

    [Serializable]
    public sealed class CityWildlifeSnapshot
    {
        public int cycle_index;
        public int active_count;
        public int migrated_count;
        public int dead_count;
        public int feeding_events;
        public int roadkill_events;
        public CityWildlifeAgent[] agents = Array.Empty<CityWildlifeAgent>();
        public CityWildlifeOutcome[] permanent_outcomes = Array.Empty<CityWildlifeOutcome>();
    }

    [Serializable]
    public sealed class CityWildlifeConfiguration
    {
        public int memory_limit = 8;
        public float positive_memory_decay_per_cycle = 0.22f;
        public float negative_memory_decay_per_cycle = 0.12f;
        public float migration_utility_threshold = -0.18f;
        public float migration_grace_seconds = 16f;
        public float target_reached_units = 0.75f;
        public float feed_interval_seconds = 8f;
    }
}

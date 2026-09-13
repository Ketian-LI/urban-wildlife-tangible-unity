using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UrbanWildlife.Strategy
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityDevelopmentPhase
    {
        PopulationGrowth,
        PublicLife,
        WastePressure,
        MobilityPressure,
        Redevelopment,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTimeBlock
    {
        Quiet,
        Active,
        Peak,
        Late,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityStrategyActionType
    {
        ConstructBuilding,
        ConstructGreenPatch,
        ConstructRoad,
        ConstructFootpath,
        InstallBench,
        InstallBin,
        Demolish,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityStrategyProjectState
    {
        Active,
        Complete,
        Cancelled,
    }

    [Serializable]
    public sealed class CityStrategyProject
    {
        public string id;
        public CityStrategyActionType action_type;
        public CityStrategyProjectState state;
        public string target_id;
        public int dp_cost;
        public int total_time_blocks;
        public int remaining_time_blocks;
        public float construction_disturbance;
        public float access_obstruction;
    }

    [Serializable]
    public sealed class CityBalanceScore
    {
        public const string FormulaVersion = "city-balance-v0.1";
        public float development;
        public float accessibility;
        public float waste_management;
        public float habitat_connectivity;
        public float wildlife_safety;
        public float total;
    }

    [Serializable]
    public sealed class CityStrategySnapshot
    {
        public CityDevelopmentPhase phase;
        public CityTimeBlock time_block;
        public int development_points;
        public float block_elapsed_seconds;
        public int completed_phase_count;
        public float active_construction_disturbance;
        public float active_access_obstruction;
        public CityStrategyProject[] projects = Array.Empty<CityStrategyProject>();
        public CityBalanceScore balance = new CityBalanceScore();
    }

    [Serializable]
    public sealed class CityStrategyConfiguration
    {
        public int starting_development_points = 20;
        public int phase_development_point_grant = 8;
        public int time_blocks_per_phase = 4;
        public float time_block_seconds = 15f;
    }
}

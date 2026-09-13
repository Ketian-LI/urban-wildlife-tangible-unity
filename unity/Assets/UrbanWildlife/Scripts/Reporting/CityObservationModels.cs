using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UrbanWildlife.Ecology;
using UrbanWildlife.Strategy;

namespace UrbanWildlife.Reporting
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTraceLayer
    {
        Human,
        Animal,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTraceDisplayMode
    {
        HumanTrace,
        AnimalTrace,
        CombinedTrace,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTraceMark
    {
        Footprint,
        VehicleTyre,
        BirdTrack,
        SmallPaw,
        FoxPaw,
        HedgehogTrack,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityFeedEventType
    {
        WasteOverflow,
        Crowding,
        Feeding,
        MigrationIn,
        MigrationOut,
        Roadkill,
        ProjectCompleted,
        BalanceWarning,
    }

    [Serializable]
    public sealed class CityTracePoint
    {
        public string agent_id;
        public CityTraceLayer layer;
        public CityTraceMark mark;
        public float[] position_norm;
        public float elapsed_seconds;
        public int cycle_index;
        public CityDevelopmentPhase phase;
    }

    [Serializable]
    public sealed class CityFeedEntry
    {
        public string id;
        public CityFeedEventType type;
        public string subject_id;
        public string cause_id;
        public string message;
        public float elapsed_seconds;
        public int cycle_index;
        public CityDevelopmentPhase phase;
    }

    [Serializable]
    public sealed class CityPhaseReport
    {
        public CityDevelopmentPhase phase;
        public CityBalanceScore before;
        public CityBalanceScore after;
        public CityBalanceScore change;
        public int human_trace_points;
        public int animal_trace_points;
        public int feeding_events;
        public int migration_events;
        public int roadkill_events;
        public int overflow_events;
        public CityFeedEntry[] city_feed = Array.Empty<CityFeedEntry>();
    }

    [Serializable]
    public sealed class CityObservationSnapshot
    {
        public float elapsed_seconds;
        public CityTracePoint[] trace_points = Array.Empty<CityTracePoint>();
        public CityFeedEntry[] city_feed = Array.Empty<CityFeedEntry>();
    }

    [Serializable]
    public sealed class CityResearchLogRecord
    {
        public string timestamp_utc;
        public string session_id;
        public string event_type;
        public string subject_id;
        public string cause_id;
        public int cycle_index;
        public string phase;
        public string time_block;
        public int development_points;
        public float city_balance_total;
        public float development;
        public float accessibility;
        public float waste_management;
        public float habitat_connectivity;
        public float wildlife_safety;
        public float waste_demand;
        public float waste_capacity;
        public bool overflow_active;
        public int active_wildlife;
        public int feeding_events;
        public int migration_events;
        public int roadkill_events;
        public int human_trace_points;
        public int animal_trace_points;
        public string note;
    }
}

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UrbanWildlife.Environmental
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityFoodSourceKind
    {
        Natural,
        Anthropogenic,
    }

    [Serializable]
    public sealed class CityFoodSource
    {
        public string id;
        public CityFoodSourceKind kind;
        public string source_id;
        public float[] position_norm;
        public float availability;
        public bool caused_by_overflow;
    }

    [Serializable]
    public sealed class CityPatchPressure
    {
        public string patch_id;
        public float[] position_norm;
        public float natural_food;
        public float anthropogenic_food;
        public float shelter;
        public float human_disturbance;
        public float traffic_exposure;
        public float litter_pressure;
    }

    [Serializable]
    public sealed class CityEnvironmentSnapshot
    {
        public float natural_food_total;
        public float anthropogenic_food_total;
        public float average_disturbance;
        public float waste_demand;
        public float waste_capacity;
        public float waste_utilisation;
        public bool overflow_active;
        public float overflow_elapsed_seconds;
        public CityFoodSource[] food_sources = Array.Empty<CityFoodSource>();
        public CityPatchPressure[] patch_pressures = Array.Empty<CityPatchPressure>();
    }

    [Serializable]
    public sealed class CityEnvironmentConfiguration
    {
        public float overflow_grace_seconds = 5f;
        public float building_influence_radius_units = 18f;
        public float road_influence_radius_units = 12f;
        public float litter_influence_radius_units = 16f;
        public float overflow_food_multiplier = 0.6f;
    }
}

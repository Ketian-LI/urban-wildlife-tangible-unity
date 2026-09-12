using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UrbanWildlife.Input
{
    public static class CityTokenScanContract
    {
        public const string SchemaVersion = "0.1";
        public const string PacketType = "city_token_scan";
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityPhysicalTokenType
    {
        Apartment,
        DetachedHouse,
        Commercial,
        CommunityFacility,
        GreenIntervention,
    }

    [Serializable]
    public sealed class CityTokenScanPacket
    {
        public string schema_version;
        public string packet_type;
        public long timestamp_ms;
        public string timestamp_utc;
        public string scan_id;
        public string session_id;
        public int development_phase;
        public string calibration_id;
        public LayoutCoordinateSystem coordinate_system;
        public LayoutCapture capture;
        public CityTokenRecognition recognition;
        public CityTokenState[] token_states = Array.Empty<CityTokenState>();
    }

    [Serializable]
    public sealed class CityTokenRecognition
    {
        public string marker_backend;
        public string marker_family;
        public string token_config_version;
    }

    [Serializable]
    public sealed class CityTokenState
    {
        public int id;
        public CityPhysicalTokenType type;
        public float x_norm;
        public float y_norm;
        public float rotation_deg;
        public float confidence;
    }
}

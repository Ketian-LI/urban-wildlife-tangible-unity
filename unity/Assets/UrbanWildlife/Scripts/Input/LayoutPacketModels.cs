using System;

namespace UrbanWildlife.Input
{
    [Serializable]
    public sealed class LayoutPacket
    {
        public string schema_version;
        public string packet_type;
        public long timestamp_ms;
        public string timestamp_utc;
        public string session_id;
        public int cycle_index;
        public string scenario_id;
        public string calibration_id;
        public LayoutCoordinateSystem coordinate_system;
        public LayoutCapture capture;
        public LayoutRecognition recognition;
        public LayoutToken[] tokens;
        public LayoutPath path;
        public LayoutValidation validation;
    }

    [Serializable]
    public sealed class LayoutCapture
    {
        public string mode;
        public bool stable;
        public float stable_seconds_required;
        public float elapsed_seconds;
        public float maximum_changed_fraction;
        public float last_changed_fraction;
        public int frames_observed;
        public int difference_pixel_threshold;
        public int analysis_width;
        public int source;
        public int[] actual_frame_size;
        public string note;
    }

    [Serializable]
    public sealed class LayoutCoordinateSystem
    {
        public string origin;
        public string x_axis;
        public string y_axis;
        public float[] range;
        public string unity_plane_mapping;
    }

    [Serializable]
    public sealed class LayoutRecognition
    {
        public string token_backend;
        public string token_config_version;
    }

    [Serializable]
    public sealed class LayoutToken
    {
        public int id;
        public string type;
        public float x_norm;
        public float y_norm;
        public float angle_deg;
        public bool in_bounds;
        public float confidence;
    }

    [Serializable]
    public sealed class LayoutPath
    {
        public string format;
        public string preset;
        public float[][] points_norm;
        public int point_count;
        public bool continuous;
        public int component_count;
        public float mask_area_fraction;
    }

    [Serializable]
    public sealed class LayoutValidation
    {
        public bool all_tokens_in_bounds;
        public bool path_detected;
        public bool path_continuous;
        public int[] required_token_ids;
        public int[] missing_token_ids;
        public int[] duplicate_token_ids;
        public bool all_required_tokens_detected;
    }
}

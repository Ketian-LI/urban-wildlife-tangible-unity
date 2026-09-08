using System;

namespace UrbanWildlife.Planning
{
    [Serializable]
    public sealed class P0Scenario
    {
        public string schema_version;
        public string scenario_id;
        public string scenario_name;
        public ScenarioBoard board;
        public ScenarioFixedRegions fixed_regions;
        public ScenarioConstraints constraints;
        public ScenarioBaselineLayout baseline_layout;
        public ScenarioExpectedConstraintCheck expected_constraint_check_before_player_changes;
    }

    [Serializable]
    public sealed class ScenarioBoard
    {
        public string origin;
        public float width_cm;
        public float height_cm;
    }

    [Serializable]
    public sealed class ScenarioFixedRegions
    {
        public ScenarioRegion entrance_a;
        public ScenarioRegion exit_b;
        public ScenarioRegion central_plaza;
        public ScenarioRegion pond;
    }

    [Serializable]
    public sealed class ScenarioRegion
    {
        public string shape;
        public float[] center_cm;
        public float[] center_norm;
        public float[] size_cm;
        public float[] bounding_size_cm;
    }

    [Serializable]
    public sealed class ScenarioConstraints
    {
        public float food_path_influence_max_cm;
        public float food_path_edge_clearance_cm;
        public float food_token_radius_cm;
        public float path_width_cm;
        public int changes_allowed;
        public float token_move_tolerance_cm;
        public float path_change_tolerance_cm;
        public string status;
    }

    [Serializable]
    public sealed class ScenarioBaselineLayout
    {
        public ScenarioToken[] tokens;
        public ScenarioPath planned_human_path;
    }

    [Serializable]
    public sealed class ScenarioToken
    {
        public int id;
        public string type;
        public float[] center_cm;
        public float[] center_norm;
    }

    [Serializable]
    public sealed class ScenarioPath
    {
        public string format;
        public float width_cm;
        public float[][] points_cm;
        public float[][] points_norm;
        public bool continuous;
        public bool self_intersects;
        public string[] required_order;
    }

    [Serializable]
    public sealed class ScenarioExpectedConstraintCheck
    {
        public bool human_connected;
        public bool animal_reachable;
        public bool food_hotspot_valid;
        public int changes_used;
        public int changes_allowed;
    }

    [Serializable]
    public sealed class P0ConstraintResult
    {
        public bool human_connected;
        public bool animal_reachable;
        public bool food_hotspot_valid;
        public int changes_used;
        public int changes_allowed;
        public bool within_change_budget;
        public bool all_constraints_satisfied;
    }
}

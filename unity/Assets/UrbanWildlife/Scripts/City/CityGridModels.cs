using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UrbanWildlife.City
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityLandCover
    {
        OpenLand,
        Woodland,
        PublicGreen,
        Water,
        CivicPlaza,
        Building,
        ShrubGarden,
        Disturbed,
        Recovering,
    }

    [Serializable]
    public sealed class CityGridCell
    {
        public string id;
        public int row;
        public int col;
        public float[] center_norm;
        public float[] size_norm;
        public CityLandCover baseline_cover;
        public CityLandCover current_cover;
        public bool buildable;
        public bool fixed_feature;
        public string occupant_id;
        public string habitat_patch_id;
        public bool was_woodland;
        public int last_changed_revision;
    }

    [Serializable]
    public sealed class CityPlanningGrid
    {
        public const int DefaultColumns = 6;
        public const int DefaultRows = 4;
        public const int DefaultCellCount = DefaultColumns * DefaultRows;

        public int cols = DefaultColumns;
        public int rows = DefaultRows;
        public int max_active_player_buildings = 9;
        public CityGridCell[] cells = Array.Empty<CityGridCell>();
    }
}

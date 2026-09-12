using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UrbanWildlife.City;

namespace UrbanWildlife.Networks
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityNetworkPlanningPhase
    {
        Planning,
        RouteSelection,
    }

    [Serializable]
    public sealed class CityRoadCandidate
    {
        public string id;
        public string building_id;
        public string connection_road_id;
        public CityRoadRouteOption route_option;
        public float[][] points_norm;
        public float estimated_length_units;
        public float estimated_green_impact_units;
        public float estimated_traffic_pressure;
        public string tradeoff_summary;
    }

    [Serializable]
    public sealed class CityRoadChoiceSet
    {
        public string building_id;
        public CityRoadCandidate[] candidates = Array.Empty<CityRoadCandidate>();
        public string selected_candidate_id;

        public bool HasSelection => SelectedCandidate != null;

        public CityRoadCandidate SelectedCandidate => candidates.FirstOrDefault(candidate =>
            candidate.id == selected_candidate_id);
    }

    [Serializable]
    public sealed class CityNetworkPlanPreview
    {
        public int source_city_revision;
        public CityRoadChoiceSet[] road_choices = Array.Empty<CityRoadChoiceSet>();
        public CityPedestrianLink[] automatic_pedestrian_links = Array.Empty<CityPedestrianLink>();
        public CityPedestrianLink[] screen_edited_extra_footpaths = Array.Empty<CityPedestrianLink>();
        public bool screen_footpaths_changed;

        public int RequiredRoadChoiceCount => road_choices.Length;
        public int SelectedRoadChoiceCount => road_choices.Count(choice => choice.HasSelection);
        public bool AllRequiredRoadsSelected =>
            SelectedRoadChoiceCount == RequiredRoadChoiceCount;
        public bool HasNetworkChanges => RequiredRoadChoiceCount > 0 ||
                                         automatic_pedestrian_links.Length > 0 ||
                                         screen_footpaths_changed;
        public bool CanConfirm => AllRequiredRoadsSelected && HasNetworkChanges;
    }
}

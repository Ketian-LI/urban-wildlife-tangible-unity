using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UrbanWildlife.City;
using UrbanWildlife.Construction;
using UrbanWildlife.Networks;
using UrbanWildlife.Strategy;

namespace UrbanWildlife.Planning
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityPlanningWorkflowPhase
    {
        ReadyToScan,
        Preview,
        RouteSelection,
        ReadyToBuild,
        Construction,
        Complete,
    }

    [Serializable]
    public sealed class CityPlanningWorkflowSnapshot
    {
        public CityPlanningWorkflowPhase phase;
        public string message;
        public CityConstructionPreview construction_preview;
        public CityNetworkPlanPreview network_preview;
        public CityStrategySnapshot strategy;
        public int required_development_points;
        public float time_scale;
        public string[] proposed_object_ids = Array.Empty<string>();
        public string[] completed_object_ids = Array.Empty<string>();

        public bool CanConfirmPreview => phase == CityPlanningWorkflowPhase.Preview &&
                                         construction_preview != null &&
                                         construction_preview.CanConfirmConstruction;
        public bool CanConfirmRoutes => phase == CityPlanningWorkflowPhase.RouteSelection &&
                                        network_preview != null &&
                                        network_preview.CanConfirm;
        public bool CanStartConstruction => phase == CityPlanningWorkflowPhase.ReadyToBuild &&
                                            strategy != null &&
                                            strategy.development_points >= required_development_points;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UrbanWildlife.City;
using UrbanWildlife.Construction;
using UrbanWildlife.Ecology;
using UrbanWildlife.Environmental;
using UrbanWildlife.Input;
using UrbanWildlife.Networks;
using UrbanWildlife.Strategy;

namespace UrbanWildlife.Planning
{
    public sealed class CityPlanningWorkflow
    {
        private sealed class ProjectRequest
        {
            public CityStrategyActionType action;
            public string targetId;
        }

        private CityConstructionManager construction;
        private CityNetworkPlanningManager networks;
        private CityStrategySimulation strategy;
        private CityState currentState;
        private CityTokenState[] confirmedTokens;
        private long lastAcceptedTimestampMs = -1;
        private float timeScale = 1f;

        public CityPlanningWorkflow(
            CityState initialState,
            IEnumerable<CityTokenState> initialConfirmedTokens)
        {
            CityStateValidationResult validation = CityStateValidator.Validate(initialState);
            if (!validation.IsValid)
            {
                throw new ArgumentException(validation.Summary, nameof(initialState));
            }
            currentState = initialState;
            confirmedTokens = CloneTokens(initialConfirmedTokens);
            construction = new CityConstructionManager(initialState, confirmedTokens);
            Phase = CityPlanningWorkflowPhase.ReadyToScan;
            LastMessage = "Ready to Scan City. New objects stay in Preview until confirmed.";
            RefreshSnapshot();
        }

        public CityPlanningWorkflowPhase Phase { get; private set; }
        public string LastMessage { get; private set; }
        public CityState CurrentState => currentState;
        public CityConstructionPreview ConstructionPreview => construction?.PendingPreview;
        public CityNetworkPlanPreview NetworkPreview => networks?.PendingPreview;
        public CityStrategySimulation Strategy => strategy;
        public CityPlanningWorkflowSnapshot Snapshot { get; private set; }

        public bool TryScanJson(string json, out string error)
        {
            error = string.Empty;
            if (Phase != CityPlanningWorkflowPhase.ReadyToScan)
            {
                error = "Finish or cancel the current planning step before scanning again.";
                SetMessage(error);
                return false;
            }
            if (!CityTokenScanReader.TryParseAndValidate(
                    json,
                    lastAcceptedTimestampMs,
                    out CityTokenScanPacket scan,
                    out error))
            {
                SetMessage(error);
                return false;
            }
            return TryAcceptValidatedScan(scan, out error);
        }

        public bool TryAcceptScan(CityTokenScanPacket scan, out string error)
        {
            string json = JsonConvert.SerializeObject(scan);
            return TryScanJson(json, out error);
        }

        public bool ConfirmPreview(out string error)
        {
            error = string.Empty;
            if (Phase != CityPlanningWorkflowPhase.Preview)
            {
                error = "Scan City and inspect Preview before confirmation.";
                SetMessage(error);
                return false;
            }
            if (!construction.ConfirmConstruction(out error))
            {
                SetMessage(error);
                return false;
            }

            currentState = construction.CurrentState;
            confirmedTokens = construction.ConfirmedTokens;
            networks = new CityNetworkPlanningManager(currentState);
            CityNetworkPlanPreview preview = networks.BeginRouteSelection();
            if (preview.HasNetworkChanges)
            {
                foreach (CityRoadChoiceSet choice in preview.road_choices)
                {
                    CityRoadCandidate automatic = choice.candidates.FirstOrDefault(candidate =>
                        candidate.route_option == CityRoadRouteOption.Direct) ??
                        choice.candidates.FirstOrDefault();
                    if (automatic == null || !networks.SelectRoadOption(
                            choice.building_id,
                            automatic.route_option,
                            out error))
                    {
                        SetMessage(string.IsNullOrEmpty(error)
                            ? $"Could not create automatic access for {choice.building_id}."
                            : error);
                        return false;
                    }
                }
                if (!networks.ConfirmNetworkPlan(out error))
                {
                    SetMessage(error);
                    return false;
                }
                currentState = networks.CurrentState;
                PrepareConstruction();
                LastMessage =
                    $"Placement confirmed. Added {preview.RequiredRoadChoiceCount} smooth automatic " +
                    "road connection(s) to the nearest existing street.";
            }
            else
            {
                PrepareConstruction();
            }
            RefreshSnapshot();
            return true;
        }

        public bool SelectRoadOption(
            string buildingId,
            CityRoadRouteOption option,
            out string error)
        {
            error = string.Empty;
            if (Phase != CityPlanningWorkflowPhase.RouteSelection || networks == null)
            {
                error = "Road choices are not open.";
                SetMessage(error);
                return false;
            }
            if (!networks.SelectRoadOption(buildingId, option, out error))
            {
                SetMessage(error);
                return false;
            }
            LastMessage = networks.LastMessage;
            RefreshSnapshot();
            return true;
        }

        public bool SelectRecommendedLowImpactRoutes(out string error)
        {
            error = string.Empty;
            if (Phase != CityPlanningWorkflowPhase.RouteSelection || NetworkPreview == null)
            {
                error = "Road choices are not open.";
                SetMessage(error);
                return false;
            }
            foreach (CityRoadChoiceSet choice in NetworkPreview.road_choices)
            {
                CityRoadRouteOption option = choice.candidates.Any(candidate =>
                    candidate.route_option == CityRoadRouteOption.LowImpact)
                    ? CityRoadRouteOption.LowImpact
                    : choice.candidates[0].route_option;
                if (!networks.SelectRoadOption(choice.building_id, option, out error))
                {
                    SetMessage(error);
                    return false;
                }
            }
            LastMessage = "Selected the low-impact route for every new building.";
            RefreshSnapshot();
            return true;
        }

        public bool ConfirmRoutes(out string error)
        {
            error = string.Empty;
            if (Phase != CityPlanningWorkflowPhase.RouteSelection || networks == null)
            {
                error = "Choose and preview roads before confirmation.";
                SetMessage(error);
                return false;
            }
            if (!networks.ConfirmNetworkPlan(out error))
            {
                SetMessage(error);
                return false;
            }
            currentState = networks.CurrentState;
            PrepareConstruction();
            RefreshSnapshot();
            return true;
        }

        public bool StartConstruction(out string error)
        {
            error = string.Empty;
            if (Phase != CityPlanningWorkflowPhase.ReadyToBuild || strategy == null)
            {
                error = "Confirm construction and route choices before starting work.";
                SetMessage(error);
                return false;
            }

            ProjectRequest[] requests = ProposedProjects().ToArray();
            int required = requests.Sum(item =>
                CityStrategySimulation.DevelopmentPointCost(item.action));
            if (required == 0)
            {
                error = "There are no proposed city objects to build.";
                SetMessage(error);
                return false;
            }
            if (strategy.DevelopmentPoints < required)
            {
                error = $"This plan needs {required} DP but only {strategy.DevelopmentPoints} are available.";
                SetMessage(error);
                return false;
            }
            foreach (ProjectRequest request in requests)
            {
                if (!strategy.TryQueueProject(request.action, request.targetId, out error))
                {
                    throw new InvalidOperationException(
                        $"Preflight passed but {request.targetId} could not enter construction: {error}");
                }
            }
            Phase = CityPlanningWorkflowPhase.Construction;
            LastMessage = $"Construction started: {requests.Length} projects, {required} DP committed.";
            RefreshSnapshot();
            return true;
        }

        public void SetTimeScale(float value)
        {
            if (!CityStrategySimulation.AllowedTimeScales.Any(item =>
                    Math.Abs(item - value) <= 0.001f))
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Use Pause, 1x or 2x.");
            }
            timeScale = value;
            LastMessage = value <= 0f ? "Construction paused." : $"Construction speed set to {value:0}x.";
            RefreshSnapshot();
        }

        public void Tick(
            float deltaSeconds,
            CityEnvironmentSnapshot environment = null,
            CityWildlifeSnapshot wildlife = null)
        {
            if (Phase != CityPlanningWorkflowPhase.Construction || strategy == null || timeScale <= 0f)
            {
                return;
            }
            strategy.Tick(deltaSeconds * timeScale, environment, wildlife);
            EvaluateConstructionCompletion();
        }

        public void AdvanceConstructionBlock()
        {
            if (Phase != CityPlanningWorkflowPhase.Construction || strategy == null)
            {
                return;
            }
            strategy.AdvanceTimeBlock();
            EvaluateConstructionCompletion();
        }

        public void CancelPreview()
        {
            if (Phase != CityPlanningWorkflowPhase.Preview)
            {
                return;
            }
            construction.CancelPreview();
            Phase = CityPlanningWorkflowPhase.ReadyToScan;
            LastMessage = construction.LastMessage;
            RefreshSnapshot();
        }

        private bool TryAcceptValidatedScan(CityTokenScanPacket scan, out string error)
        {
            error = string.Empty;
            try
            {
                CityConstructionPreview preview = construction.ScanCity(scan);
                lastAcceptedTimestampMs = scan.timestamp_ms;
                Phase = CityPlanningWorkflowPhase.Preview;
                LastMessage = preview.HasBlockingChanges
                    ? "Preview blocked. Return moved Tokens or explicitly plan demolition."
                    : construction.LastMessage;
                RefreshSnapshot();
                return true;
            }
            catch (ArgumentException exception)
            {
                error = exception.Message;
                SetMessage(error);
                return false;
            }
        }

        private void PrepareConstruction()
        {
            strategy = new CityStrategySimulation(currentState);
            Phase = CityPlanningWorkflowPhase.ReadyToBuild;
            int required = ProposedProjects().Sum(item =>
                CityStrategySimulation.DevelopmentPointCost(item.action));
            LastMessage = required <= strategy.DevelopmentPoints
                ? $"Plan ready. Starting construction will commit {required} DP."
                : $"Plan needs {required} DP; revise it or earn more DP before construction.";
        }

        private IEnumerable<ProjectRequest> ProposedProjects()
        {
            foreach (CityBuilding item in currentState.buildings
                         .Where(item => item.construction_state == CityConstructionState.Proposed)
                         .OrderBy(item => item.id))
            {
                yield return Request(CityStrategyActionType.ConstructBuilding, item.id);
            }
            foreach (CityGreenPatch item in currentState.green_patches
                         .Where(item => item.construction_state == CityConstructionState.Proposed)
                         .OrderBy(item => item.id))
            {
                yield return Request(CityStrategyActionType.ConstructGreenPatch, item.id);
            }
            foreach (CityVehicleRoad item in currentState.vehicle_roads
                         .Where(item => item.construction_state == CityConstructionState.Proposed)
                         .OrderBy(item => item.id))
            {
                yield return Request(CityStrategyActionType.ConstructRoad, item.id);
            }
            foreach (CityPedestrianLink item in currentState.pedestrian_links
                         .Where(item => item.construction_state == CityConstructionState.Proposed)
                         .OrderBy(item => item.id))
            {
                yield return Request(CityStrategyActionType.ConstructFootpath, item.id);
            }
            foreach (CityAmenity item in currentState.amenities
                         .Where(item => item.construction_state == CityConstructionState.Proposed)
                         .OrderBy(item => item.id))
            {
                CityStrategyActionType action = item.type == CityAmenityType.Bench
                    ? CityStrategyActionType.InstallBench
                    : CityStrategyActionType.InstallBin;
                yield return Request(action, item.id);
            }
        }

        private void EvaluateConstructionCompletion()
        {
            CityStrategyProject[] projects = strategy.Snapshot.projects;
            if (projects.Length > 0 && projects.All(item =>
                    item.state != CityStrategyProjectState.Active))
            {
                Phase = CityPlanningWorkflowPhase.Complete;
                LastMessage = "Construction complete. The confirmed objects are now part of the live city.";
            }
            RefreshSnapshot();
        }

        private void SetMessage(string message)
        {
            LastMessage = message;
            RefreshSnapshot();
        }

        private void RefreshSnapshot()
        {
            ProjectRequest[] proposed = currentState == null
                ? Array.Empty<ProjectRequest>()
                : ProposedProjects().ToArray();
            Snapshot = new CityPlanningWorkflowSnapshot
            {
                phase = Phase,
                message = LastMessage,
                construction_preview = ConstructionPreview,
                network_preview = NetworkPreview,
                strategy = strategy?.Snapshot,
                required_development_points = proposed.Sum(item =>
                    CityStrategySimulation.DevelopmentPointCost(item.action)),
                time_scale = timeScale,
                proposed_object_ids = proposed.Select(item => item.targetId).ToArray(),
                completed_object_ids = strategy?.Snapshot?.projects?
                    .Where(item => item.state == CityStrategyProjectState.Complete)
                    .Select(item => item.target_id)
                    .ToArray() ?? Array.Empty<string>(),
            };
        }

        private static ProjectRequest Request(CityStrategyActionType action, string targetId)
        {
            return new ProjectRequest { action = action, targetId = targetId };
        }

        private static CityTokenState[] CloneTokens(IEnumerable<CityTokenState> tokens)
        {
            return (tokens ?? Array.Empty<CityTokenState>()).Select(item => new CityTokenState
            {
                id = item.id,
                type = item.type,
                x_norm = item.x_norm,
                y_norm = item.y_norm,
                rotation_deg = item.rotation_deg,
                confidence = item.confidence,
            }).ToArray();
        }
    }
}

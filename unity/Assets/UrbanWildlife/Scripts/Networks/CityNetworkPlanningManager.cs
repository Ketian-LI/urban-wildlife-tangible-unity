using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Networks
{
    public sealed class CityNetworkPlanningManager
    {
        public const string SourceContract = "city-network-plan-0.1";

        private readonly List<CityPedestrianLink> editableExtraFootpaths =
            new List<CityPedestrianLink>();
        private bool extraFootpathsDirty;

        public CityNetworkPlanningManager(CityState initialState)
        {
            CityStateValidationResult validation = CityStateValidator.Validate(initialState);
            if (!validation.IsValid)
            {
                throw new ArgumentException($"Initial city state is invalid: {validation.Summary}");
            }
            CurrentState = initialState;
            foreach (CityPedestrianLink link in initialState.pedestrian_links
                         .Where(IsScreenEditedExtraFootpath))
            {
                editableExtraFootpaths.Add(CloneLink(link));
            }
            CurrentPhase = CityNetworkPlanningPhase.Planning;
            LastMessage = "Ready to generate road choices.";
        }

        public CityState CurrentState { get; private set; }
        public CityNetworkPlanningPhase CurrentPhase { get; private set; }
        public CityNetworkPlanPreview PendingPreview { get; private set; }
        public string LastMessage { get; private set; }

        public CityNetworkPlanPreview BeginRouteSelection()
        {
            editableExtraFootpaths.Clear();
            foreach (CityPedestrianLink link in CurrentState.pedestrian_links
                         .Where(IsScreenEditedExtraFootpath))
            {
                editableExtraFootpaths.Add(CloneLink(link));
            }
            extraFootpathsDirty = false;
            CityBuilding[] proposedBuildings = CurrentState.buildings
                .Where(building => building != null &&
                                   building.construction_state == CityConstructionState.Proposed)
                .OrderBy(building => building.id)
                .ToArray();
            CityRoadChoiceSet[] choices = proposedBuildings
                .Where(building => building.requires_vehicle_access &&
                                   (building.vehicle_road_ids == null ||
                                    building.vehicle_road_ids.Length == 0))
                .Select(building => CityRoadCandidateGenerator.Generate(CurrentState, building))
                .ToArray();
            CityPedestrianLink[] basicLinks = proposedBuildings
                .Where(building => building.requires_pedestrian_access &&
                                   (building.pedestrian_link_ids == null ||
                                    building.pedestrian_link_ids.Length == 0))
                .Select(building =>
                    CityRoadCandidateGenerator.CreateBasicPedestrianAccess(CurrentState, building))
                .ToArray();

            PendingPreview = new CityNetworkPlanPreview
            {
                source_city_revision = CurrentState.revision,
                road_choices = choices,
                automatic_pedestrian_links = basicLinks,
                screen_edited_extra_footpaths = editableExtraFootpaths
                    .Select(CloneLink)
                    .ToArray(),
                screen_footpaths_changed = false,
            };
            CurrentPhase = CityNetworkPlanningPhase.RouteSelection;
            LastMessage = choices.Length == 0
                ? "No proposed building is waiting for a vehicle route."
                : $"Choose one of three vehicle routes for {choices.Length} proposed building(s).";
            return PendingPreview;
        }

        public bool SelectRoadOption(
            string buildingId,
            CityRoadRouteOption routeOption,
            out string error)
        {
            error = string.Empty;
            if (PendingPreview == null || CurrentPhase != CityNetworkPlanningPhase.RouteSelection)
            {
                error = "Generate road choices before selecting a route.";
                LastMessage = error;
                return false;
            }
            CityRoadChoiceSet choice = PendingPreview.road_choices.FirstOrDefault(item =>
                item.building_id == buildingId);
            if (choice == null)
            {
                error = $"Building {buildingId} has no pending road choice.";
                LastMessage = error;
                return false;
            }
            CityRoadCandidate candidate = choice.candidates.FirstOrDefault(item =>
                item.route_option == routeOption);
            if (candidate == null)
            {
                error = $"Route option {routeOption} is not available for {buildingId}.";
                LastMessage = error;
                return false;
            }
            choice.selected_candidate_id = candidate.id;
            LastMessage =
                $"Selected {routeOption} for {buildingId} " +
                $"({candidate.estimated_length_units:0.0} units).";
            return true;
        }

        public bool UpsertExtraFootpath(
            string id,
            IEnumerable<float[]> pointsNorm,
            IEnumerable<string> connectedBuildingIds,
            bool stepFreeAccessible,
            out string error)
        {
            error = string.Empty;
            if (CurrentPhase != CityNetworkPlanningPhase.RouteSelection || PendingPreview == null)
            {
                error = "Open Route Selection before editing an extra Footpath.";
                LastMessage = error;
                return false;
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                error = "Extra Footpath ID is required.";
                LastMessage = error;
                return false;
            }
            float[][] points = ClonePoints(pointsNorm);
            if (!ValidPolyline(points))
            {
                error = "Extra Footpath needs at least two normalized screen points.";
                LastMessage = error;
                return false;
            }
            string[] buildingIds = (connectedBuildingIds ?? Array.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToArray();
            HashSet<string> validBuildingIds = new HashSet<string>(
                CurrentState.buildings.Where(building => building != null)
                    .Select(building => building.id));
            string unknownBuilding = buildingIds.FirstOrDefault(value =>
                !validBuildingIds.Contains(value));
            if (unknownBuilding != null)
            {
                error = $"Extra Footpath references unknown building {unknownBuilding}.";
                LastMessage = error;
                return false;
            }
            if (CurrentState.pedestrian_links.Any(link =>
                    link != null && link.id == id && !IsScreenEditedExtraFootpath(link)))
            {
                error = $"Extra Footpath ID {id} is already used by another pedestrian link.";
                LastMessage = error;
                return false;
            }

            CityPedestrianLink replacement = new CityPedestrianLink
            {
                id = id,
                type = CityPedestrianLinkType.ExtraFootpath,
                construction_state = CityConstructionState.Proposed,
                source = CityNetworkSource.ScreenEdited,
                points_norm = points,
                width_units = 1.6f,
                step_free_accessible = stepFreeAccessible,
                connected_building_ids = buildingIds,
                connected_link_ids = Array.Empty<string>(),
            };
            int existingIndex = editableExtraFootpaths.FindIndex(link => link.id == id);
            if (existingIndex >= 0)
            {
                editableExtraFootpaths[existingIndex] = replacement;
            }
            else
            {
                editableExtraFootpaths.Add(replacement);
            }
            extraFootpathsDirty = true;
            RefreshExtraFootpaths();
            LastMessage = $"Screen Footpath {id} saved in Preview.";
            return true;
        }

        public bool RemoveExtraFootpath(string id, out string error)
        {
            error = string.Empty;
            if (CurrentPhase != CityNetworkPlanningPhase.RouteSelection || PendingPreview == null)
            {
                error = "Open Route Selection before deleting an extra Footpath.";
                LastMessage = error;
                return false;
            }
            int removed = editableExtraFootpaths.RemoveAll(link => link.id == id);
            if (removed == 0)
            {
                error = $"Screen Footpath {id} was not found.";
                LastMessage = error;
                return false;
            }
            extraFootpathsDirty = true;
            RefreshExtraFootpaths();
            LastMessage = $"Screen Footpath {id} removed from Preview.";
            return true;
        }

        public bool ConfirmNetworkPlan(out string error)
        {
            error = string.Empty;
            if (PendingPreview == null || CurrentPhase != CityNetworkPlanningPhase.RouteSelection)
            {
                error = "Generate and preview network choices before confirmation.";
                LastMessage = error;
                return false;
            }
            if (PendingPreview.source_city_revision != CurrentState.revision)
            {
                error = "The city changed after routes were generated; refresh the Preview.";
                LastMessage = error;
                return false;
            }
            if (!PendingPreview.CanConfirm)
            {
                error = "Select one vehicle route for every proposed building before confirmation.";
                LastMessage = error;
                return false;
            }

            CityVehicleRoad[] selectedRoads = PendingPreview.road_choices
                .Select(choice => MaterializeRoad(choice.SelectedCandidate))
                .ToArray();
            CityPedestrianLink[] basicLinks = PendingPreview.automatic_pedestrian_links
                .Select(CloneLink)
                .ToArray();
            CityPedestrianLink[] extras = editableExtraFootpaths.Select(CloneLink).ToArray();
            CityState next = BuildNextState(CurrentState, selectedRoads, basicLinks, extras);
            CityStateValidationResult validation = CityStateValidator.Validate(next);
            if (!validation.IsValid)
            {
                error = $"Network plan would create an invalid city: {validation.Summary}";
                LastMessage = error;
                return false;
            }

            CurrentState = next;
            extraFootpathsDirty = false;
            PendingPreview = null;
            CurrentPhase = CityNetworkPlanningPhase.Planning;
            LastMessage =
                $"Confirmed {selectedRoads.Length} vehicle access route(s), " +
                $"{basicLinks.Length} automatic pedestrian link(s) and {extras.Length} screen Footpath(s).";
            return true;
        }

        public void CancelPreview()
        {
            editableExtraFootpaths.Clear();
            foreach (CityPedestrianLink link in CurrentState.pedestrian_links
                         .Where(IsScreenEditedExtraFootpath))
            {
                editableExtraFootpaths.Add(CloneLink(link));
            }
            PendingPreview = null;
            extraFootpathsDirty = false;
            CurrentPhase = CityNetworkPlanningPhase.Planning;
            LastMessage = "Network Preview cancelled; the confirmed city was not changed.";
        }

        private static CityVehicleRoad MaterializeRoad(CityRoadCandidate candidate)
        {
            if (candidate == null)
            {
                throw new ArgumentException("Every road choice requires a selected candidate.");
            }
            return new CityVehicleRoad
            {
                id = $"vehicle-access-{candidate.building_id}",
                role = CityVehicleRoadRole.BuildingAccess,
                route_option = candidate.route_option,
                construction_state = CityConstructionState.Proposed,
                source = CityNetworkSource.AutoGenerated,
                points_norm = ClonePoints(candidate.points_norm),
                width_units = 3.2f,
                speed_units_per_second = 7f,
                traffic_load = candidate.estimated_traffic_pressure,
                connected_building_ids = new[] { candidate.building_id },
                connected_road_ids = new[] { candidate.connection_road_id },
            };
        }

        private static CityState BuildNextState(
            CityState current,
            CityVehicleRoad[] selectedRoads,
            CityPedestrianLink[] basicLinks,
            CityPedestrianLink[] extras)
        {
            HashSet<string> replacedExtraIds = new HashSet<string>(
                (current.pedestrian_links ?? Array.Empty<CityPedestrianLink>())
                .Where(IsScreenEditedExtraFootpath)
                .Select(link => link.id));
            CityPedestrianLink[] preservedLinks = (current.pedestrian_links ??
                                                    Array.Empty<CityPedestrianLink>())
                .Where(link => !IsScreenEditedExtraFootpath(link))
                .Select(CloneLink)
                .ToArray();

            CityBuilding[] buildings = (current.buildings ?? Array.Empty<CityBuilding>())
                .Select(CloneBuilding)
                .ToArray();
            foreach (CityBuilding building in buildings)
            {
                building.pedestrian_link_ids = (building.pedestrian_link_ids ?? Array.Empty<string>())
                    .Where(id => !replacedExtraIds.Contains(id))
                    .ToArray();
            }
            foreach (CityVehicleRoad road in selectedRoads)
            {
                CityBuilding building = buildings.Single(item =>
                    item.id == road.connected_building_ids[0]);
                building.vehicle_road_ids = AddUnique(building.vehicle_road_ids, road.id);
            }
            foreach (CityPedestrianLink link in basicLinks.Concat(extras))
            {
                foreach (string buildingId in link.connected_building_ids ?? Array.Empty<string>())
                {
                    CityBuilding building = buildings.Single(item => item.id == buildingId);
                    building.pedestrian_link_ids = AddUnique(building.pedestrian_link_ids, link.id);
                }
            }

            return new CityState
            {
                schema_version = current.schema_version,
                city_id = current.city_id,
                revision = current.revision + 1,
                source_contract = SourceContract,
                bounds = current.bounds,
                planning_grid = CityGridResolver.DeepClone(current.planning_grid),
                buildings = buildings,
                green_patches = current.green_patches ?? Array.Empty<CityGreenPatch>(),
                vehicle_roads = (current.vehicle_roads ?? Array.Empty<CityVehicleRoad>())
                    .Concat(selectedRoads)
                    .ToArray(),
                pedestrian_links = preservedLinks.Concat(basicLinks).Concat(extras).ToArray(),
                amenities = current.amenities ?? Array.Empty<CityAmenity>(),
                waste = current.waste,
            };
        }

        private void RefreshExtraFootpaths()
        {
            PendingPreview.screen_edited_extra_footpaths = editableExtraFootpaths
                .Select(CloneLink)
                .ToArray();
            PendingPreview.screen_footpaths_changed = extraFootpathsDirty;
        }

        private static bool IsScreenEditedExtraFootpath(CityPedestrianLink link)
        {
            return link != null && link.type == CityPedestrianLinkType.ExtraFootpath &&
                   link.source == CityNetworkSource.ScreenEdited;
        }

        private static bool ValidPolyline(float[][] points)
        {
            return points.Length >= 2 && points.All(point =>
                point != null && point.Length == 2 &&
                FiniteUnit(point[0]) && FiniteUnit(point[1]));
        }

        private static bool FiniteUnit(float value)
        {
            return value >= 0f && value <= 1f &&
                   !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static string[] AddUnique(IEnumerable<string> values, string addition)
        {
            return (values ?? Array.Empty<string>())
                .Concat(new[] { addition })
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct()
                .ToArray();
        }

        private static float[][] ClonePoints(IEnumerable<float[]> points)
        {
            return (points ?? Array.Empty<float[]>())
                .Select(point => point == null ? null : (float[])point.Clone())
                .ToArray();
        }

        private static CityPedestrianLink CloneLink(CityPedestrianLink link)
        {
            return new CityPedestrianLink
            {
                id = link.id,
                type = link.type,
                construction_state = link.construction_state,
                source = link.source,
                points_norm = ClonePoints(link.points_norm),
                width_units = link.width_units,
                step_free_accessible = link.step_free_accessible,
                connected_building_ids = (link.connected_building_ids ?? Array.Empty<string>()).ToArray(),
                connected_link_ids = (link.connected_link_ids ?? Array.Empty<string>()).ToArray(),
            };
        }

        private static CityBuilding CloneBuilding(CityBuilding building)
        {
            return new CityBuilding
            {
                id = building.id,
                source_token_id = building.source_token_id,
                planning_cell_id = building.planning_cell_id,
                type = building.type,
                construction_state = building.construction_state,
                position_norm = building.position_norm == null
                    ? null
                    : (float[])building.position_norm.Clone(),
                rotation_deg = building.rotation_deg,
                footprint_units = building.footprint_units == null
                    ? null
                    : (float[])building.footprint_units.Clone(),
                housing_capacity = building.housing_capacity,
                human_origin_rate = building.human_origin_rate,
                human_destination_weight = building.human_destination_weight,
                comfortable_capacity = building.comfortable_capacity,
                vehicle_demand = building.vehicle_demand,
                waste_output = building.waste_output,
                anthropogenic_food_output = building.anthropogenic_food_output,
                disturbance_output = building.disturbance_output,
                requires_vehicle_access = building.requires_vehicle_access,
                requires_pedestrian_access = building.requires_pedestrian_access,
                vehicle_road_ids = (building.vehicle_road_ids ?? Array.Empty<string>()).ToArray(),
                pedestrian_link_ids = (building.pedestrian_link_ids ?? Array.Empty<string>()).ToArray(),
            };
        }
    }
}

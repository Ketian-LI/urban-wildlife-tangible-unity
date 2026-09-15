using System;
using System.Collections.Generic;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Mobility
{
    public static class CityTripPlanner
    {
        public const int DefaultMaximumRepresentativeAgents = 24;
        public const float DefaultDepartureIntervalSeconds = 1.5f;
        public const string ExternalGatewayDestinationId = "outside-city";

        public static CityMobilityPlan CreatePlan(
            CityState city,
            int maximumRepresentativeAgents = DefaultMaximumRepresentativeAgents,
            int deterministicSeed = 4107)
        {
            CityStateValidationResult validation = CityStateValidator.Validate(city);
            if (!validation.IsValid)
            {
                throw new ArgumentException(
                    $"Cannot plan trips for an invalid city: {validation.Summary}");
            }
            if (maximumRepresentativeAgents <= 0 || maximumRepresentativeAgents > 30)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumRepresentativeAgents),
                    "Representative human agents must stay between 1 and 30.");
            }

            CityBuilding[] operational = city.buildings
                .Where(IsOperational)
                .OrderBy(building => building.id)
                .ToArray();
            CityBuilding[] origins = operational
                .Where(building => building.housing_capacity > 0 &&
                                   building.human_origin_rate > 0f)
                .ToArray();
            CityBuilding[] destinations = operational
                .Where(building =>
                    (building.type == CityBuildingType.Commercial ||
                     building.type == CityBuildingType.CommunityFacility) &&
                    building.human_destination_weight > 0f &&
                    building.comfortable_capacity > 0)
                .ToArray();
            if (origins.Length == 0)
            {
                return new CityMobilityPlan
                {
                    city_id = city.city_id,
                    city_revision = city.revision,
                    representative_agent_limit = maximumRepresentativeAgents,
                };
            }

            Dictionary<string, CityDestinationLoad> loads = destinations.ToDictionary(
                destination => destination.id,
                destination => new CityDestinationLoad
                {
                    building_id = destination.id,
                    comfortable_capacity = destination.comfortable_capacity,
                });
            List<CityRepresentativeTrip> trips = new List<CityRepresentativeTrip>();
            int sequence = 0;
            foreach (CityBuilding origin in origins)
            {
                int desired = destinations.Length == 0
                    ? Math.Max(3, DesiredAgentCount(origin))
                    : DesiredAgentCount(origin);
                for (int localIndex = 0;
                     localIndex < desired && trips.Count < maximumRepresentativeAgents;
                     localIndex += 1)
                {
                    string agentId = $"human-rep-{origin.id}-{localIndex + 1:00}";
                    int representedPopulation = Math.Max(
                        1,
                        (int)Math.Ceiling(origin.housing_capacity / (double)desired));
                    bool externalJourney = destinations.Length == 0 ||
                                           (localIndex == 0 && origin.vehicle_demand > 0.5f);
                    if (externalJourney)
                    {
                        CityTravelMode externalMode = destinations.Length == 0 && localIndex > 0
                            ? CityTravelMode.Walk
                            : CityTravelMode.Drive;
                        float[][] externalRoute = externalMode == CityTravelMode.Drive
                            ? CityNetworkRouteBuilder.BuildExternalDrive(city, origin)
                            : CityNetworkRouteBuilder.BuildExternalWalk(city, origin);
                        float routeDistance = CityNetworkRouteBuilder.Length(
                            externalRoute,
                            city.bounds);
                        trips.Add(new CityRepresentativeTrip
                        {
                            id = $"trip-{sequence + 1:000}",
                            agent_id = agentId,
                            origin_building_id = origin.id,
                            destination_building_id = ExternalGatewayDestinationId,
                            purpose = CityTripPurpose.ExternalJourney,
                            mode = externalMode,
                            route_points_norm = externalRoute,
                            direct_distance_units = Distance(
                                origin.position_norm,
                                externalRoute[externalRoute.Length - 1],
                                city.bounds),
                            route_distance_units = routeDistance,
                            departure_seconds = sequence * DefaultDepartureIntervalSeconds,
                            dwell_seconds = 6f,
                            represented_people = representedPopulation,
                        });
                        sequence += 1;
                        continue;
                    }

                    CityBuilding destination = SelectDestination(
                        origin,
                        destinations,
                        loads,
                        city.bounds,
                        agentId,
                        deterministicSeed,
                        representedPopulation);
                    float directDistance = Distance(
                        origin.position_norm,
                        destination.position_norm,
                        city.bounds);
                    CityTravelMode mode = ChooseMode(
                        city,
                        origin,
                        destination,
                        directDistance,
                        agentId,
                        deterministicSeed);
                    float[][] route = CityNetworkRouteBuilder.Build(
                        city,
                        origin,
                        destination,
                        mode);
                    CityDestinationLoad load = loads[destination.id];
                    load.assigned_representative_agents += 1;
                    load.assigned_people += representedPopulation;
                    UpdateCrowding(load);
                    trips.Add(new CityRepresentativeTrip
                    {
                        id = $"trip-{sequence + 1:000}",
                        agent_id = agentId,
                        origin_building_id = origin.id,
                        destination_building_id = destination.id,
                        purpose = PurposeFor(destination),
                        mode = mode,
                        route_points_norm = route,
                        direct_distance_units = directDistance,
                        route_distance_units = CityNetworkRouteBuilder.Length(route, city.bounds),
                        departure_seconds = sequence * DefaultDepartureIntervalSeconds,
                        dwell_seconds = DwellSeconds(destination),
                        represented_people = representedPopulation,
                    });
                    sequence += 1;
                }
                if (trips.Count >= maximumRepresentativeAgents)
                {
                    break;
                }
            }

            return new CityMobilityPlan
            {
                city_id = city.city_id,
                city_revision = city.revision,
                representative_agent_limit = maximumRepresentativeAgents,
                trips = trips.ToArray(),
                destination_loads = loads.Values.OrderBy(load => load.building_id).ToArray(),
            };
        }

        private static CityBuilding SelectDestination(
            CityBuilding origin,
            IEnumerable<CityBuilding> destinations,
            IReadOnlyDictionary<string, CityDestinationLoad> loads,
            CityBounds bounds,
            string agentId,
            int seed,
            int representedPeople)
        {
            return destinations
                .Select(destination => new
                {
                    Destination = destination,
                    Score = DestinationScore(
                        origin,
                        destination,
                        loads[destination.id],
                        bounds,
                        agentId,
                        seed,
                        representedPeople),
                })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.Destination.id)
                .First()
                .Destination;
        }

        private static float DestinationScore(
            CityBuilding origin,
            CityBuilding destination,
            CityDestinationLoad load,
            CityBounds bounds,
            string agentId,
            int seed,
            int representedPeople)
        {
            float diagonal = (float)Math.Sqrt(
                bounds.width_units * bounds.width_units +
                bounds.height_units * bounds.height_units);
            float distancePenalty = Distance(origin.position_norm, destination.position_norm, bounds) /
                                    Math.Max(1f, diagonal) * 0.2f;
            int prospectiveCount = load.assigned_people + representedPeople;
            float crowdPenalty = Math.Max(
                0f,
                (prospectiveCount - load.comfortable_capacity) /
                (float)Math.Max(1, load.comfortable_capacity));
            float jitter = StableUnit(agentId + destination.id, seed) * 0.025f;
            return destination.human_destination_weight - distancePenalty -
                   crowdPenalty * 0.75f + jitter;
        }

        private static CityTravelMode ChooseMode(
            CityState city,
            CityBuilding origin,
            CityBuilding destination,
            float directDistance,
            string agentId,
            int seed)
        {
            bool canWalk = ReferencesExist(
                origin.pedestrian_link_ids,
                city.pedestrian_links
                    .Where(link => link.construction_state == CityConstructionState.Existing)
                    .Select(link => link.id)) &&
                           ReferencesExist(
                destination.pedestrian_link_ids,
                city.pedestrian_links
                    .Where(link => link.construction_state == CityConstructionState.Existing)
                    .Select(link => link.id));
            bool canDrive = ReferencesExist(
                origin.vehicle_road_ids,
                city.vehicle_roads
                    .Where(road => road.construction_state == CityConstructionState.Existing)
                    .Select(road => road.id)) &&
                             ReferencesExist(
                destination.vehicle_road_ids,
                city.vehicle_roads
                    .Where(road => road.construction_state == CityConstructionState.Existing)
                    .Select(road => road.id));
            if (!canDrive && !canWalk)
            {
                throw new InvalidOperationException(
                    $"Trip {origin.id} → {destination.id} has no complete movement network.");
            }
            if (!canDrive)
            {
                return CityTravelMode.Walk;
            }
            if (!canWalk)
            {
                return CityTravelMode.Drive;
            }

            if (origin.vehicle_demand <= 0.5f && directDistance <= 50f)
            {
                return CityTravelMode.Walk;
            }

            float distanceFactor = Math.Min(1f, directDistance / 45f);
            float jitter = (StableUnit(agentId + "mode", seed) - 0.5f) * 0.08f;
            float driveScore = origin.vehicle_demand * 0.55f + distanceFactor * 0.45f + jitter;
            return driveScore >= 0.55f ? CityTravelMode.Drive : CityTravelMode.Walk;
        }

        private static bool ReferencesExist(
            IEnumerable<string> references,
            IEnumerable<string> availableIds)
        {
            HashSet<string> available = new HashSet<string>(availableIds);
            return (references ?? Array.Empty<string>()).Any(available.Contains);
        }

        private static void UpdateCrowding(CityDestinationLoad load)
        {
            load.crowding_ratio = load.assigned_people /
                                  (float)Math.Max(1, load.comfortable_capacity);
            load.crowd_penalty = Math.Max(0f, load.crowding_ratio - 1f);
        }

        private static int DesiredAgentCount(CityBuilding origin)
        {
            float housingGroups = origin.housing_capacity / 12f;
            return Math.Max(1, (int)Math.Ceiling(housingGroups * origin.human_origin_rate));
        }

        private static CityTripPurpose PurposeFor(CityBuilding destination)
        {
            return destination.type == CityBuildingType.Commercial
                ? CityTripPurpose.Consumption
                : CityTripPurpose.Leisure;
        }

        private static float DwellSeconds(CityBuilding destination)
        {
            return destination.type == CityBuildingType.Commercial ? 10f : 14f;
        }

        private static bool IsOperational(CityBuilding building)
        {
            return building != null &&
                   building.construction_state == CityConstructionState.Existing;
        }

        private static float Distance(float[] first, float[] second, CityBounds bounds)
        {
            float x = (first[0] - second[0]) * bounds.width_units;
            float y = (first[1] - second[1]) * bounds.height_units;
            return (float)Math.Sqrt(x * x + y * y);
        }

        private static float StableUnit(string text, int seed)
        {
            unchecked
            {
                uint hash = (uint)(2166136261 ^ seed);
                foreach (char character in text ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return (hash & 0x00ffffff) / 16777215f;
            }
        }
    }
}

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UrbanWildlife.Mobility
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTripPurpose
    {
        Leisure,
        Consumption,
        ExternalJourney,
        ReturnHome,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTravelMode
    {
        Walk,
        Drive,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum CityTripMotionState
    {
        Pending,
        Outbound,
        Dwelling,
        Returning,
        Complete,
    }

    [Serializable]
    public sealed class CityDestinationLoad
    {
        public string building_id;
        public int comfortable_capacity;
        public int assigned_representative_agents;
        public int assigned_people;
        public float crowding_ratio;
        public float crowd_penalty;
    }

    [Serializable]
    public sealed class CityRepresentativeTrip
    {
        public string id;
        public string agent_id;
        public string origin_building_id;
        public string destination_building_id;
        public CityTripPurpose purpose;
        public CityTravelMode mode;
        public int represented_people;
        public float departure_seconds;
        public float dwell_seconds;
        public float direct_distance_units;
        public float route_distance_units;
        public float[][] route_points_norm;
    }

    [Serializable]
    public sealed class CityMobilityPlan
    {
        public string city_id;
        public int city_revision;
        public int representative_agent_limit;
        public CityRepresentativeTrip[] trips = Array.Empty<CityRepresentativeTrip>();
        public CityDestinationLoad[] destination_loads = Array.Empty<CityDestinationLoad>();

        public int RepresentativeAgentCount => trips.Length;
        public int WalkTripCount => trips.Count(trip => trip.mode == CityTravelMode.Walk);
        public int DriveTripCount => trips.Count(trip => trip.mode == CityTravelMode.Drive);
        public int RepresentedPopulation => trips.Sum(trip => trip.represented_people);
        public bool HasCrowding => destination_loads.Any(load => load.crowd_penalty > 0f);
    }

    [Serializable]
    public sealed class CityVehicleAgent
    {
        public string id;
        public string source_trip_id;
        public CityTripMotionState state;
        public float[] position_norm;
        public float route_progress_01;
        public float speed_units_per_second;
    }
}

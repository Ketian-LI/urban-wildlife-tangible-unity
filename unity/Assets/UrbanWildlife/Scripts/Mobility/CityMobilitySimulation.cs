using System;
using System.Linq;
using UrbanWildlife.City;

namespace UrbanWildlife.Mobility
{
    public sealed class CityMobilitySimulation
    {
        private readonly CityTripAgent[] agents;

        public CityMobilitySimulation(CityMobilityPlan mobilityPlan, CityBounds cityBounds)
        {
            if (mobilityPlan == null || cityBounds == null)
            {
                throw new ArgumentException("A mobility plan and city bounds are required.");
            }
            Plan = mobilityPlan;
            agents = (mobilityPlan.trips ?? Array.Empty<CityRepresentativeTrip>())
                .Select(trip => new CityTripAgent(trip, cityBounds))
                .ToArray();
        }

        public CityMobilityPlan Plan { get; }
        public float ElapsedSeconds { get; private set; }
        public int SpawnedTripCount => agents.Count(agent =>
            agent.State != CityTripMotionState.Pending);
        public int ActiveHumanAgentCount => agents.Count(agent =>
            agent.State != CityTripMotionState.Pending &&
            agent.State != CityTripMotionState.Complete);
        public int CompletedTripCount => agents.Count(agent =>
            agent.State == CityTripMotionState.Complete);
        public int SpawnedVehicleAgentCount => agents.Count(agent =>
            agent.Trip.mode == CityTravelMode.Drive &&
            agent.State != CityTripMotionState.Pending);
        public CityTripMotionState[] AgentStates => agents
            .Select(agent => agent.State)
            .ToArray();
        public int PeakActiveVehicleCount { get; private set; }
        public bool AllComplete => agents.All(agent =>
            agent.State == CityTripMotionState.Complete);
        public CityTripAgent[] Agents => agents.ToArray();
        public CityVehicleAgent[] ActiveVehicleAgents => agents
            .Select(agent => agent.VehicleSnapshot())
            .Where(vehicle => vehicle != null)
            .ToArray();

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }
            ElapsedSeconds += deltaSeconds;
            foreach (CityTripAgent agent in agents)
            {
                if (agent.State == CityTripMotionState.Pending &&
                    agent.Trip.departure_seconds <= ElapsedSeconds + 0.0001f)
                {
                    agent.Start();
                }
                agent.Tick(deltaSeconds);
            }
            PeakActiveVehicleCount = Math.Max(
                PeakActiveVehicleCount,
                ActiveVehicleAgents.Length);
        }
    }
}

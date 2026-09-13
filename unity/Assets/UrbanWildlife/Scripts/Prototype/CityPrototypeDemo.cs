using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UrbanWildlife.City;
using UrbanWildlife.Construction;
using UrbanWildlife.Ecology;
using UrbanWildlife.Environmental;
using UrbanWildlife.Input;
using UrbanWildlife.Mobility;
using UrbanWildlife.Networks;
using UrbanWildlife.Planning;
using UrbanWildlife.Strategy;
using UrbanWildlife.Reporting;

namespace UrbanWildlife.Prototype
{
    [ExecuteAlways]
    public sealed class CityPrototypeDemo : MonoBehaviour
    {
        private const float MapWidth = 12f;
        private const float MapHeight = 8f;
        private const float RuntimeSpeed = 2f;

        private sealed class ActorView
        {
            public GameObject human;
            public GameObject vehicle;
            public Vector3 lastPosition;
            public bool hasLastPosition;
        }

        private CityState city;
        private CityMobilityPlan plan;
        private CityMobilitySimulation mobility;
        private CityEnvironmentSimulation environment;
        private CityWildlifeSimulation wildlife;
        private CityStrategySimulation strategy;
        private CityPlanningWorkflow planningWorkflow;
        private CityObservationTracker observation;
        private ActorView[] actorViews = Array.Empty<ActorView>();
        private readonly Dictionary<string, GameObject> wildlifeViews =
            new Dictionary<string, GameObject>();
        private GameObject generatedRoot;
        private GameObject pedestrianRoot;
        private GameObject planningPreviewRoot;
        private bool paused;
        private bool showPedestrianNetwork = true;
        private Vector2 sidebarScroll;
        private float completeElapsed;
        private float simulationElapsed;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle metricStyle;
        private GUIStyle buttonStyle;

        public int GeneratedBuildingCount { get; private set; }
        public int GeneratedVehicleRoadCount { get; private set; }
        public int GeneratedPedestrianLinkCount { get; private set; }
        public int GeneratedAmenityCount { get; private set; }
        public int RepresentativeAgentCount => plan?.RepresentativeAgentCount ?? 0;
        public int RepresentedPopulation => plan?.RepresentedPopulation ?? 0;
        public int VehicleTripCount => plan?.DriveTripCount ?? 0;
        public int FoodSourceCount => environment?.Snapshot?.food_sources?.Length ?? 0;
        public bool OverflowActive => environment?.Snapshot?.overflow_active ?? false;
        public int WildlifeAgentCount => wildlife?.Snapshot?.agents?.Length ?? 0;
        public int ActiveWildlifeCount => wildlife?.Snapshot?.active_count ?? 0;
        public int WildlifeSpeciesCount => wildlife?.Snapshot?.agents?
            .Select(agent => agent.species).Distinct().Count() ?? 0;
        public int DevelopmentPhaseCount => strategy == null
            ? 0
            : Enum.GetValues(typeof(CityDevelopmentPhase)).Length;
        public float CityBalanceTotal => strategy?.Snapshot?.balance?.total ?? 0f;
        public int HumanTracePointCount => observation?
            .Trace(CityTraceDisplayMode.HumanTrace).Length ?? 0;
        public int AnimalTracePointCount => observation?
            .Trace(CityTraceDisplayMode.AnimalTrace).Length ?? 0;
        public int CityFeedCount => observation?.Snapshot?.city_feed?.Length ?? 0;
        public bool PlanningWorkflowConnected => planningWorkflow != null;
        public CityPlanningWorkflowPhase PlanningPhase => planningWorkflow?.Phase ??
                                                           CityPlanningWorkflowPhase.ReadyToScan;

        private void OnEnable()
        {
            ConfigureCamera();
            if (!Application.isPlaying)
            {
                InitializePrototype(true);
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                InitializePrototype(false);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || mobility == null || paused)
            {
                return;
            }
            mobility.Tick(Time.deltaTime * RuntimeSpeed);
            simulationElapsed += Time.deltaTime * RuntimeSpeed;
            environment?.Tick(Time.deltaTime * RuntimeSpeed, ActivityMultiplier());
            wildlife?.Tick(
                Time.deltaTime * RuntimeSpeed,
                environment.Snapshot,
                mobility.ActiveVehicleAgents);
            CityPlanningWorkflowPhase phaseBeforeTick = planningWorkflow?.Phase ??
                                                         CityPlanningWorkflowPhase.ReadyToScan;
            if (planningWorkflow?.Phase == CityPlanningWorkflowPhase.Construction)
            {
                planningWorkflow.Tick(
                    Time.deltaTime * RuntimeSpeed,
                    environment.Snapshot,
                    wildlife.Snapshot);
                strategy = planningWorkflow.Strategy;
            }
            else if (planningWorkflow?.Phase == CityPlanningWorkflowPhase.ReadyToBuild)
            {
                strategy?.UpdateFeedback(environment.Snapshot, wildlife.Snapshot);
            }
            else
            {
                strategy?.Tick(Time.deltaTime * RuntimeSpeed, environment.Snapshot, wildlife.Snapshot);
            }
            observation?.Capture(
                Time.deltaTime * RuntimeSpeed,
                mobility,
                environment.Snapshot,
                wildlife.Snapshot,
                strategy.Snapshot);
            UpdateActors();
            UpdateWildlife();
            if (phaseBeforeTick != planningWorkflow?.Phase &&
                planningWorkflow?.Phase == CityPlanningWorkflowPhase.Complete)
            {
                InitializePrototype(false);
                return;
            }
            if (mobility.AllComplete)
            {
                completeElapsed += Time.deltaTime;
                if (completeElapsed >= 3f)
                {
                    InitializePrototype(false);
                }
            }
        }

        public void InitializePrototype(bool advancePreview)
        {
            ClearGenerated();
            city = planningWorkflow?.CurrentState ?? CityPrototypeStateFactory.Create();
            plan = CityTripPlanner.CreatePlan(city);
            mobility = new CityMobilitySimulation(plan, city.bounds);
            environment = new CityEnvironmentSimulation(city, new CityEnvironmentConfiguration
            {
                overflow_grace_seconds = 4f,
            });
            wildlife = new CityWildlifeSimulation(city);
            strategy = planningWorkflow?.Strategy ?? new CityStrategySimulation(city);
            if (planningWorkflow == null)
            {
                planningWorkflow = new CityPlanningWorkflow(
                    city,
                    CityPlanningDemoScanFactory.ConfirmedTokensFrom(city));
            }
            observation = new CityObservationTracker();
            observation.BeginPhase(strategy.Snapshot);
            completeElapsed = 0f;
            simulationElapsed = 0f;
            paused = false;

            generatedRoot = new GameObject("Generated City Prototype");
            generatedRoot.transform.SetParent(transform, false);
            if (!Application.isPlaying)
            {
                generatedRoot.hideFlags = HideFlags.DontSave;
            }
            BuildBoard();
            BuildGreenPatches();
            BuildNetworks();
            BuildBuildings();
            BuildAmenities();
            BuildWildlife();
            BuildActors();
            BuildPlanningOverlay();
            if (advancePreview)
            {
                mobility.Tick(8f);
                environment.Tick(8f, 1f);
                wildlife.Tick(8f, environment.Snapshot, Array.Empty<CityVehicleAgent>());
                strategy.UpdateFeedback(environment.Snapshot, wildlife.Snapshot);
                observation.Capture(
                    8f,
                    mobility,
                    environment.Snapshot,
                    wildlife.Snapshot,
                    strategy.Snapshot);
                UpdateActors();
                UpdateWildlife();
            }
        }

        private float ActivityMultiplier()
        {
            // A compact Quiet -> Active -> Peak -> Late loop for the integration scene.
            float phase = simulationElapsed / 18f * Mathf.PI * 2f;
            return 0.75f + 0.65f * (0.5f + 0.5f * Mathf.Sin(phase - Mathf.PI * 0.5f));
        }

        private void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }
            camera.rect = new Rect(0f, 0f, 0.77f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 4.45f;
            camera.backgroundColor = new Color(0.56f, 0.82f, 0.90f, 1f);
        }

        private void BuildBoard()
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "City board base";
            board.transform.SetParent(generatedRoot.transform, false);
            board.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            board.transform.localScale = new Vector3(MapWidth + 0.18f, 0.14f, MapHeight + 0.18f);
            RemoveCollider(board);
            SetMaterial(board, new Color(0.94f, 0.94f, 0.88f, 1f));

            GameObject waterRoot = ChildRoot("Water features");
            CreateLine(
                waterRoot.transform,
                "East canal",
                new[]
                {
                    new[] { 0.985f, 0.01f },
                    new[] { 0.975f, 0.28f },
                    new[] { 0.990f, 0.56f },
                    new[] { 0.975f, 0.99f },
                },
                0.48f,
                new Color(0.36f, 0.76f, 0.88f, 1f),
                0.015f,
                -45);
        }

        private void BuildGreenPatches()
        {
            GameObject root = ChildRoot("Green patches");
            foreach (CityGreenPatch patch in city.green_patches.Where(item =>
                         item.construction_state == CityConstructionState.Existing))
            {
                Color colour;
                switch (patch.type)
                {
                    case CityGreenPatchType.Woodland:
                        colour = new Color(0.30f, 0.57f, 0.40f, 0.96f);
                        break;
                    case CityGreenPatchType.ShrubGarden:
                        colour = new Color(0.47f, 0.70f, 0.43f, 0.96f);
                        break;
                    default:
                        colour = new Color(0.69f, 0.84f, 0.57f, 0.92f);
                        break;
                }
                CreatePolygon(root.transform, patch.id, patch.polygon_norm, 0.025f, colour);
            }

            CreateTree(root.transform, "Tree west 1", new[] { 0.10f, 0.66f }, 0.90f);
            CreateTree(root.transform, "Tree west 2", new[] { 0.19f, 0.79f }, 0.72f);
            CreateTree(root.transform, "Tree east 1", new[] { 0.79f, 0.63f }, 0.86f);
            CreateTree(root.transform, "Tree east 2", new[] { 0.88f, 0.85f }, 0.76f);
            CreateTree(root.transform, "Garden tree", new[] { 0.42f, 0.73f }, 0.66f);
        }

        private void BuildNetworks()
        {
            GameObject roadRoot = ChildRoot("Vehicle road network");
            CityVehicleRoad[] activeRoads = city.vehicle_roads.Where(item =>
                item.construction_state == CityConstructionState.Existing).ToArray();
            foreach (CityVehicleRoad road in activeRoads)
            {
                float width = road.width_units / city.bounds.width_units * MapWidth;
                CreateLine(
                    roadRoot.transform,
                    road.id + " kerb",
                    road.points_norm,
                    width + 0.10f,
                    new Color(0.91f, 0.90f, 0.84f, 1f),
                    0.045f,
                    -8);
                CreateLine(
                    roadRoot.transform,
                    road.id + " asphalt",
                    road.points_norm,
                    width,
                    new Color(0.72f, 0.76f, 0.77f, 1f),
                    0.055f,
                    -7);
            }

            pedestrianRoot = ChildRoot("Pedestrian link network");
            CityPedestrianLink[] activeLinks = city.pedestrian_links.Where(item =>
                item.construction_state == CityConstructionState.Existing).ToArray();
            foreach (CityPedestrianLink link in activeLinks)
            {
                float width = link.width_units / city.bounds.width_units * MapWidth;
                CreateLine(
                    pedestrianRoot.transform,
                    link.id,
                    link.points_norm,
                    Mathf.Max(0.07f, width),
                    new Color(0.96f, 0.90f, 0.76f, 1f),
                    0.075f,
                    -4);
            }
            GeneratedVehicleRoadCount = activeRoads.Length;
            GeneratedPedestrianLinkCount = activeLinks.Length;
        }

        private void BuildBuildings()
        {
            GameObject root = ChildRoot("Buildings");
            CityBuilding[] activeBuildings = city.buildings.Where(item =>
                item.construction_state == CityConstructionState.Existing).ToArray();
            foreach (CityBuilding building in activeBuildings)
            {
                Color wall;
                Color roof;
                float height;
                string shortLabel;
                switch (building.type)
                {
                    case CityBuildingType.Apartment:
                        wall = new Color(0.66f, 0.77f, 0.84f, 1f);
                        roof = new Color(0.28f, 0.52f, 0.70f, 1f);
                        height = 0.34f;
                        shortLabel = "HOUSING";
                        break;
                    case CityBuildingType.DetachedHouse:
                        wall = new Color(0.94f, 0.89f, 0.75f, 1f);
                        roof = new Color(0.88f, 0.39f, 0.28f, 1f);
                        height = 0.22f;
                        shortLabel = "HOME";
                        break;
                    case CityBuildingType.Commercial:
                        wall = new Color(0.95f, 0.72f, 0.45f, 1f);
                        roof = new Color(0.91f, 0.45f, 0.25f, 1f);
                        height = 0.27f;
                        shortLabel = "SHOPS";
                        break;
                    default:
                        wall = new Color(0.68f, 0.84f, 0.82f, 1f);
                        roof = new Color(0.20f, 0.55f, 0.53f, 1f);
                        height = 0.26f;
                        shortLabel = "COMMUNITY";
                        break;
                }
                Vector3 position = ToWorld(building.position_norm, height * 0.5f + 0.09f);
                float width = building.footprint_units[0] / city.bounds.width_units * MapWidth;
                float depth = building.footprint_units[1] / city.bounds.height_units * MapHeight;
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = building.id;
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = position;
                body.transform.localScale = new Vector3(width, height, depth);
                body.transform.localRotation = Quaternion.Euler(0f, -building.rotation_deg, 0f);
                RemoveCollider(body);
                SetMaterial(body, wall);

                GameObject roofObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofObject.name = "Roof";
                roofObject.transform.SetParent(body.transform, false);
                roofObject.transform.localPosition = new Vector3(0f, 0.58f, 0f);
                roofObject.transform.localScale = new Vector3(1.07f, 0.16f, 1.07f);
                RemoveCollider(roofObject);
                SetMaterial(roofObject, roof);
                CreateLabel(root.transform, shortLabel, ToWorld(building.position_norm, height + 0.18f), 0.18f);
            }
            GeneratedBuildingCount = activeBuildings.Length;
        }

        private void BuildPlanningOverlay()
        {
            planningPreviewRoot = ChildRoot("Planning Preview Overlay");
            if (planningWorkflow == null)
            {
                return;
            }

            if (planningWorkflow.Phase == CityPlanningWorkflowPhase.Preview &&
                planningWorkflow.ConstructionPreview != null)
            {
                foreach (CityTokenChange change in planningWorkflow.ConstructionPreview.changes
                             .Where(item => item.change_kind == CityTokenChangeKind.New &&
                                            !item.blocks_confirmation))
                {
                    if (change.token_type == CityPhysicalTokenType.GreenIntervention)
                    {
                        CityGreenPatch patch = CityConstructionFactory.CreateGreenIntervention(
                            change.scanned_state,
                            city.bounds,
                            CityConstructionState.Proposed);
                        DrawPlanningPatch(patch, "SCAN PREVIEW");
                    }
                    else
                    {
                        CityBuilding building = CityConstructionFactory.CreateBuilding(
                            change.scanned_state,
                            CityConstructionState.Proposed);
                        DrawPlanningFootprint(building, "SCAN PREVIEW");
                    }
                }
                return;
            }

            foreach (CityBuilding building in planningWorkflow.CurrentState.buildings.Where(item =>
                         item.construction_state != CityConstructionState.Existing))
            {
                DrawPlanningFootprint(
                    building,
                    building.construction_state == CityConstructionState.UnderConstruction
                        ? "BUILDING"
                        : "PROPOSED");
            }
            foreach (CityGreenPatch patch in planningWorkflow.CurrentState.green_patches.Where(item =>
                         item.construction_state != CityConstructionState.Existing))
            {
                DrawPlanningPatch(
                    patch,
                    patch.construction_state == CityConstructionState.UnderConstruction
                        ? "GROWING"
                        : "PROPOSED");
            }
            foreach (CityVehicleRoad road in planningWorkflow.CurrentState.vehicle_roads.Where(item =>
                         item.construction_state != CityConstructionState.Existing))
            {
                CreateLine(
                    planningPreviewRoot.transform,
                    "Preview " + road.id,
                    road.points_norm,
                    Mathf.Max(0.08f, road.width_units / city.bounds.width_units * MapWidth * 0.45f),
                    new Color(0.18f, 0.72f, 0.82f, 1f),
                    0.095f,
                    8);
            }
            foreach (CityPedestrianLink link in planningWorkflow.CurrentState.pedestrian_links.Where(item =>
                         item.construction_state != CityConstructionState.Existing))
            {
                CreateLine(
                    planningPreviewRoot.transform,
                    "Preview " + link.id,
                    link.points_norm,
                    0.06f,
                    new Color(0.98f, 0.73f, 0.30f, 1f),
                    0.10f,
                    9);
            }
            if (planningWorkflow.Phase == CityPlanningWorkflowPhase.RouteSelection &&
                planningWorkflow.NetworkPreview != null)
            {
                foreach (CityRoadChoiceSet choice in planningWorkflow.NetworkPreview.road_choices.Where(item =>
                             item.HasSelection))
                {
                    CreateLine(
                        planningPreviewRoot.transform,
                        "Selected " + choice.SelectedCandidate.id,
                        choice.SelectedCandidate.points_norm,
                        0.10f,
                        new Color(0.18f, 0.72f, 0.82f, 1f),
                        0.105f,
                        10);
                }
            }
        }

        private void DrawPlanningFootprint(CityBuilding building, string status)
        {
            float width = building.footprint_units[0] / city.bounds.width_units * MapWidth;
            float depth = building.footprint_units[1] / city.bounds.height_units * MapHeight;
            GameObject footprint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footprint.name = $"{status} {building.id}";
            footprint.transform.SetParent(planningPreviewRoot.transform, false);
            footprint.transform.localPosition = ToWorld(building.position_norm, 0.115f);
            footprint.transform.localScale = new Vector3(width, 0.035f, depth);
            footprint.transform.localRotation = Quaternion.Euler(0f, -building.rotation_deg, 0f);
            RemoveCollider(footprint);
            SetMaterial(footprint, new Color(0.30f, 0.84f, 0.90f, 1f));
            CreateLabel(
                planningPreviewRoot.transform,
                status,
                ToWorld(building.position_norm, 0.18f),
                0.13f);
        }

        private void DrawPlanningPatch(CityGreenPatch patch, string status)
        {
            CreatePolygon(
                planningPreviewRoot.transform,
                $"{status} {patch.id}",
                patch.polygon_norm,
                0.10f,
                new Color(0.45f, 0.82f, 0.62f, 1f));
            float[] centre =
            {
                patch.polygon_norm.Average(point => point[0]),
                patch.polygon_norm.Average(point => point[1]),
            };
            CreateLabel(planningPreviewRoot.transform, status, ToWorld(centre, 0.16f), 0.13f);
        }

        private void BuildAmenities()
        {
            GameObject root = ChildRoot("Digital amenities");
            CityAmenity[] activeAmenities = (city.amenities ?? Array.Empty<CityAmenity>())
                .Where(item => item != null &&
                               item.construction_state == CityConstructionState.Existing)
                .ToArray();
            foreach (CityAmenity amenity in activeAmenities)
            {
                Vector3 position = ToWorld(amenity.position_norm, 0.12f);
                if (amenity.type == CityAmenityType.Bench)
                {
                    GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    seat.name = amenity.id;
                    seat.transform.SetParent(root.transform, false);
                    seat.transform.localPosition = position;
                    seat.transform.localScale = new Vector3(0.48f, 0.09f, 0.18f);
                    RemoveCollider(seat);
                    SetMaterial(seat, new Color(0.66f, 0.43f, 0.24f, 1f));

                    for (int side = -1; side <= 1; side += 2)
                    {
                        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        leg.name = side < 0 ? "West leg" : "East leg";
                        leg.transform.SetParent(seat.transform, false);
                        leg.transform.localPosition = new Vector3(side * 0.30f, -0.60f, 0f);
                        leg.transform.localScale = new Vector3(0.10f, 1.1f, 0.55f);
                        RemoveCollider(leg);
                        SetMaterial(leg, new Color(0.21f, 0.34f, 0.35f, 1f));
                    }
                }
                else
                {
                    GameObject bin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bin.name = amenity.id;
                    bin.transform.SetParent(root.transform, false);
                    bin.transform.localPosition = position;
                    bin.transform.localScale = new Vector3(0.13f, 0.16f, 0.13f);
                    RemoveCollider(bin);
                    SetMaterial(bin, new Color(0.18f, 0.49f, 0.43f, 1f));
                }
            }
            GeneratedAmenityCount = activeAmenities.Length;
        }

        private void BuildActors()
        {
            GameObject root = ChildRoot("Representative mobility agents");
            actorViews = new ActorView[plan.trips.Length];
            string[] humanSprites =
            {
                "UrbanWildlife/Humans/walker-topdown-v02",
                "UrbanWildlife/Humans/visitor-topdown-v02",
                "UrbanWildlife/Humans/walker-topdown-v04",
                "UrbanWildlife/Humans/visitor-topdown-v04",
            };
            for (int index = 0; index < plan.trips.Length; index += 1)
            {
                GameObject human = CreateHumanToken(root.transform, index, humanSprites[index % humanSprites.Length]);
                GameObject vehicle = CreateVehicleToken(root.transform, index);
                human.SetActive(false);
                vehicle.SetActive(false);
                actorViews[index] = new ActorView { human = human, vehicle = vehicle };
            }
        }

        private void BuildWildlife()
        {
            wildlifeViews.Clear();
            GameObject root = ChildRoot("City wildlife agents");
            foreach (CityWildlifeAgent agent in wildlife.Snapshot.agents)
            {
                GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                token.name = agent.id;
                token.transform.SetParent(root.transform, false);
                token.transform.localScale = new Vector3(0.16f, 0.035f, 0.16f);
                RemoveCollider(token);
                SetMaterial(token, WildlifeColour(agent.species));
                CreateLabel(
                    token.transform,
                    WildlifeLabel(agent.species),
                    new Vector3(0f, 0.7f, 0f),
                    0.11f);
                wildlifeViews.Add(agent.id, token);
            }
            UpdateWildlife();
        }

        private void UpdateWildlife()
        {
            if (wildlife?.Snapshot?.agents == null)
            {
                return;
            }
            foreach (CityWildlifeAgent agent in wildlife.Snapshot.agents)
            {
                if (!wildlifeViews.TryGetValue(agent.id, out GameObject view))
                {
                    continue;
                }
                bool active = agent.life_state == CityWildlifeLifeState.Active;
                view.SetActive(active);
                if (active)
                {
                    view.transform.localPosition = ToWorld(agent.position_norm, 0.15f);
                }
            }
        }

        private static Color WildlifeColour(CityWildlifeSpecies species)
        {
            switch (species)
            {
                case CityWildlifeSpecies.Pigeon:
                    return new Color(0.38f, 0.52f, 0.63f, 1f);
                case CityWildlifeSpecies.GreySquirrel:
                    return new Color(0.78f, 0.47f, 0.22f, 1f);
                case CityWildlifeSpecies.Fox:
                    return new Color(0.85f, 0.34f, 0.16f, 1f);
                default:
                    return new Color(0.38f, 0.31f, 0.27f, 1f);
            }
        }

        private static string WildlifeLabel(CityWildlifeSpecies species)
        {
            switch (species)
            {
                case CityWildlifeSpecies.Pigeon:
                    return "P";
                case CityWildlifeSpecies.GreySquirrel:
                    return "S";
                case CityWildlifeSpecies.Fox:
                    return "F";
                default:
                    return "H";
            }
        }

        private GameObject CreateHumanToken(Transform parent, int index, string spritePath)
        {
            GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            token.name = $"Representative human {index + 1:00}";
            token.transform.SetParent(parent, false);
            token.transform.localScale = new Vector3(0.24f, 0.035f, 0.24f);
            RemoveCollider(token);
            SetMaterial(token, new Color(0.70f, 0.47f, 0.24f, 1f));

            Sprite sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                GameObject artwork = new GameObject("Human artwork");
                artwork.transform.SetParent(token.transform, false);
                artwork.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                float scale = 0.55f / Mathf.Max(0.001f, sprite.bounds.size.y);
                artwork.transform.localScale = new Vector3(scale / 0.24f, scale / 0.24f, scale / 0.24f);
                SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 40;
            }
            return token;
        }

        private GameObject CreateVehicleToken(Transform parent, int index)
        {
            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = $"Trip vehicle {index + 1:00}";
            vehicle.transform.SetParent(parent, false);
            vehicle.transform.localScale = new Vector3(0.52f, 0.14f, 0.28f);
            RemoveCollider(vehicle);
            Color[] colours =
            {
                new Color(0.16f, 0.36f, 0.50f, 1f),
                new Color(0.55f, 0.18f, 0.12f, 1f),
                new Color(0.77f, 0.61f, 0.18f, 1f),
            };
            SetMaterial(vehicle, colours[index % colours.Length]);
            return vehicle;
        }

        private void UpdateActors()
        {
            CityTripAgent[] agents = mobility.Agents;
            for (int index = 0; index < agents.Length; index += 1)
            {
                CityTripAgent agent = agents[index];
                ActorView view = actorViews[index];
                bool active = agent.State != CityTripMotionState.Pending &&
                              agent.State != CityTripMotionState.Complete;
                bool travellingByCar = agent.Trip.mode == CityTravelMode.Drive &&
                                       (agent.State == CityTripMotionState.Outbound ||
                                        agent.State == CityTripMotionState.Returning);
                view.human.SetActive(active && !travellingByCar);
                view.vehicle.SetActive(active && agent.Trip.mode == CityTravelMode.Drive);
                if (!active)
                {
                    continue;
                }

                Vector3 position = ToWorld(agent.PositionNorm, 0.18f);
                GameObject movingObject = travellingByCar ? view.vehicle : view.human;
                movingObject.transform.localPosition = position;
                if (agent.Trip.mode == CityTravelMode.Drive && !travellingByCar)
                {
                    view.vehicle.transform.localPosition = position + new Vector3(0.22f, 0f, 0.16f);
                }
                if (view.hasLastPosition)
                {
                    Vector3 delta = position - view.lastPosition;
                    if (delta.sqrMagnitude > 0.00001f)
                    {
                        float heading = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                        movingObject.transform.localRotation = Quaternion.Euler(0f, heading, 0f);
                    }
                }
                view.lastPosition = position;
                view.hasLastPosition = true;
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || plan == null || mobility == null)
            {
                return;
            }
            EnsureStyles();
            float panelX = Screen.width * 0.77f;
            float panelWidth = Screen.width * 0.23f;
            GUILayout.BeginArea(new Rect(panelX, 0f, panelWidth, Screen.height), panelStyle);
            sidebarScroll = GUILayout.BeginScrollView(sidebarScroll, false, true);
            GUILayout.Space(20f);
            GUILayout.Label("RIVERSIDE WILDLIFE DISTRICT", headingStyle);
            GUILayout.Label("LIVE CITY", titleStyle);
            GUILayout.Label("A shared city for people and wildlife", bodyStyle);
            if (strategy?.Snapshot != null)
            {
                GUILayout.Label(
                    $"{strategy.Snapshot.phase}  ·  {strategy.Snapshot.time_block}",
                    headingStyle);
                GUILayout.Label(
                    $"DP {strategy.Snapshot.development_points}  ·  Balance {strategy.Snapshot.balance.total:0}",
                    metricStyle);
            }
            GUILayout.Space(18f);

            DrawPlanningWorkflowPanel();
            GUILayout.Space(18f);

            GUILayout.Label(paused ? "PAUSED" : "LIVE CITY  ·  ×2", headingStyle);
            GUILayout.Space(8f);
            GUILayout.Label($"Active residents  {mobility.ActiveHumanAgentCount}/{plan.RepresentativeAgentCount}", metricStyle);
            GUILayout.Label($"Represented people  {plan.RepresentedPopulation}", metricStyle);
            GUILayout.Label($"Walk / Drive  {plan.WalkTripCount} / {plan.DriveTripCount}", metricStyle);
            GUILayout.Label($"Active vehicles  {mobility.ActiveVehicleAgents.Length}", metricStyle);
            GUILayout.Label($"Completed returns  {mobility.CompletedTripCount}", metricStyle);
            if (environment?.Snapshot != null)
            {
                CityEnvironmentSnapshot snapshot = environment.Snapshot;
                GUILayout.Space(12f);
                GUILayout.Label("CITY ENVIRONMENT", headingStyle);
                GUILayout.Label($"Natural food  {snapshot.natural_food_total:0.00}", metricStyle);
                GUILayout.Label($"Human food  {snapshot.anthropogenic_food_total:0.00}", metricStyle);
                GUILayout.Label(
                    $"Waste  {snapshot.waste_demand:0.0} / {snapshot.waste_capacity:0.0}",
                    metricStyle);
                GUILayout.Label(
                    snapshot.overflow_active ? "● OVERFLOW · litter hotspot" : "● Waste contained",
                    bodyStyle);
                GUILayout.Label($"Mean disturbance  {snapshot.average_disturbance:0.00}", bodyStyle);
            }
            if (wildlife?.Snapshot != null)
            {
                GUILayout.Space(12f);
                GUILayout.Label("URBAN WILDLIFE", headingStyle);
                GUILayout.Label(
                    $"Active  {wildlife.Snapshot.active_count}  ·  Feeding  {wildlife.Snapshot.feeding_events}",
                    metricStyle);
                GUILayout.Label(
                    $"Migrated  {wildlife.Snapshot.migrated_count}  ·  Roadkill  {wildlife.Snapshot.roadkill_events}",
                    bodyStyle);
                GUILayout.Label(
                    $"Traces  H {HumanTracePointCount}  ·  A {AnimalTracePointCount}",
                    bodyStyle);
            }
            GUILayout.Space(16f);

            GUILayout.Label("MAP KEY", headingStyle);
            GUILayout.Label("■ Housing   ■ Shops\n■ Community   ■ Park / green space\n■ Water   ━ Road   ━ Footpath", bodyStyle);
            GUILayout.Space(16f);

            GUILayout.Label("DESTINATION PRESSURE", headingStyle);
            foreach (CityDestinationLoad load in plan.destination_loads.Where(item =>
                         item.assigned_representative_agents > 0))
            {
                string status = load.crowd_penalty > 0f ? "CROWDED" : "COMFORTABLE";
                GUILayout.Label(
                    $"{load.building_id}\n  {load.assigned_people}/{load.comfortable_capacity} people · {status}",
                    bodyStyle);
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(paused ? "RESUME" : "PAUSE", buttonStyle))
            {
                paused = !paused;
            }
            if (GUILayout.Button("RESTART TRIPS", buttonStyle))
            {
                InitializePrototype(false);
            }
            if (GUILayout.Button(showPedestrianNetwork ? "HIDE FOOTPATHS" : "SHOW FOOTPATHS", buttonStyle))
            {
                showPedestrianNetwork = !showPedestrianNetwork;
                if (pedestrianRoot != null)
                {
                    pedestrianRoot.SetActive(showPedestrianNetwork);
                }
            }
            GUILayout.Space(14f);
            GUILayout.Label(
                "Residents emerge from housing and respond to destination appeal, distance and crowding. Vehicles only appear for Drive trips.",
                bodyStyle);
            GUILayout.Space(18f);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawPlanningWorkflowPanel()
        {
            if (planningWorkflow == null)
            {
                return;
            }
            CityPlanningWorkflowSnapshot snapshot = planningWorkflow.Snapshot;
            GUILayout.Label("CITY PLANNING", headingStyle);
            GUILayout.Label(WorkflowStepLabel(snapshot.phase), metricStyle);
            GUILayout.Label(snapshot.message, bodyStyle);
            GUILayout.Space(7f);

            switch (snapshot.phase)
            {
                case CityPlanningWorkflowPhase.ReadyToScan:
                    if (GUILayout.Button("SCAN CITY · ELECTRONIC TEST", buttonStyle))
                    {
                        CityTokenScanPacket scan = CityPlanningDemoScanFactory.CreateElectronicSample(
                            city,
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        planningWorkflow.TryAcceptScan(scan, out _);
                        RefreshPlanningOverlay();
                    }
                    GUILayout.Label(
                        "Uses a stable sample frame now; the same validated packet will later come from the overhead camera.",
                        bodyStyle);
                    break;

                case CityPlanningWorkflowPhase.Preview:
                    CityConstructionPreview construction = snapshot.construction_preview;
                    if (construction != null)
                    {
                        GUILayout.Label(
                            $"New {construction.NewCount}  ·  Moved {construction.MovedCount}  ·  Missing {construction.MissingCount}",
                            bodyStyle);
                    }
                    GUI.enabled = snapshot.CanConfirmPreview;
                    if (GUILayout.Button("CONFIRM PREVIEW", buttonStyle))
                    {
                        planningWorkflow.ConfirmPreview(out _);
                        RefreshPlanningOverlay();
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("CANCEL PREVIEW", buttonStyle))
                    {
                        planningWorkflow.CancelPreview();
                        RefreshPlanningOverlay();
                    }
                    break;

                case CityPlanningWorkflowPhase.RouteSelection:
                    DrawRouteChoices(snapshot.network_preview);
                    break;

                case CityPlanningWorkflowPhase.ReadyToBuild:
                    GUILayout.Label(
                        $"Cost {snapshot.required_development_points} DP  ·  Available {snapshot.strategy.development_points} DP",
                        bodyStyle);
                    GUI.enabled = snapshot.CanStartConstruction;
                    if (GUILayout.Button("START CONSTRUCTION", buttonStyle))
                    {
                        if (planningWorkflow.StartConstruction(out _))
                        {
                            InitializePrototype(false);
                        }
                    }
                    GUI.enabled = true;
                    break;

                case CityPlanningWorkflowPhase.Construction:
                    int active = snapshot.strategy.projects.Count(item =>
                        item.state == CityStrategyProjectState.Active);
                    int total = snapshot.strategy.projects.Length;
                    GUILayout.Label($"Active projects  {active}/{total}", bodyStyle);
                    foreach (CityStrategyProject project in snapshot.strategy.projects.Where(item =>
                                 item.state == CityStrategyProjectState.Active).Take(3))
                    {
                        GUILayout.Label(
                            $"{project.target_id}  ·  {project.remaining_time_blocks} block(s)",
                            bodyStyle);
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("PAUSE", buttonStyle)) planningWorkflow.SetTimeScale(0f);
                    if (GUILayout.Button("1×", buttonStyle)) planningWorkflow.SetTimeScale(1f);
                    if (GUILayout.Button("2×", buttonStyle)) planningWorkflow.SetTimeScale(2f);
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("ADVANCE ONE BLOCK", buttonStyle))
                    {
                        planningWorkflow.AdvanceConstructionBlock();
                        if (planningWorkflow.Phase == CityPlanningWorkflowPhase.Complete)
                        {
                            InitializePrototype(false);
                        }
                        else
                        {
                            RefreshPlanningOverlay();
                        }
                    }
                    break;

                case CityPlanningWorkflowPhase.Complete:
                    GUILayout.Label(
                        $"Built {snapshot.completed_object_ids.Length} confirmed city objects.",
                        bodyStyle);
                    GUILayout.Label("The Preview footprints are now live city geometry.", bodyStyle);
                    break;
            }
        }

        private void DrawRouteChoices(CityNetworkPlanPreview preview)
        {
            if (preview == null)
            {
                return;
            }
            foreach (CityRoadChoiceSet choice in preview.road_choices)
            {
                GUILayout.Label(choice.building_id, bodyStyle);
                GUILayout.BeginHorizontal();
                DrawRoadChoiceButton(choice, CityRoadRouteOption.Direct, "DIRECT");
                DrawRoadChoiceButton(choice, CityRoadRouteOption.ExistingNetwork, "EXISTING");
                DrawRoadChoiceButton(choice, CityRoadRouteOption.LowImpact, "LOW");
                GUILayout.EndHorizontal();
                if (choice.HasSelection)
                {
                    GUILayout.Label(
                        $"Selected: {choice.SelectedCandidate.route_option} · " +
                        $"green impact {choice.SelectedCandidate.estimated_green_impact_units:0.0}",
                        bodyStyle);
                }
            }
            if (GUILayout.Button("USE LOW-IMPACT ROUTES", buttonStyle))
            {
                planningWorkflow.SelectRecommendedLowImpactRoutes(out _);
                RefreshPlanningOverlay();
            }
            GUI.enabled = preview.CanConfirm;
            if (GUILayout.Button("CONFIRM ROUTES", buttonStyle))
            {
                if (planningWorkflow.ConfirmRoutes(out _))
                {
                    InitializePrototype(false);
                }
            }
            GUI.enabled = true;
        }

        private void DrawRoadChoiceButton(
            CityRoadChoiceSet choice,
            CityRoadRouteOption option,
            string label)
        {
            bool available = choice.candidates.Any(candidate => candidate.route_option == option);
            bool previous = GUI.enabled;
            GUI.enabled = previous && available;
            if (GUILayout.Button(label, buttonStyle))
            {
                planningWorkflow.SelectRoadOption(choice.building_id, option, out _);
                RefreshPlanningOverlay();
            }
            GUI.enabled = previous;
        }

        private static string WorkflowStepLabel(CityPlanningWorkflowPhase phase)
        {
            switch (phase)
            {
                case CityPlanningWorkflowPhase.ReadyToScan: return "1  SCAN CITY";
                case CityPlanningWorkflowPhase.Preview: return "2  PREVIEW";
                case CityPlanningWorkflowPhase.RouteSelection: return "3  CHOOSE ACCESS";
                case CityPlanningWorkflowPhase.ReadyToBuild: return "4  COMMIT DP";
                case CityPlanningWorkflowPhase.Construction: return "5  CONSTRUCTION";
                default: return "COMPLETE";
            }
        }

        private void RefreshPlanningOverlay()
        {
            if (planningPreviewRoot != null)
            {
                planningPreviewRoot.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(planningPreviewRoot);
                }
                else
                {
                    DestroyImmediate(planningPreviewRoot);
                }
                planningPreviewRoot = null;
            }
            BuildPlanningOverlay();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(20, 20, 10, 18),
                normal = { background = SolidTexture(new Color(0.975f, 0.965f, 0.93f, 0.995f)) },
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.10f, 0.18f, 0.23f, 1f) },
            };
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.16f, 0.50f, 0.55f, 1f) },
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.25f, 0.31f, 0.32f, 1f) },
            };
            metricStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.12f, 0.25f, 0.27f, 1f) },
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 42f,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 5, 5),
                normal =
                {
                    background = SolidTexture(new Color(0.78f, 0.90f, 0.92f, 1f)),
                    textColor = new Color(0.10f, 0.22f, 0.26f, 1f),
                },
                hover =
                {
                    background = SolidTexture(new Color(0.67f, 0.85f, 0.88f, 1f)),
                    textColor = new Color(0.08f, 0.18f, 0.22f, 1f),
                },
            };
        }

        private void CreateTree(Transform parent, string name, float[] normalized, float scale)
        {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = ToWorld(normalized, 0.08f);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 0.14f * scale, 0f);
            trunk.transform.localScale = new Vector3(0.06f * scale, 0.14f * scale, 0.06f * scale);
            RemoveCollider(trunk);
            SetMaterial(trunk, new Color(0.55f, 0.38f, 0.22f, 1f));

            GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 0.36f * scale, 0f);
            canopy.transform.localScale = new Vector3(0.34f, 0.24f, 0.30f) * scale;
            RemoveCollider(canopy);
            SetMaterial(canopy, new Color(0.25f, 0.57f, 0.39f, 1f));
        }

        private GameObject ChildRoot(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(generatedRoot.transform, false);
            return child;
        }

        private void CreateLine(
            Transform parent,
            string name,
            float[][] points,
            float width,
            Color colour,
            float height,
            int sortingOrder)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 5;
            line.numCapVertices = 5;
            line.startColor = Color.white;
            line.endColor = Color.white;
            line.material = MaterialFor(colour);
            line.sortingOrder = sortingOrder;
            for (int index = 0; index < points.Length; index += 1)
            {
                line.SetPosition(index, ToWorld(points[index], height));
            }
        }

        private void CreatePolygon(
            Transform parent,
            string name,
            float[][] points,
            float height,
            Color colour)
        {
            GameObject polygon = new GameObject(name);
            polygon.transform.SetParent(parent, false);
            Mesh mesh = new Mesh { name = name + " mesh" };
            Vector3[] vertices = points.Select(point => ToWorld(point, height)).ToArray();
            int[] triangles = new int[(points.Length - 2) * 3];
            for (int index = 0; index < points.Length - 2; index += 1)
            {
                triangles[index * 3] = 0;
                triangles[index * 3 + 1] = index + 1;
                triangles[index * 3 + 2] = index + 2;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            polygon.AddComponent<MeshFilter>().sharedMesh = mesh;
            polygon.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(colour);
        }

        private void CreateLabel(
            Transform parent,
            string text,
            Vector3 position,
            float characterSize)
        {
            GameObject labelObject = new GameObject(text + " label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = position;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = characterSize;
            label.color = new Color(0.10f, 0.23f, 0.28f, 1f);
        }

        private Vector3 ToWorld(float[] normalized, float height)
        {
            return new Vector3(
                (normalized[0] - 0.5f) * MapWidth,
                height,
                (0.5f - normalized[1]) * MapHeight);
        }

        private static void SetMaterial(GameObject target, Color colour)
        {
            target.GetComponent<Renderer>().sharedMaterial = MaterialFor(colour);
        }

        private static Material MaterialFor(Color colour)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                color = colour,
                hideFlags = HideFlags.DontSave,
            };
            return material;
        }

        private static Texture2D SolidTexture(Color colour)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixel(0, 0, colour);
            texture.Apply();
            return texture;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }
        }

        private void ClearGenerated()
        {
            if (generatedRoot == null)
            {
                Transform existing = transform.Find("Generated City Prototype");
                generatedRoot = existing == null ? null : existing.gameObject;
            }
            if (generatedRoot == null)
            {
                return;
            }
            generatedRoot.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(generatedRoot);
            }
            else
            {
                DestroyImmediate(generatedRoot);
            }
            generatedRoot = null;
        }
    }
}

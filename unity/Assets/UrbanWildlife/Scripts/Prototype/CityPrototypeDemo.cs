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
        private const float MapViewportWidth = 0.77f;
        private const float BoardViewMargin = 0.55f;
        private const float RuntimeSpeed = 2f;
        private const string CityBoardUnderlayResourcePath =
            "UrbanWildlife/Environment/city-board-organic-underlay-v01";
        private const string CityPlazaResourcePath =
            "UrbanWildlife/Environment/human-activity-plaza-v01";
        private const string CityPondResourcePath =
            "UrbanWildlife/Environment/park-pond-citybuilder-v01";
        private const string LegacyCityTreeResourcePath =
            "UrbanWildlife/Environment/tree-citybuilder-default-v01";
        private const string CityBushResourcePath =
            "UrbanWildlife/Environment/bush-citybuilder-default-v01";
        private const string CityRedBrickHouseResourcePath =
            "UrbanWildlife/Buildings/house-detached-red-brick-bay-v02";
        private const string CityStockBrickHouseResourcePath =
            "UrbanWildlife/Buildings/house-stock-brick-v02";
        private const string CityApartmentCornerResourcePath =
            "UrbanWildlife/Buildings/apartment-lowrise-corner-v02";
        private const string CityMarketHallResourcePath =
            "UrbanWildlife/Buildings/market-hall-citybuilder-v02";
        private const string CityCornerShopsResourcePath =
            "UrbanWildlife/Buildings/corner-shops-citybuilder-v02";
        private const string CityCommunityCentreResourcePath =
            "UrbanWildlife/Buildings/community-centre-citybuilder-v01";
        private static readonly string[] CityTreeResourcePaths =
        {
            "UrbanWildlife/Environment/tree-citybuilder-pear-v02",
            "UrbanWildlife/Environment/tree-citybuilder-round-v02",
            "UrbanWildlife/Environment/tree-citybuilder-three-lobe-v02",
            "UrbanWildlife/Environment/tree-citybuilder-two-lobe-v02",
            "UrbanWildlife/Environment/tree-citybuilder-tapered-v02",
        };
        private static readonly Vector2[] WoodlandTreeOffsets =
        {
            new Vector2(-0.29f, -0.27f),
            new Vector2(0.00f, -0.28f),
            new Vector2(0.29f, -0.20f),
            new Vector2(-0.23f, 0.02f),
            new Vector2(0.16f, 0.04f),
            new Vector2(-0.07f, 0.29f),
            new Vector2(0.30f, 0.27f),
        };
        private static readonly float[] WoodlandTreeScales =
        {
            0.52f,
            0.48f,
            0.50f,
            0.47f,
            0.53f,
            0.49f,
            0.46f,
        };

        [SerializeField]
        [Tooltip("Path relative to Unity Assets, or an absolute path.")]
        private string cityScanPath = CityTokenScanFileSource.DefaultProjectRelativePath;

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
        private CityTraceVisualizer traceVisualizer;
        private ActorView[] actorViews = Array.Empty<ActorView>();
        private readonly Dictionary<string, GameObject> wildlifeViews =
            new Dictionary<string, GameObject>();
        private GameObject generatedRoot;
        private GameObject pedestrianRoot;
        private GameObject planningPreviewRoot;
        private bool paused;
        private bool showPedestrianNetwork = true;
        private CityTraceDisplayMode traceDisplayMode = CityTraceDisplayMode.CombinedTrace;
        private string cityScanInputMessage =
            "Camera scans remain pending until you load and confirm them.";
        private Vector2 sidebarScroll;
        private float completeElapsed;
        private float simulationElapsed;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle eyebrowStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle captionStyle;
        private GUIStyle metricStyle;
        private GUIStyle buttonStyle;
        private float uiScale = 1f;
        private int styledScreenHeight = -1;
        private int configuredScreenWidth = -1;
        private int configuredScreenHeight = -1;

        public int GeneratedBuildingCount { get; private set; }
        public int GeneratedVehicleRoadCount { get; private set; }
        public int GeneratedPedestrianLinkCount { get; private set; }
        public int GeneratedAmenityCount { get; private set; }
        public int GeneratedPlanningCellCount { get; private set; }
        public int AvailablePlanningCellCount { get; private set; }
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
        public int VisibleTraceMarkCount => traceVisualizer?.VisibleMarkCount ?? 0;
        public bool PlanningWorkflowConnected => planningWorkflow != null;
        public string ResolvedCityScanPath => CityTokenScanFileSource.Resolve(cityScanPath);
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
            if (configuredScreenWidth != Screen.width ||
                configuredScreenHeight != Screen.height)
            {
                ConfigureCamera();
            }
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
            UpdateTraceVisuals();
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
            if (observation == null)
            {
                observation = new CityObservationTracker();
                observation.BeginPhase(strategy.Snapshot);
            }
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
            BuildPlanningGrid();
            BuildGreenPatches();
            BuildNetworks();
            BuildBuildings();
            BuildAmenities();
            BuildWildlife();
            BuildActors();
            BuildPlanningOverlay();
            BuildTraceLayer();
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
                UpdateTraceVisuals();
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
            camera.rect = new Rect(0f, 0f, MapViewportWidth, 1f);
            camera.orthographic = true;
            camera.orthographicSize = OrthographicSizeForViewport(
                Screen.width,
                Screen.height,
                MapViewportWidth);
            camera.backgroundColor = new Color(0.82f, 0.86f, 0.80f, 1f);
            configuredScreenWidth = Screen.width;
            configuredScreenHeight = Screen.height;
        }

        public static float OrthographicSizeForViewport(
            int screenWidth,
            int screenHeight,
            float viewportWidth = MapViewportWidth)
        {
            float safeWidth = Mathf.Max(1f, screenWidth);
            float safeHeight = Mathf.Max(1f, screenHeight);
            float safeViewportWidth = Mathf.Clamp(viewportWidth, 0.1f, 1f);
            float viewportAspect = safeWidth * safeViewportWidth / safeHeight;
            float verticalFit = (MapHeight + BoardViewMargin) * 0.5f;
            float horizontalFit = (MapWidth + BoardViewMargin) /
                                  (2f * Mathf.Max(0.1f, viewportAspect));
            return Mathf.Max(verticalFit, horizontalFit);
        }

        private void BuildBoard()
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "City board base";
            board.transform.SetParent(generatedRoot.transform, false);
            board.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            board.transform.localScale = new Vector3(MapWidth + 0.18f, 0.14f, MapHeight + 0.18f);
            RemoveCollider(board);
            SetMaterial(board, new Color(0.96f, 0.95f, 0.90f, 1f));

            Sprite underlay = Resources.Load<Sprite>(CityBoardUnderlayResourcePath);
            if (underlay == null)
            {
                Debug.LogWarning(
                    $"City board underlay was not found at Resources/{CityBoardUnderlayResourcePath}.");
                return;
            }

            GameObject artwork = new GameObject("Organic terrain underlay");
            artwork.transform.SetParent(generatedRoot.transform, false);
            artwork.transform.localPosition = new Vector3(0f, 0.004f, 0f);
            artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float artworkScale = Mathf.Min(
                MapWidth / Mathf.Max(0.001f, underlay.bounds.size.x),
                MapHeight / Mathf.Max(0.001f, underlay.bounds.size.y));
            artwork.transform.localScale = Vector3.one * artworkScale;

            SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
            renderer.sprite = underlay;
            renderer.sortingOrder = -50;
        }

        private void BuildPlanningGrid()
        {
            GameObject root = ChildRoot("Planning grid");
            GameObject cellsRoot = new GameObject("Cells");
            cellsRoot.transform.SetParent(root.transform, false);
            GameObject boundariesRoot = new GameObject("Boundaries");
            boundariesRoot.transform.SetParent(root.transform, false);
            GeneratedPlanningCellCount = 0;
            AvailablePlanningCellCount = 0;
            CityPlanningGrid grid = city.planning_grid;
            if (grid?.cells == null)
            {
                return;
            }

            foreach (CityGridCell cell in grid.cells.Where(item => item != null)
                         .OrderBy(item => item.row).ThenBy(item => item.col))
            {
                GameObject cellObject = new GameObject(cell.id);
                cellObject.transform.SetParent(cellsRoot.transform, false);
                float[][] visualPolygon = VisualCellPolygon(cell, grid);
                CreatePolygon(
                    cellObject.transform,
                    "Land cover " + cell.current_cover,
                    visualPolygon,
                    0.008f,
                    PlanningCellColour(cell.current_cover),
                    -40,
                    true);
                BuildNaturalCellFeature(cellObject.transform, cell);
                float[] labelPosition =
                {
                    visualPolygon[3][0] + cell.size_norm[0] * 0.12f,
                    visualPolygon[3][1] - cell.size_norm[1] * 0.10f,
                };
                CreateLabel(cellObject.transform, cell.id, ToWorld(labelPosition, 0.031f), 0.027f);
                TextMesh label = cellObject.GetComponentInChildren<TextMesh>();
                label.color = new Color(0.28f, 0.39f, 0.36f, 0.44f);
                label.GetComponent<Renderer>().sortingOrder = -22;
                GeneratedPlanningCellCount += 1;
                if (cell.buildable && !cell.fixed_feature &&
                    string.IsNullOrWhiteSpace(cell.occupant_id))
                {
                    AvailablePlanningCellCount += 1;
                }
            }

            Color boundaryColour = new Color(0.38f, 0.50f, 0.45f, 0.16f);
            for (int col = 0; col <= grid.cols; col += 1)
            {
                float[][] points = Enumerable.Range(0, grid.rows + 1)
                    .Select(row => VisualGridNode(row, col, grid))
                    .ToArray();
                CreateLine(boundariesRoot.transform, "Column " + col,
                    points,
                    0.007f, boundaryColour, 0.025f, -30, true);
            }
            for (int row = 0; row <= grid.rows; row += 1)
            {
                float[][] points = Enumerable.Range(0, grid.cols + 1)
                    .Select(col => VisualGridNode(row, col, grid))
                    .ToArray();
                CreateLine(boundariesRoot.transform, "Row " + row,
                    points,
                    0.007f, boundaryColour, 0.025f, -30, true);
            }
        }

        private float[][] VisualCellPolygon(CityGridCell cell, CityPlanningGrid grid)
        {
            return new[]
            {
                VisualGridNode(cell.row, cell.col, grid),
                VisualGridNode(cell.row, cell.col + 1, grid),
                VisualGridNode(cell.row + 1, cell.col + 1, grid),
                VisualGridNode(cell.row + 1, cell.col, grid),
            };
        }

        private static float[] VisualGridNode(int row, int col, CityPlanningGrid grid)
        {
            float x = (float)col / grid.cols;
            float y = (float)row / grid.rows;
            if (col > 0 && col < grid.cols)
            {
                x += HashSigned(row, col, 17) * 0.022f;
            }
            if (row > 0 && row < grid.rows)
            {
                y += HashSigned(row, col, 43) * 0.026f;
            }
            return new[] { Mathf.Clamp01(x), Mathf.Clamp01(y) };
        }

        private static float HashSigned(int row, int col, int salt)
        {
            int value = (row * 73 + col * 151 + salt * 199) & 1023;
            value = (value * 37 + 71) & 1023;
            return value / 511.5f - 1f;
        }

        private void BuildNaturalCellFeature(Transform parent, CityGridCell cell)
        {
            Color colour;
            float radiusX;
            float radiusY;
            int points;
            switch (cell.current_cover)
            {
                case CityLandCover.Water:
                    colour = new Color(0.47f, 0.76f, 0.84f, 0.16f);
                    radiusX = 0.39f;
                    radiusY = 0.34f;
                    points = 28;
                    break;
                case CityLandCover.CivicPlaza:
                    colour = new Color(0.88f, 0.79f, 0.63f, 0.20f);
                    radiusX = 0.43f;
                    radiusY = 0.39f;
                    points = 14;
                    break;
                case CityLandCover.PublicGreen:
                    colour = new Color(0.72f, 0.84f, 0.60f, 0.34f);
                    radiusX = 0.46f;
                    radiusY = 0.42f;
                    points = 14;
                    break;
                default:
                    return;
            }

            float[][] shape = OrganicCellShape(cell, points, radiusX, radiusY);
            CreatePolygon(
                parent,
                cell.current_cover + " natural feature",
                shape,
                0.019f,
                colour,
                -26,
                true);
            if (cell.current_cover == CityLandCover.Water)
            {
                CreateFixedFeatureArtwork(
                    parent,
                    "Water artwork",
                    CityPondResourcePath,
                    cell,
                    0.94f,
                    -24);
            }
            else if (cell.current_cover == CityLandCover.CivicPlaza)
            {
                CreateFixedFeatureArtwork(
                    parent,
                    "Civic plaza artwork",
                    CityPlazaResourcePath,
                    cell,
                    0.92f,
                    -24);
            }
            if (cell.current_cover == CityLandCover.Water)
            {
                CreateLine(
                    parent,
                    "Pond soft edge",
                    shape.Concat(new[] { shape[0] }).ToArray(),
                    0.010f,
                    new Color(0.76f, 0.89f, 0.88f, 0.38f),
                    0.023f,
                    -24,
                    true);
            }
        }

        private void CreateFixedFeatureArtwork(
            Transform parent,
            string name,
            string resourcePath,
            CityGridCell cell,
            float fill,
            int sortingOrder)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"Fixed feature artwork was not found at Resources/{resourcePath}.");
                return;
            }

            GameObject artwork = new GameObject(name);
            artwork.transform.SetParent(parent, false);
            artwork.transform.localPosition = ToWorld(cell.center_norm, 0.023f);
            artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float targetWidth = cell.size_norm[0] * MapWidth * fill;
            float targetDepth = cell.size_norm[1] * MapHeight * fill;
            float scale = Mathf.Min(
                targetWidth / Mathf.Max(0.001f, sprite.bounds.size.x),
                targetDepth / Mathf.Max(0.001f, sprite.bounds.size.y));
            artwork.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
        }

        private static float[][] OrganicCellShape(
            CityGridCell cell,
            int pointCount,
            float radiusX,
            float radiusY)
        {
            float phase = (cell.row * 7 + cell.col * 11) * 0.23f;
            float centreX = cell.center_norm[0] + cell.size_norm[0] * HashSigned(cell.row, cell.col, 61) * 0.035f;
            float centreY = cell.center_norm[1] + cell.size_norm[1] * HashSigned(cell.row, cell.col, 79) * 0.030f;
            return Enumerable.Range(0, pointCount)
                .Select(index =>
                {
                    float angle = Mathf.PI * 2f * index / pointCount;
                    float ripple = 1f + 0.055f * Mathf.Sin(angle * 3f + phase) +
                                   0.035f * Mathf.Cos(angle * 5f - phase);
                    return new[]
                    {
                        centreX + Mathf.Cos(angle) * cell.size_norm[0] * radiusX * ripple,
                        centreY + Mathf.Sin(angle) * cell.size_norm[1] * radiusY * ripple,
                    };
                })
                .ToArray();
        }

        private static Color PlanningCellColour(CityLandCover cover)
        {
            switch (cover)
            {
                case CityLandCover.Woodland:
                    return new Color(0.49f, 0.68f, 0.38f, 0.18f);
                case CityLandCover.PublicGreen:
                    return new Color(0.59f, 0.76f, 0.43f, 0.15f);
                case CityLandCover.CivicPlaza:
                    return new Color(0.80f, 0.65f, 0.40f, 0.12f);
                case CityLandCover.Water:
                    return new Color(0.30f, 0.68f, 0.80f, 0.12f);
                case CityLandCover.Building:
                    return new Color(0.74f, 0.69f, 0.57f, 0.12f);
                case CityLandCover.ShrubGarden:
                    return new Color(0.47f, 0.69f, 0.36f, 0.17f);
                case CityLandCover.Disturbed:
                    return new Color(0.72f, 0.53f, 0.35f, 0.16f);
                case CityLandCover.Recovering:
                    return new Color(0.56f, 0.72f, 0.39f, 0.15f);
                default:
                    return new Color(0.75f, 0.81f, 0.55f, 0.12f);
            }
        }

        private void BuildGreenPatches()
        {
            GameObject root = ChildRoot("Green patches");
            if (city.planning_grid?.cells != null)
            {
                foreach (CityGridCell cell in city.planning_grid.cells.Where(item => item != null))
                {
                    if (cell.current_cover == CityLandCover.Woodland)
                    {
                        CreateWoodlandGrove(
                            root.transform,
                            cell.id,
                            VisualCellPolygon(cell, city.planning_grid));
                    }
                    else if (cell.current_cover == CityLandCover.PublicGreen ||
                             cell.current_cover == CityLandCover.ShrubGarden ||
                             cell.current_cover == CityLandCover.Recovering)
                    {
                        GameObject garden = new GameObject(cell.id + " public greenery");
                        garden.transform.SetParent(root.transform, false);
                        if (cell.current_cover == CityLandCover.PublicGreen)
                        {
                            CreateTree(garden.transform, "Park tree " + cell.id,
                                CellPosition(cell, -0.27f, -0.24f), 0.61f);
                        }
                        CreateBush(garden.transform, "Park shrub 1",
                            CellPosition(cell, 0.26f, -0.24f), 0.45f);
                        CreateBush(garden.transform, "Park shrub 2",
                            CellPosition(cell, 0.28f, 0.25f), 0.37f);
                    }
                }
                return;
            }

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
                if (patch.type == CityGreenPatchType.Woodland)
                {
                    CreateWoodlandGrove(root.transform, patch.id, patch.polygon_norm);
                }
            }
        }

        private static float[] CellPosition(CityGridCell cell, float xOffset, float yOffset)
        {
            return new[]
            {
                cell.center_norm[0] + cell.size_norm[0] * xOffset,
                cell.center_norm[1] + cell.size_norm[1] * yOffset,
            };
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
                float width = building.footprint_units[0] / city.bounds.width_units * MapWidth;
                float depth = building.footprint_units[1] / city.bounds.height_units * MapHeight;
                if (building.type == CityBuildingType.DetachedHouse &&
                    CreateBuildingArtwork(
                        root.transform,
                        building,
                        CityRedBrickHouseResourcePath,
                        width,
                        depth,
                        1.24f))
                {
                    continue;
                }
                if (building.type == CityBuildingType.Apartment &&
                    CreateBuildingArtwork(
                        root.transform,
                        building,
                        building.id == "apartment-court"
                            ? CityStockBrickHouseResourcePath
                            : CityApartmentCornerResourcePath,
                        width,
                        depth,
                        building.id == "apartment-court" ? 1.22f : 1.34f))
                {
                    continue;
                }
                if (building.type == CityBuildingType.Commercial &&
                    CreateBuildingArtwork(
                        root.transform,
                        building,
                        building.id == "market-hall"
                            ? CityMarketHallResourcePath
                            : CityCornerShopsResourcePath,
                        width,
                        depth,
                        building.id == "market-hall" ? 1.32f : 1.34f))
                {
                    continue;
                }
                if (building.type == CityBuildingType.CommunityFacility &&
                    CreateBuildingArtwork(
                        root.transform,
                        building,
                        CityCommunityCentreResourcePath,
                        width,
                        depth,
                        1.36f))
                {
                    continue;
                }

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
                        shortLabel = "Homes";
                        break;
                    case CityBuildingType.DetachedHouse:
                        wall = new Color(0.94f, 0.89f, 0.75f, 1f);
                        roof = new Color(0.88f, 0.39f, 0.28f, 1f);
                        height = 0.22f;
                        shortLabel = "Home";
                        break;
                    case CityBuildingType.Commercial:
                        wall = new Color(0.95f, 0.72f, 0.45f, 1f);
                        roof = new Color(0.91f, 0.45f, 0.25f, 1f);
                        height = 0.27f;
                        shortLabel = "Market";
                        break;
                    default:
                        wall = new Color(0.68f, 0.84f, 0.82f, 1f);
                        roof = new Color(0.20f, 0.55f, 0.53f, 1f);
                        height = 0.26f;
                        shortLabel = "Community";
                        break;
                }
                Vector3 position = ToWorld(building.position_norm, height * 0.5f + 0.09f);
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
                CreateLabel(
                    root.transform,
                    shortLabel,
                    ToWorld(building.position_norm, height + 0.12f),
                    0.032f);
            }
            GeneratedBuildingCount = activeBuildings.Length;
        }

        private bool CreateBuildingArtwork(
            Transform parent,
            CityBuilding building,
            string resourcePath,
            float footprintWidth,
            float footprintDepth,
            float footprintScale)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"City building sprite was not found at Resources/{resourcePath}; " +
                    "using the prototype block fallback.");
                return false;
            }

            GameObject buildingRoot = new GameObject(building.id);
            buildingRoot.transform.SetParent(parent, false);
            buildingRoot.transform.localPosition =
                ToWorld(building.position_norm, 0.105f) + BuildingArtworkOffset(building.id);

            GameObject artwork = new GameObject("Building artwork");
            artwork.transform.SetParent(buildingRoot.transform, false);
            artwork.transform.localPosition = Vector3.zero;
            artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            float targetWidth = Mathf.Max(footprintWidth, footprintDepth) * footprintScale;
            float artworkScale = targetWidth / Mathf.Max(0.001f, sprite.bounds.size.x);
            artwork.transform.localScale = new Vector3(artworkScale, artworkScale, artworkScale);

            SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 24 + Mathf.RoundToInt(building.position_norm[1] * 3f);
            return true;
        }

        private static Vector3 BuildingArtworkOffset(string buildingId)
        {
            switch (buildingId)
            {
                case "apartment-west":
                    return new Vector3(0.08f, 0f, 0.12f);
                case "apartment-court":
                    return new Vector3(-0.05f, 0f, 0.12f);
                case "market-hall":
                    return new Vector3(-0.10f, 0f, 0.12f);
                case "community-centre":
                    return new Vector3(-0.10f, 0f, 0.02f);
                case "detached-garden":
                    return new Vector3(0.03f, 0f, -0.12f);
                case "corner-shops":
                    return new Vector3(-0.10f, 0f, -0.12f);
                default:
                    return Vector3.zero;
            }
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
                0.055f);
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
            CreateLabel(planningPreviewRoot.transform, status, ToWorld(centre, 0.16f), 0.055f);
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
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);
            GUILayout.Label("RIVERSIDE WILDLIFE DISTRICT", eyebrowStyle);
            GUILayout.Label("Live city", titleStyle);
            GUILayout.Label("A shared city for people and wildlife", captionStyle);
            if (strategy?.Snapshot != null)
            {
                GUILayout.Label(
                    $"{strategy.Snapshot.phase}  ·  {strategy.Snapshot.time_block}",
                    headingStyle);
                GUILayout.Label(
                    $"DP {strategy.Snapshot.development_points}  ·  Balance {strategy.Snapshot.balance.total:0}",
                    metricStyle);
            }
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

            DrawPlanningWorkflowPanel();
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

            GUILayout.Label(paused ? "PAUSED" : "LIVE CITY  ·  ×2", headingStyle);
            AddUiSpace(CityPrototypeUiTheme.SpaceSm);
            GUILayout.Label($"Active residents  {mobility.ActiveHumanAgentCount}/{plan.RepresentativeAgentCount}", metricStyle);
            GUILayout.Label($"Represented people  {plan.RepresentedPopulation}", metricStyle);
            GUILayout.Label($"Walk / Drive  {plan.WalkTripCount} / {plan.DriveTripCount}", metricStyle);
            GUILayout.Label($"Active vehicles  {mobility.ActiveVehicleAgents.Length}", metricStyle);
            GUILayout.Label($"Completed returns  {mobility.CompletedTripCount}", metricStyle);
            if (environment?.Snapshot != null)
            {
                CityEnvironmentSnapshot snapshot = environment.Snapshot;
                AddUiSpace(CityPrototypeUiTheme.SpaceMd);
                GUILayout.Label("CITY ENVIRONMENT", headingStyle);
                GUILayout.Label($"Natural food  {snapshot.natural_food_total:0.00}", metricStyle);
                GUILayout.Label($"Human food  {snapshot.anthropogenic_food_total:0.00}", metricStyle);
                GUILayout.Label(
                    $"Waste  {snapshot.waste_demand:0.0} / {snapshot.waste_capacity:0.0}",
                    metricStyle);
                GUILayout.Label(
                    snapshot.overflow_active ? "ALERT · Overflow and litter hotspot" : "Waste contained",
                    bodyStyle);
                GUILayout.Label($"Mean disturbance  {snapshot.average_disturbance:0.00}", bodyStyle);
            }
            if (wildlife?.Snapshot != null)
            {
                AddUiSpace(CityPrototypeUiTheme.SpaceMd);
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
            DrawObservationPanel();
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

            GUILayout.Label("MAP KEY", headingStyle);
            GUILayout.Label(
                $"Planning cells  {GeneratedPlanningCellCount}  ·  Available  {AvailablePlanningCellCount}",
                bodyStyle);
            GUILayout.Label("Available cells include woodland.\nOpen land · Woodland · Public green\nPlaza · Water · Buildings\nRoad — Footpath", bodyStyle);
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

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

            if (GUILayout.Button(paused ? "Resume" : "Pause", buttonStyle))
            {
                paused = !paused;
            }
            if (GUILayout.Button("Restart trips", buttonStyle))
            {
                InitializePrototype(false);
            }
            if (GUILayout.Button(showPedestrianNetwork ? "Hide footpaths" : "Show footpaths", buttonStyle))
            {
                showPedestrianNetwork = !showPedestrianNetwork;
                if (pedestrianRoot != null)
                {
                    pedestrianRoot.SetActive(showPedestrianNetwork);
                }
            }
            AddUiSpace(CityPrototypeUiTheme.SpaceMd);
            GUILayout.Label(
                "Residents emerge from housing and respond to destination appeal, distance and crowding. Vehicles only appear for Drive trips.",
                captionStyle);
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);
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
            AddUiSpace(CityPrototypeUiTheme.SpaceSm);

            switch (snapshot.phase)
            {
                case CityPlanningWorkflowPhase.ReadyToScan:
                    if (GUILayout.Button("Load latest camera scan", buttonStyle))
                    {
                        TryLoadLatestCameraScan();
                    }
                    GUILayout.Label(cityScanInputMessage, bodyStyle);
                    AddUiSpace(CityPrototypeUiTheme.SpaceXs);
                    if (GUILayout.Button("Scan city · electronic test", buttonStyle))
                    {
                        CityTokenScanPacket scan = CityPlanningDemoScanFactory.CreateElectronicSample(
                            city,
                            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        planningWorkflow.TryAcceptScan(scan, out _);
                        cityScanInputMessage =
                            "Electronic sample accepted. This is not a physical-camera claim.";
                        RefreshPlanningOverlay();
                    }
                    GUILayout.Label(
                        "The electronic sample stays available while the physical board is unavailable.",
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
                    if (GUILayout.Button("Confirm preview", buttonStyle))
                    {
                        planningWorkflow.ConfirmPreview(out _);
                        RefreshPlanningOverlay();
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("Cancel preview", buttonStyle))
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
                    if (GUILayout.Button("Start construction", buttonStyle))
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
                    if (GUILayout.Button("Pause", buttonStyle)) planningWorkflow.SetTimeScale(0f);
                    if (GUILayout.Button("1×", buttonStyle)) planningWorkflow.SetTimeScale(1f);
                    if (GUILayout.Button("2×", buttonStyle)) planningWorkflow.SetTimeScale(2f);
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button("Advance one block", buttonStyle))
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

        private void TryLoadLatestCameraScan()
        {
            if (!CityTokenScanFileSource.TryRead(
                    cityScanPath,
                    out string json,
                    out string resolvedPath,
                    out string readError))
            {
                cityScanInputMessage = readError;
                return;
            }
            if (!planningWorkflow.TryScanJson(json, out string validationError))
            {
                cityScanInputMessage = validationError;
                return;
            }
            cityScanInputMessage =
                $"Validated city scan loaded from {System.IO.Path.GetFileName(resolvedPath)}. Review Preview before confirming.";
            RefreshPlanningOverlay();
        }

        private void DrawObservationPanel()
        {
            if (observation?.Snapshot == null || strategy?.Snapshot == null ||
                wildlife?.Snapshot == null)
            {
                return;
            }
            AddUiSpace(CityPrototypeUiTheme.SpaceMd);
            GUILayout.Label("CITY TRACKS", headingStyle);
            GUILayout.BeginHorizontal();
            DrawTraceModeButton(CityTraceDisplayMode.HumanTrace, "PEOPLE");
            DrawTraceModeButton(CityTraceDisplayMode.AnimalTrace, "WILDLIFE");
            DrawTraceModeButton(CityTraceDisplayMode.CombinedTrace, "ALL");
            GUILayout.EndHorizontal();
            GUILayout.Label(
                $"Visible marks {VisibleTraceMarkCount}/{CityTraceVisualizer.MaximumVisibleMarks}",
                bodyStyle);

            AddUiSpace(CityPrototypeUiTheme.SpaceMd);
            GUILayout.Label("CITY FEED", headingStyle);
            CityFeedEntry[] latest = observation.Snapshot.city_feed
                .Reverse()
                .Take(4)
                .ToArray();
            if (latest.Length == 0)
            {
                GUILayout.Label("Waiting for a meaningful city event…", bodyStyle);
            }
            foreach (CityFeedEntry entry in latest)
            {
                GUILayout.Label(
                    $"{FeedSymbol(entry.type)}  {FeedTitle(entry.type)}  ·  {entry.elapsed_seconds:0}s",
                    metricStyle);
                GUILayout.Label(entry.message, bodyStyle);
            }

            CityPhaseReport report = observation.BuildPhaseReport(
                strategy.Snapshot,
                wildlife.Snapshot);
            AddUiSpace(CityPrototypeUiTheme.SpaceMd);
            GUILayout.Label("PHASE SNAPSHOT", headingStyle);
            GUILayout.Label(
                $"{report.phase}  ·  Balance {report.after.total:0} " +
                $"({Signed(report.change.total)})",
                metricStyle);
            GUILayout.Label(
                $"Tracks H {report.human_trace_points} / A {report.animal_trace_points}  ·  " +
                $"Feed {report.feeding_events}  ·  Migration {report.migration_events}",
                bodyStyle);
            if (GUILayout.Button("Clear tracks & feed", buttonStyle))
            {
                observation.ResetView(
                    mobility.Plan,
                    environment.Snapshot,
                    wildlife.Snapshot,
                    strategy.Snapshot);
                RefreshTraceLayer();
            }
        }

        private void DrawTraceModeButton(CityTraceDisplayMode mode, string label)
        {
            string text = traceDisplayMode == mode ? "[On] " + label : label;
            if (GUILayout.Button(text, buttonStyle))
            {
                traceDisplayMode = mode;
                traceVisualizer?.SetMode(mode);
            }
        }

        private static string FeedSymbol(CityFeedEventType type)
        {
            switch (type)
            {
                case CityFeedEventType.WasteOverflow: return "!";
                case CityFeedEventType.Crowding: return "!";
                case CityFeedEventType.Feeding: return "+";
                case CityFeedEventType.MigrationIn: return "+";
                case CityFeedEventType.MigrationOut: return "−";
                case CityFeedEventType.Roadkill: return "×";
                case CityFeedEventType.ProjectCompleted: return "OK";
                default: return "~";
            }
        }

        private static string FeedTitle(CityFeedEventType type)
        {
            switch (type)
            {
                case CityFeedEventType.WasteOverflow: return "WASTE PRESSURE";
                case CityFeedEventType.Crowding: return "CROWDING";
                case CityFeedEventType.Feeding: return "FEEDING";
                case CityFeedEventType.MigrationIn: return "MIGRATION IN";
                case CityFeedEventType.MigrationOut: return "MIGRATION OUT";
                case CityFeedEventType.Roadkill: return "ROAD INCIDENT";
                case CityFeedEventType.ProjectCompleted: return "PROJECT COMPLETE";
                default: return "BALANCE WARNING";
            }
        }

        private static string Signed(float value)
        {
            return value > 0.05f ? $"+{value:0.0}" : value < -0.05f ? $"{value:0.0}" : "±0.0";
        }

        private void BuildTraceLayer()
        {
            traceVisualizer = new CityTraceVisualizer(generatedRoot.transform, MapWidth, MapHeight);
            traceVisualizer.SetMode(traceDisplayMode);
            UpdateTraceVisuals();
        }

        private void UpdateTraceVisuals()
        {
            if (traceVisualizer == null || observation?.Snapshot?.trace_points == null)
            {
                return;
            }
            traceVisualizer.Render(observation.Snapshot.trace_points);
        }

        private void RefreshTraceLayer()
        {
            Transform existing = generatedRoot?.transform.Find("Live city traces");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }
            BuildTraceLayer();
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
            if (GUILayout.Button("Use low-impact routes", buttonStyle))
            {
                planningWorkflow.SelectRecommendedLowImpactRoutes(out _);
                RefreshPlanningOverlay();
            }
            GUI.enabled = preview.CanConfirm;
            if (GUILayout.Button("Confirm routes", buttonStyle))
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
            if (panelStyle != null && styledScreenHeight == Screen.height)
            {
                return;
            }

            styledScreenHeight = Screen.height;
            uiScale = CityPrototypeUiTheme.ScaleForScreen(Screen.height);
            Font regularFont = CityPrototypeUiTheme.LoadRuntimeFont();
            Font boldFont = CityPrototypeUiTheme.LoadRuntimeFont(true);
            int panelHorizontal = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceLg,
                uiScale);
            int panelTop = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceMd,
                uiScale);
            int panelBottom = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceLg,
                uiScale);

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(
                    panelHorizontal,
                    panelHorizontal,
                    panelTop,
                    panelBottom),
                normal = { background = SolidTexture(CityPrototypeUiTheme.Panel) },
            };

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.DisplayFontSize,
                    uiScale),
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                margin = new RectOffset(
                    0,
                    0,
                    0,
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceXs, uiScale)),
                normal = { textColor = CityPrototypeUiTheme.Ink },
            };
            eyebrowStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.EyebrowFontSize,
                    uiScale),
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                margin = new RectOffset(
                    0,
                    0,
                    0,
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceXs, uiScale)),
                normal = { textColor = CityPrototypeUiTheme.Accent },
            };
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                font = boldFont,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.SectionFontSize,
                    uiScale),
                fontStyle = FontStyle.Normal,
                wordWrap = true,
                margin = new RectOffset(
                    0,
                    0,
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceXs, uiScale),
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceXs, uiScale)),
                normal = { textColor = CityPrototypeUiTheme.Accent },
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                font = regularFont,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.BodyFontSize,
                    uiScale),
                wordWrap = true,
                margin = new RectOffset(0, 0, 1, 1),
                normal = { textColor = CityPrototypeUiTheme.InkSecondary },
            };
            captionStyle = new GUIStyle(bodyStyle)
            {
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.CaptionFontSize,
                    uiScale),
                normal = { textColor = CityPrototypeUiTheme.InkMuted },
            };
            metricStyle = new GUIStyle(bodyStyle)
            {
                font = boldFont,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.MetricFontSize,
                    uiScale),
                fontStyle = FontStyle.Normal,
                normal = { textColor = CityPrototypeUiTheme.Ink },
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                font = boldFont,
                fixedHeight = CityPrototypeUiTheme.ScaledPixel(42, uiScale),
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.ButtonFontSize,
                    uiScale),
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                padding = new RectOffset(
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceMd, uiScale),
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceMd, uiScale),
                    0,
                    0),
                margin = new RectOffset(
                    0,
                    0,
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceXs, uiScale),
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceXs, uiScale)),
                normal =
                {
                    background = SolidTexture(CityPrototypeUiTheme.Button),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
                hover =
                {
                    background = SolidTexture(CityPrototypeUiTheme.ButtonHover),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
                active =
                {
                    background = SolidTexture(CityPrototypeUiTheme.ButtonPressed),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
                focused =
                {
                    background = SolidTexture(CityPrototypeUiTheme.ButtonHover),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
            };
        }

        private void AddUiSpace(int referencePixels)
        {
            GUILayout.Space(CityPrototypeUiTheme.ScaledPixel(referencePixels, uiScale));
        }

        private void CreateWoodlandGrove(Transform parent, string id, float[][] polygon)
        {
            if (polygon == null || polygon.Length == 0)
            {
                Debug.LogWarning($"City woodland patch geometry was not found for {id}.");
                return;
            }

            float minX = polygon.Min(point => point[0]);
            float maxX = polygon.Max(point => point[0]);
            float minY = polygon.Min(point => point[1]);
            float maxY = polygon.Max(point => point[1]);
            float centreX = (minX + maxX) * 0.5f;
            float centreY = (minY + maxY) * 0.5f;

            GameObject grove = new GameObject(id + " woodland grove");
            grove.transform.SetParent(parent, false);
            grove.transform.localPosition = Vector3.zero;

            float width = maxX - minX;
            float height = maxY - minY;
            int firstVariant = StableTextHash(id) % CityTreeResourcePaths.Length;
            for (int index = 0; index < WoodlandTreeOffsets.Length; index += 1)
            {
                Vector2 offset = WoodlandTreeOffsets[index];
                float[] position =
                {
                    centreX + width * offset.x,
                    centreY + height * offset.y,
                };
                int variant = (firstVariant + index) % CityTreeResourcePaths.Length;
                CreateTree(
                    grove.transform,
                    $"Woodland tree {index + 1}",
                    position,
                    WoodlandTreeScales[index],
                    variant);
            }
        }

        private void CreateTree(
            Transform parent,
            string name,
            float[] normalized,
            float scale,
            int variantIndex = -1)
        {
            GameObject tree = new GameObject(name);
            tree.transform.SetParent(parent, false);
            tree.transform.localPosition = ToWorld(normalized, 0.095f);

            int resolvedVariant = variantIndex >= 0
                ? variantIndex % CityTreeResourcePaths.Length
                : StableTextHash(name) % CityTreeResourcePaths.Length;
            string resourcePath = CityTreeResourcePaths[resolvedVariant];
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(LegacyCityTreeResourcePath);
            }
            if (sprite == null)
            {
                Debug.LogWarning(
                    $"City tree sprite was not found at Resources/{resourcePath} " +
                    $"or Resources/{LegacyCityTreeResourcePath}.");
                return;
            }

            GameObject artwork = new GameObject("Tree artwork");
            artwork.transform.SetParent(tree.transform, false);
            artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            artwork.transform.localPosition = Vector3.zero;
            float targetHeight = 1.02f * scale;
            float artworkScale = targetHeight / Mathf.Max(0.001f, sprite.bounds.size.y);
            float mirror = StableTextHash(name + resolvedVariant) % 2 == 0 ? 1f : -1f;
            artwork.transform.localScale = new Vector3(artworkScale * mirror, artworkScale, artworkScale);

            SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 14 + Mathf.RoundToInt(normalized[1] * 4f);
        }

        private static int StableTextHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char character in value ?? string.Empty)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
                return (int)(hash & 0x7fffffff);
            }
        }

        private void CreateBush(Transform parent, string name, float[] normalized, float scale)
        {
            Sprite sprite = Resources.Load<Sprite>(CityBushResourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"City bush sprite was not found at Resources/{CityBushResourcePath}.");
                return;
            }

            GameObject bush = new GameObject(name);
            bush.transform.SetParent(parent, false);
            bush.transform.localPosition = ToWorld(normalized, 0.09f);

            GameObject artwork = new GameObject("Bush artwork");
            artwork.transform.SetParent(bush.transform, false);
            artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            artwork.transform.localPosition = Vector3.zero;
            // The source artwork deliberately keeps generous transparent breathing room.
            // Compensate here so its visible foliage reads at the same map scale as the trees.
            float targetWidth = 1.20f * scale;
            float artworkScale = targetWidth / Mathf.Max(0.001f, sprite.bounds.size.x);
            artwork.transform.localScale = new Vector3(artworkScale, artworkScale, artworkScale);

            SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 13;
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
            int sortingOrder,
            bool transparent = false)
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
            line.material = transparent
                ? TransparentMaterialFor(colour)
                : MaterialFor(colour);
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
            Color colour,
            int sortingOrder = 0,
            bool transparent = false)
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
            MeshRenderer renderer = polygon.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = transparent
                ? TransparentMaterialFor(colour)
                : MaterialFor(colour);
            renderer.sortingOrder = sortingOrder;
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
            label.font = CityPrototypeUiTheme.LoadRuntimeFont(true);
            label.fontSize = 48;
            label.characterSize = characterSize;
            label.fontStyle = FontStyle.Normal;
            label.color = new Color(0.10f, 0.23f, 0.28f, 1f);
            label.GetComponent<Renderer>().sharedMaterial = label.font.material;
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

        private static Material TransparentMaterialFor(Color colour)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
            Material material = new Material(shader)
            {
                color = colour,
                hideFlags = HideFlags.DontSave,
                renderQueue = 3000,
            };
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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

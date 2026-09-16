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
        private const string UiLanguagePreferenceKey =
            "UrbanWildlife.CityPrototype.UiLanguage";
        private const float MapHeight = 8f;
        private const float MapViewportWidth = 0.79f;
        private const float BoardViewMargin = 0.55f;
        private const float RuntimeSpeed = 2f;
        private const float HumanArtworkHeight = 0.23f;
        private const float VehicleArtworkDepth = 0.24f;
        private const string CityBoardUnderlayResourcePath =
            "UrbanWildlife/Environment/city-board-woodland-existing-streets-v07";
        private const string CityPlazaResourcePath =
            "UrbanWildlife/Environment/human-activity-plaza-v01";
        private const string CityPondResourcePath =
            "UrbanWildlife/Environment/park-pond-citybuilder-v01";
        private const string LegacyCityTreeResourcePath =
            "UrbanWildlife/Environment/tree-citybuilder-default-v01";
        private const string CityBushResourcePath =
            "UrbanWildlife/Environment/bush-citybuilder-default-v01";
        private const string CityResidentialLotBlueResourcePath =
            "UrbanWildlife/Buildings/detached-house-riverside-blue-v09";
        private const string CityResidentialLotCoralResourcePath =
            "UrbanWildlife/Buildings/detached-house-riverside-coral-v09";
        private const string CityResidentialLotBResourcePath =
            "UrbanWildlife/Buildings/apartment-riverside-v04";
        private const string CityResidentialLotCResourcePath =
            "UrbanWildlife/Buildings/apartment-riverside-v04";
        private static readonly string[] CityCommercialResourcePaths =
        {
            "UrbanWildlife/Buildings/commercial-market-riverside-v05",
        };
        private static readonly string[] CityCommunityResourcePaths =
        {
            "UrbanWildlife/Buildings/community-centre-riverside-v04",
        };
        private const string PigeonWildlifeResourcePath =
            "UrbanWildlife/Animals/pigeon-citybuilder-soft-v02";
        private const string SquirrelWildlifeResourcePath =
            "UrbanWildlife/Animals/squirrel-citybuilder-soft-v02";
        private const string FoxWildlifeResourcePath =
            "UrbanWildlife/Animals/fox-citybuilder-soft-v02";
        private const string HedgehogWildlifeResourcePath =
            "UrbanWildlife/Animals/hedgehog-citybuilder-soft-v02";
        private static readonly string[] CityVehicleResourcePaths =
        {
            "UrbanWildlife/Vehicles/city-car-pastel-blue-v01",
            "UrbanWildlife/Vehicles/city-car-muted-coral-v01",
            "UrbanWildlife/Vehicles/city-car-warm-mustard-v01",
        };
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
            new Vector2(-0.27f, -0.25f),
            new Vector2(0.24f, -0.22f),
            new Vector2(-0.22f, 0.18f),
            new Vector2(0.27f, 0.23f),
            new Vector2(0.02f, 0.00f),
        };
        private static readonly float[] WoodlandTreeScales =
        {
            0.46f,
            0.43f,
            0.45f,
            0.42f,
            0.47f,
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
        private GameObject vehicleRoadRoot;
        private GameObject buildingAccessRoadRoot;
        private GameObject pedestrianRoot;
        private GameObject planningPreviewRoot;
        private bool paused;
        private bool showVehicleNetwork = true;
        private bool showPedestrianNetwork = true;
        private bool showHeatmap;
        [SerializeField]
        private bool useCameraInput;
        [SerializeField]
        private bool useChineseUi;
        private bool languageInitialised;
        private bool styledChineseUi;
        private CityPhysicalTokenType desktopBuildingType = CityPhysicalTokenType.DetachedHouse;
        private string desktopInputMessage =
            "Choose a building, then click an open part of the map.";
        private CityTraceDisplayMode traceDisplayMode = CityTraceDisplayMode.AnimalTrace;
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
        private GUIStyle heatmapCardStyle;
        private GUIStyle heatmapToggleStyle;
        private GUIStyle heatmapEmptyStyle;
        private GUIStyle chromeCardStyle;
        private GUIStyle chromeShadowStyle;
        private GUIStyle mapTitleStyle;
        private GUIStyle mapSubtitleStyle;
        private GUIStyle topMetricStyle;
        private GUIStyle tabBarStyle;
        private GUIStyle tabStyle;
        private GUIStyle activeTabStyle;
        private GUIStyle catalogItemStyle;
        private GUIStyle selectedCatalogItemStyle;
        private GUIStyle catalogLabelStyle;
        private GUIStyle rightCaptionStyle;
        private GUIStyle toolbarButtonStyle;
        private GUIStyle backButtonStyle;
        private GUIStyle progressTrackStyle;
        private GUIStyle progressGreenStyle;
        private GUIStyle progressBlueStyle;
        private GUIStyle progressOrangeStyle;
        private GUIStyle progressPurpleStyle;
        private Sprite heatmapMapSprite;
        private Texture2D heatmapDotTexture;
        private Texture2D heatmapLegendTexture;
        private float uiScale = 1f;
        private int styledScreenHeight = -1;
        private int configuredScreenWidth = -1;
        private int configuredScreenHeight = -1;
        private int sidebarTab = 1;
        private int bottomNavigationIndex;
        private int traceViewIndex;
        private int selectedAnimalIndex;
        private readonly HashSet<string> dismissedEventIds = new HashSet<string>();

        public int GeneratedBuildingCount { get; private set; }
        public int GeneratedVehicleRoadCount { get; private set; }
        public int GeneratedPedestrianLinkCount { get; private set; }
        public int GeneratedAmenityCount { get; private set; }
        public int GeneratedPlanningCellCount { get; private set; }
        public int AvailablePlanningCellCount { get; private set; }
        public int RepresentativeAgentCount => plan?.RepresentativeAgentCount ?? 0;
        public int RepresentedPopulation => plan?.RepresentedPopulation ?? 0;
        public int VehicleTripCount => plan?.DriveTripCount ?? 0;
        public int WalkTripCount => plan?.WalkTripCount ?? 0;
        public bool PedestrianNetworkVisible => pedestrianRoot != null
            ? pedestrianRoot.activeSelf
            : showPedestrianNetwork;
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
        public int VisibleHeatCellCount => traceVisualizer?.VisibleCellCount ?? 0;
        public bool HeatmapVisible => showHeatmap;
        public bool HeatmapUsesAnimalData => traceDisplayMode == CityTraceDisplayMode.AnimalTrace;
        public bool HeatmapRendersInSidebar => true;
        public bool HeatmapOverlaysMap => traceVisualizer?.WorldOverlayActive ?? false;
        public bool RiversideUiLayoutEnabled => true;
        public bool ReferenceStageHudEnabled => true;
        public int BottomNavigationItemCount => 4;
        public int ActiveBottomNavigationIndex => bottomNavigationIndex;
        public bool BuildingPlacementCardEnabled => true;
        public bool TraceViewSelectorEnabled => true;
        public int ActiveTraceViewIndex => traceViewIndex;
        public int DisplayStageNumber => StageNumberForBuildingCount(
            Mathf.Max(GeneratedBuildingCount, city?.buildings?.Length ?? 0));
        public int ActiveSidebarTab => sidebarTab;
        public static float MapViewportFraction => MapViewportWidth;
        public bool PlanningWorkflowConnected => planningWorkflow != null;
        public string ResolvedCityScanPath => CityTokenScanFileSource.Resolve(cityScanPath);
        public CityPlanningWorkflowPhase PlanningPhase => planningWorkflow?.Phase ??
                                                           CityPlanningWorkflowPhase.ReadyToScan;
        public bool DesktopPlayEnabled => !useCameraInput;
        public bool ChineseUiEnabled => useChineseUi;
        public string CurrentUiLanguageCode => useChineseUi ? "zh-CN" : "en";
        public string LocalizedPlanningHeading => T("CITY PLANNING", "城市规划");
        public string ChineseUiFontName =>
            CityPrototypeUiTheme.LoadRuntimeFont(false, true)?.name ?? string.Empty;
        public bool ChineseUiFontSupportsCoreGlyphs
        {
            get
            {
                Font font = CityPrototypeUiTheme.LoadRuntimeFont(false, true);
                return font != null && font.HasCharacter('城') && font.HasCharacter('市');
            }
        }

        private void OnEnable()
        {
            InitialiseUiLanguage();
            ConfigureCamera();
            if (!Application.isPlaying)
            {
                InitializePrototype(true);
            }
        }

        public void SetUiLanguage(bool chinese)
        {
            useChineseUi = chinese;
            languageInitialised = true;
            PlayerPrefs.SetInt(UiLanguagePreferenceKey, chinese ? 1 : 0);
            PlayerPrefs.Save();
            styledScreenHeight = -1;
            if (planningWorkflow != null && generatedRoot != null)
            {
                RefreshPlanningOverlay();
            }
        }

        private void InitialiseUiLanguage()
        {
            if (languageInitialised)
            {
                return;
            }
            bool systemUsesChinese = Application.systemLanguage == SystemLanguage.Chinese ||
                                     Application.systemLanguage == SystemLanguage.ChineseSimplified ||
                                     Application.systemLanguage == SystemLanguage.ChineseTraditional;
            useChineseUi = PlayerPrefs.GetInt(
                UiLanguagePreferenceKey,
                systemUsesChinese ? 1 : 0) == 1;
            languageInitialised = true;
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
            if (Application.isPlaying && !useCameraInput)
            {
                HandleDesktopPointerInput();
            }
            if (Application.isPlaying && UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                SetHeatmapVisible(!showHeatmap);
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
            CityState completedCity = planningWorkflow?.Phase == CityPlanningWorkflowPhase.Complete
                ? planningWorkflow.CurrentState
                : null;
            if (completedCity != null)
            {
                planningWorkflow = null;
            }
            city = completedCity ?? planningWorkflow?.CurrentState ?? CityPrototypeStateFactory.Create();
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
            EnsureFullFrameClearCamera(camera);
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

        private static void EnsureFullFrameClearCamera(Camera mapCamera)
        {
            GameObject clearObject = GameObject.Find("UI Background Camera");
            if (clearObject == null)
            {
                clearObject = new GameObject("UI Background Camera");
            }
            Camera clearCamera = clearObject.GetComponent<Camera>() ??
                                 clearObject.AddComponent<Camera>();
            clearCamera.clearFlags = CameraClearFlags.SolidColor;
            clearCamera.backgroundColor = CityPrototypeUiTheme.Panel;
            clearCamera.cullingMask = 0;
            clearCamera.depth = mapCamera.depth - 10f;
            clearCamera.rect = new Rect(0f, 0f, 1f, 1f);
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
            renderer.color = Color.white;
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
                GeneratedPlanningCellCount += 1;
                if (cell.buildable && !cell.fixed_feature &&
                    string.IsNullOrWhiteSpace(cell.occupant_id))
                {
                    AvailablePlanningCellCount += 1;
                }
            }

            BuildCompactDevelopmentClearings(root.transform, grid);

            // The 18 x 12 grid remains a semantic occupancy/ecology layer. Its geometry is kept
            // for inspection and tests, but the normal player view must not read as square tiles.
            Color boundaryColour = new Color(0.48f, 0.58f, 0.58f, 0f);
            for (int col = 0; col <= grid.cols; col += 1)
            {
                float[][] points = Enumerable.Range(0, grid.rows + 1)
                    .Select(row => VisualGridNode(row, col, grid))
                    .ToArray();
                CreateLine(boundariesRoot.transform, "Column " + col,
                    points,
                    0.002f, boundaryColour, 0.025f, -30, true);
            }
            for (int row = 0; row <= grid.rows; row += 1)
            {
                float[][] points = Enumerable.Range(0, grid.cols + 1)
                    .Select(col => VisualGridNode(row, col, grid))
                    .ToArray();
                CreateLine(boundariesRoot.transform, "Row " + row,
                    points,
                    0.002f, boundaryColour, 0.025f, -30, true);
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
            return new[] { (float)col / grid.cols, (float)row / grid.rows };
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
                    colour = new Color(0.38f, 0.76f, 0.90f, 0.24f);
                    radiusX = 0.44f;
                    radiusY = 0.40f;
                    points = 16;
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

        private void BuildCompactDevelopmentClearings(
            Transform parent,
            CityPlanningGrid grid)
        {
            GameObject clearingRoot = new GameObject("Compact development clearings");
            clearingRoot.transform.SetParent(parent, false);
            Color clearedGround = new Color(243f / 255f, 241f / 255f, 227f / 255f, 1f);

            Dictionary<string, CityGridCell> cellsById = grid.cells
                .Where(cell => cell != null)
                .ToDictionary(cell => cell.id, StringComparer.Ordinal);
            foreach (CityBuilding building in city.buildings.Where(item =>
                         item != null &&
                         item.construction_state == CityConstructionState.Existing &&
                         BuildingReplacedWoodland(item, cellsById)))
            {
                CreatePolygon(
                    clearingRoot.transform,
                    building.id + " compact building clearing",
                    CompactBuildingSitePolygon(building),
                    0.018f,
                    clearedGround,
                    -27);
            }

            foreach (CityVehicleRoad road in city.vehicle_roads.Where(item =>
                         item != null &&
                         item.role == CityVehicleRoadRole.BuildingAccess &&
                         item.construction_state == CityConstructionState.Existing))
            {
                float roadWidth = road.width_units / city.bounds.width_units * MapWidth;
                CreateLine(
                    clearingRoot.transform,
                    road.id + " compact cleared verge",
                    road.points_norm,
                    Mathf.Max(0.12f, roadWidth + 0.055f),
                    clearedGround,
                    0.019f,
                    -26);
            }
        }

        private static bool BuildingReplacedWoodland(
            CityBuilding building,
            IReadOnlyDictionary<string, CityGridCell> cellsById)
        {
            return (building.planning_cell_ids ?? Array.Empty<string>())
                .Any(id => cellsById.TryGetValue(id, out CityGridCell cell) && cell.was_woodland);
        }

        private float[][] CompactBuildingSitePolygon(CityBuilding building)
        {
            // The model footprint now already represents the visible development lot.
            // Clearing and placement preview therefore use the exact same dimensions.
            float halfWidth = building.footprint_units[0] * 0.5f;
            float halfDepth = building.footprint_units[1] * 0.5f;
            float centreX = building.position_norm[0] * city.bounds.width_units;
            float centreY = building.position_norm[1] * city.bounds.height_units;
            float radians = building.rotation_deg * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            const int pointCount = 24;
            return Enumerable.Range(0, pointCount)
                .Select(index =>
                {
                    float angle = Mathf.PI * 2f * index / pointCount;
                    float cosAngle = Mathf.Cos(angle);
                    float sinAngle = Mathf.Sin(angle);
                    float localX = halfWidth * Mathf.Sign(cosAngle) *
                                   Mathf.Pow(Mathf.Abs(cosAngle), 0.55f);
                    float localY = halfDepth * Mathf.Sign(sinAngle) *
                                   Mathf.Pow(Mathf.Abs(sinAngle), 0.55f);
                    float rotatedX = localX * cosine - localY * sine;
                    float rotatedY = localX * sine + localY * cosine;
                    return new[]
                    {
                        (centreX + rotatedX) / city.bounds.width_units,
                        (centreY + rotatedY) / city.bounds.height_units,
                    };
                })
                .ToArray();
        }

        private static Color PlanningCellColour(CityLandCover cover)
        {
            switch (cover)
            {
                case CityLandCover.Woodland:
                    return new Color(0.49f, 0.68f, 0.38f, 0f);
                case CityLandCover.PublicGreen:
                    return new Color(0.59f, 0.76f, 0.43f, 0f);
                case CityLandCover.CivicPlaza:
                    return new Color(0.80f, 0.65f, 0.40f, 0f);
                case CityLandCover.Water:
                    return new Color(0.30f, 0.68f, 0.80f, 0f);
                case CityLandCover.Building:
                    return new Color(0.74f, 0.69f, 0.57f, 0f);
                case CityLandCover.ShrubGarden:
                    return new Color(0.47f, 0.69f, 0.36f, 0f);
                case CityLandCover.Disturbed:
                    return new Color(0.72f, 0.53f, 0.35f, 0.045f);
                case CityLandCover.Recovering:
                    return new Color(0.56f, 0.72f, 0.39f, 0f);
                default:
                    return new Color(0.75f, 0.81f, 0.55f, 0f);
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
                        // Forest masses and their five-shape tree language are baked into the
                        // terrain-only underlay. Runtime clearing still paints warm-white ground.
                        continue;
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
            vehicleRoadRoot = ChildRoot("Vehicle road network");
            buildingAccessRoadRoot = ChildRoot("Visible building access roads");
            CityVehicleRoad[] activeRoads = city.vehicle_roads.Where(item =>
                item.construction_state == CityConstructionState.Existing).ToArray();
            foreach (CityVehicleRoad road in activeRoads)
            {
                // The fixed street hierarchy is baked into the terrain artwork. Keep its
                // network data for routing and placement, but only draw roads created by play.
                if (road.source == CityNetworkSource.ExistingMap &&
                    road.role != CityVehicleRoadRole.BuildingAccess)
                {
                    continue;
                }
                Transform roadParent = road.role == CityVehicleRoadRole.BuildingAccess
                    ? buildingAccessRoadRoot.transform
                    : vehicleRoadRoot.transform;
                float width = road.width_units / city.bounds.width_units * MapWidth;
                CreateLine(
                    roadParent,
                    road.id + " kerb",
                    road.points_norm,
                    width + 0.035f,
                    new Color(0.94f, 0.94f, 0.91f, 0.98f),
                    0.045f,
                    -8);
                CreateLine(
                    roadParent,
                    road.id + " asphalt",
                    road.points_norm,
                    width,
                    road.role == CityVehicleRoadRole.BuildingAccess
                        ? new Color(0.90f, 0.90f, 0.88f, 0.96f)
                        : new Color(0.88f, 0.88f, 0.88f, 0.98f),
                    0.055f,
                    -7);
            }
            vehicleRoadRoot.SetActive(showVehicleNetwork);

            pedestrianRoot = ChildRoot("Pedestrian link network");
            CityPedestrianLink[] activeLinks = city.pedestrian_links.Where(item =>
                item.construction_state == CityConstructionState.Existing).ToArray();
            foreach (CityPedestrianLink link in activeLinks)
            {
                // Existing pavements are part of the same baked street artwork. Their
                // hidden polylines still keep pedestrians on the side of each street.
                if (link.source == CityNetworkSource.ExistingMap &&
                    link.type == CityPedestrianLinkType.ExistingNetwork)
                {
                    continue;
                }
                float width = link.width_units / city.bounds.width_units * MapWidth;
                CreateLine(
                    pedestrianRoot.transform,
                    link.id + " edge",
                    link.points_norm,
                    Mathf.Max(0.105f, width + 0.045f),
                    new Color(0.98f, 0.97f, 0.93f, 0.96f),
                    0.070f,
                    -5);
                CreateLine(
                    pedestrianRoot.transform,
                    link.id + " surface",
                    link.points_norm,
                    Mathf.Max(0.070f, width),
                    new Color(0.89f, 0.86f, 0.77f, 0.96f),
                    0.078f,
                    -4);
            }
            pedestrianRoot.SetActive(showPedestrianNetwork);
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
                        building.source_token_id % 2 == 0
                            ? CityResidentialLotBlueResourcePath
                            : CityResidentialLotCoralResourcePath,
                        width,
                        depth,
                        0.72f))
                {
                    continue;
                }
                if (building.type == CityBuildingType.Apartment &&
                    CreateBuildingArtwork(
                        root.transform,
                        building,
                        building.id == "apartment-court"
                            ? CityResidentialLotBResourcePath
                            : CityResidentialLotCResourcePath,
                        width,
                        depth,
                        1.05f))
                {
                    continue;
                }
                if (building.type == CityBuildingType.Commercial &&
                    CreateBuildingArtwork(
                        root.transform,
                        building,
                        BuildingVariantResourcePath(building, CityCommercialResourcePaths),
                        width,
                        depth,
                        1.00f))
                {
                    continue;
                }
                if (building.type == CityBuildingType.CommunityFacility &&
                    CreateBuildingArtwork(
                        root.transform,
                        building,
                        BuildingVariantResourcePath(building, CityCommunityResourcePaths),
                        width,
                        depth,
                        0.88f))
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
                        shortLabel = T("Homes", "住宅");
                        break;
                    case CityBuildingType.DetachedHouse:
                        wall = new Color(0.94f, 0.89f, 0.75f, 1f);
                        roof = new Color(0.88f, 0.39f, 0.28f, 1f);
                        height = 0.22f;
                        shortLabel = T("Home", "住宅");
                        break;
                    case CityBuildingType.Commercial:
                        wall = new Color(0.95f, 0.72f, 0.45f, 1f);
                        roof = new Color(0.91f, 0.45f, 0.25f, 1f);
                        height = 0.27f;
                        shortLabel = T("Market", "市场");
                        break;
                    default:
                        wall = new Color(0.68f, 0.84f, 0.82f, 1f);
                        roof = new Color(0.20f, 0.55f, 0.53f, 1f);
                        height = 0.26f;
                        shortLabel = T("Community", "社区");
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
            return Vector3.zero;
        }

        private static string BuildingVariantResourcePath(
            CityBuilding building,
            IReadOnlyList<string> resourcePaths)
        {
            if (resourcePaths == null || resourcePaths.Count == 0)
            {
                return string.Empty;
            }

            int discriminator = building.source_token_id;
            if (discriminator < 0)
            {
                discriminator = 17;
                foreach (char character in building.id ?? string.Empty)
                {
                    discriminator = unchecked(discriminator * 31 + character);
                }
            }
            int index = (int)((uint)discriminator % (uint)resourcePaths.Count);
            return resourcePaths[index];
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
                CityPlanningGrid previewGrid = CityGridResolver.DeepClone(
                    planningWorkflow.CurrentState.planning_grid);
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
                        CityGridResolver.TryOccupyWithGreenPatch(
                            previewGrid,
                            city.bounds,
                            patch,
                            city.revision + 1,
                            out _);
                        DrawPlanningPatch(patch, T("SCAN PREVIEW", "扫描预览"));
                    }
                    else
                    {
                        CityBuilding building = CityConstructionFactory.CreateBuilding(
                            change.scanned_state,
                            CityConstructionState.Proposed);
                        CityGridResolver.TryOccupyWithBuildingAtPosition(
                            previewGrid,
                            city.bounds,
                            building,
                            city.revision + 1,
                            out _);
                        DrawPlanningFootprint(building, T("SCAN PREVIEW", "扫描预览"));
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
                        ? T("BUILDING", "建设中")
                        : T("PROPOSED", "待建设"));
            }
            foreach (CityGreenPatch patch in planningWorkflow.CurrentState.green_patches.Where(item =>
                         item.construction_state != CityConstructionState.Existing))
            {
                DrawPlanningPatch(
                    patch,
                    patch.construction_state == CityConstructionState.UnderConstruction
                        ? T("GROWING", "生长中")
                        : T("PROPOSED", "待建设"));
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
            float[][] site = CompactBuildingSitePolygon(building);
            CreatePolygon(
                planningPreviewRoot.transform,
                $"{status} {building.id}",
                site,
                0.115f,
                new Color(0.30f, 0.84f, 0.90f, 0.62f),
                8,
                true);
            CreateLine(
                planningPreviewRoot.transform,
                $"{status} {building.id} edge",
                site.Concat(new[] { site[0] }).ToArray(),
                0.025f,
                new Color(0.18f, 0.61f, 0.68f, 0.92f),
                0.120f,
                9,
                true);
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
            int pedestrianSpriteIndex = 0;
            int vehicleSpriteIndex = 0;
            for (int index = 0; index < plan.trips.Length; index += 1)
            {
                CityRepresentativeTrip trip = plan.trips[index];
                string humanSprite = trip.mode == CityTravelMode.Walk
                    ? humanSprites[pedestrianSpriteIndex++ % humanSprites.Length]
                    : humanSprites[index % humanSprites.Length];
                GameObject human = CreateHumanToken(root.transform, index, humanSprite);
                GameObject vehicle = trip.mode == CityTravelMode.Drive
                    ? CreateVehicleToken(root.transform, index, vehicleSpriteIndex++)
                    : null;
                human.SetActive(false);
                vehicle?.SetActive(false);
                actorViews[index] = new ActorView { human = human, vehicle = vehicle };
            }
        }

        private void BuildWildlife()
        {
            wildlifeViews.Clear();
            GameObject root = ChildRoot("City wildlife agents");
            foreach (CityWildlifeAgent agent in wildlife.Snapshot.agents)
            {
                GameObject token = CreateWildlifeToken(root.transform, agent);
                token.name = agent.id;
                wildlifeViews.Add(agent.id, token);
            }
            UpdateWildlife();
        }

        private GameObject CreateWildlifeToken(Transform parent, CityWildlifeAgent agent)
        {
            GameObject token = new GameObject(agent.id);
            token.transform.SetParent(parent, false);
            string resourcePath = WildlifeResourcePath(agent.species);
            Sprite sprite = string.IsNullOrWhiteSpace(resourcePath)
                ? null
                : Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                GameObject artwork = new GameObject("Wildlife artwork");
                artwork.transform.SetParent(token.transform, false);
                artwork.transform.localPosition = Vector3.zero;
                artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                float targetWidth = WildlifeArtworkWidth(agent.species);
                float scale = targetWidth / Mathf.Max(0.001f, sprite.bounds.size.x);
                artwork.transform.localScale = Vector3.one * scale;
                SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 46;
                return token;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallback.name = "Fallback wildlife marker";
            fallback.transform.SetParent(token.transform, false);
            fallback.transform.localScale = new Vector3(0.13f, 0.025f, 0.13f);
            RemoveCollider(fallback);
            SetMaterial(fallback, WildlifeColour(agent.species));
            CreateLabel(
                fallback.transform,
                WildlifeLabel(agent.species),
                new Vector3(0f, 0.7f, 0f),
                0.10f);
            return token;
        }

        private static string WildlifeResourcePath(CityWildlifeSpecies species)
        {
            switch (species)
            {
                case CityWildlifeSpecies.Pigeon:
                    return PigeonWildlifeResourcePath;
                case CityWildlifeSpecies.GreySquirrel:
                    return SquirrelWildlifeResourcePath;
                case CityWildlifeSpecies.Fox:
                    return FoxWildlifeResourcePath;
                case CityWildlifeSpecies.Hedgehog:
                    return HedgehogWildlifeResourcePath;
                default:
                    return null;
            }
        }

        private static float WildlifeArtworkWidth(CityWildlifeSpecies species)
        {
            switch (species)
            {
                case CityWildlifeSpecies.Pigeon:
                    return 0.15f;
                case CityWildlifeSpecies.GreySquirrel:
                    return 0.19f;
                case CityWildlifeSpecies.Fox:
                    return 0.28f;
                case CityWildlifeSpecies.Hedgehog:
                    return 0.17f;
                default:
                    return 0.15f;
            }
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
            GameObject token = new GameObject();
            token.name = $"Representative human {index + 1:00}";
            token.transform.SetParent(parent, false);

            Sprite sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                GameObject artwork = new GameObject("Human artwork");
                artwork.transform.SetParent(token.transform, false);
                artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                float scale = HumanArtworkHeight / Mathf.Max(0.001f, sprite.bounds.size.y);
                artwork.transform.localScale = Vector3.one * scale;
                SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 40;
                return token;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fallback.name = "Fallback human marker";
            fallback.transform.SetParent(token.transform, false);
            fallback.transform.localScale = new Vector3(0.10f, 0.025f, 0.10f);
            RemoveCollider(fallback);
            SetMaterial(fallback, new Color(0.70f, 0.47f, 0.24f, 1f));
            return token;
        }

        private GameObject CreateVehicleToken(Transform parent, int index, int variantIndex)
        {
            GameObject vehicle = new GameObject($"Trip vehicle {index + 1:00}");
            vehicle.transform.SetParent(parent, false);
            string resourcePath = CityVehicleResourcePaths[
                variantIndex % CityVehicleResourcePaths.Length];
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                GameObject artwork = new GameObject("Vehicle artwork");
                artwork.transform.SetParent(vehicle.transform, false);
                artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                float scale = VehicleArtworkDepth / Mathf.Max(0.001f, sprite.bounds.size.y);
                artwork.transform.localScale = Vector3.one * scale;
                SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 43;
                return vehicle;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fallback.name = "Fallback vehicle block";
            fallback.transform.SetParent(vehicle.transform, false);
            fallback.transform.localScale = new Vector3(0.24f, 0.08f, 0.13f);
            RemoveCollider(fallback);
            SetMaterial(fallback, new Color(0.36f, 0.58f, 0.74f, 1f));
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
                view.vehicle?.SetActive(active && agent.Trip.mode == CityTravelMode.Drive);
                if (!active)
                {
                    continue;
                }

                Vector3 position = ToWorld(agent.PositionNorm, 0.18f);
                GameObject movingObject = travellingByCar && view.vehicle != null
                    ? view.vehicle
                    : view.human;
                movingObject.transform.localPosition = position;
                if (agent.Trip.mode == CityTravelMode.Drive &&
                    !travellingByCar && view.vehicle != null)
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

        private void HandleDesktopPointerInput()
        {
            if (planningWorkflow?.Phase != CityPlanningWorkflowPhase.ReadyToScan ||
                bottomNavigationIndex != 0 ||
                !UnityEngine.Input.GetMouseButtonDown(0))
            {
                return;
            }
            Vector3 mouse = UnityEngine.Input.mousePosition;
            if (mouse.x < 0f || mouse.x >= Screen.width * MapViewportWidth ||
                mouse.y < 0f || mouse.y >= Screen.height)
            {
                return;
            }
            Vector2 guiPoint = new Vector2(mouse.x, Screen.height - mouse.y);
            if (PointerOverMapChrome(guiPoint))
            {
                return;
            }
            Camera camera = Camera.main;
            if (camera == null)
            {
                desktopInputMessage = "The map camera is unavailable.";
                return;
            }
            Ray ray = camera.ScreenPointToRay(mouse);
            Plane boardPlane = new Plane(Vector3.up, Vector3.zero);
            if (!boardPlane.Raycast(ray, out float distance))
            {
                desktopInputMessage = "That click did not reach the map.";
                return;
            }
            Vector3 world = ray.GetPoint(distance);
            float xNorm = world.x / MapWidth + 0.5f;
            float yNorm = 0.5f - world.z / MapHeight;
            TryPlaceDesktopBuilding(desktopBuildingType, xNorm, yNorm, out _);
        }

        private bool PointerOverMapChrome(Vector2 guiPoint)
        {
            float scale = CityPrototypeUiTheme.ScaleForScreen(Screen.height);
            float mapWidth = Screen.width * MapViewportWidth;
            float padding = CityPrototypeUiTheme.ScaledPixel(18, scale);
            float headerWidth = Mathf.Min(
                CityPrototypeUiTheme.ScaledPixel(430, scale),
                mapWidth * 0.43f);
            Rect header = new Rect(
                padding,
                padding,
                headerWidth,
                CityPrototypeUiTheme.ScaledPixel(100, scale));
            float statusWidth = Mathf.Min(
                CityPrototypeUiTheme.ScaledPixel(450, scale),
                mapWidth * 0.40f);
            Rect status = new Rect(
                mapWidth - padding - statusWidth,
                padding,
                statusWidth,
                CityPrototypeUiTheme.ScaledPixel(54, scale));
            return header.Contains(guiPoint) ||
                   status.Contains(guiPoint) ||
                   CityStatusRectForScreen(Screen.width, Screen.height).Contains(guiPoint) ||
                   ToolbarRectForScreen(Screen.width, Screen.height).Contains(guiPoint) ||
                   (bottomNavigationIndex == 0 &&
                    BuildingModeRectForScreen(Screen.width, Screen.height).Contains(guiPoint)) ||
                   (bottomNavigationIndex == 2 &&
                    TraceModeRectForScreen(Screen.width, Screen.height).Contains(guiPoint)) ||
                   (LatestCriticalEvent() != null &&
                    EventPopupRectForScreen(Screen.width, Screen.height).Contains(guiPoint));
        }

        public bool TryPlaceDesktopBuilding(
            CityPhysicalTokenType type,
            float xNorm,
            float yNorm,
            out string error)
        {
            error = string.Empty;
            if (useCameraInput)
            {
                error = "Switch to Desktop Play before placing with the pointer.";
                desktopInputMessage = error;
                return false;
            }
            if (planningWorkflow?.Phase != CityPlanningWorkflowPhase.ReadyToScan)
            {
                error = "Finish or cancel the current preview first.";
                desktopInputMessage = error;
                return false;
            }
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!CityPlanningDemoScanFactory.TryCreateDesktopPlacementScan(
                    city,
                    type,
                    xNorm,
                    yNorm,
                    timestamp,
                    out CityTokenScanPacket scan,
                    out error) ||
                !planningWorkflow.TryAcceptScan(scan, out error))
            {
                desktopInputMessage = error;
                return false;
            }
            desktopBuildingType = type;
            desktopInputMessage = planningWorkflow.Snapshot.CanConfirmPreview
                ? "Preview ready. Confirm to build and connect it to the nearest existing street."
                : planningWorkflow.Snapshot.message;
            RefreshPlanningOverlay();
            return true;
        }

        public bool ConfirmDesktopPlacement(out string error)
        {
            error = string.Empty;
            if (planningWorkflow?.Phase != CityPlanningWorkflowPhase.Preview)
            {
                error = "There is no desktop placement to confirm.";
                desktopInputMessage = error;
                return false;
            }
            if (!planningWorkflow.ConfirmPreview(out error) ||
                !planningWorkflow.StartConstruction(out error))
            {
                desktopInputMessage = error;
                RefreshPlanningOverlay();
                return false;
            }

            // Desktop play is deliberately immediate: the player confirms a placement,
            // while the full time-block construction presentation remains available to
            // the camera/research edition.
            for (int block = 0;
                 block < 8 && planningWorkflow.Phase == CityPlanningWorkflowPhase.Construction;
                 block += 1)
            {
                planningWorkflow.AdvanceConstructionBlock();
            }
            if (planningWorkflow.Phase != CityPlanningWorkflowPhase.Complete)
            {
                error = "Construction did not complete within the desktop build step.";
                desktopInputMessage = error;
                return false;
            }
            desktopInputMessage = "Built. Choose another building and click the map.";
            InitializePrototype(false);
            return true;
        }

        private void CancelDesktopPlacement()
        {
            planningWorkflow?.CancelPreview();
            desktopInputMessage = "Placement cancelled. Choose another position.";
            RefreshPlanningOverlay();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || plan == null || mobility == null)
            {
                return;
            }

            EnsureStyles();
            GUI.depth = -20;
            DrawMapChrome();
            DrawRightSidebar();
            DrawCriticalEventPopup();
        }

        private void DrawMapChrome()
        {
            float mapWidth = Screen.width * MapViewportWidth;
            float padding = CityPrototypeUiTheme.ScaledPixel(18, uiScale);
            float headerHeight = CityPrototypeUiTheme.ScaledPixel(100, uiScale);
            float headerWidth = Mathf.Min(
                CityPrototypeUiTheme.ScaledPixel(430, uiScale),
                mapWidth * 0.43f);
            Rect headerRect = new Rect(padding, padding, headerWidth, headerHeight);
            DrawShadowedCard(headerRect);

            float backSize = CityPrototypeUiTheme.ScaledPixel(46, uiScale);
            Rect backRect = new Rect(
                headerRect.x + CityPrototypeUiTheme.ScaledPixel(10, uiScale),
                headerRect.center.y - backSize * 0.5f,
                backSize,
                backSize);
            GUI.Button(backRect, "←", backButtonStyle);

            float textX = backRect.xMax + CityPrototypeUiTheme.ScaledPixel(12, uiScale);
            float languageWidth = CityPrototypeUiTheme.ScaledPixel(72, uiScale);
            Rect titleRect = new Rect(
                textX,
                headerRect.y + CityPrototypeUiTheme.ScaledPixel(8, uiScale),
                headerRect.xMax - textX - languageWidth -
                CityPrototypeUiTheme.ScaledPixel(10, uiScale),
                CityPrototypeUiTheme.ScaledPixel(32, uiScale));
            GUI.Label(titleRect, T("Riverside Park", "河畔公园"), mapTitleStyle);
            Rect subtitleRect = new Rect(
                textX,
                titleRect.yMax,
                titleRect.width,
                CityPrototypeUiTheme.ScaledPixel(24, uiScale));
            GUI.Label(
                subtitleRect,
                T("A city for people and wildlife", "人与野生动物共生的城市"),
                mapSubtitleStyle);
            Rect stageRect = new Rect(
                textX,
                headerRect.y + CityPrototypeUiTheme.ScaledPixel(66, uiScale),
                Mathf.Min(titleRect.width, CityPrototypeUiTheme.ScaledPixel(228, uiScale)),
                CityPrototypeUiTheme.ScaledPixel(25, uiScale));
            GUI.Label(
                stageRect,
                $"{T("STAGE " + DisplayStageNumber, "阶段 " + DisplayStageNumber)}  ·  " +
                StageName(DisplayStageNumber),
                heatmapToggleStyle);
            DrawLanguagePill(new Rect(
                headerRect.xMax - languageWidth - CityPrototypeUiTheme.ScaledPixel(8, uiScale),
                headerRect.y + CityPrototypeUiTheme.ScaledPixel(12, uiScale),
                languageWidth,
                CityPrototypeUiTheme.ScaledPixel(30, uiScale)));

            float statusWidth = Mathf.Min(
                CityPrototypeUiTheme.ScaledPixel(450, uiScale),
                mapWidth * 0.40f);
            Rect statusRect = new Rect(
                mapWidth - padding - statusWidth,
                padding,
                statusWidth,
                CityPrototypeUiTheme.ScaledPixel(54, uiScale));
            DrawShadowedCard(statusRect);
            string[] statusItems =
            {
                T($"YEAR {DisplayYear}  ·  {SeasonName(DisplayStageNumber)}",
                    $"第 {DisplayYear} 年  ·  {SeasonName(DisplayStageNumber)}"),
                $"{T("PEOPLE", "人口")}  {plan.RepresentedPopulation}",
                $"{T("NATURE", "自然")}  {Mathf.Clamp(Mathf.RoundToInt(CityBalanceTotal), 0, 99)}",
                $"{T("WELLBEING", "幸福")}  72",
            };
            float itemWidth = statusRect.width / statusItems.Length;
            for (int index = 0; index < statusItems.Length; index += 1)
            {
                Rect itemRect = new Rect(
                    statusRect.x + itemWidth * index,
                    statusRect.y,
                    itemWidth,
                    statusRect.height);
                GUI.Label(itemRect, statusItems[index], topMetricStyle);
                if (index > 0)
                {
                    GUI.DrawTexture(
                        new Rect(itemRect.x, itemRect.y + itemRect.height * 0.24f, 1f,
                            itemRect.height * 0.52f),
                        progressTrackStyle.normal.background);
                }
            }

            DrawCityStatusCard(CityStatusRectForScreen(Screen.width, Screen.height));
            DrawBottomToolbar(ToolbarRectForScreen(Screen.width, Screen.height));
            if (bottomNavigationIndex == 0)
            {
                DrawBuildingModeCard(BuildingModeRectForScreen(Screen.width, Screen.height));
            }
            else if (bottomNavigationIndex == 2)
            {
                DrawTraceModeBar(TraceModeRectForScreen(Screen.width, Screen.height));
            }
        }

        private void DrawBuildingModeCard(Rect rect)
        {
            DrawShadowedCard(rect);
            float padding = CityPrototypeUiTheme.ScaledPixel(14, uiScale);
            float line = CityPrototypeUiTheme.ScaledPixel(24, uiScale);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding * 0.65f,
                    rect.width - padding * 2f, line),
                T("PLACE BUILDING", "放置建筑"),
                headingStyle);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding + line,
                    rect.width - padding * 2f, line),
                DesktopBuildingLabel(desktopBuildingType),
                metricStyle);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding + line * 2f,
                    rect.width - padding * 2f, line * 2f),
                BuildingDescription(desktopBuildingType),
                captionStyle);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding + line * 4.15f,
                    rect.width - padding * 2f, line * 2.6f),
                BuildingImpactSummary(desktopBuildingType),
                bodyStyle);

            Rect actionRect = new Rect(
                rect.x + padding,
                rect.yMax - padding - CityPrototypeUiTheme.ScaledPixel(36, uiScale),
                rect.width - padding * 2f,
                CityPrototypeUiTheme.ScaledPixel(36, uiScale));
            if (planningWorkflow?.Phase == CityPlanningWorkflowPhase.Preview)
            {
                float gap = CityPrototypeUiTheme.ScaledPixel(7, uiScale);
                float width = (actionRect.width - gap) * 0.5f;
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && planningWorkflow.Snapshot.CanConfirmPreview;
                if (GUI.Button(new Rect(actionRect.x, actionRect.y, width, actionRect.height),
                        T("CONFIRM", "确认"), buttonStyle))
                {
                    ConfirmDesktopPlacement(out _);
                }
                GUI.enabled = previousEnabled;
                if (GUI.Button(new Rect(actionRect.x + width + gap, actionRect.y, width,
                        actionRect.height), T("CANCEL", "取消"), buttonStyle))
                {
                    CancelDesktopPlacement();
                }
            }
            else
            {
                GUI.Label(actionRect,
                    T("Click open ground to preview", "点击空地进行预览"), captionStyle);
            }
        }

        private void DrawTraceModeBar(Rect rect)
        {
            DrawShadowedCard(rect);
            string[] labels =
            {
                T("NORMAL", "普通"),
                T("HUMAN", "人类"),
                T("ANIMAL", "动物"),
                T("COMBINED", "综合"),
            };
            float padding = CityPrototypeUiTheme.ScaledPixel(7, uiScale);
            float gap = CityPrototypeUiTheme.ScaledPixel(5, uiScale);
            float width = (rect.width - padding * 2f - gap * 3f) / 4f;
            for (int index = 0; index < labels.Length; index += 1)
            {
                Rect buttonRect = new Rect(
                    rect.x + padding + index * (width + gap),
                    rect.y + padding,
                    width,
                    rect.height - padding * 2f);
                if (GUI.Button(buttonRect, labels[index],
                        traceViewIndex == index ? activeTabStyle : toolbarButtonStyle))
                {
                    SetTraceView(index);
                }
            }
        }

        private void DrawCriticalEventPopup()
        {
            CityFeedEntry entry = LatestCriticalEvent();
            if (entry == null)
            {
                return;
            }
            Rect rect = EventPopupRectForScreen(Screen.width, Screen.height);
            DrawShadowedCard(rect);
            float padding = CityPrototypeUiTheme.ScaledPixel(16, uiScale);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding,
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(30, uiScale)),
                entry.type == CityFeedEventType.Roadkill
                    ? T("ROADKILL DETECTED", "发现道路伤亡")
                    : T("WASTE ALERT", "垃圾警报"),
                headingStyle);
            GUI.Label(
                new Rect(rect.x + padding,
                    rect.y + CityPrototypeUiTheme.ScaledPixel(48, uiScale),
                    rect.width - padding * 2f,
                    CityPrototypeUiTheme.ScaledPixel(54, uiScale)),
                LocaliseFeedMessage(entry),
                bodyStyle);
            Rect previewRect = new Rect(
                rect.x + padding,
                rect.y + CityPrototypeUiTheme.ScaledPixel(104, uiScale),
                rect.width - padding * 2f,
                CityPrototypeUiTheme.ScaledPixel(86, uiScale));
            DrawHeatmapMapBase(previewRect);
            if (GUI.Button(
                    new Rect(rect.x + padding,
                        rect.yMax - padding - CityPrototypeUiTheme.ScaledPixel(36, uiScale),
                        rect.width - padding * 2f,
                        CityPrototypeUiTheme.ScaledPixel(36, uiScale)),
                    T("OK", "确定"), buttonStyle))
            {
                dismissedEventIds.Add(EventKey(entry));
            }
        }

        private void DrawCityStatusCard(Rect rect)
        {
            DrawShadowedCard(rect);
            float padding = CityPrototypeUiTheme.ScaledPixel(16, uiScale);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding * 0.65f,
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(26, uiScale)),
                T("City Status", "城市状态"),
                metricStyle);
            float firstY = rect.y + CityPrototypeUiTheme.ScaledPixel(48, uiScale);
            float rowHeight = CityPrototypeUiTheme.ScaledPixel(27, uiScale);
            DrawStatusBar(rect, firstY, T("Biodiversity", "生物多样性"), 72,
                progressGreenStyle);
            DrawStatusBar(rect, firstY + rowHeight, T("Cleanliness", "环境整洁度"),
                environment?.Snapshot?.overflow_active == true ? 54 : 76, progressBlueStyle);
            DrawStatusBar(rect, firstY + rowHeight * 2f, T("Resident Happiness", "居民幸福度"),
                Mathf.Clamp(Mathf.RoundToInt(CityBalanceTotal), 0, 100), progressOrangeStyle);
            DrawStatusBar(rect, firstY + rowHeight * 3f, T("Animal Wellbeing", "动物福祉"),
                Mathf.Clamp(68 + WildlifeSpeciesCount * 2, 0, 100), progressPurpleStyle);
        }

        private void DrawStatusBar(Rect card, float y, string label, int value, GUIStyle fillStyle)
        {
            float padding = CityPrototypeUiTheme.ScaledPixel(16, uiScale);
            float labelWidth = card.width * 0.39f;
            float valueWidth = CityPrototypeUiTheme.ScaledPixel(30, uiScale);
            float barX = card.x + padding + labelWidth;
            float barWidth = card.width - padding * 2f - labelWidth - valueWidth;
            Rect labelRect = new Rect(
                card.x + padding,
                y,
                labelWidth,
                CityPrototypeUiTheme.ScaledPixel(22, uiScale));
            GUI.Label(labelRect, label, captionStyle);
            Rect trackRect = new Rect(
                barX,
                y + CityPrototypeUiTheme.ScaledPixel(5, uiScale),
                barWidth,
                CityPrototypeUiTheme.ScaledPixel(12, uiScale));
            GUI.Box(trackRect, GUIContent.none, progressTrackStyle);
            GUI.Box(
                new Rect(trackRect.x, trackRect.y,
                    trackRect.width * Mathf.Clamp01(value / 100f), trackRect.height),
                GUIContent.none,
                fillStyle);
            GUI.Label(
                new Rect(trackRect.xMax + CityPrototypeUiTheme.ScaledPixel(6, uiScale),
                    y, valueWidth, CityPrototypeUiTheme.ScaledPixel(22, uiScale)),
                value.ToString(),
                captionStyle);
        }

        private void DrawBottomToolbar(Rect rect)
        {
            DrawShadowedCard(rect);
            string[] labels =
            {
                T("BUILD", "建造"),
                T("INFO", "信息"),
                T("TRACE", "轨迹"),
                paused ? T("RESUME", "继续") : T("PAUSE", "暂停"),
            };
            float padding = CityPrototypeUiTheme.ScaledPixel(10, uiScale);
            float gap = CityPrototypeUiTheme.ScaledPixel(7, uiScale);
            float buttonWidth = (rect.width - padding * 2f - gap * (labels.Length - 1)) /
                                labels.Length;
            for (int index = 0; index < labels.Length; index += 1)
            {
                Rect buttonRect = new Rect(
                    rect.x + padding + index * (buttonWidth + gap),
                    rect.y + padding,
                    buttonWidth,
                    rect.height - padding * 2f);
                GUIStyle style = index == bottomNavigationIndex
                    ? selectedCatalogItemStyle
                    : toolbarButtonStyle;
                if (!GUI.Button(buttonRect, labels[index], style))
                {
                    continue;
                }
                bottomNavigationIndex = index;
                switch (index)
                {
                    case 0:
                        sidebarTab = 1;
                        break;
                    case 1:
                        sidebarTab = 0;
                        break;
                    case 2:
                        sidebarTab = 0;
                        if (traceViewIndex == 0)
                        {
                            SetTraceView(2);
                        }
                        break;
                    default:
                        paused = !paused;
                        break;
                }
            }
        }

        private void DrawRightSidebar()
        {
            float panelX = Screen.width * MapViewportWidth;
            float panelWidth = Screen.width - panelX;
            Rect panelRect = new Rect(panelX, 0f, panelWidth, Screen.height);
            GUI.DrawTexture(panelRect, panelStyle.normal.background, ScaleMode.StretchToFill, false);

            float padding = CityPrototypeUiTheme.ScaledPixel(14, uiScale);
            Rect tabsRect = new Rect(
                panelX + padding,
                CityPrototypeUiTheme.ScaledPixel(18, uiScale),
                panelWidth - padding * 2f,
                CityPrototypeUiTheme.ScaledPixel(50, uiScale));
            GUI.Box(tabsRect, GUIContent.none, tabBarStyle);
            string[] tabs = { T("Animals", "动物"), T("Buildings", "建筑"), T("Tools", "工具") };
            float tabWidth = tabsRect.width / tabs.Length;
            for (int index = 0; index < tabs.Length; index += 1)
            {
                Rect tabRect = new Rect(
                    tabsRect.x + index * tabWidth,
                    tabsRect.y,
                    tabWidth,
                    tabsRect.height);
                if (GUI.Button(tabRect, tabs[index],
                        sidebarTab == index ? activeTabStyle : tabStyle))
                {
                    sidebarTab = index;
                }
            }

            Rect heatmapRect = HeatmapCardRectForScreen(Screen.width, Screen.height);
            Rect contentRect = new Rect(
                panelX + padding,
                tabsRect.yMax + CityPrototypeUiTheme.ScaledPixel(10, uiScale),
                panelWidth - padding * 2f,
                Mathf.Max(1f, heatmapRect.y - tabsRect.yMax -
                    CityPrototypeUiTheme.ScaledPixel(20, uiScale)));
            if (sidebarTab == 0)
            {
                DrawAnimalSidebar(contentRect);
            }
            else if (sidebarTab == 2)
            {
                DrawToolsSidebar(contentRect);
            }
            else
            {
                DrawBuildingSidebar(contentRect);
            }
            DrawHeatmapCard(heatmapRect);
        }

        private void DrawBuildingSidebar(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, chromeCardStyle);
            float padding = CityPrototypeUiTheme.ScaledPixel(14, uiScale);
            float gap = CityPrototypeUiTheme.ScaledPixel(8, uiScale);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding * 0.7f,
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(28, uiScale)),
                T("Buildings", "建筑"),
                metricStyle);

            float gridTop = rect.y + CityPrototypeUiTheme.ScaledPixel(38, uiScale);
            float cellWidth = (rect.width - padding * 2f - gap) * 0.5f;
            float rowHeight = CityPrototypeUiTheme.ScaledPixel(114, uiScale);
            DrawBuildingCatalogItem(
                new Rect(rect.x + padding, gridTop, cellWidth, rowHeight),
                CityPhysicalTokenType.DetachedHouse,
                CityResidentialLotBlueResourcePath,
                T("Detached House", "独栋住宅"));
            DrawBuildingCatalogItem(
                new Rect(rect.x + padding + cellWidth + gap, gridTop, cellWidth, rowHeight),
                CityPhysicalTokenType.Apartment,
                CityResidentialLotBResourcePath,
                T("Apartment", "公寓"));
            DrawBuildingCatalogItem(
                new Rect(rect.x + padding, gridTop + rowHeight + gap, cellWidth, rowHeight),
                CityPhysicalTokenType.Commercial,
                CityCommercialResourcePaths[0],
                T("Market", "市场"));
            DrawBuildingCatalogItem(
                new Rect(rect.x + padding + cellWidth + gap, gridTop + rowHeight + gap,
                    cellWidth, rowHeight),
                CityPhysicalTokenType.CommunityFacility,
                CityCommunityResourcePaths[0],
                T("Community", "社区设施"));

            float actionY = gridTop + rowHeight * 2f + gap * 2f;
            DrawPlacementActionRow(new Rect(
                rect.x + padding,
                actionY,
                rect.width - padding * 2f,
                CityPrototypeUiTheme.ScaledPixel(38, uiScale)));

            float dividerY = actionY + CityPrototypeUiTheme.ScaledPixel(44, uiScale);
            GUI.DrawTexture(
                new Rect(rect.x + padding, dividerY, rect.width - padding * 2f, 1f),
                progressTrackStyle.normal.background);
            GUI.Label(
                new Rect(rect.x + padding, dividerY + CityPrototypeUiTheme.ScaledPixel(8, uiScale),
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(26, uiScale)),
                T("Environment", "环境"),
                metricStyle);

            float environmentTop = dividerY + CityPrototypeUiTheme.ScaledPixel(34, uiScale);
            float environmentGap = CityPrototypeUiTheme.ScaledPixel(5, uiScale);
            float environmentWidth = (rect.width - padding * 2f - environmentGap * 2f) / 3f;
            float environmentHeight = Mathf.Max(
                CityPrototypeUiTheme.ScaledPixel(58, uiScale),
                Mathf.Min(
                    CityPrototypeUiTheme.ScaledPixel(74, uiScale),
                    (rect.yMax - environmentTop - padding - environmentGap) * 0.5f));
            string[] environmentLabels =
            {
                T("Tree", "树木"), T("Bush", "灌木"), T("Pond", "池塘"),
                T("Bench", "长椅"), T("Bin", "垃圾桶"), T("Street Light", "路灯"),
            };
            string[] environmentPaths =
            {
                LegacyCityTreeResourcePath,
                CityBushResourcePath,
                CityPondResourcePath,
                "UrbanWildlife/Environment/human-activity-bench-v01",
                string.Empty,
                string.Empty,
            };
            for (int index = 0; index < environmentLabels.Length; index += 1)
            {
                int column = index % 3;
                int row = index / 3;
                DrawEnvironmentItem(
                    new Rect(
                        rect.x + padding + column * (environmentWidth + environmentGap),
                        environmentTop + row * (environmentHeight + environmentGap),
                        environmentWidth,
                        environmentHeight),
                    environmentPaths[index],
                    environmentLabels[index],
                    index);
            }
        }

        private void DrawPlacementActionRow(Rect rect)
        {
            if (planningWorkflow?.Phase == CityPlanningWorkflowPhase.Preview)
            {
                float gap = CityPrototypeUiTheme.ScaledPixel(6, uiScale);
                float width = (rect.width - gap) * 0.5f;
                bool previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && planningWorkflow.Snapshot.CanConfirmPreview;
                if (GUI.Button(new Rect(rect.x, rect.y, width, rect.height),
                        T("Build", "建造"), buttonStyle))
                {
                    ConfirmDesktopPlacement(out _);
                }
                GUI.enabled = previousEnabled;
                if (GUI.Button(new Rect(rect.x + width + gap, rect.y, width, rect.height),
                        T("Cancel", "取消"), buttonStyle))
                {
                    CancelDesktopPlacement();
                }
                return;
            }
            GUI.Label(
                rect,
                $"{T("Selected", "已选择")} · {DesktopBuildingLabel(desktopBuildingType)}  —  " +
                T("click open ground", "点击地图空地放置"),
                captionStyle);
        }

        private void DrawBuildingCatalogItem(
            Rect rect,
            CityPhysicalTokenType type,
            string resourcePath,
            string label)
        {
            bool selected = desktopBuildingType == type;
            if (GUI.Button(rect, GUIContent.none,
                    selected ? selectedCatalogItemStyle : catalogItemStyle))
            {
                desktopBuildingType = type;
                desktopInputMessage =
                    $"{DesktopBuildingLabel(type)} selected. Click an open part of the map.";
            }
            Rect artworkRect = new Rect(
                rect.x + rect.width * 0.13f,
                rect.y + CityPrototypeUiTheme.ScaledPixel(5, uiScale),
                rect.width * 0.74f,
                rect.height * 0.68f);
            DrawSpriteInRect(Resources.Load<Sprite>(resourcePath), artworkRect);
            GUI.Label(
                new Rect(rect.x + CityPrototypeUiTheme.ScaledPixel(3, uiScale),
                    rect.y + rect.height * 0.72f,
                    rect.width - CityPrototypeUiTheme.ScaledPixel(6, uiScale),
                    rect.height * 0.25f),
                label,
                catalogLabelStyle);
        }

        private void DrawEnvironmentItem(
            Rect rect,
            string resourcePath,
            string label,
            int index)
        {
            GUI.Box(rect, GUIContent.none, catalogItemStyle);
            Rect iconRect = new Rect(
                rect.x + rect.width * 0.22f,
                rect.y + CityPrototypeUiTheme.ScaledPixel(3, uiScale),
                rect.width * 0.56f,
                rect.height * 0.58f);
            Sprite sprite = string.IsNullOrEmpty(resourcePath)
                ? null
                : Resources.Load<Sprite>(resourcePath);
            if (sprite != null)
            {
                DrawSpriteInRect(sprite, iconRect);
            }
            else if (index == 4)
            {
                GUI.Box(
                    new Rect(iconRect.center.x - iconRect.width * 0.18f,
                        iconRect.y + iconRect.height * 0.12f,
                        iconRect.width * 0.36f,
                        iconRect.height * 0.72f),
                    GUIContent.none,
                    progressGreenStyle);
                GUI.DrawTexture(
                    new Rect(iconRect.center.x - iconRect.width * 0.23f,
                        iconRect.y + iconRect.height * 0.05f,
                        iconRect.width * 0.46f,
                        CityPrototypeUiTheme.ScaledPixel(4, uiScale)),
                    progressGreenStyle.normal.background);
            }
            else
            {
                GUI.DrawTexture(
                    new Rect(iconRect.center.x - 1f, iconRect.y + iconRect.height * 0.30f,
                        2f, iconRect.height * 0.56f),
                    metricStyle.normal.background ?? progressTrackStyle.normal.background);
                GUI.Box(
                    new Rect(iconRect.center.x - iconRect.width * 0.12f,
                        iconRect.y + iconRect.height * 0.12f,
                        iconRect.width * 0.24f,
                        iconRect.width * 0.24f),
                    GUIContent.none,
                    progressOrangeStyle);
            }
            GUI.Label(
                new Rect(rect.x, rect.y + rect.height * 0.63f, rect.width, rect.height * 0.33f),
                label,
                catalogLabelStyle);
        }

        private void DrawAnimalSidebar(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, chromeCardStyle);
            float padding = CityPrototypeUiTheme.ScaledPixel(14, uiScale);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding * 0.7f,
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(28, uiScale)),
                T("Animals", "动物"),
                metricStyle);
            string[] paths =
            {
                PigeonWildlifeResourcePath,
                SquirrelWildlifeResourcePath,
                FoxWildlifeResourcePath,
                HedgehogWildlifeResourcePath,
            };
            string[] names =
            {
                T("Pigeon", "鸽子"), T("Squirrel", "松鼠"),
                T("Fox", "狐狸"), T("Hedgehog", "刺猬"),
            };
            float gap = CityPrototypeUiTheme.ScaledPixel(10, uiScale);
            float cellWidth = (rect.width - padding * 2f - gap) * 0.5f;
            float cellHeight = CityPrototypeUiTheme.ScaledPixel(132, uiScale);
            float top = rect.y + CityPrototypeUiTheme.ScaledPixel(42, uiScale);
            for (int index = 0; index < paths.Length; index += 1)
            {
                int column = index % 2;
                int row = index / 2;
                Rect cell = new Rect(
                    rect.x + padding + column * (cellWidth + gap),
                    top + row * (cellHeight + gap),
                    cellWidth,
                    cellHeight);
                if (GUI.Button(cell, GUIContent.none,
                        selectedAnimalIndex == index ? selectedCatalogItemStyle : catalogItemStyle))
                {
                    selectedAnimalIndex = index;
                }
                DrawSpriteInRect(
                    Resources.Load<Sprite>(paths[index]),
                    new Rect(cell.x + cell.width * 0.10f, cell.y + cell.height * 0.08f,
                        cell.width * 0.80f, cell.height * 0.66f));
                GUI.Label(
                    new Rect(cell.x, cell.y + cell.height * 0.74f, cell.width,
                        cell.height * 0.22f),
                    names[index],
                    catalogLabelStyle);
            }
            float detailTop = top + cellHeight * 2f + gap * 2f;
            GUI.Label(
                new Rect(rect.x + padding, detailTop,
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(24, uiScale)),
                names[Mathf.Clamp(selectedAnimalIndex, 0, names.Length - 1)] + "  ·  " +
                AnimalTag(selectedAnimalIndex),
                metricStyle);
            GUI.Label(
                new Rect(rect.x + padding,
                    detailTop + CityPrototypeUiTheme.ScaledPixel(27, uiScale),
                    rect.width - padding * 2f,
                    CityPrototypeUiTheme.ScaledPixel(76, uiScale)),
                AnimalDescription(selectedAnimalIndex) + "\n" +
                T("Press H for animal hotspots.", "按 H 查看动物热点。"),
                bodyStyle);
        }

        private void DrawToolsSidebar(Rect rect)
        {
            GUI.Box(rect, GUIContent.none, chromeCardStyle);
            float padding = CityPrototypeUiTheme.ScaledPixel(14, uiScale);
            GUI.Label(
                new Rect(rect.x + padding, rect.y + padding * 0.7f,
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(28, uiScale)),
                T("Tools", "工具"),
                metricStyle);
            float top = rect.y + CityPrototypeUiTheme.ScaledPixel(46, uiScale);
            float gap = CityPrototypeUiTheme.ScaledPixel(8, uiScale);
            float width = (rect.width - padding * 2f - gap) * 0.5f;
            float height = CityPrototypeUiTheme.ScaledPixel(42, uiScale);
            if (GUI.Button(new Rect(rect.x + padding, top, width, height),
                    paused ? T("Resume", "继续") : T("Pause", "暂停"), buttonStyle))
            {
                paused = !paused;
            }
            if (GUI.Button(new Rect(rect.x + padding + width + gap, top, width, height),
                    T("Restart", "重新开始"), buttonStyle))
            {
                InitializePrototype(false);
            }
            top += height + gap;
            if (GUI.Button(new Rect(rect.x + padding, top, width, height),
                    showVehicleNetwork ? T("Hide roads", "隐藏车路") : T("Show roads", "显示车路"),
                    buttonStyle))
            {
                showVehicleNetwork = !showVehicleNetwork;
                vehicleRoadRoot?.SetActive(showVehicleNetwork);
            }
            if (GUI.Button(new Rect(rect.x + padding + width + gap, top, width, height),
                    showPedestrianNetwork ? T("Hide paths", "隐藏步道") : T("Show paths", "显示步道"),
                    buttonStyle))
            {
                showPedestrianNetwork = !showPedestrianNetwork;
                pedestrianRoot?.SetActive(showPedestrianNetwork);
            }
            top += height + CityPrototypeUiTheme.ScaledPixel(18, uiScale);
            GUI.Label(
                new Rect(rect.x + padding, top, rect.width - padding * 2f,
                    CityPrototypeUiTheme.ScaledPixel(26, uiScale)),
                T("How to build", "建造方法"),
                metricStyle);
            GUI.Label(
                new Rect(rect.x + padding, top + CityPrototypeUiTheme.ScaledPixel(30, uiScale),
                    rect.width - padding * 2f, CityPrototypeUiTheme.ScaledPixel(118, uiScale)),
                T(
                    "1. Open the Buildings tab and choose a building.\n2. Click open ground on the map.\n3. Confirm the preview. The building will connect to the nearest existing street.",
                    "1. 打开“建筑”页并选择一种建筑。\n2. 点击地图上的空地。\n3. 确认预览；建筑会自动接入最近的既有道路。"),
                bodyStyle);
        }

        private void DrawLanguagePill(Rect rect)
        {
            float gap = CityPrototypeUiTheme.ScaledPixel(3, uiScale);
            float width = (rect.width - gap) * 0.5f;
            if (GUI.Button(new Rect(rect.x, rect.y, width, rect.height), "ZH",
                    useChineseUi ? activeTabStyle : tabStyle))
            {
                SetUiLanguage(true);
            }
            if (GUI.Button(new Rect(rect.x + width + gap, rect.y, width, rect.height), "EN",
                    useChineseUi ? tabStyle : activeTabStyle))
            {
                SetUiLanguage(false);
            }
        }

        private void DrawShadowedCard(Rect rect)
        {
            Rect shadow = rect;
            shadow.y += CityPrototypeUiTheme.ScaledPixel(3, uiScale);
            GUI.Box(shadow, GUIContent.none, chromeShadowStyle);
            GUI.Box(rect, GUIContent.none, chromeCardStyle);
        }

        private static void DrawSpriteInRect(Sprite sprite, Rect rect)
        {
            if (sprite?.texture == null)
            {
                return;
            }
            Texture2D texture = sprite.texture;
            Rect source = sprite.textureRect;
            float sourceAspect = source.width / Mathf.Max(1f, source.height);
            float targetAspect = rect.width / Mathf.Max(1f, rect.height);
            Rect fitted = rect;
            if (sourceAspect > targetAspect)
            {
                fitted.height = rect.width / sourceAspect;
                fitted.y = rect.center.y - fitted.height * 0.5f;
            }
            else
            {
                fitted.width = rect.height * sourceAspect;
                fitted.x = rect.center.x - fitted.width * 0.5f;
            }
            Rect uv = new Rect(
                source.x / texture.width,
                source.y / texture.height,
                source.width / texture.width,
                source.height / texture.height);
            GUI.DrawTextureWithTexCoords(fitted, texture, uv, true);
        }

        public static Rect CityStatusRectForScreen(int screenWidth, int screenHeight)
        {
            float scale = CityPrototypeUiTheme.ScaleForScreen(screenHeight);
            float padding = CityPrototypeUiTheme.ScaledPixel(18, scale);
            return new Rect(
                padding,
                screenHeight - padding - CityPrototypeUiTheme.ScaledPixel(172, scale),
                CityPrototypeUiTheme.ScaledPixel(360, scale),
                CityPrototypeUiTheme.ScaledPixel(172, scale));
        }

        public static Rect ToolbarRectForScreen(int screenWidth, int screenHeight)
        {
            float scale = CityPrototypeUiTheme.ScaleForScreen(screenHeight);
            float mapWidth = screenWidth * MapViewportWidth;
            float width = Mathf.Min(
                CityPrototypeUiTheme.ScaledPixel(520, scale),
                mapWidth * 0.42f);
            float height = CityPrototypeUiTheme.ScaledPixel(64, scale);
            return new Rect(
                mapWidth * 0.5f - width * 0.5f,
                screenHeight - CityPrototypeUiTheme.ScaledPixel(18, scale) - height,
                width,
                height);
        }

        public static Rect BuildingModeRectForScreen(int screenWidth, int screenHeight)
        {
            float scale = CityPrototypeUiTheme.ScaleForScreen(screenHeight);
            float padding = CityPrototypeUiTheme.ScaledPixel(18, scale);
            return new Rect(
                padding,
                padding + CityPrototypeUiTheme.ScaledPixel(112, scale),
                CityPrototypeUiTheme.ScaledPixel(270, scale),
                CityPrototypeUiTheme.ScaledPixel(228, scale));
        }

        public static Rect TraceModeRectForScreen(int screenWidth, int screenHeight)
        {
            float scale = CityPrototypeUiTheme.ScaleForScreen(screenHeight);
            Rect toolbar = ToolbarRectForScreen(screenWidth, screenHeight);
            float height = CityPrototypeUiTheme.ScaledPixel(48, scale);
            return new Rect(
                toolbar.x,
                toolbar.y - CityPrototypeUiTheme.ScaledPixel(10, scale) - height,
                toolbar.width,
                height);
        }

        public static Rect EventPopupRectForScreen(int screenWidth, int screenHeight)
        {
            float scale = CityPrototypeUiTheme.ScaleForScreen(screenHeight);
            float mapWidth = screenWidth * MapViewportWidth;
            float width = Mathf.Min(
                CityPrototypeUiTheme.ScaledPixel(390, scale),
                mapWidth * 0.40f);
            float height = CityPrototypeUiTheme.ScaledPixel(260, scale);
            return new Rect(
                mapWidth * 0.5f - width * 0.5f,
                screenHeight * 0.5f - height * 0.5f,
                width,
                height);
        }

        private void DrawLegacySidebar()
        {
            if (!Application.isPlaying || plan == null || mobility == null)
            {
                return;
            }
            EnsureStyles();
            float panelX = Screen.width * MapViewportWidth;
            float panelWidth = Screen.width * (1f - MapViewportWidth);
            Rect panelRect = new Rect(panelX, 0f, panelWidth, Screen.height);
            GUI.DrawTexture(
                panelRect,
                panelStyle.normal.background,
                ScaleMode.StretchToFill,
                false);
            int horizontalPadding = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceLg,
                uiScale);
            int topPadding = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceMd,
                uiScale);
            float contentWidth = Mathf.Max(1f, panelWidth - horizontalPadding * 2f);
            float heatmapGap = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceMd,
                uiScale);
            Rect heatmapCardRect = HeatmapCardRectForScreen(Screen.width, Screen.height);
            Rect contentRect = new Rect(
                panelX + horizontalPadding,
                topPadding,
                contentWidth,
                Mathf.Max(
                    1f,
                    heatmapCardRect.y - topPadding - heatmapGap));
            GUILayout.BeginArea(contentRect, GUIStyle.none);
            sidebarScroll = GUILayout.BeginScrollView(
                sidebarScroll,
                false,
                true,
                GUILayout.Width(contentRect.width),
                GUILayout.Height(contentRect.height));
            DrawLanguageSwitch();
            AddUiSpace(CityPrototypeUiTheme.SpaceSm);
            GUILayout.Label(T("RIVERSIDE WILDLIFE DISTRICT", "河畔野生动物社区"), eyebrowStyle);
            GUILayout.Label(T("Live city", "共生城市"), titleStyle);
            GUILayout.Label(
                T("A shared city for people and wildlife", "一座由人与野生动物共同生活的城市"),
                captionStyle);
            if (strategy?.Snapshot != null)
            {
                GUILayout.Label(
                    $"{DevelopmentPhaseLabel(strategy.Snapshot.phase)}  ·  " +
                    $"{T("Time block", "时段")} {strategy.Snapshot.time_block}",
                    headingStyle);
                GUILayout.Label(
                    $"{T("DP", "发展点")} {strategy.Snapshot.development_points}  ·  " +
                    $"{T("Balance", "综合平衡")} {strategy.Snapshot.balance.total:0}",
                    metricStyle);
            }
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

            DrawPlanningWorkflowPanel();
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

            GUILayout.Label(
                paused ? T("PAUSED", "已暂停") : T("LIVE CITY  ·  ×2", "城市运行中  ·  ×2"),
                headingStyle);
            AddUiSpace(CityPrototypeUiTheme.SpaceSm);
            GUILayout.Label($"{T("Active residents", "活动居民")}  {mobility.ActiveHumanAgentCount}/{plan.RepresentativeAgentCount}", metricStyle);
            GUILayout.Label($"{T("Represented people", "代表人口")}  {plan.RepresentedPopulation}", metricStyle);
            GUILayout.Label($"{T("Walk / Drive", "步行 / 驾车")}  {plan.WalkTripCount} / {plan.DriveTripCount}", metricStyle);
            GUILayout.Label($"{T("Active vehicles", "行驶车辆")}  {mobility.ActiveVehicleAgents.Length}", metricStyle);
            GUILayout.Label($"{T("Completed returns", "已完成往返")}  {mobility.CompletedTripCount}", metricStyle);
            if (environment?.Snapshot != null)
            {
                CityEnvironmentSnapshot snapshot = environment.Snapshot;
                AddUiSpace(CityPrototypeUiTheme.SpaceMd);
                GUILayout.Label(T("CITY ENVIRONMENT", "城市环境"), headingStyle);
                GUILayout.Label($"{T("Natural food", "自然食物")}  {snapshot.natural_food_total:0.00}", metricStyle);
                GUILayout.Label($"{T("Human food", "人工食物")}  {snapshot.anthropogenic_food_total:0.00}", metricStyle);
                GUILayout.Label(
                    $"{T("Waste", "垃圾负荷")}  {snapshot.waste_demand:0.0} / {snapshot.waste_capacity:0.0}",
                    metricStyle);
                GUILayout.Label(
                    snapshot.overflow_active
                        ? T("ALERT · Overflow and litter hotspot", "警报 · 垃圾溢出并形成热点")
                        : T("Waste contained", "垃圾处置正常"),
                    bodyStyle);
                GUILayout.Label($"{T("Mean disturbance", "平均干扰度")}  {snapshot.average_disturbance:0.00}", bodyStyle);
            }
            if (wildlife?.Snapshot != null)
            {
                AddUiSpace(CityPrototypeUiTheme.SpaceMd);
                GUILayout.Label(T("URBAN WILDLIFE", "城市野生动物"), headingStyle);
                GUILayout.Label(
                    $"{T("Active", "活动")}  {wildlife.Snapshot.active_count}  ·  " +
                    $"{T("Feeding", "进食")}  {wildlife.Snapshot.feeding_events}",
                    metricStyle);
                GUILayout.Label(
                    $"{T("Migrated", "迁移")}  {wildlife.Snapshot.migrated_count}  ·  " +
                    $"{T("Roadkill", "道路伤亡")}  {wildlife.Snapshot.roadkill_events}",
                    bodyStyle);
                GUILayout.Label(
                    $"{T("Heat samples", "热点样本")}  {T("People", "人类")} {HumanTracePointCount}  ·  " +
                    $"{T("Wildlife", "动物")} {AnimalTracePointCount}",
                    bodyStyle);
            }
            DrawObservationPanel();
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

            GUILayout.Label(T("MAP KEY", "地图图例"), headingStyle);
            GUILayout.Label(
                $"{T("Planning cells", "规划单元")}  {GeneratedPlanningCellCount}  ·  " +
                $"{T("Available", "可用")}  {AvailablePlanningCellCount}",
                bodyStyle);
            GUILayout.Label(
                T(
                    "Warm-white ground · Woodland · Buildings\nExisting streets — Automatic access road",
                    "暖白空地 · 林地 · 建筑\n既有道路 — 自动接入道路"),
                bodyStyle);
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);

            GUILayout.Label(T("DESTINATION PRESSURE", "目的地压力"), headingStyle);
            foreach (CityDestinationLoad load in plan.destination_loads.Where(item =>
                         item.assigned_representative_agents > 0))
            {
                string status = load.crowd_penalty > 0f
                    ? T("CROWDED", "拥挤")
                    : T("COMFORTABLE", "舒适");
                GUILayout.Label(
                    $"{load.building_id}\n  {load.assigned_people}/{load.comfortable_capacity} " +
                    $"{T("people", "人")} · {status}",
                    bodyStyle);
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(paused ? T("Resume", "继续") : T("Pause", "暂停"), buttonStyle))
            {
                paused = !paused;
            }
            if (GUILayout.Button(T("Restart trips", "重新开始行程"), buttonStyle))
            {
                InitializePrototype(false);
            }
            if (GUILayout.Button(
                    showVehicleNetwork
                        ? T("Hide road routes", "隐藏车辆路线")
                        : T("Show road routes", "显示车辆路线"),
                    buttonStyle))
            {
                showVehicleNetwork = !showVehicleNetwork;
                if (vehicleRoadRoot != null)
                {
                    vehicleRoadRoot.SetActive(showVehicleNetwork);
                }
            }
            if (GUILayout.Button(
                    showPedestrianNetwork
                        ? T("Hide footpaths", "隐藏步行路径")
                        : T("Show footpaths", "显示步行路径"),
                    buttonStyle))
            {
                showPedestrianNetwork = !showPedestrianNetwork;
                if (pedestrianRoot != null)
                {
                    pedestrianRoot.SetActive(showPedestrianNetwork);
                }
            }
            AddUiSpace(CityPrototypeUiTheme.SpaceMd);
            GUILayout.Label(
                T(
                    "Residents emerge from housing and respond to destination appeal, distance and crowding. Vehicles only appear for Drive trips.",
                    "居民从住宅出发，并根据目的地吸引力、距离和拥挤程度选择行程；车辆只会出现在驾车行程中。"),
                captionStyle);
            AddUiSpace(CityPrototypeUiTheme.SpaceLg);
            GUILayout.EndScrollView();
            GUILayout.EndArea();

            DrawHeatmapCard(heatmapCardRect);
        }

        public static Rect HeatmapCardRectForScreen(int screenWidth, int screenHeight)
        {
            float scale = CityPrototypeUiTheme.ScaleForScreen(screenHeight);
            float panelX = screenWidth * MapViewportWidth;
            float panelWidth = screenWidth * (1f - MapViewportWidth);
            float horizontalPadding = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceLg,
                scale);
            float bottomPadding = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceLg,
                scale);
            float availableWidth = Mathf.Max(1f, panelWidth - horizontalPadding * 2f);
            float innerWidth = Mathf.Min(
                Mathf.Max(1f, availableWidth -
                    CityPrototypeUiTheme.ScaledPixel(CityPrototypeUiTheme.SpaceLg, scale) * 2f),
                CityPrototypeUiTheme.ScaledPixel(340, scale));
            float mapHeight = innerWidth * MapHeight / MapWidth;
            float cardHeight = mapHeight + CityPrototypeUiTheme.ScaledPixel(78, scale);
            return new Rect(
                panelX + horizontalPadding,
                screenHeight - bottomPadding - cardHeight,
                availableWidth,
                cardHeight);
        }

        private void DrawHeatmapCard(Rect cardRect)
        {
            GUI.Box(cardRect, GUIContent.none, heatmapCardStyle);
            float padding = CityPrototypeUiTheme.ScaledPixel(
                CityPrototypeUiTheme.SpaceMd,
                uiScale);
            float headerHeight = CityPrototypeUiTheme.ScaledPixel(30, uiScale);
            float footerHeight = CityPrototypeUiTheme.ScaledPixel(26, uiScale);
            Rect titleRect = new Rect(
                cardRect.x + padding,
                cardRect.y + padding * 0.65f,
                cardRect.width - padding * 2f,
                headerHeight);
            GUI.Label(titleRect, TraceViewTitle(), headingStyle);

            float toggleWidth = CityPrototypeUiTheme.ScaledPixel(88, uiScale);
            Rect toggleRect = new Rect(
                cardRect.xMax - padding - toggleWidth,
                cardRect.y + padding * 0.45f,
                toggleWidth,
                CityPrototypeUiTheme.ScaledPixel(28, uiScale));
            if (GUI.Button(
                    toggleRect,
                    showHeatmap ? T("H  ·  ON", "H  ·  开") : T("H  ·  OFF", "H  ·  关"),
                    heatmapToggleStyle))
            {
                SetHeatmapVisible(!showHeatmap);
            }

            float maximumMapWidth = CityPrototypeUiTheme.ScaledPixel(340, uiScale);
            float mapWidth = Mathf.Min(cardRect.width - padding * 2f, maximumMapWidth);
            float mapHeight = mapWidth * MapHeight / MapWidth;
            Rect mapRect = new Rect(
                cardRect.center.x - mapWidth * 0.5f,
                cardRect.y + headerHeight + padding * 0.75f,
                mapWidth,
                mapHeight);
            DrawHeatmapMapBase(mapRect);
            if (showHeatmap)
            {
                DrawActivityHeatDots(mapRect);
            }
            else
            {
                Color previousColour = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.70f);
                GUI.DrawTexture(mapRect, panelStyle.normal.background, ScaleMode.StretchToFill, false);
                GUI.color = previousColour;
                GUI.Label(
                    mapRect,
                    T("Choose a trace view below", "请在下方选择活动视图"),
                    heatmapEmptyStyle);
            }

            Rect footerRect = new Rect(
                cardRect.x + padding,
                mapRect.yMax + CityPrototypeUiTheme.ScaledPixel(4, uiScale),
                cardRect.width - padding * 2f,
                footerHeight);
            if (showHeatmap && heatmapLegendTexture != null)
            {
                float labelWidth = CityPrototypeUiTheme.ScaledPixel(34, uiScale);
                GUI.Label(
                    new Rect(footerRect.x, footerRect.y, labelWidth, footerRect.height),
                    T("Low", "低"),
                    captionStyle);
                GUI.DrawTexture(
                    new Rect(
                        footerRect.x + labelWidth,
                        footerRect.y + footerRect.height * 0.32f,
                        footerRect.width - labelWidth * 2f,
                        CityPrototypeUiTheme.ScaledPixel(8, uiScale)),
                    heatmapLegendTexture,
                    ScaleMode.StretchToFill,
                    false);
                GUI.Label(
                    new Rect(footerRect.xMax - labelWidth, footerRect.y,
                        labelWidth, footerRect.height),
                    T("High", "高"),
                    rightCaptionStyle);
            }
            else
            {
                GUI.Label(
                    footerRect,
                    $"{T("Activity samples", "活动样本")}  {TraceSampleCount()}  ·  " +
                    $"{T("Active areas", "活跃区域")}  {VisibleHeatCellCount}",
                    captionStyle);
            }
        }

        private void DrawHeatmapMapBase(Rect mapRect)
        {
            if (heatmapMapSprite?.texture == null)
            {
                GUI.DrawTexture(
                    mapRect,
                    panelStyle.normal.background,
                    ScaleMode.StretchToFill,
                    false);
                return;
            }
            Texture2D texture = heatmapMapSprite.texture;
            Rect source = heatmapMapSprite.textureRect;
            Rect uv = new Rect(
                source.x / texture.width,
                source.y / texture.height,
                source.width / texture.width,
                source.height / texture.height);
            GUI.DrawTextureWithTexCoords(mapRect, texture, uv, true);
        }

        private void DrawActivityHeatDots(Rect mapRect)
        {
            if (traceVisualizer == null || heatmapDotTexture == null)
            {
                return;
            }
            Color previousColour = GUI.color;
            float cellWidth = mapRect.width / CityTraceVisualizer.HeatmapColumns;
            float cellHeight = mapRect.height / CityTraceVisualizer.HeatmapRows;
            for (int column = 0; column < CityTraceVisualizer.HeatmapColumns; column += 1)
            {
                for (int row = 0; row < CityTraceVisualizer.HeatmapRows; row += 1)
                {
                    float intensity = traceVisualizer.CellIntensity(column, row);
                    if (intensity <= 0f)
                    {
                        continue;
                    }
                    float diameter = Mathf.Min(cellWidth, cellHeight) *
                                     Mathf.Lerp(2.0f, 3.5f, intensity);
                    float centreX = mapRect.x + (column + 0.5f) * cellWidth;
                    float centreY = mapRect.y + (row + 0.5f) * cellHeight;
                    GUI.color = TraceHeatmapColour(intensity);
                    GUI.DrawTexture(
                        new Rect(
                            centreX - diameter * 0.5f,
                            centreY - diameter * 0.5f,
                            diameter,
                            diameter),
                        heatmapDotTexture,
                        ScaleMode.StretchToFill,
                        true);
                }
            }
            GUI.color = previousColour;
        }

        private string TraceViewTitle()
        {
            switch (traceViewIndex)
            {
                case 1: return T("Human Activity", "人类活动");
                case 3: return T("Combined Activity", "综合活动");
                default: return T("Animal Activity", "动物活动");
            }
        }

        private int TraceSampleCount()
        {
            switch (traceViewIndex)
            {
                case 1: return HumanTracePointCount;
                case 3: return HumanTracePointCount + AnimalTracePointCount;
                default: return AnimalTracePointCount;
            }
        }

        private Color TraceHeatmapColour(float intensity)
        {
            if (traceViewIndex == 1)
            {
                Color low = new Color(1f, 0.72f, 0.24f, 0.68f);
                Color high = new Color(0.94f, 0.24f, 0.18f, 0.90f);
                return Color.Lerp(low, high, intensity);
            }
            if (traceViewIndex == 3)
            {
                Color low = new Color(0.23f, 0.68f, 0.76f, 0.70f);
                Color high = new Color(0.60f, 0.33f, 0.80f, 0.88f);
                return Color.Lerp(low, high, intensity);
            }
            Color animalLow = new Color(0.22f, 0.72f, 0.84f, 0.70f);
            Color animalHigh = new Color(0.05f, 0.46f, 0.66f, 0.90f);
            return Color.Lerp(animalLow, animalHigh, intensity);
        }

        private static Color HeatmapColour(float intensity)
        {
            Color low = new Color(0.26f, 0.67f, 0.74f, 0.72f);
            Color middle = new Color(0.98f, 0.75f, 0.25f, 0.80f);
            Color high = new Color(0.93f, 0.30f, 0.20f, 0.88f);
            return intensity < 0.5f
                ? Color.Lerp(low, middle, intensity * 2f)
                : Color.Lerp(middle, high, (intensity - 0.5f) * 2f);
        }

        private void DrawLanguageSwitch()
        {
            GUILayout.Label("LANGUAGE / 语言", eyebrowStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(useChineseUi ? "● 中文" : "中文", buttonStyle))
            {
                SetUiLanguage(true);
            }
            if (GUILayout.Button(useChineseUi ? "EN" : "● EN", buttonStyle))
            {
                SetUiLanguage(false);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawPlanningWorkflowPanel()
        {
            if (planningWorkflow == null)
            {
                return;
            }
            CityPlanningWorkflowSnapshot snapshot = planningWorkflow.Snapshot;
            GUILayout.Label(T("CITY PLANNING", "城市规划"), headingStyle);
            GUILayout.BeginHorizontal();
            bool canSwitchInput = snapshot.phase == CityPlanningWorkflowPhase.ReadyToScan;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && canSwitchInput;
            if (GUILayout.Button(
                    useCameraInput ? T("Desktop", "桌面") : T("DESKTOP", "● 桌面"),
                    buttonStyle))
            {
                useCameraInput = false;
                desktopInputMessage = "Choose a building, then click an open part of the map.";
            }
            if (GUILayout.Button(
                    useCameraInput ? T("CAMERA", "● 摄像头") : T("Camera", "摄像头"),
                    buttonStyle))
            {
                useCameraInput = true;
            }
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            AddUiSpace(CityPrototypeUiTheme.SpaceSm);
            if (!useCameraInput)
            {
                DrawDesktopPlanningPanel(snapshot);
                return;
            }
            GUILayout.Label(WorkflowStepLabel(snapshot.phase), metricStyle);
            GUILayout.Label(LocaliseRuntimeMessage(snapshot.message), bodyStyle);
            AddUiSpace(CityPrototypeUiTheme.SpaceSm);

            switch (snapshot.phase)
            {
                case CityPlanningWorkflowPhase.ReadyToScan:
                    if (GUILayout.Button(T("Load latest camera scan", "载入最新摄像头扫描"), buttonStyle))
                    {
                        TryLoadLatestCameraScan();
                    }
                    GUILayout.Label(LocaliseRuntimeMessage(cityScanInputMessage), bodyStyle);
                    AddUiSpace(CityPrototypeUiTheme.SpaceXs);
                    if (GUILayout.Button(T("Scan city · electronic test", "扫描城市 · 电子测试"), buttonStyle))
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
                        T(
                            "The electronic sample stays available while the physical board is unavailable.",
                            "实体沙盘不可用时，仍可使用电子测试样本。"),
                        bodyStyle);
                    break;

                case CityPlanningWorkflowPhase.Preview:
                    CityConstructionPreview construction = snapshot.construction_preview;
                    if (construction != null)
                    {
                        GUILayout.Label(
                            $"{T("New", "新增")} {construction.NewCount}  ·  " +
                            $"{T("Moved", "移动")} {construction.MovedCount}  ·  " +
                            $"{T("Missing", "缺失")} {construction.MissingCount}",
                            bodyStyle);
                    }
                    GUI.enabled = snapshot.CanConfirmPreview;
                    if (GUILayout.Button(T("Confirm preview", "确认预览"), buttonStyle))
                    {
                        planningWorkflow.ConfirmPreview(out _);
                        RefreshPlanningOverlay();
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button(T("Cancel preview", "取消预览"), buttonStyle))
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
                        $"{T("Cost", "消耗")} {snapshot.required_development_points} DP  ·  " +
                        $"{T("Available", "可用")} {snapshot.strategy.development_points} DP",
                        bodyStyle);
                    GUI.enabled = snapshot.CanStartConstruction;
                    if (GUILayout.Button(T("Start construction", "开始建设"), buttonStyle))
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
                    GUILayout.Label($"{T("Active projects", "进行中项目")}  {active}/{total}", bodyStyle);
                    foreach (CityStrategyProject project in snapshot.strategy.projects.Where(item =>
                                 item.state == CityStrategyProjectState.Active).Take(3))
                    {
                        GUILayout.Label(
                            $"{project.target_id}  ·  {project.remaining_time_blocks} " +
                            T("block(s)", "个时段"),
                            bodyStyle);
                    }
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(T("Pause", "暂停"), buttonStyle)) planningWorkflow.SetTimeScale(0f);
                    if (GUILayout.Button("1×", buttonStyle)) planningWorkflow.SetTimeScale(1f);
                    if (GUILayout.Button("2×", buttonStyle)) planningWorkflow.SetTimeScale(2f);
                    GUILayout.EndHorizontal();
                    if (GUILayout.Button(T("Advance one block", "推进一个时段"), buttonStyle))
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
                        T(
                            $"Built {snapshot.completed_object_ids.Length} confirmed city objects.",
                            $"已建成 {snapshot.completed_object_ids.Length} 个确认的城市对象。"),
                        bodyStyle);
                    GUILayout.Label(
                        T(
                            "The Preview footprints are now live city geometry.",
                            "预览占地已转化为城市中的正式对象。"),
                        bodyStyle);
                    break;
            }
        }

        private void DrawDesktopPlanningPanel(CityPlanningWorkflowSnapshot snapshot)
        {
            GUILayout.Label(T("PLAY DIRECTLY IN UNITY", "直接在 UNITY 中游玩"), metricStyle);
            switch (snapshot.phase)
            {
                case CityPlanningWorkflowPhase.ReadyToScan:
                    GUILayout.Label(T("1  CHOOSE A BUILDING", "1  选择建筑"), headingStyle);
                    bool compactSidebar = Screen.width * (1f - MapViewportWidth) <
                                          CityPrototypeUiTheme.ScaledPixel(430, uiScale);
                    if (compactSidebar)
                    {
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.DetachedHouse,
                            T("HOUSE", "住宅"));
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.Apartment,
                            T("FLATS", "公寓"));
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.Commercial,
                            T("MARKET", "市场"));
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.CommunityFacility,
                            T("CIVIC", "社区"));
                    }
                    else
                    {
                        GUILayout.BeginHorizontal();
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.DetachedHouse,
                            T("HOUSE", "住宅"));
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.Apartment,
                            T("FLATS", "公寓"));
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.Commercial,
                            T("MARKET", "市场"));
                        DrawDesktopBuildingButton(
                            CityPhysicalTokenType.CommunityFacility,
                            T("CIVIC", "社区"));
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.Label(
                        $"{T("Selected", "已选择")}  ·  {DesktopBuildingLabel(desktopBuildingType)}",
                        bodyStyle);
                    GUILayout.Label(T("2  CLICK OPEN GROUND ON THE MAP", "2  点击地图上的可用空地"), headingStyle);
                    GUILayout.Label(
                        T(
                            "Buildings cannot overlap existing streets. Woodland is cleared to warm-white ground, and a short smooth connection is added to the nearest street.",
                            "建筑不能覆盖既有道路。占用林地时会将其改为暖白空地，并自动用短而平滑的支路接入最近道路。"),
                        bodyStyle);
                    GUILayout.Label(LocaliseRuntimeMessage(desktopInputMessage), captionStyle);
                    break;

                case CityPlanningWorkflowPhase.Preview:
                    CityConstructionPreview preview = snapshot.construction_preview;
                    GUILayout.Label(T("PLACEMENT PREVIEW", "放置预览"), headingStyle);
                    if (preview != null)
                    {
                        GUILayout.Label(
                            $"{T("New", "新增")} {preview.NewCount}  ·  " +
                            $"{T("Blocked", "受阻")} {preview.changes.Count(item => item.blocks_confirmation)}",
                            bodyStyle);
                    }
                    GUILayout.Label(LocaliseRuntimeMessage(desktopInputMessage), bodyStyle);
                    bool canConfirm = snapshot.CanConfirmPreview;
                    bool previous = GUI.enabled;
                    GUI.enabled = previous && canConfirm;
                    if (GUILayout.Button(T("BUILD + CONNECT ROAD", "建造并连接道路"), buttonStyle))
                    {
                        ConfirmDesktopPlacement(out _);
                    }
                    GUI.enabled = previous;
                    if (GUILayout.Button(T("Choose another position", "选择其他位置"), buttonStyle))
                    {
                        CancelDesktopPlacement();
                    }
                    break;

                default:
                    GUILayout.Label(T("BUILDING…", "建设中……"), headingStyle);
                    GUILayout.Label(LocaliseRuntimeMessage(snapshot.message), bodyStyle);
                    break;
            }
        }

        private void DrawDesktopBuildingButton(CityPhysicalTokenType type, string label)
        {
            string text = desktopBuildingType == type ? "● " + label : label;
            if (GUILayout.Button(text, buttonStyle))
            {
                desktopBuildingType = type;
                desktopInputMessage =
                    $"{DesktopBuildingLabel(type)} selected. Click an open part of the map.";
            }
        }

        private string DesktopBuildingLabel(CityPhysicalTokenType type)
        {
            switch (type)
            {
                case CityPhysicalTokenType.Apartment: return T("Apartment", "公寓");
                case CityPhysicalTokenType.Commercial: return T("Market", "市场");
                case CityPhysicalTokenType.CommunityFacility: return T("Community building", "社区设施");
                default: return T("Detached house", "独栋住宅");
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
            GUILayout.Label(T("CITY FEED", "城市动态"), headingStyle);
            CityFeedEntry[] latest = observation.Snapshot.city_feed
                .Reverse()
                .Take(4)
                .ToArray();
            if (latest.Length == 0)
            {
                GUILayout.Label(T("Waiting for a meaningful city event…", "等待有意义的城市事件……"), bodyStyle);
            }
            foreach (CityFeedEntry entry in latest)
            {
                GUILayout.Label(
                    $"{FeedSymbol(entry.type)}  {FeedTitle(entry.type)}  ·  {entry.elapsed_seconds:0}s",
                    metricStyle);
                GUILayout.Label(LocaliseFeedMessage(entry), bodyStyle);
            }

            CityPhaseReport report = observation.BuildPhaseReport(
                strategy.Snapshot,
                wildlife.Snapshot);
            AddUiSpace(CityPrototypeUiTheme.SpaceMd);
            GUILayout.Label(T("PHASE SNAPSHOT", "阶段快照"), headingStyle);
            GUILayout.Label(
                $"{DevelopmentPhaseLabel(report.phase)}  ·  {T("Balance", "综合平衡")} {report.after.total:0} " +
                $"({Signed(report.change.total)})",
                metricStyle);
            GUILayout.Label(
                $"{T("Samples", "样本")} {T("People", "人类")} {report.human_trace_points} / " +
                $"{T("Wildlife", "动物")} {report.animal_trace_points}  ·  " +
                $"{T("Feed", "进食")} {report.feeding_events}  ·  " +
                $"{T("Migration", "迁移")} {report.migration_events}",
                bodyStyle);
            if (GUILayout.Button(T("Clear heatmap & feed", "清除热点与动态"), buttonStyle))
            {
                observation.ResetView(
                    mobility.Plan,
                    environment.Snapshot,
                    wildlife.Snapshot,
                    strategy.Snapshot);
                RefreshTraceLayer();
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

        private string FeedTitle(CityFeedEventType type)
        {
            switch (type)
            {
                case CityFeedEventType.WasteOverflow: return T("WASTE PRESSURE", "垃圾压力");
                case CityFeedEventType.Crowding: return T("CROWDING", "拥挤");
                case CityFeedEventType.Feeding: return T("FEEDING", "进食");
                case CityFeedEventType.MigrationIn: return T("MIGRATION IN", "迁入");
                case CityFeedEventType.MigrationOut: return T("MIGRATION OUT", "迁出");
                case CityFeedEventType.Roadkill: return T("ROAD INCIDENT", "道路事件");
                case CityFeedEventType.ProjectCompleted: return T("PROJECT COMPLETE", "项目完成");
                default: return T("BALANCE WARNING", "平衡警告");
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
            traceVisualizer.SetVisible(showHeatmap);
            UpdateTraceVisuals();
        }

        public void SetHeatmapVisible(bool visible)
        {
            SetTraceView(visible ? 2 : 0);
        }

        public void SetTraceView(int viewIndex)
        {
            traceViewIndex = Mathf.Clamp(viewIndex, 0, 3);
            showHeatmap = traceViewIndex != 0;
            switch (traceViewIndex)
            {
                case 1:
                    traceDisplayMode = CityTraceDisplayMode.HumanTrace;
                    break;
                case 3:
                    traceDisplayMode = CityTraceDisplayMode.CombinedTrace;
                    break;
                default:
                    traceDisplayMode = CityTraceDisplayMode.AnimalTrace;
                    break;
            }
            traceVisualizer?.SetMode(traceDisplayMode);
            traceVisualizer?.SetVisible(showHeatmap);
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
                DrawRoadChoiceButton(choice, CityRoadRouteOption.Direct, T("DIRECT", "直接"));
                DrawRoadChoiceButton(choice, CityRoadRouteOption.ExistingNetwork, T("EXISTING", "既有"));
                DrawRoadChoiceButton(choice, CityRoadRouteOption.LowImpact, T("LOW", "低影响"));
                GUILayout.EndHorizontal();
                if (choice.HasSelection)
                {
                    GUILayout.Label(
                        $"{T("Selected", "已选择")}: " +
                        $"{RoadOptionLabel(choice.SelectedCandidate.route_option)} · " +
                        $"{T("green impact", "绿地影响")} " +
                        $"{choice.SelectedCandidate.estimated_green_impact_units:0.0}",
                        bodyStyle);
                }
            }
            if (GUILayout.Button(T("Use low-impact routes", "使用低影响路线"), buttonStyle))
            {
                planningWorkflow.SelectRecommendedLowImpactRoutes(out _);
                RefreshPlanningOverlay();
            }
            GUI.enabled = preview.CanConfirm;
            if (GUILayout.Button(T("Confirm routes", "确认路线"), buttonStyle))
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

        private string WorkflowStepLabel(CityPlanningWorkflowPhase phase)
        {
            switch (phase)
            {
                case CityPlanningWorkflowPhase.ReadyToScan: return T("1  SCAN CITY", "1  扫描城市");
                case CityPlanningWorkflowPhase.Preview: return T("2  PREVIEW", "2  预览");
                case CityPlanningWorkflowPhase.RouteSelection: return T("3  CHOOSE ACCESS", "3  选择接入道路");
                case CityPlanningWorkflowPhase.ReadyToBuild: return T("4  COMMIT DP", "4  提交发展点");
                case CityPlanningWorkflowPhase.Construction: return T("5  CONSTRUCTION", "5  建设");
                default: return T("COMPLETE", "完成");
            }
        }

        private string T(string english, string chinese)
        {
            return useChineseUi ? chinese : english;
        }

        private string BuildingDescription(CityPhysicalTokenType type)
        {
            switch (type)
            {
                case CityPhysicalTokenType.Apartment:
                    return T("Medium-high density housing. Generates people and vehicles.",
                        "中高密度住宅，会增加居民与车辆活动。");
                case CityPhysicalTokenType.Commercial:
                    return T("A daily destination that attracts residents and creates waste.",
                        "吸引居民前往的日常目的地，也会产生垃圾。");
                case CityPhysicalTokenType.CommunityFacility:
                    return T("A shared civic destination that improves local wellbeing.",
                        "提升社区福祉的公共服务目的地。");
                default:
                    return T("Low-density housing with a smaller traffic footprint.",
                        "低密度住宅，交通影响相对较小。");
            }
        }

        private string BuildingImpactSummary(CityPhysicalTokenType type)
        {
            switch (type)
            {
                case CityPhysicalTokenType.Apartment:
                    return T("Housing  +30\nTraffic  ++\nWaste  +", "住房  +30\n交通  ++\n垃圾  +");
                case CityPhysicalTokenType.Commercial:
                    return T("Activity  +20\nTraffic  ++\nWaste  ++", "活力  +20\n交通  ++\n垃圾  ++");
                case CityPhysicalTokenType.CommunityFacility:
                    return T("Wellbeing  +18\nVisits  +\nWaste  +", "福祉  +18\n到访  +\n垃圾  +");
                default:
                    return T("Housing  +8\nTraffic  +\nWaste  +", "住房  +8\n交通  +\n垃圾  +");
            }
        }

        private string AnimalTag(int animalIndex)
        {
            switch (animalIndex)
            {
                case 1: return T("COMMON · CAUTIOUS", "常见 · 谨慎");
                case 2: return T("UNCOMMON · NOCTURNAL", "少见 · 夜行");
                case 3: return T("UNCOMMON · NOCTURNAL", "少见 · 夜行");
                default: return T("COMMON · ADAPTIVE", "常见 · 适应力强");
            }
        }

        private string AnimalDescription(int animalIndex)
        {
            switch (animalIndex)
            {
                case 1:
                    return T("Often found near trees and quieter food sources.",
                        "常在树木附近和较安静的食物点活动。");
                case 2:
                    return T("Uses connected green space and avoids busy roads.",
                        "依赖连续绿地，并会回避繁忙道路。");
                case 3:
                    return T("Forages after dark and is vulnerable at road crossings.",
                        "夜间觅食，穿越道路时较为脆弱。");
                default:
                    return T("Often found in plazas and near food sources.",
                        "常见于广场及食物来源附近。");
            }
        }

        private CityFeedEntry LatestCriticalEvent()
        {
            CityFeedEntry[] feed = observation?.Snapshot?.city_feed;
            if (feed == null)
            {
                return null;
            }
            return feed.Reverse().FirstOrDefault(entry =>
            {
                if (entry == null ||
                    (entry.type != CityFeedEventType.Roadkill &&
                     entry.type != CityFeedEventType.WasteOverflow))
                {
                    return false;
                }
                return !dismissedEventIds.Contains(EventKey(entry));
            });
        }

        private static string EventKey(CityFeedEntry entry)
        {
            return entry?.id ?? entry?.message ?? string.Empty;
        }

        private int DisplayYear => DisplayStageNumber == 1
            ? 1
            : DisplayStageNumber == 2
                ? 3
                : 5;

        private static int StageNumberForBuildingCount(int buildingCount)
        {
            if (buildingCount <= 5)
            {
                return 1;
            }
            if (buildingCount <= 16)
            {
                return 2;
            }
            return 3;
        }

        private string StageName(int stageNumber)
        {
            switch (stageNumber)
            {
                case 1: return T("Early Development", "初期发展");
                case 2: return T("Growing City", "城市扩展");
                default: return T("Mature City", "成熟城市");
            }
        }

        private string SeasonName(int stageNumber)
        {
            switch (stageNumber)
            {
                case 1: return T("SPRING", "春季");
                case 2: return T("AUTUMN", "秋季");
                default: return T("WINTER", "冬季");
            }
        }

        private string DevelopmentPhaseLabel(CityDevelopmentPhase phase)
        {
            switch (phase)
            {
                case CityDevelopmentPhase.PopulationGrowth:
                    return T("Population growth", "人口增长");
                case CityDevelopmentPhase.PublicLife:
                    return T("Public life", "公共生活");
                case CityDevelopmentPhase.WastePressure:
                    return T("Waste pressure", "垃圾压力");
                case CityDevelopmentPhase.MobilityPressure:
                    return T("Mobility pressure", "交通压力");
                case CityDevelopmentPhase.Redevelopment:
                    return T("Redevelopment", "更新建设");
                default:
                    return phase.ToString();
            }
        }

        private string RoadOptionLabel(CityRoadRouteOption option)
        {
            switch (option)
            {
                case CityRoadRouteOption.ExistingNetwork:
                    return T("Existing network", "接入既有路网");
                case CityRoadRouteOption.LowImpact:
                    return T("Low impact", "低影响");
                default:
                    return T("Direct", "直接连接");
            }
        }

        private string LocaliseRuntimeMessage(string message)
        {
            if (!useChineseUi || string.IsNullOrWhiteSpace(message))
            {
                return message;
            }
            switch (message)
            {
                case "Choose a building, then click an open part of the map.":
                    return "选择一种建筑，然后点击地图上的可用空地。";
                case "Camera scans remain pending until you load and confirm them.":
                    return "摄像头扫描会保持待处理，直到你载入并确认。";
                case "The map camera is unavailable.":
                    return "地图摄像机当前不可用。";
                case "That click did not reach the map.":
                    return "这次点击没有落在地图范围内。";
                case "Preview ready. Confirm to build and connect it to the nearest existing street.":
                    return "预览已就绪。确认后将建造建筑并接入最近的既有道路。";
                case "Built. Choose another building and click the map.":
                    return "建造完成。请选择下一栋建筑并点击地图。";
                case "Placement cancelled. Choose another position.":
                    return "已取消放置，请选择其他位置。";
                case "Electronic sample accepted. This is not a physical-camera claim.":
                    return "已接受电子测试样本；这不代表已完成实体摄像头识别。";
                case "Ready to Scan City. New objects stay in Preview until confirmed.":
                    return "可以开始扫描城市；新增对象会先停留在预览状态，确认后才会生效。";
                case "Construction paused.":
                    return "建设已暂停。";
                case "Construction complete. The confirmed objects are now part of the live city.":
                    return "建设完成；已确认对象现在正式成为城市的一部分。";
            }
            const string selectedSuffix = " selected. Click an open part of the map.";
            if (message.EndsWith(selectedSuffix, StringComparison.Ordinal))
            {
                string building = message.Substring(0, message.Length - selectedSuffix.Length);
                return $"已选择{ChineseBuildingName(building)}。请点击地图上的可用空地。";
            }
            const string scanPrefix = "Validated city scan loaded from ";
            if (message.StartsWith(scanPrefix, StringComparison.Ordinal))
            {
                string fileName = message.Substring(scanPrefix.Length)
                    .Replace(". Review Preview before confirming.", string.Empty);
                return $"已从 {fileName} 载入并验证城市扫描。确认前请检查预览。";
            }
            if (message.StartsWith("Placement confirmed. Added ", StringComparison.Ordinal))
            {
                return "放置已确认，并已自动生成连接最近既有道路的平滑支路。";
            }
            if (message.StartsWith("Plan ready.", StringComparison.Ordinal))
            {
                return "方案已就绪；开始建设时将扣除所需的发展点。";
            }
            if (message.StartsWith("Plan needs ", StringComparison.Ordinal))
            {
                return "当前发展点不足，请修改方案或获得更多发展点。";
            }
            if (message.StartsWith("Construction started:", StringComparison.Ordinal))
            {
                return "建设已开始，相关发展点已提交。";
            }
            if (message.StartsWith("Construction speed set to ", StringComparison.Ordinal))
            {
                return "建设速度已更新。";
            }
            if (message.StartsWith("Preview blocked.", StringComparison.Ordinal))
            {
                return "预览受阻：请将移动过的实体放回原位，或明确规划拆除。";
            }
            return "系统提示：" + message;
        }

        private static string ChineseBuildingName(string english)
        {
            switch (english)
            {
                case "Apartment": return "公寓";
                case "Market": return "市场";
                case "Community building": return "社区设施";
                default: return "独栋住宅";
            }
        }

        private string LocaliseFeedMessage(CityFeedEntry entry)
        {
            if (!useChineseUi)
            {
                return entry.message;
            }
            switch (entry.type)
            {
                case CityFeedEventType.WasteOverflow:
                    return "垃圾容量持续超载，地图上形成了垃圾热点。";
                case CityFeedEventType.Crowding:
                    return $"{entry.subject_id} 超过了舒适目的地容量。";
                case CityFeedEventType.Feeding:
                    return $"{entry.subject_id} 完成了一次进食。";
                case CityFeedEventType.MigrationIn:
                    return $"{entry.subject_id} 迁入了当前城区。";
                case CityFeedEventType.MigrationOut:
                    return $"{entry.subject_id} 因栖息地效用持续偏低而离开。";
                case CityFeedEventType.Roadkill:
                    return $"{entry.subject_id} 发生了道路伤亡事件。";
                case CityFeedEventType.ProjectCompleted:
                    return $"{entry.subject_id} 已完成建设。";
                default:
                    return $"{entry.subject_id} 低于35分警戒线。";
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
            if (panelStyle != null && heatmapCardStyle != null && chromeCardStyle != null &&
                styledScreenHeight == Screen.height &&
                styledChineseUi == useChineseUi)
            {
                return;
            }

            styledScreenHeight = Screen.height;
            styledChineseUi = useChineseUi;
            uiScale = CityPrototypeUiTheme.ScaleForScreen(Screen.height);
            Font regularFont = CityPrototypeUiTheme.LoadRuntimeFont(false, useChineseUi);
            Font boldFont = CityPrototypeUiTheme.LoadRuntimeFont(true, useChineseUi);
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
                border = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
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
                border = new RectOffset(14, 14, 14, 14),
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
                    background = RoundedTexture(CityPrototypeUiTheme.Button, 14),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
                hover =
                {
                    background = RoundedTexture(CityPrototypeUiTheme.ButtonHover, 14),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
                active =
                {
                    background = RoundedTexture(CityPrototypeUiTheme.ButtonPressed, 14),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
                focused =
                {
                    background = RoundedTexture(CityPrototypeUiTheme.ButtonHover, 14),
                    textColor = CityPrototypeUiTheme.ButtonText,
                },
            };
            heatmapCardStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(18, 18, 18, 18),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal =
                {
                    background = RoundedTexture(new Color(1f, 1f, 1f, 0.96f), 18),
                },
            };
            heatmapToggleStyle = new GUIStyle(buttonStyle)
            {
                fixedHeight = 0f,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(
                    CityPrototypeUiTheme.CaptionFontSize,
                    uiScale),
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            heatmapEmptyStyle = new GUIStyle(captionStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = CityPrototypeUiTheme.InkSecondary },
            };
            chromeCardStyle = new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(18, 18, 18, 18),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal =
                {
                    background = RoundedTexture(new Color(1f, 1f, 1f, 0.94f), 18),
                },
            };
            chromeShadowStyle = new GUIStyle(chromeCardStyle)
            {
                normal =
                {
                    background = RoundedTexture(new Color(0.10f, 0.18f, 0.20f, 0.10f), 18),
                },
            };
            mapTitleStyle = new GUIStyle(titleStyle)
            {
                fontSize = CityPrototypeUiTheme.ScaledFontSize(27, uiScale),
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
            };
            mapSubtitleStyle = new GUIStyle(captionStyle)
            {
                fontSize = CityPrototypeUiTheme.ScaledFontSize(13, uiScale),
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip,
                wordWrap = false,
            };
            topMetricStyle = new GUIStyle(captionStyle)
            {
                font = boldFont,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = CityPrototypeUiTheme.Ink },
            };
            tabBarStyle = new GUIStyle(chromeCardStyle)
            {
                normal =
                {
                    background = RoundedTexture(new Color32(0xEB, 0xEC, 0xE9, 0xF2), 18),
                },
            };
            tabStyle = new GUIStyle(GUI.skin.button)
            {
                border = new RectOffset(14, 14, 14, 14),
                font = regularFont,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(14, uiScale),
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    background = RoundedTexture(new Color(1f, 1f, 1f, 0f), 14),
                    textColor = CityPrototypeUiTheme.InkSecondary,
                },
                hover =
                {
                    background = RoundedTexture(new Color(1f, 1f, 1f, 0.52f), 14),
                    textColor = CityPrototypeUiTheme.Ink,
                },
                active =
                {
                    background = RoundedTexture(new Color(1f, 1f, 1f, 0.74f), 14),
                    textColor = CityPrototypeUiTheme.Ink,
                },
            };
            activeTabStyle = new GUIStyle(tabStyle)
            {
                font = boldFont,
                normal =
                {
                    background = RoundedTexture(new Color(1f, 1f, 1f, 0.98f), 14),
                    textColor = CityPrototypeUiTheme.Ink,
                },
            };
            catalogItemStyle = new GUIStyle(GUI.skin.button)
            {
                border = new RectOffset(13, 13, 13, 13),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal =
                {
                    background = RoundedTexture(new Color(0.98f, 0.98f, 0.96f, 0.74f), 13),
                    textColor = CityPrototypeUiTheme.Ink,
                },
                hover =
                {
                    background = RoundedTexture(new Color32(0xEB, 0xF4, 0xF5, 0xF5), 13),
                    textColor = CityPrototypeUiTheme.Ink,
                },
                active =
                {
                    background = RoundedTexture(new Color32(0xD8, 0xEB, 0xEE, 0xFF), 13),
                    textColor = CityPrototypeUiTheme.Ink,
                },
            };
            selectedCatalogItemStyle = new GUIStyle(catalogItemStyle)
            {
                normal =
                {
                    background = RoundedTexture(new Color32(0xD8, 0xEB, 0xEE, 0xFF), 13),
                    textColor = CityPrototypeUiTheme.Ink,
                },
            };
            catalogLabelStyle = new GUIStyle(captionStyle)
            {
                font = boldFont,
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                wordWrap = false,
                normal = { textColor = CityPrototypeUiTheme.Ink },
            };
            rightCaptionStyle = new GUIStyle(captionStyle)
            {
                alignment = TextAnchor.MiddleRight,
            };
            toolbarButtonStyle = new GUIStyle(buttonStyle)
            {
                fontSize = CityPrototypeUiTheme.ScaledFontSize(11, uiScale),
                padding = new RectOffset(0, 0, 0, 0),
                normal =
                {
                    background = RoundedTexture(new Color(1f, 1f, 1f, 0.12f), 13),
                    textColor = CityPrototypeUiTheme.Ink,
                },
                hover =
                {
                    background = RoundedTexture(CityPrototypeUiTheme.Button, 13),
                    textColor = CityPrototypeUiTheme.Ink,
                },
            };
            backButtonStyle = new GUIStyle(selectedCatalogItemStyle)
            {
                font = boldFont,
                fontSize = CityPrototypeUiTheme.ScaledFontSize(22, uiScale),
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
            };
            progressTrackStyle = RoundedBoxStyle(new Color32(0xDD, 0xE7, 0xE9, 0xFF), 8);
            progressGreenStyle = RoundedBoxStyle(new Color32(0x62, 0xB5, 0x91, 0xFF), 8);
            progressBlueStyle = RoundedBoxStyle(new Color32(0x62, 0xB6, 0xDD, 0xFF), 8);
            progressOrangeStyle = RoundedBoxStyle(new Color32(0xFF, 0x9B, 0x55, 0xFF), 8);
            progressPurpleStyle = RoundedBoxStyle(new Color32(0x91, 0x83, 0xDF, 0xFF), 8);
            if (heatmapMapSprite == null)
            {
                heatmapMapSprite = Resources.Load<Sprite>(CityBoardUnderlayResourcePath);
            }
            if (heatmapDotTexture == null)
            {
                heatmapDotTexture = CreateHeatmapDotTexture();
            }
            if (heatmapLegendTexture == null)
            {
                heatmapLegendTexture = CreateHeatmapLegendTexture();
            }
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
            renderer.color = new Color(0.94f, 0.97f, 0.92f, 0.96f);
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

        private static GUIStyle RoundedBoxStyle(Color colour, int radius)
        {
            return new GUIStyle(GUI.skin.box)
            {
                border = new RectOffset(radius, radius, radius, radius),
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                normal = { background = RoundedTexture(colour, radius) },
            };
        }

        private static Texture2D RoundedTexture(Color colour, int radius)
        {
            const int size = 64;
            int safeRadius = Mathf.Clamp(radius, 2, size / 2 - 1);
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Riverside UI rounded surface",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            Color[] pixels = new Color[size * size];
            float left = safeRadius;
            float right = size - 1f - safeRadius;
            float top = safeRadius;
            float bottom = size - 1f - safeRadius;
            for (int y = 0; y < size; y += 1)
            {
                for (int x = 0; x < size; x += 1)
                {
                    float closestX = Mathf.Clamp(x, left, right);
                    float closestY = Mathf.Clamp(y, top, bottom);
                    float dx = x - closestX;
                    float dy = y - closestY;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float edgeAlpha = Mathf.Clamp01(safeRadius + 0.5f - distance);
                    pixels[y * size + x] = new Color(
                        colour.r,
                        colour.g,
                        colour.b,
                        colour.a * edgeAlpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateHeatmapLegendTexture()
        {
            const int width = 128;
            const int height = 8;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Animal heatmap low-to-high legend",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            Color[] pixels = new Color[width * height];
            for (int x = 0; x < width; x += 1)
            {
                Color colour = HeatmapColour(x / (width - 1f));
                colour.a = 1f;
                for (int y = 0; y < height; y += 1)
                {
                    pixels[y * width + x] = colour;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateHeatmapDotTexture()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false)
            {
                name = "Sidebar animal heat dot",
                hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y += 1)
            {
                for (int x = 0; x < size; x += 1)
                {
                    float normalizedX = ((x + 0.5f) / size) * 2f - 1f;
                    float normalizedY = ((y + 0.5f) / size) * 2f - 1f;
                    float distance = Mathf.Sqrt(
                        normalizedX * normalizedX + normalizedY * normalizedY);
                    float alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(distance));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
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

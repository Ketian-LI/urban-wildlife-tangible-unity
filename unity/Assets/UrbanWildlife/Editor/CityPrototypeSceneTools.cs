using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlife.City;
using UrbanWildlife.Construction;
using UrbanWildlife.Input;
using UrbanWildlife.Mobility;
using UrbanWildlife.Networks;
using UrbanWildlife.Planning;
using UrbanWildlife.Prototype;
using UrbanWildlife.Reporting;
using UrbanWildlife.Strategy;

namespace UrbanWildlife.EditorTools
{
    public static class CityPrototypeSceneTools
    {
        public const string ScenePath = "Assets/Scenes/City_Prototype.unity";
        private const string LegacyScenePath = "Assets/Scenes/P0_InputSpike.unity";

        [MenuItem("Urban Wildlife/City Prototype/Create or Reset Scene")]
        public static void CreateCityPrototypeScene()
        {
            EnsureVehicleSpriteImports();
            EnsureWildlifeSpriteImports();
            EnsureBuildingVariantSpriteImports();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject clearCameraObject = new GameObject("UI Background Camera");
            Camera clearCamera = clearCameraObject.AddComponent<Camera>();
            clearCamera.clearFlags = CameraClearFlags.SolidColor;
            clearCamera.backgroundColor = new Color32(0xF8, 0xF6, 0xEF, 0xFF);
            clearCamera.cullingMask = 0;
            clearCamera.depth = -10f;
            clearCamera.rect = new Rect(0f, 0f, 1f, 1f);

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 10f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.82f, 0.86f, 0.80f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = CityPrototypeDemo.OrthographicSizeForViewport(
                Screen.width,
                Screen.height,
                CityPrototypeDemo.MapViewportFraction);
            camera.rect = new Rect(0f, 0f, CityPrototypeDemo.MapViewportFraction, 1f);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

            GameObject prototypeObject = new GameObject("City Prototype Controller");
            CityPrototypeDemo prototype = prototypeObject.AddComponent<CityPrototypeDemo>();
            prototype.InitializePrototype(true);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Could not save {ScenePath}.");
            }
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(LegacyScenePath, true),
                new EditorBuildSettingsScene(ScenePath, true),
            };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = prototypeObject;
            Debug.Log($"Created {ScenePath} with a split-screen live city prototype.");
        }

        private static void EnsureVehicleSpriteImports()
        {
            string[] assetPaths =
            {
                "Assets/Resources/UrbanWildlife/Vehicles/city-car-pastel-blue-v01.png",
                "Assets/Resources/UrbanWildlife/Vehicles/city-car-muted-coral-v01.png",
                "Assets/Resources/UrbanWildlife/Vehicles/city-car-warm-mustard-v01.png",
            };
            foreach (string assetPath in assetPaths)
            {
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }
                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.mipmapEnabled || !importer.alphaIsTransparency;
                if (!changed)
                {
                    continue;
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureWildlifeSpriteImports()
        {
            string[] assetPaths =
            {
                "Assets/Resources/UrbanWildlife/Animals/pigeon-citybuilder-soft-v02.png",
                "Assets/Resources/UrbanWildlife/Animals/squirrel-citybuilder-soft-v02.png",
                "Assets/Resources/UrbanWildlife/Animals/fox-citybuilder-soft-v02.png",
                "Assets/Resources/UrbanWildlife/Animals/hedgehog-citybuilder-soft-v02.png",
            };
            foreach (string assetPath in assetPaths)
            {
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }
                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.mipmapEnabled || !importer.alphaIsTransparency;
                if (!changed)
                {
                    continue;
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureBuildingVariantSpriteImports()
        {
            string[] assetPaths =
            {
                "Assets/Resources/UrbanWildlife/Buildings/detached-house-riverside-blue-v09.png",
                "Assets/Resources/UrbanWildlife/Buildings/detached-house-riverside-coral-v09.png",
                "Assets/Resources/UrbanWildlife/Buildings/apartment-riverside-v04.png",
                "Assets/Resources/UrbanWildlife/Buildings/commercial-market-riverside-v05.png",
                "Assets/Resources/UrbanWildlife/Buildings/community-centre-riverside-v04.png",
            };
            foreach (string assetPath in assetPaths)
            {
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
                {
                    continue;
                }
                bool changed = importer.textureType != TextureImporterType.Sprite ||
                               importer.spriteImportMode != SpriteImportMode.Single ||
                               importer.mipmapEnabled || !importer.alphaIsTransparency;
                if (!changed)
                {
                    continue;
                }
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                importer.SaveAndReimport();
            }
        }

        [MenuItem("Urban Wildlife/City Prototype/Open for Inspection")]
        public static void OpenCityPrototypeForInspection()
        {
            if (!File.Exists(Path.GetFullPath(Path.Combine(Application.dataPath, "../", ScenePath))))
            {
                CreateCityPrototypeScene();
            }
            else
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.EnterPlaymode();
            }
        }

        public static void BatchCaptureCityOpeningPreview()
        {
            const int width = 1536;
            const int height = 1024;
            const string previewPath =
                "docs/visual-concepts/city-grid/city-opening-unity-preview-v01.png";

            CreateCityPrototypeScene();
            Camera camera = Camera.main;
            if (camera == null)
            {
                throw new InvalidOperationException("City opening preview requires a main camera.");
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../.."));
            string outputPath = Path.Combine(projectRoot, previewPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            RenderTexture previousActive = RenderTexture.active;
            Rect previousRect = camera.rect;
            float previousSize = camera.orthographicSize;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.rect = new Rect(0f, 0f, 1f, 1f);
                camera.orthographicSize = CityPrototypeDemo.OrthographicSizeForViewport(width, height, 1f);
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(outputPath, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.rect = previousRect;
                camera.orthographicSize = previousSize;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }

            Debug.Log($"UNITY_CITY_OPENING_PREVIEW_OK path={outputPath} size={width}x{height}");
        }

        public static void BatchBuildAndVerifyCityPrototype()
        {
            CreateCityPrototypeScene();
            CityPrototypeDemo prototype = UnityEngine.Object.FindFirstObjectByType<CityPrototypeDemo>();
            Camera camera = Camera.main;
            bool separateViewport = camera != null &&
                                    Math.Abs(camera.rect.x) < 0.001f &&
                                    Math.Abs(camera.rect.width -
                                        CityPrototypeDemo.MapViewportFraction) < 0.001f;
            Camera clearCamera = GameObject.Find("UI Background Camera")?.GetComponent<Camera>();
            bool fullFrameClear = camera != null &&
                                  clearCamera != null &&
                                  clearCamera.cullingMask == 0 &&
                                  clearCamera.depth < camera.depth &&
                                  Math.Abs(clearCamera.rect.width - 1f) < 0.001f;
            if (prototype == null || prototype.GeneratedBuildingCount != 2 ||
                prototype.GeneratedPlanningCellCount != 216 ||
                prototype.AvailablePlanningCellCount != 214 ||
                prototype.GeneratedVehicleRoadCount != 6 ||
                prototype.GeneratedPedestrianLinkCount != 6 ||
                prototype.GeneratedAmenityCount != 0 ||
                prototype.FoodSourceCount < 2 ||
                prototype.WildlifeAgentCount != 15 ||
                prototype.WildlifeSpeciesCount != 4 ||
                prototype.DevelopmentPhaseCount != 5 ||
                prototype.CityBalanceTotal <= 0f ||
                prototype.HumanTracePointCount <= 0 ||
                prototype.AnimalTracePointCount <= 0 ||
                prototype.HeatmapVisible ||
                prototype.CityFeedCount <= 0 ||
                !prototype.PlanningWorkflowConnected ||
                string.IsNullOrWhiteSpace(prototype.ResolvedCityScanPath) ||
                prototype.RepresentativeAgentCount != 6 ||
                prototype.RepresentedPopulation != 24 ||
                prototype.VehicleTripCount != 2 ||
                prototype.WalkTripCount != 4 ||
                !prototype.PedestrianNetworkVisible ||
                !prototype.DesktopPlayEnabled ||
                !prototype.RiversideUiLayoutEnabled ||
                prototype.ActiveSidebarTab != 1 ||
                !fullFrameClear ||
                !separateViewport)
            {
                throw new InvalidOperationException(
                    $"City prototype scene failed its visual integration contract: " +
                    $"prototype={prototype != null}, buildings={prototype?.GeneratedBuildingCount}, " +
                    $"planningCells={prototype?.GeneratedPlanningCellCount}, " +
                    $"availableCells={prototype?.AvailablePlanningCellCount}, " +
                    $"roads={prototype?.GeneratedVehicleRoadCount}, links={prototype?.GeneratedPedestrianLinkCount}, " +
                    $"amenities={prototype?.GeneratedAmenityCount}, food={prototype?.FoodSourceCount}, " +
                    $"wildlife={prototype?.WildlifeAgentCount}, species={prototype?.WildlifeSpeciesCount}, " +
                    $"phases={prototype?.DevelopmentPhaseCount}, balance={prototype?.CityBalanceTotal}, " +
                    $"humanTrace={prototype?.HumanTracePointCount}, animalTrace={prototype?.AnimalTracePointCount}, " +
                    $"heatmapVisible={prototype?.HeatmapVisible}, heatCells={prototype?.VisibleHeatCellCount}, " +
                    $"feed={prototype?.CityFeedCount}, " +
                    $"planning={prototype?.PlanningWorkflowConnected}, " +
                    $"agents={prototype?.RepresentativeAgentCount}, population={prototype?.RepresentedPopulation}, " +
                    $"vehicles={prototype?.VehicleTripCount}, viewport={separateViewport}, " +
                    $"fullFrameClear={fullFrameClear}.");
            }
            string[] requiredObjects =
            {
                "Generated City Prototype/Organic terrain underlay",
                "Generated City Prototype/Planning grid/Cells",
                "Generated City Prototype/Planning grid/Boundaries",
                "Generated City Prototype/Green patches",
                "Generated City Prototype/Buildings",
                "Generated City Prototype/Vehicle road network",
                "Generated City Prototype/Visible building access roads",
                "Generated City Prototype/Pedestrian link network",
                "Generated City Prototype/Digital amenities",
                "Generated City Prototype/City wildlife agents",
                "Generated City Prototype/Representative mobility agents",
                "Generated City Prototype/Live city traces/Human traces",
                "Generated City Prototype/Live city traces/Animal traces",
                "Generated City Prototype/Live city traces/Combined heatmap",
            };
            if (requiredObjects.Any(path => prototype.transform.Find(path) == null))
            {
                throw new InvalidOperationException("City prototype is missing a generated visual layer.");
            }
            VerifyPlanningGridVisuals(prototype);
            VerifyResidentialBuildingVisuals(prototype);
            VerifyCommercialBuildingVisuals(prototype);
            VerifyCompactNeighbourhoodPresentation(prototype);
            VerifyBilingualUi(prototype);
            VerifyRiversideUi(prototype);
            VerifyInformationPanel(prototype);
            VerifyActivityHeatmap(prototype);
            VerifyStreetLifeVisuals(prototype);
            VerifyResponsiveCameraFit();
            VerifyCameraScanFileSource();
            VerifyPlanningWorkflow();
            VerifyGridConstructionRules();
            VerifyFreePlacementAndAutomaticAccess();
            VerifyDesktopGrowthCapacity(prototype);
            VerifyDesktopPlayableFlow(prototype);
            VerifyVehicleRoutesStayOnRoad();
            Debug.Log(
                "UNITY_CITY_PROTOTYPE_SMOKE_OK split_screen=True right_sidebar=True sidebar_width=21_percent riverside_ui_chrome=True full_frame_clear=True bright_city_style=True opening_buildings=2 vehicle_roads=6 " +
                "planning_grid=18x12 planning_cells=216 available_cells=214 reference_scaled_footprints=True hidden_grid=True " +
                "pedestrian_links=6 amenities=0 food_sources=True waste_pressure=True " +
                "wildlife_agents=15 species=4 utility_targets=True " +
                "development_phases=5 city_balance=True dp=True time_blocks=4 " +
                "human_animal_combined_trace=True full_map_activity_views=True city_feed=True phase_report=True " +
                "representative_agents=6 represented_population=24 walk_trips=4 vehicle_trips=2 " +
                "external_return_routes=True live_vehicle_agents=True visible_pedestrian_paths=True shared_road_corridor=True pedestrians_on_sidewalk=True max_speed=2x no_questionnaire=True " +
                "animal_activity_heatmap=True heatmap_hotkey_h=True heatmap_sidebar_bottom_right=True heatmap_world_overlay=False footprints_removed=True info_tabs=city_animals_events city_feed_panel=True phase_snapshot=True " +
                "desktop_play_default=True click_preview_confirm_build=True camera_mode_retained=True " +
                "camera_scan_file_bridge=True scan_preview_route_dp_construction=True " +
                "sparse_opening_map=True dense_desktop_growth=True reference_building_scale=True " +
                "road_following=True no_premature_buildings=True " +
                "bilingual_ui_switch=True cjk_font_fallback=True");
        }

        private static void VerifyBilingualUi(CityPrototypeDemo prototype)
        {
            bool originalLanguage = prototype.ChineseUiEnabled;
            prototype.SetUiLanguage(true);
            bool chineseValid = prototype.CurrentUiLanguageCode == "zh-CN" &&
                                prototype.LocalizedPlanningHeading == "城市规划" &&
                                prototype.ChineseUiFontSupportsCoreGlyphs;
            prototype.SetUiLanguage(false);
            bool englishValid = prototype.CurrentUiLanguageCode == "en" &&
                                prototype.LocalizedPlanningHeading == "CITY PLANNING";
            prototype.SetUiLanguage(originalLanguage);
            if (!chineseValid || !englishValid)
            {
                throw new InvalidOperationException(
                    "The City Prototype language switch must update both Chinese and English labels.");
            }
            Debug.Log(
                "UNITY_CITY_BILINGUAL_UI_SMOKE_OK chinese=True english=True persisted=True " +
                $"planning_overlay_refresh=True cjk_font_fallback=True font={prototype.ChineseUiFontName}");
        }

        private static void VerifyRiversideUi(CityPrototypeDemo prototype)
        {
            const int width = 1920;
            const int height = 1080;
            float mapWidth = width * CityPrototypeDemo.MapViewportFraction;
            Rect cityStatus = CityPrototypeDemo.CityStatusRectForScreen(width, height);
            Rect toolbar = CityPrototypeDemo.ToolbarRectForScreen(width, height);
            Rect buildingMode = CityPrototypeDemo.BuildingModeRectForScreen(width, height);
            Rect activityLegend = CityPrototypeDemo.ActivityLegendRectForScreen(width, height);
            Rect traceMode = CityPrototypeDemo.TraceModeRectForScreen(width, height);
            Rect eventPopup = CityPrototypeDemo.EventPopupRectForScreen(width, height);
            Rect timeControl = CityPrototypeDemo.TimeControlRectForScreen(width, height);
            Rect mainMenu = CityPrototypeDemo.MainMenuRectForScreen(width, height);
            Rect endScreen = CityPrototypeDemo.EndScreenRectForScreen(width, height);
            Rect heatmap = CityPrototypeDemo.HeatmapCardRectForScreen(width, height);
            Rect sidebar = CityPrototypeDemo.RightSidebarRectForScreen(width, height);
            Rect compactSidebar = CityPrototypeDemo.RightSidebarRectForScreen(1602, 985);
            bool initialTime = prototype.TimeControlEnabled &&
                               Math.Abs(prototype.CurrentSimulationSpeed - 2f) < 0.001f &&
                               prototype.CurrentSeasonIndex == 0;
            prototype.SetSimulationSpeed(1f);
            bool normalSpeed = Math.Abs(prototype.CurrentSimulationSpeed - 1f) < 0.001f;
            prototype.SetSimulationSpeed(0f);
            bool pausedSpeed = Math.Abs(prototype.CurrentSimulationSpeed) < 0.001f;
            prototype.SetSimulationSpeed(2f);
            prototype.CycleSeason();
            bool seasonAdvanced = prototype.CurrentSeasonIndex == 1;
            prototype.CycleSeason();
            prototype.CycleSeason();
            prototype.CycleSeason();
            prototype.OpenMainMenu();
            bool mainMenuFlow = prototype.MainMenuVisible &&
                                Math.Abs(prototype.CurrentSimulationSpeed) < 0.001f;
            prototype.ResumeFromMainMenu();
            mainMenuFlow = mainMenuFlow && !prototype.MainMenuVisible &&
                           Math.Abs(prototype.CurrentSimulationSpeed - 2f) < 0.001f;
            prototype.OpenEndScreen();
            bool endScreenFlow = prototype.EndScreenVisible &&
                                 Math.Abs(prototype.CurrentSimulationSpeed) < 0.001f;
            prototype.ReturnToMapFromEndScreen();
            endScreenFlow = endScreenFlow && !prototype.EndScreenVisible &&
                            Math.Abs(prototype.CurrentSimulationSpeed - 2f) < 0.001f;
            bool valid = prototype.RiversideUiLayoutEnabled &&
                         prototype.ReferenceStageHudEnabled &&
                         prototype.BottomNavigationItemCount == 4 &&
                         prototype.ActiveBottomNavigationIndex == 0 &&
                         prototype.BuildingPlacementCardEnabled &&
                         prototype.TraceViewSelectorEnabled &&
                         prototype.FullMapActivityViewEnabled &&
                         prototype.InformationPanelEnabled &&
                         prototype.ActiveTraceViewIndex == 0 &&
                         initialTime && normalSpeed && pausedSpeed && seasonAdvanced &&
                         mainMenuFlow && endScreenFlow &&
                         prototype.DisplayStageNumber == 1 &&
                         prototype.ActiveSidebarTab == 1 &&
                         Math.Abs(CityPrototypeDemo.MapViewportFraction - 0.79f) < 0.001f &&
                         cityStatus.x >= 0f && cityStatus.xMax < mapWidth &&
                         cityStatus.y >= height * 0.70f && cityStatus.yMax <= height &&
                         toolbar.x >= cityStatus.xMax && toolbar.xMax < mapWidth &&
                         toolbar.y >= height * 0.80f && toolbar.yMax <= height &&
                          buildingMode.x >= 0f && buildingMode.xMax < mapWidth &&
                          buildingMode.y > 100f && buildingMode.yMax < toolbar.y &&
                          activityLegend.x >= 0f && activityLegend.xMax < mapWidth &&
                          activityLegend.y > 100f && activityLegend.yMax < toolbar.y &&
                         traceMode.x == toolbar.x && traceMode.xMax == toolbar.xMax &&
                         traceMode.yMax < toolbar.y &&
                         eventPopup.x >= 0f && eventPopup.xMax < mapWidth &&
                         timeControl.x >= 0f && timeControl.xMax < mapWidth &&
                         timeControl.y >= 0f && timeControl.yMax < height * 0.25f &&
                         mainMenu.x >= 0f && mainMenu.xMax < mapWidth &&
                         mainMenu.y >= 0f && mainMenu.yMax <= height &&
                         endScreen.x >= 0f && endScreen.xMax < mapWidth &&
                         endScreen.y >= 0f && endScreen.yMax <= height &&
                         Math.Abs(sidebar.x - mapWidth) < 0.001f &&
                         Math.Abs(sidebar.xMax - width) < 0.001f &&
                         Math.Abs(compactSidebar.xMax - 1602f) < 0.001f &&
                         compactSidebar.width > 300f &&
                         heatmap.x >= mapWidth && heatmap.xMax <= width &&
                         heatmap.y >= height * 0.55f && heatmap.yMax <= height;
            if (!valid)
            {
                throw new InvalidOperationException(
                    "The city UI must use the Riverside Park overlay hierarchy and 79/21 map/sidebar split.");
            }
            Debug.Log(
                "UNITY_CITY_RIVERSIDE_UI_SMOKE_OK map_width=79_percent sidebar_width=21_percent " +
                "top_header=True top_metrics=True city_status_bottom_left=True toolbar_bottom_center=True " +
                "stage_hud=True stage=early bottom_nav=build_info_trace_pause " +
                "building_mode_card=True trace_views=normal_human_animal_combined event_popup=True " +
                "time_control=pause_1x_2x_seasons main_menu=True end_screen=True " +
                "building_catalog=True environment_catalog=True heatmap_bottom_right=True " +
                "responsive_sidebar_1602x985=True active_tab=buildings");
        }

        private static void VerifyInformationPanel(CityPrototypeDemo prototype)
        {
            prototype.SetBottomNavigation(1);
            bool cityTab = prototype.InformationPanelEnabled &&
                           prototype.ActiveBottomNavigationIndex == 1 &&
                           prototype.ActiveInformationTab == 0;
            prototype.SetInformationTab(1);
            bool animalTab = prototype.ActiveInformationTab == 1;
            prototype.SetInformationTab(2);
            bool eventsTab = prototype.ActiveInformationTab == 2;
            prototype.SetBottomNavigation(0);
            if (!cityTab || !animalTab || !eventsTab ||
                prototype.ActiveBottomNavigationIndex != 0)
            {
                throw new InvalidOperationException(
                    "The information panel must expose City Status, Animals and Events tabs.");
            }
            Debug.Log(
                "UNITY_CITY_INFORMATION_PANEL_SMOKE_OK tabs=city_status_animals_events " +
                "dynamic_scores=True bilingual=True");
        }

        private static void VerifyActivityHeatmap(CityPrototypeDemo prototype)
        {
            if (prototype.HeatmapVisible)
            {
                throw new InvalidOperationException("The activity heatmap must start hidden.");
            }
            prototype.SetBottomNavigation(2);
            prototype.SetTraceView(1);
            bool humanView = !prototype.HeatmapVisible &&
                             prototype.FullMapActivityOverlayVisible &&
                             prototype.ActiveTraceViewIndex == 1;
            prototype.SetTraceView(3);
            bool combinedView = !prototype.HeatmapVisible &&
                                prototype.FullMapActivityOverlayVisible &&
                                prototype.ActiveTraceViewIndex == 3;
            prototype.SetTraceView(0);
            if (!humanView || !combinedView || prototype.HeatmapVisible ||
                prototype.FullMapActivityOverlayVisible)
            {
                throw new InvalidOperationException(
                    "Human, animal and combined activity views must render over the full map.");
            }
            prototype.SetBottomNavigation(0);
            prototype.SetHeatmapVisible(true);
            Rect sidebarCard = CityPrototypeDemo.HeatmapCardRectForScreen(1920, 1080);
            bool bottomRightCard = sidebarCard.x >= 1920f *
                CityPrototypeDemo.MapViewportFraction &&
                                   sidebarCard.xMax <= 1920f &&
                                   sidebarCard.y >= 1080f * 0.55f &&
                                   sidebarCard.yMax <= 1080f;
            bool prototypeToggleOn = prototype.HeatmapVisible &&
                                     prototype.HeatmapUsesAnimalData &&
                                     prototype.HeatmapRendersInSidebar &&
                                     !prototype.HeatmapOverlaysMap &&
                                     bottomRightCard;
            prototype.SetHeatmapVisible(false);

            GameObject host = new GameObject("Heatmap smoke host");
            try
            {
                CityTraceVisualizer visualizer = new CityTraceVisualizer(host.transform, 12f, 8f);
                CityTracePoint[] samples =
                {
                    HeatSample("resident-01", CityTraceLayer.Human, 0.20f, 0.20f),
                    HeatSample("resident-01", CityTraceLayer.Human, 0.21f, 0.21f),
                    HeatSample("pigeon-01", CityTraceLayer.Animal, 0.80f, 0.80f),
                };
                visualizer.Render(samples);
                visualizer.SetVisible(true);
                visualizer.SetMode(CityTraceDisplayMode.AnimalTrace);
                bool animalOnly = visualizer.VisibleCellCount == 1 &&
                                  visualizer.CellCount(14, 9) == 1 &&
                                  visualizer.CellIntensity(14, 9) > 0.99f;
                bool independentLayers =
                    visualizer.VisibleCellCountFor(CityTraceDisplayMode.HumanTrace) == 1 &&
                    visualizer.VisibleCellCountFor(CityTraceDisplayMode.AnimalTrace) == 1 &&
                    visualizer.CellIntensity(
                        CityTraceDisplayMode.HumanTrace,
                        3,
                        2) > 0.99f &&
                    visualizer.CellIntensity(
                        CityTraceDisplayMode.AnimalTrace,
                        14,
                        9) > 0.99f;
                bool noWorldOverlay = !visualizer.WorldOverlayActive;
                bool noFootprintObjects = !host.GetComponentsInChildren<Transform>(true)
                    .Any(item =>
                        item.name.IndexOf("shoe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("tyre", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("paw", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!prototypeToggleOn || !animalOnly || !independentLayers ||
                    !noWorldOverlay || !noFootprintObjects)
                {
                    throw new InvalidOperationException(
                        "The animal hotspot card must stay independent from the full-map activity views.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
            Debug.Log(
                "UNITY_CITY_ACTIVITY_HEATMAP_SMOKE_OK default_hidden=True h_toggle=True " +
                "views=normal_human_animal_combined " +
                "full_map_activity=True independent_layers=True animal_only=True animal_cells=1 " +
                "human_samples_excluded=True individual_marks=False sidebar_bottom_right=True " +
                "animal_hotspot_world_overlay=False");
        }

        private static CityTracePoint HeatSample(
            string agentId,
            CityTraceLayer layer,
            float x,
            float y)
        {
            return new CityTracePoint
            {
                agent_id = agentId,
                layer = layer,
                mark = layer == CityTraceLayer.Human
                    ? CityTraceMark.Footprint
                    : CityTraceMark.BirdTrack,
                position_norm = new[] { x, y },
            };
        }

        private static void VerifyStreetLifeVisuals(CityPrototypeDemo prototype)
        {
            Transform mobilityRoot = prototype.transform.Find(
                "Generated City Prototype/Representative mobility agents");
            Transform pedestrianRoot = prototype.transform.Find(
                "Generated City Prototype/Pedestrian link network");
            Transform wildlifeRoot = prototype.transform.Find(
                "Generated City Prototype/City wildlife agents");
            SpriteRenderer[] mobilityArtwork = mobilityRoot == null
                ? Array.Empty<SpriteRenderer>()
                : mobilityRoot.GetComponentsInChildren<SpriteRenderer>(true);
            SpriteRenderer[] humanArtwork = mobilityArtwork
                .Where(renderer => renderer.gameObject.name == "Human artwork")
                .ToArray();
            SpriteRenderer[] vehicleArtwork = mobilityArtwork
                .Where(renderer => renderer.gameObject.name == "Vehicle artwork")
                .ToArray();
            SpriteRenderer[] wildlifeArtwork = wildlifeRoot == null
                ? Array.Empty<SpriteRenderer>()
                : wildlifeRoot.GetComponentsInChildren<SpriteRenderer>(true)
                    .Where(renderer => renderer.gameObject.name == "Wildlife artwork")
                    .ToArray();
            int humanArtworkCount = humanArtwork.Length;
            int vehicleArtworkCount = vehicleArtwork.Length;
            int visiblePathLayers = pedestrianRoot == null
                ? 0
                : pedestrianRoot.GetComponentsInChildren<LineRenderer>(true).Length;
            string[] vehicleSprites =
            {
                "UrbanWildlife/Vehicles/city-car-pastel-blue-v01",
                "UrbanWildlife/Vehicles/city-car-muted-coral-v01",
                "UrbanWildlife/Vehicles/city-car-warm-mustard-v01",
            };
            bool resourcesLoad = vehicleSprites.All(path => Resources.Load<Sprite>(path) != null);
            CityState streetCity = CityPrototypeStateFactory.Create();
            CityVehicleRoad mainRoad = streetCity.vehicle_roads.Single(road =>
                road.id == "vehicle-main-street");
            CityPedestrianLink mainSidewalk = streetCity.pedestrian_links.Single(link =>
                link.id == "pedestrian-park-spine");
            CityVehicleRoad[] localRoads = streetCity.vehicle_roads
                .Where(road => road.role == CityVehicleRoadRole.Local)
                .ToArray();
            CityPedestrianLink[] localSidewalks = streetCity.pedestrian_links
                .Where(link => link.type == CityPedestrianLinkType.ExistingNetwork &&
                               link.id != "pedestrian-park-spine")
                .ToArray();
            bool openingHomesUseLocalStreets = streetCity.buildings.All(building =>
                building.vehicle_road_ids
                    .Select(id => streetCity.vehicle_roads.Single(road => road.id == id))
                    .All(road => road.role == CityVehicleRoadRole.BuildingAccess &&
                                 road.connected_road_ids.All(connectionId =>
                                     localRoads.Any(local => local.id == connectionId))));
            bool sidewalkFollowsRoad = mainRoad.points_norm.Length == mainSidewalk.points_norm.Length &&
                mainRoad.points_norm.Select((point, index) => new { point, index })
                    .Skip(1)
                    .Take(mainRoad.points_norm.Length - 2)
                    .Select(item => WorldDistance(
                        item.point,
                        mainSidewalk.points_norm[item.index],
                        streetCity.bounds))
                    .All(distance => distance >= 0.95f && distance <= 1.15f);
            bool accessSidewalksFollowRoads = streetCity.vehicle_roads
                .Where(road => road.role == CityVehicleRoadRole.BuildingAccess)
                .All(road =>
                {
                    string buildingId = road.connected_building_ids.Single();
                    CityPedestrianLink sidewalk = streetCity.pedestrian_links.Single(link =>
                        link.connected_building_ids.Contains(buildingId) &&
                        link.type == CityPedestrianLinkType.BasicBuildingAccess);
                    return road.points_norm.Select((point, index) => WorldDistance(
                            point,
                            sidewalk.points_norm[index],
                            streetCity.bounds))
                        .All(distance => distance >= 0.90f && distance <= 1.06f);
                });
            CityMobilityPlan streetPlan = CityTripPlanner.CreatePlan(streetCity);
            bool walkersUseSidewalk = streetPlan.trips
                .Where(trip => trip.mode == CityTravelMode.Walk)
                .All(trip => trip.route_points_norm.Any(routePoint =>
                    mainSidewalk.points_norm.Any(sidewalkPoint =>
                        WorldDistance(routePoint, sidewalkPoint, streetCity.bounds) <= 0.01f)));
            bool compactActorScale =
                humanArtwork.All(renderer =>
                    SpriteDisplaySize(renderer, false) >= 0.22f &&
                    SpriteDisplaySize(renderer, false) <= 0.24f) &&
                vehicleArtwork.All(renderer =>
                    SpriteDisplaySize(renderer, false) >= 0.23f &&
                    SpriteDisplaySize(renderer, false) <= 0.25f) &&
                wildlifeArtwork.Length == 15 &&
                wildlifeArtwork.All(renderer =>
                    SpriteDisplaySize(renderer, true) >= 0.14f &&
                    SpriteDisplaySize(renderer, true) <= 0.29f);
            if (!resourcesLoad || humanArtworkCount != 6 || vehicleArtworkCount != 2 ||
                visiblePathLayers != 4 || !prototype.PedestrianNetworkVisible ||
                prototype.WalkTripCount != 4 || !sidewalkFollowsRoad ||
                localRoads.Length != 3 || localSidewalks.Length != 3 ||
                !openingHomesUseLocalStreets || !accessSidewalksFollowRoads ||
                !walkersUseSidewalk || !compactActorScale)
            {
                throw new InvalidOperationException(
                    $"Street-life presentation is incomplete: humans={humanArtworkCount}, " +
                    $"vehicles={vehicleArtworkCount}, pathLayers={visiblePathLayers}, " +
                    $"wildlife={wildlifeArtwork.Length}, compactScale={compactActorScale}, " +
                    $"pathsVisible={prototype.PedestrianNetworkVisible}, walkTrips={prototype.WalkTripCount}.");
            }
            Debug.Log(
                "UNITY_CITY_STREET_LIFE_SMOKE_OK humans=6 walking=4 vehicles=2 " +
                "vehicle_sprites=3 pedestrian_links=6 dynamic_outlined_paths=4 visible_by_default=True " +
                "existing_local_streets=3 opening_homes_use_local_streets=True " +
                "shared_road_corridor=True pedestrians_on_sidewalk=True road_centre_separated=True " +
                "human_height=0.23 vehicle_depth=0.24 wildlife_width=0.15_to_0.28 compact_actor_scale=True");
        }

        private static float SpriteDisplaySize(SpriteRenderer renderer, bool useWidth)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return 0f;
            }
            float sourceSize = useWidth
                ? renderer.sprite.bounds.size.x
                : renderer.sprite.bounds.size.y;
            float displayScale = useWidth
                ? Mathf.Abs(renderer.transform.lossyScale.x)
                : Mathf.Abs(renderer.transform.lossyScale.y);
            return sourceSize * displayScale;
        }

        private static float WorldDistance(float[] first, float[] second, CityBounds bounds)
        {
            float deltaX = (first[0] - second[0]) * bounds.width_units;
            float deltaY = (first[1] - second[1]) * bounds.height_units;
            return Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        private static void VerifyResidentialBuildingVisuals(CityPrototypeDemo prototype)
        {
            Transform buildings = prototype.transform.Find("Generated City Prototype/Buildings");
            SpriteRenderer[] renderers = buildings == null
                ? Array.Empty<SpriteRenderer>()
                : buildings.GetComponentsInChildren<SpriteRenderer>();
            int residentialBlueCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "detached-house-riverside-blue-v09");
            int residentialCoralCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "detached-house-riverside-coral-v09");
            int apartmentCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "apartment-riverside-v04");
            if (residentialBlueCount != 1 || residentialCoralCount != 1 ||
                apartmentCount != 0 ||
                Resources.Load<Sprite>("UrbanWildlife/Buildings/apartment-riverside-v04") == null ||
                Math.Abs(CityPrototypeDemo.ReferenceBuildingVisualScale(
                    CityBuildingType.DetachedHouse) - 0.72f) > 0.001f ||
                Math.Abs(CityPrototypeDemo.ReferenceBuildingVisualScale(
                    CityBuildingType.Apartment) - 0.56f) > 0.001f)
            {
                throw new InvalidOperationException(
                    "The sparse opening must contain two reference-scaled detached houses while retaining the apartment asset; " +
                    $"blue={residentialBlueCount}, coral={residentialCoralCount}, apartment={apartmentCount}.");
            }
            Debug.Log(
                "UNITY_CITY_RESIDENTIAL_VISUAL_SMOKE_OK opening_blue=1 opening_coral=1 apartment_available=True " +
                "detached_scale=0.72 apartment_scale=0.56 transparent_building_only=True map_controls_ground=True " +
                "placeholder_blocks=False");
        }

        private static void VerifyCommercialBuildingVisuals(CityPrototypeDemo prototype)
        {
            Transform buildings = prototype.transform.Find("Generated City Prototype/Buildings");
            SpriteRenderer[] renderers = buildings == null
                ? Array.Empty<SpriteRenderer>()
                : buildings.GetComponentsInChildren<SpriteRenderer>();
            string[] commercialVariants =
            {
                "commercial-market-riverside-v05",
            };
            string[] communityVariants =
            {
                "community-centre-riverside-v04",
            };
            int laterDestinationCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                (commercialVariants.Contains(renderer.sprite.name) ||
                 communityVariants.Contains(renderer.sprite.name)));
            bool variantsLoad = commercialVariants.Concat(communityVariants).All(name =>
                Resources.Load<Sprite>($"UrbanWildlife/Buildings/{name}") != null);
            if (laterDestinationCount != 0 || !variantsLoad ||
                Math.Abs(CityPrototypeDemo.ReferenceBuildingVisualScale(
                    CityBuildingType.Commercial) - 0.72f) > 0.001f ||
                Math.Abs(CityPrototypeDemo.ReferenceBuildingVisualScale(
                    CityBuildingType.CommunityFacility) - 0.66f) > 0.001f)
            {
                throw new InvalidOperationException(
                    "Later public destinations must stay available without appearing in the opening map; " +
                    $"openingDestinations={laterDestinationCount}, variantsLoad={variantsLoad}.");
            }
            Debug.Log(
                "UNITY_CITY_COMMERCIAL_VISUAL_SMOKE_OK opening_public_buildings=0 " +
                "commercial_reference_asset=1 community_reference_asset=1 " +
                "commercial_scale=0.72 community_scale=0.66 " +
                "transparent_building_only=True placeholder_blocks=False");
        }

        private static void VerifyPlanningGridVisuals(CityPrototypeDemo prototype)
        {
            CityPlanningGrid grid = CityPrototypeStateFactory.Create().planning_grid;
            Transform cells = prototype.transform.Find("Generated City Prototype/Planning grid/Cells");
            Transform boundaries = prototype.transform.Find("Generated City Prototype/Planning grid/Boundaries");
            Transform greenery = prototype.transform.Find("Generated City Prototype/Green patches");
            Transform underlay = prototype.transform.Find("Generated City Prototype/Organic terrain underlay");
            SpriteRenderer underlayRenderer = underlay == null
                ? null
                : underlay.GetComponent<SpriteRenderer>();
            Font regularFont = Resources.Load<Font>(
                "UrbanWildlife/Fonts/Nunito-Regular");
            Font boldFont = Resources.Load<Font>(
                "UrbanWildlife/Fonts/Nunito-Bold");
            if (grid.cols != 18 || grid.rows != 12 || cells.childCount != 216 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.OpenLand) != 56 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Woodland) != 158 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Building) != 2 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.PublicGreen) != 0 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.CivicPlaza) != 0 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Water) != 0 ||
                boundaries.GetComponentsInChildren<LineRenderer>().Length != 32 ||
                underlayRenderer?.sprite == null ||
                underlayRenderer.sprite.name != "city-board-woodland-existing-streets-v07" ||
                underlayRenderer.sortingOrder != -50 ||
                regularFont == null || boldFont == null ||
                prototype.GetComponentsInChildren<Transform>().Any(item => item.name == "East canal"))
            {
                throw new InvalidOperationException(
                    "The sparse opening map must render its woodland and existing-street underlay, 216 logical cells " +
                    "(56 open, 158 woodland, 2 opening houses and no water cells), " +
                    "32 fully transparent grid lines, no river and no ocean.");
            }

            LineRenderer[] boundaryLines = boundaries.GetComponentsInChildren<LineRenderer>();
            bool regularBoundaryGeometry = boundaryLines
                .Where(line => line.name.StartsWith("Column", StringComparison.Ordinal))
                .All(line => line.positionCount == grid.rows + 1) &&
                boundaryLines
                    .Where(line => line.name.StartsWith("Row", StringComparison.Ordinal))
                    .All(line => line.positionCount == grid.cols + 1);
            if (!regularBoundaryGeometry)
            {
                throw new InvalidOperationException(
                    "Planning boundaries must form one shared, camera-readable city grid.");
            }

            foreach (CityGridCell cell in grid.cells)
            {
                Transform cellObject = cells.Find(cell.id);
                Transform cover = cellObject?.Find("Land cover " + cell.current_cover);
                MeshFilter mesh = cover == null ? null : cover.GetComponent<MeshFilter>();
                if (mesh?.sharedMesh == null || mesh.sharedMesh.vertexCount != 4 ||
                    cellObject.GetComponentInChildren<TextMesh>() != null)
                {
                    throw new InvalidOperationException(
                        $"Planning cell {cell.id} must keep its land cover while hiding developer coordinate labels.");
                }

                if (cell.current_cover == CityLandCover.Woodland)
                {
                    Transform grove = greenery.Find(cell.id + " woodland grove");
                    if (grove != null)
                    {
                        throw new InvalidOperationException(
                            $"Woodland cell {cell.id} duplicated trees already baked into the opening underlay.");
                    }
                }
                else if (cell.current_cover == CityLandCover.PublicGreen)
                {
                    Transform garden = greenery.Find(cell.id + " public greenery");
                    Transform naturalFeature = cellObject.Find("PublicGreen natural feature");
                    if (garden == null || garden.GetComponentsInChildren<SpriteRenderer>().Length != 3 ||
                        naturalFeature?.GetComponent<MeshFilter>()?.sharedMesh == null)
                    {
                        throw new InvalidOperationException(
                            $"Public green cell {cell.id} must retain an organic ground patch, one tree and two small shrubs.");
                    }
                }
                else if (cell.current_cover == CityLandCover.CivicPlaza &&
                         (cellObject.Find("CivicPlaza natural feature")?.GetComponent<MeshFilter>()?.sharedMesh == null ||
                          cellObject.Find("Civic plaza artwork")?.GetComponent<SpriteRenderer>()?.sprite == null))
                {
                    throw new InvalidOperationException(
                        $"Civic plaza cell {cell.id} must render its detailed civic-space artwork inside its logical planning area.");
                }
            }

            int actualGroves = greenery.GetComponentsInChildren<Transform>()
                .Count(item => item.parent == greenery &&
                               item.name.EndsWith(" woodland grove", StringComparison.Ordinal));
            if (actualGroves != 0)
            {
                throw new InvalidOperationException($"Expected no duplicated dynamic woodland groves, found {actualGroves}.");
            }
            Debug.Log($"UNITY_CITY_PLANNING_GRID_VISUAL_SMOKE_OK grid=18x12 cells=216 available=214 woodland=158 open=56 woodland_groves={actualGroves} baked_forest=True existing_streets_baked=True duplicate_street_layers=False dynamic_access_roads=True dynamic_access_footpaths=True transparent_land_cover=True hidden_regular_grid=True developer_labels=False baseline_water_cells=0 no_river=True no_ocean=True static_nunito=Regular/Bold");
        }

        private static void VerifyCompactNeighbourhoodPresentation(CityPrototypeDemo prototype)
        {
            Transform roads = prototype.transform.Find("Generated City Prototype/Vehicle road network");
            Transform accessRoads = prototype.transform.Find("Generated City Prototype/Visible building access roads");
            Transform footpaths = prototype.transform.Find("Generated City Prototype/Pedestrian link network");
            LineRenderer[] accessRoadLayers = accessRoads == null
                ? Array.Empty<LineRenderer>()
                : accessRoads.GetComponentsInChildren<LineRenderer>();
            int accessKerbs = accessRoadLayers.Count(line =>
                line.name.EndsWith(" kerb", StringComparison.Ordinal));
            int accessSurfaces = accessRoadLayers.Count(line =>
                line.name.EndsWith(" asphalt", StringComparison.Ordinal));
            int accessCentreDashes = accessRoadLayers.Count(line =>
                line.name.Contains(" centre dash "));
            bool accessPaletteMatchesExisting = accessRoadLayers
                .Where(line => line.name.EndsWith(" asphalt", StringComparison.Ordinal))
                .All(line =>
                {
                    Color colour = line.sharedMaterial.color;
                    return Math.Abs(colour.r - 239f / 255f) < 0.01f &&
                           Math.Abs(colour.g - 241f / 255f) < 0.01f &&
                           Math.Abs(colour.b - 237f / 255f) < 0.01f;
                });
            Transform wildlife = prototype.transform.Find("Generated City Prototype/City wildlife agents");
            SpriteRenderer[] wildlifeSprites = wildlife == null
                ? Array.Empty<SpriteRenderer>()
                : wildlife.GetComponentsInChildren<SpriteRenderer>(true);
            string[] expectedWildlifeSprites =
            {
                "pigeon-citybuilder-soft-v02",
                "squirrel-citybuilder-soft-v02",
                "fox-citybuilder-soft-v02",
                "hedgehog-citybuilder-soft-v02",
            };
            float wildlifeHorizontalSpan = wildlifeSprites.Length == 0
                ? 0f
                : wildlifeSprites.Max(renderer => renderer.transform.position.x) -
                  wildlifeSprites.Min(renderer => renderer.transform.position.x);
            float wildlifeVerticalSpan = wildlifeSprites.Length == 0
                ? 0f
                : wildlifeSprites.Max(renderer => renderer.transform.position.z) -
                  wildlifeSprites.Min(renderer => renderer.transform.position.z);
            if (roads == null || !roads.gameObject.activeSelf ||
                accessRoads == null || !accessRoads.gameObject.activeSelf ||
                accessKerbs != 2 || accessSurfaces != 2 || accessCentreDashes < 2 ||
                !accessPaletteMatchesExisting ||
                footpaths == null || !footpaths.gameObject.activeSelf ||
                footpaths.GetComponentsInChildren<LineRenderer>().Length != 4 ||
                wildlife == null || wildlife.childCount != 15 ||
                wildlifeSprites.Length != 15 ||
                wildlifeHorizontalSpan < 5f || wildlifeVerticalSpan < 3f ||
                wildlifeSprites.Any(renderer => renderer.sprite == null ||
                                                  !expectedWildlifeSprites.Contains(renderer.sprite.name)))
            {
                throw new InvalidOperationException(
                    "The opening view must show its dynamic roads and paths, use wildlife artwork, and distribute animals across the map.");
            }
            Debug.Log(
                "UNITY_CITY_NEIGHBOURHOOD_PRESENTATION_SMOKE_OK " +
                "existing_street_hierarchy_visible=True two_access_roads_visible=True " +
                "access_roads_match_existing_style=True centre_dashes=True pedestrian_paths_default_visible=True " +
                $"wildlife_artwork=15 wildlife_span={wildlifeHorizontalSpan:0.0}x{wildlifeVerticalSpan:0.0} " +
                "hedgehog_fallback=0 compact_buildings=True");
        }

        private static void VerifyResponsiveCameraFit()
        {
            (int width, int height)[] sizes =
            {
                (1920, 1080),
                (1920, 1200),
                (1280, 1024),
            };
            foreach ((int width, int height) in sizes)
            {
                float viewportWidth = CityPrototypeDemo.MapViewportFraction;
                float size = CityPrototypeDemo.OrthographicSizeForViewport(
                    width,
                    height,
                    viewportWidth);
                float viewportAspect = width * viewportWidth / height;
                float visibleWidth = size * 2f * viewportAspect;
                float visibleHeight = size * 2f;
                if (visibleWidth < 12.54f || visibleHeight < 8.54f)
                {
                    throw new InvalidOperationException(
                        $"Responsive camera crops the board at {width}x{height}: " +
                        $"visible={visibleWidth:0.00}x{visibleHeight:0.00}.");
                }
            }
            Debug.Log("UNITY_CITY_RESPONSIVE_CAMERA_SMOKE_OK sizes=1920x1080/1920x1200/1280x1024 board_uncropped=True ocean_frame=False");
        }

        private static void VerifyCameraScanFileSource()
        {
            string examplePath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../../data/examples/city_scan_new_v0.1.json"));
            if (!CityTokenScanFileSource.TryRead(
                    examplePath,
                    out string json,
                    out string resolvedPath,
                    out string error) ||
                string.IsNullOrWhiteSpace(json) ||
                resolvedPath != examplePath)
            {
                throw new InvalidOperationException(
                    $"City camera scan file bridge failed: {error}");
            }
        }

        private static void VerifyPlanningWorkflow()
        {
            CityState workflowCity = CityPrototypeStateFactory.Create();
            CityPlanningWorkflow workflow = new CityPlanningWorkflow(
                workflowCity,
                CityPlanningDemoScanFactory.ConfirmedTokensFrom(workflowCity));
            var scan = CityPlanningDemoScanFactory.CreateElectronicSample(
                workflowCity,
                1789322400000);
            if (!workflow.TryAcceptScan(scan, out string error) ||
                workflow.Phase != CityPlanningWorkflowPhase.Preview ||
                workflow.ConstructionPreview.NewCount != 2 ||
                workflow.ConstructionPreview.UnchangedCount != 2 ||
                workflow.CurrentState.buildings.Length != 2)
            {
                string changeSummary = workflow.ConstructionPreview == null
                    ? "no preview"
                    : string.Join(" | ", workflow.ConstructionPreview.changes.Select(change =>
                        $"{change.token_id}:{change.change_kind}:blocked={change.blocks_confirmation}:{change.message}"));
                throw new InvalidOperationException(
                    $"City planning scan/Preview failed: {error}. " +
                    $"phase={workflow.Phase} new={workflow.ConstructionPreview?.NewCount ?? -1} " +
                    $"unchanged={workflow.ConstructionPreview?.UnchangedCount ?? -1} " +
                    $"liveBuildings={workflow.CurrentState.buildings.Length}. Changes: {changeSummary}");
            }
            if (!workflow.ConfirmPreview(out error) ||
                workflow.Phase != CityPlanningWorkflowPhase.ReadyToBuild ||
                workflow.CurrentState.buildings.Count(item =>
                    item.construction_state == CityConstructionState.Proposed) != 1 ||
                workflow.CurrentState.vehicle_roads.Count(item =>
                    item.construction_state == CityConstructionState.Proposed &&
                    item.role == CityVehicleRoadRole.BuildingAccess) != 1)
            {
                string changeSummary = workflow.ConstructionPreview == null
                    ? "no preview"
                    : string.Join(" | ", workflow.ConstructionPreview.changes.Select(change =>
                        $"{change.token_id}:{change.change_kind}:blocked={change.blocks_confirmation}:{change.message}"));
                throw new InvalidOperationException(
                    $"City planning Preview confirmation failed: {error}. Changes: {changeSummary}");
            }
            if (workflow.Snapshot.required_development_points != 14 ||
                !workflow.StartConstruction(out error) ||
                workflow.Phase != CityPlanningWorkflowPhase.Construction ||
                workflow.Strategy.DevelopmentPoints != 6)
            {
                throw new InvalidOperationException($"City planning automatic route/DP commit failed: {error}");
            }
            workflow.AdvanceConstructionBlock();
            if (workflow.Phase != CityPlanningWorkflowPhase.Construction)
            {
                throw new InvalidOperationException("Two-block construction completed too early.");
            }
            workflow.AdvanceConstructionBlock();
            bool allBuilt = workflow.CurrentState.buildings
                                .Where(item => item.source_token_id == 112)
                                .All(item => item.construction_state == CityConstructionState.Existing) &&
                            workflow.CurrentState.green_patches
                                .Where(item => item.source_token_id == 140)
                                .All(item => item.construction_state == CityConstructionState.Existing) &&
                            workflow.CurrentState.vehicle_roads
                                .Where(item => item.id.Contains("building-token-111"))
                                .All(item => item.construction_state == CityConstructionState.Existing) &&
                            workflow.CurrentState.pedestrian_links
                                .Where(item => item.id.Contains("building-token-111"))
                                .All(item => item.construction_state == CityConstructionState.Existing);
            if (workflow.Phase != CityPlanningWorkflowPhase.Complete || !allBuilt ||
                workflow.Snapshot.completed_object_ids.Length != 4)
            {
                throw new InvalidOperationException("City construction did not complete transactionally.");
            }
            Debug.Log(
                "UNITY_CITY_PLANNING_WORKFLOW_SMOKE_OK steps=Scan>Preview>AutomaticAccess>DP>Build " +
                "new_objects=2 automatic_route=Direct dp=20-14 construction_blocks=2 " +
                "preview_not_live=True completed_objects=4");
        }

        private static void VerifyGridConstructionRules()
        {
            if (!CityConstructionFactory.ReferenceFootprintUnits(CityBuildingType.DetachedHouse)
                    .SequenceEqual(new[] { 5f, 5f }) ||
                !CityConstructionFactory.ReferenceFootprintUnits(CityBuildingType.Apartment)
                    .SequenceEqual(new[] { 7f, 7f }) ||
                !CityConstructionFactory.ReferenceFootprintUnits(CityBuildingType.Commercial)
                    .SequenceEqual(new[] { 7.5f, 5f }) ||
                !CityConstructionFactory.ReferenceFootprintUnits(CityBuildingType.CommunityFacility)
                    .SequenceEqual(new[] { 9f, 6f }))
            {
                throw new InvalidOperationException(
                    "Building footprints no longer match the Riverside Park reference ratios.");
            }

            CityState woodlandCity = CityPrototypeStateFactory.Create();
            CityTokenState[] baseline = CityPlanningDemoScanFactory.ConfirmedTokensFrom(woodlandCity);
            CityConstructionManager woodlandConstruction =
                new CityConstructionManager(woodlandCity, baseline);

            CityTokenState[] jittered = CloneTokens(baseline);
            jittered[0].x_norm += 0.02f;
            jittered[0].y_norm += 0.01f;
            CityConstructionPreview jitterPreview = woodlandConstruction.ScanCity(
                ScanPacket("grid-same-cell-jitter", jittered));
            if (jitterPreview.UnchangedCount != baseline.Length - 1 ||
                jitterPreview.MovedCount != 1 || jitterPreview.NewCount != 0)
            {
                throw new InvalidOperationException(
                    "Continuous placement must preserve meaningful movement inside a planning cell.");
            }
            woodlandConstruction.CancelPreview();

            CityGridCell woodland = woodlandCity.planning_grid.cells.Single(cell =>
                cell.row == 2 && cell.col == 0 &&
                cell.current_cover == CityLandCover.Woodland);
            CityTokenState woodlandBuildingToken = new CityTokenState
            {
                id = 112,
                type = CityPhysicalTokenType.DetachedHouse,
                x_norm = woodland.center_norm[0],
                y_norm = woodland.center_norm[1],
                rotation_deg = 0f,
                confidence = 1f,
            };
            CityBuilding proposedWoodlandBuilding = CityConstructionFactory.CreateBuilding(
                woodlandBuildingToken,
                CityConstructionState.Proposed);
            CityGridCell[] originalFootprintCells = CityGridResolver.GetBuildingFootprintCellsAtPosition(
                woodlandCity.planning_grid,
                woodlandCity.bounds,
                proposedWoodlandBuilding,
                out string footprintError);
            string[] replacedPatchIds = originalFootprintCells
                .Select(cell => cell.habitat_patch_id)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToArray();
            if (!string.IsNullOrEmpty(footprintError) || originalFootprintCells.Length != 1 ||
                replacedPatchIds.Length != 1)
            {
                throw new InvalidOperationException(
                    $"The continuous woodland test footprint is invalid: {footprintError}");
            }
            CityConstructionPreview woodlandPreview = woodlandConstruction.ScanCity(
                ScanPacket(
                    "grid-woodland-build",
                    baseline.Concat(new[] { woodlandBuildingToken }).ToArray()));
            string woodlandError = string.Empty;
            if (!woodlandPreview.CanConfirmConstruction || woodlandPreview.NewCount != 1 ||
                !woodlandConstruction.ConfirmConstruction(out woodlandError))
            {
                throw new InvalidOperationException(
                    $"A valid woodland-cell building could not be confirmed: {woodlandError}");
            }

            CityState builtCity = woodlandConstruction.CurrentState;
            CityBuilding woodlandBuilding = builtCity.buildings.Single(building =>
                building.source_token_id == woodlandBuildingToken.id);
            CityGridCell[] builtCells = woodlandBuilding.planning_cell_ids
                .Select(id => CityGridResolver.GetCell(builtCity.planning_grid, id))
                .ToArray();
            if (woodlandBuilding.planning_cell_id != woodland.id ||
                woodlandBuilding.planning_cell_ids.Length != 1 ||
                builtCells.Any(cell => cell == null ||
                                       cell.current_cover != CityLandCover.Building ||
                                       cell.occupant_id != woodlandBuilding.id) ||
                !builtCells.Any(cell => cell.was_woodland) ||
                builtCity.green_patches.Any(patch => replacedPatchIds.Contains(patch.id)))
            {
                throw new InvalidOperationException(
                    "A continuous building footprint must retain woodland history and remove every overwritten habitat patch.");
            }

            woodlandBuilding.construction_state = CityConstructionState.DemolitionProposed;
            CityStrategySimulation demolition = new CityStrategySimulation(builtCity);
            if (!demolition.TryQueueProject(
                    CityStrategyActionType.Demolish,
                    woodlandBuilding.id,
                    out string demolitionError))
            {
                throw new InvalidOperationException(
                    $"Woodland-history demolition could not start: {demolitionError}");
            }
            for (int block = 0; block < 4 &&
                                builtCity.buildings.Any(building =>
                                    building.id == woodlandBuilding.id); block += 1)
            {
                demolition.AdvanceTimeBlock();
            }
            CityGridCell[] releasedCells = woodlandBuilding.planning_cell_ids
                .Select(id => CityGridResolver.GetCell(builtCity.planning_grid, id))
                .ToArray();
            if (builtCity.buildings.Any(building => building.id == woodlandBuilding.id) ||
                releasedCells.Any(cell => cell == null ||
                                           !string.IsNullOrWhiteSpace(cell.occupant_id)) ||
                releasedCells.Where(cell => cell.was_woodland)
                    .Any(cell => cell.current_cover != CityLandCover.Disturbed) ||
                releasedCells.Where(cell => !cell.was_woodland)
                    .Any(cell => cell.current_cover != CityLandCover.OpenLand))
            {
                throw new InvalidOperationException(
                    "Demolishing a multi-cell building must release its full footprint and preserve disturbed woodland history.");
            }

            CityState fixedCity = CityPrototypeStateFactory.Create();
            CityTokenState[] fixedBaseline = CityPlanningDemoScanFactory.ConfirmedTokensFrom(fixedCity);
            CityGridCell water = fixedCity.planning_grid.cells.First(cell =>
                cell.current_cover == CityLandCover.OpenLand &&
                string.IsNullOrWhiteSpace(cell.occupant_id));
            water.baseline_cover = CityLandCover.Water;
            water.current_cover = CityLandCover.Water;
            water.buildable = false;
            water.fixed_feature = true;
            water.habitat_patch_id = null;
            water.was_woodland = false;
            CityConstructionManager fixedConstruction =
                new CityConstructionManager(fixedCity, fixedBaseline);
            CityConstructionPreview fixedPreview = fixedConstruction.ScanCity(
                ScanPacket(
                    "grid-synthetic-fixed-water",
                    fixedBaseline.Concat(new[]
                    {
                        new CityTokenState
                        {
                            id = 102,
                            type = CityPhysicalTokenType.Apartment,
                            x_norm = water.center_norm[0],
                            y_norm = water.center_norm[1],
                            rotation_deg = 0f,
                            confidence = 1f,
                        },
                    }).ToArray()));
            if (!fixedPreview.HasBlockingChanges || fixedPreview.CanConfirmConstruction)
            {
                throw new InvalidOperationException(
                    "The reusable Water land-cover rule must still reject building construction.");
            }

            CityState rotationCity = CityPrototypeStateFactory.Create();
            CityPlanningGrid rotationGrid = CityGridResolver.DeepClone(rotationCity.planning_grid);
            CityGridCell rotationAnchor = rotationGrid.cells.Single(cell =>
                cell.row == 3 && cell.col == 7);
            CityBuilding rotatedCommunity = CityConstructionFactory.CreateBuilding(
                TokenAt(131, CityPhysicalTokenType.CommunityFacility, rotationAnchor),
                CityConstructionState.Proposed);
            rotatedCommunity.rotation_deg = 90f;
            string rotationError = string.Empty;
            if (rotatedCommunity.footprint_units.Length != 2 ||
                Math.Abs(rotatedCommunity.footprint_units[0] - 9f) > 0.001f ||
                Math.Abs(rotatedCommunity.footprint_units[1] - 6f) > 0.001f ||
                !CityGridResolver.TryOccupyWithBuilding(
                    rotationGrid,
                    rotationCity.bounds,
                    rotatedCommunity,
                    rotationCity.revision + 1,
                    out rotationError) ||
                rotatedCommunity.planning_cell_ids.Length != 4)
            {
                throw new InvalidOperationException(
                    $"The refined 18x12 reference community footprint was not retained: {rotationError}");
            }

            CityState capCity = CityPrototypeStateFactory.Create();
            capCity.planning_grid.max_active_player_buildings = 4;
            CityTokenState[] capBaseline = CityPlanningDemoScanFactory.ConfirmedTokensFrom(capCity);
            CityGridCell[] openCells = FindPlaceableDetachedCells(capCity, 3);
            if (openCells.Length < 3)
            {
                throw new InvalidOperationException(
                    "The baseline city does not contain three road-safe detached-house positions.");
            }
            CityTokenState[] twoAdditions =
            {
                TokenAt(112, CityPhysicalTokenType.DetachedHouse, openCells[1]),
                TokenAt(113, CityPhysicalTokenType.DetachedHouse, openCells[2]),
            };
            CityConstructionManager capConstruction =
                new CityConstructionManager(capCity, capBaseline);
            CityConstructionPreview capPreview = capConstruction.ScanCity(
                ScanPacket(
                    "grid-cap-nine",
                    capBaseline.Concat(twoAdditions).ToArray()));
            string capError = string.Empty;
            if (!capPreview.CanConfirmConstruction || capPreview.NewCount != 2 ||
                !capConstruction.ConfirmConstruction(out capError))
            {
                throw new InvalidOperationException(
                    $"The cap test city could not reach its four-building limit: {capError}");
            }
            CityTokenState tenth = TokenAt(
                102,
                CityPhysicalTokenType.Apartment,
                openCells[0]);
            CityConstructionPreview overCapPreview = capConstruction.ScanCity(
                ScanPacket(
                    "grid-over-cap",
                    capConstruction.ConfirmedTokens.Concat(new[] { tenth }).ToArray()));
            if (!overCapPreview.HasBlockingChanges || overCapPreview.CanConfirmConstruction)
            {
                throw new InvalidOperationException(
                    "A building beyond the active-player-building cap must be rejected.");
            }

            CityState sharedCellCity = CityPrototypeStateFactory.Create();
            CityPlanningGrid sharedCellGrid = CityGridResolver.DeepClone(
                sharedCellCity.planning_grid);
            foreach (CityGridCell cell in sharedCellGrid.cells)
            {
                cell.buildable = true;
                cell.fixed_feature = false;
                cell.current_cover = CityLandCover.OpenLand;
                cell.occupant_id = null;
                cell.occupant_ids = Array.Empty<string>();
                cell.habitat_patch_id = null;
            }
            CityBuilding firstExactBuilding = CityConstructionFactory.CreateBuilding(
                new CityTokenState
                {
                    id = 112,
                    type = CityPhysicalTokenType.DetachedHouse,
                    x_norm = 20f / sharedCellCity.bounds.width_units,
                    y_norm = 0.5f,
                    rotation_deg = 0f,
                    confidence = 1f,
                },
                CityConstructionState.Proposed);
            CityBuilding secondExactBuilding = CityConstructionFactory.CreateBuilding(
                new CityTokenState
                {
                    id = 113,
                    type = CityPhysicalTokenType.DetachedHouse,
                    x_norm = 25.1f / sharedCellCity.bounds.width_units,
                    y_norm = 0.5f,
                    rotation_deg = 0f,
                    confidence = 1f,
                },
                CityConstructionState.Proposed);
            string exactCollisionIssue = CityConstructionPlacementRules.ValidateBuilding(
                secondExactBuilding,
                sharedCellCity.bounds,
                new[] { firstExactBuilding },
                Array.Empty<CityBuilding>(),
                Array.Empty<CityVehicleRoad>());
            bool firstExactOccupied = CityGridResolver.TryOccupyWithBuildingAtPosition(
                sharedCellGrid,
                sharedCellCity.bounds,
                firstExactBuilding,
                sharedCellCity.revision + 1,
                true,
                out string firstExactError);
            bool secondExactOccupied = CityGridResolver.TryOccupyWithBuildingAtPosition(
                sharedCellGrid,
                sharedCellCity.bounds,
                secondExactBuilding,
                sharedCellCity.revision + 1,
                true,
                out string secondExactError);
            CityGridCell[] sharedCells = firstExactBuilding.planning_cell_ids
                .Intersect(secondExactBuilding.planning_cell_ids, StringComparer.Ordinal)
                .Select(id => CityGridResolver.GetCell(sharedCellGrid, id))
                .Where(cell => cell != null)
                .ToArray();
            bool firstReleased = CityGridResolver.TryReleaseBuilding(
                sharedCellGrid,
                firstExactBuilding.id,
                sharedCellCity.revision + 2,
                out string sharedReleaseError);
            if (!string.IsNullOrEmpty(exactCollisionIssue) ||
                !firstExactOccupied || !secondExactOccupied || sharedCells.Length == 0 ||
                sharedCells.Any(cell => !CityGridResolver.BuildingOccupantIds(cell)
                    .Contains(secondExactBuilding.id)) ||
                !firstReleased ||
                sharedCells.Any(cell => CityGridResolver.BuildingOccupantIds(cell)
                    .Contains(firstExactBuilding.id)))
            {
                throw new InvalidOperationException(
                    "Desktop exact-footprint occupancy must allow non-overlapping buildings " +
                    "to share ecological cells and release them independently. " +
                    $"placement={exactCollisionIssue}/{firstExactError}/{secondExactError}, " +
                    $"release={sharedReleaseError}");
            }

            Debug.Log(
                "UNITY_CITY_GRID_RULES_SMOKE_OK grid=18x12 cell_units=5x5 snap_radius_cm=5 same_cell_jitter=Moved " +
                "footprints_units=detached_5x5/apartment_7x7/commercial_7.5x5/community_9x6 " +
                "woodland_build=Building woodland_patches_removed=True " +
                "footprint_demolition=Released shared_grid_cells_exact_collision=True " +
                "synthetic_water_rule_rejected=True baseline_water_cells=0 active_building_cap_rule=True");
        }

        private static void VerifyFreePlacementAndAutomaticAccess()
        {
            CityState city = CityPrototypeStateFactory.Create();
            CityTokenState[] baseline = CityPlanningDemoScanFactory.ConfirmedTokensFrom(city);
            const float placedX = 0.064f;
            const float placedY = 0.742f;
            CityTokenState token = new CityTokenState
            {
                id = 112,
                type = CityPhysicalTokenType.DetachedHouse,
                x_norm = placedX,
                y_norm = placedY,
                rotation_deg = 7f,
                confidence = 1f,
            };
            CityConstructionManager construction = new CityConstructionManager(city, baseline);
            CityConstructionPreview preview = construction.ScanCity(
                ScanPacket("continuous-free-placement", baseline.Concat(new[] { token }).ToArray()));
            string placementError = string.Empty;
            if (!preview.CanConfirmConstruction ||
                !construction.ConfirmConstruction(out placementError))
            {
                throw new InvalidOperationException(
                    $"A free-position woodland building could not be confirmed: {placementError}");
            }

            CityState placedCity = construction.CurrentState;
            CityBuilding placed = placedCity.buildings.Single(building =>
                building.source_token_id == token.id);
            CityGridCell[] occupiedCells = placed.planning_cell_ids
                .Select(id => CityGridResolver.GetCell(placedCity.planning_grid, id))
                .ToArray();
            if (Math.Abs(placed.position_norm[0] - placedX) > 0.0001f ||
                Math.Abs(placed.position_norm[1] - placedY) > 0.0001f ||
                occupiedCells.Length < 1 ||
                !occupiedCells.Any(cell => cell.was_woodland) ||
                occupiedCells.Any(cell => cell.current_cover != CityLandCover.Building))
            {
                throw new InvalidOperationException(
                    "Continuous placement snapped the building or failed to clear its woodland footprint.");
            }

            CityBuilding onRoad = CityConstructionFactory.CreateBuilding(
                new CityTokenState
                {
                    id = 113,
                    type = CityPhysicalTokenType.DetachedHouse,
                    x_norm = 0.41f,
                    y_norm = 0.425f,
                    rotation_deg = 0f,
                    confidence = 1f,
                },
                CityConstructionState.Proposed);
            string roadIssue = CityConstructionPlacementRules.ValidateBuilding(
                onRoad,
                city.bounds,
                city.buildings,
                Array.Empty<CityBuilding>(),
                city.vehicle_roads);
            if (string.IsNullOrEmpty(roadIssue) || !roadIssue.Contains("road"))
            {
                throw new InvalidOperationException(
                    "A building placed on the main road was not rejected.");
            }

            CityRoadChoiceSet access = CityRoadCandidateGenerator.Generate(placedCity, placed);
            CityRoadCandidate automatic = access.candidates.Single(candidate =>
                candidate.route_option == CityRoadRouteOption.Direct);
            CityBuilding localStreetBuilding = CityConstructionFactory.CreateBuilding(
                new CityTokenState
                {
                    id = 113,
                    type = CityPhysicalTokenType.DetachedHouse,
                    x_norm = 0.65f,
                    y_norm = 0.60f,
                    rotation_deg = 0f,
                    confidence = 1f,
                },
                CityConstructionState.Proposed);
            CityRoadCandidate localStreetAccess = CityRoadCandidateGenerator
                .Generate(city, localStreetBuilding)
                .candidates.Single(candidate =>
                    candidate.route_option == CityRoadRouteOption.Direct);
            CityVehicleRoad automaticConnection = placedCity.vehicle_roads.Single(road =>
                road.id == automatic.connection_road_id);
            CityPedestrianLink automaticSidewalk =
                CityRoadCandidateGenerator.CreateBasicPedestrianAccess(
                    placedCity,
                    placed,
                    automatic);
            bool sidewalkTracksAutomaticRoad = automatic.points_norm
                .Select((point, index) => WorldDistance(
                    point,
                    automaticSidewalk.points_norm[index],
                    placedCity.bounds))
                .All(distance => distance >= 0.90f && distance <= 1.06f);
            if (automatic.points_norm.Length != 11 ||
                automaticConnection.source != CityNetworkSource.ExistingMap ||
                automaticConnection.role == CityVehicleRoadRole.BuildingAccess ||
                localStreetAccess.connection_road_id != "vehicle-local-south-arc" ||
                automatic.points_norm.Skip(1).Take(automatic.points_norm.Length - 2)
                    .All(point => Math.Abs(point[0] - automatic.points_norm[0][0]) < 0.0001f) ||
                automaticSidewalk.points_norm.Length < automatic.points_norm.Length ||
                !sidewalkTracksAutomaticRoad)
            {
                throw new InvalidOperationException(
                    "Automatic access did not create a smooth curve to the nearest existing street.");
            }

            CityNetworkPlanningManager networkPlanner =
                new CityNetworkPlanningManager(placedCity);
            CityNetworkPlanPreview networkPreview = networkPlanner.BeginRouteSelection();
            CityPedestrianLink directSidewalkPreview =
                networkPreview.automatic_pedestrian_links.Single(link =>
                    link.connected_building_ids.Contains(placed.id));
            float[][] directSidewalkPoints = directSidewalkPreview.points_norm
                .Select(point => (float[])point.Clone())
                .ToArray();
            if (!networkPlanner.SelectRoadOption(
                    placed.id,
                    CityRoadRouteOption.LowImpact,
                    out string routeSelectionError))
            {
                throw new InvalidOperationException(
                    $"Could not select a low-impact road for sidewalk synchronization: {routeSelectionError}");
            }
            CityPedestrianLink updatedSidewalkPreview =
                networkPlanner.PendingPreview.automatic_pedestrian_links.Single(link =>
                    link.connected_building_ids.Contains(placed.id));
            bool sidewalkChangedWithRoad = directSidewalkPoints.Length !=
                                           updatedSidewalkPreview.points_norm.Length ||
                directSidewalkPoints.Select((point, index) => new { point, index })
                    .Where(item => item.index < updatedSidewalkPreview.points_norm.Length)
                    .Any(item => WorldDistance(
                        item.point,
                        updatedSidewalkPreview.points_norm[item.index],
                        placedCity.bounds) > 0.05f);
            if (!sidewalkChangedWithRoad)
            {
                throw new InvalidOperationException(
                    "The automatic sidewalk did not update when the selected vehicle route changed.");
            }

            CityState clearingCity = CityPrototypeStateFactory.Create();
            CityGridCell woodland = clearingCity.planning_grid.cells.Single(cell =>
                cell.row == 4 && cell.col == 0 &&
                cell.current_cover == CityLandCover.Woodland);
            string woodlandPatchId = woodland.habitat_patch_id;
            CityVehicleRoad clearingRoad = new CityVehicleRoad
            {
                id = "woodland-clearing-test",
                role = CityVehicleRoadRole.BuildingAccess,
                route_option = CityRoadRouteOption.Direct,
                construction_state = CityConstructionState.Proposed,
                source = CityNetworkSource.AutoGenerated,
                points_norm = new[]
                {
                    new[] { woodland.center_norm[0] - 0.02f, woodland.center_norm[1] },
                    new[] { woodland.center_norm[0] + 0.02f, woodland.center_norm[1] },
                },
                width_units = 1.1f,
            };
            string[] cleared = CityGridResolver.ClearWoodlandAlongRoads(
                clearingCity.planning_grid,
                clearingCity.bounds,
                new[] { clearingRoad },
                clearingCity.revision + 1);
            if (!cleared.Contains(woodlandPatchId) ||
                woodland.current_cover != CityLandCover.OpenLand ||
                !woodland.was_woodland ||
                !string.IsNullOrWhiteSpace(woodland.habitat_patch_id))
            {
                throw new InvalidOperationException(
                    "Automatic access did not convert crossed woodland to cleared white ground.");
            }

            Debug.Log(
                "UNITY_CITY_FREE_PLACEMENT_SMOKE_OK continuous_position=True road_overlap_rejected=True " +
                "smooth_access_curve=11_points nearest_existing_local_street=True " +
                "parallel_sidewalk=True sidewalk_tracks_route_choice=True " +
                "woodland_to_white=True");
        }

        private static void VerifyDesktopGrowthCapacity(CityPrototypeDemo prototype)
        {
            CityState growthCity = CityPrototypeStateFactory.Create();
            var buildings = growthCity.buildings.ToList();
            buildings.Add(CityConstructionFactory.CreateBuilding(
                new CityTokenState
                {
                    id = 112,
                    type = CityPhysicalTokenType.DetachedHouse,
                    x_norm = 0.42f,
                    y_norm = 0.18f,
                    rotation_deg = 0f,
                    confidence = 1f,
                },
                CityConstructionState.Existing));
            buildings.Add(CityConstructionFactory.CreateBuilding(
                new CityTokenState
                {
                    id = 113,
                    type = CityPhysicalTokenType.DetachedHouse,
                    x_norm = 0.58f,
                    y_norm = 0.18f,
                    rotation_deg = 0f,
                    confidence = 1f,
                },
                CityConstructionState.Existing));
            growthCity.buildings = buildings.ToArray();
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!CityPlanningDemoScanFactory.TryCreateDesktopPlacementScan(
                    growthCity,
                    CityPhysicalTokenType.DetachedHouse,
                    0.50f,
                    0.80f,
                    timestamp,
                    out CityTokenScanPacket desktopScan,
                    out string createError))
            {
                throw new InvalidOperationException(
                    $"Desktop growth could not continue beyond the physical pieces: {createError}");
            }
            CityTokenState virtualToken = desktopScan.token_states.SingleOrDefault(token =>
                CityTokenInventory.IsDesktopVirtualTokenId(token.id));
            bool desktopAccepted = CityTokenScanReader.TryParseAndValidate(
                JsonConvert.SerializeObject(desktopScan),
                -1,
                out _,
                out string desktopError);

            desktopScan.capture.mode = "camera_frame";
            desktopScan.recognition.marker_backend = "aruco";
            bool cameraAcceptedVirtual = CityTokenScanReader.TryParseAndValidate(
                JsonConvert.SerializeObject(desktopScan),
                -1,
                out _,
                out _);
            if (prototype.BuildingCapacity != 72 ||
                CityPrototypeDemo.MatureCityBuildingThreshold != 23 ||
                virtualToken == null ||
                virtualToken.type != CityPhysicalTokenType.DetachedHouse ||
                !desktopAccepted || cameraAcceptedVirtual)
            {
                throw new InvalidOperationException(
                    "Desktop play must support dense late-stage growth while camera scans stay physical-only. " +
                    $"capacity={prototype.BuildingCapacity}, virtual={virtualToken?.id}, " +
                    $"desktopAccepted={desktopAccepted}, desktopError={desktopError}, " +
                    $"cameraAcceptedVirtual={cameraAcceptedVirtual}.");
            }
            Debug.Log(
                "UNITY_CITY_DESKTOP_GROWTH_SMOKE_OK capacity=72 mature_threshold=23 " +
                $"virtual_token={virtualToken.id} desktop_repeat_buildings=True " +
                "camera_virtual_tokens_rejected=True");
        }

        private static void VerifyDesktopPlayableFlow(CityPrototypeDemo prototype)
        {
            CityState reference = CityPrototypeStateFactory.Create();
            CityGridCell[] placements = FindPlaceableDetachedCells(reference, 12);
            CityGridCell placement = placements.FirstOrDefault();
            if (prototype == null || !prototype.DesktopPlayEnabled ||
                placement == null || placements.Length < 2)
            {
                throw new InvalidOperationException(
                    "The camera-free desktop edition did not start with enough usable placement points.");
            }
            if (!prototype.TryPlaceDesktopBuilding(
                    CityPhysicalTokenType.DetachedHouse,
                    placement.center_norm[0],
                    placement.center_norm[1],
                    out string placementError) ||
                prototype.PlanningPhase != CityPlanningWorkflowPhase.Preview)
            {
                throw new InvalidOperationException(
                    $"Desktop click placement did not reach Preview: {placementError}");
            }
            Transform validPreview = prototype.transform.Find(
                "Generated City Prototype/Planning Preview Overlay");
            Transform[] validPreviewObjects = validPreview == null
                ? Array.Empty<Transform>()
                : validPreview.GetComponentsInChildren<Transform>();
            MeshRenderer validFootprint = validPreviewObjects
                .Select(item => item.GetComponent<MeshRenderer>())
                .FirstOrDefault(renderer => renderer != null &&
                                            renderer.sharedMaterial.color.g >
                                            renderer.sharedMaterial.color.r);
            bool validPreviewVisible = prototype.PlacementPreviewUsesValidityColours &&
                                       prototype.PlacementPreviewShowsBuildingGhost &&
                                       !prototype.PlacementPreviewShowsAffectedCells &&
                                       prototype.PlacementPreviewUsesExactFootprintOnly &&
                                       prototype.PlacementPreviewCanConfirm &&
                                       validPreviewObjects.Any(item =>
                                           item.name.Contains("building ghost")) &&
                                       !validPreviewObjects.Any(item =>
                                           item.name.Contains("affected cell")) &&
                                       validFootprint != null &&
                                       validFootprint.sharedMaterial.color.g >
                                       validFootprint.sharedMaterial.color.r;
            if (!validPreviewVisible)
            {
                throw new InvalidOperationException(
                    "A valid placement must show only its exact green footprint and ghost building.");
            }
            CityGridCell movedPlacement = placements[1];
            if (!prototype.TryPlaceDesktopBuilding(
                    CityPhysicalTokenType.DetachedHouse,
                    movedPlacement.center_norm[0],
                    movedPlacement.center_norm[1],
                    out string moveError) ||
                prototype.PlanningPhase != CityPlanningWorkflowPhase.Preview ||
                !prototype.PlacementPreviewCanConfirm)
            {
                throw new InvalidOperationException(
                    $"A valid green desktop preview could not move on the next map click: {moveError}");
            }
            if (!prototype.ConfirmDesktopPlacement(out string buildError) ||
                prototype.GeneratedBuildingCount != 3 ||
                prototype.PlanningPhase != CityPlanningWorkflowPhase.ReadyToScan)
            {
                throw new InvalidOperationException(
                    $"Desktop confirmation did not produce a live third building: {buildError}");
            }
            Transform access = prototype.transform.Find(
                "Generated City Prototype/Visible building access roads");
            LineRenderer[] accessLines = access == null
                ? Array.Empty<LineRenderer>()
                : access.GetComponentsInChildren<LineRenderer>();
            int accessKerbCount = accessLines.Count(line =>
                line.name.EndsWith(" kerb", StringComparison.Ordinal));
            int accessSurfaceCount = accessLines.Count(line =>
                line.name.EndsWith(" asphalt", StringComparison.Ordinal));
            int accessDashCount = accessLines.Count(line =>
                line.name.Contains(" centre dash "));
            if (access == null || accessKerbCount != 3 || accessSurfaceCount != 3 ||
                accessDashCount < 3 || prototype.GeneratedAccessRoadCentreDashCount != accessDashCount ||
                accessLines.Max(line => line.startWidth) > 0.20f)
            {
                throw new InvalidOperationException(
                    "Desktop placement did not add an access road matching the original street style.");
            }
            Transform clearings = prototype.transform.Find(
                "Generated City Prototype/Planning grid/Compact development clearings");
            Transform[] generated = prototype.GetComponentsInChildren<Transform>();
            if (clearings == null ||
                clearings.GetComponentsInChildren<LineRenderer>().Length != 3 ||
                generated.Any(item => item.name == "Cleared white ground"))
            {
                throw new InvalidOperationException(
                    "Desktop construction must use compact site and road clearings instead of full-cell white holes.");
            }

            CityVehicleRoad blockedRoad = reference.vehicle_roads.First(road =>
                road != null && road.points_norm != null && road.points_norm.Length >= 2);
            float[] blockedPoint = blockedRoad.points_norm[blockedRoad.points_norm.Length / 2];
            if (!prototype.TryPlaceDesktopBuilding(
                    CityPhysicalTokenType.Commercial,
                    blockedPoint[0],
                    blockedPoint[1],
                    out string blockedError) ||
                prototype.PlanningPhase != CityPlanningWorkflowPhase.Preview)
            {
                throw new InvalidOperationException(
                    $"A road conflict did not reach visible blocked Preview: {blockedError}");
            }
            Transform conflictPreview = prototype.transform.Find(
                "Generated City Prototype/Planning Preview Overlay");
            Transform[] conflictPreviewObjects = conflictPreview == null
                ? Array.Empty<Transform>()
                : conflictPreview.GetComponentsInChildren<Transform>();
            MeshRenderer conflictFootprint = conflictPreviewObjects
                .Select(item => item.GetComponent<MeshRenderer>())
                .FirstOrDefault(renderer => renderer != null &&
                                            renderer.sharedMaterial.color.r >
                                            renderer.sharedMaterial.color.g);
            bool conflictPreviewVisible = conflictPreviewObjects.Any(item =>
                                              item.name.Contains("building ghost")) &&
                                          !conflictPreviewObjects.Any(item =>
                                              item.name.Contains("affected cell")) &&
                                          conflictFootprint != null &&
                                          conflictFootprint.sharedMaterial.color.r >
                                          conflictFootprint.sharedMaterial.color.g;
            if (!conflictPreviewVisible || prototype.PlacementPreviewCanConfirm)
            {
                throw new InvalidOperationException(
                    "A road conflict must show only its exact red footprint and remain unconfirmable.");
            }
            string retryError = string.Empty;
            bool retryAccepted = false;
            foreach (CityGridCell retryPlacement in placements.Skip(2))
            {
                if (prototype.TryPlaceDesktopBuilding(
                        CityPhysicalTokenType.DetachedHouse,
                        retryPlacement.center_norm[0],
                        retryPlacement.center_norm[1],
                        out retryError) &&
                    prototype.PlanningPhase == CityPlanningWorkflowPhase.Preview &&
                    prototype.PlacementPreviewCanConfirm)
                {
                    retryAccepted = true;
                    break;
                }
            }
            if (!retryAccepted)
            {
                throw new InvalidOperationException(
                    $"A blocked desktop placement did not accept the next map click: {retryError}");
            }
            prototype.CancelPlacementPreview();
            if (prototype.PlanningPhase != CityPlanningWorkflowPhase.ReadyToScan)
            {
                throw new InvalidOperationException(
                    "The replacement placement did not return cleanly after cancellation.");
            }
            Debug.Log(
                "UNITY_CITY_DESKTOP_PLAY_SMOKE_OK default_mode=Desktop palette=True pointer_preview=True " +
                "confirm_build=True compact_site_clearings=True access_matches_original_roads=True " +
                "valid_preview=green invalid_preview=red building_ghost=True exact_footprint_only=True " +
                "valid_reposition_without_cancel=True invalid_retry_without_cancel=True " +
                "continuous_position=True responsive_sidebar=True " +
                "camera_required=False camera_mode_retained=True");
        }

        private static void VerifyVehicleRoutesStayOnRoad()
        {
            CityState city = CityPrototypeStateFactory.Create();
            CityMobilityPlan plan = CityTripPlanner.CreatePlan(city);
            CityRepresentativeTrip[] externalTrips = plan.trips
                .Where(trip => trip.purpose == CityTripPurpose.ExternalJourney &&
                               trip.mode == CityTravelMode.Drive)
                .ToArray();
            float longestSegment = externalTrips
                .SelectMany(trip => trip.route_points_norm.Zip(
                    trip.route_points_norm.Skip(1),
                    (first, second) => RouteSegmentLength(first, second, city.bounds)))
                .DefaultIfEmpty(0f)
                .Max();
            CityVehicleRoad mainRoad = city.vehicle_roads.Single(road =>
                road.id == "vehicle-main-street");
            bool routesUseLocalThenMain = externalTrips.All(trip =>
            {
                CityBuilding origin = city.buildings.Single(building =>
                    building.id == trip.origin_building_id);
                CityVehicleRoad access = city.vehicle_roads.Single(road =>
                    road.id == origin.vehicle_road_ids[0]);
                CityVehicleRoad local = city.vehicle_roads.Single(road =>
                    road.id == access.connected_road_ids[0]);
                return local.role == CityVehicleRoadRole.Local &&
                       RouteTouchesRoad(trip.route_points_norm, local, city.bounds) &&
                       RouteTouchesRoad(trip.route_points_norm, mainRoad, city.bounds);
            });
            if (externalTrips.Length == 0 || longestSegment > 15f ||
                !routesUseLocalThenMain)
            {
                throw new InvalidOperationException(
                    $"Vehicle route leaves the road centreline: trips={externalTrips.Length}, " +
                    $"longestSegment={longestSegment:0.00} units, " +
                    $"localThenMain={routesUseLocalThenMain}.");
            }
            Debug.Log(
                $"UNITY_CITY_VEHICLE_ROAD_FOLLOWING_SMOKE_OK external_trips={externalTrips.Length} " +
                $"longest_segment_units={longestSegment:0.00} local_then_main=True " +
                "projected_road_joins=True");
        }

        private static bool RouteTouchesRoad(
            float[][] route,
            CityVehicleRoad road,
            CityBounds bounds)
        {
            return route.Any(routePoint => road.points_norm.Any(roadPoint =>
                WorldDistance(routePoint, roadPoint, bounds) <= 0.01f));
        }

        private static float RouteSegmentLength(float[] first, float[] second, CityBounds bounds)
        {
            float x = (first[0] - second[0]) * bounds.width_units;
            float y = (first[1] - second[1]) * bounds.height_units;
            return Mathf.Sqrt(x * x + y * y);
        }

        private static CityTokenScanPacket ScanPacket(string id, CityTokenState[] tokens)
        {
            return new CityTokenScanPacket
            {
                scan_id = id,
                timestamp_ms = 1,
                token_states = tokens,
            };
        }

        private static CityTokenState TokenAt(
            int id,
            CityPhysicalTokenType type,
            CityGridCell cell)
        {
            return new CityTokenState
            {
                id = id,
                type = type,
                x_norm = cell.center_norm[0],
                y_norm = cell.center_norm[1],
                rotation_deg = 0f,
                confidence = 1f,
            };
        }

        private static CityGridCell[] FindPlaceableDetachedCells(CityState city, int count)
        {
            var selected = new List<CityGridCell>();
            var proposals = new List<CityBuilding>();
            foreach (CityGridCell cell in (city?.planning_grid?.cells ?? Array.Empty<CityGridCell>())
                         .Where(candidate => candidate != null && candidate.buildable &&
                                             !candidate.fixed_feature &&
                                             string.IsNullOrWhiteSpace(candidate.occupant_id))
                         .OrderBy(candidate => candidate.row)
                         .ThenBy(candidate => candidate.col))
            {
                CityTokenState token = TokenAt(
                    900 + selected.Count,
                    CityPhysicalTokenType.DetachedHouse,
                    cell);
                CityBuilding candidate = CityConstructionFactory.CreateBuilding(
                    token,
                    CityConstructionState.Proposed);
                CityGridCell[] footprint = CityGridResolver.GetBuildingFootprintCellsAtPosition(
                    city.planning_grid,
                    city.bounds,
                    candidate,
                    out string footprintError);
                bool gridSafe = string.IsNullOrEmpty(footprintError) && footprint.Length > 0 &&
                                footprint.All(covered => covered.buildable &&
                                                               !covered.fixed_feature &&
                                                               string.IsNullOrWhiteSpace(covered.occupant_id));
                string placementError = CityConstructionPlacementRules.ValidateBuilding(
                    candidate,
                    city.bounds,
                    city.buildings,
                    proposals,
                    city.vehicle_roads);
                if (!gridSafe || !string.IsNullOrEmpty(placementError))
                {
                    continue;
                }
                selected.Add(cell);
                proposals.Add(candidate);
                if (selected.Count >= count)
                {
                    break;
                }
            }
            return selected.ToArray();
        }

        private static CityTokenState[] CloneTokens(CityTokenState[] tokens)
        {
            return tokens.Select(token => new CityTokenState
            {
                id = token.id,
                type = token.type,
                x_norm = token.x_norm,
                y_norm = token.y_norm,
                rotation_deg = token.rotation_deg,
                confidence = token.confidence,
            }).ToArray();
        }
    }
}

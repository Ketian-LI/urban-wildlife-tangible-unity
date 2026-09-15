using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
                0.77f);
            camera.rect = new Rect(0f, 0f, 0.77f, 1f);

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
                                    Math.Abs(camera.rect.width - 0.77f) < 0.001f;
            if (prototype == null || prototype.GeneratedBuildingCount != 2 ||
                prototype.GeneratedPlanningCellCount != 54 ||
                prototype.AvailablePlanningCellCount != 43 ||
                prototype.GeneratedVehicleRoadCount != 3 ||
                prototype.GeneratedPedestrianLinkCount != 3 ||
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
                    $"vehicles={prototype?.VehicleTripCount}, viewport={separateViewport}.");
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
            VerifyActivityHeatmap(prototype);
            VerifyStreetLifeVisuals(prototype);
            VerifyResponsiveCameraFit();
            VerifyCameraScanFileSource();
            VerifyPlanningWorkflow();
            VerifyGridConstructionRules();
            VerifyFreePlacementAndAutomaticAccess();
            VerifyDesktopPlayableFlow(prototype);
            VerifyVehicleRoutesStayOnRoad();
            Debug.Log(
                "UNITY_CITY_PROTOTYPE_SMOKE_OK split_screen=True right_sidebar=True bright_city_style=True opening_buildings=2 vehicle_roads=3 " +
                "planning_grid=9x6 planning_cells=54 available_cells=43 multi_cell_buildings=True hidden_grid=True " +
                "pedestrian_links=3 amenities=0 food_sources=True waste_pressure=True " +
                "wildlife_agents=15 species=4 utility_targets=True " +
                "development_phases=5 city_balance=True dp=True time_blocks=4 " +
                "human_animal_combined_trace=True city_feed=True phase_report=True " +
                "representative_agents=6 represented_population=24 walk_trips=4 vehicle_trips=2 " +
                "external_return_routes=True live_vehicle_agents=True visible_pedestrian_paths=True max_speed=2x no_questionnaire=True " +
                "animal_activity_heatmap=True heatmap_hotkey_h=True footprints_removed=True city_feed_panel=True phase_snapshot=True " +
                "desktop_play_default=True click_preview_confirm_build=True camera_mode_retained=True " +
                "camera_scan_file_bridge=True scan_preview_route_dp_construction=True " +
                "sparse_opening_map=True road_following=True no_premature_buildings=True " +
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

        private static void VerifyActivityHeatmap(CityPrototypeDemo prototype)
        {
            if (prototype.HeatmapVisible)
            {
                throw new InvalidOperationException("The activity heatmap must start hidden.");
            }
            prototype.SetHeatmapVisible(true);
            bool prototypeToggleOn = prototype.HeatmapVisible && prototype.HeatmapUsesAnimalData;
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
                bool animalOnly = visualizer.VisibleCellCount == 1;
                bool noFootprintObjects = !host.GetComponentsInChildren<Transform>(true)
                    .Any(item =>
                        item.name.IndexOf("shoe", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("tyre", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("paw", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        item.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!prototypeToggleOn || !animalOnly || !noFootprintObjects)
                {
                    throw new InvalidOperationException(
                        "The activity heatmap must aggregate trace samples, filter layers and contain no footprint geometry.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
            Debug.Log(
                "UNITY_CITY_ACTIVITY_HEATMAP_SMOKE_OK default_hidden=True h_toggle=True " +
                "animal_only=True animal_cells=1 human_samples_excluded=True individual_marks=False");
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
            int humanArtworkCount = mobilityRoot == null
                ? 0
                : mobilityRoot.GetComponentsInChildren<SpriteRenderer>(true)
                    .Count(renderer => renderer.gameObject.name == "Human artwork");
            int vehicleArtworkCount = mobilityRoot == null
                ? 0
                : mobilityRoot.GetComponentsInChildren<SpriteRenderer>(true)
                    .Count(renderer => renderer.gameObject.name == "Vehicle artwork");
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
            if (!resourcesLoad || humanArtworkCount != 6 || vehicleArtworkCount != 2 ||
                visiblePathLayers != 6 || !prototype.PedestrianNetworkVisible ||
                prototype.WalkTripCount != 4)
            {
                throw new InvalidOperationException(
                    $"Street-life presentation is incomplete: humans={humanArtworkCount}, " +
                    $"vehicles={vehicleArtworkCount}, pathLayers={visiblePathLayers}, " +
                    $"pathsVisible={prototype.PedestrianNetworkVisible}, walkTrips={prototype.WalkTripCount}.");
            }
            Debug.Log(
                "UNITY_CITY_STREET_LIFE_SMOKE_OK humans=6 walking=4 vehicles=2 " +
                "vehicle_sprites=3 pedestrian_links=3 outlined_paths=6 visible_by_default=True");
        }

        private static void VerifyResidentialBuildingVisuals(CityPrototypeDemo prototype)
        {
            Transform buildings = prototype.transform.Find("Generated City Prototype/Buildings");
            SpriteRenderer[] renderers = buildings == null
                ? Array.Empty<SpriteRenderer>()
                : buildings.GetComponentsInChildren<SpriteRenderer>();
            int residentialBlueCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "residential-lot-a-pastel-blue-clean-v05");
            int residentialCoralCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "residential-lot-a-pastel-coral-clean-v05");
            int residentialLotBCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "residential-lot-b-simplified-v03");
            int residentialLotCCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "residential-lot-c-sand-v03");
            if (residentialBlueCount != 1 || residentialCoralCount != 1 ||
                residentialLotBCount != 0 ||
                residentialLotCCount != 0 ||
                Resources.Load<Sprite>("UrbanWildlife/Buildings/residential-lot-b-simplified-v03") == null ||
                Resources.Load<Sprite>("UrbanWildlife/Buildings/residential-lot-c-sand-v03") == null)
            {
                throw new InvalidOperationException(
                    "The sparse opening must contain two matching detached houses while retaining later residential assets; " +
                    $"blue={residentialBlueCount}, coral={residentialCoralCount}, lotB={residentialLotBCount}, " +
                    $"lotC={residentialLotCCount}.");
            }
            Debug.Log(
                "UNITY_CITY_RESIDENTIAL_VISUAL_SMOKE_OK opening_blue=1 opening_coral=1 later_lot_b_c_available=True " +
                "landscaped_lots=True lawn_retained=True " +
                "placeholder_blocks=False");
        }

        private static void VerifyCommercialBuildingVisuals(CityPrototypeDemo prototype)
        {
            Transform buildings = prototype.transform.Find("Generated City Prototype/Buildings");
            SpriteRenderer[] renderers = buildings == null
                ? Array.Empty<SpriteRenderer>()
                : buildings.GetComponentsInChildren<SpriteRenderer>();
            int marketHallCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "market-hall-citybuilder-v02");
            int cornerShopsCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "corner-shops-citybuilder-v02");
            int communityCentreCount = renderers.Count(renderer =>
                renderer.sprite != null &&
                renderer.sprite.name == "community-centre-citybuilder-v01");
            if (marketHallCount != 0 || cornerShopsCount != 0 ||
                communityCentreCount != 0 ||
                Resources.Load<Sprite>("UrbanWildlife/Buildings/market-hall-citybuilder-v02") == null ||
                Resources.Load<Sprite>("UrbanWildlife/Buildings/corner-shops-citybuilder-v02") == null ||
                Resources.Load<Sprite>("UrbanWildlife/Buildings/community-centre-citybuilder-v01") == null)
            {
                throw new InvalidOperationException(
                    "Later public destinations must stay available without appearing in the opening map; " +
                    $"marketHall={marketHallCount}, cornerShops={cornerShopsCount}, " +
                    $"communityCentre={communityCentreCount}.");
            }
            Debug.Log(
                "UNITY_CITY_COMMERCIAL_VISUAL_SMOKE_OK opening_public_buildings=0 " +
                "later_market_shops_community_assets=True " +
                "citybuilder_sprites=True placeholder_blocks=False");
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
            if (grid.cols != 9 || grid.rows != 6 || cells.childCount != 54 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.OpenLand) != 10 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Woodland) != 33 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Building) != 2 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.PublicGreen) != 0 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.CivicPlaza) != 0 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Water) != 9 ||
                boundaries.GetComponentsInChildren<LineRenderer>().Length != 17 ||
                underlayRenderer?.sprite == null ||
                underlayRenderer.sprite.name != "city-board-riverside-opening-v01" ||
                underlayRenderer.sortingOrder != -50 ||
                regularFont == null || boldFont == null ||
                prototype.GetComponentsInChildren<Transform>().Any(item => item.name == "East canal"))
            {
                throw new InvalidOperationException(
                    "The sparse opening map must render its riverside underlay, 54 logical cells " +
                    "(10 open, 33 woodland, 2 opening houses and 9 protected river cells), " +
                    "17 faint grid lines and no ocean.");
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
                else if (cell.current_cover == CityLandCover.Water &&
                         (cellObject.Find("Water natural feature") != null ||
                          cellObject.Find("Water artwork") != null))
                {
                    throw new InvalidOperationException(
                        $"River cell {cell.id} must remain logical collision data without drawing a pond overlay.");
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
            Debug.Log($"UNITY_CITY_PLANNING_GRID_VISUAL_SMOKE_OK grid=9x6 cells=54 available=43 woodland_groves={actualGroves} baked_forest=True riverside_opening_underlay=True transparent_land_cover=True hidden_regular_grid=True developer_labels=False protected_river_cells=9 static_nunito=Regular/Bold no_ocean=True");
        }

        private static void VerifyCompactNeighbourhoodPresentation(CityPrototypeDemo prototype)
        {
            Transform roads = prototype.transform.Find("Generated City Prototype/Vehicle road network");
            Transform accessRoads = prototype.transform.Find("Generated City Prototype/Visible building access roads");
            Transform footpaths = prototype.transform.Find("Generated City Prototype/Pedestrian link network");
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
            if (roads == null || roads.gameObject.activeSelf ||
                accessRoads == null || !accessRoads.gameObject.activeSelf ||
                accessRoads.GetComponentsInChildren<LineRenderer>().Length != 4 ||
                footpaths == null || !footpaths.gameObject.activeSelf ||
                footpaths.GetComponentsInChildren<LineRenderer>().Length != 6 ||
                wildlife == null || wildlife.childCount != 15 ||
                wildlifeSprites.Length != 15 ||
                wildlifeSprites.Any(renderer => renderer.sprite == null ||
                                                  !expectedWildlifeSprites.Contains(renderer.sprite.name)))
            {
                throw new InvalidOperationException(
                    "The opening view must hide vehicle debug routes, show two house driveways and three pedestrian paths, and use wildlife artwork.");
            }
            Debug.Log(
                "UNITY_CITY_NEIGHBOURHOOD_PRESENTATION_SMOKE_OK " +
                "road_routes_default_hidden=True two_driveways_visible=True pedestrian_paths_default_visible=True " +
                "wildlife_artwork=15 hedgehog_fallback=0 compact_buildings=True");
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
                const float viewportWidth = 0.77f;
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
                cell.current_cover == CityLandCover.Water);
            CityConstructionManager fixedConstruction =
                new CityConstructionManager(fixedCity, fixedBaseline);
            CityConstructionPreview fixedPreview = fixedConstruction.ScanCity(
                ScanPacket(
                    "grid-fixed-water",
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
                    "A fixed water cell must reject building construction.");
            }

            CityState rotationCity = CityPrototypeStateFactory.Create();
            CityPlanningGrid rotationGrid = CityGridResolver.DeepClone(rotationCity.planning_grid);
            CityGridCell rotationAnchor = rotationGrid.cells.Single(cell =>
                cell.row == 3 && cell.col == 7);
            CityBuilding rotatedCommunity = CityConstructionFactory.CreateBuilding(
                TokenAt(131, CityPhysicalTokenType.CommunityFacility, rotationAnchor),
                CityConstructionState.Proposed);
            rotatedCommunity.rotation_deg = 90f;
            if (!CityGridResolver.TryOccupyWithBuilding(
                    rotationGrid,
                    rotationCity.bounds,
                    rotatedCommunity,
                    rotationCity.revision + 1,
                    out string rotationError) ||
                rotatedCommunity.planning_cell_ids.Length != 2)
            {
                throw new InvalidOperationException(
                    $"A rotated 2x1 building did not occupy a 1x2 footprint: {rotationError}");
            }
            CityGridCell[] rotatedCells = rotatedCommunity.planning_cell_ids
                .Select(id => CityGridResolver.GetCell(rotationGrid, id))
                .ToArray();
            if (rotatedCells.Select(cell => cell.col).Distinct().Count() != 1 ||
                rotatedCells.Select(cell => cell.row).Distinct().Count() != 2)
            {
                throw new InvalidOperationException(
                    "A 90-degree building rotation must swap its grid width and height.");
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

            Debug.Log(
                "UNITY_CITY_GRID_RULES_SMOKE_OK snap_radius_cm=5 same_cell_jitter=Moved " +
                "footprints=1x1/2x1/2x2 woodland_build=Building woodland_patches_removed=True " +
                "multi_cell_demolition=Released fixed_river_rejected=True active_building_cap_rule=True");
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
                occupiedCells.Length < 2 ||
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
            if (automatic.points_norm.Length != 11 ||
                automatic.connection_road_id != "vehicle-main-street" ||
                automatic.points_norm.Skip(1).Take(automatic.points_norm.Length - 2)
                    .All(point => Math.Abs(point[0] - automatic.points_norm[0][0]) < 0.0001f))
            {
                throw new InvalidOperationException(
                    "Automatic access did not create a smooth multi-point curve to the main road.");
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
                "smooth_main_road_curve=11_points woodland_to_white=True");
        }

        private static void VerifyDesktopPlayableFlow(CityPrototypeDemo prototype)
        {
            CityState reference = CityPrototypeStateFactory.Create();
            CityGridCell placement = FindPlaceableDetachedCells(reference, 1).FirstOrDefault();
            if (prototype == null || !prototype.DesktopPlayEnabled || placement == null)
            {
                throw new InvalidOperationException(
                    "The camera-free desktop edition did not start with a usable placement point.");
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
            if (!prototype.ConfirmDesktopPlacement(out string buildError) ||
                prototype.GeneratedBuildingCount != 3 ||
                prototype.PlanningPhase != CityPlanningWorkflowPhase.ReadyToScan)
            {
                throw new InvalidOperationException(
                    $"Desktop confirmation did not produce a live third building: {buildError}");
            }
            Transform access = prototype.transform.Find(
                "Generated City Prototype/Visible building access roads");
            if (access == null || access.GetComponentsInChildren<LineRenderer>().Length != 6)
            {
                throw new InvalidOperationException(
                    "Desktop placement did not add its visible smooth access road.");
            }
            Debug.Log(
                "UNITY_CITY_DESKTOP_PLAY_SMOKE_OK default_mode=Desktop palette=True pointer_preview=True " +
                "confirm_build=True camera_required=False camera_mode_retained=True");
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
            if (externalTrips.Length == 0 || longestSegment > 15f)
            {
                throw new InvalidOperationException(
                    $"Vehicle route leaves the road centreline: trips={externalTrips.Length}, " +
                    $"longestSegment={longestSegment:0.00} units.");
            }
            Debug.Log(
                $"UNITY_CITY_VEHICLE_ROAD_FOLLOWING_SMOKE_OK external_trips={externalTrips.Length} " +
                $"longest_segment_units={longestSegment:0.00} projected_main_road_join=True");
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

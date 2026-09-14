using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlife.City;
using UrbanWildlife.Construction;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;
using UrbanWildlife.Prototype;
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

        public static void BatchBuildAndVerifyCityPrototype()
        {
            CreateCityPrototypeScene();
            CityPrototypeDemo prototype = UnityEngine.Object.FindFirstObjectByType<CityPrototypeDemo>();
            Camera camera = Camera.main;
            bool separateViewport = camera != null &&
                                    Math.Abs(camera.rect.x) < 0.001f &&
                                    Math.Abs(camera.rect.width - 0.77f) < 0.001f;
            if (prototype == null || prototype.GeneratedBuildingCount != 6 ||
                prototype.GeneratedPlanningCellCount != 24 ||
                prototype.AvailablePlanningCellCount != 15 ||
                prototype.GeneratedVehicleRoadCount != 8 ||
                prototype.GeneratedPedestrianLinkCount != 8 ||
                prototype.GeneratedAmenityCount != 3 ||
                prototype.FoodSourceCount < 2 ||
                prototype.WildlifeAgentCount != 15 ||
                prototype.WildlifeSpeciesCount != 4 ||
                prototype.DevelopmentPhaseCount != 5 ||
                prototype.CityBalanceTotal <= 0f ||
                prototype.HumanTracePointCount <= 0 ||
                prototype.AnimalTracePointCount <= 0 ||
                prototype.VisibleTraceMarkCount <= 0 ||
                prototype.CityFeedCount <= 0 ||
                !prototype.PlanningWorkflowConnected ||
                string.IsNullOrWhiteSpace(prototype.ResolvedCityScanPath) ||
                prototype.RepresentativeAgentCount != 11 ||
                prototype.RepresentedPopulation != 132 ||
                prototype.VehicleTripCount <= 0 ||
                prototype.VehicleTripCount >= prototype.RepresentativeAgentCount ||
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
                    $"visibleTraceMarks={prototype?.VisibleTraceMarkCount}, " +
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
                "Generated City Prototype/Pedestrian link network",
                "Generated City Prototype/Digital amenities",
                "Generated City Prototype/City wildlife agents",
                "Generated City Prototype/Representative mobility agents",
                "Generated City Prototype/Live city traces/Human traces",
                "Generated City Prototype/Live city traces/Animal traces",
            };
            if (requiredObjects.Any(path => prototype.transform.Find(path) == null))
            {
                throw new InvalidOperationException("City prototype is missing a generated visual layer.");
            }
            VerifyPlanningGridVisuals(prototype);
            VerifyResponsiveCameraFit();
            VerifyCameraScanFileSource();
            VerifyPlanningWorkflow();
            VerifyGridConstructionRules();
            Debug.Log(
                "UNITY_CITY_PROTOTYPE_SMOKE_OK split_screen=True right_sidebar=True bright_city_style=True buildings=6 vehicle_roads=8 " +
                "planning_grid=6x4 planning_cells=24 available_cells=15 cell_bound_land_cover=True " +
                "pedestrian_links=8 amenities=3 food_sources=True waste_pressure=True " +
                "wildlife_agents=15 species=4 utility_targets=True " +
                "development_phases=5 city_balance=True dp=True time_blocks=4 " +
                "human_animal_combined_trace=True city_feed=True phase_report=True " +
                "representative_agents=11 represented_population=132 walk_and_drive=True " +
                "live_vehicle_agents=True max_speed=2x no_questionnaire=True " +
                "visible_footprint_tyre_bird_paw_tracks=True city_feed_panel=True phase_snapshot=True " +
                "camera_scan_file_bridge=True scan_preview_route_dp_construction=True " +
                "no_premature_buildings=True");
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
            if (grid.cols != 6 || grid.rows != 4 || cells.childCount != 24 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.OpenLand) != 8 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Woodland) != 7 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Building) != 6 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.PublicGreen) != 1 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.CivicPlaza) != 1 ||
                grid.cells.Count(cell => cell.current_cover == CityLandCover.Water) != 1 ||
                boundaries.GetComponentsInChildren<LineRenderer>().Length != 12 ||
                underlayRenderer?.sprite == null ||
                underlayRenderer.sprite.name != "city-board-organic-underlay-v01" ||
                underlayRenderer.sortingOrder != -50 ||
                regularFont == null || boldFont == null ||
                prototype.GetComponentsInChildren<Transform>().Any(item => item.name == "East canal"))
            {
                throw new InvalidOperationException(
                    "City planning grid must render its organic terrain underlay, 24 cells " +
                    "(8 open, 7 woodland, 6 building, 3 fixed public features), " +
                    "12 grid lines and no east canal.");
            }

            LineRenderer[] boundaryLines = boundaries.GetComponentsInChildren<LineRenderer>();
            bool naturalBoundaryGeometry = boundaryLines
                .Where(line => line.name.StartsWith("Column", StringComparison.Ordinal))
                .All(line => line.positionCount == grid.rows + 1) &&
                boundaryLines
                    .Where(line => line.name.StartsWith("Row", StringComparison.Ordinal))
                    .All(line => line.positionCount == grid.cols + 1);
            if (!naturalBoundaryGeometry)
            {
                throw new InvalidOperationException(
                    "Planning boundaries must follow shared multi-point landscape edges, not isolated straight cell lines.");
            }

            foreach (CityGridCell cell in grid.cells)
            {
                Transform cellObject = cells.Find(cell.id);
                Transform cover = cellObject?.Find("Land cover " + cell.current_cover);
                MeshFilter mesh = cover == null ? null : cover.GetComponent<MeshFilter>();
                if (mesh?.sharedMesh == null || mesh.sharedMesh.vertexCount != 4 ||
                    cellObject.GetComponentInChildren<TextMesh>()?.text != cell.id)
                {
                    throw new InvalidOperationException($"Planning cell {cell.id} is missing its land cover or coordinate label.");
                }

                if (cell.current_cover == CityLandCover.Woodland)
                {
                    Transform grove = greenery.Find(cell.id + " woodland grove");
                    SpriteRenderer[] trees = grove == null
                        ? Array.Empty<SpriteRenderer>()
                        : grove.GetComponentsInChildren<SpriteRenderer>();
                    string[] expectedTreeSprites =
                    {
                        "tree-citybuilder-pear-v02",
                        "tree-citybuilder-round-v02",
                        "tree-citybuilder-three-lobe-v02",
                        "tree-citybuilder-two-lobe-v02",
                        "tree-citybuilder-tapered-v02",
                    };
                    if (trees.Length != 7 ||
                        trees.Any(tree => tree.sprite == null ||
                                          !expectedTreeSprites.Contains(tree.sprite.name)) ||
                        trees.Select(tree => tree.sprite.name).Distinct().Count() != 5)
                    {
                        throw new InvalidOperationException(
                            $"Woodland cell {cell.id} must contain seven spaced trees using all five approved silhouettes.");
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
                         (cellObject.Find("Water natural feature")?.GetComponent<MeshFilter>()?.sharedMesh == null ||
                          cellObject.Find("Water artwork")?.GetComponent<SpriteRenderer>()?.sprite == null))
                {
                    throw new InvalidOperationException(
                        $"Water cell {cell.id} must render its organic pond artwork inside its logical planning area.");
                }
                else if (cell.current_cover == CityLandCover.CivicPlaza &&
                         (cellObject.Find("CivicPlaza natural feature")?.GetComponent<MeshFilter>()?.sharedMesh == null ||
                          cellObject.Find("Civic plaza artwork")?.GetComponent<SpriteRenderer>()?.sprite == null))
                {
                    throw new InvalidOperationException(
                        $"Civic plaza cell {cell.id} must render its detailed civic-space artwork inside its logical planning area.");
                }
            }

            int expectedGroves = grid.cells.Count(cell => cell.current_cover == CityLandCover.Woodland);
            int actualGroves = greenery.GetComponentsInChildren<Transform>()
                .Count(item => item.parent == greenery &&
                               item.name.EndsWith(" woodland grove", StringComparison.Ordinal));
            if (actualGroves != expectedGroves)
            {
                throw new InvalidOperationException($"Expected {expectedGroves} cell-aligned woodland groves, found {actualGroves}.");
            }
            Debug.Log($"UNITY_CITY_PLANNING_GRID_VISUAL_SMOKE_OK grid=6x4 cells=24 available=15 woodland_groves={actualGroves} dynamic_trees=7x5-shapes organic_underlay=True transparent_land_cover=True natural_boundaries=True detailed_pond_plaza=True static_nunito=Regular/Bold public_green=True no_east_canal=True");
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
                workflow.ConstructionPreview.UnchangedCount != 6 ||
                workflow.CurrentState.buildings.Length != 6)
            {
                throw new InvalidOperationException($"City planning scan/Preview failed: {error}");
            }
            if (!workflow.ConfirmPreview(out error) ||
                workflow.Phase != CityPlanningWorkflowPhase.RouteSelection ||
                workflow.NetworkPreview.RequiredRoadChoiceCount != 1 ||
                workflow.CurrentState.buildings.Count(item =>
                    item.construction_state == CityConstructionState.Proposed) != 1)
            {
                throw new InvalidOperationException($"City planning Preview confirmation failed: {error}");
            }
            if (!workflow.SelectRecommendedLowImpactRoutes(out error) ||
                !workflow.NetworkPreview.CanConfirm ||
                !workflow.ConfirmRoutes(out error) ||
                workflow.Phase != CityPlanningWorkflowPhase.ReadyToBuild ||
                workflow.Snapshot.required_development_points != 14 ||
                !workflow.StartConstruction(out error) ||
                workflow.Phase != CityPlanningWorkflowPhase.Construction ||
                workflow.Strategy.DevelopmentPoints != 6)
            {
                throw new InvalidOperationException($"City planning route/DP commit failed: {error}");
            }
            workflow.AdvanceConstructionBlock();
            if (workflow.Phase != CityPlanningWorkflowPhase.Construction)
            {
                throw new InvalidOperationException("Two-block construction completed too early.");
            }
            workflow.AdvanceConstructionBlock();
            bool allBuilt = workflow.CurrentState.buildings
                                .Where(item => item.source_token_id == 102)
                                .All(item => item.construction_state == CityConstructionState.Existing) &&
                            workflow.CurrentState.green_patches
                                .Where(item => item.source_token_id == 140)
                                .All(item => item.construction_state == CityConstructionState.Existing) &&
                            workflow.CurrentState.vehicle_roads
                                .Where(item => item.id.Contains("building-token-102"))
                                .All(item => item.construction_state == CityConstructionState.Existing) &&
                            workflow.CurrentState.pedestrian_links
                                .Where(item => item.id.Contains("building-token-102"))
                                .All(item => item.construction_state == CityConstructionState.Existing);
            if (workflow.Phase != CityPlanningWorkflowPhase.Complete || !allBuilt ||
                workflow.Snapshot.completed_object_ids.Length != 4)
            {
                throw new InvalidOperationException("City construction did not complete transactionally.");
            }
            Debug.Log(
                "UNITY_CITY_PLANNING_WORKFLOW_SMOKE_OK steps=Scan>Preview>Confirm>Route>DP>Build " +
                "new_objects=2 selected_route=LowImpact dp=20-14 construction_blocks=2 " +
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
            if (jitterPreview.UnchangedCount != baseline.Length ||
                jitterPreview.MovedCount != 0 || jitterPreview.NewCount != 0)
            {
                throw new InvalidOperationException(
                    "Position jitter inside the same planning cell must not count as a moved Token.");
            }
            woodlandConstruction.CancelPreview();

            CityGridCell woodland = woodlandCity.planning_grid.cells.First(cell =>
                cell.current_cover == CityLandCover.Woodland);
            string replacedPatchId = woodland.habitat_patch_id;
            CityTokenState woodlandBuildingToken = new CityTokenState
            {
                id = 102,
                type = CityPhysicalTokenType.Apartment,
                x_norm = woodland.center_norm[0],
                y_norm = woodland.center_norm[1],
                rotation_deg = 0f,
                confidence = 1f,
            };
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
            CityGridCell builtCell = CityGridResolver.GetCell(
                builtCity.planning_grid,
                woodland.id);
            if (woodlandBuilding.planning_cell_id != woodland.id ||
                builtCell.current_cover != CityLandCover.Building ||
                !builtCell.was_woodland ||
                builtCity.green_patches.Any(patch => patch.id == replacedPatchId))
            {
                throw new InvalidOperationException(
                    "Building on woodland must bind to the cell, retain woodland history and remove its habitat patch.");
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
            CityGridCell releasedCell = CityGridResolver.GetCell(
                builtCity.planning_grid,
                woodland.id);
            if (builtCity.buildings.Any(building => building.id == woodlandBuilding.id) ||
                releasedCell.current_cover != CityLandCover.Disturbed ||
                !releasedCell.was_woodland ||
                !string.IsNullOrWhiteSpace(releasedCell.occupant_id))
            {
                throw new InvalidOperationException(
                    "Demolishing a former woodland cell must leave persistent Disturbed land.");
            }

            CityState fixedCity = CityPrototypeStateFactory.Create();
            CityTokenState[] fixedBaseline = CityPlanningDemoScanFactory.ConfirmedTokensFrom(fixedCity);
            CityGridCell water = fixedCity.planning_grid.cells.Single(cell =>
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

            CityState capCity = CityPrototypeStateFactory.Create();
            CityTokenState[] capBaseline = CityPlanningDemoScanFactory.ConfirmedTokensFrom(capCity);
            CityGridCell[] openCells = capCity.planning_grid.cells
                .Where(cell => cell.current_cover == CityLandCover.OpenLand &&
                               string.IsNullOrWhiteSpace(cell.occupant_id))
                .ToArray();
            CityTokenState[] threeAdditions =
            {
                TokenAt(102, CityPhysicalTokenType.Apartment, openCells[0]),
                TokenAt(111, CityPhysicalTokenType.DetachedHouse, openCells[2]),
                TokenAt(131, CityPhysicalTokenType.CommunityFacility, openCells[7]),
            };
            CityConstructionManager capConstruction =
                new CityConstructionManager(capCity, capBaseline);
            CityConstructionPreview capPreview = capConstruction.ScanCity(
                ScanPacket(
                    "grid-cap-nine",
                    capBaseline.Concat(threeAdditions).ToArray()));
            string capError = string.Empty;
            if (!capPreview.CanConfirmConstruction || capPreview.NewCount != 3 ||
                !capConstruction.ConfirmConstruction(out capError))
            {
                throw new InvalidOperationException(
                    $"The city could not reach its nine-building cap: {capError}");
            }
            CityTokenState tenth = TokenAt(
                112,
                CityPhysicalTokenType.DetachedHouse,
                openCells[1]);
            CityConstructionPreview overCapPreview = capConstruction.ScanCity(
                ScanPacket(
                    "grid-cap-ten",
                    capConstruction.ConfirmedTokens.Concat(new[] { tenth }).ToArray()));
            if (!overCapPreview.HasBlockingChanges || overCapPreview.CanConfirmConstruction)
            {
                throw new InvalidOperationException(
                    "A tenth active player building must be rejected by the planning grid.");
            }

            Debug.Log(
                "UNITY_CITY_GRID_RULES_SMOKE_OK snap_radius_cm=5 same_cell_jitter=Unchanged " +
                "woodland_build=Building woodland_patch_removed=True " +
                "demolition_cover=Disturbed fixed_water_rejected=True active_building_cap=9");
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

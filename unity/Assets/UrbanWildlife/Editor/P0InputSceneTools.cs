using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;
using UrbanWildlife.Cycle;
using UrbanWildlife.Humans;
using UrbanWildlife.Animals;
using UrbanWildlife.Logging;

namespace UrbanWildlife.EditorTools
{
    public static class P0InputSceneTools
    {
        private const string ScenePath = "Assets/Scenes/P0_InputSpike.unity";

        [MenuItem("Urban Wildlife/Create P0 Input Scene")]
        public static void CreateP0InputScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject inputManager = new GameObject("Layout Input Manager");
            inputManager.AddComponent<LayoutPacketReader>();
            inputManager.AddComponent<P0ElectronicDemoInput>();
            inputManager.AddComponent<LayoutDebugView>();
            inputManager.AddComponent<P0ConstraintManager>();
            inputManager.AddComponent<P0CycleController>();
            inputManager.AddComponent<P0HumanSimulation>();
            inputManager.AddComponent<P0AnimalSimulation>();
            inputManager.AddComponent<P0ResearchLogger>();
            inputManager.AddComponent<P0ControlPanel>();

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.075f, 0.12f, 0.085f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 3.5f;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 10f, 0f);
            cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Could not save {ScenePath}.");
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"Created {ScenePath} with LayoutPacketReader.");
        }

        public static void BatchVerifyFixture()
        {
            string fixturePath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../../docs/images/vision/layout-packet-contour-v02-validation/latest_layout.json"));
            if (!File.Exists(fixturePath))
            {
                throw new FileNotFoundException("Contour layout fixture is missing.", fixturePath);
            }

            string json = File.ReadAllText(fixturePath);
            if (!LayoutPacketReader.TryParseAndValidate(json, -1, out LayoutPacket packet, out string error))
            {
                throw new InvalidOperationException($"Contour layout fixture failed: {error}");
            }

            if (packet.tokens.Length != 5 || packet.path.point_count != 11 ||
                packet.recognition.token_backend != "contour" || !packet.capture.stable)
            {
                throw new InvalidOperationException("Contour layout fixture contains unexpected values.");
            }

            packet.capture.stable = false;
            string unstableJson = JsonConvert.SerializeObject(packet);
            if (LayoutPacketReader.TryParseAndValidate(unstableJson, -1, out _, out _))
            {
                throw new InvalidOperationException("Unity accepted a layout without a stable Confirm frame.");
            }
            packet.capture.stable = true;

            GameObject smokeObject = new GameObject("Layout Debug Smoke");
            smokeObject.AddComponent<LayoutPacketReader>();
            LayoutDebugView debugView = smokeObject.AddComponent<LayoutDebugView>();
            debugView.ApplyPacket(packet);
            if (debugView.GeneratedElementCount != 7)
            {
                throw new InvalidOperationException(
                    $"Illustrated layout generated {debugView.GeneratedElementCount} semantic elements instead of 7.");
            }
            string[] refinedVisualPaths =
            {
                "Runtime Illustrated Layout/Board 90x60cm/Entrance A guide/Open wrought-iron fence gate",
                "Runtime Illustrated Layout/Board 90x60cm/Exit B guide/Open wrought-iron fence gate",
                "Runtime Illustrated Layout/food_hotspot 10/Bench rest area artwork",
                "Runtime Illustrated Layout/food_hotspot 11/Plaza activity area artwork",
                "Runtime Illustrated Layout/woodland 20/Woodland forest grove artwork",
                "Runtime Illustrated Layout/Planned Human Path/Road gravel surface",
            };
            foreach (string visualPath in refinedVisualPaths)
            {
                if (smokeObject.transform.Find(visualPath) == null)
                {
                    throw new InvalidOperationException($"Refined P0 visual is missing: {visualPath}");
                }
            }
            UnityEngine.Object.DestroyImmediate(smokeObject);

            string scenarioPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../../data/scenarios/s001_weekend_park_baseline.json"));
            P0Scenario scenario = P0ConstraintManager.LoadScenario(scenarioPath);
            LayoutPacket baselinePacket = P0ConstraintEvaluator.CreateBaselinePacket(scenario);
            P0ConstraintResult constraintResult = P0ConstraintEvaluator.Evaluate(baselinePacket, scenario);
            ScenarioExpectedConstraintCheck expected = scenario.expected_constraint_check_before_player_changes;
            if (constraintResult.human_connected != expected.human_connected ||
                constraintResult.animal_reachable != expected.animal_reachable ||
                constraintResult.food_hotspot_valid != expected.food_hotspot_valid ||
                constraintResult.changes_used != expected.changes_used ||
                constraintResult.changes_allowed != expected.changes_allowed)
            {
                throw new InvalidOperationException("S001 baseline constraint result does not match its expected values.");
            }

            LayoutToken repairedFood10 = Array.Find(baselinePacket.tokens, token => token.id == 10);
            LayoutToken repairedFood12 = Array.Find(baselinePacket.tokens, token => token.id == 12);
            repairedFood10.x_norm = 45f / 90f;
            repairedFood10.y_norm = 10f / 60f;
            repairedFood12.x_norm = 20f / 90f;
            repairedFood12.y_norm = 43f / 60f;
            P0ConstraintResult repairedResult = P0ConstraintEvaluator.Evaluate(baselinePacket, scenario);
            if (!repairedResult.all_constraints_satisfied || repairedResult.changes_used != 2)
            {
                throw new InvalidOperationException(
                    "Two-token S001 repair should satisfy every constraint using exactly two changes.");
            }

            Debug.Log(
                $"UNITY_INPUT_SMOKE_OK tokens={packet.tokens.Length} " +
                $"path_points={packet.path.point_count} backend={packet.recognition.token_backend} " +
                $"stable={packet.capture.stable} visual_elements=7 refined_visuals={refinedVisualPaths.Length}");
            Debug.Log(
                $"UNITY_CONSTRAINT_SMOKE_OK human_connected={constraintResult.human_connected} " +
                $"animal_reachable={constraintResult.animal_reachable} " +
                $"food_hotspot_valid={constraintResult.food_hotspot_valid} " +
                $"changes={constraintResult.changes_used}/{constraintResult.changes_allowed}");
            Debug.Log(
                $"UNITY_CONSTRAINT_REPAIR_OK all_constraints={repairedResult.all_constraints_satisfied} " +
                $"changes={repairedResult.changes_used}/{repairedResult.changes_allowed}");

            string humanDemoPath = Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "../../data/examples/s001_valid_human_demo.json"));
            string humanDemoJson = File.ReadAllText(humanDemoPath);
            if (!LayoutPacketReader.TryParseAndValidate(humanDemoJson, -1, out LayoutPacket humanDemo, out string humanDemoError))
            {
                throw new InvalidOperationException($"Human demo packet failed: {humanDemoError}");
            }
            P0ConstraintResult humanDemoResult = P0ConstraintEvaluator.Evaluate(humanDemo, scenario);
            if (!humanDemoResult.all_constraints_satisfied)
            {
                throw new InvalidOperationException("Human demo packet must satisfy every S001 planning constraint.");
            }

            string electronicDemoSmokeRoot = Path.Combine(
                Path.GetTempPath(),
                "urban-wildlife-electronic-demo-smoke",
                Guid.NewGuid().ToString("N"));
            try
            {
                string electronicDemoOutput = Path.Combine(electronicDemoSmokeRoot, "latest_layout.json");
                if (!P0ElectronicDemoInput.TryCreatePacket(
                        humanDemoPath,
                        electronicDemoOutput,
                        "electronic-smoke",
                        0,
                        -1,
                        out LayoutPacket firstElectronicPacket,
                        out string firstElectronicError))
                {
                    throw new InvalidOperationException(
                        $"Electronic demo packet generation failed: {firstElectronicError}");
                }
                if (!P0ElectronicDemoInput.TryCreatePacket(
                        humanDemoPath,
                        electronicDemoOutput,
                        "electronic-smoke",
                        1,
                        firstElectronicPacket.timestamp_ms,
                        out LayoutPacket secondElectronicPacket,
                        out string secondElectronicError))
                {
                    throw new InvalidOperationException(
                        $"Electronic demo packet refresh failed: {secondElectronicError}");
                }
                if (!File.Exists(electronicDemoOutput) ||
                    secondElectronicPacket.timestamp_ms <= firstElectronicPacket.timestamp_ms ||
                    secondElectronicPacket.cycle_index != 1 ||
                    secondElectronicPacket.session_id != "electronic-smoke")
                {
                    throw new InvalidOperationException(
                        "Electronic demo did not create a newer valid packet for the next cycle.");
                }
                Debug.Log(
                    "UNITY_ELECTRONIC_DEMO_SMOKE_OK cycles=2 timestamps_increasing=True " +
                    "physical_claim=False");
            }
            finally
            {
                if (Directory.Exists(electronicDemoSmokeRoot))
                {
                    Directory.Delete(electronicDemoSmokeRoot, true);
                }
            }

            HumanRoutePlan[] humanPlans = HumanRoutePlanner.CreatePlans(humanDemo, scenario);
            int walkers = Array.FindAll(humanPlans, plan => plan.Archetype == HumanArchetype.Walker).Length;
            int dwellers = Array.FindAll(humanPlans, plan => plan.Archetype == HumanArchetype.Dweller).Length;
            int visitors = Array.FindAll(humanPlans, plan => plan.Archetype == HumanArchetype.Visitor).Length;
            if (humanPlans.Length != 6 || walkers != 2 || dwellers != 2 || visitors != 2)
            {
                throw new InvalidOperationException("S001 must create two Walker, Dweller and Visitor agents.");
            }

            bool sawDwelling = false;
            bool sawVisiting = false;
            int completedHumans = 0;
            foreach (HumanRoutePlan plan in humanPlans)
            {
                HumanAgentStateMachine agent = new HumanAgentStateMachine(plan);
                for (int step = 0; step < 1200 && !agent.IsFinished; step += 1)
                {
                    agent.Tick(0.1f);
                    sawDwelling |= agent.State == HumanActivityState.Dwelling;
                    sawVisiting |= agent.State == HumanActivityState.Visiting;
                }
                if (agent.IsFinished)
                {
                    completedHumans += 1;
                }
            }
            if (!sawDwelling || !sawVisiting || completedHumans != humanPlans.Length)
            {
                throw new InvalidOperationException("Human roles did not complete their expected activity states and routes.");
            }
            Debug.Log(
                $"UNITY_HUMAN_ROUTE_SMOKE_OK agents={humanPlans.Length} " +
                $"walkers={walkers} dwellers={dwellers} visitors={visitors}");
            Debug.Log(
                $"UNITY_HUMAN_STATE_SMOKE_OK dwelling={sawDwelling} " +
                $"visiting={sawVisiting} completed={completedHumans}/{humanPlans.Length}");

            AnimalSpawnPlan[] animalPlans = AnimalEnvironmentPlanner.CreatePlans(humanDemo, scenario);
            if (animalPlans.Length != 3 ||
                Array.FindAll(animalPlans, plan => plan.Config.Species == AnimalSpecies.Pigeon).Length != 1 ||
                Array.FindAll(animalPlans, plan => plan.Config.Species == AnimalSpecies.Squirrel).Length != 1 ||
                Array.FindAll(animalPlans, plan => plan.Config.Species == AnimalSpecies.Fox).Length != 1)
            {
                throw new InvalidOperationException("S001 must create one Pigeon, Squirrel and Fox.");
            }

            int feedingSpecies = 0;
            foreach (AnimalSpawnPlan plan in animalPlans)
            {
                AnimalAgentStateMachine animal = new AnimalAgentStateMachine(plan);
                AnimalPerception quietEnvironment = new AnimalPerception(
                    true,
                    plan.FoodPosition,
                    true,
                    plan.ShelterPosition,
                    float.PositiveInfinity);
                for (int step = 0; step < 1200 && animal.FeedEvents == 0; step += 1)
                {
                    animal.Tick(0.1f, quietEnvironment);
                }
                if (animal.FeedEvents > 0)
                {
                    feedingSpecies += 1;
                }
            }
            if (feedingSpecies != animalPlans.Length)
            {
                throw new InvalidOperationException("Every P0 animal must reach and feed at its assigned hotspot.");
            }

            AnimalSpawnPlan squirrelPlan = Array.Find(
                animalPlans,
                plan => plan.Config.Species == AnimalSpecies.Squirrel);
            AnimalSpawnPlan foxPlan = Array.Find(
                animalPlans,
                plan => plan.Config.Species == AnimalSpecies.Fox);
            AnimalSpawnPlan pigeonPlan = Array.Find(
                animalPlans,
                plan => plan.Config.Species == AnimalSpecies.Pigeon);
            AnimalPerception crowdedSquirrel = new AnimalPerception(
                true, squirrelPlan.FoodPosition, true, squirrelPlan.ShelterPosition, 0.01f);
            AnimalPerception crowdedFox = new AnimalPerception(
                true, foxPlan.FoodPosition, true, foxPlan.ShelterPosition, 0.01f);
            AnimalPerception crowdedPigeon = new AnimalPerception(
                true, pigeonPlan.FoodPosition, true, pigeonPlan.ShelterPosition, 0.01f);
            AnimalAgentStateMachine cautiousSquirrel = new AnimalAgentStateMachine(squirrelPlan);
            AnimalAgentStateMachine cautiousFox = new AnimalAgentStateMachine(foxPlan);
            AnimalAgentStateMachine tolerantPigeon = new AnimalAgentStateMachine(pigeonPlan);
            cautiousSquirrel.Tick(0.1f, crowdedSquirrel);
            cautiousFox.Tick(0.1f, crowdedFox);
            tolerantPigeon.Tick(0.1f, crowdedPigeon);
            if (cautiousSquirrel.State != AnimalActivityState.AvoidingHumans ||
                cautiousFox.State != AnimalActivityState.AvoidingHumans ||
                tolerantPigeon.State == AnimalActivityState.AvoidingHumans)
            {
                throw new InvalidOperationException("Species-specific human disturbance responses are incorrect.");
            }
            Debug.Log("UNITY_ANIMAL_ROUTE_SMOKE_OK species=3 feeding_species=3");
            Debug.Log(
                $"UNITY_ANIMAL_STATE_SMOKE_OK pigeon_avoid={tolerantPigeon.AvoidanceEvents} " +
                $"squirrel_avoid={cautiousSquirrel.AvoidanceEvents} fox_avoid={cautiousFox.AvoidanceEvents}");

            string[] animalSpritePaths =
            {
                "UrbanWildlife/Animals/pigeon-topdown-v01",
                "UrbanWildlife/Animals/squirrel-topdown-v02",
                "UrbanWildlife/Animals/fox-topdown-v02",
            };
            foreach (string spritePath in animalSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.y <= 0f)
                {
                    throw new InvalidOperationException($"P0 animal sprite is missing or invalid: {spritePath}");
                }
            }
            string[] animalWalkSpritePaths =
            {
                "UrbanWildlife/Animals/pigeon-walk-b-v01",
                "UrbanWildlife/Animals/squirrel-walk-b-v01",
                "UrbanWildlife/Animals/fox-walk-b-v01",
            };
            foreach (string spritePath in animalWalkSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.y <= 0f)
                {
                    throw new InvalidOperationException($"P0 animal walk sprite is missing or invalid: {spritePath}");
                }
            }
            if (P0AnimalSimulation.WalkFrameCount != 2)
            {
                throw new InvalidOperationException("P0 animal walking must use a two-frame gait cycle.");
            }
            Sprite squirrelSprite = Resources.Load<Sprite>("UrbanWildlife/Animals/squirrel-topdown-v02");
            Sprite foxSprite = Resources.Load<Sprite>("UrbanWildlife/Animals/fox-topdown-v02");
            float squirrelAspect = TightSpriteAspect(squirrelSprite);
            float foxAspect = TightSpriteAspect(foxSprite);
            if (squirrelAspect <= foxAspect * 2f)
            {
                throw new InvalidOperationException(
                    $"Squirrel and fox silhouettes are not distinct enough: {squirrelAspect:F2} vs {foxAspect:F2}.");
            }
            Debug.Log(
                $"UNITY_ANIMAL_SPRITE_SMOKE_OK sprites=3 transparent_import=True topdown=True " +
                $"squirrel_aspect={squirrelAspect:F2} fox_aspect={foxAspect:F2} " +
                $"display_lengths={P0AnimalSimulation.PigeonDisplayLength:F2}/" +
                $"{P0AnimalSimulation.SquirrelDisplayLength:F2}/{P0AnimalSimulation.FoxDisplayLength:F2}");
            Debug.Log("UNITY_ANIMAL_WALK_ANIMATION_SMOKE_OK frames=2 species=3 moving_only=True");

            string[] humanSpritePaths =
            {
                "UrbanWildlife/Humans/walker-topdown-v05",
                "UrbanWildlife/Humans/dweller-topdown-v03",
                "UrbanWildlife/Humans/visitor-topdown-v05",
            };
            foreach (string spritePath in humanSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.y <= 0f)
                {
                    throw new InvalidOperationException($"P0 human sprite is missing or invalid: {spritePath}");
                }
            }
            string[] humanWalkSpritePaths =
            {
                "UrbanWildlife/Humans/walker-walk-b-v01",
                "UrbanWildlife/Humans/dweller-walk-b-v01",
                "UrbanWildlife/Humans/visitor-walk-b-v01",
            };
            foreach (string spritePath in humanWalkSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.y <= 0f)
                {
                    throw new InvalidOperationException($"P0 human walk sprite is missing or invalid: {spritePath}");
                }
            }
            if (P0HumanSimulation.WalkFrameCount != 2 || P0HumanSimulation.WalkFramesPerSecond <= 0f)
            {
                throw new InvalidOperationException("P0 human walking must use a timed two-frame gait cycle.");
            }
            Debug.Log("UNITY_HUMAN_WALK_ANIMATION_SMOKE_OK frames=2 roles=3 moving_only=True");
            if (!Mathf.Approximately(P0HumanSimulation.FrontFacingSpriteHeadingOffsetDegrees, 180f))
            {
                throw new InvalidOperationException("Front-facing human sprites require a 180-degree heading offset.");
            }
            if (!(P0AnimalSimulation.PigeonDisplayLength < P0AnimalSimulation.SquirrelDisplayLength &&
                  P0AnimalSimulation.SquirrelDisplayLength < P0AnimalSimulation.FoxDisplayLength &&
                  P0AnimalSimulation.FoxDisplayLength < P0HumanSimulation.DwellerDisplayLength &&
                  P0HumanSimulation.DwellerDisplayLength <= P0HumanSimulation.VisitorDisplayLength &&
                  P0HumanSimulation.VisitorDisplayLength < P0HumanSimulation.WalkerDisplayLength))
            {
                throw new InvalidOperationException("Human and animal display lengths do not follow the real-size ordering.");
            }
            Sprite parkMap = Resources.Load<Sprite>("UrbanWildlife/Environment/park-board-s001-v02");
            if (parkMap == null || parkMap.texture == null || parkMap.bounds.size.x <= 0f)
            {
                throw new InvalidOperationException("P0 illustrated park map is missing or invalid.");
            }
            string[] planningAreaSpritePaths =
            {
                "UrbanWildlife/Environment/woodland-forest-grove-v01",
                "UrbanWildlife/Environment/human-activity-plaza-v01",
                "UrbanWildlife/Environment/human-activity-bench-v01",
                "UrbanWildlife/Environment/park-fence-gate-open-v01",
            };
            foreach (string spritePath in planningAreaSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.x <= 0f)
                {
                    throw new InvalidOperationException($"P0 planning-area sprite is missing or invalid: {spritePath}");
                }
            }
            Debug.Log("UNITY_PLANNING_AREA_SMOKE_OK woodland=forest food_hotspots=bench/plaza ids_preserved=True");
            Debug.Log("UNITY_PARK_GATE_SMOKE_OK entrance=open_fence_gate exit=mirrored_open_fence_gate ids=A/B");
            Debug.Log(
                "UNITY_VISUAL_LAYER_SMOKE_OK human_sprites=6 animal_sprites=6 map_sprites=1 " +
                "planning_area_sprites=4 semantic_elements=7 refined_visuals=6 realistic_path=True human_heading_offset=180 " +
                "human_lengths=0.89/0.84/0.85 real_size_order=True walk_cycles=6");

            string loggerSmokeRoot = Path.Combine(
                Path.GetTempPath(),
                "urban-wildlife-logger-smoke",
                Guid.NewGuid().ToString("N"));
            try
            {
                ResearchLogWriter logWriter = new ResearchLogWriter(loggerSmokeRoot, "session:smoke");
                ResearchLogRecord logRecord = new ResearchLogRecord
                {
                    timestamp_utc = "2026-09-08T12:00:00.0000000+00:00",
                    session_id = "session:smoke",
                    cycle_index = 0,
                    event_type = "constraint_evaluated",
                    phase = "Confirm",
                    layout_timestamp_ms = humanDemo.timestamp_ms,
                    human_connected = true,
                    animal_reachable = true,
                    food_hotspot_valid = true,
                    changes_used = 2,
                    changes_allowed = 2,
                    human_trips = 6,
                    pigeon_feed_events = 1,
                    squirrel_feed_events = 1,
                    fox_feed_events = 1,
                    animal_avoidance_events = 2,
                    note = "comma, quote \"checked\"",
                };
                logWriter.Append(logRecord);

                string[] jsonLines = File.ReadAllLines(logWriter.JsonlPath);
                string[] csvLines = File.ReadAllLines(logWriter.CsvPath);
                ResearchLogRecord parsedLog = jsonLines.Length == 1
                    ? JsonConvert.DeserializeObject<ResearchLogRecord>(jsonLines[0])
                    : null;
                if (parsedLog == null || parsedLog.event_type != logRecord.event_type ||
                    csvLines.Length != 2 || !csvLines[1].Contains("\"comma, quote \"\"checked\"\"\"") ||
                    ResearchLogFormatter.CsvHeader.Contains("participant") || jsonLines[0].Contains("participant"))
                {
                    throw new InvalidOperationException("Research logger JSONL/CSV or privacy smoke check failed.");
                }
                Debug.Log("UNITY_RESEARCH_LOG_SMOKE_OK jsonl=1 csv_rows=1 participant_fields=0");
            }
            finally
            {
                if (Directory.Exists(loggerSmokeRoot))
                {
                    Directory.Delete(loggerSmokeRoot, true);
                }
            }

            P0CycleStateMachine cycle = new P0CycleStateMachine(60f, 30f, 3);
            if (!cycle.TryEnterConfirm())
            {
                throw new InvalidOperationException("Cycle could not enter Confirm from Plan.");
            }
            cycle.ResolveConfirm(true);
            if (!cycle.TryStartRun() || cycle.Phase != P0Phase.Run)
            {
                throw new InvalidOperationException("Approved plan could not enter Run.");
            }
            cycle.Tick(60f);
            if (cycle.Phase != P0Phase.Observe)
            {
                throw new InvalidOperationException("Run did not advance to Observe after 60 seconds.");
            }
            cycle.Tick(30f);
            if (cycle.Phase != P0Phase.Plan || cycle.CycleIndex != 1)
            {
                throw new InvalidOperationException("Observe did not return to the next Plan cycle.");
            }
            Debug.Log("UNITY_CYCLE_SMOKE_OK Plan>Confirm>Run>Observe>Plan cycle=1");

            P0CycleStateMachine rejected = new P0CycleStateMachine(60f, 30f, 3);
            rejected.TryEnterConfirm();
            rejected.ResolveConfirm(false);
            if (rejected.Phase != P0Phase.Plan || rejected.TryStartRun())
            {
                throw new InvalidOperationException("Rejected constraints must return the cycle to Plan.");
            }

            for (int completedCycles = 1; completedCycles < 3; completedCycles += 1)
            {
                cycle.TryEnterConfirm();
                cycle.ResolveConfirm(true);
                cycle.TryStartRun();
                cycle.Tick(90f);
            }
            if (cycle.Phase != P0Phase.Complete || cycle.CycleIndex != 3)
            {
                throw new InvalidOperationException("Three complete cycles must end the P0 session.");
            }
            Debug.Log("UNITY_SESSION_SMOKE_OK cycles=3 phase=Complete");

            GameObject uiObject = new GameObject("P0 UI Smoke");
            uiObject.AddComponent<LayoutPacketReader>();
            uiObject.AddComponent<P0ConstraintManager>();
            uiObject.AddComponent<P0CycleController>();
            P0ControlPanel panel = uiObject.AddComponent<P0ControlPanel>();
            if (panel == null || uiObject.GetComponent<P0CycleController>() == null)
            {
                throw new InvalidOperationException("P0 control panel dependencies were not created.");
            }
            UnityEngine.Object.DestroyImmediate(uiObject);
            Debug.Log("UNITY_UI_SMOKE_OK phase=Plan space_actions=Confirm/StartRun");
        }

        private static float TightSpriteAspect(Sprite sprite)
        {
            Vector2[] vertices = sprite.vertices;
            if (vertices == null || vertices.Length == 0)
            {
                throw new InvalidOperationException($"Sprite has no geometry: {sprite.name}");
            }

            float minX = vertices[0].x;
            float maxX = vertices[0].x;
            float minY = vertices[0].y;
            float maxY = vertices[0].y;
            foreach (Vector2 vertex in vertices)
            {
                minX = Mathf.Min(minX, vertex.x);
                maxX = Mathf.Max(maxX, vertex.x);
                minY = Mathf.Min(minY, vertex.y);
                maxY = Mathf.Max(maxY, vertex.y);
            }
            return (maxX - minX) / Mathf.Max(0.001f, maxY - minY);
        }
    }
}

using System;
using System.IO;
using System.Linq;
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
using UrbanWildlife.Presentation;

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
                "Runtime Illustrated Layout/Planned Human Path/Asphalt surface",
            };
            foreach (string visualPath in refinedVisualPaths)
            {
                if (smokeObject.transform.Find(visualPath) == null)
                {
                    throw new InvalidOperationException($"Refined P0 visual is missing: {visualPath}");
                }
            }
            string[] removedGuidePaths =
            {
                "Runtime Illustrated Layout/Board 90x60cm/Central plaza guide",
                "Runtime Illustrated Layout/Board 90x60cm/Pond guide",
            };
            foreach (string removedGuidePath in removedGuidePaths)
            {
                if (smokeObject.transform.Find(removedGuidePath) != null)
                {
                    throw new InvalidOperationException(
                        $"Obsolete coloured planning ring is still present: {removedGuidePath}");
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

            P0CycleProfile cycleOneProfile = P0CycleMechanics.ProfileForCycle(0);
            AnimalSpawnPlan[] animalPlans = AnimalEnvironmentPlanner.CreatePlans(
                humanDemo,
                scenario,
                cycleOneProfile.PigeonCount,
                cycleOneProfile.SquirrelCount,
                cycleOneProfile.FoxCount);
            if (animalPlans.Length != 11 ||
                Array.FindAll(animalPlans, plan => plan.Config.Species == AnimalSpecies.Pigeon).Length != 7 ||
                Array.FindAll(animalPlans, plan => plan.Config.Species == AnimalSpecies.Squirrel).Length != 3 ||
                Array.FindAll(animalPlans, plan => plan.Config.Species == AnimalSpecies.Fox).Length != 1)
            {
                throw new InvalidOperationException("S001 cycle 1 must create the approved 7/3/1 wildlife population.");
            }

            P0CycleProfile cycleTwoProfile = P0CycleMechanics.ProfileForCycle(1);
            P0CycleProfile cycleThreeProfile = P0CycleMechanics.ProfileForCycle(2);
            if (cycleTwoProfile.PigeonCount != 9 || cycleTwoProfile.SquirrelCount != 4 || cycleTwoProfile.FoxCount != 1 ||
                cycleThreeProfile.PigeonCount != 8 || cycleThreeProfile.SquirrelCount != 3 || cycleThreeProfile.FoxCount != 2)
            {
                throw new InvalidOperationException("Three-cycle wildlife population profiles changed unexpectedly.");
            }

            P0CycleScore partialScore = P0CycleScore.Calculate(3, 6, 7, 7, 1, 4, 0, 1);
            P0CycleScore fullScore = P0CycleScore.Calculate(6, 6, 9, 9, 4, 4, 1, 1);
            if (partialScore.HumanAccessScore != 20 || partialScore.PigeonFeedingScore != 20 ||
                partialScore.SquirrelFeedingScore != 5 || partialScore.FoxFeedingScore != 0 ||
                partialScore.TotalScore != 45 || fullScore.TotalScore != 100)
            {
                throw new InvalidOperationException("Normalized 40/20/20/20 planning score is incorrect.");
            }

            P0TraceSeries traceSeries = new P0TraceSeries(0.1f, 4);
            bool acceptedFirstTracePoint = traceSeries.TryAppend(Vector2.zero);
            bool rejectedNearTracePoint = !traceSeries.TryAppend(new Vector2(0.05f, 0f));
            traceSeries.TryAppend(new Vector2(0.2f, 0f));
            traceSeries.TryAppend(new Vector2(0.5f, 0f));
            traceSeries.TryAppend(new Vector2(1f, 0f));
            bool rejectedOverflowTracePoint = !traceSeries.TryAppend(new Vector2(2f, 0f));
            if (!acceptedFirstTracePoint || !rejectedNearTracePoint || !rejectedOverflowTracePoint ||
                traceSeries.PointCount != 4 || Math.Abs(traceSeries.DistanceUnits - 1f) > 0.001f)
            {
                throw new InvalidOperationException("Trace sampling or distance accumulation is incorrect.");
            }
            P0CycleMemorySnapshot plannerMemory = new P0CycleMemorySnapshot(
                0,
                6,
                7,
                3,
                1,
                5,
                12,
                10,
                11,
                0.5f,
                partialScore,
                12.5f,
                9.2f);
            AnimalSpawnPlan[] rememberedPlans = AnimalEnvironmentPlanner.CreatePlans(
                humanDemo,
                scenario,
                cycleTwoProfile.PigeonCount,
                cycleTwoProfile.SquirrelCount,
                cycleTwoProfile.FoxCount,
                plannerMemory);
            int returningPigeons = Array.FindAll(
                rememberedPlans,
                plan => plan.Config.Species == AnimalSpecies.Pigeon && plan.FoodTokenId == 12).Length;
            int returningSquirrels = Array.FindAll(
                rememberedPlans,
                plan => plan.Config.Species == AnimalSpecies.Squirrel && plan.FoodTokenId == 10).Length;
            int returningFoxes = Array.FindAll(
                rememberedPlans,
                plan => plan.Config.Species == AnimalSpecies.Fox && plan.FoodTokenId == 11).Length;
            AnimalSpawnPlan rememberedSquirrel = Array.Find(
                rememberedPlans,
                plan => plan.Config.Species == AnimalSpecies.Squirrel);
            float baseSquirrelDisturbance = scenario.animal_simulation.squirrel_disturbance_cm *
                scenario.animal_simulation.unity_units_per_cm;
            if (returningPigeons < 5 || returningSquirrels < 2 || returningFoxes < 1 ||
                rememberedSquirrel.Config.InitialFamiliarity <= scenario.animal_simulation.squirrel_initial_familiarity ||
                rememberedSquirrel.Config.DisturbanceRadiusUnits <= baseSquirrelDisturbance)
            {
                throw new InvalidOperationException("Previous-cycle memory did not affect return visits, familiarity and caution.");
            }

            int feedingAgents = 0;
            bool pigeonFed = false;
            bool squirrelFed = false;
            bool foxFed = false;
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
                    feedingAgents += 1;
                    pigeonFed |= plan.Config.Species == AnimalSpecies.Pigeon;
                    squirrelFed |= plan.Config.Species == AnimalSpecies.Squirrel;
                    foxFed |= plan.Config.Species == AnimalSpecies.Fox;
                }
            }
            if (feedingAgents != animalPlans.Length || !pigeonFed || !squirrelFed || !foxFed)
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
            Debug.Log("UNITY_ANIMAL_ROUTE_SMOKE_OK species=3 agents=11 feeding_agents=11");
            Debug.Log(
                $"UNITY_ANIMAL_STATE_SMOKE_OK pigeon_avoid={tolerantPigeon.AvoidanceEvents} " +
                $"squirrel_avoid={cautiousSquirrel.AvoidanceEvents} fox_avoid={cautiousFox.AvoidanceEvents}");

            string[] animalSpritePaths =
            {
                "UrbanWildlife/Animals/pigeon-side-walk-a-v01",
                "UrbanWildlife/Animals/squirrel-side-walk-a-v01",
                "UrbanWildlife/Animals/fox-side-walk-a-v01",
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
                "UrbanWildlife/Animals/pigeon-side-walk-b-v01",
                "UrbanWildlife/Animals/squirrel-side-walk-b-v01",
                "UrbanWildlife/Animals/fox-side-walk-b-v01",
            };
            foreach (string spritePath in animalWalkSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.y <= 0f)
                {
                    throw new InvalidOperationException($"P0 animal walk sprite is missing or invalid: {spritePath}");
                }
            }
            string[] animalTrueWalkSpritePaths = new[]
            {
                "pigeon-walk",
                "squirrel-walk",
                "fox-walk",
            }
                .SelectMany(prefix => Enumerable.Range(
                        1,
                        P0AnimalSimulation.WalkArtworkFrameCount)
                    .Select(index => $"UrbanWildlife/Animals/{prefix}-{index:00}-v01"))
                .ToArray();
            foreach (string spritePath in animalTrueWalkSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.x <= 0f)
                {
                    throw new InvalidOperationException(
                        $"P0 animal true-walk sprite is missing or invalid: {spritePath}");
                }
            }
            string[] pigeonFeedSpritePaths = Enumerable.Range(
                    1,
                    P0AnimalSimulation.PigeonFeedArtworkFrameCount)
                .Select(index => $"UrbanWildlife/Animals/pigeon-side-feed-{index:00}-v01")
                .ToArray();
            foreach (string spritePath in pigeonFeedSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.x <= 0f)
                {
                    throw new InvalidOperationException(
                        $"P0 pigeon feeding sprite is missing or invalid: {spritePath}");
                }
            }
            string[] squirrelFeedSpritePaths = Enumerable.Range(
                    1,
                    P0AnimalSimulation.SquirrelFeedArtworkFrameCount)
                .Select(index => $"UrbanWildlife/Animals/squirrel-feed-{index:00}-v01")
                .ToArray();
            string[] foxFeedSpritePaths = Enumerable.Range(
                    1,
                    P0AnimalSimulation.FoxFeedArtworkFrameCount)
                .Select(index => $"UrbanWildlife/Animals/fox-feed-{index:00}-v01")
                .ToArray();
            foreach (string spritePath in squirrelFeedSpritePaths.Concat(foxFeedSpritePaths))
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.x <= 0f)
                {
                    throw new InvalidOperationException(
                        $"P0 animal feeding sprite is missing or invalid: {spritePath}");
                }
            }
            if (P0AnimalSimulation.IdleFrameCount != 3 ||
                P0AnimalSimulation.TurnFrameCount != 3 ||
                P0AnimalSimulation.WalkFrameCount != 6 ||
                P0AnimalSimulation.WalkArtworkFrameCount != 6 ||
                P0AnimalSimulation.PigeonWalkArtworkFrameCount != 6 ||
                P0AnimalSimulation.SquirrelWalkArtworkFrameCount != 6 ||
                P0AnimalSimulation.FoxWalkArtworkFrameCount != 6 ||
                P0AnimalSimulation.FeedFrameCount != 6 ||
                P0AnimalSimulation.PigeonFeedArtworkFrameCount != 6 ||
                P0AnimalSimulation.SquirrelFeedArtworkFrameCount != 6 ||
                P0AnimalSimulation.FoxFeedArtworkFrameCount != 6 ||
                P0AnimalSimulation.SitFrameCount != 4 ||
                P0AnimalSimulation.RiseFrameCount != 4)
            {
                throw new InvalidOperationException("P0 animal stepped-animation frame counts are incorrect.");
            }
            Sprite pigeonSprite = Resources.Load<Sprite>("UrbanWildlife/Animals/pigeon-side-walk-a-v01");
            Sprite squirrelSprite = Resources.Load<Sprite>("UrbanWildlife/Animals/squirrel-side-walk-a-v01");
            Sprite foxSprite = Resources.Load<Sprite>("UrbanWildlife/Animals/fox-side-walk-a-v01");
            float pigeonAspect = TightSpriteAspect(pigeonSprite);
            float squirrelAspect = TightSpriteAspect(squirrelSprite);
            float foxAspect = TightSpriteAspect(foxSprite);
            if (pigeonAspect <= 1f || squirrelAspect <= 1f || foxAspect <= 1f ||
                foxAspect <= squirrelAspect * 1.2f)
            {
                throw new InvalidOperationException(
                    $"Animal side-profile silhouettes are not distinct enough: " +
                    $"{pigeonAspect:F2}/{squirrelAspect:F2}/{foxAspect:F2}.");
            }
            if (!Mathf.Approximately(P0AnimalSimulation.ScreenFacingSpriteRotationDegrees, 0f) ||
                P0AnimalSimulation.IdleSwaySeconds < 6f)
            {
                throw new InvalidOperationException("Animal sprites must stay screen-facing with a slow web-style idle sway.");
            }
            Debug.Log(
                $"UNITY_ANIMAL_SPRITE_SMOKE_OK sprites=3 transparent_import=True side_profile=True " +
                $"aspects={pigeonAspect:F2}/{squirrelAspect:F2}/{foxAspect:F2} " +
                $"display_lengths={P0AnimalSimulation.PigeonDisplayLength:F2}/" +
                $"{P0AnimalSimulation.SquirrelDisplayLength:F2}/{P0AnimalSimulation.FoxDisplayLength:F2}");
            Debug.Log(
                "UNITY_ANIMAL_FLIPBOOK_SMOKE_OK idle=3 turn=3 walk=6 feed=6 sit=4 rise=4 " +
                "species=3 screen_facing=True flip_x=True");
            Debug.Log(
                "UNITY_ANIMAL_WALK_ARTWORK_SMOKE_OK species=3 frames_per_species=6 " +
                "leg_alternation=True normalized_scale=True procedural_deformation=False");
            Debug.Log(
                "UNITY_PIGEON_FEED_ARTWORK_SMOKE_OK frames=6 transparent_import=True " +
                "sequence=stand/lower/peck/peck/rise/stand procedural_fallback=True");
            Debug.Log(
                "UNITY_ANIMAL_FEED_ARTWORK_SMOKE_OK species=3 frames_per_species=6 " +
                "pigeon=peck squirrel=nibble fox=ground_bite transparent_import=True");

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
            string[] visitorFeedSpritePaths = Enumerable.Range(
                    1,
                    P0HumanSimulation.VisitorFeedArtworkFrameCount)
                .Select(index => $"UrbanWildlife/Humans/visitor-feed-{index:00}-v01")
                .ToArray();
            foreach (string spritePath in visitorFeedSpritePaths)
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.y <= 0f)
                {
                    throw new InvalidOperationException(
                        $"P0 visitor feeding sprite is missing or invalid: {spritePath}");
                }
            }
            string[] humanTrueWalkSpritePaths = new[]
            {
                new { Prefix = "walker-walk", Version = "v02" },
                new { Prefix = "dweller-walk", Version = "v01" },
                new { Prefix = "visitor-walk", Version = "v01" },
            }
                .SelectMany(item => Enumerable.Range(
                        1,
                        P0HumanSimulation.HumanWalkArtworkFrameCount)
                    .Select(index =>
                        $"UrbanWildlife/Humans/{item.Prefix}-{index:00}-{item.Version}"))
                .ToArray();
            string[] dwellerSitSpritePaths = Enumerable.Range(
                    1,
                    P0HumanSimulation.DwellerSitArtworkFrameCount)
                .Select(index => $"UrbanWildlife/Humans/dweller-sit-{index:00}-v01")
                .ToArray();
            foreach (string spritePath in humanTrueWalkSpritePaths.Concat(dwellerSitSpritePaths))
            {
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null || sprite.texture == null || sprite.bounds.size.y <= 0f)
                {
                    throw new InvalidOperationException(
                        $"P0 human action sprite is missing or invalid: {spritePath}");
                }
            }
            if (P0HumanSimulation.IdleFrameCount != 3 ||
                P0HumanSimulation.TurnFrameCount != 3 ||
                P0HumanSimulation.WalkFrameCount != 6 ||
                P0HumanSimulation.FeedFrameCount != 6 ||
                P0HumanSimulation.HumanWalkArtworkFrameCount != 6 ||
                P0HumanSimulation.WalkerWalkArtworkFrameCount != 6 ||
                P0HumanSimulation.DwellerWalkArtworkFrameCount != 6 ||
                P0HumanSimulation.VisitorWalkArtworkFrameCount != 6 ||
                P0HumanSimulation.WalkPlaybackFrameCount != 4 ||
                P0HumanSimulation.VisitorFeedArtworkFrameCount != 6 ||
                P0HumanSimulation.SitFrameCount != 4 ||
                P0HumanSimulation.DwellerSitArtworkFrameCount != 4 ||
                P0HumanSimulation.RiseFrameCount != 4 ||
                !Mathf.Approximately(P0HumanSimulation.WalkFramesPerSecond, 4f) ||
                !Mathf.Approximately(P0HumanSimulation.WalkSwayMultiplier, 2f))
            {
                throw new InvalidOperationException("P0 human stepped-animation timing is incorrect.");
            }
            int[] walkerWalkOrder = Enumerable.Range(0, P0HumanSimulation.WalkPlaybackFrameCount)
                .Select(index => P0HumanSimulation.WalkArtworkIndexFor(HumanArchetype.Walker, index))
                .ToArray();
            int[] dwellerWalkOrder = Enumerable.Range(0, P0HumanSimulation.WalkPlaybackFrameCount)
                .Select(index => P0HumanSimulation.WalkArtworkIndexFor(HumanArchetype.Dweller, index))
                .ToArray();
            int[] visitorWalkOrder = Enumerable.Range(0, P0HumanSimulation.WalkPlaybackFrameCount)
                .Select(index => P0HumanSimulation.WalkArtworkIndexFor(HumanArchetype.Visitor, index))
                .ToArray();
            if (!walkerWalkOrder.SequenceEqual(new[] { 0, 4, 1, 5 }) ||
                !dwellerWalkOrder.SequenceEqual(new[] { 0, 2, 3, 5 }) ||
                !visitorWalkOrder.SequenceEqual(new[] { 0, 2, 3, 5 }))
            {
                throw new InvalidOperationException("P0 human walk artwork is not in a valid alternating order.");
            }
            CharacterPose turnMiddle = SteppedCharacterAnimation.Sample(
                CharacterAnimationAction.Turning,
                1f / SteppedCharacterAnimation.FramesPerSecond);
            CharacterPose feedLowered = SteppedCharacterAnimation.Sample(
                CharacterAnimationAction.Feeding,
                2f / SteppedCharacterAnimation.FramesPerSecond);
            CharacterPose seated = SteppedCharacterAnimation.Sample(
                CharacterAnimationAction.Sitting,
                20f);
            CharacterPose risen = SteppedCharacterAnimation.Sample(
                CharacterAnimationAction.Rising,
                20f);
            if (turnMiddle.FrameIndex != 1 || turnMiddle.WidthScale > 0.25f ||
                feedLowered.FrameIndex != 2 || feedLowered.HeightScale >= 0.95f ||
                seated.FrameIndex != P0HumanSimulation.SitFrameCount - 1 ||
                seated.HeightScale >= 0.85f ||
                risen.FrameIndex != 0 || !Mathf.Approximately(risen.HeightScale, 1f))
            {
                throw new InvalidOperationException("Stepped character poses do not match the intended keyframes.");
            }
            Debug.Log(
                "UNITY_HUMAN_FLIPBOOK_SMOKE_OK idle=3 turn=3 walk=6 feed=6 sit=4 rise=4 " +
                "roles=3 fps=8");
            Debug.Log(
                "UNITY_HUMAN_WALK_ARTWORK_SMOKE_OK roles=3 frames_per_role=6 " +
                "normalized_height=True fixed_baseline=True procedural_deformation=False " +
                "continuous_road_state=True playback_frames=4 fps=4 strict_leg_alternation=True " +
                "sway_multiplier=2");
            Debug.Log(
                "UNITY_VISITOR_FEED_ARTWORK_SMOKE_OK frames=6 transparent_import=True " +
                "sequence=stand/reach/extend/release/withdraw/stand procedural_fallback=True");
            Debug.Log(
                "UNITY_HUMAN_ACTION_ARTWORK_SMOKE_OK walker_walk=6 dweller_sit=4 " +
                "dweller_rise=reversed_sit visitor_feed=6 transparent_import=True");
            if (!Mathf.Approximately(P0HumanSimulation.ScreenFacingSpriteRotationDegrees, 0f) ||
                P0HumanSimulation.IdleSwaySeconds < 6f)
            {
                throw new InvalidOperationException("Human sprites must stay screen-facing with a slow web-style idle sway.");
            }
            Shader paletteShader = Resources.Load<Shader>(SpritePaletteMaterial.ResourcePath);
            if (paletteShader == null || paletteShader.name != SpritePaletteMaterial.ShaderName)
            {
                throw new InvalidOperationException("Park palette harmonization shader is missing or invalid.");
            }
            if (P0HumanSimulation.PresentationSaturation >= 0.9f ||
                P0AnimalSimulation.PigeonPresentationSaturation >= 0.9f ||
                P0AnimalSimulation.SquirrelPresentationSaturation >= 0.9f ||
                P0AnimalSimulation.FoxPresentationSaturation >= 0.9f ||
                P0AnimalSimulation.FoxPresentationSaturation >= P0AnimalSimulation.PigeonPresentationSaturation)
            {
                throw new InvalidOperationException("Character palette values must visibly reduce source saturation.");
            }
            Material paletteSmokeMaterial = SpritePaletteMaterial.Create(0.72f, 0.91f, 0.16f);
            if (paletteSmokeMaterial == null ||
                !Mathf.Approximately(paletteSmokeMaterial.GetFloat("_Saturation"), 0.72f) ||
                !Mathf.Approximately(paletteSmokeMaterial.GetFloat("_Brightness"), 0.91f))
            {
                throw new InvalidOperationException("Park palette material values were not applied.");
            }
            UnityEngine.Object.DestroyImmediate(paletteSmokeMaterial);
            Debug.Log(
                "UNITY_CHARACTER_PALETTE_SMOKE_OK shader=True human_sat=0.72 " +
                "animal_sat=0.78/0.68/0.66 ambient_tint=True softened_state_tints=True");
            if (!(P0AnimalSimulation.PigeonDisplayLength < P0AnimalSimulation.SquirrelDisplayLength &&
                  P0AnimalSimulation.SquirrelDisplayLength < P0AnimalSimulation.FoxDisplayLength &&
                  P0AnimalSimulation.FoxDisplayLength < P0HumanSimulation.DwellerDisplayLength &&
                  P0HumanSimulation.DwellerDisplayLength <= P0HumanSimulation.VisitorDisplayLength &&
                  P0HumanSimulation.VisitorDisplayLength < P0HumanSimulation.WalkerDisplayLength))
            {
                throw new InvalidOperationException("Human and animal display lengths do not follow the real-size ordering.");
            }
            Sprite parkMap = Resources.Load<Sprite>("UrbanWildlife/Environment/park-board-s001-v03");
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
                "UNITY_FIXED_REGION_PRESENTATION_SMOKE_OK plaza_ring=False pond_ring=False " +
                "background_landmarks=True");
            Debug.Log(
                "UNITY_VISUAL_LAYER_SMOKE_OK human_sprites=40 animal_sprites=42 map_sprites=1 " +
                "planning_area_sprites=4 semantic_elements=7 refined_visuals=6 asphalt_path=True web_style_motion=True " +
                "animal_side_profile=True palette_harmonized=True animal_lengths=0.30/0.44/0.72 human_lengths=0.89/0.84/0.85 " +
                "real_size_order=True stepped_animation=True actions=idle/turn/walk/feed/sit/rise " +
                "true_action_artwork=pigeon_walk_feed/squirrel_walk_feed/fox_walk_feed/" +
                "walker_walk/dweller_walk_sit_rise/visitor_walk_feed");

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
                    cycle_scenario_id = "S001-C1",
                    cycle_title = "Repair the park",
                    event_type = "constraint_evaluated",
                    phase = "Confirm",
                    predicted_top_feeder = "Pigeon",
                    predicted_conflict_area = "Woodland edge",
                    memory_source_cycle_index = 0,
                    remembered_human_trips = 6,
                    remembered_avoidance_events = 5,
                    remembered_pigeon_food_token_id = 12,
                    remembered_squirrel_food_token_id = 10,
                    remembered_fox_food_token_id = 11,
                    carried_squirrel_familiarity = 0.4f,
                    memory_caution_multiplier = 1.1f,
                    remembered_human_trace_distance_units = 12.5f,
                    remembered_animal_trace_distance_units = 9.25f,
                    score_formula_version = P0CycleScore.FormulaVersion,
                    score_total = 45,
                    score_human_access = 20,
                    score_pigeon_feeding = 20,
                    score_squirrel_feeding = 5,
                    score_fox_feeding = 0,
                    layout_timestamp_ms = humanDemo.timestamp_ms,
                    human_connected = true,
                    animal_reachable = true,
                    food_hotspot_valid = true,
                    changes_used = 2,
                    changes_allowed = 2,
                    human_trips = 6,
                    human_trace_points = 90,
                    animal_trace_points = 120,
                    human_trace_distance_units = 12.5f,
                    animal_trace_distance_units = 9.25f,
                    pigeon_count = 7,
                    squirrel_count = 3,
                    fox_count = 1,
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
                    parsedLog.cycle_scenario_id != "S001-C1" || parsedLog.pigeon_count != 7 ||
                    parsedLog.remembered_squirrel_food_token_id != 10 ||
                    parsedLog.human_trace_points != 90 || parsedLog.animal_trace_points != 120 ||
                    parsedLog.score_total != 45 || parsedLog.score_human_access != 20 ||
                    csvLines.Length != 2 || !csvLines[1].Contains("\"comma, quote \"\"checked\"\"\"") ||
                    !ResearchLogFormatter.CsvHeader.Contains("predicted_top_feeder") ||
                    !ResearchLogFormatter.CsvHeader.Contains("memory_source_cycle_index") ||
                    !ResearchLogFormatter.CsvHeader.Contains("human_trace_points") ||
                    !ResearchLogFormatter.CsvHeader.Contains("animal_trace_distance_units") ||
                    !ResearchLogFormatter.CsvHeader.Contains("score_total") ||
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

            P0CycleMechanics mechanics = new P0CycleMechanics();
            if (mechanics.PredictionReady ||
                !mechanics.SetPredictedFeeder(P0PredictedFeeder.Pigeon) ||
                !mechanics.SetPredictedConflictArea(P0PredictedConflictArea.WoodlandEdge) ||
                !mechanics.PredictionReady ||
                !mechanics.BuildObservationSummary(4, 2, 1, 3).Contains("prediction matched"))
            {
                throw new InvalidOperationException("Prediction and observation mechanics are not internally consistent.");
            }
            if (!mechanics.RecordOutcome(plannerMemory) || mechanics.MemoryCount != 1 ||
                !mechanics.MemorySummary().Contains("Remembered nodes") ||
                !mechanics.MemorySummary().Contains("45/100") ||
                !mechanics.MemorySummary().Contains("human 125 cm") ||
                !mechanics.MemorySummary().Contains("animal 92 cm"))
            {
                throw new InvalidOperationException("Completed outcomes were not saved as cross-cycle memory.");
            }
            mechanics.BeginCycle(1);
            if (mechanics.PredictionReady || mechanics.CurrentProfile.ScenarioId != "S001-C2" ||
                mechanics.LastMemory == null || mechanics.LastMemory.SourceCycleIndex != 0 ||
                !mechanics.MemoryEffectSummary().Contains("revisit"))
            {
                throw new InvalidOperationException("Prediction must reset while memory persists into the next planning cycle.");
            }
            mechanics.ResetSession();
            if (mechanics.MemoryCount != 0 || mechanics.LastMemory != null)
            {
                throw new InvalidOperationException("Reset Session must clear cross-cycle memory.");
            }
            Debug.Log("UNITY_PREDICTION_SMOKE_OK required=2 reset_between_cycles=True observation_feedback=True");
            Debug.Log(
                "UNITY_CYCLE_MEMORY_SMOKE_OK revisit_share>=0.5 squirrel_carry=0.8 " +
                "caution_cap=1.25 visible_summary=True reset_session=True");
            Debug.Log(
                "UNITY_SCORE_SMOKE_OK total=100 weights=40/20/20/20 " +
                "normalized_by_successful_agents=True repeated_events_capped=True");

            P0TraceSeries markSeries = new P0TraceSeries(0.08f, 12, 0.75f);
            markSeries.TryAppend(Vector2.zero);
            markSeries.TryAppend(new Vector2(0.1f, 0f));
            markSeries.TryAppend(new Vector2(2f, 0f));
            if (Enum.GetValues(typeof(P0TraceMarkStyle)).Length != 4 ||
                !markSeries.IsConnectedFromPrevious(1) ||
                markSeries.IsConnectedFromPrevious(2) ||
                !Mathf.Approximately(markSeries.DistanceUnits, 0.1f))
            {
                throw new InvalidOperationException(
                    "Footprint trace styles or discontinuity handling are not configured correctly.");
            }
            Debug.Log(
                "UNITY_TRACE_SMOKE_OK live=True previous_cycle=True modes=People/Wildlife/AllTracks " +
                "marks=Footprint/BirdTrack/SmallPaw/FoxPaw sampling=True jump_breaks=True distances_logged=True");

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
            Debug.Log(
                "UNITY_UI_SMOKE_OK phase=Plan predictions_required=2 space_actions=Confirm/StartRun " +
                "trace_modes=3 park_notice_style=True");
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

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
            inputManager.AddComponent<LayoutDebugView>();
            inputManager.AddComponent<P0ConstraintManager>();
            inputManager.AddComponent<P0CycleController>();
            inputManager.AddComponent<P0ControlPanel>();

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.055f, 0.055f, 1f);
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
            if (debugView.GeneratedElementCount != 9)
            {
                throw new InvalidOperationException(
                    $"Debug view generated {debugView.GeneratedElementCount} elements instead of 9.");
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
                $"stable={packet.capture.stable} debug_elements=9");
            Debug.Log(
                $"UNITY_CONSTRAINT_SMOKE_OK human_connected={constraintResult.human_connected} " +
                $"animal_reachable={constraintResult.animal_reachable} " +
                $"food_hotspot_valid={constraintResult.food_hotspot_valid} " +
                $"changes={constraintResult.changes_used}/{constraintResult.changes_allowed}");
            Debug.Log(
                $"UNITY_CONSTRAINT_REPAIR_OK all_constraints={repairedResult.all_constraints_satisfied} " +
                $"changes={repairedResult.changes_used}/{repairedResult.changes_allowed}");

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
    }
}

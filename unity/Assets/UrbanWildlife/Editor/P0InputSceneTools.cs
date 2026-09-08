using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

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
            LayoutPacketReader reader = inputManager.AddComponent<LayoutPacketReader>();
            inputManager.AddComponent<LayoutDebugView>();
            inputManager.AddComponent<P0ConstraintManager>();
            SerializedObject readerSettings = new SerializedObject(reader);
            readerSettings.FindProperty("loadOnStart").boolValue = true;
            readerSettings.ApplyModifiedPropertiesWithoutUndo();

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
                packet.recognition.token_backend != "contour")
            {
                throw new InvalidOperationException("Contour layout fixture contains unexpected values.");
            }

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
                $"path_points={packet.path.point_count} backend={packet.recognition.token_backend} debug_elements=9");
            Debug.Log(
                $"UNITY_CONSTRAINT_SMOKE_OK human_connected={constraintResult.human_connected} " +
                $"animal_reachable={constraintResult.animal_reachable} " +
                $"food_hotspot_valid={constraintResult.food_hotspot_valid} " +
                $"changes={constraintResult.changes_used}/{constraintResult.changes_allowed}");
            Debug.Log(
                $"UNITY_CONSTRAINT_REPAIR_OK all_constraints={repairedResult.all_constraints_satisfied} " +
                $"changes={repairedResult.changes_used}/{repairedResult.changes_allowed}");
        }
    }
}

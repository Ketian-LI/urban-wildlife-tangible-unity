using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlife.Input;

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

            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.055f, 0.055f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 3.5f;
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

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

            Debug.Log(
                $"UNITY_INPUT_SMOKE_OK tokens={packet.tokens.Length} " +
                $"path_points={packet.path.point_count} backend={packet.recognition.token_backend}");
        }
    }
}

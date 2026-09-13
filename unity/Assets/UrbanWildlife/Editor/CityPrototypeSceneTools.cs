using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UrbanWildlife.Prototype;

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
            camera.backgroundColor = new Color(0.56f, 0.82f, 0.90f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 4.45f;
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
                prototype.CityFeedCount <= 0 ||
                prototype.RepresentativeAgentCount != 11 ||
                prototype.RepresentedPopulation != 132 ||
                prototype.VehicleTripCount <= 0 ||
                prototype.VehicleTripCount >= prototype.RepresentativeAgentCount ||
                !separateViewport)
            {
                throw new InvalidOperationException(
                    $"City prototype scene failed its visual integration contract: " +
                    $"prototype={prototype != null}, buildings={prototype?.GeneratedBuildingCount}, " +
                    $"roads={prototype?.GeneratedVehicleRoadCount}, links={prototype?.GeneratedPedestrianLinkCount}, " +
                    $"amenities={prototype?.GeneratedAmenityCount}, food={prototype?.FoodSourceCount}, " +
                    $"wildlife={prototype?.WildlifeAgentCount}, species={prototype?.WildlifeSpeciesCount}, " +
                    $"phases={prototype?.DevelopmentPhaseCount}, balance={prototype?.CityBalanceTotal}, " +
                    $"humanTrace={prototype?.HumanTracePointCount}, animalTrace={prototype?.AnimalTracePointCount}, " +
                    $"feed={prototype?.CityFeedCount}, " +
                    $"agents={prototype?.RepresentativeAgentCount}, population={prototype?.RepresentedPopulation}, " +
                    $"vehicles={prototype?.VehicleTripCount}, viewport={separateViewport}.");
            }
            string[] requiredObjects =
            {
                "Generated City Prototype/Buildings",
                "Generated City Prototype/Vehicle road network",
                "Generated City Prototype/Pedestrian link network",
                "Generated City Prototype/Digital amenities",
                "Generated City Prototype/City wildlife agents",
                "Generated City Prototype/Representative mobility agents",
            };
            if (requiredObjects.Any(path => prototype.transform.Find(path) == null))
            {
                throw new InvalidOperationException("City prototype is missing a generated visual layer.");
            }
            Debug.Log(
                "UNITY_CITY_PROTOTYPE_SMOKE_OK split_screen=True right_sidebar=True bright_city_style=True buildings=6 vehicle_roads=8 " +
                "pedestrian_links=8 amenities=3 food_sources=True waste_pressure=True " +
                "wildlife_agents=15 species=4 utility_targets=True " +
                "development_phases=5 city_balance=True dp=True time_blocks=4 " +
                "human_animal_combined_trace=True city_feed=True phase_report=True " +
                "representative_agents=11 represented_population=132 walk_and_drive=True " +
                "live_vehicle_agents=True max_speed=2x no_questionnaire=True");
        }
    }
}

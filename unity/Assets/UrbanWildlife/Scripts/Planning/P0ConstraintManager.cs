using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UrbanWildlife.Input;

namespace UrbanWildlife.Planning
{
    [RequireComponent(typeof(LayoutPacketReader))]
    public sealed class P0ConstraintManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Path relative to the Unity Assets folder, or an absolute path.")]
        private string scenarioPath = "../../data/scenarios/s001_weekend_park_baseline.json";

        private LayoutPacketReader reader;
        private P0Scenario scenario;

        public event Action<P0ConstraintResult> ConstraintsEvaluated;
        public P0ConstraintResult LatestResult { get; private set; }

        private void Awake()
        {
            reader = GetComponent<LayoutPacketReader>();
            scenario = LoadScenario(ResolvePath(scenarioPath));
        }

        private void OnEnable()
        {
            if (reader == null)
            {
                reader = GetComponent<LayoutPacketReader>();
            }

            reader.LayoutAccepted += Evaluate;
        }

        private void OnDisable()
        {
            if (reader != null)
            {
                reader.LayoutAccepted -= Evaluate;
            }
        }

        public void Evaluate(LayoutPacket packet)
        {
            if (scenario == null)
            {
                scenario = LoadScenario(ResolvePath(scenarioPath));
            }

            LatestResult = P0ConstraintEvaluator.Evaluate(packet, scenario);
            ConstraintsEvaluated?.Invoke(LatestResult);
            Debug.Log(
                $"Constraint Check: human_connected={LatestResult.human_connected}, " +
                $"animal_reachable={LatestResult.animal_reachable}, " +
                $"food_hotspot_valid={LatestResult.food_hotspot_valid}, " +
                $"changes={LatestResult.changes_used}/{LatestResult.changes_allowed}",
                this);
        }

        public static P0Scenario LoadScenario(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("P0 scenario file is missing.", path);
            }

            P0Scenario loaded = JsonConvert.DeserializeObject<P0Scenario>(File.ReadAllText(path));
            if (loaded == null || loaded.schema_version != "0.1" || loaded.scenario_id != "S001")
            {
                throw new InvalidOperationException("P0 scenario is empty or unsupported.");
            }

            return loaded;
        }

        private static string ResolvePath(string configuredPath)
        {
            return Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(Application.dataPath, configuredPath));
        }
    }
}

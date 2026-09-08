using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Cycle;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Humans
{
    [RequireComponent(typeof(LayoutPacketReader), typeof(P0CycleController), typeof(LayoutDebugView))]
    public sealed class P0HumanSimulation : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Path relative to the Unity Assets folder, or an absolute path.")]
        private string scenarioPath = "../../data/scenarios/s001_weekend_park_baseline.json";

        private sealed class RuntimeHuman
        {
            public HumanRoutePlan plan;
            public HumanAgentStateMachine model;
            public Transform visual;
            public Renderer renderer;
            public HumanActivityState lastState;
            public int completedTrips;
        }

        private readonly List<RuntimeHuman> humans = new List<RuntimeHuman>();
        private LayoutPacketReader reader;
        private P0CycleController cycle;
        private P0Scenario scenario;
        private LayoutPacket confirmedPacket;
        private Transform generatedRoot;

        public int AgentCount => humans.Count;
        public int WalkerCount => humans.Count(item => item.model.Archetype == HumanArchetype.Walker);
        public int DwellerCount => humans.Count(item => item.model.Archetype == HumanArchetype.Dweller);
        public int VisitorCount => humans.Count(item => item.model.Archetype == HumanArchetype.Visitor);
        public int CompletedTrips => humans.Sum(item => item.completedTrips);

        public bool TryGetNearestActiveHuman(Vector2 localPosition, out float distance)
        {
            distance = float.PositiveInfinity;
            bool found = false;
            foreach (RuntimeHuman human in humans)
            {
                if (human.model.State == HumanActivityState.WaitingToEnter ||
                    human.model.State == HumanActivityState.Finished)
                {
                    continue;
                }

                float candidate = Vector2.Distance(localPosition, human.model.Position);
                if (candidate < distance)
                {
                    distance = candidate;
                    found = true;
                }
            }
            return found;
        }

        private void Awake()
        {
            reader = GetComponent<LayoutPacketReader>();
            cycle = GetComponent<P0CycleController>();
            scenario = P0ConstraintManager.LoadScenario(ResolvePath(scenarioPath));
        }

        private void OnEnable()
        {
            if (reader == null)
            {
                reader = GetComponent<LayoutPacketReader>();
            }
            if (cycle == null)
            {
                cycle = GetComponent<P0CycleController>();
            }

            reader.LayoutAccepted += OnLayoutAccepted;
            cycle.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (reader != null)
            {
                reader.LayoutAccepted -= OnLayoutAccepted;
            }
            if (cycle != null)
            {
                cycle.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void Update()
        {
            if (cycle.Phase != P0Phase.Run)
            {
                return;
            }

            foreach (RuntimeHuman human in humans)
            {
                Vector2 previous = human.model.Position;
                human.model.Tick(Time.deltaTime);
                Vector2 current = human.model.Position;
                bool repeated = false;
                if (human.model.IsFinished)
                {
                    human.completedTrips += 1;
                    human.model = new HumanAgentStateMachine(human.plan);
                    current = human.model.Position;
                    repeated = true;
                }
                human.visual.localPosition = new Vector3(current.x, 0.3f, current.y);
                Vector2 movement = current - previous;
                if (!repeated && movement.sqrMagnitude > 0.000001f)
                {
                    human.visual.localRotation = Quaternion.LookRotation(new Vector3(movement.x, 0f, movement.y));
                }
                if (human.lastState != human.model.State)
                {
                    human.lastState = human.model.State;
                    human.renderer.sharedMaterial.color = ColourFor(human.model.Archetype, human.model.State);
                }
            }
        }

        private void OnLayoutAccepted(LayoutPacket packet)
        {
            confirmedPacket = packet;
        }

        private void OnPhaseChanged(P0Phase phase)
        {
            if (phase == P0Phase.Run)
            {
                BeginRun();
            }
            else if (phase == P0Phase.Plan)
            {
                ClearHumans();
            }
        }

        private void BeginRun()
        {
            ClearHumans();
            if (confirmedPacket == null)
            {
                Debug.LogWarning("Human simulation cannot start without a confirmed layout.", this);
                return;
            }

            HumanRoutePlan[] plans = HumanRoutePlanner.CreatePlans(confirmedPacket, scenario);
            GameObject root = new GameObject("Runtime Humans");
            root.transform.SetParent(transform, false);
            generatedRoot = root.transform;
            for (int index = 0; index < plans.Length; index += 1)
            {
                HumanAgentStateMachine model = new HumanAgentStateMachine(plans[index]);
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                visual.name = $"{model.Archetype} {index + 1}";
                visual.transform.SetParent(generatedRoot, false);
                visual.transform.localScale = new Vector3(0.18f, 0.24f, 0.18f);
                visual.transform.localPosition = new Vector3(model.Position.x, 0.3f, model.Position.y);
                Collider collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = visual.GetComponent<Renderer>();
                renderer.sharedMaterial = CreateMaterial(ColourFor(model.Archetype, model.State));
                humans.Add(new RuntimeHuman
                {
                    plan = plans[index],
                    model = model,
                    visual = visual.transform,
                    renderer = renderer,
                    lastState = model.State,
                });
            }

            Debug.Log(
                $"Human run started: walkers={WalkerCount}, dwellers={DwellerCount}, visitors={VisitorCount}.",
                this);
        }

        private void ClearHumans()
        {
            foreach (RuntimeHuman human in humans)
            {
                if (human.renderer == null || human.renderer.sharedMaterial == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(human.renderer.sharedMaterial);
                }
                else
                {
                    DestroyImmediate(human.renderer.sharedMaterial);
                }
            }
            humans.Clear();
            if (generatedRoot == null)
            {
                Transform existing = transform.Find("Runtime Humans");
                if (existing != null)
                {
                    generatedRoot = existing;
                }
            }
            if (generatedRoot == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(generatedRoot.gameObject);
            }
            else
            {
                DestroyImmediate(generatedRoot.gameObject);
            }
            generatedRoot = null;
        }

        private static Color ColourFor(HumanArchetype archetype, HumanActivityState state)
        {
            if (state == HumanActivityState.WaitingToEnter)
            {
                return new Color(0.45f, 0.48f, 0.52f, 1f);
            }
            if (state == HumanActivityState.Dwelling)
            {
                return new Color(1f, 0.72f, 0.1f, 1f);
            }
            if (state == HumanActivityState.Visiting)
            {
                return new Color(0.95f, 0.15f, 0.65f, 1f);
            }
            if (state == HumanActivityState.Finished)
            {
                return new Color(0.35f, 0.38f, 0.4f, 1f);
            }

            switch (archetype)
            {
                case HumanArchetype.Walker:
                    return new Color(0.15f, 0.62f, 1f, 1f);
                case HumanArchetype.Dweller:
                    return new Color(1f, 0.48f, 0.12f, 1f);
                default:
                    return new Color(0.1f, 0.82f, 0.62f, 1f);
            }
        }

        private static Material CreateMaterial(Color colour)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = colour;
            return material;
        }

        private static string ResolvePath(string configuredPath)
        {
            return Path.IsPathRooted(configuredPath)
                ? Path.GetFullPath(configuredPath)
                : Path.GetFullPath(Path.Combine(Application.dataPath, configuredPath));
        }
    }
}

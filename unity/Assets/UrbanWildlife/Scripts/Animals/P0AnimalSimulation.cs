using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Cycle;
using UrbanWildlife.Humans;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;

namespace UrbanWildlife.Animals
{
    [RequireComponent(typeof(LayoutPacketReader), typeof(P0CycleController), typeof(P0HumanSimulation))]
    public sealed class P0AnimalSimulation : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Path relative to the Unity Assets folder, or an absolute path.")]
        private string scenarioPath = "../../data/scenarios/s001_weekend_park_baseline.json";

        private sealed class RuntimeAnimal
        {
            public AnimalSpawnPlan plan;
            public AnimalAgentStateMachine model;
            public Transform visual;
            public Renderer renderer;
            public AnimalActivityState lastState;
        }

        private readonly List<RuntimeAnimal> animals = new List<RuntimeAnimal>();
        private LayoutPacketReader reader;
        private P0CycleController cycle;
        private P0HumanSimulation humans;
        private P0Scenario scenario;
        private LayoutPacket confirmedPacket;
        private Transform generatedRoot;

        public int AgentCount => animals.Count;
        public int PigeonCount => animals.Count(item => item.model.Species == AnimalSpecies.Pigeon);
        public int SquirrelCount => animals.Count(item => item.model.Species == AnimalSpecies.Squirrel);
        public int FoxCount => animals.Count(item => item.model.Species == AnimalSpecies.Fox);
        public int FeedEvents => animals.Sum(item => item.model.FeedEvents);
        public int PigeonFeedEvents => animals
            .Where(item => item.model.Species == AnimalSpecies.Pigeon)
            .Sum(item => item.model.FeedEvents);
        public int SquirrelFeedEvents => animals
            .Where(item => item.model.Species == AnimalSpecies.Squirrel)
            .Sum(item => item.model.FeedEvents);
        public int FoxFeedEvents => animals
            .Where(item => item.model.Species == AnimalSpecies.Fox)
            .Sum(item => item.model.FeedEvents);
        public int AvoidanceEvents => animals.Sum(item => item.model.AvoidanceEvents);

        private void Awake()
        {
            reader = GetComponent<LayoutPacketReader>();
            cycle = GetComponent<P0CycleController>();
            humans = GetComponent<P0HumanSimulation>();
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
            if (humans == null)
            {
                humans = GetComponent<P0HumanSimulation>();
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

            foreach (RuntimeAnimal animal in animals)
            {
                float nearestHumanDistance = float.PositiveInfinity;
                humans.TryGetNearestActiveHuman(animal.model.Position, out nearestHumanDistance);
                AnimalPerception perception = new AnimalPerception(
                    true,
                    animal.plan.FoodPosition,
                    true,
                    animal.plan.ShelterPosition,
                    nearestHumanDistance);
                Vector2 previous = animal.model.Position;
                animal.model.Tick(Time.deltaTime, perception);
                Vector2 current = animal.model.Position;
                animal.visual.localPosition = new Vector3(current.x, 0.2f, current.y);
                Vector2 movement = current - previous;
                if (movement.sqrMagnitude > 0.000001f)
                {
                    animal.visual.localRotation = Quaternion.LookRotation(new Vector3(movement.x, 0f, movement.y));
                }
                if (animal.lastState != animal.model.State)
                {
                    animal.lastState = animal.model.State;
                    animal.renderer.sharedMaterial.color = ColourFor(animal.model.Species, animal.model.State);
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
                ClearAnimals();
            }
        }

        private void BeginRun()
        {
            ClearAnimals();
            if (confirmedPacket == null)
            {
                Debug.LogWarning("Animal simulation cannot start without a confirmed layout.", this);
                return;
            }

            AnimalSpawnPlan[] plans = AnimalEnvironmentPlanner.CreatePlans(confirmedPacket, scenario);
            GameObject root = new GameObject("Runtime Animals");
            root.transform.SetParent(transform, false);
            generatedRoot = root.transform;
            for (int index = 0; index < plans.Length; index += 1)
            {
                AnimalAgentStateMachine model = new AnimalAgentStateMachine(plans[index]);
                GameObject visual = GameObject.CreatePrimitive(
                    model.Species == AnimalSpecies.Pigeon ? PrimitiveType.Sphere : PrimitiveType.Capsule);
                visual.name = model.Species.ToString();
                visual.transform.SetParent(generatedRoot, false);
                visual.transform.localScale = ScaleFor(model.Species);
                visual.transform.localPosition = new Vector3(model.Position.x, 0.2f, model.Position.y);
                Collider collider = visual.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer = visual.GetComponent<Renderer>();
                renderer.sharedMaterial = CreateMaterial(ColourFor(model.Species, model.State));
                animals.Add(new RuntimeAnimal
                {
                    plan = plans[index],
                    model = model,
                    visual = visual.transform,
                    renderer = renderer,
                    lastState = model.State,
                });
            }

            Debug.Log("Animal run started: pigeon=1, squirrel=1, fox=1.", this);
        }

        private void ClearAnimals()
        {
            foreach (RuntimeAnimal animal in animals)
            {
                if (animal.renderer == null || animal.renderer.sharedMaterial == null)
                {
                    continue;
                }
                if (Application.isPlaying)
                {
                    Destroy(animal.renderer.sharedMaterial);
                }
                else
                {
                    DestroyImmediate(animal.renderer.sharedMaterial);
                }
            }
            animals.Clear();
            if (generatedRoot == null)
            {
                Transform existing = transform.Find("Runtime Animals");
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

        private static Vector3 ScaleFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return new Vector3(0.22f, 0.14f, 0.28f);
                case AnimalSpecies.Squirrel:
                    return new Vector3(0.16f, 0.14f, 0.16f);
                default:
                    return new Vector3(0.26f, 0.13f, 0.18f);
            }
        }

        private static Color ColourFor(AnimalSpecies species, AnimalActivityState state)
        {
            if (state == AnimalActivityState.Feeding)
            {
                return new Color(1f, 0.85f, 0.12f, 1f);
            }
            if (state == AnimalActivityState.AvoidingHumans)
            {
                return new Color(1f, 0.2f, 0.58f, 1f);
            }
            if (state == AnimalActivityState.Retreating)
            {
                return new Color(0.38f, 0.28f, 0.42f, 1f);
            }

            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return new Color(0.62f, 0.72f, 0.82f, 1f);
                case AnimalSpecies.Squirrel:
                    return new Color(0.62f, 0.38f, 0.18f, 1f);
                default:
                    return new Color(0.9f, 0.3f, 0.08f, 1f);
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

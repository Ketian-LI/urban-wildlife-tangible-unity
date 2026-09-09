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
        public const float FrontFacingSpriteHeadingOffsetDegrees = 180f;

        [SerializeField]
        [Tooltip("Path relative to the Unity Assets folder, or an absolute path.")]
        private string scenarioPath = "../../data/scenarios/s001_weekend_park_baseline.json";

        private sealed class RuntimeHuman
        {
            public HumanRoutePlan plan;
            public HumanAgentStateMachine model;
            public Transform visual;
            public Transform artwork;
            public Renderer renderer;
            public SpriteRenderer spriteRenderer;
            public Vector3 artworkBaseScale;
            public float headingDegrees;
            public float animationOffset;
            public bool ownsMaterial;
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

                human.visual.localPosition = new Vector3(current.x, 0.32f, current.y);
                Vector2 movement = current - previous;
                bool moving = !repeated && movement.sqrMagnitude > 0.000001f;
                if (moving)
                {
                    float targetHeading = Mathf.Atan2(movement.x, movement.y) * Mathf.Rad2Deg;
                    float turnBlend = 1f - Mathf.Exp(-9f * Time.deltaTime);
                    human.headingDegrees = Mathf.LerpAngle(human.headingDegrees, targetHeading, turnBlend);
                }
                ApplyMotion(human, moving);

                if (human.lastState != human.model.State)
                {
                    human.lastState = human.model.State;
                    ApplyColour(human);
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
                GameObject visual = new GameObject($"{model.Archetype} {index + 1}");
                visual.transform.SetParent(generatedRoot, false);
                visual.transform.localPosition = new Vector3(model.Position.x, 0.32f, model.Position.y);
                RuntimeHuman runtime = new RuntimeHuman
                {
                    plan = plans[index],
                    model = model,
                    visual = visual.transform,
                    headingDegrees = 0f,
                    animationOffset = index * 0.83f,
                    lastState = model.State,
                };
                CreateArtwork(runtime);
                ApplyColour(runtime);
                ApplyMotion(runtime, false);
                humans.Add(runtime);
            }

            Debug.Log(
                $"Human run started: walkers={WalkerCount}, dwellers={DwellerCount}, visitors={VisitorCount}.",
                this);
        }

        private void ClearHumans()
        {
            foreach (RuntimeHuman human in humans)
            {
                if (human.ownsMaterial && human.renderer != null && human.renderer.sharedMaterial != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(human.renderer.sharedMaterial);
                    }
                    else
                    {
                        DestroyImmediate(human.renderer.sharedMaterial);
                    }
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

        private static void CreateArtwork(RuntimeHuman human)
        {
            Sprite sourceSprite = Resources.Load<Sprite>(ResourcePathFor(human.model.Archetype));
            if (sourceSprite != null)
            {
                GameObject artwork = new GameObject("Artwork");
                artwork.transform.SetParent(human.visual, false);
                artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                SpriteRenderer spriteRenderer = artwork.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sourceSprite;
                spriteRenderer.sortingOrder = 40;
                float targetLength = LengthFor(human.model.Archetype);
                float uniformScale = targetLength / Mathf.Max(0.001f, sourceSprite.bounds.size.y);
                artwork.transform.localScale = Vector3.one * uniformScale;
                human.artwork = artwork.transform;
                human.artworkBaseScale = artwork.transform.localScale;
                human.renderer = spriteRenderer;
                human.spriteRenderer = spriteRenderer;
                human.ownsMaterial = false;
                return;
            }

            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            fallback.name = "Fallback Geometry";
            fallback.transform.SetParent(human.visual, false);
            fallback.transform.localScale = new Vector3(0.18f, 0.24f, 0.18f);
            Collider collider = fallback.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            human.artwork = fallback.transform;
            human.artworkBaseScale = fallback.transform.localScale;
            human.renderer = fallback.GetComponent<Renderer>();
            human.renderer.sharedMaterial = CreateMaterial(FallbackColourFor(human.model.Archetype));
            human.ownsMaterial = true;
            Debug.LogWarning($"Human sprite missing for {human.model.Archetype}; using fallback geometry.");
        }

        private static void ApplyMotion(RuntimeHuman human, bool moving)
        {
            float time = Time.time + human.animationOffset;
            float sway = moving
                ? Mathf.Sin(time * 7.5f) * 1.1f
                : Mathf.Sin(time * (2f * Mathf.PI / 5.8f)) * 3.2f;
            if (human.model.State == HumanActivityState.Visiting)
            {
                sway += Mathf.Sin(time * 2.2f) * 2.4f;
            }
            human.visual.localRotation = Quaternion.Euler(
                0f,
                human.headingDegrees + FrontFacingSpriteHeadingOffsetDegrees + sway,
                0f);

            float pulse = 1f;
            if (moving)
            {
                pulse += Mathf.Sin(time * 9f) * 0.012f;
            }
            else if (human.model.State == HumanActivityState.Dwelling ||
                     human.model.State == HumanActivityState.Visiting)
            {
                pulse += Mathf.Sin(time * 2f) * 0.022f;
            }
            else
            {
                pulse += Mathf.Sin(time * 1.5f) * 0.01f;
            }
            human.artwork.localScale = human.artworkBaseScale * pulse;
        }

        private static void ApplyColour(RuntimeHuman human)
        {
            if (human.spriteRenderer != null)
            {
                human.spriteRenderer.color = SpriteTintFor(human.model.State);
            }
            else if (human.renderer != null && human.renderer.sharedMaterial != null)
            {
                human.renderer.sharedMaterial.color = FallbackColourFor(human.model.Archetype);
            }
        }

        private static string ResourcePathFor(HumanArchetype archetype)
        {
            switch (archetype)
            {
                case HumanArchetype.Walker:
                    return "UrbanWildlife/Humans/walker-topdown-v04";
                case HumanArchetype.Dweller:
                    return "UrbanWildlife/Humans/dweller-topdown-v02";
                default:
                    return "UrbanWildlife/Humans/visitor-topdown-v04";
            }
        }

        private static float LengthFor(HumanArchetype archetype)
        {
            return archetype == HumanArchetype.Dweller ? 0.82f : 0.9f;
        }

        private static Color SpriteTintFor(HumanActivityState state)
        {
            switch (state)
            {
                case HumanActivityState.WaitingToEnter:
                    return new Color(0.68f, 0.72f, 0.7f, 0.88f);
                case HumanActivityState.Dwelling:
                    return new Color(1f, 0.92f, 0.7f, 1f);
                case HumanActivityState.Visiting:
                    return new Color(0.88f, 1f, 0.9f, 1f);
                case HumanActivityState.Finished:
                    return new Color(0.62f, 0.65f, 0.64f, 0.82f);
                default:
                    return Color.white;
            }
        }

        private static Color FallbackColourFor(HumanArchetype archetype)
        {
            switch (archetype)
            {
                case HumanArchetype.Walker:
                    return new Color(0.32f, 0.54f, 0.7f, 1f);
                case HumanArchetype.Dweller:
                    return new Color(0.94f, 0.62f, 0.18f, 1f);
                default:
                    return new Color(0.28f, 0.62f, 0.54f, 1f);
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

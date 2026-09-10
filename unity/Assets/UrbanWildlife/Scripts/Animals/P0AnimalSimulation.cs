using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Cycle;
using UrbanWildlife.Humans;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;
using UrbanWildlife.Presentation;

namespace UrbanWildlife.Animals
{
    [RequireComponent(typeof(LayoutPacketReader), typeof(P0CycleController), typeof(P0HumanSimulation))]
    public sealed class P0AnimalSimulation : MonoBehaviour
    {
        public const float PigeonDisplayLength = 0.30f;
        public const float SquirrelDisplayLength = 0.44f;
        public const float FoxDisplayLength = 0.72f;
        public const int WalkFrameCount = 2;
        public const float ScreenFacingSpriteRotationDegrees = 0f;
        public const float IdleSwaySeconds = 7.2f;
        public const float PigeonPresentationSaturation = 0.78f;
        public const float SquirrelPresentationSaturation = 0.68f;
        public const float FoxPresentationSaturation = 0.66f;
        public const float PresentationBrightness = 0.92f;
        public const float PresentationAmbientBlend = 0.18f;

        [SerializeField]
        [Tooltip("Path relative to the Unity Assets folder, or an absolute path.")]
        private string scenarioPath = "../../data/scenarios/s001_weekend_park_baseline.json";

        private sealed class RuntimeAnimal
        {
            public AnimalSpawnPlan plan;
            public AnimalAgentStateMachine model;
            public Transform visual;
            public Transform artwork;
            public Renderer renderer;
            public SpriteRenderer spriteRenderer;
            public Sprite standingSprite;
            public Sprite alternateWalkSprite;
            public Vector3 artworkBaseScale;
            public Vector3 artworkBasePosition;
            public float animationOffset;
            public bool facingRight;
            public bool ownsMaterial;
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
                animal.visual.localPosition = new Vector3(current.x, 0.28f, current.y);
                Vector2 movement = current - previous;
                if (Mathf.Abs(movement.x) > 0.00001f)
                {
                    animal.facingRight = movement.x >= 0f;
                }
                ApplyMotion(animal, movement.sqrMagnitude > 0.000001f);
                if (animal.lastState != animal.model.State)
                {
                    animal.lastState = animal.model.State;
                    ApplyColour(animal);
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
            else
            {
                foreach (RuntimeAnimal animal in animals)
                {
                    ApplyMotion(animal, false);
                }
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
                GameObject visual = new GameObject(model.Species.ToString());
                visual.name = model.Species.ToString();
                visual.transform.SetParent(generatedRoot, false);
                visual.transform.localPosition = new Vector3(model.Position.x, 0.28f, model.Position.y);
                RuntimeAnimal runtime = new RuntimeAnimal
                {
                    plan = plans[index],
                    model = model,
                    visual = visual.transform,
                    animationOffset = index * 1.37f,
                    facingRight = (index & 1) == 0,
                    lastState = model.State,
                };
                CreateArtwork(runtime);
                ApplyColour(runtime);
                animals.Add(runtime);
            }

            Debug.Log("Animal run started: pigeon=1, squirrel=1, fox=1.", this);
        }

        private void ClearAnimals()
        {
            foreach (RuntimeAnimal animal in animals)
            {
                if (animal.ownsMaterial && animal.renderer != null && animal.renderer.sharedMaterial != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(animal.renderer.sharedMaterial);
                    }
                    else
                    {
                        DestroyImmediate(animal.renderer.sharedMaterial);
                    }
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

        private static void CreateArtwork(RuntimeAnimal animal)
        {
            Sprite sourceSprite = Resources.Load<Sprite>(ResourcePathFor(animal.model.Species));
            if (sourceSprite != null)
            {
                Sprite alternateWalkSprite = Resources.Load<Sprite>(WalkAlternateResourcePathFor(animal.model.Species));
                GameObject artwork = new GameObject("Artwork");
                artwork.transform.SetParent(animal.visual, false);
                artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                SpriteRenderer spriteRenderer = artwork.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = sourceSprite;
                spriteRenderer.sortingOrder = 30;
                float targetLength = LengthFor(animal.model.Species);
                float uniformScale = targetLength / Mathf.Max(0.001f, sourceSprite.bounds.size.x);
                artwork.transform.localScale = Vector3.one * uniformScale;
                animal.artwork = artwork.transform;
                animal.artworkBaseScale = artwork.transform.localScale;
                animal.artworkBasePosition = artwork.transform.localPosition;
                animal.renderer = spriteRenderer;
                animal.spriteRenderer = spriteRenderer;
                animal.standingSprite = sourceSprite;
                animal.alternateWalkSprite = alternateWalkSprite;
                Material paletteMaterial = SpritePaletteMaterial.Create(
                    PaletteSaturationFor(animal.model.Species),
                    PresentationBrightness,
                    PresentationAmbientBlend);
                if (paletteMaterial != null)
                {
                    spriteRenderer.sharedMaterial = paletteMaterial;
                    animal.ownsMaterial = true;
                }
                else
                {
                    animal.ownsMaterial = false;
                    Debug.LogWarning("Park palette shader is unavailable; using the default animal Sprite material.");
                }
                if (alternateWalkSprite == null)
                {
                    Debug.LogWarning($"Alternate walk sprite missing for {animal.model.Species}; using the standing frame.");
                }
                return;
            }

            GameObject fallback = GameObject.CreatePrimitive(
                animal.model.Species == AnimalSpecies.Pigeon ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            fallback.name = "Fallback Geometry";
            fallback.transform.SetParent(animal.visual, false);
            fallback.transform.localScale = FallbackScaleFor(animal.model.Species);
            Collider collider = fallback.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
            animal.artwork = fallback.transform;
            animal.artworkBaseScale = fallback.transform.localScale;
            animal.artworkBasePosition = fallback.transform.localPosition;
            animal.renderer = fallback.GetComponent<Renderer>();
            animal.renderer.sharedMaterial = CreateMaterial(
                FallbackColourFor(animal.model.Species, animal.model.State));
            animal.ownsMaterial = true;
            Debug.LogWarning($"Animal sprite missing for {animal.model.Species}; using fallback geometry.");
        }

        private static void ApplyMotion(RuntimeAnimal animal, bool moving)
        {
            float time = Time.time + animal.animationOffset;
            float walkFramesPerSecond = WalkFramesPerSecondFor(animal.model.Species);
            float stepPhase = time * walkFramesPerSecond;
            if (animal.spriteRenderer != null)
            {
                bool useAlternateFrame = moving &&
                    animal.alternateWalkSprite != null &&
                    (Mathf.FloorToInt(stepPhase) & 1) == 1;
                animal.spriteRenderer.sprite = useAlternateFrame
                    ? animal.alternateWalkSprite
                    : animal.standingSprite;
                animal.spriteRenderer.flipX = !animal.facingRight;
            }

            float sway = moving
                ? Mathf.Sin(stepPhase * Mathf.PI) * 0.45f
                : Mathf.Sin(time * (2f * Mathf.PI / IdleSwaySeconds)) * 1.15f;
            animal.visual.localRotation = Quaternion.Euler(
                0f,
                ScreenFacingSpriteRotationDegrees + sway,
                0f);

            float pulse = 1f;
            if (animal.model.State == AnimalActivityState.Feeding)
            {
                pulse += Mathf.Sin(time * 5f) * 0.018f;
            }
            else if (!moving)
            {
                pulse += Mathf.Sin(time * 1.2f) * 0.007f;
            }
            animal.artwork.localScale = animal.artworkBaseScale * pulse;
            float gaitLift = moving
                ? Mathf.Abs(Mathf.Sin(stepPhase * Mathf.PI)) * StepLiftFor(animal.model.Species)
                : 0f;
            animal.artwork.localPosition = animal.artworkBasePosition + Vector3.up * gaitLift;
        }

        private static void ApplyColour(RuntimeAnimal animal)
        {
            if (animal.spriteRenderer != null)
            {
                animal.spriteRenderer.color = SpriteTintFor(animal.model.State);
            }
            else if (animal.renderer != null && animal.renderer.sharedMaterial != null)
            {
                animal.renderer.sharedMaterial.color = FallbackColourFor(
                    animal.model.Species,
                    animal.model.State);
            }
        }

        private static string ResourcePathFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return "UrbanWildlife/Animals/pigeon-side-walk-a-v01";
                case AnimalSpecies.Squirrel:
                    return "UrbanWildlife/Animals/squirrel-side-walk-a-v01";
                default:
                    return "UrbanWildlife/Animals/fox-side-walk-a-v01";
            }
        }

        private static string WalkAlternateResourcePathFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return "UrbanWildlife/Animals/pigeon-side-walk-b-v01";
                case AnimalSpecies.Squirrel:
                    return "UrbanWildlife/Animals/squirrel-side-walk-b-v01";
                default:
                    return "UrbanWildlife/Animals/fox-side-walk-b-v01";
            }
        }

        private static float WalkFramesPerSecondFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return 3.2f;
                case AnimalSpecies.Squirrel:
                    return 3.8f;
                default:
                    return 3.1f;
            }
        }

        private static float StepLiftFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return 0.002f;
                case AnimalSpecies.Squirrel:
                    return 0.004f;
                default:
                    return 0.003f;
            }
        }

        private static float PaletteSaturationFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return PigeonPresentationSaturation;
                case AnimalSpecies.Squirrel:
                    return SquirrelPresentationSaturation;
                default:
                    return FoxPresentationSaturation;
            }
        }

        private static float LengthFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return PigeonDisplayLength;
                case AnimalSpecies.Squirrel:
                    return SquirrelDisplayLength;
                default:
                    return FoxDisplayLength;
            }
        }

        private static Vector3 FallbackScaleFor(AnimalSpecies species)
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

        private static Color SpriteTintFor(AnimalActivityState state)
        {
            if (state == AnimalActivityState.Feeding)
            {
                return new Color(1f, 0.96f, 0.82f, 1f);
            }
            if (state == AnimalActivityState.AvoidingHumans)
            {
                return new Color(1f, 0.82f, 0.87f, 1f);
            }
            if (state == AnimalActivityState.Retreating)
            {
                return new Color(0.82f, 0.78f, 0.86f, 1f);
            }
            return Color.white;
        }

        private static Color FallbackColourFor(AnimalSpecies species, AnimalActivityState state)
        {
            Color tint = SpriteTintFor(state);
            if (tint != Color.white)
            {
                return tint;
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

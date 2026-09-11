using System;
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
        public const int IdleFrameCount = SteppedCharacterAnimation.IdleFrameCount;
        public const int TurnFrameCount = SteppedCharacterAnimation.TurnFrameCount;
        public const int WalkFrameCount = SteppedCharacterAnimation.WalkFrameCount;
        public const int WalkArtworkFrameCount = 6;
        public const int PigeonWalkArtworkFrameCount = WalkArtworkFrameCount;
        public const int SquirrelWalkArtworkFrameCount = WalkArtworkFrameCount;
        public const int FoxWalkArtworkFrameCount = WalkArtworkFrameCount;
        public const int FeedFrameCount = SteppedCharacterAnimation.FeedFrameCount;
        public const int FeedArtworkFrameCount = 6;
        public const int PigeonFeedArtworkFrameCount = FeedArtworkFrameCount;
        public const int SquirrelFeedArtworkFrameCount = FeedArtworkFrameCount;
        public const int FoxFeedArtworkFrameCount = FeedArtworkFrameCount;
        public const int SitFrameCount = SteppedCharacterAnimation.SitFrameCount;
        public const int RiseFrameCount = SteppedCharacterAnimation.RiseFrameCount;
        public const float ScreenFacingSpriteRotationDegrees = 0f;
        public const float IdleSwaySeconds = 7.2f;
        public const float PigeonPresentationSaturation = 0.78f;
        public const float SquirrelPresentationSaturation = 0.68f;
        public const float FoxPresentationSaturation = 0.66f;
        public const float PresentationBrightness = 0.92f;
        public const float PresentationAmbientBlend = 0.18f;
        public const float TraceSampleDistance = 0.08f;
        public const float TraceWidth = 0.11f;

        private static readonly Color AnimalTraceColour = new Color(0.74f, 0.38f, 0.12f, 0.5f);
        private static readonly Color PreviousAnimalTraceColour = new Color(0.74f, 0.38f, 0.12f, 0.12f);

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
            public Sprite[] walkingSprites;
            public Sprite[] feedingSprites;
            public Vector3 artworkBaseScale;
            public Vector3 artworkBasePosition;
            public float animationOffset;
            public bool facingRight;
            public bool turnFromFacingRight;
            public bool ownsMaterial;
            public AnimalActivityState lastState;
            public CharacterAnimationAction lastAnimationAction;
            public Vector2 lastMovementDirection;
            public float actionStartedAt;
            public float turnStartedAt;
            public float turnEndsAt;
            public float riseEndsAt;
            public P0TraceLine trace;
        }

        private readonly List<RuntimeAnimal> animals = new List<RuntimeAnimal>();
        private LayoutPacketReader reader;
        private P0CycleController cycle;
        private P0HumanSimulation humans;
        private P0Scenario scenario;
        private LayoutPacket confirmedPacket;
        private Transform generatedRoot;
        private Transform previousTraceRoot;
        private Material animalTraceMaterial;
        private Material previousAnimalTraceMaterial;
        private AnimalMovementObstacle[] movementObstacles = Array.Empty<AnimalMovementObstacle>();
        private Vector2 animalBoardHalfExtents;
        private bool traceVisible = true;

        public event Action<P0CycleMemorySnapshot> CycleMemoryRecorded;
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
        public int FedPigeonCount => animals.Count(
            item => item.model.Species == AnimalSpecies.Pigeon && item.model.FeedEvents > 0);
        public int FedSquirrelCount => animals.Count(
            item => item.model.Species == AnimalSpecies.Squirrel && item.model.FeedEvents > 0);
        public int FedFoxCount => animals.Count(
            item => item.model.Species == AnimalSpecies.Fox && item.model.FeedEvents > 0);
        public int AvoidanceEvents => animals.Sum(item => item.model.AvoidanceEvents);
        public float AverageSquirrelFamiliarity => animals
            .Where(item => item.model.Species == AnimalSpecies.Squirrel)
            .Select(item => item.model.Familiarity)
            .DefaultIfEmpty(0f)
            .Average();
        public int TracePointCount => animals.Sum(item => item.trace?.Series.PointCount ?? 0);
        public float TraceDistanceUnits => animals.Sum(item => item.trace?.Series.DistanceUnits ?? 0f);
        public float TraceDistanceCm => TraceDistanceUnits * 10f;

        public void SetTraceVisible(bool visible)
        {
            traceVisible = visible;
            foreach (RuntimeAnimal animal in animals)
            {
                animal.trace?.SetVisible(visible);
            }
            if (previousTraceRoot != null)
            {
                previousTraceRoot.gameObject.SetActive(visible);
            }
        }

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
                    nearestHumanDistance,
                    movementObstacles,
                    animalBoardHalfExtents);
                Vector2 previous = animal.model.Position;
                animal.model.Tick(Time.deltaTime, perception);
                Vector2 current = animal.model.Position;
                animal.trace?.TryAppend(current);
                animal.visual.localPosition = new Vector3(current.x, 0.28f, current.y);
                Vector2 movement = current - previous;
                bool moving = movement.sqrMagnitude > 0.000001f;
                if (moving)
                {
                    UpdateHeading(animal, movement);
                }
                if (animal.lastState != animal.model.State)
                {
                    if ((animal.lastState == AnimalActivityState.Feeding ||
                         animal.lastState == AnimalActivityState.Resting) &&
                        animal.model.State != animal.lastState)
                    {
                        BeginRise(animal);
                    }
                    animal.lastState = animal.model.State;
                    ApplyColour(animal);
                }
                ApplyMotion(animal, moving);
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
            else if (phase == P0Phase.Observe)
            {
                RecordCycleMemory();
                StopAnimalMotion();
            }
            else if (phase == P0Phase.Plan)
            {
                if (cycle.CycleIndex > 0)
                {
                    ArchiveCurrentTrace();
                }
                else
                {
                    ClearPreviousTrace();
                }
                ClearAnimals();
            }
            else
            {
                StopAnimalMotion();
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

            P0CycleProfile profile = cycle.CurrentProfile;
            movementObstacles = AnimalObstacleAvoidance.BuildObstacles(confirmedPacket, scenario);
            float environmentScale = scenario.animal_simulation.unity_units_per_cm;
            animalBoardHalfExtents = new Vector2(
                scenario.board.width_cm * environmentScale * 0.5f - 0.28f,
                scenario.board.height_cm * environmentScale * 0.5f - 0.28f);
            AnimalSpawnPlan[] plans = AnimalEnvironmentPlanner.CreatePlans(
                confirmedPacket,
                scenario,
                profile.PigeonCount,
                profile.SquirrelCount,
                profile.FoxCount,
                cycle.Mechanics.LastMemory);
            GameObject root = new GameObject("Runtime Animals");
            root.transform.SetParent(transform, false);
            generatedRoot = root.transform;
            animalTraceMaterial = CreateTraceMaterial(AnimalTraceColour);
            for (int index = 0; index < plans.Length; index += 1)
            {
                AnimalAgentStateMachine model = new AnimalAgentStateMachine(plans[index]);
                GameObject visual = new GameObject(model.Species.ToString());
                visual.name = $"{model.Species} {index + 1:00}";
                visual.transform.SetParent(generatedRoot, false);
                visual.transform.localPosition = new Vector3(model.Position.x, 0.28f, model.Position.y);
                RuntimeAnimal runtime = new RuntimeAnimal
                {
                    plan = plans[index],
                    model = model,
                    visual = visual.transform,
                    animationOffset = index * 1.37f,
                    facingRight = (index & 1) == 0,
                    turnFromFacingRight = (index & 1) == 0,
                    lastState = model.State,
                    lastAnimationAction = CharacterAnimationAction.Idle,
                    actionStartedAt = Time.time,
                };
                CreateArtwork(runtime);
                runtime.trace = new P0TraceLine(
                    generatedRoot,
                    $"Animal Trace {index + 1:00}",
                    animalTraceMaterial,
                    TraceMarkSizeFor(model.Species),
                    0.12f,
                    TraceSampleDistance,
                    markStyle: TraceMarkStyleFor(model.Species));
                runtime.trace.TryAppend(model.Position);
                runtime.trace.SetVisible(traceVisible);
                ApplyColour(runtime);
                animals.Add(runtime);
            }

            Debug.Log(
                $"Animal run started: pigeon={PigeonCount}, squirrel={SquirrelCount}, fox={FoxCount}; " +
                $"scenario={profile.ScenarioId}.",
                this);
        }

        private void RecordCycleMemory()
        {
            P0CycleScore score = P0CycleScore.Calculate(
                humans?.SuccessfulAgentCount ?? 0,
                humans?.AgentCount ?? 0,
                FedPigeonCount,
                PigeonCount,
                FedSquirrelCount,
                SquirrelCount,
                FedFoxCount,
                FoxCount);
            P0CycleMemorySnapshot snapshot = new P0CycleMemorySnapshot(
                cycle.CycleIndex,
                humans?.CompletedTrips ?? 0,
                PigeonFeedEvents,
                SquirrelFeedEvents,
                FoxFeedEvents,
                AvoidanceEvents,
                PreferredFoodTokenId(AnimalSpecies.Pigeon),
                PreferredFoodTokenId(AnimalSpecies.Squirrel),
                PreferredFoodTokenId(AnimalSpecies.Fox),
                AverageSquirrelFamiliarity,
                score,
                humans?.TraceDistanceUnits ?? 0f,
                TraceDistanceUnits);
            if (!cycle.Mechanics.RecordOutcome(snapshot))
            {
                Debug.LogWarning("Could not record the current animal outcome as cross-cycle memory.", this);
                return;
            }
            CycleMemoryRecorded?.Invoke(snapshot);
        }

        private int PreferredFoodTokenId(AnimalSpecies species)
        {
            return animals
                .Where(item => item.model.Species == species && item.model.FeedEvents > 0)
                .GroupBy(item => item.plan.FoodTokenId)
                .Select(group => new
                {
                    TokenId = group.Key,
                    FeedEvents = group.Sum(item => item.model.FeedEvents),
                })
                .OrderByDescending(item => item.FeedEvents)
                .ThenBy(item => item.TokenId)
                .Select(item => item.TokenId)
                .DefaultIfEmpty(-1)
                .First();
        }

        private void StopAnimalMotion()
        {
            foreach (RuntimeAnimal animal in animals)
            {
                ApplyMotion(animal, false);
            }
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
            DestroyObject(animalTraceMaterial);
            animalTraceMaterial = null;
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

        private void ArchiveCurrentTrace()
        {
            ClearPreviousTrace();
            if (TracePointCount == 0)
            {
                return;
            }

            GameObject root = new GameObject("Previous Animal Trace");
            root.transform.SetParent(transform, false);
            previousTraceRoot = root.transform;
            previousAnimalTraceMaterial = CreateTraceMaterial(PreviousAnimalTraceColour);
            int index = 0;
            foreach (RuntimeAnimal animal in animals)
            {
                animal.trace?.CopyTo(
                    previousTraceRoot,
                    $"Previous Animal Trace {++index:00}",
                    previousAnimalTraceMaterial,
                    TraceMarkSizeFor(animal.model.Species) * 0.74f);
            }
            previousTraceRoot.gameObject.SetActive(traceVisible);
        }

        private void ClearPreviousTrace()
        {
            DestroyObject(previousTraceRoot == null ? null : previousTraceRoot.gameObject);
            previousTraceRoot = null;
            DestroyObject(previousAnimalTraceMaterial);
            previousAnimalTraceMaterial = null;
        }

        private static Material CreateTraceMaterial(Color colour)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = colour;
            return material;
        }

        private static float TraceMarkSizeFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return TraceWidth * 0.82f;
                case AnimalSpecies.Fox:
                    return TraceWidth * 1.4f;
                default:
                    return TraceWidth;
            }
        }

        private static P0TraceMarkStyle TraceMarkStyleFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return P0TraceMarkStyle.BirdTrack;
                case AnimalSpecies.Fox:
                    return P0TraceMarkStyle.FoxPaw;
                default:
                    return P0TraceMarkStyle.SmallPaw;
            }
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
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
                animal.walkingSprites = LoadActionSprites(
                    WalkingResourcePrefixFor(animal.model.Species),
                    WalkArtworkFrameCount);
                animal.feedingSprites = LoadActionSprites(
                    FeedingResourcePrefixFor(animal.model.Species),
                    FeedArtworkFrameCount);
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
            CharacterAnimationAction action = ResolveAction(animal, moving);
            if (animal.lastAnimationAction != action)
            {
                animal.lastAnimationAction = action;
                animal.actionStartedAt = Time.time;
            }

            float elapsed = Mathf.Max(0f, Time.time - animal.actionStartedAt);
            float speciesRate = WalkFramesPerSecondFor(animal.model.Species) /
                SteppedCharacterAnimation.FramesPerSecond;
            float sampledElapsed = action == CharacterAnimationAction.Walking
                ? elapsed * speciesRate
                : elapsed;
            bool hasWalkingArtwork = action == CharacterAnimationAction.Walking &&
                animal.walkingSprites != null &&
                animal.walkingSprites.Length == WalkFrameCount;
            bool hasFeedingArtwork = action == CharacterAnimationAction.Feeding &&
                animal.feedingSprites != null &&
                animal.feedingSprites.Length == FeedFrameCount;
            bool hasActionArtwork = hasWalkingArtwork || hasFeedingArtwork;
            float offset = IsLooping(action) && !hasActionArtwork
                ? animal.animationOffset
                : 0f;
            CharacterPose pose = SteppedCharacterAnimation.Sample(action, sampledElapsed, offset);
            Sprite selectedSprite = animal.standingSprite;
            bool usesActionArtwork = hasActionArtwork;
            if (animal.spriteRenderer != null)
            {
                if (usesActionArtwork)
                {
                    selectedSprite = hasWalkingArtwork
                        ? animal.walkingSprites[pose.FrameIndex]
                        : animal.feedingSprites[pose.FrameIndex];
                }
                else
                {
                    bool useAlternateFrame =
                        animal.alternateWalkSprite != null &&
                        SteppedCharacterAnimation.UseAlternateArtwork(action, pose.FrameIndex);
                    selectedSprite = useAlternateFrame
                        ? animal.alternateWalkSprite
                        : animal.standingSprite;
                }
                animal.spriteRenderer.sprite = selectedSprite;
                bool renderedFacingRight = action == CharacterAnimationAction.Turning &&
                    !SteppedCharacterAnimation.HasTurnPassedMidpoint(
                        Mathf.Max(0f, Time.time - animal.turnStartedAt))
                    ? animal.turnFromFacingRight
                    : animal.facingRight;
                animal.spriteRenderer.flipX = !renderedFacingRight;
            }

            animal.visual.localRotation = Quaternion.Euler(
                0f,
                ScreenFacingSpriteRotationDegrees + pose.SwayDegrees,
                0f);

            Vector3 frameBaseScale = animal.artworkBaseScale;
            if (usesActionArtwork && selectedSprite != null)
            {
                float uniformScale = LengthFor(animal.model.Species) /
                    Mathf.Max(0.001f, selectedSprite.bounds.size.x);
                frameBaseScale = Vector3.one * uniformScale;
            }
            animal.artwork.localScale = usesActionArtwork
                ? frameBaseScale
                : Vector3.Scale(
                    frameBaseScale,
                    new Vector3(pose.WidthScale, pose.HeightScale, 1f));
            float liftScale = StepLiftFor(animal.model.Species) / 0.004f;
            animal.artwork.localPosition = animal.artworkBasePosition +
                Vector3.up * (usesActionArtwork ? 0f : pose.Lift * liftScale);
        }

        private static CharacterAnimationAction ResolveAction(RuntimeAnimal animal, bool moving)
        {
            if (Time.time < animal.turnEndsAt)
            {
                return CharacterAnimationAction.Turning;
            }
            if (Time.time < animal.riseEndsAt)
            {
                return CharacterAnimationAction.Rising;
            }
            if (animal.model.State == AnimalActivityState.Feeding)
            {
                return CharacterAnimationAction.Feeding;
            }
            if (animal.model.State == AnimalActivityState.Resting)
            {
                return CharacterAnimationAction.Sitting;
            }
            return moving
                ? CharacterAnimationAction.Walking
                : CharacterAnimationAction.Idle;
        }

        private static void UpdateHeading(RuntimeAnimal animal, Vector2 movement)
        {
            Vector2 direction = movement.normalized;
            bool changedDirection = animal.lastMovementDirection.sqrMagnitude > 0.5f &&
                Vector2.Dot(animal.lastMovementDirection, direction) < 0.72f;
            bool nextFacingRight = Mathf.Abs(direction.x) > 0.0001f
                ? direction.x >= 0f
                : animal.facingRight;
            if (changedDirection || nextFacingRight != animal.facingRight)
            {
                animal.turnFromFacingRight = animal.facingRight;
                animal.facingRight = nextFacingRight;
                animal.turnStartedAt = Time.time;
                animal.turnEndsAt = Time.time + SteppedCharacterAnimation.TurnSeconds;
            }
            animal.lastMovementDirection = direction;
        }

        private static void BeginRise(RuntimeAnimal animal)
        {
            animal.riseEndsAt = Time.time + SteppedCharacterAnimation.RiseSeconds;
        }

        private static bool IsLooping(CharacterAnimationAction action)
        {
            return action == CharacterAnimationAction.Idle ||
                action == CharacterAnimationAction.Walking ||
                action == CharacterAnimationAction.Feeding;
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

        private static string FeedingResourcePrefixFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return "UrbanWildlife/Animals/pigeon-side-feed";
                case AnimalSpecies.Squirrel:
                    return "UrbanWildlife/Animals/squirrel-feed";
                default:
                    return "UrbanWildlife/Animals/fox-feed";
            }
        }

        private static string WalkingResourcePrefixFor(AnimalSpecies species)
        {
            switch (species)
            {
                case AnimalSpecies.Pigeon:
                    return "UrbanWildlife/Animals/pigeon-walk";
                case AnimalSpecies.Squirrel:
                    return "UrbanWildlife/Animals/squirrel-walk";
                default:
                    return "UrbanWildlife/Animals/fox-walk";
            }
        }

        private static Sprite[] LoadActionSprites(string prefix, int frameCount)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return new Sprite[0];
            }

            Sprite[] frames = new Sprite[frameCount];
            for (int index = 0; index < frameCount; index += 1)
            {
                frames[index] = Resources.Load<Sprite>(
                    $"{prefix}-{index + 1:00}-v01");
                if (frames[index] == null)
                {
                    Debug.LogWarning(
                        $"Action artwork frame missing: {prefix}-{index + 1:00}-v01. " +
                        "Using the procedural pose fallback.");
                    return new Sprite[0];
                }
            }
            return frames;
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

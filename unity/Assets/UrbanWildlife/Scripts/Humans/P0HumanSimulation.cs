using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UrbanWildlife.Cycle;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;
using UrbanWildlife.Presentation;

namespace UrbanWildlife.Humans
{
    [RequireComponent(typeof(LayoutPacketReader), typeof(P0CycleController), typeof(LayoutDebugView))]
    public sealed class P0HumanSimulation : MonoBehaviour
    {
        public const float ScreenFacingSpriteRotationDegrees = 0f;
        public const float WalkerDisplayLength = 0.89f;
        public const float DwellerDisplayLength = 0.84f;
        public const float VisitorDisplayLength = 0.85f;
        public const int IdleFrameCount = SteppedCharacterAnimation.IdleFrameCount;
        public const int TurnFrameCount = SteppedCharacterAnimation.TurnFrameCount;
        public const int WalkFrameCount = SteppedCharacterAnimation.WalkFrameCount;
        public const int FeedFrameCount = SteppedCharacterAnimation.FeedFrameCount;
        public const int HumanWalkArtworkFrameCount = 6;
        public const int WalkerWalkArtworkFrameCount = HumanWalkArtworkFrameCount;
        public const int DwellerWalkArtworkFrameCount = HumanWalkArtworkFrameCount;
        public const int VisitorWalkArtworkFrameCount = HumanWalkArtworkFrameCount;
        public const int VisitorFeedArtworkFrameCount = 6;
        public const int SitFrameCount = SteppedCharacterAnimation.SitFrameCount;
        public const int DwellerSitArtworkFrameCount = 4;
        public const int RiseFrameCount = SteppedCharacterAnimation.RiseFrameCount;
        public const float WalkFramesPerSecond = 6f;
        public const float WalkSwayMultiplier = 2f;
        public const float IdleSwaySeconds = 6.8f;
        public const float PresentationSaturation = 0.72f;
        public const float PresentationBrightness = 0.91f;
        public const float PresentationAmbientBlend = 0.16f;

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
            public Sprite standingSprite;
            public Sprite alternateWalkSprite;
            public Sprite[] walkingSprites;
            public Sprite[] feedingSprites;
            public Sprite[] sittingSprites;
            public Vector3 artworkBaseScale;
            public Vector3 artworkBasePosition;
            public float animationOffset;
            public bool facingRight;
            public bool turnFromFacingRight;
            public bool ownsMaterial;
            public HumanActivityState lastState;
            public CharacterAnimationAction lastAnimationAction;
            public Vector2 lastMovementDirection;
            public float actionStartedAt;
            public float turnStartedAt;
            public float turnEndsAt;
            public float riseEndsAt;
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
                bool hasMovement = !repeated && movement.sqrMagnitude > 0.000001f;
                bool moving = !repeated && human.model.State == HumanActivityState.Moving;
                if (hasMovement)
                {
                    UpdateHeading(human, movement);
                }

                if (human.lastState != human.model.State)
                {
                    if (IsActivityState(human.lastState) &&
                        human.model.State == HumanActivityState.Moving)
                    {
                        BeginRise(human);
                    }
                    human.lastState = human.model.State;
                    ApplyColour(human);
                }
                ApplyMotion(human, moving);
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
            else
            {
                foreach (RuntimeHuman human in humans)
                {
                    ApplyMotion(human, false);
                }
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
                    animationOffset = index * 0.83f,
                    facingRight = (index & 1) == 0,
                    turnFromFacingRight = (index & 1) == 0,
                    lastState = model.State,
                    lastAnimationAction = CharacterAnimationAction.Idle,
                    actionStartedAt = Time.time,
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
                Sprite alternateWalkSprite = Resources.Load<Sprite>(WalkAlternateResourcePathFor(human.model.Archetype));
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
                human.artworkBasePosition = artwork.transform.localPosition;
                human.renderer = spriteRenderer;
                human.spriteRenderer = spriteRenderer;
                human.standingSprite = sourceSprite;
                human.alternateWalkSprite = alternateWalkSprite;
                human.walkingSprites = LoadActionSprites(
                    WalkingResourcePrefixFor(human.model.Archetype),
                    HumanWalkArtworkFrameCount,
                    WalkingResourceVersionFor(human.model.Archetype));
                human.feedingSprites = LoadActionSprites(
                    FeedingResourcePrefixFor(human.model.Archetype),
                    VisitorFeedArtworkFrameCount);
                human.sittingSprites = LoadActionSprites(
                    SittingResourcePrefixFor(human.model.Archetype),
                    DwellerSitArtworkFrameCount);
                Material paletteMaterial = SpritePaletteMaterial.Create(
                    PresentationSaturation,
                    PresentationBrightness,
                    PresentationAmbientBlend);
                if (paletteMaterial != null)
                {
                    spriteRenderer.sharedMaterial = paletteMaterial;
                    human.ownsMaterial = true;
                }
                else
                {
                    human.ownsMaterial = false;
                    Debug.LogWarning("Park palette shader is unavailable; using the default human Sprite material.");
                }
                if (alternateWalkSprite == null)
                {
                    Debug.LogWarning($"Alternate walk sprite missing for {human.model.Archetype}; using the standing frame.");
                }
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
            human.artworkBasePosition = fallback.transform.localPosition;
            human.renderer = fallback.GetComponent<Renderer>();
            human.renderer.sharedMaterial = CreateMaterial(FallbackColourFor(human.model.Archetype));
            human.ownsMaterial = true;
            Debug.LogWarning($"Human sprite missing for {human.model.Archetype}; using fallback geometry.");
        }

        private static void ApplyMotion(RuntimeHuman human, bool moving)
        {
            CharacterAnimationAction action = ResolveAction(human, moving);
            if (human.lastAnimationAction != action)
            {
                human.lastAnimationAction = action;
                human.actionStartedAt = Time.time;
            }

            float elapsed = Mathf.Max(0f, Time.time - human.actionStartedAt);
            bool hasWalkingArtwork = action == CharacterAnimationAction.Walking &&
                human.walkingSprites != null &&
                human.walkingSprites.Length == WalkFrameCount;
            bool hasFeedingArtwork = action == CharacterAnimationAction.Feeding &&
                human.feedingSprites != null &&
                human.feedingSprites.Length == FeedFrameCount;
            bool hasSittingArtwork =
                (action == CharacterAnimationAction.Sitting ||
                 action == CharacterAnimationAction.Rising) &&
                human.sittingSprites != null &&
                human.sittingSprites.Length == SitFrameCount;
            bool hasActionArtwork = hasWalkingArtwork || hasFeedingArtwork || hasSittingArtwork;
            float offset = IsLooping(action) && !hasActionArtwork
                ? human.animationOffset
                : 0f;
            float sampledElapsed = hasWalkingArtwork
                ? elapsed * (WalkFramesPerSecond / SteppedCharacterAnimation.FramesPerSecond)
                : elapsed;
            CharacterPose pose = SteppedCharacterAnimation.Sample(action, sampledElapsed, offset);
            Sprite selectedSprite = human.standingSprite;
            bool usesActionArtwork = hasActionArtwork;
            if (human.spriteRenderer != null)
            {
                if (usesActionArtwork)
                {
                    if (hasWalkingArtwork)
                    {
                        selectedSprite = human.walkingSprites[pose.FrameIndex];
                    }
                    else if (hasFeedingArtwork)
                    {
                        selectedSprite = human.feedingSprites[pose.FrameIndex];
                    }
                    else
                    {
                        selectedSprite = human.sittingSprites[pose.FrameIndex];
                    }
                }
                else
                {
                    bool useAlternateFrame =
                        human.alternateWalkSprite != null &&
                        SteppedCharacterAnimation.UseAlternateArtwork(action, pose.FrameIndex);
                    selectedSprite = useAlternateFrame
                        ? human.alternateWalkSprite
                        : human.standingSprite;
                }
                human.spriteRenderer.sprite = selectedSprite;
                bool renderedFacingRight = action == CharacterAnimationAction.Turning &&
                    !SteppedCharacterAnimation.HasTurnPassedMidpoint(
                        Mathf.Max(0f, Time.time - human.turnStartedAt))
                    ? human.turnFromFacingRight
                    : human.facingRight;
                human.spriteRenderer.flipX = !renderedFacingRight;
            }

            float swayDegrees = hasWalkingArtwork
                ? pose.SwayDegrees * WalkSwayMultiplier
                : pose.SwayDegrees;
            human.visual.localRotation = Quaternion.Euler(
                0f,
                ScreenFacingSpriteRotationDegrees + swayDegrees,
                0f);

            Vector3 frameBaseScale = human.artworkBaseScale;
            if (usesActionArtwork && selectedSprite != null)
            {
                float uniformScale = LengthFor(human.model.Archetype) /
                    Mathf.Max(0.001f, selectedSprite.bounds.size.y);
                frameBaseScale = Vector3.one * uniformScale;
            }
            human.artwork.localScale = usesActionArtwork
                ? frameBaseScale
                : Vector3.Scale(
                    frameBaseScale,
                    new Vector3(pose.WidthScale, pose.HeightScale, 1f));
            human.artwork.localPosition = human.artworkBasePosition +
                Vector3.up * (usesActionArtwork ? 0f : pose.Lift);
        }

        private static CharacterAnimationAction ResolveAction(RuntimeHuman human, bool moving)
        {
            if (Time.time < human.turnEndsAt)
            {
                return CharacterAnimationAction.Turning;
            }
            if (Time.time < human.riseEndsAt)
            {
                return CharacterAnimationAction.Rising;
            }
            if (human.model.State == HumanActivityState.Visiting)
            {
                return CharacterAnimationAction.Feeding;
            }
            if (human.model.State == HumanActivityState.Dwelling)
            {
                return CharacterAnimationAction.Sitting;
            }
            return moving
                ? CharacterAnimationAction.Walking
                : CharacterAnimationAction.Idle;
        }

        private static void UpdateHeading(RuntimeHuman human, Vector2 movement)
        {
            Vector2 direction = movement.normalized;
            bool changedDirection = human.lastMovementDirection.sqrMagnitude > 0.5f &&
                Vector2.Dot(human.lastMovementDirection, direction) < 0.72f;
            bool nextFacingRight = Mathf.Abs(direction.x) > 0.0001f
                ? direction.x >= 0f
                : human.facingRight;
            if (changedDirection || nextFacingRight != human.facingRight)
            {
                human.turnFromFacingRight = human.facingRight;
                human.facingRight = nextFacingRight;
                human.turnStartedAt = Time.time;
                human.turnEndsAt = Time.time + SteppedCharacterAnimation.TurnSeconds;
            }
            human.lastMovementDirection = direction;
        }

        private static void BeginRise(RuntimeHuman human)
        {
            human.riseEndsAt = Time.time + SteppedCharacterAnimation.RiseSeconds;
        }

        private static bool IsActivityState(HumanActivityState state)
        {
            return state == HumanActivityState.Dwelling || state == HumanActivityState.Visiting;
        }

        private static bool IsLooping(CharacterAnimationAction action)
        {
            return action == CharacterAnimationAction.Idle ||
                action == CharacterAnimationAction.Walking ||
                action == CharacterAnimationAction.Feeding;
        }

        private static void ApplyColour(RuntimeHuman human)
        {
            bool visible = human.model.State != HumanActivityState.WaitingToEnter &&
                human.model.State != HumanActivityState.Finished;
            if (human.spriteRenderer != null)
            {
                human.spriteRenderer.enabled = visible;
                human.spriteRenderer.color = SpriteTintFor(human.model.State);
            }
            else if (human.renderer != null && human.renderer.sharedMaterial != null)
            {
                human.renderer.enabled = visible;
                human.renderer.sharedMaterial.color = FallbackColourFor(human.model.Archetype);
            }
        }

        private static string ResourcePathFor(HumanArchetype archetype)
        {
            switch (archetype)
            {
                case HumanArchetype.Walker:
                    return "UrbanWildlife/Humans/walker-topdown-v05";
                case HumanArchetype.Dweller:
                    return "UrbanWildlife/Humans/dweller-topdown-v03";
                default:
                    return "UrbanWildlife/Humans/visitor-topdown-v05";
            }
        }

        private static string WalkAlternateResourcePathFor(HumanArchetype archetype)
        {
            switch (archetype)
            {
                case HumanArchetype.Walker:
                    return "UrbanWildlife/Humans/walker-walk-b-v01";
                case HumanArchetype.Dweller:
                    return "UrbanWildlife/Humans/dweller-walk-b-v01";
                default:
                    return "UrbanWildlife/Humans/visitor-walk-b-v01";
            }
        }

        private static string FeedingResourcePrefixFor(HumanArchetype archetype)
        {
            return archetype == HumanArchetype.Visitor
                ? "UrbanWildlife/Humans/visitor-feed"
                : string.Empty;
        }

        private static string WalkingResourcePrefixFor(HumanArchetype archetype)
        {
            switch (archetype)
            {
                case HumanArchetype.Walker:
                    return "UrbanWildlife/Humans/walker-walk";
                case HumanArchetype.Dweller:
                    return "UrbanWildlife/Humans/dweller-walk";
                default:
                    return "UrbanWildlife/Humans/visitor-walk";
            }
        }

        private static string WalkingResourceVersionFor(HumanArchetype archetype)
        {
            return archetype == HumanArchetype.Walker ? "v02" : "v01";
        }

        private static string SittingResourcePrefixFor(HumanArchetype archetype)
        {
            return archetype == HumanArchetype.Dweller
                ? "UrbanWildlife/Humans/dweller-sit"
                : string.Empty;
        }

        private static Sprite[] LoadActionSprites(
            string prefix,
            int frameCount,
            string version = "v01")
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return new Sprite[0];
            }

            Sprite[] frames = new Sprite[frameCount];
            for (int index = 0; index < frameCount; index += 1)
            {
                frames[index] = Resources.Load<Sprite>(
                    $"{prefix}-{index + 1:00}-{version}");
                if (frames[index] == null)
                {
                    Debug.LogWarning(
                        $"Action artwork frame missing: {prefix}-{index + 1:00}-{version}. " +
                        "Using the procedural pose fallback.");
                    return new Sprite[0];
                }
            }
            return frames;
        }

        private static float LengthFor(HumanArchetype archetype)
        {
            switch (archetype)
            {
                case HumanArchetype.Walker:
                    return WalkerDisplayLength;
                case HumanArchetype.Dweller:
                    return DwellerDisplayLength;
                default:
                    return VisitorDisplayLength;
            }
        }

        private static Color SpriteTintFor(HumanActivityState state)
        {
            switch (state)
            {
                case HumanActivityState.WaitingToEnter:
                    return new Color(0.68f, 0.72f, 0.7f, 0.88f);
                case HumanActivityState.Dwelling:
                    return new Color(1f, 0.97f, 0.88f, 1f);
                case HumanActivityState.Visiting:
                    return new Color(0.94f, 1f, 0.95f, 1f);
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

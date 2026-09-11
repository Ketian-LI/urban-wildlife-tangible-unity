using UnityEngine;
using UrbanWildlife.Input;
using UrbanWildlife.Planning;
using UrbanWildlife.Humans;
using UrbanWildlife.Animals;

namespace UrbanWildlife.Cycle
{
    [RequireComponent(typeof(P0CycleController), typeof(P0ConstraintManager), typeof(LayoutPacketReader))]
    public sealed class P0ControlPanel : MonoBehaviour
    {
        private enum TraceDisplayMode
        {
            Human,
            Animal,
            Combined,
        }

        private P0CycleController cycle;
        private P0ConstraintManager constraints;
        private LayoutPacketReader reader;
        private P0HumanSimulation humanSimulation;
        private P0AnimalSimulation animalSimulation;
        private P0ElectronicDemoInput electronicDemoInput;
        private GUIStyle titleStyle;
        private GUIStyle noticeStyle;
        private GUIStyle phaseStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle toggleButtonStyle;
        private GUIStyle selectedToggleButtonStyle;
        private GUIStyle statusStyle;
        private GUIStyle passedStyle;
        private GUIStyle fixStyle;
        private GUIStyle panelStyle;
        private Texture2D panelTexture;
        private Texture2D panelShadowTexture;
        private Texture2D brassTexture;
        private Texture2D innerBorderTexture;
        private Texture2D buttonTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D selectedButtonTexture;
        private Texture2D boltTexture;
        private TraceDisplayMode traceDisplayMode = TraceDisplayMode.Combined;

        private void Awake()
        {
            cycle = GetComponent<P0CycleController>();
            constraints = GetComponent<P0ConstraintManager>();
            reader = GetComponent<LayoutPacketReader>();
            humanSimulation = GetComponent<P0HumanSimulation>();
            animalSimulation = GetComponent<P0AnimalSimulation>();
            electronicDemoInput = GetComponent<P0ElectronicDemoInput>();
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.D) &&
                cycle.Phase == P0Phase.Plan && electronicDemoInput != null)
            {
                electronicDemoInput.TryPrepareCurrentCycle();
                return;
            }

            if (!UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                return;
            }

            if (cycle.Phase == P0Phase.Plan)
            {
                cycle.ConfirmCurrentPlan();
            }
            else if (cycle.Phase == P0Phase.Confirm && cycle.CanStartRun)
            {
                cycle.StartRun();
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            float scale = Mathf.Clamp(Screen.height / 900f, 0.75f, 1.35f);
            bool running = cycle.Phase == P0Phase.Run;
            bool observing = cycle.Phase == P0Phase.Observe;
            bool compact = running || observing;
            float width = Mathf.Min((running ? 430f : 440f) * scale, Screen.width - 32f);
            bool hasMemory = cycle.Mechanics.LastMemory != null;
            float requestedHeight = running
                ? hasMemory ? 655f : 555f
                : observing
                    ? 730f
                    : cycle.Phase == P0Phase.Confirm
                        ? hasMemory ? 900f : 830f
                        : hasMemory ? 900f : 555f;
            float height = Mathf.Min(requestedHeight * scale, Screen.height - 32f);
            Rect panel = new Rect(16f, 16f, width, height);
            GUI.DrawTexture(
                new Rect(panel.x + 7f * scale, panel.y + 8f * scale, panel.width, panel.height),
                panelShadowTexture);
            GUI.Box(panel, GUIContent.none, panelStyle);
            DrawParkNoticeFrame(panel, scale);
            Color previousColour = GUI.color;
            GUI.color = PhaseColour();
            GUI.DrawTexture(
                new Rect(panel.x + 8f * scale, panel.y + 12f * scale, 4f * scale, panel.height - 24f * scale),
                Texture2D.whiteTexture);
            GUI.color = previousColour;

            GUILayout.BeginArea(new Rect(panel.x + 27f, panel.y + 18f, panel.width - 54f, panel.height - 36f));
            GUILayout.Label("BOROUGH PARKS · FIELD NOTICE", noticeStyle);
            GUILayout.Label("URBAN WILDLIFE PLANNER", titleStyle);
            GUILayout.Label(PhaseLine(), phaseStyle);
            DrawScenarioBrief();
            if (cycle.Phase != P0Phase.Observe)
            {
                DrawMemoryContext(running);
            }

            if (cycle.Phase == P0Phase.Run || cycle.Phase == P0Phase.Observe)
            {
                GUILayout.Label($"{Mathf.CeilToInt(cycle.RemainingSeconds)} seconds remaining", bodyStyle);
            }
            else
            {
                GUILayout.Label(
                    electronicDemoInput == null
                        ? "SPACE = current action"
                        : "D = fresh electronic layout   SPACE = current action",
                    bodyStyle);
            }

            if (compact)
            {
                DrawTraceControls();
                DrawAgentSummary();
                if (running)
                {
                    DrawScore(CurrentScore(), "LIVE PLANNING SCORE");
                    GUILayout.Space(6f);
                    GUILayout.Label($"PREDICTION  {cycle.Mechanics.PredictionSummary()}", bodyStyle);
                }
                else
                {
                    DrawObservation();
                }
                GUILayout.EndArea();
                return;
            }

            DrawAgentSummary();

            GUILayout.Space(10f);
            DrawConstraintStatus();
            if (cycle.Phase == P0Phase.Confirm)
            {
                DrawPredictionControls();
            }
            DrawElectronicDemoControl();
            GUILayout.FlexibleSpace();
            DrawActionButton();
            GUILayout.EndArea();
        }

        private void DrawScenarioBrief()
        {
            P0CycleProfile profile = cycle.CurrentProfile;
            GUILayout.Label(profile.Title.ToUpperInvariant(), statusStyle);
            GUILayout.Label(profile.Brief, bodyStyle);
            GUILayout.Label(
                $"This run: {profile.PigeonCount} pigeons · {profile.SquirrelCount} squirrels · {profile.FoxCount} foxes",
                bodyStyle);
        }

        private void DrawMemoryContext(bool compact)
        {
            if (cycle.Mechanics.LastMemory == null)
            {
                return;
            }

            GUILayout.Space(5f);
            GUILayout.Label("MEMORY FROM LAST RUN", statusStyle);
            GUILayout.Label(cycle.Mechanics.MemorySummary(), bodyStyle);
            if (!compact)
            {
                GUILayout.Label(cycle.Mechanics.MemoryEffectSummary(), bodyStyle);
            }
        }

        private void DrawAgentSummary()
        {
            if (humanSimulation != null && humanSimulation.AgentCount > 0)
            {
                GUILayout.Label(
                    $"PEOPLE  Walker {humanSimulation.WalkerCount} · Dweller {humanSimulation.DwellerCount} · " +
                    $"Visitor {humanSimulation.VisitorCount} · Trips {humanSimulation.CompletedTrips}",
                    bodyStyle);
            }
            if (animalSimulation != null && animalSimulation.AgentCount > 0)
            {
                GUILayout.Label(
                    $"WILDLIFE  Pigeon {animalSimulation.PigeonCount} · Squirrel {animalSimulation.SquirrelCount} · " +
                    $"Fox {animalSimulation.FoxCount} · Feeds {animalSimulation.FeedEvents} · " +
                    $"Avoids {animalSimulation.AvoidanceEvents}",
                    bodyStyle);
            }
        }

        private void DrawElectronicDemoControl()
        {
            if (cycle.Phase != P0Phase.Plan || electronicDemoInput == null)
            {
                return;
            }

            GUILayout.Space(8f);
            if (GUILayout.Button("LOAD ELECTRONIC DEMO", toggleButtonStyle, GUILayout.Height(38f)))
            {
                electronicDemoInput.TryPrepareCurrentCycle();
            }
            if (!string.IsNullOrWhiteSpace(electronicDemoInput.LastMessage))
            {
                GUILayout.Label(electronicDemoInput.LastMessage, bodyStyle);
            }
        }

        private void DrawPredictionControls()
        {
            GUILayout.Space(10f);
            GUILayout.Label("BEFORE RUN: MAKE A PREDICTION", statusStyle);
            GUILayout.Label("1. Which species will record the most feeding?", bodyStyle);
            GUILayout.BeginHorizontal();
            DrawFeederButton("Pigeon", P0PredictedFeeder.Pigeon);
            DrawFeederButton("Squirrel", P0PredictedFeeder.Squirrel);
            DrawFeederButton("Fox", P0PredictedFeeder.Fox);
            GUILayout.EndHorizontal();

            GUILayout.Label("2. Where will human–wildlife pressure be most likely?", bodyStyle);
            GUILayout.BeginHorizontal();
            DrawConflictButton("Main route", P0PredictedConflictArea.MainRoute);
            DrawConflictButton("Activity node", P0PredictedConflictArea.ActivityNode);
            DrawConflictButton("Woodland edge", P0PredictedConflictArea.WoodlandEdge);
            GUILayout.EndHorizontal();

            GUILayout.Label(cycle.Mechanics.PredictionSummary(), bodyStyle);
        }

        private void DrawFeederButton(string label, P0PredictedFeeder value)
        {
            GUIStyle style = cycle.Mechanics.PredictedFeeder == value
                ? selectedToggleButtonStyle
                : toggleButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(34f)))
            {
                cycle.SetPredictedFeeder(value);
            }
        }

        private void DrawConflictButton(string label, P0PredictedConflictArea value)
        {
            GUIStyle style = cycle.Mechanics.PredictedConflictArea == value
                ? selectedToggleButtonStyle
                : toggleButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(40f)))
            {
                cycle.SetPredictedConflictArea(value);
            }
        }

        private void DrawObservation()
        {
            if (animalSimulation == null)
            {
                return;
            }

            GUILayout.Space(8f);
            DrawScore(cycle.Mechanics.LastMemory?.Score ?? CurrentScore(), "CYCLE PLANNING SCORE");
            GUILayout.Space(5f);
            GUILayout.Label("WHAT HAPPENED", statusStyle);
            GUILayout.Label(
                cycle.Mechanics.BuildObservationSummary(
                    animalSimulation.PigeonFeedEvents,
                    animalSimulation.SquirrelFeedEvents,
                    animalSimulation.FoxFeedEvents,
                    animalSimulation.AvoidanceEvents),
                bodyStyle);
            GUILayout.Space(5f);
            GUILayout.Label("WHY IT MAY HAVE HAPPENED", statusStyle);
            GUILayout.Label(
                cycle.Mechanics.BuildCausalExplanation(
                    animalSimulation.PigeonFeedEvents,
                    animalSimulation.SquirrelFeedEvents,
                    animalSimulation.FoxFeedEvents,
                    animalSimulation.AvoidanceEvents),
                bodyStyle);
            GUILayout.Label(
                "Compare the cool human trace with the warm animal trace to inspect the predicted pressure area.",
                bodyStyle);
            GUILayout.Space(5f);
            GUILayout.Label("SAVED FOR THE NEXT CYCLE", statusStyle);
            GUILayout.Label(cycle.Mechanics.MemoryEffectSummary(), bodyStyle);
        }

        private void DrawTraceControls()
        {
            GUILayout.Space(6f);
            GUILayout.Label("FIELD TRACKS · LIVE SURVEY", statusStyle);
            GUILayout.BeginHorizontal();
            DrawTraceButton("People", TraceDisplayMode.Human);
            DrawTraceButton("Wildlife", TraceDisplayMode.Animal);
            DrawTraceButton("All tracks", TraceDisplayMode.Combined);
            GUILayout.EndHorizontal();
            GUILayout.Label(
                $"TEAL footsteps {humanSimulation?.TraceDistanceCm ?? 0f:0} cm  ·  " +
                $"OCHRE tracks {animalSimulation?.TraceDistanceCm ?? 0f:0} cm",
                bodyStyle);
        }

        private void DrawTraceButton(string label, TraceDisplayMode mode)
        {
            GUIStyle style = traceDisplayMode == mode
                ? selectedToggleButtonStyle
                : toggleButtonStyle;
            if (GUILayout.Button(label, style, GUILayout.Height(32f)))
            {
                traceDisplayMode = mode;
                humanSimulation?.SetTraceVisible(mode != TraceDisplayMode.Animal);
                animalSimulation?.SetTraceVisible(mode != TraceDisplayMode.Human);
            }
        }

        private P0CycleScore CurrentScore()
        {
            return P0CycleScore.Calculate(
                humanSimulation?.SuccessfulAgentCount ?? 0,
                humanSimulation?.AgentCount ?? 0,
                animalSimulation?.FedPigeonCount ?? 0,
                animalSimulation?.PigeonCount ?? 0,
                animalSimulation?.FedSquirrelCount ?? 0,
                animalSimulation?.SquirrelCount ?? 0,
                animalSimulation?.FedFoxCount ?? 0,
                animalSimulation?.FoxCount ?? 0);
        }

        private void DrawScore(P0CycleScore score, string heading)
        {
            if (score == null)
            {
                return;
            }

            GUILayout.Space(5f);
            GUILayout.Label(heading, statusStyle);
            GUILayout.Label(score.Summary(), bodyStyle);
        }

        private void DrawConstraintStatus()
        {
            P0ConstraintResult result = constraints.LatestResult;
            if (result == null)
            {
                GUILayout.Label("No layout confirmed yet.", statusStyle);
                if (!string.IsNullOrWhiteSpace(reader.LastError))
                {
                    GUILayout.Label($"Last check: {reader.LastError}", bodyStyle);
                }
                return;
            }

            DrawStatusLine("Human route connected", result.human_connected);
            DrawStatusLine("Animal route reachable", result.animal_reachable);
            DrawStatusLine("Activity hotspots valid", result.food_hotspot_valid);
            DrawStatusLine($"Changes {result.changes_used}/{result.changes_allowed}", result.within_change_budget);
        }

        private void DrawStatusLine(string label, bool passed)
        {
            GUILayout.Label(StatusLine(label, passed), passed ? passedStyle : fixStyle);
        }

        private void DrawActionButton()
        {
            switch (cycle.Phase)
            {
                case P0Phase.Plan:
                    if (GUILayout.Button("CONFIRM LAYOUT", buttonStyle, GUILayout.Height(62f)))
                    {
                        cycle.ConfirmCurrentPlan();
                    }
                    break;
                case P0Phase.Confirm:
                    GUI.enabled = cycle.CanStartRun;
                    string startLabel = cycle.CanStartRun
                        ? "START RUN"
                        : cycle.ConstraintsReady
                            ? "MAKE BOTH PREDICTIONS"
                            : "CHECKING…";
                    if (GUILayout.Button(startLabel, buttonStyle, GUILayout.Height(62f)))
                    {
                        cycle.StartRun();
                    }
                    GUI.enabled = true;
                    break;
                case P0Phase.Run:
                    GUILayout.Label("Simulation running", titleStyle);
                    break;
                case P0Phase.Observe:
                    GUILayout.Label("Observe the outcome", titleStyle);
                    break;
                case P0Phase.Complete:
                    if (GUILayout.Button("RESET SESSION", buttonStyle, GUILayout.Height(62f)))
                    {
                        cycle.ResetSession();
                    }
                    break;
            }
        }

        private string PhaseLine()
        {
            int displayCycle = cycle.Phase == P0Phase.Complete
                ? cycle.CyclesPerSession
                : Mathf.Min(cycle.CycleIndex + 1, cycle.CyclesPerSession);
            return $"{cycle.Phase.ToString().ToUpperInvariant()}   CYCLE {displayCycle}/{cycle.CyclesPerSession}";
        }

        private static string StatusLine(string label, bool passed)
        {
            return $"{(passed ? "PASS" : "FIX")}  {label}";
        }

        private Color PhaseColour()
        {
            switch (cycle.Phase)
            {
                case P0Phase.Plan:
                    return new Color(0.96f, 0.68f, 0.22f, 1f);
                case P0Phase.Confirm:
                    return new Color(0.35f, 0.75f, 0.82f, 1f);
                case P0Phase.Run:
                    return new Color(0.95f, 0.27f, 0.62f, 1f);
                case P0Phase.Observe:
                    return new Color(0.48f, 0.72f, 0.4f, 1f);
                default:
                    return new Color(0.74f, 0.64f, 0.86f, 1f);
            }
        }

        private void EnsureStyles()
        {
            if (titleStyle != null && panelTexture != null && panelShadowTexture != null &&
                brassTexture != null && innerBorderTexture != null && buttonTexture != null &&
                buttonHoverTexture != null && selectedButtonTexture != null && boltTexture != null)
            {
                return;
            }

            Color parkGreen = new Color(0.055f, 0.16f, 0.125f, 0.965f);
            Color deepGreen = new Color(0.035f, 0.105f, 0.082f, 1f);
            Color brass = new Color(0.72f, 0.56f, 0.28f, 1f);
            Color parchment = new Color(0.89f, 0.86f, 0.72f, 1f);
            Color ink = new Color(0.06f, 0.14f, 0.105f, 1f);
            Color paleInk = new Color(0.83f, 0.86f, 0.76f, 1f);

            panelTexture = CreateGrainTexture("Park notice green", parkGreen);
            panelShadowTexture = CreateSolidTexture("Park notice shadow", new Color(0f, 0f, 0f, 0.42f));
            brassTexture = CreateSolidTexture("Park notice brass", brass);
            innerBorderTexture = CreateSolidTexture("Park notice inner border", new Color(0.82f, 0.77f, 0.58f, 0.72f));
            buttonTexture = CreateSolidTexture("Park notice button", new Color(0.78f, 0.76f, 0.64f, 0.96f));
            buttonHoverTexture = CreateSolidTexture("Park notice button hover", parchment);
            selectedButtonTexture = CreateSolidTexture("Park notice selected", new Color(0.78f, 0.58f, 0.24f, 1f));
            boltTexture = CreateBoltTexture(18, brass, deepGreen);
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTexture;

            noticeStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = brass },
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.94f, 0.82f) },
            };
            phaseStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.97f, 0.91f, 0.7f) },
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = paleInk },
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.82f, 0.55f) },
            };
            passedStyle = new GUIStyle(statusStyle)
            {
                normal = { textColor = new Color(0.58f, 0.82f, 0.58f) },
            };
            fixStyle = new GUIStyle(statusStyle)
            {
                normal = { textColor = new Color(0.98f, 0.56f, 0.36f) },
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { background = selectedButtonTexture, textColor = ink },
                hover = { background = buttonHoverTexture, textColor = ink },
                active = { background = brassTexture, textColor = ink },
            };
            toggleButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(8, 8, 5, 5),
                normal = { background = buttonTexture, textColor = ink },
                hover = { background = buttonHoverTexture, textColor = ink },
                active = { background = brassTexture, textColor = ink },
            };
            selectedToggleButtonStyle = new GUIStyle(toggleButtonStyle)
            {
                normal = { background = selectedButtonTexture, textColor = ink },
                hover = { background = selectedButtonTexture, textColor = ink },
            };
        }

        private void DrawParkNoticeFrame(Rect panel, float scale)
        {
            float outer = 3f * scale;
            float inset = 7f * scale;
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, outer), brassTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.yMax - outer, panel.width, outer), brassTexture);
            GUI.DrawTexture(new Rect(panel.x, panel.y, outer, panel.height), brassTexture);
            GUI.DrawTexture(new Rect(panel.xMax - outer, panel.y, outer, panel.height), brassTexture);
            GUI.DrawTexture(new Rect(panel.x + inset, panel.y + inset, panel.width - inset * 2f, 1f), innerBorderTexture);
            GUI.DrawTexture(new Rect(panel.x + inset, panel.yMax - inset, panel.width - inset * 2f, 1f), innerBorderTexture);
            GUI.DrawTexture(new Rect(panel.x + inset, panel.y + inset, 1f, panel.height - inset * 2f), innerBorderTexture);
            GUI.DrawTexture(new Rect(panel.xMax - inset, panel.y + inset, 1f, panel.height - inset * 2f), innerBorderTexture);

            float boltSize = 11f * scale;
            float boltInset = 8f * scale;
            GUI.DrawTexture(new Rect(panel.x + boltInset, panel.y + boltInset, boltSize, boltSize), boltTexture);
            GUI.DrawTexture(new Rect(panel.xMax - boltInset - boltSize, panel.y + boltInset, boltSize, boltSize), boltTexture);
            GUI.DrawTexture(new Rect(panel.x + boltInset, panel.yMax - boltInset - boltSize, boltSize, boltSize), boltTexture);
            GUI.DrawTexture(new Rect(panel.xMax - boltInset - boltSize, panel.yMax - boltInset - boltSize, boltSize, boltSize), boltTexture);
        }

        private static Texture2D CreateSolidTexture(string name, Color colour)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            texture.SetPixels(new[] { colour, colour, colour, colour });
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateGrainTexture(string name, Color baseColour)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y += 1)
            {
                for (int x = 0; x < size; x += 1)
                {
                    uint hash = (uint)(x * 374761393 + y * 668265263);
                    hash = (hash ^ (hash >> 13)) * 1274126177u;
                    float grain = ((hash & 255u) / 255f - 0.5f) * 0.035f;
                    float vertical = Mathf.Sin(x * 0.7f + y * 0.08f) * 0.007f;
                    pixels[y * size + x] = new Color(
                        Mathf.Clamp01(baseColour.r + grain + vertical),
                        Mathf.Clamp01(baseColour.g + grain + vertical),
                        Mathf.Clamp01(baseColour.b + grain + vertical),
                        baseColour.a);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateBoltTexture(int size, Color brass, Color groove)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Park notice bolt",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            Color[] pixels = new Color[size * size];
            Vector2 centre = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.46f;
            for (int y = 0; y < size; y += 1)
            {
                for (int x = 0; x < size; x += 1)
                {
                    Vector2 offset = new Vector2(x, y) - centre;
                    float distance = offset.magnitude;
                    Color colour = Color.clear;
                    if (distance <= radius)
                    {
                        colour = distance > radius * 0.78f ? groove : brass;
                        if (Mathf.Abs(offset.y - offset.x * 0.18f) < 1.15f && distance < radius * 0.62f)
                        {
                            colour = groove;
                        }
                    }
                    pixels[y * size + x] = colour;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnDestroy()
        {
            Texture2D[] textures =
            {
                panelTexture,
                panelShadowTexture,
                brassTexture,
                innerBorderTexture,
                buttonTexture,
                buttonHoverTexture,
                selectedButtonTexture,
                boltTexture,
            };
            foreach (Texture2D texture in textures)
            {
                if (texture != null)
                {
                    Destroy(texture);
                }
            }
        }
    }
}

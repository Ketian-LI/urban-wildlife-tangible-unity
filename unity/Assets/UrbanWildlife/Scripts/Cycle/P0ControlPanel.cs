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
        private P0CycleController cycle;
        private P0ConstraintManager constraints;
        private LayoutPacketReader reader;
        private P0HumanSimulation humanSimulation;
        private P0AnimalSimulation animalSimulation;
        private P0ElectronicDemoInput electronicDemoInput;
        private GUIStyle titleStyle;
        private GUIStyle phaseStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle statusStyle;
        private GUIStyle passedStyle;
        private GUIStyle fixStyle;
        private GUIStyle panelStyle;
        private Texture2D panelTexture;

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
            bool compact = cycle.Phase == P0Phase.Run || cycle.Phase == P0Phase.Observe;
            float width = Mathf.Min((compact ? 360f : 420f) * scale, Screen.width - 32f);
            float height = Mathf.Min((compact ? 205f : 470f) * scale, Screen.height - 32f);
            Rect panel = new Rect(16f, 16f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);
            Color previousColour = GUI.color;
            GUI.color = PhaseColour();
            GUI.DrawTexture(new Rect(panel.x, panel.y, 6f * scale, panel.height), Texture2D.whiteTexture);
            GUI.color = previousColour;

            GUILayout.BeginArea(new Rect(panel.x + 22f, panel.y + 18f, panel.width - 44f, panel.height - 36f));
            GUILayout.Label("URBAN WILDLIFE PLANNER", titleStyle);
            GUILayout.Label(PhaseLine(), phaseStyle);

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

            DrawAgentSummary();
            if (compact)
            {
                GUILayout.EndArea();
                return;
            }

            GUILayout.Space(10f);
            DrawConstraintStatus();
            DrawElectronicDemoControl();
            GUILayout.FlexibleSpace();
            DrawActionButton();
            GUILayout.EndArea();
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
            if (GUILayout.Button("LOAD ELECTRONIC DEMO", GUILayout.Height(38f)))
            {
                electronicDemoInput.TryPrepareCurrentCycle();
            }
            if (!string.IsNullOrWhiteSpace(electronicDemoInput.LastMessage))
            {
                GUILayout.Label(electronicDemoInput.LastMessage, bodyStyle);
            }
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
            DrawStatusLine("Food hotspots valid", result.food_hotspot_valid);
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
                    if (GUILayout.Button(cycle.CanStartRun ? "START RUN" : "CHECKING…", buttonStyle, GUILayout.Height(62f)))
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
            if (titleStyle != null)
            {
                return;
            }

            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.035f, 0.045f, 0.055f, 0.94f));
            panelTexture.Apply();
            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = panelTexture;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.94f, 0.95f, 0.9f) },
            };
            phaseStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.82f, 0.85f) },
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.9f, 0.92f) },
            };
            passedStyle = new GUIStyle(statusStyle)
            {
                normal = { textColor = new Color(0.5f, 0.88f, 0.58f) },
            };
            fixStyle = new GUIStyle(statusStyle)
            {
                normal = { textColor = new Color(1f, 0.58f, 0.42f) },
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
            };
        }

        private void OnDestroy()
        {
            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }
        }
    }
}

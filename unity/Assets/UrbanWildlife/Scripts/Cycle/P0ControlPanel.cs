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
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;
        private GUIStyle statusStyle;
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
            float width = Mathf.Min(500f * scale, Screen.width - 32f);
            float height = Mathf.Min(520f * scale, Screen.height - 32f);
            Rect panel = new Rect(16f, 16f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 22f, panel.y + 18f, panel.width - 44f, panel.height - 36f));
            GUILayout.Label("URBAN WILDLIFE — P0", titleStyle);
            GUILayout.Space(6f);
            GUILayout.Label(PhaseLine(), titleStyle);

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

            if (humanSimulation != null && humanSimulation.AgentCount > 0)
            {
                GUILayout.Label(
                    $"HUMANS  W {humanSimulation.WalkerCount}  D {humanSimulation.DwellerCount}  " +
                    $"V {humanSimulation.VisitorCount}  TRIPS {humanSimulation.CompletedTrips}",
                    bodyStyle);
            }
            if (animalSimulation != null && animalSimulation.AgentCount > 0)
            {
                GUILayout.Label(
                    $"ANIMALS  P {animalSimulation.PigeonCount}  S {animalSimulation.SquirrelCount}  " +
                    $"F {animalSimulation.FoxCount}  FEEDS {animalSimulation.FeedEvents}  " +
                    $"AVOIDS {animalSimulation.AvoidanceEvents}",
                    bodyStyle);
            }

            GUILayout.Space(12f);
            DrawConstraintStatus();
            DrawElectronicDemoControl();
            GUILayout.FlexibleSpace();
            DrawActionButton();
            GUILayout.EndArea();
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

            GUILayout.Label(StatusLine("Human route connected", result.human_connected), statusStyle);
            GUILayout.Label(StatusLine("Animal route reachable", result.animal_reachable), statusStyle);
            GUILayout.Label(StatusLine("Food hotspots valid", result.food_hotspot_valid), statusStyle);
            GUILayout.Label(
                StatusLine($"Changes {result.changes_used}/{result.changes_allowed}", result.within_change_budget),
                statusStyle);
        }

        private void DrawActionButton()
        {
            switch (cycle.Phase)
            {
                case P0Phase.Plan:
                    if (GUILayout.Button("CONFIRM LAYOUT", buttonStyle, GUILayout.Height(78f)))
                    {
                        cycle.ConfirmCurrentPlan();
                    }
                    break;
                case P0Phase.Confirm:
                    GUI.enabled = cycle.CanStartRun;
                    if (GUILayout.Button(cycle.CanStartRun ? "START RUN" : "CHECKING…", buttonStyle, GUILayout.Height(78f)))
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
                    if (GUILayout.Button("RESET SESSION", buttonStyle, GUILayout.Height(78f)))
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
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                wordWrap = true,
                normal = { textColor = new Color(0.78f, 0.82f, 0.85f) },
            };
            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.85f, 0.9f, 0.92f) },
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 28,
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

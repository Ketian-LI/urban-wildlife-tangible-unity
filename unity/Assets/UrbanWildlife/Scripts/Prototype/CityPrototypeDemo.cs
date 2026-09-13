using System;
using System.Linq;
using UnityEngine;
using UrbanWildlife.City;
using UrbanWildlife.Mobility;

namespace UrbanWildlife.Prototype
{
    [ExecuteAlways]
    public sealed class CityPrototypeDemo : MonoBehaviour
    {
        private const float MapWidth = 12f;
        private const float MapHeight = 8f;
        private const float RuntimeSpeed = 4f;
        private const string ParkMapResource = "UrbanWildlife/Environment/park-board-s001-v03";

        private sealed class ActorView
        {
            public GameObject human;
            public GameObject vehicle;
            public Vector3 lastPosition;
            public bool hasLastPosition;
        }

        private CityState city;
        private CityMobilityPlan plan;
        private CityMobilitySimulation mobility;
        private ActorView[] actorViews = Array.Empty<ActorView>();
        private GameObject generatedRoot;
        private GameObject pedestrianRoot;
        private bool paused;
        private bool showPedestrianNetwork = true;
        private float completeElapsed;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle headingStyle;
        private GUIStyle bodyStyle;
        private GUIStyle metricStyle;
        private GUIStyle buttonStyle;

        public int GeneratedBuildingCount { get; private set; }
        public int GeneratedVehicleRoadCount { get; private set; }
        public int GeneratedPedestrianLinkCount { get; private set; }
        public int RepresentativeAgentCount => plan?.RepresentativeAgentCount ?? 0;
        public int RepresentedPopulation => plan?.RepresentedPopulation ?? 0;
        public int VehicleTripCount => plan?.DriveTripCount ?? 0;

        private void OnEnable()
        {
            ConfigureCamera();
            if (!Application.isPlaying)
            {
                InitializePrototype(true);
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                InitializePrototype(false);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || mobility == null || paused)
            {
                return;
            }
            mobility.Tick(Time.deltaTime * RuntimeSpeed);
            UpdateActors();
            if (mobility.AllComplete)
            {
                completeElapsed += Time.deltaTime;
                if (completeElapsed >= 3f)
                {
                    InitializePrototype(false);
                }
            }
        }

        public void InitializePrototype(bool advancePreview)
        {
            ClearGenerated();
            city = CityPrototypeStateFactory.Create();
            plan = CityTripPlanner.CreatePlan(city);
            mobility = new CityMobilitySimulation(plan, city.bounds);
            completeElapsed = 0f;
            paused = false;

            generatedRoot = new GameObject("Generated City Prototype");
            generatedRoot.transform.SetParent(transform, false);
            if (!Application.isPlaying)
            {
                generatedRoot.hideFlags = HideFlags.DontSave;
            }
            BuildBoard();
            BuildGreenPatches();
            BuildNetworks();
            BuildBuildings();
            BuildActors();
            if (advancePreview)
            {
                mobility.Tick(8f);
                UpdateActors();
            }
        }

        private void ConfigureCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return;
            }
            camera.rect = new Rect(0.29f, 0f, 0.71f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 4.45f;
            camera.backgroundColor = new Color(0.045f, 0.075f, 0.055f, 1f);
        }

        private void BuildBoard()
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "City board base";
            board.transform.SetParent(generatedRoot.transform, false);
            board.transform.localPosition = new Vector3(0f, -0.08f, 0f);
            board.transform.localScale = new Vector3(MapWidth + 0.18f, 0.14f, MapHeight + 0.18f);
            RemoveCollider(board);
            SetMaterial(board, new Color(0.035f, 0.075f, 0.052f, 1f));

            Sprite mapSprite = Resources.Load<Sprite>(ParkMapResource);
            if (mapSprite == null)
            {
                return;
            }
            GameObject artwork = new GameObject("Urban neighbourhood park artwork");
            artwork.transform.SetParent(generatedRoot.transform, false);
            artwork.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            artwork.transform.localScale = new Vector3(
                MapWidth / Mathf.Max(0.001f, mapSprite.bounds.size.x),
                MapHeight / Mathf.Max(0.001f, mapSprite.bounds.size.y),
                1f);
            SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
            renderer.sprite = mapSprite;
            renderer.color = new Color(0.82f, 0.84f, 0.72f, 0.96f);
            renderer.sortingOrder = -50;
        }

        private void BuildGreenPatches()
        {
            GameObject root = ChildRoot("Green patches");
            foreach (CityGreenPatch patch in city.green_patches)
            {
                Color colour;
                switch (patch.type)
                {
                    case CityGreenPatchType.Woodland:
                        colour = new Color(0.055f, 0.23f, 0.13f, 0.76f);
                        break;
                    case CityGreenPatchType.ShrubGarden:
                        colour = new Color(0.23f, 0.42f, 0.18f, 0.70f);
                        break;
                    default:
                        colour = new Color(0.46f, 0.58f, 0.26f, 0.28f);
                        break;
                }
                CreatePolygon(root.transform, patch.id, patch.polygon_norm, 0.025f, colour);
            }
        }

        private void BuildNetworks()
        {
            GameObject roadRoot = ChildRoot("Vehicle road network");
            foreach (CityVehicleRoad road in city.vehicle_roads)
            {
                float width = road.width_units / city.bounds.width_units * MapWidth;
                CreateLine(
                    roadRoot.transform,
                    road.id + " kerb",
                    road.points_norm,
                    width + 0.10f,
                    new Color(0.72f, 0.67f, 0.53f, 1f),
                    0.045f,
                    -8);
                CreateLine(
                    roadRoot.transform,
                    road.id + " asphalt",
                    road.points_norm,
                    width,
                    new Color(0.075f, 0.082f, 0.078f, 1f),
                    0.055f,
                    -7);
            }

            pedestrianRoot = ChildRoot("Pedestrian link network");
            foreach (CityPedestrianLink link in city.pedestrian_links)
            {
                float width = link.width_units / city.bounds.width_units * MapWidth;
                CreateLine(
                    pedestrianRoot.transform,
                    link.id,
                    link.points_norm,
                    Mathf.Max(0.07f, width),
                    new Color(0.88f, 0.78f, 0.55f, 0.94f),
                    0.075f,
                    -4);
            }
            GeneratedVehicleRoadCount = city.vehicle_roads.Length;
            GeneratedPedestrianLinkCount = city.pedestrian_links.Length;
        }

        private void BuildBuildings()
        {
            GameObject root = ChildRoot("Buildings");
            foreach (CityBuilding building in city.buildings)
            {
                Color wall;
                Color roof;
                float height;
                string shortLabel;
                switch (building.type)
                {
                    case CityBuildingType.Apartment:
                        wall = new Color(0.50f, 0.19f, 0.12f, 1f);
                        roof = new Color(0.25f, 0.08f, 0.055f, 1f);
                        height = 0.34f;
                        shortLabel = "HOUSING";
                        break;
                    case CityBuildingType.DetachedHouse:
                        wall = new Color(0.77f, 0.65f, 0.46f, 1f);
                        roof = new Color(0.37f, 0.17f, 0.10f, 1f);
                        height = 0.22f;
                        shortLabel = "HOME";
                        break;
                    case CityBuildingType.Commercial:
                        wall = new Color(0.72f, 0.45f, 0.14f, 1f);
                        roof = new Color(0.30f, 0.24f, 0.11f, 1f);
                        height = 0.27f;
                        shortLabel = "SHOPS";
                        break;
                    default:
                        wall = new Color(0.15f, 0.39f, 0.36f, 1f);
                        roof = new Color(0.07f, 0.20f, 0.18f, 1f);
                        height = 0.26f;
                        shortLabel = "COMMUNITY";
                        break;
                }
                Vector3 position = ToWorld(building.position_norm, height * 0.5f + 0.09f);
                float width = building.footprint_units[0] / city.bounds.width_units * MapWidth;
                float depth = building.footprint_units[1] / city.bounds.height_units * MapHeight;
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = building.id;
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = position;
                body.transform.localScale = new Vector3(width, height, depth);
                body.transform.localRotation = Quaternion.Euler(0f, -building.rotation_deg, 0f);
                RemoveCollider(body);
                SetMaterial(body, wall);

                GameObject roofObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofObject.name = "Roof";
                roofObject.transform.SetParent(body.transform, false);
                roofObject.transform.localPosition = new Vector3(0f, 0.58f, 0f);
                roofObject.transform.localScale = new Vector3(1.07f, 0.16f, 1.07f);
                RemoveCollider(roofObject);
                SetMaterial(roofObject, roof);
                CreateLabel(root.transform, shortLabel, ToWorld(building.position_norm, height + 0.18f), 0.18f);
            }
            GeneratedBuildingCount = city.buildings.Length;
        }

        private void BuildActors()
        {
            GameObject root = ChildRoot("Representative mobility agents");
            actorViews = new ActorView[plan.trips.Length];
            string[] humanSprites =
            {
                "UrbanWildlife/Humans/walker-topdown-v02",
                "UrbanWildlife/Humans/visitor-topdown-v02",
                "UrbanWildlife/Humans/walker-topdown-v04",
                "UrbanWildlife/Humans/visitor-topdown-v04",
            };
            for (int index = 0; index < plan.trips.Length; index += 1)
            {
                GameObject human = CreateHumanToken(root.transform, index, humanSprites[index % humanSprites.Length]);
                GameObject vehicle = CreateVehicleToken(root.transform, index);
                human.SetActive(false);
                vehicle.SetActive(false);
                actorViews[index] = new ActorView { human = human, vehicle = vehicle };
            }
        }

        private GameObject CreateHumanToken(Transform parent, int index, string spritePath)
        {
            GameObject token = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            token.name = $"Representative human {index + 1:00}";
            token.transform.SetParent(parent, false);
            token.transform.localScale = new Vector3(0.24f, 0.035f, 0.24f);
            RemoveCollider(token);
            SetMaterial(token, new Color(0.70f, 0.47f, 0.24f, 1f));

            Sprite sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                GameObject artwork = new GameObject("Human artwork");
                artwork.transform.SetParent(token.transform, false);
                artwork.transform.localPosition = new Vector3(0f, 1.55f, 0f);
                artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                float scale = 0.55f / Mathf.Max(0.001f, sprite.bounds.size.y);
                artwork.transform.localScale = new Vector3(scale / 0.24f, scale / 0.24f, scale / 0.24f);
                SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = 40;
            }
            return token;
        }

        private GameObject CreateVehicleToken(Transform parent, int index)
        {
            GameObject vehicle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vehicle.name = $"Trip vehicle {index + 1:00}";
            vehicle.transform.SetParent(parent, false);
            vehicle.transform.localScale = new Vector3(0.52f, 0.14f, 0.28f);
            RemoveCollider(vehicle);
            Color[] colours =
            {
                new Color(0.16f, 0.36f, 0.50f, 1f),
                new Color(0.55f, 0.18f, 0.12f, 1f),
                new Color(0.77f, 0.61f, 0.18f, 1f),
            };
            SetMaterial(vehicle, colours[index % colours.Length]);
            return vehicle;
        }

        private void UpdateActors()
        {
            CityTripAgent[] agents = mobility.Agents;
            for (int index = 0; index < agents.Length; index += 1)
            {
                CityTripAgent agent = agents[index];
                ActorView view = actorViews[index];
                bool active = agent.State != CityTripMotionState.Pending &&
                              agent.State != CityTripMotionState.Complete;
                bool travellingByCar = agent.Trip.mode == CityTravelMode.Drive &&
                                       (agent.State == CityTripMotionState.Outbound ||
                                        agent.State == CityTripMotionState.Returning);
                view.human.SetActive(active && !travellingByCar);
                view.vehicle.SetActive(active && agent.Trip.mode == CityTravelMode.Drive);
                if (!active)
                {
                    continue;
                }

                Vector3 position = ToWorld(agent.PositionNorm, 0.18f);
                GameObject movingObject = travellingByCar ? view.vehicle : view.human;
                movingObject.transform.localPosition = position;
                if (agent.Trip.mode == CityTravelMode.Drive && !travellingByCar)
                {
                    view.vehicle.transform.localPosition = position + new Vector3(0.22f, 0f, 0.16f);
                }
                if (view.hasLastPosition)
                {
                    Vector3 delta = position - view.lastPosition;
                    if (delta.sqrMagnitude > 0.00001f)
                    {
                        float heading = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                        movingObject.transform.localRotation = Quaternion.Euler(0f, heading, 0f);
                    }
                }
                view.lastPosition = position;
                view.hasLastPosition = true;
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || plan == null || mobility == null)
            {
                return;
            }
            EnsureStyles();
            float panelWidth = Screen.width * 0.29f;
            GUILayout.BeginArea(new Rect(0f, 0f, panelWidth, Screen.height), panelStyle);
            GUILayout.Space(20f);
            GUILayout.Label("BOROUGH PLANNING OFFICE", headingStyle);
            GUILayout.Label("LIVE CITY PROTOTYPE", titleStyle);
            GUILayout.Label("CITY MOBILITY CHECK · V0.1", bodyStyle);
            GUILayout.Space(18f);

            GUILayout.Label(paused ? "PAUSED" : "LIVE  ·  SIMULATION ×4", headingStyle);
            GUILayout.Space(8f);
            GUILayout.Label($"Active residents  {mobility.ActiveHumanAgentCount}/{plan.RepresentativeAgentCount}", metricStyle);
            GUILayout.Label($"Represented people  {plan.RepresentedPopulation}", metricStyle);
            GUILayout.Label($"Walk / Drive  {plan.WalkTripCount} / {plan.DriveTripCount}", metricStyle);
            GUILayout.Label($"Active vehicles  {mobility.ActiveVehicleAgents.Length}", metricStyle);
            GUILayout.Label($"Completed returns  {mobility.CompletedTripCount}", metricStyle);
            GUILayout.Space(16f);

            GUILayout.Label("DESTINATION PRESSURE", headingStyle);
            foreach (CityDestinationLoad load in plan.destination_loads.Where(item =>
                         item.assigned_representative_agents > 0))
            {
                string status = load.crowd_penalty > 0f ? "CROWDED" : "COMFORTABLE";
                GUILayout.Label(
                    $"{load.building_id}\n  {load.assigned_people}/{load.comfortable_capacity} people · {status}",
                    bodyStyle);
            }
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(paused ? "RESUME" : "PAUSE", buttonStyle))
            {
                paused = !paused;
            }
            if (GUILayout.Button("RESTART TRIPS", buttonStyle))
            {
                InitializePrototype(false);
            }
            if (GUILayout.Button(showPedestrianNetwork ? "HIDE FOOTPATHS" : "SHOW FOOTPATHS", buttonStyle))
            {
                showPedestrianNetwork = !showPedestrianNetwork;
                if (pedestrianRoot != null)
                {
                    pedestrianRoot.SetActive(showPedestrianNetwork);
                }
            }
            GUILayout.Space(14f);
            GUILayout.Label(
                "Residents emerge from housing and respond to destination appeal, distance and crowding. Vehicles only appear for Drive trips.",
                bodyStyle);
            GUILayout.Space(18f);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }
            panelStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(24, 24, 10, 18),
                normal = { background = SolidTexture(new Color(0.035f, 0.13f, 0.105f, 0.99f)) },
            };
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.96f, 0.90f, 0.69f, 1f) },
            };
            headingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.80f, 0.66f, 0.30f, 1f) },
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.82f, 0.86f, 0.75f, 1f) },
            };
            metricStyle = new GUIStyle(bodyStyle)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.92f, 0.78f, 1f) },
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 42f,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 5, 5),
            };
        }

        private GameObject ChildRoot(string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(generatedRoot.transform, false);
            return child;
        }

        private void CreateLine(
            Transform parent,
            string name,
            float[][] points,
            float width,
            Color colour,
            float height,
            int sortingOrder)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.startWidth = width;
            line.endWidth = width;
            line.numCornerVertices = 5;
            line.numCapVertices = 5;
            line.startColor = Color.white;
            line.endColor = Color.white;
            line.material = MaterialFor(colour);
            line.sortingOrder = sortingOrder;
            for (int index = 0; index < points.Length; index += 1)
            {
                line.SetPosition(index, ToWorld(points[index], height));
            }
        }

        private void CreatePolygon(
            Transform parent,
            string name,
            float[][] points,
            float height,
            Color colour)
        {
            GameObject polygon = new GameObject(name);
            polygon.transform.SetParent(parent, false);
            Mesh mesh = new Mesh { name = name + " mesh" };
            Vector3[] vertices = points.Select(point => ToWorld(point, height)).ToArray();
            int[] triangles = new int[(points.Length - 2) * 3];
            for (int index = 0; index < points.Length - 2; index += 1)
            {
                triangles[index * 3] = 0;
                triangles[index * 3 + 1] = index + 1;
                triangles[index * 3 + 2] = index + 2;
            }
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            polygon.AddComponent<MeshFilter>().sharedMesh = mesh;
            polygon.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(colour);
        }

        private void CreateLabel(
            Transform parent,
            string text,
            Vector3 position,
            float characterSize)
        {
            GameObject labelObject = new GameObject(text + " label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = position;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = characterSize;
            label.color = new Color(1f, 0.92f, 0.67f, 1f);
        }

        private Vector3 ToWorld(float[] normalized, float height)
        {
            return new Vector3(
                (normalized[0] - 0.5f) * MapWidth,
                height,
                (0.5f - normalized[1]) * MapHeight);
        }

        private static void SetMaterial(GameObject target, Color colour)
        {
            target.GetComponent<Renderer>().sharedMaterial = MaterialFor(colour);
        }

        private static Material MaterialFor(Color colour)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader)
            {
                color = colour,
                hideFlags = HideFlags.DontSave,
            };
            return material;
        }

        private static Texture2D SolidTexture(Color colour)
        {
            Texture2D texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixel(0, 0, colour);
            texture.Apply();
            return texture;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(collider);
                }
                else
                {
                    DestroyImmediate(collider);
                }
            }
        }

        private void ClearGenerated()
        {
            if (generatedRoot == null)
            {
                Transform existing = transform.Find("Generated City Prototype");
                generatedRoot = existing == null ? null : existing.gameObject;
            }
            if (generatedRoot == null)
            {
                return;
            }
            generatedRoot.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(generatedRoot);
            }
            else
            {
                DestroyImmediate(generatedRoot);
            }
            generatedRoot = null;
        }
    }
}

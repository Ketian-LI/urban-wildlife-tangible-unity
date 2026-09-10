using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlife.Input
{
    [RequireComponent(typeof(LayoutPacketReader))]
    public sealed class LayoutDebugView : MonoBehaviour
    {
        private const string ParkMapResourcePath = "UrbanWildlife/Environment/park-board-s001-v02";
        private const string WoodlandGroveResourcePath = "UrbanWildlife/Environment/woodland-forest-grove-v01";
        private const string PlazaResourcePath = "UrbanWildlife/Environment/human-activity-plaza-v01";
        private const string BenchResourcePath = "UrbanWildlife/Environment/human-activity-bench-v01";
        private const string ParkGateResourcePath = "UrbanWildlife/Environment/park-fence-gate-open-v01";

        [SerializeField]
        private Vector2 boardSizeUnits = new Vector2(9f, 6f);

        [SerializeField]
        private float pathWidthUnits = 0.15f;

        private readonly List<Material> generatedMaterials = new List<Material>();
        private readonly List<Mesh> generatedMeshes = new List<Mesh>();
        private LayoutPacketReader reader;
        private Transform generatedRoot;

        public int GeneratedElementCount => generatedRoot == null ? 0 : generatedRoot.childCount;

        private void Awake()
        {
            reader = GetComponent<LayoutPacketReader>();
        }

        private void OnEnable()
        {
            if (reader == null)
            {
                reader = GetComponent<LayoutPacketReader>();
            }

            reader.LayoutAccepted += ApplyPacket;
        }

        private void OnDisable()
        {
            if (reader != null)
            {
                reader.LayoutAccepted -= ApplyPacket;
            }
        }

        public void ApplyPacket(LayoutPacket packet)
        {
            ClearGenerated();
            GameObject root = new GameObject("Runtime Illustrated Layout");
            root.transform.SetParent(transform, false);
            generatedRoot = root.transform;

            CreateBoard();
            CreatePath(packet.path);
            foreach (LayoutToken token in packet.tokens)
            {
                CreateToken(token);
            }
        }

        public Vector3 NormalizedToLocal(float xNorm, float yNorm, float height)
        {
            return new Vector3(
                (xNorm - 0.5f) * boardSizeUnits.x,
                height,
                (0.5f - yNorm) * boardSizeUnits.y);
        }

        private void CreateBoard()
        {
            GameObject boardRoot = new GameObject("Board 90x60cm");
            boardRoot.transform.SetParent(generatedRoot, false);

            GameObject boardBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boardBase.name = "Dark board edge";
            boardBase.transform.SetParent(boardRoot.transform, false);
            boardBase.transform.localPosition = new Vector3(0f, -0.015f, 0f);
            boardBase.transform.localScale = new Vector3(boardSizeUnits.x + 0.16f, 0.08f, boardSizeUnits.y + 0.16f);
            RemoveCollider(boardBase);
            SetColour(boardBase, new Color(0.055f, 0.09f, 0.065f, 1f));

            Sprite mapSprite = Resources.Load<Sprite>(ParkMapResourcePath);
            if (mapSprite != null)
            {
                GameObject mapArtwork = new GameObject("S001 hand-painted park map");
                mapArtwork.transform.SetParent(boardRoot.transform, false);
                mapArtwork.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                mapArtwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                mapArtwork.transform.localScale = new Vector3(
                    boardSizeUnits.x / Mathf.Max(0.001f, mapSprite.bounds.size.x),
                    boardSizeUnits.y / Mathf.Max(0.001f, mapSprite.bounds.size.y),
                    1f);
                SpriteRenderer mapRenderer = mapArtwork.AddComponent<SpriteRenderer>();
                mapRenderer.sprite = mapSprite;
                mapRenderer.sortingOrder = -20;
            }
            else
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.name = "Map artwork fallback";
                fallback.transform.SetParent(boardRoot.transform, false);
                fallback.transform.localPosition = new Vector3(0f, 0.03f, 0f);
                fallback.transform.localScale = new Vector3(boardSizeUnits.x, 0.012f, boardSizeUnits.y);
                RemoveCollider(fallback);
                SetColour(fallback, new Color(0.38f, 0.52f, 0.28f, 1f));
                Debug.LogWarning("Illustrated S001 map is missing; using a green fallback surface.");
            }

            CreateEllipseOutline(
                boardRoot.transform,
                "Central plaza guide",
                NormalizedToLocal(0.5f, 0.3f, 0.07f),
                new Vector2(2f, 1.4f),
                new Color(0.96f, 0.76f, 0.38f, 0.9f));
            CreateEllipseOutline(
                boardRoot.transform,
                "Pond guide",
                NormalizedToLocal(0.556f, 0.7f, 0.07f),
                new Vector2(1.8f, 1.2f),
                new Color(0.38f, 0.78f, 0.78f, 0.9f));
            CreateParkGateGuide(boardRoot.transform, "Entrance A guide", 0.033f, 0.5f, 0.6f, 0.8f, "A", "ENTRY");
            CreateParkGateGuide(boardRoot.transform, "Exit B guide", 0.967f, 0.5f, 0.6f, 0.8f, "B", "EXIT");
        }

        private void CreatePath(LayoutPath path)
        {
            GameObject pathObject = new GameObject("Planned Human Path");
            pathObject.transform.SetParent(generatedRoot, false);

            LineRenderer shoulder = CreatePathLayer(
                pathObject.transform,
                "Road earth shoulder",
                path.points_norm.Length,
                pathWidthUnits + 0.23f,
                new Color(0.24f, 0.17f, 0.1f, 0.9f),
                10);
            LineRenderer edging = CreatePathLayer(
                pathObject.transform,
                "Road stone edging",
                path.points_norm.Length,
                pathWidthUnits + 0.17f,
                new Color(0.88f, 0.8f, 0.65f, 1f),
                11);
            LineRenderer surface = CreatePathLayer(
                pathObject.transform,
                "Road gravel surface",
                path.points_norm.Length,
                pathWidthUnits + 0.11f,
                new Color(0.66f, 0.57f, 0.45f, 1f),
                12);
            Vector3[] positions = new Vector3[path.points_norm.Length];
            for (int index = 0; index < path.points_norm.Length; index += 1)
            {
                Vector3 position = NormalizedToLocal(
                    path.points_norm[index][0],
                    path.points_norm[index][1],
                    0.095f);
                positions[index] = position;
                shoulder.SetPosition(index, position);
                position.y = 0.102f;
                edging.SetPosition(index, position);
                position.y = 0.109f;
                surface.SetPosition(index, position);
            }

            CreateRoadDetails(pathObject.transform, positions);
        }

        private LineRenderer CreatePathLayer(
            Transform parent,
            string name,
            int pointCount,
            float width,
            Color colour,
            int sortingOrder)
        {
            GameObject layer = new GameObject(name);
            layer.transform.SetParent(parent, false);
            return ConfigurePathLine(layer, pointCount, width, colour, sortingOrder);
        }

        private void CreateRoadDetails(Transform parent, Vector3[] positions)
        {
            GameObject detailRoot = new GameObject("Road gravel details");
            detailRoot.transform.SetParent(parent, false);
            int detailIndex = 0;

            for (int segment = 0; segment < positions.Length - 1; segment += 1)
            {
                Vector3 start = positions[segment];
                Vector3 end = positions[segment + 1];
                Vector3 delta = end - start;
                float segmentLength = new Vector2(delta.x, delta.z).magnitude;
                int sampleCount = Mathf.FloorToInt(segmentLength / 0.34f);
                if (sampleCount < 1)
                {
                    continue;
                }

                Vector3 perpendicular = new Vector3(-delta.z, 0f, delta.x).normalized;
                float heading = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                for (int sample = 1; sample <= sampleCount; sample += 1)
                {
                    float t = sample / (sampleCount + 1f);
                    Vector3 position = Vector3.Lerp(start, end, t);
                    float side = detailIndex % 2 == 0 ? 1f : -1f;
                    position += perpendicular * side * (0.035f + (detailIndex % 3) * 0.012f);

                    Transform pebble = CreateAnchor(
                        detailRoot.transform,
                        $"Gravel pebble {detailIndex}",
                        new Vector2(position.x, position.z));
                    pebble.localRotation = Quaternion.Euler(0f, heading + (detailIndex % 3 - 1) * 17f, 0f);
                    float lengthScale = 0.82f + (detailIndex % 4) * 0.12f;
                    pebble.localScale = new Vector3(lengthScale, 1f, 0.58f);
                    Color colour = detailIndex % 3 == 0
                        ? new Color(0.46f, 0.39f, 0.31f, 0.72f)
                        : new Color(0.91f, 0.84f, 0.71f, 0.62f);
                    CreatePolygon(
                        pebble,
                        "Pebble",
                        RegularPolygon(8, 0.026f, 22.5f),
                        0.124f,
                        colour,
                        14);
                    detailIndex += 1;
                }
            }
        }

        private LineRenderer ConfigurePathLine(
            GameObject target,
            int pointCount,
            float width,
            Color colour,
            int sortingOrder)
        {
            LineRenderer line = target.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = width;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            line.positionCount = pointCount;
            line.material = CreateOverlayMaterial(colour);
            line.sortingOrder = sortingOrder;
            return line;
        }

        private void CreateToken(LayoutToken token)
        {
            GameObject tokenRoot = new GameObject($"{token.type} {token.id}");
            tokenRoot.transform.SetParent(generatedRoot, false);
            tokenRoot.transform.localPosition = NormalizedToLocal(token.x_norm, token.y_norm, 0.125f);
            tokenRoot.transform.localRotation = Quaternion.Euler(0f, token.angle_deg, 0f);

            if (token.type == "woodland")
            {
                CreateWoodlandToken(tokenRoot.transform, token.id);
            }
            else
            {
                CreateFoodToken(tokenRoot.transform, token.id);
            }
        }

        private void CreateFoodToken(Transform parent, int id)
        {
            bool useBench = id % 2 == 0;
            string sceneName = useBench ? "Bench rest area artwork" : "Plaza activity area artwork";
            string resourcePath = useBench ? BenchResourcePath : PlazaResourcePath;
            float displayWidth = useBench ? 1.12f : 1.08f;
            if (CreateEnvironmentSprite(parent, sceneName, resourcePath, displayWidth, 0.018f, 20))
            {
                CreateTokenIdBadge(
                    parent,
                    id,
                    new Vector2(0f, -0.48f),
                    new Color(0.96f, 0.8f, 0.48f, 0.97f),
                    new Color(0.25f, 0.13f, 0.045f, 1f));
                return;
            }

            Vector2[] outer = RegularPolygon(6, 0.4f, 30f);
            Vector2[] rim = RegularPolygon(6, 0.345f, 30f);
            Vector2[] inner = RegularPolygon(6, 0.292f, 30f);
            CreatePolygon(parent, "Food hotspot shadow", outer, 0f, new Color(0.22f, 0.12f, 0.05f, 0.92f), 18);
            CreatePolygon(parent, "Food hotspot cream rim", rim, 0.012f, new Color(1f, 0.86f, 0.53f, 1f), 19);
            CreatePolygon(parent, "Food hotspot amber face", inner, 0.024f, new Color(0.95f, 0.55f, 0.13f, 1f), 20);
            CreateOutline(parent, "Food hotspot outline", outer, 0.038f, 0.024f, new Color(1f, 0.92f, 0.67f, 1f), 21);

            for (int index = 0; index < 6; index += 1)
            {
                float angle = (30f + index * 60f) * Mathf.Deg2Rad;
                Transform dot = CreateAnchor(
                    parent,
                    $"Activity dot {index}",
                    new Vector2(Mathf.Cos(angle) * 0.235f, Mathf.Sin(angle) * 0.235f));
                Color dotColour = index % 2 == 0
                    ? new Color(1f, 0.9f, 0.56f, 1f)
                    : new Color(0.45f, 0.22f, 0.07f, 1f);
                CreatePolygon(dot, "Activity indicator", RegularPolygon(12, 0.032f, 15f), 0.043f, dotColour, 22);
            }

            CreatePolygon(
                parent,
                "Food hotspot centre badge",
                RegularPolygon(18, 0.17f, 0f),
                0.048f,
                new Color(1f, 0.91f, 0.67f, 1f),
                23);
            CreateOutline(
                parent,
                "Food hotspot centre ring",
                RegularPolygon(18, 0.178f, 0f),
                0.054f,
                0.014f,
                new Color(0.43f, 0.22f, 0.07f, 1f),
                24);
            CreateMediumLabel(parent, id.ToString(), 0.068f, new Color(0.2f, 0.1f, 0.035f, 1f));
        }

        private void CreateWoodlandToken(Transform parent, int id)
        {
            if (CreateEnvironmentSprite(
                    parent,
                    "Woodland forest grove artwork",
                    WoodlandGroveResourcePath,
                    1.55f,
                    0.018f,
                    20))
            {
                CreateTokenIdBadge(
                    parent,
                    id,
                    new Vector2(0f, -0.56f),
                    new Color(0.78f, 0.86f, 0.54f, 0.97f),
                    new Color(0.09f, 0.2f, 0.09f, 1f));
                return;
            }

            Vector2[] leaf =
            {
                new Vector2(0f, 0.48f),
                new Vector2(0.28f, 0.39f),
                new Vector2(0.52f, 0.15f),
                new Vector2(0.57f, -0.08f),
                new Vector2(0.36f, -0.34f),
                new Vector2(0f, -0.45f),
                new Vector2(-0.36f, -0.34f),
                new Vector2(-0.57f, -0.08f),
                new Vector2(-0.52f, 0.15f),
                new Vector2(-0.28f, 0.39f),
            };
            CreatePolygon(parent, "Woodland felt shadow", leaf, 0f, new Color(0.08f, 0.2f, 0.12f, 0.94f), 18);
            CreatePolygon(parent, "Woodland felt inner", ScalePolygon(leaf, 0.88f), 0.012f, new Color(0.25f, 0.48f, 0.25f, 0.98f), 19);
            CreateOutline(parent, "Woodland felt outline", leaf, 0.025f, 0.03f, new Color(0.68f, 0.81f, 0.4f, 1f), 20);

            Vector2[] canopyOffsets =
            {
                new Vector2(-0.27f, 0.08f),
                new Vector2(0.2f, 0.24f),
                new Vector2(0.27f, -0.2f),
            };
            Color[] canopyColours =
            {
                new Color(0.32f, 0.58f, 0.27f, 0.95f),
                new Color(0.4f, 0.64f, 0.3f, 0.95f),
                new Color(0.27f, 0.52f, 0.22f, 0.95f),
            };
            for (int index = 0; index < canopyOffsets.Length; index += 1)
            {
                Transform canopy = CreateAnchor(parent, $"Woodland canopy patch {index + 1}", canopyOffsets[index]);
                CreatePolygon(
                    canopy,
                    "Canopy rosette",
                    RegularPolygon(12, 0.105f + index * 0.012f, index * 9f),
                    0.03f,
                    canopyColours[index],
                    20);
            }

            Color veinColour = new Color(0.62f, 0.77f, 0.36f, 0.92f);
            CreateOpenLine(
                parent,
                "Woodland central vein",
                new[] { new Vector2(0f, -0.35f), new Vector2(0f, 0.34f) },
                0.035f,
                0.018f,
                veinColour,
                21);
            CreateOpenLine(
                parent,
                "Woodland left veins",
                new[] { new Vector2(0f, -0.15f), new Vector2(-0.3f, 0.08f), new Vector2(0f, 0.02f), new Vector2(-0.25f, 0.25f) },
                0.035f,
                0.014f,
                veinColour,
                21);
            CreateOpenLine(
                parent,
                "Woodland right veins",
                new[] { new Vector2(0f, -0.04f), new Vector2(0.3f, 0.15f), new Vector2(0f, 0.13f), new Vector2(0.2f, 0.31f) },
                0.035f,
                0.014f,
                veinColour,
                21);

            CreatePolygon(parent, "Woodland wooden core bark", RegularPolygon(24, 0.27f, 7.5f), 0.045f, new Color(0.33f, 0.19f, 0.09f, 1f), 22);
            CreatePolygon(parent, "Woodland wooden core", RegularPolygon(24, 0.225f, 7.5f), 0.052f, new Color(0.72f, 0.5f, 0.27f, 1f), 23);
            CreatePolygon(parent, "Woodland wooden heart", RegularPolygon(24, 0.15f, 7.5f), 0.058f, new Color(0.86f, 0.67f, 0.4f, 1f), 24);
            CreateOutline(parent, "Woodland growth ring", RegularPolygon(24, 0.185f, 7.5f), 0.064f, 0.012f, new Color(0.47f, 0.29f, 0.12f, 0.9f), 25);
            CreateLabel(parent, id.ToString(), 0.075f, new Color(0.16f, 0.09f, 0.035f, 1f));
        }

        private static bool CreateEnvironmentSprite(
            Transform parent,
            string name,
            string resourcePath,
            float displayWidth,
            float height,
            int sortingOrder,
            bool mirrorX = false)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                Debug.LogWarning($"Planning-area sprite missing: {resourcePath}; using the procedural fallback.");
                return false;
            }

            GameObject artwork = new GameObject(name);
            artwork.transform.SetParent(parent, false);
            artwork.transform.localPosition = new Vector3(0f, height, 0f);
            artwork.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float uniformScale = displayWidth / Mathf.Max(0.001f, sprite.bounds.size.x);
            artwork.transform.localScale = new Vector3(
                mirrorX ? -uniformScale : uniformScale,
                uniformScale,
                uniformScale);
            SpriteRenderer renderer = artwork.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return true;
        }

        private void CreateTokenIdBadge(
            Transform parent,
            int id,
            Vector2 offset,
            Color fill,
            Color ink)
        {
            Transform badge = CreateAnchor(parent, $"Research ID {id}", offset);
            Vector2[] disc = RegularPolygon(20, 0.105f, 0f);
            CreatePolygon(badge, "ID badge fill", disc, 0.052f, fill, 27);
            CreateOutline(badge, "ID badge outline", disc, 0.058f, 0.014f, ink, 28);
            CreateSmallLabel(badge, id.ToString(), 0.068f, ink);
        }

        private void CreateParkGateGuide(
            Transform parent,
            string name,
            float xNorm,
            float yNorm,
            float width,
            float height,
            string label,
            string caption)
        {
            GameObject guide = new GameObject(name);
            guide.transform.SetParent(parent, false);
            guide.transform.localPosition = NormalizedToLocal(xNorm, yNorm, 0.07f);
            float inward = xNorm < 0.5f ? 1f : -1f;
            if (CreateEnvironmentSprite(
                    guide.transform,
                    "Open wrought-iron fence gate",
                    ParkGateResourcePath,
                    1.12f,
                    0.012f,
                    26,
                    inward < 0f))
            {
                Transform gateBadge = CreateAnchor(
                    guide.transform,
                    $"Gate ID {label}",
                    new Vector2(inward * 0.36f, 0f));
                Vector2[] badgeDisc = RegularPolygon(20, 0.095f, 0f);
                Color badgeInk = new Color(0.16f, 0.1f, 0.045f, 1f);
                CreatePolygon(
                    gateBadge,
                    "Gate ID badge fill",
                    badgeDisc,
                    0.052f,
                    new Color(0.96f, 0.82f, 0.55f, 0.97f),
                    27);
                CreateOutline(gateBadge, "Gate ID badge outline", badgeDisc, 0.058f, 0.014f, badgeInk, 28);
                CreateSmallLabel(gateBadge, label, 0.068f, badgeInk);
                return;
            }

            Vector2[] points =
            {
                new Vector2(-width * 0.5f, -height * 0.5f),
                new Vector2(width * 0.5f, -height * 0.5f),
                new Vector2(width * 0.5f, height * 0.5f),
                new Vector2(-width * 0.5f, height * 0.5f),
            };
            CreatePolygon(guide.transform, "Gate landing", points, 0f, new Color(0.96f, 0.82f, 0.53f, 0.38f), 4);
            CreateOutline(guide.transform, "Boundary", points, 0.01f, 0.018f, new Color(1f, 0.91f, 0.65f, 0.92f), 5);

            Vector2[] postOuter =
            {
                new Vector2(-0.085f, -0.105f),
                new Vector2(0.085f, -0.105f),
                new Vector2(0.085f, 0.105f),
                new Vector2(-0.085f, 0.105f),
            };
            Vector2[] postInner = ScalePolygon(postOuter, 0.68f);
            for (int index = 0; index < 2; index += 1)
            {
                float z = index == 0 ? -height * 0.36f : height * 0.36f;
                Transform post = CreateAnchor(guide.transform, $"Gate post {index + 1}", new Vector2(-inward * width * 0.12f, z));
                CreatePolygon(post, "Stone base", postOuter, 0.022f, new Color(0.25f, 0.18f, 0.1f, 1f), 26);
                CreatePolygon(post, "Warm stone cap", postInner, 0.03f, new Color(0.94f, 0.74f, 0.38f, 1f), 27);
            }

            const int curvePoints = 11;
            Vector2[] arch = new Vector2[curvePoints];
            for (int index = 0; index < curvePoints; index += 1)
            {
                float t = index / (curvePoints - 1f);
                arch[index] = new Vector2(
                    inward * (0.02f + Mathf.Sin(t * Mathf.PI) * 0.13f),
                    Mathf.Lerp(-height * 0.31f, height * 0.31f, t));
            }
            CreateOpenLine(guide.transform, "Park gate arch", arch, 0.038f, 0.035f, new Color(0.35f, 0.2f, 0.08f, 1f), 28);

            Transform labelAnchor = CreateAnchor(guide.transform, "Gate label anchor", new Vector2(inward * 0.23f, height * 0.05f));
            CreateMediumLabel(labelAnchor, label, 0.05f, new Color(0.2f, 0.12f, 0.055f, 1f));
            Transform captionAnchor = CreateAnchor(guide.transform, "Gate caption anchor", new Vector2(inward * 0.2f, -height * 0.28f));
            CreateSmallLabel(captionAnchor, caption, 0.052f, new Color(0.28f, 0.17f, 0.07f, 1f));
        }

        private void CreateEllipseOutline(
            Transform parent,
            string name,
            Vector3 position,
            Vector2 size,
            Color colour)
        {
            const int pointCount = 48;
            Vector2[] outline = new Vector2[pointCount];
            for (int index = 0; index < pointCount; index += 1)
            {
                float angle = index * Mathf.PI * 2f / pointCount;
                outline[index] = new Vector2(
                    Mathf.Cos(angle) * size.x * 0.5f,
                    Mathf.Sin(angle) * size.y * 0.5f);
            }

            GameObject guide = new GameObject(name);
            guide.transform.SetParent(parent, false);
            guide.transform.localPosition = position;
            CreateOutline(guide.transform, "Boundary", outline, 0f, 0.03f, colour, 5);
        }

        private static Transform CreateAnchor(Transform parent, string name, Vector2 offset)
        {
            GameObject anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(offset.x, 0f, offset.y);
            return anchor.transform;
        }

        private void CreateOpenLine(
            Transform parent,
            string name,
            Vector2[] points,
            float height,
            float width,
            Color colour,
            int sortingOrder)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = width;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.positionCount = points.Length;
            line.material = CreateOverlayMaterial(colour);
            line.sortingOrder = sortingOrder;
            for (int index = 0; index < points.Length; index += 1)
            {
                line.SetPosition(index, new Vector3(points[index].x, height, points[index].y));
            }
        }

        private void CreatePolygon(
            Transform parent,
            string name,
            Vector2[] outline,
            float height,
            Color colour,
            int sortingOrder)
        {
            GameObject polygon = new GameObject(name);
            polygon.transform.SetParent(parent, false);
            polygon.transform.localPosition = new Vector3(0f, height, 0f);
            MeshFilter filter = polygon.AddComponent<MeshFilter>();
            MeshRenderer renderer = polygon.AddComponent<MeshRenderer>();
            filter.sharedMesh = CreatePolygonMesh(outline);
            renderer.sharedMaterial = CreateOverlayMaterial(colour);
            renderer.sortingOrder = sortingOrder;
        }

        private void CreateOutline(
            Transform parent,
            string name,
            Vector2[] outline,
            float height,
            float width,
            Color colour,
            int sortingOrder)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = width;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.positionCount = outline.Length;
            line.material = CreateOverlayMaterial(colour);
            line.sortingOrder = sortingOrder;
            for (int index = 0; index < outline.Length; index += 1)
            {
                line.SetPosition(index, new Vector3(outline[index].x, height, outline[index].y));
            }
        }

        private void CreateLabel(Transform parent, string value, float height, Color colour)
        {
            GameObject labelObject = new GameObject($"Label {value}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, height, 0f);
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = 0.052f;
            label.color = colour;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 30;
            }
        }

        private void CreateSmallLabel(Transform parent, string value, float height, Color colour)
        {
            GameObject labelObject = new GameObject($"Label {value}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, height, 0f);
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = 0.018f;
            label.fontStyle = FontStyle.Bold;
            label.color = colour;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 30;
            }
        }

        private void CreateMediumLabel(Transform parent, string value, float height, Color colour)
        {
            GameObject labelObject = new GameObject($"Label {value}");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, height, 0f);
            labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = value;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 64;
            label.characterSize = 0.038f;
            label.fontStyle = FontStyle.Bold;
            label.color = colour;
            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 30;
            }
        }

        private Mesh CreatePolygonMesh(Vector2[] outline)
        {
            Vector3[] vertices = new Vector3[outline.Length + 1];
            vertices[0] = Vector3.zero;
            for (int index = 0; index < outline.Length; index += 1)
            {
                vertices[index + 1] = new Vector3(outline[index].x, 0f, outline[index].y);
            }

            int[] triangles = new int[outline.Length * 3];
            for (int index = 0; index < outline.Length; index += 1)
            {
                int next = ((index + 1) % outline.Length) + 1;
                triangles[index * 3] = 0;
                triangles[index * 3 + 1] = next;
                triangles[index * 3 + 2] = index + 1;
            }

            Mesh mesh = new Mesh
            {
                name = "Runtime planning marker mesh",
                vertices = vertices,
                triangles = triangles,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);
            return mesh;
        }

        private static Vector2[] RegularPolygon(int sides, float radius, float rotationDegrees)
        {
            Vector2[] points = new Vector2[sides];
            float rotation = rotationDegrees * Mathf.Deg2Rad;
            for (int index = 0; index < sides; index += 1)
            {
                float angle = rotation + index * Mathf.PI * 2f / sides;
                points[index] = new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
            }
            return points;
        }

        private static Vector2[] ScalePolygon(Vector2[] source, float scale)
        {
            Vector2[] scaled = new Vector2[source.Length];
            for (int index = 0; index < source.Length; index += 1)
            {
                scaled[index] = source[index] * scale;
            }
            return scaled;
        }

        private void ClearGenerated()
        {
            if (generatedRoot == null)
            {
                Transform existing = transform.Find("Runtime Illustrated Layout");
                if (existing != null)
                {
                    generatedRoot = existing;
                }
            }

            if (generatedRoot != null)
            {
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

            foreach (Material material in generatedMaterials)
            {
                DestroyGeneratedObject(material);
            }
            generatedMaterials.Clear();

            foreach (Mesh mesh in generatedMeshes)
            {
                DestroyGeneratedObject(mesh);
            }
            generatedMeshes.Clear();
        }

        private static void DestroyGeneratedObject(Object target)
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

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyGeneratedObject(collider);
            }
        }

        private void SetColour(GameObject target, Color colour)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateSurfaceMaterial(colour);
        }

        private Material CreateSurfaceMaterial(Color colour)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.color = colour;
            generatedMaterials.Add(material);
            return material;
        }

        private Material CreateOverlayMaterial(Color colour)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
            Material material = new Material(shader);
            material.color = colour;
            generatedMaterials.Add(material);
            return material;
        }
    }
}

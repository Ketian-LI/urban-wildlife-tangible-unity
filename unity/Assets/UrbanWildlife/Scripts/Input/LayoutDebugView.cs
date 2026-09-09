using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlife.Input
{
    [RequireComponent(typeof(LayoutPacketReader))]
    public sealed class LayoutDebugView : MonoBehaviour
    {
        private const string ParkMapResourcePath = "UrbanWildlife/Environment/park-board-s001-v02";

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

            GameObject shadowObject = new GameObject("Path outline");
            shadowObject.transform.SetParent(pathObject.transform, false);
            LineRenderer shadow = ConfigurePathLine(
                shadowObject,
                path.points_norm.Length,
                pathWidthUnits + 0.1f,
                new Color(0.22f, 0.08f, 0.2f, 0.82f),
                10);
            LineRenderer line = ConfigurePathLine(
                pathObject,
                path.points_norm.Length,
                pathWidthUnits,
                new Color(0.95f, 0.2f, 0.67f, 1f),
                11);

            for (int index = 0; index < path.points_norm.Length; index += 1)
            {
                Vector3 shadowPosition = NormalizedToLocal(
                    path.points_norm[index][0],
                    path.points_norm[index][1],
                    0.095f);
                Vector3 linePosition = shadowPosition;
                linePosition.y = 0.11f;
                shadow.SetPosition(index, shadowPosition);
                line.SetPosition(index, linePosition);
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
            Vector2[] points =
            {
                new Vector2(-width * 0.5f, -height * 0.5f),
                new Vector2(width * 0.5f, -height * 0.5f),
                new Vector2(width * 0.5f, height * 0.5f),
                new Vector2(-width * 0.5f, height * 0.5f),
            };
            GameObject guide = new GameObject(name);
            guide.transform.SetParent(parent, false);
            guide.transform.localPosition = NormalizedToLocal(xNorm, yNorm, 0.07f);
            CreatePolygon(guide.transform, "Gate landing", points, 0f, new Color(0.96f, 0.82f, 0.53f, 0.38f), 4);
            CreateOutline(guide.transform, "Boundary", points, 0.01f, 0.018f, new Color(1f, 0.91f, 0.65f, 0.92f), 5);

            float inward = xNorm < 0.5f ? 1f : -1f;
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

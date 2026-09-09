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
            CreateRectangleGuide(boardRoot.transform, "Entrance A guide", 0.033f, 0.5f, 0.6f, 0.8f, "A");
            CreateRectangleGuide(boardRoot.transform, "Exit B guide", 0.967f, 0.5f, 0.6f, 0.8f, "B");
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
            Vector2[] outer = RegularPolygon(6, 0.37f, 30f);
            Vector2[] inner = RegularPolygon(6, 0.3f, 30f);
            CreatePolygon(parent, "Food hotspot shadow", outer, 0f, new Color(0.34f, 0.2f, 0.08f, 0.86f), 18);
            CreatePolygon(parent, "Food hotspot", inner, 0.018f, new Color(0.96f, 0.69f, 0.28f, 1f), 19);
            CreateOutline(parent, "Food hotspot outline", outer, 0.034f, 0.025f, new Color(1f, 0.9f, 0.62f, 1f), 20);
            CreateLabel(parent, id.ToString(), 0.045f, new Color(0.23f, 0.13f, 0.06f, 1f));
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
            CreatePolygon(parent, "Woodland felt", leaf, 0f, new Color(0.2f, 0.38f, 0.22f, 0.97f), 18);
            CreateOutline(parent, "Woodland felt outline", leaf, 0.02f, 0.028f, new Color(0.54f, 0.7f, 0.36f, 1f), 19);

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "Woodland wooden core";
            core.transform.SetParent(parent, false);
            core.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            core.transform.localScale = new Vector3(0.5f, 0.018f, 0.5f);
            RemoveCollider(core);
            SetColour(core, new Color(0.68f, 0.48f, 0.28f, 1f));
            CreateLabel(parent, id.ToString(), 0.065f, new Color(0.18f, 0.12f, 0.06f, 1f));
        }

        private void CreateRectangleGuide(
            Transform parent,
            string name,
            float xNorm,
            float yNorm,
            float width,
            float height,
            string label)
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
            CreateOutline(guide.transform, "Boundary", points, 0f, 0.035f, new Color(1f, 0.9f, 0.58f, 0.95f), 5);
            CreateLabel(guide.transform, label, 0.018f, new Color(0.24f, 0.2f, 0.1f, 1f));
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
                renderer.sortingOrder = 25;
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

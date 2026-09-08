using UnityEngine;

namespace UrbanWildlife.Input
{
    [RequireComponent(typeof(LayoutPacketReader))]
    public sealed class LayoutDebugView : MonoBehaviour
    {
        [SerializeField]
        private Vector2 boardSizeUnits = new Vector2(9f, 6f);

        [SerializeField]
        private float pathWidthUnits = 0.15f;

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
            GameObject root = new GameObject("Runtime Layout Debug");
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
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Board 90x60cm";
            board.transform.SetParent(generatedRoot, false);
            board.transform.localPosition = Vector3.zero;
            board.transform.localScale = new Vector3(boardSizeUnits.x, 0.08f, boardSizeUnits.y);
            RemoveCollider(board);
            SetColour(board, new Color(0.045f, 0.045f, 0.045f, 1f));
        }

        private void CreatePath(LayoutPath path)
        {
            GameObject pathObject = new GameObject("Planned Human Path");
            pathObject.transform.SetParent(generatedRoot, false);
            LineRenderer line = pathObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.widthMultiplier = pathWidthUnits;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.positionCount = path.points_norm.Length;
            line.material = CreateMaterial(new Color(0.95f, 0.02f, 0.72f, 1f));
            for (int index = 0; index < path.points_norm.Length; index += 1)
            {
                line.SetPosition(index, NormalizedToLocal(path.points_norm[index][0], path.points_norm[index][1], 0.07f));
            }
        }

        private void CreateToken(LayoutToken token)
        {
            Vector3 position = NormalizedToLocal(token.x_norm, token.y_norm, 0.1f);
            if (token.type == "woodland")
            {
                GameObject felt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                felt.name = $"Woodland Felt {token.id}";
                felt.transform.SetParent(generatedRoot, false);
                felt.transform.localPosition = new Vector3(position.x, 0.065f, position.z);
                felt.transform.localScale = new Vector3(1.2f, 0.015f, 0.9f);
                felt.transform.localRotation = Quaternion.Euler(0f, token.angle_deg, 0f);
                RemoveCollider(felt);
                SetColour(felt, new Color(0.22f, 0.32f, 0.2f, 1f));
            }

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = $"{token.type} {token.id}";
            core.transform.SetParent(generatedRoot, false);
            core.transform.localPosition = position;
            float diameter = token.type == "food_hotspot" ? 0.6f : 0.5f;
            core.transform.localScale = new Vector3(diameter, 0.04f, diameter);
            core.transform.localRotation = Quaternion.Euler(0f, token.angle_deg, 0f);
            RemoveCollider(core);
            SetColour(core, new Color(0.82f, 0.68f, 0.48f, 1f));
        }

        private void ClearGenerated()
        {
            if (generatedRoot == null)
            {
                Transform existing = transform.Find("Runtime Layout Debug");
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

        private static void SetColour(GameObject target, Color colour)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            renderer.sharedMaterial = CreateMaterial(colour);
        }

        private static Material CreateMaterial(Color colour)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            material.color = colour;
            return material;
        }
    }
}

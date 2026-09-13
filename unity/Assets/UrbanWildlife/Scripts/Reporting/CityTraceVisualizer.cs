using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UrbanWildlife.Reporting
{
    public sealed class CityTraceVisualizer
    {
        public const int MaximumVisibleMarks = 900;

        private readonly float mapWidth;
        private readonly float mapHeight;
        private readonly Transform humanRoot;
        private readonly Transform animalRoot;
        private readonly Dictionary<string, Vector2> lastPositions =
            new Dictionary<string, Vector2>();
        private readonly Dictionary<CityTraceMark, Material> materials =
            new Dictionary<CityTraceMark, Material>();
        private readonly Queue<GameObject> visibleMarks = new Queue<GameObject>();
        private int renderedPointCount;

        public CityTraceVisualizer(Transform parent, float mapWidth, float mapHeight)
        {
            if (parent == null || mapWidth <= 0f || mapHeight <= 0f)
            {
                throw new ArgumentException("A trace parent and positive map size are required.");
            }
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            GameObject root = new GameObject("Live city traces");
            root.transform.SetParent(parent, false);
            humanRoot = Child(root.transform, "Human traces");
            animalRoot = Child(root.transform, "Animal traces");
            SetMode(CityTraceDisplayMode.CombinedTrace);
        }

        public int VisibleMarkCount { get; private set; }
        public CityTraceDisplayMode DisplayMode { get; private set; }

        public void Render(IEnumerable<CityTracePoint> source)
        {
            CityTracePoint[] points = (source ?? Array.Empty<CityTracePoint>()).ToArray();
            if (points.Length < renderedPointCount)
            {
                ClearVisibleMarks();
                renderedPointCount = 0;
                lastPositions.Clear();
            }
            int startIndex = renderedPointCount == 0
                ? Math.Max(0, points.Length - MaximumVisibleMarks)
                : renderedPointCount;
            for (int index = startIndex; index < points.Length; index += 1)
            {
                CityTracePoint point = points[index];
                if (point?.position_norm == null || point.position_norm.Length != 2)
                {
                    continue;
                }
                GameObject mark = Draw(point, index);
                visibleMarks.Enqueue(mark);
                while (visibleMarks.Count > MaximumVisibleMarks)
                {
                    GameObject oldest = visibleMarks.Dequeue();
                    oldest.SetActive(false);
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(oldest);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(oldest);
                    }
                }
            }
            renderedPointCount = points.Length;
            VisibleMarkCount = visibleMarks.Count;
        }

        private void ClearVisibleMarks()
        {
            while (visibleMarks.Count > 0)
            {
                GameObject mark = visibleMarks.Dequeue();
                mark.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(mark);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(mark);
                }
            }
            VisibleMarkCount = 0;
        }

        public void SetMode(CityTraceDisplayMode mode)
        {
            DisplayMode = mode;
            humanRoot.gameObject.SetActive(mode != CityTraceDisplayMode.AnimalTrace);
            animalRoot.gameObject.SetActive(mode != CityTraceDisplayMode.HumanTrace);
        }

        private GameObject Draw(CityTracePoint point, int index)
        {
            Transform layer = point.layer == CityTraceLayer.Human ? humanRoot : animalRoot;
            GameObject mark = new GameObject($"{point.mark} {index:0000}");
            mark.transform.SetParent(layer, false);
            Vector2 current = new Vector2(point.position_norm[0], point.position_norm[1]);
            Vector2 direction = Vector2.up;
            if (lastPositions.TryGetValue(point.agent_id, out Vector2 previous) &&
                Vector2.Distance(previous, current) > 0.0001f)
            {
                direction = (current - previous).normalized;
            }
            lastPositions[point.agent_id] = current;
            mark.transform.localPosition = ToWorld(current, 0.115f);
            float heading = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            mark.transform.localRotation = Quaternion.Euler(0f, heading, 0f);

            switch (point.mark)
            {
                case CityTraceMark.Footprint:
                    DrawFootprints(mark.transform, index);
                    break;
                case CityTraceMark.VehicleTyre:
                    DrawTyres(mark.transform);
                    break;
                case CityTraceMark.BirdTrack:
                    DrawBirdTrack(mark.transform, index);
                    break;
                case CityTraceMark.SmallPaw:
                    DrawPaws(mark.transform, 0.034f, index, point.mark);
                    break;
                case CityTraceMark.FoxPaw:
                    DrawPaws(mark.transform, 0.050f, index, point.mark);
                    break;
                default:
                    DrawHedgehogTrack(mark.transform, index);
                    break;
            }
            return mark;
        }

        private void DrawFootprints(Transform root, int index)
        {
            float alternating = index % 2 == 0 ? 1f : -1f;
            CreateDisc(root, "Left shoe", new Vector3(-0.035f, 0f, -0.025f * alternating),
                new Vector3(0.028f, 0.006f, 0.060f), CityTraceMark.Footprint);
            CreateDisc(root, "Right shoe", new Vector3(0.035f, 0f, 0.025f * alternating),
                new Vector3(0.028f, 0.006f, 0.060f), CityTraceMark.Footprint);
        }

        private void DrawTyres(Transform root)
        {
            CreateBar(root, "Left tyre", new Vector3(-0.055f, 0f, 0f),
                new Vector3(0.018f, 0.008f, 0.105f), CityTraceMark.VehicleTyre, 0f);
            CreateBar(root, "Right tyre", new Vector3(0.055f, 0f, 0f),
                new Vector3(0.018f, 0.008f, 0.105f), CityTraceMark.VehicleTyre, 0f);
        }

        private void DrawBirdTrack(Transform root, int index)
        {
            float side = index % 2 == 0 ? -0.025f : 0.025f;
            CreateBar(root, "Bird toe centre", new Vector3(side, 0f, 0f),
                new Vector3(0.010f, 0.006f, 0.050f), CityTraceMark.BirdTrack, 0f);
            CreateBar(root, "Bird toe left", new Vector3(side - 0.014f, 0f, 0.005f),
                new Vector3(0.009f, 0.006f, 0.040f), CityTraceMark.BirdTrack, -28f);
            CreateBar(root, "Bird toe right", new Vector3(side + 0.014f, 0f, 0.005f),
                new Vector3(0.009f, 0.006f, 0.040f), CityTraceMark.BirdTrack, 28f);
        }

        private void DrawPaws(
            Transform root,
            float size,
            int index,
            CityTraceMark mark)
        {
            float side = index % 2 == 0 ? -size * 0.65f : size * 0.65f;
            CreateDisc(root, "Paw pad", new Vector3(side, 0f, 0f),
                new Vector3(size, 0.006f, size * 1.15f), mark);
            for (int toe = -1; toe <= 1; toe += 1)
            {
                CreateDisc(root, $"Toe {toe + 2}",
                    new Vector3(side + toe * size * 0.42f, 0f, size * 0.75f),
                    new Vector3(size * 0.30f, 0.005f, size * 0.34f), mark);
            }
        }

        private void DrawHedgehogTrack(Transform root, int index)
        {
            float side = index % 2 == 0 ? -0.025f : 0.025f;
            CreateDisc(root, "Hedgehog pad", new Vector3(side, 0f, 0f),
                new Vector3(0.030f, 0.005f, 0.038f), CityTraceMark.HedgehogTrack);
            CreateDisc(root, "Hedgehog toe", new Vector3(side, 0f, 0.040f),
                new Vector3(0.018f, 0.005f, 0.021f), CityTraceMark.HedgehogTrack);
        }

        private void CreateDisc(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            CityTraceMark mark)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            disc.name = name;
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = position;
            disc.transform.localScale = scale;
            RemoveCollider(disc);
            disc.GetComponent<Renderer>().sharedMaterial = MaterialFor(mark);
        }

        private void CreateBar(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            CityTraceMark mark,
            float yaw)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = name;
            bar.transform.SetParent(parent, false);
            bar.transform.localPosition = position;
            bar.transform.localScale = scale;
            bar.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            RemoveCollider(bar);
            bar.GetComponent<Renderer>().sharedMaterial = MaterialFor(mark);
        }

        private Material MaterialFor(CityTraceMark mark)
        {
            if (materials.TryGetValue(mark, out Material material))
            {
                return material;
            }
            Color colour;
            switch (mark)
            {
                case CityTraceMark.Footprint: colour = new Color(0.12f, 0.52f, 0.58f, 0.78f); break;
                case CityTraceMark.VehicleTyre: colour = new Color(0.25f, 0.31f, 0.34f, 0.68f); break;
                case CityTraceMark.BirdTrack: colour = new Color(0.32f, 0.50f, 0.70f, 0.82f); break;
                case CityTraceMark.SmallPaw: colour = new Color(0.72f, 0.45f, 0.20f, 0.78f); break;
                case CityTraceMark.FoxPaw: colour = new Color(0.82f, 0.30f, 0.16f, 0.82f); break;
                default: colour = new Color(0.43f, 0.31f, 0.46f, 0.78f); break;
            }
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            material = new Material(shader)
            {
                color = colour,
                hideFlags = HideFlags.DontSave,
            };
            materials.Add(mark, material);
            return material;
        }

        private Vector3 ToWorld(Vector2 normalized, float height)
        {
            return new Vector3(
                (normalized.x - 0.5f) * mapWidth,
                height,
                (0.5f - normalized.y) * mapHeight);
        }

        private static Transform Child(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void RemoveCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider == null)
            {
                return;
            }
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(collider);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }
    }
}

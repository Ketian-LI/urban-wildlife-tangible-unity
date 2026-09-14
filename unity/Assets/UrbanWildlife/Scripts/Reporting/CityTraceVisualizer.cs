using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UrbanWildlife.Reporting
{
    /// <summary>
    /// Aggregates recorded movement samples into a deliberately coarse heatmap.
    /// The observation data remains exact, but the map no longer renders individual
    /// shoes, tyres or animal paws. The visual layer starts hidden and is toggled by
    /// the City Prototype with H.
    /// </summary>
    public sealed class CityTraceVisualizer
    {
        public const int HeatmapColumns = 18;
        public const int HeatmapRows = 12;
        public const int MaximumVisibleMarks = HeatmapColumns * HeatmapRows;
        private const int MaximumSourcePoints = 6000;
        private const int IntensityBuckets = 8;
        private const int RingsPerCell = 3;

        private readonly float mapWidth;
        private readonly float mapHeight;
        private readonly Transform root;
        private readonly Transform humanRoot;
        private readonly Transform animalRoot;
        private readonly Transform combinedRoot;
        private readonly Dictionary<int, Material> materials =
            new Dictionary<int, Material>();
        private CityTracePoint[] cachedPoints = Array.Empty<CityTracePoint>();
        private object cachedSourceReference;
        private int cachedSourceCount = -1;

        public CityTraceVisualizer(Transform parent, float mapWidth, float mapHeight)
        {
            if (parent == null || mapWidth <= 0f || mapHeight <= 0f)
            {
                throw new ArgumentException("A trace parent and positive map size are required.");
            }
            this.mapWidth = mapWidth;
            this.mapHeight = mapHeight;
            GameObject rootObject = new GameObject("Live city traces");
            rootObject.transform.SetParent(parent, false);
            root = rootObject.transform;
            humanRoot = Child(root, "Human traces");
            animalRoot = Child(root, "Animal traces");
            combinedRoot = Child(root, "Combined heatmap");
            SetMode(CityTraceDisplayMode.CombinedTrace);
            SetVisible(false);
        }

        public int VisibleMarkCount { get; private set; }
        public int VisibleCellCount => VisibleMarkCount;
        public CityTraceDisplayMode DisplayMode { get; private set; }
        public bool IsVisible { get; private set; }

        public void Render(IEnumerable<CityTracePoint> source)
        {
            if (source is ICollection<CityTracePoint> collection &&
                ReferenceEquals(source, cachedSourceReference) &&
                collection.Count == cachedSourceCount)
            {
                return;
            }
            CityTracePoint[] points = (source ?? Array.Empty<CityTracePoint>())
                .Where(IsUsable)
                .ToArray();
            cachedSourceReference = source;
            cachedSourceCount = source is ICollection<CityTracePoint> sourceCollection
                ? sourceCollection.Count
                : points.Length;
            cachedPoints = points.Length <= MaximumSourcePoints
                ? points
                : points.Skip(points.Length - MaximumSourcePoints).ToArray();
            if (IsVisible)
            {
                RebuildSelectedHeatmap();
            }
        }

        public void SetMode(CityTraceDisplayMode mode)
        {
            DisplayMode = mode;
            humanRoot.gameObject.SetActive(mode == CityTraceDisplayMode.HumanTrace);
            animalRoot.gameObject.SetActive(mode == CityTraceDisplayMode.AnimalTrace);
            combinedRoot.gameObject.SetActive(mode == CityTraceDisplayMode.CombinedTrace);
            if (IsVisible)
            {
                RebuildSelectedHeatmap();
            }
        }

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            root.gameObject.SetActive(visible);
            if (visible)
            {
                RebuildSelectedHeatmap();
            }
        }

        private void RebuildSelectedHeatmap()
        {
            ClearChildren(humanRoot);
            ClearChildren(animalRoot);
            ClearChildren(combinedRoot);
            Transform target = RootFor(DisplayMode);
            int[,] counts = new int[HeatmapColumns, HeatmapRows];
            foreach (CityTracePoint point in cachedPoints.Where(MatchesMode))
            {
                int column = Mathf.Clamp(
                    Mathf.FloorToInt(point.position_norm[0] * HeatmapColumns),
                    0,
                    HeatmapColumns - 1);
                int row = Mathf.Clamp(
                    Mathf.FloorToInt(point.position_norm[1] * HeatmapRows),
                    0,
                    HeatmapRows - 1);
                counts[column, row] += 1;
            }

            int peak = 0;
            for (int column = 0; column < HeatmapColumns; column += 1)
            {
                for (int row = 0; row < HeatmapRows; row += 1)
                {
                    peak = Math.Max(peak, counts[column, row]);
                }
            }
            if (peak == 0)
            {
                VisibleMarkCount = 0;
                return;
            }

            int visibleCells = 0;
            for (int column = 0; column < HeatmapColumns; column += 1)
            {
                for (int row = 0; row < HeatmapRows; row += 1)
                {
                    int count = counts[column, row];
                    if (count <= 0)
                    {
                        continue;
                    }
                    float intensity = Mathf.Sqrt(count / (float)peak);
                    CreateHeatCell(target, column, row, count, intensity);
                    visibleCells += 1;
                }
            }
            VisibleMarkCount = visibleCells;
        }

        private bool MatchesMode(CityTracePoint point)
        {
            switch (DisplayMode)
            {
                case CityTraceDisplayMode.HumanTrace:
                    return point.layer == CityTraceLayer.Human;
                case CityTraceDisplayMode.AnimalTrace:
                    return point.layer == CityTraceLayer.Animal;
                default:
                    return true;
            }
        }

        private void CreateHeatCell(
            Transform parent,
            int column,
            int row,
            int count,
            float intensity)
        {
            GameObject cell = new GameObject($"Heat cell {column:00}-{row:00} · {count}");
            cell.transform.SetParent(parent, false);
            Vector2 normalized = new Vector2(
                (column + 0.5f) / HeatmapColumns,
                (row + 0.5f) / HeatmapRows);
            cell.transform.localPosition = ToWorld(normalized, 0.118f);

            float cellWidth = mapWidth / HeatmapColumns;
            float cellHeight = mapHeight / HeatmapRows;
            float diameter = Mathf.Min(cellWidth, cellHeight) *
                             Mathf.Lerp(0.70f, 1.30f, intensity);
            CreateRing(cell.transform, "Outer glow", diameter, intensity, 0);
            CreateRing(cell.transform, "Middle glow", diameter * 0.68f, intensity, 1);
            CreateRing(cell.transform, "Hot core", diameter * 0.34f, intensity, 2);
        }

        private void CreateRing(
            Transform parent,
            string name,
            float diameter,
            float intensity,
            int ring)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = new Vector3(0f, ring * 0.0015f, 0f);
            disc.transform.localScale = new Vector3(diameter, 0.004f, diameter);
            RemoveCollider(disc);
            int bucket = Mathf.Clamp(
                Mathf.RoundToInt(intensity * (IntensityBuckets - 1)),
                0,
                IntensityBuckets - 1);
            disc.GetComponent<Renderer>().sharedMaterial = MaterialFor(bucket, ring);
        }

        private Material MaterialFor(int bucket, int ring)
        {
            int key = bucket * RingsPerCell + ring;
            if (materials.TryGetValue(key, out Material material))
            {
                return material;
            }
            float intensity = bucket / (float)(IntensityBuckets - 1);
            Color low = new Color(0.12f, 0.58f, 0.92f, 1f);
            Color middle = new Color(0.98f, 0.82f, 0.18f, 1f);
            Color high = new Color(0.94f, 0.20f, 0.12f, 1f);
            Color colour = intensity < 0.5f
                ? Color.Lerp(low, middle, intensity * 2f)
                : Color.Lerp(middle, high, (intensity - 0.5f) * 2f);
            float[] ringAlpha = { 0.11f, 0.22f, 0.48f };
            colour.a = ringAlpha[ring] * Mathf.Lerp(0.55f, 1f, intensity);
            Shader shader = Shader.Find("Sprites/Default") ??
                            Shader.Find("Universal Render Pipeline/Unlit") ??
                            Shader.Find("Unlit/Transparent");
            material = new Material(shader)
            {
                color = colour,
                hideFlags = HideFlags.DontSave,
                renderQueue = 3000 + ring,
            };
            materials.Add(key, material);
            return material;
        }

        private Transform RootFor(CityTraceDisplayMode mode)
        {
            switch (mode)
            {
                case CityTraceDisplayMode.HumanTrace: return humanRoot;
                case CityTraceDisplayMode.AnimalTrace: return animalRoot;
                default: return combinedRoot;
            }
        }

        private Vector3 ToWorld(Vector2 normalized, float height)
        {
            return new Vector3(
                (normalized.x - 0.5f) * mapWidth,
                height,
                (0.5f - normalized.y) * mapHeight);
        }

        private static bool IsUsable(CityTracePoint point)
        {
            return point?.position_norm != null && point.position_norm.Length == 2;
        }

        private static Transform Child(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void ClearChildren(Transform parent)
        {
            for (int index = parent.childCount - 1; index >= 0; index -= 1)
            {
                GameObject child = parent.GetChild(index).gameObject;
                child.SetActive(false);
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UrbanWildlife.Reporting
{
    /// <summary>
    /// Aggregates recorded movement samples into a deliberately coarse heatmap.
    /// The observation data remains exact, while presentation is handled by the
    /// fixed sidebar card instead of drawing footprints or heat discs over the map.
    /// </summary>
    public sealed class CityTraceVisualizer
    {
        public const int HeatmapColumns = 18;
        public const int HeatmapRows = 12;
        public const int MaximumVisibleMarks = HeatmapColumns * HeatmapRows;
        private const int MaximumSourcePoints = 6000;

        private readonly Transform root;
        private readonly Transform humanRoot;
        private readonly Transform animalRoot;
        private readonly Transform combinedRoot;
        private CityTracePoint[] cachedPoints = Array.Empty<CityTracePoint>();
        private object cachedSourceReference;
        private int cachedSourceCount = -1;
        private int[,] selectedCounts = new int[HeatmapColumns, HeatmapRows];
        private int selectedPeak;

        public CityTraceVisualizer(Transform parent, float mapWidth, float mapHeight)
        {
            if (parent == null || mapWidth <= 0f || mapHeight <= 0f)
            {
                throw new ArgumentException("A trace parent and positive map size are required.");
            }
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
        public bool WorldOverlayActive => root.gameObject.activeSelf;

        public int CellCount(int column, int row)
        {
            return column < 0 || column >= HeatmapColumns || row < 0 || row >= HeatmapRows
                ? 0
                : selectedCounts[column, row];
        }

        public float CellIntensity(int column, int row)
        {
            int count = CellCount(column, row);
            return selectedPeak <= 0 || count <= 0
                ? 0f
                : Mathf.Sqrt(count / (float)selectedPeak);
        }

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
            // This object remains as the generated observation-data layer expected by
            // the scene contract. Visual heat is drawn only in the bottom-right UI card.
            root.gameObject.SetActive(false);
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
            selectedCounts = new int[HeatmapColumns, HeatmapRows];
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
                selectedCounts[column, row] += 1;
            }

            selectedPeak = 0;
            for (int column = 0; column < HeatmapColumns; column += 1)
            {
                for (int row = 0; row < HeatmapRows; row += 1)
                {
                    selectedPeak = Math.Max(selectedPeak, selectedCounts[column, row]);
                }
            }
            if (selectedPeak == 0)
            {
                VisibleMarkCount = 0;
                return;
            }

            int visibleCells = 0;
            for (int column = 0; column < HeatmapColumns; column += 1)
            {
                for (int row = 0; row < HeatmapRows; row += 1)
                {
                    int count = selectedCounts[column, row];
                    if (count <= 0)
                    {
                        continue;
                    }
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

    }
}

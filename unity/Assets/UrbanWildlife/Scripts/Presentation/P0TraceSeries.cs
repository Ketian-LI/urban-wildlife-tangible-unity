using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlife.Presentation
{
    public sealed class P0TraceSeries
    {
        private readonly List<Vector2> points = new List<Vector2>();
        private readonly float minimumSampleDistance;
        private readonly int maximumPointCount;

        public P0TraceSeries(float minimumSampleDistance = 0.08f, int maximumPointCount = 1200)
        {
            this.minimumSampleDistance = Mathf.Max(0.001f, minimumSampleDistance);
            this.maximumPointCount = Mathf.Max(2, maximumPointCount);
        }

        public IReadOnlyList<Vector2> Points => points;
        public int PointCount => points.Count;
        public float DistanceUnits { get; private set; }

        public bool TryAppend(Vector2 point)
        {
            if (!IsFinite(point) || points.Count >= maximumPointCount)
            {
                return false;
            }

            if (points.Count == 0)
            {
                points.Add(point);
                return true;
            }

            float distance = Vector2.Distance(points[points.Count - 1], point);
            if (distance < minimumSampleDistance)
            {
                return false;
            }

            points.Add(point);
            DistanceUnits += distance;
            return true;
        }

        private static bool IsFinite(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
                !float.IsNaN(point.y) && !float.IsInfinity(point.y);
        }
    }

    public sealed class P0TraceLine
    {
        private readonly LineRenderer renderer;
        private readonly float surfaceHeight;

        public P0TraceLine(
            Transform parent,
            string name,
            Material material,
            Color colour,
            float width,
            float surfaceHeight = 0.12f,
            float minimumSampleDistance = 0.08f,
            int maximumPointCount = 1200)
        {
            GameObject traceObject = new GameObject(name);
            traceObject.transform.SetParent(parent, false);
            renderer = traceObject.AddComponent<LineRenderer>();
            renderer.useWorldSpace = false;
            renderer.loop = false;
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.numCapVertices = 2;
            renderer.numCornerVertices = 2;
            renderer.widthMultiplier = width;
            renderer.startColor = colour;
            renderer.endColor = colour;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 12;
            this.surfaceHeight = surfaceHeight;
            Series = new P0TraceSeries(minimumSampleDistance, maximumPointCount);
        }

        public P0TraceSeries Series { get; }

        public bool TryAppend(Vector2 point)
        {
            if (!Series.TryAppend(point))
            {
                return false;
            }

            renderer.positionCount = Series.PointCount;
            renderer.SetPosition(Series.PointCount - 1, ToWorldPoint(point));
            return true;
        }

        public void SetVisible(bool visible)
        {
            if (renderer != null)
            {
                renderer.enabled = visible;
            }
        }

        public P0TraceLine CopyTo(
            Transform parent,
            string name,
            Material material,
            Color colour,
            float width)
        {
            P0TraceLine copy = new P0TraceLine(
                parent,
                name,
                material,
                colour,
                width,
                surfaceHeight,
                0.001f,
                Math.Max(2, Series.PointCount));
            foreach (Vector2 point in Series.Points)
            {
                copy.TryAppend(point);
            }
            return copy;
        }

        private Vector3 ToWorldPoint(Vector2 point)
        {
            return new Vector3(point.x, surfaceHeight, point.y);
        }
    }
}

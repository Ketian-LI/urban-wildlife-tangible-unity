using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlife.Presentation
{
    public sealed class P0TraceSeries
    {
        private readonly List<Vector2> points = new List<Vector2>();
        private readonly List<bool> connectsFromPrevious = new List<bool>();
        private readonly float minimumSampleDistance;
        private readonly float maximumSegmentDistance;
        private readonly int maximumPointCount;

        public P0TraceSeries(
            float minimumSampleDistance = 0.08f,
            int maximumPointCount = 1200,
            float maximumSegmentDistance = 0.75f)
        {
            this.minimumSampleDistance = Mathf.Max(0.001f, minimumSampleDistance);
            this.maximumPointCount = Mathf.Max(2, maximumPointCount);
            this.maximumSegmentDistance = Mathf.Max(this.minimumSampleDistance, maximumSegmentDistance);
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
                connectsFromPrevious.Add(false);
                return true;
            }

            float distance = Vector2.Distance(points[points.Count - 1], point);
            if (distance < minimumSampleDistance)
            {
                return false;
            }

            bool isContinuous = distance <= maximumSegmentDistance;
            points.Add(point);
            connectsFromPrevious.Add(isContinuous);
            if (isContinuous)
            {
                DistanceUnits += distance;
            }
            return true;
        }

        public bool IsConnectedFromPrevious(int pointIndex)
        {
            return pointIndex > 0 && pointIndex < connectsFromPrevious.Count &&
                connectsFromPrevious[pointIndex];
        }

        private static bool IsFinite(Vector2 point)
        {
            return !float.IsNaN(point.x) && !float.IsInfinity(point.x) &&
                !float.IsNaN(point.y) && !float.IsInfinity(point.y);
        }
    }

    public sealed class P0TraceLine
    {
        private readonly Mesh mesh;
        private readonly MeshRenderer renderer;
        private readonly float width;
        private readonly float surfaceHeight;

        public P0TraceLine(
            Transform parent,
            string name,
            Material material,
            float width,
            float surfaceHeight = 0.12f,
            float minimumSampleDistance = 0.08f,
            int maximumPointCount = 1200)
        {
            GameObject traceObject = new GameObject(name);
            traceObject.transform.SetParent(parent, false);
            MeshFilter filter = traceObject.AddComponent<MeshFilter>();
            renderer = traceObject.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = $"{name} Ribbon" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 35;
            this.width = Mathf.Max(0.001f, width);
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

            RebuildRibbon();
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
            float width)
        {
            P0TraceLine copy = new P0TraceLine(
                parent,
                name,
                material,
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

        private void RebuildRibbon()
        {
            int pointCount = Series.PointCount;
            mesh.Clear();
            if (pointCount < 2)
            {
                return;
            }

            Vector3[] vertices = new Vector3[pointCount * 2];
            List<int> triangles = new List<int>((pointCount - 1) * 6);
            float halfWidth = width * 0.5f;
            for (int index = 0; index < pointCount; index += 1)
            {
                bool previousConnected = Series.IsConnectedFromPrevious(index);
                bool nextConnected = index < pointCount - 1 && Series.IsConnectedFromPrevious(index + 1);
                Vector2 tangent;
                if (previousConnected && nextConnected)
                {
                    tangent = Series.Points[index + 1] - Series.Points[index - 1];
                }
                else if (nextConnected)
                {
                    tangent = Series.Points[index + 1] - Series.Points[index];
                }
                else if (previousConnected)
                {
                    tangent = Series.Points[index] - Series.Points[index - 1];
                }
                else
                {
                    tangent = Vector2.right;
                }

                if (tangent.sqrMagnitude < 0.000001f)
                {
                    tangent = Vector2.right;
                }
                tangent.Normalize();
                Vector2 normal = new Vector2(-tangent.y, tangent.x) * halfWidth;
                Vector2 point = Series.Points[index];
                vertices[index * 2] = ToWorldPoint(point + normal);
                vertices[index * 2 + 1] = ToWorldPoint(point - normal);
            }

            for (int segment = 0; segment < pointCount - 1; segment += 1)
            {
                if (!Series.IsConnectedFromPrevious(segment + 1))
                {
                    continue;
                }

                int vertex = segment * 2;
                triangles.Add(vertex);
                triangles.Add(vertex + 2);
                triangles.Add(vertex + 1);
                triangles.Add(vertex + 1);
                triangles.Add(vertex + 2);
                triangles.Add(vertex + 3);
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateBounds();
        }
    }
}

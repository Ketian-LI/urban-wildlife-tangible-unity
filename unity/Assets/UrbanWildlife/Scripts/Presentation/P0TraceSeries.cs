using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanWildlife.Presentation
{
    public enum P0TraceMarkStyle
    {
        HumanFootprint,
        BirdTrack,
        SmallPaw,
        FoxPaw,
    }

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
        private readonly float markSize;
        private readonly float surfaceHeight;
        private readonly P0TraceMarkStyle markStyle;

        public P0TraceLine(
            Transform parent,
            string name,
            Material material,
            float width,
            float surfaceHeight = 0.12f,
            float minimumSampleDistance = 0.08f,
            int maximumPointCount = 1200,
            P0TraceMarkStyle markStyle = P0TraceMarkStyle.HumanFootprint)
        {
            GameObject traceObject = new GameObject(name);
            traceObject.transform.SetParent(parent, false);
            MeshFilter filter = traceObject.AddComponent<MeshFilter>();
            renderer = traceObject.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = $"{name} Track Marks" };
            mesh.MarkDynamic();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 35;
            markSize = Mathf.Max(0.02f, width);
            this.surfaceHeight = surfaceHeight;
            this.markStyle = markStyle;
            Series = new P0TraceSeries(minimumSampleDistance, maximumPointCount);
        }

        public P0TraceSeries Series { get; }

        public bool TryAppend(Vector2 point)
        {
            if (!Series.TryAppend(point))
            {
                return false;
            }

            RebuildMarks();
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
                Math.Max(2, Series.PointCount),
                markStyle);
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

        private void RebuildMarks()
        {
            int pointCount = Series.PointCount;
            mesh.Clear();
            if (pointCount < 2)
            {
                return;
            }

            List<Vector3> vertices = new List<Vector3>(pointCount * 12);
            List<int> triangles = new List<int>(pointCount * 18);
            float spacing = StampSpacing();
            float distanceUntilNextStamp = 0f;
            int stampIndex = 0;
            for (int segment = 0; segment < pointCount - 1; segment += 1)
            {
                if (!Series.IsConnectedFromPrevious(segment + 1))
                {
                    distanceUntilNextStamp = 0f;
                    continue;
                }

                Vector2 start = Series.Points[segment];
                Vector2 delta = Series.Points[segment + 1] - start;
                float segmentLength = delta.magnitude;
                if (segmentLength < 0.000001f)
                {
                    continue;
                }

                Vector2 direction = delta / segmentLength;
                float offset = distanceUntilNextStamp;
                while (offset <= segmentLength)
                {
                    AddStamp(vertices, triangles, start + direction * offset, direction, stampIndex);
                    stampIndex += 1;
                    offset += spacing;
                }

                distanceUntilNextStamp = offset - segmentLength;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
        }

        private float StampSpacing()
        {
            switch (markStyle)
            {
                case P0TraceMarkStyle.BirdTrack:
                    return markSize * 1.55f;
                case P0TraceMarkStyle.SmallPaw:
                    return markSize * 1.65f;
                case P0TraceMarkStyle.FoxPaw:
                    return markSize * 1.8f;
                default:
                    return markSize * 1.85f;
            }
        }

        private void AddStamp(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2 position,
            Vector2 direction,
            int stampIndex)
        {
            Vector2 right = new Vector2(direction.y, -direction.x);
            float side = (stampIndex & 1) == 0 ? -1f : 1f;
            switch (markStyle)
            {
                case P0TraceMarkStyle.BirdTrack:
                    AddBirdTrack(vertices, triangles, position + right * side * markSize * 0.12f, direction);
                    break;
                case P0TraceMarkStyle.SmallPaw:
                    AddPaw(vertices, triangles, position + right * side * markSize * 0.1f, direction, 0.82f);
                    break;
                case P0TraceMarkStyle.FoxPaw:
                    AddPaw(vertices, triangles, position + right * side * markSize * 0.13f, direction, 1f);
                    break;
                default:
                    AddHumanFootprint(
                        vertices,
                        triangles,
                        position + right * side * markSize * 0.2f,
                        direction,
                        side);
                    break;
            }
        }

        private void AddHumanFootprint(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2 position,
            Vector2 direction,
            float side)
        {
            Vector2 right = new Vector2(direction.y, -direction.x);
            Vector2[] outline =
            {
                position + direction * markSize * 0.52f + right * markSize * 0.2f,
                position + direction * markSize * 0.52f - right * markSize * 0.2f,
                position + direction * markSize * 0.12f - right * markSize * 0.17f,
                position - direction * markSize * 0.5f - right * markSize * 0.11f,
                position - direction * markSize * 0.5f + right * markSize * 0.11f,
                position + direction * markSize * 0.12f + right * markSize * 0.17f,
            };
            AddPolygon(vertices, triangles, outline);
            AddEllipse(
                vertices,
                triangles,
                position + direction * markSize * 0.49f + right * side * markSize * 0.03f,
                direction,
                markSize * 0.2f,
                markSize * 0.31f,
                8);
        }

        private void AddBirdTrack(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2 position,
            Vector2 direction)
        {
            float stroke = markSize * 0.11f;
            Vector2 origin = position - direction * markSize * 0.1f;
            AddLine(vertices, triangles, origin, origin + direction * markSize * 0.52f, stroke);
            AddLine(vertices, triangles, origin, origin + Rotate(direction, 34f) * markSize * 0.46f, stroke);
            AddLine(vertices, triangles, origin, origin + Rotate(direction, -34f) * markSize * 0.46f, stroke);
            AddLine(vertices, triangles, origin, origin - direction * markSize * 0.28f, stroke * 0.82f);
        }

        private void AddPaw(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2 position,
            Vector2 direction,
            float shapeScale)
        {
            Vector2 right = new Vector2(direction.y, -direction.x);
            AddEllipse(
                vertices,
                triangles,
                position - direction * markSize * 0.08f,
                direction,
                markSize * 0.42f * shapeScale,
                markSize * 0.5f * shapeScale,
                8);

            float[] toeOffsets = { -0.3f, -0.1f, 0.1f, 0.3f };
            for (int index = 0; index < toeOffsets.Length; index += 1)
            {
                float centreBias = 1f - Mathf.Abs(toeOffsets[index]) * 0.35f;
                Vector2 toe = position + direction * markSize * (0.28f + 0.08f * centreBias) +
                    right * markSize * toeOffsets[index];
                AddEllipse(
                    vertices,
                    triangles,
                    toe,
                    direction,
                    markSize * 0.18f * shapeScale,
                    markSize * 0.16f * shapeScale,
                    7);
            }
        }

        private void AddLine(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2 start,
            Vector2 end,
            float width)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude < 0.000001f)
            {
                return;
            }
            direction.Normalize();
            Vector2 normal = new Vector2(-direction.y, direction.x) * width * 0.5f;
            int first = vertices.Count;
            vertices.Add(ToWorldPoint(start + normal));
            vertices.Add(ToWorldPoint(end + normal));
            vertices.Add(ToWorldPoint(end - normal));
            vertices.Add(ToWorldPoint(start - normal));
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }

        private void AddEllipse(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2 centre,
            Vector2 direction,
            float length,
            float width,
            int segmentCount)
        {
            Vector2 right = new Vector2(direction.y, -direction.x);
            int centreIndex = vertices.Count;
            vertices.Add(ToWorldPoint(centre));
            for (int segment = 0; segment < segmentCount; segment += 1)
            {
                float angle = Mathf.PI * 2f * segment / segmentCount;
                Vector2 point = centre + direction * (Mathf.Sin(angle) * length * 0.5f) +
                    right * (Mathf.Cos(angle) * width * 0.5f);
                vertices.Add(ToWorldPoint(point));
            }

            for (int segment = 0; segment < segmentCount; segment += 1)
            {
                triangles.Add(centreIndex);
                triangles.Add(centreIndex + 1 + segment);
                triangles.Add(centreIndex + 1 + ((segment + 1) % segmentCount));
            }
        }

        private void AddPolygon(
            List<Vector3> vertices,
            List<int> triangles,
            IReadOnlyList<Vector2> outline)
        {
            int first = vertices.Count;
            foreach (Vector2 point in outline)
            {
                vertices.Add(ToWorldPoint(point));
            }
            for (int index = 1; index < outline.Count - 1; index += 1)
            {
                triangles.Add(first);
                triangles.Add(first + index);
                triangles.Add(first + index + 1);
            }
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sine = Mathf.Sin(radians);
            float cosine = Mathf.Cos(radians);
            return new Vector2(
                vector.x * cosine - vector.y * sine,
                vector.x * sine + vector.y * cosine);
        }
    }
}

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace BetterLegacy.Core.Helpers
{
    /// <summary>
    /// Shape helper from Default branch, thanks to Pidge for giving this
    /// </summary>
    public static class VGShapes
    {
        public const int MAX_VERTEX_COUNT = 32;
        public const int MIN_VERTEX_COUNT = 3;

        // Add these at the top of the class:
        public struct MeshParams
        {
            public float radius;
            int vertexCount;
            public int VertexCount
            {
                get => vertexCount;
                set
                {
                    vertexCount = value;
                    if (vertexCount < MIN_VERTEX_COUNT)
                        vertexCount = MIN_VERTEX_COUNT;
                    if (vertexCount > MAX_VERTEX_COUNT)
                        vertexCount = MAX_VERTEX_COUNT;
                }
            }
            public float cornerRoundness;
            public float thickness;
            int sliceCount;
            public int SliceCount
            {
                get => sliceCount;
                set
                {
                    sliceCount = value;
                    if (sliceCount <= 0)
                        sliceCount = -1;
                }
            }

            public Vector2 thicknessOffset;
            public Vector2 thicknessScale;

            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 23 + radius.GetHashCode();
                hash = hash * 23 + VertexCount.GetHashCode();
                hash = hash * 23 + cornerRoundness.GetHashCode();
                hash = hash * 23 + thickness.GetHashCode();
                hash = hash * 23 + SliceCount.GetHashCode();
                hash = hash * 23 + thicknessOffset.GetHashCode();
                hash = hash * 23 + thicknessScale.GetHashCode();
                return hash;
            }

            public override bool Equals(object obj)
            {
                if (obj is MeshParams other)
                {
                    return Mathf.Approximately(radius, other.radius) &&
                           VertexCount == other.VertexCount &&
                           Mathf.Approximately(cornerRoundness, other.cornerRoundness) &&
                           Mathf.Approximately(thickness, other.thickness) &&
                           SliceCount == other.SliceCount &&
                           Mathf.Approximately(thicknessOffset.x, other.thicknessOffset.x) &&
                           Mathf.Approximately(thicknessOffset.y, other.thicknessOffset.y) &&
                           Mathf.Approximately(thicknessScale.x, other.thicknessScale.x) &&
                           Mathf.Approximately(thicknessScale.y, other.thicknessScale.y);
                }
                return false;
            }
        }

        struct CachedMesh
        {
            public Mesh mesh;
            public Vector2[] colliderPaths;
        }

        static Dictionary<MeshParams, CachedMesh> shapeCache = new Dictionary<MeshParams, CachedMesh>();

        static CachedMesh GetOrCreateMesh(MeshParams parameters)
        {
            if (shapeCache.TryGetValue(parameters, out CachedMesh cachedMesh))
                return cachedMesh;

            var newCachedMesh = new CachedMesh
            {
                mesh = new Mesh(),
                colliderPaths = new Vector2[0]
            };
            shapeCache[parameters] = newCachedMesh;
            return newCachedMesh;
        }

        public static void FilledMesh(GameObject _go, float radius, int vertexCount) => FilledMesh(_go.GetComponent<MeshFilter>(), _go.GetComponent<PolygonCollider2D>(), radius, vertexCount);

        public static void FilledMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius, int vertexCount)
        {
            vertexCount = Mathf.Clamp(vertexCount, 3, MAX_VERTEX_COUNT);

            var cache = GetOrCreateMesh(new MeshParams
            {
                radius = radius,
                VertexCount = vertexCount,
                cornerRoundness = 0,
                thickness = 1,
                SliceCount = -1,
                thicknessScale = Vector2.one,
            });

            if (cache.mesh.vertexCount > 0)
            {
                meshFilter.sharedMesh = cache.mesh;
                polygonCollider.pathCount = 1;
                polygonCollider.SetPath(0, cache.colliderPaths);
                return;
            }

            // Generate vertices
            Vector3[] vertices = new Vector3[vertexCount + 1]; // +1 for center point
            vertices[0] = Vector3.zero; // Center vertex

            // Generate outer vertices
            float angleStep = (2f * Mathf.PI) / vertexCount;
            float startAngle = -Mathf.PI / 2f; // -90 degrees in radians

            if (vertexCount == 4)
            {
                // make the square not look like a diamond
                startAngle += angleStep / 2;
            }
            else if (vertexCount % 2 == 1)
            {
                startAngle += angleStep / 2;
            }

            for (int i = 0; i < vertexCount; i++)
            {
                float angle = startAngle + i * angleStep;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                vertices[i + 1] = new Vector3(x, y, 0);
            }

            // Generate triangles
            int[] triangles = new int[vertexCount * 3];
            for (int i = 0; i < vertexCount; i++)
            {
                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0; // Center
                triangles[triangleIndex + 1] = (i + 2 > vertexCount) ? 1 : i + 2;
                triangles[triangleIndex + 2] = i + 1;
            }

            // Create outer and inner ring points
            Vector2[] outerPoints = new Vector2[vertexCount];

            // Get points from vertices
            for (int i = 0; i < vertexCount; i++)
                outerPoints[i] = new Vector2(vertices[i].x, vertices[i].y);

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return;

            // Set paths on collider
            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, outerPoints);
        }

        public static void RoundedPolygonMesh(GameObject _go, float radius, int cornerCount, float cornerRoundness) => RoundedPolygonMesh(_go.GetComponent<MeshFilter>(), _go.GetComponent<PolygonCollider2D>(), radius, cornerCount, cornerRoundness);

        public static void RoundedPolygonMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius, int cornerCount, float cornerRoundness)
        {
            cornerCount = Mathf.Clamp(cornerCount, MIN_VERTEX_COUNT, MAX_VERTEX_COUNT);
            cornerRoundness = Mathf.Clamp01(cornerRoundness);

            const int SEGMENTS_PER_CORNER = 4; // Adjust for smoother/rougher corners
            int totalVertices = cornerCount * (SEGMENTS_PER_CORNER + 1);

            var cache = GetOrCreateMesh(new MeshParams
            {
                radius = radius,
                VertexCount = cornerCount,
                cornerRoundness = cornerRoundness,
                thickness = 1,
                SliceCount = -1,
                thicknessScale = Vector2.one,
            });

            if (cache.mesh.vertexCount > 0)
            {
                meshFilter.sharedMesh = cache.mesh;
                polygonCollider.SetPath(0, cache.colliderPaths);
                return;
            }

            // Generate base corner positions
            Vector3[] cornerPositions = new Vector3[cornerCount];
            float angleStep = (2f * Mathf.PI) / cornerCount;
            float startAngle = -Mathf.PI / 2f + (cornerCount == 4 || cornerCount % 2 == 1 ? angleStep / 2 : 0);

            for (int i = 0; i < cornerCount; i++)
            {
                float angle = startAngle + i * angleStep;
                cornerPositions[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );
            }

            // Generate rounded corners
            Vector3[] vertices = new Vector3[totalVertices];
            int currentVertex = 0;

            for (int i = 0; i < cornerCount; i++)
            {
                Vector3 corner = cornerPositions[i];
                Vector3 prevCorner = cornerPositions[(i - 1 + cornerCount) % cornerCount];
                Vector3 nextCorner = cornerPositions[(i + 1) % cornerCount];

                // Calculate control points for rounded corner
                Vector3 toPrev = (prevCorner - corner).normalized * (radius * cornerRoundness);
                Vector3 toNext = (nextCorner - corner).normalized * (radius * cornerRoundness);
                Vector3 p1 = corner + toPrev;  // Changed minus to plus
                Vector3 p2 = corner;
                Vector3 p3 = corner + toNext;  // Changed minus to plus

                // Generate points along the rounded corner
                for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                {
                    float t = j / (float)SEGMENTS_PER_CORNER;
                    vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
                }
            }

            // Generate triangles
            Vector3[] finalVertices = new Vector3[totalVertices + 1];
            finalVertices[0] = Vector3.zero; // Center point
            for (int i = 0; i < totalVertices; i++)
            {
                finalVertices[i + 1] = vertices[i];
            }

            int[] triangles = new int[totalVertices * 3];
            int triIndex = 0;

            // Create triangle fan from center
            for (int i = 0; i < totalVertices; i++)
            {
                triangles[triIndex++] = 0; // Center vertex
                triangles[triIndex++] = (i + 2 > totalVertices) ? 1 : i + 2;
                triangles[triIndex++] = i + 1;
            }

            // Create and assign mesh
            Mesh mesh = new Mesh();
            mesh.vertices = finalVertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return;

            // Update collider
            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, vertices.Select(v => new Vector2(v.x, v.y)).ToArray());
        }

        static Vector3 QuadraticBezier(Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return u * u * p1 + 2 * u * t * p2 + t * t * p3;
        }

        public static void RingMesh(GameObject _go, float radius, int vertexCount, float thickness, Vector2 thicknessOffset = default, Vector2? thicknessScale = null) => RingMesh(_go.GetComponent<MeshFilter>(), _go.GetComponent<PolygonCollider2D>(), radius, vertexCount, thickness, thicknessOffset, thicknessScale);

        public static void RingMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius, int vertexCount, float thickness, Vector2 thicknessOffset = default, Vector2? thicknessScale = null)
        {
            if (thickness >= 1)
            {
                FilledMesh(meshFilter, polygonCollider, radius, vertexCount);
                return;
            }

            var cache = GetOrCreateMesh(new MeshParams
            {
                radius = radius,
                VertexCount = vertexCount,
                cornerRoundness = 0,
                thickness = thickness,
                SliceCount = -1,
                thicknessOffset = thicknessOffset,
                thicknessScale = thicknessScale.GetValueOrDefault(Vector2.one),
            });

            if (cache.mesh.vertexCount > 0)
            {
                meshFilter.sharedMesh = cache.mesh;
                polygonCollider.SetPath(0, cache.colliderPaths);
                return;
            }

            // Minimum 3 vertices for a circle
            vertexCount = Mathf.Clamp(vertexCount, MIN_VERTEX_COUNT, MAX_VERTEX_COUNT);

            // Generate vertices
            Vector3[] vertices = new Vector3[vertexCount * 2]; // +1 for center point

            // Generate outer vertices
            float angleStep = (2f * Mathf.PI) / vertexCount;
            float startAngle = -Mathf.PI / 2f; // -90 degrees in radians

            if (vertexCount == 4)
            {
                // make the square not look like a diamond
                startAngle += angleStep / 2;
            }
            else if (vertexCount % 2 == 1)
            {
                startAngle += angleStep / 2;
            }

            for (int i = 0; i < vertexCount; i++)
            {
                float angle = startAngle + i * angleStep;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                vertices[i] = new Vector3(x, y, 0);
                vertices[i + vertexCount] = new Vector3(x * (1 - thickness), y * (1 - thickness), 0);

                if (thicknessScale.HasValue)
                {
                    var scale = thicknessScale.Value;
                    vertices[i + vertexCount].x *= scale.x;
                    vertices[i + vertexCount].y *= scale.y;
                }
                if (thicknessOffset != default)
                    vertices[i + vertexCount] += (Vector3)thicknessOffset;
            }

            // Generate triangles
            int[] triangles = new int[vertexCount * 6];
            for (int i = 0; i < vertexCount; i++)
            {
                int triangleIndex = i * 6;
                int next = (i + 1) % vertexCount;
                triangles[triangleIndex] = i;
                triangles[triangleIndex + 1] = i + vertexCount;
                triangles[triangleIndex + 2] = next;

                triangles[triangleIndex + 3] = next;
                triangles[triangleIndex + 4] = i + vertexCount;
                triangles[triangleIndex + 5] = next + vertexCount;
            }

            // Create mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;

            // update the collider

            if (!polygonCollider)
                return;

            // Create outer and inner ring points
            Vector2[] outerPoints = new Vector2[vertexCount];
            Vector2[] innerPoints = new Vector2[vertexCount];

            // Get points from vertices
            for (int i = 0; i < vertexCount; i++)
            {
                outerPoints[i] = new Vector2(vertices[i].x, vertices[i].y);
                innerPoints[i] = new Vector2(vertices[i + vertexCount].x, vertices[i + vertexCount].y);
            }

            // Create paths array (outer path and inner path)
            Vector2[][] paths = new Vector2[2][];
            paths[0] = outerPoints;
            // Reverse inner points to create hole
            paths[1] = innerPoints.Reverse().ToArray();

            // Set paths on collider
            polygonCollider.pathCount = 2;
            polygonCollider.SetPath(0, paths[0]);
            polygonCollider.SetPath(1, paths[1]);
        }

        public static void RoundedRingMesh(GameObject _go, float radius = 0.5f, int cornerCount = 4, float cornerRoundness = 0.25f, float thickness = 0.2f, Vector2 thicknessOffset = default, Vector2? thicknessScale = null) => RoundedRingMesh(_go.GetComponent<MeshFilter>(), _go.GetComponent<PolygonCollider2D>(), radius, cornerCount, cornerRoundness, thickness, thicknessOffset, thicknessScale);

        public static void RoundedRingMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius = 0.5f, int cornerCount = 4, float cornerRoundness = 0.25f, float thickness = 0.2f, Vector2 thicknessOffset = default, Vector2? thicknessScale = null)
        {
            if (thickness >= 1)
            {
                RoundedPolygonMesh(meshFilter, polygonCollider, radius, cornerCount, cornerRoundness);
                return;
            }

            if (cornerRoundness <= 0)
            {
                RingMesh(meshFilter, polygonCollider, radius, cornerCount, thickness, thicknessOffset, thicknessScale);
                return;
            }

            const int SEGMENTS_PER_CORNER = 4;
            int verticesPerRing = cornerCount * (SEGMENTS_PER_CORNER + 1);
            int totalVertices = verticesPerRing * 2;

            // Generate base corner positions for outer and inner rings
            Vector3[] outerCorners = new Vector3[cornerCount];
            Vector3[] innerCorners = new Vector3[cornerCount];
            float angleStep = (2f * Mathf.PI) / cornerCount;
            float startAngle = -Mathf.PI / 2f + (cornerCount == 4 || cornerCount % 2 == 1 ? angleStep / 2 : 0);

            for (int i = 0; i < cornerCount; i++)
            {
                float angle = startAngle + i * angleStep;
                outerCorners[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );
                innerCorners[i] = outerCorners[i] * (1 - thickness);
                if (thicknessScale.HasValue)
                {
                    var scale = thicknessScale.Value;
                    innerCorners[i].x *= scale.x;
                    innerCorners[i].y *= scale.y;
                }
                if (thicknessOffset != default)
                    innerCorners[i] += (Vector3)thicknessOffset;
            }

            // Generate vertices for both rings
            Vector3[] vertices = new Vector3[totalVertices];
            int currentVertex = 0;

            // Generate outer ring vertices
            for (int i = 0; i < cornerCount; i++)
            {
                Vector3 corner = outerCorners[i];
                Vector3 prevCorner = outerCorners[(i - 1 + cornerCount) % cornerCount];
                Vector3 nextCorner = outerCorners[(i + 1) % cornerCount];

                Vector3 toPrev = (prevCorner - corner).normalized * (radius * cornerRoundness);
                Vector3 toNext = (nextCorner - corner).normalized * (radius * cornerRoundness);
                Vector3 p1 = corner + toPrev;
                Vector3 p2 = corner;
                Vector3 p3 = corner + toNext;

                for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                {
                    float t = j / (float)SEGMENTS_PER_CORNER;
                    vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
                }
            }

            float insideRadius = radius * (1 - thickness) * (cornerRoundness * (1 - thickness));

            // Generate inner ring vertices
            for (int i = 0; i < cornerCount; i++)
            {
                Vector3 corner = innerCorners[i];
                Vector3 prevCorner = innerCorners[(i - 1 + cornerCount) % cornerCount];
                Vector3 nextCorner = innerCorners[(i + 1) % cornerCount];


                Vector3 toPrev = (prevCorner - corner).normalized * insideRadius;
                Vector3 toNext = (nextCorner - corner).normalized * insideRadius;
                Vector3 p1 = corner + toPrev;
                Vector3 p2 = corner;
                Vector3 p3 = corner + toNext;

                for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                {
                    float t = j / (float)SEGMENTS_PER_CORNER;
                    vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
                }
            }

            // Generate triangles connecting inner and outer rings
            int[] triangles = new int[verticesPerRing * 6];
            int triIndex = 0;

            for (int i = 0; i < verticesPerRing; i++)
            {
                int next = (i + 1) % verticesPerRing;

                // First triangle
                triangles[triIndex++] = i;
                triangles[triIndex++] = i + verticesPerRing;
                triangles[triIndex++] = next;

                // Second triangle
                triangles[triIndex++] = next;
                triangles[triIndex++] = i + verticesPerRing;
                triangles[triIndex++] = next + verticesPerRing;
            }

            // Create and assign mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return;

            // Update collider
            Vector2[] outerPath = vertices.Take(verticesPerRing).Select(v => new Vector2(v.x, v.y)).ToArray();
            Vector2[] innerPath = vertices.Skip(verticesPerRing).Select(v => new Vector2(v.x, v.y)).Reverse().ToArray();

            polygonCollider.pathCount = 2;
            polygonCollider.SetPath(0, outerPath);
            polygonCollider.SetPath(1, innerPath);
        }

        public static void RoundedRingMesh(GameObject _go, float radius = 0.5f, int cornerCount = 4, float cornerRoundness = 0.25f, float thickness = 0.2f, int sliceCount = -1, Vector2 thicknessOffset = default, Vector2? thicknessScale = null) => RoundedRingMesh(_go.GetComponent<MeshFilter>(), _go.GetComponent<PolygonCollider2D>(), radius, cornerCount, cornerRoundness, thickness, sliceCount, thicknessOffset, thicknessScale);

        public static void RoundedRingMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius = 0.5f, int cornerCount = 4, float cornerRoundness = 0.25f, float thickness = 0.2f, int sliceCount = -1, Vector2 thicknessOffset = default, Vector2? thicknessScale = null) // -1 means draw full shape
        {
            cornerCount = Mathf.Clamp(cornerCount, MIN_VERTEX_COUNT, MAX_VERTEX_COUNT);
            sliceCount = sliceCount < 0 ? cornerCount : Mathf.Clamp(sliceCount, 0, cornerCount);

            if (cornerCount > 12)
                cornerRoundness = 0;
            else
            {
                float pos = Mathf.Lerp(0.5f, 0.25f, ((float)cornerCount - 3f) / 9f);
                cornerRoundness = Mathf.Lerp(0, pos, Mathf.Clamp01(cornerRoundness));
            }
            thickness = Mathf.Clamp01(thickness);

            if (thickness >= 1 && cornerCount == sliceCount)
            {
                RoundedPolygonMesh(meshFilter, polygonCollider, radius, cornerCount, cornerRoundness);
                return;
            }

            if (cornerCount == sliceCount)
            {
                RoundedRingMesh(meshFilter, polygonCollider, radius, cornerCount, cornerRoundness, thickness, thicknessOffset, thicknessScale);
                return;
            }

            const int SEGMENTS_PER_CORNER = 4;
            int verticesPerRing = 0;

            for (int i = 0; i < sliceCount; i++)
            {
                if (i == 0 && cornerCount != sliceCount)
                {
                    verticesPerRing += 1;
                }
                else
                {
                    verticesPerRing += SEGMENTS_PER_CORNER + 1;
                }
            }

            verticesPerRing += 1; // +1 for end cap

            int totalVertices = verticesPerRing * 2;

            // Generate base corner positions for outer and inner rings
            Vector3[] outerCorners = new Vector3[sliceCount + 1]; // +1 for end position
            Vector3[] innerCorners = new Vector3[sliceCount + 1];
            float angleStep = (2f * Mathf.PI) / cornerCount;
            float startAngle = -Mathf.PI / 2f + (cornerCount == 4 || cornerCount % 2 == 1 ? angleStep / 2 : 0);

            for (int i = 0; i <= sliceCount; i++)
            {
                float angle = startAngle + i * angleStep;
                outerCorners[i] = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    0
                );
                innerCorners[i] = outerCorners[i] * (1 - thickness);
                if (thicknessScale.HasValue)
                {
                    var scale = thicknessScale.Value;
                    innerCorners[i].x *= scale.x;
                    innerCorners[i].y *= scale.y;
                }
                if (thicknessOffset != default)
                    innerCorners[i] += (Vector3)thicknessOffset;
            }

            // Generate vertices for both rings
            Vector3[] vertices = new Vector3[totalVertices];
            int currentVertex = 0;

            // Generate outer ring vertices
            for (int i = 0; i < sliceCount; i++)
            {
                Vector3 corner = outerCorners[i];

                if (i == 0 && cornerCount != sliceCount)
                {
                    vertices[currentVertex++] = corner;
                }
                else
                {
                    Vector3 prevCorner = i == 0 ? corner : outerCorners[i - 1];
                    Vector3 nextCorner = outerCorners[i + 1];

                    Vector3 toPrev = (prevCorner - corner).normalized * (radius * cornerRoundness);
                    Vector3 toNext = (nextCorner - corner).normalized * (radius * cornerRoundness);
                    Vector3 p1 = corner + toPrev;
                    Vector3 p2 = corner;
                    Vector3 p3 = corner + toNext;

                    for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                    {
                        float t = j / (float)SEGMENTS_PER_CORNER;
                        vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
                    }
                }
            }

            // Add final vertex for end cap
            vertices[currentVertex++] = outerCorners[sliceCount];

            float insideRadius = radius * (1 - thickness) * (cornerRoundness * (1 - thickness));

            // Generate inner ring vertices (same pattern as outer)
            for (int i = 0; i < sliceCount; i++)
            {
                Vector3 corner = innerCorners[i];

                if (i == 0 && cornerCount != sliceCount)
                {
                    vertices[currentVertex++] = corner;
                }
                else
                {
                    Vector3 prevCorner = i == 0 ? corner : innerCorners[i - 1];
                    Vector3 nextCorner = innerCorners[i + 1];

                    Vector3 toPrev = (prevCorner - corner).normalized * insideRadius;
                    Vector3 toNext = (nextCorner - corner).normalized * insideRadius;
                    Vector3 p1 = corner + toPrev;
                    Vector3 p2 = corner;
                    Vector3 p3 = corner + toNext;

                    for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                    {
                        float t = j / (float)SEGMENTS_PER_CORNER;
                        vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, t);
                    }
                }
            }

            // Add final vertex for inner end cap
            vertices[currentVertex++] = innerCorners[sliceCount];

            // Generate triangles connecting inner and outer rings
            int[] triangles = new int[(verticesPerRing - 1) * 6];
            int triIndex = 0;

            for (int i = 0; i < verticesPerRing - 1; i++)
            {
                int next = i + 1;

                // First triangle
                triangles[triIndex++] = i;
                triangles[triIndex++] = i + verticesPerRing;
                triangles[triIndex++] = next;

                // Second triangle
                triangles[triIndex++] = next;
                triangles[triIndex++] = i + verticesPerRing;
                triangles[triIndex++] = next + verticesPerRing;
            }

            // Create and assign mesh
            Mesh mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return;

            // Update collider
            // Get outer and inner vertices
            Vector2[] outerVerts = vertices.Take(verticesPerRing).Select(v => new Vector2(v.x, v.y)).ToArray();
            Vector2[] innerVerts = vertices.Skip(verticesPerRing).Select(v => new Vector2(v.x, v.y)).ToArray();

            // Create single path that goes around the shape
            Vector2[] colliderPath = new Vector2[verticesPerRing * 2];
            int pathIndex = 0;

            // Add outer vertices
            for (int i = 0; i < verticesPerRing; i++)
                colliderPath[pathIndex++] = outerVerts[i];

            // Add inner vertices in reverse
            for (int i = verticesPerRing - 1; i >= 0; i--)
                colliderPath[pathIndex++] = innerVerts[i];

            // Set single closed path
            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, colliderPath);
        }
    }
}

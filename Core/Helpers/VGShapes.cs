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
        const int SEGMENTS_PER_CORNER = 4;

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
            public float thicknessRotation;
            public float rotation;
            public float alternate;

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
                hash = hash * 23 + thicknessRotation.GetHashCode();
                hash = hash * 23 + rotation.GetHashCode();
                hash = hash * 23 + alternate.GetHashCode();
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
                           Mathf.Approximately(thicknessScale.y, other.thicknessScale.y) &&
                           Mathf.Approximately(thicknessRotation, other.thicknessRotation) &&
                           Mathf.Approximately(rotation, other.rotation) &&
                           Mathf.Approximately(alternate, other.alternate);
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

        /// <summary>
        /// Generates a custom polygon shape.
        /// </summary>
        /// <param name="meshFilter">Mesh Filter to assign the polygon mesh to.</param>
        /// <param name="polygonCollider">Polygon Collider to draw collider path to.</param>
        /// <param name="radius">Size of the polygon.</param>
        /// <param name="cornerCount">Amount of corners the polygon has.</param>
        public static Mesh FilledMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius, int sides, float rotation = 0f, float alternate = 1f)
        {
            sides = Mathf.Clamp(sides, MIN_VERTEX_COUNT, MAX_VERTEX_COUNT);

            var cache = GetOrCreateMesh(new MeshParams
            {
                radius = radius,
                VertexCount = sides,
                cornerRoundness = 0,
                thickness = 1,
                SliceCount = -1,
                thicknessScale = Vector2.one,
            });

            if (cache.mesh.vertexCount > 0)
            {
                if (meshFilter)
                    meshFilter.sharedMesh = cache.mesh;
                if (polygonCollider)
                {
                    polygonCollider.pathCount = 1;
                    polygonCollider.SetPath(0, cache.colliderPaths);
                }
                return cache.mesh;
            }

            // Generate vertices
            Vector3[] vertices = new Vector3[sides + 1]; // +1 for center point
            vertices[0] = Vector3.zero; // Center vertex

            // Generate outer vertices
            float angleStep = (2f * Mathf.PI) / sides;
            float startAngle = -Mathf.PI / 2f + (sides == 4 || sides % 2 == 1 ? angleStep / 2 : 0) + Rotation(rotation);

            for (int i = 0; i < sides; i++)
            {
                float angle = startAngle + i * angleStep;
                var r = radius * (i % 2 == 0 ? alternate : 1f);
                float x = Mathf.Cos(angle) * r;
                float y = Mathf.Sin(angle) * r;
                vertices[i + 1] = new Vector3(x, y, 0);
            }

            // Generate triangles
            int[] triangles = new int[sides * 3];
            for (int i = 0; i < sides; i++)
            {
                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0; // Center
                triangles[triangleIndex + 1] = (i + 2 > sides) ? 1 : i + 2;
                triangles[triangleIndex + 2] = i + 1;
            }

            // Create mesh
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            if (meshFilter)
                meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return mesh;

            // Set paths on collider
            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, vertices.Select(v => new Vector2(v.x, v.y)).ToArray());
            return mesh;
        }

        /// <summary>
        /// Generates a custom polygon shape.
        /// </summary>
        /// <param name="meshFilter">Mesh Filter to assign the polygon mesh to.</param>
        /// <param name="polygonCollider">Polygon Collider to draw collider path to.</param>
        /// <param name="radius">Size of the polygon.</param>
        /// <param name="cornerCount">Amount of corners the polygon has.</param>
        /// <param name="cornerRoundness">How round the polygons' corners are.</param>
        public static Mesh RoundedPolygonMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius, int sides, float roundness, float rotation = 0f, float alternate = 1f)
        {
            sides = Mathf.Clamp(sides, MIN_VERTEX_COUNT, MAX_VERTEX_COUNT);
            roundness = Mathf.Clamp01(roundness);

            int totalVertices = sides * (roundness == 0f ? 1 : (SEGMENTS_PER_CORNER + 1));

            var cache = GetOrCreateMesh(new MeshParams
            {
                radius = radius,
                VertexCount = sides,
                cornerRoundness = roundness,
                thickness = 1,
                SliceCount = -1,
                thicknessScale = Vector2.one,
            });

            if (cache.mesh.vertexCount > 0)
            {
                if (meshFilter)
                    meshFilter.sharedMesh = cache.mesh;
                if (polygonCollider)
                    polygonCollider.SetPath(0, cache.colliderPaths);
                return cache.mesh;
            }

            // Generate base corner positions
            Vector3[] cornerPositions = new Vector3[sides];
            float angleStep = (2f * Mathf.PI) / sides;
            float startAngle = -Mathf.PI / 2f + (sides == 4 || sides % 2 == 1 ? angleStep / 2 : 0) + Rotation(rotation);

            for (int i = 0; i < sides; i++)
            {
                float angle = startAngle + i * angleStep;
                var r = radius * (i % 2 == 0 ? alternate : 1f);
                cornerPositions[i] = new Vector3(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r,
                    0
                );
            }

            // Generate rounded corners
            Vector3[] vertices = new Vector3[totalVertices];
            int currentVertex = 0;

            if (roundness > 0f)
                for (int i = 0; i < sides; i++)
                {
                    Vector3 corner = cornerPositions[i];
                    Vector3 prevCorner = cornerPositions[(i - 1 + sides) % sides];
                    Vector3 nextCorner = cornerPositions[(i + 1) % sides];

                    // Calculate control points for rounded corner
                    Vector3 toPrev = (prevCorner - corner).normalized * (radius * roundness);
                    Vector3 toNext = (nextCorner - corner).normalized * (radius * roundness);
                    Vector3 p1 = corner + toPrev;  // Changed minus to plus
                    Vector3 p2 = corner;
                    Vector3 p3 = corner + toNext;  // Changed minus to plus

                    // Generate points along the rounded corner
                    for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                        vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, j / (float)SEGMENTS_PER_CORNER);
                }
            else
                System.Array.Copy(cornerPositions, vertices, vertices.Length);

            // Generate triangles
            Vector3[] finalVertices = new Vector3[totalVertices + 1];
            finalVertices[0] = Vector3.zero; // Center point
            for (int i = 0; i < totalVertices; i++)
                finalVertices[i + 1] = vertices[i];

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
            var mesh = new Mesh();
            mesh.vertices = finalVertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            if (meshFilter)
                meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return mesh;

            // Update collider
            polygonCollider.pathCount = 1;
            polygonCollider.SetPath(0, vertices.Select(v => new Vector2(v.x, v.y)).ToArray());
            return mesh;
        }

        /// <summary>
        /// Generates a custom polygon shape.
        /// </summary>
        /// <param name="meshFilter">Mesh Filter to assign the polygon mesh to.</param>
        /// <param name="polygonCollider">Polygon Collider to draw collider path to.</param>
        /// <param name="radius">Size of the polygon.</param>
        /// <param name="cornerCount">Amount of corners the polygon has.</param>
        /// <param name="thickness">Outline thickness.</param>
        /// <param name="thicknessOffset">Outline position offset.</param>
        /// <param name="thicknessScale">Outline scale offset.</param>
        public static Mesh RingMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius, int sides, float thickness, Vector2 thicknessOffset = default, Vector2? thicknessScale = null, float rotation = 0f, float thicknessRotation = 0f, float alternate = 1f)
        {
            if (thickness >= 1)
                return FilledMesh(meshFilter, polygonCollider, radius, sides, rotation, alternate);

            var cache = GetOrCreateMesh(new MeshParams
            {
                radius = radius,
                VertexCount = sides,
                cornerRoundness = 0,
                thickness = thickness,
                SliceCount = -1,
                thicknessOffset = thicknessOffset,
                thicknessScale = thicknessScale.GetValueOrDefault(Vector2.one),
            });

            if (cache.mesh.vertexCount > 0)
            {
                if (meshFilter)
                    meshFilter.sharedMesh = cache.mesh;
                if (polygonCollider)
                    polygonCollider.SetPath(0, cache.colliderPaths);
                return cache.mesh;
            }

            // Minimum 3 vertices for a circle
            sides = Mathf.Clamp(sides, MIN_VERTEX_COUNT, MAX_VERTEX_COUNT);

            // Generate vertices
            Vector3[] vertices = new Vector3[sides * 2]; // +1 for center point

            // Generate outer vertices
            float angleStep = (2f * Mathf.PI) / sides;
            // Angle specific shapes according to their regular angle.
            float startAngle = -Mathf.PI / 2f + (sides == 4 || sides % 2 == 1 ? angleStep / 2 : 0) + Rotation(rotation);

            for (int i = 0; i < sides; i++)
            {
                float angle = startAngle + i * angleStep;
                var r = radius * (i % 2 == 0 ? alternate : 1f);
                float x = Mathf.Cos(angle) * r;
                float y = Mathf.Sin(angle) * r;
                vertices[i] = new Vector3(x, y, 0);
                vertices[i + sides] = new Vector3(x * (1 - thickness), y * (1 - thickness), 0);

                if (thicknessScale.HasValue)
                {
                    var scale = thicknessScale.Value;
                    vertices[i + sides].x *= scale.x;
                    vertices[i + sides].y *= scale.y;
                }
                if (thicknessRotation != 0f)
                    vertices[i + sides] = RTMath.Rotate(vertices[i + sides], thicknessRotation);
                if (thicknessOffset != default)
                    vertices[i + sides] += (Vector3)thicknessOffset;
            }

            // Generate triangles
            int[] triangles = new int[sides * 6];
            for (int i = 0; i < sides; i++)
            {
                int triangleIndex = i * 6;
                int next = (i + 1) % sides;
                triangles[triangleIndex] = i;
                triangles[triangleIndex + 1] = i + sides;
                triangles[triangleIndex + 2] = next;

                triangles[triangleIndex + 3] = next;
                triangles[triangleIndex + 4] = i + sides;
                triangles[triangleIndex + 5] = next + sides;
            }

            // Create mesh
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            if (meshFilter)
                meshFilter.sharedMesh = mesh;

            // update the collider

            if (!polygonCollider)
                return mesh;

            // Create outer and inner ring points
            Vector2[] outerPoints = new Vector2[sides];
            Vector2[] innerPoints = new Vector2[sides];

            // Get points from vertices
            for (int i = 0; i < sides; i++)
            {
                outerPoints[i] = new Vector2(vertices[i].x, vertices[i].y);
                innerPoints[i] = new Vector2(vertices[i + sides].x, vertices[i + sides].y);
            }

            // Create paths array (outer path and inner path)
            Vector2[][] paths = new Vector2[2][]
            {
                outerPoints,
                // Reverse inner points to create hole
                innerPoints.Reverse().ToArray()
            };

            // Set paths on collider
            polygonCollider.pathCount = 2;
            polygonCollider.SetPath(0, paths[0]);
            polygonCollider.SetPath(1, paths[1]);
            return mesh;
        }

        /// <summary>
        /// Generates a custom polygon shape.
        /// </summary>
        /// <param name="meshFilter">Mesh Filter to assign the polygon mesh to.</param>
        /// <param name="polygonCollider">Polygon Collider to draw collider path to.</param>
        /// <param name="radius">Size of the polygon.</param>
        /// <param name="cornerCount">Amount of corners the polygon has.</param>
        /// <param name="cornerRoundness">How round the polygons' corners are.</param>
        /// <param name="thickness">Outline thickness.</param>
        /// <param name="thicknessOffset">Outline position offset.</param>
        /// <param name="thicknessScale">Outline scale offset.</param>
        public static Mesh RoundedRingMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius = 0.5f, int sides = 4, float roundness = 0.25f, float thickness = 0.2f, Vector2 thicknessOffset = default, Vector2? thicknessScale = null, float rotation = 0f, float thicknessRotation = 0f, float alternate = 1f)
        {
            if (thickness >= 1)
                return RoundedPolygonMesh(meshFilter, polygonCollider, radius, sides, roundness, rotation, alternate);

            if (roundness <= 0)
                return RingMesh(meshFilter, polygonCollider, radius, sides, thickness, thicknessOffset, thicknessScale, rotation, thicknessRotation, alternate);

            int verticesPerRing = sides * (roundness == 0f ? 1 : (SEGMENTS_PER_CORNER + 1));
            int totalVertices = verticesPerRing * 2;

            // Generate base corner positions for outer and inner rings
            Vector3[] outerCorners = new Vector3[sides];
            Vector3[] innerCorners = new Vector3[sides];
            float angleStep = (2f * Mathf.PI) / sides;
            float startAngle = -Mathf.PI / 2f + (sides == 4 || sides % 2 == 1 ? angleStep / 2 : 0) + Rotation(rotation);

            for (int i = 0; i < sides; i++)
            {
                float angle = startAngle + i * angleStep;
                var r = radius * (i % 2 == 0 ? alternate : 1f);
                outerCorners[i] = new Vector3(
                    Mathf.Cos(angle) * r,
                    Mathf.Sin(angle) * r,
                    0
                );
                innerCorners[i] = outerCorners[i] * (1 - thickness);
                if (thicknessScale.HasValue)
                {
                    var scale = thicknessScale.Value;
                    innerCorners[i].x *= scale.x;
                    innerCorners[i].y *= scale.y;
                }
                if (thicknessRotation != 0f)
                    innerCorners[i] = RTMath.Rotate(innerCorners[i], thicknessRotation);
                if (thicknessOffset != default)
                    innerCorners[i] += (Vector3)thicknessOffset;
            }

            // Generate vertices for both rings
            Vector3[] vertices = new Vector3[totalVertices];
            int currentVertex = 0;

            // Generate outer ring vertices
            for (int i = 0; i < sides; i++)
            {
                Vector3 corner = outerCorners[i];
                Vector3 prevCorner = outerCorners[(i - 1 + sides) % sides];
                Vector3 nextCorner = outerCorners[(i + 1) % sides];

                Vector3 toPrev = (prevCorner - corner).normalized * (radius * roundness);
                Vector3 toNext = (nextCorner - corner).normalized * (radius * roundness);
                Vector3 p1 = corner + toPrev;
                Vector3 p2 = corner;
                Vector3 p3 = corner + toNext;

                for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                    vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, j / (float)SEGMENTS_PER_CORNER);
            }

            float insideRadius = radius * (1 - thickness) * (roundness * (1 - thickness));

            // Generate inner ring vertices
            for (int i = 0; i < sides; i++)
            {
                Vector3 corner = innerCorners[i];
                Vector3 prevCorner = innerCorners[(i - 1 + sides) % sides];
                Vector3 nextCorner = innerCorners[(i + 1) % sides];

                Vector3 toPrev = (prevCorner - corner).normalized * insideRadius;
                Vector3 toNext = (nextCorner - corner).normalized * insideRadius;
                Vector3 p1 = corner + toPrev;
                Vector3 p2 = corner;
                Vector3 p3 = corner + toNext;

                for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                    vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, j / (float)SEGMENTS_PER_CORNER);
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
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return mesh;

            // Update collider
            Vector2[] outerPath = vertices.Take(verticesPerRing).Select(v => new Vector2(v.x, v.y)).ToArray();
            Vector2[] innerPath = vertices.Skip(verticesPerRing).Select(v => new Vector2(v.x, v.y)).Reverse().ToArray();

            polygonCollider.pathCount = 2;
            polygonCollider.SetPath(0, outerPath);
            polygonCollider.SetPath(1, innerPath);
            return mesh;
        }

        /// <summary>
        /// Generates a custom polygon shape.
        /// </summary>
        /// <param name="meshFilter">Mesh Filter to assign the polygon mesh to.</param>
        /// <param name="polygonCollider">Polygon Collider to draw collider path to.</param>
        /// <param name="radius">Size of the polygon.</param>
        /// <param name="cornerCount">Amount of corners the polygon has.</param>
        /// <param name="cornerRoundness">How round the polygons' corners are.</param>
        /// <param name="thickness">Outline thickness.</param>
        /// <param name="sliceCount">Amount of slices. -1 draws the full shape.</param>
        /// <param name="thicknessOffset">Outline position offset.</param>
        /// <param name="thicknessScale">Outline scale offset.</param>
        public static Mesh RoundedRingMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius = 0.5f, int sides = 4, float roundness = 0.25f, float thickness = 0.2f, int slices = -1, Vector2 thicknessOffset = default, Vector2? thicknessScale = null, float angle = 0f, float thicknessRotation = 0f, float alternate = 1f)
        {
            sides = Mathf.Clamp(sides, MIN_VERTEX_COUNT, MAX_VERTEX_COUNT);
            slices = slices < 0 ? sides : Mathf.Clamp(slices, 0, sides);

            if (sides > 12)
                roundness = 0;
            else
                roundness = Mathf.Lerp(0, Mathf.Lerp(0.5f, 0.25f, ((float)sides - 3f) / 9f), Mathf.Clamp01(roundness));
            thickness = Mathf.Clamp01(thickness);

            if (thickness >= 1 && sides == slices)
                return RoundedPolygonMesh(meshFilter, polygonCollider, radius, sides, roundness, angle, alternate);

            if (sides == slices)
                return RoundedRingMesh(meshFilter, polygonCollider, radius, sides, roundness, thickness, thicknessOffset, thicknessScale, angle, thicknessRotation, alternate);

            int verticesPerRing = 0;

            for (int i = 0; i < slices; i++)
                verticesPerRing += i == 0 && sides != slices || roundness == 0f ? 1 : SEGMENTS_PER_CORNER + 1;

            verticesPerRing += 1; // +1 for end cap

            int totalVertices = verticesPerRing * 2;

            // Generate base corner positions for outer and inner rings
            Vector3[] outerCorners = new Vector3[slices + 1]; // +1 for end position
            Vector3[] innerCorners = new Vector3[slices + 1];
            float angleStep = (2f * Mathf.PI) / sides;
            float startAngle = -Mathf.PI / 2f + (sides == 4 || sides % 2 == 1 ? angleStep / 2 : 0) + Rotation(angle);

            for (int i = 0; i <= slices; i++)
            {
                float sliceAngle = startAngle + i * angleStep;
                var r = radius * (i % 2 == 0 ? alternate : 1f);
                outerCorners[i] = new Vector3(
                    Mathf.Cos(sliceAngle) * r,
                    Mathf.Sin(sliceAngle) * r,
                    0
                );
                innerCorners[i] = outerCorners[i] * (1 - thickness);
                if (thicknessScale.HasValue)
                {
                    var scale = thicknessScale.Value;
                    innerCorners[i].x *= scale.x;
                    innerCorners[i].y *= scale.y;
                }
                if (thicknessRotation != 0f)
                    innerCorners[i] = RTMath.Rotate(innerCorners[i], thicknessRotation);
                if (thicknessOffset != default)
                    innerCorners[i] += (Vector3)thicknessOffset;
            }

            // Generate vertices for both rings
            Vector3[] vertices = new Vector3[totalVertices];
            int currentVertex = 0;

            // Generate outer ring vertices
            for (int i = 0; i < slices; i++)
            {
                Vector3 corner = outerCorners[i];

                if (i == 0 && sides != slices || roundness == 0f)
                    vertices[currentVertex++] = corner;
                else
                {
                    Vector3 prevCorner = i == 0 ? corner : outerCorners[i - 1];
                    Vector3 nextCorner = outerCorners[i + 1];

                    Vector3 toPrev = (prevCorner - corner).normalized * (radius * roundness);
                    Vector3 toNext = (nextCorner - corner).normalized * (radius * roundness);
                    Vector3 p1 = corner + toPrev;
                    Vector3 p2 = corner;
                    Vector3 p3 = corner + toNext;

                    for (int j = 0; j <= SEGMENTS_PER_CORNER; j++)
                        vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, j / (float)SEGMENTS_PER_CORNER);
                }
            }

            // Add final vertex for end cap
            vertices[currentVertex++] = outerCorners[slices];

            float insideRadius = radius * (1 - thickness) * (roundness * (1 - thickness));

            // Generate inner ring vertices (same pattern as outer)
            for (int i = 0; i < slices; i++)
            {
                Vector3 corner = innerCorners[i];

                if (i == 0 && sides != slices || roundness == 0f)
                    vertices[currentVertex++] = corner;
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
                        vertices[currentVertex++] = QuadraticBezier(p1, p2, p3, j / (float)SEGMENTS_PER_CORNER);
                }
            }

            // Add final vertex for inner end cap
            vertices[currentVertex++] = innerCorners[slices];

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
            var mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            if (meshFilter)
                meshFilter.sharedMesh = mesh;

            if (!polygonCollider)
                return mesh;

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
            return mesh;
        }

        public static Mesh RoundedRingMesh3D(MeshFilter meshFilter, PolygonCollider2D polygonCollider, float radius = 0.5f, int sides = 4, float roundness = 0.25f, float thickness = 0.2f, int slices = -1, Vector2 thicknessOffset = default, Vector2? thicknessScale = null, float angle = 0f, float thicknessRotation = 0f, float alternate = 1f)
        {
            var mesh = RoundedRingMesh(meshFilter, polygonCollider, radius, sides, roundness, thickness, slices, thicknessOffset, thicknessScale, angle, thicknessRotation, alternate);
            
            // Copy the shape and add depth
            var vertices = mesh.vertices.Copy();
            for (int i = 0; i < vertices.Length; i++)
                vertices[i].z = 0.5f;
            var triangles = mesh.triangles.Copy();
            // reverse triangles so the other side renders outside instead of inside
            for (int i = 0; i < triangles.Length; i++)
                triangles[i] = -(triangles[i] - (vertices.Length));
            var vertexList = new List<Vector3>();
            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                var vertex = mesh.vertices[i];
                vertexList.Add(new Vector3(vertex.x, vertex.y, -0.5f));
            }
            vertexList.AddRange(vertices);
            var triangleList = new List<int>(mesh.triangles);
            triangleList.AddRange(triangles);

            var vertexCount = vertices.Length;
            // Generate triangles around the sides
            for (int i = 0; i < vertexCount; i++)
            {
                int next = (i + 1) % (vertexCount);

                // Second triangle (reverse)
                triangleList.Add(next + vertexCount);
                triangleList.Add(i + vertexCount);
                triangleList.Add(next);

                // First triangle
                triangleList.Add(next);
                triangleList.Add(i + vertexCount);
                triangleList.Add(i);
            }
            mesh.vertices = vertexList.ToArray();
            mesh.triangles = triangleList.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        // code from Project Arrhythmia 1.0.0 demo
        public static Mesh PolygonMesh(MeshFilter meshFilter, PolygonCollider2D polygonCollider, List<Vector2> vertices2D)
        {
            var points = vertices2D.ToArray();
            var mesh = new Mesh();
            Vector3[] array2 = new Vector3[points.Length];
            for (int i = 0; i < points.Length; i++)
                array2[i] = new Vector3(points[i].x, points[i].y, 0f);
            int[] triangles = Triangulate(points);
            mesh.vertices = array2;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (meshFilter)
                meshFilter.mesh = mesh;
            if (polygonCollider)
                polygonCollider.SetPath(0, points);
            return mesh;
        }

        static int[] Triangulate(Vector2[] points)
        {
            var list = new List<int>();
            var count = points.Length;
            int[] result;
            if (count < 3)
                result = list.ToArray();
            else
            {
                int[] array = new int[count];
                int i;
                if (Area(points) > 0f)
                    for (i = 0; i < count; i++)
                        array[i] = i;
                else
                    for (i = 0; i < count; i++)
                        array[i] = count - 1 - i;

                int amount = count;
                int num2 = 2 * amount;
                i = amount - 1;
                while (true)
                {
                    if (amount > 2)
                    {
                        if (num2-- <= 0)
                        {
                            result = list.ToArray();
                            break;
                        }
                        int u = i;
                        if (amount <= u)
                            u = 0;
                        i = u + 1;
                        if (amount <= i)
                            i = 0;
                        int w = i + 1;
                        if (amount <= w)
                            w = 0;
                        if (!Snip(points, u, i, w, amount, array))
                            continue;
                        list.Add(array[u]);
                        list.Add(array[i]);
                        list.Add(array[w]);
                        var cornerIndex = i;
                        for (int num10 = i + 1; num10 < amount; num10++)
                        {
                            array[cornerIndex] = array[num10];
                            cornerIndex++;
                        }
                        amount--;
                        num2 = 2 * amount;
                        continue;
                    }
                    list.Reverse();
                    result = list.ToArray();
                    break;
                }
            }
            return result;
        }

        static float Area(Vector2[] points)
        {
            int count = points.Length;
            float num = 0f;
            int num2 = 0;
            int index = count - 1;
            while (num2 < count)
            {
                Vector2 vector = points[index];
                Vector2 vector2 = points[num2];
                num += vector.x * vector2.y - vector2.x * vector.y;
                index = num2++;
            }
            return num * 0.5f;
        }

        static bool Snip(Vector2[] points, int u, int v, int w, int n, int[] V)
        {
            Vector2 a = points[V[u]];
            Vector2 b = points[V[v]];
            Vector2 c = points[V[w]];
            bool snipped;
            if (!(Mathf.Epsilon <= (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)))
                snipped = false;
            else
            {
                var num = 0;
                while (true)
                {
                    if (num < n)
                    {
                        if (num != u && num != v && num != w)
                        {
                            Vector2 p = points[V[num]];
                            if (InsideTriangle(a, b, c, p))
                            {
                                snipped = false;
                                break;
                            }
                        }
                        num++;
                        continue;
                    }
                    snipped = true;
                    break;
                }
            }
            return snipped;
        }

        static bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            bool num16 = !(((C.x - B.x) * (P.y - B.y) - (C.y - B.y) * (P.x - B.x)) < 0f);
            if (num16)
                num16 = !(((A.x - C.x) * (P.y - C.y) - (A.y - C.y) * (P.x - C.x)) < 0f);
            if (num16)
                num16 = !(((B.x - A.x) * (P.y - A.y) - (B.y - A.y) * (P.x - A.x)) < 0f);
            return num16;
        }

        static float Rotation(float rotation) => rotation == 0 ? 0 : rotation * Mathf.PI / 180f;

        static Vector3 QuadraticBezier(Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1 - t;
            return u * u * p1 + 2 * u * t * p2 + t * t * p3;
        }
    }
}

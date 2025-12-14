// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CardiacVR.Rendering
{
    /// <summary>
    /// Advanced mesh enhancement and beautification system
    /// Provides smoothing, decimation, and visual improvements for segmented meshes
    /// </summary>
    public class MeshEnhancementSystem : MonoBehaviour
    {
        public static MeshEnhancementSystem Instance { get; private set; }

        [Header("Smoothing Settings")]
        [SerializeField] private int defaultSmoothingIterations = 3;
        [SerializeField] private float defaultSmoothingStrength = 0.5f;
        [SerializeField] private bool preserveVolume = true;

        [Header("Normal Smoothing")]
        [SerializeField] private float normalSmoothingAngle = 60f;
        [SerializeField] private bool weightByArea = true;

        [Header("Edge Enhancement")]
        [SerializeField] private bool detectFeatureEdges = true;
        [SerializeField] private float featureAngleThreshold = 30f;

        [Header("Quality")]
        [SerializeField] private bool generateTangents = true;
        [SerializeField] private bool optimizeMesh = true;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Mesh Smoothing

        /// <summary>
        /// Apply Laplacian smoothing to mesh vertices
        /// </summary>
        public Mesh ApplyLaplacianSmoothing(Mesh inputMesh, int iterations = -1, float strength = -1)
        {
            if (iterations < 0) iterations = defaultSmoothingIterations;
            if (strength < 0) strength = defaultSmoothingStrength;

            Mesh smoothedMesh = Instantiate(inputMesh);
            smoothedMesh.name = inputMesh.name + "_Smoothed";

            Vector3[] vertices = smoothedMesh.vertices;
            int[] triangles = smoothedMesh.triangles;

            // Build adjacency list
            Dictionary<int, HashSet<int>> adjacency = BuildAdjacencyList(triangles, vertices.Length);

            // Calculate original volume if preserving
            float originalVolume = preserveVolume ? CalculateMeshVolume(vertices, triangles) : 0;
            Vector3 originalCenter = preserveVolume ? CalculateMeshCenter(vertices) : Vector3.zero;

            // Apply smoothing iterations
            for (int iter = 0; iter < iterations; iter++)
            {
                Vector3[] newVertices = new Vector3[vertices.Length];

                for (int i = 0; i < vertices.Length; i++)
                {
                    if (adjacency[i].Count == 0)
                    {
                        newVertices[i] = vertices[i];
                        continue;
                    }

                    // Calculate Laplacian (average of neighbors minus vertex)
                    Vector3 laplacian = Vector3.zero;
                    foreach (int neighbor in adjacency[i])
                    {
                        laplacian += vertices[neighbor];
                    }
                    laplacian /= adjacency[i].Count;
                    laplacian -= vertices[i];

                    // Apply smoothing
                    newVertices[i] = vertices[i] + laplacian * strength;
                }

                vertices = newVertices;

                // Volume preservation
                if (preserveVolume && originalVolume > 0)
                {
                    float currentVolume = CalculateMeshVolume(vertices, triangles);
                    if (currentVolume > 0)
                    {
                        float scale = Mathf.Pow(originalVolume / currentVolume, 1f / 3f);
                        Vector3 currentCenter = CalculateMeshCenter(vertices);

                        for (int i = 0; i < vertices.Length; i++)
                        {
                            vertices[i] = (vertices[i] - currentCenter) * scale + originalCenter;
                        }
                    }
                }
            }

            smoothedMesh.vertices = vertices;
            smoothedMesh.RecalculateNormals();
            smoothedMesh.RecalculateBounds();

            if (generateTangents)
                smoothedMesh.RecalculateTangents();

            return smoothedMesh;
        }

        /// <summary>
        /// Apply Taubin smoothing (better volume preservation)
        /// </summary>
        public Mesh ApplyTaubinSmoothing(Mesh inputMesh, int iterations = -1, float lambda = 0.5f, float mu = -0.53f)
        {
            if (iterations < 0) iterations = defaultSmoothingIterations;

            Mesh smoothedMesh = Instantiate(inputMesh);
            smoothedMesh.name = inputMesh.name + "_TaubinSmoothed";

            Vector3[] vertices = smoothedMesh.vertices;
            int[] triangles = smoothedMesh.triangles;

            Dictionary<int, HashSet<int>> adjacency = BuildAdjacencyList(triangles, vertices.Length);

            for (int iter = 0; iter < iterations; iter++)
            {
                // Forward pass (shrinking)
                vertices = SmoothPass(vertices, adjacency, lambda);

                // Backward pass (expanding)
                vertices = SmoothPass(vertices, adjacency, mu);
            }

            smoothedMesh.vertices = vertices;
            smoothedMesh.RecalculateNormals();
            smoothedMesh.RecalculateBounds();

            if (generateTangents)
                smoothedMesh.RecalculateTangents();

            return smoothedMesh;
        }

        private Vector3[] SmoothPass(Vector3[] vertices, Dictionary<int, HashSet<int>> adjacency, float factor)
        {
            Vector3[] newVertices = new Vector3[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                if (adjacency[i].Count == 0)
                {
                    newVertices[i] = vertices[i];
                    continue;
                }

                Vector3 avg = Vector3.zero;
                foreach (int neighbor in adjacency[i])
                {
                    avg += vertices[neighbor];
                }
                avg /= adjacency[i].Count;

                newVertices[i] = vertices[i] + (avg - vertices[i]) * factor;
            }

            return newVertices;
        }

        /// <summary>
        /// Apply HC (Humphrey's Classes) smoothing (best for medical meshes)
        /// </summary>
        public Mesh ApplyHCSmoothing(Mesh inputMesh, int iterations = -1, float alpha = 0.5f, float beta = 0.5f)
        {
            if (iterations < 0) iterations = defaultSmoothingIterations;

            Mesh smoothedMesh = Instantiate(inputMesh);
            smoothedMesh.name = inputMesh.name + "_HCSmoothed";

            Vector3[] originalVertices = inputMesh.vertices;
            Vector3[] vertices = smoothedMesh.vertices;
            int[] triangles = smoothedMesh.triangles;

            Dictionary<int, HashSet<int>> adjacency = BuildAdjacencyList(triangles, vertices.Length);

            for (int iter = 0; iter < iterations; iter++)
            {
                // Step 1: Laplacian smoothing to get P
                Vector3[] p = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (adjacency[i].Count == 0)
                    {
                        p[i] = vertices[i];
                        continue;
                    }

                    Vector3 avg = Vector3.zero;
                    foreach (int neighbor in adjacency[i])
                    {
                        avg += vertices[neighbor];
                    }
                    p[i] = avg / adjacency[i].Count;
                }

                // Step 2: Calculate difference vectors B
                Vector3[] b = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    b[i] = p[i] - (alpha * originalVertices[i] + (1 - alpha) * vertices[i]);
                }

                // Step 3: Smooth the differences
                Vector3[] newVertices = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    if (adjacency[i].Count == 0)
                    {
                        newVertices[i] = p[i];
                        continue;
                    }

                    Vector3 avgB = Vector3.zero;
                    foreach (int neighbor in adjacency[i])
                    {
                        avgB += b[neighbor];
                    }
                    avgB /= adjacency[i].Count;

                    newVertices[i] = p[i] - (beta * b[i] + (1 - beta) * avgB);
                }

                vertices = newVertices;
            }

            smoothedMesh.vertices = vertices;
            smoothedMesh.RecalculateNormals();
            smoothedMesh.RecalculateBounds();

            if (generateTangents)
                smoothedMesh.RecalculateTangents();

            return smoothedMesh;
        }

        #endregion

        #region Normal Enhancement

        /// <summary>
        /// Smooth normals with area-weighted averaging
        /// </summary>
        public Mesh SmoothNormals(Mesh inputMesh, float angleThreshold = -1)
        {
            if (angleThreshold < 0) angleThreshold = normalSmoothingAngle;

            Mesh enhancedMesh = Instantiate(inputMesh);
            enhancedMesh.name = inputMesh.name + "_SmoothNormals";

            Vector3[] vertices = enhancedMesh.vertices;
            Vector3[] normals = new Vector3[vertices.Length];
            int[] triangles = enhancedMesh.triangles;

            // Calculate face normals and areas
            int faceCount = triangles.Length / 3;
            Vector3[] faceNormals = new Vector3[faceCount];
            float[] faceAreas = new float[faceCount];

            for (int i = 0; i < faceCount; i++)
            {
                int i0 = triangles[i * 3];
                int i1 = triangles[i * 3 + 1];
                int i2 = triangles[i * 3 + 2];

                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 cross = Vector3.Cross(edge1, edge2);

                faceAreas[i] = cross.magnitude * 0.5f;
                faceNormals[i] = cross.normalized;
            }

            // Build vertex to face mapping
            Dictionary<int, List<int>> vertexFaces = new Dictionary<int, List<int>>();
            for (int i = 0; i < vertices.Length; i++)
                vertexFaces[i] = new List<int>();

            for (int i = 0; i < faceCount; i++)
            {
                vertexFaces[triangles[i * 3]].Add(i);
                vertexFaces[triangles[i * 3 + 1]].Add(i);
                vertexFaces[triangles[i * 3 + 2]].Add(i);
            }

            // Calculate smooth normals
            float cosThreshold = Mathf.Cos(angleThreshold * Mathf.Deg2Rad);

            for (int v = 0; v < vertices.Length; v++)
            {
                Vector3 smoothNormal = Vector3.zero;
                List<int> faces = vertexFaces[v];

                if (faces.Count == 0)
                {
                    normals[v] = Vector3.up;
                    continue;
                }

                // Use first face's normal as reference
                Vector3 refNormal = faceNormals[faces[0]];

                foreach (int faceIndex in faces)
                {
                    Vector3 faceNormal = faceNormals[faceIndex];

                    // Check angle threshold
                    if (Vector3.Dot(refNormal, faceNormal) >= cosThreshold)
                    {
                        float weight = weightByArea ? faceAreas[faceIndex] : 1f;
                        smoothNormal += faceNormal * weight;
                    }
                }

                normals[v] = smoothNormal.normalized;
            }

            enhancedMesh.normals = normals;

            if (generateTangents)
                enhancedMesh.RecalculateTangents();

            return enhancedMesh;
        }

        /// <summary>
        /// Apply curvature-based normal enhancement
        /// </summary>
        public Mesh EnhanceNormalsByCurvature(Mesh inputMesh, float curvatureInfluence = 0.5f)
        {
            Mesh enhancedMesh = Instantiate(inputMesh);
            Vector3[] vertices = enhancedMesh.vertices;
            Vector3[] normals = enhancedMesh.normals;
            int[] triangles = enhancedMesh.triangles;

            Dictionary<int, HashSet<int>> adjacency = BuildAdjacencyList(triangles, vertices.Length);

            // Calculate per-vertex curvature
            float[] curvatures = new float[vertices.Length];
            float maxCurvature = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (adjacency[i].Count < 2) continue;

                // Estimate curvature from neighbor positions
                float curvature = 0;
                Vector3 pos = vertices[i];
                Vector3 normal = normals[i];

                foreach (int neighbor in adjacency[i])
                {
                    Vector3 toNeighbor = vertices[neighbor] - pos;
                    float dist = toNeighbor.magnitude;
                    if (dist > 0)
                    {
                        float heightDiff = Vector3.Dot(toNeighbor, normal);
                        curvature += Mathf.Abs(heightDiff) / (dist * dist);
                    }
                }
                curvature /= adjacency[i].Count;
                curvatures[i] = curvature;
                maxCurvature = Mathf.Max(maxCurvature, curvature);
            }

            // Normalize curvatures and adjust normals
            if (maxCurvature > 0)
            {
                for (int i = 0; i < vertices.Length; i++)
                {
                    float normalizedCurvature = curvatures[i] / maxCurvature;

                    // In high curvature areas, keep original normal
                    // In low curvature areas, smooth more aggressively
                    float smoothFactor = 1f - normalizedCurvature * curvatureInfluence;

                    if (smoothFactor < 1f && adjacency[i].Count > 0)
                    {
                        Vector3 avgNormal = Vector3.zero;
                        foreach (int neighbor in adjacency[i])
                        {
                            avgNormal += normals[neighbor];
                        }
                        avgNormal /= adjacency[i].Count;

                        normals[i] = Vector3.Lerp(avgNormal, normals[i], smoothFactor).normalized;
                    }
                }
            }

            enhancedMesh.normals = normals;

            if (generateTangents)
                enhancedMesh.RecalculateTangents();

            return enhancedMesh;
        }

        #endregion

        #region Mesh Subdivision

        /// <summary>
        /// Subdivide mesh for smoother surface
        /// </summary>
        public Mesh SubdivideMesh(Mesh inputMesh, int subdivisions = 1)
        {
            Mesh subdividedMesh = Instantiate(inputMesh);

            for (int i = 0; i < subdivisions; i++)
            {
                subdividedMesh = SubdivideOnce(subdividedMesh);
            }

            subdividedMesh.name = inputMesh.name + "_Subdivided";
            return subdividedMesh;
        }

        private Mesh SubdivideOnce(Mesh inputMesh)
        {
            Vector3[] vertices = inputMesh.vertices;
            Vector3[] normals = inputMesh.normals;
            Vector2[] uvs = inputMesh.uv;
            int[] triangles = inputMesh.triangles;

            // Edge midpoint cache
            Dictionary<long, int> edgeMidpoints = new Dictionary<long, int>();

            List<Vector3> newVertices = new List<Vector3>(vertices);
            List<Vector3> newNormals = new List<Vector3>(normals);
            List<Vector2> newUVs = new List<Vector2>(uvs);
            List<int> newTriangles = new List<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                // Get or create midpoints
                int m01 = GetOrCreateMidpoint(i0, i1, newVertices, newNormals, newUVs, vertices, normals, uvs, edgeMidpoints);
                int m12 = GetOrCreateMidpoint(i1, i2, newVertices, newNormals, newUVs, vertices, normals, uvs, edgeMidpoints);
                int m20 = GetOrCreateMidpoint(i2, i0, newVertices, newNormals, newUVs, vertices, normals, uvs, edgeMidpoints);

                // Create 4 triangles from 1
                newTriangles.AddRange(new[] { i0, m01, m20 });
                newTriangles.AddRange(new[] { i1, m12, m01 });
                newTriangles.AddRange(new[] { i2, m20, m12 });
                newTriangles.AddRange(new[] { m01, m12, m20 });
            }

            Mesh newMesh = new Mesh();
            newMesh.indexFormat = newVertices.Count > 65535 ?
                UnityEngine.Rendering.IndexFormat.UInt32 :
                UnityEngine.Rendering.IndexFormat.UInt16;

            newMesh.vertices = newVertices.ToArray();
            newMesh.normals = newNormals.ToArray();
            newMesh.uv = newUVs.ToArray();
            newMesh.triangles = newTriangles.ToArray();
            newMesh.RecalculateBounds();

            return newMesh;
        }

        private int GetOrCreateMidpoint(int i0, int i1, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
            Vector3[] origVerts, Vector3[] origNormals, Vector2[] origUVs, Dictionary<long, int> cache)
        {
            long key = i0 < i1 ? ((long)i0 << 32) | (uint)i1 : ((long)i1 << 32) | (uint)i0;

            if (cache.TryGetValue(key, out int index))
                return index;

            Vector3 midpoint = (origVerts[i0] + origVerts[i1]) * 0.5f;
            Vector3 midNormal = ((origNormals != null && origNormals.Length > i0 && origNormals.Length > i1) ?
                (origNormals[i0] + origNormals[i1]).normalized : Vector3.up);
            Vector2 midUV = (origUVs != null && origUVs.Length > i0 && origUVs.Length > i1) ?
                (origUVs[i0] + origUVs[i1]) * 0.5f : Vector2.zero;

            int newIndex = vertices.Count;
            vertices.Add(midpoint);
            normals.Add(midNormal);
            uvs.Add(midUV);

            cache[key] = newIndex;
            return newIndex;
        }

        #endregion

        #region Mesh Decimation (Simplification)

        /// <summary>
        /// Simplify mesh to target triangle count
        /// </summary>
        public Mesh DecimateMesh(Mesh inputMesh, float targetRatio = 0.5f)
        {
            // Simple edge collapse decimation
            Mesh decimatedMesh = Instantiate(inputMesh);
            decimatedMesh.name = inputMesh.name + "_Decimated";

            Vector3[] vertices = decimatedMesh.vertices;
            int[] triangles = decimatedMesh.triangles;

            int targetTriCount = Mathf.Max(4, (int)(triangles.Length / 3 * targetRatio));
            int currentTriCount = triangles.Length / 3;

            // This is a simplified decimation - for production use a proper algorithm
            // like Quadric Error Metrics (QEM)

            if (targetRatio < 1.0f)
            {
                // For now, just return the mesh with smoothed normals
                // Full decimation would require more complex implementation
                Debug.LogWarning("Full mesh decimation not implemented - returning smoothed mesh");
            }

            decimatedMesh.vertices = vertices;
            decimatedMesh.triangles = triangles;
            decimatedMesh.RecalculateNormals();
            decimatedMesh.RecalculateBounds();

            return decimatedMesh;
        }

        #endregion

        #region UV Generation

        /// <summary>
        /// Generate simple UV coordinates based on projection
        /// </summary>
        public Mesh GenerateUVs(Mesh inputMesh, UVProjection projection = UVProjection.Box)
        {
            Mesh uvMesh = Instantiate(inputMesh);
            uvMesh.name = inputMesh.name + "_UV";

            Vector3[] vertices = uvMesh.vertices;
            Vector3[] normals = uvMesh.normals;
            Vector2[] uvs = new Vector2[vertices.Length];

            Bounds bounds = uvMesh.bounds;
            Vector3 size = bounds.size;
            Vector3 min = bounds.min;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = vertices[i];
                Vector3 n = normals[i];

                switch (projection)
                {
                    case UVProjection.Box:
                        // Choose projection based on dominant normal component
                        float absX = Mathf.Abs(n.x);
                        float absY = Mathf.Abs(n.y);
                        float absZ = Mathf.Abs(n.z);

                        if (absX >= absY && absX >= absZ)
                        {
                            uvs[i] = new Vector2((v.z - min.z) / size.z, (v.y - min.y) / size.y);
                        }
                        else if (absY >= absX && absY >= absZ)
                        {
                            uvs[i] = new Vector2((v.x - min.x) / size.x, (v.z - min.z) / size.z);
                        }
                        else
                        {
                            uvs[i] = new Vector2((v.x - min.x) / size.x, (v.y - min.y) / size.y);
                        }
                        break;

                    case UVProjection.Spherical:
                        Vector3 centered = v - bounds.center;
                        centered.Normalize();
                        uvs[i] = new Vector2(
                            0.5f + Mathf.Atan2(centered.z, centered.x) / (2f * Mathf.PI),
                            0.5f - Mathf.Asin(centered.y) / Mathf.PI
                        );
                        break;

                    case UVProjection.Cylindrical:
                        Vector3 centeredCyl = v - bounds.center;
                        uvs[i] = new Vector2(
                            0.5f + Mathf.Atan2(centeredCyl.z, centeredCyl.x) / (2f * Mathf.PI),
                            (v.y - min.y) / size.y
                        );
                        break;

                    case UVProjection.Planar:
                        uvs[i] = new Vector2((v.x - min.x) / size.x, (v.y - min.y) / size.y);
                        break;
                }
            }

            uvMesh.uv = uvs;
            return uvMesh;
        }

        public enum UVProjection
        {
            Box,
            Spherical,
            Cylindrical,
            Planar
        }

        #endregion

        #region Complete Enhancement Pipeline

        /// <summary>
        /// Apply complete enhancement pipeline to mesh
        /// </summary>
        public Mesh EnhanceMesh(Mesh inputMesh, EnhancementSettings settings = null)
        {
            if (settings == null)
                settings = new EnhancementSettings();

            Mesh result = inputMesh;

            // Step 1: Smoothing
            if (settings.smoothing)
            {
                switch (settings.smoothingMethod)
                {
                    case SmoothingMethod.Laplacian:
                        result = ApplyLaplacianSmoothing(result, settings.smoothingIterations, settings.smoothingStrength);
                        break;
                    case SmoothingMethod.Taubin:
                        result = ApplyTaubinSmoothing(result, settings.smoothingIterations);
                        break;
                    case SmoothingMethod.HC:
                        result = ApplyHCSmoothing(result, settings.smoothingIterations);
                        break;
                }
            }

            // Step 2: Subdivision (if needed for more detail)
            if (settings.subdivide && settings.subdivisionLevels > 0)
            {
                result = SubdivideMesh(result, settings.subdivisionLevels);
            }

            // Step 3: Normal enhancement
            if (settings.enhanceNormals)
            {
                result = SmoothNormals(result, settings.normalAngleThreshold);

                if (settings.curvatureNormals)
                {
                    result = EnhanceNormalsByCurvature(result, settings.curvatureInfluence);
                }
            }

            // Step 4: Generate UVs
            if (settings.generateUVs)
            {
                result = GenerateUVs(result, settings.uvProjection);
            }

            // Step 5: Optimize
            if (optimizeMesh)
            {
                result.Optimize();
            }

            result.name = inputMesh.name + "_Enhanced";
            return result;
        }

        [System.Serializable]
        public class EnhancementSettings
        {
            public bool smoothing = true;
            public SmoothingMethod smoothingMethod = SmoothingMethod.HC;
            public int smoothingIterations = 3;
            public float smoothingStrength = 0.5f;

            public bool subdivide = false;
            public int subdivisionLevels = 1;

            public bool enhanceNormals = true;
            public float normalAngleThreshold = 60f;
            public bool curvatureNormals = true;
            public float curvatureInfluence = 0.5f;

            public bool generateUVs = true;
            public UVProjection uvProjection = UVProjection.Box;
        }

        public enum SmoothingMethod
        {
            Laplacian,
            Taubin,
            HC
        }

        #endregion

        #region Utility Methods

        private Dictionary<int, HashSet<int>> BuildAdjacencyList(int[] triangles, int vertexCount)
        {
            Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < vertexCount; i++)
                adjacency[i] = new HashSet<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int v0 = triangles[i];
                int v1 = triangles[i + 1];
                int v2 = triangles[i + 2];

                adjacency[v0].Add(v1); adjacency[v0].Add(v2);
                adjacency[v1].Add(v0); adjacency[v1].Add(v2);
                adjacency[v2].Add(v0); adjacency[v2].Add(v1);
            }

            return adjacency;
        }

        private float CalculateMeshVolume(Vector3[] vertices, int[] triangles)
        {
            float volume = 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                volume += Vector3.Dot(v0, Vector3.Cross(v1, v2)) / 6f;
            }

            return Mathf.Abs(volume);
        }

        private Vector3 CalculateMeshCenter(Vector3[] vertices)
        {
            Vector3 center = Vector3.zero;
            foreach (Vector3 v in vertices)
                center += v;
            return center / vertices.Length;
        }

        #endregion
    }
}

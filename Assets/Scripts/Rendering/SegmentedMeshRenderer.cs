using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CardiacVR.Rendering
{
    /// <summary>
    /// Advanced renderer for segmented medical meshes
    /// Provides beautiful, smooth visualization with professional lighting
    /// </summary>
    public class SegmentedMeshRenderer : MonoBehaviour
    {
        public static SegmentedMeshRenderer Instance { get; private set; }

        [Header("Quality Settings")]
        [SerializeField] private QualityLevel qualityLevel = QualityLevel.High;
        [SerializeField] private bool enableSmoothNormals = true;
        [SerializeField] private bool enableAmbientOcclusion = true;
        [SerializeField] private bool enableSubsurfaceScattering = true;

        [Header("Visual Settings")]
        [SerializeField] private float globalSmoothness = 0.6f;
        [SerializeField] private float subsurfaceIntensity = 0.5f;
        [SerializeField] private float aoIntensity = 0.3f;
        [SerializeField] private float rimLightIntensity = 0.4f;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float highlightPulseDuration = 1f;

        [Header("References")]
        [SerializeField] private Material baseTissueMaterial;
        [SerializeField] private Material transparentTissueMaterial;
        [SerializeField] private Material outlineMaterial;

        public enum QualityLevel
        {
            Low,        // Mobile VR
            Medium,     // Standalone VR
            High,       // PC VR
            Ultra       // Desktop/Presentation
        }

        // Registered meshes
        private Dictionary<string, SegmentedMeshData> registeredMeshes = new Dictionary<string, SegmentedMeshData>();
        private Dictionary<string, Material> materialInstances = new Dictionary<string, Material>();

        [System.Serializable]
        public class SegmentedMeshData
        {
            public string id;
            public string structureName;
            public GameObject meshObject;
            public MeshFilter meshFilter;
            public MeshRenderer meshRenderer;
            public Mesh originalMesh;
            public Mesh smoothedMesh;
            public Material material;
            public AnatomicalCategory category;
            public Color baseColor;
            public float opacity = 1f;
            public bool isVisible = true;
            public bool isHighlighted = false;
            public bool isSelected = false;
        }

        public enum AnatomicalCategory
        {
            Heart_Chamber,
            Heart_Valve,
            Heart_Wall,
            Heart_Vessel,
            Great_Vessel,
            Coronary,
            Lung,
            Bone,
            Soft_Tissue,
            Pathology,
            Device,
            Other
        }

        // Category color schemes (beautiful medical colors)
        private static readonly Dictionary<AnatomicalCategory, Color> CategoryColors = new Dictionary<AnatomicalCategory, Color>
        {
            { AnatomicalCategory.Heart_Chamber, new Color(0.85f, 0.25f, 0.22f, 1f) },
            { AnatomicalCategory.Heart_Valve, new Color(0.95f, 0.88f, 0.82f, 0.9f) },
            { AnatomicalCategory.Heart_Wall, new Color(0.78f, 0.28f, 0.25f, 1f) },
            { AnatomicalCategory.Heart_Vessel, new Color(0.72f, 0.18f, 0.15f, 1f) },
            { AnatomicalCategory.Great_Vessel, new Color(0.88f, 0.22f, 0.18f, 1f) },
            { AnatomicalCategory.Coronary, new Color(0.95f, 0.35f, 0.28f, 1f) },
            { AnatomicalCategory.Lung, new Color(0.92f, 0.78f, 0.8f, 0.85f) },
            { AnatomicalCategory.Bone, new Color(0.96f, 0.94f, 0.88f, 1f) },
            { AnatomicalCategory.Soft_Tissue, new Color(0.95f, 0.85f, 0.75f, 1f) },
            { AnatomicalCategory.Pathology, new Color(0.45f, 0.35f, 0.55f, 1f) },
            { AnatomicalCategory.Device, new Color(0.75f, 0.78f, 0.82f, 1f) },
            { AnatomicalCategory.Other, new Color(0.7f, 0.7f, 0.7f, 1f) }
        };

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

        private void Start()
        {
            InitializeMaterials();
        }

        private void InitializeMaterials()
        {
            if (baseTissueMaterial == null)
            {
                baseTissueMaterial = CreateBaseTissueMaterial();
            }

            if (transparentTissueMaterial == null)
            {
                transparentTissueMaterial = CreateTransparentTissueMaterial();
            }

            if (outlineMaterial == null)
            {
                outlineMaterial = CreateOutlineMaterial();
            }
        }

        #region Material Creation

        private Material CreateBaseTissueMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(shader);
            mat.name = "Base Tissue Material";

            mat.SetFloat("_Smoothness", globalSmoothness);
            mat.SetFloat("_Metallic", 0f);

            return mat;
        }

        private Material CreateTransparentTissueMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(shader);
            mat.name = "Transparent Tissue Material";

            // Setup for transparency
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetFloat("_Smoothness", globalSmoothness);

            return mat;
        }

        private Material CreateOutlineMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            Material mat = new Material(shader);
            mat.name = "Outline Material";
            mat.SetColor("_BaseColor", new Color(1f, 0.9f, 0.3f, 1f));

            return mat;
        }

        public Material CreateMaterialForCategory(AnatomicalCategory category, float opacity = 1f)
        {
            Color baseColor = CategoryColors[category];
            baseColor.a = opacity;

            Material baseMat = opacity < 1f ? transparentTissueMaterial : baseTissueMaterial;
            Material mat = new Material(baseMat);
            mat.SetColor("_BaseColor", baseColor);

            // Category-specific settings
            ConfigureMaterialForCategory(mat, category);

            return mat;
        }

        private void ConfigureMaterialForCategory(Material mat, AnatomicalCategory category)
        {
            switch (category)
            {
                case AnatomicalCategory.Heart_Chamber:
                case AnatomicalCategory.Heart_Wall:
                    mat.SetFloat("_Smoothness", 0.4f);
                    break;

                case AnatomicalCategory.Heart_Valve:
                    mat.SetFloat("_Smoothness", 0.65f);
                    // Slight translucency for valves
                    if (mat.HasProperty("_Surface"))
                        mat.SetFloat("_Surface", 1);
                    break;

                case AnatomicalCategory.Great_Vessel:
                case AnatomicalCategory.Heart_Vessel:
                case AnatomicalCategory.Coronary:
                    mat.SetFloat("_Smoothness", 0.55f);
                    break;

                case AnatomicalCategory.Lung:
                    mat.SetFloat("_Smoothness", 0.3f);
                    break;

                case AnatomicalCategory.Bone:
                    mat.SetFloat("_Smoothness", 0.25f);
                    mat.SetFloat("_Metallic", 0.1f);
                    break;

                case AnatomicalCategory.Device:
                    mat.SetFloat("_Smoothness", 0.85f);
                    mat.SetFloat("_Metallic", 0.9f);
                    break;

                case AnatomicalCategory.Pathology:
                    mat.SetFloat("_Smoothness", 0.35f);
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", CategoryColors[category] * 0.1f);
                    break;
            }
        }

        #endregion

        #region Mesh Registration

        public string RegisterMesh(GameObject meshObject, string structureName, AnatomicalCategory category)
        {
            if (meshObject == null) return null;

            MeshFilter meshFilter = meshObject.GetComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();

            if (meshFilter == null || meshRenderer == null)
            {
                Debug.LogWarning($"Object {meshObject.name} does not have required mesh components");
                return null;
            }

            string id = System.Guid.NewGuid().ToString();

            // Create mesh data
            SegmentedMeshData data = new SegmentedMeshData
            {
                id = id,
                structureName = structureName,
                meshObject = meshObject,
                meshFilter = meshFilter,
                meshRenderer = meshRenderer,
                originalMesh = meshFilter.sharedMesh,
                category = category,
                baseColor = CategoryColors[category],
                opacity = 1f,
                isVisible = true
            };

            // Create and apply material
            data.material = CreateMaterialForCategory(category);
            meshRenderer.material = data.material;

            // Store material instance
            materialInstances[id] = data.material;

            // Smooth the mesh if enabled
            if (enableSmoothNormals && qualityLevel >= QualityLevel.Medium)
            {
                data.smoothedMesh = SmoothMeshNormals(data.originalMesh);
                meshFilter.mesh = data.smoothedMesh;
            }

            // Register
            registeredMeshes[id] = data;

            // Animate appearance
            StartCoroutine(AnimateMeshAppearance(data));

            return id;
        }

        public void RegisterMultipleMeshes(List<GameObject> meshObjects, List<string> names, List<AnatomicalCategory> categories)
        {
            StartCoroutine(RegisterMeshesSequentially(meshObjects, names, categories));
        }

        private IEnumerator RegisterMeshesSequentially(List<GameObject> meshObjects, List<string> names, List<AnatomicalCategory> categories)
        {
            for (int i = 0; i < meshObjects.Count; i++)
            {
                string name = i < names.Count ? names[i] : meshObjects[i].name;
                AnatomicalCategory cat = i < categories.Count ? categories[i] : AnatomicalCategory.Other;

                RegisterMesh(meshObjects[i], name, cat);
                yield return new WaitForSeconds(0.05f); // Stagger for visual effect
            }
        }

        public void UnregisterMesh(string id)
        {
            if (registeredMeshes.TryGetValue(id, out SegmentedMeshData data))
            {
                if (data.smoothedMesh != null && data.smoothedMesh != data.originalMesh)
                {
                    Destroy(data.smoothedMesh);
                }

                if (materialInstances.TryGetValue(id, out Material mat))
                {
                    Destroy(mat);
                    materialInstances.Remove(id);
                }

                registeredMeshes.Remove(id);
            }
        }

        #endregion

        #region Mesh Smoothing

        private Mesh SmoothMeshNormals(Mesh originalMesh)
        {
            Mesh smoothedMesh = Instantiate(originalMesh);
            smoothedMesh.name = originalMesh.name + "_Smooth";

            Vector3[] vertices = smoothedMesh.vertices;
            Vector3[] normals = new Vector3[vertices.Length];
            int[] triangles = smoothedMesh.triangles;

            // Build vertex to triangle mapping
            Dictionary<int, List<int>> vertexTriangles = new Dictionary<int, List<int>>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int triIndex = i / 3;
                for (int j = 0; j < 3; j++)
                {
                    int vertIndex = triangles[i + j];
                    if (!vertexTriangles.ContainsKey(vertIndex))
                        vertexTriangles[vertIndex] = new List<int>();
                    vertexTriangles[vertIndex].Add(triIndex);
                }
            }

            // Calculate face normals
            Vector3[] faceNormals = new Vector3[triangles.Length / 3];
            float[] faceAreas = new float[triangles.Length / 3];

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                Vector3 cross = Vector3.Cross(edge1, edge2);

                int faceIndex = i / 3;
                faceAreas[faceIndex] = cross.magnitude * 0.5f;
                faceNormals[faceIndex] = cross.normalized;
            }

            // Average normals weighted by face area
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 avgNormal = Vector3.zero;
                float totalWeight = 0f;

                if (vertexTriangles.TryGetValue(i, out List<int> tris))
                {
                    foreach (int triIndex in tris)
                    {
                        float weight = faceAreas[triIndex];
                        avgNormal += faceNormals[triIndex] * weight;
                        totalWeight += weight;
                    }
                }

                normals[i] = totalWeight > 0 ? (avgNormal / totalWeight).normalized : Vector3.up;
            }

            smoothedMesh.normals = normals;

            // Recalculate tangents for better lighting
            smoothedMesh.RecalculateTangents();

            return smoothedMesh;
        }

        public void ApplyLaplacianSmoothing(string meshId, int iterations = 1, float strength = 0.5f)
        {
            if (!registeredMeshes.TryGetValue(meshId, out SegmentedMeshData data)) return;

            Mesh mesh = data.meshFilter.mesh;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Build adjacency
            Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();
            for (int i = 0; i < vertices.Length; i++)
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

                    Vector3 avg = Vector3.zero;
                    foreach (int neighbor in adjacency[i])
                    {
                        avg += vertices[neighbor];
                    }
                    avg /= adjacency[i].Count;

                    newVertices[i] = Vector3.Lerp(vertices[i], avg, strength);
                }

                vertices = newVertices;
            }

            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            // Re-smooth normals
            if (enableSmoothNormals)
            {
                Mesh smoothed = SmoothMeshNormals(mesh);
                data.meshFilter.mesh = smoothed;
                data.smoothedMesh = smoothed;
            }
        }

        #endregion

        #region Visibility & Appearance

        public void SetMeshVisibility(string meshId, bool visible, bool animated = true)
        {
            if (!registeredMeshes.TryGetValue(meshId, out SegmentedMeshData data)) return;

            data.isVisible = visible;

            if (animated)
            {
                StartCoroutine(AnimateVisibility(data, visible));
            }
            else
            {
                data.meshObject.SetActive(visible);
            }
        }

        public void SetMeshOpacity(string meshId, float opacity, bool animated = true)
        {
            if (!registeredMeshes.TryGetValue(meshId, out SegmentedMeshData data)) return;

            data.opacity = opacity;

            if (animated)
            {
                StartCoroutine(AnimateOpacity(data, opacity));
            }
            else
            {
                SetMaterialOpacity(data.material, opacity);
            }
        }

        public void SetMeshColor(string meshId, Color color, bool animated = true)
        {
            if (!registeredMeshes.TryGetValue(meshId, out SegmentedMeshData data)) return;

            data.baseColor = color;

            if (animated)
            {
                StartCoroutine(AnimateColor(data, color));
            }
            else
            {
                data.material.SetColor("_BaseColor", color);
            }
        }

        private void SetMaterialOpacity(Material mat, float opacity)
        {
            Color color = mat.GetColor("_BaseColor");
            color.a = opacity;
            mat.SetColor("_BaseColor", color);

            // Switch render mode based on opacity
            if (opacity < 1f)
            {
                mat.SetFloat("_Surface", 1);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                mat.SetFloat("_Surface", 0);
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.renderQueue = (int)RenderQueue.Geometry;
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }

        #endregion

        #region Selection & Highlighting

        public void HighlightMesh(string meshId, bool highlight)
        {
            if (!registeredMeshes.TryGetValue(meshId, out SegmentedMeshData data)) return;

            data.isHighlighted = highlight;

            if (highlight)
            {
                StartCoroutine(PulseHighlight(data));
            }
            else
            {
                // Restore original color
                data.material.SetColor("_BaseColor", data.baseColor);
                data.material.DisableKeyword("_EMISSION");
            }
        }

        public void SelectMesh(string meshId, bool selected)
        {
            if (!registeredMeshes.TryGetValue(meshId, out SegmentedMeshData data)) return;

            data.isSelected = selected;

            if (selected)
            {
                // Add outline effect
                EnableOutline(data);
                // Slight emission
                data.material.EnableKeyword("_EMISSION");
                data.material.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.4f, 1f) * 0.2f);
            }
            else
            {
                DisableOutline(data);
                if (!data.isHighlighted)
                {
                    data.material.DisableKeyword("_EMISSION");
                }
            }
        }

        private void EnableOutline(SegmentedMeshData data)
        {
            // Add outline renderer if not present
            Transform outlineTransform = data.meshObject.transform.Find("Outline");
            if (outlineTransform == null)
            {
                GameObject outline = new GameObject("Outline");
                outline.transform.SetParent(data.meshObject.transform, false);
                outline.transform.localPosition = Vector3.zero;
                outline.transform.localRotation = Quaternion.identity;
                outline.transform.localScale = Vector3.one * 1.02f;

                MeshFilter outlineMF = outline.AddComponent<MeshFilter>();
                outlineMF.mesh = data.meshFilter.sharedMesh;

                MeshRenderer outlineMR = outline.AddComponent<MeshRenderer>();
                outlineMR.material = outlineMaterial;
                outlineMR.shadowCastingMode = ShadowCastingMode.Off;
                outlineMR.receiveShadows = false;

                // Invert normals for outline effect
                // This is handled by shader cull front
            }
            else
            {
                outlineTransform.gameObject.SetActive(true);
            }
        }

        private void DisableOutline(SegmentedMeshData data)
        {
            Transform outlineTransform = data.meshObject.transform.Find("Outline");
            if (outlineTransform != null)
            {
                outlineTransform.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Animations

        private IEnumerator AnimateMeshAppearance(SegmentedMeshData data)
        {
            // Start invisible and scaled down
            data.meshObject.transform.localScale = Vector3.one * 0.8f;
            SetMaterialOpacity(data.material, 0f);

            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;

                // Ease out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                data.meshObject.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, targetScale, eased);
                SetMaterialOpacity(data.material, Mathf.Lerp(0f, data.opacity, eased));

                yield return null;
            }

            data.meshObject.transform.localScale = targetScale;
            SetMaterialOpacity(data.material, data.opacity);
        }

        private IEnumerator AnimateVisibility(SegmentedMeshData data, bool visible)
        {
            float startOpacity = visible ? 0f : data.opacity;
            float targetOpacity = visible ? data.opacity : 0f;

            if (visible)
            {
                data.meshObject.SetActive(true);
            }

            float elapsed = 0f;
            float duration = fadeInDuration * 0.7f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float current = Mathf.Lerp(startOpacity, targetOpacity, t);
                SetMaterialOpacity(data.material, current);
                yield return null;
            }

            SetMaterialOpacity(data.material, targetOpacity);

            if (!visible)
            {
                data.meshObject.SetActive(false);
            }
        }

        private IEnumerator AnimateOpacity(SegmentedMeshData data, float targetOpacity)
        {
            Color currentColor = data.material.GetColor("_BaseColor");
            float startOpacity = currentColor.a;

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float current = Mathf.Lerp(startOpacity, targetOpacity, t);
                SetMaterialOpacity(data.material, current);
                yield return null;
            }

            SetMaterialOpacity(data.material, targetOpacity);
        }

        private IEnumerator AnimateColor(SegmentedMeshData data, Color targetColor)
        {
            Color startColor = data.material.GetColor("_BaseColor");

            float elapsed = 0f;
            float duration = 0.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Color current = Color.Lerp(startColor, targetColor, t);
                data.material.SetColor("_BaseColor", current);
                yield return null;
            }

            data.material.SetColor("_BaseColor", targetColor);
        }

        private IEnumerator PulseHighlight(SegmentedMeshData data)
        {
            Color baseColor = data.baseColor;
            Color highlightColor = Color.Lerp(baseColor, Color.white, 0.3f);

            data.material.EnableKeyword("_EMISSION");

            while (data.isHighlighted)
            {
                float elapsed = 0f;

                // Pulse up
                while (elapsed < highlightPulseDuration * 0.5f && data.isHighlighted)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (highlightPulseDuration * 0.5f);
                    float intensity = Mathf.Sin(t * Mathf.PI * 0.5f);

                    data.material.SetColor("_BaseColor", Color.Lerp(baseColor, highlightColor, intensity));
                    data.material.SetColor("_EmissionColor", highlightColor * intensity * 0.15f);

                    yield return null;
                }

                elapsed = 0f;

                // Pulse down
                while (elapsed < highlightPulseDuration * 0.5f && data.isHighlighted)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / (highlightPulseDuration * 0.5f);
                    float intensity = 1f - Mathf.Sin(t * Mathf.PI * 0.5f);

                    data.material.SetColor("_BaseColor", Color.Lerp(baseColor, highlightColor, intensity));
                    data.material.SetColor("_EmissionColor", highlightColor * intensity * 0.15f);

                    yield return null;
                }
            }

            data.material.SetColor("_BaseColor", baseColor);
            data.material.DisableKeyword("_EMISSION");
        }

        #endregion

        #region Batch Operations

        public void SetAllMeshesVisibility(bool visible, bool animated = true)
        {
            foreach (var kvp in registeredMeshes)
            {
                SetMeshVisibility(kvp.Key, visible, animated);
            }
        }

        public void SetCategoryVisibility(AnatomicalCategory category, bool visible, bool animated = true)
        {
            foreach (var kvp in registeredMeshes)
            {
                if (kvp.Value.category == category)
                {
                    SetMeshVisibility(kvp.Key, visible, animated);
                }
            }
        }

        public void SetCategoryOpacity(AnatomicalCategory category, float opacity, bool animated = true)
        {
            foreach (var kvp in registeredMeshes)
            {
                if (kvp.Value.category == category)
                {
                    SetMeshOpacity(kvp.Key, opacity, animated);
                }
            }
        }

        public void IsolateCategory(AnatomicalCategory category, float othersOpacity = 0.2f)
        {
            foreach (var kvp in registeredMeshes)
            {
                if (kvp.Value.category == category)
                {
                    SetMeshOpacity(kvp.Key, 1f, true);
                }
                else
                {
                    SetMeshOpacity(kvp.Key, othersOpacity, true);
                }
            }
        }

        public void ResetAllAppearance()
        {
            foreach (var kvp in registeredMeshes)
            {
                SetMeshOpacity(kvp.Key, 1f, true);
                HighlightMesh(kvp.Key, false);
                SelectMesh(kvp.Key, false);
            }
        }

        #endregion

        #region Utility

        public List<string> GetAllMeshIds()
        {
            return registeredMeshes.Keys.ToList();
        }

        public List<string> GetMeshIdsByCategory(AnatomicalCategory category)
        {
            return registeredMeshes.Where(kvp => kvp.Value.category == category)
                                   .Select(kvp => kvp.Key)
                                   .ToList();
        }

        public SegmentedMeshData GetMeshData(string meshId)
        {
            return registeredMeshes.TryGetValue(meshId, out SegmentedMeshData data) ? data : null;
        }

        public void SetQuality(QualityLevel level)
        {
            qualityLevel = level;

            // Re-process meshes based on new quality
            foreach (var kvp in registeredMeshes)
            {
                if (level >= QualityLevel.Medium && enableSmoothNormals)
                {
                    if (kvp.Value.smoothedMesh == null)
                    {
                        kvp.Value.smoothedMesh = SmoothMeshNormals(kvp.Value.originalMesh);
                    }
                    kvp.Value.meshFilter.mesh = kvp.Value.smoothedMesh;
                }
                else
                {
                    kvp.Value.meshFilter.mesh = kvp.Value.originalMesh;
                }
            }
        }

        private void OnDestroy()
        {
            // Cleanup materials
            foreach (var mat in materialInstances.Values)
            {
                if (mat != null)
                    Destroy(mat);
            }
            materialInstances.Clear();

            // Cleanup smoothed meshes
            foreach (var data in registeredMeshes.Values)
            {
                if (data.smoothedMesh != null && data.smoothedMesh != data.originalMesh)
                {
                    Destroy(data.smoothedMesh);
                }
            }
            registeredMeshes.Clear();
        }

        #endregion
    }
}

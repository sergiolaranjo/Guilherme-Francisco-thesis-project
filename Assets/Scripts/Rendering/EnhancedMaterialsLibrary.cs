// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace CardiacVR.Rendering
{
    /// <summary>
    /// Enhanced materials library for professional medical visualization
    /// Provides high-quality materials for anatomical structures
    /// </summary>
    public class EnhancedMaterialsLibrary : MonoBehaviour
    {
        public static EnhancedMaterialsLibrary Instance { get; private set; }

        [Header("Base Shaders")]
        [SerializeField] private Shader litShader;
        [SerializeField] private Shader subsurfaceShader;
        [SerializeField] private Shader transparentShader;
        [SerializeField] private Shader outlineShader;

        [Header("Material Quality")]
        [SerializeField] private MaterialQuality quality = MaterialQuality.High;

        // Material caches
        private Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
        private Dictionary<AnatomicalStructure, MaterialPreset> structurePresets;

        public enum MaterialQuality
        {
            Low,      // Mobile VR
            Medium,   // Standard VR
            High,     // Desktop VR
            Ultra     // Presentation
        }

        public enum AnatomicalStructure
        {
            // Heart structures
            Heart_Myocardium,
            Heart_Endocardium,
            Heart_Epicardium,
            Heart_Valve_Aortic,
            Heart_Valve_Mitral,
            Heart_Valve_Tricuspid,
            Heart_Valve_Pulmonary,
            Heart_Septum,
            Heart_Papillary,
            Heart_Chordae,

            // Blood vessels
            Vessel_Artery,
            Vessel_Vein,
            Vessel_Capillary,
            Vessel_Aorta,
            Vessel_Coronary,
            Vessel_Pulmonary,

            // Other organs
            Lung_Tissue,
            Bone_Cortical,
            Bone_Cancellous,
            Cartilage,
            Fat_Tissue,
            Muscle_Skeletal,
            Muscle_Smooth,
            Skin,
            Nerve,

            // Pathological
            Calcification,
            Thrombus,
            Tumor,
            Scar_Tissue,
            Inflammation,

            // Surgical
            Implant_Metal,
            Implant_Polymer,
            Suture,
            Patch,
            Stent,

            // Visualization
            Ghost,
            Highlight,
            Selection,
            Error,
            Warning,
            Success
        }

        [System.Serializable]
        public class MaterialPreset
        {
            public string name;
            public Color baseColor = Color.white;
            public Color emissionColor = Color.black;
            public float metallic = 0f;
            public float smoothness = 0.5f;
            public float subsurfaceStrength = 0f;
            public Color subsurfaceColor = Color.red;
            public float opacity = 1f;
            public float fresnelPower = 2f;
            public float rimIntensity = 0.3f;
            public Color rimColor = Color.white;
            public bool useNormalMap = false;
            public float normalStrength = 1f;
            public bool castShadows = true;
            public bool receiveShadows = true;
            public RenderMode renderMode = RenderMode.Opaque;
            public CullMode cullMode = CullMode.Back;

            public enum RenderMode { Opaque, Cutout, Transparent, Fade }
            public enum CullMode { Off, Front, Back }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePresets();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (litShader == null)
                litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (transparentShader == null)
                transparentShader = Shader.Find("Universal Render Pipeline/Lit");
        }

        private void InitializePresets()
        {
            structurePresets = new Dictionary<AnatomicalStructure, MaterialPreset>
            {
                // Heart Myocardium - Rich red with subsurface scattering
                [AnatomicalStructure.Heart_Myocardium] = new MaterialPreset
                {
                    name = "Heart Myocardium",
                    baseColor = new Color(0.75f, 0.22f, 0.17f, 1f),
                    metallic = 0f,
                    smoothness = 0.4f,
                    subsurfaceStrength = 0.6f,
                    subsurfaceColor = new Color(0.9f, 0.3f, 0.2f, 1f),
                    fresnelPower = 3f,
                    rimIntensity = 0.2f,
                    rimColor = new Color(1f, 0.5f, 0.4f, 1f)
                },

                // Endocardium - Smooth inner lining
                [AnatomicalStructure.Heart_Endocardium] = new MaterialPreset
                {
                    name = "Heart Endocardium",
                    baseColor = new Color(0.85f, 0.75f, 0.7f, 1f),
                    metallic = 0f,
                    smoothness = 0.7f,
                    subsurfaceStrength = 0.3f,
                    subsurfaceColor = new Color(0.9f, 0.5f, 0.4f, 1f),
                    fresnelPower = 2f,
                    rimIntensity = 0.15f
                },

                // Epicardium - Outer surface
                [AnatomicalStructure.Heart_Epicardium] = new MaterialPreset
                {
                    name = "Heart Epicardium",
                    baseColor = new Color(0.95f, 0.85f, 0.75f, 1f),
                    metallic = 0f,
                    smoothness = 0.5f,
                    subsurfaceStrength = 0.2f,
                    subsurfaceColor = new Color(0.8f, 0.4f, 0.3f, 1f)
                },

                // Aortic Valve - Thin, translucent
                [AnatomicalStructure.Heart_Valve_Aortic] = new MaterialPreset
                {
                    name = "Aortic Valve",
                    baseColor = new Color(0.95f, 0.9f, 0.85f, 0.9f),
                    metallic = 0f,
                    smoothness = 0.6f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(0.95f, 0.6f, 0.5f, 1f),
                    opacity = 0.9f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Mitral Valve
                [AnatomicalStructure.Heart_Valve_Mitral] = new MaterialPreset
                {
                    name = "Mitral Valve",
                    baseColor = new Color(0.93f, 0.88f, 0.82f, 0.9f),
                    metallic = 0f,
                    smoothness = 0.55f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(0.92f, 0.55f, 0.45f, 1f),
                    opacity = 0.9f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Tricuspid Valve
                [AnatomicalStructure.Heart_Valve_Tricuspid] = new MaterialPreset
                {
                    name = "Tricuspid Valve",
                    baseColor = new Color(0.92f, 0.86f, 0.8f, 0.9f),
                    metallic = 0f,
                    smoothness = 0.55f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(0.9f, 0.52f, 0.42f, 1f),
                    opacity = 0.9f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Pulmonary Valve
                [AnatomicalStructure.Heart_Valve_Pulmonary] = new MaterialPreset
                {
                    name = "Pulmonary Valve",
                    baseColor = new Color(0.9f, 0.85f, 0.8f, 0.9f),
                    metallic = 0f,
                    smoothness = 0.55f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(0.88f, 0.5f, 0.4f, 1f),
                    opacity = 0.9f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Septum
                [AnatomicalStructure.Heart_Septum] = new MaterialPreset
                {
                    name = "Heart Septum",
                    baseColor = new Color(0.7f, 0.25f, 0.2f, 1f),
                    metallic = 0f,
                    smoothness = 0.35f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(0.85f, 0.35f, 0.25f, 1f)
                },

                // Papillary Muscles
                [AnatomicalStructure.Heart_Papillary] = new MaterialPreset
                {
                    name = "Papillary Muscle",
                    baseColor = new Color(0.65f, 0.2f, 0.15f, 1f),
                    metallic = 0f,
                    smoothness = 0.3f,
                    subsurfaceStrength = 0.4f,
                    subsurfaceColor = new Color(0.8f, 0.3f, 0.2f, 1f)
                },

                // Chordae Tendineae
                [AnatomicalStructure.Heart_Chordae] = new MaterialPreset
                {
                    name = "Chordae Tendineae",
                    baseColor = new Color(0.9f, 0.85f, 0.8f, 1f),
                    metallic = 0f,
                    smoothness = 0.5f,
                    subsurfaceStrength = 0.3f,
                    subsurfaceColor = new Color(0.9f, 0.6f, 0.5f, 1f)
                },

                // Arteries - Bright red, elastic
                [AnatomicalStructure.Vessel_Artery] = new MaterialPreset
                {
                    name = "Artery",
                    baseColor = new Color(0.85f, 0.15f, 0.1f, 1f),
                    metallic = 0f,
                    smoothness = 0.6f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(1f, 0.25f, 0.15f, 1f),
                    rimIntensity = 0.25f,
                    rimColor = new Color(1f, 0.4f, 0.3f, 1f)
                },

                // Veins - Dark red/blue
                [AnatomicalStructure.Vessel_Vein] = new MaterialPreset
                {
                    name = "Vein",
                    baseColor = new Color(0.25f, 0.15f, 0.45f, 1f),
                    metallic = 0f,
                    smoothness = 0.55f,
                    subsurfaceStrength = 0.4f,
                    subsurfaceColor = new Color(0.4f, 0.2f, 0.5f, 1f),
                    rimIntensity = 0.2f,
                    rimColor = new Color(0.5f, 0.3f, 0.6f, 1f)
                },

                // Aorta
                [AnatomicalStructure.Vessel_Aorta] = new MaterialPreset
                {
                    name = "Aorta",
                    baseColor = new Color(0.9f, 0.2f, 0.15f, 1f),
                    metallic = 0f,
                    smoothness = 0.65f,
                    subsurfaceStrength = 0.55f,
                    subsurfaceColor = new Color(1f, 0.3f, 0.2f, 1f),
                    rimIntensity = 0.3f,
                    rimColor = new Color(1f, 0.5f, 0.4f, 1f)
                },

                // Coronary Arteries
                [AnatomicalStructure.Vessel_Coronary] = new MaterialPreset
                {
                    name = "Coronary Artery",
                    baseColor = new Color(0.92f, 0.25f, 0.18f, 1f),
                    metallic = 0f,
                    smoothness = 0.6f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(1f, 0.35f, 0.25f, 1f),
                    rimIntensity = 0.25f
                },

                // Pulmonary Vessels
                [AnatomicalStructure.Vessel_Pulmonary] = new MaterialPreset
                {
                    name = "Pulmonary Vessel",
                    baseColor = new Color(0.35f, 0.2f, 0.55f, 1f),
                    metallic = 0f,
                    smoothness = 0.55f,
                    subsurfaceStrength = 0.45f,
                    subsurfaceColor = new Color(0.5f, 0.25f, 0.6f, 1f)
                },

                // Lung Tissue
                [AnatomicalStructure.Lung_Tissue] = new MaterialPreset
                {
                    name = "Lung Tissue",
                    baseColor = new Color(0.85f, 0.7f, 0.72f, 0.85f),
                    metallic = 0f,
                    smoothness = 0.3f,
                    subsurfaceStrength = 0.4f,
                    subsurfaceColor = new Color(0.9f, 0.6f, 0.62f, 1f),
                    opacity = 0.85f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Cortical Bone
                [AnatomicalStructure.Bone_Cortical] = new MaterialPreset
                {
                    name = "Cortical Bone",
                    baseColor = new Color(0.95f, 0.92f, 0.85f, 1f),
                    metallic = 0.1f,
                    smoothness = 0.3f,
                    subsurfaceStrength = 0.15f,
                    subsurfaceColor = new Color(0.9f, 0.8f, 0.7f, 1f)
                },

                // Cancellous Bone
                [AnatomicalStructure.Bone_Cancellous] = new MaterialPreset
                {
                    name = "Cancellous Bone",
                    baseColor = new Color(0.9f, 0.85f, 0.75f, 1f),
                    metallic = 0.05f,
                    smoothness = 0.2f,
                    subsurfaceStrength = 0.2f,
                    subsurfaceColor = new Color(0.85f, 0.7f, 0.55f, 1f)
                },

                // Cartilage
                [AnatomicalStructure.Cartilage] = new MaterialPreset
                {
                    name = "Cartilage",
                    baseColor = new Color(0.85f, 0.9f, 0.95f, 0.95f),
                    metallic = 0f,
                    smoothness = 0.7f,
                    subsurfaceStrength = 0.5f,
                    subsurfaceColor = new Color(0.75f, 0.85f, 0.9f, 1f),
                    opacity = 0.95f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Fat Tissue
                [AnatomicalStructure.Fat_Tissue] = new MaterialPreset
                {
                    name = "Fat Tissue",
                    baseColor = new Color(0.98f, 0.92f, 0.6f, 1f),
                    metallic = 0f,
                    smoothness = 0.4f,
                    subsurfaceStrength = 0.6f,
                    subsurfaceColor = new Color(1f, 0.85f, 0.4f, 1f)
                },

                // Skeletal Muscle
                [AnatomicalStructure.Muscle_Skeletal] = new MaterialPreset
                {
                    name = "Skeletal Muscle",
                    baseColor = new Color(0.6f, 0.25f, 0.22f, 1f),
                    metallic = 0f,
                    smoothness = 0.35f,
                    subsurfaceStrength = 0.45f,
                    subsurfaceColor = new Color(0.75f, 0.35f, 0.3f, 1f)
                },

                // Smooth Muscle
                [AnatomicalStructure.Muscle_Smooth] = new MaterialPreset
                {
                    name = "Smooth Muscle",
                    baseColor = new Color(0.7f, 0.35f, 0.3f, 1f),
                    metallic = 0f,
                    smoothness = 0.45f,
                    subsurfaceStrength = 0.4f,
                    subsurfaceColor = new Color(0.82f, 0.42f, 0.35f, 1f)
                },

                // Skin
                [AnatomicalStructure.Skin] = new MaterialPreset
                {
                    name = "Skin",
                    baseColor = new Color(0.9f, 0.75f, 0.65f, 1f),
                    metallic = 0f,
                    smoothness = 0.35f,
                    subsurfaceStrength = 0.65f,
                    subsurfaceColor = new Color(0.95f, 0.5f, 0.35f, 1f),
                    fresnelPower = 4f
                },

                // Nerve
                [AnatomicalStructure.Nerve] = new MaterialPreset
                {
                    name = "Nerve",
                    baseColor = new Color(0.95f, 0.95f, 0.8f, 1f),
                    metallic = 0f,
                    smoothness = 0.5f,
                    subsurfaceStrength = 0.3f,
                    subsurfaceColor = new Color(0.9f, 0.9f, 0.6f, 1f)
                },

                // Calcification
                [AnatomicalStructure.Calcification] = new MaterialPreset
                {
                    name = "Calcification",
                    baseColor = new Color(0.98f, 0.98f, 0.95f, 1f),
                    metallic = 0.2f,
                    smoothness = 0.25f,
                    subsurfaceStrength = 0.1f,
                    emissionColor = new Color(0.1f, 0.1f, 0.1f, 1f)
                },

                // Thrombus
                [AnatomicalStructure.Thrombus] = new MaterialPreset
                {
                    name = "Thrombus",
                    baseColor = new Color(0.4f, 0.08f, 0.08f, 1f),
                    metallic = 0f,
                    smoothness = 0.3f,
                    subsurfaceStrength = 0.35f,
                    subsurfaceColor = new Color(0.5f, 0.1f, 0.1f, 1f)
                },

                // Tumor
                [AnatomicalStructure.Tumor] = new MaterialPreset
                {
                    name = "Tumor",
                    baseColor = new Color(0.5f, 0.35f, 0.35f, 1f),
                    metallic = 0f,
                    smoothness = 0.25f,
                    subsurfaceStrength = 0.4f,
                    subsurfaceColor = new Color(0.6f, 0.3f, 0.3f, 1f)
                },

                // Scar Tissue
                [AnatomicalStructure.Scar_Tissue] = new MaterialPreset
                {
                    name = "Scar Tissue",
                    baseColor = new Color(0.85f, 0.8f, 0.75f, 1f),
                    metallic = 0.05f,
                    smoothness = 0.35f,
                    subsurfaceStrength = 0.2f,
                    subsurfaceColor = new Color(0.8f, 0.7f, 0.65f, 1f)
                },

                // Inflammation
                [AnatomicalStructure.Inflammation] = new MaterialPreset
                {
                    name = "Inflammation",
                    baseColor = new Color(0.9f, 0.4f, 0.35f, 1f),
                    metallic = 0f,
                    smoothness = 0.45f,
                    subsurfaceStrength = 0.55f,
                    subsurfaceColor = new Color(1f, 0.45f, 0.35f, 1f),
                    emissionColor = new Color(0.15f, 0.02f, 0.02f, 1f)
                },

                // Metal Implant
                [AnatomicalStructure.Implant_Metal] = new MaterialPreset
                {
                    name = "Metal Implant",
                    baseColor = new Color(0.85f, 0.85f, 0.88f, 1f),
                    metallic = 0.95f,
                    smoothness = 0.8f,
                    subsurfaceStrength = 0f,
                    fresnelPower = 5f
                },

                // Polymer Implant
                [AnatomicalStructure.Implant_Polymer] = new MaterialPreset
                {
                    name = "Polymer Implant",
                    baseColor = new Color(0.9f, 0.9f, 0.92f, 0.9f),
                    metallic = 0f,
                    smoothness = 0.7f,
                    subsurfaceStrength = 0.2f,
                    opacity = 0.9f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Suture
                [AnatomicalStructure.Suture] = new MaterialPreset
                {
                    name = "Suture",
                    baseColor = new Color(0.15f, 0.25f, 0.5f, 1f),
                    metallic = 0.1f,
                    smoothness = 0.4f,
                    subsurfaceStrength = 0.1f
                },

                // Patch
                [AnatomicalStructure.Patch] = new MaterialPreset
                {
                    name = "Patch",
                    baseColor = new Color(0.95f, 0.95f, 0.9f, 1f),
                    metallic = 0f,
                    smoothness = 0.3f,
                    subsurfaceStrength = 0.15f
                },

                // Stent
                [AnatomicalStructure.Stent] = new MaterialPreset
                {
                    name = "Stent",
                    baseColor = new Color(0.8f, 0.82f, 0.85f, 1f),
                    metallic = 0.9f,
                    smoothness = 0.75f,
                    subsurfaceStrength = 0f
                },

                // Ghost (Semi-transparent visualization)
                [AnatomicalStructure.Ghost] = new MaterialPreset
                {
                    name = "Ghost",
                    baseColor = new Color(0.7f, 0.8f, 0.9f, 0.25f),
                    metallic = 0f,
                    smoothness = 0.8f,
                    subsurfaceStrength = 0f,
                    opacity = 0.25f,
                    fresnelPower = 2f,
                    rimIntensity = 0.5f,
                    rimColor = new Color(0.8f, 0.9f, 1f, 1f),
                    renderMode = MaterialPreset.RenderMode.Transparent,
                    cullMode = MaterialPreset.CullMode.Off
                },

                // Highlight
                [AnatomicalStructure.Highlight] = new MaterialPreset
                {
                    name = "Highlight",
                    baseColor = new Color(0.3f, 0.7f, 1f, 0.5f),
                    metallic = 0f,
                    smoothness = 0.9f,
                    emissionColor = new Color(0.1f, 0.3f, 0.5f, 1f),
                    opacity = 0.5f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Selection
                [AnatomicalStructure.Selection] = new MaterialPreset
                {
                    name = "Selection",
                    baseColor = new Color(1f, 0.85f, 0.2f, 0.6f),
                    metallic = 0f,
                    smoothness = 0.85f,
                    emissionColor = new Color(0.4f, 0.35f, 0.05f, 1f),
                    opacity = 0.6f,
                    rimIntensity = 0.6f,
                    rimColor = new Color(1f, 0.9f, 0.4f, 1f),
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Error
                [AnatomicalStructure.Error] = new MaterialPreset
                {
                    name = "Error",
                    baseColor = new Color(1f, 0.2f, 0.2f, 0.7f),
                    metallic = 0f,
                    smoothness = 0.7f,
                    emissionColor = new Color(0.5f, 0.05f, 0.05f, 1f),
                    opacity = 0.7f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Warning
                [AnatomicalStructure.Warning] = new MaterialPreset
                {
                    name = "Warning",
                    baseColor = new Color(1f, 0.7f, 0.1f, 0.7f),
                    metallic = 0f,
                    smoothness = 0.7f,
                    emissionColor = new Color(0.5f, 0.35f, 0.02f, 1f),
                    opacity = 0.7f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                },

                // Success
                [AnatomicalStructure.Success] = new MaterialPreset
                {
                    name = "Success",
                    baseColor = new Color(0.2f, 0.85f, 0.4f, 0.7f),
                    metallic = 0f,
                    smoothness = 0.7f,
                    emissionColor = new Color(0.05f, 0.4f, 0.1f, 1f),
                    opacity = 0.7f,
                    renderMode = MaterialPreset.RenderMode.Transparent
                }
            };
        }

        #region Material Creation

        public Material GetMaterial(AnatomicalStructure structure)
        {
            string key = structure.ToString();

            if (materialCache.TryGetValue(key, out Material cached))
                return cached;

            if (structurePresets.TryGetValue(structure, out MaterialPreset preset))
            {
                Material material = CreateMaterialFromPreset(preset);
                materialCache[key] = material;
                return material;
            }

            Debug.LogWarning($"No preset found for structure: {structure}");
            return GetDefaultMaterial();
        }

        public Material GetMaterial(AnatomicalStructure structure, Color colorOverride)
        {
            Material baseMaterial = GetMaterial(structure);
            Material instance = new Material(baseMaterial);
            instance.color = colorOverride;
            return instance;
        }

        public Material GetMaterial(AnatomicalStructure structure, float opacityOverride)
        {
            Material baseMaterial = GetMaterial(structure);
            Material instance = new Material(baseMaterial);

            Color color = instance.color;
            color.a = opacityOverride;
            instance.color = color;

            return instance;
        }

        private Material CreateMaterialFromPreset(MaterialPreset preset)
        {
            Shader shader = preset.renderMode == MaterialPreset.RenderMode.Opaque ? litShader : transparentShader;
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");

            Material material = new Material(shader);
            material.name = preset.name;

            // Base properties
            material.SetColor("_BaseColor", preset.baseColor);
            material.SetFloat("_Metallic", preset.metallic);
            material.SetFloat("_Smoothness", preset.smoothness);

            // Emission
            if (preset.emissionColor != Color.black)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", preset.emissionColor);
            }

            // Render mode setup
            SetupRenderMode(material, preset);

            // Quality-based settings
            ApplyQualitySettings(material, preset);

            return material;
        }

        private void SetupRenderMode(Material material, MaterialPreset preset)
        {
            switch (preset.renderMode)
            {
                case MaterialPreset.RenderMode.Opaque:
                    material.SetFloat("_Surface", 0);
                    material.SetFloat("_Blend", 0);
                    material.renderQueue = (int)RenderQueue.Geometry;
                    break;

                case MaterialPreset.RenderMode.Cutout:
                    material.SetFloat("_Surface", 0);
                    material.SetFloat("_AlphaClip", 1);
                    material.renderQueue = (int)RenderQueue.AlphaTest;
                    material.EnableKeyword("_ALPHATEST_ON");
                    break;

                case MaterialPreset.RenderMode.Transparent:
                    material.SetFloat("_Surface", 1);
                    material.SetFloat("_Blend", 0);
                    material.renderQueue = (int)RenderQueue.Transparent;
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    break;

                case MaterialPreset.RenderMode.Fade:
                    material.SetFloat("_Surface", 1);
                    material.SetFloat("_Blend", 1);
                    material.renderQueue = (int)RenderQueue.Transparent;
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    break;
            }

            // Culling
            switch (preset.cullMode)
            {
                case MaterialPreset.CullMode.Off:
                    material.SetInt("_Cull", 0);
                    break;
                case MaterialPreset.CullMode.Front:
                    material.SetInt("_Cull", 1);
                    break;
                case MaterialPreset.CullMode.Back:
                    material.SetInt("_Cull", 2);
                    break;
            }

            // Shadows
            material.SetFloat("_CastShadows", preset.castShadows ? 1 : 0);
            material.SetFloat("_ReceiveShadows", preset.receiveShadows ? 1 : 0);
        }

        private void ApplyQualitySettings(Material material, MaterialPreset preset)
        {
            switch (quality)
            {
                case MaterialQuality.Low:
                    material.DisableKeyword("_NORMALMAP");
                    material.DisableKeyword("_EMISSION");
                    break;

                case MaterialQuality.Medium:
                    if (preset.useNormalMap)
                        material.EnableKeyword("_NORMALMAP");
                    break;

                case MaterialQuality.High:
                case MaterialQuality.Ultra:
                    if (preset.useNormalMap)
                        material.EnableKeyword("_NORMALMAP");
                    material.EnableKeyword("_SPECULARHIGHLIGHTS_ON");
                    material.EnableKeyword("_ENVIRONMENTREFLECTIONS_ON");
                    break;
            }
        }

        private Material GetDefaultMaterial()
        {
            if (!materialCache.TryGetValue("_Default", out Material defaultMat))
            {
                Shader shader = litShader ?? Shader.Find("Universal Render Pipeline/Lit");
                defaultMat = new Material(shader);
                defaultMat.color = Color.gray;
                materialCache["_Default"] = defaultMat;
            }
            return defaultMat;
        }

        #endregion

        #region Material Modification

        public void SetMaterialOpacity(Material material, float opacity)
        {
            Color color = material.color;
            color.a = opacity;
            material.color = color;
        }

        public void SetMaterialEmission(Material material, Color emissionColor, float intensity = 1f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor * intensity);
        }

        public void PulseMaterial(Material material, Color pulseColor, float duration = 1f, int pulseCount = 2)
        {
            StartCoroutine(PulseMaterialCoroutine(material, pulseColor, duration, pulseCount));
        }

        private System.Collections.IEnumerator PulseMaterialCoroutine(Material material, Color pulseColor,
            float duration, int pulseCount)
        {
            Color originalColor = material.color;
            float pulseDuration = duration / (pulseCount * 2);

            for (int i = 0; i < pulseCount; i++)
            {
                float elapsed = 0;

                // Fade to pulse color
                while (elapsed < pulseDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / pulseDuration;
                    material.color = Color.Lerp(originalColor, pulseColor, t);
                    yield return null;
                }

                elapsed = 0;

                // Fade back to original
                while (elapsed < pulseDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / pulseDuration;
                    material.color = Color.Lerp(pulseColor, originalColor, t);
                    yield return null;
                }
            }

            material.color = originalColor;
        }

        #endregion

        #region Batch Operations

        public void ApplyMaterialToRenderer(Renderer renderer, AnatomicalStructure structure)
        {
            renderer.material = GetMaterial(structure);
        }

        public void ApplyMaterialToRenderers(Renderer[] renderers, AnatomicalStructure structure)
        {
            Material material = GetMaterial(structure);
            foreach (var renderer in renderers)
            {
                renderer.material = material;
            }
        }

        public void ApplyMaterialByTag(string tag, AnatomicalStructure structure)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (var obj in objects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material = GetMaterial(structure);
            }
        }

        #endregion

        #region Material Presets Access

        public MaterialPreset GetPreset(AnatomicalStructure structure)
        {
            return structurePresets.TryGetValue(structure, out MaterialPreset preset) ? preset : null;
        }

        public void SetQuality(MaterialQuality newQuality)
        {
            if (quality != newQuality)
            {
                quality = newQuality;
                // Clear cache to regenerate materials with new quality
                ClearCache();
            }
        }

        public void ClearCache()
        {
            foreach (var material in materialCache.Values)
            {
                if (material != null)
                    Destroy(material);
            }
            materialCache.Clear();
        }

        public List<string> GetAllStructureNames()
        {
            List<string> names = new List<string>();
            foreach (AnatomicalStructure structure in System.Enum.GetValues(typeof(AnatomicalStructure)))
            {
                names.Add(structure.ToString());
            }
            return names;
        }

        #endregion

        private void OnDestroy()
        {
            ClearCache();
        }
    }
}

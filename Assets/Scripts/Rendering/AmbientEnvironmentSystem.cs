// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

namespace CardiacVR.Rendering
{
    /// <summary>
    /// Ambient effects and environment polish for stunning visual presentation
    /// Creates beautiful, professional medical visualization environments
    /// </summary>
    public class AmbientEnvironmentSystem : MonoBehaviour
    {
        public static AmbientEnvironmentSystem Instance { get; private set; }

        [Header("Environment Presets")]
        [SerializeField] private EnvironmentPreset currentPreset = EnvironmentPreset.MedicalStudio;

        [Header("Ground Plane")]
        [SerializeField] private bool showGroundPlane = true;
        [SerializeField] private Material groundMaterial;
        [SerializeField] private float groundSize = 20f;
        [SerializeField] private float groundReflectivity = 0.3f;

        [Header("Ambient Particles")]
        [SerializeField] private bool enableAmbientParticles = true;
        [SerializeField] private ParticleSystem dustParticles;
        [SerializeField] private float particleDensity = 50f;

        [Header("Gradient Background")]
        [SerializeField] private bool useGradientBackground = true;
        [SerializeField] private Color topColor = new Color(0.15f, 0.18f, 0.25f, 1f);
        [SerializeField] private Color bottomColor = new Color(0.08f, 0.1f, 0.15f, 1f);

        [Header("Vignette")]
        [SerializeField] private bool enableVignette = true;
        [SerializeField] private float vignetteIntensity = 0.35f;
        [SerializeField] private float vignetteSmoothness = 0.4f;

        [Header("Dynamic Effects")]
        [SerializeField] private bool enableBreathing = true;
        [SerializeField] private float breathingSpeed = 0.5f;
        [SerializeField] private float breathingIntensity = 0.02f;

        [Header("Spotlight Effects")]
        [SerializeField] private bool enableSpotlight = true;
        [SerializeField] private Light spotLight;
        [SerializeField] private float spotlightIntensity = 1.2f;
        [SerializeField] private float spotlightRange = 10f;

        [Header("References")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform modelCenter;

        // Created objects
        private GameObject groundPlaneObject;
        private GameObject backgroundObject;
        private GameObject ambientLightRig;
        private List<Light> environmentLights = new List<Light>();

        public enum EnvironmentPreset
        {
            MedicalStudio,      // Clean, professional medical imaging look
            DarkRoom,           // Dark background with dramatic lighting
            OperatingRoom,      // Bright surgical lighting
            PresentationHall,   // Conference/presentation setting
            VRComfort,          // Optimized for VR viewing comfort
            Educational,        // Bright, clear educational setting
            Cinematic,          // Dramatic, film-like presentation
            InfiniteWhite,      // Clean white studio
            InfiniteBlack       // Dark void with focused lighting
        }

        [System.Serializable]
        public class EnvironmentSettings
        {
            public string name;

            // Colors
            public Color backgroundColor = new Color(0.1f, 0.12f, 0.15f, 1f);
            public Color gradientTop = new Color(0.15f, 0.18f, 0.25f, 1f);
            public Color gradientBottom = new Color(0.08f, 0.1f, 0.15f, 1f);
            public Color ambientColor = new Color(0.2f, 0.22f, 0.25f, 1f);
            public Color groundColor = new Color(0.1f, 0.1f, 0.12f, 1f);

            // Lighting
            public float ambientIntensity = 1f;
            public float mainLightIntensity = 1f;
            public float fillLightIntensity = 0.5f;
            public float rimLightIntensity = 0.8f;
            public Color mainLightColor = Color.white;

            // Effects
            public float vignetteIntensity = 0.35f;
            public float bloomIntensity = 0.5f;
            public float groundReflectivity = 0.3f;
            public bool showGround = true;
            public bool enableParticles = true;
            public bool enableSpotlight = false;

            // Fog
            public bool enableFog = false;
            public Color fogColor = new Color(0.1f, 0.12f, 0.15f, 1f);
            public float fogDensity = 0.02f;
        }

        private Dictionary<EnvironmentPreset, EnvironmentSettings> presets;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePresets();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            SetupEnvironment();
            ApplyPreset(currentPreset, false);
        }

        private void InitializePresets()
        {
            presets = new Dictionary<EnvironmentPreset, EnvironmentSettings>
            {
                [EnvironmentPreset.MedicalStudio] = new EnvironmentSettings
                {
                    name = "Medical Studio",
                    backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f),
                    gradientTop = new Color(0.18f, 0.2f, 0.28f, 1f),
                    gradientBottom = new Color(0.08f, 0.1f, 0.14f, 1f),
                    ambientColor = new Color(0.25f, 0.27f, 0.32f, 1f),
                    groundColor = new Color(0.08f, 0.09f, 0.11f, 0.8f),
                    ambientIntensity = 1.1f,
                    mainLightIntensity = 1f,
                    fillLightIntensity = 0.6f,
                    rimLightIntensity = 0.7f,
                    mainLightColor = new Color(1f, 0.98f, 0.95f, 1f),
                    vignetteIntensity = 0.3f,
                    bloomIntensity = 0.4f,
                    groundReflectivity = 0.25f,
                    showGround = true,
                    enableParticles = true,
                    enableSpotlight = false
                },

                [EnvironmentPreset.DarkRoom] = new EnvironmentSettings
                {
                    name = "Dark Room",
                    backgroundColor = new Color(0.02f, 0.03f, 0.05f, 1f),
                    gradientTop = new Color(0.05f, 0.06f, 0.1f, 1f),
                    gradientBottom = new Color(0.01f, 0.01f, 0.02f, 1f),
                    ambientColor = new Color(0.08f, 0.1f, 0.15f, 1f),
                    groundColor = new Color(0.02f, 0.02f, 0.03f, 0.9f),
                    ambientIntensity = 0.5f,
                    mainLightIntensity = 1.3f,
                    fillLightIntensity = 0.2f,
                    rimLightIntensity = 1.2f,
                    mainLightColor = new Color(0.95f, 0.97f, 1f, 1f),
                    vignetteIntensity = 0.5f,
                    bloomIntensity = 0.6f,
                    groundReflectivity = 0.4f,
                    showGround = true,
                    enableParticles = true,
                    enableSpotlight = true
                },

                [EnvironmentPreset.OperatingRoom] = new EnvironmentSettings
                {
                    name = "Operating Room",
                    backgroundColor = new Color(0.35f, 0.38f, 0.4f, 1f),
                    gradientTop = new Color(0.45f, 0.47f, 0.5f, 1f),
                    gradientBottom = new Color(0.3f, 0.32f, 0.35f, 1f),
                    ambientColor = new Color(0.4f, 0.42f, 0.45f, 1f),
                    groundColor = new Color(0.25f, 0.27f, 0.3f, 1f),
                    ambientIntensity = 1.3f,
                    mainLightIntensity = 1.5f,
                    fillLightIntensity = 0.8f,
                    rimLightIntensity = 0.5f,
                    mainLightColor = new Color(1f, 0.99f, 0.97f, 1f),
                    vignetteIntensity = 0.15f,
                    bloomIntensity = 0.3f,
                    groundReflectivity = 0.1f,
                    showGround = true,
                    enableParticles = false,
                    enableSpotlight = false
                },

                [EnvironmentPreset.PresentationHall] = new EnvironmentSettings
                {
                    name = "Presentation Hall",
                    backgroundColor = new Color(0.08f, 0.1f, 0.15f, 1f),
                    gradientTop = new Color(0.12f, 0.15f, 0.22f, 1f),
                    gradientBottom = new Color(0.05f, 0.06f, 0.1f, 1f),
                    ambientColor = new Color(0.2f, 0.22f, 0.28f, 1f),
                    groundColor = new Color(0.05f, 0.06f, 0.08f, 0.85f),
                    ambientIntensity = 0.9f,
                    mainLightIntensity = 1.2f,
                    fillLightIntensity = 0.4f,
                    rimLightIntensity = 0.9f,
                    mainLightColor = new Color(1f, 0.97f, 0.92f, 1f),
                    vignetteIntensity = 0.4f,
                    bloomIntensity = 0.5f,
                    groundReflectivity = 0.35f,
                    showGround = true,
                    enableParticles = true,
                    enableSpotlight = true
                },

                [EnvironmentPreset.VRComfort] = new EnvironmentSettings
                {
                    name = "VR Comfort",
                    backgroundColor = new Color(0.15f, 0.17f, 0.2f, 1f),
                    gradientTop = new Color(0.2f, 0.22f, 0.27f, 1f),
                    gradientBottom = new Color(0.12f, 0.14f, 0.17f, 1f),
                    ambientColor = new Color(0.25f, 0.27f, 0.3f, 1f),
                    groundColor = new Color(0.1f, 0.11f, 0.13f, 0.7f),
                    ambientIntensity = 1f,
                    mainLightIntensity = 0.9f,
                    fillLightIntensity = 0.6f,
                    rimLightIntensity = 0.4f,
                    mainLightColor = new Color(1f, 0.98f, 0.96f, 1f),
                    vignetteIntensity = 0.2f,
                    bloomIntensity = 0.3f,
                    groundReflectivity = 0.2f,
                    showGround = true,
                    enableParticles = false,
                    enableSpotlight = false
                },

                [EnvironmentPreset.Educational] = new EnvironmentSettings
                {
                    name = "Educational",
                    backgroundColor = new Color(0.22f, 0.25f, 0.3f, 1f),
                    gradientTop = new Color(0.3f, 0.33f, 0.4f, 1f),
                    gradientBottom = new Color(0.18f, 0.2f, 0.25f, 1f),
                    ambientColor = new Color(0.35f, 0.37f, 0.42f, 1f),
                    groundColor = new Color(0.15f, 0.16f, 0.19f, 0.75f),
                    ambientIntensity = 1.2f,
                    mainLightIntensity = 1f,
                    fillLightIntensity = 0.7f,
                    rimLightIntensity = 0.5f,
                    mainLightColor = new Color(1f, 0.99f, 0.97f, 1f),
                    vignetteIntensity = 0.2f,
                    bloomIntensity = 0.35f,
                    groundReflectivity = 0.15f,
                    showGround = true,
                    enableParticles = false,
                    enableSpotlight = false
                },

                [EnvironmentPreset.Cinematic] = new EnvironmentSettings
                {
                    name = "Cinematic",
                    backgroundColor = new Color(0.03f, 0.04f, 0.06f, 1f),
                    gradientTop = new Color(0.08f, 0.1f, 0.15f, 1f),
                    gradientBottom = new Color(0.01f, 0.01f, 0.02f, 1f),
                    ambientColor = new Color(0.1f, 0.12f, 0.18f, 1f),
                    groundColor = new Color(0.02f, 0.02f, 0.03f, 0.9f),
                    ambientIntensity = 0.6f,
                    mainLightIntensity = 1.4f,
                    fillLightIntensity = 0.15f,
                    rimLightIntensity = 1.4f,
                    mainLightColor = new Color(1f, 0.95f, 0.85f, 1f),
                    vignetteIntensity = 0.55f,
                    bloomIntensity = 0.7f,
                    groundReflectivity = 0.5f,
                    showGround = true,
                    enableParticles = true,
                    enableSpotlight = true,
                    enableFog = true,
                    fogColor = new Color(0.03f, 0.04f, 0.06f, 1f),
                    fogDensity = 0.015f
                },

                [EnvironmentPreset.InfiniteWhite] = new EnvironmentSettings
                {
                    name = "Infinite White",
                    backgroundColor = new Color(0.95f, 0.95f, 0.97f, 1f),
                    gradientTop = new Color(1f, 1f, 1f, 1f),
                    gradientBottom = new Color(0.92f, 0.93f, 0.95f, 1f),
                    ambientColor = new Color(0.9f, 0.9f, 0.92f, 1f),
                    groundColor = new Color(0.85f, 0.86f, 0.88f, 0.5f),
                    ambientIntensity = 1.4f,
                    mainLightIntensity = 0.8f,
                    fillLightIntensity = 0.8f,
                    rimLightIntensity = 0.3f,
                    mainLightColor = Color.white,
                    vignetteIntensity = 0.1f,
                    bloomIntensity = 0.2f,
                    groundReflectivity = 0.05f,
                    showGround = true,
                    enableParticles = false,
                    enableSpotlight = false
                },

                [EnvironmentPreset.InfiniteBlack] = new EnvironmentSettings
                {
                    name = "Infinite Black",
                    backgroundColor = Color.black,
                    gradientTop = new Color(0.02f, 0.02f, 0.03f, 1f),
                    gradientBottom = Color.black,
                    ambientColor = new Color(0.05f, 0.05f, 0.07f, 1f),
                    groundColor = new Color(0.01f, 0.01f, 0.01f, 0.95f),
                    ambientIntensity = 0.3f,
                    mainLightIntensity = 1.5f,
                    fillLightIntensity = 0.1f,
                    rimLightIntensity = 1.5f,
                    mainLightColor = new Color(0.9f, 0.95f, 1f, 1f),
                    vignetteIntensity = 0.6f,
                    bloomIntensity = 0.8f,
                    groundReflectivity = 0.6f,
                    showGround = true,
                    enableParticles = true,
                    enableSpotlight = true
                }
            };
        }

        #region Environment Setup

        private void SetupEnvironment()
        {
            CreateGroundPlane();
            CreateAmbientLightRig();
            CreateAmbientParticles();
            SetupPostProcessing();
        }

        private void CreateGroundPlane()
        {
            if (groundPlaneObject != null)
                Destroy(groundPlaneObject);

            groundPlaneObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            groundPlaneObject.name = "GroundPlane";
            groundPlaneObject.transform.SetParent(transform);
            groundPlaneObject.transform.localPosition = Vector3.zero;
            groundPlaneObject.transform.localScale = new Vector3(groundSize * 0.1f, 1f, groundSize * 0.1f);

            // Remove collider (we don't need physics)
            Collider col = groundPlaneObject.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Setup material
            Renderer renderer = groundPlaneObject.GetComponent<Renderer>();
            if (groundMaterial == null)
            {
                groundMaterial = CreateGroundMaterial();
            }
            renderer.material = groundMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private Material CreateGroundMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            Material mat = new Material(shader);
            mat.name = "Ground Material";

            mat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.12f, 0.8f));
            mat.SetFloat("_Smoothness", 0.85f);
            mat.SetFloat("_Metallic", 0.1f);

            // Setup transparency
            mat.SetFloat("_Surface", 1);
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return mat;
        }

        private void CreateAmbientLightRig()
        {
            if (ambientLightRig != null)
                Destroy(ambientLightRig);

            ambientLightRig = new GameObject("AmbientLightRig");
            ambientLightRig.transform.SetParent(transform);

            // Create main key light
            Light keyLight = CreateLight("KeyLight", LightType.Directional,
                new Vector3(45f, -30f, 0f), 1f, Color.white);
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.5f;

            // Create fill light
            Light fillLight = CreateLight("FillLight", LightType.Directional,
                new Vector3(30f, 120f, 0f), 0.5f, new Color(0.9f, 0.95f, 1f));
            fillLight.shadows = LightShadows.None;

            // Create rim/back light
            Light rimLight = CreateLight("RimLight", LightType.Directional,
                new Vector3(10f, -150f, 0f), 0.8f, new Color(0.95f, 0.98f, 1f));
            rimLight.shadows = LightShadows.None;

            // Create spotlight (for dramatic effect)
            if (enableSpotlight)
            {
                spotLight = CreateLight("Spotlight", LightType.Spot,
                    new Vector3(60f, 0f, 0f), spotlightIntensity, Color.white).GetComponent<Light>();

                GameObject spotObj = spotLight.gameObject;
                spotObj.transform.position = Vector3.up * 5f;
                spotLight.range = spotlightRange;
                spotLight.spotAngle = 45f;
                spotLight.shadows = LightShadows.Soft;
            }
        }

        private Light CreateLight(string name, LightType type, Vector3 rotation, float intensity, Color color)
        {
            GameObject lightObj = new GameObject(name);
            lightObj.transform.SetParent(ambientLightRig.transform);
            lightObj.transform.eulerAngles = rotation;

            Light light = lightObj.AddComponent<Light>();
            light.type = type;
            light.intensity = intensity;
            light.color = color;

            environmentLights.Add(light);

            return light;
        }

        private void CreateAmbientParticles()
        {
            if (!enableAmbientParticles) return;

            if (dustParticles == null)
            {
                GameObject particleObj = new GameObject("AmbientParticles");
                particleObj.transform.SetParent(transform);
                particleObj.transform.localPosition = Vector3.zero;

                dustParticles = particleObj.AddComponent<ParticleSystem>();
                ConfigureAmbientParticles(dustParticles);
            }
        }

        private void ConfigureAmbientParticles(ParticleSystem ps)
        {
            var main = ps.main;
            main.startLifetime = 8f;
            main.startSpeed = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.02f);
            main.startColor = new Color(1f, 1f, 1f, 0.15f);
            main.maxParticles = (int)particleDensity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = particleDensity * 0.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(10f, 5f, 10f);

            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-0.01f, 0.01f);
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.2f, 0.3f), new GradientAlphaKey(0.2f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            renderer.material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.15f));
        }

        private void SetupPostProcessing()
        {
            if (postProcessVolume == null)
            {
                postProcessVolume = FindObjectOfType<Volume>();
            }

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                // Setup vignette
                if (postProcessVolume.profile.TryGet(out Vignette vignette))
                {
                    vignette.intensity.value = vignetteIntensity;
                    vignette.smoothness.value = vignetteSmoothness;
                }
            }
        }

        #endregion

        #region Preset Application

        public void ApplyPreset(EnvironmentPreset preset, bool animated = true)
        {
            if (!presets.TryGetValue(preset, out EnvironmentSettings settings))
                return;

            currentPreset = preset;

            if (animated)
            {
                StartCoroutine(TransitionToSettings(settings));
            }
            else
            {
                ApplySettingsImmediate(settings);
            }
        }

        private void ApplySettingsImmediate(EnvironmentSettings settings)
        {
            // Background
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = settings.backgroundColor;
            }

            topColor = settings.gradientTop;
            bottomColor = settings.gradientBottom;

            // Ground
            if (groundPlaneObject != null)
            {
                groundPlaneObject.SetActive(settings.showGround);
                if (groundMaterial != null)
                {
                    groundMaterial.SetColor("_BaseColor", settings.groundColor);
                    groundMaterial.SetFloat("_Smoothness", settings.groundReflectivity);
                }
            }

            // Ambient
            RenderSettings.ambientLight = settings.ambientColor;
            RenderSettings.ambientIntensity = settings.ambientIntensity;

            // Lights
            foreach (Light light in environmentLights)
            {
                if (light == null) continue;

                if (light.name == "KeyLight")
                {
                    light.intensity = settings.mainLightIntensity;
                    light.color = settings.mainLightColor;
                }
                else if (light.name == "FillLight")
                {
                    light.intensity = settings.fillLightIntensity;
                }
                else if (light.name == "RimLight")
                {
                    light.intensity = settings.rimLightIntensity;
                }
            }

            // Spotlight
            if (spotLight != null)
            {
                spotLight.gameObject.SetActive(settings.enableSpotlight);
            }

            // Particles
            if (dustParticles != null)
            {
                if (settings.enableParticles)
                    dustParticles.Play();
                else
                    dustParticles.Stop();
            }

            // Fog
            RenderSettings.fog = settings.enableFog;
            if (settings.enableFog)
            {
                RenderSettings.fogColor = settings.fogColor;
                RenderSettings.fogDensity = settings.fogDensity;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
            }

            // Post processing
            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                if (postProcessVolume.profile.TryGet(out Vignette vignette))
                {
                    vignette.intensity.value = settings.vignetteIntensity;
                }

                if (postProcessVolume.profile.TryGet(out Bloom bloom))
                {
                    bloom.intensity.value = settings.bloomIntensity;
                }
            }
        }

        private IEnumerator TransitionToSettings(EnvironmentSettings targetSettings)
        {
            EnvironmentSettings startSettings = CaptureCurrentSettings();
            float duration = 1f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // Smooth step

                LerpSettings(startSettings, targetSettings, t);
                yield return null;
            }

            ApplySettingsImmediate(targetSettings);
        }

        private EnvironmentSettings CaptureCurrentSettings()
        {
            EnvironmentSettings settings = new EnvironmentSettings();

            if (mainCamera != null)
                settings.backgroundColor = mainCamera.backgroundColor;

            settings.gradientTop = topColor;
            settings.gradientBottom = bottomColor;

            if (groundMaterial != null)
            {
                settings.groundColor = groundMaterial.GetColor("_BaseColor");
                settings.groundReflectivity = groundMaterial.GetFloat("_Smoothness");
            }

            settings.ambientColor = RenderSettings.ambientLight;
            settings.ambientIntensity = RenderSettings.ambientIntensity;

            foreach (Light light in environmentLights)
            {
                if (light == null) continue;

                if (light.name == "KeyLight")
                {
                    settings.mainLightIntensity = light.intensity;
                    settings.mainLightColor = light.color;
                }
                else if (light.name == "FillLight")
                {
                    settings.fillLightIntensity = light.intensity;
                }
                else if (light.name == "RimLight")
                {
                    settings.rimLightIntensity = light.intensity;
                }
            }

            return settings;
        }

        private void LerpSettings(EnvironmentSettings from, EnvironmentSettings to, float t)
        {
            if (mainCamera != null)
                mainCamera.backgroundColor = Color.Lerp(from.backgroundColor, to.backgroundColor, t);

            topColor = Color.Lerp(from.gradientTop, to.gradientTop, t);
            bottomColor = Color.Lerp(from.gradientBottom, to.gradientBottom, t);

            if (groundMaterial != null)
            {
                groundMaterial.SetColor("_BaseColor", Color.Lerp(from.groundColor, to.groundColor, t));
                groundMaterial.SetFloat("_Smoothness", Mathf.Lerp(from.groundReflectivity, to.groundReflectivity, t));
            }

            RenderSettings.ambientLight = Color.Lerp(from.ambientColor, to.ambientColor, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(from.ambientIntensity, to.ambientIntensity, t);

            foreach (Light light in environmentLights)
            {
                if (light == null) continue;

                if (light.name == "KeyLight")
                {
                    light.intensity = Mathf.Lerp(from.mainLightIntensity, to.mainLightIntensity, t);
                    light.color = Color.Lerp(from.mainLightColor, to.mainLightColor, t);
                }
                else if (light.name == "FillLight")
                {
                    light.intensity = Mathf.Lerp(from.fillLightIntensity, to.fillLightIntensity, t);
                }
                else if (light.name == "RimLight")
                {
                    light.intensity = Mathf.Lerp(from.rimLightIntensity, to.rimLightIntensity, t);
                }
            }

            if (to.enableFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = Color.Lerp(from.fogColor, to.fogColor, t);
                RenderSettings.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
            }
        }

        #endregion

        #region Dynamic Effects

        private void Update()
        {
            if (enableBreathing)
            {
                ApplyBreathingEffect();
            }

            // Update spotlight to follow model center
            if (enableSpotlight && spotLight != null && modelCenter != null)
            {
                spotLight.transform.LookAt(modelCenter);
            }
        }

        private void ApplyBreathingEffect()
        {
            // Subtle ambient light breathing
            float breathingValue = 1f + Mathf.Sin(Time.time * breathingSpeed * Mathf.PI * 2f) * breathingIntensity;

            RenderSettings.ambientIntensity = presets[currentPreset].ambientIntensity * breathingValue;
        }

        public void SetModelCenter(Transform center)
        {
            modelCenter = center;
        }

        #endregion

        #region Public API

        public void SetGroundVisibility(bool visible)
        {
            showGroundPlane = visible;
            if (groundPlaneObject != null)
                groundPlaneObject.SetActive(visible);
        }

        public void SetAmbientParticles(bool enabled)
        {
            enableAmbientParticles = enabled;
            if (dustParticles != null)
            {
                if (enabled)
                    dustParticles.Play();
                else
                    dustParticles.Stop();
            }
        }

        public void SetVignetteIntensity(float intensity)
        {
            vignetteIntensity = intensity;
            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                if (postProcessVolume.profile.TryGet(out Vignette vignette))
                {
                    vignette.intensity.value = intensity;
                }
            }
        }

        public EnvironmentPreset GetCurrentPreset()
        {
            return currentPreset;
        }

        public List<string> GetAvailablePresets()
        {
            List<string> names = new List<string>();
            foreach (var preset in presets)
            {
                names.Add(preset.Value.name);
            }
            return names;
        }

        #endregion

        private void OnDestroy()
        {
            if (groundPlaneObject != null)
                Destroy(groundPlaneObject);

            if (ambientLightRig != null)
                Destroy(ambientLightRig);

            if (groundMaterial != null)
                Destroy(groundMaterial);
        }
    }
}

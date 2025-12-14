using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

namespace CardiacVR.Rendering
{
    /// <summary>
    /// Professional lighting setup for medical visualization
    /// Provides optimized lighting configurations for different scenarios
    /// </summary>
    public class ProfessionalLightingSetup : MonoBehaviour
    {
        public static ProfessionalLightingSetup Instance { get; private set; }

        [Header("Main Lights")]
        [SerializeField] private Light mainLight;
        [SerializeField] private Light fillLight;
        [SerializeField] private Light rimLight;
        [SerializeField] private Light ambientLight;

        [Header("Environment")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private ReflectionProbe reflectionProbe;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 1f;

        private LightingPreset currentPreset;
        private Coroutine transitionCoroutine;

        public enum LightingMode
        {
            Surgical,           // Bright, clinical lighting
            Presentation,       // Dramatic, presentation-ready
            Education,          // Soft, comfortable for learning
            VR_Comfort,         // Optimized for VR viewing
            Cinematic,          // Dramatic with depth
            Photography,        // Studio-quality
            Examination,        // Detailed inspection
            Night_Mode          // Low strain for dark environments
        }

        [System.Serializable]
        public class LightingPreset
        {
            public string name;

            // Main light settings
            public Color mainLightColor = Color.white;
            public float mainLightIntensity = 1f;
            public Vector3 mainLightRotation = new Vector3(50, -30, 0);
            public LightShadows mainLightShadows = LightShadows.Soft;
            public float mainLightShadowStrength = 0.5f;

            // Fill light settings
            public Color fillLightColor = new Color(0.8f, 0.85f, 0.9f, 1f);
            public float fillLightIntensity = 0.5f;
            public Vector3 fillLightRotation = new Vector3(30, 120, 0);
            public bool fillLightEnabled = true;

            // Rim light settings
            public Color rimLightColor = new Color(0.9f, 0.95f, 1f, 1f);
            public float rimLightIntensity = 0.8f;
            public Vector3 rimLightRotation = new Vector3(10, -150, 0);
            public bool rimLightEnabled = true;

            // Ambient settings
            public AmbientMode ambientMode = AmbientMode.Flat;
            public Color ambientColor = new Color(0.2f, 0.22f, 0.25f, 1f);
            public Color ambientSkyColor = new Color(0.3f, 0.35f, 0.4f, 1f);
            public Color ambientEquatorColor = new Color(0.25f, 0.28f, 0.3f, 1f);
            public Color ambientGroundColor = new Color(0.15f, 0.17f, 0.2f, 1f);
            public float ambientIntensity = 1f;

            // Reflection settings
            public float reflectionIntensity = 0.5f;
            public int reflectionBounces = 1;

            // Fog settings
            public bool fogEnabled = false;
            public Color fogColor = new Color(0.2f, 0.22f, 0.25f, 1f);
            public float fogDensity = 0.01f;
            public FogMode fogMode = FogMode.ExponentialSquared;

            // Skybox
            public Color skyboxTint = Color.white;
            public float skyboxExposure = 1f;
        }

        private Dictionary<LightingMode, LightingPreset> lightingPresets;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializePresets();
                SetupLights();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializePresets()
        {
            lightingPresets = new Dictionary<LightingMode, LightingPreset>
            {
                [LightingMode.Surgical] = new LightingPreset
                {
                    name = "Surgical",
                    mainLightColor = new Color(1f, 0.98f, 0.95f, 1f),
                    mainLightIntensity = 1.5f,
                    mainLightRotation = new Vector3(60, 0, 0),
                    mainLightShadows = LightShadows.Soft,
                    mainLightShadowStrength = 0.3f,

                    fillLightColor = new Color(0.95f, 0.97f, 1f, 1f),
                    fillLightIntensity = 0.8f,
                    fillLightRotation = new Vector3(30, 90, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(0.9f, 0.92f, 0.95f, 1f),
                    rimLightIntensity = 0.6f,
                    rimLightRotation = new Vector3(20, 180, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Flat,
                    ambientColor = new Color(0.35f, 0.37f, 0.4f, 1f),
                    ambientIntensity = 1.2f,

                    reflectionIntensity = 0.3f,
                    fogEnabled = false
                },

                [LightingMode.Presentation] = new LightingPreset
                {
                    name = "Presentation",
                    mainLightColor = new Color(1f, 0.97f, 0.92f, 1f),
                    mainLightIntensity = 1.2f,
                    mainLightRotation = new Vector3(45, -30, 0),
                    mainLightShadows = LightShadows.Soft,
                    mainLightShadowStrength = 0.5f,

                    fillLightColor = new Color(0.85f, 0.9f, 1f, 1f),
                    fillLightIntensity = 0.4f,
                    fillLightRotation = new Vector3(30, 120, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(0.95f, 0.98f, 1f, 1f),
                    rimLightIntensity = 1f,
                    rimLightRotation = new Vector3(10, -150, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Trilight,
                    ambientSkyColor = new Color(0.25f, 0.3f, 0.4f, 1f),
                    ambientEquatorColor = new Color(0.18f, 0.2f, 0.25f, 1f),
                    ambientGroundColor = new Color(0.1f, 0.12f, 0.15f, 1f),
                    ambientIntensity = 1f,

                    reflectionIntensity = 0.6f,
                    fogEnabled = true,
                    fogColor = new Color(0.12f, 0.14f, 0.18f, 1f),
                    fogDensity = 0.005f
                },

                [LightingMode.Education] = new LightingPreset
                {
                    name = "Education",
                    mainLightColor = new Color(1f, 0.98f, 0.96f, 1f),
                    mainLightIntensity = 1f,
                    mainLightRotation = new Vector3(50, -20, 0),
                    mainLightShadows = LightShadows.Soft,
                    mainLightShadowStrength = 0.4f,

                    fillLightColor = new Color(0.9f, 0.92f, 0.98f, 1f),
                    fillLightIntensity = 0.6f,
                    fillLightRotation = new Vector3(35, 100, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(0.85f, 0.9f, 0.95f, 1f),
                    rimLightIntensity = 0.5f,
                    rimLightRotation = new Vector3(15, -140, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Flat,
                    ambientColor = new Color(0.3f, 0.32f, 0.35f, 1f),
                    ambientIntensity = 1.1f,

                    reflectionIntensity = 0.4f,
                    fogEnabled = false
                },

                [LightingMode.VR_Comfort] = new LightingPreset
                {
                    name = "VR Comfort",
                    mainLightColor = new Color(1f, 0.98f, 0.95f, 1f),
                    mainLightIntensity = 0.9f,
                    mainLightRotation = new Vector3(55, -25, 0),
                    mainLightShadows = LightShadows.Hard, // Less intensive for VR
                    mainLightShadowStrength = 0.35f,

                    fillLightColor = new Color(0.88f, 0.9f, 0.95f, 1f),
                    fillLightIntensity = 0.55f,
                    fillLightRotation = new Vector3(30, 110, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(0.85f, 0.88f, 0.92f, 1f),
                    rimLightIntensity = 0.4f,
                    rimLightRotation = new Vector3(15, -160, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Flat,
                    ambientColor = new Color(0.28f, 0.3f, 0.33f, 1f),
                    ambientIntensity = 1f,

                    reflectionIntensity = 0.3f,
                    fogEnabled = false // No fog in VR for comfort
                },

                [LightingMode.Cinematic] = new LightingPreset
                {
                    name = "Cinematic",
                    mainLightColor = new Color(1f, 0.95f, 0.85f, 1f),
                    mainLightIntensity = 1.4f,
                    mainLightRotation = new Vector3(35, -45, 0),
                    mainLightShadows = LightShadows.Soft,
                    mainLightShadowStrength = 0.7f,

                    fillLightColor = new Color(0.7f, 0.8f, 1f, 1f),
                    fillLightIntensity = 0.25f,
                    fillLightRotation = new Vector3(25, 135, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(1f, 0.95f, 0.9f, 1f),
                    rimLightIntensity = 1.2f,
                    rimLightRotation = new Vector3(5, -165, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Trilight,
                    ambientSkyColor = new Color(0.15f, 0.18f, 0.25f, 1f),
                    ambientEquatorColor = new Color(0.1f, 0.12f, 0.15f, 1f),
                    ambientGroundColor = new Color(0.05f, 0.06f, 0.08f, 1f),
                    ambientIntensity = 0.8f,

                    reflectionIntensity = 0.7f,
                    fogEnabled = true,
                    fogColor = new Color(0.08f, 0.1f, 0.15f, 1f),
                    fogDensity = 0.008f
                },

                [LightingMode.Photography] = new LightingPreset
                {
                    name = "Photography",
                    mainLightColor = new Color(1f, 1f, 1f, 1f),
                    mainLightIntensity = 1.3f,
                    mainLightRotation = new Vector3(45, -30, 0),
                    mainLightShadows = LightShadows.Soft,
                    mainLightShadowStrength = 0.4f,

                    fillLightColor = new Color(0.95f, 0.97f, 1f, 1f),
                    fillLightIntensity = 0.7f,
                    fillLightRotation = new Vector3(30, 90, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(1f, 1f, 1f, 1f),
                    rimLightIntensity = 0.9f,
                    rimLightRotation = new Vector3(10, 180, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Flat,
                    ambientColor = new Color(0.4f, 0.42f, 0.45f, 1f),
                    ambientIntensity = 1f,

                    reflectionIntensity = 0.5f,
                    fogEnabled = false
                },

                [LightingMode.Examination] = new LightingPreset
                {
                    name = "Examination",
                    mainLightColor = new Color(1f, 0.99f, 0.97f, 1f),
                    mainLightIntensity = 1.6f,
                    mainLightRotation = new Vector3(70, 0, 0),
                    mainLightShadows = LightShadows.Soft,
                    mainLightShadowStrength = 0.25f,

                    fillLightColor = new Color(0.97f, 0.98f, 1f, 1f),
                    fillLightIntensity = 0.9f,
                    fillLightRotation = new Vector3(40, 90, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(0.95f, 0.97f, 1f, 1f),
                    rimLightIntensity = 0.7f,
                    rimLightRotation = new Vector3(30, -90, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Flat,
                    ambientColor = new Color(0.4f, 0.42f, 0.45f, 1f),
                    ambientIntensity = 1.3f,

                    reflectionIntensity = 0.4f,
                    fogEnabled = false
                },

                [LightingMode.Night_Mode] = new LightingPreset
                {
                    name = "Night Mode",
                    mainLightColor = new Color(0.8f, 0.85f, 0.95f, 1f),
                    mainLightIntensity = 0.6f,
                    mainLightRotation = new Vector3(50, -30, 0),
                    mainLightShadows = LightShadows.Soft,
                    mainLightShadowStrength = 0.5f,

                    fillLightColor = new Color(0.7f, 0.75f, 0.85f, 1f),
                    fillLightIntensity = 0.3f,
                    fillLightRotation = new Vector3(30, 120, 0),
                    fillLightEnabled = true,

                    rimLightColor = new Color(0.6f, 0.7f, 0.85f, 1f),
                    rimLightIntensity = 0.4f,
                    rimLightRotation = new Vector3(10, -150, 0),
                    rimLightEnabled = true,

                    ambientMode = AmbientMode.Flat,
                    ambientColor = new Color(0.1f, 0.12f, 0.15f, 1f),
                    ambientIntensity = 0.7f,

                    reflectionIntensity = 0.3f,
                    fogEnabled = false
                }
            };
        }

        private void SetupLights()
        {
            // Create main light if not assigned
            if (mainLight == null)
            {
                GameObject mainLightObj = new GameObject("Main Light");
                mainLightObj.transform.SetParent(transform);
                mainLight = mainLightObj.AddComponent<Light>();
                mainLight.type = LightType.Directional;
            }

            // Create fill light if not assigned
            if (fillLight == null)
            {
                GameObject fillLightObj = new GameObject("Fill Light");
                fillLightObj.transform.SetParent(transform);
                fillLight = fillLightObj.AddComponent<Light>();
                fillLight.type = LightType.Directional;
            }

            // Create rim light if not assigned
            if (rimLight == null)
            {
                GameObject rimLightObj = new GameObject("Rim Light");
                rimLightObj.transform.SetParent(transform);
                rimLight = rimLightObj.AddComponent<Light>();
                rimLight.type = LightType.Directional;
            }

            // Apply default preset
            ApplyPreset(LightingMode.Education, false);
        }

        #region Public Methods

        public void SetLightingMode(LightingMode mode, bool animated = true)
        {
            if (transitionCoroutine != null)
                StopCoroutine(transitionCoroutine);

            if (animated)
                transitionCoroutine = StartCoroutine(TransitionToPreset(lightingPresets[mode]));
            else
                ApplyPreset(mode, false);
        }

        public void ApplyPreset(LightingMode mode, bool animated = true)
        {
            if (lightingPresets.TryGetValue(mode, out LightingPreset preset))
            {
                if (animated)
                {
                    if (transitionCoroutine != null)
                        StopCoroutine(transitionCoroutine);
                    transitionCoroutine = StartCoroutine(TransitionToPreset(preset));
                }
                else
                {
                    ApplyPresetImmediate(preset);
                }
            }
        }

        public void ApplyCustomPreset(LightingPreset preset, bool animated = true)
        {
            if (animated)
            {
                if (transitionCoroutine != null)
                    StopCoroutine(transitionCoroutine);
                transitionCoroutine = StartCoroutine(TransitionToPreset(preset));
            }
            else
            {
                ApplyPresetImmediate(preset);
            }
        }

        public LightingPreset GetCurrentPreset()
        {
            return currentPreset;
        }

        public LightingPreset GetPreset(LightingMode mode)
        {
            return lightingPresets.TryGetValue(mode, out LightingPreset preset) ? preset : null;
        }

        #endregion

        #region Preset Application

        private void ApplyPresetImmediate(LightingPreset preset)
        {
            currentPreset = preset;

            // Main light
            if (mainLight != null)
            {
                mainLight.color = preset.mainLightColor;
                mainLight.intensity = preset.mainLightIntensity;
                mainLight.transform.eulerAngles = preset.mainLightRotation;
                mainLight.shadows = preset.mainLightShadows;
                mainLight.shadowStrength = preset.mainLightShadowStrength;
            }

            // Fill light
            if (fillLight != null)
            {
                fillLight.gameObject.SetActive(preset.fillLightEnabled);
                fillLight.color = preset.fillLightColor;
                fillLight.intensity = preset.fillLightIntensity;
                fillLight.transform.eulerAngles = preset.fillLightRotation;
                fillLight.shadows = LightShadows.None;
            }

            // Rim light
            if (rimLight != null)
            {
                rimLight.gameObject.SetActive(preset.rimLightEnabled);
                rimLight.color = preset.rimLightColor;
                rimLight.intensity = preset.rimLightIntensity;
                rimLight.transform.eulerAngles = preset.rimLightRotation;
                rimLight.shadows = LightShadows.None;
            }

            // Ambient
            RenderSettings.ambientMode = preset.ambientMode;
            if (preset.ambientMode == AmbientMode.Flat)
            {
                RenderSettings.ambientLight = preset.ambientColor;
            }
            else if (preset.ambientMode == AmbientMode.Trilight)
            {
                RenderSettings.ambientSkyColor = preset.ambientSkyColor;
                RenderSettings.ambientEquatorColor = preset.ambientEquatorColor;
                RenderSettings.ambientGroundColor = preset.ambientGroundColor;
            }
            RenderSettings.ambientIntensity = preset.ambientIntensity;

            // Reflection
            RenderSettings.reflectionIntensity = preset.reflectionIntensity;
            RenderSettings.reflectionBounces = preset.reflectionBounces;

            // Fog
            RenderSettings.fog = preset.fogEnabled;
            if (preset.fogEnabled)
            {
                RenderSettings.fogColor = preset.fogColor;
                RenderSettings.fogMode = preset.fogMode;
                RenderSettings.fogDensity = preset.fogDensity;
            }

            // Skybox
            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetColor("_Tint", preset.skyboxTint);
                skyboxMaterial.SetFloat("_Exposure", preset.skyboxExposure);
            }

            // Update reflection probe
            if (reflectionProbe != null)
            {
                reflectionProbe.RenderProbe();
            }
        }

        private IEnumerator TransitionToPreset(LightingPreset targetPreset)
        {
            LightingPreset startPreset = CaptureCurrentState();
            float elapsed = 0;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / transitionDuration;

                // Smooth step for more natural transitions
                t = t * t * (3f - 2f * t);

                LerpPresets(startPreset, targetPreset, t);
                yield return null;
            }

            ApplyPresetImmediate(targetPreset);
        }

        private LightingPreset CaptureCurrentState()
        {
            LightingPreset state = new LightingPreset();

            if (mainLight != null)
            {
                state.mainLightColor = mainLight.color;
                state.mainLightIntensity = mainLight.intensity;
                state.mainLightRotation = mainLight.transform.eulerAngles;
                state.mainLightShadowStrength = mainLight.shadowStrength;
            }

            if (fillLight != null)
            {
                state.fillLightEnabled = fillLight.gameObject.activeSelf;
                state.fillLightColor = fillLight.color;
                state.fillLightIntensity = fillLight.intensity;
                state.fillLightRotation = fillLight.transform.eulerAngles;
            }

            if (rimLight != null)
            {
                state.rimLightEnabled = rimLight.gameObject.activeSelf;
                state.rimLightColor = rimLight.color;
                state.rimLightIntensity = rimLight.intensity;
                state.rimLightRotation = rimLight.transform.eulerAngles;
            }

            state.ambientMode = RenderSettings.ambientMode;
            state.ambientColor = RenderSettings.ambientLight;
            state.ambientSkyColor = RenderSettings.ambientSkyColor;
            state.ambientEquatorColor = RenderSettings.ambientEquatorColor;
            state.ambientGroundColor = RenderSettings.ambientGroundColor;
            state.ambientIntensity = RenderSettings.ambientIntensity;

            state.reflectionIntensity = RenderSettings.reflectionIntensity;

            state.fogEnabled = RenderSettings.fog;
            state.fogColor = RenderSettings.fogColor;
            state.fogDensity = RenderSettings.fogDensity;

            return state;
        }

        private void LerpPresets(LightingPreset from, LightingPreset to, float t)
        {
            // Main light
            if (mainLight != null)
            {
                mainLight.color = Color.Lerp(from.mainLightColor, to.mainLightColor, t);
                mainLight.intensity = Mathf.Lerp(from.mainLightIntensity, to.mainLightIntensity, t);
                mainLight.transform.eulerAngles = LerpEuler(from.mainLightRotation, to.mainLightRotation, t);
                mainLight.shadowStrength = Mathf.Lerp(from.mainLightShadowStrength, to.mainLightShadowStrength, t);
            }

            // Fill light
            if (fillLight != null)
            {
                fillLight.color = Color.Lerp(from.fillLightColor, to.fillLightColor, t);
                fillLight.intensity = Mathf.Lerp(from.fillLightIntensity, to.fillLightIntensity, t);
                fillLight.transform.eulerAngles = LerpEuler(from.fillLightRotation, to.fillLightRotation, t);
            }

            // Rim light
            if (rimLight != null)
            {
                rimLight.color = Color.Lerp(from.rimLightColor, to.rimLightColor, t);
                rimLight.intensity = Mathf.Lerp(from.rimLightIntensity, to.rimLightIntensity, t);
                rimLight.transform.eulerAngles = LerpEuler(from.rimLightRotation, to.rimLightRotation, t);
            }

            // Ambient
            RenderSettings.ambientLight = Color.Lerp(from.ambientColor, to.ambientColor, t);
            RenderSettings.ambientSkyColor = Color.Lerp(from.ambientSkyColor, to.ambientSkyColor, t);
            RenderSettings.ambientEquatorColor = Color.Lerp(from.ambientEquatorColor, to.ambientEquatorColor, t);
            RenderSettings.ambientGroundColor = Color.Lerp(from.ambientGroundColor, to.ambientGroundColor, t);
            RenderSettings.ambientIntensity = Mathf.Lerp(from.ambientIntensity, to.ambientIntensity, t);

            // Reflection
            RenderSettings.reflectionIntensity = Mathf.Lerp(from.reflectionIntensity, to.reflectionIntensity, t);

            // Fog
            RenderSettings.fogColor = Color.Lerp(from.fogColor, to.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
        }

        private Vector3 LerpEuler(Vector3 from, Vector3 to, float t)
        {
            return new Vector3(
                Mathf.LerpAngle(from.x, to.x, t),
                Mathf.LerpAngle(from.y, to.y, t),
                Mathf.LerpAngle(from.z, to.z, t)
            );
        }

        #endregion

        #region Light Manipulation

        public void RotateMainLightAround(Vector3 pivot, float horizontalAngle, float verticalAngle, float duration = 0.5f)
        {
            StartCoroutine(RotateLightAroundCoroutine(mainLight, pivot, horizontalAngle, verticalAngle, duration));
        }

        private IEnumerator RotateLightAroundCoroutine(Light light, Vector3 pivot, float horizontalAngle,
            float verticalAngle, float duration)
        {
            if (light == null) yield break;

            Vector3 startRotation = light.transform.eulerAngles;
            Vector3 targetRotation = new Vector3(
                startRotation.x + verticalAngle,
                startRotation.y + horizontalAngle,
                startRotation.z
            );

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t); // Smooth step

                light.transform.eulerAngles = LerpEuler(startRotation, targetRotation, t);
                yield return null;
            }

            light.transform.eulerAngles = targetRotation;
        }

        public void SetMainLightIntensity(float intensity, bool animated = true, float duration = 0.3f)
        {
            if (animated)
                StartCoroutine(AnimateLightIntensity(mainLight, intensity, duration));
            else if (mainLight != null)
                mainLight.intensity = intensity;
        }

        private IEnumerator AnimateLightIntensity(Light light, float targetIntensity, float duration)
        {
            if (light == null) yield break;

            float startIntensity = light.intensity;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                light.intensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                yield return null;
            }

            light.intensity = targetIntensity;
        }

        public void HighlightObject(GameObject target, float intensity = 0.5f, Color? color = null)
        {
            if (target == null) return;

            // Create temporary spotlight for highlight
            GameObject spotObj = new GameObject("Highlight Light");
            spotObj.transform.position = target.transform.position + Vector3.up * 2f;
            spotObj.transform.LookAt(target.transform);

            Light spot = spotObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = color ?? Color.white;
            spot.intensity = 0;
            spot.range = 5f;
            spot.spotAngle = 45f;

            StartCoroutine(HighlightCoroutine(spot, intensity, 0.3f));
        }

        private IEnumerator HighlightCoroutine(Light spotlight, float targetIntensity, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                spotlight.intensity = Mathf.Lerp(0, targetIntensity, elapsed / duration);
                yield return null;
            }
        }

        public void RemoveHighlight(float fadeDuration = 0.3f)
        {
            Light[] spotlights = FindObjectsOfType<Light>();
            foreach (var light in spotlights)
            {
                if (light.gameObject.name == "Highlight Light")
                {
                    StartCoroutine(FadeAndDestroyLight(light, fadeDuration));
                }
            }
        }

        private IEnumerator FadeAndDestroyLight(Light light, float duration)
        {
            float startIntensity = light.intensity;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                light.intensity = Mathf.Lerp(startIntensity, 0, elapsed / duration);
                yield return null;
            }

            Destroy(light.gameObject);
        }

        #endregion

        #region Utility Methods

        public void OptimizeForVR()
        {
            SetLightingMode(LightingMode.VR_Comfort);

            // Additional VR optimizations
            if (mainLight != null)
                mainLight.shadows = LightShadows.Hard; // Less expensive shadows

            // Disable volumetric effects
            RenderSettings.fog = false;
        }

        public void OptimizeForMobile()
        {
            // Use simpler lighting
            if (fillLight != null)
                fillLight.gameObject.SetActive(false);

            if (rimLight != null)
                rimLight.gameObject.SetActive(false);

            if (mainLight != null)
                mainLight.shadows = LightShadows.None;

            RenderSettings.fog = false;
            RenderSettings.reflectionIntensity = 0;
        }

        public void RestoreFullQuality()
        {
            if (currentPreset != null)
                ApplyPresetImmediate(currentPreset);
            else
                SetLightingMode(LightingMode.Presentation, false);
        }

        #endregion
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;

/// <summary>
/// Professional post-processing manager for medical visualization.
/// Provides smooth, elegant visual effects optimized for VR cardiac surgery planning.
/// </summary>
public class PostProcessingManager : MonoBehaviour
{
    public static PostProcessingManager Instance { get; private set; }

    public event EventHandler<PresetChangedEventArgs> OnPresetChanged;

    public class PresetChangedEventArgs : EventArgs
    {
        public VisualPreset Preset;
    }

    public enum VisualPreset
    {
        Clinical,           // Clean, neutral for medical accuracy
        Presentation,       // Enhanced for presentations
        Education,          // Friendly, warm for patient education
        Surgery,            // High contrast for surgical planning
        Cinematic,          // Dramatic for recordings
        VR_Comfort,         // Optimized for VR comfort
        Custom
    }

    [Header("Volume Profile")]
    [SerializeField] private Volume globalVolume;
    [SerializeField] private VolumeProfile volumeProfile;

    [Header("Current Settings")]
    [SerializeField] private VisualPreset currentPreset = VisualPreset.Clinical;

    [Header("Bloom Settings")]
    [SerializeField] private bool enableBloom = true;
    [SerializeField, Range(0, 2)] private float bloomIntensity = 0.3f;
    [SerializeField, Range(0, 1)] private float bloomThreshold = 0.9f;
    [SerializeField, Range(0, 1)] private float bloomScatter = 0.7f;
    [SerializeField] private Color bloomTint = Color.white;

    [Header("Color Adjustments")]
    [SerializeField, Range(-100, 100)] private float colorTemperature = 0f;
    [SerializeField, Range(-100, 100)] private float colorTint = 0f;
    [SerializeField, Range(-100, 100)] private float saturation = 10f;
    [SerializeField, Range(-100, 100)] private float contrast = 5f;
    [SerializeField, Range(0.2f, 5)] private float postExposure = 1f;

    [Header("Vignette")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField, Range(0, 1)] private float vignetteIntensity = 0.25f;
    [SerializeField, Range(0, 1)] private float vignetteSmoothness = 0.4f;
    [SerializeField] private Color vignetteColor = Color.black;

    [Header("Depth of Field")]
    [SerializeField] private bool enableDOF = false;
    [SerializeField] private DepthOfFieldMode dofMode = DepthOfFieldMode.Bokeh;
    [SerializeField, Range(0.1f, 10)] private float focusDistance = 2f;
    [SerializeField, Range(1, 32)] private float aperture = 5.6f;

    [Header("Ambient Occlusion")]
    [SerializeField] private bool enableAO = true;
    [SerializeField, Range(0, 4)] private float aoIntensity = 0.5f;
    [SerializeField, Range(0.25f, 5)] private float aoRadius = 0.3f;

    [Header("Anti-Aliasing")]
    [SerializeField] private AntialiasingMode antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    [SerializeField] private AntialiasingQuality aaQuality = AntialiasingQuality.High;

    [Header("Motion Blur")]
    [SerializeField] private bool enableMotionBlur = false;
    [SerializeField, Range(0, 1)] private float motionBlurIntensity = 0.2f;

    [Header("Film Grain")]
    [SerializeField] private bool enableFilmGrain = false;
    [SerializeField, Range(0, 1)] private float filmGrainIntensity = 0.1f;

    [Header("Chromatic Aberration")]
    [SerializeField] private bool enableChromaticAberration = false;
    [SerializeField, Range(0, 1)] private float chromaticAberrationIntensity = 0.05f;

    [Header("Lens Distortion")]
    [SerializeField] private bool enableLensDistortion = false;
    [SerializeField, Range(-1, 1)] private float lensDistortionIntensity = 0f;

    // Volume components
    private Bloom bloom;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    private DepthOfField depthOfField;
    private MotionBlur motionBlur;
    private FilmGrain filmGrain;
    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;

    // Preset configurations
    private struct PresetConfig
    {
        public float BloomIntensity;
        public float BloomThreshold;
        public float Saturation;
        public float Contrast;
        public float PostExposure;
        public float Temperature;
        public float VignetteIntensity;
        public bool EnableAO;
        public float AOIntensity;
        public bool EnableDOF;
        public bool EnableMotionBlur;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeVolume();
        ApplyPreset(currentPreset);
    }

    private void InitializeVolume()
    {
        if (globalVolume == null)
        {
            globalVolume = FindObjectOfType<Volume>();

            if (globalVolume == null)
            {
                GameObject volumeObj = new GameObject("Global Volume");
                globalVolume = volumeObj.AddComponent<Volume>();
                globalVolume.isGlobal = true;
            }
        }

        if (volumeProfile == null)
        {
            volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            globalVolume.profile = volumeProfile;
        }

        // Get or add volume components
        GetOrAddComponent<Bloom>(out bloom);
        GetOrAddComponent<ColorAdjustments>(out colorAdjustments);
        GetOrAddComponent<Vignette>(out vignette);
        GetOrAddComponent<DepthOfField>(out depthOfField);
        GetOrAddComponent<MotionBlur>(out motionBlur);
        GetOrAddComponent<FilmGrain>(out filmGrain);
        GetOrAddComponent<ChromaticAberration>(out chromaticAberration);
        GetOrAddComponent<LensDistortion>(out lensDistortion);
    }

    private void GetOrAddComponent<T>(out T component) where T : VolumeComponent
    {
        if (!volumeProfile.TryGet(out component))
        {
            component = volumeProfile.Add<T>();
        }
    }

    #region Preset Application

    /// <summary>
    /// Apply a visual preset.
    /// </summary>
    public void ApplyPreset(VisualPreset preset)
    {
        currentPreset = preset;

        PresetConfig config = GetPresetConfig(preset);

        // Apply configuration
        bloomIntensity = config.BloomIntensity;
        bloomThreshold = config.BloomThreshold;
        saturation = config.Saturation;
        contrast = config.Contrast;
        postExposure = config.PostExposure;
        colorTemperature = config.Temperature;
        vignetteIntensity = config.VignetteIntensity;
        enableAO = config.EnableAO;
        aoIntensity = config.AOIntensity;
        enableDOF = config.EnableDOF;
        enableMotionBlur = config.EnableMotionBlur;

        UpdateAllSettings();

        OnPresetChanged?.Invoke(this, new PresetChangedEventArgs { Preset = preset });

        Debug.Log($"Applied visual preset: {preset}");
    }

    private PresetConfig GetPresetConfig(VisualPreset preset)
    {
        switch (preset)
        {
            case VisualPreset.Clinical:
                return new PresetConfig
                {
                    BloomIntensity = 0.2f,
                    BloomThreshold = 0.95f,
                    Saturation = 0f,
                    Contrast = 5f,
                    PostExposure = 1f,
                    Temperature = 0f,
                    VignetteIntensity = 0.15f,
                    EnableAO = true,
                    AOIntensity = 0.4f,
                    EnableDOF = false,
                    EnableMotionBlur = false
                };

            case VisualPreset.Presentation:
                return new PresetConfig
                {
                    BloomIntensity = 0.4f,
                    BloomThreshold = 0.85f,
                    Saturation = 15f,
                    Contrast = 10f,
                    PostExposure = 1.1f,
                    Temperature = 5f,
                    VignetteIntensity = 0.3f,
                    EnableAO = true,
                    AOIntensity = 0.6f,
                    EnableDOF = false,
                    EnableMotionBlur = false
                };

            case VisualPreset.Education:
                return new PresetConfig
                {
                    BloomIntensity = 0.35f,
                    BloomThreshold = 0.88f,
                    Saturation = 20f,
                    Contrast = 8f,
                    PostExposure = 1.15f,
                    Temperature = 10f,
                    VignetteIntensity = 0.2f,
                    EnableAO = true,
                    AOIntensity = 0.5f,
                    EnableDOF = false,
                    EnableMotionBlur = false
                };

            case VisualPreset.Surgery:
                return new PresetConfig
                {
                    BloomIntensity = 0.15f,
                    BloomThreshold = 0.98f,
                    Saturation = -5f,
                    Contrast = 15f,
                    PostExposure = 1.05f,
                    Temperature = -5f,
                    VignetteIntensity = 0.1f,
                    EnableAO = true,
                    AOIntensity = 0.7f,
                    EnableDOF = false,
                    EnableMotionBlur = false
                };

            case VisualPreset.Cinematic:
                return new PresetConfig
                {
                    BloomIntensity = 0.6f,
                    BloomThreshold = 0.8f,
                    Saturation = 10f,
                    Contrast = 20f,
                    PostExposure = 1.2f,
                    Temperature = -10f,
                    VignetteIntensity = 0.4f,
                    EnableAO = true,
                    AOIntensity = 0.8f,
                    EnableDOF = true,
                    EnableMotionBlur = true
                };

            case VisualPreset.VR_Comfort:
                return new PresetConfig
                {
                    BloomIntensity = 0.25f,
                    BloomThreshold = 0.92f,
                    Saturation = 5f,
                    Contrast = 0f,
                    PostExposure = 1f,
                    Temperature = 0f,
                    VignetteIntensity = 0f,
                    EnableAO = false,
                    AOIntensity = 0f,
                    EnableDOF = false,
                    EnableMotionBlur = false
                };

            default:
                return GetPresetConfig(VisualPreset.Clinical);
        }
    }

    #endregion

    #region Settings Update

    private void UpdateAllSettings()
    {
        UpdateBloom();
        UpdateColorAdjustments();
        UpdateVignette();
        UpdateDepthOfField();
        UpdateMotionBlur();
        UpdateFilmGrain();
        UpdateChromaticAberration();
        UpdateLensDistortion();
    }

    private void UpdateBloom()
    {
        if (bloom == null) return;

        bloom.active = enableBloom;
        bloom.intensity.Override(bloomIntensity);
        bloom.threshold.Override(bloomThreshold);
        bloom.scatter.Override(bloomScatter);
        bloom.tint.Override(bloomTint);
        bloom.highQualityFiltering.Override(true);
    }

    private void UpdateColorAdjustments()
    {
        if (colorAdjustments == null) return;

        colorAdjustments.active = true;
        colorAdjustments.colorFilter.Override(Color.white);
        colorAdjustments.postExposure.Override(postExposure);
        colorAdjustments.contrast.Override(contrast);
        colorAdjustments.saturation.Override(saturation);
        colorAdjustments.hueShift.Override(0f);
    }

    private void UpdateVignette()
    {
        if (vignette == null) return;

        vignette.active = enableVignette;
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(vignetteSmoothness);
        vignette.color.Override(vignetteColor);
        vignette.rounded.Override(true);
    }

    private void UpdateDepthOfField()
    {
        if (depthOfField == null) return;

        depthOfField.active = enableDOF;
        depthOfField.mode.Override(dofMode);
        depthOfField.focusDistance.Override(focusDistance);
        depthOfField.aperture.Override(aperture);
        depthOfField.highQualitySampling.Override(true);
    }

    private void UpdateMotionBlur()
    {
        if (motionBlur == null) return;

        motionBlur.active = enableMotionBlur;
        motionBlur.intensity.Override(motionBlurIntensity);
        motionBlur.quality.Override(MotionBlurQuality.High);
    }

    private void UpdateFilmGrain()
    {
        if (filmGrain == null) return;

        filmGrain.active = enableFilmGrain;
        filmGrain.intensity.Override(filmGrainIntensity);
        filmGrain.type.Override(FilmGrainLookup.Medium1);
    }

    private void UpdateChromaticAberration()
    {
        if (chromaticAberration == null) return;

        chromaticAberration.active = enableChromaticAberration;
        chromaticAberration.intensity.Override(chromaticAberrationIntensity);
    }

    private void UpdateLensDistortion()
    {
        if (lensDistortion == null) return;

        lensDistortion.active = enableLensDistortion;
        lensDistortion.intensity.Override(lensDistortionIntensity);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Set bloom intensity.
    /// </summary>
    public void SetBloomIntensity(float intensity)
    {
        bloomIntensity = Mathf.Clamp(intensity, 0f, 2f);
        UpdateBloom();
    }

    /// <summary>
    /// Set saturation.
    /// </summary>
    public void SetSaturation(float value)
    {
        saturation = Mathf.Clamp(value, -100f, 100f);
        UpdateColorAdjustments();
    }

    /// <summary>
    /// Set contrast.
    /// </summary>
    public void SetContrast(float value)
    {
        contrast = Mathf.Clamp(value, -100f, 100f);
        UpdateColorAdjustments();
    }

    /// <summary>
    /// Set exposure.
    /// </summary>
    public void SetExposure(float value)
    {
        postExposure = Mathf.Clamp(value, 0.2f, 5f);
        UpdateColorAdjustments();
    }

    /// <summary>
    /// Set vignette intensity.
    /// </summary>
    public void SetVignetteIntensity(float intensity)
    {
        vignetteIntensity = Mathf.Clamp01(intensity);
        UpdateVignette();
    }

    /// <summary>
    /// Enable/disable depth of field.
    /// </summary>
    public void SetDepthOfField(bool enabled, float focusDist = 2f)
    {
        enableDOF = enabled;
        focusDistance = focusDist;
        UpdateDepthOfField();
    }

    /// <summary>
    /// Set focus distance for DOF.
    /// </summary>
    public void SetFocusDistance(float distance)
    {
        focusDistance = Mathf.Max(0.1f, distance);
        UpdateDepthOfField();
    }

    /// <summary>
    /// Enable/disable ambient occlusion.
    /// </summary>
    public void SetAmbientOcclusion(bool enabled, float intensity = 0.5f)
    {
        enableAO = enabled;
        aoIntensity = Mathf.Clamp(intensity, 0f, 4f);
        // Note: SSAO is configured in URP settings, not volume
    }

    /// <summary>
    /// Get current preset.
    /// </summary>
    public VisualPreset GetCurrentPreset()
    {
        return currentPreset;
    }

    /// <summary>
    /// Reset to default settings.
    /// </summary>
    public void ResetToDefault()
    {
        ApplyPreset(VisualPreset.Clinical);
    }

    #endregion

    #region VR Optimization

    /// <summary>
    /// Optimize settings for VR.
    /// </summary>
    public void OptimizeForVR()
    {
        // Disable effects that cause motion sickness
        enableMotionBlur = false;
        enableChromaticAberration = false;
        enableLensDistortion = false;
        enableDOF = false;
        vignetteIntensity = 0f;

        // Reduce bloom for clarity
        bloomIntensity *= 0.7f;

        // Neutral color grading
        saturation = 0f;
        contrast = 0f;

        UpdateAllSettings();

        Debug.Log("Optimized post-processing for VR");
    }

    /// <summary>
    /// Enable comfortable VR vignette (reduces motion sickness).
    /// </summary>
    public void EnableComfortVignette(bool enable)
    {
        if (enable)
        {
            enableVignette = true;
            vignetteIntensity = 0.4f;
            vignetteSmoothness = 0.6f;
        }
        else
        {
            vignetteIntensity = 0f;
        }
        UpdateVignette();
    }

    #endregion
}

public enum AntialiasingMode
{
    None,
    FastApproximateAntialiasing,
    SubpixelMorphologicalAntiAliasing
}

public enum AntialiasingQuality
{
    Low,
    Medium,
    High
}

// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using CardiacVR.Rendering;
using CardiacVR.Animation;
using CardiacVR.UI;

namespace CardiacVR.Core
{
    /// <summary>
    /// Central integration manager for the CardiacVR visualization platform.
    /// Coordinates all rendering, UI, animation, and effect systems for seamless operation.
    /// </summary>
    public class CardiacVRIntegrationManager : MonoBehaviour
    {
        public static CardiacVRIntegrationManager Instance { get; private set; }

        #region Events

        public event Action OnSystemsInitialized;
        public event Action<VisualizationMode> OnVisualizationModeChanged;
        public event Action<QualityPreset> OnQualityPresetChanged;

        #endregion

        #region Enums

        public enum VisualizationMode
        {
            Standard,           // Normal viewing
            Surgical,           // Surgical planning mode
            Education,          // Educational mode with simplified visuals
            Presentation,       // High quality presentation
            VR,                 // Optimized for VR
            Review              // Case review mode
        }

        public enum QualityPreset
        {
            Mobile,             // Low quality for mobile VR
            Balanced,           // Medium quality
            High,               // High quality desktop
            Ultra               // Maximum quality
        }

        #endregion

        #region Serialized Fields

        [Header("System References")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool createMissingSystems = true;

        [Header("Default Settings")]
        [SerializeField] private VisualizationMode defaultMode = VisualizationMode.Standard;
        [SerializeField] private QualityPreset defaultQuality = QualityPreset.High;

        [Header("Performance")]
        [SerializeField] private int targetFrameRate = 90;
        [SerializeField] private bool adaptiveQuality = true;
        [SerializeField] private float minAcceptableFrameRate = 72f;

        [Header("Visual Consistency")]
        [SerializeField] private bool synchronizeThemes = true;
        [SerializeField] private bool synchronizeLighting = true;
        [SerializeField] private bool synchronizePostProcessing = true;

        #endregion

        #region Private Fields

        private VisualizationMode currentMode;
        private QualityPreset currentQuality;
        private bool isInitialized = false;
        private float frameRateSampleTime = 0f;
        private int frameCount = 0;
        private float currentFrameRate = 0f;

        // System references
        private PostProcessingManager postProcessing;
        private ProfessionalUITheme uiTheme;
        private SmoothAnimationSystem animationSystem;
        private SegmentedMeshRenderer meshRenderer;
        private VisualEffectsSystem visualEffects;
        private MeshEnhancementSystem meshEnhancement;
        private EnhancedMaterialsLibrary materialsLibrary;
        private ProfessionalLightingSetup lightingSetup;
        private AmbientEnvironmentSystem environmentSystem;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (autoInitialize)
            {
                StartCoroutine(InitializeSystems());
            }
        }

        private void Update()
        {
            if (adaptiveQuality && isInitialized)
            {
                MonitorPerformance();
            }
        }

        #endregion

        #region Initialization

        private IEnumerator InitializeSystems()
        {
            Debug.Log("[CardiacVR] Initializing integration manager...");

            // Set target frame rate
            Application.targetFrameRate = targetFrameRate;

            // Find or create all systems
            yield return StartCoroutine(FindOrCreateSystems());

            // Apply default settings
            ApplyVisualizationMode(defaultMode, false);
            ApplyQualityPreset(defaultQuality, false);

            // Subscribe to events
            SubscribeToSystemEvents();

            isInitialized = true;
            OnSystemsInitialized?.Invoke();

            Debug.Log("[CardiacVR] All systems initialized successfully");
        }

        private IEnumerator FindOrCreateSystems()
        {
            // Post Processing
            postProcessing = FindFirstObjectByType<PostProcessingManager>();
            if (postProcessing == null && createMissingSystems)
            {
                GameObject obj = new GameObject("PostProcessingManager");
                obj.transform.SetParent(transform);
                postProcessing = obj.AddComponent<PostProcessingManager>();
            }
            yield return null;

            // UI Theme
            uiTheme = FindFirstObjectByType<ProfessionalUITheme>();
            if (uiTheme == null && createMissingSystems)
            {
                GameObject obj = new GameObject("ProfessionalUITheme");
                obj.transform.SetParent(transform);
                uiTheme = obj.AddComponent<ProfessionalUITheme>();
            }
            yield return null;

            // Animation System
            animationSystem = FindFirstObjectByType<SmoothAnimationSystem>();
            if (animationSystem == null && createMissingSystems)
            {
                GameObject obj = new GameObject("SmoothAnimationSystem");
                obj.transform.SetParent(transform);
                animationSystem = obj.AddComponent<SmoothAnimationSystem>();
            }
            yield return null;

            // Segmented Mesh Renderer
            meshRenderer = FindFirstObjectByType<SegmentedMeshRenderer>();
            if (meshRenderer == null && createMissingSystems)
            {
                GameObject obj = new GameObject("SegmentedMeshRenderer");
                obj.transform.SetParent(transform);
                meshRenderer = obj.AddComponent<SegmentedMeshRenderer>();
            }
            yield return null;

            // Visual Effects
            visualEffects = FindFirstObjectByType<VisualEffectsSystem>();
            if (visualEffects == null && createMissingSystems)
            {
                GameObject obj = new GameObject("VisualEffectsSystem");
                obj.transform.SetParent(transform);
                visualEffects = obj.AddComponent<VisualEffectsSystem>();
            }
            yield return null;

            // Mesh Enhancement
            meshEnhancement = FindFirstObjectByType<MeshEnhancementSystem>();
            if (meshEnhancement == null && createMissingSystems)
            {
                GameObject obj = new GameObject("MeshEnhancementSystem");
                obj.transform.SetParent(transform);
                meshEnhancement = obj.AddComponent<MeshEnhancementSystem>();
            }
            yield return null;

            // Materials Library
            materialsLibrary = FindFirstObjectByType<EnhancedMaterialsLibrary>();
            if (materialsLibrary == null && createMissingSystems)
            {
                GameObject obj = new GameObject("EnhancedMaterialsLibrary");
                obj.transform.SetParent(transform);
                materialsLibrary = obj.AddComponent<EnhancedMaterialsLibrary>();
            }
            yield return null;

            // Lighting Setup
            lightingSetup = FindFirstObjectByType<ProfessionalLightingSetup>();
            if (lightingSetup == null && createMissingSystems)
            {
                GameObject obj = new GameObject("ProfessionalLightingSetup");
                obj.transform.SetParent(transform);
                lightingSetup = obj.AddComponent<ProfessionalLightingSetup>();
            }
            yield return null;

            // Environment System
            environmentSystem = FindFirstObjectByType<AmbientEnvironmentSystem>();
            if (environmentSystem == null && createMissingSystems)
            {
                GameObject obj = new GameObject("AmbientEnvironmentSystem");
                obj.transform.SetParent(transform);
                environmentSystem = obj.AddComponent<AmbientEnvironmentSystem>();
            }
            yield return null;

            Debug.Log($"[CardiacVR] Systems found/created: " +
                $"PostProcessing={postProcessing != null}, " +
                $"UITheme={uiTheme != null}, " +
                $"Animation={animationSystem != null}, " +
                $"MeshRenderer={meshRenderer != null}, " +
                $"VisualEffects={visualEffects != null}");
        }

        private void SubscribeToSystemEvents()
        {
            if (postProcessing != null)
            {
                postProcessing.OnPresetChanged += OnPostProcessingPresetChanged;
            }

            if (uiTheme != null)
            {
                uiTheme.OnThemeChanged += OnUIThemeChanged;
            }
        }

        #endregion

        #region Visualization Modes

        public void SetVisualizationMode(VisualizationMode mode)
        {
            ApplyVisualizationMode(mode, true);
        }

        private void ApplyVisualizationMode(VisualizationMode mode, bool animated)
        {
            currentMode = mode;

            switch (mode)
            {
                case VisualizationMode.Standard:
                    ApplyStandardMode(animated);
                    break;
                case VisualizationMode.Surgical:
                    ApplySurgicalMode(animated);
                    break;
                case VisualizationMode.Education:
                    ApplyEducationMode(animated);
                    break;
                case VisualizationMode.Presentation:
                    ApplyPresentationMode(animated);
                    break;
                case VisualizationMode.VR:
                    ApplyVRMode(animated);
                    break;
                case VisualizationMode.Review:
                    ApplyReviewMode(animated);
                    break;
            }

            OnVisualizationModeChanged?.Invoke(mode);
            Debug.Log($"[CardiacVR] Visualization mode set to: {mode}");
        }

        private void ApplyStandardMode(bool animated)
        {
            if (synchronizePostProcessing && postProcessing != null)
                postProcessing.ApplyPreset(PostProcessingManager.VisualPreset.Clinical);

            if (synchronizeThemes && uiTheme != null)
                uiTheme.ApplyTheme(ProfessionalUITheme.UITheme.Dark);

            if (synchronizeLighting && lightingSetup != null)
                lightingSetup.SetLightingMode(ProfessionalLightingSetup.LightingMode.Education, animated);

            if (environmentSystem != null)
                environmentSystem.ApplyPreset(AmbientEnvironmentSystem.EnvironmentPreset.MedicalStudio, animated);
        }

        private void ApplySurgicalMode(bool animated)
        {
            if (synchronizePostProcessing && postProcessing != null)
                postProcessing.ApplyPreset(PostProcessingManager.VisualPreset.Surgery);

            if (synchronizeThemes && uiTheme != null)
                uiTheme.ApplyTheme(ProfessionalUITheme.UITheme.Surgery);

            if (synchronizeLighting && lightingSetup != null)
                lightingSetup.SetLightingMode(ProfessionalLightingSetup.LightingMode.Surgical, animated);

            if (environmentSystem != null)
                environmentSystem.ApplyPreset(AmbientEnvironmentSystem.EnvironmentPreset.OperatingRoom, animated);
        }

        private void ApplyEducationMode(bool animated)
        {
            if (synchronizePostProcessing && postProcessing != null)
                postProcessing.ApplyPreset(PostProcessingManager.VisualPreset.Education);

            if (synchronizeThemes && uiTheme != null)
                uiTheme.ApplyTheme(ProfessionalUITheme.UITheme.Medical);

            if (synchronizeLighting && lightingSetup != null)
                lightingSetup.SetLightingMode(ProfessionalLightingSetup.LightingMode.Education, animated);

            if (environmentSystem != null)
                environmentSystem.ApplyPreset(AmbientEnvironmentSystem.EnvironmentPreset.Educational, animated);
        }

        private void ApplyPresentationMode(bool animated)
        {
            if (synchronizePostProcessing && postProcessing != null)
                postProcessing.ApplyPreset(PostProcessingManager.VisualPreset.Presentation);

            if (synchronizeThemes && uiTheme != null)
                uiTheme.ApplyTheme(ProfessionalUITheme.UITheme.Dark);

            if (synchronizeLighting && lightingSetup != null)
                lightingSetup.SetLightingMode(ProfessionalLightingSetup.LightingMode.Presentation, animated);

            if (environmentSystem != null)
                environmentSystem.ApplyPreset(AmbientEnvironmentSystem.EnvironmentPreset.PresentationHall, animated);
        }

        private void ApplyVRMode(bool animated)
        {
            if (synchronizePostProcessing && postProcessing != null)
            {
                postProcessing.ApplyPreset(PostProcessingManager.VisualPreset.VR_Comfort);
                postProcessing.OptimizeForVR();
            }

            if (synchronizeThemes && uiTheme != null)
                uiTheme.ApplyTheme(ProfessionalUITheme.UITheme.Dark);

            if (synchronizeLighting && lightingSetup != null)
            {
                lightingSetup.SetLightingMode(ProfessionalLightingSetup.LightingMode.VR_Comfort, animated);
                lightingSetup.OptimizeForVR();
            }

            if (environmentSystem != null)
                environmentSystem.ApplyPreset(AmbientEnvironmentSystem.EnvironmentPreset.VRComfort, animated);
        }

        private void ApplyReviewMode(bool animated)
        {
            if (synchronizePostProcessing && postProcessing != null)
                postProcessing.ApplyPreset(PostProcessingManager.VisualPreset.Cinematic);

            if (synchronizeThemes && uiTheme != null)
                uiTheme.ApplyTheme(ProfessionalUITheme.UITheme.Cardiology);

            if (synchronizeLighting && lightingSetup != null)
                lightingSetup.SetLightingMode(ProfessionalLightingSetup.LightingMode.Cinematic, animated);

            if (environmentSystem != null)
                environmentSystem.ApplyPreset(AmbientEnvironmentSystem.EnvironmentPreset.Cinematic, animated);
        }

        #endregion

        #region Quality Presets

        public void SetQualityPreset(QualityPreset quality)
        {
            ApplyQualityPreset(quality, true);
        }

        private void ApplyQualityPreset(QualityPreset quality, bool animated)
        {
            currentQuality = quality;

            switch (quality)
            {
                case QualityPreset.Mobile:
                    ApplyMobileQuality();
                    break;
                case QualityPreset.Balanced:
                    ApplyBalancedQuality();
                    break;
                case QualityPreset.High:
                    ApplyHighQuality();
                    break;
                case QualityPreset.Ultra:
                    ApplyUltraQuality();
                    break;
            }

            OnQualityPresetChanged?.Invoke(quality);
            Debug.Log($"[CardiacVR] Quality preset set to: {quality}");
        }

        private void ApplyMobileQuality()
        {
            QualitySettings.SetQualityLevel(0);

            if (meshRenderer != null)
                meshRenderer.SetQuality(SegmentedMeshRenderer.QualityLevel.Low);

            if (materialsLibrary != null)
                materialsLibrary.SetQuality(EnhancedMaterialsLibrary.MaterialQuality.Low);

            if (lightingSetup != null)
                lightingSetup.OptimizeForMobile();
        }

        private void ApplyBalancedQuality()
        {
            QualitySettings.SetQualityLevel(2);

            if (meshRenderer != null)
                meshRenderer.SetQuality(SegmentedMeshRenderer.QualityLevel.Medium);

            if (materialsLibrary != null)
                materialsLibrary.SetQuality(EnhancedMaterialsLibrary.MaterialQuality.Medium);
        }

        private void ApplyHighQuality()
        {
            QualitySettings.SetQualityLevel(4);

            if (meshRenderer != null)
                meshRenderer.SetQuality(SegmentedMeshRenderer.QualityLevel.High);

            if (materialsLibrary != null)
                materialsLibrary.SetQuality(EnhancedMaterialsLibrary.MaterialQuality.High);
        }

        private void ApplyUltraQuality()
        {
            QualitySettings.SetQualityLevel(5);

            if (meshRenderer != null)
                meshRenderer.SetQuality(SegmentedMeshRenderer.QualityLevel.Ultra);

            if (materialsLibrary != null)
                materialsLibrary.SetQuality(EnhancedMaterialsLibrary.MaterialQuality.Ultra);
        }

        #endregion

        #region Performance Monitoring

        private void MonitorPerformance()
        {
            frameCount++;
            frameRateSampleTime += Time.unscaledDeltaTime;

            if (frameRateSampleTime >= 1f)
            {
                currentFrameRate = frameCount / frameRateSampleTime;
                frameCount = 0;
                frameRateSampleTime = 0;

                if (currentFrameRate < minAcceptableFrameRate && currentQuality != QualityPreset.Mobile)
                {
                    // Reduce quality
                    QualityPreset newQuality = (QualityPreset)Mathf.Max(0, (int)currentQuality - 1);
                    if (newQuality != currentQuality)
                    {
                        Debug.Log($"[CardiacVR] Reducing quality due to low frame rate: {currentFrameRate:F1} FPS");
                        ApplyQualityPreset(newQuality, false);
                    }
                }
            }
        }

        public float GetCurrentFrameRate() => currentFrameRate;

        #endregion

        #region Event Handlers

        private void OnPostProcessingPresetChanged(object sender, PostProcessingManager.PresetChangedEventArgs e)
        {
            // Synchronize other systems if needed
            Debug.Log($"[CardiacVR] Post-processing preset changed to: {e.Preset}");
        }

        private void OnUIThemeChanged(object sender, ProfessionalUITheme.ThemeChangedEventArgs e)
        {
            // Synchronize other systems if needed
            Debug.Log($"[CardiacVR] UI theme changed to: {e.Theme}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current visualization mode
        /// </summary>
        public VisualizationMode GetCurrentMode() => currentMode;

        /// <summary>
        /// Get the current quality preset
        /// </summary>
        public QualityPreset GetCurrentQuality() => currentQuality;

        /// <summary>
        /// Check if all systems are initialized
        /// </summary>
        public bool IsInitialized() => isInitialized;

        /// <summary>
        /// Get reference to post-processing manager
        /// </summary>
        public PostProcessingManager GetPostProcessing() => postProcessing;

        /// <summary>
        /// Get reference to UI theme manager
        /// </summary>
        public ProfessionalUITheme GetUITheme() => uiTheme;

        /// <summary>
        /// Get reference to animation system
        /// </summary>
        public SmoothAnimationSystem GetAnimationSystem() => animationSystem;

        /// <summary>
        /// Get reference to mesh renderer
        /// </summary>
        public SegmentedMeshRenderer GetMeshRenderer() => meshRenderer;

        /// <summary>
        /// Get reference to visual effects system
        /// </summary>
        public VisualEffectsSystem GetVisualEffects() => visualEffects;

        /// <summary>
        /// Get reference to mesh enhancement system
        /// </summary>
        public MeshEnhancementSystem GetMeshEnhancement() => meshEnhancement;

        /// <summary>
        /// Get reference to materials library
        /// </summary>
        public EnhancedMaterialsLibrary GetMaterialsLibrary() => materialsLibrary;

        /// <summary>
        /// Get reference to lighting setup
        /// </summary>
        public ProfessionalLightingSetup GetLightingSetup() => lightingSetup;

        /// <summary>
        /// Get reference to environment system
        /// </summary>
        public AmbientEnvironmentSystem GetEnvironmentSystem() => environmentSystem;

        /// <summary>
        /// Manually initialize systems if auto-initialize is disabled
        /// </summary>
        public void Initialize()
        {
            if (!isInitialized)
            {
                StartCoroutine(InitializeSystems());
            }
        }

        /// <summary>
        /// Force refresh all systems
        /// </summary>
        public void RefreshAllSystems()
        {
            ApplyVisualizationMode(currentMode, true);
            ApplyQualityPreset(currentQuality, true);
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (postProcessing != null)
            {
                postProcessing.OnPresetChanged -= OnPostProcessingPresetChanged;
            }

            if (uiTheme != null)
            {
                uiTheme.OnThemeChanged -= OnUIThemeChanged;
            }
        }

        #endregion
    }
}

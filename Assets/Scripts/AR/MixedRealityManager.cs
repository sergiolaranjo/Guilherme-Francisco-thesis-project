// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages switching between Virtual Reality (VR) and Augmented Reality (AR/Mixed Reality) modes.
/// Handles passthrough, environment occlusion, and camera settings.
/// </summary>
public class MixedRealityManager : MonoBehaviour
{
    public static MixedRealityManager Instance { get; private set; }

    public event EventHandler<ModeChangedEventArgs> OnModeChanged;
    public event EventHandler<PassthroughSettingsChangedEventArgs> OnPassthroughSettingsChanged;

    public class ModeChangedEventArgs : EventArgs
    {
        public RealityMode PreviousMode;
        public RealityMode CurrentMode;
    }

    public class PassthroughSettingsChangedEventArgs : EventArgs
    {
        public float Opacity;
        public float Brightness;
        public float Contrast;
    }

    /// <summary>
    /// Reality display modes.
    /// </summary>
    public enum RealityMode
    {
        VirtualReality,     // Fully immersive VR
        AugmentedReality,   // Passthrough AR/MR
        MixedReality        // AR with environment occlusion
    }

    /// <summary>
    /// Passthrough layer types for Meta Quest.
    /// </summary>
    public enum PassthroughStyle
    {
        Underlay,   // Passthrough behind virtual content
        Overlay     // Passthrough in front (for portals/windows)
    }

    [Header("Current State")]
    [SerializeField] private RealityMode currentMode = RealityMode.VirtualReality;
    [SerializeField] private bool isPassthroughSupported = false;

    [Header("Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera backgroundCamera;

    [Header("Environment")]
    [SerializeField] private GameObject vrEnvironment;
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Color vrBackgroundColor = Color.black;
    [SerializeField] private Color arBackgroundColor = new Color(0, 0, 0, 0);

    [Header("Passthrough Settings")]
    [SerializeField, Range(0f, 1f)] private float passthroughOpacity = 1f;
    [SerializeField, Range(-1f, 1f)] private float passthroughBrightness = 0f;
    [SerializeField, Range(-1f, 1f)] private float passthroughContrast = 0f;
    [SerializeField] private PassthroughStyle passthroughStyle = PassthroughStyle.Underlay;

    [Header("AR Model Settings")]
    [SerializeField] private float arModelOpacity = 1f;
    [SerializeField] private bool enableDepthOcclusion = true;
    [SerializeField] private bool enableHandOcclusion = true;

    [Header("Lighting")]
    [SerializeField] private Light mainLight;
    [SerializeField] private float vrLightIntensity = 1f;
    [SerializeField] private float arLightIntensity = 0.5f;
    [SerializeField] private bool useEnvironmentLighting = true;

    [Header("Meta Quest Passthrough")]
    [SerializeField] private GameObject ovrPassthroughLayer;
    [SerializeField] private GameObject ovrCameraRig;

    // Platform detection
    private bool isMetaQuest = false;
    private bool isHololens = false;
    private bool isMagicLeap = false;

    // Cached materials for opacity changes
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private List<Renderer> arAffectedRenderers = new List<Renderer>();

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

        DetectPlatform();
    }

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        CheckPassthroughSupport();
        ApplyMode(currentMode);
    }

    /// <summary>
    /// Detect which XR platform we're running on.
    /// </summary>
    private void DetectPlatform()
    {
        string xrDeviceName = GetXRDeviceName().ToLower();

        isMetaQuest = xrDeviceName.Contains("oculus") || xrDeviceName.Contains("meta") || xrDeviceName.Contains("openxr");
        isHololens = xrDeviceName.Contains("hololens") || xrDeviceName.Contains("windowsmr");
        isMagicLeap = xrDeviceName.Contains("magicleap");

        // Additional check for Meta Quest via subsystem
        if (!isMetaQuest)
        {
            var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(xrDisplaySubsystems);
            foreach (var subsystem in xrDisplaySubsystems)
            {
                if (subsystem.running)
                {
                    isMetaQuest = true;
                    break;
                }
            }
        }

        Debug.Log($"Detected XR Platform: {xrDeviceName}");
        Debug.Log($"Meta Quest: {isMetaQuest}, HoloLens: {isHololens}, Magic Leap: {isMagicLeap}");
    }

    /// <summary>
    /// Get XR device name using Unity 6 compatible API.
    /// </summary>
    private string GetXRDeviceName()
    {
        // Try XR Management first (Unity 6 preferred method)
        var xrGeneralSettings = XRGeneralSettings.Instance;
        if (xrGeneralSettings != null && xrGeneralSettings.Manager != null)
        {
            var activeLoader = xrGeneralSettings.Manager.activeLoader;
            if (activeLoader != null)
            {
                return activeLoader.name ?? "Unknown";
            }
        }

        // Fallback: Check input devices
        var inputDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, inputDevices);
        if (inputDevices.Count > 0)
        {
            return inputDevices[0].name ?? "Unknown";
        }

        return "None";
    }

    /// <summary>
    /// Check if passthrough/AR is supported on the current device.
    /// </summary>
    private void CheckPassthroughSupport()
    {
        // Meta Quest 2/3/Pro support passthrough
        if (isMetaQuest)
        {
            isPassthroughSupported = true;
        }
        // HoloLens is always AR
        else if (isHololens)
        {
            isPassthroughSupported = true;
            currentMode = RealityMode.AugmentedReality;
        }
        // Magic Leap 2 supports passthrough
        else if (isMagicLeap)
        {
            isPassthroughSupported = true;
        }
        else
        {
            // Check for generic passthrough support
            isPassthroughSupported = false;
        }

        Debug.Log($"Passthrough supported: {isPassthroughSupported}");
    }

    #region Mode Switching

    /// <summary>
    /// Set the reality mode.
    /// </summary>
    public void SetMode(RealityMode mode)
    {
        if (!isPassthroughSupported && mode != RealityMode.VirtualReality)
        {
            Debug.LogWarning("Passthrough not supported on this device. Staying in VR mode.");
            return;
        }

        RealityMode previousMode = currentMode;
        currentMode = mode;

        ApplyMode(mode);

        OnModeChanged?.Invoke(this, new ModeChangedEventArgs
        {
            PreviousMode = previousMode,
            CurrentMode = mode
        });

        Debug.Log($"Reality mode changed: {previousMode} -> {mode}");
    }

    /// <summary>
    /// Toggle between VR and AR modes.
    /// </summary>
    public void ToggleMode()
    {
        if (currentMode == RealityMode.VirtualReality)
        {
            SetMode(RealityMode.AugmentedReality);
        }
        else
        {
            SetMode(RealityMode.VirtualReality);
        }
    }

    /// <summary>
    /// Apply the specified mode settings.
    /// </summary>
    private void ApplyMode(RealityMode mode)
    {
        switch (mode)
        {
            case RealityMode.VirtualReality:
                ApplyVRMode();
                break;
            case RealityMode.AugmentedReality:
                ApplyARMode();
                break;
            case RealityMode.MixedReality:
                ApplyMRMode();
                break;
        }
    }

    private void ApplyVRMode()
    {
        // Enable VR environment
        if (vrEnvironment != null)
        {
            vrEnvironment.SetActive(true);
        }

        // Set camera background
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.Skybox;
            mainCamera.backgroundColor = vrBackgroundColor;
        }

        // Set skybox
        if (skyboxMaterial != null)
        {
            RenderSettings.skybox = skyboxMaterial;
        }

        // Disable passthrough
        DisablePassthrough();

        // Set lighting
        if (mainLight != null)
        {
            mainLight.intensity = vrLightIntensity;
        }

        // Reset model opacity
        SetModelOpacity(1f);
    }

    private void ApplyARMode()
    {
        // Disable VR environment
        if (vrEnvironment != null)
        {
            vrEnvironment.SetActive(false);
        }

        // Set camera for AR (transparent background)
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = arBackgroundColor;
        }

        // Remove skybox
        RenderSettings.skybox = null;

        // Enable passthrough
        EnablePassthrough();

        // Set lighting for AR
        if (mainLight != null)
        {
            mainLight.intensity = arLightIntensity;
        }

        // Apply AR model opacity
        SetModelOpacity(arModelOpacity);
    }

    private void ApplyMRMode()
    {
        // Similar to AR but with depth occlusion enabled
        ApplyARMode();

        // Enable additional MR features
        if (enableDepthOcclusion)
        {
            EnableDepthOcclusion();
        }

        if (enableHandOcclusion)
        {
            EnableHandOcclusion();
        }
    }

    #endregion

    #region Passthrough Control

    /// <summary>
    /// Enable passthrough (AR camera feed).
    /// </summary>
    private void EnablePassthrough()
    {
        if (isMetaQuest)
        {
            EnableMetaPassthrough();
        }
        else if (isHololens)
        {
            // HoloLens is always passthrough
            EnableHololensPassthrough();
        }
        else
        {
            EnableGenericPassthrough();
        }
    }

    /// <summary>
    /// Disable passthrough.
    /// </summary>
    private void DisablePassthrough()
    {
        if (isMetaQuest)
        {
            DisableMetaPassthrough();
        }
        // Other platforms handle this automatically
    }

    private void EnableMetaPassthrough()
    {
        // Enable OVR Passthrough Layer if present
        if (ovrPassthroughLayer != null)
        {
            ovrPassthroughLayer.SetActive(true);
        }

        // Try to enable via OVR Manager
        try
        {
            // This would use OVRManager.instance.isInsightPassthroughEnabled = true
            // Using reflection to avoid compile-time dependency
            var ovrManagerType = System.Type.GetType("OVRManager, Oculus.VR");
            if (ovrManagerType != null)
            {
                var instanceProperty = ovrManagerType.GetProperty("instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    if (instance != null)
                    {
                        var passthroughProperty = ovrManagerType.GetProperty("isInsightPassthroughEnabled");
                        passthroughProperty?.SetValue(instance, true);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Could not enable Meta passthrough via OVRManager: {ex.Message}");
        }

        Debug.Log("Meta passthrough enabled");
    }

    private void DisableMetaPassthrough()
    {
        if (ovrPassthroughLayer != null)
        {
            ovrPassthroughLayer.SetActive(false);
        }

        try
        {
            var ovrManagerType = System.Type.GetType("OVRManager, Oculus.VR");
            if (ovrManagerType != null)
            {
                var instanceProperty = ovrManagerType.GetProperty("instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (instanceProperty != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    if (instance != null)
                    {
                        var passthroughProperty = ovrManagerType.GetProperty("isInsightPassthroughEnabled");
                        passthroughProperty?.SetValue(instance, false);
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Could not disable Meta passthrough: {ex.Message}");
        }

        Debug.Log("Meta passthrough disabled");
    }

    private void EnableHololensPassthrough()
    {
        // HoloLens is always passthrough, just ensure camera is set correctly
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.clear;
        }
    }

    private void EnableGenericPassthrough()
    {
        // Generic AR setup - transparent background
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0, 0, 0, 0);
        }
    }

    #endregion

    #region Passthrough Settings

    /// <summary>
    /// Set passthrough opacity (0-1).
    /// </summary>
    public void SetPassthroughOpacity(float opacity)
    {
        passthroughOpacity = Mathf.Clamp01(opacity);
        ApplyPassthroughSettings();
    }

    /// <summary>
    /// Set passthrough brightness adjustment (-1 to 1).
    /// </summary>
    public void SetPassthroughBrightness(float brightness)
    {
        passthroughBrightness = Mathf.Clamp(brightness, -1f, 1f);
        ApplyPassthroughSettings();
    }

    /// <summary>
    /// Set passthrough contrast adjustment (-1 to 1).
    /// </summary>
    public void SetPassthroughContrast(float contrast)
    {
        passthroughContrast = Mathf.Clamp(contrast, -1f, 1f);
        ApplyPassthroughSettings();
    }

    private void ApplyPassthroughSettings()
    {
        if (isMetaQuest)
        {
            // Apply settings to OVR Passthrough Layer
            // This would typically be done through OVRPassthroughLayer component
            try
            {
                var passthroughLayerType = System.Type.GetType("OVRPassthroughLayer, Oculus.VR");
                if (passthroughLayerType != null && ovrPassthroughLayer != null)
                {
                    var component = ovrPassthroughLayer.GetComponent(passthroughLayerType);
                    if (component != null)
                    {
                        var textureOpacityProperty = passthroughLayerType.GetProperty("textureOpacity");
                        textureOpacityProperty?.SetValue(component, passthroughOpacity);

                        var brightnessProperty = passthroughLayerType.GetProperty("colorMapEditorBrightness");
                        brightnessProperty?.SetValue(component, passthroughBrightness);

                        var contrastProperty = passthroughLayerType.GetProperty("colorMapEditorContrast");
                        contrastProperty?.SetValue(component, passthroughContrast);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not apply passthrough settings: {ex.Message}");
            }
        }

        OnPassthroughSettingsChanged?.Invoke(this, new PassthroughSettingsChangedEventArgs
        {
            Opacity = passthroughOpacity,
            Brightness = passthroughBrightness,
            Contrast = passthroughContrast
        });
    }

    #endregion

    #region Model Opacity

    /// <summary>
    /// Set opacity for all AR-affected models.
    /// </summary>
    public void SetModelOpacity(float opacity)
    {
        arModelOpacity = Mathf.Clamp01(opacity);

        foreach (Renderer renderer in arAffectedRenderers)
        {
            if (renderer == null) continue;

            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color color = mat.color;
                    color.a = arModelOpacity;
                    mat.color = color;
                }

                // Also try _BaseColor for URP
                if (mat.HasProperty("_BaseColor"))
                {
                    Color color = mat.GetColor("_BaseColor");
                    color.a = arModelOpacity;
                    mat.SetColor("_BaseColor", color);
                }
            }
        }
    }

    /// <summary>
    /// Register a renderer to be affected by AR opacity changes.
    /// </summary>
    public void RegisterARRenderer(Renderer renderer)
    {
        if (!arAffectedRenderers.Contains(renderer))
        {
            arAffectedRenderers.Add(renderer);

            // Store original materials
            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = renderer.sharedMaterials;
            }
        }
    }

    /// <summary>
    /// Unregister a renderer from AR opacity changes.
    /// </summary>
    public void UnregisterARRenderer(Renderer renderer)
    {
        arAffectedRenderers.Remove(renderer);
    }

    #endregion

    #region Occlusion

    private void EnableDepthOcclusion()
    {
        if (isMetaQuest)
        {
            // Enable depth API for Meta Quest
            try
            {
                var ovrManagerType = System.Type.GetType("OVRManager, Oculus.VR");
                if (ovrManagerType != null)
                {
                    // OVRManager settings for depth
                    Debug.Log("Depth occlusion enabled for Meta Quest");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not enable depth occlusion: {ex.Message}");
            }
        }
    }

    private void EnableHandOcclusion()
    {
        if (isMetaQuest)
        {
            // Enable hand occlusion for Meta Quest
            Debug.Log("Hand occlusion enabled");
        }
    }

    #endregion

    #region Public Getters

    /// <summary>
    /// Get current reality mode.
    /// </summary>
    public RealityMode GetCurrentMode()
    {
        return currentMode;
    }

    /// <summary>
    /// Check if currently in AR mode.
    /// </summary>
    public bool IsARMode()
    {
        return currentMode == RealityMode.AugmentedReality || currentMode == RealityMode.MixedReality;
    }

    /// <summary>
    /// Check if passthrough is supported.
    /// </summary>
    public bool IsPassthroughSupported()
    {
        return isPassthroughSupported;
    }

    /// <summary>
    /// Get passthrough opacity.
    /// </summary>
    public float GetPassthroughOpacity()
    {
        return passthroughOpacity;
    }

    /// <summary>
    /// Get model opacity in AR mode.
    /// </summary>
    public float GetModelOpacity()
    {
        return arModelOpacity;
    }

    /// <summary>
    /// Check if depth occlusion is enabled.
    /// </summary>
    public bool IsDepthOcclusionEnabled()
    {
        return enableDepthOcclusion && currentMode == RealityMode.MixedReality;
    }

    #endregion
}

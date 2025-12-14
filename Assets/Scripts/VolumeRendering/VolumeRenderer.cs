// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System;

/// <summary>
/// Component that handles volume rendering of 3D medical imaging data (DICOM/CT/MRI).
/// Attach this to a cube primitive and assign the volume rendering material.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class VolumeRenderer : MonoBehaviour
{
    public static VolumeRenderer Instance { get; private set; }

    public event EventHandler OnVolumeLoaded;

    [Header("Volume Data")]
    [SerializeField] private Texture3D volumeData;
    [SerializeField] private Material volumeMaterial;

    [Header("Rendering Settings")]
    [SerializeField, Range(0f, 5f)] private float alphaMultiplier = 1.0f;
    [SerializeField, Range(0.001f, 0.1f)] private float stepSize = 0.01f;
    [SerializeField, Range(32, 512)] private int maxIterations = 256;
    [SerializeField, Range(0.1f, 10f)] private float density = 1.0f;

    [Header("Window Level (CT/MRI)")]
    [SerializeField, Range(0f, 1f)] private float windowCenter = 0.5f;
    [SerializeField, Range(0.001f, 1f)] private float windowWidth = 0.5f;

    [Header("Threshold")]
    [SerializeField, Range(0f, 1f)] private float minThreshold = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxThreshold = 1.0f;

    [Header("Lighting")]
    [SerializeField] private Vector3 lightDirection = new Vector3(0, 1, 0);
    [SerializeField, Range(0f, 1f)] private float ambientLight = 0.3f;
    [SerializeField, Range(0f, 1f)] private float diffuseLight = 0.7f;
    [SerializeField, Range(0f, 1f)] private float specularLight = 0.2f;
    [SerializeField, Range(1f, 128f)] private float shininess = 32f;

    [Header("Clipping")]
    [SerializeField] private bool useClipPlane = false;
    [SerializeField] private Transform clipPlaneTransform;

    [Header("Transfer Function")]
    [SerializeField] private Gradient transferFunctionGradient;
    [SerializeField] private AnimationCurve opacityCurve = AnimationCurve.Linear(0, 0, 1, 1);
    private Texture2D transferFunctionTexture;

    [Header("Presets")]
    [SerializeField] private VolumeRenderingPreset currentPreset = VolumeRenderingPreset.SoftTissue;

    public enum VolumeRenderingPreset
    {
        Custom,
        SoftTissue,
        Bone,
        Blood,
        Lung,
        Cardiac,
        MIP // Maximum Intensity Projection
    }

    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Duplicate VolumeRenderer instance destroyed.");
            Destroy(gameObject);
            return;
        }

        meshRenderer = GetComponent<MeshRenderer>();
        propertyBlock = new MaterialPropertyBlock();

        InitializeTransferFunction();
    }

    void Start()
    {
        if (volumeMaterial != null)
        {
            meshRenderer.material = volumeMaterial;
        }

        UpdateMaterialProperties();

        if (currentPreset != VolumeRenderingPreset.Custom)
        {
            ApplyPreset(currentPreset);
        }
    }

    void Update()
    {
        if (useClipPlane && clipPlaneTransform != null)
        {
            UpdateClipPlane();
        }
    }

    /// <summary>
    /// Initialize the transfer function texture from the gradient and opacity curve.
    /// </summary>
    private void InitializeTransferFunction()
    {
        if (transferFunctionGradient == null)
        {
            // Create default gradient (grayscale)
            transferFunctionGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[2];
            colorKeys[0] = new GradientColorKey(Color.black, 0f);
            colorKeys[1] = new GradientColorKey(Color.white, 1f);

            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(0f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);

            transferFunctionGradient.SetKeys(colorKeys, alphaKeys);
        }

        GenerateTransferFunctionTexture();
    }

    /// <summary>
    /// Generate a 1D transfer function texture from the gradient and opacity curve.
    /// </summary>
    public void GenerateTransferFunctionTexture()
    {
        const int resolution = 256;
        transferFunctionTexture = new Texture2D(resolution, 1, TextureFormat.RGBA32, false);
        transferFunctionTexture.wrapMode = TextureWrapMode.Clamp;
        transferFunctionTexture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            Color c = transferFunctionGradient.Evaluate(t);
            c.a = opacityCurve.Evaluate(t);
            colors[i] = c;
        }

        transferFunctionTexture.SetPixels(colors);
        transferFunctionTexture.Apply();
    }

    /// <summary>
    /// Update all material properties.
    /// </summary>
    public void UpdateMaterialProperties()
    {
        if (volumeMaterial == null || meshRenderer == null) return;

        meshRenderer.GetPropertyBlock(propertyBlock);

        // Volume data
        if (volumeData != null)
        {
            propertyBlock.SetTexture("_Data", volumeData);
        }

        // Transfer function
        if (transferFunctionTexture != null)
        {
            propertyBlock.SetTexture("_TransferFunction", transferFunctionTexture);
        }

        // Rendering settings
        propertyBlock.SetFloat("_Alpha", alphaMultiplier);
        propertyBlock.SetFloat("_StepSize", stepSize);
        propertyBlock.SetInt("_Iterations", maxIterations);
        propertyBlock.SetFloat("_Density", density);

        // Window level
        propertyBlock.SetFloat("_WindowCenter", windowCenter);
        propertyBlock.SetFloat("_WindowWidth", windowWidth);

        // Threshold
        propertyBlock.SetFloat("_MinThreshold", minThreshold);
        propertyBlock.SetFloat("_MaxThreshold", maxThreshold);

        // Lighting
        propertyBlock.SetVector("_LightDir", lightDirection.normalized);
        propertyBlock.SetFloat("_AmbientLight", ambientLight);
        propertyBlock.SetFloat("_DiffuseLight", diffuseLight);
        propertyBlock.SetFloat("_SpecularLight", specularLight);
        propertyBlock.SetFloat("_Shininess", shininess);

        // Clipping
        propertyBlock.SetFloat("_UseClipPlane", useClipPlane ? 1f : 0f);

        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Update clipping plane from transform.
    /// </summary>
    private void UpdateClipPlane()
    {
        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetVector("_ClipPlaneNormal", clipPlaneTransform.forward);
        propertyBlock.SetVector("_ClipPlanePosition", clipPlaneTransform.position);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    /// <summary>
    /// Set the volume data texture.
    /// </summary>
    public void SetVolumeData(Texture3D texture)
    {
        volumeData = texture;
        UpdateMaterialProperties();
        OnVolumeLoaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Get the current volume data.
    /// </summary>
    public Texture3D GetVolumeData()
    {
        return volumeData;
    }

    /// <summary>
    /// Set the transfer function gradient.
    /// </summary>
    public void SetTransferFunction(Gradient gradient, AnimationCurve opacity = null)
    {
        transferFunctionGradient = gradient;
        if (opacity != null) opacityCurve = opacity;
        GenerateTransferFunctionTexture();
        UpdateMaterialProperties();
    }

    /// <summary>
    /// Set window level (for CT/MRI visualization).
    /// </summary>
    public void SetWindowLevel(float center, float width)
    {
        windowCenter = Mathf.Clamp01(center);
        windowWidth = Mathf.Clamp(width, 0.001f, 1f);
        UpdateMaterialProperties();
    }

    /// <summary>
    /// Set the density threshold range.
    /// </summary>
    public void SetThreshold(float min, float max)
    {
        minThreshold = Mathf.Clamp01(min);
        maxThreshold = Mathf.Clamp01(max);
        UpdateMaterialProperties();
    }

    /// <summary>
    /// Enable or disable the clipping plane.
    /// </summary>
    public void SetClipPlane(bool enabled, Transform planeTransform = null)
    {
        useClipPlane = enabled;
        if (planeTransform != null) clipPlaneTransform = planeTransform;
        UpdateMaterialProperties();
    }

    /// <summary>
    /// Set rendering quality (affects step size and iterations).
    /// </summary>
    public void SetQuality(float quality)
    {
        quality = Mathf.Clamp01(quality);
        stepSize = Mathf.Lerp(0.05f, 0.005f, quality);
        maxIterations = Mathf.RoundToInt(Mathf.Lerp(64, 512, quality));
        UpdateMaterialProperties();
    }

    /// <summary>
    /// Apply a preset visualization mode.
    /// </summary>
    public void ApplyPreset(VolumeRenderingPreset preset)
    {
        currentPreset = preset;

        switch (preset)
        {
            case VolumeRenderingPreset.SoftTissue:
                ApplySoftTissuePreset();
                break;
            case VolumeRenderingPreset.Bone:
                ApplyBonePreset();
                break;
            case VolumeRenderingPreset.Blood:
                ApplyBloodPreset();
                break;
            case VolumeRenderingPreset.Lung:
                ApplyLungPreset();
                break;
            case VolumeRenderingPreset.Cardiac:
                ApplyCardiacPreset();
                break;
            case VolumeRenderingPreset.MIP:
                ApplyMIPPreset();
                break;
        }

        GenerateTransferFunctionTexture();
        UpdateMaterialProperties();
    }

    private void ApplySoftTissuePreset()
    {
        windowCenter = 0.5f;
        windowWidth = 0.4f;
        minThreshold = 0.1f;
        maxThreshold = 0.8f;

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(0.8f, 0.6f, 0.5f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(0.9f, 0.7f, 0.6f), 0.5f);
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.85f, 0.8f), 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.5f, 0.3f);
        alphaKeys[2] = new GradientAlphaKey(1f, 1f);

        transferFunctionGradient.SetKeys(colorKeys, alphaKeys);
    }

    private void ApplyBonePreset()
    {
        windowCenter = 0.75f;
        windowWidth = 0.3f;
        minThreshold = 0.5f;
        maxThreshold = 1.0f;

        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(new Color(0.9f, 0.9f, 0.85f), 0f);
        colorKeys[1] = new GradientColorKey(Color.white, 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 0.5f);

        transferFunctionGradient.SetKeys(colorKeys, alphaKeys);
    }

    private void ApplyBloodPreset()
    {
        windowCenter = 0.45f;
        windowWidth = 0.35f;
        minThreshold = 0.2f;
        maxThreshold = 0.7f;

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(0.4f, 0f, 0f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.1f, 0.1f), 0.5f);
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.3f, 0.2f), 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.7f, 0.4f);
        alphaKeys[2] = new GradientAlphaKey(1f, 1f);

        transferFunctionGradient.SetKeys(colorKeys, alphaKeys);
    }

    private void ApplyLungPreset()
    {
        windowCenter = 0.3f;
        windowWidth = 0.5f;
        minThreshold = 0.05f;
        maxThreshold = 0.5f;

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        colorKeys[0] = new GradientColorKey(new Color(0.1f, 0.1f, 0.15f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.5f, 0.6f), 0.5f);
        colorKeys[2] = new GradientColorKey(new Color(0.8f, 0.8f, 0.9f), 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0] = new GradientAlphaKey(0.1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.3f, 0.3f);
        alphaKeys[2] = new GradientAlphaKey(0.6f, 1f);

        transferFunctionGradient.SetKeys(colorKeys, alphaKeys);
    }

    private void ApplyCardiacPreset()
    {
        windowCenter = 0.4f;
        windowWidth = 0.45f;
        minThreshold = 0.15f;
        maxThreshold = 0.85f;

        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(0.5f, 0.1f, 0.1f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.2f, 0.15f), 0.35f);
        colorKeys[2] = new GradientColorKey(new Color(0.9f, 0.5f, 0.4f), 0.65f);
        colorKeys[3] = new GradientColorKey(new Color(1f, 0.85f, 0.8f), 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.6f, 0.25f);
        alphaKeys[2] = new GradientAlphaKey(0.8f, 0.5f);
        alphaKeys[3] = new GradientAlphaKey(1f, 1f);

        transferFunctionGradient.SetKeys(colorKeys, alphaKeys);
    }

    private void ApplyMIPPreset()
    {
        // Maximum Intensity Projection
        windowCenter = 0.5f;
        windowWidth = 1f;
        minThreshold = 0f;
        maxThreshold = 1f;
        alphaMultiplier = 0.1f;

        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(Color.black, 0f);
        colorKeys[1] = new GradientColorKey(Color.white, 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        transferFunctionGradient.SetKeys(colorKeys, alphaKeys);
    }

    // Editor validation
    void OnValidate()
    {
        if (Application.isPlaying && meshRenderer != null)
        {
            GenerateTransferFunctionTexture();
            UpdateMaterialProperties();
        }
    }
}

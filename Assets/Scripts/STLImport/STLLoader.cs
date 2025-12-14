// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Component for loading and managing STL models at runtime.
/// Supports loading from file path, Resources, or byte array.
/// </summary>
public class STLLoader : MonoBehaviour
{
    public static STLLoader Instance { get; private set; }

    public event EventHandler<STLLoadedEventArgs> OnSTLLoaded;
    public event EventHandler<float> OnLoadProgress;
    public event EventHandler<string> OnLoadError;

    public class STLLoadedEventArgs : EventArgs
    {
        public GameObject ModelObject;
        public Mesh Mesh;
        public string Name;
        public int TriangleCount;
        public Bounds Bounds;
    }

    [Header("Default Settings")]
    [SerializeField] private Material defaultMaterial;
    [SerializeField] private bool optimizeMesh = true;
    [SerializeField] private bool generateCollider = true;
    [SerializeField] private bool centerModel = true;
    [SerializeField] private float defaultScale = 0.001f; // STL files are often in mm

    [Header("Parent Transform")]
    [SerializeField] private Transform modelsParent;

    [Header("Layer Settings")]
    [SerializeField] private string modelLayer = "Default";

    private List<GameObject> loadedModels = new List<GameObject>();
    private bool isLoading = false;

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

    /// <summary>
    /// Load an STL file from a file path.
    /// </summary>
    public void LoadFromFile(string filePath)
    {
        if (isLoading)
        {
            Debug.LogWarning("Already loading an STL file. Please wait.");
            return;
        }

        StartCoroutine(LoadFromFileAsync(filePath));
    }

    private IEnumerator LoadFromFileAsync(string filePath)
    {
        isLoading = true;
        OnLoadProgress?.Invoke(this, 0.1f);

        if (!File.Exists(filePath))
        {
            string error = $"STL file not found: {filePath}";
            Debug.LogError(error);
            OnLoadError?.Invoke(this, error);
            isLoading = false;
            yield break;
        }

        yield return null;
        OnLoadProgress?.Invoke(this, 0.2f);

        // Import STL
        STLImporter.STLImportResult result = STLImporter.Import(filePath);

        if (!result.Success)
        {
            Debug.LogError($"Failed to import STL: {result.ErrorMessage}");
            OnLoadError?.Invoke(this, result.ErrorMessage);
            isLoading = false;
            yield break;
        }

        OnLoadProgress?.Invoke(this, 0.6f);
        yield return null;

        // Optimize mesh if requested
        Mesh finalMesh = result.Mesh;
        if (optimizeMesh)
        {
            finalMesh = STLImporter.OptimizeMesh(result.Mesh);
            if (finalMesh == null) finalMesh = result.Mesh;
        }

        OnLoadProgress?.Invoke(this, 0.8f);
        yield return null;

        // Create GameObject
        GameObject modelObject = CreateModelObject(finalMesh, result.Name);

        OnLoadProgress?.Invoke(this, 1f);

        loadedModels.Add(modelObject);

        OnSTLLoaded?.Invoke(this, new STLLoadedEventArgs
        {
            ModelObject = modelObject,
            Mesh = finalMesh,
            Name = result.Name,
            TriangleCount = result.TriangleCount,
            Bounds = finalMesh.bounds
        });

        Debug.Log($"STL loaded successfully: {result.Name} ({result.TriangleCount} triangles)");
        isLoading = false;
    }

    /// <summary>
    /// Load an STL file from a byte array (useful for network loading or embedded resources).
    /// </summary>
    public void LoadFromBytes(byte[] data, string name = "STLModel")
    {
        if (isLoading)
        {
            Debug.LogWarning("Already loading an STL file. Please wait.");
            return;
        }

        StartCoroutine(LoadFromBytesAsync(data, name));
    }

    private IEnumerator LoadFromBytesAsync(byte[] data, string name)
    {
        isLoading = true;
        OnLoadProgress?.Invoke(this, 0.1f);

        yield return null;

        // Import STL
        STLImporter.STLImportResult result = STLImporter.Import(data, name);

        if (!result.Success)
        {
            Debug.LogError($"Failed to import STL: {result.ErrorMessage}");
            OnLoadError?.Invoke(this, result.ErrorMessage);
            isLoading = false;
            yield break;
        }

        OnLoadProgress?.Invoke(this, 0.5f);
        yield return null;

        // Optimize mesh
        Mesh finalMesh = result.Mesh;
        if (optimizeMesh)
        {
            finalMesh = STLImporter.OptimizeMesh(result.Mesh);
            if (finalMesh == null) finalMesh = result.Mesh;
        }

        OnLoadProgress?.Invoke(this, 0.8f);
        yield return null;

        // Create GameObject
        GameObject modelObject = CreateModelObject(finalMesh, name);

        OnLoadProgress?.Invoke(this, 1f);

        loadedModels.Add(modelObject);

        OnSTLLoaded?.Invoke(this, new STLLoadedEventArgs
        {
            ModelObject = modelObject,
            Mesh = finalMesh,
            Name = name,
            TriangleCount = result.TriangleCount,
            Bounds = finalMesh.bounds
        });

        isLoading = false;
    }

    /// <summary>
    /// Load an STL file from Resources folder.
    /// </summary>
    public void LoadFromResources(string resourcePath)
    {
        TextAsset stlAsset = Resources.Load<TextAsset>(resourcePath);

        if (stlAsset == null)
        {
            string error = $"STL resource not found: {resourcePath}";
            Debug.LogError(error);
            OnLoadError?.Invoke(this, error);
            return;
        }

        LoadFromBytes(stlAsset.bytes, Path.GetFileNameWithoutExtension(resourcePath));
    }

    /// <summary>
    /// Create a GameObject from a mesh.
    /// </summary>
    private GameObject CreateModelObject(Mesh mesh, string name)
    {
        GameObject modelObject = new GameObject(name);

        // Set parent
        if (modelsParent != null)
        {
            modelObject.transform.SetParent(modelsParent);
        }

        // Set layer
        int layer = LayerMask.NameToLayer(modelLayer);
        if (layer >= 0)
        {
            modelObject.layer = layer;
        }

        // Add MeshFilter
        MeshFilter meshFilter = modelObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        // Add MeshRenderer
        MeshRenderer meshRenderer = modelObject.AddComponent<MeshRenderer>();
        meshRenderer.material = defaultMaterial != null ? defaultMaterial : CreateDefaultMaterial();

        // Apply scale
        modelObject.transform.localScale = Vector3.one * defaultScale;

        // Center model
        if (centerModel)
        {
            Vector3 center = mesh.bounds.center;
            modelObject.transform.position = -center * defaultScale;
        }

        // Add collider
        if (generateCollider)
        {
            MeshCollider collider = modelObject.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;
            collider.convex = mesh.triangles.Length / 3 < 256; // Use convex only for simple meshes
        }

        // Add tag for surgical interaction
        modelObject.tag = "STLModel";

        return modelObject;
    }

    /// <summary>
    /// Create a default material if none is assigned.
    /// </summary>
    private Material CreateDefaultMaterial()
    {
        // Create a simple URP Lit material
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = new Color(0.9f, 0.7f, 0.6f); // Flesh-like color
        material.SetFloat("_Smoothness", 0.3f);

        return material;
    }

    /// <summary>
    /// Get all loaded models.
    /// </summary>
    public List<GameObject> GetLoadedModels()
    {
        return new List<GameObject>(loadedModels);
    }

    /// <summary>
    /// Unload a specific model.
    /// </summary>
    public void UnloadModel(GameObject model)
    {
        if (loadedModels.Contains(model))
        {
            loadedModels.Remove(model);
            Destroy(model);
        }
    }

    /// <summary>
    /// Unload all models.
    /// </summary>
    public void UnloadAllModels()
    {
        foreach (GameObject model in loadedModels)
        {
            if (model != null)
            {
                Destroy(model);
            }
        }
        loadedModels.Clear();
    }

    /// <summary>
    /// Check if currently loading.
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }

    /// <summary>
    /// Set the scale for newly loaded models.
    /// </summary>
    public void SetDefaultScale(float scale)
    {
        defaultScale = scale;
    }

    /// <summary>
    /// Set the material for newly loaded models.
    /// </summary>
    public void SetDefaultMaterial(Material material)
    {
        defaultMaterial = material;
    }

    /// <summary>
    /// Apply a material to all loaded models.
    /// </summary>
    public void ApplyMaterialToAll(Material material)
    {
        foreach (GameObject model in loadedModels)
        {
            if (model != null)
            {
                MeshRenderer renderer = model.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.material = material;
                }
            }
        }
    }

    /// <summary>
    /// Set visibility of all loaded models.
    /// </summary>
    public void SetVisibility(bool visible)
    {
        foreach (GameObject model in loadedModels)
        {
            if (model != null)
            {
                model.SetActive(visible);
            }
        }
    }
}

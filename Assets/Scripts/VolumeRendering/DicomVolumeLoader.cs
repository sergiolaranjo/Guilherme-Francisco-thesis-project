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
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Loads DICOM image series and creates 3D textures for volume rendering.
/// Supports runtime and editor loading of DICOM data.
/// </summary>
public class DicomVolumeLoader : MonoBehaviour
{
    public static DicomVolumeLoader Instance { get; private set; }

    public event EventHandler<DicomLoadedEventArgs> OnDicomLoaded;
    public event EventHandler<float> OnLoadProgress;

    public class DicomLoadedEventArgs : EventArgs
    {
        public Texture3D VolumeTexture;
        public Vector3Int Dimensions;
        public Vector3 VoxelSpacing;
        public string PatientName;
        public string StudyDescription;
    }

    [Header("Load Settings")]
    [SerializeField] private string defaultDicomFolder = "";
    [SerializeField] private bool loadOnStart = false;
    [SerializeField] private bool normalizeValues = true;

    [Header("Volume Settings")]
    [SerializeField] private TextureFormat textureFormat = TextureFormat.RFloat;
    [SerializeField] private FilterMode filterMode = FilterMode.Trilinear;
    [SerializeField] private bool generateMipmaps = false;

    [Header("References")]
    [SerializeField] private VolumeRenderer volumeRenderer;

    // Loaded data
    private Texture3D currentVolumeTexture;
    private Vector3Int currentDimensions;
    private Vector3 currentVoxelSpacing;
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

    void Start()
    {
        if (loadOnStart && !string.IsNullOrEmpty(defaultDicomFolder))
        {
            LoadDicomFolder(defaultDicomFolder);
        }
    }

    /// <summary>
    /// Load DICOM files from a folder path.
    /// </summary>
    public void LoadDicomFolder(string folderPath)
    {
        if (isLoading)
        {
            Debug.LogWarning("Already loading DICOM data. Please wait.");
            return;
        }

        StartCoroutine(LoadDicomFolderAsync(folderPath));
    }

    /// <summary>
    /// Load DICOM data asynchronously.
    /// </summary>
    private IEnumerator LoadDicomFolderAsync(string folderPath)
    {
        isLoading = true;

        if (!Directory.Exists(folderPath))
        {
            Debug.LogError($"DICOM folder not found: {folderPath}");
            isLoading = false;
            yield break;
        }

        // Get all DICOM files
        string[] dcmFiles = Directory.GetFiles(folderPath, "*.dcm");
        string[] allFiles = Directory.GetFiles(folderPath);

        // Also check for files without extension (common in DICOM)
        List<string> dicomFiles = new List<string>(dcmFiles);
        foreach (string file in allFiles)
        {
            if (string.IsNullOrEmpty(Path.GetExtension(file)))
            {
                dicomFiles.Add(file);
            }
        }

        if (dicomFiles.Count == 0)
        {
            Debug.LogError("No DICOM files found in folder.");
            isLoading = false;
            yield break;
        }

        Debug.Log($"Found {dicomFiles.Count} potential DICOM files.");
        OnLoadProgress?.Invoke(this, 0.1f);
        yield return null;

        // Try to load using fo-dicom (platform dependent)
#if UNITY_WSA
        yield return StartCoroutine(LoadWithFoDicom(dicomFiles));
#else
        // Fallback: try to load from pre-processed resources or raw files
        yield return StartCoroutine(LoadFromResources());
#endif

        isLoading = false;
    }

#if UNITY_WSA
    /// <summary>
    /// Load DICOM using fo-dicom library (Windows/WSA only).
    /// </summary>
    private IEnumerator LoadWithFoDicom(List<string> files)
    {
        using Dicom;
        using Dicom.Imaging;

        List<DicomFile> dicomFiles = new List<DicomFile>();
        int processed = 0;

        // Load all DICOM files
        foreach (string file in files)
        {
            try
            {
                DicomFile dcmFile = DicomFile.Open(file);
                if (dcmFile.Dataset.Contains(DicomTag.PixelData))
                {
                    dicomFiles.Add(dcmFile);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load DICOM file {file}: {ex.Message}");
            }

            processed++;
            if (processed % 10 == 0)
            {
                OnLoadProgress?.Invoke(this, 0.1f + (0.3f * processed / files.Count));
                yield return null;
            }
        }

        if (dicomFiles.Count == 0)
        {
            Debug.LogError("No valid DICOM files with pixel data found.");
            yield break;
        }

        // Sort by slice location
        dicomFiles = dicomFiles
            .Where(f => f.Dataset.Contains(DicomTag.SliceLocation))
            .OrderBy(f => f.Dataset.Get<double>(DicomTag.SliceLocation))
            .ToList();

        // Get dimensions
        int rows = dicomFiles[0].Dataset.Get<int>(DicomTag.Rows);
        int columns = dicomFiles[0].Dataset.Get<int>(DicomTag.Columns);
        int slices = dicomFiles.Count;

        // Get voxel spacing
        float[] pixelSpacing = dicomFiles[0].Dataset.Get<float[]>(DicomTag.PixelSpacing, new float[] { 1f, 1f });
        float sliceThickness = dicomFiles[0].Dataset.Get<float>(DicomTag.SliceThickness, 1f);
        currentVoxelSpacing = new Vector3(pixelSpacing[0], pixelSpacing[1], sliceThickness);

        currentDimensions = new Vector3Int(columns, rows, slices);

        Debug.Log($"Creating volume texture: {columns}x{rows}x{slices}");
        OnLoadProgress?.Invoke(this, 0.5f);
        yield return null;

        // Create texture
        currentVolumeTexture = new Texture3D(columns, rows, slices, textureFormat, generateMipmaps);
        currentVolumeTexture.wrapMode = TextureWrapMode.Clamp;
        currentVolumeTexture.filterMode = filterMode;

        // Process pixel data
        Color[] voxelColors = new Color[columns * rows * slices];
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        // First pass: find min/max for normalization
        for (int z = 0; z < slices; z++)
        {
            DicomImage image = new DicomImage(dicomFiles[z].Dataset);
            IPixelData pixelData = image.PixelData;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    double value = pixelData.GetPixel(x, y);
                    minValue = Mathf.Min(minValue, (float)value);
                    maxValue = Mathf.Max(maxValue, (float)value);
                }
            }

            if (z % 10 == 0)
            {
                OnLoadProgress?.Invoke(this, 0.5f + (0.2f * z / slices));
                yield return null;
            }
        }

        // Second pass: normalize and store
        float range = maxValue - minValue;
        for (int z = 0; z < slices; z++)
        {
            DicomImage image = new DicomImage(dicomFiles[z].Dataset);
            IPixelData pixelData = image.PixelData;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int index = x + y * columns + z * columns * rows;
                    double value = pixelData.GetPixel(x, y);

                    float normalizedValue = normalizeValues
                        ? (float)((value - minValue) / range)
                        : (float)value / 4095f; // Assume 12-bit DICOM

                    voxelColors[index] = new Color(normalizedValue, normalizedValue, normalizedValue, normalizedValue);
                }
            }

            if (z % 10 == 0)
            {
                OnLoadProgress?.Invoke(this, 0.7f + (0.2f * z / slices));
                yield return null;
            }
        }

        currentVolumeTexture.SetPixels(voxelColors);
        currentVolumeTexture.Apply();

        OnLoadProgress?.Invoke(this, 1f);

        // Apply to volume renderer
        if (volumeRenderer != null)
        {
            volumeRenderer.SetVolumeData(currentVolumeTexture);
        }

        // Get patient info
        string patientName = dicomFiles[0].Dataset.Get<string>(DicomTag.PatientName, "Unknown");
        string studyDesc = dicomFiles[0].Dataset.Get<string>(DicomTag.StudyDescription, "");

        OnDicomLoaded?.Invoke(this, new DicomLoadedEventArgs
        {
            VolumeTexture = currentVolumeTexture,
            Dimensions = currentDimensions,
            VoxelSpacing = currentVoxelSpacing,
            PatientName = patientName,
            StudyDescription = studyDesc
        });

        Debug.Log($"DICOM volume loaded successfully: {columns}x{rows}x{slices}");
    }
#endif

    /// <summary>
    /// Load from pre-processed resources (for non-WSA platforms).
    /// </summary>
    private IEnumerator LoadFromResources()
    {
        // Try to load pre-processed texture from Resources
        Texture3D preloadedTexture = Resources.Load<Texture3D>("heart");

        if (preloadedTexture != null)
        {
            currentVolumeTexture = preloadedTexture;
            currentDimensions = new Vector3Int(
                preloadedTexture.width,
                preloadedTexture.height,
                preloadedTexture.depth
            );
            currentVoxelSpacing = Vector3.one;

            if (volumeRenderer != null)
            {
                volumeRenderer.SetVolumeData(currentVolumeTexture);
            }

            OnLoadProgress?.Invoke(this, 1f);
            OnDicomLoaded?.Invoke(this, new DicomLoadedEventArgs
            {
                VolumeTexture = currentVolumeTexture,
                Dimensions = currentDimensions,
                VoxelSpacing = currentVoxelSpacing,
                PatientName = "Preloaded",
                StudyDescription = "From Resources"
            });

            Debug.Log("Loaded pre-processed volume from Resources.");
        }
        else
        {
            Debug.LogWarning("No pre-processed volume data found in Resources. Please process DICOM files in editor first.");
        }

        yield return null;
    }

    /// <summary>
    /// Load raw volume data file.
    /// </summary>
    public void LoadRawFile(string filePath, int width, int height, int depth, int bytesPerVoxel = 1)
    {
        StartCoroutine(LoadRawFileAsync(filePath, width, height, depth, bytesPerVoxel));
    }

    private IEnumerator LoadRawFileAsync(string filePath, int width, int height, int depth, int bytesPerVoxel)
    {
        isLoading = true;

        if (!File.Exists(filePath))
        {
            Debug.LogError($"Raw file not found: {filePath}");
            isLoading = false;
            yield break;
        }

        OnLoadProgress?.Invoke(this, 0.1f);
        yield return null;

        byte[] rawData;
        try
        {
            rawData = File.ReadAllBytes(filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read raw file: {ex.Message}");
            isLoading = false;
            yield break;
        }

        int expectedSize = width * height * depth * bytesPerVoxel;
        if (rawData.Length != expectedSize)
        {
            Debug.LogWarning($"Raw file size mismatch. Expected {expectedSize}, got {rawData.Length}");
        }

        OnLoadProgress?.Invoke(this, 0.3f);
        yield return null;

        currentDimensions = new Vector3Int(width, height, depth);
        currentVoxelSpacing = Vector3.one;

        currentVolumeTexture = new Texture3D(width, height, depth, textureFormat, generateMipmaps);
        currentVolumeTexture.wrapMode = TextureWrapMode.Clamp;
        currentVolumeTexture.filterMode = filterMode;

        Color[] voxelColors = new Color[width * height * depth];

        for (int i = 0; i < width * height * depth && i < rawData.Length / bytesPerVoxel; i++)
        {
            float value;
            if (bytesPerVoxel == 1)
            {
                value = rawData[i] / 255f;
            }
            else if (bytesPerVoxel == 2)
            {
                ushort rawValue = BitConverter.ToUInt16(rawData, i * 2);
                value = rawValue / 65535f;
            }
            else
            {
                value = rawData[i * bytesPerVoxel] / 255f;
            }

            voxelColors[i] = new Color(value, value, value, value);

            if (i % 100000 == 0)
            {
                OnLoadProgress?.Invoke(this, 0.3f + (0.6f * i / (width * height * depth)));
                yield return null;
            }
        }

        currentVolumeTexture.SetPixels(voxelColors);
        currentVolumeTexture.Apply();

        OnLoadProgress?.Invoke(this, 1f);

        if (volumeRenderer != null)
        {
            volumeRenderer.SetVolumeData(currentVolumeTexture);
        }

        OnDicomLoaded?.Invoke(this, new DicomLoadedEventArgs
        {
            VolumeTexture = currentVolumeTexture,
            Dimensions = currentDimensions,
            VoxelSpacing = currentVoxelSpacing,
            PatientName = "Raw Volume",
            StudyDescription = Path.GetFileName(filePath)
        });

        isLoading = false;
        Debug.Log($"Raw volume loaded successfully: {width}x{height}x{depth}");
    }

    /// <summary>
    /// Load from a stack of 2D image files.
    /// </summary>
    public void LoadImageStack(string folderPath, string extension = "*.png")
    {
        StartCoroutine(LoadImageStackAsync(folderPath, extension));
    }

    private IEnumerator LoadImageStackAsync(string folderPath, string extension)
    {
        isLoading = true;

        string[] imageFiles = Directory.GetFiles(folderPath, extension);
        Array.Sort(imageFiles);

        if (imageFiles.Length == 0)
        {
            Debug.LogError("No image files found in folder.");
            isLoading = false;
            yield break;
        }

        OnLoadProgress?.Invoke(this, 0.1f);
        yield return null;

        // Load first image to get dimensions
        byte[] firstImageData = File.ReadAllBytes(imageFiles[0]);
        Texture2D firstImage = new Texture2D(2, 2);
        firstImage.LoadImage(firstImageData);

        int width = firstImage.width;
        int height = firstImage.height;
        int depth = imageFiles.Length;

        currentDimensions = new Vector3Int(width, height, depth);
        currentVoxelSpacing = Vector3.one;

        currentVolumeTexture = new Texture3D(width, height, depth, textureFormat, generateMipmaps);
        currentVolumeTexture.wrapMode = TextureWrapMode.Clamp;
        currentVolumeTexture.filterMode = filterMode;

        Color[] voxelColors = new Color[width * height * depth];

        for (int z = 0; z < depth; z++)
        {
            byte[] imageData = File.ReadAllBytes(imageFiles[z]);
            Texture2D sliceTexture = new Texture2D(2, 2);
            sliceTexture.LoadImage(imageData);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width + z * width * height;
                    Color pixelColor = sliceTexture.GetPixel(x, y);
                    float gray = (pixelColor.r + pixelColor.g + pixelColor.b) / 3f;
                    voxelColors[index] = new Color(gray, gray, gray, gray);
                }
            }

            Destroy(sliceTexture);

            if (z % 10 == 0)
            {
                OnLoadProgress?.Invoke(this, 0.1f + (0.8f * z / depth));
                yield return null;
            }
        }

        Destroy(firstImage);

        currentVolumeTexture.SetPixels(voxelColors);
        currentVolumeTexture.Apply();

        OnLoadProgress?.Invoke(this, 1f);

        if (volumeRenderer != null)
        {
            volumeRenderer.SetVolumeData(currentVolumeTexture);
        }

        OnDicomLoaded?.Invoke(this, new DicomLoadedEventArgs
        {
            VolumeTexture = currentVolumeTexture,
            Dimensions = currentDimensions,
            VoxelSpacing = currentVoxelSpacing,
            PatientName = "Image Stack",
            StudyDescription = Path.GetFileName(folderPath)
        });

        isLoading = false;
        Debug.Log($"Image stack loaded successfully: {width}x{height}x{depth}");
    }

    /// <summary>
    /// Get the currently loaded volume texture.
    /// </summary>
    public Texture3D GetVolumeTexture()
    {
        return currentVolumeTexture;
    }

    /// <summary>
    /// Get the current volume dimensions.
    /// </summary>
    public Vector3Int GetDimensions()
    {
        return currentDimensions;
    }

    /// <summary>
    /// Get the current voxel spacing.
    /// </summary>
    public Vector3 GetVoxelSpacing()
    {
        return currentVoxelSpacing;
    }

    /// <summary>
    /// Check if currently loading data.
    /// </summary>
    public bool IsLoading()
    {
        return isLoading;
    }
}

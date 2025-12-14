// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Advanced volume rendering controller with medical imaging presets,
/// depth layer visualization, and standard medical viewing angles.
/// </summary>
public class VolumeRenderingController : MonoBehaviour
{
    public static VolumeRenderingController Instance { get; private set; }

    public event EventHandler<PresetChangedEventArgs> OnPresetChanged;
    public event EventHandler<ViewAngleChangedEventArgs> OnViewAngleChanged;
    public event EventHandler<DepthLayerChangedEventArgs> OnDepthLayerChanged;

    public class PresetChangedEventArgs : EventArgs
    {
        public VisualizationPreset Preset;
        public string PresetName;
    }

    public class ViewAngleChangedEventArgs : EventArgs
    {
        public MedicalViewAngle Angle;
        public Vector3 CameraPosition;
        public Quaternion CameraRotation;
    }

    public class DepthLayerChangedEventArgs : EventArgs
    {
        public float CurrentDepth;
        public float LayerThickness;
        public int CurrentLayer;
        public int TotalLayers;
    }

    #region Enums

    /// <summary>
    /// Extended visualization presets based on CT Hounsfield units and tissue types.
    /// </summary>
    public enum VisualizationPreset
    {
        // General
        Custom,
        Default,

        // Bone presets
        Bone,
        BoneWithSoftTissue,
        Skull,
        Spine,

        // Soft tissue presets
        SoftTissue,
        Muscle,
        Fat,

        // Vascular presets
        Blood,
        Vessels,
        VesselsWithContrast,
        Angiography,

        // Cardiac presets
        Cardiac,
        CardiacChambers,
        CardiacWall,
        Coronaries,
        Aorta,
        Valves,

        // Pulmonary presets
        Lung,
        LungParenchyma,
        Airways,
        Bronchi,

        // Special rendering
        MIP,                    // Maximum Intensity Projection
        MinIP,                  // Minimum Intensity Projection
        AverageIP,              // Average Intensity Projection
        SurfaceShaded,          // Surface shaded display
        VolumeRendered,         // Standard volume rendering
        XRay                    // X-ray like visualization
    }

    /// <summary>
    /// Standard medical viewing angles used in cardiology and radiology.
    /// </summary>
    public enum MedicalViewAngle
    {
        // Standard views
        Anterior,               // AP - Anterior-Posterior (front)
        Posterior,              // PA - Posterior-Anterior (back)
        LeftLateral,            // Left side
        RightLateral,           // Right side
        Superior,               // Top (cranial)
        Inferior,               // Bottom (caudal)

        // Oblique views (Cardiology)
        LAO,                    // Left Anterior Oblique
        RAO,                    // Right Anterior Oblique
        LAO_Cranial,            // LAO with cranial angulation
        LAO_Caudal,             // LAO with caudal angulation
        RAO_Cranial,            // RAO with cranial angulation
        RAO_Caudal,             // RAO with caudal angulation

        // Specific cardiac views
        LAO_30,                 // LAO 30°
        LAO_45,                 // LAO 45°
        LAO_60,                 // LAO 60°
        RAO_30,                 // RAO 30°
        RAO_45,                 // RAO 45°

        // Cath lab standard views
        Spider,                 // LAO 45° Caudal 25° (spider view for LM bifurcation)
        Hepatoclavicular,       // RAO 30° Caudal 25°

        // Free rotation
        Custom
    }

    #endregion

    [Header("References")]
    [SerializeField] private VolumeRenderer volumeRenderer;
    [SerializeField] private Transform volumeTransform;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform pivotPoint;

    [Header("Current State")]
    [SerializeField] private VisualizationPreset currentPreset = VisualizationPreset.Cardiac;
    [SerializeField] private MedicalViewAngle currentViewAngle = MedicalViewAngle.Anterior;

    [Header("Depth Layer Settings")]
    [SerializeField] private bool depthLayerMode = false;
    [SerializeField, Range(0f, 1f)] private float currentDepth = 0f;
    [SerializeField, Range(0.01f, 0.5f)] private float layerThickness = 0.1f;
    [SerializeField] private int numberOfLayers = 10;
    [SerializeField] private bool animateDepth = false;
    [SerializeField] private float depthAnimationSpeed = 0.2f;

    [Header("View Settings")]
    [SerializeField] private float viewDistance = 2f;
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("CT Hounsfield Presets")]
    [SerializeField] private bool useCTHounsfieldUnits = true;

    // Preset configurations
    private Dictionary<VisualizationPreset, PresetConfiguration> presetConfigs;

    // View angle configurations
    private Dictionary<MedicalViewAngle, ViewAngleConfiguration> viewAngleConfigs;

    // Animation state
    private bool isTransitioning = false;
    private float transitionProgress = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

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

        InitializePresets();
        InitializeViewAngles();
    }

    void Start()
    {
        if (volumeRenderer == null)
        {
            volumeRenderer = VolumeRenderer.Instance;
        }

        if (volumeTransform == null && volumeRenderer != null)
        {
            volumeTransform = volumeRenderer.transform;
        }

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main?.transform;
        }

        if (pivotPoint == null && volumeTransform != null)
        {
            pivotPoint = volumeTransform;
        }

        ApplyPreset(currentPreset);
    }

    void Update()
    {
        if (isTransitioning)
        {
            UpdateViewTransition();
        }

        if (depthLayerMode && animateDepth)
        {
            AnimateDepthLayer();
        }
    }

    #region Preset Initialization

    private void InitializePresets()
    {
        presetConfigs = new Dictionary<VisualizationPreset, PresetConfiguration>();

        // Default preset
        presetConfigs[VisualizationPreset.Default] = new PresetConfiguration
        {
            Name = "Default",
            WindowCenter = 0.5f,
            WindowWidth = 0.5f,
            MinThreshold = 0.1f,
            MaxThreshold = 0.9f,
            HounsfieldMin = -1000,
            HounsfieldMax = 1000,
            ColorGradient = CreateGradient(Color.black, Color.white),
            OpacityCurve = AnimationCurve.Linear(0, 0, 1, 1),
            Description = "Standard visualization"
        };

        // Bone preset (CT: 300-3000 HU)
        presetConfigs[VisualizationPreset.Bone] = new PresetConfiguration
        {
            Name = "Osso",
            WindowCenter = 0.75f,
            WindowWidth = 0.35f,
            MinThreshold = 0.55f,
            MaxThreshold = 1.0f,
            HounsfieldMin = 300,
            HounsfieldMax = 3000,
            ColorGradient = CreateGradient(new Color(0.9f, 0.85f, 0.8f), Color.white),
            OpacityCurve = CreateOpacityCurve(0.5f, 1f),
            Description = "Visualização óssea (300-3000 HU)"
        };

        // Soft Tissue preset (CT: -100 to 300 HU)
        presetConfigs[VisualizationPreset.SoftTissue] = new PresetConfiguration
        {
            Name = "Tecidos Moles",
            WindowCenter = 0.4f,
            WindowWidth = 0.4f,
            MinThreshold = 0.2f,
            MaxThreshold = 0.7f,
            HounsfieldMin = -100,
            HounsfieldMax = 300,
            ColorGradient = CreateGradient(new Color(0.8f, 0.6f, 0.5f), new Color(1f, 0.85f, 0.8f)),
            OpacityCurve = CreateOpacityCurve(0.3f, 0.8f),
            Description = "Tecidos moles (-100 a 300 HU)"
        };

        // Blood/Vessels preset (CT: 30-70 HU without contrast, 100-400 with contrast)
        presetConfigs[VisualizationPreset.Blood] = new PresetConfiguration
        {
            Name = "Sangue",
            WindowCenter = 0.45f,
            WindowWidth = 0.25f,
            MinThreshold = 0.3f,
            MaxThreshold = 0.6f,
            HounsfieldMin = 30,
            HounsfieldMax = 70,
            ColorGradient = CreateGradient(new Color(0.5f, 0f, 0f), new Color(1f, 0.2f, 0.1f)),
            OpacityCurve = CreateOpacityCurve(0.4f, 1f),
            Description = "Sangue e vasos (30-70 HU)"
        };

        // Vessels with contrast
        presetConfigs[VisualizationPreset.VesselsWithContrast] = new PresetConfiguration
        {
            Name = "Vasos com Contraste",
            WindowCenter = 0.55f,
            WindowWidth = 0.3f,
            MinThreshold = 0.4f,
            MaxThreshold = 0.85f,
            HounsfieldMin = 100,
            HounsfieldMax = 400,
            ColorGradient = CreateGradient(new Color(0.8f, 0.1f, 0.05f), new Color(1f, 0.4f, 0.3f)),
            OpacityCurve = CreateOpacityCurve(0.5f, 1f),
            Description = "Vasos com contraste (100-400 HU)"
        };

        // Angiography preset
        presetConfigs[VisualizationPreset.Angiography] = new PresetConfiguration
        {
            Name = "Angiografia",
            WindowCenter = 0.6f,
            WindowWidth = 0.35f,
            MinThreshold = 0.35f,
            MaxThreshold = 0.9f,
            HounsfieldMin = 150,
            HounsfieldMax = 500,
            ColorGradient = CreateGradient(Color.black, new Color(1f, 0.3f, 0.2f)),
            OpacityCurve = CreateOpacityCurve(0.3f, 1f),
            Description = "Angiografia (150-500 HU)"
        };

        // Cardiac preset
        presetConfigs[VisualizationPreset.Cardiac] = new PresetConfiguration
        {
            Name = "Cardíaco",
            WindowCenter = 0.45f,
            WindowWidth = 0.45f,
            MinThreshold = 0.15f,
            MaxThreshold = 0.85f,
            HounsfieldMin = -50,
            HounsfieldMax = 350,
            ColorGradient = CreateCardiacGradient(),
            OpacityCurve = CreateOpacityCurve(0.25f, 1f),
            Description = "Coração completo (-50 a 350 HU)"
        };

        // Cardiac Chambers
        presetConfigs[VisualizationPreset.CardiacChambers] = new PresetConfiguration
        {
            Name = "Câmaras Cardíacas",
            WindowCenter = 0.35f,
            WindowWidth = 0.3f,
            MinThreshold = 0.1f,
            MaxThreshold = 0.5f,
            HounsfieldMin = -30,
            HounsfieldMax = 150,
            ColorGradient = CreateGradient(new Color(0.3f, 0f, 0f), new Color(0.9f, 0.2f, 0.15f)),
            OpacityCurve = CreateOpacityCurve(0.2f, 0.7f),
            Description = "Câmaras cardíacas (sangue intracavitário)"
        };

        // Cardiac Wall
        presetConfigs[VisualizationPreset.CardiacWall] = new PresetConfiguration
        {
            Name = "Parede Miocárdica",
            WindowCenter = 0.5f,
            WindowWidth = 0.35f,
            MinThreshold = 0.3f,
            MaxThreshold = 0.75f,
            HounsfieldMin = 30,
            HounsfieldMax = 150,
            ColorGradient = CreateGradient(new Color(0.7f, 0.4f, 0.35f), new Color(1f, 0.7f, 0.6f)),
            OpacityCurve = CreateOpacityCurve(0.4f, 0.9f),
            Description = "Miocárdio (30-150 HU)"
        };

        // Coronaries
        presetConfigs[VisualizationPreset.Coronaries] = new PresetConfiguration
        {
            Name = "Coronárias",
            WindowCenter = 0.6f,
            WindowWidth = 0.3f,
            MinThreshold = 0.45f,
            MaxThreshold = 0.9f,
            HounsfieldMin = 150,
            HounsfieldMax = 450,
            ColorGradient = CreateGradient(new Color(0.9f, 0.1f, 0.05f), new Color(1f, 0.5f, 0.4f)),
            OpacityCurve = CreateOpacityCurve(0.5f, 1f),
            Description = "Artérias coronárias com contraste"
        };

        // Aorta
        presetConfigs[VisualizationPreset.Aorta] = new PresetConfiguration
        {
            Name = "Aorta",
            WindowCenter = 0.55f,
            WindowWidth = 0.35f,
            MinThreshold = 0.35f,
            MaxThreshold = 0.85f,
            HounsfieldMin = 100,
            HounsfieldMax = 400,
            ColorGradient = CreateGradient(new Color(0.7f, 0.05f, 0.02f), new Color(1f, 0.35f, 0.25f)),
            OpacityCurve = CreateOpacityCurve(0.45f, 1f),
            Description = "Aorta com contraste"
        };

        // Lung preset (CT: -950 to -500 HU for parenchyma)
        presetConfigs[VisualizationPreset.Lung] = new PresetConfiguration
        {
            Name = "Pulmão",
            WindowCenter = 0.25f,
            WindowWidth = 0.5f,
            MinThreshold = 0.05f,
            MaxThreshold = 0.45f,
            HounsfieldMin = -950,
            HounsfieldMax = -500,
            ColorGradient = CreateGradient(new Color(0.1f, 0.15f, 0.2f), new Color(0.7f, 0.75f, 0.85f)),
            OpacityCurve = CreateOpacityCurve(0.1f, 0.5f),
            Description = "Parênquima pulmonar (-950 a -500 HU)"
        };

        // Airways
        presetConfigs[VisualizationPreset.Airways] = new PresetConfiguration
        {
            Name = "Vias Aéreas",
            WindowCenter = 0.15f,
            WindowWidth = 0.3f,
            MinThreshold = 0.01f,
            MaxThreshold = 0.25f,
            HounsfieldMin = -1000,
            HounsfieldMax = -900,
            ColorGradient = CreateGradient(Color.black, new Color(0.5f, 0.6f, 0.8f)),
            OpacityCurve = CreateOpacityCurve(0.1f, 0.4f),
            Description = "Vias aéreas (ar)"
        };

        // Bronchi
        presetConfigs[VisualizationPreset.Bronchi] = new PresetConfiguration
        {
            Name = "Brônquios",
            WindowCenter = 0.18f,
            WindowWidth = 0.35f,
            MinThreshold = 0.02f,
            MaxThreshold = 0.3f,
            HounsfieldMin = -1000,
            HounsfieldMax = -800,
            ColorGradient = CreateGradient(new Color(0.1f, 0.1f, 0.2f), new Color(0.6f, 0.65f, 0.8f)),
            OpacityCurve = CreateOpacityCurve(0.15f, 0.5f),
            Description = "Árvore brônquica"
        };

        // Lung Parenchyma
        presetConfigs[VisualizationPreset.LungParenchyma] = new PresetConfiguration
        {
            Name = "Parênquima Pulmonar",
            WindowCenter = 0.22f,
            WindowWidth = 0.45f,
            MinThreshold = 0.03f,
            MaxThreshold = 0.4f,
            HounsfieldMin = -900,
            HounsfieldMax = -400,
            ColorGradient = CreateGradient(new Color(0.15f, 0.2f, 0.25f), new Color(0.75f, 0.8f, 0.9f)),
            OpacityCurve = CreateOpacityCurve(0.12f, 0.55f),
            Description = "Parênquima pulmonar detalhado"
        };

        // Bone with soft tissue
        presetConfigs[VisualizationPreset.BoneWithSoftTissue] = new PresetConfiguration
        {
            Name = "Osso com Tecidos Moles",
            WindowCenter = 0.55f,
            WindowWidth = 0.6f,
            MinThreshold = 0.15f,
            MaxThreshold = 1f,
            HounsfieldMin = -100,
            HounsfieldMax = 3000,
            ColorGradient = CreateBoneWithTissueGradient(),
            OpacityCurve = CreateOpacityCurve(0.2f, 1f),
            Description = "Osso com tecidos moles (-100 a 3000 HU)"
        };

        // Skull
        presetConfigs[VisualizationPreset.Skull] = new PresetConfiguration
        {
            Name = "Crânio",
            WindowCenter = 0.8f,
            WindowWidth = 0.3f,
            MinThreshold = 0.6f,
            MaxThreshold = 1f,
            HounsfieldMin = 400,
            HounsfieldMax = 3000,
            ColorGradient = CreateGradient(new Color(0.85f, 0.8f, 0.75f), Color.white),
            OpacityCurve = CreateOpacityCurve(0.55f, 1f),
            Description = "Crânio e ossos da cabeça"
        };

        // Spine
        presetConfigs[VisualizationPreset.Spine] = new PresetConfiguration
        {
            Name = "Coluna Vertebral",
            WindowCenter = 0.72f,
            WindowWidth = 0.35f,
            MinThreshold = 0.5f,
            MaxThreshold = 0.95f,
            HounsfieldMin = 250,
            HounsfieldMax = 2500,
            ColorGradient = CreateGradient(new Color(0.88f, 0.82f, 0.76f), new Color(1f, 0.98f, 0.95f)),
            OpacityCurve = CreateOpacityCurve(0.5f, 1f),
            Description = "Coluna vertebral"
        };

        // Muscle
        presetConfigs[VisualizationPreset.Muscle] = new PresetConfiguration
        {
            Name = "Músculo",
            WindowCenter = 0.42f,
            WindowWidth = 0.35f,
            MinThreshold = 0.25f,
            MaxThreshold = 0.65f,
            HounsfieldMin = 10,
            HounsfieldMax = 80,
            ColorGradient = CreateGradient(new Color(0.6f, 0.35f, 0.3f), new Color(0.9f, 0.6f, 0.5f)),
            OpacityCurve = CreateOpacityCurve(0.35f, 0.85f),
            Description = "Tecido muscular (10-80 HU)"
        };

        // Fat
        presetConfigs[VisualizationPreset.Fat] = new PresetConfiguration
        {
            Name = "Gordura",
            WindowCenter = 0.3f,
            WindowWidth = 0.25f,
            MinThreshold = 0.15f,
            MaxThreshold = 0.45f,
            HounsfieldMin = -150,
            HounsfieldMax = -50,
            ColorGradient = CreateGradient(new Color(0.9f, 0.85f, 0.5f), new Color(1f, 0.95f, 0.7f)),
            OpacityCurve = CreateOpacityCurve(0.25f, 0.7f),
            Description = "Tecido adiposo (-150 a -50 HU)"
        };

        // Vessels (without contrast)
        presetConfigs[VisualizationPreset.Vessels] = new PresetConfiguration
        {
            Name = "Vasos",
            WindowCenter = 0.42f,
            WindowWidth = 0.22f,
            MinThreshold = 0.28f,
            MaxThreshold = 0.58f,
            HounsfieldMin = 25,
            HounsfieldMax = 65,
            ColorGradient = CreateGradient(new Color(0.6f, 0.05f, 0.02f), new Color(1f, 0.25f, 0.15f)),
            OpacityCurve = CreateOpacityCurve(0.38f, 0.95f),
            Description = "Vasos sanguíneos (sem contraste)"
        };

        // Valves
        presetConfigs[VisualizationPreset.Valves] = new PresetConfiguration
        {
            Name = "Válvulas Cardíacas",
            WindowCenter = 0.52f,
            WindowWidth = 0.3f,
            MinThreshold = 0.35f,
            MaxThreshold = 0.7f,
            HounsfieldMin = 50,
            HounsfieldMax = 200,
            ColorGradient = CreateGradient(new Color(0.75f, 0.45f, 0.4f), new Color(1f, 0.75f, 0.65f)),
            OpacityCurve = CreateOpacityCurve(0.42f, 0.92f),
            Description = "Válvulas cardíacas"
        };

        // MinIP (Minimum Intensity Projection)
        presetConfigs[VisualizationPreset.MinIP] = new PresetConfiguration
        {
            Name = "MinIP",
            WindowCenter = 0.3f,
            WindowWidth = 1f,
            MinThreshold = 0f,
            MaxThreshold = 0.5f,
            HounsfieldMin = -1000,
            HounsfieldMax = 0,
            ColorGradient = CreateGradient(Color.black, Color.white),
            OpacityCurve = AnimationCurve.Linear(0, 0.1f, 1, 0f),
            Description = "Projeção de Intensidade Mínima"
        };

        // Average IP
        presetConfigs[VisualizationPreset.AverageIP] = new PresetConfiguration
        {
            Name = "Média IP",
            WindowCenter = 0.5f,
            WindowWidth = 0.8f,
            MinThreshold = 0.1f,
            MaxThreshold = 0.9f,
            HounsfieldMin = -500,
            HounsfieldMax = 1500,
            ColorGradient = CreateGradient(Color.black, Color.white),
            OpacityCurve = AnimationCurve.Linear(0, 0.05f, 1, 0.15f),
            Description = "Projeção de Intensidade Média"
        };

        // Surface Shaded
        presetConfigs[VisualizationPreset.SurfaceShaded] = new PresetConfiguration
        {
            Name = "Superfície Sombreada",
            WindowCenter = 0.5f,
            WindowWidth = 0.4f,
            MinThreshold = 0.3f,
            MaxThreshold = 0.8f,
            HounsfieldMin = -100,
            HounsfieldMax = 400,
            ColorGradient = CreateGradient(new Color(0.6f, 0.5f, 0.45f), new Color(1f, 0.9f, 0.85f)),
            OpacityCurve = CreateSurfaceShadedCurve(),
            Description = "Renderização de superfície sombreada"
        };

        // Volume Rendered
        presetConfigs[VisualizationPreset.VolumeRendered] = new PresetConfiguration
        {
            Name = "Volume Renderizado",
            WindowCenter = 0.45f,
            WindowWidth = 0.5f,
            MinThreshold = 0.1f,
            MaxThreshold = 0.9f,
            HounsfieldMin = -200,
            HounsfieldMax = 500,
            ColorGradient = CreateVolumeRenderedGradient(),
            OpacityCurve = CreateOpacityCurve(0.15f, 0.85f),
            Description = "Renderização volumétrica padrão"
        };

        // MIP (Maximum Intensity Projection)
        presetConfigs[VisualizationPreset.MIP] = new PresetConfiguration
        {
            Name = "MIP",
            WindowCenter = 0.5f,
            WindowWidth = 1f,
            MinThreshold = 0f,
            MaxThreshold = 1f,
            HounsfieldMin = -1000,
            HounsfieldMax = 3000,
            ColorGradient = CreateGradient(Color.black, Color.white),
            OpacityCurve = AnimationCurve.Linear(0, 0, 1, 0.1f),
            Description = "Projeção de Intensidade Máxima"
        };

        // X-Ray like
        presetConfigs[VisualizationPreset.XRay] = new PresetConfiguration
        {
            Name = "Raio-X",
            WindowCenter = 0.5f,
            WindowWidth = 0.8f,
            MinThreshold = 0.1f,
            MaxThreshold = 0.95f,
            HounsfieldMin = -500,
            HounsfieldMax = 2000,
            ColorGradient = CreateGradient(Color.white, Color.black),
            OpacityCurve = AnimationCurve.Linear(0, 0.05f, 1, 0.3f),
            Description = "Visualização tipo Raio-X"
        };
    }

    private void InitializeViewAngles()
    {
        viewAngleConfigs = new Dictionary<MedicalViewAngle, ViewAngleConfiguration>();

        // Standard views
        viewAngleConfigs[MedicalViewAngle.Anterior] = new ViewAngleConfiguration
        {
            Name = "Anterior (AP)",
            Rotation = Quaternion.Euler(0, 0, 0),
            Description = "Vista anterior (frente)"
        };

        viewAngleConfigs[MedicalViewAngle.Posterior] = new ViewAngleConfiguration
        {
            Name = "Posterior (PA)",
            Rotation = Quaternion.Euler(0, 180, 0),
            Description = "Vista posterior (costas)"
        };

        viewAngleConfigs[MedicalViewAngle.LeftLateral] = new ViewAngleConfiguration
        {
            Name = "Lateral Esquerda",
            Rotation = Quaternion.Euler(0, 90, 0),
            Description = "Vista lateral esquerda"
        };

        viewAngleConfigs[MedicalViewAngle.RightLateral] = new ViewAngleConfiguration
        {
            Name = "Lateral Direita",
            Rotation = Quaternion.Euler(0, -90, 0),
            Description = "Vista lateral direita"
        };

        viewAngleConfigs[MedicalViewAngle.Superior] = new ViewAngleConfiguration
        {
            Name = "Superior (Cranial)",
            Rotation = Quaternion.Euler(90, 0, 0),
            Description = "Vista superior/cranial"
        };

        viewAngleConfigs[MedicalViewAngle.Inferior] = new ViewAngleConfiguration
        {
            Name = "Inferior (Caudal)",
            Rotation = Quaternion.Euler(-90, 0, 0),
            Description = "Vista inferior/caudal"
        };

        // Oblique views - LAO (Left Anterior Oblique)
        viewAngleConfigs[MedicalViewAngle.LAO] = new ViewAngleConfiguration
        {
            Name = "LAO 45°",
            Rotation = Quaternion.Euler(0, 45, 0),
            Description = "Oblíqua anterior esquerda 45°"
        };

        viewAngleConfigs[MedicalViewAngle.LAO_30] = new ViewAngleConfiguration
        {
            Name = "LAO 30°",
            Rotation = Quaternion.Euler(0, 30, 0),
            Description = "Oblíqua anterior esquerda 30°"
        };

        viewAngleConfigs[MedicalViewAngle.LAO_45] = new ViewAngleConfiguration
        {
            Name = "LAO 45°",
            Rotation = Quaternion.Euler(0, 45, 0),
            Description = "Oblíqua anterior esquerda 45°"
        };

        viewAngleConfigs[MedicalViewAngle.LAO_60] = new ViewAngleConfiguration
        {
            Name = "LAO 60°",
            Rotation = Quaternion.Euler(0, 60, 0),
            Description = "Oblíqua anterior esquerda 60°"
        };

        viewAngleConfigs[MedicalViewAngle.LAO_Cranial] = new ViewAngleConfiguration
        {
            Name = "LAO Cranial",
            Rotation = Quaternion.Euler(25, 45, 0),
            Description = "LAO 45° com angulação cranial 25°"
        };

        viewAngleConfigs[MedicalViewAngle.LAO_Caudal] = new ViewAngleConfiguration
        {
            Name = "LAO Caudal",
            Rotation = Quaternion.Euler(-25, 45, 0),
            Description = "LAO 45° com angulação caudal 25°"
        };

        // RAO (Right Anterior Oblique)
        viewAngleConfigs[MedicalViewAngle.RAO] = new ViewAngleConfiguration
        {
            Name = "RAO 30°",
            Rotation = Quaternion.Euler(0, -30, 0),
            Description = "Oblíqua anterior direita 30°"
        };

        viewAngleConfigs[MedicalViewAngle.RAO_30] = new ViewAngleConfiguration
        {
            Name = "RAO 30°",
            Rotation = Quaternion.Euler(0, -30, 0),
            Description = "Oblíqua anterior direita 30°"
        };

        viewAngleConfigs[MedicalViewAngle.RAO_45] = new ViewAngleConfiguration
        {
            Name = "RAO 45°",
            Rotation = Quaternion.Euler(0, -45, 0),
            Description = "Oblíqua anterior direita 45°"
        };

        viewAngleConfigs[MedicalViewAngle.RAO_Cranial] = new ViewAngleConfiguration
        {
            Name = "RAO Cranial",
            Rotation = Quaternion.Euler(25, -30, 0),
            Description = "RAO 30° com angulação cranial 25°"
        };

        viewAngleConfigs[MedicalViewAngle.RAO_Caudal] = new ViewAngleConfiguration
        {
            Name = "RAO Caudal",
            Rotation = Quaternion.Euler(-25, -30, 0),
            Description = "RAO 30° com angulação caudal 25°"
        };

        // Special cath lab views
        viewAngleConfigs[MedicalViewAngle.Spider] = new ViewAngleConfiguration
        {
            Name = "Spider",
            Rotation = Quaternion.Euler(-25, 45, 0),
            Description = "LAO 45° Caudal 25° (vista spider para bifurcação do TC)"
        };

        viewAngleConfigs[MedicalViewAngle.Hepatoclavicular] = new ViewAngleConfiguration
        {
            Name = "Hepatoclavicular",
            Rotation = Quaternion.Euler(-25, -30, 0),
            Description = "RAO 30° Caudal 25° (hepatoclavicular)"
        };
    }

    #endregion

    #region Gradient Helpers

    private Gradient CreateGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[2];
        colorKeys[0] = new GradientColorKey(start, 0f);
        colorKeys[1] = new GradientColorKey(end, 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private Gradient CreateCardiacGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(0.4f, 0.1f, 0.1f), 0f);
        colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.2f, 0.15f), 0.35f);
        colorKeys[2] = new GradientColorKey(new Color(0.95f, 0.5f, 0.4f), 0.65f);
        colorKeys[3] = new GradientColorKey(new Color(1f, 0.85f, 0.8f), 1f);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.6f, 0.25f);
        alphaKeys[2] = new GradientAlphaKey(0.85f, 0.5f);
        alphaKeys[3] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private AnimationCurve CreateOpacityCurve(float startOpacity, float endOpacity)
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.2f, startOpacity);
        curve.AddKey(1f, endOpacity);
        return curve;
    }

    private Gradient CreateBoneWithTissueGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[4];
        colorKeys[0] = new GradientColorKey(new Color(0.7f, 0.5f, 0.45f), 0f);      // Soft tissue
        colorKeys[1] = new GradientColorKey(new Color(0.85f, 0.65f, 0.55f), 0.35f); // Mixed
        colorKeys[2] = new GradientColorKey(new Color(0.95f, 0.9f, 0.85f), 0.6f);   // Cortical bone
        colorKeys[3] = new GradientColorKey(Color.white, 1f);                        // Dense bone

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];
        alphaKeys[0] = new GradientAlphaKey(0.15f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.4f, 0.3f);
        alphaKeys[2] = new GradientAlphaKey(0.8f, 0.6f);
        alphaKeys[3] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private Gradient CreateVolumeRenderedGradient()
    {
        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[5];
        colorKeys[0] = new GradientColorKey(new Color(0.2f, 0.2f, 0.3f), 0f);       // Air/background
        colorKeys[1] = new GradientColorKey(new Color(0.5f, 0.3f, 0.25f), 0.25f);   // Fat/low density
        colorKeys[2] = new GradientColorKey(new Color(0.8f, 0.55f, 0.45f), 0.5f);   // Soft tissue
        colorKeys[3] = new GradientColorKey(new Color(0.95f, 0.8f, 0.7f), 0.75f);   // Dense tissue
        colorKeys[4] = new GradientColorKey(Color.white, 1f);                        // Bone

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[5];
        alphaKeys[0] = new GradientAlphaKey(0f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.2f, 0.2f);
        alphaKeys[2] = new GradientAlphaKey(0.5f, 0.45f);
        alphaKeys[3] = new GradientAlphaKey(0.8f, 0.7f);
        alphaKeys[4] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

    private AnimationCurve CreateSurfaceShadedCurve()
    {
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0f);
        curve.AddKey(0.25f, 0.05f);
        curve.AddKey(0.35f, 0.3f);
        curve.AddKey(0.45f, 0.9f);
        curve.AddKey(1f, 1f);
        return curve;
    }

    #endregion

    #region Preset Application

    /// <summary>
    /// Apply a visualization preset.
    /// </summary>
    public void ApplyPreset(VisualizationPreset preset)
    {
        if (!presetConfigs.ContainsKey(preset))
        {
            Debug.LogWarning($"Preset {preset} not found, using default.");
            preset = VisualizationPreset.Default;
        }

        currentPreset = preset;
        PresetConfiguration config = presetConfigs[preset];

        if (volumeRenderer != null)
        {
            volumeRenderer.SetWindowLevel(config.WindowCenter, config.WindowWidth);
            volumeRenderer.SetThreshold(config.MinThreshold, config.MaxThreshold);
            volumeRenderer.SetTransferFunction(config.ColorGradient, config.OpacityCurve);
        }

        OnPresetChanged?.Invoke(this, new PresetChangedEventArgs
        {
            Preset = preset,
            PresetName = config.Name
        });

        Debug.Log($"Applied preset: {config.Name}");
    }

    /// <summary>
    /// Get preset configuration.
    /// </summary>
    public PresetConfiguration GetPresetConfiguration(VisualizationPreset preset)
    {
        return presetConfigs.ContainsKey(preset) ? presetConfigs[preset] : null;
    }

    /// <summary>
    /// Get all available presets.
    /// </summary>
    public List<VisualizationPreset> GetAvailablePresets()
    {
        return new List<VisualizationPreset>(presetConfigs.Keys);
    }

    /// <summary>
    /// Get current preset.
    /// </summary>
    public VisualizationPreset GetCurrentPreset()
    {
        return currentPreset;
    }

    #endregion

    #region View Angle Control

    /// <summary>
    /// Set the viewing angle.
    /// </summary>
    public void SetViewAngle(MedicalViewAngle angle, bool animate = true)
    {
        if (!viewAngleConfigs.ContainsKey(angle))
        {
            Debug.LogWarning($"View angle {angle} not configured.");
            return;
        }

        currentViewAngle = angle;
        ViewAngleConfiguration config = viewAngleConfigs[angle];

        // Calculate camera position based on pivot and view distance
        Vector3 pivotPos = pivotPoint != null ? pivotPoint.position : Vector3.zero;
        Vector3 viewDirection = config.Rotation * Vector3.forward;
        targetPosition = pivotPos - viewDirection * viewDistance;
        targetRotation = config.Rotation;

        if (animate && transitionDuration > 0)
        {
            StartViewTransition();
        }
        else
        {
            ApplyViewImmediate();
        }

        OnViewAngleChanged?.Invoke(this, new ViewAngleChangedEventArgs
        {
            Angle = angle,
            CameraPosition = targetPosition,
            CameraRotation = targetRotation
        });

        Debug.Log($"View angle set to: {config.Name}");
    }

    /// <summary>
    /// Set custom view angle.
    /// </summary>
    public void SetCustomViewAngle(float lateralAngle, float cranialCaudalAngle)
    {
        currentViewAngle = MedicalViewAngle.Custom;

        Quaternion rotation = Quaternion.Euler(cranialCaudalAngle, lateralAngle, 0);

        Vector3 pivotPos = pivotPoint != null ? pivotPoint.position : Vector3.zero;
        Vector3 viewDirection = rotation * Vector3.forward;
        targetPosition = pivotPos - viewDirection * viewDistance;
        targetRotation = rotation;

        StartViewTransition();

        OnViewAngleChanged?.Invoke(this, new ViewAngleChangedEventArgs
        {
            Angle = MedicalViewAngle.Custom,
            CameraPosition = targetPosition,
            CameraRotation = targetRotation
        });
    }

    private void StartViewTransition()
    {
        if (cameraTransform == null) return;

        startPosition = cameraTransform.position;
        startRotation = cameraTransform.rotation;
        transitionProgress = 0f;
        isTransitioning = true;
    }

    private void UpdateViewTransition()
    {
        transitionProgress += Time.deltaTime / transitionDuration;

        if (transitionProgress >= 1f)
        {
            transitionProgress = 1f;
            isTransitioning = false;
        }

        float t = transitionCurve.Evaluate(transitionProgress);

        if (cameraTransform != null)
        {
            cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
        }
    }

    private void ApplyViewImmediate()
    {
        if (cameraTransform != null)
        {
            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
        }
    }

    /// <summary>
    /// Get current view angle.
    /// </summary>
    public MedicalViewAngle GetCurrentViewAngle()
    {
        return currentViewAngle;
    }

    /// <summary>
    /// Get view angle configuration.
    /// </summary>
    public ViewAngleConfiguration GetViewAngleConfiguration(MedicalViewAngle angle)
    {
        return viewAngleConfigs.ContainsKey(angle) ? viewAngleConfigs[angle] : null;
    }

    #endregion

    #region Depth Layer Control

    /// <summary>
    /// Enable or disable depth layer mode.
    /// </summary>
    public void SetDepthLayerMode(bool enabled)
    {
        depthLayerMode = enabled;

        if (volumeRenderer != null && enabled)
        {
            // Enable clipping plane for layer mode
            volumeRenderer.SetClipPlane(true);
        }

        UpdateDepthLayer();
    }

    /// <summary>
    /// Set current depth (0 = surface, 1 = deepest).
    /// </summary>
    public void SetDepth(float depth)
    {
        currentDepth = Mathf.Clamp01(depth);
        UpdateDepthLayer();

        int currentLayer = Mathf.FloorToInt(currentDepth * numberOfLayers);

        OnDepthLayerChanged?.Invoke(this, new DepthLayerChangedEventArgs
        {
            CurrentDepth = currentDepth,
            LayerThickness = layerThickness,
            CurrentLayer = currentLayer,
            TotalLayers = numberOfLayers
        });
    }

    /// <summary>
    /// Set layer by index.
    /// </summary>
    public void SetLayer(int layerIndex)
    {
        layerIndex = Mathf.Clamp(layerIndex, 0, numberOfLayers - 1);
        SetDepth((float)layerIndex / (numberOfLayers - 1));
    }

    /// <summary>
    /// Go to next layer.
    /// </summary>
    public void NextLayer()
    {
        int currentLayer = Mathf.FloorToInt(currentDepth * numberOfLayers);
        SetLayer(currentLayer + 1);
    }

    /// <summary>
    /// Go to previous layer.
    /// </summary>
    public void PreviousLayer()
    {
        int currentLayer = Mathf.FloorToInt(currentDepth * numberOfLayers);
        SetLayer(currentLayer - 1);
    }

    /// <summary>
    /// Set layer thickness.
    /// </summary>
    public void SetLayerThickness(float thickness)
    {
        layerThickness = Mathf.Clamp(thickness, 0.01f, 0.5f);
        UpdateDepthLayer();
    }

    /// <summary>
    /// Toggle depth animation.
    /// </summary>
    public void SetDepthAnimation(bool enabled)
    {
        animateDepth = enabled;
    }

    private void UpdateDepthLayer()
    {
        if (!depthLayerMode || volumeRenderer == null) return;

        // Update threshold to show only current layer
        float layerStart = currentDepth - layerThickness / 2f;
        float layerEnd = currentDepth + layerThickness / 2f;

        volumeRenderer.SetThreshold(Mathf.Max(0, layerStart), Mathf.Min(1, layerEnd));
    }

    private void AnimateDepthLayer()
    {
        currentDepth += depthAnimationSpeed * Time.deltaTime;
        if (currentDepth > 1f)
        {
            currentDepth = 0f;
        }
        UpdateDepthLayer();
    }

    /// <summary>
    /// Get current depth.
    /// </summary>
    public float GetCurrentDepth()
    {
        return currentDepth;
    }

    /// <summary>
    /// Get current layer index.
    /// </summary>
    public int GetCurrentLayer()
    {
        return Mathf.FloorToInt(currentDepth * numberOfLayers);
    }

    /// <summary>
    /// Check if depth layer mode is active.
    /// </summary>
    public bool IsDepthLayerMode()
    {
        return depthLayerMode;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Convert CT Hounsfield units to normalized value (0-1).
    /// </summary>
    public float HounsfieldToNormalized(int hu)
    {
        // Typical CT range: -1024 to +3071 HU
        const int minHU = -1024;
        const int maxHU = 3071;

        return Mathf.InverseLerp(minHU, maxHU, hu);
    }

    /// <summary>
    /// Convert normalized value to Hounsfield units.
    /// </summary>
    public int NormalizedToHounsfield(float normalized)
    {
        const int minHU = -1024;
        const int maxHU = 3071;

        return Mathf.RoundToInt(Mathf.Lerp(minHU, maxHU, normalized));
    }

    /// <summary>
    /// Set view distance from pivot.
    /// </summary>
    public void SetViewDistance(float distance)
    {
        viewDistance = Mathf.Max(0.5f, distance);

        // Update current view if we have one
        if (currentViewAngle != MedicalViewAngle.Custom)
        {
            SetViewAngle(currentViewAngle, false);
        }
    }

    #endregion
}

/// <summary>
/// Configuration for a visualization preset.
/// </summary>
[System.Serializable]
public class PresetConfiguration
{
    public string Name;
    public string Description;
    public float WindowCenter;
    public float WindowWidth;
    public float MinThreshold;
    public float MaxThreshold;
    public int HounsfieldMin;
    public int HounsfieldMax;
    public Gradient ColorGradient;
    public AnimationCurve OpacityCurve;
}

/// <summary>
/// Configuration for a medical viewing angle.
/// </summary>
[System.Serializable]
public class ViewAngleConfiguration
{
    public string Name;
    public string Description;
    public Quaternion Rotation;
}

// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Manages virtual room environments for cardiac surgery planning.
/// Supports loading pre-defined rooms (OR, Cath Lab) and custom room models.
/// </summary>
public class VirtualRoomManager : MonoBehaviour
{
    public static VirtualRoomManager Instance { get; private set; }

    public event EventHandler<RoomChangedEventArgs> OnRoomChanged;
    public event EventHandler<RoomLoadedEventArgs> OnRoomLoaded;
    public event EventHandler<EquipmentPlacedEventArgs> OnEquipmentPlaced;

    public class RoomChangedEventArgs : EventArgs
    {
        public RoomType PreviousRoom;
        public RoomType CurrentRoom;
        public string RoomName;
    }

    public class RoomLoadedEventArgs : EventArgs
    {
        public GameObject RoomObject;
        public RoomType RoomType;
        public bool IsCustomRoom;
    }

    public class EquipmentPlacedEventArgs : EventArgs
    {
        public GameObject Equipment;
        public EquipmentType Type;
        public Vector3 Position;
    }

    /// <summary>
    /// Types of virtual rooms available.
    /// </summary>
    public enum RoomType
    {
        None,
        OperatingRoom,          // Sala de Bloco Operatório
        HemodynamicsLab,        // Laboratório de Hemodinâmica / Cath Lab
        HybridOR,               // Sala Híbrida
        ICU,                    // Unidade de Cuidados Intensivos
        ExaminationRoom,        // Sala de Exame
        ConferenceRoom,         // Sala de Conferência/Discussão
        Custom                  // Sala Personalizada
    }

    /// <summary>
    /// Types of medical equipment that can be placed in rooms.
    /// </summary>
    public enum EquipmentType
    {
        None,
        // Operating Room Equipment
        OperatingTable,
        SurgicalLights,
        AnesthesiaMachine,
        PatientMonitor,
        Defibrillator,
        SurgicalInstrumentTable,
        MayoStand,
        ElectrosurgicalUnit,
        SuctionUnit,
        IVPole,

        // Cath Lab Equipment
        CArm,
        FluoroscopySystem,
        IVUSConsole,
        HemodynamicMonitor,
        ContrastInjector,

        // General Equipment
        MedicalCart,
        WallMonitor,
        Ventilator,
        ECMO,
        HeartLungMachine
    }

    [Header("Current State")]
    [SerializeField] private RoomType currentRoomType = RoomType.None;
    [SerializeField] private GameObject currentRoomInstance;

    [Header("Room Prefabs")]
    [SerializeField] private GameObject operatingRoomPrefab;
    [SerializeField] private GameObject hemodynamicsLabPrefab;
    [SerializeField] private GameObject hybridORPrefab;
    [SerializeField] private GameObject icuPrefab;
    [SerializeField] private GameObject examinationRoomPrefab;
    [SerializeField] private GameObject conferenceRoomPrefab;

    [Header("Equipment Prefabs")]
    [SerializeField] private GameObject operatingTablePrefab;
    [SerializeField] private GameObject surgicalLightsPrefab;
    [SerializeField] private GameObject anesthesiaMachinePrefab;
    [SerializeField] private GameObject patientMonitorPrefab;
    [SerializeField] private GameObject cArmPrefab;
    [SerializeField] private GameObject ivusPrefab;

    [Header("Room Settings")]
    [SerializeField] private Transform roomParent;
    [SerializeField] private Vector3 defaultRoomPosition = Vector3.zero;
    [SerializeField] private float roomScale = 1f;

    [Header("Lighting")]
    [SerializeField] private Light[] roomLights;
    [SerializeField] private float operatingRoomLightIntensity = 1.5f;
    [SerializeField] private float cathLabLightIntensity = 0.8f;
    [SerializeField] private Color operatingRoomLightColor = Color.white;
    [SerializeField] private Color cathLabLightColor = new Color(0.9f, 0.95f, 1f);

    [Header("Environment")]
    [SerializeField] private Material operatingRoomSkybox;
    [SerializeField] private Material cathLabSkybox;
    [SerializeField] private Color ambientLightOR = new Color(0.8f, 0.8f, 0.85f);
    [SerializeField] private Color ambientLightCathLab = new Color(0.4f, 0.45f, 0.5f);

    // Placed equipment tracking
    private List<GameObject> placedEquipment = new List<GameObject>();
    private Dictionary<string, RoomConfiguration> roomConfigurations = new Dictionary<string, RoomConfiguration>();

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

        InitializeRoomConfigurations();
    }

    void Start()
    {
        if (roomParent == null)
        {
            roomParent = transform;
        }
    }

    /// <summary>
    /// Initialize default room configurations.
    /// </summary>
    private void InitializeRoomConfigurations()
    {
        // Operating Room Configuration
        roomConfigurations["OperatingRoom"] = new RoomConfiguration
        {
            RoomType = RoomType.OperatingRoom,
            DisplayName = "Sala de Bloco Operatório",
            Description = "Standard cardiac operating room with surgical equipment",
            Dimensions = new Vector3(8f, 3.5f, 8f),
            LightIntensity = operatingRoomLightIntensity,
            LightColor = operatingRoomLightColor,
            AmbientColor = ambientLightOR,
            DefaultEquipment = new List<EquipmentPlacement>
            {
                new EquipmentPlacement { Type = EquipmentType.OperatingTable, Position = Vector3.zero, Rotation = Quaternion.identity },
                new EquipmentPlacement { Type = EquipmentType.SurgicalLights, Position = new Vector3(0, 2.5f, 0), Rotation = Quaternion.identity },
                new EquipmentPlacement { Type = EquipmentType.AnesthesiaMachine, Position = new Vector3(-2f, 0, -1f), Rotation = Quaternion.Euler(0, 90, 0) },
                new EquipmentPlacement { Type = EquipmentType.PatientMonitor, Position = new Vector3(-2f, 1.2f, 1f), Rotation = Quaternion.Euler(0, 90, 0) },
                new EquipmentPlacement { Type = EquipmentType.HeartLungMachine, Position = new Vector3(2f, 0, -1f), Rotation = Quaternion.Euler(0, -90, 0) }
            }
        };

        // Hemodynamics Lab / Cath Lab Configuration
        roomConfigurations["HemodynamicsLab"] = new RoomConfiguration
        {
            RoomType = RoomType.HemodynamicsLab,
            DisplayName = "Laboratório de Hemodinâmica",
            Description = "Cardiac catheterization laboratory with fluoroscopy",
            Dimensions = new Vector3(7f, 3f, 6f),
            LightIntensity = cathLabLightIntensity,
            LightColor = cathLabLightColor,
            AmbientColor = ambientLightCathLab,
            DefaultEquipment = new List<EquipmentPlacement>
            {
                new EquipmentPlacement { Type = EquipmentType.OperatingTable, Position = Vector3.zero, Rotation = Quaternion.identity },
                new EquipmentPlacement { Type = EquipmentType.CArm, Position = new Vector3(0, 0, -0.5f), Rotation = Quaternion.identity },
                new EquipmentPlacement { Type = EquipmentType.HemodynamicMonitor, Position = new Vector3(-2.5f, 1f, 0), Rotation = Quaternion.Euler(0, 90, 0) },
                new EquipmentPlacement { Type = EquipmentType.IVUSConsole, Position = new Vector3(2f, 0, 1f), Rotation = Quaternion.Euler(0, -45, 0) },
                new EquipmentPlacement { Type = EquipmentType.ContrastInjector, Position = new Vector3(1.5f, 0, -1.5f), Rotation = Quaternion.Euler(0, -90, 0) }
            }
        };

        // Hybrid OR Configuration
        roomConfigurations["HybridOR"] = new RoomConfiguration
        {
            RoomType = RoomType.HybridOR,
            DisplayName = "Sala Híbrida",
            Description = "Combined operating room and catheterization lab",
            Dimensions = new Vector3(10f, 4f, 10f),
            LightIntensity = 1.2f,
            LightColor = Color.white,
            AmbientColor = new Color(0.6f, 0.6f, 0.65f),
            DefaultEquipment = new List<EquipmentPlacement>
            {
                new EquipmentPlacement { Type = EquipmentType.OperatingTable, Position = Vector3.zero, Rotation = Quaternion.identity },
                new EquipmentPlacement { Type = EquipmentType.SurgicalLights, Position = new Vector3(0, 3f, 0), Rotation = Quaternion.identity },
                new EquipmentPlacement { Type = EquipmentType.CArm, Position = new Vector3(0, 0, -1f), Rotation = Quaternion.identity },
                new EquipmentPlacement { Type = EquipmentType.AnesthesiaMachine, Position = new Vector3(-3f, 0, -2f), Rotation = Quaternion.Euler(0, 90, 0) },
                new EquipmentPlacement { Type = EquipmentType.HeartLungMachine, Position = new Vector3(3f, 0, -2f), Rotation = Quaternion.Euler(0, -90, 0) },
                new EquipmentPlacement { Type = EquipmentType.HemodynamicMonitor, Position = new Vector3(-3f, 1f, 2f), Rotation = Quaternion.Euler(0, 90, 0) }
            }
        };
    }

    #region Room Loading

    /// <summary>
    /// Load a room by type.
    /// </summary>
    public void LoadRoom(RoomType roomType)
    {
        RoomType previousRoom = currentRoomType;

        // Unload current room
        UnloadCurrentRoom();

        currentRoomType = roomType;

        if (roomType == RoomType.None)
        {
            OnRoomChanged?.Invoke(this, new RoomChangedEventArgs
            {
                PreviousRoom = previousRoom,
                CurrentRoom = roomType,
                RoomName = "None"
            });
            return;
        }

        // Get room prefab
        GameObject roomPrefab = GetRoomPrefab(roomType);
        string configKey = roomType.ToString();

        if (roomPrefab != null)
        {
            // Instantiate room prefab
            currentRoomInstance = Instantiate(roomPrefab, defaultRoomPosition, Quaternion.identity, roomParent);
            currentRoomInstance.transform.localScale = Vector3.one * roomScale;
        }
        else
        {
            // Create procedural room
            currentRoomInstance = CreateProceduralRoom(roomType);
        }

        // Apply room configuration
        if (roomConfigurations.ContainsKey(configKey))
        {
            ApplyRoomConfiguration(roomConfigurations[configKey]);
        }

        OnRoomLoaded?.Invoke(this, new RoomLoadedEventArgs
        {
            RoomObject = currentRoomInstance,
            RoomType = roomType,
            IsCustomRoom = false
        });

        OnRoomChanged?.Invoke(this, new RoomChangedEventArgs
        {
            PreviousRoom = previousRoom,
            CurrentRoom = roomType,
            RoomName = GetRoomDisplayName(roomType)
        });

        Debug.Log($"Room loaded: {roomType}");
    }

    /// <summary>
    /// Load a custom room from a file path.
    /// </summary>
    public void LoadCustomRoom(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Custom room file not found: {filePath}");
            return;
        }

        string extension = Path.GetExtension(filePath).ToLower();

        if (extension == ".stl")
        {
            LoadCustomRoomFromSTL(filePath);
        }
        else if (extension == ".obj")
        {
            LoadCustomRoomFromOBJ(filePath);
        }
        else if (extension == ".fbx" || extension == ".glb" || extension == ".gltf")
        {
            Debug.LogWarning($"Format {extension} requires runtime import plugin. Using placeholder.");
            LoadRoom(RoomType.OperatingRoom);
        }
        else
        {
            Debug.LogError($"Unsupported room file format: {extension}");
        }
    }

    private void LoadCustomRoomFromSTL(string filePath)
    {
        // Use STL importer
        STLImporter.STLImportResult result = STLImporter.Import(filePath);

        if (!result.Success)
        {
            Debug.LogError($"Failed to load custom room: {result.ErrorMessage}");
            return;
        }

        UnloadCurrentRoom();

        currentRoomInstance = new GameObject("CustomRoom");
        currentRoomInstance.transform.SetParent(roomParent);
        currentRoomInstance.transform.position = defaultRoomPosition;

        MeshFilter meshFilter = currentRoomInstance.AddComponent<MeshFilter>();
        meshFilter.mesh = result.Mesh;

        MeshRenderer meshRenderer = currentRoomInstance.AddComponent<MeshRenderer>();
        meshRenderer.material = CreateRoomMaterial();

        MeshCollider collider = currentRoomInstance.AddComponent<MeshCollider>();
        collider.sharedMesh = result.Mesh;

        currentRoomType = RoomType.Custom;

        // Scale to reasonable room size
        Bounds bounds = result.Mesh.bounds;
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        float targetSize = 8f;
        currentRoomInstance.transform.localScale = Vector3.one * (targetSize / maxDimension);

        OnRoomLoaded?.Invoke(this, new RoomLoadedEventArgs
        {
            RoomObject = currentRoomInstance,
            RoomType = RoomType.Custom,
            IsCustomRoom = true
        });

        Debug.Log($"Custom room loaded from: {filePath}");
    }

    private void LoadCustomRoomFromOBJ(string filePath)
    {
        // Simple OBJ loader for room geometry
        StartCoroutine(LoadOBJAsync(filePath));
    }

    private IEnumerator LoadOBJAsync(string filePath)
    {
        yield return null;

        // Basic OBJ loading - in production, use a proper OBJ importer
        Debug.Log($"Loading OBJ room from: {filePath}");

        // Fallback to procedural room
        LoadRoom(RoomType.OperatingRoom);
    }

    /// <summary>
    /// Unload the current room.
    /// </summary>
    public void UnloadCurrentRoom()
    {
        if (currentRoomInstance != null)
        {
            Destroy(currentRoomInstance);
            currentRoomInstance = null;
        }

        // Clear placed equipment
        foreach (GameObject equipment in placedEquipment)
        {
            if (equipment != null)
            {
                Destroy(equipment);
            }
        }
        placedEquipment.Clear();

        currentRoomType = RoomType.None;
    }

    #endregion

    #region Procedural Room Generation

    /// <summary>
    /// Create a procedural room based on type.
    /// </summary>
    private GameObject CreateProceduralRoom(RoomType roomType)
    {
        RoomConfiguration config = null;
        string configKey = roomType.ToString();

        if (roomConfigurations.ContainsKey(configKey))
        {
            config = roomConfigurations[configKey];
        }

        Vector3 dimensions = config?.Dimensions ?? new Vector3(8f, 3.5f, 8f);

        GameObject room = new GameObject($"ProceduralRoom_{roomType}");
        room.transform.SetParent(roomParent);
        room.transform.position = defaultRoomPosition;

        // Create floor
        GameObject floor = CreateRoomSurface("Floor", new Vector3(dimensions.x, 0.1f, dimensions.z),
            new Vector3(0, -0.05f, 0), GetFloorMaterial(roomType));
        floor.transform.SetParent(room.transform);

        // Create ceiling
        GameObject ceiling = CreateRoomSurface("Ceiling", new Vector3(dimensions.x, 0.1f, dimensions.z),
            new Vector3(0, dimensions.y + 0.05f, 0), GetCeilingMaterial(roomType));
        ceiling.transform.SetParent(room.transform);

        // Create walls
        CreateWalls(room.transform, dimensions, roomType);

        // Add room lighting
        AddRoomLighting(room.transform, dimensions, config);

        return room;
    }

    private GameObject CreateRoomSurface(string name, Vector3 scale, Vector3 position, Material material)
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = name;
        surface.transform.localScale = scale;
        surface.transform.localPosition = position;
        surface.GetComponent<MeshRenderer>().material = material;

        return surface;
    }

    private void CreateWalls(Transform parent, Vector3 dimensions, RoomType roomType)
    {
        Material wallMaterial = GetWallMaterial(roomType);
        float wallThickness = 0.15f;

        // Back wall
        GameObject backWall = CreateRoomSurface("BackWall",
            new Vector3(dimensions.x, dimensions.y, wallThickness),
            new Vector3(0, dimensions.y / 2, -dimensions.z / 2), wallMaterial);
        backWall.transform.SetParent(parent);

        // Front wall (with door opening)
        GameObject frontWallLeft = CreateRoomSurface("FrontWallLeft",
            new Vector3(dimensions.x * 0.35f, dimensions.y, wallThickness),
            new Vector3(-dimensions.x * 0.325f, dimensions.y / 2, dimensions.z / 2), wallMaterial);
        frontWallLeft.transform.SetParent(parent);

        GameObject frontWallRight = CreateRoomSurface("FrontWallRight",
            new Vector3(dimensions.x * 0.35f, dimensions.y, wallThickness),
            new Vector3(dimensions.x * 0.325f, dimensions.y / 2, dimensions.z / 2), wallMaterial);
        frontWallRight.transform.SetParent(parent);

        GameObject frontWallTop = CreateRoomSurface("FrontWallTop",
            new Vector3(dimensions.x * 0.3f, dimensions.y * 0.3f, wallThickness),
            new Vector3(0, dimensions.y * 0.85f, dimensions.z / 2), wallMaterial);
        frontWallTop.transform.SetParent(parent);

        // Left wall
        GameObject leftWall = CreateRoomSurface("LeftWall",
            new Vector3(wallThickness, dimensions.y, dimensions.z),
            new Vector3(-dimensions.x / 2, dimensions.y / 2, 0), wallMaterial);
        leftWall.transform.SetParent(parent);

        // Right wall
        GameObject rightWall = CreateRoomSurface("RightWall",
            new Vector3(wallThickness, dimensions.y, dimensions.z),
            new Vector3(dimensions.x / 2, dimensions.y / 2, 0), wallMaterial);
        rightWall.transform.SetParent(parent);
    }

    private void AddRoomLighting(Transform parent, Vector3 dimensions, RoomConfiguration config)
    {
        // Main ceiling lights
        int numLightsX = Mathf.CeilToInt(dimensions.x / 3f);
        int numLightsZ = Mathf.CeilToInt(dimensions.z / 3f);

        float intensity = config?.LightIntensity ?? 1f;
        Color lightColor = config?.LightColor ?? Color.white;

        for (int x = 0; x < numLightsX; x++)
        {
            for (int z = 0; z < numLightsZ; z++)
            {
                GameObject lightObj = new GameObject($"CeilingLight_{x}_{z}");
                lightObj.transform.SetParent(parent);

                float posX = -dimensions.x / 2 + (x + 0.5f) * (dimensions.x / numLightsX);
                float posZ = -dimensions.z / 2 + (z + 0.5f) * (dimensions.z / numLightsZ);

                lightObj.transform.localPosition = new Vector3(posX, dimensions.y - 0.1f, posZ);

                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.intensity = intensity;
                light.color = lightColor;
                light.range = 5f;
                light.shadows = LightShadows.Soft;
            }
        }

        // Surgical light (for OR)
        if (config?.RoomType == RoomType.OperatingRoom || config?.RoomType == RoomType.HybridOR)
        {
            GameObject surgicalLight = new GameObject("SurgicalLight");
            surgicalLight.transform.SetParent(parent);
            surgicalLight.transform.localPosition = new Vector3(0, dimensions.y - 0.5f, 0);

            Light mainLight = surgicalLight.AddComponent<Light>();
            mainLight.type = LightType.Spot;
            mainLight.intensity = 3f;
            mainLight.color = Color.white;
            mainLight.range = 5f;
            mainLight.spotAngle = 60f;
            mainLight.shadows = LightShadows.Hard;
            surgicalLight.transform.rotation = Quaternion.Euler(90, 0, 0);
        }
    }

    #endregion

    #region Materials

    private Material CreateRoomMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = new Color(0.9f, 0.9f, 0.9f);
        return mat;
    }

    private Material GetFloorMaterial(RoomType roomType)
    {
        Material mat = CreateRoomMaterial();

        switch (roomType)
        {
            case RoomType.OperatingRoom:
            case RoomType.HybridOR:
                mat.color = new Color(0.6f, 0.7f, 0.65f); // Green-ish OR floor
                break;
            case RoomType.HemodynamicsLab:
                mat.color = new Color(0.5f, 0.55f, 0.6f); // Gray-blue cath lab floor
                break;
            default:
                mat.color = new Color(0.7f, 0.7f, 0.7f);
                break;
        }

        return mat;
    }

    private Material GetCeilingMaterial(RoomType roomType)
    {
        Material mat = CreateRoomMaterial();
        mat.color = new Color(0.95f, 0.95f, 0.95f);
        return mat;
    }

    private Material GetWallMaterial(RoomType roomType)
    {
        Material mat = CreateRoomMaterial();

        switch (roomType)
        {
            case RoomType.OperatingRoom:
                mat.color = new Color(0.7f, 0.8f, 0.75f); // Light green OR walls
                break;
            case RoomType.HemodynamicsLab:
                mat.color = new Color(0.65f, 0.7f, 0.75f); // Blue-gray cath lab walls
                break;
            case RoomType.HybridOR:
                mat.color = new Color(0.75f, 0.75f, 0.8f);
                break;
            default:
                mat.color = new Color(0.85f, 0.85f, 0.85f);
                break;
        }

        return mat;
    }

    #endregion

    #region Equipment Management

    /// <summary>
    /// Place equipment in the room.
    /// </summary>
    public GameObject PlaceEquipment(EquipmentType equipmentType, Vector3 position, Quaternion rotation)
    {
        GameObject prefab = GetEquipmentPrefab(equipmentType);
        GameObject equipment;

        if (prefab != null)
        {
            equipment = Instantiate(prefab, position, rotation, currentRoomInstance?.transform ?? roomParent);
        }
        else
        {
            // Create placeholder equipment
            equipment = CreatePlaceholderEquipment(equipmentType);
            equipment.transform.position = position;
            equipment.transform.rotation = rotation;

            if (currentRoomInstance != null)
            {
                equipment.transform.SetParent(currentRoomInstance.transform);
            }
        }

        placedEquipment.Add(equipment);

        OnEquipmentPlaced?.Invoke(this, new EquipmentPlacedEventArgs
        {
            Equipment = equipment,
            Type = equipmentType,
            Position = position
        });

        return equipment;
    }

    /// <summary>
    /// Remove equipment from the room.
    /// </summary>
    public void RemoveEquipment(GameObject equipment)
    {
        if (placedEquipment.Contains(equipment))
        {
            placedEquipment.Remove(equipment);
            Destroy(equipment);
        }
    }

    /// <summary>
    /// Get all placed equipment.
    /// </summary>
    public List<GameObject> GetPlacedEquipment()
    {
        return new List<GameObject>(placedEquipment);
    }

    private GameObject GetEquipmentPrefab(EquipmentType type)
    {
        switch (type)
        {
            case EquipmentType.OperatingTable: return operatingTablePrefab;
            case EquipmentType.SurgicalLights: return surgicalLightsPrefab;
            case EquipmentType.AnesthesiaMachine: return anesthesiaMachinePrefab;
            case EquipmentType.PatientMonitor: return patientMonitorPrefab;
            case EquipmentType.CArm: return cArmPrefab;
            case EquipmentType.IVUSConsole: return ivusPrefab;
            default: return null;
        }
    }

    private GameObject CreatePlaceholderEquipment(EquipmentType type)
    {
        GameObject equipment = new GameObject(type.ToString());

        // Create visual placeholder based on equipment type
        GameObject visual;
        Vector3 scale;
        Color color;

        switch (type)
        {
            case EquipmentType.OperatingTable:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scale = new Vector3(0.6f, 0.8f, 2f);
                color = new Color(0.3f, 0.3f, 0.35f);
                break;

            case EquipmentType.SurgicalLights:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                scale = new Vector3(1f, 0.1f, 1f);
                color = Color.white;
                break;

            case EquipmentType.AnesthesiaMachine:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scale = new Vector3(0.8f, 1.5f, 0.6f);
                color = new Color(0.7f, 0.7f, 0.75f);
                break;

            case EquipmentType.PatientMonitor:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scale = new Vector3(0.5f, 0.4f, 0.1f);
                color = new Color(0.1f, 0.1f, 0.15f);
                break;

            case EquipmentType.CArm:
                visual = CreateCArmPlaceholder();
                visual.transform.SetParent(equipment.transform);
                return equipment;

            case EquipmentType.HeartLungMachine:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scale = new Vector3(1f, 1.8f, 0.8f);
                color = new Color(0.8f, 0.8f, 0.85f);
                break;

            default:
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                scale = new Vector3(0.5f, 1f, 0.5f);
                color = Color.gray;
                break;
        }

        visual.transform.SetParent(equipment.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = scale;
        visual.GetComponent<MeshRenderer>().material.color = color;

        return equipment;
    }

    private GameObject CreateCArmPlaceholder()
    {
        GameObject cArm = new GameObject("CArm_Visual");

        // Base
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.transform.SetParent(cArm.transform);
        baseObj.transform.localPosition = new Vector3(0, 0.1f, 0);
        baseObj.transform.localScale = new Vector3(0.8f, 0.2f, 0.6f);
        baseObj.GetComponent<MeshRenderer>().material.color = new Color(0.3f, 0.3f, 0.35f);

        // Vertical arm
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(cArm.transform);
        arm.transform.localPosition = new Vector3(-0.3f, 1f, 0);
        arm.transform.localScale = new Vector3(0.15f, 1.8f, 0.15f);
        arm.GetComponent<MeshRenderer>().material.color = Color.white;

        // C-shaped arc
        GameObject arc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arc.transform.SetParent(cArm.transform);
        arc.transform.localPosition = new Vector3(0.3f, 1.5f, 0);
        arc.transform.localRotation = Quaternion.Euler(0, 0, 90);
        arc.transform.localScale = new Vector3(1.2f, 0.05f, 1.2f);
        arc.GetComponent<MeshRenderer>().material.color = Color.white;

        return cArm;
    }

    #endregion

    #region Configuration

    private void ApplyRoomConfiguration(RoomConfiguration config)
    {
        // Apply lighting settings
        RenderSettings.ambientLight = config.AmbientColor;

        // Place default equipment
        foreach (EquipmentPlacement placement in config.DefaultEquipment)
        {
            PlaceEquipment(placement.Type, placement.Position, placement.Rotation);
        }
    }

    private GameObject GetRoomPrefab(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.OperatingRoom: return operatingRoomPrefab;
            case RoomType.HemodynamicsLab: return hemodynamicsLabPrefab;
            case RoomType.HybridOR: return hybridORPrefab;
            case RoomType.ICU: return icuPrefab;
            case RoomType.ExaminationRoom: return examinationRoomPrefab;
            case RoomType.ConferenceRoom: return conferenceRoomPrefab;
            default: return null;
        }
    }

    private string GetRoomDisplayName(RoomType roomType)
    {
        switch (roomType)
        {
            case RoomType.OperatingRoom: return "Sala de Bloco Operatório";
            case RoomType.HemodynamicsLab: return "Laboratório de Hemodinâmica";
            case RoomType.HybridOR: return "Sala Híbrida";
            case RoomType.ICU: return "Unidade de Cuidados Intensivos";
            case RoomType.ExaminationRoom: return "Sala de Exame";
            case RoomType.ConferenceRoom: return "Sala de Conferência";
            case RoomType.Custom: return "Sala Personalizada";
            default: return "None";
        }
    }

    #endregion

    #region Public Getters

    public RoomType GetCurrentRoomType() => currentRoomType;
    public GameObject GetCurrentRoomInstance() => currentRoomInstance;
    public string GetCurrentRoomName() => GetRoomDisplayName(currentRoomType);

    public RoomConfiguration GetRoomConfiguration(string key)
    {
        return roomConfigurations.ContainsKey(key) ? roomConfigurations[key] : null;
    }

    public List<string> GetAvailableRoomTypes()
    {
        return new List<string>(roomConfigurations.Keys);
    }

    #endregion
}

/// <summary>
/// Configuration for a virtual room.
/// </summary>
[System.Serializable]
public class RoomConfiguration
{
    public VirtualRoomManager.RoomType RoomType;
    public string DisplayName;
    public string Description;
    public Vector3 Dimensions;
    public float LightIntensity;
    public Color LightColor;
    public Color AmbientColor;
    public List<EquipmentPlacement> DefaultEquipment;
}

/// <summary>
/// Represents equipment placement in a room.
/// </summary>
[System.Serializable]
public class EquipmentPlacement
{
    public VirtualRoomManager.EquipmentType Type;
    public Vector3 Position;
    public Quaternion Rotation;
}

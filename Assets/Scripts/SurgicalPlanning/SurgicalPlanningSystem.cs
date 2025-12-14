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
/// VR3S-style Surgical Planning System.
/// Allows placement of valves, medical devices, and intracardiac baffles on 3D heart models.
/// Inspired by Cincinnati Children's VR Surgical Simulation Suite.
/// </summary>
public class SurgicalPlanningSystem : MonoBehaviour
{
    public static SurgicalPlanningSystem Instance { get; private set; }

    public event EventHandler<DevicePlacedEventArgs> OnDevicePlaced;
    public event EventHandler<DeviceRemovedEventArgs> OnDeviceRemoved;
    public event EventHandler<PlanSavedEventArgs> OnPlanSaved;
    public event EventHandler<PlanLoadedEventArgs> OnPlanLoaded;

    public class DevicePlacedEventArgs : EventArgs
    {
        public PlaceableDevice Device;
        public Vector3 Position;
        public Quaternion Rotation;
    }

    public class DeviceRemovedEventArgs : EventArgs
    {
        public PlaceableDevice Device;
    }

    public class PlanSavedEventArgs : EventArgs
    {
        public string PlanName;
        public int DeviceCount;
    }

    public class PlanLoadedEventArgs : EventArgs
    {
        public string PlanName;
        public SurgicalPlan Plan;
    }

    #region Enums

    public enum DeviceCategory
    {
        Valve,
        Stent,
        Occluder,
        Conduit,
        Patch,
        Baffle,
        Cannula,
        Pacemaker,
        LVAD,
        ECMO,
        Marker,
        Custom
    }

    public enum ValveType
    {
        // Mechanical Valves
        MechanicalMitral,
        MechanicalAortic,
        MechanicalTricuspid,
        MechanicalPulmonary,

        // Bioprosthetic Valves
        BioprostheticMitral,
        BioprostheticAortic,
        BioprostheticTricuspid,
        BioprostheticPulmonary,

        // Transcatheter Valves
        TAVR,           // Transcatheter Aortic Valve Replacement
        TMVR,           // Transcatheter Mitral Valve Replacement
        MelodyValve,    // Melody Pulmonary Valve
        SapienValve,    // Edwards SAPIEN

        // Repair Devices
        MitraClip,
        TriClip,

        // Pediatric/Congenital
        Contegra,       // Bovine jugular vein conduit
        Homograft,

        Custom
    }

    public enum ValveSize
    {
        Size15mm,
        Size17mm,
        Size19mm,
        Size21mm,
        Size23mm,
        Size25mm,
        Size27mm,
        Size29mm,
        Size31mm,
        Custom
    }

    public enum StentType
    {
        CoronaryStent,
        AorticStent,
        PulmonaryStent,
        DuctStent,
        CoarctationStent,
        Custom
    }

    public enum OccluderType
    {
        ASD,            // Atrial Septal Defect
        VSD,            // Ventricular Septal Defect
        PDA,            // Patent Ductus Arteriosus
        PFO,            // Patent Foramen Ovale
        LAA,            // Left Atrial Appendage
        Vascular,
        Custom
    }

    public enum PlanningMode
    {
        Viewing,
        Placing,
        Adjusting,
        Measuring,
        Annotating,
        BaffleDesign
    }

    #endregion

    [Header("References")]
    [SerializeField] private Transform heartModel;
    [SerializeField] private Transform deviceContainer;
    [SerializeField] private Camera planningCamera;

    [Header("Device Prefabs")]
    [SerializeField] private GameObject valvePrefab;
    [SerializeField] private GameObject stentPrefab;
    [SerializeField] private GameObject occluderPrefab;
    [SerializeField] private GameObject conduitPrefab;
    [SerializeField] private GameObject patchPrefab;
    [SerializeField] private GameObject bafflePrefab;
    [SerializeField] private GameObject cannulaPrefab;
    [SerializeField] private GameObject markerPrefab;

    [Header("Current State")]
    [SerializeField] private PlanningMode currentMode = PlanningMode.Viewing;
    [SerializeField] private DeviceCategory selectedCategory = DeviceCategory.Valve;
    [SerializeField] private ValveType selectedValveType = ValveType.BioprostheticAortic;
    [SerializeField] private ValveSize selectedValveSize = ValveSize.Size23mm;

    [Header("Placement Settings")]
    [SerializeField] private float placementDistance = 0.5f;
    [SerializeField] private bool snapToSurface = true;
    [SerializeField] private float rotationSpeed = 45f;
    [SerializeField] private float scaleSpeed = 0.1f;
    [SerializeField] private LayerMask heartLayerMask;

    [Header("Visualization")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;
    [SerializeField] private Color validColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Color invalidColor = new Color(1, 0, 0, 0.5f);

    // Placed devices
    private List<PlaceableDevice> placedDevices = new List<PlaceableDevice>();
    private PlaceableDevice currentGhostDevice;
    private PlaceableDevice selectedDevice;

    // Undo/Redo
    private Stack<PlanningAction> undoStack = new Stack<PlanningAction>();
    private Stack<PlanningAction> redoStack = new Stack<PlanningAction>();

    // Device library
    private Dictionary<ValveType, ValveSpecification> valveSpecs;
    private Dictionary<StentType, StentSpecification> stentSpecs;
    private Dictionary<OccluderType, OccluderSpecification> occluderSpecs;

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

        InitializeDeviceLibrary();
    }

    void Start()
    {
        if (deviceContainer == null)
        {
            deviceContainer = new GameObject("PlacedDevices").transform;
            deviceContainer.SetParent(transform);
        }
    }

    void Update()
    {
        if (currentMode == PlanningMode.Placing && currentGhostDevice != null)
        {
            UpdateGhostPosition();
        }
    }

    #region Device Library Initialization

    private void InitializeDeviceLibrary()
    {
        // Initialize valve specifications
        valveSpecs = new Dictionary<ValveType, ValveSpecification>
        {
            { ValveType.MechanicalAortic, new ValveSpecification
                {
                    Name = "Mechanical Aortic Valve",
                    Description = "Mechanical prosthetic valve for aortic position",
                    AvailableSizes = new[] { 19, 21, 23, 25, 27 },
                    DefaultSize = 23,
                    Color = new Color(0.6f, 0.6f, 0.7f)
                }
            },
            { ValveType.BioprostheticAortic, new ValveSpecification
                {
                    Name = "Bioprosthetic Aortic Valve",
                    Description = "Tissue valve for aortic position",
                    AvailableSizes = new[] { 19, 21, 23, 25, 27, 29 },
                    DefaultSize = 23,
                    Color = new Color(0.9f, 0.85f, 0.75f)
                }
            },
            { ValveType.TAVR, new ValveSpecification
                {
                    Name = "TAVR (Transcatheter Aortic)",
                    Description = "Transcatheter aortic valve replacement",
                    AvailableSizes = new[] { 23, 26, 29 },
                    DefaultSize = 26,
                    Color = new Color(0.7f, 0.75f, 0.8f)
                }
            },
            { ValveType.MelodyValve, new ValveSpecification
                {
                    Name = "Melody Valve",
                    Description = "Transcatheter pulmonary valve for RVOT",
                    AvailableSizes = new[] { 18, 20, 22 },
                    DefaultSize = 22,
                    Color = new Color(0.8f, 0.7f, 0.75f)
                }
            },
            { ValveType.MitraClip, new ValveSpecification
                {
                    Name = "MitraClip",
                    Description = "Percutaneous mitral valve repair device",
                    AvailableSizes = new[] { 0 }, // One size
                    DefaultSize = 0,
                    Color = new Color(0.5f, 0.5f, 0.6f)
                }
            },
            { ValveType.Contegra, new ValveSpecification
                {
                    Name = "Contegra Conduit",
                    Description = "Bovine jugular vein valved conduit",
                    AvailableSizes = new[] { 12, 14, 16, 18, 20, 22 },
                    DefaultSize = 16,
                    Color = new Color(0.85f, 0.8f, 0.7f)
                }
            },
            { ValveType.Homograft, new ValveSpecification
                {
                    Name = "Homograft",
                    Description = "Human donor valve/conduit",
                    AvailableSizes = new[] { 14, 16, 18, 20, 22, 24, 26 },
                    DefaultSize = 20,
                    Color = new Color(0.9f, 0.8f, 0.75f)
                }
            }
        };

        // Initialize stent specifications
        stentSpecs = new Dictionary<StentType, StentSpecification>
        {
            { StentType.PulmonaryStent, new StentSpecification
                {
                    Name = "Pulmonary Artery Stent",
                    Description = "Stent for pulmonary artery stenosis",
                    DiameterRange = new Vector2(8, 25),
                    LengthRange = new Vector2(15, 45),
                    Color = new Color(0.7f, 0.75f, 0.8f)
                }
            },
            { StentType.CoarctationStent, new StentSpecification
                {
                    Name = "Coarctation Stent",
                    Description = "Covered stent for aortic coarctation",
                    DiameterRange = new Vector2(10, 28),
                    LengthRange = new Vector2(20, 55),
                    Color = new Color(0.65f, 0.7f, 0.75f)
                }
            },
            { StentType.DuctStent, new StentSpecification
                {
                    Name = "Duct Stent",
                    Description = "Stent for patent ductus arteriosus",
                    DiameterRange = new Vector2(3, 8),
                    LengthRange = new Vector2(10, 20),
                    Color = new Color(0.6f, 0.65f, 0.7f)
                }
            }
        };

        // Initialize occluder specifications
        occluderSpecs = new Dictionary<OccluderType, OccluderSpecification>
        {
            { OccluderType.ASD, new OccluderSpecification
                {
                    Name = "ASD Occluder",
                    Description = "Atrial septal defect closure device",
                    SizeRange = new Vector2(4, 40),
                    Color = new Color(0.6f, 0.65f, 0.7f)
                }
            },
            { OccluderType.VSD, new OccluderSpecification
                {
                    Name = "VSD Occluder",
                    Description = "Ventricular septal defect closure device",
                    SizeRange = new Vector2(4, 18),
                    Color = new Color(0.65f, 0.6f, 0.7f)
                }
            },
            { OccluderType.PDA, new OccluderSpecification
                {
                    Name = "PDA Occluder",
                    Description = "Patent ductus arteriosus closure device",
                    SizeRange = new Vector2(3, 16),
                    Color = new Color(0.6f, 0.6f, 0.65f)
                }
            },
            { OccluderType.LAA, new OccluderSpecification
                {
                    Name = "LAA Occluder",
                    Description = "Left atrial appendage closure device",
                    SizeRange = new Vector2(21, 35),
                    Color = new Color(0.55f, 0.6f, 0.65f)
                }
            }
        };
    }

    #endregion

    #region Planning Mode Control

    /// <summary>
    /// Set the current planning mode.
    /// </summary>
    public void SetPlanningMode(PlanningMode mode)
    {
        // Exit current mode
        if (currentMode == PlanningMode.Placing && currentGhostDevice != null)
        {
            DestroyGhostDevice();
        }

        currentMode = mode;

        if (mode == PlanningMode.Placing)
        {
            CreateGhostDevice();
        }

        Debug.Log($"Planning mode set to: {mode}");
    }

    /// <summary>
    /// Get current planning mode.
    /// </summary>
    public PlanningMode GetCurrentMode()
    {
        return currentMode;
    }

    #endregion

    #region Device Selection

    /// <summary>
    /// Select device category to place.
    /// </summary>
    public void SelectDeviceCategory(DeviceCategory category)
    {
        selectedCategory = category;

        if (currentMode == PlanningMode.Placing)
        {
            DestroyGhostDevice();
            CreateGhostDevice();
        }
    }

    /// <summary>
    /// Select valve type to place.
    /// </summary>
    public void SelectValveType(ValveType type)
    {
        selectedValveType = type;
        selectedCategory = DeviceCategory.Valve;

        if (currentMode == PlanningMode.Placing)
        {
            DestroyGhostDevice();
            CreateGhostDevice();
        }
    }

    /// <summary>
    /// Select valve size.
    /// </summary>
    public void SelectValveSize(ValveSize size)
    {
        selectedValveSize = size;

        if (currentMode == PlanningMode.Placing && currentGhostDevice != null)
        {
            UpdateGhostDeviceSize();
        }
    }

    /// <summary>
    /// Get valve size in millimeters.
    /// </summary>
    public int GetValveSizeInMM(ValveSize size)
    {
        switch (size)
        {
            case ValveSize.Size15mm: return 15;
            case ValveSize.Size17mm: return 17;
            case ValveSize.Size19mm: return 19;
            case ValveSize.Size21mm: return 21;
            case ValveSize.Size23mm: return 23;
            case ValveSize.Size25mm: return 25;
            case ValveSize.Size27mm: return 27;
            case ValveSize.Size29mm: return 29;
            case ValveSize.Size31mm: return 31;
            default: return 23;
        }
    }

    #endregion

    #region Ghost Device (Preview)

    private void CreateGhostDevice()
    {
        GameObject prefab = GetPrefabForCategory(selectedCategory);
        if (prefab == null)
        {
            // Create default ghost
            prefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(prefab.GetComponent<Collider>());
        }

        GameObject ghostObj = Instantiate(prefab, deviceContainer);
        ghostObj.name = "GhostDevice";

        currentGhostDevice = ghostObj.GetComponent<PlaceableDevice>();
        if (currentGhostDevice == null)
        {
            currentGhostDevice = ghostObj.AddComponent<PlaceableDevice>();
        }

        currentGhostDevice.SetAsGhost(true);
        currentGhostDevice.Category = selectedCategory;

        if (selectedCategory == DeviceCategory.Valve)
        {
            currentGhostDevice.ValveType = selectedValveType;
            currentGhostDevice.Size = GetValveSizeInMM(selectedValveSize);
        }

        UpdateGhostDeviceSize();
        ApplyGhostMaterial(ghostObj);
    }

    private void DestroyGhostDevice()
    {
        if (currentGhostDevice != null)
        {
            Destroy(currentGhostDevice.gameObject);
            currentGhostDevice = null;
        }
    }

    private void UpdateGhostPosition()
    {
        if (planningCamera == null)
        {
            planningCamera = Camera.main;
        }

        Ray ray = new Ray(planningCamera.transform.position, planningCamera.transform.forward);
        RaycastHit hit;

        if (snapToSurface && Physics.Raycast(ray, out hit, 10f, heartLayerMask))
        {
            currentGhostDevice.transform.position = hit.point;
            currentGhostDevice.transform.rotation = Quaternion.LookRotation(hit.normal);
            SetGhostValidity(true);
        }
        else
        {
            currentGhostDevice.transform.position = ray.origin + ray.direction * placementDistance;
            SetGhostValidity(false);
        }
    }

    private void UpdateGhostDeviceSize()
    {
        if (currentGhostDevice == null) return;

        float sizeMM = GetValveSizeInMM(selectedValveSize);
        float sizeMeters = sizeMM / 1000f; // Convert to meters

        currentGhostDevice.transform.localScale = Vector3.one * sizeMeters * 10f; // Scale for visibility
    }

    private void ApplyGhostMaterial(GameObject obj)
    {
        if (ghostMaterial == null) return;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.material = ghostMaterial;
        }
    }

    private void SetGhostValidity(bool valid)
    {
        if (currentGhostDevice == null) return;

        Material mat = valid ? validPlacementMaterial : invalidPlacementMaterial;
        Color color = valid ? validColor : invalidColor;

        if (mat != null)
        {
            Renderer[] renderers = currentGhostDevice.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = mat;
                renderer.material.color = color;
            }
        }

        currentGhostDevice.IsValidPlacement = valid;
    }

    private GameObject GetPrefabForCategory(DeviceCategory category)
    {
        switch (category)
        {
            case DeviceCategory.Valve: return valvePrefab;
            case DeviceCategory.Stent: return stentPrefab;
            case DeviceCategory.Occluder: return occluderPrefab;
            case DeviceCategory.Conduit: return conduitPrefab;
            case DeviceCategory.Patch: return patchPrefab;
            case DeviceCategory.Baffle: return bafflePrefab;
            case DeviceCategory.Cannula: return cannulaPrefab;
            case DeviceCategory.Marker: return markerPrefab;
            default: return null;
        }
    }

    #endregion

    #region Device Placement

    /// <summary>
    /// Confirm placement of current ghost device.
    /// </summary>
    public PlaceableDevice ConfirmPlacement()
    {
        if (currentGhostDevice == null || !currentGhostDevice.IsValidPlacement)
        {
            Debug.LogWarning("Cannot place device - invalid position");
            return null;
        }

        // Convert ghost to placed device
        currentGhostDevice.SetAsGhost(false);
        PlaceableDevice placedDevice = currentGhostDevice;

        // Apply proper material
        ApplyDeviceMaterial(placedDevice);

        // Register device
        placedDevices.Add(placedDevice);

        // Record for undo
        undoStack.Push(new PlanningAction
        {
            Type = ActionType.Place,
            Device = placedDevice,
            Position = placedDevice.transform.position,
            Rotation = placedDevice.transform.rotation
        });
        redoStack.Clear();

        // Notify
        OnDevicePlaced?.Invoke(this, new DevicePlacedEventArgs
        {
            Device = placedDevice,
            Position = placedDevice.transform.position,
            Rotation = placedDevice.transform.rotation
        });

        Debug.Log($"Placed {placedDevice.Category}: {placedDevice.GetDisplayName()}");

        // Create new ghost for continuous placement
        currentGhostDevice = null;
        CreateGhostDevice();

        return placedDevice;
    }

    /// <summary>
    /// Cancel current placement.
    /// </summary>
    public void CancelPlacement()
    {
        DestroyGhostDevice();
        SetPlanningMode(PlanningMode.Viewing);
    }

    /// <summary>
    /// Remove a placed device.
    /// </summary>
    public void RemoveDevice(PlaceableDevice device)
    {
        if (device == null || !placedDevices.Contains(device)) return;

        // Record for undo
        undoStack.Push(new PlanningAction
        {
            Type = ActionType.Remove,
            Device = device,
            Position = device.transform.position,
            Rotation = device.transform.rotation
        });
        redoStack.Clear();

        placedDevices.Remove(device);

        OnDeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs { Device = device });

        // Don't destroy - just deactivate for undo
        device.gameObject.SetActive(false);
    }

    /// <summary>
    /// Select a device for adjustment.
    /// </summary>
    public void SelectDevice(PlaceableDevice device)
    {
        if (selectedDevice != null)
        {
            DeselectDevice();
        }

        selectedDevice = device;
        device.SetSelected(true);

        if (selectedMaterial != null)
        {
            device.SetHighlightMaterial(selectedMaterial);
        }

        SetPlanningMode(PlanningMode.Adjusting);
    }

    /// <summary>
    /// Deselect current device.
    /// </summary>
    public void DeselectDevice()
    {
        if (selectedDevice != null)
        {
            selectedDevice.SetSelected(false);
            selectedDevice.RestoreOriginalMaterial();
            selectedDevice = null;
        }
    }

    private void ApplyDeviceMaterial(PlaceableDevice device)
    {
        // Apply material based on device type
        Color deviceColor = GetDeviceColor(device);

        Renderer[] renderers = device.GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = deviceColor;
            renderer.material = mat;
        }

        device.SaveOriginalMaterial();
    }

    private Color GetDeviceColor(PlaceableDevice device)
    {
        if (device.Category == DeviceCategory.Valve && valveSpecs.ContainsKey(device.ValveType))
        {
            return valveSpecs[device.ValveType].Color;
        }

        // Default colors
        switch (device.Category)
        {
            case DeviceCategory.Valve: return new Color(0.8f, 0.75f, 0.7f);
            case DeviceCategory.Stent: return new Color(0.7f, 0.75f, 0.8f);
            case DeviceCategory.Occluder: return new Color(0.6f, 0.65f, 0.7f);
            case DeviceCategory.Baffle: return new Color(0.9f, 0.85f, 0.8f);
            case DeviceCategory.Patch: return new Color(0.85f, 0.8f, 0.75f);
            default: return Color.gray;
        }
    }

    #endregion

    #region Device Adjustment

    /// <summary>
    /// Rotate selected device.
    /// </summary>
    public void RotateSelectedDevice(Vector3 axis, float amount)
    {
        if (selectedDevice == null) return;

        selectedDevice.transform.Rotate(axis, amount * rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Scale selected device.
    /// </summary>
    public void ScaleSelectedDevice(float amount)
    {
        if (selectedDevice == null) return;

        Vector3 newScale = selectedDevice.transform.localScale + Vector3.one * amount * scaleSpeed * Time.deltaTime;
        newScale = Vector3.Max(newScale, Vector3.one * 0.001f); // Minimum scale
        selectedDevice.transform.localScale = newScale;
    }

    /// <summary>
    /// Move selected device.
    /// </summary>
    public void MoveSelectedDevice(Vector3 direction)
    {
        if (selectedDevice == null) return;

        selectedDevice.transform.position += direction * Time.deltaTime;
    }

    #endregion

    #region Undo/Redo

    /// <summary>
    /// Undo last action.
    /// </summary>
    public void Undo()
    {
        if (undoStack.Count == 0) return;

        PlanningAction action = undoStack.Pop();

        switch (action.Type)
        {
            case ActionType.Place:
                // Remove the placed device
                action.Device.gameObject.SetActive(false);
                placedDevices.Remove(action.Device);
                break;

            case ActionType.Remove:
                // Restore the removed device
                action.Device.gameObject.SetActive(true);
                placedDevices.Add(action.Device);
                break;

            case ActionType.Move:
                // Restore previous position
                action.Device.transform.position = action.Position;
                action.Device.transform.rotation = action.Rotation;
                break;
        }

        redoStack.Push(action);
        Debug.Log($"Undid action: {action.Type}");
    }

    /// <summary>
    /// Redo last undone action.
    /// </summary>
    public void Redo()
    {
        if (redoStack.Count == 0) return;

        PlanningAction action = redoStack.Pop();

        switch (action.Type)
        {
            case ActionType.Place:
                // Re-place the device
                action.Device.gameObject.SetActive(true);
                placedDevices.Add(action.Device);
                break;

            case ActionType.Remove:
                // Re-remove the device
                action.Device.gameObject.SetActive(false);
                placedDevices.Remove(action.Device);
                break;
        }

        undoStack.Push(action);
        Debug.Log($"Redid action: {action.Type}");
    }

    #endregion

    #region Plan Save/Load

    /// <summary>
    /// Save current surgical plan.
    /// </summary>
    public SurgicalPlan SavePlan(string planName)
    {
        SurgicalPlan plan = new SurgicalPlan
        {
            Name = planName,
            CreatedAt = DateTime.Now,
            Devices = new List<DeviceData>()
        };

        foreach (var device in placedDevices)
        {
            plan.Devices.Add(new DeviceData
            {
                Category = device.Category,
                ValveType = device.ValveType,
                Size = device.Size,
                Position = device.transform.position,
                Rotation = device.transform.rotation,
                Scale = device.transform.localScale
            });
        }

        string json = JsonUtility.ToJson(plan, true);
        PlayerPrefs.SetString($"SurgicalPlan_{planName}", json);
        PlayerPrefs.Save();

        OnPlanSaved?.Invoke(this, new PlanSavedEventArgs
        {
            PlanName = planName,
            DeviceCount = plan.Devices.Count
        });

        Debug.Log($"Saved surgical plan: {planName} with {plan.Devices.Count} devices");

        return plan;
    }

    /// <summary>
    /// Load a surgical plan.
    /// </summary>
    public SurgicalPlan LoadPlan(string planName)
    {
        string key = $"SurgicalPlan_{planName}";
        if (!PlayerPrefs.HasKey(key))
        {
            Debug.LogWarning($"Plan not found: {planName}");
            return null;
        }

        string json = PlayerPrefs.GetString(key);
        SurgicalPlan plan = JsonUtility.FromJson<SurgicalPlan>(json);

        // Clear current devices
        ClearAllDevices();

        // Recreate devices
        foreach (var deviceData in plan.Devices)
        {
            selectedCategory = deviceData.Category;
            selectedValveType = deviceData.ValveType;

            GameObject prefab = GetPrefabForCategory(deviceData.Category);
            if (prefab == null)
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            }

            GameObject obj = Instantiate(prefab, deviceContainer);
            obj.transform.position = deviceData.Position;
            obj.transform.rotation = deviceData.Rotation;
            obj.transform.localScale = deviceData.Scale;

            PlaceableDevice device = obj.GetComponent<PlaceableDevice>();
            if (device == null)
            {
                device = obj.AddComponent<PlaceableDevice>();
            }

            device.Category = deviceData.Category;
            device.ValveType = deviceData.ValveType;
            device.Size = deviceData.Size;

            ApplyDeviceMaterial(device);
            placedDevices.Add(device);
        }

        OnPlanLoaded?.Invoke(this, new PlanLoadedEventArgs
        {
            PlanName = planName,
            Plan = plan
        });

        Debug.Log($"Loaded surgical plan: {planName} with {plan.Devices.Count} devices");

        return plan;
    }

    /// <summary>
    /// Clear all placed devices.
    /// </summary>
    public void ClearAllDevices()
    {
        foreach (var device in placedDevices)
        {
            if (device != null)
            {
                Destroy(device.gameObject);
            }
        }
        placedDevices.Clear();
        undoStack.Clear();
        redoStack.Clear();
    }

    /// <summary>
    /// Get list of saved plans.
    /// </summary>
    public List<string> GetSavedPlans()
    {
        List<string> plans = new List<string>();
        // Note: PlayerPrefs doesn't provide a way to enumerate keys
        // In production, use a proper database or file system
        return plans;
    }

    #endregion

    #region Getters

    /// <summary>
    /// Get all placed devices.
    /// </summary>
    public List<PlaceableDevice> GetPlacedDevices()
    {
        return new List<PlaceableDevice>(placedDevices);
    }

    /// <summary>
    /// Get selected device.
    /// </summary>
    public PlaceableDevice GetSelectedDevice()
    {
        return selectedDevice;
    }

    /// <summary>
    /// Get valve specification.
    /// </summary>
    public ValveSpecification GetValveSpec(ValveType type)
    {
        return valveSpecs.ContainsKey(type) ? valveSpecs[type] : null;
    }

    #endregion
}

#region Data Classes

/// <summary>
/// Represents a placeable surgical device.
/// </summary>
public class PlaceableDevice : MonoBehaviour
{
    public SurgicalPlanningSystem.DeviceCategory Category;
    public SurgicalPlanningSystem.ValveType ValveType;
    public int Size;
    public bool IsGhost;
    public bool IsValidPlacement;

    private bool isSelected;
    private Material[] originalMaterials;

    public void SetAsGhost(bool ghost)
    {
        IsGhost = ghost;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public void SaveOriginalMaterial()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].material;
        }
    }

    public void RestoreOriginalMaterial()
    {
        if (originalMaterials == null) return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        for (int i = 0; i < renderers.Length && i < originalMaterials.Length; i++)
        {
            renderers[i].material = originalMaterials[i];
        }
    }

    public void SetHighlightMaterial(Material mat)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            renderer.material = mat;
        }
    }

    public string GetDisplayName()
    {
        if (Category == SurgicalPlanningSystem.DeviceCategory.Valve)
        {
            return $"{ValveType} ({Size}mm)";
        }
        return $"{Category}";
    }
}

/// <summary>
/// Valve specification data.
/// </summary>
[System.Serializable]
public class ValveSpecification
{
    public string Name;
    public string Description;
    public int[] AvailableSizes;
    public int DefaultSize;
    public Color Color;
}

/// <summary>
/// Stent specification data.
/// </summary>
[System.Serializable]
public class StentSpecification
{
    public string Name;
    public string Description;
    public Vector2 DiameterRange;
    public Vector2 LengthRange;
    public Color Color;
}

/// <summary>
/// Occluder specification data.
/// </summary>
[System.Serializable]
public class OccluderSpecification
{
    public string Name;
    public string Description;
    public Vector2 SizeRange;
    public Color Color;
}

/// <summary>
/// Surgical plan data.
/// </summary>
[System.Serializable]
public class SurgicalPlan
{
    public string Name;
    public DateTime CreatedAt;
    public List<DeviceData> Devices;
}

/// <summary>
/// Device placement data.
/// </summary>
[System.Serializable]
public class DeviceData
{
    public SurgicalPlanningSystem.DeviceCategory Category;
    public SurgicalPlanningSystem.ValveType ValveType;
    public int Size;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}

/// <summary>
/// Planning action for undo/redo.
/// </summary>
public class PlanningAction
{
    public ActionType Type;
    public PlaceableDevice Device;
    public Vector3 Position;
    public Quaternion Rotation;
}

public enum ActionType
{
    Place,
    Remove,
    Move,
    Rotate,
    Scale
}

#endregion

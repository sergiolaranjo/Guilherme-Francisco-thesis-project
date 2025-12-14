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
/// Central manager for cardiac surgery simulation.
/// Coordinates all surgical tools and procedures.
/// </summary>
public class SurgerySimulator : MonoBehaviour
{
    public static SurgerySimulator Instance { get; private set; }

    // Events
    public event EventHandler<SurgicalActionEventArgs> OnSurgicalAction;
    public event EventHandler<ToolChangedEventArgs> OnToolChanged;
    public event EventHandler OnSurgeryStarted;
    public event EventHandler OnSurgeryEnded;
    public event EventHandler<UndoEventArgs> OnActionUndone;

    public class SurgicalActionEventArgs : EventArgs
    {
        public SurgicalActionType ActionType;
        public Vector3 Position;
        public string Description;
        public float Timestamp;
    }

    public class ToolChangedEventArgs : EventArgs
    {
        public SurgicalTool PreviousTool;
        public SurgicalTool CurrentTool;
    }

    public class UndoEventArgs : EventArgs
    {
        public SurgicalAction UndoneAction;
        public int RemainingActions;
    }

    /// <summary>
    /// Types of surgical actions that can be performed.
    /// </summary>
    public enum SurgicalActionType
    {
        None,
        Incision,
        Suture,
        Ablation,
        Clamp,
        Cannulation,
        Patch,
        Excision,
        Anastomosis,
        Measurement,
        Marker
    }

    /// <summary>
    /// Available surgical tools.
    /// </summary>
    public enum SurgicalTool
    {
        None,
        Scalpel,
        Scissors,
        Forceps,
        NeedleHolder,
        SutureNeedle,
        AblationCatheter,
        VascularClamp,
        Cannula,
        Retractor,
        Electrocautery,
        Marker
    }

    [Header("Current State")]
    [SerializeField] private SurgicalTool currentTool = SurgicalTool.None;
    [SerializeField] private bool isSurgeryActive = false;

    [Header("Tool References")]
    [SerializeField] private GameObject scalpelPrefab;
    [SerializeField] private GameObject scissorsPrefab;
    [SerializeField] private GameObject forcepsPrefab;
    [SerializeField] private GameObject needleHolderPrefab;
    [SerializeField] private GameObject ablationCatheterPrefab;
    [SerializeField] private GameObject clampPrefab;
    [SerializeField] private GameObject cannulaPrefab;
    [SerializeField] private GameObject electrocauteryPrefab;

    [Header("Visual Feedback")]
    [SerializeField] private Material incisionMaterial;
    [SerializeField] private Material sutureMaterial;
    [SerializeField] private Material ablationMaterial;
    [SerializeField] private Material clampMaterial;

    [Header("Tool Settings")]
    [SerializeField] private float incisionWidth = 0.002f;
    [SerializeField] private float sutureSpacing = 0.005f;
    [SerializeField] private float ablationRadius = 0.003f;

    [Header("Controller References")]
    [SerializeField] private Transform rightController;
    [SerializeField] private Transform leftController;

    // Action history for undo
    private Stack<SurgicalAction> actionHistory = new Stack<SurgicalAction>();
    private const int MaxHistorySize = 100;

    // Active surgical elements
    private List<GameObject> activeIncisions = new List<GameObject>();
    private List<GameObject> activeSutures = new List<GameObject>();
    private List<GameObject> activeAblations = new List<GameObject>();
    private List<GameObject> activeClamps = new List<GameObject>();
    private List<GameObject> activeCannulas = new List<GameObject>();
    private List<GameObject> activeMarkers = new List<GameObject>();

    // Current tool instance
    private GameObject currentToolInstance;

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
        InitializeMaterials();
    }

    private void InitializeMaterials()
    {
        if (incisionMaterial == null)
        {
            incisionMaterial = CreateDefaultMaterial(new Color(0.8f, 0.1f, 0.1f));
        }
        if (sutureMaterial == null)
        {
            sutureMaterial = CreateDefaultMaterial(new Color(0.2f, 0.2f, 0.8f));
        }
        if (ablationMaterial == null)
        {
            ablationMaterial = CreateDefaultMaterial(new Color(0.9f, 0.5f, 0.1f));
        }
        if (clampMaterial == null)
        {
            clampMaterial = CreateDefaultMaterial(new Color(0.5f, 0.5f, 0.5f));
        }
    }

    private Material CreateDefaultMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    #region Surgery Session Management

    /// <summary>
    /// Start a new surgery session.
    /// </summary>
    public void StartSurgery()
    {
        if (isSurgeryActive)
        {
            Debug.LogWarning("Surgery session already active.");
            return;
        }

        isSurgeryActive = true;
        actionHistory.Clear();
        OnSurgeryStarted?.Invoke(this, EventArgs.Empty);
        Debug.Log("Surgery session started.");
    }

    /// <summary>
    /// End the current surgery session.
    /// </summary>
    public void EndSurgery()
    {
        if (!isSurgeryActive)
        {
            Debug.LogWarning("No active surgery session.");
            return;
        }

        isSurgeryActive = false;
        SetTool(SurgicalTool.None);
        OnSurgeryEnded?.Invoke(this, EventArgs.Empty);
        Debug.Log($"Surgery session ended. Total actions: {actionHistory.Count}");
    }

    /// <summary>
    /// Reset the surgery simulation, removing all surgical elements.
    /// </summary>
    public void ResetSurgery()
    {
        // Clear all surgical elements
        ClearList(activeIncisions);
        ClearList(activeSutures);
        ClearList(activeAblations);
        ClearList(activeClamps);
        ClearList(activeCannulas);
        ClearList(activeMarkers);

        actionHistory.Clear();
        Debug.Log("Surgery simulation reset.");
    }

    private void ClearList(List<GameObject> list)
    {
        foreach (GameObject obj in list)
        {
            if (obj != null) Destroy(obj);
        }
        list.Clear();
    }

    #endregion

    #region Tool Management

    /// <summary>
    /// Set the current surgical tool.
    /// </summary>
    public void SetTool(SurgicalTool tool)
    {
        SurgicalTool previousTool = currentTool;
        currentTool = tool;

        // Destroy previous tool instance
        if (currentToolInstance != null)
        {
            Destroy(currentToolInstance);
            currentToolInstance = null;
        }

        // Spawn new tool instance
        GameObject toolPrefab = GetToolPrefab(tool);
        if (toolPrefab != null && rightController != null)
        {
            currentToolInstance = Instantiate(toolPrefab, rightController);
            currentToolInstance.transform.localPosition = Vector3.zero;
            currentToolInstance.transform.localRotation = Quaternion.identity;
        }

        OnToolChanged?.Invoke(this, new ToolChangedEventArgs
        {
            PreviousTool = previousTool,
            CurrentTool = tool
        });

        Debug.Log($"Tool changed: {previousTool} -> {tool}");
    }

    private GameObject GetToolPrefab(SurgicalTool tool)
    {
        switch (tool)
        {
            case SurgicalTool.Scalpel: return scalpelPrefab;
            case SurgicalTool.Scissors: return scissorsPrefab;
            case SurgicalTool.Forceps: return forcepsPrefab;
            case SurgicalTool.NeedleHolder: return needleHolderPrefab;
            case SurgicalTool.AblationCatheter: return ablationCatheterPrefab;
            case SurgicalTool.VascularClamp: return clampPrefab;
            case SurgicalTool.Cannula: return cannulaPrefab;
            case SurgicalTool.Electrocautery: return electrocauteryPrefab;
            default: return null;
        }
    }

    /// <summary>
    /// Get the current surgical tool.
    /// </summary>
    public SurgicalTool GetCurrentTool()
    {
        return currentTool;
    }

    #endregion

    #region Surgical Actions

    /// <summary>
    /// Perform an incision at the specified position and direction.
    /// </summary>
    public GameObject PerformIncision(Vector3 startPosition, Vector3 endPosition, Vector3 normal)
    {
        if (!isSurgeryActive)
        {
            Debug.LogWarning("No active surgery session.");
            return null;
        }

        // Create incision visualization
        GameObject incision = CreateIncisionVisual(startPosition, endPosition, normal);
        activeIncisions.Add(incision);

        // Record action
        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Incision,
            Position = (startPosition + endPosition) / 2,
            Direction = (endPosition - startPosition).normalized,
            CreatedObject = incision,
            Timestamp = Time.time
        });

        OnSurgicalAction?.Invoke(this, new SurgicalActionEventArgs
        {
            ActionType = SurgicalActionType.Incision,
            Position = startPosition,
            Description = $"Incision: {Vector3.Distance(startPosition, endPosition) * 1000:F1}mm",
            Timestamp = Time.time
        });

        return incision;
    }

    private GameObject CreateIncisionVisual(Vector3 start, Vector3 end, Vector3 normal)
    {
        GameObject incision = new GameObject("Incision");

        // Create a line renderer for the incision
        LineRenderer lineRenderer = incision.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = incisionWidth;
        lineRenderer.endWidth = incisionWidth;
        lineRenderer.material = incisionMaterial;

        return incision;
    }

    /// <summary>
    /// Place a suture at the specified position.
    /// </summary>
    public GameObject PlaceSuture(Vector3 position, Vector3 direction, float length = 0.01f)
    {
        if (!isSurgeryActive)
        {
            Debug.LogWarning("No active surgery session.");
            return null;
        }

        GameObject suture = CreateSutureVisual(position, direction, length);
        activeSutures.Add(suture);

        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Suture,
            Position = position,
            Direction = direction,
            CreatedObject = suture,
            Timestamp = Time.time
        });

        OnSurgicalAction?.Invoke(this, new SurgicalActionEventArgs
        {
            ActionType = SurgicalActionType.Suture,
            Position = position,
            Description = "Suture placed",
            Timestamp = Time.time
        });

        return suture;
    }

    private GameObject CreateSutureVisual(Vector3 position, Vector3 direction, float length)
    {
        GameObject suture = new GameObject("Suture");
        suture.transform.position = position;
        suture.transform.forward = direction;

        // Create suture thread visual
        LineRenderer lineRenderer = suture.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, position - direction * length / 2);
        lineRenderer.SetPosition(1, position + direction * length / 2);
        lineRenderer.startWidth = 0.0005f;
        lineRenderer.endWidth = 0.0005f;
        lineRenderer.material = sutureMaterial;

        // Add small spheres at ends to represent knots
        GameObject knot1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knot1.transform.SetParent(suture.transform);
        knot1.transform.position = position - direction * length / 2;
        knot1.transform.localScale = Vector3.one * 0.001f;
        knot1.GetComponent<MeshRenderer>().material = sutureMaterial;
        Destroy(knot1.GetComponent<Collider>());

        GameObject knot2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knot2.transform.SetParent(suture.transform);
        knot2.transform.position = position + direction * length / 2;
        knot2.transform.localScale = Vector3.one * 0.001f;
        knot2.GetComponent<MeshRenderer>().material = sutureMaterial;
        Destroy(knot2.GetComponent<Collider>());

        return suture;
    }

    /// <summary>
    /// Perform ablation at the specified position.
    /// </summary>
    public GameObject PerformAblation(Vector3 position, float radius = -1)
    {
        if (!isSurgeryActive)
        {
            Debug.LogWarning("No active surgery session.");
            return null;
        }

        if (radius < 0) radius = ablationRadius;

        GameObject ablation = CreateAblationVisual(position, radius);
        activeAblations.Add(ablation);

        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Ablation,
            Position = position,
            Radius = radius,
            CreatedObject = ablation,
            Timestamp = Time.time
        });

        OnSurgicalAction?.Invoke(this, new SurgicalActionEventArgs
        {
            ActionType = SurgicalActionType.Ablation,
            Position = position,
            Description = $"Ablation: {radius * 1000:F1}mm radius",
            Timestamp = Time.time
        });

        return ablation;
    }

    private GameObject CreateAblationVisual(Vector3 position, float radius)
    {
        GameObject ablation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ablation.name = "AblationSite";
        ablation.transform.position = position;
        ablation.transform.localScale = Vector3.one * radius * 2;
        ablation.GetComponent<MeshRenderer>().material = ablationMaterial;
        Destroy(ablation.GetComponent<Collider>());

        // Add particle effect for visual feedback
        ParticleSystem ps = ablation.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startSize = radius * 0.5f;
        main.startLifetime = 0.5f;
        main.startSpeed = radius * 2;
        main.startColor = new Color(1f, 0.5f, 0.2f, 0.5f);
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 10;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        return ablation;
    }

    /// <summary>
    /// Apply a vascular clamp at the specified position.
    /// </summary>
    public GameObject ApplyClamp(Vector3 position, Quaternion rotation)
    {
        if (!isSurgeryActive)
        {
            Debug.LogWarning("No active surgery session.");
            return null;
        }

        GameObject clamp = CreateClampVisual(position, rotation);
        activeClamps.Add(clamp);

        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Clamp,
            Position = position,
            Rotation = rotation,
            CreatedObject = clamp,
            Timestamp = Time.time
        });

        OnSurgicalAction?.Invoke(this, new SurgicalActionEventArgs
        {
            ActionType = SurgicalActionType.Clamp,
            Position = position,
            Description = "Vascular clamp applied",
            Timestamp = Time.time
        });

        return clamp;
    }

    private GameObject CreateClampVisual(Vector3 position, Quaternion rotation)
    {
        GameObject clamp = new GameObject("VascularClamp");
        clamp.transform.position = position;
        clamp.transform.rotation = rotation;

        // Create simple clamp geometry
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.transform.SetParent(clamp.transform);
        bar.transform.localPosition = Vector3.zero;
        bar.transform.localScale = new Vector3(0.02f, 0.002f, 0.005f);
        bar.GetComponent<MeshRenderer>().material = clampMaterial;
        Destroy(bar.GetComponent<Collider>());

        // Add jaws
        GameObject jaw1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jaw1.transform.SetParent(clamp.transform);
        jaw1.transform.localPosition = new Vector3(-0.01f, -0.002f, 0);
        jaw1.transform.localScale = new Vector3(0.002f, 0.005f, 0.005f);
        jaw1.GetComponent<MeshRenderer>().material = clampMaterial;
        Destroy(jaw1.GetComponent<Collider>());

        GameObject jaw2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jaw2.transform.SetParent(clamp.transform);
        jaw2.transform.localPosition = new Vector3(0.01f, -0.002f, 0);
        jaw2.transform.localScale = new Vector3(0.002f, 0.005f, 0.005f);
        jaw2.GetComponent<MeshRenderer>().material = clampMaterial;
        Destroy(jaw2.GetComponent<Collider>());

        return clamp;
    }

    /// <summary>
    /// Perform cannulation at the specified position.
    /// </summary>
    public GameObject PerformCannulation(Vector3 position, Vector3 direction)
    {
        if (!isSurgeryActive)
        {
            Debug.LogWarning("No active surgery session.");
            return null;
        }

        GameObject cannula = CreateCannulaVisual(position, direction);
        activeCannulas.Add(cannula);

        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Cannulation,
            Position = position,
            Direction = direction,
            CreatedObject = cannula,
            Timestamp = Time.time
        });

        OnSurgicalAction?.Invoke(this, new SurgicalActionEventArgs
        {
            ActionType = SurgicalActionType.Cannulation,
            Position = position,
            Description = "Cannulation performed",
            Timestamp = Time.time
        });

        return cannula;
    }

    private GameObject CreateCannulaVisual(Vector3 position, Vector3 direction)
    {
        GameObject cannula = new GameObject("Cannula");
        cannula.transform.position = position;
        cannula.transform.forward = direction;

        // Create cannula tube
        GameObject tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tube.transform.SetParent(cannula.transform);
        tube.transform.localPosition = direction * 0.02f;
        tube.transform.localRotation = Quaternion.Euler(90, 0, 0);
        tube.transform.localScale = new Vector3(0.004f, 0.02f, 0.004f);

        Material tubeMaterial = CreateDefaultMaterial(new Color(0.8f, 0.8f, 0.9f, 0.8f));
        tube.GetComponent<MeshRenderer>().material = tubeMaterial;
        Destroy(tube.GetComponent<Collider>());

        return cannula;
    }

    /// <summary>
    /// Place a marker at the specified position.
    /// </summary>
    public GameObject PlaceMarker(Vector3 position, Color color, string label = "")
    {
        GameObject marker = CreateMarkerVisual(position, color, label);
        activeMarkers.Add(marker);

        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Marker,
            Position = position,
            CreatedObject = marker,
            Timestamp = Time.time
        });

        OnSurgicalAction?.Invoke(this, new SurgicalActionEventArgs
        {
            ActionType = SurgicalActionType.Marker,
            Position = position,
            Description = string.IsNullOrEmpty(label) ? "Marker placed" : label,
            Timestamp = Time.time
        });

        return marker;
    }

    private GameObject CreateMarkerVisual(Vector3 position, Color color, string label)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = string.IsNullOrEmpty(label) ? "Marker" : label;
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * 0.003f;

        Material markerMat = CreateDefaultMaterial(color);
        marker.GetComponent<MeshRenderer>().material = markerMat;

        // Replace collider with trigger
        Destroy(marker.GetComponent<Collider>());
        SphereCollider trigger = marker.AddComponent<SphereCollider>();
        trigger.isTrigger = true;

        return marker;
    }

    /// <summary>
    /// Create a running suture line between multiple points.
    /// </summary>
    public GameObject CreateRunningSuture(Vector3[] points)
    {
        if (!isSurgeryActive || points == null || points.Length < 2)
        {
            return null;
        }

        GameObject sutureLine = new GameObject("RunningSuture");

        LineRenderer lineRenderer = sutureLine.AddComponent<LineRenderer>();
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.startWidth = 0.0005f;
        lineRenderer.endWidth = 0.0005f;
        lineRenderer.material = sutureMaterial;

        // Add knots at each point
        foreach (Vector3 point in points)
        {
            GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knot.transform.SetParent(sutureLine.transform);
            knot.transform.position = point;
            knot.transform.localScale = Vector3.one * 0.0008f;
            knot.GetComponent<MeshRenderer>().material = sutureMaterial;
            Destroy(knot.GetComponent<Collider>());
        }

        activeSutures.Add(sutureLine);

        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Suture,
            Position = points[0],
            CreatedObject = sutureLine,
            Timestamp = Time.time
        });

        return sutureLine;
    }

    /// <summary>
    /// Perform excision (cutting out tissue).
    /// </summary>
    public GameObject PerformExcision(Vector3[] boundaryPoints)
    {
        if (!isSurgeryActive || boundaryPoints == null || boundaryPoints.Length < 3)
        {
            return null;
        }

        GameObject excision = new GameObject("ExcisionArea");

        // Create boundary visualization
        LineRenderer lineRenderer = excision.AddComponent<LineRenderer>();
        lineRenderer.positionCount = boundaryPoints.Length + 1;
        lineRenderer.SetPositions(boundaryPoints);
        lineRenderer.SetPosition(boundaryPoints.Length, boundaryPoints[0]); // Close loop
        lineRenderer.startWidth = incisionWidth;
        lineRenderer.endWidth = incisionWidth;
        lineRenderer.material = incisionMaterial;
        lineRenderer.loop = true;

        RecordAction(new SurgicalAction
        {
            Type = SurgicalActionType.Excision,
            Position = boundaryPoints[0],
            CreatedObject = excision,
            Timestamp = Time.time
        });

        OnSurgicalAction?.Invoke(this, new SurgicalActionEventArgs
        {
            ActionType = SurgicalActionType.Excision,
            Position = boundaryPoints[0],
            Description = "Excision performed",
            Timestamp = Time.time
        });

        return excision;
    }

    #endregion

    #region Undo System

    private void RecordAction(SurgicalAction action)
    {
        actionHistory.Push(action);

        // Limit history size
        if (actionHistory.Count > MaxHistorySize)
        {
            // Convert to array, remove oldest, convert back
            SurgicalAction[] actions = actionHistory.ToArray();
            actionHistory.Clear();
            for (int i = 0; i < MaxHistorySize - 1; i++)
            {
                actionHistory.Push(actions[actions.Length - 1 - i]);
            }
        }
    }

    /// <summary>
    /// Undo the last surgical action.
    /// </summary>
    public bool UndoLastAction()
    {
        if (actionHistory.Count == 0)
        {
            Debug.Log("No actions to undo.");
            return false;
        }

        SurgicalAction action = actionHistory.Pop();

        // Remove the created object
        if (action.CreatedObject != null)
        {
            // Remove from appropriate list
            switch (action.Type)
            {
                case SurgicalActionType.Incision:
                    activeIncisions.Remove(action.CreatedObject);
                    break;
                case SurgicalActionType.Suture:
                    activeSutures.Remove(action.CreatedObject);
                    break;
                case SurgicalActionType.Ablation:
                    activeAblations.Remove(action.CreatedObject);
                    break;
                case SurgicalActionType.Clamp:
                    activeClamps.Remove(action.CreatedObject);
                    break;
                case SurgicalActionType.Cannulation:
                    activeCannulas.Remove(action.CreatedObject);
                    break;
                case SurgicalActionType.Marker:
                    activeMarkers.Remove(action.CreatedObject);
                    break;
            }

            Destroy(action.CreatedObject);
        }

        OnActionUndone?.Invoke(this, new UndoEventArgs
        {
            UndoneAction = action,
            RemainingActions = actionHistory.Count
        });

        Debug.Log($"Undid action: {action.Type}");
        return true;
    }

    /// <summary>
    /// Get the number of actions in history.
    /// </summary>
    public int GetActionCount()
    {
        return actionHistory.Count;
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Get all active incisions.
    /// </summary>
    public List<GameObject> GetActiveIncisions()
    {
        return new List<GameObject>(activeIncisions);
    }

    /// <summary>
    /// Get all active sutures.
    /// </summary>
    public List<GameObject> GetActiveSutures()
    {
        return new List<GameObject>(activeSutures);
    }

    /// <summary>
    /// Get all active ablations.
    /// </summary>
    public List<GameObject> GetActiveAblations()
    {
        return new List<GameObject>(activeAblations);
    }

    /// <summary>
    /// Get all active clamps.
    /// </summary>
    public List<GameObject> GetActiveClamps()
    {
        return new List<GameObject>(activeClamps);
    }

    /// <summary>
    /// Check if surgery is active.
    /// </summary>
    public bool IsSurgeryActive()
    {
        return isSurgeryActive;
    }

    #endregion
}

/// <summary>
/// Represents a single surgical action for undo/redo.
/// </summary>
[System.Serializable]
public class SurgicalAction
{
    public SurgerySimulator.SurgicalActionType Type;
    public Vector3 Position;
    public Vector3 Direction;
    public Quaternion Rotation;
    public float Radius;
    public GameObject CreatedObject;
    public float Timestamp;
}

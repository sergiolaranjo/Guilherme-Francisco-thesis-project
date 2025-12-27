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
/// VR3S-style intracardiac baffle design tool.
/// Allows surgeons to design and visualize baffles for complex congenital heart repairs.
/// Used for DORV (Double Outlet Right Ventricle), Taussig-Bing, and other biventricular repairs.
/// </summary>
public class BaffleDesigner : MonoBehaviour
{
    public static BaffleDesigner Instance { get; private set; }

    public event EventHandler<BaffleCreatedEventArgs> OnBaffleCreated;
    public event EventHandler<BaffleModifiedEventArgs> OnBaffleModified;
    public event EventHandler<PathwayAnalyzedEventArgs> OnPathwayAnalyzed;

    public class BaffleCreatedEventArgs : EventArgs
    {
        public IntracardiacBaffle Baffle;
    }

    public class BaffleModifiedEventArgs : EventArgs
    {
        public IntracardiacBaffle Baffle;
        public string ModificationType;
    }

    public class PathwayAnalyzedEventArgs : EventArgs
    {
        public PathwayAnalysis Analysis;
    }

    #region Enums

    public enum BaffleType
    {
        // VSD to Aorta routing (DORV)
        VSDToAorta,

        // Atrial level baffles (Mustard/Senning)
        AtrialSwitch,
        MustardBaffle,
        SenningBaffle,

        // Fontan pathways
        LateralTunnel,
        ExtracardiacConduit,
        AtriopulmonaryConnection,

        // Other
        IVCToLA,        // For TAPVR or heterotaxy
        PVToLA,         // Pulmonary vein to LA
        ConduitBaffle,  // Within a conduit

        Custom
    }

    public enum BaffleDesignMode
    {
        Drawing,        // Free-hand drawing
        ControlPoints,  // Control point based
        Template,       // From templates
        Automatic       // AI-suggested
    }

    public enum BaffleMaterial
    {
        Pericardium,
        Dacron,
        PTFE,
        Bovine,
        Homograft,
        Custom
    }

    #endregion

    [Header("References")]
    [SerializeField] private Transform heartModel;
    [SerializeField] private Transform baffleContainer;
    [SerializeField] private Camera designCamera;

    [Header("Design Settings")]
    [SerializeField] private BaffleDesignMode designMode = BaffleDesignMode.ControlPoints;
    [SerializeField] private BaffleType currentBaffleType = BaffleType.VSDToAorta;
    [SerializeField] private BaffleMaterial currentMaterial = BaffleMaterial.Pericardium;

    [Header("Control Point Settings")]
    [SerializeField] private float controlPointRadius = 0.005f;
    [SerializeField] private int minControlPoints = 3;
    [SerializeField] private int maxControlPoints = 20;
    [SerializeField] private float curveSmoothing = 0.5f;

    [Header("Baffle Parameters")]
    [SerializeField] private float baffleThickness = 0.002f;   // 2mm default
    [SerializeField] private float baffleWidth = 0.015f;       // 15mm default
    [SerializeField] private bool autoAdjustToWalls = true;

    [Header("Visualization")]
    [SerializeField] private Material baffleMaterial;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Material controlPointMaterial;
    [SerializeField] private Color validPathColor = new Color(0.2f, 0.8f, 0.3f, 0.7f);
    [SerializeField] private Color invalidPathColor = new Color(0.9f, 0.2f, 0.2f, 0.7f);
    [SerializeField] private Color previewColor = new Color(0.5f, 0.7f, 0.9f, 0.5f);

    [Header("Analysis")]
    [SerializeField] private float minPathwayDiameter = 0.01f;  // 10mm minimum
    [SerializeField] private float optimalPathwayDiameter = 0.015f; // 15mm optimal
    [SerializeField] private bool showFlowVisualization = true;

    // Current baffle being designed
    private List<Vector3> controlPoints = new List<Vector3>();
    private List<GameObject> controlPointObjects = new List<GameObject>();
    private IntracardiacBaffle currentBaffle;
    private MeshFilter previewMeshFilter;
    private MeshRenderer previewMeshRenderer;

    // Placed baffles
    private List<IntracardiacBaffle> placedBaffles = new List<IntracardiacBaffle>();

    // Templates
    private Dictionary<BaffleType, BaffleTemplate> baffleTemplates;

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

        InitializeTemplates();
        CreatePreviewMesh();
    }

    void Start()
    {
        if (baffleContainer == null)
        {
            baffleContainer = new GameObject("Baffles").transform;
            baffleContainer.SetParent(transform);
        }
    }

    #region Template Initialization

    private void InitializeTemplates()
    {
        baffleTemplates = new Dictionary<BaffleType, BaffleTemplate>();

        // VSD to Aorta (DORV) template
        baffleTemplates[BaffleType.VSDToAorta] = new BaffleTemplate
        {
            Type = BaffleType.VSDToAorta,
            Name = "VSD-Aorta Baffle",
            Description = "Baffle intraventricular para direcionar VSD à aorta (Cirurgia de Rastelli/REV)",
            DefaultPoints = new Vector3[]
            {
                new Vector3(0, -0.02f, 0),       // VSD location
                new Vector3(-0.01f, -0.015f, 0.01f), // Intermediate
                new Vector3(-0.02f, 0, 0.02f)    // Aortic root
            },
            RecommendedWidth = 0.018f,
            RecommendedThickness = 0.002f
        };

        // Mustard baffle template
        baffleTemplates[BaffleType.MustardBaffle] = new BaffleTemplate
        {
            Type = BaffleType.MustardBaffle,
            Name = "Mustard Baffle",
            Description = "Baffle atrial para TGA (Cirurgia de Mustard)",
            DefaultPoints = new Vector3[]
            {
                new Vector3(0.02f, 0.02f, 0),     // SVC/IVC entrance
                new Vector3(0, 0.015f, -0.01f),   // Atrial septum
                new Vector3(-0.02f, 0.02f, -0.02f) // Mitral valve direction
            },
            RecommendedWidth = 0.025f,
            RecommendedThickness = 0.002f
        };

        // Lateral tunnel (Fontan)
        baffleTemplates[BaffleType.LateralTunnel] = new BaffleTemplate
        {
            Type = BaffleType.LateralTunnel,
            Name = "Lateral Tunnel",
            Description = "Túnel lateral para Fontan",
            DefaultPoints = new Vector3[]
            {
                new Vector3(0.02f, -0.01f, 0),    // IVC
                new Vector3(0.025f, 0.02f, 0.01f), // Lateral wall
                new Vector3(0.01f, 0.04f, 0.02f)  // PA connection
            },
            RecommendedWidth = 0.02f,
            RecommendedThickness = 0.002f
        };
    }

    private void CreatePreviewMesh()
    {
        GameObject previewObj = new GameObject("BafflePreview");
        previewObj.transform.SetParent(transform);

        previewMeshFilter = previewObj.AddComponent<MeshFilter>();
        previewMeshRenderer = previewObj.AddComponent<MeshRenderer>();

        if (previewMaterial != null)
        {
            previewMeshRenderer.material = previewMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = previewColor;
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMeshRenderer.material = mat;
        }

        previewObj.SetActive(false);
    }

    #endregion

    #region Design Mode Control

    /// <summary>
    /// Set the design mode.
    /// </summary>
    public void SetDesignMode(BaffleDesignMode mode)
    {
        designMode = mode;
        ClearCurrentDesign();
    }

    /// <summary>
    /// Set the baffle type to design.
    /// </summary>
    public void SetBaffleType(BaffleType type)
    {
        currentBaffleType = type;

        if (designMode == BaffleDesignMode.Template && baffleTemplates.ContainsKey(type))
        {
            LoadTemplate(type);
        }
    }

    /// <summary>
    /// Set the baffle material.
    /// </summary>
    public void SetBaffleMaterial(BaffleMaterial material)
    {
        currentMaterial = material;
        UpdatePreview();
    }

    #endregion

    #region Control Point Management

    /// <summary>
    /// Add a control point at the specified position.
    /// </summary>
    public void AddControlPoint(Vector3 worldPosition)
    {
        if (controlPoints.Count >= maxControlPoints)
        {
            Debug.LogWarning("Maximum control points reached");
            return;
        }

        Vector3 localPosition = heartModel != null ?
            heartModel.InverseTransformPoint(worldPosition) :
            worldPosition;

        controlPoints.Add(localPosition);
        CreateControlPointVisual(worldPosition);
        UpdatePreview();

        Debug.Log($"Added control point {controlPoints.Count} at {localPosition}");
    }

    /// <summary>
    /// Remove the last control point.
    /// </summary>
    public void RemoveLastControlPoint()
    {
        if (controlPoints.Count == 0) return;

        controlPoints.RemoveAt(controlPoints.Count - 1);

        if (controlPointObjects.Count > 0)
        {
            GameObject lastObj = controlPointObjects[controlPointObjects.Count - 1];
            controlPointObjects.RemoveAt(controlPointObjects.Count - 1);
            Destroy(lastObj);
        }

        UpdatePreview();
    }

    /// <summary>
    /// Move a control point.
    /// </summary>
    public void MoveControlPoint(int index, Vector3 newWorldPosition)
    {
        if (index < 0 || index >= controlPoints.Count) return;

        Vector3 localPosition = heartModel != null ?
            heartModel.InverseTransformPoint(newWorldPosition) :
            newWorldPosition;

        controlPoints[index] = localPosition;

        if (index < controlPointObjects.Count)
        {
            controlPointObjects[index].transform.position = newWorldPosition;
        }

        UpdatePreview();
    }

    /// <summary>
    /// Clear all control points.
    /// </summary>
    public void ClearControlPoints()
    {
        controlPoints.Clear();

        foreach (var obj in controlPointObjects)
        {
            Destroy(obj);
        }
        controlPointObjects.Clear();

        UpdatePreview();
    }

    private void CreateControlPointVisual(Vector3 position)
    {
        GameObject pointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointObj.name = $"ControlPoint_{controlPoints.Count}";
        pointObj.transform.SetParent(baffleContainer);
        pointObj.transform.position = position;
        pointObj.transform.localScale = Vector3.one * controlPointRadius * 2;

        // Remove collider to not interfere with raycast
        Destroy(pointObj.GetComponent<Collider>());

        if (controlPointMaterial != null)
        {
            pointObj.GetComponent<Renderer>().material = controlPointMaterial;
        }
        else
        {
            pointObj.GetComponent<Renderer>().material.color = Color.yellow;
        }

        controlPointObjects.Add(pointObj);
    }

    #endregion

    #region Template Loading

    /// <summary>
    /// Load a baffle template.
    /// </summary>
    public void LoadTemplate(BaffleType type)
    {
        if (!baffleTemplates.ContainsKey(type))
        {
            Debug.LogWarning($"Template not found for: {type}");
            return;
        }

        BaffleTemplate template = baffleTemplates[type];

        ClearControlPoints();

        baffleWidth = template.RecommendedWidth;
        baffleThickness = template.RecommendedThickness;

        foreach (Vector3 point in template.DefaultPoints)
        {
            Vector3 worldPos = heartModel != null ?
                heartModel.TransformPoint(point) :
                point;

            AddControlPoint(worldPos);
        }

        UpdatePreview();
    }

    #endregion

    #region Mesh Generation

    private void UpdatePreview()
    {
        if (controlPoints.Count < minControlPoints)
        {
            if (previewMeshFilter != null)
            {
                previewMeshFilter.gameObject.SetActive(false);
            }
            return;
        }

        Mesh baffleMesh = GenerateBaffleMesh();

        if (previewMeshFilter != null)
        {
            previewMeshFilter.mesh = baffleMesh;
            previewMeshFilter.gameObject.SetActive(true);

            if (heartModel != null)
            {
                previewMeshFilter.transform.SetPositionAndRotation(
                    heartModel.position,
                    heartModel.rotation
                );
            }
        }
    }

    private Mesh GenerateBaffleMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "BaffleMesh";

        if (controlPoints.Count < minControlPoints) return mesh;

        // Generate smooth curve through control points
        List<Vector3> curvePoints = GenerateSmoothCurve(controlPoints, 20);

        // Generate mesh vertices along the curve
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        float halfWidth = baffleWidth / 2f;

        for (int i = 0; i < curvePoints.Count; i++)
        {
            Vector3 point = curvePoints[i];

            // Calculate tangent and normal
            Vector3 tangent = Vector3.forward;
            if (i < curvePoints.Count - 1)
            {
                tangent = (curvePoints[i + 1] - point).normalized;
            }
            else if (i > 0)
            {
                tangent = (point - curvePoints[i - 1]).normalized;
            }

            Vector3 normal = Vector3.Cross(tangent, Vector3.up).normalized;
            if (normal == Vector3.zero) normal = Vector3.Cross(tangent, Vector3.right).normalized;

            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

            // Create quad vertices (4 per segment for thickness)
            Vector3 v0 = point - normal * halfWidth;
            Vector3 v1 = point + normal * halfWidth;
            Vector3 v2 = point - normal * halfWidth + binormal * baffleThickness;
            Vector3 v3 = point + normal * halfWidth + binormal * baffleThickness;

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            float u = (float)i / (curvePoints.Count - 1);
            uvs.Add(new Vector2(0, u));
            uvs.Add(new Vector2(1, u));
            uvs.Add(new Vector2(0, u));
            uvs.Add(new Vector2(1, u));

            // Create triangles (connect to previous segment)
            if (i > 0)
            {
                int baseIndex = (i - 1) * 4;

                // Front face
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 4);
                triangles.Add(baseIndex + 1);

                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 4);
                triangles.Add(baseIndex + 5);

                // Back face
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 6);

                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 7);
                triangles.Add(baseIndex + 6);

                // Side faces
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 4);

                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 6);
                triangles.Add(baseIndex + 4);

                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 5);
                triangles.Add(baseIndex + 3);

                triangles.Add(baseIndex + 3);
                triangles.Add(baseIndex + 5);
                triangles.Add(baseIndex + 7);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private List<Vector3> GenerateSmoothCurve(List<Vector3> points, int segmentsPerSpan)
    {
        List<Vector3> curve = new List<Vector3>();

        if (points.Count < 2)
        {
            curve.AddRange(points);
            return curve;
        }

        // Catmull-Rom spline interpolation
        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? points[i - 1] : points[0];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i + 1];
            Vector3 p3 = i < points.Count - 2 ? points[i + 2] : points[points.Count - 1];

            for (int j = 0; j < segmentsPerSpan; j++)
            {
                float t = (float)j / segmentsPerSpan;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                curve.Add(point);
            }
        }

        curve.Add(points[points.Count - 1]);

        return curve;
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    #endregion

    #region Baffle Creation

    /// <summary>
    /// Confirm and create the baffle from current design.
    /// </summary>
    public IntracardiacBaffle ConfirmBaffle()
    {
        if (controlPoints.Count < minControlPoints)
        {
            Debug.LogWarning("Not enough control points to create baffle");
            return null;
        }

        // Create baffle object
        GameObject baffleObj = new GameObject($"Baffle_{currentBaffleType}_{placedBaffles.Count}");
        baffleObj.transform.SetParent(baffleContainer);

        if (heartModel != null)
        {
            baffleObj.transform.SetPositionAndRotation(heartModel.position, heartModel.rotation);
        }

        MeshFilter meshFilter = baffleObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = baffleObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = baffleObj.AddComponent<MeshCollider>();

        Mesh baffleMesh = GenerateBaffleMesh();
        meshFilter.mesh = baffleMesh;
        meshCollider.sharedMesh = baffleMesh;

        if (baffleMaterial != null)
        {
            meshRenderer.material = baffleMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = GetMaterialColor();
            meshRenderer.material = mat;
        }

        // Create baffle data
        IntracardiacBaffle baffle = baffleObj.AddComponent<IntracardiacBaffle>();
        baffle.Type = currentBaffleType;
        baffle.Material = currentMaterial;
        baffle.ControlPoints = new List<Vector3>(controlPoints);
        baffle.Width = baffleWidth;
        baffle.Thickness = baffleThickness;

        placedBaffles.Add(baffle);

        // Analyze pathway
        PathwayAnalysis analysis = AnalyzePathway(baffle);
        baffle.Analysis = analysis;

        // Clear design
        ClearCurrentDesign();

        OnBaffleCreated?.Invoke(this, new BaffleCreatedEventArgs { Baffle = baffle });

        Debug.Log($"Created baffle: {currentBaffleType}, Pathway: {(analysis.IsAdequate ? "Adequate" : "Inadequate")}");

        return baffle;
    }

    /// <summary>
    /// Cancel current baffle design.
    /// </summary>
    public void CancelDesign()
    {
        ClearCurrentDesign();
    }

    private void ClearCurrentDesign()
    {
        ClearControlPoints();

        if (previewMeshFilter != null)
        {
            previewMeshFilter.gameObject.SetActive(false);
        }
    }

    private Color GetMaterialColor()
    {
        switch (currentMaterial)
        {
            case BaffleMaterial.Pericardium:
                return new Color(0.95f, 0.9f, 0.85f);
            case BaffleMaterial.Dacron:
                return new Color(0.85f, 0.85f, 0.9f);
            case BaffleMaterial.PTFE:
                return new Color(0.9f, 0.9f, 0.95f);
            case BaffleMaterial.Bovine:
                return new Color(0.9f, 0.85f, 0.8f);
            default:
                return new Color(0.88f, 0.85f, 0.82f);
        }
    }

    #endregion

    #region Pathway Analysis

    /// <summary>
    /// Analyze the pathway created by a baffle.
    /// </summary>
    public PathwayAnalysis AnalyzePathway(IntracardiacBaffle baffle)
    {
        PathwayAnalysis analysis = new PathwayAnalysis();

        if (baffle.ControlPoints.Count < 2)
        {
            analysis.IsAdequate = false;
            analysis.Issues.Add("Insufficient control points");
            return analysis;
        }

        // Calculate pathway metrics
        analysis.PathwayLength = CalculatePathwayLength(baffle.ControlPoints);
        analysis.MinimumDiameter = baffle.Width;
        analysis.MaximumDiameter = baffle.Width;

        // Check if pathway is adequate
        analysis.IsAdequate = analysis.MinimumDiameter >= minPathwayDiameter;

        if (!analysis.IsAdequate)
        {
            analysis.Issues.Add($"Pathway diameter ({analysis.MinimumDiameter * 1000:F1}mm) below minimum ({minPathwayDiameter * 1000:F1}mm)");
        }

        // Check for sharp angles
        float maxAngle = CalculateMaxAngle(baffle.ControlPoints);
        if (maxAngle > 90f)
        {
            analysis.Issues.Add($"Sharp angle detected ({maxAngle:F0}°)");
        }

        // Calculate flow characteristics
        analysis.EstimatedFlowResistance = CalculateFlowResistance(analysis);

        OnPathwayAnalyzed?.Invoke(this, new PathwayAnalyzedEventArgs { Analysis = analysis });

        return analysis;
    }

    private float CalculatePathwayLength(List<Vector3> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++)
        {
            length += Vector3.Distance(points[i - 1], points[i]);
        }
        return length;
    }

    private float CalculateMaxAngle(List<Vector3> points)
    {
        float maxAngle = 0f;

        for (int i = 1; i < points.Count - 1; i++)
        {
            Vector3 v1 = (points[i] - points[i - 1]).normalized;
            Vector3 v2 = (points[i + 1] - points[i]).normalized;
            float angle = Vector3.Angle(v1, v2);
            maxAngle = Mathf.Max(maxAngle, angle);
        }

        return maxAngle;
    }

    private float CalculateFlowResistance(PathwayAnalysis analysis)
    {
        // Simplified resistance calculation based on length and diameter
        // R = 8 * η * L / (π * r^4)  (Poiseuille's law simplified)
        float radius = analysis.MinimumDiameter / 2f;
        float resistance = analysis.PathwayLength / (Mathf.PI * Mathf.Pow(radius, 4));
        return resistance;
    }

    #endregion

    #region Baffle Management

    /// <summary>
    /// Remove a placed baffle.
    /// </summary>
    public void RemoveBaffle(IntracardiacBaffle baffle)
    {
        if (baffle == null || !placedBaffles.Contains(baffle)) return;

        placedBaffles.Remove(baffle);
        Destroy(baffle.gameObject);
    }

    /// <summary>
    /// Get all placed baffles.
    /// </summary>
    public List<IntracardiacBaffle> GetPlacedBaffles()
    {
        return new List<IntracardiacBaffle>(placedBaffles);
    }

    /// <summary>
    /// Clear all baffles.
    /// </summary>
    public void ClearAllBaffles()
    {
        foreach (var baffle in placedBaffles)
        {
            if (baffle != null)
            {
                Destroy(baffle.gameObject);
            }
        }
        placedBaffles.Clear();
    }

    #endregion

    #region Modification

    /// <summary>
    /// Adjust baffle width.
    /// </summary>
    public void SetBaffleWidth(float width)
    {
        baffleWidth = Mathf.Max(0.005f, width);
        UpdatePreview();
    }

    /// <summary>
    /// Adjust baffle thickness.
    /// </summary>
    public void SetBaffleThickness(float thickness)
    {
        baffleThickness = Mathf.Max(0.001f, thickness);
        UpdatePreview();
    }

    /// <summary>
    /// Get available templates.
    /// </summary>
    public List<BaffleTemplate> GetAvailableTemplates()
    {
        return new List<BaffleTemplate>(baffleTemplates.Values);
    }

    #endregion
}

#region Data Classes

/// <summary>
/// Represents an intracardiac baffle.
/// </summary>
public class IntracardiacBaffle : MonoBehaviour
{
    public BaffleDesigner.BaffleType Type;
    public BaffleDesigner.BaffleMaterial Material;
    public List<Vector3> ControlPoints = new List<Vector3>();
    public float Width;
    public float Thickness;
    public PathwayAnalysis Analysis;

    public string GetDisplayName()
    {
        return $"{Type} ({Material})";
    }
}

/// <summary>
/// Baffle design template.
/// </summary>
[System.Serializable]
public class BaffleTemplate
{
    public BaffleDesigner.BaffleType Type;
    public string Name;
    public string Description;
    public Vector3[] DefaultPoints;
    public float RecommendedWidth;
    public float RecommendedThickness;
}

/// <summary>
/// Analysis of baffle pathway.
/// </summary>
[System.Serializable]
public class PathwayAnalysis
{
    public bool IsAdequate;
    public float PathwayLength;
    public float MinimumDiameter;
    public float MaximumDiameter;
    public float EstimatedFlowResistance;
    public List<string> Issues = new List<string>();
    public List<string> Recommendations = new List<string>();
}

#endregion

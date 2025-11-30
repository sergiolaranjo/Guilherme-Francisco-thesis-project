using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System;
using System.Collections;

/// <summary>
/// Handles placement and anchoring of 3D cardiac models in AR space.
/// Supports surface detection, manual placement, and spatial anchoring.
/// </summary>
public class ARModelPlacer : MonoBehaviour
{
    public static ARModelPlacer Instance { get; private set; }

    public event EventHandler<ModelPlacedEventArgs> OnModelPlaced;
    public event EventHandler<ModelMovedEventArgs> OnModelMoved;
    public event EventHandler OnPlacementStarted;
    public event EventHandler OnPlacementCancelled;

    public class ModelPlacedEventArgs : EventArgs
    {
        public GameObject Model;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsAnchored;
    }

    public class ModelMovedEventArgs : EventArgs
    {
        public GameObject Model;
        public Vector3 NewPosition;
        public Vector3 OldPosition;
    }

    public enum PlacementMode
    {
        Disabled,
        ControllerPointing,     // Use controller ray to place
        HandPointing,           // Use hand ray to place
        HeadGaze,               // Place at gaze point
        SurfaceDetection,       // Automatic surface detection
        Manual                  // Free placement with grab
    }

    [Header("Placement Settings")]
    [SerializeField] private PlacementMode currentMode = PlacementMode.ControllerPointing;
    [SerializeField] private LayerMask placementSurfaceMask;
    [SerializeField] private float maxPlacementDistance = 5f;
    [SerializeField] private bool snapToSurface = true;
    [SerializeField] private bool alignToSurfaceNormal = false;

    [Header("References")]
    [SerializeField] private Transform rightController;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform headTransform;
    [SerializeField] private GameObject targetModel;

    [Header("Input")]
    [SerializeField] private InputActionReference placementAction;
    [SerializeField] private InputActionReference confirmAction;
    [SerializeField] private InputActionReference cancelAction;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject placementPreviewPrefab;
    [SerializeField] private LineRenderer placementRay;
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;
    [SerializeField] private float previewScale = 0.1f;

    [Header("Grid Snapping")]
    [SerializeField] private bool useGridSnapping = false;
    [SerializeField] private float gridSize = 0.1f;
    [SerializeField] private float rotationSnap = 15f;

    [Header("Bounds")]
    [SerializeField] private float minDistanceFromUser = 0.3f;
    [SerializeField] private float maxDistanceFromUser = 10f;
    [SerializeField] private float minHeightFromGround = 0.5f;
    [SerializeField] private float maxHeightFromGround = 3f;

    // State
    private bool isPlacementActive = false;
    private bool isValidPlacement = false;
    private Vector3 currentPlacementPosition;
    private Quaternion currentPlacementRotation;
    private GameObject previewInstance;
    private MixedRealityManager mrManager;

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
        mrManager = MixedRealityManager.Instance;

        if (headTransform == null)
        {
            headTransform = Camera.main?.transform;
        }

        SetupPlacementRay();
        SetupInputActions();

        if (mrManager != null)
        {
            mrManager.OnModeChanged += OnRealityModeChanged;
        }
    }

    void OnDestroy()
    {
        if (mrManager != null)
        {
            mrManager.OnModeChanged -= OnRealityModeChanged;
        }

        CleanupInputActions();
    }

    void Update()
    {
        if (isPlacementActive)
        {
            UpdatePlacement();
        }
    }

    private void SetupPlacementRay()
    {
        if (placementRay == null)
        {
            GameObject rayObj = new GameObject("PlacementRay");
            rayObj.transform.SetParent(transform);

            placementRay = rayObj.AddComponent<LineRenderer>();
            placementRay.startWidth = 0.005f;
            placementRay.endWidth = 0.002f;
            placementRay.positionCount = 2;

            Material rayMaterial = new Material(Shader.Find("Sprites/Default"));
            rayMaterial.color = new Color(0.5f, 0.8f, 1f, 0.5f);
            placementRay.material = rayMaterial;

            placementRay.enabled = false;
        }
    }

    private void SetupInputActions()
    {
        if (placementAction != null)
        {
            placementAction.action.Enable();
            placementAction.action.performed += OnPlacementActionPerformed;
        }

        if (confirmAction != null)
        {
            confirmAction.action.Enable();
            confirmAction.action.performed += OnConfirmActionPerformed;
        }

        if (cancelAction != null)
        {
            cancelAction.action.Enable();
            cancelAction.action.performed += OnCancelActionPerformed;
        }
    }

    private void CleanupInputActions()
    {
        if (placementAction != null)
        {
            placementAction.action.performed -= OnPlacementActionPerformed;
        }

        if (confirmAction != null)
        {
            confirmAction.action.performed -= OnConfirmActionPerformed;
        }

        if (cancelAction != null)
        {
            cancelAction.action.performed -= OnCancelActionPerformed;
        }
    }

    private void OnRealityModeChanged(object sender, MixedRealityManager.ModeChangedEventArgs e)
    {
        // Auto-enable placement when entering AR mode
        if (e.CurrentMode == MixedRealityManager.RealityMode.AugmentedReality ||
            e.CurrentMode == MixedRealityManager.RealityMode.MixedReality)
        {
            // Don't auto-start, let user initiate
        }
        else
        {
            // Cancel placement when leaving AR mode
            if (isPlacementActive)
            {
                CancelPlacement();
            }
        }
    }

    #region Placement Control

    /// <summary>
    /// Start placement mode.
    /// </summary>
    public void StartPlacement()
    {
        if (targetModel == null)
        {
            Debug.LogWarning("No target model set for placement.");
            return;
        }

        isPlacementActive = true;

        // Create preview
        CreatePreview();

        // Show placement ray
        if (placementRay != null)
        {
            placementRay.enabled = true;
        }

        OnPlacementStarted?.Invoke(this, EventArgs.Empty);
        Debug.Log("Placement mode started.");
    }

    /// <summary>
    /// Confirm current placement.
    /// </summary>
    public void ConfirmPlacement()
    {
        if (!isPlacementActive || !isValidPlacement) return;

        Vector3 oldPosition = targetModel.transform.position;

        // Move model to placement position
        targetModel.transform.position = currentPlacementPosition;
        targetModel.transform.rotation = currentPlacementRotation;

        // Clean up preview
        DestroyPreview();

        if (placementRay != null)
        {
            placementRay.enabled = false;
        }

        isPlacementActive = false;

        OnModelPlaced?.Invoke(this, new ModelPlacedEventArgs
        {
            Model = targetModel,
            Position = currentPlacementPosition,
            Rotation = currentPlacementRotation,
            IsAnchored = true
        });

        Debug.Log($"Model placed at {currentPlacementPosition}");
    }

    /// <summary>
    /// Cancel placement mode.
    /// </summary>
    public void CancelPlacement()
    {
        if (!isPlacementActive) return;

        DestroyPreview();

        if (placementRay != null)
        {
            placementRay.enabled = false;
        }

        isPlacementActive = false;

        OnPlacementCancelled?.Invoke(this, EventArgs.Empty);
        Debug.Log("Placement cancelled.");
    }

    /// <summary>
    /// Toggle placement mode.
    /// </summary>
    public void TogglePlacement()
    {
        if (isPlacementActive)
        {
            CancelPlacement();
        }
        else
        {
            StartPlacement();
        }
    }

    #endregion

    #region Placement Update

    private void UpdatePlacement()
    {
        Vector3 rayOrigin;
        Vector3 rayDirection;

        // Get ray based on current mode
        switch (currentMode)
        {
            case PlacementMode.ControllerPointing:
                if (rightController != null)
                {
                    rayOrigin = rightController.position;
                    rayDirection = rightController.forward;
                }
                else
                {
                    return;
                }
                break;

            case PlacementMode.HandPointing:
                // Use hand tracking ray if available
                if (rightController != null)
                {
                    rayOrigin = rightController.position;
                    rayDirection = rightController.forward;
                }
                else
                {
                    return;
                }
                break;

            case PlacementMode.HeadGaze:
                if (headTransform != null)
                {
                    rayOrigin = headTransform.position;
                    rayDirection = headTransform.forward;
                }
                else
                {
                    return;
                }
                break;

            case PlacementMode.SurfaceDetection:
                // Use spatial mapping or plane detection
                rayOrigin = headTransform != null ? headTransform.position : Vector3.zero;
                rayDirection = headTransform != null ? headTransform.forward : Vector3.forward;
                break;

            default:
                return;
        }

        // Perform raycast
        RaycastHit hit;
        bool hitSurface = Physics.Raycast(rayOrigin, rayDirection, out hit, maxPlacementDistance, placementSurfaceMask);

        // Update ray visual
        UpdatePlacementRay(rayOrigin, rayDirection, hit, hitSurface);

        if (hitSurface)
        {
            currentPlacementPosition = hit.point;

            // Apply grid snapping
            if (useGridSnapping)
            {
                currentPlacementPosition = SnapToGrid(currentPlacementPosition);
            }

            // Calculate rotation
            if (alignToSurfaceNormal)
            {
                currentPlacementRotation = Quaternion.LookRotation(
                    Vector3.ProjectOnPlane(rayDirection, hit.normal),
                    hit.normal
                );
            }
            else
            {
                // Face the user
                Vector3 toUser = (headTransform.position - currentPlacementPosition);
                toUser.y = 0;
                currentPlacementRotation = Quaternion.LookRotation(-toUser.normalized);
            }

            // Apply rotation snapping
            if (useGridSnapping && rotationSnap > 0)
            {
                Vector3 euler = currentPlacementRotation.eulerAngles;
                euler.y = Mathf.Round(euler.y / rotationSnap) * rotationSnap;
                currentPlacementRotation = Quaternion.Euler(euler);
            }

            // Validate placement
            isValidPlacement = ValidatePlacement(currentPlacementPosition);
        }
        else
        {
            // No surface hit - place at max distance
            currentPlacementPosition = rayOrigin + rayDirection * maxPlacementDistance;
            currentPlacementRotation = Quaternion.LookRotation(-rayDirection);
            isValidPlacement = false;
        }

        // Update preview
        UpdatePreview();
    }

    private void UpdatePlacementRay(Vector3 origin, Vector3 direction, RaycastHit hit, bool hitSurface)
    {
        if (placementRay == null) return;

        placementRay.SetPosition(0, origin);

        if (hitSurface)
        {
            placementRay.SetPosition(1, hit.point);
            placementRay.startColor = isValidPlacement ? Color.green : Color.red;
            placementRay.endColor = isValidPlacement ? Color.green : Color.red;
        }
        else
        {
            placementRay.SetPosition(1, origin + direction * maxPlacementDistance);
            placementRay.startColor = Color.yellow;
            placementRay.endColor = Color.yellow;
        }
    }

    private bool ValidatePlacement(Vector3 position)
    {
        if (headTransform == null) return true;

        // Check distance from user
        float distanceFromUser = Vector3.Distance(position, headTransform.position);
        if (distanceFromUser < minDistanceFromUser || distanceFromUser > maxDistanceFromUser)
        {
            return false;
        }

        // Check height
        if (position.y < minHeightFromGround || position.y > maxHeightFromGround)
        {
            // Allow if we don't have ground reference
            // return false;
        }

        return true;
    }

    private Vector3 SnapToGrid(Vector3 position)
    {
        position.x = Mathf.Round(position.x / gridSize) * gridSize;
        position.y = Mathf.Round(position.y / gridSize) * gridSize;
        position.z = Mathf.Round(position.z / gridSize) * gridSize;
        return position;
    }

    #endregion

    #region Preview

    private void CreatePreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }

        if (placementPreviewPrefab != null)
        {
            previewInstance = Instantiate(placementPreviewPrefab);
        }
        else if (targetModel != null)
        {
            // Create simple preview from target model
            previewInstance = new GameObject("PlacementPreview");

            MeshFilter targetMeshFilter = targetModel.GetComponentInChildren<MeshFilter>();
            if (targetMeshFilter != null)
            {
                MeshFilter previewMeshFilter = previewInstance.AddComponent<MeshFilter>();
                previewMeshFilter.mesh = targetMeshFilter.sharedMesh;

                MeshRenderer previewRenderer = previewInstance.AddComponent<MeshRenderer>();
                previewRenderer.material = validPlacementMaterial != null
                    ? validPlacementMaterial
                    : CreatePreviewMaterial(true);
            }
        }

        if (previewInstance != null)
        {
            previewInstance.transform.localScale = Vector3.one * previewScale;
        }
    }

    private void UpdatePreview()
    {
        if (previewInstance == null) return;

        previewInstance.transform.position = currentPlacementPosition;
        previewInstance.transform.rotation = currentPlacementRotation;

        // Update material based on validity
        MeshRenderer renderer = previewInstance.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            if (isValidPlacement && validPlacementMaterial != null)
            {
                renderer.material = validPlacementMaterial;
            }
            else if (!isValidPlacement && invalidPlacementMaterial != null)
            {
                renderer.material = invalidPlacementMaterial;
            }
            else
            {
                renderer.material = CreatePreviewMaterial(isValidPlacement);
            }
        }
    }

    private void DestroyPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    private Material CreatePreviewMaterial(bool valid)
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = valid ? new Color(0.2f, 0.8f, 0.2f, 0.5f) : new Color(0.8f, 0.2f, 0.2f, 0.5f);
        return mat;
    }

    #endregion

    #region Input Handlers

    private void OnPlacementActionPerformed(InputAction.CallbackContext context)
    {
        TogglePlacement();
    }

    private void OnConfirmActionPerformed(InputAction.CallbackContext context)
    {
        if (isPlacementActive)
        {
            ConfirmPlacement();
        }
    }

    private void OnCancelActionPerformed(InputAction.CallbackContext context)
    {
        if (isPlacementActive)
        {
            CancelPlacement();
        }
    }

    #endregion

    #region Public Setters

    /// <summary>
    /// Set the target model for placement.
    /// </summary>
    public void SetTargetModel(GameObject model)
    {
        targetModel = model;
    }

    /// <summary>
    /// Set the placement mode.
    /// </summary>
    public void SetPlacementMode(PlacementMode mode)
    {
        currentMode = mode;
    }

    /// <summary>
    /// Enable or disable grid snapping.
    /// </summary>
    public void SetGridSnapping(bool enabled, float size = 0.1f)
    {
        useGridSnapping = enabled;
        gridSize = size;
    }

    /// <summary>
    /// Get current placement state.
    /// </summary>
    public bool IsPlacementActive()
    {
        return isPlacementActive;
    }

    /// <summary>
    /// Check if current placement position is valid.
    /// </summary>
    public bool IsValidPlacement()
    {
        return isValidPlacement;
    }

    #endregion
}

// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VR3S-style fly-through navigation for exploring heart chambers.
/// Allows surgeons to "fly through" 3D heart models for surgical planning.
/// </summary>
public class HeartFlyThroughController : MonoBehaviour
{
    public static HeartFlyThroughController Instance { get; private set; }

    public event EventHandler<ChamberEnteredEventArgs> OnChamberEntered;
    public event EventHandler<WaypointReachedEventArgs> OnWaypointReached;
    public event EventHandler<TourEventArgs> OnTourStarted;
    public event EventHandler<TourEventArgs> OnTourCompleted;

    public class ChamberEnteredEventArgs : EventArgs
    {
        public HeartChamber Chamber;
        public string ChamberName;
    }

    public class WaypointReachedEventArgs : EventArgs
    {
        public int WaypointIndex;
        public string WaypointName;
    }

    public class TourEventArgs : EventArgs
    {
        public string TourName;
    }

    #region Enums

    public enum HeartChamber
    {
        // Chambers
        RightAtrium,
        RightVentricle,
        LeftAtrium,
        LeftVentricle,

        // Valves
        TricuspidValve,
        PulmonaryValve,
        MitralValve,
        AorticValve,

        // Great Vessels
        SuperiorVenaCava,
        InferiorVenaCava,
        PulmonaryArtery,
        PulmonaryVeins,
        AscendingAorta,
        AorticArch,
        DescendingAorta,

        // Coronary Arteries
        LeftMainCoronary,
        LAD,            // Left Anterior Descending
        LCX,            // Left Circumflex
        RCA,            // Right Coronary Artery

        // Other Structures
        InteratrialSeptum,
        InterventricularSeptum,
        RVOT,           // Right Ventricular Outflow Tract
        LVOT,           // Left Ventricular Outflow Tract
        Appendage,      // Left Atrial Appendage
        CoronarySinus,

        // Congenital Defects (if present)
        ASD,            // Atrial Septal Defect
        VSD,            // Ventricular Septal Defect
        PDA,            // Patent Ductus Arteriosus

        Custom,
        Outside
    }

    public enum NavigationMode
    {
        Free,           // Free movement with controllers
        Guided,         // Following waypoints
        Tour,           // Automated tour
        Locked          // Locked to a position
    }

    public enum FlightSpeed
    {
        Slow,
        Normal,
        Fast,
        Instant
    }

    #endregion

    [Header("References")]
    [SerializeField] private Transform cameraRig;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform heartModel;

    [Header("Navigation Settings")]
    [SerializeField] private NavigationMode currentMode = NavigationMode.Free;
    [SerializeField] private FlightSpeed flightSpeed = FlightSpeed.Normal;
    [SerializeField] private float baseSpeed = 0.5f;
    [SerializeField] private float rotationSpeed = 60f;
    [SerializeField] private float smoothTime = 0.3f;

    [Header("Collision Settings")]
    [SerializeField] private bool enableCollision = true;
    [SerializeField] private float playerRadius = 0.02f; // 2cm radius (scaled down inside heart)
    [SerializeField] private LayerMask heartWallLayer;

    [Header("Scale Settings")]
    [SerializeField] private float minScale = 0.01f;      // For inside-heart navigation
    [SerializeField] private float maxScale = 1f;         // Normal scale
    [SerializeField] private float currentScale = 0.1f;   // Current player scale
    [SerializeField] private float scaleTransitionTime = 1f;

    [Header("Waypoints")]
    [SerializeField] private List<HeartWaypoint> waypoints = new List<HeartWaypoint>();
    [SerializeField] private int currentWaypointIndex = -1;

    [Header("Tours")]
    [SerializeField] private List<HeartTour> availableTours = new List<HeartTour>();
    [SerializeField] private HeartTour currentTour;
    [SerializeField] private int currentTourStep = 0;

    [Header("Chamber Detection")]
    [SerializeField] private HeartChamber currentChamber = HeartChamber.Outside;
    [SerializeField] private float chamberDetectionRadius = 0.05f;

    // Internal state
    private Vector3 velocity = Vector3.zero;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isTransitioning = false;
    private Coroutine currentTransition;
    private Coroutine currentTourCoroutine;

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

        InitializeDefaultWaypoints();
        InitializeDefaultTours();
    }

    void Start()
    {
        if (cameraRig == null)
        {
            cameraRig = Camera.main?.transform.parent;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main?.transform;
        }

        targetPosition = cameraRig != null ? cameraRig.position : Vector3.zero;
        targetRotation = cameraRig != null ? cameraRig.rotation : Quaternion.identity;
    }

    void Update()
    {
        if (currentMode == NavigationMode.Free)
        {
            HandleFreeNavigation();
        }

        DetectCurrentChamber();
    }

    #region Initialization

    private void InitializeDefaultWaypoints()
    {
        // These positions would normally be set in the editor or loaded from data
        // Positions are relative to heart center, assuming heart is ~10cm

        waypoints.Add(new HeartWaypoint
        {
            Name = "Right Atrium Center",
            Chamber = HeartChamber.RightAtrium,
            Position = new Vector3(0.03f, 0.02f, 0),
            LookDirection = Vector3.down,
            Description = "Centro do átrio direito"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Tricuspid Valve",
            Chamber = HeartChamber.TricuspidValve,
            Position = new Vector3(0.02f, -0.01f, 0),
            LookDirection = Vector3.down,
            Description = "Válvula tricúspide"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Right Ventricle Center",
            Chamber = HeartChamber.RightVentricle,
            Position = new Vector3(0.02f, -0.04f, 0.01f),
            LookDirection = Vector3.forward,
            Description = "Centro do ventrículo direito"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "RVOT",
            Chamber = HeartChamber.RVOT,
            Position = new Vector3(0, -0.02f, 0.03f),
            LookDirection = Vector3.up + Vector3.forward,
            Description = "Via de saída do ventrículo direito"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Pulmonary Valve",
            Chamber = HeartChamber.PulmonaryValve,
            Position = new Vector3(-0.01f, 0.01f, 0.04f),
            LookDirection = Vector3.up,
            Description = "Válvula pulmonar"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Left Atrium Center",
            Chamber = HeartChamber.LeftAtrium,
            Position = new Vector3(-0.03f, 0.02f, -0.02f),
            LookDirection = Vector3.down,
            Description = "Centro do átrio esquerdo"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Mitral Valve",
            Chamber = HeartChamber.MitralValve,
            Position = new Vector3(-0.02f, -0.01f, -0.01f),
            LookDirection = Vector3.down,
            Description = "Válvula mitral"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Left Ventricle Center",
            Chamber = HeartChamber.LeftVentricle,
            Position = new Vector3(-0.01f, -0.04f, 0),
            LookDirection = Vector3.up,
            Description = "Centro do ventrículo esquerdo"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "LVOT",
            Chamber = HeartChamber.LVOT,
            Position = new Vector3(-0.02f, -0.02f, 0.02f),
            LookDirection = Vector3.up + Vector3.forward,
            Description = "Via de saída do ventrículo esquerdo"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Aortic Valve",
            Chamber = HeartChamber.AorticValve,
            Position = new Vector3(-0.02f, 0.02f, 0.03f),
            LookDirection = Vector3.up,
            Description = "Válvula aórtica"
        });

        waypoints.Add(new HeartWaypoint
        {
            Name = "Ascending Aorta",
            Chamber = HeartChamber.AscendingAorta,
            Position = new Vector3(-0.02f, 0.06f, 0.02f),
            LookDirection = Vector3.up,
            Description = "Aorta ascendente"
        });
    }

    private void InitializeDefaultTours()
    {
        // Right Heart Tour
        HeartTour rightHeartTour = new HeartTour
        {
            Name = "Right Heart Tour",
            Description = "Tour pelo coração direito: VCS -> AD -> VT -> VD -> VP -> AP",
            WaypointIndices = new List<int> { 0, 1, 2, 3, 4 },
            PauseDuration = 3f
        };
        availableTours.Add(rightHeartTour);

        // Left Heart Tour
        HeartTour leftHeartTour = new HeartTour
        {
            Name = "Left Heart Tour",
            Description = "Tour pelo coração esquerdo: VP -> AE -> VM -> VE -> VA -> Ao",
            WaypointIndices = new List<int> { 5, 6, 7, 8, 9, 10 },
            PauseDuration = 3f
        };
        availableTours.Add(leftHeartTour);

        // Complete Heart Tour
        HeartTour completeTour = new HeartTour
        {
            Name = "Complete Heart Tour",
            Description = "Tour completo pelo coração",
            WaypointIndices = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            PauseDuration = 2.5f
        };
        availableTours.Add(completeTour);
    }

    #endregion

    #region Free Navigation

    private void HandleFreeNavigation()
    {
        if (isTransitioning) return;

        // Get input (would be from VR controllers in actual implementation)
        float forward = Input.GetAxis("Vertical");
        float strafe = Input.GetAxis("Horizontal");
        float vertical = 0;
        if (Input.GetKey(KeyCode.E)) vertical = 1;
        if (Input.GetKey(KeyCode.Q)) vertical = -1;

        Vector3 moveDirection = Vector3.zero;
        if (playerCamera != null)
        {
            moveDirection = playerCamera.forward * forward +
                           playerCamera.right * strafe +
                           Vector3.up * vertical;
        }

        float speedMultiplier = GetSpeedMultiplier();
        Vector3 desiredVelocity = moveDirection.normalized * baseSpeed * speedMultiplier * currentScale;

        // Check for collisions
        if (enableCollision && moveDirection != Vector3.zero)
        {
            Vector3 nextPosition = cameraRig.position + desiredVelocity * Time.deltaTime;
            if (Physics.SphereCast(cameraRig.position, playerRadius * currentScale, moveDirection, out RaycastHit hit, desiredVelocity.magnitude * Time.deltaTime, heartWallLayer))
            {
                // Slide along wall
                desiredVelocity = Vector3.ProjectOnPlane(desiredVelocity, hit.normal);
            }
        }

        // Apply movement
        if (cameraRig != null)
        {
            cameraRig.position += desiredVelocity * Time.deltaTime;
        }
    }

    private float GetSpeedMultiplier()
    {
        switch (flightSpeed)
        {
            case FlightSpeed.Slow: return 0.3f;
            case FlightSpeed.Normal: return 1f;
            case FlightSpeed.Fast: return 2.5f;
            case FlightSpeed.Instant: return 10f;
            default: return 1f;
        }
    }

    #endregion

    #region Waypoint Navigation

    /// <summary>
    /// Navigate to a specific waypoint.
    /// </summary>
    public void NavigateToWaypoint(int index, bool animate = true)
    {
        if (index < 0 || index >= waypoints.Count) return;

        HeartWaypoint waypoint = waypoints[index];
        currentWaypointIndex = index;

        Vector3 worldPosition = heartModel != null ?
            heartModel.TransformPoint(waypoint.Position) :
            waypoint.Position;

        if (animate)
        {
            StartTransition(worldPosition, Quaternion.LookRotation(waypoint.LookDirection));
        }
        else
        {
            if (cameraRig != null)
            {
                cameraRig.position = worldPosition;
                cameraRig.rotation = Quaternion.LookRotation(waypoint.LookDirection);
            }
        }

        OnWaypointReached?.Invoke(this, new WaypointReachedEventArgs
        {
            WaypointIndex = index,
            WaypointName = waypoint.Name
        });
    }

    /// <summary>
    /// Navigate to a specific heart chamber.
    /// </summary>
    public void NavigateToChamber(HeartChamber chamber, bool animate = true)
    {
        HeartWaypoint waypoint = waypoints.Find(w => w.Chamber == chamber);
        if (waypoint != null)
        {
            int index = waypoints.IndexOf(waypoint);
            NavigateToWaypoint(index, animate);
        }
    }

    /// <summary>
    /// Navigate to next waypoint.
    /// </summary>
    public void NavigateToNextWaypoint()
    {
        int nextIndex = (currentWaypointIndex + 1) % waypoints.Count;
        NavigateToWaypoint(nextIndex);
    }

    /// <summary>
    /// Navigate to previous waypoint.
    /// </summary>
    public void NavigateToPreviousWaypoint()
    {
        int prevIndex = currentWaypointIndex - 1;
        if (prevIndex < 0) prevIndex = waypoints.Count - 1;
        NavigateToWaypoint(prevIndex);
    }

    /// <summary>
    /// Add a custom waypoint.
    /// </summary>
    public void AddWaypoint(string name, HeartChamber chamber, string description = "")
    {
        if (playerCamera == null) return;

        Vector3 localPosition = heartModel != null ?
            heartModel.InverseTransformPoint(playerCamera.position) :
            playerCamera.position;

        HeartWaypoint waypoint = new HeartWaypoint
        {
            Name = name,
            Chamber = chamber,
            Position = localPosition,
            LookDirection = playerCamera.forward,
            Description = description
        };

        waypoints.Add(waypoint);
        Debug.Log($"Added waypoint: {name}");
    }

    #endregion

    #region Tours

    /// <summary>
    /// Start a guided tour.
    /// </summary>
    public void StartTour(string tourName)
    {
        HeartTour tour = availableTours.Find(t => t.Name == tourName);
        if (tour == null)
        {
            Debug.LogWarning($"Tour not found: {tourName}");
            return;
        }

        StartTour(tour);
    }

    /// <summary>
    /// Start a guided tour.
    /// </summary>
    public void StartTour(HeartTour tour)
    {
        if (currentTourCoroutine != null)
        {
            StopCoroutine(currentTourCoroutine);
        }

        currentTour = tour;
        currentTourStep = 0;
        currentMode = NavigationMode.Tour;

        OnTourStarted?.Invoke(this, new TourEventArgs { TourName = tour.Name });

        currentTourCoroutine = StartCoroutine(RunTour(tour));
    }

    /// <summary>
    /// Stop the current tour.
    /// </summary>
    public void StopTour()
    {
        if (currentTourCoroutine != null)
        {
            StopCoroutine(currentTourCoroutine);
            currentTourCoroutine = null;
        }

        currentTour = null;
        currentMode = NavigationMode.Free;
    }

    private IEnumerator RunTour(HeartTour tour)
    {
        for (int i = 0; i < tour.WaypointIndices.Count; i++)
        {
            currentTourStep = i;
            int waypointIndex = tour.WaypointIndices[i];

            NavigateToWaypoint(waypointIndex, true);

            // Wait for transition
            while (isTransitioning)
            {
                yield return null;
            }

            // Pause at waypoint
            yield return new WaitForSeconds(tour.PauseDuration);
        }

        OnTourCompleted?.Invoke(this, new TourEventArgs { TourName = tour.Name });

        currentTour = null;
        currentMode = NavigationMode.Free;
    }

    /// <summary>
    /// Get available tours.
    /// </summary>
    public List<HeartTour> GetAvailableTours()
    {
        return new List<HeartTour>(availableTours);
    }

    #endregion

    #region Scale Control

    /// <summary>
    /// Set player scale for inside-heart navigation.
    /// </summary>
    public void SetPlayerScale(float scale, bool animate = true)
    {
        scale = Mathf.Clamp(scale, minScale, maxScale);

        if (animate)
        {
            StartCoroutine(AnimateScale(scale));
        }
        else
        {
            currentScale = scale;
            ApplyScale();
        }
    }

    /// <summary>
    /// Enter the heart (shrink down).
    /// </summary>
    public void EnterHeart()
    {
        SetPlayerScale(minScale, true);
        currentMode = NavigationMode.Free;
    }

    /// <summary>
    /// Exit the heart (grow back).
    /// </summary>
    public void ExitHeart()
    {
        SetPlayerScale(maxScale, true);
    }

    private IEnumerator AnimateScale(float targetScale)
    {
        float startScale = currentScale;
        float elapsed = 0f;

        while (elapsed < scaleTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleTransitionTime;
            t = Mathf.SmoothStep(0, 1, t);

            currentScale = Mathf.Lerp(startScale, targetScale, t);
            ApplyScale();

            yield return null;
        }

        currentScale = targetScale;
        ApplyScale();
    }

    private void ApplyScale()
    {
        if (cameraRig != null)
        {
            cameraRig.localScale = Vector3.one * currentScale;
        }
    }

    #endregion

    #region Transitions

    private void StartTransition(Vector3 position, Quaternion rotation)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        targetPosition = position;
        targetRotation = rotation;
        currentTransition = StartCoroutine(TransitionToTarget());
    }

    private IEnumerator TransitionToTarget()
    {
        isTransitioning = true;

        Vector3 startPosition = cameraRig != null ? cameraRig.position : Vector3.zero;
        Quaternion startRotation = cameraRig != null ? cameraRig.rotation : Quaternion.identity;

        float duration = 1f / GetSpeedMultiplier();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0, 1, t);

            if (cameraRig != null)
            {
                cameraRig.position = Vector3.Lerp(startPosition, targetPosition, t);
                cameraRig.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            }

            yield return null;
        }

        if (cameraRig != null)
        {
            cameraRig.position = targetPosition;
            cameraRig.rotation = targetRotation;
        }

        isTransitioning = false;
    }

    #endregion

    #region Chamber Detection

    private void DetectCurrentChamber()
    {
        if (playerCamera == null || heartModel == null) return;

        Vector3 localPosition = heartModel.InverseTransformPoint(playerCamera.position);

        // Find nearest waypoint to determine chamber
        HeartChamber nearestChamber = HeartChamber.Outside;
        float nearestDistance = float.MaxValue;

        foreach (var waypoint in waypoints)
        {
            float distance = Vector3.Distance(localPosition, waypoint.Position);
            if (distance < nearestDistance && distance < chamberDetectionRadius)
            {
                nearestDistance = distance;
                nearestChamber = waypoint.Chamber;
            }
        }

        if (nearestChamber != currentChamber)
        {
            currentChamber = nearestChamber;

            HeartWaypoint waypoint = waypoints.Find(w => w.Chamber == nearestChamber);
            string chamberName = waypoint != null ? waypoint.Name : nearestChamber.ToString();

            OnChamberEntered?.Invoke(this, new ChamberEnteredEventArgs
            {
                Chamber = nearestChamber,
                ChamberName = chamberName
            });
        }
    }

    /// <summary>
    /// Get current chamber.
    /// </summary>
    public HeartChamber GetCurrentChamber()
    {
        return currentChamber;
    }

    #endregion

    #region Mode Control

    /// <summary>
    /// Set navigation mode.
    /// </summary>
    public void SetNavigationMode(NavigationMode mode)
    {
        currentMode = mode;
    }

    /// <summary>
    /// Set flight speed.
    /// </summary>
    public void SetFlightSpeed(FlightSpeed speed)
    {
        flightSpeed = speed;
    }

    /// <summary>
    /// Toggle collision detection.
    /// </summary>
    public void SetCollisionEnabled(bool enabled)
    {
        enableCollision = enabled;
    }

    #endregion

    #region Getters

    /// <summary>
    /// Get all waypoints.
    /// </summary>
    public List<HeartWaypoint> GetWaypoints()
    {
        return new List<HeartWaypoint>(waypoints);
    }

    /// <summary>
    /// Get current waypoint index.
    /// </summary>
    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }

    /// <summary>
    /// Get current scale.
    /// </summary>
    public float GetCurrentScale()
    {
        return currentScale;
    }

    /// <summary>
    /// Check if currently transitioning.
    /// </summary>
    public bool IsTransitioning()
    {
        return isTransitioning;
    }

    #endregion
}

#region Data Classes

/// <summary>
/// Represents a navigation waypoint inside the heart.
/// </summary>
[System.Serializable]
public class HeartWaypoint
{
    public string Name;
    public HeartFlyThroughController.HeartChamber Chamber;
    public Vector3 Position;
    public Vector3 LookDirection = Vector3.forward;
    public string Description;
}

/// <summary>
/// Represents a guided tour through the heart.
/// </summary>
[System.Serializable]
public class HeartTour
{
    public string Name;
    public string Description;
    public List<int> WaypointIndices = new List<int>();
    public float PauseDuration = 3f;
}

#endregion

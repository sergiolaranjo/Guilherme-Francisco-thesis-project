using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// VR Vascular Clamp tool for clamping blood vessels during cardiac surgery.
/// Supports multiple clamp types and visual feedback.
/// </summary>
public class ClampTool : MonoBehaviour
{
    public enum ClampType
    {
        Bulldog,        // Small vessel clamp
        Satinsky,       // Partial occlusion clamp
        DeBakey,        // Aortic clamp
        CrossClamp,     // Complete occlusion
        SoftJaw         // Delicate tissue clamp
    }

    public enum ClampState
    {
        Open,
        Closing,
        Closed,
        Opening
    }

    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private InputActionReference gripAction;

    [Header("Clamp Settings")]
    [SerializeField] private ClampType clampType = ClampType.Bulldog;
    [SerializeField] private float clampOpenAngle = 45f;
    [SerializeField] private float clampCloseSpeed = 90f; // Degrees per second
    [SerializeField] private LayerMask clampableLayerMask;

    [Header("Clamp Parts")]
    [SerializeField] private Transform upperJaw;
    [SerializeField] private Transform lowerJaw;
    [SerializeField] private Transform jawPivot;

    [Header("Visual Feedback")]
    [SerializeField] private Material openMaterial;
    [SerializeField] private Material closedMaterial;
    [SerializeField] private GameObject clampIndicator;

    [Header("Audio")]
    [SerializeField] private AudioSource clampAudio;
    [SerializeField] private AudioClip clampCloseSound;
    [SerializeField] private AudioClip clampOpenSound;

    [Header("Haptics")]
    [SerializeField] private float clampHapticIntensity = 0.5f;

    private SurgerySimulator surgerySimulator;
    private ClampState currentState = ClampState.Open;
    private float currentJawAngle;
    private List<GameObject> placedClamps = new List<GameObject>();
    private bool isTriggerHeld = false;

    void Start()
    {
        surgerySimulator = SurgerySimulator.Instance;
        currentJawAngle = clampOpenAngle;
        UpdateJawPositions();
    }

    void OnEnable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.started += OnTriggerStarted;
            triggerAction.action.canceled += OnTriggerCanceled;
        }

        if (gripAction != null)
        {
            gripAction.action.Enable();
            gripAction.action.performed += OnGripPerformed;
        }
    }

    void OnDisable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.started -= OnTriggerStarted;
            triggerAction.action.canceled -= OnTriggerCanceled;
        }

        if (gripAction != null)
        {
            gripAction.action.performed -= OnGripPerformed;
        }
    }

    void Update()
    {
        UpdateClampAnimation();
        UpdateClampPreview();
    }

    private void OnTriggerStarted(InputAction.CallbackContext context)
    {
        isTriggerHeld = true;
        if (currentState == ClampState.Open)
        {
            StartClosing();
        }
    }

    private void OnTriggerCanceled(InputAction.CallbackContext context)
    {
        isTriggerHeld = false;
    }

    private void OnGripPerformed(InputAction.CallbackContext context)
    {
        // Grip releases/removes clamps
        if (currentState == ClampState.Closed)
        {
            StartOpening();
        }
        else
        {
            // Try to remove nearby placed clamp
            TryRemoveNearestClamp();
        }
    }

    private void StartClosing()
    {
        currentState = ClampState.Closing;
    }

    private void StartOpening()
    {
        currentState = ClampState.Opening;
    }

    private void UpdateClampAnimation()
    {
        float targetAngle = currentJawAngle;

        switch (currentState)
        {
            case ClampState.Closing:
                targetAngle = 0f;
                break;
            case ClampState.Opening:
                targetAngle = clampOpenAngle;
                break;
            case ClampState.Open:
                targetAngle = clampOpenAngle;
                break;
            case ClampState.Closed:
                targetAngle = 0f;
                break;
        }

        // Animate jaw angle
        if (!Mathf.Approximately(currentJawAngle, targetAngle))
        {
            float direction = Mathf.Sign(targetAngle - currentJawAngle);
            currentJawAngle += direction * clampCloseSpeed * Time.deltaTime;
            currentJawAngle = Mathf.Clamp(currentJawAngle, 0f, clampOpenAngle);

            UpdateJawPositions();

            // Check if animation complete
            if (Mathf.Approximately(currentJawAngle, 0f) && currentState == ClampState.Closing)
            {
                OnClampClosed();
            }
            else if (Mathf.Approximately(currentJawAngle, clampOpenAngle) && currentState == ClampState.Opening)
            {
                currentState = ClampState.Open;
                PlaySound(clampOpenSound);
            }
        }
    }

    private void UpdateJawPositions()
    {
        if (upperJaw != null)
        {
            upperJaw.localRotation = Quaternion.Euler(-currentJawAngle, 0, 0);
        }

        if (lowerJaw != null)
        {
            lowerJaw.localRotation = Quaternion.Euler(currentJawAngle, 0, 0);
        }
    }

    private void OnClampClosed()
    {
        currentState = ClampState.Closed;
        PlaySound(clampCloseSound);

        // Check if we're clamping something
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 0.05f, clampableLayerMask))
        {
            PlaceClamp(hit.point, transform.rotation);
        }
    }

    /// <summary>
    /// Place a clamp at the specified position.
    /// </summary>
    public void PlaceClamp(Vector3 position, Quaternion rotation)
    {
        if (!surgerySimulator.IsSurgeryActive())
        {
            Debug.LogWarning("Cannot place clamp: No active surgery session.");
            return;
        }

        GameObject clamp = surgerySimulator.ApplyClamp(position, rotation);

        if (clamp != null)
        {
            // Add clamp type component
            ClampMarker marker = clamp.AddComponent<ClampMarker>();
            marker.ClampType = clampType;
            marker.PlacementTime = Time.time;

            placedClamps.Add(clamp);

            Debug.Log($"{clampType} clamp placed at {position}");
        }
    }

    /// <summary>
    /// Try to remove the nearest placed clamp.
    /// </summary>
    private void TryRemoveNearestClamp()
    {
        if (placedClamps.Count == 0) return;

        float nearestDistance = float.MaxValue;
        GameObject nearestClamp = null;

        foreach (GameObject clamp in placedClamps)
        {
            if (clamp == null) continue;

            float distance = Vector3.Distance(transform.position, clamp.transform.position);
            if (distance < nearestDistance && distance < 0.1f)
            {
                nearestDistance = distance;
                nearestClamp = clamp;
            }
        }

        if (nearestClamp != null)
        {
            placedClamps.Remove(nearestClamp);
            Destroy(nearestClamp);
            PlaySound(clampOpenSound);
            Debug.Log("Clamp removed.");
        }
    }

    private void UpdateClampPreview()
    {
        if (clampIndicator == null) return;

        // Show indicator when clamp is open and pointing at clampable surface
        if (currentState == ClampState.Open)
        {
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 0.1f, clampableLayerMask))
            {
                clampIndicator.SetActive(true);
                clampIndicator.transform.position = hit.point;
                clampIndicator.transform.rotation = transform.rotation;
            }
            else
            {
                clampIndicator.SetActive(false);
            }
        }
        else
        {
            clampIndicator.SetActive(false);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clampAudio != null && clip != null)
        {
            clampAudio.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// Set the clamp type.
    /// </summary>
    public void SetClampType(ClampType type)
    {
        clampType = type;
        UpdateClampAppearance();
    }

    private void UpdateClampAppearance()
    {
        // Update visual appearance based on clamp type
        float jawSize = 1f;

        switch (clampType)
        {
            case ClampType.Bulldog:
                jawSize = 0.5f;
                break;
            case ClampType.Satinsky:
                jawSize = 0.8f;
                break;
            case ClampType.DeBakey:
                jawSize = 1.2f;
                break;
            case ClampType.CrossClamp:
                jawSize = 1.5f;
                break;
            case ClampType.SoftJaw:
                jawSize = 0.7f;
                break;
        }

        if (upperJaw != null)
        {
            upperJaw.localScale = new Vector3(1, jawSize, 1);
        }
        if (lowerJaw != null)
        {
            lowerJaw.localScale = new Vector3(1, jawSize, 1);
        }
    }

    /// <summary>
    /// Get current clamp state.
    /// </summary>
    public ClampState GetState()
    {
        return currentState;
    }

    /// <summary>
    /// Get list of placed clamps.
    /// </summary>
    public List<GameObject> GetPlacedClamps()
    {
        // Clean up null references
        placedClamps.RemoveAll(c => c == null);
        return new List<GameObject>(placedClamps);
    }

    /// <summary>
    /// Force open the clamp.
    /// </summary>
    public void ForceOpen()
    {
        currentState = ClampState.Opening;
    }

    /// <summary>
    /// Force close the clamp.
    /// </summary>
    public void ForceClose()
    {
        currentState = ClampState.Closing;
    }
}

/// <summary>
/// Component attached to placed clamps for identification.
/// </summary>
public class ClampMarker : MonoBehaviour
{
    public ClampTool.ClampType ClampType;
    public float PlacementTime;
}

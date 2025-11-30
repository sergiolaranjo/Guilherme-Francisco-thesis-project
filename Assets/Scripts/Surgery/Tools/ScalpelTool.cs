using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// VR Scalpel tool for making incisions on cardiac tissue.
/// Tracks controller movement to create incision lines.
/// </summary>
public class ScalpelTool : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private InputActionReference gripAction;

    [Header("Settings")]
    [SerializeField] private float minIncisionDistance = 0.001f;
    [SerializeField] private float maxIncisionLength = 0.1f;
    [SerializeField] private LayerMask incisionLayerMask;

    [Header("Visual Feedback")]
    [SerializeField] private LineRenderer previewLine;
    [SerializeField] private ParticleSystem incisionParticles;
    [SerializeField] private AudioSource incisionAudio;

    [Header("Haptics")]
    [SerializeField] private float hapticIntensity = 0.3f;
    [SerializeField] private float hapticDuration = 0.05f;

    private SurgerySimulator surgerySimulator;
    private bool isIncising = false;
    private Vector3 incisionStartPoint;
    private List<Vector3> incisionPoints = new List<Vector3>();
    private float lastHapticTime;

    void Start()
    {
        surgerySimulator = SurgerySimulator.Instance;

        if (previewLine == null)
        {
            previewLine = gameObject.AddComponent<LineRenderer>();
            previewLine.startWidth = 0.001f;
            previewLine.endWidth = 0.001f;
            previewLine.material = new Material(Shader.Find("Sprites/Default"));
            previewLine.startColor = Color.red;
            previewLine.endColor = Color.red;
            previewLine.enabled = false;
        }
    }

    void OnEnable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.started += OnTriggerStarted;
            triggerAction.action.canceled += OnTriggerCanceled;
        }
    }

    void OnDisable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.started -= OnTriggerStarted;
            triggerAction.action.canceled -= OnTriggerCanceled;
        }

        EndIncision();
    }

    void Update()
    {
        if (isIncising)
        {
            UpdateIncision();
        }
    }

    private void OnTriggerStarted(InputAction.CallbackContext context)
    {
        StartIncision();
    }

    private void OnTriggerCanceled(InputAction.CallbackContext context)
    {
        EndIncision();
    }

    /// <summary>
    /// Start making an incision.
    /// </summary>
    public void StartIncision()
    {
        if (!surgerySimulator.IsSurgeryActive())
        {
            Debug.LogWarning("Cannot incise: No active surgery session.");
            return;
        }

        // Raycast to find surface
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 0.1f, incisionLayerMask))
        {
            isIncising = true;
            incisionStartPoint = hit.point;
            incisionPoints.Clear();
            incisionPoints.Add(hit.point);

            previewLine.enabled = true;
            previewLine.positionCount = 1;
            previewLine.SetPosition(0, hit.point);

            if (incisionParticles != null)
            {
                incisionParticles.transform.position = hit.point;
                incisionParticles.Play();
            }

            if (incisionAudio != null)
            {
                incisionAudio.Play();
            }

            TriggerHaptic();
        }
    }

    /// <summary>
    /// Update incision during drawing.
    /// </summary>
    private void UpdateIncision()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 0.1f, incisionLayerMask))
        {
            Vector3 lastPoint = incisionPoints[incisionPoints.Count - 1];
            float distance = Vector3.Distance(hit.point, lastPoint);

            if (distance >= minIncisionDistance)
            {
                // Check total length
                float totalLength = CalculateTotalLength();
                if (totalLength + distance > maxIncisionLength)
                {
                    EndIncision();
                    return;
                }

                incisionPoints.Add(hit.point);

                // Update preview
                previewLine.positionCount = incisionPoints.Count;
                previewLine.SetPositions(incisionPoints.ToArray());

                // Update particles
                if (incisionParticles != null)
                {
                    incisionParticles.transform.position = hit.point;
                }

                // Haptic feedback at intervals
                if (Time.time - lastHapticTime > 0.1f)
                {
                    TriggerHaptic();
                    lastHapticTime = Time.time;
                }
            }
        }
    }

    /// <summary>
    /// End the incision and create the surgical element.
    /// </summary>
    public void EndIncision()
    {
        if (!isIncising) return;

        isIncising = false;
        previewLine.enabled = false;

        if (incisionParticles != null)
        {
            incisionParticles.Stop();
        }

        if (incisionAudio != null)
        {
            incisionAudio.Stop();
        }

        // Create incision if we have enough points
        if (incisionPoints.Count >= 2)
        {
            Vector3 start = incisionPoints[0];
            Vector3 end = incisionPoints[incisionPoints.Count - 1];

            // Calculate average normal from incision path
            Vector3 normal = Vector3.up; // Default
            if (incisionPoints.Count >= 3)
            {
                Vector3 v1 = incisionPoints[1] - incisionPoints[0];
                Vector3 v2 = incisionPoints[2] - incisionPoints[1];
                normal = Vector3.Cross(v1, v2).normalized;
            }

            surgerySimulator.PerformIncision(start, end, normal);

            Debug.Log($"Incision created: {CalculateTotalLength() * 1000:F1}mm");
        }

        incisionPoints.Clear();
    }

    private float CalculateTotalLength()
    {
        float length = 0f;
        for (int i = 1; i < incisionPoints.Count; i++)
        {
            length += Vector3.Distance(incisionPoints[i], incisionPoints[i - 1]);
        }
        return length;
    }

    private void TriggerHaptic()
    {
        // Haptic feedback would be implemented via XR Interaction Toolkit
        // This is a placeholder for the haptic system
    }

    /// <summary>
    /// Cancel the current incision without creating it.
    /// </summary>
    public void CancelIncision()
    {
        isIncising = false;
        previewLine.enabled = false;
        incisionPoints.Clear();

        if (incisionParticles != null) incisionParticles.Stop();
        if (incisionAudio != null) incisionAudio.Stop();
    }
}

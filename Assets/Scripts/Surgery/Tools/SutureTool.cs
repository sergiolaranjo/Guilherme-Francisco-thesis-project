using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// VR Suture tool for placing surgical sutures.
/// Supports both individual sutures and running suture lines.
/// </summary>
public class SutureTool : MonoBehaviour
{
    public enum SutureMode
    {
        SingleStitch,
        RunningStitch,
        FigureEight,
        Interrupted
    }

    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private InputActionReference gripAction;
    [SerializeField] private InputActionReference primaryButtonAction;

    [Header("Settings")]
    [SerializeField] private SutureMode currentMode = SutureMode.SingleStitch;
    [SerializeField] private float sutureLength = 0.008f;
    [SerializeField] private float minStitchSpacing = 0.003f;
    [SerializeField] private LayerMask sutureLayerMask;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject suturePreviewPrefab;
    [SerializeField] private LineRenderer runningPreviewLine;
    [SerializeField] private AudioSource sutureAudio;

    [Header("Needle Settings")]
    [SerializeField] private Transform needleTip;
    [SerializeField] private float needleRadius = 0.005f;

    private SurgerySimulator surgerySimulator;
    private List<Vector3> runningStitchPoints = new List<Vector3>();
    private GameObject currentPreview;
    private bool isRunningMode = false;

    void Start()
    {
        surgerySimulator = SurgerySimulator.Instance;

        if (needleTip == null)
        {
            needleTip = transform;
        }

        SetupPreviewLine();
    }

    void OnEnable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTriggerPerformed;
        }

        if (primaryButtonAction != null)
        {
            primaryButtonAction.action.Enable();
            primaryButtonAction.action.performed += OnPrimaryButtonPerformed;
        }
    }

    void OnDisable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.performed -= OnTriggerPerformed;
        }

        if (primaryButtonAction != null)
        {
            primaryButtonAction.action.performed -= OnPrimaryButtonPerformed;
        }

        EndRunningStitch();
    }

    void Update()
    {
        UpdatePreview();
    }

    private void SetupPreviewLine()
    {
        if (runningPreviewLine == null)
        {
            GameObject lineObj = new GameObject("SuturePreviewLine");
            lineObj.transform.SetParent(transform);
            runningPreviewLine = lineObj.AddComponent<LineRenderer>();
            runningPreviewLine.startWidth = 0.0005f;
            runningPreviewLine.endWidth = 0.0005f;
            runningPreviewLine.material = new Material(Shader.Find("Sprites/Default"));
            runningPreviewLine.startColor = new Color(0.2f, 0.2f, 0.8f, 0.5f);
            runningPreviewLine.endColor = new Color(0.2f, 0.2f, 0.8f, 0.5f);
            runningPreviewLine.enabled = false;
        }
    }

    private void OnTriggerPerformed(InputAction.CallbackContext context)
    {
        PlaceStitch();
    }

    private void OnPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        // Toggle running stitch mode or finish current running stitch
        if (isRunningMode)
        {
            FinishRunningStitch();
        }
        else
        {
            // Cycle through modes
            currentMode = (SutureMode)(((int)currentMode + 1) % System.Enum.GetValues(typeof(SutureMode)).Length);
            Debug.Log($"Suture mode: {currentMode}");
        }
    }

    /// <summary>
    /// Place a stitch at the current position.
    /// </summary>
    public void PlaceStitch()
    {
        if (!surgerySimulator.IsSurgeryActive())
        {
            Debug.LogWarning("Cannot suture: No active surgery session.");
            return;
        }

        // Raycast to find surface
        if (Physics.Raycast(needleTip.position, needleTip.forward, out RaycastHit hit, 0.05f, sutureLayerMask))
        {
            Vector3 stitchPosition = hit.point;
            Vector3 stitchDirection = Vector3.Cross(hit.normal, needleTip.right).normalized;

            switch (currentMode)
            {
                case SutureMode.SingleStitch:
                    PlaceSingleStitch(stitchPosition, stitchDirection);
                    break;

                case SutureMode.RunningStitch:
                    AddRunningStitchPoint(stitchPosition);
                    break;

                case SutureMode.FigureEight:
                    PlaceFigureEightStitch(stitchPosition, stitchDirection, hit.normal);
                    break;

                case SutureMode.Interrupted:
                    PlaceInterruptedStitch(stitchPosition, stitchDirection);
                    break;
            }

            PlaySutureSound();
        }
    }

    private void PlaceSingleStitch(Vector3 position, Vector3 direction)
    {
        surgerySimulator.PlaceSuture(position, direction, sutureLength);
        Debug.Log("Single stitch placed.");
    }

    private void AddRunningStitchPoint(Vector3 position)
    {
        // Check minimum spacing
        if (runningStitchPoints.Count > 0)
        {
            float distance = Vector3.Distance(position, runningStitchPoints[runningStitchPoints.Count - 1]);
            if (distance < minStitchSpacing)
            {
                Debug.Log("Stitch point too close to previous.");
                return;
            }
        }

        runningStitchPoints.Add(position);
        isRunningMode = true;

        // Update preview
        runningPreviewLine.enabled = true;
        runningPreviewLine.positionCount = runningStitchPoints.Count;
        runningPreviewLine.SetPositions(runningStitchPoints.ToArray());

        Debug.Log($"Running stitch point {runningStitchPoints.Count} added.");
    }

    private void FinishRunningStitch()
    {
        if (runningStitchPoints.Count >= 2)
        {
            surgerySimulator.CreateRunningSuture(runningStitchPoints.ToArray());
            Debug.Log($"Running suture completed with {runningStitchPoints.Count} points.");
        }

        EndRunningStitch();
    }

    private void EndRunningStitch()
    {
        runningStitchPoints.Clear();
        isRunningMode = false;
        runningPreviewLine.enabled = false;
    }

    private void PlaceFigureEightStitch(Vector3 position, Vector3 direction, Vector3 normal)
    {
        // Figure-8 stitch creates a crossing pattern
        Vector3 perpendicular = Vector3.Cross(direction, normal).normalized;

        Vector3[] points = new Vector3[5];
        float offset = sutureLength * 0.5f;

        points[0] = position - direction * offset - perpendicular * offset;
        points[1] = position + direction * offset + perpendicular * offset;
        points[2] = position; // Center crossing
        points[3] = position - direction * offset + perpendicular * offset;
        points[4] = position + direction * offset - perpendicular * offset;

        surgerySimulator.CreateRunningSuture(points);
        Debug.Log("Figure-8 stitch placed.");
    }

    private void PlaceInterruptedStitch(Vector3 position, Vector3 direction)
    {
        // Interrupted stitch with visible knot
        surgerySimulator.PlaceSuture(position, direction, sutureLength);

        // Add a marker for the knot
        surgerySimulator.PlaceMarker(position + direction * sutureLength * 0.5f,
            new Color(0.2f, 0.2f, 0.8f), "Knot");

        Debug.Log("Interrupted stitch with knot placed.");
    }

    private void UpdatePreview()
    {
        // Show preview of where stitch will be placed
        if (Physics.Raycast(needleTip.position, needleTip.forward, out RaycastHit hit, 0.05f, sutureLayerMask))
        {
            if (currentPreview == null && suturePreviewPrefab != null)
            {
                currentPreview = Instantiate(suturePreviewPrefab);
            }

            if (currentPreview != null)
            {
                currentPreview.SetActive(true);
                currentPreview.transform.position = hit.point;
                currentPreview.transform.forward = Vector3.Cross(hit.normal, needleTip.right).normalized;
            }

            // Update running stitch preview
            if (isRunningMode && runningStitchPoints.Count > 0)
            {
                runningPreviewLine.positionCount = runningStitchPoints.Count + 1;
                runningPreviewLine.SetPositions(runningStitchPoints.ToArray());
                runningPreviewLine.SetPosition(runningStitchPoints.Count, hit.point);
            }
        }
        else
        {
            if (currentPreview != null)
            {
                currentPreview.SetActive(false);
            }
        }
    }

    private void PlaySutureSound()
    {
        if (sutureAudio != null)
        {
            sutureAudio.pitch = Random.Range(0.9f, 1.1f);
            sutureAudio.PlayOneShot(sutureAudio.clip);
        }
    }

    /// <summary>
    /// Set the suture mode.
    /// </summary>
    public void SetMode(SutureMode mode)
    {
        if (isRunningMode && mode != SutureMode.RunningStitch)
        {
            FinishRunningStitch();
        }
        currentMode = mode;
    }

    /// <summary>
    /// Get the current suture mode.
    /// </summary>
    public SutureMode GetMode()
    {
        return currentMode;
    }

    /// <summary>
    /// Cancel the current running stitch without completing it.
    /// </summary>
    public void CancelRunningStitch()
    {
        EndRunningStitch();
    }
}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// VR Ablation catheter tool for cardiac ablation procedures.
/// Simulates radiofrequency or cryoablation on cardiac tissue.
/// </summary>
public class AblationTool : MonoBehaviour
{
    public enum AblationType
    {
        Radiofrequency,
        Cryo,
        Laser
    }

    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private InputActionReference gripAction;

    [Header("Ablation Settings")]
    [SerializeField] private AblationType ablationType = AblationType.Radiofrequency;
    [SerializeField] private float ablationRadius = 0.003f;
    [SerializeField] private float ablationDuration = 2.0f;
    [SerializeField] private float minAblationSpacing = 0.004f;
    [SerializeField] private LayerMask ablationLayerMask;

    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem ablationParticles;
    [SerializeField] private Light ablationLight;
    [SerializeField] private GameObject catheterTip;
    [SerializeField] private Material activeMaterial;
    [SerializeField] private Material inactiveMaterial;

    [Header("Audio")]
    [SerializeField] private AudioSource ablationAudio;
    [SerializeField] private AudioClip rfAblationSound;
    [SerializeField] private AudioClip cryoAblationSound;
    [SerializeField] private AudioClip laserAblationSound;

    [Header("Temperature Display")]
    [SerializeField] private TMPro.TextMeshProUGUI temperatureDisplay;

    private SurgerySimulator surgerySimulator;
    private bool isAblating = false;
    private float ablationTimer = 0f;
    private Vector3 lastAblationPosition;
    private Coroutine ablationCoroutine;
    private MeshRenderer tipRenderer;

    void Start()
    {
        surgerySimulator = SurgerySimulator.Instance;

        if (catheterTip != null)
        {
            tipRenderer = catheterTip.GetComponent<MeshRenderer>();
        }

        SetupVisuals();
        UpdateTypeVisuals();
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

        StopAblation();
    }

    private void SetupVisuals()
    {
        if (ablationParticles == null)
        {
            GameObject particleObj = new GameObject("AblationParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.forward * 0.01f;

            ablationParticles = particleObj.AddComponent<ParticleSystem>();
            var main = ablationParticles.main;
            main.startSize = ablationRadius;
            main.startLifetime = 0.3f;
            main.startSpeed = 0.01f;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ablationParticles.emission;
            emission.rateOverTime = 30;
            emission.enabled = false;

            var shape = ablationParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = ablationRadius * 0.5f;
        }

        if (ablationLight == null)
        {
            GameObject lightObj = new GameObject("AblationLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.forward * 0.01f;

            ablationLight = lightObj.AddComponent<Light>();
            ablationLight.type = LightType.Point;
            ablationLight.range = 0.05f;
            ablationLight.intensity = 0;
        }
    }

    private void UpdateTypeVisuals()
    {
        Color particleColor;
        Color lightColor;

        switch (ablationType)
        {
            case AblationType.Radiofrequency:
                particleColor = new Color(1f, 0.3f, 0.1f, 0.6f);
                lightColor = new Color(1f, 0.5f, 0.2f);
                break;
            case AblationType.Cryo:
                particleColor = new Color(0.3f, 0.7f, 1f, 0.6f);
                lightColor = new Color(0.5f, 0.8f, 1f);
                break;
            case AblationType.Laser:
                particleColor = new Color(0.1f, 1f, 0.1f, 0.6f);
                lightColor = new Color(0.2f, 1f, 0.2f);
                break;
            default:
                particleColor = Color.white;
                lightColor = Color.white;
                break;
        }

        if (ablationParticles != null)
        {
            var main = ablationParticles.main;
            main.startColor = particleColor;
        }

        if (ablationLight != null)
        {
            ablationLight.color = lightColor;
        }
    }

    private void OnTriggerStarted(InputAction.CallbackContext context)
    {
        StartAblation();
    }

    private void OnTriggerCanceled(InputAction.CallbackContext context)
    {
        StopAblation();
    }

    /// <summary>
    /// Start ablation at current position.
    /// </summary>
    public void StartAblation()
    {
        if (!surgerySimulator.IsSurgeryActive())
        {
            Debug.LogWarning("Cannot ablate: No active surgery session.");
            return;
        }

        if (isAblating) return;

        // Check if we're touching tissue
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 0.02f, ablationLayerMask))
        {
            // Check spacing from last ablation
            if (Vector3.Distance(hit.point, lastAblationPosition) < minAblationSpacing)
            {
                Debug.Log("Too close to previous ablation site.");
                return;
            }

            isAblating = true;
            ablationTimer = 0f;
            lastAblationPosition = hit.point;

            // Start visual effects
            if (ablationParticles != null)
            {
                ablationParticles.transform.position = hit.point;
                var emission = ablationParticles.emission;
                emission.enabled = true;
                ablationParticles.Play();
            }

            if (tipRenderer != null && activeMaterial != null)
            {
                tipRenderer.material = activeMaterial;
            }

            // Play sound
            PlayAblationSound();

            // Start ablation coroutine
            ablationCoroutine = StartCoroutine(AblationProcess(hit.point));
        }
    }

    /// <summary>
    /// Stop current ablation.
    /// </summary>
    public void StopAblation()
    {
        if (!isAblating) return;

        isAblating = false;

        if (ablationCoroutine != null)
        {
            StopCoroutine(ablationCoroutine);
            ablationCoroutine = null;
        }

        // Stop visual effects
        if (ablationParticles != null)
        {
            var emission = ablationParticles.emission;
            emission.enabled = false;
            ablationParticles.Stop();
        }

        if (ablationLight != null)
        {
            ablationLight.intensity = 0;
        }

        if (tipRenderer != null && inactiveMaterial != null)
        {
            tipRenderer.material = inactiveMaterial;
        }

        if (ablationAudio != null)
        {
            ablationAudio.Stop();
        }

        if (temperatureDisplay != null)
        {
            temperatureDisplay.text = "";
        }
    }

    private IEnumerator AblationProcess(Vector3 position)
    {
        float targetTemperature = GetTargetTemperature();
        float currentTemperature = 37f; // Body temperature

        while (isAblating && ablationTimer < ablationDuration)
        {
            ablationTimer += Time.deltaTime;
            float progress = ablationTimer / ablationDuration;

            // Simulate temperature change
            currentTemperature = Mathf.Lerp(37f, targetTemperature, progress);

            // Update visual feedback
            if (ablationLight != null)
            {
                ablationLight.intensity = progress * 2f;
            }

            // Update temperature display
            if (temperatureDisplay != null)
            {
                temperatureDisplay.text = $"{currentTemperature:F1}°C";
            }

            yield return null;
        }

        if (isAblating && ablationTimer >= ablationDuration)
        {
            // Ablation complete - create permanent marker
            CompleteAblation(position);
        }

        StopAblation();
    }

    private void CompleteAblation(Vector3 position)
    {
        // Create ablation lesion
        surgerySimulator.PerformAblation(position, ablationRadius);

        Debug.Log($"{ablationType} ablation completed at {position}");
    }

    private float GetTargetTemperature()
    {
        switch (ablationType)
        {
            case AblationType.Radiofrequency:
                return 60f; // RF ablation typically reaches 50-70°C
            case AblationType.Cryo:
                return -40f; // Cryoablation typically reaches -40 to -80°C
            case AblationType.Laser:
                return 80f; // Laser ablation can reach higher temperatures
            default:
                return 60f;
        }
    }

    private void PlayAblationSound()
    {
        if (ablationAudio == null) return;

        AudioClip clip = null;
        switch (ablationType)
        {
            case AblationType.Radiofrequency:
                clip = rfAblationSound;
                break;
            case AblationType.Cryo:
                clip = cryoAblationSound;
                break;
            case AblationType.Laser:
                clip = laserAblationSound;
                break;
        }

        if (clip != null)
        {
            ablationAudio.clip = clip;
            ablationAudio.loop = true;
            ablationAudio.Play();
        }
    }

    /// <summary>
    /// Set the ablation type.
    /// </summary>
    public void SetAblationType(AblationType type)
    {
        ablationType = type;
        UpdateTypeVisuals();
    }

    /// <summary>
    /// Get the current ablation type.
    /// </summary>
    public AblationType GetAblationType()
    {
        return ablationType;
    }

    /// <summary>
    /// Set the ablation radius.
    /// </summary>
    public void SetAblationRadius(float radius)
    {
        ablationRadius = Mathf.Clamp(radius, 0.001f, 0.01f);
    }

    /// <summary>
    /// Get current ablation progress (0-1).
    /// </summary>
    public float GetAblationProgress()
    {
        if (!isAblating) return 0f;
        return Mathf.Clamp01(ablationTimer / ablationDuration);
    }

    /// <summary>
    /// Check if currently ablating.
    /// </summary>
    public bool IsAblating()
    {
        return isAblating;
    }
}

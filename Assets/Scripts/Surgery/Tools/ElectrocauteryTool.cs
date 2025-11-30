using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// VR Electrocautery tool for cutting and coagulating tissue.
/// Combines cutting and hemostasis functions.
/// </summary>
public class ElectrocauteryTool : MonoBehaviour
{
    public enum CauteryMode
    {
        Cut,        // High power, focused - for cutting tissue
        Coagulate,  // Lower power, spread - for stopping bleeding
        Blend       // Combination of cut and coagulate
    }

    [Header("Input")]
    [SerializeField] private InputActionReference triggerAction;
    [SerializeField] private InputActionReference gripAction;
    [SerializeField] private InputActionReference primaryButtonAction;

    [Header("Settings")]
    [SerializeField] private CauteryMode currentMode = CauteryMode.Cut;
    [SerializeField] private float powerLevel = 0.5f;
    [SerializeField] private float effectRadius = 0.002f;
    [SerializeField] private LayerMask tissueLayerMask;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem sparkParticles;
    [SerializeField] private ParticleSystem smokeParticles;
    [SerializeField] private Light cauteryLight;
    [SerializeField] private LineRenderer arcRenderer;
    [SerializeField] private Transform electrodeTip;

    [Header("Audio")]
    [SerializeField] private AudioSource cauteryAudio;
    [SerializeField] private AudioClip cutSound;
    [SerializeField] private AudioClip coagSound;

    [Header("Tissue Effects")]
    [SerializeField] private Material charredMaterial;
    [SerializeField] private float charDistance = 0.001f;

    private SurgerySimulator surgerySimulator;
    private bool isActive = false;
    private Vector3 lastPosition;
    private float activationTime;

    void Start()
    {
        surgerySimulator = SurgerySimulator.Instance;

        if (electrodeTip == null)
        {
            electrodeTip = transform;
        }

        SetupEffects();
        UpdateModeVisuals();
    }

    void OnEnable()
    {
        if (triggerAction != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.started += OnTriggerStarted;
            triggerAction.action.canceled += OnTriggerCanceled;
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
            triggerAction.action.started -= OnTriggerStarted;
            triggerAction.action.canceled -= OnTriggerCanceled;
        }

        if (primaryButtonAction != null)
        {
            primaryButtonAction.action.performed -= OnPrimaryButtonPerformed;
        }

        Deactivate();
    }

    void Update()
    {
        if (isActive)
        {
            UpdateCauteryEffect();
        }
    }

    private void SetupEffects()
    {
        if (sparkParticles == null)
        {
            GameObject sparkObj = new GameObject("SparkParticles");
            sparkObj.transform.SetParent(electrodeTip);
            sparkObj.transform.localPosition = Vector3.zero;

            sparkParticles = sparkObj.AddComponent<ParticleSystem>();
            var main = sparkParticles.main;
            main.startSize = 0.002f;
            main.startLifetime = 0.1f;
            main.startSpeed = 0.5f;
            main.startColor = new Color(1f, 0.8f, 0.2f);
            main.maxParticles = 100;

            var emission = sparkParticles.emission;
            emission.rateOverTime = 50;
            emission.enabled = false;

            var shape = sparkParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.001f;
        }

        if (smokeParticles == null)
        {
            GameObject smokeObj = new GameObject("SmokeParticles");
            smokeObj.transform.SetParent(electrodeTip);
            smokeObj.transform.localPosition = Vector3.zero;

            smokeParticles = smokeObj.AddComponent<ParticleSystem>();
            var main = smokeParticles.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.005f, 0.02f);
            main.startLifetime = 1.5f;
            main.startSpeed = 0.02f;
            main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            main.maxParticles = 50;
            main.gravityModifier = -0.1f;

            var emission = smokeParticles.emission;
            emission.rateOverTime = 10;
            emission.enabled = false;

            var sizeOverLifetime = smokeParticles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 2f);

            var colorOverLifetime = smokeParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.gray, 0f), new GradientColorKey(Color.gray, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.3f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = gradient;
        }

        if (cauteryLight == null)
        {
            GameObject lightObj = new GameObject("CauteryLight");
            lightObj.transform.SetParent(electrodeTip);
            lightObj.transform.localPosition = Vector3.zero;

            cauteryLight = lightObj.AddComponent<Light>();
            cauteryLight.type = LightType.Point;
            cauteryLight.range = 0.05f;
            cauteryLight.intensity = 0;
            cauteryLight.color = new Color(1f, 0.8f, 0.2f);
        }

        if (arcRenderer == null)
        {
            GameObject arcObj = new GameObject("ElectricArc");
            arcObj.transform.SetParent(electrodeTip);

            arcRenderer = arcObj.AddComponent<LineRenderer>();
            arcRenderer.startWidth = 0.001f;
            arcRenderer.endWidth = 0.0005f;
            arcRenderer.material = new Material(Shader.Find("Sprites/Default"));
            arcRenderer.startColor = new Color(0.5f, 0.8f, 1f);
            arcRenderer.endColor = new Color(1f, 1f, 1f);
            arcRenderer.positionCount = 2;
            arcRenderer.enabled = false;
        }
    }

    private void OnTriggerStarted(InputAction.CallbackContext context)
    {
        Activate();
    }

    private void OnTriggerCanceled(InputAction.CallbackContext context)
    {
        Deactivate();
    }

    private void OnPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        // Cycle through modes
        CycleMode();
    }

    /// <summary>
    /// Activate the electrocautery.
    /// </summary>
    public void Activate()
    {
        if (!surgerySimulator.IsSurgeryActive())
        {
            Debug.LogWarning("Cannot use electrocautery: No active surgery session.");
            return;
        }

        isActive = true;
        activationTime = Time.time;
        lastPosition = electrodeTip.position;

        // Start effects
        if (sparkParticles != null)
        {
            var emission = sparkParticles.emission;
            emission.enabled = true;
            sparkParticles.Play();
        }

        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.enabled = true;
            smokeParticles.Play();
        }

        if (cauteryLight != null)
        {
            cauteryLight.intensity = powerLevel * 3f;
        }

        PlayModeSound();
    }

    /// <summary>
    /// Deactivate the electrocautery.
    /// </summary>
    public void Deactivate()
    {
        isActive = false;

        if (sparkParticles != null)
        {
            var emission = sparkParticles.emission;
            emission.enabled = false;
            sparkParticles.Stop();
        }

        if (smokeParticles != null)
        {
            var emission = smokeParticles.emission;
            emission.enabled = false;
            smokeParticles.Stop();
        }

        if (cauteryLight != null)
        {
            cauteryLight.intensity = 0;
        }

        if (arcRenderer != null)
        {
            arcRenderer.enabled = false;
        }

        if (cauteryAudio != null)
        {
            cauteryAudio.Stop();
        }
    }

    private void UpdateCauteryEffect()
    {
        // Raycast to find tissue
        if (Physics.Raycast(electrodeTip.position, electrodeTip.forward, out RaycastHit hit, 0.02f, tissueLayerMask))
        {
            // Update arc effect
            if (arcRenderer != null)
            {
                arcRenderer.enabled = true;
                arcRenderer.SetPosition(0, electrodeTip.position);
                arcRenderer.SetPosition(1, hit.point);

                // Add some randomness to arc
                Vector3 midPoint = (electrodeTip.position + hit.point) / 2f;
                midPoint += Random.insideUnitSphere * 0.002f;
            }

            // Move particles to contact point
            if (sparkParticles != null)
            {
                sparkParticles.transform.position = hit.point;
            }
            if (smokeParticles != null)
            {
                smokeParticles.transform.position = hit.point;
            }

            // Apply cautery effect based on mode
            ApplyCauteryEffect(hit.point, hit.normal);

            // Flickering light effect
            if (cauteryLight != null)
            {
                cauteryLight.intensity = powerLevel * 3f * Random.Range(0.8f, 1.2f);
                cauteryLight.transform.position = hit.point;
            }
        }
        else
        {
            if (arcRenderer != null)
            {
                arcRenderer.enabled = false;
            }
        }
    }

    private void ApplyCauteryEffect(Vector3 position, Vector3 normal)
    {
        float distance = Vector3.Distance(position, lastPosition);

        // Only apply effect if we've moved enough
        if (distance > charDistance)
        {
            switch (currentMode)
            {
                case CauteryMode.Cut:
                    // Create incision effect
                    if (distance > charDistance * 2)
                    {
                        surgerySimulator.PerformIncision(lastPosition, position, normal);
                    }
                    break;

                case CauteryMode.Coagulate:
                    // Create coagulation marker
                    surgerySimulator.PlaceMarker(position, new Color(0.4f, 0.2f, 0.1f), "Coagulation");
                    break;

                case CauteryMode.Blend:
                    // Combination effect
                    if (distance > charDistance * 3)
                    {
                        surgerySimulator.PerformIncision(lastPosition, position, normal);
                    }
                    surgerySimulator.PlaceMarker(position, new Color(0.3f, 0.15f, 0.1f), "Cauterized");
                    break;
            }

            lastPosition = position;
        }
    }

    private void CycleMode()
    {
        currentMode = (CauteryMode)(((int)currentMode + 1) % System.Enum.GetValues(typeof(CauteryMode)).Length);
        UpdateModeVisuals();
        Debug.Log($"Electrocautery mode: {currentMode}");
    }

    private void UpdateModeVisuals()
    {
        Color modeColor;

        switch (currentMode)
        {
            case CauteryMode.Cut:
                modeColor = new Color(1f, 0.9f, 0.2f); // Yellow
                break;
            case CauteryMode.Coagulate:
                modeColor = new Color(0.2f, 0.5f, 1f); // Blue
                break;
            case CauteryMode.Blend:
                modeColor = new Color(0.2f, 1f, 0.2f); // Green
                break;
            default:
                modeColor = Color.white;
                break;
        }

        if (sparkParticles != null)
        {
            var main = sparkParticles.main;
            main.startColor = modeColor;
        }

        if (arcRenderer != null)
        {
            arcRenderer.startColor = modeColor;
        }
    }

    private void PlayModeSound()
    {
        if (cauteryAudio == null) return;

        AudioClip clip = null;

        switch (currentMode)
        {
            case CauteryMode.Cut:
                clip = cutSound;
                break;
            case CauteryMode.Coagulate:
                clip = coagSound;
                break;
            case CauteryMode.Blend:
                clip = cutSound; // Use cut sound for blend mode
                break;
        }

        if (clip != null)
        {
            cauteryAudio.clip = clip;
            cauteryAudio.loop = true;
            cauteryAudio.Play();
        }
    }

    /// <summary>
    /// Set the cautery mode.
    /// </summary>
    public void SetMode(CauteryMode mode)
    {
        currentMode = mode;
        UpdateModeVisuals();

        if (isActive)
        {
            PlayModeSound();
        }
    }

    /// <summary>
    /// Get the current cautery mode.
    /// </summary>
    public CauteryMode GetMode()
    {
        return currentMode;
    }

    /// <summary>
    /// Set the power level (0-1).
    /// </summary>
    public void SetPowerLevel(float level)
    {
        powerLevel = Mathf.Clamp01(level);
    }

    /// <summary>
    /// Get the current power level.
    /// </summary>
    public float GetPowerLevel()
    {
        return powerLevel;
    }

    /// <summary>
    /// Check if the electrocautery is active.
    /// </summary>
    public bool IsActive()
    {
        return isActive;
    }
}

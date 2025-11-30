using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI Controller for AR/MR settings panel.
/// Provides controls for passthrough, opacity, occlusion, and environment settings.
/// </summary>
public class ARSettingsUI : MonoBehaviour
{
    public static ARSettingsUI Instance { get; private set; }

    public event EventHandler<ModeToggledEventArgs> OnModeToggled;

    public class ModeToggledEventArgs : EventArgs
    {
        public MixedRealityManager.RealityMode Mode;
    }

    [Header("Panel")]
    [SerializeField] private GameObject arSettingsPanel;

    [Header("Mode Selection")]
    [SerializeField] private Button vrModeButton;
    [SerializeField] private Button arModeButton;
    [SerializeField] private Button mrModeButton;
    [SerializeField] private Toggle modeToggle;
    [SerializeField] private TextMeshProUGUI currentModeText;

    [Header("Passthrough Settings")]
    [SerializeField] private Slider passthroughOpacitySlider;
    [SerializeField] private TextMeshProUGUI passthroughOpacityText;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TextMeshProUGUI brightnessText;
    [SerializeField] private Slider contrastSlider;
    [SerializeField] private TextMeshProUGUI contrastText;

    [Header("Model Settings")]
    [SerializeField] private Slider modelOpacitySlider;
    [SerializeField] private TextMeshProUGUI modelOpacityText;

    [Header("Occlusion Settings")]
    [SerializeField] private Toggle depthOcclusionToggle;
    [SerializeField] private Toggle handOcclusionToggle;

    [Header("Environment")]
    [SerializeField] private Toggle environmentLightingToggle;
    [SerializeField] private Slider lightIntensitySlider;
    [SerializeField] private TextMeshProUGUI lightIntensityText;

    [Header("Visual Feedback")]
    [SerializeField] private Image vrButtonHighlight;
    [SerializeField] private Image arButtonHighlight;
    [SerializeField] private Image mrButtonHighlight;
    [SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Status")]
    [SerializeField] private TextMeshProUGUI passthroughStatusText;
    [SerializeField] private Image passthroughSupportedIndicator;

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

        SetupUIListeners();
        SetupSliderRanges();

        if (mrManager != null)
        {
            mrManager.OnModeChanged += OnMixedRealityModeChanged;
            mrManager.OnPassthroughSettingsChanged += OnPassthroughSettingsUpdated;

            UpdateUI();
            UpdatePassthroughStatus();
        }
    }

    void OnDestroy()
    {
        if (mrManager != null)
        {
            mrManager.OnModeChanged -= OnMixedRealityModeChanged;
            mrManager.OnPassthroughSettingsChanged -= OnPassthroughSettingsUpdated;
        }
    }

    private void SetupUIListeners()
    {
        // Mode buttons
        if (vrModeButton != null)
            vrModeButton.onClick.AddListener(() => SetMode(MixedRealityManager.RealityMode.VirtualReality));

        if (arModeButton != null)
            arModeButton.onClick.AddListener(() => SetMode(MixedRealityManager.RealityMode.AugmentedReality));

        if (mrModeButton != null)
            mrModeButton.onClick.AddListener(() => SetMode(MixedRealityManager.RealityMode.MixedReality));

        if (modeToggle != null)
            modeToggle.onValueChanged.AddListener(OnModeToggleChanged);

        // Passthrough sliders
        if (passthroughOpacitySlider != null)
            passthroughOpacitySlider.onValueChanged.AddListener(OnPassthroughOpacityChanged);

        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);

        if (contrastSlider != null)
            contrastSlider.onValueChanged.AddListener(OnContrastChanged);

        // Model settings
        if (modelOpacitySlider != null)
            modelOpacitySlider.onValueChanged.AddListener(OnModelOpacityChanged);

        // Occlusion toggles
        if (depthOcclusionToggle != null)
            depthOcclusionToggle.onValueChanged.AddListener(OnDepthOcclusionChanged);

        if (handOcclusionToggle != null)
            handOcclusionToggle.onValueChanged.AddListener(OnHandOcclusionChanged);

        // Environment
        if (environmentLightingToggle != null)
            environmentLightingToggle.onValueChanged.AddListener(OnEnvironmentLightingChanged);

        if (lightIntensitySlider != null)
            lightIntensitySlider.onValueChanged.AddListener(OnLightIntensityChanged);
    }

    private void SetupSliderRanges()
    {
        if (passthroughOpacitySlider != null)
        {
            passthroughOpacitySlider.minValue = 0f;
            passthroughOpacitySlider.maxValue = 1f;
            passthroughOpacitySlider.value = 1f;
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = -1f;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.value = 0f;
        }

        if (contrastSlider != null)
        {
            contrastSlider.minValue = -1f;
            contrastSlider.maxValue = 1f;
            contrastSlider.value = 0f;
        }

        if (modelOpacitySlider != null)
        {
            modelOpacitySlider.minValue = 0f;
            modelOpacitySlider.maxValue = 1f;
            modelOpacitySlider.value = 1f;
        }

        if (lightIntensitySlider != null)
        {
            lightIntensitySlider.minValue = 0f;
            lightIntensitySlider.maxValue = 2f;
            lightIntensitySlider.value = 1f;
        }
    }

    #region Mode Control

    /// <summary>
    /// Set the reality mode.
    /// </summary>
    public void SetMode(MixedRealityManager.RealityMode mode)
    {
        if (mrManager != null)
        {
            mrManager.SetMode(mode);
        }

        OnModeToggled?.Invoke(this, new ModeToggledEventArgs { Mode = mode });
    }

    /// <summary>
    /// Toggle between VR and AR modes.
    /// </summary>
    public void ToggleMode()
    {
        if (mrManager != null)
        {
            mrManager.ToggleMode();
        }
    }

    private void OnModeToggleChanged(bool isAR)
    {
        if (mrManager != null)
        {
            if (isAR)
            {
                mrManager.SetMode(MixedRealityManager.RealityMode.AugmentedReality);
            }
            else
            {
                mrManager.SetMode(MixedRealityManager.RealityMode.VirtualReality);
            }
        }
    }

    #endregion

    #region Event Handlers

    private void OnMixedRealityModeChanged(object sender, MixedRealityManager.ModeChangedEventArgs e)
    {
        UpdateUI();
    }

    private void OnPassthroughSettingsUpdated(object sender, MixedRealityManager.PassthroughSettingsChangedEventArgs e)
    {
        UpdatePassthroughUI(e.Opacity, e.Brightness, e.Contrast);
    }

    private void OnPassthroughOpacityChanged(float value)
    {
        if (mrManager != null)
        {
            mrManager.SetPassthroughOpacity(value);
        }
        UpdatePassthroughOpacityText(value);
    }

    private void OnBrightnessChanged(float value)
    {
        if (mrManager != null)
        {
            mrManager.SetPassthroughBrightness(value);
        }
        UpdateBrightnessText(value);
    }

    private void OnContrastChanged(float value)
    {
        if (mrManager != null)
        {
            mrManager.SetPassthroughContrast(value);
        }
        UpdateContrastText(value);
    }

    private void OnModelOpacityChanged(float value)
    {
        if (mrManager != null)
        {
            mrManager.SetModelOpacity(value);
        }
        UpdateModelOpacityText(value);
    }

    private void OnDepthOcclusionChanged(bool enabled)
    {
        // Depth occlusion setting is handled by MixedRealityManager
        Debug.Log($"Depth occlusion: {enabled}");
    }

    private void OnHandOcclusionChanged(bool enabled)
    {
        // Hand occlusion setting is handled by MixedRealityManager
        Debug.Log($"Hand occlusion: {enabled}");
    }

    private void OnEnvironmentLightingChanged(bool enabled)
    {
        Debug.Log($"Environment lighting: {enabled}");
    }

    private void OnLightIntensityChanged(float value)
    {
        if (lightIntensityText != null)
        {
            lightIntensityText.text = $"{value:F1}";
        }
    }

    #endregion

    #region UI Updates

    private void UpdateUI()
    {
        if (mrManager == null) return;

        MixedRealityManager.RealityMode currentMode = mrManager.GetCurrentMode();

        // Update mode text
        if (currentModeText != null)
        {
            currentModeText.text = currentMode.ToString();
        }

        // Update toggle
        if (modeToggle != null)
        {
            modeToggle.SetIsOnWithoutNotify(mrManager.IsARMode());
        }

        // Update button highlights
        UpdateModeButtonHighlights(currentMode);

        // Enable/disable AR-specific controls
        bool isAR = mrManager.IsARMode();
        SetARControlsInteractable(isAR);

        // Update slider values
        if (passthroughOpacitySlider != null)
        {
            passthroughOpacitySlider.SetValueWithoutNotify(mrManager.GetPassthroughOpacity());
            UpdatePassthroughOpacityText(mrManager.GetPassthroughOpacity());
        }

        if (modelOpacitySlider != null)
        {
            modelOpacitySlider.SetValueWithoutNotify(mrManager.GetModelOpacity());
            UpdateModelOpacityText(mrManager.GetModelOpacity());
        }
    }

    private void UpdateModeButtonHighlights(MixedRealityManager.RealityMode mode)
    {
        if (vrButtonHighlight != null)
            vrButtonHighlight.color = mode == MixedRealityManager.RealityMode.VirtualReality ? activeColor : inactiveColor;

        if (arButtonHighlight != null)
            arButtonHighlight.color = mode == MixedRealityManager.RealityMode.AugmentedReality ? activeColor : inactiveColor;

        if (mrButtonHighlight != null)
            mrButtonHighlight.color = mode == MixedRealityManager.RealityMode.MixedReality ? activeColor : inactiveColor;
    }

    private void SetARControlsInteractable(bool interactable)
    {
        if (passthroughOpacitySlider != null)
            passthroughOpacitySlider.interactable = interactable;

        if (brightnessSlider != null)
            brightnessSlider.interactable = interactable;

        if (contrastSlider != null)
            contrastSlider.interactable = interactable;

        if (modelOpacitySlider != null)
            modelOpacitySlider.interactable = interactable;

        if (depthOcclusionToggle != null)
            depthOcclusionToggle.interactable = interactable;

        if (handOcclusionToggle != null)
            handOcclusionToggle.interactable = interactable;
    }

    private void UpdatePassthroughStatus()
    {
        if (mrManager == null) return;

        bool supported = mrManager.IsPassthroughSupported();

        if (passthroughStatusText != null)
        {
            passthroughStatusText.text = supported ? "Passthrough Supported" : "Passthrough Not Available";
        }

        if (passthroughSupportedIndicator != null)
        {
            passthroughSupportedIndicator.color = supported ? Color.green : Color.red;
        }

        // Disable AR/MR buttons if not supported
        if (!supported)
        {
            if (arModeButton != null) arModeButton.interactable = false;
            if (mrModeButton != null) mrModeButton.interactable = false;
            if (modeToggle != null) modeToggle.interactable = false;
        }
    }

    private void UpdatePassthroughUI(float opacity, float brightness, float contrast)
    {
        if (passthroughOpacitySlider != null)
            passthroughOpacitySlider.SetValueWithoutNotify(opacity);

        if (brightnessSlider != null)
            brightnessSlider.SetValueWithoutNotify(brightness);

        if (contrastSlider != null)
            contrastSlider.SetValueWithoutNotify(contrast);

        UpdatePassthroughOpacityText(opacity);
        UpdateBrightnessText(brightness);
        UpdateContrastText(contrast);
    }

    private void UpdatePassthroughOpacityText(float value)
    {
        if (passthroughOpacityText != null)
        {
            passthroughOpacityText.text = $"{value * 100:F0}%";
        }
    }

    private void UpdateBrightnessText(float value)
    {
        if (brightnessText != null)
        {
            brightnessText.text = $"{value:F2}";
        }
    }

    private void UpdateContrastText(float value)
    {
        if (contrastText != null)
        {
            contrastText.text = $"{value:F2}";
        }
    }

    private void UpdateModelOpacityText(float value)
    {
        if (modelOpacityText != null)
        {
            modelOpacityText.text = $"{value * 100:F0}%";
        }
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// Show the AR settings panel.
    /// </summary>
    public void Show()
    {
        if (arSettingsPanel != null)
        {
            arSettingsPanel.SetActive(true);
        }
        UpdateUI();
    }

    /// <summary>
    /// Hide the AR settings panel.
    /// </summary>
    public void Hide()
    {
        if (arSettingsPanel != null)
        {
            arSettingsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle panel visibility.
    /// </summary>
    public void Toggle()
    {
        if (arSettingsPanel != null)
        {
            if (arSettingsPanel.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }

    #endregion
}

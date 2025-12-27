using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// UI Controller for volume rendering settings.
/// Provides controls for visualization presets, depth layers, and medical viewing angles.
/// </summary>
public class VolumeRenderingUI : MonoBehaviour
{
    public static VolumeRenderingUI Instance { get; private set; }

    public event EventHandler<PresetSelectedEventArgs> OnPresetSelected;
    public event EventHandler<ViewAngleSelectedEventArgs> OnViewAngleSelected;
    public event EventHandler<DepthChangedEventArgs> OnDepthChanged;

    public class PresetSelectedEventArgs : EventArgs
    {
        public VolumeRenderingController.VisualizationPreset Preset;
    }

    public class ViewAngleSelectedEventArgs : EventArgs
    {
        public VolumeRenderingController.MedicalViewAngle Angle;
    }

    public class DepthChangedEventArgs : EventArgs
    {
        public float Depth;
        public int Layer;
    }

    [Header("Panel")]
    [SerializeField] private GameObject volumeRenderingPanel;

    [Header("Tab Navigation")]
    [SerializeField] private Button presetsTabButton;
    [SerializeField] private Button viewsTabButton;
    [SerializeField] private Button depthTabButton;
    [SerializeField] private Button settingsTabButton;
    [SerializeField] private GameObject presetsPanel;
    [SerializeField] private GameObject viewsPanel;
    [SerializeField] private GameObject depthPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Color activeTabColor = new Color(0.2f, 0.6f, 0.9f);
    [SerializeField] private Color inactiveTabColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Preset Buttons - General")]
    [SerializeField] private Button presetDefaultButton;
    [SerializeField] private Button presetCustomButton;

    [Header("Preset Buttons - Bone")]
    [SerializeField] private Button presetBoneButton;
    [SerializeField] private Button presetSkullButton;
    [SerializeField] private Button presetSpineButton;

    [Header("Preset Buttons - Soft Tissue")]
    [SerializeField] private Button presetSoftTissueButton;
    [SerializeField] private Button presetMuscleButton;
    [SerializeField] private Button presetFatButton;

    [Header("Preset Buttons - Vascular")]
    [SerializeField] private Button presetBloodButton;
    [SerializeField] private Button presetVesselsButton;
    [SerializeField] private Button presetAngiographyButton;

    [Header("Preset Buttons - Cardiac")]
    [SerializeField] private Button presetCardiacButton;
    [SerializeField] private Button presetCardiacChambersButton;
    [SerializeField] private Button presetCardiacWallButton;
    [SerializeField] private Button presetCoronariesButton;
    [SerializeField] private Button presetAortaButton;
    [SerializeField] private Button presetValvesButton;

    [Header("Preset Buttons - Pulmonary")]
    [SerializeField] private Button presetLungButton;
    [SerializeField] private Button presetAirwaysButton;
    [SerializeField] private Button presetBronchiButton;

    [Header("Preset Buttons - Special")]
    [SerializeField] private Button presetMIPButton;
    [SerializeField] private Button presetXRayButton;

    [Header("View Angle Buttons - Standard")]
    [SerializeField] private Button viewAnteriorButton;
    [SerializeField] private Button viewPosteriorButton;
    [SerializeField] private Button viewLeftLateralButton;
    [SerializeField] private Button viewRightLateralButton;
    [SerializeField] private Button viewSuperiorButton;
    [SerializeField] private Button viewInferiorButton;

    [Header("View Angle Buttons - LAO")]
    [SerializeField] private Button viewLAO30Button;
    [SerializeField] private Button viewLAO45Button;
    [SerializeField] private Button viewLAO60Button;
    [SerializeField] private Button viewLAOCranialButton;
    [SerializeField] private Button viewLAOCaudalButton;

    [Header("View Angle Buttons - RAO")]
    [SerializeField] private Button viewRAO30Button;
    [SerializeField] private Button viewRAO45Button;
    [SerializeField] private Button viewRAOCranialButton;
    [SerializeField] private Button viewRAOCaudalButton;

    [Header("View Angle Buttons - Special")]
    [SerializeField] private Button viewSpiderButton;
    [SerializeField] private Button viewHepatoclavicularButton;

    [Header("Custom View Controls")]
    [SerializeField] private Slider lateralAngleSlider;
    [SerializeField] private Slider cranialCaudalSlider;
    [SerializeField] private TextMeshProUGUI lateralAngleText;
    [SerializeField] private TextMeshProUGUI cranialCaudalText;
    [SerializeField] private Button applyCustomViewButton;

    [Header("Depth Layer Controls")]
    [SerializeField] private Toggle depthLayerToggle;
    [SerializeField] private Slider depthSlider;
    [SerializeField] private TextMeshProUGUI depthValueText;
    [SerializeField] private TextMeshProUGUI layerText;
    [SerializeField] private Button previousLayerButton;
    [SerializeField] private Button nextLayerButton;
    [SerializeField] private Slider layerThicknessSlider;
    [SerializeField] private TextMeshProUGUI layerThicknessText;
    [SerializeField] private Toggle animateDepthToggle;
    [SerializeField] private Slider animationSpeedSlider;
    [SerializeField] private TextMeshProUGUI animationSpeedText;

    [Header("Settings Controls")]
    [SerializeField] private Slider windowCenterSlider;
    [SerializeField] private Slider windowWidthSlider;
    [SerializeField] private TextMeshProUGUI windowCenterText;
    [SerializeField] private TextMeshProUGUI windowWidthText;
    [SerializeField] private Slider viewDistanceSlider;
    [SerializeField] private TextMeshProUGUI viewDistanceText;
    [SerializeField] private Slider qualitySlider;
    [SerializeField] private TextMeshProUGUI qualityText;

    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI currentPresetText;
    [SerializeField] private TextMeshProUGUI currentViewText;
    [SerializeField] private TextMeshProUGUI presetDescriptionText;
    [SerializeField] private TextMeshProUGUI viewDescriptionText;

    [Header("Quick Access Buttons")]
    [SerializeField] private Button resetViewButton;
    [SerializeField] private Button resetPresetsButton;

    private VolumeRenderingController renderingController;
    private VolumeRenderer volumeRenderer;
    private int activeTab = 0;

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
        renderingController = VolumeRenderingController.Instance;
        volumeRenderer = VolumeRenderer.Instance;

        SetupTabNavigation();
        SetupPresetButtons();
        SetupViewAngleButtons();
        SetupDepthControls();
        SetupSettingsControls();
        SetupQuickAccessButtons();

        if (renderingController != null)
        {
            renderingController.OnPresetChanged += OnPresetChangedHandler;
            renderingController.OnViewAngleChanged += OnViewAngleChangedHandler;
            renderingController.OnDepthLayerChanged += OnDepthLayerChangedHandler;
        }

        UpdateUI();
        ShowTab(0);
    }

    void OnDestroy()
    {
        if (renderingController != null)
        {
            renderingController.OnPresetChanged -= OnPresetChangedHandler;
            renderingController.OnViewAngleChanged -= OnViewAngleChangedHandler;
            renderingController.OnDepthLayerChanged -= OnDepthLayerChangedHandler;
        }
    }

    #region Tab Navigation

    private void SetupTabNavigation()
    {
        if (presetsTabButton != null)
            presetsTabButton.onClick.AddListener(() => ShowTab(0));

        if (viewsTabButton != null)
            viewsTabButton.onClick.AddListener(() => ShowTab(1));

        if (depthTabButton != null)
            depthTabButton.onClick.AddListener(() => ShowTab(2));

        if (settingsTabButton != null)
            settingsTabButton.onClick.AddListener(() => ShowTab(3));
    }

    private void ShowTab(int tabIndex)
    {
        activeTab = tabIndex;

        // Update panels
        if (presetsPanel != null) presetsPanel.SetActive(tabIndex == 0);
        if (viewsPanel != null) viewsPanel.SetActive(tabIndex == 1);
        if (depthPanel != null) depthPanel.SetActive(tabIndex == 2);
        if (settingsPanel != null) settingsPanel.SetActive(tabIndex == 3);

        // Update tab button colors
        UpdateTabButton(presetsTabButton, tabIndex == 0);
        UpdateTabButton(viewsTabButton, tabIndex == 1);
        UpdateTabButton(depthTabButton, tabIndex == 2);
        UpdateTabButton(settingsTabButton, tabIndex == 3);
    }

    private void UpdateTabButton(Button button, bool active)
    {
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = active ? activeTabColor : inactiveTabColor;
        }
    }

    #endregion

    #region Preset Buttons Setup

    private void SetupPresetButtons()
    {
        // General
        SetupPresetButton(presetDefaultButton, VolumeRenderingController.VisualizationPreset.Default);
        SetupPresetButton(presetCustomButton, VolumeRenderingController.VisualizationPreset.Custom);

        // Bone
        SetupPresetButton(presetBoneButton, VolumeRenderingController.VisualizationPreset.Bone);
        SetupPresetButton(presetSkullButton, VolumeRenderingController.VisualizationPreset.Skull);
        SetupPresetButton(presetSpineButton, VolumeRenderingController.VisualizationPreset.Spine);

        // Soft Tissue
        SetupPresetButton(presetSoftTissueButton, VolumeRenderingController.VisualizationPreset.SoftTissue);
        SetupPresetButton(presetMuscleButton, VolumeRenderingController.VisualizationPreset.Muscle);
        SetupPresetButton(presetFatButton, VolumeRenderingController.VisualizationPreset.Fat);

        // Vascular
        SetupPresetButton(presetBloodButton, VolumeRenderingController.VisualizationPreset.Blood);
        SetupPresetButton(presetVesselsButton, VolumeRenderingController.VisualizationPreset.Vessels);
        SetupPresetButton(presetAngiographyButton, VolumeRenderingController.VisualizationPreset.Angiography);

        // Cardiac
        SetupPresetButton(presetCardiacButton, VolumeRenderingController.VisualizationPreset.Cardiac);
        SetupPresetButton(presetCardiacChambersButton, VolumeRenderingController.VisualizationPreset.CardiacChambers);
        SetupPresetButton(presetCardiacWallButton, VolumeRenderingController.VisualizationPreset.CardiacWall);
        SetupPresetButton(presetCoronariesButton, VolumeRenderingController.VisualizationPreset.Coronaries);
        SetupPresetButton(presetAortaButton, VolumeRenderingController.VisualizationPreset.Aorta);
        SetupPresetButton(presetValvesButton, VolumeRenderingController.VisualizationPreset.Valves);

        // Pulmonary
        SetupPresetButton(presetLungButton, VolumeRenderingController.VisualizationPreset.Lung);
        SetupPresetButton(presetAirwaysButton, VolumeRenderingController.VisualizationPreset.Airways);
        SetupPresetButton(presetBronchiButton, VolumeRenderingController.VisualizationPreset.Bronchi);

        // Special
        SetupPresetButton(presetMIPButton, VolumeRenderingController.VisualizationPreset.MIP);
        SetupPresetButton(presetXRayButton, VolumeRenderingController.VisualizationPreset.XRay);
    }

    private void SetupPresetButton(Button button, VolumeRenderingController.VisualizationPreset preset)
    {
        if (button == null) return;

        button.onClick.AddListener(() => SelectPreset(preset));
    }

    private void SelectPreset(VolumeRenderingController.VisualizationPreset preset)
    {
        if (renderingController != null)
        {
            renderingController.ApplyPreset(preset);
        }

        OnPresetSelected?.Invoke(this, new PresetSelectedEventArgs { Preset = preset });
    }

    #endregion

    #region View Angle Buttons Setup

    private void SetupViewAngleButtons()
    {
        // Standard views
        SetupViewButton(viewAnteriorButton, VolumeRenderingController.MedicalViewAngle.Anterior);
        SetupViewButton(viewPosteriorButton, VolumeRenderingController.MedicalViewAngle.Posterior);
        SetupViewButton(viewLeftLateralButton, VolumeRenderingController.MedicalViewAngle.LeftLateral);
        SetupViewButton(viewRightLateralButton, VolumeRenderingController.MedicalViewAngle.RightLateral);
        SetupViewButton(viewSuperiorButton, VolumeRenderingController.MedicalViewAngle.Superior);
        SetupViewButton(viewInferiorButton, VolumeRenderingController.MedicalViewAngle.Inferior);

        // LAO views
        SetupViewButton(viewLAO30Button, VolumeRenderingController.MedicalViewAngle.LAO_30);
        SetupViewButton(viewLAO45Button, VolumeRenderingController.MedicalViewAngle.LAO_45);
        SetupViewButton(viewLAO60Button, VolumeRenderingController.MedicalViewAngle.LAO_60);
        SetupViewButton(viewLAOCranialButton, VolumeRenderingController.MedicalViewAngle.LAO_Cranial);
        SetupViewButton(viewLAOCaudalButton, VolumeRenderingController.MedicalViewAngle.LAO_Caudal);

        // RAO views
        SetupViewButton(viewRAO30Button, VolumeRenderingController.MedicalViewAngle.RAO_30);
        SetupViewButton(viewRAO45Button, VolumeRenderingController.MedicalViewAngle.RAO_45);
        SetupViewButton(viewRAOCranialButton, VolumeRenderingController.MedicalViewAngle.RAO_Cranial);
        SetupViewButton(viewRAOCaudalButton, VolumeRenderingController.MedicalViewAngle.RAO_Caudal);

        // Special views
        SetupViewButton(viewSpiderButton, VolumeRenderingController.MedicalViewAngle.Spider);
        SetupViewButton(viewHepatoclavicularButton, VolumeRenderingController.MedicalViewAngle.Hepatoclavicular);

        // Custom view controls
        if (lateralAngleSlider != null)
        {
            lateralAngleSlider.minValue = -90f;
            lateralAngleSlider.maxValue = 90f;
            lateralAngleSlider.value = 0f;
            lateralAngleSlider.onValueChanged.AddListener(OnLateralAngleChanged);
        }

        if (cranialCaudalSlider != null)
        {
            cranialCaudalSlider.minValue = -45f;
            cranialCaudalSlider.maxValue = 45f;
            cranialCaudalSlider.value = 0f;
            cranialCaudalSlider.onValueChanged.AddListener(OnCranialCaudalChanged);
        }

        if (applyCustomViewButton != null)
        {
            applyCustomViewButton.onClick.AddListener(ApplyCustomView);
        }
    }

    private void SetupViewButton(Button button, VolumeRenderingController.MedicalViewAngle angle)
    {
        if (button == null) return;

        button.onClick.AddListener(() => SelectViewAngle(angle));
    }

    private void SelectViewAngle(VolumeRenderingController.MedicalViewAngle angle)
    {
        if (renderingController != null)
        {
            renderingController.SetViewAngle(angle, true);
        }

        OnViewAngleSelected?.Invoke(this, new ViewAngleSelectedEventArgs { Angle = angle });
    }

    private void OnLateralAngleChanged(float value)
    {
        if (lateralAngleText != null)
        {
            string direction = value >= 0 ? "LAO" : "RAO";
            lateralAngleText.text = $"{direction} {Mathf.Abs(value):F0}\u00B0";
        }
    }

    private void OnCranialCaudalChanged(float value)
    {
        if (cranialCaudalText != null)
        {
            string direction = value >= 0 ? "Cranial" : "Caudal";
            cranialCaudalText.text = $"{direction} {Mathf.Abs(value):F0}\u00B0";
        }
    }

    private void ApplyCustomView()
    {
        if (renderingController != null && lateralAngleSlider != null && cranialCaudalSlider != null)
        {
            renderingController.SetCustomViewAngle(lateralAngleSlider.value, cranialCaudalSlider.value);
        }
    }

    #endregion

    #region Depth Controls Setup

    private void SetupDepthControls()
    {
        if (depthLayerToggle != null)
        {
            depthLayerToggle.onValueChanged.AddListener(OnDepthLayerToggleChanged);
        }

        if (depthSlider != null)
        {
            depthSlider.minValue = 0f;
            depthSlider.maxValue = 1f;
            depthSlider.value = 0f;
            depthSlider.onValueChanged.AddListener(OnDepthSliderChanged);
        }

        if (previousLayerButton != null)
        {
            previousLayerButton.onClick.AddListener(OnPreviousLayer);
        }

        if (nextLayerButton != null)
        {
            nextLayerButton.onClick.AddListener(OnNextLayer);
        }

        if (layerThicknessSlider != null)
        {
            layerThicknessSlider.minValue = 0.01f;
            layerThicknessSlider.maxValue = 0.5f;
            layerThicknessSlider.value = 0.1f;
            layerThicknessSlider.onValueChanged.AddListener(OnLayerThicknessChanged);
        }

        if (animateDepthToggle != null)
        {
            animateDepthToggle.onValueChanged.AddListener(OnAnimateDepthToggleChanged);
        }

        if (animationSpeedSlider != null)
        {
            animationSpeedSlider.minValue = 0.05f;
            animationSpeedSlider.maxValue = 1f;
            animationSpeedSlider.value = 0.2f;
            animationSpeedSlider.onValueChanged.AddListener(OnAnimationSpeedChanged);
        }
    }

    private void OnDepthLayerToggleChanged(bool enabled)
    {
        if (renderingController != null)
        {
            renderingController.SetDepthLayerMode(enabled);
        }

        // Enable/disable depth controls
        if (depthSlider != null) depthSlider.interactable = enabled;
        if (previousLayerButton != null) previousLayerButton.interactable = enabled;
        if (nextLayerButton != null) nextLayerButton.interactable = enabled;
        if (layerThicknessSlider != null) layerThicknessSlider.interactable = enabled;
        if (animateDepthToggle != null) animateDepthToggle.interactable = enabled;
    }

    private void OnDepthSliderChanged(float value)
    {
        if (renderingController != null)
        {
            renderingController.SetDepth(value);
        }

        UpdateDepthUI(value);

        OnDepthChanged?.Invoke(this, new DepthChangedEventArgs
        {
            Depth = value,
            Layer = renderingController != null ? renderingController.GetCurrentLayer() : 0
        });
    }

    private void OnPreviousLayer()
    {
        if (renderingController != null)
        {
            renderingController.PreviousLayer();
            if (depthSlider != null)
            {
                depthSlider.SetValueWithoutNotify(renderingController.GetCurrentDepth());
            }
        }
    }

    private void OnNextLayer()
    {
        if (renderingController != null)
        {
            renderingController.NextLayer();
            if (depthSlider != null)
            {
                depthSlider.SetValueWithoutNotify(renderingController.GetCurrentDepth());
            }
        }
    }

    private void OnLayerThicknessChanged(float value)
    {
        if (renderingController != null)
        {
            renderingController.SetLayerThickness(value);
        }

        if (layerThicknessText != null)
        {
            layerThicknessText.text = $"{value * 100:F0}%";
        }
    }

    private void OnAnimateDepthToggleChanged(bool enabled)
    {
        if (renderingController != null)
        {
            renderingController.SetDepthAnimation(enabled);
        }

        if (animationSpeedSlider != null)
        {
            animationSpeedSlider.interactable = enabled;
        }
    }

    private void OnAnimationSpeedChanged(float value)
    {
        if (animationSpeedText != null)
        {
            animationSpeedText.text = $"{value:F2}x";
        }
    }

    private void UpdateDepthUI(float depth)
    {
        if (depthValueText != null)
        {
            depthValueText.text = $"Profundidade: {depth * 100:F0}%";
        }

        if (layerText != null && renderingController != null)
        {
            int currentLayer = renderingController.GetCurrentLayer();
            layerText.text = $"Camada: {currentLayer + 1}";
        }
    }

    #endregion

    #region Settings Controls Setup

    private void SetupSettingsControls()
    {
        if (windowCenterSlider != null)
        {
            windowCenterSlider.minValue = 0f;
            windowCenterSlider.maxValue = 1f;
            windowCenterSlider.value = 0.5f;
            windowCenterSlider.onValueChanged.AddListener(OnWindowCenterChanged);
        }

        if (windowWidthSlider != null)
        {
            windowWidthSlider.minValue = 0.01f;
            windowWidthSlider.maxValue = 1f;
            windowWidthSlider.value = 0.5f;
            windowWidthSlider.onValueChanged.AddListener(OnWindowWidthChanged);
        }

        if (viewDistanceSlider != null)
        {
            viewDistanceSlider.minValue = 0.5f;
            viewDistanceSlider.maxValue = 10f;
            viewDistanceSlider.value = 2f;
            viewDistanceSlider.onValueChanged.AddListener(OnViewDistanceChanged);
        }

        if (qualitySlider != null)
        {
            qualitySlider.minValue = 0f;
            qualitySlider.maxValue = 1f;
            qualitySlider.value = 0.5f;
            qualitySlider.onValueChanged.AddListener(OnQualityChanged);
        }
    }

    private void OnWindowCenterChanged(float value)
    {
        if (volumeRenderer != null && windowWidthSlider != null)
        {
            volumeRenderer.SetWindowLevel(value, windowWidthSlider.value);
        }

        if (windowCenterText != null)
        {
            windowCenterText.text = $"Centro: {value:F2}";
        }
    }

    private void OnWindowWidthChanged(float value)
    {
        if (volumeRenderer != null && windowCenterSlider != null)
        {
            volumeRenderer.SetWindowLevel(windowCenterSlider.value, value);
        }

        if (windowWidthText != null)
        {
            windowWidthText.text = $"Largura: {value:F2}";
        }
    }

    private void OnViewDistanceChanged(float value)
    {
        if (renderingController != null)
        {
            renderingController.SetViewDistance(value);
        }

        if (viewDistanceText != null)
        {
            viewDistanceText.text = $"Distância: {value:F1}m";
        }
    }

    private void OnQualityChanged(float value)
    {
        if (volumeRenderer != null)
        {
            volumeRenderer.SetQuality(value);
        }

        if (qualityText != null)
        {
            string qualityLabel = value < 0.33f ? "Baixa" : (value < 0.66f ? "Média" : "Alta");
            qualityText.text = $"Qualidade: {qualityLabel}";
        }
    }

    #endregion

    #region Quick Access Buttons

    private void SetupQuickAccessButtons()
    {
        if (resetViewButton != null)
        {
            resetViewButton.onClick.AddListener(ResetView);
        }

        if (resetPresetsButton != null)
        {
            resetPresetsButton.onClick.AddListener(ResetToDefaultPreset);
        }
    }

    private void ResetView()
    {
        if (renderingController != null)
        {
            renderingController.SetViewAngle(VolumeRenderingController.MedicalViewAngle.Anterior, true);
        }

        if (lateralAngleSlider != null)
            lateralAngleSlider.value = 0f;

        if (cranialCaudalSlider != null)
            cranialCaudalSlider.value = 0f;
    }

    private void ResetToDefaultPreset()
    {
        if (renderingController != null)
        {
            renderingController.ApplyPreset(VolumeRenderingController.VisualizationPreset.Cardiac);
        }
    }

    #endregion

    #region Event Handlers

    private void OnPresetChangedHandler(object sender, VolumeRenderingController.PresetChangedEventArgs e)
    {
        UpdatePresetUI(e.Preset, e.PresetName);
    }

    private void OnViewAngleChangedHandler(object sender, VolumeRenderingController.ViewAngleChangedEventArgs e)
    {
        UpdateViewAngleUI(e.Angle);
    }

    private void OnDepthLayerChangedHandler(object sender, VolumeRenderingController.DepthLayerChangedEventArgs e)
    {
        if (depthSlider != null)
        {
            depthSlider.SetValueWithoutNotify(e.CurrentDepth);
        }
        UpdateDepthUI(e.CurrentDepth);
    }

    #endregion

    #region UI Updates

    private void UpdateUI()
    {
        if (renderingController == null) return;

        // Update preset display
        var currentPreset = renderingController.GetCurrentPreset();
        var presetConfig = renderingController.GetPresetConfiguration(currentPreset);
        if (presetConfig != null)
        {
            UpdatePresetUI(currentPreset, presetConfig.Name);
        }

        // Update view angle display
        var currentViewAngle = renderingController.GetCurrentViewAngle();
        UpdateViewAngleUI(currentViewAngle);

        // Update depth UI
        UpdateDepthUI(renderingController.GetCurrentDepth());
    }

    private void UpdatePresetUI(VolumeRenderingController.VisualizationPreset preset, string presetName)
    {
        if (currentPresetText != null)
        {
            currentPresetText.text = $"Preset: {presetName}";
        }

        if (presetDescriptionText != null && renderingController != null)
        {
            var config = renderingController.GetPresetConfiguration(preset);
            if (config != null)
            {
                presetDescriptionText.text = config.Description;
            }
        }
    }

    private void UpdateViewAngleUI(VolumeRenderingController.MedicalViewAngle angle)
    {
        if (currentViewText != null && renderingController != null)
        {
            var config = renderingController.GetViewAngleConfiguration(angle);
            if (config != null)
            {
                currentViewText.text = $"Vista: {config.Name}";
            }
        }

        if (viewDescriptionText != null && renderingController != null)
        {
            var config = renderingController.GetViewAngleConfiguration(angle);
            if (config != null)
            {
                viewDescriptionText.text = config.Description;
            }
        }
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// Show the volume rendering panel.
    /// </summary>
    public void Show()
    {
        if (volumeRenderingPanel != null)
        {
            volumeRenderingPanel.SetActive(true);
        }
        UpdateUI();
    }

    /// <summary>
    /// Hide the volume rendering panel.
    /// </summary>
    public void Hide()
    {
        if (volumeRenderingPanel != null)
        {
            volumeRenderingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle panel visibility.
    /// </summary>
    public void Toggle()
    {
        if (volumeRenderingPanel != null)
        {
            if (volumeRenderingPanel.activeSelf)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }
    }

    /// <summary>
    /// Check if panel is visible.
    /// </summary>
    public bool IsVisible()
    {
        return volumeRenderingPanel != null && volumeRenderingPanel.activeSelf;
    }

    #endregion

    #region Preset Categories

    /// <summary>
    /// Get presets for a specific category.
    /// </summary>
    public List<VolumeRenderingController.VisualizationPreset> GetPresetsByCategory(string category)
    {
        var presets = new List<VolumeRenderingController.VisualizationPreset>();

        switch (category.ToLower())
        {
            case "bone":
            case "osso":
                presets.Add(VolumeRenderingController.VisualizationPreset.Bone);
                presets.Add(VolumeRenderingController.VisualizationPreset.BoneWithSoftTissue);
                presets.Add(VolumeRenderingController.VisualizationPreset.Skull);
                presets.Add(VolumeRenderingController.VisualizationPreset.Spine);
                break;

            case "softtissue":
            case "tecido":
                presets.Add(VolumeRenderingController.VisualizationPreset.SoftTissue);
                presets.Add(VolumeRenderingController.VisualizationPreset.Muscle);
                presets.Add(VolumeRenderingController.VisualizationPreset.Fat);
                break;

            case "cardiac":
            case "cardiaco":
                presets.Add(VolumeRenderingController.VisualizationPreset.Cardiac);
                presets.Add(VolumeRenderingController.VisualizationPreset.CardiacChambers);
                presets.Add(VolumeRenderingController.VisualizationPreset.CardiacWall);
                presets.Add(VolumeRenderingController.VisualizationPreset.Coronaries);
                presets.Add(VolumeRenderingController.VisualizationPreset.Aorta);
                presets.Add(VolumeRenderingController.VisualizationPreset.Valves);
                break;

            case "vascular":
            case "vasos":
                presets.Add(VolumeRenderingController.VisualizationPreset.Blood);
                presets.Add(VolumeRenderingController.VisualizationPreset.Vessels);
                presets.Add(VolumeRenderingController.VisualizationPreset.VesselsWithContrast);
                presets.Add(VolumeRenderingController.VisualizationPreset.Angiography);
                break;

            case "lung":
            case "pulmonar":
            case "pulmao":
                presets.Add(VolumeRenderingController.VisualizationPreset.Lung);
                presets.Add(VolumeRenderingController.VisualizationPreset.LungParenchyma);
                presets.Add(VolumeRenderingController.VisualizationPreset.Airways);
                presets.Add(VolumeRenderingController.VisualizationPreset.Bronchi);
                break;

            case "special":
            case "especial":
                presets.Add(VolumeRenderingController.VisualizationPreset.MIP);
                presets.Add(VolumeRenderingController.VisualizationPreset.MinIP);
                presets.Add(VolumeRenderingController.VisualizationPreset.AverageIP);
                presets.Add(VolumeRenderingController.VisualizationPreset.SurfaceShaded);
                presets.Add(VolumeRenderingController.VisualizationPreset.VolumeRendered);
                presets.Add(VolumeRenderingController.VisualizationPreset.XRay);
                break;
        }

        return presets;
    }

    #endregion
}

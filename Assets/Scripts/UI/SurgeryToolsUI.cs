using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// UI Controller for surgery tools panel in VR.
/// Provides interface for selecting tools and managing surgical procedures.
/// </summary>
public class SurgeryToolsUI : MonoBehaviour
{
    public static SurgeryToolsUI Instance { get; private set; }

    public event EventHandler<ToolSelectedEventArgs> OnToolSelected;
    public event EventHandler OnSurgeryStarted;
    public event EventHandler OnSurgeryEnded;
    public event EventHandler OnUndoRequested;

    public class ToolSelectedEventArgs : EventArgs
    {
        public SurgerySimulator.SurgicalTool Tool;
    }

    [Header("Panel References")]
    [SerializeField] private GameObject surgeryPanel;
    [SerializeField] private GameObject toolSelectionPanel;
    [SerializeField] private GameObject procedurePanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Tool Buttons")]
    [SerializeField] private Button scalpelButton;
    [SerializeField] private Button sutureButton;
    [SerializeField] private Button ablationButton;
    [SerializeField] private Button clampButton;
    [SerializeField] private Button cannulaButton;
    [SerializeField] private Button electrocauteryButton;
    [SerializeField] private Button markerButton;

    [Header("Control Buttons")]
    [SerializeField] private Button startSurgeryButton;
    [SerializeField] private Button endSurgeryButton;
    [SerializeField] private Button undoButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button settingsButton;

    [Header("Status Display")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI currentToolText;
    [SerializeField] private TextMeshProUGUI actionCountText;
    [SerializeField] private Image surgeryIndicator;

    [Header("Settings Controls")]
    [SerializeField] private Slider incisionWidthSlider;
    [SerializeField] private Slider ablationRadiusSlider;
    [SerializeField] private Slider powerLevelSlider;
    [SerializeField] private TMP_Dropdown ablationTypeDropdown;
    [SerializeField] private TMP_Dropdown sutureTypeDropdown;

    [Header("Visual Feedback")]
    [SerializeField] private Color activeToolColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color inactiveToolColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color surgeryActiveColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color surgeryInactiveColor = new Color(0.8f, 0.2f, 0.2f);

    private SurgerySimulator surgerySimulator;
    private SurgerySimulator.SurgicalTool currentTool = SurgerySimulator.SurgicalTool.None;
    private Button currentSelectedButton;

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
        surgerySimulator = SurgerySimulator.Instance;

        SetupButtonListeners();
        SetupSliderListeners();
        SetupDropdownListeners();

        // Subscribe to surgery events
        if (surgerySimulator != null)
        {
            surgerySimulator.OnSurgeryStarted += OnSurgerySimulatorStarted;
            surgerySimulator.OnSurgeryEnded += OnSurgerySimulatorEnded;
            surgerySimulator.OnToolChanged += OnSurgeryToolChanged;
            surgerySimulator.OnSurgicalAction += OnSurgicalActionPerformed;
        }

        UpdateUI();
    }

    void OnDestroy()
    {
        if (surgerySimulator != null)
        {
            surgerySimulator.OnSurgeryStarted -= OnSurgerySimulatorStarted;
            surgerySimulator.OnSurgeryEnded -= OnSurgerySimulatorEnded;
            surgerySimulator.OnToolChanged -= OnSurgeryToolChanged;
            surgerySimulator.OnSurgicalAction -= OnSurgicalActionPerformed;
        }
    }

    private void SetupButtonListeners()
    {
        // Tool buttons
        if (scalpelButton != null)
            scalpelButton.onClick.AddListener(() => SelectTool(SurgerySimulator.SurgicalTool.Scalpel));

        if (sutureButton != null)
            sutureButton.onClick.AddListener(() => SelectTool(SurgerySimulator.SurgicalTool.NeedleHolder));

        if (ablationButton != null)
            ablationButton.onClick.AddListener(() => SelectTool(SurgerySimulator.SurgicalTool.AblationCatheter));

        if (clampButton != null)
            clampButton.onClick.AddListener(() => SelectTool(SurgerySimulator.SurgicalTool.VascularClamp));

        if (cannulaButton != null)
            cannulaButton.onClick.AddListener(() => SelectTool(SurgerySimulator.SurgicalTool.Cannula));

        if (electrocauteryButton != null)
            electrocauteryButton.onClick.AddListener(() => SelectTool(SurgerySimulator.SurgicalTool.Electrocautery));

        if (markerButton != null)
            markerButton.onClick.AddListener(() => SelectTool(SurgerySimulator.SurgicalTool.Marker));

        // Control buttons
        if (startSurgeryButton != null)
            startSurgeryButton.onClick.AddListener(StartSurgery);

        if (endSurgeryButton != null)
            endSurgeryButton.onClick.AddListener(EndSurgery);

        if (undoButton != null)
            undoButton.onClick.AddListener(UndoLastAction);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetSurgery);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(ToggleSettings);
    }

    private void SetupSliderListeners()
    {
        if (incisionWidthSlider != null)
        {
            incisionWidthSlider.onValueChanged.AddListener(OnIncisionWidthChanged);
        }

        if (ablationRadiusSlider != null)
        {
            ablationRadiusSlider.onValueChanged.AddListener(OnAblationRadiusChanged);
        }

        if (powerLevelSlider != null)
        {
            powerLevelSlider.onValueChanged.AddListener(OnPowerLevelChanged);
        }
    }

    private void SetupDropdownListeners()
    {
        if (ablationTypeDropdown != null)
        {
            ablationTypeDropdown.ClearOptions();
            ablationTypeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Radiofrequency", "Cryo", "Laser"
            });
            ablationTypeDropdown.onValueChanged.AddListener(OnAblationTypeChanged);
        }

        if (sutureTypeDropdown != null)
        {
            sutureTypeDropdown.ClearOptions();
            sutureTypeDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "Single Stitch", "Running Stitch", "Figure-8", "Interrupted"
            });
            sutureTypeDropdown.onValueChanged.AddListener(OnSutureTypeChanged);
        }
    }

    #region Public Methods

    /// <summary>
    /// Select a surgical tool.
    /// </summary>
    public void SelectTool(SurgerySimulator.SurgicalTool tool)
    {
        currentTool = tool;

        if (surgerySimulator != null)
        {
            surgerySimulator.SetTool(tool);
        }

        UpdateToolButtonHighlight(tool);

        OnToolSelected?.Invoke(this, new ToolSelectedEventArgs { Tool = tool });

        Debug.Log($"Tool selected: {tool}");
    }

    /// <summary>
    /// Start a surgery session.
    /// </summary>
    public void StartSurgery()
    {
        if (surgerySimulator != null)
        {
            surgerySimulator.StartSurgery();
        }

        OnSurgeryStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// End the current surgery session.
    /// </summary>
    public void EndSurgery()
    {
        if (surgerySimulator != null)
        {
            surgerySimulator.EndSurgery();
        }

        OnSurgeryEnded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Undo the last surgical action.
    /// </summary>
    public void UndoLastAction()
    {
        if (surgerySimulator != null)
        {
            surgerySimulator.UndoLastAction();
        }

        OnUndoRequested?.Invoke(this, EventArgs.Empty);
        UpdateUI();
    }

    /// <summary>
    /// Reset the surgery simulation.
    /// </summary>
    public void ResetSurgery()
    {
        if (surgerySimulator != null)
        {
            surgerySimulator.ResetSurgery();
        }

        UpdateUI();
    }

    /// <summary>
    /// Show or hide the surgery panel.
    /// </summary>
    public void SetPanelVisible(bool visible)
    {
        if (surgeryPanel != null)
        {
            surgeryPanel.SetActive(visible);
        }
    }

    /// <summary>
    /// Toggle settings panel visibility.
    /// </summary>
    public void ToggleSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }
    }

    #endregion

    #region Event Handlers

    private void OnSurgerySimulatorStarted(object sender, EventArgs e)
    {
        UpdateUI();
        SetStatus("Surgery session started");
    }

    private void OnSurgerySimulatorEnded(object sender, EventArgs e)
    {
        UpdateUI();
        SetStatus("Surgery session ended");
    }

    private void OnSurgeryToolChanged(object sender, SurgerySimulator.ToolChangedEventArgs e)
    {
        currentTool = e.CurrentTool;
        UpdateToolButtonHighlight(currentTool);
        UpdateToolText();
    }

    private void OnSurgicalActionPerformed(object sender, SurgerySimulator.SurgicalActionEventArgs e)
    {
        SetStatus(e.Description);
        UpdateActionCount();
    }

    private void OnIncisionWidthChanged(float value)
    {
        // Update incision width setting
        Debug.Log($"Incision width: {value}mm");
    }

    private void OnAblationRadiusChanged(float value)
    {
        // Update ablation radius
        AblationTool ablationTool = FindObjectOfType<AblationTool>();
        if (ablationTool != null)
        {
            ablationTool.SetAblationRadius(value / 1000f); // Convert mm to meters
        }
    }

    private void OnPowerLevelChanged(float value)
    {
        // Update power level
        ElectrocauteryTool cauteryTool = FindObjectOfType<ElectrocauteryTool>();
        if (cauteryTool != null)
        {
            cauteryTool.SetPowerLevel(value);
        }
    }

    private void OnAblationTypeChanged(int index)
    {
        AblationTool ablationTool = FindObjectOfType<AblationTool>();
        if (ablationTool != null)
        {
            ablationTool.SetAblationType((AblationTool.AblationType)index);
        }
    }

    private void OnSutureTypeChanged(int index)
    {
        SutureTool sutureTool = FindObjectOfType<SutureTool>();
        if (sutureTool != null)
        {
            sutureTool.SetMode((SutureTool.SutureMode)index);
        }
    }

    #endregion

    #region UI Updates

    private void UpdateUI()
    {
        bool isSurgeryActive = surgerySimulator != null && surgerySimulator.IsSurgeryActive();

        // Update button states
        if (startSurgeryButton != null)
            startSurgeryButton.interactable = !isSurgeryActive;

        if (endSurgeryButton != null)
            endSurgeryButton.interactable = isSurgeryActive;

        if (undoButton != null)
            undoButton.interactable = isSurgeryActive && surgerySimulator.GetActionCount() > 0;

        // Update tool buttons
        SetToolButtonsInteractable(isSurgeryActive);

        // Update indicator
        if (surgeryIndicator != null)
        {
            surgeryIndicator.color = isSurgeryActive ? surgeryActiveColor : surgeryInactiveColor;
        }

        UpdateToolText();
        UpdateActionCount();
    }

    private void SetToolButtonsInteractable(bool interactable)
    {
        if (scalpelButton != null) scalpelButton.interactable = interactable;
        if (sutureButton != null) sutureButton.interactable = interactable;
        if (ablationButton != null) ablationButton.interactable = interactable;
        if (clampButton != null) clampButton.interactable = interactable;
        if (cannulaButton != null) cannulaButton.interactable = interactable;
        if (electrocauteryButton != null) electrocauteryButton.interactable = interactable;
        if (markerButton != null) markerButton.interactable = interactable;
    }

    private void UpdateToolButtonHighlight(SurgerySimulator.SurgicalTool tool)
    {
        // Reset all buttons
        ResetButtonColor(scalpelButton);
        ResetButtonColor(sutureButton);
        ResetButtonColor(ablationButton);
        ResetButtonColor(clampButton);
        ResetButtonColor(cannulaButton);
        ResetButtonColor(electrocauteryButton);
        ResetButtonColor(markerButton);

        // Highlight selected button
        Button selectedButton = GetButtonForTool(tool);
        if (selectedButton != null)
        {
            ColorBlock colors = selectedButton.colors;
            colors.normalColor = activeToolColor;
            selectedButton.colors = colors;
            currentSelectedButton = selectedButton;
        }
    }

    private Button GetButtonForTool(SurgerySimulator.SurgicalTool tool)
    {
        switch (tool)
        {
            case SurgerySimulator.SurgicalTool.Scalpel:
            case SurgerySimulator.SurgicalTool.Scissors:
                return scalpelButton;
            case SurgerySimulator.SurgicalTool.NeedleHolder:
            case SurgerySimulator.SurgicalTool.SutureNeedle:
                return sutureButton;
            case SurgerySimulator.SurgicalTool.AblationCatheter:
                return ablationButton;
            case SurgerySimulator.SurgicalTool.VascularClamp:
                return clampButton;
            case SurgerySimulator.SurgicalTool.Cannula:
                return cannulaButton;
            case SurgerySimulator.SurgicalTool.Electrocautery:
                return electrocauteryButton;
            case SurgerySimulator.SurgicalTool.Marker:
                return markerButton;
            default:
                return null;
        }
    }

    private void ResetButtonColor(Button button)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = inactiveToolColor;
        button.colors = colors;
    }

    private void UpdateToolText()
    {
        if (currentToolText != null)
        {
            currentToolText.text = $"Tool: {currentTool}";
        }
    }

    private void UpdateActionCount()
    {
        if (actionCountText != null && surgerySimulator != null)
        {
            actionCountText.text = $"Actions: {surgerySimulator.GetActionCount()}";
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    #endregion
}

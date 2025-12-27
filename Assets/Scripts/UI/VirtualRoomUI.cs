using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// UI Controller for virtual room selection and configuration.
/// Allows users to choose different operating environments and place equipment.
/// </summary>
public class VirtualRoomUI : MonoBehaviour
{
    public static VirtualRoomUI Instance { get; private set; }

    public event EventHandler<RoomSelectedEventArgs> OnRoomSelected;
    public event EventHandler OnEquipmentModeToggled;

    public class RoomSelectedEventArgs : EventArgs
    {
        public VirtualRoomManager.RoomType RoomType;
        public string RoomName;
    }

    [Header("Panel References")]
    [SerializeField] private GameObject roomSelectionPanel;
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject roomSettingsPanel;

    [Header("Room Selection Buttons")]
    [SerializeField] private Button operatingRoomButton;
    [SerializeField] private Button hemodynamicsLabButton;
    [SerializeField] private Button hybridORButton;
    [SerializeField] private Button icuButton;
    [SerializeField] private Button customRoomButton;
    [SerializeField] private Button noRoomButton;

    [Header("Equipment Buttons")]
    [SerializeField] private Button operatingTableButton;
    [SerializeField] private Button surgicalLightsButton;
    [SerializeField] private Button anesthesiaMachineButton;
    [SerializeField] private Button patientMonitorButton;
    [SerializeField] private Button cArmButton;
    [SerializeField] private Button heartLungMachineButton;
    [SerializeField] private Button ecmoButton;

    [Header("Control Buttons")]
    [SerializeField] private Button toggleEquipmentModeButton;
    [SerializeField] private Button clearEquipmentButton;
    [SerializeField] private Button resetRoomButton;
    [SerializeField] private Button importRoomButton;

    [Header("Display")]
    [SerializeField] private TextMeshProUGUI currentRoomText;
    [SerializeField] private TextMeshProUGUI roomDescriptionText;
    [SerializeField] private TextMeshProUGUI equipmentCountText;
    [SerializeField] private Image roomPreviewImage;

    [Header("Room Settings")]
    [SerializeField] private Slider lightIntensitySlider;
    [SerializeField] private TextMeshProUGUI lightIntensityText;
    [SerializeField] private Slider ambientLightSlider;
    [SerializeField] private Toggle shadowsToggle;

    [Header("Equipment Placement")]
    [SerializeField] private Toggle equipmentPlacementToggle;
    [SerializeField] private TextMeshProUGUI selectedEquipmentText;

    [Header("Visual Feedback")]
    [SerializeField] private Color selectedButtonColor = new Color(0.2f, 0.8f, 0.2f);
    [SerializeField] private Color normalButtonColor = new Color(0.5f, 0.5f, 0.5f);

    [Header("Room Preview Images")]
    [SerializeField] private Sprite operatingRoomPreview;
    [SerializeField] private Sprite hemodynamicsLabPreview;
    [SerializeField] private Sprite hybridORPreview;

    private VirtualRoomManager roomManager;
    private VirtualRoomManager.EquipmentType selectedEquipment = VirtualRoomManager.EquipmentType.None;
    private bool isEquipmentPlacementMode = false;
    private Button currentSelectedRoomButton;

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
        roomManager = VirtualRoomManager.Instance;

        SetupButtonListeners();
        SetupSliderListeners();

        if (roomManager != null)
        {
            roomManager.OnRoomChanged += OnRoomManagerRoomChanged;
            roomManager.OnEquipmentPlaced += OnEquipmentPlacedHandler;
        }

        UpdateUI();
    }

    void OnDestroy()
    {
        if (roomManager != null)
        {
            roomManager.OnRoomChanged -= OnRoomManagerRoomChanged;
            roomManager.OnEquipmentPlaced -= OnEquipmentPlacedHandler;
        }
    }

    private void SetupButtonListeners()
    {
        // Room selection buttons
        if (operatingRoomButton != null)
            operatingRoomButton.onClick.AddListener(() => SelectRoom(VirtualRoomManager.RoomType.OperatingRoom, operatingRoomButton));

        if (hemodynamicsLabButton != null)
            hemodynamicsLabButton.onClick.AddListener(() => SelectRoom(VirtualRoomManager.RoomType.HemodynamicsLab, hemodynamicsLabButton));

        if (hybridORButton != null)
            hybridORButton.onClick.AddListener(() => SelectRoom(VirtualRoomManager.RoomType.HybridOR, hybridORButton));

        if (icuButton != null)
            icuButton.onClick.AddListener(() => SelectRoom(VirtualRoomManager.RoomType.ICU, icuButton));

        if (customRoomButton != null)
            customRoomButton.onClick.AddListener(OnCustomRoomButtonClick);

        if (noRoomButton != null)
            noRoomButton.onClick.AddListener(() => SelectRoom(VirtualRoomManager.RoomType.None, noRoomButton));

        // Equipment buttons
        if (operatingTableButton != null)
            operatingTableButton.onClick.AddListener(() => SelectEquipment(VirtualRoomManager.EquipmentType.OperatingTable));

        if (surgicalLightsButton != null)
            surgicalLightsButton.onClick.AddListener(() => SelectEquipment(VirtualRoomManager.EquipmentType.SurgicalLights));

        if (anesthesiaMachineButton != null)
            anesthesiaMachineButton.onClick.AddListener(() => SelectEquipment(VirtualRoomManager.EquipmentType.AnesthesiaMachine));

        if (patientMonitorButton != null)
            patientMonitorButton.onClick.AddListener(() => SelectEquipment(VirtualRoomManager.EquipmentType.PatientMonitor));

        if (cArmButton != null)
            cArmButton.onClick.AddListener(() => SelectEquipment(VirtualRoomManager.EquipmentType.CArm));

        if (heartLungMachineButton != null)
            heartLungMachineButton.onClick.AddListener(() => SelectEquipment(VirtualRoomManager.EquipmentType.HeartLungMachine));

        if (ecmoButton != null)
            ecmoButton.onClick.AddListener(() => SelectEquipment(VirtualRoomManager.EquipmentType.ECMO));

        // Control buttons
        if (toggleEquipmentModeButton != null)
            toggleEquipmentModeButton.onClick.AddListener(ToggleEquipmentPlacementMode);

        if (clearEquipmentButton != null)
            clearEquipmentButton.onClick.AddListener(ClearAllEquipment);

        if (resetRoomButton != null)
            resetRoomButton.onClick.AddListener(ResetRoom);

        if (importRoomButton != null)
            importRoomButton.onClick.AddListener(OnImportRoomButtonClick);

        // Toggle
        if (equipmentPlacementToggle != null)
            equipmentPlacementToggle.onValueChanged.AddListener(OnEquipmentPlacementToggleChanged);
    }

    private void SetupSliderListeners()
    {
        if (lightIntensitySlider != null)
        {
            lightIntensitySlider.minValue = 0f;
            lightIntensitySlider.maxValue = 3f;
            lightIntensitySlider.value = 1f;
            lightIntensitySlider.onValueChanged.AddListener(OnLightIntensityChanged);
        }

        if (ambientLightSlider != null)
        {
            ambientLightSlider.minValue = 0f;
            ambientLightSlider.maxValue = 1f;
            ambientLightSlider.value = 0.5f;
            ambientLightSlider.onValueChanged.AddListener(OnAmbientLightChanged);
        }

        if (shadowsToggle != null)
        {
            shadowsToggle.onValueChanged.AddListener(OnShadowsToggleChanged);
        }
    }

    #region Room Selection

    /// <summary>
    /// Select a room type.
    /// </summary>
    public void SelectRoom(VirtualRoomManager.RoomType roomType, Button button = null)
    {
        if (roomManager != null)
        {
            roomManager.LoadRoom(roomType);
        }

        // Update button highlight
        if (button != null)
        {
            UpdateRoomButtonHighlight(button);
        }

        OnRoomSelected?.Invoke(this, new RoomSelectedEventArgs
        {
            RoomType = roomType,
            RoomName = roomManager?.GetCurrentRoomName() ?? roomType.ToString()
        });
    }

    private void OnCustomRoomButtonClick()
    {
        // In a full implementation, this would open a file browser
        // For now, we'll show a message
        Debug.Log("Custom room import requested. Use LoadCustomRoom(filePath) method.");

        // Try to load from a default path for testing
        string defaultPath = Application.dataPath + "/Resources/CustomRooms/room.stl";
        if (System.IO.File.Exists(defaultPath))
        {
            roomManager?.LoadCustomRoom(defaultPath);
        }
        else
        {
            // Fall back to a procedural room
            SelectRoom(VirtualRoomManager.RoomType.OperatingRoom, customRoomButton);
        }
    }

    private void OnImportRoomButtonClick()
    {
        Debug.Log("Import room button clicked. File browser would open here.");
        // In production, integrate with a file browser asset like SimpleFileBrowser
    }

    private void UpdateRoomButtonHighlight(Button selectedButton)
    {
        // Reset all room buttons
        ResetButtonColor(operatingRoomButton);
        ResetButtonColor(hemodynamicsLabButton);
        ResetButtonColor(hybridORButton);
        ResetButtonColor(icuButton);
        ResetButtonColor(customRoomButton);
        ResetButtonColor(noRoomButton);

        // Highlight selected
        if (selectedButton != null)
        {
            ColorBlock colors = selectedButton.colors;
            colors.normalColor = selectedButtonColor;
            selectedButton.colors = colors;
            currentSelectedRoomButton = selectedButton;
        }
    }

    private void ResetButtonColor(Button button)
    {
        if (button == null) return;

        ColorBlock colors = button.colors;
        colors.normalColor = normalButtonColor;
        button.colors = colors;
    }

    #endregion

    #region Equipment Management

    /// <summary>
    /// Select equipment type for placement.
    /// </summary>
    public void SelectEquipment(VirtualRoomManager.EquipmentType equipmentType)
    {
        selectedEquipment = equipmentType;

        if (selectedEquipmentText != null)
        {
            selectedEquipmentText.text = $"Selected: {equipmentType}";
        }

        // Auto-enable placement mode when equipment is selected
        if (!isEquipmentPlacementMode)
        {
            SetEquipmentPlacementMode(true);
        }
    }

    /// <summary>
    /// Toggle equipment placement mode.
    /// </summary>
    public void ToggleEquipmentPlacementMode()
    {
        SetEquipmentPlacementMode(!isEquipmentPlacementMode);
    }

    /// <summary>
    /// Set equipment placement mode.
    /// </summary>
    public void SetEquipmentPlacementMode(bool enabled)
    {
        isEquipmentPlacementMode = enabled;

        if (equipmentPlacementToggle != null)
        {
            equipmentPlacementToggle.SetIsOnWithoutNotify(enabled);
        }

        if (equipmentPanel != null)
        {
            // Highlight equipment panel when in placement mode
            // This could show/hide additional placement UI elements
        }

        OnEquipmentModeToggled?.Invoke(this, EventArgs.Empty);
    }

    private void OnEquipmentPlacementToggleChanged(bool value)
    {
        SetEquipmentPlacementMode(value);
    }

    /// <summary>
    /// Place the currently selected equipment at a position.
    /// Called by VR controller interaction.
    /// </summary>
    public void PlaceSelectedEquipment(Vector3 position, Quaternion rotation)
    {
        if (!isEquipmentPlacementMode || selectedEquipment == VirtualRoomManager.EquipmentType.None)
        {
            return;
        }

        if (roomManager != null)
        {
            roomManager.PlaceEquipment(selectedEquipment, position, rotation);
        }
    }

    /// <summary>
    /// Clear all placed equipment.
    /// </summary>
    public void ClearAllEquipment()
    {
        if (roomManager != null)
        {
            List<GameObject> equipment = roomManager.GetPlacedEquipment();
            foreach (GameObject obj in equipment)
            {
                roomManager.RemoveEquipment(obj);
            }
        }

        UpdateEquipmentCount();
    }

    /// <summary>
    /// Reset room to default state.
    /// </summary>
    public void ResetRoom()
    {
        if (roomManager != null)
        {
            VirtualRoomManager.RoomType currentType = roomManager.GetCurrentRoomType();
            roomManager.LoadRoom(currentType);
        }
    }

    #endregion

    #region Event Handlers

    private void OnRoomManagerRoomChanged(object sender, VirtualRoomManager.RoomChangedEventArgs e)
    {
        UpdateUI();
    }

    private void OnEquipmentPlacedHandler(object sender, VirtualRoomManager.EquipmentPlacedEventArgs e)
    {
        UpdateEquipmentCount();
    }

    private void OnLightIntensityChanged(float value)
    {
        if (lightIntensityText != null)
        {
            lightIntensityText.text = $"{value:F1}";
        }

        // Apply to room lights
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                light.intensity = value;
            }
        }
    }

    private void OnAmbientLightChanged(float value)
    {
        RenderSettings.ambientIntensity = value;
    }

    private void OnShadowsToggleChanged(bool enabled)
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            light.shadows = enabled ? LightShadows.Soft : LightShadows.None;
        }
    }

    #endregion

    #region UI Updates

    private void UpdateUI()
    {
        if (roomManager == null) return;

        // Update room text
        if (currentRoomText != null)
        {
            currentRoomText.text = roomManager.GetCurrentRoomName();
        }

        // Update description
        if (roomDescriptionText != null)
        {
            VirtualRoomManager.RoomType roomType = roomManager.GetCurrentRoomType();
            RoomConfiguration config = roomManager.GetRoomConfiguration(roomType.ToString());

            if (config != null)
            {
                roomDescriptionText.text = config.Description;
            }
            else
            {
                roomDescriptionText.text = "";
            }
        }

        // Update preview image
        UpdatePreviewImage();

        // Update equipment count
        UpdateEquipmentCount();
    }

    private void UpdatePreviewImage()
    {
        if (roomPreviewImage == null || roomManager == null) return;

        VirtualRoomManager.RoomType roomType = roomManager.GetCurrentRoomType();

        Sprite preview = null;
        switch (roomType)
        {
            case VirtualRoomManager.RoomType.OperatingRoom:
                preview = operatingRoomPreview;
                break;
            case VirtualRoomManager.RoomType.HemodynamicsLab:
                preview = hemodynamicsLabPreview;
                break;
            case VirtualRoomManager.RoomType.HybridOR:
                preview = hybridORPreview;
                break;
        }

        roomPreviewImage.sprite = preview;
        roomPreviewImage.enabled = preview != null;
    }

    private void UpdateEquipmentCount()
    {
        if (equipmentCountText != null && roomManager != null)
        {
            int count = roomManager.GetPlacedEquipment().Count;
            equipmentCountText.text = $"Equipment: {count}";
        }
    }

    #endregion

    #region Panel Control

    /// <summary>
    /// Show the room selection panel.
    /// </summary>
    public void Show()
    {
        if (roomSelectionPanel != null)
        {
            roomSelectionPanel.SetActive(true);
        }
        UpdateUI();
    }

    /// <summary>
    /// Hide the room selection panel.
    /// </summary>
    public void Hide()
    {
        if (roomSelectionPanel != null)
        {
            roomSelectionPanel.SetActive(false);
        }

        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Toggle panel visibility.
    /// </summary>
    public void Toggle()
    {
        if (roomSelectionPanel != null)
        {
            if (roomSelectionPanel.activeSelf)
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
    /// Show equipment panel.
    /// </summary>
    public void ShowEquipmentPanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hide equipment panel.
    /// </summary>
    public void HideEquipmentPanel()
    {
        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
        }
    }

    #endregion

    #region Public Getters

    public bool IsEquipmentPlacementMode() => isEquipmentPlacementMode;
    public VirtualRoomManager.EquipmentType GetSelectedEquipment() => selectedEquipment;

    #endregion
}

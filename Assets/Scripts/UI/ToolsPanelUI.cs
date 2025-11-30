using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;

public class ToolsPanelUI : MonoBehaviour
{
    public event EventHandler OnNavigateEvent;
    public event EventHandler OnModeChange;
    public event EventHandler OnSurgeryModeEnter;
    public event EventHandler OnSurgeryModeExit;
    public event EventHandler OnARModeEnter;
    public event EventHandler OnARModeExit;

    [Header("UIs")]
    [SerializeField] private GameObject dicomImageUI;

    [SerializeField] private GameObject measurementToolsUI;
    [SerializeField] private GameObject settingsUI;
    [SerializeField] private GameObject surgeryToolsUI;
    [SerializeField] private GameObject arSettingsUI;
    public static ToolsPanelUI Instance { get; private set; }

    public enum Modes
    {
        Default,
        Move,
        Rotate,
        Scale,
        Measure,
        Tag,
        Clip,
        Navigate,
        Dicom,
        Settings,
        Surgery,
        AR
    }

    public enum Navigation {
        Outside,
        Inside,
    }

    [Header("Buttons")]
    [SerializeField] private Button moveButton;
    [SerializeField] private Button rotateButton;
    [SerializeField] private Button scaleButton;
    [SerializeField] private Button measureButton;
    [SerializeField] private Button tagButton;
    [SerializeField] private Button clipButton;
    [SerializeField] private Button navigateButton;
    [SerializeField] private Button dicomButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button surgeryButton;
    [SerializeField] private Button arButton;

    private Modes currentMode = Modes.Default;

    private Navigation currentNavigation = Navigation.Outside;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Add listeners
        moveButton.onClick.AddListener(() => OnButtonClick(moveButton, Modes.Move));
        rotateButton.onClick.AddListener(() => OnButtonClick(rotateButton, Modes.Rotate));
        scaleButton.onClick.AddListener(() => OnButtonClick(scaleButton, Modes.Scale));
        measureButton.onClick.AddListener(() => OnButtonClick(measureButton, Modes.Measure));
        tagButton.onClick.AddListener(() => OnButtonClick(tagButton, Modes.Tag));
        clipButton.onClick.AddListener(() => OnButtonClick(clipButton, Modes.Clip));
        navigateButton.onClick.AddListener(() => OnButtonClick(navigateButton, Modes.Navigate));
        dicomButton.onClick.AddListener(() => OnButtonClick(dicomButton, Modes.Dicom));
        settingsButton.onClick.AddListener(() => OnButtonClick(settingsButton, Modes.Settings));
        if (surgeryButton != null)
            surgeryButton.onClick.AddListener(() => OnButtonClick(surgeryButton, Modes.Surgery));
        if (arButton != null)
            arButton.onClick.AddListener(() => OnButtonClick(arButton, Modes.AR));

        XRIDefaultInputActions inputAction = InputActionsManager.Instance.InputActions;
        inputAction.XRILeftHand.MenuButton.performed += OnMenuButtonPerformed;

        //dicomImageUI.SetActive(false);
        Hide();
    }

    private void OnMenuButtonPerformed(InputAction.CallbackContext context)
    {
        if (context.interaction is not TapInteraction) {
            return;
        }

        if (dicomImageUI.activeSelf ||
        measurementToolsUI.activeSelf ||
        (surgeryToolsUI != null && surgeryToolsUI.activeSelf) ||
        (arSettingsUI != null && arSettingsUI.activeSelf))
        {
            dicomImageUI.SetActive(false);
            measurementToolsUI.SetActive(false);
            if (surgeryToolsUI != null) surgeryToolsUI.SetActive(false);
            if (arSettingsUI != null) arSettingsUI.SetActive(false);
            return;
        } else if (currentNavigation == Navigation.Outside) {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    private void OnButtonClick(Button clickedButton, Modes mode)
    {
        Modes previousMode = currentMode;
        currentMode = mode;
        clickedButton.Select();

        if(mode == Modes.Navigate) {
            OnNavigateEvent?.Invoke(this, EventArgs.Empty);
        }

        if (mode == Modes.Measure) {
            measurementToolsUI.SetActive(true);
        } else {
            measurementToolsUI.SetActive(false);
        }

        if (mode == Modes.Settings) {
            settingsUI.SetActive(true);
        } else {
            settingsUI.SetActive(false);
        }

        if (mode == Modes.Dicom) {
            currentMode = Modes.Dicom;
            dicomImageUI.SetActive(true);
        } else {
            dicomImageUI.SetActive(false);
        }

        if (mode == Modes.Surgery) {
            if (surgeryToolsUI != null) surgeryToolsUI.SetActive(true);
            OnSurgeryModeEnter?.Invoke(this, EventArgs.Empty);
        } else {
            if (surgeryToolsUI != null) surgeryToolsUI.SetActive(false);
            if (previousMode == Modes.Surgery) {
                OnSurgeryModeExit?.Invoke(this, EventArgs.Empty);
            }
        }

        if (mode == Modes.AR) {
            if (arSettingsUI != null) arSettingsUI.SetActive(true);
            OnARModeEnter?.Invoke(this, EventArgs.Empty);
        } else {
            if (arSettingsUI != null) arSettingsUI.SetActive(false);
            if (previousMode == Modes.AR) {
                OnARModeExit?.Invoke(this, EventArgs.Empty);
            }
        }

        OnModeChange?.Invoke(this, EventArgs.Empty);
        Debug.Log("Current Mode: " + mode.ToString());
        Hide();
    }

    public Modes GetMode()
    {
        return currentMode;
    }

    public void SetMode(Modes mode) {
        currentMode = mode;
    }

    public Navigation GetNavigation()
    {
        return currentNavigation;
    }

    public void SetNavigation(Navigation navigation) {
        currentNavigation = navigation;
    }

    public void Hide() { 
        gameObject.SetActive(false);
    }
}

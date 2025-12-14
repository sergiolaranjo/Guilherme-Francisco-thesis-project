using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.UI;

public class MenuToolsUI : MonoBehaviour
{
    public static MenuToolsUI Instance { get; private set;}
    public event EventHandler OnLeaveButtonEvent;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button dicomButton;
    [SerializeField] private Button measurementButton;

    [SerializeField] private Button mapDisplayButton;

    [SerializeField] private GameObject dicomImageUI;
    [SerializeField] private GameObject sliders;

    [SerializeField] private GameObject minimap;
    [SerializeField] private GameObject measurementToolsUI;  

    private ToolsPanelUI toolsPanelUI;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        toolsPanelUI = ToolsPanelUI.Instance;
        
        leaveButton.onClick.AddListener(OnLeaveClick);
        dicomButton.onClick.AddListener(OnDicomClick);
        measurementButton.onClick.AddListener(OnMeasurementClick);
        mapDisplayButton.onClick.AddListener(OnMapDisplayClick);

        XRIDefaultInputActions inputActions = InputActionsManager.Instance.InputActions;

        inputActions.XRILeftHand.MenuButton.performed += OnMenuButtonPerformed;

        minimap.SetActive(false);
        Hide();
    }

    private void OnMenuButtonPerformed(InputAction.CallbackContext context)
    {

        if (context.interaction is not TapInteraction) {
            return;
        }
        
        Debug.Log("Right Menu Button performed!");

        if (minimap.activeSelf || dicomImageUI.activeSelf 
            || measurementToolsUI.activeSelf) 
        {
            minimap.SetActive(false);
            dicomImageUI.SetActive(false);
            measurementToolsUI.SetActive(false);
            return;
        } else if (toolsPanelUI.GetNavigation() == ToolsPanelUI.Navigation.Inside) {
            Debug.Log("Menu mode:" + gameObject.activeSelf.ToString());
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

    private void OnMapDisplayClick()
    {
        minimap.SetActive(true);
        Hide();
    }


    private void Hide(){
        gameObject.SetActive(false);
    }

    private void OnMeasurementClick()
    {
        measurementToolsUI.SetActive(true);
        toolsPanelUI.SetMode(ToolsPanelUI.Modes.Measure);
        Hide();
    }


    private void OnDicomClick()
    {
        dicomImageUI.SetActive(true);
        sliders.SetActive(false);
        Hide();
    }


    private void OnLeaveClick()
    {
        OnLeaveButtonEvent?.Invoke(this, EventArgs.Empty);
        Hide();
    }

}

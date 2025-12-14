using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputActionsManager : MonoBehaviour
{
    public static InputActionsManager Instance { get; private set; }  
    public XRIDefaultInputActions InputActions { get; private set; }
    private void Awake() {
        Instance = this;

        InputActions = new XRIDefaultInputActions();

         // Left
		InputActions.XRILeftHand.Activate.performed += OnActivatePerformed;
        InputActions.XRILeftHand.PrimaryButton.performed += OnPrimaryButtonPerformed;
        InputActions.XRILeftHand.SecondaryButton.performed += OnSecondaryButtonPerformed;
        InputActions.XRILeftHand.MenuButton.performed += OnMenuButtonPerformed;

        // Right
        InputActions.XRIRightHand.Move.performed += OnRightHandMovedPerformed;
    }

    private void OnMenuButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Menu Left Hand performed!");
    }

    private void OnEnable()
    {
        // Left
        InputActions.XRILeftHand.PrimaryButton.Enable();
        InputActions.XRILeftHand.SecondaryButton.Enable();
        InputActions.XRILeftHand.Move.Enable();
        InputActions.XRILeftHand.Activate.Enable();
        InputActions.XRILeftHand.Select.Enable();
        InputActions.XRILeftHand.MenuButton.Enable();
        // Right
        InputActions.XRIRightHand.PrimaryButton.Enable();
        InputActions.XRIRightHand.SecondaryButton.Enable();
        InputActions.XRIRightHand.Move.Enable();
        InputActions.XRIRightHand.Select.Enable();
    }

    private void OnDisable()
    {
        // Unsubscribe from events first
        InputActions.XRILeftHand.Activate.performed -= OnActivatePerformed;
        InputActions.XRILeftHand.PrimaryButton.performed -= OnPrimaryButtonPerformed;
        InputActions.XRILeftHand.SecondaryButton.performed -= OnSecondaryButtonPerformed;
        InputActions.XRILeftHand.MenuButton.performed -= OnMenuButtonPerformed;
        InputActions.XRIRightHand.Move.performed -= OnRightHandMovedPerformed;

        // Left
        InputActions.XRILeftHand.PrimaryButton.Disable();
        InputActions.XRILeftHand.SecondaryButton.Disable();
        InputActions.XRILeftHand.Move.Disable();
        InputActions.XRILeftHand.Activate.Disable();
        InputActions.XRILeftHand.Select.Disable();
        InputActions.XRILeftHand.MenuButton.Disable();
        // Right
        InputActions.XRIRightHand.PrimaryButton.Disable();
        InputActions.XRIRightHand.SecondaryButton.Disable();
        InputActions.XRIRightHand.Move.Disable();
        InputActions.XRIRightHand.Select.Disable();
    }


    private void OnRightHandMovedPerformed(InputAction.CallbackContext context)
    {
        Vector2 moveDirection = context.ReadValue<Vector2>();
        Debug.Log("Right Hand Moved: " + moveDirection.ToString());
    }

    private void OnPrimaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Primary Button performed!");
    }

    private void OnSecondaryButtonPerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Secondary Button performed!");
    }

    private void OnActivatePerformed(InputAction.CallbackContext context)
    {
        Debug.Log("Activate action performed on the left hand!");
    }

}

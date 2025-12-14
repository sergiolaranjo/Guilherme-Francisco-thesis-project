using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class NavigationManager : MonoBehaviour
{
    private EventSystem eventSystem;

    private XRIDefaultInputActions inputActions;
    private void Start()
    {
        // Get the EventSystem component
        eventSystem = GetComponent<EventSystem>();

        // Use shared InputActionsManager instance instead of creating duplicate
        inputActions = InputActionsManager.Instance.InputActions;

        inputActions.XRIRightHand.Move.performed += OnRightHandMove;
    }

    private void OnRightHandMove(InputAction.CallbackContext context)
    {
        AxisEventData data = new AxisEventData(EventSystem.current);

        data.moveDir = GetMoveDirection(context.ReadValue<Vector2>());

        data.selectedObject = EventSystem.current.currentSelectedGameObject;

        ExecuteEvents.Execute(data.selectedObject, data, ExecuteEvents.moveHandler); ;
    }

    MoveDirection GetMoveDirection(Vector2 inputVector)
    {
        if (inputVector.magnitude < 0.1f)
        {
            return MoveDirection.None;
        }

        if (Mathf.Abs(inputVector.x) > Mathf.Abs(inputVector.y))
        {
            if (inputVector.x > 0)
            {
                return MoveDirection.Left;
            }
            else
            {
                return MoveDirection.Right;
            }
        }
        else
        {
            if (inputVector.y > 0)
            {
                return MoveDirection.Up;
            }
            else
            {
                return MoveDirection.Down;
            }
        }
    }
}

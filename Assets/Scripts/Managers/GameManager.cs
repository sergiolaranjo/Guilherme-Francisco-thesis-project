// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    public static GameManager Instance { get; private set; }
    public event EventHandler<OnScaleChangeEventArgs> OnScaleChange;
    public class OnScaleChangeEventArgs : EventArgs {
        public float scale;
    }

    [Header("3D Model")]
    [SerializeField] private GameObject model3D;

    [Header("Tag")]
    [SerializeField] private float sphereRadius = 1.0f;
    [SerializeField] private GameObject spherePrefab;

    [Header("Rotate")]
    [SerializeField] private float  rotateSpeed = 4.0f;

    [SerializeField] private float zoomSpeed = 1.0f;

    [Header("Translate")]
    [SerializeField] private float panSpeed = 2.0f;


    [Header("Scale")]
    [SerializeField] private float scaleMin = 0.001f;
	[SerializeField] private float scaleMax = 100f;

    [Header("Navigation")]
    [SerializeField] private Transform xrOrigin;

    [Header("Main Controller")]
    [SerializeField] private GameObject leftController;

    [Header("Camera")]
    [SerializeField] private GameObject cameraObject;

    private XRIDefaultInputActions inputActions;
	private ToolsPanelUI toolsPanelUI;

    private List<GameObject> sphereCreatedArray; 

	void Awake () {
        sphereCreatedArray = new List<GameObject>();
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Duplicate instance of GameManager. Destroying the duplicate.");
            Destroy(gameObject);
        }
	}

    void Start () {
        inputActions = InputActionsManager.Instance.InputActions;
        toolsPanelUI = ToolsPanelUI.Instance;
        toolsPanelUI.OnNavigateEvent += ToolsPanelUI_OnNavigateEvent;
        MenuToolsUI.Instance.OnLeaveButtonEvent += MenuToolsUI_OnLeaveButtonEvent;
    }

    private void ToolsPanelUI_OnNavigateEvent(object sender, EventArgs e)
    {
        float originalScale = ScaleManager.Instance.GetOriginalScale();
        ScaleManager.Instance.previousScale = model3D.transform.localScale.y;
        model3D.transform.localScale = new Vector3(originalScale, originalScale, originalScale);
        
        OnScaleChange?.Invoke(this, new OnScaleChangeEventArgs {
            scale = originalScale
        });

        Camera cameraComponent = cameraObject.GetComponent<Camera>();

        cameraComponent.nearClipPlane = 0.0008f;
        
        xrOrigin.localScale /= 70;
        sphereRadius /= 20;
        xrOrigin.position = model3D.transform.position;
        toolsPanelUI.SetNavigation(ToolsPanelUI.Navigation.Inside);
    }

    private void MenuToolsUI_OnLeaveButtonEvent(object sender, EventArgs e) {
        float previousScale = ScaleManager.Instance.previousScale;
        model3D.transform.localScale = new Vector3(previousScale, previousScale, previousScale);
        ScaleManager.Instance.currentScale = previousScale;
        xrOrigin.localScale *= 70;
        sphereRadius *= 20;

        Camera cameraComponent = cameraObject.GetComponent<Camera>();

        cameraComponent.nearClipPlane = 0.01f;

        xrOrigin.position = Vector3.zero;
        toolsPanelUI.SetMode(ToolsPanelUI.Modes.Default);
        toolsPanelUI.SetNavigation(ToolsPanelUI.Navigation.Outside);
    }

    void Update()
    {
        switch (toolsPanelUI.GetMode())
        {
            case ToolsPanelUI.Modes.Rotate:
                RotateModel();
                break;
            case ToolsPanelUI.Modes.Move:
                MoveGameObject(model3D, panSpeed);
                break;
            case ToolsPanelUI.Modes.Scale:
                ScaleModel();
                break;
            case ToolsPanelUI.Modes.Tag:
                HandleTag();
                break;
        }
    }

    // Move 
    public void MoveGameObject(GameObject gameObject, float speed)
    {
        Vector2 thumbstickInput = inputActions.XRILeftHand.Move.ReadValue<Vector2>();

        Debug.Log("Move vector:" + thumbstickInput.ToString());

        if (Mathf.Abs( inputActions.XRIRightHand.Move.ReadValue<Vector2>().y) > 0.5f)
        {
            Vector3 moveDirection = Mathf.Sign(inputActions.XRIRightHand.Move.ReadValue<Vector2>().y) * Camera.main.transform.up;
            float distance = speed * Time.deltaTime;
            gameObject.transform.Translate(moveDirection * distance, Space.World);
        }

        if (Mathf.Abs(thumbstickInput.x) > 0.5f)
        {
            float direction = Mathf.Sign(thumbstickInput.x);
            Vector3 translation = direction * Camera.main.transform.right * speed * Time.deltaTime;
            gameObject.transform.Translate(translation, Space.World);
        }

        if (Mathf.Abs(thumbstickInput.y) > 0.5f)
        {
            float direction = Mathf.Sign(thumbstickInput.y);
            Vector3 translation = direction * Camera.main.transform.forward * speed * Time.deltaTime;
            gameObject.transform.Translate(translation, Space.World);
        }
    }

    // Rotate

    private void RotateModel()
    {
        Vector2 thumbstickInput = inputActions.XRILeftHand.Move.ReadValue<Vector2>();

        Debug.Log("Thumbstick input: " + thumbstickInput.ToString());

        if (inputActions.XRILeftHand.PrimaryButton.IsPressed())
        {
            model3D.transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
        }

        if (inputActions.XRILeftHand.SecondaryButton.IsPressed())
        {
            model3D.transform.Rotate(Vector3.back, rotateSpeed * Time.deltaTime);
        }

        if (Mathf.Abs(thumbstickInput.x) > 0.5f)
        {
            float rotationAmount = Mathf.Sign(thumbstickInput.x) * rotateSpeed * Time.deltaTime;
            model3D.transform.Rotate(Vector3.up, rotationAmount);
        }

        if (Mathf.Abs(thumbstickInput.y) > 0.5f)
        {
            float rotationAmount = Mathf.Sign(thumbstickInput.y) * rotateSpeed * Time.deltaTime;
            model3D.transform.Rotate(Vector3.right, rotationAmount);
        }
    }


    // Scale 

    private void ScaleModel()
    {
        Vector2 thumbstickInput = inputActions.XRILeftHand.Move.ReadValue<Vector2>();

        float scale = Mathf.Clamp(model3D.transform.localScale[0], scaleMin, scaleMax);

        float scroll = thumbstickInput.y * Time.deltaTime;
        
        Debug.Log("Scale Factor:" + scroll.ToString());
        
        bool isScaling = Mathf.Abs(scroll) > 0;

        if (isScaling)
        {
            scale *= 1 + scroll * zoomSpeed;
            scale = Mathf.Clamp(scale, scaleMin, scaleMax);
            model3D.transform.localScale = new Vector3(scale, scale, scale);

            foreach (GameObject sphere in sphereCreatedArray) {
                sphere.transform.localScale = new Vector3(scale, scale, scale);
            }

            OnScaleChange?.Invoke(this, new OnScaleChangeEventArgs {
                scale = scale,
            });

            if (Mathf.Approximately(scroll, 0)) isScaling = false;
        }
    }

    // Tag
    public bool TryToCreateSphere(out GameObject sphere)
    {
        if (Physics.Raycast(leftController.transform.position,
            leftController.transform.forward, out RaycastHit hit))
        {
            if (!hit.collider.CompareTag("SpherePrefab"))
            {
                sphere = Instantiate(spherePrefab, hit.point, Quaternion.identity);
                sphere.transform.localScale = Vector3.one * sphereRadius * 2f;

                sphere.transform.SetParent(hit.collider.transform);

                sphereCreatedArray.Add(sphere);
                return true;
            }
        }

        sphere = null;
        return false;
    }

    public bool TryToCreatePrefab(GameObject prefab, GameObject parent)
    {
        if (Physics.Raycast(leftController.transform.position,
            leftController.transform.forward, out RaycastHit hit))
        {
            if (!hit.collider.CompareTag("MeasurementPrefab"))
            {
                prefab = Instantiate(prefab, hit.point, Quaternion.identity, parent.transform);
                prefab.transform.localScale = 2f * sphereRadius * Vector3.one;

                return true;
            }
        }
        return false;
    }

    private void HandleTag()
    {
        if (inputActions.XRILeftHand.PrimaryButton.triggered)
        {
            bool success = TryToCreateSphere(out GameObject sphere);

            if (success)
            {
                Debug.Log("Sphere created successfully!");
            }
            else
            {
                // Sphere creation failed
                Debug.Log("Failed to create sphere.");
            }
        }
        else if (inputActions.XRILeftHand.SecondaryButton.triggered)
        {
            RaycastHit hit;
            if (Physics.Raycast(leftController.transform.position,
            leftController.transform.forward, out hit))
            {
                if (hit.collider.CompareTag("SpherePrefab"))
                {
                    Destroy(hit.collider.gameObject);
                }
            }

        }
    }


    public float GetSphereRadiusSize() {
        return sphereRadius;
    }
}

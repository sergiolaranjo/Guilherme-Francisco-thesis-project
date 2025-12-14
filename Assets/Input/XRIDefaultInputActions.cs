// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================
//
// IMPORTANT: This file is auto-generated from "XRI Default Input Actions.inputactions"
// Do not modify this file directly. Regenerate it from the Input Actions asset.
//

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public partial class @XRIDefaultInputActions : IInputActionCollection2, IDisposable
{
    public InputActionAsset asset { get; }
    public @XRIDefaultInputActions()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""XRI Default Input Actions"",
    ""maps"": [
        {
            ""name"": ""XRILeftHand"",
            ""id"": ""5fe596f9-1b7b-49b7-80a7-3b5195caf43d"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""9693e25f-8a4f-4aed-842f-3961243c69a1"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Activate"",
                    ""type"": ""Button"",
                    ""id"": ""aa8ff983-0f46-4c89-b1c4-e727e0a3f1f0"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Select"",
                    ""type"": ""Button"",
                    ""id"": ""33754c03-48ec-46ef-9bc6-22ed6bfdd8e8"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""PrimaryButton"",
                    ""type"": ""Button"",
                    ""id"": ""95fa1c5a-d0c8-4f8c-8d68-c3ab8e2f1091"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""SecondaryButton"",
                    ""type"": ""Button"",
                    ""id"": ""85fa2c5b-e1d9-5f9d-9e79-d4bc9f3f2192"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""MenuButton"",
                    ""type"": ""Button"",
                    ""id"": ""75fa3c6c-f2ea-6a0e-0f8a-e5cd0a4a3293"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": ""Tap;Hold"",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""1bea6b0f-3c67-4a7c-b9d8-e1f2a3b4c5d6"",
                    ""path"": ""<XRController>{LeftHand}/thumbstick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""2cfa7b1a-4d78-5b8d-c0e9-f2a3b4c5d6e7"",
                    ""path"": ""<XRController>{LeftHand}/triggerPressed"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""Activate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3dfb8c2b-5e89-6c9e-d1f0-a3b4c5d6e7f8"",
                    ""path"": ""<XRController>{LeftHand}/gripPressed"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""Select"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""4eac9d3c-6f9a-7d0f-e2a1-b4c5d6e7f8a9"",
                    ""path"": ""<XRController>{LeftHand}/primaryButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""PrimaryButton"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""5fbd0e4d-7a0b-8e1a-f3b2-c5d6e7f8a9b0"",
                    ""path"": ""<XRController>{LeftHand}/secondaryButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""SecondaryButton"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""6ace1f5e-8b1c-9f2b-a4c3-d6e7f8a9b0c1"",
                    ""path"": ""<XRController>{LeftHand}/menuButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""MenuButton"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        },
        {
            ""name"": ""XRIRightHand"",
            ""id"": ""6ab897fa-2c8c-5ab8-91b8-4c6206dbf54e"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""a9794a26-d5d1-4d87-b8f6-5a8b9c0d1e2f"",
                    ""expectedControlType"": ""Vector2"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": true
                },
                {
                    ""name"": ""Activate"",
                    ""type"": ""Button"",
                    ""id"": ""b0895b37-e6e2-5e98-c9a7-6b9c0d1e2f3a"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""Select"",
                    ""type"": ""Button"",
                    ""id"": ""c1906c48-f7f3-6fa9-d0b8-7c0d1e2f3a4b"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""PrimaryButton"",
                    ""type"": ""Button"",
                    ""id"": ""d2017d59-a8a4-7ab0-e1c9-8d1e2f3a4b5c"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                },
                {
                    ""name"": ""SecondaryButton"",
                    ""type"": ""Button"",
                    ""id"": ""e3128e6a-b9b5-8bc1-f2d0-9e2f3a4b5c6d"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """",
                    ""initialStateCheck"": false
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""7bdf2a6f-9c2d-0a3c-b5d4-e8f9a0b1c2d3"",
                    ""path"": ""<XRController>{RightHand}/thumbstick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""8cea3b7a-0d3e-1b4d-c6e5-f9a0b1c2d3e4"",
                    ""path"": ""<XRController>{RightHand}/triggerPressed"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""Activate"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""9dfb4c8b-1e4f-2c5e-d7f6-a0b1c2d3e4f5"",
                    ""path"": ""<XRController>{RightHand}/gripPressed"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""Select"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""0eac5d9c-2f5a-3d6f-e8a7-b1c2d3e4f5a6"",
                    ""path"": ""<XRController>{RightHand}/primaryButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""PrimaryButton"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""1fbd6e0d-3a6b-4e7a-f9b8-c2d3e4f5a6b7"",
                    ""path"": ""<XRController>{RightHand}/secondaryButton"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""XR"",
                    ""action"": ""SecondaryButton"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""XR"",
            ""bindingGroup"": ""XR"",
            ""devices"": [
                {
                    ""devicePath"": ""<XRController>"",
                    ""isOptional"": false,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // XRILeftHand
        m_XRILeftHand = asset.FindActionMap("XRILeftHand", throwIfNotFound: true);
        m_XRILeftHand_Move = m_XRILeftHand.FindAction("Move", throwIfNotFound: true);
        m_XRILeftHand_Activate = m_XRILeftHand.FindAction("Activate", throwIfNotFound: true);
        m_XRILeftHand_Select = m_XRILeftHand.FindAction("Select", throwIfNotFound: true);
        m_XRILeftHand_PrimaryButton = m_XRILeftHand.FindAction("PrimaryButton", throwIfNotFound: true);
        m_XRILeftHand_SecondaryButton = m_XRILeftHand.FindAction("SecondaryButton", throwIfNotFound: true);
        m_XRILeftHand_MenuButton = m_XRILeftHand.FindAction("MenuButton", throwIfNotFound: true);
        // XRIRightHand
        m_XRIRightHand = asset.FindActionMap("XRIRightHand", throwIfNotFound: true);
        m_XRIRightHand_Move = m_XRIRightHand.FindAction("Move", throwIfNotFound: true);
        m_XRIRightHand_Activate = m_XRIRightHand.FindAction("Activate", throwIfNotFound: true);
        m_XRIRightHand_Select = m_XRIRightHand.FindAction("Select", throwIfNotFound: true);
        m_XRIRightHand_PrimaryButton = m_XRIRightHand.FindAction("PrimaryButton", throwIfNotFound: true);
        m_XRIRightHand_SecondaryButton = m_XRIRightHand.FindAction("SecondaryButton", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    public IEnumerable<InputBinding> bindings => asset.bindings;

    public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false)
    {
        return asset.FindAction(actionNameOrId, throwIfNotFound);
    }

    public int FindBinding(InputBinding bindingMask, out InputAction action)
    {
        return asset.FindBinding(bindingMask, out action);
    }

    // XRILeftHand
    private readonly InputActionMap m_XRILeftHand;
    private readonly InputAction m_XRILeftHand_Move;
    private readonly InputAction m_XRILeftHand_Activate;
    private readonly InputAction m_XRILeftHand_Select;
    private readonly InputAction m_XRILeftHand_PrimaryButton;
    private readonly InputAction m_XRILeftHand_SecondaryButton;
    private readonly InputAction m_XRILeftHand_MenuButton;

    public struct XRILeftHandActions
    {
        private @XRIDefaultInputActions m_Wrapper;
        public XRILeftHandActions(@XRIDefaultInputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_XRILeftHand_Move;
        public InputAction @Activate => m_Wrapper.m_XRILeftHand_Activate;
        public InputAction @Select => m_Wrapper.m_XRILeftHand_Select;
        public InputAction @PrimaryButton => m_Wrapper.m_XRILeftHand_PrimaryButton;
        public InputAction @SecondaryButton => m_Wrapper.m_XRILeftHand_SecondaryButton;
        public InputAction @MenuButton => m_Wrapper.m_XRILeftHand_MenuButton;
        public InputActionMap Get() { return m_Wrapper.m_XRILeftHand; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(XRILeftHandActions set) { return set.Get(); }
    }
    public XRILeftHandActions @XRILeftHand => new XRILeftHandActions(this);

    // XRIRightHand
    private readonly InputActionMap m_XRIRightHand;
    private readonly InputAction m_XRIRightHand_Move;
    private readonly InputAction m_XRIRightHand_Activate;
    private readonly InputAction m_XRIRightHand_Select;
    private readonly InputAction m_XRIRightHand_PrimaryButton;
    private readonly InputAction m_XRIRightHand_SecondaryButton;

    public struct XRIRightHandActions
    {
        private @XRIDefaultInputActions m_Wrapper;
        public XRIRightHandActions(@XRIDefaultInputActions wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_XRIRightHand_Move;
        public InputAction @Activate => m_Wrapper.m_XRIRightHand_Activate;
        public InputAction @Select => m_Wrapper.m_XRIRightHand_Select;
        public InputAction @PrimaryButton => m_Wrapper.m_XRIRightHand_PrimaryButton;
        public InputAction @SecondaryButton => m_Wrapper.m_XRIRightHand_SecondaryButton;
        public InputActionMap Get() { return m_Wrapper.m_XRIRightHand; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(XRIRightHandActions set) { return set.Get(); }
    }
    public XRIRightHandActions @XRIRightHand => new XRIRightHandActions(this);

    private int m_XRSchemeIndex = -1;
    public InputControlScheme XRScheme
    {
        get
        {
            if (m_XRSchemeIndex == -1) m_XRSchemeIndex = asset.FindControlSchemeIndex("XR");
            return asset.controlSchemes[m_XRSchemeIndex];
        }
    }
}

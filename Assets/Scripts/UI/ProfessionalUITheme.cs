// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CardiacVR.UI
{
    /// <summary>
    /// Professional UI Theme Manager for medical VR application.
    /// Provides elegant, modern styling for all UI elements.
    /// </summary>
    public class ProfessionalUITheme : MonoBehaviour
{
    public static ProfessionalUITheme Instance { get; private set; }

    public event EventHandler<ThemeChangedEventArgs> OnThemeChanged;

    public class ThemeChangedEventArgs : EventArgs
    {
        public UITheme Theme;
    }

    public enum UITheme
    {
        Dark,           // Professional dark theme
        Light,          // Clean light theme
        Medical,        // Medical blue theme
        Surgery,        // Operating room green
        Cardiology,     // Cardiac red accent
        Custom
    }

    [System.Serializable]
    public class ThemeColors
    {
        [Header("Primary Colors")]
        public Color Primary = new Color(0.2f, 0.4f, 0.8f);
        public Color PrimaryDark = new Color(0.15f, 0.3f, 0.6f);
        public Color PrimaryLight = new Color(0.3f, 0.5f, 0.9f);
        public Color Accent = new Color(0.1f, 0.8f, 0.6f);

        [Header("Background Colors")]
        public Color Background = new Color(0.08f, 0.09f, 0.12f);
        public Color BackgroundSecondary = new Color(0.12f, 0.14f, 0.18f);
        public Color Surface = new Color(0.15f, 0.17f, 0.22f);
        public Color SurfaceElevated = new Color(0.18f, 0.2f, 0.26f);

        [Header("Text Colors")]
        public Color TextPrimary = new Color(0.95f, 0.95f, 0.97f);
        public Color TextSecondary = new Color(0.7f, 0.72f, 0.78f);
        public Color TextDisabled = new Color(0.45f, 0.47f, 0.52f);
        public Color TextOnPrimary = Color.white;

        [Header("State Colors")]
        public Color Success = new Color(0.2f, 0.8f, 0.4f);
        public Color Warning = new Color(0.95f, 0.75f, 0.2f);
        public Color Error = new Color(0.9f, 0.25f, 0.3f);
        public Color Info = new Color(0.3f, 0.6f, 0.95f);

        [Header("Interactive Colors")]
        public Color ButtonNormal = new Color(0.25f, 0.28f, 0.35f);
        public Color ButtonHover = new Color(0.3f, 0.33f, 0.42f);
        public Color ButtonPressed = new Color(0.2f, 0.22f, 0.28f);
        public Color ButtonDisabled = new Color(0.18f, 0.2f, 0.25f);

        [Header("Border & Dividers")]
        public Color Border = new Color(0.3f, 0.32f, 0.38f, 0.5f);
        public Color Divider = new Color(0.25f, 0.27f, 0.32f, 0.3f);

        [Header("Overlay Colors")]
        public Color OverlayLight = new Color(1f, 1f, 1f, 0.05f);
        public Color OverlayDark = new Color(0f, 0f, 0f, 0.4f);

        [Header("Glass Effect")]
        public Color GlassBackground = new Color(0.1f, 0.12f, 0.16f, 0.85f);
        public Color GlassBorder = new Color(0.4f, 0.42f, 0.48f, 0.3f);
    }

    [System.Serializable]
    public class ThemeTypography
    {
        public TMP_FontAsset PrimaryFont;
        public TMP_FontAsset SecondaryFont;
        public TMP_FontAsset MonospaceFont;

        [Header("Font Sizes")]
        public float HeaderLarge = 28f;
        public float HeaderMedium = 22f;
        public float HeaderSmall = 18f;
        public float BodyLarge = 16f;
        public float BodyMedium = 14f;
        public float BodySmall = 12f;
        public float Caption = 10f;

        [Header("Font Weights")]
        public FontWeight HeaderWeight = FontWeight.SemiBold;
        public FontWeight BodyWeight = FontWeight.Regular;
        public FontWeight ButtonWeight = FontWeight.Medium;
    }

    [System.Serializable]
    public class ThemeSpacing
    {
        public float XSmall = 4f;
        public float Small = 8f;
        public float Medium = 16f;
        public float Large = 24f;
        public float XLarge = 32f;
        public float XXLarge = 48f;
    }

    [System.Serializable]
    public class ThemeBorders
    {
        public float RadiusSmall = 4f;
        public float RadiusMedium = 8f;
        public float RadiusLarge = 12f;
        public float RadiusXLarge = 16f;
        public float RadiusFull = 999f;

        public float WidthThin = 1f;
        public float WidthMedium = 2f;
        public float WidthThick = 3f;
    }

    [System.Serializable]
    public class ThemeShadows
    {
        public Color ShadowColor = new Color(0f, 0f, 0f, 0.3f);
        public Vector2 ShadowOffsetSmall = new Vector2(0, 2);
        public Vector2 ShadowOffsetMedium = new Vector2(0, 4);
        public Vector2 ShadowOffsetLarge = new Vector2(0, 8);
        public float ShadowBlurSmall = 4f;
        public float ShadowBlurMedium = 8f;
        public float ShadowBlurLarge = 16f;
    }

    [Header("Current Theme")]
    [SerializeField] private UITheme currentTheme = UITheme.Dark;
    [SerializeField] private ThemeColors colors = new ThemeColors();
    [SerializeField] private ThemeTypography typography = new ThemeTypography();
    [SerializeField] private ThemeSpacing spacing = new ThemeSpacing();
    [SerializeField] private ThemeBorders borders = new ThemeBorders();
    [SerializeField] private ThemeShadows shadows = new ThemeShadows();

    [Header("Animation")]
    [SerializeField] private float transitionDuration = 0.25f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Icons")]
    [SerializeField] private Sprite iconClose;
    [SerializeField] private Sprite iconMenu;
    [SerializeField] private Sprite iconSettings;
    [SerializeField] private Sprite iconInfo;
    [SerializeField] private Sprite iconWarning;
    [SerializeField] private Sprite iconError;
    [SerializeField] private Sprite iconSuccess;
    [SerializeField] private Sprite iconArrowLeft;
    [SerializeField] private Sprite iconArrowRight;
    [SerializeField] private Sprite iconArrowUp;
    [SerializeField] private Sprite iconArrowDown;

    // Registered UI elements
    private List<ThemedElement> themedElements = new List<ThemedElement>();

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
        ApplyTheme(currentTheme);
    }

    #region Theme Application

    /// <summary>
    /// Apply a UI theme.
    /// </summary>
    public void ApplyTheme(UITheme theme)
    {
        currentTheme = theme;
        colors = GetThemeColors(theme);

        // Update all registered elements
        foreach (var element in themedElements)
        {
            if (element != null && element.gameObject != null)
            {
                ApplyThemeToElement(element);
            }
        }

        OnThemeChanged?.Invoke(this, new ThemeChangedEventArgs { Theme = theme });

        Debug.Log($"Applied UI theme: {theme}");
    }

    private ThemeColors GetThemeColors(UITheme theme)
    {
        switch (theme)
        {
            case UITheme.Dark:
                return new ThemeColors
                {
                    Primary = new Color(0.25f, 0.5f, 0.95f),
                    PrimaryDark = new Color(0.18f, 0.38f, 0.75f),
                    PrimaryLight = new Color(0.4f, 0.65f, 1f),
                    Accent = new Color(0.1f, 0.85f, 0.65f),
                    Background = new Color(0.06f, 0.07f, 0.1f),
                    BackgroundSecondary = new Color(0.1f, 0.11f, 0.15f),
                    Surface = new Color(0.13f, 0.15f, 0.2f),
                    SurfaceElevated = new Color(0.17f, 0.19f, 0.25f),
                    TextPrimary = new Color(0.95f, 0.96f, 0.98f),
                    TextSecondary = new Color(0.68f, 0.7f, 0.76f),
                    TextDisabled = new Color(0.42f, 0.44f, 0.5f),
                    ButtonNormal = new Color(0.22f, 0.24f, 0.32f),
                    ButtonHover = new Color(0.28f, 0.31f, 0.4f),
                    ButtonPressed = new Color(0.18f, 0.2f, 0.26f),
                    GlassBackground = new Color(0.08f, 0.1f, 0.14f, 0.9f),
                    GlassBorder = new Color(0.35f, 0.38f, 0.45f, 0.25f)
                };

            case UITheme.Light:
                return new ThemeColors
                {
                    Primary = new Color(0.2f, 0.45f, 0.9f),
                    PrimaryDark = new Color(0.15f, 0.35f, 0.7f),
                    PrimaryLight = new Color(0.35f, 0.6f, 0.98f),
                    Accent = new Color(0.05f, 0.75f, 0.55f),
                    Background = new Color(0.96f, 0.97f, 0.98f),
                    BackgroundSecondary = new Color(0.92f, 0.93f, 0.95f),
                    Surface = new Color(1f, 1f, 1f),
                    SurfaceElevated = new Color(1f, 1f, 1f),
                    TextPrimary = new Color(0.1f, 0.12f, 0.18f),
                    TextSecondary = new Color(0.4f, 0.42f, 0.48f),
                    TextDisabled = new Color(0.65f, 0.67f, 0.72f),
                    ButtonNormal = new Color(0.88f, 0.89f, 0.92f),
                    ButtonHover = new Color(0.82f, 0.84f, 0.88f),
                    ButtonPressed = new Color(0.78f, 0.8f, 0.84f),
                    GlassBackground = new Color(1f, 1f, 1f, 0.92f),
                    GlassBorder = new Color(0.75f, 0.77f, 0.82f, 0.3f)
                };

            case UITheme.Medical:
                return new ThemeColors
                {
                    Primary = new Color(0.18f, 0.55f, 0.85f),
                    PrimaryDark = new Color(0.12f, 0.42f, 0.68f),
                    PrimaryLight = new Color(0.32f, 0.68f, 0.95f),
                    Accent = new Color(0.15f, 0.78f, 0.6f),
                    Background = new Color(0.94f, 0.96f, 0.98f),
                    BackgroundSecondary = new Color(0.88f, 0.92f, 0.96f),
                    Surface = new Color(1f, 1f, 1f),
                    SurfaceElevated = new Color(1f, 1f, 1f),
                    TextPrimary = new Color(0.12f, 0.18f, 0.28f),
                    TextSecondary = new Color(0.35f, 0.42f, 0.52f),
                    TextDisabled = new Color(0.6f, 0.65f, 0.72f),
                    ButtonNormal = new Color(0.18f, 0.55f, 0.85f),
                    ButtonHover = new Color(0.22f, 0.62f, 0.92f),
                    ButtonPressed = new Color(0.12f, 0.45f, 0.72f),
                    GlassBackground = new Color(0.96f, 0.98f, 1f, 0.95f),
                    GlassBorder = new Color(0.18f, 0.55f, 0.85f, 0.2f)
                };

            case UITheme.Surgery:
                return new ThemeColors
                {
                    Primary = new Color(0.15f, 0.65f, 0.45f),
                    PrimaryDark = new Color(0.1f, 0.52f, 0.35f),
                    PrimaryLight = new Color(0.25f, 0.78f, 0.58f),
                    Accent = new Color(0.22f, 0.72f, 0.88f),
                    Background = new Color(0.05f, 0.08f, 0.08f),
                    BackgroundSecondary = new Color(0.08f, 0.12f, 0.11f),
                    Surface = new Color(0.1f, 0.15f, 0.14f),
                    SurfaceElevated = new Color(0.13f, 0.18f, 0.17f),
                    TextPrimary = new Color(0.92f, 0.96f, 0.94f),
                    TextSecondary = new Color(0.65f, 0.72f, 0.68f),
                    TextDisabled = new Color(0.4f, 0.46f, 0.44f),
                    ButtonNormal = new Color(0.15f, 0.65f, 0.45f),
                    ButtonHover = new Color(0.18f, 0.75f, 0.52f),
                    ButtonPressed = new Color(0.1f, 0.55f, 0.38f),
                    GlassBackground = new Color(0.05f, 0.12f, 0.1f, 0.92f),
                    GlassBorder = new Color(0.15f, 0.65f, 0.45f, 0.25f)
                };

            case UITheme.Cardiology:
                return new ThemeColors
                {
                    Primary = new Color(0.85f, 0.22f, 0.32f),
                    PrimaryDark = new Color(0.68f, 0.15f, 0.25f),
                    PrimaryLight = new Color(0.95f, 0.35f, 0.45f),
                    Accent = new Color(0.25f, 0.52f, 0.88f),
                    Background = new Color(0.08f, 0.06f, 0.07f),
                    BackgroundSecondary = new Color(0.12f, 0.1f, 0.11f),
                    Surface = new Color(0.16f, 0.13f, 0.14f),
                    SurfaceElevated = new Color(0.2f, 0.16f, 0.18f),
                    TextPrimary = new Color(0.98f, 0.95f, 0.96f),
                    TextSecondary = new Color(0.75f, 0.68f, 0.7f),
                    TextDisabled = new Color(0.48f, 0.42f, 0.44f),
                    ButtonNormal = new Color(0.85f, 0.22f, 0.32f),
                    ButtonHover = new Color(0.92f, 0.3f, 0.4f),
                    ButtonPressed = new Color(0.72f, 0.18f, 0.26f),
                    GlassBackground = new Color(0.12f, 0.08f, 0.1f, 0.92f),
                    GlassBorder = new Color(0.85f, 0.22f, 0.32f, 0.25f)
                };

            default:
                return colors;
        }
    }

    #endregion

    #region Element Registration

    /// <summary>
    /// Register a UI element for theming.
    /// </summary>
    public void RegisterElement(ThemedElement element)
    {
        if (!themedElements.Contains(element))
        {
            themedElements.Add(element);
            ApplyThemeToElement(element);
        }
    }

    /// <summary>
    /// Unregister a UI element.
    /// </summary>
    public void UnregisterElement(ThemedElement element)
    {
        themedElements.Remove(element);
    }

    private void ApplyThemeToElement(ThemedElement element)
    {
        if (element == null) return;

        switch (element.ElementType)
        {
            case ElementType.Panel:
                ApplyPanelStyle(element);
                break;
            case ElementType.Button:
                ApplyButtonStyle(element);
                break;
            case ElementType.Text:
                ApplyTextStyle(element);
                break;
            case ElementType.Input:
                ApplyInputStyle(element);
                break;
            case ElementType.Toggle:
                ApplyToggleStyle(element);
                break;
            case ElementType.Slider:
                ApplySliderStyle(element);
                break;
            case ElementType.Card:
                ApplyCardStyle(element);
                break;
            case ElementType.Header:
                ApplyHeaderStyle(element);
                break;
        }
    }

    #endregion

    #region Style Application

    private void ApplyPanelStyle(ThemedElement element)
    {
        Image image = element.GetComponent<Image>();
        if (image != null)
        {
            image.color = element.UseGlass ? colors.GlassBackground : colors.Surface;
        }
    }

    private void ApplyButtonStyle(ThemedElement element)
    {
        Button button = element.GetComponent<Button>();
        Image image = element.GetComponent<Image>();
        TMP_Text text = element.GetComponentInChildren<TMP_Text>();

        if (image != null)
        {
            if (element.IsPrimary)
            {
                image.color = colors.Primary;
            }
            else
            {
                image.color = colors.ButtonNormal;
            }
        }

        if (button != null)
        {
            ColorBlock colorBlock = button.colors;

            if (element.IsPrimary)
            {
                colorBlock.normalColor = colors.Primary;
                colorBlock.highlightedColor = colors.PrimaryLight;
                colorBlock.pressedColor = colors.PrimaryDark;
                colorBlock.selectedColor = colors.Primary;
            }
            else
            {
                colorBlock.normalColor = colors.ButtonNormal;
                colorBlock.highlightedColor = colors.ButtonHover;
                colorBlock.pressedColor = colors.ButtonPressed;
                colorBlock.selectedColor = colors.ButtonNormal;
            }

            colorBlock.disabledColor = colors.ButtonDisabled;
            colorBlock.fadeDuration = 0.1f;
            button.colors = colorBlock;
        }

        if (text != null)
        {
            text.color = element.IsPrimary ? colors.TextOnPrimary : colors.TextPrimary;
            text.fontSize = typography.BodyMedium;
            text.fontWeight = typography.ButtonWeight;
        }
    }

    private void ApplyTextStyle(ThemedElement element)
    {
        TMP_Text text = element.GetComponent<TMP_Text>();
        if (text == null) return;

        switch (element.TextStyle)
        {
            case TextStyle.HeaderLarge:
                text.fontSize = typography.HeaderLarge;
                text.fontWeight = typography.HeaderWeight;
                text.color = colors.TextPrimary;
                break;
            case TextStyle.HeaderMedium:
                text.fontSize = typography.HeaderMedium;
                text.fontWeight = typography.HeaderWeight;
                text.color = colors.TextPrimary;
                break;
            case TextStyle.HeaderSmall:
                text.fontSize = typography.HeaderSmall;
                text.fontWeight = typography.HeaderWeight;
                text.color = colors.TextPrimary;
                break;
            case TextStyle.BodyLarge:
                text.fontSize = typography.BodyLarge;
                text.fontWeight = typography.BodyWeight;
                text.color = colors.TextPrimary;
                break;
            case TextStyle.BodyMedium:
                text.fontSize = typography.BodyMedium;
                text.fontWeight = typography.BodyWeight;
                text.color = colors.TextPrimary;
                break;
            case TextStyle.BodySmall:
                text.fontSize = typography.BodySmall;
                text.fontWeight = typography.BodyWeight;
                text.color = colors.TextSecondary;
                break;
            case TextStyle.Caption:
                text.fontSize = typography.Caption;
                text.fontWeight = typography.BodyWeight;
                text.color = colors.TextSecondary;
                break;
        }
    }

    private void ApplyInputStyle(ThemedElement element)
    {
        TMP_InputField input = element.GetComponent<TMP_InputField>();
        Image image = element.GetComponent<Image>();

        if (image != null)
        {
            image.color = colors.BackgroundSecondary;
        }

        if (input != null)
        {
            input.textComponent.color = colors.TextPrimary;
            input.textComponent.fontSize = typography.BodyMedium;

            if (input.placeholder is TMP_Text placeholder)
            {
                placeholder.color = colors.TextDisabled;
            }
        }
    }

    private void ApplyToggleStyle(ThemedElement element)
    {
        Toggle toggle = element.GetComponent<Toggle>();
        if (toggle == null) return;

        Image background = toggle.targetGraphic as Image;
        Image checkmark = toggle.graphic as Image;

        if (background != null)
        {
            background.color = colors.ButtonNormal;
        }

        if (checkmark != null)
        {
            checkmark.color = colors.Primary;
        }

        ColorBlock colorBlock = toggle.colors;
        colorBlock.normalColor = colors.ButtonNormal;
        colorBlock.highlightedColor = colors.ButtonHover;
        colorBlock.pressedColor = colors.ButtonPressed;
        colorBlock.selectedColor = colors.Primary;
        toggle.colors = colorBlock;
    }

    private void ApplySliderStyle(ThemedElement element)
    {
        Slider slider = element.GetComponent<Slider>();
        if (slider == null) return;

        // Background
        Image background = slider.GetComponentInChildren<Image>();
        if (background != null && background.name == "Background")
        {
            background.color = colors.BackgroundSecondary;
        }

        // Fill
        if (slider.fillRect != null)
        {
            Image fill = slider.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                fill.color = colors.Primary;
            }
        }

        // Handle
        if (slider.handleRect != null)
        {
            Image handle = slider.handleRect.GetComponent<Image>();
            if (handle != null)
            {
                handle.color = colors.PrimaryLight;
            }
        }
    }

    private void ApplyCardStyle(ThemedElement element)
    {
        Image image = element.GetComponent<Image>();
        if (image != null)
        {
            image.color = colors.SurfaceElevated;
        }

        // Apply shadow if available
        Shadow shadow = element.GetComponent<Shadow>();
        if (shadow != null)
        {
            shadow.effectColor = shadows.ShadowColor;
            shadow.effectDistance = shadows.ShadowOffsetMedium;
        }
    }

    private void ApplyHeaderStyle(ThemedElement element)
    {
        Image image = element.GetComponent<Image>();
        if (image != null)
        {
            image.color = colors.Primary;
        }

        TMP_Text text = element.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.color = colors.TextOnPrimary;
            text.fontSize = typography.HeaderMedium;
            text.fontWeight = typography.HeaderWeight;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Get current theme colors.
    /// </summary>
    public ThemeColors GetColors() => colors;

    /// <summary>
    /// Get current typography settings.
    /// </summary>
    public ThemeTypography GetTypography() => typography;

    /// <summary>
    /// Get current spacing settings.
    /// </summary>
    public ThemeSpacing GetSpacing() => spacing;

    /// <summary>
    /// Get current border settings.
    /// </summary>
    public ThemeBorders GetBorders() => borders;

    /// <summary>
    /// Get current theme.
    /// </summary>
    public UITheme GetCurrentTheme() => currentTheme;

    /// <summary>
    /// Create a styled button.
    /// </summary>
    public Button CreateStyledButton(Transform parent, string text, bool isPrimary = false)
    {
        GameObject buttonObj = new GameObject("StyledButton");
        buttonObj.transform.SetParent(parent);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 48);

        Image image = buttonObj.AddComponent<Image>();
        image.color = isPrimary ? colors.Primary : colors.ButtonNormal;

        Button button = buttonObj.AddComponent<Button>();

        // Add text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = isPrimary ? colors.TextOnPrimary : colors.TextPrimary;
        tmpText.fontSize = typography.BodyMedium;

        // Register for theming
        ThemedElement themed = buttonObj.AddComponent<ThemedElement>();
        themed.ElementType = ElementType.Button;
        themed.IsPrimary = isPrimary;
        RegisterElement(themed);

        return button;
    }

    #endregion
}

    #region Supporting Types

    public enum ElementType
    {
        Panel,
        Button,
        Text,
        Input,
        Toggle,
        Slider,
        Card,
        Header,
        Custom
    }

    public enum TextStyle
    {
        HeaderLarge,
        HeaderMedium,
        HeaderSmall,
        BodyLarge,
        BodyMedium,
        BodySmall,
        Caption
    }

    /// <summary>
    /// Component to mark UI elements for theming.
    /// </summary>
    public class ThemedElement : MonoBehaviour
    {
        public ElementType ElementType = ElementType.Panel;
        public TextStyle TextStyle = TextStyle.BodyMedium;
        public bool IsPrimary = false;
        public bool UseGlass = false;
        public bool AutoRegister = true;

        void Start()
        {
            if (AutoRegister && ProfessionalUITheme.Instance != null)
            {
                ProfessionalUITheme.Instance.RegisterElement(this);
            }
        }

        void OnDestroy()
        {
            if (ProfessionalUITheme.Instance != null)
            {
                ProfessionalUITheme.Instance.UnregisterElement(this);
            }
        }
    }

    #endregion
}

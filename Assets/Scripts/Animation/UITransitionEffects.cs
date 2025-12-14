using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace CardiacVR.Animation
{
    /// <summary>
    /// Pre-built elegant UI transition effects
    /// </summary>
    public class UITransitionEffects : MonoBehaviour
    {
        public static UITransitionEffects Instance { get; private set; }

        [Header("Transition Settings")]
        [SerializeField] private float buttonHoverScale = 1.05f;
        [SerializeField] private float buttonClickScale = 0.95f;
        [SerializeField] private float hoverDuration = 0.15f;
        [SerializeField] private float clickDuration = 0.1f;

        [Header("Modal Settings")]
        [SerializeField] private float modalScaleStart = 0.8f;
        [SerializeField] private float modalDuration = 0.35f;
        [SerializeField] private float overlayFadeDuration = 0.25f;

        [Header("List Settings")]
        [SerializeField] private float listItemStagger = 0.05f;
        [SerializeField] private float listItemDuration = 0.3f;

        [Header("Notification Settings")]
        [SerializeField] private float notificationSlideDistance = 100f;
        [SerializeField] private float notificationDuration = 0.4f;
        [SerializeField] private float notificationStayDuration = 3f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Button Effects

        public void SetupButtonHover(Button button)
        {
            var hover = button.gameObject.AddComponent<ButtonHoverEffect>();
            hover.Initialize(buttonHoverScale, buttonClickScale, hoverDuration, clickDuration);
        }

        public void SetupAllButtonsInCanvas(Canvas canvas)
        {
            var buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (button.GetComponent<ButtonHoverEffect>() == null)
                {
                    SetupButtonHover(button);
                }
            }
        }

        #endregion

        #region Modal Dialogs

        public void ShowModal(RectTransform modal, CanvasGroup overlay = null, System.Action onComplete = null)
        {
            StartCoroutine(ShowModalCoroutine(modal, overlay, onComplete));
        }

        private IEnumerator ShowModalCoroutine(RectTransform modal, CanvasGroup overlay, System.Action onComplete)
        {
            // Setup initial state
            modal.gameObject.SetActive(true);
            modal.localScale = Vector3.one * modalScaleStart;

            var modalCanvasGroup = modal.GetComponent<CanvasGroup>();
            if (modalCanvasGroup == null)
                modalCanvasGroup = modal.gameObject.AddComponent<CanvasGroup>();

            modalCanvasGroup.alpha = 0;
            modalCanvasGroup.interactable = false;
            modalCanvasGroup.blocksRaycasts = false;

            // Animate overlay if present
            if (overlay != null)
            {
                overlay.gameObject.SetActive(true);
                overlay.alpha = 0;
                SmoothAnimationSystem.Instance.FadeCanvasGroup(overlay, 0.6f, overlayFadeDuration);
            }

            // Animate modal
            float elapsed = 0;
            Vector3 startScale = modal.localScale;

            while (elapsed < modalDuration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / modalDuration, SmoothAnimationSystem.EaseType.BackOut);

                modal.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                modalCanvasGroup.alpha = t;

                yield return null;
            }

            modal.localScale = Vector3.one;
            modalCanvasGroup.alpha = 1;
            modalCanvasGroup.interactable = true;
            modalCanvasGroup.blocksRaycasts = true;

            onComplete?.Invoke();
        }

        public void HideModal(RectTransform modal, CanvasGroup overlay = null, System.Action onComplete = null)
        {
            StartCoroutine(HideModalCoroutine(modal, overlay, onComplete));
        }

        private IEnumerator HideModalCoroutine(RectTransform modal, CanvasGroup overlay, System.Action onComplete)
        {
            var modalCanvasGroup = modal.GetComponent<CanvasGroup>();
            if (modalCanvasGroup != null)
            {
                modalCanvasGroup.interactable = false;
                modalCanvasGroup.blocksRaycasts = false;
            }

            // Animate overlay if present
            if (overlay != null)
            {
                SmoothAnimationSystem.Instance.FadeCanvasGroup(overlay, 0f, overlayFadeDuration,
                    SmoothAnimationSystem.EaseType.CubicIn, () => overlay.gameObject.SetActive(false));
            }

            // Animate modal
            float elapsed = 0;
            Vector3 startScale = modal.localScale;
            float startAlpha = modalCanvasGroup != null ? modalCanvasGroup.alpha : 1f;

            while (elapsed < modalDuration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / (modalDuration * 0.7f), SmoothAnimationSystem.EaseType.BackIn);

                modal.localScale = Vector3.Lerp(startScale, Vector3.one * modalScaleStart, t);
                if (modalCanvasGroup != null)
                    modalCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0, t);

                yield return null;
            }

            modal.gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        #endregion

        #region List Animations

        public void AnimateListIn(List<RectTransform> items, ListAnimationType animationType = ListAnimationType.FadeSlideUp)
        {
            StartCoroutine(AnimateListInCoroutine(items, animationType));
        }

        private IEnumerator AnimateListInCoroutine(List<RectTransform> items, ListAnimationType animationType)
        {
            // Setup initial state
            foreach (var item in items)
            {
                var canvasGroup = item.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = item.gameObject.AddComponent<CanvasGroup>();

                canvasGroup.alpha = 0;

                switch (animationType)
                {
                    case ListAnimationType.FadeSlideUp:
                        item.anchoredPosition += new Vector2(0, -30);
                        break;
                    case ListAnimationType.FadeSlideLeft:
                        item.anchoredPosition += new Vector2(50, 0);
                        break;
                    case ListAnimationType.FadeScale:
                        item.localScale = Vector3.one * 0.8f;
                        break;
                }
            }

            // Animate each item with stagger
            for (int i = 0; i < items.Count; i++)
            {
                StartCoroutine(AnimateSingleListItem(items[i], animationType));
                yield return new WaitForSeconds(listItemStagger);
            }
        }

        private IEnumerator AnimateSingleListItem(RectTransform item, ListAnimationType animationType)
        {
            var canvasGroup = item.GetComponent<CanvasGroup>();
            Vector2 startPos = item.anchoredPosition;
            Vector3 startScale = item.localScale;
            Vector2 targetPos = startPos;
            Vector3 targetScale = Vector3.one;

            switch (animationType)
            {
                case ListAnimationType.FadeSlideUp:
                    targetPos = startPos + new Vector2(0, 30);
                    break;
                case ListAnimationType.FadeSlideLeft:
                    targetPos = startPos + new Vector2(-50, 0);
                    break;
            }

            float elapsed = 0;
            while (elapsed < listItemDuration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / listItemDuration, SmoothAnimationSystem.EaseType.CubicOut);

                canvasGroup.alpha = t;

                switch (animationType)
                {
                    case ListAnimationType.FadeSlideUp:
                    case ListAnimationType.FadeSlideLeft:
                        item.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                        break;
                    case ListAnimationType.FadeScale:
                        item.localScale = Vector3.Lerp(startScale, targetScale, t);
                        break;
                }

                yield return null;
            }

            canvasGroup.alpha = 1;
            item.anchoredPosition = targetPos;
            item.localScale = targetScale;
        }

        public enum ListAnimationType
        {
            FadeSlideUp,
            FadeSlideLeft,
            FadeScale,
            Fade
        }

        #endregion

        #region Tab Transitions

        public void SwitchTabs(RectTransform fromTab, RectTransform toTab, TabTransitionType transitionType = TabTransitionType.CrossFade)
        {
            StartCoroutine(SwitchTabsCoroutine(fromTab, toTab, transitionType));
        }

        private IEnumerator SwitchTabsCoroutine(RectTransform fromTab, RectTransform toTab, TabTransitionType transitionType)
        {
            var fromCG = fromTab.GetComponent<CanvasGroup>() ?? fromTab.gameObject.AddComponent<CanvasGroup>();
            var toCG = toTab.GetComponent<CanvasGroup>() ?? toTab.gameObject.AddComponent<CanvasGroup>();

            fromCG.interactable = false;
            fromCG.blocksRaycasts = false;

            toTab.gameObject.SetActive(true);
            toCG.alpha = 0;

            float duration = 0.25f;
            float elapsed = 0;

            Vector2 fromStartPos = fromTab.anchoredPosition;
            Vector2 toStartPos = toTab.anchoredPosition;
            Vector2 fromTargetPos = fromStartPos;
            Vector2 toTargetPos = toStartPos;

            switch (transitionType)
            {
                case TabTransitionType.SlideLeft:
                    toTab.anchoredPosition = toStartPos + new Vector2(100, 0);
                    fromTargetPos = fromStartPos - new Vector2(100, 0);
                    break;
                case TabTransitionType.SlideRight:
                    toTab.anchoredPosition = toStartPos - new Vector2(100, 0);
                    fromTargetPos = fromStartPos + new Vector2(100, 0);
                    break;
                case TabTransitionType.SlideUp:
                    toTab.anchoredPosition = toStartPos - new Vector2(0, 100);
                    fromTargetPos = fromStartPos + new Vector2(0, 100);
                    break;
            }

            toStartPos = toTab.anchoredPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / duration, SmoothAnimationSystem.EaseType.CubicInOut);

                fromCG.alpha = 1 - t;
                toCG.alpha = t;

                if (transitionType != TabTransitionType.CrossFade)
                {
                    fromTab.anchoredPosition = Vector2.Lerp(fromStartPos, fromTargetPos, t);
                    toTab.anchoredPosition = Vector2.Lerp(toStartPos, toTargetPos, t);
                }

                yield return null;
            }

            fromTab.gameObject.SetActive(false);
            fromTab.anchoredPosition = fromStartPos; // Reset position

            toCG.alpha = 1;
            toCG.interactable = true;
            toCG.blocksRaycasts = true;
            toTab.anchoredPosition = toTargetPos;
        }

        public enum TabTransitionType
        {
            CrossFade,
            SlideLeft,
            SlideRight,
            SlideUp
        }

        #endregion

        #region Notifications

        public void ShowNotification(RectTransform notification, NotificationPosition position = NotificationPosition.TopRight,
            bool autoHide = true, System.Action onHide = null)
        {
            StartCoroutine(ShowNotificationCoroutine(notification, position, autoHide, onHide));
        }

        private IEnumerator ShowNotificationCoroutine(RectTransform notification, NotificationPosition position,
            bool autoHide, System.Action onHide)
        {
            notification.gameObject.SetActive(true);

            var canvasGroup = notification.GetComponent<CanvasGroup>() ?? notification.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            Vector2 startOffset = GetNotificationOffset(position);
            Vector2 originalPos = notification.anchoredPosition;
            notification.anchoredPosition = originalPos + startOffset;

            float elapsed = 0;
            while (elapsed < notificationDuration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / notificationDuration, SmoothAnimationSystem.EaseType.BackOut);

                canvasGroup.alpha = t;
                notification.anchoredPosition = Vector2.Lerp(originalPos + startOffset, originalPos, t);

                yield return null;
            }

            canvasGroup.alpha = 1;
            notification.anchoredPosition = originalPos;

            if (autoHide)
            {
                yield return new WaitForSeconds(notificationStayDuration);
                yield return HideNotificationCoroutine(notification, position, onHide);
            }
        }

        public void HideNotification(RectTransform notification, NotificationPosition position = NotificationPosition.TopRight,
            System.Action onHide = null)
        {
            StartCoroutine(HideNotificationCoroutine(notification, position, onHide));
        }

        private IEnumerator HideNotificationCoroutine(RectTransform notification, NotificationPosition position,
            System.Action onHide)
        {
            var canvasGroup = notification.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = notification.gameObject.AddComponent<CanvasGroup>();

            Vector2 targetOffset = GetNotificationOffset(position);
            Vector2 startPos = notification.anchoredPosition;

            float elapsed = 0;
            while (elapsed < notificationDuration * 0.7f)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / (notificationDuration * 0.7f), SmoothAnimationSystem.EaseType.CubicIn);

                canvasGroup.alpha = 1 - t;
                notification.anchoredPosition = Vector2.Lerp(startPos, startPos + targetOffset, t);

                yield return null;
            }

            notification.gameObject.SetActive(false);
            notification.anchoredPosition = startPos; // Reset
            onHide?.Invoke();
        }

        private Vector2 GetNotificationOffset(NotificationPosition position)
        {
            switch (position)
            {
                case NotificationPosition.TopRight:
                case NotificationPosition.TopLeft:
                    return new Vector2(0, notificationSlideDistance);
                case NotificationPosition.BottomRight:
                case NotificationPosition.BottomLeft:
                    return new Vector2(0, -notificationSlideDistance);
                case NotificationPosition.TopCenter:
                    return new Vector2(0, notificationSlideDistance);
                case NotificationPosition.BottomCenter:
                    return new Vector2(0, -notificationSlideDistance);
                default:
                    return new Vector2(0, notificationSlideDistance);
            }
        }

        public enum NotificationPosition
        {
            TopRight,
            TopLeft,
            TopCenter,
            BottomRight,
            BottomLeft,
            BottomCenter
        }

        #endregion

        #region Loading Indicators

        public void ShowLoadingSpinner(RectTransform spinner, float rotationSpeed = 360f)
        {
            spinner.gameObject.SetActive(true);
            var rotator = spinner.GetComponent<SpinnerRotator>();
            if (rotator == null)
                rotator = spinner.gameObject.AddComponent<SpinnerRotator>();
            rotator.SetSpeed(rotationSpeed);
            rotator.StartSpinning();

            var canvasGroup = spinner.GetComponent<CanvasGroup>() ?? spinner.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            SmoothAnimationSystem.Instance.FadeCanvasGroup(canvasGroup, 1f, 0.2f);
        }

        public void HideLoadingSpinner(RectTransform spinner, System.Action onComplete = null)
        {
            var canvasGroup = spinner.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = spinner.gameObject.AddComponent<CanvasGroup>();

            SmoothAnimationSystem.Instance.FadeCanvasGroup(canvasGroup, 0f, 0.2f, SmoothAnimationSystem.EaseType.CubicOut, () =>
            {
                var rotator = spinner.GetComponent<SpinnerRotator>();
                if (rotator != null) rotator.StopSpinning();
                spinner.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        public void ShowProgressBar(RectTransform progressFill, float progress, float duration = 0.3f)
        {
            SmoothAnimationSystem.Instance.AnimateFloat(
                progressFill.localScale.x,
                progress,
                duration,
                (value) => progressFill.localScale = new Vector3(value, 1, 1),
                SmoothAnimationSystem.EaseType.CubicOut
            );
        }

        #endregion

        #region Tooltip Effects

        public void ShowTooltip(RectTransform tooltip, Vector2 position)
        {
            tooltip.gameObject.SetActive(true);
            tooltip.anchoredPosition = position;

            var canvasGroup = tooltip.GetComponent<CanvasGroup>() ?? tooltip.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            tooltip.localScale = Vector3.one * 0.9f;

            SmoothAnimationSystem.Instance.FadeCanvasGroup(canvasGroup, 1f, 0.15f);
            SmoothAnimationSystem.Instance.ScaleTransform(tooltip, Vector3.one, 0.15f, SmoothAnimationSystem.EaseType.CubicOut);
        }

        public void HideTooltip(RectTransform tooltip)
        {
            var canvasGroup = tooltip.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = tooltip.gameObject.AddComponent<CanvasGroup>();

            SmoothAnimationSystem.Instance.FadeCanvasGroup(canvasGroup, 0f, 0.1f, SmoothAnimationSystem.EaseType.CubicIn,
                () => tooltip.gameObject.SetActive(false));
        }

        #endregion

        #region Card Effects

        public void FlipCard(RectTransform card, RectTransform frontFace, RectTransform backFace,
            bool showBack = true, float duration = 0.5f, System.Action onComplete = null)
        {
            StartCoroutine(FlipCardCoroutine(card, frontFace, backFace, showBack, duration, onComplete));
        }

        private IEnumerator FlipCardCoroutine(RectTransform card, RectTransform frontFace, RectTransform backFace,
            bool showBack, float duration, System.Action onComplete)
        {
            float elapsed = 0;
            float halfDuration = duration * 0.5f;

            // First half - rotate to 90 degrees
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / halfDuration, SmoothAnimationSystem.EaseType.CubicIn);
                card.localEulerAngles = new Vector3(0, Mathf.Lerp(0, 90, t), 0);
                yield return null;
            }

            // Switch faces
            frontFace.gameObject.SetActive(!showBack);
            backFace.gameObject.SetActive(showBack);

            // Second half - rotate from 90 to 180 (or 0)
            elapsed = 0;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / halfDuration, SmoothAnimationSystem.EaseType.CubicOut);
                card.localEulerAngles = new Vector3(0, Mathf.Lerp(90, showBack ? 180 : 0, t), 0);
                yield return null;
            }

            card.localEulerAngles = new Vector3(0, showBack ? 180 : 0, 0);
            onComplete?.Invoke();
        }

        public void LiftCard(RectTransform card, float liftAmount = 10f, float duration = 0.2f)
        {
            var shadow = card.Find("Shadow");
            Vector2 currentPos = card.anchoredPosition;

            SmoothAnimationSystem.Instance.MoveRectTransform(card, currentPos + new Vector2(0, liftAmount),
                duration, SmoothAnimationSystem.EaseType.CubicOut);

            if (shadow != null)
            {
                var shadowRT = shadow as RectTransform;
                SmoothAnimationSystem.Instance.ScaleTransform(shadowRT, Vector3.one * 1.1f, duration);
                var shadowImage = shadow.GetComponent<Image>();
                if (shadowImage != null)
                {
                    Color shadowColor = shadowImage.color;
                    shadowColor.a *= 0.7f;
                    SmoothAnimationSystem.Instance.ColorImage(shadowImage, shadowColor, duration);
                }
            }
        }

        public void DropCard(RectTransform card, Vector2 originalPosition, float duration = 0.2f)
        {
            var shadow = card.Find("Shadow");

            SmoothAnimationSystem.Instance.MoveRectTransform(card, originalPosition, duration,
                SmoothAnimationSystem.EaseType.CubicOut);

            if (shadow != null)
            {
                var shadowRT = shadow as RectTransform;
                SmoothAnimationSystem.Instance.ScaleTransform(shadowRT, Vector3.one, duration);
            }
        }

        #endregion

        #region Screen Transitions

        public void FadeToBlack(CanvasGroup blackOverlay, float duration = 0.5f, System.Action onComplete = null)
        {
            blackOverlay.gameObject.SetActive(true);
            blackOverlay.alpha = 0;
            SmoothAnimationSystem.Instance.FadeCanvasGroup(blackOverlay, 1f, duration,
                SmoothAnimationSystem.EaseType.CubicInOut, onComplete);
        }

        public void FadeFromBlack(CanvasGroup blackOverlay, float duration = 0.5f, System.Action onComplete = null)
        {
            SmoothAnimationSystem.Instance.FadeCanvasGroup(blackOverlay, 0f, duration,
                SmoothAnimationSystem.EaseType.CubicInOut, () =>
                {
                    blackOverlay.gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }

        public void CircleWipe(RectTransform wipeCircle, bool wipeIn = true, float duration = 0.8f,
            System.Action onComplete = null)
        {
            StartCoroutine(CircleWipeCoroutine(wipeCircle, wipeIn, duration, onComplete));
        }

        private IEnumerator CircleWipeCoroutine(RectTransform wipeCircle, bool wipeIn, float duration,
            System.Action onComplete)
        {
            wipeCircle.gameObject.SetActive(true);

            // Calculate max scale to cover screen
            float screenDiagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
            float circleSize = wipeCircle.rect.width;
            float maxScale = (screenDiagonal / circleSize) * 1.2f;

            float startScale = wipeIn ? maxScale : 0;
            float endScale = wipeIn ? 0 : maxScale;

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = SmoothAnimationSystem.Ease(elapsed / duration, SmoothAnimationSystem.EaseType.CubicInOut);
                float scale = Mathf.Lerp(startScale, endScale, t);
                wipeCircle.localScale = Vector3.one * scale;
                yield return null;
            }

            wipeCircle.localScale = Vector3.one * endScale;

            if (wipeIn)
                wipeCircle.gameObject.SetActive(false);

            onComplete?.Invoke();
        }

        #endregion
    }

    #region Helper Components

    public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        private float hoverScale = 1.05f;
        private float clickScale = 0.95f;
        private float hoverDuration = 0.15f;
        private float clickDuration = 0.1f;

        private Vector3 originalScale;
        private bool isHovering = false;
        private bool isPressed = false;

        public void Initialize(float hover, float click, float hoverDur, float clickDur)
        {
            hoverScale = hover;
            clickScale = click;
            hoverDuration = hoverDur;
            clickDuration = clickDur;
            originalScale = transform.localScale;
        }

        private void Start()
        {
            if (originalScale == Vector3.zero)
                originalScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
            if (!isPressed)
                SmoothAnimationSystem.Instance?.ScaleTransform(transform, originalScale * hoverScale,
                    hoverDuration, SmoothAnimationSystem.EaseType.CubicOut);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
            if (!isPressed)
                SmoothAnimationSystem.Instance?.ScaleTransform(transform, originalScale,
                    hoverDuration, SmoothAnimationSystem.EaseType.CubicOut);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            SmoothAnimationSystem.Instance?.ScaleTransform(transform, originalScale * clickScale,
                clickDuration, SmoothAnimationSystem.EaseType.CubicOut);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            Vector3 targetScale = isHovering ? originalScale * hoverScale : originalScale;
            SmoothAnimationSystem.Instance?.ScaleTransform(transform, targetScale,
                clickDuration, SmoothAnimationSystem.EaseType.BackOut);
        }
    }

    public class SpinnerRotator : MonoBehaviour
    {
        private float rotationSpeed = 360f;
        private bool isSpinning = false;

        public void SetSpeed(float speed)
        {
            rotationSpeed = speed;
        }

        public void StartSpinning()
        {
            isSpinning = true;
        }

        public void StopSpinning()
        {
            isSpinning = false;
        }

        private void Update()
        {
            if (isSpinning)
            {
                transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
            }
        }
    }

    #endregion
}

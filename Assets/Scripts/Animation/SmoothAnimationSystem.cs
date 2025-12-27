// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace CardiacVR.Animation
{
    /// <summary>
    /// Comprehensive smooth animation and transition system
    /// Provides elegant, professional animations for UI and 3D elements
    /// </summary>
    public class SmoothAnimationSystem : MonoBehaviour
    {
        public static SmoothAnimationSystem Instance { get; private set; }

        [Header("Global Settings")]
        [SerializeField] private float globalSpeedMultiplier = 1f;
        [SerializeField] private bool useUnscaledTime = false;

        [Header("Default Durations")]
        [SerializeField] private float defaultFadeDuration = 0.3f;
        [SerializeField] private float defaultScaleDuration = 0.25f;
        [SerializeField] private float defaultMoveDuration = 0.4f;
        [SerializeField] private float defaultRotateDuration = 0.5f;

        [Header("Default Easing")]
        [SerializeField] private EaseType defaultEaseIn = EaseType.CubicIn;
        [SerializeField] private EaseType defaultEaseOut = EaseType.CubicOut;
        [SerializeField] private EaseType defaultEaseInOut = EaseType.CubicInOut;

        // Active animations tracking
        private Dictionary<int, Coroutine> activeAnimations = new Dictionary<int, Coroutine>();
        private int animationIdCounter = 0;

        public enum EaseType
        {
            Linear,
            // Quadratic
            QuadIn, QuadOut, QuadInOut,
            // Cubic
            CubicIn, CubicOut, CubicInOut,
            // Quartic
            QuartIn, QuartOut, QuartInOut,
            // Quintic
            QuintIn, QuintOut, QuintInOut,
            // Sinusoidal
            SineIn, SineOut, SineInOut,
            // Exponential
            ExpoIn, ExpoOut, ExpoInOut,
            // Circular
            CircIn, CircOut, CircInOut,
            // Elastic
            ElasticIn, ElasticOut, ElasticInOut,
            // Back (overshoot)
            BackIn, BackOut, BackInOut,
            // Bounce
            BounceIn, BounceOut, BounceInOut,
            // Smooth step variants
            SmoothStep, SmootherStep
        }

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

        #region Easing Functions

        public static float Ease(float t, EaseType easeType)
        {
            t = Mathf.Clamp01(t);

            switch (easeType)
            {
                case EaseType.Linear:
                    return t;

                // Quadratic
                case EaseType.QuadIn:
                    return t * t;
                case EaseType.QuadOut:
                    return t * (2 - t);
                case EaseType.QuadInOut:
                    return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;

                // Cubic
                case EaseType.CubicIn:
                    return t * t * t;
                case EaseType.CubicOut:
                    return (--t) * t * t + 1;
                case EaseType.CubicInOut:
                    return t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;

                // Quartic
                case EaseType.QuartIn:
                    return t * t * t * t;
                case EaseType.QuartOut:
                    return 1 - (--t) * t * t * t;
                case EaseType.QuartInOut:
                    return t < 0.5f ? 8 * t * t * t * t : 1 - 8 * (--t) * t * t * t;

                // Quintic
                case EaseType.QuintIn:
                    return t * t * t * t * t;
                case EaseType.QuintOut:
                    return 1 + (--t) * t * t * t * t;
                case EaseType.QuintInOut:
                    return t < 0.5f ? 16 * t * t * t * t * t : 1 + 16 * (--t) * t * t * t * t;

                // Sinusoidal
                case EaseType.SineIn:
                    return 1 - Mathf.Cos(t * Mathf.PI / 2);
                case EaseType.SineOut:
                    return Mathf.Sin(t * Mathf.PI / 2);
                case EaseType.SineInOut:
                    return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;

                // Exponential
                case EaseType.ExpoIn:
                    return t == 0 ? 0 : Mathf.Pow(2, 10 * (t - 1));
                case EaseType.ExpoOut:
                    return t == 1 ? 1 : 1 - Mathf.Pow(2, -10 * t);
                case EaseType.ExpoInOut:
                    if (t == 0) return 0;
                    if (t == 1) return 1;
                    return t < 0.5f ? Mathf.Pow(2, 20 * t - 10) / 2 : (2 - Mathf.Pow(2, -20 * t + 10)) / 2;

                // Circular
                case EaseType.CircIn:
                    return 1 - Mathf.Sqrt(1 - t * t);
                case EaseType.CircOut:
                    return Mathf.Sqrt(1 - (--t) * t);
                case EaseType.CircInOut:
                    return t < 0.5f ? (1 - Mathf.Sqrt(1 - 4 * t * t)) / 2 : (Mathf.Sqrt(1 - (-2 * t + 2) * (-2 * t + 2)) + 1) / 2;

                // Elastic
                case EaseType.ElasticIn:
                    return t == 0 ? 0 : t == 1 ? 1 : -Mathf.Pow(2, 10 * t - 10) * Mathf.Sin((t * 10 - 10.75f) * (2 * Mathf.PI / 3));
                case EaseType.ElasticOut:
                    return t == 0 ? 0 : t == 1 ? 1 : Mathf.Pow(2, -10 * t) * Mathf.Sin((t * 10 - 0.75f) * (2 * Mathf.PI / 3)) + 1;
                case EaseType.ElasticInOut:
                    if (t == 0) return 0;
                    if (t == 1) return 1;
                    return t < 0.5f
                        ? -(Mathf.Pow(2, 20 * t - 10) * Mathf.Sin((20 * t - 11.125f) * (2 * Mathf.PI / 4.5f))) / 2
                        : (Mathf.Pow(2, -20 * t + 10) * Mathf.Sin((20 * t - 11.125f) * (2 * Mathf.PI / 4.5f))) / 2 + 1;

                // Back (overshoot)
                case EaseType.BackIn:
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1;
                    return c3 * t * t * t - c1 * t * t;
                case EaseType.BackOut:
                    const float c1b = 1.70158f;
                    const float c3b = c1b + 1;
                    return 1 + c3b * Mathf.Pow(t - 1, 3) + c1b * Mathf.Pow(t - 1, 2);
                case EaseType.BackInOut:
                    const float c1c = 1.70158f;
                    const float c2 = c1c * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
                        : (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;

                // Bounce
                case EaseType.BounceIn:
                    return 1 - EaseBounceOut(1 - t);
                case EaseType.BounceOut:
                    return EaseBounceOut(t);
                case EaseType.BounceInOut:
                    return t < 0.5f
                        ? (1 - EaseBounceOut(1 - 2 * t)) / 2
                        : (1 + EaseBounceOut(2 * t - 1)) / 2;

                // Smooth step
                case EaseType.SmoothStep:
                    return t * t * (3 - 2 * t);
                case EaseType.SmootherStep:
                    return t * t * t * (t * (t * 6 - 15) + 10);

                default:
                    return t;
            }
        }

        private static float EaseBounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
                return n1 * t * t;
            else if (t < 2 / d1)
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            else if (t < 2.5f / d1)
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            else
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }

        #endregion

        #region Core Animation Methods

        private float GetDeltaTime()
        {
            return (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * globalSpeedMultiplier;
        }

        private int GetNextAnimationId()
        {
            return ++animationIdCounter;
        }

        public void StopAnimation(int animationId)
        {
            if (activeAnimations.TryGetValue(animationId, out Coroutine coroutine))
            {
                if (coroutine != null)
                    StopCoroutine(coroutine);
                activeAnimations.Remove(animationId);
            }
        }

        public void StopAllAnimationsOnObject(GameObject target)
        {
            int instanceId = target.GetInstanceID();
            List<int> toRemove = new List<int>();

            foreach (var kvp in activeAnimations)
            {
                if (kvp.Key.ToString().Contains(instanceId.ToString()))
                {
                    if (kvp.Value != null)
                        StopCoroutine(kvp.Value);
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (int id in toRemove)
                activeAnimations.Remove(id);
        }

        #endregion

        #region Fade Animations

        public int FadeCanvasGroup(CanvasGroup canvasGroup, float targetAlpha, float duration = -1,
            EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            if (duration < 0) duration = defaultFadeDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(FadeCanvasGroupCoroutine(
                canvasGroup, targetAlpha, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator FadeCanvasGroupCoroutine(CanvasGroup canvasGroup, float targetAlpha,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int FadeImage(Image image, float targetAlpha, float duration = -1,
            EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            if (duration < 0) duration = defaultFadeDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(FadeImageCoroutine(
                image, targetAlpha, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator FadeImageCoroutine(Image image, float targetAlpha,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            Color startColor = image.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                image.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            image.color = targetColor;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int FadeText(TMP_Text text, float targetAlpha, float duration = -1,
            EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            if (duration < 0) duration = defaultFadeDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(FadeTextCoroutine(
                text, targetAlpha, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator FadeTextCoroutine(TMP_Text text, float targetAlpha,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            Color startColor = text.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                text.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            text.color = targetColor;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int FadeMaterial(Material material, float targetAlpha, float duration = -1,
            EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            if (duration < 0) duration = defaultFadeDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(FadeMaterialCoroutine(
                material, targetAlpha, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator FadeMaterialCoroutine(Material material, float targetAlpha,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            Color startColor = material.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                material.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            material.color = targetColor;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        #endregion

        #region Scale Animations

        public int ScaleTransform(Transform target, Vector3 targetScale, float duration = -1,
            EaseType easeType = EaseType.BackOut, Action onComplete = null)
        {
            if (duration < 0) duration = defaultScaleDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(ScaleTransformCoroutine(
                target, targetScale, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator ScaleTransformCoroutine(Transform target, Vector3 targetScale,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                target.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }

            target.localScale = targetScale;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int PunchScale(Transform target, Vector3 punchAmount, float duration = 0.3f,
            int vibrato = 6, float elasticity = 0.5f, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(PunchScaleCoroutine(
                target, punchAmount, duration, vibrato, elasticity, onComplete, animId));
            return animId;
        }

        private IEnumerator PunchScaleCoroutine(Transform target, Vector3 punchAmount,
            float duration, int vibrato, float elasticity, Action onComplete, int animId)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = elapsed / duration;

                // Decaying oscillation
                float decay = 1 - t;
                float oscillation = Mathf.Sin(t * vibrato * Mathf.PI) * decay;

                Vector3 currentPunch = punchAmount * oscillation * elasticity;
                target.localScale = startScale + currentPunch;

                yield return null;
            }

            target.localScale = startScale;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int PopIn(Transform target, float duration = 0.3f, Action onComplete = null)
        {
            target.localScale = Vector3.zero;
            return ScaleTransform(target, Vector3.one, duration, EaseType.BackOut, onComplete);
        }

        public int PopOut(Transform target, float duration = 0.2f, Action onComplete = null)
        {
            return ScaleTransform(target, Vector3.zero, duration, EaseType.BackIn, onComplete);
        }

        #endregion

        #region Move Animations

        public int MoveTransform(Transform target, Vector3 targetPosition, float duration = -1,
            EaseType easeType = EaseType.CubicInOut, bool useLocalSpace = false, Action onComplete = null)
        {
            if (duration < 0) duration = defaultMoveDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(MoveTransformCoroutine(
                target, targetPosition, duration, easeType, useLocalSpace, onComplete, animId));
            return animId;
        }

        private IEnumerator MoveTransformCoroutine(Transform target, Vector3 targetPosition,
            float duration, EaseType easeType, bool useLocalSpace, Action onComplete, int animId)
        {
            Vector3 startPosition = useLocalSpace ? target.localPosition : target.position;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                Vector3 newPos = Vector3.Lerp(startPosition, targetPosition, t);

                if (useLocalSpace)
                    target.localPosition = newPos;
                else
                    target.position = newPos;

                yield return null;
            }

            if (useLocalSpace)
                target.localPosition = targetPosition;
            else
                target.position = targetPosition;

            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int MoveRectTransform(RectTransform target, Vector2 targetAnchoredPosition,
            float duration = -1, EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            if (duration < 0) duration = defaultMoveDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(MoveRectTransformCoroutine(
                target, targetAnchoredPosition, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator MoveRectTransformCoroutine(RectTransform target, Vector2 targetAnchoredPosition,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            Vector2 startPosition = target.anchoredPosition;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                target.anchoredPosition = Vector2.Lerp(startPosition, targetAnchoredPosition, t);
                yield return null;
            }

            target.anchoredPosition = targetAnchoredPosition;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int SlideIn(RectTransform target, SlideDirection direction, float duration = 0.4f,
            EaseType easeType = EaseType.CubicOut, Action onComplete = null)
        {
            Vector2 targetPos = target.anchoredPosition;
            Vector2 startOffset = GetSlideOffset(target, direction);
            target.anchoredPosition = targetPos + startOffset;

            return MoveRectTransform(target, targetPos, duration, easeType, onComplete);
        }

        public int SlideOut(RectTransform target, SlideDirection direction, float duration = 0.3f,
            EaseType easeType = EaseType.CubicIn, Action onComplete = null)
        {
            Vector2 startPos = target.anchoredPosition;
            Vector2 endOffset = GetSlideOffset(target, direction);

            return MoveRectTransform(target, startPos + endOffset, duration, easeType, onComplete);
        }

        public enum SlideDirection { Left, Right, Up, Down }

        private Vector2 GetSlideOffset(RectTransform target, SlideDirection direction)
        {
            Rect rect = target.rect;
            switch (direction)
            {
                case SlideDirection.Left: return new Vector2(-rect.width - 100, 0);
                case SlideDirection.Right: return new Vector2(rect.width + 100, 0);
                case SlideDirection.Up: return new Vector2(0, rect.height + 100);
                case SlideDirection.Down: return new Vector2(0, -rect.height - 100);
                default: return Vector2.zero;
            }
        }

        #endregion

        #region Rotation Animations

        public int RotateTransform(Transform target, Quaternion targetRotation, float duration = -1,
            EaseType easeType = EaseType.CubicInOut, bool useLocalSpace = false, Action onComplete = null)
        {
            if (duration < 0) duration = defaultRotateDuration;

            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(RotateTransformCoroutine(
                target, targetRotation, duration, easeType, useLocalSpace, onComplete, animId));
            return animId;
        }

        private IEnumerator RotateTransformCoroutine(Transform target, Quaternion targetRotation,
            float duration, EaseType easeType, bool useLocalSpace, Action onComplete, int animId)
        {
            Quaternion startRotation = useLocalSpace ? target.localRotation : target.rotation;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                Quaternion newRot = Quaternion.Slerp(startRotation, targetRotation, t);

                if (useLocalSpace)
                    target.localRotation = newRot;
                else
                    target.rotation = newRot;

                yield return null;
            }

            if (useLocalSpace)
                target.localRotation = targetRotation;
            else
                target.rotation = targetRotation;

            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int RotateTransformEuler(Transform target, Vector3 targetEuler, float duration = -1,
            EaseType easeType = EaseType.CubicInOut, bool useLocalSpace = false, Action onComplete = null)
        {
            return RotateTransform(target, Quaternion.Euler(targetEuler), duration, easeType, useLocalSpace, onComplete);
        }

        public int SpinTransform(Transform target, Vector3 axis, float rotations, float duration = 1f,
            EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(SpinTransformCoroutine(
                target, axis, rotations, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator SpinTransformCoroutine(Transform target, Vector3 axis, float rotations,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            Quaternion startRotation = target.localRotation;
            float totalDegrees = rotations * 360f;
            float elapsed = 0;
            float lastAngle = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                float currentAngle = totalDegrees * t;
                float deltaAngle = currentAngle - lastAngle;
                lastAngle = currentAngle;

                target.Rotate(axis, deltaAngle, Space.Self);
                yield return null;
            }

            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        #endregion

        #region Color Animations

        public int ColorImage(Image image, Color targetColor, float duration = 0.3f,
            EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(ColorImageCoroutine(
                image, targetColor, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator ColorImageCoroutine(Image image, Color targetColor,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            Color startColor = image.color;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                image.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            image.color = targetColor;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int ColorMaterial(Material material, Color targetColor, float duration = 0.3f,
            EaseType easeType = EaseType.CubicInOut, string propertyName = "_Color", Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(ColorMaterialCoroutine(
                material, targetColor, duration, easeType, propertyName, onComplete, animId));
            return animId;
        }

        private IEnumerator ColorMaterialCoroutine(Material material, Color targetColor,
            float duration, EaseType easeType, string propertyName, Action onComplete, int animId)
        {
            Color startColor = material.GetColor(propertyName);
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                material.SetColor(propertyName, Color.Lerp(startColor, targetColor, t));
                yield return null;
            }

            material.SetColor(propertyName, targetColor);
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        #endregion

        #region Camera Animations

        public int CameraTransition(Camera camera, Vector3 targetPosition, Quaternion targetRotation,
            float duration = 1f, EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(CameraTransitionCoroutine(
                camera, targetPosition, targetRotation, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator CameraTransitionCoroutine(Camera camera, Vector3 targetPosition,
            Quaternion targetRotation, float duration, EaseType easeType, Action onComplete, int animId)
        {
            Transform camTransform = camera.transform;
            Vector3 startPos = camTransform.position;
            Quaternion startRot = camTransform.rotation;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);

                camTransform.position = Vector3.Lerp(startPos, targetPosition, t);
                camTransform.rotation = Quaternion.Slerp(startRot, targetRotation, t);

                yield return null;
            }

            camTransform.position = targetPosition;
            camTransform.rotation = targetRotation;

            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int CameraFOVTransition(Camera camera, float targetFOV, float duration = 0.5f,
            EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(CameraFOVCoroutine(
                camera, targetFOV, duration, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator CameraFOVCoroutine(Camera camera, float targetFOV,
            float duration, EaseType easeType, Action onComplete, int animId)
        {
            float startFOV = camera.fieldOfView;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                camera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);
                yield return null;
            }

            camera.fieldOfView = targetFOV;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int CameraShake(Camera camera, float intensity = 0.3f, float duration = 0.5f,
            float frequency = 25f, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(CameraShakeCoroutine(
                camera, intensity, duration, frequency, onComplete, animId));
            return animId;
        }

        private IEnumerator CameraShakeCoroutine(Camera camera, float intensity,
            float duration, float frequency, Action onComplete, int animId)
        {
            Vector3 originalPos = camera.transform.localPosition;
            float elapsed = 0;
            float seed = UnityEngine.Random.value * 1000f;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float decay = 1 - (elapsed / duration);
                float currentIntensity = intensity * decay;

                float x = (Mathf.PerlinNoise(seed, elapsed * frequency) - 0.5f) * 2f * currentIntensity;
                float y = (Mathf.PerlinNoise(seed + 100, elapsed * frequency) - 0.5f) * 2f * currentIntensity;

                camera.transform.localPosition = originalPos + new Vector3(x, y, 0);
                yield return null;
            }

            camera.transform.localPosition = originalPos;
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        #endregion

        #region Value Animations

        public int AnimateFloat(float startValue, float endValue, float duration,
            Action<float> onUpdate, EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(AnimateFloatCoroutine(
                startValue, endValue, duration, onUpdate, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator AnimateFloatCoroutine(float startValue, float endValue,
            float duration, Action<float> onUpdate, EaseType easeType, Action onComplete, int animId)
        {
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                float value = Mathf.Lerp(startValue, endValue, t);
                onUpdate?.Invoke(value);
                yield return null;
            }

            onUpdate?.Invoke(endValue);
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int AnimateVector3(Vector3 startValue, Vector3 endValue, float duration,
            Action<Vector3> onUpdate, EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(AnimateVector3Coroutine(
                startValue, endValue, duration, onUpdate, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator AnimateVector3Coroutine(Vector3 startValue, Vector3 endValue,
            float duration, Action<Vector3> onUpdate, EaseType easeType, Action onComplete, int animId)
        {
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                Vector3 value = Vector3.Lerp(startValue, endValue, t);
                onUpdate?.Invoke(value);
                yield return null;
            }

            onUpdate?.Invoke(endValue);
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        public int AnimateColor(Color startColor, Color endColor, float duration,
            Action<Color> onUpdate, EaseType easeType = EaseType.CubicInOut, Action onComplete = null)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(AnimateColorCoroutine(
                startColor, endColor, duration, onUpdate, easeType, onComplete, animId));
            return animId;
        }

        private IEnumerator AnimateColorCoroutine(Color startColor, Color endColor,
            float duration, Action<Color> onUpdate, EaseType easeType, Action onComplete, int animId)
        {
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += GetDeltaTime();
                float t = Ease(elapsed / duration, easeType);
                Color value = Color.Lerp(startColor, endColor, t);
                onUpdate?.Invoke(value);
                yield return null;
            }

            onUpdate?.Invoke(endColor);
            activeAnimations.Remove(animId);
            onComplete?.Invoke();
        }

        #endregion

        #region Sequence Animations

        public int PlaySequence(AnimationSequence sequence)
        {
            int animId = GetNextAnimationId();
            activeAnimations[animId] = StartCoroutine(PlaySequenceCoroutine(sequence, animId));
            return animId;
        }

        private IEnumerator PlaySequenceCoroutine(AnimationSequence sequence, int animId)
        {
            foreach (var step in sequence.steps)
            {
                if (step.delay > 0)
                    yield return new WaitForSeconds(step.delay);

                bool stepComplete = false;
                step.action?.Invoke(() => stepComplete = true);

                if (step.waitForCompletion)
                {
                    while (!stepComplete)
                        yield return null;
                }
            }

            activeAnimations.Remove(animId);
            sequence.onSequenceComplete?.Invoke();
        }

        #endregion

        #region UI Panel Animations

        public void ShowPanel(CanvasGroup panel, bool animated = true, Action onComplete = null)
        {
            panel.gameObject.SetActive(true);

            if (animated)
            {
                panel.alpha = 0;
                FadeCanvasGroup(panel, 1f, defaultFadeDuration, EaseType.CubicOut, () =>
                {
                    panel.interactable = true;
                    panel.blocksRaycasts = true;
                    onComplete?.Invoke();
                });
            }
            else
            {
                panel.alpha = 1;
                panel.interactable = true;
                panel.blocksRaycasts = true;
                onComplete?.Invoke();
            }
        }

        public void HidePanel(CanvasGroup panel, bool animated = true, Action onComplete = null)
        {
            panel.interactable = false;
            panel.blocksRaycasts = false;

            if (animated)
            {
                FadeCanvasGroup(panel, 0f, defaultFadeDuration, EaseType.CubicIn, () =>
                {
                    panel.gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
            }
            else
            {
                panel.alpha = 0;
                panel.gameObject.SetActive(false);
                onComplete?.Invoke();
            }
        }

        public void TransitionPanels(CanvasGroup fromPanel, CanvasGroup toPanel,
            float duration = 0.3f, Action onComplete = null)
        {
            fromPanel.interactable = false;
            fromPanel.blocksRaycasts = false;

            FadeCanvasGroup(fromPanel, 0f, duration * 0.5f, EaseType.CubicIn, () =>
            {
                fromPanel.gameObject.SetActive(false);
                toPanel.gameObject.SetActive(true);
                toPanel.alpha = 0;

                FadeCanvasGroup(toPanel, 1f, duration * 0.5f, EaseType.CubicOut, () =>
                {
                    toPanel.interactable = true;
                    toPanel.blocksRaycasts = true;
                    onComplete?.Invoke();
                });
            });
        }

        #endregion
    }

    /// <summary>
    /// Represents a sequence of animations to play in order
    /// </summary>
    [System.Serializable]
    public class AnimationSequence
    {
        public List<AnimationStep> steps = new List<AnimationStep>();
        public Action onSequenceComplete;

        public AnimationSequence AddStep(Action<Action> action, float delay = 0, bool waitForCompletion = true)
        {
            steps.Add(new AnimationStep
            {
                action = action,
                delay = delay,
                waitForCompletion = waitForCompletion
            });
            return this;
        }
    }

    [System.Serializable]
    public class AnimationStep
    {
        public Action<Action> action;
        public float delay;
        public bool waitForCompletion;
    }
}

// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace CardiacVR.Rendering
{
    /// <summary>
    /// Beautiful visual effects for selection, highlighting, and interaction
    /// </summary>
    public class VisualEffectsSystem : MonoBehaviour
    {
        public static VisualEffectsSystem Instance { get; private set; }

        [Header("Selection Effects")]
        [SerializeField] private Color selectionColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Color hoverColor = new Color(0.3f, 0.7f, 1f, 1f);
        [SerializeField] private float outlineWidth = 0.015f;
        [SerializeField] private float pulseSpeed = 2f;

        [Header("Highlight Effects")]
        [SerializeField] private float highlightIntensity = 0.3f;
        [SerializeField] private float glowRadius = 0.5f;
        [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0.8f, 1, 1.2f);

        [Header("Focus Effects")]
        [SerializeField] private float focusFadeTime = 0.5f;
        [SerializeField] private float unfocusedOpacity = 0.15f;
        [SerializeField] private float focusDOFAmount = 0.5f;

        [Header("Transition Effects")]
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Particle Effects")]
        [SerializeField] private GameObject sparkleParticlePrefab;
        [SerializeField] private GameObject glowParticlePrefab;
        [SerializeField] private GameObject pulseRingPrefab;

        [Header("Materials")]
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private Material glowMaterial;
        [SerializeField] private Material ghostMaterial;

        // Active effects tracking
        private Dictionary<GameObject, EffectState> activeEffects = new Dictionary<GameObject, EffectState>();
        private Dictionary<GameObject, List<GameObject>> effectObjects = new Dictionary<GameObject, List<GameObject>>();

        private class EffectState
        {
            public bool isSelected;
            public bool isHovered;
            public bool isFocused;
            public bool isPulsing;
            public Coroutine pulseCoroutine;
            public Coroutine transitionCoroutine;
            public Material[] originalMaterials;
            public Material[] effectMaterials;
            public GameObject outlineObject;
            public GameObject glowObject;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeMaterials();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeMaterials()
        {
            if (outlineMaterial == null)
            {
                Shader outlineShader = Shader.Find("CardiacVR/SelectionOutline");
                if (outlineShader == null)
                    outlineShader = Shader.Find("Universal Render Pipeline/Unlit");

                outlineMaterial = new Material(outlineShader);
                outlineMaterial.SetColor("_OutlineColor", selectionColor);
                outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
            }

            if (glowMaterial == null)
            {
                glowMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                glowMaterial.SetColor("_BaseColor", new Color(selectionColor.r, selectionColor.g, selectionColor.b, 0.3f));
                glowMaterial.SetFloat("_Surface", 1);
                glowMaterial.renderQueue = (int)RenderQueue.Transparent;
            }

            if (ghostMaterial == null)
            {
                ghostMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                ghostMaterial.SetFloat("_Surface", 1);
                ghostMaterial.SetFloat("_Blend", 0);
                ghostMaterial.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.5f, unfocusedOpacity));
                ghostMaterial.renderQueue = (int)RenderQueue.Transparent;
                ghostMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }

        #region Selection Effects

        public void SetSelected(GameObject target, bool selected)
        {
            EnsureEffectState(target);
            EffectState state = activeEffects[target];

            if (state.isSelected == selected) return;
            state.isSelected = selected;

            if (selected)
            {
                ApplySelectionEffect(target, state);
                StartPulseEffect(target, state);
            }
            else
            {
                RemoveSelectionEffect(target, state);
                StopPulseEffect(state);
            }
        }

        public void SetHovered(GameObject target, bool hovered)
        {
            EnsureEffectState(target);
            EffectState state = activeEffects[target];

            if (state.isHovered == hovered) return;
            state.isHovered = hovered;

            if (hovered && !state.isSelected)
            {
                ApplyHoverEffect(target, state);
            }
            else if (!hovered && !state.isSelected)
            {
                RemoveHoverEffect(target, state);
            }
        }

        private void ApplySelectionEffect(GameObject target, EffectState state)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            // Store original materials
            if (state.originalMaterials == null)
            {
                state.originalMaterials = renderer.materials;
            }

            // Create outline object
            if (state.outlineObject == null)
            {
                state.outlineObject = CreateOutlineObject(target, selectionColor);
            }
            state.outlineObject.SetActive(true);

            // Apply emission to original material
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", selectionColor * highlightIntensity);
                }
            }

            // Spawn selection particles
            SpawnSelectionParticles(target);
        }

        private void RemoveSelectionEffect(GameObject target, EffectState state)
        {
            Renderer renderer = target.GetComponent<Renderer>();

            if (state.outlineObject != null)
            {
                state.outlineObject.SetActive(false);
            }

            // Remove emission
            if (renderer != null)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", Color.black);
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        private void ApplyHoverEffect(GameObject target, EffectState state)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null) return;

            // Store original materials
            if (state.originalMaterials == null)
            {
                state.originalMaterials = renderer.materials;
            }

            // Create subtle outline
            if (state.outlineObject == null)
            {
                state.outlineObject = CreateOutlineObject(target, hoverColor, outlineWidth * 0.5f);
            }
            else
            {
                // Update outline color for hover
                Renderer outlineRenderer = state.outlineObject.GetComponent<Renderer>();
                if (outlineRenderer != null)
                {
                    outlineRenderer.material.SetColor("_OutlineColor", hoverColor);
                    outlineRenderer.material.SetFloat("_OutlineWidth", outlineWidth * 0.5f);
                }
            }
            state.outlineObject.SetActive(true);

            // Subtle emission
            foreach (Material mat in renderer.materials)
            {
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", hoverColor * highlightIntensity * 0.5f);
                }
            }
        }

        private void RemoveHoverEffect(GameObject target, EffectState state)
        {
            Renderer renderer = target.GetComponent<Renderer>();

            if (state.outlineObject != null)
            {
                state.outlineObject.SetActive(false);
            }

            if (renderer != null)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.SetColor("_EmissionColor", Color.black);
                        mat.DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        private GameObject CreateOutlineObject(GameObject target, Color color, float width = -1)
        {
            if (width < 0) width = outlineWidth;

            MeshFilter meshFilter = target.GetComponent<MeshFilter>();
            if (meshFilter == null) return null;

            GameObject outline = new GameObject("SelectionOutline");
            outline.transform.SetParent(target.transform, false);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localRotation = Quaternion.identity;
            outline.transform.localScale = Vector3.one;

            MeshFilter outlineMF = outline.AddComponent<MeshFilter>();
            outlineMF.mesh = meshFilter.sharedMesh;

            MeshRenderer outlineMR = outline.AddComponent<MeshRenderer>();
            Material mat = new Material(outlineMaterial);
            mat.SetColor("_OutlineColor", color);
            mat.SetFloat("_OutlineWidth", width);
            mat.SetFloat("_EnablePulse", 1);
            mat.SetFloat("_PulseSpeed", pulseSpeed);
            outlineMR.material = mat;
            outlineMR.shadowCastingMode = ShadowCastingMode.Off;
            outlineMR.receiveShadows = false;

            return outline;
        }

        #endregion

        #region Pulse Effects

        private void StartPulseEffect(GameObject target, EffectState state)
        {
            if (state.isPulsing) return;
            state.isPulsing = true;
            state.pulseCoroutine = StartCoroutine(PulseEffectCoroutine(target, state));
        }

        private void StopPulseEffect(EffectState state)
        {
            state.isPulsing = false;
            if (state.pulseCoroutine != null)
            {
                StopCoroutine(state.pulseCoroutine);
                state.pulseCoroutine = null;
            }
        }

        private IEnumerator PulseEffectCoroutine(GameObject target, EffectState state)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            float time = 0;

            while (state.isPulsing && target != null)
            {
                time += Time.deltaTime * pulseSpeed;
                float pulseValue = pulseCurve.Evaluate(Mathf.PingPong(time, 1f));

                // Pulse emission
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", selectionColor * highlightIntensity * pulseValue);
                        }
                    }
                }

                // Pulse outline width
                if (state.outlineObject != null)
                {
                    Renderer outlineRenderer = state.outlineObject.GetComponent<Renderer>();
                    if (outlineRenderer != null && outlineRenderer.material != null)
                    {
                        outlineRenderer.material.SetFloat("_OutlineWidth", outlineWidth * pulseValue);
                    }
                }

                yield return null;
            }
        }

        #endregion

        #region Focus Effects (Isolation)

        public void FocusOnObject(GameObject target, List<GameObject> allObjects)
        {
            StartCoroutine(FocusTransitionCoroutine(target, allObjects, true));
        }

        public void ClearFocus(List<GameObject> allObjects)
        {
            StartCoroutine(FocusTransitionCoroutine(null, allObjects, false));
        }

        private IEnumerator FocusTransitionCoroutine(GameObject focusTarget, List<GameObject> allObjects, bool focusing)
        {
            float elapsed = 0;

            // Capture initial states
            Dictionary<GameObject, float> startOpacities = new Dictionary<GameObject, float>();
            Dictionary<GameObject, float> targetOpacities = new Dictionary<GameObject, float>();

            foreach (GameObject obj in allObjects)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer == null) continue;

                float currentOpacity = 1f;
                if (renderer.material.HasProperty("_BaseColor"))
                {
                    currentOpacity = renderer.material.GetColor("_BaseColor").a;
                }
                startOpacities[obj] = currentOpacity;

                if (focusing)
                {
                    targetOpacities[obj] = (obj == focusTarget) ? 1f : unfocusedOpacity;
                }
                else
                {
                    targetOpacities[obj] = 1f;
                }
            }

            // Animate transition
            while (elapsed < focusFadeTime)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / focusFadeTime);

                foreach (GameObject obj in allObjects)
                {
                    if (!startOpacities.ContainsKey(obj)) continue;

                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer == null) continue;

                    float opacity = Mathf.Lerp(startOpacities[obj], targetOpacities[obj], t);

                    if (renderer.material.HasProperty("_BaseColor"))
                    {
                        Color color = renderer.material.GetColor("_BaseColor");
                        color.a = opacity;
                        renderer.material.SetColor("_BaseColor", color);

                        // Setup transparency if needed
                        if (opacity < 1f)
                        {
                            renderer.material.SetFloat("_Surface", 1);
                            renderer.material.renderQueue = (int)RenderQueue.Transparent;
                            renderer.material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        }
                        else
                        {
                            renderer.material.SetFloat("_Surface", 0);
                            renderer.material.renderQueue = (int)RenderQueue.Geometry;
                            renderer.material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        }
                    }
                }

                yield return null;
            }

            // Ensure final values
            foreach (GameObject obj in allObjects)
            {
                if (!targetOpacities.ContainsKey(obj)) continue;

                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer == null || !renderer.material.HasProperty("_BaseColor")) continue;

                Color color = renderer.material.GetColor("_BaseColor");
                color.a = targetOpacities[obj];
                renderer.material.SetColor("_BaseColor", color);
            }
        }

        #endregion

        #region Explode/Implode Effects

        public void ExplodeFromCenter(List<GameObject> objects, Vector3 center, float distance, float duration = 1f)
        {
            StartCoroutine(ExplodeCoroutine(objects, center, distance, duration, true));
        }

        public void ImplodeToCenter(List<GameObject> objects, Vector3 center, float duration = 1f)
        {
            StartCoroutine(ExplodeCoroutine(objects, center, 0, duration, false));
        }

        private IEnumerator ExplodeCoroutine(List<GameObject> objects, Vector3 center, float distance, float duration, bool exploding)
        {
            Dictionary<GameObject, Vector3> startPositions = new Dictionary<GameObject, Vector3>();
            Dictionary<GameObject, Vector3> targetPositions = new Dictionary<GameObject, Vector3>();

            foreach (GameObject obj in objects)
            {
                startPositions[obj] = obj.transform.position;

                if (exploding)
                {
                    Vector3 direction = (obj.transform.position - center).normalized;
                    if (direction.magnitude < 0.01f)
                        direction = Vector3.up;
                    targetPositions[obj] = obj.transform.position + direction * distance;
                }
                else
                {
                    // For implode, assume we stored original positions somewhere
                    // For now, just move slightly toward center
                    targetPositions[obj] = startPositions[obj];
                }
            }

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / duration);

                foreach (GameObject obj in objects)
                {
                    if (obj == null) continue;
                    obj.transform.position = Vector3.Lerp(startPositions[obj], targetPositions[obj], t);
                }

                yield return null;
            }

            // Ensure final positions
            foreach (GameObject obj in objects)
            {
                if (obj == null) continue;
                obj.transform.position = targetPositions[obj];
            }
        }

        #endregion

        #region Particle Effects

        public void SpawnSelectionParticles(GameObject target)
        {
            if (sparkleParticlePrefab == null) return;

            Bounds bounds = GetObjectBounds(target);

            GameObject particles = Instantiate(sparkleParticlePrefab, bounds.center, Quaternion.identity);
            particles.transform.localScale = bounds.size * 1.5f;

            // Auto-destroy after effect
            Destroy(particles, 2f);
        }

        public void SpawnGlowEffect(Vector3 position, Color color, float duration = 1f)
        {
            if (glowParticlePrefab == null) return;

            GameObject glow = Instantiate(glowParticlePrefab, position, Quaternion.identity);

            ParticleSystem ps = glow.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = color;
            }

            Destroy(glow, duration);
        }

        public void SpawnPulseRing(Vector3 position, float radius, Color color)
        {
            if (pulseRingPrefab == null)
            {
                // Create simple pulse ring
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.transform.position = position;
                ring.transform.localScale = new Vector3(0.1f, 0.01f, 0.1f);

                Renderer renderer = ring.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                renderer.material.SetColor("_BaseColor", color);

                StartCoroutine(AnimatePulseRing(ring, radius, 0.5f));
            }
            else
            {
                GameObject ring = Instantiate(pulseRingPrefab, position, Quaternion.identity);
                StartCoroutine(AnimatePulseRing(ring, radius, 0.5f));
            }
        }

        private IEnumerator AnimatePulseRing(GameObject ring, float targetRadius, float duration)
        {
            float elapsed = 0;
            Vector3 startScale = ring.transform.localScale;

            Renderer renderer = ring.GetComponent<Renderer>();
            Color startColor = renderer.material.color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Expand
                float currentRadius = Mathf.Lerp(0.1f, targetRadius, t);
                ring.transform.localScale = new Vector3(currentRadius, startScale.y, currentRadius);

                // Fade out
                Color color = startColor;
                color.a = 1f - t;
                renderer.material.color = color;

                yield return null;
            }

            Destroy(ring);
        }

        #endregion

        #region X-Ray Effect

        public void EnableXRayView(List<GameObject> outerObjects, List<GameObject> innerObjects)
        {
            // Make outer objects semi-transparent
            foreach (GameObject obj in outerObjects)
            {
                EnsureEffectState(obj);
                StartCoroutine(TransitionToGhost(obj, 0.2f));
            }

            // Highlight inner objects
            foreach (GameObject obj in innerObjects)
            {
                EnsureEffectState(obj);
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", new Color(0.2f, 0.4f, 0.6f, 1f) * 0.3f);
                        }
                    }
                }
            }
        }

        public void DisableXRayView(List<GameObject> allObjects)
        {
            foreach (GameObject obj in allObjects)
            {
                if (obj == null) continue;

                StartCoroutine(TransitionFromGhost(obj));

                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    foreach (Material mat in renderer.materials)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", Color.black);
                            mat.DisableKeyword("_EMISSION");
                        }
                    }
                }
            }
        }

        private IEnumerator TransitionToGhost(GameObject obj, float targetOpacity)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) yield break;

            float elapsed = 0;

            foreach (Material mat in renderer.materials)
            {
                if (!mat.HasProperty("_BaseColor")) continue;

                Color startColor = mat.GetColor("_BaseColor");
                Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetOpacity);

                // Enable transparency
                mat.SetFloat("_Surface", 1);
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
            }

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / transitionDuration);

                foreach (Material mat in renderer.materials)
                {
                    if (!mat.HasProperty("_BaseColor")) continue;

                    Color currentColor = mat.GetColor("_BaseColor");
                    currentColor.a = Mathf.Lerp(1f, targetOpacity, t);
                    mat.SetColor("_BaseColor", currentColor);
                }

                yield return null;
            }
        }

        private IEnumerator TransitionFromGhost(GameObject obj)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) yield break;

            float elapsed = 0;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = transitionCurve.Evaluate(elapsed / transitionDuration);

                foreach (Material mat in renderer.materials)
                {
                    if (!mat.HasProperty("_BaseColor")) continue;

                    Color currentColor = mat.GetColor("_BaseColor");
                    currentColor.a = Mathf.Lerp(currentColor.a, 1f, t);
                    mat.SetColor("_BaseColor", currentColor);
                }

                yield return null;
            }

            // Restore opaque rendering
            foreach (Material mat in renderer.materials)
            {
                if (!mat.HasProperty("_BaseColor")) continue;

                Color color = mat.GetColor("_BaseColor");
                color.a = 1f;
                mat.SetColor("_BaseColor", color);

                mat.SetFloat("_Surface", 0);
                mat.renderQueue = (int)RenderQueue.Geometry;
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetInt("_SrcBlend", (int)BlendMode.One);
                mat.SetInt("_DstBlend", (int)BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
            }
        }

        #endregion

        #region Utility

        private void EnsureEffectState(GameObject target)
        {
            if (!activeEffects.ContainsKey(target))
            {
                activeEffects[target] = new EffectState();
                effectObjects[target] = new List<GameObject>();
            }
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }

            Collider collider = obj.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            return new Bounds(obj.transform.position, Vector3.one);
        }

        public void ClearEffects(GameObject target)
        {
            if (activeEffects.TryGetValue(target, out EffectState state))
            {
                StopPulseEffect(state);

                if (state.outlineObject != null)
                    Destroy(state.outlineObject);

                if (state.glowObject != null)
                    Destroy(state.glowObject);

                activeEffects.Remove(target);
            }

            if (effectObjects.TryGetValue(target, out List<GameObject> objects))
            {
                foreach (GameObject obj in objects)
                {
                    if (obj != null)
                        Destroy(obj);
                }
                effectObjects.Remove(target);
            }
        }

        public void ClearAllEffects()
        {
            foreach (var kvp in activeEffects)
            {
                if (kvp.Value.outlineObject != null)
                    Destroy(kvp.Value.outlineObject);
                if (kvp.Value.glowObject != null)
                    Destroy(kvp.Value.glowObject);
            }
            activeEffects.Clear();

            foreach (var kvp in effectObjects)
            {
                foreach (GameObject obj in kvp.Value)
                {
                    if (obj != null)
                        Destroy(obj);
                }
            }
            effectObjects.Clear();
        }

        private void OnDestroy()
        {
            ClearAllEffects();
        }

        #endregion
    }
}

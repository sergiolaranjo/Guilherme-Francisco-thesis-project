using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// VR3S-style voice annotation system.
/// Enables voice-to-text annotations in multiple languages with real-time transcription.
/// Annotations are placed in 3D space and synchronized with other participants.
/// </summary>
public class VoiceAnnotationSystem : MonoBehaviour
{
    public static VoiceAnnotationSystem Instance { get; private set; }

    public event EventHandler<VoiceAnnotationEventArgs> OnAnnotationCreated;
    public event EventHandler<TranscriptionEventArgs> OnTranscriptionReceived;
    public event EventHandler<RecordingEventArgs> OnRecordingStateChanged;

    public class VoiceAnnotationEventArgs : EventArgs
    {
        public VoiceAnnotation Annotation;
    }

    public class TranscriptionEventArgs : EventArgs
    {
        public string Text;
        public string Language;
        public bool IsFinal;
    }

    public class RecordingEventArgs : EventArgs
    {
        public bool IsRecording;
    }

    #region Enums

    public enum TranscriptionLanguage
    {
        Portuguese_BR,
        Portuguese_PT,
        English_US,
        English_UK,
        Spanish,
        French,
        German,
        Italian,
        Mandarin,
        Japanese
    }

    public enum AnnotationVisualStyle
    {
        SpeechBubble,
        TextLabel,
        Floating,
        Attached,
        Minimized
    }

    #endregion

    [Header("Recording Settings")]
    [SerializeField] private bool isRecording = false;
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordingLengthSeconds = 60;
    [SerializeField] private float voiceActivationLevel = 0.01f;
    [SerializeField] private bool useVoiceActivation = false;

    [Header("Transcription Settings")]
    [SerializeField] private TranscriptionLanguage transcriptionLanguage = TranscriptionLanguage.Portuguese_BR;
    [SerializeField] private bool continuousTranscription = true;
    [SerializeField] private bool showInterimResults = true;

    [Header("Annotation Settings")]
    [SerializeField] private AnnotationVisualStyle visualStyle = AnnotationVisualStyle.SpeechBubble;
    [SerializeField] private float annotationLifetime = 0f; // 0 = permanent
    [SerializeField] private float annotationScale = 0.02f;
    [SerializeField] private Transform annotationContainer;

    [Header("Visual Settings")]
    [SerializeField] private GameObject annotationPrefab;
    [SerializeField] private Material annotationMaterial;
    [SerializeField] private Color authorColor = Color.white;
    [SerializeField] private Color otherColor = Color.cyan;
    [SerializeField] private Font annotationFont;

    [Header("Audio Visualization")]
    [SerializeField] private bool showAudioWaveform = true;
    [SerializeField] private LineRenderer waveformRenderer;
    [SerializeField] private int waveformResolution = 64;

    // Recording state
    private AudioClip recordingClip;
    private string microphoneDevice;
    private float[] audioSamples;
    private bool isMicrophoneInitialized = false;

    // Current transcription
    private string currentTranscription = "";
    private bool isTranscribing = false;

    // Annotations
    private List<VoiceAnnotation> annotations = new List<VoiceAnnotation>();
    private Dictionary<string, GameObject> annotationObjects = new Dictionary<string, GameObject>();

    // Pending annotation
    private Vector3 pendingAnnotationPosition;
    private Quaternion pendingAnnotationRotation;

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

        InitializeMicrophone();
    }

    void Start()
    {
        if (annotationContainer == null)
        {
            annotationContainer = new GameObject("VoiceAnnotations").transform;
            annotationContainer.SetParent(transform);
        }

        if (waveformRenderer == null && showAudioWaveform)
        {
            CreateWaveformRenderer();
        }
    }

    void Update()
    {
        if (isRecording)
        {
            UpdateAudioVisualization();
            ProcessVoiceActivation();
        }
    }

    void OnDestroy()
    {
        StopRecording();
    }

    #region Microphone Initialization

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            isMicrophoneInitialized = true;
            Debug.Log($"Microphone initialized: {microphoneDevice}");
        }
        else
        {
            Debug.LogWarning("No microphone found");
            isMicrophoneInitialized = false;
        }
    }

    /// <summary>
    /// Get available microphone devices.
    /// </summary>
    public string[] GetMicrophoneDevices()
    {
        return Microphone.devices;
    }

    /// <summary>
    /// Set active microphone device.
    /// </summary>
    public void SetMicrophoneDevice(string deviceName)
    {
        if (isRecording)
        {
            StopRecording();
        }

        microphoneDevice = deviceName;
    }

    #endregion

    #region Recording Control

    /// <summary>
    /// Start voice recording for annotation.
    /// </summary>
    public void StartRecording(Vector3 annotationPosition, Quaternion annotationRotation)
    {
        if (!isMicrophoneInitialized)
        {
            Debug.LogError("Microphone not initialized");
            return;
        }

        if (isRecording)
        {
            StopRecording();
        }

        pendingAnnotationPosition = annotationPosition;
        pendingAnnotationRotation = annotationRotation;

        recordingClip = Microphone.Start(microphoneDevice, true, recordingLengthSeconds, sampleRate);
        audioSamples = new float[sampleRate];
        isRecording = true;
        currentTranscription = "";

        if (continuousTranscription)
        {
            StartCoroutine(ContinuousTranscriptionCoroutine());
        }

        OnRecordingStateChanged?.Invoke(this, new RecordingEventArgs { IsRecording = true });

        Debug.Log("Started recording");
    }

    /// <summary>
    /// Stop recording and create annotation.
    /// </summary>
    public VoiceAnnotation StopRecording()
    {
        if (!isRecording) return null;

        Microphone.End(microphoneDevice);
        isRecording = false;
        isTranscribing = false;

        OnRecordingStateChanged?.Invoke(this, new RecordingEventArgs { IsRecording = false });

        // Create annotation if we have transcription
        VoiceAnnotation annotation = null;
        if (!string.IsNullOrEmpty(currentTranscription))
        {
            annotation = CreateAnnotation(
                pendingAnnotationPosition,
                pendingAnnotationRotation,
                currentTranscription,
                recordingClip
            );
        }

        Debug.Log("Stopped recording");

        return annotation;
    }

    /// <summary>
    /// Cancel recording without creating annotation.
    /// </summary>
    public void CancelRecording()
    {
        if (!isRecording) return;

        Microphone.End(microphoneDevice);
        isRecording = false;
        isTranscribing = false;
        currentTranscription = "";

        OnRecordingStateChanged?.Invoke(this, new RecordingEventArgs { IsRecording = false });

        Debug.Log("Cancelled recording");
    }

    /// <summary>
    /// Check if currently recording.
    /// </summary>
    public bool IsRecording()
    {
        return isRecording;
    }

    #endregion

    #region Transcription

    private IEnumerator ContinuousTranscriptionCoroutine()
    {
        isTranscribing = true;
        float lastTranscriptionTime = 0f;
        float transcriptionInterval = 0.5f;

        while (isRecording && isTranscribing)
        {
            if (Time.time - lastTranscriptionTime >= transcriptionInterval)
            {
                lastTranscriptionTime = Time.time;

                // Get audio samples
                int micPosition = Microphone.GetPosition(microphoneDevice);
                if (micPosition > 0 && recordingClip != null)
                {
                    // In production, send audio to speech-to-text API
                    // For now, simulate transcription
                    SimulateTranscription();
                }
            }

            yield return null;
        }
    }

    private void SimulateTranscription()
    {
        // In production, this would use a speech-to-text service like:
        // - Google Cloud Speech-to-Text
        // - Azure Speech Services
        // - Amazon Transcribe
        // - OpenAI Whisper

        // For demonstration, show placeholder text
        if (showInterimResults && string.IsNullOrEmpty(currentTranscription))
        {
            OnTranscriptionReceived?.Invoke(this, new TranscriptionEventArgs
            {
                Text = "A ouvir...",
                Language = GetLanguageCode(transcriptionLanguage),
                IsFinal = false
            });
        }
    }

    /// <summary>
    /// Manually set transcription text (for testing or external STT services).
    /// </summary>
    public void SetTranscription(string text, bool isFinal = true)
    {
        currentTranscription = text;

        OnTranscriptionReceived?.Invoke(this, new TranscriptionEventArgs
        {
            Text = text,
            Language = GetLanguageCode(transcriptionLanguage),
            IsFinal = isFinal
        });
    }

    /// <summary>
    /// Set transcription language.
    /// </summary>
    public void SetTranscriptionLanguage(TranscriptionLanguage language)
    {
        transcriptionLanguage = language;
    }

    private string GetLanguageCode(TranscriptionLanguage language)
    {
        switch (language)
        {
            case TranscriptionLanguage.Portuguese_BR: return "pt-BR";
            case TranscriptionLanguage.Portuguese_PT: return "pt-PT";
            case TranscriptionLanguage.English_US: return "en-US";
            case TranscriptionLanguage.English_UK: return "en-GB";
            case TranscriptionLanguage.Spanish: return "es-ES";
            case TranscriptionLanguage.French: return "fr-FR";
            case TranscriptionLanguage.German: return "de-DE";
            case TranscriptionLanguage.Italian: return "it-IT";
            case TranscriptionLanguage.Mandarin: return "zh-CN";
            case TranscriptionLanguage.Japanese: return "ja-JP";
            default: return "en-US";
        }
    }

    #endregion

    #region Annotation Creation

    private VoiceAnnotation CreateAnnotation(Vector3 position, Quaternion rotation, string text, AudioClip audio)
    {
        VoiceAnnotation annotation = new VoiceAnnotation
        {
            Id = System.Guid.NewGuid().ToString(),
            Position = position,
            Rotation = rotation,
            Text = text,
            Language = GetLanguageCode(transcriptionLanguage),
            AudioClip = audio,
            Timestamp = DateTime.Now,
            AuthorId = GetAuthorId(),
            AuthorName = GetAuthorName()
        };

        annotations.Add(annotation);

        // Create visual representation
        CreateAnnotationVisual(annotation);

        OnAnnotationCreated?.Invoke(this, new VoiceAnnotationEventArgs { Annotation = annotation });

        Debug.Log($"Created voice annotation: {text}");

        return annotation;
    }

    /// <summary>
    /// Create annotation from text (without voice recording).
    /// </summary>
    public VoiceAnnotation CreateTextAnnotation(Vector3 position, string text)
    {
        VoiceAnnotation annotation = new VoiceAnnotation
        {
            Id = System.Guid.NewGuid().ToString(),
            Position = position,
            Rotation = Quaternion.identity,
            Text = text,
            Language = GetLanguageCode(transcriptionLanguage),
            AudioClip = null,
            Timestamp = DateTime.Now,
            AuthorId = GetAuthorId(),
            AuthorName = GetAuthorName()
        };

        annotations.Add(annotation);
        CreateAnnotationVisual(annotation);

        OnAnnotationCreated?.Invoke(this, new VoiceAnnotationEventArgs { Annotation = annotation });

        return annotation;
    }

    private void CreateAnnotationVisual(VoiceAnnotation annotation)
    {
        GameObject annotationObj;

        if (annotationPrefab != null)
        {
            annotationObj = Instantiate(annotationPrefab, annotationContainer);
        }
        else
        {
            annotationObj = CreateDefaultAnnotationVisual(annotation);
        }

        annotationObj.name = $"VoiceAnnotation_{annotation.Id.Substring(0, 8)}";
        annotationObj.transform.position = annotation.Position;
        annotationObj.transform.rotation = annotation.Rotation;

        annotationObjects[annotation.Id] = annotationObj;

        // Set lifetime
        if (annotationLifetime > 0)
        {
            StartCoroutine(DestroyAnnotationAfterDelay(annotation.Id, annotationLifetime));
        }
    }

    private GameObject CreateDefaultAnnotationVisual(VoiceAnnotation annotation)
    {
        GameObject obj = new GameObject("AnnotationVisual");

        switch (visualStyle)
        {
            case AnnotationVisualStyle.SpeechBubble:
                CreateSpeechBubbleStyle(obj, annotation);
                break;

            case AnnotationVisualStyle.TextLabel:
                CreateTextLabelStyle(obj, annotation);
                break;

            case AnnotationVisualStyle.Floating:
                CreateFloatingStyle(obj, annotation);
                break;

            default:
                CreateTextLabelStyle(obj, annotation);
                break;
        }

        return obj;
    }

    private void CreateSpeechBubbleStyle(GameObject obj, VoiceAnnotation annotation)
    {
        // Background bubble
        GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bubble.transform.SetParent(obj.transform);
        bubble.transform.localPosition = Vector3.zero;

        // Calculate size based on text
        float textWidth = annotation.Text.Length * 0.01f;
        float textHeight = 0.03f;
        bubble.transform.localScale = new Vector3(Mathf.Max(0.1f, textWidth), textHeight, 1f);

        Renderer bubbleRenderer = bubble.GetComponent<Renderer>();
        if (annotationMaterial != null)
        {
            bubbleRenderer.material = annotationMaterial;
        }
        else
        {
            bubbleRenderer.material.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        }

        Destroy(bubble.GetComponent<Collider>());

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform);
        textObj.transform.localPosition = new Vector3(0, 0, -0.001f);

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = annotation.Text;
        textMesh.fontSize = 24;
        textMesh.characterSize = annotationScale * 0.5f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        if (annotationFont != null)
        {
            textMesh.font = annotationFont;
        }

        // Author label
        GameObject authorObj = new GameObject("Author");
        authorObj.transform.SetParent(obj.transform);
        authorObj.transform.localPosition = new Vector3(0, textHeight / 2 + 0.01f, 0);

        TextMesh authorMesh = authorObj.AddComponent<TextMesh>();
        authorMesh.text = $"- {annotation.AuthorName}";
        authorMesh.fontSize = 16;
        authorMesh.characterSize = annotationScale * 0.3f;
        authorMesh.anchor = TextAnchor.MiddleCenter;
        authorMesh.color = authorColor;

        // Play button for voice annotations
        if (annotation.AudioClip != null)
        {
            GameObject playButton = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            playButton.transform.SetParent(obj.transform);
            playButton.transform.localPosition = new Vector3(-textWidth / 2 - 0.02f, 0, 0);
            playButton.transform.localScale = Vector3.one * 0.015f;
            playButton.GetComponent<Renderer>().material.color = Color.green;

            // Add audio source
            AudioSource audioSource = obj.AddComponent<AudioSource>();
            audioSource.clip = annotation.AudioClip;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }

        // Billboard component
        obj.AddComponent<BillboardBehavior>();
    }

    private void CreateTextLabelStyle(GameObject obj, VoiceAnnotation annotation)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform);
        textObj.transform.localPosition = Vector3.zero;

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = $"{annotation.Text}\n- {annotation.AuthorName}";
        textMesh.fontSize = 24;
        textMesh.characterSize = annotationScale;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = authorColor;

        obj.AddComponent<BillboardBehavior>();
    }

    private void CreateFloatingStyle(GameObject obj, VoiceAnnotation annotation)
    {
        CreateTextLabelStyle(obj, annotation);

        // Add floating animation
        FloatingBehavior floating = obj.AddComponent<FloatingBehavior>();
        floating.amplitude = 0.005f;
        floating.frequency = 1f;
    }

    private IEnumerator DestroyAnnotationAfterDelay(string annotationId, float delay)
    {
        yield return new WaitForSeconds(delay);
        RemoveAnnotation(annotationId);
    }

    #endregion

    #region Annotation Management

    /// <summary>
    /// Remove an annotation.
    /// </summary>
    public void RemoveAnnotation(string annotationId)
    {
        annotations.RemoveAll(a => a.Id == annotationId);

        if (annotationObjects.ContainsKey(annotationId))
        {
            Destroy(annotationObjects[annotationId]);
            annotationObjects.Remove(annotationId);
        }
    }

    /// <summary>
    /// Get all annotations.
    /// </summary>
    public List<VoiceAnnotation> GetAnnotations()
    {
        return new List<VoiceAnnotation>(annotations);
    }

    /// <summary>
    /// Clear all annotations.
    /// </summary>
    public void ClearAllAnnotations()
    {
        foreach (var obj in annotationObjects.Values)
        {
            if (obj != null) Destroy(obj);
        }
        annotationObjects.Clear();
        annotations.Clear();
    }

    /// <summary>
    /// Play audio from a voice annotation.
    /// </summary>
    public void PlayAnnotationAudio(string annotationId)
    {
        if (!annotationObjects.ContainsKey(annotationId)) return;

        AudioSource audioSource = annotationObjects[annotationId].GetComponent<AudioSource>();
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    #endregion

    #region Audio Visualization

    private void CreateWaveformRenderer()
    {
        GameObject waveformObj = new GameObject("Waveform");
        waveformObj.transform.SetParent(transform);

        waveformRenderer = waveformObj.AddComponent<LineRenderer>();
        waveformRenderer.startWidth = 0.002f;
        waveformRenderer.endWidth = 0.002f;
        waveformRenderer.positionCount = waveformResolution;
        waveformRenderer.material = new Material(Shader.Find("Sprites/Default"));
        waveformRenderer.startColor = Color.green;
        waveformRenderer.endColor = Color.green;
        waveformRenderer.enabled = false;
    }

    private void UpdateAudioVisualization()
    {
        if (!showAudioWaveform || waveformRenderer == null) return;

        waveformRenderer.enabled = isRecording;

        if (!isRecording) return;

        int micPosition = Microphone.GetPosition(microphoneDevice);
        if (micPosition <= 0 || recordingClip == null) return;

        // Get recent audio samples
        int sampleCount = Mathf.Min(waveformResolution * 10, micPosition);
        float[] samples = new float[sampleCount];

        int startPosition = Mathf.Max(0, micPosition - sampleCount);
        recordingClip.GetData(samples, startPosition);

        // Calculate waveform points
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 basePosition = cam.transform.position + cam.transform.forward * 0.3f + cam.transform.right * -0.1f;

        for (int i = 0; i < waveformResolution; i++)
        {
            int sampleIndex = i * (sampleCount / waveformResolution);
            float amplitude = Mathf.Abs(samples[Mathf.Min(sampleIndex, samples.Length - 1)]);

            Vector3 point = basePosition + cam.transform.right * (i * 0.003f);
            point += cam.transform.up * amplitude * 0.1f;

            waveformRenderer.SetPosition(i, point);
        }
    }

    private void ProcessVoiceActivation()
    {
        if (!useVoiceActivation || !isRecording) return;

        // Calculate current volume level
        int micPosition = Microphone.GetPosition(microphoneDevice);
        if (micPosition > 0 && recordingClip != null)
        {
            float[] samples = new float[256];
            recordingClip.GetData(samples, Mathf.Max(0, micPosition - 256));

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += Mathf.Abs(sample);
            }
            float averageLevel = sum / samples.Length;

            // Voice activity detection
            bool voiceDetected = averageLevel > voiceActivationLevel;

            // Could be used to auto-start/stop recording
        }
    }

    #endregion

    #region Helpers

    private string GetAuthorId()
    {
        if (MultiplayerCollaboration.Instance != null)
        {
            var participant = MultiplayerCollaboration.Instance.GetLocalParticipant();
            if (participant != null) return participant.Id;
        }
        return System.Environment.MachineName;
    }

    private string GetAuthorName()
    {
        if (MultiplayerCollaboration.Instance != null)
        {
            var participant = MultiplayerCollaboration.Instance.GetLocalParticipant();
            if (participant != null) return participant.Name;
        }
        return "Utilizador";
    }

    #endregion
}

#region Data Classes

/// <summary>
/// Represents a voice annotation.
/// </summary>
[System.Serializable]
public class VoiceAnnotation
{
    public string Id;
    public Vector3 Position;
    public Quaternion Rotation;
    public string Text;
    public string TranslatedText;
    public string Language;
    public AudioClip AudioClip;
    public DateTime Timestamp;
    public string AuthorId;
    public string AuthorName;
}

#endregion

#region Helper Components

/// <summary>
/// Makes object always face the camera.
/// </summary>
public class BillboardBehavior : MonoBehaviour
{
    void Update()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.LookAt(transform.position + cam.transform.forward);
        }
    }
}

/// <summary>
/// Adds floating animation to object.
/// </summary>
public class FloatingBehavior : MonoBehaviour
{
    public float amplitude = 0.01f;
    public float frequency = 1f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.localPosition;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) * amplitude;
        transform.localPosition = startPosition + Vector3.up * offset;
    }
}

#endregion

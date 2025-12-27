// ============================================================================
// CardiacVR - VR Platform for Cardiac Surgery Planning
// Copyright (c) 2024 Sérgio Laranjo
// Computational Cardiology, AI and Data Science for Health Lab
// Nova Medical School, Universidade Nova de Lisboa
// All rights reserved.
// ============================================================================

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// VR3S-style "Surgical Mediverse" - multiplayer collaboration system.
/// Allows multiple surgeons to meet inside the same 3D heart model for
/// collaborative surgical planning, regardless of physical location.
/// Supports real-time translation in multiple languages.
/// </summary>
public class MultiplayerCollaboration : MonoBehaviour
{
    public static MultiplayerCollaboration Instance { get; private set; }

    public event EventHandler<ParticipantEventArgs> OnParticipantJoined;
    public event EventHandler<ParticipantEventArgs> OnParticipantLeft;
    public event EventHandler<PointerEventArgs> OnPointerUpdated;
    public event EventHandler<AnnotationEventArgs> OnAnnotationReceived;
    public event EventHandler<SessionEventArgs> OnSessionStarted;
    public event EventHandler<SessionEventArgs> OnSessionEnded;

    public class ParticipantEventArgs : EventArgs
    {
        public CollaborationParticipant Participant;
    }

    public class PointerEventArgs : EventArgs
    {
        public string ParticipantId;
        public Vector3 Position;
        public Vector3 Direction;
    }

    public class AnnotationEventArgs : EventArgs
    {
        public string ParticipantId;
        public CollaborationAnnotation Annotation;
    }

    public class SessionEventArgs : EventArgs
    {
        public string SessionId;
        public string SessionName;
    }

    #region Enums

    public enum ParticipantRole
    {
        Host,
        Surgeon,
        Cardiologist,
        Fellow,
        Resident,
        Observer,
        Patient,
        Family
    }

    public enum CollaborationLanguage
    {
        English,
        Portuguese,
        Spanish,
        French,
        German,
        Italian,
        Mandarin,
        Japanese,
        Korean,
        Arabic
    }

    public enum PointerType
    {
        Laser,
        Sphere,
        Arrow,
        Hand,
        Custom
    }

    public enum SessionType
    {
        CasePlanning,
        CaseReview,
        Teaching,
        FamilyConsultation,
        LiveGuidance,
        Conference
    }

    #endregion

    [Header("Session Settings")]
    [SerializeField] private string currentSessionId;
    [SerializeField] private string currentSessionName;
    [SerializeField] private SessionType sessionType = SessionType.CasePlanning;
    [SerializeField] private bool isHost = false;
    [SerializeField] private int maxParticipants = 10;

    [Header("Local Participant")]
    [SerializeField] private string localParticipantId;
    [SerializeField] private string localParticipantName = "Surgeon";
    [SerializeField] private ParticipantRole localRole = ParticipantRole.Surgeon;
    [SerializeField] private CollaborationLanguage localLanguage = CollaborationLanguage.Portuguese;
    [SerializeField] private Color localColor = Color.blue;

    [Header("Pointer Settings")]
    [SerializeField] private PointerType pointerType = PointerType.Laser;
    [SerializeField] private float laserMaxLength = 5f;
    [SerializeField] private float pointerUpdateRate = 30f; // Hz

    [Header("Avatar Settings")]
    [SerializeField] private GameObject avatarPrefab;
    [SerializeField] private Transform avatarContainer;
    [SerializeField] private bool showAvatars = true;
    [SerializeField] private bool showNameTags = true;

    [Header("Voice Communication")]
    [SerializeField] private bool voiceEnabled = true;
    [SerializeField] private bool spatialAudio = true;
    [SerializeField] private float voiceActivationThreshold = 0.01f;

    [Header("Translation")]
    [SerializeField] private bool translationEnabled = true;
    [SerializeField] private bool autoTranslate = true;
    [SerializeField] private bool showSubtitles = true;

    // Participants
    private Dictionary<string, CollaborationParticipant> participants = new Dictionary<string, CollaborationParticipant>();
    private Dictionary<string, GameObject> participantAvatars = new Dictionary<string, GameObject>();
    private Dictionary<string, LineRenderer> participantPointers = new Dictionary<string, LineRenderer>();

    // Session state
    private bool isInSession = false;
    private float lastPointerUpdate = 0f;

    // Shared annotations
    private List<CollaborationAnnotation> sharedAnnotations = new List<CollaborationAnnotation>();

    // Synchronization
    private Queue<SyncMessage> outgoingMessages = new Queue<SyncMessage>();
    private Queue<SyncMessage> incomingMessages = new Queue<SyncMessage>();

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

        localParticipantId = System.Guid.NewGuid().ToString();
    }

    void Start()
    {
        if (avatarContainer == null)
        {
            avatarContainer = new GameObject("Participants").transform;
            avatarContainer.SetParent(transform);
        }
    }

    void Update()
    {
        if (isInSession)
        {
            UpdateLocalPointer();
            ProcessIncomingMessages();
        }
    }

    #region Session Management

    /// <summary>
    /// Create and host a new collaboration session.
    /// </summary>
    public string CreateSession(string sessionName, SessionType type)
    {
        currentSessionId = GenerateSessionId();
        currentSessionName = sessionName;
        sessionType = type;
        isHost = true;
        isInSession = true;

        // Add local participant
        AddLocalParticipant();

        OnSessionStarted?.Invoke(this, new SessionEventArgs
        {
            SessionId = currentSessionId,
            SessionName = sessionName
        });

        Debug.Log($"Created session: {sessionName} ({currentSessionId})");

        return currentSessionId;
    }

    /// <summary>
    /// Join an existing session.
    /// </summary>
    public bool JoinSession(string sessionId, string participantName, ParticipantRole role)
    {
        // In production, this would connect to a network server
        currentSessionId = sessionId;
        localParticipantName = participantName;
        localRole = role;
        isHost = false;
        isInSession = true;

        AddLocalParticipant();

        // Notify others of join
        SendMessage(new SyncMessage
        {
            Type = MessageType.ParticipantJoin,
            ParticipantId = localParticipantId,
            Data = SerializeParticipant(GetLocalParticipant())
        });

        Debug.Log($"Joined session: {sessionId}");

        return true;
    }

    /// <summary>
    /// Leave the current session.
    /// </summary>
    public void LeaveSession()
    {
        if (!isInSession) return;

        // Notify others
        SendMessage(new SyncMessage
        {
            Type = MessageType.ParticipantLeave,
            ParticipantId = localParticipantId
        });

        // Clean up
        ClearParticipants();
        isInSession = false;

        OnSessionEnded?.Invoke(this, new SessionEventArgs
        {
            SessionId = currentSessionId,
            SessionName = currentSessionName
        });

        Debug.Log("Left session");
    }

    /// <summary>
    /// End the session (host only).
    /// </summary>
    public void EndSession()
    {
        if (!isHost)
        {
            Debug.LogWarning("Only the host can end the session");
            return;
        }

        // Notify all participants
        SendMessage(new SyncMessage
        {
            Type = MessageType.SessionEnd
        });

        ClearParticipants();
        isInSession = false;

        OnSessionEnded?.Invoke(this, new SessionEventArgs
        {
            SessionId = currentSessionId,
            SessionName = currentSessionName
        });

        Debug.Log("Session ended");
    }

    private string GenerateSessionId()
    {
        return System.Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    }

    #endregion

    #region Participant Management

    private void AddLocalParticipant()
    {
        CollaborationParticipant localParticipant = new CollaborationParticipant
        {
            Id = localParticipantId,
            Name = localParticipantName,
            Role = localRole,
            Language = localLanguage,
            Color = localColor,
            IsLocal = true,
            JoinTime = DateTime.Now
        };

        participants[localParticipantId] = localParticipant;
    }

    /// <summary>
    /// Handle remote participant joining.
    /// </summary>
    private void HandleParticipantJoin(CollaborationParticipant participant)
    {
        if (participants.ContainsKey(participant.Id)) return;

        participants[participant.Id] = participant;

        // Create avatar
        CreateParticipantAvatar(participant);

        // Create pointer
        CreateParticipantPointer(participant);

        OnParticipantJoined?.Invoke(this, new ParticipantEventArgs { Participant = participant });

        Debug.Log($"Participant joined: {participant.Name} ({participant.Role})");
    }

    /// <summary>
    /// Handle remote participant leaving.
    /// </summary>
    private void HandleParticipantLeave(string participantId)
    {
        if (!participants.ContainsKey(participantId)) return;

        CollaborationParticipant participant = participants[participantId];

        // Remove avatar
        if (participantAvatars.ContainsKey(participantId))
        {
            Destroy(participantAvatars[participantId]);
            participantAvatars.Remove(participantId);
        }

        // Remove pointer
        if (participantPointers.ContainsKey(participantId))
        {
            Destroy(participantPointers[participantId].gameObject);
            participantPointers.Remove(participantId);
        }

        participants.Remove(participantId);

        OnParticipantLeft?.Invoke(this, new ParticipantEventArgs { Participant = participant });

        Debug.Log($"Participant left: {participant.Name}");
    }

    private void ClearParticipants()
    {
        foreach (var avatar in participantAvatars.Values)
        {
            if (avatar != null) Destroy(avatar);
        }
        participantAvatars.Clear();

        foreach (var pointer in participantPointers.Values)
        {
            if (pointer != null) Destroy(pointer.gameObject);
        }
        participantPointers.Clear();

        participants.Clear();
    }

    /// <summary>
    /// Get local participant info.
    /// </summary>
    public CollaborationParticipant GetLocalParticipant()
    {
        return participants.ContainsKey(localParticipantId) ? participants[localParticipantId] : null;
    }

    /// <summary>
    /// Get all participants.
    /// </summary>
    public List<CollaborationParticipant> GetAllParticipants()
    {
        return new List<CollaborationParticipant>(participants.Values);
    }

    #endregion

    #region Avatar Management

    private void CreateParticipantAvatar(CollaborationParticipant participant)
    {
        if (!showAvatars) return;

        GameObject avatar;

        if (avatarPrefab != null)
        {
            avatar = Instantiate(avatarPrefab, avatarContainer);
        }
        else
        {
            // Create simple avatar (head + torso)
            avatar = new GameObject($"Avatar_{participant.Name}");
            avatar.transform.SetParent(avatarContainer);

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(avatar.transform);
            head.transform.localPosition = new Vector3(0, 0.1f, 0);
            head.transform.localScale = Vector3.one * 0.1f;
            head.GetComponent<Renderer>().material.color = participant.Color;
            Destroy(head.GetComponent<Collider>());

            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(avatar.transform);
            body.transform.localPosition = new Vector3(0, -0.1f, 0);
            body.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
            body.GetComponent<Renderer>().material.color = participant.Color * 0.8f;
            Destroy(body.GetComponent<Collider>());

            // Name tag
            if (showNameTags)
            {
                CreateNameTag(avatar, participant);
            }
        }

        participantAvatars[participant.Id] = avatar;
    }

    private void CreateNameTag(GameObject avatar, CollaborationParticipant participant)
    {
        GameObject tagObj = new GameObject("NameTag");
        tagObj.transform.SetParent(avatar.transform);
        tagObj.transform.localPosition = new Vector3(0, 0.2f, 0);

        TextMesh textMesh = tagObj.AddComponent<TextMesh>();
        textMesh.text = $"{participant.Name}\n({GetRoleDisplayName(participant.Role)})";
        textMesh.fontSize = 20;
        textMesh.characterSize = 0.01f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = participant.Color;

        // Billboard behavior would be added here
    }

    private string GetRoleDisplayName(ParticipantRole role)
    {
        switch (role)
        {
            case ParticipantRole.Surgeon: return "Cirurgião";
            case ParticipantRole.Cardiologist: return "Cardiologista";
            case ParticipantRole.Fellow: return "Fellow";
            case ParticipantRole.Resident: return "Residente";
            case ParticipantRole.Observer: return "Observador";
            case ParticipantRole.Patient: return "Paciente";
            case ParticipantRole.Family: return "Família";
            default: return role.ToString();
        }
    }

    /// <summary>
    /// Update remote participant position.
    /// </summary>
    public void UpdateParticipantPosition(string participantId, Vector3 position, Quaternion rotation)
    {
        if (!participantAvatars.ContainsKey(participantId)) return;

        GameObject avatar = participantAvatars[participantId];
        avatar.transform.position = position;
        avatar.transform.rotation = rotation;
    }

    #endregion

    #region Pointer System

    private void CreateParticipantPointer(CollaborationParticipant participant)
    {
        GameObject pointerObj = new GameObject($"Pointer_{participant.Name}");
        pointerObj.transform.SetParent(avatarContainer);

        LineRenderer lineRenderer = pointerObj.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.002f;
        lineRenderer.endWidth = 0.001f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = participant.Color;
        lineRenderer.endColor = participant.Color * 0.5f;
        lineRenderer.positionCount = 2;

        participantPointers[participant.Id] = lineRenderer;
    }

    private void UpdateLocalPointer()
    {
        if (Time.time - lastPointerUpdate < 1f / pointerUpdateRate) return;
        lastPointerUpdate = Time.time;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 position = cam.transform.position;
        Vector3 direction = cam.transform.forward;

        // Send pointer update
        SendMessage(new SyncMessage
        {
            Type = MessageType.PointerUpdate,
            ParticipantId = localParticipantId,
            Position = position,
            Direction = direction
        });
    }

    /// <summary>
    /// Update remote participant pointer.
    /// </summary>
    public void UpdateParticipantPointer(string participantId, Vector3 position, Vector3 direction)
    {
        if (!participantPointers.ContainsKey(participantId)) return;

        LineRenderer pointer = participantPointers[participantId];

        // Raycast to find end point
        Vector3 endPoint = position + direction * laserMaxLength;

        if (Physics.Raycast(position, direction, out RaycastHit hit, laserMaxLength))
        {
            endPoint = hit.point;
        }

        pointer.SetPosition(0, position);
        pointer.SetPosition(1, endPoint);

        OnPointerUpdated?.Invoke(this, new PointerEventArgs
        {
            ParticipantId = participantId,
            Position = position,
            Direction = direction
        });
    }

    /// <summary>
    /// Show/hide participant pointers.
    /// </summary>
    public void SetPointersVisible(bool visible)
    {
        foreach (var pointer in participantPointers.Values)
        {
            pointer.enabled = visible;
        }
    }

    #endregion

    #region Annotations

    /// <summary>
    /// Create a shared annotation.
    /// </summary>
    public CollaborationAnnotation CreateAnnotation(Vector3 position, string text, AnnotationType type = AnnotationType.Text)
    {
        CollaborationAnnotation annotation = new CollaborationAnnotation
        {
            Id = System.Guid.NewGuid().ToString(),
            AuthorId = localParticipantId,
            AuthorName = localParticipantName,
            Position = position,
            Text = text,
            Type = type,
            Language = localLanguage,
            Timestamp = DateTime.Now
        };

        sharedAnnotations.Add(annotation);

        // Send to others
        SendMessage(new SyncMessage
        {
            Type = MessageType.Annotation,
            ParticipantId = localParticipantId,
            Data = JsonUtility.ToJson(annotation)
        });

        return annotation;
    }

    /// <summary>
    /// Handle remote annotation.
    /// </summary>
    private void HandleAnnotation(CollaborationAnnotation annotation)
    {
        sharedAnnotations.Add(annotation);

        // Translate if needed
        if (translationEnabled && annotation.Language != localLanguage)
        {
            annotation.TranslatedText = TranslateText(annotation.Text, annotation.Language, localLanguage);
        }

        OnAnnotationReceived?.Invoke(this, new AnnotationEventArgs
        {
            ParticipantId = annotation.AuthorId,
            Annotation = annotation
        });
    }

    /// <summary>
    /// Get all shared annotations.
    /// </summary>
    public List<CollaborationAnnotation> GetAnnotations()
    {
        return new List<CollaborationAnnotation>(sharedAnnotations);
    }

    /// <summary>
    /// Remove an annotation.
    /// </summary>
    public void RemoveAnnotation(string annotationId)
    {
        sharedAnnotations.RemoveAll(a => a.Id == annotationId);

        SendMessage(new SyncMessage
        {
            Type = MessageType.AnnotationRemove,
            Data = annotationId
        });
    }

    #endregion

    #region Translation

    /// <summary>
    /// Translate text between languages.
    /// </summary>
    public string TranslateText(string text, CollaborationLanguage from, CollaborationLanguage to)
    {
        // In production, this would call a translation API
        // For now, return original text with language marker
        return $"[{GetLanguageCode(to)}] {text}";
    }

    private string GetLanguageCode(CollaborationLanguage language)
    {
        switch (language)
        {
            case CollaborationLanguage.English: return "EN";
            case CollaborationLanguage.Portuguese: return "PT";
            case CollaborationLanguage.Spanish: return "ES";
            case CollaborationLanguage.French: return "FR";
            case CollaborationLanguage.German: return "DE";
            case CollaborationLanguage.Italian: return "IT";
            case CollaborationLanguage.Mandarin: return "ZH";
            case CollaborationLanguage.Japanese: return "JA";
            case CollaborationLanguage.Korean: return "KO";
            case CollaborationLanguage.Arabic: return "AR";
            default: return "EN";
        }
    }

    /// <summary>
    /// Set local participant language.
    /// </summary>
    public void SetLanguage(CollaborationLanguage language)
    {
        localLanguage = language;

        if (participants.ContainsKey(localParticipantId))
        {
            participants[localParticipantId].Language = language;
        }

        // Re-translate existing annotations
        foreach (var annotation in sharedAnnotations)
        {
            if (annotation.Language != localLanguage)
            {
                annotation.TranslatedText = TranslateText(annotation.Text, annotation.Language, localLanguage);
            }
        }
    }

    #endregion

    #region Message Handling

    private void SendMessage(SyncMessage message)
    {
        message.Timestamp = DateTime.Now;
        outgoingMessages.Enqueue(message);

        // In production, this would send over network
        // For local testing, process immediately
        ProcessMessage(message);
    }

    private void ProcessIncomingMessages()
    {
        while (incomingMessages.Count > 0)
        {
            SyncMessage message = incomingMessages.Dequeue();
            ProcessMessage(message);
        }
    }

    private void ProcessMessage(SyncMessage message)
    {
        switch (message.Type)
        {
            case MessageType.ParticipantJoin:
                CollaborationParticipant participant = DeserializeParticipant(message.Data);
                if (participant != null)
                {
                    HandleParticipantJoin(participant);
                }
                break;

            case MessageType.ParticipantLeave:
                HandleParticipantLeave(message.ParticipantId);
                break;

            case MessageType.PointerUpdate:
                UpdateParticipantPointer(message.ParticipantId, message.Position, message.Direction);
                break;

            case MessageType.Annotation:
                CollaborationAnnotation annotation = JsonUtility.FromJson<CollaborationAnnotation>(message.Data);
                if (annotation != null)
                {
                    HandleAnnotation(annotation);
                }
                break;

            case MessageType.SessionEnd:
                LeaveSession();
                break;
        }
    }

    private string SerializeParticipant(CollaborationParticipant participant)
    {
        return JsonUtility.ToJson(participant);
    }

    private CollaborationParticipant DeserializeParticipant(string data)
    {
        try
        {
            return JsonUtility.FromJson<CollaborationParticipant>(data);
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Getters

    /// <summary>
    /// Check if in active session.
    /// </summary>
    public bool IsInSession()
    {
        return isInSession;
    }

    /// <summary>
    /// Check if local participant is host.
    /// </summary>
    public bool IsHost()
    {
        return isHost;
    }

    /// <summary>
    /// Get current session ID.
    /// </summary>
    public string GetSessionId()
    {
        return currentSessionId;
    }

    /// <summary>
    /// Get participant count.
    /// </summary>
    public int GetParticipantCount()
    {
        return participants.Count;
    }

    #endregion
}

#region Data Classes

/// <summary>
/// Represents a collaboration session participant.
/// </summary>
[System.Serializable]
public class CollaborationParticipant
{
    public string Id;
    public string Name;
    public MultiplayerCollaboration.ParticipantRole Role;
    public MultiplayerCollaboration.CollaborationLanguage Language;
    public Color Color;
    public bool IsLocal;
    public DateTime JoinTime;
    public Vector3 Position;
    public Quaternion Rotation;
    public bool IsSpeaking;
}

/// <summary>
/// Represents a shared annotation.
/// </summary>
[System.Serializable]
public class CollaborationAnnotation
{
    public string Id;
    public string AuthorId;
    public string AuthorName;
    public Vector3 Position;
    public string Text;
    public string TranslatedText;
    public AnnotationType Type;
    public MultiplayerCollaboration.CollaborationLanguage Language;
    public DateTime Timestamp;
}

public enum AnnotationType
{
    Text,
    Voice,
    Drawing,
    Measurement,
    Marker
}

/// <summary>
/// Synchronization message.
/// </summary>
public class SyncMessage
{
    public MessageType Type;
    public string ParticipantId;
    public Vector3 Position;
    public Vector3 Direction;
    public string Data;
    public DateTime Timestamp;
}

public enum MessageType
{
    ParticipantJoin,
    ParticipantLeave,
    ParticipantUpdate,
    PointerUpdate,
    Annotation,
    AnnotationRemove,
    DevicePlaced,
    DeviceRemoved,
    SessionEnd
}

#endregion

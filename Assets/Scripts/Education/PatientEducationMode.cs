using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// VR3S-style patient and family education mode.
/// Enables guided tours through the heart with simplified explanations,
/// helping families understand complex cardiac conditions and planned surgeries.
/// "A mom told us this was the first time she ever understood her child's heart condition."
/// </summary>
public class PatientEducationMode : MonoBehaviour
{
    public static PatientEducationMode Instance { get; private set; }

    public event EventHandler<EducationEventArgs> OnEducationStarted;
    public event EventHandler<EducationEventArgs> OnEducationEnded;
    public event EventHandler<TopicEventArgs> OnTopicChanged;
    public event EventHandler<NarrationEventArgs> OnNarrationStarted;

    public class EducationEventArgs : EventArgs
    {
        public EducationSession Session;
    }

    public class TopicEventArgs : EventArgs
    {
        public EducationTopic Topic;
        public int TopicIndex;
    }

    public class NarrationEventArgs : EventArgs
    {
        public string Text;
        public AudioClip Audio;
    }

    #region Enums

    public enum EducationLevel
    {
        Child,          // Age 5-10: Very simple, uses analogies
        Teen,           // Age 11-17: More detail, some medical terms
        Adult,          // Age 18+: Full explanation with medical terms
        Professional    // For medical staff/students
    }

    public enum CardiacCondition
    {
        // Septal Defects
        ASD,            // Atrial Septal Defect
        VSD,            // Ventricular Septal Defect
        AVSD,           // Atrioventricular Septal Defect

        // Outflow Abnormalities
        ToF,            // Tetralogy of Fallot
        DORV,           // Double Outlet Right Ventricle
        TGA,            // Transposition of Great Arteries
        Truncus,        // Truncus Arteriosus

        // Single Ventricle
        HLHS,           // Hypoplastic Left Heart Syndrome
        Tricuspid_Atresia,
        Pulmonary_Atresia,

        // Valve Diseases
        Aortic_Stenosis,
        Pulmonary_Stenosis,
        Mitral_Regurgitation,
        Tricuspid_Regurgitation,

        // Coronary Artery Disease
        CAD,
        ALCAPA,

        // Other
        Coarctation,
        PDA,
        Normal_Heart,   // For comparison
        Custom
    }

    public enum SurgeryType
    {
        // Open Heart
        VSD_Repair,
        ASD_Repair,
        ToF_Repair,
        Arterial_Switch,
        Rastelli,
        Fontan,
        Glenn,
        Norwood,

        // Valve Surgery
        Valve_Replacement,
        Valve_Repair,
        Ross_Procedure,

        // Catheterization
        Device_Closure,
        Balloon_Valvuloplasty,
        Stent_Placement,

        // Coronary
        CABG,
        PCI,

        Custom
    }

    #endregion

    [Header("Session Settings")]
    [SerializeField] private bool isInEducationMode = false;
    [SerializeField] private EducationLevel currentLevel = EducationLevel.Adult;
    [SerializeField] private CardiacCondition currentCondition = CardiacCondition.Normal_Heart;
    [SerializeField] private SurgeryType plannedSurgery = SurgeryType.Custom;

    [Header("Content")]
    [SerializeField] private List<EducationTopic> topics = new List<EducationTopic>();
    [SerializeField] private int currentTopicIndex = 0;
    [SerializeField] private float narrationSpeed = 1f;

    [Header("Visualization")]
    [SerializeField] private bool useSimplifiedVisuals = true;
    [SerializeField] private bool highlightAffectedAreas = true;
    [SerializeField] private bool showBloodFlow = true;
    [SerializeField] private bool showComparisons = true;

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.9f, 0.4f, 0.4f);
    [SerializeField] private Color abnormalColor = new Color(0.4f, 0.4f, 0.9f);
    [SerializeField] private Color surgicalColor = new Color(0.4f, 0.9f, 0.4f);
    [SerializeField] private Color oxygenatedBlood = new Color(0.9f, 0.2f, 0.2f);
    [SerializeField] private Color deoxygenatedBlood = new Color(0.2f, 0.2f, 0.9f);

    [Header("Audio")]
    [SerializeField] private AudioSource narrationAudioSource;
    [SerializeField] private bool autoNarrate = true;

    [Header("UI")]
    [SerializeField] private GameObject educationUIPanel;
    [SerializeField] private Transform labelContainer;

    // Current session
    private EducationSession currentSession;
    private Coroutine currentTourCoroutine;
    private List<GameObject> educationLabels = new List<GameObject>();
    private List<GameObject> bloodFlowParticles = new List<GameObject>();

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

        InitializeEducationContent();
    }

    void Start()
    {
        if (labelContainer == null)
        {
            labelContainer = new GameObject("EducationLabels").transform;
            labelContainer.SetParent(transform);
        }

        if (narrationAudioSource == null)
        {
            narrationAudioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    #region Content Initialization

    private void InitializeEducationContent()
    {
        // Normal Heart Topics
        CreateNormalHeartTopics();

        // Condition-specific topics would be loaded based on patient
        CreateConditionTopics();
    }

    private void CreateNormalHeartTopics()
    {
        // Topic: Heart Overview
        topics.Add(new EducationTopic
        {
            Id = "heart_overview",
            Title = "O Coração",
            TitleChild = "O Teu Coração",
            Condition = CardiacCondition.Normal_Heart,
            Chamber = HeartFlyThroughController.HeartChamber.Outside,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "O teu coração é como uma bomba muito especial! Ele bate o dia todo para mandar sangue para todo o teu corpo. É do tamanho do teu punho fechado!" },
                { EducationLevel.Teen, "O coração é um órgão muscular que funciona como uma bomba, enviando sangue para todo o corpo. Bate cerca de 100.000 vezes por dia!" },
                { EducationLevel.Adult, "O coração é um órgão muscular com quatro câmaras que bombeia sangue através do sistema circulatório. O lado direito recebe sangue pobre em oxigénio e envia-o para os pulmões. O lado esquerdo recebe sangue oxigenado dos pulmões e distribui-o para todo o corpo." },
                { EducationLevel.Professional, "O coração é um órgão muscular com quatro câmaras: duas aurículas e dois ventrículos. O débito cardíaco é determinado pela frequência cardíaca e volume sistólico, tipicamente 5-6 L/min em repouso." }
            }
        });

        // Topic: Right Atrium
        topics.Add(new EducationTopic
        {
            Id = "right_atrium",
            Title = "Aurícula Direita",
            TitleChild = "A Sala de Espera Azul",
            Condition = CardiacCondition.Normal_Heart,
            Chamber = HeartFlyThroughController.HeartChamber.RightAtrium,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "Esta é a sala de espera para o sangue cansado! O sangue azul (que precisa de ar novo) espera aqui antes de ir para os pulmões." },
                { EducationLevel.Teen, "A aurícula direita recebe o sangue que já foi usado pelo corpo e precisa de ir aos pulmões para receber oxigénio novo." },
                { EducationLevel.Adult, "A aurícula direita recebe sangue venoso (desoxigenado) da veia cava superior, veia cava inferior e seio coronário. O sangue passa então para o ventrículo direito através da válvula tricúspide." },
                { EducationLevel.Professional, "A aurícula direita recebe retorno venoso sistémico através da VCS, VCI e seio coronário. A condução elétrica inicia-se no nodo SA localizado na junção entre a VCS e a AD." }
            }
        });

        // Topic: Right Ventricle
        topics.Add(new EducationTopic
        {
            Id = "right_ventricle",
            Title = "Ventrículo Direito",
            TitleChild = "A Bomba para os Pulmões",
            Condition = CardiacCondition.Normal_Heart,
            Chamber = HeartFlyThroughController.HeartChamber.RightVentricle,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "Esta é a bomba que manda o sangue azul para os pulmões! Quando o coração bate, esta bomba empurra o sangue para ir buscar ar novo." },
                { EducationLevel.Teen, "O ventrículo direito bombeia o sangue para os pulmões através da artéria pulmonar. Lá o sangue deixa o dióxido de carbono e recebe oxigénio novo." },
                { EducationLevel.Adult, "O ventrículo direito tem paredes mais finas que o esquerdo porque apenas bombeia para o circuito pulmonar de baixa pressão. O sangue sai pela válvula pulmonar para a artéria pulmonar." },
                { EducationLevel.Professional, "O VD tem geometria crescente única com três componentes: entrada, trabécula e infundíbulo (RVOT). Gera pressões de 15-30/4-12 mmHg em condições normais." }
            }
        });

        // Topic: Lungs
        topics.Add(new EducationTopic
        {
            Id = "lungs",
            Title = "Os Pulmões",
            TitleChild = "As Fábricas de Ar",
            Condition = CardiacCondition.Normal_Heart,
            Chamber = HeartFlyThroughController.HeartChamber.PulmonaryArtery,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "Os pulmões são como esponjas mágicas! Quando respiras, eles enchem-se de ar e dão oxigénio ao sangue. O sangue fica vermelho e feliz!" },
                { EducationLevel.Teen, "Nos pulmões, o sangue passa por pequenos vasos (capilares) onde troca dióxido de carbono por oxigénio. Por isso precisamos de respirar!" },
                { EducationLevel.Adult, "A circulação pulmonar é onde ocorrem as trocas gasosas. O sangue desoxigenado liberta CO2 e capta O2 nos alvéolos pulmonares, retornando oxigenado pelas veias pulmonares." },
                { EducationLevel.Professional, "As trocas gasosas ocorrem na membrana alvéolo-capilar com espessura de ~0.5μm. A capacidade de difusão pulmonar (DLCO) é ~25 mL/min/mmHg em repouso." }
            }
        });

        // Topic: Left Atrium
        topics.Add(new EducationTopic
        {
            Id = "left_atrium",
            Title = "Aurícula Esquerda",
            TitleChild = "A Sala de Espera Vermelha",
            Condition = CardiacCondition.Normal_Heart,
            Chamber = HeartFlyThroughController.HeartChamber.LeftAtrium,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "Esta é a sala de espera para o sangue vermelho que vem dos pulmões! Está cheio de ar novo e pronto para ir alimentar todo o teu corpo!" },
                { EducationLevel.Teen, "A aurícula esquerda recebe o sangue que já foi aos pulmões buscar oxigénio. Este sangue vermelho vai ser enviado para todo o corpo." },
                { EducationLevel.Adult, "A aurícula esquerda recebe sangue oxigenado das quatro veias pulmonares. Tem paredes ligeiramente mais espessas que a aurícula direita e inclui o apêndice auricular esquerdo." },
                { EducationLevel.Professional, "A AE recebe retorno venoso pulmonar das 4 VPs. O apêndice auricular esquerdo é local comum de formação de trombos em FA. A pressão normal é 4-12 mmHg." }
            }
        });

        // Topic: Left Ventricle
        topics.Add(new EducationTopic
        {
            Id = "left_ventricle",
            Title = "Ventrículo Esquerdo",
            TitleChild = "A Super Bomba",
            Condition = CardiacCondition.Normal_Heart,
            Chamber = HeartFlyThroughController.HeartChamber.LeftVentricle,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "Esta é a bomba mais forte do coração! Manda o sangue vermelho para todo o teu corpo - da cabeça até aos pés! É um verdadeiro super-herói!" },
                { EducationLevel.Teen, "O ventrículo esquerdo é a câmara mais forte do coração. Tem paredes grossas porque precisa de bombear sangue para todo o corpo com muita força." },
                { EducationLevel.Adult, "O ventrículo esquerdo tem paredes 2-3 vezes mais espessas que o direito pois bombeia contra a resistência sistémica. A fração de ejeção normal é 55-70%." },
                { EducationLevel.Professional, "O VE tem geometria elipsoidal otimizada para gerar pressões de 120/8 mmHg. A contractilidade é avaliada por FE, strain longitudinal global (normal > -18%), e dP/dt." }
            }
        });

        // Topic: Aorta
        topics.Add(new EducationTopic
        {
            Id = "aorta",
            Title = "Aorta",
            TitleChild = "A Auto-estrada do Sangue",
            Condition = CardiacCondition.Normal_Heart,
            Chamber = HeartFlyThroughController.HeartChamber.AscendingAorta,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "A aorta é como uma auto-estrada super grande! O sangue vermelho sai do coração por aqui e viaja para todas as partes do teu corpo!" },
                { EducationLevel.Teen, "A aorta é a maior artéria do corpo. Sai do ventrículo esquerdo e divide-se em ramos que levam sangue oxigenado a todos os órgãos." },
                { EducationLevel.Adult, "A aorta é a principal artéria do corpo. Origina-se no ventrículo esquerdo, curva sobre o coração (arco aórtico) e desce pelo tórax e abdómen, ramificando-se em artérias menores." },
                { EducationLevel.Professional, "A aorta tem diâmetro normal de 2.1 cm/m² na raiz. Os seios de Valsalva dão origem às coronárias. O arco dá origem ao tronco braquiocefálico, carótida comum esquerda e subclávia esquerda." }
            }
        });
    }

    private void CreateConditionTopics()
    {
        // VSD Topics
        topics.Add(new EducationTopic
        {
            Id = "vsd_what",
            Title = "Comunicação Interventricular (CIV)",
            TitleChild = "O Buraquinho no Coração",
            Condition = CardiacCondition.VSD,
            Chamber = HeartFlyThroughController.HeartChamber.InterventricularSeptum,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "No teu coração há uma parede que separa os dois lados. Tu tens um pequeno buraquinho nessa parede. É por isso que vamos fazer uma pequena operação para tapar esse buraquinho!" },
                { EducationLevel.Teen, "Uma CIV é um buraco na parede entre os dois ventrículos. Isto faz com que sangue passe do lado esquerdo para o direito, o que pode sobrecarregar o coração e os pulmões." },
                { EducationLevel.Adult, "A comunicação interventricular é um defeito no septo ventricular que permite passagem de sangue do VE (alta pressão) para o VD. Dependendo do tamanho, pode causar sobrecarga de volume pulmonar e insuficiência cardíaca." },
                { EducationLevel.Professional, "CIVs classificam-se por localização: perimembranosas (80%), musculares, duplamente relacionadas (outlet), e inlet. O shunt E-D depende do tamanho do defeito e RVP. Indicação cirúrgica com Qp:Qs > 1.5:1." }
            }
        });

        // ToF Topics
        topics.Add(new EducationTopic
        {
            Id = "tof_what",
            Title = "Tetralogia de Fallot",
            TitleChild = "Os Quatro Problemas",
            Condition = CardiacCondition.ToF,
            Chamber = HeartFlyThroughController.HeartChamber.RVOT,
            Explanations = new Dictionary<EducationLevel, string>
            {
                { EducationLevel.Child, "O teu coração nasceu com quatro coisas diferentes. Mas não te preocupes! Os médicos sabem exatamente como ajudar, e vais ficar muito melhor depois da operação!" },
                { EducationLevel.Teen, "A Tetralogia de Fallot tem 4 características: um buraco entre os ventrículos, estreitamento na saída para os pulmões, a aorta está deslocada, e o ventrículo direito está mais grosso." },
                { EducationLevel.Adult, "A Tetralogia de Fallot consiste em: CIV, estenose pulmonar (infundibular e/ou valvular), cavalgamento da aorta sobre o septo, e hipertrofia do VD. A cianose depende do grau de obstrução pulmonar." },
                { EducationLevel.Professional, "A ToF resulta de desvio anterocefálico do septo outlet. O espectro inclui ToF com AP (atresia pulmonar) e ToF com válvula pulmonar ausente. Reparação cirúrgica inclui encerramento de CIV e alívio de obstrução do RVOT." }
            }
        });
    }

    #endregion

    #region Session Control

    /// <summary>
    /// Start education session.
    /// </summary>
    public void StartEducationSession(CardiacCondition condition, EducationLevel level, SurgeryType surgery = SurgeryType.Custom)
    {
        currentCondition = condition;
        currentLevel = level;
        plannedSurgery = surgery;

        currentSession = new EducationSession
        {
            Condition = condition,
            Level = level,
            Surgery = surgery,
            StartTime = DateTime.Now
        };

        isInEducationMode = true;
        currentTopicIndex = 0;

        // Filter topics for this condition
        currentSession.Topics = GetTopicsForCondition(condition);

        // Enable simplified visuals
        if (useSimplifiedVisuals)
        {
            ApplySimplifiedVisuals();
        }

        OnEducationStarted?.Invoke(this, new EducationEventArgs { Session = currentSession });

        Debug.Log($"Started education session: {condition} at {level} level");

        // Start first topic
        if (currentSession.Topics.Count > 0)
        {
            ShowTopic(0);
        }
    }

    /// <summary>
    /// End education session.
    /// </summary>
    public void EndEducationSession()
    {
        if (!isInEducationMode) return;

        isInEducationMode = false;

        // Stop any running coroutines
        if (currentTourCoroutine != null)
        {
            StopCoroutine(currentTourCoroutine);
        }

        // Stop narration
        if (narrationAudioSource != null && narrationAudioSource.isPlaying)
        {
            narrationAudioSource.Stop();
        }

        // Clean up visuals
        ClearEducationVisuals();

        currentSession.EndTime = DateTime.Now;

        OnEducationEnded?.Invoke(this, new EducationEventArgs { Session = currentSession });

        Debug.Log("Ended education session");
    }

    private List<EducationTopic> GetTopicsForCondition(CardiacCondition condition)
    {
        List<EducationTopic> relevantTopics = new List<EducationTopic>();

        // Always include normal heart basics
        relevantTopics.AddRange(topics.FindAll(t => t.Condition == CardiacCondition.Normal_Heart));

        // Add condition-specific topics
        relevantTopics.AddRange(topics.FindAll(t => t.Condition == condition));

        return relevantTopics;
    }

    #endregion

    #region Topic Navigation

    /// <summary>
    /// Show specific topic.
    /// </summary>
    public void ShowTopic(int index)
    {
        if (currentSession == null || index < 0 || index >= currentSession.Topics.Count) return;

        currentTopicIndex = index;
        EducationTopic topic = currentSession.Topics[index];

        // Navigate to chamber
        if (HeartFlyThroughController.Instance != null)
        {
            HeartFlyThroughController.Instance.NavigateToChamber(topic.Chamber);
        }

        // Show labels and highlights
        ShowTopicVisuals(topic);

        // Narrate
        if (autoNarrate)
        {
            NarrateTopic(topic);
        }

        OnTopicChanged?.Invoke(this, new TopicEventArgs
        {
            Topic = topic,
            TopicIndex = index
        });
    }

    /// <summary>
    /// Go to next topic.
    /// </summary>
    public void NextTopic()
    {
        if (currentTopicIndex < currentSession.Topics.Count - 1)
        {
            ShowTopic(currentTopicIndex + 1);
        }
    }

    /// <summary>
    /// Go to previous topic.
    /// </summary>
    public void PreviousTopic()
    {
        if (currentTopicIndex > 0)
        {
            ShowTopic(currentTopicIndex - 1);
        }
    }

    /// <summary>
    /// Start guided tour through all topics.
    /// </summary>
    public void StartGuidedTour()
    {
        if (currentTourCoroutine != null)
        {
            StopCoroutine(currentTourCoroutine);
        }

        currentTourCoroutine = StartCoroutine(GuidedTourCoroutine());
    }

    private IEnumerator GuidedTourCoroutine()
    {
        for (int i = 0; i < currentSession.Topics.Count; i++)
        {
            ShowTopic(i);

            // Wait for narration to finish
            if (narrationAudioSource.isPlaying)
            {
                while (narrationAudioSource.isPlaying)
                {
                    yield return null;
                }
            }
            else
            {
                // Estimate reading time
                string text = GetExplanationForLevel(currentSession.Topics[i]);
                float readingTime = text.Length * 0.05f; // ~50ms per character
                yield return new WaitForSeconds(Mathf.Max(5f, readingTime));
            }

            // Pause between topics
            yield return new WaitForSeconds(2f);
        }
    }

    #endregion

    #region Visuals

    private void ShowTopicVisuals(EducationTopic topic)
    {
        ClearEducationVisuals();

        // Create title label
        CreateLabel(topic.Title, Vector3.up * 0.2f, true);

        // Create explanation panel
        string explanation = GetExplanationForLevel(topic);
        CreateExplanationPanel(explanation);

        // Highlight affected area
        if (highlightAffectedAreas)
        {
            HighlightChamber(topic.Chamber);
        }

        // Show blood flow
        if (showBloodFlow)
        {
            ShowBloodFlowForChamber(topic.Chamber);
        }
    }

    private string GetExplanationForLevel(EducationTopic topic)
    {
        if (topic.Explanations.ContainsKey(currentLevel))
        {
            return topic.Explanations[currentLevel];
        }

        // Fallback to adult level
        if (topic.Explanations.ContainsKey(EducationLevel.Adult))
        {
            return topic.Explanations[EducationLevel.Adult];
        }

        return topic.Title;
    }

    private void CreateLabel(string text, Vector3 offset, bool isTitle)
    {
        GameObject labelObj = new GameObject("EducationLabel");
        labelObj.transform.SetParent(labelContainer);

        Camera cam = Camera.main;
        if (cam != null)
        {
            labelObj.transform.position = cam.transform.position + cam.transform.forward * 0.5f + offset;
        }

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = isTitle ? 32 : 24;
        textMesh.characterSize = 0.01f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = isTitle ? Color.yellow : Color.white;

        labelObj.AddComponent<BillboardBehavior>();
        educationLabels.Add(labelObj);
    }

    private void CreateExplanationPanel(string text)
    {
        GameObject panelObj = new GameObject("ExplanationPanel");
        panelObj.transform.SetParent(labelContainer);

        Camera cam = Camera.main;
        if (cam != null)
        {
            panelObj.transform.position = cam.transform.position + cam.transform.forward * 0.5f + Vector3.down * 0.1f;
        }

        // Background
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.transform.SetParent(panelObj.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(0.4f, 0.15f, 1f);

        Renderer bgRenderer = bg.GetComponent<Renderer>();
        bgRenderer.material = new Material(Shader.Find("UI/Default"));
        bgRenderer.material.color = new Color(0.1f, 0.1f, 0.2f, 0.85f);
        Destroy(bg.GetComponent<Collider>());

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panelObj.transform);
        textObj.transform.localPosition = new Vector3(0, 0, -0.001f);

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = WrapText(text, 50);
        textMesh.fontSize = 20;
        textMesh.characterSize = 0.008f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        panelObj.AddComponent<BillboardBehavior>();
        educationLabels.Add(panelObj);
    }

    private string WrapText(string text, int maxCharsPerLine)
    {
        string[] words = text.Split(' ');
        string result = "";
        string currentLine = "";

        foreach (string word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxCharsPerLine)
            {
                result += currentLine.Trim() + "\n";
                currentLine = "";
            }
            currentLine += word + " ";
        }
        result += currentLine.Trim();

        return result;
    }

    private void HighlightChamber(HeartFlyThroughController.HeartChamber chamber)
    {
        // In production, this would highlight the specific mesh/region
        Debug.Log($"Highlighting chamber: {chamber}");
    }

    private void ShowBloodFlowForChamber(HeartFlyThroughController.HeartChamber chamber)
    {
        // In production, this would create animated blood flow particles
        Debug.Log($"Showing blood flow for: {chamber}");
    }

    private void ApplySimplifiedVisuals()
    {
        // Reduce visual complexity for easier understanding
        if (VolumeRenderingController.Instance != null)
        {
            VolumeRenderingController.Instance.ApplyPreset(VolumeRenderingController.VisualizationPreset.Cardiac);
        }
    }

    private void ClearEducationVisuals()
    {
        foreach (var label in educationLabels)
        {
            if (label != null) Destroy(label);
        }
        educationLabels.Clear();

        foreach (var particle in bloodFlowParticles)
        {
            if (particle != null) Destroy(particle);
        }
        bloodFlowParticles.Clear();
    }

    #endregion

    #region Narration

    private void NarrateTopic(EducationTopic topic)
    {
        string text = GetExplanationForLevel(topic);

        // Check for pre-recorded audio
        AudioClip audioClip = GetAudioClipForTopic(topic.Id, currentLevel);

        if (audioClip != null)
        {
            narrationAudioSource.clip = audioClip;
            narrationAudioSource.Play();
        }

        OnNarrationStarted?.Invoke(this, new NarrationEventArgs
        {
            Text = text,
            Audio = audioClip
        });
    }

    private AudioClip GetAudioClipForTopic(string topicId, EducationLevel level)
    {
        // In production, load from Resources or streaming assets
        string path = $"Education/Audio/{topicId}_{level}";
        return Resources.Load<AudioClip>(path);
    }

    /// <summary>
    /// Stop current narration.
    /// </summary>
    public void StopNarration()
    {
        if (narrationAudioSource != null && narrationAudioSource.isPlaying)
        {
            narrationAudioSource.Stop();
        }
    }

    /// <summary>
    /// Set narration speed.
    /// </summary>
    public void SetNarrationSpeed(float speed)
    {
        narrationSpeed = Mathf.Clamp(speed, 0.5f, 2f);
        if (narrationAudioSource != null)
        {
            narrationAudioSource.pitch = narrationSpeed;
        }
    }

    #endregion

    #region Level Control

    /// <summary>
    /// Set education level.
    /// </summary>
    public void SetEducationLevel(EducationLevel level)
    {
        currentLevel = level;

        // Refresh current topic display
        if (isInEducationMode && currentSession != null && currentSession.Topics.Count > 0)
        {
            ShowTopic(currentTopicIndex);
        }
    }

    /// <summary>
    /// Get current education level.
    /// </summary>
    public EducationLevel GetEducationLevel()
    {
        return currentLevel;
    }

    #endregion

    #region Getters

    /// <summary>
    /// Check if in education mode.
    /// </summary>
    public bool IsInEducationMode()
    {
        return isInEducationMode;
    }

    /// <summary>
    /// Get current topic.
    /// </summary>
    public EducationTopic GetCurrentTopic()
    {
        if (currentSession != null && currentTopicIndex >= 0 && currentTopicIndex < currentSession.Topics.Count)
        {
            return currentSession.Topics[currentTopicIndex];
        }
        return null;
    }

    /// <summary>
    /// Get total topic count.
    /// </summary>
    public int GetTopicCount()
    {
        return currentSession != null ? currentSession.Topics.Count : 0;
    }

    /// <summary>
    /// Get current topic index.
    /// </summary>
    public int GetCurrentTopicIndex()
    {
        return currentTopicIndex;
    }

    #endregion
}

#region Data Classes

/// <summary>
/// Represents an education topic.
/// </summary>
[System.Serializable]
public class EducationTopic
{
    public string Id;
    public string Title;
    public string TitleChild;
    public PatientEducationMode.CardiacCondition Condition;
    public HeartFlyThroughController.HeartChamber Chamber;
    public Dictionary<PatientEducationMode.EducationLevel, string> Explanations = new Dictionary<PatientEducationMode.EducationLevel, string>();
    public AudioClip NarrationAudio;
    public List<string> RelatedTopics = new List<string>();
}

/// <summary>
/// Represents an education session.
/// </summary>
[System.Serializable]
public class EducationSession
{
    public PatientEducationMode.CardiacCondition Condition;
    public PatientEducationMode.EducationLevel Level;
    public PatientEducationMode.SurgeryType Surgery;
    public List<EducationTopic> Topics = new List<EducationTopic>();
    public DateTime StartTime;
    public DateTime EndTime;
}

#endregion

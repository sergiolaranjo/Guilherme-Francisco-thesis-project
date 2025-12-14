# CardiacVR - VR Platform for Cardiac Surgery Planning

<p align="center">
  <img src="https://img.shields.io/badge/Unity-2022.3.49f1-blue?logo=unity" alt="Unity Version"/>
  <img src="https://img.shields.io/badge/Platform-Meta%20Quest-orange" alt="Platform"/>
  <img src="https://img.shields.io/badge/License-All%20Rights%20Reserved-red" alt="License"/>
</p>

A professional virtual reality application for cardiac surgery planning, designed to improve spatial perception and diagnostic capabilities for complex cardiac conditions, particularly congenital heart diseases. Inspired by Cincinnati Children's Hospital VR3S (VR Surgical Simulation Suite).

---

**Copyright (c) 2024 Sérgio Laranjo**
**Computational Cardiology, AI and Data Science for Health Lab**
**Nova Medical School, Universidade Nova de Lisboa**

---

## Demo

<p align="center">
  <a href="https://youtu.be/U2nSuAB95VU">
    <img src="https://img.youtube.com/vi/U2nSuAB95VU/0.jpg" alt="CardiacVR Demo" width="560"/>
  </a>
</p>

Watch the demo: [https://youtu.be/U2nSuAB95VU](https://youtu.be/U2nSuAB95VU)

---

## Key Features

### Core Visualization
- **Volume Rendering** - Real-time DICOM/CT/MRI volume visualization with raymarching
- **Segmented Mesh Rendering** - Beautiful visualization of segmented anatomical structures
- **STL Import** - Load 3D models from STL files (ASCII and Binary formats)
- **Professional Lighting** - 3-point lighting system with multiple presets

### Surgical Planning Tools
- **Device Placement** - Virtual placement of valves, stents, and medical devices
- **Baffle Designer** - Intracardiac baffle design for complex repairs (DORV, Taussig-Bing)
- **Heart Fly-Through** - Navigate through heart chambers for surgical planning
- **Measurement Tools** - Linear, curved, volume, area, and radius measurements

### Surgery Simulation
- **Surgical Instruments** - Scalpel, electrocautery, ablation, suture, and clamp tools
- **Tissue Interaction** - Realistic cutting and manipulation
- **Procedure Recording** - Record and review surgical plans

### Collaboration
- **Multiplayer Sessions** - Multiple surgeons can meet inside the same 3D heart
- **Voice Annotations** - Voice-to-text annotations with multi-language support
- **Real-time Translation** - Break language barriers in international collaboration

### Patient Education
- **Guided Tours** - Step-by-step heart exploration with simplified explanations
- **Educational Mode** - Optimized visuals for patient and family understanding
- **Condition Visualization** - Help families understand complex cardiac conditions

### Mixed Reality
- **VR/AR Toggle** - Switch between virtual and augmented reality modes
- **Passthrough Support** - See real environment with holographic heart overlay
- **Spatial Anchoring** - Place and anchor models in physical space

---

## Visual Systems

### Rendering
| System | Description |
|--------|-------------|
| **PostProcessingManager** | Bloom, color grading, vignette, ambient occlusion |
| **SegmentedMeshRenderer** | Smooth surface rendering with anatomical categorization |
| **MeshEnhancementSystem** | Laplacian, Taubin, HC smoothing algorithms |
| **VisualEffectsSystem** | Selection, hover, focus, and X-ray effects |
| **AmbientEnvironmentSystem** | 9 environment presets for different scenarios |

### Materials & Shaders
- **AnatomicalTissue.shader** - Subsurface scattering, fresnel, rim lighting
- **TranslucentAnatomy.shader** - For valves and translucent structures
- **SelectionOutline.shader** - Pulsing selection outlines
- **VolumeRendering.shader** - GPU raymarching with transfer functions

### Animation
- **SmoothAnimationSystem** - 30+ easing functions for smooth transitions
- **UITransitionEffects** - Professional UI animations

### UI Themes
- Dark, Light, Medical, Surgery, Cardiology theme presets
- Automatic theme synchronization across all UI elements

---

## Architecture

```
CardiacVR/
├── Core/
│   └── CardiacVRIntegrationManager    # Central system coordinator
├── Rendering/
│   ├── PostProcessingManager          # Visual effects pipeline
│   ├── SegmentedMeshRenderer          # Mesh visualization
│   ├── VisualEffectsSystem            # Interactive effects
│   ├── MeshEnhancementSystem          # Mesh smoothing
│   ├── EnhancedMaterialsLibrary       # 50+ anatomical presets
│   ├── ProfessionalLightingSetup      # Lighting configurations
│   └── AmbientEnvironmentSystem       # Environment presets
├── Animation/
│   ├── SmoothAnimationSystem          # Animation engine
│   └── UITransitionEffects            # UI transitions
├── UI/
│   └── ProfessionalUITheme            # Theme management
├── SurgicalPlanning/
│   ├── SurgicalPlanningSystem         # Device placement
│   ├── HeartFlyThroughController      # Chamber navigation
│   └── BaffleDesigner                 # Baffle design tool
├── Surgery/
│   ├── SurgerySimulator               # Simulation coordinator
│   └── Tools/                         # Surgical instruments
├── Collaboration/
│   ├── MultiplayerCollaboration       # Multi-user sessions
│   └── VoiceAnnotationSystem          # Voice annotations
├── Education/
│   └── PatientEducationMode           # Educational features
├── VolumeRendering/
│   ├── VolumeRenderer                 # Volume visualization
│   ├── VolumeRenderingController      # Rendering controls
│   └── DicomVolumeLoader              # DICOM loading
├── AR/
│   ├── MixedRealityManager            # VR/AR switching
│   └── ARModelPlacer                  # Spatial placement
├── VirtualRoom/
│   └── VirtualRoomManager             # Environment management
└── STLImport/
    ├── STLImporter                    # STL parsing
    └── STLLoader                      # Runtime loading
```

---

## Visualization Modes

The platform supports multiple visualization modes, each optimizing the visual experience for specific use cases:

| Mode | Theme | Lighting | Environment |
|------|-------|----------|-------------|
| **Standard** | Dark | Education | Medical Studio |
| **Surgical** | Surgery | Surgical | Operating Room |
| **Education** | Medical | Education | Educational |
| **Presentation** | Dark | Presentation | Presentation Hall |
| **VR** | Dark | VR Comfort | VR Comfort |
| **Review** | Cardiology | Cinematic | Cinematic |

---

## Quality Presets

Adaptive quality system with automatic frame rate monitoring:

| Preset | Target Use | Features |
|--------|------------|----------|
| **Mobile** | Standalone VR | Optimized for Quest |
| **Balanced** | General use | Good quality/performance |
| **High** | Desktop VR | High fidelity |
| **Ultra** | Presentations | Maximum quality |

---

## Installation

### Prerequisites
- **Unity 2022.3.49f1** (LTS)
- **Universal Render Pipeline (URP)**
- **XR Interaction Toolkit 2.5.x**
- **TextMeshPro**

### Setup
1. Clone the repository
   ```bash
   git clone https://github.com/sergiolaranjo/Guilherme-Francisco-thesis-project.git
   ```

2. Open the project in Unity 2022.3.49f1

3. Import required packages if prompted:
   - XR Plugin Management
   - XR Interaction Toolkit
   - TextMeshPro

4. Configure XR settings for your target platform (Meta Quest, etc.)

5. Open the main scene and press Play

### Loading Your Own Models

**For STL Models:**
1. Use `STLLoader.Instance.LoadFromFile(path)` to load at runtime
2. Or place STL files in the Resources folder

**For DICOM Data:**
1. Use `DicomVolumeLoader` to load DICOM series
2. Assign the generated 3D texture to `VolumeRenderer`

**For Segmented Meshes:**
1. Import your mesh into Unity
2. Use `SegmentedMeshRenderer.RegisterMesh()` to apply professional rendering

---

## Usage

### Basic Controls
- **Grip** - Grab and manipulate objects
- **Trigger** - Select/interact
- **Thumbstick** - Navigate menus
- **Menu Button** - Open tools panel

### Changing Visualization Mode
```csharp
CardiacVRIntegrationManager.Instance.SetVisualizationMode(VisualizationMode.Surgical);
```

### Applying Quality Preset
```csharp
CardiacVRIntegrationManager.Instance.SetQualityPreset(QualityPreset.High);
```

---

## Technology Stack

- **Engine:** Unity 2022.3 LTS
- **Rendering:** Universal Render Pipeline (URP)
- **VR SDK:** OpenXR / Meta XR SDK
- **Interaction:** XR Interaction Toolkit
- **DICOM:** fo-dicom library
- **UI:** TextMeshPro

---

## Roadmap

- [x] Volume Rendering with DICOM support
- [x] STL Model Import
- [x] Surgical Planning Tools
- [x] Surgery Simulation
- [x] Mixed Reality (VR/AR)
- [x] Professional Visual System
- [x] UI Theme System
- [x] Integration Manager
- [ ] Multiplayer Networking (Backend)
- [ ] Hand Tracking Support
- [ ] AI-Assisted Surgical Planning
- [ ] Cloud Case Repository

---

## References

Inspired by research and systems from:
- Cincinnati Children's Hospital VR3S (VR Surgical Simulation Suite)
- "Hands-Free VR for Surgical Planning" - CCHMC
- Advances in 3D printing and VR for congenital heart disease

---

## License

**Copyright (c) 2024 Sérgio Laranjo**
**Computational Cardiology, AI and Data Science for Health Lab**
**Nova Medical School, Universidade Nova de Lisboa**
**All rights reserved.**

This software is proprietary. Unauthorized copying, modification, distribution, or use of this software, via any medium, is strictly prohibited without express written permission.

---

## Contact

For inquiries about this project, please contact:

**Computational Cardiology, AI and Data Science for Health Lab**
Nova Medical School, Universidade Nova de Lisboa

# Sky Parkour 🌋

A fast-paced, atmospheric first-person 3D parkour game built in **Unity 6** utilizing the **Universal Render Pipeline (URP)**. Navigate treacherous basalt spires, leap across collapsing volcanic rock platforms, evade moving hazards above a searing molten lava chasm, and race against the clock to reach the summit finish line!

---

## 🎮 Gameplay & Features

- **Fluid First-Person Parkour Mechanics**:
  - Momentum-preserving movement, sprinting, and smooth mouse-look.
  - Generous coyote time and jump-buffering for responsive platforming.
  - Mid-air steering and dynamic velocity dampening.
- **Volcanic Lava Canyon Obstacle Course**:
  - Procedurally structured fractured rock slabs, floating magma boulders, and towering basalt columns.
  - Searing kill-plane floor with instant respawn at the latest activated checkpoint.
  - Moving hazard platforms and swinging obstacles powered by smooth trigonometric oscillation.
- **Race HUD & Speedrun Timer**:
  - Live timer tracking seconds and milliseconds.
  - Checkpoint progress tracker with sound effects and material glow activation.
  - Victory summary screen displaying final time and personal bests.
  - Quick-restart shortcut (`R` key) for rapid speedrun attempts.
- **Procedural Level Generator Tool**:
  - Includes a custom Unity Editor builder (`Tools > Sky Parkour > Build Volcanic Lava Canyon Course`) that instantly generates the complete course, lighting, materials, and triggers directly into the scene.

---

## 🕹️ Controls

| Action | Keyboard & Mouse | Gamepad |
| :--- | :--- | :--- |
| **Move** | `W` / `A` / `S` / `D` | Left Analog Stick |
| **Look** | Mouse Movement | Right Analog Stick |
| **Jump** | `Spacebar` | South Button (`A` / `Cross`) |
| **Sprint** | `Left Shift` (Hold) | Left Stick Click (`L3`) |
| **Quick Restart** | `R` | Select / Back |

---

## 📂 Project Architecture

```
Assets/_Project/
├── Audio/            # Horror.mp3 (looping horror BGM) & myinstants.mp3 (death sound FX)
├── Materials/        # Basalt, magma rock, molten lava, storm clouds, skybox, embers & smoke
├── Scripts/
│   ├── BackgroundMusic.cs  # Atmospheric looping horror BGM controller
│   ├── Checkpoint.cs       # Trigger collision, material state swap, audio chime, UI updates
│   ├── FinishLine.cs       # End-of-course detection and victory screen trigger
│   ├── GameUI.cs           # HUD canvas controller (live timer, checkpoint tracker, 60 FPS lock)
│   ├── HazardTrigger.cs    # Kill zone and moving obstacle damage triggers
│   ├── LavaFlow.cs         # Smooth texture UV scrolling for viscous lava & storm clouds
│   ├── Oscillator.cs       # Trigonometric harmonic motion for moving platforms
│   ├── PlayerMovement.cs   # CharacterController parkour physics, coyote time, and air control
│   ├── PlayerRespawn.cs    # Death sound trigger (myinstants.mp3) and checkpoint teleportation
│   └── Editor/
│       └── SkyParkourBuilder.cs  # Procedural level generator and scene assembler
├── Settings/
│   └── Volcanic_VolumeProfile.asset  # URP post-processing (ACES tonemapping, bloom, vignette)
└── Textures/         # PBR surface textures (molten lava, basalt, magma, moss)
```

---

## ⚡ Laptop & Mobile GPU Optimization

This project is pre-configured to run with a rock-solid, smooth 60 FPS on laptops (both integrated and dedicated GPUs) while keeping thermals and battery usage cool:
- **Frame Rate Capping & VSync**: Automatically enabled in `GameUI.Awake()` and `QualitySettings` to eliminate screen tearing, microstuttering, and GPU fan throttling.
- **Tuned Universal Render Pipeline**: Shadow maps are balanced with optimized filtering (`SoftShadowQuality: Medium`, 1024 shadow atlas) to maximize fill-rate efficiency on mobile GPUs.
- **Static Geometry & Material Sharing**: Course spires, canyon bluffs, and platforms are flagged as static with shared materials for maximum SRP batching throughput.
- **Budgeted Particles**: Rising embers and smoke particles are strictly capped with local bounds to prevent overdraw.

---

## 🚀 Getting Started

### Prerequisites
- **Unity 6 (6000.0+)** or compatible Unity LTS version with **Universal Render Pipeline (URP)** support.

### Setup Instructions
1. Clone this repository:
   ```bash
   git clone https://github.com/Daud140/Sky-Parkour.git
   ```
2. Open **Unity Hub**.
3. Click **Add** > **Add project from disk**.
4. Select the cloned `Sky-Parkour` folder.
5. In the Unity Editor, open `Assets/Scenes/SampleScene.unity`.
6. *(Optional)* To regenerate or customize the course, click **Tools > Sky Parkour > Build Volcanic Lava Canyon Course** from the top menu.
7. Click **Play** (▶) and start running!

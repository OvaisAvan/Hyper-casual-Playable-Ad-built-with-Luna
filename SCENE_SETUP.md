# Scene Setup Guide

Complete Unity scene hierarchy and Inspector wiring for TapBlitz Luna playable ad.

---

## Requirements

- Unity 2022.3 LTS or newer
- TextMeshPro (Window → TextMeshPro → Import TMP Essential Resources)
- Luna SDK optional (see LUNA_SETUP.md — works without it)

---

## Scene: `TapBlitz` (single scene)

### Hierarchy

```
TapBlitz (Scene)
├── _Managers
│   ├── GameLoop            → GameLoop.cs
│   ├── AudioManager        → AudioManager.cs  (+ AudioSource)
│   ├── EffectsManager      → EffectsManager.cs
│   ├── PoolManager         → PoolManager.cs
│   ├── WebGLBridge         → WebGLBridge.cs
│   └── CameraShake         → CameraShake.cs  (on Main Camera, or own GO)
├── _Luna
│   ├── LunaAdController    → LunaAdController.cs    ← NAME MUST MATCH EXACTLY
│   ├── LunaCtaHandler      → LunaCtaHandler.cs      ← NAME MUST MATCH EXACTLY
│   └── LunaAnalytics       → LunaAnalytics.cs
├── _Game
│   ├── TapController       → TapController.cs
│   ├── ComboSystem         → ComboSystem.cs
│   ├── ScoreManager        → ScoreManager.cs
│   ├── TargetSpawner       → TargetSpawner.cs
│   └── TargetParent        (empty Transform — parent for spawned targets)
├── Main Camera             (Orthographic, CameraShake.cs)
└── UI (Canvas — Screen Space Overlay)
    ├── ScaleMode: Scale with Screen Size
    ├── Reference Resolution: 1080 × 1920
    │
    ├── HUD                 → AdHUD.cs
    │   ├── ScoreText       (TMP_Text, top-left)
    │   ├── ScoreDeltaText  (TMP_Text, animated, starts alpha=0)
    │   ├── TimerBar        (Image, type=Filled, Horizontal)
    │   ├── TimerLabel      (TMP_Text, inside TimerBar)
    │   ├── ComboPanel      (GameObject, starts inactive)
    │   │   ├── ComboCountText   (TMP_Text, "×12")
    │   │   ├── ComboMultText    (TMP_Text, "×2.0")
    │   │   └── ComboLabelText   (TMP_Text, "GREAT!", alpha=0)
    │   └── ComboBreakText  (TMP_Text, alpha=0)
    │
    ├── CountdownPanel      (starts inactive)
    │   └── CountdownText   (TMP_Text, large centred number)
    │
    ├── CTAPanel            → CTAOverlay.cs (starts inactive)
    │   ├── Background      (Image — semi-opaque dark overlay)
    │   ├── FinalScoreText  (TMP_Text)
    │   ├── BestComboText   (TMP_Text)
    │   ├── TaglineText     (TMP_Text)
    │   ├── Stars           (3× Image, star sprites)
    │   └── InstallButton   (Button → CTAOverlay.OnInstallTapped)
    │       └── ButtonLabel (TMP_Text, "INSTALL FREE")
    │
    ├── TutorialFinger      → TutorialFinger.cs
    │   ├── CanvasGroup     (starts alpha=0)
    │   ├── FingerImage     (Image — finger/hand sprite)
    │   └── HintLabel       (TMP_Text, "Tap the targets!")
    │
    ├── ComboPopupContainer (empty RectTransform, parent for combo popups)
    └── FXContainer         (empty RectTransform, parent for score popups)
```

---

## Prefabs Needed

### Target Prefabs (×3)

Create in `Assets/Prefabs/Targets/`:

**NormalTarget.prefab**
- `SpriteRenderer` (circle sprite, color #66CCFF)
- `CircleCollider2D` (Layer: **Tappable**)
- `TargetController.cs` (type: Normal)
- Scale: 0.9 × 0.9

**BonusTarget.prefab**
- Same structure, color #FFD700
- `TargetController.cs` (type: Bonus)
- Scale: 1.1 × 1.1

**BombTarget.prefab**
- Same structure, color #FF4444
- `TargetController.cs` (type: Bomb)
- Scale: 1.0 × 1.0

Assign all 3 to `TargetSpawner → Target Prefabs[]`.

### Tap Ripple Prefab

`Assets/Prefabs/FX/TapRipple.prefab`
- `SpriteRenderer` (ring/circle sprite, white)
- Scale animation: 0→1.5, alpha 1→0 over 0.3s
- Add `PoolManager` key to `PoolManager → Prewarmed Pools`

### Hit Burst Prefab

`Assets/Prefabs/FX/HitBurst.prefab`
- `ParticleSystem`
  - Duration: 0.35s, Burst: 8 particles at t=0
  - Start Size: 0.05–0.15, Start Speed: 3–6
  - Color over Lifetime: white → transparent
  - Shape: Sphere, Radius 0.05

### Combo Popup Prefab

`Assets/Prefabs/UI/ComboPopup.prefab`
- `RectTransform` (200 × 60)
- `CanvasGroup`
- `ComboPopup.cs`
- Children: MainLabel (TMP bold 32pt), MultLabel (TMP 22pt)

---

## Physics Layer Setup

1. Edit → Project Settings → Tags and Layers
2. Add layer: **Tappable** (e.g. Layer 8)
3. Assign `CircleCollider2D` on target prefabs to **Tappable**
4. Set `TapController → Tappable Layer` to **Tappable** layer mask

---

## Pool Manager Setup

In the Inspector on `PoolManager`:

| Prefab | Initial Size |
|--------|-------------|
| NormalTarget | 8 |
| BonusTarget  | 4 |
| BombTarget   | 3 |
| TapRipple    | 10 |

---

## Build Settings

**File → Build Settings:**
- Platform: WebGL
- Scene: `Assets/Scenes/TapBlitz.unity`

**Player Settings → WebGL:**
- Template: LunaPreview
- Compression: Gzip
- Exception Support: None
- Strip Engine Code: ON

Run **Window → TapBlitz → Validate Luna Project** to verify everything is correct before building.

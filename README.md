# 🌙 TapBlitz — Luna Playable Ad (Unity)

> A production-ready, open-source **hyper-casual playable ad** built in Unity with full **Unity Luna** integration. Tap targets, build combos, hit the CTA.

![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-black?logo=unity)
![Luna](https://img.shields.io/badge/Unity%20Luna-Integrated-purple)
![Platform](https://img.shields.io/badge/platform-WebGL%20%7C%20HTML5-orange)
![License](https://img.shields.io/badge/license-MIT-green)

---

## 🎯 What is this?

A complete Unity project for building a **Luna-compatible playable ad** featuring a hyper-casual tap/clicker game. Players tap appearing targets, build combos, and earn a star rating — then hit "INSTALL FREE."

```
┌──────────────────────────┐
│  Score: 140  ████░░ 10s  │  ← HUD
│                          │
│     ●    ★    ●          │
│  ●     ●    ●    ★       │  ← Targets (tap them!)
│     ●    ●    ●          │
│  ●     ●       ●         │
│                          │
│  COMBO ×8  ×2.0  GREAT!  │  ← Combo bar
└──────────────────────────┘
         ↓ after 15s
┌──────────────────────────┐
│     Score: 320           │
│     Best Combo: ×18      │
│     ⭐ ⭐ ⭐             │
│                          │
│   [ INSTALL FREE 🎮 ]    │  ← Luna CTA
└──────────────────────────┘
```

**Luna lifecycle:**  
`adReady` → `GameStart` (on first tap) → gameplay → `GameEnd` (score payload) → `ShowEndCard`

---

## 🗂️ Project Structure

```
TapBlitzLuna/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── TapController.cs       ← Input (mouse + touch), hit/miss routing
│   │   │   ├── TargetSpawner.cs       ← Wave-based target spawning, difficulty ramp
│   │   │   ├── TargetController.cs    ← Per-target (Normal / Bonus / Bomb) behaviour
│   │   │   ├── ScoreManager.cs        ← Score tracking, combo multiplier application
│   │   │   ├── ComboSystem.cs         ← Combo tiers (×1.5 / ×2 / ×3 / ×5), break logic
│   │   │   └── GameLoop.cs            ← Phase machine: start → play → CTA trigger
│   │   ├── Ad/
│   │   │   ├── LunaAdController.cs    ← Luna SDK bridge (GameStart, GameEnd, ShowEndCard)
│   │   │   ├── LunaCtaHandler.cs      ← Install button, store URL, Luna end card
│   │   │   ├── LunaAnalytics.cs       ← Luna analytics event tracker
│   │   │   └── LunaBridgeJS.cs        ← C# wrapper for LunaBridge.jslib DllImport calls
│   │   ├── UI/
│   │   │   ├── AdHUD.cs               ← Score, timer bar, combo display, countdown
│   │   │   ├── CTAOverlay.cs          ← End-of-game overlay: stars, score, install btn
│   │   │   ├── TutorialFinger.cs      ← Animated tap hint, auto-hides after 3s
│   │   │   ├── ComboPopup.cs          ← Floating "NICE! ×1.5" label (pooled)
│   │   │   └── CountdownTimer.cs      ← 3-2-1-GO countdown visual component
│   │   ├── Managers/
│   │   │   ├── AudioManager.cs        ← SFX + music, WebGL gesture unlock
│   │   │   ├── EffectsManager.cs      ← Particle pool (hit burst), combo popup pool
│   │   │   ├── PoolManager.cs         ← Generic GameObject pool (targets + FX)
│   │   │   ├── CameraShake.cs         ← Perlin-noise camera shake on combo tiers
│   │   │   └── WebGLBridge.cs         ← WebGL perf settings + SendMessage receiver
│   │   └── Editor/
│   │       ├── LunaAdBuilder.cs       ← One-click build window (Window → TapBlitz)
│   │       ├── LunaSizeValidator.cs   ← Post-build size check (warns >2MB, errors >5MB)
│   │       └── LunaProjectValidator.cs ← Pre-build settings validation
│   ├── Plugins/WebGL/
│   │   └── LunaBridge.jslib           ← JS plugin: Luna runtime + MRAID + postMessage
│   ├── Resources/
│   │   ├── wave_config.json           ← 4-wave difficulty config (timings, weights)
│   │   └── ad_config.json             ← Runtime ad config (CTA copy, thresholds)
│   └── WebGLTemplates/LunaPreview/
│       ├── index.html                 ← Custom WebGL template with phone-frame UI
│       └── luna_stub.js               ← Luna JS runtime stub for local preview
├── BuildConfig/
│   ├── build_luna.py                  ← Python packager → single HTML + ZIP
│   └── luna_config.json               ← Build config (store URLs, size limits)
├── LUNA_SETUP.md                      ← Luna SDK installation + configuration guide
├── SCENE_SETUP.md                     ← Unity scene hierarchy + Inspector wiring
├── SUBMISSION_GUIDE.md                ← Luna dashboard upload + A/B testing guide
├── .gitignore
├── LICENSE
└── README.md
```

---

## 🚀 Getting Started

### Requirements
- **Unity 2022.3 LTS** or newer
- **TextMeshPro** (Window → TextMeshPro → Import TMP Essential Resources)
- **Python 3.8+** for the build packager
- **Luna SDK** (optional — project works without it via JS bridge fallback)

### 1. Clone
```bash
git clone https://github.com/YOUR_USERNAME/TapBlitzLuna.git
```

### 2. Open in Unity Hub
Open Unity Hub → **Open** → select `TapBlitzLuna/`

### 3. Set Up Scene
Follow **[SCENE_SETUP.md](SCENE_SETUP.md)** for the full hierarchy and prefab setup.

### 4. (Optional) Install Luna SDK
Follow **[LUNA_SETUP.md](LUNA_SETUP.md)** to install `com.unity.luna`.  
The project works without it — the `LunaBridgeJS.cs` fallback handles everything.

### 5. Validate
```
Window → TapBlitz → Validate Luna Project
```

### 6. Hit Play
The game runs in the editor. Targets spawn, combos build, CTA shows after 15 seconds.

### 7. Build for Luna
```
Window → TapBlitz → Luna Ad Builder → ▶ Build Luna Playable Ad
```
Or manually:
```bash
python3 BuildConfig/build_luna.py Builds/WebGL Builds/Luna \
  --title "TapBlitz" \
  --android "https://play.google.com/store/apps/details?id=com.yourstudio.tapblitz" \
  --ios "https://apps.apple.com/app/id000000"
```

---

## 🌙 Luna Integration Details

| Luna API | Where called | Trigger |
|----------|-------------|---------|
| `LifeCycle.GameStart()` | `LunaAdController.NotifyGameStarted()` | First tap |
| `LifeCycle.GameEnd(data)` | `LunaAdController.NotifyGameEnded()` | CTA shown |
| `Playable.ShowEndCard()` | `LunaCtaHandler.OnInstallButtonTapped()` | Install tapped |
| `Analytics.TrackEvent()` | `LunaAnalytics.Track*()` | Throughout |
| `SendMessage → SetStoreUrl` | `LunaCtaHandler.SetStoreUrl()` | Ad load |
| `SendMessage → OnMuteAudio` | `WebGLBridge.OnMuteAudio()` | Network request |

**Without Luna SDK:** All calls fall back to `LunaBridgeJS.jslib` → JS bridge → `postMessage` to parent frame (compatible with any MRAID 2.0 network).

**With Luna SDK:** Add `LUNA_ENABLED` scripting define → calls go directly to `Luna.Unity.*` APIs.

---

## 🎮 Gameplay Design

| Element | Detail |
|---------|--------|
| Duration | 15 seconds (configurable) |
| Targets | Normal (×1pts), Bonus (×3pts, gold), Bomb (penalty, red) |
| Combo Tiers | ×5 hits = ×1.5, ×10 = ×2.0, ×15 = ×3.0, ×20 = ×5.0 |
| Difficulty | Spawn interval ramps 1.2s → 0.35s over 12 seconds |
| Early CTA | Triggered if score ≥ 150 or combo ≥ 15 (tunable) |
| Stars | 1★ (50pts), 2★ (100pts), 3★ (180pts) |

---

## ⚙️ Configuration

All tunable values live in JSON — **no code changes required**:

| File | What to change |
|------|---------------|
| `BuildConfig/luna_config.json` | Store URLs, build limits, title |
| `Assets/Resources/ad_config.json` | CTA copy, timing, tutorial toggle |
| `Assets/Resources/wave_config.json` | Spawn rates, target weights per wave |

---

## 🤝 Contributing

PRs welcome!

- New target types (slow, split, multiplier)
- Landscape mode support
- Additional Luna analytics events
- Background / theme variants for A/B testing
- Automated build pipeline (GitHub Actions)

---

## 📄 License

MIT © 2025 — free to use in commercial and personal projects.

---

## 🔗 Resources

- [Unity Luna](https://unity.com/products/luna)
- [Luna Dashboard](https://luna.unity.com)
- [Luna Developer Docs](https://developer.unity.com/products/luna)
- [ironSource Luna (archived)](https://developers.is.com/ironsource-mobile/unity/playable-ads/)

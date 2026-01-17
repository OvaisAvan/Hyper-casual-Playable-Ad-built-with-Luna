# Luna SDK Setup Guide

Complete instructions for integrating Unity Luna into TapBlitz.

---

## What is Luna?

**Unity Luna** (formerly ironSource Luna, acquired by Unity 2022) is Unity's playable ad creation platform. It provides:

- A Unity package (`com.unity.luna`) that wraps the Luna JS runtime
- A dashboard at [luna.unity.com](https://luna.unity.com) for uploading and A/B testing creatives
- Analytics for tracking player engagement (taps, combo, CTA tap rate)
- Automatic end card generation

---

## Step 1 — Get Luna Access

1. Go to [luna.unity.com](https://luna.unity.com)
2. Sign in with your Unity ID (same as Unity Hub)
3. Create an app entry for TapBlitz
4. Note your **App ID** and **API Key** from the dashboard

---

## Step 2 — Install the Luna Unity Package

### Option A — Package Manager (recommended)
```
Window → Package Manager → + → Add package by name
Name: com.unity.luna
```

### Option B — Git URL
```
Window → Package Manager → + → Add package from git URL
https://github.com/Unity-Technologies/luna-playable-ads.git
```

### Option C — Without Luna SDK (fallback mode)
This project works **without the Luna SDK** using the `LunaBridgeJS.cs` + `LunaBridge.jslib` fallback layer. You can build and test without installing the package. Add `LUNA_ENABLED` to your scripting defines only after installing the SDK.

---

## Step 3 — Enable LUNA_ENABLED Define (after SDK install)

Once the Luna package is installed:

```
Edit → Project Settings → Player → Other Settings → Scripting Define Symbols
Add: LUNA_ENABLED
```

This switches all `LunaAdController.cs` calls from the JS bridge to the native Luna SDK.

---

## Step 4 — Configure Luna in Unity

After installing the package:

1. **Window → Luna → Settings**
2. Enter your App ID and API Key
3. Set the **Platform** (iOS / Android / Both)
4. Set your **Store URLs** (or leave blank — Luna injects them at runtime)

---

## Step 5 — Scene Setup

See **[SCENE_SETUP.md](SCENE_SETUP.md)** for the full hierarchy.

Key GameObjects that must be present and named exactly:

| GameObject Name   | Required Component    | Why |
|-------------------|-----------------------|-----|
| `LunaAdController` | `LunaAdController.cs` | Luna SendMessage target |
| `LunaCtaHandler`   | `LunaCtaHandler.cs`   | Store URL injection |
| `WebGLBridge`      | `WebGLBridge.cs`      | Mute / pause / URL messages |

> **Critical:** Luna uses `SendMessage` by GameObject name. These names must match exactly.

---

## Step 6 — WebGL Template

1. Copy `Assets/WebGLTemplates/LunaPreview/` — this is already in the project
2. **Edit → Project Settings → Player → WebGL → Resolution and Presentation**
3. Set **WebGL Template** to `LunaPreview`

The template includes `luna_stub.js` for local preview. The stub is NOT included in the final Luna submission HTML (the packager removes it).

---

## Step 7 — Player Settings for Luna

| Setting | Value | Location |
|---------|-------|----------|
| Compression Format | Gzip | Player → WebGL → Publishing |
| Exception Support | None | Player → WebGL → Publishing |
| Strip Engine Code | ✅ ON | Player → Other Settings |
| Development Build | ❌ OFF | Build Settings (for final) |
| WebGL Template | LunaPreview | Player → WebGL → Resolution |

Run **Window → TapBlitz → Validate Luna Project** to check all settings automatically.

---

## Step 8 — Build for Luna

### Option A — Editor Window (easiest)
```
Window → TapBlitz → Luna Ad Builder → ▶ Build Luna Playable Ad
```

### Option B — Manual
```bash
# 1. File → Build Settings → Build → Builds/WebGL/
# 2. Package for Luna:
python3 BuildConfig/build_luna.py Builds/WebGL Builds/Luna \
  --title "TapBlitz" \
  --ios "https://apps.apple.com/app/idYOURID" \
  --android "https://play.google.com/store/apps/details?id=com.yourstudio.tapblitz"
```

Output:
```
Builds/Luna/
├── TapBlitz_Luna.html           ← single-file submission
├── TapBlitz_Luna_package.zip    ← ZIP submission (fallback)
└── luna_build_info.json         ← size + metadata
```

---

## Step 9 — Upload to Luna Dashboard

1. Go to [luna.unity.com](https://luna.unity.com)
2. Select your app → **Creatives** → **Upload**
3. Upload `TapBlitz_Luna.html` (single file) or `TapBlitz_Luna_package.zip`
4. Luna validates size and compatibility
5. Set up A/B test variants if desired
6. **Publish** to your connected ad networks

---

## Luna Analytics Events Tracked

| Event | When |
|-------|------|
| `game_start` | First tap |
| `tap` | Every tap (hit or miss) |
| `combo_tier` | New combo tier reached |
| `cta_shown` | CTA overlay displayed |
| `cta_tapped` | Install button tapped |
| `game_end` | Session ended (score + combo payload) |

View in Luna Dashboard → **Analytics → Events**.

---

## Troubleshooting

**"Luna runtime not found" in console**
→ Normal in editor. Use the LunaPreview template for browser preview. Luna runtime is only present inside the Luna ad network.

**Build size too large**
→ Run `Window → TapBlitz → Validate Luna Project` and follow the texture compression tips.

**CTA button doesn't open store**
→ Check that `LunaCtaHandler` GameObject name matches exactly. Verify `SetStoreUrl` is being called (check console).

**Audio doesn't play on mobile**
→ Expected — WebGL requires a user gesture before audio. `AudioManager.cs` handles this automatically on first tap.

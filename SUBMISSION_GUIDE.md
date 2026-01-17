# Luna Submission & A/B Testing Guide

How to upload TapBlitz to the Unity Luna dashboard and run creative A/B tests.

---

## Pre-submission Checklist

- [ ] Build passes all validations (`Window → TapBlitz → Validate Luna Project`)
- [ ] Single HTML file ≤ 5 MB (aim ≤ 2 MB)
- [ ] CTA button tested — opens correct store URL
- [ ] Tested on Chrome (desktop), Safari (iOS), Chrome (Android)
- [ ] Audio works after first tap
- [ ] No external network requests at runtime
- [ ] Tutorial finger appears and auto-hides after 3 seconds
- [ ] Countdown 3-2-1-GO plays before gameplay
- [ ] CTA overlay appears at 15 seconds OR on high score/combo

---

## Upload to Luna Dashboard

1. Go to [luna.unity.com](https://luna.unity.com)
2. Select your app (or create one)
3. Navigate to **Creatives → Playable Ads → New Creative**
4. Upload `Builds/Luna/TapBlitz_Luna.html`
5. Luna validates:
   - File size
   - CTA button presence
   - `Luna.Unity.LifeCycle.GameStart()` call
   - `Luna.Unity.Playable.ShowEndCard()` call
6. Preview in Luna's device simulator
7. Save and name the creative

---

## A/B Testing with Luna

Luna makes it easy to test creative variants. Recommended tests:

### Test 1 — CTA Timing
| Variant | Play Duration |
|---------|--------------|
| A       | 10 seconds   |
| B       | 15 seconds   |
| C       | 20 seconds   |

Change `AdSettings.playDurationSeconds` in `luna_config.json` per variant.

### Test 2 — Tutorial Presence
| Variant | Tutorial Finger |
|---------|----------------|
| A       | Shown (3s)     |
| B       | Hidden         |

Toggle `tutorial.showFinger` in `ad_config.json`.

### Test 3 — Difficulty Curve
| Variant | Initial Spawn Interval |
|---------|----------------------|
| A (Easy)   | 1.4s              |
| B (Normal) | 1.2s (default)    |
| C (Hard)   | 0.9s              |

Edit `gameplay.baseSpawnIntervalSec` in `luna_config.json`.

### Test 4 — CTA Copy
| Variant | Button Text |
|---------|------------|
| A | "INSTALL FREE" |
| B | "PLAY NOW — FREE" |
| C | "GET THE FULL GAME" |

Edit `ad.ctaButtonText` in `ad_config.json`.

---

## Key Metrics to Track (Luna Dashboard)

| Metric | What it means | Target |
|--------|---------------|--------|
| **IPM** (Installs per Mille) | Installs per 1000 impressions | > 30 is strong |
| **CTR** (CTA tap rate) | % who tap install | > 8% |
| **Engagement Rate** | % who interact at all | > 60% |
| **Avg Session Duration** | How long they play | > 8s (of 15) |
| **First Tap Time** | How fast they engage | < 3s |

---

## Distributing to Ad Networks via Luna

Once approved in Luna dashboard:

1. **Luna → Distribution → Connected Networks**
2. Connect your ironSource / Unity Ads / Meta / AppLovin accounts
3. Set budget and targeting per network
4. Luna handles the creative delivery automatically

---

## Build Variants Script

To quickly build multiple A/B variants:

```bash
# Variant A — 10s, easy
python3 BuildConfig/build_luna.py Builds/WebGL Builds/Luna/VariantA \
  --title "TapBlitz_A"

# Variant B — 15s, normal (default)
python3 BuildConfig/build_luna.py Builds/WebGL Builds/Luna/VariantB \
  --title "TapBlitz_B"
```

Upload each variant separately to Luna and assign to the same A/B test group.

---

## File Size Reduction Cheatsheet

If your build exceeds 2 MB:

| Action | Typical Saving |
|--------|---------------|
| Enable Crunch compression on all Sprite assets | 25–50% |
| Use ASTC texture format (mobile) | 30–60% |
| Reduce audio quality (22kHz, Vorbis) | 40% on audio |
| Strip Engine Code ON | 5–15% |
| Exception Support → None | 5–10% |
| Remove unused packages | 5–20% |
| Use SpritePacking / Atlas | Reduces overhead |
| Disable Development Build | ~200 KB |

Run `LunaSizeValidator` post-build — it lists the largest files so you know where to optimise.

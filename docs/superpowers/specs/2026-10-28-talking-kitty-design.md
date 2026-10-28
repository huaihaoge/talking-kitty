# Talking Kitty — Design Spec

**Date:** 2026-10-28  
**Repo:** https://github.com/huaihaoge/talking-kitty  
**Platform:** Android APK (sideload; no store required for MVP)  
**Engine:** Unity (LTS), chosen to allow a later 3D character without rewriting voice/lock logic

## Problem

Commercial “Talking Tom” style apps are full of ads. A ~1-year-old child mis-taps into ads and leaves the play experience. Parents need a **local, ad-free** app that only does voice echo (learn-to-talk play) plus a **child lock** so random taps do not exit into other actions.

## Goals (MVP)

1. **Classic voice echo:** record the child’s speech and play it back with a higher pitch (Talking Tom style), not STT→TTS.
2. **Auto-listen:** on launch, listen continuously; after a pause, pitch-shift and replay. No hold-to-talk required.
3. **Simple cat presenter:** 2D sprite or low-poly stand-in; animation API ready to swap for 3D later.
4. **In-app child lock:** block Back, hide exit/settings; parent PIN (or gesture) to unlock.
5. **No ads, no analytics, no network** in MVP.
6. **Ship** Unity project + installable APK on GitHub under `huaihaoge/talking-kitty`.

## Non-Goals (MVP)

- App store listing, IAP, accounts
- System-wide kiosk / device owner / MDM
- Cloud speech, AI chat, online content
- Complex mini-games or large content packs
- Official Talking Tom IP / assets (must use original or clearly licensed art)

## Decisions Locked

| Topic | Choice |
|-------|--------|
| Echo style | A — record + pitch-shifted playback |
| Child lock | A — in-app lock (PIN / long-press); not system kiosk |
| Visuals | C — simple 2D/low-poly first; swap later for 3D |
| Listen UX | A — auto VAD / silence-end |
| Stack | Unity (path to 3D) |
| Repo name | `talking-kitty` |
| GitHub owner | `huaihaoge` |

## Architecture

```text
Microphone
  → VoiceActivityDetector (energy + silence)
  → AudioClipBuffer
  → PitchShifter
  → AudioPlayback
  → CatPresenter (Idle / Listening / Speaking)

ChildLockController ← ParentGate (PIN)
  → UI + Back-key handling

MainScene hosts CatPresenter + ChildLock UI
```

### Modules

- **`Voice/`** — mic capture, silence detection, pitch shift, playback. Independent of visuals.
- **`Cat/`** — `CatPresenter` interface + MVP 2D/low-poly implementation. Future 3D replaces only this layer.
- **`ChildLock/`** — lock state, PIN storage, parent screen, back-key swallow.
- **`Scenes/Main`** — single play scene for MVP.

### Data / privacy

- Microphone permission only.
- Audio kept in memory or short-lived temp files; discarded after playback (or next utterance).
- No network permission in MVP Android manifest if feasible.
- PIN stored on-device (hashed); not committed to git. Factory reset / clear app data recovers forgotten PIN (documented in README).

## Voice loop (detail)

1. Request mic on first run; after grant, enter Listening.
2. When level exceeds threshold, begin capturing; notify Cat → Listening reaction.
3. After **0.8–1.2 s** of continuous silence → end utterance.
4. Ignore clips shorter than **~0.3 s**; cap capture at **~5–8 s**.
5. Playback at pitch scale **~1.3–1.6** (parent-tunable later on parent screen).
6. Cat → Speaking during playback; when done, return to Listening immediately.

Optional MVP touch: tap cat → short reaction (wiggle / meow). Primary path remains auto-listen.

## Child lock (detail)

**On:** Recommended at first launch; toggle on parent screen.

**While locked:**

- Hardware/gesture Back does not leave the app.
- No visible Exit / Settings / Share.
- Immersive fullscreen where practical.

**Parent unlock (both require PIN):**

1. Long-press any screen corner **~3 s**.
2. Or tap top-center **5 times**.

**PIN:** 4 digits, set on first parent entry. Parent screen: disable lock, change PIN, pitch slider, Quit app.

**Known limit:** System Home / Recents may still leave the app. README may mention Android Screen Pinning as an optional extra layer (not implemented in-app).

## UI (MVP)

- One fullscreen scene: large cat, minimal status (Listening / Speaking via color or motion; little/no text for the child).
- Parent UI only after unlock.
- No ads, no external links in child mode.

## Repository layout

```text
talking-kitty/
  Assets/
    Scripts/Voice/
    Scripts/Cat/
    Scripts/ChildLock/
    Scenes/Main.unity
    Art/   # original or licensed only
  ProjectSettings/
  docs/
    superpowers/specs/   # this document
    parent-guide.md      # install + lock usage (Chinese)
  README.md              # Chinese: build, sideload, privacy
  LICENSE                # MIT recommended
```

## Delivery

1. Public or private GitHub repo `huaihaoge/talking-kitty` with full Unity project.
2. Release APK (no ad SDKs).
3. Parent-facing install notes in Chinese.

## Future 3D

Replace `Cat/` presenter + art with a 3D model and animation controllers. **Do not** rewrite Voice or ChildLock. Keep the same Idle / Listening / Speaking state contract.

## Success criteria

- Child can babble; after a pause, hears a higher-pitched echo without precise button use.
- With lock on, Back and on-screen UI cannot exit; parent can unlock with PIN via corner long-press or top taps.
- APK contains no ads and does not require network for core play.
- Design allows swapping the cat visual to 3D without changing the voice pipeline.

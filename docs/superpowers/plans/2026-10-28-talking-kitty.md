# Talking Kitty Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship an ad-free Android APK (Unity) that auto-listens, pitch-echoes a child’s voice like Talking Tom, shows a simple swappable cat presenter, and uses an in-app PIN child lock — hosted at `huaihaoge/talking-kitty`.

**Architecture:** Single Main scene. Pure C# voice/lock logic is EditMode-testable; MonoBehaviours wire mic, AudioSource, UI, and `ICatPresenter`. Future 3D only replaces the `Cat/` presenter implementation.

**Tech Stack:** Unity 2022.3 LTS (or Unity 6 LTS if already installed), C#, Unity Test Framework (EditMode), Android IL2CPP build, GitHub `huaihaoge/talking-kitty`.

## Global Constraints

- Echo = record + pitch playback only (no STT/TTS).
- Auto-listen: silence end **0.8–1.2 s**; min clip **~0.3 s**; max **~5–8 s**; pitch **~1.3–1.6**.
- Child lock = in-app only (Back blocked; PIN via corner long-press ~3 s or top-center 5 taps).
- No ads, no analytics; MVP Android build should omit internet permission when possible.
- Original/licensed art only — no Talking Tom IP.
- Spec: `docs/superpowers/specs/2026-10-28-talking-kitty-design.md`.

## File map (create unless noted)

| Path | Responsibility |
|------|----------------|
| `.gitignore` | Unity + OS ignores |
| `LICENSE` | MIT |
| `README.md` | Chinese install/build/privacy |
| `docs/parent-guide.md` | Parent lock/PIN guide (ZH) |
| `Assets/Scripts/Cat/CatState.cs` | Idle / Listening / Speaking |
| `Assets/Scripts/Cat/ICatPresenter.cs` | Presenter contract |
| `Assets/Scripts/Cat/SimpleCatPresenter.cs` | MVP 2D/sprite implementation |
| `Assets/Scripts/Voice/SilenceDetector.cs` | Energy + silence end (pure C#) |
| `Assets/Scripts/Voice/VoiceLoopSettings.cs` | Tunable thresholds |
| `Assets/Scripts/Voice/VoiceLoop.cs` | Mic capture → detect → pitch play |
| `Assets/Scripts/ChildLock/PinHasher.cs` | PIN hash helper |
| `Assets/Scripts/ChildLock/ChildLockController.cs` | Lock state + Back swallow |
| `Assets/Scripts/ChildLock/ParentGateInput.cs` | Corner hold / top taps |
| `Assets/Scripts/ChildLock/ParentPanel.cs` | PIN UI + parent actions |
| `Assets/Tests/EditMode/*` | EditMode tests |
| `Assets/Scenes/Main.unity` | Created in Editor |
| `ProjectSettings/*` | From Unity project create |

---

### Task 1: Repo bootstrap (docs + ignore + license)

**Files:**
- Create: `.gitignore`, `LICENSE`, `README.md`
- Keep: `docs/superpowers/specs/2026-10-28-talking-kitty-design.md`

**Interfaces:**
- Produces: clean git root ready for Unity project files

- [ ] **Step 1: Add Unity `.gitignore`**

Create `.gitignore` with standard Unity ignores including:

```
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Uu]ser[Ss]ettings/
*.csproj
*.unityproj
*.sln
*.user
*.pidb
*.booproj
*.svd
*.pdb
*.mdb
*.opendb
*.VC.db
.DS_Store
```

- [ ] **Step 2: Add MIT `LICENSE`** with copyright holder `huaihaoge` and year `2026`.

- [ ] **Step 3: Write Chinese `README.md`** covering: what the app is, no ads/offline, mic permission, child lock summary, how to open in Unity Hub, how to build Android APK, link to spec + parent guide (guide may be added in a later task).

- [ ] **Step 4: Commit**

```bash
cd /Users/qitmac001677/talking-kitty
git add .gitignore LICENSE README.md docs/superpowers/specs/2026-10-28-talking-kitty-design.md
git commit -m "$(cat <<'EOF'
docs: add Talking Kitty design spec and repo bootstrap

EOF
)"
```

---

### Task 2: Create Unity project skeleton

**Files:**
- Create via Unity Hub/Editor: Unity project at `/Users/qitmac001677/talking-kitty` (or merge generated folders into this repo root)
- Create folders: `Assets/Scripts/Cat`, `Assets/Scripts/Voice`, `Assets/Scripts/ChildLock`, `Assets/Art`, `Assets/Scenes`, `Assets/Tests/EditMode`

**Interfaces:**
- Produces: openable Unity project with Android module installable later

- [ ] **Step 1: Detect Unity**

```bash
ls "/Applications/Unity/Hub/Editor" 2>/dev/null || mdfind "kMDItemDisplayName == 'Unity Hub'" 2>/dev/null | head -5
```

If missing: stop and ask user to install **Unity 2022.3 LTS** (or Unity 6 LTS) + **Android Build Support** (SDK/NDK/OpenJDK) via Unity Hub before continuing this task.

- [ ] **Step 2: Create project**

Prefer creating a **3D (Core)** or **2D** URP/Built-in template named `talking-kitty` **into** `/Users/qitmac001677/talking-kitty` if Hub allows existing folder; otherwise create in a temp folder and move `Assets`, `Packages`, `ProjectSettings` into the repo (keep existing `docs/`, `.git`).

- [ ] **Step 3: Create script/art/test folders** listed above (empty `.gitkeep` if needed).

- [ ] **Step 4: Enable Unity Test Framework**

In Package Manager, ensure `com.unity.test-framework` is present. Create an **EditMode** test assembly definition:

`Assets/Tests/EditMode/TalkingKitty.EditModeTests.asmdef`:

```json
{
  "name": "TalkingKitty.EditModeTests",
  "rootNamespace": "TalkingKitty.Tests",
  "optionalUnityReferences": ["TestAssemblies"],
  "includePlatforms": ["Editor"],
  "reference": []
}
```

Also create `Assets/Scripts/TalkingKitty.Runtime.asmdef`:

```json
{
  "name": "TalkingKitty.Runtime",
  "rootNamespace": "TalkingKitty",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": []
}
```

Update the test asmdef `"references": ["TalkingKitty.Runtime"]` after Runtime asmdef exists.

- [ ] **Step 5: Commit**

```bash
git add Assets Packages ProjectSettings .gitignore
git commit -m "$(cat <<'EOF'
chore: scaffold Unity project and test assemblies

EOF
)"
```

---

### Task 3: Cat state + presenter contract (TDD)

**Files:**
- Create: `Assets/Scripts/Cat/CatState.cs`
- Create: `Assets/Scripts/Cat/ICatPresenter.cs`
- Create: `Assets/Scripts/Cat/SimpleCatPresenter.cs`
- Test: `Assets/Tests/EditMode/CatStateMachineTests.cs`

**Interfaces:**
- Produces:
  - `enum CatState { Idle, Listening, Speaking }`
  - `interface ICatPresenter { void SetState(CatState state); void PlayTapReaction(); CatState CurrentState { get; } }`

- [ ] **Step 1: Write failing EditMode test**

```csharp
using NUnit.Framework;
using TalkingKitty.Cat;
using UnityEngine;

public class CatStateMachineTests
{
    [Test]
    public void SetState_UpdatesCurrentState()
    {
        var go = new GameObject("cat");
        var presenter = go.AddComponent<SimpleCatPresenter>();
        presenter.SetState(CatState.Listening);
        Assert.AreEqual(CatState.Listening, presenter.CurrentState);
        presenter.SetState(CatState.Speaking);
        Assert.AreEqual(CatState.Speaking, presenter.CurrentState);
        Object.DestroyImmediate(go);
    }
}
```

- [ ] **Step 2: Run EditMode tests in Unity** (Window → General → Test Runner → EditMode → Run All). Expected: FAIL (types missing).

- [ ] **Step 3: Implement types**

`CatState.cs`:

```csharp
namespace TalkingKitty.Cat
{
    public enum CatState
    {
        Idle = 0,
        Listening = 1,
        Speaking = 2
    }
}
```

`ICatPresenter.cs`:

```csharp
namespace TalkingKitty.Cat
{
    public interface ICatPresenter
    {
        CatState CurrentState { get; }
        void SetState(CatState state);
        void PlayTapReaction();
    }
}
```

`SimpleCatPresenter.cs` (MVP: scale pulse / color tint; assign SpriteRenderer in Inspector):

```csharp
using UnityEngine;

namespace TalkingKitty.Cat
{
    public class SimpleCatPresenter : MonoBehaviour, ICatPresenter
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] Color idleColor = Color.white;
        [SerializeField] Color listeningColor = new Color(1f, 0.92f, 0.4f);
        [SerializeField] Color speakingColor = new Color(1f, 0.6f, 0.7f);

        public CatState CurrentState { get; private set; } = CatState.Idle;

        public void SetState(CatState state)
        {
            CurrentState = state;
            if (body == null) return;
            body.color = state switch
            {
                CatState.Listening => listeningColor,
                CatState.Speaking => speakingColor,
                _ => idleColor
            };
            transform.localScale = state == CatState.Speaking
                ? Vector3.one * 1.08f
                : Vector3.one;
        }

        public void PlayTapReaction()
        {
            // MVP: brief squash; later 3D can play animation clip
            transform.localScale = new Vector3(1.12f, 0.92f, 1f);
        }
    }
}
```

- [ ] **Step 4: Re-run EditMode tests.** Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Cat Assets/Tests/EditMode
git commit -m "$(cat <<'EOF'
feat: add cat presenter contract and simple 2D presenter

EOF
)"
```

---

### Task 4: Silence detector (pure C#, TDD)

**Files:**
- Create: `Assets/Scripts/Voice/VoiceLoopSettings.cs`
- Create: `Assets/Scripts/Voice/SilenceDetector.cs`
- Test: `Assets/Tests/EditMode/SilenceDetectorTests.cs`

**Interfaces:**
- Produces:
  - `class VoiceLoopSettings` with floats: `startThreshold`, `silenceSeconds` (default `1.0f`), `minSeconds` (`0.3f`), `maxSeconds` (`6f`), `pitch` (`1.45f`)
  - `class SilenceDetector` methods:
    - `void Reset()`
    - `SilenceDetectorResult Push(float absPeak, float deltaTime)` 
    - `enum SilenceDetectorResult { Idle, Capturing, UtteranceReady, DiscardedTooShort, CapturingMaxReached }`

- [ ] **Step 1: Failing tests**

```csharp
using NUnit.Framework;
using TalkingKitty.Voice;

public class SilenceDetectorTests
{
    VoiceLoopSettings Settings() => new VoiceLoopSettings
    {
        startThreshold = 0.05f,
        silenceSeconds = 1.0f,
        minSeconds = 0.3f,
        maxSeconds = 6f
    };

    [Test]
    public void LoudThenSilence_YieldsUtteranceReady()
    {
        var d = new SilenceDetector(Settings());
        Assert.AreEqual(SilenceDetectorResult.Capturing, d.Push(0.2f, 0.1f));
        // stay loud briefly
        d.Push(0.2f, 0.3f);
        // silence for 1.0s
        var r = SilenceDetectorResult.Capturing;
        for (int i = 0; i < 10; i++)
            r = d.Push(0.001f, 0.1f);
        Assert.AreEqual(SilenceDetectorResult.UtteranceReady, r);
    }

    [Test]
    public void ShortBlip_Discarded()
    {
        var d = new SilenceDetector(Settings());
        d.Push(0.2f, 0.05f);
        var r = SilenceDetectorResult.Capturing;
        for (int i = 0; i < 10; i++)
            r = d.Push(0.001f, 0.1f);
        Assert.AreEqual(SilenceDetectorResult.DiscardedTooShort, r);
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL.**

- [ ] **Step 3: Implement `VoiceLoopSettings` and `SilenceDetector`**

Logic:
- Idle until `absPeak >= startThreshold` → Capturing; accumulate `captureTime`.
- While Capturing, if `absPeak < startThreshold`, accumulate `silenceTime`; else reset `silenceTime`.
- If `silenceTime >= silenceSeconds`: if `captureTime >= minSeconds` → `UtteranceReady` then internal Reset to Idle; else → `DiscardedTooShort` then Reset.
- If `captureTime >= maxSeconds` → `CapturingMaxReached` (treat as ready) then Reset.

- [ ] **Step 4: Run tests — expect PASS.**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/Voice Assets/Tests/EditMode/SilenceDetectorTests.cs
git commit -m "$(cat <<'EOF'
feat: add silence detector for auto-listen voice loop

EOF
)"
```

---

### Task 5: VoiceLoop MonoBehaviour (mic + pitch playback)

**Files:**
- Create: `Assets/Scripts/Voice/VoiceLoop.cs`

**Interfaces:**
- Consumes: `SilenceDetector`, `VoiceLoopSettings`, `ICatPresenter`
- Produces: `VoiceLoop` that starts mic on `Start` (after permission), drives detector each frame, on utterance copies samples to a new `AudioClip`, plays via `AudioSource` with `pitch = settings.pitch`, sets cat states

- [ ] **Step 1: Implement `VoiceLoop.cs`**

Requirements:
- `[SerializeField] VoiceLoopSettings settings` (or plain serialized fields mirroring settings).
- `[SerializeField] AudioSource output`.
- `[SerializeField] MonoBehaviour catPresenterBehaviour` — resolve `ICatPresenter` in `Awake`.
- Use `Microphone.Start(null, true, 8, 16000)` (or `44100` if device requires); read peaks via `AudioClip.GetData`.
- While `output.isPlaying`, do not start a new capture utterance (pause detector or keep Idle).
- On `UtteranceReady` / `CapturingMaxReached`: build clip from recorded ring buffer segment, `output.pitch = settings.pitch`, `output.Play()`, `cat.SetState(Speaking)`; on `AudioSource` end (poll in Update) → `Listening` and resume.
- Android: before `Microphone.Start`, if `Permission.HasUserAuthorizedPermission(Permission.Microphone)` is false, `Permission.RequestUserPermission(Permission.Microphone)` (Unity `UnityEngine.Android` API).

Keep ring-buffer bookkeeping explicit in comments; prefer a `float[]` rolling buffer sized for `maxSeconds * frequency`.

- [ ] **Step 2: Manual Editor smoke test** (Play Mode on Mac with mic): speak → pause → hear higher-pitch echo; cat color/scale changes. No automated mic test required.

- [ ] **Step 3: Commit**

```bash
git add Assets/Scripts/Voice/VoiceLoop.cs
git commit -m "$(cat <<'EOF'
feat: wire microphone auto-listen loop with pitched playback

EOF
)"
```

---

### Task 6: Child lock core (TDD) + Back key

**Files:**
- Create: `Assets/Scripts/ChildLock/PinHasher.cs`
- Create: `Assets/Scripts/ChildLock/ChildLockController.cs`
- Test: `Assets/Tests/EditMode/ChildLockTests.cs`

**Interfaces:**
- Produces:
  - `PinHasher.Hash(string pin) -> string` (SHA256 hex; salt constant `TalkingKitty.v1`)
  - `ChildLockController`: `bool IsLocked`, `void SetLocked(bool)`, `bool VerifyPin(string)`, `void SetPin(string)`, `bool HasPin`, uses `PlayerPrefs` keys `tk_pin_hash`, `tk_locked`

- [ ] **Step 1: Failing tests**

```csharp
using NUnit.Framework;
using TalkingKitty.ChildLock;
using UnityEngine;

public class ChildLockTests
{
    [SetUp]
    public void SetUp()
    {
        PlayerPrefs.DeleteKey("tk_pin_hash");
        PlayerPrefs.DeleteKey("tk_locked");
        PlayerPrefs.Save();
    }

    [Test]
    public void SetPin_ThenVerify_Works()
    {
        var go = new GameObject();
        var c = go.AddComponent<ChildLockController>();
        c.SetPin("1234");
        Assert.IsTrue(c.VerifyPin("1234"));
        Assert.IsFalse(c.VerifyPin("0000"));
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Hash_IsDeterministicAndNotPlaintext()
    {
        var a = PinHasher.Hash("1234");
        var b = PinHasher.Hash("1234");
        Assert.AreEqual(a, b);
        Assert.AreNotEqual("1234", a);
    }
}
```

- [ ] **Step 2: Run — expect FAIL.**

- [ ] **Step 3: Implement `PinHasher` + `ChildLockController`**

`ChildLockController.Update` (or dedicated method called from a bootstrap): when `IsLocked`, call `Input.GetKeyDown(KeyCode.Escape)` handling — on Android Back maps to Escape in Unity; consume by not calling quit. Add public `bool ShouldAllowQuit()` → false when locked.

Do **not** call `Application.Quit` from Back while locked.

- [ ] **Step 4: Tests PASS.**

- [ ] **Step 5: Commit**

```bash
git add Assets/Scripts/ChildLock Assets/Tests/EditMode/ChildLockTests.cs
git commit -m "$(cat <<'EOF'
feat: add PIN child lock controller

EOF
)"
```

---

### Task 7: Parent gate gestures + parent panel UI

**Files:**
- Create: `Assets/Scripts/ChildLock/ParentGateInput.cs`
- Create: `Assets/Scripts/ChildLock/ParentPanel.cs`

**Interfaces:**
- Consumes: `ChildLockController`
- Produces: unlock UI flow

- [ ] **Step 1: `ParentGateInput`**

- Corner long-press: if pointer down in any corner rect (e.g. 120px from corner) for `>= 3f` seconds → invoke `onParentGateRequested`.
- Top-center taps: region at top center; count taps within 2 s window; on 5th → same event.
- Only active when locked (or always; panel still requires PIN).

- [ ] **Step 2: `ParentPanel`**

uGUI Canvas (create in Editor in Task 8, script now):
- Hidden by default.
- On gate request: show PIN entry (`InputField` or 4 buttons digit pad — digit pad preferred for phones).
- First run (`!HasPin`): “Set PIN” then confirm.
- On success: show parent menu — Toggle lock, Change PIN, Pitch slider (writes into `VoiceLoop` settings), Quit (`Application.Quit` / stop Play Mode in Editor).
- Wrong PIN: clear + shake; no unlock.

- [ ] **Step 3: Manual test checklist** (document results in commit message or leave for Task 8 scene): lock on → Back does nothing → corner hold shows PIN → unlock → Quit works.

- [ ] **Step 4: Commit**

```bash
git add Assets/Scripts/ChildLock/ParentGateInput.cs Assets/Scripts/ChildLock/ParentPanel.cs
git commit -m "$(cat <<'EOF'
feat: add parent gate gestures and parent panel

EOF
)"
```

---

### Task 8: Main scene wiring + placeholder art

**Files:**
- Create: `Assets/Art/` placeholder cat sprite (simple geometric cat PNG generated or drawn; not third-party Tom assets)
- Create: `Assets/Scenes/Main.unity` (Editor)
- Modify: Editor Build Settings — add Main as scene 0

**Interfaces:**
- Produces: Playable scene with VoiceLoop + Cat + ChildLock + Canvas

- [ ] **Step 1: Create sprite** (any simple original PNG). Import; set PPU appropriately.

- [ ] **Step 2: Build hierarchy in Main**

```text
Main
  Cat (SpriteRenderer + SimpleCatPresenter)
  Audio (AudioSource + VoiceLoop)
  ChildLock (ChildLockController + ParentGateInput)
  Canvas
    StatusDot (optional Image)
    ParentPanel (ParentPanel)
      PinRow / Menu
```

Wire references in Inspector. Default `tk_locked` suggestion: on first launch show a one-time parent tip then set locked true after PIN set.

- [ ] **Step 3: Play Mode verify** full happy path on Editor.

- [ ] **Step 4: Commit scene + art**

```bash
git add Assets/Art Assets/Scenes ProjectSettings/EditorBuildSettings.asset
git commit -m "$(cat <<'EOF'
feat: wire Main scene with voice, cat, and child lock UI

EOF
)"
```

---

### Task 9: Android player settings + APK build

**Files:**
- Modify: `ProjectSettings/ProjectSettings.asset` via Editor
- Create: `docs/parent-guide.md`
- Modify: `README.md` with exact build steps

- [ ] **Step 1: Player Settings**

- Company: `huaihaoge`; Product: `TalkingKitty`; Package Name: `com.huaihaoge.talkingkitty`
- Minimum API: 24+; Target: device current
- Orientation: Portrait (or Auto Portrait)
- Scripting Backend: IL2CPP; Target Architectures: ARM64
- Remove Ads; do not integrate any ad/analytics packages
- In Android Manifest / Player: disable Internet if no packages need it (Unity sometimes adds it — strip via custom manifest if present):

`Assets/Plugins/Android/AndroidManifest.xml` only if needed to force no `INTERNET` and ensure `RECORD_AUDIO`.

- [ ] **Step 2: Build APK**

Unity → File → Build Settings → Android → Build → output `Builds/TalkingKitty.apk` (folder gitignored; upload via GitHub Release later).

- [ ] **Step 3: Write `docs/parent-guide.md` (Chinese)** — install APK, grant mic, set PIN, enable lock, unlock gestures, known Home-key limit, optional system Screen Pinning.

- [ ] **Step 4: Commit project settings + docs** (not the APK binary unless using GitHub Releases)

```bash
git add ProjectSettings Assets/Plugins/Android docs/parent-guide.md README.md
git commit -m "$(cat <<'EOF'
feat: configure Android build and parent guide

EOF
)"
```

---

### Task 10: Publish GitHub repo + release

**Files:** remote only

- [ ] **Step 1: Ensure `gh` CLI available** (install via `brew install gh` if missing) and authenticated as `huaihaoge`.

- [ ] **Step 2: Create repo and push**

```bash
cd /Users/qitmac001677/talking-kitty
gh repo create huaihaoge/talking-kitty --private --source=. --remote=origin --push
```

(Use `--public` if user prefers public.)

- [ ] **Step 3: Upload APK**

```bash
gh release create v0.1.0 Builds/TalkingKitty.apk --title "v0.1.0" --notes "First ad-free Talking Kitty APK: auto echo + child lock."
```

- [ ] **Step 4: Smoke-install on a physical Android device** — mic, echo, lock, unlock. Fix critical bugs in follow-up commits if found.

---

## Spec coverage check

| Spec requirement | Task |
|------------------|------|
| Record + pitch echo | 4, 5 |
| Auto-listen silence 0.8–1.2s / min / max | 4, 5 |
| Simple cat + swappable presenter | 3, 8 |
| In-app lock + PIN + gestures | 6, 7 |
| No ads / no network MVP | 9 |
| GitHub huaihaoge/talking-kitty + APK | 1, 10 |
| Path to 3D via Cat/ only | 3 (interface) |

## Plan self-review

- No TBD placeholders in task steps.
- Types aligned: `CatState`, `ICatPresenter`, `SilenceDetectorResult`, `ChildLockController` PlayerPrefs keys.
- Pitch via `AudioSource.pitch` (classic Tom chipmunk; documented as MVP).

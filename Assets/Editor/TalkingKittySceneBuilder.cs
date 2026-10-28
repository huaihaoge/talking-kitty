// TalkingKittySceneBuilder — programmatically builds Assets/Scenes/Main.unity.
//
// HOW TO USE: after Unity Editor has imported this project at least once,
// run the menu item "TalkingKitty → Build Main Scene". This creates the
// full scene hierarchy, wires every component reference, saves the scene,
// and adds it to Editor Build Settings as scene 0. The menu is safe to run
// again; it rebuilds the scene from scratch each time.
//
// Hierarchy built (from Task 8 brief):
//   Main
//     Main Camera   (Camera + AudioListener)
//     Cat           (SpriteRenderer + SimpleCatPresenter)
//     Audio         (AudioSource + VoiceLoop)
//     ChildLock     (ChildLockController + ParentGateInput)
//     Canvas        (Canvas + CanvasScaler + GraphicRaycaster)
//       ParentPanel (ParentPanel)
//         PanelRoot (Image; hidden — panelRoot + shakeTarget)
//           PinPadGroup  (prompt, pin bullets, digits 0-9, Clear, Delete)
//           MenuGroup    (lock toggle, change PIN, pitch slider, quit)
//           CloseButton
//     EventSystem   (EventSystem + StandaloneInputModule)

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TalkingKitty.Cat;
using TalkingKitty.ChildLock;
using TalkingKitty.Voice;

namespace TalkingKitty.EditorTools
{
    public static class TalkingKittySceneBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string CatSpritePath = "Assets/Art/cat_placeholder.png";
        const string MouthClosedPath = "Assets/Art/mouth_closed.png";
        const string MouthOpenPath = "Assets/Art/mouth_open.png";

        // CanvasScaler reference resolution (portrait phone).
        static readonly Vector2 RefResolution = new Vector2(1080f, 1920f);

        [MenuItem("TalkingKitty/Build Main Scene")]
        public static void BuildMainScene()
        {
            if (!ConfigureCatSpriteImport())
            {
                Debug.LogError($"[TalkingKittySceneBuilder] Cat sprite missing at {CatSpritePath}; aborting.");
                return;
            }

            ConfigureSpriteImport(MouthClosedPath, 64f);
            ConfigureSpriteImport(MouthOpenPath, 64f);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            var cat = BuildCat();
            var voiceLoop = BuildAudio(cat);
            BuildChildLockAndUi(voiceLoop);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"[TalkingKittySceneBuilder] Failed to save scene to {ScenePath}.");
                return;
            }

            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();

            Debug.Log($"[TalkingKittySceneBuilder] Built and saved {ScenePath} and set it as build scene 0. " +
                      "Enter Play Mode and verify: speak → pitched echo, cat tints, corner hold 3s → parent panel.");
        }

        // ---- import settings -------------------------------------------------

        static bool ConfigureCatSpriteImport()
        {
            if (!File.Exists(CatSpritePath)) return false;
            ConfigureSpriteImport(CatSpritePath, 128f); // 512px → 4 world units at ortho size 4
            return true;
        }

        static void ConfigureSpriteImport(string path, float pixelsPerUnit)
        {
            if (!File.Exists(path)) return;
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        // ---- scene objects ----------------------------------------------------

        static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);

            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.13f, 0.16f, 0.28f);

            go.AddComponent<AudioListener>();
        }

        static SimpleCatPresenter BuildCat()
        {
            var go = new GameObject("Cat");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CatSpritePath);
            sr.sortingOrder = 0;

            var mouthGo = new GameObject("Mouth");
            mouthGo.transform.SetParent(go.transform, false);
            // Lower-center of the cat face (sprite is 4 units tall at PPU 128).
            mouthGo.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            mouthGo.transform.localScale = Vector3.one * 0.85f;

            var mouthSr = mouthGo.AddComponent<SpriteRenderer>();
            mouthSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MouthClosedPath);
            mouthSr.sortingOrder = 1;

            var presenter = go.AddComponent<SimpleCatPresenter>();
            var so = new SerializedObject(presenter);
            so.FindProperty("body").objectReferenceValue = sr;
            so.FindProperty("mouth").objectReferenceValue = mouthSr;
            so.FindProperty("mouthClosed").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(MouthClosedPath);
            so.FindProperty("mouthOpen").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(MouthOpenPath);
            so.ApplyModifiedProperties();
            return presenter;
        }

        static VoiceLoop BuildAudio(SimpleCatPresenter cat)
        {
            var go = new GameObject("Audio");

            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D UI/voice audio

            var loop = go.AddComponent<VoiceLoop>();
            var so = new SerializedObject(loop);
            so.FindProperty("output").objectReferenceValue = src;
            so.FindProperty("catPresenterBehaviour").objectReferenceValue = cat;
            so.ApplyModifiedProperties();
            return loop;
        }

        static void BuildChildLockAndUi(VoiceLoop voiceLoop)
        {
            // --- ChildLock (logic + hidden gestures) ---
            var lockGo = new GameObject("ChildLock");
            var controller = lockGo.AddComponent<ChildLockController>();
            var gate = lockGo.AddComponent<ParentGateInput>();
            var gateSo = new SerializedObject(gate);
            gateSo.FindProperty("childLock").objectReferenceValue = controller;
            gateSo.ApplyModifiedProperties();

            // --- Canvas (Screen Space Overlay, portrait phone scaling) ---
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = RefResolution;
            scaler.matchWidthOrHeight = 0.5f;

            // --- ParentPanel component GO (always active) ---
            var panelGo = new GameObject("ParentPanel", typeof(RectTransform));
            var panelRect = (RectTransform)panelGo.transform;
            panelRect.SetParent(canvasGo.transform, false);
            Stretch(panelRect);
            var panel = panelGo.AddComponent<ParentPanel>();

            // --- PanelRoot: hidden dim overlay, shake target ---
            var panelRootRt = UiNode("PanelRoot", panelRect);
            Stretch(panelRootRt);
            var dim = panelRootRt.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            panelRootRt.gameObject.SetActive(false);

            var pinPadGroup = BuildPinPadGroup(panelRootRt);
            var menuGroup = BuildMenuGroup(panelRootRt);
            menuGroup.gameObject.SetActive(false);

            var closeButton = MakeButton("CloseButton", panelRootRt, "\u2715", new Vector2(90f, 90f), 44);
            var closeRt = (RectTransform)closeButton.transform;
            closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-30f, -30f);

            // --- EventSystem ---
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // --- wire ParentPanel serialized references ---
            var so = new SerializedObject(panel);
            so.FindProperty("childLock").objectReferenceValue = controller;
            so.FindProperty("voiceLoop").objectReferenceValue = voiceLoop;
            so.FindProperty("panelRoot").objectReferenceValue = panelRootRt.gameObject;
            so.FindProperty("shakeTarget").objectReferenceValue = panelRootRt;
            so.FindProperty("pinPadGroup").objectReferenceValue = pinPadGroup.gameObject;
            so.FindProperty("promptText").objectReferenceValue = pinPadGroup.GetComponentInChildren<Text>(true);
            so.FindProperty("pinDisplay").objectReferenceValue = pinPadGroup.transform.Find("PinDisplay")?.GetComponent<Text>();
            so.FindProperty("menuGroup").objectReferenceValue = menuGroup.gameObject;

            var digits = so.FindProperty("digitButtons");
            digits.arraySize = 10;
            for (int i = 0; i < 10; i++)
                digits.GetArrayElementAtIndex(i).objectReferenceValue = _digitButtons[i];
            so.FindProperty("clearButton").objectReferenceValue = _clearButton;
            so.FindProperty("deleteButton").objectReferenceValue = _deleteButton;

            so.FindProperty("lockToggle").objectReferenceValue = _lockToggle;
            so.FindProperty("changePinButton").objectReferenceValue = _changePinButton;
            so.FindProperty("pitchSlider").objectReferenceValue = _pitchSlider;
            so.FindProperty("pitchValueText").objectReferenceValue = _pitchValueText;
            so.FindProperty("quitButton").objectReferenceValue = _quitButton;
            so.FindProperty("closeButton").objectReferenceValue = closeButton;
            so.ApplyModifiedProperties();

            // --- wire gate → panel.Open (persistent listener) ---
            UnityEventTools.AddVoidPersistentListener(gate.OnParentGateRequested, panel.Open);
        }

        // ---- UI stubs -----------------------------------------------------------

        static Button[] _digitButtons = new Button[10];
        static Button _clearButton, _deleteButton;
        static Toggle _lockToggle;
        static Button _changePinButton, _quitButton;
        static Slider _pitchSlider;
        static Text _pitchValueText;

        static RectTransform BuildPinPadGroup(RectTransform parent)
        {
            var group = UiNode("PinPadGroup", parent);
            group.sizeDelta = new Vector2(760f, 860f);
            group.anchoredPosition = new Vector2(0f, 60f);

            var prompt = MakeText("PromptText", group, "Enter PIN", 52, TextAnchor.MiddleCenter, Color.white);
            var promptRt = (RectTransform)prompt.transform;
            promptRt.anchorMin = promptRt.anchorMax = new Vector2(0.5f, 1f);
            promptRt.pivot = new Vector2(0.5f, 1f);
            promptRt.sizeDelta = new Vector2(700f, 76f);
            promptRt.anchoredPosition = new Vector2(0f, -10f);

            var pinDisplay = MakeText("PinDisplay", group, "", 72, TextAnchor.MiddleCenter, Color.white);
            var pinRt = (RectTransform)pinDisplay.transform;
            pinRt.anchorMin = pinRt.anchorMax = new Vector2(0.5f, 1f);
            pinRt.pivot = new Vector2(0.5f, 1f);
            pinRt.sizeDelta = new Vector2(400f, 90f);
            pinRt.anchoredPosition = new Vector2(0f, -100f);

            // 3x4 pad: 1..9, then Clear / 0 / Delete. digitButtons[i] index == digit.
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int slot = r * 3 + c;
                    int digit = (slot < 9) ? slot + 1 : (c == 1 ? 0 : -1);
                    string name = digit >= 0 ? $"Digit_{digit}" : (c == 0 ? "ClearButton" : "DeleteButton");
                    string label = digit >= 0 ? digit.ToString() : (c == 0 ? "C" : "\u232B");

                    var btn = MakeButton(name, group, label, new Vector2(210f, 130f), 48);
                    var rt = (RectTransform)btn.transform;
                    rt.anchoredPosition = new Vector2((c - 1) * 234f, -250f - r * 154f);

                    if (digit >= 0) _digitButtons[digit] = btn;
                    else if (c == 0) _clearButton = btn;
                    else _deleteButton = btn;
                }
            }
            return group;
        }

        static RectTransform BuildMenuGroup(RectTransform parent)
        {
            var group = UiNode("MenuGroup", parent);
            group.sizeDelta = new Vector2(800f, 980f);

            var title = MakeText("MenuTitle", group, "Parent Menu", 52, TextAnchor.MiddleCenter, Color.white);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchoredPosition = new Vector2(0f, 390f);
            titleRt.sizeDelta = new Vector2(700f, 80f);

            _lockToggle = MakeToggle("LockToggle", group, "Child Lock", new Vector2(660f, 64f));
            ((RectTransform)_lockToggle.transform).anchoredPosition = new Vector2(0f, 250f);

            _changePinButton = MakeButton("ChangePinButton", group, "Change PIN", new Vector2(660f, 110f), 44);
            ((RectTransform)_changePinButton.transform).anchoredPosition = new Vector2(0f, 110f);

            _pitchSlider = MakeSlider("PitchSlider", group, new Vector2(600f, 40f));
            ((RectTransform)_pitchSlider.transform).anchoredPosition = new Vector2(0f, -20f);

            _pitchValueText = MakeText("PitchValueText", group, "1.45x", 40, TextAnchor.MiddleCenter, Color.white);
            ((RectTransform)_pitchValueText.transform).anchoredPosition = new Vector2(0f, -90f);
            ((RectTransform)_pitchValueText.transform).sizeDelta = new Vector2(300f, 60f);

            _quitButton = MakeButton("QuitButton", group, "Quit", new Vector2(660f, 110f), 44);
            ((RectTransform)_quitButton.transform).anchoredPosition = new Vector2(0f, -220f);

            return group;
        }

        // ---- UI primitives --------------------------------------------------------

        static RectTransform UiNode(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Text MakeText(string name, Transform parent, string content, int size, TextAnchor anchor, Color color)
        {
            var rt = UiNode(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            // Unity 2022.2+ renamed the built-in font asset from Arial to LegacyRuntime.
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = content;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        static Button MakeButton(string name, Transform parent, string label, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = new Color(0.92f, 0.92f, 0.95f);

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;

            var labelRt = UiNode("Label", rt);
            Stretch(labelRt);
            var t = labelRt.gameObject.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = label;
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.12f, 0.12f, 0.14f);
            t.raycastTarget = false;
            return btn;
        }

        static Toggle MakeToggle(string name, Transform parent, string label, Vector2 size)
        {
            var rt = UiNode(name, parent);
            rt.sizeDelta = size;

            var bg = UiNode("Background", rt);
            bg.anchorMin = bg.anchorMax = new Vector2(0f, 0.5f);
            bg.pivot = new Vector2(0f, 0.5f);
            bg.anchoredPosition = Vector2.zero;
            bg.sizeDelta = new Vector2(52f, 52f);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.color = Color.white;

            var check = UiNode("Checkmark", bg);
            Stretch(check);
            var checkImg = check.gameObject.AddComponent<Image>();
            checkImg.color = new Color(0.25f, 0.68f, 0.3f);

            var labelRt = (RectTransform)MakeText("Label", rt, label, 40, TextAnchor.MiddleLeft, Color.white).transform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.offsetMin = new Vector2(68f, 0f);
            labelRt.offsetMax = Vector2.zero;

            var toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = false;
            return toggle;
        }

        static Slider MakeSlider(string name, Transform parent, Vector2 size)
        {
            var rt = UiNode(name, parent);
            rt.sizeDelta = size;

            var bg = UiNode("Background", rt);
            Stretch(bg);
            bg.gameObject.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

            var fillArea = UiNode("Fill Area", rt);
            fillArea.anchorMin = new Vector2(0f, 0.25f);
            fillArea.anchorMax = new Vector2(1f, 0.75f);
            fillArea.offsetMin = fillArea.offsetMax = new Vector2(8f, 0f);
            var fill = UiNode("Fill", fillArea);
            Stretch(fill);
            fill.gameObject.AddComponent<Image>().color = new Color(0.96f, 0.62f, 0.26f);

            var handleArea = UiNode("Handle Slide Area", rt);
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(8f, -8f);
            handleArea.offsetMax = new Vector2(-8f, 8f);
            var handle = UiNode("Handle", handleArea);
            handle.sizeDelta = new Vector2(40f, 40f);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = Color.white;

            var slider = rt.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = VoiceLoop.PitchMin;
            slider.maxValue = VoiceLoop.PitchMax;
            slider.value = 1.45f;
            return slider;
        }

        // ---- build settings --------------------------------------------------------

        static void AddSceneToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TalkingKitty.Voice;

namespace TalkingKitty.ChildLock
{
    /// <summary>
    /// PIN entry + parent menu panel. Hidden by default; opened by
    /// <see cref="Open"/> (wire <c>ParentGateInput.onParentGateRequested</c>
    /// to it). First run asks to set and confirm a PIN; afterwards the PIN
    /// must be verified. On success the menu offers: toggle the child lock,
    /// change the PIN, a playback-pitch slider (applied to the
    /// <see cref="VoiceLoop"/> and persisted), and Quit. A wrong PIN clears
    /// the entry and shakes the panel — it never unlocks.
    ///
    /// Everything is Inspector-wirable serialized fields; the Canvas itself
    /// is built in the scene in Task 8. Put this component on an
    /// always-active GameObject and point <see cref="panelRoot"/> at the
    /// panel child that gets shown/hidden.
    /// </summary>
    public class ParentPanel : MonoBehaviour
    {
        enum PanelState { Hidden, EnterPin, SetPin, ConfirmPin, Menu }

        const string PitchPrefKey = "tk_pitch";
        const float DefaultPitch = 1.45f;

        [Header("Wiring")]
        [SerializeField] ChildLockController childLock;
        [SerializeField] VoiceLoop voiceLoop;

        [Header("Panel")]
        [Tooltip("Root GameObject of the panel; hidden until Open().")]
        [SerializeField] GameObject panelRoot;
        [Tooltip("RectTransform that wiggles on a wrong PIN.")]
        [SerializeField] RectTransform shakeTarget;

        [Header("PIN entry")]
        [Tooltip("Digit pad + prompt group; hidden while the menu shows.")]
        [SerializeField] GameObject pinPadGroup;
        [Tooltip("Prompt shown above the pad: Enter PIN / Set PIN / Confirm PIN / New PIN.")]
        [SerializeField] Text promptText;
        [Tooltip("Shows one bullet per entered digit.")]
        [SerializeField] Text pinDisplay;
        [Tooltip("Digit buttons 0-9; array index is the digit. Auto-wired in Awake. Individual buttons can instead be wired to PressDigit(int) with a static parameter.")]
        [SerializeField] Button[] digitButtons;
        [SerializeField] Button clearButton;
        [SerializeField] Button deleteButton;

        [Header("Menu")]
        [Tooltip("Menu controls group; hidden during PIN entry.")]
        [SerializeField] GameObject menuGroup;
        [Tooltip("Kid-lock on/off; state synced from ChildLockController when the menu opens.")]
        [SerializeField] Toggle lockToggle;
        [SerializeField] Button changePinButton;
        [SerializeField] Slider pitchSlider;
        [SerializeField] Text pitchValueText;
        [SerializeField] Button quitButton;

        [Header("Other")]
        [SerializeField] Button closeButton;

        [Header("Config")]
        [SerializeField] int pinLength = 4;
        [SerializeField] float shakeSeconds = 0.5f;
        [SerializeField] float shakePixels = 12f;

        PanelState _state = PanelState.Hidden;
        readonly StringBuilder _entry = new StringBuilder();
        string _pendingPin;      // first entry while setting/confirming a PIN
        bool _shaking;

        void Awake()
        {
            // Hidden by default, even if the scene saved it visible.
            if (panelRoot != null) panelRoot.SetActive(false);

            // Wire the digit pad: index == digit, so nothing needs per-button
            // Inspector wiring beyond dropping the ten buttons in order.
            if (digitButtons != null)
            {
                for (int i = 0; i < digitButtons.Length; i++)
                {
                    int digit = i; // capture for the closure
                    if (digitButtons[i] != null)
                        digitButtons[i].onClick.AddListener(() => EnterDigit(digit));
                }
            }

            if (clearButton != null) clearButton.onClick.AddListener(ClearEntry);
            if (deleteButton != null) deleteButton.onClick.AddListener(DeleteDigit);
            if (changePinButton != null) changePinButton.onClick.AddListener(OnChangePin);
            if (quitButton != null) quitButton.onClick.AddListener(Quit);
            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (lockToggle != null) lockToggle.onValueChanged.AddListener(OnLockToggled);
            if (pitchSlider != null) pitchSlider.onValueChanged.AddListener(OnPitchChanged);
        }

        void Start()
        {
            // Restore the persisted pitch into the voice loop so the
            // parent's choice survives restarts. The slider picks the same
            // value up when the menu opens.
            if (voiceLoop != null && PlayerPrefs.HasKey(PitchPrefKey))
                voiceLoop.Pitch = PlayerPrefs.GetFloat(PitchPrefKey);
        }

        /// <summary>Wire ParentGateInput.onParentGateRequested here.</summary>
        public void Open()
        {
            if (panelRoot == null) return;
            if (panelRoot.activeSelf) return; // already open — don't kick the parent out

            if (childLock == null)
            {
                Debug.LogError("[ParentPanel] ChildLockController not assigned.");
                return;
            }

            if (childLock.HasPin) ShowPinPad(PanelState.EnterPin, "Enter PIN");
            else ShowPinPad(PanelState.SetPin, "Set PIN");
        }

        /// <summary>Hide the panel without changing anything.</summary>
        public void Close()
        {
            _state = PanelState.Hidden;
            _pendingPin = null;
            ClearEntry();
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        /// <summary>Alternative wiring for a single digit button with a static int parameter.</summary>
        public void PressDigit(int digit) => EnterDigit(digit);

        // ---- PIN entry ----

        void EnterDigit(int digit)
        {
            if (digit < 0 || digit > 9) return;
            if (_state != PanelState.EnterPin && _state != PanelState.SetPin && _state != PanelState.ConfirmPin) return;
            if (_entry.Length >= pinLength) return;

            _entry.Append((char)('0' + digit));
            UpdatePinDisplay();

            // Auto-submit once the full PIN length is entered.
            if (_entry.Length >= pinLength) SubmitPin();
        }

        void DeleteDigit()
        {
            if (_entry.Length > 0) _entry.Length--;
            UpdatePinDisplay();
        }

        void ClearEntry()
        {
            _entry.Clear();
            UpdatePinDisplay();
        }

        void SubmitPin()
        {
            string pin = _entry.ToString();
            switch (_state)
            {
                case PanelState.EnterPin:
                    if (childLock.VerifyPin(pin)) EnterMenu();
                    else WrongPin();
                    break;

                case PanelState.SetPin:
                    // First half of set/change: ask for the same PIN again.
                    _pendingPin = pin;
                    _state = PanelState.ConfirmPin;
                    if (promptText != null) promptText.text = "Confirm PIN";
                    ClearEntry();
                    break;

                case PanelState.ConfirmPin:
                    if (pin == _pendingPin)
                    {
                        childLock.SetPin(pin);
                        _pendingPin = null;
                        EnterMenu();
                    }
                    else
                    {
                        // Mismatch: back to square one, wiggle, no unlock.
                        _pendingPin = null;
                        _state = PanelState.SetPin;
                        if (promptText != null) promptText.text = "Set PIN";
                        WrongPin();
                    }
                    break;
            }
        }

        void WrongPin()
        {
            ClearEntry();
            if (!_shaking) StartCoroutine(Shake());
        }

        IEnumerator Shake()
        {
            if (shakeTarget == null) yield break;
            _shaking = true;
            Vector2 origin = shakeTarget.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < shakeSeconds)
            {
                elapsed += Time.deltaTime;
                float fade = 1f - elapsed / shakeSeconds;      // ease out
                float offset = Mathf.Sin(elapsed * 60f) * shakePixels * fade;
                shakeTarget.anchoredPosition = origin + Vector2.left * offset;
                yield return null;
            }
            shakeTarget.anchoredPosition = origin;
            _shaking = false;
        }

        void ShowPinPad(PanelState state, string prompt)
        {
            _state = state;
            panelRoot.SetActive(true);
            if (menuGroup != null) menuGroup.SetActive(false);
            if (pinPadGroup != null) pinPadGroup.SetActive(true);
            if (promptText != null) promptText.text = prompt;
            ClearEntry();
        }

        // ---- Menu ----

        void EnterMenu()
        {
            _state = PanelState.Menu;
            ClearEntry();

            if (!panelRoot.activeSelf) panelRoot.SetActive(true);
            if (pinPadGroup != null) pinPadGroup.SetActive(false);
            if (menuGroup != null) menuGroup.SetActive(true);

            // Sync controls without firing their change handlers.
            if (lockToggle != null)
                lockToggle.SetIsOnWithoutNotify(childLock.IsLocked);

            float pitch = CurrentPitch;
            if (pitchSlider != null)
                pitchSlider.SetValueWithoutNotify(pitch);
            UpdatePitchText();
        }

        void OnLockToggled(bool on)
        {
            if (_state != PanelState.Menu)
            {
                // Only the menu may change the lock; revert stray events.
                if (lockToggle != null && childLock != null)
                    lockToggle.SetIsOnWithoutNotify(childLock.IsLocked);
                return;
            }
            if (childLock != null) childLock.SetLocked(on);
        }

        void OnChangePin()
        {
            if (_state != PanelState.Menu) return;
            // The parent already authenticated to reach the menu, so go
            // straight to entering the new PIN.
            ShowPinPad(PanelState.SetPin, "New PIN");
        }

        void OnPitchChanged(float value)
        {
            float pitch = Mathf.Clamp(value, VoiceLoop.PitchMin, VoiceLoop.PitchMax);
            if (voiceLoop != null) voiceLoop.Pitch = pitch;
            PlayerPrefs.SetFloat(PitchPrefKey, pitch);
            PlayerPrefs.Save();
            UpdatePitchText();
        }

        float CurrentPitch => voiceLoop != null ? voiceLoop.Pitch
            : PlayerPrefs.GetFloat(PitchPrefKey, DefaultPitch);

        void UpdatePitchText()
        {
            if (pitchValueText == null) return;
            pitchValueText.text = CurrentPitch.ToString("0.00") + "x";
        }

        void Quit()
        {
#if UNITY_EDITOR
            // Stop Play Mode in the Editor.
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void UpdatePinDisplay()
        {
            if (pinDisplay != null)
                pinDisplay.text = new string('\u2022', _entry.Length);
        }
    }
}

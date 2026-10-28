using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace TalkingKitty.ChildLock
{
    /// <summary>
    /// Hidden gestures that surface the parent panel: a press held for at
    /// least <see cref="holdSeconds"/> inside any screen corner, or
    /// <see cref="requiredTaps"/> taps within <see cref="tapWindowSeconds"/>
    /// in the top-center region. Both raise
    /// <see cref="onParentGateRequested"/> (wire it to
    /// <c>ParentPanel.Open</c> in the Inspector).
    ///
    /// Uses raw touch/mouse input so the gestures work when no UI is
    /// visible; presses that begin over UI controls are ignored. Multi-touch
    /// safe: each touch is tracked independently.
    /// </summary>
    public class ParentGateInput : MonoBehaviour
    {
        /// <summary>Synthetic pointer id for the mouse (Editor/desktop).</summary>
        const int MousePointerId = int.MinValue;

        [Header("Wiring")]
        [SerializeField] ChildLockController childLock;
        [SerializeField] UnityEvent onParentGateRequested = new UnityEvent();

        [Header("Activation")]
        [Tooltip("If true, gestures only respond while the app is locked (kid mode). Otherwise always active; the panel still requires the PIN.")]
        [SerializeField] bool onlyWhenLocked = false;

        [Header("Corner long-press")]
        [Tooltip("Size in pixels of the corner squares that accept the long-press.")]
        [SerializeField] float cornerSize = 120f;
        [Tooltip("Hold a corner this long (seconds) to request the gate.")]
        [SerializeField] float holdSeconds = 3f;

        [Header("Top-center taps")]
        [Tooltip("Width of the tap region as a fraction of screen width.")]
        [SerializeField, Range(0.1f, 1f)] float topRegionWidth = 0.5f;
        [Tooltip("Height of the tap region as a fraction of screen height, measured from the top edge.")]
        [SerializeField, Range(0.05f, 1f)] float topRegionHeight = 0.25f;
        [Tooltip("Each tap must begin within this many seconds of the previous one to stay in the same sequence.")]
        [SerializeField] float tapWindowSeconds = 2f;
        [Tooltip("Taps in the region needed to request the gate.")]
        [SerializeField] int requiredTaps = 5;

        struct Press
        {
            public float startTime;   // Time.time when the press began
            public bool inCorner;     // began inside a corner square
            public bool fired;        // already raised the gate this press
        }

        readonly Dictionary<int, Press> _presses = new Dictionary<int, Press>();

        int _tapCount;
        float _lastTapTime = float.NegativeInfinity;

        bool GateActive => !onlyWhenLocked || (childLock != null && childLock.IsLocked);

        void Update()
        {
            if (!GateActive)
            {
                ResetTracking();
                return;
            }

            // Mouse fallback (Editor / desktop). Only when no touches, so a
            // touch device never double-counts a press.
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0)) BeginPress(MousePointerId, Input.mousePosition);
                else if (Input.GetMouseButtonUp(0)) EndPress(MousePointerId);
            }

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Began) BeginPress(t.fingerId, t.position);
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) EndPress(t.fingerId);
            }

            CheckHolds();
        }

        void BeginPress(int pointerId, Vector2 pos)
        {
            if (IsOverUi(pointerId))
            {
                // Over a UI control: leave the press to the UI. A stray
                // off-region touch also cancels any half-built tap sequence.
                if (!InTopRegion(pos)) _tapCount = 0;
                return;
            }

            bool inCorner = InCornerRegion(pos);
            _presses[pointerId] = new Press
            {
                startTime = Time.time,
                inCorner = inCorner,
            };

            if (InTopRegion(pos))
            {
                float now = Time.time;
                _tapCount = (now - _lastTapTime <= tapWindowSeconds) ? _tapCount + 1 : 1;
                _lastTapTime = now;
                if (_tapCount >= requiredTaps) Fire();
            }
            else
            {
                // A press outside the region restarts the tap sequence, so
                // random kid mashing cannot accumulate toward a trigger.
                _tapCount = 0;
            }
        }

        void EndPress(int pointerId)
        {
            _presses.Remove(pointerId);
        }

        /// <summary>
        /// Fire the corner long-press for any press that crossed the hold
        /// threshold this frame (once per press).
        /// </summary>
        void CheckHolds()
        {
            if (_presses.Count == 0) return;

            int? toFire = null;
            foreach (var kvp in _presses)
            {
                var p = kvp.Value;
                if (p.inCorner && !p.fired && Time.time - p.startTime >= holdSeconds)
                {
                    toFire = kvp.Key;
                    break;
                }
            }

            if (!toFire.HasValue) return;

            // Mark fired outside the foreach so the dictionary is not
            // modified during enumeration.
            var press = _presses[toFire.Value];
            press.fired = true;
            _presses[toFire.Value] = press;
            Fire();
        }

        void Fire()
        {
            _tapCount = 0;
            onParentGateRequested.Invoke();
        }

        void ResetTracking()
        {
            _presses.Clear();
            _tapCount = 0;
            _lastTapTime = float.NegativeInfinity;
        }

        bool IsOverUi(int pointerId)
        {
            var es = EventSystem.current;
            if (es == null) return false;

            if (pointerId == MousePointerId) return es.IsPointerOverGameObject();
            return es.IsPointerOverGameObject(pointerId);
        }

        /// <summary>True when pos lies inside any of the four corner squares.</summary>
        bool InCornerRegion(Vector2 pos)
        {
            float w = Screen.width;
            float h = Screen.height;
            bool nearLeft = pos.x <= cornerSize;
            bool nearRight = pos.x >= w - cornerSize;
            bool nearBottom = pos.y <= cornerSize;
            bool nearTop = pos.y >= h - cornerSize;
            return (nearLeft && nearBottom) || (nearLeft && nearTop)
                || (nearRight && nearBottom) || (nearRight && nearTop);
        }

        /// <summary>True when pos lies inside the top-center tap region.</summary>
        bool InTopRegion(Vector2 pos)
        {
            float w = Screen.width;
            float regionW = w * topRegionWidth;
            float half = regionW * 0.5f;
            float mid = w * 0.5f;
            return pos.x >= mid - half && pos.x <= mid + half
                && pos.y >= Screen.height * (1f - topRegionHeight);
        }
    }
}

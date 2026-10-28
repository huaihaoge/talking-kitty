using UnityEngine;

namespace TalkingKitty.Cat
{
    public class SimpleCatPresenter : MonoBehaviour, ICatPresenter
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer mouth;
        [SerializeField] Sprite mouthClosed;
        [SerializeField] Sprite mouthOpen;
        [SerializeField] Color idleColor = Color.white;
        [SerializeField] Color listeningColor = new Color(1f, 0.92f, 0.4f);
        [SerializeField] Color speakingColor = new Color(1f, 0.6f, 0.7f);
        [SerializeField] float mouthCyclesPerSecond = 4f;
        [SerializeField] float mouthOpenAmount = 1f;

        Vector3 _baseScale = Vector3.one;
        Vector3 _mouthBaseScale = Vector3.one;

        public CatState CurrentState { get; private set; } = CatState.Idle;

        void Awake()
        {
            _baseScale = transform.localScale;
            if (mouth != null)
                _mouthBaseScale = mouth.transform.localScale;
            ApplyMouthClosed();
        }

        public void SetState(CatState state)
        {
            CurrentState = state;
            if (body != null)
            {
                body.color = state switch
                {
                    CatState.Listening => listeningColor,
                    CatState.Speaking => speakingColor,
                    _ => idleColor
                };
            }

            transform.localScale = state == CatState.Speaking
                ? _baseScale * 1.08f
                : _baseScale;

            if (state != CatState.Speaking)
                ApplyMouthClosed();
        }

        void Update()
        {
            if (CurrentState != CatState.Speaking || mouth == null)
                return;

            // Ping-pong open/close while echoing speech.
            var t = (Mathf.Sin(Time.time * Mathf.PI * 2f * mouthCyclesPerSecond) + 1f) * 0.5f;
            var open = Mathf.Lerp(0.15f, Mathf.Clamp01(mouthOpenAmount), t);
            ApplyMouthOpenAmount(open);
        }

        public void PlayTapReaction()
        {
            transform.localScale = new Vector3(_baseScale.x * 1.12f, _baseScale.y * 0.92f, _baseScale.z);
        }

        void ApplyMouthClosed()
        {
            ApplyMouthOpenAmount(0.12f);
            if (mouth != null && mouthClosed != null)
                mouth.sprite = mouthClosed;
        }

        void ApplyMouthOpenAmount(float open01)
        {
            if (mouth == null) return;

            if (mouthOpen != null && mouthClosed != null)
                mouth.sprite = open01 > 0.45f ? mouthOpen : mouthClosed;

            // Stretch mouth vertically for a clearer talk flap even with one sprite.
            var y = Mathf.Lerp(0.25f, 1.15f, open01);
            mouth.transform.localScale = new Vector3(_mouthBaseScale.x, _mouthBaseScale.y * y, _mouthBaseScale.z);
        }
    }
}

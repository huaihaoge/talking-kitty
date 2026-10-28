using UnityEngine;

namespace TalkingKitty.Cat
{
    /// <summary>
    /// Shows the approved concept-art kid (looks like the pictures), with a
    /// speaking mouth overlay that flaps while echoing. This is 2.5D/billboard
    /// on purpose so the in-app look matches the concept images.
    /// </summary>
    public class ConceptKidPresenter : MonoBehaviour, ICatPresenter
    {
        [SerializeField] SpriteRenderer body;
        [SerializeField] SpriteRenderer mouth;
        [SerializeField] Sprite bodyIdle;
        [SerializeField] Sprite bodySpeaking;
        [SerializeField] Sprite mouthClosed;
        [SerializeField] Sprite mouthOpen;
        [SerializeField] float mouthCyclesPerSecond = 4.5f;
        [SerializeField] Vector3 idleScale = Vector3.one;
        [SerializeField] Vector3 speakingScale = new Vector3(1.04f, 1.04f, 1f);

        public CatState CurrentState { get; private set; } = CatState.Idle;

        void Awake()
        {
            ApplyMouth(0f);
            if (body != null && bodyIdle != null)
                body.sprite = bodyIdle;
        }

        public void SetState(CatState state)
        {
            CurrentState = state;
            transform.localScale = state == CatState.Speaking ? speakingScale : idleScale;

            if (body != null)
            {
                if (state == CatState.Speaking && bodySpeaking != null)
                    body.sprite = bodySpeaking;
                else if (bodyIdle != null)
                    body.sprite = bodyIdle;
            }

            if (state != CatState.Speaking)
                ApplyMouth(0f);
        }

        void Update()
        {
            if (CurrentState != CatState.Speaking) return;
            var t = (Mathf.Sin(Time.time * Mathf.PI * 2f * mouthCyclesPerSecond) + 1f) * 0.5f;
            ApplyMouth(t);
        }

        public void PlayTapReaction()
        {
            transform.localScale = new Vector3(idleScale.x * 1.08f, idleScale.y * 0.94f, idleScale.z);
        }

        void ApplyMouth(float open01)
        {
            if (mouth == null) return;
            mouth.enabled = open01 > 0.05f || CurrentState == CatState.Speaking;
            if (mouthClosed != null && mouthOpen != null)
                mouth.sprite = open01 > 0.45f ? mouthOpen : mouthClosed;

            // Subtle vertical stretch so the flap reads clearly.
            var y = Mathf.Lerp(0.35f, 1.05f, open01);
            mouth.transform.localScale = new Vector3(1f, y, 1f);
            // Soften overlay when nearly closed.
            var c = mouth.color;
            c.a = Mathf.Lerp(0.15f, 1f, open01);
            mouth.color = c;
        }
    }
}

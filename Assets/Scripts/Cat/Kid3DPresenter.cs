using UnityEngine;

namespace TalkingKitty.Cat
{
    /// <summary>
    /// Stylized procedural 3D toddler presenter. Implements the same
    /// Idle / Listening / Speaking contract as the 2D cat so VoiceLoop
    /// needs no changes. Mouth open/close runs while Speaking.
    /// </summary>
    public class Kid3DPresenter : MonoBehaviour, ICatPresenter
    {
        [SerializeField] Transform root;
        [SerializeField] Transform jaw;
        [SerializeField] Renderer[] tintRenderers;
        [SerializeField] Color idleColor = Color.white;
        [SerializeField] Color listeningColor = new Color(1f, 0.95f, 0.7f);
        [SerializeField] Color speakingColor = new Color(1f, 0.85f, 0.9f);
        [SerializeField] float mouthCyclesPerSecond = 4.5f;
        [SerializeField] float jawOpenDegrees = 22f;

        Vector3 _baseScale = Vector3.one;
        Quaternion _jawClosed = Quaternion.identity;
        MaterialPropertyBlock _mpb;

        public CatState CurrentState { get; private set; } = CatState.Idle;

        void Awake()
        {
            if (root == null) root = transform;
            _baseScale = root.localScale;
            if (jaw != null) _jawClosed = jaw.localRotation;
            _mpb = new MaterialPropertyBlock();
            ApplyJaw(0f);
            ApplyTint(idleColor);
        }

        public void SetState(CatState state)
        {
            CurrentState = state;
            ApplyTint(state switch
            {
                CatState.Listening => listeningColor,
                CatState.Speaking => speakingColor,
                _ => idleColor
            });

            root.localScale = state == CatState.Speaking
                ? _baseScale * 1.04f
                : _baseScale;

            if (state != CatState.Speaking)
                ApplyJaw(0f);
        }

        void Update()
        {
            if (CurrentState != CatState.Speaking) return;
            var t = (Mathf.Sin(Time.time * Mathf.PI * 2f * mouthCyclesPerSecond) + 1f) * 0.5f;
            ApplyJaw(t);
        }

        public void PlayTapReaction()
        {
            root.localScale = new Vector3(_baseScale.x * 1.08f, _baseScale.y * 0.94f, _baseScale.z * 1.08f);
        }

        void ApplyJaw(float open01)
        {
            if (jaw == null) return;
            jaw.localRotation = _jawClosed * Quaternion.Euler(jawOpenDegrees * open01, 0f, 0f);
        }

        void ApplyTint(Color c)
        {
            if (tintRenderers == null) return;
            foreach (var r in tintRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor("_Color", c);
                // URP Lit / Built-in both commonly use _BaseColor or _Color.
                _mpb.SetColor("_BaseColor", c);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}

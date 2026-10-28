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

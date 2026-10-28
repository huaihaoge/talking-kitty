namespace TalkingKitty.Cat
{
    public interface ICatPresenter
    {
        CatState CurrentState { get; }
        void SetState(CatState state);
        void PlayTapReaction();
    }
}

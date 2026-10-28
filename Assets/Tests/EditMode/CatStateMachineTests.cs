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

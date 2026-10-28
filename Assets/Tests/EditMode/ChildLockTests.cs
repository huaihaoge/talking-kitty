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
        Assert.IsTrue(c.HasPin);
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

    [Test]
    public void Hash_UsesSaltConstant()
    {
        // Same pin hashed here must match the documented salt TalkingKitty.v1
        // (verified independently against SHA256("TalkingKitty.v1" + "1234")).
        Assert.AreEqual(
            "f03e38b062a3a20042863f228a2c6ff5c481ad3ff9e85f35e2c6811ed8ca1432",
            PinHasher.Hash("1234"));
    }

    [Test]
    public void WithoutPin_NotLocked_AndQuitAllowed()
    {
        var go = new GameObject();
        var c = go.AddComponent<ChildLockController>();
        Assert.IsFalse(c.HasPin);
        Assert.IsFalse(c.IsLocked);
        Assert.IsTrue(c.ShouldAllowQuit());
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SetLocked_PersistsAcrossNewInstance()
    {
        var go = new GameObject();
        var c = go.AddComponent<ChildLockController>();
        c.SetPin("1234");
        c.SetLocked(true);
        Object.DestroyImmediate(go);

        var go2 = new GameObject();
        var c2 = go2.AddComponent<ChildLockController>();
        Assert.IsTrue(c2.IsLocked);
        Assert.IsFalse(c2.ShouldAllowQuit());
        Object.DestroyImmediate(go2);
    }

    [Test]
    public void VerifyPin_NoPin_ReturnsFalse()
    {
        var go = new GameObject();
        var c = go.AddComponent<ChildLockController>();
        Assert.IsFalse(c.VerifyPin("1234"));
        Object.DestroyImmediate(go);
    }
}

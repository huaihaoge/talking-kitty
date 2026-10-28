using NUnit.Framework;
using TalkingKitty.Voice;

public class SilenceDetectorTests
{
    VoiceLoopSettings Settings() => new VoiceLoopSettings
    {
        startThreshold = 0.05f,
        silenceSeconds = 1.0f,
        minSeconds = 0.3f,
        maxSeconds = 6f
    };

    [Test]
    public void LoudThenSilence_YieldsUtteranceReady()
    {
        var d = new SilenceDetector(Settings());
        Assert.AreEqual(SilenceDetectorResult.Capturing, d.Push(0.2f, 0.1f));
        // stay loud briefly
        d.Push(0.2f, 0.3f);
        // silence for 1.0s
        var r = SilenceDetectorResult.Capturing;
        for (int i = 0; i < 10; i++)
            r = d.Push(0.001f, 0.1f);
        Assert.AreEqual(SilenceDetectorResult.UtteranceReady, r);
    }

    [Test]
    public void ShortBlip_Discarded()
    {
        var d = new SilenceDetector(Settings());
        d.Push(0.2f, 0.05f);
        var r = SilenceDetectorResult.Capturing;
        for (int i = 0; i < 10; i++)
            r = d.Push(0.001f, 0.1f);
        Assert.AreEqual(SilenceDetectorResult.DiscardedTooShort, r);
    }

    [Test]
    public void Reset_ReturnsToIdle()
    {
        var d = new SilenceDetector(Settings());
        d.Push(0.2f, 0.1f);
        d.Reset();
        // After reset, loud audio should start a new capture from Idle.
        Assert.AreEqual(SilenceDetectorResult.Capturing, d.Push(0.2f, 0.1f));
    }

    [Test]
    public void QuietAudio_StaysIdle()
    {
        var d = new SilenceDetector(Settings());
        for (int i = 0; i < 10; i++)
            Assert.AreEqual(SilenceDetectorResult.Idle, d.Push(0.001f, 0.1f));
    }

    [Test]
    public void LongCapture_ReturnsCapturingMaxReached()
    {
        var d = new SilenceDetector(Settings());
        SilenceDetectorResult r = SilenceDetectorResult.Idle;
        // 70 pushes of 0.1s loud audio = 7s > maxSeconds (6s)
        for (int i = 0; i < 70 && r != SilenceDetectorResult.CapturingMaxReached; i++)
            r = d.Push(0.2f, 0.1f);
        Assert.AreEqual(SilenceDetectorResult.CapturingMaxReached, r);
        // After max reached, detector resets to Idle; quiet audio keeps it Idle.
        Assert.AreEqual(SilenceDetectorResult.Idle, d.Push(0.001f, 0.1f));
    }

    [Test]
    public void ReadyResult_IsTerminalForThatPush_ResetReactivates()
    {
        var d = new SilenceDetector(Settings());
        d.Push(0.2f, 0.5f);
        SilenceDetectorResult r = SilenceDetectorResult.Capturing;
        for (int i = 0; i < 10; i++)
            r = d.Push(0.001f, 0.1f);
        Assert.AreEqual(SilenceDetectorResult.UtteranceReady, r);
        // Detector is back to Idle after UtteranceReady; loud audio restarts capture.
        Assert.AreEqual(SilenceDetectorResult.Capturing, d.Push(0.2f, 0.1f));
    }
}

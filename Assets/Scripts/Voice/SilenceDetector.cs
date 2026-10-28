namespace TalkingKitty.Voice
{
    /// <summary>
    /// Tunable settings for the voice loop's silence detection and capture clamps.
    /// </summary>
    public class VoiceLoopSettings
    {
        /// <summary>Abs peak (0..1) above which we consider audio "loud" enough to capture.</summary>
        public float startThreshold = 0.05f;

        /// <summary>Seconds of sustained silence that ends an utterance.</summary>
        public float silenceSeconds = 1.0f;

        /// <summary>Minimum capture duration for an utterance to be considered valid.</summary>
        public float minSeconds = 0.3f;

        /// <summary>Maximum capture duration before the utterance is force-readied.</summary>
        public float maxSeconds = 6f;

        /// <summary>Playback pitch shift for the cat voice.</summary>
        public float pitch = 1.45f;
    }

    /// <summary>Outcome of feeding one audio frame into <see cref="SilenceDetector"/>.</summary>
    public enum SilenceDetectorResult
    {
        Idle,
        Capturing,
        UtteranceReady,
        DiscardedTooShort,
        CapturingMaxReached
    }

    /// <summary>
    /// Pure C# state machine: listens for loud audio, captures until silence,
    /// then emits an utterance-ready (or discard) result. Frame-driven via Push().
    /// </summary>
    public class SilenceDetector
    {
        private readonly VoiceLoopSettings _settings;

        private enum State
        {
            Idle,
            Capturing
        }

        private State _state = State.Idle;
        private float _captureTime;
        private float _silenceTime;

        public SilenceDetector(VoiceLoopSettings settings)
        {
            _settings = settings ?? new VoiceLoopSettings();
        }

        /// <summary>Return the detector to the Idle state, discarding any capture in progress.</summary>
        public void Reset()
        {
            _state = State.Idle;
            _captureTime = 0f;
            _silenceTime = 0f;
        }

        /// <summary>
        /// Feed one frame. <paramref name="absPeak"/> is the frame's absolute peak amplitude
        /// (0..1); <paramref name="deltaTime"/> is the elapsed time since the previous push.
        /// Returns at most one terminal result per utterance; the detector resets itself
        /// back to Idle immediately after emitting UtteranceReady, DiscardedTooShort, or
        /// CapturingMaxReached.
        /// </summary>
        public SilenceDetectorResult Push(float absPeak, float deltaTime)
        {
            if (_state == State.Idle)
            {
                if (absPeak >= _settings.startThreshold)
                {
                    _state = State.Capturing;
                    _captureTime = deltaTime;
                    _silenceTime = 0f;
                    return SilenceDetectorResult.Capturing;
                }
                return SilenceDetectorResult.Idle;
            }

            // State == Capturing
            if (absPeak >= _settings.startThreshold)
            {
                _captureTime += deltaTime;
                _silenceTime = 0f;
            }
            else
            {
                // Silence frame: only silence time accumulates; the captured
                // (loud) duration stays frozen so short blips can be discarded.
                _silenceTime += deltaTime;
            }

            if (_silenceTime >= _settings.silenceSeconds)
            {
                var result = _captureTime >= _settings.minSeconds
                    ? SilenceDetectorResult.UtteranceReady
                    : SilenceDetectorResult.DiscardedTooShort;
                Reset();
                return result;
            }

            if (_captureTime >= _settings.maxSeconds)
            {
                Reset();
                return SilenceDetectorResult.CapturingMaxReached;
            }

            return SilenceDetectorResult.Capturing;
        }
    }
}

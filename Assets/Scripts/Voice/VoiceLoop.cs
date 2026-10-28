using System;
using UnityEngine;
using TalkingKitty.Cat;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace TalkingKitty.Voice
{
    /// <summary>
    /// Auto-listen voice loop: starts the microphone in <see cref="Start"/>,
    /// feeds each frame's peak into the <see cref="SilenceDetector"/>, and when
    /// an utterance completes plays it back through <see cref="output"/> at a
    /// raised pitch while the cat shows the Speaking state. Playback blocks new
    /// captures; when playback ends the cat returns to Listening.
    /// </summary>
    public class VoiceLoop : MonoBehaviour
    {
        [Header("Detection (mirrors VoiceLoopSettings)")]
        [SerializeField] float startThreshold = 0.05f;
        [SerializeField] float silenceSeconds = 1f;
        [SerializeField] float minSeconds = 0.3f;
        [SerializeField] float maxSeconds = 6f;

        [Header("Voice")]
        [SerializeField] float pitch = 1.45f;

        [Header("Scene wiring")]
        [SerializeField] AudioSource output;
        [SerializeField] MonoBehaviour catPresenterBehaviour;

        /// <summary>Minimum playback pitch; <see cref="Pitch"/> clamps to this.</summary>
        public const float PitchMin = 0.5f;
        /// <summary>Maximum playback pitch; <see cref="Pitch"/> clamps to this.</summary>
        public const float PitchMax = 2f;

        /// <summary>
        /// Playback pitch, clamped to [PitchMin, PitchMax]. Applied on the
        /// next playback; the parent panel's slider writes here.
        /// </summary>
        public float Pitch
        {
            get => pitch;
            set => pitch = Mathf.Clamp(value, PitchMin, PitchMax);
        }

        /// <summary>Mic loop clip length. Long enough that a maxSeconds capture plus mic latency fits.</summary>
        const int MicLengthSeconds = 8;
        /// <summary>Requested mic sample rate; the device may return a different one (we use the clip's actual rate).</summary>
        const int MicFrequency = 16000;
        /// <summary>Max mic samples read per GetData call (samples, not floats).</summary>
        const int ReadChunkSamples = 2048;

        SilenceDetector _detector;
        ICatPresenter _cat;

        AudioClip _micClip;
        int _channels;
        int _lastHead;        // mic ring write head at the previous Update

        float[] _frame;        // scratch buffer for one mic read chunk

        // ---- Utterance ring buffer ----
        // Stages every mic sample read during a capture. Writes wrap at
        // _ring.Length (sized for maxSeconds of audio), so a capture longer
        // than capacity keeps only the newest samples. _ringCount tracks the
        // total written since the capture began (may exceed _ring.Length);
        // playback copies out the newest min(_ringCount, _ring.Length) samples.
        float[] _ring;
        int _ringWrite;       // next index to write
        int _ringCount;       // total samples staged since capture began

        bool _speaking;        // true while our pitched playback is running

        void Awake()
        {
            // Mirror the serialized fields into the settings object the pure
            // detector consumes (VoiceLoopSettings itself is not serializable).
            var settings = new VoiceLoopSettings
            {
                startThreshold = startThreshold,
                silenceSeconds = silenceSeconds,
                minSeconds = minSeconds,
                maxSeconds = maxSeconds,
                pitch = pitch,
            };
            _detector = new SilenceDetector(settings);

            _cat = catPresenterBehaviour as ICatPresenter;
            if (_cat == null && catPresenterBehaviour != null)
                Debug.LogWarning("[VoiceLoop] catPresenterBehaviour does not implement ICatPresenter; cat states will not update.");
        }

        void Start()
        {
#if UNITY_ANDROID
            // Request mic permission before starting capture. The request is
            // asynchronous; Unity simply records silence until it is granted.
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                Permission.RequestUserPermission(Permission.Microphone);
#endif

            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("[VoiceLoop] No microphone device found; voice loop disabled.");
                enabled = false;
                return;
            }

            _micClip = Microphone.Start(null, true, MicLengthSeconds, MicFrequency);
            if (_micClip == null)
            {
                Debug.LogError("[VoiceLoop] Microphone.Start failed; voice loop disabled.");
                enabled = false;
                return;
            }

            // The clip Unity hands back may run at a different rate/channel
            // count than requested; size all buffers from the actual clip.
            _channels = _micClip.channels;
            _ring = new float[Mathf.CeilToInt(maxSeconds * _micClip.frequency) * _channels];
            _frame = new float[ReadChunkSamples * _channels];
            ResetRing();
            _lastHead = Microphone.GetPosition(null);

            if (output == null)
                Debug.LogWarning("[VoiceLoop] No AudioSource assigned; captures will be discarded.");

            _cat?.SetState(CatState.Listening);
        }

        void Update()
        {
            if (!enabled || _micClip == null || _detector == null) return;

            // Playback-end poll: when our pitched echo finishes, stop "speaking",
            // drop any half-formed capture, and go back to listening.
            if (_speaking && output != null && !output.isPlaying)
            {
                _speaking = false;
                _detector.Reset();
                ResetRing();
                _cat?.SetState(CatState.Listening);
            }

            // While the cat is talking back, do not start a new capture:
            // skip feeding the detector, but keep the mic head current so we
            // do not drain a stale backlog on resume.
            if (output != null && output.isPlaying)
            {
                _lastHead = Microphone.GetPosition(null);
                return;
            }

            int length = _micClip.samples;
            int head = Microphone.GetPosition(null);
            int count = head - _lastHead;
            if (count < 0) count += length;            // mic ring wrapped
            if (count == 0) return;
            if (count >= length) count = length - 1;   // clamp huge backlog

            // Copy the newly written mic samples, measuring the frame peak and
            // staging every sample into the utterance ring (rolling overwrite).
            float absPeak = 0f;
            int pos = _lastHead;
            int remaining = count;
            while (remaining > 0)
            {
                int chunk = Mathf.Min(remaining, ReadChunkSamples, length - pos);
                _micClip.GetData(_frame, pos);
                int floats = chunk * _channels;
                for (int i = 0; i < floats; i++)
                {
                    float s = _frame[i];
                    float a = s < 0f ? -s : s;
                    if (a > absPeak) absPeak = a;
                    _ring[_ringWrite] = s;
                    _ringWrite = (_ringWrite + 1) % _ring.Length;
                    _ringCount++;
                }
                pos = (pos + chunk) % length;
                remaining -= chunk;
            }
            _lastHead = head;

            var result = _detector.Push(absPeak, Time.deltaTime);
            switch (result)
            {
                case SilenceDetectorResult.Idle:
                    // Nothing captured; the staged frame was silence, discard it.
                    ResetRing();
                    break;

                case SilenceDetectorResult.Capturing:
                    // Keep staging samples; capture continues.
                    break;

                case SilenceDetectorResult.UtteranceReady:
                case SilenceDetectorResult.CapturingMaxReached:
                    PlayUtterance();
                    break;

                case SilenceDetectorResult.DiscardedTooShort:
                    ResetRing();
                    break;
            }
        }

        /// <summary>Copy the captured ring segment into a fresh clip and play it pitched.</summary>
        void PlayUtterance()
        {
            int capacity = _ring.Length;
            int valid = Mathf.Min(_ringCount, capacity);
            if (valid <= 0 || output == null)
            {
                ResetRing();
                return;
            }

            // The oldest `valid` staged samples start this many slots behind the
            // write head (wrapping around the ring if the capture overflowed it).
            var data = new float[valid];
            int start = (_ringWrite - valid + capacity) % capacity;
            int first = Mathf.Min(valid, capacity - start);
            Array.Copy(_ring, start, data, 0, first);
            if (valid > first) Array.Copy(_ring, 0, data, first, valid - first);

            var clip = AudioClip.Create(
                "KittyUtterance",
                valid / _channels,
                _channels,
                _micClip.frequency,
                false);
            clip.SetData(data, 0);
            ResetRing();

            output.pitch = pitch;
            output.clip = clip;
            output.Play();

            _speaking = true;
            _detector.Reset();
            _cat?.SetState(CatState.Speaking);
        }

        void ResetRing()
        {
            _ringWrite = 0;
            _ringCount = 0;
        }
    }
}

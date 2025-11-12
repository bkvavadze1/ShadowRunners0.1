using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

[DefaultExecutionOrder(-50)]
public class AmbienceManager : MonoBehaviour
{
    public static AmbienceManager Instance { get; private set; }

    [Header("Crowd Bed")]
    [Tooltip("Looping 2D bed. e.g., crowd_loop_01")]
    public AudioClip crowdLoop;
    [Range(0f, 1f)] public float crowdVolume = 0.35f;

    [Header("Bell One-Shots")]
    [Tooltip("Random one-shots picked at intervals.")]
    public AudioClip[] bellClips;
    [Range(0f, 1f)] public float bellVolume = 0.85f;
    [Tooltip("Seconds (real time) between bell rolls.")]
    public Vector2 bellIntervalSeconds = new Vector2(22f, 45f);
    [Tooltip("Chance (0-1) to play a bell when the interval elapses.")]
    [Range(0f, 1f)] public float bellChance = 0.8f;

    [Header("Mixer (Optional)")]
    public AudioMixerGroup mixerOutput;

    [Header("Behaviour")]
    [Tooltip("If true, will auto-start when audio is unpaused (e.g. after IntroPanel).")]
    public bool startWhenAudioUnpaused = true;
    [Tooltip("Seconds (real time) to delay first bell after bed starts.")]
    public float firstBellDelay = 5f;

    [Header("Advanced")]
    [Tooltip("Persist across scene loads.")]
    public bool dontDestroyOnLoad = true;

    AudioSource _bedSrc;     // looping bed
    AudioSource _oneShotSrc; // bell one-shots
    Coroutine _bellRoutine;
    bool _started;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        // Create / configure sources
        _bedSrc = gameObject.AddComponent<AudioSource>();
        _bedSrc.playOnAwake = false;
        _bedSrc.loop = true;
        _bedSrc.spatialBlend = 0f;              // 2D
        _bedSrc.ignoreListenerPause = false;    // obey IntroPanel + pauses
        _bedSrc.outputAudioMixerGroup = mixerOutput;

        _oneShotSrc = gameObject.AddComponent<AudioSource>();
        _oneShotSrc.playOnAwake = false;
        _oneShotSrc.loop = false;
        _oneShotSrc.spatialBlend = 0f;          // 2D
        _oneShotSrc.ignoreListenerPause = false;
        _oneShotSrc.outputAudioMixerGroup = mixerOutput;

        // assign clips/volumes
        _bedSrc.clip = crowdLoop;
        _bedSrc.volume = crowdVolume;
        _oneShotSrc.volume = bellVolume;
    }

    void OnEnable()
    {
        if (startWhenAudioUnpaused) StartCoroutine(CoAutoStartWhenReady());
    }

    IEnumerator CoAutoStartWhenReady()
    {
        if (_started) yield break;

        // Wait for AudioListener to be unpaused (IntroPanel pauses AudioListener + timeScale)
        yield return new WaitWhile(() => AudioListener.pause);

        StartAmbience();

        // Small delay before first bell (real time, unaffected by timeScale)
        if (firstBellDelay > 0f)
            yield return new WaitForSecondsRealtime(firstBellDelay);

        if (_bellRoutine == null)
            _bellRoutine = StartCoroutine(CoBellLoop());
    }

    public void StartAmbience()
    {
        if (_started) return;
        _started = true;

        if (_bedSrc != null && _bedSrc.clip != null && !_bedSrc.isPlaying)
            _bedSrc.Play();
    }

    public void StopAmbience()
    {
        _started = false;
        if (_bellRoutine != null) { StopCoroutine(_bellRoutine); _bellRoutine = null; }
        if (_bedSrc != null && _bedSrc.isPlaying) _bedSrc.Stop();
    }

    IEnumerator CoBellLoop()
    {
        // Use real-time waits so it keeps proper cadence during gameplay pauses
        while (true)
        {
            float wait = Mathf.Max(0.5f, Random.Range(bellIntervalSeconds.x, bellIntervalSeconds.y));
            yield return new WaitForSecondsRealtime(wait);

            // If listener is paused (Intro/game pause), skip until it resumes
            if (AudioListener.pause) continue;

            if (bellClips != null && bellClips.Length > 0 && Random.value <= bellChance)
            {
                var clip = bellClips[Random.Range(0, bellClips.Length)];
                if (clip != null) _oneShotSrc.PlayOneShot(clip, bellVolume);
            }
        }
    }

    // Convenience for other systems (optional)
    public void SetMixerGroup(AudioMixerGroup group)
    {
        mixerOutput = group;
        if (_bedSrc) _bedSrc.outputAudioMixerGroup = mixerOutput;
        if (_oneShotSrc) _oneShotSrc.outputAudioMixerGroup = mixerOutput;
    }
}

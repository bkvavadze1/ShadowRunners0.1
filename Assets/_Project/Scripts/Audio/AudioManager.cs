using System.Collections;
using UnityEngine;

namespace ShadowRunners.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Default Music")]
        public AudioClip defaultMusic;
        [Range(0f, 1f)] public float musicVolume = 0.45f;
        public float defaultFade = 0.75f;

        AudioSource _a, _b;   // two sources for crossfades
        AudioSource Active => _a.isPlaying ? _a : _b;
        AudioSource Idle => _a.isPlaying ? _b : _a;
        Coroutine _fadeCo;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { _a, _b })
            {
                s.playOnAwake = false;
                s.loop = true;
                s.spatialBlend = 0f; // 2D
                s.volume = 0f;
            }
        }

        void Start()
        {
            if (defaultMusic) PlayMusic(defaultMusic, defaultFade);
        }

        public void PlayMusic(AudioClip clip, float fade = -1f)
        {
            if (!clip) return;
            if (Active.clip == clip) return;
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeTo(clip, fade < 0 ? defaultFade : fade));
        }

        public void StopMusic(float fade = -1f)
        {
            if (_fadeCo != null) StopCoroutine(_fadeCo);
            _fadeCo = StartCoroutine(FadeTo(null, fade < 0 ? defaultFade : fade));
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            // Update active immediately
            if (_a.isPlaying) _a.volume = musicVolume;
            if (_b.isPlaying) _b.volume = musicVolume;
        }

        IEnumerator FadeTo(AudioClip next, float dur)
        {
            var from = Active;
            var to = Idle;

            if (next == null)
            {
                // Fade out current and stop
                float t = 0f, start = from.volume;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    from.volume = Mathf.Lerp(start, 0f, t / dur);
                    yield return null;
                }
                from.Stop();
                yield break;
            }

            // Prepare target source
            to.clip = next;
            to.volume = 0f;
            to.Play();

            float time = 0f;
            float startFrom = from.isPlaying ? from.volume : 0f;
            while (time < dur)
            {
                time += Time.unscaledDeltaTime; // unaffected by pause
                float k = time / dur;
                if (from.isPlaying) from.volume = Mathf.Lerp(startFrom, 0f, k);
                to.volume = Mathf.Lerp(0f, musicVolume, k);
                yield return null;
            }

            if (from.isPlaying) from.Stop();
            to.volume = musicVolume;
        }
    }
}

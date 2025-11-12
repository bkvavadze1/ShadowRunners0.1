using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowRunners.Audio
{
    /// <summary>
    /// Lightweight pooled one-shot audio player.
    /// Use: AudioOneShotPool.Play(clip, pos, volume, spatialBlend, minDist, maxDist);
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class AudioOneShotPool : MonoBehaviour
    {
        static AudioOneShotPool _instance;
        public static AudioOneShotPool Instance
        {
            get
            {
                if (_instance) return _instance;
                var go = new GameObject("[AudioOneShotPool]");
                _instance = go.AddComponent<AudioOneShotPool>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        [Header("Pool")]
        [Tooltip("How many AudioSources to keep ready.")]
        public int prewarm = 8;

        readonly Queue<AudioSource> _pool = new Queue<AudioSource>();
        bool _initialized;

        void Awake()
        {
            if (_instance && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitIfNeeded();
        }

        void InitIfNeeded()
        {
            if (_initialized) return;
            _initialized = true;
            for (int i = 0; i < prewarm; i++)
                _pool.Enqueue(MakeSource());
        }

        AudioSource MakeSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            go.SetActive(false);
            return src;
        }

        AudioSource Get()
        {
            InitIfNeeded();
            return _pool.Count > 0 ? _pool.Dequeue() : MakeSource();
        }

        void Release(AudioSource src)
        {
            if (!src) return;
            src.clip = null;
            src.gameObject.SetActive(false);
            src.transform.SetParent(transform, false);
            _pool.Enqueue(src);
        }

        public static void Play(AudioClip clip, Vector3 worldPos, float volume, float spatialBlend, float minDistance, float maxDistance)
        {
            if (!clip) return;
            var p = Instance;
            var src = p.Get();
            var go = src.gameObject;

            go.transform.SetParent(null, true);
            go.transform.position = worldPos;

            src.clip = clip;
            src.volume = Mathf.Clamp01(volume);
            src.spatialBlend = Mathf.Clamp01(spatialBlend);
            src.minDistance = Mathf.Max(0.01f, minDistance);
            src.maxDistance = Mathf.Max(src.minDistance, maxDistance);
            go.SetActive(true);
            src.Play();

            p.StartCoroutine(p.CoReturnAfter(src, clip.length + 0.05f));
        }

        IEnumerator CoReturnAfter(AudioSource src, float t)
        {
            yield return new WaitForSeconds(t);
            Release(src);
        }
    }
}

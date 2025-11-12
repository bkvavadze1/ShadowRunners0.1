using UnityEngine;

namespace ShadowRunners.Audio
{
    /// <summary>
    /// Plays a one-shot crash/impact when the game enters GameOver.
    /// Put it anywhere in the scene (e.g., Canvas_HUD or an Audio root).
    /// </summary>
    [AddComponentMenu("ShadowRunners/Audio/Game Over Crash SFX")]
    public class GameOverCrashSFX : MonoBehaviour
    {
        public AudioClip crashClip;
        [Range(0f, 1f)] public float volume = 0.9f;
        [Tooltip("2D recommended; set to >0 for positional crash.")]
        [Range(0f, 1f)] public float spatialBlend = 0f;

        bool _playedThisDeath;

        void OnEnable()
        {
            _playedThisDeath = false;
        }

        void Update()
        {
            var gm = ShadowRunners.Gameplay.GameManager.Instance;
            if (!gm) return;

            if (gm.State == ShadowRunners.Gameplay.GameManager.GameState.GameOver)
            {
                if (!_playedThisDeath)
                {
                    _playedThisDeath = true;
                    Play();
                }
            }
            else
            {
                // reset when leaving game over
                _playedThisDeath = false;
            }
        }

        void Play()
        {
            if (!crashClip) return;
            var go = new GameObject("SFX_Crash");
            go.transform.position = Vector3.zero;
            var src = go.AddComponent<AudioSource>();
            src.clip = crashClip;
            src.volume = volume;
            src.spatialBlend = spatialBlend;
            src.dopplerLevel = 0f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 5f;
            src.maxDistance = 25f;
            src.Play();
            Destroy(go, crashClip.length + 0.1f);
        }
    }
}

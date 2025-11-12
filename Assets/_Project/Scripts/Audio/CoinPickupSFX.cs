using UnityEngine;

namespace ShadowRunners.Audio
{
    /// <summary>
    /// Plays a one-shot when the coin is collected.
    /// - Triggers on player enter, OR if the coin is disabled near the player (magnet).
    /// - Uses AudioOneShotPool instead of spawning/destroying GameObjects.
    /// </summary>
    [AddComponentMenu("ShadowRunners/Audio/Coin Pickup SFX")]
    public class CoinPickupSFX : MonoBehaviour
    {
        [Header("Sound")]
        public AudioClip sfx;
        [Range(0f, 1f)] public float volume = 0.9f;

        [Header("3D Settings (0 = 2D)")]
        [Range(0f, 1f)] public float spatialBlend = 0.2f;
        public float minDistance = 5f;
        public float maxDistance = 20f;

        [Header("Magnet/Pooling Guard")]
        [Tooltip("If the coin is disabled within this radius of the player and no SFX has played yet, play it.")]
        public float playOnDisableNearPlayerRadius = 6f;
        [Tooltip("Ignore disables that happen immediately on spawn (pooling).")]
        public float minLifetimeBeforeDisableS = 0.03f;

        bool _played;
        float _spawnT;

        void OnEnable()
        {
            _spawnT = Time.unscaledTime;
            _played = false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_played || !sfx) return;
            if (other && other.CompareTag("Player"))
                PlayAt(transform.position);
        }

        void OnDisable()
        {
            if (_played || !sfx) return;

            // If we were disabled very quickly, likely pooled despawn — skip.
            if (Time.unscaledTime - _spawnT < minLifetimeBeforeDisableS) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (!player) return;

            float d = Vector3.Distance(player.transform.position, transform.position);
            if (d <= playOnDisableNearPlayerRadius)
                PlayAt(transform.position);
        }

        /// <summary>Call from other scripts/UnityEvents if you have a collection hook.</summary>
        public void PlaySFX()
        {
            if (!_played && sfx) PlayAt(transform.position);
        }

        void PlayAt(Vector3 pos)
        {
            _played = true;
            AudioOneShotPool.Play(sfx, pos, volume, spatialBlend, minDistance, maxDistance);
        }
    }
}

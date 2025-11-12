using UnityEngine;

namespace ShadowRunners.Gameplay
{
    /// Attached to spawned coin instances; reports collection for a given runId.
    [AddComponentMenu("ShadowRunners/Gameplay/Coin Run Id Tag")]
    public class CoinRunIdTag : MonoBehaviour
    {
        public int runId = -1;

        [Header("Disable/Magnet Guard")]
        public float playOnDisableNearPlayerRadius = 6f;
        public float minLifetimeBeforeDisableS = 0.03f;

        bool _reported;
        float _spawnT;

        void OnEnable() { _spawnT = Time.unscaledTime; _reported = false; }

        void OnTriggerEnter(Collider other)
        {
            if (_reported) return;
            if (other && other.CompareTag("Player"))
                Report();
        }

        void OnDisable()
        {
            if (_reported) return;

            if (Time.unscaledTime - _spawnT < minLifetimeBeforeDisableS) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (!player) return;

            float d = Vector3.Distance(player.transform.position, transform.position);
            if (d <= playOnDisableNearPlayerRadius)
                Report();
        }

        void Report()
        {
            _reported = true;
            CoinRunCollectionEvents.ReportCollected(runId);
        }
    }
}

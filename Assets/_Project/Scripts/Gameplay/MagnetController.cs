using UnityEngine;
using ShadowRunners.Systems;

namespace ShadowRunners.Gameplay
{
    public class MagnetController : MonoBehaviour
    {
        public float magnetRadius = 3.0f;
        public float magnetForce = 20f;

        private float _timer;

        public void ActivateMagnet(float duration)
        {
            _timer = Mathf.Max(_timer, duration);
            if (StatusPanel.Instance) StatusPanel.Instance.SetLine("Magnet", $"Magnet {Mathf.CeilToInt(_timer)}s");
        }

        void Update()
        {
            if (_timer > 0f)
            {
                _timer -= Time.unscaledDeltaTime;
                if (StatusPanel.Instance)
                {
                    if (_timer > 0f) StatusPanel.Instance.SetLine("Magnet", $"Magnet {Mathf.CeilToInt(_timer)}s");
                    else StatusPanel.Instance.SetLine("Magnet", string.Empty);
                }
            }

            if (_timer <= 0f) return;

            // Attract nearby coins
            var hits = Physics.OverlapSphere(transform.position, magnetRadius, ~0, QueryTriggerInteraction.Collide);
            foreach (var h in hits)
            {
                if (!h || !h.CompareTag("Coin")) continue;
                var rb = h.attachedRigidbody;
                Vector3 dir = (transform.position - h.transform.position).normalized;
                if (rb) rb.AddForce(dir * magnetForce, ForceMode.Acceleration);
                else h.transform.position += dir * (magnetForce * Time.deltaTime);
            }
        }
    }
}

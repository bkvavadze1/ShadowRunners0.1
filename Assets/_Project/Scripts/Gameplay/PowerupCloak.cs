using UnityEngine;

namespace ShadowRunners.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public class PowerupCloak : MonoBehaviour
    {
        public float duration = 6f;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            var rb = GetComponent<Rigidbody>();
            if (!rb) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            var motor = other.GetComponent<RunnerMotor>();
            if (!motor) return;

            var p = other.GetComponent<PowerupController>();
            if (!p) p = other.gameObject.AddComponent<PowerupController>();

            Debug.Log("[PowerupCloak] Activating Cloak");
            p.ActivateCloak(duration);
            Destroy(gameObject);
        }
    }
}

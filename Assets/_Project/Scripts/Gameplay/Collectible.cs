using UnityEngine;

namespace ShadowRunners.Gameplay
{
    /// <summary>
    /// Coin pickup with proper child Visual animation:
    /// - Spins + bobs the Visual in LOCAL space (no world-space distortion)
    /// - Root transform moves toward runner when magnet is active
    /// - Root holds Collider + script; Visual holds renderers only
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        [Header("Value")]
        public int value = 1;

        [Header("Visual (child)")]
        [Tooltip("Child transform that contains only renderers. If empty, will try to find a child named 'Visual', else use self.")]
        public Transform visual;

        [Header("Spin + Bob (applies to Visual LOCAL transform)")]
        public bool spin = true;
        public float spinSpeedY = 180f;        // deg/sec around local Y
        public bool bob = true;
        public float bobAmplitude = 0.08f;     // meters
        public float bobFrequency = 2f;        // Hz
        private Vector3 _baseLocalPos;
        private Quaternion _baseLocalRot;

        [Header("Magnet Attract (moves ROOT)")]
        public bool isAttracted;
        public float attractSpeed = 18f;       // m/s toward runner
        public float pickupDistance = 0.5f;    // collect radius from runner

        private Transform _target;             // runner
        private Collider _col;
        private bool _collected;
        private float _bobTime;

        void Reset()
        {
            var c = GetComponent<Collider>();
            if (c) c.isTrigger = true;
        }

        void Awake()
        {
            _col = GetComponent<Collider>();
            _col.isTrigger = true;

            if (!visual)
            {
                var v = transform.Find("Visual");
                visual = v ? v : transform;
            }

            _baseLocalPos = visual.localPosition;
            _baseLocalRot = visual.localRotation;
            _bobTime = Random.Range(0f, 10f); // small phase offset so groups aren’t in sync
        }

        void OnEnable()
        {
            // Re-bake base pose on enable (handles pooled objects)
            if (!visual)
            {
                var v = transform.Find("Visual");
                visual = v ? v : transform;
            }
            _baseLocalPos = visual.localPosition;
            _baseLocalRot = visual.localRotation;
            _bobTime = Random.Range(0f, 10f);
            _collected = false;
            isAttracted = false;
            _target = null;
        }

        void Update()
        {
            if (!_collected && visual)
            {
                // LOCAL spin (no shear from parent motion)
                if (spin)
                {
                    visual.localRotation *= Quaternion.Euler(0f, spinSpeedY * Time.deltaTime, 0f);
                }

                // LOCAL bob around base pose
                if (bob && bobAmplitude > 0f && bobFrequency > 0f)
                {
                    _bobTime += Time.deltaTime;
                    float y = Mathf.Sin(_bobTime * Mathf.PI * 2f * bobFrequency) * bobAmplitude;
                    visual.localPosition = _baseLocalPos + new Vector3(0f, y, 0f);
                }
            }

            // Magnet attraction moves the ROOT in world space (do NOT touch visual world pos)
            if (isAttracted && !_collected && _target)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    _target.position,
                    attractSpeed * Time.deltaTime
                );

                // Close enough? Collect
                if ((transform.position - _target.position).sqrMagnitude <= pickupDistance * pickupDistance)
                    CollectNow();
            }
        }

        /// <summary>Called by MagnetController to begin attraction.</summary>
        public void BeginAttract(Transform runner)
        {
            if (_collected) return;
            _target = runner;
            isAttracted = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (_collected) return;

            // Collected by touching the runner (with or without magnet)
            var motor = other.GetComponent<RunnerMotor>();
            if (motor != null)
                CollectNow();
        }

        void CollectNow()
        {
            if (_collected) return;
            _collected = true;

            // Optional: small pop on visual (if you later add such a script)
            // var pop = visual ? visual.GetComponent<PickupPop>() : null;
            // if (pop) pop.PlayPop();

            var score = Systems.ScoreSystem.Instance;
            if (score != null) score.AddCoins(value);

            Destroy(gameObject);
        }
    }
}

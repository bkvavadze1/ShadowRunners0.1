using UnityEngine;

namespace ShadowRunners.Gameplay
{
    /// <summary>
    /// Gentle up/down floating motion for a pickup Visual (no spin).
    /// Put this on the Visual child (renderers only). The pickup root holds collider/scripts.
    /// </summary>
    public class PickupFloat : MonoBehaviour
    {
        public float amplitude = 0.10f;    // meters
        public float frequency = 1.4f;     // Hz
        [Tooltip("Randomize initial phase so instances don't move in sync.")]
        public bool randomizePhase = true;

        private Vector3 _baseLocalPos;
        private float _t;

        void Awake()
        {
            _baseLocalPos = transform.localPosition;
            _t = randomizePhase ? Random.Range(0f, 10f) : 0f;
        }

        void OnEnable()
        {
            _baseLocalPos = transform.localPosition;
            if (randomizePhase) _t = Random.Range(0f, 10f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            float y = Mathf.Sin(_t * Mathf.PI * 2f * frequency) * amplitude;
            transform.localPosition = _baseLocalPos + new Vector3(0f, y, 0f);
        }
    }
}

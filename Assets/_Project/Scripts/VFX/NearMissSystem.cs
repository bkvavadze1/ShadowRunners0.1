using UnityEngine;
using ShadowRunners.Gameplay;     // RunnerMotor, ComboSystem
using ShadowRunners.Systems;      // StatusPanel (optional)

namespace ShadowRunners.Gameplay
{
    /// Detects close calls with obstacles (no hit) and fires a near-miss event:
    /// - Boosts ComboSystem (Flow)
    /// - Optional SFX and VFX
    /// Attach to the Runner (same GO as RunnerMotor).
    [AddComponentMenu("ShadowRunners/Gameplay/Near Miss System")]
    public class NearMissSystem : MonoBehaviour
    {
        [Header("Detection")]
        [Tooltip("Physics Layer for obstacles (must match your Obstacle layer).")]
        public string obstacleLayerName = "Obstacle";
        [Tooltip("Forward distance (m) to watch in front of the runner.")]
        public float forwardWindow = 1.6f;
        [Tooltip("Side distance (m) from runner center considered a near-miss (each side).")]
        public float lateralWindow = 0.9f;
        [Tooltip("Vertical window relative to runner Y (meters above/below).")]
        public float verticalWindow = 1.2f;
        [Tooltip("Minimum speed (m/s) to consider near-miss.")]
        public float minSpeed = 2.0f;
        [Tooltip("Cooldown between near-miss triggers (seconds).")]
        public float cooldown = 0.45f;

        [Header("Feedback")]
        [Tooltip("Optional: spawn this VFX at the closest point of the near-miss.")]
        public GameObject vfxPrefab;
        [Tooltip("Auto-destroy VFX after seconds (if spawned).")]
        public float vfxLifetime = 0.6f;

        [Tooltip("Optional: play this SFX on near-miss.")]
        public AudioClip sfxWhoosh;
        [Range(0f, 1f)] public float sfxVolume = 0.9f;
        [Range(0f, 1f)] public float sfxSpatialBlend = 0.2f;

        [Header("Flow Boost")]
        [Tooltip("Uses ComboSystem.AddNearMissBoost() if available.")]
        public float flowBoostPercent = 35f; // informational; ComboSystem owns its own value

        RunnerMotor _motor;
        CharacterController _cc;
        int _obstacleMask;
        float _lastTime;
        Vector3 _prevPos;

        void Awake()
        {
            _motor = GetComponent<RunnerMotor>();
            _cc = GetComponent<CharacterController>();
            _prevPos = transform.position;

            int layer = LayerMask.NameToLayer(obstacleLayerName);
            _obstacleMask = (layer >= 0) ? (1 << layer) : ~0; // fallback all if not found
            if (layer < 0)
                Debug.LogWarning($"[NearMissSystem] Layer '{obstacleLayerName}' not found. Check Project Settings → Tags & Layers.");
        }

        void LateUpdate()
        {
            // require min speed
            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            float speed = Vector3.Distance(transform.position, _prevPos) / dt;
            _prevPos = transform.position;
            if (speed < minSpeed) return;

            // cooldown
            if (Time.time < _lastTime + cooldown) return;

            // Box check ahead: small volume in front of runner
            Vector3 fwd = transform.forward;
            Vector3 center = transform.position + Vector3.up * (_cc ? _cc.center.y : 1f);
            Vector3 halfExtents = new Vector3(lateralWindow, verticalWindow * 0.5f, forwardWindow * 0.5f);

            // Shift the box slightly forward so the near-miss is truly *ahead*
            Vector3 boxCenter = center + fwd * (forwardWindow * 0.6f);

            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, transform.rotation, _obstacleMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return;

            // Find the closest valid scrape
            bool found = false;
            Collider best = null;
            float bestDist = 999f;

            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (!c || !c.gameObject.activeInHierarchy) continue;

                // Reject if it’s actually behind us (dot < 0)
                Vector3 to = c.bounds.ClosestPoint(center) - center;
                if (Vector3.Dot(fwd, to) < -0.01f) continue;

                float dSide = Mathf.Abs(Vector3.Dot(to, transform.right));
                float dUp = Mathf.Abs(Vector3.Dot(to, Vector3.up));
                float dFwd = Mathf.Abs(Vector3.Dot(to, fwd));

                // must be *near* on side and within windows
                if (dSide <= lateralWindow && dUp <= verticalWindow && dFwd <= forwardWindow)
                {
                    float d = to.magnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        best = c;
                        found = true;
                    }
                }
            }

            if (!found) return;

            _lastTime = Time.time;
            TriggerNearMiss(best, center);
        }

        void TriggerNearMiss(Collider nearCol, Vector3 center)
        {
            // Flow boost via ComboSystem if present
            var combo = ComboSystem.Instance;
            if (combo != null)
            {
                combo.AddNearMissBoost(); // uses ComboSystem.nearMissBoost internally

                // Optional status line (no 'Meter' usage)
                if (StatusPanel.Instance)
                    StatusPanel.Instance.SetLine("Streak", $"Streak x{combo.Multiplier:0.##}");
            }

            // VFX at closest point
            if (vfxPrefab && nearCol)
            {
                Vector3 pos = nearCol.ClosestPoint(center);
                var vfx = Instantiate(vfxPrefab, pos, Quaternion.identity);
                Destroy(vfx, vfxLifetime);
            }

            // SFX
            if (sfxWhoosh)
            {
                var go = new GameObject("SFX_NearMiss");
                go.transform.position = transform.position;
                var src = go.AddComponent<AudioSource>();
                src.clip = sfxWhoosh;
                src.volume = sfxVolume;
                src.spatialBlend = sfxSpatialBlend;
                src.rolloffMode = AudioRolloffMode.Linear;
                src.dopplerLevel = 0f;
                src.Play();
                Destroy(go, sfxWhoosh.length + 0.05f);
            }
        }

        // debug gizmo
        void OnDrawGizmosSelected()
        {
            if (!enabled) return;
            Vector3 fwd = transform.forward;
            Vector3 center = transform.position + Vector3.up * 1f;
            Vector3 halfExtents = new Vector3(lateralWindow, verticalWindow * 0.5f, forwardWindow * 0.5f);
            Vector3 boxCenter = center + fwd * (forwardWindow * 0.6f);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(boxCenter, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        }
    }
}

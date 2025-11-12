using UnityEngine;
using ShadowRunners.Systems;

namespace ShadowRunners.Gameplay
{
    /// <summary>
    /// Cloak/Dash (ignore collisions) + Shield (single charge). Writes stacked status lines via StatusPanel.
    /// </summary>
    public class PowerupController : MonoBehaviour
    {
        [Header("Layer Settings")]
        public string obstacleLayerName = "Obstacle";
        public bool debugLogs = false;

        [Header("Shield (single charge)")]
        public bool shieldActive { get; private set; }
        public float shieldDuration = 8f;
        public int shieldChargesDefault = 1;
        private float _shieldTimer; private int _shieldCharges;

        [Header("Dash/Boost")]
        public bool dashActive { get; private set; }
        public float dashDuration = 4f;
        public float dashSpeedMultiplier = 2.2f;
        public float dashLaneChangeMultiplier = 1.6f;
        private float _dashTimer;

        [Header("Cloak")]
        public bool cloakActive { get; private set; }
        public float cloakDuration = 6f;
        private float _cloakTimer;

        [Header("Timing")]
        public float activationGraceSeconds = 0.2f;
        private float _lastActivationUnscaledTime;

        public bool IgnoreCollisions => dashActive || cloakActive;
        public bool DashSmash => dashActive;
        public bool ShieldReady => shieldActive && _shieldCharges > 0;
        public bool RecentlyActivated(float window) => (Time.unscaledTime - _lastActivationUnscaledTime) < window;

        public float CurrentSpeedMultiplier => dashActive ? dashSpeedMultiplier : 1f;
        public float CurrentLaneChangeMultiplier => dashActive ? dashLaneChangeMultiplier : 1f;

        private int _playerLayer; private int _obstacleLayer = -1; private bool _ignored;

        void Awake()
        {
            _playerLayer = gameObject.layer;
            _obstacleLayer = LayerMask.NameToLayer(obstacleLayerName);
            if (_obstacleLayer < 0) Debug.LogWarning($"[PowerupController] Layer '{obstacleLayerName}' not found.");
            ApplyIgnore(false);
        }

        void OnDisable()
        {
            ApplyIgnore(false);
            if (StatusPanel.Instance)
            {
                StatusPanel.Instance.SetLine("Dash", "");
                StatusPanel.Instance.SetLine("Cloak", "");
                StatusPanel.Instance.SetLine("Shield", "");
            }
        }

        void Update()
        {
            bool changed = false;

            if (shieldActive)
            {
                _shieldTimer -= Time.unscaledDeltaTime;
                if (_shieldTimer <= 0f)
                {
                    shieldActive = false; changed = true;
                    if (debugLogs) Debug.Log("[PowerupController] Shield expired");
                }
                if (StatusPanel.Instance)
                    StatusPanel.Instance.SetLine("Shield", shieldActive ? $"Shield {_shieldCharges}" : "");
            }

            if (dashActive)
            {
                _dashTimer -= Time.unscaledDeltaTime;
                if (_dashTimer <= 0f)
                {
                    dashActive = false; changed = true;
                    if (debugLogs) Debug.Log("[PowerupController] Dash OFF");
                }
                if (StatusPanel.Instance)
                    StatusPanel.Instance.SetLine("Dash", dashActive ? $"Dash {Mathf.Max(0, _dashTimer):0}s" : "");
            }

            if (cloakActive)
            {
                _cloakTimer -= Time.unscaledDeltaTime;
                if (_cloakTimer <= 0f)
                {
                    cloakActive = false; changed = true;
                    if (debugLogs) Debug.Log("[PowerupController] Cloak OFF");
                }
                if (StatusPanel.Instance)
                    StatusPanel.Instance.SetLine("Cloak", cloakActive ? $"Cloak {Mathf.Max(0, _cloakTimer):0}s" : "");
            }

            if (changed) ApplyIgnore(IgnoreCollisions);
        }

        // --- Public API ---
        public void ActivateShield(float requested = -1f, int charges = -1)
        {
            shieldActive = true;
            _shieldTimer = ResolveDuration(requested, shieldDuration, 8f);
            _shieldCharges = (charges > 0) ? charges : Mathf.Max(1, shieldChargesDefault);
            _lastActivationUnscaledTime = Time.unscaledTime;
            StatusPanel.Instance?.SetLine("Shield", $"Shield {_shieldCharges}");
            if (debugLogs) Debug.Log($"[PowerupController] Shield ON (charges={_shieldCharges}, dur={_shieldTimer:0.0}s)");
        }

        public void ActivateDash(float requested = -1f)
        {
            dashActive = true;
            _dashTimer = ResolveDuration(requested, dashDuration, 4f);
            _lastActivationUnscaledTime = Time.unscaledTime;
            ApplyIgnore(true);
            StatusPanel.Instance?.SetLine("Dash", $"Dash {_dashTimer:0}s");
            if (debugLogs) Debug.Log($"[PowerupController] Dash ON (dur={_dashTimer:0.0}s)");
        }

        public void ActivateCloak(float requested = -1f)
        {
            cloakActive = true;
            _cloakTimer = ResolveDuration(requested, cloakDuration, 6f);
            _lastActivationUnscaledTime = Time.unscaledTime;
            ApplyIgnore(true);
            StatusPanel.Instance?.SetLine("Cloak", $"Cloak {_cloakTimer:0}s");
            if (debugLogs) Debug.Log($"[PowerupController] Cloak ON (dur={_cloakTimer:0.0}s)");
        }

        /// Call from RunnerMotor on obstacle hit; returns true if the hit is absorbed.
        public bool TryAbsorbShieldHit()
        {
            bool available = ShieldReady || RecentlyActivated(activationGraceSeconds);
            if (!available) return false;

            if (_shieldCharges > 0) _shieldCharges--;
            if (_shieldCharges <= 0)
            {
                shieldActive = false;
                if (debugLogs) Debug.Log("[PowerupController] Shield charge used (now 0) → Shield OFF");
                StatusPanel.Instance?.SetLine("Shield", "");
            }
            else
            {
                StatusPanel.Instance?.SetLine("Shield", $"Shield {_shieldCharges}");
                if (debugLogs) Debug.Log($"[PowerupController] Shield charge used → remaining={_shieldCharges}");
            }
            return true;
        }

        // --- Internals ---
        float ResolveDuration(float requested, float fieldValue, float fallback)
        {
            float d = (requested > 0f) ? requested : (fieldValue > 0f ? fieldValue : fallback);
            if (d < 0.05f) d = fallback;
            return d;
        }

        void ApplyIgnore(bool ignore)
        {
            if (_obstacleLayer < 0) return;
            if (_ignored == ignore) return;
            Physics.IgnoreLayerCollision(_playerLayer, _obstacleLayer, ignore);
            _ignored = ignore;
            if (debugLogs)
                Debug.Log($"[PowerupController] Ignore Player({LayerMask.LayerToName(_playerLayer)}) ↔ Obstacle({LayerMask.LayerToName(_obstacleLayer)}) = {ignore}");
        }
    }
}

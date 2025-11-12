// Assets/_Project/Scripts/Gameplay/ComboSystem.cs
using UnityEngine;
using ShadowRunners.Systems;      // ScoreSystem, StatusPanel
using static ShadowRunners.Gameplay.GameManager;

namespace ShadowRunners.Gameplay
{
    /// Flow Meter:
    /// - Decays while Running
    /// - Gains on events (coin, near-miss, powerup, dash, risky dodge)
    /// - Maps 0..flowMax to minMult..maxMult (continuous)
    /// - Feeds ScoreSystem.SetMultiplier(mult)
    public class ComboSystem : MonoBehaviour
    {
        public static ComboSystem Instance { get; private set; }

        [Header("Flow")]
        [Tooltip("Maximum flow value.")]
        public float flowMax = 100f;
        [Tooltip("Flow lost per second while running (doing nothing).")]
        public float decayPerSecond = 15f;

        [Header("Event Gains")]
        [Tooltip("Per-coin gain.")]
        public float coinGain = 1.2f;
        [Tooltip("Bonus when a coin row/arc is fully collected.")]
        public float coinSequenceBonus = 8f;
        [Tooltip("Near-miss burst.")]
        public float nearMissGain = 12f;
        [Tooltip("On powerup pickup (any type).")]
        public float powerupPickupGain = 6f;
        [Tooltip("On dash activation/use.")]
        public float dashUseGain = 10f;
        [Tooltip("Late lane change near an obstacle.")]
        public float riskyDodgeGain = 8f;

        [Header("Multiplier Mapping")]
        [Tooltip("Multiplier at zero flow.")]
        public float minMult = 1f;
        [Tooltip("Multiplier at full flow.")]
        public float maxMult = 2f;

        [Header("Status UI")]
        public bool showInStatusPanel = true;
        public string statusKey = "Flow";

        float _flow;   // 0..flowMax
        float _mult = 1f;

        public float Multiplier => _mult;
        public float Flow => _flow;                          // absolute
        public float Flow01 => flowMax > 0 ? Mathf.Clamp01(_flow / flowMax) : 0f;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnEnable()
        {
            ResetAll();
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (!gm || gm.State != GameState.Running) return;

            // Passive decay while running
            _flow = Mathf.Max(0f, _flow - decayPerSecond * Time.deltaTime);
            UpdateMultiplier();

            if (showInStatusPanel && StatusPanel.Instance)
                StatusPanel.Instance.SetLine(statusKey, $"Flow x{_mult:0.##} ({Mathf.RoundToInt(Flow01 * 100f)}%)");

            if (ScoreSystem.Instance)
                ScoreSystem.Instance.SetMultiplier(_mult);
        }

        void UpdateMultiplier()
        {
            _mult = Mathf.Lerp(minMult, maxMult, Flow01);
        }
        // --- Back-compat shim for older callers ---
        public void AddNearMissBoost() => OnNearMiss();
        void AddFlowInternal(float amount)
        {
            if (amount <= 0f) return;
            _flow = Mathf.Min(flowMax, _flow + amount);
            UpdateMultiplier();

            if (showInStatusPanel && StatusPanel.Instance)
                StatusPanel.Instance.SetLine(statusKey, $"Flow x{_mult:0.##} ({Mathf.RoundToInt(Flow01 * 100f)}%)");

            if (ScoreSystem.Instance)
                ScoreSystem.Instance.SetMultiplier(_mult);
        }

        // ---- Public hooks you can call from other systems ----
        public void OnCoinCollected() => AddFlowInternal(coinGain);
        public void OnCoinSequenceCompleted() => AddFlowInternal(coinSequenceBonus);
        public void OnNearMiss() => AddFlowInternal(nearMissGain);
        public void OnPowerupPickup() => AddFlowInternal(powerupPickupGain);
        public void OnDashUsed() => AddFlowInternal(dashUseGain);
        public void OnRiskyDodge() => AddFlowInternal(riskyDodgeGain);

        public void ResetAll()
        {
            _flow = 0f;
            UpdateMultiplier();
            if (showInStatusPanel && StatusPanel.Instance)
                StatusPanel.Instance.SetLine(statusKey, string.Empty);
            if (ScoreSystem.Instance)
                ScoreSystem.Instance.SetMultiplier(_mult);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using ShadowRunners.UI;
using ShadowRunners.Systems;

namespace ShadowRunners.Gameplay
{
    /// Tracks collected coins per runId; accurately sums the actual amounts
    /// added by ScoreSystem for coins belonging to that run, using a pairing queue.
    [AddComponentMenu("ShadowRunners/Gameplay/Coin Run Collection Tracker")]
    public class CoinRunCollectionTracker : MonoBehaviour
    {
        // runId -> (planned count, collected count, summed value)
        private readonly Dictionary<int, (int planned, int collected, int valueSum)> _runs = new();

        private CoinRunDirector _dir;

        // Pairing FIFOs
        private readonly Queue<(int runId, float t)> _pendingRunIds = new();
        private readonly Queue<(int amount, float t)> _pendingAmounts = new();

        // If events come out of order, we allow this many seconds to pair them
        [Tooltip("Max time window to pair a runId with a coin amount.")]
        public float pairWindowSeconds = 0.25f;

        void OnEnable()
        {
            TryBindDirector();
            CoinRunCollectionEvents.OnCoinCollectedForRun += OnCoinCollectedForRun;
            ScoreSystem.OnCoinsAdded += OnCoinsAdded;
        }

        void OnDisable()
        {
            UnbindDirector();
            CoinRunCollectionEvents.OnCoinCollectedForRun -= OnCoinCollectedForRun;
            ScoreSystem.OnCoinsAdded -= OnCoinsAdded;
        }

        void Update()
        {
            if (_dir == null) TryBindDirector();
            PairQueues();
            CullStale();
        }

        void TryBindDirector()
        {
            var d = CoinRunDirector.Instance;
            if (!d || d == _dir) return;

            UnbindDirector();
            _dir = d;
            _dir.OnRunStarted += HandleRunStarted;

            // Seed active run if we attached mid-stream
            if (_dir.TryGetActiveRun(out int runId, out int total, out int remaining))
            {
                if (runId >= 0 && total > 0 && !_runs.ContainsKey(runId))
                    _runs[runId] = (planned: total, collected: 0, valueSum: 0);
            }
        }

        void UnbindDirector()
        {
            if (_dir != null)
            {
                _dir.OnRunStarted -= HandleRunStarted;
                _dir = null;
            }
        }

        void HandleRunStarted(int runId, int totalCoins)
        {
            _runs[runId] = (planned: Mathf.Max(0, totalCoins), collected: 0, valueSum: 0);
        }

        // Event from CoinRunIdTag (per coin)
        void OnCoinCollectedForRun(int runId)
        {
            _pendingRunIds.Enqueue((runId, Time.unscaledTime));
            PairQueues();
        }

        // Event from ScoreSystem (per coin, final amount after multipliers)
        void OnCoinsAdded(int amount)
        {
            if (amount <= 0) return;
            _pendingAmounts.Enqueue((amount, Time.unscaledTime));
            PairQueues();
        }

        void PairQueues()
        {
            // Pair FIFO within the allowed window
            while (_pendingRunIds.Count > 0 && _pendingAmounts.Count > 0)
            {
                var (rid, tr) = _pendingRunIds.Peek();
                var (amt, ta) = _pendingAmounts.Peek();

                if (Mathf.Abs(ta - tr) > pairWindowSeconds)
                {
                    // If the earliest pair is too far apart, drop the older one
                    if (tr < ta) { _pendingRunIds.Dequeue(); }
                    else { _pendingAmounts.Dequeue(); }
                    continue;
                }

                // Pair them
                _pendingRunIds.Dequeue();
                _pendingAmounts.Dequeue();

                if (!_runs.TryGetValue(rid, out var entry))
                {
                    // Missed run start, recover planned total
                    int planned = 1;
                    if (_dir != null && _dir.TryGetPlannedCount(rid, out var plannedFromDir))
                        planned = plannedFromDir;
                    entry = (planned: Mathf.Max(1, planned), collected: 0, valueSum: 0);
                }

                entry.collected = Mathf.Min(entry.planned, entry.collected + 1);
                entry.valueSum += amt;
                _runs[rid] = entry;

                if (entry.collected >= entry.planned && entry.planned > 0)
                {
                    int payout = Mathf.Max(0, entry.valueSum);
                    _runs.Remove(rid);

                    if (CoinRunPopupSystem.Instance)
                        CoinRunPopupSystem.Instance.Show(payout);
                }
            }
        }

        void CullStale()
        {
            // Keep queues tidy: drop entries that exceeded the pairing window
            float now = Time.unscaledTime;
            while (_pendingRunIds.Count > 0 && now - _pendingRunIds.Peek().t > pairWindowSeconds)
                _pendingRunIds.Dequeue();
            while (_pendingAmounts.Count > 0 && now - _pendingAmounts.Peek().t > pairWindowSeconds)
                _pendingAmounts.Dequeue();
        }
    }
}

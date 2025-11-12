using UnityEngine;
using System;
using System.Collections.Generic;

namespace ShadowRunners.Gameplay
{
    /// Manages single-lane coin "runs" (10..15 coins), spaced and gapped.
    /// Tiles ask for a plan per tile. Emits OnRunStarted(runId,total) and remembers planned totals.
    public class CoinRunDirector : MonoBehaviour
    {
        public static CoinRunDirector Instance { get; private set; }

        [Header("Run Settings")]
        public Vector2Int runLengthRange = new Vector2Int(10, 15);
        public float coinSpacing = 1.5f;         // meters
        public float gapAfterRun = 3.5f;         // meters
        [Range(0f, 1f)] public float startChancePerTile = 0.65f;

        [Header("Lanes")]
        public float laneOffset = 2f;

        // Runtime state
        System.Random _rng;
        bool _active;
        int _lane;               // -1,0,+1
        int _coinsRemaining;
        int _runTotalCoins;
        float _zCursor;            // next coin world Z
        float _cooldownZ;          // earliest world Z we may start a new run
        int _runIdCounter;       // unique id per run
        int _currentRunId = -1;

        // Keeps planned totals even if tracker subscribes late.
        readonly Dictionary<int, int> _plannedByRun = new Dictionary<int, int>();

        /// Fired when a new run is created (before any coins spawn).
        /// Args: runId, totalCoinsPlanned.
        public event Action<int, int> OnRunStarted;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            _rng = new System.Random();
        }

        /// Call at scene/track start.
        public void ResetAll(float worldZStart)
        {
            _active = false;
            _coinsRemaining = 0;
            _runTotalCoins = 0;
            _zCursor = worldZStart;
            _cooldownZ = worldZStart;
            _currentRunId = -1;
            _plannedByRun.Clear();
        }

        /// Tiles call this to get the portion of the current run to place on THIS tile.
        /// Outputs: hasRun, lane, firstCoinZ (world), count (coins on this tile), runId.
        public void PlanForTile(float tileZStart, float tileZEnd,
                                out bool hasRun, out int lane,
                                out float firstCoinZ, out int count, out int runId)
        {
            hasRun = false; lane = 0; firstCoinZ = 0f; count = 0; runId = -1;

            // Continue current run
            if (_active)
            {
                hasRun = true;
                lane = _lane;
                runId = _currentRunId;

                firstCoinZ = Mathf.Max(_zCursor, tileZStart);
                int canFit = Mathf.Max(0, 1 + Mathf.FloorToInt((tileZEnd - firstCoinZ) / coinSpacing));
                count = Mathf.Min(_coinsRemaining, canFit);

                _zCursor = firstCoinZ + count * coinSpacing;
                _coinsRemaining -= count;

                if (_coinsRemaining <= 0)
                {
                    _active = false;
                    _cooldownZ = _zCursor + gapAfterRun;
                    _currentRunId = -1;
                }
                return;
            }

            // Maybe start a new run on/within this tile
            if (tileZStart >= _cooldownZ && _rng.NextDouble() <= startChancePerTile)
            {
                _active = true;
                _lane = _rng.Next(-1, 2); // -1,0,+1
                _coinsRemaining = UnityEngine.Random.Range(runLengthRange.x, runLengthRange.y + 1);
                _runTotalCoins = _coinsRemaining;
                _currentRunId = ++_runIdCounter;

                // Remember planned total; announce to listeners
                _plannedByRun[_currentRunId] = _runTotalCoins;
                OnRunStarted?.Invoke(_currentRunId, _runTotalCoins);

                float startZ = Mathf.Max(tileZStart, _cooldownZ);
                _zCursor = startZ;

                hasRun = true;
                lane = _lane;
                runId = _currentRunId;

                firstCoinZ = Mathf.Max(_zCursor, tileZStart);
                int canFit = Mathf.Max(0, 1 + Mathf.FloorToInt((tileZEnd - firstCoinZ) / coinSpacing));
                count = Mathf.Min(_coinsRemaining, canFit);

                _zCursor = firstCoinZ + count * coinSpacing;
                _coinsRemaining -= count;

                if (_coinsRemaining <= 0)
                {
                    _active = false;
                    _cooldownZ = _zCursor + gapAfterRun;
                    _currentRunId = -1;
                }
            }
        }

        /// Tracker can call this if it missed OnRunStarted.
        public bool TryGetPlannedCount(int runId, out int total)
        {
            return _plannedByRun.TryGetValue(runId, out total);
        }

        /// Optional: expose current active run for late binders.
        public bool TryGetActiveRun(out int runId, out int totalPlanned, out int remaining)
        {
            if (_currentRunId < 0 || !_active) { runId = -1; totalPlanned = 0; remaining = 0; return false; }
            runId = _currentRunId;
            _plannedByRun.TryGetValue(_currentRunId, out totalPlanned);
            remaining = _coinsRemaining;
            return true;
        }
    }
}

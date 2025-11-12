using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
[AddComponentMenu("ShadowRunners/Decor/Random Child Activator")]
public class RandomChildActivator : MonoBehaviour
{
    public enum SeedMode { None, FromTransformPosition, FromNameHash, FromCustom }

    [Header("Selection")]
    [Tooltip("Minimum number of children to activate.")]
    public int minActive = 1;
    [Tooltip("Maximum number of children to activate.")]
    public int maxActive = 3;

    [Tooltip("If true, only considers direct children. If false, includes all descendants.")]
    public bool onlyDirectChildren = true;

    [Tooltip("Optional filter: only consider children on this Layer (-1 = any).")]
    public int filterLayer = -1;

    [Header("Determinism (avoid sync across pooled tiles)")]
    public SeedMode seedMode = SeedMode.FromTransformPosition;
    [Tooltip("Used when SeedMode = FromCustom.")]
    public int customSeed = 12345;
    [Tooltip("Extra salt you can set per tile from a spawner if you want.")]
    public int salt = 0;

    [Header("Lifecycle")]
    [Tooltip("Run the first selection in Awake (pre-frame) and make all non-selected children inactive immediately.")]
    public bool selectInAwake = true;
    [Tooltip("Re-run selection each time the object is enabled (good for pooled tiles).")]
    public bool reselectionOnEnable = true;

    readonly List<GameObject> _candidates = new List<GameObject>(64);
    static readonly List<int> sTempIndices = new List<int>(64);

    bool _cached;
    System.Random _rng;

    void Awake()
    {
        CacheCandidates();
        BuildRng();            // independent RNG
        SetAllCandidatesActive(false);
        if (selectInAwake) ApplySelection();
    }

    void OnEnable()
    {
        if (!_cached) { CacheCandidates(); BuildRng(); }
        if (reselectionOnEnable)
        {
            SetAllCandidatesActive(false);
            ApplySelection();
        }
    }

    void CacheCandidates()
    {
        _candidates.Clear();

        if (onlyDirectChildren)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var t = transform.GetChild(i);
                if (Accept(t.gameObject)) _candidates.Add(t.gameObject);
            }
        }
        else
        {
            var trs = GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var tr in trs)
            {
                if (tr == transform) continue;
                if (onlyDirectChildren && tr.parent != transform) continue;
                if (Accept(tr.gameObject)) _candidates.Add(tr.gameObject);
            }
        }

        _cached = true;
    }

    bool Accept(GameObject go)
    {
        if (filterLayer >= 0 && go.layer != filterLayer) return false;
        return true;
    }

    void SetAllCandidatesActive(bool value)
    {
        for (int i = 0; i < _candidates.Count; i++)
        {
            var go = _candidates[i];
            if (go && go.activeSelf != value) go.SetActive(value);
        }
    }

    void ApplySelection()
    {
        if (_candidates.Count == 0) return;

        int lo = Mathf.Clamp(minActive, 0, _candidates.Count);
        int hi = Mathf.Clamp(maxActive, lo, _candidates.Count);
        int count = RngRange(lo, hi + 1);
        if (count == 0) return;

        // Shuffle indices (Fisher–Yates) using local RNG
        var idx = sTempIndices;
        idx.Clear();
        for (int i = 0; i < _candidates.Count; i++) idx.Add(i);
        for (int i = idx.Count - 1; i > 0; i--)
        {
            int j = RngRange(0, i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }

        // Activate first N
        for (int k = 0; k < count && k < idx.Count; k++)
        {
            var go = _candidates[idx[k]];
            if (go) go.SetActive(true);
        }
    }

    void BuildRng()
    {
        int seed = salt;

        if (seedMode != SeedMode.None)
        {
            unchecked
            {
                if (seedMode == SeedMode.FromTransformPosition)
                {
                    var p = transform.position;
                    seed ^= Mathf.FloorToInt(p.x * 73856093);
                    seed ^= Mathf.FloorToInt(p.y * 19349663) << 8;
                    seed ^= Mathf.FloorToInt(p.z * 83492791) << 16;
                }
                else if (seedMode == SeedMode.FromNameHash)
                {
                    seed ^= gameObject.name.GetHashCode();
                }
                else if (seedMode == SeedMode.FromCustom)
                {
                    seed ^= customSeed;
                }
            }
        }

        // Decorrelate further with instance + time (keeps things de-synced across tiles)
        seed ^= GetInstanceID();
        seed ^= Environment.TickCount;

        _rng = seed == 0 ? new System.Random() : new System.Random(seed);
    }

    int RngRange(int minInclusive, int maxExclusive)
    {
        if (_rng == null) _rng = new System.Random();
        return minInclusive + _rng.Next(maxExclusive - minInclusive);
    }
}

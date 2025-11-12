using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// For a single group of fire decors (direct children of this object).
/// Picks up to 'maxActive' children to enable on each tile, using a tile-seeded RNG,
/// and respects DecorSpawnLimiter spacing BEFORE enabling.
/// If all candidates are blocked, can optionally enable one fallback to avoid starvation.
/// </summary>
[AddComponentMenu("ShadowRunners/Decor/Fire Single Group Activator")]
public class FireSingleGroupActivator : MonoBehaviour
{
    [Header("Selection")]
    [Tooltip("Maximum number of fires to enable per tile (0..childCount).")]
    public int maxActive = 1;

    [Tooltip("Only consider direct children of this object. Turn OFF if your fires are nested deeper.")]
    public bool onlyDirectChildren = true;

    [Tooltip("Only consider objects on this layer (-1 = any).")]
    public int filterLayer = -1;

    [Header("Tile Awareness")]
    [Tooltip("Approx tile length in meters used to compute a tile index from world Z (GroundTile_Large ≈ 36).")]
    public float approxTileLength = 36f;

    [Header("Fallback")]
    [Tooltip("If spacing blocks all candidates this tile, still enable one (prevents 'nothing ever spawns').")]
    public bool requireAtLeastOne = true;

    [Header("Debug")]
    public bool logDecisions = false;

    System.Random _rng;
    static readonly List<ChildInfo> sBuf = new List<ChildInfo>(64);

    struct ChildInfo
    {
        public GameObject go;
        public DecorSpawnLimiter lim;
        public string typeKey;
        public float z;
    }

    void Awake()
    {
        // no-op; we seed inside Apply() so pooled tiles get fresh seeds
    }

    void OnEnable()
    {
        Apply();
    }

    void Apply()
    {
        BuildRng(); // seed per tile position

        // Gather candidates (include inactive)
        sBuf.Clear();
        if (onlyDirectChildren)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                var t = transform.GetChild(i);
                if (filterLayer >= 0 && t.gameObject.layer != filterLayer) continue;
                sBuf.Add(BuildInfo(t.gameObject));
            }
        }
        else
        {
            var trs = GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var tr in trs)
            {
                if (tr == transform) continue;
                if (onlyDirectChildren && tr.parent != transform) continue;
                if (filterLayer >= 0 && tr.gameObject.layer != filterLayer) continue;
                sBuf.Add(BuildInfo(tr.gameObject));
            }
        }

        // Turn all OFF first to avoid initial flashes
        for (int i = 0; i < sBuf.Count; i++)
        {
            var go = sBuf[i].go;
            if (go && go.activeSelf) go.SetActive(false);
        }

        if (sBuf.Count == 0)
        {
            if (logDecisions) Debug.Log($"[FireSingleGroupActivator] No candidates found (check onlyDirectChildren/layer filter).", this);
            return;
        }

        if (maxActive <= 0)
        {
            if (logDecisions) Debug.Log($"[FireSingleGroupActivator] maxActive <= 0; skipping enable.", this);
            return;
        }

        // Shuffle using local RNG
        for (int i = sBuf.Count - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (sBuf[i], sBuf[j]) = (sBuf[j], sBuf[i]);
        }

        int enabled = 0;
        string lastBlockReason = "";

        // Try to enable up to maxActive respecting spacing
        for (int i = 0; i < sBuf.Count && enabled < maxActive; i++)
        {
            var c = sBuf[i];
            if (c.go == null) continue;

            if (c.lim == null)
            {
                c.go.SetActive(true);
                enabled++;
                if (logDecisions) Debug.Log($"[FireSingleGroupActivator] Enabled '{c.go.name}' (no limiter).", c.go);
                continue;
            }

            // Query + reserve BEFORE enabling
            bool ok = DecorSpawnLimiter.TryReserve(
                c.lim.typeKey,
                c.z,
                c.lim.approxTileLength,
                c.lim.minTileGap,
                c.lim.minZGap,
                c.lim.minTimeGap,
                out string reason
            );

            if (ok)
            {
                c.go.SetActive(true);
                enabled++;
                if (logDecisions) Debug.Log($"[FireSingleGroupActivator] Enabled '{c.go.name}' type={c.lim.typeKey}.", c.go);
            }
            else
            {
                lastBlockReason = reason;
                if (logDecisions) Debug.Log($"[FireSingleGroupActivator] Skipped '{c.go.name}' type={c.lim.typeKey} because {reason}.", c.go);
            }
        }

        // Fallback if everything was blocked
        if (enabled == 0 && requireAtLeastOne)
        {
            // Pick the first that either has no limiter or has the loosest rules
            for (int i = 0; i < sBuf.Count; i++)
            {
                var c = sBuf[i];
                if (c.go == null) continue;

                // Prefer no-limiter or minTileGap == 0
                if (c.lim == null || (c.lim.minTileGap == 0 && c.lim.minZGap == 0 && c.lim.minTimeGap == 0))
                {
                    c.go.SetActive(true);
                    if (logDecisions) Debug.Log($"[FireSingleGroupActivator] Fallback enabled '{c.go.name}' (no limiter/loose rules).", c.go);
                    return;
                }
            }

            // Otherwise just force-enable the first candidate (rare)
            sBuf[0].go.SetActive(true);
            if (logDecisions)
                Debug.Log($"[FireSingleGroupActivator] Fallback force-enabled '{sBuf[0].go.name}'. Last block reason: {lastBlockReason}", sBuf[0].go);
        }
    }

    void BuildRng()
    {
        int tileIndex = 0;
        if (approxTileLength > 0.01f)
            tileIndex = Mathf.FloorToInt(transform.position.z / approxTileLength);

        int seed = tileIndex;
        seed ^= GetInstanceID();
        seed ^= gameObject.name.GetHashCode();
        _rng = new System.Random(seed);
    }

    ChildInfo BuildInfo(GameObject go)
    {
        var lim = go.GetComponent<DecorSpawnLimiter>();
        var tr = lim && lim.worldRef ? lim.worldRef : go.transform;
        return new ChildInfo
        {
            go = go,
            lim = lim,
            typeKey = lim ? lim.typeKey : "",
            z = tr.position.z
        };
    }
}

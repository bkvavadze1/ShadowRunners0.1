using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data + optional enforcement for decor spacing.
/// If 'enforceOnEnable' is OFF, this acts as a data holder (parent activator will query spacing).
/// If ON, it will enforce by deactivating itself when too close/soon.
/// Also exposes a static registry method to check + reserve a spawn for a given type.
/// </summary>
[AddComponentMenu("ShadowRunners/Decor/Decor Spawn Limiter")]
public class DecorSpawnLimiter : MonoBehaviour
{
    [Tooltip("Identifier for this decor type. Use the same string for visually identical variants (e.g., 'Fire_Wall_A').")]
    public string typeKey = "Fire_Wall_A";

    [Header("Tile-based limit (recommended)")]
    [Tooltip("Approx tile length in meters. Set to your GroundTile_Large length (≈36).")]
    public float approxTileLength = 36f;
    [Tooltip("Require at least this many tiles between instances of the same type. 0 disables tile-gap check.")]
    public int minTileGap = 2;

    [Header("Optional extra limits")]
    [Tooltip("Minimum forward (Z) meters between instances of the same type. 0 disables.")]
    public float minZGap = 0f;
    [Tooltip("Minimum seconds (real time) between instances of the same type. 0 disables.")]
    public float minTimeGap = 0f;

    [Header("World Reference")]
    [Tooltip("If null, uses this.transform to read Z.")]
    public Transform worldRef;

    [Header("Mode")]
    [Tooltip("If ON, this component will auto-enforce on enable by disabling the GameObject when spacing fails.\nIf OFF, it acts as data only; a parent activator will enforce before enabling.")]
    public bool enforceOnEnable = false; // <-- key change: default OFF

    [Header("Debug")]
    public bool logDecisions = false;

    // Shared registry (per type)
    static readonly Dictionary<string, int> s_LastTileIndexByType = new Dictionary<string, int>();
    static readonly Dictionary<string, float> s_LastZByType = new Dictionary<string, float>();
    static readonly Dictionary<string, float> s_LastTimeByType = new Dictionary<string, float>();

    void OnEnable()
    {
        if (!enforceOnEnable) return; // passive mode

        var t = worldRef ? worldRef : transform;
        float zNow = t.position.z;
        float timeNow = Time.unscaledTime;

        if (TryReserve(typeKey, zNow, approxTileLength, minTileGap, minZGap, minTimeGap, out string reason))
        {
            if (logDecisions) Debug.Log($"[DecorSpawnLimiter:{typeKey}] accepted (self).", this);
            return;
        }

        if (logDecisions) Debug.Log($"[DecorSpawnLimiter:{typeKey}] skipped (self): {reason}", this);
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Check spacing for a type and, if allowed, reserve it (update registry).
    /// Returns true if accepted; false if blocked. 'reason' explains the block.
    /// </summary>
    public static bool TryReserve(
        string typeKey,
        float zNow,
        float approxTileLength,
        int minTileGap,
        float minZGap,
        float minTimeGap,
        out string reason)
    {
        reason = "";

        // Derive tile index from Z (supports negative Z)
        int tileIdx = (approxTileLength > 0.01f) ? Mathf.FloorToInt(zNow / approxTileLength) : 0;

        bool hadTile = s_LastTileIndexByType.TryGetValue(typeKey, out int lastTile);
        bool hadZ = s_LastZByType.TryGetValue(typeKey, out float lastZ);
        bool hadTime = s_LastTimeByType.TryGetValue(typeKey, out float lastTime);

        // First-ever of this type is always allowed
        if (!hadTile && !hadZ && !hadTime)
        {
            Accept(typeKey, tileIdx, zNow, Time.unscaledTime);
            return true;
        }

        if (hadTile && minTileGap > 0 && Mathf.Abs(tileIdx - lastTile) < minTileGap)
        {
            reason = $"tile gap {Mathf.Abs(tileIdx - lastTile)} < {minTileGap}";
            return false;
        }
        if (hadZ && minZGap > 0f && Mathf.Abs(zNow - lastZ) < minZGap)
        {
            reason = $"z gap {Mathf.Abs(zNow - lastZ):F1} < {minZGap}";
            return false;
        }
        float dt = Time.unscaledTime - lastTime;
        if (hadTime && minTimeGap > 0f && dt < minTimeGap)
        {
            reason = $"time gap {dt:F2}s < {minTimeGap}s";
            return false;
        }

        Accept(typeKey, tileIdx, zNow, Time.unscaledTime);
        return true;
    }

    static void Accept(string key, int tileIdx, float z, float t)
    {
        s_LastTileIndexByType[key] = tileIdx;
        s_LastZByType[key] = z;
        s_LastTimeByType[key] = t;
    }
}

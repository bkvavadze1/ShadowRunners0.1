using ShadowRunners.Gameplay;
using UnityEngine;

/// <summary>
/// Keeps a landmark (bell tower) at a constant distance ahead of the runner,
/// while applying a looping "loom" scale so it feels like you're getting closer
/// without ever actually closing the gap.
/// </summary>
[AddComponentMenu("ShadowRunners/World/Bell Tower Horizon")]
public class BellTowerHorizon : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Runner / player transform. Auto-finds by Player tag or RunnerMotor if unset.")]
    public Transform runner;
    [Tooltip("Root visual of the tower (scales/drifts). If null, uses this transform.")]
    public Transform towerRoot;

    [Header("Horizon Placement")]
    [Tooltip("Meters to keep the tower in front of the runner (Z+).")]
    public float horizonDistance = 520f;
    [Tooltip("World Y position for the tower (e.g., ground height).")]
    public float worldY = 0f;
    [Tooltip("Lock X to this value (0 = centered). Set to 0 to keep it in the middle street axis.")]
    public float lockX = 0f;

    [Header("Loom (fake approach loop)")]
    [Tooltip("Base local scale of the tower.")]
    public Vector3 baseScale = Vector3.one;
    [Tooltip("Min/Max multiplier applied over a loop (e.g., 0.9..1.2).")]
    public Vector2 loomScaleRange = new Vector2(0.92f, 1.18f);
    [Tooltip("How many meters of runner distance complete one loom cycle (bigger = slower loop).")]
    public float loomCycleMeters = 320f;
    [Tooltip("Easing sharpness of looming (1 = linear, 2 = ease, 3+ = stronger).")]
    public float loomEasePower = 2f;

    [Header("Subtle Drift (optional)")]
    [Tooltip("Small left/right sway in meters (0 to disable).")]
    public float driftXAmplitude = 3.0f;
    [Tooltip("Meters per full drift cycle (phase advances with runner distance).")]
    public float driftCycleMeters = 240f;

    [Header("Visibility Tweaks")]
    [Tooltip("Clamp the camera-facing rotation around Y so the tower always faces the camera a bit (0 = off, 1 = billboardy).")]
    [Range(0f, 1f)] public float billboardish = 0.0f;

    // Optional ScoreSystem hook to read distance smoothly; otherwise derive from runner Z.
    ShadowRunners.Systems.ScoreSystem _score;
    Transform _self;

    void Awake()
    {
        _self = transform;

        // Auto-find runner if not assigned
        if (!runner)
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged) runner = tagged.transform;
            else
            {
                var motor = FindObjectOfType<RunnerMotor>();
                if (motor) runner = motor.transform;
            }
        }

        if (!towerRoot) towerRoot = _self;

        // Optional: grab ScoreSystem for stable distance accumulation
        _score = FindObjectOfType<ShadowRunners.Systems.ScoreSystem>();
    }

    void LateUpdate()
    {
        if (!runner || !towerRoot) return;

        // 1) Keep tower always horizonDistance ahead of runner, centered on track
        Vector3 p = _self.position;
        p.x = lockX + DriftX();                  // small sway
        p.y = worldY;
        p.z = runner.position.z + horizonDistance;
        _self.position = p;

        // 2) Billboardish turn (very subtle, optional)
        if (billboardish > 0f)
        {
            Vector3 toCam = Camera.main ? (Camera.main.transform.position - _self.position) : Vector3.forward;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 0.001f)
            {
                Quaternion facing = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                _self.rotation = Quaternion.Slerp(_self.rotation, facing, billboardish);
            }
        }

        // 3) Loom scale loop based on runner distance (not time)
        float dist = GetRunnerDistanceMeters();
        float sMul = LoomScale(dist);
        towerRoot.localScale = baseScale * sMul;
    }

    float GetRunnerDistanceMeters()
    {
        if (_score != null) return _score.Distance;                    // uses your public property we added earlier
        // Fallback: delta runner Z since play start. Works fine for an endless track.
        return Mathf.Max(0f, runner.position.z);
    }

    float LoomScale(float distanceMeters)
    {
        if (loomCycleMeters <= 0.01f) return 1f;
        // Phase 0..1 along cycle
        float t = Mathf.Repeat(distanceMeters / loomCycleMeters, 1f);  // sawtooth
        // Mirror to 0..1..0 (ping-pong)
        float ping = (t < 0.5f) ? (t * 2f) : (1f - (t - 0.5f) * 2f);
        // Ease (smoothstep-like via power)
        float eased = Mathf.Pow(ping, Mathf.Max(1f, loomEasePower));
        return Mathf.Lerp(loomScaleRange.x, loomScaleRange.y, eased);
    }

    float DriftX()
    {
        if (driftXAmplitude <= 0f || driftCycleMeters <= 0.01f) return 0f;
        float dist = GetRunnerDistanceMeters();
        float phase = (dist / driftCycleMeters) * Mathf.PI * 2f;
        return Mathf.Sin(phase) * driftXAmplitude;
    }
}

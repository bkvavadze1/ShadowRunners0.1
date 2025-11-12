using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(CharacterController))]
public class CapsuleAutoFit : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("Leave empty to auto-find the first SkinnedMeshRenderer/MeshRenderer in children.")]
    public Renderer sourceRenderer;

    [Header("Padding (meters)")]
    [Tooltip("Extra height added above and below visual bounds.")]
    public float verticalPadding = 0.06f;
    [Tooltip("Extra radius around the thinnest horizontal axis.")]
    public float radiusPadding = 0.02f;

    [Header("Clamp")]
    public float minHeight = 1.2f;
    public float maxHeight = 2.2f;
    public float minRadius = 0.18f;
    public float maxRadius = 0.45f;

    CharacterController cc;

    void OnEnable()
    {
        cc = GetComponent<CharacterController>();
        if (!sourceRenderer) sourceRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (!sourceRenderer) sourceRenderer = GetComponentInChildren<MeshRenderer>();
    }

    [ContextMenu("Fit To Visual")]
    public void FitNow()
    {
        if (!cc) cc = GetComponent<CharacterController>();
        if (!sourceRenderer)
        {
            Debug.LogWarning("[CapsuleAutoFit] No Renderer found.");
            return;
        }

        // World-space bounds → convert to local space relative to CC
        var bounds = sourceRenderer.bounds;

        // Height from visual Y-size + padding
        float h = Mathf.Clamp(bounds.size.y + verticalPadding * 2f, minHeight, maxHeight);

        // Radius from min of X/Z (character is slimmer than wide), + padding
        float r = Mathf.Clamp(Mathf.Min(bounds.size.x, bounds.size.z) * 0.5f + radiusPadding, minRadius, maxRadius);

        // Set center so feet are near ground (CharacterController uses center at half height by default)
        // We place capsule bottom slightly below visual min to keep grounded.
        float bottomY = bounds.min.y - transform.position.y;
        float centerY = bottomY + h * 0.5f - 0.02f; // nudge down a hair

        cc.height = h;
        cc.radius = r;
        cc.center = new Vector3(0f, centerY, 0f);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(cc);
        UnityEditor.SceneView.RepaintAll();
#endif

        Debug.Log($"[CapsuleAutoFit] height={h:F2}, radius={r:F2}, centerY={centerY:F2}");
    }

    void OnValidate()
    {
        if (cc)
        {
            cc.height = Mathf.Clamp(cc.height, minHeight, maxHeight);
            cc.radius = Mathf.Clamp(cc.radius, minRadius, maxRadius);
        }
    }
}

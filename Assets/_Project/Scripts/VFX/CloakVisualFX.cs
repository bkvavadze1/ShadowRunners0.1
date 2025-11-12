using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ShadowRunners.Gameplay
{
    /// <summary>
    /// Fades the character when PowerupController.cloakActive is true.
    /// Converts URP materials to Transparent once, then drives _BaseColor alpha via MPB.
    /// </summary>
    public class CloakVisualFX : MonoBehaviour
    {
        [Header("References")]
        public PowerupController powerups;
        public Transform visualsRoot;

        [Header("Fade Settings")]
        [Range(0.05f, 1f)] public float cloakAlpha = 0.35f;
        [Range(1f, 20f)] public float fadeSpeed = 6f;
        public bool makeTransparentAtStart = true;

        // Internals
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly Dictionary<(Renderer, int), Color> _originalColors = new Dictionary<(Renderer, int), Color>();
        private MaterialPropertyBlock _mpb; // <-- create in Awake

        private float _currentAlpha = 1f;

        private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorID = Shader.PropertyToID("_Color");

        void Awake()
        {
            if (!powerups) powerups = GetComponent<PowerupController>();
            if (!visualsRoot) visualsRoot = transform;

            _mpb = new MaterialPropertyBlock(); // <-- allowed here

            // Collect all renderers
            visualsRoot.GetComponentsInChildren(true, _renderers);

            foreach (var r in _renderers)
            {
                if (!r || !r.enabled) continue;

                // Read from sharedMaterials to avoid instantiating; cache original color
                var shared = r.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    var mat = shared[i];
                    Color c = Color.white;
                    if (mat)
                    {
                        if (mat.HasProperty(BaseColorID)) c = mat.GetColor(BaseColorID);
                        else if (mat.HasProperty(ColorID)) c = mat.GetColor(ColorID);
                    }
                    _originalColors[(r, i)] = c;

                    if (makeTransparentAtStart && mat)
                    {
                        // Make an instance so we don't alter the project material
                        var instanced = r.materials; // this instantiates per-submesh materials on this renderer only
                        if (i < instanced.Length && instanced[i])
                            ForceURPTransparent(instanced[i]);
                    }
                }
            }
        }

        void Update()
        {
            bool wantCloak = powerups && powerups.cloakActive;
            float target = wantCloak ? cloakAlpha : 1f;

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, target, fadeSpeed * Time.deltaTime);

            // Apply alpha via MPB per submesh
            foreach (var r in _renderers)
            {
                if (!r || !r.enabled) continue;

                var shared = r.sharedMaterials;
                for (int sub = 0; sub < shared.Length; sub++)
                {
                    if (!_originalColors.TryGetValue((r, sub), out var baseCol)) baseCol = Color.white;
                    var c = baseCol; c.a = baseCol.a * _currentAlpha;

                    r.GetPropertyBlock(_mpb, sub);
                    // Use whichever color property exists
                    if (shared[sub] && shared[sub].HasProperty(BaseColorID))
                        _mpb.SetColor(BaseColorID, c);
                    else
                        _mpb.SetColor(ColorID, c);
                    r.SetPropertyBlock(_mpb, sub);
                }
            }
        }

        // Switch URP material instance to Transparent surface (once)
        private void ForceURPTransparent(Material m)
        {
            if (!m) return;

            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // 1 = Transparent
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // Standard URP transparent setup
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.DisableKeyword("_ALPHATEST_ON");

            m.renderQueue = (int)RenderQueue.Transparent;
        }
    }
}

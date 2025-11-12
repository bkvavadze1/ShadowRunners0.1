using UnityEngine;
using System.Collections.Generic;

namespace ShadowRunners.Perf
{
    /// <summary>
    /// Per-tile culling: disables decor (Renderers, ParticleSystems, Lights) on this tile
    /// when it is far behind or far ahead of the runner, reducing overdraw and light cost.
    /// Attach once on the GroundTile root prefab.
    /// </summary>
    [AddComponentMenu("ShadowRunners/Performance/Tile Decor Culling")]
    public class TileDecorCulling : MonoBehaviour
    {
        [Header("Runner (auto-find if empty)")]
        public Transform runner;

        [Header("Cull Window (relative to runner Z)")]
        [Tooltip("Distance ahead of runner to keep decor enabled.")]
        public float keepAhead = 40f;
        [Tooltip("Distance behind runner to keep decor enabled.")]
        public float keepBehind = 10f;

        [Header("What to cull")]
        [Tooltip("If true, only objects on the Decor layer are affected. Turn off to cull all child visuals.")]
        public bool onlyDecorLayer = true;

        [Tooltip("Optional: restrict culling to these roots (leave empty = scan all children).")]
        public Transform[] decorRoots;

        [Header("Update Cadence")]
        [Tooltip("How often to reevaluate (seconds). Higher = cheaper.")]
        public float checkInterval = 0.25f;

        List<Renderer> _renderers = new();
        List<ParticleSystem> _particles = new();
        List<Light> _lights = new();
        float _t;

        int _decorLayer;

        void Awake()
        {
            _decorLayer = LayerMask.NameToLayer("Decor");
            if (!runner)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player) runner = player.transform;
            }

            // Collect targets
            if (decorRoots != null && decorRoots.Length > 0)
            {
                foreach (var root in decorRoots) CollectUnder(root);
            }
            else
            {
                CollectUnder(transform);
            }
        }

        void CollectUnder(Transform root)
        {
            if (!root) return;

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (onlyDecorLayer && r.gameObject.layer != _decorLayer) continue;
                _renderers.Add(r);
            }
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (onlyDecorLayer && ps.gameObject.layer != _decorLayer) continue;
                _particles.Add(ps);
            }
            foreach (var l in root.GetComponentsInChildren<Light>(true))
            {
                if (onlyDecorLayer && l.gameObject.layer != _decorLayer) continue;
                _lights.Add(l);
            }
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            if (_t < checkInterval) return;
            _t = 0f;

            if (!runner) return;

            float rz = runner.position.z;
            float tz = transform.position.z; // tile origin
            // Consider the tile "inside window" if its center is within these bounds
            bool inside = (tz > rz - keepBehind) && (tz < rz + keepAhead);

            SetEnabled(inside);
        }

        void SetEnabled(bool on)
        {
            // Renderers
            for (int i = 0; i < _renderers.Count; i++)
            {
                var r = _renderers[i];
                if (!r) continue;
                if (r.enabled != on) r.enabled = on;
            }
            // Particles
            for (int i = 0; i < _particles.Count; i++)
            {
                var ps = _particles[i];
                if (!ps) continue;
                if (on)
                {
                    if (!ps.isPlaying) ps.Play(true);
                }
                else
                {
                    if (ps.isPlaying) ps.Pause(true);
                    var em = ps.emission; em.enabled = false; // stop spawning when culled
                }
            }
            // Lights
            for (int i = 0; i < _lights.Count; i++)
            {
                var l = _lights[i];
                if (!l) continue;
                if (l.enabled != on) l.enabled = on;
            }
        }
    }
}

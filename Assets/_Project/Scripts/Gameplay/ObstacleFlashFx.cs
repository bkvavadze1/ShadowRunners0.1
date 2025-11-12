// Assets/_Project/Scripts/Feel/ObstacleFlashFx.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowRunners.Feel
{
    /// Applies a quick white/emissive flash to a target obstacle's renderers.
    [AddComponentMenu("ShadowRunners/Feel/Obstacle Flash FX")]
    public class ObstacleFlashFx : MonoBehaviour
    {
        static ObstacleFlashFx _instance;
        readonly List<Renderer> _buf = new List<Renderer>(32);
        MaterialPropertyBlock _mpb; // create in Awake (not in field initializer)

        [Header("Flash")]
        [Range(0.02f, 0.5f)] public float flashDuration = 0.12f;
        [Tooltip("Intensity added to emission during flash (approx).")]
        public float emissionBoost = 1.6f;
        public Color flashTint = Color.white;

        void Awake()
        {
            if (_instance && _instance != this) { Destroy(gameObject); return; }
            _instance = this;

            // SAFE: instantiate MPB here (not in constructor/field init)
            _mpb = new MaterialPropertyBlock();
        }

        void OnEnable()
        {
            GameFeelBus.OnShieldAbsorb += HandleAbsorb;
        }

        void OnDisable()
        {
            GameFeelBus.OnShieldAbsorb -= HandleAbsorb;
        }

        void HandleAbsorb(GameObject obstacleRoot)
        {
            if (!obstacleRoot) return;
            StartCoroutine(CoFlash(obstacleRoot));
        }

        IEnumerator CoFlash(GameObject root)
        {
            _buf.Clear();
            root.GetComponentsInChildren(true, _buf);

            float t0 = Time.unscaledTime;
            float T = Mathf.Max(0.02f, flashDuration);

            while (Time.unscaledTime - t0 < T)
            {
                float u = Mathf.InverseLerp(0f, T, Time.unscaledTime - t0);
                float e = 1f - Mathf.Abs(2f * u - 1f); // peak mid-pulse

                for (int i = 0; i < _buf.Count; i++)
                {
                    var r = _buf[i];
                    if (!r) continue;

                    r.GetPropertyBlock(_mpb);

                    // Boost base color slightly toward white
                    _mpb.SetColor("_BaseColor", Color.LerpUnclamped(Color.white, flashTint, 0.5f + 0.5f * e));

                    // Boost emission if property exists
                    if (r.sharedMaterial && r.sharedMaterial.HasProperty("_EmissionColor"))
                    {
                        var baseEmis = r.sharedMaterial.GetColor("_EmissionColor");
                        _mpb.SetColor("_EmissionColor", baseEmis + Color.white * (emissionBoost * e));
                    }

                    r.SetPropertyBlock(_mpb);
                }

                yield return null;
            }

            // Clear MPBs
            for (int i = 0; i < _buf.Count; i++)
            {
                var r = _buf[i];
                if (!r) continue;
                r.SetPropertyBlock(null);
            }
        }
    }
}

using UnityEngine;
using ShadowRunners.Gameplay; // PowerupController

namespace ShadowRunners.VFX
{
    public class DashVFXToggle : MonoBehaviour
    {
        public PowerupController powerups;   // drag Runner's PowerupController here
        public bool softStop = true;        // let particles fade out naturally

        ParticleSystem[] _ps;
        TrailRenderer[] _tr;
        bool _on;

        void Awake()
        {
            if (!powerups) powerups = GetComponentInParent<PowerupController>();
            _ps = GetComponentsInChildren<ParticleSystem>(true);
            _tr = GetComponentsInChildren<TrailRenderer>(true);
            SetVfx(false, immediate: true);
        }

        void Update()
        {
            bool dash = powerups && powerups.dashActive;
            if (dash == _on) return;
            SetVfx(dash, immediate: false);
            _on = dash;
        }

        void SetVfx(bool enable, bool immediate)
        {
            foreach (var ps in _ps)
            {
                if (!ps) continue;
                if (enable) ps.Play(true);
                else
                {
                    if (softStop && !immediate) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    else ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
            foreach (var tr in _tr)
            {
                if (!tr) continue;
                if (tr.emitting != enable)
                {
                    tr.emitting = enable;
                    if (!enable && immediate) tr.Clear();
                }
            }
        }
    }
}

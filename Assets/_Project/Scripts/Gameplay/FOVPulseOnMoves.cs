using System.Collections;
using UnityEngine;

namespace ShadowRunners.Feel
{
    /// Adds a tiny, quick FOV pulse when the player jumps or slides.
    /// Works alongside other FOV scripts by adding a small offset.
    [AddComponentMenu("ShadowRunners/Feel/FOV Pulse On Moves")]
    public class FOVPulseOnMoves : MonoBehaviour
    {
        public Camera targetCamera;
        [Header("Pulse")]
        public float jumpAdd = 1.4f;
        public float slideAdd = 1.0f;
        [Range(5f, 30f)] public float returnSpeed = 12f;

        float _offset; // additive to whatever other scripts set
        float _base;   // tracked base (we assume other scripts set camera.fieldOfView before LateUpdate)

        void Awake()
        {
            if (!targetCamera) targetCamera = Camera.main;
        }

        void OnEnable()
        {
            GameFeelBus.OnJump += DoJump;
            GameFeelBus.OnSlide += DoSlide;
        }

        void OnDisable()
        {
            GameFeelBus.OnJump -= DoJump;
            GameFeelBus.OnSlide -= DoSlide;
        }

        void DoJump() { _offset += jumpAdd; }
        void DoSlide() { _offset += slideAdd; }

        void LateUpdate()
        {
            if (!targetCamera) return;

            // We treat current FOV as base each frame and add a decaying offset.
            _base = targetCamera.fieldOfView;
            _offset = Mathf.Lerp(_offset, 0f, Time.unscaledDeltaTime * returnSpeed);
            targetCamera.fieldOfView = _base + _offset;
        }
    }
}

using UnityEngine;

namespace ShadowRunners.CameraFX
{
    /// Gently scales Camera.fieldOfView with runner speed. Plug & play.
    public class FOVSpeedBreather : MonoBehaviour
    {
        public ShadowRunners.Gameplay.RunnerMotor runner;
        public Camera targetCamera;

        [Header("FOV")]
        public float baseFOV = 60f;
        public float maxExtraFOV = 6f;            // ~66 at max
        public float smooth = 6f;

        void Awake()
        {
            if (!targetCamera) targetCamera = Camera.main;
            if (!runner) runner = FindObjectOfType<ShadowRunners.Gameplay.RunnerMotor>();
        }

        void LateUpdate()
        {
            if (!targetCamera || !runner) return;

            float t = Mathf.Approximately(runner.maxSpeed, 0f) ? 0f : Mathf.Clamp01(runner.forwardSpeed / runner.maxSpeed);
            float target = baseFOV + t * maxExtraFOV;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, target, Time.unscaledDeltaTime * smooth);
        }
    }
}

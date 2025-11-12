using UnityEngine;

namespace ShadowRunners.Gameplay
{
    public class Bootstrap : MonoBehaviour
    {
        public RunnerMotor runnerMotor;
        public SwipeInput swipeInput;
        public TrackManager trackManager;
        public GameObject groundTilePrefab;

        private void Awake()
        {
            if (!runnerMotor) runnerMotor = FindObjectOfType<RunnerMotor>();
            if (!swipeInput) swipeInput = FindObjectOfType<SwipeInput>();
            if (!trackManager) trackManager = FindObjectOfType<TrackManager>();

            if (swipeInput && runnerMotor) runnerMotor.HookInput(swipeInput);

            if (trackManager)
            {
                if (!trackManager.runner && runnerMotor) trackManager.runner = runnerMotor.transform;
                if (!trackManager.groundTilePrefab && groundTilePrefab) trackManager.groundTilePrefab = groundTilePrefab;
            }
        }
    }
}

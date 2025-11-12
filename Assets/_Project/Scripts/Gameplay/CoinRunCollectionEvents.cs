using System;

namespace ShadowRunners.Gameplay
{
    /// Static event hub for per-run coin collection.
    public static class CoinRunCollectionEvents
    {
        public static event Action<int> OnCoinCollectedForRun; // arg: runId

        public static void ReportCollected(int runId)
        {
            if (runId < 0) return;
            OnCoinCollectedForRun?.Invoke(runId);
        }
    }
}

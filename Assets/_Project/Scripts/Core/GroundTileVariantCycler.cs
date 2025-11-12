using UnityEngine;

namespace ShadowRunners.Track
{
    /// <summary>
    /// Enables exactly one visual "variant" group per spawned tile,
    /// cycling in blocks (e.g., 5 A, then 5 B, then 5 C, repeat).
    /// Does not destroy or replace the tile; your TrackManager stays happy.
    /// </summary>
    [AddComponentMenu("ShadowRunners/Track/Ground Tile Variant Cycler")]
    public class GroundTileVariantCycler : MonoBehaviour
    {
        [Header("Variant Groups (visuals only)")]
        [Tooltip("Assign 1 GameObject per variant (e.g., Variants/Variant_A, _B, _C). Only visuals go inside these.")]
        public GameObject[] variantGroups;

        [Header("Cycle")]
        [Tooltip("How many consecutive tiles of each variant before switching to the next.")]
        public int countPerVariant = 5;

        [Tooltip("Optional custom order by index into 'variantGroups', e.g. [0,1,2]. Empty = natural order.")]
        public int[] order;

        [Header("Safe Start")]
        [Tooltip("Force the first N spawned tiles (your TrackManager's 'safe' tiles) to use variant 0.")]
        public bool forceFirstSafeToZero = true;

        [Tooltip("How many initial safe tiles your TrackManager spawns.")]
        public int initialSafeTiles = 1;

        // Global running index across tiles so pattern persists even with pooling
        static int s_globalTileIndex = 0;

        public static void ResetSequence() => s_globalTileIndex = 0;

        void OnEnable()
        {
            ApplyVariantForThisTile();
        }

        void ApplyVariantForThisTile()
        {
            if (variantGroups == null || variantGroups.Length == 0) return;

            // Build order sequence
            int[] seq;
            if (order != null && order.Length > 0)
            {
                seq = new int[order.Length];
                for (int i = 0; i < order.Length; i++)
                    seq[i] = Mathf.Clamp(order[i], 0, variantGroups.Length - 1);
            }
            else
            {
                seq = new int[variantGroups.Length];
                for (int i = 0; i < seq.Length; i++) seq[i] = i;
            }

            int tileIdx = s_globalTileIndex;
            int chosenVariantIndex = 0;

            if (forceFirstSafeToZero && tileIdx < Mathf.Max(0, initialSafeTiles))
            {
                chosenVariantIndex = 0;
            }
            else
            {
                int afterSafe = Mathf.Max(0, tileIdx - Mathf.Max(0, initialSafeTiles));
                int block = Mathf.Max(1, countPerVariant);
                int blockIndex = afterSafe / block;            // 0,1,2,…
                int seqIndex = blockIndex % seq.Length;      // wrap within sequence
                chosenVariantIndex = seq[seqIndex];
            }

            // Enable exactly one, disable the others
            for (int i = 0; i < variantGroups.Length; i++)
            {
                if (!variantGroups[i]) continue;
                variantGroups[i].SetActive(i == chosenVariantIndex);
            }

            s_globalTileIndex++;
        }
    }
}

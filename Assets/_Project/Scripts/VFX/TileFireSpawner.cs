using System.Collections.Generic;
using UnityEngine;

namespace ShadowRunners.VFX
{
    /// <summary>
    /// Spawns a SMALL, RANDOM subset of anchors per tile to avoid "lanes".
    /// - Optionally skips whole tiles (enableThisTileChance)
    /// - Picks between [minPerTile..maxPerTile] anchors total
    /// - Adds slight Z jitter so rows don't align across tiles
    /// - Picks a random prefab per anchor (from firePrefabs)
    /// </summary>
    public class TileFireSpawner : MonoBehaviour
    {
        [Header("Anchors (drag FireAnchor_* here)")]
        public Transform[] anchors;

        [Header("Fire Prefabs (distinct PROJECT prefabs)")]
        public GameObject[] firePrefabs;

        [Header("Tile Spawn Rules")]
        [Tooltip("Chance this tile spawns ANY fires at all.")]
        [Range(0f, 1f)] public float enableThisTileChance = 0.75f;

        [Tooltip("Number of anchors to use on this tile (random in range).")]
        public Vector2Int perTileCount = new Vector2Int(1, 3);

        [Tooltip("Avoid picking adjacent anchors by shuffling and picking sparse indices.")]
        public bool avoidAdjacency = true;

        [Header("Per-Anchor Variation")]
        public Vector2 yOffset = new Vector2(0.00f, 0.12f);
        [Tooltip("Small Z jitter breaks straight lines across tiles.")]
        public Vector2 zJitter = new Vector2(-0.6f, 0.6f);
        public Vector2 yawJitter = new Vector2(-25f, 25f);
        public Vector2 scaleJitter = new Vector2(0.9f, 1.12f);

        [Header("Layer/Tag override (safety)")]
        public string setTag = "Untagged";
        public string setLayer = "Decor";   // create this layer if you don't have it

        [Header("Debug")]
        public bool logInEditor = false;

        void Start()
        {
            if (anchors == null || anchors.Length == 0) return;
            if (firePrefabs == null || firePrefabs.Length == 0) return;

            if (Random.value > enableThisTileChance) return;

            // Build a shuffled index list of anchors
            var idx = new List<int>(anchors.Length);
            for (int i = 0; i < anchors.Length; i++) idx.Add(i);
            Shuffle(idx);

            // Optionally de-densify by skipping every second index after shuffle
            if (avoidAdjacency && idx.Count > 2)
            {
                var sparse = new List<int>();
                for (int i = 0; i < idx.Count; i += 2) sparse.Add(idx[i]);
                idx = sparse;
            }

            int toSpawn = Mathf.Clamp(Random.Range(perTileCount.x, perTileCount.y + 1), 0, idx.Count);
            int spawned = 0;

            for (int i = 0; i < idx.Count && spawned < toSpawn; i++)
            {
                var a = anchors[idx[i]];
                if (!a) continue;

                var prefab = PickValidPrefab();
                if (!prefab) continue;

                var inst = Instantiate(prefab, a.position, a.rotation, a);

                // Nudge pose
                var lp = inst.transform.localPosition;
                lp.y += Random.Range(yOffset.x, yOffset.y);
                lp.z += Random.Range(zJitter.x, zJitter.y);
                inst.transform.localPosition = lp;

                inst.transform.localRotation = Quaternion.Euler(0f, Random.Range(yawJitter.x, yawJitter.y), 0f);

                float s = Random.Range(scaleJitter.x, scaleJitter.y);
                inst.transform.localScale = new Vector3(s, s, s);

                // Safety layer/tag on root and children
                if (!string.IsNullOrEmpty(setTag)) inst.tag = setTag;
                if (!string.IsNullOrEmpty(setLayer))
                {
                    int layer = LayerMask.NameToLayer(setLayer);
                    if (layer >= 0) SetLayerRecursive(inst, layer);
                }

                spawned++;
            }

#if UNITY_EDITOR
            if (logInEditor && spawned > 0)
                Debug.Log($"[TileFireSpawner] Spawned {spawned}/{anchors.Length} fires (tile {name})");
#endif
        }

        GameObject PickValidPrefab()
        {
            // pick until we find a non-null project prefab (avoid scene instances)
            for (int tries = 0; tries < 8; tries++)
            {
                var p = firePrefabs[Random.Range(0, firePrefabs.Length)];
                if (!p) continue;
#if UNITY_EDITOR
                // Warn if you accidentally dragged a scene object
                if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(p))
                    Debug.LogWarning($"[TileFireSpawner] '{p.name}' is not a PROJECT prefab asset. Drag from Project window, not Scene.", p);
#endif
                return p;
            }
            return null;
        }

        void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform c in go.transform) SetLayerRecursive(c.gameObject, layer);
        }

        void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}

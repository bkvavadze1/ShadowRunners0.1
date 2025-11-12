using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-10000)]
public class EnsureSingleEventSystem : MonoBehaviour
{
    [Tooltip("If true, marks the surviving EventSystem as persistent across scenes.")]
    public bool makePersistent = false;

    void Awake()
    {
        var systems = FindObjectsOfType<EventSystem>(true);
        if (systems.Length <= 1)
        {
            if (systems.Length == 1 && makePersistent) DontDestroyOnLoad(systems[0].gameObject);
            return;
        }

        // Keep the oldest enabled one, remove others
        EventSystem keep = null;
        float keepT = float.MaxValue;
        foreach (var es in systems)
        {
            var t = es.gameObject.scene.buildIndex >= 0 ? es.gameObject.scene.buildIndex : int.MaxValue;
            if (keep == null || t < keepT) { keep = es; keepT = t; }
        }

        foreach (var es in systems)
        {
            if (es != keep) Destroy(es.gameObject);
        }
        if (makePersistent && keep) DontDestroyOnLoad(keep.gameObject);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class SceneLoadLogger : MonoBehaviour
{
    static SceneLoadLogger _inst;

    void Awake()
    {
        if (_inst) { Destroy(gameObject); return; }
        _inst = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += (s, m) =>
            Debug.Log($"[SceneLoadLogger] LOADED: {s.name} via {m}");
        SceneManager.sceneUnloaded += s =>
            Debug.Log($"[SceneLoadLogger] UNLOADED: {s.name}");
        SceneManager.activeSceneChanged += (a, b) =>
            Debug.Log($"[SceneLoadLogger] ACTIVE: {a.name} -> {b.name}");
    }
}

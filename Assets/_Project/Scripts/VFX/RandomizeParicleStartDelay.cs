using UnityEngine;

namespace ShadowRunners.Decor
{
    /// <summary>
    /// Adds a random StartDelay to all child ParticleSystems at runtime,
    /// so multiple fires never pulse in sync. Apply on fire/explosion prefab roots.
    /// </summary>
    [AddComponentMenu("ShadowRunners/Decor/Randomize Particle Start Delay")]
    public class RandomizeParticleStartDelay : MonoBehaviour
    {
        [Tooltip("Random delay range (seconds).")]
        public Vector2 delayRange = new Vector2(0f, 0.6f);

        void Awake()
        {
            var psList = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in psList)
            {
                var main = ps.main;
                var a = Mathf.Min(delayRange.x, delayRange.y);
                var b = Mathf.Max(delayRange.x, delayRange.y);
                main.startDelay = new ParticleSystem.MinMaxCurve(Random.Range(a, b));
            }
        }
    }
}

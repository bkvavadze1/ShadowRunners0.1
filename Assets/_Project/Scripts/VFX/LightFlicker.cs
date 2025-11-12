using UnityEngine;
public class LightFlicker : MonoBehaviour
{
    public Light L; public float baseIntensity = 2.5f; public float range = 0.6f; public float speed = 10f;
    void Reset() { L = GetComponent<Light>(); if (L) baseIntensity = L.intensity; }
    void Update()
    {
        if (!L) return; float n = Mathf.PerlinNoise(Time.time * speed, 0f);
        L.intensity = baseIntensity + (n - 0.5f) * range;
    }
}

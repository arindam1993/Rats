using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    private float defaultIntensity;
    private float defaultFalloff;

    [Range(0.0f, 100.0f)]
    public float flickerAmount;

    [Range(0.0f, 10.0f)]
    public float flickerSpeed;

    private Light2D light;
    // Start is called before the first frame update
    void Start()
    {
        light = GetComponent<Light2D>();
        defaultIntensity = light.intensity;
        defaultFalloff = light.falloffIntensity;
    }

    // Update is called once per frame
    void Update()
    {
        float x = Time.realtimeSinceStartup * flickerSpeed;
        float y = 135.0f;
        float noiseSample = Mathf.PerlinNoise(x, y) * flickerAmount;
        float currentIntensity = defaultIntensity + noiseSample - (flickerAmount / 2.0f);

        light.intensity = currentIntensity;
    }
}

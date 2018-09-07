using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Liquid Container Manager
 * 
 * This script manages a "liquid volume", handling both fluid spillage and refilling.
 * This script is to be used with an object that utilizes the "LiquidVolume" script
 * and uses the "Custom/LiquidVolume" shader.
 * 
 * Example prefabs using this technique can be found under
 * /Assets/Prefabs/LiquidContainers/ directory.
 */
public class LiquidContainerManager : MonoBehaviour
{
    [Tooltip("Specify the degree in which the object will start spilling")]
    public float spillDegree = 60.0f;
    [Tooltip("Specify how fast to spill/refill in percent per second")]
    public float spillageSpeed = 20.0f;
    [Tooltip("Specify what the max fill amount is")]
    public float maxFillAmount = 1.0f;
    [Tooltip("Specify what the min fill amount is")]
    public float minFillAmount = 0.0f;
    [Tooltip("Specify the current fill amount")]
    public float fillAmount = 0.5f;
    [Tooltip("Specify the liquid volume script")]
    public LiquidVolume liquidVolumeScript;
    [Tooltip("Specify the particle system to simulate spillage")]
    public ParticleSystem spillageFluid;

    private bool isSpilling;
    private long lastUpdateTime;

    // Use this for initialization
    void Start()
    {
        isSpilling = true;
        lastUpdateTime = DateTime.Now.Ticks;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if object should be spilling
        long currentTime = DateTime.Now.Ticks;
        if (isSpilling == false)
        {
            // Fill the fluid
            float refillAmount = ((float)(currentTime - lastUpdateTime) / ((float)TimeSpan.TicksPerSecond) * (spillageSpeed / 100.0f) * (maxFillAmount - minFillAmount));
            fillAmount = Mathf.Min(maxFillAmount, fillAmount + refillAmount);

            // Ensure container is tipped and still has fluid inside
            if ((Vector3.Angle(gameObject.transform.up, Vector3.up) > spillDegree) && (fillAmount > minFillAmount))
            {
                spillageFluid.Play();
                isSpilling = true;
            }
        }

        // Check if object should stop spilling
        if(isSpilling == true)
        {
            // Drain the fluid
            float drainAmount = ((float)(currentTime - lastUpdateTime) / ((float)TimeSpan.TicksPerSecond) * (spillageSpeed / 100.0f) * (maxFillAmount - minFillAmount));
            fillAmount = Mathf.Max(minFillAmount, fillAmount - drainAmount);

            // Check if container is up right or is out of fluid
            if ((Vector3.Angle(gameObject.transform.up, Vector3.up) <= spillDegree) || (fillAmount <= minFillAmount))
            {
                spillageFluid.Stop();
                isSpilling = false;
            }
        }

        liquidVolumeScript.SetVolume(fillAmount);
        lastUpdateTime = currentTime;
    }
}

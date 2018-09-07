using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Liquid Volume
 * 
 * This liquid volume script is in charge providing a low resource intensive way
 * of simulating fluid physics inside a closed container. It emulates sloshes
 * in fluid movement as the container is moved around and tilted using the
 * "Custom/LiquidVolume" shader.
 * 
 * Add this script to a game object whose renderer uses "Custom/LiquidVolume"
 * shader to get started.
 */
public class LiquidVolume : MonoBehaviour
{
    [Tooltip("Specify the degree in which the object will start spilling")]
    public float spillDegree = 60.0f;

    Renderer rend;
    Vector3 lastPos;
    Vector3 velocity;
    Vector3 lastRot;
    Vector3 angularVelocity;
    public float MaxWobble = 0.03f;
    public float WobbleSpeed = 1f;
    public float Recovery = 1f;
    float wobbleAmountX;
    float wobbleAmountZ;
    float wobbleAmountToAddX;
    float wobbleAmountToAddZ;
    float pulse;
    float time = 0.5f;

    // Use this for initialization
    void Start()
    {
        rend = GetComponent<Renderer>();
    }
    private void Update()
    {
        time += Time.deltaTime;
        // decrease wobble over time
        wobbleAmountToAddX = Mathf.Lerp(wobbleAmountToAddX, 0, Time.deltaTime * (Recovery));
        wobbleAmountToAddZ = Mathf.Lerp(wobbleAmountToAddZ, 0, Time.deltaTime * (Recovery));

        // make a sine wave of the decreasing wobble
        pulse = 2 * Mathf.PI * WobbleSpeed;
        wobbleAmountX = wobbleAmountToAddX * Mathf.Sin(pulse * time);
        wobbleAmountZ = wobbleAmountToAddZ * Mathf.Sin(pulse * time);

        // send it to the shader
        rend.material.SetFloat("_WobbleX", wobbleAmountX);
        rend.material.SetFloat("_WobbleZ", wobbleAmountZ);

        // velocity
        velocity = (lastPos - transform.position) / Time.deltaTime;
        angularVelocity = transform.rotation.eulerAngles - lastRot;

        // add clamped velocity to wobble
        wobbleAmountToAddX += Mathf.Clamp((velocity.x + (angularVelocity.z * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);
        wobbleAmountToAddZ += Mathf.Clamp((velocity.z + (angularVelocity.x * 0.2f)) * MaxWobble, -MaxWobble, MaxWobble);

        // keep last position
        lastPos = transform.position;
        lastRot = transform.rotation.eulerAngles;
    }

    /**
     * Check the container's fill amount. Note that the filled and empty value differs
     * drastically by container size and should be measured independently for each object.
     */
    private float ValidVolume(float amount)
    {
        if (amount < 0.0f)
            return 0.0f;
        if (amount > 1.0f)
            return 1.0f;
        return amount;
    }

    // Get the current filled amount
    public float GetVolume()
    {
        return (1.0f - rend.material.GetFloat("_FillAmount"));
    }

    // Set the fill amount
    public void SetVolume(float amount)
    {
        rend.material.SetFloat("_FillAmount", ValidVolume(1.0f - amount));
    }

    // Increment current liquid filled amount by the specified incremental value
    public void FillVolume(float amount)
    {
        float liquidFillAmount = rend.material.GetFloat("_FillAmount") - amount;
        rend.material.SetFloat("_FillAmount", ValidVolume(liquidFillAmount));
    }

    // Decrement current liquid filled amount by the specified decremental value
    public void DrainVolume(float amount)
    {
        float liquidFillAmount = rend.material.GetFloat("_FillAmount") + amount;
        rend.material.SetFloat("_FillAmount", ValidVolume(liquidFillAmount));
    }

}
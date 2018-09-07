using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Fluid Spillage
 * 
 * This script simulates fluid spillage by reducing the amount of liquid in a container
 * when the user tips the container over the specified "spillDegree".
 */
public class FluidSpillage : MonoBehaviour
{
    [Tooltip("Specify the degree in which the object will start spilling")]
    public float spillDegree = 60.0f;

    private ParticleSystem fluidParticles;
    private bool isSpilling;

    // Use this for initialization
    void Start()
    {
        fluidParticles = gameObject.GetComponent<ParticleSystem>();
        isSpilling = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if object should be spilling
        if ( (Vector3.Angle(gameObject.transform.up, Vector3.up) > spillDegree) && isSpilling == false)
        {
            fluidParticles.Play();
            isSpilling = true;
        }

        // Check if object should not be spilling
        if ((Vector3.Angle(gameObject.transform.up, Vector3.up) <= spillDegree) && isSpilling == true)
        {
            fluidParticles.Stop();
            isSpilling = false;
        }
    }
}

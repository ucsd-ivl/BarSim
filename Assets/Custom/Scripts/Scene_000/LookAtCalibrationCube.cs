using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCalibrationCube : MonoBehaviour
{
    public Light foregroundLightSource;
    public Light backgroundLightSource;
    public GameObject frontCube;

    private float foregroundLightRange;
    private float backgroundLightRange;
    private bool hasBeenLookedAt;

    // Use this for initialization
    void Start()
    {
        foregroundLightSource.range = 0.1f;
        backgroundLightSource.enabled = false;
        hasBeenLookedAt = false;

        float lightScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        foregroundLightRange = foregroundLightSource.range * lightScale;
        backgroundLightRange = backgroundLightSource.range * lightScale;
        foregroundLightSource.range = foregroundLightRange;
        backgroundLightSource.range = backgroundLightRange;

        //transform.LookAt(Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {

        // Get a normalize ray of the direction the user's eye is looking at
        FoveInterfaceBase.GazeConvergenceData gazeConvergenceData = FoveInterface.GetGazeConvergence();

        // Determine where the ray hit if it does hit something
        RaycastHit eyeRayHit;
        Physics.Raycast(gazeConvergenceData.ray, out eyeRayHit, Mathf.Infinity);

        // If the ray does hit something, put the cursor at that location
        if ((eyeRayHit.point != Vector3.zero) && (eyeRayHit.collider == frontCube.GetComponent<Collider>()))
        {
            foregroundLightSource.range = foregroundLightRange * 5.0f;
            backgroundLightSource.enabled = true;
            hasBeenLookedAt = true;
        }
        else
        {
            foregroundLightSource.range = foregroundLightRange;
        }
    }

    public bool HasBeenLookedAt()
    {
        return hasBeenLookedAt;
    }
}

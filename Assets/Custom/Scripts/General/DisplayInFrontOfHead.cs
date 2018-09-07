using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Display in Front of Head
 * 
 * Attach this script to any object to place this object directly in front of the
 * user using the specified distance. The object would then move around based on
 * the user's head rotation and stay directly in the front center of the user's
 * head.
 */
public class DisplayInFrontOfHead : MonoBehaviour
{
    [Tooltip("Specify the distance in meters to display object in front of HMD")]
    public float distance;
    [Tooltip("Specify the name of the HMD game object")]
    public string hmdGameObjectName;

    private GameObject hmdGameObject;

    // Use this for initialization
    void Start()
    {
        hmdGameObject = GameObject.Find(hmdGameObjectName);
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion userHeadsetDirection = hmdGameObject.transform.rotation;
        Vector3 userHeadsetPosition = hmdGameObject.transform.position;

        transform.position = userHeadsetPosition + hmdGameObject.transform.forward * distance;
        transform.rotation = userHeadsetDirection;
    }
}

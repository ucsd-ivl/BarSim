using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Circular Progress Bar Selector
 * 
 * This script can be used with a circular progress bar image that will always
 * show the cursor in front of the user. The user can rotate their head around
 * and look at certain objects with a collider to select their choice. The
 * progress bar will fill up to 100% over the duration specified, locking in
 * their response.
 */
public class CircularProgressBarSelector : MonoBehaviour
{
    [Tooltip("Specify an instance to the progress bar foreground's image script")]
    public Image circularProgressBarInstance;
    [Tooltip("Specify the duration to look at to fill 100% of progress bar")]
    public float duration = 3.0f;
    [Tooltip("Specify the name of the HMD game object")]
    public string hmdGameObjectName = "Fove Camera";

    private GameObject hmdGameObject;
    private long beginLookTime;

    private GameObject lookAtItem;

    // Use this for initialization
    void Start()
    {
        hmdGameObject = GameObject.Find(hmdGameObjectName);
        beginLookTime = -1;
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        if (Physics.Raycast(hmdGameObject.transform.position, hmdGameObject.transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
        {
            // If user was not looking at the game object, reset timer
            if (beginLookTime == -1)
                beginLookTime = DateTime.Now.Ticks;

            // Figure out the percentage of the progress bar
            float progressBarPercent = (DateTime.Now.Ticks - beginLookTime) / (duration * TimeSpan.TicksPerSecond);
            progressBarPercent = Math.Min(progressBarPercent, 100.0f);
            circularProgressBarInstance.fillAmount = progressBarPercent;

            // Store what the user is looking at
            lookAtItem = hit.transform.gameObject;
        }
        else
        {
            circularProgressBarInstance.fillAmount = 0.0f;
            beginLookTime = -1;
        }
    }

    // Get the current progress bar's progress percentage (0.0 - 1.0)
    public float GetProgress()
    {
        return circularProgressBarInstance.fillAmount;
    }

    // Get what the current user is looking at
    public GameObject GetCurrentLookedAtItem()
    {
        return lookAtItem;
    }
}

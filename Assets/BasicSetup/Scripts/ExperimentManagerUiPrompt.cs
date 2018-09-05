using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Experiment Manager UI Prompt
 * 
 * This class provides a way to present the user a UI prompt in virtual reality
 * that will always appear in front of the user.
 */
public class ExperimentManagerUiPrompt : MonoBehaviour
{
    [Tooltip("Specify the FOVE camera game object to get direction transform")]
    public GameObject foveCamera;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Quaternion userHeadsetDirection = foveCamera.transform.rotation;
        Vector3 userHeadsetPosition = foveCamera.transform.position;

        transform.position = userHeadsetPosition + foveCamera.transform.forward * 30.0f;
        transform.rotation = userHeadsetDirection;
    }

    // Toggle UI prompt on
    public void TurnOnUiPrompt()
    {
        gameObject.GetComponent<Renderer>().enabled = true;
    }

    // Toggle UI prompt off
    public void TurnOffUiPrompt()
    {
        gameObject.GetComponent<Renderer>().enabled = false;
    }

    // Set message for UI prompt
    public void SetUiPromptMessage(string message)
    {
        ((TextMesh)gameObject.GetComponent(typeof(TextMesh))).text = message;
    }
}

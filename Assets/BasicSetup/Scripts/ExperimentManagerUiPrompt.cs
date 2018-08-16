using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExperimentManagerUiPrompt : MonoBehaviour
{
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

    public void TurnOnUiPrompt()
    {
        gameObject.GetComponent<Renderer>().enabled = true;
    }

    public void TurnOffUiPrompt()
    {
        gameObject.GetComponent<Renderer>().enabled = false;
    }

    public void SetUiPromptMessage(string message)
    {
        ((TextMesh)gameObject.GetComponent(typeof(TextMesh))).text = message;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Follow Head
 * 
 * Attach this script to any object to have it use the same world position
 * and orientation of the head/camera's transform.
 */
public class FollowHead : MonoBehaviour
{
    [Tooltip("Specify the game object name of the experiment manager")]
    public string experimentManagerName = "Experiment Manager";

    private ExperimentManager experimentManager;

    // Use this for initialization
    void Start()
    {
        try
        {
            experimentManager = GameObject.Find(experimentManagerName).GetComponent<ExperimentManager>();
        }
        catch
        {
            Debug.LogError("Unable to link to experiment manager...");
            Application.Quit();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Transform userHeadTransform = experimentManager.getUserHeadTransform();
        transform.position = userHeadTransform.position;
        transform.rotation = userHeadTransform.rotation;
    }
}

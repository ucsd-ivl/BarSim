using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Survey Cube Choices Manager
 * 
 * This script as a manager that handles the procedure of selecting various choices
 * presented in front of them. Each choice must have a collider attached to it.
 * When the user looks at a choice long enough and fill in the progress bar,
 * their choice would be locked in and will be recorded.
 */
public class SurveyCubeChoices_Manager : MonoBehaviour
{
    [Tooltip("Specify the game object name of the experiment manager")]
    public string experimentManagerName;
    [Tooltip("Specify the circular progress bar selector")]
    public CircularProgressBarSelector circularProgressBarSelectorInstance;

    private ExperimentManager experimentManager;
    private bool choiceSelected;

    // Use this for initialization
    void Start()
    {
        choiceSelected = false;

        try
        {
            experimentManager = GameObject.Find(experimentManagerName).GetComponent<ExperimentManager>();
            experimentManager.ToggleAutoSceneChange(false);

            // Set survey in front of user
            transform.position = new Vector3(0.0f, experimentManager.getUserHeadTransform().position.y, transform.position.z);
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
        if(choiceSelected == false)
        {
            if(circularProgressBarSelectorInstance.GetProgress() == 1.0f)
            {
                GameObject selection = circularProgressBarSelectorInstance.GetCurrentLookedAtItem();
                choiceSelected = true;
                experimentManager.ChangeToNextScene();
            }
        }
    }
}

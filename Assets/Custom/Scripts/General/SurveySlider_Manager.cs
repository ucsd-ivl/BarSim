using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/**
 * Survey Slider Manager
 * 
 * This script acts as the manager in charge of the procedure for selecting a value
 * in a progress bar. The user can slide the progress bar around using the track pad
 * on the controller and lock in their response by holding down the index trigger on
 * both of the controllers. The response would be saved into "surveyResult.csv" and
 * this class would initialize loading in the next scene.
 */
public class SurveySlider_Manager : MonoBehaviour
{
    [Tooltip("Get instance to rectangular progress bar script to change progress bar")]
    public ProgressBar rectangularProgressBarInstance;
    [Tooltip("Specify the game object in charge of displaying the percentage user selected")]
    public GameObject userInputUiGameObject;
    [Tooltip("Specify the sensitivity of the progress bar slider")]
    public float sensitivity = 0.5f;
    [Tooltip("Specify the name of the experiment manager game object")]
    public string experimentManagerName;
    [Tooltip("Specify the output file to save the result")]
    public string outputResultFileName = "surveyResult.csv";
    [Tooltip("Specify the TAG associated with the current survey question")]
    public string surveyTag = "Q0";

    private ExperimentManager experimentManager;
    private TextMesh userInputUi;
    private bool choiceSelected;
    private SteamVR_Controller.Device[] controllers;
    private Vector2[] lastTrackPadValues;
    private bool[] lastTouchTrackPad;

    // Use this for initialization
    void Start()
    {
        choiceSelected = false;
        lastTrackPadValues = new Vector2[2];
        lastTouchTrackPad = new bool[2];

        try
        {
            experimentManager = GameObject.Find(experimentManagerName).GetComponent<ExperimentManager>();
            userInputUi = userInputUiGameObject.GetComponent<TextMesh>();
            controllers = experimentManager.GetControllers();
            experimentManager.ToggleAutoSceneChange(false);

            // Set survey one meter in front of user
            transform.position = new Vector3(0.0f, experimentManager.getUserHeadTransform().position.y, 1.0f);
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
        // Do nothing if user already confirmed choice
        if (choiceSelected == true)
            return;

        // See if user is trying to confirm their selection
        if(controllers[0].GetHairTrigger() && controllers[1].GetHairTrigger())
        {
            // Prevent user from clicking too early (there's a cooldown between scene change)
            if (experimentManager.ChangeToNextScene())
            {
                choiceSelected = true;
                SaveResultToFile(experimentManager.GetLoggingDirectory(), (rectangularProgressBarInstance.GetProgress() * 100.0f).ToString());
                return;
            }
        }

        // Update progress bar accordingly
        for (int i = 0; i < 2; i++)
        {
            // Detect if finger is on trackpad
            SteamVR_Controller.Device device = controllers[i];
            if (device.GetTouch(SteamVR_Controller.ButtonMask.Touchpad))
            {
                // Get touch pad value and handle case where user's input was not continuous
                Vector2 touchpad = device.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
                if (lastTouchTrackPad[i] == false)
                    lastTrackPadValues[i] = touchpad;

                // Update progress bar
                float currentProgress = rectangularProgressBarInstance.GetProgress();
                rectangularProgressBarInstance.SetProgress(currentProgress + (touchpad.x - lastTrackPadValues[i].x ) * sensitivity);
                lastTrackPadValues[i] = touchpad;
            }
            lastTouchTrackPad[i] = device.GetTouch(SteamVR_Controller.ButtonMask.Touchpad);
        }

        userInputUi.text = (int)(rectangularProgressBarInstance.GetProgress() * 100.0f) + "%";
    }

    private void SaveResultToFile(string directory, string result)
    {
        // Check to see if a header is needed (file doesn't exist yet)
        string resultFilePath = directory + "/" + outputResultFileName;
        bool headerNeeded = !File.Exists(resultFilePath);

        // Append result to file
        using (StreamWriter outputStream = new StreamWriter(resultFilePath, true))
        {
            if (headerNeeded)
                outputStream.WriteLine("Question,Response");

            outputStream.WriteLine(surveyTag + "," + result);
        }
    }
}

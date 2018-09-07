using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scene_000_Manager : MonoBehaviour
{
    [Tooltip("Specify the parent of all the calibration objects to check")]
    public GameObject calibrationObjects;
    [Tooltip("Specify the game object name of the experiment manager")]
    public string experimentManagerName;

    private static readonly string EYE_TRACKING_TEST_UI_MESSAGE = 
        "Look at all the boxes to turn on the lights.\n" +
        "Be sure to look all the way around you.";

    private static readonly string CONTROLLER_TEST_UI_MESSAGE =
        "Great!\n" +
        "Press both triggers under your index fingers to start.";

    private static readonly string COUNTDOWN_TIMER_MESSAGE =
        "Experiment starting in\n";

    private enum SCENE_STATES
    {
        EyeTrackingTest,
        ControllerTest,
        SceneChangeCountdown,
        RequestingNextScene,
        Done
    }

    private TextMesh uiPrompt;
    private ExperimentManager experimentManager;
    private SCENE_STATES currentSceneState;
    private long sceneChangeCountdownTimer;

    // Use this for initialization
    void Start()
    {
        currentSceneState = SCENE_STATES.EyeTrackingTest;
        uiPrompt = gameObject.GetComponent<TextMesh>();
        sceneChangeCountdownTimer = -1;

        try
        {
            experimentManager = GameObject.Find(experimentManagerName).GetComponent<ExperimentManager>();
        }
        catch
        {
            Debug.LogError("Unable to link to experiment manager...");
            Application.Quit();
        }
        Debug.Log("Started scene 000 experiment manager");
    }

    // Update is called once per frame
    void Update()
    {
        switch(currentSceneState)
        {
            case SCENE_STATES.EyeTrackingTest:
                CheckGazeTestStatus();
                break;

            case SCENE_STATES.ControllerTest:
                CheckControllerTestStatus();
                break;

            case SCENE_STATES.SceneChangeCountdown:
                SceneChangeCountdown();
                break;

            case SCENE_STATES.RequestingNextScene:
                experimentManager.ChangeToNextScene();
                currentSceneState = SCENE_STATES.Done;
                break;

            case SCENE_STATES.Done:
                break;
        }
    }

    private void CheckGazeTestStatus()
    {
        uiPrompt.text = EYE_TRACKING_TEST_UI_MESSAGE;

        bool testResult = true;
        foreach(Transform child in calibrationObjects.transform) {
            LookAtCalibrationCube calibrationCube = child.gameObject.GetComponent<LookAtCalibrationCube>();
            testResult = testResult && calibrationCube.HasBeenLookedAt();
        }

        experimentManager.ToggleEyeCursor( !testResult );
        currentSceneState = (testResult) ? SCENE_STATES.ControllerTest : currentSceneState;
    }

    private void CheckControllerTestStatus()
    {
        uiPrompt.text = CONTROLLER_TEST_UI_MESSAGE;

        bool testResult = true;
        SteamVR_Controller.Device[] controllers = experimentManager.GetControllers();
        foreach(SteamVR_Controller.Device controller in controllers)
            testResult = (testResult && controller.GetHairTrigger());

        if (testResult == true)
            currentSceneState = SCENE_STATES.SceneChangeCountdown;
    }

    private void SceneChangeCountdown()
    {
        // Initialize countdown timer if we have not
        if (sceneChangeCountdownTimer == -1)
            sceneChangeCountdownTimer = DateTime.Now.Ticks + 10 * TimeSpan.TicksPerSecond;

        // Update UI Prompt
        long secondsUntilChange = (sceneChangeCountdownTimer - DateTime.Now.Ticks) / TimeSpan.TicksPerSecond;
        uiPrompt.text = COUNTDOWN_TIMER_MESSAGE + secondsUntilChange;

        // Change scene if time is exceeded
        if (DateTime.Now.Ticks > sceneChangeCountdownTimer)
            currentSceneState = SCENE_STATES.RequestingNextScene;
    }
}

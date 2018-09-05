using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/**
 * Experiment Manager
 * 
 * This class is responsible for managing the various scenes in the experiment, logging data,
 * and providing an abstract layer for scripts within various scenes to obtain data about the
 * scene, headset, and logging information. This class operates in the background and should
 * be placed in a scene that remains active across the entire experiment session.
 * 
 * Scene Setup: 
 *   
 *   To automate loading and unloading scenes, each scene must be labeled in the format of
 *   "Scene_" + 3 digit representing scene number (e.g. "Scene_007"). This experiment manager
 *   will start out with "Scene_000", followed by "Scene_001", "Scene_002", ..., until it
 *   reaches the "totalNumberOfScenes" specified.
 *   
 *   Upon loading in a new scene, it will attempt to find the skybox associated with that scene.
 *   This skybox should be saved as a Cubemap located under "/Custom/SkyBox/Resources/" with the
 *   same name format as the scene ("Scene_XXX"). If no skybox is detected, it will load in the
 *   specified "defaultSkyBoxMaterial".
 *
 * 
 * Note: This script requires the "Fove Vive Setup" prefab be included in the same scene to work.
 * Note: For this to work properly, please unload all scenes except the scene with this experiment
 *       manager prior to starting the application.
 * 
 * Keycode:
 *   N - Change to next scene
 */
public class ExperimentManager : MonoBehaviour
{
    [Header("Experiment Settings")]
    [Tooltip("Specify if we should request the user to enter test subject name at start")]
    public bool requestTestSubjectName = true;
    [Tooltip("Specify the directory to log data relative to this application's home directory")]
    public string logRootDirectory = "ExperimentData";

    [Header("Scene Settings")]
    [Tooltip("Specify how many total scenes are there total")]
    public uint totalNumberOfScenes;
    [Tooltip("Number of seconds to stay on each scene")]
    public uint secondsBetweenScene;
    [Tooltip("The fade out transition duration in seconds")]
    public uint fadeOutDuration;
    [Tooltip("The cooldown time between manually requesting scene change in seconds")]
    public uint sceneChangeCooldownDuration = 1;

    [Header("Required Linked Components")]
    [Tooltip("Link to an instance of the eye tracking's script")]
    public EyeTracker eyeTrackerInstance;
    [Tooltip("Instance to script controlling camera opacity")]
    public CameraFade cameraFadeInstance;
    [Tooltip("Instance to headset localization script")]
    public UpdateFoveFromViveTracker headsetLocalizationInstance;
    [Tooltip("Instance to UI Prompt for Experiment Manager")]
    public ExperimentManagerUiPrompt uiPrompt;
    [Tooltip("Get a game object instance of the headset for position/orientation data")]
    public GameObject cameraGameObject;
    [Tooltip("Get the input field for user name of subject")]
    public InputField usernameInputField;
    [Tooltip("Instance of the username input field")]
    public GameObject usernameInputGameObject;

    [Header("Skybox Specifications")]
    [Tooltip("Set texture directory")]
    public string textureDirectory = "Assets/SkyBox";
    [Tooltip("Default texture if cannot find specified")]
    public Material defaultSkyboxMaterial;
    [Tooltip("Set the skybox texture to know what to change")]
    public Material skyboxMaterial;

    private enum SCENE_CHANGE_STATE
    {
        NotChanging,
        FadeOut,
        InitiateChange,
        Changing,
        FadeIn
    }

    private bool systemCalibrated = false;
    private bool eyeTrackerCalibrationStarted = false;

    private string experimentStartTime;
    private string logDirectory;
    private string testSubjectName;
    private int currentSceneNumber;
    private long nextSceneTime = long.MaxValue;
    private long fadeSceneTime = long.MaxValue;
    private long nextAllowedSceneChangeRequestTime = 0;
    private SCENE_CHANGE_STATE currentSceneState;

    private Vector3 scenePositionShiftAmount;

    // DEBUG: Set true to skip headset calibration and calibration scene
    private bool skipIntro = false;

    /**
     * Initialize the experiment
     * 
     * This function will initiate a setup procedure for the fove vive setup, which includes
     * calibrating the FOVE headset to vive space and calibrating FOVE's eye tracking. It
     * will also create a logging instance to track which object the user is looking at.
     */
    void Start()
    {
        // Initialize variables
        systemCalibrated = false;
        eyeTrackerCalibrationStarted = false;
        currentSceneNumber = 0;
        currentSceneState = SCENE_CHANGE_STATE.NotChanging;
        nextAllowedSceneChangeRequestTime = 0;
        testSubjectName = "TestSubject";

        // Create logging directory
        experimentStartTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        logDirectory = Directory.GetParent(Application.dataPath) + "/" + logRootDirectory + "/" + experimentStartTime;
        Directory.CreateDirectory(logDirectory);

        nextSceneTime = long.MaxValue;
        if (skipIntro)
            systemCalibrated = true;
    }

    /**
     * Set up a callback for user to enter in experiment username at start up
     */
    private void Awake()
    {
        // Initialize callbacks
        usernameInputGameObject.SetActive(requestTestSubjectName);
        usernameInputField.onEndEdit.AddListener(delegate { UsernameInputFieldCallback(usernameInputField); });
    }

    // Update is called once per frame
    void Update()
    {
        // Ensure everything is calibrated before starting experiment
        if (systemCalibrated == false)
        {
            RunCalibrationSetup();
            return;
        }

        // Ensure that user has entered in the subject's name
        if (usernameInputGameObject.activeInHierarchy)
            return;

        // Debug purposes if skipIntro == TRUE. Allow to skip calibration
        if (skipIntro && (currentSceneNumber == 0))
        {
            currentSceneNumber = 1;
            uiPrompt.TurnOffUiPrompt();
            eyeTrackerInstance.CreateNewScene(GenerateSceneName(currentSceneNumber));
            SceneManager.LoadSceneAsync(GenerateSceneName(currentSceneNumber), LoadSceneMode.Additive);
            ChangeSkybox(currentSceneNumber);
            currentSceneState = SCENE_CHANGE_STATE.NotChanging;
            nextSceneTime = DateTime.Now.Ticks + secondsBetweenScene * TimeSpan.TicksPerSecond;
        }

        // Check if we should begin transition onto the next scene
        if (DateTime.Now.Ticks >= nextSceneTime || currentSceneState != SCENE_CHANGE_STATE.NotChanging)
        {
            switch (currentSceneState)
            {
                // Initiate change scene if we're not at the last scene
                case SCENE_CHANGE_STATE.NotChanging:
                    if (currentSceneNumber < totalNumberOfScenes)
                    {
                        eyeTrackerInstance.SaveToFile(logDirectory);
                        fadeSceneTime = nextSceneTime;
                        currentSceneState = SCENE_CHANGE_STATE.FadeOut;
                    }
                    break;

                // Fade scene out
                case SCENE_CHANGE_STATE.FadeOut:
                    float fadeOutOpacity = 1.0f - ((float)(DateTime.Now.Ticks - fadeSceneTime)) / ((float)(fadeOutDuration * TimeSpan.TicksPerSecond));
                    fadeOutOpacity = Math.Max(0.0f, fadeOutOpacity);
                    cameraFadeInstance.setOpacity(fadeOutOpacity);
                    if (fadeOutOpacity == 0.0f)
                        currentSceneState = SCENE_CHANGE_STATE.InitiateChange;
                    break;

                // Tell program to start changing scene
                case SCENE_CHANGE_STATE.InitiateChange:
                    headsetLocalizationInstance.ReCenterUser();
                    SceneManager.UnloadSceneAsync(GenerateSceneName(currentSceneNumber));
                    currentSceneNumber++;
                    if (currentSceneNumber < totalNumberOfScenes)
                    {
                        SceneManager.LoadSceneAsync(GenerateSceneName(currentSceneNumber), LoadSceneMode.Additive);
                        ChangeSkybox(currentSceneNumber);
                        currentSceneState = SCENE_CHANGE_STATE.Changing;
                    }
                    else
                    {
                        ChangeSkybox(-1);
                        uiPrompt.SetUiPromptMessage("Session has ended.");
                        uiPrompt.TurnOnUiPrompt();
                        fadeSceneTime = DateTime.Now.Ticks;
                        currentSceneState = SCENE_CHANGE_STATE.FadeIn;
                    }
                    break;

                // Wait for scene to finish loading
                case SCENE_CHANGE_STATE.Changing:
                    if (SceneManager.GetSceneByName(GenerateSceneName(currentSceneNumber)).isLoaded)
                    {
                        fadeSceneTime = DateTime.Now.Ticks;
                        currentSceneState = SCENE_CHANGE_STATE.FadeIn;
                    }
                    break;

                // Fade back in
                case SCENE_CHANGE_STATE.FadeIn:
                    float fadeInOpacity = ((float)(DateTime.Now.Ticks - fadeSceneTime)) / ((float)(fadeOutDuration * TimeSpan.TicksPerSecond));
                    fadeInOpacity = Math.Min(1.0f, fadeInOpacity);
                    cameraFadeInstance.setOpacity(fadeInOpacity);
                    if (fadeInOpacity == 1.0f)
                    {
                        eyeTrackerInstance.CreateNewScene(GenerateSceneName(currentSceneNumber));
                        currentSceneState = SCENE_CHANGE_STATE.NotChanging;
                        if (nextSceneTime <= DateTime.Now.Ticks)
                            nextSceneTime = DateTime.Now.Ticks + secondsBetweenScene * TimeSpan.TicksPerSecond;
                    }
                    break;
            }
        }

        // Allow change to next scene
        if (Input.GetKeyDown(KeyCode.N))
            ChangeToNextScene();
    }

    /**
     * Helper function for calibration setup.
     * This will perform the following procedures
     * 
     *    1) Initialize eye tracking calibration
     *    2) Ensure Vive Tracker for Headset and Vive Controllers are connected
     *    3) Prompt to calibrate FOVE headset to Vive space (Press H)
     *    4) Load initial scene (Scene_000) if everything is completed
     */
    private void RunCalibrationSetup()
    {
        if (eyeTrackerCalibrationStarted == false)
        {
            FoveInterface.EnsureEyeTrackingCalibration();
            eyeTrackerCalibrationStarted = true;
        }

        if (RenderSettings.skybox != defaultSkyboxMaterial)
            RenderSettings.skybox = defaultSkyboxMaterial;

        bool eyeTrackerCalibrated = (!FoveInterface.IsEyeTrackingCalibrating() && FoveInterface.IsEyeTrackingCalibrated());
        bool headsetCalibrated = headsetLocalizationInstance.IsHeadsetCalibrated();
        bool viveTrackerHeadsetConnected = headsetLocalizationInstance.IsHeadsetPositionTracked();
        bool controllersConnected = headsetLocalizationInstance.IsControllersConnected();

        string uiPromptMessage = "";
        if (viveTrackerHeadsetConnected == false) uiPromptMessage += "Please turn on Vive Tracker for headset.\n";
        if (controllersConnected == false) uiPromptMessage += "Please connect both Vive controllers.\n";
        if (eyeTrackerCalibrated == false) uiPromptMessage += "Please calibrate eye tracking.\n";
        if (headsetCalibrated == false) uiPromptMessage += "Please calibrate headset and vive to world space [H]\n";
        uiPrompt.SetUiPromptMessage(uiPromptMessage);
        uiPrompt.TurnOnUiPrompt();

        if (eyeTrackerCalibrated && headsetCalibrated)
        {
            systemCalibrated = true;
            uiPrompt.TurnOffUiPrompt();

            // Initialize a new scene
            headsetLocalizationInstance.ReCenterUser();
            eyeTrackerInstance.CreateNewScene(GenerateSceneName(currentSceneNumber));
            SceneManager.LoadSceneAsync(GenerateSceneName(currentSceneNumber), LoadSceneMode.Additive);
            ChangeSkybox(currentSceneNumber);
            currentSceneState = SCENE_CHANGE_STATE.Changing;
        }
    }


    // Callback to log experiment username
    private void UsernameInputFieldCallback(InputField input)
    {
        // Get the username and turn off prompt
        if (input.text.Length > 0)
            testSubjectName = input.text;
        else
            testSubjectName = input.placeholder.GetComponent<Text>().text;
        usernameInputGameObject.SetActive(false);

        // Save key-value pair into properties file
        using (StreamWriter propertyFile = new StreamWriter(logDirectory + "/properties.txt", true))
            propertyFile.WriteLine("username : " + testSubjectName);
    }

    // Change skybox based on the current scene
    private void ChangeSkybox(int sceneNumber)
    {
        Debug.Log("Loading in scene " + sceneNumber); ;

        // Set universal skybox
        Cubemap newSkyBox = Resources.Load<Cubemap>(GenerateSceneName(currentSceneNumber));
        if (newSkyBox != null)
        {
            skyboxMaterial.SetTexture("_Tex", newSkyBox);
            RenderSettings.skybox = skyboxMaterial;
        }
        else
            RenderSettings.skybox = defaultSkyboxMaterial;
    }

    // Generate a unique hash for the current scene
    private string GenerateSceneName(int sceneNumber)
    {
        return "Scene_" + sceneNumber.ToString("000");
    }

    // Change to the next scene if current scene is fully loaded
    public bool ChangeToNextScene()
    {
        if (nextAllowedSceneChangeRequestTime < DateTime.Now.Ticks)
        {
            if (currentSceneState == SCENE_CHANGE_STATE.NotChanging)
            {
                nextSceneTime = DateTime.Now.Ticks;
                nextAllowedSceneChangeRequestTime = DateTime.Now.Ticks + sceneChangeCooldownDuration * TimeSpan.TicksPerSecond;
                return true;
            }
        }
        return false;
    }

    // Return the transforms for the user's head
    public Transform getUserHeadTransform()
    {
        return cameraGameObject.transform;
    }

    /**
     * This is to toggle automatic scene change for the *current* scene only.
     * If this function is not called, it is default to TRUE for automatic scene change.
     */
    public void ToggleAutoSceneChange(bool automatic)
    {
        if (automatic)
            nextSceneTime = DateTime.Now.Ticks + secondsBetweenScene * TimeSpan.TicksPerSecond;
        else
            nextSceneTime = long.MaxValue;
    }

    // Toggle to show or don't show the eye tracking cursor / text
    public void ToggleEyeCursor(bool show)
    {
        eyeTrackerInstance.renderCursor = show;
    }

    // Get an instance of the steam controllers to attach callback
    public SteamVR_Controller.Device[] GetControllers()
    {
        return headsetLocalizationInstance.GetControllers();
    }

    // Get the controller's current transform
    public Transform[] GetControllerTransforms()
    {
        return headsetLocalizationInstance.GetControllerTransforms();
    }

    // Teleport a user to a certain location in world space. Will only change X and Z coordinates
    public void TeleportUser(Vector3 newLocation)
    {
        headsetLocalizationInstance.TeleportUser(newLocation);
    }

    // Return the root logging directory
    public string GetLoggingDirectory()
    {
        return logDirectory;
    }

    // Return the username of the current experiment
    public string GetTestSubjectName()
    {
        return testSubjectName;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperimentManager : MonoBehaviour
{

    /**
     * Note: Scene_00 is the default waiting scene. It will only move
     *       onto the next scene when setup is complete.
     */

    [Header("Scene Settings")]
    [Tooltip("Specify how many total scenes are there total")]
    public uint totalNumberOfScenes;
    [Tooltip("Number of seconds to stay on each scene")]
    public uint secondsBetweenScene;
    [Tooltip("The fade out transition duration in seconds")]
    public uint fadeOutDuration;

    [Header("Required Linked Components")]
    [Tooltip("Link to an instance of the eye tracking's script")]
    public EyeTracker eyeTrackerInstance;
    [Tooltip("Instance to script controlling camera opacity")]
    public CameraFade cameraFadeInstance;

    [Header("Skybox Specifications")]
    [Tooltip("Set texture directory")]
    public string textureDirectory = "Assets/SkyBox";
    [Tooltip("Default texture if cannot find specified")]
    public Material defaultSkyboxMaterial;
    [Tooltip("Set the skybox texture to know what to change")]
    public Material skyboxMaterial;
    [Tooltip("Set the skybox textures to use")]
    public Cubemap[] skyboxCubemap;

    enum SCENE_CHANGE_STATE
    {
        NotChanging,
        FadeOut,
        InitiateChange,
        Changing,
        FadeIn
    }

    private int currentSceneNumber;
    private long nextSceneTime = long.MaxValue;
    private SCENE_CHANGE_STATE currentSceneState;

    private Vector3 scenePositionShiftAmount;

    private Queue fpsTracker;
    private long lastUpdateTimeNs;
    private double fps;

    // Use this for initialization
    void Start()
    {
        fpsTracker = new Queue();
        lastUpdateTimeNs = DateTime.Now.Ticks;
        fps = 0;

        // Initialize variables
        currentSceneNumber = 0;
        currentSceneState = SCENE_CHANGE_STATE.NotChanging;

        // Unload all scenes
        int totalScenesKnown = SceneManager.sceneCount;
        for (int i = 0; i < totalScenesKnown; i++)
        {
            Scene scene = SceneManager.GetSceneByBuildIndex(i);
            if (scene.isLoaded && scene.name != "Default")
                SceneManager.UnloadSceneAsync(i);
        }

        // Initialize a new scene
        eyeTrackerInstance.CreateNewScene(GenerateSceneName(currentSceneNumber));
        SceneManager.LoadSceneAsync(GenerateSceneName(currentSceneNumber), LoadSceneMode.Additive);
        ChangeSkybox(currentSceneNumber);

        nextSceneTime = DateTime.Now.Ticks + secondsBetweenScene * TimeSpan.TicksPerSecond;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if we should begin transition onto the next scene
        if (DateTime.Now.Ticks > nextSceneTime)
        {
            switch (currentSceneState)
            {
                // Initiate change scene if we're not at the last scene
                case SCENE_CHANGE_STATE.NotChanging:
                    if (currentSceneNumber != totalNumberOfScenes - 1)
                    {
                        eyeTrackerInstance.SaveToFile();
                        currentSceneState = SCENE_CHANGE_STATE.FadeOut;
                    }
                    break;
                
                // Fade scene out
                case SCENE_CHANGE_STATE.FadeOut:
                    float fadeOutOpacity = 1.0f - ((float)(DateTime.Now.Ticks - nextSceneTime)) / ((float)(fadeOutDuration * TimeSpan.TicksPerSecond));
                    fadeOutOpacity = Math.Max(0.0f, fadeOutOpacity);
                    cameraFadeInstance.setOpacity(fadeOutOpacity);
                    if (fadeOutOpacity == 0.0f)
                        currentSceneState = SCENE_CHANGE_STATE.InitiateChange;
                    break;
                
                // Tell program to start changing scene
                case SCENE_CHANGE_STATE.InitiateChange:
                    SceneManager.UnloadSceneAsync(GenerateSceneName(currentSceneNumber));
                    currentSceneNumber++;
                    SceneManager.LoadSceneAsync(GenerateSceneName(currentSceneNumber), LoadSceneMode.Additive);
                    ChangeSkybox(currentSceneNumber);
                    eyeTrackerInstance.CreateNewScene(GenerateSceneName(currentSceneNumber));
                    currentSceneState = SCENE_CHANGE_STATE.Changing;
                    break;

                // Wait for scene to finish loading
                case SCENE_CHANGE_STATE.Changing:
                    if (SceneManager.GetSceneByName(GenerateSceneName(currentSceneNumber)).isLoaded)
                    {
                        nextSceneTime = DateTime.Now.Ticks;
                        currentSceneState = SCENE_CHANGE_STATE.FadeIn;
                    }
                    break;
                
                // Fade back in
                case SCENE_CHANGE_STATE.FadeIn:
                    float fadeInOpacity = ((float)(DateTime.Now.Ticks - nextSceneTime)) / ((float)(fadeOutDuration * TimeSpan.TicksPerSecond));
                    fadeInOpacity = Math.Min(1.0f, fadeInOpacity);
                    cameraFadeInstance.setOpacity(fadeInOpacity);
                    if (fadeInOpacity == 1.0f)
                    {
                        currentSceneState = SCENE_CHANGE_STATE.NotChanging;
                        nextSceneTime = DateTime.Now.Ticks + secondsBetweenScene * TimeSpan.TicksPerSecond;
                    }
                    break;
            }
        }

        if (Input.GetKeyDown(KeyCode.N))
        {

            SceneManager.UnloadSceneAsync(GenerateSceneName(currentSceneNumber));
            SceneManager.LoadSceneAsync(GenerateSceneName(currentSceneNumber + 1), LoadSceneMode.Additive);
            currentSceneNumber++;
            ChangeSkybox(currentSceneNumber);
            eyeTrackerInstance.CreateNewScene(GenerateSceneName(currentSceneNumber));

            Scene scene = SceneManager.GetActiveScene();
            Debug.Log("Active scene is '" + scene.name + "'.");
        }

        // For debugging purposes, keep track of FPS
        long timeBetweenFrameNs = DateTime.Now.Ticks - lastUpdateTimeNs;
        fps += (double) TimeSpan.TicksPerSecond / (double) timeBetweenFrameNs / 100.0;
        fpsTracker.Enqueue(timeBetweenFrameNs);
        if( fpsTracker.Count > 100 )
            fps -= (double)TimeSpan.TicksPerSecond / (double)(long) fpsTracker.Dequeue() / 100.0;
        lastUpdateTimeNs = DateTime.Now.Ticks;
        //Debug.Log("FPS: " + fps );
    }

    private void ChangeSkybox(int sceneNumber)
    {
        Debug.Log("Loading in scene " + sceneNumber); ;
        if (sceneNumber < skyboxCubemap.Length)
        {
            // Set universal skybox
            skyboxMaterial.SetTexture("_Tex", skyboxCubemap[currentSceneNumber]);
            RenderSettings.skybox = skyboxMaterial;

            // Set stereoscopic skybox -- left is default skybox. Change right skybox only 
            /*
            if (sceneNumber >= 0)
            {
                if (GameObject.Find("FOVE Eye (Right)").GetComponent<Skybox>() == null)
                    GameObject.Find("FOVE Eye (Right)").AddComponent<Skybox>();
                Camera camRight = GameObject.Find("FOVE Eye (Right)").GetComponent<Camera>();
                camRight.clearFlags = CameraClearFlags.Skybox;
                camRight.GetComponent<Skybox>().material = defaultSkyboxMaterial;// Resources.Load("Material/New_Material", typeof(Material)) as Material;
            }
            else
            {
                if (GameObject.Find("FOVE Eye (Right)").GetComponent<Skybox>() != null)
                    Destroy(GameObject.Find("FOVE Eye (Right)").GetComponent<Skybox>());
            }*/
        }

        // If invalid scene, just use default skybox
        else
        {
            RenderSettings.skybox = defaultSkyboxMaterial;
        }
    }

    private string GenerateSceneName(int sceneNumber)
    {
        return "Scene_" + sceneNumber.ToString("000");
    }
}

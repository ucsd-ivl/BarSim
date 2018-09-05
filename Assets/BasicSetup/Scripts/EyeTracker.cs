using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Eye Tracker
 * 
 * This class is responsible for performing all eye gaze/blink tracking throughout the various
 * scenes in the experiment. It is capable of tracking where the user is looking (whether that
 * be at an object or somewhere in the skybox), for how long, as well as keeping track of when
 * the user blinks. It has an "EyeTrackingLogger" helper class that will deal with saving the
 * data at the end of each scene.
 *
 *
 * Tracking objects within the scene:
 *
 *   To track items within the scene, just add a collider to it. This eye tracker class will
 *   raycast from the headset using the current gaze direction to see if it hit any object's
 *   collider. If it does hit, it will note the object that it hit, as well as the full path
 *   from the root object to the hit object.
 *   
 *   If you need a collider but don't what it to be tracked, please place it in the "Ignore
 *   Raycast" layer.
 *
 *
 * Tracking objects within the skybox:
 *
 *   To track items within the skybox, we would need to come up with an image file in the
 *   format of longitude and latitude, where tracked objects are color coded in the file. We
 *   would need an additional ".txt" file to specify what each pixel value in the labeled
 *   image file means.
 *   
 *   Place the image file as "Scene_XXX.jpg" into "Custom/Labels/".
 *   Place the text file as "Scene_XXX.txt" into "Custom/Labels".
 *   Note: "XXX" is 3 digits representing the current scene.
 *   
 *   Format of the text file:              Example:
 *   ------------------------              ------------------------------------
 *   Label\n                               Skybox, Ceiling, Lights, Light_00
 *   r g b\n                               1.0 0.5 0.5
 *   
 *   Please check GitHub for more examples.
 */
public class EyeTracker : MonoBehaviour
{
    [Tooltip("Toggle on to see cursor pointing to what user is looking at")]
    public bool renderCursor = false;
    [Tooltip("Specify the eye cursor game object")]
    public GameObject eyeCursor;
    [Tooltip("Specify the eye labeler game object")]
    public GameObject eyeLabeler;
    [Tooltip("Specify the FOVE headset game object (The parent of FOVE camera)")]
    public GameObject foveHeadset;

    private string currentScene;

    private const string LABEL_IMAGE = "Assets/Labels/";
    private const string LABEL_TEXT = "Assets/Labels/";

    private long currentEyeBlinkCount;
    private long lastEyeBlinkCount;
    private Fove.Managed.EFVR_Eye lastEyeClosedStatus;

    private Dictionary<Color, List<string>> labelDictionary;
    private EyeTrackingLogger eyeTrackingLogger;
    private Texture2D labelTexture;

    long lastUpdateTimeTicks = DateTime.Now.Ticks;

    // Use this for initialization
    void Start()
    {
        // Create a new instance of dictionaries we'll use
        labelDictionary = new Dictionary<Color, List<string>>();
        eyeTrackingLogger = new EyeTrackingLogger();

        // Initialize variables
        CreateNewScene("Scene_000");
    }

    // Initialize fresh tracking instance for new scene. Read in skybox labels if exist.
    public void CreateNewScene(string sceneName)
    {
        // Create a new instance of dictionaries we'll use
        labelDictionary = new Dictionary<Color, List<string>>();
        eyeTrackingLogger = new EyeTrackingLogger();
        labelTexture = new Texture2D(2, 2);

        // Initialize variables
        currentEyeBlinkCount = 0;
        lastEyeBlinkCount = 0;
        lastEyeClosedStatus = Fove.Managed.EFVR_Eye.Neither;
        currentScene = sceneName;

        // Check if file exists
        if (File.Exists(LABEL_IMAGE + sceneName + ".jpg") == false)
            return;
        if (File.Exists(LABEL_TEXT + sceneName + ".txt") == false)
            return;

        // Read in texture label
        byte[] labelTextureByteStream = File.ReadAllBytes(LABEL_IMAGE + sceneName + ".jpg");
        labelTexture.LoadImage(labelTextureByteStream);

        // Read in the labels
        string[] labels = System.IO.File.ReadAllLines(LABEL_TEXT + sceneName + ".txt");
        for (int i = 0; i < labels.Length; i += 2)
        {
            // Get the path to object and remove trailing/leading white spaces
            string[] pathToObject = labels[i].Split(',');
            for (int k = 0; k < pathToObject.Length; k++)
                pathToObject[k] = pathToObject[k].Trim();

            // Get the color
            string[] colorRGB = labels[i + 1].Split(' ');
            Color labelColor = new Color(
                (float)Convert.ToDecimal(colorRGB[0]),
                (float)Convert.ToDecimal(colorRGB[1]),
                (float)Convert.ToDecimal(colorRGB[2]));

            // Assign to dictionary to keep track
            labelDictionary.Add(labelColor, new List<string>(pathToObject));
        }
    }

    // Update is called once per frame. Figure out what the user is looking at
    void Update()
    {
        // Get the current time
        long currentTimeTicks = DateTime.Now.Ticks;
        List<string> currentLookAtItemPath = null;

        // Get a normalize ray of the direction the user's eye is looking at
        FoveInterfaceBase.GazeConvergenceData gazeConvergenceData = FoveInterface.GetGazeConvergence();

        // Determine where the ray hit if it does hit something
        RaycastHit eyeRayHit;
        Physics.Raycast(gazeConvergenceData.ray, out eyeRayHit, Mathf.Infinity);

        // If the ray does hit something, put the cursor at that location
        if (eyeRayHit.point != Vector3.zero)
        {
            transform.position = eyeRayHit.point;
            currentLookAtItemPath = TransformToObjectPath(eyeRayHit.collider.transform);
        }

        // Else, just set it as a point 3 meters away in the direction of the ray
        // and determine what user is looking at in the skybox
        else
        {
            transform.position = gazeConvergenceData.ray.GetPoint(3.0f);
            Vector3 gazeDirection = gazeConvergenceData.ray.direction;

            // Convert from spherical coordinates to longitude and latitude
            float magnitude = gazeDirection.magnitude;
            float longitude = Mathf.PI - Mathf.Acos(gazeDirection.y / magnitude);      // 0 to PI
            float latitude = Mathf.Atan2(gazeDirection.x, gazeDirection.z) + Mathf.PI; // 0 to 2 * PI

            // Map longitude/latitude over to UV coordinates
            float U = (latitude / (Mathf.PI * 2.0f)) % 1.0f;
            float V = (longitude / Mathf.PI) % 1.0f;

            // See if user is looking at a labeled area
            Color gazeItemColor = labelTexture.GetPixel((int)(U * labelTexture.width), (int)(V * labelTexture.height));

            // Fix any floating point rounding issues -- nearest 0.01
            gazeItemColor.r = (float)Math.Round(gazeItemColor.r, 2);
            gazeItemColor.g = (float)Math.Round(gazeItemColor.g, 2);
            gazeItemColor.b = (float)Math.Round(gazeItemColor.b, 2);

            // Keep track of what the user is looking at
            if (labelDictionary.ContainsKey(gazeItemColor))
                currentLookAtItemPath = labelDictionary[gazeItemColor];
            else
                currentLookAtItemPath = null;
        }

        // Keep track of eye blink
        Fove.Managed.EFVR_Eye eyeClosedStatus = FoveInterface.CheckEyesClosed();
        if ((lastEyeClosedStatus != Fove.Managed.EFVR_Eye.Neither) && (eyeClosedStatus == Fove.Managed.EFVR_Eye.Neither))
            currentEyeBlinkCount++;

        // Keep track of the duration we've looked at that item and blink count
        long totalLookAtDuration = eyeTrackingLogger.UpdateLabels(currentLookAtItemPath,
            currentTimeTicks - lastUpdateTimeTicks, currentEyeBlinkCount - lastEyeBlinkCount);

        // Make sure labeled text is facing user
        transform.LookAt(foveHeadset.transform);
        transform.RotateAround(transform.position, transform.up, 180.0f);

        // Set the labeler to how long we've looked at that item
        string itemName = (currentLookAtItemPath == null) ? "null" : currentLookAtItemPath[currentLookAtItemPath.Count - 1];
        ((TextMesh)eyeLabeler.GetComponent(typeof(TextMesh))).text = itemName +
            Environment.NewLine + (totalLookAtDuration / TimeSpan.TicksPerMillisecond) + " ms";

        // Only render if user specified
        eyeCursor.GetComponent<Renderer>().enabled = renderCursor;
        eyeLabeler.GetComponent<Renderer>().enabled = renderCursor;

        // Update state
        lastUpdateTimeTicks = currentTimeTicks;
        lastEyeClosedStatus = eyeClosedStatus;
        lastEyeBlinkCount = currentEyeBlinkCount;
    }

    // Helper function to get full path from root object to leaf object
    private List<string> TransformToObjectPath(Transform leafObject)
    {
        // Get the path to go from root to object
        List<string> pathToObject = new List<string>();
        Transform currentObject = leafObject;
        while (currentObject != null)
        {
            pathToObject.Add(currentObject.name);
            currentObject = currentObject.parent;
        }
        pathToObject.Reverse();
        return pathToObject;
    }

    // Save the current tracking data to file
    public void SaveToFile(string logDirectory)
    {
        eyeTrackingLogger.SaveToFile(logDirectory, currentScene);
    }

    private void OnApplicationQuit()
    {

    }
}

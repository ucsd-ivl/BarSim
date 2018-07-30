using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EyeTracker : MonoBehaviour
{
    [Tooltip("Toggle on to see cursor pointing to what user is looking at")]
    public bool renderCursor = false;
    public GameObject eyeCursor;
    public GameObject eyeLabeler;
    public GameObject foveHeadset;
    [Tooltip("Specify the directory to log data relative to this application's home directory")]
    public string logDirectory = "ExperimentData";

    private string experimentStartTime;
    private string currentScene;

    private const string LABEL_IMAGE = "Assets/Labels/";
    private const string LABEL_TEXT = "Assets/Labels/";

    private long eyeBlinkCount;
    private Fove.Managed.EFVR_Eye lastEyeClosedStatus;

    private Dictionary<Color, List<string>> labelDictionary;
    private HierarchicalLabel hierarchicalLabels;
    private Texture2D labelTexture;

    long lastUpdateTimeTicks = DateTime.Now.Ticks;

    // Use this for initialization
    void Start()
    {
        // Create a new instance of dictionaries we'll use
        labelDictionary = new Dictionary<Color, List<string>>();
        hierarchicalLabels = new HierarchicalLabel();

        // Initialize variables
        experimentStartTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        CreateNewScene("Scene_000");
    }

    public void CreateNewScene(string sceneName)
    {
        // Create a new instance of dictionaries we'll use
        labelDictionary = new Dictionary<Color, List<string>>();
        hierarchicalLabels = new HierarchicalLabel();
        labelTexture = new Texture2D(2, 2);

        // Initialize variables
        eyeBlinkCount = 0;
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

    // Update is called once per frame
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

        // Keep track of the duration we've looked at that item
        long totalLookAtDuration = hierarchicalLabels.UpdateLabelLookAtTime(
            currentLookAtItemPath, currentTimeTicks - lastUpdateTimeTicks);

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

        // Keep track of eye blink
        Fove.Managed.EFVR_Eye eyeClosedStatus = FoveInterface.CheckEyesClosed();
        if ((lastEyeClosedStatus != Fove.Managed.EFVR_Eye.Neither) && (eyeClosedStatus == Fove.Managed.EFVR_Eye.Neither))
            eyeBlinkCount++;

        // Update state
        lastUpdateTimeTicks = currentTimeTicks;
        lastEyeClosedStatus = eyeClosedStatus;
    }

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

    public void SaveToFile()
    {
        hierarchicalLabels.SaveToFile(logDirectory + "/" + experimentStartTime, currentScene);
    }

    private void OnApplicationQuit()
    {
        
    }
}

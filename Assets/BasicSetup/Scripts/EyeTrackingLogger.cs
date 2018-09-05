using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

/**
 * EyeTrackingLogger
 * 
 * This is a helper class in charge of logging eye tracking data and saving it to a file.
 * This class will create three files: "labeledDataSummary.data", "blink.csv", "lookAt.csv"
 * 
 * ---------------------------------------------------------------------------------------
 * 
 * labeledDataSummary.data
 * 
 *   This is the main summary file storing how long (in milliseconds) and how many
 *   times the user looked at each object in the scene. It keeps the hiearchical aspect
 *   of the data. Please use "parseRawExperimentData.py" to convert this to proper ".csv"
 *   and ".json" format.
 *   
 *   Format: duration,blink_count,label
 *   
 *   Example:
 *     duration,blink_count,label
 *     7983,3,Background
 *     7983,3,Background,ComputerStation
 *     7983,3,Background,ComputerStation,Tables
 *     5062,1,Background,ComputerStation,Tables,Table_2
 *     2920,2,Background,ComputerStation,Tables,Table_3
 *     1801,0,skybox
 *     1801,0,skybox,neutral
 *     1801,0,skybox,neutral,white_board
 *     
 * ---------------------------------------------------------------------------------------
 * 
 * blink.csv
 * 
 *   This tracks when the user blinked and what the user looked at when they blinked
 *   
 *   Format: Time,Item
 *   
 *   Example:
 *     Time,Item
 *     17:44:18.089,Wall,RightWall
 *     17:44:20.058,Wall,RightWall
 *     17:44:20.975,Wall,BackWall
 *     17:44:23.074,Wall,FrontWall
 *   
 * ---------------------------------------------------------------------------------------
 *
 * lookAt.csv
 * 
 *   This tracks when the user switched from looking at one item to another item
 *   
 *   Format: Time,Item
 *   
 *   Example:
 *     Time,Item
 *     17:44:16.589,Floor__
 *     17:44:16.622,Wall,RightWall
 *     17:44:17.239,Calibration Objects,Calibration Object (6),Front Cube
 */
public class EyeTrackingLogger
{
    struct TrackedEyeFeatures
    {
        public long lookAtTime;
        public long blinkCount;
    };

    // Store mapping of hierarchical path to duration (Ticks) looked at
    private Dictionary<string, TrackedEyeFeatures> trackedFeatures;

    private const string SUMMARY_FILE = "labeledDataSummary.data";
    private const string BLINK_TRACKER_FILE = "blink.csv";
    private const string LOOKAT_TRACKER_FILE = "lookAt.csv";

    // TODO: Check for Out Of Memory Exception (using MemorySteam as location)
    private MemoryStream blinkTrackerMemoryStream;
    private MemoryStream lookAtTrackerMemoryStream;
    private StreamWriter blinkTrackerStreamWriter;
    private StreamWriter lookAtTrackerStreamWriter;

    private string lastLookedAtObject;

    // Constructor: Initialize variable
    public EyeTrackingLogger()
    {
        ResetEnvironment();
    }

    // Initializes the environment as if it was new
    private void ResetEnvironment()
    {
        trackedFeatures = new Dictionary<string, TrackedEyeFeatures>();
        lastLookedAtObject = "";

        blinkTrackerMemoryStream = new MemoryStream();
        lookAtTrackerMemoryStream = new MemoryStream();

        blinkTrackerStreamWriter = new StreamWriter(blinkTrackerMemoryStream);
        lookAtTrackerStreamWriter = new StreamWriter(lookAtTrackerMemoryStream);

        blinkTrackerStreamWriter.WriteLine("Time,Item");
        lookAtTrackerStreamWriter.WriteLine("Time,Item");
    }

    // Update the current label. Specify the look at time (milliseconds) and blink count since last update.
    public long UpdateLabels(List<string> pathToObject, long lookAtTime, long blinkCount)
    {
        // If object path is null, create a pseudo-null path
        if (pathToObject == null || pathToObject.Count == 0)
            pathToObject = new List<string> { "null" };

        // Update the hiearchy
        string pathKey = "";
        foreach (string label in pathToObject)
        {
            // If current label doesn't exist, create it
            pathKey += (pathKey == "") ? label : ("," + label);
            if (!trackedFeatures.ContainsKey(pathKey))
            {
                TrackedEyeFeatures newEyeFeatures = new TrackedEyeFeatures
                {
                    lookAtTime = 0,
                    blinkCount = 0
                };
                trackedFeatures.Add(pathKey, newEyeFeatures);
            }

            // Update the time spent looking at it
            TrackedEyeFeatures updatedEyeFeatures = trackedFeatures[pathKey];
            updatedEyeFeatures.lookAtTime += lookAtTime;
            updatedEyeFeatures.blinkCount += blinkCount;
            trackedFeatures[pathKey] = updatedEyeFeatures;
        }

        // Keep track of what user was doing when blinking.
        // As well as when they switch what they're looking at
        try
        {
            string currentTime = DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            if (blinkCount != 0)
                blinkTrackerStreamWriter.WriteLine(currentTime + "," + pathKey);
            if (lastLookedAtObject != pathKey)
                lookAtTrackerStreamWriter.WriteLine(currentTime + "," + pathKey);
        }
        // TODO: Catch and handle out of memory exception
        catch (Exception exception)
        {
            Debug.LogError(exception);
        }

        // Return the time spent looking at that item
        lastLookedAtObject = pathKey;
        return trackedFeatures[pathKey].lookAtTime;
    }

    // Save the result to file
    public void SaveToFile(string filePath, string sceneName)
    {
        // Sort the dictionary by alphanumeric order
        SortedDictionary<string, TrackedEyeFeatures> sortedLabeledDictionary
            = new SortedDictionary<string, TrackedEyeFeatures>(trackedFeatures);

        // Create directory to store the data
        string logDirectory = filePath + "/" + sceneName;
        Directory.CreateDirectory(logDirectory);
        string rawDataFilePath = logDirectory + "/" + SUMMARY_FILE;

        // Iterate through dictionary and save as CSV format
        System.IO.StreamWriter file = new StreamWriter(rawDataFilePath, false);
        file.WriteLine("duration,blink_count,label");
        foreach (var labelData in sortedLabeledDictionary)
        {
            file.WriteLine(
                (labelData.Value.lookAtTime / TimeSpan.TicksPerMillisecond) + "," +
                (labelData.Value.blinkCount) + "," +
                labelData.Key);
        }
        file.Close();

        // Save more precise blink and look at data
        string blinkDataFilePath = logDirectory + "/" + BLINK_TRACKER_FILE;
        string lookAtDataFilePath = logDirectory + "/" + LOOKAT_TRACKER_FILE;
        blinkTrackerStreamWriter.Flush();
        blinkTrackerMemoryStream.WriteTo(new FileStream(blinkDataFilePath, FileMode.Create, FileAccess.Write));
        lookAtTrackerStreamWriter.Flush();
        lookAtTrackerMemoryStream.WriteTo(new FileStream(lookAtDataFilePath, FileMode.Create, FileAccess.Write));
    }

    // Clear all data in the dictionary
    public void ClearData()
    {
        ResetEnvironment();
    }
}

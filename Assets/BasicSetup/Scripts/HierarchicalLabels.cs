using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class HierarchicalLabel
{
    // Store mapping of hierarchical path to duration looked at
    private Dictionary<string, long> labeledDurationDictionary;

    private const string SUMMARY_FILE = "labeledDataSummary";

    // Constructor: Initialize variable
    public HierarchicalLabel()
    {
        labeledDurationDictionary = new Dictionary<string, long>();
    }

    public long UpdateLabelLookAtTime(List<string> pathToObject, long lookAtTime)
    {
        // If object path is null, create a pseudo-null path
        if (pathToObject == null || pathToObject.Count == 0)
            pathToObject = new List<string> { "null" };

        // Update the hiearchy
        string pathKey = "";
        foreach(string label in pathToObject)
        {
            // If current label doesn't exist, create it
            pathKey += (pathKey == "") ? label : ("," + label);
            if (!labeledDurationDictionary.ContainsKey(pathKey))
                labeledDurationDictionary.Add(pathKey, 0);

            // Update the time spent looking at it
            labeledDurationDictionary[pathKey] += lookAtTime;
        }

        // Return the time spent looking at that item
        return labeledDurationDictionary[pathKey];
    }

    // Save the result to file
    public void SaveToFile(string filePath, string sceneName)
    {
        // Sort the dictionary by alphanumeric order
        SortedDictionary<string, long> sortedLabeledDurationDictionary
            = new SortedDictionary<string, long>(labeledDurationDictionary);

        // Create directory to store the data
        filePath = Directory.GetParent(Application.dataPath) + "/" + filePath;
        Directory.CreateDirectory(filePath);
        filePath += "/" + SUMMARY_FILE + "_" + sceneName + ".csv";

        // Iterate through dictionary and save as CSV format
        System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, false);
        file.WriteLine("duration,label");
        foreach (var labelData in sortedLabeledDurationDictionary)
            file.WriteLine(labelData.Value + "," + labelData.Key);
        file.Close();
    }

    // Clear all data in the dictionary
    public void ClearData()
    {
        labeledDurationDictionary = new Dictionary<string, long>();
    }
}

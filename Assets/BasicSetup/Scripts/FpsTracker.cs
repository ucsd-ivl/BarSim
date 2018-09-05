using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * FPS Tracker
 * 
 * This script is meant for debugging purposes. This will show the current FPS text as a
 * UI Text component in the Unity player. The user in the VR headset will not see this text.
 */
public class FpsTracker : MonoBehaviour
{
    [Tooltip("Specify the number of frames to consider when calculating current FPS")]
    public long framesToConsider = 30;

    private Queue fpsTrackerQueue;
    private double fps;
    private long lastUpdateTimeNs;
    private Text fpsText;

    // Use this for initialization
    void Start()
    {
        fps = 0;
        fpsTrackerQueue = new Queue();
        lastUpdateTimeNs = DateTime.Now.Ticks;
        fpsText = gameObject.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        long timeBetweenFrameNs = DateTime.Now.Ticks - lastUpdateTimeNs;
        fps += (double)TimeSpan.TicksPerSecond / (double)timeBetweenFrameNs / (double)framesToConsider;
        fpsTrackerQueue.Enqueue(timeBetweenFrameNs);
        if (fpsTrackerQueue.Count > framesToConsider)
            fps -= (double)TimeSpan.TicksPerSecond / (double)(long)fpsTrackerQueue.Dequeue() / (double)framesToConsider;
        lastUpdateTimeNs = DateTime.Now.Ticks;
        fpsText.text = fps.ToString("00.0") + " FPS";
    }
}

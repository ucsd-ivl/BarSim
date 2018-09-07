using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Progress Bar
 * 
 * We can create a pseudo progress bar by creating an image that gets filled/rendered
 * from 0.0 (no image) to 1.0 (complete image). This script allows us to set the
 * "progress bar" amount by specifying the number between 0.0f and 1.0f.
 */
public class ProgressBar : MonoBehaviour
{
    [Tooltip("Specify the progress bar image sprite object")]
    public Image progressBarInstance;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Set how much to render the image
    public void SetProgress(float percentage)
    {
        if (percentage < 0.0f)
            percentage = 0.0f;
        if (percentage > 1.0f)
            percentage = 1.0f;

        progressBarInstance.fillAmount = percentage;
    }

    // See how much of the image is rendered
    public float GetProgress()
    {
        return progressBarInstance.fillAmount;
    }
}

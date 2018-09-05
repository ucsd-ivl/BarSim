using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Camera Fade
 * 
 * Since the current VR setup is unable to render any GUI aspect onto the head mounted display,
 * this class is used as a hack to emulate fading between scenes. Please place a small object
 * that covers the user's headset (eg. small sphere). Add this script to that object to
 * emulate scene fading.
 */
public class CameraFade : MonoBehaviour
{

    private Material fadeToMaterial;

    // Use this for initialization
    void Start()
    {
        // Initialize the opacity to show
        fadeToMaterial = GetComponent<Renderer>().material;
        fadeToMaterial.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Set the fade amount. 1.0f = Able to see scene. 0.0f = Completely black
    public void setOpacity(float newOpacity)
    {
        // Calculate the new opacity
        float opacity = 1.0f - newOpacity;
        if (opacity < 0.0f)
            opacity = 0.0f;
        if (opacity > 1.0f)
            opacity = 1.0f;

        // Assign the new opacity
        fadeToMaterial.color = new Color(0.0f, 0.0f, 0.0f, opacity);
    }
}

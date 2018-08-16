using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

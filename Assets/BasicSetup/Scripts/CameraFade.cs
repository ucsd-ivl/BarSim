using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraFade : MonoBehaviour
{

    public Texture fadeToTexture;

    private float opacity;

    // Use this for initialization
    void Start()
    {
        opacity = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        var cameras = Camera.allCameras;
    }

    private void OnGUI()
    {
        Color newColor = GUI.color;
        newColor.a = 1.0f - opacity;

        GUI.color = newColor;
        GUI.depth = 1000;

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeToTexture);
    }

    public void setOpacity(float newOpacity)
    {
        this.opacity = newOpacity;
    }
}

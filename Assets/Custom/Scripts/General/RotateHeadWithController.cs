using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Rotate Head With Controller
 * 
 * This class allows the user to rotate their body in the scene purely using the
 * VIVE touch pad controller, rather than rotating their head. This could be
 * useful for debugging or allowing them to rotate around the scene without
 * requiring too much movement in their body. Simply drag the "RotateHeadWithController"
 * prefab into your scene and the user will be able to rotate by pressing
 * down on the touch pad for their desired rotation direction.
 */
public class RotateHeadWithController : MonoBehaviour
{
    [Tooltip("Specify the name of the experiment manager")]
    public string experimentManagerName = "Experiment Manager";
    [Tooltip("Specify how many degrees to rotate user when user clicks")]
    public float rotationAmount = 15.0f;

    private ExperimentManager experimentManager;
    private float currentRotationAmount;

    // Use this for initialization
    void Start()
    {
        currentRotationAmount = 0.0f;

        try
        {
            experimentManager = GameObject.Find(experimentManagerName).GetComponent<ExperimentManager>();
            experimentManager.RotatePerson(currentRotationAmount);
        }
        catch
        {
            Debug.LogError("Unable to link to experiment manager...");
            Application.Quit();
        }
    }

    // Update is called once per frame
    void Update()
    {
        SteamVR_Controller.Device[] controllerDevices = experimentManager.GetControllers();

        bool controller1_TouchPadDown = controllerDevices[0].GetPressDown(SteamVR_Controller.ButtonMask.Touchpad);
        if (controller1_TouchPadDown)
            RotatePersonHelper(controllerDevices[0]);

        bool controller2_TouchPadDown = controllerDevices[1].GetPressDown(SteamVR_Controller.ButtonMask.Touchpad);
        if (controller2_TouchPadDown)
            RotatePersonHelper(controllerDevices[1]);
    }

    private void RotatePersonHelper(SteamVR_Controller.Device device)
    {
        Vector2 touchpad = device.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad);
        currentRotationAmount += (touchpad.x > 0) ? rotationAmount : (-1 * rotationAmount);

        if (currentRotationAmount > 360.0f)
            currentRotationAmount -= 360.0f;
        if (currentRotationAmount < 0.0f)
            currentRotationAmount += 360.0f;

        experimentManager.RotatePerson(currentRotationAmount);
    }
}

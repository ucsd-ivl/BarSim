using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/**
 * Teleporter
 * 
 * This class allows the user to teleport in the X and Z direction to various part of
 * the scene. The user can hold down the palm trigger to initate their intent to want
 * to teleport. This will send out a raycast line that is visible from their controller
 * outward. When the user let go of the palm trigger, they will be teleported to that
 * location.
 * 
 * Users can only be teleported to places that have a collider around it, as we are using
 * raycast to see where the user will land. To restrict where the user can go, we can
 * add a few symbols to the end of the object's name to indicate that the user can
 * raycast and teleport to that location. This can be specified in the "moveableEnding"
 * field. The controller will have a red line indicating a user cannot teleport to a
 * certain location, and a green line to indicate a location they can teleport to.
 * 
 * Example:
 *   moveableEnding = "__"
 *   objectName = "OutdoorGrass"      | TELEPORT NOT ALLOWED
 *   objectName = "IndoorFloor__"     | TELEPORT ALLOWED
 */
public class Teleporter : MonoBehaviour
{
    [Tooltip("Specify the name of the experiment manager")]
    public string experimentManagerName = "Experiment Manager";
    [Tooltip("Specify the line object to draw the ray cast teleporter")]
    public LineRenderer rayCastLineUi;
    [Tooltip("Specify the last character in a collider name that indicate we can move there")]
    public string moveableEnding = "__";

    private ExperimentManager experimentManager;
    private int activeLaserIndex = 1;
    private bool laserActive = false;
    private bool canMoveThere = false;
    private RaycastHit lastHit;

    // Use this for initialization
    void Start()
    {
        activeLaserIndex = 1;
        laserActive = false;
        canMoveThere = false;
        lastHit = new RaycastHit();

        try
        {
            experimentManager = GameObject.Find(experimentManagerName).GetComponent<ExperimentManager>();
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
        Transform [] controllerTransforms = experimentManager.GetControllerTransforms();
        SteamVR_Controller.Device [] controllerDevices = experimentManager.GetControllers();
        rayCastLineUi.enabled = false;

        // Determine which hand is being used for laser
        if(laserActive == false)
        {
            for (int i = 0; i < 2; i++)
            {
                if (controllerDevices[i].GetPress(EVRButtonId.k_EButton_Grip))
                {
                    laserActive = true;
                    activeLaserIndex = i;
                    break;
                }
            }
        }

        // Determine if laser is still active
        bool currentLaserStatus = controllerDevices[activeLaserIndex].GetPress(EVRButtonId.k_EButton_Grip);
        if ((currentLaserStatus == false) && (laserActive == true) && (lastHit.point != Vector3.zero) && canMoveThere)
            experimentManager.TeleportUser(lastHit.point);
        laserActive = currentLaserStatus;
        rayCastLineUi.enabled = laserActive;

        // Draw out line if laser is active
        if (laserActive == true)
        {
            rayCastLineUi.enabled = true;
            rayCastLineUi.SetPosition(0, controllerTransforms[activeLaserIndex].position + controllerTransforms[activeLaserIndex].forward.normalized * 0.025f);
            rayCastLineUi.startColor = Color.red;
            rayCastLineUi.endColor = Color.red + Color.white * 0.75f;

            RaycastHit hit;
            Physics.Raycast(controllerTransforms[activeLaserIndex].position, 
                controllerTransforms[activeLaserIndex].TransformDirection(Vector3.forward), out hit, Mathf.Infinity);
            lastHit = hit;

            // Great user is pointing at something, see if we can move there
            canMoveThere = false;
            if(hit.point != Vector3.zero)
            {
                rayCastLineUi.SetPosition(1, hit.point);

                if(hit.collider.name.Length >= moveableEnding.Length)
                {
                    if(hit.collider.name.Substring(hit.collider.name.Length - moveableEnding.Length, moveableEnding.Length) == moveableEnding)
                    {
                        canMoveThere = true;
                        rayCastLineUi.startColor = Color.green;
                        rayCastLineUi.endColor = Color.green + Color.white * 0.75f;
                    }
                }

            }

            // User is not pointing at anything, we can't move there....
            else
                rayCastLineUi.SetPosition(1, controllerTransforms[activeLaserIndex].position + 
                    controllerTransforms[activeLaserIndex].forward.normalized * 3.0f);
        }
    }
}

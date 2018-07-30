using System.Collections;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;

public class ControllerGrabObject : MonoBehaviour
{

    // Keep a reference to the object being tracked
    private SteamVR_TrackedObject trackedObj;

    // Keep track of what the hand is coliding with or holding
    private GameObject collidingObject;
    private GameObject objectInHand;

    // Vibration duration
    private ushort pulseDurationMicroSecond = unchecked((ushort)3999);

    // Get controller's input via objec's index
    private SteamVR_Controller.Device Controller
    {
        get { return SteamVR_Controller.Input((int)trackedObj.index); }
    }

    // Get a reference of the object once script loads
    void Awake()
    {
        trackedObj = GetComponent<SteamVR_TrackedObject>();
    }

    private void SetCollidingObject(Collider col)
    {
        // Only grab object if user is not holding anything AND object has a rigid body
        if (collidingObject || !col.GetComponent<Rigidbody>())
        {
            return;
        }

        // All is good, grab object
        collidingObject = col.gameObject;
    }

    // Trigger: Attempt to grab the object
    public void OnTriggerEnter(Collider otherObject)
    {
        SetCollidingObject(otherObject);
    }

    // Trigger: Don't drop the object
    public void OnTriggerStay(Collider otherObject)
    {
        SetCollidingObject(otherObject);
    }

    // Trigger: Release object if we are holding something
    public void OnTriggerExit(Collider otherObject)
    {
        if (!collidingObject)
            return;

        collidingObject = null;
    }

    private void GrabObject()
    {
        // Have user hold object and is no longer colliding
        objectInHand = collidingObject;
        collidingObject = null;

        // Add fix joint to connect object to controller
        var joint = AddFixedJoint();
        joint.connectedBody = objectInHand.GetComponent<Rigidbody>();
    }

    // Make the fixed joint
    private FixedJoint AddFixedJoint()
    {
        FixedJoint fx = gameObject.AddComponent<FixedJoint>();
        fx.breakForce = 20000;
        fx.breakTorque = 20000;
        return fx;
    }

    private void ReleaseObject()
    {
        // Only release if there's a fixed joint attached to controller
        if (GetComponent<FixedJoint>())
        {
            // Remove fixed joint connection
            GetComponent<FixedJoint>().connectedBody = null;
            Destroy(GetComponent<FixedJoint>());

            // Set object's speed/rotation as user releases
            objectInHand.GetComponent<Rigidbody>().velocity = transform.parent.rotation * Controller.velocity;
            objectInHand.GetComponent<Rigidbody>().angularVelocity = transform.parent.rotation * Controller.angularVelocity;
        }

        // User is no longer holding object
        objectInHand = null;
    }

    public void Vibrate()
    {
        //new Thread(() =>
        //{
            for (int i = 0; i < 1000; i++)
                Controller.TriggerHapticPulse(pulseDurationMicroSecond);
        //}).Start();
    }

    // Update is called once per frame
    void Update()
    {

        // Attempt to grab object if user squeezes trigger
        if (Controller.GetHairTriggerDown())
            if (collidingObject)
                GrabObject();

        // Attempt to release object if holding something
        if (Controller.GetHairTriggerUp())
            if (objectInHand)
                ReleaseObject();
    }
}

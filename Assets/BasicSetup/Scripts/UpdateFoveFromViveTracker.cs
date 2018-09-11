using System;
using UnityEngine;
using Valve.VR;

/**
 * Update Fove From Vive Tracker
 * 
 * This is the magic sauce in making the FOVE headset work with the Vive Controllers.
 * 
 * Hardware Requirements:
 *   - Two Vive Controllers for each hand
 *   - 1 FOVE headset
 *   - 1 Vive Tracker Attached to FOVE Headset for better tracking/range
 *   
 * What this script does:
 *   - Automatically detect when the Vive Tracker for the headset is connected
 *   - Calibrate the FOVE headset to Vive space (place headset & controllers pointing same direction and press H)
 *   - Correct yaw drift from FOVE headset over extended usage
 *   - Update user's position based on data from Vive Tracker attached to headset
 *   
 * Keycode:
 *   H - Calibrate the FOVE headset to Vive space
 *   P - Recenter the user's position to (0,0,0) world space coordinates
 */
public class UpdateFoveFromViveTracker : MonoBehaviour
{
    public enum EIndex
    {
        None = -1,
        Hmd = (int)OpenVR.k_unTrackedDeviceIndex_Hmd,
        Device1,
        Device2,
        Device3,
        Device4,
        Device5,
        Device6,
        Device7,
        Device8,
        Device9,
        Device10,
        Device11,
        Device12,
        Device13,
        Device14,
        Device15
    }

    [Header("Required Linked Components")]
    [Tooltip("Specify the left controller object")]
    public GameObject leftController;
    [Tooltip("Specify the right controller object")]
    public GameObject rightController;
    [Tooltip("Specify the whole person (hmd, controller) setup object")]
    public GameObject personSetup;

    [Header("Vive Tracker for Headset")]
    [Tooltip("Device ID of tracker. This will automatically be populated.")]
    public EIndex device = EIndex.None;

    [Header("Headset Localization")]
    [Tooltip("Specify if program should correct yaw drift of FOVE headset using Vive tracker")]
    public bool driftCorrection = true;
    [Tooltip("Specify the amount of milliseconds between drift correct")]
    public ulong millisecondsBetweenDriftCorrection = 0;
    [Tooltip("Specify how much to rotate the scene by in degrees")]
    public float sceneRotationAmount;
    [Tooltip("Specify how much to rotate the yaw orientation of the headset in degree")]
    public float yawOrientationOffset;
    [Tooltip("Specify the offset from the tracker to the center of the two eyes in meters")]
    public Vector3 trackerToEyePositionOffset;

    private SteamVR_Events.Action newPosesAction, deviceConnectedAction;
    private SteamVR_Utils.RigidTransform currentViveTrackerPose;
    private Quaternion ViveTrackerTareOrientation;
    private Quaternion FoveHeadsetTareOrientation;
    private Quaternion FoveViveToWorldSpace;
    private float yawDriftCorrectionOffset;
    private long lastDriftCorrectionTimeMs;
    private float rotateSetupDegree;
    private bool isHeadsetCalibrated = false;

    // Callback function to handle registering new connected Vive Tracker
    private void onNewDeviceConnection(int index, bool connected)
    {
        if (connected && (device == EIndex.None))
        {
            var system = OpenVR.System;
            if (system != null)
            {
                // Assign tracker to headset if it is not a controller
                var deviceClass = system.GetTrackedDeviceClass((uint)index);
                if (deviceClass == ETrackedDeviceClass.GenericTracker)
                    device = (EIndex)index;
            }
        }
    }

    // Callback function to handle new poses from Vive Tracker
    private void onNewPose(TrackedDevicePose_t[] devicePoses)
    {
        int deviceID = (int)device;

        // Check that the device the user specified is valid
        if ((device == EIndex.None) || (devicePoses.Length <= deviceID))
            return;

        // Check that the device is connected and providing poses
        if (!devicePoses[deviceID].bDeviceIsConnected || !devicePoses[deviceID].bPoseIsValid)
            return;

        // Keep track of Vive Tracker's current pose
        currentViveTrackerPose = new SteamVR_Utils.RigidTransform(devicePoses[deviceID].mDeviceToAbsoluteTracking);
    }

    // Constructor: Attach callback function to Vive Tracker data
    UpdateFoveFromViveTracker()
    {
        newPosesAction = SteamVR_Events.NewPosesAction(onNewPose);
        deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(onNewDeviceConnection);
        lastDriftCorrectionTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        FoveViveToWorldSpace = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        rotateSetupDegree = 0;
        isHeadsetCalibrated = false;
    }

    // Correct the yaw drift component of the FOVE headset
    private void CorrectHeadsetYawDrift()
    {
        long currentTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        long timeSinceLastUpdateMs = currentTimeMs - lastDriftCorrectionTimeMs;
        if (timeSinceLastUpdateMs < (long)millisecondsBetweenDriftCorrection)
            return;

        Quaternion foveToFlatSurface = Quaternion.Euler(FoveHeadsetTareOrientation.eulerAngles.x, 0.0f, FoveHeadsetTareOrientation.eulerAngles.z);
        Quaternion viveToFlatSurface = Quaternion.Euler(ViveTrackerTareOrientation.eulerAngles.x, 0.0f, ViveTrackerTareOrientation.eulerAngles.z);
        Quaternion tareFoveOnFlatSurface = (FoveHeadsetTareOrientation * Quaternion.Inverse(foveToFlatSurface));
        Quaternion tareViveOnFlatSurface = (ViveTrackerTareOrientation * Quaternion.Inverse(viveToFlatSurface));

        Quaternion currentFoveRotation = FoveInterface.GetHMDRotation();
        Quaternion curFoveOnFlatSurface = (currentFoveRotation * Quaternion.Inverse(foveToFlatSurface));
        Quaternion curViveOnFlatSurface = (currentViveTrackerPose.rot * Quaternion.Inverse(viveToFlatSurface));

        Quaternion desiredYawDiff = tareFoveOnFlatSurface * Quaternion.Inverse(tareViveOnFlatSurface);
        Quaternion currentYawDiff = curFoveOnFlatSurface * Quaternion.Inverse(curViveOnFlatSurface);
        yawDriftCorrectionOffset = (desiredYawDiff * Quaternion.Inverse(currentYawDiff)).eulerAngles.y;

        lastDriftCorrectionTimeMs = currentTimeMs;
    }

    private void Update()
    {
        /**
         * Press H to localize FOVE space -> Vive Space -> World Space
         *   The user's current head orientation will now be used as a reference for 0 degree yaw in world space
         *   The position of the user will now be shifted to the origin of world space
         */
        if (Input.GetKeyDown(KeyCode.H))
        {
            // Check that headset vive tracker and controllers are both active
            if ((IsHeadsetPositionTracked() && IsControllersConnected()) == false)
                return;

            // Check that fove is set up correctly
            if ((FoveInterface.IsHardwareConnected() && FoveInterface.IsHardwareReady()) == false)
                return;

            // See how much we should rotate FOVE to get it to its proper Vive space
            Vector3 leftControllerRotation = leftController.transform.localRotation.eulerAngles;
            Vector3 rightControllerRotation = rightController.transform.localRotation.eulerAngles;
            Vector3 averageControllerRotation = (leftControllerRotation + rightControllerRotation) / 2.0f;
            Vector3 foveHmdRotation = FoveInterface.GetHMDRotation().eulerAngles;
            Quaternion foveToViveRotationTransform = Quaternion.Euler(0.0f, (averageControllerRotation - foveHmdRotation).y, 0.0f);
            Debug.Log("FoveToViveRotationTransform: " + foveToViveRotationTransform + averageControllerRotation);
            yawOrientationOffset = foveToViveRotationTransform.eulerAngles.y;

            // Shift everything back to center
            personSetup.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            personSetup.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

            // See how much we should rotate the Vive to get it into its proper world space
            FoveViveToWorldSpace = Quaternion.Euler(0.0f, averageControllerRotation.y * -1.0f, 0.0f);
            personSetup.transform.rotation = FoveViveToWorldSpace;
            Vector3 scenePositionShiftAmount = transform.position * -1.0f;
            personSetup.transform.position = new Vector3(
                scenePositionShiftAmount.x,
                personSetup.transform.position.y,
                scenePositionShiftAmount.z);

            // Log the current FOVE headset and Vive Tracker orientation
            FoveHeadsetTareOrientation = FoveInterface.GetHMDRotation();
            ViveTrackerTareOrientation = currentViveTrackerPose.rot;

            // Everything is done
            isHeadsetCalibrated = true;
            Debug.Log("Finished setting up FOVE localization");
        }

        /**
         * Press P to shift the user's current position to the center of world space
         */
        if (Input.GetKeyDown(KeyCode.P))
            ReCenterUser();

        // Attempt to mitigate FOVE headset's yaw drift
        if (driftCorrection)
            CorrectHeadsetYawDrift();
        else
            yawDriftCorrectionOffset = 0.0f;

        // Update the position and orientation of the headset
        transform.localRotation = Quaternion.Euler(0.0f, yawOrientationOffset + yawDriftCorrectionOffset, 0.0f);
        var eyePositionOffset = transform.localRotation * FoveInterface.GetHMDRotation() * trackerToEyePositionOffset;
        transform.localPosition = currentViveTrackerPose.pos + eyePositionOffset;

        // Rotate person setup to world space + any additional rotate offset specified by user
        Vector3 currentPersonPosition = transform.position;
        personSetup.transform.rotation = Quaternion.Euler(FoveViveToWorldSpace.eulerAngles + new Vector3(0.0f, rotateSetupDegree, 0.0f));
        TeleportUser(currentPersonPosition);
    }

    // Re-center to (0,0,0) in world space
    public void ReCenterUser()
    {
        personSetup.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
        Vector3 scenePositionShiftAmount = transform.position * -1.0f;
        personSetup.transform.position = new Vector3(
            scenePositionShiftAmount.x,
            personSetup.transform.position.y,
            scenePositionShiftAmount.z);
    }

    // Teleport user to the given position in world space. Will only change x and z components.
    public void TeleportUser(Vector3 newPosition)
    {
        ReCenterUser();
        personSetup.transform.position = new Vector3(
            personSetup.transform.position.x + newPosition.x,
            personSetup.transform.position.y,
            personSetup.transform.position.z + newPosition.z);
    }

    // Return if the FOVE headset has been calibrated to Vive space
    public bool IsHeadsetCalibrated()
    {
        return isHeadsetCalibrated;
    }

    // Determine if Vive Tracker for headset is detected
    public bool IsHeadsetPositionTracked()
    {
        return (device != EIndex.None);
    }

    // Determine if the controllers are both connected
    public bool IsControllersConnected()
    {
        return (leftController.activeInHierarchy) && (rightController.activeInHierarchy);
    }

    // Return an instance of each of the Vive's left/right controller
    public SteamVR_Controller.Device[] GetControllers()
    {
        SteamVR_Controller.Device[] controllers = new SteamVR_Controller.Device[2];
        controllers[0] = leftController.GetComponent<ControllerGrabObject>().GetDevice();
        controllers[1] = rightController.GetComponent<ControllerGrabObject>().GetDevice();
        return controllers;
    }

    // Return the transform of the Vive's left/right controller
    public Transform[] GetControllerTransforms()
    {
        Transform[] controllersTransform = new Transform[2];
        controllersTransform[0] = leftController.transform;
        controllersTransform[1] = rightController.transform;
        return controllersTransform;
    }

    public void RotatePerson(float degree)
    {
        rotateSetupDegree = degree;
    }

    // Enable callback to detect new pose from Vive Tracker at start
    void OnEnable()
    {
        var render = SteamVR_Render.instance;
        if (render == null)
        {
            enabled = false;
            return;
        }

        newPosesAction.enabled = true;
        deviceConnectedAction.enabled = true;
    }

    // Disable callback to detect new pose from Vive Tracker
    void OnDisable()
    {
        newPosesAction.enabled = false;
        deviceConnectedAction.enabled = false;
    }
}
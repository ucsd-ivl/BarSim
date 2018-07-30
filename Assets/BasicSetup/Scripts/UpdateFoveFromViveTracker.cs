using System;
using UnityEngine;
using Valve.VR;

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
    private float yawDriftCorrectionOffset;
    private long lastDriftCorrectionTimeMs;

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
    }

    private void CorrectHeadsetYawDrift()
    {
        long currentTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        long timeSinceLastUpdateMs = currentTimeMs - lastDriftCorrectionTimeMs;
        if (timeSinceLastUpdateMs < (long) millisecondsBetweenDriftCorrection)
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
        if(Input.GetKeyDown(KeyCode.H))
        {
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
            personSetup.transform.rotation = Quaternion.Euler(0.0f, averageControllerRotation.y * -1.0f, 0.0f);
            Vector3 scenePositionShiftAmount = transform.position * -1.0f;
            personSetup.transform.position = new Vector3(
                scenePositionShiftAmount.x,
                personSetup.transform.position.y,
                scenePositionShiftAmount.z);

            // Log the current FOVE headset and Vive Tracker orientation
            FoveHeadsetTareOrientation = FoveInterface.GetHMDRotation();
            ViveTrackerTareOrientation = currentViveTrackerPose.rot;
            
            // Everything is done
            Debug.Log("Finished setting up FOVE localization");
        }

        /**
         * Press P to shift the user's current position to the center of world space
         */
        if(Input.GetKeyDown(KeyCode.P))
        {
            personSetup.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 scenePositionShiftAmount = transform.position * -1.0f;
            personSetup.transform.position = new Vector3(
                scenePositionShiftAmount.x,
                personSetup.transform.position.y,
                scenePositionShiftAmount.z);
        }

        // Attempt to mitigate FOVE headset's yaw drift
        if (driftCorrection)
            CorrectHeadsetYawDrift();
        else
            yawDriftCorrectionOffset = 0.0f;

        // Update the position and orientation of the headset
        transform.localRotation = Quaternion.Euler(0.0f, yawOrientationOffset + yawDriftCorrectionOffset, 0.0f);
        var eyePositionOffset = transform.localRotation * FoveInterface.GetHMDRotation() * trackerToEyePositionOffset;
        transform.localPosition = currentViveTrackerPose.pos + eyePositionOffset;
    }

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

    void OnDisable()
    {
        newPosesAction.enabled = false;
        deviceConnectedAction.enabled = false;
    }
}
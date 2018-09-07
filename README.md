# Basic Unity FOVE/Vive Setup for Eye Gaze and Blink Tracking

![Fove Vive Picture](/Documentations/Images/FoveVive.jpg)

## Introduction

The goal of this project is to create an immersive virtual reality
environment where we could track to great accuracy what the user is
drawn to in a virtual scene as part of a research study by the [Psychology
Department of the University of California, San Diego](https://psychology.ucsd.edu/).
With that being said however, this code base is intended as a general purpose eye
tracking system for virtual reality and can be used by anyone interested in
the setup.

## Setup

### Hardware Setup

* [FOVE Eye Tracking Virtual Reality Headset](https://www.getfove.com/)
* [Full HTC Vive Setup](https://www.vive.com/us/)
* [One HTC Vive Tracker](https://www.vive.com/us/vive-tracker/)

We chose to use the FOVE headset as it provided fairly accurate eye gaze and
blink tracking. To incorporate controllers, we used the full HTC Vive setup
(including the HTC Vive headset as this was how the controllers communicated).
The FOVE headset does have its own infrared camera used for position tracking.
However, we found their solution a bit limited as it only performed well
when the user is within the camera's narrow tracking window and is looking within 90
degrees towards the infrared camera. Therefore, we chose to add an HTC Vive
tracker on top of the FOVE headset instead, giving us more reliable tracking
and greater freedom to move around.

Here is a picture illustrating our setup.

![Hardware Setup Illustration](/Documentations/Images/HardwareSetup.jpg)

### Software Setup

At the time of which we created this project, FOVE has not been integrated
into SteamVR yet. In order to use the FOVE headset with the HTC Vive controllers
in the SteamVR environment, this was our work around setup:

1. Download and install the required components
    1. [FOVE Setup](https://www.getfove.com/setup/)
    1. [Steam](https://store.steampowered.com/)
        1. Please also install the SteamVR Beta plugin
    1. [Unity 3D](https://unity3d.com/)
1. Use SteamVR with no head mounted display (HMD)
    1. Run the `/SteamVR_Setup/initializeEnvironment.py` Python script

*Note: We are using FOVE Version 0.14.1 at the time of this project. FOVE
Version 0.15.0 provided much higher eye gaze accuracy. However, this version
was quite buggy and we would not recommend.

Note: If you want to use SteamVR with the HTC Vive HMD again, just run
`/SteamVR_Setup/restoreEnvironment.py`*

## Unity Usage

Now that you have all the required components to get started, we have a `Default`
scene located under `\Assets\BasicSetup\Scenes\` with all the required components
needed for using the FOVE headset with the HTC Vive Controllers. Just drag the
`Default` scene into your hierarchy and keep it active across all your scenes.
You're all set to get off the ground and running now!

![Default Scene Compressed](/Documentations/Images/DefaultSceneCompressed.png)

The `Default` scene consists of two prefabs. Below is a description of each
of the prefabs and how to further tune them to your needs. For implementation
details, please checkout the C# source code and our
[implementation summary page](/Documentations/BasicSetupImplementationSummary.md).

### Fove Vive Setup Prefab

![Fove Vive Setup Prefab Expanded](/Documentations/Images/FoveViveSetupPrefabExpanded.png)

The `Fove Vive Setup` prefab located under `/Assets/BasicSetup/Prefabs/` is a
standalone prefab you can throw into your scene. It is in charge of localizing
the FOVE headset in the Vive space, as well as performing all the necessary
eye tracking and logging procedures. A more detailed look into what it consists
of is included below.

#### Vive Controllers

![Controller Collider](/Documentations/Images/ControllerCollider.png)

The `Vive Controller` game object is in charge of updating where the current
HTC Vive controller is, and rendering it to the VR space. Each controller has a
`ControllerGrabObject.cs` script attached. This allows the user to grab any
objects in the scene which has a collider and rigid body attached using the
index trigger of the controller.

#### Fove Headset

The `Fove Headset` game object is in charge of controlling the user's head
(camera) movement in the VR space.

![Fove Headset Inspector](/Documentations/Images/FoveHeadsetInspector.png)

It has a script called `UpdateFoveFromViveTracker.cs` which is in charge of
localizing the FOVE headset in the Vive space, updating the headset's position,
and mitigating any drift in the yaw direction read in from the FOVE headset.

![Fove Vive Calibration](/Documentations/Images/FoveViveCalibration.jpg)

To calibrate the FOVE headset with the Vive world space, please place it so
the FOVE headset and both Vive controllers are facing the same direction. Then
press `H` on the keyboard to sync up the two spaces.

However, since the Vive tracker attached to the FOVE headset isn't at the center
of the FOVE headset, the user will notice something is off when they are
rotating their head. To fix this, we need to offset the tracker position so
that it would be located where the center of the user's head is. To do this,
change the `Tracker To Eye Position` variable in the `Fove Headset` inspector
to account for the offset.

If you want to run the eye gaze calibration procedure at the start of every
application launch, please uncheck `Skip Auto Calibration` in the `Fove Camera`
inspector panel.

#### Eye Tracker

![Eye Tracker Inspector](/Documentations/Images/EyeTrackerInspector.png)

The `Eye Tracker` game object is in charge of tracking what the user is looking
at, for how long, when they blinked, and when they switched looking at different
objects in the scene.

Please look at the [implementation summary page](/Documentations/BasicSetupImplementationSummary.md)
for more details.

### Experiment Manager Prefab

![Experiment Manager Inspector](/Documentations/Images/ExperimentManagerInspector.png)

The `Experiment Manager` prefab located under `/Assets/BasicSetup/Prefabs/` is a
prefab that requires the `Fove Vive Setup` already be in the same scene. Think of
this prefab as the scene manager of your application. It will walk you through the
calibration process at the beginning and then automatically change scene based
on a set time. It also acts as an abstraction layer for other scenes to get
information about not only the current scene, but also the controllers and
headset.

## Implementation Summary

For a more detailed summary of how things are implemented, checkout our
[implementation summary page](/Documentations/BasicSetupImplementationSummary.md).

## Example

Please checkout the `example` branch to see a full setup.

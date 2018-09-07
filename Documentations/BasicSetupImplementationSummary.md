# Documentation - Basic Setup Implementation Summary

![Basic Setup Relationship](/Documentations/Images/BasicSetupRelationship.jpg)

The basic idea behind our design is to have all the required components needed
for this setup to work be in a dedicated (`Default`) scene that would remain
active during the entire runtime of the application. There are two prefabs
in this scene. The first one is the `Fove Vive Setup` prefab. This includes
implementation regarding displaying HTC Vive controllers, controlling the camera
(head) in VR, and performing all necessary eye tracking and logging procedures.
To ease and separate responsibilities for dealing with scenes in the application,
we created another prefab called the `Experiment Manager`. This prefab talks
directly to the `Fove Vive Setup` prefab, handles scene management (auto
loading/unloading), and act as an abstraction layer for other scenes
to get information about the current state of the application, headset, controllers,
and logging information. This means other scenes would only need to talk to the
`ExperimentManager` script if it needs to do anything involving the scene or
the FOVE / Vive setup (e.g. attaching callback to controllers, getting headset
transforms, requesting scene change, etc.).

Since this `Default` scene is active across all scenes, anything implemented in
this `Default` scene can be used by all scenes. Therefore, if you have any
scene specific features (e.g. teleporting with controller's palm trigger,
rotating camera in scene with controller's track pad, etc.), please save
them as a separate prefab that you can just drag into individual scenes to
restrict those features to the intended scenes.

Below is a more detailed overview of each of the script in the `Default` scene.

* * *

## CameraFade.cs

Since the current VR setup is unable to render any GUI aspect onto the head mounted display,
this class is used as a hack to emulate fading between scenes. Place a small object
that covers the camera (eg. small sphere) and add this script to that object to
emulate scene fading.

* * *

## ControllerGrabObject.cs

![Controller Inspector](/Documentations/Images/ControllerInspector.png)

This class enables the user to grab objects with the index trigger behind each
of the Vive controllers. This script can grab any object that has a collider
and rigid body attached to it.

To use:

1. Make sure you have `SteamVR` and are using `OpenVR` as the `Virtual Reality SDK`
1. Attach a box collider to controller's game object near front of controller
1. Attach this script to the controller's game object

![Controller Collider](/Documentations/Images/ControllerCollider.png)

* * *

## ExperimentManager.cs

![Experiment Manager Script](/Documentations/Images/ExperimentManagerScript.png)

This class is responsible for managing the various scenes in the experiment, logging data,
and providing an abstract layer for scripts within various scenes to obtain data about the
scene, headset, and logging information. This class operates in the background and should
be placed in a scene that remains active across the entire experiment session.

### Scene Setup

To automate loading and unloading scenes, each scene must be labeled in the format of
`Scene_` + 3 digit representing scene number (e.g. `Scene_007`). This experiment manager
will start out with `Scene_000`, followed by `Scene_001`, `Scene_002`, ..., until it
reaches the `totalNumberOfScenes` specified.

![Scene Naming](/Documentations/Images/ScenesNaming.png)

Upon loading in a new scene, it will attempt to find the skybox associated with that scene.
This skybox should be saved as a Cubemap located under `/Custom/SkyBox/Resources/` with the
same name format as the scene (`Scene_XXX`). If no skybox is detected, it will load in the
specified `defaultSkyBoxMaterial`.

![Scene Skybox](/Documentations/Images/SceneSkybox.png)

*Note: This script requires the `Fove Vive Setup` prefab be included in the same scene to work.*

*Note: For this to work properly, please unload all scenes except the scene with this experiment
manager prefab in it prior to starting the application.*

### Keycode

 Key | Function
 :--:|:--------------------:
 N   | Change to next scene

 * * *

## ExperimentManagerUiPrompt.cs

This class provides a way to present the user a UI prompt in virtual reality
that will always appear in front of the user.

* * *

## EyeTracker.cs

![Eye Tracker Script](/Documentations/Images/EyeTrackerScript.png)

This class is responsible for performing all eye gaze/blink tracking throughout the various
scenes in the experiment. It is capable of tracking where the user is looking (whether that
be at an object or somewhere in the skybox), for how long, as well as keeping track of when
the user blinks. It has an `EyeTrackingLogger` helper class that will deal with saving the
data at the end of each scene.

### Tracking objects within the scene

To track items within the scene, just add a collider to it. This eye tracker class will
raycast from the headset using the current gaze direction to see if it hit any object's
collider. If it does hit, it will note the object that it hit, as well as the full path
from the root object to the hit object.

If you need a collider but don't what it to be tracked, please place it in the `Ignore
Raycast` layer.

### Tracking objects within the skybox

To track items within the skybox, we would need to come up with an image file in the
format of longitude and latitude, where tracked objects are color coded in the file. We
would need an additional `.txt` file to specify what each pixel value in the labeled
image means.

* Place the labeled image file as `Scene_XXX.jpg` into `Custom/Labels/`
* Place the labeled text file as `Scene_XXX.txt` into `Custom/Labels/`

*Note: `XXX` is 3 digits representing the current scene.*

### Example

Format of the text file            | Example
:---------------------------------:|:------------------------------------:
Label\n                            | Skybox, Ceiling, Lights, Light_00
r g b\n                            | 1.0 0.5 0.5

![Skybox Labeling Demo](/Documentations/Images/SkyboxLabelingDemo.jpg)

* * *

## EyeTrackingLogger.cs

This is a helper class in charge of logging eye tracking data and saving it to a file.
This class will create three files: `labeledDataSummary.data`, `blink.csv`, `lookAt.csv`

### labeledDataSummary.data

This is the main summary file storing how long (in milliseconds) and how many
times the user blinked while looking at each object in the scene. It keeps the
hierarchical aspect of the data. Please use `parseRawExperimentData.py` to
convert this to proper `.csv` and `.json` format.

Format: `duration,blink_count,label`

Example:
```
duration,blink_count,label
7983,3,Background
7983,3,Background,ComputerStation
7983,3,Background,ComputerStation,Tables
5062,1,Background,ComputerStation,Tables,Table_2
2920,2,Background,ComputerStation,Tables,Table_3
1801,0,skybox
1801,0,skybox,neutral
1801,0,skybox,neutral,white_board
```

### blink.csv

This tracks when the user blinked and what the user was looking at when they blinked.

Format: `Time,Item`

Example:
```
Time,Item
17:44:18.089,Background,Wall,RightWall
17:44:20.058,Background,Wall,RightWall
17:44:20.975,Background,Wall,BackWall
17:44:23.074,Background,Wall,FrontWall
```

### lookAt.csv

This tracks when the user switched from looking at one item to another item.

Format: `Time,Item`

Example:
```
Time,Item
17:44:16.589,Background,Floor__
17:44:16.622,Background,Wall,RightWall
17:44:17.239,Calibration_Objects,Calibration_Object_7,Front_Cube
```

* * *

## UpdateFoveFromViveTracker.cs

This is the magic sauce in making the FOVE headset work with the Vive Controllers.

![Update Fove From Vive Tracker Script](/Documentations/Images/UpdateFoveFromViveTrackerScript.png)

### Hardware Requirements

* Two Vive Controllers for each hand
* One FOVE headset
* One Vive Tracker attached to FOVE Headset for better tracking/range

### What this script does

* Automatically detect when the Vive Tracker for the headset is connected
* Calibrate the FOVE headset to Vive space (place headset & controllers pointing towards the same direction and press H)
* Correct yaw drift from FOVE headset over extended usage
* Update user's position based on data from Vive Tracker attached to headset

### Keycode

Key | Function
:--:|:---------------------------------------------------------------:
H   | Calibrate the FOVE headset to Vive space
P   | Recenter the user's position to (0,0,0) in world space coordinates

# Gesture Warfare: Cyber Dojo

A gesture-controlled shooter built with Unity and MediaPipe hand tracking.

## Gameplay

Use a webcam to control combat with your hands. The game recognizes gestures for aiming and shooting in sniper mode, shooting in pistol mode, throwing a pulse grenade, and pausing. Fight through enemy waves and bosses, use cover, and advance through the levels. An in-game gesture tutorial introduces the controls.

The main playable scene is `Assets/Scenes/HandDemo.unity`. Gesture recognition depends on a clearly visible hand and adequate lighting.

## Deployment

1. Install **Unity 2022.3.62f3** with the build support module for your target platform, Git, and Git LFS.
2. Clone this repository and run `git lfs pull` from its root. The large MediaPipe plugins and font asset are stored with Git LFS.
3. Open the repository root in Unity Hub and wait for the initial asset import and package resolution to finish.
4. Open `Assets/Scenes/HandDemo.unity` and press **Play** to run the project in the Editor. Connect a webcam and grant camera access if prompted.
5. To create a standalone build, open **File > Build Settings**, select the target platform, verify that `HandDemo.unity` is included and enabled, then choose **Build**. The checked-in Build Settings already include this scene.

Unity package dependencies are declared in `Packages/manifest.json` and `Packages/packages-lock.json`. Keep the `.meta` files with their assets when moving or copying the project.

### Expected frame rate

- **Desktop rendering goal:** 60 FPS at 1920 × 1080. This is a target, **not a measured benchmark**; no hardware-specific FPS test results are available in this repository.
- **Webcam input:** the included webcam diagnostic requests 640 × 480 at 30 FPS. Camera capture rate and game rendering rate are separate.
- Actual frame rate depends on the CPU, GPU, camera, quality preset, and number of active enemies. Measure a player build on your intended hardware before publishing a performance claim.

## Configuration requirements

| Item | Requirement or current project setting |
| --- | --- |
| Unity Editor | 2022.3.62f3 for opening and rebuilding the project |
| Version control | Git and Git LFS; run `git lfs pull` after cloning |
| Camera | A working webcam with permission granted to the Editor or player; 640 × 480 at 30 FPS is requested by the diagnostic script |
| Display | The standalone default resolution is 1920 × 1080; it can be changed in Unity Player Settings |
| Android builds | Android Build Support is required in Unity; the project setting specifies minimum Android API level 23 |
| Disk space | The source assets are about 1.1 GB, and local Unity-generated data can use several additional gigabytes; leave room for imports and builds |
| CPU, GPU, and RAM | No tested minimum specification is available yet; validate the game on the devices you intend to support |

## Publication notes

The project contains third-party Unity assets, fonts, and music. Check their redistribution terms before publishing a public repository or build. The email login code currently contains a hard-coded HTTP service address; review the service configuration and transport security before a public release.

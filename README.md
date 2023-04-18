# HoloNeoDoppler

This project is part of a master thesis by Tore Bergebakken with the supervision of Gabriel Kiss and Frank Lindseth.

It expands upon the HoloUmoja master thesis by Maria Nylund.

## Prerequisites

This project requires **Unity 2022.3.20f1** (exactly) with Universal Windows Platform build support, and **Visual Studio 2019** or newer (preferably 2022).

The following Visual Studio workloads are required:

![](img/visual_studio_workloads.png)

You'll need to enable development mode on your HoloLens in order to be able to deploy to it from Visual Studio.
Follow the steps in [this guide](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-visual-studio?tabs=hl2#enabling-developer-mode)

## Build

After cloning the repo, you need to run ```git lfs fetch``` and ```git lfs pull``` to get the DLLs and the Vuforia package.

Build the project in Unity with the following settings (excluding VS version):

![](img/unity_build_settings.png)

Replace NeoDoppler with HoloUmoja in "Scenes in Build" if desired.

In Visual Studio choose "Release", "ARM64" and click "Build without Debugging" ("Build" in VS 2019) with HoloLens 2 connected through USB and turned on.

## Tracking images

Can be found in the project in the
[pdf](Assets/Editor/Vuforia/ForPrint/ImageTargets/target_images_USLetter.pdf)


- **Astronaut** for ultrasound probe in revamped HoloUmoja
- **Drone** for infant or abdominal

## Issues

- **Tracking images are not recognised"** => Go to "Settings" on HoloLens 2 and manually allow for camera usage in the HoloNeoDoppler/HoloUmoja application.

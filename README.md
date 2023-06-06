
# UdonPlatformHook  [![Licence](https://img.shields.io/github/license/Superbstingray/UdonPlayerPlatformHook?color=blue&label=License)](https://github.com/Superbstingray/UdonPlayerPlatformHook/blob/main/LICENSE) [![Releases](https://img.shields.io/github/v/tag/Superbstingray/UdonPlayerPlatformHook?color=blue&label=Download)](https://github.com/Superbstingray/UdonPlayerPlatformHook/releases/download/v1.37/UdonPlatformHook.v1.37.unitypackage)

 Drag and Drop solution for making Players correctly follow moving colliders in the scene when standing on them. The prefab makes Players functionally behave as if they were parented to any collider they stand on.
## How to use
* Drag into the root of your scene and set your moving Objects/Platforms to layer 11(Environment) or a custom layer and assign layers in the scripts layer mask.
* You only need to add one instance of the prefab to your scene.

## Functionality / Features

* Makes the Player move seamlessly with colliders.
* Will correctly move and rotate the Player with colliders they stand on.
* Behavior is enabled/disabled based on GameObject layers.
* Correct physics/momentum when jumping off of moving colliders.
* Ability to stop moving the player when they are accessing their menu(s)
* Player will seamlessly teleport with colliders that get teleported / moved quickly.

## Example
Typically the VRChat Player controller is unable to follow moving colliders and will remain stationary as colliders slide out from underneath it. With the prefab added to the scene, the Player will follow collider movement and Y euler axis rotation correctly.

https://user-images.githubusercontent.com/74171114/130368388-7721e8c7-ec4a-403f-b4bc-e561a8ad06fb.mp4

##
Overview of script component:

![UPHUdon](https://user-images.githubusercontent.com/74171114/165277190-5be33308-f2f3-43b6-a14c-c1dc019797b1.png)

## Example World

Basic example setup can be found in this VRChat world:
https://vrchat.com/home/world/wrld_6eaf7a85-ffcb-4765-a9b6-c7e435802079

## Requirements
 
 * [VRCSDK3-Udon](https://vrchat.com/home/download) v.2022.02.16.19.13+
 * [VRChat-Community / Merlins Udon Sharp](https://github.com/vrchat-community/UdonSharp) (v0.20.3, v1.0)
 * Unity 2019.4.30f1+

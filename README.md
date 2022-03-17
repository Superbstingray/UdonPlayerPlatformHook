



# UdonPlatformHook
 
## Requirements
 
 * [VRCSDK3-Udon](https://vrchat.com/home/download) v.2022.02.16.19.13+
 * [VRChat-Community / Merlins Udon Sharp](https://github.com/vrchat-community/UdonSharp) (v0.20.3)
 * Unity 2019.4.30f1+
## About
* Drag & Drop solution for making players correctly follow moving Platforms / Vehicles / GameObjects in your scene when standing on them. Makes players functionally behave as if they were parented to any Collider they stand on.
* Behavior can be enabled or disabled based on layers.

## Usage

Set your moving objects/platforms to layer 11(Environment) or a custom layer and assign layers in the layer mask.

## Example
Typically the player is unable to follow moving colliders automatically and will remain stationary, with this prefab the player will follow collider movement and rotation correctly.

https://user-images.githubusercontent.com/74171114/130368388-7721e8c7-ec4a-403f-b4bc-e561a8ad06fb.mp4

## Other

Doesn't currently work with CyanEmu due to it not supporting origin tracking. Testing will need to be done in game.

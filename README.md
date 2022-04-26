



# UdonPlatformHook

> Makes moving with colliders seamless.

> Behavior can be enabled or disabled based on layers.

> Will move and rotate the player correctly with objects they stand on.

> Will teleport the player seamlessly relative to objects they stand on.

> Supports moving the player relative to objects that are changing scale.
 
## About
* Drag & Drop solution for making players correctly follow moving colliders in your scene when standing on them. Makes players functionally behave as if they were parented to the collider they stand on.
* Set your moving objects/platforms to layer 11(Environment) or a custom layer and assign layers in the layer mask.

## Example
Typically the player is unable to follow moving colliders automatically and will remain stationary, with this prefab the player will follow collider movement and rotation correctly.

https://user-images.githubusercontent.com/74171114/130368388-7721e8c7-ec4a-403f-b4bc-e561a8ad06fb.mp4

![UPHUdon](https://user-images.githubusercontent.com/74171114/165277190-5be33308-f2f3-43b6-a14c-c1dc019797b1.png)

## Requirements
 
 * [VRCSDK3-Udon](https://vrchat.com/home/download) v.2022.02.16.19.13+
 * [VRChat-Community / Merlins Udon Sharp](https://github.com/vrchat-community/UdonSharp) (v0.20.3)
 * Unity 2019.4.30f1+

Created by Superbstingray

https://github.com/Superbstingray/UdonPlayerPlatformHook

v1.0 -  15/03/22  -  Made in Unity 2019.4.31f1 / SDK3-2022.02.16.19.13 / UdonSharp_v0.20.3

(Prefab Functionality)
Drag & Drop solution for making players correctly follow moving Platforms / Vehicles / GameObjects in your scene when standing on them. Makes players functionally behave as if they were parented to any Collider they stand on.

Behavior can be enabled or disabled based on layers.

(Usage)
Drag into root of your scene and set your moving objects/platforms to layer 11(Environment) or a custom layer and assign layers in the layer mask.

You only need one of these prefabs in your scene.

(Additional)

Colliders with Rigidbodies that have Interpolate or Extrapolate set can have issues with fast linear vertical movement.

Objects with mesh Colliders may need RigidBodies to work properly.

If your world utilizes VRCPlayerAPI.Immobilize the ReduceIKDrift setting may interfere with it.

Won't work with CyanEmu versions v0.3.10 or below as it wont support player origin tracking so you will need to verify behavior in game.

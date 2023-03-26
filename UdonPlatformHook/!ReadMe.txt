
https://github.com/Superbstingray/UdonPlayerPlatformHook

v1.37 -  27/3/23  -  Made in Unity 2019.4.31f1 / SDK3-2022.02.16.19.13 / UdonSharp_v0.20.3


(Prefab Functionality)

* Drag and Drop solution for making Players correctly follow moving colliders in the scene when standing on them. The prefab makes Players functionally behave as if they were parented to any collider they stand on.
* Behavior can be enabled or disabled based on layers.


(Usage)

* Drag into the root of your scene and set your moving Objects/Platforms to layer 11(Environment) or a custom layer and assign layers in the scripts layer mask.
* You only need to add one instance of the prefab to your scene, ideally not parented to any other Game Object.


(Functionality / Features)

* Makes the Player move seamlessly with colliders.
* Will correctly move and rotate the Player with colliders they stand on.
* Behavior is enabled/disabled based on GameObject layers.
* Correct physics/momentum when jumping off of moving colliders.
* Ability to stop moving the player when they are accessing their menu(s)
* Player will seamlessly teleport with colliders that get teleported / moved quickly.


(Additional)

Objects with Rigidbodies that have Interpolate or Extrapolate set that move on FixedUpdate can have issues with fast linear vertical movement causing the player to detach from the platform, this includes any object that uses VRC Object Sync as it silently overrides Rigidody Interpolation settings.

If your world utilizes VRCPlayerAPI.Immobilize the ReduceIKDrift setting may interfere with it.

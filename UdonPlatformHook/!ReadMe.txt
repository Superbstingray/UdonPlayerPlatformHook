Created by Superbstingray

https://github.com/Superbstingray/UdonPlayerPlatformHook

v1.0 -  15/03/22  -  Made in SDK3-2022.02.16.19.13 & UdonSharp_v0.20.3

Unity 2019.4.31f1, SDK version 2022.02.16.19.13 and UdonSharp_v0.20.3 or above is required.

Prefab that functions as a drag and drop solution for making players correctly follow any moving Platforms / Vehicles / GameObjects in your scene when standing on them. Makes players "Functionally" behave as if they were parented to the game object they are standing on. Behavior can be enabled or disabled based on layers.

(Prefab Functionality)
Moves the player by teleporting them with an offset from the GameObject they are standing on creating a parenting effect. Players will move and seamlessly teleport with colliders they stand on.

(Usage)
Set your moving objects/platforms to layer 11(Environment) or a custom layer and set the layer mask.

You only need one of these prefabs in your scene. Do not add more than one.

(Additional)

Won't work with CyanEmu versions v0.3.10 or below as it wont support player origin tracking so you will need to verify behavior in game.
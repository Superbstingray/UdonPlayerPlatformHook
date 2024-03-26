
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
using UdonSharp;
using VRC.Udon.Wrapper.Modules;

namespace Superbstingray
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class UdonPlatformHook : UdonSharp.UdonSharpBehaviour
{
    [Tooltip("Layers that the Player will move with.")]
    public LayerMask hookLayerMask;

    [Tooltip("If the Player will hook onto trigger colliders")]
	public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.UseGlobal;

    [Tooltip("Vertical distance between the Player and Platform before the Script Unhooks from Colliders. You may want to increase this value if your world has a higher than average jump impulse.")]
    public float hookDistance = 0.75F;

    [Tooltip("Will make the Player keep their Velocity + the Platforms Velocity when they detach from it.")]
    public bool inheritVelocity = true;

    [Tooltip("Will Immobilize(true) players when standing still on moving platforms to prevent Avatars from having IK drift / IK walk.")]
    public bool reduceIKDrift = true;

    [Tooltip("Try Detect if the Player has their Main Menu open and stop moving them. Will help with menu interactions.")]
    public bool mainMenuPause = true;

    [Tooltip("Try Detect if the Player has their Quick Menu open and stop moving them. Will help with menu interactions.")]
    public bool quickMenuPause = false;

    BoxCollider platformOverride;
    Collider[] colliderArray;
    Collider sceneCollider;
    Quaternion headRotation;
    RaycastHit hitInfo;	
    Transform PlayerOffset, playerTracker, hook;
    Vector3 playerVelocity, hookLastPos, hookOffsetPos, lastFramePos;
    VRCPlayerApi localPlayer;

    bool menuOpen;
    float InputMoveH, InputMoveV;
    int unhookThreshold, localColliders, intUI;

    [FieldChangeCallback(nameof(isHookedCallback))]
    bool isHooked;
    bool isHookedCallback
    {	set
        {	isHooked = value;
            if (isHooked) // isHooked=true functions
            {
                hook.localPosition = Vector3.zero; hook.eulerAngles = Vector3.zero;
                platformOverride.enabled = true;	
                PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);

                // When hooking count the number of PlayerLocal colliders the player has
                // as this will help us know when the Player enters a station.
                localColliders = Mathf.Clamp(Physics.OverlapSphere((localPlayer.GetPosition()), 1024f, 1024).Length, 1, 100);
            }
            else // isHooked=false functions
            {
                if(inheritVelocity) { localPlayer.SetVelocity(playerVelocity); } // Sets the players velocity to their actual worldspace velocity when they Unhook.

                OverridesOff();
                hook.localPosition = Vector3.zero; hook.eulerAngles = Vector3.zero;
                PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);
            }	
        }
    }

        void Start()
        {
            InitializeVariables();
            IgnoreSceneCollision();
        }

        void FixedUpdate()
        {
            FixedUpdateFunctions();
        }

        void LateUpdate()
        {
            LateUpdateFunctions();
        }

        void FixedUpdateFunctions() 
        {	
            if (!VRC.SDKBase.Utilities.IsValid(localPlayer)) { return; }

            if (isHooked && inheritVelocity) // Average the last X frames of the players global velocity.
            {
                playerVelocity = Vector3.ClampMagnitude((playerVelocity * 3f + (localPlayer.GetPosition() - lastFramePos) / Time.deltaTime) / 4f, 100f);
                lastFramePos = localPlayer.GetPosition();
            }

            if (!menuOpen)
            {
                // Override_Spherecast. Set position of override collider.
                Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3f, 0f), 0.25f, Vector3.down, out hitInfo, 10f, hookLayerMask.value, TriggerInteraction);
                platformOverride.center = hitInfo.point;

                // FixedUpdate_Spherecast. Check for valid platforms.
                // Add to the unhookThreshold if it misses a valid platform and unhook if unhookThreshold is greater than X.
                if (!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0f, .3f, 0f), 0.25f, Vector3.down, out hitInfo, hookDistance + .3f, hookLayerMask.value, TriggerInteraction))
                {
                    unhookThreshold++;
                    if (unhookThreshold > 10 && isHooked)
                    {
                        hook.parent = transform;
                        SetProgramVariable(nameof(isHooked), false);
                        SendCustomEventDelayedSeconds(nameof(OverridesOff), 0.5f);
                    }
                }
            }
        }

        void LateUpdateFunctions() 
        {
            if (!VRC.SDKBase.Utilities.IsValid(localPlayer)) { return; }
#if !UNITY_EDITOR
            if (isHooked) // Count the number of InterntalUI colliders as a means to know if the menu is open or not.
            {
                intUI = Physics.OverlapSphereNonAlloc(localPlayer.GetPosition(), 10f, colliderArray, 524288);
                menuOpen = (mainMenuPause && (intUI >= 7 && intUI <= 14)) || (quickMenuPause && (intUI >= 15 && intUI <= 19));
            }
            if (isHooked && !menuOpen) // Move the parented hook to the Players position
            {
                hookLastPos = hook.position;
                hook.position = localPlayer.GetPosition();
                hookOffsetPos = hook.position - hookLastPos;
            }
            if (isHooked && menuOpen) { localPlayer.SetVelocity(Vector3.zero); } // Override Player Velocity to make it easier to use their menu.

            // Teleport the player to the new offset position only if the players PlayerLocal collider count
            // didn't decrease otherwise assume the player entered a station.
            if (isHooked && !menuOpen)
            {
                playerTracker.SetPositionAndRotation(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation);
                PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);

                if (Physics.OverlapSphere(localPlayer.GetPosition(), 1024f, 1024).Length >= localColliders)
                {
                    localPlayer.TeleportTo(playerTracker.position - hookOffsetPos, playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
                }
            }
#else
            // "EditorOnly" duplicate funtion from above specifically for CyanEmu/ClientSim as Origin tracking does not behave the same in editor as in client.
            if (isHooked)
            {
                playerTracker.SetPositionAndRotation(localPlayer.GetPosition(), localPlayer.GetRotation());
                PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);

                if (Physics.OverlapSphere(localPlayer.GetPosition(), 1024f, 1024).Length >= localColliders)
                {
                    localPlayer.TeleportTo(playerTracker.position - hookOffsetPos, playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, true);
                    hookOffsetPos = Vector3.zero;
                }
            }
#endif
            // Players in Desktop or 3 Point tracking will have their Inverse Kinematics drag behind and "IK Walk" while being moved when they aren't intentionally locomoting.
            // This function is to prevent that from occuring by Immobilizing the player when they are hooked to a platform and aren't moving relative to the platform.
            // This is scuffed and I should optimize it.
            if(reduceIKDrift && isHooked)
            {
                headRotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
                localPlayer.Immobilize(!(InputMoveH * 0.1f + InputMoveV != 0f)
                && Mathf.Abs(playerVelocity.x) + Mathf.Abs(playerVelocity.z) > .1f
                && Mathf.Abs(localPlayer.GetVelocity().x) + Mathf.Abs(localPlayer.GetVelocity().z) < .01f
                && Quaternion.Angle(new Quaternion(0f, headRotation.y, 0f, headRotation.w).normalized, localPlayer.GetRotation()) < 90f);
            }
            // LateUpdate_Spherecast. Check for valid platforms.
            if (Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0f, .3f, 0f), 0.25f, Vector3.down, out hitInfo, hookDistance + .3f, hookLayerMask.value, TriggerInteraction))
            {
                unhookThreshold = 0;
                if (unhookThreshold < 10 && (localPlayer.IsPlayerGrounded())) // Hook to the valid platform if the Player is grounded.
                {
                    hook.parent = hitInfo.transform;
                    SetProgramVariable(nameof(isHooked), true);
                }
            }
        }

        // INPUT EVENTS: MoveVertical, MoveHorizontal, InputJump.
        public override void InputMoveVertical(float Value, UdonInputEventArgs InputMoveVerticalArgs)
        {
            InputMoveV = InputMoveVerticalArgs.floatValue;
        }
        public override void InputMoveHorizontal(float Value, UdonInputEventArgs InputMoveHorizontalArgs)
        {
            InputMoveH = InputMoveHorizontalArgs.floatValue;
        }
        public override void InputJump(bool outputJumpBool, UdonInputEventArgs inputJumpArgs)
        {
            if (reduceIKDrift) { localPlayer.Immobilize(false); }
        }

        // Reset prefab state and call unhook on Respawn.
        public override void OnPlayerRespawn(VRCPlayerApi onPlayerRespawnPlayer) 
        {
            hook.parent = transform;
            unhookThreshold = System.Int32.MaxValue;
            localPlayer.SetVelocity(Vector3.zero);
            SetProgramVariable(nameof(isHooked), false);
            OverridesOff();
        }

        // The prefab uses an additional overriding collider to prevent the VRC player controller from being affected by moving world
        // colliders which can affect locomotion and animations, and this is to prevent the override collider from interacting with other colliders in the scene.
        public void IgnoreSceneCollision()
        {
            SendCustomEventDelayedSeconds(nameof(IgnoreSceneCollision), 60f);
            colliderArray = Physics.OverlapSphere(Vector3.zero, System.Single.MaxValue);
            for (int i = 0; i < colliderArray.Length; i++)
            {
                sceneCollider = colliderArray[i];
                if (VRC.SDKBase.Utilities.IsValid(sceneCollider))
                {
                    Physics.IgnoreCollision(sceneCollider, platformOverride);
                }
            }
        }

        void InitializeVariables() // Set Prefab Variables 
        {
            localPlayer = Networking.LocalPlayer;
            if (!VRC.SDKBase.Utilities.IsValid(localPlayer)) { return; }
            lastFramePos = localPlayer.GetPosition();
            playerTracker = transform.GetChild(0).GetChild(0);
            PlayerOffset = transform.GetChild(0);
            hook = transform.GetChild(1);
            platformOverride = transform.GetComponent<BoxCollider>();
            platformOverride.size = new Vector3(0.5f, 0.05f, 0.5f);
            transform.position = Vector3.zero;

            if (hookLayerMask.value == -1) // Override if using "Everything" as a layermask to Everything -PlayerLocal,-MirrorReflection to prevent interference with the prefabs functionality.
            {
                hookLayerMask.value = -263369;
            }
        }

        // Disable Override Collider and set Immobilize state false.
        void OverridesOff()
        {
            if (!localPlayer.IsPlayerGrounded()) { platformOverride.enabled = false; }

            if (reduceIKDrift) { localPlayer.Immobilize(false); }
        }
    }
}

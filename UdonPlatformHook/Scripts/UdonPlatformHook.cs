
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using VRC.Udon.Common;
using UdonSharp;

namespace Superbstingray
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
	public class UdonPlatformHook : UdonSharp.UdonSharpBehaviour
{
	[Tooltip("Layers that the Player will move with.")]
	public LayerMask hookLayerMask;

	[Tooltip("Vertical distance between the Player and Platform before the Script Unhooks from Colliders. You may want to increase this value if your world has a higher than average jump impulse.")]
	public float hookDistance = 0.75F;

	[Tooltip("Will make the Player keep their Velocity + the Platforms Velocity when they walk off of it.")]
	public bool inheritVelocity = true;

	[Tooltip("Will Immobilize(true) players when standing still on moving platforms to prevent Avatars from having IK drift / IK walk.")]
	public bool reduceIKDrift = true;

	[Tooltip("Detect if the Player has their Main Menu open and stop moving them.")]
	public bool mainMenuPause = true;

	[Tooltip("Detect if the Player has their Quick Menu open and stop moving them.")]
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
			if (isHooked) // #isHooked=true functions
			{
				hook.localPosition = Vector3.zero; hook.eulerAngles = Vector3.zero;
				platformOverride.enabled = true;	
				PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);

				// When hooking count the number of PlayerLocal colliders the player has
				// as this will help us know when the Player enters a station.
				localColliders = Mathf.Clamp(Physics.OverlapSphere((localPlayer.GetPosition()), 1024f, 1024).Length, 1, 100);
			}
			else // #isHooked=false functions
			{
				if(inheritVelocity) { localPlayer.SetVelocity(playerVelocity); } // Sets the players velocity to their actual worldspace velocity when they Unhook.

				hook.localPosition = Vector3.zero; hook.eulerAngles = Vector3.zero;
				PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);
			}	
		}
	}

		public void Start() // Set Prefab Variables 
		{
			localPlayer = Networking.LocalPlayer;
			lastFramePos = localPlayer.GetPosition();
			playerTracker = transform.GetChild(0).GetChild(0);
			PlayerOffset = transform.GetChild(0);
			hook = transform.GetChild(1);
			platformOverride = transform.GetComponent<BoxCollider>();
			platformOverride.size = new Vector3(0.5f, 0.05f, 0.5f);
			transform.position = Vector3.zero;

			_IgnoreSceneCollision(); // Start collision Check loop

			if (hookLayerMask.value == -1) // Override from using "Everything" as a layermask to prevent interference with the prefabs functionality.
			{
				hookLayerMask.value = -263369;
			}
		}

		public void FixedUpdate() 
		{	
			if (!VRC.SDKBase.Utilities.IsValid(localPlayer)) { return; }

			if (isHooked && inheritVelocity) // #Average the last X frames of the players global velocity.
			{
				playerVelocity = Vector3.ClampMagnitude((playerVelocity * 3f + (localPlayer.GetPosition() - lastFramePos) / Time.deltaTime) / 4f, 100f);
				lastFramePos = localPlayer.GetPosition();
			}

			if (!menuOpen)
			{
				// #OverrideSpherecast. Set position of override collider.
				Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3f, 0f), 0.25f, new Vector3(0F, -1f, 0f), out hitInfo, 10f, hookLayerMask.value);
				platformOverride.center = hitInfo.point;

				// #FixedUpdate_Spherecast. Check for valid platforms.
				// Add to the unhookThreshold if it misses a valid platform and unhook if unhookThreshold is greater than X.
				if (!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0f, .3f, 0f), 0.25f, new Vector3(0f, -1f, 0f), out hitInfo, hookDistance + .3f, hookLayerMask.value))
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

		public void LateUpdate() 
		{
			if (!VRC.SDKBase.Utilities.IsValid(localPlayer)) { return; }

#if !UNITY_EDITOR

			if (isHooked) // Count the number of InterntalUI colliders as a means to know if the menu is open or not. ?#MENUOPEN
			{
				intUI = Physics.OverlapSphere(localPlayer.GetPosition(), 10f, 524288).Length;
				menuOpen = (mainMenuPause && (intUI == 8 || intUI == 9 || intUI == 10)) || (quickMenuPause && (intUI == 11 || intUI == 12));
			}
			if (isHooked && !menuOpen) // Move the parented hook to the Players position
			{
				hookLastPos = hook.position;
				hook.position = localPlayer.GetPosition();
				hookOffsetPos = hook.position - hookLastPos;
			}
			if (isHooked && menuOpen) { localPlayer.SetVelocity(Vector3.zero); } // Override Player Velocity to make it easier to use their menu.

			// Teleport the player to the new offset position only if the players PlayerLocal collider count
			// hasn't gone down as then we should assume that means they have entered a station.
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
			// Editor Only duplicate funtion from above specifically for CyanEmu/ClientSim as they don't properly support Origin tracking.
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
			// Typically the Players IK will drag behind and "IK walk" while being moved so this function is to prevent that from
			// occuring by Immobilizing the player when they are hooked to a platform and aren't moving relative to the platform.
			if(reduceIKDrift && isHooked) //?#reduceIKDrift
			{
				headRotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
				localPlayer.Immobilize(!(InputMoveH * 0.1f + InputMoveV != 0f)
				&& Mathf.Abs(playerVelocity.x) + Mathf.Abs(playerVelocity.z) > .1f
				&& Mathf.Abs(localPlayer.GetVelocity().x) + Mathf.Abs(localPlayer.GetVelocity().z) < .01f
				&& Quaternion.Angle(new Quaternion(0f, headRotation.y, 0f, headRotation.w).normalized, localPlayer.GetRotation()) < 90f);
			}
			// #LateUpdate_Spherecast. Check for valid platforms.
			if (Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0f, .3f, 0f), 0.25f, new Vector3(0f, -1f, 0f), out hitInfo, hookDistance + .3f, hookLayerMask.value))
			{
				unhookThreshold = 0;
				if (unhookThreshold < 10 && (localPlayer.IsPlayerGrounded())) // Hook to the valid platform if the Player is grounded.
				{
					hook.parent = hitInfo.transform;
					SetProgramVariable(nameof(isHooked), true);
				}
			}
		}

		// INPUT EVENTS: #MoveVertical, #MoveHorizontal, #InputJump.
		public override void InputMoveVertical(float Value, UdonInputEventArgs InputMoveVerticalArgs)
		{
			InputMoveV = InputMoveVerticalArgs.floatValue;
		}
		public override void InputMoveHorizontal(float Value, UdonInputEventArgs InputMoveHorizontalArgs)
		{
			InputMoveH = InputMoveHorizontalArgs.floatValue;
		}
		public void InputJump(bool outputJumpBool, UdonInputEventArgs inputJumpArgs)
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

		// The prefab uses a proxy collider to prevent the player controller from being affected by moving world
		// colliders and this is to prevent the override collider from interacting with other colliders in the scene.
		public void _IgnoreSceneCollision() //#IgnoreSceneCollision
		{
			SendCustomEventDelayedSeconds(nameof(_IgnoreSceneCollision), 60f);
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

		// Disable Override Collider and set Immobilize state false.
		public void OverridesOff()
		{
			if (!localPlayer.IsPlayerGrounded()) { platformOverride.enabled = false; }

			if (reduceIKDrift) { localPlayer.Immobilize(false); }
		}
	}
}

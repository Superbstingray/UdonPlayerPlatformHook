
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using UdonSharp;

namespace superbstingray
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
	public class UdonPlatformHook : UdonSharp.UdonSharpBehaviour
{

	[Tooltip("Layers that the Player will move with.")]
	public LayerMask hookLayerMask;

	[Tooltip("Distance below the Player before the Script hooks to Colliders. You may want to increase this value if your world has a higher than average jump impulse.")]
	public float hookDistance = 0.75F;

	[Tooltip("Will make the Player keep the Velocity of the Platform when they walk off it.")]
	public bool inheritVelocity = true;

	[Tooltip("Will partially reset Avatar Inverse Kinematics periodically when being moved by platforms to prevent Avatar IK drift / IK walk.")]
	public bool reduceIKDrift = true;

	[Tooltip("Detect if the Player has their Main Menu open and stop moving them.")]
	public bool mainMenuPause = true;

	[Tooltip("Detect if the Player has their Quick Menu open and stop moving them.")]
	public bool quickMenuPause = false;

	private VRCPlayerApi localPlayer;
	private RaycastHit hitInfo;
	private Collider[] colliderArray;
	private Collider sceneCollider;
	private BoxCollider platformOverride;
	private Vector3 playerVelocity, playerVelocityLast, hookLastPos, hookOffsetPos, lastFramePos;	
	private Transform PlayerOffset, playerTracker, hook;
	private Quaternion headRotation;
	private int unhookThreshold, localColliders, intUI, fixedUpdateLimit, shouldSkip;
	private bool isHooked, menuOpen;
	private float InputMoveH, InputMoveV;

	[FieldChangeCallback(nameof(_hookChangeStateCallback))]
	private bool hookChangeState;
	public bool _hookChangeStateCallback
	{
		set
		{
			if (hookChangeState)	
			{
				// Upon Unhook functions
				hookChangeState = false;
				hook.localPosition = Vector3.zero;
				hook.eulerAngles = Vector3.zero;
				PlayerOffset.position = hook.position;
				PlayerOffset.rotation = hook.rotation;

				// Set the players velocity to their actual world space velocity.
				if(inheritVelocity)
				{
					localPlayer.SetVelocity(playerVelocity);			
				}

				isHooked = false;
			}
			else
			{
				// Upon Hook functions
				hookChangeState = true;
				hook.localPosition = Vector3.zero;
				hook.eulerAngles = Vector3.zero;
				platformOverride.enabled = true;	
				PlayerOffset.position = hook.position;
				PlayerOffset.rotation = hook.rotation;

				// When hooking count the number of PlayerLocal colliders the player has
				// as this will help us know if the Player has entered a station or not.
				localColliders = Mathf.Clamp(Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length, 1, 100);

				isHooked = true;
			}	

		}

	}

		public void Start() 
		{
			// Set variables
			localPlayer = Networking.LocalPlayer;
			playerTracker = transform.GetChild(0).GetChild(0);
			PlayerOffset = transform.GetChild(0);
			hook = transform.GetChild(1);
			platformOverride = transform.GetComponent<BoxCollider>();
			transform.position = Vector3.zero;
			platformOverride.size = new Vector3(0.5F, 0.05F, 0.5F);
			lastFramePos = localPlayer.GetPosition();

			// Will ovveride from using "Everything" as a layermask
			// as it will interfere with the prefabs functionality.
			// sets "Everything" mask minus MirrorReflection & PlayerLocal.
			if (hookLayerMask.value == -1)
			{
				hookLayerMask.value = -263369;
			}

			// Start collision Check loop
			SendCustomEventDelayedSeconds("_SetIgnoreCollision", 2F);
		}

		public void FixedUpdate() 
		{	
			if (!VRC.SDKBase.Utilities.IsValid(localPlayer))
			{
				return;
			}
			// Average the last X frames of the players global velocity.
			if (isHooked || inheritVelocity)
			{
				playerVelocityLast = playerVelocity;
				playerVelocity = (((playerVelocity * 3F) + ((localPlayer.GetPosition() - lastFramePos) / Time.deltaTime)) / 4F);
				lastFramePos = localPlayer.GetPosition();
			}

			if (!menuOpen)
			{
				// Spherecast downwards from the players position to find a point where the override platform should be.
				Physics.SphereCast((localPlayer.GetPosition() + new Vector3(0F, .3F, 0F)), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, 10F, hookLayerMask.value);
				platformOverride.center = hitInfo.point;

				// Spherecast downwards from the players position and add to the unhookTreshold if it misses a valid platform
				if	(!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .3F, hookLayerMask.value))
				{
					unhookThreshold++;
				}
				else // Otherwise reset the unhookTreshold back to 0
				{
					unhookThreshold = 0;
				} 
			}
		}

		public void Update()
		{
			if (!VRC.SDKBase.Utilities.IsValid(localPlayer))
			{
				return;
			}
			// Move the parented hook to the Players position
			if (isHooked && !menuOpen)
			{
				hookLastPos = hook.position;
				hook.position = localPlayer.GetPosition();
				hookOffsetPos = hook.position - hookLastPos;
			}
		}

		public void LateUpdate() 
		{
			if (!VRC.SDKBase.Utilities.IsValid(localPlayer))
			{
				return;
			}

			#if !UNITY_EDITOR

			// Use OverlapSphere to count the number of InterntalUI colliders as a means to know if the menu is open or not
			// the values are hardcoded and future VRC updates could break this.
			if (mainMenuPause || quickMenuPause && isHooked)
			{
				intUI = Physics.OverlapSphere(localPlayer.GetPosition(), 10F, 524288).Length;
				
				if (mainMenuPause && intUI == 3 || intUI == 12 || intUI == 13) 
				{
					menuOpen = true; 
				}
				else
				{
					if (quickMenuPause && intUI == 8 || intUI == 17 || intUI == 18 || intUI == 19 || intUI == 20) 
					{
						menuOpen = true; 
					}
					else
					{
						menuOpen = false;
					}
				}

				// Typically the Players IK will drag behind and "IK walk" while being moved so this function is to
				// prevent that from occuring by Immobilizing the player when they aren't moving.
				if(reduceIKDrift)
				{
					headRotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
					localPlayer.Immobilize(!(((InputMoveH * 0.1F + InputMoveV) != 0F))
					&& (Mathf.Abs(playerVelocity.x)) + (Mathf.Abs(playerVelocity.z)) > .1F
					&& ((Mathf.Abs(localPlayer.GetVelocity().x)) + (Mathf.Abs(localPlayer.GetVelocity().z)) < .01F)
					&& (Quaternion.Angle(new Quaternion(0F, headRotation.y, 0F, headRotation.w).normalized, localPlayer.GetRotation()) < 90F));
				}
			}

			if (isHooked && menuOpen)
			{
				localPlayer.SetVelocity(Vector3.zero);
			}

			// Teleport the player to the new offset position only if the players PlayerLocal collider count
			// hasn't gone down as then we should assume that means they have entered a station.
			if (isHooked && !menuOpen)
			{
				playerTracker.SetPositionAndRotation(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation);
				PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);

				if (Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length >= localColliders)
				{
					localPlayer.TeleportTo(playerTracker.position - hookOffsetPos, playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
				}
			}
			#else

			// Editor Only duplicate funtion from above specifically for CyanEmu/ClientSim as they don't support
			// Origin tracking. This falls back on using .GetPosition()
			if (isHooked)
			{
				playerTracker.SetPositionAndRotation(localPlayer.GetPosition(), localPlayer.GetRotation());
				PlayerOffset.SetPositionAndRotation(hook.position, hook.rotation);

				if (Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length >= localColliders)
				{
					localPlayer.TeleportTo(playerTracker.position - hookOffsetPos, playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, true);
					hookOffsetPos = Vector3.zero;
				}
			}
			#endif

			// Spherecast downwards from the players position.
			if (!menuOpen && isHooked && !Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .3F, hookLayerMask.value))
			{
				// Add to the unhookTreshold if it misses a valid platform and unhook if unhookThreshold is greater than X.
				unhookThreshold++;
				if (unhookThreshold > 10)
				{
					hook.parent = transform;
					SetProgramVariable("hookChangeState", false);
					SendCustomEventDelayedSeconds("_OverrideOff", 0.5F);
				}
			}
			else // Hook to the valid platform if the Player is grounded.
			{
				if (unhookThreshold < 10 && (localPlayer.IsPlayerGrounded()))
				{
					unhookThreshold = 0;
					hook.parent = hitInfo.transform;
					platformOverride.enabled = true;
					SetProgramVariable("hookChangeState", true);
				}
			}
		}

		public override void InputMoveVertical(float Value, VRC.Udon.Common.UdonInputEventArgs InputMoveVerticalArgs)
		{
			InputMoveV = InputMoveVerticalArgs.floatValue;
		}
		public override void InputMoveHorizontal(float Value, VRC.Udon.Common.UdonInputEventArgs InputMoveHorizontalArgs)
		{
			InputMoveH = InputMoveHorizontalArgs.floatValue;
		}

		// Reset prefab state and call unhook on Respawn.
		public override void OnPlayerRespawn(VRCPlayerApi onPlayerRespawnPlayer) 
		{
			hook.parent = transform;
			unhookThreshold = 35;
			SetProgramVariable("hookChangeState", false);

			localPlayer.SetVelocity(Vector3.zero);

			if (reduceIKDrift)
			{
				localPlayer.Immobilize(false);
			}
		}

		// The prefab uses a proxy collider to prevent the player controller from interacting weirdly with moving world
		// colliders and this is to prevent the override collider from interacting with physics objects in the world.
		public void _SetIgnoreCollision()
		{
			SendCustomEventDelayedSeconds("_SetIgnoreCollision", 60F);

			colliderArray = Physics.OverlapSphere(Vector3.zero, 10000F);
			for (int i = 0; (i < colliderArray.Length); i = (i + 1))
			{
				sceneCollider = colliderArray[i];
				if (VRC.SDKBase.Utilities.IsValid(sceneCollider))
				{
					Physics.IgnoreCollision(sceneCollider, platformOverride);
				}
			}
		}

		// On Unhook disable override collider and force Immobilize state just in case.
		public void _OverrideOff() 
		{
			if (!(localPlayer.IsPlayerGrounded())) 
			{
				platformOverride.enabled = false;
			}
			
			if (reduceIKDrift)
			{
				localPlayer.Immobilize(false);
			}
		}
	}
}
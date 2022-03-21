
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
using UdonSharp;

namespace superbstingray
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
	public class UdonPlatformHook : UdonSharp.UdonSharpBehaviour
{
	[HideInInspector]
	public Transform hook;
	[HideInInspector]
	public Transform playerTracker;
	[HideInInspector]
	public Transform originTracker;
	[HideInInspector]
	public Transform platformOffset;
	[HideInInspector]
	public BoxCollider platformOverride;

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
	private Vector3 lastHookPosition;
	private Vector3 lastHookRotation;
	private Vector3 playerVelocity;
	private Vector3 lastFramePos;	
	private int unhookThreshold;
	private int localColliders;
	private int fixedFrame;
	private int intUI;
	private bool menuOpen;
	private bool isHooked;

	[FieldChangeCallback(nameof(_hookChangeStateCallback))]
	private bool hookChangeState;
	public bool _hookChangeStateCallback
	{
		set
		{
			if (hookChangeState)	
			{
				hookChangeState = false;
				hook.localPosition = Vector3.zero;
				hook.eulerAngles = Vector3.zero;
				originTracker.parent.position = hook.position;
				originTracker.parent.rotation = hook.rotation;
				localPlayer.SetVelocity(playerVelocity);
				isHooked = false;
			}
			else
			{
				hookChangeState = true;
				hook.localPosition = Vector3.zero;
				hook.eulerAngles = Vector3.zero;
				platformOverride.enabled = true;	
				originTracker.parent.position = hook.position;
				originTracker.parent.rotation = hook.rotation;
				localColliders = Mathf.Clamp(Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length, 1, 100);
				isHooked = true;
			}		
		}
	}

		public void Start() 
		{
			localPlayer = Networking.LocalPlayer;
			playerTracker = transform.GetChild(0).GetChild(1);
			originTracker = transform.GetChild(0).GetChild(0);
			hook = transform.GetChild(0).GetChild(0).GetChild(0);
			platformOffset = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0);
			platformOverride = transform.GetChild(1).GetComponent<BoxCollider>();
			transform.position = Vector3.zero;
			platformOverride.size = new Vector3(0.5F, 0.05F, 0.5F);

			SendCustomEventDelayedSeconds("_SetIgnoreCollision", 2F);
		}

		public void FixedUpdate() 
		{
			if (!menuOpen)
			{
				Physics.SphereCast((localPlayer.GetPosition() + new Vector3(0F, .3F, 0F)), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, 10F, hookLayerMask.value);
				platformOverride.center = hitInfo.point;

				if	(!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .3F, hookLayerMask.value))
				{
					unhookThreshold++;
				}
				else
				{
					unhookThreshold = 0;
				} 

				#if !UNITY_EDITOR
				if (isHooked && reduceIKDrift)
				{
					fixedFrame++;
					lastHookPosition = Vector3.Lerp(lastHookPosition, hook.position, 0.025F);
					lastHookRotation = Vector3.Lerp(lastHookRotation, hook.eulerAngles, 0.025F);
					platformOffset.position = Vector3.Lerp(platformOffset.position, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position, 0.05F);
					if (!((Vector3.Distance(platformOffset.position, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position) > 0.1F)) 
					&& ((Vector3.Distance(lastHookPosition, hook.position) + Vector3.Distance(lastHookRotation, hook.eulerAngles)) > 0.1F)) 
					{
						localPlayer.Immobilize((fixedFrame >= 160));
						if ((fixedFrame > 160))
						{
							fixedFrame = 0;
						}
					}
					else
					{
						localPlayer.Immobilize(false);
					}
				}
				#endif
			}
			if (isHooked && inheritVelocity)
			{
				playerVelocity = (((playerVelocity * 10F) + ((localPlayer.GetPosition() - lastFramePos) / Time.deltaTime)) / 11F);
				lastFramePos = localPlayer.GetPosition();
			}
		}
		
		public void LateUpdate() 
		{ 
		#if !UNITY_EDITOR

			if (mainMenuPause || quickMenuPause)
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
			}

			if (isHooked && menuOpen)
			{
				localPlayer.SetVelocity(Vector3.zero);
			}

			if (isHooked && !menuOpen)
			{
				playerTracker.SetPositionAndRotation(localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation);
				originTracker.parent.SetPositionAndRotation(hook.position, hook.rotation);

				if (Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length >= localColliders)
				{
					localPlayer.TeleportTo(playerTracker.position, playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
				}
			}
			#else
			if (isHooked)
			{
				playerTracker.SetPositionAndRotation(localPlayer.GetPosition(), localPlayer.GetRotation());
				originTracker.parent.SetPositionAndRotation(hook.position, hook.rotation);

				if (Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length >= localColliders)
				{
					localPlayer.TeleportTo(playerTracker.position, playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, true);
				}
			}
			#endif
		}

		public void PostLateUpdate() 
		{
			if (!menuOpen && isHooked && !Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .3F, hookLayerMask.value))
			{
				unhookThreshold++;
				if (unhookThreshold > 30)
				{
					hook.parent = originTracker;
					SetProgramVariable("hookChangeState", false);

					SendCustomEventDelayedSeconds("_OverrideOff", 0.5F);
				}
			}
			else
			{
				if (unhookThreshold < 10 && (localPlayer.IsPlayerGrounded()))
				{
					hook.parent = hitInfo.transform;
					platformOverride.enabled = true;
					SetProgramVariable("hookChangeState", true);
				}
			}
		}

		public override void OnPlayerRespawn(VRCPlayerApi onPlayerRespawnPlayer) 
		{
			hook.parent = originTracker;
			unhookThreshold = 35;
			SetProgramVariable("hookChangeState", false);

			localPlayer.SetVelocity(Vector3.zero);

			if (reduceIKDrift)
			{
				localPlayer.Immobilize(false);
			}
		}

		public void _SetIgnoreCollision()
		{
			SendCustomEventDelayedSeconds("_SetIgnoreCollision", 60F);

			colliderArray = Physics.OverlapSphere(Vector3.zero, 10000F);
			for (int i = 0; (i < colliderArray.Length); i = (i + 1))
			{
				sceneCollider = colliderArray[i];
				if (Utilities.IsValid(sceneCollider))
				{
					Physics.IgnoreCollision(sceneCollider, platformOverride);
				}
			}
		}

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
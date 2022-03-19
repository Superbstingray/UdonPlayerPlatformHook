
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

	[Tooltip("Will partially reset Avatar Inverse Kinematics periodically when being moved by platforms to prevent Avatar IK drift / IK walk.")]
	public bool reduceIKDrift = true;

	private VRCPlayerApi localPlayer;
	private RaycastHit hitInfo;
	private Collider[] colliderArray;
	private Collider sceneCollider;
	private Vector3 lastHookPosition;
	private Vector3 lastHookRotation;
	private int unhookThreshold;
	private int localColliders;
	private int fixedFrame;
	private int internalUI;
	private bool menuOpen;

	[FieldChangeCallback(nameof(_IsHookedCallback))]
	private bool IsHooked;
	public bool _IsHookedCallback
	{
		set
		{
			{
				if (IsHooked)	
				{
					IsHooked = false;
					hook.localPosition = Vector3.zero;
					hook.eulerAngles = Vector3.zero;
					originTracker.parent.position = hook.position;
					originTracker.parent.rotation = hook.rotation;

				}
				else
				{
					IsHooked = true;
					hook.localPosition = Vector3.zero;
					hook.eulerAngles = Vector3.zero;
					platformOverride.enabled = true;
					originTracker.parent.position = hook.position;
					originTracker.parent.rotation = hook.rotation;
					localColliders = Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length;
				}
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
			Physics.SphereCast((localPlayer.GetPosition() + new Vector3(0F, .3F, 0F)), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, 10F, hookLayerMask.value);
			platformOverride.center = hitInfo.point;

			if (!menuOpen)
			{
				if	(!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .3F, hookLayerMask.value))
				{
					unhookThreshold++;
				}
				else
				{
					unhookThreshold = 0;
				}
				if (IsHooked)
				{
					if (reduceIKDrift)
					{
						lastHookPosition = Vector3.Lerp(lastHookPosition, hook.position, 0.025F);
						lastHookRotation = Vector3.Lerp(lastHookRotation, hook.eulerAngles, 0.025F);
						platformOffset.position = Vector3.Lerp(platformOffset.position, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position, 0.05F);

						fixedFrame++;
						if (!((Vector3.Distance(platformOffset.position, localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position) > 0.01F))) 
						{
							if (((Vector3.Distance(lastHookPosition, hook.position) + Vector3.Distance(lastHookRotation, hook.eulerAngles)) > 0.01F)) 
							{
								localPlayer.Immobilize((fixedFrame >= 150));
								if ((fixedFrame > 150))
								{
									fixedFrame = 0;
								}
							}
						}
					}
				}
			}
		}

		public void Update() 
		{
			internalUI = Physics.OverlapSphere(localPlayer.GetPosition(), 10F, 524288).Length;
			if ((internalUI >= 3)) 
			{
				menuOpen = true;

				if ((internalUI >= 6)) 
				{
					menuOpen = false;
				}
				else
				{
					menuOpen = true;
				}
			}
			else
			{
				menuOpen = false;
			}
			if (!menuOpen)
			{
				if (IsHooked) 
				{
					playerTracker.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
					playerTracker.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation;
				}
			}
		}

		public void LateUpdate() 
		{
			if (!menuOpen)
			{
				if (IsHooked)
				{
					originTracker.parent.position = hook.position;
					originTracker.parent.rotation = hook.rotation;

					if (Physics.OverlapSphere((localPlayer.GetPosition()), 1024F, 1024).Length >= localColliders)
					{
						localPlayer.TeleportTo(playerTracker.position, localPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, true);
						localPlayer.TeleportTo(localPlayer.GetPosition(), playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
					}
				}
			}
		}

		public void PostLateUpdate() 
		{
			if (!menuOpen)
			{
				if (!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .3F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .3F, hookLayerMask.value))
				{
					unhookThreshold++;
					if (unhookThreshold > 50)
					{
						hook.parent = originTracker;
						SetProgramVariable("IsHooked", false);

						SendCustomEventDelayedSeconds("_OverrideOff", 0.5F);
					}
				}
				else
				{
					unhookThreshold = 0;
					hook.parent = hitInfo.transform;
					platformOverride.enabled = true;
					SetProgramVariable("IsHooked", true);
				}
			}
		}

		public override void OnPlayerRespawn(VRCPlayerApi onPlayerRespawnPlayer) 
		{
			hook.parent = originTracker;
			SetProgramVariable("IsHooked", false);

			if (reduceIKDrift) { localPlayer.Immobilize(false); }
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
			if (!(localPlayer.IsPlayerGrounded())) { platformOverride.enabled = false; }
			
			if (reduceIKDrift) { localPlayer.Immobilize(false); }
		}
	}
}

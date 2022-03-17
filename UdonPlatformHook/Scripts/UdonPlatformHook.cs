
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
	public BoxCollider platformOverride;

	[Tooltip("Layers the Player will move with.")]
	public LayerMask hookLayerMask;
	[Tooltip("Distance below the Player before hooking to Colliders. You may want to increase this value if your world has a higher than average jump impulse.")]
	public float hookDistance = 0.75F;

	private VRCPlayerApi localPlayer;
	private RaycastHit hitInfo;
	private Collider[] nullArray;
	private Collider[] colliderArray;
	private Collider sceneCollider;
	private int unhookThreshold;

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
					originTracker.parent.position = hook.position;
					originTracker.parent.rotation = hook.rotation;
					platformOverride.enabled = true;
				}
			}
		}
	}

		public void Start() 
		{
			hook = transform.GetChild(0).GetChild(0).GetChild(0);
			playerTracker = transform.GetChild(0).GetChild(1);
			originTracker = transform.GetChild(0).GetChild(0);
			platformOverride = transform.GetChild(1).GetComponent<BoxCollider>();
			transform.position = Vector3.zero;
			localPlayer = Networking.LocalPlayer;
			SendCustomEventDelayedSeconds("_SetIgnoreCollision", 2F);
			platformOverride.size = new Vector3(0.5F, 0.035F, 0.5F);
		}

		public void FixedUpdate() 
		{
			Physics.SphereCast((localPlayer.GetPosition() + new Vector3(0F, .25F, 0F)), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, 10F, hookLayerMask.value);
			platformOverride.center = hitInfo.point;
			if (!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .25F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .25F, hookLayerMask.value))
			{
				unhookThreshold++;
			} else
			{
				unhookThreshold = 0;
			}
		}

		public void Update() 
		{
			if (IsHooked) 
			{
				playerTracker.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
				playerTracker.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation;
			}
		}

		public void LateUpdate() 
		{
			if (IsHooked)
			{
				originTracker.parent.position = hook.position;
				originTracker.parent.rotation = hook.rotation;
				nullArray = Physics.OverlapSphere((localPlayer.GetPosition()), 10000F, 1024);
				for(int i=0; i<nullArray.Length; i++)
				{
					if (nullArray[i] == null)
					{
						localPlayer.TeleportTo(playerTracker.position, localPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, true);
						localPlayer.TeleportTo(localPlayer.GetPosition(), playerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
					}
				}
			}
		}

		public void PostLateUpdate() 
		{
			if (!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, .25F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, hookDistance + .25F, hookLayerMask.value))
			{
				unhookThreshold++;
				if (unhookThreshold > 50)
				{
					hook.parent = originTracker;
					SendCustomEventDelayedSeconds("_OverrideOff", 0.5F);
					SetProgramVariable("IsHooked", false);
				}
			} else
				{
					hook.parent = hitInfo.transform;
					SetProgramVariable("IsHooked", true);
					platformOverride.enabled = true;
					unhookThreshold = 0;
				}
		}

		public override void OnPlayerRespawn(VRCPlayerApi onPlayerRespawnPlayer) 
		{
			hook.parent = originTracker;
			SetProgramVariable("IsHooked", false);
		}

		public void _SetIgnoreCollision()
		{
			colliderArray = Physics.OverlapSphere(Vector3.zero, 10000F);
			SendCustomEventDelayedSeconds("_SetIgnoreCollision", 60F);
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
		}
	}
}
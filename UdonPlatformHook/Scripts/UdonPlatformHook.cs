
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
	public Transform Hook;
	[HideInInspector]
	public Transform PlayerTracker;
	[HideInInspector]
	public Transform BaseTransform;
	[HideInInspector]
	public BoxCollider PlatformOverride;
	[HideInInspector]
	public BoxCollider BaseCollider;

	public LayerMask HookLayerMask;
	
	private VRCPlayerApi localPlayer;
	private RaycastHit hitInfo;
	private Collider[] nullArray;
	private Collider[] colliderArray;
	private Collider sceneCollider;
	private Vector3 PlayerPosition;
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
					Hook.localPosition = new Vector3(0F, 0F, 0F);
					Hook.eulerAngles = new Vector3(0F, 0F, 0F);
					BaseTransform.parent.position = Hook.position;
					BaseTransform.parent.rotation = Hook.rotation;
				}
				else
				{
					IsHooked = true;
					Hook.localPosition = new Vector3(0F, 0F, 0F);
					Hook.eulerAngles = new Vector3(0F, 0F, 0F);
					BaseTransform.parent.position = Hook.position;
					BaseTransform.parent.rotation = Hook.rotation;
					PlatformOverride.enabled = true;
				}
			}
		}
	}

		public void Start() 
		{
			localPlayer = Networking.LocalPlayer;
			SendCustomEventDelayedSeconds("_SetIgnoreCollision", 2F);
			PlatformOverride.size = new Vector3(0.5F, 0.035F, 0.5F);
		}

		public void FixedUpdate() 
		{
			Physics.SphereCast((localPlayer.GetPosition() + new Vector3(0F, 1F, 0F)), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, 10F, HookLayerMask.value);
			PlatformOverride.center = hitInfo.point;
			if (!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, 1F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, 1.25F, HookLayerMask.value))
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
				PlayerTracker.position = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).position;
				PlayerTracker.rotation = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin).rotation;
			}
			PlayerPosition.x = localPlayer.GetPosition().x;
			PlayerPosition.z = localPlayer.GetPosition().z;
		}

		public void LateUpdate() 
		{
			if (IsHooked)
			{
				BaseTransform.parent.position = Hook.position;
				BaseTransform.parent.rotation = Hook.rotation;
				nullArray = Physics.OverlapSphere((localPlayer.GetPosition()), 10000F, 1024);
				for(int i=0; i<nullArray.Length; i++)
				{
					if (nullArray[i] == null)
					{ 
						localPlayer.TeleportTo(PlayerTracker.position, localPlayer.GetRotation(), VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint, true);
						localPlayer.TeleportTo(localPlayer.GetPosition(), PlayerTracker.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignRoomWithSpawnPoint, true);
					}
				}
			}
		}

		public void PostLateUpdate() 
		{
			if (!Physics.SphereCast(localPlayer.GetPosition() + new Vector3(0F, 1F, 0F), 0.25F, new Vector3(0F, -90F, 0F), out hitInfo, 1.25F, HookLayerMask.value))
			{
				unhookThreshold++;
				if (unhookThreshold > 50)
				{
					Hook.parent = BaseTransform;
					SendCustomEventDelayedSeconds("_OverrideOff", 0.5F);
					SetProgramVariable("IsHooked", false);
				}
			} else
				{
					Hook.parent = hitInfo.transform;
					SetProgramVariable("IsHooked", true);
					PlatformOverride.enabled = true;
					unhookThreshold = 0;
				}
			Debug.Log(unhookThreshold);
		}

		public override void OnPlayerRespawn(VRCPlayerApi onPlayerRespawnPlayer) 
		{
			Hook.parent = BaseTransform;
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
					Physics.IgnoreCollision(sceneCollider, PlatformOverride);
				}
			}
		}

		public void _OverrideOff() 
		{
			if (!(localPlayer.IsPlayerGrounded())) { PlatformOverride.enabled = false; }
		}
	}
}

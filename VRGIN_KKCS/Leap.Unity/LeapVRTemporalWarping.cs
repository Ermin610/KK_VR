using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;

namespace Leap.Unity;

public class LeapVRTemporalWarping : MonoBehaviour
{
	public enum WarpedAnchor
	{
		CENTER,
		LEFT,
		RIGHT
	}

	public enum SyncMode
	{
		SYNC_WITH_HANDS,
		LOW_LATENCY
	}

	protected struct TransformData
	{
		public long leapTime;

		public Vector3 localPosition;

		public Quaternion localRotation;

		public static TransformData Lerp(TransformData from, TransformData to, long time)
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			if (from.leapTime == to.leapTime)
			{
				return from;
			}
			float num = (float)(time - from.leapTime) / (float)(to.leapTime - from.leapTime);
			TransformData result = default(TransformData);
			result.leapTime = time;
			result.localPosition = Vector3.Lerp(from.localPosition, to.localPosition, num);
			result.localRotation = Quaternion.Slerp(from.localRotation, to.localRotation, num);
			return result;
		}
	}

	private const long MAX_LATENCY = 200000L;

	[SerializeField]
	private LeapServiceProvider provider;

	[Tooltip("The transform that represents the head object.")]
	[SerializeField]
	private Transform _headTransform;

	[Tooltip("The transform that is the anchor that tracking movement is relative to.  Can be null if head motion is in world space.")]
	[SerializeField]
	private Transform _trackingAnchor;

	[Tooltip("Key to recenter the VR tracking space.")]
	[SerializeField]
	private KeyCode recenter = (KeyCode)114;

	[Tooltip("Allows smooth enabling or disabling of the Image-Warping feature.  Usually should match rotation warping.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float tweenImageWarping;

	[Tooltip("Allows smooth enabling or disabling of the Rotational warping of Leap Space.  Usually should match image warping.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float tweenRotationalWarping;

	[Tooltip("Allows smooth enabling or disabling of the Positional warping of Leap Space.  Usually should be disabled when using image warping.")]
	[Range(0f, 1f)]
	[SerializeField]
	private float tweenPositionalWarping;

	[Tooltip("Controls when this script synchronizes the time warp of images.  Use LowLatency for AR, and SyncWithHands for VR.")]
	[SerializeField]
	private SyncMode syncMode;

	[Tooltip("Allow manual adjustment of the rewind time.")]
	[SerializeField]
	private bool allowManualTimeAlignment;

	[Tooltip("Timestamps and other uncertanties can lead to sub-optimal alignment, this value can be tuned to get desired alignment.")]
	[SerializeField]
	private int warpingAdjustment = 60;

	[SerializeField]
	private KeyCode unlockHold = (KeyCode)303;

	[SerializeField]
	private KeyCode moreRewind = (KeyCode)276;

	[SerializeField]
	private KeyCode lessRewind = (KeyCode)275;

	private LeapDeviceInfo deviceInfo;

	private Matrix4x4 _projectionMatrix;

	private List<TransformData> _history = new List<TransformData>();

	public float TweenImageWarping
	{
		get
		{
			return tweenImageWarping;
		}
		set
		{
			tweenImageWarping = Mathf.Clamp01(value);
		}
	}

	public float TweenRotationalWarping
	{
		get
		{
			return tweenRotationalWarping;
		}
		set
		{
			tweenRotationalWarping = Mathf.Clamp01(value);
		}
	}

	public float TweenPositionalWarping
	{
		get
		{
			return tweenPositionalWarping;
		}
		set
		{
			tweenPositionalWarping = Mathf.Clamp01(value);
		}
	}

	public SyncMode TemporalSyncMode
	{
		get
		{
			return syncMode;
		}
		set
		{
			syncMode = value;
		}
	}

	public float RewindAdjust => warpingAdjustment;

	public bool TryGetWarpedTransform(WarpedAnchor anchor, out Vector3 rewoundPosition, out Quaternion rewoundRotation, long leapTime)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_headTransform == (Object)null)
		{
			rewoundPosition = Vector3.one;
			rewoundRotation = Quaternion.identity;
			return false;
		}
		TransformData transformData = transformAtTime(leapTime - warpingAdjustment * 1000);
		if ((Object)(object)_trackingAnchor == (Object)null)
		{
			rewoundRotation = transformData.localRotation;
			rewoundPosition = transformData.localPosition + rewoundRotation * Vector3.forward * deviceInfo.focalPlaneOffset;
		}
		else
		{
			rewoundRotation = _trackingAnchor.rotation * transformData.localRotation;
			rewoundPosition = _trackingAnchor.TransformPoint(transformData.localPosition) + rewoundRotation * Vector3.forward * deviceInfo.focalPlaneOffset;
		}
		switch (anchor)
		{
		case WarpedAnchor.LEFT:
			rewoundPosition += rewoundRotation * Vector3.left * deviceInfo.baseline * 0.5f;
			break;
		case WarpedAnchor.RIGHT:
			rewoundPosition += rewoundRotation * Vector3.right * deviceInfo.baseline * 0.5f;
			break;
		default:
			throw new Exception("Unexpected Rewind Type " + anchor);
		case WarpedAnchor.CENTER:
			break;
		}
		return true;
	}

	public bool TryGetWarpedTransform(WarpedAnchor anchor, out Vector3 rewoundPosition, out Quaternion rewoundRotation)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		long timestamp = provider.CurrentFrame.Timestamp;
		if (TryGetWarpedTransform(anchor, out rewoundPosition, out rewoundRotation, timestamp))
		{
			return true;
		}
		rewoundPosition = Vector3.zero;
		rewoundRotation = Quaternion.identity;
		return false;
	}

	public void ManualyUpdateTemporalWarping()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if ((Object)(object)_trackingAnchor == (Object)null)
		{
			updateHistory(_headTransform.position, _headTransform.rotation);
			updateTemporalWarping(_headTransform.position, _headTransform.rotation);
		}
		else
		{
			updateHistory(_trackingAnchor.InverseTransformPoint(_headTransform.position), Quaternion.Inverse(_trackingAnchor.rotation) * _headTransform.rotation);
		}
	}

	protected void Start()
	{
		if (provider.IsConnected())
		{
			deviceInfo = provider.GetDeviceInfo();
			LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
			if (deviceInfo.type == LeapDeviceType.Invalid)
			{
				Debug.LogWarning((object)"Invalid Leap Device -> enabled = false");
				((Behaviour)this).enabled = false;
			}
		}
		else
		{
			((MonoBehaviour)this).StartCoroutine(waitForConnection());
			provider.GetLeapController().Device += OnDevice;
		}
	}

	private IEnumerator waitForConnection()
	{
		while (!provider.IsConnected())
		{
			yield return null;
		}
		LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
		LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
	}

	protected void OnDevice(object sender, DeviceEventArgs args)
	{
		deviceInfo = provider.GetDeviceInfo();
		if (deviceInfo.type == LeapDeviceType.Invalid)
		{
			Debug.LogWarning((object)"Invalid Leap Device -> enabled = false");
			((Behaviour)this).enabled = false;
		}
		else
		{
			LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
			LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
		}
	}

	protected void OnEnable()
	{
		if (deviceInfo.type != 0)
		{
			LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
			LeapVRCameraControl.OnValidCameraParams += onValidCameraParams;
		}
	}

	protected void OnDisable()
	{
		LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
	}

	protected void OnDestroy()
	{
		LeapVRCameraControl.OnValidCameraParams -= onValidCameraParams;
	}

	protected void Update()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if (Input.GetKeyDown(recenter) && VRSettings.enabled && VRDevice.isPresent)
		{
			InputTracking.Recenter();
		}
		if (allowManualTimeAlignment && ((int)unlockHold == 0 || Input.GetKey(unlockHold)))
		{
			if (Input.GetKeyDown(moreRewind))
			{
				warpingAdjustment++;
			}
			if (Input.GetKeyDown(lessRewind))
			{
				warpingAdjustment--;
			}
		}
	}

	protected void LateUpdate()
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (VRSettings.enabled)
		{
			updateTemporalWarping(InputTracking.GetLocalPosition((VRNode)2), InputTracking.GetLocalRotation((VRNode)2));
		}
	}

	private void onValidCameraParams(LeapVRCameraControl.CameraParams cameraParams)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		_projectionMatrix = cameraParams.ProjectionMatrix;
		if (VRSettings.enabled)
		{
			if ((Object)(object)provider != (Object)null)
			{
				updateHistory(InputTracking.GetLocalPosition((VRNode)2), InputTracking.GetLocalRotation((VRNode)2));
			}
			if (syncMode == SyncMode.LOW_LATENCY)
			{
				updateTemporalWarping(InputTracking.GetLocalPosition((VRNode)2), InputTracking.GetLocalRotation((VRNode)2));
			}
		}
	}

	private void updateHistory(Vector3 currLocalPosition, Quaternion currLocalRotation)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		long num = provider.GetLeapController().Now();
		_history.Add(new TransformData
		{
			leapTime = num,
			localPosition = currLocalPosition,
			localRotation = currLocalRotation
		});
		while (_history.Count > 0 && 200000 < num - _history[0].leapTime)
		{
			_history.RemoveAt(0);
		}
	}

	private void updateTemporalWarping(Vector3 currLocalPosition, Quaternion currLocalRotation)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		if (!((Object)(object)_trackingAnchor == (Object)null) && provider.GetLeapController() != null)
		{
			Vector3 val = _trackingAnchor.TransformPoint(currLocalPosition);
			Quaternion val2 = _trackingAnchor.rotation * currLocalRotation;
			long time = provider.CurrentFrame.Timestamp - warpingAdjustment * 1000;
			TransformData transformData = transformAtTime(time);
			Vector3 val3 = _trackingAnchor.TransformPoint(transformData.localPosition);
			Quaternion val4 = _trackingAnchor.rotation * transformData.localRotation;
			Quaternion val5 = Quaternion.Slerp(val2, val4, tweenImageWarping);
			Quaternion val6 = Quaternion.Inverse(val2) * val5;
			Matrix4x4 val7 = _projectionMatrix * Matrix4x4.TRS(Vector3.zero, val6, Vector3.one) * ((Matrix4x4)(ref _projectionMatrix)).inverse;
			Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", val7);
			((Component)this).transform.position = Vector3.Lerp(val, val3, tweenPositionalWarping);
			((Component)this).transform.rotation = Quaternion.Slerp(val2, val4, tweenRotationalWarping);
			Transform transform = ((Component)this).transform;
			transform.position += ((Component)this).transform.forward * deviceInfo.focalPlaneOffset;
		}
	}

	private TransformData transformAtTime(long time)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (_history.Count == 0)
		{
			TransformData result = default(TransformData);
			result.leapTime = 0L;
			result.localPosition = Vector3.zero;
			result.localRotation = Quaternion.identity;
			return result;
		}
		if (_history[0].leapTime >= time)
		{
			return _history[0];
		}
		int i;
		for (i = 1; i < _history.Count && _history[i].leapTime <= time; i++)
		{
		}
		if (i >= _history.Count)
		{
			return _history[_history.Count - 1];
		}
		return TransformData.Lerp(_history[i - 1], _history[i], time);
	}
}

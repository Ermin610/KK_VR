using System;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Rendering;

namespace Leap.Unity;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class LeapVRCameraControl : MonoBehaviour
{
	public struct CameraParams
	{
		public readonly Transform CenterEyeTransform;

		public readonly Matrix4x4 ProjectionMatrix;

		public readonly int Width;

		public readonly int Height;

		public CameraParams(Camera camera)
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Invalid comparison between Unknown and I4
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Invalid comparison between Unknown and I4
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Invalid comparison between Unknown and I4
			CenterEyeTransform = ((Component)camera).transform;
			ProjectionMatrix = camera.projectionMatrix;
			GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;
			if ((int)graphicsDeviceType == 1 || (int)graphicsDeviceType == 2 || (int)graphicsDeviceType == 18)
			{
				for (int i = 0; i < 4; i++)
				{
					ProjectionMatrix[1, i] = 0f - ProjectionMatrix[1, i];
				}
				for (int j = 0; j < 4; j++)
				{
					ProjectionMatrix[2, j] = ProjectionMatrix[2, j] * 0.5f + ProjectionMatrix[3, j] * 0.5f;
				}
			}
			Width = camera.pixelWidth;
			Height = camera.pixelHeight;
		}
	}

	public const string GLOBAL_EYE_UV_OFFSET_NAME = "_LeapGlobalStereoUVOffset";

	private static Vector2 LEFT_EYE_UV_OFFSET = new Vector2(0f, 0f);

	private static Vector2 RIGHT_EYE_UV_OFFSET = new Vector2(0f, 0.5f);

	private static bool _hasDispatchedValidCameraParams = false;

	[SerializeField]
	private EyeType _eyeType = new EyeType(EyeType.OrderType.CENTER);

	[SerializeField]
	private bool _overrideEyePosition = true;

	private Camera _cachedCamera;

	private Matrix4x4 _finalCenterMatrix;

	private LeapDeviceInfo _deviceInfo;

	public bool OverrideEyePosition
	{
		get
		{
			return _overrideEyePosition;
		}
		set
		{
			_overrideEyePosition = value;
		}
	}

	private Camera _camera
	{
		get
		{
			if (_cachedCamera == null)
			{
				_cachedCamera = ((Component)this).GetComponent<Camera>();
			}
			return _cachedCamera;
		}
	}

	public static event Action<CameraParams> OnValidCameraParams;

	public static event Action<Camera> OnLeftPreRender;

	public static event Action<Camera> OnRightPreRender;

	private void Start()
	{
		_deviceInfo = new LeapDeviceInfo(LeapDeviceType.Peripheral);
	}

	private void Update()
	{
		_hasDispatchedValidCameraParams = false;
	}

	private void OnPreCull()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		_camera.ResetWorldToCameraMatrix();
		_finalCenterMatrix = _camera.worldToCameraMatrix;
		if (!_hasDispatchedValidCameraParams)
		{
			CameraParams obj = new CameraParams(_cachedCamera);
			if (LeapVRCameraControl.OnValidCameraParams != null)
			{
				LeapVRCameraControl.OnValidCameraParams(obj);
			}
			_hasDispatchedValidCameraParams = true;
		}
	}

	private void OnPreRender()
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		_eyeType.BeginCamera();
		if (_eyeType.IsLeftEye)
		{
			Shader.SetGlobalVector("_LeapGlobalStereoUVOffset", ((Vector4)(LEFT_EYE_UV_OFFSET)));
			if (LeapVRCameraControl.OnLeftPreRender != null)
			{
				LeapVRCameraControl.OnLeftPreRender(_cachedCamera);
			}
		}
		else
		{
			Shader.SetGlobalVector("_LeapGlobalStereoUVOffset", ((Vector4)(RIGHT_EYE_UV_OFFSET)));
			if (LeapVRCameraControl.OnRightPreRender != null)
			{
				LeapVRCameraControl.OnRightPreRender(_cachedCamera);
			}
		}
		Matrix4x4 finalCenterMatrix;
		if (_overrideEyePosition)
		{
			finalCenterMatrix = _finalCenterMatrix;
			Vector3 val = (float)(_eyeType.IsLeftEye ? 1 : (-1)) * ((Component)this).transform.right * _deviceInfo.baseline * 0.5f;
			Vector3 val2 = -((Component)this).transform.forward * _deviceInfo.focalPlaneOffset;
			finalCenterMatrix *= Matrix4x4.TRS(val + val2, Quaternion.identity, Vector3.one);
		}
		else
		{
			finalCenterMatrix = _camera.worldToCameraMatrix;
		}
		_camera.worldToCameraMatrix = finalCenterMatrix;
	}
}

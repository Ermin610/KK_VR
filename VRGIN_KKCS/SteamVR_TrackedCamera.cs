using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

public class SteamVR_TrackedCamera
{
	public class VideoStreamTexture
	{
		private Texture2D _texture;

		private int prevFrameCount = -1;

		private uint glTextureId;

		private VideoStream videostream;

		private CameraVideoStreamFrameHeader_t header;

		public bool undistorted { get; private set; }

		public uint deviceIndex => videostream.deviceIndex;

		public bool hasCamera => videostream.hasCamera;

		public bool hasTracking
		{
			get
			{
				Update();
				return header.standingTrackedDevicePose.bPoseIsValid;
			}
		}

		public uint frameId
		{
			get
			{
				Update();
				return header.nFrameSequence;
			}
		}

		public VRTextureBounds_t frameBounds { get; private set; }

		public EVRTrackedCameraFrameType frameType
		{
			get
			{
				if (!undistorted)
				{
					return EVRTrackedCameraFrameType.Distorted;
				}
				return EVRTrackedCameraFrameType.Undistorted;
			}
		}

		public Texture2D texture
		{
			get
			{
				Update();
				return _texture;
			}
		}

		public SteamVR_Utils.RigidTransform transform
		{
			get
			{
				Update();
				return new SteamVR_Utils.RigidTransform(header.standingTrackedDevicePose.mDeviceToAbsoluteTracking);
			}
		}

		public Vector3 velocity
		{
			get
			{
				//IL_0034: Unknown result type (might be due to invalid IL or missing references)
				Update();
				TrackedDevicePose_t standingTrackedDevicePose = header.standingTrackedDevicePose;
				return new Vector3(standingTrackedDevicePose.vVelocity.v0, standingTrackedDevicePose.vVelocity.v1, 0f - standingTrackedDevicePose.vVelocity.v2);
			}
		}

		public Vector3 angularVelocity
		{
			get
			{
				//IL_0035: Unknown result type (might be due to invalid IL or missing references)
				Update();
				TrackedDevicePose_t standingTrackedDevicePose = header.standingTrackedDevicePose;
				return new Vector3(0f - standingTrackedDevicePose.vAngularVelocity.v0, 0f - standingTrackedDevicePose.vAngularVelocity.v1, standingTrackedDevicePose.vAngularVelocity.v2);
			}
		}

		public VideoStreamTexture(uint deviceIndex, bool undistorted)
		{
			this.undistorted = undistorted;
			videostream = Stream(deviceIndex);
		}

		public TrackedDevicePose_t GetPose()
		{
			Update();
			return header.standingTrackedDevicePose;
		}

		public ulong Acquire()
		{
			return videostream.Acquire();
		}

		public ulong Release()
		{
			ulong result = videostream.Release();
			if (videostream.handle == 0L)
			{
				Object.Destroy((Object)(object)_texture);
				_texture = null;
			}
			return result;
		}

		private void Update()
		{
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			if (Time.frameCount == prevFrameCount)
			{
				return;
			}
			prevFrameCount = Time.frameCount;
			if (videostream.handle == 0L)
			{
				return;
			}
			SteamVR instance = SteamVR.instance;
			if (instance == null)
			{
				return;
			}
			CVRTrackedCamera trackedCamera = OpenVR.TrackedCamera;
			if (trackedCamera == null)
			{
				return;
			}
			IntPtr ppD3D11ShaderResourceView = IntPtr.Zero;
			Texture2D val = (Texture2D)(((Object)(object)_texture != (Object)null) ? ((object)_texture) : ((object)new Texture2D(2, 2)));
			uint nFrameHeaderSize = (uint)Marshal.SizeOf(header.GetType());
			if (instance.textureType == ETextureType.OpenGL)
			{
				if (glTextureId != 0)
				{
					trackedCamera.ReleaseVideoStreamTextureGL(videostream.handle, glTextureId);
				}
				if (trackedCamera.GetVideoStreamTextureGL(videostream.handle, frameType, ref glTextureId, ref header, nFrameHeaderSize) != 0)
				{
					return;
				}
				ppD3D11ShaderResourceView = (IntPtr)glTextureId;
			}
			else if (instance.textureType == ETextureType.DirectX && trackedCamera.GetVideoStreamTextureD3D11(videostream.handle, frameType, ((Texture)val).GetNativeTexturePtr(), ref ppD3D11ShaderResourceView, ref header, nFrameHeaderSize) != 0)
			{
				return;
			}
			if ((Object)(object)_texture == (Object)null)
			{
				_texture = Texture2D.CreateExternalTexture((int)header.nWidth, (int)header.nHeight, (TextureFormat)4, false, false, ppD3D11ShaderResourceView);
				uint pnWidth = 0u;
				uint pnHeight = 0u;
				VRTextureBounds_t pTextureBounds = default(VRTextureBounds_t);
				if (trackedCamera.GetVideoStreamTextureSize(deviceIndex, frameType, ref pTextureBounds, ref pnWidth, ref pnHeight) == EVRTrackedCameraError.None)
				{
					pTextureBounds.vMin = 1f - pTextureBounds.vMin;
					pTextureBounds.vMax = 1f - pTextureBounds.vMax;
					frameBounds = pTextureBounds;
				}
			}
			else
			{
				_texture.UpdateExternalTexture(ppD3D11ShaderResourceView);
			}
		}
	}

	private class VideoStream
	{
		private ulong _handle;

		private bool _hasCamera;

		private ulong refCount;

		public uint deviceIndex { get; private set; }

		public ulong handle => _handle;

		public bool hasCamera => _hasCamera;

		public VideoStream(uint deviceIndex)
		{
			this.deviceIndex = deviceIndex;
			OpenVR.TrackedCamera?.HasCamera(deviceIndex, ref _hasCamera);
		}

		public ulong Acquire()
		{
			if (_handle == 0L && hasCamera)
			{
				OpenVR.TrackedCamera?.AcquireVideoStreamingService(deviceIndex, ref _handle);
			}
			return ++refCount;
		}

		public ulong Release()
		{
			if (refCount != 0 && --refCount == 0L && _handle != 0L)
			{
				OpenVR.TrackedCamera?.ReleaseVideoStreamingService(_handle);
				_handle = 0uL;
			}
			return refCount;
		}
	}

	private static VideoStreamTexture[] distorted;

	private static VideoStreamTexture[] undistorted;

	private static VideoStream[] videostreams;

	public static VideoStreamTexture Distorted(int deviceIndex = 0)
	{
		if (distorted == null)
		{
			distorted = new VideoStreamTexture[64];
		}
		if (distorted[deviceIndex] == null)
		{
			distorted[deviceIndex] = new VideoStreamTexture((uint)deviceIndex, undistorted: false);
		}
		return distorted[deviceIndex];
	}

	public static VideoStreamTexture Undistorted(int deviceIndex = 0)
	{
		if (undistorted == null)
		{
			undistorted = new VideoStreamTexture[64];
		}
		if (undistorted[deviceIndex] == null)
		{
			undistorted[deviceIndex] = new VideoStreamTexture((uint)deviceIndex, undistorted: true);
		}
		return undistorted[deviceIndex];
	}

	public static VideoStreamTexture Source(bool undistorted, int deviceIndex = 0)
	{
		if (!undistorted)
		{
			return Distorted(deviceIndex);
		}
		return Undistorted(deviceIndex);
	}

	private static VideoStream Stream(uint deviceIndex)
	{
		if (videostreams == null)
		{
			videostreams = new VideoStream[64];
		}
		if (videostreams[deviceIndex] == null)
		{
			videostreams[deviceIndex] = new VideoStream(deviceIndex);
		}
		return videostreams[deviceIndex];
	}
}

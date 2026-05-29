using System.Runtime.InteropServices;
using UnityEngine;
using Valve.VR;

public class SteamVR_Overlay : MonoBehaviour
{
	public struct IntersectionResults
	{
		public Vector3 point;

		public Vector3 normal;

		public Vector2 UVs;

		public float distance;
	}

	public Texture texture;

	public bool curved = true;

	public bool antialias = true;

	public bool highquality = true;

	[Tooltip("Size of overlay view.")]
	public float scale = 3f;

	[Tooltip("Distance from surface.")]
	public float distance = 1.25f;

	[Tooltip("Opacity")]
	[Range(0f, 1f)]
	public float alpha = 1f;

	public Vector4 uvOffset = new Vector4(0f, 0f, 1f, 1f);

	public Vector2 mouseScale = new Vector2(1f, 1f);

	public Vector2 curvedRange = new Vector2(1f, 2f);

	public VROverlayInputMethod inputMethod;

	private ulong handle;

	public static SteamVR_Overlay instance { get; private set; }

	public static string key => "unity:" + Application.companyName + "." + Application.productName;

	private void OnEnable()
	{
		CVROverlay overlay = OpenVR.Overlay;
		if (overlay != null)
		{
			EVROverlayError eVROverlayError = overlay.CreateOverlay(key, ((Object)((Component)this).gameObject).name, ref handle);
			if (eVROverlayError != 0)
			{
				Debug.Log((object)overlay.GetOverlayErrorNameFromEnum(eVROverlayError));
				((Behaviour)this).enabled = false;
				return;
			}
		}
		instance = this;
	}

	private void OnDisable()
	{
		if (handle != 0L)
		{
			OpenVR.Overlay?.DestroyOverlay(handle);
			handle = 0uL;
		}
		instance = null;
	}

	public void UpdateOverlay()
	{
		//IL_0202: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		CVROverlay overlay = OpenVR.Overlay;
		if (overlay == null)
		{
			return;
		}
		if (texture != null)
		{
			EVROverlayError eVROverlayError = overlay.ShowOverlay(handle);
			if ((eVROverlayError != EVROverlayError.InvalidHandle && eVROverlayError != EVROverlayError.UnknownOverlay) || overlay.FindOverlay(key, ref handle) == EVROverlayError.None)
			{
				Texture_t pTexture = default(Texture_t);
				pTexture.handle = texture.GetNativeTexturePtr();
				pTexture.eType = SteamVR.instance.textureType;
				pTexture.eColorSpace = EColorSpace.Auto;
				overlay.SetOverlayTexture(handle, ref pTexture);
				overlay.SetOverlayAlpha(handle, alpha);
				overlay.SetOverlayWidthInMeters(handle, scale);
				overlay.SetOverlayAutoCurveDistanceRangeInMeters(handle, curvedRange.x, curvedRange.y);
				VRTextureBounds_t pOverlayTextureBounds = default(VRTextureBounds_t);
				pOverlayTextureBounds.uMin = (0f + uvOffset.x) * uvOffset.z;
				pOverlayTextureBounds.vMin = (1f + uvOffset.y) * uvOffset.w;
				pOverlayTextureBounds.uMax = (1f + uvOffset.x) * uvOffset.z;
				pOverlayTextureBounds.vMax = (0f + uvOffset.y) * uvOffset.w;
				overlay.SetOverlayTextureBounds(handle, ref pOverlayTextureBounds);
				HmdVector2_t pvecMouseScale = default(HmdVector2_t);
				pvecMouseScale.v0 = mouseScale.x;
				pvecMouseScale.v1 = mouseScale.y;
				overlay.SetOverlayMouseScale(handle, ref pvecMouseScale);
				SteamVR_Camera steamVR_Camera = SteamVR_Render.Top();
				if (steamVR_Camera != null && steamVR_Camera.origin != null)
				{
					SteamVR_Utils.RigidTransform rigidTransform = new SteamVR_Utils.RigidTransform(steamVR_Camera.origin, ((Component)this).transform);
					rigidTransform.pos.x /= steamVR_Camera.origin.localScale.x;
					rigidTransform.pos.y /= steamVR_Camera.origin.localScale.y;
					rigidTransform.pos.z /= steamVR_Camera.origin.localScale.z;
					rigidTransform.pos.z += distance;
					HmdMatrix34_t pmatTrackingOriginToOverlayTransform = rigidTransform.ToHmdMatrix34();
					overlay.SetOverlayTransformAbsolute(handle, SteamVR_Render.instance.trackingSpace, ref pmatTrackingOriginToOverlayTransform);
				}
				overlay.SetOverlayInputMethod(handle, inputMethod);
				if (curved || antialias)
				{
					highquality = true;
				}
				if (highquality)
				{
					overlay.SetHighQualityOverlay(handle);
					overlay.SetOverlayFlag(handle, VROverlayFlags.Curved, curved);
					overlay.SetOverlayFlag(handle, VROverlayFlags.RGSS4X, antialias);
				}
				else if (overlay.GetHighQualityOverlay() == handle)
				{
					overlay.SetHighQualityOverlay(0uL);
				}
			}
		}
		else
		{
			overlay.HideOverlay(handle);
		}
	}

	public bool PollNextEvent(ref VREvent_t pEvent)
	{
		CVROverlay overlay = OpenVR.Overlay;
		if (overlay == null)
		{
			return false;
		}
		uint uncbVREvent = (uint)Marshal.SizeOf(typeof(VREvent_t));
		return overlay.PollNextOverlayEvent(handle, ref pEvent, uncbVREvent);
	}

	public bool ComputeIntersection(Vector3 source, Vector3 direction, ref IntersectionResults results)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		CVROverlay overlay = OpenVR.Overlay;
		if (overlay == null)
		{
			return false;
		}
		VROverlayIntersectionParams_t pParams = default(VROverlayIntersectionParams_t);
		pParams.eOrigin = SteamVR_Render.instance.trackingSpace;
		pParams.vSource.v0 = source.x;
		pParams.vSource.v1 = source.y;
		pParams.vSource.v2 = 0f - source.z;
		pParams.vDirection.v0 = direction.x;
		pParams.vDirection.v1 = direction.y;
		pParams.vDirection.v2 = 0f - direction.z;
		VROverlayIntersectionResults_t pResults = default(VROverlayIntersectionResults_t);
		if (!overlay.ComputeOverlayIntersection(handle, ref pParams, ref pResults))
		{
			return false;
		}
		results.point = new Vector3(pResults.vPoint.v0, pResults.vPoint.v1, 0f - pResults.vPoint.v2);
		results.normal = new Vector3(pResults.vNormal.v0, pResults.vNormal.v1, 0f - pResults.vNormal.v2);
		results.UVs = new Vector2(pResults.vUVs.v0, pResults.vUVs.v1);
		results.distance = pResults.fDistance;
		return true;
	}
}

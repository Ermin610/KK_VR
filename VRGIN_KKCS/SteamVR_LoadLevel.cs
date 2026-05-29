using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using UnityEngine.SceneManagement;
using Valve.VR;

public class SteamVR_LoadLevel : MonoBehaviour
{
	private static SteamVR_LoadLevel _active;

	public string levelName;

	public string internalProcessPath;

	public string internalProcessArgs;

	public bool loadAdditive;

	public bool loadAsync = true;

	public Texture loadingScreen;

	public Texture progressBarEmpty;

	public Texture progressBarFull;

	public float loadingScreenWidthInMeters = 6f;

	public float progressBarWidthInMeters = 3f;

	public float loadingScreenDistance;

	public Transform loadingScreenTransform;

	public Transform progressBarTransform;

	public Texture front;

	public Texture back;

	public Texture left;

	public Texture right;

	public Texture top;

	public Texture bottom;

	public Color backgroundColor = Color.black;

	public bool showGrid;

	public float fadeOutTime = 0.5f;

	public float fadeInTime = 0.5f;

	public float postLoadSettleTime;

	public float loadingScreenFadeInTime = 1f;

	public float loadingScreenFadeOutTime = 0.25f;

	private float fadeRate = 1f;

	private float alpha;

	private AsyncOperation async;

	private RenderTexture renderTexture;

	private ulong loadingScreenOverlayHandle;

	private ulong progressBarOverlayHandle;

	public bool autoTriggerOnEnable;

	public static bool loading => _active != null;

	public static float progress
	{
		get
		{
			if (!(_active != null) || _active.async == null)
			{
				return 0f;
			}
			return _active.async.progress;
		}
	}

	public static Texture progressTexture
	{
		get
		{
			if (!(_active != null))
			{
				return null;
			}
			return (Texture)(object)_active.renderTexture;
		}
	}

	private void OnEnable()
	{
		if (autoTriggerOnEnable)
		{
			Trigger();
		}
	}

	public void Trigger()
	{
		if (!loading && !string.IsNullOrEmpty(levelName))
		{
			((MonoBehaviour)this).StartCoroutine(LoadLevel());
		}
	}

	public static void Begin(string levelName, bool showGrid = false, float fadeOutTime = 0.5f, float r = 0f, float g = 0f, float b = 0f, float a = 1f)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		SteamVR_LoadLevel steamVR_LoadLevel = new GameObject("loader").AddComponent<SteamVR_LoadLevel>();
		steamVR_LoadLevel.levelName = levelName;
		steamVR_LoadLevel.showGrid = showGrid;
		steamVR_LoadLevel.fadeOutTime = fadeOutTime;
		steamVR_LoadLevel.backgroundColor = new Color(r, g, b, a);
		steamVR_LoadLevel.Trigger();
	}

	private void OnGUI()
	{
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Invalid comparison between Unknown and I4
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Expected O, but got Unknown
		//IL_0108: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		if (_active != this || !(progressBarEmpty != null) || !(progressBarFull != null))
		{
			return;
		}
		if (progressBarOverlayHandle == 0L)
		{
			progressBarOverlayHandle = GetOverlayHandle("progressBar", (progressBarTransform != null) ? progressBarTransform : ((Component)this).transform, progressBarWidthInMeters);
		}
		if (progressBarOverlayHandle != 0L)
		{
			float num = ((async != null) ? async.progress : 0f);
			int width = progressBarFull.width;
			int height = progressBarFull.height;
			if (renderTexture == null)
			{
				renderTexture = new RenderTexture(width, height, 0);
				renderTexture.Create();
			}
			RenderTexture active = RenderTexture.active;
			RenderTexture.active = renderTexture;
			if ((int)Event.current.type == 7)
			{
				GL.Clear(false, true, Color.clear);
			}
			GUILayout.BeginArea(new Rect(0f, 0f, (float)width, (float)height));
			GUI.DrawTexture(new Rect(0f, 0f, (float)width, (float)height), progressBarEmpty);
			GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, num * (float)width, (float)height), progressBarFull, new Rect(0f, 0f, num, 1f));
			GUILayout.EndArea();
			RenderTexture.active = active;
			CVROverlay overlay = OpenVR.Overlay;
			if (overlay != null)
			{
				Texture_t pTexture = default(Texture_t);
				pTexture.handle = ((Texture)renderTexture).GetNativeTexturePtr();
				pTexture.eType = SteamVR.instance.textureType;
				pTexture.eColorSpace = EColorSpace.Auto;
				overlay.SetOverlayTexture(progressBarOverlayHandle, ref pTexture);
			}
		}
	}

	private void Update()
	{
		if (_active != this)
		{
			return;
		}
		alpha = Mathf.Clamp01(alpha + fadeRate * Time.deltaTime);
		CVROverlay overlay = OpenVR.Overlay;
		if (overlay != null)
		{
			if (loadingScreenOverlayHandle != 0L)
			{
				overlay.SetOverlayAlpha(loadingScreenOverlayHandle, alpha);
			}
			if (progressBarOverlayHandle != 0L)
			{
				overlay.SetOverlayAlpha(progressBarOverlayHandle, alpha);
			}
		}
	}

	private IEnumerator LoadLevel()
	{
		if (loadingScreen != null && loadingScreenDistance > 0f)
		{
			SteamVR_Controller.Device hmd = SteamVR_Controller.Input(0);
			while (!hmd.hasTracking)
			{
				yield return null;
			}
			SteamVR_Utils.RigidTransform transform = hmd.transform;
			transform.rot = Quaternion.Euler(0f, transform.rot.eulerAngles.y, 0f);
			transform.pos += transform.rot * new Vector3(0f, 0f, loadingScreenDistance);
			Transform obj = ((loadingScreenTransform != null) ? loadingScreenTransform : ((Component)this).transform);
			obj.position = transform.pos;
			obj.rotation = transform.rot;
		}
		_active = this;
		SteamVR_Events.Loading.Send(arg0: true);
		if (loadingScreenFadeInTime > 0f)
		{
			fadeRate = 1f / loadingScreenFadeInTime;
		}
		else
		{
			alpha = 1f;
		}
		CVROverlay overlay = OpenVR.Overlay;
		if (loadingScreen != null && overlay != null)
		{
			loadingScreenOverlayHandle = GetOverlayHandle("loadingScreen", (loadingScreenTransform != null) ? loadingScreenTransform : ((Component)this).transform, loadingScreenWidthInMeters);
			if (loadingScreenOverlayHandle != 0L)
			{
				Texture_t pTexture = default(Texture_t);
				pTexture.handle = loadingScreen.GetNativeTexturePtr();
				pTexture.eType = SteamVR.instance.textureType;
				pTexture.eColorSpace = EColorSpace.Auto;
				overlay.SetOverlayTexture(loadingScreenOverlayHandle, ref pTexture);
			}
		}
		bool fadedForeground = false;
		SteamVR_Events.LoadingFadeOut.Send(fadeOutTime);
		CVRCompositor compositor2 = OpenVR.Compositor;
		if (compositor2 != null)
		{
			if (front != null)
			{
				SteamVR_Skybox.SetOverride(front, back, left, right, top, bottom);
				compositor2.FadeGrid(fadeOutTime, bFadeIn: true);
				yield return (object)new WaitForSeconds(fadeOutTime);
			}
			else if (backgroundColor != Color.clear)
			{
				if (showGrid)
				{
					compositor2.FadeToColor(0f, backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a, bBackground: true);
					compositor2.FadeGrid(fadeOutTime, bFadeIn: true);
					yield return (object)new WaitForSeconds(fadeOutTime);
				}
				else
				{
					compositor2.FadeToColor(fadeOutTime, backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a, bBackground: false);
					yield return (object)new WaitForSeconds(fadeOutTime + 0.1f);
					compositor2.FadeGrid(0f, bFadeIn: true);
					fadedForeground = true;
				}
			}
		}
		SteamVR_Render.pauseRendering = true;
		while (alpha < 1f)
		{
			yield return null;
		}
		((Component)this).transform.parent = null;
		Object.DontDestroyOnLoad((Object)(object)((Component)this).gameObject);
		if (!string.IsNullOrEmpty(internalProcessPath))
		{
			Debug.Log((object)"Launching external application...");
			CVRApplications applications = OpenVR.Applications;
			if (applications == null)
			{
				Debug.Log((object)"Failed to get OpenVR.Applications interface!");
			}
			else
			{
				string currentDirectory = Directory.GetCurrentDirectory();
				string text = Path.Combine(currentDirectory, internalProcessPath);
				Debug.Log((object)"LaunchingInternalProcess");
				Debug.Log((object)("ExternalAppPath = " + internalProcessPath));
				Debug.Log((object)("FullPath = " + text));
				Debug.Log((object)("ExternalAppArgs = " + internalProcessArgs));
				Debug.Log((object)("WorkingDirectory = " + currentDirectory));
				EVRApplicationError eVRApplicationError = applications.LaunchInternalProcess(text, internalProcessArgs, currentDirectory);
				Debug.Log((object)("LaunchInternalProcessError: " + eVRApplicationError));
				Process.GetCurrentProcess().Kill();
			}
		}
		else
		{
			LoadSceneMode mode = (LoadSceneMode)(loadAdditive ? 1 : 0);
			if (loadAsync)
			{
				Application.backgroundLoadingPriority = (ThreadPriority)0;
				async = SceneManager.LoadSceneAsync(levelName, mode);
				while (!async.isDone)
				{
					yield return null;
				}
			}
			else
			{
				SceneManager.LoadScene(levelName, mode);
			}
		}
		yield return null;
		GC.Collect();
		yield return null;
		Shader.WarmupAllShaders();
		yield return (object)new WaitForSeconds(postLoadSettleTime);
		SteamVR_Render.pauseRendering = false;
		if (loadingScreenFadeOutTime > 0f)
		{
			fadeRate = -1f / loadingScreenFadeOutTime;
		}
		else
		{
			alpha = 0f;
		}
		SteamVR_Events.LoadingFadeIn.Send(fadeInTime);
		compositor2 = OpenVR.Compositor;
		if (compositor2 != null)
		{
			if (fadedForeground)
			{
				compositor2.FadeGrid(0f, bFadeIn: false);
				compositor2.FadeToColor(fadeInTime, 0f, 0f, 0f, 0f, bBackground: false);
				yield return (object)new WaitForSeconds(fadeInTime);
			}
			else
			{
				compositor2.FadeGrid(fadeInTime, bFadeIn: false);
				yield return (object)new WaitForSeconds(fadeInTime);
				if (front != null)
				{
					SteamVR_Skybox.ClearOverride();
				}
			}
		}
		while (alpha > 0f)
		{
			yield return null;
		}
		if (overlay != null)
		{
			if (progressBarOverlayHandle != 0L)
			{
				overlay.HideOverlay(progressBarOverlayHandle);
			}
			if (loadingScreenOverlayHandle != 0L)
			{
				overlay.HideOverlay(loadingScreenOverlayHandle);
			}
		}
		Object.Destroy((Object)(object)((Component)this).gameObject);
		_active = null;
		SteamVR_Events.Loading.Send(arg0: false);
	}

	private ulong GetOverlayHandle(string overlayName, Transform transform, float widthInMeters = 1f)
	{
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		ulong pOverlayHandle = 0uL;
		CVROverlay overlay = OpenVR.Overlay;
		if (overlay == null)
		{
			return pOverlayHandle;
		}
		string pchOverlayKey = SteamVR_Overlay.key + "." + overlayName;
		EVROverlayError eVROverlayError = overlay.FindOverlay(pchOverlayKey, ref pOverlayHandle);
		if (eVROverlayError != 0)
		{
			eVROverlayError = overlay.CreateOverlay(pchOverlayKey, overlayName, ref pOverlayHandle);
		}
		if (eVROverlayError == EVROverlayError.None)
		{
			overlay.ShowOverlay(pOverlayHandle);
			overlay.SetOverlayAlpha(pOverlayHandle, alpha);
			overlay.SetOverlayWidthInMeters(pOverlayHandle, widthInMeters);
			if (SteamVR.instance.textureType == ETextureType.DirectX)
			{
				VRTextureBounds_t pOverlayTextureBounds = default(VRTextureBounds_t);
				pOverlayTextureBounds.uMin = 0f;
				pOverlayTextureBounds.vMin = 1f;
				pOverlayTextureBounds.uMax = 1f;
				pOverlayTextureBounds.vMax = 0f;
				overlay.SetOverlayTextureBounds(pOverlayHandle, ref pOverlayTextureBounds);
			}
			SteamVR_Camera steamVR_Camera = ((loadingScreenDistance == 0f) ? SteamVR_Render.Top() : null);
			if (steamVR_Camera != null && steamVR_Camera.origin != null)
			{
				SteamVR_Utils.RigidTransform rigidTransform = new SteamVR_Utils.RigidTransform(steamVR_Camera.origin, transform);
				rigidTransform.pos.x /= steamVR_Camera.origin.localScale.x;
				rigidTransform.pos.y /= steamVR_Camera.origin.localScale.y;
				rigidTransform.pos.z /= steamVR_Camera.origin.localScale.z;
				HmdMatrix34_t pmatTrackingOriginToOverlayTransform = rigidTransform.ToHmdMatrix34();
				overlay.SetOverlayTransformAbsolute(pOverlayHandle, SteamVR_Render.instance.trackingSpace, ref pmatTrackingOriginToOverlayTransform);
			}
			else
			{
				HmdMatrix34_t pmatTrackingOriginToOverlayTransform2 = new SteamVR_Utils.RigidTransform(transform).ToHmdMatrix34();
				overlay.SetOverlayTransformAbsolute(pOverlayHandle, SteamVR_Render.instance.trackingSpace, ref pmatTrackingOriginToOverlayTransform2);
			}
		}
		return pOverlayHandle;
	}
}

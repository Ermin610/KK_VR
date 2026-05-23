using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using CapturePanorama.Internals;
using UnityEngine;
using VRGIN.Core;

namespace CapturePanorama;

public class CapturePanorama : ProtectedBehaviour
{
	public enum ImageFormat
	{
		PNG,
		JPEG,
		BMP
	}

	public enum AntiAliasing
	{
		_1 = 1,
		_2 = 2,
		_4 = 4,
		_8 = 8
	}

	public string panoramaName;

	public string qualitySetting;

	public KeyCode captureKey = (KeyCode)112;

	public ImageFormat imageFormat;

	public bool captureStereoscopic;

	public float interpupillaryDistance = 0.0635f;

	public int numCirclePoints = 128;

	public int panoramaWidth = 8192;

	public AntiAliasing antiAliasing = AntiAliasing._8;

	public int ssaaFactor = 1;

	public string saveImagePath = "";

	public bool saveCubemap;

	public bool uploadImages;

	public bool useDefaultOrientation;

	public bool useGpuTransform = true;

	public float cpuMillisecondsPerFrame = 8.333333f;

	public bool captureEveryFrame;

	public int frameRate = 30;

	public int maxFramesToRecord;

	public int frameNumberDigits = 6;

	public AudioClip startSound;

	public AudioClip doneSound;

	public AudioClip failSound;

	public bool fadeDuringCapture = true;

	public float fadeTime = 0.25f;

	public Color fadeColor = new Color(0f, 0f, 0f, 1f);

	public Material fadeMaterial;

	public ComputeShader convertPanoramaShader;

	public ComputeShader convertPanoramaStereoShader;

	public ComputeShader textureToBufferShader;

	public bool enableDebugging;

	private string apiUrl = "http://alpha.vrchive.com/api/1/";

	private string apiKey = "0b26e4dca20793a83fd92ad83e3e859e";

	private GameObject[] camGos;

	private Camera cam;

	private ImageEffectCopyCamera copyCameraScript;

	private bool capturingEveryFrame;

	private bool usingGpuTransform;

	private CubemapFace[] faces;

	private int panoramaHeight;

	private int cameraWidth;

	private int cameraHeight;

	private RenderTexture cubemapRenderTexture;

	private Texture2D forceWaitTexture;

	private int convertPanoramaKernelIdx = -1;

	private int convertPanoramaYPositiveKernelIdx = -1;

	private int convertPanoramaYNegativeKernelIdx = -1;

	private int textureToBufferIdx = -1;

	private int renderStereoIdx = -1;

	private int[] convertPanoramaKernelIdxs;

	private byte[] imageFileBytes;

	private string videoBaseName = "";

	private int frameNumber;

	private const int ResultBufferSlices = 8;

	private float hFov = -1f;

	private float vFov = -1f;

	private float hFovAdjustDegrees = -1f;

	private float vFovAdjustDegrees = -1f;

	private float circleRadius = -1f;

	private int threadsX = 32;

	private int threadsY = 32;

	private int numCameras;

	private const int CamerasPerCirclePoint = 4;

	private uint[] cameraPixels;

	private uint[] resultPixels;

	private float tanHalfHFov;

	private float tanHalfVFov;

	private float hFovAdjust;

	private float vFovAdjust;

	private int overlapTextures;

	private bool initializeFailed = true;

	private AudioSource audioSource;

	private const uint BufferSentinelValue = 1419455993u;

	private int lastConfiguredPanoramaWidth;

	private int lastConfiguredNumCirclePoints;

	private int lastConfiguredSsaaFactor;

	private float lastConfiguredInterpupillaryDistance;

	private bool lastConfiguredCaptureStereoscopic;

	private bool lastConfiguredSaveCubemap;

	private bool lastConfiguredUseGpuTransform;

	private AntiAliasing lastConfiguredAntiAliasing = AntiAliasing._1;

	private static CapturePanorama instance;

	internal bool Capturing;

	private static List<Process> resizingProcessList = new List<Process>();

	private static List<string> resizingFilenames = new List<string>();

	private System.Drawing.Imaging.ImageFormat FormatToDrawingFormat(ImageFormat format)
	{
		return format switch
		{
			ImageFormat.PNG => System.Drawing.Imaging.ImageFormat.Png, 
			ImageFormat.JPEG => System.Drawing.Imaging.ImageFormat.Jpeg, 
			ImageFormat.BMP => System.Drawing.Imaging.ImageFormat.Bmp, 
			_ => System.Drawing.Imaging.ImageFormat.Png, 
		};
	}

	private string FormatMimeType(ImageFormat format)
	{
		return format switch
		{
			ImageFormat.PNG => "image/png", 
			ImageFormat.JPEG => "image/jpeg", 
			ImageFormat.BMP => "image/bmp", 
			_ => "", 
		};
	}

	private string FormatToExtension(ImageFormat format)
	{
		return format switch
		{
			ImageFormat.PNG => "png", 
			ImageFormat.JPEG => "jpg", 
			ImageFormat.BMP => "bmp", 
			_ => "", 
		};
	}

	protected override void OnAwake()
	{
		if ((Object)(object)instance == (Object)null)
		{
			instance = this;
		}
		else
		{
			Debug.LogError((object)"More than one CapturePanorama instance detected.");
		}
	}

	protected override void OnStart()
	{
		audioSource = ((Component)this).gameObject.AddComponent<AudioSource>();
		audioSource.spatialBlend = 0f;
		audioSource.Play();
		Reinitialize();
		VRLog.Info("Started panorama");
	}

	private float IpdScaleFunction(float latitudeNormalized)
	{
		return 1.5819767f * Mathf.Exp((0f - latitudeNormalized) * latitudeNormalized) - 0.5819767f;
	}

	public virtual void OnDestroy()
	{
		Cleanup();
	}

	private void Cleanup()
	{
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Expected O, but got Unknown
		faces = null;
		Object.Destroy((Object)(object)copyCameraScript);
		Object.Destroy((Object)(object)cam);
		if (camGos != null)
		{
			for (int num = camGos.Length - 1; num >= 0; num--)
			{
				if ((Object)(object)camGos[num] != (Object)null)
				{
					Object.Destroy((Object)(object)camGos[num]);
				}
			}
		}
		camGos = null;
		numCameras = -1;
		hFov = (vFov = -1f);
		if ((Object)(object)cubemapRenderTexture != (Object)null)
		{
			Object.Destroy((Object)(object)cubemapRenderTexture);
		}
		cubemapRenderTexture = null;
		convertPanoramaKernelIdx = (renderStereoIdx = (textureToBufferIdx = -1));
		convertPanoramaKernelIdxs = null;
		resultPixels = (cameraPixels = null);
		if ((Object)(object)forceWaitTexture != (Object)null)
		{
			Object.Destroy((Object)(object)forceWaitTexture);
		}
		forceWaitTexture = new Texture2D(1, 1);
	}

	private void Reinitialize()
	{
		try
		{
			ReinitializeBody();
		}
		catch (Exception)
		{
			Cleanup();
			throw;
		}
	}

	private void ReinitializeBody()
	{
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		//IL_0358: Unknown result type (might be due to invalid IL or missing references)
		//IL_0362: Expected O, but got Unknown
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f2: Invalid comparison between Unknown and I4
		Log("Settings changed, calling Reinitialize()");
		initializeFailed = true;
		if (!SystemInfo.supportsComputeShaders)
		{
			Debug.LogWarning((object)"CapturePanorama requires compute shaders. Your system does not support them. On PC, compute shaders require DirectX 11, Windows Vista or later, and a GPU capable of Shader Model 5.0.");
			return;
		}
		lastConfiguredCaptureStereoscopic = captureStereoscopic;
		lastConfiguredPanoramaWidth = panoramaWidth;
		lastConfiguredInterpupillaryDistance = interpupillaryDistance;
		lastConfiguredNumCirclePoints = numCirclePoints;
		lastConfiguredSsaaFactor = ssaaFactor;
		lastConfiguredAntiAliasing = antiAliasing;
		lastConfiguredSaveCubemap = saveCubemap;
		lastConfiguredUseGpuTransform = useGpuTransform;
		Cleanup();
		faces = (CubemapFace[])(object)new CubemapFace[6]
		{
			default(CubemapFace),
			(CubemapFace)1,
			(CubemapFace)2,
			(CubemapFace)3,
			(CubemapFace)4,
			(CubemapFace)5
		};
		panoramaHeight = panoramaWidth / 2;
		camGos = (GameObject[])(object)new GameObject[3];
		for (int i = 0; i < 3; i++)
		{
			camGos[i] = new GameObject("PanoramaCaptureCamera" + i);
			((Object)camGos[i]).hideFlags = (HideFlags)61;
			if (i > 0)
			{
				camGos[i].transform.parent = camGos[i - 1].transform;
			}
		}
		camGos[2].AddComponent<Camera>();
		cam = camGos[2].GetComponent<Camera>();
		((Behaviour)cam).enabled = false;
		camGos[2].AddComponent<ImageEffectCopyCamera>();
		copyCameraScript = camGos[2].GetComponent<ImageEffectCopyCamera>();
		((Behaviour)copyCameraScript).enabled = false;
		numCameras = faces.Length;
		hFov = (vFov = 90f);
		if (captureStereoscopic)
		{
			float num = 360f / (float)numCirclePoints;
			float num2 = 0.001f;
			float num3 = 2f * ((float)Math.PI / 2f - Mathf.Acos(IpdScaleFunction(0.5f))) * 360f / ((float)Math.PI * 2f);
			hFov = Mathf.Max(90f + num, num3) + num2;
			vFov = 90f;
			numCameras = 2 + numCirclePoints * 4;
			circleRadius = interpupillaryDistance / 2f;
			hFovAdjustDegrees = hFov / 2f;
			vFovAdjustDegrees = vFov / 2f;
		}
		double num4 = (double)panoramaWidth * 90.0 / 360.0;
		cameraWidth = (int)Math.Ceiling(Math.Tan(hFov * ((float)Math.PI * 2f) / 360f / 2f) * num4 * (double)ssaaFactor);
		cameraHeight = (int)Math.Ceiling(Math.Tan(vFov * ((float)Math.PI * 2f) / 360f / 2f) * num4 * (double)ssaaFactor);
		Log("Number of cameras: " + numCameras);
		Log("Camera dimensions: " + cameraWidth + "x" + cameraHeight);
		usingGpuTransform = useGpuTransform && (Object)(object)convertPanoramaShader != (Object)null;
		cubemapRenderTexture = new RenderTexture(cameraWidth, cameraHeight, 24, (RenderTextureFormat)0);
		cubemapRenderTexture.antiAliasing = (int)antiAliasing;
		cubemapRenderTexture.Create();
		if (usingGpuTransform)
		{
			convertPanoramaKernelIdx = convertPanoramaShader.FindKernel("CubeMapToEquirectangular");
			convertPanoramaYPositiveKernelIdx = convertPanoramaShader.FindKernel("CubeMapToEquirectangularPositiveY");
			convertPanoramaYNegativeKernelIdx = convertPanoramaShader.FindKernel("CubeMapToEquirectangularNegativeY");
			convertPanoramaKernelIdxs = new int[3] { convertPanoramaKernelIdx, convertPanoramaYPositiveKernelIdx, convertPanoramaYNegativeKernelIdx };
			convertPanoramaShader.SetInt("equirectangularWidth", panoramaWidth);
			convertPanoramaShader.SetInt("equirectangularHeight", panoramaHeight);
			convertPanoramaShader.SetInt("ssaaFactor", ssaaFactor);
			convertPanoramaShader.SetInt("cameraWidth", cameraWidth);
			convertPanoramaShader.SetInt("cameraHeight", cameraHeight);
			int num5 = (panoramaHeight + 8 - 1) / 8;
			int num6 = panoramaWidth;
			int num7 = (captureStereoscopic ? (2 * panoramaHeight) : num5);
			resultPixels = new uint[num6 * num7 + 1];
		}
		textureToBufferIdx = textureToBufferShader.FindKernel("TextureToBuffer");
		textureToBufferShader.SetInt("width", cameraWidth);
		textureToBufferShader.SetInt("height", cameraHeight);
		textureToBufferShader.SetFloat("gamma", ((int)QualitySettings.activeColorSpace == 1) ? 0.45454544f : 1f);
		renderStereoIdx = convertPanoramaStereoShader.FindKernel("RenderStereo");
		if ((saveCubemap || !usingGpuTransform) && (cameraPixels == null || cameraPixels.Length != numCameras * cameraWidth * cameraHeight))
		{
			cameraPixels = new uint[numCameras * cameraWidth * cameraHeight + 1];
		}
		tanHalfHFov = Mathf.Tan(hFov * ((float)Math.PI * 2f) / 360f / 2f);
		tanHalfVFov = Mathf.Tan(vFov * ((float)Math.PI * 2f) / 360f / 2f);
		hFovAdjust = hFovAdjustDegrees * ((float)Math.PI * 2f) / 360f;
		vFovAdjust = vFovAdjustDegrees * ((float)Math.PI * 2f) / 360f;
		if (captureStereoscopic && usingGpuTransform)
		{
			convertPanoramaStereoShader.SetFloat("tanHalfHFov", tanHalfHFov);
			convertPanoramaStereoShader.SetFloat("tanHalfVFov", tanHalfVFov);
			convertPanoramaStereoShader.SetFloat("hFovAdjust", hFovAdjust);
			convertPanoramaStereoShader.SetFloat("vFovAdjust", vFovAdjust);
			convertPanoramaStereoShader.SetFloat("interpupillaryDistance", interpupillaryDistance);
			convertPanoramaStereoShader.SetFloat("circleRadius", circleRadius);
			convertPanoramaStereoShader.SetInt("numCirclePoints", numCirclePoints);
			convertPanoramaStereoShader.SetInt("equirectangularWidth", panoramaWidth);
			convertPanoramaStereoShader.SetInt("equirectangularHeight", panoramaHeight);
			convertPanoramaStereoShader.SetInt("cameraWidth", cameraWidth);
			convertPanoramaStereoShader.SetInt("cameraHeight", cameraHeight);
			convertPanoramaStereoShader.SetInt("ssaaFactor", ssaaFactor);
		}
		initializeFailed = false;
	}

	private void Log(string s)
	{
		VRLog.Info(s);
		if (enableDebugging)
		{
			Debug.Log((object)s, (Object)(object)this);
		}
	}

	protected override void OnUpdate()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0185: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Invalid comparison between Unknown and I4
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Invalid comparison between Unknown and I4
		bool keyDown = Input.GetKeyDown(captureKey);
		if (initializeFailed || panoramaWidth < 4 || (captureStereoscopic && numCirclePoints < 8))
		{
			if (keyDown)
			{
				if (panoramaWidth < 4)
				{
					Debug.LogError((object)"Panorama Width must be at least 4. No panorama captured.");
				}
				if (captureStereoscopic && numCirclePoints < 8)
				{
					Debug.LogError((object)"Num Circle Points must be at least 8. No panorama captured.");
				}
				if (initializeFailed)
				{
					Debug.LogError((object)"Initialization of Capture Panorama script failed. Cannot capture content.");
				}
				if ((Object)(object)failSound != (Object)null && (Object)(object)Camera.main != (Object)null)
				{
					audioSource.PlayOneShot(failSound);
				}
			}
			return;
		}
		if (captureStereoscopic != lastConfiguredCaptureStereoscopic || panoramaWidth != lastConfiguredPanoramaWidth || interpupillaryDistance != lastConfiguredInterpupillaryDistance || numCirclePoints != lastConfiguredNumCirclePoints || ssaaFactor != lastConfiguredSsaaFactor || antiAliasing != lastConfiguredAntiAliasing || saveCubemap != lastConfiguredSaveCubemap || useGpuTransform != lastConfiguredUseGpuTransform)
		{
			Reinitialize();
		}
		if (capturingEveryFrame)
		{
			if (((int)captureKey > 0 && keyDown) || (maxFramesToRecord > 0 && frameNumber >= maxFramesToRecord))
			{
				StopCaptureEveryFrame();
				return;
			}
			CaptureScreenshotSync(videoBaseName + "_" + frameNumber.ToString(new string('0', frameNumberDigits)));
			frameNumber++;
		}
		else if ((int)captureKey > 0 && keyDown && !Capturing)
		{
			if (captureEveryFrame)
			{
				StartCaptureEveryFrame();
				return;
			}
			string text = $"{panoramaName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
			Log("Panorama capture key pressed, capturing " + text);
			CaptureScreenshotAsync(text);
		}
	}

	public void StartCaptureEveryFrame()
	{
		Time.captureFramerate = frameRate;
		videoBaseName = $"{panoramaName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
		frameNumber = 0;
		capturingEveryFrame = true;
	}

	public void StopCaptureEveryFrame()
	{
		Time.captureFramerate = 0;
		capturingEveryFrame = false;
	}

	public void CaptureScreenshotSync(string filenameBase)
	{
		IEnumerator enumerator = CaptureScreenshotAsyncHelper(filenameBase, async: false);
		while (enumerator.MoveNext())
		{
		}
	}

	public void CaptureScreenshotAsync(string filenameBase)
	{
		((MonoBehaviour)this).StartCoroutine(CaptureScreenshotAsyncHelper(filenameBase, async: true));
	}

	private void SetFadersEnabled(IEnumerable<ScreenFadeControl> fadeControls, bool value)
	{
		foreach (ScreenFadeControl fadeControl in fadeControls)
		{
			((Behaviour)fadeControl).enabled = value;
		}
	}

	public IEnumerator FadeOut(IEnumerable<ScreenFadeControl> fadeControls)
	{
		Log("Doing fade out");
		float elapsedTime = 0f;
		Color color = fadeColor;
		color.a = 0f;
		fadeMaterial.color = color;
		SetFadersEnabled(fadeControls, value: true);
		while (elapsedTime < fadeTime)
		{
			yield return (object)new WaitForEndOfFrame();
			elapsedTime += Time.deltaTime;
			color.a = Mathf.Clamp01(elapsedTime / fadeTime);
			fadeMaterial.color = color;
		}
	}

	public IEnumerator FadeIn(IEnumerable<ScreenFadeControl> fadeControls)
	{
		Log("Fading back in");
		float elapsedTime = 0f;
		Color val2 = (fadeMaterial.color = fadeColor);
		Color color = val2;
		while (elapsedTime < fadeTime)
		{
			yield return (object)new WaitForEndOfFrame();
			elapsedTime += Time.deltaTime;
			color.a = 1f - Mathf.Clamp01(elapsedTime / fadeTime);
			fadeMaterial.color = color;
		}
		SetFadersEnabled(fadeControls, value: false);
	}

	public IEnumerator CaptureScreenshotAsyncHelper(string filenameBase, bool async)
	{
		if (async)
		{
			while (Capturing)
			{
				yield return null;
			}
		}
		Capturing = true;
		if (!OnCaptureStart())
		{
			audioSource.PlayOneShot(failSound);
			Capturing = false;
			yield break;
		}
		Camera[] cameras = GetCaptureCameras();
		Array.Sort(cameras, (Camera x, Camera y) => x.depth.CompareTo(y.depth));
		if (cameras.Length == 0)
		{
			Debug.LogWarning((object)"No cameras found to capture");
			audioSource.PlayOneShot(failSound);
			Capturing = false;
			yield break;
		}
		Camera[] array;
		if (antiAliasing != AntiAliasing._1)
		{
			array = cameras;
			foreach (Camera val in array)
			{
				if ((int)val.actualRenderingPath == 2 || (int)val.actualRenderingPath == 3)
				{
					Debug.LogWarning((object)"CapturePanorama: Setting Anti Aliasing=1 because at least one camera in deferred mode. Use SSAA setting or Antialiasing image effect if needed.");
					antiAliasing = AntiAliasing._1;
					Reinitialize();
					break;
				}
			}
		}
		Log("Starting panorama capture");
		if (!captureEveryFrame && (Object)(object)startSound != (Object)null && (Object)(object)Camera.main != (Object)null)
		{
			audioSource.PlayOneShot(startSound);
		}
		List<ScreenFadeControl> fadeControls = new List<ScreenFadeControl>();
		array = Camera.allCameras;
		foreach (Camera val2 in array)
		{
			if (((Behaviour)val2).isActiveAndEnabled && (Object)(object)val2.targetTexture == (Object)null)
			{
				ScreenFadeControl screenFadeControl = ((Component)val2).gameObject.AddComponent<ScreenFadeControl>();
				screenFadeControl.fadeMaterial = fadeMaterial;
				fadeControls.Add(screenFadeControl);
			}
		}
		SetFadersEnabled(fadeControls, value: false);
		if (fadeDuringCapture && async)
		{
			yield return ((MonoBehaviour)this).StartCoroutine(FadeOut(fadeControls));
		}
		for (int j = 0; j < 2; j++)
		{
			yield return (object)new WaitForEndOfFrame();
		}
		ComputeBuffer convertPanoramaResultBuffer = null;
		ComputeBuffer forceWaitResultConvertPanoramaStereoBuffer = null;
		if (usingGpuTransform)
		{
			if (captureStereoscopic)
			{
				convertPanoramaResultBuffer = new ComputeBuffer(panoramaWidth * panoramaHeight * 2 + 1, 4);
				convertPanoramaStereoShader.SetBuffer(renderStereoIdx, "result", convertPanoramaResultBuffer);
				forceWaitResultConvertPanoramaStereoBuffer = new ComputeBuffer(1, 4);
				convertPanoramaStereoShader.SetBuffer(renderStereoIdx, "forceWaitResultBuffer", forceWaitResultConvertPanoramaStereoBuffer);
			}
			else
			{
				int num = (panoramaHeight + 8 - 1) / 8;
				convertPanoramaResultBuffer = new ComputeBuffer(panoramaWidth * num + 1, 4);
				int[] array2 = convertPanoramaKernelIdxs;
				foreach (int num2 in array2)
				{
					convertPanoramaShader.SetBuffer(num2, "result", convertPanoramaResultBuffer);
				}
			}
		}
		int num3 = numCameras;
		overlapTextures = 0;
		int num4 = 0;
		if (captureStereoscopic && usingGpuTransform)
		{
			overlapTextures = ((ssaaFactor == 1) ? 1 : 2);
			num4 = 1 + overlapTextures;
			num3 = Math.Min(numCameras, 2 + 2 * num4);
		}
		ComputeBuffer cameraPixelsBuffer = new ComputeBuffer(num3 * cameraWidth * cameraHeight + 1, 4);
		textureToBufferShader.SetBuffer(textureToBufferIdx, "result", cameraPixelsBuffer);
		textureToBufferShader.SetInt("sentinelIdx", cameraPixelsBuffer.count - 1);
		if (usingGpuTransform && !captureStereoscopic)
		{
			convertPanoramaShader.SetInt("cameraPixelsSentinelIdx", cameraPixelsBuffer.count - 1);
			convertPanoramaShader.SetInt("sentinelIdx", convertPanoramaResultBuffer.count - 1);
			int[] array2 = convertPanoramaKernelIdxs;
			foreach (int num5 in array2)
			{
				convertPanoramaShader.SetBuffer(num5, "cameraPixels", cameraPixelsBuffer);
			}
		}
		if (usingGpuTransform && captureStereoscopic)
		{
			convertPanoramaStereoShader.SetInt("cameraPixelsSentinelIdx", cameraPixelsBuffer.count - 1);
			convertPanoramaStereoShader.SetBuffer(renderStereoIdx, "cameraPixels", cameraPixelsBuffer);
		}
		ComputeBuffer forceWaitResultTextureToBufferBuffer = new ComputeBuffer(1, 4);
		textureToBufferShader.SetBuffer(textureToBufferIdx, "forceWaitResultBuffer", forceWaitResultTextureToBufferBuffer);
		float startTime = Time.realtimeSinceStartup;
		Quaternion identity = Quaternion.identity;
		Log("Rendering camera views");
		array = cameras;
		foreach (Camera val3 in array)
		{
			Log("Camera name: " + ((Object)((Component)val3).gameObject).name);
		}
		Dictionary<Camera, List<ImageEffectCopyCamera.InstanceMethodPair>> dictionary = new Dictionary<Camera, List<ImageEffectCopyCamera.InstanceMethodPair>>();
		array = cameras;
		foreach (Camera val4 in array)
		{
			dictionary[val4] = ImageEffectCopyCamera.GenerateMethodList(val4);
		}
		string suffix = "." + FormatToExtension(imageFormat);
		string filePath = "";
		string imagePath = saveImagePath;
		if (imagePath == null || imagePath == "")
		{
			imagePath = Application.dataPath + "/..";
		}
		convertPanoramaStereoShader.SetInt("circlePointCircularBufferSize", num4);
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		int num9 = (usingGpuTransform ? (numCameras + overlapTextures * 4) : numCameras);
		int num10 = (num9 - 2) / 2 + 2;
		int num11 = 0;
		int num12 = 0;
		Log("Changing quality level");
		int qualityLevel = QualitySettings.GetQualityLevel();
		bool flag = false;
		string[] names = QualitySettings.names;
		if (qualitySetting != names[qualityLevel])
		{
			for (int l = 0; l < names.Length; l++)
			{
				if (names[l] == qualitySetting)
				{
					QualitySettings.SetQualityLevel(l, false);
					flag = true;
				}
			}
			if (qualitySetting != "" && !flag)
			{
				Debug.LogError((object)"Quality setting specified for CapturePanorama is invalid, ignoring.", (Object)(object)this);
			}
		}
		BeforeRenderPanorama();
		RenderTexture.active = null;
		for (int m = 0; m < num9; m++)
		{
			if (captureStereoscopic)
			{
				if (m < 2)
				{
					camGos[1].transform.localPosition = Vector3.zero;
					camGos[1].transform.localRotation = Quaternion.Euler((m == 0) ? 90f : (-90f), 0f, 0f);
				}
				else
				{
					int num13;
					int num14;
					if (m < num10)
					{
						num13 = m - 2;
						num14 = 0;
					}
					else
					{
						num13 = m - num10;
						num14 = 2;
					}
					int num15 = num13 / 2 % numCirclePoints;
					int num16 = num13 % 2 + num14;
					float num17 = 360f * (float)num15 / (float)numCirclePoints;
					camGos[1].transform.localPosition = Quaternion.Euler(0f, num17, 0f) * Vector3.forward * circleRadius;
					if (num16 < 2)
					{
						camGos[1].transform.localRotation = Quaternion.Euler(0f, num17 + ((num16 == 0) ? (0f - hFovAdjustDegrees) : hFovAdjustDegrees), 0f);
					}
					else
					{
						camGos[1].transform.localRotation = Quaternion.Euler((num16 == 2) ? (0f - vFovAdjustDegrees) : vFovAdjustDegrees, num17, 0f);
					}
					if (num16 == 1 || num16 == 3)
					{
						num11++;
					}
				}
			}
			else
			{
				CubemapFace val5 = (CubemapFace)m;
				switch ((int)val5)
				{
				case 0:
					camGos[1].transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
					break;
				case 1:
					camGos[1].transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
					break;
				case 2:
					camGos[1].transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
					break;
				case 3:
					camGos[1].transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
					break;
				case 4:
					camGos[1].transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
					break;
				case 5:
					camGos[1].transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
					break;
				}
			}
			array = cameras;
			foreach (Camera val6 in array)
			{
				camGos[2].transform.parent = null;
				cam.CopyFrom(val6);
				camGos[0].transform.localPosition = ((Component)cam).transform.localPosition;
				camGos[0].transform.localRotation = ((Component)cam).transform.localRotation;
				camGos[2].transform.parent = camGos[1].transform;
				((Component)cam).transform.localPosition = Vector3.zero;
				((Component)cam).transform.localRotation = Quaternion.identity;
				((Behaviour)copyCameraScript).enabled = dictionary[val6].Count > 0;
				copyCameraScript.onRenderImageMethods = dictionary[val6];
				cam.fieldOfView = vFov;
				Transform transform = camGos[0].transform;
				transform.rotation *= Quaternion.Inverse(identity);
				if (useDefaultOrientation)
				{
					camGos[0].transform.rotation = Quaternion.identity;
				}
				cam.targetTexture = cubemapRenderTexture;
				cam.ResetAspect();
				Vector3 position = ((Component)val6).transform.position;
				Quaternion rotation = ((Component)val6).transform.rotation;
				float fieldOfView = val6.fieldOfView;
				RenderTexture targetTexture = val6.targetTexture;
				((Component)val6).transform.position = ((Component)cam).transform.position;
				((Component)val6).transform.rotation = ((Component)cam).transform.rotation;
				val6.fieldOfView = cam.fieldOfView;
				cam.Render();
				((Component)val6).transform.position = position;
				((Component)val6).transform.rotation = rotation;
				val6.fieldOfView = fieldOfView;
				val6.targetTexture = targetTexture;
			}
			RenderTexture.active = cubemapRenderTexture;
			forceWaitTexture.ReadPixels(new Rect((float)(cameraWidth - 1), (float)(cameraHeight - 1), 1f, 1f), 0, 0);
			int num18 = 1000000 + m;
			textureToBufferShader.SetInt("forceWaitValue", num18);
			textureToBufferShader.SetTexture(textureToBufferIdx, "source", (Texture)(object)cubemapRenderTexture);
			textureToBufferShader.SetInt("startIdx", num8 * cameraWidth * cameraHeight);
			textureToBufferShader.Dispatch(textureToBufferIdx, (cameraWidth + threadsX - 1) / threadsX, (cameraHeight + threadsY - 1) / threadsY, 1);
			uint[] array3 = new uint[1];
			forceWaitResultTextureToBufferBuffer.GetData((Array)array3);
			if (array3[0] != num18)
			{
				Debug.LogError((object)("TextureToBufferShader: Unexpected forceWaitResult value " + array3[0] + ", should be " + num18));
			}
			if (saveCubemap && (m < 2 || (m >= 2 && m < 2 + numCirclePoints * 2) || (m >= num10 && m < num10 + numCirclePoints * 2)))
			{
				cameraPixelsBuffer.GetData((Array)cameraPixels);
				if (cameraPixels[cameraPixelsBuffer.count - 1] != 1419455993)
				{
					ReportOutOfGraphicsMemory();
				}
				SaveCubemapImage(cameraPixels, filenameBase, suffix, imagePath, num12, num8);
				num12++;
			}
			num8++;
			if (num8 >= num3)
			{
				num8 = 2;
			}
			if (captureStereoscopic && usingGpuTransform && (m - 2 + 1) % 2 == 0 && (num11 - num7 >= num4 || m + 1 == 2 + (num9 - 2) / 2 || m + 1 == num9))
			{
				num18 = 2000000 + m;
				convertPanoramaStereoShader.SetInt("forceWaitValue", num18);
				convertPanoramaStereoShader.SetInt("leftRightPass", (m < num10) ? 1 : 0);
				convertPanoramaStereoShader.SetInt("circlePointStart", num7);
				convertPanoramaStereoShader.SetInt("circlePointEnd", (num3 < numCameras) ? num11 : (num11 + 1));
				convertPanoramaStereoShader.SetInt("circlePointCircularBufferStart", num6);
				convertPanoramaStereoShader.Dispatch(renderStereoIdx, (panoramaWidth + threadsX - 1) / threadsX, (panoramaHeight + threadsY - 1) / threadsY, 2);
				forceWaitResultConvertPanoramaStereoBuffer.GetData((Array)array3);
				if (array3[0] != num18)
				{
					Debug.LogError((object)("ConvertPanoramaStereoShader: Unexpected forceWaitResult value " + array3[0] + ", should be " + num18));
				}
				if (m + 1 == num10)
				{
					num6 = (num6 + num4) % num4;
					num7 = 0;
					num11 = 0;
				}
				else
				{
					num7 = num11 - overlapTextures;
					num6 = (num6 + num4 - overlapTextures) % num4;
				}
			}
			RenderTexture.active = null;
		}
		AfterRenderPanorama();
		Log("Resetting quality level");
		if (flag)
		{
			QualitySettings.SetQualityLevel(qualityLevel, false);
		}
		if (saveCubemap || !usingGpuTransform)
		{
			cameraPixelsBuffer.GetData((Array)cameraPixels);
			if (cameraPixels[cameraPixelsBuffer.count - 1] != 1419455993)
			{
				ReportOutOfGraphicsMemory();
			}
		}
		RenderTexture.active = null;
		if (saveCubemap && (!captureStereoscopic || !usingGpuTransform))
		{
			for (int n = 0; n < numCameras; n++)
			{
				int bufferIdx = n;
				SaveCubemapImage(cameraPixels, filenameBase, suffix, imagePath, n, bufferIdx);
			}
		}
		for (int i = 0; i < 2; i++)
		{
			yield return (object)new WaitForEndOfFrame();
		}
		if (async && !usingGpuTransform && fadeDuringCapture)
		{
			yield return ((MonoBehaviour)this).StartCoroutine(FadeIn(fadeControls));
		}
		filePath = imagePath + "/" + filenameBase + suffix;
		Bitmap bitmap = new Bitmap(panoramaWidth, panoramaHeight * ((!captureStereoscopic) ? 1 : 2), PixelFormat.Format32bppArgb);
		BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
		IntPtr ptr = bmpData.Scan0;
		byte[] pixelValues = new byte[Math.Abs(bmpData.Stride) * bitmap.Height];
		if (async)
		{
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangular(cameraPixelsBuffer, cameraPixels, convertPanoramaResultBuffer, cameraWidth, cameraHeight, pixelValues, bmpData.Stride, panoramaWidth, panoramaHeight, ssaaFactor, async));
		}
		else
		{
			IEnumerator enumerator = CubemapToEquirectangular(cameraPixelsBuffer, cameraPixels, convertPanoramaResultBuffer, cameraWidth, cameraHeight, pixelValues, bmpData.Stride, panoramaWidth, panoramaHeight, ssaaFactor, async);
			while (enumerator.MoveNext())
			{
			}
		}
		bool producedImageSuccess = pixelValues[3] == byte.MaxValue;
		yield return null;
		Marshal.Copy(pixelValues, 0, ptr, pixelValues.Length);
		bitmap.UnlockBits(bmpData);
		yield return null;
		Log("Time to take panorama screenshot: " + (Time.realtimeSinceStartup - startTime) + " sec");
		if (producedImageSuccess)
		{
			Thread thread = new Thread((ThreadStart)delegate
			{
				Log("Saving equirectangular image");
				bitmap.Save(filePath, FormatToDrawingFormat(imageFormat));
			});
			thread.Start();
			while (thread.ThreadState == System.Threading.ThreadState.Running)
			{
				if (async)
				{
					yield return null;
				}
				else
				{
					Thread.Sleep(0);
				}
			}
		}
		bitmap.Dispose();
		ComputeBuffer[] array4 = (ComputeBuffer[])(object)new ComputeBuffer[4] { convertPanoramaResultBuffer, cameraPixelsBuffer, forceWaitResultConvertPanoramaStereoBuffer, forceWaitResultTextureToBufferBuffer };
		foreach (ComputeBuffer val7 in array4)
		{
			if (val7 != null)
			{
				val7.Release();
			}
		}
		if (async && usingGpuTransform && fadeDuringCapture)
		{
			yield return ((MonoBehaviour)this).StartCoroutine(FadeIn(fadeControls));
		}
		foreach (ScreenFadeControl item in fadeControls)
		{
			Object.Destroy((Object)(object)item);
		}
		fadeControls.Clear();
		if (producedImageSuccess && uploadImages && !captureEveryFrame)
		{
			Log("Uploading image");
			imageFileBytes = File.ReadAllBytes(filePath);
			string mimeType = FormatMimeType(imageFormat);
			if (async)
			{
				yield return ((MonoBehaviour)this).StartCoroutine(UploadImage(imageFileBytes, filenameBase + suffix, mimeType, async));
				yield break;
			}
			IEnumerator enumerator3 = UploadImage(imageFileBytes, filenameBase + suffix, mimeType, async);
			while (enumerator3.MoveNext())
			{
			}
			yield break;
		}
		if (!producedImageSuccess)
		{
			if ((Object)(object)failSound != (Object)null && (Object)(object)Camera.main != (Object)null)
			{
				audioSource.PlayOneShot(failSound);
			}
		}
		else if (!captureEveryFrame && (Object)(object)doneSound != (Object)null && (Object)(object)Camera.main != (Object)null)
		{
			audioSource.PlayOneShot(doneSound);
		}
		Capturing = false;
	}

	public virtual bool OnCaptureStart()
	{
		return true;
	}

	public virtual Camera[] GetCaptureCameras()
	{
		Camera[] allCameras = Camera.allCameras;
		List<Camera> list = new List<Camera>();
		Camera[] array = allCameras;
		foreach (Camera val in array)
		{
			VRLog.Info("Camera found: " + ((Object)val).name);
			list.Add(val);
		}
		return list.ToArray();
	}

	public virtual void BeforeRenderPanorama()
	{
	}

	public virtual void AfterRenderPanorama()
	{
	}

	private static void ReportOutOfGraphicsMemory()
	{
		throw new OutOfMemoryException("Exhausted graphics memory while capturing panorama. Lower Panorama Width, increase Num Circle Points for stereoscopic images, disable Anti Aliasing, or disable Stereoscopic Capture.");
	}

	private void SaveCubemapImage(uint[] cameraPixels, string filenameBase, string suffix, string imagePath, int i, int bufferIdx)
	{
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		Bitmap bitmap = new Bitmap(cameraWidth, cameraHeight, PixelFormat.Format32bppArgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
		IntPtr scan = bitmapData.Scan0;
		byte[] array = new byte[Math.Abs(bitmapData.Stride) * bitmap.Height];
		int stride = bitmapData.Stride;
		int height = bitmapData.Height;
		int num = bufferIdx * cameraWidth * cameraHeight;
		for (int j = 0; j < cameraHeight; j++)
		{
			int num2 = stride * (height - 1 - j);
			for (int k = 0; k < cameraWidth; k++)
			{
				uint num3 = cameraPixels[num];
				array[num2] = (byte)(num3 & 0xFFu);
				array[num2 + 1] = (byte)((num3 >> 8) & 0xFFu);
				array[num2 + 2] = (byte)(num3 >> 16);
				array[num2 + 3] = byte.MaxValue;
				num2 += 4;
				num++;
			}
		}
		Marshal.Copy(array, 0, scan, array.Length);
		bitmap.UnlockBits(bitmapData);
		string text;
		if (captureStereoscopic)
		{
			text = i.ToString();
			Log("Saving lightfield camera image number " + text);
		}
		else
		{
			CubemapFace val = (CubemapFace)i;
			text = ((object)(CubemapFace)(ref val)).ToString();
			Log("Saving cubemap image " + text);
		}
		string filename = imagePath + "/" + filenameBase + "_" + text + suffix;
		bitmap.Save(filename, FormatToDrawingFormat(imageFormat));
		bitmap.Dispose();
	}

	private Color32 GetCameraPixelBilinear(uint[] cameraPixels, int cameraNum, float u, float v)
	{
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		u *= (float)cameraWidth;
		v *= (float)cameraHeight;
		int num = (int)Math.Floor(u);
		int num2 = Math.Min(cameraWidth - 1, num + 1);
		int num3 = (int)Math.Floor(v);
		int num4 = Math.Min(cameraHeight - 1, num3 + 1);
		float num5 = u - (float)num;
		float num6 = v - (float)num3;
		int num7 = cameraNum * cameraWidth * cameraHeight;
		int num8 = num7 + num3 * cameraWidth;
		int num9 = num7 + num4 * cameraWidth;
		uint num10 = cameraPixels[num8 + num];
		uint num11 = cameraPixels[num8 + num2];
		uint num12 = cameraPixels[num9 + num];
		uint num13 = cameraPixels[num9 + num2];
		float num14 = Mathf.Lerp(Mathf.Lerp((float)(num10 >> 16), (float)(num12 >> 16), num6), Mathf.Lerp((float)(num11 >> 16), (float)(num13 >> 16), num6), num5);
		float num15 = Mathf.Lerp(Mathf.Lerp((float)((num10 >> 8) & 0xFFu), (float)((num12 >> 8) & 0xFFu), num6), Mathf.Lerp((float)((num11 >> 8) & 0xFFu), (float)((num13 >> 8) & 0xFFu), num6), num5);
		float num16 = Mathf.Lerp(Mathf.Lerp((float)(num10 & 0xFFu), (float)(num12 & 0xFFu), num6), Mathf.Lerp((float)(num11 & 0xFFu), (float)(num13 & 0xFFu), num6), num5);
		return Color32.op_Implicit(new Color(num14 / 255f, num15 / 255f, num16 / 255f, 1f));
	}

	internal void ClearProcessQueue()
	{
		while (resizingProcessList.Count > 0)
		{
			resizingProcessList[0].WaitForExit();
			File.Delete(resizingFilenames[0]);
			resizingProcessList.RemoveAt(0);
			resizingFilenames.RemoveAt(0);
		}
	}

	private IEnumerator UploadImage(byte[] imageFileBytes, string filename, string mimeType, bool async)
	{
		float startTime = Time.realtimeSinceStartup;
		WWWForm val = new WWWForm();
		val.AddField("key", apiKey);
		val.AddField("action", "upload");
		val.AddBinaryData("source", imageFileBytes, filename, mimeType);
		WWW w = new WWW(apiUrl + "upload", val);
		yield return w;
		if (!string.IsNullOrEmpty(w.error))
		{
			Debug.LogError((object)("Panorama upload failed: " + w.error), (Object)(object)this);
			if ((Object)(object)failSound != (Object)null && (Object)(object)Camera.main != (Object)null)
			{
				audioSource.PlayOneShot(failSound);
			}
		}
		else
		{
			Log("Time to upload panorama screenshot: " + (Time.realtimeSinceStartup - startTime) + " sec");
			if (!captureEveryFrame && (Object)(object)doneSound != (Object)null && (Object)(object)Camera.main != (Object)null)
			{
				audioSource.PlayOneShot(doneSound);
			}
		}
		Capturing = false;
	}

	private IEnumerator CubemapToEquirectangular(ComputeBuffer cameraPixelsBuffer, uint[] cameraPixels, ComputeBuffer convertPanoramaResultBuffer, int cameraWidth, int cameraHeight, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, bool async)
	{
		if (captureStereoscopic && usingGpuTransform)
		{
			convertPanoramaResultBuffer.GetData((Array)resultPixels);
			if (resultPixels[convertPanoramaResultBuffer.count - 1] != 1419455993)
			{
				ReportOutOfGraphicsMemory();
			}
			writeOutputPixels(pixelValues, stride, panoramaWidth, panoramaHeight * 2, panoramaHeight * 2, 0);
		}
		else if (captureStereoscopic && !usingGpuTransform)
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			float processingTimePerFrame = cpuMillisecondsPerFrame / 1000f;
			Color val = default(Color);
			Vector3 val2 = default(Vector3);
			Vector3 val3 = default(Vector3);
			Vector3 val6 = default(Vector3);
			for (int y = 0; y < panoramaHeight; y++)
			{
				for (int x = 0; x < panoramaWidth; x++)
				{
					float num = (float)x / (float)panoramaWidth;
					float num2 = ((float)y / (float)panoramaHeight - 0.5f) * (float)Math.PI;
					float num3 = Mathf.Sin(num2);
					float num4 = Mathf.Cos(num2);
					float num5 = (num * 2f - 1f) * (float)Math.PI;
					float num6 = Mathf.Sin(num5);
					float num7 = Mathf.Cos(num5);
					float latitudeNormalized = num2 / ((float)Math.PI / 2f);
					float num8 = IpdScaleFunction(latitudeNormalized);
					float num9 = num8 * interpupillaryDistance / 2f;
					float num10 = 1f - num8 * 5f;
					((Color)(ref val))._002Ector(0f, 0f, 0f, 0f);
					if (num10 > 0f)
					{
						((Vector3)(ref val2))._002Ector(num4 * num6, num3, num4 * num7);
						float num11 = 1f / val2.y;
						float num12 = val2.x * num11;
						float num13 = val2.z * num11;
						if (num12 * num12 <= 1f && num13 * num13 <= 1f)
						{
							int cameraNum;
							if (val2.y > 0f)
							{
								cameraNum = 0;
							}
							else
							{
								num12 = 0f - num12;
								cameraNum = 1;
							}
							num12 = (num12 + 1f) * 0.5f;
							num13 = (num13 + 1f) * 0.5f;
							val = Color32.op_Implicit(GetCameraPixelBilinear(cameraPixels, cameraNum, num12, num13));
						}
					}
					for (int i = 0; i < 2; i++)
					{
						((Vector3)(ref val3))._002Ector(num6, 0f, num7);
						float num14 = (float)Math.PI / 2f - Mathf.Acos(num9 / circleRadius);
						if (i == 0)
						{
							num14 = 0f - num14;
						}
						float num15 = num5 + num14;
						if (num15 < 0f)
						{
							num15 += (float)Math.PI * 2f;
						}
						if (num15 >= (float)Math.PI * 2f)
						{
							num15 -= (float)Math.PI * 2f;
						}
						float num16 = num15 / ((float)Math.PI * 2f) * (float)numCirclePoints;
						int num17 = (int)Mathf.Floor(num16) % numCirclePoints;
						Color val4 = default(Color);
						Color val5 = default(Color);
						for (int j = 0; j < 2; j++)
						{
							int num18 = ((j == 0) ? num17 : ((num17 + 1) % numCirclePoints));
							float num19 = (float)Math.PI * 2f * (float)num18 / (float)numCirclePoints;
							float num20 = Mathf.Sin(num19);
							float num21 = Mathf.Cos(num19);
							float num22 = Mathf.Sign(val3.x * num21 - val3.z * num20) * Mathf.Acos(val3.z * num21 + val3.x * num20);
							float num23 = Mathf.Cos(num22);
							float num24 = Mathf.Sin(num22);
							int cameraNum = 2 + num18 * 2 + ((num22 >= 0f) ? 1 : 0);
							float num25 = ((num22 >= 0f) ? (0f - hFovAdjust) : hFovAdjust);
							float num26 = num22 + num25;
							((Vector3)(ref val6))._002Ector(num4 * Mathf.Sin(num26), num3, num4 * Mathf.Cos(num26));
							float num12 = val6.x / val6.z / tanHalfHFov;
							float num13 = (0f - val6.y) / val6.z / tanHalfVFov;
							if (!(val6.z > 0f) || !(num12 * num12 <= 1f) || !(num13 * num13 <= 0.9f))
							{
								cameraNum = 2 + numCirclePoints * 2 + num18 * 2 + ((num2 >= 0f) ? 1 : 0);
								float num27 = ((num2 >= 0f) ? vFovAdjust : (0f - vFovAdjust));
								float num28 = Mathf.Cos(num27);
								float num29 = Mathf.Sin(num27);
								((Vector3)(ref val6))._002Ector(num4 * num24, num28 * num3 - num4 * num23 * num29, num29 * num3 + num4 * num23 * num28);
								num12 = val6.x / val6.z / tanHalfHFov;
								num13 = (0f - val6.y) / val6.z / tanHalfVFov;
							}
							num12 = (num12 + 1f) * 0.5f;
							num13 = (num13 + 1f) * 0.5f;
							Color val7 = Color32.op_Implicit(GetCameraPixelBilinear(cameraPixels, cameraNum, num12, num13));
							if (j == 0)
							{
								val4 = val7;
							}
							else
							{
								val5 = val7;
							}
						}
						Color32 val8 = Color32.op_Implicit(Color.Lerp(val4, val5, num16 - Mathf.Floor(num16)));
						if (val.a > 0f && num10 > 0f)
						{
							val8 = Color32.op_Implicit(Color.Lerp(Color32.op_Implicit(val8), val, num10));
						}
						int num30 = stride * (y + panoramaHeight * i) + x * 4;
						pixelValues[num30] = val8.b;
						pixelValues[num30 + 1] = val8.g;
						pixelValues[num30 + 2] = val8.r;
						pixelValues[num30 + 3] = byte.MaxValue;
					}
					if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - realtimeSinceStartup > processingTimePerFrame)
					{
						yield return null;
						realtimeSinceStartup = Time.realtimeSinceStartup;
					}
				}
			}
		}
		else if (!captureStereoscopic && usingGpuTransform)
		{
			int num31 = (panoramaHeight + 8 - 1) / 8;
			Log("Invoking GPU shader for equirectangular reprojection");
			int num32 = (int)Mathf.Floor((float)panoramaHeight * 0.25f);
			int num33 = (int)Mathf.Ceil((float)panoramaHeight * 0.75f);
			for (int k = 0; k < 8; k++)
			{
				int num34 = k * num31;
				int num35 = Math.Min(num34 + num31, panoramaHeight);
				convertPanoramaShader.SetInt("startY", k * num31);
				convertPanoramaShader.SetInt("sliceHeight", num35 - num34);
				if (num35 <= num32)
				{
					convertPanoramaShader.Dispatch(convertPanoramaYNegativeKernelIdx, (panoramaWidth + threadsX - 1) / threadsX, (num31 + threadsY - 1) / threadsY, 1);
				}
				else if (num34 >= num33)
				{
					convertPanoramaShader.Dispatch(convertPanoramaYPositiveKernelIdx, (panoramaWidth + threadsX - 1) / threadsX, (num31 + threadsY - 1) / threadsY, 1);
				}
				else
				{
					convertPanoramaShader.Dispatch(convertPanoramaKernelIdx, (panoramaWidth + threadsX - 1) / threadsX, (panoramaHeight + threadsY - 1) / threadsY, 1);
				}
				convertPanoramaResultBuffer.GetData((Array)resultPixels);
				if (resultPixels[convertPanoramaResultBuffer.count - 1] != 1419455993)
				{
					ReportOutOfGraphicsMemory();
				}
				writeOutputPixels(pixelValues, stride, panoramaWidth, num31, panoramaHeight, num34);
			}
		}
		else if (async)
		{
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpu(cameraPixels, cameraWidth, cameraHeight, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, async));
		}
		else
		{
			IEnumerator enumerator = CubemapToEquirectangularCpu(cameraPixels, cameraWidth, cameraHeight, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, async);
			while (enumerator.MoveNext())
			{
			}
		}
	}

	private void writeOutputPixels(byte[] pixelValues, int stride, int bitmapWidth, int inHeight, int outHeight, int yStart)
	{
		int num = 0;
		for (int i = yStart; i < yStart + inHeight && i < outHeight; i++)
		{
			int num2 = stride * i;
			for (int j = 0; j < bitmapWidth; j++)
			{
				uint num3 = resultPixels[num];
				pixelValues[num2] = (byte)(num3 & 0xFFu);
				pixelValues[num2 + 1] = (byte)((num3 >> 8) & 0xFFu);
				pixelValues[num2 + 2] = (byte)((num3 >> 16) & 0xFFu);
				pixelValues[num2 + 3] = byte.MaxValue;
				num2 += 4;
				num++;
			}
		}
	}

	private IEnumerator CubemapToEquirectangularCpu(uint[] cameraPixels, int cameraWidth, int cameraHeight, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, bool async)
	{
		Log("Converting to equirectangular");
		yield return null;
		float startTime = Time.realtimeSinceStartup;
		float processingTimePerFrame = cpuMillisecondsPerFrame / 1000f;
		float maxWidth = 1f - 1f / (float)cameraWidth;
		float maxHeight = 1f - 1f / (float)cameraHeight;
		int numPixelsAveraged = ssaaFactor * ssaaFactor;
		int endYPositive = (int)Mathf.Floor((float)panoramaHeight * 0.25f);
		int startYNegative = (int)Mathf.Ceil((float)panoramaHeight * 0.75f);
		int endTopMixedRegion = (int)Mathf.Ceil((float)panoramaHeight * 0.30408698f);
		int startBottomMixedRegion = (int)Mathf.Floor((float)panoramaHeight * 0.695913f);
		int startXNegative = (int)Mathf.Ceil((float)panoramaWidth * 1f / 8f);
		int endXNegative = (int)Mathf.Floor((float)panoramaWidth * 3f / 8f);
		int startZPositive = (int)Mathf.Ceil((float)panoramaWidth * 3f / 8f);
		int endZPositive = (int)Mathf.Floor((float)panoramaWidth * 5f / 8f);
		int startXPositive = (int)Mathf.Ceil((float)panoramaWidth * 5f / 8f);
		int endXPositive = (int)Mathf.Floor((float)panoramaWidth * 7f / 8f);
		int startZNegative = (int)Mathf.Ceil((float)panoramaWidth * 7f / 8f);
		int endZNegative = (int)Mathf.Floor((float)panoramaWidth * 1f / 8f);
		if (async)
		{
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuPositiveY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0, 0, panoramaWidth, endYPositive));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuNegativeY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0, startYNegative, panoramaWidth, panoramaHeight));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuPositiveX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startXPositive, endTopMixedRegion, endXPositive, startBottomMixedRegion));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuNegativeX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startXNegative, endTopMixedRegion, endXNegative, startBottomMixedRegion));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuPositiveZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startZPositive, endTopMixedRegion, endZPositive, startBottomMixedRegion));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startZNegative, endTopMixedRegion, panoramaWidth, startBottomMixedRegion));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0, endTopMixedRegion, endZNegative, startBottomMixedRegion));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, 0, endYPositive, panoramaWidth, endTopMixedRegion));
			yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, 0, startBottomMixedRegion, panoramaWidth, startYNegative));
			if (endZNegative < startXNegative)
			{
				yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endZNegative, endTopMixedRegion, startXNegative, startBottomMixedRegion));
			}
			if (endXNegative < startZPositive)
			{
				yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endXNegative, endTopMixedRegion, startZPositive, startBottomMixedRegion));
			}
			if (endZPositive < startXPositive)
			{
				yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endZPositive, endTopMixedRegion, startXPositive, startBottomMixedRegion));
			}
			if (endXPositive < startZNegative)
			{
				yield return ((MonoBehaviour)this).StartCoroutine(CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endXPositive, endTopMixedRegion, startZNegative, startBottomMixedRegion));
			}
		}
		else
		{
			IEnumerator enumerator = CubemapToEquirectangularCpuPositiveY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0, 0, panoramaWidth, endYPositive);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuNegativeY(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0, startYNegative, panoramaWidth, panoramaHeight);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuPositiveX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startXPositive, endTopMixedRegion, endXPositive, startBottomMixedRegion);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuNegativeX(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startXNegative, endTopMixedRegion, endXNegative, startBottomMixedRegion);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuPositiveZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startZPositive, endTopMixedRegion, endZPositive, startBottomMixedRegion);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, startZNegative, endTopMixedRegion, panoramaWidth, startBottomMixedRegion);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuNegativeZ(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, numPixelsAveraged, 0, endTopMixedRegion, endZNegative, startBottomMixedRegion);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, 0, endYPositive, panoramaWidth, endTopMixedRegion);
			while (enumerator.MoveNext())
			{
			}
			enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, 0, startBottomMixedRegion, panoramaWidth, startYNegative);
			while (enumerator.MoveNext())
			{
			}
			if (endZNegative < startXNegative)
			{
				enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endZNegative, endTopMixedRegion, startXNegative, startBottomMixedRegion);
				while (enumerator.MoveNext())
				{
				}
			}
			if (endXNegative < startZPositive)
			{
				enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endXNegative, endTopMixedRegion, startZPositive, startBottomMixedRegion);
				while (enumerator.MoveNext())
				{
				}
			}
			if (endZPositive < startXPositive)
			{
				enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endZPositive, endTopMixedRegion, startXPositive, startBottomMixedRegion);
				while (enumerator.MoveNext())
				{
				}
			}
			if (endXPositive < startZNegative)
			{
				enumerator = CubemapToEquirectangularCpuGeneralCase(cameraPixels, pixelValues, stride, panoramaWidth, panoramaHeight, ssaaFactor, startTime, processingTimePerFrame, maxWidth, maxHeight, numPixelsAveraged, endXPositive, endTopMixedRegion, startZNegative, startBottomMixedRegion);
				while (enumerator.MoveNext())
				{
				}
			}
		}
		yield return null;
	}

	private IEnumerator CubemapToEquirectangularCpuPositiveY(uint[] cameraPixels, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, float startTime, float processingTimePerFrame, int numPixelsAveraged, int startX, int startY, int endX, int endY)
	{
		Vector3 val = default(Vector3);
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
				{
					for (int j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
					{
						float num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
						float num6 = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
						float num7 = (num5 * 2f - 1f) * (float)Math.PI;
						float num8 = Mathf.Cos(num6);
						((Vector3)(ref val))._002Ector(num8 * Mathf.Sin(num7), 0f - Mathf.Sin(num6), num8 * Mathf.Cos(num7));
						float num9 = 1f / val.y;
						float num10 = val.x * num9;
						float num11 = val.z * num9;
						num10 = (num10 + 1f) / 2f;
						num11 = (num11 + 1f) / 2f;
						Color32 cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 2, num10, num11);
						num += cameraPixelBilinear.r;
						num2 += cameraPixelBilinear.g;
						num3 += cameraPixelBilinear.b;
						num4 += cameraPixelBilinear.a;
					}
				}
				int num12 = stride * (panoramaHeight - 1 - y) + x * 4;
				pixelValues[num12] = (byte)(num3 / numPixelsAveraged);
				pixelValues[num12 + 1] = (byte)(num2 / numPixelsAveraged);
				pixelValues[num12 + 2] = (byte)(num / numPixelsAveraged);
				pixelValues[num12 + 3] = (byte)(num4 / numPixelsAveraged);
				if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
				{
					yield return null;
					startTime = Time.realtimeSinceStartup;
				}
			}
		}
	}

	private IEnumerator CubemapToEquirectangularCpuNegativeY(uint[] cameraPixels, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, float startTime, float processingTimePerFrame, int numPixelsAveraged, int startX, int startY, int endX, int endY)
	{
		Vector3 val = default(Vector3);
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
				{
					for (int j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
					{
						float num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
						float num6 = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
						float num7 = (num5 * 2f - 1f) * (float)Math.PI;
						float num8 = Mathf.Cos(num6);
						((Vector3)(ref val))._002Ector(num8 * Mathf.Sin(num7), 0f - Mathf.Sin(num6), num8 * Mathf.Cos(num7));
						float num9 = 1f / val.y;
						float num10 = val.x * num9;
						float num11 = val.z * num9;
						num10 = 0f - num10;
						num10 = (num10 + 1f) / 2f;
						num11 = (num11 + 1f) / 2f;
						Color32 cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 3, num10, num11);
						num += cameraPixelBilinear.r;
						num2 += cameraPixelBilinear.g;
						num3 += cameraPixelBilinear.b;
						num4 += cameraPixelBilinear.a;
					}
				}
				int num12 = stride * (panoramaHeight - 1 - y) + x * 4;
				pixelValues[num12] = (byte)(num3 / numPixelsAveraged);
				pixelValues[num12 + 1] = (byte)(num2 / numPixelsAveraged);
				pixelValues[num12 + 2] = (byte)(num / numPixelsAveraged);
				pixelValues[num12 + 3] = (byte)(num4 / numPixelsAveraged);
				if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
				{
					yield return null;
					startTime = Time.realtimeSinceStartup;
				}
			}
		}
	}

	private IEnumerator CubemapToEquirectangularCpuPositiveX(uint[] cameraPixels, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, float startTime, float processingTimePerFrame, int numPixelsAveraged, int startX, int startY, int endX, int endY)
	{
		Vector3 val = default(Vector3);
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
				{
					for (int j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
					{
						float num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
						float num6 = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
						float num7 = (num5 * 2f - 1f) * (float)Math.PI;
						float num8 = Mathf.Cos(num6);
						((Vector3)(ref val))._002Ector(num8 * Mathf.Sin(num7), 0f - Mathf.Sin(num6), num8 * Mathf.Cos(num7));
						float num9 = 1f / val.x;
						float num10 = (0f - val.z) * num9;
						float num11 = val.y * num9;
						num11 = 0f - num11;
						num10 = (num10 + 1f) / 2f;
						num11 = (num11 + 1f) / 2f;
						Color32 cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 0, num10, num11);
						num += cameraPixelBilinear.r;
						num2 += cameraPixelBilinear.g;
						num3 += cameraPixelBilinear.b;
						num4 += cameraPixelBilinear.a;
					}
				}
				int num12 = stride * (panoramaHeight - 1 - y) + x * 4;
				pixelValues[num12] = (byte)(num3 / numPixelsAveraged);
				pixelValues[num12 + 1] = (byte)(num2 / numPixelsAveraged);
				pixelValues[num12 + 2] = (byte)(num / numPixelsAveraged);
				pixelValues[num12 + 3] = (byte)(num4 / numPixelsAveraged);
				if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
				{
					yield return null;
					startTime = Time.realtimeSinceStartup;
				}
			}
		}
	}

	private IEnumerator CubemapToEquirectangularCpuNegativeX(uint[] cameraPixels, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, float startTime, float processingTimePerFrame, int numPixelsAveraged, int startX, int startY, int endX, int endY)
	{
		Vector3 val = default(Vector3);
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
				{
					for (int j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
					{
						float num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
						float num6 = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
						float num7 = (num5 * 2f - 1f) * (float)Math.PI;
						float num8 = Mathf.Cos(num6);
						((Vector3)(ref val))._002Ector(num8 * Mathf.Sin(num7), 0f - Mathf.Sin(num6), num8 * Mathf.Cos(num7));
						float num9 = 1f / val.x;
						float num10 = (0f - val.z) * num9;
						float num11 = val.y * num9;
						num10 = (num10 + 1f) / 2f;
						num11 = (num11 + 1f) / 2f;
						Color32 cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 1, num10, num11);
						num += cameraPixelBilinear.r;
						num2 += cameraPixelBilinear.g;
						num3 += cameraPixelBilinear.b;
						num4 += cameraPixelBilinear.a;
					}
				}
				int num12 = stride * (panoramaHeight - 1 - y) + x * 4;
				pixelValues[num12] = (byte)(num3 / numPixelsAveraged);
				pixelValues[num12 + 1] = (byte)(num2 / numPixelsAveraged);
				pixelValues[num12 + 2] = (byte)(num / numPixelsAveraged);
				pixelValues[num12 + 3] = (byte)(num4 / numPixelsAveraged);
				if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
				{
					yield return null;
					startTime = Time.realtimeSinceStartup;
				}
			}
		}
	}

	private IEnumerator CubemapToEquirectangularCpuPositiveZ(uint[] cameraPixels, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, float startTime, float processingTimePerFrame, int numPixelsAveraged, int startX, int startY, int endX, int endY)
	{
		Vector3 val = default(Vector3);
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
				{
					for (int j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
					{
						float num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
						float num6 = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
						float num7 = (num5 * 2f - 1f) * (float)Math.PI;
						float num8 = Mathf.Cos(num6);
						((Vector3)(ref val))._002Ector(num8 * Mathf.Sin(num7), 0f - Mathf.Sin(num6), num8 * Mathf.Cos(num7));
						float num9 = 1f / val.z;
						float num10 = val.x * num9;
						float num11 = val.y * num9;
						num11 = 0f - num11;
						num10 = (num10 + 1f) / 2f;
						num11 = (num11 + 1f) / 2f;
						Color32 cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 4, num10, num11);
						num += cameraPixelBilinear.r;
						num2 += cameraPixelBilinear.g;
						num3 += cameraPixelBilinear.b;
						num4 += cameraPixelBilinear.a;
					}
				}
				int num12 = stride * (panoramaHeight - 1 - y) + x * 4;
				pixelValues[num12] = (byte)(num3 / numPixelsAveraged);
				pixelValues[num12 + 1] = (byte)(num2 / numPixelsAveraged);
				pixelValues[num12 + 2] = (byte)(num / numPixelsAveraged);
				pixelValues[num12 + 3] = (byte)(num4 / numPixelsAveraged);
				if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
				{
					yield return null;
					startTime = Time.realtimeSinceStartup;
				}
			}
		}
	}

	private IEnumerator CubemapToEquirectangularCpuNegativeZ(uint[] cameraPixels, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, float startTime, float processingTimePerFrame, int numPixelsAveraged, int startX, int startY, int endX, int endY)
	{
		Vector3 val = default(Vector3);
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
				{
					for (int j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
					{
						float num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
						float num6 = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
						float num7 = (num5 * 2f - 1f) * (float)Math.PI;
						float num8 = Mathf.Cos(num6);
						((Vector3)(ref val))._002Ector(num8 * Mathf.Sin(num7), 0f - Mathf.Sin(num6), num8 * Mathf.Cos(num7));
						float num9 = 1f / val.z;
						float num10 = val.x * num9;
						float num11 = val.y * num9;
						num10 = (num10 + 1f) / 2f;
						num11 = (num11 + 1f) / 2f;
						Color32 cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, 5, num10, num11);
						num += cameraPixelBilinear.r;
						num2 += cameraPixelBilinear.g;
						num3 += cameraPixelBilinear.b;
						num4 += cameraPixelBilinear.a;
					}
				}
				int num12 = stride * (panoramaHeight - 1 - y) + x * 4;
				pixelValues[num12] = (byte)(num3 / numPixelsAveraged);
				pixelValues[num12 + 1] = (byte)(num2 / numPixelsAveraged);
				pixelValues[num12 + 2] = (byte)(num / numPixelsAveraged);
				pixelValues[num12 + 3] = (byte)(num4 / numPixelsAveraged);
				if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
				{
					yield return null;
					startTime = Time.realtimeSinceStartup;
				}
			}
		}
	}

	private IEnumerator CubemapToEquirectangularCpuGeneralCase(uint[] cameraPixels, byte[] pixelValues, int stride, int panoramaWidth, int panoramaHeight, int ssaaFactor, float startTime, float processingTimePerFrame, float maxWidth, float maxHeight, int numPixelsAveraged, int startX, int startY, int endX, int endY)
	{
		Vector3 val = default(Vector3);
		for (int y = startY; y < endY; y++)
		{
			for (int x = startX; x < endX; x++)
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				for (int i = y * ssaaFactor; i < (y + 1) * ssaaFactor; i++)
				{
					for (int j = x * ssaaFactor; j < (x + 1) * ssaaFactor; j++)
					{
						float num5 = (float)j / (float)(panoramaWidth * ssaaFactor);
						float num6 = ((float)i / (float)(panoramaHeight * ssaaFactor) - 0.5f) * (float)Math.PI;
						float num7 = (num5 * 2f - 1f) * (float)Math.PI;
						float num8 = Mathf.Cos(num6);
						((Vector3)(ref val))._002Ector(num8 * Mathf.Sin(num7), 0f - Mathf.Sin(num6), num8 * Mathf.Cos(num7));
						float num9 = 1f / val.y;
						float num10 = val.x * num9;
						float num11 = val.z * num9;
						CubemapFace val2;
						if (val.y > 0f)
						{
							val2 = (CubemapFace)2;
						}
						else
						{
							val2 = (CubemapFace)3;
							num10 = 0f - num10;
						}
						if (Mathf.Abs(num10) > 1f || Mathf.Abs(num11) > 1f)
						{
							num9 = 1f / val.x;
							num10 = (0f - val.z) * num9;
							num11 = val.y * num9;
							if (val.x > 0f)
							{
								val2 = (CubemapFace)0;
								num11 = 0f - num11;
							}
							else
							{
								val2 = (CubemapFace)1;
							}
						}
						if (Mathf.Abs(num10) > 1f || Mathf.Abs(num11) > 1f)
						{
							num9 = 1f / val.z;
							num10 = val.x * num9;
							num11 = val.y * num9;
							if (val.z > 0f)
							{
								val2 = (CubemapFace)4;
								num11 = 0f - num11;
							}
							else
							{
								val2 = (CubemapFace)5;
							}
						}
						num10 = (num10 + 1f) / 2f;
						num11 = (num11 + 1f) / 2f;
						num10 = Mathf.Min(num10, maxWidth);
						num11 = Mathf.Min(num11, maxHeight);
						Color32 cameraPixelBilinear = GetCameraPixelBilinear(cameraPixels, (int)val2, num10, num11);
						num += cameraPixelBilinear.r;
						num2 += cameraPixelBilinear.g;
						num3 += cameraPixelBilinear.b;
						num4 += cameraPixelBilinear.a;
					}
				}
				int num12 = stride * (panoramaHeight - 1 - y) + x * 4;
				pixelValues[num12] = (byte)(num3 / numPixelsAveraged);
				pixelValues[num12 + 1] = (byte)(num2 / numPixelsAveraged);
				pixelValues[num12 + 2] = (byte)(num / numPixelsAveraged);
				pixelValues[num12 + 3] = (byte)(num4 / numPixelsAveraged);
				if ((x & 0xFF) == 0 && Time.realtimeSinceStartup - startTime > processingTimePerFrame)
				{
					yield return null;
					startTime = Time.realtimeSinceStartup;
				}
			}
		}
	}
}

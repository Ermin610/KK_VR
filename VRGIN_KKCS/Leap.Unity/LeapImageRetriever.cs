using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Leap.Unity;

[RequireComponent(typeof(Camera))]
public class LeapImageRetriever : MonoBehaviour
{
	public class LeapTextureData
	{
		private Texture2D _combinedTexture;

		private byte[] _intermediateArray;

		public Texture2D CombinedTexture => _combinedTexture;

		public bool CheckStale(Image image)
		{
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_combinedTexture == (Object)null || _intermediateArray == null)
			{
				return true;
			}
			if (image.Width != ((Texture)_combinedTexture).width || image.Height * 2 != ((Texture)_combinedTexture).height)
			{
				return true;
			}
			if (_combinedTexture.format != getTextureFormat(image))
			{
				return true;
			}
			return false;
		}

		public void Reconstruct(Image image, string globalShaderName, string pixelSizeName)
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected O, but got Unknown
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			int width = image.Width;
			int num = image.Height * 2;
			TextureFormat textureFormat = getTextureFormat(image);
			if ((Object)(object)_combinedTexture != (Object)null)
			{
				Object.DestroyImmediate((Object)(object)_combinedTexture);
			}
			_combinedTexture = new Texture2D(width, num, textureFormat, false, true);
			((Texture)_combinedTexture).wrapMode = (TextureWrapMode)1;
			((Texture)_combinedTexture).filterMode = (FilterMode)1;
			((Object)_combinedTexture).name = globalShaderName;
			((Object)_combinedTexture).hideFlags = (HideFlags)52;
			_intermediateArray = new byte[width * num * bytesPerPixel(textureFormat)];
			Shader.SetGlobalTexture(globalShaderName, (Texture)(object)_combinedTexture);
			Shader.SetGlobalVector(pixelSizeName, Vector4.op_Implicit(new Vector2(1f / (float)image.Width, 1f / (float)image.Height)));
		}

		public void UpdateTexture(Image image)
		{
			Array.Copy(image.Data, 0, _intermediateArray, 0, _intermediateArray.Length);
			_combinedTexture.LoadRawTextureData(_intermediateArray);
			_combinedTexture.Apply();
		}

		private TextureFormat getTextureFormat(Image image)
		{
			switch (image.Format)
			{
			case Image.FormatType.INFRARED:
				return (TextureFormat)1;
			case Image.FormatType.IBRG:
			case (Image.FormatType)4:
				return (TextureFormat)4;
			default:
				throw new Exception(string.Concat("Unexpected image format ", image.Format, "!"));
			}
		}

		private int bytesPerPixel(TextureFormat format)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Expected I4, but got Unknown
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Invalid comparison between Unknown and I4
			switch (format - 1)
			{
			default:
				if ((int)format != 14)
				{
					break;
				}
				goto case 3;
			case 0:
				return 1;
			case 3:
			case 4:
				return 4;
			case 1:
			case 2:
				break;
			}
			throw new Exception("Unexpected texture format " + format);
		}
	}

	public class LeapDistortionData
	{
		private Texture2D _combinedTexture;

		public Texture2D CombinedTexture => _combinedTexture;

		public bool CheckStale()
		{
			return (Object)(object)_combinedTexture == (Object)null;
		}

		public void Reconstruct(Image image, string shaderName)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Expected O, but got Unknown
			int num = image.DistortionWidth / 2;
			int num2 = image.DistortionHeight * 2;
			if ((Object)(object)_combinedTexture != (Object)null)
			{
				Object.DestroyImmediate((Object)(object)_combinedTexture);
			}
			Color32[] array = (Color32[])(object)new Color32[num * num2];
			_combinedTexture = new Texture2D(num, num2, (TextureFormat)4, false, true);
			((Texture)_combinedTexture).filterMode = (FilterMode)1;
			((Texture)_combinedTexture).wrapMode = (TextureWrapMode)1;
			((Object)_combinedTexture).hideFlags = (HideFlags)52;
			addDistortionData(image, array, 0);
			_combinedTexture.SetPixels32(array);
			_combinedTexture.Apply();
			Shader.SetGlobalTexture(shaderName, (Texture)(object)_combinedTexture);
		}

		private void addDistortionData(Image image, Color32[] colors, int startIndex)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			float[] distortion = image.Distortion;
			for (int i = 0; i < distortion.Length; i += 2)
			{
				encodeFloat(distortion[i], out var @byte, out var byte2);
				encodeFloat(distortion[i + 1], out var byte3, out var byte4);
				colors[i / 2 + startIndex] = new Color32(@byte, byte2, byte3, byte4);
			}
		}

		private void encodeFloat(float value, out byte byte0, out byte byte1)
		{
			value = (value + 0.6f) / 2.3f;
			float num = value;
			float num2 = value * 255f;
			num -= (float)(int)num;
			num2 -= (float)(int)num2;
			num -= 0.003921569f * num2;
			byte0 = (byte)(num * 256f);
			byte1 = (byte)(num2 * 256f);
		}
	}

	public class EyeTextureData
	{
		private const string IR_SHADER_VARIANT_NAME = "LEAP_FORMAT_IR";

		private const string RGB_SHADER_VARIANT_NAME = "LEAP_FORMAT_RGB";

		private const string GLOBAL_BRIGHT_TEXTURE_NAME = "_LeapGlobalBrightnessTexture";

		private const string GLOBAL_RAW_TEXTURE_NAME = "_LeapGlobalRawTexture";

		private const string GLOBAL_DISTORTION_TEXTURE_NAME = "_LeapGlobalDistortion";

		private const string GLOBAL_BRIGHT_PIXEL_SIZE_NAME = "_LeapGlobalBrightnessPixelSize";

		private const string GLOBAL_RAW_PIXEL_SIZE_NAME = "_LeapGlobalRawPixelSize";

		public readonly LeapTextureData BrightTexture;

		public readonly LeapTextureData RawTexture;

		public readonly LeapDistortionData Distortion;

		private bool _isStale;

		public static void ResetGlobalShaderValues()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val = new Texture2D(1, 1, (TextureFormat)5, false, false);
			((Object)val).name = "EmptyTexture";
			((Object)val).hideFlags = (HideFlags)52;
			val.SetPixel(0, 0, new Color(0f, 0f, 0f, 0f));
			Shader.SetGlobalTexture("_LeapGlobalBrightnessTexture", (Texture)(object)val);
			Shader.SetGlobalTexture("_LeapGlobalRawTexture", (Texture)(object)val);
			Shader.SetGlobalTexture("_LeapGlobalDistortion", (Texture)(object)val);
		}

		public EyeTextureData()
		{
			BrightTexture = new LeapTextureData();
			RawTexture = new LeapTextureData();
			Distortion = new LeapDistortionData();
		}

		public bool CheckStale(Image bright, Image raw)
		{
			if (!BrightTexture.CheckStale(bright) && !RawTexture.CheckStale(raw) && !Distortion.CheckStale())
			{
				return _isStale;
			}
			return true;
		}

		public void MarkStale()
		{
			_isStale = true;
		}

		public void Reconstruct(Image bright, Image raw)
		{
			BrightTexture.Reconstruct(bright, "_LeapGlobalBrightnessTexture", "_LeapGlobalBrightnessPixelSize");
			RawTexture.Reconstruct(raw, "_LeapGlobalRawTexture", "_LeapGlobalRawPixelSize");
			Distortion.Reconstruct(raw, "_LeapGlobalDistortion");
			switch (raw.Format)
			{
			case Image.FormatType.INFRARED:
				Shader.DisableKeyword("LEAP_FORMAT_RGB");
				Shader.EnableKeyword("LEAP_FORMAT_IR");
				break;
			case (Image.FormatType)4:
				Shader.DisableKeyword("LEAP_FORMAT_IR");
				Shader.EnableKeyword("LEAP_FORMAT_RGB");
				break;
			default:
				Debug.LogWarning((object)("Unexpected format type " + raw.Format));
				break;
			}
			_isStale = false;
		}

		public void UpdateTextures(Image bright, Image raw)
		{
			BrightTexture.UpdateTexture(bright);
			RawTexture.UpdateTexture(raw);
		}
	}

	public const string GLOBAL_COLOR_SPACE_GAMMA_NAME = "_LeapGlobalColorSpaceGamma";

	public const string GLOBAL_GAMMA_CORRECTION_EXPONENT_NAME = "_LeapGlobalGammaCorrectionExponent";

	public const string GLOBAL_CAMERA_PROJECTION_NAME = "_LeapGlobalProjection";

	public const int IMAGE_WARNING_WAIT = 10;

	public const int LEFT_IMAGE_INDEX = 0;

	public const int RIGHT_IMAGE_INDEX = 1;

	public const float IMAGE_SETTING_POLL_RATE = 2f;

	[SerializeField]
	private LeapServiceProvider _provider;

	[SerializeField]
	[FormerlySerializedAs("gammaCorrection")]
	private float _gammaCorrection = 1f;

	[SerializeField]
	protected long ImageTimeout = 9000L;

	private EyeTextureData _eyeTextureData = new EyeTextureData();

	protected Image _requestedImage = new Image();

	protected bool imagesEnabled = true;

	private bool checkingImageState;

	public EyeTextureData TextureData => _eyeTextureData;

	private void Start()
	{
		if ((Object)(object)_provider == (Object)null)
		{
			Debug.LogWarning((object)"Cannot use LeapImageRetriever if there is no LeapProvider!");
			((Behaviour)this).enabled = false;
		}
		else
		{
			ApplyGammaCorrectionValues();
			ApplyCameraProjectionValues(((Component)this).GetComponent<Camera>());
		}
	}

	private void OnEnable()
	{
		Controller leapController = _provider.GetLeapController();
		if (leapController != null)
		{
			onController(leapController);
		}
		else
		{
			((MonoBehaviour)this).StartCoroutine(waitForController());
		}
	}

	private void OnDisable()
	{
		((MonoBehaviour)this).StopAllCoroutines();
		if (_provider.GetLeapController() != null)
		{
			_provider.GetLeapController().DistortionChange -= onDistortionChange;
		}
	}

	private void OnDestroy()
	{
		((MonoBehaviour)this).StopAllCoroutines();
		if (_provider.GetLeapController() != null)
		{
			_provider.GetLeapController().DistortionChange -= onDistortionChange;
		}
	}

	private void OnPreRender()
	{
		if (!imagesEnabled)
		{
			return;
		}
		Controller leapController = _provider.GetLeapController();
		long num = leapController.Now();
		while (!_requestedImage.IsComplete && leapController.Now() - num <= ImageTimeout)
		{
		}
		if (_requestedImage.IsComplete)
		{
			if (_eyeTextureData.CheckStale(_requestedImage, _requestedImage))
			{
				_eyeTextureData.Reconstruct(_requestedImage, _requestedImage);
			}
			_eyeTextureData.UpdateTextures(_requestedImage, _requestedImage);
		}
		else if (!checkingImageState)
		{
			((MonoBehaviour)this).StartCoroutine(checkImageMode());
		}
	}

	private void Update()
	{
		if (imagesEnabled)
		{
			Frame currentFrame = _provider.CurrentFrame;
			Controller leapController = _provider.GetLeapController();
			_requestedImage = leapController.RequestImages(currentFrame.Id, Image.ImageType.DEFAULT);
		}
		else if (!checkingImageState)
		{
			((MonoBehaviour)this).StartCoroutine(checkImageMode());
		}
	}

	private IEnumerator waitForController()
	{
		Controller controller;
		do
		{
			controller = _provider.GetLeapController();
			yield return null;
		}
		while (controller == null);
		onController(controller);
	}

	private IEnumerator checkImageMode()
	{
		checkingImageState = true;
		yield return (object)new WaitForSeconds(2f);
		_provider.GetLeapController().Config.Get("images_mode", delegate(int enabled)
		{
			imagesEnabled = ((enabled != 0) ? true : false);
			checkingImageState = false;
		});
	}

	private void onController(Controller controller)
	{
		controller.DistortionChange += onDistortionChange;
		controller.Connect += delegate
		{
			_provider.GetLeapController().Config.Get("images_mode", delegate(int enabled)
			{
				imagesEnabled = ((enabled != 0) ? true : false);
			});
		};
		if (!checkingImageState)
		{
			((MonoBehaviour)this).StartCoroutine(checkImageMode());
		}
	}

	public void ApplyGammaCorrectionValues()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Invalid comparison between Unknown and I4
		float num = 1f;
		if ((int)QualitySettings.activeColorSpace != 1)
		{
			num = 0f - Mathf.Log10(Mathf.GammaToLinearSpace(0.1f));
		}
		Shader.SetGlobalFloat("_LeapGlobalColorSpaceGamma", num);
		Shader.SetGlobalFloat("_LeapGlobalGammaCorrectionExponent", 1f / _gammaCorrection);
	}

	public void ApplyCameraProjectionValues(Camera camera)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		Vector4 val = default(Vector4);
		Matrix4x4 projectionMatrix = camera.projectionMatrix;
		val.x = ((Matrix4x4)(ref projectionMatrix))[0, 2];
		val.y = 0f;
		projectionMatrix = camera.projectionMatrix;
		val.z = ((Matrix4x4)(ref projectionMatrix))[0, 0];
		projectionMatrix = camera.projectionMatrix;
		val.w = ((Matrix4x4)(ref projectionMatrix))[1, 1];
		Shader.SetGlobalVector("_LeapGlobalProjection", val);
	}

	private void onDistortionChange(object sender, LeapEventArgs args)
	{
		_eyeTextureData.MarkStale();
	}
}

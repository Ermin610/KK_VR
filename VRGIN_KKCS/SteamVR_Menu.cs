using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Valve.VR;

public class SteamVR_Menu : MonoBehaviour
{
	public Texture cursor;

	public Texture background;

	public Texture logo;

	public float logoHeight;

	public float menuOffset;

	public Vector2 scaleLimits = new Vector2(0.1f, 5f);

	public float scaleRate = 0.5f;

	private SteamVR_Overlay overlay;

	private Camera overlayCam;

	private Vector4 uvOffset;

	private float distance;

	private string scaleLimitX;

	private string scaleLimitY;

	private string scaleRateText;

	private CursorLockMode savedCursorLockState;

	private bool savedCursorVisible;

	public RenderTexture texture
	{
		get
		{
			if (!(overlay != null))
			{
				return null;
			}
			Texture obj = overlay.texture;
			return (RenderTexture)(object)((obj is RenderTexture) ? obj : null);
		}
	}

	public float scale { get; private set; }

	private void Awake()
	{
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		scaleLimitX = $"{scaleLimits.x:N1}";
		scaleLimitY = $"{scaleLimits.y:N1}";
		scaleRateText = $"{scaleRate:N1}";
		SteamVR_Overlay instance = SteamVR_Overlay.instance;
		if (instance != null)
		{
			uvOffset = instance.uvOffset;
			distance = instance.distance;
		}
	}

	private void OnGUI()
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Invalid comparison between Unknown and I4
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_05a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_05df: Unknown result type (might be due to invalid IL or missing references)
		if (overlay == null)
		{
			return;
		}
		Texture obj = overlay.texture;
		RenderTexture val = (RenderTexture)(object)((obj is RenderTexture) ? obj : null);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = val;
		if ((int)Event.current.type == 7)
		{
			GL.Clear(false, true, Color.clear);
		}
		Rect val2 = default(Rect);
		val2 = new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height);
		if (Screen.width < ((Texture)val).width)
		{
			val2.width = Screen.width;
			overlay.uvOffset.x = (0f - (float)(((Texture)val).width - Screen.width)) / (float)(2 * ((Texture)val).width);
		}
		if (Screen.height < ((Texture)val).height)
		{
			val2.height = Screen.height;
			overlay.uvOffset.y = (float)(((Texture)val).height - Screen.height) / (float)(2 * ((Texture)val).height);
		}
		GUILayout.BeginArea(val2);
		if (background != null)
		{
			GUI.DrawTexture(new Rect((val2.width - (float)background.width) / 2f, (val2.height - (float)background.height) / 2f, (float)background.width, (float)background.height), background);
		}
		GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[0]);
		GUILayout.FlexibleSpace();
		GUILayout.BeginVertical((GUILayoutOption[])(object)new GUILayoutOption[0]);
		if (logo != null)
		{
			GUILayout.Space(val2.height / 2f - logoHeight);
			GUILayout.Box(logo, (GUILayoutOption[])(object)new GUILayoutOption[0]);
		}
		GUILayout.Space(menuOffset);
		bool flag = GUILayout.Button("[Esc] - Close menu", (GUILayoutOption[])(object)new GUILayoutOption[0]);
		GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[0]);
		GUILayout.Label($"Scale: {scale:N4}", (GUILayoutOption[])(object)new GUILayoutOption[0]);
		float num = GUILayout.HorizontalSlider(scale, scaleLimits.x, scaleLimits.y, (GUILayoutOption[])(object)new GUILayoutOption[0]);
		if (num != scale)
		{
			SetScale(num);
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[0]);
		GUILayout.Label($"Scale limits:", (GUILayoutOption[])(object)new GUILayoutOption[0]);
		string text = GUILayout.TextField(scaleLimitX, (GUILayoutOption[])(object)new GUILayoutOption[0]);
		if (text != scaleLimitX && float.TryParse(text, out scaleLimits.x))
		{
			scaleLimitX = text;
		}
		string text2 = GUILayout.TextField(scaleLimitY, (GUILayoutOption[])(object)new GUILayoutOption[0]);
		if (text2 != scaleLimitY && float.TryParse(text2, out scaleLimits.y))
		{
			scaleLimitY = text2;
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[0]);
		GUILayout.Label($"Scale rate:", (GUILayoutOption[])(object)new GUILayoutOption[0]);
		string text3 = GUILayout.TextField(scaleRateText, (GUILayoutOption[])(object)new GUILayoutOption[0]);
		if (text3 != scaleRateText && float.TryParse(text3, out scaleRate))
		{
			scaleRateText = text3;
		}
		GUILayout.EndHorizontal();
		if (SteamVR.active)
		{
			SteamVR instance = SteamVR.instance;
			GUILayout.BeginHorizontal((GUILayoutOption[])(object)new GUILayoutOption[0]);
			float sceneResolutionScale = SteamVR_Camera.sceneResolutionScale;
			int num2 = (int)(instance.sceneWidth * sceneResolutionScale);
			int num3 = (int)(instance.sceneHeight * sceneResolutionScale);
			int num4 = (int)(100f * sceneResolutionScale);
			GUILayout.Label($"Scene quality: {num2}x{num3} ({num4}%)", (GUILayoutOption[])(object)new GUILayoutOption[0]);
			int num5 = Mathf.RoundToInt(GUILayout.HorizontalSlider((float)num4, 50f, 200f, (GUILayoutOption[])(object)new GUILayoutOption[0]));
			if (num5 != num4)
			{
				SteamVR_Camera.sceneResolutionScale = (float)num5 / 100f;
			}
			GUILayout.EndHorizontal();
		}
		overlay.highquality = GUILayout.Toggle(overlay.highquality, "High quality", (GUILayoutOption[])(object)new GUILayoutOption[0]);
		if (overlay.highquality)
		{
			overlay.curved = GUILayout.Toggle(overlay.curved, "Curved overlay", (GUILayoutOption[])(object)new GUILayoutOption[0]);
			overlay.antialias = GUILayout.Toggle(overlay.antialias, "Overlay RGSS(2x2)", (GUILayoutOption[])(object)new GUILayoutOption[0]);
		}
		else
		{
			overlay.curved = false;
			overlay.antialias = false;
		}
		SteamVR_Camera steamVR_Camera = SteamVR_Render.Top();
		if (steamVR_Camera != null)
		{
			steamVR_Camera.wireframe = GUILayout.Toggle(steamVR_Camera.wireframe, "Wireframe", (GUILayoutOption[])(object)new GUILayoutOption[0]);
			SteamVR_Render instance2 = SteamVR_Render.instance;
			if (instance2.trackingSpace == ETrackingUniverseOrigin.TrackingUniverseSeated)
			{
				if (GUILayout.Button("Switch to Standing", (GUILayoutOption[])(object)new GUILayoutOption[0]))
				{
					instance2.trackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;
				}
				if (GUILayout.Button("Center View", (GUILayoutOption[])(object)new GUILayoutOption[0]))
				{
					OpenVR.System?.ResetSeatedZeroPose();
				}
			}
			else if (GUILayout.Button("Switch to Seated", (GUILayoutOption[])(object)new GUILayoutOption[0]))
			{
				instance2.trackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
			}
		}
		if (GUILayout.Button("Exit", (GUILayoutOption[])(object)new GUILayoutOption[0]))
		{
			Application.Quit();
		}
		GUILayout.Space(menuOffset);
		string environmentVariable = Environment.GetEnvironmentVariable("VR_OVERRIDE");
		if (environmentVariable != null)
		{
			GUILayout.Label("VR_OVERRIDE=" + environmentVariable, (GUILayoutOption[])(object)new GUILayoutOption[0]);
		}
		GUILayout.Label("Graphics device: " + SystemInfo.graphicsDeviceVersion, (GUILayoutOption[])(object)new GUILayoutOption[0]);
		GUILayout.EndVertical();
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.EndArea();
		if (cursor != null)
		{
			float x = Input.mousePosition.x;
			float num6 = (float)Screen.height - Input.mousePosition.y;
			float num7 = cursor.width;
			float num8 = cursor.height;
			GUI.DrawTexture(new Rect(x, num6, num7, num8), cursor);
		}
		RenderTexture.active = active;
		if (flag)
		{
			HideMenu();
		}
	}

	public void ShowMenu()
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		SteamVR_Overlay instance = SteamVR_Overlay.instance;
		if (instance == null)
		{
			return;
		}
		Texture obj = instance.texture;
		RenderTexture val = (RenderTexture)(object)((obj is RenderTexture) ? obj : null);
		if (val == null)
		{
			Debug.LogError((object)"Menu requires overlay texture to be a render texture.");
			return;
		}
		SaveCursorState();
		Cursor.visible = true;
		Cursor.lockState = (CursorLockMode)0;
		overlay = instance;
		uvOffset = instance.uvOffset;
		distance = instance.distance;
		Camera[] array = Object.FindObjectsOfType(typeof(Camera)) as Camera[];
		foreach (Camera val2 in array)
		{
			if (((Behaviour)val2).enabled && val2.targetTexture == val)
			{
				overlayCam = val2;
				((Behaviour)overlayCam).enabled = false;
				break;
			}
		}
		SteamVR_Camera steamVR_Camera = SteamVR_Render.Top();
		if (steamVR_Camera != null)
		{
			scale = steamVR_Camera.origin.localScale.x;
		}
	}

	public void HideMenu()
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		RestoreCursorState();
		if (overlayCam != null)
		{
			((Behaviour)overlayCam).enabled = true;
		}
		if (overlay != null)
		{
			overlay.uvOffset = uvOffset;
			overlay.distance = distance;
			overlay = null;
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown((KeyCode)27) || Input.GetKeyDown((KeyCode)357))
		{
			if (overlay == null)
			{
				ShowMenu();
			}
			else
			{
				HideMenu();
			}
		}
		else if (Input.GetKeyDown((KeyCode)278))
		{
			SetScale(1f);
		}
		else if (Input.GetKey((KeyCode)280))
		{
			SetScale(Mathf.Clamp(scale + scaleRate * Time.deltaTime, scaleLimits.x, scaleLimits.y));
		}
		else if (Input.GetKey((KeyCode)281))
		{
			SetScale(Mathf.Clamp(scale - scaleRate * Time.deltaTime, scaleLimits.x, scaleLimits.y));
		}
	}

	private void SetScale(float scale)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		this.scale = scale;
		SteamVR_Camera steamVR_Camera = SteamVR_Render.Top();
		if (steamVR_Camera != null)
		{
			steamVR_Camera.origin.localScale = new Vector3(scale, scale, scale);
		}
	}

	private void SaveCursorState()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		savedCursorVisible = Cursor.visible;
		savedCursorLockState = Cursor.lockState;
	}

	private void RestoreCursorState()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		Cursor.visible = savedCursorVisible;
		Cursor.lockState = savedCursorLockState;
	}
}

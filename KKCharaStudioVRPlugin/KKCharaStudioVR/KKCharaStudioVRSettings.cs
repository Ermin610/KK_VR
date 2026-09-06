using System.Xml.Serialization;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

[XmlRoot("Settings")]
public class KKCharaStudioVRSettings : VRSettings
{
	public const string ControllerLayoutSplitHands = "split-hands";
	public const string ControllerLayoutLeftHand = "left-hand";
	public const string ControllerLayoutRightHand = "right-hand";

	private bool _LockRotXZ = true;
	private float _LocomotionSpeed = 2.0f;
	private float _SnapTurnAngle = 45f;
	private float _SnapTurnCooldown = 0.3f;
	private bool _SmoothTurnEnabled = false;
	private float _SmoothTurnSpeed = 90f;
	private bool _HandModelEnabled = true;
	private float _HandModelAlpha = 0.3f;
	private float _HandModelScale = 1.0f;

	[XmlComment("Lock XZ Axis (pitch / roll) rotation.")]
	public bool LockRotXZ
	{
		get { return _LockRotXZ; }
		set { _LockRotXZ = value; TriggerPropertyChanged("LockRotXZ"); }
	}

	[XmlComment("Speed for thumbstick locomotion")]
	public float LocomotionSpeed
	{
		get { return _LocomotionSpeed; }
		set { _LocomotionSpeed = value; TriggerPropertyChanged("LocomotionSpeed"); }
	}

	[XmlComment("Angle for snap turning")]
	public float SnapTurnAngle
	{
		get { return _SnapTurnAngle; }
		set { _SnapTurnAngle = value; TriggerPropertyChanged("SnapTurnAngle"); }
	}

	[XmlComment("Cooldown time for snap turning")]
	public float SnapTurnCooldown
	{
		get { return _SnapTurnCooldown; }
		set { _SnapTurnCooldown = value; TriggerPropertyChanged("SnapTurnCooldown"); }
	}

	[XmlComment("Enable smooth turning instead of snap turning")]
	public bool SmoothTurnEnabled
	{
		get { return _SmoothTurnEnabled; }
		set { _SmoothTurnEnabled = value; TriggerPropertyChanged("SmoothTurnEnabled"); }
	}

	[XmlComment("Speed for smooth turning (degrees per second)")]
	public float SmoothTurnSpeed
	{
		get { return _SmoothTurnSpeed; }
		set { _SmoothTurnSpeed = value; TriggerPropertyChanged("SmoothTurnSpeed"); }
	}

	[XmlComment("Enable VR hand models")]
	public bool HandModelEnabled
	{
		get { return _HandModelEnabled; }
		set { _HandModelEnabled = value; TriggerPropertyChanged("HandModelEnabled"); }
	}

	[XmlComment("Alpha transparency for VR hand models")]
	public float HandModelAlpha
	{
		get { return _HandModelAlpha; }
		set { _HandModelAlpha = value; TriggerPropertyChanged("HandModelAlpha"); }
	}

	[XmlComment("Scale of VR hand models")]
	public float HandModelScale
	{
		get { return _HandModelScale; }
		set { _HandModelScale = value; TriggerPropertyChanged("HandModelScale"); }
	}

	private bool _DynamicBoneCollisionEnabled = true;
	private float _ColliderRadius = 0.02f;
	private bool _HapticFeedbackEnabled = true;
	private float _HapticFeedbackIntensity = 0.5f;
	private bool _PhysicsHandsEnabled = true;
	private bool _VibrateOnlyOnBreasts = true;

	[XmlComment("Enable VAM-style non-clipping physics hands")]
	public bool PhysicsHandsEnabled
	{
		get { return _PhysicsHandsEnabled; }
		set { _PhysicsHandsEnabled = value; TriggerPropertyChanged("PhysicsHandsEnabled"); }
	}

	[XmlComment("Enable DynamicBone collision on hands")]
	public bool DynamicBoneCollisionEnabled
	{
		get { return _DynamicBoneCollisionEnabled; }
		set { _DynamicBoneCollisionEnabled = value; TriggerPropertyChanged("DynamicBoneCollisionEnabled"); }
	}

	[XmlComment("Hand collider radius")]
	public float ColliderRadius
	{
		get { return _ColliderRadius; }
		set { _ColliderRadius = value; TriggerPropertyChanged("ColliderRadius"); }
	}

	[XmlComment("Enable haptic feedback when touching characters")]
	public bool HapticFeedbackEnabled
	{
		get { return _HapticFeedbackEnabled; }
		set { _HapticFeedbackEnabled = value; TriggerPropertyChanged("HapticFeedbackEnabled"); }
	}

	[XmlComment("Haptic feedback intensity (0.0 to 1.0)")]
	public float HapticFeedbackIntensity
	{
		get { return _HapticFeedbackIntensity; }
		set { _HapticFeedbackIntensity = value; TriggerPropertyChanged("HapticFeedbackIntensity"); }
	}

	[XmlComment("Only vibrate when touching breasts")]
	public bool VibrateOnlyOnBreasts
	{
		get { return _VibrateOnlyOnBreasts; }
		set { _VibrateOnlyOnBreasts = value; TriggerPropertyChanged("VibrateOnlyOnBreasts"); }
	}

	private bool _ProximityGrabEnabled = true;
	private float _ProximityGrabRadius = 0.12f;

	[XmlComment("Enable proximity-based IK target grab")]
	public bool ProximityGrabEnabled
	{
		get { return _ProximityGrabEnabled; }
		set { _ProximityGrabEnabled = value; TriggerPropertyChanged("ProximityGrabEnabled"); }
	}

	[XmlComment("Radius for proximity grab detection (meters)")]
	public float ProximityGrabRadius
	{
		get { return _ProximityGrabRadius; }
		set { _ProximityGrabRadius = value; TriggerPropertyChanged("ProximityGrabRadius"); }
	}

	private float _UISpawnDistance = 2.0f;
	private float _UISpawnScale = 1.0f;
	private string _ControllerFaceButtonLayout = ControllerLayoutSplitHands;
	private bool _TimelineFollowCamera = true;
	private float _MirrorResolutionScale = KKCharaStudioVR.Mirror.VRReflection.DefaultResolutionScale;

	[XmlComment("Distance in front of head when UI respawns (meters)")]
	public float UISpawnDistance
	{
		get { return _UISpawnDistance; }
		set { _UISpawnDistance = value; TriggerPropertyChanged("UISpawnDistance"); }
	}

	[XmlComment("Main Studio GUI scale multiplier when it respawns")]
	public float UISpawnScale
	{
		get { return _UISpawnScale; }
		set { _UISpawnScale = value; TriggerPropertyChanged("UISpawnScale"); }
	}

	[XmlComment("Face button layout: split-hands, left-hand, or right-hand")]
	public string ControllerFaceButtonLayout
	{
		get { return _ControllerFaceButtonLayout; }
		set
		{
			_ControllerFaceButtonLayout = NormalizeControllerFaceButtonLayout(value);
			TriggerPropertyChanged("ControllerFaceButtonLayout");
		}
	}

	[XmlComment("Mirror reflection resolution as a fraction of the eye resolution, 0.1 to 1. Lower costs less fill")]
	public float MirrorResolutionScale
	{
		get { return _MirrorResolutionScale; }
		set { _MirrorResolutionScale = Mathf.Clamp(value, 0.1f, 1f); TriggerPropertyChanged("MirrorResolutionScale"); }
	}

	[XmlComment("Follow Timeline camera animation in VR; false plays animation without moving the VR view")]
	public bool TimelineFollowCamera
	{
		get { return _TimelineFollowCamera; }
		set { _TimelineFollowCamera = value; TriggerPropertyChanged("TimelineFollowCamera"); }
	}

	private bool _ComfortVignetteEnabled = true;
	private float _ComfortVignetteRadius = 0.5f;
	private bool _TwoHandScaleEnabled = true;
	private bool _WristMenuEnabled = true;
	private float _WristMenuScale = 1.0f;
	private string _WristMenuLanguage = "zh-CN";
	private string _VmdRootPath = string.Empty;
	private bool _TimelineFovOverrideEnabled;
	private float _TimelineFovOverrideValue = 53.13f;
	private float _TimelineVerticalOffset;
	private float _TimelineYawOffset;
	private bool _TimelineSavedControlPresetAvailable;
	private bool _TimelineSavedFovOverrideEnabled = true;
	private float _TimelineSavedFovOverrideValue = 53.13f;
	private float _TimelineSavedVerticalOffset;
	private float _TimelineSavedYawOffset;
	private bool _PreserveOutfitOnCharacterReplace;
	private bool _AutoApplyHighHeelsPreset = true;
	private bool _HideHandsAndUiDuringMmd;
	private float _MmdFovAdjustSpeed = 20f;
	private bool _MmdClothingCueEnabled;
	private string _MmdClothingCuePresetId = VRMmdCueSheetStore.DefaultPresetId;

	[XmlComment("Enable comfort vignette during movement")]
	public bool ComfortVignetteEnabled
	{
		get { return _ComfortVignetteEnabled; }
		set { _ComfortVignetteEnabled = value; TriggerPropertyChanged("ComfortVignetteEnabled"); }
	}

	[XmlComment("Vignette clear radius (0.3 = strong, 0.8 = subtle)")]
	public float ComfortVignetteRadius
	{
		get { return _ComfortVignetteRadius; }
		set { _ComfortVignetteRadius = value; TriggerPropertyChanged("ComfortVignetteRadius"); }
	}

	[XmlComment("Enable two-hand world scaling")]
	public bool TwoHandScaleEnabled
	{
		get { return _TwoHandScaleEnabled; }
		set { _TwoHandScaleEnabled = value; TriggerPropertyChanged("TwoHandScaleEnabled"); }
	}

	[XmlComment("Enable the compact left-wrist quick menu")]
	public bool WristMenuEnabled
	{
		get { return _WristMenuEnabled; }
		set { _WristMenuEnabled = value; TriggerPropertyChanged("WristMenuEnabled"); }
	}

	[XmlComment("Scale of the compact wrist menu")]
	public float WristMenuScale
	{
		get { return _WristMenuScale; }
		set { _WristMenuScale = value; TriggerPropertyChanged("WristMenuScale"); }
	}

	[XmlComment("Wrist menu language: zh-CN, ja-JP, or en-US")]
	public string WristMenuLanguage
	{
		get { return _WristMenuLanguage; }
		set
		{
			_WristMenuLanguage = NormalizeWristMenuLanguage(value);
			TriggerPropertyChanged("WristMenuLanguage");
		}
	}

	[XmlComment("Root folder containing VMD motion, camera, and audio files")]
	public string VmdRootPath
	{
		get { return _VmdRootPath; }
		set
		{
			_VmdRootPath = value ?? string.Empty;
			TriggerPropertyChanged("VmdRootPath");
		}
	}

	[XmlComment("Match Timeline desktop framing in VR by converting the animated source FOV to a bounded camera distance")]
	public bool TimelineFovOverrideEnabled
	{
		get { return _TimelineFovOverrideEnabled; }
		set { _TimelineFovOverrideEnabled = value; TriggerPropertyChanged("TimelineFovOverrideEnabled"); }
	}

	[XmlComment("Reference FOV used by Timeline VR composition-distance matching, in degrees")]
	public float TimelineFovOverrideValue
	{
		get { return _TimelineFovOverrideValue; }
		set
		{
			_TimelineFovOverrideValue = System.Math.Max(20f, System.Math.Min(120f, value));
			TriggerPropertyChanged("TimelineFovOverrideValue");
		}
	}

	[XmlComment("Vertical world-space offset applied to the Timeline VR camera, in meters")]
	public float TimelineVerticalOffset
	{
		get { return _TimelineVerticalOffset; }
		set
		{
			_TimelineVerticalOffset = System.Math.Max(-10f, System.Math.Min(10f, value));
			TriggerPropertyChanged("TimelineVerticalOffset");
		}
	}

	[XmlComment("Linear yaw offset applied to the Timeline VR camera, in degrees")]
	public float TimelineYawOffset
	{
		get { return _TimelineYawOffset; }
		set
		{
			_TimelineYawOffset = NormalizeTimelineYaw(value);
			TriggerPropertyChanged("TimelineYawOffset");
		}
	}

	[XmlComment("Whether an explicit Timeline control preset has been saved")]
	public bool TimelineSavedControlPresetAvailable
	{
		get { return _TimelineSavedControlPresetAvailable; }
		set
		{
			_TimelineSavedControlPresetAvailable = value;
			TriggerPropertyChanged("TimelineSavedControlPresetAvailable");
		}
	}

	[XmlComment("Saved Timeline composition-match state")]
	public bool TimelineSavedFovOverrideEnabled
	{
		get { return _TimelineSavedFovOverrideEnabled; }
		set
		{
			_TimelineSavedFovOverrideEnabled = value;
			TriggerPropertyChanged("TimelineSavedFovOverrideEnabled");
		}
	}

	[XmlComment("Saved Timeline reference FOV, in degrees")]
	public float TimelineSavedFovOverrideValue
	{
		get { return _TimelineSavedFovOverrideValue; }
		set
		{
			_TimelineSavedFovOverrideValue = System.Math.Max(20f, System.Math.Min(120f, value));
			TriggerPropertyChanged("TimelineSavedFovOverrideValue");
		}
	}

	[XmlComment("Saved Timeline vertical world-space offset, in meters")]
	public float TimelineSavedVerticalOffset
	{
		get { return _TimelineSavedVerticalOffset; }
		set
		{
			_TimelineSavedVerticalOffset = System.Math.Max(-10f, System.Math.Min(10f, value));
			TriggerPropertyChanged("TimelineSavedVerticalOffset");
		}
	}

	[XmlComment("Saved Timeline linear yaw offset, in degrees")]
	public float TimelineSavedYawOffset
	{
		get { return _TimelineSavedYawOffset; }
		set
		{
			_TimelineSavedYawOffset = NormalizeTimelineYaw(value);
			TriggerPropertyChanged("TimelineSavedYawOffset");
		}
	}

	[XmlComment("Keep the current outfit when replacing a Studio character card")]
	public bool PreserveOutfitOnCharacterReplace
	{
		get { return _PreserveOutfitOnCharacterReplace; }
		set { _PreserveOutfitOnCharacterReplace = value; TriggerPropertyChanged("PreserveOutfitOnCharacterReplace"); }
	}

	[XmlComment("Automatically apply the matching per-character high-heels preset when available")]
	public bool AutoApplyHighHeelsPreset
	{
		get { return _AutoApplyHighHeelsPreset; }
		set { _AutoApplyHighHeelsPreset = value; TriggerPropertyChanged("AutoApplyHighHeelsPreset"); }
	}

	[XmlComment("Hide VR hands and interaction UI while MMD playback is running")]
	public bool HideHandsAndUiDuringMmd
	{
		get { return _HideHandsAndUiDuringMmd; }
		set { _HideHandsAndUiDuringMmd = value; TriggerPropertyChanged("HideHandsAndUiDuringMmd"); }
	}

	[XmlComment("Fixed-FOV adjustment speed during MMD presentation mode, in degrees per second")]
	public float MmdFovAdjustSpeed
	{
		get { return _MmdFovAdjustSpeed; }
		set
		{
			_MmdFovAdjustSpeed = System.Math.Max(5f, System.Math.Min(60f, value));
			TriggerPropertyChanged("MmdFovAdjustSpeed");
		}
	}

	[XmlComment("Enable percentage-based clothing cues during MMD playback")]
	public bool MmdClothingCueEnabled
	{
		get { return _MmdClothingCueEnabled; }
		set { _MmdClothingCueEnabled = value; TriggerPropertyChanged("MmdClothingCueEnabled"); }
	}

	[XmlComment("Stable ID of the selected global MMD clothing fade preset")]
	public string MmdClothingCuePresetId
	{
		get { return _MmdClothingCuePresetId; }
		set
		{
			_MmdClothingCuePresetId = NormalizeMmdClothingCuePresetId(value);
			TriggerPropertyChanged("MmdClothingCuePresetId");
		}
	}

	private float _HandOffsetX = 0f;
	private float _HandOffsetY = -0.02f;
	private float _HandOffsetZ = -0.05f;

	[XmlComment("Hand model X offset")]
	public float HandOffsetX
	{
		get { return _HandOffsetX; }
		set { _HandOffsetX = value; TriggerPropertyChanged("HandOffsetX"); }
	}

	[XmlComment("Hand model Y offset")]
	public float HandOffsetY
	{
		get { return _HandOffsetY; }
		set { _HandOffsetY = value; TriggerPropertyChanged("HandOffsetY"); }
	}

	[XmlComment("Hand model Z offset")]
	public float HandOffsetZ
	{
		get { return _HandOffsetZ; }
		set { _HandOffsetZ = value; TriggerPropertyChanged("HandOffsetZ"); }
	}

	private float _HandRotPitch = 30f;
	private float _HandRotYaw = 0f;
	private float _HandRotRoll = 0f;

	[XmlComment("Hand model Pitch rotation offset")]
	public float HandRotPitch
	{
		get { return _HandRotPitch; }
		set { _HandRotPitch = value; TriggerPropertyChanged("HandRotPitch"); }
	}

	[XmlComment("Hand model Yaw rotation offset")]
	public float HandRotYaw
	{
		get { return _HandRotYaw; }
		set { _HandRotYaw = value; TriggerPropertyChanged("HandRotYaw"); }
	}

	[XmlComment("Hand model Roll rotation offset")]
	public float HandRotRoll
	{
		get { return _HandRotRoll; }
		set { _HandRotRoll = value; TriggerPropertyChanged("HandRotRoll"); }
	}

	public static KKCharaStudioVRSettings Load(string path)
	{
		return VRSettings.Load<KKCharaStudioVRSettings>(path);
	}

	private static string NormalizeWristMenuLanguage(string value)
	{
		if (string.Equals(value, "ja-JP", System.StringComparison.OrdinalIgnoreCase))
			return "ja-JP";
		if (string.Equals(value, "en-US", System.StringComparison.OrdinalIgnoreCase))
			return "en-US";
		return "zh-CN";
	}

	private static string NormalizeControllerFaceButtonLayout(string value)
	{
		if (string.Equals(value, ControllerLayoutLeftHand, System.StringComparison.OrdinalIgnoreCase))
			return ControllerLayoutLeftHand;
		if (string.Equals(value, ControllerLayoutRightHand, System.StringComparison.OrdinalIgnoreCase))
			return ControllerLayoutRightHand;
		return ControllerLayoutSplitHands;
	}

	private static float NormalizeTimelineYaw(float value)
	{
		if (float.IsNaN(value) || float.IsInfinity(value))
			return 0f;
		while (value > 180f)
			value -= 360f;
		while (value < -180f)
			value += 360f;
		return value;
	}

	private static string NormalizeMmdClothingCuePresetId(string value)
	{
		return VRMmdCueSheetStore.NormalizePresetId(value);
	}

}

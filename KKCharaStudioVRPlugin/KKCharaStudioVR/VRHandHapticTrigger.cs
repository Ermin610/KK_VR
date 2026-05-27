using System;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;

namespace KKCharaStudioVR
{
    public class VRHandHapticTrigger : MonoBehaviour
    {
        private SteamVR_TrackedObject _trackedObject;
        private KKCharaStudioVRSettings _settings;
        private float _lastPulseTime;
        private bool _isLeftHand;

        private void Start()
        {
            _trackedObject = GetComponentInParent<SteamVR_TrackedObject>();
            _isLeftHand = GetComponentInParent<VRGIN.Controls.LeftController>() != null;
            if (VR.Manager != null && VR.Manager.Context != null)
            {
                _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_settings == null || !_settings.HapticFeedbackEnabled) return;
            if (_trackedObject == null || !_trackedObject.isValid) return;

            // Only fire haptics when touching character meshes (SkinnedMeshRenderer) or dynamic bones
            if (other == null) return;
            var smr = other.GetComponentInParent<SkinnedMeshRenderer>();
            var db = other.GetComponentInParent<DynamicBone>();
            if (smr == null && db == null) return;

            int deviceIndex = (int)_trackedObject.index;
            var device = SteamVR_Controller.Input(deviceIndex);
            if (device == null) return;

            float speed = device.velocity.magnitude;

            // Detect if touching the pelvic/crotch region for insertion haptics
            string nameLower = other.name.ToLower();
            string parentNameLower = other.transform.parent != null ? other.transform.parent.name.ToLower() : "";
            bool isPelvic = nameLower.Contains("pelvis") || nameLower.Contains("kokan") || nameLower.Contains("siri") || nameLower.Contains("parts") || nameLower.Contains("penetration") ||
                            parentNameLower.Contains("pelvis") || parentNameLower.Contains("kokan") || parentNameLower.Contains("siri") || parentNameLower.Contains("parts") || parentNameLower.Contains("penetration");

            if (isPelvic)
            {
                // A. Pelvic Insertion: Deep, firm vibration wave modulated by velocity (25Hz)
                if (Time.time - _lastPulseTime > 0.04f)
                {
                    float intensity = _settings.HapticFeedbackIntensity;
                    ushort duration = (ushort)Mathf.Clamp((speed * 3500f + 700f) * intensity, 400f * intensity, 3999f);
                    device.TriggerHapticPulse(duration, EVRButtonId.k_EButton_Axis0);
                    _lastPulseTime = Time.time;

                    if (VRHandModelManager.Instance != null)
                        VRHandModelManager.Instance.NotifyTouch(_isLeftHand);
                }
            }
            else
            {
                // B. Skin Sliding Micro-textures: Continuous high-frequency silky friction purr (50Hz), only when hand is moving
                if (speed > 0.02f && Time.time - _lastPulseTime > 0.02f)
                {
                    float intensity = _settings.HapticFeedbackIntensity;
                    ushort duration = (ushort)Mathf.Clamp((speed * 400f + 100f) * intensity, 100f * intensity, 450f * intensity);
                    device.TriggerHapticPulse(duration, EVRButtonId.k_EButton_Axis0);
                    _lastPulseTime = Time.time;

                    if (VRHandModelManager.Instance != null)
                        VRHandModelManager.Instance.NotifyTouch(_isLeftHand);
                }
            }
        }
    }
}

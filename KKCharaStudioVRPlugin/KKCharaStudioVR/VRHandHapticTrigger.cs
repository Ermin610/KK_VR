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

            // Only fire haptics when touching character meshes (SkinnedMeshRenderer),
            // not IK markers, GUIQuad, or other tool colliders
            if (other == null) return;
            var smr = other.GetComponentInParent<SkinnedMeshRenderer>();
            var db = other.GetComponentInParent<DynamicBone>();
            if (smr == null && db == null) return;

            // Cooldown to prevent rapid continuous firing
            if (Time.time - _lastPulseTime > 0.05f)
            {
                int deviceIndex = (int)_trackedObject.index;
                var device = SteamVR_Controller.Input(deviceIndex);
                if (device != null)
                {
                    ushort duration = (ushort)Mathf.Clamp(_settings.HapticFeedbackIntensity * 2000f, 100f, 3999f);
                    device.TriggerHapticPulse(duration, EVRButtonId.k_EButton_Axis0);
                    _lastPulseTime = Time.time;

                    // 通知手部模型变色
                    if (VRHandModelManager.Instance != null)
                        VRHandModelManager.Instance.NotifyTouch(_isLeftHand);
                }
            }
        }
    }
}

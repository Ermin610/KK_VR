using System;
using UnityEngine;
using Object = UnityEngine.Object;
using Valve.VR;
using VRGIN.Core;

namespace KKCharaStudioVR
{
    public class VRHandHapticTrigger : MonoBehaviour
    {
        public SteamVR_TrackedObject trackedObject;
        public bool isLeftHand;
        private KKCharaStudioVRSettings _settings;
        private float _lastPulseTime;

        private void Start()
        {
            if (trackedObject == null)
            {
                trackedObject = GetComponentInParent<SteamVR_TrackedObject>();
            }
            if (trackedObject == null)
            {
                var left = GetComponentInParent<VRGIN.Controls.LeftController>();
                if (left != null)
                {
                    trackedObject = left.Tracking;
                    isLeftHand = true;
                }
                else
                {
                    var right = GetComponentInParent<VRGIN.Controls.RightController>();
                    if (right != null)
                    {
                        trackedObject = right.Tracking;
                        isLeftHand = false;
                    }
                }
            }
            else
            {
                var left = GetComponentInParent<VRGIN.Controls.LeftController>() ?? trackedObject.GetComponent<VRGIN.Controls.LeftController>();
                if (left != null)
                {
                    isLeftHand = true;
                }
            }

            if (VR.Manager != null && VR.Manager.Context != null)
            {
                _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (_settings == null || !_settings.HapticFeedbackEnabled) return;
            if (trackedObject == null || trackedObject.index == SteamVR_TrackedObject.EIndex.None) return;

            if (other == null) return;

            // Prevent self-vibration by ignoring any collider that is part of the VR controller or VR hands
            var t = other.transform;
            while (t != null)
            {
                string lowerName = t.name.ToLower();
                if (lowerName.Contains("vrhand") || 
                    lowerName.Contains("controller") || 
                    lowerName.Contains("trackedobject") ||
                    lowerName.Contains("steamvr") ||
                    lowerName.StartsWith("l_") && lowerName.EndsWith("_col") ||
                    lowerName.StartsWith("r_") && lowerName.EndsWith("_col"))
                {
                    return;
                }
                t = t.parent;
            }
            var smr = other.GetComponentInParent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                string smrName = smr.name;
                if (smrName.Contains("o_hand") || smrName.Contains("silhouette") || smrName.Contains("VRHand"))
                {
                    smr = null;
                }
            }
            
            // Check for any version of DynamicBone in parent using runtime class name checks
            bool hasDynamicBone = false;
            var parent = other.transform;
            while (parent != null)
            {
                var behaviours = parent.GetComponents<MonoBehaviour>();
                foreach (var b in behaviours)
                {
                    if (b != null)
                    {
                        string name = b.GetType().Name;
                        if (name.Contains("DynamicBone") && !name.Contains("Collider"))
                        {
                            hasDynamicBone = true;
                            break;
                        }
                    }
                }
                if (hasDynamicBone) break;
                parent = parent.parent;
            }

            // Only fire haptics if touching character meshes or dynamic bones
            if (smr == null && !hasDynamicBone) return;

            // Only vibrate on breasts if setting is enabled
            if (_settings != null && _settings.VibrateOnlyOnBreasts)
            {
                bool isBreast = false;
                var curr = other.transform;
                while (curr != null)
                {
                    string currName = curr.name.ToLower();
                    if (currName.Contains("mune") || currName.Contains("glands") || currName.Contains("breast"))
                    {
                        isBreast = true;
                        break;
                    }
                    curr = curr.parent;
                }
                if (!isBreast) return;
            }

            // Cooldown to prevent rapid continuous firing
            if (Time.time - _lastPulseTime > 0.05f)
            {
                int deviceIndex = (int)trackedObject.index;
                var device = SteamVR_Controller.Input(deviceIndex);
                if (device != null)
                {
                    ushort duration = (ushort)Mathf.Clamp(_settings.HapticFeedbackIntensity * 2000f, 100f, 3999f);
                    device.TriggerHapticPulse(duration, EVRButtonId.k_EButton_Axis0);
                    _lastPulseTime = Time.time;

                    // 通知手部模型变色
                    if (VRHandModelManager.Instance != null)
                        VRHandModelManager.Instance.NotifyTouch(isLeftHand);
                }
            }
        }
    }
}

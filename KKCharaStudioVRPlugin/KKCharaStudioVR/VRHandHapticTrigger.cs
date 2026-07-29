using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;

namespace KKCharaStudioVR
{
    public class VRHandHapticTrigger : MonoBehaviour
    {
        private struct TargetInfo
        {
            public bool isValid;
            public bool isBreast;

            public TargetInfo(bool valid, bool breast)
            {
                isValid = valid;
                isBreast = breast;
            }
        }

        private const float PulseCooldown = 0.05f;
        private const int MaxCachedColliders = 512;
        private static readonly Dictionary<Collider, TargetInfo> TargetCache = new Dictionary<Collider, TargetInfo>();
        private static readonly Dictionary<int, float> LastPulseByDevice = new Dictionary<int, float>();

        public SteamVR_TrackedObject trackedObject;
        public bool isLeftHand;
        private KKCharaStudioVRSettings _settings;

        private void Start()
        {
            if (trackedObject == null)
                trackedObject = GetComponentInParent<SteamVR_TrackedObject>();

            if (trackedObject == null)
            {
                VRGIN.Controls.LeftController left = GetComponentInParent<VRGIN.Controls.LeftController>();
                if (left != null)
                {
                    trackedObject = left.Tracking;
                    isLeftHand = true;
                }
                else
                {
                    VRGIN.Controls.RightController right = GetComponentInParent<VRGIN.Controls.RightController>();
                    if (right != null)
                    {
                        trackedObject = right.Tracking;
                        isLeftHand = false;
                    }
                }
            }
            else
            {
                VRGIN.Controls.LeftController left = GetComponentInParent<VRGIN.Controls.LeftController>()
                    ?? trackedObject.GetComponent<VRGIN.Controls.LeftController>();
                if (left != null) isLeftHand = true;
            }

            ResolveSettings();
        }

        private void ResolveSettings()
        {
            if (VR.Manager != null && VR.Manager.Context != null)
                _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
        }

        private void OnTriggerStay(Collider other)
        {
            if (_settings == null) ResolveSettings();
            if (_settings == null || !_settings.HapticFeedbackEnabled) return;
            if (trackedObject == null || trackedObject.index == SteamVR_TrackedObject.EIndex.None || other == null) return;

            int deviceIndex = (int)trackedObject.index;
            float lastPulse;
            if (LastPulseByDevice.TryGetValue(deviceIndex, out lastPulse) &&
                Time.time - lastPulse <= PulseCooldown)
                return;

            TargetInfo target = GetTargetInfo(other);
            if (!target.isValid) return;
            if (_settings.VibrateOnlyOnBreasts && !target.isBreast) return;

            SteamVR_Controller.Device device = SteamVR_Controller.Input(deviceIndex);
            if (device == null) return;

            ushort duration = (ushort)Mathf.Clamp(_settings.HapticFeedbackIntensity * 2000f, 100f, 3999f);
            device.TriggerHapticPulse(duration, EVRButtonId.k_EButton_Axis0);
            LastPulseByDevice[deviceIndex] = Time.time;

            if (VRHandModelManager.Instance != null)
                VRHandModelManager.Instance.NotifyTouch(isLeftHand);
        }

        private static TargetInfo GetTargetInfo(Collider collider)
        {
            TargetInfo cached;
            if (TargetCache.TryGetValue(collider, out cached)) return cached;
            if (TargetCache.Count >= MaxCachedColliders) TargetCache.Clear();

            bool isBreast = false;
            bool hasDynamicBone = false;
            Transform current = collider.transform;
            while (current != null)
            {
                string objectName = current.name ?? string.Empty;
                if (ContainsIgnoreCase(objectName, "vrhand") ||
                    ContainsIgnoreCase(objectName, "controller") ||
                    ContainsIgnoreCase(objectName, "trackedobject") ||
                    ContainsIgnoreCase(objectName, "steamvr") ||
                    (objectName.StartsWith("l_", StringComparison.OrdinalIgnoreCase) && objectName.EndsWith("_col", StringComparison.OrdinalIgnoreCase)) ||
                    (objectName.StartsWith("r_", StringComparison.OrdinalIgnoreCase) && objectName.EndsWith("_col", StringComparison.OrdinalIgnoreCase)))
                {
                    cached = new TargetInfo(false, false);
                    TargetCache[collider] = cached;
                    return cached;
                }

                if (ContainsIgnoreCase(objectName, "mune") ||
                    ContainsIgnoreCase(objectName, "glands") ||
                    ContainsIgnoreCase(objectName, "breast"))
                    isBreast = true;

                if (!hasDynamicBone)
                {
                    MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                    foreach (MonoBehaviour behaviour in behaviours)
                    {
                        if (behaviour == null) continue;
                        string typeName = behaviour.GetType().Name;
                        if (typeName.IndexOf("DynamicBone", StringComparison.Ordinal) >= 0 &&
                            typeName.IndexOf("Collider", StringComparison.Ordinal) < 0)
                        {
                            hasDynamicBone = true;
                            break;
                        }
                    }
                }
                current = current.parent;
            }

            bool hasCharacterMesh = false;
            SkinnedMeshRenderer skinnedMesh = collider.GetComponentInParent<SkinnedMeshRenderer>();
            if (skinnedMesh != null)
            {
                string rendererName = skinnedMesh.name ?? string.Empty;
                hasCharacterMesh = !ContainsIgnoreCase(rendererName, "o_hand") &&
                    !ContainsIgnoreCase(rendererName, "silhouette") &&
                    !ContainsIgnoreCase(rendererName, "vrhand");
            }

            cached = new TargetInfo(hasCharacterMesh || hasDynamicBone, isBreast);
            TargetCache[collider] = cached;
            return cached;
        }

        private static bool ContainsIgnoreCase(string value, string fragment)
        {
            return value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

using UnityEngine;
using VRGIN.Core;
using Valve.VR;

namespace KKCharaStudioVR
{
    public class VRTwoHandScale : MonoBehaviour
    {
        public static VRTwoHandScale Instance { get; private set; }

        /// <summary>
        /// 是否正在双手缩放。GripMoveKKCharaStudioTool 检查此属性以跳过世界移动。
        /// </summary>
        public bool IsScaling
        {
            get { return _isScaling; }
        }

        private float _initialDistance;
        private Vector3 _initialScale;
        private bool _isScaling;
        private KKCharaStudioVRSettings _settings;

        void Start()
        {
            Instance = this;
            if (VR.Manager != null && VR.Manager.Context != null)
                _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
        }

        void Update()
        {
            // Fully disabled two-hand scaling as it modifies the world Y-axis
            return;

            var leftTracked = ((Component)VR.Mode.Left).GetComponent<SteamVR_TrackedObject>();
            var rightTracked = ((Component)VR.Mode.Right).GetComponent<SteamVR_TrackedObject>();
            if (leftTracked == null || rightTracked == null) return;
            if (leftTracked.index == SteamVR_TrackedObject.EIndex.None ||
                rightTracked.index == SteamVR_TrackedObject.EIndex.None) return;

            var leftDevice = SteamVR_Controller.Input((int)leftTracked.index);
            var rightDevice = SteamVR_Controller.Input((int)rightTracked.index);
            if (leftDevice == null || rightDevice == null) return;

            bool bothGrip = leftDevice.GetPress(EVRButtonId.k_EButton_Grip)
                         && rightDevice.GetPress(EVRButtonId.k_EButton_Grip);

            Transform origin = VR.Camera.SteamCam.origin;
            if (origin == null) return;

            Vector3 leftPos = ((Component)VR.Mode.Left).transform.position;
            Vector3 rightPos = ((Component)VR.Mode.Right).transform.position;

            if (bothGrip)
            {
                // 关键：用 scale-invariant 距离避免振荡
                // 世界空间距离会随 origin.localScale 变化，必须除以当前 scale
                float worldDist = Vector3.Distance(leftPos, rightPos);
                float unscaledDist = worldDist / origin.localScale.x;

                if (!_isScaling)
                {
                    _isScaling = true;
                    _initialDistance = unscaledDist;
                    _initialScale = origin.localScale;

                    // 触觉反馈通知用户进入缩放模式
                    leftDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
                    rightDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
                }
                else if (_initialDistance > 0.01f)
                {
                    // 双手拉开 → ratio > 1 → scale 增大 → 用户变大（世界变小）
                    float ratio = unscaledDist / _initialDistance;
                    float newMagnitude = Mathf.Clamp(_initialScale.x * ratio, 0.1f, 10f);

                    // 以双手中点为缩放中心，保持中点世界坐标不变
                    Vector3 midpoint = (leftPos + rightPos) * 0.5f;
                    Vector3 originToMid = midpoint - origin.position;
                    float scaleChange = newMagnitude / origin.localScale.x;

                    origin.localScale = Vector3.one * newMagnitude;
                    origin.position = midpoint - originToMid * scaleChange;
                }
            }
            else
            {
                _isScaling = false;
            }
        }
    }
}

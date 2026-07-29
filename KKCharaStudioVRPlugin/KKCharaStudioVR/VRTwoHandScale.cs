using UnityEngine;
using VRGIN.Core;
using Valve.VR;

namespace KKCharaStudioVR
{
    public class VRTwoHandScale : MonoBehaviour
    {
        public static VRTwoHandScale Instance { get; private set; }

        public bool IsScaling
        {
            get { return _isScaling; }
        }

        public bool ShouldSuppressWorldMove
        {
            get
            {
                return !VRWristMenuController.IsOpen
                    && isActiveAndEnabled
                    && IsFeatureEnabled
                    && AreBothGripsPressed();
            }
        }

        private float _initialDistance;
        private Vector3 _initialScale;
        private bool _isScaling;
        private KKCharaStudioVRSettings _settings;

        private bool IsFeatureEnabled
        {
            get { return _settings == null || _settings.TwoHandScaleEnabled; }
        }

        void Start()
        {
            Instance = this;
            if (VR.Manager != null && VR.Manager.Context != null)
                _settings = VR.Manager.Context.Settings as KKCharaStudioVRSettings;
        }

        void Update()
        {
            if (!IsFeatureEnabled || VRWristMenuController.IsOpen)
            {
                ResetScaling();
                return;
            }

            SteamVR_Controller.Device leftDevice;
            SteamVR_Controller.Device rightDevice;
            if (!TryGetControllers(out leftDevice, out rightDevice))
            {
                ResetScaling();
                return;
            }

            bool bothGrip = leftDevice.GetPress(EVRButtonId.k_EButton_Grip)
                         && rightDevice.GetPress(EVRButtonId.k_EButton_Grip);
            if (!bothGrip || GripMoveKKCharaStudioTool.AnyObjectInteractionActive)
            {
                ResetScaling();
                return;
            }

            if (VR.Camera == null || VR.Camera.SteamCam == null)
            {
                ResetScaling();
                return;
            }

            Transform origin = VR.Camera.SteamCam.origin;
            if (origin == null || Mathf.Abs(origin.localScale.x) < 0.0001f)
            {
                ResetScaling();
                return;
            }

            Vector3 leftPos = ((Component)VR.Mode.Left).transform.position;
            Vector3 rightPos = ((Component)VR.Mode.Right).transform.position;
            float worldDist = Vector3.Distance(leftPos, rightPos);
            float unscaledDist = worldDist / Mathf.Abs(origin.localScale.x);

            if (!_isScaling)
            {
                if (unscaledDist <= 0.01f) return;
                _isScaling = true;
                _initialDistance = unscaledDist;
                _initialScale = origin.localScale;
                leftDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
                rightDevice.TriggerHapticPulse(500, EVRButtonId.k_EButton_Axis0);
                return;
            }

            if (_initialDistance <= 0.01f) return;

            float ratio = unscaledDist / _initialDistance;
            float newMagnitude = Mathf.Clamp(Mathf.Abs(_initialScale.x) * ratio, 0.1f, 10f);
            Vector3 midpoint = (leftPos + rightPos) * 0.5f;
            Vector3 originToMid = midpoint - origin.position;
            float scaleChange = newMagnitude / Mathf.Abs(origin.localScale.x);

            origin.localScale = Vector3.one * newMagnitude;
            Vector3 newPos = midpoint - originToMid * scaleChange;
            newPos.y = origin.position.y;
            origin.position = newPos;
        }

        private bool TryGetControllers(out SteamVR_Controller.Device leftDevice, out SteamVR_Controller.Device rightDevice)
        {
            leftDevice = null;
            rightDevice = null;
            if (VR.Mode == null || VR.Mode.Left == null || VR.Mode.Right == null) return false;

            SteamVR_TrackedObject leftTracked = ((Component)VR.Mode.Left).GetComponent<SteamVR_TrackedObject>();
            SteamVR_TrackedObject rightTracked = ((Component)VR.Mode.Right).GetComponent<SteamVR_TrackedObject>();
            if (leftTracked == null || rightTracked == null) return false;
            if (leftTracked.index == SteamVR_TrackedObject.EIndex.None ||
                rightTracked.index == SteamVR_TrackedObject.EIndex.None) return false;

            leftDevice = SteamVR_Controller.Input((int)leftTracked.index);
            rightDevice = SteamVR_Controller.Input((int)rightTracked.index);
            return leftDevice != null && rightDevice != null;
        }

        private bool AreBothGripsPressed()
        {
            SteamVR_Controller.Device leftDevice;
            SteamVR_Controller.Device rightDevice;
            return TryGetControllers(out leftDevice, out rightDevice)
                && leftDevice.GetPress(EVRButtonId.k_EButton_Grip)
                && rightDevice.GetPress(EVRButtonId.k_EButton_Grip);
        }

        private void ResetScaling()
        {
            _isScaling = false;
            _initialDistance = 0f;
        }

        void OnDisable()
        {
            ResetScaling();
        }

        void OnDestroy()
        {
            ResetScaling();
            if (Instance == this) Instance = null;
        }
    }
}

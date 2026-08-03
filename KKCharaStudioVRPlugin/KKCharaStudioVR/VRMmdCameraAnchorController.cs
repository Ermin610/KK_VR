using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

/// <summary>
/// MMDD's legacy VR path recentres the current eye on every camera evaluation.
/// Capture one neutral room-scale offset at playback start and restore it after
/// MMDD has authored the frame, so leaning or stepping does not become a new
/// camera centre while Fixed FOV changes the composition distance.
/// </summary>
[DefaultExecutionOrder(31900)]
internal sealed class VRMmdCameraAnchorController : MonoBehaviour
{
    private const float ReporterStaleSeconds = 1f;
    private const float MaxLocalOffset = 5f;
    private const float AppliedRotationMatchDegrees = 0.05f;

    private static VRMmdCameraAnchorController _instance;

    private bool _anchorValid;
    private Vector3 _neutralHeadOriginLocal;
    private int _playbackGeneration = -1;
    private bool _preparedNeutralValid;
    private Vector3 _preparedNeutralHeadOriginLocal;
    private Vector3 _preparedOriginPosition;
    private Quaternion _preparedOriginRotation = Quaternion.identity;
    private bool _presentationYawApplied;
    private Quaternion _presentationYawBaseRotation = Quaternion.identity;
    private Quaternion _presentationYawAppliedRotation = Quaternion.identity;

    private void Awake()
    {
        _instance = this;
    }

    private void LateUpdate()
    {
        bool reporterFresh = VRMmddStateBridge.PlaybackReported
            && Time.realtimeSinceStartup - VRMmddStateBridge.PlaybackReportRealtime
                <= ReporterStaleSeconds;
        bool mmddOwnsVrCamera = reporterFresh
            && VRMmddStateBridge.PlaybackAvailable
            && VRMmddStateBridge.PlaybackIsPlaying
            && VRMmddStateBridge.DirectVrCameraOwner;

        // A followed Timeline camera is the single final writer when both
        // systems happen to play together.
        if (VRTimelineCameraFollowController.IsTimelineCameraWriterActive)
        {
            ClearAnchor();
            ClearPresentationYawTracking();
            return;
        }

        if (!mmddOwnsVrCamera)
        {
            ClearAnchor();
            if (VRMmdPlaybackController.IsPresentationCameraControlActive)
                ApplyPresentationYawToCurrentRig();
            return;
        }

        CorrectCurrentEvaluation();
    }

    private void OnDisable()
    {
        ClearAnchor();
        RestorePresentationYawIfStillApplied();
    }

    private void OnDestroy()
    {
        ClearAnchor();
        RestorePresentationYawIfStillApplied();
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Captures the tracked head offset before an operation that can synchronously
    /// evaluate MMDD's legacy VR camera. Capturing after mmdd.update(True) is too
    /// late: at that point MMDD has already made the current eye its new centre.
    /// </summary>
    internal static bool PrepareForSynchronousCameraEvaluation()
    {
        if (_instance == null
            || !VRMmddStateBridge.PlaybackReported
            || !VRMmddStateBridge.PlaybackAvailable
            || !VRMmddStateBridge.DirectVrCameraOwner
            || Time.realtimeSinceStartup - VRMmddStateBridge.PlaybackReportRealtime
                > ReporterStaleSeconds
            || VRTimelineCameraFollowController.IsTimelineCameraWriterActive)
        {
            return false;
        }

        return _instance.CapturePreparedNeutral();
    }

    /// <summary>
    /// Package loading can create the first direct VR camera, so there may be no
    /// prior ownership report to gate the pre-evaluation snapshot. The caller
    /// must refresh ownership after execution before applying the correction.
    /// </summary>
    internal static bool PrepareForPotentialDirectCameraEvaluation()
    {
        if (_instance == null
            || VRTimelineCameraFollowController.IsTimelineCameraWriterActive)
        {
            return false;
        }

        return _instance.CapturePreparedNeutral();
    }

    /// <summary>
    /// Fixed-FOV edits while MMDD is paused synchronously evaluate one camera
    /// frame. Correct that one legacy recenter immediately; LateUpdate will not
    /// run the continuous path because playback is already reported as paused.
    /// </summary>
    internal static void CorrectAfterSynchronousCameraEvaluation()
    {
        if (_instance == null
            || VRTimelineCameraFollowController.IsTimelineCameraWriterActive)
        {
            CancelPreparedSynchronousCameraEvaluation();
            return;
        }

        if (!VRMmddStateBridge.DirectVrCameraOwner)
        {
            CancelPreparedSynchronousCameraEvaluation();
            return;
        }

        _instance.CorrectCurrentEvaluation();
    }

    internal static void CancelPreparedSynchronousCameraEvaluation()
    {
        if (_instance != null)
            _instance.RestoreAndClearPreparedOrigin();
    }

    internal static void ResetForSceneTransition()
    {
        VRMmdCameraAnchorController[] controllers =
            Object.FindObjectsOfType<VRMmdCameraAnchorController>();
        foreach (VRMmdCameraAnchorController controller in controllers)
        {
            controller.ClearAnchor();
            controller.ClearPresentationYawTracking();
        }
    }

    internal static void RefreshPresentationYawNow()
    {
        if (_instance != null)
            _instance.ApplyPresentationYawToCurrentRig();
    }

    private void ClearAnchor()
    {
        _anchorValid = false;
        _neutralHeadOriginLocal = Vector3.zero;
        _playbackGeneration = -1;
        _preparedNeutralValid = false;
        _preparedNeutralHeadOriginLocal = Vector3.zero;
        _preparedOriginPosition = Vector3.zero;
        _preparedOriginRotation = Quaternion.identity;
    }

    private bool CapturePreparedNeutral()
    {
        Transform origin;
        Transform head;
        if (!TryGetVrRig(out origin, out head))
        {
            _preparedNeutralValid = false;
            return false;
        }

        Vector3 local = origin.InverseTransformPoint(head.position);
        if (!IsFinite(local) || local.sqrMagnitude > MaxLocalOffset * MaxLocalOffset)
        {
            _preparedNeutralValid = false;
            return false;
        }

        _preparedNeutralHeadOriginLocal = local;
        _preparedOriginPosition = origin.position;
        _preparedOriginRotation = origin.rotation;
        _preparedNeutralValid = true;
        return true;
    }

    private void RestoreAndClearPreparedOrigin()
    {
        if (!_preparedNeutralValid)
            return;

        Transform origin;
        Transform head;
        if (TryGetVrRig(out origin, out head)
            && IsFinite(_preparedOriginPosition)
            && IsFinite(_preparedOriginRotation))
        {
            origin.rotation = _preparedOriginRotation;
            origin.position = _preparedOriginPosition;
        }

        _preparedNeutralValid = false;
        _preparedNeutralHeadOriginLocal = Vector3.zero;
        _preparedOriginPosition = Vector3.zero;
        _preparedOriginRotation = Quaternion.identity;
    }

    private void CorrectCurrentEvaluation()
    {
        Transform origin;
        Transform head;
        if (!TryGetVrRig(out origin, out head))
        {
            ClearAnchor();
            return;
        }

        int generation = VRMmddStateBridge.PlaybackGeneration;
        if (_preparedNeutralValid)
        {
            _neutralHeadOriginLocal = _preparedNeutralHeadOriginLocal;
            _playbackGeneration = generation;
            _anchorValid = true;
            _preparedNeutralValid = false;
            _preparedOriginPosition = Vector3.zero;
            _preparedOriginRotation = Quaternion.identity;
        }
        else if (!_anchorValid || _playbackGeneration != generation)
        {
            Vector3 local = origin.InverseTransformPoint(head.position);
            if (!IsFinite(local) || local.sqrMagnitude > MaxLocalOffset * MaxLocalOffset)
            {
                ClearAnchor();
                return;
            }

            _neutralHeadOriginLocal = local;
            _playbackGeneration = generation;
            _anchorValid = true;
            VRLog.Info("MMDD VR camera captured a neutral head-position anchor.");
        }

        // After MMDD's legacy correction, head.position is the authored camera
        // position. Rebuild the origin using the captured neutral offset rather
        // than the current tracking offset, preserving subsequent physical motion.
        Vector3 authoredCameraPosition = head.position;
        Vector3 neutralWorldOffset = origin.TransformVector(_neutralHeadOriginLocal);
        Vector3 nextOriginPosition = authoredCameraPosition - neutralWorldOffset;
        if (IsFinite(nextOriginPosition))
        {
            origin.position = nextOriginPosition;
            if (VRMmdPlaybackController.IsPresentationCameraControlActive)
                ApplyPresentationYaw(origin, head);
        }
        else
            ClearAnchor();
    }

    private void ApplyPresentationYawToCurrentRig()
    {
        Transform origin;
        Transform head;
        if (!TryGetVrRig(out origin, out head))
            return;
        ApplyPresentationYaw(origin, head);
    }

    private void ApplyPresentationYaw(Transform origin, Transform head)
    {
        Quaternion currentRotation = origin.rotation;
        if (!IsFinite(currentRotation))
        {
            ClearPresentationYawTracking();
            return;
        }

        Quaternion baseRotation = _presentationYawApplied
            && Quaternion.Angle(
                currentRotation,
                _presentationYawAppliedRotation) <= AppliedRotationMatchDegrees
            ? _presentationYawBaseRotation
            : currentRotation;
        float yawOffset = VRMmdPlaybackController.PresentationYawOffset;
        Quaternion targetRotation =
            Quaternion.AngleAxis(yawOffset, Vector3.up) * baseRotation;
        if (!IsFinite(baseRotation) || !IsFinite(targetRotation))
        {
            ClearPresentationYawTracking();
            return;
        }

        // Rotate around the tracked head rather than the origin. This keeps the
        // current eye position fixed and avoids the centre-point drift caused by
        // rotating a room-scale origin around its own pivot.
        Vector3 headPosition = head.position;
        origin.rotation = targetRotation;
        Vector3 positionCorrection = headPosition - head.position;
        if (IsFinite(positionCorrection))
            origin.position += positionCorrection;

        _presentationYawBaseRotation = baseRotation;
        _presentationYawAppliedRotation = targetRotation;
        _presentationYawApplied = true;
    }

    private void ClearPresentationYawTracking()
    {
        _presentationYawApplied = false;
        _presentationYawBaseRotation = Quaternion.identity;
        _presentationYawAppliedRotation = Quaternion.identity;
    }

    private void RestorePresentationYawIfStillApplied()
    {
        if (!_presentationYawApplied)
            return;

        Transform origin;
        Transform head;
        if (TryGetVrRig(out origin, out head)
            && IsFinite(origin.rotation)
            && IsFinite(_presentationYawBaseRotation)
            && Quaternion.Angle(
                origin.rotation,
                _presentationYawAppliedRotation) <= AppliedRotationMatchDegrees)
        {
            Vector3 headPosition = head.position;
            origin.rotation = _presentationYawBaseRotation;
            Vector3 positionCorrection = headPosition - head.position;
            if (IsFinite(positionCorrection))
                origin.position += positionCorrection;
        }

        ClearPresentationYawTracking();
    }

    private static bool TryGetVrRig(out Transform origin, out Transform head)
    {
        origin = null;
        head = null;
        if (!VR.Active || VR.Camera == null)
            return false;

        origin = VR.Camera.Origin;
        head = VR.Camera.Head;
        return origin != null && head != null;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    private static bool IsFinite(Quaternion value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y)
            && !float.IsNaN(value.z) && !float.IsInfinity(value.z)
            && !float.IsNaN(value.w) && !float.IsInfinity(value.w);
    }
}

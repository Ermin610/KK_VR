using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;

namespace Leap.Unity;

public class PinchDetector : Detector
{
	protected const float MM_TO_M = 0.001f;

	[SerializeField]
	public IHandModel _handModel;

	[SerializeField]
	protected float _activatePinchDist = 0.03f;

	[SerializeField]
	protected float _deactivatePinchDist = 0.04f;

	protected int _lastUpdateFrame = -1;

	protected bool _isPinching;

	protected bool _didChange;

	protected float _lastPinchTime;

	protected float _lastUnpinchTime;

	protected Vector3 _pinchPos;

	protected Quaternion _pinchRotation;

	public bool IsPinching
	{
		get
		{
			ensurePinchInfoUpToDate();
			return _isPinching;
		}
	}

	public bool DidChangeFromLastFrame
	{
		get
		{
			ensurePinchInfoUpToDate();
			return _didChange;
		}
	}

	public bool DidStartPinch
	{
		get
		{
			ensurePinchInfoUpToDate();
			if (DidChangeFromLastFrame)
			{
				return IsPinching;
			}
			return false;
		}
	}

	public bool DidEndPinch
	{
		get
		{
			ensurePinchInfoUpToDate();
			if (DidChangeFromLastFrame)
			{
				return !IsPinching;
			}
			return false;
		}
	}

	public float LastPinchTime
	{
		get
		{
			ensurePinchInfoUpToDate();
			return _lastPinchTime;
		}
	}

	public float LastUnpinchTime
	{
		get
		{
			ensurePinchInfoUpToDate();
			return _lastUnpinchTime;
		}
	}

	public Vector3 Position
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			ensurePinchInfoUpToDate();
			return _pinchPos;
		}
	}

	public Quaternion Rotation
	{
		get
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			ensurePinchInfoUpToDate();
			return _pinchRotation;
		}
	}

	protected virtual void OnValidate()
	{
		if ((Object)(object)_handModel == (Object)null)
		{
			_handModel = ((Component)this).GetComponentInParent<IHandModel>();
		}
		_activatePinchDist = Mathf.Max(0f, _activatePinchDist);
		_deactivatePinchDist = Mathf.Max(0f, _deactivatePinchDist);
		if (_activatePinchDist > _deactivatePinchDist)
		{
			_deactivatePinchDist = _activatePinchDist;
		}
	}

	protected virtual void Start()
	{
		if ((Object)(object)((Component)this).GetComponent<IHandModel>() != (Object)null)
		{
			VRLog.Warn("LeapPinchDetector should not be attached to the IHandModel's transform. It should be attached to its own transform.");
		}
		if ((Object)(object)_handModel == (Object)null)
		{
			VRLog.Warn("The HandModel field of LeapPinchDetector was unassigned and the detector has been disabled.");
			((Behaviour)this).enabled = false;
		}
	}

	protected virtual void Update()
	{
		ensurePinchInfoUpToDate();
	}

	protected virtual void ensurePinchInfoUpToDate()
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		if (Time.frameCount == _lastUpdateFrame)
		{
			return;
		}
		_lastUpdateFrame = Time.frameCount;
		_didChange = false;
		Hand leapHand = _handModel.GetLeapHand();
		if (leapHand == null || !_handModel.IsTracked)
		{
			changePinchState(shouldBePinching: false);
			return;
		}
		float num = leapHand.PinchDistance * 0.001f;
		((Component)this).transform.rotation = leapHand.Basis.CalculateRotation();
		List<Finger> fingers = leapHand.Fingers;
		((Component)this).transform.position = Vector3.zero;
		for (int i = 0; i < fingers.Count; i++)
		{
			Finger finger = fingers[i];
			if (finger.Type == Finger.FingerType.TYPE_INDEX || finger.Type == Finger.FingerType.TYPE_THUMB)
			{
				Transform transform = ((Component)this).transform;
				transform.position += finger.Bone(Bone.BoneType.TYPE_DISTAL).NextJoint.ToVector3();
			}
		}
		Transform transform2 = ((Component)this).transform;
		transform2.position /= 2f;
		if (_isPinching)
		{
			if (num > _deactivatePinchDist)
			{
				changePinchState(shouldBePinching: false);
				return;
			}
		}
		else if (num < _activatePinchDist)
		{
			changePinchState(shouldBePinching: true);
		}
		if (_isPinching)
		{
			_pinchPos = ((Component)this).transform.position;
			_pinchRotation = ((Component)this).transform.rotation;
		}
	}

	protected virtual void changePinchState(bool shouldBePinching)
	{
		if (_isPinching != shouldBePinching)
		{
			_isPinching = shouldBePinching;
			if (_isPinching)
			{
				_lastPinchTime = Time.time;
				Activate();
			}
			else
			{
				_lastUnpinchTime = Time.time;
				Deactivate();
			}
			_didChange = true;
		}
	}
}

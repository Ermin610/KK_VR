using System.Collections;
using UnityEngine;

namespace Leap.Unity;

public class LeapServiceProvider : LeapProvider
{
	protected const float NS_TO_S = 1E-06f;

	protected const float S_TO_NS = 1000000f;

	protected const float FIXED_UPDATE_OFFSET_SMOOTHING_DELAY = 0.1f;

	[Tooltip("Set true if the Leap Motion hardware is mounted on an HMD; otherwise, leave false.")]
	[SerializeField]
	public bool _isHeadMounted;

	[Header("Device Type")]
	[SerializeField]
	protected bool _overrideDeviceType;

	[Tooltip("If overrideDeviceType is enabled, the hand controller will return a device of this type.")]
	[SerializeField]
	protected LeapDeviceType _overrideDeviceTypeWith = LeapDeviceType.Peripheral;

	[Header("Interpolation")]
	[Tooltip("Interpolate frames to deliver a potentially smoother experience.  Currently experimental.")]
	[SerializeField]
	protected bool _useInterpolation;

	[Tooltip("How much delay should be added to interpolation.  A non-zero amount is needed to prevent extrapolation artifacts.")]
	[SerializeField]
	protected long _interpolationDelay = 15L;

	protected Controller leap_controller_;

	protected SmoothedFloat _fixedOffset = new SmoothedFloat();

	protected Frame _untransformedUpdateFrame;

	protected Frame _transformedUpdateFrame;

	protected Image _currentImage;

	protected int _currentUpdateCount = -1;

	protected Frame _untransformedFixedFrame;

	protected Frame _transformedFixedFrame;

	protected float _currentFixedTime = -1f;

	private ClockCorrelator clockCorrelator;

	public override Frame CurrentFrame
	{
		get
		{
			updateIfTransformMoved(_untransformedUpdateFrame, ref _transformedUpdateFrame);
			return _transformedUpdateFrame;
		}
	}

	public override Image CurrentImage => _currentImage;

	public override Frame CurrentFixedFrame
	{
		get
		{
			updateIfTransformMoved(_untransformedFixedFrame, ref _transformedFixedFrame);
			return _transformedFixedFrame;
		}
	}

	public bool UseInterpolation
	{
		get
		{
			return _useInterpolation;
		}
		set
		{
			_useInterpolation = value;
		}
	}

	public long InterpolationDelay
	{
		get
		{
			return _interpolationDelay;
		}
		set
		{
			_interpolationDelay = value;
		}
	}

	public Controller GetLeapController()
	{
		return leap_controller_;
	}

	public bool IsConnected()
	{
		return GetLeapController().IsConnected;
	}

	public LeapDeviceInfo GetDeviceInfo()
	{
		if (_overrideDeviceType)
		{
			return new LeapDeviceInfo(_overrideDeviceTypeWith);
		}
		DeviceList devices = GetLeapController().Devices;
		if (devices.Count == 1)
		{
			LeapDeviceInfo result = new LeapDeviceInfo(LeapDeviceType.Peripheral);
			if (devices[0].SerialNumber.Length >= 2)
			{
				string text = devices[0].SerialNumber.Substring(0, 2);
				if (!(text == "LP"))
				{
					if (text == "LE")
					{
						result = new LeapDeviceInfo(LeapDeviceType.Dragonfly);
					}
				}
				else
				{
					result = new LeapDeviceInfo(LeapDeviceType.Peripheral);
				}
			}
			result.isEmbedded = devices[0].IsEmbedded;
			result.horizontalViewAngle = devices[0].HorizontalViewAngle * 57.29578f;
			result.verticalViewAngle = devices[0].VerticalViewAngle * 57.29578f;
			result.trackingRange = devices[0].Range / 1000f;
			result.serialID = devices[0].SerialNumber;
			return result;
		}
		if (devices.Count > 1)
		{
			return new LeapDeviceInfo(LeapDeviceType.Peripheral);
		}
		return new LeapDeviceInfo(LeapDeviceType.Invalid);
	}

	protected virtual void Awake()
	{
		clockCorrelator = new ClockCorrelator();
		_fixedOffset.delay = 0.4f;
	}

	protected virtual void Start()
	{
		createController();
		_untransformedUpdateFrame = new Frame();
		_untransformedFixedFrame = new Frame();
		((MonoBehaviour)this).StartCoroutine(waitCoroutine());
	}

	protected IEnumerator waitCoroutine()
	{
		WaitForEndOfFrame endWaiter = new WaitForEndOfFrame();
		while (true)
		{
			yield return endWaiter;
			long applicationClock = (long)((double)Time.time * 1000000.0);
			clockCorrelator.UpdateRebaseEstimate(applicationClock);
		}
	}

	protected virtual void Update()
	{
		_fixedOffset.Update(Time.time - Time.fixedTime, Time.deltaTime);
		if (_useInterpolation)
		{
			long applicationClock = (long)((double)Time.time * 1000000.0) - _interpolationDelay * 1000;
			long time = clockCorrelator.ExternalClockToLeapTime(applicationClock);
			_untransformedUpdateFrame = leap_controller_.GetInterpolatedFrame(time) ?? _untransformedUpdateFrame;
		}
		else
		{
			_untransformedUpdateFrame = leap_controller_.Frame();
		}
		_transformedUpdateFrame = null;
	}

	protected virtual void FixedUpdate()
	{
		if (_useInterpolation)
		{
			long applicationClock = (long)((double)(Time.fixedTime + _fixedOffset.value) * 1000000.0) - _interpolationDelay * 1000;
			long time = clockCorrelator.ExternalClockToLeapTime(applicationClock);
			_untransformedFixedFrame = leap_controller_.GetInterpolatedFrame(time) ?? _untransformedFixedFrame;
		}
		else
		{
			_untransformedFixedFrame = leap_controller_.Frame();
		}
		_transformedFixedFrame = null;
	}

	protected virtual void OnDestroy()
	{
		destroyController();
	}

	protected virtual void OnApplicationPause(bool isPaused)
	{
		if (leap_controller_ != null)
		{
			if (isPaused)
			{
				leap_controller_.StopConnection();
			}
			else
			{
				leap_controller_.StartConnection();
			}
		}
	}

	protected virtual void OnApplicationQuit()
	{
		destroyController();
	}

	protected void initializeFlags()
	{
		if (leap_controller_ != null)
		{
			if (_isHeadMounted)
			{
				leap_controller_.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
			}
			else
			{
				leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
			}
		}
	}

	protected void createController()
	{
		if (leap_controller_ != null)
		{
			destroyController();
		}
		leap_controller_ = new Controller();
		if (leap_controller_.IsConnected)
		{
			initializeFlags();
		}
		else
		{
			leap_controller_.Device += onHandControllerConnect;
		}
	}

	protected void destroyController()
	{
		if (leap_controller_ != null)
		{
			if (leap_controller_.IsConnected)
			{
				leap_controller_.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
			}
			leap_controller_.StopConnection();
			leap_controller_ = null;
		}
	}

	protected void onHandControllerConnect(object sender, LeapEventArgs args)
	{
		initializeFlags();
		leap_controller_.Device -= onHandControllerConnect;
	}

	protected void updateIfTransformMoved(Frame source, ref Frame toUpdate)
	{
		if (((Component)this).transform.hasChanged)
		{
			_transformedFixedFrame = null;
			_transformedUpdateFrame = null;
			((Component)this).transform.hasChanged = false;
		}
		if (toUpdate == null)
		{
			LeapTransform leapMatrix = ((Component)this).transform.GetLeapMatrix();
			toUpdate = source.TransformedCopy(leapMatrix);
		}
	}
}

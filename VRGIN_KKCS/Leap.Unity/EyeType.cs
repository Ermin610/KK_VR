using System;
using UnityEngine;

namespace Leap.Unity;

[Serializable]
public class EyeType
{
	public enum OrderType
	{
		LEFT = 1,
		RIGHT,
		CENTER
	}

	private const string TARGET_EYE_PROPERTY_NAME = "m_TargetEye";

	private const int TARGET_EYE_LEFT_INDEX = 1;

	private const int TARGET_EYE_RIGHT_INDEX = 2;

	private const int TARGET_EYE_CENTER_INDEX = 3;

	[SerializeField]
	private OrderType _orderType = OrderType.LEFT;

	private bool _isOnFirst;

	private bool _hasBegun;

	public OrderType Type => _orderType;

	public bool IsLeftEye
	{
		get
		{
			if (!_hasBegun)
			{
				throw new Exception("Cannot call IsLeftEye or IsRightEye before BeginCamera has been called!");
			}
			return _orderType switch
			{
				OrderType.LEFT => true, 
				OrderType.RIGHT => false, 
				OrderType.CENTER => _isOnFirst, 
				_ => throw new Exception("Unexpected order type " + _orderType), 
			};
		}
	}

	public bool IsRightEye => !IsLeftEye;

	public EyeType(OrderType type)
	{
		_orderType = type;
	}

	public void BeginCamera()
	{
		if (!_hasBegun)
		{
			_isOnFirst = true;
			_hasBegun = true;
		}
		else
		{
			_isOnFirst = !_isOnFirst;
		}
	}

	public void Reset()
	{
		_hasBegun = false;
	}
}

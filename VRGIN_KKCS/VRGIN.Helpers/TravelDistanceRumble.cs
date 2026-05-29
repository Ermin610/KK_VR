using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VRGIN.Helpers;

public class TravelDistanceRumble : IRumbleSession, IComparable<IRumbleSession>
{
	private Transform _Transform;

	private float _Distance;

	protected Vector3 PrevPosition;

	protected Vector3 CurrentPosition;

	private bool _UseLocalPosition;

	public bool UseLocalPosition
	{
		get
		{
			return _UseLocalPosition;
		}
		set
		{
			_UseLocalPosition = value;
			Reset();
		}
	}

	public bool IsOver { get; private set; }

	public ushort MicroDuration { get; set; }

	public float MilliInterval
	{
		get
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			CurrentPosition = (_UseLocalPosition ? _Transform.localPosition : _Transform.position);
			if (DistanceTraveled > _Distance)
			{
				PrevPosition = CurrentPosition;
				return 0f;
			}
			return float.MaxValue;
		}
	}

	protected virtual float DistanceTraveled => Vector3.Distance(PrevPosition, CurrentPosition);

	public void Reset()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		PrevPosition = (_UseLocalPosition ? _Transform.localPosition : _Transform.position);
	}

	public TravelDistanceRumble(ushort intensity, float distance, Transform transform)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		MicroDuration = intensity;
		_Transform = transform;
		_Distance = distance;
		PrevPosition = transform.position;
	}

	public int CompareTo(IRumbleSession other)
	{
		return MicroDuration.CompareTo(other.MicroDuration);
	}

	public void Consume()
	{
	}

	public void Close()
	{
		IsOver = true;
	}
}

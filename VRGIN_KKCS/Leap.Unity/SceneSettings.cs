using System;
using UnityEngine;

namespace Leap.Unity;

public class SceneSettings : MonoBehaviour
{
	public class ToggleValue<T>
	{
		public bool Override;

		public T Value;
	}

	[Serializable]
	public class ToggleFloat : ToggleValue<float>
	{
	}

	[Serializable]
	public class ToggleVector3 : ToggleValue<Vector3>
	{
	}

	[SerializeField]
	private ToggleFloat _shadowDistance = new ToggleFloat();

	[SerializeField]
	private ToggleVector3 _gravity = new ToggleVector3();

	private void Reset()
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		_shadowDistance.Value = QualitySettings.shadowDistance;
		_gravity.Value = Physics.gravity;
	}

	private void Awake()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (_shadowDistance.Override)
		{
			QualitySettings.shadowDistance = _shadowDistance.Value;
		}
		if (_gravity.Override)
		{
			Physics.gravity = _gravity.Value;
		}
	}
}

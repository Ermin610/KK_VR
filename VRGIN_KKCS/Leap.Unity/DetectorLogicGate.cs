using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity;

public class DetectorLogicGate : Detector
{
	[SerializeField]
	[Tooltip("The list of observed detectors.")]
	private List<Detector> Detectors = new List<Detector>();

	[Tooltip("Add all detectors on this object automatically.")]
	public bool AddAllSiblingDetectorsOnAwake = true;

	[Tooltip("The type of logic used to combine detector state.")]
	public LogicType GateType;

	[Tooltip("Whether to negate the gate output.")]
	public bool Negate;

	public void AddDetector(Detector detector)
	{
		if (!Detectors.Contains(detector))
		{
			Detectors.Add(detector);
			activateDetector(detector);
		}
	}

	public void RemoveDetector(Detector detector)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		detector.OnActivate.RemoveListener(new UnityAction(CheckDetectors));
		detector.OnDeactivate.RemoveListener(new UnityAction(CheckDetectors));
		Detectors.Remove(detector);
	}

	public void AddAllSiblingDetectors()
	{
		Detector[] components = ((Component)this).GetComponents<Detector>();
		for (int i = 0; i < components.Length; i++)
		{
			if ((Object)(object)components[i] != (Object)(object)this && ((Behaviour)components[i]).enabled)
			{
				AddDetector(components[i]);
			}
		}
	}

	private void Awake()
	{
		for (int i = 0; i < Detectors.Count; i++)
		{
			activateDetector(Detectors[i]);
		}
		if (AddAllSiblingDetectorsOnAwake)
		{
			AddAllSiblingDetectors();
		}
	}

	private void activateDetector(Detector detector)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		detector.OnActivate.RemoveListener(new UnityAction(CheckDetectors));
		detector.OnDeactivate.RemoveListener(new UnityAction(CheckDetectors));
		detector.OnActivate.AddListener(new UnityAction(CheckDetectors));
		detector.OnDeactivate.AddListener(new UnityAction(CheckDetectors));
	}

	private void OnDisable()
	{
		Deactivate();
	}

	protected void CheckDetectors()
	{
		if (Detectors.Count >= 1)
		{
			bool flag = Detectors[0].IsActive;
			for (int i = 1; i < Detectors.Count; i++)
			{
				flag = ((GateType != 0) ? (flag || Detectors[i].IsActive) : (flag && Detectors[i].IsActive));
			}
			if (Negate)
			{
				flag = !flag;
			}
			if (flag)
			{
				Activate();
			}
			else
			{
				Deactivate();
			}
		}
	}
}

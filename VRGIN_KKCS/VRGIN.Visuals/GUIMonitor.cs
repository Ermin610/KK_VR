using System;
using System.ComponentModel;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Visuals;

public class GUIMonitor : GUIQuad
{
	public enum CurvinessState
	{
		Flat,
		Curved,
		Spherical
	}

	public CurvinessState TargetCurviness = VR.Settings.Projection;

	private float _Curviness = 1f;

	public float Angle;

	public float Distance;

	private ProceduralPlane _Plane;

	protected override void OnStart()
	{
		base.OnStart();
		_Plane = ((Component)this).GetComponent<ProceduralPlane>();
		_Plane.xSegments = 100;
		if (Object.op_Implicit((Object)(object)_Plane))
		{
			VRLog.Info("Plane was added...");
		}
		else
		{
			VRLog.Info("No plane either?");
		}
		UpdateGUI();
		Rebuild();
	}

	protected override void OnEnable()
	{
		base.OnEnable();
		VR.Settings.PropertyChanged += OnPropertyChanged;
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		VR.Settings.PropertyChanged -= OnPropertyChanged;
	}

	public static GUIMonitor Create()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		return ((Component)new GameObject("GUI Monitor").AddComponent<ProceduralPlane>()).gameObject.AddComponent<GUIMonitor>();
	}

	public override void UpdateAspect()
	{
	}

	private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
	{
		if (Object.op_Implicit((Object)(object)_Plane))
		{
			switch (e.PropertyName)
			{
			case "Angle":
			case "OffsetY":
			case "Distance":
			case "Rotation":
				Rebuild();
				break;
			case "Projection":
				TargetCurviness = VR.Settings.Projection;
				break;
			}
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (Mathf.Abs(_Curviness - (float)TargetCurviness) > float.Epsilon)
		{
			_Curviness = Mathf.MoveTowards(_Curviness, (float)TargetCurviness, Time.deltaTime * 5f);
			Rebuild();
		}
	}

	public void Rebuild()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		VRLog.Info("Build monitor");
		try
		{
			((Component)this).transform.localPosition = new Vector3(((Component)this).transform.localPosition.x, VR.Settings.OffsetY, ((Component)this).transform.localPosition.z);
			((Component)this).transform.localScale = Vector3.one * VR.Settings.Distance;
			((Component)this).transform.localRotation = Quaternion.Euler(0f, VR.Settings.Rotation, 0f);
			_Plane.angleSpan = VR.Settings.Angle;
			_Plane.curviness = _Curviness;
			_Plane.height = VR.Settings.Angle / 100f;
			_Plane.distance = 1f;
			_Plane.Rebuild();
		}
		catch (Exception obj)
		{
			VRLog.Error(obj);
		}
	}
}

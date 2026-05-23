using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity;

public class LeapHandController : MonoBehaviour
{
	protected LeapProvider provider;

	protected HandFactory factory;

	protected Dictionary<int, HandRepresentation> graphicsReps = new Dictionary<int, HandRepresentation>();

	protected Dictionary<int, HandRepresentation> physicsReps = new Dictionary<int, HandRepresentation>();

	protected const float GIZMO_SCALE = 5f;

	protected bool graphicsEnabled = true;

	protected bool physicsEnabled = true;

	public bool GraphicsEnabled
	{
		get
		{
			return graphicsEnabled;
		}
		set
		{
			graphicsEnabled = value;
		}
	}

	public bool PhysicsEnabled
	{
		get
		{
			return physicsEnabled;
		}
		set
		{
			physicsEnabled = value;
		}
	}

	private void OnDrawGizmos()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		Gizmos.matrix = Matrix4x4.Scale(5f * Vector3.one);
		Gizmos.DrawIcon(((Component)this).transform.position, "leap_motion.png");
	}

	protected virtual void Start()
	{
		provider = requireComponent<LeapProvider>();
		factory = requireComponent<HandFactory>();
	}

	protected virtual void Update()
	{
		Frame currentFrame = provider.CurrentFrame;
		if (currentFrame != null && graphicsEnabled)
		{
			UpdateHandRepresentations(graphicsReps, ModelType.Graphics, currentFrame);
		}
	}

	protected virtual void FixedUpdate()
	{
		Frame currentFixedFrame = provider.CurrentFixedFrame;
		if (currentFixedFrame != null && physicsEnabled)
		{
			UpdateHandRepresentations(physicsReps, ModelType.Physics, currentFixedFrame);
		}
	}

	private void UpdateHandRepresentations(Dictionary<int, HandRepresentation> all_hand_reps, ModelType modelType, Frame frame)
	{
		foreach (Hand hand in frame.Hands)
		{
			if (!all_hand_reps.TryGetValue(hand.Id, out var value))
			{
				value = factory.MakeHandRepresentation(hand, modelType);
				if (value != null)
				{
					all_hand_reps.Add(hand.Id, value);
				}
			}
			if (value != null)
			{
				value.IsMarked = true;
				value.UpdateRepresentation(hand);
				value.LastUpdatedTime = (int)frame.Timestamp;
			}
		}
		HandRepresentation handRepresentation = null;
		foreach (KeyValuePair<int, HandRepresentation> all_hand_rep in all_hand_reps)
		{
			if (all_hand_rep.Value != null)
			{
				if (all_hand_rep.Value.IsMarked)
				{
					all_hand_rep.Value.IsMarked = false;
				}
				else
				{
					handRepresentation = all_hand_rep.Value;
				}
			}
		}
		if (handRepresentation != null)
		{
			all_hand_reps.Remove(handRepresentation.HandID);
			handRepresentation.Finish();
		}
	}

	private T requireComponent<T>() where T : Component
	{
		T component = ((Component)this).GetComponent<T>();
		if ((Object)(object)component == (Object)null)
		{
			string name = typeof(T).Name;
			Debug.LogError((object)("LeapHandController could not find a " + name + " and has been disabled.  Make sure there is a " + name + " on the same gameObject."));
			((Behaviour)this).enabled = false;
		}
		return component;
	}
}

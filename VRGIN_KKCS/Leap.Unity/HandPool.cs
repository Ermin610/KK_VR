using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;

namespace Leap.Unity;

public class HandPool : HandFactory
{
	[Serializable]
	public class ModelGroup
	{
		public string GroupName;

		[HideInInspector]
		public HandPool _handPool;

		public IHandModel LeftModel;

		[HideInInspector]
		public bool IsLeftToBeSpawned;

		public IHandModel RightModel;

		[HideInInspector]
		public bool IsRightToBeSpawned;

		[HideInInspector]
		public List<IHandModel> modelList;

		[HideInInspector]
		public List<IHandModel> modelsCheckedOut;

		public bool IsEnabled = true;

		public bool CanDuplicate;

		public IHandModel TryGetModel(Chirality chirality, ModelType modelType)
		{
			for (int i = 0; i < modelList.Count; i++)
			{
				if ((modelList[i].HandModelType == modelType && modelList[i].Handedness == chirality) || modelList[i].Handedness == Chirality.Either)
				{
					IHandModel handModel = modelList[i];
					modelList.RemoveAt(i);
					modelsCheckedOut.Add(handModel);
					return handModel;
				}
			}
			if (CanDuplicate)
			{
				for (int j = 0; j < modelsCheckedOut.Count; j++)
				{
					if ((modelsCheckedOut[j].HandModelType == modelType && modelsCheckedOut[j].Handedness == chirality) || modelsCheckedOut[j].Handedness == Chirality.Either)
					{
						IHandModel handModel2 = Object.Instantiate<IHandModel>(modelsCheckedOut[j]);
						((Component)handModel2).transform.parent = ((Component)_handPool).transform;
						_handPool.modelGroupMapping.Add(handModel2, this);
						modelsCheckedOut.Add(handModel2);
						return handModel2;
					}
				}
			}
			return null;
		}

		public void ReturnToGroup(IHandModel model)
		{
			modelsCheckedOut.Remove(model);
			modelList.Add(model);
			_handPool.modelToHandRepMapping.Remove(model);
		}
	}

	[SerializeField]
	public List<ModelGroup> ModelPool;

	private List<HandRepresentation> activeHandReps = new List<HandRepresentation>();

	private Dictionary<IHandModel, ModelGroup> modelGroupMapping = new Dictionary<IHandModel, ModelGroup>();

	private Dictionary<IHandModel, HandRepresentation> modelToHandRepMapping = new Dictionary<IHandModel, HandRepresentation>();

	public void ReturnToPool(IHandModel model)
	{
		modelGroupMapping.TryGetValue(model, out var value);
		value.ReturnToGroup(model);
	}

	public void RemoveHandRepresentation(HandRepresentation handRep)
	{
		activeHandReps.Remove(handRep);
	}

	private void Start()
	{
		foreach (ModelGroup item in ModelPool)
		{
			item._handPool = this;
			IHandModel handModel;
			if (item.IsLeftToBeSpawned)
			{
				handModel = Object.Instantiate<GameObject>(((Component)item.LeftModel).gameObject).GetComponent<IHandModel>();
				((Component)handModel).transform.parent = ((Component)this).transform;
			}
			else
			{
				handModel = item.LeftModel;
			}
			item.modelList.Add(handModel);
			modelGroupMapping.Add(handModel, item);
			IHandModel handModel2;
			if (item.IsRightToBeSpawned)
			{
				handModel2 = Object.Instantiate<GameObject>(((Component)item.RightModel).gameObject).GetComponent<IHandModel>();
				((Component)handModel2).transform.parent = ((Component)this).transform;
			}
			else
			{
				handModel2 = item.RightModel;
			}
			item.modelList.Add(handModel2);
			modelGroupMapping.Add(handModel2, item);
		}
	}

	public override HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType)
	{
		VRLog.Info("Make hand representation: {0}", modelType);
		Chirality chirality = (hand.IsRight ? Chirality.Right : Chirality.Left);
		HandRepresentation handRepresentation = new HandProxy(this, hand, chirality, modelType);
		for (int i = 0; i < ModelPool.Count; i++)
		{
			VRLog.Info("Try group {0}", i);
			ModelGroup modelGroup = ModelPool[i];
			if (!modelGroup.IsEnabled)
			{
				continue;
			}
			VRLog.Info("Enabled!");
			try
			{
				IHandModel handModel = modelGroup.TryGetModel(chirality, modelType);
				if ((Object)(object)handModel != (Object)null)
				{
					VRLog.Info("Model found");
					handRepresentation.AddModel(handModel);
					modelToHandRepMapping.Add(handModel, handRepresentation);
				}
				else
				{
					VRLog.Info("Model is null");
				}
			}
			catch (Exception obj)
			{
				VRLog.Error(obj);
			}
		}
		activeHandReps.Add(handRepresentation);
		return handRepresentation;
	}

	public void EnableGroup(string groupName)
	{
		((MonoBehaviour)this).StartCoroutine(enableGroup(groupName));
	}

	private IEnumerator enableGroup(string groupName)
	{
		yield return (object)new WaitForEndOfFrame();
		ModelGroup modelGroup = null;
		for (int i = 0; i < ModelPool.Count; i++)
		{
			if (!(ModelPool[i].GroupName == groupName))
			{
				continue;
			}
			modelGroup = ModelPool[i];
			for (int j = 0; j < activeHandReps.Count; j++)
			{
				HandRepresentation handRepresentation = activeHandReps[j];
				IHandModel handModel = modelGroup.TryGetModel(handRepresentation.RepChirality, handRepresentation.RepType);
				if ((Object)(object)handModel != (Object)null)
				{
					handRepresentation.AddModel(handModel);
					modelToHandRepMapping.Add(handModel, handRepresentation);
				}
			}
			modelGroup.IsEnabled = true;
		}
		if (modelGroup == null)
		{
			Debug.LogWarning((object)"A group matching that name does not exisit in the modelPool");
		}
	}

	public void DisableGroup(string groupName)
	{
		((MonoBehaviour)this).StartCoroutine(disableGroup(groupName));
	}

	private IEnumerator disableGroup(string groupName)
	{
		yield return (object)new WaitForEndOfFrame();
		ModelGroup modelGroup = null;
		for (int i = 0; i < ModelPool.Count; i++)
		{
			if (!(ModelPool[i].GroupName == groupName))
			{
				continue;
			}
			modelGroup = ModelPool[i];
			for (int j = 0; j < modelGroup.modelsCheckedOut.Count; j++)
			{
				IHandModel handModel = modelGroup.modelsCheckedOut[j];
				if (modelToHandRepMapping.TryGetValue(handModel, out var value))
				{
					value.RemoveModel(handModel);
					modelGroup.ReturnToGroup(handModel);
					j--;
				}
			}
			modelGroup.IsEnabled = false;
		}
		if (modelGroup == null)
		{
			Debug.LogWarning((object)"A group matching that name does not exisit in the modelPool");
		}
	}

	public void ToggleGroup(string groupName)
	{
		((MonoBehaviour)this).StartCoroutine(toggleGroup(groupName));
	}

	private IEnumerator toggleGroup(string groupName)
	{
		yield return (object)new WaitForEndOfFrame();
		ModelGroup modelGroup = ModelPool.Find((ModelGroup i) => i.GroupName == groupName);
		if (modelGroup != null)
		{
			if (modelGroup.IsEnabled)
			{
				DisableGroup(groupName);
				modelGroup.IsEnabled = false;
			}
			else
			{
				EnableGroup(groupName);
				modelGroup.IsEnabled = true;
			}
		}
		else
		{
			Debug.LogWarning((object)"A group matching that name does not exisit in the modelPool");
		}
	}
}

using System.Collections.Generic;

namespace Leap.Unity;

public class HandProxy : HandRepresentation
{
	private HandPool parent;

	public List<IHandModel> handModels;

	public HandProxy(HandPool parent, Hand hand, Chirality repChirality, ModelType repType)
		: base(hand.Id, hand, repChirality, repType)
	{
		this.parent = parent;
		base.RepChirality = repChirality;
		base.RepType = repType;
		base.MostRecentHand = hand;
	}

	public override void Finish()
	{
		if (handModels != null)
		{
			for (int i = 0; i < handModels.Count; i++)
			{
				handModels[i].FinishHand();
				parent.ReturnToPool(handModels[i]);
				handModels[i] = null;
			}
		}
		parent.RemoveHandRepresentation(this);
	}

	public override void AddModel(IHandModel model)
	{
		if (handModels == null)
		{
			handModels = new List<IHandModel>();
		}
		handModels.Add(model);
		if (model.GetLeapHand() == null)
		{
			model.SetLeapHand(base.MostRecentHand);
			model.InitHand();
			model.BeginHand();
			model.UpdateHand();
		}
		else
		{
			model.SetLeapHand(base.MostRecentHand);
			model.BeginHand();
		}
	}

	public override void RemoveModel(IHandModel model)
	{
		if (handModels != null)
		{
			model.FinishHand();
			handModels.Remove(model);
		}
	}

	public override void UpdateRepresentation(Hand hand)
	{
		base.UpdateRepresentation(hand);
		if (handModels != null)
		{
			for (int i = 0; i < handModels.Count; i++)
			{
				handModels[i].SetLeapHand(hand);
				handModels[i].UpdateHand();
			}
		}
	}
}

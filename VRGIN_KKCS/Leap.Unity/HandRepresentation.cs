namespace Leap.Unity;

public abstract class HandRepresentation
{
	public int HandID { get; private set; }

	public int LastUpdatedTime { get; set; }

	public bool IsMarked { get; set; }

	public Chirality RepChirality { get; protected set; }

	public ModelType RepType { get; protected set; }

	public Hand MostRecentHand { get; protected set; }

	public HandRepresentation(int handID, Hand hand, Chirality chirality, ModelType modelType)
	{
		HandID = handID;
		MostRecentHand = hand;
		RepChirality = chirality;
		RepType = modelType;
	}

	public virtual void UpdateRepresentation(Hand hand)
	{
		MostRecentHand = hand;
	}

	public abstract void Finish();

	public abstract void AddModel(IHandModel model);

	public abstract void RemoveModel(IHandModel model);
}

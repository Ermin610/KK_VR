using UnityEngine;

namespace Leap.Unity;

public abstract class HandFactory : MonoBehaviour
{
	public abstract HandRepresentation MakeHandRepresentation(Hand hand, ModelType modelType);
}

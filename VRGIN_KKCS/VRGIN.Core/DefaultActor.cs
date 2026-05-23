using UnityEngine;

namespace VRGIN.Core;

public abstract class DefaultActor<T> : IActor where T : MonoBehaviour
{
	public T Actor { get; protected set; }

	public virtual bool IsValid => Object.op_Implicit((Object)(object)Actor);

	public abstract Transform Eyes { get; }

	public abstract bool HasHead { get; set; }

	public DefaultActor(T nativeActor)
	{
		Actor = nativeActor;
		Initialize(nativeActor);
	}

	protected virtual void Initialize(T actor)
	{
		((Component)(object)Actor).gameObject.AddComponent<Marker>();
	}

	public static bool IsAlreadyMapped(T nativeActor)
	{
		return Object.op_Implicit((Object)(object)((Component)(object)nativeActor).GetComponent<Marker>());
	}
}

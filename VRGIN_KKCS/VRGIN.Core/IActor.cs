using UnityEngine;

namespace VRGIN.Core;

public interface IActor
{
	bool IsValid { get; }

	Transform Eyes { get; }

	bool HasHead { get; set; }
}

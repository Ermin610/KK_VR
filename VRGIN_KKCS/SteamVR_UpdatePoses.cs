using UnityEngine;

[ExecuteInEditMode]
public class SteamVR_UpdatePoses : MonoBehaviour
{
	private void Awake()
	{
		Debug.Log((object)"SteamVR_UpdatePoses has been deprecated - REMOVING");
		Object.DestroyImmediate((Object)(object)this);
	}
}

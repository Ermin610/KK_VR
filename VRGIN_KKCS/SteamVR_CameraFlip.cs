using UnityEngine;

[ExecuteInEditMode]
public class SteamVR_CameraFlip : MonoBehaviour
{
	private void Awake()
	{
		Debug.Log((object)"SteamVR_CameraFlip is deprecated in Unity 5.4 - REMOVING");
		Object.DestroyImmediate((Object)(object)this);
	}
}

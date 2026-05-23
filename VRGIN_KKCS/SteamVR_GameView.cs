using UnityEngine;

[ExecuteInEditMode]
public class SteamVR_GameView : MonoBehaviour
{
	private void Awake()
	{
		Debug.Log((object)"SteamVR_GameView is deprecated in Unity 5.4 - REMOVING");
		Object.DestroyImmediate((Object)(object)this);
	}
}

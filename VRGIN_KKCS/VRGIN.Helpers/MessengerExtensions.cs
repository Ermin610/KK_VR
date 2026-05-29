using System.Reflection;
using UnityEngine;

namespace VRGIN.Helpers;

public static class MessengerExtensions
{
	private static void InvokeIfExists(this object objectToCheck, string methodName, params object[] parameters)
	{
		objectToCheck.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(objectToCheck, parameters);
	}

	public static void BroadcastToAll(this GameObject gameobject, string methodName, params object[] parameters)
	{
		MonoBehaviour[] components = gameobject.GetComponents<MonoBehaviour>();
		for (int i = 0; i < components.Length; i++)
		{
			components[i].InvokeIfExists(methodName, parameters);
		}
	}

	public static void BroadcastToAll(this Component component, string methodName, params object[] parameters)
	{
		component.gameObject.BroadcastToAll(methodName, parameters);
	}

	public static void SendMessageToAll(this GameObject gameobject, string methodName, params object[] parameters)
	{
		MonoBehaviour[] componentsInChildren = gameobject.GetComponentsInChildren<MonoBehaviour>(true);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].InvokeIfExists(methodName, parameters);
		}
	}

	public static void SendMessageToAll(this Component component, string methodName, params object[] parameters)
	{
		component.gameObject.SendMessageToAll(methodName, parameters);
	}

	public static void SendMessageUpwardsToAll(this GameObject gameobject, string methodName, params object[] parameters)
	{
		Transform val = gameobject.transform;
		while (val != null)
		{
			((Component)val).gameObject.BroadcastToAll(methodName, parameters);
			val = val.parent;
		}
	}

	public static void SendMessageUpwardsToAll(this Component component, string methodName, params object[] parameters)
	{
		component.gameObject.SendMessageUpwardsToAll(methodName, parameters);
	}
}

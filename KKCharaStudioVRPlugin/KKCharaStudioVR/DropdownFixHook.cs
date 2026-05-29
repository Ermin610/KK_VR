using System;
using System.Reflection;
using Harmony;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UI;
using VRGIN.Core;

namespace KKCharaStudioVR
{
	public static class DropdownFixHook
	{
		public static void InstallHook()
		{
			HarmonyInstance.Create("KKChacaStudioVR.DropdownFixHook").PatchAll(typeof(DropdownFixHook));
		}

		[HarmonyPatch(typeof(Dropdown), "Show", new Type[] { }, null)]
		[HarmonyPostfix]
		public static void Postfix(Dropdown __instance)
		{
			try
			{
				// In uGUI, Dropdown.Show creates two private fields: m_Dropdown and m_Blocker.
				FieldInfo blockerField = typeof(Dropdown).GetField("m_Blocker", BindingFlags.NonPublic | BindingFlags.Instance);
				FieldInfo dropdownField = typeof(Dropdown).GetField("m_Dropdown", BindingFlags.NonPublic | BindingFlags.Instance);

				int uiLayer = LayerMask.NameToLayer(VR.Context.UILayer);

				if (blockerField != null)
				{
					GameObject blocker = blockerField.GetValue(__instance) as GameObject;
					if (blocker != null)
					{
						ChangeLayer(blocker, uiLayer);
					}
				}

				if (dropdownField != null)
				{
					GameObject dropdownList = dropdownField.GetValue(__instance) as GameObject;
					if (dropdownList != null)
					{
						ChangeLayer(dropdownList, uiLayer);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[KKCharaStudioVRPlugin] Error in DropdownFixHook: {ex}");
			}
		}

		private static void ChangeLayer(GameObject go, int layer)
		{
			go.layer = layer;
			Transform[] children = go.GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < children.Length; i++)
			{
				children[i].gameObject.layer = layer;
			}
		}
	}
}

using System;
using System.Reflection;
using Harmony;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KKCharaStudioVR
{
	public static class FolderBrowserWidthPatch
	{
		public static void InstallHook()
		{
			try
			{
				Assembly browserFoldersAssembly = null;
				foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (asm.GetName().Name == "KK_BrowserFolders")
					{
						browserFoldersAssembly = asm;
						break;
					}
				}

				if (browserFoldersAssembly == null)
				{
					Console.WriteLine("[KKCharaStudioVRPlugin] KK_BrowserFolders assembly not loaded, skipping patch.");
					return;
				}

				Type baseFolderBrowserType = browserFoldersAssembly.GetType("BrowserFolders.BaseFolderBrowser");
				if (baseFolderBrowserType == null)
				{
					Console.WriteLine("[KKCharaStudioVRPlugin] BrowserFolders.BaseFolderBrowser type not found.");
					return;
				}

				var harmony = HarmonyInstance.Create("KKChacaStudioVR.FolderBrowserWidthPatch");
				MethodInfo postfixMethod = typeof(FolderBrowserWidthPatch).GetMethod(nameof(GetDefaultRect_Postfix), BindingFlags.Public | BindingFlags.Static);

				// Patch BaseFolderBrowser.GetDefaultRect
				MethodInfo baseGetDefaultRect = baseFolderBrowserType.GetMethod("GetDefaultRect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				if (baseGetDefaultRect != null)
				{
					harmony.Patch(baseGetDefaultRect, null, new HarmonyMethod(postfixMethod));
					Console.WriteLine("[KKCharaStudioVRPlugin] Patched BaseFolderBrowser.GetDefaultRect successfully!");
				}

				// Dynamically find and patch all subclasses overriding GetDefaultRect
				try
				{
					Type[] types = browserFoldersAssembly.GetTypes();
					foreach (var t in types)
					{
						if (t != null && t.IsSubclassOf(baseFolderBrowserType))
						{
							MethodInfo m = t.GetMethod("GetDefaultRect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
							if (m != null)
							{
								harmony.Patch(m, null, new HarmonyMethod(postfixMethod));
								Console.WriteLine($"[KKCharaStudioVRPlugin] Patched subclass {t.FullName}.GetDefaultRect successfully!");
							}
						}
					}
				}
				catch (ReflectionTypeLoadException ex)
				{
					foreach (var t in ex.Types)
					{
						if (t != null && t.IsSubclassOf(baseFolderBrowserType))
						{
							MethodInfo m = t.GetMethod("GetDefaultRect", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
							if (m != null)
							{
								harmony.Patch(m, null, new HarmonyMethod(postfixMethod));
								Console.WriteLine($"[KKCharaStudioVRPlugin] Patched subclass {t.FullName}.GetDefaultRect (from load exception) successfully!");
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[KKCharaStudioVRPlugin] Error installing FolderBrowserWidthPatch: {ex}");
			}
		}

		public static void GetDefaultRect_Postfix(object __instance, ref Rect __result)
		{
			try
			{
				// Make the default width wider (at least 16% of screen width instead of 8% or 10%)
				float minWidthPercentage = 0.16f;
				float minWidth = Screen.width * minWidthPercentage;
				if (__result.width < minWidth)
				{
					__result.width = minWidth;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[KKCharaStudioVRPlugin] Error in GetDefaultRect Postfix: {ex}");
			}
		}
	}
}

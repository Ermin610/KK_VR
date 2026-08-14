using System;
using Harmony;
using VRGIN.Core;
using MirrorManager = KKCharaStudioVR.Mirror.Manager;

namespace KKCharaStudioVR
{
	// Mirrors render one reflection from the head, so both eyes get the same image.
	// Studio mirrors can show up at any time and MirrorReflection has no Awake, so swap
	// them on the first render.
	public static class MirrorFixHook
	{
		private static readonly MirrorManager _manager = new MirrorManager();

		public static void InstallHook()
		{
			HarmonyInstance.Create("KKCharaStudioVR.MirrorFixHook").PatchAll(typeof(MirrorFixHook));
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MirrorReflection), "OnWillRenderObject")]
		public static bool OnWillRenderObjectPreHook(MirrorReflection __instance)
		{
			try
			{
				// no-op once the mirror already has a VRReflection
				if (!_manager.Fix(__instance))
					return true;

				// VRReflection draws it now, skip the flat one
				return false;
			}
			catch (Exception obj)
			{
				VRLog.Error(obj);
				return true;
			}
		}
	}
}

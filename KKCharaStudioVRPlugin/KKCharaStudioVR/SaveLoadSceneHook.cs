using System;
using System.Reflection;
using BepInEx.Logging;
using BepInEx4;
using HarmonyLib;
using Studio;
using UnityEngine;
using VRGIN.Core;
using Logger = BepInEx4.Logger;

namespace KKCharaStudioVR;

public static class SaveLoadSceneHook
{
    private sealed class SaveSceneState
    {
        public FieldInfo renderCameraField;
        public Studio.GameScreenShot screenShot;
        public Camera[] originalCameras;
        public bool replaced;
    }

    private static bool _installed;

    public static void InstallHook()
    {
        if (_installed) return;

        try
        {
            Logger.Log((LogLevel)16, (object)"Install SaveLoadSceneHook");
            MethodInfo original = AccessTools.Method(typeof(Studio.Studio), "SaveScene", Type.EmptyTypes);
            if (original == null) throw new MissingMethodException(typeof(Studio.Studio).FullName, "SaveScene");

            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("KKCharaStudioVR.SaveLoadSceneHook");
            HarmonyMethod prefix = new HarmonyMethod(AccessTools.Method(typeof(SaveLoadSceneHook), nameof(SaveScenePreHook)));
            HarmonyMethod postfix = new HarmonyMethod(AccessTools.Method(typeof(SaveLoadSceneHook), nameof(SaveScenePostHook)));
            HarmonyMethod finalizer = new HarmonyMethod(AccessTools.Method(typeof(SaveLoadSceneHook), nameof(SaveSceneFinalizer)));

            // Harmony 2.9 added an ilmanipulator argument. Resolve the overload at
            // runtime so the same plugin also loads on older HF Patch installations.
            MethodInfo patchMethod = null;
            foreach (MethodInfo candidate in typeof(HarmonyLib.Harmony).GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (candidate.Name != "Patch") continue;
                int parameterCount = candidate.GetParameters().Length;
                if (parameterCount == 6)
                {
                    patchMethod = candidate;
                    break;
                }
                if (parameterCount == 5) patchMethod = candidate;
            }
            if (patchMethod == null) throw new MissingMethodException(typeof(HarmonyLib.Harmony).FullName, "Patch");

            object[] patchArguments = patchMethod.GetParameters().Length == 6
                ? new object[] { original, prefix, postfix, null, finalizer, null }
                : new object[] { original, prefix, postfix, null, finalizer };
            patchMethod.Invoke(harmony, patchArguments);
            _installed = true;
        }
        catch (Exception ex)
        {
            Logger.Log((LogLevel)2, (object)("Failed to install SaveLoadSceneHook: " + ex));
        }
    }

    private static bool SaveScenePreHook(Studio.Studio __instance, ref SaveSceneState __state)
    {
        __state = new SaveSceneState();
        try
        {
            Logger.Log((LogLevel)32, (object)"Update camera position and rotation for scene capture.");
            if (VRCameraMoveHelper.Instance != null)
                VRCameraMoveHelper.Instance.CurrentToCameraCtrl();

            Studio.Studio studio = __instance ?? Singleton<Studio.Studio>.Instance;
            if (studio == null || studio.gameScreenShot == null || VR.Camera == null ||
                VR.Camera.SteamCam == null || VR.Camera.SteamCam.camera == null)
                return true;

            FieldInfo field = typeof(Studio.GameScreenShot).GetField(
                "renderCam", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field == null)
            {
                Logger.Log((LogLevel)4, (object)"GameScreenShot.renderCam was not found; saving without the VR capture override.");
                return true;
            }

            __state.renderCameraField = field;
            __state.screenShot = studio.gameScreenShot;
            __state.originalCameras = field.GetValue(__state.screenShot) as Camera[];
            field.SetValue(__state.screenShot, new Camera[1] { VR.Camera.SteamCam.camera });
            __state.replaced = true;
        }
        catch (Exception ex)
        {
            RestoreRenderCameras(__state);
            Logger.Log((LogLevel)4, (object)("VR scene capture setup failed; continuing normal save: " + ex.Message));
        }
        return true;
    }

    private static void SaveScenePostHook(SaveSceneState __state)
    {
        RestoreRenderCameras(__state);
    }

    private static Exception SaveSceneFinalizer(Exception __exception, SaveSceneState __state)
    {
        RestoreRenderCameras(__state);
        return __exception;
    }

    private static void RestoreRenderCameras(SaveSceneState state)
    {
        if (state == null || !state.replaced || state.renderCameraField == null || state.screenShot == null)
            return;

        try
        {
            state.renderCameraField.SetValue(state.screenShot, state.originalCameras);
        }
        catch (Exception ex)
        {
            Logger.Log((LogLevel)2, (object)("Failed to restore screenshot cameras: " + ex.Message));
        }
        finally
        {
            state.replaced = false;
        }
    }
}

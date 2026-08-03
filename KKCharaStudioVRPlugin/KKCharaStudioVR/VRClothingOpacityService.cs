using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KKCharaStudioVR;

internal sealed class VRClothingOpacityInfo
{
    public bool MaterialEditorAvailable;
    public float Opacity = 1f;
    public float MinimumOpacity = 1f;
    public float MaximumOpacity = 1f;
    public bool Mixed;
    public int MaterialCount;
    public int SupportedCount;
    public int CompatibleCount;
    public int AzPairedCount;
    public int ProtectedCount;
    public int UnsupportedCount;
}

internal static class VRClothingOpacityService
{
    private const string CompatibilityShader = "Shader Forge/main_item_studio_alpha";
    private const string AzClothCutoutShader = "Az/StandardClothCutout";
    private const string AzClothAlphaShader = "Az/StandardClothAlpha";
    private const string AzItemCutoutShader = "Az/StandardItemCutout";
    private const string AzItemAlphaShader = "Az/StandardItemAlpha";
    private const string AzLiteCutoutShader = "Az/StandardLiteCutout";
    private const string AzLiteAlphaShader = "Az/StandardLiteAlpha";
    private const float OpacityTolerance = 0.005f;

    private static readonly string[] FloatOpacityProperties =
    {
        "alpha",
        "Alpha",
        "Opacity",
        "opacity"
    };

    private static readonly string[] ColorOpacityProperties =
    {
        "Color",
        "MainColor",
        "Color1",
        "Color2",
        "Color3"
    };

    private static MaterialEditorBridge _bridge;
    private static readonly bool DiagnosticsEnabled = Environment.CommandLine.IndexOf(
        "--kkvr-opacity-self-test", StringComparison.OrdinalIgnoreCase) >= 0
        || Environment.CommandLine.IndexOf(
            "--kkvr-opacity-calibration", StringComparison.OrdinalIgnoreCase) >= 0;
    // Only entries created by this service are restored. This is deliberately
    // separate from MaterialEditor's saved data so Reset never deletes a
    // character card's pre-existing shader, queue, Alpha, or color overrides.
    private static readonly Dictionary<string, CompatibilityOverrideState> CompatibilityStates =
        new Dictionary<string, CompatibilityOverrideState>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Shader> LoadedShaderCache =
        new Dictionary<string, Shader>(StringComparer.OrdinalIgnoreCase);

    public static bool HasPart(ChaControl character, int partId)
    {
        if (character == null || partId < 0 || partId >= VRCharacterClothingService.PartCount)
            return false;
        if (SlotHasRenderableClothing(character, partId))
            return true;
        return partId == 7 && SlotHasRenderableClothing(character, 8);
    }

    public static bool TryInspect(
        ChaControl character,
        int partId,
        out VRClothingOpacityInfo info,
        out string status)
    {
        info = new VRClothingOpacityInfo();
        if (!TryPrepare(character, partId, out MaterialEditorBridge bridge, out object controller,
                out List<MaterialTarget> targets, out status))
        {
            return false;
        }

        info.MaterialEditorAvailable = true;
        PopulateInfo(bridge, controller, targets, info);
        status = targets.Count == 0
            ? VRCharacterClothingService.GetPartName(partId) + "没有可调整的材质"
            : null;
        return targets.Count > 0;
    }

    public static bool TrySetPartOpacity(
        ChaControl character,
        int partId,
        float opacity,
        bool compatibilityEnabled,
        out VRClothingOpacityInfo info,
        out string status)
    {
        info = new VRClothingOpacityInfo();
        if (!TryPrepare(character, partId, out MaterialEditorBridge bridge, out object controller,
                out List<MaterialTarget> targets, out status))
        {
            return false;
        }
        if (targets.Count == 0)
        {
            status = VRCharacterClothingService.GetPartName(partId) + "没有可调整的材质";
            return false;
        }

        opacity = Mathf.Clamp01(opacity);
        int changed = 0;
        int unchanged = 0;
        int skipped = 0;
        int failed = 0;
        int azPaired = 0;
        int genericConverted = 0;
        int protectedSkipped = 0;
        string firstFailure = null;
        foreach (MaterialTarget target in targets)
        {
            try
            {
                LogDiagnostic("before", target, null, opacity);
                if (opacity >= 1f - OpacityTolerance
                    && IsCompatibilityActive(target))
                {
                    RestoreCompatibilityShader(bridge, controller, target);
                    changed++;
                    continue;
                }

                OpacityBinding binding = ResolveBinding(target.Material);
                if (binding.IsSupported)
                {
                    CaptureCompatibilityState(
                        bridge,
                        controller,
                        target,
                        false,
                        null);
                    MarkTouchedProperties(target, binding);
                    ApplyBinding(bridge, controller, target, binding, opacity);
                    LogDiagnostic("native-after", target, binding, opacity);
                    changed++;
                    continue;
                }

                // A Cutout/opaque material is already at 0% user-facing
                // transparency. Do not convert its shader just to write 1.0.
                if (opacity >= 1f - OpacityTolerance)
                {
                    unchanged++;
                    continue;
                }

                if (!compatibilityEnabled)
                {
                    skipped++;
                    continue;
                }

                string compatibilityShader;
                bool azPair;
                if (!TryGetCompatibilityShader(
                        target.Material,
                        out compatibilityShader,
                        out azPair))
                {
                    skipped++;
                    continue;
                }

                // AZ Cutout -> matching AZ Alpha is a semantic, reversible
                // conversion. The generic fallback cannot safely replace a
                // user-authored MaterialEditor shader/queue override.
                bool protectedOverride = azPair
                    ? HasConflictingAzMaterialEditorOverride(
                        bridge, controller, target)
                    : HasProtectedMaterialEditorOverride(
                        bridge, controller, target);
                if (protectedOverride)
                {
                    protectedSkipped++;
                    skipped++;
                    continue;
                }

                CaptureCompatibilityState(
                    bridge,
                    controller,
                    target,
                    true,
                    compatibilityShader);
                bridge.SetShader(controller, target, compatibilityShader);
                RestoreConvertedMaterialProperties(target);
                ApplyTransparentRenderQueue(
                    bridge,
                    controller,
                    target,
                    compatibilityShader);
                OpacityBinding compatibilityBinding = ResolveBinding(target.Material);
                LogDiagnostic("compatibility-before", target, compatibilityBinding, opacity);
                if (!compatibilityBinding.IsSupported)
                {
                    string liveShader = target.Material.shader == null
                        ? "(null)"
                        : target.Material.shader.name;
                    firstFailure = "透明 Shader 未暴露 Alpha：" + liveShader
                        + "；" + DescribeOpacityCandidates(target.Material);
                    TryRollbackCompatibilityShader(bridge, controller, target);
                    failed++;
                    continue;
                }

                MarkTouchedProperties(target, compatibilityBinding);
                ApplyBinding(
                    bridge,
                    controller,
                    target,
                    compatibilityBinding,
                    opacity);
                LogDiagnostic("compatibility-after", target, compatibilityBinding, opacity);
                if (azPair)
                    azPaired++;
                else
                    genericConverted++;
                changed++;
            }
            catch (Exception ex)
            {
                failed++;
                Exception cause = Unwrap(ex);
                if (string.IsNullOrEmpty(firstFailure))
                    firstFailure = cause.Message;
                if (IsCompatibilityActive(target))
                    TryRollbackCompatibilityShader(bridge, controller, target);
                Debug.LogWarning("[KK VR] Unable to adjust clothing opacity for "
                    + target.MaterialName + ": " + cause.Message);
            }
        }

        info.MaterialEditorAvailable = true;
        PopulateInfo(bridge, controller, targets, info);
        string partName = VRCharacterClothingService.GetPartName(partId);
        status = partName + "透明度 "
            + Mathf.RoundToInt((1f - opacity) * 100f) + "%"
            + "；已调整 " + changed;
        if (unchanged > 0)
            status += "，无需修改 " + unchanged;
        if (azPaired > 0)
            status += "，AZ 同族 " + azPaired;
        if (genericConverted > 0)
            status += "，通用兼容 " + genericConverted;
        if (protectedSkipped > 0)
            status += "，受保护未覆盖 " + protectedSkipped;
        if (skipped > 0)
            status += "，跳过 " + skipped;
        if (failed > 0)
        {
            status += "，失败 " + failed;
            if (!string.IsNullOrEmpty(firstFailure))
                status += "（" + firstFailure + "）";
        }
        return (changed > 0 || unchanged > 0) && failed == 0;
    }

    /// <summary>
    /// Creates a non-persistent opacity session for MMD cue playback.  Unlike
    /// the editor-facing methods above, this path never writes MaterialEditor
    /// card data on every animation tick.  Every touched live material is
    /// snapshotted and restored exactly when the session ends.
    /// </summary>
    internal static bool TryCreateRuntimeSession(
        ChaControl character,
        out RuntimeOpacitySession session,
        out string status)
    {
        session = null;
        if (character == null)
        {
            status = "角色材质尚未就绪";
            return false;
        }

        session = new RuntimeOpacitySession(character);
        status = null;
        return true;
    }

    /// <summary>
    /// A reversible live-material overlay used only while an MMD cue sheet is
    /// active.  AZ Cutout materials are paired with the matching AZ Alpha
    /// shader; unsupported AZ families are skipped instead of being replaced
    /// by the generic compatibility shader.
    /// </summary>
    internal sealed class RuntimeOpacitySession : IDisposable
    {
        private readonly ChaControl _character;
        private readonly MaterialEditorBridge _bridge;
        private readonly object _controller;
        private readonly Dictionary<long, RuntimeCueMaterialState> _states =
            new Dictionary<long, RuntimeCueMaterialState>();
        private bool _disposed;

        internal RuntimeOpacitySession(ChaControl character)
        {
            _character = character;
            _bridge = GetBridge();
            if (_bridge != null)
            {
                string ignored;
                object controller;
                _controller = _bridge.TryGetController(
                    character, out controller, out ignored)
                    ? controller
                    : null;
            }
        }

        public bool TrySetPartTransparency(
            int partId,
            float transparency,
            out string status)
        {
            if (_disposed || _character == null)
            {
                status = "角色透明渐变会话已结束";
                return false;
            }
            if (partId < 0 || partId >= VRCharacterClothingService.PartCount)
            {
                status = "不支持的服装部位";
                return false;
            }

            List<MaterialTarget> targets = GetTargets(_character, partId);
            if (targets.Count == 0)
            {
                status = VRCharacterClothingService.GetPartName(partId) + "没有可渐变的材质";
                return false;
            }

            float opacity = 1f - Mathf.Clamp01(transparency);
            int changed = 0;
            int skipped = 0;
            string firstFailure = null;
            HashSet<int> seen = new HashSet<int>();
            foreach (MaterialTarget target in targets)
            {
                foreach (Material material in GetLiveMaterials(target))
                {
                    if (material == null || !seen.Add(material.GetInstanceID()))
                        continue;

                    string error;
                    if (TryApplyMaterial(partId, target, material, opacity, out error))
                    {
                        changed++;
                    }
                    else
                    {
                        skipped++;
                        if (string.IsNullOrEmpty(firstFailure))
                            firstFailure = error;
                    }
                }
            }

            status = changed > 0
                ? VRCharacterClothingService.GetPartName(partId) + "渐变已更新"
                : firstFailure ?? VRCharacterClothingService.GetPartName(partId) + "没有兼容的透明材质";
            if (changed > 0 && skipped > 0)
                status += "；跳过 " + skipped;
            return changed > 0;
        }

        public void RestorePart(int partId)
        {
            List<long> keys = new List<long>();
            foreach (KeyValuePair<long, RuntimeCueMaterialState> pair in _states)
            {
                if (pair.Value.PartId == partId)
                    keys.Add(pair.Key);
            }
            foreach (long key in keys)
            {
                RuntimeCueMaterialState state;
                if (!_states.TryGetValue(key, out state))
                    continue;
                state.RestoreAndDispose();
                _states.Remove(key);
            }
        }

        public void RestoreAll()
        {
            foreach (RuntimeCueMaterialState state in
                     new List<RuntimeCueMaterialState>(_states.Values))
            {
                state.RestoreAndDispose();
            }
            _states.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            RestoreAll();
            _disposed = true;
        }

        private bool TryApplyMaterial(
            int partId,
            MaterialTarget target,
            Material material,
            float opacity,
            out string status)
        {
            long key = ((long)partId << 32) ^ (uint)material.GetInstanceID();
            RuntimeCueMaterialState state;
            if (!_states.TryGetValue(key, out state))
            {
                state = RuntimeCueMaterialState.Capture(partId, material);
                try
                {
                    OpacityBinding initialBinding = ResolveBinding(material);
                    if (!initialBinding.IsSupported)
                    {
                        string shaderName;
                        bool azPair;
                        if (!TryGetCompatibilityShader(material, out shaderName, out azPair))
                        {
                            state.Dispose();
                            status = "材质不支持透明渐变：" + NormalizeMaterialName(material.name);
                            return false;
                        }

                        // Respect authored MaterialEditor shader/queue choices.
                        // A same-family AZ pairing may keep an identical saved
                        // shader, but a conflicting override is never replaced.
                        if (_bridge != null && _controller == null)
                        {
                            state.Dispose();
                            status = "MaterialEditor 角色控制器尚未初始化，暂不转换 Shader";
                            return false;
                        }
                        if (_bridge != null && _controller != null)
                        {
                            MaterialTarget exactTarget = new MaterialTarget(
                                target.Slot, target.Root, material, target.MaterialName);
                            bool protectedOverride = azPair
                                ? HasConflictingAzMaterialEditorOverride(
                                    _bridge, _controller, exactTarget)
                                : HasProtectedMaterialEditorOverride(
                                    _bridge, _controller, exactTarget);
                            if (protectedOverride)
                            {
                                state.Dispose();
                                status = "材质已有受保护的 MaterialEditor Shader/Queue 设置";
                                return false;
                            }
                        }

                        Shader compatibleShader = FindLoadedShader(shaderName);
                        if (compatibleShader == null)
                        {
                            state.Dispose();
                            status = "找不到透明 Shader：" + shaderName;
                            return false;
                        }

                        material.shader = compatibleShader;
                        material.CopyPropertiesFromMaterial(state.Snapshot);
                        // Unity 5.6 may retain the old shader property layout
                        // until the exact AssetBundle shader is rebound.
                        material.shader = compatibleShader;
                        ApplyRuntimeTransparentRenderState(material, compatibleShader);
                    }

                    OpacityBinding binding = ResolveBinding(material);
                    if (!binding.IsSupported)
                    {
                        state.RestoreAndDispose();
                        status = "透明 Shader 没有可写入的 Alpha："
                            + (material.shader == null ? "(null)" : material.shader.name);
                        return false;
                    }

                    _states[key] = state;
                }
                catch (Exception ex)
                {
                    state.RestoreAndDispose();
                    status = "准备透明渐变失败：" + Unwrap(ex).Message;
                    return false;
                }
            }

            try
            {
                OpacityBinding binding = ResolveBinding(material);
                if (!binding.IsSupported)
                {
                    status = "当前材质不再支持透明渐变";
                    return false;
                }
                ApplyRuntimeBinding(material, binding, opacity);
                status = null;
                return true;
            }
            catch (Exception ex)
            {
                status = "应用透明渐变失败：" + Unwrap(ex).Message;
                return false;
            }
        }
    }

    private static void ApplyRuntimeBinding(
        Material material,
        OpacityBinding binding,
        float opacity)
    {
        foreach (string property in binding.FloatProperties)
            material.SetFloat("_" + property, opacity);
        foreach (string property in binding.ColorProperties)
        {
            string shaderProperty = "_" + property;
            Color color = material.GetColor(shaderProperty);
            color.a = opacity;
            material.SetColor(shaderProperty, color);
        }
    }

    private static void ApplyRuntimeTransparentRenderState(
        Material material,
        Shader shader)
    {
        int queue = shader != null && shader.renderQueue >= 0 ? shader.renderQueue : 3000;
        material.renderQueue = queue;
        if (material.HasProperty("_BlendSrc"))
            material.SetFloat("_BlendSrc", 5f);
        if (material.HasProperty("_BlendDst"))
            material.SetFloat("_BlendDst", 10f);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 1f);
        if (material.HasProperty("_PREMULTIPLY_ALPHA"))
            material.SetFloat("_PREMULTIPLY_ALPHA", 0f);
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
    }

    private static void LogDiagnostic(
        string stage,
        MaterialTarget target,
        OpacityBinding binding,
        float requestedOpacity)
    {
        if (!DiagnosticsEnabled || target == null || target.Material == null)
            return;

        Material material = target.Material;
        string values = string.Empty;
        IEnumerable<string> diagnosticFloats = binding == null
            ? FloatOpacityProperties
            : binding.FloatProperties;
        foreach (string property in diagnosticFloats)
        {
            string unityProperty = "_" + property;
            if (!material.HasProperty(unityProperty))
                continue;
            if (values.Length > 0)
                values += ",";
            values += unityProperty + "=" + material.GetFloat(unityProperty).ToString("R");
        }
        IEnumerable<string> diagnosticColors = binding == null
            ? ColorOpacityProperties
            : binding.ColorProperties;
        foreach (string property in diagnosticColors)
        {
            string unityProperty = "_" + property;
            if (!material.HasProperty(unityProperty))
                continue;
            if (values.Length > 0)
                values += ",";
            values += unityProperty + ".a=" + material.GetColor(unityProperty).a.ToString("R");
        }

        string selected = "none";
        if (binding != null)
        {
            if (binding.FloatProperties.Count > 0)
                selected = "float:" + string.Join(",", binding.FloatProperties.ToArray());
            else if (binding.ColorProperties.Count > 0)
                selected = "color:" + string.Join(",", binding.ColorProperties.ToArray());
        }
        VRGIN.Core.VRLog.Info("[KK VR opacity diagnostic] stage=" + stage
            + "; material=" + target.MaterialName
            + "; shader=" + (material.shader == null ? "(null)" : material.shader.name)
            + "; queue=" + material.renderQueue
            + "; requested=" + requestedOpacity.ToString("R")
            + "; selected=" + selected
            + "; values=" + values);
    }

    private static string DescribeOpacityCandidates(Material material)
    {
        if (material == null)
            return "material=(null)";
        string result = "float[";
        for (int index = 0; index < FloatOpacityProperties.Length; index++)
        {
            if (index > 0)
                result += ",";
            string property = FloatOpacityProperties[index];
            result += property + "=" + material.HasProperty("_" + property);
        }
        result += "]; color[";
        for (int index = 0; index < ColorOpacityProperties.Length; index++)
        {
            if (index > 0)
                result += ",";
            string property = ColorOpacityProperties[index];
            result += property + "=" + material.HasProperty("_" + property);
        }
        return result + "]";
    }

    public static bool TryResetPart(
        ChaControl character,
        int partId,
        out VRClothingOpacityInfo info,
        out string status)
    {
        info = new VRClothingOpacityInfo();
        if (!TryPrepare(character, partId, out MaterialEditorBridge bridge, out object controller,
                out List<MaterialTarget> targets, out status))
        {
            return false;
        }
        if (targets.Count == 0)
        {
            status = VRCharacterClothingService.GetPartName(partId) + "没有可恢复的材质";
            return false;
        }

        int changed = 0;
        int failed = 0;
        foreach (MaterialTarget target in targets)
        {
            try
            {
                if (!IsCompatibilityActive(target))
                    continue;
                RestoreCompatibilityShader(bridge, controller, target);
                changed++;
            }
            catch (Exception ex)
            {
                failed++;
                Debug.LogWarning("[KK VR] Unable to reset clothing opacity for "
                    + target.MaterialName + ": " + Unwrap(ex).Message);
            }
        }

        info.MaterialEditorAvailable = true;
        PopulateInfo(bridge, controller, targets, info);
        status = changed > 0
            ? VRCharacterClothingService.GetPartName(partId) + "已恢复原始材质与透明度"
            : VRCharacterClothingService.GetPartName(partId) + "没有由本插件创建的透明度修改";
        if (failed > 0)
            status += "；失败 " + failed;
        return changed > 0 && failed == 0;
    }

    private static bool TryPrepare(
        ChaControl character,
        int partId,
        out MaterialEditorBridge bridge,
        out object controller,
        out List<MaterialTarget> targets,
        out string status)
    {
        bridge = null;
        controller = null;
        targets = new List<MaterialTarget>();
        PruneStaleStates();
        if (character == null)
        {
            status = "请先选择场景角色";
            return false;
        }
        if (partId < 0 || partId >= VRCharacterClothingService.PartCount)
        {
            status = "不支持的服装部位";
            return false;
        }

        bridge = GetBridge();
        if (bridge == null)
        {
            status = "MaterialEditor 未安装或尚未初始化";
            return false;
        }
        if (!bridge.TryGetController(character, out controller, out status))
            return false;

        targets = GetTargets(character, partId);
        status = null;
        return true;
    }

    private static void PruneStaleStates()
    {
        List<string> staleKeys = null;
        foreach (var pair in CompatibilityStates)
        {
            bool anyLiveMaterial = false;
            foreach (RuntimeMaterialState runtimeState in pair.Value.RuntimeMaterials)
            {
                if (runtimeState.Material == null)
                    continue;
                anyLiveMaterial = true;
                break;
            }
            if (anyLiveMaterial)
                continue;
            if (staleKeys == null)
                staleKeys = new List<string>();
            staleKeys.Add(pair.Key);
        }
        if (staleKeys == null)
            return;
        foreach (string key in staleKeys)
        {
            CompatibilityOverrideState state;
            if (CompatibilityStates.TryGetValue(key, out state))
                state.Dispose();
            CompatibilityStates.Remove(key);
        }
    }

    private static MaterialEditorBridge GetBridge()
    {
        if (_bridge != null)
            return _bridge;
        try
        {
            _bridge = MaterialEditorBridge.TryCreate();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[KK VR] Unable to initialize MaterialEditor bridge: "
                + Unwrap(ex).Message);
        }
        return _bridge;
    }

    private static List<MaterialTarget> GetTargets(ChaControl character, int partId)
    {
        List<MaterialTarget> targets = new List<MaterialTarget>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddSlotTargets(character, partId, targets, seen);
        if (partId == 7)
            AddSlotTargets(character, 8, targets, seen);
        return targets;
    }

    private static bool SlotHasRenderableClothing(ChaControl character, int slot)
    {
        GameObject[] clothes = character.objClothes;
        if (clothes == null || slot < 0 || slot >= clothes.Length)
            return false;
        GameObject root = clothes[slot];
        if (root == null)
            return false;
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null)
                    return true;
            }
        }
        return false;
    }

    private static void AddSlotTargets(
        ChaControl character,
        int slot,
        ICollection<MaterialTarget> targets,
        HashSet<string> seen)
    {
        GameObject[] clothes = character.objClothes;
        if (clothes == null || slot < 0 || slot >= clothes.Length)
            return;
        GameObject root = clothes[slot];
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;
            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                if (material == null)
                    continue;
                string materialName = NormalizeMaterialName(material.name);
                string key = slot + "|" + materialName;
                if (!seen.Add(key))
                    continue;
                targets.Add(new MaterialTarget(slot, root, material, materialName));
            }
        }
    }

    private static void PopulateInfo(
        MaterialEditorBridge bridge,
        object controller,
        List<MaterialTarget> targets,
        VRClothingOpacityInfo info)
    {
        info.MaterialCount = targets.Count;
        float sum = 0f;
        float minimum = 1f;
        float maximum = 0f;
        int valueCount = 0;
        foreach (MaterialTarget target in targets)
        {
            OpacityBinding binding = ResolveBinding(target.Material);
            if (binding.IsSupported)
            {
                info.SupportedCount++;
                CompatibilityOverrideState activeState;
                if (CompatibilityStates.TryGetValue(
                        BuildCompatibilityKey(target), out activeState)
                    && activeState.ShaderConverted
                    && !string.IsNullOrEmpty(activeState.AppliedShader)
                    && activeState.AppliedShader.StartsWith(
                        "Az/Standard", StringComparison.OrdinalIgnoreCase))
                {
                    info.AzPairedCount++;
                }
                foreach (float value in binding.Values)
                {
                    float clamped = Mathf.Clamp01(value);
                    minimum = Mathf.Min(minimum, clamped);
                    maximum = Mathf.Max(maximum, clamped);
                    sum += clamped;
                    valueCount++;
                }
                continue;
            }

            string compatibilityShader;
            bool azPair;
            if (TryGetCompatibilityShader(
                    target.Material,
                    out compatibilityShader,
                    out azPair))
            {
                info.CompatibleCount++;
                if (azPair)
                {
                    info.AzPairedCount++;
                    if (HasConflictingAzMaterialEditorOverride(
                            bridge, controller, target))
                    {
                        info.ProtectedCount++;
                    }
                }
                else if (HasProtectedMaterialEditorOverride(bridge, controller, target))
                    info.ProtectedCount++;
            }
            else
                info.UnsupportedCount++;
        }

        if (valueCount == 0)
        {
            info.Opacity = 1f;
            info.MinimumOpacity = 1f;
            info.MaximumOpacity = 1f;
            info.Mixed = false;
            return;
        }

        info.Opacity = sum / valueCount;
        info.MinimumOpacity = minimum;
        info.MaximumOpacity = maximum;
        info.Mixed = maximum - minimum > OpacityTolerance;
    }

    private static OpacityBinding ResolveBinding(Material material)
    {
        OpacityBinding binding = new OpacityBinding();
        if (material == null)
            return binding;

        foreach (string property in FloatOpacityProperties)
        {
            string shaderProperty = "_" + property;
            if (!material.HasProperty(shaderProperty))
                continue;
            binding.FloatProperties.Add(property);
            binding.Values.Add(material.GetFloat(shaderProperty));
            // A shader should expose only one whole-material opacity scalar. If
            // aliases coexist, the first known property is the least invasive.
            break;
        }
        if (binding.FloatProperties.Count > 0)
            return binding;

        if (!IsTransparentShader(material))
            return binding;
        foreach (string property in ColorOpacityProperties)
        {
            string shaderProperty = "_" + property;
            if (!material.HasProperty(shaderProperty))
                continue;
            binding.ColorProperties.Add(property);
            binding.Values.Add(material.GetColor(shaderProperty).a);
        }
        return binding;
    }

    private static bool IsTransparentShader(Material material)
    {
        string shaderName = material.shader != null ? material.shader.name : string.Empty;
        if (shaderName.IndexOf("alpha", StringComparison.OrdinalIgnoreCase) >= 0
            || shaderName.IndexOf("transparent", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        string renderType = material.GetTag("RenderType", false, string.Empty);
        if (renderType.IndexOf("Transparent", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        bool hasBlendState = material.HasProperty("_SrcBlend") && material.HasProperty("_DstBlend")
            && (Mathf.Abs(material.GetFloat("_SrcBlend") - 1f) > OpacityTolerance
                || Mathf.Abs(material.GetFloat("_DstBlend")) > OpacityTolerance);
        return material.renderQueue >= 3000 && hasBlendState;
    }

    private static void ApplyBinding(
        MaterialEditorBridge bridge,
        object controller,
        MaterialTarget target,
        OpacityBinding binding,
        float opacity)
    {
        foreach (string property in binding.FloatProperties)
            bridge.SetFloat(controller, target, property, opacity);
        foreach (string property in binding.ColorProperties)
        {
            Color color = target.Material.GetColor("_" + property);
            color.a = opacity;
            bridge.SetColor(controller, target, property, color);
        }
    }

    private static bool TryGetCompatibilityShader(
        Material material,
        out string shaderName,
        out bool azPair)
    {
        shaderName = null;
        azPair = false;
        if (material == null || !material.HasProperty("_MainTex"))
            return false;

        string currentShader = material.shader == null
            ? string.Empty
            : material.shader.name;
        if (string.Equals(
                currentShader,
                AzClothCutoutShader,
                StringComparison.OrdinalIgnoreCase))
        {
            shaderName = AzClothAlphaShader;
            azPair = true;
        }
        else if (string.Equals(
                     currentShader,
                     AzItemCutoutShader,
                     StringComparison.OrdinalIgnoreCase))
        {
            shaderName = AzItemAlphaShader;
            azPair = true;
        }
        else if (string.Equals(
                     currentShader,
                     AzLiteCutoutShader,
                     StringComparison.OrdinalIgnoreCase))
        {
            shaderName = AzLiteAlphaShader;
            azPair = true;
        }
        else if (currentShader.StartsWith(
                     "Az/Standard", StringComparison.OrdinalIgnoreCase))
        {
            // Never downgrade an AZ material to the generic Shader Forge
            // fallback.  Only documented same-family Cutout -> Alpha pairs
            // are converted; native AZ Alpha shaders are handled earlier by
            // ResolveBinding.
            return false;
        }
        else
        {
            shaderName = CompatibilityShader;
        }

        // MaterialEditor can resolve AssetBundle shaders that Shader.Find does
        // not expose until the paired variant is first applied.
        if (azPair || Shader.Find(shaderName) != null)
            return true;
        shaderName = null;
        azPair = false;
        return false;
    }

    private static bool HasProtectedMaterialEditorOverride(
        MaterialEditorBridge bridge,
        object controller,
        MaterialTarget target)
    {
        string savedShader = bridge.GetSavedShader(controller, target);
        return !string.IsNullOrEmpty(savedShader)
            || bridge.HasSavedRenderQueue(controller, target);
    }

    private static bool HasConflictingAzMaterialEditorOverride(
        MaterialEditorBridge bridge,
        object controller,
        MaterialTarget target)
    {
        string savedShader = bridge.GetSavedShader(controller, target);
        if (string.IsNullOrEmpty(savedShader))
            return false;
        string runtimeShader = target.Material.shader == null
            ? string.Empty
            : target.Material.shader.name;
        return !string.Equals(
            savedShader,
            runtimeShader,
            StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyTransparentRenderQueue(
        MaterialEditorBridge bridge,
        object controller,
        MaterialTarget target,
        string shaderName)
    {
        Shader shader = FindLoadedShader(shaderName);
        int renderQueue = shader != null && shader.renderQueue >= 0
            ? shader.renderQueue
            : 3000;
        bridge.SetRenderQueue(controller, target, renderQueue);

        // AZ Alpha defaults documented by Az.Shader.Standard v5.6.0. These
        // runtime writes make the first live frame deterministic; the shader's
        // own defaults provide the same values after a saved card is reloaded.
        foreach (Material material in GetLiveMaterials(target))
        {
            material.renderQueue = renderQueue;
            if (material.HasProperty("_BlendSrc"))
                material.SetFloat("_BlendSrc", 5f);
            if (material.HasProperty("_BlendDst"))
                material.SetFloat("_BlendDst", 10f);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 1f);
            if (material.HasProperty("_PREMULTIPLY_ALPHA"))
                material.SetFloat("_PREMULTIPLY_ALPHA", 0f);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
        }
    }

    private static void RestoreCompatibilityShader(
        MaterialEditorBridge bridge,
        object controller,
        MaterialTarget target)
    {
        string key = BuildCompatibilityKey(target);
        CompatibilityOverrideState state;
        if (!CompatibilityStates.TryGetValue(key, out state))
            return;

        foreach (string property in state.TouchedFloats)
        {
            float savedValue;
            if (state.SavedFloats.TryGetValue(property, out savedValue))
                bridge.SetFloat(controller, target, property, savedValue);
            else if (bridge.HasSavedFloat(controller, target, property))
                bridge.RemoveFloat(controller, target, property);
        }
        foreach (string property in state.TouchedColors)
        {
            Color savedValue;
            if (state.SavedColors.TryGetValue(property, out savedValue))
                bridge.SetColor(controller, target, property, savedValue);
            else if (bridge.HasSavedColor(controller, target, property))
                bridge.RemoveColor(controller, target, property);
        }

        if (state.ShaderConverted)
        {
            if (string.IsNullOrEmpty(state.SavedShader))
                bridge.RemoveShader(controller, target);
            else
                bridge.SetShader(controller, target, state.SavedShader);
            if (state.SavedRenderQueue.HasValue)
                bridge.SetRenderQueue(controller, target, state.SavedRenderQueue.Value);
            else
                bridge.RemoveRenderQueue(controller, target);
        }

        foreach (RuntimeMaterialState runtimeState in state.RuntimeMaterials)
            runtimeState.Restore(
                state.ShaderConverted,
                state.TouchedFloats,
                state.TouchedColors);
        state.Dispose();
        CompatibilityStates.Remove(key);
    }

    private static bool IsCompatibilityActive(MaterialTarget target)
    {
        return CompatibilityStates.ContainsKey(BuildCompatibilityKey(target));
    }

    private static void CaptureCompatibilityState(
        MaterialEditorBridge bridge,
        object controller,
        MaterialTarget target,
        bool shaderConverted,
        string appliedShader)
    {
        string key = BuildCompatibilityKey(target);
        if (CompatibilityStates.ContainsKey(key))
            return;

        CompatibilityOverrideState state = new CompatibilityOverrideState
        {
            SavedShader = bridge.GetSavedShader(controller, target),
            SavedRenderQueue = bridge.GetSavedRenderQueue(controller, target),
            ShaderConverted = shaderConverted,
            AppliedShader = appliedShader
        };
        foreach (string property in FloatOpacityProperties)
        {
            float value;
            if (bridge.TryGetSavedFloat(controller, target, property, out value))
                state.SavedFloats[property] = value;
        }
        foreach (string property in ColorOpacityProperties)
        {
            Color value;
            if (bridge.TryGetSavedColor(controller, target, property, out value))
                state.SavedColors[property] = value;
        }
        foreach (Material material in GetLiveMaterials(target))
            state.RuntimeMaterials.Add(RuntimeMaterialState.Capture(
                material, shaderConverted));
        CompatibilityStates[key] = state;
    }

    private static void RestoreConvertedMaterialProperties(MaterialTarget target)
    {
        CompatibilityOverrideState state;
        if (!CompatibilityStates.TryGetValue(
                BuildCompatibilityKey(target), out state)
            || !state.ShaderConverted)
        {
            return;
        }
        foreach (RuntimeMaterialState runtimeState in state.RuntimeMaterials)
            runtimeState.ApplyToConvertedShader();
    }

    private static void MarkTouchedProperties(
        MaterialTarget target,
        OpacityBinding binding)
    {
        CompatibilityOverrideState state;
        if (!CompatibilityStates.TryGetValue(
                BuildCompatibilityKey(target), out state))
        {
            return;
        }
        foreach (string property in binding.FloatProperties)
            state.TouchedFloats.Add(property);
        foreach (string property in binding.ColorProperties)
            state.TouchedColors.Add(property);
    }

    private static string BuildCompatibilityKey(MaterialTarget target)
    {
        return target.Root.GetInstanceID() + "|" + target.Slot + "|" + target.MaterialName;
    }

    private static int ApplyRuntimeFloat(MaterialTarget target, string property, float value)
    {
        int changed = 0;
        string unityProperty = "_" + property;
        foreach (Material material in GetLiveMaterials(target))
        {
            if (!material.HasProperty(unityProperty))
                continue;
            material.SetFloat(unityProperty, value);
            changed++;
        }
        return changed;
    }

    private static int ApplyRuntimeColor(MaterialTarget target, string property, Color value)
    {
        int changed = 0;
        string unityProperty = "_" + property;
        foreach (Material material in GetLiveMaterials(target))
        {
            if (!material.HasProperty(unityProperty))
                continue;
            material.SetColor(unityProperty, value);
            changed++;
        }
        return changed;
    }

    private static int ApplyRuntimeShader(MaterialTarget target, string shaderName)
    {
        int matched = 0;
        Material representative = null;
        Shader shader = FindLoadedShader(shaderName);
        bool requireOpacity = IsAzAlphaShaderName(shaderName);
        foreach (Material material in GetLiveMaterials(target))
        {
            // Do not compare names alone: AssetBundles can expose an incomplete
            // placeholder and the real AZ shader under the same name.
            if (shader != null
                && (material.shader != shader
                    || (requireOpacity && !HasDirectOpacityProperty(material))))
            {
                material.shader = shader;
            }
            if (material.shader == null
                || !string.Equals(
                    material.shader.name,
                    shaderName,
                    StringComparison.OrdinalIgnoreCase)
                || (requireOpacity && !HasDirectOpacityProperty(material)))
            {
                continue;
            }
            if (representative == null)
                representative = material;
            matched++;
        }
        if (representative != null)
            target.Material = representative;
        return matched;
    }

    private static Shader FindLoadedShader(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
            return null;

        bool requireOpacity = IsAzAlphaShaderName(shaderName);
        Shader cached;
        if (LoadedShaderCache.TryGetValue(shaderName, out cached)
            && cached != null
            && (!requireOpacity || ShaderExposesOpacity(cached)))
        {
            return cached;
        }

        // AssetBundles can leave several Shader objects with the same name.
        // Prefer one that is already driving a live material and, for AZ Alpha,
        // prove that the actual shader exposes a whole-material alpha scalar.
        foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (material == null || material.shader == null)
                continue;
            if (!string.Equals(
                    material.shader.name,
                    shaderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (requireOpacity && !HasDirectOpacityProperty(material))
                continue;
            LoadedShaderCache[shaderName] = material.shader;
            return material.shader;
        }

        Shader shader = Shader.Find(shaderName);
        if (shader != null && (!requireOpacity || ShaderExposesOpacity(shader)))
        {
            LoadedShaderCache[shaderName] = shader;
            return shader;
        }
        foreach (Shader candidate in Resources.FindObjectsOfTypeAll<Shader>())
        {
            if (candidate != null
                && string.Equals(candidate.name, shaderName, StringComparison.OrdinalIgnoreCase)
                && (!requireOpacity || ShaderExposesOpacity(candidate)))
            {
                LoadedShaderCache[shaderName] = candidate;
                return candidate;
            }
        }
        return null;
    }

    private static bool IsAzAlphaShaderName(string shaderName)
    {
        return string.Equals(shaderName, AzClothAlphaShader, StringComparison.OrdinalIgnoreCase)
            || string.Equals(shaderName, AzItemAlphaShader, StringComparison.OrdinalIgnoreCase)
            || string.Equals(shaderName, AzLiteAlphaShader, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasDirectOpacityProperty(Material material)
    {
        if (material == null)
            return false;
        foreach (string property in FloatOpacityProperties)
        {
            if (material.HasProperty("_" + property))
                return true;
        }
        return false;
    }

    private static bool ShaderExposesOpacity(Shader shader)
    {
        if (shader == null)
            return false;
        Material probe = null;
        try
        {
            probe = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return HasDirectOpacityProperty(probe);
        }
        finally
        {
            if (probe != null)
                Object.Destroy(probe);
        }
    }

    private static IEnumerable<Material> GetLiveMaterials(MaterialTarget target)
    {
        HashSet<int> seen = new HashSet<int>();
        foreach (Renderer renderer in target.Root.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null)
                continue;
            foreach (Material material in renderer.materials)
            {
                if (material == null
                    || !string.Equals(
                        NormalizeMaterialName(material.name),
                        target.MaterialName,
                        StringComparison.OrdinalIgnoreCase)
                    || !seen.Add(material.GetInstanceID()))
                {
                    continue;
                }
                yield return material;
            }
        }
    }

    private static void TryRollbackCompatibilityShader(
        MaterialEditorBridge bridge,
        object controller,
        MaterialTarget target)
    {
        try
        {
            RestoreCompatibilityShader(bridge, controller, target);
        }
        catch (Exception rollbackError)
        {
            Debug.LogWarning("[KK VR] Unable to roll back compatibility shader for "
                + target.MaterialName + ": " + Unwrap(rollbackError).Message);
        }
    }

    private static string NormalizeMaterialName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;
        string normalized = value.Trim();
        const string instance = " (Instance)";
        if (normalized.EndsWith(instance, StringComparison.Ordinal))
            normalized = normalized.Substring(0, normalized.Length - instance.Length);
        int copy = normalized.IndexOf(".MECopy", StringComparison.OrdinalIgnoreCase);
        if (copy >= 0)
            normalized = normalized.Substring(0, copy);
        return normalized;
    }

    private static Exception Unwrap(Exception ex)
    {
        while (ex is TargetInvocationException invocation && invocation.InnerException != null)
            ex = invocation.InnerException;
        return ex;
    }

    private sealed class OpacityBinding
    {
        public readonly List<string> FloatProperties = new List<string>();
        public readonly List<string> ColorProperties = new List<string>();
        public readonly List<float> Values = new List<float>();
        public bool IsSupported => FloatProperties.Count > 0 || ColorProperties.Count > 0;
    }

    private sealed class CompatibilityOverrideState
    {
        public string SavedShader;
        public int? SavedRenderQueue;
        public bool ShaderConverted;
        public string AppliedShader;
        public readonly Dictionary<string, float> SavedFloats =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, Color> SavedColors =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> TouchedFloats =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> TouchedColors =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public readonly List<RuntimeMaterialState> RuntimeMaterials =
            new List<RuntimeMaterialState>();

        public void Dispose()
        {
            foreach (RuntimeMaterialState runtimeState in RuntimeMaterials)
                runtimeState.Dispose();
        }
    }

    private sealed class RuntimeMaterialState
    {
        public Material Material;
        public Material FullSnapshot;
        public Shader Shader;
        public int RenderQueue;
        public string[] ShaderKeywords;
        public readonly Dictionary<string, float> Floats =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, Color> Colors =
            new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        public static RuntimeMaterialState Capture(
            Material material,
            bool captureFullSnapshot)
        {
            RuntimeMaterialState state = new RuntimeMaterialState
            {
                Material = material,
                FullSnapshot = captureFullSnapshot ? new Material(material) : null,
                Shader = material.shader,
                RenderQueue = material.renderQueue,
                ShaderKeywords = material.shaderKeywords == null
                    ? new string[0]
                    : (string[])material.shaderKeywords.Clone()
            };
            if (state.FullSnapshot != null)
                state.FullSnapshot.hideFlags = HideFlags.HideAndDontSave;
            foreach (string property in FloatOpacityProperties)
            {
                string unityProperty = "_" + property;
                if (material.HasProperty(unityProperty))
                    state.Floats[property] = material.GetFloat(unityProperty);
            }
            foreach (string property in ColorOpacityProperties)
            {
                string unityProperty = "_" + property;
                if (material.HasProperty(unityProperty))
                    state.Colors[property] = material.GetColor(unityProperty);
            }
            return state;
        }

        public void ApplyToConvertedShader()
        {
            if (Material == null || FullSnapshot == null)
                return;
            // Unity 5.6 can retain the source shader's native property layout
            // after CopyPropertiesFromMaterial even when the displayed shader
            // name remains the AZ Alpha name. Rebinding the exact validated
            // Shader object rebuilds the Alpha layout while retaining the
            // copied common PBR, texture and UV values by property name.
            Shader convertedShader = Material.shader;
            Material.CopyPropertiesFromMaterial(FullSnapshot);
            if (convertedShader != null)
                Material.shader = convertedShader;
        }

        public void Restore(
            bool shaderConverted,
            ICollection<string> touchedFloats,
            ICollection<string> touchedColors)
        {
            if (Material == null)
                return;
            if (shaderConverted && Shader != null)
                Material.shader = Shader;
            if (shaderConverted && FullSnapshot != null)
                Material.CopyPropertiesFromMaterial(FullSnapshot);
            Material.renderQueue = RenderQueue;
            Material.shaderKeywords = ShaderKeywords == null
                ? new string[0]
                : (string[])ShaderKeywords.Clone();
            foreach (string property in touchedFloats)
            {
                float value;
                if (!Floats.TryGetValue(property, out value))
                    continue;
                string unityProperty = "_" + property;
                if (Material.HasProperty(unityProperty))
                    Material.SetFloat(unityProperty, value);
            }
            foreach (string property in touchedColors)
            {
                Color value;
                if (!Colors.TryGetValue(property, out value))
                    continue;
                string unityProperty = "_" + property;
                if (Material.HasProperty(unityProperty))
                    Material.SetColor(unityProperty, value);
            }
        }

        public void Dispose()
        {
            if (FullSnapshot == null)
                return;
            Object.Destroy(FullSnapshot);
            FullSnapshot = null;
        }
    }

    private sealed class RuntimeCueMaterialState
    {
        public int PartId;
        public Material Material;
        public Material Snapshot;
        public static RuntimeCueMaterialState Capture(int partId, Material material)
        {
            Material snapshot = new Material(material)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            return new RuntimeCueMaterialState
            {
                PartId = partId,
                Material = material,
                Snapshot = snapshot
            };
        }

        public void RestoreAndDispose()
        {
            if (Material != null && Snapshot != null)
            {
                Shader originalShader = Snapshot.shader;
                if (originalShader != null)
                    Material.shader = originalShader;
                Material.CopyPropertiesFromMaterial(Snapshot);
                if (originalShader != null)
                    Material.shader = originalShader;
                Material.renderQueue = Snapshot.renderQueue;
                Material.shaderKeywords = Snapshot.shaderKeywords == null
                    ? new string[0]
                    : (string[])Snapshot.shaderKeywords.Clone();
            }
            Dispose();
        }

        public void Dispose()
        {
            if (Snapshot == null)
                return;
            Object.Destroy(Snapshot);
            Snapshot = null;
        }
    }

    private sealed class MaterialTarget
    {
        public readonly int Slot;
        public readonly GameObject Root;
        public Material Material;
        public readonly string MaterialName;

        public MaterialTarget(int slot, GameObject root, Material material, string materialName)
        {
            Slot = slot;
            Root = root;
            Material = material;
            MaterialName = materialName;
        }
    }

    private sealed class MaterialEditorBridge
    {
        private readonly MethodInfo _getController;
        private readonly object _clothingObjectType;
        private readonly MethodInfo _setFloat;
        private readonly MethodInfo _getFloat;
        private readonly MethodInfo _removeFloat;
        private readonly MethodInfo _setColor;
        private readonly MethodInfo _getColor;
        private readonly MethodInfo _removeColor;
        private readonly MethodInfo _setShader;
        private readonly MethodInfo _getShader;
        private readonly MethodInfo _removeShader;
        private readonly MethodInfo _getRenderQueue;
        private readonly MethodInfo _setRenderQueue;
        private readonly MethodInfo _removeRenderQueue;
        private readonly FieldInfo _loadedShaders;
        private readonly ConstructorInfo _shaderDataConstructor;

        private MaterialEditorBridge(
            Type pluginType,
            Type controllerType,
            Type objectType,
            Type pluginBaseType)
        {
            _getController = RequireMethod(pluginType, "GetCharaController", 1);
            _clothingObjectType = Enum.Parse(objectType, "Clothing");
            _setFloat = RequireMethod(controllerType, "SetMaterialFloatProperty", 7);
            _getFloat = RequireMethod(controllerType, "GetMaterialFloatPropertyValue", 5);
            _removeFloat = RequireMethod(controllerType, "RemoveMaterialFloatProperty", 6);
            _setColor = RequireMethod(controllerType, "SetMaterialColorProperty", 7);
            _getColor = RequireMethod(controllerType, "GetMaterialColorPropertyValue", 5);
            _removeColor = RequireMethod(controllerType, "RemoveMaterialColorProperty", 6);
            _setShader = RequireMethod(controllerType, "SetMaterialShader", 6);
            _getShader = RequireMethod(controllerType, "GetMaterialShader", 4);
            _removeShader = RequireMethod(controllerType, "RemoveMaterialShader", 5);
            _getRenderQueue = RequireMethod(controllerType, "GetMaterialShaderRenderQueue", 4);
            _setRenderQueue = RequireMethod(controllerType, "SetMaterialShaderRenderQueue", 6);
            _removeRenderQueue = RequireMethod(controllerType, "RemoveMaterialShaderRenderQueue", 5);
            _loadedShaders = pluginBaseType.GetField(
                "LoadedShaders",
                BindingFlags.Public | BindingFlags.Static);
            Type shaderDataType = pluginBaseType.GetNestedType(
                "ShaderData",
                BindingFlags.Public);
            if (_loadedShaders == null || shaderDataType == null)
                throw new MissingMemberException(pluginBaseType.FullName, "LoadedShaders/ShaderData");
            _shaderDataConstructor = shaderDataType.GetConstructor(new[]
            {
                typeof(Shader), typeof(string), typeof(string), typeof(string)
            });
            if (_shaderDataConstructor == null)
                throw new MissingMethodException(shaderDataType.FullName, ".ctor");
        }

        public static MaterialEditorBridge TryCreate()
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type pluginType = assembly.GetType("KK_Plugins.MaterialEditor.MaterialEditorPlugin", false);
                Type controllerType = assembly.GetType(
                    "KK_Plugins.MaterialEditor.MaterialEditorCharaController", false);
                Type pluginBaseType = assembly.GetType(
                    "MaterialEditorAPI.MaterialEditorPluginBase", false);
                if (pluginType == null || controllerType == null || pluginBaseType == null)
                    continue;
                Type objectType = controllerType.GetNestedType("ObjectType", BindingFlags.Public);
                if (objectType != null)
                    return new MaterialEditorBridge(
                        pluginType,
                        controllerType,
                        objectType,
                        pluginBaseType);
            }
            return null;
        }

        public bool TryGetController(ChaControl character, out object controller, out string status)
        {
            controller = _getController.Invoke(null, new object[] { character });
            if (controller == null || controller is Object unityObject && unityObject == null)
            {
                status = "MaterialEditor 角色控制器尚未初始化";
                controller = null;
                return false;
            }
            status = null;
            return true;
        }

        public void SetFloat(
            object controller,
            MaterialTarget target,
            string property,
            float value)
        {
            _setFloat.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, value, target.Root, true
            });
            if (ApplyRuntimeFloat(target, property, value) == 0)
                throw new InvalidOperationException("材质没有可写入的 _" + property + " 属性");
        }

        public bool HasSavedFloat(object controller, MaterialTarget target, string property)
        {
            object value = _getFloat.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, target.Root
            });
            return value != null;
        }

        public bool TryGetSavedFloat(
            object controller,
            MaterialTarget target,
            string property,
            out float value)
        {
            object saved = _getFloat.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, target.Root
            });
            if (saved == null)
            {
                value = 0f;
                return false;
            }
            value = Convert.ToSingle(saved, CultureInfo.InvariantCulture);
            return true;
        }

        public void RemoveFloat(object controller, MaterialTarget target, string property)
        {
            _removeFloat.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, target.Root, true
            });
        }

        public void SetColor(
            object controller,
            MaterialTarget target,
            string property,
            Color value)
        {
            _setColor.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, value, target.Root, true
            });
            if (ApplyRuntimeColor(target, property, value) == 0)
                throw new InvalidOperationException("材质没有可写入的 _" + property + " 颜色属性");
        }

        public bool HasSavedColor(object controller, MaterialTarget target, string property)
        {
            object value = _getColor.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, target.Root
            });
            return value != null;
        }

        public bool TryGetSavedColor(
            object controller,
            MaterialTarget target,
            string property,
            out Color value)
        {
            object saved = _getColor.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, target.Root
            });
            if (saved is Color color)
            {
                value = color;
                return true;
            }
            value = default(Color);
            return false;
        }

        public void RemoveColor(object controller, MaterialTarget target, string property)
        {
            _removeColor.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, property, target.Root, true
            });
        }

        public void SetShader(object controller, MaterialTarget target, string shaderName)
        {
            if (!EnsureShaderAvailable(shaderName))
            {
                throw new InvalidOperationException(
                    "找不到已加载的透明 Shader：" + shaderName);
            }
            _setShader.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, shaderName, target.Root, true
            });
            if (ApplyRuntimeShader(target, shaderName) == 0)
                throw new InvalidOperationException("透明 Shader 未能应用：" + shaderName);
        }

        private bool EnsureShaderAvailable(string shaderName)
        {
            Shader shader = FindLoadedShader(shaderName);
            if (shader == null)
                return false;

            IDictionary loadedShaders = _loadedShaders.GetValue(null) as IDictionary;
            if (loadedShaders == null)
                return false;

            object current = loadedShaders[shaderName];
            if (current != null)
            {
                FieldInfo shaderField = current.GetType().GetField(
                    "Shader",
                    BindingFlags.Public | BindingFlags.Instance);
                if (shaderField != null)
                {
                    Shader registered = shaderField.GetValue(current) as Shader;
                    bool azAlpha = IsAzAlphaShaderName(shaderName);
                    if (registered != null
                        && (!azAlpha || ShaderExposesOpacity(registered)))
                    {
                        return true;
                    }
                    shaderField.SetValue(current, shader);
                    VRGIN.Core.VRLog.Info(
                        "[KK VR] Replaced incomplete MaterialEditor shader entry: "
                        + shaderName);
                    return true;
                }
            }

            object shaderData = _shaderDataConstructor.Invoke(new object[]
            {
                shader,
                shaderName,
                "3000",
                "false"
            });
            loadedShaders[shaderName] = shaderData;
            VRGIN.Core.VRLog.Info("[KK VR] Registered loaded AZ shader with MaterialEditor: "
                + shaderName);
            return true;
        }

        public string GetSavedShader(object controller, MaterialTarget target)
        {
            return _getShader.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, target.Root
            }) as string;
        }

        public void RemoveShader(object controller, MaterialTarget target)
        {
            _removeShader.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, target.Root, true
            });
        }

        public bool HasSavedRenderQueue(object controller, MaterialTarget target)
        {
            object value = _getRenderQueue.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, target.Root
            });
            return value != null;
        }

        public int? GetSavedRenderQueue(object controller, MaterialTarget target)
        {
            object value = _getRenderQueue.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, target.Root
            });
            return value == null ? (int?)null : System.Convert.ToInt32(value);
        }

        public void SetRenderQueue(object controller, MaterialTarget target, int value)
        {
            _setRenderQueue.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, value, target.Root, true
            });
        }

        public void RemoveRenderQueue(object controller, MaterialTarget target)
        {
            _removeRenderQueue.Invoke(controller, new object[]
            {
                target.Slot, _clothingObjectType, target.Material, target.Root, true
            });
        }

        private static MethodInfo RequireMethod(Type type, string name, int parameterCount)
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static
                    | BindingFlags.Instance))
            {
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                    return method;
            }
            throw new MissingMethodException(type.FullName, name);
        }
    }
}

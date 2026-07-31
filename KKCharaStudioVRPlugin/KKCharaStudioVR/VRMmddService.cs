using System;
using System.IO;
using System.Globalization;
using System.Text;

namespace KKCharaStudioVR;

internal static class VRMmddService
{
    public const string DefaultVmdRoot = @"E:\action";
    public const float DefaultFixedFov = 53.13f;
    public const float MinFixedFov = 20f;
    public const float MaxFixedFov = 120f;

    public static bool TogglePlayPause(out string status)
    {
        string code =
            BuildBootstrapCode()
            + "game.gdata.mmdd.startStop()\n";

        string error;
        if (VRPythonBridge.TryExecute(code, "MMDD play/pause", out error))
        {
            status = "MMDD 播放状态已切换";
            return true;
        }

        status = "MMDD 无法控制：" + error;
        return false;
    }

    public static bool RefreshFixedFov(out string status)
    {
        return ExecuteFixedFovAction(string.Empty, "MMDD Fixed FOV state", out status);
    }

    public static bool ToggleFixedFov(out string status)
    {
        return ExecuteFixedFovAction(
            "config.useFixedFOV = not current_fixed_fov\n",
            "MMDD Fixed FOV toggle",
            out status);
    }

    public static bool SetFixedFov(float value, out string status)
    {
        value = Math.Max(MinFixedFov, Math.Min(MaxFixedFov, value));
        return ExecuteFixedFovAction(
            "config.useFixedFOV = current_fixed_fov\n"
            + "config.useFixedFOVValue = "
            + value.ToString("R", CultureInfo.InvariantCulture)
            + "\n",
            "MMDD Fixed FOV value",
            out status);
    }

    public static bool RefreshHighHeels(int objectKey, out string status)
    {
        return ExecuteHighHeelsAction(objectKey, string.Empty, "MMDD high heels state", out status);
    }

    public static bool ToggleHighHeelsMode(int objectKey, out string status)
    {
        const string action =
            "if mc.HighHeels.enable:\n"
            + "    mc.HighHeels.refreshHeelInfo()\n"
            + "    mc.enableHeelzCtrl(False)\n"
            + "else:\n"
            + "    mc.enableHeelzCtrl(True)\n";
        return ExecuteHighHeelsAction(objectKey, action, "MMDD high heels mode", out status);
    }

    public static bool ResetHighHeels(int objectKey, out string status)
    {
        const string action =
            "if mc.HighHeels.enable:\n"
            + "    raise Exception('Switch High Heels to manual mode first')\n"
            + "mc.HighHeels.refreshHeelInfo()\n"
            + "defaults = mc.HighHeels.defaultRotate\n"
            + "mc.setHeelzRotation(defaults[0], defaults[1], defaults[2])\n";
        return ExecuteHighHeelsAction(objectKey, action, "MMDD high heels reset", out status);
    }

    public static bool ToggleHighHeelsShoesDetect(int objectKey, out string status)
    {
        const string action =
            "if mc.HighHeels.enable:\n"
            + "    raise Exception('Switch High Heels to manual mode first')\n"
            + "mc.ShoesDetect = not mc.ShoesDetect\n";
        return ExecuteHighHeelsAction(objectKey, action, "MMDD shoe detection toggle", out status);
    }

    public static bool ToggleShoesOffset(int objectKey, out string status)
    {
        const string action =
            "mc.enableShoesOffsetCtrl(not mc.ShoesOffset[0])\n";
        return ExecuteHighHeelsAction(objectKey, action, "MMDD shoe offset toggle", out status);
    }

    public static bool AdjustHighHeelsRotation(
        int objectKey,
        int component,
        float delta,
        out string status)
    {
        if (component < 0 || component > 2)
        {
            status = "未知的高跟鞋角度";
            return false;
        }

        string[] arguments = { "value, None, None", "None, value, None", "None, None, value" };
        string action =
            "if mc.HighHeels.enable:\n"
            + "    raise Exception('Switch High Heels to manual mode first')\n"
            + "rot = mc.getHeelzRotation()\n"
            + "value = max(-180.0, min(180.0, rot["
            + component
            + "] + "
            + delta.ToString("R", CultureInfo.InvariantCulture)
            + "))\n"
            + "mc.setHeelzRotation("
            + arguments[component]
            + ")\n";
        return ExecuteHighHeelsAction(objectKey, action, "MMDD high heels angle", out status);
    }

    public static bool AdjustShoesOffset(
        int objectKey,
        bool shoesOn,
        float delta,
        out string status)
    {
        int index = shoesOn ? 1 : 2;
        string action =
            "mc.ShoesOffset["
            + index
            + "] = max(-1.0, min(1.0, mc.ShoesOffset["
            + index
            + "] + "
            + delta.ToString("R", CultureInfo.InvariantCulture)
            + "))\n"
            + "if mc.ShoesOffset[0]:\n"
            + "    mc.setShoesOffset()\n";
        return ExecuteHighHeelsAction(objectKey, action, "MMDD shoe offset value", out status);
    }

    public static bool LoadVmdPackage(
        string motionPath,
        string cameraPath,
        string audioPath,
        int[] actorObjectKeys,
        out string status)
    {
        if (!ValidateOptionalFile(motionPath, ".vmd", out status)
            || !ValidateOptionalFile(cameraPath, ".vmd", out status)
            || !ValidateOptionalAudio(audioPath, out status))
        {
            return false;
        }

        if (string.IsNullOrEmpty(motionPath) && string.IsNullOrEmpty(cameraPath))
        {
            status = "没有选择动作或镜头 VMD";
            return false;
        }

        string code = BuildLoadPackageCode(
            motionPath,
            cameraPath,
            audioPath,
            actorObjectKeys);

        string error;
        if (!VRPythonBridge.TryExecute(code, "MMDD wrist VMD load", out error))
        {
            status = "VMD 载入失败：" + error;
            return false;
        }

        string motionName = FileNameWithoutExtension(motionPath);
        string cameraName = FileNameWithoutExtension(cameraPath);
        string audioName = FileNameWithoutExtension(audioPath);
        StringBuilder message = new StringBuilder("已载入");
        if (!string.IsNullOrEmpty(motionName))
            message.Append(" 动作：").Append(motionName);
        if (!string.IsNullOrEmpty(cameraName))
            message.Append(" 镜头：").Append(cameraName);
        if (!string.IsNullOrEmpty(audioName))
            message.Append(" 音频：").Append(audioName);
        status = message.ToString();
        return true;
    }

    private static string BuildBootstrapCode()
    {
        return
            "from vngameengine import vnge_game\n"
            + "import mmddirector\n"
            + "game = vnge_game\n"
            + "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n"
            + "    mmddirector.start(game)\n"
            + "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n"
            + "    raise Exception('MMD Director could not be started')\n"
            + "mmdd = game.gdata.mmdd\n"
            + "config = game.gdata.mmdd_config\n"
            + "config.vmdHomeDir = " + ToPythonString(DefaultVmdRoot) + "\n"
            + "game.visible = 0\n";
    }

    private static string BuildLoadPackageCode(
        string motionPath,
        string cameraPath,
        string audioPath,
        int[] actorObjectKeys)
    {
        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append("from vmdlib import VmdLib\n");
        code.Append("motion_path = ").Append(ToPythonString(motionPath)).Append("\n");
        code.Append("camera_path = ").Append(ToPythonString(cameraPath)).Append("\n");
        code.Append("audio_path = ").Append(ToPythonString(audioPath)).Append("\n");
        code.Append("target_keys = ").Append(ToPythonIntList(actorObjectKeys)).Append("\n");
        code.Append("motion_result = None\n");
        code.Append("camera_result = None\n");
        code.Append("actors = []\n");
        code.Append("if motion_path:\n");
        code.Append("    motion_result = VmdLib.loadVmd(motion_path)\n");
        code.Append("    if len(motion_result) == 1:\n");
        code.Append("        raise Exception(motion_result[0])\n");
        code.Append("    all_actors = [x.as_actor for x in game.scene_get_all_females()] + [x.as_actor for x in game.scene_get_all_males()]\n");
        code.Append("    if len(target_keys) > 0:\n");
        code.Append("        actors = [actor for actor in all_actors if actor.objctrl.objectInfo.dicKey in target_keys]\n");
        code.Append("    else:\n");
        code.Append("        selected_nodes = tuple(game.studio.treeNodeCtrl.selectNodes)\n");
        code.Append("        actors = [actor for actor in all_actors if actor.treeNodeObject in selected_nodes]\n");
        code.Append("    if len(actors) == 0 and len(all_actors) == 1:\n");
        code.Append("        actors = all_actors\n");
        code.Append("    if (len(motion_result[1]) > 0 or len(motion_result[2]) > 0 or len(motion_result[4]) > 0) and len(actors) == 0:\n");
        code.Append("        raise Exception('Select at least one character in Studio before loading a motion VMD')\n");
        code.Append("if camera_path:\n");
        code.Append("    camera_result = motion_result if camera_path == motion_path else VmdLib.loadVmd(camera_path)\n");
        code.Append("    if len(camera_result) == 1:\n");
        code.Append("        raise Exception(camera_result[0])\n");
        code.Append("    if len(camera_result[3]) == 0:\n");
        code.Append("        raise Exception('The selected camera VMD has no camera keyframes')\n");
        code.Append("if motion_result:\n");
        code.Append("    for actor in actors:\n");
        code.Append("        if len(motion_result[1]) > 0 or len(motion_result[4]) > 0:\n");
        code.Append("            motion_ctrl = mmdd.createMotionController(actor)\n");
        code.Append("            motion_ctrl.importBoneKeyframes(motion_path, motion_result[1])\n");
        code.Append("            motion_ctrl.importIkKeyframes(motion_result[4])\n");
        code.Append("            mmdd.postProcessForMotionImport(actor)\n");
        code.Append("        if len(motion_result[2]) > 0:\n");
        code.Append("            morph_ctrl = mmdd.createMorphController(actor)\n");
        code.Append("            morph_ctrl.importMorphKeyframes(motion_path, motion_result[2])\n");
        code.Append("    mmdd.lastVmdFilename = motion_path\n");
        code.Append("if camera_result:\n");
        code.Append("    mmdd.clearCameraControllers()\n");
        code.Append("    camera_ctrl = mmdd.loadCameraKeyframes(camera_path, camera_result[3])\n");
        code.Append("    if camera_ctrl is None:\n");
        code.Append("        raise Exception('MMDD could not create the camera controller')\n");
        code.Append("    camera_ctrl.switchMinFramePeriod = config.switchMinFramePeriod\n");
        code.Append("    camera_ctrl.switchMaxFramePeriod = config.switchMaxFramePeriod\n");
        code.Append("    camera_ctrl.switchProbability = config.switchProbability\n");
        code.Append("    camera_ctrl.fixedFOV = config.useFixedFOVValue\n");
        code.Append("    if config.useFixedFOV:\n");
        code.Append("        camera_ctrl.postProcess.add('fixed_fov')\n");
        code.Append("    mmdd.lastVmdFilename = camera_path if not motion_path else motion_path\n");
        code.Append("if audio_path:\n");
        code.Append("    audio_error = mmdd.loadWavFile(audio_path, False)\n");
        code.Append("    if audio_error:\n");
        code.Append("        raise Exception('Audio load failed: ' + str(audio_error))\n");
        code.Append("mmdd.refreshTotalFrame()\n");
        code.Append("mmdd.verify()\n");
        code.Append("mmdd.update(True)\n");
        code.Append("game.visible = 0\n");
        return code.ToString();
    }

    private static bool ExecuteFixedFovAction(
        string action,
        string operation,
        out string status)
    {
        VRMmddStateBridge.ResetFixedFov();
        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append(BuildStateBridgeImportCode());
        code.Append(BuildCurrentFixedFovCode());
        code.Append(action);
        if (!string.IsNullOrEmpty(action))
        {
            code.Append("for cc in mmdd.cameraControllers:\n");
            code.Append("    if hasattr(cc, 'postProcess'):\n");
            code.Append("        cc.fixedFOV = config.useFixedFOVValue\n");
            code.Append("        if config.useFixedFOV:\n");
            code.Append("            cc.postProcess.add('fixed_fov')\n");
            code.Append("        else:\n");
            code.Append("            cc.postProcess.discard('fixed_fov')\n");
            code.Append("if not mmdd.isPlaying:\n");
            code.Append("    mmdd.update(True)\n");
            code.Append("mmddirector.saveConfig(game)\n");
        }
        code.Append(BuildFixedFovReportCode());

        string error;
        if (!VRPythonBridge.TryExecute(code.ToString(), operation, out error))
        {
            status = "Fixed FOV 无法控制：" + error;
            return false;
        }

        if (!VRMmddStateBridge.FixedFovReported)
        {
            status = "MMDD 没有返回 Fixed FOV 状态";
            return false;
        }

        status = "Fixed FOV "
            + (VRMmddStateBridge.FixedFovEnabled ? "已开启  " : "已关闭  ")
            + VRMmddStateBridge.FixedFovValue.ToString("F2", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool ExecuteHighHeelsAction(
        int objectKey,
        string action,
        string operation,
        out string status)
    {
        VRVmdActorTarget target;
        if (!VRVmdTargetService.TryGetTarget(objectKey, out target, out status))
            return false;
        string targetName = target.DisplayName;

        VRMmddStateBridge.ResetHighHeels();
        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append(BuildStateBridgeImportCode());
        code.Append("target_key = ").Append(objectKey).Append("\n");
        code.Append("all_actors = [x.as_actor for x in game.scene_get_all_females()] + [x.as_actor for x in game.scene_get_all_males()]\n");
        code.Append("target_actors = [actor for actor in all_actors if actor.objctrl.objectInfo.dicKey == target_key]\n");
        code.Append("if len(target_actors) != 1:\n");
        code.Append("    raise Exception('The selected Studio character is no longer available')\n");
        code.Append("mc = mmdd.getMotionController(target_actors[0])\n");
        code.Append("if mc is None:\n");
        code.Append("    raise Exception('Load a motion VMD for this character first')\n");
        code.Append("if mc.HighHeels is None:\n");
        code.Append("    raise Exception('This MMDD motion controller has no High Heels support')\n");
        code.Append(action);
        // High-heel setters apply immediately. MMDD start() updates the camera when Play is pressed.
        code.Append("rot = mc.getHeelzRotation()\n");
        code.Append("VRMmddStateBridge.ReportHighHeels(mc.CharName, mc.HighHeels.PluginName or 'Unknown', bool(mc.HighHeels.enable), bool(mc.ShoesDetect), float(rot[0]), float(rot[1]), float(rot[2]), bool(mc.ShoesOffset[0]), float(mc.ShoesOffset[1]), float(mc.ShoesOffset[2]))\n");

        string error;
        if (!VRPythonBridge.TryExecute(code.ToString(), operation, out error))
        {
            status = targetName + "：" + TranslateHighHeelsError(error);
            return false;
        }

        if (!VRMmddStateBridge.HighHeelsReported)
        {
            status = targetName + "：MMDD 没有返回高跟鞋状态";
            return false;
        }

        status = VRMmddStateBridge.HighHeelsTargetName
            + "：高跟鞋 "
            + (VRMmddStateBridge.HighHeelsAutoMode ? "自动" : "手动");
        return true;
    }

    private static string BuildStateBridgeImportCode()
    {
        return
            "import clr\n"
            + "clr.AddReference('KKCharaStudioVRPlugin')\n"
            + "from KKCharaStudioVR import VRMmddStateBridge\n";
    }

    private static string BuildFixedFovReportCode()
    {
        return
            BuildCurrentFixedFovCode()
            + "VRMmddStateBridge.ReportFixedFov(bool(current_fixed_fov), float(current_fixed_fov_value), int(len(mmdd.cameraControllers)))\n";
    }

    private static string BuildCurrentFixedFovCode()
    {
        return
            "current_fixed_fov = bool(config.useFixedFOV)\n"
            + "current_fixed_fov_value = float(config.useFixedFOVValue)\n"
            + "for current_cc in mmdd.cameraControllers:\n"
            + "    if hasattr(current_cc, 'postProcess'):\n"
            + "        current_fixed_fov = 'fixed_fov' in current_cc.postProcess\n"
            + "        current_fixed_fov_value = float(current_cc.fixedFOV)\n"
            + "        break\n";
    }

    private static string TranslateHighHeelsError(string error)
    {
        if (string.IsNullOrEmpty(error))
            return "高跟鞋控制失败";
        if (error.IndexOf("Load a motion VMD", StringComparison.OrdinalIgnoreCase) >= 0)
            return "请先给这个角色读取动作 VMD";
        if (error.IndexOf("manual mode", StringComparison.OrdinalIgnoreCase) >= 0)
            return "请先切换到手动骨骼调节";
        if (error.IndexOf("no High Heels support", StringComparison.OrdinalIgnoreCase) >= 0)
            return "当前动作控制器不支持高跟鞋调节";
        return "高跟鞋控制失败：" + error;
    }

    private static bool ValidateOptionalFile(string path, string extension, out string status)
    {
        status = null;
        if (string.IsNullOrEmpty(path))
            return true;
        if (!File.Exists(path)
            || !string.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase))
        {
            status = "VMD 文件不存在或格式无效";
            return false;
        }
        return true;
    }

    private static bool ValidateOptionalAudio(string path, out string status)
    {
        status = null;
        if (string.IsNullOrEmpty(path))
            return true;
        if (!File.Exists(path))
        {
            status = "匹配到的音频文件不存在";
            return false;
        }

        string extension = Path.GetExtension(path);
        if (!string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".ogg", StringComparison.OrdinalIgnoreCase))
        {
            status = "音频格式不受 MMDD 支持";
            return false;
        }
        return true;
    }

    private static string ToPythonString(string value)
    {
        if (value == null)
            return "None";

        StringBuilder escaped = new StringBuilder(value.Length + 8);
        escaped.Append("u'");
        foreach (char character in value)
        {
            switch (character)
            {
                case '\\':
                    escaped.Append("\\\\");
                    break;
                case '\'':
                    escaped.Append("\\'");
                    break;
                case '\r':
                    escaped.Append("\\r");
                    break;
                case '\n':
                    escaped.Append("\\n");
                    break;
                default:
                    escaped.Append(character);
                    break;
            }
        }
        escaped.Append('\'');
        return escaped.ToString();
    }

    private static string ToPythonIntList(int[] values)
    {
        if (values == null || values.Length == 0)
            return "[]";

        StringBuilder result = new StringBuilder("[");
        for (int index = 0; index < values.Length; index++)
        {
            if (index > 0)
                result.Append(',');
            result.Append(values[index]);
        }
        result.Append(']');
        return result.ToString();
    }

    private static string FileNameWithoutExtension(string path)
    {
        return string.IsNullOrEmpty(path) ? null : Path.GetFileNameWithoutExtension(path);
    }
}

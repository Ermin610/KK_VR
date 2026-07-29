using System;
using System.IO;
using System.Text;

namespace KKCharaStudioVR;

internal static class VRMmddService
{
    public const string DefaultVmdRoot = @"E:\action";

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

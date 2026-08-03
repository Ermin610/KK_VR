using System;
using System.IO;
using System.Globalization;
using System.Text;
using VRGIN.Core;

namespace KKCharaStudioVR;

internal sealed class VRMmddCharacterReplacementSession
{
    public string Token;
    public int ObjectKey;
    public string MotionFile;
    public string MorphFile;
    public bool WasPlaying;
    public float Frame;
}

internal static class VRMmddService
{
    public const string SuggestedVmdRoot = "";
    public const float DefaultFixedFov = 53.13f;
    public const float MinFixedFov = 20f;
    public const float MaxFixedFov = 120f;
    private static int _characterReplacementSequence;

    public static bool TryPrepareCharacterReplacement(
        int objectKey,
        out VRMmddCharacterReplacementSession session,
        out string status)
    {
        session = null;

        VRVmdActorTarget target;
        if (!VRVmdTargetService.TryGetTarget(objectKey, out target, out status))
            return false;

        string token = "kkvr-character-replace-"
            + (++_characterReplacementSequence).ToString(CultureInfo.InvariantCulture);
        VRMmddStateBridge.ResetCharacterVmd();
        VRMmddStateBridge.ResetPlayback();

        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append(BuildStateBridgeImportCode());
        code.Append(BuildCharacterVmdRestoreFunctionCode());
        code.Append("target_key = ").Append(objectKey).Append("\n");
        code.Append("session_token = ").Append(ToPythonString(token)).Append("\n");
        code.Append("all_actors = [x.as_actor for x in game.scene_get_all_females()] + [x.as_actor for x in game.scene_get_all_males()]\n");
        code.Append("target_actors = [actor for actor in all_actors if actor.objctrl.objectInfo.dicKey == target_key]\n");
        code.Append("if len(target_actors) != 1:\n");
        code.Append("    raise Exception('The selected Studio character is no longer available')\n");
        code.Append("target_actor = target_actors[0]\n");
        code.Append("motion_ctrl = mmdd.getMotionController(target_actor)\n");
        code.Append("morph_ctrl = mmdd.getMorphController(target_actor)\n");
        code.Append("if motion_ctrl is None and morph_ctrl is None:\n");
        code.Append("    VRMmddStateBridge.ReportCharacterVmd(False, False, None, None, False, float(mmdd.curFrame))\n");
        code.Append("else:\n");
        code.Append("    controller_id = None\n");
        code.Append("    if motion_ctrl is not None:\n");
        code.Append("        for candidate_id in list(mmdd.motionControllers.keys()):\n");
        code.Append("            if mmdd.motionControllers[candidate_id] == motion_ctrl:\n");
        code.Append("                controller_id = candidate_id\n");
        code.Append("                break\n");
        code.Append("    if controller_id is None and morph_ctrl is not None:\n");
        code.Append("        for candidate_id in list(mmdd.morphControllers.keys()):\n");
        code.Append("            if mmdd.morphControllers[candidate_id] == morph_ctrl:\n");
        code.Append("                controller_id = candidate_id\n");
        code.Append("                break\n");
        code.Append("    if controller_id is None:\n");
        code.Append("        raise Exception('MMDD could not identify the selected character controller')\n");
        code.Append("    import os\n");
        code.Append("    motion_embed = motion_ctrl is not None and (not motion_ctrl.vmdFilePath or not os.path.isfile(motion_ctrl.vmdFilePath))\n");
        code.Append("    morph_embed = morph_ctrl is not None and (not morph_ctrl.vmdFilePath or not os.path.isfile(morph_ctrl.vmdFilePath))\n");
        code.Append("    motion_setting = motion_ctrl.exportSetting(motion_embed) if motion_ctrl is not None else None\n");
        code.Append("    morph_setting = morph_ctrl.exportSetting(morph_embed) if morph_ctrl is not None else None\n");
        code.Append("    motion_file = motion_ctrl.vmdFilePath if motion_ctrl is not None else None\n");
        code.Append("    morph_file = morph_ctrl.vmdFilePath if morph_ctrl is not None else None\n");
        code.Append("    was_playing = bool(mmdd.isPlaying)\n");
        code.Append("    resume_frame = float(mmdd.curFrame)\n");
        code.Append("    motion_index = mmdd.motionSequence.index(controller_id) if controller_id in mmdd.motionSequence else -1\n");
        code.Append("    replacement_session = {'controller_id': controller_id, 'motion_setting': motion_setting, 'morph_setting': morph_setting, 'motion_index': motion_index, 'was_playing': was_playing, 'resume_frame': resume_frame}\n");
        code.Append("    if not hasattr(game.gdata, 'kkvrCharacterReplacementVmdSessions'):\n");
        code.Append("        game.gdata.kkvrCharacterReplacementVmdSessions = {}\n");
        code.Append("    game.gdata.kkvrCharacterReplacementVmdSessions[session_token] = replacement_session\n");
        code.Append("    try:\n");
        code.Append("        if mmdd.isPlaying:\n");
        code.Append("            mmdd.stop()\n");
        code.Append("        if motion_ctrl is not None:\n");
        code.Append("            mmdd.deleteMotionController(motion_ctrl)\n");
        code.Append("        remaining_morph = mmdd.getMorphController(target_actor)\n");
        code.Append("        if remaining_morph is not None:\n");
        code.Append("            mmdd.deleteMorphController(remaining_morph)\n");
        code.Append("        if mmdd.getMotionController(target_actor) is not None or mmdd.getMorphController(target_actor) is not None:\n");
        code.Append("            raise Exception('MMDD controllers remained attached after cleanup')\n");
        code.Append("        mmdd.refreshTotalFrame()\n");
        code.Append("        VRMmddStateBridge.ReportCharacterVmd(motion_setting is not None, morph_setting is not None, motion_file, morph_file, was_playing, resume_frame)\n");
        code.Append("    except Exception as prepare_error:\n");
        code.Append("        prepare_error_text = str(prepare_error)\n");
        code.Append("        rollback_error = None\n");
        code.Append("        try:\n");
        code.Append("            kkvr_restore_character_vmd(target_actor, replacement_session)\n");
        code.Append("        except Exception as caught_rollback_error:\n");
        code.Append("            rollback_error = caught_rollback_error\n");
        code.Append("        if session_token in game.gdata.kkvrCharacterReplacementVmdSessions:\n");
        code.Append("            del game.gdata.kkvrCharacterReplacementVmdSessions[session_token]\n");
        code.Append("        if rollback_error is not None:\n");
        code.Append("            raise Exception('MMDD prepare failed: ' + prepare_error_text + '; rollback failed: ' + str(rollback_error))\n");
        code.Append("        raise Exception('MMDD prepare failed and was rolled back: ' + prepare_error_text)\n");

        string error;
        if (!VRPythonBridge.TryExecute(
            code.ToString(),
            "MMDD character replacement prepare",
            out error))
        {
            if (IsMmddRuntimeUnavailable(error))
            {
                status = null;
                return true;
            }

            status = "角色替换前无法清理旧 VMD：" + error;
            return false;
        }

        if (!VRMmddStateBridge.CharacterVmdReported)
        {
            VRMmddCharacterReplacementSession recoverySession =
                new VRMmddCharacterReplacementSession { Token = token, ObjectKey = objectKey };
            string rollbackStatus;
            bool rolledBack = TryRestoreCharacterReplacement(
                recoverySession,
                objectKey,
                out rollbackStatus);
            if (!rolledBack)
                DiscardCharacterReplacement(recoverySession);
            status = rolledBack
                ? "角色替换前无法确认旧 VMD 状态；原绑定已恢复"
                : "角色替换前无法确认旧 VMD 状态；" + rollbackStatus;
            return false;
        }

        if (!VRMmddStateBridge.CharacterVmdHadMotion
            && !VRMmddStateBridge.CharacterVmdHadMorph)
        {
            status = null;
            return true;
        }

        session = new VRMmddCharacterReplacementSession
        {
            Token = token,
            ObjectKey = objectKey,
            MotionFile = VRMmddStateBridge.CharacterVmdMotionFile,
            MorphFile = VRMmddStateBridge.CharacterVmdMorphFile,
            WasPlaying = VRMmddStateBridge.CharacterVmdWasPlaying,
            Frame = VRMmddStateBridge.CharacterVmdFrame
        };
        status = "已暂存并清理旧 VMD";
        return true;
    }

    public static bool TryRestoreCharacterReplacement(
        VRMmddCharacterReplacementSession session,
        int objectKey,
        out string status)
    {
        return TryRestoreCharacterReplacement(
            session,
            objectKey,
            false,
            out status);
    }

    public static bool TryRestoreCharacterReplacement(
        VRMmddCharacterReplacementSession session,
        int objectKey,
        bool resetToStartFrame,
        out string status)
    {
        if (session == null || string.IsNullOrEmpty(session.Token))
        {
            status = "没有可恢复的 VMD 临时数据";
            return false;
        }

        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append(BuildStateBridgeImportCode());
        code.Append(BuildCharacterVmdRestoreFunctionCode());
        code.Append("target_key = ").Append(objectKey).Append("\n");
        code.Append("session_token = ").Append(ToPythonString(session.Token)).Append("\n");
        code.Append("reset_to_start = ").Append(resetToStartFrame ? "True" : "False").Append("\n");
        code.Append("if not hasattr(game.gdata, 'kkvrCharacterReplacementVmdSessions') or session_token not in game.gdata.kkvrCharacterReplacementVmdSessions:\n");
        code.Append("    raise Exception('The temporary VMD replacement session is no longer available')\n");
        code.Append("replacement_session = game.gdata.kkvrCharacterReplacementVmdSessions[session_token]\n");
        code.Append("all_actors = [x.as_actor for x in game.scene_get_all_females()] + [x.as_actor for x in game.scene_get_all_males()]\n");
        code.Append("target_actors = [actor for actor in all_actors if actor.objctrl.objectInfo.dicKey == target_key]\n");
        code.Append("if len(target_actors) != 1:\n");
        code.Append("    raise Exception('The replacement character is not ready')\n");
        code.Append("target_actor = target_actors[0]\n");
        code.Append("kkvr_restore_character_vmd(target_actor, replacement_session, reset_to_start)\n");
        code.Append("del game.gdata.kkvrCharacterReplacementVmdSessions[session_token]\n");
        code.Append(BuildImmediatePlaybackReportCode());

        string error;
        if (!VRPythonBridge.TryExecute(
            code.ToString(),
            "MMDD character replacement restore",
            out error))
        {
            status = "VMD 重新绑定失败：" + error;
            return false;
        }

        string file = !string.IsNullOrEmpty(session.MotionFile)
            ? session.MotionFile
            : session.MorphFile;
        status = resetToStartFrame
            ? "VMD 已重新绑定并停在首帧：" + FileNameWithoutExtension(file)
            : "VMD 已重新绑定：" + FileNameWithoutExtension(file);
        return true;
    }

    public static void DiscardCharacterReplacement(
        VRMmddCharacterReplacementSession session)
    {
        if (session == null || string.IsNullOrEmpty(session.Token))
            return;

        string code =
            "from vngameengine import vnge_game\n"
            + "game = vnge_game\n"
            + "session_token = " + ToPythonString(session.Token) + "\n"
            + "if hasattr(game.gdata, 'kkvrCharacterReplacementVmdSessions') and session_token in game.gdata.kkvrCharacterReplacementVmdSessions:\n"
            + "    del game.gdata.kkvrCharacterReplacementVmdSessions[session_token]\n";
        string ignoredError;
        VRPythonBridge.TryExecute(
            code,
            "MMDD character replacement session discard",
            out ignoredError);
    }

    public static bool TryClearSelectedCharacterMmdData(out string status)
    {
        int[] selectedKeys = VRVmdTargetService.GetSelectedObjectKeys();
        if (selectedKeys == null || selectedKeys.Length == 0)
        {
            status = "请先在工作室中只选中一个角色";
            return false;
        }
        if (selectedKeys.Length != 1)
        {
            status = "一次只能清空一个角色，请只保留一个选中角色";
            return false;
        }

        return TryClearCharacterMmdData(selectedKeys[0], out status);
    }

    public static bool TryClearCharacterMmdData(int objectKey, out string status)
    {
        int[] selectedKeys = VRVmdTargetService.GetSelectedObjectKeys();
        if (selectedKeys == null || selectedKeys.Length != 1)
        {
            status = selectedKeys == null || selectedKeys.Length == 0
                ? "请先在工作室中只选中一个角色"
                : "一次只能清空一个角色，请只保留一个选中角色";
            return false;
        }
        if (selectedKeys[0] != objectKey)
        {
            status = "当前选中角色已经变化，请重新确认后再清空";
            return false;
        }

        VRVmdActorTarget target;
        if (!VRVmdTargetService.TryGetTarget(objectKey, out target, out status))
            return false;

        VRMmddStateBridge.ResetMmdClear();
        VRMmddStateBridge.ResetPlayback();
        VRMmddStateBridge.ResetFixedFov();

        string error;
        bool preparedCameraAnchor =
            VRMmdCameraAnchorController.PrepareForPotentialDirectCameraEvaluation();
        bool executed = VRPythonBridge.TryExecute(
            BuildClearCharacterMmdDataCode(objectKey, target.DisplayName),
            "MMDD clear selected character motion and global cameras",
            out error);
        if (preparedCameraAnchor)
        {
            if (executed || VRMmddStateBridge.MmdClearReported)
                VRMmdCameraAnchorController.CorrectAfterSynchronousCameraEvaluation();
            else
                VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
        }
        if (!executed)
        {
            if (VRMmddStateBridge.MmdClearReported
                && VRMmddStateBridge.MmdClearObjectKey == objectKey)
            {
                // The transaction reported its outcome before a trailing state
                // refresh failed. Keep that authoritative result instead of
                // claiming that a successful clear did not happen.
                VRLog.Warn("MMDD clear state refresh failed after transaction result: " + error);
            }
            else
            {
                VRMmddStateBridge.ReportMmdClear(
                    false,
                    objectKey,
                    target.DisplayName,
                    0,
                    0,
                    0,
                    false,
                    false,
                    error);
                status = "清空 MMD 数据失败：" + error;
                return false;
            }
        }

        if (!VRMmddStateBridge.MmdClearReported
            || VRMmddStateBridge.MmdClearObjectKey != objectKey)
        {
            status = "MMDD 没有返回可信的清空结果，未确认数据状态";
            return false;
        }

        if (!VRMmddStateBridge.MmdClearSucceeded)
        {
            string reportedError = string.IsNullOrEmpty(VRMmddStateBridge.MmdClearError)
                ? "未知错误"
                : VRMmddStateBridge.MmdClearError;
            if (VRMmddStateBridge.MmdClearRollbackFailed)
            {
                status = "清空失败且回滚不完整，请勿保存场景并先重载备份："
                    + reportedError;
            }
            else if (VRMmddStateBridge.MmdClearRolledBack)
            {
                status = "清空失败，原 MMDD 数据已恢复：" + reportedError;
            }
            else
            {
                status = "未执行清空：" + reportedError;
            }
            return false;
        }

        int motionCount = VRMmddStateBridge.MmdClearMotionCount;
        int morphCount = VRMmddStateBridge.MmdClearMorphCount;
        int cameraCount = VRMmddStateBridge.MmdClearCameraCount;
        if (motionCount == 0 && morphCount == 0 && cameraCount == 0)
        {
            status = target.DisplayName + " 没有 MMDD 动作或表情，场景也没有 MMDD 镜头";
            return true;
        }

        status = "已清空 "
            + target.DisplayName
            + " 的 MMDD 动作 "
            + motionCount.ToString(CultureInfo.InvariantCulture)
            + "、表情 "
            + morphCount.ToString(CultureInfo.InvariantCulture)
            + "，以及场景全局 MMDD 镜头 "
            + cameraCount.ToString(CultureInfo.InvariantCulture)
            + "；Timeline、音频和其他角色动作已保留";
        return true;
    }

    public static bool TogglePlayPause(out string status)
    {
        // MMDD's start path can evaluate its VR camera before our LateUpdate.
        // Preserve the current room-scale head offset across that first frame.
        VRMmdCameraAnchorController.PrepareForSynchronousCameraEvaluation();
        string code =
            BuildBootstrapCode()
            + BuildStateBridgeImportCode()
            + "game.gdata.mmdd.startStop()\n"
            + BuildImmediatePlaybackReportCode();

        string error;
        if (VRPythonBridge.TryExecute(code, "MMDD play/pause", out error))
        {
            if (VRMmddStateBridge.PlaybackIsPlaying)
                VRMmdCameraAnchorController.CorrectAfterSynchronousCameraEvaluation();
            else
                VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
            status = "MMDD 播放状态已切换";
            return true;
        }

        VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
        status = "MMDD 无法控制：" + error;
        return false;
    }

    public static bool StartPlayback(out string status)
    {
        VRMmdCameraAnchorController.PrepareForSynchronousCameraEvaluation();
        string code =
            BuildBootstrapCode()
            + BuildStateBridgeImportCode()
            + "if not mmdd.isPlaying:\n"
            + "    mmdd.start()\n"
            + BuildImmediatePlaybackReportCode();

        string error;
        if (VRPythonBridge.TryExecute(code, "MMDD explicit play", out error))
        {
            if (!VRMmddStateBridge.PlaybackIsPlaying)
            {
                VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
                status = "MMD 未能开始播放";
                return false;
            }

            VRMmdCameraAnchorController.CorrectAfterSynchronousCameraEvaluation();
            status = "MMD 开始播放";
            return true;
        }

        VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
        status = "MMD 播放失败：" + error;
        return false;
    }

    public static bool PausePlayback(out string status)
    {
        VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
        string code =
            BuildBootstrapCode()
            + BuildStateBridgeImportCode()
            + "if mmdd.isPlaying:\n"
            + "    mmdd.stop()\n"
            + BuildImmediatePlaybackReportCode();

        string error;
        if (VRPythonBridge.TryExecute(code, "MMDD explicit pause", out error))
        {
            status = "MMD 已暂停";
            return true;
        }

        status = "MMD 暂停失败：" + error;
        return false;
    }

    public static bool GoToStartFrame(out string status)
    {
        VRMmdCameraAnchorController.PrepareForSynchronousCameraEvaluation();
        string code =
            BuildBootstrapCode()
            + BuildStateBridgeImportCode()
            + "if mmdd.isPlaying:\n"
            + "    mmdd.stop()\n"
            + "mmdd.refreshTotalFrame()\n"
            + "mmdd.curFrame = float(mmdd.StartFrame)\n"
            + "mmdd.update(True)\n"
            + BuildImmediatePlaybackReportCode();

        string error;
        if (!VRPythonBridge.TryExecute(code, "MMDD return to first frame", out error))
        {
            VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
            status = "回到首帧失败：" + error;
            return false;
        }

        VRMmdCameraAnchorController.CorrectAfterSynchronousCameraEvaluation();
        if (VRMmdPlaybackController.Instance != null)
            VRMmdPlaybackController.Instance.NotifyReturnedToStart();
        status = "动作已回到首帧并停止";
        return true;
    }

    public static bool EnsurePlaybackStateReporter(out string status)
    {
        string code =
            "from vngameengine import vnge_game\n"
            + BuildStateBridgeImportCode()
            + "game = vnge_game\n"
            + "def kkvr_mmdd_owns_vr_camera(game_obj, active_mmdd):\n"
            + "    try:\n"
            + "        studio_obj = getattr(game_obj, 'studio', None) if game_obj is not None else None\n"
            + "        if studio_obj is not None and getattr(studio_obj, 'ociCamera', None) is not None:\n"
            + "            return False\n"
            + "        for camera_controller in getattr(active_mmdd, 'cameraControllers', []):\n"
            + "            if bool(getattr(camera_controller, 'enable', True)) and getattr(camera_controller, 'bindCameraTrans', None) is None and getattr(camera_controller, 'vrOriginTransform', None) is not None:\n"
            + "                return True\n"
            + "    except:\n"
            + "        pass\n"
            + "    return False\n"
            + "def kkvr_report_mmdd_playback(game_obj, event_id, param):\n"
            + "    try:\n"
            + "        game_data = getattr(game_obj, 'gdata', None) if game_obj is not None else None\n"
            + "        active_mmdd = getattr(game_data, 'mmdd', None) if game_data is not None else None\n"
            + "        if active_mmdd is None:\n"
            + "            VRMmddStateBridge.ReportPlayback(False, False, 0.0, 0.0, 0.0, 0, False)\n"
            + "            return\n"
            + "        generation = int(getattr(game_data, 'kkvrMmdPlaybackGeneration', 0))\n"
            + "        direct_vr_camera_owner = kkvr_mmdd_owns_vr_camera(game_obj, active_mmdd)\n"
            + "        VRMmddStateBridge.ReportPlayback(True, bool(active_mmdd.isPlaying), float(active_mmdd.curFrame), float(active_mmdd.StartFrame), float(active_mmdd.EndFrame), generation, direct_vr_camera_owner)\n"
            + "    except:\n"
            + "        VRMmddStateBridge.ReportPlayback(False, False, 0.0, 0.0, 0.0, 0, False)\n"
            + "game_data = getattr(game, 'gdata', None) if game is not None else None\n"
            + "if game is None or game_data is None:\n"
            + "    VRMmddStateBridge.ReportPlayback(False, False, 0.0, 0.0, 0.0, 0, False)\n"
            + "else:\n"
            + "    if game.event_is_reg_funcid('update', 'kkvr_playback_state'):\n"
            + "        game.event_unreg_listener('update', None, 'kkvr_playback_state')\n"
            + "    game.event_reg_listener('update', kkvr_report_mmdd_playback, 'kkvr_playback_state', True)\n"
            + "    kkvr_report_mmdd_playback(game, 'update', None)\n";

        string error;
        if (VRPythonBridge.TryExecute(code, "MMDD playback state bridge", out error))
        {
            status = null;
            return true;
        }

        status = error;
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
            // A stick adjustment is an explicit request for a fixed FOV. Persist
            // both the value and the enabled state so the first adjustment works
            // even when MMDD previously had Fixed FOV disabled.
            "config.useFixedFOV = True\n"
            + "config.useFixedFOVValue = "
            + value.ToString("R", CultureInfo.InvariantCulture)
            + "\n",
            "MMDD Fixed FOV value",
            out status);
    }

    public static bool SetFixedFovRuntime(float value, out string status)
    {
        value = Math.Max(MinFixedFov, Math.Min(MaxFixedFov, value));
        return ExecuteFixedFovAction(
            "config.useFixedFOV = True\n"
            + "config.useFixedFOVValue = "
            + value.ToString("R", CultureInfo.InvariantCulture)
            + "\n",
            "MMDD runtime Fixed FOV value",
            false,
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

    public static bool SaveHighHeelsPreset(int objectKey, out string status)
    {
        VRVmdActorTarget target;
        if (!VRVmdTargetService.TryGetTarget(objectKey, out target, out status))
            return false;
        if ((!VRMmddStateBridge.HighHeelsReported
                || VRMmddStateBridge.HighHeelsObjectKey != objectKey)
            && !RefreshHighHeels(objectKey, out status))
            return false;

        VRHighHeelsPreset preset = new VRHighHeelsPreset
        {
            AutoMode = VRMmddStateBridge.HighHeelsAutoMode,
            ShoesDetect = VRMmddStateBridge.HighHeelsShoesDetect,
            Ankle = VRMmddStateBridge.HighHeelsAnkle,
            Heel = VRMmddStateBridge.HighHeelsHeel,
            Toes = VRMmddStateBridge.HighHeelsToes,
            ShoesOffsetEnabled = VRMmddStateBridge.ShoesOffsetEnabled,
            ShoesOnOffset = VRMmddStateBridge.ShoesOnOffset,
            ShoesOffOffset = VRMmddStateBridge.ShoesOffOffset
        };
        return VRHighHeelsPresetStore.Save(target.Character, preset, out status);
    }

    public static bool LoadHighHeelsPreset(int objectKey, out string status)
    {
        VRVmdActorTarget target;
        if (!VRVmdTargetService.TryGetTarget(objectKey, out target, out status))
            return false;

        VRHighHeelsPreset preset;
        if (!VRHighHeelsPresetStore.TryLoad(target.Character, out preset, out status))
            return false;

        float ankle = Math.Max(-180f, Math.Min(180f, preset.Ankle));
        float heel = Math.Max(-180f, Math.Min(180f, preset.Heel));
        float toes = Math.Max(-180f, Math.Min(180f, preset.Toes));
        float shoesOnOffset = Math.Max(-1f, Math.Min(1f, preset.ShoesOnOffset));
        float shoesOffOffset = Math.Max(-1f, Math.Min(1f, preset.ShoesOffOffset));

        string action =
            "mc.HighHeels.refreshHeelInfo()\n"
            + "mc.enableHeelzCtrl(False)\n"
            + "mc.ShoesDetect = " + ToPythonBool(preset.ShoesDetect) + "\n"
            + "mc.setHeelzRotation("
            + ankle.ToString("R", CultureInfo.InvariantCulture)
            + ", "
            + heel.ToString("R", CultureInfo.InvariantCulture)
            + ", "
            + toes.ToString("R", CultureInfo.InvariantCulture)
            + ")\n"
            + "mc.HighHeelsSaveValue = ("
            + ankle.ToString("R", CultureInfo.InvariantCulture)
            + ", "
            + heel.ToString("R", CultureInfo.InvariantCulture)
            + ", "
            + toes.ToString("R", CultureInfo.InvariantCulture)
            + ")\n"
            + "mc.ShoesOffset[1] = "
            + shoesOnOffset.ToString("R", CultureInfo.InvariantCulture)
            + "\n"
            + "mc.ShoesOffset[2] = "
            + shoesOffOffset.ToString("R", CultureInfo.InvariantCulture)
            + "\n"
            + "mc.enableShoesOffsetCtrl("
            + ToPythonBool(preset.ShoesOffsetEnabled)
            + ")\n"
            + "if mc.ShoesOffset[0]:\n"
            + "    mc.setShoesOffset()\n";

        if (!ExecuteHighHeelsAction(
            objectKey,
            action,
            "MMDD high heels preset load",
            out status))
        {
            return false;
        }

        status = preset.CharacterCardName + "：高跟鞋参数已应用";
        return true;
    }

    public static bool TryApplyHighHeelsPresetOrAutomatic(
        int objectKey,
        out bool deferred,
        out string status)
    {
        deferred = false;
        VRVmdActorTarget target;
        if (!VRVmdTargetService.TryGetTarget(objectKey, out target, out status))
            return false;

        bool hasPreset = VRHighHeelsPresetStore.HasPreset(target.Character);
        if (hasPreset)
        {
            if (LoadHighHeelsPreset(objectKey, out status))
                return true;
            deferred = IsDeferredHighHeelsStatus(status);
            return false;
        }

        const string automaticAction =
            "mc.enableShoesOffsetCtrl(False)\n"
            + "mc.HighHeels.refreshHeelInfo()\n"
            + "mc.enableHeelzCtrl(True)\n"
            + "mc.HighHeelsSaveValue = None\n"
            + "mc.ShoesDetect = True\n"
            + "mc.ShoesOffset[1] = mc.HighHeels.defaultOffset[0]\n"
            + "mc.ShoesOffset[2] = mc.HighHeels.defaultOffset[1]\n";
        if (ExecuteHighHeelsAction(
            objectKey,
            automaticAction,
            "MMDD high heels automatic reset",
            out status))
        {
            status = target.DisplayName + "：未找到缓存，已恢复自动高跟模式";
            return true;
        }

        deferred = IsDeferredHighHeelsStatus(status);
        return false;
    }

    public static bool LoadVmdPackage(
        string motionPath,
        string cameraPath,
        string audioPath,
        int[] actorObjectKeys,
        string vmdRoot,
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
        if (string.IsNullOrEmpty(vmdRoot) || !Directory.Exists(vmdRoot))
        {
            status = "动作数据目录不存在，请重新选择";
            return false;
        }

        if (VRMmdPlaybackController.Instance != null)
            VRMmdPlaybackController.Instance.PrepareForPackageChange();

        string code = BuildLoadPackageCode(
            motionPath,
            cameraPath,
            audioPath,
            actorObjectKeys,
            vmdRoot,
            false);

        string error;
        bool preparedLoadAnchor =
            VRMmdCameraAnchorController.PrepareForPotentialDirectCameraEvaluation();
        bool loaded = VRPythonBridge.TryExecute(code, "MMDD wrist VMD load", out error);
        if (!loaded && RequiresMmddStateRecovery(error))
        {
            if (preparedLoadAnchor)
                VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
            VRLog.Warn(
                "MMDD retained destroyed character state after a scene/card change; rebuilding its controllers and retrying once.");
            string recoveryCode = BuildLoadPackageCode(
                motionPath,
                cameraPath,
                audioPath,
                actorObjectKeys,
                vmdRoot,
                true);
            preparedLoadAnchor =
                VRMmdCameraAnchorController.PrepareForPotentialDirectCameraEvaluation();
            loaded = VRPythonBridge.TryExecute(
                recoveryCode,
                "MMDD wrist VMD recovery",
                out error);
        }

        if (!loaded)
        {
            if (preparedLoadAnchor)
                VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
            if (VRMmdPlaybackController.Instance != null)
                VRMmdPlaybackController.Instance.NotifyPackageLoadFailed();
            status = "VMD 载入失败：" + error;
            return false;
        }

        if (preparedLoadAnchor)
            VRMmdCameraAnchorController.CorrectAfterSynchronousCameraEvaluation();

        int[] liveActorKeys = VRVmdTargetService.ResolveLiveObjectKeys(actorObjectKeys);
        if (VRMmdPlaybackController.Instance != null)
        {
            VRMmdPlaybackController.Instance.NotifyPackageLoaded(
                motionPath,
                liveActorKeys);
            VRMmdPlaybackController.Instance.RequestHighHeelsRefresh(liveActorKeys);
        }

        string reporterStatus;
        EnsurePlaybackStateReporter(out reporterStatus);

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
            + "if game is None or getattr(game, 'gdata', None) is None:\n"
            + "    raise Exception('VNGE game data is not ready')\n"
            + "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n"
            + "    mmddirector.start(game)\n"
            + "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n"
            + "    raise Exception('MMD Director could not be started')\n"
            + "mmdd = game.gdata.mmdd\n"
            + "config = game.gdata.mmdd_config\n"
            + "game.visible = 0\n";
    }

    private static string BuildCharacterVmdRestoreFunctionCode()
    {
        return
            "from vmdlib import MotionController, MorphController\n"
            + "def kkvr_restore_character_vmd(target_actor, replacement_session, reset_to_start=False):\n"
            + "    if mmdd.isPlaying:\n"
            + "        mmdd.stop()\n"
            + "    existing_motion = mmdd.getMotionController(target_actor)\n"
            + "    if existing_motion is not None:\n"
            + "        mmdd.deleteMotionController(existing_motion)\n"
            + "    existing_morph = mmdd.getMorphController(target_actor)\n"
            + "    if existing_morph is not None:\n"
            + "        mmdd.deleteMorphController(existing_morph)\n"
            + "    controller_id = replacement_session['controller_id']\n"
            + "    motion_setting = replacement_session['motion_setting']\n"
            + "    morph_setting = replacement_session['morph_setting']\n"
            + "    vmd_frame_buffer = {}\n"
            + "    motion_ctrl = None\n"
            + "    if motion_setting is not None:\n"
            + "        motion_ctrl = MotionController.parseFromSetting(motion_setting, target_actor, vmd_frame_buffer)\n"
            + "        mmdd.postProcessForMotionImport(target_actor)\n"
            + "        mmdd.registerMotionController(controller_id, motion_ctrl)\n"
            + "        motion_index = replacement_session['motion_index']\n"
            + "        if motion_index >= 0 and controller_id in mmdd.motionSequence:\n"
            + "            mmdd.motionSequence.remove(controller_id)\n"
            + "            mmdd.motionSequence.insert(min(motion_index, len(mmdd.motionSequence)), controller_id)\n"
            + "    if morph_setting is not None:\n"
            + "        morph_param = motion_ctrl if motion_ctrl is not None else target_actor\n"
            + "        morph_ctrl = MorphController.parseFromSetting(morph_setting, morph_param, vmd_frame_buffer)\n"
            + "        mmdd.morphControllers[controller_id] = morph_ctrl\n"
            + "    if motion_ctrl is not None and motion_ctrl.NeedActiveMotion:\n"
            + "        def kkvr_enable_rebound_motion(game_obj, rebound_motion):\n"
            + "            if not rebound_motion.Enable:\n"
            + "                rebound_motion.setEnable(True)\n"
            + "            return bool(rebound_motion.Enable)\n"
            + "        game.append_update_job(kkvr_enable_rebound_motion, motion_ctrl)\n"
            + "    if motion_setting is not None and motion_setting.get('filename'):\n"
            + "        mmdd.lastVmdFilename = motion_setting['filename']\n"
            + "    elif morph_setting is not None and morph_setting.get('filename'):\n"
            + "        mmdd.lastVmdFilename = morph_setting['filename']\n"
            + "    mmdd.refreshTotalFrame()\n"
            + "    if reset_to_start:\n"
            + "        target_frame = float(mmdd.StartFrame)\n"
            + "    else:\n"
            + "        target_frame = max(float(mmdd.StartFrame), min(float(replacement_session['resume_frame']), float(mmdd.EndFrame)))\n"
            + "    mmdd.curFrame = target_frame\n"
            + "    if not mmdd.verify():\n"
            + "        raise Exception('MMDD verification failed after rebinding')\n"
            + "    mmdd.update(True)\n"
            + "    if (not reset_to_start) and replacement_session['was_playing']:\n"
            + "        mmdd.start()\n"
            + "        if not mmdd.isPlaying:\n"
            + "            raise Exception('MMDD could not resume playback after rebinding')\n";
    }

    private static string BuildClearCharacterMmdDataCode(
        int objectKey,
        string targetName)
    {
        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append(BuildStateBridgeImportCode());
        code.Append(BuildCharacterVmdRestoreFunctionCode());
        code.Append("target_key = ").Append(objectKey).Append("\n");
        code.Append("expected_target_name = ").Append(ToPythonString(targetName)).Append("\n");
        code.Append("def kkvr_clear_selected_character_mmd():\n");
        code.Append("    target_name = expected_target_name\n");
        code.Append("    target_actor = None\n");
        code.Append("    motion_count = 0\n");
        code.Append("    morph_count = 0\n");
        code.Append("    camera_count = 0\n");
        code.Append("    old_was_playing = False\n");
        code.Append("    old_frame = 0.0\n");
        code.Append("    old_last_vmd = None\n");
        code.Append("    old_generation = int(getattr(game.gdata, 'kkvrMmdPlaybackGeneration', 0))\n");
        code.Append("    pause_attempted = False\n");
        code.Append("    backup_ready = False\n");
        code.Append("    mutation_started = False\n");
        code.Append("    replacement_session = None\n");
        code.Append("    camera_backup = []\n");
        code.Append("    sound_backup = []\n");
        code.Append("    timeline_backup = []\n");
        code.Append("    try:\n");
        code.Append("        all_actors = [x.as_actor for x in game.scene_get_all_females()] + [x.as_actor for x in game.scene_get_all_males()]\n");
        code.Append("        target_actors = [actor for actor in all_actors if getattr(getattr(actor, 'objctrl', None), 'objectInfo', None) is not None and actor.objctrl.objectInfo.dicKey == target_key]\n");
        code.Append("        if len(target_actors) != 1:\n");
        code.Append("            raise Exception('The selected Studio character is no longer uniquely available')\n");
        code.Append("        target_actor = target_actors[0]\n");
        code.Append("        if not target_name:\n");
        code.Append("            target_name = getattr(target_actor, 'text_name', None) or 'Selected character'\n");
        code.Append("        plugin_controllers = getattr(mmdd, 'vmdControllers', {})\n");
        code.Append("        for plugin_actor in list(plugin_controllers.keys()):\n");
        code.Append("            if getattr(plugin_actor, 'objctrl', None) == target_actor.objctrl:\n");
        code.Append("                raise Exception('The selected character is controlled by external KKVMDPlay data, which cannot be unloaded reliably')\n");
        code.Append("        if getattr(mmdd, 'vmdCameraCtrl', None) is not None:\n");
        code.Append("            raise Exception('An external KKVMDPlay camera is active and cannot be unloaded reliably')\n");
        code.Append("        old_was_playing = bool(mmdd.isPlaying)\n");
        code.Append("        old_frame = float(mmdd.curFrame)\n");
        code.Append("        old_last_vmd = getattr(mmdd, 'lastVmdFilename', None)\n");
        code.Append("        if old_was_playing:\n");
        code.Append("            pause_attempted = True\n");
        code.Append("            mmdd.stop()\n");
        code.Append("            if mmdd.isPlaying:\n");
        code.Append("                raise Exception('MMDD did not pause before clearing data')\n");
        code.Append("        motion_ctrl = mmdd.getMotionController(target_actor)\n");
        code.Append("        morph_ctrl = mmdd.getMorphController(target_actor)\n");
        code.Append("        motion_count = 1 if motion_ctrl is not None else 0\n");
        code.Append("        morph_count = 1 if morph_ctrl is not None else 0\n");
        code.Append("        controller_id = None\n");
        code.Append("        if motion_ctrl is not None:\n");
        code.Append("            for candidate_id in list(mmdd.motionControllers.keys()):\n");
        code.Append("                if mmdd.motionControllers[candidate_id] == motion_ctrl:\n");
        code.Append("                    controller_id = candidate_id\n");
        code.Append("                    break\n");
        code.Append("        if controller_id is None and morph_ctrl is not None:\n");
        code.Append("            for candidate_id in list(mmdd.morphControllers.keys()):\n");
        code.Append("                if mmdd.morphControllers[candidate_id] == morph_ctrl:\n");
        code.Append("                    controller_id = candidate_id\n");
        code.Append("                    break\n");
        code.Append("        if controller_id is None and (motion_ctrl is not None or morph_ctrl is not None):\n");
        code.Append("            raise Exception('MMDD could not identify the selected character controller')\n");
        code.Append("        import os\n");
        code.Append("        motion_embed = motion_ctrl is not None and (not motion_ctrl.vmdFilePath or not os.path.isfile(motion_ctrl.vmdFilePath))\n");
        code.Append("        morph_embed = morph_ctrl is not None and (not morph_ctrl.vmdFilePath or not os.path.isfile(morph_ctrl.vmdFilePath))\n");
        code.Append("        motion_setting = motion_ctrl.exportSetting(motion_embed) if motion_ctrl is not None else None\n");
        code.Append("        morph_setting = morph_ctrl.exportSetting(morph_embed) if morph_ctrl is not None else None\n");
        code.Append("        motion_index = mmdd.motionSequence.index(controller_id) if controller_id in mmdd.motionSequence else -1\n");
        code.Append("        replacement_session = {'controller_id': controller_id, 'motion_setting': motion_setting, 'morph_setting': morph_setting, 'motion_index': motion_index, 'was_playing': old_was_playing, 'resume_frame': old_frame}\n");
        code.Append("        camera_backup = list(mmdd.cameraControllers)\n");
        code.Append("        sound_backup = list(mmdd.soundControllers)\n");
        code.Append("        timeline_backup = list(mmdd.timelineControllers)\n");
        code.Append("        camera_count = len(camera_backup)\n");
        code.Append("        other_motion = [(candidate_id, candidate_ctrl) for candidate_id, candidate_ctrl in mmdd.motionControllers.items() if candidate_ctrl is not motion_ctrl]\n");
        code.Append("        other_morph = [(candidate_id, candidate_ctrl) for candidate_id, candidate_ctrl in mmdd.morphControllers.items() if candidate_ctrl is not morph_ctrl]\n");
        code.Append("        backup_ready = True\n");
        code.Append("        mutation_started = True\n");
        code.Append("        if motion_ctrl is not None:\n");
        code.Append("            mmdd.deleteMotionController(motion_ctrl)\n");
        code.Append("        remaining_morph = mmdd.getMorphController(target_actor)\n");
        code.Append("        if remaining_morph is not None:\n");
        code.Append("            mmdd.deleteMorphController(remaining_morph)\n");
        code.Append("        mmdd.clearCameraControllers()\n");
        code.Append("        if mmdd.getMotionController(target_actor) is not None or mmdd.getMorphController(target_actor) is not None:\n");
        code.Append("            raise Exception('MMDD controllers remained attached to the selected character')\n");
        code.Append("        if len(mmdd.cameraControllers) != 0:\n");
        code.Append("            raise Exception('MMDD camera controllers remained after cleanup')\n");
        code.Append("        for preserved_id, preserved_ctrl in other_motion:\n");
        code.Append("            if preserved_id not in mmdd.motionControllers or mmdd.motionControllers[preserved_id] is not preserved_ctrl:\n");
        code.Append("                raise Exception('Another character motion controller changed during cleanup')\n");
        code.Append("        for preserved_id, preserved_ctrl in other_morph:\n");
        code.Append("            if preserved_id not in mmdd.morphControllers or mmdd.morphControllers[preserved_id] is not preserved_ctrl:\n");
        code.Append("                raise Exception('Another character morph controller changed during cleanup')\n");
        code.Append("        if len(mmdd.soundControllers) != len(sound_backup):\n");
        code.Append("            raise Exception('MMDD audio controller list changed during cleanup')\n");
        code.Append("        for preserved_index in range(len(sound_backup)):\n");
        code.Append("            if mmdd.soundControllers[preserved_index] is not sound_backup[preserved_index]:\n");
        code.Append("                raise Exception('MMDD audio controller changed during cleanup')\n");
        code.Append("        if len(mmdd.timelineControllers) != len(timeline_backup):\n");
        code.Append("            raise Exception('MMDD Timeline controller list changed during cleanup')\n");
        code.Append("        for preserved_index in range(len(timeline_backup)):\n");
        code.Append("            if mmdd.timelineControllers[preserved_index] is not timeline_backup[preserved_index]:\n");
        code.Append("                raise Exception('MMDD Timeline controller changed during cleanup')\n");
        code.Append("        mmdd.refreshTotalFrame()\n");
        code.Append("        mmdd.curFrame = max(float(mmdd.StartFrame), min(old_frame, float(mmdd.EndFrame)))\n");
        code.Append("        has_remaining_vmd = len(mmdd.motionControllers) > 0 or len(mmdd.morphControllers) > 0 or len(mmdd.cameraControllers) > 0 or len(getattr(mmdd, 'vmdControllers', {})) > 0 or getattr(mmdd, 'vmdCameraCtrl', None) is not None\n");
        code.Append("        if not has_remaining_vmd:\n");
        code.Append("            mmdd.lastVmdFilename = None\n");
        code.Append("        game.gdata.kkvrMmdPlaybackGeneration = old_generation + 1\n");
        code.Append("        VRMmddStateBridge.ReportMmdClear(True, target_key, target_name, motion_count, morph_count, camera_count, False, False, None)\n");
        code.Append("        return\n");
        code.Append("    except Exception as clear_error:\n");
        code.Append("        clear_error_text = str(clear_error)\n");
        code.Append("        rollback_errors = []\n");
        code.Append("        rolled_back = False\n");
        code.Append("        needs_rollback = mutation_started or (pause_attempted and old_was_playing)\n");
        code.Append("        if needs_rollback:\n");
        code.Append("            try:\n");
        code.Append("                if mutation_started and backup_ready:\n");
        code.Append("                    mmdd.cameraControllers = camera_backup\n");
        code.Append("                    mmdd.soundControllers = sound_backup\n");
        code.Append("                    mmdd.timelineControllers = timeline_backup\n");
        code.Append("                    kkvr_restore_character_vmd(target_actor, replacement_session)\n");
        code.Append("                    mmdd.lastVmdFilename = old_last_vmd\n");
        code.Append("                else:\n");
        code.Append("                    mmdd.curFrame = max(float(mmdd.StartFrame), min(old_frame, float(mmdd.EndFrame)))\n");
        code.Append("                    if old_was_playing and not mmdd.isPlaying:\n");
        code.Append("                        mmdd.start()\n");
        code.Append("                if bool(mmdd.isPlaying) != old_was_playing:\n");
        code.Append("                    raise Exception('MMDD playback state was not restored')\n");
        code.Append("                game.gdata.kkvrMmdPlaybackGeneration = old_generation\n");
        code.Append("                rolled_back = True\n");
        code.Append("            except Exception as rollback_error:\n");
        code.Append("                rollback_errors.append(str(rollback_error))\n");
        code.Append("        rollback_failed = len(rollback_errors) > 0\n");
        code.Append("        if rollback_failed:\n");
        code.Append("            clear_error_text = clear_error_text + '; rollback failed: ' + ' | '.join(rollback_errors)\n");
        code.Append("        VRMmddStateBridge.ReportMmdClear(False, target_key, target_name, motion_count, morph_count, camera_count, rolled_back, rollback_failed, clear_error_text)\n");
        code.Append("        print('KKVR MMDD clear failed: ' + clear_error_text)\n");
        code.Append("kkvr_clear_selected_character_mmd()\n");
        code.Append(BuildImmediatePlaybackReportCode());
        code.Append(BuildFixedFovReportCode());
        return code.ToString();
    }

    private static string BuildLoadPackageCode(
        string motionPath,
        string cameraPath,
        string audioPath,
        int[] actorObjectKeys,
        string vmdRoot,
        bool resetMmddState)
    {
        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append(BuildStateBridgeImportCode());
        code.Append(BuildMmddStateRecoveryCode(resetMmddState));
        code.Append("config.vmdHomeDir = ").Append(ToPythonString(vmdRoot)).Append("\n");
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
        code.Append("from vmdlib import MotionController, MorphController, SoundController\n");
        code.Append("has_motion_payload = motion_result is not None and (len(motion_result[1]) > 0 or len(motion_result[2]) > 0 or len(motion_result[4]) > 0)\n");
        code.Append("def kkvr_find_controller_id(motion_ctrl, morph_ctrl):\n");
        code.Append("    for candidate_id in list(mmdd.motionControllers.keys()):\n");
        code.Append("        if mmdd.motionControllers[candidate_id] == motion_ctrl:\n");
        code.Append("            return candidate_id\n");
        code.Append("    for candidate_id in list(mmdd.morphControllers.keys()):\n");
        code.Append("        if mmdd.morphControllers[candidate_id] == morph_ctrl:\n");
        code.Append("            return candidate_id\n");
        code.Append("    return None\n");
        code.Append("def kkvr_snapshot_actor(actor):\n");
        code.Append("    import os\n");
        code.Append("    motion_ctrl = mmdd.getMotionController(actor)\n");
        code.Append("    morph_ctrl = mmdd.getMorphController(actor)\n");
        code.Append("    controller_id = kkvr_find_controller_id(motion_ctrl, morph_ctrl)\n");
        code.Append("    if motion_ctrl is None and morph_ctrl is None:\n");
        code.Append("        return {'actor': actor, 'controller_id': None, 'motion_setting': None, 'morph_setting': None, 'motion_index': -1}\n");
        code.Append("    if controller_id is None:\n");
        code.Append("        raise Exception('MMDD could not identify an existing character controller')\n");
        code.Append("    motion_embed = motion_ctrl is not None and (not motion_ctrl.vmdFilePath or not os.path.isfile(motion_ctrl.vmdFilePath))\n");
        code.Append("    morph_embed = morph_ctrl is not None and (not morph_ctrl.vmdFilePath or not os.path.isfile(morph_ctrl.vmdFilePath))\n");
        code.Append("    return {'actor': actor, 'controller_id': controller_id, 'motion_setting': motion_ctrl.exportSetting(motion_embed) if motion_ctrl is not None else None, 'morph_setting': morph_ctrl.exportSetting(morph_embed) if morph_ctrl is not None else None, 'motion_index': mmdd.motionSequence.index(controller_id) if controller_id in mmdd.motionSequence else -1}\n");
        code.Append("def kkvr_remove_actor_vmd(actor):\n");
        code.Append("    motion_ctrl = mmdd.getMotionController(actor)\n");
        code.Append("    morph_ctrl = mmdd.getMorphController(actor)\n");
        code.Append("    if motion_ctrl is not None:\n");
        code.Append("        mmdd.deleteMotionController(motion_ctrl)\n");
        code.Append("    elif morph_ctrl is not None:\n");
        code.Append("        mmdd.deleteMorphController(morph_ctrl)\n");
        code.Append("def kkvr_restore_actor(backup, vmd_frame_buffer):\n");
        code.Append("    actor = backup['actor']\n");
        code.Append("    kkvr_remove_actor_vmd(actor)\n");
        code.Append("    controller_id = backup['controller_id']\n");
        code.Append("    if controller_id is None:\n");
        code.Append("        return\n");
        code.Append("    motion_ctrl = None\n");
        code.Append("    if backup['motion_setting'] is not None:\n");
        code.Append("        motion_ctrl = MotionController.parseFromSetting(backup['motion_setting'], actor, vmd_frame_buffer)\n");
        code.Append("        mmdd.postProcessForMotionImport(actor)\n");
        code.Append("        mmdd.registerMotionController(controller_id, motion_ctrl)\n");
        code.Append("        motion_index = backup['motion_index']\n");
        code.Append("        if motion_index >= 0 and controller_id in mmdd.motionSequence:\n");
        code.Append("            mmdd.motionSequence.remove(controller_id)\n");
        code.Append("            mmdd.motionSequence.insert(min(motion_index, len(mmdd.motionSequence)), controller_id)\n");
        code.Append("    if backup['morph_setting'] is not None:\n");
        code.Append("        morph_param = motion_ctrl if motion_ctrl is not None else actor\n");
        code.Append("        mmdd.morphControllers[controller_id] = MorphController.parseFromSetting(backup['morph_setting'], morph_param, vmd_frame_buffer)\n");
        code.Append("character_backups = [kkvr_snapshot_actor(actor) for actor in actors] if has_motion_payload else []\n");
        code.Append("camera_backup = list(mmdd.cameraControllers) if camera_result is not None else None\n");
        code.Append("sound_backup = list(mmdd.soundControllers) if audio_path else None\n");
        code.Append("old_last_vmd = mmdd.lastVmdFilename\n");
        code.Append("old_last_wav = mmdd.lastWavFilename\n");
        code.Append("old_was_playing = bool(mmdd.isPlaying)\n");
        code.Append("old_frame = float(mmdd.curFrame)\n");
        code.Append("new_sound = None\n");
        code.Append("try:\n");
        code.Append("    if old_was_playing:\n");
        code.Append("        mmdd.stop()\n");
        code.Append("    if has_motion_payload:\n");
        code.Append("        for actor in actors:\n");
        code.Append("            kkvr_remove_actor_vmd(actor)\n");
        code.Append("    if motion_result:\n");
        code.Append("        for actor in actors:\n");
        code.Append("            if len(motion_result[1]) > 0 or len(motion_result[4]) > 0:\n");
        code.Append("                motion_ctrl = mmdd.createMotionController(actor)\n");
        code.Append("                motion_ctrl.importBoneKeyframes(motion_path, motion_result[1])\n");
        code.Append("                motion_ctrl.importIkKeyframes(motion_result[4])\n");
        code.Append("                mmdd.postProcessForMotionImport(actor)\n");
        code.Append("            if len(motion_result[2]) > 0:\n");
        code.Append("                morph_ctrl = mmdd.createMorphController(actor)\n");
        code.Append("                morph_ctrl.importMorphKeyframes(motion_path, motion_result[2])\n");
        code.Append("        mmdd.lastVmdFilename = motion_path\n");
        code.Append("    if camera_result:\n");
        code.Append("        mmdd.cameraControllers = []\n");
        code.Append("        camera_ctrl = mmdd.loadCameraKeyframes(camera_path, camera_result[3])\n");
        code.Append("        if camera_ctrl is None:\n");
        code.Append("            raise Exception('MMDD could not create the camera controller')\n");
        code.Append("        camera_ctrl.switchMinFramePeriod = config.switchMinFramePeriod\n");
        code.Append("        camera_ctrl.switchMaxFramePeriod = config.switchMaxFramePeriod\n");
        code.Append("        camera_ctrl.switchProbability = config.switchProbability\n");
        code.Append("        camera_ctrl.fixedFOV = config.useFixedFOVValue\n");
        code.Append("        if config.useFixedFOV:\n");
        code.Append("            camera_ctrl.postProcess.add('fixed_fov')\n");
        code.Append("        mmdd.lastVmdFilename = camera_path if not motion_path else motion_path\n");
        code.Append("    if audio_path:\n");
        code.Append("        new_sound = SoundController()\n");
        code.Append("        audio_error = new_sound.loadWavFile(audio_path)\n");
        code.Append("        if audio_error:\n");
        code.Append("            new_sound.cleanup()\n");
        code.Append("            new_sound = None\n");
        code.Append("            raise Exception('Audio load failed: ' + str(audio_error))\n");
        code.Append("        mmdd.soundControllers = [new_sound]\n");
        code.Append("        mmdd.lastWavFilename = audio_path\n");
        code.Append("    mmdd.refreshTotalFrame()\n");
        code.Append("    mmdd.curFrame = float(mmdd.StartFrame)\n");
        code.Append("    if not mmdd.verify():\n");
        code.Append("        raise Exception('MMDD verification failed after loading VMD')\n");
        code.Append("    mmdd.update(True)\n");
        code.Append("    game.gdata.kkvrMmdPlaybackGeneration = int(getattr(game.gdata, 'kkvrMmdPlaybackGeneration', 0)) + 1\n");
        code.Append("except Exception as load_error:\n");
        code.Append("    load_error_text = str(load_error)\n");
        code.Append("    rollback_errors = []\n");
        code.Append("    restore_buffer = {}\n");
        code.Append("    for backup in character_backups:\n");
        code.Append("        try:\n");
        code.Append("            kkvr_restore_actor(backup, restore_buffer)\n");
        code.Append("        except Exception as actor_rollback_error:\n");
        code.Append("            rollback_errors.append(str(actor_rollback_error))\n");
        code.Append("    if camera_backup is not None:\n");
        code.Append("        mmdd.cameraControllers = camera_backup\n");
        code.Append("    if sound_backup is not None:\n");
        code.Append("        if new_sound is not None:\n");
        code.Append("            try:\n");
        code.Append("                new_sound.cleanup()\n");
        code.Append("            except Exception as sound_cleanup_error:\n");
        code.Append("                rollback_errors.append(str(sound_cleanup_error))\n");
        code.Append("        mmdd.soundControllers = sound_backup\n");
        code.Append("    mmdd.lastVmdFilename = old_last_vmd\n");
        code.Append("    mmdd.lastWavFilename = old_last_wav\n");
        code.Append("    try:\n");
        code.Append("        mmdd.refreshTotalFrame()\n");
        code.Append("        mmdd.curFrame = max(float(mmdd.StartFrame), min(old_frame, float(mmdd.EndFrame)))\n");
        code.Append("        if not mmdd.verify():\n");
        code.Append("            raise Exception('MMDD verification failed during rollback')\n");
        code.Append("        mmdd.update(True)\n");
        code.Append("        if old_was_playing:\n");
        code.Append("            mmdd.start()\n");
        code.Append("            if not mmdd.isPlaying:\n");
        code.Append("                raise Exception('MMDD could not resume playback during rollback')\n");
        code.Append("    except Exception as state_rollback_error:\n");
        code.Append("        rollback_errors.append(str(state_rollback_error))\n");
        code.Append("    if len(rollback_errors) > 0:\n");
        code.Append("        raise Exception('VMD load failed: ' + load_error_text + '; rollback failed: ' + ' | '.join(rollback_errors))\n");
        code.Append("    raise Exception('VMD load failed and was rolled back: ' + load_error_text)\n");
        code.Append("if sound_backup is not None:\n");
        code.Append("    for old_sound in sound_backup:\n");
        code.Append("        try:\n");
        code.Append("            old_sound.cleanup()\n");
        code.Append("        except:\n");
        code.Append("            pass\n");
        code.Append(BuildImmediatePlaybackReportCode());
        code.Append("game.visible = 0\n");
        return code.ToString();
    }

    private static string BuildMmddStateRecoveryCode(bool resetMmddState)
    {
        if (resetMmddState)
        {
            // The retry must not destroy valid camera/audio/live-character state.
            // Repeat the targeted stale-controller cleanup, then refresh VNGE's
            // actor registry instead of calling mmdd.cleanup().
            return BuildMmddStateRecoveryCode(false)
                + "game.scenef_clean_actorsprops()\n";
        }

        return
            "kkvr_live_char_ctrls = [x.objctrl for x in game.scene_get_all_females()] + [x.objctrl for x in game.scene_get_all_males()]\n"
            + "def kkvr_motion_is_live(ctrl):\n"
            + "    try:\n"
            + "        if getattr(ctrl, 'boneType', None) != 'chara':\n"
            + "            return True\n"
            + "        objctrl = ctrl.ociChar\n"
            + "        return objctrl in kkvr_live_char_ctrls and objctrl.charInfo != None and objctrl.charInfo.gameObject != None and ctrl.actualBoneRootGo != None\n"
            + "    except:\n"
            + "        return False\n"
            + "for kkvr_aid in list(mmdd.motionControllers.keys()):\n"
            + "    if not kkvr_motion_is_live(mmdd.motionControllers[kkvr_aid]):\n"
            + "        try:\n"
            + "            mmdd.deleteMotionController(kkvr_aid)\n"
            + "        except:\n"
            + "            if kkvr_aid in mmdd.motionControllers:\n"
            + "                del mmdd.motionControllers[kkvr_aid]\n"
            + "            if kkvr_aid in mmdd.motionSequence:\n"
            + "                mmdd.motionSequence.remove(kkvr_aid)\n"
            + "            if kkvr_aid in mmdd.morphControllers:\n"
            + "                del mmdd.morphControllers[kkvr_aid]\n"
            + "for kkvr_aid in list(mmdd.morphControllers.keys()):\n"
            + "    kkvr_mp = mmdd.morphControllers[kkvr_aid]\n"
            + "    kkvr_morph_live = kkvr_mp.motionCtrl != None and kkvr_motion_is_live(kkvr_mp.motionCtrl)\n"
            + "    if kkvr_morph_live and kkvr_mp.blendRendererInfo:\n"
            + "        kkvr_morph_live = all([x[0] != None for x in kkvr_mp.blendRendererInfo.values()])\n"
            + "    if not kkvr_morph_live:\n"
            + "        try:\n"
            + "            mmdd.deleteMorphController(kkvr_aid)\n"
            + "        except:\n"
            + "            if kkvr_aid in mmdd.morphControllers:\n"
            + "                del mmdd.morphControllers[kkvr_aid]\n"
            + "mmdd.refreshTotalFrame()\n";
    }

    private static bool RequiresMmddStateRecovery(string error)
    {
        return !string.IsNullOrEmpty(error)
            && (error.IndexOf("NullReferenceException", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("GetComponentsInChildren", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("destroyed", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsMmddRuntimeUnavailable(string error)
    {
        return !string.IsNullOrEmpty(error)
            && (error.IndexOf("Unity.Console is not loaded", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("VNGE Python engine is not ready", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("MMD Director could not be started", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("No module named mmddirector", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool ExecuteFixedFovAction(
        string action,
        string operation,
        out string status)
    {
        return ExecuteFixedFovAction(action, operation, true, out status);
    }

    private static bool ExecuteFixedFovAction(
        string action,
        string operation,
        bool persist,
        out string status)
    {
        VRMmddStateBridge.ResetFixedFov();
        bool preparedSynchronousAnchor = !string.IsNullOrEmpty(action)
            && VRMmdCameraAnchorController.PrepareForSynchronousCameraEvaluation();
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
            if (persist)
                code.Append("mmddirector.saveConfig(game)\n");
        }
        code.Append(BuildFixedFovReportCode());

        string error;
        if (!VRPythonBridge.TryExecute(code.ToString(), operation, out error))
        {
            if (preparedSynchronousAnchor)
                VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
            status = "Fixed FOV 无法控制：" + error;
            return false;
        }

        if (!VRMmddStateBridge.FixedFovReported)
        {
            if (preparedSynchronousAnchor)
                VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
            status = "MMDD 没有返回 Fixed FOV 状态";
            return false;
        }

        if (!string.IsNullOrEmpty(action)
            && VRMmddStateBridge.PlaybackReported
            && !VRMmddStateBridge.PlaybackIsPlaying)
        {
            VRMmdCameraAnchorController.CorrectAfterSynchronousCameraEvaluation();
        }
        else if (preparedSynchronousAnchor)
        {
            VRMmdCameraAnchorController.CancelPreparedSynchronousCameraEvaluation();
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
        if (!string.IsNullOrEmpty(action))
        {
            // Apply the selected actor only. A global mmdd.update() also rewrites the VR camera.
            code.Append("if not mmdd.isPlaying:\n");
            code.Append("    mc.setFrame(mmdd.curFrame)\n");
        }
        code.Append("rot = mc.getHeelzRotation()\n");
        code.Append("VRMmddStateBridge.ReportHighHeels(target_key, mc.CharName, mc.HighHeels.PluginName or 'Unknown', bool(mc.HighHeels.enable), bool(mc.ShoesDetect), float(rot[0]), float(rot[1]), float(rot[2]), bool(mc.ShoesOffset[0]), float(mc.ShoesOffset[1]), float(mc.ShoesOffset[2]))\n");

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

    private static string BuildImmediatePlaybackReportCode()
    {
        return
            "playback_generation = int(getattr(game.gdata, 'kkvrMmdPlaybackGeneration', 0))\n"
            + "direct_vr_camera_owner = False\n"
            + "try:\n"
            + "    studio_obj = getattr(game, 'studio', None)\n"
            + "    object_camera_active = studio_obj is not None and getattr(studio_obj, 'ociCamera', None) is not None\n"
            + "    if not object_camera_active:\n"
            + "        for camera_controller in getattr(mmdd, 'cameraControllers', []):\n"
            + "            if bool(getattr(camera_controller, 'enable', True)) and getattr(camera_controller, 'bindCameraTrans', None) is None and getattr(camera_controller, 'vrOriginTransform', None) is not None:\n"
            + "                direct_vr_camera_owner = True\n"
            + "                break\n"
            + "except:\n"
            + "    direct_vr_camera_owner = False\n"
            + "VRMmddStateBridge.ReportPlayback(True, bool(mmdd.isPlaying), float(mmdd.curFrame), float(mmdd.StartFrame), float(mmdd.EndFrame), playback_generation, direct_vr_camera_owner)\n";
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

    private static bool IsDeferredHighHeelsStatus(string status)
    {
        return !string.IsNullOrEmpty(status)
            && (status.IndexOf("请先给这个角色读取动作 VMD", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("motion controller", StringComparison.OrdinalIgnoreCase) >= 0
                || status.IndexOf("MMDD", StringComparison.OrdinalIgnoreCase) >= 0);
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

    private static string ToPythonBool(bool value)
    {
        return value ? "True" : "False";
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

    public static bool CompleteCharacterRemoval(
        VRMmddCharacterReplacementSession session,
        out string status)
    {
        status = null;
        if (session == null || string.IsNullOrEmpty(session.Token))
            return true;

        StringBuilder code = new StringBuilder();
        code.Append(BuildBootstrapCode());
        code.Append(BuildStateBridgeImportCode());
        code.Append("session_token = ").Append(ToPythonString(session.Token)).Append("\n");
        code.Append("if not hasattr(game.gdata, 'kkvrCharacterReplacementVmdSessions') or session_token not in game.gdata.kkvrCharacterReplacementVmdSessions:\n");
        code.Append("    raise Exception('The temporary VMD removal session is no longer available')\n");
        code.Append("removal_session = game.gdata.kkvrCharacterReplacementVmdSessions[session_token]\n");
        code.Append("del game.gdata.kkvrCharacterReplacementVmdSessions[session_token]\n");
        code.Append("mmdd.refreshTotalFrame()\n");
        code.Append("target_frame = max(float(mmdd.StartFrame), min(float(removal_session['resume_frame']), float(mmdd.EndFrame)))\n");
        code.Append("mmdd.curFrame = target_frame\n");
        code.Append("mmdd.update(True)\n");
        code.Append("if removal_session['was_playing'] and not mmdd.isPlaying:\n");
        code.Append("    mmdd.start()\n");
        code.Append("    if not mmdd.isPlaying:\n");
        code.Append("        raise Exception('MMDD could not resume after character removal')\n");
        code.Append(BuildImmediatePlaybackReportCode());

        string error;
        if (!VRPythonBridge.TryExecute(
            code.ToString(),
            "MMDD character removal complete",
            out error))
        {
            status = "人物已移除，但 MMDD 恢复失败：" + error;
            return false;
        }

        status = session.WasPlaying
            ? "MMDD 已移除该人物动作并继续播放"
            : "MMDD 已移除该人物动作";
        return true;
    }
}

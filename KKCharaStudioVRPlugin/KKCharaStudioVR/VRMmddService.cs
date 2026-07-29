namespace KKCharaStudioVR;

internal static class VRMmddService
{
    public static bool TogglePlayPause(out string status)
    {
        const string code =
            "from vngameengine import vnge_game\n" +
            "import mmddirector\n" +
            "game = vnge_game\n" +
            "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n" +
            "    mmddirector.start(game)\n" +
            "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n" +
            "    raise Exception('MMD Director could not be started')\n" +
            "game.gdata.mmdd.startStop()\n";

        string error;
        if (VRPythonBridge.TryExecute(code, "MMDD play/pause", out error))
        {
            status = "MMDD 播放状态已切换";
            return true;
        }

        status = "MMDD 无法控制：" + error;
        return false;
    }

    public static bool OpenVmdBrowser(out string status)
    {
        const string code =
            "from vngameengine import vnge_game\n" +
            "import mmddirector\n" +
            "game = vnge_game\n" +
            "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n" +
            "    mmddirector.start(game)\n" +
            "if not hasattr(game.gdata, 'mmdd') or game.gdata.mmdd is None:\n" +
            "    raise Exception('MMD Director could not be started')\n" +
            "config = game.gdata.mmdd_config\n" +
            "if config.fileDialog != 'vnge':\n" +
            "    config.fileDialog = 'vnge'\n" +
            "    try:\n" +
            "        mmddirector.saveConfig(game)\n" +
            "    except:\n" +
            "        pass\n" +
            "def kkvr_on_vmd_selected(files):\n" +
            "    if files and len(files) > 0:\n" +
            "        game.gdata.mmdd.lastVmdFilename = files[0]\n" +
            "        mmddirector.guiImportVmdData_normal(game)\n" +
            "last_file = game.gdata.mmdd.lastVmdFilename\n" +
            "if not last_file:\n" +
            "    last_file = config.vmdHomeDir\n" +
            "mmddirector.guiOpenFileDialog(game, kkvr_on_vmd_selected, last_file, True, 'VMD')\n";

        string error;
        if (VRPythonBridge.TryExecute(code, "MMDD in-game VMD browser", out error))
        {
            status = "已打开游戏内 VMD 浏览器";
            return true;
        }

        status = "VMD 浏览器无法打开：" + error;
        return false;
    }
}

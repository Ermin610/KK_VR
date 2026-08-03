using System;
using System.Collections;
using UnityEngine;
using VRGIN.Core;

namespace KKCharaStudioVR;

public sealed partial class VRWristMenuController
{
    private sealed class VRCharacterOutfitRestoreResult
    {
        public bool Success;
        public string Status;
    }

    private IEnumerator MonitorCharacterOutfitRestore(
        VRCharacterOutfitRestoreSession session,
        VRCharacterOutfitRestoreResult result)
    {
        if (result == null)
            yield break;
        result.Success = false;
        result.Status = L(
            "旧服装恢复没有启动",
            "元の衣装の復元を開始できませんでした",
            "The previous outfit restore did not start");
        if (session == null)
            yield break;

        float warningDeadline = Time.realtimeSinceStartup + 30f;
        float hardDeadline = Time.realtimeSinceStartup + 90f;
        bool warned = false;
        while (true)
        {
            bool completed;
            bool success;
            string status;
            try
            {
                completed = session.TryGetCompletion(out success, out status);
            }
            catch (Exception ex)
            {
                if (session.CanAbortSafely)
                {
                    try
                    {
                        session.Abort();
                    }
                    catch (Exception abortException)
                    {
                        VRLog.Warn("Character outfit rollback failed: " + abortException.Message);
                    }
                    status = session.TerminalRecoveryStatus
                        ?? ("恢复旧服装异常，已请求回滚：" + ex.Message);
                }
                else
                {
                    status = L(
                        "旧服装仍可能在后台恢复：",
                        "元の衣装はバックグラウンドで復元中の可能性があります：",
                        "The previous outfit may still be restoring in the background: ")
                        + ex.Message;
                    StartCoroutine(MonitorCharacterOutfitRestoreInBackground(session));
                    result.Status = status;
                    yield break;
                }
                completed = !session.RequiresBackgroundMonitoring;
                success = false;
            }

            if (completed)
            {
                result.Success = success;
                result.Status = status;
                yield break;
            }

            if (Time.realtimeSinceStartup >= warningDeadline && !warned)
            {
                warned = true;
                SetStatus(
                    L(
                        "旧服装和材质仍在恢复，请稍候…",
                        "元の衣装とマテリアルを復元中です…",
                        "Restoring the previous outfit and materials…"),
                    new Color(1f, 0.72f, 0.25f, 1f),
                    0f);
            }

            if (Time.realtimeSinceStartup >= hardDeadline)
            {
                if (session.CanAbortSafely)
                {
                    session.Abort();
                    hardDeadline = Time.realtimeSinceStartup + 30f;
                }
                else
                {
                    result.Status = L(
                        "旧服装恢复耗时过长，已转入后台监控",
                        "衣装復元に時間がかかるため、バックグラウンド監視へ移行しました",
                        "Outfit restoration is taking too long and is now monitored in the background");
                    StartCoroutine(MonitorCharacterOutfitRestoreInBackground(session));
                    yield break;
                }
            }
            yield return null;
        }
    }

    private IEnumerator MonitorCharacterOutfitRestoreInBackground(
        VRCharacterOutfitRestoreSession session)
    {
        if (session == null)
            yield break;
        while (true)
        {
            yield return null;
            bool complete;
            bool success;
            string status;
            try
            {
                complete = session.TryGetCompletion(out success, out status);
            }
            catch (Exception ex)
            {
                VRLog.Warn("Background character outfit restore will retry: " + ex.Message);
                continue;
            }
            if (!complete)
                continue;
            if (success)
                VRLog.Info(status);
            else
                VRLog.Warn(status);
            yield break;
        }
    }
}

using System;
using System.Runtime.InteropServices;

namespace KKCharaStudioVR
{
    internal static class KeyboradSimulatorUtil
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte VK_END = 0x23; // END key code
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        public static void PressEndKey()
        {
            try
            {
                // Press End key
                keybd_event(VK_END, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                // Release End key
                keybd_event(VK_END, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
                VRGIN.Core.VRLog.Info("KeyboardSimulator: Successfully simulated End keypress for ReShade toggle!");
            }
            catch (Exception ex)
            {
                VRGIN.Core.VRLog.Error("KeyboardSimulator: PressEndKey failed: " + ex.Message);
            }
        }
    }
}

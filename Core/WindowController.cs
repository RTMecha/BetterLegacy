using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using System;
using System.Runtime.InteropServices;

namespace BetterLegacy.Core
{
    public static class WindowController
    {
        [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Unicode)]
        public static extern bool SetWindowText(IntPtr hwnd, string lpString);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string className, string windowName);

        public static UnityEngine.Vector2Int WindowCenter => new UnityEngine.Vector2Int((UnityEngine.Display.main.systemWidth - UnityEngine.Screen.width) / 2, (UnityEngine.Display.main.systemHeight - UnityEngine.Screen.height) / 2);

        public static UnityEngine.Vector2Int CurrentPosition { get; set; }
        public static UnityEngine.Vector2Int CurrentResolution { get; set; }

        static IntPtr windowHandle;
        public static IntPtr WindowHandle
           => windowHandle == IntPtr.Zero
               ? windowHandle = FindWindow(null, UnityEngine.Application.productName)
               : windowHandle;

        public static void SetPosition(int x, int y)
        {
            CurrentPosition = new UnityEngine.Vector2Int(x, y);
            SetWindowPos(WindowHandle, 0, x, y, 0, 0, 1);
        }

        public static void SetResolution(Resolutions value, bool fullScreen = false)
        {
            var res = DataManager.inst.resolutions[(int)value];

            SetResolution((int)res.x, (int)res.y, fullScreen);
        }

        public static void SetResolution(int x, int y, bool fullScreen = false)
        {
            CurrentResolution = new UnityEngine.Vector2Int(x, y);

            if (GameManager.inst && !EditorManager.inst && (GameManager.inst.gameState == GameManager.State.Paused || GameManager.inst.gameState == GameManager.State.Finish))
                ResetResolution();

            UnityEngine.Screen.SetResolution(x < 0 ? 1280 : x, y < 0 ? 720 : y, fullScreen);
        }

        public static void ResetResolution(bool setPosition = true)
        {
            if (setPosition)
                SetPosition(WindowCenter.x, WindowCenter.y);

            SetResolution(CoreConfig.Instance.Resolution.Value, CoreConfig.Instance.Fullscreen.Value);
        }

        public static void SetTitle(string title) => SetWindowText(WindowHandle, title);

        public static void ResetTitle() => SetWindowText(WindowHandle, ProjectArrhythmia.Title);
    }
}

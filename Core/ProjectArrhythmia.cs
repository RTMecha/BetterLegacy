using System;
using System.Runtime.InteropServices;

using UnityEngine;

using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;

using Version = BetterLegacy.Core.Data.Version;

namespace BetterLegacy.Core
{
    /// <summary>
    /// Handles the current instance of Project Arrhythmia.
    /// </summary>
    public static class ProjectArrhythmia
    {
        #region Values

        /// <summary>
        /// The version of PA Legacy. Calculated from version 4.0.19 onwards.
        /// </summary>
        public const string GAME_VERSION = "4.1.16";

        /// <summary>
        /// The version vanilla PA Legacy uses.
        /// </summary>
        public const string VANILLA_VERSION = "20.4.4";

        /// <summary>
        /// The Steam App ID of Project Arrhythmia.
        /// </summary>
        public const uint STEAM_APP_ID = 440310U;

        /// <summary>
        /// The game version of PA Legacy.
        /// </summary>
        public static Version GameVersion = new Version(GAME_VERSION);

        /// <summary>
        /// The vanilla version number of PA Legacy.
        /// </summary>
        public static Version VanillaVersion = new Version(Versions.LEGACY);

        #endregion

        #region Methods

        /// <summary>
        /// Checks if the version matches PA Legacy's version.
        /// </summary>
        /// <param name="version">Version number to compare.</param>
        /// <returns>Returns true if the version number matches the PA Legacy version numbers.</returns>
        public static bool IsMatchingVersion(string version) => version == GAME_VERSION || version == Versions.LEGACY;

        /// <summary>
        /// Checks if the version matches PA Legacy's version.
        /// </summary>
        /// <param name="version">Version number to compare.</param>
        /// <returns>Returns true if the version number matches the PA Legacy version numbers.</returns>
        public static bool IsMatchingVersion(Version version) => version == GameVersion || version == VanillaVersion;

        /// <summary>
        /// Checks if an update is required.
        /// </summary>
        /// <param name="version">Version to compare.</param>
        /// <returns>Returns true if the version doesn't match the PA Legacy versions.</returns>
        public static bool RequireUpdate(string version) => version != GAME_VERSION && version != Versions.LEGACY;

        /// <summary>
        /// Checks if an update is required.
        /// </summary>
        /// <param name="version">Version to compare.</param>
        /// <returns>Returns true if the version doesn't match the PA Legacy versions.</returns>
        public static bool RequireUpdate(Version version) => version != GameVersion && version != VanillaVersion;

        #endregion

        /// <summary>
        /// Application window of the current instance of Project Arrhythmia.
        /// </summary>
        public static class Window
        {
            #region Values

            /// <summary>
            /// The default application title of Project Arrhythmia.
            /// </summary>
            public const string TITLE = "Project Arrhythmia";

            /// <summary>
            /// Center origin of the window.
            /// </summary>
            public static Vector2Int WindowOrigin => new Vector2Int((Display.main.systemWidth - Screen.width) / 2, (Display.main.systemHeight - Screen.height) / 2);

            /// <summary>
            /// The current position of the window.
            /// </summary>
            public static Vector2Int CurrentPosition { get; set; }

            /// <summary>
            /// The current resolution of the window.
            /// </summary>
            public static Vector2Int CurrentResolution { get; set; }

            static HandleIntPtr handle;
            /// <summary>
            /// The window handle.
            /// </summary>
            public static IntPtr Handle
            {
                get
                {
                    if (!handle)
                        handle = new HandleIntPtr(FindWindow(null, Application.productName));
                    return handle.peter;
                }
            }

            class HandleIntPtr : Exists
            {
                public HandleIntPtr(IntPtr peter) => this.peter = peter;

                public IntPtr peter;
            }

            /// <summary>
            /// Position or resolution changed.
            /// </summary>
            public static bool positionResolutionChanged;

            /// <summary>
            /// If the window is currently borderless.
            /// </summary>
            public static bool borderless;

            #endregion

            #region Methods

            #region External

            [DllImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
            static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

            [DllImport("user32.dll", EntryPoint = "SetWindowText", CharSet = CharSet.Unicode)]
            static extern bool SetWindowText(IntPtr hwnd, string lpString);

            [DllImport("user32.dll")]
            static extern IntPtr FindWindow(string className, string windowName);

            [DllImport("user32.dll")]
            static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, uint dwNewLong);

            [DllImport("user32.dll")]
            static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

            #endregion

            /// <summary>
            /// Sets the position and resolution of the window.
            /// </summary>
            /// <param name="x">Position X to set.</param>
            /// <param name="y">Position Y to set.</param>
            /// <param name="width">Width to set.</param>
            /// <param name="height">Height to set.</param>
            /// <param name="windowFlags">Window flags.</param>
            public static void SetPositionResolution(int x, int y, int width, int height, int windowFlags)
            {
                CurrentPosition = new Vector2Int(x, y);
                SetWindowPos(Handle, 0, x, y, width, height, windowFlags);
            }

            /// <summary>
            /// Sets the position of the window.
            /// </summary>
            /// <param name="x">Position X to set.</param>
            /// <param name="y">Position Y to set.</param>
            public static void SetPosition(int x, int y)
            {
                CurrentPosition = new Vector2Int(x, y);
                SetWindowPos(Handle, 0, x, y, 0, 0, 1);
            }

            /// <summary>
            /// Sets the resolution of the window.
            /// </summary>
            /// <param name="value">Resolution type.</param>
            /// <param name="fullScreen">If the window should be fullscreen.</param>
            public static void SetResolution(ResolutionType value, bool fullscreen = false)
            {
                var resolution = value.Resolution;

                SetResolution((int)resolution.x, (int)resolution.y, fullscreen);
            }

            /// <summary>
            /// Sets the resolution of the window.
            /// </summary>
            /// <param name="width">Width to set.</param>
            /// <param name="height">Height to set.</param>
            /// <param name="fullScreen">If the window should be fullscreen.</param>
            public static void SetResolution(int width, int height, bool fullscreen = false)
            {
                CurrentResolution = new Vector2Int(width, height);

                var borderless = CoreConfig.Instance.BorderlessFullscreen.Value;
                if (!fullscreen && borderless)
                {
                    Window.borderless = false;
                    Screen.fullScreen = true;
                    CoroutineHelper.PerformAtNextFrame(() => Screen.SetResolution(width < 0 ? 1280 : width, height < 0 ? 720 : height, false));
                    return;
                }

                Screen.SetResolution(width < 0 ? 1280 : width, height < 0 ? 720 : height, fullscreen && !borderless);

                // perform at next frame because unity is annoying
                CoroutineHelper.PerformAtNextFrame(() => SetBorderState(borderless));
            }

            /// <summary>
            /// Resets the window position.
            /// </summary>
            public static void ResetPosition()
            {
                var windowOrigin = WindowOrigin;
                SetPosition(windowOrigin.x, windowOrigin.y);
            }

            /// <summary>
            /// Sets the borderless state of the window.
            /// </summary>
            /// <param name="borderless">Borderless state to set.</param>
            public static void SetBorderState(bool borderless)
            {
                if (borderless)
                {
                    SetWindowLong(Handle, -16, 2415919104U);
                    SetPositionResolution(0, 0, Display.main.systemWidth, Display.main.systemHeight, 0);
                }

                Window.borderless = borderless;
            }

            /// <summary>
            /// Sets the window title.
            /// </summary>
            /// <param name="title">Title to set.</param>
            public static void SetTitle(string title) => SetWindowText(Handle, title);

            /// <summary>
            /// Resets the window title.
            /// </summary>
            public static void ResetTitle() => SetTitle(TITLE);

            /// <summary>
            /// Applies the window settings.
            /// </summary>
            public static void ApplySettings()
            {
                SetResolution(CoreConfig.Instance.Resolution.Value, CoreConfig.Instance.Fullscreen.Value);

                QualitySettings.vSyncCount = CoreConfig.Instance.VSync.Value ? 1 : 0;
                QualitySettings.antiAliasing = 0;

                int fps = CoreConfig.Instance.FPSLimit.Value;
                if (fps != -1) // limit FPS if FPS is not set to unlimited.
                    fps = RTMath.Clamp(fps, 15, int.MaxValue);

                Application.targetFrameRate = fps;
                CoreHelper.Log($"Apply Video Settings\nResolution: [{Screen.currentResolution}]\nFullscreen: [{Screen.fullScreen}]\nVSync Count: [{QualitySettings.vSyncCount}]");
            }

            #endregion
        }

        /// <summary>
        /// Library of PA updates.
        /// </summary>
        public static class Versions
        {
            /// <summary>
            /// Legacy branch version.
            /// </summary>
            public const string LEGACY = "20.4.4";
            /// <summary>
            /// Depths' default value was changed.
            /// </summary>
            public const string DEPTH_DEFAULT_CHANGED = "23.1.4";
            /// <summary>
            /// Opacity slider was added.
            /// </summary>
            public const string OPACITY = "24.1.6a";
            /// <summary>
            /// Mode value was added to gradient event keyframes.
            /// </summary>
            public const string GRADIENT_MODE_EVENT = "24.1.7a";
            /// <summary>
            /// Text sprites changed and player bubble.
            /// </summary>
            public const string SPRITES_CHANGED = "24.2.2";
            /// <summary>
            /// Triggers were added.
            /// </summary>
            public const string EVENT_TRIGGER = "24.3.1";
            /// <summary>
            /// The Polygon shape was added.
            /// </summary>
            public const string CUSTOM_SHAPES = "25.2.1";
            /// <summary>
            /// Rotation keyframes now have a fixed setting and shake has more values.
            /// </summary>
            public const string FIXED_ROTATION_SHAKE = "25.2.2";
            /// <summary>
            /// Prefab Objecs now have "parenting".
            /// </summary>
            public const string PREFAB_PARENTING = "25.6.1";
        }
    }
}

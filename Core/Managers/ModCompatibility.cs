using System;
using System.Collections.Generic;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Runtime;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class is used to share mod variables and functions, as well as check if a mod is installed.
    /// </summary>
    public class ModCompatibility : BaseManager<ModCompatibility, ManagerSettings>
    {
        #region Values

        /// <summary>
        /// BepInEx game object.
        /// </summary>
        public static GameObject bepinex;

        /// <summary>
        /// Dictionary of shared values.
        /// </summary>
        public static Dictionary<string, object> sharedValues = new Dictionary<string, object>();

        /// <summary>
        /// If EditorOnStartup is installed.
        /// </summary>
        public static bool EditorOnStartupInstalled { get; set; }

        /// <summary>
        /// If Example should be loaded due to EditorOnStartup loading a scene differently.
        /// </summary>
        public static bool ShouldLoadExample { get; set; }

        /// <summary>
        /// If UnityExplorer is installed.
        /// </summary>
        public static bool UnityExplorerInstalled { get; private set; }

        /// <summary>
        /// UnityExplorer Inspector type.
        /// </summary>
        public static Type UEInspector => UnityExplorerInstalled ? AccessTools.TypeByName("UnityExplorer.InspectorManager") : null;

        /// <summary>
        /// UnityExplorer UI Manager type.
        /// </summary>
        public static Type UEUIManager => UnityExplorerInstalled ? AccessTools.TypeByName("UnityExplorer.UI.UIManager") : null;

        #endregion

        #region Functions

        public override void OnInit()
        {
            bepinex = GameObject.Find("BepInEx_Manager");

            UnityExplorerInstalled = bepinex.GetComponentByName("ExplorerBepInPlugin");
        }

        public static void Inspect(object obj)
        {
            if (!UnityExplorerInstalled)
                return;

            var ui = UEUIManager;
            var inspector = UEInspector;
            ui.GetProperty("ShowMenu").SetValue(ui, true);
            inspector.GetMethod("Inspect", new[] { typeof(object), AccessTools.TypeByName("UnityExplorer.CacheObject.CacheObjectBase") })
            .Invoke(inspector, new object[] { obj, null });
            AchievementManager.inst.UnlockAchievement("hackerman");
        }

        public static void ShowExplorer()
        {
            if (!UnityExplorerInstalled)
                return;

            var ui = UEUIManager;
            ui.GetProperty("ShowMenu").SetValue(ui, true);
            AchievementManager.inst.UnlockAchievement("hackerman");
        }

        public static void HideExplorer()
        {
            if (!UnityExplorerInstalled)
                return;

            var ui = UEUIManager;
            ui.GetProperty("ShowMenu").SetValue(ui, false);
        }

        public static void ToggleExplorer()
        {
            if (!UnityExplorerInstalled)
                return;

            var ui = UEUIManager;
            var enabled = (bool)ui.GetProperty("ShowMenu").GetValue(ui);
            ui.GetProperty("ShowMenu").SetValue(ui, !enabled);

            if (!enabled)
                AchievementManager.inst.UnlockAchievement("hackerman");
        }

        #endregion

        InspectorDebugList inspectorDebugList = new InspectorDebugList();

        class InspectorDebugList
        {
            public GameData CurrentGameData => GameData.Current;
            public RTLevel CurrentRuntimeLevel => RTLevel.Current;
            public RTBeatmap CurrentBeatmap => RTBeatmap.Current;
        }
    }
}

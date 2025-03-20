using System;
using System.Collections.Generic;

using UnityEngine;

using HarmonyLib;

namespace BetterLegacy.Core.Managers
{
    /// <summary>
    /// This class is used to share mod variables and functions, as well as check if a mod is installed.
    /// </summary>
    public class ModCompatibility : MonoBehaviour
    {
        public static ModCompatibility inst;

        public static GameObject bepinex;

        public static Dictionary<string, object> sharedFunctions = new Dictionary<string, object>();

        /// <summary>
        /// Inits ModCompatibility.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(ModCompatibility), SystemManager.inst.transform).AddComponent<ModCompatibility>();

        void Awake()
        {
            inst = this;

            bepinex = GameObject.Find("BepInEx_Manager");

            UnityExplorerInstalled = bepinex.GetComponentByName("ExplorerBepInPlugin");
        }

        public static bool EditorOnStartupInstalled { get; set; }

        public static bool ShouldLoadExample { get; set; }

        public static bool UnityExplorerInstalled { get; private set; }
        public static Type UEInspector => UnityExplorerInstalled ? AccessTools.TypeByName("UnityExplorer.InspectorManager") : null;
        public static Type UEUIManager => UnityExplorerInstalled ? AccessTools.TypeByName("UnityExplorer.UI.UIManager") : null;

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
    }
}

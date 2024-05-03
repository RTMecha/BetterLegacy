using BetterLegacy.Core.Helpers;
using BetterLegacy.Example;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(SceneManager))]
    public class SceneManagerPatch
    {
        [HarmonyPatch("DisplayLoadingScreen")]
        [HarmonyPrefix]
        static void DisplayLoadingScreenPrefix(string __0, bool __1 = true)
        {
            ExampleManager.onSceneLoad?.Invoke(__0);
            CoreHelper.CurrentSceneType = __0 == "Editor" ? SceneType.Editor : __0 == "Game" ? SceneType.Game : SceneType.Interface;
            CoreHelper.Log($"Set Scene\nType: {CoreHelper.CurrentSceneType}\nName: {__0}");
        }
    }
}

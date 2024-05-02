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
        [HarmonyPatch("LoadScene", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        static void LoadScenePostfix(string __0) => ExampleManager.onSceneLoad?.Invoke(__0);

    }
}

using System.Collections;

using UnityEngine;

using HarmonyLib;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundManager))]
    public class BackgroundManagerPatch : MonoBehaviour
    {
        public static AudioSource Audio => AudioManager.inst.CurrentAudioSource;

        public static BackgroundManager Instance { get => BackgroundManager.inst; set => BackgroundManager.inst = value; }

        [HarmonyPatch(nameof(BackgroundManager.CreateBackgroundObject))]
        [HarmonyPrefix]
        static bool CreateBackgroundObjectPrefix(ref GameObject __result)
        {
            __result = null;
            return false;
        }

        [HarmonyPatch(nameof(BackgroundManager.UpdateBackgroundObjects))]
        [HarmonyPrefix]
        static bool UpdateBackgroundObjectsPrefix() => false; // original tick method

        [HarmonyPatch(nameof(BackgroundManager.UpdateBackgrounds))]
        [HarmonyPrefix]
        static bool UpdateBackgrounds() => false;

        [HarmonyPatch(nameof(BackgroundManager.LoadBackground))]
        [HarmonyPrefix]
        static bool LoadBackgroundPrefix(ref IEnumerator __result)
        {
            __result = LoadBackgrounds();
            return false;
        }

        static IEnumerator LoadBackgrounds()
        {
            yield break;
        }
    }
}

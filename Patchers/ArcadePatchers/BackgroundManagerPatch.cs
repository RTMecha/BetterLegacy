using System.Collections;
using System.Linq;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;

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
        static bool UpdateBackgroundObjectsPrefix(BackgroundManager __instance)
        {
            return false;
        }

        [HarmonyPatch(nameof(BackgroundManager.UpdateBackgrounds))]
        [HarmonyPrefix]
        static bool UpdateBackgrounds()
        {
            RTLevel.Current?.UpdateBackgroundObjects();
            return false;
        }

        [HarmonyPatch(nameof(BackgroundManager.LoadBackground))]
        [HarmonyPrefix]
        static bool LoadBackgroundPrefix(ref IEnumerator __result)
        {
            __result = LoadBackgrounds();
            return false;
        }

        static IEnumerator LoadBackgrounds()
        {
            while (!GameData.Current || GameManager.inst.gameState != GameManager.State.Playing)
                yield return null;

            Instance.audio = AudioManager.inst.CurrentAudioSource;
            Instance.samples = new float[256];
            if (Instance.audio.clip != null)
                Instance.audio.clip.GetData(Instance.samples, 0);

            foreach (var backgroundObject in GameData.Current.backgroundObjects)
                RTLevel.Current?.CreateBackgroundObject(backgroundObject);

            yield break;
        }
    }
}

using System;

using UnityEngine;

using HarmonyLib;

using LSFunctions;

using TMPro;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(ObjectManager))]
    public class ObjectManagerPatch : MonoBehaviour
    {
        [HarmonyPatch(nameof(ObjectManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(ObjectManager __instance)
        {
            if (SceneHelper.CurrentSceneType != SceneType.Editor)
                ShapeManager.inst.SetupShapes();

            // Fixes Text being red.
            __instance.objectPrefabs[4].options[0].GetComponentInChildren<TextMeshPro>().color = new Color(0f, 0f, 0f, 0f);

            // Fixes Hexagons being solid.
            foreach (var option in __instance.objectPrefabs[5].options)
                option.GetComponentInChildren<Collider2D>().isTrigger = true;

            LSHelpers.DeleteChildren(__instance.objectParent.transform);

            CoreHelper.LogInit($"{nameof(ObjectManager)} - {SceneHelper.CurrentSceneType}\n");
        }

        [HarmonyPatch(nameof(ObjectManager.AddPrefabToLevel))]
        [HarmonyPrefix]
        static bool AddPrefabToLevelPrefix() => false;

        [HarmonyPatch(nameof(ObjectManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (!CoreHelper.Paused && !CoreHelper.Parsing && !CoreHelper.Loading)
                RTLevel.Current?.Tick();

            return false;
        }

        [HarmonyPatch(nameof(ObjectManager.updateObjects), new Type[] { })]
        [HarmonyPrefix]
        static bool updateObjectsPrefix()
        {
            RTLevel.Reinit();
            return false;
        }
    }
}

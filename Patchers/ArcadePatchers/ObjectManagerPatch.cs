using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using HarmonyLib;
using System;
using TMPro;
using UnityEngine;

namespace BetterLegacy.Patchers
{
    public delegate void LevelTickEventHandler();

    [HarmonyPatch(typeof(ObjectManager))]
    public class ObjectManagerPatch : MonoBehaviour
    {
        public static bool debugOpacity = false;

        [HarmonyPatch(nameof(ObjectManager.Awake))]
        [HarmonyPostfix]
        static void AwakePostfix(ObjectManager __instance)
        {
            ShapeManager.inst.SetupShapes();

            // This is here for debug purposes.
            if (debugOpacity)
                for (int i = 0; i < __instance.objectPrefabs.Count; i++)
                {
                    foreach (var option in __instance.objectPrefabs[i].options)
                        if (option.transform.childCount > 0 && option.transform.GetChild(0).gameObject.TryGetComponent(out Renderer renderer))
                            renderer.material.color = new Color(1f, 1f, 1f, 1f);
                }

            // Fixes Text being red.
            __instance.objectPrefabs[4].options[0].GetComponentInChildren<TextMeshPro>().color = new Color(0f, 0f, 0f, 0f);

            // Fixes Hexagons being solid.
            foreach (var option in __instance.objectPrefabs[5].options)
            {
                option.GetComponentInChildren<Collider2D>().isTrigger = true;
            }
        }

        [HarmonyPatch(nameof(ObjectManager.AddPrefabToLevel))]
        [HarmonyPrefix]
        static bool AddPrefabToLevelPrefix(DataManager.GameData.PrefabObject __0)
        {
            Updater.AddPrefabToLevel((PrefabObject)__0);

            return false;
        }

        public static event LevelTickEventHandler LevelTick;

        [HarmonyPatch(nameof(ObjectManager.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (!CoreHelper.Paused)
                LevelTick?.Invoke();

            return false;
        }

        [HarmonyPatch(nameof(ObjectManager.updateObjects), new Type[] { })]
        [HarmonyPrefix]
        static bool updateObjectsPrefix()
        {
            Updater.UpdateObjects();
            return false;
        }
    }
}

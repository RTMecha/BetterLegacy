using System;

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundEditor))]
    public class BackgroundEditorPatch : MonoBehaviour
    {
        public static BackgroundEditor Instance { get => BackgroundEditor.inst; set => BackgroundEditor.inst = value; }

        [HarmonyPatch(nameof(BackgroundEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(BackgroundEditor __instance)
        {
            if (Instance == null)
                BackgroundEditor.inst = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix(int __0)
        {
            CoreHelper.LogError($"Cannot run {nameof(BackgroundEditor.OpenDialog)}, please use {nameof(RTBackgroundEditor.OpenDialog)} instead!");
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.CreateNewBackground))]
        [HarmonyPrefix]
        static bool CreateNewBackgroundPrefix()
        {
            CoreHelper.LogError($"Cannot run {nameof(BackgroundEditor.CreateNewBackground)}, please use {nameof(RTBackgroundEditor.CreateNewBackground)} instead!");
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateBackgroundList))]
        [HarmonyPrefix]
        static bool UpdateBackgroundListPrefix()
        {
            CoreHelper.LogError($"Cannot run {nameof(BackgroundEditor.UpdateBackgroundList)}, please use {nameof(RTBackgroundEditor.UpdateBackgroundList)} instead!");
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateBackground))]
        [HarmonyPrefix]
        static bool UpdateBackgroundPrefix(int __0)
        {
            CoreHelper.LogError($"Cannot run {nameof(BackgroundEditor.UpdateBackground)}, please use {nameof(Updater.UpdateBackgroundObject)} instead!");
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetCurrentBackground))]
        [HarmonyPrefix]
        static bool SetCurrentBackgroundPrefix(int __0)
        {
            CoreHelper.LogError($"Cannot run {nameof(BackgroundEditor.SetCurrentBackground)}, please use {nameof(RTBackgroundEditor.SetCurrentBackground)} instead!");
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.CopyBackground))]
        [HarmonyPrefix]
        static bool CopyBackgroundPrefix() => false;

        [HarmonyPatch(nameof(BackgroundEditor.DeleteBackground))]
        [HarmonyPrefix]
        static bool DeleteBackgroundPrefix(ref string __result, int __0)
        {
            __result = string.Empty;
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.PasteBackground))]
        [HarmonyPrefix]
        static bool PasteBackgroundPrefix(ref string __result)
        {
            __result = string.Empty;
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateColorSelection))]
        [HarmonyPrefix]
        static bool UpdateColorSelectionPrefix() => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetName))]
        [HarmonyPrefix]
        static bool SetNamePrefix(string __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetActive))]
        [HarmonyPrefix]
        static bool SetActivePrefix(bool __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.AddToPosX))]
        [HarmonyPrefix]
        static bool AddToPosXPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetPosX), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetPosXPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetPosX), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetPosXPrefix(string __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.AddToPosY))]
        [HarmonyPrefix]
        static bool AddToPosYPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetPosY), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetPosYPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetPosY), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetPosYPrefix(string __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.AddToScaleX))]
        [HarmonyPrefix]
        static bool AddToScaleXPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleX), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetScaleXPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleX), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetScaleXPrefix(string __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.AddToScaleY))]
        [HarmonyPrefix]
        static bool AddToScaleYPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleY), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetScaleYPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleY), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetScaleYPrefix(string __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.AddToLayer))]
        [HarmonyPrefix]
        static bool AddToLayerPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetLayer))]
        [HarmonyPrefix]
        static bool SetLayerPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetRot), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetRotPrefix(string __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetRot), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetRotPrefix(float __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetColor))]
        [HarmonyPrefix]
        static bool SetColorPrefix(int __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetDrawFade))]
        [HarmonyPrefix]
        static bool SetDrawFadePrefix(bool __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveFalse))]
        [HarmonyPrefix]
        static bool SetReactiveFalsePrefix(bool __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveRangeLow))]
        [HarmonyPrefix]
        static bool SetReactiveRangeLowPrefix(bool __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveRangeMid))]
        [HarmonyPrefix]
        static bool SetReactiveRangeMidPrefix(bool __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveRangeHigh))]
        [HarmonyPrefix]
        static bool SetReactiveRangeHighPrefix(bool __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveScale), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetReactiveScalePrefix(string __0) => false;

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveScale), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetReactiveScale(float __0) => false;
    }
}

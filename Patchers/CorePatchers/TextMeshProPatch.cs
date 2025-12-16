
using HarmonyLib;

using TMPro;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;

namespace BetterLegacy.Patchers.CorePatchers
{
    //[HarmonyPatch(typeof(TextMeshPro))]
    //public class TextMeshProPatch
    //{
    //    [HarmonyPatch(nameof(TextMeshPro.Awake))]
    //    [HarmonyPrefix]
    //    static bool AwakePrefix(TextMeshPro __instance)
    //    {
    //        return true;
    //    }
    //}

    [HarmonyPatch(typeof(TextMeshProUGUI))]
    public class TextMeshProUGUIPatch
    {
        [HarmonyPatch(nameof(TextMeshProUGUI.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(TextMeshProUGUI __instance)
        {
            __instance.m_canvas = __instance.canvas;
            __instance.m_isOrthographic = true;
            __instance.m_rectTransform = __instance.transform.AsRT();
            if (!__instance.m_rectTransform)
                __instance.m_rectTransform = __instance.gameObject.AddComponent<RectTransform>();

            __instance.m_canvasRenderer = __instance.gameObject.GetOrAddComponent<CanvasRenderer>();

            if (!__instance.m_mesh)
            {
                __instance.m_mesh = new Mesh();
                __instance.m_mesh.hideFlags = HideFlags.HideAndDontSave;
                __instance.m_textInfo = new TMP_TextInfo(__instance);
            }

            __instance.LoadDefaultSettings();
            __instance.LoadFontAsset();
            TMP_StyleSheet.LoadDefaultStyleSheet();

            if (__instance.m_TextParsingBuffer == null)
                __instance.m_TextParsingBuffer = new TMP_Text.UnicodeChar[__instance.m_max_characters];
            __instance.m_cached_TextElement = new TMP_Character();
            __instance.m_isFirstAllocation = true;

            if (__instance.m_fontAsset == null)
            {
                Debug.LogWarning("Please assign a Font Asset to this " + __instance.transform.name + " gameobject.", __instance);
                return false;
            }

            var componentsInChildren = __instance.GetComponentsInChildren<TMP_SubMeshUI>();
            if (componentsInChildren.Length != 0)
                for (int i = 0; i < componentsInChildren.Length; i++)
                    __instance.m_subTextObjects[i + 1] = componentsInChildren[i];

            __instance.m_isInputParsingRequired = true;
            __instance.m_havePropertiesChanged = true;
            __instance.m_isCalculateSizeRequired = true;
            __instance.m_isAwake = true;
            return false;
        }

        [HarmonyPatch(nameof(TextMeshProUGUI.OnEnable))]
        [HarmonyPrefix]
        static bool OnEnablePrefix(TextMeshProUGUI __instance)
        {
            if (!__instance.m_isAwake)
                return false;

            if (!__instance.m_isRegisteredForEvents)
                __instance.m_isRegisteredForEvents = true;

            //__instance.m_canvas = __instance.GetCanvas();
            __instance.SetActiveSubMeshes(true);
            GraphicRegistry.RegisterGraphicForCanvas(__instance.m_canvas, __instance);
            TMP_UpdateManager.RegisterTextObjectForUpdate(__instance);
            __instance.ComputeMarginSize();

            __instance.m_verticesAlreadyDirty = false;
            __instance.m_layoutAlreadyDirty = false;
            __instance.m_ShouldRecalculateStencil = true;
            __instance.m_isInputParsingRequired = true;

            __instance.SetAllDirty();
            __instance.RecalculateClipping();
            return false;
        }
    }

    [HarmonyPatch(typeof(TMP_UpdateManager))]
    public class TMP_UpdateManagerPatch
    {
        [HarmonyPatch(nameof(TMP_UpdateManager.InternalRegisterTextObjectForUpdate))]
        [HarmonyPrefix]
        static bool InternalRegisterTextObjectForUpdatePrefix(TMP_UpdateManager __instance, TMP_Text __0)
        {
            int instanceID = __0.GetInstanceID();
            if (__instance.m_InternalUpdateLookup.TryAdd(instanceID, instanceID))
                __instance.m_InternalUpdateQueue.Add(__0);
            return false;
        }

        [HarmonyPatch(nameof(TMP_UpdateManager.InternalRegisterTextElementForLayoutRebuild))]
        [HarmonyPrefix]
        static bool InternalRegisterTextElementForLayoutRebuild(TMP_UpdateManager __instance, TMP_Text __0)
        {
            int instanceID = __0.GetInstanceID();
            if (__instance.m_LayoutQueueLookup.TryAdd(instanceID, instanceID))
                __instance.m_LayoutRebuildQueue.Add(__0);
            return false;
        }

        [HarmonyPatch(nameof(TMP_UpdateManager.InternalRegisterTextElementForGraphicRebuild))]
        [HarmonyPrefix]
        static bool InternalRegisterTextElementForGraphicRebuild(TMP_UpdateManager __instance, TMP_Text __0)
        {
            int instanceID = __0.GetInstanceID();
            if (__instance.m_GraphicQueueLookup.TryAdd(instanceID, instanceID))
                __instance.m_GraphicRebuildQueue.Add(__0);
            return false;
        }
    }
}

using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(MarkerEditor))]
    public class MarkerEditorPatch : MonoBehaviour
    {
        static MarkerEditor Instance { get => MarkerEditor.inst; set => MarkerEditor.inst = value; }

        static string className = "[<color=#FFAF38>MarkerEditor</color>] \n";

        [HarmonyPatch(nameof(MarkerEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(MarkerEditor __instance)
        {
            if (!Instance)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(className);

            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix() => false;

        [HarmonyPatch(nameof(MarkerEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix(int __0)
        {
            if (__0 < RTMarkerEditor.inst.timelineMarkers.Count)
                RTMarkerEditor.inst.OpenDialog(RTMarkerEditor.inst.timelineMarkers[__0]);
            else
            {
                RTMarkerEditor.inst.CreateMarker(__0);
                RTMarkerEditor.inst.OpenDialog(RTMarkerEditor.inst.timelineMarkers[__0]);
            }
            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.RenderMarker))]
        [HarmonyPrefix]
        static bool RenderMarkersPrefix(int __0)
        {
            if (__0 < RTMarkerEditor.inst.timelineMarkers.Count)
                RTMarkerEditor.inst.timelineMarkers[__0].Render();
            else
                RTMarkerEditor.inst.CreateMarker(__0);
            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.UpdateMarkerList))]
        [HarmonyPrefix]
        static bool UpdateMarkerListPrefix()
        {
            RTMarkerEditor.inst.UpdateMarkerList();
            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.CreateNewMarker), new Type[] { })]
        [HarmonyPrefix]
        static bool CreateNewMarkerPrefix()
        {
            Instance.CreateNewMarker(SettingEditor.inst.SnapActive ? RTEditor.SnapToBPM(EditorManager.inst.CurrentAudioPos) : EditorManager.inst.CurrentAudioPos);
            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.CreateNewMarker), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool CreateNewMarkerPrefix(float __0)
        {
            RTMarkerEditor.inst.CreateNewMarker(__0);
            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.UpdateColorSelection))]
        [HarmonyPrefix]
        static bool UpdateColorSelectionPrefix()
        {
            RTMarkerEditor.inst.UpdateColorSelection();
            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.DeleteMarker))]
        [HarmonyPrefix]
        static bool DeleteMarkerPrefix(int __0)
        {
            RTMarkerEditor.inst.DeleteMarker(__0);
            return false;
        }

        [HarmonyPatch(nameof(MarkerEditor.SetCurrentMarker))]
        [HarmonyPrefix]
        static bool SetCurrentMarkerPrefix(int __0, bool __1)
        {
            RTMarkerEditor.inst.SetCurrentMarker(RTMarkerEditor.inst.timelineMarkers[__0], __1);
            return false;
        }
    }
}

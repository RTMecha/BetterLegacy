using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(MetadataEditor))]
    public class MetadataEditorPatch : MonoBehaviour
    {
        static MetadataEditor Instance { get => MetadataEditor.inst; set => MetadataEditor.inst = value; }

        [HarmonyPatch(nameof(MetadataEditor.Awake))]
        [HarmonyPrefix]
        static bool Awake(MetadataEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            return false;
        }

        [HarmonyPatch(nameof(MetadataEditor.Render))]
        [HarmonyPrefix]
        static bool Render()
        {
            RTMetaDataEditor.inst.RenderEditor();
            return false;
        }

        // Moved code to BetterLegacy.Editor.Managers.RTMetaDataEditor
    }
}

using UnityEngine;

using HarmonyLib;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Managers;

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

        [HarmonyPatch(nameof(MetadataEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            RTMetaDataEditor.inst.OpenDialog();
            return false;
        }
        
        [HarmonyPatch(nameof(MetadataEditor.Render))]
        [HarmonyPrefix]
        static bool RenderPrefix()
        {
            RTMetaDataEditor.inst.RenderDialog();
            return false;
        }

        // Moved code to BetterLegacy.Editor.Managers.RTMetaDataEditor
    }
}

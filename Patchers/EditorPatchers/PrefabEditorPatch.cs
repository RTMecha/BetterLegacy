using System.Collections;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(PrefabEditor))]
    public class PrefabEditorPatch : MonoBehaviour
    {
        static PrefabEditor Instance { get => PrefabEditor.inst; set => PrefabEditor.inst = value; }

        [HarmonyPatch(nameof(PrefabEditor.Awake))]
        [HarmonyPrefix]
        static bool AwakePrefix(PrefabEditor __instance)
        {
            if (Instance == null)
                Instance = __instance;
            else if (Instance != __instance)
            {
                Destroy(__instance.gameObject);
                return false;
            }

            CoreHelper.LogInit(__instance.className);

            // Prefab Panel Prefab
            {
                var gameObject = PrefabEditor.inst.AddPrefab.Duplicate(__instance.transform, PrefabEditor.inst.AddPrefab.name);

                var type = gameObject.transform.Find("category");
                type.GetComponent<LayoutElement>().minWidth = 32f;

                var b = Creator.NewUIObject("type", type);
                b.transform.AsRT().anchoredPosition = Vector2.zero;
                b.transform.AsRT().sizeDelta = new Vector2(28f, 28f);

                var bImage = b.AddComponent<Image>();
                bImage.color = new Color(0f, 0f, 0f, 0.45f);

                var icon = Creator.NewUIObject("type", b.transform);
                icon.transform.AsRT().anchoredPosition = Vector2.zero;
                icon.transform.AsRT().sizeDelta = new Vector2(28f, 28f);

                icon.AddComponent<Image>();

                var storage = gameObject.AddComponent<PrefabPanelStorage>();

                var tf = gameObject.transform;
                storage.label = tf.Find("name").GetComponent<Text>();
                storage.typeNameText = tf.Find("type-name").GetComponent<Text>();
                storage.typeImage = tf.Find("category").GetComponent<Image>();
                storage.typeImageShade = tf.Find("category/type").GetComponent<Image>();
                storage.typeIconImage = tf.Find("category/type/type").GetComponent<Image>();
                storage.button = gameObject.GetComponent<Button>();
                storage.deleteButton = tf.Find("delete").GetComponent<Button>();

                PrefabEditor.inst.AddPrefab = gameObject;
            }

            // Add Prefab Prefab
            {
                var gameObject = PrefabEditor.inst.CreatePrefab.Duplicate(__instance.transform, PrefabEditor.inst.CreatePrefab.name);
                gameObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
                var storage = gameObject.AddComponent<FunctionButtonStorage>();
                storage.button = gameObject.GetComponent<Button>();
                storage.label = gameObject.transform.Find("Text").GetComponent<Text>();
                PrefabEditor.inst.CreatePrefab = gameObject;
            }

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix() => false;

        [HarmonyPatch(nameof(PrefabEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            var count = EditorTimeline.inst.SelectedObjectCount;
            if (count <= 0)
                return false;

            var creating = Instance.dialog && Instance.dialog.gameObject.activeSelf;
            var selected = count == 1 && EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.isPrefabObject;
            var offset = creating ? EditorTimeline.inst.SelectedObjects.Min(x => x.Time) - Instance.NewPrefabOffset : EditorTimeline.inst.CurrentSelection.Time;

            if (creating || selected)
            {
                if (!Instance.OffsetLine.activeSelf)
                {
                    Instance.OffsetLine.transform.SetAsLastSibling();
                    Instance.OffsetLine.SetActive(true);
                }

                Instance.OffsetLine.transform.AsRT().anchoredPosition = new Vector2(Instance.posCalc(offset), 0f);
            }
            else if (Instance.OffsetLine.activeSelf)
                Instance.OffsetLine.SetActive(false);

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.CreateNewPrefab))]
        [HarmonyPrefix]
        static bool CreateNewPrefabPrefix()
        {
            RTPrefabEditor.inst.CreateNewPrefab();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.SavePrefab))]
        [HarmonyPrefix]
        static bool SavePrefabPrefix() => false;

        [HarmonyPatch(nameof(PrefabEditor.DeleteExternalPrefab))]
        [HarmonyPrefix]
        static bool DeleteExternalPrefabPrefix(int __0) => false;

        [HarmonyPatch(nameof(PrefabEditor.DeleteInternalPrefab))]
        [HarmonyPrefix]
        static bool DeleteInternalPrefabPrefix(int __0)
        {
            RTPrefabEditor.inst.DeleteInternalPrefab(__0);
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ExpandCurrentPrefab))]
        [HarmonyPrefix]
        static bool ExpandCurrentPrefabPrefix()
        {
            RTPrefabEditor.inst.ExpandCurrentPrefab();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.CollapseCurrentPrefab))]
        [HarmonyPrefix]
        static bool CollapseCurrentPrefabPrefix()
        {
            if (EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
            {
                RTEditor.inst.ShowWarningPopup("Are you sure you want to collapse this Prefab group and save the changes to the Internal Prefab?", () =>
                {
                    RTPrefabEditor.inst.CollapseCurrentPrefab();
                    RTEditor.inst.HideWarningPopup();
                }, RTEditor.inst.HideWarningPopup);

                return false;
            }

            RTPrefabEditor.inst.CollapseCurrentPrefab();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ReloadExternalPrefabsInPopup))]
        [HarmonyPrefix]
        static bool ReloadExternalPrefabsInPopupPrefix(bool __0)
        {
            if (Instance.externalPrefabDialog == null || Instance.externalSearch == null || Instance.externalContent == null)
            {
                Debug.LogErrorFormat("External Prefabs Error: \n{0}\n{1}\n{2}", Instance.externalPrefabDialog, Instance.externalSearch, Instance.externalContent);
            }
            Debug.Log("Loading External Prefabs Popup");
            RTEditor.inst.StartCoroutine(RTPrefabEditor.inst.IRenderExternalPrefabs());
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ReloadInternalPrefabsInPopup))]
        [HarmonyPrefix]
        static bool ReloadInternalPrefabsInPopupPrefix(bool __0)
        {
            if (Instance.internalPrefabDialog == null || Instance.internalSearch == null || Instance.internalContent == null)
            {
                Debug.LogErrorFormat("Internal Prefabs Error: \n{0}\n{1}\n{2}", Instance.internalPrefabDialog, Instance.internalSearch, Instance.internalContent);
            }
            Debug.Log("Loading Internal Prefabs Popup");
            RTEditor.inst.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs(__0));
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.LoadExternalPrefabs))]
        [HarmonyPrefix]
        static bool LoadExternalPrefabsPrefix(ref IEnumerator __result)
        {
            __result = CoroutineHelper.IEmpty();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.OpenPrefabDialog))]
        [HarmonyPrefix]
        static bool OpenPrefabDialogPrefix()
        {
            RTPrefabEditor.inst.OpenPrefabObjectDialog();
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix()
        {
            RTPrefabEditor.inst.OpenDialog();

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.OpenPopup))]
        [HarmonyPrefix]
        static bool OpenPopupPrefix()
        {
            RTPrefabEditor.inst.OpenPopup();

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.ImportPrefabIntoLevel))]
        [HarmonyPrefix]
        static bool ImportPrefabIntoLevelPrefix() => false;

        [HarmonyPatch(nameof(PrefabEditor.AddPrefabObjectToLevel))]
        [HarmonyPrefix]
        static bool AddPrefabObjectToLevelPrefix() => false;
    }
}

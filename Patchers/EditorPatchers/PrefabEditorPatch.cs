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
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using BasePrefab = DataManager.GameData.Prefab;

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

            // Prefab Type Icon
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
                storage.nameText = tf.Find("name").GetComponent<Text>();
                storage.typeNameText = tf.Find("type-name").GetComponent<Text>();
                storage.typeImage = tf.Find("category").GetComponent<Image>();
                storage.typeImageShade = tf.Find("category/type").GetComponent<Image>();
                storage.typeIconImage = tf.Find("category/type/type").GetComponent<Image>();
                storage.button = gameObject.GetComponent<Button>();
                storage.deleteButton = tf.Find("delete").GetComponent<Button>();

                PrefabEditor.inst.AddPrefab = gameObject;
            }

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.Start))]
        [HarmonyPrefix]
        static bool StartPrefix()
        {
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.Update))]
        [HarmonyPrefix]
        static bool UpdatePrefix()
        {
            if (Instance.dialog && Instance.dialog.gameObject.activeSelf)
            {
                float num;
                if (ObjectEditor.inst.SelectedObjects.Count <= 0)
                    num = 0f;
                else
                    num = ObjectEditor.inst.SelectedObjects.Min(x => x.Time);

                if (!Instance.OffsetLine.activeSelf && ObjectEditor.inst.SelectedObjects.Count > 0)
                {
                    Instance.OffsetLine.transform.SetAsLastSibling();
                    Instance.OffsetLine.SetActive(true);
                }
                ((RectTransform)Instance.OffsetLine.transform).anchoredPosition = new Vector2(Instance.posCalc(num - Instance.NewPrefabOffset), 0f);
            }
            if (((!Instance.dialog || !Instance.dialog.gameObject.activeSelf) || ObjectEditor.inst.SelectedBeatmapObjects.Count <= 0) && Instance.OffsetLine.activeSelf)
            {
                Instance.OffsetLine.SetActive(false);
            }
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
        static bool SavePrefabPrefix(BasePrefab __0)
        {
            RTPrefabEditor.inst.SavePrefab((Prefab)__0);
            return false;
        }

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
        [HarmonyPostfix]
        static void ReloadExternalPrefabsInPopupPostfix()
        {
            CoreHelper.Log($"Run patch: {nameof(ReloadExternalPrefabsInPopupPostfix)}");

            //Internal Config
            {
                var internalPrefab = PrefabEditor.inst.internalPrefabDialog;

                var internalPrefabGLG = internalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                internalPrefabGLG.spacing = EditorConfig.Instance.PrefabInternalSpacing.Value;
                internalPrefabGLG.cellSize = EditorConfig.Instance.PrefabInternalCellSize.Value;
                internalPrefabGLG.constraint = EditorConfig.Instance.PrefabInternalConstraintMode.Value;
                internalPrefabGLG.constraintCount = EditorConfig.Instance.PrefabInternalConstraint.Value;
                internalPrefabGLG.startAxis = EditorConfig.Instance.PrefabInternalStartAxis.Value;

                internalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabInternalPopupPos.Value;
                internalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabInternalPopupSize.Value;

                internalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabInternalHorizontalScroll.Value;
            }

            //External Config
            {
                var externalPrefab = PrefabEditor.inst.externalPrefabDialog;

                var externalPrefabGLG = externalPrefab.Find("mask/content").GetComponent<GridLayoutGroup>();

                externalPrefabGLG.spacing = EditorConfig.Instance.PrefabExternalSpacing.Value;
                externalPrefabGLG.cellSize = EditorConfig.Instance.PrefabExternalCellSize.Value;
                externalPrefabGLG.constraint = EditorConfig.Instance.PrefabExternalConstraintMode.Value;
                externalPrefabGLG.constraintCount = EditorConfig.Instance.PrefabExternalConstraint.Value;
                externalPrefabGLG.startAxis = EditorConfig.Instance.PrefabExternalStartAxis.Value;

                externalPrefab.AsRT().anchoredPosition = EditorConfig.Instance.PrefabExternalPopupPos.Value;
                externalPrefab.AsRT().sizeDelta = EditorConfig.Instance.PrefabExternalPopupSize.Value;

                externalPrefab.GetComponent<ScrollRect>().horizontal = EditorConfig.Instance.PrefabExternalHorizontalScroll.Value;
            }
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
            RTEditor.inst.StartCoroutine(RTPrefabEditor.inst.ExternalPrefabFiles(__0));
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
            RTEditor.inst.StartCoroutine(RTPrefabEditor.inst.InternalPrefabs(__0));
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.LoadExternalPrefabs))]
        [HarmonyPrefix]
        static bool LoadExternalPrefabsPrefix(PrefabEditor __instance, ref IEnumerator __result)
        {
            __result = RTEditor.inst.LoadPrefabs(__instance);
            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.OpenPrefabDialog))]
        [HarmonyPrefix]
        static bool OpenPrefabDialogPrefix()
        {
            EditorManager.inst.ClearDialogs();

            bool isPrefab = ObjectEditor.inst.CurrentSelection != null && ObjectEditor.inst.CurrentSelection.Data != null && ObjectEditor.inst.CurrentSelection.IsPrefabObject;
            if (!isPrefab)
            {
                Debug.LogError($"{Instance.className}Cannot select non-Prefab with this editor!");
                EditorManager.inst.ShowDialog("Object Editor", false);
                return false;
            }

            EditorManager.inst.ShowDialog("Prefab Selector");
            RTPrefabEditor.inst.RenderPrefabObjectDialog(ObjectEditor.inst.CurrentSelection.GetData<PrefabObject>());

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
        static bool ImportPrefabIntoLevelPrefix(PrefabEditor __instance, BasePrefab __0)
        {
            CoreHelper.Log($"Adding Prefab [{__0.Name}]");

            var tmpPrefab = Prefab.DeepCopy((Prefab)__0);
            int num = DataManager.inst.gameData.prefabs.FindAll(x => Regex.Replace(x.Name, "( +\\[\\d+])", string.Empty) == tmpPrefab.Name).Count();
            if (num > 0)
                tmpPrefab.Name = $"{tmpPrefab.Name}[{num}]";

            DataManager.inst.gameData.prefabs.Add(tmpPrefab);
            __instance.ReloadInternalPrefabsInPopup();

            return false;
        }

        [HarmonyPatch(nameof(PrefabEditor.AddPrefabObjectToLevel))]
        [HarmonyPrefix]
        static bool AddPrefabObjectToLevelPrefix(BasePrefab __0)
        {
            RTPrefabEditor.inst.AddPrefabObjectToLevel(__0);
            return false;
        }
    }
}

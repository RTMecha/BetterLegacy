using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;
using HarmonyLib;
using LSFunctions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundEditor))]
    public class BackgroundEditorPatch : MonoBehaviour
    {
        public static BackgroundEditor Instance { get => BackgroundEditor.inst; set => BackgroundEditor.inst = value; }

        public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null ? null : (BackgroundObject)DataManager.inst.gameData.backgroundObjects[BackgroundEditor.inst.currentObj];

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

            RTBackgroundEditor.Init(); // Init has to be here due to BackgroundEditor initializing after EditorManager.

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.OpenDialog))]
        [HarmonyPrefix]
        static bool OpenDialogPrefix(int __0)
        {
            RTBackgroundEditor.inst.OpenDialog(__0);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.CreateNewBackground))]
        [HarmonyPrefix]
        static bool CreateNewBackgroundPrefix()
        {
            var backgroundObject = new BackgroundObject
            {
                name = "Background",
                pos = Vector2.zero,
                scale = new Vector2(2f, 2f),
            };

            DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

            BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
            Instance.SetCurrentBackground(DataManager.inst.gameData.backgroundObjects.Count - 1);
            Instance.OpenDialog(DataManager.inst.gameData.backgroundObjects.Count - 1);

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateBackgroundList))]
        [HarmonyPrefix]
        static bool UpdateBackgroundListPrefix()
        {
            var parent = Instance.right.Find("backgrounds/viewport/content");
            LSHelpers.DeleteChildren(parent);
            int num = 0;
            foreach (var backgroundObject in DataManager.inst.gameData.backgroundObjects)
            {
                if (!CoreHelper.SearchString(Instance.sortedName, backgroundObject.name))
                {
                    num++;
                    continue;
                }

                int index = num;
                var gameObject = Instance.backgroundButtonPrefab.Duplicate(parent, $"BG {index}");
                gameObject.transform.localScale = Vector3.one;

                var name = gameObject.transform.Find("name").GetComponent<Text>();
                var text = gameObject.transform.Find("pos").GetComponent<Text>();
                var image = gameObject.transform.Find("color").GetComponent<Image>();

                name.text = backgroundObject.name;
                text.text = $"({backgroundObject.pos.x}, {backgroundObject.pos.y})";

                image.color = GameManager.inst.LiveTheme.GetBGColor(backgroundObject.color);

                var button = gameObject.GetComponent<Button>();
                button.onClick.AddListener(() => { Instance.SetCurrentBackground(index); });

                EditorThemeManager.ApplyGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(name, ThemeGroup.List_Button_2_Text);
                EditorThemeManager.ApplyGraphic(text, ThemeGroup.List_Button_2_Text);

                num++;
            }

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateBackground))]
        [HarmonyPrefix]
        static bool UpdateBackgroundPrefix(int __0)
        {
            var backgroundObject = (BackgroundObject)DataManager.inst.gameData.backgroundObjects[__0];

            if (backgroundObject.BaseObject)
                Destroy(backgroundObject.BaseObject);

            Updater.CreateBackgroundObject(backgroundObject);

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.CopyBackground))]
        [HarmonyPrefix]
        static bool CopyBackgroundPrefix()
        {
            CoreHelper.Log($"Copied Background Object");
            Instance.backgroundObjCopy = BackgroundObject.DeepCopy((BackgroundObject)DataManager.inst.gameData.backgroundObjects[Instance.currentObj]);
            Instance.hasCopiedObject = true;

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.DeleteBackground))]
        [HarmonyPrefix]
        static bool DeleteBackgroundPrefix(ref string __result, int __0)
        {
            if (DataManager.inst.gameData.backgroundObjects.Count <= 1)
            {
                EditorManager.inst.DisplayNotification("Unable to delete last background element! Consider moving it off screen or turning it into your first background element for the level.", 2f, EditorManager.NotificationType.Error, false);
                __result = null;
                return false;
            }

            string name = DataManager.inst.gameData.backgroundObjects[__0].name;
            DataManager.inst.gameData.backgroundObjects.RemoveAt(__0);

            if (DataManager.inst.gameData.backgroundObjects.Count > 0)
                Instance.SetCurrentBackground(Mathf.Clamp(Instance.currentObj - 1, 0, DataManager.inst.gameData.backgroundObjects.Count - 1));

            BackgroundManager.inst.UpdateBackgrounds();

            __result = name;
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.PasteBackground))]
        [HarmonyPrefix]
        static bool PasteBackgroundPrefix(ref string __result)
        {
            if (!Instance.hasCopiedObject || Instance.backgroundObjCopy == null)
            {
                EditorManager.inst.DisplayNotification("No copied background yet!", 2f, EditorManager.NotificationType.Error);
                __result = "";
                return false;
            }

            var backgroundObject = BackgroundObject.DeepCopy((BackgroundObject)Instance.backgroundObjCopy);
            int currentBackground = DataManager.inst.gameData.backgroundObjects.Count;
            DataManager.inst.gameData.backgroundObjects.Add(backgroundObject);

            BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
            Instance.SetCurrentBackground(currentBackground);
            __result = backgroundObject.name.ToString();

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetRot), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetRotPrefix(string __0)
        {
            if (float.TryParse(__0, out float rot))
            {
                DataManager.inst.gameData.backgroundObjects[Instance.currentObj].rot = rot;
                Instance.left.Find("rotation/slider").GetComponent<Slider>().value = rot;
                Instance.UpdateBackground(Instance.currentObj);
            }

            return false;
        }
    }
}

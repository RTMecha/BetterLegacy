using System;

using UnityEngine;
using UnityEngine.UI;

using HarmonyLib;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;

using BaseBackgroundObject = DataManager.GameData.BackgroundObject;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(BackgroundEditor))]
    public class BackgroundEditorPatch : MonoBehaviour
    {
        public static BackgroundEditor Instance { get => BackgroundEditor.inst; set => BackgroundEditor.inst = value; }

        public static BackgroundObject CurrentSelectedBG => BackgroundEditor.inst == null ? null : GameData.Current.backgroundObjects[BackgroundEditor.inst.currentObj];

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

            GameData.Current.backgroundObjects.Add(backgroundObject);

            BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
            Instance.SetCurrentBackground(GameData.Current.backgroundObjects.Count - 1);
            Instance.OpenDialog(GameData.Current.backgroundObjects.Count - 1);

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateBackgroundList))]
        [HarmonyPrefix]
        static bool UpdateBackgroundListPrefix()
        {
            var parent = Instance.right.Find("backgrounds/viewport/content");
            LSHelpers.DeleteChildren(parent);
            int num = 0;
            foreach (var backgroundObject in GameData.Current.backgroundObjects)
            {
                if (!RTString.SearchString(Instance.sortedName, backgroundObject.name))
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

                image.color = ThemeManager.inst.Current.GetBGColor(backgroundObject.color);

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
            var backgroundObject = GameData.Current.backgroundObjects[__0];

            if (backgroundObject.BaseObject)
                Destroy(backgroundObject.BaseObject);

            Updater.CreateBackgroundObject(backgroundObject);

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetCurrentBackground))]
        [HarmonyPrefix]
        static bool SetCurrentBackgroundPrefix(int __0)
        {
            int index = Mathf.Clamp(__0, 0, GameData.Current.backgroundObjects.Count - 1);
            if (!CoreHelper.IsUsingInputField)
            {
                DataManager.inst.UpdateSettingInt("EditorBG", index);
                Instance.currentObj = index;
                Debug.Log("Set bg to " + index);
                Instance.OpenDialog(index);
            }
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.CopyBackground))]
        [HarmonyPrefix]
        static bool CopyBackgroundPrefix()
        {
            CoreHelper.Log($"Copied Background Object");
            Instance.backgroundObjCopy = BackgroundObject.DeepCopy(GameData.Current.backgroundObjects[Instance.currentObj]);
            Instance.hasCopiedObject = true;

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.DeleteBackground))]
        [HarmonyPrefix]
        static bool DeleteBackgroundPrefix(ref string __result, int __0)
        {
            if (GameData.Current.backgroundObjects.Count <= 1)
            {
                EditorManager.inst.DisplayNotification("Unable to delete last background element! Consider moving it off screen or turning it into your first background element for the level.", 2f, EditorManager.NotificationType.Error, false);
                __result = null;
                return false;
            }

            var backgroundObject = GameData.Current.backgroundObjects[__0];
            string name = backgroundObject.name;
            Updater.DestroyBackgroundObject(backgroundObject);
            GameData.Current.backgroundObjects.RemoveAt(__0);

            if (GameData.Current.backgroundObjects.Count > 0)
                Instance.SetCurrentBackground(Mathf.Clamp(Instance.currentObj - 1, 0, GameData.Current.backgroundObjects.Count - 1));

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
            int currentBackground = GameData.Current.backgroundObjects.Count;
            GameData.Current.backgroundObjects.Add(backgroundObject);

            BackgroundManager.inst.CreateBackgroundObject(backgroundObject);
            Instance.SetCurrentBackground(currentBackground);
            __result = backgroundObject.name.ToString();

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.UpdateColorSelection))]
        [HarmonyPrefix]
        static bool UpdateColorSelectionPrefix()
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            for (int i = 0; i < ThemeManager.inst.Current.backgroundColors.Count; i++)
                Instance.left.Find("color").GetChild(i).Find("Image").gameObject.SetActive(backgroundObject.color == i);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetName))]
        [HarmonyPrefix]
        static bool SetNamePrefix(string __0)
        {
            GameData.Current.backgroundObjects[Instance.currentObj].name = __0;
            return false;
        }
        
        [HarmonyPatch(nameof(BackgroundEditor.SetActive))]
        [HarmonyPrefix]
        static bool SetActivePrefix(bool __0)
        {
            GameData.Current.backgroundObjects[Instance.currentObj].active = __0;
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.AddToPosX))]
        [HarmonyPrefix]
        static bool AddToPosXPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.pos.x += __0;
            Instance.left.Find("position/x").GetComponent<InputField>().text = backgroundObject.pos.x.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }
        
        [HarmonyPatch(nameof(BackgroundEditor.SetPosX), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetPosXPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.pos.x = __0;
            Instance.left.Find("position/x").GetComponent<InputField>().text = backgroundObject.pos.x.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetPosX), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetPosXPrefix(string __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.pos.x = Parser.TryParse(__0, backgroundObject.pos.x);
            Instance.left.Find("position/x").GetComponent<InputField>().text = backgroundObject.pos.x.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }
        
        [HarmonyPatch(nameof(BackgroundEditor.AddToPosY))]
        [HarmonyPrefix]
        static bool AddToPosYPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.pos.y += __0;
            Instance.left.Find("position/y").GetComponent<InputField>().text = backgroundObject.pos.y.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }
        
        [HarmonyPatch(nameof(BackgroundEditor.SetPosY), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetPosYPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.pos.y = __0;
            Instance.left.Find("position/y").GetComponent<InputField>().text = backgroundObject.pos.y.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetPosY), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetPosYPrefix(string __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.pos.y = Parser.TryParse(__0, backgroundObject.pos.x);
            Instance.left.Find("position/y").GetComponent<InputField>().text = backgroundObject.pos.y.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.AddToScaleX))]
        [HarmonyPrefix]
        static bool AddToScaleXPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.scale.x += __0;
            Instance.left.Find("scale/x").GetComponent<InputField>().text = backgroundObject.scale.x.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleX), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetScaleXPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.scale.x = __0;
            Instance.left.Find("scale/x").GetComponent<InputField>().text = backgroundObject.scale.x.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleX), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetScaleXPrefix(string __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.scale.x = Parser.TryParse(__0, backgroundObject.scale.x);
            Instance.left.Find("scale/x").GetComponent<InputField>().text = backgroundObject.scale.x.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }
        
        [HarmonyPatch(nameof(BackgroundEditor.AddToScaleY))]
        [HarmonyPrefix]
        static bool AddToScaleYPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.scale.y += __0;
            Instance.left.Find("scale/y").GetComponent<InputField>().text = backgroundObject.scale.y.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleY), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetScaleYPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.scale.y = __0;
            Instance.left.Find("scale/y").GetComponent<InputField>().text = backgroundObject.scale.y.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetScaleY), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetScaleYPrefix(string __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.scale.y = Parser.TryParse(__0, backgroundObject.scale.y);
            Instance.left.Find("scale/y").GetComponent<InputField>().text = backgroundObject.scale.y.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.AddToLayer))]
        [HarmonyPrefix]
        static bool AddToLayerPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.layer += (int)__0;
            Instance.left.Find("depth/layer").GetComponent<Slider>().value = backgroundObject.layer;
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetLayer))]
        [HarmonyPrefix]
        static bool SetLayerPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.layer = (int)__0;
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetRot), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetRotPrefix(string __0)
        {
            if (float.TryParse(__0, out float rot))
            {
                GameData.Current.backgroundObjects[Instance.currentObj].rot = rot;
                Instance.left.Find("rotation/slider").GetComponent<Slider>().value = rot;
                Instance.UpdateBackground(Instance.currentObj);
            }

            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetRot), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetRotPrefix(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.rot = __0;
            Instance.left.Find("rotation/x").GetComponent<InputField>().text = backgroundObject.rot.ToString();
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetColor))]
        [HarmonyPrefix]
        static bool SetColorPrefix(int __0)
        {
            GameData.Current.backgroundObjects[Instance.currentObj].color = __0;
            Instance.UpdateBackground(Instance.currentObj);
            Instance.UpdateColorSelection();
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetDrawFade))]
        [HarmonyPrefix]
        static bool SetDrawFadePrefix(bool __0)
        {
            GameData.Current.backgroundObjects[Instance.currentObj].drawFade = __0;
            Instance.UpdateBackground(Instance.currentObj);
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveFalse))]
        [HarmonyPrefix]
        static bool SetReactiveFalsePrefix(bool __0)
        {
            if (__0)
                GameData.Current.backgroundObjects[Instance.currentObj].reactive = false;
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveRangeLow))]
        [HarmonyPrefix]
        static bool SetReactiveRangeLowPrefix(bool __0)
        {
            if (__0)
            {
                var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
                backgroundObject.reactive = true;
                backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.LOW;
            }
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveRangeMid))]
        [HarmonyPrefix]
        static bool SetReactiveRangeMidPrefix(bool __0)
        {
            if (__0)
            {
                var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
                backgroundObject.reactive = true;
                backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.MID;
            }
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveRangeHigh))]
        [HarmonyPrefix]
        static bool SetReactiveRangeHighPrefix(bool __0)
        {
            if (__0)
            {
                var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
                backgroundObject.reactive = true;
                backgroundObject.reactiveType = BaseBackgroundObject.ReactiveType.HIGH;
            }
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveScale), new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool SetReactiveScalePrefix(string __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.reactiveScale = Mathf.Clamp(Parser.TryParse(__0, backgroundObject.reactiveScale), 0f, 1f);
            Instance.left.Find("reactive/slider").GetComponent<Slider>().value = backgroundObject.reactiveScale;
            return false;
        }

        [HarmonyPatch(nameof(BackgroundEditor.SetReactiveScale), new Type[] { typeof(float) })]
        [HarmonyPrefix]
        static bool SetReactiveScale(float __0)
        {
            var backgroundObject = GameData.Current.backgroundObjects[Instance.currentObj];
            backgroundObject.reactiveScale = __0;
            Instance.left.Find("reactive/x").GetComponent<InputField>().text = backgroundObject.reactiveScale.ToString("f2");
            return false;
        }
    }
}

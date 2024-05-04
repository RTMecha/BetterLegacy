using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Menus;
using HarmonyLib;
using InControl;
using LSFunctions;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BranchType = InterfaceController.InterfaceBranch.Type;
using ButtonType = InterfaceController.ButtonSetting.Type;
using Element = InterfaceController.InterfaceElement;
using ElementType = InterfaceController.InterfaceElement.Type;

namespace BetterLegacy.Patchers
{
    [HarmonyPatch(typeof(InterfaceController))]
    public class InterfaceControllerPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPrefix]
        static bool StartPrefix(InterfaceController __instance)
        {
            if (EditorManager.inst)
                __instance.gameObject.SetActive(false);

            MenuManager.inst.ic = __instance;

            try
            {
                Destroy(GameObject.Find("EventSystem").GetComponent<InControlInputModule>());
                Destroy(GameObject.Find("EventSystem").GetComponent<BaseInput>());
                GameObject.Find("EventSystem").AddComponent<StandaloneInputModule>();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            foreach (var quickElement in QuickElementManager.AllQuickElements)
            {
                if (!__instance.quickElements.ContainsKey(quickElement.Key))
                    __instance.quickElements.Add(quickElement.Key, quickElement.Value);
            }

            DataManager.inst.UpdateSettingString("colon", ":");

            if (!GameManager.inst)
                LSHelpers.ShowCursor();

            if (!DataManager.inst.HasKey("MasterVolume"))
                __instance.ResetAudioSettings();

            if (!DataManager.inst.HasKey("Resolution_i"))
                __instance.ResetVideoSettings();

            InputDataManager.inst.BindMenuKeys();
            __instance.MainPanel = __instance.transform.Find("Panel");

            CoreHelper.Log($"Load On Start: {__instance.loadOnStart}");
            if (__instance.loadOnStart)
            {
                CoreHelper.Log("Loading null...");
                LoadInterface(null);
            }

            if (!GameManager.inst)
                MenuManager.inst.PlayMusic();

            return false;
        }

        [HarmonyPatch("LoadInterface", new Type[] { typeof(string) })]
        [HarmonyPrefix]
        static bool LoadInterfacePrefix(string _filename)
        {
            LoadInterface(_filename);
            return false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        static bool UpdatePrefix(InterfaceController __instance)
        {
            if (InputDataManager.inst.menuActions.Cancel.WasPressed && __instance.screenDone && __instance.currentBranch != "main_menu" && __instance.interfaceBranches[__instance.CurrentBranchIndex].type == BranchType.Menu)
            {
                if (__instance.branchChain.Count > 1)
                {
                    if (!string.IsNullOrEmpty(__instance.interfaceBranches[__instance.CurrentBranchIndex].BackBranch))
                    {
                        __instance.SwitchBranch(__instance.interfaceBranches[__instance.CurrentBranchIndex].BackBranch);
                    }
                    else
                    {
                        __instance.SwitchBranch(__instance.branchChain[__instance.branchChain.Count - 2]);
                    }
                }
                else
                {
                    AudioManager.inst.PlaySound("Block");
                }
            }
            else if (InputDataManager.inst.menuActions.Cancel.WasPressed && __instance.screenDone && __instance.interfaceBranches[__instance.CurrentBranchIndex].type == BranchType.MainMenu && !string.IsNullOrEmpty(__instance.interfaceSettings.returnBranch))
            {
                __instance.SwitchBranch(__instance.interfaceSettings.returnBranch);
            }

            int num = 0;
            foreach (var gameObject in __instance.buttons)
            {
                if (__instance.buttonSettings.Count > num && __instance.buttonSettings[num] != null && gameObject == EventSystem.current.currentSelectedGameObject)
                    MenuManager.inst.UpdateSetting(__instance.buttonSettings[num], InputDataManager.inst.menuActions.Left.WasPressed, InputDataManager.inst.menuActions.Right.WasPressed);

                var selected = gameObject == EventSystem.current.currentSelectedGameObject;
                // Handle Selected
                {
                    gameObject.transform.Find("bg").GetComponent<Image>().color = selected ? __instance.interfaceSettings.borderHighlightColor : __instance.interfaceSettings.borderColor;

                    var textMeshProUGUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
                    var textMeshPro = gameObject.transform.Find("text").GetComponent<TextMeshPro>();
                    var textColor = selected ? __instance.interfaceSettings.textHighlightColor : __instance.interfaceSettings.textColor;

                    if (textMeshProUGUI)
                        textMeshProUGUI.color = textColor;
                    if (textMeshPro)
                        textMeshPro.color = textColor;

                    if (gameObject.transform.Find("float"))
                    {
                        var otherTextMeshProUGUI = gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>();
                        var otherTextMeshPro = gameObject.transform.Find("float").GetComponent<TextMeshPro>();

                        if (otherTextMeshProUGUI)
                            otherTextMeshProUGUI.color = textColor;
                        if (otherTextMeshPro)
                            otherTextMeshPro.color = textColor;
                    }

                    if (gameObject.transform.Find("bool"))
                    {
                        var otherTextMeshProUGUI = gameObject.transform.Find("bool").GetComponent<TextMeshProUGUI>();
                        var otherTextMeshPro = gameObject.transform.Find("bool").GetComponent<TextMeshPro>();

                        if (otherTextMeshProUGUI)
                            otherTextMeshProUGUI.color = textColor;
                        if (otherTextMeshPro)
                            otherTextMeshPro.color = textColor;
                    }

                    if (gameObject.transform.Find("vector2"))
                    {
                        var otherTextMeshProUGUI = gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>();
                        var otherTextMeshPro = gameObject.transform.Find("vector2").GetComponent<TextMeshPro>();

                        if (otherTextMeshProUGUI)
                            otherTextMeshProUGUI.color = textColor;
                        if (otherTextMeshPro)
                            otherTextMeshPro.color = textColor;
                    }

                    if (gameObject.transform.Find("string"))
                    {
                        var otherTextMeshProUGUI = gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>();
                        var otherTextMeshPro = gameObject.transform.Find("vector2").GetComponent<TextMeshPro>();

                        if (otherTextMeshProUGUI)
                            otherTextMeshProUGUI.color = textColor;
                        if (otherTextMeshPro)
                            otherTextMeshPro.color = textColor;
                    }
                }

                if (selected)
                    __instance.currHoveredButton = gameObject;

                if (!__instance.screenGlitch)
                {
                    switch (__instance.buttonSettings[num].type)
                    {
                        case ButtonType.Int:
                            {
                                int integer = DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting);
                                integer = Mathf.Clamp(integer, 0, 9);

                                var textMeshProUGUI = gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>();
                                var textMeshPro = gameObject.transform.Find("float").GetComponent<TextMeshPro>();

                                if (textMeshProUGUI)
                                    textMeshProUGUI.text = "< [         ] >".Insert(integer + 3, "■");

                                if (textMeshPro)
                                    textMeshPro.text = "< [         ] >".Insert(integer + 3, "■");

                                break;
                            }
                        case ButtonType.Bool:
                            {
                                var textMeshProUGUI = gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>();
                                var textMeshPro = gameObject.transform.Find("float").GetComponent<TextMeshPro>();

                                bool settingBool = DataManager.inst.GetSettingBool(__instance.buttonSettings[num].setting);
                                if (textMeshProUGUI)
                                    textMeshProUGUI.text = "< [ " + (settingBool ? "true" : "false") + " ] >";
                                if (textMeshPro)
                                    textMeshPro.text = "< [ " + (settingBool ? "true" : "false") + " ] >";

                                break;
                            }
                        case ButtonType.Vector2:
                            {
                                var textMeshProUGUI = gameObject.transform.Find("vector2").GetComponent<TextMeshProUGUI>();
                                var textMeshPro = gameObject.transform.Find("vector2").GetComponent<TextMeshPro>();

                                Vector2 settingVector2D = DataManager.inst.GetSettingVector2D(__instance.buttonSettings[num].setting);
                                if (textMeshProUGUI)
                                    textMeshProUGUI.text = $"< [ {settingVector2D.x}, {settingVector2D.y} ] >";
                                if (textMeshPro)
                                    textMeshPro.text = $"< [ {settingVector2D.x}, {settingVector2D.y} ] >";

                                break;
                            }
                        case ButtonType.String:
                            {
                                var textMeshProUGUI = gameObject.transform.Find("float").GetComponent<TextMeshProUGUI>();
                                var textMeshPro = gameObject.transform.Find("float").GetComponent<TextMeshPro>();

                                string str = __instance.buttonSettings[num].setting == "Language" ?
                                    DataManager.inst.GetLanguage(DataManager.inst.GetSettingInt(__instance.buttonSettings[num].setting + "_i", 0)) :
                                    DataManager.inst.GetSettingEnumName(__instance.buttonSettings[num].setting, 0);

                                if (textMeshProUGUI)
                                    textMeshProUGUI.text = "< [ " + str + " ] >";
                                if (textMeshPro)
                                    textMeshPro.text = "< [ " + str + " ] >";

                                break;
                            }
                    }
                }
                num++;
            }

            __instance.SpeedUp = InputDataManager.inst.menuActions.Submit.IsPressed;
            if (EventSystem.current.currentSelectedGameObject == null && __instance.buttonsActive)
                EventSystem.current.SetSelectedGameObject(__instance.lastSelectedObj);

            if (__instance.lastSelectedObj != EventSystem.current.currentSelectedGameObject && __instance.screenDone)
                AudioManager.inst.PlaySound("UpDown");

            __instance.lastSelectedObj = EventSystem.current.currentSelectedGameObject;
            return false;
        }

        public static void LoadInterface(string path, bool switchBranch = true)
        {
            string text = string.IsNullOrEmpty(path) ? SaveManager.inst.CurrentStoryLevel.BeatmapJson.text : FileManager.inst.LoadJSONFile(path);

            MenuManager.currentInterface = path;
            CoreHelper.Log($"Loading interface [{path}]");
            MenuManager.inst.ParseLilScript(text, switchBranch);
        }

        [HarmonyPatch("handleEvent")]
        [HarmonyPrefix]
        static bool handleEventPrefix(ref IEnumerator __result, ref string __0, string __1, bool __2 = false)
        {
            __result = MenuManager.inst.HandleEvent(__0, __1, __2);
            return false;
        }

        [HarmonyPatch("AddElement")]
        [HarmonyPrefix]
        static bool AddElementPrefix(ref IEnumerator __result, Element __0, bool __1)
        {
            __result = MenuManager.inst.AddElement(__0, __1);
            return false;
        }

        [HarmonyPatch("ParseLilScript")]
        [HarmonyPrefix]
        static bool ParseLilScriptPrefix(string __0)
        {
            MenuManager.inst.ParseLilScript(__0);
            return false;
        }

        public static BranchType convertInterfaceBranchToEnum(InterfaceController __instance, string _type) => __instance.convertInterfaceBranchToEnum(_type);

        public static ElementType convertInterfaceElementToEnum(InterfaceController __instance, string _type) => __instance.convertInterfaceElementToEnum(_type);

        public static string RunTextTransformations(InterfaceController __instance, string dataText, int childCount) => __instance.RunTextTransformations(dataText, childCount);

        public static IEnumerator ScrollBottom(InterfaceController __instance) => __instance.ScrollBottom();

        public static ButtonType ConvertStringToButtonType(InterfaceController __instance, string _type) => __instance.ConvertStringToButtonType(_type);

        public static EventTrigger.Entry CreateButtonHoverTrigger(InterfaceController __instance, EventTriggerType _type, GameObject _element) => __instance.CreateButtonHoverTrigger(_type, _element);

        public static EventTrigger.Entry CreateButtonTrigger(InterfaceController __instance, EventTriggerType _type, GameObject element, string _link) => __instance.CreateButtonTrigger(_type, element, _link);
    }
}

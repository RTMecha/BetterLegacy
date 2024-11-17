using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Editor.Managers;
using BetterLegacy.Patchers;
using DG.Tweening;
using LSFunctions;
using SimpleJSON;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Branch = InterfaceController.InterfaceBranch;
using ButtonSetting = InterfaceController.ButtonSetting;
using ButtonType = InterfaceController.ButtonSetting.Type;
using Element = InterfaceController.InterfaceElement;
using ElementType = InterfaceController.InterfaceElement.Type;

namespace BetterLegacy.Menus
{
    public class MenuManager : MonoBehaviour
    {
        public static MenuManager inst;

        public InterfaceController ic;

        public static void Init() => Creator.NewGameObject(nameof(MenuManager), SystemManager.inst.transform).AddComponent<MenuManager>();

        void Awake() => inst = this;

        void Update()
        {
            if (CoreHelper.IsUsingInputField)
                return;

            // For loading the Interface scene when you make a change to the menus.
            if (Input.GetKeyDown(MenuConfig.Instance.ReloadMainMenu.Value) && ic && ic.gameObject.scene.name == "Main Menu")
                SceneHelper.LoadScene(SceneName.Main_Menu);

            // For resetting menu selection, due to UnityExplorer removing the menu selection.
            if (Input.GetKeyDown(MenuConfig.Instance.SelectFirstButton.Value) && ic && ic.buttons != null && ic.buttons.Count > 0)
            {
                ic.currHoveredButton = ic.buttons[0];
                EventSystem.current.SetSelectedGameObject(ic.buttons[0]);
            }
        }

        public void ApplyInterfaceTheme()
        {
            if (!ic)
                return;

            var previousTextColor = ic.interfaceSettings.textColor;
            ic.interfaceSettings.textHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text-highlight"]);
            ic.interfaceSettings.bgColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);
            ic.interfaceSettings.borderHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["highlight"]);
            ic.interfaceSettings.textColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text"]);
            ic.interfaceSettings.borderColor = LSColors.HexToColorAlpha(DataManager.inst.GetSettingEnumValues("UITheme", 0)["buttonbg"]);
            ic.cam.GetComponent<Camera>().backgroundColor = ic.interfaceSettings.bgColor;

            var tmpUGUI = ic.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var textMeshProUGUI2 in tmpUGUI)
            {
                if (textMeshProUGUI2.color == previousTextColor)
                    textMeshProUGUI2.color = ic.interfaceSettings.textColor;
            }

            var tmp = ic.MainPanel.transform.GetComponentsInChildren<TextMeshPro>();
            foreach (var textMeshProUGUI2 in tmp)
            {
                if (textMeshProUGUI2.color == previousTextColor)
                    textMeshProUGUI2.color = ic.interfaceSettings.textColor;
            }

            SaveManager.inst.UpdateSettingsFile(false);
        }

        #region Base

        public void ParseLilScript(string _json, bool switchBranch = true)
        {
            if (ic == null)
                ic = Resources.FindObjectsOfTypeAll<InterfaceController>()[0];

            DOTween.KillAll();
            DOTween.Clear(true);
            JSONNode jn = JSON.Parse(_json);
            if (jn["settings"] != null)
            {
                ic.interfaceSettings.times = new Vector2(jn["settings"]["times"]["min"].AsFloat, jn["settings"]["times"]["max"].AsFloat);
                ic.interfaceSettings.language = DataManager.inst.GetSettingInt("Language_i", 0);
                ic.interfaceSettings.initialBranch = jn["settings"]["initial_branch"];
                if (jn["settings"]["text_color"] != null)
                    ic.interfaceSettings.textColor = LSColors.HexToColor(jn["settings"]["text_color"]);

                if (jn["settings"]["bg_color"] != null)
                    ic.interfaceSettings.bgColor = LSColors.HexToColor(jn["settings"]["bg_color"]);

                ic.interfaceSettings.music = jn["settings"]["music"];
                ic.interfaceSettings.returnBranch = jn["settings"]["return_branch"];
            }

            ic.StartCoroutine(HandleEvent(null, "apply_ui_theme", true));
            for (int i = 0; i < jn["branches"].Count; i++)
            {
                var jnbranch = jn["branches"];
                ic.interfaceBranches.Add(new Branch(jnbranch[i]["name"]));
                ic.interfaceBranches[i].clear_screen = jnbranch[i]["settings"]["clear_screen"].AsBool;
                ic.interfaceBranches[i].BackBranch = jnbranch[i]["settings"]["back_branch"] != null ? jnbranch[i]["settings"]["back_branch"] : "";

                ic.interfaceBranches[i].type = ic.convertInterfaceBranchToEnum(jnbranch[i]["settings"]["type"]);
                for (int j = 0; j < jnbranch[i]["elements"].Count; j++)
                {
                    var type = ElementType.Text;
                    var settings = new Dictionary<string, string>();
                    var list = new List<string>();
                    if (jnbranch[i]["elements"][j]["type"] != null)
                        type = ic.convertInterfaceElementToEnum(jnbranch[i]["elements"][j]["type"]);

                    if (jnbranch[i]["elements"][j]["settings"] != null)
                    {
                        foreach (var child in jnbranch[i]["elements"][j]["settings"].Children)
                        {
                            string[] array = ((string)child).Split(new char[1] { ':' }, 2);
                            settings.Add(array[0], array[1]);
                        }
                    }

                    int loop = 1;
                    if (settings.TryGetValue("loop", out string loopSetting))
                        loop = Parser.TryParse(loopSetting, 1);

                    if (jnbranch[i]["elements"][j]["data"] != null)
                    {
                        foreach (string item in jnbranch[i]["elements"][j]["data"].Children)
                            list.Add(item);
                    }
                    else
                        CoreHelper.LogError($"Couldn't load data for branch [{i}] element [{j}]");

                    if (list.Has(x => x.Contains("listlevels")) && type == ElementType.Buttons)
                    {
                        string[] data = list.Find(x => x.Contains("listlevels")).Split(new string[1] { "::" }, 5, StringSplitOptions.None);

                        if (data.Length > 1)
                        {
                            var path = data[1];

                            if (RTFile.DirectoryExists(RTFile.ApplicationDirectory + path))
                            {
                                list.Clear();

                                var str = "";
                                var dataStr = "";
                                var directories = Directory.GetDirectories(RTFile.ApplicationDirectory + path, data.Length > 2 ? data[2] : "*", SearchOption.TopDirectoryOnly);
                                for (int k = 0; k < directories.Length; k++)
                                {
                                    var p = directories[k].Replace("\\", "/");

                                    if (!RTFile.FileExists(p + "/level.lsb"))
                                        continue;

                                    str += "event|loadlevel|" + p.Replace(RTFile.ApplicationDirectory, "") + "|True";
                                    dataStr += $"      [{Path.GetFileName(p)}]:";

                                    if (k != directories.Length - 1)
                                    {
                                        str += ":";
                                        dataStr += "&&";
                                    }
                                }

                                if (!settings.ContainsKey("buttons"))
                                    settings.Add("buttons", str);
                                list.Add(dataStr);
                            }
                        }
                    }

                    if (settings.Count > 0)
                    {
                        for (int k = 0; k < loop; k++)
                            ic.interfaceBranches[i].elements.Add(new Element(jnbranch[i]["name"], type, settings, list));
                        continue;
                    }

                    for (int k = 0; k < loop; k++)
                        ic.interfaceBranches[i].elements.Add(new Element(jnbranch[i]["name"], type, list));
                }
            }

            CoreHelper.Log($"Parsed interface with [{jn["branches"].Count}] branches");

            if (switchBranch)
                ic.SwitchBranch(ic.interfaceSettings.initialBranch);
        }

        public IEnumerator HandleEvent(string _branch, string _data, bool _override = false)
        {
            if (!(ic.currentBranch == _branch || _override))
                yield break;

            string dataEvent = _data;

            if (!dataEvent.Contains("::"))
                dataEvent = dataEvent.Replace("|", "::");

            string[] data = dataEvent.Split(new string[1] { "::" }, 5, StringSplitOptions.None);
            switch (data[0].ToLower())
            {
                case "if":
                    if (DataManager.inst.GetSettingBool(data[1]))
                        ic.SwitchBranch(data[2]);
                    break;
                case "setting":
                    switch (data[1].ToLower())
                    {
                        case "bool":
                            DataManager.inst.UpdateSettingBool(data[2], bool.Parse(data[3]));
                            break;
                        case "enum":
                            DataManager.inst.UpdateSettingEnum(data[2], int.Parse(data[3]));
                            break;
                        case "string":
                        case "str":
                            DataManager.inst.UpdateSettingString(data[2], data[3]);
                            break;
                        case "achievement":
                        case "achieve":
                            SteamWrapper.inst.achievements.SetAchievement(data[2]);
                            break;
                        case "clearachievement":
                        case "clearachieve":
                            SteamWrapper.inst.achievements.ClearAchievement(data[2]);
                            break;
                        case "int":
                            if (data[3] == "add")
                            {
                                DataManager.inst.UpdateSettingInt(data[2], DataManager.inst.GetSettingInt(data[2]) + 1);
                            }
                            else if (data[3] == "sub")
                            {
                                DataManager.inst.UpdateSettingInt(data[2], DataManager.inst.GetSettingInt(data[2]) - 1);
                            }
                            else
                            {
                                DataManager.inst.UpdateSettingInt(data[2], int.Parse(data[3]));
                            }
                            break;
                        default:
                            Debug.LogError("Kind not found for setting [" + dataEvent + "]");
                            break;
                    }
                    break;
                case "apply_ui_theme_with_reload":
                    {
                        Color textColor3 = ic.interfaceSettings.textColor;
                        ic.interfaceSettings.textHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text-highlight"]);
                        ic.interfaceSettings.bgColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["bg"]);
                        ic.interfaceSettings.borderHighlightColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["highlight"]);
                        ic.interfaceSettings.textColor = LSColors.HexToColor(DataManager.inst.GetSettingEnumValues("UITheme", 0)["text"]);
                        ic.interfaceSettings.borderColor = LSColors.HexToColorAlpha(DataManager.inst.GetSettingEnumValues("UITheme", 0)["buttonbg"]);
                        ic.SwitchBranch(ic.currentBranch);
                        ic.cam.GetComponent<Camera>().backgroundColor = ic.interfaceSettings.bgColor;

                        var tmpUGUI = ic.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>();
                        foreach (var textMeshProUGUI3 in tmpUGUI)
                        {
                            if (textMeshProUGUI3.color == textColor3)
                            {
                                textMeshProUGUI3.color = ic.interfaceSettings.textColor;
                            }
                        }

                        var tmp = ic.MainPanel.transform.GetComponentsInChildren<TextMeshPro>();
                        foreach (var textMeshProUGUI3 in tmp)
                        {
                            if (textMeshProUGUI3.color == textColor3)
                            {
                                textMeshProUGUI3.color = ic.interfaceSettings.textColor;
                            }
                        }

                        SaveManager.inst.UpdateSettingsFile(false);
                        break;
                    }
                case "apply_ui_theme":
                    {
                        ApplyInterfaceTheme();
                        break;
                    }
                case "apply_menu_music":
                    AudioManager.inst.PlayMusic(DataManager.inst.GetSettingEnumValues("MenuMusic", 0), 1f);
                    break;
                case "apply_video_settings_with_reload":
                    ic.SwitchBranch(ic.currentBranch);
                    ic.ApplyVideoSettings();
                    SaveManager.inst.UpdateSettingsFile(false);
                    break;
                case "apply_video_settings":
                    ic.ApplyVideoSettings();
                    break;
                case "save_settings":
                    SaveManager.inst.UpdateSettingsFile(false);
                    break;
                case "wait":
                    if (data.Length >= 2)
                    {
                        float result = 0.5f;
                        float.TryParse(data[1], out result);
                        if (ic.SpeedUp && ic.FastSpeed > 0f)
                            yield return new WaitForSeconds(result / ic.FastSpeed);
                        else
                            yield return new WaitForSeconds(result);
                    }
                    break;
                case "branch":
                    ic.SwitchBranch(data[1]);
                    break;
                case "setsavedlevel":
                    Debug.LogFormat("setsavedlevel: {0} - {1}", int.Parse(data[1]), int.Parse(data[2]));
                    SaveManager.inst.SetSaveStoryLevel(int.Parse(data[1]), int.Parse(data[2]));
                    break;
                case "setcurrentlevel":
                    SaveManager.inst.SetCurrentStoryLevel(int.Parse(data[1]), int.Parse(data[2]));
                    break;
                case "loadscene":
                    Debug.Log("Try to load [" + data[1] + "]");
                    if (data.Length >= 3)
                        SceneManager.inst.LoadScene(data[1], bool.Parse(data[2]));
                    else
                        SceneManager.inst.LoadScene(data[1]);
                    break;
                case "deleteline":
                    if (data.Length > 2)
                        Destroy(ic.MainPanel.GetChild(ic.MainPanel.childCount - 1 + int.Parse(data[1])).gameObject);
                    else
                        Destroy(ic.MainPanel.GetChild(int.Parse(data[1])).gameObject);
                    break;
                case "replaceline":
                    {
                        AudioManager.inst.PlaySound("Click");
                        string dataText = data[2];
                        int childCount = ((data.Length > 3) ? (ic.MainPanel.childCount - 1 + int.Parse(data[1])) : int.Parse(data[1]));
                        dataText = ic.RunTextTransformations(dataText, childCount);
                        if (data.Length > 3)
                        {
                            if (ic.MainPanel.GetChild(ic.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
                                textMeshProUGUI.text = dataText;
                            if (ic.MainPanel.GetChild(ic.MainPanel.childCount - 1 + int.Parse(data[1])).Find("text").gameObject.TryGetComponent(out TextMeshPro textMeshPro))
                                textMeshPro.text = dataText;
                        }
                        else
                        {
                            if (ic.MainPanel.GetChild(int.Parse(data[1])).Find("text").gameObject.TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
                                textMeshProUGUI.text = dataText;
                            if (ic.MainPanel.GetChild(int.Parse(data[1])).Find("text").gameObject.TryGetComponent(out TextMeshPro textMeshPro))
                                textMeshPro.text = dataText;
                        }
                        break;
                    }
                case "replacelineinbranch":
                    {
                        int index = ic.interfaceBranches.FindIndex(x => x.name == data[1]);
                        ic.interfaceBranches[index].elements[int.Parse(data[2])].data = new List<string> { data[3] };
                        break;
                    }
                case "playsound":
                    {
                        if (!RTFile.FileExists(RTFile.ApplicationDirectory + data[1]))
                            AudioManager.inst.PlaySound(data[1]);
                        else
                            ic.StartCoroutine(FileManager.inst.LoadMusicFile(data[1], AudioManager.inst.PlaySound));
                        break;
                    }
                case "playsoundonline":
                    {
                        try
                        {
                            if (data[1].ToLower().Substring(data[1].ToLower().Length - 4, 4) == ".ogg")
                                ic.StartCoroutine(AlephNetworkManager.DownloadAudioClip(data[1], AudioType.OGGVORBIS, AudioManager.inst.PlaySound));
                        }
                        catch
                        {

                        }
                        break;
                    }
                case "playmusic":
                    {
                        if (!RTFile.FileExists(RTFile.ApplicationDirectory + data[1]))
                            AudioManager.inst.PlayMusic(data[1], 0.5f);
                        else
                            ic.StartCoroutine(FileManager.inst.LoadMusicFile(data[1], clip => { AudioManager.inst.PlayMusic(data[1], clip, false, 0.5f); }));
                        break;
                    }
                case "pausemusic":
                    AudioManager.inst.CurrentAudioSource.Pause();
                    break;
                case "resumemusic":
                    AudioManager.inst.CurrentAudioSource.Play();
                    break;
                case "setmusicvol":
                    if (data[1] == "back")
                        AudioManager.inst.CurrentAudioSource.volume = AudioManager.inst.musicVol;
                    else
                        AudioManager.inst.CurrentAudioSource.volume = float.Parse(data[1]);
                    break;
                case "clearplayers":
                    if (data.Length > 1)
                        InputDataManager.inst.ClearInputs((data[1] == "true") ? true : false);
                    else
                        InputDataManager.inst.ClearInputs();
                    break;
                case "setbg":
                    ic.interfaceSettings.bgColor = LSColors.HexToColor(data[1].Replace("#", ""));
                    ic.cam.GetComponent<Camera>().backgroundColor = ic.interfaceSettings.bgColor;
                    break;
                case "sethighlight":
                    ic.interfaceSettings.borderHighlightColor = LSColors.HexToColor(data[1].Replace("#", ""));
                    break;
                case "settext":
                    {
                        Color textColor = ic.interfaceSettings.textColor;
                        ic.interfaceSettings.textColor = LSColors.HexToColor(data[1].Replace("#", ""));

                        var tmpUGUI = ic.MainPanel.transform.GetComponentsInChildren<TextMeshProUGUI>();
                        foreach (var textMeshProUGUI in tmpUGUI)
                        {
                            if (textMeshProUGUI.color == textColor)
                            {
                                textMeshProUGUI.color = ic.interfaceSettings.textColor;
                            }
                        }

                        var tmp = ic.MainPanel.transform.GetComponentsInChildren<TextMeshPro>();
                        foreach (var textMeshProUGUI in tmp)
                        {
                            if (textMeshProUGUI.color == textColor)
                            {
                                textMeshProUGUI.color = ic.interfaceSettings.textColor;
                            }
                        }
                        break;
                    }
                case "setbuttonbg":
                    {
                        string text = data[1].Replace("#", "");
                        Color borderColor = ic.interfaceSettings.borderColor;
                        if (text == "none")
                        {
                            ic.interfaceSettings.borderColor = new Color(0f, 0f, 0f, 0f);
                        }
                        else
                        {
                            ic.interfaceSettings.borderColor = LSColors.HexToColorAlpha(text);
                        }
                        break;
                    }
                case "quittoarcade":
                    {
                        if (GameManager.inst != null)
                        {
                            GameManager.inst.QuitToArcade();
                        }
                        break;
                    } 
            }
            yield return null;
        }

        public void UpdateSetting(ButtonSetting buttonSetting, bool decrease, bool increase)
        {
            switch (buttonSetting.type)
            {
                case ButtonType.Int:
                    {
                        int num2 = DataManager.inst.GetSettingInt(buttonSetting.setting);
                        if (decrease)
                        {
                            num2 -= buttonSetting.value;
                            if (num2 < buttonSetting.min)
                            {
                                AudioManager.inst.PlaySound("Block");
                                num2 = buttonSetting.min;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                Debug.Log(string.Concat(new object[]
                                {
                                    "Subtract : ",
                                    num2,
                                    " : ",
                                    buttonSetting.setting
                                }));
                                DataManager.inst.UpdateSettingInt(buttonSetting.setting, num2);
                            }
                        }
                        if (increase)
                        {
                            num2 += buttonSetting.value;
                            if (num2 > buttonSetting.max)
                            {
                                AudioManager.inst.PlaySound("Block");
                                num2 = buttonSetting.max;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                Debug.Log(string.Concat(new object[]
                                {
                                    "Add : ",
                                    num2,
                                    " : ",
                                    buttonSetting.setting
                                }));
                                DataManager.inst.UpdateSettingInt(buttonSetting.setting, num2);
                            }
                        }

                        break;
                    }
                case ButtonType.Bool:
                    {
                        bool enabled = DataManager.inst.GetSettingBool(buttonSetting.setting);
                        if (decrease || increase)
                        {
                            enabled = !enabled;
                            AudioManager.inst.PlaySound("LeftRight");
                            DataManager.inst.UpdateSettingBool(buttonSetting.setting, enabled);
                        }
                        break;
                    }
                case ButtonType.Vector2:
                    {
                        int vector2Index = DataManager.inst.GetSettingVector2DIndex(buttonSetting.setting);
                        if (decrease)
                        {
                            vector2Index -= buttonSetting.value;
                            if (vector2Index < buttonSetting.min)
                            {
                                AudioManager.inst.PlaySound("Block");
                                vector2Index = buttonSetting.min;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                DataManager.inst.UpdateSettingVector2D(buttonSetting.setting, vector2Index, DataManager.inst.resolutions.ToArray());
                            }
                        }
                        if (increase)
                        {
                            vector2Index += buttonSetting.value;
                            if (vector2Index > buttonSetting.max)
                            {
                                AudioManager.inst.PlaySound("Block");
                                vector2Index = buttonSetting.max;
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                DataManager.inst.UpdateSettingVector2D(buttonSetting.setting, vector2Index, DataManager.inst.resolutions.ToArray());
                            }
                        }
                        break;
                    }
                case ButtonType.String:
                    {
                        int enumIndex = DataManager.inst.GetSettingEnum(buttonSetting.setting, 0);
                        if (buttonSetting.setting == "Language")
                        {
                            enumIndex = DataManager.inst.GetSettingInt(buttonSetting.setting + "_i");
                            DataManager.inst.GetSettingString(buttonSetting.setting);
                        }
                        if (decrease)
                        {
                            if (buttonSetting.setting == "Language")
                            {
                                enumIndex -= buttonSetting.value;
                                if (enumIndex < buttonSetting.min)
                                {
                                    AudioManager.inst.PlaySound("Block");
                                    enumIndex = buttonSetting.min;
                                }
                                else
                                {
                                    AudioManager.inst.PlaySound("LeftRight");
                                    DataManager.inst.UpdateSettingInt(buttonSetting.setting + "_i", enumIndex);
                                }
                                break;
                            }

                            enumIndex--;
                            if (enumIndex < 0)
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                DataManager.inst.UpdateSettingEnum(buttonSetting.setting, enumIndex);
                                string settingEnumFunctionCall = DataManager.inst.GetSettingEnumFunctionCall(buttonSetting.setting, enumIndex);
                                if (!string.IsNullOrEmpty(settingEnumFunctionCall))
                                {
                                    ic.StartCoroutine(HandleEvent(null, settingEnumFunctionCall, true));
                                }
                            }
                        }
                        if (increase)
                        {
                            if (buttonSetting.setting == "Language")
                            {
                                enumIndex += buttonSetting.value;
                                if (enumIndex > buttonSetting.max)
                                {
                                    AudioManager.inst.PlaySound("Block");
                                    enumIndex = buttonSetting.max;
                                }
                                else
                                {
                                    AudioManager.inst.PlaySound("LeftRight");
                                    DataManager.inst.UpdateSettingInt(buttonSetting.setting + "_i", enumIndex);
                                }
                                break;
                            }

                            enumIndex++;
                            if (enumIndex >= DataManager.inst.GetSettingEnumCount(buttonSetting.setting))
                            {
                                AudioManager.inst.PlaySound("Block");
                            }
                            else
                            {
                                AudioManager.inst.PlaySound("LeftRight");
                                DataManager.inst.UpdateSettingEnum(buttonSetting.setting, enumIndex);
                                string settingEnumFunctionCall2 = DataManager.inst.GetSettingEnumFunctionCall(buttonSetting.setting, enumIndex);
                                if (!string.IsNullOrEmpty(settingEnumFunctionCall2))
                                {
                                    ic.StartCoroutine(HandleEvent(null, settingEnumFunctionCall2, true));
                                }
                            }
                        }
                        break;
                    }
            }
        }

        public IEnumerator AddElement(Element _element, bool _immediate)
        {
            if (!(_element.branch == ic.currentBranch))
                yield break;

            ic.StartCoroutine(ic.ScrollBottom());
            float totalTime = ((!ic.SpeedUp) ? UnityEngine.Random.Range(ic.interfaceSettings.times.x, ic.interfaceSettings.times.y) : (UnityEngine.Random.Range(ic.interfaceSettings.times.x, ic.interfaceSettings.times.y) / ic.FastSpeed));
            if (!_immediate)
                AudioManager.inst.PlaySound("Click");

            int childCount = ic.MainPanel.childCount;
            switch (_element.type)
            {
                case ElementType.Text:
                    {
                        string text5 = _element.data.Count > 0 ? _element.data[0] : " ";

                        string dataText2 = (_element.data.Count > ic.interfaceSettings.language) ? _element.data[ic.interfaceSettings.language] : text5;
                        var gameObject = ic.TextPrefab.Duplicate(ic.MainPanel, $"[{childCount}] Text");
                        gameObject.transform.localScale = Vector3.one;

                        var gameObject3 = gameObject.transform.Find("bg").gameObject;
                        var text = gameObject.transform.Find("text").gameObject;

                        var textMeshProUGUI = text.GetComponent<TextMeshProUGUI>();

                        if (_element.settings.ContainsKey("bg-color"))
                        {
                            if (_element.settings["bg-color"] == "text-color")
                            {
                                gameObject3.GetComponent<Image>().color = ic.interfaceSettings.textColor;
                            }
                            else
                            {
                                gameObject3.GetComponent<Image>().color = LSColors.HexToColor(_element.settings["bg-color"]);
                            }
                        }
                        else
                        {
                            gameObject3.GetComponent<Image>().color = LSColors.transparent;
                        }
                        if (_element.settings.ContainsKey("text-color"))
                        {
                            if (_element.settings["text-color"] == "bg-color")
                            {
                                textMeshProUGUI.color = ic.interfaceSettings.bgColor;
                            }
                            else
                            {
                                textMeshProUGUI.color = LSColors.HexToColor(_element.settings["text-color"]);
                            }
                        }
                        else
                        {
                            textMeshProUGUI.color = ic.interfaceSettings.textColor;
                        }

                        if (string.IsNullOrEmpty(dataText2))
                        {
                            break;
                        }

                        if (_element.settings.ContainsKey("alignment"))
                        {
                            switch (_element.settings["alignment"])
                            {
                                case "left":
                                    if (!_element.settings.ContainsKey("valignment"))
                                    {
                                        textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
                                        break;
                                    }
                                    switch (_element.settings["valignment"])
                                    {
                                        case "top":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
                                            break;
                                        case "center":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
                                            break;
                                        case "bottom":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.BottomLeft;
                                            break;
                                    }
                                    break;
                                case "center":
                                    if (!_element.settings.ContainsKey("valignment"))
                                    {
                                        textMeshProUGUI.alignment = TextAlignmentOptions.Midline;
                                        break;
                                    }
                                    switch (_element.settings["valignment"])
                                    {
                                        case "top":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.Top;
                                            break;
                                        case "center":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.Midline;
                                            break;
                                        case "bottom":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.Bottom;
                                            break;
                                    }
                                    break;
                                case "right":
                                    if (!_element.settings.ContainsKey("valignment"))
                                    {
                                        textMeshProUGUI.alignment = TextAlignmentOptions.MidlineRight;
                                        break;
                                    }
                                    switch (_element.settings["valignment"])
                                    {
                                        case "top":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.TopRight;
                                            break;
                                        case "center":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.MidlineRight;
                                            break;
                                        case "bottom":
                                            textMeshProUGUI.alignment = TextAlignmentOptions.BottomRight;
                                            break;
                                    }
                                    break;
                            }
                        }
                        else if (_element.settings.ContainsKey("valignment"))
                        {
                            switch (_element.settings["valignment"])
                            {
                                case "top":
                                    textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
                                    break;
                                case "center":
                                    textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
                                    break;
                                case "bottom":
                                    textMeshProUGUI.alignment = TextAlignmentOptions.BottomLeft;
                                    break;
                            }
                        }

                        dataText2 = ic.RunTextTransformations(dataText2, childCount);
                        if (dataText2.Contains("[[") && dataText2.Contains("]]"))
                        {
                            foreach (Match item in Regex.Matches(dataText2, "\\[\\[([^\\]]*)\\]\\]"))
                            {
                                Debug.Log(item.Groups[0].Value);
                                string value = item.Groups[0].Value;
                                string value2 = item.Groups[1].Value;
                                dataText2 = dataText2.Replace(value, LSText.FormatString(value2));
                            }
                        }
                        string[] words = dataText2.Split(new string[1] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        string tempText = "";
                        for (int i = 0; i < words.Length; i++)
                        {
                            float seconds = totalTime / (float)words.Length;
                            if (text != null)
                            {
                                tempText = tempText + words[i] + " ";
                                textMeshProUGUI.text = tempText + ((i % 2 == 0) ? "▓▒░" : "▒░░");
                            }
                            yield return new WaitForSeconds(seconds);
                        }

                        if (_element.settings.ContainsKey("font-style"))
                        {
                            switch (_element.settings["font-style"])
                            {
                                case "light":
                                    textMeshProUGUI.fontStyle = FontStyles.Italic;
                                    break;
                                case "normal":
                                    textMeshProUGUI.fontStyle = FontStyles.Normal;
                                    break;
                                case "bold":
                                    textMeshProUGUI.fontStyle = FontStyles.Bold;
                                    break;
                            }
                        }
                        else
                        {
                            textMeshProUGUI.fontStyle = FontStyles.Normal;
                        }

                        textMeshProUGUI.text = dataText2;
                        break;
                    }
                case ElementType.Buttons:
                    {
                        var gameObject = ic.ButtonElementPrefab.Duplicate(ic.MainPanel, $"[{childCount} Button Holder]");
                        gameObject.transform.localScale = Vector3.one;

                        if (_element.settings.ContainsKey("width"))
                            gameObject.GetComponent<LayoutElement>().preferredWidth = Parser.TryParse(_element.settings["width"], 0.5f) * 1792f;

                        if (_element.settings.ContainsKey("orientation"))
                        {
                            if (_element.settings["orientation"] == "horizontal")
                            {
                                gameObject.GetComponent<VerticalLayoutGroup>().enabled = false;
                            }
                            else if (_element.settings["orientation"] == "vertical")
                            {
                                gameObject.GetComponent<HorizontalLayoutGroup>().enabled = false;
                            }
                            else if (_element.settings["orientation"] == "grid")
                            {
                                DestroyImmediate(gameObject.GetComponent<HorizontalLayoutGroup>());
                                DestroyImmediate(gameObject.GetComponent<VerticalLayoutGroup>());
                                var gridLayoutGroup = gameObject.AddComponent<GridLayoutGroup>();
                                gridLayoutGroup.spacing = new Vector2(16f, 16f);

                                float gridH = 1f;
                                if (_element.settings.ContainsKey("grid_h"))
                                    float.TryParse(_element.settings["grid_h"], out gridH);

                                int gridCorner = 0;
                                if (_element.settings.ContainsKey("grid_corner"))
                                    int.TryParse(_element.settings["grid_corner"], out gridCorner);

                                float gridV = 1f;
                                if (_element.settings.ContainsKey("grid_v"))
                                    float.TryParse(_element.settings["grid_v"], out gridV);

                                gridLayoutGroup.cellSize = new Vector2((1792f - 16f * (gridH - 1f)) / gridH, gridV * 54f);
                                gridLayoutGroup.childAlignment = (TextAnchor)gridCorner;
                            }
                        }
                        else
                        {
                            gameObject.GetComponent<HorizontalLayoutGroup>().enabled = false;
                        }

                        string[] array = ((_element.data.Count > ic.interfaceSettings.language) ? _element.data[ic.interfaceSettings.language] : _element.data[0]).Split(new string[1] { "&&" }, StringSplitOptions.RemoveEmptyEntries);
                        ic.buttonSettings.Clear();
                        if (_element.settings.ContainsKey("buttons"))
                        {
                            string[] array2 = _element.settings["buttons"].Split(new string[1] { ":" }, StringSplitOptions.None);
                            int num = 0;
                            string[] array3 = array2;
                            foreach (string text2 in array3)
                            {
                                if (!string.IsNullOrEmpty(text2))
                                {
                                    string[] splitLines = text2.Split(new string[1] { "|" }, StringSplitOptions.None);
                                    var buttonSetting = new ButtonSetting(ic.ConvertStringToButtonType(splitLines[0]));
                                    if (buttonSetting.type == ButtonType.Event)
                                    {
                                        int splitLineIndex = 0;
                                        foreach (string splitLine in splitLines)
                                        {
                                            if (splitLineIndex != 0)
                                            {
                                                buttonSetting.setting += splitLine;
                                            }
                                            if (splitLineIndex != 0 && splitLineIndex < splitLines.Length - 1)
                                            {
                                                buttonSetting.setting += "|";
                                            }

                                            splitLineIndex++;
                                        }
                                    }
                                    else
                                    {
                                        buttonSetting.setting = splitLines[1];
                                        buttonSetting.value = int.Parse(splitLines[2]);
                                        buttonSetting.min = int.Parse(splitLines[3]);
                                        buttonSetting.max = int.Parse(splitLines[4]);
                                    }
                                    ic.buttonSettings.Add(buttonSetting);
                                }
                                else if (num != 0)
                                {
                                    ic.buttonSettings.Add(new ButtonSetting(ButtonType.Empty));
                                }
                                num++;
                            }
                        }
                        else
                        {
                            for (int l = 0; l < array.Length; l++)
                            {
                                ic.buttonSettings.Add(new ButtonSetting(ButtonType.Empty));
                            }
                        }

                        for (int i = 0; i < array.Length; i++)
                        {
                            int index = i;
                            string[] array6 = array[i].Split(':');
                            GameObject gameObject2;
                            if (ic.buttonSettings.Count > i && ic.buttonSettings[i].setting != null)
                            {
                                switch (ic.buttonSettings[i].type)
                                {
                                    case ButtonType.Int:
                                        gameObject2 = Instantiate(ic.IntButtonPrefab, Vector3.zero, Quaternion.identity);
                                        gameObject2.name = "button";
                                        break;
                                    case ButtonType.Vector2:
                                        gameObject2 = Instantiate(ic.Vector2ButtonPrefab, Vector3.zero, Quaternion.identity);
                                        gameObject2.name = "button";
                                        break;
                                    case ButtonType.Bool:
                                        gameObject2 = Instantiate(ic.BoolButtonPrefab, Vector3.zero, Quaternion.identity);
                                        gameObject2.name = "button";
                                        break;
                                    case ButtonType.String:
                                        gameObject2 = Instantiate(ic.StringButtonPrefab, Vector3.zero, Quaternion.identity);
                                        gameObject2.name = "button";
                                        break;
                                    default:
                                        gameObject2 = Instantiate(ic.ButtonPrefab, Vector3.zero, Quaternion.identity);
                                        gameObject2.name = "button";
                                        break;
                                }
                            }
                            else
                            {
                                gameObject2 = Instantiate(ic.ButtonPrefab, Vector3.zero, Quaternion.identity);
                                gameObject2.name = "button";
                            }

                            gameObject2.transform.SetParent(gameObject.transform);
                            gameObject2.transform.localScale = Vector3.one;
                            gameObject2.name = string.Format("[{0}][{1}] Button", childCount, i);

                            if (ic.buttonSettings.Count > i && ic.buttonSettings[i].setting != null &&
                                ic.buttonSettings[i].type == ButtonType.Event && ic.buttonSettings[i].setting.Contains("loadlevel") &&
                                gameObject2.transform.Find("img"))
                            {
                                var image = gameObject2.transform.Find("img").GetComponent<Image>();

                                var loadLevelArray = ic.buttonSettings[i].setting.Split(new string[1] { "|" }, StringSplitOptions.None);

                                var path = $"{RTFile.ApplicationDirectory}{loadLevelArray[1]}/level.jpg";
                                if (RTFile.FileExists(path))
                                {
                                    StartCoroutine(AlephNetworkManager.DownloadImageTexture($"file://{path}", delegate (Texture2D texture2D)
                                    {
                                        image.sprite = SpriteHelper.CreateSprite(texture2D);
                                        image.color = Color.white;
                                    }, delegate (string onError)
                                    {
                                        image.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                        image.color = Color.white;
                                    }));
                                }
                                else
                                {
                                    image.sprite = SteamWorkshop.inst.defaultSteamImageSprite;
                                    image.color = Color.white;
                                }
                            }

                            var textMeshProUGUI = gameObject2.transform.Find("text").GetComponent<TextMeshProUGUI>();

                            if (_element.settings.ContainsKey("alignment"))
                            {
                                switch (_element.settings["alignment"])
                                {
                                    case "left":
                                        if (!_element.settings.ContainsKey("valignment"))
                                        {
                                            textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
                                            break;
                                        }
                                        switch (_element.settings["valignment"])
                                        {
                                            case "top":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
                                                break;
                                            case "center":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.MidlineLeft;
                                                break;
                                            case "bottom":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.BottomLeft;
                                                break;
                                        }
                                        break;
                                    case "center":
                                        if (!_element.settings.ContainsKey("valignment"))
                                        {
                                            textMeshProUGUI.alignment = TextAlignmentOptions.Midline;
                                            break;
                                        }
                                        switch (_element.settings["valignment"])
                                        {
                                            case "top":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.Top;
                                                break;
                                            case "center":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.Midline;
                                                break;
                                            case "bottom":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.Bottom;
                                                break;
                                        }
                                        break;
                                    case "right":
                                        if (!_element.settings.ContainsKey("valignment"))
                                        {
                                            textMeshProUGUI.alignment = TextAlignmentOptions.MidlineRight;
                                            break;
                                        }
                                        switch (_element.settings["valignment"])
                                        {
                                            case "top":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.TopRight;
                                                break;
                                            case "center":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.MidlineRight;
                                                break;
                                            case "bottom":
                                                textMeshProUGUI.alignment = TextAlignmentOptions.BottomRight;
                                                break;
                                        }
                                        break;
                                }
                            }
                            else if (_element.settings.ContainsKey("valignment"))
                            {
                                switch (_element.settings["valignment"])
                                {
                                    case "top":
                                        textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
                                        break;
                                    case "center":
                                        textMeshProUGUI.alignment = TextAlignmentOptions.Left;
                                        break;
                                    case "bottom":
                                        textMeshProUGUI.alignment = TextAlignmentOptions.BottomLeft;
                                        break;
                                }
                            }
                            ic.buttons.Add(gameObject2);
                            if (i == 0 && ic.buttonsActive)
                            {
                                EventSystem.current.SetSelectedGameObject(gameObject2);
                            }

                            textMeshProUGUI.text = ic.ParseText(array6[0]);
                            if (array6[0] == "")
                            {
                                Navigation navigation = default;
                                navigation.mode = Navigation.Mode.None;
                                gameObject2.GetComponent<Button>().navigation = navigation;
                                gameObject2.transform.Find("bg").GetComponent<Image>().enabled = false;
                                continue;
                            }

                            var eventTrigger = gameObject2.GetComponent<EventTrigger>();
                            eventTrigger.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.Scroll, delegate (BaseEventData eventData)
                            {
                                var pointerEventData = (PointerEventData)eventData;
                                if (ic.buttonSettings.Count <= index)
                                    return;

                                var buttonSetting = ic.buttonSettings[index];

                                if (buttonSetting.type == ButtonType.Event || buttonSetting.type == ButtonType.Empty)
                                    return;

                                UpdateSetting(buttonSetting, pointerEventData.scrollDelta.y < 0f, pointerEventData.scrollDelta.y > 0f);
                            }));

                            eventTrigger.triggers.Add(ic.CreateButtonHoverTrigger(EventTriggerType.PointerEnter, gameObject2));
                            if (_element.settings.ContainsKey("buttons") && ic.buttonSettings[i].type == ButtonType.Event)
                            {
                                eventTrigger.triggers.Add(ic.CreateButtonTriggerForEvent(EventTriggerType.Submit, _element.branch, ic.buttonSettings[i].setting));
                                eventTrigger.triggers.Add(ic.CreateButtonTriggerForEvent(EventTriggerType.PointerClick, _element.branch, ic.buttonSettings[i].setting));
                            }
                            else if (array6.Length == 2)
                            {
                                eventTrigger.triggers.Add(ic.CreateButtonTrigger(EventTriggerType.Submit, gameObject, array6[1]));
                                eventTrigger.triggers.Add(ic.CreateButtonTrigger(EventTriggerType.PointerClick, gameObject, array6[1]));
                            }
                            else
                            {
                                if (array6[1] == "setting_str")
                                    DataManager.inst.UpdateSettingString(array6[2], array6[3]);
                            }
                            if (_element.settings.ContainsKey("default_button") && ic.buttons.Count > int.Parse(_element.settings["default_button"]) && ic.buttonsActive)
                            {
                                EventSystem.current.SetSelectedGameObject(ic.buttons[int.Parse(_element.settings["default_button"])]);
                            }
                        }
                        break;
                    }
                case ElementType.Event:
                    foreach (string datum in _element.data)
                    {
                        yield return ic.StartCoroutine(HandleEvent(_element.branch, datum));
                    }
                    break;
            }
        }

        #endregion
    }
}

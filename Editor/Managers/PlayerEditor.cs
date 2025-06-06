﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using LSFunctions;

using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// The Player editor.
    /// </summary>
    public class PlayerEditor : MonoBehaviour
    {
        public static PlayerEditor inst;

        public string modelSearchTerm;
        public int playerModelIndex = 0;
        public Transform content;
        public string CustomObjectID { get; set; }

        GameObject labelPrefab;

        public PlayerEditorDialog Dialog { get; set; }

        public static void Init() => Creator.NewGameObject(nameof(PlayerEditor), EditorManager.inst.transform.parent).AddComponent<PlayerEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                PlayersData.Load(null);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            StartCoroutine(GenerateUI());
        }

        public enum Tab
        {
            Global,
            Base, // includes stretch
            Head,
            GUI,
            Boost,
            Spawners, // Bullet and Pulse
            Tail, // All tail related parts go here
            Custom
        }

        public static Tab ParseTab(string str)
        {
            return
                str.Contains("Base") && !str.Contains("GUI") && !str.Contains("Tail") || str.Contains("Stretch") ? Tab.Base :
                !str.Contains("Pulse") && str.Contains("Head") || str.Contains("Face") ? Tab.Head :
                str.Contains("GUI") ? Tab.GUI :
                str.Contains("Boost") && !str.Contains("Tail") ? Tab.Boost :
                str.Contains("Pulse") || str.Contains("Bullet") ? Tab.Spawners :
                str.Contains("Tail") ? Tab.Tail : Tab.Custom;
        }

        public IEnumerator GenerateUI()
        {
            var dialog = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "PlayerEditorDialog");
            dialog.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            dialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = dialog.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor(BeatmapTheme.PLAYER_1_COLOR);
            dialogStorage.title.text = "- Player Editor -";

            dialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(dialog.transform.Find("Text").gameObject);

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

            var search = RTEditor.inst.OpenLevelPopup.GameObject.transform.Find("search-box").gameObject.Duplicate(dialog.transform, "search");

            var searchField = search.transform.GetChild(0).GetComponent<InputField>();

            searchField.onValueChanged.ClearAll();
            searchField.text = string.Empty;
            searchField.onValueChanged.AddListener(_val =>
            {
                searchTerm = _val;
                StartCoroutine(RefreshEditor());
            });

            ((Text)searchField.placeholder).text = "Search for value...";

            // Tabs
            {
                var spacer = dialog.transform.Find("spacer");
                var layout = Creator.NewUIObject("layout", spacer);
                UIManager.SetRectTransform(layout.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 43.2f));
                var layoutHLG = layout.AddComponent<HorizontalLayoutGroup>();
                layoutHLG.childControlWidth = false;
                layoutHLG.childForceExpandWidth = false;
                layoutHLG.spacing = 2f;

                var enumNames = Enum.GetNames(typeof(Tab));
                for (int i = 0; i < enumNames.Length; i++)
                {
                    var e = enumNames[i];
                    var tabIndex = i;
                    var tab = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, e);
                    tab.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                    var functionButtonStorage = tab.GetComponent<FunctionButtonStorage>();
                    functionButtonStorage.label.fontSize = 16;
                    functionButtonStorage.label.text = e;
                    functionButtonStorage.button.onClick.ClearAll();
                    functionButtonStorage.button.onClick.AddListener(() =>
                    {
                        CurrentTab = (Tab)tabIndex;
                        StartCoroutine(RefreshEditor());
                    });

                    EditorThemeManager.AddSelectable(functionButtonStorage.button, EditorTheme.GetGroup($"Tab Color {i + 1}"));
                    tab.AddComponent<ContrastColors>().Init(functionButtonStorage.label, tab.GetComponent<Image>());
                }
            }

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(dialog.transform, "Scroll View");
            var boolInput = EditorPrefabHolder.Instance.Toggle;

            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 512f);

            EditorHelper.AddEditorDialog(EditorDialog.PLAYER_EDITOR, dialog);
            var playerEditor = EditorHelper.AddEditorDropdown("Player Editor", "", "Edit", EditorSprites.PlayerSprite, () =>
            {
                Dialog.Open();
                StartCoroutine(RefreshEditor());
            });
            EditorHelper.SetComplexity(playerEditor, Complexity.Advanced);

            content = scrollView.transform.Find("Viewport/Content");

            labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;

            LSHelpers.DeleteChildren(content);

            // Default
            {
                var gameObject = Creator.NewUIObject("handler", content);
                gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                var label = labelPrefab.Duplicate(gameObject.transform, "label");
                var labelText = label.GetComponent<Text>();
                labelText.text = "Cannot edit default Player models.";
                UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));


                gameObject.SetActive(false);
            }

            // Global
            {
                // Speed
                {
                    var name = "Global Speed";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Float,
                        Index = -1,
                    });
                }

                // Lock Boost
                {
                    var name = "Global Lock Boost";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Bool,
                        Index = -1,
                    });
                }

                // Gamemode
                {
                    var name = "Global Gamemode";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    RectValues.Default.AnchoredPosition(212f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
                    dropdown.onValueChanged.ClearAll();
                    dropdown.options = CoreHelper.StringToOptionData("Regular", "Platformer");
                    dropdown.GetComponent<HideDropdownOptions>().DisabledOptions.Clear();

                    EditorThemeManager.AddDropdown(dropdown);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Enum,
                        Index = -1,
                    });
                }

                // Max Health
                {
                    var name = "Global Max Health";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Int,
                        Index = -1,
                    });
                }

                // Max Jump Count
                {
                    var name = "Global Max Jump Count";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Int,
                        Index = -1,
                    });
                }
                
                // Max Jump Boost Count
                {
                    var name = "Global Max Jump Boost Count";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Int,
                        Index = -1,
                    });
                }

                // Jump Gravity
                {
                    var name = "Global Jump Gravity";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Float,
                        Index = -1,
                    });
                }

                // Jump Intensity
                {
                    var name = "Global Jump Intensity";

                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Float,
                        Index = -1,
                    });
                }

                CoreHelper.For(name =>
                {
                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Bool,
                        Index = -1,
                    });
                },
                "Spawn Players",
                "Allow Custom Player Models",
                "Limit Player"
                );

                // Vector2
                CoreHelper.For(name =>
                {
                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var inputX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "x");
                    UIManager.SetRectTransform(inputX.transform.AsRT(), new Vector2(-4f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputXStorage = inputX.GetComponent<InputFieldStorage>();

                    Destroy(inputXStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputXStorage.inputField);
                    EditorThemeManager.AddSelectable(inputXStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputXStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputXStorage.leftGreaterButton.gameObject);
                    Destroy(inputXStorage.rightGreaterButton.gameObject);

                    var inputY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "y");
                    UIManager.SetRectTransform(inputY.transform.AsRT(), new Vector2(246f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputYStorage = inputY.GetComponent<InputFieldStorage>();

                    Destroy(inputYStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputYStorage.inputField);
                    EditorThemeManager.AddSelectable(inputYStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputYStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputYStorage.leftGreaterButton.gameObject);
                    Destroy(inputYStorage.rightGreaterButton.gameObject);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Bool,
                        Index = -1,
                    });
                },
                "Limit Move Speed",
                "Limit Boost Speed",
                "Limit Boost Cooldown",
                "Limit Boost Min Time",
                "Limit Boost Max Time",
                "Limit Hit Cooldown"
                );

                CoreHelper.For(name =>
                {
                    var gameObject = Creator.NewUIObject(name, content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = name;
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var image = gameObject.AddComponent<Image>();

                    var button = gameObject.AddComponent<Button>();
                    button.image = image;

                    EditorThemeManager.AddSelectable(button, ThemeGroup.List_Button_1);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = ValueType.Function,
                        Index = -1,
                    });
                },
                "Respawn Players",
                "Update Properties"
                );


            }

            for (int i = 0; i < PlayerModel.Values.Count; i++)
            {
                var name = PlayerModel.Values[i];
                var gameObject = Creator.NewUIObject(name, content);
                gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                var label = labelPrefab.Duplicate(gameObject.transform, "label");
                var labelText = label.GetComponent<Text>();
                labelText.text = name;
                UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));
                EditorThemeManager.AddLightText(labelText);

                ValueType valueType = ValueType.Function;
                if (name == "Base ID")
                {
                    valueType = ValueType.Function;

                    var id = labelPrefab.Duplicate(gameObject.transform, "id");
                    UIManager.SetRectTransform(id.transform.AsRT(), new Vector2(-32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(750f, 32f));
                    gameObject.AddComponent<Button>();
                }

                if (name == "Base Name" || name.Contains("Color") && name.Contains("Custom"))
                {
                    valueType = ValueType.String;

                    var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

                    EditorThemeManager.AddInputField(input.GetComponent<InputField>());
                }

                if (name == "Base Health" || name == "Boost Particles Amount" || name == "Base Jump Gravity" ||
                    name.Contains("Scale") && (name.Contains("Start") || name.Contains("End")) && !name.Contains("Pulse") && !name.Contains("Bullet") ||
                    name.Contains("Rotation") && !name.Contains("Easing") ||
                    name == "Tail Base Distance" ||
                    name.Contains("Opacity") && !name.Contains("Easing") ||
                    name.Contains("Lifetime") ||
                    name.Contains("Start Width") ||
                    name.Contains("End Width") ||
                    name.Contains("Trail Time") ||
                    name == "Boost Particles Duration" ||
                    name.Contains("Amount") && !name.Contains("Boost") ||
                    name.Contains("Speed") || name.Contains("Depth") || name == "Pulse Duration" ||
                    name.Contains("Cooldown") || name.Contains("Boost Time") || name == "Bullet Lifetime" || name.Contains("Duration") ||
                    name == "Tail Base Time" || name == "Base Jump Gravity" || name == "Base Jump Count" || name == "Base Jump Boost Count" || name == "Base Jump Intensity" || name == "Base Bounciness")
                {
                    if (name == "Base Health" || name == "Boost Particles Amount" || name == "Base Jump Count" || name == "Base Jump Boost Count")
                        valueType = ValueType.Int;
                    if (name.Contains("Scale") && (name.Contains("Start") || name.Contains("End")) && !name.Contains("Pulse") && !name.Contains("Bullet") ||
                        name.Contains("Rotation") && !name.Contains("Easing") ||
                        name == "Tail Base Distance" ||
                        name.Contains("Opacity") && !name.Contains("Easing") ||
                        name.Contains("Lifetime") ||
                        name.Contains("Start Width") ||
                        name.Contains("End Width") ||
                        name.Contains("Trail Time") ||
                        name == "Boost Particles Duration" ||
                        name.Contains("Amount") && !name.Contains("Boost") ||
                        name.Contains("Speed") || name.Contains("Depth") || name == "Pulse Duration" ||
                        name.Contains("Cooldown") || name.Contains("Boost Time") || name == "Bullet Lifetime" || name.Contains("Duration") ||
                        name == "Tail Base Time" || name == "Base Jump Gravity" || name == "Base Jump Intensity" || name == "Base Bounciness")
                        valueType = ValueType.Float;

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                }

                if (name.Contains("Position") && !name.Contains("Easing") && !name.Contains("Duration") ||
                    name.Contains("Scale") && !name.Contains("Particles") && !name.Contains("Easing") && !name.Contains("Duration") ||
                    name.Contains("Force") || name.Contains("Origin"))
                {
                    valueType = ValueType.Vector2;

                    var inputX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "x");
                    UIManager.SetRectTransform(inputX.transform.AsRT(), new Vector2(-4f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputXStorage = inputX.GetComponent<InputFieldStorage>();

                    Destroy(inputXStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputXStorage.inputField);
                    EditorThemeManager.AddSelectable(inputXStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputXStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputXStorage.leftGreaterButton.gameObject);
                    Destroy(inputXStorage.rightGreaterButton.gameObject);

                    var inputY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "y");
                    UIManager.SetRectTransform(inputY.transform.AsRT(), new Vector2(246f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputYStorage = inputY.GetComponent<InputFieldStorage>();

                    Destroy(inputYStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputYStorage.inputField);
                    EditorThemeManager.AddSelectable(inputYStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputYStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputYStorage.leftGreaterButton.gameObject);
                    Destroy(inputYStorage.rightGreaterButton.gameObject);
                }

                if (name == "Tail Base Mode")
                {
                    valueType = ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    RectValues.Default.AnchoredPosition(212f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
                    dropdown.onValueChanged.ClearAll();
                    dropdown.options = CoreHelper.StringToOptionData("Legacy", "Dev+");
                    dropdown.GetComponent<HideDropdownOptions>().DisabledOptions.Clear();

                    EditorThemeManager.AddDropdown(dropdown);
                }
                
                if (name == "Base Rotate Mode")
                {
                    valueType = ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    RectValues.Default.AnchoredPosition(212f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
                    dropdown.onValueChanged.ClearAll();
                    dropdown.options = CoreHelper.StringToOptionData("Face Direction", "None", "Flip X", "Flip Y", "Rotate Reset", "Rotate Flip X", "Rotate Flip Y");
                    dropdown.GetComponent<HideDropdownOptions>().DisabledOptions.Clear();

                    EditorThemeManager.AddDropdown(dropdown);
                }
                
                if (name == "GUI Health Mode")
                {
                    valueType = ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    RectValues.Default.AnchoredPosition(212f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
                    dropdown.onValueChanged.ClearAll();
                    dropdown.options = CoreHelper.StringToOptionData("Images", "Text", "Equals Bar", "Bar");
                    dropdown.GetComponent<HideDropdownOptions>().DisabledOptions.Clear();

                    EditorThemeManager.AddDropdown(dropdown);
                }
                
                if (name.Contains("Easing"))
                {
                    valueType = ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    RectValues.Default.AnchoredPosition(212f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
                    dropdown.options.Clear();
                    dropdown.onValueChanged.ClearAll();

                    dropdown.options = EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();
                    dropdown.GetComponent<HideDropdownOptions>().DisabledOptions.Clear();

                    EditorThemeManager.AddDropdown(dropdown);
                }

                if (name.Contains("Color") && !name.Contains("Easing") && !name.Contains("Custom") && !name.Contains("Duration"))
                {
                    valueType = ValueType.Color;

                    var layout = Creator.NewUIObject("colors", gameObject.transform);
                    layout.transform.AsRT().anchoredPosition = new Vector2(170f, -16f);
                    layout.transform.AsRT().sizeDelta = new Vector2(400f, 100f);
                    var layoutGLG = layout.AddComponent<GridLayoutGroup>();
                    layoutGLG.cellSize = new Vector2(32f, 32f);
                    layoutGLG.spacing = new Vector2(8f, 8f);

                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 162f);

                    for (int j = 0; j < 25; j++)
                    {
                        var color = EditorManager.inst.colorGUI.Duplicate(layout.transform, $"{j + 1}");
                        EditorThemeManager.AddGraphic(color.GetComponent<Image>(), ThemeGroup.Null, true);
                        EditorThemeManager.AddGraphic(color.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Background_1);
                    }
                }

                if (name.Contains("Active") || name.Contains("Emitting") || name == "Pulse Rotate to Head" ||
                    name == "Tail Base Grows" || name == "Base Collision Accurate" || name == "Bullet Constant" ||
                    name == "Bullet Hurt Players" || name == "Bullet AutoKill" || name == "Base Can Boost")
                {
                    valueType = ValueType.Bool;

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                if (name.Contains("Shape"))
                {
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 92f);

                    var shape = ObjEditor.inst.ObjectView.transform.Find("shape").gameObject.Duplicate(gameObject.transform, "shape");
                    UIManager.SetRectTransform(shape.transform.AsRT(), new Vector2(568f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(351f, 32f));
                    var shapeSettings = ObjEditor.inst.ObjectView.transform.Find("shapesettings").gameObject.Duplicate(gameObject.transform, "shapesettings");
                    UIManager.SetRectTransform(shapeSettings.transform.AsRT(), new Vector2(568f, -54f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(351f, 32f));
                }

                if (name == "Custom Objects")
                {
                    valueType = ValueType.Function;

                    labelText.text = "Select a custom object.";
                    var button = gameObject.AddComponent<Button>();
                    button.image = gameObject.AddComponent<Image>();

                    EditorThemeManager.AddSelectable(button, ThemeGroup.List_Button_1, true);
                }

                editorUIs.Add(new PlayerEditorUI
                {
                    Name = name,
                    GameObject = gameObject,
                    Tab = ParseTab(name),
                    ValueType = valueType,
                    Index = i,
                });
            }

            // Custom Objects
            {
                // ID
                {
                    var gameObject = GenerateUIPart("ID", Tab.Custom, ValueType.Function);

                    var id = labelPrefab.Duplicate(gameObject.transform, "id");
                    UIManager.SetRectTransform(id.transform.AsRT(), new Vector2(-32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(750f, 32f));
                    var button = gameObject.AddComponent<Button>();
                }

                // Name
                {
                    var gameObject = GenerateUIPart("Name", Tab.Custom, ValueType.String);

                    var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

                    EditorThemeManager.AddInputField(input.GetComponent<InputField>());
                }

                // Position
                {
                    var gameObject = GenerateUIPart("Position", Tab.Custom, ValueType.Vector2);

                    var inputX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "x");
                    UIManager.SetRectTransform(inputX.transform.AsRT(), new Vector2(-4f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputXStorage = inputX.GetComponent<InputFieldStorage>();

                    Destroy(inputXStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputXStorage.inputField);
                    EditorThemeManager.AddSelectable(inputXStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputXStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputXStorage.leftGreaterButton.gameObject);
                    Destroy(inputXStorage.rightGreaterButton.gameObject);

                    var inputY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "y");
                    UIManager.SetRectTransform(inputY.transform.AsRT(), new Vector2(246f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputYStorage = inputY.GetComponent<InputFieldStorage>();

                    Destroy(inputYStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputYStorage.inputField);
                    EditorThemeManager.AddSelectable(inputYStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputYStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputYStorage.leftGreaterButton.gameObject);
                    Destroy(inputYStorage.rightGreaterButton.gameObject);
                }

                // Scale
                {
                    var gameObject = GenerateUIPart("Scale", Tab.Custom, ValueType.Vector2);

                    var inputX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "x");
                    UIManager.SetRectTransform(inputX.transform.AsRT(), new Vector2(-4f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputXStorage = inputX.GetComponent<InputFieldStorage>();

                    Destroy(inputXStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputXStorage.inputField);
                    EditorThemeManager.AddSelectable(inputXStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputXStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputXStorage.leftGreaterButton.gameObject);
                    Destroy(inputXStorage.rightGreaterButton.gameObject);

                    var inputY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "y");
                    UIManager.SetRectTransform(inputY.transform.AsRT(), new Vector2(246f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(230f, 32f));

                    var inputYStorage = inputY.GetComponent<InputFieldStorage>();

                    Destroy(inputYStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputYStorage.inputField);
                    EditorThemeManager.AddSelectable(inputYStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputYStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputYStorage.leftGreaterButton.gameObject);
                    Destroy(inputYStorage.rightGreaterButton.gameObject);
                }

                // Rotation
                {
                    var gameObject = GenerateUIPart("Rotation", Tab.Custom, ValueType.Float);

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                }

                // Depth
                {
                    var gameObject = GenerateUIPart("Depth", Tab.Custom, ValueType.Float);

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                }

                // Color
                {
                    var gameObject = GenerateUIPart("Color", Tab.Custom, ValueType.Color);

                    var layout = Creator.NewUIObject("colors", gameObject.transform);
                    layout.transform.AsRT().anchoredPosition = new Vector2(170f, -16f);
                    layout.transform.AsRT().sizeDelta = new Vector2(400f, 100f);
                    var layoutGLG = layout.AddComponent<GridLayoutGroup>();
                    layoutGLG.cellSize = new Vector2(32f, 32f);
                    layoutGLG.spacing = new Vector2(8f, 8f);

                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 162f);

                    for (int j = 0; j < 25; j++)
                    {
                        var color = EditorManager.inst.colorGUI.Duplicate(layout.transform, $"{j + 1}");
                        EditorThemeManager.AddGraphic(color.GetComponent<Image>(), ThemeGroup.Null, true);
                        EditorThemeManager.AddGraphic(color.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Background_1);
                    }
                }

                // Custom Color
                {
                    var gameObject = GenerateUIPart("Custom Color", Tab.Custom, ValueType.String);

                    var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

                    EditorThemeManager.AddInputField(input.GetComponent<InputField>());
                }

                // Opacity
                {
                    var gameObject = GenerateUIPart("Opacity", Tab.Custom, ValueType.Float);

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                }

                // Parent
                {
                    var gameObject = GenerateUIPart("Parent", Tab.Custom, ValueType.Enum);

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    RectValues.Default.AnchoredPosition(212f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());
                    dropdown.options.Clear();
                    dropdown.onValueChanged.ClearAll();
                    dropdown.options = CoreHelper.StringToOptionData("Head", "Boost", "Boost Tail", "Tail 1", "Tail 2", "Tail 3", "Face");
                    dropdown.GetComponent<HideDropdownOptions>().DisabledOptions.Clear();

                    EditorThemeManager.AddDropdown(dropdown);
                }

                // Custom Parent
                {
                    var gameObject = GenerateUIPart("Custom Parent", Tab.Custom, ValueType.String);

                    var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

                    EditorThemeManager.AddInputField(input.GetComponent<InputField>());
                }

                // Position Offset
                {
                    var gameObject = GenerateUIPart("Position Offset", Tab.Custom, ValueType.Float);

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                }

                // Scale Offset
                {
                    var gameObject = GenerateUIPart("Scale Offset", Tab.Custom, ValueType.Float);

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                }

                // Rotation Offset
                {
                    var gameObject = GenerateUIPart("Rotation Offset", Tab.Custom, ValueType.Float);

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(214f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                }

                // Scale Parent
                {
                    var gameObject = GenerateUIPart("Scale Parent", Tab.Custom, ValueType.Bool);

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                // Rotation Parent
                {
                    var gameObject = GenerateUIPart("Rotation Parent", Tab.Custom, ValueType.Bool);

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                // Shape
                {
                    var gameObject = GenerateUIPart("Shape", Tab.Custom, ValueType.Enum);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 92f);

                    var shape = ObjEditor.inst.ObjectView.transform.Find("shape").gameObject.Duplicate(gameObject.transform, "shape");
                    UIManager.SetRectTransform(shape.transform.AsRT(), new Vector2(568f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(351f, 32f));
                    var shapeSettings = ObjEditor.inst.ObjectView.transform.Find("shapesettings").gameObject.Duplicate(gameObject.transform, "shapesettings");
                    UIManager.SetRectTransform(shapeSettings.transform.AsRT(), new Vector2(568f, -54f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(351f, 32f));

                    var text = shapeSettings.transform.Find("5");
                    var textLayoutElement = text.gameObject.AddComponent<LayoutElement>();
                    textLayoutElement.ignoreLayout = true;
                    UIManager.SetRectTransform(text.AsRT(), new Vector2(0f, -24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(351f, 74f));
                }

                // Text
                // Require All
                {
                    var gameObject = GenerateUIPart("Require All", Tab.Custom, ValueType.Bool);

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                // Visibility
                {
                    var gameObject = GenerateUIPart("Visibility", Tab.Custom, ValueType.Bool);

                    var visibilityScrollRect = Creator.NewUIObject("ScrollRect", gameObject.transform);
                    visibilityScrollRect.transform.localScale = Vector3.one;
                    UIManager.SetRectTransform(visibilityScrollRect.transform.AsRT(), new Vector2(64f, 0f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(600f, 0f));
                    var visibilityScrollRectSR = visibilityScrollRect.AddComponent<ScrollRect>();

                    var visibilityMaskGO = Creator.NewUIObject("Mask", visibilityScrollRect.transform);
                    visibilityMaskGO.transform.localScale = Vector3.one;
                    visibilityMaskGO.transform.AsRT().anchoredPosition = new Vector2(0f, 0f);
                    visibilityMaskGO.transform.AsRT().anchorMax = new Vector2(1f, 1f);
                    visibilityMaskGO.transform.AsRT().anchorMin = new Vector2(0f, 0f);
                    visibilityMaskGO.transform.AsRT().sizeDelta = new Vector2(0f, 0f);
                    var visibilityMaskImage = visibilityMaskGO.AddComponent<Image>();
                    visibilityMaskImage.color = new Color(1f, 1f, 1f, 0.04f);
                    visibilityMaskGO.AddComponent<Mask>();

                    var visibilityContentGO = Creator.NewUIObject("Content", visibilityMaskGO.transform);
                    visibilityContentGO.transform.localScale = Vector3.one;
                    UIManager.SetRectTransform(visibilityContentGO.transform.AsRT(), new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, 250f));

                    var visibilityContentCSF = visibilityContentGO.AddComponent<ContentSizeFitter>();
                    visibilityContentCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    visibilityContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

                    var visibilityContentVLG = visibilityContentGO.AddComponent<VerticalLayoutGroup>();
                    visibilityContentVLG.childControlHeight = false;
                    visibilityContentVLG.childForceExpandHeight = false;
                    visibilityContentVLG.spacing = 4f;

                    var visibilityContentLE = visibilityContentGO.AddComponent<LayoutElement>();
                    visibilityContentLE.layoutPriority = 10000;
                    visibilityContentLE.minWidth = 349;

                    visibilityScrollRectSR.content = visibilityContentGO.transform.AsRT();
                }
            }

            // todo: figure out how adding / removing tail model elements works with the health system. maybe consider also reworking the UI a little too?
            // Misc
            {
                // Add Tail
                //{
                //    var gameObject = Creator.NewUIObject("Add Tail", content);
                //    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                //    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                //    var labelText = label.GetComponent<Text>();
                //    labelText.text = name;
                //    EditorThemeManager.AddLightText(labelText);
                //    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                //    var image = gameObject.AddComponent<Image>();

                //    var button = gameObject.AddComponent<Button>();
                //    button.image = image;

                //    EditorThemeManager.AddSelectable(button, ThemeGroup.List_Button_1);

                //    editorUIs.Add(new PlayerEditorUI
                //    {
                //        Name = name,
                //        GameObject = gameObject,
                //        Tab = Tab.Tail,
                //        ValueType = ValueType.Function,
                //        Index = -1,
                //    });
                //}
            }

            // Functions
            {
                var spacer = Creator.NewUIObject("spacer", dialog.transform);
                spacer.transform.AsRT().sizeDelta = new Vector2(765f, 54f);

                var layout = Creator.NewUIObject("layout", spacer.transform);
                UIManager.SetRectTransform(layout.transform.AsRT(), new Vector2(8f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760f, 43.2f));
                var layoutHLG = layout.AddComponent<HorizontalLayoutGroup>();
                layoutHLG.childControlWidth = false;
                layoutHLG.childForceExpandWidth = false;
                layoutHLG.spacing = 2f;

                var select = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                select.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var selectStorage = select.GetComponent<FunctionButtonStorage>();
                selectStorage.label.fontSize = 16;
                selectStorage.label.text = "Select";
                selectStorage.button.onClick.ClearAll();
                selectStorage.button.onClick.AddListener(() =>
                {
                    RTEditor.inst.PlayerModelsPopup.Open();
                    StartCoroutine(RefreshModels());
                });

                EditorThemeManager.AddSelectable(selectStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(selectStorage.label, ThemeGroup.Function_2_Text);

                var playerIndexObject = EditorPrefabHolder.Instance.Dropdown.Duplicate(layout.transform, "dropdown");
                playerIndexObject.transform.AsRT().sizeDelta = new Vector2(200f, 43.2f);
                var playerIndexDropdown = playerIndexObject.GetComponent<Dropdown>();
                playerIndexDropdown.onValueChanged.ClearAll();
                playerIndexDropdown.options = new List<Dropdown.OptionData>();
                for (int i = 1; i <= 4; i++)
                    playerIndexDropdown.options.Add(new Dropdown.OptionData($"Player {i}"));
                playerIndexDropdown.value = 0;
                playerIndexDropdown.onValueChanged.AddListener(_val =>
                {
                    playerModelIndex = _val;
                    StartCoroutine(RefreshEditor());
                });

                EditorThemeManager.AddDropdown(playerIndexDropdown);

                var create = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                create.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var createStorage = create.GetComponent<FunctionButtonStorage>();
                createStorage.label.fontSize = 16;
                createStorage.label.text = "Create";
                createStorage.button.onClick.ClearAll();
                createStorage.button.onClick.AddListener(CreateNewModel);

                EditorThemeManager.AddSelectable(createStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(createStorage.label, ThemeGroup.Function_2_Text);

                var save = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                save.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var saveStorage = save.GetComponent<FunctionButtonStorage>();
                saveStorage.label.fontSize = 16;
                saveStorage.label.text = "Save";
                saveStorage.button.onClick.ClearAll();
                saveStorage.button.onClick.AddListener(Save);

                EditorThemeManager.AddSelectable(saveStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(saveStorage.label, ThemeGroup.Function_2_Text);

                var load = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                load.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var loadStorage = load.GetComponent<FunctionButtonStorage>();
                loadStorage.label.fontSize = 16;
                loadStorage.label.text = "Reload";
                loadStorage.button.onClick.ClearAll();
                loadStorage.button.onClick.AddListener(Reload);

                EditorThemeManager.AddSelectable(loadStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(loadStorage.label, ThemeGroup.Function_2_Text);

                var setToGlobal = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                setToGlobal.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var setToGlobalStorage = setToGlobal.GetComponent<FunctionButtonStorage>();
                setToGlobalStorage.label.fontSize = 16;
                setToGlobalStorage.label.text = "Set to Global";
                setToGlobalStorage.button.onClick.ClearAll();
                setToGlobalStorage.button.onClick.AddListener(() => PlayerManager.PlayerIndexes[playerModelIndex].Value = PlayersData.Current.playerModelsIndex[playerModelIndex]);

                EditorThemeManager.AddSelectable(setToGlobalStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(setToGlobalStorage.label, ThemeGroup.Function_2_Text);
            }

            LSHelpers.SetActiveChildren(content, false);

            RTEditor.inst.PlayerModelsPopup = RTEditor.inst.GeneratePopup(EditorPopup.PLAYER_MODELS_POPUP, "Player Models", Vector2.zero, Vector2.zero, _val =>
            {
                modelSearchTerm = _val;
                StartCoroutine(RefreshModels());
            });


            try
            {
                Dialog = new PlayerEditorDialog();
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            yield break;
        }

        public GameObject GenerateUIPart(string name, Tab tab, ValueType valueType, int index = -1)
        {
            var gameObject = Creator.NewUIObject(name, content);
            gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

            var label = labelPrefab.Duplicate(gameObject.transform, "label");
            var labelText = label.GetComponent<Text>();
            labelText.text = name;
            UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));
            EditorThemeManager.AddLightText(labelText);

            editorUIs.Add(new PlayerEditorUI
            {
                Name = name,
                GameObject = gameObject,
                Tab = tab,
                ValueType = valueType,
                Index = index,
            });

            return gameObject;
        }

        public IEnumerator RefreshEditor()
        {
            var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

            var isDefault = currentModel.IsDefault;
            content.Find("handler").gameObject.SetActive(isDefault && CurrentTab != Tab.Global);

            for (int i = 0; i < editorUIs.Count; i++)
            {
                var ui = editorUIs[i];
                var active = (!isDefault || ui.Name == "Base ID" || ui.Tab == CurrentTab && CurrentTab == Tab.Global) && RTString.SearchString(searchTerm, ui.Name) && ui.Tab == CurrentTab;
                ui.GameObject?.SetActive(active);
                if (!active)
                    continue;

                if (ui.Tab == Tab.Global)
                {
                    switch (ui.Name)
                    {
                        case "Global Speed":
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = GameData.Current.data.level.speedMultiplier.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        GameData.Current.data.level.speedMultiplier = result;
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                                break;
                            }
                        case "Global Lock Boost":
                            {
                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = GameData.Current.data.level.lockBoost;
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.lockBoost = _val;
                                    RTPlayer.SetGameDataProperties();
                                });

                                break;
                            }
                        case "Global Gamemode":
                            {
                                var dropdown = ui.GameObject.transform.Find("dropdown").GetComponent<Dropdown>();
                                dropdown.onValueChanged.ClearAll();
                                dropdown.value = GameData.Current.data.level.gameMode;
                                dropdown.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.gameMode = _val;
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggers(dropdown.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
                                {
                                    if (!EditorConfig.Instance.ScrollOnEasing.Value)
                                        return;

                                    var pointerEventData = (PointerEventData)baseEventData;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        dropdown.value = dropdown.value == 0 ? dropdown.options.Count - 1 : dropdown.value - 1;
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        dropdown.value = dropdown.value == dropdown.options.Count - 1 ? 0 : dropdown.value + 1;
                                }));

                                break;
                            }
                        case "Global Max Jump Count":
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = GameData.Current.data.level.maxJumpCount.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        GameData.Current.data.level.maxJumpCount = result;
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                                break;
                            }
                        case "Global Max Jump Boost Count":
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = GameData.Current.data.level.maxJumpBoostCount.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        GameData.Current.data.level.maxJumpBoostCount = result;
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                                break;
                            }
                        case "Global Jump Gravity":
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = GameData.Current.data.level.jumpGravity.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        GameData.Current.data.level.jumpGravity = result;
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                                break;
                            }
                        case "Global Jump Intensity":
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = GameData.Current.data.level.jumpIntensity.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        GameData.Current.data.level.jumpIntensity = result;
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                                break;
                            }
                        case "Global Max Health":
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = GameData.Current.data.level.maxHealth.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        GameData.Current.data.level.maxHealth = Mathf.Clamp(result, 3, int.MaxValue);
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage, min: 3, max: int.MaxValue);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField, min: 3, max: int.MaxValue));

                                break;
                            }
                        case "Spawn Players":
                            {
                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = GameData.Current.data.level.spawnPlayers;
                                toggle.onValueChanged.AddListener(_val => GameData.Current.data.level.spawnPlayers = _val);

                                break;
                            }
                        case "Allow Custom Player Models":
                            {
                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = GameData.Current.data.level.allowCustomPlayerModels;
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.allowCustomPlayerModels = _val;
                                    RTPlayer.SetGameDataProperties();
                                });

                                break;
                            }
                        case "Limit Player":
                            {
                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = GameData.Current.data.level.limitPlayer;
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitPlayer = _val;
                                    RTPlayer.SetGameDataProperties();
                                });

                                break;
                            }
                        case "Limit Move Speed":
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = GameData.Current.data.level.limitMoveSpeed.x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitMoveSpeed.x = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = GameData.Current.data.level.limitMoveSpeed.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitMoveSpeed.y = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);


                                break;
                            }
                        case "Limit Boost Speed":
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = GameData.Current.data.level.limitBoostSpeed.x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostSpeed.x = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = GameData.Current.data.level.limitBoostSpeed.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostSpeed.y = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);


                                break;
                            }
                        case "Limit Boost Cooldown":
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = GameData.Current.data.level.limitBoostCooldown.x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostCooldown.x = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = GameData.Current.data.level.limitBoostCooldown.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostCooldown.y = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);


                                break;
                            }
                        case "Limit Boost Min Time":
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = GameData.Current.data.level.limitBoostMinTime.x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostMinTime.x = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = GameData.Current.data.level.limitBoostMinTime.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostMinTime.y = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);


                                break;
                            }
                        case "Limit Boost Max Time":
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = GameData.Current.data.level.limitBoostMaxTime.x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostMaxTime.x = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = GameData.Current.data.level.limitBoostMaxTime.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitBoostMaxTime.y = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);


                                break;
                            }
                        case "Limit Hit Cooldown":
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = GameData.Current.data.level.limitHitCooldown.x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitHitCooldown.x = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = GameData.Current.data.level.limitHitCooldown.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    GameData.Current.data.level.limitHitCooldown.y = Parser.TryParse(_val, 0f);
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);


                                break;
                            }

                        case "Respawn Players":
                            {
                                var button = ui.GameObject.GetComponent<Button>();
                                button.onClick.ClearAll();
                                button.onClick.AddListener(PlayerManager.RespawnPlayers);

                                break;
                            }
                        case "Update Properties":
                            {
                                var button = ui.GameObject.GetComponent<Button>();
                                button.onClick.ClearAll();
                                button.onClick.AddListener(RTPlayer.SetGameDataProperties);

                                break;
                            }
                    }
                    continue;
                }

                if (ui.Name == "Custom Objects")
                {
                    var button = ui.GameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() =>
                    {
                        RTEditor.inst.PlayerModelsPopup.Open();
                        StartCoroutine(RefreshCustomObjects());
                    });

                    continue;
                }

                if (ui.Tab == Tab.Custom)
                {
                    PlayerModel.CustomObject customObject = null;
                    var customActive = active && !string.IsNullOrEmpty(CustomObjectID) && currentModel.customObjects.TryFind(x => x.id == CustomObjectID, out customObject);
                    ui.GameObject?.SetActive(customActive);
                    if (!customActive)
                        continue;

                    ui.Reference = customObject;

                    switch (ui.Name)
                    {
                        case "ID":
                            {
                                var text = ui.GameObject.transform.Find("id").GetComponent<Text>();
                                text.alignment = TextAnchor.MiddleRight;
                                text.text = customObject.id + " (Click to copy)";
                                var button = ui.GameObject.GetComponent<Button>();
                                button.onClick.ClearAll();
                                button.onClick.AddListener(() =>
                                {
                                    LSText.CopyToClipboard(customObject.id);
                                    EditorManager.inst.DisplayNotification($"Copied ID \"{customObject.id}\" to clipboard!", 2f, EditorManager.NotificationType.Success);
                                });

                                break;
                            }
                        case "Name":
                        case "Custom Color":
                        case "Custom Parent":
                            {
                                var inputField = ui.GameObject.transform.Find("input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.text = ui.Name == "Name" ? customObject.name : ui.Name == "Custom Parent" ? customObject.customParent : customObject.customColor;
                                inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (ui.Name == "Name")
                                        customObject.name = _val;
                                    else if (ui.Name == "Custom Parent")
                                        customObject.customParent = _val;
                                    else
                                        customObject.customColor = _val;
                                });

                                break;
                            }
                        case "Rotation":
                        case "Depth":
                        case "Opacity":
                        case "Position Offset":
                        case "Scale Offset":
                        case "Rotation Offset":
                            {

                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = (ui.Name switch
                                {
                                    "Rotation" => customObject.rotation,
                                    "Depth" => customObject.depth,
                                    "Opacity" => customObject.opacity,
                                    "Position Offset" => customObject.positionOffset,
                                    "Scale Offset" => customObject.scaleOffset,
                                    _ => customObject.rotationOffset,
                                }).ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        if (ui.Name == "Rotation")
                                            customObject.rotation = result;
                                        if (ui.Name == "Depth")
                                            customObject.depth = result;
                                        if (ui.Name == "Opacity")
                                            customObject.opacity = result;
                                        if (ui.Name == "Position Offset")
                                            customObject.positionOffset = result;
                                        if (ui.Name == "Scale Offset")
                                            customObject.scaleOffset = result;
                                        if (ui.Name == "Rotation Offset")
                                            customObject.rotationOffset = result;

                                        PlayerManager.UpdatePlayerModels();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                                break;
                            }
                        case "Position":
                        case "Scale":
                            {
                                var isPosition = ui.Name == "Position";
                                var vector = isPosition ? customObject.position : customObject.scale;

                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = vector.x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    var vector = ui.Name == "Position" ? customObject.position : customObject.scale;

                                    if (float.TryParse(_val, out float result))
                                    {
                                        if (isPosition)
                                            customObject.position = new Vector2(result, vector.y);
                                        else
                                            customObject.scale = new Vector2(result, vector.y);
                                        
                                        PlayerManager.UpdatePlayerModels();
                                    }
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = vector.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    var vector = ui.Name == "Position" ? customObject.position : customObject.scale;

                                    if (float.TryParse(_val, out float result))
                                    {
                                        if (isPosition)
                                            customObject.position = new Vector2(vector.x, result);
                                        else
                                            customObject.scale = new Vector2(vector.x, result);

                                        PlayerManager.UpdatePlayerModels();
                                    }
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);

                                break;
                            }
                        case "Color":
                            {
                                var colors = ui.GameObject.transform.Find("colors");

                                for (int j = 0; j < colors.childCount; j++)
                                {
                                    var colorIndex = j;
                                    var color = colors.GetChild(j);
                                    color.GetChild(0).gameObject.SetActive(customObject.color == j);
                                    color.GetComponent<Image>().color = RTColors.GetPlayerColor(playerModelIndex, j, 1f, customObject.customColor);

                                    var button = color.GetComponent<Button>();
                                    button.onClick.ClearAll();
                                    button.onClick.AddListener(() =>
                                    {
                                        customObject.color = colorIndex;
                                        StartCoroutine(RefreshEditor());
                                    });
                                }

                                break;
                            }
                        case "Parent":
                            {
                                var dropdown = ui.GameObject.transform.Find("dropdown").GetComponent<Dropdown>();
                                dropdown.onValueChanged.ClearAll();
                                dropdown.value = customObject.parent;
                                dropdown.onValueChanged.AddListener(_val =>
                                {
                                    customObject.parent = _val;

                                    PlayerManager.UpdatePlayerModels();
                                });

                                TriggerHelper.AddEventTriggers(dropdown.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
                                {
                                    if (!EditorConfig.Instance.ScrollOnEasing.Value)
                                        return;

                                    var pointerEventData = (PointerEventData)baseEventData;
                                    if (pointerEventData.scrollDelta.y > 0f)
                                        dropdown.value = dropdown.value == 0 ? dropdown.options.Count - 1 : dropdown.value - 1;
                                    if (pointerEventData.scrollDelta.y < 0f)
                                        dropdown.value = dropdown.value == dropdown.options.Count - 1 ? 0 : dropdown.value + 1;
                                }));

                                break;
                            }
                        case "Scale Parent":
                        case "Rotation Parent":
                        case "Require All":
                            {
                                var isScale = ui.Name == "Scale Parent";
                                var isRequireAll = ui.Name == "Require All";

                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = isScale ? customObject.scaleParent : isRequireAll ? customObject.requireAll : customObject.rotationParent;
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    if (isScale)
                                        customObject.scaleParent = _val;
                                    else if (isRequireAll)
                                        customObject.requireAll = _val;
                                    else
                                        customObject.rotationParent = _val;

                                    PlayerManager.UpdatePlayerModels();
                                });

                                break;
                            }
                        case "Shape":
                            {
                                RenderShape(ui);
                                break;
                            }
                        case "Visibility":
                            {
                                ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 32f * (customObject.visibilitySettings.Count + 1));
                                LayoutRebuilder.ForceRebuildLayoutImmediate(ui.GameObject.transform.AsRT());
                                LayoutRebuilder.ForceRebuildLayoutImmediate(ui.GameObject.transform.parent.AsRT());

                                var content = ui.GameObject.transform.Find("ScrollRect/Mask/Content");
                                LSHelpers.DeleteChildren(content);

                                var add = PrefabEditor.inst.CreatePrefab.Duplicate(content, "Add");
                                var addText = add.transform.Find("Text").GetComponent<Text>();
                                addText.text = "Add Visiblity Setting";
                                ((RectTransform)add.transform).sizeDelta = new Vector2(760f, 32f);
                                var addButton = add.GetComponent<Button>();
                                addButton.onClick.ClearAll();
                                addButton.onClick.AddListener(() =>
                                {
                                    var newVisibility = new PlayerModel.CustomObject.Visiblity();
                                    newVisibility.command = IntToVisibility(0);
                                    customObject.visibilitySettings.Add(newVisibility);
                                    StartCoroutine(RefreshEditor());
                                });
                                EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add);
                                EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

                                int num = 0;
                                foreach (var visibility in customObject.visibilitySettings)
                                {
                                    int index = num;
                                    var bar = Creator.NewUIObject($"Visiblity {num}", content);
                                    bar.transform.AsRT().sizeDelta = new Vector2(500f, 32f);
                                    bar.AddComponent<HorizontalLayoutGroup>().spacing = 4;
                                    bar.transform.localScale = Vector3.one;

                                    var image = bar.AddComponent<Image>();
                                    image.color = new Color(1f, 1f, 1f, 0.03f);

                                    var toggle = EditorPrefabHolder.Instance.Function2Button.Duplicate(bar.transform, "not").GetComponent<FunctionButtonStorage>();

                                    toggle.button.onClick.ClearAll();
                                    toggle.label.text = $"Not: {(visibility.not ? "Yes" : "No")}";
                                    toggle.button.onClick.AddListener(() =>
                                    {
                                        visibility.not = !visibility.not;
                                        toggle.label.text = $"Not: {(visibility.not ? "Yes" : "No")}";
                                    });
                                    EditorThemeManager.ApplySelectable(toggle.button, ThemeGroup.Function_2);
                                    EditorThemeManager.ApplyGraphic(toggle.label, ThemeGroup.Function_2_Text);

                                    var x = EditorPrefabHolder.Instance.Dropdown.Duplicate(bar.transform);
                                    x.transform.SetParent(bar.transform);
                                    x.transform.localScale = Vector3.one;

                                    Destroy(x.GetComponent<HoverTooltip>());
                                    Destroy(x.GetComponent<HideDropdownOptions>());
                                    var layoutElement = x.GetComponent<LayoutElement>();
                                    layoutElement.minWidth = 200f;
                                    layoutElement.preferredWidth = 400f;

                                    var dropdown = x.GetComponent<Dropdown>();
                                    dropdown.template.sizeDelta = new Vector2(120f, 192f);
                                    dropdown.onValueChanged.ClearAll();
                                    dropdown.options = CoreHelper.StringToOptionData("Is Boosting", "Is Taking Hit", "Is Zen Mode", "Is Health Percentage Greater", "Is Health Greater Equals", "Is Health Equals", "Is Health Greater", "Is Pressing Key");
                                    dropdown.value = VisibilityToInt(visibility.command);
                                    dropdown.onValueChanged.AddListener(_val => visibility.command = IntToVisibility(_val));
                                    EditorThemeManager.ApplyDropdown(dropdown);

                                    // Value
                                    {
                                        var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(bar.transform, "input");
                                        var valueStorage = value.GetComponent<InputFieldStorage>();

                                        valueStorage.inputField.onValueChanged.ClearAll();
                                        valueStorage.inputField.text = visibility.value.ToString();
                                        valueStorage.inputField.onValueChanged.AddListener(_val =>
                                        {
                                            if (float.TryParse(_val, out float result))
                                                visibility.value = result;
                                        });

                                        DestroyImmediate(valueStorage.leftGreaterButton.gameObject);
                                        DestroyImmediate(valueStorage.middleButton.gameObject);
                                        DestroyImmediate(valueStorage.rightGreaterButton.gameObject);

                                        TriggerHelper.AddEventTriggers(value, TriggerHelper.ScrollDelta(valueStorage.inputField));
                                        TriggerHelper.IncreaseDecreaseButtons(valueStorage);
                                        EditorThemeManager.ApplyInputField(valueStorage.inputField);
                                        EditorThemeManager.ApplySelectable(valueStorage.leftButton, ThemeGroup.Function_2, false);
                                        EditorThemeManager.ApplySelectable(valueStorage.rightButton, ThemeGroup.Function_2, false);
                                    }

                                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(bar.transform, "delete");

                                    delete.transform.AsRT().anchoredPosition = new Vector2(-5f, 0f);

                                    delete.GetComponent<LayoutElement>().ignoreLayout = false;

                                    var deleteButton = delete.GetComponent<DeleteButtonStorage>();
                                    deleteButton.button.onClick.ClearAll();
                                    deleteButton.button.onClick.AddListener(() =>
                                    {
                                        customObject.visibilitySettings.RemoveAt(index);
                                        StartCoroutine(RefreshEditor());
                                    });
                                    EditorThemeManager.ApplyGraphic(deleteButton.baseImage, ThemeGroup.Delete, true);
                                    EditorThemeManager.ApplyGraphic(deleteButton.image, ThemeGroup.Delete_Text);

                                    num++;
                                }

                                break;
                            }
                    }
                    continue;
                }

                try
                {
                    var value = currentModel[ui.Index];
                    var key = PlayerModel.Values[ui.Index];

                    switch (ui.Tab)
                    {
                        case Tab.Base:
                            {
                                ui.Reference = currentModel.basePart;

                                break;
                            }
                        case Tab.Head:
                            {
                                ui.Reference = currentModel.headPart;

                                break;
                            }
                        case Tab.Boost:
                            {
                                ui.Reference = currentModel.boostPart;

                                break;
                            }
                        case Tab.Spawners:
                            {
                                if (ui.Name.Contains("Pulse"))
                                    ui.Reference = currentModel.pulsePart;
                                if (ui.Name.Contains("Bullet"))
                                    ui.Reference = currentModel.bulletPart;

                                break;
                            }
                        case Tab.Tail:
                            {
                                if (ui.Name.Contains("Base"))
                                    ui.Reference = currentModel.tailBase;
                                if (ui.Name.Contains("Boost"))
                                    ui.Reference = currentModel.boostTailPart;
                                if (ui.Name.Contains("Tail 1"))
                                    ui.Reference = currentModel.tailParts[0];
                                if (ui.Name.Contains("Tail 2"))
                                    ui.Reference = currentModel.tailParts[1];
                                if (ui.Name.Contains("Tail 3"))
                                    ui.Reference = currentModel.tailParts[2];

                                break;
                            }
                    }

                    if (key == "Base ID")
                    {
                        var text = ui.GameObject.transform.Find("id").GetComponent<Text>();
                        text.alignment = TextAnchor.MiddleRight;
                        text.text = value.ToString() + " (Click to copy)";
                        var button = ui.GameObject.GetComponent<Button>();
                        button.onClick.ClearAll();
                        button.onClick.AddListener(() =>
                        {
                            LSText.CopyToClipboard(value.ToString());
                            EditorManager.inst.DisplayNotification($"Copied ID \"{value}\" to clipboard!", 2f, EditorManager.NotificationType.Success);
                        });

                        continue;
                    }

                    if (key.Contains("Shape"))
                    {
                        RenderShape(ui);

                        continue;
                    }
                    
                    //if (key == "Add Tail")
                    //{
                    //    //currentModel.AddTail();

                    //    break;
                    //}

                    switch (ui.ValueType)
                    {
                        case ValueType.Bool:
                            {
                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = (bool)value;
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    currentModel[key] = _val;
                                    PlayerManager.UpdatePlayerModels();
                                });

                                break;
                            }
                        case ValueType.String:
                            {
                                var inputField = ui.GameObject.transform.Find("input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.text = value.ToString();
                                inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (!(key.Contains("Color") && key.Contains("Custom")))
                                        currentModel[key] = _val;
                                    else
                                        currentModel[key] = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(LSColors.pink500);

                                    PlayerManager.UpdatePlayerModels();
                                });

                                break;
                            }
                        case ValueType.Int:
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = value.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        currentModel[key] = result;
                                        PlayerManager.UpdatePlayerModels();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                                break;
                            }
                        case ValueType.Float:
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = value.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        currentModel[key] = result;
                                        PlayerManager.UpdatePlayerModels();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                                break;
                            }
                        case ValueType.Vector2:
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = ((Vector2)value).x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        var value = currentModel[ui.Index];
                                        currentModel[key] = new Vector2(result, ((Vector2)value).y);
                                        PlayerManager.UpdatePlayerModels();
                                    }
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = ((Vector2)value).y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(_val =>
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        var value = currentModel[ui.Index];
                                        currentModel[key] = new Vector2(((Vector2)value).x, result);
                                        PlayerManager.UpdatePlayerModels();
                                    }
                                });

                                TriggerHelper.AddEventTriggers(inputXStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggers(inputYStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);

                                break;
                            }
                        case ValueType.Color:
                            {
                                var colors = ui.GameObject.transform.Find("colors");

                                for (int j = 0; j < colors.childCount; j++)
                                {
                                    var colorIndex = j;
                                    var color = colors.GetChild(j);
                                    color.GetChild(0).gameObject.SetActive((int)value == j);
                                    color.GetComponent<Image>().color = RTColors.GetPlayerColor(playerModelIndex, j, 1f, "FFFFFF");

                                    var button = color.GetComponent<Button>();
                                    button.onClick.ClearAll();
                                    button.onClick.AddListener(() =>
                                    {
                                        currentModel[key] = colorIndex;
                                        StartCoroutine(RefreshEditor());
                                    });
                                }

                                break;
                            }
                        case ValueType.Enum:
                            {
                                var dropdown = ui.GameObject.transform.Find("dropdown").GetComponent<Dropdown>();
                                dropdown.onValueChanged.ClearAll();
                                dropdown.value = (int)value;
                                dropdown.onValueChanged.AddListener(_val =>
                                {
                                    currentModel[key] = _val;
                                    PlayerManager.UpdatePlayerModels();
                                });

                                TriggerHelper.AddEventTriggers(dropdown.gameObject, TriggerHelper.ScrollDelta(dropdown));

                                break;
                            }
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Exception: {ex}");
                }
            }

            yield break;
        }

        public IEnumerator RefreshModels(Action<PlayerModel> onSelect = null)
        {
            RTEditor.inst.PlayerModelsPopup.ClearContent();
            RTEditor.inst.PlayerModelsPopup.SearchField.onValueChanged.NewListener(_val =>
            {
                modelSearchTerm = _val;
                StartCoroutine(RefreshModels(onSelect));
            });

            int num = 0;
            foreach (var playerModel in PlayersData.externalPlayerModels)
            {
                int index = num;
                var name = playerModel.Value.basePart.name;
                if (!RTString.SearchString(modelSearchTerm, name))
                {
                    num++;
                    continue;
                }

                var model = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(RTEditor.inst.PlayerModelsPopup.Content, name);
                var modelButton = model.GetComponent<Button>();
                modelButton.onClick.NewListener(() =>
                {
                    if (onSelect != null)
                    {
                        onSelect.Invoke(playerModel.Value);
                        return;
                    }

                    PlayersData.Current.playerModels[playerModel.Key] = playerModel.Value;
                    PlayersData.Current.SetPlayerModel(playerModelIndex, playerModel.Key);
                    PlayerManager.RespawnPlayers();
                    StartCoroutine(RefreshEditor());
                });

                var modelContextMenu = model.AddComponent<ContextClickable>();
                modelContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Open & Use", () =>
                        {
                            PlayersData.Current.SetPlayerModel(playerModelIndex, playerModel.Key);
                            PlayerManager.RespawnPlayers();
                            StartCoroutine(RefreshEditor());
                        }),
                        new ButtonFunction("Set to Global", () => PlayerManager.PlayerIndexes[playerModelIndex].Value = playerModel.Key),
                        new ButtonFunction("Create New", CreateNewModel),
                        new ButtonFunction("Save", Save),
                        new ButtonFunction("Reload", Reload),
                        new ButtonFunction(true),
                        new ButtonFunction("Duplicate", () =>
                        {
                            var dup = PlayersData.Current.DuplicatePlayerModel(playerModel.Key);
                            PlayersData.externalPlayerModels[dup.basePart.id] = dup;
                            if (dup)
                                PlayersData.Current.SetPlayerModel(playerModelIndex, dup.basePart.id);
                        }),
                        new ButtonFunction("Delete", () =>
                        {
                            if (index < 5)
                            {
                                EditorManager.inst.DisplayNotification($"Cannot delete a default player model.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this Player Model?", () =>
                            {
                                PlayersData.Current.SetPlayerModel(playerModelIndex, PlayerModel.DEFAULT_ID);
                                PlayersData.externalPlayerModels.Remove(playerModel.Key);
                                PlayersData.Current.playerModels.Remove(playerModel.Key);
                                PlayerManager.RespawnPlayers();
                                StartCoroutine(RefreshEditor());
                                StartCoroutine(RefreshModels(onSelect));

                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup);
                        })
                        );
                };

                var text = model.transform.GetChild(0).GetComponent<Text>();
                text.text = name;

                var image = model.transform.Find("Image").GetComponent<Image>();
                image.sprite = EditorSprites.PlayerSprite;

                EditorThemeManager.ApplySelectable(modelButton, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Light_Text);
                EditorThemeManager.ApplyLightText(text);

                if (index < 5)
                {
                    num++;
                    continue;
                }

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(model.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(280f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this Player Model?", () =>
                    {
                        PlayersData.Current.SetPlayerModel(playerModelIndex, PlayerModel.DEFAULT_ID);
                        PlayersData.externalPlayerModels.Remove(playerModel.Key);
                        PlayersData.Current.playerModels.Remove(playerModel.Key);
                        PlayerManager.RespawnPlayers();
                        StartCoroutine(RefreshEditor());
                        StartCoroutine(RefreshModels(onSelect));


                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup);
                });

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }

            yield break;
        }

        public IEnumerator RefreshCustomObjects()
        {
            RTEditor.inst.PlayerModelsPopup.ClearContent();

            var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

            var isDefault = PlayerModel.DefaultModels.Any(x => currentModel.basePart.id == x.basePart.id);

            if (isDefault)
                yield break;

            var createNew = PrefabEditor.inst.CreatePrefab.Duplicate(RTEditor.inst.PlayerModelsPopup.Content, "Create");
            var createNewButton = createNew.GetComponent<Button>();
            createNewButton.onClick.ClearAll();
            createNewButton.onClick.AddListener(() =>
            {
                var customObject = new PlayerModel.CustomObject(currentModel);
                var id = LSText.randomNumString(16);
                customObject.id = id;
                currentModel.customObjects.Add(customObject);

                CustomObjectID = id;

                StartCoroutine(RefreshCustomObjects());
                StartCoroutine(RefreshEditor());
                PlayerManager.UpdatePlayerModels();
            });

            var createNewText = createNew.transform.GetChild(0).GetComponent<Text>();
            createNewText.text = "Create custom object";

            EditorThemeManager.ApplyGraphic(createNewButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(createNewText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var customObject in currentModel.customObjects)
            {
                int index = num;
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.PlayerModelsPopup.Content, customObject.name);
                var folderButtonFunction = gameObject.AddComponent<FolderButtonFunction>();
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderButtonStorage.label.text = customObject.name;
                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorage.label);

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                folderButtonFunction.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Open", () =>
                            {
                                CustomObjectID = customObject.id;
                                StartCoroutine(RefreshEditor());
                            }),
                            new ButtonFunction("Delete", () =>
                            {
                                RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this custom object?", () =>
                                {
                                    currentModel.customObjects.RemoveAll(x => x.id == CustomObjectID);
                                    StartCoroutine(RefreshCustomObjects());
                                    StartCoroutine(RefreshEditor());
                                    PlayerManager.UpdatePlayerModels();
                                    RTEditor.inst.HideWarningPopup();
                                }, RTEditor.inst.HideWarningPopup);
                            }),
                            new ButtonFunction("Duplicate", () =>
                            {
                                var duplicateObject = PlayerModel.CustomObject.DeepCopy(currentModel, customObject);
                                while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                                    duplicateObject.id = LSText.randomNumString(16);

                                var id = duplicateObject.id;
                                currentModel.customObjects.Add(duplicateObject);

                                CustomObjectID = id;

                                StartCoroutine(RefreshCustomObjects());
                                StartCoroutine(RefreshEditor());
                                PlayerManager.UpdatePlayerModels();
                            }),
                            new ButtonFunction("Copy", () =>
                            {
                                copiedCustomObject = PlayerModel.CustomObject.DeepCopy(currentModel, customObject, false);
                                EditorManager.inst.DisplayNotification("Copied custom player object!", 2f, EditorManager.NotificationType.Success);
                            }),
                            new ButtonFunction("Paste", () =>
                            {
                                if (!copiedCustomObject)
                                {
                                    EditorManager.inst.DisplayNotification("No copied object yet.", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                var duplicateObject = PlayerModel.CustomObject.DeepCopy(currentModel, copiedCustomObject);
                                while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                                    duplicateObject.id = LSText.randomNumString(16);

                                var id = duplicateObject.id;
                                currentModel.customObjects.Add(duplicateObject);

                                CustomObjectID = id;

                                StartCoroutine(RefreshCustomObjects());
                                StartCoroutine(RefreshEditor());
                                PlayerManager.UpdatePlayerModels();
                                EditorManager.inst.DisplayNotification("Pasted custom player object!", 2f, EditorManager.NotificationType.Success);
                            })
                            );
                        return;
                    }

                    CustomObjectID = customObject.id;
                    StartCoroutine(RefreshEditor());
                    RTEditor.inst.PlayerModelsPopup.Close();
                };

                var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(gameObject.transform, "Delete");
                UIManager.SetRectTransform(delete.transform.AsRT(), new Vector2(280f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                var deleteStorage = delete.GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.ClearAll();
                deleteStorage.button.onClick.AddListener(() =>
                {
                    RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this custom object?", () =>
                    {
                        currentModel.customObjects.RemoveAll(x => x.id == CustomObjectID);
                        StartCoroutine(RefreshCustomObjects());
                        StartCoroutine(RefreshEditor());
                        PlayerManager.UpdatePlayerModels();
                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup);
                });
                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                var duplicate = EditorPrefabHolder.Instance.Function1Button.Duplicate(gameObject.transform, "Duplicate");
                UIManager.SetRectTransform(duplicate.transform.AsRT(), new Vector2(180f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120f, 32f));
                var duplicateStorage = duplicate.GetComponent<FunctionButtonStorage>();
                duplicateStorage.button.onClick.ClearAll();
                duplicateStorage.button.onClick.AddListener(() =>
                {
                    var duplicateObject = PlayerModel.CustomObject.DeepCopy(currentModel, customObject);
                    while (currentModel.customObjects.Has(x => x.id == duplicateObject.id)) // Ensure ID is not in list.
                        duplicateObject.id = LSText.randomNumString(16);

                    var id = duplicateObject.id;
                    currentModel.customObjects.Add(duplicateObject);

                    CustomObjectID = id;

                    StartCoroutine(RefreshCustomObjects());
                    StartCoroutine(RefreshEditor());
                    PlayerManager.UpdatePlayerModels();
                });

                duplicateStorage.label.text = "Duplicate";

                EditorThemeManager.ApplyGraphic(duplicateStorage.button.image, ThemeGroup.Paste, true);
                EditorThemeManager.ApplyGraphic(duplicateStorage.label, ThemeGroup.Paste_Text);

                num++;
            }

            yield break;
        }

        public PlayerModel.CustomObject copiedCustomObject;

        public void RenderShape(PlayerEditorUI ui)
        {
            PlayerModel.Generic generic = null;
            if (ui.Reference is PlayerModel.Generic genericAssign)
                generic = genericAssign;
            PlayerModel.Pulse pulse = null;
            if (ui.Reference is PlayerModel.Pulse pulseAssign)
                pulse = pulseAssign;
            PlayerModel.Bullet bullet = null;
            if (ui.Reference is PlayerModel.Bullet bulletAssign)
                bullet = bulletAssign;

            var shape = ui.GameObject.transform.Find("shape");
            var shapeSettings = ui.GameObject.transform.Find("shapesettings");

            shape.AsRT().sizeDelta = new Vector2(400f, 32);
            shapeSettings.AsRT().sizeDelta = new Vector2(400f, 32);

            var shapeGLG = shape.GetComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (!ui.updatedShapes)
            {
                // Initial removing
                DestroyImmediate(shape.GetComponent<ToggleGroup>());

                var toDestroy = new List<GameObject>();

                for (int j = 0; j < shape.childCount; j++)
                    toDestroy.Add(shape.GetChild(j).gameObject);

                for (int j = 0; j < shapeSettings.childCount; j++)
                {
                    if (j != 4 && j != 6)
                        for (int k = 0; k < shapeSettings.GetChild(j).childCount; k++)
                        {
                            toDestroy.Add(shapeSettings.GetChild(j).GetChild(k).gameObject);
                        }
                }

                foreach (var obj in toDestroy)
                    DestroyImmediate(obj);

                toDestroy = null;

                for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
                {
                    var shapeType = (ShapeType)i;
                    var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = ShapeManager.inst.Shapes2D[i].icon;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    if (!obj.GetComponent<HoverUI>())
                    {
                        var hoverUI = obj.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                    ui.shapeToggles.Add(shapeToggle);

                    ui.shapeOptionToggles.Add(new List<Toggle>());

                    if (shapeType != ShapeType.Text && shapeType != ShapeType.Image && shapeType != ShapeType.Polygon)
                    {
                        var so = shapeSettings.Find((i + 1).ToString());
                        if (!so)
                        {
                            so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                            CoreHelper.DestroyChildren(so);
                        }

                        var rect = so.AsRT();
                        if (!so.GetComponent<ScrollRect>())
                        {
                            var scroll = so.gameObject.AddComponent<ScrollRect>();
                            so.gameObject.AddComponent<Mask>();
                            var ad = so.gameObject.AddComponent<Image>();

                            scroll.horizontal = true;
                            scroll.vertical = false;
                            scroll.content = rect;
                            scroll.viewport = rect;
                            ad.color = new Color(1f, 1f, 1f, 0.01f);
                        }

                        for (int j = 0; j < ShapeManager.inst.Shapes2D[i].Count; j++)
                        {
                            var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                            if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            {
                                image1.sprite = ShapeManager.inst.Shapes2D[i][j].icon;
                                EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                            }

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var hoverUI = opt.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }

                            var shapeOptionToggle = opt.GetComponent<Toggle>();
                            EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);

                            ui.shapeOptionToggles[i].Add(shapeOptionToggle);

                            var layoutElement = opt.AddComponent<LayoutElement>();
                            layoutElement.layoutPriority = 1;
                            layoutElement.minWidth = 32f;

                            ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var he = opt.AddComponent<HoverUI>();
                                he.animatePos = false;
                                he.animateSca = true;
                                he.size = 1.1f;
                            }
                        }

                        ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
                    }

                    if (shapeType == ShapeType.Polygon)
                    {
                        var so = shapeSettings.Find((i + 1).ToString());

                        if (!so)
                        {
                            so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                            CoreHelper.DestroyChildren(so);
                        }

                        var rect = so.AsRT();
                        DestroyImmediate(so.GetComponent<ScrollRect>());
                        DestroyImmediate(so.GetComponent<HorizontalLayoutGroup>());

                        so.gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.05f);

                        var verticalLayoutGroup = so.gameObject.GetOrAddComponent<VerticalLayoutGroup>();
                        verticalLayoutGroup.spacing = 4f;

                        // Polygon Settings
                        {
                            #region Sides

                            var sides = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "sides");
                            var sidesStorage = sides.GetComponent<InputFieldStorage>();

                            Destroy(sidesStorage.addButton.gameObject);
                            Destroy(sidesStorage.subButton.gameObject);
                            Destroy(sidesStorage.leftGreaterButton.gameObject);
                            Destroy(sidesStorage.middleButton.gameObject);
                            Destroy(sidesStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(sidesStorage.inputField);
                            EditorThemeManager.AddSelectable(sidesStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(sidesStorage.rightButton, ThemeGroup.Function_2, false);

                            var sidesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(sides.transform, "label", 0);
                            var sidesLabelText = sidesLabel.GetComponent<Text>();
                            sidesLabelText.alignment = TextAnchor.MiddleLeft;
                            sidesLabelText.text = "Sides";
                            sidesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(sidesLabelText);
                            var sidesLabelLayout = sidesLabel.AddComponent<LayoutElement>();
                            sidesLabelLayout.minWidth = 100f;

                            #endregion

                            #region Roundness

                            var roundness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "roundness");
                            var roundnessStorage = roundness.GetComponent<InputFieldStorage>();

                            Destroy(roundnessStorage.addButton.gameObject);
                            Destroy(roundnessStorage.subButton.gameObject);
                            Destroy(roundnessStorage.leftGreaterButton.gameObject);
                            Destroy(roundnessStorage.middleButton.gameObject);
                            Destroy(roundnessStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(roundnessStorage.inputField);
                            EditorThemeManager.AddSelectable(roundnessStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(roundnessStorage.rightButton, ThemeGroup.Function_2, false);

                            var roundnessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(roundness.transform, "label", 0);
                            var roundnessLabelText = roundnessLabel.GetComponent<Text>();
                            roundnessLabelText.alignment = TextAnchor.MiddleLeft;
                            roundnessLabelText.text = "Roundness";
                            roundnessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(roundnessLabelText);
                            var roundnessLabelLayout = roundnessLabel.AddComponent<LayoutElement>();
                            roundnessLabelLayout.minWidth = 100f;

                            #endregion

                            #region Thickness

                            var thickness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "thickness");
                            var thicknessStorage = thickness.GetComponent<InputFieldStorage>();

                            Destroy(thicknessStorage.addButton.gameObject);
                            Destroy(thicknessStorage.subButton.gameObject);
                            Destroy(thicknessStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessStorage.middleButton.gameObject);
                            Destroy(thicknessStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thickness.transform, "label", 0);
                            var thicknessLabelText = thicknessLabel.GetComponent<Text>();
                            thicknessLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessLabelText.text = "Thickness";
                            thicknessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(thicknessLabelText);
                            var thicknessLabelLayout = thicknessLabel.AddComponent<LayoutElement>();
                            thicknessLabelLayout.minWidth = 100f;

                            #endregion

                            #region Thickness Offset

                            var thicknessOffset = Creator.NewUIObject("thickness offset", so);
                            var thicknessOffsetLayout = thicknessOffset.AddComponent<HorizontalLayoutGroup>();

                            var thicknessOffsetLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessOffset.transform, "label");
                            var thicknessOffsetLabelText = thicknessOffsetLabel.GetComponent<Text>();
                            thicknessOffsetLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessOffsetLabelText.text = "Thick Offset";
                            thicknessOffsetLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                            EditorThemeManager.AddLightText(thicknessOffsetLabelText);
                            var thicknessOffsetLabelLayout = thicknessOffsetLabel.AddComponent<LayoutElement>();
                            thicknessOffsetLabelLayout.minWidth = 130f;

                            var thicknessOffsetX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "x");
                            var thicknessOffsetXStorage = thicknessOffsetX.GetComponent<InputFieldStorage>();

                            Destroy(thicknessOffsetXStorage.addButton.gameObject);
                            Destroy(thicknessOffsetXStorage.subButton.gameObject);
                            Destroy(thicknessOffsetXStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessOffsetXStorage.middleButton.gameObject);
                            Destroy(thicknessOffsetXStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessOffsetXStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessOffsetXStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessOffsetXStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessOffsetY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "y");
                            var thicknessOffsetYStorage = thicknessOffsetY.GetComponent<InputFieldStorage>();

                            Destroy(thicknessOffsetYStorage.addButton.gameObject);
                            Destroy(thicknessOffsetYStorage.subButton.gameObject);
                            Destroy(thicknessOffsetYStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessOffsetYStorage.middleButton.gameObject);
                            Destroy(thicknessOffsetYStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessOffsetYStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessOffsetYStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessOffsetYStorage.rightButton, ThemeGroup.Function_2, false);

                            #endregion

                            #region Thickness Scale

                            var thicknessScale = Creator.NewUIObject("thickness scale", so);
                            var thicknessScaleLayout = thicknessScale.AddComponent<HorizontalLayoutGroup>();

                            var thicknessScaleLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessScale.transform, "label");
                            var thicknessScaleLabelText = thicknessScaleLabel.GetComponent<Text>();
                            thicknessScaleLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessScaleLabelText.text = "Thick Scale";
                            thicknessScaleLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                            EditorThemeManager.AddLightText(thicknessScaleLabelText);
                            var thicknessScaleLabelLayout = thicknessScaleLabel.AddComponent<LayoutElement>();
                            thicknessScaleLabelLayout.minWidth = 130f;

                            var thicknessScaleX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "x");
                            var thicknessScaleXStorage = thicknessScaleX.GetComponent<InputFieldStorage>();

                            Destroy(thicknessScaleXStorage.addButton.gameObject);
                            Destroy(thicknessScaleXStorage.subButton.gameObject);
                            Destroy(thicknessScaleXStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessScaleXStorage.middleButton.gameObject);
                            Destroy(thicknessScaleXStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessScaleXStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessScaleXStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessScaleXStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessScaleY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "y");
                            var thicknessScaleYStorage = thicknessScaleY.GetComponent<InputFieldStorage>();

                            Destroy(thicknessScaleYStorage.addButton.gameObject);
                            Destroy(thicknessScaleYStorage.subButton.gameObject);
                            Destroy(thicknessScaleYStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessScaleYStorage.middleButton.gameObject);
                            Destroy(thicknessScaleYStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessScaleYStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessScaleYStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessScaleYStorage.rightButton, ThemeGroup.Function_2, false);

                            #endregion

                            #region Slices

                            var slices = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "slices");
                            var slicesStorage = slices.GetComponent<InputFieldStorage>();

                            Destroy(slicesStorage.addButton.gameObject);
                            Destroy(slicesStorage.subButton.gameObject);
                            Destroy(slicesStorage.leftGreaterButton.gameObject);
                            Destroy(slicesStorage.middleButton.gameObject);
                            Destroy(slicesStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(slicesStorage.inputField);
                            EditorThemeManager.AddSelectable(slicesStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(slicesStorage.rightButton, ThemeGroup.Function_2, false);

                            var slicesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(slices.transform, "label", 0);
                            var slicesLabelText = slicesLabel.GetComponent<Text>();
                            slicesLabelText.alignment = TextAnchor.MiddleLeft;
                            slicesLabelText.text = "Slices";
                            slicesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(slicesLabelText);
                            var slicesLabelLayout = slicesLabel.AddComponent<LayoutElement>();
                            slicesLabelLayout.minWidth = 100f;

                            #endregion
                        }
                    }
                }

                ui.updatedShapes = true;
            }

            LSHelpers.SetActiveChildren(shapeSettings, false);

            int type = 0;
            int option = 0;
            if (generic != null)
            {
                type = generic.shape.Type;
                option = generic.shape.Option;
            }
            if (pulse != null)
            {
                type = pulse.shape.Type;
                option = pulse.shape.Option;
            }
            if (bullet != null)
            {
                type = bullet.shape.Type;
                option = bullet.shape.Option;
            }

            if (type >= shapeSettings.childCount)
            {
                CoreHelper.Log($"Somehow, the object ended up being at a higher shape than normal.");
                if (generic != null)
                    generic.shape = ShapeManager.inst.Shapes2D[shapeSettings.childCount - 1][0];
                if (pulse != null)
                    pulse.shape = ShapeManager.inst.Shapes2D[shapeSettings.childCount - 1][0];
                if (bullet != null)
                    bullet.shape = ShapeManager.inst.Shapes2D[shapeSettings.childCount - 1][0];

                PlayerManager.UpdatePlayerModels();
                RenderShape(ui);
                return;
            }

            shapeSettings.GetChild(type).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in ui.shapeToggles)
            {
                int index = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = type == index;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);

                if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length)
                    toggle.onValueChanged.AddListener(_val =>
                    {
                        if (_val)
                        {
                            CoreHelper.Log($"Set shape to {index}");
                            if (generic != null)
                                generic.shape = ShapeManager.inst.Shapes2D[index][0];
                            if (pulse != null)
                                pulse.shape = ShapeManager.inst.Shapes2D[index][0];
                            if (bullet != null)
                                bullet.shape = ShapeManager.inst.Shapes2D[index][0];

                            if (ui.Reference is PlayerModel.CustomObject customObject && customObject.shape.ShapeType == ShapeType.Polygon && EditorConfig.Instance.AutoPolygonRadius.Value)
                                customObject.polygonShape.Radius = customObject.polygonShape.GetAutoRadius();

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui);
                            LayoutRebuilder.ForceRebuildLayoutImmediate(ui.GameObject.transform.parent.AsRT());
                        }
                    });

                num++;
            }
            
            switch ((ShapeType)type)
            {
                case ShapeType.Text: {
                        if (ui.Reference is not PlayerModel.CustomObject customObject)
                        {
                            CoreHelper.Log($"Player shape cannot be text.");
                            if (generic != null)
                                generic.shape = ShapeManager.inst.Shapes2D[0][0];
                            if (bullet != null)
                                bullet.shape = ShapeManager.inst.Shapes2D[0][0];
                            if (pulse != null)
                                pulse.shape = ShapeManager.inst.Shapes2D[0][0];

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui);

                            break;
                        }

                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 114f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -54f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(400f, 74f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(400f, 74f);

                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.textComponent.alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                        textIF.lineType = InputField.LineType.MultiLineNewline;
                        textIF.onValueChanged.ClearAll();
                        textIF.text = customObject.text;
                        textIF.onValueChanged.AddListener(_val =>
                        {
                            CoreHelper.Log($"Set text to {_val}");
                            customObject.text = _val;

                            PlayerManager.UpdatePlayerModels();
                        });

                        break;
                    }
                case ShapeType.Image: {
                        if (ui.Reference is not PlayerModel.CustomObject customObject)
                        {
                            CoreHelper.Log($"Player shape cannot be image.");
                            if (generic != null)
                                generic.shape = ShapeManager.inst.Shapes2D[0][0];
                            if (bullet != null)
                                bullet.shape = ShapeManager.inst.Shapes2D[0][0];
                            if (pulse != null)
                                pulse.shape = ShapeManager.inst.Shapes2D[0][0];

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui);

                            break;
                        }

                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 92f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -54f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);

                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.onValueChanged.ClearAll();
                        textIF.text = customObject.text;
                        textIF.onValueChanged.AddListener(_val =>
                        {
                            CoreHelper.Log($"Set text to {_val}");
                            customObject.text = _val;

                            PlayerManager.UpdatePlayerModels();
                        });
                        var select = shapeSettings.Find("7/select").GetComponent<Button>();
                        select.onClick.NewListener(() => OpenImageSelector(ui));
                        shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(customObject.text) ? "No image selected" : customObject.text;

                        var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

                        // Stores / Removes Image Data for transfering of Image Objects between levels.
                        var dataText = shapeSettings.Find("7/set/Text").GetComponent<Text>();
                        dataText.text = !currentModel.assets.sprites.Has(x => x.name == customObject.text) ? "Store Data" : "Clear Data";
                        var set = shapeSettings.Find("7/set").GetComponent<Button>();
                        set.onClick.NewListener(() =>
                        {
                            var path = RTFile.CombinePaths(RTFile.BasePath, customObject.text);

                            if (!currentModel.assets.sprites.Has(x => x.name == customObject.text))
                                StoreImage(ui, path);
                            else
                            {
                                currentModel.assets.RemoveSprite(customObject.text);
                                if (!RTFile.FileExists(path))
                                    customObject.text = string.Empty;
                            }

                            PlayerManager.UpdatePlayerModels();

                            RenderShape(ui);
                        });

                        break;
                    }
                case ShapeType.Polygon: {
                        if (ui.Reference is not PlayerModel.CustomObject customObject)
                        {
                            CoreHelper.Log($"Player shape cannot be polygon.");
                            if (generic != null)
                                generic.shape = ShapeManager.inst.Shapes2D[0][0];
                            if (bullet != null)
                                bullet.shape = ShapeManager.inst.Shapes2D[0][0];
                            if (pulse != null)
                                pulse.shape = ShapeManager.inst.Shapes2D[0][0];

                            PlayerManager.UpdatePlayerModels();
                            RenderShape(ui);

                            break;
                        }

                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 332f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -145f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 244f);

                        var radius = shapeSettings.Find("10/radius").gameObject.GetComponent<InputFieldStorage>();
                        radius.inputField.onValueChanged.ClearAll();
                        radius.inputField.text = customObject.polygonShape.Radius.ToString();
                        radius.SetInteractible(!EditorConfig.Instance.AutoPolygonRadius.Value);
                        if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                        {
                            radius.inputField.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float num))
                                {
                                    num = Mathf.Clamp(num, 0.1f, 10f);
                                    customObject.polygonShape.Radius = num;

                                    PlayerManager.UpdatePlayerModels();
                                }
                            });

                            TriggerHelper.IncreaseDecreaseButtons(radius, min: 0.1f, max: 10f);
                            TriggerHelper.AddEventTriggers(radius.inputField.gameObject, TriggerHelper.ScrollDelta(radius.inputField, min: 0.1f, max: 10f));
                        }

                        var contextMenu = radius.inputField.gameObject.GetOrAddComponent<ContextClickable>();
                        contextMenu.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            var buttonFunctions = new List<ButtonFunction>()
                            {
                                new ButtonFunction($"Auto Assign Radius [{(EditorConfig.Instance.AutoPolygonRadius.Value ? "On" : "Off")}]", () =>
                                {
                                    EditorConfig.Instance.AutoPolygonRadius.Value = !EditorConfig.Instance.AutoPolygonRadius.Value;
                                    RenderShape(ui);
                                })
                            };
                            if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                            {
                                buttonFunctions.Add(new ButtonFunction("Set to Triangle Radius", () =>
                                {
                                    customObject.polygonShape.Radius = PolygonShape.TRIANGLE_RADIUS;

                                    PlayerManager.UpdatePlayerModels();
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Square Radius", () =>
                                {
                                    customObject.polygonShape.Radius = PolygonShape.SQUARE_RADIUS;

                                    PlayerManager.UpdatePlayerModels();
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Normal Radius", () =>
                                {
                                    customObject.polygonShape.Radius = PolygonShape.NORMAL_RADIUS;

                                    PlayerManager.UpdatePlayerModels();
                                }));
                            }

                            EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                        };

                        var sides = shapeSettings.Find("10/sides").gameObject.GetComponent<InputFieldStorage>();
                        sides.inputField.onValueChanged.ClearAll();
                        sides.inputField.text = customObject.polygonShape.Sides.ToString();
                        sides.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 3, 32);
                                customObject.polygonShape.Sides = num;
                                if (EditorConfig.Instance.AutoPolygonRadius.Value)
                                {
                                    customObject.polygonShape.Radius = customObject.polygonShape.GetAutoRadius();
                                    RenderShape(ui);
                                }

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(sides, min: 3, max: 32);
                        TriggerHelper.AddEventTriggers(sides.inputField.gameObject, TriggerHelper.ScrollDeltaInt(sides.inputField, min: 3, max: 32));
                        
                        var roundness = shapeSettings.Find("10/roundness").gameObject.GetComponent<InputFieldStorage>();
                        roundness.inputField.onValueChanged.ClearAll();
                        roundness.inputField.text = customObject.polygonShape.Roundness.ToString();
                        roundness.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                customObject.polygonShape.Roundness = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(roundness, max: 1f);
                        TriggerHelper.AddEventTriggers(roundness.inputField.gameObject, TriggerHelper.ScrollDelta(roundness.inputField, max: 1f));

                        var thickness = shapeSettings.Find("10/thickness").gameObject.GetComponent<InputFieldStorage>();
                        thickness.inputField.onValueChanged.ClearAll();
                        thickness.inputField.text = customObject.polygonShape.Thickness.ToString();
                        thickness.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                customObject.polygonShape.Thickness = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thickness, max: 1f);
                        TriggerHelper.AddEventTriggers(thickness.inputField.gameObject, TriggerHelper.ScrollDelta(thickness.inputField, max: 1f));
                        
                        var thicknessOffsetX = shapeSettings.Find("10/thickness offset/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetX.inputField.onValueChanged.ClearAll();
                        thicknessOffsetX.inputField.text = customObject.polygonShape.ThicknessOffset.x.ToString();
                        thicknessOffsetX.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                customObject.polygonShape.ThicknessOffset = new Vector2(num, customObject.polygonShape.ThicknessOffset.y);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetX);
                        TriggerHelper.AddEventTriggers(thicknessOffsetX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetX.inputField));
                        
                        var thicknessOffsetY = shapeSettings.Find("10/thickness offset/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetY.inputField.onValueChanged.ClearAll();
                        thicknessOffsetY.inputField.text = customObject.polygonShape.ThicknessOffset.y.ToString();
                        thicknessOffsetY.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                customObject.polygonShape.ThicknessOffset = new Vector2(customObject.polygonShape.ThicknessOffset.x, num);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetY);
                        TriggerHelper.AddEventTriggers(thicknessOffsetY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetY.inputField));
                        
                        var thicknessScaleX = shapeSettings.Find("10/thickness scale/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleX.inputField.onValueChanged.ClearAll();
                        thicknessScaleX.inputField.text = customObject.polygonShape.ThicknessScale.x.ToString();
                        thicknessScaleX.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                customObject.polygonShape.ThicknessScale = new Vector2(num, customObject.polygonShape.ThicknessScale.y);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleX);
                        TriggerHelper.AddEventTriggers(thicknessScaleX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleX.inputField));
                        
                        var thicknessScaleY = shapeSettings.Find("10/thickness scale/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleY.inputField.onValueChanged.ClearAll();
                        thicknessScaleY.inputField.text = customObject.polygonShape.ThicknessScale.y.ToString();
                        thicknessScaleY.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                customObject.polygonShape.ThicknessScale = new Vector2(customObject.polygonShape.ThicknessScale.x, num);

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleY);
                        TriggerHelper.AddEventTriggers(thicknessScaleY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleY.inputField));

                        var slices = shapeSettings.Find("10/slices").gameObject.GetComponent<InputFieldStorage>();
                        slices.inputField.onValueChanged.ClearAll();
                        slices.inputField.text = customObject.polygonShape.Slices.ToString();
                        slices.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 1, 32);
                                customObject.polygonShape.Slices = num;

                                PlayerManager.UpdatePlayerModels();
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(slices, min: 1, max: 32);
                        TriggerHelper.AddEventTriggers(slices.inputField.gameObject, TriggerHelper.ScrollDeltaInt(slices.inputField, min: 1, max: 32));

                        break;
                    }
                default: {
                        ui.GameObject.transform.AsRT().sizeDelta = new Vector2(750f, 92f);
                        shapeSettings.AsRT().anchoredPosition = new Vector2(568f, -54f);
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);

                        num = 0;
                        foreach (var toggle in ui.shapeOptionToggles[type])
                        {
                            int index = num;
                            toggle.onValueChanged.ClearAll();
                            toggle.isOn = option == index;
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[type]);

                            if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[type])
                                toggle.onValueChanged.AddListener(_val =>
                                {
                                    if (_val)
                                    {
                                        CoreHelper.Log($"Set shape option to {index}");
                                        if (generic != null)
                                            generic.shape = ShapeManager.inst.Shapes2D[type][index];
                                        if (pulse != null)
                                            pulse.shape = ShapeManager.inst.Shapes2D[type][index];
                                        if (bullet != null)
                                            bullet.shape = ShapeManager.inst.Shapes2D[type][index];

                                        PlayerManager.UpdatePlayerModels();
                                        RenderShape(ui);
                                    }
                                });

                            num++;
                        }

                        break;
                    }
            }
        }

        public void OpenImageSelector(PlayerEditorUI ui, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            SelectImage(jpgFile, ui, copyFile: copyFile, storeImage: storeImage);
        }

        public void StoreImage(PlayerEditorUI ui, string file)
        {
            if (ui.Reference is not PlayerModel.Generic generic)
                return;

            var currentModel = PlayersData.Current.GetPlayerModel(playerModelIndex);

            if (RTFile.FileExists(file))
            {
                var imageData = File.ReadAllBytes(file);

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                currentModel.assets.AddSprite(generic.text, SpriteHelper.CreateSprite(texture2d));
            }
            else
            {
                var imageData = LegacyPlugin.PALogoSprite.texture.EncodeToPNG();

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                currentModel.assets.AddSprite(generic.text, SpriteHelper.CreateSprite(texture2d));
            }
        }

        void SelectImage(string file, PlayerEditorUI ui, bool renderEditor = true, bool updateObject = true, bool copyFile = true, bool storeImage = false)
        {
            if (ui.Reference is not PlayerModel.Generic generic)
                return;

            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            RTFile.CreateDirectory(RTFile.CombinePaths(editorPath, "images"));

            file = RTFile.ReplaceSlash(file);
            CoreHelper.Log($"Selected file: {file}");
            if (!RTFile.FileExists(file))
                return;

            string jpgFileLocation = RTFile.CombinePaths(editorPath, "images", Path.GetFileName(file));

            if (copyFile && (EditorConfig.Instance.OverwriteImportedImages.Value || !RTFile.FileExists(jpgFileLocation)) && !file.Contains(editorPath))
                RTFile.CopyFile(file, jpgFileLocation);

            generic.text = jpgFileLocation.Remove(editorPath + "/");

            if (storeImage)
                StoreImage(ui, file);

            // Since setting image has no affect on the timeline object, we will only need to update the physical object.
            if (updateObject)
                PlayerManager.UpdatePlayerModels();

            if (renderEditor)
                RenderShape(ui);
        }

        public int VisibilityToInt(string vis) => vis switch
        {
            "isBoosting" => 0,
            "isTakingHit" => 1,
            "isZenMode" => 2,
            "isHealthPercentageGreater" => 3,
            "isHealthGreaterEquals" => 4,
            "isHealthEquals" => 5,
            "isHealthGreater" => 6,
            "isPressingKey" => 7,
            _ => 0,
        };

        public string IntToVisibility(int val) => val switch
        {
            0 => "isBoosting",
            1 => "isTakingHit",
            2 => "isZenMode",
            3 => "isHealthPercentageGreater",
            4 => "isHealthGreaterEquals",
            5 => "isHealthEquals",
            6 => "isHealthGreater",
            7 => "isPressingKey",
            _ => "isBoosting",
        };

        public void CreateNewModel()
        {
            var playerModel = PlayersData.Current.CreateNewPlayerModel();
            PlayersData.Current.SetPlayerModel(playerModelIndex, playerModel.basePart.id);
            PlayerManager.RespawnPlayers();
            StartCoroutine(RefreshEditor());
            EditorManager.inst.DisplayNotification("Created a new player model!", 1.5f, EditorManager.NotificationType.Success);
        }

        public void Save()
        {
            try
            {
                if (PlayersData.Save())
                    EditorManager.inst.DisplayNotification("Successfully saved player models!", 2f, EditorManager.NotificationType.Success);
                else
                    EditorManager.inst.DisplayNotification("Failed to save player models.", 2f, EditorManager.NotificationType.Error);
            }
            catch (Exception ex)
            {
                EditorManager.inst.DisplayNotification("Failed to save player models.", 2f, EditorManager.NotificationType.Error);
                CoreHelper.LogException(ex);
            }
        }

        public void Reload()
        {
            if (EditorLevelManager.inst.CurrentLevel)
                PlayersData.Load(EditorLevelManager.inst.CurrentLevel.GetFile(Level.PLAYERS_LSB));
            PlayerManager.RespawnPlayers();
            if (Dialog.IsCurrent)
                StartCoroutine(RefreshEditor());
            RTEditor.inst.PlayerModelsPopup.Close();

            EditorManager.inst.DisplayNotification("Loaded player models", 1.5f, EditorManager.NotificationType.Success);
        }

        public Tab CurrentTab { get; set; } = Tab.Base;
        public string searchTerm;
        public List<PlayerEditorUI> editorUIs = new List<PlayerEditorUI>();
        public List<PlayerEditorUI> EditorUIsActive => editorUIs.Where(x => x.GameObject.activeSelf).ToList();
    }
}

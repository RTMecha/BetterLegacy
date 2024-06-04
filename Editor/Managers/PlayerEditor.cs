using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BetterLegacy.Core;
using LSFunctions;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Configs;
using BetterLegacy.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Components.Player;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// The new Player editor.
    /// </summary>
    public class PlayerEditor : MonoBehaviour
    {
        public static PlayerEditor inst;

        public RTEditor.Popup ModelsPopup { get; set; }
        public string modelSearchTerm;
        public int playerModelIndex = 0;
        public Transform content;
        public Sprite PlayerSprite { get; set; }

        public static void Init() => Creator.NewGameObject("PlayerEditor", EditorManager.inst.transform.parent).AddComponent<PlayerEditor>();

        void Awake()
        {
            inst = this;
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
            var dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(EditorManager.inst.dialogs, "PlayerEditorDialog");

            dialog.transform.localScale = Vector3.one;
            dialog.transform.position = new Vector3(1537.5f, 714.945f, 0f) * EditorManager.inst.ScreenScale;
            dialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

            var dialogTitle = dialog.transform.GetChild(0);
            dialogTitle.GetComponent<Image>().color = LSColors.HexToColor("E57373");
            dialogTitle.GetChild(0).GetComponent<Text>().text = "- Player Editor -";

            dialog.transform.GetChild(1).AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(dialog.transform.Find("Text").gameObject);

            EditorThemeManager.AddGraphic(dialog.GetComponent<Image>(), ThemeGroup.Background_1);

            var search = EditorManager.inst.GetDialog("Open File Popup").Dialog.Find("search-box").gameObject.Duplicate(dialog.transform, "search");

            var searchField = search.transform.GetChild(0).GetComponent<InputField>();

            searchField.onValueChanged.ClearAll();
            searchField.text = "";
            searchField.onValueChanged.AddListener(delegate (string _val)
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
                    functionButtonStorage.text.fontSize = 16;
                    functionButtonStorage.text.text = e;
                    functionButtonStorage.button.onClick.ClearAll();
                    functionButtonStorage.button.onClick.AddListener(delegate ()
                    {
                        CurrentTab = (Tab)tabIndex;
                        StartCoroutine(RefreshEditor());
                    });

                    EditorThemeManager.AddSelectable(functionButtonStorage.button, EditorThemeManager.EditorTheme.GetGroup($"Tab Color {i + 1}"));
                    tab.AddComponent<ContrastColors>().Init(functionButtonStorage.text, tab.GetComponent<Image>());
                }
            }

            var scrollView = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/GameObjectDialog/data/left/Scroll View").Duplicate(dialog.transform, "Scroll View");
            var boolInput = GameObject.Find("Editor Systems/Editor GUI/sizer/main/EditorDialogs/SettingsDialog/snap/toggle/toggle");

            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 512f);

            PlayerSprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_player.png");

            EditorHelper.AddEditorDialog("Player Editor New", dialog);
            EditorHelper.AddEditorDropdown("Player Editor New", "", "Edit", PlayerSprite, delegate ()
            {
                EditorManager.inst.ShowDialog("Player Editor New");
                StartCoroutine(RefreshEditor());
            });

            content = scrollView.transform.Find("Viewport/Content");

            var labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;

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
                    var gameObject = Creator.NewUIObject("Global Speed", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = "Global Speed";
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(84f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = "Global Speed",
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Float,
                        Index = -1,
                    });
                }
                
                // Lock Boost
                {
                    var gameObject = Creator.NewUIObject("Global Lock Boost", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = "Global Lock Boost";
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = "Global Lock Boost",
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Bool,
                        Index = -1,
                    });
                }

                // Gamemode
                {
                    var gameObject = Creator.NewUIObject("Global Lock Boost", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = "Global Gamemode";
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    dropdown.transform.AsRT().anchoredPosition = new Vector2(566f, -16f);
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Regular"),
                        new Dropdown.OptionData("Platformer"),
                    };

                    EditorThemeManager.AddDropdown(dropdown);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = "Global Gamemode",
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Enum,
                        Index = -1,
                    });
                }

                // Max Jump Count
                {
                    var gameObject = Creator.NewUIObject("Global Max Jump Count", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = "Global Max Jump Count";
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(84f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                    var inputFieldStorage = input.GetComponent<InputFieldStorage>();

                    Destroy(inputFieldStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputFieldStorage.inputField);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = "Global Max Jump Count",
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Int,
                        Index = -1,
                    });
                }

                // Update Properties
                {
                    var gameObject = Creator.NewUIObject("Update Properties", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(750f, 42f);

                    var label = labelPrefab.Duplicate(gameObject.transform, "label");
                    var labelText = label.GetComponent<Text>();
                    labelText.text = "Update Properties";
                    EditorThemeManager.AddLightText(labelText);
                    UIManager.SetRectTransform(label.transform.AsRT(), new Vector2(32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(762f, 32f));

                    var image = gameObject.AddComponent<Image>();

                    var button = gameObject.AddComponent<Button>();
                    button.image = image;
                    button.onClick.AddListener(delegate ()
                    {
                        RTPlayer.SetGameDataProperties();
                    });

                    EditorThemeManager.AddSelectable(button, ThemeGroup.List_Button_1);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = "Update Properties",
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Function,
                        Index = -1,
                    });
                }
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

                RTEditor.EditorProperty.ValueType valueType = RTEditor.EditorProperty.ValueType.Function;
                if (name == "Base ID")
                {
                    var id = labelPrefab.Duplicate(gameObject.transform, "id");
                    UIManager.SetRectTransform(id.transform.AsRT(), new Vector2(-32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(750f, 32f));
                    var button = gameObject.AddComponent<Button>();
                }

                if (name == "Base Name" || name.Contains("Color") && name.Contains("Custom"))
                {
                    valueType = RTEditor.EditorProperty.ValueType.String;

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
                    name == "Tail Base Time" || name == "Base Jump Gravity" || name == "Base Jump Intensity" || name == "Base Bounciness")
                {
                    if (name == "Base Health" || name == "Boost Particles Amount" || name == "Base Jump Count")
                        valueType = RTEditor.EditorProperty.ValueType.Int;
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
                        valueType = RTEditor.EditorProperty.ValueType.Float;

                    var input = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(84f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

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
                    valueType = RTEditor.EditorProperty.ValueType.Vector2;

                    var inputX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "x");
                    UIManager.SetRectTransform(inputX.transform.AsRT(), new Vector2(-52f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                    var inputXStorage = inputX.GetComponent<InputFieldStorage>();

                    Destroy(inputXStorage.middleButton.gameObject);
                    EditorThemeManager.AddInputField(inputXStorage.inputField);
                    EditorThemeManager.AddSelectable(inputXStorage.leftButton, ThemeGroup.Function_2, false);
                    EditorThemeManager.AddSelectable(inputXStorage.rightButton, ThemeGroup.Function_2, false);

                    Destroy(inputXStorage.leftGreaterButton.gameObject);
                    Destroy(inputXStorage.rightGreaterButton.gameObject);

                    var inputY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(gameObject.transform, "y");
                    UIManager.SetRectTransform(inputY.transform.AsRT(), new Vector2(162f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

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
                    valueType = RTEditor.EditorProperty.ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    dropdown.transform.AsRT().anchoredPosition = new Vector2(566f, -16f);
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Legacy"),
                        new Dropdown.OptionData("Dev+")
                    };

                    EditorThemeManager.AddDropdown(dropdown);
                }
                
                if (name == "Base Rotate Mode")
                {
                    valueType = RTEditor.EditorProperty.ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    dropdown.transform.AsRT().anchoredPosition = new Vector2(566f, -16f);
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Face Direction"),
                        new Dropdown.OptionData("None"),
                        new Dropdown.OptionData("Flip X"),
                        new Dropdown.OptionData("Flip Y")
                    };

                    EditorThemeManager.AddDropdown(dropdown);
                }
                
                if (name == "GUI Health Mode")
                {
                    valueType = RTEditor.EditorProperty.ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    dropdown.transform.AsRT().anchoredPosition = new Vector2(566f, -16f);
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Images"),
                        new Dropdown.OptionData("Text"),
                        new Dropdown.OptionData("Equals Bar"),
                        new Dropdown.OptionData("Bar"),
                    };

                    EditorThemeManager.AddDropdown(dropdown);
                }
                
                if (name.Contains("Easing"))
                {
                    valueType = RTEditor.EditorProperty.ValueType.Enum;

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    dropdown.transform.AsRT().anchoredPosition = new Vector2(566f, -16f);
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = EditorManager.inst.CurveOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();

                    EditorThemeManager.AddDropdown(dropdown);
                }

                if (name.Contains("Color") && !name.Contains("Easing") && !name.Contains("Custom") && !name.Contains("Duration"))
                {
                    valueType = RTEditor.EditorProperty.ValueType.Color;

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
                    valueType = RTEditor.EditorProperty.ValueType.Bool;

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                if (name == "Custom Objects")
                {
                    // maybe have a select custom objects thing similar to select models button
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
                selectStorage.text.fontSize = 16;
                selectStorage.text.text = "Select";
                selectStorage.button.onClick.ClearAll();
                selectStorage.button.onClick.AddListener(delegate ()
                {
                    EditorManager.inst.ShowDialog("Player Models Popup");
                    StartCoroutine(RefreshModels());
                });

                EditorThemeManager.AddSelectable(selectStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(selectStorage.text, ThemeGroup.Function_2_Text);

                var playerIndexObject = EditorPrefabHolder.Instance.Dropdown.Duplicate(layout.transform, "dropdown");
                playerIndexObject.transform.AsRT().sizeDelta = new Vector2(164f, 43.2f);
                var playerIndexDropdown = playerIndexObject.GetComponent<Dropdown>();
                playerIndexDropdown.onValueChanged.ClearAll();
                playerIndexDropdown.options = new List<Dropdown.OptionData>
                {
                    new Dropdown.OptionData("Player 1"),
                    new Dropdown.OptionData("Player 2"),
                    new Dropdown.OptionData("Player 3"),
                    new Dropdown.OptionData("Player 4"),
                };
                playerIndexDropdown.value = 0;
                playerIndexDropdown.onValueChanged.AddListener(delegate (int _val)
                {
                    playerModelIndex = _val;
                    StartCoroutine(RefreshEditor());
                });

                EditorThemeManager.AddDropdown(playerIndexDropdown);

                var create = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                create.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var createStorage = create.GetComponent<FunctionButtonStorage>();
                createStorage.text.fontSize = 16;
                createStorage.text.text = "Create";
                createStorage.button.onClick.ClearAll();
                createStorage.button.onClick.AddListener(delegate ()
                {
                    var num = PlayerManager.PlayerModels.Count;

                    PlayerManager.CreateNewPlayerModel();
                    PlayerManager.SetPlayerModel(playerModelIndex, PlayerManager.PlayerModels.ElementAt(num).Key);
                    PlayerManager.RespawnPlayers();
                    StartCoroutine(RefreshEditor());
                    EditorManager.inst.DisplayNotification("Created a new player model!", 1.5f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddSelectable(createStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(createStorage.text, ThemeGroup.Function_2_Text);

                var save = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                save.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var saveStorage = save.GetComponent<FunctionButtonStorage>();
                saveStorage.text.fontSize = 16;
                saveStorage.text.text = "Save";
                saveStorage.button.onClick.ClearAll();
                saveStorage.button.onClick.AddListener(delegate ()
                {
                    PlayerManager.SaveGlobalModels();
                    EditorManager.inst.DisplayNotification("Saved player models", 1.5f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddSelectable(saveStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(saveStorage.text, ThemeGroup.Function_2_Text);

                var load = EditorPrefabHolder.Instance.Function1Button.Duplicate(layout.transform, "function");
                load.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                var loadStorage = load.GetComponent<FunctionButtonStorage>();
                loadStorage.text.fontSize = 16;
                loadStorage.text.text = "Reload";
                loadStorage.button.onClick.ClearAll();
                loadStorage.button.onClick.AddListener(delegate ()
                {
                    PlayerManager.LoadGlobalModels();
                    PlayerManager.RespawnPlayers();
                    StartCoroutine(RefreshEditor());
                    EditorManager.inst.HideDialog("Player Models Popup");

                    EditorManager.inst.DisplayNotification("Loaded player models", 1.5f, EditorManager.NotificationType.Success);
                });

                EditorThemeManager.AddSelectable(loadStorage.button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(loadStorage.text, ThemeGroup.Function_2_Text);
            }

            ModelsPopup = RTEditor.inst.GeneratePopup("Player Models Popup", "Player Models", Vector2.zero, Vector2.zero, delegate (string _val)
            {
                modelSearchTerm = _val;
                StartCoroutine(RefreshModels());
            });

            yield break;
        }

        public IEnumerator RefreshEditor()
        {
            var currentIndex = PlayerManager.GetPlayerModelIndex(playerModelIndex);
            var currentModel = PlayerManager.PlayerModels[currentIndex];

            var isDefault = PlayerModel.DefaultModels.Any(x => currentModel.basePart.id == x.basePart.id);
            content.Find("handler").gameObject.SetActive(isDefault && CurrentTab != Tab.Global);

            for (int i = 0; i < editorUIs.Count; i++)
            {
                var ui = editorUIs[i];
                var active = (!isDefault || ui.Name == "Base ID" || ui.Tab == CurrentTab && CurrentTab == Tab.Global) && CoreHelper.SearchString(searchTerm, ui.Name) && ui.Tab == CurrentTab;
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
                                inputFieldStorage.inputField.text = GameData.Current.LevelBeatmapData.ModLevelData.speedMultiplier.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        GameData.Current.LevelBeatmapData.ModLevelData.speedMultiplier = result;
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggerParams(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                                break;
                            }
                        case "Global Lock Boost":
                            {
                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = GameData.Current.LevelBeatmapData.ModLevelData.lockBoost;
                                toggle.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    GameData.Current.LevelBeatmapData.ModLevelData.lockBoost = _val;
                                    RTPlayer.SetGameDataProperties();
                                });

                                break;
                            }
                        case "Global Gamemode":
                            {
                                var dropdown = ui.GameObject.transform.Find("dropdown").GetComponent<Dropdown>();
                                dropdown.onValueChanged.ClearAll();
                                dropdown.value = GameData.Current.LevelBeatmapData.ModLevelData.gameMode;
                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    GameData.Current.LevelBeatmapData.ModLevelData.gameMode = _val;
                                    RTPlayer.SetGameDataProperties();
                                });

                                TriggerHelper.AddEventTriggerParams(dropdown.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, delegate (BaseEventData baseEventData)
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
                                inputFieldStorage.inputField.text = GameData.Current.LevelBeatmapData.ModLevelData.maxJumpCount.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        GameData.Current.LevelBeatmapData.ModLevelData.maxJumpCount = result;
                                        RTPlayer.SetGameDataProperties();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage);
                                TriggerHelper.AddEventTriggerParams(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                                break;
                            }
                    }
                    continue;
                }

                try
                {
                    var value = PlayerManager.PlayerModels[PlayerManager.PlayerModelsIndex[Mathf.Clamp(playerModelIndex, 0, 3)]][ui.Index];
                    var key = PlayerModel.Values[ui.Index];

                    if (key == "Base ID")
                    {
                        var text = ui.GameObject.transform.Find("id").GetComponent<Text>();
                        text.alignment = TextAnchor.MiddleRight;
                        text.text = value.ToString() + " (Click to copy)";
                        var button = ui.GameObject.GetComponent<Button>();
                        button.onClick.ClearAll();
                        button.onClick.AddListener(delegate ()
                        {
                            LSText.CopyToClipboard(value.ToString());
                            EditorManager.inst.DisplayNotification($"Copied ID \"{value}\" to clipboard!", 2f, EditorManager.NotificationType.Success);
                        });

                        continue;
                    }

                    switch (ui.ValueType)
                    {
                        case RTEditor.EditorProperty.ValueType.Bool:
                            {
                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = (bool)value;
                                toggle.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    currentModel[key] = _val;
                                    PlayerManager.UpdatePlayers();
                                });

                                break;
                            }
                        case RTEditor.EditorProperty.ValueType.String:
                            {
                                var inputField = ui.GameObject.transform.Find("input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.text = value.ToString();
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (!(key.Contains("Color") && key.Contains("Custom")))
                                        currentModel[key] = _val;
                                    else
                                        currentModel[key] = _val.Length == 6 || _val.Length == 8 ? _val : LSColors.ColorToHex(LSColors.pink500);

                                    PlayerManager.UpdatePlayers();
                                });

                                break;
                            }
                        case RTEditor.EditorProperty.ValueType.Int:
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = value.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (int.TryParse(_val, out int result))
                                    {
                                        currentModel[key] = result;
                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage);
                                TriggerHelper.AddEventTriggerParams(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                                break;
                            }
                        case RTEditor.EditorProperty.ValueType.Float:
                            {
                                var inputFieldStorage = ui.GameObject.transform.Find("input").GetComponent<InputFieldStorage>();
                                inputFieldStorage.inputField.onValueChanged.ClearAll();
                                inputFieldStorage.inputField.text = value.ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        currentModel[key] = result;
                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggerParams(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                                break;
                            }
                        case RTEditor.EditorProperty.ValueType.Vector2:
                            {
                                var inputXStorage = ui.GameObject.transform.Find("x").GetComponent<InputFieldStorage>();
                                var inputYStorage = ui.GameObject.transform.Find("y").GetComponent<InputFieldStorage>();

                                inputXStorage.inputField.onValueChanged.ClearAll();
                                inputXStorage.inputField.text = ((Vector2)value).x.ToString();
                                inputXStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        currentModel[key] = new Vector2(result, ((Vector2)value).y);
                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = ((Vector2)value).y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (float.TryParse(_val, out float result))
                                    {
                                        currentModel[key] = new Vector2(((Vector2)value).x, result);
                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                break;
                            }
                        case RTEditor.EditorProperty.ValueType.Color:
                            {
                                var colors = ui.GameObject.transform.Find("colors");

                                for (int j = 0; j < colors.childCount; j++)
                                {
                                    var colorIndex = j;
                                    var color = colors.GetChild(j);
                                    color.GetChild(0).gameObject.SetActive((int)value == j);
                                    color.GetComponent<Image>().color = CoreHelper.GetPlayerColor(playerModelIndex, j, 1f, "FFFFFF");

                                    var button = color.GetComponent<Button>();
                                    button.onClick.ClearAll();
                                    button.onClick.AddListener(delegate ()
                                    {
                                        currentModel[key] = colorIndex;
                                        StartCoroutine(RefreshEditor());
                                    });
                                }

                                break;
                            }
                        case RTEditor.EditorProperty.ValueType.Enum:
                            {
                                var dropdown = ui.GameObject.transform.Find("dropdown").GetComponent<Dropdown>();
                                dropdown.onValueChanged.ClearAll();
                                dropdown.value = (int)value;
                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    currentModel[key] = _val;
                                    PlayerManager.UpdatePlayers();
                                });

                                TriggerHelper.AddEventTriggerParams(dropdown.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, delegate (BaseEventData baseEventData)
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
                    }
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Exception: {ex}");
                }
            }

            if (CurrentTab == Tab.Global)
            {
                // handle global functions here
            }

            yield break;
        }

        public IEnumerator RefreshModels()
        {
            LSHelpers.DeleteChildren(ModelsPopup.Content);

            int num = 0;
            foreach (var playerModel in PlayerManager.PlayerModels)
            {
                int index = num;
                var name = playerModel.Value.basePart.name;
                if (!CoreHelper.SearchString(modelSearchTerm, name))
                    continue;

                var model = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(ModelsPopup.Content, name);
                var modelButton = model.GetComponent<Button>();
                modelButton.onClick.ClearAll();
                modelButton.onClick.AddListener(delegate ()
                {
                    PlayerManager.SetPlayerModelIndex(playerModelIndex, index);
                    PlayerManager.RespawnPlayers();
                    StartCoroutine(RefreshEditor());
                });

                var text = model.transform.GetChild(0).GetComponent<Text>();
                text.text = name;

                var image = model.transform.Find("Image").GetComponent<Image>();
                image.sprite = PlayerSprite;

                EditorThemeManager.ApplySelectable(modelButton, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(image, ThemeGroup.Light_Text);
                EditorThemeManager.ApplyLightText(text);

                num++;
            }

            yield break;
        }

        public Tab CurrentTab { get; set; } = Tab.Base;
        public string searchTerm;
        public List<PlayerEditorUI> editorUIs = new List<PlayerEditorUI>();

        public class PlayerEditorUI
        {
            public string Name { get; set; }
            public GameObject GameObject { get; set; }
            public Tab Tab { get; set; }
            public RTEditor.EditorProperty.ValueType ValueType { get; set; }
            public int Index { get; set; }
        }
    }
}

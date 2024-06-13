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
using Crosstales.FB;
using System.IO;

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
        public string CustomObjectID { get; set; }

        GameObject labelPrefab;
        GameObject visiilityPrefab;

        public static int[] UnmoddedShapeCounts => new int[]
        {
            3,
            9,
            4,
            2,
            1,
            6
        };

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

            EditorHelper.AddEditorDialog("Player Editor", dialog);
            EditorHelper.AddEditorDropdown("Player Editor", "", "Edit", PlayerSprite, delegate ()
            {
                EditorManager.inst.ShowDialog("Player Editor");
                StartCoroutine(RefreshEditor());
            });

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
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Float,
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
                    labelText.text = "Global Lock Boost";
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
                        ValueType = RTEditor.EditorProperty.ValueType.Bool,
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
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Enum,
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
                        Name = name,
                        GameObject = gameObject,
                        Tab = Tab.Global,
                        ValueType = RTEditor.EditorProperty.ValueType.Int,
                        Index = -1,
                    });
                }

                // Update Properties
                {
                    var name = "Update Properties";

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
                    button.onClick.AddListener(delegate ()
                    {
                        RTPlayer.SetGameDataProperties();
                    });

                    EditorThemeManager.AddSelectable(button, ThemeGroup.List_Button_1);

                    editorUIs.Add(new PlayerEditorUI
                    {
                        Name = name,
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
                    valueType = RTEditor.EditorProperty.ValueType.Function;

                    var id = labelPrefab.Duplicate(gameObject.transform, "id");
                    UIManager.SetRectTransform(id.transform.AsRT(), new Vector2(-32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(750f, 32f));
                    gameObject.AddComponent<Button>();
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
                    name == "Tail Base Time" || name == "Base Jump Gravity" || name == "Base Jump Count" || name == "Base Jump Intensity" || name == "Base Bounciness")
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
                    valueType = RTEditor.EditorProperty.ValueType.Function;

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
                    var gameObject = GenerateUIPart("ID", Tab.Custom, RTEditor.EditorProperty.ValueType.Function);

                    var id = labelPrefab.Duplicate(gameObject.transform, "id");
                    UIManager.SetRectTransform(id.transform.AsRT(), new Vector2(-32f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(750f, 32f));
                    var button = gameObject.AddComponent<Button>();
                }

                // Name
                {
                    var gameObject = GenerateUIPart("Name", Tab.Custom, RTEditor.EditorProperty.ValueType.String);

                    var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

                    EditorThemeManager.AddInputField(input.GetComponent<InputField>());
                }

                // Position
                {
                    var gameObject = GenerateUIPart("Position", Tab.Custom, RTEditor.EditorProperty.ValueType.Vector2);

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

                // Scale
                {
                    var gameObject = GenerateUIPart("Scale", Tab.Custom, RTEditor.EditorProperty.ValueType.Vector2);

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

                // Rotation
                {
                    var gameObject = GenerateUIPart("Rotation", Tab.Custom, RTEditor.EditorProperty.ValueType.Float);

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

                // Depth
                {
                    var gameObject = GenerateUIPart("Depth", Tab.Custom, RTEditor.EditorProperty.ValueType.Float);

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

                // Color
                {
                    var gameObject = GenerateUIPart("Color", Tab.Custom, RTEditor.EditorProperty.ValueType.Color);

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
                    var gameObject = GenerateUIPart("Custom Color", Tab.Custom, RTEditor.EditorProperty.ValueType.String);

                    var input = EditorPrefabHolder.Instance.NumberInputField.transform.Find("input").gameObject.Duplicate(gameObject.transform, "input");
                    UIManager.SetRectTransform(input.transform.AsRT(), new Vector2(260f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200f, 32f));

                    EditorThemeManager.AddInputField(input.GetComponent<InputField>());
                }

                // Opacity
                {
                    var gameObject = GenerateUIPart("Opacity", Tab.Custom, RTEditor.EditorProperty.ValueType.Float);

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

                // Parent
                {
                    var gameObject = GenerateUIPart("Parent", Tab.Custom, RTEditor.EditorProperty.ValueType.Enum);

                    var dropdown = EditorPrefabHolder.Instance.Dropdown.Duplicate(gameObject.transform, "dropdown").GetComponent<Dropdown>();
                    dropdown.transform.AsRT().anchoredPosition = new Vector2(566f, -16f);
                    dropdown.options.Clear();
                    dropdown.onValueChanged.RemoveAllListeners();

                    dropdown.options = new List<Dropdown.OptionData>
                    {
                        new Dropdown.OptionData("Head"),
                        new Dropdown.OptionData("Boost"),
                        new Dropdown.OptionData("Boost"),
                        new Dropdown.OptionData("Boost Tail"),
                        new Dropdown.OptionData("Tail 1"),
                        new Dropdown.OptionData("Tail 2"),
                        new Dropdown.OptionData("Tail 3"),
                        new Dropdown.OptionData("Face"),
                    };

                    EditorThemeManager.AddDropdown(dropdown);
                }

                // Position Offset
                {
                    var gameObject = GenerateUIPart("Position Offset", Tab.Custom, RTEditor.EditorProperty.ValueType.Float);

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

                // Scale Offset
                {
                    var gameObject = GenerateUIPart("Scale Offset", Tab.Custom, RTEditor.EditorProperty.ValueType.Float);

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

                // Rotation Offset
                {
                    var gameObject = GenerateUIPart("Rotation Offset", Tab.Custom, RTEditor.EditorProperty.ValueType.Float);

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

                // Scale Parent
                {
                    var gameObject = GenerateUIPart("Scale Parent", Tab.Custom, RTEditor.EditorProperty.ValueType.Bool);

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                // Rotation Parent
                {
                    var gameObject = GenerateUIPart("Rotation Parent", Tab.Custom, RTEditor.EditorProperty.ValueType.Bool);

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                // Shape
                {
                    var gameObject = GenerateUIPart("Shape", Tab.Custom, RTEditor.EditorProperty.ValueType.Enum);
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
                    var gameObject = GenerateUIPart("Require All", Tab.Custom, RTEditor.EditorProperty.ValueType.Bool);

                    var toggle = boolInput.Duplicate(gameObject.transform, "toggle").GetComponent<Toggle>();
                    toggle.transform.AsRT().anchoredPosition = new Vector2(725f, -21f);

                    EditorThemeManager.AddToggle(toggle);
                }

                // Visibility
                {
                    var gameObject = GenerateUIPart("Visibility", Tab.Custom, RTEditor.EditorProperty.ValueType.Bool);

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
                playerIndexObject.transform.AsRT().sizeDelta = new Vector2(200f, 43.2f);
                var playerIndexDropdown = playerIndexObject.GetComponent<Dropdown>();
                playerIndexDropdown.onValueChanged.ClearAll();
                playerIndexDropdown.options = new List<Dropdown.OptionData>();
                for (int i = 1; i <= 4; i++)
                    playerIndexDropdown.options.Add(new Dropdown.OptionData($"Player {i}"));
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

        public GameObject GenerateUIPart(string name, Tab tab, RTEditor.EditorProperty.ValueType valueType, int index = -1)
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

                if (ui.Name == "Custom Objects")
                {
                    var button = ui.GameObject.GetComponent<Button>();
                    button.onClick.ClearAll();
                    button.onClick.AddListener(delegate ()
                    {
                        EditorManager.inst.ShowDialog("Player Models Popup");
                        StartCoroutine(RefreshCustomObjects());
                    });

                    continue;
                }

                if (ui.Tab == Tab.Custom)
                {
                    var customActive = active && !string.IsNullOrEmpty(CustomObjectID) && currentModel.customObjects.ContainsKey(CustomObjectID);
                    ui.GameObject?.SetActive(customActive);
                    if (!customActive)
                        continue;

                    var customObject = currentModel.customObjects[CustomObjectID];

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
                                button.onClick.AddListener(delegate ()
                                {
                                    LSText.CopyToClipboard(customObject.id);
                                    EditorManager.inst.DisplayNotification($"Copied ID \"{customObject.id}\" to clipboard!", 2f, EditorManager.NotificationType.Success);
                                });

                                break;
                            }
                        case "Name":
                        case "Custom Color":
                            {
                                var inputField = ui.GameObject.transform.Find("input").GetComponent<InputField>();
                                inputField.onValueChanged.ClearAll();
                                inputField.text = ui.Name == "Name" ? customObject.name : customObject.customColor;
                                inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    if (ui.Name == "Name")
                                        customObject.name = _val;
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
                                inputFieldStorage.inputField.text =
                                    (ui.Name == "Rotation" ? customObject.rotation : ui.Name == "Depth" ? customObject.depth :
                                    ui.Name == "Opacity" ? customObject.opacity : ui.Name == "Position Offset" ? customObject.positionOffset :
                                    ui.Name == "Scale Offset" ? customObject.scaleOffset : customObject.rotationOffset).ToString();
                                inputFieldStorage.inputField.onValueChanged.AddListener(delegate (string _val)
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

                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage);
                                TriggerHelper.AddEventTriggerParams(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

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
                                inputXStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    var vector = ui.Name == "Position" ? customObject.position : customObject.scale;

                                    if (float.TryParse(_val, out float result))
                                    {
                                        if (isPosition)
                                            customObject.position = new Vector2(result, vector.y);
                                        else
                                            customObject.scale = new Vector2(result, vector.y);
                                        
                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                inputYStorage.inputField.onValueChanged.ClearAll();
                                inputYStorage.inputField.text = vector.y.ToString();
                                inputYStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                {
                                    var vector = ui.Name == "Position" ? customObject.position : customObject.scale;

                                    if (float.TryParse(_val, out float result))
                                    {
                                        if (isPosition)
                                            customObject.position = new Vector2(vector.x, result);
                                        else
                                            customObject.scale = new Vector2(vector.x, result);

                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                TriggerHelper.AddEventTriggerParams(inputXStorage.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggerParams(inputYStorage.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
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
                                    color.GetComponent<Image>().color = CoreHelper.GetPlayerColor(playerModelIndex, j, 1f, customObject.customColor);

                                    var button = color.GetComponent<Button>();
                                    button.onClick.ClearAll();
                                    button.onClick.AddListener(delegate ()
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
                                dropdown.onValueChanged.AddListener(delegate (int _val)
                                {
                                    customObject.parent = _val;

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
                        case "Scale Parent":
                        case "Rotation Parent":
                        case "Require All":
                            {
                                var isScale = ui.Name == "Scale Parent";
                                var isRequireAll = ui.Name == "Require All";

                                var toggle = ui.GameObject.transform.Find("toggle").GetComponent<Toggle>();
                                toggle.onValueChanged.ClearAll();
                                toggle.isOn = isScale ? customObject.scaleParent : isRequireAll ? customObject.requireAll : customObject.rotationParent;
                                toggle.onValueChanged.AddListener(delegate (bool _val)
                                {
                                    if (isScale)
                                        customObject.scaleParent = _val;
                                    else if (isRequireAll)
                                        customObject.requireAll = _val;
                                    else
                                        customObject.rotationParent = _val;

                                    PlayerManager.UpdatePlayers();
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
                                add.transform.Find("Text").GetComponent<Text>().text = "Add Visiblity Setting";
                                ((RectTransform)add.transform).sizeDelta = new Vector2(760f, 32f);
                                var addButton = add.GetComponent<Button>();
                                addButton.onClick.ClearAll();
                                addButton.onClick.AddListener(delegate ()
                                {
                                    var newVisibility = new PlayerModel.CustomObject.Visiblity();
                                    newVisibility.command = IntToVisibility(0);
                                    customObject.visibilitySettings.Add(newVisibility);
                                    StartCoroutine(RefreshEditor());
                                });

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
                                    toggle.text.text = $"Not: {(visibility.not ? "Yes" : "No")}";
                                    toggle.button.onClick.AddListener(delegate ()
                                    {
                                        visibility.not = !visibility.not;
                                        toggle.text.text = $"Not: {(visibility.not ? "Yes" : "No")}";
                                    });
                                    EditorThemeManager.ApplySelectable(toggle.button, ThemeGroup.Function_2);
                                    EditorThemeManager.ApplyGraphic(toggle.text, ThemeGroup.Function_2_Text);

                                    var x = EditorPrefabHolder.Instance.Dropdown.Duplicate(bar.transform);
                                    x.transform.SetParent(bar.transform);
                                    x.transform.localScale = Vector3.one;

                                    Destroy(x.GetComponent<HoverTooltip>());

                                    Destroy(x.GetComponent<HideDropdownOptions>());

                                    var dropdown = x.GetComponent<Dropdown>();
                                    dropdown.onValueChanged.RemoveAllListeners();
                                    dropdown.options.Clear();
                                    dropdown.options = new List<Dropdown.OptionData>
                                    {
                                        new Dropdown.OptionData("Is Boosting"),
                                        new Dropdown.OptionData("Is Taking Hit"),
                                        new Dropdown.OptionData("Is Zen Mode"),
                                        new Dropdown.OptionData("Is Health Percentage Greater"),
                                        new Dropdown.OptionData("Is Health Greater Equals"),
                                        new Dropdown.OptionData("Is Health Equals"),
                                        new Dropdown.OptionData("Is Health Greater"),
                                        new Dropdown.OptionData("Is Pressing Key"),
                                    };
                                    dropdown.value = VisibilityToInt(visibility.command);
                                    dropdown.onValueChanged.AddListener(delegate (int _val)
                                    {
                                        visibility.command = IntToVisibility(_val);
                                    });

                                    // Value
                                    {
                                        var value = EditorPrefabHolder.Instance.NumberInputField.Duplicate(bar.transform, "input");
                                        var valueStorage = value.GetComponent<InputFieldStorage>();

                                        valueStorage.inputField.onValueChanged.ClearAll();
                                        valueStorage.inputField.text = visibility.value.ToString();
                                        valueStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                                        {
                                            if (float.TryParse(_val, out float result))
                                                visibility.value = result;
                                        });

                                        DestroyImmediate(valueStorage.leftGreaterButton.gameObject);
                                        DestroyImmediate(valueStorage.middleButton.gameObject);
                                        DestroyImmediate(valueStorage.rightGreaterButton.gameObject);

                                        TriggerHelper.AddEventTriggerParams(value, TriggerHelper.ScrollDelta(valueStorage.inputField));
                                        TriggerHelper.IncreaseDecreaseButtons(valueStorage);
                                    }

                                    var delete = EditorPrefabHolder.Instance.DeleteButton.Duplicate(bar.transform, "delete");

                                    delete.transform.AsRT().anchoredPosition = new Vector2(-5f, 0f);

                                    delete.GetComponent<LayoutElement>().ignoreLayout = false;

                                    var deleteButton = delete.GetComponent<DeleteButtonStorage>();
                                    deleteButton.button.onClick.ClearAll();
                                    deleteButton.button.onClick.AddListener(delegate ()
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
                    var value = PlayerManager.PlayerModels[PlayerManager.PlayerModelsIndex[Mathf.Clamp(playerModelIndex, 0, 3)]][ui.Index];
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
                        button.onClick.AddListener(delegate ()
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
                                        var value = PlayerManager.PlayerModels[PlayerManager.PlayerModelsIndex[Mathf.Clamp(playerModelIndex, 0, 3)]][ui.Index];
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
                                        var value = PlayerManager.PlayerModels[PlayerManager.PlayerModelsIndex[Mathf.Clamp(playerModelIndex, 0, 3)]][ui.Index];
                                        currentModel[key] = new Vector2(((Vector2)value).x, result);
                                        PlayerManager.UpdatePlayers();
                                    }
                                });

                                TriggerHelper.AddEventTriggerParams(inputXStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputXStorage.inputField));
                                TriggerHelper.AddEventTriggerParams(inputYStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputYStorage.inputField));
                                TriggerHelper.IncreaseDecreaseButtons(inputXStorage);
                                TriggerHelper.IncreaseDecreaseButtons(inputYStorage);

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

        public IEnumerator RefreshCustomObjects()
        {
            LSHelpers.DeleteChildren(ModelsPopup.Content);

            var currentIndex = PlayerManager.GetPlayerModelIndex(playerModelIndex);
            var currentModel = PlayerManager.PlayerModels[currentIndex];

            var isDefault = PlayerModel.DefaultModels.Any(x => currentModel.basePart.id == x.basePart.id);

            if (isDefault)
                yield break;

            var createNew = PrefabEditor.inst.CreatePrefab.Duplicate(ModelsPopup.Content, "Create");
            var createNewButton = createNew.GetComponent<Button>();
            createNewButton.onClick.ClearAll();
            createNewButton.onClick.AddListener(delegate ()
            {
                var customObject = new PlayerModel.CustomObject(currentModel);
                var id = LSText.randomNumString(16);
                customObject.id = id;
                currentModel.customObjects.Add(id, customObject);

                CustomObjectID = id;

                StartCoroutine(RefreshCustomObjects());
                StartCoroutine(RefreshEditor());
            });

            var createNewText = createNew.transform.GetChild(0).GetComponent<Text>();
            createNewText.text = "Create custom object";

            EditorThemeManager.AddGraphic(createNewButton.image, ThemeGroup.Add, true);
            EditorThemeManager.AddGraphic(createNewText, ThemeGroup.Add_Text);

            int num = 0;
            foreach (var customObject in currentModel.customObjects)
            {
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(ModelsPopup.Content, customObject.Value.name);
                var text = gameObject.transform.GetChild(0).GetComponent<Text>();
                text.text = customObject.Value.name;

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(delegate ()
                {
                    CustomObjectID = customObject.Value.id;
                    StartCoroutine(RefreshEditor());
                    EditorManager.inst.HideDialog("Player Models Popup");
                });

                num++;
            }

            yield break;
        }

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

                for (int j = 0; j < ShapeManager.inst.Shapes2D.Count; j++)
                {
                    var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (j + 1).ToString(), j);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = ShapeManager.inst.Shapes2D[j][0].Icon;
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
                    shapeToggle.group = null;
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);

                    ui.shapeToggles.Add(shapeToggle);

                    ui.shapeOptionToggles.Add(new List<Toggle>());

                    if (j != 4 && j != 6)
                    {
                        if (!shapeSettings.Find((j + 1).ToString()))
                        {
                            var sh = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (j + 1).ToString());
                            LSHelpers.DeleteChildren(sh.transform, true);

                            var d = new List<GameObject>();
                            for (int k = 0; k < sh.transform.childCount; k++)
                                d.Add(sh.transform.GetChild(k).gameObject);

                            foreach (var go in d)
                                DestroyImmediate(go);
                            d.Clear();
                            d = null;
                        }

                        var so = shapeSettings.Find((j + 1).ToString());

                        var rect = (RectTransform)so;
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

                        for (int k = 0; k < ShapeManager.inst.Shapes2D[j].Count; k++)
                        {
                            var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(j), (k + 1).ToString(), k);
                            if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            {
                                image1.sprite = ShapeManager.inst.Shapes2D[j][k].Icon;
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
                            shapeOptionToggle.group = null;
                            EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);

                            ui.shapeOptionToggles[j].Add(shapeOptionToggle);

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

                        ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(j));
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

                PlayerManager.UpdatePlayers();
                RenderShape(ui);
            }

            if (type == 4)
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 74f);
                var child = shapeSettings.GetChild(4);
                child.AsRT().sizeDelta = new Vector2(351f, 74f);
                child.Find("Text").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.Find("Placeholder").GetComponent<Text>().alignment = TextAnchor.UpperLeft;
                child.GetComponent<InputField>().lineType = InputField.LineType.MultiLineNewline;
            }
            else
            {
                shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            shapeSettings.GetChild(type).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in ui.shapeToggles)
            {
                int index = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = type == index;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < UnmoddedShapeCounts.Length);

                if (RTEditor.ShowModdedUI || index < UnmoddedShapeCounts.Length)
                    toggle.onValueChanged.AddListener(delegate (bool _val)
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

                            PlayerManager.UpdatePlayers();
                            RenderShape(ui);
                        }
                    });

                num++;
            }

            if (type != 4 && type != 6)
            {
                num = 0;
                foreach (var toggle in ui.shapeOptionToggles[type])
                {
                    int index = num;
                    toggle.onValueChanged.ClearAll();
                    toggle.isOn = option == index;
                    toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < UnmoddedShapeCounts[type]);

                    if (RTEditor.ShowModdedUI || index < UnmoddedShapeCounts[type])
                        toggle.onValueChanged.AddListener(delegate (bool _val)
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

                                PlayerManager.UpdatePlayers();
                                RenderShape(ui);
                            }
                        });

                    num++;
                }
            }
            else if (generic is PlayerModel.CustomObject customObject)
            {
                if (customObject.shape.type == 4)
                {
                    var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                    textIF.onValueChanged.ClearAll();
                    textIF.text = customObject.text;
                    textIF.onValueChanged.AddListener(delegate (string _val)
                    {
                        CoreHelper.Log($"Set text to {_val}");
                        customObject.text = _val;

                        PlayerManager.UpdatePlayers();
                    });
                }
                else if (customObject.shape.type == 6)
                {
                    var select = shapeSettings.Find("7/select").GetComponent<Button>();
                    select.onClick.ClearAll();
                    select.onClick.AddListener(delegate ()
                    {
                        OpenImageSelector(ui);
                    });
                    shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(customObject.text) ? "No image selected" : customObject.text;

                    if (shapeSettings.Find("7/set"))
                        Destroy(shapeSettings.Find("7/set").gameObject);
                }
            }
            else
            {
                CoreHelper.Log($"Player shape cannot be text nor image.");
                if (generic != null)
                    generic.shape = ShapeManager.inst.Shapes2D[0][0];
                if (bullet != null)
                    bullet.shape = ShapeManager.inst.Shapes2D[0][0];
                if (pulse != null)
                    pulse.shape = ShapeManager.inst.Shapes2D[0][0];

                PlayerManager.UpdatePlayers();
                RenderShape(ui);
            }
        }

        public void OpenImageSelector(PlayerEditorUI ui)
        {
            var customObject = (PlayerModel.CustomObject)ui.Reference;

            var editorPath = RTFile.ApplicationDirectory + RTEditor.editorListSlash + EditorManager.inst.currentLoadedLevel;
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            CoreHelper.Log($"Selected file: {jpgFile}");
            if (!string.IsNullOrEmpty(jpgFile))
            {
                string jpgFileLocation = editorPath + "/" + Path.GetFileName(jpgFile);
                CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");

                var levelPath = jpgFile.Replace("\\", "/").Replace(editorPath + "/", "");
                CoreHelper.Log($"levelPath: {levelPath}");

                if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
                {
                    File.Copy(jpgFile, jpgFileLocation);
                    CoreHelper.Log($"Copied file to : {jpgFileLocation}");
                }
                else
                    jpgFileLocation = editorPath + "/" + levelPath;

                CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");
                customObject.text = jpgFileLocation.Replace(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1), "");

                PlayerManager.UpdatePlayers();
                RenderShape(ui);
            }
        }

        public int VisibilityToInt(string vis)
        {
            switch (vis)
            {
                case "isBoosting": return 0;
                case "isTakingHit": return 1;
                case "isZenMode": return 2;
                case "isHealthPercentageGreater": return 3;
                case "isHealthGreaterEquals": return 4;
                case "isHealthEquals": return 5;
                case "isHealthGreater": return 6;
                case "isPressingKey": return 7;
                default: return 0;
            }
        }

        public string IntToVisibility(int val)
        {
            switch (val)
            {
                case 0: return "isBoosting";
                case 1: return "isTakingHit";
                case 2: return "isZenMode";
                case 3: return "isHealthPercentageGreater";
                case 4: return "isHealthGreaterEquals";
                case 5: return "isHealthEquals";
                case 6: return "isHealthGreater";
                case 7: return "isPressingKey";
                default: return "isBoosting";
            }
        }

        public Tab CurrentTab { get; set; } = Tab.Base;
        public string searchTerm;
        public List<PlayerEditorUI> editorUIs = new List<PlayerEditorUI>();
        public List<PlayerEditorUI> EditorUIsActive => editorUIs.Where(x => x.GameObject.activeSelf).ToList();

        public class PlayerEditorUI
        {
            public string Name { get; set; }
            public GameObject GameObject { get; set; }
            public Tab Tab { get; set; }
            public RTEditor.EditorProperty.ValueType ValueType { get; set; }
            public int Index { get; set; }

            public object Reference { get; set; }

            public bool updatedShapes = false;
            public List<Toggle> shapeToggles = new List<Toggle>();
            public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();

            public override string ToString() => $"{Tab} - {Name}";
        }
    }
}

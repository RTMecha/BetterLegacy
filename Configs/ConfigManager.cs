using BetterLegacy.Components;
using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using LSFunctions;
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Configs
{
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager inst;

        public UICanvas canvas;
        public GameObject configBase;
        public Transform subTabs;
        public InputFieldStorage pageFieldStorage;
        public Transform content;
        public Text descriptionText;

        public GameObject numberFieldStorage;

        public List<BaseConfig> configs = new List<BaseConfig>();

        public static void Init()
        {
            var gameObject = new GameObject("ConfigManager");
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<ConfigManager>();
        }

        void Awake()
        {
            inst = this;
            canvas = UIManager.GenerateUICanvas("Config Canvas", null, true);

            configBase = Creator.NewUIObject("Base", canvas.GameObject.transform);
            configBase.transform.AsRT().anchoredPosition = Vector2.zero;
            configBase.transform.AsRT().sizeDelta = new Vector2(1000f, 800f);
            var configBaseImage = configBase.AddComponent<Image>();

            EditorThemeManager.ApplyGraphic(configBaseImage, ThemeGroup.Background_1, true);

            var selectable = configBase.AddComponent<SelectGUI>();
            selectable.target = configBase.transform;

            var panel = Creator.NewUIObject("Panel", configBase.transform);
            UIManager.SetRectTransform(panel.transform.AsRT(), Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 32f));

            var panelImage = panel.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(panelImage, ThemeGroup.Background_1, true);

            var title = Creator.NewUIObject("Title", panel.transform);
            UIManager.SetRectTransform(title.transform.AsRT(), new Vector2(2f, 0f), Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, -8f));

            var titleText = title.AddComponent<Text>();
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.font = Font.GetDefault();
            titleText.fontSize = 20;
            titleText.text = "Config Manager";
            EditorThemeManager.ApplyLightText(titleText);

            var close = Creator.NewUIObject("x", panel.transform);
            UIManager.SetRectTransform(close.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));

            var closeImage = close.AddComponent<Image>();
            var closeButton = close.AddComponent<Button>();
            closeButton.image = closeImage;

            EditorThemeManager.ApplySelectable(closeButton, ThemeGroup.Close);

            closeButton.onClick.AddListener(delegate ()
            {
                Hide();
            });

            var closeX = Creator.NewUIObject("Image", close.transform);
            UIManager.SetRectTransform(closeX.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f));

            var closeXImage = closeX.AddComponent<Image>();
            closeXImage.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_close.png");

            EditorThemeManager.ApplyGraphic(closeXImage, ThemeGroup.Close_X);

            var tabs = Creator.NewUIObject("Tabs", configBase.transform);
            UIManager.SetRectTransform(tabs.transform.AsRT(), Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 42f));

            var tabsImage = tabs.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(tabsImage, ThemeGroup.Background_3, true);

            var tabsHorizontalLayout = tabs.AddComponent<HorizontalLayoutGroup>();

            string[] tabTitles = new string[]
            {
                "Core", // 1
                "Arcade", // 2
                "Editor", // 3
                "Events", // 4
                "Players", // 5
                "Modifiers", // 6
                "Menus", // 7
                "Example", // 8
            };

            Color[] tabColors = new Color[]
            {
                new Color(0.18f, 0.4151f, 1f, 1f), // 1
                new Color(1f, 0.143f, 0.22f, 1f), // 2
                new Color(0.5694f, 0.3f, 1f, 1f), // 3
                new Color(0.1509f, 0.7096f, 1f, 1f), // 4
                new Color(1f, 0.4956f, 0.5192f, 1f), // 5
                new Color(1f, 0.4972f, 0f, 1f), // 6
                new Color(0.86f, 0.86f, 0.86f, 1f), // 7
                new Color(0.1158f, 0.3352f, 1f, 1f), // 8
            };

            for (int i = 0; i < tabTitles.Length; i++)
            {
                var index = i;

                var tab = Creator.NewUIObject($"Tab {i}", tabs.transform);

                var tabBase = Creator.NewUIObject("Image", tab.transform);
                UIManager.SetRectTransform(tabBase.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f));
                var tabBaseImage = tabBase.AddComponent<Image>();

                tabBaseImage.color = tabColors[i];

                var tabTitle = Creator.NewUIObject("Title", tabBase.transform);
                UIManager.SetRectTransform(tabTitle.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
                var tabTitleText = tabTitle.AddComponent<Text>();
                tabTitleText.alignment = TextAnchor.MiddleCenter;
                tabTitleText.font = Font.GetDefault();
                tabTitleText.fontSize = 18;
                tabTitleText.text = tabTitles[Mathf.Clamp(i, 0, tabTitles.Length - 1)];

                tabTitle.AddComponent<ContrastColors>().Init(tabTitleText, tabBaseImage);

                var tabButton = tabBase.AddComponent<Button>();
                tabButton.image = tabBaseImage;
                tabButton.onClick.AddListener(delegate ()
                {
                    SetTab(index);
                });

                EditorThemeManager.ApplyGraphic(tabBaseImage, ThemeGroup.Null, true);
            }

            var subTabs = Creator.NewUIObject("Tabs", configBase.transform);
            UIManager.SetRectTransform(subTabs.transform.AsRT(), Vector2.zero, new Vector2(0f, 0.948f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(132f, 0f));

            var subTabsImage = subTabs.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(subTabsImage, ThemeGroup.Background_2, true);

            var subTabsVerticalLayout = subTabs.AddComponent<VerticalLayoutGroup>();
            subTabsVerticalLayout.childControlHeight = false;
            subTabsVerticalLayout.childForceExpandHeight = false;

            this.subTabs = subTabs.transform;

            var content = Creator.NewUIObject("Content", configBase.transform);
            this.content = content.transform;
            var contentVerticalLayoutGroup = content.AddComponent<VerticalLayoutGroup>();
            contentVerticalLayoutGroup.spacing = 8f;
            contentVerticalLayoutGroup.childControlHeight = false;
            contentVerticalLayoutGroup.childForceExpandHeight = false;
            UIManager.SetRectTransform(this.content.AsRT(), Vector2.zero, new Vector2(0.995f, 0.94f), new Vector2(0.136f, 0.136f), new Vector2(0.5f, 0.5f), Vector2.zero);

            var pagePanel = Creator.NewUIObject("Page Panel", configBase.transform);
            UIManager.SetRectTransform(pagePanel.transform.AsRT(), Vector2.zero, new Vector2(1f, 0f), new Vector2(0.132f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f));

            var pagePanelImage = pagePanel.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(pagePanelImage, ThemeGroup.Background_3, true);

            // Prefab
            {
                var page = Creator.NewUIObject("Page", transform);
                UIManager.SetRectTransform(page.transform.AsRT(), new Vector2(580f, 32f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                numberFieldStorage = page;

                var pageHorizontalLayoutGroup = page.AddComponent<HorizontalLayoutGroup>();
                pageHorizontalLayoutGroup.spacing = 8f;
                pageHorizontalLayoutGroup.childControlHeight = false;
                pageHorizontalLayoutGroup.childControlWidth = false;
                pageHorizontalLayoutGroup.childForceExpandHeight = true;
                pageHorizontalLayoutGroup.childForceExpandWidth = false;

                var pageInput = Creator.NewUIObject("input", page.transform);
                var pageInputImage = pageInput.AddComponent<Image>();
                var pageInputField = pageInput.AddComponent<InputField>();
                var pageInputLayoutElement = pageInput.AddComponent<LayoutElement>();
                pageInputLayoutElement.preferredWidth = 10000f;
                UIManager.SetRectTransform(pageInput.transform.AsRT(), Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(151f, 32f));

                var pageInputPlaceholder = Creator.NewUIObject("Placeholder", pageInput.transform);
                var pageInputPlaceholderText = pageInputPlaceholder.AddComponent<Text>();
                pageInputPlaceholderText.alignment = TextAnchor.MiddleCenter;
                pageInputPlaceholderText.font = Font.GetDefault();
                pageInputPlaceholderText.fontSize = 20;
                pageInputPlaceholderText.fontStyle = FontStyle.Italic;
                pageInputPlaceholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageInputPlaceholderText.verticalOverflow = VerticalWrapMode.Overflow;
                pageInputPlaceholderText.color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
                UIManager.SetRectTransform(pageInputPlaceholder.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f));

                var pageInputText = Creator.NewUIObject("Text", pageInput.transform);
                var pageInputTextText = pageInputText.AddComponent<Text>();
                pageInputTextText.alignment = TextAnchor.MiddleCenter;
                pageInputTextText.font = Font.GetDefault();
                pageInputTextText.fontSize = 20;
                pageInputTextText.fontStyle = FontStyle.Normal;
                pageInputTextText.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageInputTextText.verticalOverflow = VerticalWrapMode.Overflow;
                pageInputTextText.color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
                UIManager.SetRectTransform(pageInputText.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f));

                pageInputField.placeholder = pageInputPlaceholderText;
                pageInputField.textComponent = pageInputTextText;
                pageInputField.characterValidation = InputField.CharacterValidation.None;
                pageInputField.contentType = InputField.ContentType.Standard;
                pageInputField.inputType = InputField.InputType.Standard;
                pageInputField.keyboardType = TouchScreenKeyboardType.Default;
                pageInputField.lineType = InputField.LineType.SingleLine;

                pageInputField.onValueChanged.ClearAll();
                pageInputField.text = "0";

                var leftGreater = Creator.NewUIObject("<<", page.transform);
                var leftGreaterImage = leftGreater.AddComponent<Image>();
                leftGreaterImage.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_left_double.png");
                var leftGreaterButton = leftGreater.AddComponent<Button>();
                leftGreaterButton.image = leftGreaterImage;
                var leftGreaterLayoutElement = leftGreater.AddComponent<LayoutElement>();
                leftGreaterLayoutElement.minWidth = 32f;
                leftGreaterLayoutElement.preferredWidth = 32f;
                UIManager.SetRectTransform(leftGreater.transform.AsRT(), new Vector2(175f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

                var left = Creator.NewUIObject("<", page.transform);
                var leftImage = left.AddComponent<Image>();
                leftImage.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_left_small.png");
                var leftButton = left.AddComponent<Button>();
                leftButton.image = leftImage;
                var leftLayoutElement = left.AddComponent<LayoutElement>();
                leftLayoutElement.minWidth = 32f;
                leftLayoutElement.preferredWidth = 32f;
                UIManager.SetRectTransform(left.transform.AsRT(), new Vector2(199f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, 32f));

                var right = Creator.NewUIObject(">", page.transform);
                var rightImage = right.AddComponent<Image>();
                rightImage.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_right_small.png");
                var rightButton = right.AddComponent<Button>();
                rightButton.image = rightImage;
                var rightLayoutElement = right.AddComponent<LayoutElement>();
                rightLayoutElement.minWidth = 32f;
                rightLayoutElement.preferredWidth = 32f;
                UIManager.SetRectTransform(right.transform.AsRT(), new Vector2(247f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, 32f));

                var rightGreater = Creator.NewUIObject(">>", page.transform);
                var rightGreaterImage = rightGreater.AddComponent<Image>();
                rightGreaterImage.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_right_double.png");
                var rightGreaterButton = rightGreater.AddComponent<Button>();
                rightGreaterButton.image = rightGreaterImage;
                var rightGreaterLayoutElement = rightGreater.AddComponent<LayoutElement>();
                rightGreaterLayoutElement.minWidth = 32f;
                rightGreaterLayoutElement.preferredWidth = 32f;
                UIManager.SetRectTransform(rightGreater.transform.AsRT(), new Vector2(271f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));

                var fieldStorage = page.AddComponent<InputFieldStorage>();
                fieldStorage.inputField = pageInputField;
                fieldStorage.leftGreaterButton = leftGreaterButton;
                fieldStorage.leftButton = leftButton;
                fieldStorage.rightButton = rightButton;
                fieldStorage.rightGreaterButton = rightGreaterButton;

            }

            var pageObject = numberFieldStorage.Duplicate(pagePanel.transform, "Page");
            UIManager.SetRectTransform(pageObject.transform.AsRT(), new Vector2(580f, 32f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

            pageFieldStorage = pageObject.GetComponent<InputFieldStorage>();
            EditorThemeManager.ApplyInputField(pageFieldStorage.inputField);
            EditorThemeManager.ApplySelectable(pageFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(pageFieldStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(pageFieldStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(pageFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            var descriptionBase = Creator.NewUIObject("Description Base", configBase.transform);
            UIManager.SetRectTransform(descriptionBase.transform.AsRT(), new Vector2(500f, -180f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0.5f), new Vector2(240f, 350f));
            var descriptionBaseImage = descriptionBase.AddComponent<Image>();

            var description = Creator.NewUIObject("Description", descriptionBase.transform);
            UIManager.SetRectTransform(description.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f));
            descriptionText = description.AddComponent<Text>();
            descriptionText.font = Font.GetDefault();
            descriptionText.fontSize = 18;
            descriptionText.text = "Hover over a setting to get the description.";

            EditorThemeManager.ApplyGraphic(descriptionBaseImage, ThemeGroup.Background_2, true, roundedSide: SpriteManager.RoundedSide.Left);
            EditorThemeManager.ApplyLightText(descriptionText);

            configs.Add(CoreConfig.Instance);
            configs.Add(ArcadeConfig.Instance);
            configs.Add(EditorConfig.Instance);
            configs.Add(EventsConfig.Instance);
            configs.Add(PlayerConfig.Instance);
            configs.Add(ModifiersConfig.Instance);
            configs.Add(MenuConfig.Instance);
            configs.Add(ExampleConfig.Instance);

            Hide();

            configBase.AddComponent<ConfigManagerUI>();
        }

        void Update()
        {
            if (Input.GetKeyDown(CoreConfig.Instance.OpenConfigKey.Value))
                (Active ? (Action)Hide : Show).Invoke();
        }

        void FixedUpdate()
        {
            if (canvas != null)
                canvas.CanvasScaler.referenceResolution = new Vector2(Screen.width, Screen.height);
        }

        public bool Active => configBase && configBase.activeSelf;

        public void Show() => configBase.SetActive(true);

        public void Hide() => configBase.SetActive(false);

        public int maxSettingsPerPage = 15;
        public int currentSubTabPage;
        public int currentSubTab;
        public int currentTab;
        public int lastTab;
        public List<string> subTabSections = new List<string>();
        public void SetTab(int tabIndex)
        {
            if (currentTab != tabIndex)
            {
                currentSubTab = 0;
                currentSubTabPage = 0;
            }

            lastTab = currentTab;
            currentTab = tabIndex;
            LSHelpers.DeleteChildren(this.subTabs);
            var config = configs[Mathf.Clamp(tabIndex, 0, configs.Count - 1)];

            subTabSections.Clear();

            int index = 0;
            for (int i = 0; i < config.Settings.Count; i++)
            {
                if (subTabSections.Any(x => x == config.Settings[i].Section))
                    continue;

                subTabSections.Add(config.Settings[i].Section);

                int currentIndex = index;

                var tab = Creator.NewUIObject($"Tab {i}", this.subTabs.transform);
                tab.transform.AsRT().sizeDelta = new Vector2(0f, 32f);

                var tabBase = Creator.NewUIObject("Image", tab.transform);
                UIManager.SetRectTransform(tabBase.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f));
                var tabBaseImage = tabBase.AddComponent<Image>();

                //tabBaseImage.color = tabColors[i];

                var tabTitle = Creator.NewUIObject("Title", tabBase.transform);
                UIManager.SetRectTransform(tabTitle.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero);
                var tabTitleText = tabTitle.AddComponent<Text>();
                tabTitleText.alignment = TextAnchor.MiddleCenter;
                tabTitleText.font = Font.GetDefault();
                tabTitleText.fontSize = 15;
                tabTitleText.text = config.Settings[i].Section;

                var tabButton = tabBase.AddComponent<Button>();
                tabButton.image = tabBaseImage;
                tabButton.onClick.AddListener(delegate ()
                {
                    currentSubTab = currentIndex;
                    currentSubTabPage = 0;
                    RefreshSettings();
                });

                EditorThemeManager.ApplySelectable(tabButton, ThemeGroup.Function_2);
                EditorThemeManager.ApplyGraphic(tabTitleText, ThemeGroup.Function_2_Text);

                index++;
            }

            RefreshSettings();
        }

        public void RefreshSettings()
        {
            var config = configs[Mathf.Clamp(currentTab, 0, configs.Count - 1)];

            currentSubTab = Mathf.Clamp(currentSubTab, 0, subTabSections.Count - 1);

            var settings = config.Settings.Where(x => subTabSections[currentSubTab] == x.Section);
            var page = currentSubTabPage + 1;
            var max = page * maxSettingsPerPage;

            pageFieldStorage.inputField.onValueChanged.ClearAll();
            pageFieldStorage.inputField.text = currentSubTabPage.ToString();
            pageFieldStorage.inputField.onValueChanged.AddListener(delegate (string _val)
            {
                if (int.TryParse(_val, out int p))
                {
                    currentSubTabPage = p;
                    RefreshSettings();
                }
            });

            if (settings.Count() / maxSettingsPerPage != 0)
                TriggerHelper.AddEventTriggerParams(pageFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(pageFieldStorage.inputField, max: settings.Count() / maxSettingsPerPage));
            else
                TriggerHelper.AddEventTriggerParams(pageFieldStorage.inputField.gameObject);

            pageFieldStorage.leftGreaterButton.onClick.ClearAll();
            pageFieldStorage.leftGreaterButton.onClick.AddListener(delegate ()
            {
                currentSubTabPage = 0;
                RefreshSettings();
            });
            pageFieldStorage.leftButton.onClick.ClearAll();
            pageFieldStorage.leftButton.onClick.AddListener(delegate ()
            {
                if (int.TryParse(pageFieldStorage.inputField.text, out int p))
                {
                    currentSubTabPage = Mathf.Clamp(p - 1, 0, settings.Count() / maxSettingsPerPage);
                    RefreshSettings();
                }
            });
            pageFieldStorage.rightButton.onClick.ClearAll();
            pageFieldStorage.rightButton.onClick.AddListener(delegate ()
            {
                if (int.TryParse(pageFieldStorage.inputField.text, out int p))
                {
                    currentSubTabPage = Mathf.Clamp(p + 1, 0, settings.Count() / maxSettingsPerPage);
                    RefreshSettings();
                }
            });
            pageFieldStorage.rightGreaterButton.onClick.ClearAll();
            pageFieldStorage.rightGreaterButton.onClick.AddListener(delegate ()
            {
                currentSubTabPage = settings.Count() / maxSettingsPerPage;
                RefreshSettings();
            });

            LSHelpers.DeleteChildren(content);
            for (int i = 0; i < settings.Count(); i++)
            {
                var setting = settings.ElementAt(i);

                if (i >= max - maxSettingsPerPage && i < max)
                {
                    CoreHelper.Log($"Setting: {setting.Key}");

                    var gameObject = Creator.NewUIObject("Setting", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(830f, 38f);

                    gameObject.AddComponent<HoverConfig>().Init(setting);

                    var image = gameObject.AddComponent<Image>();

                    var label = Creator.NewUIObject("Label", gameObject.transform);
                    UIManager.SetRectTransform(label.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-12f, 0f));

                    var labelText = label.AddComponent<Text>();
                    labelText.alignment = TextAnchor.MiddleLeft;
                    labelText.font = Font.GetDefault();
                    labelText.fontSize = 16;
                    labelText.text = setting.Key;

                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.List_Button_1_Normal, true);
                    EditorThemeManager.ApplyLightText(labelText);

                    var type = setting.BoxedValue.GetType();

                    if (type == typeof(bool))
                    {
                        var boolSetting = (Setting<bool>)setting;
                        var boolean = Creator.NewUIObject("Toggle", gameObject.transform);
                        UIManager.SetRectTransform(boolean.transform.AsRT(), Vector2.zero, new Vector2(0.975f, 0.5f), new Vector2(0.975f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                        var booleanImage = boolean.AddComponent<Image>();

                        var checkmark = Creator.NewUIObject("Checkmark", boolean.transform);
                        UIManager.SetRectTransform(checkmark.transform.AsRT(), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(-8f, -8f));
                        var checkmarkImage = checkmark.AddComponent<Image>();
                        checkmarkImage.sprite = SpriteManager.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_checkmark.png");

                        var booleanToggle = boolean.AddComponent<Toggle>();
                        booleanToggle.image = booleanImage;
                        booleanToggle.graphic = checkmarkImage;
                        booleanToggle.onValueChanged.ClearAll();
                        booleanToggle.isOn = boolSetting.Value;
                        booleanToggle.onValueChanged.AddListener(delegate (bool _val)
                        {
                            setting.BoxedValue = _val;
                        });

                        EditorThemeManager.ApplyToggle(booleanToggle);
                    }

                    if (type == typeof(int))
                    {
                        var intSetting = (Setting<int>)setting;
                        var integer = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                        UIManager.SetRectTransform(integer.transform.AsRT(), new Vector2(560f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                        var integerStorage = integer.GetComponent<InputFieldStorage>();

                        integerStorage.inputField.onValueChanged.ClearAll();
                        integerStorage.inputField.text = intSetting.Value.ToString();
                        integerStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (int.TryParse(_val, out int value))
                                setting.BoxedValue = value;
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(integerStorage.inputField, min: intSetting.MinValue, max: intSetting.MaxValue, t: integer.transform);
                        TriggerHelper.AddEventTriggerParams(integer.gameObject, TriggerHelper.ScrollDeltaInt(integerStorage.inputField, min: intSetting.MinValue, max: intSetting.MaxValue));

                        EditorThemeManager.ApplyInputField(integerStorage.inputField);
                        EditorThemeManager.ApplySelectable(integerStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(integerStorage.leftButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(integerStorage.rightButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(integerStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                    }

                    if (type == typeof(float))
                    {
                        var floatSetting = (Setting<float>)setting;
                        var floatingPoint = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                        UIManager.SetRectTransform(floatingPoint.transform.AsRT(), new Vector2(560f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                        var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                        floatingPointStorage.inputField.onValueChanged.ClearAll();
                        floatingPointStorage.inputField.text = floatSetting.Value.ToString();
                        floatingPointStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            if (float.TryParse(_val, out float value))
                                setting.BoxedValue = value;
                        });

                        TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: floatSetting.MinValue, max: floatSetting.MaxValue, t: floatingPoint.transform);
                        TriggerHelper.AddEventTriggerParams(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: floatSetting.MinValue, max: floatSetting.MaxValue));

                        EditorThemeManager.ApplyInputField(floatingPointStorage.inputField);
                        EditorThemeManager.ApplySelectable(floatingPointStorage.leftGreaterButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(floatingPointStorage.leftButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(floatingPointStorage.rightButton, ThemeGroup.Function_2, false);
                        EditorThemeManager.ApplySelectable(floatingPointStorage.rightGreaterButton, ThemeGroup.Function_2, false);
                    }

                    if (type == typeof(Vector2))
                    {
                        var vector2Setting = (Setting<Vector2>)setting;

                        // X
                        {
                            var floatingPoint = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                            UIManager.SetRectTransform(floatingPoint.transform.AsRT(), new Vector2(420f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                            var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                            floatingPointStorage.inputField.onValueChanged.ClearAll();
                            floatingPointStorage.inputField.text = vector2Setting.Value.x.ToString();
                            floatingPointStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float value))
                                    setting.BoxedValue = new Vector2(value, vector2Setting.Value.y);
                            });

                            TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: vector2Setting.MinValue.x, max: vector2Setting.MaxValue.x, t: floatingPoint.transform);
                            TriggerHelper.AddEventTriggerParams(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: vector2Setting.MinValue.x, max: vector2Setting.MaxValue.x));

                            Destroy(floatingPointStorage.leftGreaterButton.gameObject);
                            Destroy(floatingPointStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.ApplyInputField(floatingPointStorage.inputField);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.rightButton, ThemeGroup.Function_2, false);
                        }

                        // Y
                        {
                            var floatingPoint = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                            UIManager.SetRectTransform(floatingPoint.transform.AsRT(), new Vector2(640f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f));

                            var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                            floatingPointStorage.inputField.onValueChanged.ClearAll();
                            floatingPointStorage.inputField.text = vector2Setting.Value.y.ToString();
                            floatingPointStorage.inputField.onValueChanged.AddListener(delegate (string _val)
                            {
                                if (float.TryParse(_val, out float value))
                                    setting.BoxedValue = new Vector2(vector2Setting.Value.x, value);
                            });

                            TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: vector2Setting.MinValue.y, max: vector2Setting.MaxValue.y, t: floatingPoint.transform);
                            TriggerHelper.AddEventTriggerParams(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: vector2Setting.MinValue.y, max: vector2Setting.MaxValue.y));

                            Destroy(floatingPointStorage.leftGreaterButton.gameObject);
                            Destroy(floatingPointStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.ApplyInputField(floatingPointStorage.inputField);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.rightButton, ThemeGroup.Function_2, false);
                        }
                    }

                    if (type == typeof(string))
                    {
                        var stringSetting = (Setting<string>)setting;

                        var stringObject = numberFieldStorage.transform.Find("input").gameObject.Duplicate(gameObject.transform, "Input");
                        stringObject.transform.AsRT().sizeDelta = new Vector2(438f, 32f);
                        var stringInputField = stringObject.GetComponent<InputField>();
                        stringInputField.onValueChanged.ClearAll();
                        stringInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                        ((Text)stringInputField.placeholder).alignment = TextAnchor.MiddleLeft;
                        stringInputField.text = stringSetting.Value;
                        stringInputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            stringSetting.Value = _val;
                        });

                        EditorThemeManager.ApplyInputField(stringInputField);
                    }

                    if (type == typeof(Color))
                    {
                        var colorSetting = (Setting<Color>)setting;

                        var colorObject = Creator.NewUIObject("Color", gameObject.transform);
                        UIManager.SetRectTransform(colorObject.transform.AsRT(), new Vector2(160f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(32f, 32f));
                        var colorImage = colorObject.AddComponent<Image>();
                        colorImage.color = colorSetting.Value;

                        EditorThemeManager.ApplyGraphic(colorImage, ThemeGroup.Null, true);

                        var stringObject = numberFieldStorage.transform.Find("input").gameObject.Duplicate(gameObject.transform, "Input");
                        stringObject.transform.AsRT().anchoredPosition = new Vector2(615f, -3f);
                        stringObject.transform.AsRT().sizeDelta = new Vector2(238f, 32f);
                        var stringInputField = stringObject.GetComponent<InputField>();
                        stringInputField.onValueChanged.ClearAll();
                        stringInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                        ((Text)stringInputField.placeholder).alignment = TextAnchor.MiddleLeft;
                        stringInputField.text = LSColors.ColorToHex(colorSetting.Value);
                        stringInputField.onValueChanged.AddListener(delegate (string _val)
                        {
                            colorSetting.Value = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : LSColors.HexToColor(_val);
                            colorImage.color = colorSetting.Value;
                        });

                        EditorThemeManager.ApplyInputField(stringInputField);
                    }

                    if (type.IsEnum)
                    {
                        var enumObject = UIManager.GenerateUIDropdown("Dropdown", gameObject.transform);
                        var dropdown = (Dropdown)enumObject["Dropdown"];
                        var hide = ((GameObject)enumObject["GameObject"]).AddComponent<HideDropdownOptions>();

                        UIManager.SetRectTransform(dropdown.transform.AsRT(), new Vector2(276f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(300f, 32f));

                        dropdown.onValueChanged.ClearAll();
                        dropdown.options.Clear();
                        hide.DisabledOptions = new List<bool>();

                        var enums = Enum.GetValues(type);

                        for (int j = 0; j < enums.Length; j++)
                        {
                            var name = Enum.GetName(type, j);
                            hide.remove = true;
                            hide.DisabledOptions.Add(name == null);

                            dropdown.options.Add(new Dropdown.OptionData(name ?? "Invalid Value"));
                        }

                        dropdown.value = (int)setting.BoxedValue;
                        dropdown.onValueChanged.AddListener(delegate (int _val)
                        {
                            setting.BoxedValue = _val;
                        });

                        EditorThemeManager.ApplyDropdown(dropdown);
                    }
                }
            }
        }
    }

    public class ConfigManagerUI : MonoBehaviour
    {
        void OnEnable() => ConfigManager.inst.SetTab(ConfigManager.inst.currentTab);
    }

    public class HoverConfig : MonoBehaviour, IPointerEnterHandler
    {
        public BaseSetting Setting { get; set; }

        public void Init(BaseSetting baseSetting) => Setting = baseSetting;

        public void OnPointerEnter(PointerEventData pointerEventData)
        {
            ConfigManager.inst.descriptionText.text = Setting.Description;
        }
    }
}

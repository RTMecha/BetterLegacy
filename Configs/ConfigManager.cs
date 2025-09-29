using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Configs
{
    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager inst;

        #region Fields

        public bool watchingKeybind;
        public Action<KeyCode> onSelectKey;

        #region UI

        public UICanvas canvas;
        public GameObject configBase;
        public Transform subTabs;
        public InputFieldStorage pageFieldStorage;
        public Transform content;
        public Text descriptionText;

        public GameObject numberFieldStorage;

        public RectTransform tabs;

        #endregion

        #region Tabs & Pages

        public const int MAX_SETTINGS_PER_PAGE = 14;
        public int currentSubTabPage;
        public int currentSubTab;
        public int currentTab;
        public int lastTab;
        public string searchTerm;
        public List<string> subTabSections = new List<string>();

        #endregion

        #endregion

        public static void Init() => Creator.NewPersistentGameObject(nameof(ConfigManager)).AddComponent<ConfigManager>();

        void Awake()
        {
            inst = this;
            canvas = UIManager.GenerateUICanvas("Config Canvas", null, true);
            canvas.Canvas.scaleFactor = 1f;
            canvas.CanvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            configBase = Creator.NewUIObject("Base", canvas.GameObject.transform);
            RectValues.Default.SizeDelta(1000f, 800f).AssignToRectTransform(configBase.transform.AsRT());
            var configBaseImage = configBase.AddComponent<Image>();

            EditorThemeManager.ApplyGraphic(configBaseImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

            var selectable = configBase.AddComponent<SelectGUI>();
            selectable.target = configBase.transform;

            var panel = Creator.NewUIObject("Panel", configBase.transform);
            new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 32f)).AssignToRectTransform(panel.transform.AsRT());

            var panelImage = panel.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(panelImage, ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            var title = Creator.NewUIObject("Title", panel.transform);
            RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f).AssignToRectTransform(title.transform.AsRT());

            var titleText = title.AddComponent<Text>();
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.font = Font.GetDefault();
            titleText.fontSize = 20;
            titleText.text = "Config Manager";
            EditorThemeManager.ApplyLightText(titleText);

            var close = Creator.NewUIObject("x", panel.transform);
            RectValues.TopRightAnchored.SizeDelta(32f, 32f).AssignToRectTransform(close.transform.AsRT());

            var closeImage = close.AddComponent<Image>();
            var closeButton = close.AddComponent<Button>();
            closeButton.image = closeImage;

            EditorThemeManager.ApplySelectable(closeButton, ThemeGroup.Close);

            closeButton.onClick.AddListener(Hide);

            var closeX = Creator.NewUIObject("Image", close.transform);
            RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(closeX.transform.AsRT());

            var closeXImage = closeX.AddComponent<Image>();
            closeXImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/close{FileFormat.PNG.Dot()}"));

            EditorThemeManager.ApplyGraphic(closeXImage, ThemeGroup.Close_X);

            var tabs = Creator.NewUIObject("Tabs", configBase.transform);
            this.tabs = tabs.transform.AsRT();
            new RectValues(Vector2.zero, Vector2.one, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 42f)).AssignToRectTransform(this.tabs);

            var tabsImage = tabs.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(tabsImage, ThemeGroup.Background_3, true);

            var tabsHorizontalLayout = tabs.AddComponent<HorizontalLayoutGroup>();

            UpdateTabs();

            var subTabs = Creator.NewUIObject("Tabs", configBase.transform);
            new RectValues(Vector2.zero, new Vector2(0f, 0.948f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(132f, 0f)).AssignToRectTransform(subTabs.transform.AsRT());

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
            new RectValues(Vector2.zero, new Vector2(0.995f, 0.88f), new Vector2(0.136f, 0.136f), new Vector2(0.5f, 0.5f), Vector2.zero).AssignToRectTransform(this.content.AsRT());

            var pagePanel = Creator.NewUIObject("Page Panel", configBase.transform);
            new RectValues(Vector2.zero, new Vector2(1f, 0f), new Vector2(0.132f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 64f)).AssignToRectTransform(pagePanel.transform.AsRT());

            var pagePanelImage = pagePanel.AddComponent<Image>();
            EditorThemeManager.ApplyGraphic(pagePanelImage, ThemeGroup.Background_3, true);

            // Prefab
            {
                var page = Creator.NewUIObject("Page", transform);
                RectValues.BottomLeftAnchored.AnchoredPosition(580f, 32f).Pivot(0.5f, 0.5f).SizeDelta(0f, 32f).AssignToRectTransform(page.transform.AsRT());

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
                RectValues.LeftAnchored.SizeDelta(151f, 32f).AssignToRectTransform(pageInput.transform.AsRT());

                var pageInputPlaceholder = Creator.NewUIObject("Placeholder", pageInput.transform);
                var pageInputPlaceholderText = pageInputPlaceholder.AddComponent<Text>();
                pageInputPlaceholderText.alignment = TextAnchor.MiddleLeft;
                pageInputPlaceholderText.font = Font.GetDefault();
                pageInputPlaceholderText.fontSize = 20;
                pageInputPlaceholderText.fontStyle = FontStyle.Italic;
                pageInputPlaceholderText.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageInputPlaceholderText.verticalOverflow = VerticalWrapMode.Overflow;
                pageInputPlaceholderText.color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
                pageInputPlaceholderText.text = "Set number...";
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(pageInputPlaceholder.transform.AsRT());

                var pageInputText = Creator.NewUIObject("Text", pageInput.transform);
                var pageInputTextText = pageInputText.AddComponent<Text>();
                pageInputTextText.alignment = TextAnchor.MiddleCenter;
                pageInputTextText.font = Font.GetDefault();
                pageInputTextText.fontSize = 20;
                pageInputTextText.fontStyle = FontStyle.Normal;
                pageInputTextText.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageInputTextText.verticalOverflow = VerticalWrapMode.Overflow;
                pageInputTextText.color = new Color(0.1961f, 0.1961f, 0.1961f, 0.5f);
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(pageInputText.transform.AsRT());

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
                leftGreaterImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/left_double{FileFormat.PNG.Dot()}"));
                var leftGreaterButton = leftGreater.AddComponent<Button>();
                leftGreaterButton.image = leftGreaterImage;
                var leftGreaterLayoutElement = leftGreater.AddComponent<LayoutElement>();
                leftGreaterLayoutElement.minWidth = 32f;
                leftGreaterLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(175f, -16f).Pivot(0.5f, 0.5f).SizeDelta(32f, 32f).AssignToRectTransform(leftGreater.transform.AsRT());

                var left = Creator.NewUIObject("<", page.transform);
                var leftImage = left.AddComponent<Image>();
                leftImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/left_small{FileFormat.PNG.Dot()}"));
                var leftButton = left.AddComponent<Button>();
                leftButton.image = leftImage;
                var leftLayoutElement = left.AddComponent<LayoutElement>();
                leftLayoutElement.minWidth = 32f;
                leftLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(199f, -16f).SizeDelta(16f, 32f).AssignToRectTransform(left.transform.AsRT());

                var right = Creator.NewUIObject(">", page.transform);
                var rightImage = right.AddComponent<Image>();
                rightImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/right_small{FileFormat.PNG.Dot()}"));
                var rightButton = right.AddComponent<Button>();
                rightButton.image = rightImage;
                var rightLayoutElement = right.AddComponent<LayoutElement>();
                rightLayoutElement.minWidth = 32f;
                rightLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(247f, -16f).SizeDelta(16f, 32f).AssignToRectTransform(right.transform.AsRT());

                var rightGreater = Creator.NewUIObject(">>", page.transform);
                var rightGreaterImage = rightGreater.AddComponent<Image>();
                rightGreaterImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/right_double{FileFormat.PNG.Dot()}"));
                var rightGreaterButton = rightGreater.AddComponent<Button>();
                rightGreaterButton.image = rightGreaterImage;
                var rightGreaterLayoutElement = rightGreater.AddComponent<LayoutElement>();
                rightGreaterLayoutElement.minWidth = 32f;
                rightGreaterLayoutElement.preferredWidth = 32f;
                RectValues.LeftAnchored.AnchoredPosition(271f, -16f).Pivot(0.5f, 0.5f).SizeDelta(32f, 32f).AssignToRectTransform(rightGreater.transform.AsRT());

                var fieldStorage = page.AddComponent<InputFieldStorage>();
                fieldStorage.inputField = pageInputField;
                fieldStorage.leftGreaterButton = leftGreaterButton;
                fieldStorage.leftButton = leftButton;
                fieldStorage.rightButton = rightButton;
                fieldStorage.rightGreaterButton = rightGreaterButton;
            }

            var searchField = numberFieldStorage.transform.Find("input").gameObject.Duplicate(configBase.transform);
            RectValues.LeftAnchored.AnchoredPosition(134f, -50f).SizeDelta(856f, 32f).AssignToRectTransform(searchField.transform.AsRT());
            var searchFieldInput = searchField.GetComponent<InputField>();
            searchFieldInput.textComponent.alignment = TextAnchor.MiddleLeft;
            searchFieldInput.onValueChanged.ClearAll();
            searchFieldInput.text = "";
            searchFieldInput.onValueChanged.AddListener(_val =>
            {
                searchTerm = _val;
                currentSubTabPage = 0;
                RefreshSettings();
            });
            searchFieldInput.GetPlaceholderText().text = "Search setting...";
            EditorThemeManager.ApplyInputField(searchFieldInput, ThemeGroup.Search_Field_1);

            // Page Label
            {
                var pageLabel = Creator.NewUIObject("Text", pagePanel.transform);
                var pageLabelText = pageLabel.AddComponent<Text>();
                pageLabelText.alignment = TextAnchor.MiddleCenter;
                pageLabelText.font = Font.GetDefault();
                pageLabelText.fontSize = 20;
                pageLabelText.fontStyle = FontStyle.Normal;
                pageLabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
                pageLabelText.verticalOverflow = VerticalWrapMode.Overflow;
                pageLabelText.text = "Page";
                EditorThemeManager.ApplyLightText(pageLabelText);
                RectValues.Default.AnchoredPosition(100f, 0f).SizeDelta(64f, 32f).AssignToRectTransform(pageLabelText.rectTransform);
            }

            var pageObject = numberFieldStorage.Duplicate(pagePanel.transform, "Page");
            RectValues.BottomLeftAnchored.AnchoredPosition(580f, 32f).Pivot(0.5f, 0.5f).SizeDelta(0f, 32f).AssignToRectTransform(pageObject.transform.AsRT());

            pageFieldStorage = pageObject.GetComponent<InputFieldStorage>();
            pageFieldStorage.inputField.GetPlaceholderText().text = "Go to page...";
            EditorThemeManager.ApplyInputField(pageFieldStorage.inputField);
            EditorThemeManager.ApplySelectable(pageFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(pageFieldStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(pageFieldStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.ApplySelectable(pageFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            var descriptionBase = Creator.NewUIObject("Description Base", configBase.transform);
            RectValues.Default.AnchoredPosition(500f, -180f).Pivot(0f, 0.5f).SizeDelta(240f, 350f).AssignToRectTransform(descriptionBase.transform.AsRT());
            var descriptionBaseImage = descriptionBase.AddComponent<Image>();

            var description = Creator.NewUIObject("Description", descriptionBase.transform);
            RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(description.transform.AsRT());
            descriptionText = description.AddComponent<Text>();
            descriptionText.font = Font.GetDefault();
            descriptionText.fontSize = 18;
            descriptionText.text = "Hover over a setting to get the description.";

            EditorThemeManager.ApplyGraphic(descriptionBaseImage, ThemeGroup.Background_2, true, roundedSide: SpriteHelper.RoundedSide.Right);
            EditorThemeManager.ApplyLightText(descriptionText);

            Hide();

            configBase.AddComponent<ConfigManagerUI>();
        }

        public void UpdateTabs()
        {
            LegacyPlugin.configs.RemoveAll(x => x is CustomConfig);
            if (AssetPack.TryGetDirectory("configs", out string configsDirectory))
            {
                var configFiles = System.IO.Directory.GetFiles(configsDirectory, FileFormat.LSC.ToPattern());
                for (int i = 0; i < configFiles.Length; i++)
                    LegacyPlugin.configs.Add(new CustomConfig(System.IO.Path.GetFileName(configFiles[i])));
            }

            LSHelpers.DeleteChildren(tabs);

            for (int i = 0; i < LegacyPlugin.configs.Count; i++)
            {
                var index = i;
                var config = LegacyPlugin.configs[i];

                var tab = Creator.NewUIObject($"Tab {i}", tabs);

                var tabBase = Creator.NewUIObject("Image", tab.transform);
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(tabBase.transform.AsRT());
                var tabBaseImage = tabBase.AddComponent<Image>();

                tabBaseImage.color = config.TabColor;

                var tabTitle = Creator.NewUIObject("Title", tabBase.transform);
                RectValues.FullAnchored.AssignToRectTransform(tabTitle.transform.AsRT());
                var tabTitleText = tabTitle.AddComponent<Text>();
                tabTitleText.alignment = TextAnchor.MiddleCenter;
                tabTitleText.font = Font.GetDefault();
                tabTitleText.fontSize = 18;
                tabTitleText.text = config.TabName;

                tabTitle.AddComponent<ContrastColors>().Init(tabTitleText, tabBaseImage);

                var tabButton = tabBase.AddComponent<Button>();
                tabButton.image = tabBaseImage;
                tabButton.onClick.AddListener(() => SetTab(index));

                EditorThemeManager.ApplyGraphic(tabBaseImage, ThemeGroup.Null, true);

                tab.AddComponent<HoverConfig>().Init(config.TabDesc);
            }

        }

        void Update()
        {
            if (!watchingKeybind && Input.GetKeyDown(CoreConfig.Instance.OpenConfigKey.Value))
                Toggle();

            if (canvas != null)
                canvas.Canvas.scaleFactor = CoreHelper.ScreenScale;

            if (!watchingKeybind)
                return;

            var key = CoreHelper.GetKeyCodeDown();

            if (key == KeyCode.None)
                return;

            watchingKeybind = false;

            try
            {
                onSelectKey?.Invoke(key);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Error with keybind action: {ex}");
            }
        }

        #region Active States

        public bool Active => configBase.activeSelf;

        public void Show() => configBase.SetActive(true);

        public void Hide() => configBase.SetActive(false);

        public void Toggle() => configBase.SetActive(!Active);

        #endregion

        public void SetTab(int tabIndex)
        {
            if (currentTab != tabIndex)
            {
                currentSubTab = 0;
                currentSubTabPage = 0;
            }

            lastTab = currentTab;
            currentTab = tabIndex;
            LSHelpers.DeleteChildren(subTabs);
            var config = LegacyPlugin.configs[Mathf.Clamp(tabIndex, 0, LegacyPlugin.configs.Count - 1)];

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
                RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(tabBase.transform.AsRT());
                var tabBaseImage = tabBase.AddComponent<Image>();

                var tabTitle = Creator.NewUIObject("Title", tabBase.transform);
                RectValues.FullAnchored.AssignToRectTransform(tabTitle.transform.AsRT());
                var tabTitleText = tabTitle.AddComponent<Text>();
                tabTitleText.alignment = TextAnchor.MiddleCenter;
                tabTitleText.font = Font.GetDefault();
                tabTitleText.fontSize = 15;
                tabTitleText.text = config.Settings[i].Section;

                var tabButton = tabBase.AddComponent<Button>();
                tabButton.image = tabBaseImage;
                tabButton.onClick.AddListener(() =>
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
            var config = LegacyPlugin.configs[Mathf.Clamp(currentTab, 0, LegacyPlugin.configs.Count - 1)];

            currentSubTab = Mathf.Clamp(currentSubTab, 0, subTabSections.Count - 1);

            var settings = config.Settings.Where(x => subTabSections[currentSubTab] == x.Section);
            var page = currentSubTabPage + 1;
            var max = page * MAX_SETTINGS_PER_PAGE;
            
            LSHelpers.DeleteChildren(content);
            int num = 0;
            for (int i = 0; i < settings.Count(); i++)
            {
                var setting = settings.ElementAt(i);

                if (!RTString.SearchString(searchTerm, setting.Key, setting.Section))
                    continue;

                if (num >= max - MAX_SETTINGS_PER_PAGE && num < max)
                {
                    var gameObject = Creator.NewUIObject("Setting", content);
                    gameObject.transform.AsRT().sizeDelta = new Vector2(830f, 38f);

                    gameObject.AddComponent<HoverConfig>().Init(setting);

                    var image = gameObject.AddComponent<Image>();

                    var label = Creator.NewUIObject("Label", gameObject.transform);
                    RectValues.FullAnchored.SizeDelta(-12f, 0f).AssignToRectTransform(label.transform.AsRT());

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
                        RectValues.Default.AnchoredPosition(330f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(boolean.transform.AsRT());
                        var booleanImage = boolean.AddComponent<Image>();

                        var checkmark = Creator.NewUIObject("Checkmark", boolean.transform);
                        RectValues.FullAnchored.SizeDelta(-8f, -8f).AssignToRectTransform(checkmark.transform.AsRT());
                        var checkmarkImage = checkmark.AddComponent<Image>();
                        checkmarkImage.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/checkmark{FileFormat.PNG.Dot()}"));

                        var booleanToggle = boolean.AddComponent<Toggle>();
                        booleanToggle.image = booleanImage;
                        booleanToggle.graphic = checkmarkImage;
                        booleanToggle.onValueChanged.ClearAll();
                        booleanToggle.isOn = boolSetting.Value;
                        booleanToggle.onValueChanged.AddListener(_val => setting.BoxedValue = _val);

                        EditorThemeManager.ApplyToggle(booleanToggle);
                    }

                    if (type == typeof(int))
                    {
                        var intSetting = (Setting<int>)setting;
                        var integer = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                        new RectValues(new Vector2(480f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f)).AssignToRectTransform(integer.transform.AsRT());

                        var integerStorage = integer.GetComponent<InputFieldStorage>();

                        integerStorage.inputField.onValueChanged.ClearAll();
                        integerStorage.inputField.text = intSetting.Value.ToString();
                        integerStorage.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (int.TryParse(_val, out int value))
                            {
                                if (intSetting.MinValue != 0 || intSetting.MaxValue != 0)
                                    value = Mathf.Clamp(value, intSetting.MinValue, intSetting.MaxValue);

                                setting.BoxedValue = value;
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(integerStorage.inputField, min: intSetting.MinValue, max: intSetting.MaxValue, t: integer.transform);
                        TriggerHelper.AddEventTriggers(integer.gameObject, TriggerHelper.ScrollDeltaInt(integerStorage.inputField, min: intSetting.MinValue, max: intSetting.MaxValue));

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
                        new RectValues(new Vector2(480f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f)).AssignToRectTransform(floatingPoint.transform.AsRT());

                        var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                        floatingPointStorage.inputField.onValueChanged.ClearAll();
                        floatingPointStorage.inputField.text = floatSetting.Value.ToString();
                        floatingPointStorage.inputField.onValueChanged.AddListener(_val =>
                        {
                            if (float.TryParse(_val, out float value))
                            {
                                if (floatSetting.MinValue != 0 || floatSetting.MaxValue != 0)
                                    value = Mathf.Clamp(value, floatSetting.MinValue, floatSetting.MaxValue);

                                setting.BoxedValue = value;
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: floatSetting.MinValue, max: floatSetting.MaxValue, t: floatingPoint.transform);
                        TriggerHelper.AddEventTriggers(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: floatSetting.MinValue, max: floatSetting.MaxValue));

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
                            new RectValues(new Vector2(340f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f)).AssignToRectTransform(floatingPoint.transform.AsRT());

                            var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                            floatingPointStorage.inputField.onValueChanged.ClearAll();
                            floatingPointStorage.inputField.text = vector2Setting.Value.x.ToString();
                            floatingPointStorage.inputField.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float value))
                                    setting.BoxedValue = new Vector2(value, vector2Setting.Value.y);
                            });

                            TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: vector2Setting.MinValue.x, max: vector2Setting.MaxValue.x, t: floatingPoint.transform);
                            TriggerHelper.AddEventTriggers(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: vector2Setting.MinValue.x, max: vector2Setting.MaxValue.x));

                            Destroy(floatingPointStorage.leftGreaterButton.gameObject);
                            Destroy(floatingPointStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.ApplyInputField(floatingPointStorage.inputField);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.rightButton, ThemeGroup.Function_2, false);
                        }

                        // Y
                        {
                            var floatingPoint = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                            new RectValues(new Vector2(560f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f)).AssignToRectTransform(floatingPoint.transform.AsRT());

                            var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                            floatingPointStorage.inputField.onValueChanged.ClearAll();
                            floatingPointStorage.inputField.text = vector2Setting.Value.y.ToString();
                            floatingPointStorage.inputField.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float value))
                                    setting.BoxedValue = new Vector2(vector2Setting.Value.x, value);
                            });

                            TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: vector2Setting.MinValue.y, max: vector2Setting.MaxValue.y, t: floatingPoint.transform);
                            TriggerHelper.AddEventTriggers(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: vector2Setting.MinValue.y, max: vector2Setting.MaxValue.y));

                            Destroy(floatingPointStorage.leftGreaterButton.gameObject);
                            Destroy(floatingPointStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.ApplyInputField(floatingPointStorage.inputField);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.rightButton, ThemeGroup.Function_2, false);
                        }
                    }
                    
                    if (type == typeof(Vector2Int))
                    {
                        var vector2Setting = (Setting<Vector2Int>)setting;

                        // X
                        {
                            var floatingPoint = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                            new RectValues(new Vector2(340f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f)).AssignToRectTransform(floatingPoint.transform.AsRT());

                            var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                            floatingPointStorage.inputField.onValueChanged.ClearAll();
                            floatingPointStorage.inputField.text = vector2Setting.Value.x.ToString();
                            floatingPointStorage.inputField.onValueChanged.AddListener(_val =>
                            {
                                if (int.TryParse(_val, out int value))
                                    setting.BoxedValue = new Vector2Int(value, vector2Setting.Value.y);
                            });

                            TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: vector2Setting.MinValue.x, max: vector2Setting.MaxValue.x, t: floatingPoint.transform);
                            TriggerHelper.AddEventTriggers(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: vector2Setting.MinValue.x, max: vector2Setting.MaxValue.x));

                            Destroy(floatingPointStorage.leftGreaterButton.gameObject);
                            Destroy(floatingPointStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.ApplyInputField(floatingPointStorage.inputField);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.ApplySelectable(floatingPointStorage.rightButton, ThemeGroup.Function_2, false);
                        }

                        // Y
                        {
                            var floatingPoint = numberFieldStorage.Duplicate(gameObject.transform, "Input");
                            new RectValues(new Vector2(560f, 18f), Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0f, 32f)).AssignToRectTransform(floatingPoint.transform.AsRT());

                            var floatingPointStorage = floatingPoint.GetComponent<InputFieldStorage>();

                            floatingPointStorage.inputField.onValueChanged.ClearAll();
                            floatingPointStorage.inputField.text = vector2Setting.Value.y.ToString();
                            floatingPointStorage.inputField.onValueChanged.AddListener(_val =>
                            {
                                if (int.TryParse(_val, out int value))
                                    setting.BoxedValue = new Vector2Int(vector2Setting.Value.x, value);
                            });

                            TriggerHelper.IncreaseDecreaseButtons(floatingPointStorage.inputField, min: vector2Setting.MinValue.y, max: vector2Setting.MaxValue.y, t: floatingPoint.transform);
                            TriggerHelper.AddEventTriggers(floatingPoint.gameObject, TriggerHelper.ScrollDelta(floatingPointStorage.inputField, min: vector2Setting.MinValue.y, max: vector2Setting.MaxValue.y));

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
                        stringObject.transform.AsRT().sizeDelta = new Vector2(358f, 32f);
                        var stringInputField = stringObject.GetComponent<InputField>();
                        stringInputField.onValueChanged.ClearAll();
                        stringInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                        ((Text)stringInputField.placeholder).alignment = TextAnchor.MiddleLeft;
                        stringInputField.text = stringSetting.Value;
                        stringInputField.onValueChanged.AddListener(_val => { stringSetting.Value = _val; });

                        EditorThemeManager.ApplyInputField(stringInputField);
                    }

                    if (type == typeof(Color))
                    {
                        var colorSetting = (Setting<Color>)setting;

                        var colorObject = Creator.NewUIObject("Color", gameObject.transform);
                        RectValues.Default.AnchoredPosition(80f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(colorObject.transform.AsRT());
                        var colorImage = colorObject.AddComponent<Image>();
                        colorImage.color = colorSetting.Value;

                        EditorThemeManager.ApplyGraphic(colorImage, ThemeGroup.Null, true);

                        var stringObject = numberFieldStorage.transform.Find("input").gameObject.Duplicate(gameObject.transform, "Input");
                        stringObject.transform.AsRT().anchoredPosition = new Vector2(535f, 0f);
                        stringObject.transform.AsRT().sizeDelta = new Vector2(238f, 32f);
                        var stringInputField = stringObject.GetComponent<InputField>();
                        stringInputField.onValueChanged.ClearAll();
                        stringInputField.textComponent.alignment = TextAnchor.MiddleLeft;
                        ((Text)stringInputField.placeholder).alignment = TextAnchor.MiddleLeft;
                        stringInputField.text = RTColors.ColorToHexOptional(colorSetting.Value);
                        stringInputField.onValueChanged.AddListener(_val =>
                        {
                            colorSetting.Value = _val.Length == 8 ? LSColors.HexToColorAlpha(_val) : LSColors.HexToColor(_val);
                            colorImage.color = colorSetting.Value;
                        });

                        EditorThemeManager.ApplyInputField(stringInputField);
                    }

                    if (type.IsEnum)
                    {
                        if (type == typeof(KeyCode))
                        {
                            var watchKeyCodeBase = Creator.NewUIObject("Image", gameObject.transform);
                            RectValues.Default.AnchoredPosition(-32f, 0f).SizeDelta(136f, 32f).AssignToRectTransform(watchKeyCodeBase.transform.AsRT());
                            var watchKeyCodeBaseImage = watchKeyCodeBase.AddComponent<Image>();

                            var watchKeyCodeTitle = Creator.NewUIObject("Title", watchKeyCodeBase.transform);
                            RectValues.FullAnchored.AssignToRectTransform(watchKeyCodeTitle.transform.AsRT());
                            var watchKeyCodeTitleText = watchKeyCodeTitle.AddComponent<Text>();
                            watchKeyCodeTitleText.alignment = TextAnchor.MiddleCenter;
                            watchKeyCodeTitleText.font = Font.GetDefault();
                            watchKeyCodeTitleText.fontSize = 15;
                            watchKeyCodeTitleText.text = "Set key";

                            var watchKeyCodeButton = watchKeyCodeBase.AddComponent<Button>();
                            watchKeyCodeButton.image = watchKeyCodeBaseImage;
                            watchKeyCodeButton.onClick.AddListener(() =>
                            {
                                if (watchingKeybind)
                                    return;

                                watchingKeybind = true;
                                watchKeyCodeTitleText.text = "Press a key...";

                                onSelectKey = keyCode =>
                                {
                                    setting.BoxedValue = keyCode;
                                    RefreshSettings();
                                };
                            });

                            EditorThemeManager.ApplyGraphic(watchKeyCodeButton.image, ThemeGroup.Function_1, true);
                            EditorThemeManager.ApplyGraphic(watchKeyCodeTitleText, ThemeGroup.Function_1_Text);
                        }

                        var enumObject = UIManager.GenerateDropdown("Dropdown", gameObject.transform);
                        var dropdown = enumObject.dropdown;
                        var hide = enumObject.hideOptions;

                        RectValues.Default.AnchoredPosition(196f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());

                        dropdown.onValueChanged.ClearAll();
                        dropdown.options.Clear();
                        hide.DisabledOptions = new List<bool>();
                        hide.remove = true;

                        var enums = Enum.GetValues(type);

                        for (int j = 0; j < enums.Length; j++)
                        {
                            var name = Enum.GetName(type, j);
                            hide.DisabledOptions.Add(name == null);

                            dropdown.options.Add(new Dropdown.OptionData(name ?? "Invalid Value"));
                        }

                        dropdown.value = (int)setting.BoxedValue;
                        dropdown.onValueChanged.AddListener(_val => setting.BoxedValue = _val);

                        EditorThemeManager.ApplyDropdown(dropdown);
                    }

                    if (setting.BoxedValue is ICustomEnum customEnum)
                    {
                        var enumObject = UIManager.GenerateDropdown("Dropdown", gameObject.transform);
                        var dropdown = enumObject.dropdown;
                        var hide = enumObject.hideOptions;

                        RectValues.Default.AnchoredPosition(196f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(dropdown.transform.AsRT());

                        dropdown.onValueChanged.ClearAll();
                        dropdown.options.Clear();
                        hide.DisabledOptions = new List<bool>();
                        hide.remove = true;

                        var values = customEnum.GetBoxedValues();

                        for (int j = 0; j < values.Length; j++)
                        {
                            var value = values[j];
                            hide.DisabledOptions.Add(value == null);

                            dropdown.options.Add(new Dropdown.OptionData(value.DisplayName ?? "Invalid Value"));
                        }

                        dropdown.value = customEnum.Ordinal;
                        dropdown.onValueChanged.AddListener(_val => setting.BoxedValue = customEnum.GetBoxedValue(_val));

                        EditorThemeManager.ApplyDropdown(dropdown);
                    }

                    // Reset
                    {
                        var resetBase = Creator.NewUIObject("Reset", gameObject.transform);
                        RectValues.Default.AnchoredPosition(395f, 0f).SizeDelta(64f, 32f).AssignToRectTransform(resetBase.transform.AsRT());
                        var resetBaseImage = resetBase.AddComponent<Image>();

                        var resetTitle = Creator.NewUIObject("Title", resetBase.transform);
                        RectValues.FullAnchored.AssignToRectTransform(resetTitle.transform.AsRT());
                        var resetTitleText = resetTitle.AddComponent<Text>();
                        resetTitleText.alignment = TextAnchor.MiddleCenter;
                        resetTitleText.font = Font.GetDefault();
                        resetTitleText.fontSize = 15;
                        resetTitleText.text = "Reset";

                        var resetButton = resetBase.AddComponent<Button>();
                        resetButton.image = resetBaseImage;
                        resetButton.onClick.AddListener(() =>
                        {
                            setting.BoxedValue = setting.DefaultValue;
                            RefreshSettings();
                        });

                        EditorThemeManager.ApplyGraphic(resetButton.image, ThemeGroup.Function_1, true);
                        EditorThemeManager.ApplyGraphic(resetTitleText, ThemeGroup.Function_1_Text);
                    }
                }

                num++;
            }

            pageFieldStorage.inputField.onValueChanged.ClearAll();
            pageFieldStorage.inputField.text = currentSubTabPage.ToString();
            pageFieldStorage.inputField.onValueChanged.AddListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                {
                    currentSubTabPage = p;
                    RefreshSettings();
                }
            });

            if (num / MAX_SETTINGS_PER_PAGE != 0)
                TriggerHelper.AddEventTriggers(pageFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(pageFieldStorage.inputField, max: num / MAX_SETTINGS_PER_PAGE));
            else
                TriggerHelper.AddEventTriggers(pageFieldStorage.inputField.gameObject);

            pageFieldStorage.leftGreaterButton.onClick.ClearAll();
            pageFieldStorage.leftGreaterButton.onClick.AddListener(() =>
            {
                currentSubTabPage = 0;
                RefreshSettings();
            });
            pageFieldStorage.leftButton.onClick.ClearAll();
            pageFieldStorage.leftButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageFieldStorage.inputField.text, out int p))
                {
                    currentSubTabPage = Mathf.Clamp(p - 1, 0, num / MAX_SETTINGS_PER_PAGE);
                    RefreshSettings();
                }
            });
            pageFieldStorage.rightButton.onClick.ClearAll();
            pageFieldStorage.rightButton.onClick.AddListener(() =>
            {
                if (int.TryParse(pageFieldStorage.inputField.text, out int p))
                {
                    currentSubTabPage = Mathf.Clamp(p + 1, 0, num / MAX_SETTINGS_PER_PAGE);
                    RefreshSettings();
                }
            });
            pageFieldStorage.rightGreaterButton.onClick.ClearAll();
            pageFieldStorage.rightGreaterButton.onClick.AddListener(() =>
            {
                currentSubTabPage = num / MAX_SETTINGS_PER_PAGE;
                RefreshSettings();
            });
        }
    }

    public class ConfigManagerUI : MonoBehaviour { void OnEnable() => ConfigManager.inst.SetTab(ConfigManager.inst.currentTab); }

    public class HoverConfig : MonoBehaviour, IPointerEnterHandler
    {
        public string tooltip;

        public void Init(BaseSetting baseSetting) => tooltip = baseSetting.Description;

        public void Init(string text) => tooltip = text;

        public void OnPointerEnter(PointerEventData pointerEventData) => ConfigManager.inst.descriptionText.text = tooltip;
    }
}

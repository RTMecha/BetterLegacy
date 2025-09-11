using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class UploadedLevelsDialog : EditorDialog, IContentUI, IPageUI
    {
        public UploadedLevelsDialog() : base() { }

        public RectTransform TabsContent { get; set; }
        public List<Button> TabButtons { get; set; } = new List<Button>();

        public Toggle UploadedToggle { get; set; }

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get => SearchField.text; set => SearchField.text = value; }

        public InputFieldStorage PageField { get; set; }

        public int Page { get; set; }

        public int MaxPageCount { get; }

        public void ClearContent() => LSHelpers.DeleteChildren(Content);

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "UploadedDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor("F05355");
            dialogStorage.title.text = "- Online Files -";

            CoreHelper.Delete(editorDialogObject.transform.GetChild(2));

            EditorHelper.AddEditorDialog(UPLOADED_LEVELS, editorDialogObject);

            var tabs = Creator.NewUIObject("tabs", editorDialogObject.transform);
            TabsContent = tabs.transform.AsRT();
            TabsContent.sizeDelta = new Vector2(750f, 32f);
            var tabsLayout = tabs.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandHeight = true;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = true;
            tabsLayout.spacing = 8f;

            SetupTab("Levels", EditorServerManager.Tab.Levels);
            SetupTab("Level Collections", EditorServerManager.Tab.LevelCollections);
            SetupTab("Prefabs", EditorServerManager.Tab.Prefabs);

            var bar = editorDialogObject.transform.Find("spacer");
            bar.AsRT().sizeDelta = new Vector2(765f, 32f);
            bar.name = "bar";

            var barLayout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            barLayout.childControlWidth = false;

            var search = EditorPrefabHolder.Instance.StringInputField.Duplicate(bar, "search");
            RectValues.Default.AnchoredPosition(-200f, 0f).SizeDelta(240f, 32f).AssignToRectTransform(search.transform.AsRT());
            SearchField = search.GetComponent<InputField>();
            SearchField.SetTextWithoutNotify(string.Empty);
            SearchField.GetPlaceholderText().text = "Search levels...";
            SearchField.onValueChanged.ClearAll();
            var contextClickable = search.AddComponent<ContextClickable>();
            contextClickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button != UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                    return;

                var buttonFunctions = new List<ButtonFunction>();

                switch (EditorServerManager.inst.tab)
                {
                    case EditorServerManager.Tab.Levels: {
                            var sort = EditorServerManager.inst.CurrentTabSettings.sort;
                            buttonFunctions.AddRange(new List<ButtonFunction>
                            {
                                GetSortButton("Default", 0),
                                GetSortButton("Name", 1),
                                GetSortButton("Song Title", 2),
                                GetSortButton("Difficulty", 3),
                                GetSortButton("Creator", 4),
                                GetSortButton("Song Artist", 5),
                                GetSortButton("Date Published", 6),
                            });
                            break;
                        }
                    case EditorServerManager.Tab.LevelCollections: {
                            buttonFunctions.AddRange(new List<ButtonFunction>
                            {
                                GetSortButton("Default", 0),
                                GetSortButton("Name", 1),
                                GetSortButton("Difficulty", 2),
                                GetSortButton("Creator", 3),
                                GetSortButton("Date Published", 4),
                            });
                            break;
                        }
                    case EditorServerManager.Tab.Prefabs: {
                            buttonFunctions.AddRange(new List<ButtonFunction>
                            {
                                GetSortButton("Default", 0),
                                GetSortButton("Name", 1),
                                GetSortButton("Creator", 2),
                                GetSortButton("Type", 3),
                                GetSortButton("Date Published", 4),
                            });
                            break;
                        }
                }

                buttonFunctions.Add(new ButtonFunction($"Ascend [{(EditorServerManager.inst.CurrentTabSettings.ascend ? "On" : "Off")}]", () =>
                {
                    EditorServerManager.inst.CurrentTabSettings.ascend = !EditorServerManager.inst.CurrentTabSettings.ascend;
                    RTEditor.inst.SaveGlobalSettings();
                }));
                EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
            };

            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(bar, "page");
            RectValues.Default.AnchoredPosition(-40f, 0f).SizeDelta(200f, 32f).AssignToRectTransform(page.transform.AsRT());
            var pageLayoutElement = page.GetOrAddComponent<LayoutElement>();
            pageLayoutElement.minWidth = 100f;
            pageLayoutElement.preferredWidth = 100f;

            var pageStorage = page.GetComponent<InputFieldStorage>();
            PageField = pageStorage;

            PageField.SetTextWithoutNotify("0");
            PageField.OnValueChanged.NewListener(_val =>
            {
                if (!int.TryParse(_val, out int p))
                    return;

                EditorServerManager.inst.CurrentTabSettings.page = Mathf.Clamp(p, 0, int.MaxValue);
                RTEditor.inst.SaveGlobalSettings();
            });

            pageStorage.leftGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.Text, out int p))
                    PageField.Text = Mathf.Clamp(p - 10, 0, int.MaxValue).ToString();
            });
            pageStorage.leftButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.Text, out int p))
                    PageField.Text = Mathf.Clamp(p - 1, 0, int.MaxValue).ToString();
            });
            pageStorage.rightButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.Text, out int p))
                    PageField.Text = Mathf.Clamp(p + 1, 0, int.MaxValue).ToString();
            });
            pageStorage.rightGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.Text, out int p))
                    PageField.Text = Mathf.Clamp(p + 10, 0, int.MaxValue).ToString();
            });
            CoreHelper.Delete(pageStorage.middleButton);

            var uploaded = EditorPrefabHolder.Instance.ToggleButton.Duplicate(bar, "uploaded");
            RectValues.Default.SizeDelta(160f, 32f).AssignToRectTransform(uploaded.transform.AsRT());
            var uploadedToggleStorage = uploaded.GetComponent<ToggleButtonStorage>();
            UploadedToggle = uploadedToggleStorage.toggle;
            uploadedToggleStorage.Text = "Uploaded";
            uploadedToggleStorage.SetIsOnWithoutNotify(true);
            uploadedToggleStorage.OnValueChanged.NewListener(_val =>
            {
                EditorServerManager.inst.CurrentTabSettings.uploaded = _val;
                RTEditor.inst.SaveGlobalSettings();
            });
            EditorThemeManager.AddToggle(uploadedToggleStorage.toggle, graphic: uploadedToggleStorage.label);

            var searchButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(bar, "search button");
            RectValues.Default.AnchoredPosition(310f, 0f).SizeDelta(100f, 32f).AssignToRectTransform(searchButton.transform.AsRT());
            var searchButtonStorage = searchButton.GetComponent<FunctionButtonStorage>();
            searchButtonStorage.Text = "Search";
            searchButtonStorage.OnClick.NewListener(EditorServerManager.inst.Search);

            EditorThemeManager.AddInputField(SearchField);
            EditorThemeManager.AddInputField(pageStorage);
            EditorThemeManager.AddSelectable(searchButtonStorage.button, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(searchButtonStorage.label, ThemeGroup.Function_2_Text);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            Content = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.localScale = Vector3.one;

            LSHelpers.DeleteChildren(Content);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 560f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorHelper.AddEditorDropdown("View Uploaded", string.Empty, "Steam", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_levels{FileFormat.PNG.Dot()}"), () =>
            {
                EditorServerManager.inst.Dialog.Open();
                EditorServerManager.inst.Search();
            });

            EditorHelper.AddEditorDialog(UPLOADED_LEVELS, editorDialogObject);

            InitDialog(UPLOADED_LEVELS);
        }

        ButtonFunction GetSortButton(string name, int sort) => new ButtonFunction((EditorServerManager.inst.CurrentTabSettings.sort == sort ? "> " : string.Empty) + $"Sort: {name}", () =>
        {
            EditorServerManager.inst.CurrentTabSettings.sort = sort;
            RTEditor.inst.SaveGlobalSettings();
        });

        void SetupTab(string name, EditorServerManager.Tab tab)
        {
            var tabObj = EditorPrefabHolder.Instance.Function1Button.Duplicate(TabsContent);
            var tabStorage = tabObj.GetComponent<FunctionButtonStorage>();

            tabStorage.Text = name;
            tabStorage.OnClick.NewListener(() =>
            {
                EditorServerManager.inst.tab = tab;
                EditorServerManager.inst.Search();
                RTEditor.inst.SaveGlobalSettings();
            });
            TabButtons.Add(tabStorage.button);

            EditorThemeManager.AddGraphic(tabStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(tabStorage.label, ThemeGroup.Function_1_Text);
        }
    }
}

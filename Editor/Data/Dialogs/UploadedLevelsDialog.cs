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
    public class UploadedLevelsDialog : EditorDialog, IContentUI
    {
        public UploadedLevelsDialog() : base() { }

        public InputField PageField { get; set; }

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get => SearchField.text; set => SearchField.text = value; }

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
            dialogStorage.title.text = "- Uploaded Levels -";

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            CoreHelper.Delete(editorDialogObject.transform.GetChild(2));

            EditorHelper.AddEditorDialog(UPLOADED_LEVELS, editorDialogObject);

            var search = EditorPrefabHolder.Instance.StringInputField.Duplicate(editorDialogObject.transform.Find("spacer"), "search");
            RectValues.Default.AnchoredPosition(-200f, 0f).SizeDelta(300f, 32f).AssignToRectTransform(search.transform.AsRT());
            SearchField = search.GetComponent<InputField>();
            SearchField.SetTextWithoutNotify(string.Empty);
            SearchField.GetPlaceholderText().text = "Search levels...";
            SearchField.onValueChanged.ClearAll();

            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(editorDialogObject.transform.Find("spacer"), "page");
            RectValues.Default.AnchoredPosition(-40f, 0f).SizeDelta(0f, 32f).AssignToRectTransform(page.transform.AsRT());
            var pageStorage = page.GetComponent<InputFieldStorage>();
            PageField = pageStorage.inputField;

            var pageLayoutElement = PageField.GetComponent<LayoutElement>();
            pageLayoutElement.minWidth = 100f;
            pageLayoutElement.preferredWidth = 100f;
            PageField.SetTextWithoutNotify("0");
            PageField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                    UploadedLevelsManager.inst.page = Mathf.Clamp(p, 0, int.MaxValue);
            });

            pageStorage.leftGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.text, out int p))
                    PageField.text = Mathf.Clamp(p - 10, 0, int.MaxValue).ToString();
            });
            pageStorage.leftButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.text, out int p))
                    PageField.text = Mathf.Clamp(p - 1, 0, int.MaxValue).ToString();
            });
            pageStorage.rightButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.text, out int p))
                    PageField.text = Mathf.Clamp(p + 1, 0, int.MaxValue).ToString();
            });
            pageStorage.rightGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.text, out int p))
                    PageField.text = Mathf.Clamp(p + 10, 0, int.MaxValue).ToString();
            });
            CoreHelper.Delete(pageStorage.middleButton.gameObject);

            var searchButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(editorDialogObject.transform.Find("spacer"), "search button");
            RectValues.Default.AnchoredPosition(310f, 0f).SizeDelta(100f, 32f).AssignToRectTransform(searchButton.transform.AsRT());
            var searchButtonStorage = searchButton.GetComponent<FunctionButtonStorage>();
            searchButtonStorage.label.text = "Search";
            searchButtonStorage.button.onClick.NewListener(UploadedLevelsManager.inst.Search);

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
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorHelper.AddEditorDropdown("View Uploaded", string.Empty, "Steam", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_levels{FileFormat.PNG.Dot()}"), () =>
            {
                UploadedLevelsManager.inst.Dialog.Open();
                UploadedLevelsManager.inst.Search();
            });

            EditorHelper.AddEditorDialog(UPLOADED_LEVELS, editorDialogObject);

            InitDialog(UPLOADED_LEVELS);
        }
    }
}

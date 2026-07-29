using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class ScreenshotsViewDialog : EditorDialog, IContentUI, IPageUI
    {
        public ScreenshotsViewDialog() : base() { }

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get; set; }

        public InputFieldStorage PageField { get; set; }

        public int Page { get; set; }

        public int MaxPageCount => ScreenshotsView.inst.screenshotCount / ScreenshotsView.inst.screenshotsPerPage;

        public void ClearContent() => CoreHelper.DestroyChildren(Content);

        public override void Init()
        {
            if (init)
                return;
            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "ScreenshotDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            editorDialogObject.AddComponent<ActiveState>().onStateChanged = enabled => CaptureArea.inst.SetActive(enabled);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor("00FF8C");
            dialogStorage.title.text = "- Screenshots -";

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            CoreHelper.Delete(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog(SCREENSHOTS, editorDialogObject);

            InitDialog(SCREENSHOTS);

            var spacer = editorDialogObject.transform.Find("spacer");

            var takeScreenshot = EditorPrefabHolder.Instance.Function2Button.Duplicate(spacer, "take screenshot");
            takeScreenshot.transform.AsRT().anchoredPosition = new Vector2(120f, 27f);
            takeScreenshot.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
            var takeScreenshotButton = takeScreenshot.GetComponent<FunctionButtonStorage>();
            takeScreenshotButton.Text = "Take Screenshot";
            takeScreenshotButton.OnClick.NewListener(CoreHelper.TakeScreenshot);
            EditorThemeManager.ApplySelectable(takeScreenshotButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(takeScreenshotButton.label, ThemeGroup.Function_2_Text);
            
            var openFolder = EditorPrefabHolder.Instance.Function2Button.Duplicate(spacer, "open folder");
            openFolder.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
            var openFolderButton = openFolder.GetComponent<FunctionButtonStorage>();
            openFolderButton.Text = "Open Folder";
            openFolderButton.OnClick.NewListener(() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(RTFile.ApplicationDirectory, CoreConfig.Instance.ScreenshotsPath.Value)));
            EditorThemeManager.ApplySelectable(openFolderButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(openFolderButton.label, ThemeGroup.Function_2_Text);

            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(spacer);
            page.transform.AsRT().anchoredPosition = new Vector2(600f, 27f);
            page.transform.AsRT().sizeDelta = new Vector2(200f, 32f);
            PageField = page.GetComponent<InputFieldStorage>();

            PageField.SetTextWithoutNotify(Page.ToString());
            PageField.OnValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                    SetPage(p);
            });
            PageField.leftGreaterButton.onClick.NewListener(() => SetPage(0));
            PageField.leftButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.Text, out int p))
                    SetPage(p - 1);
            });
            PageField.rightButton.onClick.NewListener(() =>
            {
                if (int.TryParse(PageField.Text, out int p))
                    SetPage(p + 1);
            });
            PageField.rightGreaterButton.onClick.NewListener(() => SetPage(MaxPageCount));

            CoreHelper.Delete(PageField.middleButton.gameObject);

            EditorThemeManager.ApplyInputField(PageField);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            Content = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.localScale = Vector3.one;

            ClearContent();

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.ApplyGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorHelper.AddEditorDropdown("View Screenshots", string.Empty, EditorHelper.VIEW_DROPDOWN, EditorSprites.SearchSprite, () =>
            {
                Open();
                ScreenshotsView.inst.Refresh();
            });
        }

        /// <summary>
        /// Sets the screenshots view page.
        /// </summary>
        /// <param name="page">Page to show.</param>
        public void SetPage(int page)
        {
            PageField.SetTextWithoutNotify(page.ToString());
            Page = Mathf.Clamp(page, 0, ScreenshotsView.inst.screenshotCount / ScreenshotsView.inst.screenshotsPerPage);
            ScreenshotsView.inst.Refresh();
        }
    }
}

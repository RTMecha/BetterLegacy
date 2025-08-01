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
    public class LevelCollectionEditorDialog : EditorDialog
    {
        public LevelCollectionEditorDialog() : base() { }

        #region Values

        public RectTransform Content { get; set; }

        public InputField NameField { get; set; }

        public InputField DescriptionField { get; set; }

        public InputField CreatorField { get; set; }

        #region Icon

        public RectTransform IconBase { get; set; }
        public Image IconImage { get; set; }

        public Button SelectIconButton { get; set; }
        public Toggle CollapseIconToggle { get; set; }

        #endregion

        #region Icon

        public RectTransform BannerBase { get; set; }
        public Image BannerImage { get; set; }

        public Button SelectBannerButton { get; set; }
        public Toggle CollapseBannerToggle { get; set; }

        #endregion

        #endregion

        #region Methods

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "LevelCollectionDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.title.text = "- Level Collection Editor -";

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.AddGraphic(dialogStorage.topPanel, ThemeGroup.Add);
            EditorThemeManager.AddGraphic(dialogStorage.title, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            EditorHelper.AddEditorDialog(LEVEL_COLLECTION_EDITOR, editorDialogObject);

            InitDialog(LEVEL_COLLECTION_EDITOR);

            CoreHelper.Delete(GameObject.transform.Find("spacer"));
            CoreHelper.Delete(GameObject.transform.Find("Text"));
            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            scrollView.transform.AsRT().sizeDelta = new Vector2(765f, 696f);
            Content = scrollView.transform.Find("Viewport/Content").AsRT();

            #region Setup

            new Labels(Labels.InitSettings.Default.Parent(Content), "Name");
            var name = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "name");
            NameField = name.GetComponent<InputField>();
            NameField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.AddInputField(NameField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Description");
            var description = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "description");
            description.transform.AsRT().sizeDelta = new Vector2(0f, 200f);
            DescriptionField = description.GetComponent<InputField>();
            DescriptionField.textComponent.alignment = TextAnchor.UpperLeft;
            EditorThemeManager.AddInputField(DescriptionField);

            new Labels(Labels.InitSettings.Default.Parent(Content), "Creator");
            var creator = EditorPrefabHolder.Instance.StringInputField.Duplicate(Content, "creator");
            CreatorField = creator.GetComponent<InputField>();
            CreatorField.textComponent.alignment = TextAnchor.MiddleLeft;
            EditorThemeManager.AddInputField(CreatorField);

            var labelRect = new RectValues(new Vector2(16f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, -32f));

            #region Icon

            var iconBase = Creator.NewUIObject("icon", Content);
            IconBase = iconBase.transform.AsRT();
            RectValues.Default.SizeDelta(764f, 574f).AssignToRectTransform(IconBase);
            new Labels(Labels.InitSettings.Default.Parent(IconBase).Rect(labelRect), new Label("Icon") { fontStyle = FontStyle.Bold, });

            var icon = Creator.NewUIObject("image", IconBase);
            IconImage = icon.AddComponent<Image>();
            new RectValues(new Vector2(16f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(512f, 512f)).AssignToRectTransform(IconImage.rectTransform);

            var selectIcon = EditorPrefabHolder.Instance.Function2Button.Duplicate(IconBase, "select");
            new RectValues(new Vector2(240f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(150f, 32f)).AssignToRectTransform(selectIcon.transform.AsRT());
            var selectIconStorage = selectIcon.GetComponent<FunctionButtonStorage>();
            SelectIconButton = selectIconStorage.button;
            selectIconStorage.label.text = "Browse";

            EditorThemeManager.AddSelectable(SelectIconButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(selectIconStorage.label, ThemeGroup.Function_2_Text);

            var collapseIcon = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(IconBase, "collapse");
            new RectValues(new Vector2(340f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, 32f)).AssignToRectTransform(collapseIcon.transform.AsRT());
            CollapseIconToggle = collapseIcon.GetComponent<Toggle>();

            EditorThemeManager.AddToggle(CollapseIconToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapseIcon.transform.Find("dots").childCount; i++)
                EditorThemeManager.AddGraphic(collapseIcon.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #region Banner

            var bannerBase = Creator.NewUIObject("banner", Content);
            BannerBase = bannerBase.transform.AsRT();
            RectValues.Default.SizeDelta(764f, 354f).AssignToRectTransform(BannerBase);
            new Labels(Labels.InitSettings.Default.Parent(BannerBase).Rect(labelRect), new Label("Banner") { fontStyle = FontStyle.Bold, });

            var banner = Creator.NewUIObject("image", BannerBase);
            BannerImage = banner.AddComponent<Image>();
            new RectValues(new Vector2(16f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(512f, 512f)).AssignToRectTransform(BannerImage.rectTransform);

            var selectBanner = EditorPrefabHolder.Instance.Function2Button.Duplicate(BannerBase, "select");
            new RectValues(new Vector2(240f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(150f, 32f)).AssignToRectTransform(selectBanner.transform.AsRT());
            var selectBannerStorage = selectBanner.GetComponent<FunctionButtonStorage>();
            SelectBannerButton = selectBannerStorage.button;
            selectBannerStorage.label.text = "Browse";

            EditorThemeManager.AddSelectable(SelectBannerButton, ThemeGroup.Function_2);
            EditorThemeManager.AddGraphic(selectBannerStorage.label, ThemeGroup.Function_2_Text);

            var collapseBanner = EditorPrefabHolder.Instance.CollapseToggle.Duplicate(BannerBase, "collapse");
            new RectValues(new Vector2(340f, -62f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(32f, 32f)).AssignToRectTransform(collapseBanner.transform.AsRT());
            CollapseBannerToggle = collapseBanner.GetComponent<Toggle>();

            EditorThemeManager.AddToggle(CollapseBannerToggle, ThemeGroup.Background_1);

            for (int i = 0; i < collapseBanner.transform.Find("dots").childCount; i++)
                EditorThemeManager.AddGraphic(collapseBanner.transform.Find("dots").GetChild(i).GetComponent<Image>(), ThemeGroup.Dark_Text);

            #endregion

            #endregion
        }

        public void CollapseIcon(bool collapse)
        {
            var size = collapse ? 32f : 512f;
            IconImage.rectTransform.sizeDelta = new Vector2(size, size);
            IconBase.transform.AsRT().sizeDelta = new Vector2(764f, collapse ? 94f : 574f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        public void CollapseBanner(bool collapse)
        {
            BannerImage.rectTransform.sizeDelta = new Vector2(collapse ? 32f : 512f, collapse ? 18.20444f : 170.6666f);
            BannerBase.transform.AsRT().sizeDelta = new Vector2(764f, collapse ? 94f : 234f);

            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        #endregion
    }
}

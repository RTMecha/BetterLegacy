using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class IntroDialog : EditorDialog
    {
        public RectTransform Content { get; set; }

        public List<ToggleButtonStorage> ComplexityToggles { get; set; } = new List<ToggleButtonStorage>();

        public List<ToggleButtonStorage> ThemeToggles { get; set; } = new List<ToggleButtonStorage>();

        public ToggleButtonStorage RoundedToggle { get; set; }

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "IntroDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.title.text = "- Welcome to Project Arrhythmia -";

            EditorThemeManager.ApplyGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorThemeManager.ApplyGraphic(dialogStorage.topPanel, ThemeGroup.Add);
            EditorThemeManager.ApplyGraphic(dialogStorage.title, ThemeGroup.Add_Text);

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            EditorHelper.AddEditorDialog(INTRO_DIALOG, editorDialogObject);

            InitDialog(INTRO_DIALOG);

            CoreHelper.Delete(GameObject.transform.Find("spacer"));
            CoreHelper.Delete(GameObject.transform.Find("Text"));

            var main = Creator.NewUIObject("Main", editorDialogObject.transform);
            main.transform.AsRT().sizeDelta = new Vector2(765f, 696f);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(main.transform, "Scroll View");
            RectValues.Default.SizeDelta(745f, 696f).AssignToRectTransform(scrollView.transform.AsRT());
            Content = scrollView.transform.Find("Viewport/Content").AsRT();
            Content.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(left: 1, right: 8, top: 0, bottom: 8);

            #region Setup

            Creator.NewUIObject("top spacer", Content).transform.AsRT().sizeDelta = new Vector2(0f, 64f);

            var welcome = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(Content);
            welcome.transform.AsRT().sizeDelta = new Vector2(0f, 130f);
            var welcomeText = welcome.GetComponent<Text>();
            welcomeText.alignment = TextAnchor.UpperLeft;
            LangObject.Init(welcomeText, "editor.intro.welcome", "Welcome to the Project Arrhythmia (BetterLegacy) editor!\n" +
                "Below you'll find some settings to help you get into the editor. If you need to change them at any time, you can find them in the Config Manager.");
            EditorThemeManager.ApplyLightText(welcomeText);

            new ButtonElement("editor.intro.help", () => EditorDocumentation.inst.OpenPopup()).Init(EditorElement.InitSettings.Default.Parent(Content));

            new Labels(Labels.InitSettings.Default.Parent(Content), "Editor Complexity");
            var complexityTogglesParent = new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(Content).Rect(RectValues.Default.SizeDelta(0f, 32f)), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(8f));
            var complexities = EnumHelper.GetValues<Complexity>();
            for (int i = 0; i < complexities.Length; i++)
            {
                var complexity = complexities[i];
                var complexityToggleObject = EditorPrefabHolder.Instance.ToggleButton.Duplicate(complexityTogglesParent.GameObject.transform, complexity.ToString());
                var complexityToggle = complexityToggleObject.GetComponent<ToggleButtonStorage>();
                complexityToggle.Text = complexity.ToString();
                EditorThemeManager.ApplyToggle(complexityToggle);
                TooltipHelper.AssignTooltip(complexityToggleObject, $"{complexity} Editor Complexity");
                ComplexityToggles.Add(complexityToggle);
            }

            new Labels(Labels.InitSettings.Default.Parent(Content), "Editor Theme");
            var themeTogglesParent = new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(Content).Rect(RectValues.Default.SizeDelta(0f, 32f)), HorizontalOrVerticalLayoutValues.Vertical.ChildControlHeight(false).Spacing(4f));
            themeTogglesParent.GameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.MinSize;
            var themes = CustomEnumHelper.GetValues<EditorThemeType>();
            for (int i = 0; i < themes.Length; i++)
            {
                var theme = themes[i];
                var value = EditorThemeManager.EditorThemes[i];
                var themeToggleObject = EditorPrefabHolder.Instance.ToggleButton.Duplicate(themeTogglesParent.GameObject.transform, theme.DisplayName);
                themeToggleObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
                var themeToggle = themeToggleObject.GetComponent<ToggleButtonStorage>();
                themeToggle.label.rectTransform.anchoredPosition = new Vector2(16f, 0f);
                themeToggle.label.rectTransform.sizeDelta = new Vector2(-32f, 0f);
                themeToggle.label.alignment = TextAnchor.MiddleLeft;
                themeToggle.Text = theme.DisplayName;

                var toggle = themeToggle.toggle;

                toggle.image.fillCenter = true;
                toggle.image.color = value.ColorGroups.GetOrDefault(ThemeGroup.Toggle_1, Color.black);
                toggle.graphic.color = value.ColorGroups.GetOrDefault(ThemeGroup.Toggle_1_Check, Color.white);
                themeToggle.label.color = value.ColorGroups.GetOrDefault(ThemeGroup.Toggle_1_Check, Color.white);

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);

                var cover = Creator.NewUIObject("cover", themeToggleObject.transform);
                var coverImage = cover.AddComponent<Image>();
                RectValues.RightAnchored.SizeDelta(550f, 32f).AssignToRectTransform(coverImage.rectTransform);
                EditorThemeManager.ApplyGraphic(coverImage, ThemeGroup.Background_2, true, roundedSide: SpriteHelper.RoundedSide.Right);

                var desc = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(coverImage.rectTransform);
                var descText = desc.GetComponent<Text>();
                RectValues.FullAnchored.SizeDelta(-32f, 0f).AssignToRectTransform(descText.rectTransform);
                descText.alignment = TextAnchor.MiddleLeft;
                LangObject.Init(descText, $"editor.theme.{theme.Name.ToLower()}.desc");
                EditorThemeManager.ApplyLightText(descText);

                ThemeToggles.Add(themeToggle);
            }

            var rounded = EditorPrefabHolder.Instance.ToggleButton.Duplicate(Content, "Rounded");
            RoundedToggle = rounded.GetComponent<ToggleButtonStorage>();
            LangObject.Init(RoundedToggle.label, "editor.intro.rounded_toggle", "Should the UI be rounded?");
            EditorThemeManager.ApplyToggle(RoundedToggle);

            #endregion
        }
    }
}

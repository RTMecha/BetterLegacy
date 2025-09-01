using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the Theme Keyframe Dialog.
    /// </summary>
    public class ThemeKeyframeDialog : KeyframeDialog, IContentUI, IPageUI
    {
        public ThemeKeyframeDialog() : base(4) { }

        #region Properties

        #region Content

        public InputField SearchField { get; set; }

        public Transform Content { get; set; }

        public GridLayoutGroup Grid { get; set; }

        public Scrollbar ContentScrollbar { get; set; }

        public string SearchTerm { get => SearchField.text; set => SearchField.text = value; }

        public InputFieldStorage PageField { get; set; }

        public int Page { get; set; }

        public int MaxPageCount => RTThemeEditor.inst.InternalThemesCount / RTThemeEditor.eventThemesPerPage;

        #endregion

        #region Preview

        public Text CurrentTitle { get; set; }

        public Image BGColor { get; set; }
        public Image GUIColor { get; set; }
        public List<Image> PlayerColors { get; set; } = new List<Image>();
        public List<Image> ObjectColors { get; set; } = new List<Image>();
        public List<Image> BGColors { get; set; } = new List<Image>();

        #endregion

        #region Editor

        public GameObject Editor { get; set; }
        public RectTransform EditorContent { get; set; }

        public Transform EditorActions { get; set; }
        public Button EditorCreateNew { get; set; }
        public Button EditorSaveUse { get; set; }
        public Button EditorShuffleID { get; set; }
        public Button EditorUpdate { get; set; }
        public Button EditorCancel { get; set; }

        public InputField EditorNameField { get; set; }

        #endregion

        #endregion

        #region Fields

        public GameObject themeAddButton;

        #endregion

        #region Methods

        public override void Init()
        {
            base.Init();

            #region Content

            PageField = GameObject.transform.Find("themepathers/page").GetComponent<InputFieldStorage>();
            SearchField = GameObject.transform.Find("theme-search").GetComponent<InputField>();
            Content = GameObject.transform.Find("themes/viewport/content");
            ClearContent();

            CoreHelper.Destroy(Content.GetComponent<VerticalLayoutGroup>(), true);

            GameObject.transform.Find("themes").GetComponent<ScrollRect>().horizontal = false;
            Grid = Content.gameObject.AddComponent<GridLayoutGroup>();

            Grid.cellSize = new Vector2(344f, 30f);
            Grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            Grid.constraintCount = 1;
            Grid.spacing = new Vector2(4f, 4f);
            Grid.startAxis = GridLayoutGroup.Axis.Horizontal;

            Content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.MinSize;

            if (!themeAddButton)
            {
                themeAddButton = EventEditor.inst.ThemeAdd.Duplicate(Content, "Create New", 0);
                themeAddButton.SetActive(true);
                themeAddButton.transform.localScale = Vector2.one;
                var button = themeAddButton.GetComponent<Button>();
                button.onClick.AddListener(() => RTThemeEditor.inst.RenderThemeEditor());

                var contextClickable = themeAddButton.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create theme", RTThemeEditor.inst.RenderThemeEditor));
                };

                EditorThemeManager.AddGraphic(button.image, ThemeGroup.List_Button_2_Normal, true);
                EditorThemeManager.AddGraphic(themeAddButton.transform.Find("edit").GetComponent<Image>(), ThemeGroup.List_Button_2_Text);
                EditorThemeManager.AddGraphic(themeAddButton.transform.Find("text").GetComponent<Text>(), ThemeGroup.List_Button_2_Text);
            }

            #endregion

            #region Preview

            CurrentTitle = GameObject.transform.Find("current_title/current_title").GetComponent<Text>();

            for (int k = 0; k <= 3; k++)
                PlayerColors.Add(GameObject.transform.Find("player_cols/" + k).GetComponent<Image>());

            BGColor = GameObject.transform.Find("player_cols/4").GetComponent<Image>();
            GUIColor = GameObject.transform.Find("player_cols/5").GetComponent<Image>();

            for (int i = 0; i <= 8; i++)
                ObjectColors.Add(GameObject.transform.Find("object_cols/" + i).GetComponent<Image>());

            for (int i = 0; i <= 8; i++)
                BGColors.Add(GameObject.transform.Find("bg_cols/" + i).GetComponent<Image>());

            #endregion

            #region Editor

            Editor = RTEventEditor.inst.Dialog.GameObject.transform.Find("data/left/theme").gameObject;
            EditorContent = Editor.transform.Find("theme/viewport/content").AsRT();

            for (int i = 10; i < 19; i++)
            {
                var col = EditorContent.Find("object8").gameObject.Duplicate(EditorContent, "object" + (i - 1).ToString(), 8 + i);
                col.transform.Find("text").GetComponent<Text>().text = i.ToString();
            }

            var guiAccent = EditorContent.Find("gui").gameObject.Duplicate(EditorContent, "guiaccent", 3);
            guiAccent.transform.Find("text").GetComponent<Text>().text = "Tail";
            EditorContent.Find("gui/text").GetComponent<Text>().text = "GUI";

            var label = EditorContent.GetChild(0).gameObject.Duplicate(EditorContent, "effect_label");
            label.transform.Find("text").GetComponent<Text>().text = "Effects";

            for (int i = 0; i < 18; i++)
            {
                var col = EditorContent.Find("object8").gameObject.Duplicate(EditorContent, "effect" + i.ToString());
                col.transform.Find("text").GetComponent<Text>().text = (i + 1).ToString();
            }

            EditorActions = Editor.transform.Find("actions");
            var createNew = EditorActions.Find("create-new");
            EditorCreateNew = createNew.GetComponent<Button>();
            createNew.AsRT().sizeDelta = new Vector2(100f, 32f);
            createNew.GetChild(0).gameObject.GetComponent<Text>().fontSize = 18;

            var update = EditorActions.Find("update");
            EditorUpdate = update.GetComponent<Button>();
            update.AsRT().sizeDelta = new Vector2(70f, 32f);
            update.GetChild(0).gameObject.GetComponent<Text>().fontSize = 18;

            var cancel = EditorActions.Find("cancel");
            EditorCancel = cancel.GetComponent<Button>();
            cancel.AsRT().sizeDelta = new Vector2(70f, 32f);
            cancel.GetChild(0).gameObject.GetComponent<Text>().fontSize = 18;

            // Save & Use
            {
                var saveUse = createNew.gameObject.Duplicate(EditorActions, "save-use", 1);
                EditorSaveUse = saveUse.GetComponent<Button>();
                saveUse.transform.GetChild(0).GetComponent<Text>().text = "Save & Use";
            }

            // Shuffle ID
            {
                var shuffleID = createNew.gameObject.Duplicate(EditorActions.parent, "shuffle", 3);
                EditorShuffleID = shuffleID.GetComponent<Button>();
                var shuffleIDText = shuffleID.transform.GetChild(0).GetComponent<Text>();
                shuffleIDText.text = "Shuffle ID";

                var button = shuffleID.GetComponent<Button>();

                EditorThemeManager.AddSelectable(button, ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(shuffleIDText, ThemeGroup.Function_2_Text);
            }

            Editor.transform.Find("theme").AsRT().sizeDelta = new Vector2(366f, 570f);
            EditorNameField = Editor.transform.Find("name").GetComponent<InputField>();

            // fixes theme name not allowing non-alphabetical characters
            EditorNameField.characterValidation = InputField.CharacterValidation.None;

            EditorThemeManager.AddInputField(EditorNameField);

            for (int i = 0; i < EditorActions.childCount; i++)
            {
                var child = EditorActions.GetChild(i);
                var button = child.GetComponent<Button>();

                EditorThemeManager.AddSelectable(button, child.name == "cancel" ? ThemeGroup.Close : ThemeGroup.Function_2);
                EditorThemeManager.AddGraphic(child.GetChild(0).GetComponent<Text>(), child.name == "cancel" ? ThemeGroup.Close_X : ThemeGroup.Function_2_Text);
            }

            for (int i = 0; i < EditorContent.childCount; i++)
            {
                var child = EditorContent.GetChild(i);

                if (child.name == "label" || child.name == "effect_label")
                {
                    EditorThemeManager.AddLightText(child.GetChild(0).GetComponent<Text>());
                    continue;
                }

                var hex = child.Find("hex");
                var pound = hex.Find("pound");

                EditorThemeManager.AddLightText(child.Find("text").GetComponent<Text>());
                EditorThemeManager.AddGraphic(child.Find("preview").GetComponent<Image>(), ThemeGroup.Null, true);
                EditorThemeManager.AddInputField(hex.GetComponent<InputField>());
                EditorThemeManager.AddGraphic(pound.GetComponent<Text>(), ThemeGroup.Input_Field_Text);
            }

            EditorThemeManager.AddScrollbar(Editor.transform.Find("theme/Scrollbar Vertical").GetComponent<Scrollbar>(), scrollbarGroup: ThemeGroup.Scrollbar_2, handleGroup: ThemeGroup.Scrollbar_2_Handle);

            #endregion
        }

        /// <summary>
        /// Clears the content from the popup.
        /// </summary>
        public void ClearContent() => LSHelpers.DeleteChildren(Content);

        #endregion
    }
}

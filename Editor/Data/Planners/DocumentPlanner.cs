using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    /// <summary>
    /// Used for planning out a story, extended notes, etc.
    /// </summary>
    public class DocumentPlanner : PlannerBase<DocumentPlanner>
    {
        public DocumentPlanner() : base() { }

        #region Values

        #region Data

        public override Type PlannerType => Type.Document;

        /// <summary>
        /// Name of the document.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Text of the document.
        /// </summary>
        public string Text { get; set; }

        #endregion

        #region UI

        /// <summary>
        /// Name text display.
        /// </summary>
        public TextMeshProUGUI NameUI { get; set; }

        /// <summary>
        /// Text display.
        /// </summary>
        public TextMeshProUGUI TextUI { get; set; }

        #endregion

        #endregion

        #region Functions

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[0].Duplicate(ProjectPlanner.inst.content, "document");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            var buttonFunctions = new List<EditorElement>
            {
                new ButtonElement("Edit", () => ProjectPlanner.inst.OpenDocumentEditor(this)),
                new ButtonElement("Delete", () =>
                {
                    ProjectPlanner.inst.documents.RemoveAll(x => x is DocumentPlanner && x.ID == ID);
                    ProjectPlanner.inst.SaveDocuments();
                    CoreHelper.Destroy(gameObject);
                }),
                new SpacerElement(),
                new ButtonElement("Copy", () =>
                {
                    ProjectPlanner.inst.copiedPlanners.Clear();
                    ProjectPlanner.inst.copiedPlanners.Add(this);
                    EditorManager.inst.DisplayNotification("Copied document!", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                new ButtonElement("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                new ButtonElement("Paste", ProjectPlanner.inst.PastePlanners),
                new SpacerElement(),
            };

            buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.documents, () => ProjectPlanner.inst.documents.IndexOf(this), () =>
            {
                for (int i = 0; i < ProjectPlanner.inst.documents.Count; i++)
                    ProjectPlanner.inst.documents[i].Init();
                ProjectPlanner.inst.RefreshList();
            }));

            EditorContextMenu.AddContextMenu(gameObject, leftClick: () =>
            {
                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                ProjectPlanner.inst.OpenDocumentEditor(this);
            }, buttonFunctions);

            NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
            NameUI.text = Name;
            EditorThemeManager.ApplyLightText(NameUI);

            TextUI = gameObject.transform.Find("words").GetComponent<TextMeshProUGUI>();
            TextUI.text = Text;
            EditorThemeManager.ApplyLightText(TextUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this document?", () =>
            {
                ProjectPlanner.inst.documents.RemoveAll(x => x is DocumentPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveDocuments();
                CoreHelper.Destroy(gameObject);
            }));

            EditorThemeManager.ApplyDeleteButton(delete);
            EditorThemeManager.ApplyGraphic(gameObject.transform.Find("gradient").GetComponent<Image>(), ThemeGroup.Background_1);

            ProjectPlanner.inst.SetupPlannerLinks(Text, TextUI, null, false);

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void ReadJSON(JSONNode jn)
        {
            Name = jn["name"];
            if (jn["text"] != null)
                Text = jn["text"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = Name;
            if (!string.IsNullOrEmpty(Text))
                jn["text"] = Text;

            return jn;
        }

        public override DocumentPlanner CreateCopy() => new DocumentPlanner
        {
            Name = Name,
            Text = Text,
        };

        public override bool SamePlanner(PlannerBase other) => other is DocumentPlanner document && document.Name == Name;

        #endregion
    }
}

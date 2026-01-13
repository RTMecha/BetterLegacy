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
    public class FolderPlanner : PlannerBase<FolderPlanner>
    {
        public FolderPlanner() : base() { }

        #region Values

        #region Data

        public override Type PlannerType => Type.Folder;

        /// <summary>
        /// Name of the document.
        /// </summary>
        public string Name { get; set; }

        public string FullPath => RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.PlannersPath, Name);

        #endregion

        #region UI

        /// <summary>
        /// Name text display.
        /// </summary>
        public TextMeshProUGUI NameUI { get; set; }

        #endregion

        #endregion

        #region Functions

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[(int)PlannerType].Duplicate(ProjectPlanner.inst.content, "folder");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            var buttonFunctions = new List<EditorElement>
            {
                new ButtonElement("Edit", () => ProjectPlanner.inst.OpenFolderEditor(this)),
                new ButtonElement("Delete", () =>
                {
                    ProjectPlanner.inst.folders.RemoveAll(x => x is FolderPlanner && x.ID == ID);
                    RTFile.DeleteDirectory(FullPath);
                    CoreHelper.Destroy(gameObject);
                }),
                new SpacerElement(),
                new ButtonElement("Copy", () =>
                {
                    ProjectPlanner.inst.copiedPlanners.Clear();
                    ProjectPlanner.inst.copiedPlanners.Add(this);
                    EditorManager.inst.DisplayNotification("Copied folder!", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                new ButtonElement("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                new ButtonElement("Paste", ProjectPlanner.inst.PastePlanners),
                new SpacerElement(),
            };

            buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.folders, () => ProjectPlanner.inst.folders.IndexOf(this), () =>
            {
                for (int i = 0; i < ProjectPlanner.inst.folders.Count; i++)
                    ProjectPlanner.inst.folders[i].Init();
                ProjectPlanner.inst.RefreshList();
            }));

            EditorContextMenu.AddContextMenu(gameObject, leftClick: () =>
            {
                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                ProjectPlanner.inst.SetFolder(RTFile.CombinePaths(RTEditor.inst.PlannersPath, Name));
            }, buttonFunctions);

            NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
            NameUI.text = Name;
            EditorThemeManager.ApplyLightText(NameUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this folder?", () =>
            {
                ProjectPlanner.inst.folders.RemoveAll(x => x is FolderPlanner && x.ID == ID);
                RTFile.DeleteDirectory(FullPath);
                CoreHelper.Destroy(gameObject);
            }));

            EditorThemeManager.ApplyDeleteButton(delete);

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void Render() => NameUI.text = Name;

        public override void ReadJSON(JSONNode jn) { }

        public override JSONNode ToJSON() => Parser.NewJSONObject();

        public override FolderPlanner CreateCopy() => new FolderPlanner
        {
            Name = Name,
        };

        public override bool SamePlanner(PlannerBase other) => other is FolderPlanner folder && folder.Name == Name;

        #endregion
    }
}

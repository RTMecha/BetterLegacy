using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class TODOPlanner : PlannerBase<TODOPlanner>
    {
        public TODOPlanner() : base() { }

        public string Text { get; set; }
        public TextMeshProUGUI TextUI { get; set; }
        public OpenHyperlinks Hyperlinks { get; set; }
        public bool Checked { get; set; }
        public Toggle CheckedUI { get; set; }

        public override Type PlannerType => Type.TODO;

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[1].Duplicate(ProjectPlanner.inst.content, "todo");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            TextUI.text = Text;
            EditorThemeManager.ApplyLightText(TextUI);

            Hyperlinks = gameObject.AddComponent<OpenHyperlinks>();
            Hyperlinks.Text = TextUI;
            Hyperlinks.onClick = eventData =>
            {
                if (Hyperlinks.IsLinkHighlighted)
                    return;

                if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                {
                    var buttonFunctions = new List<EditorElement>
                    {
                        new ButtonElement("Edit", () => ProjectPlanner.inst.OpenTODOEditor(this)),
                        new ButtonElement("Delete", () =>
                        {
                            ProjectPlanner.inst.schedules.RemoveAll(x => x is SchedulePlanner && x.ID == ID);
                            ProjectPlanner.inst.SaveSchedules();
                            CoreHelper.Destroy(gameObject);
                        }),
                        new SpacerElement(),
                        new ButtonElement("Copy", () =>
                        {
                            ProjectPlanner.inst.copiedPlanners.Clear();
                            ProjectPlanner.inst.copiedPlanners.Add(this);
                            EditorManager.inst.DisplayNotification("Copied TODO!", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonElement("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                        new ButtonElement("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                        new ButtonElement("Paste", ProjectPlanner.inst.PastePlanners),
                        new SpacerElement(),
                    };

                    buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.todos, () => ProjectPlanner.inst.todos.IndexOf(this), () =>
                    {
                        for (int i = 0; i < ProjectPlanner.inst.todos.Count; i++)
                            ProjectPlanner.inst.todos[i].Init();
                        ProjectPlanner.inst.RefreshList();
                    }));

                    EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                    return;
                }

                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                ProjectPlanner.inst.OpenTODOEditor(this);
            };

            var toggle = gameObject.transform.Find("checked").GetComponent<Toggle>();
            CheckedUI = toggle;
            toggle.SetIsOnWithoutNotify(Checked);
            toggle.onValueChanged.NewListener(_val =>
            {
                Checked = _val;
                ProjectPlanner.inst.SaveTODO();
            });

            EditorThemeManager.ApplyToggle(toggle);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this todo?", () =>
            {
                ProjectPlanner.inst.todos.RemoveAll(x => x is TODOPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveTODO();
                CoreHelper.Destroy(gameObject);
            }));

            EditorThemeManager.ApplyDeleteButton(delete);

            ProjectPlanner.inst.SetupPlannerLinks(Text, TextUI, Hyperlinks);

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void ReadJSON(JSONNode jn)
        {
            Checked = jn["ch"].AsBool;
            Text = jn["text"];
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["ch"] = Checked;
            jn["text"] = Text;

            return jn;
        }

        public override TODOPlanner CreateCopy() => new TODOPlanner
        {
            Text = Text,
            Checked = Checked,
        };

        public override bool SamePlanner(PlannerBase other) => other is TODOPlanner todo && todo.Text == Text;
    }
}

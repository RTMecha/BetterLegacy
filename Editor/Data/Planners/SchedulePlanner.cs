using System;
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
    public class SchedulePlanner : PlannerBase<SchedulePlanner>
    {
        public SchedulePlanner() : base() { }

        public TextMeshProUGUI TextUI { get; set; }
        public OpenHyperlinks Hyperlinks { get; set; }
        public string Text => $"{Date} - {Description}";
        public string Date { get; set; } = DateTime.Now.AddDays(1).ToString("g");

        public string FormatDateFull(int day, int month, int year, int hour, int minute) => $"{day}/{(month < 10 ? "0" + month.ToString() : month.ToString())}/{year} {(hour > 12 ? hour - 12 : hour)}:{minute} {(hour > 12 ? "PM" : "AM")}";

        public string FormatDate(int day, int month, int year, int hour, int minute, string apm) => $"{day}/{(month < 10 ? "0" + month.ToString() : month.ToString())}/{year} {(hour)}:{minute} {apm}";

        public string DateFormat => $"{DateTime.Day}/{(DateTime.Month < 10 ? "0" + DateTime.Month.ToString() : DateTime.Month.ToString())}/{DateTime.Year} {(DateTime.Hour > 12 ? DateTime.Hour - 12 : DateTime.Hour)}:{DateTime.Minute} {(DateTime.Hour > 12 ? "PM" : "AM")}";
        public DateTime DateTime { get; set; } = DateTime.Now.AddDays(1);
        public string Description { get; set; }

        public bool IsActive => CompareDates(DateTime.Now);

        public bool IsTomorrow => CompareDates(DateTime.Now.AddDays(1));

        public bool IsNextWeek => CompareDates(DateTime.Now.AddDays(7));

        bool CompareDates(DateTime date) => DateTime.Day == date.Day && DateTime.Month == date.Month && DateTime.Year == date.Year;

        public bool hasBeenChecked;

        public override Type PlannerType => Type.Schedule;

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[4].Duplicate(ProjectPlanner.inst.content, "schedule");
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
                    var buttonFunctions = new List<ButtonFunction>
                    {
                        new ButtonFunction("Edit", () => ProjectPlanner.inst.OpenScheduleEditor(this)),
                        new ButtonFunction("Delete", () =>
                        {
                            ProjectPlanner.inst.schedules.RemoveAll(x => x is SchedulePlanner && x.ID == ID);
                            ProjectPlanner.inst.SaveSchedules();
                            CoreHelper.Destroy(gameObject);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Copy", () =>
                        {
                            ProjectPlanner.inst.copiedPlanners.Clear();
                            ProjectPlanner.inst.copiedPlanners.Add(this);
                            EditorManager.inst.DisplayNotification("Copied schedule!", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonFunction("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                        new ButtonFunction("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                        new ButtonFunction("Paste", ProjectPlanner.inst.PastePlanners),
                        new ButtonFunction(true),
                    };

                    buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.schedules, () => ProjectPlanner.inst.schedules.IndexOf(this), () =>
                    {
                        for (int i = 0; i < ProjectPlanner.inst.schedules.Count; i++)
                            ProjectPlanner.inst.schedules[i].Init();
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

                ProjectPlanner.inst.OpenScheduleEditor(this);
            };

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this schedule?", () =>
            {
                ProjectPlanner.inst.schedules.RemoveAll(x => x is SchedulePlanner && x.ID == ID);
                ProjectPlanner.inst.SaveSchedules();
                CoreHelper.Destroy(gameObject);
                RTEditor.inst.HideWarningPopup();
            }, RTEditor.inst.HideWarningPopup));

            EditorThemeManager.ApplyDeleteButton(delete);

            ProjectPlanner.inst.SetupPlannerLinks(Text, TextUI, Hyperlinks);

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void ReadJSON(JSONNode jn)
        {
            Date = jn["date"];
            Description = jn["desc"];
            if (jn["checked"] != null)
                hasBeenChecked = jn["checked"].AsBool;

            if (DateTime.TryParse(Date, out DateTime dateTime))
                DateTime = dateTime;
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["date"] = Date;
            jn["desc"] = Description;
            if (hasBeenChecked)
                jn["checked"] = hasBeenChecked;

            return jn;
        }

        public override SchedulePlanner CreateCopy() => new SchedulePlanner
        {
            Date = Date,
            DateTime = DateTime,
            Description = Description,
            hasBeenChecked = hasBeenChecked,
        };

        public override bool SamePlanner(PlannerBase other) => other is SchedulePlanner schedule && schedule.Date == Date;
    }
}

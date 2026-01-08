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
    /// <summary>
    /// Used for planning out a schedule.
    /// </summary>
    public class SchedulePlanner : PlannerBase<SchedulePlanner>
    {
        public SchedulePlanner() : base() { }

        #region Values

        #region Data

        public override Type PlannerType => Type.Schedule;

        /// <summary>
        /// Text of the schedule.
        /// </summary>
        public string Text => $"{Date} - {Description}";

        /// <summary>
        /// Date string of the schedule.
        /// </summary>
        public string Date { get; set; } = DateTime.Now.AddDays(1).ToString("g");

        /// <summary>
        /// Date time of the schedule.
        /// </summary>
        public DateTime DateTime { get; set; } = DateTime.Now.AddDays(1);

        /// <summary>
        /// Description of the schedule.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Checks if the schedule is now.
        /// </summary>
        public bool IsActive => CompareDates(DateTime.Now);

        /// <summary>
        /// Checks if the schedule is tomorrow.
        /// </summary>
        public bool IsTomorrow => CompareDates(DateTime.Now.AddDays(1));

        /// <summary>
        /// Checks if the schedule is tomorrow.
        /// </summary>
        public bool IsNextWeek => CompareDates(DateTime.Now.AddDays(7));

        /// <summary>
        /// If the schedule has been checked by Example.
        /// </summary>
        public bool hasBeenChecked;

        #endregion

        #region UI

        /// <summary>
        /// Text display.
        /// </summary>
        public TextMeshProUGUI TextUI { get; set; }

        /// <summary>
        /// Text hyperlinks.
        /// </summary>
        public OpenHyperlinks Hyperlinks { get; set; }

        #endregion

        #endregion

        #region Functions

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
                    var buttonFunctions = new List<EditorElement>
                    {
                        new ButtonElement("Edit", () => ProjectPlanner.inst.OpenScheduleEditor(this)),
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
                            EditorManager.inst.DisplayNotification("Copied schedule!", 2f, EditorManager.NotificationType.Success);
                        }),
                        new ButtonElement("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                        new ButtonElement("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                        new ButtonElement("Paste", ProjectPlanner.inst.PastePlanners),
                        new SpacerElement(),
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
            }));

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

        /// <summary>
        /// Formats <see cref="Date"/> into a <see cref="System.DateTime"/> compatible string.
        /// </summary>
        /// <param name="day">Day of the date time.</param>
        /// <param name="month">Month of the date time.</param>
        /// <param name="year">Year of the date time.</param>
        /// <param name="hour">Hour of the date time.</param>
        /// <param name="minute">Minute of the date time.</param>
        /// <param name="apm">AM or PM value.</param>
        /// <returns>Returns a formatted date time string.</returns>
        public string FormatDate(int day, int month, int year, int hour, int minute, string apm) => $"{day}/{(month < 10 ? "0" + month.ToString() : month.ToString())}/{year} {(hour)}:{minute} {apm}";

        bool CompareDates(DateTime date) => DateTime.Day == date.Day && DateTime.Month == date.Month && DateTime.Year == date.Year;

        #endregion
    }
}

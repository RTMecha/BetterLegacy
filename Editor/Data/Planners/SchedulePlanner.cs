using System;

using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class SchedulePlanner : PlannerBase
    {
        public SchedulePlanner() : base(Type.Schedule) { }

        public TextMeshProUGUI TextUI { get; set; }
        public string Text => $"{Date} - {Description}";
        public string Date { get; set; } = DateTime.Now.AddDays(1).ToString("g");

        public string FormatDateFull(int day, int month, int year, int hour, int minute) => $"{day}/{(month < 10 ? "0" + month.ToString() : month.ToString())}/{year} {(hour > 12 ? hour - 12 : hour)}:{minute} {(hour > 12 ? "PM" : "AM")}";

        public string FormatDate(int day, int month, int year, int hour, int minute, string apm) => $"{day}/{(month < 10 ? "0" + month.ToString() : month.ToString())}/{year} {(hour)}:{minute} {apm}";

        public string DateFormat => $"{DateTime.Day}/{(DateTime.Month < 10 ? "0" + DateTime.Month.ToString() : DateTime.Month.ToString())}/{DateTime.Year} {(DateTime.Hour > 12 ? DateTime.Hour - 12 : DateTime.Hour)}:{DateTime.Minute} {(DateTime.Hour > 12 ? "PM" : "AM")}";
        public DateTime DateTime { get; set; } = DateTime.Now.AddDays(1);
        public string Description { get; set; }

        public bool IsActive => DateTime.Day == DateTime.Now.Day && DateTime.Month == DateTime.Now.Month && DateTime.Year == DateTime.Now.Year;
        public bool hasBeenChecked;

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[4].Duplicate(ProjectPlanner.inst.content, "schedule");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.NewListener(() => ProjectPlanner.inst.OpenScheduleEditor(this));

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            TextUI.text = Text;
            EditorThemeManager.ApplyLightText(TextUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.schedules.RemoveAll(x => x is SchedulePlanner && x.ID == ID);
                ProjectPlanner.inst.SaveSchedules();
                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);
        }
    }
}

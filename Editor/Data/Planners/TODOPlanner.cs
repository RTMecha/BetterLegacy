using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Editor.Data.Planners
{
    public class TODOPlanner : PlannerBase
    {
        public TODOPlanner() : base(Type.TODO) { }

        public string Text { get; set; }
        public TextMeshProUGUI TextUI { get; set; }
        public bool Checked { get; set; }
        public Toggle CheckedUI { get; set; }

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
            button.onClick.AddListener(() => ProjectPlanner.inst.OpenTODOEditor(this));

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            TextUI.text = Text;
            EditorThemeManager.ApplyLightText(TextUI);

            var toggle = gameObject.transform.Find("checked").GetComponent<Toggle>();
            CheckedUI = toggle;
            toggle.onValueChanged.ClearAll();
            toggle.isOn = Checked;
            toggle.onValueChanged.AddListener(_val =>
            {
                Checked = _val;
                ProjectPlanner.inst.SaveTODO();
            });

            EditorThemeManager.ApplyToggle(toggle);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.ClearAll();
            delete.button.onClick.AddListener(() =>
            {
                ProjectPlanner.inst.todos.RemoveAll(x => x is TODOPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveTODO();
                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);
        }
    }
}

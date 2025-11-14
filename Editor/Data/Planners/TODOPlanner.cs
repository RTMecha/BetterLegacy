using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class TODOPlanner : PlannerBase
    {
        public TODOPlanner() : base(Type.TODO) { }

        public string Text { get; set; }
        public TextMeshProUGUI TextUI { get; set; }
        public OpenHyperlinks Hyperlinks { get; set; }
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

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            TextUI.text = Text;
            EditorThemeManager.ApplyLightText(TextUI);

            Hyperlinks = gameObject.AddComponent<OpenHyperlinks>();
            Hyperlinks.Text = TextUI;
            Hyperlinks.onClick = eventData =>
            {
                if (!Hyperlinks.IsLinkHighlighted)
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
            delete.button.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.todos.RemoveAll(x => x is TODOPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveTODO();
                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

            ProjectPlanner.inst.SetupPlannerLinks(Text, TextUI, Hyperlinks);

            gameObject.SetActive(false);
        }
    }
}

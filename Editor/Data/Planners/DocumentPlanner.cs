using UnityEngine;
using UnityEngine.UI;

using TMPro;

using BetterLegacy.Core;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class DocumentPlanner : PlannerBase
    {
        public DocumentPlanner() : base(Type.Document) { }

        public string Name { get; set; }
        public TextMeshProUGUI NameUI { get; set; }
        public string Text { get; set; }
        public TextMeshProUGUI TextUI { get; set; }

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[0].Duplicate(ProjectPlanner.inst.content, "document");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var button = gameObject.GetComponent<Button>();
            button.onClick.NewListener(() => ProjectPlanner.inst.OpenDocumentEditor(this));

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);

            NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
            NameUI.text = Name;
            EditorThemeManager.ApplyLightText(NameUI);

            TextUI = gameObject.transform.Find("words").GetComponent<TextMeshProUGUI>();
            TextUI.text = Text;
            EditorThemeManager.ApplyLightText(TextUI);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.documents.RemoveAll(x => x is DocumentPlanner && x.ID == ID);
                ProjectPlanner.inst.SaveDocuments();
                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);
            EditorThemeManager.ApplyGraphic(gameObject.transform.Find("gradient").GetComponent<Image>(), ThemeGroup.Background_1);

            ProjectPlanner.inst.SetupPlannerLinks(Text, TextUI, null, false);

            gameObject.SetActive(false);
        }
    }
}

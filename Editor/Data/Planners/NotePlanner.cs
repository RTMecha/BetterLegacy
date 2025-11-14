using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class NotePlanner : PlannerBase
    {
        public NotePlanner() : base(Type.Note) { }

        public bool Dragging { get; set; }

        public bool Active { get; set; }
        public string Name { get; set; }
        public Vector2 Position { get; set; } = Vector2.zero;
        public Vector2 Scale { get; set; } = new Vector2(1f, 1f);
        public Vector2 Size { get; set; } = new Vector2(300f, 150f);
        public int Color { get; set; }
        public string Text { get; set; }

        public Toggle ActiveUI { get; set; }
        public Image TopBar { get; set; }
        public TextMeshProUGUI TitleUI { get; set; }
        public TextMeshProUGUI TextUI { get; set; }
        public OpenHyperlinks Hyperlinks { get; set; }

        public Color TopColor => Color >= 0 && Color < MarkerEditor.inst.markerColors.Count ? MarkerEditor.inst.markerColors[Color] : LSColors.red700;

        public static bool DisplayEdges { get; set; }

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[5].Duplicate(ProjectPlanner.inst.content, "note");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            var noteDraggable = gameObject.AddComponent<NoteDraggable>();
            noteDraggable.note = this;

            EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Background_3, true, roundedSide: SpriteHelper.RoundedSide.Bottom);

            string[] names = new string[] { "left", "right", "up", "down" };
            for (int i = 0; i < 4; i++)
            {
                var anchoredPositon = Vector2.zero;
                var anchorMax = Vector2.zero;
                var anchorMin = Vector2.zero;
                var sizeDelta = new Vector2(4f, 0f);

                switch (i)
                {
                    case 0:
                        anchorMax = Vector2.one;
                        anchorMin = new Vector2(1f, 0f);
                        break;
                    case 1:
                        anchorMax = new Vector2(0f, 1f);
                        anchorMin = Vector2.zero;
                        break;
                    case 2:
                        anchoredPositon = new Vector2(0f, 30f);
                        anchorMax = Vector2.one;
                        anchorMin = new Vector2(0f, 1f);
                        sizeDelta = new Vector2(0f, 4f);
                        break;
                    case 3:
                        anchorMax = new Vector2(1f, 0f);
                        anchorMin = Vector2.zero;
                        sizeDelta = new Vector2(0f, 4f);
                        break;
                }

                var left = Creator.NewUIObject(names[i], gameObject.transform);
                UIManager.SetRectTransform(left.transform.AsRT(), anchoredPositon, anchorMax, anchorMin, new Vector2(0.5f, 0.5f), sizeDelta);
                var leftImage = left.AddComponent<Image>();
                leftImage.color = new Color(1f, 1f, 1f, DisplayEdges ? 1f : 0f);
                var noteDraggableLeft = left.AddComponent<NoteDraggable>();
                noteDraggableLeft.part = (NoteDraggable.DragPart)(i + 1);
                noteDraggableLeft.note = this;
            }

            var edit = gameObject.transform.Find("panel/edit").GetComponent<Button>();
            edit.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.CurrentTab = 5;
                ProjectPlanner.inst.Open();
                ProjectPlanner.inst.RenderTabs();
                ProjectPlanner.inst.RefreshList();
                ProjectPlanner.inst.OpenNoteEditor(this);
            });

            EditorThemeManager.ApplyGraphic(edit.image, ThemeGroup.Function_3, true);
            EditorThemeManager.ApplyGraphic(edit.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);

            TitleUI = gameObject.transform.Find("panel/title").GetComponent<TextMeshProUGUI>();
            TitleUI.text = $"Note - {Name}";

            ActiveUI = gameObject.transform.Find("panel/active").GetComponent<Toggle>();
            TopBar = gameObject.transform.Find("panel").GetComponent<Image>();
            TextUI = gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>();
            TextUI.text = Text;
            EditorThemeManager.ApplyLightText(TextUI);

            Hyperlinks = gameObject.AddComponent<OpenHyperlinks>();
            Hyperlinks.Text = TextUI;
            //EditorThemeManager.ClearSelectableColors(Hyperlinks.gameObject.AddComponent<Button>());

            EditorThemeManager.ApplyGraphic(TopBar, ThemeGroup.Background_3, true);
            TitleUI.gameObject.AddComponent<ContrastColors>().Init(TitleUI, TopBar);

            ActiveUI.SetIsOnWithoutNotify(Active);
            ActiveUI.onValueChanged.NewListener(_val =>
            {
                Active = _val;
                ProjectPlanner.inst.SaveNotes();
            });

            EditorThemeManager.ApplyToggle(ActiveUI);

            var delete = gameObject.transform.Find("panel/delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.notes.RemoveAll(x => x is NotePlanner && x.ID == ID);
                ProjectPlanner.inst.SaveNotes();
                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

            var close = gameObject.transform.Find("panel/close").GetComponent<Button>();
            close.onClick.NewListener(() => ActiveUI.isOn = false);

            EditorThemeManager.ApplySelectable(close, ThemeGroup.Close);
            EditorThemeManager.ApplyGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            gameObject.AddComponent<NoteCloseDelete>().Init(delete.gameObject, close.gameObject);

            ProjectPlanner.inst.SetupPlannerLinks(Text, TextUI, Hyperlinks);
        }
    }
}

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;

using BetterLegacy.Core;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    public class TimelinePlanner : PlannerBase
    {
        public TimelinePlanner() : base(Type.Timeline) { }

        public string Name { get; set; }

        public TextMeshProUGUI NameUI { get; set; }

        public List<Event> Levels { get; set; } = new List<Event>();

        public Transform Content { get; set; }

        public GameObject Add { get; set; }

        public void UpdateTimeline(bool destroy = true)
        {
            if (destroy)
            {
                LSHelpers.DeleteChildren(Content);
                int num = 0;
                foreach (var level in Levels)
                {
                    int index = num;
                    var gameObject = ProjectPlanner.inst.timelineButtonPrefab.Duplicate(Content, "event");
                    gameObject.transform.localScale = Vector3.one;
                    level.GameObject = gameObject;

                    level.Button = gameObject.GetComponent<Button>();
                    level.Button.onClick.NewListener(() =>
                    {
                        string path = $"{RTFile.ApplicationDirectory}beatmaps/{RTFile.ReplaceSlash(level.Path).Remove("/" + Level.LEVEL_LSB)}";
                        if (Level.TryVerify(path, true, out Level actualLevel))
                        {
                            ProjectPlanner.inst.Close();
                            EditorLevelManager.inst.LoadLevel(actualLevel);
                        }
                    });

                    EditorThemeManager.ApplySelectable(level.Button, ThemeGroup.List_Button_1);

                    level.NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
                    level.NameUI.text = $"{level.ElementType}: {level.Name}";
                    EditorThemeManager.ApplyLightText(level.NameUI);
                    level.DescriptionUI = gameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();
                    level.DescriptionUI.text = level.Description;
                    EditorThemeManager.ApplyLightText(level.DescriptionUI);

                    var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
                    delete.button.onClick.NewListener(() =>
                    {
                        Levels.RemoveAt(index);
                        UpdateTimeline();
                        ProjectPlanner.inst.SaveTimelines();
                    });

                    EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
                    EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

                    var edit = gameObject.transform.Find("edit").GetComponent<Button>();
                    edit.onClick.NewListener(() =>
                    {
                        CoreHelper.Log($"Editing {Name}");
                        ProjectPlanner.inst.OpenEventEditor(level);
                    });

                    EditorThemeManager.ApplyGraphic(edit.image, ThemeGroup.Function_3, true);
                    EditorThemeManager.ApplyGraphic(edit.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);

                    num++;
                }

                Add = ProjectPlanner.inst.timelineAddPrefab.Duplicate(Content, "add");
                Add.transform.localScale = Vector3.one;
                var button = Add.GetComponent<Button>();
                button.onClick.NewListener(() =>
                {
                    var level = new Event
                    {
                        Name = "New Level",
                        Description = "Set my path to a level in your beatmaps folder and then click me!",
                        Path = string.Empty
                    };

                    Levels.Add(level);
                    UpdateTimeline();
                    ProjectPlanner.inst.SaveTimelines();
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(Add.transform.GetChild(0).GetComponent<TextMeshProUGUI>());
            }
            else
            {
                foreach (var level in Levels)
                {
                    if (!level.GameObject)
                    {
                        var gameObject = ProjectPlanner.inst.timelineButtonPrefab.Duplicate(Content, "event");
                        gameObject.transform.localScale = Vector3.one;
                        level.GameObject = gameObject;
                    }

                    if (!level.NameUI)
                        level.NameUI = level.GameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
                    if (!level.DescriptionUI)
                        level.DescriptionUI = level.GameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();

                    level.NameUI.text = $"{level.ElementType}: {level.Name}";
                    level.DescriptionUI.text = level.Description;
                }
            }
        }

        public class Event
        {
            public GameObject GameObject { get; set; }
            public Button Button { get; set; }
            public TextMeshProUGUI NameUI { get; set; }
            public TextMeshProUGUI DescriptionUI { get; set; }

            public string Name { get; set; }
            public string Description { get; set; }
            public string Path { get; set; }
            public Type ElementType { get; set; }

            public enum Type
            {
                Level,
                Cutscene,
                Story
            }
        }

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[3].Duplicate(ProjectPlanner.inst.content, "timeline");
            gameObject.transform.localScale = Vector3.one;
            GameObject = gameObject;

            Content = gameObject.transform.Find("Scroll/Viewport/Content");

            EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, true);

            var scrollbar = gameObject.transform.Find("Scrollbar");
            EditorThemeManager.ApplyScrollbar(scrollbar.GetComponent<Scrollbar>(), scrollbar.GetComponent<Image>(), ThemeGroup.List_Button_1_Normal, ThemeGroup.Scrollbar_1_Handle, scrollbarRoundedSide: SpriteHelper.RoundedSide.Bottom);

            NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
            NameUI.text = Name;
            EditorThemeManager.ApplyLightText(NameUI);

            var edit = gameObject.transform.Find("edit").GetComponent<Button>();
            edit.onClick.NewListener(() => ProjectPlanner.inst.OpenTimelineEditor(this));

            EditorThemeManager.ApplyGraphic(edit.image, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.ApplyGraphic(edit.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_2_Text);

            var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
            delete.button.onClick.NewListener(() =>
            {
                ProjectPlanner.inst.timelines.RemoveAll(x => x is TimelinePlanner && x.ID == ID);
                ProjectPlanner.inst.SaveTimelines();
                CoreHelper.Destroy(gameObject);
            });

            EditorThemeManager.ApplyGraphic(delete.button.image, ThemeGroup.Delete, true);
            EditorThemeManager.ApplyGraphic(delete.image, ThemeGroup.Delete_Text);

            UpdateTimeline();
        }
    }
}

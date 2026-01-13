using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using TMPro;
using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Planners
{
    /// <summary>
    /// Used for planning out a story timeline.
    /// </summary>
    public class TimelinePlanner : PlannerBase<TimelinePlanner>
    {
        public TimelinePlanner() : base() { }

        #region Values

        #region Data

        public override Type PlannerType => Type.Timeline;

        /// <summary>
        /// Name of the timeline.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List of timeline events.
        /// </summary>
        public List<Event> Events { get; set; } = new List<Event>();

        #endregion

        #region UI

        /// <summary>
        /// Name text display.
        /// </summary>
        public TextMeshProUGUI NameUI { get; set; }

        /// <summary>
        /// Event content parent.
        /// </summary>
        public Transform Content { get; set; }

        /// <summary>
        /// Add button.
        /// </summary>
        public GameObject Add { get; set; }

        #endregion

        #endregion

        #region Functions

        public override void Init()
        {
            var gameObject = GameObject;
            if (gameObject)
                CoreHelper.Destroy(gameObject);

            gameObject = ProjectPlanner.inst.prefabs[(int)PlannerType].Duplicate(ProjectPlanner.inst.content, "timeline");
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
            delete.OnClick.NewListener(() => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this timeline?", () =>
            {
                ProjectPlanner.inst.timelines.RemoveAll(x => x is TimelinePlanner && x.ID == ID);
                ProjectPlanner.inst.SaveTimelines();
                CoreHelper.Destroy(gameObject);
            }));

            EditorThemeManager.ApplyDeleteButton(delete);

            var buttonFunctions = new List<EditorElement>
            {
                new ButtonElement("Edit", () => ProjectPlanner.inst.OpenTimelineEditor(this)),
                new ButtonElement("Delete", () =>
                {
                    ProjectPlanner.inst.timelines.RemoveAll(x => x is TimelinePlanner && x.ID == ID);
                    ProjectPlanner.inst.SaveTimelines();
                    CoreHelper.Destroy(gameObject);
                }),
                new SpacerElement(),
                new ButtonElement("Copy", () =>
                {
                    ProjectPlanner.inst.copiedPlanners.Clear();
                    ProjectPlanner.inst.copiedPlanners.Add(this);
                    EditorManager.inst.DisplayNotification("Copied timeline!", 2f, EditorManager.NotificationType.Success);
                }),
                new ButtonElement("Copy Selected", ProjectPlanner.inst.CopySelectedPlanners),
                new ButtonElement("Copy Current Tab", ProjectPlanner.inst.CopyCurrentTabPlanners),
                new ButtonElement("Paste", ProjectPlanner.inst.PastePlanners),
                new SpacerElement(),
            };

            buttonFunctions.AddRange(EditorContextMenu.GetMoveIndexFunctions(ProjectPlanner.inst.timelines, () => ProjectPlanner.inst.timelines.IndexOf(this), () =>
            {
                for (int i = 0; i < ProjectPlanner.inst.timelines.Count; i++)
                    ProjectPlanner.inst.timelines[i].Init();
                ProjectPlanner.inst.RefreshList();
            }));

            EditorContextMenu.AddContextMenu(gameObject, leftClick: () =>
            {
                if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
                {
                    Selected = !Selected;
                    return;
                }

                ProjectPlanner.inst.OpenTimelineEditor(this);
            }, buttonFunctions);

            UpdateTimeline();

            InitSelectedUI();

            gameObject.SetActive(false);
        }

        public override void Render()
        {
            NameUI.text = Name;
            for (int i = 0; i < Events.Count; i++)
                Events[i].Render();
        }

        public override void ReadJSON(JSONNode jn)
        {
            Name = jn["name"];

            if (jn["levels"] != null)
                for (int j = 0; j < jn["levels"].Count; j++)
                    Events.Add(Event.Parse(jn["levels"][j]));

            if (jn["events"] != null)
                for (int j = 0; j < jn["events"].Count; j++)
                    Events.Add(Event.Parse(jn["events"][j]));
        }

        public override JSONNode ToJSON()
        {
            var jn = Parser.NewJSONObject();

            jn["name"] = Name;

            for (int i = 0; i < Events.Count; i++)
                jn["events"][i] = Events[i].ToJSON();

            return jn;
        }

        public override TimelinePlanner CreateCopy() => new TimelinePlanner
        {
            Name = Name,
            Events = new List<Event>(Events.Select(x => x.CreateCopy())),
        };

        public override bool SamePlanner(PlannerBase other) => other is TimelinePlanner timeline && timeline.Name == Name;

        /// <summary>
        /// Updates the timeline events.
        /// </summary>
        /// <param name="destroy">If the timeline events list should be reinitialized.</param>
        public void UpdateTimeline(bool destroy = true)
        {
            if (destroy)
            {
                LSHelpers.DeleteChildren(Content);
                int num = 0;
                foreach (var level in Events)
                {
                    int index = num;
                    level.Init(this, index);
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

                    Events.Add(level);
                    UpdateTimeline();
                    ProjectPlanner.inst.SaveTimelines();
                });

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(Add.transform.GetChild(0).GetComponent<TextMeshProUGUI>());
            }
            else
            {
                int num = 0;
                foreach (var level in Events)
                {
                    if (!level.GameObject)
                        level.Init(this, num);

                    if (!level.NameUI)
                        level.NameUI = level.GameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
                    if (!level.DescriptionUI)
                        level.DescriptionUI = level.GameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();

                    level.NameUI.text = $"{level.EventType}: {level.Name}";
                    level.DescriptionUI.text = level.Description;
                    num++;
                }
            }
        }

        #endregion

        #region Sub Classes

        /// <summary>
        /// Represents an event in the timeline. <see cref="EventType"/> indicates what the event is. 
        /// </summary>
        public class Event : Exists
        {
            #region Values

            #region Data

            /// <summary>
            /// Name of the event.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Description of the event.
            /// </summary>
            public string Description { get; set; }

            /// <summary>
            /// Relative path to the level to open when the event is clicked.
            /// </summary>
            public string Path { get; set; }

            /// <summary>
            /// The type of this event.
            /// </summary>
            public Type EventType { get; set; }

            /// <summary>
            /// The type an event represents.
            /// </summary>
            public enum Type
            {
                /// <summary>
                /// Represents a gameplay oriented level.
                /// </summary>
                Level,
                /// <summary>
                /// Represents an animation oriented level.
                /// </summary>
                Cutscene,
                /// <summary>
                /// Represents an event with no associated level.
                /// </summary>
                Story
            }

            #endregion

            #region UI

            /// <summary>
            /// Unity Game Object of the event.
            /// </summary>
            public GameObject GameObject { get; set; }

            /// <summary>
            /// Button of the event.
            /// </summary>
            public Button Button { get; set; }

            /// <summary>
            /// Name text display.
            /// </summary>
            public TextMeshProUGUI NameUI { get; set; }

            /// <summary>
            /// Description text display.
            /// </summary>
            public TextMeshProUGUI DescriptionUI { get; set; }

            /// <summary>
            /// Description hyperlinks.
            /// </summary>
            public OpenHyperlinks Hyperlinks { get; set; }

            #endregion

            #endregion

            #region Functions

            /// <summary>
            /// Initializes the event.
            /// </summary>
            /// <param name="timelinePlanner">Parent timeline planner reference.</param>
            /// <param name="index">Index of the event.</param>
            public void Init(TimelinePlanner timelinePlanner, int index)
            {
                GameObject gameObject = GameObject;
                if (gameObject)
                    CoreHelper.Delete(gameObject);

                gameObject = ProjectPlanner.inst.timelineButtonPrefab.Duplicate(timelinePlanner.Content, "event", index);
                gameObject.transform.localScale = Vector3.one;
                GameObject = gameObject;

                Button = gameObject.GetComponent<Button>();
                Button.onClick.ClearAll();

                EditorThemeManager.ApplySelectable(Button, ThemeGroup.List_Button_1);

                NameUI = gameObject.transform.Find("name").GetComponent<TextMeshProUGUI>();
                NameUI.text = $"{EventType}: {Name}";
                EditorThemeManager.ApplyLightText(NameUI);
                DescriptionUI = gameObject.transform.Find("description").GetComponent<TextMeshProUGUI>();
                DescriptionUI.text = Description;
                EditorThemeManager.ApplyLightText(DescriptionUI);

                Hyperlinks = gameObject.AddComponent<OpenHyperlinks>();
                Hyperlinks.Text = DescriptionUI;
                Hyperlinks.onClick = eventData =>
                {
                    if (Hyperlinks.IsLinkHighlighted)
                        return;

                    if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonElement("Open Level", Open),
                            new ButtonElement("Delete", () =>
                            {
                                timelinePlanner.Events.RemoveAt(index);
                                timelinePlanner.UpdateTimeline();
                                ProjectPlanner.inst.SaveTimelines();
                            }));
                        return;
                    }

                    Open();
                };

                var delete = gameObject.transform.Find("delete").GetComponent<DeleteButtonStorage>();
                delete.OnClick.NewListener(() =>
                {
                    timelinePlanner.Events.RemoveAt(index);
                    timelinePlanner.UpdateTimeline();
                    ProjectPlanner.inst.SaveTimelines();
                });

                EditorThemeManager.ApplyDeleteButton(delete);

                var edit = gameObject.transform.Find("edit").GetComponent<Button>();
                edit.onClick.NewListener(() =>
                {
                    CoreHelper.Log($"Editing {Name}");
                    ProjectPlanner.inst.OpenEventEditor(this);
                });

                EditorThemeManager.ApplyGraphic(edit.image, ThemeGroup.Function_3, true);
                EditorThemeManager.ApplyGraphic(edit.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);

                var moveBack = gameObject.transform.Find("<").GetComponent<Button>();
                moveBack.onClick.NewListener(() =>
                {
                    if (index - 1 < 0)
                        return;

                    timelinePlanner.Events.Move(index, index - 1);
                    timelinePlanner.UpdateTimeline();
                    ProjectPlanner.inst.SaveTimelines();
                });

                EditorThemeManager.ApplySelectable(moveBack, ThemeGroup.Function_2, false);

                var moveForward = gameObject.transform.Find(">").GetComponent<Button>();
                moveForward.onClick.NewListener(() =>
                {
                    if (index + 1 >= timelinePlanner.Events.Count)
                        return;

                    timelinePlanner.Events.Move(index, index + 1);
                    timelinePlanner.UpdateTimeline();
                    ProjectPlanner.inst.SaveTimelines();
                });

                EditorThemeManager.ApplySelectable(moveForward, ThemeGroup.Function_2, false);

                ProjectPlanner.inst.SetupPlannerLinks(Description, DescriptionUI, Hyperlinks);
            }

            /// <summary>
            /// Renders the event.
            /// </summary>
            public void Render()
            {
                if (NameUI)
                    NameUI.text = $"{EventType}: {Name}";
                if (DescriptionUI)
                    DescriptionUI.text = Description;
                if (DescriptionUI && Hyperlinks)
                    ProjectPlanner.inst.SetupPlannerLinks(Description, DescriptionUI, Hyperlinks);
            }

            /// <summary>
            /// Parses an event from JSON.
            /// </summary>
            /// <param name="jn">JSON to parse.</param>
            /// <returns>Returns a parsed event.</returns>
            public static Event Parse(JSONNode jn) => new Event
            {
                Name = jn["n"],
                Path = !string.IsNullOrEmpty(jn["p"]) ? jn["p"] : string.Empty,
                EventType = (Type)jn["t"].AsInt,
                Description = !string.IsNullOrEmpty(jn["d"]) ? jn["d"] : string.Empty,
            };

            /// <summary>
            /// Converts the event to JSON.
            /// </summary>
            /// <returns>Returns a <see cref="JSONNode"/> representing the event.</returns>
            public JSONNode ToJSON()
            {
                var jn = Parser.NewJSONObject();

                jn["n"] = Name;
                if (!string.IsNullOrEmpty(Path))
                    jn["p"] = Path;
                jn["t"] = ((int)EventType).ToString();
                if (!string.IsNullOrEmpty(Description))
                    jn["d"] = Description;

                return jn;
            }

            /// <summary>
            /// Creates a copy of the event.
            /// </summary>
            /// <returns>Returns a copy of the event with the same data values.</returns>
            public Event CreateCopy() => new Event
            {
                Name = Name,
                Description = Description,
                Path = Path,
                EventType = EventType,
            };

            /// <summary>
            /// Opens the associated level.
            /// </summary>
            public void Open()
            {
                string path = $"{RTFile.ApplicationDirectory}beatmaps/{RTFile.ReplaceSlash(Path).Remove("/" + Level.LEVEL_LSB)}";
                if (!Level.TryVerify(path, true, out Level actualLevel))
                    return;

                ProjectPlanner.inst.Close();
                EditorLevelManager.inst.LoadLevel(actualLevel);
            }

            #endregion
        }

        #endregion
    }
}

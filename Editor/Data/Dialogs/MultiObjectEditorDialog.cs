using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using Crosstales.FB;

using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    /// <summary>
    /// Represents the editor dialog for editing multiple objects.
    /// </summary>
    public class MultiObjectEditorDialog : EditorDialog
    {
        /*
        TABS
        - Editor
           - Editor Layer
           - Editor Bin
           - Object Name
           - Index
           - Colors
           - Timeline Collapse
           - Hidden
           - Opacity Collision
        - Prefab
        - Object Properties
           - Start Time
           - Autokill
           - Parent
           - Time lock state
           - Low Detail Mode
        - Modifiers
           - Ignore Lifespan
           - Order Matters
        - Keyframes
           - Assign Colors
           - Paste Keyframes to Selected
           - Repeat Paste Keyframes to Selected
           - Pasting Data
        - Replace
        - Sync
         */

        public MultiObjectEditorDialog() : base(MULTI_OBJECT_EDITOR) { }

        #region Values

        /// <summary>
        /// Text to update.
        /// </summary>
        public Text Text { get; set; }

        public EditorDialog Dialog { get; set; }

        /// <summary>
        /// String to format from.
        /// </summary>
        public const string DEFAULT_TEXT = "You are currently editing multiple objects.\n\nObject Count: {0}/{3}\nBG Count: {5}/{6}\nPrefab Object Count: {1}/{4}\nTotal: {2}";

        List<MultiColorButton> multiColorButtons = new List<MultiColorButton>();
        List<MultiColorButton> multiGradientColorButtons = new List<MultiColorButton>();
        int currentMultiColorSelection = -1;
        int currentMultiGradientColorSelection = -1;

        bool updatedShapes;
        bool updatedText;
        public List<Toggle> shapeToggles = new List<Toggle>();
        public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();

        Transform multiShapes;
        Transform multiShapeSettings;
        public Vector2Int multiShapeSelection;
        public Transform multiObjectContent;

        public bool rework = true;

        /// <summary>
        /// Parent of the tab buttons.
        /// </summary>
        public Transform TabsParent { get; set; }

        /// <summary>
        /// Tabs of the dialog.
        /// </summary>
        public List<FunctionButtonStorage> TabButtons { get; set; } = new List<FunctionButtonStorage>();

        public List<ScrollViewElement> ScrollViews { get; set; } = new List<ScrollViewElement>();

        public ScrollViewElement ActiveScrollView { get; set; }

        /*
        TABS
        - Editor
           - Editor Layer
           - Editor Bin
           - Object Name
           - Index
           - Colors
           - Timeline Collapse
           - Hidden
           - Opacity Collision
           - Info
        - Prefab
        - Object Properties
           - Start Time
           - Autokill
           - Parent
           - Time lock state
           - Low Detail Mode
        - Modifiers
           - Ignore Lifespan
           - Order Matters
        - Keyframes
           - Assign Colors
           - Paste Keyframes to Selected
           - Repeat Paste Keyframes to Selected
           - Pasting Data
        - Replace
        - Sync
         */

        public Tab CurrentTab { get; set; }

        public enum Tab
        {
            Editor,
            Prefab,
            Properties,
            Modifiers,
            Keyframes,
            Replace,
            Sync,
        }

        #endregion

        #region Functions

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            #region Setup

            if (rework)
            {
                EditorThemeManager.ApplyGraphic(GameObject.GetComponent<Image>(), ThemeGroup.Background_1);

                var title = GameObject.transform.Find("data/right/Object Editor Title");
                title.SetParent(GameObject.transform);
                RectValues.FullAnchored.AnchoredPosition(0f, -16f).AnchorMin(0f, 1f).SizeDelta(0f, 32f).AssignToRectTransform(title.AsRT());

                CoreHelper.Delete(GameObject.transform.Find("data"));

                var layout = Creator.NewUIObject("layout", GameObject.transform);
                TabsParent = layout.transform;
                new RectValues(new Vector2(0f, 310f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), RectValues.CenterPivot, new Vector2(-16f, 32f)).AssignToRectTransform(layout.transform.AsRT());
                HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false).Spacing(4f).AssignToLayout(layout.AddComponent<HorizontalLayoutGroup>());

                var tabNames = EnumHelper.GetNames<Tab>();
                for (int i = 0; i < tabNames.Length; i++)
                {
                    int index = i;
                    var name = tabNames[i];
                    var tab = EditorPrefabHolder.Instance.Function1Button.Duplicate(TabsParent.transform, name);
                    tab.transform.AsRT().sizeDelta = new Vector2(92f, 43.2f);
                    var tabButton = tab.GetComponent<FunctionButtonStorage>();
                    tabButton.label.fontSize = 16;
                    tabButton.Text = name;
                    tabButton.OnClick.NewListener(() =>
                    {
                        CurrentTab = (Tab)index;
                        ActiveScrollView?.SetActive(false);
                        ScrollViews[index].SetActive(true);
                        ActiveScrollView = ScrollViews[index];
                    });

                    EditorThemeManager.ApplySelectable(tabButton.button, EditorTheme.GetGroup($"Tab Color {index + 1}"));
                    tab.AddComponent<ContrastColors>().Init(tabButton.label, tab.GetComponent<Image>());

                    TabButtons.Add(tabButton);

                    var scrollViewElement = new ScrollViewElement(ScrollViewElement.Direction.Vertical);
                    scrollViewElement.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Rect(RectValues.HorizontalAnchored.AnchoredPosition(0f, -45f).SizeDelta(0f, 635f)));
                    scrollViewElement.SetActive(i == 0);
                    ScrollViews.Add(scrollViewElement);
                    var parent = scrollViewElement.Content;
                    parent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(left: 8, right: 8, top: 8, bottom: 8);

                    switch ((Tab)i)
                    {
                        case Tab.Editor: {
                                new LabelsElement("Editor Layer").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerInt()
                                {
                                    standardArrowFunctions = false,
                                    max = int.MaxValue,
                                    leftGreaterArrowClicked = _val => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Layer = 0),
                                    leftArrowClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Layer = Mathf.Clamp(timelineObject.Layer - num, 0, int.MaxValue));
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Layer = Mathf.Clamp(num - 1, 0, int.MaxValue));
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Layer = Mathf.Clamp(timelineObject.Layer + num, 0, int.MaxValue));
                                    },
                                    rightGreaterArrowClicked = _val => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Layer = EditorTimeline.inst.Layer),
                                    rightGreaterSprite = EditorSprites.DownArrow,
                                }).Init(EditorElement.InitSettings.Default.Parent(parent));

                                new LabelsElement("Object Name").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new NumberInputElement("object name", null, new NumberInputElement.ArrowHandler()
                                {
                                    standardArrowFunctions = false,
                                    middleClicked = _val =>
                                    {
                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            switch (timelineObject.TimelineReference)
                                            {
                                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                        timelineObject.GetData<BeatmapObject>().name = _val;
                                                        break;
                                                    }
                                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                        timelineObject.GetData<BackgroundObject>().name = _val;
                                                        break;
                                                    }
                                            }
                                            timelineObject.RenderText(timelineObject.Name);
                                        });
                                        EditorManager.inst.DisplayNotification($"Added the name \"{_val}\" to all selected objects.", 4f, EditorManager.NotificationType.Success);
                                    },
                                    subClicked = _val =>
                                    {
                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            switch (timelineObject.TimelineReference)
                                            {
                                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                                        beatmapObject.name = beatmapObject.name.Remove(_val);
                                                        break;
                                                    }
                                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                                        backgroundObject.name = backgroundObject.name.Remove(_val);
                                                        break;
                                                    }
                                            }
                                            timelineObject.RenderText(timelineObject.Name);
                                        });
                                        EditorManager.inst.DisplayNotification($"Removed the name \"{_val}\" from all selected objects.", 4f, EditorManager.NotificationType.Success);
                                    },
                                    addClicked = _val =>
                                    {
                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            switch (timelineObject.TimelineReference)
                                            {
                                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                        timelineObject.GetData<BeatmapObject>().name += _val;
                                                        break;
                                                    }
                                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                        timelineObject.GetData<BackgroundObject>().name += _val;
                                                        break;
                                                    }
                                            }
                                            timelineObject.RenderText(timelineObject.Name);
                                        });
                                        EditorManager.inst.DisplayNotification($"Added the name \"{_val}\" to all selected objects.", 4f, EditorManager.NotificationType.Success);
                                    },
                                }).Init(EditorElement.InitSettings.Default.Parent(parent));

                                new LabelsElement("Editor Index").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerInt()
                                {
                                    standardArrowFunctions = false,
                                    leftGreaterArrowClicked = _val => EditorHelper.SetSelectedObjectIndexes(0),
                                    leftArrowClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            EditorHelper.AddSelectedObjectIndexes(-num);
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            EditorHelper.SetSelectedObjectIndexes(num);
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            EditorHelper.AddSelectedObjectIndexes(num);
                                    },
                                    rightGreaterArrowClicked = _val => EditorHelper.SetSelectedObjectIndexes(EditorTimeline.inst.timelineObjects.Count),
                                }).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced));
                                ButtonElement.Label1Button("Reverse Indexes", EditorHelper.ReverseSelectedObjectIndexes).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced).Rect(RectValues.Default.SizeDelta(0f, 32f)));

                                var baseColorInput = new StringInputElement("FFFFFF", null)
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                };
                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Base Color")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    baseColorInput,
                                    ButtonElement.Label1Button("Set", () =>
                                    {
                                        if (!baseColorInput.inputField)
                                            return;

                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            timelineObject.EditorData.color = baseColorInput.inputField.text;
                                            timelineObject.Render();
                                        });
                                    }, labelAlignment: TextAnchor.MiddleCenter));

                                var selectColorInput = new StringInputElement("FFFFFF", null)
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                };
                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Select Color")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    selectColorInput,
                                    ButtonElement.Label1Button("Set", () =>
                                    {
                                        if (!baseColorInput.inputField)
                                            return;

                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            timelineObject.EditorData.selectedColor = baseColorInput.inputField.text;
                                            timelineObject.Render();
                                        });
                                    }, labelAlignment: TextAnchor.MiddleCenter));

                                var textColorInput = new StringInputElement("FFFFFF", null)
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                };
                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Text Color")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    textColorInput,
                                    ButtonElement.Label1Button("Set", () =>
                                    {
                                        if (!baseColorInput.inputField)
                                            return;

                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            timelineObject.EditorData.textColor = baseColorInput.inputField.text;
                                            timelineObject.Render();
                                        });
                                    }, labelAlignment: TextAnchor.MiddleCenter));

                                var markColorInput = new StringInputElement("FFFFFF", null)
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                };
                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Mark Color")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    markColorInput,
                                    ButtonElement.Label1Button("Set", () =>
                                    {
                                        if (!baseColorInput.inputField)
                                            return;

                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            timelineObject.EditorData.markColor = baseColorInput.inputField.text;
                                            timelineObject.Render();
                                        });
                                    }, labelAlignment: TextAnchor.MiddleCenter));
                                break;
                            }
                        case Tab.Prefab: {
                                new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Assign Objects to Prefab").Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    ButtonElement.Label1Button("Assign", () =>
                                    {
                                        RTEditor.inst.selectingMultiple = true;
                                        RTEditor.inst.prefabPickerEnabled = true;
                                    }),
                                    ButtonElement.Label1Button("Remove", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.TryGetPrefabable(out IPrefabable prefabable))
                                            prefabable.RemovePrefabReference();
                                    }), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text));

                                new LabelsElement("New Prefab Instance").Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal));

                                ButtonElement.Label1Button("New Instance", () => RTEditor.inst.ShowWarningPopup("This will change the instance ID of all selected objects, assuming they all have the same ID. Are you sure you want to do this?", () =>
                                {
                                    var selected = EditorTimeline.inst.timelineObjects.Where(x => x.Selected).ToList();
                                    if (selected.Count < 0)
                                        return;

                                    var firstSelected = selected.Find(x => !x.isPrefabObject);

                                    var first = !firstSelected ? string.Empty : firstSelected.TimelineReference switch
                                    {
                                        TimelineObject.TimelineReferenceType.BeatmapObject => firstSelected.GetData<BeatmapObject>().prefabInstanceID,
                                        TimelineObject.TimelineReferenceType.BackgroundObject => firstSelected.GetData<BackgroundObject>().prefabInstanceID,
                                        _ => string.Empty,
                                    };

                                    // validate that all selected timeline objects are beatmap objects and have the same prefab instance ID.
                                    if (selected.Any(x => x.isPrefabObject || x.isBeatmapObject && x.GetData<BeatmapObject>().prefabInstanceID != first || x.isBackgroundObject && x.GetData<BackgroundObject>().prefabInstanceID != first))
                                        return;

                                    var prefabInstanceID = PAObjectBase.GetStringID();

                                    selected.ForLoop(timelineObject =>
                                    {
                                        if (timelineObject.TryGetPrefabable(out IPrefabable prefabable))
                                            prefabable.PrefabInstanceID = prefabInstanceID;
                                    });
                                    EditorManager.inst.DisplayNotification("Successfully created a new instance ID.", 2f, EditorManager.NotificationType.Success);
                                }), buttonThemeGroup: ThemeGroup.Add, graphicThemeGroup: ThemeGroup.Add_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal).Rect(RectValues.Default.SizeDelta(0f, 32f)));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    ButtonElement.Label1Button("Collapse", () => RTPrefabEditor.inst.CollapseCurrentPrefab()),
                                    ButtonElement.Label1Button("Collapse New", () => RTPrefabEditor.inst.CollapseCurrentPrefab(true)));

                                new LabelsElement("Move Prefabs X", "Move Prefabs Y").Init(EditorElement.InitSettings.Default.Parent(parent));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[0].values[0] -= num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[0].values[0] = num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[0].values[0] += num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                    }),
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[0].values[1] -= num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[0].values[1] = num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[0].values[1] += num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                    }));

                                new LabelsElement("Scale Prefabs X", "Scale Prefabs Y").Init(EditorElement.InitSettings.Default.Parent(parent));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[1].values[0] -= num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[1].values[0] = num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[1].values[0] += num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                    }),
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[1].values[1] -= num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[1].values[1] = num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                                {
                                                    prefabObject.events[1].values[1] += num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                                });
                                        },
                                    }));

                                new LabelsElement("Rotate Prefabs").Init(EditorElement.InitSettings.Default.Parent(parent));

                                new NumberInputElement("15", null, new NumberInputElement.ArrowHandlerFloat()
                                {
                                    standardArrowFunctions = false,
                                    leftArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                            {
                                                prefabObject.events[2].values[0] -= num;
                                                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                            });
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                            {
                                                prefabObject.events[2].values[0] = num;
                                                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                            });
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                            {
                                                prefabObject.events[2].values[0] += num;
                                                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                            });
                                    },
                                }).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal));

                                new LabelsElement("Change Prefab Depth").Init(EditorElement.InitSettings.Default.Parent(parent));

                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                {
                                    standardArrowFunctions = false,
                                    leftArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                            {
                                                prefabObject.depth -= num;
                                                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                            });
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                            {
                                                prefabObject.depth = num;
                                                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                            });
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachPrefabObject((PrefabObject prefabObject) =>
                                            {
                                                prefabObject.depth += num;
                                                RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                                            });
                                    },
                                }).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal));

                                new LabelsElement("Instance Data").Init(EditorElement.InitSettings.Default.Parent(parent));

                                ButtonElement.Label1Button("Paste Data", () =>
                                {
                                    if (!RTPrefabEditor.inst.copiedInstanceData)
                                    {
                                        EditorManager.inst.DisplayNotification($"No copied data.", 2f, EditorManager.NotificationType.Warning);
                                        return;
                                    }

                                    var timelineObjects = EditorTimeline.inst.SelectedPrefabObjects;
                                    foreach (var timelineObject in timelineObjects)
                                        RTPrefabEditor.inst.PasteInstanceData(timelineObject.GetData<PrefabObject>());

                                    if (!timelineObjects.IsEmpty())
                                        EditorManager.inst.DisplayNotification($"Pasted Prefab instance data.", 2f, EditorManager.NotificationType.Success);
                                }, buttonThemeGroup: ThemeGroup.Paste, graphicThemeGroup: ThemeGroup.Paste_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal).Rect(RectValues.Default.SizeDelta(0f, 32f)));
                                break;
                            }
                        case Tab.Properties: {
                                new LabelsElement("Start Time").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                {
                                    standardArrowFunctions = false,
                                    leftArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                            {
                                                timelineObject.Time -= num;

                                                switch (timelineObject.TimelineReference)
                                                {
                                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                                            break;
                                                        }
                                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                                            break;
                                                        }
                                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                                            break;
                                                        }
                                                }

                                                timelineObject.RenderPosLength();
                                            });
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                            {
                                                timelineObject.Time = num;

                                                switch (timelineObject.TimelineReference)
                                                {
                                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                                            break;
                                                        }
                                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                                            break;
                                                        }
                                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                                            break;
                                                        }
                                                }

                                                timelineObject.RenderPosLength();
                                            });
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                            {
                                                timelineObject.Time += num;

                                                switch (timelineObject.TimelineReference)
                                                {
                                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                                            break;
                                                        }
                                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                                            break;
                                                        }
                                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                                            break;
                                                        }
                                                }

                                                timelineObject.RenderPosLength();
                                            });
                                    },
                                    rightGreaterArrowClicked = _val => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        timelineObject.Time = RTLevel.Current.FixedTime;

                                        switch (timelineObject.TimelineReference)
                                        {
                                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                                    break;
                                                }
                                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                    RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                                    break;
                                                }
                                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                                    break;
                                                }
                                        }

                                        timelineObject.RenderPosLength();
                                    }),
                                    rightGreaterSprite = EditorSprites.DownArrow,
                                }).Init(EditorElement.InitSettings.Default.Parent(parent));

                                new LabelsElement("Autokill").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                {
                                    standardArrowFunctions = false,
                                    leftArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                            {
                                                if (timelineObject.isBeatmapObject)
                                                {
                                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                                    beatmapObject.autoKillOffset -= num;
                                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                                }
                                                if (timelineObject.isBackgroundObject)
                                                {
                                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                                    backgroundObject.autoKillOffset -= num;
                                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                                }
                                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                            });
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                            {
                                                if (timelineObject.isBeatmapObject)
                                                {
                                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                                    beatmapObject.autoKillOffset = num;
                                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                                }
                                                if (timelineObject.isBackgroundObject)
                                                {
                                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                                    backgroundObject.autoKillOffset = num;
                                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                                }
                                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                            });
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                            {
                                                if (timelineObject.isBeatmapObject)
                                                {
                                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                                    beatmapObject.autoKillOffset += num;
                                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                                }
                                                if (timelineObject.isBackgroundObject)
                                                {
                                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                                    backgroundObject.autoKillOffset += num;
                                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                                }
                                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                            });
                                    },
                                    rightGreaterArrowClicked = _val => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();

                                            float num = 0f;

                                            if (beatmapObject.autoKillType == AutoKillType.SongTime)
                                                num = AudioManager.inst.CurrentAudioSource.time;
                                            else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                                            if (num < 0f)
                                                num = 0f;

                                            beatmapObject.autoKillOffset = num;

                                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                        }
                                        if (timelineObject.isBackgroundObject)
                                        {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();

                                            float num = 0f;

                                            if (backgroundObject.autoKillType == AutoKillType.SongTime)
                                                num = AudioManager.inst.CurrentAudioSource.time;
                                            else num = AudioManager.inst.CurrentAudioSource.time - backgroundObject.StartTime;

                                            if (num < 0f)
                                                num = 0f;

                                            backgroundObject.autoKillOffset = num;

                                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                        }
                                    }),
                                    rightGreaterSprite = EditorSprites.DownArrow,
                                }).Init(EditorElement.InitSettings.Default.Parent(parent));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    ButtonElement.Label1Button("No Autokill", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.autoKillType = AutoKillType.NoAutokill;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                        }
                                        if (timelineObject.isBackgroundObject)
                                        {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.autoKillType = AutoKillType.NoAutokill;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                        }
                                        timelineObject.RenderPosLength();
                                    })),
                                    ButtonElement.Label1Button("Last KF", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.autoKillType = AutoKillType.LastKeyframe;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                        }
                                        if (timelineObject.isBackgroundObject)
                                        {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.autoKillType = AutoKillType.LastKeyframe;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                        }
                                        timelineObject.RenderPosLength();
                                    })),
                                    ButtonElement.Label1Button("Last KF Offset", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                        }
                                        if (timelineObject.isBackgroundObject)
                                        {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.autoKillType = AutoKillType.LastKeyframeOffset;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                        }
                                        timelineObject.RenderPosLength();
                                    })),
                                    ButtonElement.Label1Button("Fixed Time", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.autoKillType = AutoKillType.FixedTime;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                        }
                                        if (timelineObject.isBackgroundObject)
                                        {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.autoKillType = AutoKillType.FixedTime;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                        }
                                        timelineObject.RenderPosLength();
                                    })),
                                    ButtonElement.Label1Button("Song Time", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.autoKillType = AutoKillType.SongTime;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                        }
                                        if (timelineObject.isBackgroundObject)
                                        {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.autoKillType = AutoKillType.SongTime;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                                        }
                                        timelineObject.RenderPosLength();
                                    })));

                                ButtonElement.Label1Button("Set Autokill to Scaled 0x0", () => MultiObjectEditor.inst.ForEachBeatmapObject(timelineObject =>
                                {
                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                    beatmapObject.SetAutokillToScale(GameData.Current.beatmapObjects);
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                    timelineObject.RenderPosLength();
                                })).Init(EditorElement.InitSettings.Default.Parent(parent).Rect(RectValues.Default.SizeDelta(0f, 32f)));

                                new LabelsElement("Parent").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.ChildForceExpandWidth(false).Spacing(4f),
                                    new ButtonElement(ButtonElement.Type.Sprite, "Search List", ObjectEditor.inst.ShowObjectSearch)
                                    {
                                        buttonThemeGroup = ThemeGroup.Function_2,
                                        sprite = EditorSprites.SearchSprite,
                                    },
                                    new ButtonElement(ButtonElement.Type.Icon, "Picker", () =>
                                    {
                                        RTEditor.inst.parentPickerEnabled = true;
                                        RTEditor.inst.selectingMultiple = true;
                                    })
                                    {
                                        buttonThemeGroup = ThemeGroup.Picker,
                                        sprite = EditorSprites.DropperSprite,
                                    },
                                    new ButtonElement(ButtonElement.Type.Icon, "Remove", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to remove parents from all selected objects? This <b>CANNOT</b> be undone!", () =>
                                    {
                                        MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                        {
                                            beatmapObject.Parent = string.Empty;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                                        });
                                        MultiObjectEditor.inst.ForEachPrefabObject(prefabObject =>
                                        {
                                            prefabObject.Parent = string.Empty;
                                            RTLevel.Current?.UpdatePrefab(prefabObject, ObjectContext.PARENT_CHAIN);
                                        });
                                    }))
                                    {
                                        buttonThemeGroup = ThemeGroup.Close,
                                        graphicThemeGroup = ThemeGroup.Close_X,
                                        sprite = EditorSprites.CloseSprite,
                                    });

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Desync")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.desync = true;
                                            RTLevel.Current?.UpdateObject(beatmapObject);
                                        }
                                        if (timelineObject.isPrefabObject)
                                        {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            prefabObject.desync = true;
                                            RTLevel.Current?.UpdatePrefab(prefabObject);
                                        }
                                    })),
                                    ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.desync = false;
                                            RTLevel.Current?.UpdateObject(beatmapObject);
                                        }
                                        if (timelineObject.isPrefabObject)
                                        {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            prefabObject.desync = false;
                                            RTLevel.Current?.UpdatePrefab(prefabObject);
                                        }
                                    })),
                                    ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (timelineObject.isBeatmapObject)
                                        {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.desync = !beatmapObject.desync;
                                            RTLevel.Current?.UpdateObject(beatmapObject);
                                        }
                                        if (timelineObject.isPrefabObject)
                                        {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            prefabObject.desync = !prefabObject.desync;
                                            RTLevel.Current?.UpdatePrefab(prefabObject);
                                        }
                                    })));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Position Toggle")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentToggle(0, 1)),
                                    ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentToggle(0, 0)),
                                    ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentToggle(0, 2)));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Scale Toggle")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentToggle(1, 1)),
                                    ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentToggle(1, 0)),
                                    ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentToggle(01, 2)));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Rotation Toggle")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentToggle(2, 1)),
                                    ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentToggle(2, 0)),
                                    ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentToggle(2, 2)));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Position Offset (Delay)")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(0, num, MathOperation.Subtract);
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(0, num, MathOperation.Subtract);
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(0, num, MathOperation.Addition);
                                        },
                                    })
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                    });

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Scale Offset (Delay)")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(1, num, MathOperation.Subtract);
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(1, num, MathOperation.Set);
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(1, num, MathOperation.Addition);
                                        },
                                    })
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                    });

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Rotation Offset (Delay)")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(2, num, MathOperation.Subtract);
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(2, num, MathOperation.Set);
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentOffset(2, num, MathOperation.Addition);
                                        },
                                    })
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                    });

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Position Additive")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentAdditive(0, 1)),
                                    ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentAdditive(0, 0)),
                                    ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentAdditive(0, 2)));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Scale Additive")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentAdditive(1, 1)),
                                    ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentAdditive(1, 0)),
                                    ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentAdditive(01, 2)));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Rotation Additive")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentAdditive(2, 1)),
                                    ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentAdditive(2, 0)),
                                    ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentAdditive(2, 2)));

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Position Parallax (Delay)")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(0, num, MathOperation.Subtract);
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(0, num, MathOperation.Set);
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(0, num, MathOperation.Addition);
                                        },
                                    })
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                    });

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Scale Parallax (Delay)")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(1, num, MathOperation.Subtract);
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(1, num, MathOperation.Set);
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(1, num, MathOperation.Addition);
                                        },
                                    })
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                    });

                                new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                    new LabelElement("Rotation Parallax (Delay)")
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                    },
                                    new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                    {
                                        standardArrowFunctions = false,
                                        leftArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(2, num, MathOperation.Subtract);
                                        },
                                        middleClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(2, num, MathOperation.Set);
                                        },
                                        rightArrowClicked = _val =>
                                        {
                                            if (float.TryParse(_val, out float num))
                                                MultiObjectEditor.inst.SetParentParallax(2, num, MathOperation.Addition);
                                        },
                                    })
                                    {
                                        layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                    });

                                new LabelsElement("Render Depth").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerInt()
                                {
                                    standardArrowFunctions = false,
                                    leftArrowClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.Depth -= num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                            });
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.Depth = num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                            });
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (int.TryParse(_val, out int num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.Depth += num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                            });
                                    },
                                }).Init(EditorElement.InitSettings.Default.Parent(parent));
                                break;
                            }
                        case Tab.Modifiers: {
                                new LabelsElement("Clear Data").Init(EditorElement.InitSettings.Default.Parent(parent));
                                ButtonElement.Label1Button("Clear Modifiers", () => RTEditor.inst.ShowWarningPopup("You are about to clear modifiers from all selected objects, this <b>CANNOT</b> be undone!", () =>
                                {
                                    MultiObjectEditor.inst.ForEachModifyable(modifyable =>
                                    {
                                        modifyable.Modifiers.Clear();
                                        if (modifyable is BeatmapObject beatmapObject)
                                            RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                        if (modifyable is BackgroundObject backgroundObject)
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                    });
                                    RTLevel.Current?.RecalculateObjectStates();
                                }), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced).Rect(RectValues.Default.SizeDelta(0f, 32f)));
                                ButtonElement.Label1Button("Clear Tags", () => RTEditor.inst.ShowWarningPopup("You are about to clear tags from all selected objects, this <b>CANNOT</b> be undone!", () =>
                                {
                                    MultiObjectEditor.inst.ForEachModifyable(modifyable => modifyable.Tags.Clear());
                                }), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced).Rect(RectValues.Default.SizeDelta(0f, 32f)));

                                new LabelsElement("Tags").Init(EditorElement.InitSettings.Default.Parent(parent));
                                new NumberInputElement("object group", null, new NumberInputElement.ArrowHandler()
                                {
                                    standardArrowFunctions = false,
                                    subClicked = _val =>
                                    {
                                        MultiObjectEditor.inst.ForEachModifyable(modifyable => modifyable.Tags.Remove(_val));
                                        EditorManager.inst.DisplayNotification($"Removed the tag \"{_val}\" from all selected objects.", 4f, EditorManager.NotificationType.Success);
                                    },
                                    addClicked = _val =>
                                    {
                                        MultiObjectEditor.inst.ForEachModifyable(modifyable =>
                                        {
                                            if (!modifyable.Tags.Contains(_val))
                                                modifyable.Tags.Add(_val);
                                        });
                                        EditorManager.inst.DisplayNotification($"Added the tag \"{_val}\" to all selected objects.", 4f, EditorManager.NotificationType.Success);
                                    },
                                }).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced));
                                break;
                            }
                        case Tab.Keyframes: {
                                new LabelsElement("Clear").Init(EditorElement.InitSettings.Default.Parent(parent));
                                ButtonElement.Label1Button("Clear All Keyframes", () => RTEditor.inst.ShowWarningPopup("You are about to clear animations from all selected objects, this <b>CANNOT</b> be undone!", () =>
                                {
                                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                    {
                                        var bm = timelineObject.GetData<BeatmapObject>();
                                        foreach (var tkf in bm.TimelineKeyframes)
                                            CoreHelper.Delete(tkf.GameObject);
                                        bm.TimelineKeyframes.Clear();
                                        for (int i = 0; i < bm.events.Count; i++)
                                        {
                                            bm.events[i].Sort((a, b) => a.time.CompareTo(b.time));
                                            var firstKF = bm.events[i][0].Copy(false);
                                            bm.events[i].Clear();
                                            bm.events[i].Add(firstKF);
                                        }
                                        if (EditorTimeline.inst.SelectedObjects.Count == 1)
                                        {
                                            ObjectEditor.inst.Dialog.Timeline.ResizeKeyframeTimeline(bm);
                                            ObjectEditor.inst.Dialog.Timeline.RenderKeyframes(bm);
                                        }

                                        RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                        EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    }
                                }), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Rect(RectValues.Default.SizeDelta(0f, 32f)));
                                ButtonElement.Label1Button("Clear Position Keyframes", () => MultiObjectEditor.inst.ClearKeyframes(0), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Rect(RectValues.Default.SizeDelta(0f, 32f)));
                                ButtonElement.Label1Button("Clear Scale Keyframes", () => MultiObjectEditor.inst.ClearKeyframes(1), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Rect(RectValues.Default.SizeDelta(0f, 32f)));
                                ButtonElement.Label1Button("Clear Rotation Keyframes", () => MultiObjectEditor.inst.ClearKeyframes(2), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Rect(RectValues.Default.SizeDelta(0f, 32f)));
                                ButtonElement.Label1Button("Clear Color Keyframes", () => MultiObjectEditor.inst.ClearKeyframes(3), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Rect(RectValues.Default.SizeDelta(0f, 32f)));
                                break;
                            }
                    }
                }

                ActiveScrollView = ScrollViews[0];
            }
            else
            {
                var multiObjectEditorDialog = GameObject.transform;

                EditorThemeManager.ApplyGraphic(multiObjectEditorDialog.GetComponent<Image>(), ThemeGroup.Background_1);

                var dataLeft = multiObjectEditorDialog.Find("data/left");

                dataLeft.gameObject.SetActive(true);

                CoreHelper.DestroyChildren(dataLeft);

                var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(dataLeft, "Scroll View");
                scrollView.transform.AsRT().anchoredPosition = new Vector2(240f, 345f);
                scrollView.transform.AsRT().sizeDelta = new Vector2(410f, 690f);

                var parent = scrollView.transform.Find("Viewport/Content");
                multiObjectContent = parent;

                var title = multiObjectEditorDialog.Find("data/right/Object Editor Title");
                title.SetParent(multiObjectEditorDialog);
                RectValues.FullAnchored.AnchoredPosition(0f, -16f).AnchorMin(0f, 1f).SizeDelta(0f, 32f).AssignToRectTransform(title.AsRT());

                var textHolder = multiObjectEditorDialog.Find("data/right/text holder/Text");
                var textHolderText = textHolder.GetComponent<Text>();

                EditorThemeManager.ApplyLightText(textHolderText);

                Text = textHolderText;

                textHolderText.fontSize = 22;

                textHolder.AsRT().anchoredPosition = new Vector2(0f, -125f);
                textHolder.AsRT().sizeDelta = new Vector2(-68f, 0f);

                CoreHelper.Destroy(dataLeft.GetComponent<VerticalLayoutGroup>());

                GenerateLabels(parent, 32f, new Label("- Main Properties -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));
                // Layers (done)
                {
                    GenerateLabels(parent, 32f, "Set Group Editor Layer");

                    var inputFieldStorage = GenerateInputField(parent, "layer", "1", "Enter layer...", true, true, true);
                    inputFieldStorage.GetComponent<HorizontalLayoutGroup>().spacing = 0f;
                    inputFieldStorage.leftGreaterButton.onClick.NewListener(() =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            timelineObject.Layer = 0;
                    });
                    inputFieldStorage.leftButton.onClick.NewListener(() =>
                    {
                        if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            timelineObject.Layer = Mathf.Clamp(timelineObject.Layer - num, 0, int.MaxValue);
                    });
                    inputFieldStorage.middleButton.onClick.NewListener(() =>
                    {
                        if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            timelineObject.Layer = Mathf.Clamp(num - 1, 0, int.MaxValue);
                    });
                    inputFieldStorage.rightButton.onClick.NewListener(() =>
                    {
                        if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            timelineObject.Layer = Mathf.Clamp(timelineObject.Layer + num, 0, int.MaxValue);
                    });
                    inputFieldStorage.rightGreaterButton.image.sprite = EditorSprites.DownArrow;
                    inputFieldStorage.rightGreaterButton.onClick.NewListener(() =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            timelineObject.Layer = EditorTimeline.inst.Layer;
                    });
                    TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                    EditorHelper.SetComplexity(inputFieldStorage.leftGreaterButton.gameObject, Complexity.Normal);
                }

                // Depth (done)
                {
                    GenerateLabels(parent, 32f, "Set Group Render Depth");

                    var inputFieldStorage = GenerateInputField(parent, "depth", "1", "Enter depth...", true);
                    inputFieldStorage.leftButton.onClick.NewListener(() =>
                    {
                        if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.Depth -= num;
                            RTLevel.Current?.UpdateObject(bm, ObjectContext.VISUAL_OFFSET);
                        }
                    });
                    inputFieldStorage.middleButton.onClick.NewListener(() =>
                    {
                        if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.Depth = num;
                            RTLevel.Current?.UpdateObject(bm, ObjectContext.VISUAL_OFFSET);
                        }
                    });
                    inputFieldStorage.rightButton.onClick.NewListener(() =>
                    {
                        if (!int.TryParse(inputFieldStorage.inputField.text, out int num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.Depth += num;
                            RTLevel.Current?.UpdateObject(bm, ObjectContext.VISUAL_OFFSET);
                        }
                    });
                    TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));
                }

                // Song Time (done)
                {
                    GenerateLabels(parent, 32f, "Set Song Time");

                    var inputFieldStorage = GenerateInputField(parent, "time", "1", "Enter time...", true);
                    inputFieldStorage.leftButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        //float first = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            //timelineObject.Time = AudioManager.inst.CurrentAudioSource.time - first + timelineObject.Time + num;
                            timelineObject.Time -= num;

                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                        break;
                                    }
                            }

                            timelineObject.RenderPosLength();
                        }
                    });
                    inputFieldStorage.middleButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;

                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            timelineObject.Time = num;

                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                        break;
                                    }
                            }

                            timelineObject.RenderPosLength();
                        }
                    });
                    inputFieldStorage.rightButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        //float first = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            //timelineObject.Time = AudioManager.inst.CurrentAudioSource.time - first + timelineObject.Time - num;
                            timelineObject.Time += num;

                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                        break;
                                    }
                            }

                            timelineObject.RenderPosLength();
                        }
                    });
                    TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));
                }

                // Autokill Offset (done)
                {
                    var labels = GenerateLabels(parent, 32f, "Set Autokill Offset");

                    var inputFieldStorage = GenerateInputField(parent, "autokill offset", "0", "Enter autokill...", true);
                    inputFieldStorage.leftButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            if (timelineObject.isBeatmapObject)
                            {
                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                beatmapObject.autoKillOffset -= num;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                            }
                            if (timelineObject.isBackgroundObject)
                            {
                                var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                backgroundObject.autoKillOffset -= num;
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                            }
                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    });
                    inputFieldStorage.middleButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            if (timelineObject.isBeatmapObject)
                            {
                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                beatmapObject.autoKillOffset = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                            }
                            if (timelineObject.isBackgroundObject)
                            {
                                var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                backgroundObject.autoKillOffset = num;
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                            }
                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    });
                    inputFieldStorage.rightButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            if (timelineObject.isBeatmapObject)
                            {
                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                beatmapObject.autoKillOffset += num;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                            }
                            if (timelineObject.isBackgroundObject)
                            {
                                var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                backgroundObject.autoKillOffset += num;
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                            }
                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    });
                    TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(inputFieldStorage.gameObject, Complexity.Normal);
                }

                // Name (done)
                {
                    GenerateLabels(parent, 32f, "Set Name");

                    var multiNameSet = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, "name");
                    multiNameSet.transform.localScale = Vector3.one;
                    var inputFieldStorage = multiNameSet.GetComponent<InputFieldStorage>();

                    multiNameSet.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

                    inputFieldStorage.inputField.onValueChanged.ClearAll();
                    inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                    inputFieldStorage.inputField.characterLimit = 0;
                    inputFieldStorage.inputField.text = "name";
                    inputFieldStorage.inputField.transform.AsRT().sizeDelta = new Vector2(300f, 32f);
                    inputFieldStorage.inputField.GetPlaceholderText().text = "Enter name...";

                    CoreHelper.Delete(inputFieldStorage.leftGreaterButton);
                    CoreHelper.Delete(inputFieldStorage.leftButton);
                    CoreHelper.Delete(inputFieldStorage.rightButton);
                    CoreHelper.Delete(inputFieldStorage.rightGreaterButton);

                    EditorThemeManager.ApplyInputField(inputFieldStorage);

                    inputFieldStorage.addButton.gameObject.SetActive(true);
                    inputFieldStorage.middleButton.onClick.NewListener(() =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            if (timelineObject.isBeatmapObject)
                                timelineObject.GetData<BeatmapObject>().name = inputFieldStorage.inputField.text;
                            if (timelineObject.isBackgroundObject)
                                timelineObject.GetData<BackgroundObject>().name = inputFieldStorage.inputField.text;
                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    });

                    EditorThemeManager.ApplySelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);

                    var mtnLeftLE = inputFieldStorage.addButton.gameObject.AddComponent<LayoutElement>();
                    mtnLeftLE.ignoreLayout = true;

                    inputFieldStorage.addButton.transform.AsRT().anchoredPosition = new Vector2(339f, 0f);
                    inputFieldStorage.addButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                    inputFieldStorage.addButton.onClick.NewListener(() =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            if (timelineObject.isBeatmapObject)
                                timelineObject.GetData<BeatmapObject>().name += inputFieldStorage.inputField.text;
                            if (timelineObject.isBackgroundObject)
                                timelineObject.GetData<BackgroundObject>().name += inputFieldStorage.inputField.text;
                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    });
                }

                // Tags (done)
                {
                    var labels = GenerateLabels(parent, 32f, "Add a Tag");

                    var multiNameSet = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, "name");
                    multiNameSet.transform.localScale = Vector3.one;
                    var inputFieldStorage = multiNameSet.GetComponent<InputFieldStorage>();

                    multiNameSet.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

                    inputFieldStorage.inputField.onValueChanged.ClearAll();
                    inputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                    inputFieldStorage.inputField.characterLimit = 0;
                    inputFieldStorage.inputField.text = "object group";
                    inputFieldStorage.inputField.transform.AsRT().sizeDelta = new Vector2(300f, 32f);
                    inputFieldStorage.inputField.GetPlaceholderText().text = "Enter a tag...";

                    CoreHelper.Delete(inputFieldStorage.leftGreaterButton);
                    CoreHelper.Delete(inputFieldStorage.leftButton);
                    CoreHelper.Delete(inputFieldStorage.middleButton);
                    CoreHelper.Delete(inputFieldStorage.rightButton);
                    CoreHelper.Delete(inputFieldStorage.rightGreaterButton);

                    EditorThemeManager.ApplyInputField(inputFieldStorage);

                    inputFieldStorage.addButton.gameObject.SetActive(true);
                    inputFieldStorage.addButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
                    inputFieldStorage.addButton.onClick.NewListener(() =>
                    {
                        var tag = inputFieldStorage.inputField.text;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        if (!beatmapObject.tags.Contains(tag))
                                            beatmapObject.tags.Add(tag);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        var prefabObject = timelineObject.GetData<PrefabObject>();
                                        if (!prefabObject.tags.Contains(tag))
                                            prefabObject.tags.Add(tag);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        if (!backgroundObject.tags.Contains(tag))
                                            backgroundObject.tags.Add(tag);
                                        break;
                                    }
                            }
                        }
                    });

                    inputFieldStorage.subButton.gameObject.SetActive(true);
                    inputFieldStorage.subButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);
                    inputFieldStorage.subButton.onClick.NewListener(() =>
                    {
                        var tag = inputFieldStorage.inputField.text;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        timelineObject.GetData<BeatmapObject>().tags.Remove(tag);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        timelineObject.GetData<PrefabObject>().tags.Remove(tag);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        timelineObject.GetData<BackgroundObject>().tags.Remove(tag);
                                        break;
                                    }
                            }
                        }
                    });

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(multiNameSet, Complexity.Advanced);
                }

                // Timeline Object Index (done)
                {
                    var labels1 = GenerateLabels(parent, 32f, "Set Group Index");

                    var inputFieldStorage = GenerateInputField(parent, "indexer", "1", "Enter index...", true, true, true);
                    inputFieldStorage.GetComponent<HorizontalLayoutGroup>().spacing = 0f;
                    inputFieldStorage.leftGreaterButton.onClick.NewListener(() => { EditorHelper.SetSelectedObjectIndexes(0); });
                    inputFieldStorage.leftButton.onClick.NewListener(() =>
                    {
                        if (int.TryParse(inputFieldStorage.inputField.text, out int num))
                            EditorHelper.AddSelectedObjectIndexes(-num);
                    });
                    inputFieldStorage.middleButton.onClick.NewListener(() =>
                    {
                        if (int.TryParse(inputFieldStorage.inputField.text, out int num))
                            EditorHelper.SetSelectedObjectIndexes(num);
                    });
                    inputFieldStorage.rightButton.onClick.NewListener(() =>
                    {
                        if (int.TryParse(inputFieldStorage.inputField.text, out int num))
                            EditorHelper.AddSelectedObjectIndexes(num);
                    });
                    inputFieldStorage.rightGreaterButton.onClick.NewListener(() => EditorHelper.SetSelectedObjectIndexes(EditorTimeline.inst.timelineObjects.Count));
                    TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                    var buttons1 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Reverse Indexes", EditorHelper.ReverseSelectedObjectIndexes));

                    EditorHelper.SetComplexity(labels1, Complexity.Normal);
                    EditorHelper.SetComplexity(inputFieldStorage.leftGreaterButton.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(inputFieldStorage.gameObject, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                // Editor Colors (done)
                {
                    SetupEditorColorSetter(parent, "base color", "Set Base Color", "Set Base Color...", "Set", inputField =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            var editorData = timelineObject.EditorData;
                            editorData.color = inputField.text;
                            timelineObject.Render();
                        }
                    });
                    SetupEditorColorSetter(parent, "select color", "Set Select Color", "Set Select Color...", "Set", inputField =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            var editorData = timelineObject.EditorData;
                            editorData.selectedColor = inputField.text;
                            timelineObject.Render();
                        }
                    });
                    SetupEditorColorSetter(parent, "text color", "Set Text Color", "Set Text Color...", "Set", inputField =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            var editorData = timelineObject.EditorData;
                            editorData.textColor = inputField.text;
                            timelineObject.Render();
                        }
                    });
                    SetupEditorColorSetter(parent, "mark color", "Set Mark Color", "Set Mark Color...", "Set", inputField =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            var editorData = timelineObject.EditorData;
                            editorData.markColor = inputField.text;
                            timelineObject.Render();
                        }
                    });
                }

                GeneratePad(parent);
                GenerateLabels(parent, 32f, new Label("- Actions -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

                // Clear data (donE)
                {
                    var labels = GenerateLabels(parent, 32f, "Clear data from objects");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                         new ButtonFunction("Clear tags", () =>
                         {
                             RTEditor.inst.ShowWarningPopup("You are about to clear tags from all selected objects, this <b>CANNOT</b> be undone!", () =>
                             {
                                 foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                                 {
                                     if (timelineObject.isBeatmapObject)
                                         timelineObject.GetData<BeatmapObject>().tags.Clear();
                                     if (timelineObject.isBackgroundObject)
                                         timelineObject.GetData<BackgroundObject>().tags.Clear();
                                 }
                             });
                         }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text) { FontSize = 16 },
                         new ButtonFunction("Clear anims", () =>
                         {
                             RTEditor.inst.ShowWarningPopup("You are about to clear animations from all selected objects, this <b>CANNOT</b> be undone!", () =>
                             {
                                 foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                 {
                                     var bm = timelineObject.GetData<BeatmapObject>();
                                     foreach (var tkf in bm.TimelineKeyframes)
                                         CoreHelper.Delete(tkf.GameObject);
                                     bm.TimelineKeyframes.Clear();
                                     for (int i = 0; i < bm.events.Count; i++)
                                     {
                                         bm.events[i].Sort((a, b) => a.time.CompareTo(b.time));
                                         var firstKF = bm.events[i][0].Copy(false);
                                         bm.events[i].Clear();
                                         bm.events[i].Add(firstKF);
                                     }
                                     if (EditorTimeline.inst.SelectedObjects.Count == 1)
                                     {
                                         ObjectEditor.inst.Dialog.Timeline.ResizeKeyframeTimeline(bm);
                                         ObjectEditor.inst.Dialog.Timeline.RenderKeyframes(bm);
                                     }

                                     RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                     EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                 }
                             });
                         }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text) { FontSize = 16 },
                         new ButtonFunction("Clear modifiers", () =>
                         {
                             RTEditor.inst.ShowWarningPopup("You are about to clear modifiers from all selected objects, this <b>CANNOT</b> be undone!", () =>
                             {
                                 foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                                 {
                                     if (timelineObject.isBeatmapObject)
                                     {
                                         var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                         beatmapObject.modifiers.Clear();
                                         RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                     }
                                     if (timelineObject.isBackgroundObject)
                                     {
                                         var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                         backgroundObject.modifiers.Clear();
                                         RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                     }
                                 }
                                 RTLevel.Current?.RecalculateObjectStates();
                             });
                         }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text) { FontSize = 16 });

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                // Optimization (done)
                {
                    var labels = GenerateLabels(parent, 32f, "Auto optimize objects");
                    var buttons1 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Optimize", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                        {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            beatmapObject.SetAutokillToScale(GameData.Current.beatmapObjects);
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                            timelineObject.RenderPosLength();
                        }
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }

                // Song Time Autokill (done)
                {
                    var labels = GenerateLabels(parent, 32f, "Set autokill to current time");
                    var buttons1 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Set", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            if (timelineObject.isBeatmapObject)
                            {
                                var beatmapObject = timelineObject.GetData<BeatmapObject>();

                                float num = 0f;

                                if (beatmapObject.autoKillType == AutoKillType.SongTime)
                                    num = AudioManager.inst.CurrentAudioSource.time;
                                else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                                if (num < 0f)
                                    num = 0f;

                                beatmapObject.autoKillOffset = num;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                            }
                            if (timelineObject.isBackgroundObject)
                            {
                                var backgroundObject = timelineObject.GetData<BackgroundObject>();

                                float num = 0f;

                                if (backgroundObject.autoKillType == AutoKillType.SongTime)
                                    num = AudioManager.inst.CurrentAudioSource.time;
                                else num = AudioManager.inst.CurrentAudioSource.time - backgroundObject.StartTime;

                                if (num < 0f)
                                    num = 0f;

                                backgroundObject.autoKillOffset = num;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.AUTOKILL);
                            }
                        }
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                GeneratePad(parent);
                GenerateLabels(parent, 32f, new Label("- Object Properties -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

                // Autokill Type (done)
                {
                    var labels = GenerateLabels(parent, 32f, "Set Autokill Type");

                    var buttons1 = GenerateButtons(parent, 48f, 8f,
                        new ButtonFunction("No Autokill", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                if (timelineObject.isBeatmapObject)
                                {

                                    var bm = timelineObject.GetData<BeatmapObject>();
                                    bm.autoKillType = AutoKillType.NoAutokill;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.AUTOKILL);
                                }
                                if (timelineObject.isBackgroundObject)
                                {
                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                    backgroundObject.autoKillType = AutoKillType.NoAutokill;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.AUTOKILL);
                                }
                            }
                        }),
                        new ButtonFunction("Last KF", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                if (timelineObject.isBeatmapObject)
                                {

                                    var bm = timelineObject.GetData<BeatmapObject>();
                                    bm.autoKillType = AutoKillType.LastKeyframe;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.AUTOKILL);
                                }
                                if (timelineObject.isBackgroundObject)
                                {
                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                    backgroundObject.autoKillType = AutoKillType.LastKeyframe;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.AUTOKILL);
                                }
                            }
                        }),
                        new ButtonFunction("Last KF Offset", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                if (timelineObject.isBeatmapObject)
                                {

                                    var bm = timelineObject.GetData<BeatmapObject>();
                                    bm.autoKillType = AutoKillType.LastKeyframeOffset;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.AUTOKILL);
                                }
                                if (timelineObject.isBackgroundObject)
                                {
                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                    backgroundObject.autoKillType = AutoKillType.LastKeyframeOffset;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.AUTOKILL);
                                }
                            }
                        }),
                        new ButtonFunction("Fixed Time", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                if (timelineObject.isBeatmapObject)
                                {

                                    var bm = timelineObject.GetData<BeatmapObject>();
                                    bm.autoKillType = AutoKillType.FixedTime;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.AUTOKILL);
                                }
                                if (timelineObject.isBackgroundObject)
                                {
                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                    backgroundObject.autoKillType = AutoKillType.FixedTime;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.AUTOKILL);
                                }
                            }
                        }),
                        new ButtonFunction("Song Time", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                if (timelineObject.isBeatmapObject)
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();
                                    bm.autoKillType = AutoKillType.SongTime;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.AUTOKILL);
                                }
                                if (timelineObject.isBackgroundObject)
                                {
                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                    backgroundObject.autoKillType = AutoKillType.SongTime;

                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.AUTOKILL);
                                }
                            }
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                // Set Parent (done)
                {
                    GenerateLabels(parent, 32f, "Set Parent");
                    GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Search list", ObjectEditor.inst.ShowParentSearch),
                        new ButtonFunction("Picker", () =>
                        {
                            RTEditor.inst.parentPickerEnabled = true;
                            RTEditor.inst.selectingMultiple = true;
                        }),
                        new ButtonFunction("Remove", () =>
                        {
                            RTEditor.inst.ShowWarningPopup("Are you sure you want to remove parents from all selected objects? This <b>CANNOT</b> be undone!", () =>
                            {
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.Parent = string.Empty;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                                }

                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup);
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                }

                // Parent Desync (done)
                {
                    var labels = GenerateLabels(parent, 32f, "Modify parent desync");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.FindAll(x => x.isBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().desync = true;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.FindAll(x => x.isBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().desync = false;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.FindAll(x => x.isBeatmapObject))
                        {
                            timelineObject.GetData<BeatmapObject>().desync = !timelineObject.GetData<BeatmapObject>().desync;

                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
                }

                // Force Snap BPM
                {
                    var labels = GenerateLabels(parent, 32f, "Force Snap Start Time to BPM");
                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Snap", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Time = RTEditor.SnapToBPM(timelineObject.Time);

                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                            break;
                                        }
                                }

                                timelineObject.RenderPosLength();
                            }
                        }),
                        new ButtonFunction("Snap Offset", () =>
                        {
                            var time = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);
                            var snappedTime = RTEditor.SnapToBPM(time);
                            var distance = -time + snappedTime;
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Time += distance;

                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.START_TIME);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.TIME);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.START_TIME);
                                            break;
                                        }
                                }

                                timelineObject.RenderPosLength();
                            }
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                // Object Type
                {
                    GenerateLabels(parent, 32f, "Set Object Type");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Sub", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                int objectType = (int)bm.objectType;

                                objectType--;
                                if (objectType < 0)
                                    objectType = 4;

                                bm.objectType = (BeatmapObject.ObjectType)objectType;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                            }
                        }),
                        new ButtonFunction("Add", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                int objectType = (int)bm.objectType;

                                objectType++;
                                if (objectType > 4)
                                    objectType = 0;

                                bm.objectType = (BeatmapObject.ObjectType)objectType;


                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                            }
                        }));

                    GenerateButtons(parent, 48f, 8f,
                        new ButtonFunction(nameof(BeatmapObject.ObjectType.Normal), () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.objectType = BeatmapObject.ObjectType.Normal;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                            }
                        }),
                        new ButtonFunction(nameof(BeatmapObject.ObjectType.Helper), () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.objectType = BeatmapObject.ObjectType.Helper;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                            }
                        }),
                        new ButtonFunction("Deco", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.objectType = BeatmapObject.ObjectType.Decoration;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                            }
                        }),
                        new ButtonFunction(nameof(BeatmapObject.ObjectType.Empty), () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.objectType = BeatmapObject.ObjectType.Empty;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                            }
                        }),
                        new ButtonFunction(nameof(BeatmapObject.ObjectType.Solid), () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.objectType = BeatmapObject.ObjectType.Solid;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.OBJECT_TYPE);
                            }
                        }));

                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }
            
                // Color Blend Mode
                {
                    var labels = GenerateLabels(parent, 32f, "Set Color Blend Mode");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Sub", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                int colorBlendMode = (int)bm.colorBlendMode;

                                colorBlendMode--;
                                if (colorBlendMode < 0)
                                    colorBlendMode = 2;

                                bm.colorBlendMode = (ColorBlendMode)colorBlendMode;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Add", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                int colorBlendMode = (int)bm.colorBlendMode;

                                colorBlendMode--;
                                if (colorBlendMode > 2)
                                    colorBlendMode = 0;

                                bm.colorBlendMode = (ColorBlendMode)colorBlendMode;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }));

                    var buttons2 = GenerateButtons(parent, 48f, 8f,
                        new ButtonFunction("Normal", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.colorBlendMode = ColorBlendMode.Normal;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Multiply", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.colorBlendMode = ColorBlendMode.Multiply;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Additive", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.colorBlendMode = ColorBlendMode.Additive;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
                }

                // Gradient Type
                {
                    var labels = GenerateLabels(parent, 32f, "Set Gradient Type");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Sub", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                int gradientType = (int)bm.gradientType;

                                gradientType--;
                                if (gradientType < 0)
                                    gradientType = 4;

                                bm.gradientType = (GradientType)gradientType;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Add", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                int gradientType = (int)bm.gradientType;

                                gradientType--;
                                if (gradientType > 4)
                                    gradientType = 0;

                                bm.gradientType = (GradientType)gradientType;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }));

                    var buttons2 = GenerateButtons(parent, 48f, 8f,
                        new ButtonFunction("None", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.gradientType = GradientType.Normal;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Linear Right", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.gradientType = GradientType.RightLinear;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Linear Left", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.gradientType = GradientType.LeftLinear;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Radial In", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.gradientType = GradientType.OutInRadial;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Radial Out", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();
                                bm.gradientType = GradientType.InOutRadial;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                RTLevel.Current?.UpdateObject(bm, ObjectContext.RENDERING);
                            }
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons2, Complexity.Normal);
                }

                // Shape
                {
                    GenerateLabels(parent, 32f, "Shape");
                    RenderShape();
                }

                // Store Images
                {
                    var labels = GenerateLabels(parent, 32f, "Image");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Store", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                if (beatmapObject.ShapeType != ShapeType.Image)
                                    continue;

                                if (GameData.Current.assets.sprites.Has(x => x.name == beatmapObject.text))
                                    continue;

                                var regex = new Regex(@"img\((.*?)\)");
                                var match = regex.Match(beatmapObject.text);

                                var path = match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : RTFile.CombinePaths(RTFile.BasePath, beatmapObject.text);

                                ObjectEditor.inst.StoreImage(beatmapObject, path);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.IMAGE);
                            }
                        }),
                        new ButtonFunction("Clear", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the images of all selected objects?", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                if (beatmapObject.ShapeType != ShapeType.Image)
                                    continue;

                                beatmapObject.text = string.Empty;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.IMAGE);
                            }
                        }), buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                }

                // Render Type
                {
                    var labels = GenerateLabels(parent, 32f, "Render Type");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Background", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.renderLayerType = BeatmapObject.RenderLayerType.Background;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("Foreground", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.renderLayerType = BeatmapObject.RenderLayerType.Foreground;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                            }
                        }),
                        new ButtonFunction("UI", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.renderLayerType = BeatmapObject.RenderLayerType.UI;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                            }
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }

                GeneratePad(parent);
                GenerateLabels(parent, 32f, new Label("- Prefab -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

                // Assign Objects to Prefab
                {
                    var labels = GenerateLabels(parent, 32f, "Assign Objects to Prefab");
                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Assign", () =>
                        {
                            RTEditor.inst.selectingMultiple = true;
                            RTEditor.inst.prefabPickerEnabled = true;
                        }),
                        new ButtonFunction("Remove", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                timelineObject.GetData<BeatmapObject>().RemovePrefabReference();
                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }
            
                // New Prefab Instance
                {
                    var labels = GenerateLabels(parent, 32f, "New Prefab Instance");
                    var buttons1 = GenerateButtons(parent, 32f, 8f, ThemeGroup.Add, ThemeGroup.Add_Text,
                        new ButtonFunction("New Instance", () => RTEditor.inst.ShowWarningPopup("This will change the instance ID of all selected beatmap objects, assuming they all have the same ID. Are you sure you want to do this?", () =>
                        {
                            var selected = EditorTimeline.inst.timelineObjects.Where(x => x.Selected).ToList();
                            if (selected.Count < 0)
                                return;

                            var firstSelected = selected.Find(x => !x.isPrefabObject);

                            var first = !firstSelected ? string.Empty : firstSelected.TimelineReference switch
                            {
                                TimelineObject.TimelineReferenceType.BeatmapObject => firstSelected.GetData<BeatmapObject>().prefabInstanceID,
                                TimelineObject.TimelineReferenceType.BackgroundObject => firstSelected.GetData<BackgroundObject>().prefabInstanceID,
                                _ => string.Empty,
                            };

                            // validate that all selected timeline objects are beatmap objects and have the same prefab instance ID.
                            if (selected.Any(x => x.isPrefabObject || x.isBeatmapObject && x.GetData<BeatmapObject>().prefabInstanceID != first || x.isBackgroundObject && x.GetData<BackgroundObject>().prefabInstanceID != first))
                                return;

                            var prefabInstanceID = PAObjectBase.GetStringID();

                            selected.ForLoop(timelineObject =>
                            {
                                if (timelineObject.TryGetPrefabable(out IPrefabable prefabable))
                                    prefabable.PrefabInstanceID = prefabInstanceID;
                            });
                            EditorManager.inst.DisplayNotification("Successfully created a new instance ID.", 2f, EditorManager.NotificationType.Success);
                        })));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                // Collapse
                {
                    var labels = GenerateLabels(parent, 32f, "Collapse Prefab");
                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Collapse", () => RTPrefabEditor.inst.CollapseCurrentPrefab()),
                        new ButtonFunction("Collapse New", () => RTPrefabEditor.inst.CollapseCurrentPrefab(true))
                        );

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                // Move Prefabs
                {
                    GenerateLabels(parent, 32f, "Move Prefabs X", "Move Prefabs Y");

                    var movePrefabsParent = Creator.NewUIObject("move prefabs", parent);
                    movePrefabsParent.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                    var multiSyncGLG = movePrefabsParent.AddComponent<GridLayoutGroup>();
                    multiSyncGLG.spacing = new Vector2(8f, 8f);
                    multiSyncGLG.cellSize = new Vector2(188f, 32f);

                    var inputFieldStorageX = GenerateInputField(movePrefabsParent.transform, "move prefabs", "1", "Enter value...", true);
                    inputFieldStorageX.inputField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    inputFieldStorageX.leftButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageX.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[0] -= num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageX.middleButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageX.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[0] = num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageX.rightButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageX.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[0] += num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    var inputFieldStorageY = GenerateInputField(movePrefabsParent.transform, "move prefabs", "1", "Enter value...", true);
                    inputFieldStorageY.inputField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    inputFieldStorageY.leftButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageY.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[1] -= num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageY.middleButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageY.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[1] = num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageY.rightButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageY.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[1] += num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    TriggerHelper.AddEventTriggers(inputFieldStorageX.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorageX.inputField));
                    TriggerHelper.AddEventTriggers(inputFieldStorageY.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorageY.inputField));
                }
            
                // Scale Prefabs
                {
                    GenerateLabels(parent, 32f, "Scale Prefabs X", "Scale Prefabs Y");

                    var movePrefabsParent = Creator.NewUIObject("scale prefabs", parent);
                    movePrefabsParent.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                    var multiSyncGLG = movePrefabsParent.AddComponent<GridLayoutGroup>();
                    multiSyncGLG.spacing = new Vector2(8f, 8f);
                    multiSyncGLG.cellSize = new Vector2(188f, 32f);

                    var inputFieldStorageX = GenerateInputField(movePrefabsParent.transform, "scale prefabs", "0.1", "Enter value...", true);
                    inputFieldStorageX.inputField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    inputFieldStorageX.leftButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageX.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[1].values[0] -= num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageX.middleButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageX.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[1].values[0] = num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageX.rightButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageX.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[1].values[0] += num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    var inputFieldStorageY = GenerateInputField(movePrefabsParent.transform, "scale prefabs", "0.1", "Enter value...", true);
                    inputFieldStorageY.inputField.transform.AsRT().sizeDelta = new Vector2(100f, 32f);
                    inputFieldStorageY.leftButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageY.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[1].values[1] -= num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageY.middleButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageY.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[1].values[1] = num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorageY.rightButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorageY.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[1].values[1] += num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    TriggerHelper.AddEventTriggers(inputFieldStorageX.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorageX.inputField));
                    TriggerHelper.AddEventTriggers(inputFieldStorageY.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorageY.inputField));
                }
            
                // Rotate Prefabs
                {
                    GenerateLabels(parent, 32f, "Rotate Prefabs");

                    var inputFieldStorage = GenerateInputField(parent, "rotate prefabs", "15", "Enter value...", true);
                    inputFieldStorage.leftButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[2].values[0] -= num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorage.middleButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[2].values[0] = num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    inputFieldStorage.rightButton.onClick.NewListener(() =>
                    {
                        if (!float.TryParse(inputFieldStorage.inputField.text, out float num))
                            return;
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isPrefabObject))
                        {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[2].values[0] += num;
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                        }
                    });
                    TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField));
                }

                // Instance Data
                {
                    var labels = GenerateLabels(parent, 32f, "Instance Data");
                    var buttons1 = GenerateButtons(parent, 32f, 8f, ThemeGroup.Paste, ThemeGroup.Paste_Text,
                        new ButtonFunction("Paste Data", () =>
                        {
                            if (!RTPrefabEditor.inst.copiedInstanceData)
                            {
                                EditorManager.inst.DisplayNotification($"No copied data.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            var timelineObjects = EditorTimeline.inst.SelectedPrefabObjects;
                            foreach (var timelineObject in timelineObjects)
                                RTPrefabEditor.inst.PasteInstanceData(timelineObject.GetData<PrefabObject>());

                            if (!timelineObjects.IsEmpty())
                                EditorManager.inst.DisplayNotification($"Pasted Prefab instance data.", 2f, EditorManager.NotificationType.Success);
                        })
                        );

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                GeneratePad(parent);
                GenerateLabels(parent, 32f, new Label("- Toggles -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

                // Lock
                {
                    GenerateLabels(parent, 32f, "Modify time lock state");

                    GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Locked = true;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Locked = false;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            timelineObject.Locked = !timelineObject.Locked;

                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    }));
                }

                // Collapse
                {
                    GenerateLabels(parent, 32f, "Modify timeline collapse state");

                    GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Collapse = true;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Collapse = false;

                                EditorTimeline.inst.RenderTimelineObject(timelineObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            timelineObject.Collapse = !timelineObject.Collapse;

                            EditorTimeline.inst.RenderTimelineObject(timelineObject);
                        }
                    }));
                }

                // Hidden
                {
                    GenerateLabels(parent, 32f, "Modify hidden state");

                    GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Hidden = true;
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                                            break;
                                        }
                                }
                            }
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                timelineObject.Hidden = false;
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                                            break;
                                        }
                                }
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            timelineObject.Hidden = !timelineObject.Hidden;
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.HIDE);

                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.HIDE);

                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.HIDE);

                                        break;
                                    }
                            }
                        }
                    }));
                }
            
                // Selectable
                {
                    GenerateLabels(parent, 32f, "Modify selectable in preview state");

                    GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                if (timelineObject.isBackgroundObject)
                                    continue;

                                timelineObject.SelectableInPreview = true;
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.SELECTABLE);

                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.SELECTABLE);

                                            break;
                                        }
                                }
                            }
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                if (timelineObject.isBackgroundObject)
                                    continue;

                                timelineObject.SelectableInPreview = false;
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.SELECTABLE);

                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.SELECTABLE);

                                            break;
                                        }
                                }
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            if (timelineObject.isBackgroundObject)
                                continue;

                            timelineObject.SelectableInPreview = !timelineObject.SelectableInPreview;
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.SELECTABLE);

                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.SELECTABLE);

                                        break;
                                    }
                            }
                        }
                    }));
                }

                // LDM
                {
                    var labels = GenerateLabels(parent, 32f, "Modify Low Detail Mode");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.LDM = true;
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.LDM = false;
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.LDM = !beatmapObject.LDM;
                            RTLevel.Current?.UpdateObject(beatmapObject);
                        }
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
                }

                // Ignore Lifespan
                {
                    var labels = GenerateLabels(parent, 32f, "Modify Modifier Ignore Lifespan");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.ignoreLifespan = true;
                                            RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            prefabObject.ignoreLifespan = true;
                                            RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.ignoreLifespan = true;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                            break;
                                        }
                                }
                            }
                            RTLevel.Current?.RecalculateObjectStates();
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.ignoreLifespan = false;
                                            RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            prefabObject.ignoreLifespan = false;
                                            RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.ignoreLifespan = false;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                            break;
                                        }
                                }
                            }
                            RTLevel.Current?.RecalculateObjectStates();
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.ignoreLifespan = !beatmapObject.ignoreLifespan;
                                        RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        var prefabObject = timelineObject.GetData<PrefabObject>();
                                        prefabObject.ignoreLifespan = !prefabObject.ignoreLifespan;
                                        RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        backgroundObject.ignoreLifespan = !backgroundObject.ignoreLifespan;
                                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                        break;
                                    }
                            }
                        }
                        RTLevel.Current?.RecalculateObjectStates();
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
                }
            
                // Order Matters
                {
                    var labels = GenerateLabels(parent, 32f, "Modify Modifier Order Matters");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.orderModifiers = true;
                                            RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            prefabObject.orderModifiers = true;
                                            RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.orderModifiers = true;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                            break;
                                        }
                                }
                            }
                            RTLevel.Current?.RecalculateObjectStates();
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                            beatmapObject.orderModifiers = false;
                                            RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            var prefabObject = timelineObject.GetData<PrefabObject>();
                                            prefabObject.orderModifiers = false;
                                            RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                            backgroundObject.orderModifiers = false;
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                            break;
                                        }
                                }
                            }
                            RTLevel.Current?.RecalculateObjectStates();
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        {
                            switch (timelineObject.TimelineReference)
                            {
                                case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.orderModifiers = !beatmapObject.ignoreLifespan;
                                        RTLevel.Current?.UpdateObject(beatmapObject, recalculate: false);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.PrefabObject: {
                                        var prefabObject = timelineObject.GetData<PrefabObject>();
                                        prefabObject.orderModifiers = !prefabObject.ignoreLifespan;
                                        RTLevel.Current?.UpdatePrefab(prefabObject, recalculate: false);
                                        break;
                                    }
                                case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        backgroundObject.orderModifiers = !backgroundObject.ignoreLifespan;
                                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                        break;
                                    }
                            }
                        }
                        RTLevel.Current?.RecalculateObjectStates();
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
                }
            
                // Opacity Collision
                {
                    var labels = GenerateLabels(parent, 32f, "Modify Opacity Collision");

                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("On", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.opacityCollision = true;
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Add, labelThemeGroup: ThemeGroup.Add_Text),
                        new ButtonFunction("Off", () =>
                        {
                            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                            {
                                beatmapObject.opacityCollision = false;
                                RTLevel.Current?.UpdateObject(beatmapObject);
                            }
                        }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
                    var buttons2 = GenerateButtons(parent, 32f, 0f, new ButtonFunction("Swap", () =>
                    {
                        foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                        {
                            beatmapObject.opacityCollision = !beatmapObject.opacityCollision;
                            RTLevel.Current?.UpdateObject(beatmapObject);
                        }
                    }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons2, Complexity.Advanced);
                }

                GeneratePad(parent);
                GenerateLabels(parent, 32f, new Label("- Pasting -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

                // Paste Modifier
                {
                    var labels = GenerateLabels(parent, 32f, "Paste Modifiers to Selected");
                    var buttons1 = GenerateButtons(parent, 32f, 8f, ThemeGroup.Paste, ThemeGroup.Paste_Text,
                        new ButtonFunction("Paste", () =>
                        {
                            bool pasted = false;
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                switch (timelineObject.TimelineReference)
                                {
                                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                            var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(ModifierReferenceType.BeatmapObject);
                                            if (copiedModifiers == null || copiedModifiers.IsEmpty())
                                                continue;

                                            var beatmapObject = timelineObject.GetData<BeatmapObject>();

                                            beatmapObject.modifiers.AddRange(copiedModifiers.Select(x => x.Copy()));

                                            CoroutineHelper.StartCoroutine(ObjectEditor.inst.Dialog.ModifiersDialog.RenderModifiers(beatmapObject));
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);

                                            pasted = true;
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                            var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(ModifierReferenceType.BackgroundObject);
                                            if (copiedModifiers == null || copiedModifiers.IsEmpty())
                                                continue;

                                            var backgroundObject = timelineObject.GetData<BackgroundObject>();

                                            backgroundObject.modifiers.AddRange(copiedModifiers.Select(x => x.Copy()));

                                            CoroutineHelper.StartCoroutine(RTBackgroundEditor.inst.Dialog.ModifiersDialog.RenderModifiers(backgroundObject));
                                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, BackgroundObjectContext.MODIFIERS);

                                            pasted = true;
                                            break;
                                        }
                                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                                            var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(ModifierReferenceType.PrefabObject);
                                            if (copiedModifiers == null || copiedModifiers.IsEmpty())
                                                continue;

                                            var prefabObject = timelineObject.GetData<PrefabObject>();

                                            prefabObject.modifiers.AddRange(copiedModifiers.Select(x => x.Copy()));

                                            CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.PrefabObjectEditor.ModifiersDialog.RenderModifiers(prefabObject));
                                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.MODIFIERS);

                                            pasted = true;
                                            break;
                                        }
                                }
                            }

                            if (pasted)
                                EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                            else
                                EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                        }));

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }

                // Paste Keyframes
                {
                    var labels = GenerateLabels(parent, 32f, "Paste Keyframes to Selected");
                    var buttons1 = GenerateButtons(parent, 32f, 8f, ThemeGroup.Paste, ThemeGroup.Paste_Text,
                        new ButtonFunction("Paste", EditorHelper.PasteKeyframes));

                    EditorHelper.SetComplexity(labels, Complexity.Normal);
                    EditorHelper.SetComplexity(buttons1, Complexity.Normal);
                }

                // Repeat Paste Keyframes
                {
                    var labels = GenerateLabels(parent, 32f, "Repeat Paste Keyframes to Selected");

                    var repeatCountInputField = GenerateInputField(parent, "repeat count", "1", "Enter count...", false, false);
                    TriggerHelper.IncreaseDecreaseButtonsInt(repeatCountInputField);
                    TriggerHelper.AddEventTriggers(repeatCountInputField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(repeatCountInputField.inputField));
                    var repeatOffsetTimeInputField = GenerateInputField(parent, "repeat offset time", "1", "Enter offset time...", false, false);
                    TriggerHelper.IncreaseDecreaseButtons(repeatOffsetTimeInputField);
                    TriggerHelper.AddEventTriggers(repeatOffsetTimeInputField.inputField.gameObject, TriggerHelper.ScrollDelta(repeatOffsetTimeInputField.inputField));

                    var buttons1 = GenerateButtons(parent, 32f, 8f, ThemeGroup.Paste, ThemeGroup.Paste_Text,
                        new ButtonFunction("Paste", () => EditorHelper.RepeatPasteKeyframes(Parser.TryParse(repeatCountInputField.inputField.text, 0), Parser.TryParse(repeatOffsetTimeInputField.inputField.text, 1f))));

                    EditorHelper.SetComplexity(repeatCountInputField.gameObject, Complexity.Advanced);
                    EditorHelper.SetComplexity(repeatOffsetTimeInputField.gameObject, Complexity.Advanced);

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                }

                GeneratePad(parent);

                // Sync object selection
                {
                    var labels = GenerateLabels(parent, 32f, "Sync to specific object");

                    var syncLayout = Creator.NewUIObject("sync layout", parent);
                    syncLayout.transform.AsRT().sizeDelta = new Vector2(390f, 210f);
                    var multiSyncGLG = syncLayout.AddComponent<GridLayoutGroup>();
                    multiSyncGLG.spacing = new Vector2(4f, 4f);
                    multiSyncGLG.cellSize = new Vector2(61.6f, 49f);

                    GenerateButton(syncLayout.transform, new ButtonFunction("ST", eventData =>
                    {
                        SyncObjectData("Start Time", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().StartTime = beatmapObject.StartTime;
                        }, true, true, "StartTime");
                    })); // Start Time
                    GenerateButton(syncLayout.transform, new ButtonFunction("N", eventData =>
                    {
                        SyncObjectData("Name", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().name = beatmapObject.name;
                        }, true, false);
                    })); // Name
                    GenerateButton(syncLayout.transform, new ButtonFunction("OT", eventData =>
                    {
                        SyncObjectData("Object Type", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().objectType = beatmapObject.objectType;
                        }, true, true, "ObjectType");
                    })); // Object Type
                    GenerateButton(syncLayout.transform, new ButtonFunction("AKT", eventData =>
                    {
                        SyncObjectData("AutoKill Type", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().autoKillType = beatmapObject.autoKillType;
                        }, true, true, "AutoKill");
                    })); // Autokill Type
                    GenerateButton(syncLayout.transform, new ButtonFunction("AKO", eventData =>
                    {
                        SyncObjectData("AutoKill Offset", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().autoKillOffset = beatmapObject.autoKillOffset;
                        }, true, true, "AutoKill");
                    })); // Autokill Offset
                    GenerateButton(syncLayout.transform, new ButtonFunction("P", eventData =>
                    {
                        SyncObjectData("Parent", eventData, (TimelineObject currentSelection, BeatmapObject beatmapObjectToParentTo) =>
                        {
                            currentSelection.GetData<BeatmapObject>().SetParent(beatmapObjectToParentTo, renderParent: false);
                        }, false, true, "Parent");
                    })); // Parent
                    GenerateButton(syncLayout.transform, new ButtonFunction("PD", eventData =>
                    {
                        SyncObjectData("Parent Desync", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().desync = beatmapObject.desync;
                        }, false, true, "Parent");
                    })); // Parent Desync
                    GenerateButton(syncLayout.transform, new ButtonFunction("PT", eventData =>
                    {
                        SyncObjectData("Parent Types", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().parentType = beatmapObject.parentType;
                        }, false, true, "ParentType");
                    })); // Parent Type
                    GenerateButton(syncLayout.transform, new ButtonFunction("PO", eventData =>
                    {
                        SyncObjectData("Parent Offsets", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().parentOffsets = beatmapObject.parentOffsets.Copy();
                        }, false, true, "ParentOffset");
                    })); // Parent Offset
                    GenerateButton(syncLayout.transform, new ButtonFunction("PA", eventData =>
                    {
                        SyncObjectData("Parent Additive", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().parentAdditive = beatmapObject.parentAdditive;
                        }, false, true, "ParentOffset");
                    })); // Parent Additive
                    GenerateButton(syncLayout.transform, new ButtonFunction("PP", eventData =>
                    {
                        SyncObjectData("Parent Parallax", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().parallaxSettings = beatmapObject.parallaxSettings.Copy();
                        }, false, true, "ParentOffset");
                    })); // Parent Parallax
                    GenerateButton(syncLayout.transform, new ButtonFunction("O", eventData =>
                    {
                        SyncObjectData("Origin", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().origin = beatmapObject.origin;
                        }, false, true, "Origin");
                    })); // Origin
                    GenerateButton(syncLayout.transform, new ButtonFunction("S", eventData =>
                    {
                        SyncObjectData("Shape", eventData, (timelineObject, beatmapObject) =>
                        {
                            var syncTo = timelineObject.GetData<BeatmapObject>();
                            syncTo.Shape = beatmapObject.Shape;
                            syncTo.ShapeOption = beatmapObject.ShapeOption;
                            syncTo.Polygon.CopyData(beatmapObject.Polygon);
                        }, false, true, "Shape");
                    })); // Shape
                    GenerateButton(syncLayout.transform, new ButtonFunction("T", eventData =>
                    {
                        SyncObjectData("Text", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().text = beatmapObject.text;
                        }, false, true, "Text");
                    })); // Text
                    GenerateButton(syncLayout.transform, new ButtonFunction("D", eventData =>
                    {
                        SyncObjectData("Depth", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().Depth = beatmapObject.Depth;
                        }, false, true, "Depth");
                    })); // Depth
                    GenerateButton(syncLayout.transform, new ButtonFunction("KF", eventData =>
                    {
                        SyncObjectData("Keyframes", eventData, (timelineObject, beatmapObject) =>
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            for (int i = 0; i < bm.events.Count; i++)
                            {
                                bm.events[i].Clear();
                                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                                    bm.events[i].Add(beatmapObject.events[i][j].Copy());
                            }

                        }, true, true, "Keyframes");
                    })); // Keyframes
                    GenerateButton(syncLayout.transform, new ButtonFunction("MOD", eventData =>
                    {
                        SyncObjectData("Modifiers", eventData, (timelineObject, beatmapObject) =>
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();

                            bm.modifiers.AddRange(beatmapObject.modifiers.Select(x => x.Copy()));
                        }, false, true);
                    })); // Modifiers
                    GenerateButton(syncLayout.transform, new ButtonFunction("IGN", eventData =>
                    {
                        SyncObjectData("Ignore Lifespan", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().ignoreLifespan = beatmapObject.ignoreLifespan;
                        }, false, false);
                    })); // Ignore lifespan
                    GenerateButton(syncLayout.transform, new ButtonFunction("ORD", eventData =>
                    {
                        SyncObjectData("Order Matters", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().ignoreLifespan = beatmapObject.ignoreLifespan;
                        }, false, true);
                    })); // Modifiers
                    GenerateButton(syncLayout.transform, new ButtonFunction("IGN", eventData =>
                    {
                        SyncObjectData("Ignore Lifespan", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().ignoreLifespan = beatmapObject.ignoreLifespan;
                        }, false, false);
                    })); // Ignore lifespan
                    GenerateButton(syncLayout.transform, new ButtonFunction("TAG", eventData =>
                    {
                        SyncObjectData("Tags", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().tags = beatmapObject.tags.Clone();
                        }, false, false);
                    })); // Tags
                    GenerateButton(syncLayout.transform, new ButtonFunction("RT", eventData =>
                    {
                        SyncObjectData("Render Type", eventData, (timelineObject, beatmapObject) =>
                        {
                            timelineObject.GetData<BeatmapObject>().renderLayerType = beatmapObject.renderLayerType;
                        }, false, true);
                    })); // Render Type
                    GenerateButton(syncLayout.transform, new ButtonFunction("PR", eventData =>
                    {
                        SyncObjectData("Prefab Reference", eventData, (timelineObject, beatmapObject) =>
                        {
                            var bm = timelineObject.GetData<BeatmapObject>();
                            bm.prefabID = beatmapObject.prefabID;
                            bm.prefabInstanceID = beatmapObject.prefabInstanceID;
                        }, true, false);
                    })); // Prefab

                    EditorHelper.SetComplexity(labels, Complexity.Advanced);
                    EditorHelper.SetComplexity(syncLayout, Complexity.Advanced);
                }

                GeneratePad(parent, Complexity.Advanced);

                var replaceLabels = GenerateLabels(parent, 32f, new Label("- Replace strings -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));
                EditorHelper.SetComplexity(replaceLabels, Complexity.Advanced);

                // Replace Name
                SetupReplaceStrings(parent, "Replace Name", "Old Name", "Enter old name...", "New Name", "Enter new name...", (oldNameIF, newNameIF) =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.name = bm.name.Replace(oldNameIF.text, newNameIF.text);
                        EditorTimeline.inst.RenderTimelineObject(timelineObject);
                    }
                });

                // Replace Tags
                SetupReplaceStrings(parent, "Replace Tags", "Old Tag", "Enter old tag...", "New Tag", "Enter new tag...", (oldNameIF, newNameIF) =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        for (int i = 0; i < bm.tags.Count; i++)
                            bm.tags[i] = bm.tags[i].Replace(oldNameIF.text, newNameIF.text);
                    }
                });

                // Replace Text
                SetupReplaceStrings(parent, "Replace Text", "Old Text", "Enter old text...", "New Text", "Enter new text...", (oldNameIF, newNameIF) =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();
                        bm.text = bm.text.Replace(oldNameIF.text, newNameIF.text);
                        RTLevel.Current?.UpdateObject(bm, ObjectContext.SHAPE);
                    }
                });

                // Replace Modifier
                SetupReplaceStrings(parent, "Replace Modifier values", "Old Value", "Enter old value...", "New Value", "Enter new value...", (oldNameIF, newNameIF) =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();

                        foreach (var modifier in bm.modifiers)
                        {
                            for (int i = 0; i < modifier.values.Count; i++)
                                modifier.values[i] = modifier.values[i].Replace(oldNameIF.text, newNameIF.text);
                        }
                    }
                });

                GeneratePad(parent);

                // Assign Colors
                {
                    var labels1 = GenerateLabels(parent, 32f, new Label("- Assign colors -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));

                    var labelsColor = GenerateLabels(parent, 32f, "Primary Color");

                    var disable = EditorPrefabHolder.Instance.Function2Button.Duplicate(parent, "disable color");
                    var disableX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(disable.transform, "x");
                    var disableXImage = disableX.GetComponent<Image>();
                    disableXImage.sprite = EditorSprites.CloseSprite;
                    RectValues.Default.AnchoredPosition(-170f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(disableXImage.rectTransform);
                    var disableButtonStorage = disable.GetComponent<FunctionButtonStorage>();
                    disableButtonStorage.OnClick.NewListener(() =>
                    {
                        disableX.gameObject.SetActive(true);
                        currentMultiColorSelection = -1;
                        UpdateMultiColorButtons();
                    });
                    disableButtonStorage.Text = "Don't set color";
                    EditorThemeManager.ApplyGraphic(disableXImage, ThemeGroup.Function_2_Text);
                    EditorThemeManager.ApplyGraphic(disableButtonStorage.label, ThemeGroup.Function_2_Text);
                    EditorThemeManager.ApplySelectable(disableButtonStorage.button, ThemeGroup.Function_2);

                    var colorLayout = Creator.NewUIObject("color layout", parent);
                    colorLayout.transform.AsRT().sizeDelta = new Vector2(390f, 76f);
                    var colorLayoutGLG = colorLayout.AddComponent<GridLayoutGroup>();
                    colorLayoutGLG.spacing = new Vector2(4f, 4f);
                    colorLayoutGLG.cellSize = new Vector2(36f, 36f);

                    for (int i = 0; i < 18; i++)
                    {
                        var index = i;
                        var colorGUI = EditorManager.inst.colorGUI.Duplicate(colorLayout.transform, (i + 1).ToString());
                        var assigner = colorGUI.AddComponent<AssignToTheme>();
                        assigner.Index = i;
                        var image = colorGUI.GetComponent<Image>();
                        assigner.Graphic = image;

                        var selected = colorGUI.transform.GetChild(0).gameObject;
                        selected.SetActive(false);

                        var button = colorGUI.GetComponent<Button>();
                        button.onClick.NewListener(() =>
                        {
                            disableX.gameObject.SetActive(false);
                            currentMultiColorSelection = index;
                            UpdateMultiColorButtons();
                        });

                        multiColorButtons.Add(new MultiColorButton
                        {
                            Button = button,
                            Image = image,
                            Selected = selected
                        });
                    }

                    var labels2 = GenerateLabels(parent, 32f, "Primary Opacity");

                    var opacityIF = CreateInputField("opacity", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)opacityIF.placeholder).fontSize = 13;

                    var labels3 = GenerateLabels(parent, 32f, "Primary Hue");

                    var hueIF = CreateInputField("hue", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)hueIF.placeholder).fontSize = 13;

                    var labels4 = GenerateLabels(parent, 32f, "Primary Saturation");

                    var satIF = CreateInputField("sat", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)satIF.placeholder).fontSize = 13;

                    var labels5 = GenerateLabels(parent, 32f, "Primary Value (Brightness)");

                    var valIF = CreateInputField("val", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)valIF.placeholder).fontSize = 13;

                    var labelsSecondaryColor = GenerateLabels(parent, 32f, "Secondary Color");

                    var disableGradient = EditorPrefabHolder.Instance.Function2Button.Duplicate(parent, "disable color");
                    var disableGradientX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(disableGradient.transform, "x");
                    var disableGradientXImage = disableGradientX.GetComponent<Image>();
                    disableGradientXImage.sprite = EditorSprites.CloseSprite;
                    RectValues.Default.AnchoredPosition(-170f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(disableGradientXImage.rectTransform);
                    var disableGradientButtonStorage = disableGradient.GetComponent<FunctionButtonStorage>();
                    disableGradientButtonStorage.OnClick.NewListener(() =>
                    {
                        disableGradientX.gameObject.SetActive(true);
                        currentMultiGradientColorSelection = -1;
                        UpdateMultiColorButtons();
                    });
                    disableGradientButtonStorage.Text = "Don't set color";
                    EditorThemeManager.ApplyGraphic(disableGradientXImage, ThemeGroup.Function_2_Text);
                    EditorThemeManager.ApplyGraphic(disableGradientButtonStorage.label, ThemeGroup.Function_2_Text);
                    EditorThemeManager.ApplySelectable(disableGradientButtonStorage.button, ThemeGroup.Function_2);

                    var colorGradientLayout = Creator.NewUIObject("color layout", parent);
                    colorGradientLayout.transform.AsRT().sizeDelta = new Vector2(390f, 76f);
                    var colorGradientLayoutGLG = colorGradientLayout.AddComponent<GridLayoutGroup>();
                    colorGradientLayoutGLG.spacing = new Vector2(4f, 4f);
                    colorGradientLayoutGLG.cellSize = new Vector2(36f, 36f);

                    for (int i = 0; i < 18; i++)
                    {
                        var index = i;
                        var colorGUI = EditorManager.inst.colorGUI.Duplicate(colorGradientLayout.transform, (i + 1).ToString());
                        var assigner = colorGUI.AddComponent<AssignToTheme>();
                        assigner.Index = i;
                        var image = colorGUI.GetComponent<Image>();
                        assigner.Graphic = image;

                        var selected = colorGUI.transform.GetChild(0).gameObject;
                        selected.SetActive(false);

                        var button = colorGUI.GetComponent<Button>();
                        button.onClick.NewListener(() =>
                        {
                            disableGradientX.gameObject.SetActive(false);
                            currentMultiGradientColorSelection = index;
                            UpdateMultiColorButtons();
                        });

                        multiGradientColorButtons.Add(new MultiColorButton
                        {
                            Button = button,
                            Image = image,
                            Selected = selected
                        });
                    }

                    var labels6 = GenerateLabels(parent, 32f, "Secondary Opacity");

                    var opacityGradientIF = CreateInputField("opacity", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)opacityGradientIF.placeholder).fontSize = 13;

                    var labels7 = GenerateLabels(parent, 32f, "Secondary Hue");

                    var hueGradientIF = CreateInputField("hue", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)hueGradientIF.placeholder).fontSize = 13;

                    var labels8 = GenerateLabels(parent, 32f, "Secondary Saturation");

                    var satGradientIF = CreateInputField("sat", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)satGradientIF.placeholder).fontSize = 13;

                    var labels9 = GenerateLabels(parent, 32f, "Secondary Value (Brightness)");

                    var valGradientIF = CreateInputField("val", string.Empty, "Enter value... (Keep empty to not set)", parent, isInteger: false);
                    ((Text)valGradientIF.placeholder).fontSize = 13;

                    var labels10 = GenerateLabels(parent, 32f, "Ease Type");

                    var curvesObject = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(parent, "curves");
                    var curves = curvesObject.GetComponent<Dropdown>();
                    RTEditor.inst.SetupEaseDropdown(curves);
                    curves.onValueChanged.ClearAll();
                    curves.options.Insert(0, new Dropdown.OptionData("None (Doesn't Set Easing)"));

                    TriggerHelper.AddEventTriggers(curves.gameObject, TriggerHelper.ScrollDelta(curves));

                    EditorThemeManager.ApplyDropdown(curves);

                    // Assign to All
                    {
                        var labels = GenerateLabels(parent, 32f, "Assign to all Color Keyframes");
                        var buttons1 = GenerateButtons(parent, 32f, 8f,
                            new ButtonFunction("Set", () =>
                            {
                                var anim = RTEditor.inst.GetEasing(curves.value - 1);
                                bool setCurve = curves.value != 0;
                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    for (int i = 0; i < bm.events[3].Count; i++)
                                    {
                                        var kf = bm.events[3][i];
                                        if (setCurve)
                                            kf.curve = anim;
                                        if (currentMultiColorSelection >= 0)
                                            kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                        if (!string.IsNullOrEmpty(opacityIF.text))
                                            kf.values[1] = -Mathf.Clamp(Parser.TryParse(opacityIF.text, 1f), 0f, 1f) + 1f;
                                        if (!string.IsNullOrEmpty(hueIF.text))
                                            kf.values[2] = Parser.TryParse(hueIF.text, 0f);
                                        if (!string.IsNullOrEmpty(satIF.text))
                                            kf.values[3] = Parser.TryParse(satIF.text, 0f);
                                        if (!string.IsNullOrEmpty(valIF.text))
                                            kf.values[4] = Parser.TryParse(valIF.text, 0f);

                                        // Gradient
                                        if (currentMultiGradientColorSelection >= 0)
                                            kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
                                        if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                            kf.values[6] = -Mathf.Clamp(Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f) + 1f;
                                        if (!string.IsNullOrEmpty(hueGradientIF.text))
                                            kf.values[7] = Parser.TryParse(hueGradientIF.text, 0f);
                                        if (!string.IsNullOrEmpty(satGradientIF.text))
                                            kf.values[8] = Parser.TryParse(satGradientIF.text, 0f);
                                        if (!string.IsNullOrEmpty(valGradientIF.text))
                                            kf.values[9] = Parser.TryParse(valGradientIF.text, 0f);
                                    }

                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                }
                            }),
                            new ButtonFunction("Add", () =>
                            {
                                var anim = RTEditor.inst.GetEasing(curves.value - 1);
                                bool setCurve = curves.value != 0;
                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    for (int i = 0; i < bm.events[3].Count; i++)
                                    {
                                        var kf = bm.events[3][i];
                                        if (setCurve)
                                            kf.curve = anim;
                                        if (currentMultiColorSelection >= 0)
                                            kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18); // color slots can't be added onto.
                                        if (!string.IsNullOrEmpty(opacityIF.text))
                                            kf.values[1] = Mathf.Clamp(kf.values[1] - Parser.TryParse(opacityIF.text, 1f), 0f, 1f);
                                        if (!string.IsNullOrEmpty(hueIF.text))
                                            kf.values[2] += Parser.TryParse(hueIF.text, 0f);
                                        if (!string.IsNullOrEmpty(satIF.text))
                                            kf.values[3] += Parser.TryParse(satIF.text, 0f);
                                        if (!string.IsNullOrEmpty(valIF.text))
                                            kf.values[4] += Parser.TryParse(valIF.text, 0f);

                                        // Gradient
                                        if (currentMultiGradientColorSelection >= 0)
                                            kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
                                        if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                            kf.values[6] = Mathf.Clamp(kf.values[6] - Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f);
                                        if (!string.IsNullOrEmpty(hueGradientIF.text))
                                            kf.values[7] += Parser.TryParse(hueGradientIF.text, 0f);
                                        if (!string.IsNullOrEmpty(satGradientIF.text))
                                            kf.values[8] += Parser.TryParse(satGradientIF.text, 0f);
                                        if (!string.IsNullOrEmpty(valGradientIF.text))
                                            kf.values[9] += Parser.TryParse(valGradientIF.text, 0f);
                                    }

                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                }
                            }),
                            new ButtonFunction("Sub", () =>
                            {
                                var anim = RTEditor.inst.GetEasing(curves.value - 1);
                                bool setCurve = curves.value != 0;
                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    for (int i = 0; i < bm.events[3].Count; i++)
                                    {
                                        var kf = bm.events[3][i];
                                        if (setCurve)
                                            kf.curve = anim;
                                        if (currentMultiColorSelection >= 0)
                                            kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18); // color slots can't be added onto.
                                        if (!string.IsNullOrEmpty(opacityIF.text))
                                            kf.values[1] = Mathf.Clamp(kf.values[1] + Parser.TryParse(opacityIF.text, 1f), 0f, 1f);
                                        if (!string.IsNullOrEmpty(hueIF.text))
                                            kf.values[2] -= Parser.TryParse(hueIF.text, 0f);
                                        if (!string.IsNullOrEmpty(satIF.text))
                                            kf.values[3] -= Parser.TryParse(satIF.text, 0f);
                                        if (!string.IsNullOrEmpty(valIF.text))
                                            kf.values[4] -= Parser.TryParse(valIF.text, 0f);

                                        // Gradient
                                        if (currentMultiGradientColorSelection >= 0)
                                            kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
                                        if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                            kf.values[6] = Mathf.Clamp(kf.values[6] + Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f);
                                        if (!string.IsNullOrEmpty(hueGradientIF.text))
                                            kf.values[7] -= Parser.TryParse(hueGradientIF.text, 0f);
                                        if (!string.IsNullOrEmpty(satGradientIF.text))
                                            kf.values[8] -= Parser.TryParse(satGradientIF.text, 0f);
                                        if (!string.IsNullOrEmpty(valGradientIF.text))
                                            kf.values[9] -= Parser.TryParse(valGradientIF.text, 0f);
                                    }

                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                }
                            }));

                        EditorHelper.SetComplexity(labels, Complexity.Advanced);
                        EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    }

                    // Assign to Index
                    {
                        var labels = GenerateLabels(parent, 32f, "Assign to Index");

                        var assignIndex = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);
                        var buttons1 = GenerateButtons(parent, 32f, 8f,
                            new ButtonFunction("Set", () =>
                            {
                                if (assignIndex.text.Contains(","))
                                {
                                    var split = assignIndex.text.Split(',');

                                    for (int i = 0; i < split.Length; i++)
                                    {
                                        var text = split[i];
                                        if (!int.TryParse(text, out int a))
                                            continue;

                                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                        {
                                            var bm = timelineObject.GetData<BeatmapObject>();

                                            SetKeyframeValues(bm.events[3][Mathf.Clamp(a, 0, bm.events[3].Count - 1)], curves,
                                                opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                            RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                        }
                                    }

                                    return;
                                }

                                if (!int.TryParse(assignIndex.text, out int num))
                                    return;
                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    SetKeyframeValues(bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)], curves,
                                        opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                }
                            }),
                            new ButtonFunction("Add", () =>
                            {
                                if (assignIndex.text.Contains(","))
                                {
                                    var split = assignIndex.text.Split(',');

                                    for (int i = 0; i < split.Length; i++)
                                    {
                                        var text = split[i];
                                        if (!int.TryParse(text, out int a))
                                            return;

                                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                        {
                                            var bm = timelineObject.GetData<BeatmapObject>();

                                            AddKeyframeValues(bm.events[3][Mathf.Clamp(a, 0, bm.events[3].Count - 1)], curves,
                                                opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                            RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                        }
                                    }

                                    return;
                                }

                                if (!int.TryParse(assignIndex.text, out int num))
                                    return;
                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    AddKeyframeValues(bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)], curves,
                                        opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                }
                            }),
                            new ButtonFunction("Sub", () =>
                            {
                                if (assignIndex.text.Contains(","))
                                {
                                    var split = assignIndex.text.Split(',');

                                    for (int i = 0; i < split.Length; i++)
                                    {
                                        var text = split[i];
                                        if (!int.TryParse(text, out int a))
                                            return;

                                        foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                        {
                                            var bm = timelineObject.GetData<BeatmapObject>();

                                            SubKeyframeValues(bm.events[3][Mathf.Clamp(a, 0, bm.events[3].Count - 1)], curves,
                                                opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                            RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                        }
                                    }

                                    return;
                                }

                                if (!int.TryParse(assignIndex.text, out int num))
                                    return;
                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    SubKeyframeValues(bm.events[3][Mathf.Clamp(num, 0, bm.events[3].Count - 1)], curves,
                                        opacityIF.text, hueIF.text, satIF.text, valIF.text, opacityGradientIF.text, hueGradientIF.text, satGradientIF.text, valGradientIF.text);

                                    RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                }
                            }));

                        EditorHelper.SetComplexity(labels, Complexity.Advanced);
                        try
                        {
                            EditorHelper.SetComplexity(assignIndex.transform.parent.gameObject, Complexity.Normal);
                        }
                        catch (Exception ex)
                        {
                            CoreHelper.LogException(ex);
                        }
                        EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    }

                    // Create Color Keyframe
                    {
                        var labels = GenerateLabels(parent, 32f, "Create Color Keyframe");
                        var buttons1 = GenerateButtons(parent, 32f, 0f, ThemeGroup.Add, ThemeGroup.Add_Text, new ButtonFunction("Create", () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                var currentTime = AudioManager.inst.CurrentAudioSource.time;

                                if (currentTime < bm.StartTime) // don't want people creating keyframes before the objects' start time.
                                    continue;

                                var index = bm.events[3].FindLastIndex(x => currentTime > bm.StartTime + x.time);

                                if (index >= 0 && currentTime > bm.StartTime)
                                {
                                    var kf = bm.events[3][index].Copy();
                                    kf.time = currentTime - bm.StartTime;
                                    if (curves.value != 0)
                                        kf.curve = RTEditor.inst.GetEasing(curves.value - 1);

                                    if (currentMultiColorSelection >= 0)
                                        kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                    if (!string.IsNullOrEmpty(opacityIF.text))
                                        kf.values[1] = -Mathf.Clamp(Parser.TryParse(opacityIF.text, 1f), 0f, 1f) + 1f;
                                    if (!string.IsNullOrEmpty(hueIF.text))
                                        kf.values[2] = Parser.TryParse(hueIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satIF.text))
                                        kf.values[3] = Parser.TryParse(satIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valIF.text))
                                        kf.values[4] = Parser.TryParse(valIF.text, 0f);

                                    // Gradient
                                    if (currentMultiGradientColorSelection >= 0)
                                        kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
                                    if (!string.IsNullOrEmpty(opacityGradientIF.text))
                                        kf.values[6] = -Mathf.Clamp(Parser.TryParse(opacityGradientIF.text, 1f), 0f, 1f) + 1f;
                                    if (!string.IsNullOrEmpty(hueGradientIF.text))
                                        kf.values[7] = Parser.TryParse(hueGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(satGradientIF.text))
                                        kf.values[8] = Parser.TryParse(satGradientIF.text, 0f);
                                    if (!string.IsNullOrEmpty(valGradientIF.text))
                                        kf.values[9] = Parser.TryParse(valGradientIF.text, 0f);

                                    bm.events[3].Add(kf);
                                }

                                RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(bm));
                            }
                        }));

                        EditorHelper.SetComplexity(labels, Complexity.Advanced);
                        EditorHelper.SetComplexity(buttons1, Complexity.Advanced);
                    }

                    EditorHelper.SetComplexity(labelsColor, Complexity.Advanced);
                    EditorHelper.SetComplexity(labelsSecondaryColor, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels1, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels2, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels3, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels4, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels5, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels6, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels7, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels8, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels9, Complexity.Advanced);
                    EditorHelper.SetComplexity(labels10, Complexity.Advanced);

                    EditorHelper.SetComplexity(disable, Complexity.Advanced);
                    EditorHelper.SetComplexity(colorLayout, Complexity.Advanced);
                    EditorHelper.SetComplexity(disableGradient, Complexity.Advanced);
                    EditorHelper.SetComplexity(colorGradientLayout, Complexity.Advanced);
                    EditorHelper.SetComplexity(curvesObject, Complexity.Advanced);
                    try
                    {
                        EditorHelper.SetComplexity(opacityIF.transform.parent.gameObject, Complexity.Advanced);
                        EditorHelper.SetComplexity(hueIF.transform.parent.gameObject, Complexity.Advanced);
                        EditorHelper.SetComplexity(satIF.transform.parent.gameObject, Complexity.Advanced);
                        EditorHelper.SetComplexity(valIF.transform.parent.gameObject, Complexity.Advanced);
                        EditorHelper.SetComplexity(opacityGradientIF.transform.parent.gameObject, Complexity.Advanced);
                        EditorHelper.SetComplexity(hueGradientIF.transform.parent.gameObject, Complexity.Advanced);
                        EditorHelper.SetComplexity(satGradientIF.transform.parent.gameObject, Complexity.Advanced);
                        EditorHelper.SetComplexity(valGradientIF.transform.parent.gameObject, Complexity.Advanced);
                    }
                    catch (Exception ex)
                    {
                        CoreHelper.LogException(ex);
                    }
                }

                GeneratePad(parent, Complexity.Normal);
                var pastingDataLabels = GenerateLabels(parent, 32f, new Label("- Pasting Data -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));
                EditorHelper.SetComplexity(pastingDataLabels, Complexity.Normal);

                // Paste Data
                {
                    var allTypesLabel = GenerateLabels(parent, 32f, "Paste Keyframe data (All types)");

                    // All Types
                    {
                        GeneratePasteKeyframeData(parent, () =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                            {
                                var bm = timelineObject.GetData<BeatmapObject>();

                                for (int i = 0; i < bm.events.Count; i++)
                                {
                                    var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(i);
                                    if (copiedKeyframeData == null)
                                        continue;

                                    for (int j = 0; j < bm.events[i].Count; j++)
                                    {
                                        var kf = bm.events[i][j];
                                        kf.curve = copiedKeyframeData.curve;
                                        kf.values = copiedKeyframeData.values.Copy();
                                        kf.randomValues = copiedKeyframeData.randomValues.Copy();
                                        kf.random = copiedKeyframeData.random;
                                        kf.relative = copiedKeyframeData.relative;

                                        RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                    }
                                }
                            }
                            EditorManager.inst.DisplayNotification("Pasted keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                        }, _val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                                {
                                    var bm = timelineObject.GetData<BeatmapObject>();

                                    for (int i = 0; i < bm.events.Count; i++)
                                    {
                                        var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(i);
                                        if (copiedKeyframeData == null)
                                            continue;

                                        var kf = bm.events[i][Mathf.Clamp(num, 0, bm.events[i].Count - 1)];
                                        kf.curve = copiedKeyframeData.curve;
                                        kf.values = copiedKeyframeData.values.Copy();
                                        kf.randomValues = copiedKeyframeData.randomValues.Copy();
                                        kf.random = copiedKeyframeData.random;
                                        kf.relative = copiedKeyframeData.relative;

                                        RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                    }
                                }
                                EditorManager.inst.DisplayNotification("Pasted keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                            }
                        });
                    }

                    EditorHelper.SetComplexity(allTypesLabel, Complexity.Advanced);

                    for (int i = 0; i < 4; i++)
                    {
                        string name = i switch
                        {
                            0 => "Position",
                            1 => "Scale",
                            2 => "Rotation",
                            3 => "Color",
                            _ => "Null",
                        };
                        var typeLabel = GenerateLabels(parent, 32f, $"Paste Keyframe data ({name})");
                        GeneratePasteKeyframeData(parent, i, name);
                        EditorHelper.SetComplexity(typeLabel, Complexity.Advanced);
                    }
                }

                multiObjectEditorDialog.Find("data").AsRT().sizeDelta = new Vector2(810f, 730.11f);
                multiObjectEditorDialog.Find("data/left").AsRT().sizeDelta = new Vector2(355f, 730f);

                #endregion
            }
        }

        public void InitShape(Transform parent)
        {
            if (!multiShapes)
            {
                var shapes = ObjEditor.inst.ObjectView.transform.Find("shape").gameObject.Duplicate(multiObjectContent, "shape");
                var shapeOption = ObjEditor.inst.ObjectView.transform.Find("shapesettings").gameObject.Duplicate(multiObjectContent, "shapesettings");
                multiShapes = shapes.transform;
                multiShapeSettings = shapeOption.transform;

                multiShapes.AsRT().sizeDelta = new Vector2(388.4f, 32f);
                multiShapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
            }

            var shape = multiShapes;
            var shapeSettings = multiShapeSettings;

            var shapeGLG = shape.GetComponent<GridLayoutGroup>();
            shapeGLG.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            shapeGLG.constraintCount = 1;
            shapeGLG.spacing = new Vector2(7.6f, 0f);

            if (!updatedShapes)
            {
                // Initial removing
                CoreHelper.Destroy(shape.GetComponent<ToggleGroup>(), true);

                CoreHelper.DestroyChildren(shape.transform);

                for (int i = 0; i < shapeSettings.childCount; i++)
                    if (i != 4 && i != 6)
                        CoreHelper.DestroyChildren(shapeSettings.GetChild(i));

                for (int i = 0; i < ShapeManager.inst.Shapes2D.Count; i++)
                {
                    var shapeType = (ShapeType)i;

                    var obj = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shape, (i + 1).ToString(), i);
                    if (obj.transform.Find("Image") && obj.transform.Find("Image").gameObject.TryGetComponent(out Image image))
                    {
                        image.sprite = ShapeManager.inst.Shapes2D[i].icon;
                        EditorThemeManager.ApplyGraphic(image, ThemeGroup.Toggle_1_Check);
                    }

                    if (!obj.GetComponent<HoverUI>())
                    {
                        var hoverUI = obj.AddComponent<HoverUI>();
                        hoverUI.animatePos = false;
                        hoverUI.animateSca = true;
                        hoverUI.size = 1.1f;
                    }

                    var shapeToggle = obj.GetComponent<Toggle>();
                    EditorThemeManager.ApplyToggle(shapeToggle, ThemeGroup.Background_1);
                    shapeToggle.group = null;

                    shapeToggles.Add(shapeToggle);

                    shapeOptionToggles.Add(new List<Toggle>());

                    if (shapeType != ShapeType.Text && shapeType != ShapeType.Image && shapeType != ShapeType.Polygon)
                    {
                        if (!shapeSettings.Find((i + 1).ToString()))
                        {
                            var sh = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString());
                            CoreHelper.DestroyChildren(sh.transform);
                        }

                        var so = shapeSettings.Find((i + 1).ToString());

                        var rect = (RectTransform)so;
                        if (!so.GetComponent<ScrollRect>())
                        {
                            var scroll = so.gameObject.AddComponent<ScrollRect>();
                            so.gameObject.AddComponent<Mask>();
                            var ad = so.gameObject.AddComponent<Image>();

                            scroll.horizontal = true;
                            scroll.vertical = false;
                            scroll.content = rect;
                            scroll.viewport = rect;
                            ad.color = new Color(1f, 1f, 1f, 0.01f);
                        }

                        for (int j = 0; j < ShapeManager.inst.Shapes2D[i].Count; j++)
                        {
                            var opt = ObjectEditor.inst.shapeButtonPrefab.Duplicate(shapeSettings.GetChild(i), (j + 1).ToString(), j);
                            if (opt.transform.Find("Image") && opt.transform.Find("Image").gameObject.TryGetComponent(out Image image1))
                            {
                                image1.sprite = ShapeManager.inst.Shapes2D[i][j].icon;
                                EditorThemeManager.ApplyGraphic(image1, ThemeGroup.Toggle_1_Check);
                            }

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var hoverUI = opt.AddComponent<HoverUI>();
                                hoverUI.animatePos = false;
                                hoverUI.animateSca = true;
                                hoverUI.size = 1.1f;
                            }

                            var shapeOptionToggle = opt.GetComponent<Toggle>();
                            EditorThemeManager.ApplyToggle(shapeOptionToggle, ThemeGroup.Background_1);
                            shapeOptionToggle.group = null;

                            shapeOptionToggles[i].Add(shapeOptionToggle);

                            var layoutElement = opt.AddComponent<LayoutElement>();
                            layoutElement.layoutPriority = 1;
                            layoutElement.minWidth = 32f;

                            opt.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                            if (!opt.GetComponent<HoverUI>())
                            {
                                var he = opt.AddComponent<HoverUI>();
                                he.animatePos = false;
                                he.animateSca = true;
                                he.size = 1.1f;
                            }
                        }

                        ObjectEditor.inst.LastGameObject(shapeSettings.GetChild(i));
                    }

                    if (shapeType == ShapeType.Polygon)
                    {
                        var so = shapeSettings.Find((i + 1).ToString());

                        if (!so)
                        {
                            so = shapeSettings.Find("6").gameObject.Duplicate(shapeSettings, (i + 1).ToString()).transform;
                            CoreHelper.DestroyChildren(so);
                        }

                        CoreHelper.Destroy(so.GetComponent<ScrollRect>(), true);
                        CoreHelper.Destroy(so.GetComponent<HorizontalLayoutGroup>(), true);
                        CoreHelper.Destroy(so.GetComponent<VerticalLayoutGroup>(), true);

                        so.gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.05f);

                        var verticalLayoutGroup = so.gameObject.AddComponent<VerticalLayoutGroup>();
                        verticalLayoutGroup.spacing = 4f;

                        // Polygon Settings
                        {
                            #region Radius

                            var radius = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "radius");
                            var radiusStorage = radius.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(radiusStorage.addButton);
                            CoreHelper.Delete(radiusStorage.subButton);
                            CoreHelper.Delete(radiusStorage.leftGreaterButton);
                            CoreHelper.Delete(radiusStorage.middleButton);
                            CoreHelper.Delete(radiusStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(radiusStorage);

                            var radiusLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(radius.transform, "label", 0);
                            var radiusLabelText = radiusLabel.GetComponent<Text>();
                            radiusLabelText.alignment = TextAnchor.MiddleLeft;
                            radiusLabelText.text = "Radius";
                            radiusLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.ApplyLightText(radiusLabelText);
                            var radiusLabelLayout = radiusLabel.AddComponent<LayoutElement>();
                            radiusLabelLayout.minWidth = 100f;

                            #endregion

                            #region Sides

                            var sides = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "sides");
                            var sidesStorage = sides.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(sidesStorage.addButton);
                            CoreHelper.Delete(sidesStorage.subButton);
                            CoreHelper.Delete(sidesStorage.leftGreaterButton);
                            CoreHelper.Delete(sidesStorage.middleButton);
                            CoreHelper.Delete(sidesStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(sidesStorage);

                            var sidesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(sides.transform, "label", 0);
                            var sidesLabelText = sidesLabel.GetComponent<Text>();
                            sidesLabelText.alignment = TextAnchor.MiddleLeft;
                            sidesLabelText.text = "Sides";
                            sidesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.ApplyLightText(sidesLabelText);
                            var sidesLabelLayout = sidesLabel.AddComponent<LayoutElement>();
                            sidesLabelLayout.minWidth = 100f;

                            #endregion

                            #region Roundness

                            var roundness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "roundness");
                            var roundnessStorage = roundness.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(roundnessStorage.addButton);
                            CoreHelper.Delete(roundnessStorage.subButton);
                            CoreHelper.Delete(roundnessStorage.leftGreaterButton);
                            CoreHelper.Delete(roundnessStorage.middleButton);
                            CoreHelper.Delete(roundnessStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(roundnessStorage);

                            var roundnessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(roundness.transform, "label", 0);
                            var roundnessLabelText = roundnessLabel.GetComponent<Text>();
                            roundnessLabelText.alignment = TextAnchor.MiddleLeft;
                            roundnessLabelText.text = "Roundness";
                            roundnessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.ApplyLightText(roundnessLabelText);
                            var roundnessLabelLayout = roundnessLabel.AddComponent<LayoutElement>();
                            roundnessLabelLayout.minWidth = 100f;

                            #endregion

                            #region Thickness

                            var thickness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "thickness");
                            var thicknessStorage = thickness.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(thicknessStorage.addButton);
                            CoreHelper.Delete(thicknessStorage.subButton);
                            CoreHelper.Delete(thicknessStorage.leftGreaterButton);
                            CoreHelper.Delete(thicknessStorage.middleButton);
                            CoreHelper.Delete(thicknessStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(thicknessStorage);

                            var thicknessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thickness.transform, "label", 0);
                            var thicknessLabelText = thicknessLabel.GetComponent<Text>();
                            thicknessLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessLabelText.text = "Thickness";
                            thicknessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.ApplyLightText(thicknessLabelText);
                            var thicknessLabelLayout = thicknessLabel.AddComponent<LayoutElement>();
                            thicknessLabelLayout.minWidth = 100f;

                            #endregion

                            #region Thickness Offset

                            var thicknessOffset = Creator.NewUIObject("thickness offset", so);
                            var thicknessOffsetLayout = thicknessOffset.AddComponent<HorizontalLayoutGroup>();

                            var thicknessOffsetLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessOffset.transform, "label");
                            var thicknessOffsetLabelText = thicknessOffsetLabel.GetComponent<Text>();
                            thicknessOffsetLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessOffsetLabelText.text = "Thick Offset";
                            thicknessOffsetLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                            EditorThemeManager.ApplyLightText(thicknessOffsetLabelText);
                            var thicknessOffsetLabelLayout = thicknessOffsetLabel.AddComponent<LayoutElement>();
                            thicknessOffsetLabelLayout.minWidth = 130f;

                            var thicknessOffsetX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "x");
                            var thicknessOffsetXStorage = thicknessOffsetX.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(thicknessOffsetXStorage.addButton);
                            CoreHelper.Delete(thicknessOffsetXStorage.subButton);
                            CoreHelper.Delete(thicknessOffsetXStorage.leftGreaterButton);
                            CoreHelper.Delete(thicknessOffsetXStorage.middleButton);
                            CoreHelper.Delete(thicknessOffsetXStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(thicknessOffsetXStorage);

                            var thicknessOffsetY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "y");
                            var thicknessOffsetYStorage = thicknessOffsetY.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(thicknessOffsetYStorage.addButton);
                            CoreHelper.Delete(thicknessOffsetYStorage.subButton);
                            CoreHelper.Delete(thicknessOffsetYStorage.leftGreaterButton);
                            CoreHelper.Delete(thicknessOffsetYStorage.middleButton);
                            CoreHelper.Delete(thicknessOffsetYStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(thicknessOffsetYStorage);

                            #endregion

                            #region Thickness Scale

                            var thicknessScale = Creator.NewUIObject("thickness scale", so);
                            var thicknessScaleLayout = thicknessScale.AddComponent<HorizontalLayoutGroup>();

                            var thicknessScaleLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessScale.transform, "label");
                            var thicknessScaleLabelText = thicknessScaleLabel.GetComponent<Text>();
                            thicknessScaleLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessScaleLabelText.text = "Thick Scale";
                            thicknessScaleLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                            EditorThemeManager.ApplyLightText(thicknessScaleLabelText);
                            var thicknessScaleLabelLayout = thicknessScaleLabel.AddComponent<LayoutElement>();
                            thicknessScaleLabelLayout.minWidth = 130f;

                            var thicknessScaleX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "x");
                            var thicknessScaleXStorage = thicknessScaleX.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(thicknessScaleXStorage.addButton);
                            CoreHelper.Delete(thicknessScaleXStorage.subButton);
                            CoreHelper.Delete(thicknessScaleXStorage.leftGreaterButton);
                            CoreHelper.Delete(thicknessScaleXStorage.middleButton);
                            CoreHelper.Delete(thicknessScaleXStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(thicknessScaleXStorage);

                            var thicknessScaleY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "y");
                            var thicknessScaleYStorage = thicknessScaleY.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(thicknessScaleYStorage.addButton);
                            CoreHelper.Delete(thicknessScaleYStorage.subButton);
                            CoreHelper.Delete(thicknessScaleYStorage.leftGreaterButton);
                            CoreHelper.Delete(thicknessScaleYStorage.middleButton);
                            CoreHelper.Delete(thicknessScaleYStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(thicknessScaleYStorage);

                            #endregion

                            #region Slices

                            var slices = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "slices");
                            var slicesStorage = slices.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(slicesStorage.addButton);
                            CoreHelper.Delete(slicesStorage.subButton);
                            CoreHelper.Delete(slicesStorage.leftGreaterButton);
                            CoreHelper.Delete(slicesStorage.middleButton);
                            CoreHelper.Delete(slicesStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(slicesStorage);

                            var slicesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(slices.transform, "label", 0);
                            var slicesLabelText = slicesLabel.GetComponent<Text>();
                            slicesLabelText.alignment = TextAnchor.MiddleLeft;
                            slicesLabelText.text = "Slices";
                            slicesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.ApplyLightText(slicesLabelText);
                            var slicesLabelLayout = slicesLabel.AddComponent<LayoutElement>();
                            slicesLabelLayout.minWidth = 100f;

                            #endregion

                            #region Angle

                            var rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "rotation");
                            var rotationStorage = rotation.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(rotationStorage.addButton);
                            CoreHelper.Delete(rotationStorage.subButton);
                            CoreHelper.Delete(rotationStorage.leftGreaterButton);
                            CoreHelper.Delete(rotationStorage.middleButton);
                            CoreHelper.Delete(rotationStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(rotationStorage);

                            var rotationLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(rotation.transform, "label", 0);
                            var rotationLabelText = rotationLabel.GetComponent<Text>();
                            rotationLabelText.alignment = TextAnchor.MiddleLeft;
                            rotationLabelText.text = "Angle";
                            rotationLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.ApplyLightText(rotationLabelText);
                            var rotationLabelLayout = rotationLabel.AddComponent<LayoutElement>();
                            rotationLabelLayout.minWidth = 100f;

                            #endregion
                        }
                    }
                }

                updatedShapes = true;
            }

        }
        
        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        public void RenderShape()
        {
            InitShape(multiObjectContent);

            var shape = multiShapes;
            var shapeSettings = multiShapeSettings;

            LSHelpers.SetActiveChildren(shapeSettings, false);

            shapeSettings.AsRT().sizeDelta = new Vector2(351f, multiShapeSelection.x == 4 ? 74f : 32f);
            shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, multiShapeSelection.x == 4 ? 74f : 32f);

            shapeSettings.GetChild(multiShapeSelection.x).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in shapeToggles)
            {
                int index = num;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);
                toggle.SetIsOnWithoutNotify(multiShapeSelection.x == index);
                toggle.onValueChanged.NewListener(_val =>
                {
                    multiShapeSelection = new Vector2Int(index, 0);

                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBeatmapObject)
                        {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            beatmapObject.Shape = multiShapeSelection.x;
                            beatmapObject.ShapeOption = multiShapeSelection.y;

                            if (beatmapObject.gradientType != GradientType.Normal && (index == 4 || index == 6 || index == 10))
                                beatmapObject.Shape = 0;

                            if (beatmapObject.ShapeType == ShapeType.Polygon && EditorConfig.Instance.AutoPolygonRadius.Value)
                                beatmapObject.polygonShape.Radius = beatmapObject.polygonShape.GetAutoRadius();

                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
                        }
                        if (timelineObject.isBackgroundObject)
                        {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                            backgroundObject.Shape = multiShapeSelection.x;
                            backgroundObject.ShapeOption = multiShapeSelection.y;

                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                        }
                    }

                    RTLevel.Current?.RecalculateObjectStates();
                    RenderShape();
                });

                num++;
            }

            switch ((ShapeType)multiShapeSelection.x)
            {
                case ShapeType.Text: {
                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        if (!updatedText)
                        {
                            updatedText = true;
                            textIF.textComponent.alignment = TextAnchor.UpperLeft;
                            textIF.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                            textIF.GetPlaceholderText().text = "Enter text...";
                            textIF.lineType = InputField.LineType.MultiLineNewline;

                            textIF.SetTextWithoutNotify(string.Empty);
                        }
                        textIF.onValueChanged.NewListener(_val =>
                        {
                            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                            {
                                if (timelineObject.isBeatmapObject)
                                {
                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                    beatmapObject.text = _val;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.TEXT);
                                }
                                if (timelineObject.isBackgroundObject)
                                {
                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                    backgroundObject.text = _val;

                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                }
                            }

                            RTLevel.Current?.RecalculateObjectStates();
                        });

                        if (!textIF.transform.Find("edit"))
                        {
                            var button = EditorPrefabHolder.Instance.DeleteButton.Duplicate(textIF.transform, "edit");
                            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                            buttonStorage.Sprite = EditorSprites.EditSprite;
                            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
                            buttonStorage.OnClick.NewListener(() => RTTextEditor.inst.SetInputField(textIF));
                            UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(160f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
                            EditorHelper.SetComplexity(button, Complexity.Advanced);
                        }
                        else
                        {
                            var button = textIF.transform.Find("edit").gameObject;
                            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                            buttonStorage.OnClick.NewListener(() => RTTextEditor.inst.SetInputField(textIF));
                        }

                        break;
                    }
                case ShapeType.Image: {
                        var select = shapeSettings.Find("7/select").GetComponent<Button>();
                        select.onClick.NewListener(() =>
                        {
                            var editorPath = EditorLevelManager.inst.CurrentLevel.path;
                            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
                            CoreHelper.Log($"Selected file: {jpgFile}");
                            if (!string.IsNullOrEmpty(jpgFile))
                            {
                                string jpgFileLocation = editorPath + "/" + Path.GetFileName(jpgFile);
                                CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");

                                var levelPath = jpgFile.Replace("\\", "/").Remove(editorPath + "/");
                                CoreHelper.Log($"levelPath: {levelPath}");

                                if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
                                {
                                    RTFile.CopyFile(jpgFile, jpgFileLocation);
                                    CoreHelper.Log($"Copied file to : {jpgFileLocation}");
                                }
                                else
                                    jpgFileLocation = editorPath + "/" + levelPath;

                                CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");

                                var _val = jpgFileLocation.Remove(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1));
                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                                {
                                    if (timelineObject.isBeatmapObject)
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.text = _val;
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.TEXT);
                                    }
                                    if (timelineObject.isBackgroundObject)
                                    {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        backgroundObject.text = _val;

                                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                    }
                                }

                                RTLevel.Current?.RecalculateObjectStates();
                                RenderShape();
                            }
                        });
                        shapeSettings.Find("7/text").GetComponent<Text>().text = "Select an image";

                        if (shapeSettings.Find("7/set"))
                            CoreHelper.Destroy(shapeSettings.Find("7/set").gameObject);

                        break;
                    }
                case ShapeType.Polygon: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 276f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 276f);

                        var radius = shapeSettings.Find("10/radius").gameObject.GetComponent<InputFieldStorage>();
                        radius.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (EditorConfig.Instance.AutoPolygonRadius.Value)
                            {
                                EditorManager.inst.DisplayNotification($"Cannot set a custom radius for polygon shapes due to {EditorConfig.Instance.AutoPolygonRadius.Key} being on.", 6f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0.1f, 10f);
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.Radius = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(radius, min: 0.1f, max: 10f);
                        TriggerHelper.AddEventTriggers(radius.inputField.gameObject, TriggerHelper.ScrollDelta(radius.inputField, min: 0.1f, max: 10f));
                        
                        var sides = shapeSettings.Find("10/sides").gameObject.GetComponent<InputFieldStorage>();
                        sides.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 3, 32);
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.Sides = num;
                                    if (EditorConfig.Instance.AutoPolygonRadius.Value)
                                        beatmapObject.polygonShape.Radius = beatmapObject.polygonShape.GetAutoRadius();
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(sides, min: 3, max: 32);
                        TriggerHelper.AddEventTriggers(sides.inputField.gameObject, TriggerHelper.ScrollDeltaInt(sides.inputField, min: 3, max: 32));

                        var roundness = shapeSettings.Find("10/roundness").gameObject.GetComponent<InputFieldStorage>();
                        roundness.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.Roundness = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(roundness, max: 1f);
                        TriggerHelper.AddEventTriggers(roundness.inputField.gameObject, TriggerHelper.ScrollDelta(roundness.inputField, max: 1f));

                        var thickness = shapeSettings.Find("10/thickness").gameObject.GetComponent<InputFieldStorage>();
                        thickness.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.Thickness = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thickness, max: 1f);
                        TriggerHelper.AddEventTriggers(thickness.inputField.gameObject, TriggerHelper.ScrollDelta(thickness.inputField, max: 1f));
                        
                        var thicknessOffsetX = shapeSettings.Find("10/thickness offset/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetX.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.ThicknessOffset = new Vector2(num, beatmapObject.polygonShape.ThicknessOffset.y);
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetX);
                        TriggerHelper.AddEventTriggers(thicknessOffsetX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetX.inputField));
                        
                        var thicknessOffsetY = shapeSettings.Find("10/thickness offset/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetY.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.ThicknessOffset = new Vector2(beatmapObject.polygonShape.ThicknessOffset.x, num);
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetY);
                        TriggerHelper.AddEventTriggers(thicknessOffsetY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetY.inputField));
                        
                        var thicknessScaleX = shapeSettings.Find("10/thickness scale/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleX.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.ThicknessScale = new Vector2(num, beatmapObject.polygonShape.ThicknessScale.y);
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleX);
                        TriggerHelper.AddEventTriggers(thicknessScaleX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleX.inputField));
                        
                        var thicknessScaleY = shapeSettings.Find("10/thickness scale/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleY.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.ThicknessScale = new Vector2(beatmapObject.polygonShape.ThicknessScale.x, num);
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleY);
                        TriggerHelper.AddEventTriggers(thicknessScaleY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleY.inputField));

                        var slices = shapeSettings.Find("10/slices").gameObject.GetComponent<InputFieldStorage>();
                        slices.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 1, 32);
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.Slices = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(slices, min: 1, max: 32);
                        TriggerHelper.AddEventTriggers(slices.inputField.gameObject, TriggerHelper.ScrollDeltaInt(slices.inputField, min: 1, max: 32));

                        var rotation = shapeSettings.Find("10/rotation").gameObject.GetComponent<InputFieldStorage>();
                        rotation.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.Angle = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(rotation, 15f, 3f);
                        TriggerHelper.AddEventTriggers(rotation.inputField.gameObject, TriggerHelper.ScrollDelta(rotation.inputField, 15f, 3f));

                        break;
                    }
                default: {
                        num = 0;
                        foreach (var toggle in shapeOptionToggles[multiShapeSelection.x])
                        {
                            int index = num;
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[multiShapeSelection.x]);
                            toggle.SetIsOnWithoutNotify(multiShapeSelection.y == index);
                            toggle.onValueChanged.NewListener(_val =>
                            {
                                multiShapeSelection.y = index;

                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                                {
                                    if (timelineObject.isBeatmapObject)
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.Shape = multiShapeSelection.x;
                                        beatmapObject.ShapeOption = multiShapeSelection.y;

                                        if (beatmapObject.gradientType != GradientType.Normal && (index == 4 || index == 6 || index == 10))
                                            beatmapObject.Shape = 0;

                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
                                    }
                                    if (timelineObject.isBackgroundObject)
                                    {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        backgroundObject.Shape = multiShapeSelection.x;
                                        backgroundObject.ShapeOption = multiShapeSelection.y;

                                        RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                                    }
                                }

                                RTLevel.Current?.RecalculateObjectStates();
                                RenderShape();
                            });

                            num++;
                        }

                        break;
                    }
            }
        }

        void SetupEditorColorSetter(Transform parent, string name, string label, string placeholder, string buttonLabel, Action<InputField> setColor)
        {
            var labels = GenerateLabels(parent, 32f, label);

            var replaceName = Creator.NewUIObject(name.ToLower(), parent);
            replaceName.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
            var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
            multiSyncGLG.spacing = new Vector2(8f, 8f);
            multiSyncGLG.cellSize = new Vector2(124f, 32f);

            var oldName = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(replaceName.transform, name.ToLower());

            CoreHelper.Destroy(oldName.GetComponent<EventTrigger>());
            var inputField = oldName.GetComponent<InputField>();
            inputField.characterValidation = InputField.CharacterValidation.None;
            inputField.textComponent.alignment = TextAnchor.MiddleLeft;
            inputField.textComponent.fontSize = 16;
            inputField.text = string.Empty;
            inputField.GetPlaceholderText().text = placeholder;
            inputField.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
            inputField.GetPlaceholderText().fontSize = 16;
            inputField.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

            inputField.onValueChanged.ClearAll();

            var contextClickable = oldName.AddComponent<ContextClickable>();
            contextClickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                var currentHexColor = inputField.text;
                EditorContextMenu.inst.ShowContextMenu(EditorContextMenu.GetEditorColorFunctions(inputField, () => currentHexColor));
            };

            EditorHelper.AddInputFieldContextMenu(inputField);
            TriggerHelper.InversableField(inputField, InputFieldSwapper.Type.String);

            EditorThemeManager.ApplyInputField(inputField);

            var setColorButton = EditorPrefabHolder.Instance.Function1Button.Duplicate(replaceName.transform, "set color");
            var setColorButtonStorage = setColorButton.GetComponent<FunctionButtonStorage>();
            setColorButton.transform.localScale = Vector3.one;
            setColorButton.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
            setColorButton.GetComponent<LayoutElement>().minWidth = 32f;

            setColorButtonStorage.Text = buttonLabel;
            setColorButtonStorage.OnClick.NewListener(() => setColor?.Invoke(inputField));

            EditorThemeManager.ApplyGraphic(setColorButtonStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(setColorButtonStorage.label, ThemeGroup.Function_1_Text);

            EditorHelper.SetComplexity(labels, Complexity.Normal);
            EditorHelper.SetComplexity(replaceName, Complexity.Normal);
        }

        void SetupReplaceStrings(Transform parent, string name, string oldNameStr, string oldNamePlaceholder, string newNameStr, string newNamePlaceholder, Action<InputField, InputField> replacer)
        {
            var labels = GenerateLabels(parent, 32f, name);

            var replaceName = Creator.NewUIObject(name.ToLower(), parent);
            replaceName.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
            var multiSyncGLG = replaceName.AddComponent<GridLayoutGroup>();
            multiSyncGLG.spacing = new Vector2(8f, 8f);
            multiSyncGLG.cellSize = new Vector2(124f, 32f);

            var oldName = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(replaceName.transform, oldNameStr.ToLower());

            CoreHelper.Destroy(oldName.GetComponent<EventTrigger>());
            var oldNameIF = oldName.GetComponent<InputField>();
            oldNameIF.characterValidation = InputField.CharacterValidation.None;
            oldNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
            oldNameIF.textComponent.fontSize = 16;
            oldNameIF.text = oldNameStr;
            oldNameIF.GetPlaceholderText().text = oldNamePlaceholder;
            oldNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
            oldNameIF.GetPlaceholderText().fontSize = 16;
            oldNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

            oldNameIF.onValueChanged.ClearAll();

            EditorHelper.AddInputFieldContextMenu(oldNameIF);
            TriggerHelper.InversableField(oldNameIF, InputFieldSwapper.Type.String);

            EditorThemeManager.ApplyInputField(oldNameIF);

            var newName = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(replaceName.transform, newNameStr.ToLower());

            CoreHelper.Destroy(newName.GetComponent<EventTrigger>());
            var newNameIF = newName.GetComponent<InputField>();
            newNameIF.characterValidation = InputField.CharacterValidation.None;
            newNameIF.textComponent.alignment = TextAnchor.MiddleLeft;
            newNameIF.textComponent.fontSize = 16;
            newNameIF.text = newNameStr;
            newNameIF.GetPlaceholderText().text = newNamePlaceholder;
            newNameIF.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
            newNameIF.GetPlaceholderText().fontSize = 16;
            newNameIF.GetPlaceholderText().color = new Color(0f, 0f, 0f, 0.3f);

            newNameIF.onValueChanged.ClearAll();

            EditorHelper.AddInputFieldContextMenu(newNameIF);
            TriggerHelper.InversableField(newNameIF, InputFieldSwapper.Type.String);

            EditorThemeManager.ApplyInputField(newNameIF);

            var replace = EditorPrefabHolder.Instance.Function1Button.Duplicate(replaceName.transform, "replace");
            replace.transform.localScale = Vector3.one;
            replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
            replace.GetComponent<LayoutElement>().minWidth = 32f;

            var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

            replaceText.text = "Replace";

            EditorThemeManager.ApplyGraphic(replace.GetComponent<Image>(), ThemeGroup.Function_1, true);
            EditorThemeManager.ApplyGraphic(replaceText, ThemeGroup.Function_1_Text);

            var button = replace.GetComponent<Button>();
            button.onClick.NewListener(() => replacer?.Invoke(oldNameIF, newNameIF));

            EditorHelper.SetComplexity(labels, Complexity.Advanced);
            EditorHelper.SetComplexity(replaceName, Complexity.Advanced);
        }

        void GeneratePad(Transform parent)
        {
            var gameObject = Creator.NewUIObject("padder", parent);
            var image = gameObject.AddComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(395f, 4f);
            EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_3);
        }

        void GeneratePad(Transform parent, Complexity complexity, bool onlySpecificComplexity = false)
        {
            var gameObject = Creator.NewUIObject("padder", parent);
            var image = gameObject.AddComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(395f, 4f);
            EditorThemeManager.ApplyGraphic(image, ThemeGroup.Background_3);
            EditorHelper.SetComplexity(gameObject, complexity, onlySpecificComplexity);
        }

        void GeneratePasteKeyframeData(Transform parent, int type, string name)
        {
            GeneratePasteKeyframeData(parent, () =>
            {
                var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(type);
                if (copiedKeyframeData == null)
                {
                    EditorManager.inst.DisplayNotification($"{name} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                {
                    var bm = timelineObject.GetData<BeatmapObject>();
                    for (int i = 0; i < bm.events[type].Count; i++)
                    {
                        var kf = (EventKeyframe)bm.events[type][i];
                        kf.curve = copiedKeyframeData.curve;
                        kf.values = copiedKeyframeData.values.Copy();
                        kf.randomValues = copiedKeyframeData.randomValues.Copy();
                        kf.random = copiedKeyframeData.random;
                        kf.relative = copiedKeyframeData.relative;

                        RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                    }
                }
                EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
            }, _val =>
            {
                var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(type);
                string name = type switch
                {
                    0 => "Position",
                    1 => "Scale",
                    2 => "Rotation",
                    3 => "Color",
                    _ => "Null"
                };
                if (copiedKeyframeData == null)
                {
                    EditorManager.inst.DisplayNotification($"{name} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (int.TryParse(_val, out int num))
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedBeatmapObjects)
                    {
                        var bm = timelineObject.GetData<BeatmapObject>();

                        var kf = bm.events[type][Mathf.Clamp(num, 0, bm.events[type].Count - 1)];
                        kf.curve = copiedKeyframeData.curve;
                        kf.values = copiedKeyframeData.values.Copy();
                        kf.randomValues = copiedKeyframeData.randomValues.Copy();
                        kf.random = copiedKeyframeData.random;
                        kf.relative = copiedKeyframeData.relative;

                        RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                    }
                    EditorManager.inst.DisplayNotification($"Pasted {name.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                }
            });
        }

        void GeneratePasteKeyframeData(Transform parent, Action pasteAll, Action<string> pasteToIndex)
        {
            var index = CreateInputField("index", "0", "Enter index...", parent, maxValue: int.MaxValue);

            var pasteAllTypesBase = Creator.NewUIObject("paste all types", parent);
            pasteAllTypesBase.transform.AsRT().sizeDelta = new Vector2(390f, 32f);

            var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
            pasteAllTypesBaseHLG.childControlHeight = false;
            pasteAllTypesBaseHLG.childControlWidth = false;
            pasteAllTypesBaseHLG.childForceExpandHeight = false;
            pasteAllTypesBaseHLG.childForceExpandWidth = false;
            pasteAllTypesBaseHLG.spacing = 8f;

            var pasteAllTypesToAllObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteAllTypesBase.transform, "paste");
            pasteAllTypesToAllObject.transform.localScale = Vector3.one;

            pasteAllTypesToAllObject.transform.AsRT().sizeDelta = new Vector2(180f, 32f);

            var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
            pasteAllTypesToAllText.text = "Paste to All";

            EditorThemeManager.ApplyGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

            var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
            pasteAllTypesToAll.onClick.NewListener(() => pasteAll?.Invoke());

            var pasteAllTypesToIndexObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteAllTypesBase.transform, "paste");
            pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
            pasteAllTypesToIndexText.text = "Paste to Index";

            EditorThemeManager.ApplyGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

            var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
            pasteAllTypesToIndex.onClick.NewListener(() => pasteToIndex?.Invoke(index.text));

            EditorHelper.SetComplexity(index.transform.parent.gameObject, Complexity.Advanced);
            EditorHelper.SetComplexity(pasteAllTypesBase, Complexity.Advanced);
        }

        void SyncObjectData(string nameContext, PointerEventData eventData, Action<TimelineObject, BeatmapObject> update, bool renderTimelineObject = false, bool updateObject = true, string updateContext = "")
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                EditorContextMenu.inst.ShowContextMenu(400f,
                    new ButtonElement($"Sync {nameContext} via Search", () => ObjectEditor.inst.ShowObjectSearch(beatmapObject =>
                    {
                        SyncObjectData(timelineObject => update?.Invoke(timelineObject, beatmapObject), renderTimelineObject, updateObject, updateContext);
                        ObjectEditor.inst.ObjectSearchPopup.Close();
                    })),
                    new ButtonElement($"Sync {nameContext} via Picker", () => EditorTimeline.inst.onSelectTimelineObject = to =>
                    {
                        if (!to.isBeatmapObject)
                            return;

                        var beatmapObject = to.GetData<BeatmapObject>();
                        SyncObjectData(timelineObject => update?.Invoke(timelineObject, beatmapObject), renderTimelineObject, updateObject, updateContext);
                    }));

                return;
            }

            ObjectEditor.inst.ShowObjectSearch(beatmapObject =>
            {
                SyncObjectData(timelineObject => update?.Invoke(timelineObject, beatmapObject), renderTimelineObject, updateObject, updateContext);
                ObjectEditor.inst.ObjectSearchPopup.Close();
            });
        }

        void SyncObjectData(Action<TimelineObject> update, bool renderTimelineObject = false, bool updateObject = true, string updateContext = "")
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
            {
                update?.Invoke(timelineObject);

                if (renderTimelineObject)
                    EditorTimeline.inst.RenderTimelineObject(timelineObject);

                if (!updateObject)
                    continue;

                if (!string.IsNullOrEmpty(updateContext))
                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), updateContext);
                else
                    RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>());
            }
        }

        public void SetKeyframeValues(EventKeyframe kf, Dropdown curves,
            string opacity, string hue, string sat, string val, string opacityGradient, string hueGradient, string satGradient, string valGradient)
        {
            if (curves.value != 0)
                kf.curve = RTEditor.inst.GetEasing(curves.value - 1);
            if (currentMultiColorSelection >= 0)
                kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacity))
                kf.values[1] = Mathf.Clamp(kf.values[1] - Parser.TryParse(opacity, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hue))
                kf.values[2] = Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(sat))
                kf.values[3] = Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(val))
                kf.values[4] = Parser.TryParse(val, 0f);

            // Gradient
            if (currentMultiGradientColorSelection >= 0)
                kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacityGradient))
                kf.values[6] = -Mathf.Clamp(Parser.TryParse(opacityGradient, 1f), 0f, 1f) + 1f;
            if (!string.IsNullOrEmpty(hueGradient))
                kf.values[7] = Parser.TryParse(hueGradient, 0f);
            if (!string.IsNullOrEmpty(satGradient))
                kf.values[8] = Parser.TryParse(satGradient, 0f);
            if (!string.IsNullOrEmpty(valGradient))
                kf.values[9] = Parser.TryParse(valGradient, 0f);
        }

        public void AddKeyframeValues(EventKeyframe kf, Dropdown curves,
            string opacity, string hue, string sat, string val, string opacityGradient, string hueGradient, string satGradient, string valGradient)
        {
            if (curves.value != 0)
                kf.curve = RTEditor.inst.GetEasing(curves.value - 1);
            if (currentMultiColorSelection >= 0)
                kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacity))
                kf.values[1] = Mathf.Clamp(kf.values[1] - Parser.TryParse(opacity, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hue))
                kf.values[2] += Parser.TryParse(hue, 0f);
            if (!string.IsNullOrEmpty(sat))
                kf.values[3] += Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(val))
                kf.values[4] += Parser.TryParse(val, 0f);

            // Gradient
            if (currentMultiGradientColorSelection >= 0)
                kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
            if (!string.IsNullOrEmpty(opacityGradient))
                kf.values[6] = Mathf.Clamp(kf.values[6] - Parser.TryParse(opacityGradient, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hueGradient))
                kf.values[7] += Parser.TryParse(hueGradient, 0f);
            if (!string.IsNullOrEmpty(satGradient))
                kf.values[8] += Parser.TryParse(satGradient, 0f);
            if (!string.IsNullOrEmpty(valGradient))
                kf.values[9] += Parser.TryParse(valGradient, 0f);
        }

        public void SubKeyframeValues(EventKeyframe kf, Dropdown curves,
            string opacity, string hue, string sat, string val, string opacityGradient, string hueGradient, string satGradient, string valGradient)
        {
            if (curves.value != 0)
                kf.curve = RTEditor.inst.GetEasing(curves.value - 1);
            if (currentMultiColorSelection >= 0)
                kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(opacity))
                kf.values[1] = Mathf.Clamp(kf.values[1] + Parser.TryParse(opacity, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hue))
                kf.values[2] -= Parser.TryParse(hue, 0f);
            if (!string.IsNullOrEmpty(sat))
                kf.values[3] -= Parser.TryParse(sat, 0f);
            if (!string.IsNullOrEmpty(val))
                kf.values[4] -= Parser.TryParse(val, 0f);

            // Gradient
            if (currentMultiGradientColorSelection >= 0)
                kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
            if (!string.IsNullOrEmpty(opacityGradient))
                kf.values[6] = Mathf.Clamp(kf.values[6] + Parser.TryParse(opacityGradient, 1f), 0f, 1f);
            if (!string.IsNullOrEmpty(hueGradient))
                kf.values[7] -= Parser.TryParse(hueGradient, 0f);
            if (!string.IsNullOrEmpty(satGradient))
                kf.values[8] -= Parser.TryParse(satGradient, 0f);
            if (!string.IsNullOrEmpty(valGradient))
                kf.values[9] -= Parser.TryParse(valGradient, 0f);
        }

        public GameObject GenerateLabels(Transform parent, float sizeY, params string[] labels)
        {
            var labelBase = Creator.NewUIObject("label", parent);
            labelBase.transform.AsRT().sizeDelta = new Vector2(0f, sizeY);
            labelBase.AddComponent<HorizontalLayoutGroup>();
            var labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;

            for (int i = 0; i < labels.Length; i++)
            {
                var label = labelPrefab.Duplicate(labelBase.transform, "text");
                var labelText = label.GetComponent<Text>();
                labelText.text = labels[i];
                EditorThemeManager.ApplyLightText(labelText);
            }

            return labelBase;
        }

        public GameObject GenerateLabels(Transform parent, float sizeY, params Label[] labels)
        {
            var labelBase = Creator.NewUIObject("label", parent);
            labelBase.transform.AsRT().sizeDelta = new Vector2(0f, sizeY);
            labelBase.AddComponent<HorizontalLayoutGroup>();
            var labelPrefab = EditorManager.inst.folderButtonPrefab.transform.GetChild(0).gameObject;

            for (int i = 0; i < labels.Length; i++)
            {
                var label = labelPrefab.Duplicate(labelBase.transform, "text");
                var labelText = label.GetComponent<Text>();
                labels[i].Apply(labelText);
                EditorThemeManager.ApplyLightText(labelText);
            }

            return labelBase;
        }

        public InputFieldStorage GenerateInputField(Transform parent, string name, string defaultValue, string placeholder, bool doMiddle = false, bool doLeftGreater = false, bool doRightGreater = false)
        {
            var gameObject = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, name);
            gameObject.transform.localScale = Vector3.one;
            var inputFieldStorage = gameObject.GetComponent<InputFieldStorage>();
            inputFieldStorage.inputField.GetPlaceholderText().text = placeholder;

            gameObject.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

            inputFieldStorage.inputField.onValueChanged.ClearAll();
            inputFieldStorage.inputField.text = defaultValue;
            inputFieldStorage.inputField.transform.AsRT().sizeDelta = new Vector2(300f, 32f);

            if (doLeftGreater)
                EditorThemeManager.ApplySelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            else
                CoreHelper.Delete(inputFieldStorage.leftGreaterButton);

            if (doRightGreater)
                EditorThemeManager.ApplySelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
            else
                CoreHelper.Delete(inputFieldStorage.rightGreaterButton);

            if (doMiddle)
                EditorThemeManager.ApplySelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);
            else
                CoreHelper.Delete(inputFieldStorage.middleButton);

            EditorThemeManager.ApplyInputField(inputFieldStorage);

            return inputFieldStorage;
        }

        public GameObject GenerateButtons(Transform parent, float sizeY, float spacing, params ButtonFunction[] buttons)
        {
            var p = Creator.NewUIObject("buttons", parent);
            p.transform.AsRT().sizeDelta = new Vector2(0f, sizeY);
            var pHLG = p.AddComponent<HorizontalLayoutGroup>();
            pHLG.spacing = spacing;

            for (int i = 0; i < buttons.Length; i++)
                GenerateButton(p.transform, buttons[i]);

            return p;
        }

        public GameObject GenerateButtons(Transform parent, float sizeY, float spacing, ThemeGroup buttonGroup, ThemeGroup labelGroup, params ButtonFunction[] buttons)
        {
            var p = Creator.NewUIObject("buttons", parent);
            p.transform.AsRT().sizeDelta = new Vector2(0f, sizeY);
            var pHLG = p.AddComponent<HorizontalLayoutGroup>();
            pHLG.spacing = spacing;

            for (int i = 0; i < buttons.Length; i++)
                GenerateButton(p.transform, buttons[i], buttonGroup, labelGroup);

            return p;
        }

        public GameObject GenerateButton(Transform parent, ButtonFunction buttonFunction, ThemeGroup buttonGroup = ThemeGroup.Function_1, ThemeGroup labelGroup = ThemeGroup.Function_1_Text)
        {
            var button = EditorPrefabHolder.Instance.Function1Button.Duplicate(parent, buttonFunction.Name);
            var buttonStorage = button.GetComponent<FunctionButtonStorage>();

            if (buttonFunction.OnClick != null)
            {
                var clickable = button.AddComponent<ContextClickable>();
                clickable.onClick = buttonFunction.OnClick;
            }
            else
                buttonStorage.OnClick.NewListener(() => buttonFunction.Action?.Invoke());

            buttonStorage.label.fontSize = buttonFunction.FontSize;
            buttonStorage.Text = buttonFunction.Name;

            EditorThemeManager.ApplyGraphic(buttonStorage.button.image, buttonFunction.ButtonThemeGroup ?? buttonGroup, true);
            EditorThemeManager.ApplyGraphic(buttonStorage.label, buttonFunction.LabelThemeGroup ?? labelGroup);

            return button;
        }

        InputField CreateInputField(string name, string value, string placeholder, Transform parent, float length = 340f, bool isInteger = true, double minValue = 0f, double maxValue = 0f)
        {
            var gameObject = EditorPrefabHolder.Instance.NumberInputField.Duplicate(parent, name);
            gameObject.transform.localScale = Vector3.one;
            var inputFieldStorage = gameObject.GetComponent<InputFieldStorage>();

            inputFieldStorage.inputField.image.rectTransform.sizeDelta = new Vector2(length, 32f);
            inputFieldStorage.inputField.GetPlaceholderText().text = placeholder;

            gameObject.transform.AsRT().sizeDelta = new Vector2(428f, 32f);

            inputFieldStorage.inputField.text = value;

            if (isInteger)
            {
                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField, min: (int)minValue, max: (int)maxValue));
                TriggerHelper.IncreaseDecreaseButtonsInt(inputFieldStorage.inputField, min: (int)minValue, max: (int)maxValue, t: gameObject.transform);
            }
            else
            {
                TriggerHelper.AddEventTriggers(gameObject, TriggerHelper.ScrollDelta(inputFieldStorage.inputField, min: (float)minValue, max: (float)maxValue));
                TriggerHelper.IncreaseDecreaseButtons(inputFieldStorage.inputField, min: (float)minValue, max: (float)maxValue, t: gameObject.transform);
            }

            CoreHelper.Delete(inputFieldStorage.leftGreaterButton);
            CoreHelper.Delete(inputFieldStorage.middleButton);
            CoreHelper.Delete(inputFieldStorage.rightGreaterButton);

            EditorThemeManager.ApplyInputField(inputFieldStorage);

            return inputFieldStorage.inputField;
        }

        void UpdateMultiColorButtons()
        {
            for (int i = 0; i < multiColorButtons.Count; i++)
                multiColorButtons[i].Selected.SetActive(currentMultiColorSelection == i);

            for (int i = 0; i < multiGradientColorButtons.Count; i++)
                multiGradientColorButtons[i].Selected.SetActive(currentMultiGradientColorSelection == i);
        }

        #endregion

        public class ButtonFunction
        {
            public ButtonFunction(bool isSpacer, float spacerSize = 4f)
            {
                IsSpacer = isSpacer;
                SpacerSize = spacerSize;
            }

            public ButtonFunction(string name, Action action, string tooltipGroup = null, ThemeGroup? buttonThemeGroup = null, ThemeGroup? labelThemeGroup = null)
            {
                Name = name;
                Action = action;
                TooltipGroup = tooltipGroup;

                ButtonThemeGroup = buttonThemeGroup;
                LabelThemeGroup = labelThemeGroup;
            }

            public ButtonFunction(string name, Action<PointerEventData> onClick, string tooltipGroup = null, ThemeGroup? buttonThemeGroup = null, ThemeGroup? labelThemeGroup = null)
            {
                Name = name;
                OnClick = onClick;
                TooltipGroup = tooltipGroup;

                ButtonThemeGroup = buttonThemeGroup;
                LabelThemeGroup = labelThemeGroup;
            }

            public bool IsSpacer { get; set; }
            public float SpacerSize { get; set; } = 4f;
            public string Name { get; set; }
            public int FontSize { get; set; } = 20;
            public Action Action { get; set; }
            public Action<PointerEventData> OnClick { get; set; }

            public ThemeGroup? ButtonThemeGroup { get; set; }
            public ThemeGroup? LabelThemeGroup { get; set; }

            public string TooltipGroup { get; set; }
        }
    }
}

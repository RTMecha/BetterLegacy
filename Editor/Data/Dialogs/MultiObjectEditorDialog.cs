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
using BetterLegacy.Core.Data.Modifiers;
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
        public MultiObjectEditorDialog() : base(MULTI_OBJECT_EDITOR) { }

        #region Values

        public EditorDialog Dialog { get; set; }

        List<MultiColorButton> multiColorButtons = new List<MultiColorButton>();
        List<MultiColorButton> multiGradientColorButtons = new List<MultiColorButton>();
        int currentMultiColorSelection = -1;
        int currentMultiGradientColorSelection = -1;
        bool setCurve;

        bool updatedShapes;
        bool updatedText;
        public List<Toggle> shapeToggles = new List<Toggle>();
        public List<List<Toggle>> shapeOptionToggles = new List<List<Toggle>>();

        Transform multiShapes;
        Transform multiShapeSettings;
        public Vector2Int shapeSelection;
        public Transform multiObjectContent;

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

        public LabelElement SelectedObjectCountLabel { get; set; }
        public LabelElement SelectedBackgroundObjectCountLabel { get; set; }
        public LabelElement SelectedPrefabObjectCountLabel { get; set; }
        public LabelElement SelectedTotalCountLabel { get; set; }

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

                EditorThemeManager.ApplySelectable(tabButton.button, EditorThemeManager.GetTabThemeGroup(index));
                tab.AddComponent<ContrastColors>().Init(tabButton.label, tab.GetComponent<Image>());

                var tabType = (Tab)i;
                if (tabType == Tab.Modifiers || tabType == Tab.Sync)
                    EditorHelper.SetComplexity(tab, Complexity.Advanced);
                CoreHelper.Log($"Setting up {tabType}");

                TabButtons.Add(tabButton);

                var scrollViewElement = new ScrollViewElement(ScrollViewElement.Direction.Vertical);
                scrollViewElement.Init(EditorElement.InitSettings.Default.Parent(GameObject.transform).Rect(RectValues.HorizontalAnchored.AnchoredPosition(0f, -45f).SizeDelta(0f, 635f)));
                scrollViewElement.SetActive(i == 0);
                ScrollViews.Add(scrollViewElement);
                var parent = scrollViewElement.Content;
                parent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(left: 8, right: 8, top: 8, bottom: 8);

                switch (tabType)
                {
                    case Tab.Editor: {
                            SelectedObjectCountLabel = new LabelElement(string.Empty);
                            SelectedObjectCountLabel.Init(EditorElement.InitSettings.Default.Parent(parent));
                            SelectedBackgroundObjectCountLabel = new LabelElement(string.Empty);
                            SelectedBackgroundObjectCountLabel.Init(EditorElement.InitSettings.Default.Parent(parent));
                            SelectedPrefabObjectCountLabel = new LabelElement(string.Empty);
                            SelectedPrefabObjectCountLabel.Init(EditorElement.InitSettings.Default.Parent(parent));
                            SelectedTotalCountLabel = new LabelElement(string.Empty);
                            SelectedTotalCountLabel.Init(EditorElement.InitSettings.Default.Parent(parent));

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Properties

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
                                
                            new LabelsElement("Editor Bin").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerInt()
                            {
                                standardArrowFunctions = false,
                                max = EditorTimeline.MAX_BINS,
                                leftGreaterArrowClicked = _val => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Bin = 0;
                                    timelineObject.RenderPosLength();
                                }),
                                leftGreaterSprite = EditorSprites.UpArrow,
                                leftArrowClicked = _val =>
                                {
                                    if (int.TryParse(_val, out int num))
                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            timelineObject.Bin -= num;
                                            timelineObject.RenderPosLength();
                                        });
                                },
                                middleClicked = _val =>
                                {
                                    if (int.TryParse(_val, out int num))
                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            timelineObject.Bin = num;
                                            timelineObject.RenderPosLength();
                                        });
                                },
                                rightArrowClicked = _val =>
                                {
                                    if (int.TryParse(_val, out int num))
                                        MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                        {
                                            timelineObject.Bin += num;
                                            timelineObject.RenderPosLength();
                                        });
                                },
                                rightGreaterArrowClicked = _val => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Bin = EditorTimeline.inst.BinCount),
                                rightGreaterSprite = EditorSprites.DownArrow,
                            }).Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LabelsElement("Object Name").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new NumberInputElement("object name", null, new NumberInputElement.ArrowHandler()
                            {
                                standardArrowFunctions = false,
                                middleClicked = _val =>
                                {
                                    MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.SetName(_val));
                                    EditorManager.inst.DisplayNotification($"Set the name \"{_val}\" to all selected objects.", 4f, EditorManager.NotificationType.Success);
                                },
                                subClicked = _val =>
                                {
                                    MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (!timelineObject.isPrefabObject)
                                            timelineObject.SetName(timelineObject.Name.Remove(_val));
                                    });
                                    EditorManager.inst.DisplayNotification($"Removed the name \"{_val}\" from all selected objects.", 4f, EditorManager.NotificationType.Success);
                                },
                                addClicked = _val =>
                                {
                                    MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (!timelineObject.isPrefabObject)
                                            timelineObject.SetName(timelineObject.Name + _val);
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

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Colors

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

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Editing States

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Hidden State")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
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
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
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
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
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
                                }), labelAlignment: TextAnchor.MiddleCenter));
                                
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Selectable in Preview")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (timelineObject.isBackgroundObject)
                                        return;
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
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (timelineObject.isBackgroundObject)
                                        return;
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
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (timelineObject.isBackgroundObject)
                                        return;
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
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Groups

                            new LabelsElement("Editor Groups").Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button("Assign Selected to Editor Group", EditorTimeline.inst.OpenEditorGroupsPopup),
                                ButtonElement.Label1Button("Remove Editor Groups from Selected", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Group = string.Empty;
                                    timelineObject.Render();
                                }), buttonThemeGroup: ThemeGroup.Delete, graphicThemeGroup: ThemeGroup.Delete_Text));
                            // validates all the objects editor groups.
                            ButtonElement.Label1Button("Validate Editor Groups", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                            {
                                if (!RTEditor.inst.editorInfo.editorGroups.Has(x => x.name == timelineObject.Group))
                                    RTEditor.inst.editorInfo.editorGroups.Add(new EditorGroup(timelineObject.Group));
                            }));
                            new NumberInputElement("editor group", null, new NumberInputElement.ArrowHandler()
                            {
                                standardArrowFunctions = false,
                                middleClicked = _val =>
                                {
                                    MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Group = _val);
                                    EditorManager.inst.DisplayNotification($"Set the editor group \"{_val}\" to all selected objects.", 4f, EditorManager.NotificationType.Success);
                                },
                                subClicked = _val =>
                                {
                                    MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Group = timelineObject.Group.Remove(_val));
                                    EditorManager.inst.DisplayNotification($"Removed \"{_val}\" from the editor group of all selected objects.", 4f, EditorManager.NotificationType.Success);
                                },
                                addClicked = _val =>
                                {
                                    MultiObjectEditor.inst.ForEachTimelineObject(timelineObject => timelineObject.Group += _val);
                                    EditorManager.inst.DisplayNotification($"Added \"{_val}\" to the editor group of all selected objects.", 4f, EditorManager.NotificationType.Success);
                                },
                            }).Init(EditorElement.InitSettings.Default.Parent(parent));

                            #endregion

                            break;
                        }
                    case Tab.Prefab: {
                            #region Prefab Instance

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

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

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

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Transform

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

                            #endregion

                            #region Instance Data

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

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

                            #endregion

                            #region Parent

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Offset Parent Desync")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachPrefabObject(prefabObject =>
                                {
                                    prefabObject.offsetParentDesyncTime = true;
                                    RTLevel.Current?.UpdatePrefab(prefabObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachPrefabObject(prefabObject =>
                                {
                                    prefabObject.offsetParentDesyncTime = false;
                                    RTLevel.Current?.UpdatePrefab(prefabObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachPrefabObject(prefabObject =>
                                {
                                    prefabObject.offsetParentDesyncTime = !prefabObject.offsetParentDesyncTime;
                                    RTLevel.Current?.UpdatePrefab(prefabObject);
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Parent Self (Prefab Object is origin)")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachPrefabObject(prefabObject =>
                                {
                                    prefabObject.parentSelf = true;
                                    RTLevel.Current?.UpdatePrefab(prefabObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachPrefabObject(prefabObject =>
                                {
                                    prefabObject.parentSelf = false;
                                    RTLevel.Current?.UpdatePrefab(prefabObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachPrefabObject(prefabObject =>
                                {
                                    prefabObject.parentSelf = !prefabObject.parentSelf;
                                    RTLevel.Current?.UpdatePrefab(prefabObject);
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            #endregion

                            break;
                        }
                    case Tab.Properties: {
                            #region Detail Mode

                            new LabelsElement("Detail Mode").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                CreateAddSubButtons(
                                    timelineObject => true,
                                    timelineObject => timelineObject.isBeatmapObject ? (int)timelineObject.GetData<BeatmapObject>().detailMode : timelineObject.isBackgroundObject ? (int)timelineObject.GetData<BackgroundObject>().detailMode : (int)timelineObject.GetData<PrefabObject>().detailMode,
                                    EnumHelper.GetNames<DetailMode>().Length,
                                    (timelineObject, num) =>
                                    {
                                        switch (timelineObject.TimelineReference)
                                        {
                                            case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                                    beatmapObject.detailMode = (DetailMode)num;
                                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                                    break;
                                                }
                                            case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                    var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                                    backgroundObject.detailMode = (DetailMode)num;
                                                    RTLevel.Current?.UpdateBackgroundObject(backgroundObject);
                                                    break;
                                                }
                                            case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                    var prefabObject = timelineObject.GetData<PrefabObject>();
                                                    prefabObject.detailMode = (DetailMode)num;
                                                    RTLevel.Current?.UpdatePrefab(prefabObject);
                                                    break;
                                                }
                                        }
                                    }));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button(nameof(DetailMode.Normal), () => MultiObjectEditor.inst.SetDetailMode(DetailMode.Normal), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(RTString.SplitWords(nameof(DetailMode.HighDetail)), () => MultiObjectEditor.inst.SetDetailMode(DetailMode.HighDetail), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(RTString.SplitWords(nameof(DetailMode.LowDetail)), () => MultiObjectEditor.inst.SetDetailMode(DetailMode.LowDetail), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(RTString.SplitWords(nameof(DetailMode.NoDetail)), () => MultiObjectEditor.inst.SetDetailMode(DetailMode.NoDetail), labelAlignment: TextAnchor.MiddleCenter)
                                );

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Time

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
                                
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button("Snap BPM", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
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
                                })),
                                ButtonElement.Label1Button("Snap Offset BPM", () =>
                                {
                                    var selectedObjects = EditorTimeline.inst.SelectedObjects;
                                    var time = selectedObjects.Min(x => x.Time);
                                    var snappedTime = RTEditor.SnapToBPM(time);
                                    var distance = -time + snappedTime;
                                    foreach (var timelineObject in selectedObjects)
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

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Lock State")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Locked = true;
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Locked = false;
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Locked = !timelineObject.Locked;
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                }), labelAlignment: TextAnchor.MiddleCenter));

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
                                }), labelAlignment: TextAnchor.MiddleCenter),
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
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Last KF Offst", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
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
                                }), labelAlignment: TextAnchor.MiddleCenter),
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
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            ButtonElement.Label1Button("Set Autokill to Scaled 0x0", () => MultiObjectEditor.inst.ForEachBeatmapObject(timelineObject =>
                            {
                                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                beatmapObject.SetAutokillToScale(GameData.Current.beatmapObjects);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                timelineObject.RenderPosLength();
                            })).Init(EditorElement.InitSettings.Default.Parent(parent).Rect(RectValues.Default.SizeDelta(0f, 32f)));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Collapse State")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Collapse = true;
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Collapse = false;
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    timelineObject.Collapse = !timelineObject.Collapse;
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Parent

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
                                }), labelAlignment: TextAnchor.MiddleCenter),
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
                                }), labelAlignment: TextAnchor.MiddleCenter),
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
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Position Toggle")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentToggle(0, 1), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentToggle(0, 0), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentToggle(0, 2), labelAlignment: TextAnchor.MiddleCenter));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Scale Toggle")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentToggle(1, 1), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentToggle(1, 0), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentToggle(01, 2), labelAlignment: TextAnchor.MiddleCenter));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Rotation Toggle")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentToggle(2, 1), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentToggle(2, 0), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentToggle(2, 2), labelAlignment: TextAnchor.MiddleCenter));

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
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentAdditive(0, 1), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentAdditive(0, 0), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentAdditive(0, 2), labelAlignment: TextAnchor.MiddleCenter));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Scale Additive")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentAdditive(1, 1), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentAdditive(1, 0), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentAdditive(01, 2), labelAlignment: TextAnchor.MiddleCenter));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Rotation Additive")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.SetParentAdditive(2, 1), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.SetParentAdditive(2, 0), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.SetParentAdditive(2, 2), labelAlignment: TextAnchor.MiddleCenter));

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

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Object Type

                            new LabelsElement("Object Type").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                CreateAddSubButtons(
                                    timelineObject => timelineObject.isBeatmapObject,
                                    timelineObject => (int)timelineObject.GetData<BeatmapObject>().renderLayerType,
                                    EnumHelper.GetNames<BeatmapObject.ObjectType>().Length,
                                    (timelineObject, num) =>
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.objectType = (BeatmapObject.ObjectType)num;
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                    }));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button(nameof(BeatmapObject.ObjectType.Normal), () => MultiObjectEditor.inst.SetObjectType(BeatmapObject.ObjectType.Normal), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(BeatmapObject.ObjectType.Helper), () => MultiObjectEditor.inst.SetObjectType(BeatmapObject.ObjectType.Helper), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(BeatmapObject.ObjectType.Decoration), () => MultiObjectEditor.inst.SetObjectType(BeatmapObject.ObjectType.Decoration), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(BeatmapObject.ObjectType.Empty), () => MultiObjectEditor.inst.SetObjectType(BeatmapObject.ObjectType.Empty), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(BeatmapObject.ObjectType.Solid), () => MultiObjectEditor.inst.SetObjectType(BeatmapObject.ObjectType.Solid), labelAlignment: TextAnchor.MiddleCenter));

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Color Blend Mode

                            new LabelsElement("Color Blend Mode").Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                CreateAddSubButtons(
                                    timelineObject => timelineObject.isBeatmapObject,
                                    timelineObject => (int)timelineObject.GetData<BeatmapObject>().gradientType,
                                    EnumHelper.GetNames<ColorBlendMode>().Length,
                                    (timelineObject, num) =>
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.colorBlendMode = (ColorBlendMode)num;
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                    }));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button(nameof(ColorBlendMode.Normal), () => MultiObjectEditor.inst.SetColorBlendMode(ColorBlendMode.Normal), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(ColorBlendMode.Multiply), () => MultiObjectEditor.inst.SetColorBlendMode(ColorBlendMode.Multiply), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(ColorBlendMode.Additive), () => MultiObjectEditor.inst.SetColorBlendMode(ColorBlendMode.Additive), labelAlignment: TextAnchor.MiddleCenter));

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Gradient Type

                            new LabelsElement("Gradient Type").Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                CreateAddSubButtons(
                                    timelineObject => timelineObject.isBeatmapObject,
                                    timelineObject => (int)timelineObject.GetData<BeatmapObject>().gradientType,
                                    EnumHelper.GetNames<GradientType>().Length,
                                    (timelineObject, num) =>
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.gradientType = (GradientType)num;
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                    }));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button(nameof(GradientType.Normal), () => MultiObjectEditor.inst.SetGradientType(GradientType.Normal), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(GradientType.RightLinear), () => MultiObjectEditor.inst.SetGradientType(GradientType.RightLinear), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(GradientType.LeftLinear), () => MultiObjectEditor.inst.SetGradientType(GradientType.LeftLinear), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(GradientType.OutInRadial), () => MultiObjectEditor.inst.SetGradientType(GradientType.OutInRadial), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button(nameof(GradientType.InOutRadial), () => MultiObjectEditor.inst.SetGradientType(GradientType.InOutRadial), labelAlignment: TextAnchor.MiddleCenter));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Gradient Scale")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                {
                                    standardArrowFunctions = false,
                                    leftArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.gradientScale -= num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                            });
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.gradientScale = num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                            });
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.gradientScale += num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                            });
                                    },
                                })
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                });
                                
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Gradient Rotation")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerFloat()
                                {
                                    standardArrowFunctions = false,
                                    leftArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.gradientRotation -= num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                            });
                                    },
                                    middleClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.gradientRotation = num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                            });
                                    },
                                    rightArrowClicked = _val =>
                                    {
                                        if (float.TryParse(_val, out float num))
                                            MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                            {
                                                beatmapObject.gradientRotation += num;
                                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                            });
                                    },
                                })
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                });

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Shape

                            new LabelsElement("Shape").Init(EditorElement.InitSettings.Default.Parent(parent));
                            InitShape(parent);
                            RenderShape();

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LabelsElement("Images").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button("Store Images", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                {
                                    if (beatmapObject.ShapeType != ShapeType.Image)
                                        return;

                                    if (GameData.Current.assets.sprites.Has(x => x.name == beatmapObject.text))
                                        return;

                                    var regex = new Regex(@"img\((.*?)\)");
                                    var match = regex.Match(beatmapObject.text);

                                    var path = match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : RTFile.CombinePaths(RTFile.BasePath, beatmapObject.text);

                                    RTEditor.inst.StoreImage(beatmapObject, ObjectEditor.inst.Dialog, context => RTLevel.Current.UpdateObject(beatmapObject, context), path);
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.IMAGE);
                                })),
                                ButtonElement.Label1Button("Clear Images", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the images of all selected objects?", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                {
                                    if (beatmapObject.ShapeType != ShapeType.Image)
                                        return;

                                    beatmapObject.text = string.Empty;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.IMAGE);
                                }))));

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LabelsElement("Render Depth").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerInt()
                            {
                                standardArrowFunctions = false,
                                leftArrowClicked = _val =>
                                {
                                    if (int.TryParse(_val, out int num))
                                        MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                        {
                                            beatmapObject.Depth += num;
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
                                            beatmapObject.Depth -= num;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                        });
                                },
                            }).Init(EditorElement.InitSettings.Default.Parent(parent));

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Render Layer Type

                            new LabelsElement("Render Layer Type").Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                CreateAddSubButtons(
                                    timelineObject => timelineObject.isBeatmapObject,
                                    timelineObject => (int)timelineObject.GetData<BeatmapObject>().renderLayerType,
                                    EnumHelper.GetNames<BeatmapObject.RenderLayerType>().Length,
                                    (timelineObject, num) =>
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.renderLayerType = (BeatmapObject.RenderLayerType)num;
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                                    }));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ToButtonArray<BeatmapObject.RenderLayerType>(index => MultiObjectEditor.inst.SetRenderLayerType((BeatmapObject.RenderLayerType)index)));

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Opacity Collision State")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                {
                                    beatmapObject.opacityCollision = true;
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                {
                                    beatmapObject.opacityCollision = false;
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                {
                                    beatmapObject.opacityCollision = !beatmapObject.opacityCollision;
                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            #region Background Objects

                            #endregion

                            break;
                        }
                    case Tab.Modifiers: {
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

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Ignore Lifespan State")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (!timelineObject.TryGetData(out IModifyable modifyable))
                                        return;
                                    modifyable.IgnoreLifespan = true;
                                    switch (timelineObject.TimelineReference)
                                    {
                                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                    }
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (!timelineObject.TryGetData(out IModifyable modifyable))
                                        return;
                                    modifyable.IgnoreLifespan = false;
                                    switch (timelineObject.TimelineReference)
                                    {
                                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                    }
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (!timelineObject.TryGetData(out IModifyable modifyable))
                                        return;
                                    modifyable.IgnoreLifespan = !modifyable.IgnoreLifespan;
                                    switch (timelineObject.TimelineReference)
                                    {
                                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                    }
                                }), labelAlignment: TextAnchor.MiddleCenter));
                                
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Order Matters State")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (!timelineObject.TryGetData(out IModifyable modifyable))
                                        return;
                                    modifyable.OrderModifiers = true;
                                    switch (timelineObject.TimelineReference)
                                    {
                                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                    }
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (!timelineObject.TryGetData(out IModifyable modifyable))
                                        return;
                                    modifyable.OrderModifiers = false;
                                    switch (timelineObject.TimelineReference)
                                    {
                                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                    }
                                }), labelAlignment: TextAnchor.MiddleCenter),
                                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (!timelineObject.TryGetData(out IModifyable modifyable))
                                        return;
                                    modifyable.OrderModifiers = !modifyable.OrderModifiers;
                                    switch (timelineObject.TimelineReference)
                                    {
                                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                    }
                                }), labelAlignment: TextAnchor.MiddleCenter));

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            ButtonElement.Label1Button("Paste Modifiers", () =>
                            {
                                bool pasted = false;
                                MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                {
                                    if (!timelineObject.TryGetData(out IModifyable modifyable))
                                        return;

                                    var copiedModifiers = ModifiersEditor.inst.GetCopiedModifiers(modifyable.ReferenceType);
                                    if (copiedModifiers == null || copiedModifiers.IsEmpty())
                                        return;

                                    modifyable.Modifiers.AddRange(copiedModifiers.Select(x => x.Copy()));
                                    pasted = true;

                                    switch (timelineObject.TimelineReference)
                                    {
                                        case TimelineObject.TimelineReferenceType.BeatmapObject: {
                                                RTLevel.Current?.UpdateObject(timelineObject.GetData<BeatmapObject>(), ObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.BackgroundObject: {
                                                RTLevel.Current?.UpdateBackgroundObject(timelineObject.GetData<BackgroundObject>(), BackgroundObjectContext.MODIFIERS);
                                                break;
                                            }
                                        case TimelineObject.TimelineReferenceType.PrefabObject: {
                                                RTLevel.Current?.UpdatePrefab(timelineObject.GetData<PrefabObject>(), PrefabObjectContext.MODIFIERS);
                                                break;
                                            }
                                    }
                                });

                                if (pasted)
                                    EditorManager.inst.DisplayNotification("Pasted Modifier!", 1.5f, EditorManager.NotificationType.Success);
                                else
                                    EditorManager.inst.DisplayNotification($"No copied modifiers yet.", 3f, EditorManager.NotificationType.Error);
                            }, buttonThemeGroup: ThemeGroup.Paste, graphicThemeGroup: ThemeGroup.Paste_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced).Rect(RectValues.Default.SizeDelta(0f, 32f)));

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LabelsElement(new LabelElement("Clear") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
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

                            break;
                        }
                    case Tab.Keyframes: {
                            // todo: add creating keyframes for other types

                            #region Assign Colors

                            var checkmarkRect = RectValues.Default.SizeDelta(32f, 32f);
                            new LabelsElement(new LabelElement("Assign Colors") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Primary

                            new LabelsElement("Primary Color").Init(EditorElement.InitSettings.Default.Parent(parent));

                            var colorLayout = Creator.NewUIObject("color layout", parent);
                            colorLayout.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                            var colorLayoutGLG = colorLayout.AddComponent<GridLayoutGroup>();
                            colorLayoutGLG.spacing = new Vector2(4f, 4f);
                            colorLayoutGLG.cellSize = new Vector2(32f, 32f);

                            var disable = EditorPrefabHolder.Instance.Function2Button.Duplicate(colorLayout.transform, "disable color");
                            var disableX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(disable.transform, "x");
                            var disableXImage = disableX.GetComponent<Image>();
                            disableXImage.sprite = EditorSprites.CloseSprite;
                            checkmarkRect.AssignToRectTransform(disableXImage.rectTransform);
                            var disableButtonStorage = disable.GetComponent<FunctionButtonStorage>();
                            disableButtonStorage.OnClick.NewListener(() =>
                            {
                                disableX.gameObject.SetActive(true);
                                currentMultiColorSelection = -1;
                                UpdateMultiColorButtons();
                            });
                            CoreHelper.Delete(disableButtonStorage.label);
                            EditorThemeManager.ApplyGraphic(disableXImage, ThemeGroup.Function_2_Text);
                            EditorThemeManager.ApplySelectable(disableButtonStorage.button, ThemeGroup.Function_2);

                            for (int j = 0; j < 18; j++)
                            {
                                var colorSlot = j;
                                var colorGUI = EditorManager.inst.colorGUI.Duplicate(colorLayout.transform, (colorSlot + 1).ToString());
                                var assigner = colorGUI.AddComponent<AssignToTheme>();
                                assigner.Index = colorSlot;
                                var image = colorGUI.GetComponent<Image>();
                                assigner.Graphic = image;

                                var selected = colorGUI.transform.GetChild(0).gameObject;
                                EditorThemeManager.ApplyGraphic(selected.GetComponent<Image>(), ThemeGroup.Background_1);
                                selected.SetActive(false);

                                var button = colorGUI.GetComponent<Button>();
                                button.onClick.NewListener(() =>
                                {
                                    disableX.gameObject.SetActive(false);
                                    currentMultiColorSelection = colorSlot;
                                    UpdateMultiColorButtons();
                                });

                                multiColorButtons.Add(new MultiColorButton
                                {
                                    Button = button,
                                    Image = image,
                                    Selected = selected
                                });
                            }

                            var primaryOpacityField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());
                            var primaryHueField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());
                            var primarySaturationField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());
                            var primaryValueField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());

                            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Primary Opacity", "Primary Hue", "Primary Saturation", "Primary Value").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                primaryOpacityField,
                                primaryHueField,
                                primarySaturationField,
                                primaryValueField);

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Secondary

                            new LabelsElement("Secondary Color").Init(EditorElement.InitSettings.Default.Parent(parent));
                            var colorGradientLayout = Creator.NewUIObject("color layout", parent);
                            colorGradientLayout.transform.AsRT().sizeDelta = new Vector2(390f, 32f);
                            var colorGradientLayoutGLG = colorGradientLayout.AddComponent<GridLayoutGroup>();
                            colorGradientLayoutGLG.spacing = new Vector2(4f, 4f);
                            colorGradientLayoutGLG.cellSize = new Vector2(32f, 32f);

                            var disableGradient = EditorPrefabHolder.Instance.Function2Button.Duplicate(colorGradientLayout.transform, "disable color");
                            var disableGradientX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(disableGradient.transform, "x");
                            var disableGradientXImage = disableGradientX.GetComponent<Image>();
                            disableGradientXImage.sprite = EditorSprites.CloseSprite;
                            checkmarkRect.AssignToRectTransform(disableGradientXImage.rectTransform);
                            var disableGradientButtonStorage = disableGradient.GetComponent<FunctionButtonStorage>();
                            disableGradientButtonStorage.OnClick.NewListener(() =>
                            {
                                disableGradientX.gameObject.SetActive(true);
                                currentMultiGradientColorSelection = -1;
                                UpdateMultiColorButtons();
                            });
                            CoreHelper.Delete(disableGradientButtonStorage.label);
                            EditorThemeManager.ApplyGraphic(disableGradientXImage, ThemeGroup.Function_2_Text);
                            EditorThemeManager.ApplySelectable(disableGradientButtonStorage.button, ThemeGroup.Function_2);

                            for (int j = 0; j < 18; j++)
                            {
                                var colorSlot = j;
                                var colorGUI = EditorManager.inst.colorGUI.Duplicate(colorGradientLayout.transform, (colorSlot + 1).ToString());
                                var assigner = colorGUI.AddComponent<AssignToTheme>();
                                assigner.Index = colorSlot;
                                var image = colorGUI.GetComponent<Image>();
                                assigner.Graphic = image;

                                var selected = colorGUI.transform.GetChild(0).gameObject;
                                EditorThemeManager.ApplyGraphic(selected.GetComponent<Image>(), ThemeGroup.Background_1);
                                selected.SetActive(false);

                                var button = colorGUI.GetComponent<Button>();
                                button.onClick.NewListener(() =>
                                {
                                    disableGradientX.gameObject.SetActive(false);
                                    currentMultiGradientColorSelection = colorSlot;
                                    UpdateMultiColorButtons();
                                });

                                multiGradientColorButtons.Add(new MultiColorButton
                                {
                                    Button = button,
                                    Image = image,
                                    Selected = selected
                                });
                            }

                            var secondaryOpacityField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());
                            var secondaryHueField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());
                            var secondarySaturationField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());
                            var secondaryValueField = new NumberInputElement(string.Empty, null, new NumberInputElement.ArrowHandlerFloat());

                            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Secondary Opacity", "Secondary Hue", "Secondary Saturation", "Secondary Value").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                secondaryOpacityField,
                                secondaryHueField,
                                secondarySaturationField,
                                secondaryValueField);

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Ease Type

                            new LabelsElement("Ease Type").Init(EditorElement.InitSettings.Default.Parent(parent));
                            var easeTypeLayout = new LayoutGroupElement(LayoutGroupElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f));

                            var enableCurves = EditorPrefabHolder.Instance.Function2Button.Duplicate(easeTypeLayout.GameObject.transform, "disable curves");
                            enableCurves.GetOrAddComponent<LayoutElement>().minWidth = 200f;
                            var enableCurvesX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(enableCurves.transform, "x");
                            var enableCurvesXImage = enableCurvesX.GetComponent<Image>();
                            enableCurvesXImage.sprite = EditorSprites.CheckmarkSprite;
                            RectValues.Default.AnchorMax(0f, 0.5f).AnchorMin(0f, 0.5f).Pivot(0f, 0.5f).SizeDelta(32f, 32f).AssignToRectTransform(enableCurvesXImage.rectTransform);
                            var enableCurvesButtonStorage = enableCurves.GetComponent<FunctionButtonStorage>();
                            enableCurvesButtonStorage.OnClick.NewListener(() =>
                            {
                                setCurve = !setCurve;
                                enableCurvesX.gameObject.SetActive(setCurve);
                            });
                            enableCurvesX.gameObject.SetActive(setCurve);
                            enableCurvesButtonStorage.Text = "Set Easing";
                            EditorThemeManager.ApplyGraphic(enableCurvesXImage, ThemeGroup.Function_2_Text);
                            EditorThemeManager.ApplyGraphic(enableCurvesButtonStorage.label, ThemeGroup.Function_2_Text);
                            EditorThemeManager.ApplySelectable(enableCurvesButtonStorage.button, ThemeGroup.Function_2);

                            var curvesObject = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(easeTypeLayout.GameObject.transform, "curves");
                            var curves = curvesObject.GetComponent<Dropdown>();
                            RTEditor.inst.SetupEaseDropdown(curves);
                            curves.onValueChanged.ClearAll();
                            TriggerHelper.AddEventTriggers(curves.gameObject, TriggerHelper.ScrollDelta(curves));
                            EditorThemeManager.ApplyDropdown(curves);

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Set

                            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Assign to All Color Keyframes").Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button("Set", () =>
                                {
                                    var anim = RTEditor.inst.GetEasing(curves.value);
                                    MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        for (int i = 0; i < beatmapObject.events[3].Count; i++)
                                        {
                                            var kf = beatmapObject.events[3][i];
                                            if (setCurve)
                                                kf.curve = anim;
                                            if (currentMultiColorSelection >= 0)
                                                kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
                                            if (!string.IsNullOrEmpty(primaryOpacityField.numberInputField.Text))
                                                kf.values[1] = -Mathf.Clamp(Parser.TryParse(primaryOpacityField.numberInputField.Text, 1f), 0f, 1f) + 1f;
                                            if (!string.IsNullOrEmpty(primaryHueField.numberInputField.Text))
                                                kf.values[2] = Parser.TryParse(primaryHueField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(primarySaturationField.numberInputField.Text))
                                                kf.values[3] = Parser.TryParse(primarySaturationField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(primaryValueField.numberInputField.Text))
                                                kf.values[4] = Parser.TryParse(primaryValueField.numberInputField.Text, 0f);

                                            // Gradient
                                            if (currentMultiGradientColorSelection >= 0)
                                                kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
                                            if (!string.IsNullOrEmpty(secondaryOpacityField.numberInputField.Text))
                                                kf.values[6] = -Mathf.Clamp(Parser.TryParse(secondaryOpacityField.numberInputField.Text, 1f), 0f, 1f) + 1f;
                                            if (!string.IsNullOrEmpty(secondaryHueField.numberInputField.Text))
                                                kf.values[7] = Parser.TryParse(secondaryHueField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(secondarySaturationField.numberInputField.Text))
                                                kf.values[8] = Parser.TryParse(secondarySaturationField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(secondaryValueField.numberInputField.Text))
                                                kf.values[9] = Parser.TryParse(secondaryValueField.numberInputField.Text, 0f);
                                        }
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    });
                                }),
                                ButtonElement.Label1Button("Sub", () =>
                                {
                                    var anim = RTEditor.inst.GetEasing(curves.value);
                                    MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        for (int i = 0; i < beatmapObject.events[3].Count; i++)
                                        {
                                            var kf = beatmapObject.events[3][i];
                                            if (setCurve)
                                                kf.curve = anim;
                                            if (currentMultiColorSelection >= 0)
                                                kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18); // color slots can't be added onto.
                                            if (!string.IsNullOrEmpty(primaryOpacityField.numberInputField.Text))
                                                kf.values[1] = Mathf.Clamp(kf.values[1] + Parser.TryParse(primaryOpacityField.numberInputField.Text, 1f), 0f, 1f);
                                            if (!string.IsNullOrEmpty(primaryHueField.numberInputField.Text))
                                                kf.values[2] -= Parser.TryParse(primaryHueField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(primarySaturationField.numberInputField.Text))
                                                kf.values[3] -= Parser.TryParse(primarySaturationField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(primaryValueField.numberInputField.Text))
                                                kf.values[4] -= Parser.TryParse(primaryValueField.numberInputField.Text, 0f);

                                            // Gradient
                                            if (currentMultiGradientColorSelection >= 0)
                                                kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
                                            if (!string.IsNullOrEmpty(secondaryOpacityField.numberInputField.Text))
                                                kf.values[6] = Mathf.Clamp(kf.values[6] + Parser.TryParse(secondaryOpacityField.numberInputField.Text, 1f), 0f, 1f);
                                            if (!string.IsNullOrEmpty(secondaryHueField.numberInputField.Text))
                                                kf.values[7] -= Parser.TryParse(secondaryHueField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(secondarySaturationField.numberInputField.Text))
                                                kf.values[8] -= Parser.TryParse(secondarySaturationField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(secondaryValueField.numberInputField.Text))
                                                kf.values[9] -= Parser.TryParse(secondaryValueField.numberInputField.Text, 0f);
                                        }
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    });
                                }),
                                ButtonElement.Label1Button("Add", () =>
                                {
                                    var anim = RTEditor.inst.GetEasing(curves.value);
                                    MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        for (int i = 0; i < beatmapObject.events[3].Count; i++)
                                        {
                                            var kf = beatmapObject.events[3][i];
                                            if (setCurve)
                                                kf.curve = anim;
                                            if (currentMultiColorSelection >= 0)
                                                kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18); // color slots can't be added onto.
                                            if (!string.IsNullOrEmpty(primaryOpacityField.numberInputField.Text))
                                                kf.values[1] = Mathf.Clamp(kf.values[1] - Parser.TryParse(primaryOpacityField.numberInputField.Text, 1f), 0f, 1f);
                                            if (!string.IsNullOrEmpty(primaryHueField.numberInputField.Text))
                                                kf.values[2] += Parser.TryParse(primaryHueField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(primarySaturationField.numberInputField.Text))
                                                kf.values[3] += Parser.TryParse(primarySaturationField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(primaryValueField.numberInputField.Text))
                                                kf.values[4] += Parser.TryParse(primaryValueField.numberInputField.Text, 0f);

                                            // Gradient
                                            if (currentMultiGradientColorSelection >= 0)
                                                kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18); // color slots can't be added onto.
                                            if (!string.IsNullOrEmpty(secondaryOpacityField.numberInputField.Text))
                                                kf.values[6] = Mathf.Clamp(kf.values[6] - Parser.TryParse(secondaryOpacityField.numberInputField.Text, 1f), 0f, 1f);
                                            if (!string.IsNullOrEmpty(secondaryHueField.numberInputField.Text))
                                                kf.values[7] += Parser.TryParse(secondaryHueField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(secondarySaturationField.numberInputField.Text))
                                                kf.values[8] += Parser.TryParse(secondarySaturationField.numberInputField.Text, 0f);
                                            if (!string.IsNullOrEmpty(secondaryValueField.numberInputField.Text))
                                                kf.values[9] += Parser.TryParse(secondaryValueField.numberInputField.Text, 0f);
                                        }
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    });
                                }));

                            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Assign to Color Keyframes").Init(EditorElement.InitSettings.Default.Parent(parent));
                            var assignToIndexField = new StringInputElement("0", null, "Set the indexes you want to modify. e.g 0,1,2");
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Indexes")
                                {
                                    layoutElementValues = LayoutElementValues.Default.MinWidth(120),
                                },
                                assignToIndexField);
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                ButtonElement.Label1Button("Set", () =>
                                {
                                    var anim = RTEditor.inst.GetEasing(curves.value);
                                    ParseIterationIndex(assignToIndexField.inputField.text, index => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        SetKeyframeValues(beatmapObject.events[3][Mathf.Clamp(index, 0, beatmapObject.events[3].Count - 1)], setCurve, anim,
                                            primaryOpacityField.numberInputField.Text,
                                            primaryHueField.numberInputField.Text,
                                            primarySaturationField.numberInputField.Text,
                                            primaryValueField.numberInputField.Text,
                                            secondaryOpacityField.numberInputField.Text,
                                            secondaryHueField.numberInputField.Text,
                                            secondarySaturationField.numberInputField.Text,
                                            secondaryValueField.numberInputField.Text, MathOperation.Set);
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    }));
                                }),
                                ButtonElement.Label1Button("Sub", () =>
                                {
                                    var anim = RTEditor.inst.GetEasing(curves.value);
                                    ParseIterationIndex(assignToIndexField.inputField.text, index => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        SetKeyframeValues(beatmapObject.events[3][Mathf.Clamp(index, 0, beatmapObject.events[3].Count - 1)], setCurve, anim,
                                            primaryOpacityField.numberInputField.Text,
                                            primaryHueField.numberInputField.Text,
                                            primarySaturationField.numberInputField.Text,
                                            primaryValueField.numberInputField.Text,
                                            secondaryOpacityField.numberInputField.Text,
                                            secondaryHueField.numberInputField.Text,
                                            secondarySaturationField.numberInputField.Text,
                                            secondaryValueField.numberInputField.Text, MathOperation.Subtract);
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    }));
                                }),
                                ButtonElement.Label1Button("Add", () =>
                                {
                                    var anim = RTEditor.inst.GetEasing(curves.value);
                                    ParseIterationIndex(assignToIndexField.inputField.text, index => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        SetKeyframeValues(beatmapObject.events[3][Mathf.Clamp(index, 0, beatmapObject.events[3].Count - 1)], setCurve, anim,
                                            primaryOpacityField.numberInputField.Text,
                                            primaryHueField.numberInputField.Text,
                                            primarySaturationField.numberInputField.Text,
                                            primaryValueField.numberInputField.Text,
                                            secondaryOpacityField.numberInputField.Text,
                                            secondaryHueField.numberInputField.Text,
                                            secondarySaturationField.numberInputField.Text,
                                            secondaryValueField.numberInputField.Text, MathOperation.Addition);
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    }));
                                }));

                            ButtonElement.Label1Button("Create Color Keyframe", () =>
                            {
                                var anim = RTEditor.inst.GetEasing(curves.value);
                                MultiObjectEditor.inst.ForEachBeatmapObject(timelineObject =>
                                {
                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                    var currentTime = AudioManager.inst.CurrentAudioSource.time;
                                    if (currentTime < beatmapObject.StartTime) // don't want people creating keyframes before the objects' start time.
                                        return;

                                    var index = beatmapObject.events[3].FindLastIndex(x => currentTime > beatmapObject.StartTime + x.time);
                                    if (index < 0)
                                        return;

                                    var kf = beatmapObject.events[3][index].Copy();
                                    kf.time = currentTime - beatmapObject.StartTime;
                                    SetKeyframeValues(kf, setCurve, anim,
                                        primaryOpacityField.numberInputField.Text,
                                        primaryHueField.numberInputField.Text,
                                        primarySaturationField.numberInputField.Text,
                                        primaryValueField.numberInputField.Text,
                                        secondaryOpacityField.numberInputField.Text,
                                        secondaryHueField.numberInputField.Text,
                                        secondarySaturationField.numberInputField.Text,
                                        secondaryValueField.numberInputField.Text,
                                        MathOperation.Set);
                                    beatmapObject.events[3].Add(kf);

                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                });
                            }, buttonThemeGroup: ThemeGroup.Add, graphicThemeGroup: ThemeGroup.Add_Text).Init(EditorElement.InitSettings.Default.Rect(RectValues.Default.SizeDelta(0f, 32f)).Parent(parent));

                            #endregion

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Pasting

                            new LabelsElement(new LabelElement("Pasting") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LabelsElement(HorizontalOrVerticalLayoutValues.Horizontal.ChildControlHeight(false), "Paste Keyframes to Selected").Init(EditorElement.InitSettings.Default.Parent(parent));
                            ButtonElement.Label1Button("Paste Keyframes", EditorHelper.PasteKeyframes, buttonThemeGroup: ThemeGroup.Paste, graphicThemeGroup: ThemeGroup.Paste_Text).Init(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced).Rect(RectValues.Default.SizeDelta(0f, 32f)));

                            new LabelsElement("Repeat Paste Keyframes").Init(EditorElement.InitSettings.Default.Parent(parent));
                            var repeatCountElement = new NumberInputElement("1", null, new NumberInputElement.ArrowHandlerInt()
                            {
                                max = int.MaxValue,
                            });
                            var repeatOffsetTime = new NumberInputElement("0.1", null, new NumberInputElement.ArrowHandlerFloat());
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Normal), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Repeat Count")
                                {
                                    layoutElementValues = LayoutElementValues.Default.MinWidth(120f),
                                },
                                repeatCountElement,
                                new LabelElement("Repeat Offset Time")
                                {
                                    layoutElementValues = LayoutElementValues.Default.MinWidth(120f),
                                },
                                repeatOffsetTime,
                                ButtonElement.Label1Button("Repeat Paste Keyframes", () => EditorHelper.RepeatPasteKeyframes(Parser.TryParse(repeatCountElement.numberInputField.Text, 0), Parser.TryParse(repeatOffsetTime.numberInputField.Text, 0f)),
                                    buttonThemeGroup: ThemeGroup.Paste,
                                    graphicThemeGroup: ThemeGroup.Paste_Text,
                                    labelAlignment: TextAnchor.MiddleCenter,
                                    layoutElementValues: LayoutElementValues.Default.MinWidth(120f)));

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Keyframe Data

                            new LabelsElement(new LabelElement("Keyframe Data") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
                            new LabelsElement("Paste All Types").Init(EditorElement.InitSettings.Default.Parent(parent));
                            GeneratePasteKeyframeDataElements(parent,
                                () =>
                                {
                                    MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        for (int i = 0; i < beatmapObject.events.Count; i++)
                                        {
                                            var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(i);
                                            if (copiedKeyframeData == null)
                                                continue;

                                            for (int j = 0; j < beatmapObject.events[i].Count; j++)
                                            {
                                                var kf = beatmapObject.events[i][j];
                                                kf.curve = copiedKeyframeData.curve;
                                                kf.values = copiedKeyframeData.values.Copy();
                                                kf.randomValues = copiedKeyframeData.randomValues.Copy();
                                                kf.random = copiedKeyframeData.random;
                                                kf.relative = copiedKeyframeData.relative;
                                            }
                                        }
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    });
                                    EditorManager.inst.DisplayNotification("Pasted keyframe data to all keyframes.", 2f, EditorManager.NotificationType.Success);
                                },
                                _val =>
                                {
                                    ParseIterationIndex(_val, index => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        for (int i = 0; i < beatmapObject.events.Count; i++)
                                        {
                                            var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(i);
                                            if (copiedKeyframeData == null)
                                                continue;

                                            var kf = beatmapObject.events[i][Mathf.Clamp(index, 0, beatmapObject.events[i].Count - 1)];
                                            kf.curve = copiedKeyframeData.curve;
                                            kf.values = copiedKeyframeData.values.Copy();
                                            kf.randomValues = copiedKeyframeData.randomValues.Copy();
                                            kf.random = copiedKeyframeData.random;
                                            kf.relative = copiedKeyframeData.relative;
                                        }
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                    }));
                                    EditorManager.inst.DisplayNotification("Pasted keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                });
                            for (int j = 0; j < 4; j++)
                            {
                                int type = j;
                                string n = j switch
                                {
                                    0 => "Position",
                                    1 => "Scale",
                                    2 => "Rotation",
                                    3 => "Color",
                                    _ => "Null",
                                };
                                new LabelsElement($"Paste {n}").Init(EditorElement.InitSettings.Default.Parent(parent));
                                GeneratePasteKeyframeDataElements(parent,
                                    () =>
                                    {
                                        var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(type);
                                        if (copiedKeyframeData == null)
                                        {
                                            EditorManager.inst.DisplayNotification($"{n} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                        {
                                            for (int i = 0; i < beatmapObject.events[type].Count; i++)
                                            {
                                                var kf = beatmapObject.events[type][i];
                                                kf.curve = copiedKeyframeData.curve;
                                                kf.values = copiedKeyframeData.values.Copy();
                                                kf.randomValues = copiedKeyframeData.randomValues.Copy();
                                                kf.random = copiedKeyframeData.random;
                                                kf.relative = copiedKeyframeData.relative;
                                            }
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                        });
                                        EditorManager.inst.DisplayNotification($"Pasted {n.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                    },
                                    _val =>
                                    {
                                        var copiedKeyframeData = ObjectEditor.inst.Dialog.Timeline.GetCopiedData(type);
                                        if (!copiedKeyframeData)
                                        {
                                            EditorManager.inst.DisplayNotification($"{n} keyframe data not copied yet.", 2f, EditorManager.NotificationType.Error);
                                            return;
                                        }

                                        ParseIterationIndex(_val, index => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                        {
                                            var kf = beatmapObject.events[type][Mathf.Clamp(index, 0, beatmapObject.events[type].Count - 1)];
                                            kf.curve = copiedKeyframeData.curve;
                                            kf.values = copiedKeyframeData.values.Copy();
                                            kf.randomValues = copiedKeyframeData.randomValues.Copy();
                                            kf.random = copiedKeyframeData.random;
                                            kf.relative = copiedKeyframeData.relative;
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                        }));
                                        EditorManager.inst.DisplayNotification($"Pasted {n.ToLower()} keyframe data to current selected keyframe.", 2f, EditorManager.NotificationType.Success);
                                    });
                            }

                            #endregion

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            MultiKeyframeRelativeEdit(parent, "Position Relative", 0);
                            MultiKeyframeRelativeEdit(parent, "Scale Relative", 1);
                            MultiKeyframeRelativeEdit(parent, "Rotation Relative", 2);

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            #region Clear

                            new LabelsElement(new LabelElement("Clear") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
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

                            #endregion

                            break;
                        }
                    case Tab.Replace: {
                            new LabelsElement(new LabelElement("Name") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
                            var oldNameInput = new StringInputElement("name", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            var newNameInput = new StringInputElement("name", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Old Name")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                oldNameInput,
                                new LabelElement("New Name")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                newNameInput,
                                ButtonElement.Label1Button("Replace", () =>
                                {
                                    if (!oldNameInput.inputField || !newNameInput.inputField)
                                        return;

                                    MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
                                    {
                                        if (!timelineObject.isPrefabObject)
                                            timelineObject.SetName(timelineObject.Name.Replace(oldNameInput.inputField.text, newNameInput.inputField.text));
                                    });
                                }, labelAlignment: TextAnchor.MiddleCenter));

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LabelsElement(new LabelElement("Text") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
                            var oldTextInput = new StringInputElement("text", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            var newTextInput = new StringInputElement("text", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Old Text")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                oldTextInput,
                                new LabelElement("New Text")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                newTextInput,
                                ButtonElement.Label1Button("Replace", () =>
                                {
                                    if (!oldTextInput.inputField || !newTextInput.inputField)
                                        return;

                                    MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                                    {
                                        beatmapObject.text = beatmapObject.text.Replace(oldTextInput.inputField.text, newTextInput.inputField.text);
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.TEXT);
                                    });
                                }, labelAlignment: TextAnchor.MiddleCenter));

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LabelsElement(new LabelElement("Tag") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
                            var oldTagInput = new StringInputElement("object group", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            var newTagInput = new StringInputElement("object group", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Old Tag")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                oldTagInput,
                                new LabelElement("New Tag")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                newTagInput,
                                ButtonElement.Label1Button("Replace", () =>
                                {
                                    if (!oldTagInput.inputField || !newTagInput.inputField)
                                        return;

                                    MultiObjectEditor.inst.ForEachModifyable(modifyable =>
                                    {
                                        for (int i = 0; i < modifyable.Tags.Count; i++)
                                            modifyable.Tags[i] = modifyable.Tags[i].Replace(oldTagInput.inputField.text, newTagInput.inputField.text);
                                    });
                                }, labelAlignment: TextAnchor.MiddleCenter));

                            new SpacerElement().Init(EditorElement.InitSettings.Default.Parent(parent));

                            new LabelsElement(new LabelElement("Modifier Values") { fontSize = 22, fontStyle = FontStyle.Bold }).Init(EditorElement.InitSettings.Default.Parent(parent));
                            var oldModifierValueInput = new StringInputElement("value", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            var newModifierValueInput = new StringInputElement("value", null)
                            {
                                layoutElementValues = LayoutElementValues.Default.PreferredWidth(100f),
                                swapType = InputFieldSwapper.Type.String,
                            };
                            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                                new LabelElement("Old Modifier Value")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                oldModifierValueInput,
                                new LabelElement("New Modifier Value")
                                {
                                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                                },
                                newModifierValueInput,
                                ButtonElement.Label1Button("Replace", () =>
                                {
                                    if (!oldModifierValueInput.inputField || !newModifierValueInput.inputField)
                                        return;

                                    MultiObjectEditor.inst.ForEachModifyable(modifyable =>
                                    {
                                        for (int i = 0; i < modifyable.Modifiers.Count; i++)
                                        {
                                            var modifier = modifyable.Modifiers[i];
                                            for (int j = 0; j < modifier.values.Count; j++)
                                                modifier.values[j] = modifier.values[j].Replace(oldModifierValueInput.inputField.text, newModifierValueInput.inputField.text);
                                        }
                                    });
                                }, labelAlignment: TextAnchor.MiddleCenter));

                            break;
                        }
                    case Tab.Sync: {
                            GenerateSyncButtons(parent, "Start Time", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.StartTime = syncObject.StartTime;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.START_TIME);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            });
                            GenerateSyncButtons(parent, "Object Name", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.name = syncObject.name;
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            });
                            GenerateSyncButtons(parent, "Object Type", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.objectType = syncObject.objectType;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.OBJECT_TYPE);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            });
                            GenerateSyncButtons(parent, "Autokill Type", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.autoKillType = syncObject.autoKillType;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            });
                            GenerateSyncButtons(parent, "Autokill Offset", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.autoKillOffset = syncObject.autoKillOffset;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            });
                            GenerateSyncButtons(parent, "Parent", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.Parent = syncObject.parent;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                            });
                            GenerateSyncButtons(parent, "Parent Desync", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.desync = syncObject.desync;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                            });
                            GenerateSyncButtons(parent, "Parent Toggles", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.parentType = syncObject.parentType;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_SETTING);
                            });
                            GenerateSyncButtons(parent, "Parent Offsets (Delay)", (syncObject, beatmapObject) =>
                            {
                                for (int i = 0; i < syncObject.parentOffsets.Length; i++)
                                    beatmapObject.SetParentOffset(i, syncObject.GetParentOffset(i));
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_SETTING);
                            });
                            GenerateSyncButtons(parent, "Parent Additive", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.parentAdditive = syncObject.parentAdditive;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_SETTING);
                            });
                            GenerateSyncButtons(parent, "Parent Parallax", (syncObject, beatmapObject) =>
                            {
                                for (int i = 0; i < syncObject.parallaxSettings.Length; i++)
                                    beatmapObject.parallaxSettings[i] = syncObject.parallaxSettings[i];
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_SETTING);
                            });
                            GenerateSyncButtons(parent, "Origin", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.origin = syncObject.origin;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                            });
                            GenerateSyncButtons(parent, "Depth", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.Depth = syncObject.Depth;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                            });
                            GenerateSyncButtons(parent, "Render Layer Type", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.renderLayerType = syncObject.renderLayerType;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                            });
                            GenerateSyncButtons(parent, "Gradient Type", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.gradientType = syncObject.gradientType;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                            });
                            GenerateSyncButtons(parent, "Gradient Scale", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.gradientScale = syncObject.gradientScale;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                            });
                            GenerateSyncButtons(parent, "Gradient Rotation", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.gradientRotation = syncObject.gradientRotation;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                            });
                            GenerateSyncButtons(parent, "Shape", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.shape = syncObject.shape;
                                beatmapObject.shapeOption = syncObject.shapeOption;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
                            });
                            GenerateSyncButtons(parent, "Text", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.text = syncObject.text;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.TEXT);
                            });
                            GenerateSyncButtons(parent, "Polygon", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.polygonShape = syncObject.polygonShape.Copy();
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            });
                            GenerateSyncButtons(parent, "Keyframes", (syncObject, beatmapObject) =>
                            {
                                for (int i = 0; i < beatmapObject.TimelineKeyframes.Count; i++)
                                    CoreHelper.Delete(beatmapObject.TimelineKeyframes[i].GameObject);
                                beatmapObject.TimelineKeyframes.Clear();
                                beatmapObject.events.Clear();
                                for (int i = 0; i < syncObject.events.Count; i++)
                                    beatmapObject.events.Add(syncObject.events[i].Select(x => x.Copy()).ToList());

                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            });
                            GenerateSyncButtons(parent, "Modifiers List", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.Modifiers = new List<Modifier>(syncObject.Modifiers.Select(x => x.Copy()));
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                            });
                            GenerateSyncButtons(parent, "Modifiers Ignore Lifespan", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.ignoreLifespan = syncObject.ignoreLifespan;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                            });
                            GenerateSyncButtons(parent, "Modifiers Order Matters", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.orderModifiers = syncObject.orderModifiers;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                            });
                            GenerateSyncButtons(parent, "Tags", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.tags = new List<string>(syncObject.tags);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.MODIFIERS);
                            });
                            GenerateSyncButtons(parent, "Prefab", (syncObject, beatmapObject) =>
                            {
                                beatmapObject.SetPrefabReference(syncObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                            });

                            break;
                        }
                }
            }

            ActiveScrollView = ScrollViews[0];

            #endregion
        }

        void MultiKeyframeRelativeEdit(Transform parent, string label, int type)
        {
            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent).Complexity(Complexity.Advanced), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                new LabelElement(label)
                {
                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(200f),
                },
                ButtonElement.Label1Button("On", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                {
                    for (int i = 0; i < beatmapObject.events[type].Count; i++)
                        beatmapObject.events[type][i].relative = true;
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                }), labelAlignment: TextAnchor.MiddleCenter),
                ButtonElement.Label1Button("Off", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                {
                    for (int i = 0; i < beatmapObject.events[type].Count; i++)
                        beatmapObject.events[type][i].relative = false;
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                }), labelAlignment: TextAnchor.MiddleCenter),
                ButtonElement.Label1Button("Swap", () => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject =>
                {
                    for (int i = 0; i < beatmapObject.events[type].Count; i++)
                        beatmapObject.events[type][i].relative = !beatmapObject.events[type][i].relative;
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                }), labelAlignment: TextAnchor.MiddleCenter));
        }

        void GenerateSyncButtons(Transform parent, string name, Action<BeatmapObject, BeatmapObject> action)
        {
            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                new LabelElement(name)
                {
                    layoutElementValues = LayoutElementValues.Default.PreferredWidth(1000f),
                },
                new ButtonElement(ButtonElement.Type.Sprite, "Search List", () => ObjectEditor.inst.ShowObjectSearch(syncObject => MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject => action?.Invoke(syncObject, beatmapObject))))
                {
                    layoutElementValues = LayoutElementValues.Default.MinWidth(32f).PreferredWidth(32f),
                    buttonThemeGroup = ThemeGroup.Function_2,
                    sprite = EditorSprites.SearchSprite,
                },
                new ButtonElement(ButtonElement.Type.Icon, "Picker", () => EditorTimeline.inst.onSelectTimelineObject = timelineObject =>
                {
                    if (!timelineObject.isBeatmapObject)
                        return;
                    var syncObject = timelineObject.GetData<BeatmapObject>();
                    MultiObjectEditor.inst.ForEachBeatmapObject(beatmapObject => action?.Invoke(syncObject, beatmapObject));
                })
                {
                    layoutElementValues = LayoutElementValues.Default.MinWidth(32f).PreferredWidth(32f),
                    buttonThemeGroup = ThemeGroup.Picker,
                    sprite = EditorSprites.DropperSprite,
                });
        }

        void ParseIterationIndex(string _val, Action<int> action)
        {
            if (string.IsNullOrEmpty(_val))
                return;

            if (_val.Contains(","))
            {
                var split = _val.Split(",");
                for (int i = 0; i < split.Length; i++)
                {
                    if (int.TryParse(split[i], out int splitIndex))
                        action?.Invoke(splitIndex);
                }
                return;
            }

            if (int.TryParse(_val, out int index))
                action?.Invoke(index);
        }

        EditorElement[] CreateAddSubButtons(Predicate<TimelineObject> predicate, Func<TimelineObject, int> get, int max, Action<TimelineObject, int> set) => new EditorElement[2]
        {
            ButtonElement.Label1Button("Sub", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
            {
                if (!predicate.Invoke(timelineObject))
                    return;

                var num = get.Invoke(timelineObject);
                num--;
                if (num < 0)
                    num = max - 1;
                set.Invoke(timelineObject, num);
            }), labelAlignment: TextAnchor.MiddleCenter),
            ButtonElement.Label1Button("Add", () => MultiObjectEditor.inst.ForEachTimelineObject(timelineObject =>
            {
                if (!predicate.Invoke(timelineObject))
                    return;

                var num = get.Invoke(timelineObject);
                num++;
                if (num >= max)
                    num = 0;
                set.Invoke(timelineObject, num);
            }), labelAlignment: TextAnchor.MiddleCenter)
        };

        EditorElement[] ToButtonArray<T>(Action<int> onClick) where T : Enum
        {
            var enumNames = EnumHelper.GetNames<T>();
            var elements = new EditorElement[enumNames.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                var index = i;
                elements[i] = ButtonElement.Label1Button(enumNames[i], () => onClick?.Invoke(index), labelAlignment: TextAnchor.MiddleCenter);
            }
            return elements;
        }

        void GeneratePasteKeyframeDataElements(Transform parent, Action pasteToAll, Action<string> pasteToIndexes)
        {
            var indexField = new StringInputElement("0", null, "Set the indexes you want to modify. e.g 0,1,2");
            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                new LabelElement("Indexes")
                {
                    layoutElementValues = LayoutElementValues.Default.MinWidth(120),
                },
                indexField);
            new LayoutGroupElement(EditorElement.InitSettings.Default.Parent(parent), HorizontalOrVerticalLayoutValues.Horizontal.Spacing(4f),
                ButtonElement.Label1Button("Paste to All", pasteToAll, buttonThemeGroup: ThemeGroup.Paste, graphicThemeGroup: ThemeGroup.Paste_Text),
                ButtonElement.Label1Button("Paste to Indexes", () => pasteToIndexes?.Invoke(indexField.inputField.text), buttonThemeGroup: ThemeGroup.Paste, graphicThemeGroup: ThemeGroup.Paste_Text));
        }

        void InitShape(Transform parent)
        {
            if (!multiShapes)
            {
                var shapes = ObjEditor.inst.ObjectView.transform.Find("shape").gameObject.Duplicate(parent, "shape");
                var shapeOption = ObjEditor.inst.ObjectView.transform.Find("shapesettings").gameObject.Duplicate(parent, "shapesettings");
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

                            #region Thickness Angle

                            var thicknessRotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "thickness angle");
                            var thicknessRotationStorage = thicknessRotation.GetComponent<InputFieldStorage>();

                            CoreHelper.Delete(thicknessRotationStorage.addButton);
                            CoreHelper.Delete(thicknessRotationStorage.subButton);
                            CoreHelper.Delete(thicknessRotationStorage.leftGreaterButton);
                            CoreHelper.Delete(thicknessRotationStorage.middleButton);
                            CoreHelper.Delete(thicknessRotationStorage.rightGreaterButton);

                            EditorThemeManager.ApplyInputField(thicknessRotationStorage);

                            var thicknessRotationLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessRotation.transform, "label", 0);
                            var thicknessRotationLabelText = thicknessRotationLabel.GetComponent<Text>();
                            thicknessRotationLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessRotationLabelText.text = "Thick Angle";
                            thicknessRotationLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.ApplyLightText(thicknessRotationLabelText);
                            var thicknessRotationLabelLayout = thicknessRotationLabel.AddComponent<LayoutElement>();
                            thicknessRotationLabelLayout.minWidth = 100f;

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
            var shape = multiShapes;
            var shapeSettings = multiShapeSettings;

            LSHelpers.SetActiveChildren(shapeSettings, false);

            shapeSettings.AsRT().sizeDelta = new Vector2(351f, shapeSelection.x == 4 ? 74f : 32f);
            shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, shapeSelection.x == 4 ? 74f : 32f);

            shapeSettings.GetChild(shapeSelection.x).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in shapeToggles)
            {
                int index = num;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);
                toggle.SetIsOnWithoutNotify(shapeSelection.x == index);
                toggle.onValueChanged.NewListener(_val =>
                {
                    shapeSelection = new Vector2Int(index, 0);

                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBeatmapObject)
                        {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            beatmapObject.Shape = shapeSelection.x;
                            beatmapObject.ShapeOption = shapeSelection.y;

                            if (beatmapObject.gradientType != GradientType.Normal && (index == 4 || index == 6 || index == 10))
                                beatmapObject.Shape = 0;

                            if (beatmapObject.ShapeType == ShapeType.Polygon && EditorConfig.Instance.AutoPolygonRadius.Value)
                                beatmapObject.polygonShape.Radius = beatmapObject.polygonShape.GetAutoRadius();

                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
                        }
                        if (timelineObject.isBackgroundObject)
                        {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                            backgroundObject.Shape = shapeSelection.x;
                            backgroundObject.ShapeOption = shapeSelection.y;

                            RTLevel.Current?.UpdateBackgroundObject(backgroundObject, recalculate: false);
                        }
                    }

                    RTLevel.Current?.RecalculateObjectStates();
                    RenderShape();
                });

                num++;
            }

            switch ((ShapeType)shapeSelection.x)
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
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.IMAGE);
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
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 320f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 320f);

                        var radius = shapeSettings.Find("10/radius").gameObject.GetComponent<InputFieldStorage>();
                        radius.OnValueChanged.NewListener(_val =>
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
                        sides.OnValueChanged.NewListener(_val =>
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
                        roundness.OnValueChanged.NewListener(_val =>
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
                        thickness.OnValueChanged.NewListener(_val =>
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
                        thicknessOffsetX.OnValueChanged.NewListener(_val =>
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
                        thicknessOffsetY.OnValueChanged.NewListener(_val =>
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
                        thicknessScaleX.OnValueChanged.NewListener(_val =>
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
                        thicknessScaleY.OnValueChanged.NewListener(_val =>
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

                        var thicknessRotation = shapeSettings.Find("10/thickness angle").gameObject.GetComponent<InputFieldStorage>();
                        thicknessRotation.OnValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
                                {
                                    beatmapObject.polygonShape.ThicknessRotation = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessRotation, 15f, 3f);
                        TriggerHelper.AddEventTriggers(thicknessRotation.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessRotation.inputField, 15f, 3f));

                        var slices = shapeSettings.Find("10/slices").gameObject.GetComponent<InputFieldStorage>();
                        slices.OnValueChanged.NewListener(_val =>
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
                        rotation.OnValueChanged.NewListener(_val =>
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
                        foreach (var toggle in shapeOptionToggles[shapeSelection.x])
                        {
                            int index = num;
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[shapeSelection.x]);
                            toggle.SetIsOnWithoutNotify(shapeSelection.y == index);
                            toggle.onValueChanged.NewListener(_val =>
                            {
                                shapeSelection.y = index;

                                foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                                {
                                    if (timelineObject.isBeatmapObject)
                                    {
                                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                                        beatmapObject.Shape = shapeSelection.x;
                                        beatmapObject.ShapeOption = shapeSelection.y;

                                        if (beatmapObject.gradientType != GradientType.Normal && (index == 4 || index == 6 || index == 10))
                                            beatmapObject.Shape = 0;

                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
                                    }
                                    if (timelineObject.isBackgroundObject)
                                    {
                                        var backgroundObject = timelineObject.GetData<BackgroundObject>();
                                        backgroundObject.Shape = shapeSelection.x;
                                        backgroundObject.ShapeOption = shapeSelection.y;

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

        public void SetKeyframeValues(EventKeyframe kf, bool setCurves, Easing anim,
            string primaryOpacity, string primaryHue, string primarySaturation, string primaryValue,
            string secondaryOpacity, string secondaryHue, string secondarySaturation, string secondaryValue, MathOperation operation)
        {
            if (setCurves)
                kf.curve = anim;
            if (currentMultiColorSelection >= 0)
                kf.values[0] = Mathf.Clamp(currentMultiColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(primaryOpacity))
            {
                switch (operation)
                {
                    case MathOperation.Addition: {
                            kf.values[1] = Mathf.Clamp(kf.values[1] - Parser.TryParse(primaryOpacity, 1f), 0f, 1f);
                            break;
                        }
                    case MathOperation.Subtract: {
                            kf.values[1] = Mathf.Clamp(kf.values[1] + Parser.TryParse(primaryOpacity, 1f), 0f, 1f);
                            break;
                        }
                    case MathOperation.Set: {
                            kf.values[1] = -Mathf.Clamp(Parser.TryParse(primaryOpacity, 1f), 0f, 1f) + 1f;
                            break;
                        }
                }
            }
            if (!string.IsNullOrEmpty(primaryHue))
                RTMath.Operation(ref kf.values[2], Parser.TryParse(primaryHue, 0f), operation);
            if (!string.IsNullOrEmpty(primarySaturation))
                RTMath.Operation(ref kf.values[3], Parser.TryParse(primarySaturation, 0f), operation);
            if (!string.IsNullOrEmpty(primaryValue))
                RTMath.Operation(ref kf.values[4], Parser.TryParse(primaryValue, 0f), operation);

            // Gradient
            if (currentMultiGradientColorSelection >= 0)
                kf.values[5] = Mathf.Clamp(currentMultiGradientColorSelection, 0, 18);
            if (!string.IsNullOrEmpty(secondaryOpacity))
            {
                switch (operation)
                {
                    case MathOperation.Addition: {
                            kf.values[6] = Mathf.Clamp(kf.values[6] - Parser.TryParse(secondaryOpacity, 1f), 0f, 1f);
                            break;
                        }
                    case MathOperation.Subtract: {
                            kf.values[6] = Mathf.Clamp(kf.values[6] + Parser.TryParse(secondaryOpacity, 1f), 0f, 1f);
                            break;
                        }
                    case MathOperation.Set: {
                            kf.values[6] = -Mathf.Clamp(Parser.TryParse(secondaryOpacity, 1f), 0f, 1f) + 1f;
                            break;
                        }
                }
            }
            if (!string.IsNullOrEmpty(secondaryHue))
                RTMath.Operation(ref kf.values[7], Parser.TryParse(secondaryHue, 0f), operation);
            if (!string.IsNullOrEmpty(secondarySaturation))
                RTMath.Operation(ref kf.values[8], Parser.TryParse(secondarySaturation, 0f), operation);
            if (!string.IsNullOrEmpty(secondaryValue))
                RTMath.Operation(ref kf.values[9], Parser.TryParse(secondaryValue, 0f), operation);
        }

        void UpdateMultiColorButtons()
        {
            for (int i = 0; i < multiColorButtons.Count; i++)
                multiColorButtons[i].Selected.SetActive(currentMultiColorSelection == i);

            for (int i = 0; i < multiGradientColorButtons.Count; i++)
                multiGradientColorButtons[i].Selected.SetActive(currentMultiGradientColorSelection == i);
        }

        #endregion

        /// <summary>
        /// Represents a color button in the keyframe tab.
        /// </summary>
        public class MultiColorButton
        {
            /// <summary>
            /// Button of the color button.
            /// </summary>
            public Button Button { get; set; }

            /// <summary>
            /// Image of the color button.
            /// </summary>
            public Image Image { get; set; }

            /// <summary>
            /// Selected display of the color button.
            /// </summary>
            public GameObject Selected { get; set; }
        }
    }
}

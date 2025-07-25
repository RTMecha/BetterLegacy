﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    public class MultiObjectEditor : MonoBehaviour
    {
        #region Init

        public static MultiObjectEditor inst;

        public static void Init() => Creator.NewGameObject(nameof(MultiObjectEditor), EditorManager.inst.transform.parent).AddComponent<MultiObjectEditor>();

        void Awake()
        {
            inst = this;
            GenerateUI();

            try
            {
                Dialog = new EditorDialog(EditorDialog.MULTI_OBJECT_EDITOR);
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        /// <summary>
        /// Text to update.
        /// </summary>
        public Text Text { get; set; }

        public EditorDialog Dialog { get; set; }

        /// <summary>
        /// String to format from.
        /// </summary>
        public const string DEFAULT_TEXT = "You are currently editing multiple objects.\n\nObject Count: {0}/{3}\nBG Count: {5}/{6}\nPrefab Object Count: {1}/{4}\nTotal: {2}";

        void Update()
        {
            if (!Text || !Text.isActiveAndEnabled || !GameData.Current)
                return;

            Text.text = string.Format(DEFAULT_TEXT,
                EditorTimeline.inst.SelectedBeatmapObjects.Count,
                EditorTimeline.inst.SelectedPrefabObjects.Count,
                EditorTimeline.inst.SelectedObjects.Count,
                GameData.Current.beatmapObjects.Count,
                GameData.Current.prefabObjects.Count,
                EditorTimeline.inst.SelectedBackgroundObjects.Count,
                GameData.Current.backgroundObjects.Count);
        }

        #endregion

        #region Values

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

        #endregion

        #region Methods

        void GenerateUI()
        {
            var multiObjectEditorDialog = EditorManager.inst.GetDialog("Multi Object Editor").Dialog;

            EditorThemeManager.AddGraphic(multiObjectEditorDialog.GetComponent<Image>(), ThemeGroup.Background_1);

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

            EditorThemeManager.AddLightText(textHolderText);

            Text = textHolderText;

            textHolderText.fontSize = 22;

            textHolder.AsRT().anchoredPosition = new Vector2(0f, -125f);

            textHolder.AsRT().sizeDelta = new Vector2(-68f, 0f);

            Destroy(dataLeft.GetComponent<VerticalLayoutGroup>());

            GenerateLabels(parent, 32f, new Label("- Main Properties -", 22, FontStyle.Bold, TextAnchor.MiddleCenter));
            // Layers
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
                inputFieldStorage.rightGreaterButton.image.sprite = SpriteHelper.LoadSprite(RTFile.GetAsset($"editor_gui_down{FileFormat.PNG.Dot()}"));
                inputFieldStorage.rightGreaterButton.onClick.NewListener(() =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                        timelineObject.Layer = EditorTimeline.inst.Layer;
                });
                TriggerHelper.AddEventTriggers(inputFieldStorage.inputField.gameObject, TriggerHelper.ScrollDeltaInt(inputFieldStorage.inputField));

                EditorHelper.SetComplexity(inputFieldStorage.leftGreaterButton.gameObject, Complexity.Normal);
            }

            // Depth
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

            // Song Time
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

            // Autokill Offset
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

            // Name
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
                ((Text)inputFieldStorage.inputField.placeholder).text = "Enter name...";

                EditorThemeManager.AddInputField(inputFieldStorage.inputField);

                Destroy(inputFieldStorage.leftGreaterButton.gameObject);
                Destroy(inputFieldStorage.leftButton.gameObject);
                Destroy(inputFieldStorage.rightGreaterButton.gameObject);

                inputFieldStorage.middleButton.onClick.ClearAll();
                inputFieldStorage.middleButton.onClick.AddListener(() =>
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

                EditorThemeManager.AddSelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);

                inputFieldStorage.rightButton.name = "+";

                inputFieldStorage.rightButton.image.sprite = EditorSprites.AddSprite;

                var mtnLeftLE = inputFieldStorage.rightButton.gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                inputFieldStorage.rightButton.transform.AsRT().anchoredPosition = new Vector2(339f, 0f);
                inputFieldStorage.rightButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                inputFieldStorage.rightButton.onClick.ClearAll();
                inputFieldStorage.rightButton.onClick.AddListener(() =>
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

                EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);
            }

            // Tags
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

                EditorThemeManager.AddInputField(inputFieldStorage.inputField);

                Destroy(inputFieldStorage.leftGreaterButton.gameObject);
                Destroy(inputFieldStorage.leftButton.gameObject);
                Destroy(inputFieldStorage.middleButton.gameObject);
                Destroy(inputFieldStorage.rightGreaterButton.gameObject);

                inputFieldStorage.rightButton.name = "+";

                inputFieldStorage.rightButton.image.sprite = EditorSprites.AddSprite;

                var mtnLeftLE = inputFieldStorage.rightButton.gameObject.AddComponent<LayoutElement>();
                mtnLeftLE.ignoreLayout = true;

                inputFieldStorage.rightButton.transform.AsRT().anchoredPosition = new Vector2(339f, 0f);
                inputFieldStorage.rightButton.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

                inputFieldStorage.rightButton.onClick.NewListener(() =>
                {
                    foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
                    {
                        if (timelineObject.isBeatmapObject)
                            timelineObject.GetData<BeatmapObject>().tags.Add(inputFieldStorage.inputField.text);
                        if (timelineObject.isBackgroundObject)
                            timelineObject.GetData<BackgroundObject>().tags.Add(inputFieldStorage.inputField.text);
                    }
                });

                EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);

                EditorHelper.SetComplexity(labels, Complexity.Advanced);
                EditorHelper.SetComplexity(multiNameSet, Complexity.Advanced);
            }

            // Timeline Object Index
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

            // Editor Colors
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

            // Clear data
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

                             RTEditor.inst.HideWarningPopup();
                         }, RTEditor.inst.HideWarningPopup);
                     }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text) { FontSize = 16 },
                     new ButtonFunction("Clear anims", () =>
                     {
                         RTEditor.inst.ShowWarningPopup("You are about to clear animations from all selected objects, this <b>CANNOT</b> be undone!", () =>
                         {
                             foreach (var timelineObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject))
                             {
                                 var bm = timelineObject.GetData<BeatmapObject>();
                                 foreach (var tkf in timelineObject.InternalTimelineObjects)
                                     Destroy(tkf.GameObject);
                                 timelineObject.InternalTimelineObjects.Clear();
                                 for (int i = 0; i < bm.events.Count; i++)
                                 {
                                     bm.events[i] = bm.events[i].OrderBy(x => x.time).ToList();
                                     var firstKF = bm.events[i][0].Copy(false);
                                     bm.events[i].Clear();
                                     bm.events[i].Add(firstKF);
                                 }
                                 if (EditorTimeline.inst.SelectedObjects.Count == 1)
                                 {
                                     ObjectEditor.inst.ResizeKeyframeTimeline(bm);
                                     ObjectEditor.inst.RenderKeyframes(bm);
                                 }

                                 RTLevel.Current?.UpdateObject(bm, ObjectContext.KEYFRAMES);
                                 EditorTimeline.inst.RenderTimelineObject(timelineObject);
                             }

                             RTEditor.inst.HideWarningPopup();
                         }, RTEditor.inst.HideWarningPopup);
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

                             RTEditor.inst.HideWarningPopup();
                         }, RTEditor.inst.HideWarningPopup);
                     }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text) { FontSize = 16 });

                EditorHelper.SetComplexity(labels, Complexity.Normal);
                EditorHelper.SetComplexity(buttons1, Complexity.Normal);
            }

            // Optimization
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

            // Song Time Autokill
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

            // Autokill Type
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

            // Set Parent
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
                                beatmapObject.Parent = "";
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                            }

                            RTEditor.inst.HideWarningPopup();
                        }, RTEditor.inst.HideWarningPopup);
                    }, buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));
            }

            // Parent Desync
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
                var buttons1 = GenerateButtons(parent, 32f, 8f, new ButtonFunction("Snap", () =>
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
                }), new ButtonFunction("Snap Offset", () =>
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
                RenderMultiShape();
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
                        RTEditor.inst.HideWarningPopup();
                    }, RTEditor.inst.HideWarningPopup), buttonThemeGroup: ThemeGroup.Delete, labelThemeGroup: ThemeGroup.Delete_Text));

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
                        RTEditor.inst.HideWarningPopup();
                        EditorManager.inst.DisplayNotification("Successfully created a new instance ID.", 2f, EditorManager.NotificationType.Success);
                    }, RTEditor.inst.HideWarningPopup)));

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
                        for (int i = 1; i < modifier.commands.Count; i++)
                            modifier.commands[i] = modifier.commands[i].Replace(oldNameIF.text, newNameIF.text);

                        modifier.value = modifier.value.Replace(oldNameIF.text, newNameIF.text);
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
                disableButtonStorage.button.onClick.ClearAll();
                disableButtonStorage.button.onClick.AddListener(() =>
                {
                    disableX.gameObject.SetActive(true);
                    currentMultiColorSelection = -1;
                    UpdateMultiColorButtons();
                });
                disableButtonStorage.label.text = "Don't set color";
                EditorThemeManager.AddGraphic(disableXImage, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddGraphic(disableButtonStorage.label, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddSelectable(disableButtonStorage.button, ThemeGroup.Function_2);

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
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() =>
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

                var opacityIF = CreateInputField("opacity", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)opacityIF.placeholder).fontSize = 13;

                var labels3 = GenerateLabels(parent, 32f, "Primary Hue");

                var hueIF = CreateInputField("hue", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)hueIF.placeholder).fontSize = 13;

                var labels4 = GenerateLabels(parent, 32f, "Primary Saturation");

                var satIF = CreateInputField("sat", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)satIF.placeholder).fontSize = 13;

                var labels5 = GenerateLabels(parent, 32f, "Primary Value (Brightness)");

                var valIF = CreateInputField("val", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)valIF.placeholder).fontSize = 13;

                var labelsSecondaryColor = GenerateLabels(parent, 32f, "Secondary Color");

                var disableGradient = EditorPrefabHolder.Instance.Function2Button.Duplicate(parent, "disable color");
                var disableGradientX = EditorManager.inst.colorGUI.transform.Find("Image").gameObject.Duplicate(disableGradient.transform, "x");
                var disableGradientXImage = disableGradientX.GetComponent<Image>();
                disableGradientXImage.sprite = EditorSprites.CloseSprite;
                RectValues.Default.AnchoredPosition(-170f, 0f).SizeDelta(32f, 32f).AssignToRectTransform(disableGradientXImage.rectTransform);
                var disableGradientButtonStorage = disableGradient.GetComponent<FunctionButtonStorage>();
                disableGradientButtonStorage.button.onClick.ClearAll();
                disableGradientButtonStorage.button.onClick.AddListener(() =>
                {
                    disableGradientX.gameObject.SetActive(true);
                    currentMultiGradientColorSelection = -1;
                    UpdateMultiColorButtons();
                });
                disableGradientButtonStorage.label.text = "Don't set color";
                EditorThemeManager.AddGraphic(disableGradientXImage, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddGraphic(disableGradientButtonStorage.label, ThemeGroup.Function_2_Text);
                EditorThemeManager.AddSelectable(disableGradientButtonStorage.button, ThemeGroup.Function_2);

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
                    button.onClick.ClearAll();
                    button.onClick.AddListener(() =>
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

                var opacityGradientIF = CreateInputField("opacity", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)opacityGradientIF.placeholder).fontSize = 13;

                var labels7 = GenerateLabels(parent, 32f, "Secondary Hue");

                var hueGradientIF = CreateInputField("hue", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)hueGradientIF.placeholder).fontSize = 13;

                var labels8 = GenerateLabels(parent, 32f, "Secondary Saturation");

                var satGradientIF = CreateInputField("sat", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)satGradientIF.placeholder).fontSize = 13;

                var labels9 = GenerateLabels(parent, 32f, "Secondary Value (Brightness)");

                var valGradientIF = CreateInputField("val", "", "Enter value... (Keep empty to not set)", parent, isInteger: false);
                ((Text)valGradientIF.placeholder).fontSize = 13;

                var labels10 = GenerateLabels(parent, 32f, "Ease Type");

                var curvesObject = EditorPrefabHolder.Instance.CurvesDropdown.Duplicate(parent, "curves");
                var curves = curvesObject.GetComponent<Dropdown>();
                curves.onValueChanged.ClearAll();
                curves.options.Insert(0, new Dropdown.OptionData("None (Doesn't Set Easing)"));

                TriggerHelper.AddEventTriggers(curves.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, baseEventData =>
                {
                    if (!EditorConfig.Instance.ScrollOnEasing.Value)
                        return;

                    var pointerEventData = (PointerEventData)baseEventData;
                    if (pointerEventData.scrollDelta.y > 0f)
                        curves.value = curves.value == 0 ? curves.options.Count - 1 : curves.value - 1;
                    if (pointerEventData.scrollDelta.y < 0f)
                        curves.value = curves.value == curves.options.Count - 1 ? 0 : curves.value + 1;
                }));

                EditorThemeManager.AddDropdown(curves);

                // Assign to All
                {
                    var labels = GenerateLabels(parent, 32f, "Assign to all Color Keyframes");
                    var buttons1 = GenerateButtons(parent, 32f, 8f,
                        new ButtonFunction("Set", () =>
                        {
                            Easing anim = (Easing)(curves.value - 1);
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
                            Easing anim = (Easing)(curves.value - 1);
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
                            Easing anim = (Easing)(curves.value - 1);
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
                                    kf.curve = (Easing)(curves.value - 1);

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
                                var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(i);
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
                                    var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(i);
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
        }

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        public void RenderMultiShape()
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
                DestroyImmediate(shape.GetComponent<ToggleGroup>());

                var toDestroy = new List<GameObject>();

                for (int i = 0; i < shape.childCount; i++)
                {
                    toDestroy.Add(shape.GetChild(i).gameObject);
                }

                for (int i = 0; i < shapeSettings.childCount; i++)
                {
                    if (i != 4 && i != 6)
                        for (int j = 0; j < shapeSettings.GetChild(i).childCount; j++)
                            toDestroy.Add(shapeSettings.GetChild(i).GetChild(j).gameObject);
                }

                foreach (var obj in toDestroy)
                    DestroyImmediate(obj);

                toDestroy = null;

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
                            LSHelpers.DeleteChildren(sh.transform, true);

                            var d = new List<GameObject>();
                            for (int j = 0; j < sh.transform.childCount; j++)
                            {
                                d.Add(sh.transform.GetChild(j).gameObject);
                            }
                            foreach (var go in d)
                                DestroyImmediate(go);
                            d.Clear();
                            d = null;
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

                            ((RectTransform)opt.transform).sizeDelta = new Vector2(32f, 32f);

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

                        var rect = so.AsRT();
                        DestroyImmediate(so.GetComponent<ScrollRect>());
                        DestroyImmediate(so.GetComponent<HorizontalLayoutGroup>());
                        DestroyImmediate(so.GetComponent<VerticalLayoutGroup>());

                        so.gameObject.GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 0.05f);

                        var verticalLayoutGroup = so.gameObject.AddComponent<VerticalLayoutGroup>();
                        verticalLayoutGroup.spacing = 4f;

                        // Polygon Settings
                        {
                            #region Radius

                            var radius = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "radius");
                            var radiusStorage = radius.GetComponent<InputFieldStorage>();

                            Destroy(radiusStorage.addButton.gameObject);
                            Destroy(radiusStorage.subButton.gameObject);
                            Destroy(radiusStorage.leftGreaterButton.gameObject);
                            Destroy(radiusStorage.middleButton.gameObject);
                            Destroy(radiusStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(radiusStorage.inputField);
                            EditorThemeManager.AddSelectable(radiusStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(radiusStorage.rightButton, ThemeGroup.Function_2, false);

                            var radiusLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(radius.transform, "label", 0);
                            var radiusLabelText = radiusLabel.GetComponent<Text>();
                            radiusLabelText.alignment = TextAnchor.MiddleLeft;
                            radiusLabelText.text = "Radius";
                            radiusLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(radiusLabelText);
                            var radiusLabelLayout = radiusLabel.AddComponent<LayoutElement>();
                            radiusLabelLayout.minWidth = 100f;

                            #endregion

                            #region Sides

                            var sides = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "sides");
                            var sidesStorage = sides.GetComponent<InputFieldStorage>();

                            Destroy(sidesStorage.addButton.gameObject);
                            Destroy(sidesStorage.subButton.gameObject);
                            Destroy(sidesStorage.leftGreaterButton.gameObject);
                            Destroy(sidesStorage.middleButton.gameObject);
                            Destroy(sidesStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(sidesStorage.inputField);
                            EditorThemeManager.AddSelectable(sidesStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(sidesStorage.rightButton, ThemeGroup.Function_2, false);

                            var sidesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(sides.transform, "label", 0);
                            var sidesLabelText = sidesLabel.GetComponent<Text>();
                            sidesLabelText.alignment = TextAnchor.MiddleLeft;
                            sidesLabelText.text = "Sides";
                            sidesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(sidesLabelText);
                            var sidesLabelLayout = sidesLabel.AddComponent<LayoutElement>();
                            sidesLabelLayout.minWidth = 100f;

                            #endregion

                            #region Roundness

                            var roundness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "roundness");
                            var roundnessStorage = roundness.GetComponent<InputFieldStorage>();

                            Destroy(roundnessStorage.addButton.gameObject);
                            Destroy(roundnessStorage.subButton.gameObject);
                            Destroy(roundnessStorage.leftGreaterButton.gameObject);
                            Destroy(roundnessStorage.middleButton.gameObject);
                            Destroy(roundnessStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(roundnessStorage.inputField);
                            EditorThemeManager.AddSelectable(roundnessStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(roundnessStorage.rightButton, ThemeGroup.Function_2, false);

                            var roundnessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(roundness.transform, "label", 0);
                            var roundnessLabelText = roundnessLabel.GetComponent<Text>();
                            roundnessLabelText.alignment = TextAnchor.MiddleLeft;
                            roundnessLabelText.text = "Roundness";
                            roundnessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(roundnessLabelText);
                            var roundnessLabelLayout = roundnessLabel.AddComponent<LayoutElement>();
                            roundnessLabelLayout.minWidth = 100f;

                            #endregion

                            #region Thickness

                            var thickness = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "thickness");
                            var thicknessStorage = thickness.GetComponent<InputFieldStorage>();

                            Destroy(thicknessStorage.addButton.gameObject);
                            Destroy(thicknessStorage.subButton.gameObject);
                            Destroy(thicknessStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessStorage.middleButton.gameObject);
                            Destroy(thicknessStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thickness.transform, "label", 0);
                            var thicknessLabelText = thicknessLabel.GetComponent<Text>();
                            thicknessLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessLabelText.text = "Thickness";
                            thicknessLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(thicknessLabelText);
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
                            EditorThemeManager.AddLightText(thicknessOffsetLabelText);
                            var thicknessOffsetLabelLayout = thicknessOffsetLabel.AddComponent<LayoutElement>();
                            thicknessOffsetLabelLayout.minWidth = 130f;

                            var thicknessOffsetX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "x");
                            var thicknessOffsetXStorage = thicknessOffsetX.GetComponent<InputFieldStorage>();

                            Destroy(thicknessOffsetXStorage.addButton.gameObject);
                            Destroy(thicknessOffsetXStorage.subButton.gameObject);
                            Destroy(thicknessOffsetXStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessOffsetXStorage.middleButton.gameObject);
                            Destroy(thicknessOffsetXStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessOffsetXStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessOffsetXStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessOffsetXStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessOffsetY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessOffset.transform, "y");
                            var thicknessOffsetYStorage = thicknessOffsetY.GetComponent<InputFieldStorage>();

                            Destroy(thicknessOffsetYStorage.addButton.gameObject);
                            Destroy(thicknessOffsetYStorage.subButton.gameObject);
                            Destroy(thicknessOffsetYStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessOffsetYStorage.middleButton.gameObject);
                            Destroy(thicknessOffsetYStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessOffsetYStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessOffsetYStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessOffsetYStorage.rightButton, ThemeGroup.Function_2, false);

                            #endregion

                            #region Thickness Scale

                            var thicknessScale = Creator.NewUIObject("thickness scale", so);
                            var thicknessScaleLayout = thicknessScale.AddComponent<HorizontalLayoutGroup>();

                            var thicknessScaleLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(thicknessScale.transform, "label");
                            var thicknessScaleLabelText = thicknessScaleLabel.GetComponent<Text>();
                            thicknessScaleLabelText.alignment = TextAnchor.MiddleLeft;
                            thicknessScaleLabelText.text = "Thick Scale";
                            thicknessScaleLabelText.rectTransform.sizeDelta = new Vector2(130f, 32f);
                            EditorThemeManager.AddLightText(thicknessScaleLabelText);
                            var thicknessScaleLabelLayout = thicknessScaleLabel.AddComponent<LayoutElement>();
                            thicknessScaleLabelLayout.minWidth = 130f;

                            var thicknessScaleX = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "x");
                            var thicknessScaleXStorage = thicknessScaleX.GetComponent<InputFieldStorage>();

                            Destroy(thicknessScaleXStorage.addButton.gameObject);
                            Destroy(thicknessScaleXStorage.subButton.gameObject);
                            Destroy(thicknessScaleXStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessScaleXStorage.middleButton.gameObject);
                            Destroy(thicknessScaleXStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessScaleXStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessScaleXStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessScaleXStorage.rightButton, ThemeGroup.Function_2, false);

                            var thicknessScaleY = EditorPrefabHolder.Instance.NumberInputField.Duplicate(thicknessScale.transform, "y");
                            var thicknessScaleYStorage = thicknessScaleY.GetComponent<InputFieldStorage>();

                            Destroy(thicknessScaleYStorage.addButton.gameObject);
                            Destroy(thicknessScaleYStorage.subButton.gameObject);
                            Destroy(thicknessScaleYStorage.leftGreaterButton.gameObject);
                            Destroy(thicknessScaleYStorage.middleButton.gameObject);
                            Destroy(thicknessScaleYStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(thicknessScaleYStorage.inputField);
                            EditorThemeManager.AddSelectable(thicknessScaleYStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(thicknessScaleYStorage.rightButton, ThemeGroup.Function_2, false);

                            #endregion

                            #region Slices

                            var slices = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "slices");
                            var slicesStorage = slices.GetComponent<InputFieldStorage>();

                            Destroy(slicesStorage.addButton.gameObject);
                            Destroy(slicesStorage.subButton.gameObject);
                            Destroy(slicesStorage.leftGreaterButton.gameObject);
                            Destroy(slicesStorage.middleButton.gameObject);
                            Destroy(slicesStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(slicesStorage.inputField);
                            EditorThemeManager.AddSelectable(slicesStorage.leftButton, ThemeGroup.Function_2, false);
                            EditorThemeManager.AddSelectable(slicesStorage.rightButton, ThemeGroup.Function_2, false);

                            var slicesLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(slices.transform, "label", 0);
                            var slicesLabelText = slicesLabel.GetComponent<Text>();
                            slicesLabelText.alignment = TextAnchor.MiddleLeft;
                            slicesLabelText.text = "Slices";
                            slicesLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(slicesLabelText);
                            var slicesLabelLayout = slicesLabel.AddComponent<LayoutElement>();
                            slicesLabelLayout.minWidth = 100f;

                            #endregion

                            #region Angle

                            var rotation = EditorPrefabHolder.Instance.NumberInputField.Duplicate(so, "rotation");
                            var rotationStorage = rotation.GetComponent<InputFieldStorage>();

                            Destroy(rotationStorage.addButton.gameObject);
                            Destroy(rotationStorage.subButton.gameObject);
                            Destroy(rotationStorage.leftGreaterButton.gameObject);
                            Destroy(rotationStorage.middleButton.gameObject);
                            Destroy(rotationStorage.rightGreaterButton.gameObject);

                            EditorThemeManager.AddInputField(rotationStorage);

                            var rotationLabel = EditorPrefabHolder.Instance.Labels.transform.GetChild(0).gameObject.Duplicate(rotation.transform, "label", 0);
                            var rotationLabelText = rotationLabel.GetComponent<Text>();
                            rotationLabelText.alignment = TextAnchor.MiddleLeft;
                            rotationLabelText.text = "Angle";
                            rotationLabelText.rectTransform.sizeDelta = new Vector2(100f, 32f);
                            EditorThemeManager.AddLightText(rotationLabelText);
                            var rotationLabelLayout = rotationLabel.AddComponent<LayoutElement>();
                            rotationLabelLayout.minWidth = 100f;

                            #endregion
                        }
                    }
                }

                updatedShapes = true;
            }

            LSHelpers.SetActiveChildren(shapeSettings, false);

            shapeSettings.AsRT().sizeDelta = new Vector2(351f, multiShapeSelection.x == 4 ? 74f : 32f);
            shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, multiShapeSelection.x == 4 ? 74f : 32f);

            shapeSettings.GetChild(multiShapeSelection.x).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in shapeToggles)
            {
                int index = num;
                toggle.onValueChanged.ClearAll();
                toggle.isOn = multiShapeSelection.x == index;
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);

                if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length)
                    toggle.onValueChanged.AddListener(_val =>
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
                        RenderMultiShape();
                    });


                num++;
            }

            switch ((ShapeType)multiShapeSelection.x)
            {
                case ShapeType.Text: {
                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.onValueChanged.ClearAll();
                        if (!updatedText)
                        {
                            updatedText = true;
                            textIF.textComponent.alignment = TextAnchor.UpperLeft;
                            textIF.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                            textIF.GetPlaceholderText().text = "Enter text...";
                            textIF.lineType = InputField.LineType.MultiLineNewline;

                            textIF.text = "";
                        }
                        textIF.onValueChanged.AddListener(_val =>
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
                            buttonStorage.image.sprite = EditorSprites.EditSprite;
                            EditorThemeManager.ApplySelectable(buttonStorage.button, ThemeGroup.Function_2);
                            EditorThemeManager.ApplyGraphic(buttonStorage.image, ThemeGroup.Function_2_Text);
                            buttonStorage.button.onClick.NewListener(() => RTTextEditor.inst.SetInputField(textIF));
                            UIManager.SetRectTransform(buttonStorage.baseImage.rectTransform, new Vector2(160f, 24f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(22f, 22f));
                            EditorHelper.SetComplexity(button, Complexity.Advanced);
                        }
                        else
                        {
                            var button = textIF.transform.Find("edit").gameObject;
                            var buttonStorage = button.GetComponent<DeleteButtonStorage>();
                            buttonStorage.button.onClick.NewListener(() => RTTextEditor.inst.SetInputField(textIF));
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

                                var levelPath = jpgFile.Replace("\\", "/").Replace(editorPath + "/", "");
                                CoreHelper.Log($"levelPath: {levelPath}");

                                if (!RTFile.FileExists(jpgFileLocation) && !jpgFile.Replace("\\", "/").Contains(editorPath))
                                {
                                    RTFile.CopyFile(jpgFile, jpgFileLocation);
                                    CoreHelper.Log($"Copied file to : {jpgFileLocation}");
                                }
                                else
                                    jpgFileLocation = editorPath + "/" + levelPath;

                                CoreHelper.Log($"jpgFileLocation: {jpgFileLocation}");

                                var _val = jpgFileLocation.Replace(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1), ""); ;
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
                                RenderMultiShape();
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
                            toggle.onValueChanged.ClearAll();
                            toggle.isOn = multiShapeSelection.y == index;
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[multiShapeSelection.x]);

                            if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[multiShapeSelection.x])
                                toggle.onValueChanged.AddListener(_val =>
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
                                    RenderMultiShape();
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

            Destroy(oldName.GetComponent<EventTrigger>());
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
                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Edit Color", () =>
                    {
                        RTColorPicker.inst.Show(RTColors.HexToColor(inputField.text),
                            (col, hex) =>
                            {
                                inputField.SetTextWithoutNotify(hex);
                            },
                            (col, hex) =>
                            {
                                CoreHelper.Log($"Set timeline object color: {hex}");
                                    // set the input field's text empty so it notices there was a change
                                    inputField.SetTextWithoutNotify(string.Empty);
                                inputField.text = hex;
                            }, () =>
                            {
                                inputField.SetTextWithoutNotify(currentHexColor);
                            });
                    }),
                    new ButtonFunction("Clear", () =>
                    {
                        inputField.text = string.Empty;
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("VG Red", () =>
                    {
                        inputField.text = ObjectEditorData.RED;
                    }),
                    new ButtonFunction("VG Red Green", () =>
                    {
                        inputField.text = ObjectEditorData.RED_GREEN;
                    }),
                    new ButtonFunction("VG Green", () =>
                    {
                        inputField.text = ObjectEditorData.GREEN;
                    }),
                    new ButtonFunction("VG Green Blue", () =>
                    {
                        inputField.text = ObjectEditorData.GREEN_BLUE;
                    }),
                    new ButtonFunction("VG Blue", () =>
                    {
                        inputField.text = ObjectEditorData.BLUE;
                    }),
                    new ButtonFunction("VG Blue Red", () =>
                    {
                        inputField.text = ObjectEditorData.RED_BLUE;
                    }));
            };

            EditorHelper.AddInputFieldContextMenu(inputField);
            TriggerHelper.InversableField(inputField, InputFieldSwapper.Type.String);

            EditorThemeManager.AddInputField(inputField);

            var setColorButton = EditorPrefabHolder.Instance.Function1Button.Duplicate(replaceName.transform, "set color");
            var setColorButtonStorage = setColorButton.GetComponent<FunctionButtonStorage>();
            setColorButton.transform.localScale = Vector3.one;
            setColorButton.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
            setColorButton.GetComponent<LayoutElement>().minWidth = 32f;

            setColorButtonStorage.label.text = buttonLabel;
            setColorButtonStorage.button.onClick.NewListener(() => setColor?.Invoke(inputField));

            EditorThemeManager.AddGraphic(setColorButtonStorage.button.image, ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(setColorButtonStorage.label, ThemeGroup.Function_1_Text);

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

            Destroy(oldName.GetComponent<EventTrigger>());
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

            EditorThemeManager.AddInputField(oldNameIF);

            var newName = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(replaceName.transform, newNameStr.ToLower());

            Destroy(newName.GetComponent<EventTrigger>());
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

            EditorThemeManager.AddInputField(newNameIF);

            var replace = EditorPrefabHolder.Instance.Function1Button.Duplicate(replaceName.transform, "replace");
            replace.transform.localScale = Vector3.one;
            replace.transform.AsRT().sizeDelta = new Vector2(66f, 32f);
            replace.GetComponent<LayoutElement>().minWidth = 32f;

            var replaceText = replace.transform.GetChild(0).GetComponent<Text>();

            replaceText.text = "Replace";

            EditorThemeManager.AddGraphic(replace.GetComponent<Image>(), ThemeGroup.Function_1, true);
            EditorThemeManager.AddGraphic(replaceText, ThemeGroup.Function_1_Text);

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
            EditorThemeManager.AddGraphic(image, ThemeGroup.Background_3);
        }

        void GeneratePad(Transform parent, Complexity complexity, bool onlySpecificComplexity = false)
        {
            var gameObject = Creator.NewUIObject("padder", parent);
            var image = gameObject.AddComponent<Image>();
            image.rectTransform.sizeDelta = new Vector2(395f, 4f);
            EditorThemeManager.AddGraphic(image, ThemeGroup.Background_3);
            EditorHelper.SetComplexity(gameObject, complexity, onlySpecificComplexity);
        }

        void GeneratePasteKeyframeData(Transform parent, int type, string name)
        {
            GeneratePasteKeyframeData(parent, () =>
            {
                var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(type);
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
                var copiedKeyframeData = ObjectEditor.inst.GetCopiedData(type);
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

                        var kf = (EventKeyframe)bm.events[type][Mathf.Clamp(num, 0, bm.events[type].Count - 1)];
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

            var pasteAllTypesBase = new GameObject("paste all types");
            pasteAllTypesBase.transform.SetParent(parent);
            pasteAllTypesBase.transform.localScale = Vector3.one;

            var pasteAllTypesBaseRT = pasteAllTypesBase.AddComponent<RectTransform>();
            pasteAllTypesBaseRT.sizeDelta = new Vector2(390f, 32f);

            var pasteAllTypesBaseHLG = pasteAllTypesBase.AddComponent<HorizontalLayoutGroup>();
            pasteAllTypesBaseHLG.childControlHeight = false;
            pasteAllTypesBaseHLG.childControlWidth = false;
            pasteAllTypesBaseHLG.childForceExpandHeight = false;
            pasteAllTypesBaseHLG.childForceExpandWidth = false;
            pasteAllTypesBaseHLG.spacing = 8f;

            var pasteAllTypesToAllObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteAllTypesBaseRT, name);
            pasteAllTypesToAllObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteAllTypesToAllObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteAllTypesToAllText = pasteAllTypesToAllObject.transform.GetChild(0).GetComponent<Text>();
            pasteAllTypesToAllText.text = "Paste to All";

            EditorThemeManager.AddGraphic(pasteAllTypesToAllObject.GetComponent<Image>(), ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteAllTypesToAllText, ThemeGroup.Paste_Text);

            var pasteAllTypesToAll = pasteAllTypesToAllObject.GetComponent<Button>();
            pasteAllTypesToAll.onClick.ClearAll();
            pasteAllTypesToAll.onClick.AddListener(() => { pasteAll?.Invoke(); });

            var pasteAllTypesToIndexObject = EditorPrefabHolder.Instance.Function1Button.Duplicate(pasteAllTypesBaseRT, name);
            pasteAllTypesToIndexObject.transform.localScale = Vector3.one;

            ((RectTransform)pasteAllTypesToIndexObject.transform).sizeDelta = new Vector2(180f, 32f);

            var pasteAllTypesToIndexText = pasteAllTypesToIndexObject.transform.GetChild(0).GetComponent<Text>();
            pasteAllTypesToIndexText.text = "Paste to Index";

            EditorThemeManager.AddGraphic(pasteAllTypesToIndexObject.GetComponent<Image>(), ThemeGroup.Paste, true);
            EditorThemeManager.AddGraphic(pasteAllTypesToIndexText, ThemeGroup.Paste_Text);

            var pasteAllTypesToIndex = pasteAllTypesToIndexObject.GetComponent<Button>();
            pasteAllTypesToIndex.onClick.ClearAll();
            pasteAllTypesToIndex.onClick.AddListener(() => { pasteToIndex?.Invoke(index.text); });

            EditorHelper.SetComplexity(index.transform.parent.gameObject, Complexity.Advanced);
            EditorHelper.SetComplexity(pasteAllTypesBase, Complexity.Advanced);
        }

        void SyncObjectData(string nameContext, PointerEventData eventData, Action<TimelineObject, BeatmapObject> update, bool renderTimelineObject = false, bool updateObject = true, string updateContext = "")
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                EditorContextMenu.inst.ShowContextMenu(400f,
                    new ButtonFunction($"Sync {nameContext} via Search", () =>
                    {
                        ObjectEditor.inst.ShowObjectSearch(beatmapObject =>
                        {
                            SyncObjectData(timelineObject => update?.Invoke(timelineObject, beatmapObject), renderTimelineObject, updateObject, updateContext);
                            RTEditor.inst.ObjectSearchPopup.Close();
                        });
                    }),
                    new ButtonFunction($"Sync {nameContext} via Picker", () =>
                    {
                        EditorTimeline.inst.onSelectTimelineObject = to =>
                        {
                            if (!to.isBeatmapObject)
                                return;

                            var beatmapObject = to.GetData<BeatmapObject>();
                            SyncObjectData(timelineObject => update?.Invoke(timelineObject, beatmapObject), renderTimelineObject, updateObject, updateContext);
                        };
                    }));

                return;
            }

            ObjectEditor.inst.ShowObjectSearch(beatmapObject =>
            {
                SyncObjectData(timelineObject => update?.Invoke(timelineObject, beatmapObject), renderTimelineObject, updateObject, updateContext);
                RTEditor.inst.ObjectSearchPopup.Close();
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
                kf.curve = (Easing)(curves.value - 1);
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
                kf.curve = (Easing)(curves.value - 1);
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
                kf.curve = (Easing)(curves.value - 1);
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
                EditorThemeManager.AddLightText(labelText);
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
                EditorThemeManager.AddLightText(labelText);
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
                EditorThemeManager.AddSelectable(inputFieldStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            else
                Destroy(inputFieldStorage.leftGreaterButton.gameObject);

            if (doRightGreater)
                EditorThemeManager.AddSelectable(inputFieldStorage.rightGreaterButton, ThemeGroup.Function_2, false);
            else
                Destroy(inputFieldStorage.rightGreaterButton.gameObject);

            if (doMiddle)
                EditorThemeManager.AddSelectable(inputFieldStorage.middleButton, ThemeGroup.Function_2, false);
            else
                Destroy(inputFieldStorage.middleButton.gameObject);

            EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);

            EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);

            EditorThemeManager.AddInputField(inputFieldStorage.inputField);

            return inputFieldStorage;
        }

        /// <summary>
        /// Generates a horizontal group of buttons.
        /// </summary>
        /// <param name="parent">The transform to parent the buttons group to.</param>
        /// <param name="sizeY">The Y size of the base. Default is 32 or 48.</param>
        /// <param name="spacing">Spacing for the layout group. Default is 8.</param>
        /// <param name="buttons">Array of buttons to generate.</param>
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

        /// <summary>
        /// Generates a horizontal group of buttons.
        /// </summary>
        /// <param name="parent">The transform to parent the buttons group to.</param>
        /// <param name="sizeY">The Y size of the base. Default is 32 or 48.</param>
        /// <param name="spacing">Spacing for the layout group. Default is 8.</param>
        /// <param name="buttons">Array of buttons to generate.</param>
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
                buttonStorage.button.onClick.NewListener(() => buttonFunction.Action?.Invoke());

            buttonStorage.label.fontSize = buttonFunction.FontSize;
            buttonStorage.label.text = buttonFunction.Name;

            EditorThemeManager.AddGraphic(buttonStorage.button.image, buttonFunction.ButtonThemeGroup ?? buttonGroup, true);
            EditorThemeManager.AddGraphic(buttonStorage.label, buttonFunction.LabelThemeGroup ?? labelGroup);

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

            EditorThemeManager.AddInputField(inputFieldStorage.inputField);

            Destroy(inputFieldStorage.leftGreaterButton.gameObject);
            Destroy(inputFieldStorage.middleButton.gameObject);
            Destroy(inputFieldStorage.rightGreaterButton.gameObject);
            EditorThemeManager.AddSelectable(inputFieldStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(inputFieldStorage.rightButton, ThemeGroup.Function_2, false);

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
    }
}

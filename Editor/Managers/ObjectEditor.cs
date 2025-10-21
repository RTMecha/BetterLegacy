using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Companion.Data.Parameters;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Popups;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Editor.Managers
{
    public class ObjectEditor : MonoBehaviour
    {
        #region Init

        public static ObjectEditor inst;

        public static void Init() => ObjEditor.inst.gameObject.AddComponent<ObjectEditor>();

        void Awake()
        {
            inst = this;

            try
            {
                Dialog = new ObjectEditorDialog();
                Dialog.Init();

                CreateObjectSearch();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog

            ApplyConfig();

            try
            {
                LoadObjectTemplates();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // load object templates

            LoadGlobalCopy();
        }

        void CreateObjectSearch()
        {
            ObjectSearchPopup = RTEditor.inst.GeneratePopup(EditorPopup.OBJECT_SEARCH_POPUP, "Object Search", Vector2.zero, new Vector2(600f, 450f), placeholderText: "Search for object...");
            ObjectSearchPopup.getMaxPageCount = () => GameData.Current.beatmapObjects.FindAll(x => !x.FromPrefab).Count / ObjectsPerPage;
            ObjectSearchPopup.InitPageField();

            var dropdown = EditorHelper.AddEditorDropdown("Search Objects", string.Empty, EditorHelper.VIEW_DROPDOWN, EditorSprites.SearchSprite, ShowObjectSearch);

            EditorHelper.SetComplexity(dropdown, Complexity.Normal);
        }

        public void ApplyConfig()
        {
            ObjEditor.inst.SelectedColor = EditorConfig.Instance.ObjectSelectionColor.Value;
            ObjEditor.inst.ObjectLengthOffset = EditorConfig.Instance.KeyframeEndLengthOffset.Value;
        }

        #endregion

        #region Values

        public ObjectEditorDialog Dialog { get; set; }

        public ContentPopup ObjectSearchPopup { get; set; }

        public static bool AllowTimeExactlyAtStart => false;

        public GameObject shapeButtonPrefab;

        public Prefab copy;

        public CustomUIDisplay copiedUIDisplay;


        public List<Toggle> gradientColorButtons = new List<Toggle>();

        public bool colorShifted;

        public static float TimelineObjectHoverSize { get; set; }

        #endregion

        #region Dragging

        void Update()
        {
            Dialog?.ModifiersDialog?.Tick();
            KeyframeTimeline.CurrentTimeline?.Tick();

            if (Input.GetMouseButtonUp(0))
            {
                ObjEditor.inst.beatmapObjectsDrag = false;
                RTEditor.inst.dragOffset = -1f;
                RTEditor.inst.dragBinOffset = -100;
            }

            HandleObjectsDrag();
        }

        void HandleObjectsDrag()
        {
            if (!ObjEditor.inst.beatmapObjectsDrag)
                return;

            var musicLength = SoundManager.inst.MusicLength;
            var selectedObjects = EditorTimeline.inst.SelectedObjects;

            if (InputDataManager.inst.editorActions.MultiSelect.IsPressed)
            {
                int binOffset = 14 - Mathf.RoundToInt((float)((Input.mousePosition.y - 25) * EditorManager.inst.ScreenScaleInverse / 20)) + ObjEditor.inst.mouseOffsetYForDrag;

                bool hasChanged = false;

                foreach (var timelineObject in selectedObjects)
                {
                    if (timelineObject.Locked)
                        continue;

                    int binCalc = EditorTimeline.inst.CalculateMaxBin(binOffset + timelineObject.binOffset);

                    if (timelineObject.Bin != binCalc)
                        hasChanged = true;

                    timelineObject.Bin = binCalc;
                    timelineObject.RenderPosLength();
                    if (timelineObject.isBeatmapObject && selectedObjects.Count == 1)
                        RenderBin(timelineObject.GetData<BeatmapObject>());
                    if (timelineObject.isPrefabObject && selectedObjects.Count == 1)
                        RTPrefabEditor.inst.RenderPrefabObjectBin(timelineObject.GetData<PrefabObject>());
                    if (timelineObject.isBackgroundObject && selectedObjects.Count == 1)
                        RTBackgroundEditor.inst.RenderBin(timelineObject.GetData<BackgroundObject>());
                }

                if (RTEditor.inst.dragBinOffset != binOffset && !Input.GetKey(KeyCode.LeftAlt) && !selectedObjects.All(x => x.Locked))
                {
                    if (hasChanged && RTEditor.DraggingPlaysSound)
                        SoundManager.inst.PlaySound(DefaultSounds.UpDown, 0.4f, 0.6f);

                    RTEditor.inst.dragBinOffset = binOffset;
                }

                return;
            }

            float timeOffset = EditorTimeline.inst.GetTimelineTime(RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsObjects.Value) + ObjEditor.inst.mouseOffsetXForDrag;
            if (EditorConfig.Instance.ClampedTimelineDrag.Value)
                timeOffset = Mathf.Clamp(timeOffset, 0f, musicLength);
            timeOffset = Mathf.Round(timeOffset * 1000f) / 1000f;

            if (RTEditor.inst.dragOffset != timeOffset && !Input.GetKey(KeyCode.LeftAlt) &&!EditorTimeline.inst.SelectedObjects.All(x => x.Locked))
            {
                if (RTEditor.DraggingPlaysSound && (RTEditor.inst.editorInfo.bpmSnapActive || !RTEditor.DraggingPlaysSoundBPM))
                    SoundManager.inst.PlaySound(DefaultSounds.LeftRight, RTEditor.inst.editorInfo.bpmSnapActive ? 0.6f : 0.1f, 0.7f);

                RTEditor.inst.dragOffset = timeOffset;
            }

            foreach (var timelineObject in selectedObjects)
            {
                if (timelineObject.Locked)
                    continue;

                var time = timeOffset + timelineObject.timeOffset;
                if (EditorConfig.Instance.ClampedTimelineDrag.Value)
                    time = Mathf.Clamp(time, 0f, musicLength);

                timelineObject.Time = time;

                timelineObject.RenderPosLength();
                
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();

                            var runtimeObject = beatmapObject.runtimeObject;

                            if (runtimeObject)
                            {
                                runtimeObject.StartTime = beatmapObject.StartTime;
                                runtimeObject.KillTime = beatmapObject.StartTime + beatmapObject.SpawnDuration;

                                runtimeObject.SetActive(beatmapObject.Alive);
                            }

                            if (selectedObjects.Count == 1)
                            {
                                RenderStartTime(beatmapObject);
                                Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                                Dialog.Timeline.RenderMarkerPositions(beatmapObject);
                            }
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            RTPrefabEditor.inst.RenderPrefabObjectStartTime(prefabObject);
                            RTLevel.Current?.UpdatePrefab(prefabObject, PrefabObjectContext.TIME, false);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();

                            var runtimeObject = backgroundObject.runtimeObject;

                            if (runtimeObject)
                            {
                                runtimeObject.StartTime = backgroundObject.StartTime;
                                runtimeObject.KillTime = backgroundObject.StartTime + backgroundObject.SpawnDuration;

                                runtimeObject.SetActive(backgroundObject.Alive);
                            }

                            if (selectedObjects.Count == 1)
                                RTBackgroundEditor.inst.RenderStartTime(backgroundObject);
                            break;
                        }
                }
            }

            RTLevel.Current?.Sort();
            RTLevel.Current?.backgroundEngine?.spawner?.RecalculateObjectStates();

            if (EditorConfig.Instance.UpdateHomingKeyframesDrag.Value && RTLevel.Current)
                System.Threading.Tasks.Task.Run(RTLevel.Current.UpdateHomingKeyframes);
        }

        #endregion

        #region Deleting

        public IEnumerator DeleteKeyframes()
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                yield return CoroutineHelper.StartCoroutine(Dialog.Timeline.DeleteKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>()));
            yield break;
        }

        #endregion

        #region Copy / Paste

        /// <summary>
        /// Loads the globally copied file.
        /// </summary>
        public void LoadGlobalCopy()
        {
            try
            {
                var prefabFilePath = RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}");
                if (!RTFile.FileExists(prefabFilePath))
                    return;

                var jn = JSON.Parse(RTFile.ReadFromFile(prefabFilePath));
                copy = Prefab.Parse(jn);
                ObjEditor.inst.hasCopiedObject = true;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Could not load global copied objects.\n{ex}");
            } // load global copy
        }

        public void CopyObjects()
        {
            var selected = EditorTimeline.inst.SelectedObjects;

            float start = 0f;
            if (EditorConfig.Instance.PasteOffset.Value)
                start = -AudioManager.inst.CurrentAudioSource.time + selected.Min(x => x.Time);

            var copy = new Prefab("copied prefab", 0, start,
                selected.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                selected.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList(),
                null,
                selected.Where(x => x.isBackgroundObject).Select(x => x.GetData<BackgroundObject>()).ToList());

            copy.description = "Take me wherever you go!";
            this.copy = copy;
            ObjEditor.inst.hasCopiedObject = true;

            if (EditorConfig.Instance.CopyPasteGlobal.Value && RTFile.DirectoryExists(Application.persistentDataPath))
                RTFile.WriteToFile(RTFile.CombinePaths(Application.persistentDataPath, $"copied_objects{FileFormat.LSP.Dot()}"), copy.ToJSON().ToString());
        }

        public void PasteObject() => PasteObject(0f);

        public void PasteObject(float offsetTime) => PasteObject(offsetTime, false);

        public void PasteObject(float offsetTime, bool regen) => PasteObject(offsetTime, false, regen);

        public void PasteObject(float offsetTime, bool dup, bool regen)
        {
            var copy = this.copy;
            if (!ObjEditor.inst.hasCopiedObject || !copy || (copy.prefabObjects.IsEmpty() && copy.beatmapObjects.IsEmpty() && copy.backgroundObjects.IsEmpty()))
            {
                EditorManager.inst.DisplayNotification("No copied object yet!", 1f, EditorManager.NotificationType.Error, false);
                return;
            }

            List<TimelineObject> selected = null;
            PrefabExpander.Expanded expanded = null;
            var time = RTLevel.Current.FixedTime + copy.offset;

            EditorManager.inst.history.Add(new History.Command("Paste Objects",
                () =>
                {
                    selected = EditorTimeline.inst.SelectedObjects;
                    EditorTimeline.inst.DeselectAllObjects();
                    EditorManager.inst.DisplayNotification("Pasting objects.", 1f, EditorManager.NotificationType.Success);

                    new PrefabExpander(copy)
                        .Select()
                        .Offset(dup ? offsetTime : time)
                        .Regen(regen)
                        .AddBin(dup)
                        .Expand(e =>
                        {
                            expanded = e;
                        });
                },
                () =>
                {
                    if (!expanded)
                        return;

                    RTEditor.inst.RemoveBeatmap(expanded);
                    if (selected != null)
                        foreach (var timelineObject in selected)
                            timelineObject.Selected = true;
                }), true);
        }
        
        public void PasteKeyframes(bool setTime = true)
        {
            if (EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                Dialog.Timeline.PasteKeyframes(EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>(), setTime);
        }

        #endregion

        #region Create New Objects

        public List<ObjectOptionPanel> defaultObjectOptions = new List<ObjectOptionPanel>();

        /// <summary>
        /// List of extra options used to create objects.
        /// </summary>
        public List<ObjectOption> objectOptions = new List<ObjectOption>()
        {
            new ObjectOption("Normal", "A regular square object that hits the player.", null),
            new ObjectOption("Helper", "A regular square object that is transparent and doesn't hit the player. This can be used to warn players of an attack.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Helper;
                beatmapObject.name = nameof(BeatmapObject.ObjectType.Helper);
            }),
            new ObjectOption("Decoration", "A regular square object that is opaque and doesn't hit the player.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Decoration;
                beatmapObject.name = nameof(BeatmapObject.ObjectType.Decoration);
            }),
            new ObjectOption("Solid", "A regular square object that doesn't allow the player to passh through.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Solid;
                beatmapObject.name = nameof(BeatmapObject.ObjectType.Solid);
            }),
            new ObjectOption("Alpha Helper", "A regular square object that is transparent and doesn't hit the player. This can be used to warn players of an attack.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Decoration;
                beatmapObject.name = nameof(BeatmapObject.ObjectType.Helper);
                beatmapObject.events[3][0].values[1] = 0.65f;
                beatmapObject.opacityCollision = true;
            }),
            new ObjectOption("Empty Hitbox", "A square object that is invisible but still has a collision and can hit the player.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Normal;
                beatmapObject.name = "Collision";
                beatmapObject.events[3][0].values[1] = 1f;
            }),
            new ObjectOption("Empty Solid", "A square object that is invisible but still has a collision and prevents the player from passing through.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Solid;
                beatmapObject.name = "Collision";
                beatmapObject.events[3][0].values[1] = 1f;
            }),
            new ObjectOption("Text", "A text object that can be used for dialogue.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Decoration;
                beatmapObject.name = "Text";
                beatmapObject.text = "A text object that can be used for dialogue.";
                beatmapObject.shape = 4;
                beatmapObject.shapeOption = 0;
            }),
            new ObjectOption("Text Sequence", "A text object that can be used for dialogue. Includes a textSequence modifier.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Decoration;
                beatmapObject.name = "Text";
                beatmapObject.text = "A text object that can be used for dialogue. Includes a textSequence modifier.";
                beatmapObject.shape = 4;
                beatmapObject.shapeOption = 0;
                if (ModifiersManager.inst.modifiers.TryFind(x => x.Name == nameof(ModifierFunctions.textSequence), out Modifier modifier))
                    beatmapObject.modifiers.Add(modifier.Copy());
            }),
            new ObjectOption("Screen Overlay", "An object that covers the screen.", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Decoration;
                beatmapObject.renderLayerType = BeatmapObject.RenderLayerType.UI;
                beatmapObject.Parent = BeatmapObject.CAMERA_PARENT;
                beatmapObject.ParentType = "111";
                beatmapObject.events[1][0].values[0] = 1000f;
                beatmapObject.events[1][0].values[1] = 1000f;
                beatmapObject.editorData.selectable = false; // prevent overlay from being selectable in the preview area.
            }),
            new ObjectOption("Actor Frame Texture", "An object that captures an area and applies it to the objects texture..", timelineObject =>
            {
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                beatmapObject.objectType = BeatmapObject.ObjectType.Decoration;

                Modifier modifier;
                if (ModifiersManager.inst.modifiers.TryFind(x => x.Name == nameof(ModifierFunctions.actorFrameTexture), out modifier))
                    beatmapObject.modifiers.Add(modifier.Copy());
                if (ModifiersManager.inst.modifiers.TryFind(x => x.Name == nameof(ModifierFunctions.translateShape), out modifier))
                {
                    beatmapObject.origin = new Vector2(-0.5f, -0.5f);
                    modifier = modifier.Copy();
                    modifier.values[1] = "0.5";
                    modifier.values[2] = "0.5";
                    beatmapObject.modifiers.Add(modifier);
                }
            }),
        };

        /// <summary>
        /// List of custom object templates.
        /// </summary>
        public List<ObjectOption> customObjectOptions = new List<ObjectOption>();

        /// <summary>
        /// Loads the custom object templates list.
        /// </summary>
        public void LoadObjectTemplates()
        {
            var defaultTemplatesFilePath = AssetPack.GetFile($"editor/data/object_templates{FileFormat.JSON.Dot()}");

            if (RTFile.TryReadFromFile(defaultTemplatesFilePath, out string defaultTemplatesFile))
            {
                var jn = JSON.Parse(defaultTemplatesFile);

                if (jn["layout"] != null)
                {
                    var gridLayoutGroup = RTEditor.inst.ObjectOptionsPopup.GameObject.GetComponent<GridLayoutGroup>();
                    gridLayoutGroup.cellSize = Parser.TryParse(jn["layout"]["cell_size"], new Vector2(142f, 34f));
                    gridLayoutGroup.spacing = Parser.TryParse(jn["layout"]["spacing"], new Vector2(8f, 8f));
                    gridLayoutGroup.constraint = jn["layout"]["constraint"] != null ? (GridLayoutGroup.Constraint)jn["layout"]["constraint"].AsInt : GridLayoutGroup.Constraint.FixedColumnCount;
                    gridLayoutGroup.constraintCount = jn["layout"]["constraint_count"] != null ? jn["layout"]["constraint_count"].AsInt : 2;
                }

                defaultObjectOptions.Clear();

                var parent = RTEditor.inst.ObjectOptionsPopup.GameObject.transform;
                for (int i = parent.childCount - 1; i >= 1; i--)
                    CoreHelper.Delete(parent.GetChild(i));

                for (int i = 0; i < jn["options"].Count; i++)
                {
                    var jnOption = jn["options"][i];
                    var objectOptionPanel = new ObjectOptionPanel(true, parent);

                    if (jnOption["options"] != null)
                    {
                        var options = Parser.ParseObjectList<ObjectOption>(jnOption["options"]);
                        var cellSize = new Vector2(34f, 32f);
                        var spacing = new Vector2(2f, 0f);
                        if (jnOption["layout"] != null)
                        {
                            cellSize = Parser.TryParse(jnOption["layout"]["cell_size"], new Vector2(34f, 34f));
                            spacing = Parser.TryParse(jnOption["layout"]["spacing"], new Vector2(2f, 0f));
                        }

                        objectOptionPanel.Init(options, cellSize, spacing);
                    }
                    else
                        objectOptionPanel.Init(ObjectOption.Parse(jnOption));

                    defaultObjectOptions.Add(objectOptionPanel);
                }
            }

            if (RTFile.TryReadFromFile(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"create_object_templates{FileFormat.JSON.Dot()}"), out string customTemplatesFile))
            {
                customObjectOptions.Clear();
                var jn = JSON.Parse(customTemplatesFile);

                for (int i = 0; i < jn["objects"].Count; i++)
                {
                    var data = jn["data"];
                    customObjectOptions.Add(new ObjectOption(jn["name"], jn["desc"], timelineObject => timelineObject.GetData<BeatmapObject>().ReadJSON(data)));
                }
            }
        }

        /// <summary>
        /// Adds a Beatmap Object to the custom object templates.
        /// </summary>
        /// <param name="beatmapObject">Object to create a template of.</param>
        /// <param name="name">Name of the template.</param>
        /// <param name="desc">Description of the template.</param>
        public void AddObjectTemplate(BeatmapObject beatmapObject, string name, string desc)
        {
            var filePath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, $"create_object_templates{FileFormat.JSON.Dot()}");
            var jn = !RTFile.FileExists(filePath) ? Parser.NewJSONObject() : JSON.Parse(RTFile.ReadFromFile(filePath));

            var jnObject = Parser.NewJSONObject();
            jnObject["name"] = name;
            jnObject["desc"] = desc;
            jnObject["data"] = beatmapObject.ToJSON();

            jn["objects"][jn["objects"].Count] = jnObject;

            RTFile.WriteToFile(filePath, jn.ToString());
        }

        /// <summary>
        /// Shows extra object templates.
        /// </summary>
        public void ShowObjectTemplates()
        {
            RTEditor.inst.ObjectTemplatePopup.Open();
            RTEditor.inst.ObjectTemplatePopup.UpdateSearchFunction(RefreshObjectTemplates);
            RefreshObjectTemplates(RTEditor.inst.ObjectTemplatePopup.SearchField.text);
        }

        /// <summary>
        /// Refreshes the list of extra object templates.
        /// </summary>
        /// <param name="search">The search term.</param>
        public void RefreshObjectTemplates(string search)
        {
            RTEditor.inst.ObjectTemplatePopup.ClearContent();
            var objectOptions = customObjectOptions.IsEmpty() ? this.objectOptions : this.objectOptions.Union(customObjectOptions).ToList();
            for (int i = 0; i < objectOptions.Count; i++)
            {
                if (!RTString.SearchString(search, objectOptions[i].name))
                    continue;

                var name = objectOptions[i].name;
                var hint = objectOptions[i].hint;

                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ObjectTemplatePopup.Content, "Function");

                gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip { desc = name, hint = hint });

                var button = gameObject.GetComponent<Button>();
                button.onClick.NewListener(objectOptions[i].Create);

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                var text = gameObject.transform.GetChild(0).GetComponent<Text>();
                text.text = name;
                EditorThemeManager.ApplyLightText(text);
            }
        }

        /// <summary>
        /// Creates a new Beatmap Object and generates a Timeline Object for it.
        /// </summary>
        /// <param name="select">If the Timeline Object should be selected.</param>
        /// <returns>Returns the generated Timeline Object.</returns>
        public TimelineObject CreateNewDefaultObject(bool select = true)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                return null;
            }

            var beatmapObject = CreateNewBeatmapObject(AudioManager.inst.CurrentAudioSource.time);
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 5f;

            ApplyObjectCreationSettings(beatmapObject);

            if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                EditorTimeline.inst.SetLayer(beatmapObject.editorData.Layer, EditorTimeline.LayerType.Objects);

            GameData.Current.beatmapObjects.Add(beatmapObject);

            var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

            AudioManager.inst.SetMusicTime(AllowTimeExactlyAtStart ? AudioManager.inst.CurrentAudioSource.time : AudioManager.inst.CurrentAudioSource.time + 0.001f);

            if (select)
                EditorTimeline.inst.SetCurrentObject(timelineObject);

            return timelineObject;
        }

        public void ApplyObjectCreationSettings(BeatmapObject beatmapObject)
        {
            beatmapObject.orderModifiers = EditorConfig.Instance.CreateObjectModifierOrderDefault.Value;
            beatmapObject.opacityCollision = EditorConfig.Instance.CreateObjectOpacityCollisionDefault.Value;
            beatmapObject.autoTextAlign = EditorConfig.Instance.CreateObjectAutoTextAlignDefault.Value;

            // setup default parent values
            beatmapObject.SetParentType(0, EditorConfig.Instance.CreateObjectPositionParentDefault.Value);
            beatmapObject.SetParentType(1, EditorConfig.Instance.CreateObjectScaleParentDefault.Value);
            beatmapObject.SetParentType(2, EditorConfig.Instance.CreateObjectRotationParentDefault.Value);

            // setup default keyframe values
            beatmapObject.events[0][0].relative = EditorConfig.Instance.CreateObjectPositionKFRelativeDefault.Value;
            beatmapObject.events[1][0].relative = EditorConfig.Instance.CreateObjectScaleKFRelativeDefault.Value;
            beatmapObject.events[2][0].relative = EditorConfig.Instance.CreateObjectRotationKFRelativeDefault.Value;
        }

        /// <summary>
        /// Creates a new Beatmap Object with the default start keyframes.
        /// </summary>
        /// <param name="time">Time to create the object at.</param>
        /// <returns>Returns a new Beatmap Object.</returns>
        public BeatmapObject CreateNewBeatmapObject(float time)
        {
            if (RTEditor.inst.editorInfo.bpmSnapActive && EditorConfig.Instance.BPMSnapsCreated.Value && EditorConfig.Instance.BPMSnapsObjects.Value)
                time = RTEditor.SnapToBPM(time);

            var beatmapObject = new BeatmapObject(time);

            if (!Seasons.IsAprilFools)
                beatmapObject.editorData.Layer = EditorTimeline.inst.Layer;

            beatmapObject.InitDefaultEvents();

            return beatmapObject;
        }

        /// <summary>
        /// Creates a new beatmap object.
        /// </summary>
        /// <param name="action">Action to apply to the timeline object.</param>
        /// <param name="select">If the object should be selected.</param>
        /// <param name="setHistory">If undo / redo history should be set.</param>
        public void CreateNewObject(Action<TimelineObject> action = null, bool select = true, bool setHistory = true, bool recalculate = true, bool openDialog = true, bool exampleNotice = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            action?.Invoke(timelineObject);
            RTLevel.Current?.UpdateObject(bm, recalculate: recalculate);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();

            if (openDialog)
                OpenDialog(bm);

            if (exampleNotice)
                Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm, true));

            if (setHistory)
                EditorManager.inst.history.Add(new History.Command("Create New Object", () => CreateNewObject(action, select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewNormalObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Object", () => CreateNewNormalObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewCircleObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 1;
            bm.shapeOption = 0;
            bm.name = Seasons.IsAprilFools ? "<font=Arrhythmia>bro" : "circle";

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Circle Object", () => CreateNewCircleObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewTriangleObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 2;
            bm.shapeOption = 0;
            bm.name = Seasons.IsAprilFools ? "baracuda <i>beat plays</i>" : "triangle";

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Triangle Object", () => CreateNewTriangleObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewTextObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 4;
            bm.shapeOption = 0;
            bm.text = Seasons.IsAprilFools ? "Never gonna give you up<br>" +
                                            "Never gonna let you down<br>" +
                                            "Never gonna run around and desert you<br>" +
                                            "Never gonna make you cry<br>" +
                                            "Never gonna say goodbye<br>" +
                                            "Never gonna tell a lie and hurt you" : "text";
            bm.name = Seasons.IsAprilFools ? "Don't look at my text" : "text";
            bm.objectType = BeatmapObject.ObjectType.Decoration;
            if (Seasons.IsAprilFools)
                bm.StartTime += 1f;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();

            if (!Seasons.IsAprilFools)
                OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Text Object", () => CreateNewTextObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewHexagonObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.shape = 5;
            bm.shapeOption = 0;
            bm.name = Seasons.IsAprilFools ? "super" : "hexagon";

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Normal Hexagon Object", () => CreateNewHexagonObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewHelperObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = Seasons.IsAprilFools ? "totally not deprecated object" : "helper";
            bm.objectType = Seasons.IsAprilFools ? BeatmapObject.ObjectType.Decoration : BeatmapObject.ObjectType.Helper;
            if (Seasons.IsAprilFools)
                bm.events[3][0].values[1] = 0.65f;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Helper Object", () => CreateNewHelperObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewDecorationObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "decoration";
            if (!Seasons.IsAprilFools)
                bm.objectType = BeatmapObject.ObjectType.Decoration;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Decoration Object", () => CreateNewDecorationObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewEmptyObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = "empty";
            if (!Seasons.IsAprilFools)
                bm.objectType = BeatmapObject.ObjectType.Empty;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y + (Seasons.IsAprilFools ? 999f : 0f);
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New Empty Object", () => CreateNewEmptyObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        public void CreateNewNoAutokillObject(bool select = true, bool setHistory = true)
        {
            var timelineObject = CreateNewDefaultObject(select);

            var bm = timelineObject.GetData<BeatmapObject>();
            bm.name = Seasons.IsAprilFools ? "dead" : "no autokill";
            bm.autoKillType = AutoKillType.NoAutokill;
            bm.objectType = BeatmapObject.ObjectType.Decoration;

            if (EditorConfig.Instance.CreateObjectsatCameraCenter.Value)
            {
                var pos = EventManager.inst.cam.transform.position;

                bm.events[0][0].values[0] = pos.x;
                bm.events[0][0].values[1] = pos.y;
            }

            RTLevel.Current?.UpdateObject(bm);
            EditorTimeline.inst.RenderTimelineObject(timelineObject);
            EditorTimeline.inst.UpdateTransformIndex();
            OpenDialog(bm);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.NEW_OBJECT, new BeatmapObjectNoticeParameters(bm));

            if (!setHistory)
                return;

            EditorManager.inst.history.Add(new History.Command("Create New No Autokill Object", () => CreateNewNoAutokillObject(select, false), () => EditorTimeline.inst.DeleteObject(timelineObject)));
        }

        /// <summary>
        /// Creates a sequence of image objects.
        /// </summary>
        /// <param name="directory">Directory that contains images.</param>
        /// <param name="fps">FPS of the image sequence.</param>
        public void CreateImageSequence(string directory, int fps)
        {
            if (RTFile.DirectoryExists(directory))
                CreateImageSequence(Directory.GetFiles(directory), fps);
        }

        /// <summary>
        /// Creates a sequence of image objects.
        /// </summary>
        /// <param name="files">Files to create an image sequence from.</param>
        /// <param name="fps">FPS of the image sequence.</param>
        public void CreateImageSequence(string[] files, int fps)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Cannot create an image sequence wihtout a level loaded.", 4f, EditorManager.NotificationType.Error);
                return;
            }

            EditorManager.inst.DisplayNotification("Creating image sequence. Please wait...", 3f, EditorManager.NotificationType.Warning);

            TimelineObject parentObject = null;
            string parentID = string.Empty;
            var time = AudioManager.inst.CurrentAudioSource.time;
            int frame = 0;
            var sw = CoreHelper.StartNewStopwatch();
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                if (!RTFile.FileIsFormat(file, FileFormat.PNG, FileFormat.JPG))
                    continue;

                if (!parentObject)
                    CreateNewObject(timelineObject =>
                    {
                        var beatmapObject = timelineObject.GetData<BeatmapObject>();
                        beatmapObject.name = "P_Sequence Parent";
                        beatmapObject.StartTime = time;
                        beatmapObject.objectType = BeatmapObject.ObjectType.Empty;
                        parentID = beatmapObject.id;
                        parentObject = timelineObject;
                    }, false, true, false, false, false);

                float t = 1f / fps;

                CreateNewObject(timelineObject =>
                {
                    var beatmapObject = timelineObject.GetData<BeatmapObject>();
                    beatmapObject.name = $"{frame} frame";
                    beatmapObject.parentType = "111";
                    beatmapObject.Parent = parentID;
                    beatmapObject.StartTime = time;
                    beatmapObject.ShapeType = ShapeType.Image;
                    beatmapObject.autoKillOffset = t;
                    beatmapObject.autoKillType = AutoKillType.FixedTime;
                    beatmapObject.editorData.Bin = 1;
                    SelectImage(file, beatmapObject, false, false);
                }, false, true, false, false, false);

                time += t;
                frame++;
            }

            RTLevel.Current?.RecalculateObjectStates();

            if (parentObject)
                EditorTimeline.inst.SetCurrentObject(parentObject);

            CoreHelper.StopAndLogStopwatch(sw);
            EditorManager.inst.DisplayNotification($"Created image sequence! Took {sw.Elapsed}", 3f, EditorManager.NotificationType.Warning);
        }

        #endregion

        #region Render Dialog

        public static bool UpdateObjects => true;

        public static bool HideVisualElementsWhenObjectIsEmpty { get; set; }

        /// <summary>
        /// Opens the Object Editor dialog.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to edit.</param>
        public void OpenDialog(BeatmapObject beatmapObject)
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Open a level first before trying to select an object.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!beatmapObject || string.IsNullOrEmpty(beatmapObject.id))
            {
                EditorManager.inst.DisplayNotification("Cannot edit non-object!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            EditorManager.inst.ClearPopups();

            if (!Dialog)
            {
                EditorManager.inst.DisplayNotification("Object Editor Dialog is null. Please report this to RTMecha.", 4f, EditorManager.NotificationType.Error);
                return;
            }

            Dialog.Open();

            if (EditorTimeline.inst.CurrentSelection.ID != beatmapObject.id)
                for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                    LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i], true);

            RenderDialog(beatmapObject);
        }

        /// <summary>
        /// Refreshes the Object Editor to the specified BeatmapObject, allowing for any object to be edited from anywhere.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to render the editor for.</param>
        public void RenderDialog(BeatmapObject beatmapObject)
        {
            if (!EditorManager.inst.hasLoadedLevel || string.IsNullOrEmpty(beatmapObject.id))
                return;

            EditorTimeline.inst.CurrentSelection = EditorTimeline.inst.GetTimelineObject(beatmapObject);
            EditorTimeline.inst.CurrentSelection.Selected = true;

            RenderID(beatmapObject);
            RenderLDM(beatmapObject);
            RenderName(beatmapObject);
            RenderTags(beatmapObject);
            RenderObjectType(beatmapObject);

            RenderStartTime(beatmapObject);
            RenderAutokill(beatmapObject);

            RenderParent(beatmapObject);

            RenderOrigin(beatmapObject);
            RenderBlendMode(beatmapObject);
            RenderGradient(beatmapObject);
            RenderShape(beatmapObject);
            RenderDepth(beatmapObject);

            RenderLayers(beatmapObject);
            RenderBin(beatmapObject);

            RenderIndex(beatmapObject);

            RenderEditorColors(beatmapObject);
            RenderCustomUIDisplay(beatmapObject);

            RenderGameObjectInspector(beatmapObject);
            RenderPrefabReference(beatmapObject);

            Dialog.Timeline.SetTimeline(beatmapObject, EditorTimeline.inst.CurrentSelection.Zoom, EditorTimeline.inst.CurrentSelection.TimelinePosition);

            Dialog.Timeline.RenderDialog(beatmapObject);
            Dialog.Timeline.RenderMarkers(beatmapObject);

            CoroutineHelper.StartCoroutine(Dialog.ModifiersDialog.RenderModifiers(beatmapObject));
        }

        /// <summary>
        /// Renders the ID Text.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderID(BeatmapObject beatmapObject)
        {
            Dialog.IDBase.gameObject.SetActive(RTEditor.NotSimple);
            if (!RTEditor.NotSimple)
                return;

            Dialog.IDText.text = $"ID: {beatmapObject.id}";

            var clickable = Dialog.IDBase.gameObject.GetOrAddComponent<Clickable>();

            clickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Copy ID", () =>
                        {
                            EditorManager.inst.DisplayNotification($"Copied ID from {beatmapObject.name}!", 2f, EditorManager.NotificationType.Success);
                            LSText.CopyToClipboard(beatmapObject.id);
                        }),
                        new ButtonFunction("Shuffle ID", () =>
                        {
                            RTEditor.inst.ShowWarningPopup("Are you sure you want to shuffle the ID of this object?", () =>
                            {
                                EditorHelper.ShuffleID(beatmapObject);
                                RenderID(beatmapObject);
                                RenderParent(beatmapObject);
                                RTEditor.inst.HideWarningPopup();
                            }, RTEditor.inst.HideWarningPopup);
                        }));
                    return;
                }

                EditorManager.inst.DisplayNotification($"Copied ID from {beatmapObject.name}!", 2f, EditorManager.NotificationType.Success);
                LSText.CopyToClipboard(beatmapObject.id);
            };
        }

        /// <summary>
        /// Renders the LDM Toggle.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderLDM(BeatmapObject beatmapObject)
        {
            Dialog.LDMLabel.gameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.LDMToggle.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            Dialog.LDMToggle.SetIsOnWithoutNotify(beatmapObject.LDM);
            Dialog.LDMToggle.onValueChanged.NewListener(_val =>
            {
                beatmapObject.LDM = _val;
                RTLevel.Current?.UpdateObject(beatmapObject);
            });
        }

        /// <summary>
        /// Renders the Name InputField.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderName(BeatmapObject beatmapObject)
        {
            // Allows for left / right flipping.
            TriggerHelper.InversableField(Dialog.NameField, InputFieldSwapper.Type.String);
            EditorHelper.AddInputFieldContextMenu(Dialog.NameField);

            Dialog.NameField.SetTextWithoutNotify(beatmapObject.name);
            Dialog.NameField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.name = _val;

                // Since name has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders the Tags list.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderTags(BeatmapObject beatmapObject) => RTEditor.inst.RenderTags(beatmapObject, Dialog);

        /// <summary>
        /// Renders the ObjectType Dropdown.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderObjectType(BeatmapObject beatmapObject)
        {
            Dialog.ObjectTypeDropdown.options =
                EditorConfig.Instance.EditorComplexity.Value == Complexity.Advanced ?
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty", "Solid") :
                    CoreHelper.StringToOptionData("Normal", "Helper", "Decoration", "Empty"); // don't show solid object type 

            Dialog.ObjectTypeDropdown.SetValueWithoutNotify(Mathf.Clamp((int)beatmapObject.objectType, 0, Dialog.ObjectTypeDropdown.options.Count - 1));
            Dialog.ObjectTypeDropdown.onValueChanged.NewListener(_val =>
            {
                beatmapObject.objectType = (BeatmapObject.ObjectType)_val;
                RenderGameObjectInspector(beatmapObject);
                // ObjectType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.OBJECT_TYPE);

                RenderDialog(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all StartTime UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderStartTime(BeatmapObject beatmapObject)
        {
            var startTimeField = Dialog.StartTimeField;

            startTimeField.lockToggle.SetIsOnWithoutNotify(beatmapObject.editorData.locked);
            startTimeField.lockToggle.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.locked = _val;

                // Since locking has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });

            startTimeField.inputField.SetTextWithoutNotify(beatmapObject.StartTime.ToString());
            startTimeField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    if (EditorConfig.Instance.ClampedTimelineDrag.Value)
                        num = Mathf.Clamp(num, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                    beatmapObject.StartTime = num;

                    // StartTime affects both physical object and timeline object.
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.START_TIME);

                    beatmapObject.modifiers.ForEach(modifier =>
                    {
                        modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                        ModifiersHelper.OnRemoveCache(modifier);
                        modifier.Result = default;
                    });

                    Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                    Dialog.Timeline.RenderMarkers(beatmapObject);
                }
            });

            TriggerHelper.AddEventTriggers(Dialog.StartTimeField.gameObject, TriggerHelper.ScrollDelta(startTimeField.inputField));

            startTimeField.leftGreaterButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });

                Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                Dialog.Timeline.RenderMarkers(beatmapObject);
            });
            startTimeField.leftButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime - 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });

                Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                Dialog.Timeline.RenderMarkers(beatmapObject);
            });
            startTimeField.middleButton.onClick.NewListener(() =>
            {
                startTimeField.inputField.text = EditorManager.inst.CurrentAudioPos.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });

                Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                Dialog.Timeline.RenderMarkers(beatmapObject);
            });
            startTimeField.rightButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 0.1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });

                Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                Dialog.Timeline.RenderMarkers(beatmapObject);
            });
            startTimeField.rightGreaterButton.onClick.NewListener(() =>
            {
                float moveTime = beatmapObject.StartTime + 1f;
                moveTime = Mathf.Clamp(moveTime, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
                startTimeField.inputField.text = moveTime.ToString();

                // StartTime affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.START_TIME);

                beatmapObject.modifiers.ForEach(modifier =>
                {
                    modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                    ModifiersHelper.OnRemoveCache(modifier);
                    modifier.Result = default;
                });

                Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                Dialog.Timeline.RenderMarkers(beatmapObject);
            });
        }

        /// <summary>
        /// Renders all Autokill UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderAutokill(BeatmapObject beatmapObject)
        {
            Dialog.AutokillDropdown.SetValueWithoutNotify((int)beatmapObject.autoKillType);
            Dialog.AutokillDropdown.onValueChanged.NewListener(_val =>
            {
                beatmapObject.autoKillType = (AutoKillType)_val;
                // AutoKillType affects both physical object and timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
                Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                RenderAutokill(beatmapObject);
                Dialog.Timeline.RenderMarkers(beatmapObject);
            });

            if (beatmapObject.autoKillType == AutoKillType.FixedTime ||
                beatmapObject.autoKillType == AutoKillType.SongTime ||
                beatmapObject.autoKillType == AutoKillType.LastKeyframeOffset)
            {
                Dialog.AutokillField.gameObject.SetActive(true);

                Dialog.AutokillField.SetTextWithoutNotify(beatmapObject.autoKillOffset.ToString());
                Dialog.AutokillField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        {
                            float startTime = beatmapObject.StartTime;
                            if (num < startTime)
                                num = startTime + 0.1f;
                        }

                        if (num < 0f)
                            num = 0f;

                        beatmapObject.autoKillOffset = num;

                        // AutoKillType affects both physical object and timeline object.
                        EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);

                        beatmapObject.modifiers.ForEach(modifier =>
                        {
                            modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                            ModifiersHelper.OnRemoveCache(modifier);
                            modifier.Result = default;
                        });

                        Dialog.Timeline.ResizeKeyframeTimeline(beatmapObject);
                        Dialog.Timeline.RenderMarkers(beatmapObject);
                    }
                });

                Dialog.AutokillSetButton.gameObject.SetActive(true);
                Dialog.AutokillSetButton.onClick.NewListener(() =>
                {
                    float num = 0f;

                    if (beatmapObject.autoKillType == AutoKillType.SongTime)
                        num = AudioManager.inst.CurrentAudioSource.time;
                    else num = AudioManager.inst.CurrentAudioSource.time - beatmapObject.StartTime;

                    if (num < 0f)
                        num = 0f;

                    Dialog.AutokillField.text = num.ToString();
                });

                // Add Scrolling for easy changing of values.
                TriggerHelper.AddEventTriggers(Dialog.AutokillField.gameObject, TriggerHelper.ScrollDelta(Dialog.AutokillField, 0.1f, 10f, 0f, float.PositiveInfinity));
            }
            else
            {
                Dialog.AutokillField.gameObject.SetActive(false);
                Dialog.AutokillField.onValueChanged.ClearAll();
                Dialog.AutokillSetButton.gameObject.SetActive(false);
                Dialog.AutokillSetButton.onClick.ClearAll();
            }

            Dialog.CollapseToggle.SetIsOnWithoutNotify(beatmapObject.editorData.collapse);
            Dialog.CollapseToggle.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.collapse = _val;

                // Since autokill collapse has no affect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders all Parent UI.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderParent(BeatmapObject beatmapObject) => RTEditor.inst.RenderParent(beatmapObject, Dialog);

        /// <summary>
        /// Renders the Origin InputFields.
        /// </summary>
        /// <param name="beatmapObject">The Beatmap Object to set.</param>
        public void RenderOrigin(BeatmapObject beatmapObject)
        {
            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty;

            var originTF = Dialog.OriginParent;
            originTF.parent.GetChild(originTF.GetSiblingIndex() - 1).gameObject.SetActive(active);
            originTF.gameObject.SetActive(active);

            if (!active)
                return;

            // Reimplemented origin toggles for Simple Editor Complexity.
            float[] originDefaultPositions = new float[] { 0f, -0.5f, 0f, 0.5f };
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = Dialog.OriginXToggles[i - 1];
                toggle.SetIsOnWithoutNotify(beatmapObject.origin.x == originDefaultPositions[i]);
                toggle.onValueChanged.NewListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1: {
                                beatmapObject.origin.x = -0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 2: {
                                beatmapObject.origin.x = 0f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 3: {
                                beatmapObject.origin.x = 0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                    }
                });

                var originContextMenu = toggle.gameObject.GetOrAddComponent<ContextClickable>();

                originContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    OriginContextMenu(beatmapObject);
                };
            }
            for (int i = 1; i <= 3; i++)
            {
                int index = i;
                var toggle = Dialog.OriginYToggles[i - 1];
                toggle.SetIsOnWithoutNotify(beatmapObject.origin.y == originDefaultPositions[i]);
                toggle.onValueChanged.NewListener(_val =>
                {
                    if (!_val)
                        return;

                    switch (index)
                    {
                        case 1: {
                                beatmapObject.origin.y = -0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 2: {
                                beatmapObject.origin.y = 0f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                        case 3: {
                                beatmapObject.origin.y = 0.5f;

                                // Since origin has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                                break;
                            }
                    }
                });

                var originContextMenu = toggle.gameObject.GetOrAddComponent<ContextClickable>();

                originContextMenu.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    OriginContextMenu(beatmapObject);
                };
            }

            Dialog.OriginXField.inputField.SetTextWithoutNotify(beatmapObject.origin.x.ToString());
            Dialog.OriginXField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.x = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                }
            });

            Dialog.OriginYField.inputField.SetTextWithoutNotify(beatmapObject.origin.y.ToString());
            Dialog.OriginYField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    beatmapObject.origin.y = num;

                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                }
            });

            TriggerHelper.IncreaseDecreaseButtons(Dialog.OriginXField);
            TriggerHelper.IncreaseDecreaseButtons(Dialog.OriginYField);

            TriggerHelper.AddEventTriggers(Dialog.OriginXField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.OriginXField.inputField, multi: true), TriggerHelper.ScrollDeltaVector2(Dialog.OriginXField.inputField, Dialog.OriginYField.inputField, 0.1f, 10f));
            TriggerHelper.AddEventTriggers(Dialog.OriginYField.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.OriginYField.inputField, multi: true), TriggerHelper.ScrollDeltaVector2(Dialog.OriginXField.inputField, Dialog.OriginYField.inputField, 0.1f, 10f));

            var originXContextMenu = Dialog.OriginXField.inputField.gameObject.GetOrAddComponent<ContextClickable>();

            originXContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                OriginContextMenu(beatmapObject);
            };

            var originYContextMenu = Dialog.OriginYField.inputField.gameObject.GetOrAddComponent<ContextClickable>();

            originYContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                OriginContextMenu(beatmapObject);
            };
        }

        void OriginContextMenu(BeatmapObject beatmapObject)
        {
            EditorContextMenu.inst.ShowContextMenu(
                new ButtonFunction("Center", () =>
                {
                    beatmapObject.origin = Vector2.zero;
                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Top", () =>
                {
                    beatmapObject.origin.y = -0.5f;
                    // Since origin has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Bottom", () =>
                {
                    beatmapObject.origin.y = 0.5f;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Left", () =>
                {
                    beatmapObject.origin.x = -0.5f;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Right", () =>
                {
                    beatmapObject.origin.x = 0.5f;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Top (Triangle)", () =>
                {
                    beatmapObject.origin.y = BeatmapObject.TRIANGLE_TOP_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Bottom (Triangle)", () =>
                {
                    beatmapObject.origin.y = BeatmapObject.TRIANGLE_BOTTOM_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Left (Triangle)", () =>
                {
                    beatmapObject.origin.x = -BeatmapObject.TRIANGLE_HORIZONTAL_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                }),
                new ButtonFunction("Right (Triangle)", () =>
                {
                    beatmapObject.origin.x = BeatmapObject.TRIANGLE_HORIZONTAL_OFFSET;
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
                    RenderOrigin(beatmapObject);
                })
                );
        }
        
        public void RenderBlendMode(BeatmapObject beatmapObject)
        {
            var active = (!HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty) && RTEditor.NotSimple;
            Dialog.ColorBlendModeLabel.gameObject.SetActive(active);
            Dialog.ColorBlendModeDropdown.gameObject.SetActive(active);

            Dialog.ColorBlendModeDropdown.SetValueWithoutNotify((int)beatmapObject.colorBlendMode);
            Dialog.ColorBlendModeDropdown.onValueChanged.NewListener(_val =>
            {
                beatmapObject.colorBlendMode = (ColorBlendMode)_val;
                var incompatibleGradient = beatmapObject.colorBlendMode != ColorBlendMode.Normal && beatmapObject.IsSpecialShape;

                if (incompatibleGradient)
                {
                    beatmapObject.Shape = 0;
                    beatmapObject.ShapeOption = 0;
                    RenderShape(beatmapObject);
                }

                // Since shape has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, incompatibleGradient ? ObjectContext.SHAPE : ObjectContext.RENDERING);
            });
        }

        /// <summary>
        /// Renders the Gradient ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderGradient(BeatmapObject beatmapObject)
        {
            var active = (!HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty) && RTEditor.NotSimple;
            var gradientScaleActive = beatmapObject.gradientType != GradientType.Normal;
            var gradientRotationActive = beatmapObject.gradientType == GradientType.LeftLinear || beatmapObject.gradientType == GradientType.RightLinear;

            Dialog.GradientShapesLabel.transform.parent.gameObject.SetActive(active);
            Dialog.GradientParent.gameObject.SetActive(active);
            Dialog.GradientScale.gameObject.SetActive(active && gradientScaleActive);
            Dialog.GradientRotation.gameObject.SetActive(active && gradientRotationActive);

            if (!active)
                return;

            Dialog.GradientShapesLabel.text = RTEditor.NotSimple ? "Gradient / Shape" : "Shape";

            for (int i = 0; i < Dialog.GradientToggles.Count; i++)
            {
                var index = i;
                var toggle = Dialog.GradientToggles[i];
                toggle.SetIsOnWithoutNotify(index == (int)beatmapObject.gradientType);
                toggle.onValueChanged.NewListener(_val =>
                {
                    beatmapObject.gradientType = (GradientType)index;
                    var incompatibleGradient = beatmapObject.gradientType != GradientType.Normal && beatmapObject.IsSpecialShape;

                    if (incompatibleGradient)
                    {
                        beatmapObject.Shape = 0;
                        beatmapObject.ShapeOption = 0;
                        RenderShape(beatmapObject);
                    }

                    if (!RTEditor.ShowModdedUI)
                    {
                        for (int i = 0; i < beatmapObject.events[3].Count; i++)
                            beatmapObject.events[3][i].values[6] = 10f;
                    }

                    // Since shape has no affect on the timeline object, we will only need to update the physical object.
                    if (UpdateObjects)
                        RTLevel.Current?.UpdateObject(beatmapObject, incompatibleGradient ? ObjectContext.SHAPE : ObjectContext.RENDERING);

                    RenderGradient(beatmapObject);
                    Dialog.Timeline.RenderDialog(beatmapObject);
                });
            }

            Dialog.GradientScale.inputField.onValueChanged.ClearAll();
            if (gradientScaleActive)
            {
                Dialog.GradientScale.inputField.text = beatmapObject.gradientScale.ToString();
                Dialog.GradientScale.inputField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.gradientScale = num;
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(Dialog.GradientScale);
                TriggerHelper.AddEventTriggers(Dialog.GradientScale.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.GradientScale.inputField));
                TriggerHelper.InversableField(Dialog.GradientScale);
            }

            Dialog.GradientRotation.inputField.onValueChanged.ClearAll();
            if (gradientRotationActive)
            {
                Dialog.GradientRotation.inputField.text = beatmapObject.gradientRotation.ToString();
                Dialog.GradientRotation.inputField.onValueChanged.AddListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        beatmapObject.gradientRotation = num;
                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
                    }
                });

                TriggerHelper.IncreaseDecreaseButtons(Dialog.GradientRotation, 15f, 3f);
                TriggerHelper.AddEventTriggers(Dialog.GradientRotation.inputField.gameObject, TriggerHelper.ScrollDelta(Dialog.GradientRotation.inputField, 15f, 3f));
                TriggerHelper.InversableField(Dialog.GradientRotation);
            }
        }

        /// <summary>
        /// Ensures a toggle list ends with a non-toggle game object.
        /// </summary>
        /// <param name="parent">The parent for the end non-toggle.</param>
        public void LastGameObject(Transform parent)
        {
            var gameObject = new GameObject("GameObject");
            gameObject.transform.SetParent(parent);
            gameObject.transform.localScale = Vector3.one;

            var rectTransform = gameObject.AddComponent<RectTransform>();

            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(0f, 32f);

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.layoutPriority = 1;
            layoutElement.preferredWidth = 1000f;
        }

        /// <summary>
        /// Renders the Shape ToggleGroup.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderShape(BeatmapObject beatmapObject)
        {
            var shape = Dialog.ShapeTypesParent;
            var shapeSettings = Dialog.ShapeOptionsParent;

            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty;
            Dialog.ShapeTypesParent.gameObject.SetActive(active);
            Dialog.ShapeOptionsParent.gameObject.SetActive(active);

            if (!active)
                return;

            LSHelpers.SetActiveChildren(shapeSettings, false);

            if (beatmapObject.Shape >= shapeSettings.childCount)
            {
                Debug.Log($"{ObjEditor.inst.className}Somehow, the object ended up being at a higher shape than normal.");
                beatmapObject.Shape = shapeSettings.childCount - 1;
                // Since shape has no affect on the timeline object, we will only need to update the physical object.
                if (UpdateObjects)
                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);

                RenderShape(beatmapObject);
                return;
            }

            shapeSettings.AsRT().sizeDelta = new Vector2(351f, beatmapObject.ShapeType == ShapeType.Text ? 74f : 32f);
            shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, beatmapObject.ShapeType == ShapeType.Text ? 74f : 32f);
            // 351 164 = polygon
            shapeSettings.GetChild(beatmapObject.Shape).gameObject.SetActive(true);

            int num = 0;
            foreach (var toggle in Dialog.ShapeToggles)
            {
                int index = num;
                toggle.SetIsOnWithoutNotify(beatmapObject.Shape == index);
                toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length);

                if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes.Length)
                    toggle.onValueChanged.NewListener(_val =>
                    {
                        beatmapObject.Shape = index;
                        beatmapObject.ShapeOption = 0;

                        if (beatmapObject.gradientType != GradientType.Normal && (index == 4 || index == 6))
                            beatmapObject.Shape = 0;

                        if (beatmapObject.ShapeType == ShapeType.Polygon && EditorConfig.Instance.AutoPolygonRadius.Value)
                            beatmapObject.polygonShape.Radius = beatmapObject.polygonShape.GetAutoRadius();

                        // Since shape has no affect on the timeline object, we will only need to update the physical object.
                        if (UpdateObjects)
                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);

                        RenderShape(beatmapObject);
                    });

                num++;
            }

            switch (beatmapObject.ShapeType)
            {
                case ShapeType.Text: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 74f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 74f);

                        var textIF = shapeSettings.Find("5").GetComponent<InputField>();
                        textIF.textComponent.alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().alignment = TextAnchor.UpperLeft;
                        textIF.GetPlaceholderText().text = "Enter text...";
                        textIF.lineType = InputField.LineType.MultiLineNewline;

                        textIF.SetTextWithoutNotify(beatmapObject.text);
                        textIF.onValueChanged.NewListener(_val =>
                        {
                            beatmapObject.text = _val;

                            // Since text has no affect on the timeline object, we will only need to update the physical object.
                            if (UpdateObjects)
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.TEXT);
                        });

                        var textContextClickable = textIF.gameObject.GetOrAddComponent<ContextClickable>();
                        textContextClickable.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            EditorContextMenu.inst.ShowContextMenu(
                                new ButtonFunction($"Open Text Editor", () => RTTextEditor.inst.SetInputField(textIF)),
                                new ButtonFunction(true),
                                new ButtonFunction($"Insert a Font", () => RTEditor.inst.ShowFontSelector(font => textIF.text = font + textIF.text)),
                                new ButtonFunction($"Add a Font", () => RTEditor.inst.ShowFontSelector(font => textIF.text += font)),
                                new ButtonFunction(true),
                                new ButtonFunction($"Clear Formatting", () =>
                                {
                                    RTEditor.inst.ShowWarningPopup("Are you sure you want to clear the fomratting of this text? This cannot be undone!", () =>
                                    {
                                        textIF.text = Regex.Replace(beatmapObject.text, @"<(.*?)>", string.Empty);
                                        RTEditor.inst.HideWarningPopup();
                                    }, RTEditor.inst.HideWarningPopup);
                                }),
                                new ButtonFunction($"Force Modded Formatting", () =>
                                {
                                    var formatText = "formatText";
                                    if (beatmapObject.modifiers.Has(x => x.Name == formatText))
                                        return;

                                    if (ModifiersManager.inst.modifiers.TryFind(x => x.Name == formatText, out Modifier modifier))
                                    {
                                        beatmapObject.modifiers.Add(modifier.Copy());
                                        CoroutineHelper.StartCoroutine(Dialog.ModifiersDialog.RenderModifiers(beatmapObject));
                                    }
                                }),
                                new ButtonFunction(true),
                                new ButtonFunction($"Auto Align: [{beatmapObject.autoTextAlign}]", () =>
                                {
                                    beatmapObject.autoTextAlign = !beatmapObject.autoTextAlign;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);
                                }),
                                new ButtonFunction("Align Left", () => textIF.text = "<align=left>" + textIF.text),
                                new ButtonFunction("Align Center", () => textIF.text = "<align=center>" + textIF.text),
                                new ButtonFunction("Align Right", () => textIF.text = "<align=right>" + textIF.text)
                                );
                        };

                        break;
                    }
                case ShapeType.Image: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);

                        var select = shapeSettings.Find("7/select").GetComponent<Button>();
                        select.onClick.ClearAll();
                        var selectContextClickable = select.gameObject.GetOrAddComponent<ContextClickable>();
                        selectContextClickable.onClick = eventData =>
                        {
                            if (eventData.button == PointerEventData.InputButton.Right)
                            {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction($"Use {RTEditor.SYSTEM_BROWSER}", () => OpenImageSelector(beatmapObject)),
                                    new ButtonFunction($"Use {RTEditor.EDITOR_BROWSER}", () =>
                                    {
                                        var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
                                        RTEditor.inst.BrowserPopup.Open();
                                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot(), FileFormat.JPG.Dot() }, file =>
                                        {
                                            SelectImage(file, beatmapObject);
                                            RTEditor.inst.BrowserPopup.Close();
                                        });
                                    }),
                                    new ButtonFunction($"Store & Use {RTEditor.SYSTEM_BROWSER}", () => OpenImageSelector(beatmapObject, copyFile: false, storeImage: true)),
                                    new ButtonFunction($"Store & Use {RTEditor.EDITOR_BROWSER}", () =>
                                    {
                                        var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
                                        RTEditor.inst.BrowserPopup.Open();
                                        RTFileBrowser.inst.UpdateBrowserFile(new string[] { FileFormat.PNG.Dot(), FileFormat.JPG.Dot() }, file =>
                                        {
                                            SelectImage(file, beatmapObject, copyFile: false, storeImage: true);
                                            RTEditor.inst.BrowserPopup.Close();
                                        });
                                    }),
                                    new ButtonFunction(true),
                                    new ButtonFunction("Remove Image", () =>
                                    {
                                        beatmapObject.text = string.Empty;

                                        // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                                        if (UpdateObjects)
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);

                                        RenderShape(beatmapObject);
                                    }),
                                    new ButtonFunction("Delete Image", () => RTEditor.inst.ShowWarningPopup("Are you sure you want to delete the image and remove it from the image object?", () =>
                                    {
                                        RTFile.DeleteFile(RTFile.CombinePaths(EditorLevelManager.inst.CurrentLevel.path, beatmapObject.text));

                                        beatmapObject.text = string.Empty;

                                // Since setting image has no affect on the timeline object, we will only need to update the physical object.
                                if (UpdateObjects)
                                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);

                                        RenderShape(beatmapObject);
                                    }, RTEditor.inst.HideWarningPopup))
                                    );
                                return;
                            }
                            OpenImageSelector(beatmapObject);
                        };
                        shapeSettings.Find("7/text").GetComponent<Text>().text = string.IsNullOrEmpty(beatmapObject.text) ? "No image selected" : beatmapObject.text;

                        // Stores / Removes Image Data for transfering of Image Objects between levels.
                        var dataText = shapeSettings.Find("7/set/Text").GetComponent<Text>();
                        dataText.text = !GameData.Current.assets.sprites.Has(x => x.name == beatmapObject.text) ? "Store Data" : "Clear Data";
                        var set = shapeSettings.Find("7/set").GetComponent<Button>();
                        set.onClick.NewListener(() =>
                        {
                            var regex = new Regex(@"img\((.*?)\)");
                            var match = regex.Match(beatmapObject.text);

                            var path = match.Success ? RTFile.CombinePaths(RTFile.BasePath, match.Groups[1].ToString()) : RTFile.CombinePaths(RTFile.BasePath, beatmapObject.text);

                            if (!GameData.Current.assets.sprites.Has(x => x.name == beatmapObject.text))
                                StoreImage(beatmapObject, path);
                            else
                            {
                                GameData.Current.assets.RemoveSprite(beatmapObject.text);
                                if (!RTFile.FileExists(path))
                                    beatmapObject.text = string.Empty;
                            }

                            RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.IMAGE);

                            RenderShape(beatmapObject);
                        });

                        break;
                    }
                case ShapeType.Polygon: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 276f);

                        var radius = shapeSettings.Find("10/radius").gameObject.GetComponent<InputFieldStorage>();
                        radius.inputField.onValueChanged.ClearAll();
                        radius.inputField.text = beatmapObject.polygonShape.Radius.ToString();
                        radius.SetInteractible(!EditorConfig.Instance.AutoPolygonRadius.Value);
                        if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                        {
                            radius.inputField.onValueChanged.AddListener(_val =>
                            {
                                if (float.TryParse(_val, out float num))
                                {
                                    num = Mathf.Clamp(num, 0.1f, 10f);
                                    beatmapObject.polygonShape.Radius = num;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }
                            });

                            TriggerHelper.IncreaseDecreaseButtons(radius, min: 0.1f, max: 10f);
                            TriggerHelper.AddEventTriggers(radius.inputField.gameObject, TriggerHelper.ScrollDelta(radius.inputField, min: 0.1f, max: 10f));
                        }

                        var contextMenu = radius.inputField.gameObject.GetOrAddComponent<ContextClickable>();
                        contextMenu.onClick = eventData =>
                        {
                            if (eventData.button != PointerEventData.InputButton.Right)
                                return;

                            var buttonFunctions = new List<ButtonFunction>()
                            {
                                new ButtonFunction($"Auto Assign Radius [{(EditorConfig.Instance.AutoPolygonRadius.Value ? "On" : "Off")}]", () =>
                                {
                                    EditorConfig.Instance.AutoPolygonRadius.Value = !EditorConfig.Instance.AutoPolygonRadius.Value;
                                    RenderShape(beatmapObject);
                                })
                            };

                            if (!EditorConfig.Instance.AutoPolygonRadius.Value)
                            {
                                buttonFunctions.Add(new ButtonFunction("Set to Triangle Radius", () =>
                                {
                                    beatmapObject.polygonShape.Radius = PolygonShape.TRIANGLE_RADIUS;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Square Radius", () =>
                                {
                                    beatmapObject.polygonShape.Radius = PolygonShape.SQUARE_RADIUS;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }));
                                buttonFunctions.Add(new ButtonFunction("Set to Normal Radius", () =>
                                {
                                    beatmapObject.polygonShape.Radius = PolygonShape.NORMAL_RADIUS;
                                    RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                                }));
                            }

                            EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
                        };

                        var sides = shapeSettings.Find("10/sides").gameObject.GetComponent<InputFieldStorage>();
                        sides.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Sides.ToString());
                        sides.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 3, 32);
                                beatmapObject.polygonShape.Sides = num;
                                if (EditorConfig.Instance.AutoPolygonRadius.Value)
                                {
                                    beatmapObject.polygonShape.Radius = beatmapObject.polygonShape.GetAutoRadius();
                                    radius.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Radius.ToString());
                                }
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(sides, min: 3, max: 32);
                        TriggerHelper.AddEventTriggers(sides.inputField.gameObject, TriggerHelper.ScrollDeltaInt(sides.inputField, min: 3, max: 32));
                        
                        var roundness = shapeSettings.Find("10/roundness").gameObject.GetComponent<InputFieldStorage>();
                        roundness.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Roundness.ToString());
                        roundness.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                beatmapObject.polygonShape.Roundness = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(roundness, max: 1f);
                        TriggerHelper.AddEventTriggers(roundness.inputField.gameObject, TriggerHelper.ScrollDelta(roundness.inputField, max: 1f));

                        var thickness = shapeSettings.Find("10/thickness").gameObject.GetComponent<InputFieldStorage>();
                        thickness.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Thickness.ToString());
                        thickness.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                num = Mathf.Clamp(num, 0f, 1f);
                                beatmapObject.polygonShape.Thickness = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thickness, max: 1f);
                        TriggerHelper.AddEventTriggers(thickness.inputField.gameObject, TriggerHelper.ScrollDelta(thickness.inputField, max: 1f));
                        
                        var thicknessOffsetX = shapeSettings.Find("10/thickness offset/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetX.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessOffset.x.ToString());
                        thicknessOffsetX.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessOffset = new Vector2(num, beatmapObject.polygonShape.ThicknessOffset.y);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetX);
                        TriggerHelper.AddEventTriggers(thicknessOffsetX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetX.inputField));
                        
                        var thicknessOffsetY = shapeSettings.Find("10/thickness offset/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessOffsetY.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessOffset.y.ToString());
                        thicknessOffsetY.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessOffset = new Vector2(beatmapObject.polygonShape.ThicknessOffset.x, num);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessOffsetY);
                        TriggerHelper.AddEventTriggers(thicknessOffsetY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessOffsetY.inputField));
                        
                        var thicknessScaleX = shapeSettings.Find("10/thickness scale/x").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleX.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessScale.x.ToString());
                        thicknessScaleX.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessScale = new Vector2(num, beatmapObject.polygonShape.ThicknessScale.y);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleX);
                        TriggerHelper.AddEventTriggers(thicknessScaleX.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleX.inputField));
                        
                        var thicknessScaleY = shapeSettings.Find("10/thickness scale/y").gameObject.GetComponent<InputFieldStorage>();
                        thicknessScaleY.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.ThicknessScale.y.ToString());
                        thicknessScaleY.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.ThicknessScale = new Vector2(beatmapObject.polygonShape.ThicknessScale.x, num);
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(thicknessScaleY);
                        TriggerHelper.AddEventTriggers(thicknessScaleY.inputField.gameObject, TriggerHelper.ScrollDelta(thicknessScaleY.inputField));

                        var slices = shapeSettings.Find("10/slices").gameObject.GetComponent<InputFieldStorage>();
                        slices.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Slices.ToString());
                        slices.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (int.TryParse(_val, out int num))
                            {
                                num = Mathf.Clamp(num, 1, 32);
                                beatmapObject.polygonShape.Slices = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtonsInt(slices, min: 1, max: 32);
                        TriggerHelper.AddEventTriggers(slices.inputField.gameObject, TriggerHelper.ScrollDeltaInt(slices.inputField, min: 1, max: 32));
                        
                        var rotation = shapeSettings.Find("10/rotation").gameObject.GetComponent<InputFieldStorage>();
                        rotation.inputField.SetTextWithoutNotify(beatmapObject.polygonShape.Angle.ToString());
                        rotation.inputField.onValueChanged.NewListener(_val =>
                        {
                            if (float.TryParse(_val, out float num))
                            {
                                beatmapObject.polygonShape.Angle = num;
                                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.POLYGONS);
                            }
                        });

                        TriggerHelper.IncreaseDecreaseButtons(rotation, 15f, 3f);
                        TriggerHelper.AddEventTriggers(rotation.inputField.gameObject, TriggerHelper.ScrollDelta(rotation.inputField, 15f, 3f));

                        break;
                    }
                default: {
                        shapeSettings.AsRT().sizeDelta = new Vector2(351f, 32f);
                        shapeSettings.GetChild(4).AsRT().sizeDelta = new Vector2(351f, 32f);

                        num = 0;
                        foreach (var toggle in Dialog.ShapeOptionToggles[beatmapObject.Shape])
                        {
                            int index = num;
                            toggle.SetIsOnWithoutNotify(beatmapObject.shapeOption == index);
                            toggle.gameObject.SetActive(RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[beatmapObject.Shape]);

                            if (RTEditor.ShowModdedUI || index < Shape.unmoddedMaxShapes[beatmapObject.Shape])
                                toggle.onValueChanged.NewListener(_val =>
                                {
                                    beatmapObject.ShapeOption = index;

                                    // Since shape has no affect on the timeline object, we will only need to update the physical object.
                                    if (UpdateObjects)
                                        RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.SHAPE);

                                    RenderShape(beatmapObject);
                                });

                            num++;
                        }

                        break;
                    }
            }
        }

        void SetDepthSlider(BeatmapObject beatmapObject, int value, InputField inputField, Slider slider)
        {
            if (!RTEditor.ShowModdedUI)
                value = Mathf.Clamp(value, EditorConfig.Instance.RenderDepthRange.Value.y, EditorConfig.Instance.RenderDepthRange.Value.x);

            beatmapObject.Depth = value;

            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.NewListener(_val => SetDepthInputField(beatmapObject, ((int)_val).ToString(), inputField, slider));

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
        }

        void SetDepthInputField(BeatmapObject beatmapObject, string value, InputField inputField, Slider slider)
        {
            if (!int.TryParse(value, out int num))
                return;

            if (!RTEditor.ShowModdedUI)
                num = Mathf.Clamp(num, EditorConfig.Instance.RenderDepthRange.Value.y, EditorConfig.Instance.RenderDepthRange.Value.x);

            beatmapObject.Depth = num;

            inputField.SetTextWithoutNotify(num.ToString());
            inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int numb))
                    SetDepthSlider(beatmapObject, numb, inputField, slider);
            });

            // Since depth has no affect on the timeline object, we will only need to update the physical object.
            if (UpdateObjects)
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.VISUAL_OFFSET);
        }

        /// <summary>
        /// Renders the Depth InputField and Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderDepth(BeatmapObject beatmapObject)
        {
            var active = !HideVisualElementsWhenObjectIsEmpty || beatmapObject.objectType != BeatmapObject.ObjectType.Empty;

            Dialog.DepthParent.gameObject.SetActive(active);

            var depthTf = Dialog.DepthField.transform.parent;
            depthTf.parent.GetChild(depthTf.GetSiblingIndex() - 1).gameObject.SetActive(active);
            depthTf.gameObject.SetActive(RTEditor.NotSimple && active);
            Dialog.DepthSlider.transform.AsRT().sizeDelta = new Vector2(RTEditor.NotSimple ? 352f : 292f, 32f);

            var renderTypeTF = Dialog.RenderTypeDropdown.transform;
            renderTypeTF.parent.GetChild(renderTypeTF.GetSiblingIndex() - 1).gameObject.SetActive(active && RTEditor.ShowModdedUI);
            renderTypeTF.gameObject.SetActive(active && RTEditor.ShowModdedUI);

            if (!active)
                return;

            Dialog.DepthField.inputField.SetTextWithoutNotify(beatmapObject.Depth.ToString());
            Dialog.DepthField.inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    SetDepthSlider(beatmapObject, num, Dialog.DepthField.inputField, Dialog.DepthSlider);
            });

            var max = EditorConfig.Instance.EditorComplexity.Value == Complexity.Simple ? 30 : EditorConfig.Instance.RenderDepthRange.Value.x;
            var min = EditorConfig.Instance.EditorComplexity.Value == Complexity.Simple ? 0 : EditorConfig.Instance.RenderDepthRange.Value.y;

            Dialog.DepthSlider.maxValue = max;
            Dialog.DepthSlider.minValue = min;

            Dialog.DepthSlider.SetValueWithoutNotify(beatmapObject.Depth);
            Dialog.DepthSlider.onValueChanged.NewListener(_val => SetDepthInputField(beatmapObject, _val.ToString(), Dialog.DepthField.inputField, Dialog.DepthSlider));

            if (RTEditor.ShowModdedUI)
            {
                max = 0;
                min = 0;
            }

            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField, -1, min, max);
            TriggerHelper.AddEventTriggers(Dialog.DepthField.inputField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.DepthField.inputField, 1, min, max));
            TriggerHelper.IncreaseDecreaseButtonsInt(Dialog.DepthField.inputField, -1, min, max, Dialog.DepthParent);

            // allow negative flipping
            if (min < 0)
                TriggerHelper.InversableField(Dialog.DepthField);
            else if (Dialog.DepthField.fieldSwapper)
                CoreHelper.Destroy(Dialog.DepthField.fieldSwapper);

            Dialog.RenderTypeDropdown.SetValueWithoutNotify((int)beatmapObject.renderLayerType);
            Dialog.RenderTypeDropdown.onValueChanged.NewListener(_val =>
            {
                beatmapObject.renderLayerType = (BeatmapObject.RenderLayerType)_val;
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.RENDERING);
            });
        }

        /// <summary>
        /// Creates and Renders the UnityExplorer GameObject Inspector.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to get.</param>
        public void RenderGameObjectInspector(BeatmapObject beatmapObject)
        {
            if (!ModCompatibility.UnityExplorerInstalled)
                return;

            if (Dialog.UnityExplorerLabel)
                Dialog.UnityExplorerLabel.transform.parent.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (Dialog.InspectBeatmapObjectButton)
            {
                Dialog.InspectBeatmapObjectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
                Dialog.InspectBeatmapObjectButton.button.onClick.NewListener(() => ModCompatibility.Inspect(beatmapObject));
            }

            if (Dialog.InspectRuntimeObjectButton)
            {
                bool active = beatmapObject.runtimeObject && RTEditor.ShowModdedUI;
                Dialog.InspectRuntimeObjectButton.gameObject.SetActive(active);
                Dialog.InspectRuntimeObjectButton.button.onClick.NewListener(() => ModCompatibility.Inspect(beatmapObject.runtimeObject));
            }

            if (Dialog.InspectTimelineObjectButton)
            {
                Dialog.InspectTimelineObjectButton.gameObject.SetActive(RTEditor.ShowModdedUI);
                Dialog.InspectTimelineObjectButton.button.onClick.NewListener(() => ModCompatibility.Inspect(EditorTimeline.inst.GetTimelineObject(beatmapObject)));
            }
        }

        /// <summary>
        /// Renders the Layers InputField.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderLayers(BeatmapObject beatmapObject)
        {
            Dialog.EditorLayerField.image.color = EditorTimeline.GetLayerColor(beatmapObject.editorData.Layer);
            Dialog.EditorLayerField.SetTextWithoutNotify((beatmapObject.editorData.Layer + 1).ToString());
            Dialog.EditorLayerField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    num = Mathf.Clamp(num - 1, 0, int.MaxValue);
                    beatmapObject.editorData.Layer = num;
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    RenderLayers(beatmapObject);
                }
            });

            if (Dialog.EditorLayerField.gameObject)
                TriggerHelper.AddEventTriggers(Dialog.EditorLayerField.gameObject, TriggerHelper.ScrollDeltaInt(Dialog.EditorLayerField, 1, 1, int.MaxValue));

            var editorLayerContextMenu = Dialog.EditorLayerField.gameObject.GetOrAddComponent<ContextClickable>();
            editorLayerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Go to Editor Layer", () => EditorTimeline.inst.SetLayer(beatmapObject.editorData.Layer, EditorTimeline.LayerType.Objects))
                    );
            };

            if (Dialog.EditorLayerToggles == null)
                return;

            for (int i = 0; i < Dialog.EditorLayerToggles.Length; i++)
            {
                var index = i;
                var toggle = Dialog.EditorLayerToggles[i];
                toggle.SetIsOnWithoutNotify(index == beatmapObject.editorData.Layer);
                toggle.onValueChanged.NewListener(_val =>
                {
                    beatmapObject.editorData.Layer = index;
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                    RenderLayers(beatmapObject);
                });
            }
        }

        /// <summary>
        /// Renders the Bin Slider.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderBin(BeatmapObject beatmapObject)
        {
            Dialog.BinSlider.onValueChanged.ClearAll();
            Dialog.BinSlider.maxValue = EditorTimeline.inst.BinCount;
            Dialog.BinSlider.value = beatmapObject.editorData.Bin;
            Dialog.BinSlider.onValueChanged.AddListener(_val =>
            {
                beatmapObject.editorData.Bin = Mathf.Clamp((int)_val, 0, EditorTimeline.inst.BinCount);

                // Since bin has no effect on the physical object, we will only need to update the timeline object.
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
            });
        }

        /// <summary>
        /// Renders the Index field.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderIndex(BeatmapObject beatmapObject)
        {
            if (!Dialog.EditorIndexField)
                return;

            Dialog.Content.Find("indexer_label").gameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.EditorIndexField.gameObject.SetActive(RTEditor.ShowModdedUI);

            if (!RTEditor.ShowModdedUI)
                return;

            var currentIndex = GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.id);
            Dialog.EditorIndexField.inputField.onEndEdit.ClearAll();
            Dialog.EditorIndexField.inputField.onValueChanged.ClearAll();
            Dialog.EditorIndexField.inputField.text = currentIndex.ToString();
            Dialog.EditorIndexField.inputField.onEndEdit.AddListener(_val =>
            {
                if (currentIndex < 0)
                {
                    EditorManager.inst.DisplayNotification($"Object is not in the Beatmap Object list.", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                if (int.TryParse(_val, out int index))
                {
                    index = Mathf.Clamp(index, 0, GameData.Current.beatmapObjects.Count - 1);
                    if (currentIndex == index)
                        return;

                    GameData.Current.beatmapObjects.Move(currentIndex, index);
                    EditorTimeline.inst.UpdateTransformIndex();
                    RenderIndex(beatmapObject);
                }
            });

            Dialog.EditorIndexField.leftGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, 0);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });
            Dialog.EditorIndexField.leftButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index <= 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, index - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });
            Dialog.EditorIndexField.rightButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index >= GameData.Current.beatmapObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, index + 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });
            Dialog.EditorIndexField.rightGreaterButton.onClick.NewListener(() =>
            {
                var index = GameData.Current.beatmapObjects.FindIndex(x => x == beatmapObject);
                if (index >= GameData.Current.beatmapObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(index, GameData.Current.beatmapObjects.Count - 1);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            });

            TriggerHelper.AddEventTriggers(Dialog.EditorIndexField.gameObject, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                if (!int.TryParse(Dialog.EditorIndexField.inputField.text, out int index))
                    return;

                if (pointerEventData.scrollDelta.y < 0f)
                    index -= (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);
                if (pointerEventData.scrollDelta.y > 0f)
                    index += (Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10 : 1);

                if (index < 0)
                {
                    EditorManager.inst.DisplayNotification("Could not move object back since it's already at the start.", 3f, EditorManager.NotificationType.Error);
                    return;
                }
                if (index > GameData.Current.beatmapObjects.Count - 1)
                {
                    EditorManager.inst.DisplayNotification("Could not move object forwards since it's already at the end.", 3f, EditorManager.NotificationType.Error);
                    return;
                }

                GameData.Current.beatmapObjects.Move(currentIndex, index);
                EditorTimeline.inst.UpdateTransformIndex();
                RenderIndex(beatmapObject);
            }));

            var contextMenu = Dialog.EditorIndexField.inputField.gameObject.GetOrAddComponent<ContextClickable>();
            contextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex <= 0)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var prevObject = GameData.Current.beatmapObjects[currentIndex - 1];

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Previous", () =>
                    {
                        if (currentIndex >= GameData.Current.beatmapObjects.Count - 1)
                        {
                            EditorManager.inst.DisplayNotification($"There are no previous objects to select.", 2f, EditorManager.NotificationType.Error);
                            return;
                        }

                        var nextObject = GameData.Current.beatmapObjects[currentIndex + 1];

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Select First", () =>
                    {
                        if (GameData.Current.beatmapObjects.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"There are no Beatmap Objects!", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        var prevObject = GameData.Current.beatmapObjects.First();

                        if (!prevObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(prevObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }),
                    new ButtonFunction("Select Last", () =>
                    {
                        if (GameData.Current.beatmapObjects.IsEmpty())
                        {
                            EditorManager.inst.DisplayNotification($"There are no Beatmap Objects!", 3f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        var nextObject = GameData.Current.beatmapObjects.Last();

                        if (!nextObject)
                            return;

                        var timelineObject = EditorTimeline.inst.GetTimelineObject(nextObject);

                        if (timelineObject)
                            EditorTimeline.inst.SetCurrentObject(timelineObject, EditorConfig.Instance.BringToSelection.Value);
                    }));
            };
        }

        /// <summary>
        /// Renders the Editor Colors.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderEditorColors(BeatmapObject beatmapObject)
        {
            Dialog.BaseColorField.SetTextWithoutNotify(beatmapObject.editorData.color);
            Dialog.BaseColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.color = _val;
                beatmapObject.timelineObject?.RenderVisibleState(false);
            });
            var baseColorContextMenu = Dialog.BaseColorField.gameObject.GetOrAddComponent<ContextClickable>();
            baseColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.BaseColorField, beatmapObject.editorData.color);
            };

            Dialog.SelectColorField.SetTextWithoutNotify(beatmapObject.editorData.selectedColor);
            Dialog.SelectColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.selectedColor = _val;
                beatmapObject.timelineObject?.RenderVisibleState(false);
            });
            var selectColorContextMenu = Dialog.SelectColorField.gameObject.GetOrAddComponent<ContextClickable>();
            selectColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.SelectColorField, beatmapObject.editorData.selectedColor);
            };

            Dialog.TextColorField.SetTextWithoutNotify(beatmapObject.editorData.textColor);
            Dialog.TextColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.textColor = _val;
                beatmapObject.timelineObject?.RenderText(beatmapObject.name);
            });
            var textColorContextMenu = Dialog.TextColorField.gameObject.GetOrAddComponent<ContextClickable>();
            textColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.TextColorField, beatmapObject.editorData.textColor);
            };

            Dialog.MarkColorField.SetTextWithoutNotify(beatmapObject.editorData.markColor);
            Dialog.MarkColorField.onValueChanged.NewListener(_val =>
            {
                beatmapObject.editorData.markColor = _val;
                beatmapObject.timelineObject?.RenderText(beatmapObject.name);
            });
            var markColorContextMenu = Dialog.MarkColorField.gameObject.GetOrAddComponent<ContextClickable>();
            markColorContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                    beatmapObject.timelineObject?.ShowColorContextMenu(Dialog.MarkColorField, beatmapObject.editorData.markColor);
            };
        }

        /// <summary>
        /// Renders the custom keyframe UI.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderCustomUIDisplay(BeatmapObject beatmapObject)
        {
            Dialog.keyframeDialogs[0].InitCustomUI(
                beatmapObject.editorData.GetDisplay("position/x", CustomUIDisplay.DefaultPositionXDisplay),
                beatmapObject.editorData.GetDisplay("position/y", CustomUIDisplay.DefaultPositionYDisplay),
                beatmapObject.editorData.GetDisplay("position/z", CustomUIDisplay.DefaultPositionZDisplay));
            Dialog.keyframeDialogs[0].EventValueElements[2].GameObject.SetActive(RTEditor.ShowModdedUI);
            Dialog.keyframeDialogs[1].InitCustomUI(
                beatmapObject.editorData.GetDisplay("scale/x", CustomUIDisplay.DefaultScaleXDisplay),
                beatmapObject.editorData.GetDisplay("scale/y", CustomUIDisplay.DefaultScaleYDisplay));
            Dialog.keyframeDialogs[2].InitCustomUI(
                beatmapObject.editorData.GetDisplay("rotation/x", CustomUIDisplay.DefaultRotationDisplay));
        }

        /// <summary>
        /// Renders the Prefab references.
        /// </summary>
        /// <param name="beatmapObject">The BeatmapObject to set.</param>
        public void RenderPrefabReference(BeatmapObject beatmapObject)
        {
            bool fromPrefab = !string.IsNullOrEmpty(beatmapObject.prefabID);
            Dialog.CollapsePrefabLabel.SetActive(fromPrefab);
            Dialog.CollapsePrefabButton.gameObject.SetActive(fromPrefab);
            Dialog.CollapsePrefabButton.button.onClick.ClearAll();

            var prefab = beatmapObject.GetPrefab();
            Dialog.PrefabName.gameObject.SetActive(prefab);
            if (prefab)
                Dialog.PrefabNameText.text = $"[ <b>{prefab.name}</b> ]";

            var collapsePrefabContextMenu = Dialog.CollapsePrefabButton.button.gameObject.GetOrAddComponent<ContextClickable>();
            collapsePrefabContextMenu.onClick = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                {
                    if (EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
                    {
                        RTEditor.inst.ShowWarningPopup("Are you sure you want to collapse this Prefab group and save the changes to the Internal Prefab?", () =>
                        {
                            RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData);
                            RTEditor.inst.HideWarningPopup();
                        }, RTEditor.inst.HideWarningPopup);

                        return;
                    }

                    RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData);
                    return;
                }

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Apply", () => RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData)),
                    new ButtonFunction("Create New", () => RTPrefabEditor.inst.Collapse(beatmapObject, beatmapObject.editorData, true)),
                    new ButtonFunction(EditorConfig.Instance.ShowCollapsePrefabWarning.Value ? "Hide Warning" : "Show Warning", () => EditorConfig.Instance.ShowCollapsePrefabWarning.Value = !EditorConfig.Instance.ShowCollapsePrefabWarning.Value)
                    );
            };

            Dialog.AssignPrefabButton.button.onClick.NewListener(() =>
            {
                RTEditor.inst.selectingMultiple = false;
                RTEditor.inst.prefabPickerEnabled = true;
            });

            Dialog.RemovePrefabButton.button.onClick.NewListener(() =>
            {
                beatmapObject.RemovePrefabReference();
                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(beatmapObject));
                OpenDialog(beatmapObject);
            });
        }

        public void OpenImageSelector(BeatmapObject beatmapObject, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            string jpgFile = FileBrowser.OpenSingleFile("Select an image!", editorPath, new string[] { "png", "jpg" });
            SelectImage(jpgFile, beatmapObject, copyFile: copyFile, storeImage: storeImage);
        }

        public void StoreImage(BeatmapObject beatmapObject, string file)
        {
            if (RTFile.FileExists(file))
            {
                var imageData = File.ReadAllBytes(file);

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                GameData.Current.assets.AddSprite(beatmapObject.text, SpriteHelper.CreateSprite(texture2d));
            }
            else
            {
                var imageData = LegacyPlugin.PALogoSprite.texture.EncodeToPNG();

                var texture2d = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                texture2d.LoadImage(imageData);

                texture2d.wrapMode = TextureWrapMode.Clamp;
                texture2d.filterMode = FilterMode.Point;
                texture2d.Apply();

                GameData.Current.assets.AddSprite(beatmapObject.text, SpriteHelper.CreateSprite(texture2d));
            }
        }

        void SelectImage(string file, BeatmapObject beatmapObject, bool renderEditor = true, bool updateObject = true, bool copyFile = true, bool storeImage = false)
        {
            var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
            RTFile.CreateDirectory(RTFile.CombinePaths(editorPath, "images"));

            file = RTFile.ReplaceSlash(file);
            CoreHelper.Log($"Selected file: {file}");
            if (!RTFile.FileExists(file))
                return;
            
            string jpgFileLocation = RTFile.CombinePaths(editorPath, "images", Path.GetFileName(file));
            if (RTFile.FileExists(RTFile.CombinePaths(editorPath, Path.GetFileName(file))))
                jpgFileLocation = RTFile.CombinePaths(editorPath, Path.GetFileName(file));

            if (copyFile && (EditorConfig.Instance.OverwriteImportedImages.Value || !RTFile.FileExists(jpgFileLocation)) && !file.Contains(editorPath))
                RTFile.CopyFile(file, jpgFileLocation);

            beatmapObject.text = jpgFileLocation.Remove(editorPath + "/");

            if (storeImage)
                StoreImage(beatmapObject, file);

            // Since setting image has no affect on the timeline object, we will only need to update the physical object.
            if (updateObject && UpdateObjects)
                RTLevel.Current?.UpdateObject(beatmapObject, ObjectContext.IMAGE);

            if (renderEditor)
                RenderShape(beatmapObject);
        }

        #endregion

        #region Object Search

        public static int ObjectsPerPage { get; set; } = 100;
        public static int ParentObjectsPerPage { get; set; } = 100;

        public void ShowObjectSearch() => ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)));

        /// <summary>
        /// Shows a list of <see cref="BeatmapObject"/>s in the level.
        /// </summary>
        /// <param name="onSelect">Function to run when a button is clicked.</param>
        /// <param name="clearParent">If the Clear Parents button should render.</param>
        /// <param name="beatmapObjects">List of <see cref="BeatmapObject"/> to render.</param>
        public void ShowObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false, List<BeatmapObject> beatmapObjects = null)
        {
            if (!EditorLevelManager.inst.HasLoadedLevel())
                return;

            ObjectSearchPopup.Open();
            RefreshObjectSearch(onSelect, clearParent, beatmapObjects);
        }

        /// <summary>
        /// Refreshes the list of <see cref="BeatmapObject"/>s in the level.
        /// </summary>
        /// <param name="onSelect">Function to run when a button is clicked.</param>
        /// <param name="clearParent">If the Clear Parents button should render.</param>
        /// <param name="beatmapObjects">List of <see cref="BeatmapObject"/> to render.</param>
        public void RefreshObjectSearch(Action<BeatmapObject> onSelect, bool clearParent = false, List<BeatmapObject> beatmapObjects = null)
        {
            ObjectSearchPopup.SearchField.onValueChanged.NewListener(_val => RefreshObjectSearch(onSelect, clearParent, beatmapObjects));
            ObjectSearchPopup.ClearContent();
            ObjectSearchPopup.RenderPageField();

            if (clearParent && ObjectSearchPopup.Page == 0)
            {
                var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(ObjectSearchPopup.Content, "Clear Parents");
                var buttonText = buttonPrefab.transform.GetChild(0).GetComponent<Text>();
                buttonText.text = "Clear Parents";

                var button = buttonPrefab.GetComponent<Button>();
                button.onClick.NewListener(() =>
                {
                    foreach (var bm in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                    {
                        bm.Parent = "";
                        RTLevel.Current?.UpdateObject(bm, ObjectContext.PARENT_CHAIN);
                    }
                });

                var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                image.color = Color.red;
                image.sprite = EditorSprites.CloseSprite;

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(buttonText);
            }

            if (beatmapObjects == null)
                beatmapObjects = GameData.Current.beatmapObjects;

            var searchTerm = ObjectSearchPopup.SearchTerm;
            List<string> matchTags = null;
            if (!string.IsNullOrEmpty(searchTerm))
            {
                try
                {
                    var jn = JSON.Parse(searchTerm);
                    var jnTags = jn["tags"];
                    if (jnTags != null && jnTags.IsArray)
                    {
                        matchTags = new List<string>(jnTags.Count);
                        for (int i = 0; i < jnTags.Count; i++)
                            matchTags.Add(jnTags[i]);
                    }
                }
                catch
                {

                }
            }

            var list = beatmapObjects.FindAll(x => !x.fromPrefab);
            int index = 0;
            int pageIndex = 0;
            foreach (var beatmapObject in beatmapObjects)
            {
                if (beatmapObject.fromPrefab)
                    continue;

                if (matchTags != null)
                {
                    if (!beatmapObject.tags.Has(x => matchTags.Contains(x)))
                    {
                        index++;
                        continue;
                    }
                }
                else if (!RTString.SearchString(ObjectSearchPopup.SearchTerm, beatmapObject.name, new SearchMatcher(beatmapObject.id, SearchMatchType.Exact)))
                {
                    index++;
                    continue;
                }

                if (!ObjectSearchPopup.InPage(pageIndex, ObjectsPerPage))
                {
                    index++;
                    pageIndex++;
                    continue;
                }

                string nm = $"[{index + 1:0000}/{list.Count:0000} - {beatmapObject.id}] : {beatmapObject.name}";
                var buttonPrefab = EditorManager.inst.spriteFolderButtonPrefab.Duplicate(ObjectSearchPopup.Content, nm);
                var buttonText = buttonPrefab.transform.GetChild(0).GetComponent<Text>();
                buttonText.text = nm;

                var button = buttonPrefab.GetComponent<Button>();
                button.onClick.NewListener(() => onSelect?.Invoke(beatmapObject));

                var image = buttonPrefab.transform.Find("Image").GetComponent<Image>();
                image.color = RTEditor.GetObjectColor(beatmapObject, false);

                var shape = Mathf.Clamp(beatmapObject.shape, 0, ShapeManager.inst.Shapes2D.Count - 1);
                var shapeOption = Mathf.Clamp(beatmapObject.shapeOption, 0, ShapeManager.inst.Shapes2D[shape].Count - 1);

                image.sprite = ShapeManager.inst.Shapes2D[shape][shapeOption].icon;

                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(buttonText);

                #region Info

                var runtimeObject = beatmapObject.runtimeObject;

                if (!runtimeObject || !runtimeObject.visualObject || !runtimeObject.visualObject.gameObject)
                {
                    index++;
                    pageIndex++;
                    continue;
                }

                var transform = runtimeObject.visualObject.gameObject.transform;

                string parent = "";
                if (!string.IsNullOrEmpty(beatmapObject.Parent))
                    parent = "<br>P: " + beatmapObject.Parent + " (" + beatmapObject.parentType + ")";
                else
                    parent = "<br>P: No Parent" + " (" + beatmapObject.parentType + ")";

                string text = "";
                if (beatmapObject.shape != 4 || beatmapObject.shape != 6)
                    text = "<br>S: " + CoreHelper.GetShape(beatmapObject.shape, beatmapObject.shapeOption) +
                        "<br>T: " + beatmapObject.text;
                if (beatmapObject.shape == 4)
                    text = "<br>S: Text" +
                        "<br>T: " + beatmapObject.text;
                if (beatmapObject.shape == 6)
                    text = "<br>S: Image" +
                        "<br>T: " + beatmapObject.text;

                string ptr = "";
                if (!string.IsNullOrEmpty(beatmapObject.prefabID) && !string.IsNullOrEmpty(beatmapObject.prefabInstanceID))
                    ptr = "<br><#" + RTColors.ColorToHex(beatmapObject.GetPrefab().GetPrefabType().color) + ">PID: " + beatmapObject.prefabID + " | PIID: " + beatmapObject.prefabInstanceID + "</color>";
                else
                    ptr = "<br>Not from prefab";

                var desc = "N/ST: " + beatmapObject.name + " [ " + beatmapObject.StartTime + " ]";
                var hint = "ID: {" + beatmapObject.id + "}" +
                    parent +
                    "<br>Alive: " + beatmapObject.Alive.ToString() +
                    "<br>Origin: {X: " + beatmapObject.origin.x + ", Y: " + beatmapObject.origin.y + "}" +
                    text +
                    "<br>Depth: " + beatmapObject.Depth +
                    "<br>ED: {L: " + beatmapObject.editorData.Layer + ", B: " + beatmapObject.editorData.Bin + "}" +
                    "<br>POS: {X: " + transform.position.x + ", Y: " + transform.position.y + "}" +
                    "<br>SCA: {X: " + transform.localScale.x + ", Y: " + transform.localScale.y + "}" +
                    "<br>ROT: " + transform.eulerAngles.z +
                    "<br>COL: " + "<#" + RTColors.ColorToHex(RTEditor.GetObjectColor(beatmapObject, false)) + ">" + "█ <b>#" + RTColors.ColorToHex(RTEditor.GetObjectColor(beatmapObject, true)) + "</b></color>" +
                    ptr;

                TooltipHelper.AddHoverTooltip(buttonPrefab, desc, hint);

                #endregion

                index++;
                pageIndex++;
            }
        }

        /// <summary>
        /// Shows the parent search.
        /// </summary>
        public void ShowParentSearch() => ShowParentSearch(EditorTimeline.inst.CurrentSelection);

        /// <summary>
        /// Shows the parent search.
        /// </summary>
        /// <param name="timelineObject">The object to parent.</param>
        public void ShowParentSearch(TimelineObject timelineObject)
        {
            RTEditor.inst.ParentSelectorPopup.Open();
            RefreshParentSearch(timelineObject);
        }

        /// <summary>
        /// Refrehes the parent search.
        /// </summary>
        /// <param name="timelineObject">The object to parent.</param>
        public void RefreshParentSearch(TimelineObject timelineObject)
        {
            RTEditor.inst.ParentSelectorPopup.SearchField.onValueChanged.NewListener(_val => RefreshParentSearch(timelineObject));
            RTEditor.inst.ParentSelectorPopup.ClearContent();
            RTEditor.inst.ParentSelectorPopup.RenderPageField();

            var noParent = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ParentSelectorPopup.Content, "No Parent");
            var noParentStorage = noParent.GetComponent<FunctionButtonStorage>();
            noParentStorage.Text = "No Parent";
            noParentStorage.OnClick.NewListener(() =>
            {
                var list = EditorTimeline.inst.SelectedObjects;
                foreach (var timelineObject in list)
                {
                    if (timelineObject.isPrefabObject)
                        timelineObject.GetData<PrefabObject>().SetParent(string.Empty, false);
                    if (timelineObject.isBeatmapObject)
                        timelineObject.GetData<BeatmapObject>().SetParent(string.Empty, false);
                }

                RTLevel.Current?.RecalculateObjectStates();
                RTEditor.inst.ParentSelectorPopup.Close();
                if (list.Count == 1 && timelineObject.isBeatmapObject)
                    RenderDialog(timelineObject.GetData<BeatmapObject>());
                if (list.Count == 1 && timelineObject.isPrefabObject)
                    RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
            });

            EditorThemeManager.ApplySelectable(noParentStorage.button, ThemeGroup.List_Button_1);
            EditorThemeManager.ApplyLightText(noParentStorage.label);

            if (RTString.SearchString(RTEditor.inst.ParentSelectorPopup.SearchTerm, "camera") && RTEditor.inst.ParentSelectorPopup.Page == 0)
            {
                var cam = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ParentSelectorPopup.Content, "Camera");
                var camStorage = cam.GetComponent<FunctionButtonStorage>();

                camStorage.Text = "Camera";
                camStorage.OnClick.NewListener(() =>
                {
                    var list = EditorTimeline.inst.SelectedObjects;
                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.isPrefabObject)
                            timelineObject.GetData<PrefabObject>().SetParent(BeatmapObject.CAMERA_PARENT, false);
                        if (timelineObject.isBeatmapObject)
                            timelineObject.GetData<BeatmapObject>().SetParent(BeatmapObject.CAMERA_PARENT, false);
                    }

                    RTLevel.Current?.RecalculateObjectStates();
                    RTEditor.inst.ParentSelectorPopup.Close();
                    if (list.Count == 1 && timelineObject.isBeatmapObject)
                        RenderDialog(timelineObject.GetData<BeatmapObject>());
                    if (list.Count == 1 && timelineObject.isPrefabObject)
                        RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());
                });

                EditorThemeManager.ApplySelectable(camStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(camStorage.label);
            }

            int index = 0;
            int pageIndex = 0;
            foreach (var beatmapObject in GameData.Current.beatmapObjects)
            {
                if (beatmapObject.fromPrefab)
                    continue;

                if (timelineObject.isBeatmapObject && !timelineObject.GetData<BeatmapObject>().CanParent(beatmapObject))
                {
                    index++;
                    pageIndex++;
                    continue;
                }

                if (!RTString.SearchString(RTEditor.inst.ParentSelectorPopup.SearchTerm, beatmapObject.name, index, new SearchMatcher(beatmapObject.id, SearchMatchType.Exact)))
                {
                    index++;
                    continue;
                }

                if (!RTEditor.inst.ParentSelectorPopup.InPage(pageIndex, ParentObjectsPerPage))
                {
                    index++;
                    pageIndex++;
                    continue;
                }

                string s = $"{beatmapObject.name} {index:0000}";
                var objectToParent = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.ParentSelectorPopup.Content, s);
                var storage = objectToParent.GetComponent<FunctionButtonStorage>();

                storage.Text = s;
                storage.OnClick.NewListener(() =>
                {
                    string id = beatmapObject.id;

                    var list = EditorTimeline.inst.SelectedObjects;
                    foreach (var timelineObject in list)
                    {
                        if (timelineObject.isPrefabObject)
                            timelineObject.GetData<PrefabObject>().SetParent(beatmapObject, false);
                        if (timelineObject.isBeatmapObject)
                            timelineObject.GetData<BeatmapObject>().SetParent(beatmapObject, false);
                    }

                    RTLevel.Current?.RecalculateObjectStates();

                    RTEditor.inst.ParentSelectorPopup.Close();
                    if (list.Count == 1 && timelineObject.isBeatmapObject)
                        RenderDialog(timelineObject.GetData<BeatmapObject>());
                    if (list.Count == 1 && timelineObject.isPrefabObject)
                        RTPrefabEditor.inst.RenderPrefabObjectDialog(timelineObject.GetData<PrefabObject>());

                    Debug.Log($"{EditorManager.inst.className}Set Parent ID: {id}");
                });

                EditorThemeManager.ApplySelectable(storage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(storage.label);

                index++;
                pageIndex++;
            }
        }

        #endregion
    }
}

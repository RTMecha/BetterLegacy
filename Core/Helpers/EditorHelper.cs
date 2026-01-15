using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using SimpleJSON;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Timeline;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Helpers
{
    public static class EditorHelper
    {
        public const string FILE_DROPDOWN = "File";
        public const string EDIT_DROPDOWN = "Edit";
        public const string VIEW_DROPDOWN = "View";
        public const string SETTINGS_DROPDOWN = "Settings";
        public const string UPLOAD_DROPDOWN = "Steam";
        public const string HELP_DROPDOWN = "Help";

        public static string[] dropdownNames = new string[]
        {
            FILE_DROPDOWN,
            EDIT_DROPDOWN,
            VIEW_DROPDOWN,
            SETTINGS_DROPDOWN,
            UPLOAD_DROPDOWN,
            HELP_DROPDOWN,
        };
        
        public static string[] dropdownDisplayNames = new string[]
        {
            FILE_DROPDOWN,
            EDIT_DROPDOWN,
            VIEW_DROPDOWN,
            SETTINGS_DROPDOWN,
            "Upload",
            HELP_DROPDOWN,
        };

        /// <summary>
        /// complexity.json file that stores complexity values.
        /// </summary>
        public static JSONNode complexityJSON;

        /// <summary>
        /// Applies a complexity value to an object.
        /// </summary>
        /// <param name="gameObject">Unity Game Object to apply the complexity to.</param>
        /// <param name="complexity">Complexity to set.</param>
        /// <param name="onlySpecificComplexity">If only the specific complexity should be used.</param>
        /// <param name="visible">Additional visible check function.</param>
        /// <param name="onUpdate">Function to run per complexity update.</param>
        /// <param name="autoSpecify">If <paramref name="onlySpecificComplexity"/> is automatically specified as simple.</param>
        public static void SetComplexity(GameObject gameObject, Complexity complexity, bool onlySpecificComplexity = false, Func<bool> visible = null, Action<ComplexityObject> onUpdate = null, bool autoSpecify = true)
        {
            if (!gameObject)
                return;

            var obj = gameObject.GetOrAddComponent<ComplexityObject>();
            obj.complexity = complexity;
            obj.onlySpecificComplexity = autoSpecify ? complexity == Complexity.Simple || onlySpecificComplexity : onlySpecificComplexity;
            obj.visible = visible;
            obj.onUpdate = onUpdate;
            obj.UpdateActiveState();
        }

        /// <summary>
        /// Applies a complexity value from a path in the complexity.json file to an object.
        /// </summary>
        /// <param name="gameObject">Unity Game Object to apply the complexity to.</param>
        /// <param name="path">Path of the complexity.</param>
        /// <param name="defaultComplexity">The default complexity to use if the path doesn't exist.</param>
        /// <param name="defaultOnlySpecificComplexity">The default specific complexity check if the path doesn't exist.</param>
        /// <param name="visible">Additional visible check function.</param>
        /// <param name="onUpdate">Function to run per complexity update.</param>
        public static void SetComplexity(GameObject gameObject, string path, Complexity defaultComplexity, bool defaultOnlySpecificComplexity = false, Func<bool> visible = null, Action<ComplexityObject> onUpdate = null)
        {
            if (!gameObject)
                return;

            if (complexityJSON == null)
            {
                SetComplexity(gameObject, defaultComplexity, defaultOnlySpecificComplexity, visible, onUpdate, false);
                return;
            }

            var referenceJSON = complexityJSON[path];
            if (referenceJSON == null)
            {
                SetComplexity(gameObject, defaultComplexity, defaultOnlySpecificComplexity, visible, onUpdate, false);
                return;
            }

            var obj = gameObject.GetOrAddComponent<ComplexityObject>();
            obj.complexity = (Complexity)referenceJSON["complexity"].AsInt;
            obj.onlySpecificComplexity = referenceJSON["only_this"].AsBool;
            obj.disabled = referenceJSON["disabled"].AsBool;
            if (referenceJSON["active"] != null)
                obj.active = referenceJSON["active"].AsBool;
            obj.path = path;
            obj.visible = visible;
            obj.onUpdate = onUpdate;
            obj.UpdateActiveState();
        }

        /// <summary>
        /// Checks if a complexity is active.
        /// </summary>
        /// <param name="complexity">Complexity to check.</param>
        /// <param name="onlySpecificComplexity">If only the specific complexity should be used.</param>
        /// <returns>Returns <see langword="true"/> if the <see cref="EditorConfig.EditorComplexity"/> setting matches <paramref name="complexity"/>, otherwise returns <see langword="false"/>.</returns>
        public static bool CheckComplexity(Complexity complexity, bool onlySpecificComplexity = false) => onlySpecificComplexity ?
                    complexity == EditorConfig.Instance.EditorComplexity.Value :
                    complexity <= EditorConfig.Instance.EditorComplexity.Value;

        /// <summary>
        /// Gets a complexity value from the complexity.json file.
        /// </summary>
        /// <param name="path">Path to the complexity value.</param>
        /// <param name="defaultComplexity">The default complexity.</param>
        /// <returns>Returns the complexity value found in the complexity.json file if the path exists, otherwise returns <paramref name="defaultComplexity"/>.</returns>
        public static Complexity GetComplexity(string path, Complexity defaultComplexity)
        {
            if (complexityJSON == null)
                return defaultComplexity;
            var referenceJSON = complexityJSON[path];
            if (referenceJSON == null)
                return defaultComplexity;
            return (Complexity)referenceJSON["complexity"].AsInt;
        }

        public static void AddEditorPopup(string name, GameObject gameObject)
        {
            EditorManager.inst.EditorDialogs.Add(new EditorManager.EditorDialog
            {
                Dialog = gameObject.transform,
                Name = name,
                Type = EditorManager.EditorDialog.DialogType.Popup
            });
            EditorManager.inst.RefreshDialogDictionary();
        }

        public static void AddEditorDialog(string name, GameObject gameObject)
        {
            EditorManager.inst.EditorDialogs.Add(new EditorManager.EditorDialog
            {
                Dialog = gameObject.transform,
                Name = name,
                Type = EditorManager.EditorDialog.DialogType.Settings
            });
            EditorManager.inst.RefreshDialogDictionary();
        }

        public static GameObject AddEditorDropdown(string name, string key, string dropdown, Sprite sprite, Action action, int siblingIndex = -1)
        {
            if (!RTEditor.inst.titleBar.Find($"{dropdown}"))
                return null;

            var parent = RTEditor.inst.titleBar.Find($"{dropdown}/{dropdown} Dropdown");

            var gameObject = RTEditor.inst.titleBar.Find("Edit/Edit Dropdown/Cut").gameObject.Duplicate(parent, name, siblingIndex < 0 ? parent.childCount : siblingIndex);
            gameObject.transform.Find("Text").GetComponent<Text>().text = name;
            gameObject.transform.Find("Text").AsRT().sizeDelta = new Vector2(224f, 0f);
            gameObject.transform.Find("Text 1").GetComponent<Text>().text = key;

            var propWinButton = gameObject.GetComponent<Button>();
            propWinButton.onClick.NewListener(action);

            gameObject.SetActive(true);

            var image = gameObject.transform.Find("Image").GetComponent<Image>();
            gameObject.transform.Find("Image").GetComponent<Image>().sprite = sprite;

            EditorThemeManager.ApplySelectable(propWinButton, ThemeGroup.Title_Bar_Dropdown, false);
            EditorThemeManager.ApplyGraphic(gameObject.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Title_Bar_Text);
            EditorThemeManager.ApplyGraphic(image, ThemeGroup.Title_Bar_Text);

            return gameObject;
        }

        public static void AddInputFieldContextMenu(InputField name)
        {
            if (!name.GetComponent<ContextClickable>() && name.gameObject)
            {
                var contextClickable = name.gameObject.AddComponent<ContextClickable>();
                contextClickable.onClick = eventData =>
                {
                    if (eventData.button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonElement("Flip Left/Right", () => name.text = RTString.FlipLeftRight(name.text)),
                        new ButtonElement("Flip Up/Down", () => name.text = RTString.FlipUpDown(name.text)),
                        new ButtonElement("Flip Upper/Lower", () => name.text = RTString.FlipUpperLower(name.text)),
                        new SpacerElement(),
                        new ButtonElement("Flip Number", () =>
                        {
                            RTString.RegexMatches(name.text, new Regex(@"([-0-9]+)"), match =>
                            {
                                int num = Parser.TryParse(match.Groups[1].ToString(), 0);
                                num = -num;
                                name.text = name.text.Replace(match.Groups[1].ToString(), num.ToString());
                            });
                        }),
                        new ButtonElement("Increase Number", () =>
                        {
                            RTString.RegexMatches(name.text, new Regex(@"([-0-9]+)"), match =>
                            {
                                int num = Parser.TryParse(match.Groups[1].ToString(), 0);
                                num++;
                                name.text = name.text.Replace(match.Groups[1].ToString(), num.ToString());
                            });
                        }),
                        new ButtonElement("Decrease Number", () =>
                        {
                            RTString.RegexMatches(name.text, new Regex(@"([-0-9]+)"), match =>
                            {
                                int num = Parser.TryParse(match.Groups[1].ToString(), 0);
                                num--;
                                name.text = name.text.Replace(match.Groups[1].ToString(), num.ToString());
                            });
                        }),
                        new SpacerElement(),
                        new ButtonElement("To Lower", () => name.text = name.text.ToLower()),
                        new ButtonElement("To Upper", () => name.text = name.text.ToUpper())
                        );
                };
            }
        }

        public static void LogAvailableInstances<T>()
        {
            Debug.Log($"------ {typeof(T)} ------\n{typeof(PrefabEditor)} is null: {PrefabEditor.inst == null}\n" +
                $"{typeof(EditorManager)} is null: {EditorManager.inst == null}\n" +
                $"{typeof(MarkerEditor)} is null: {MarkerEditor.inst == null}\n" +
                $"{typeof(ObjEditor)} is null: {ObjEditor.inst == null}\n" +
                $"{typeof(EventEditor)} is null: {EventEditor.inst == null}\n" +
                $"{typeof(BackgroundEditor)} is null: {BackgroundEditor.inst == null}\n" +
                $"{typeof(CheckpointEditor)} is null: {CheckpointEditor.inst == null}\n");

        }

        public static void LogIsNull<T>(string message, object obj) => Debug.Log($"{message}{typeof(T)} is null: {obj == null}");

        //EditorHelper.LoadLevel("C:/Users/Mecha/Desktop/Project Launcher/instances/Mod Testing/beatmaps/editor/RhythmTech/Apocrypha but Platformer")
        public static void LoadLevel(string fullPath) => SceneHelper.LoadScene(SceneName.Editor, scene => EditorLevelManager.inst.LoadLevel(new Level(fullPath)));

        public static IEnumerator ILoadLevel(string fullPath, float delay = 2f)
        {
            if (!ProjectArrhythmia.State.InEditor)
                yield return SceneHelper.ILoadScene(SceneName.Editor);

            if (delay != 0.0)
                yield return CoroutineHelper.Seconds(delay);

            EditorLevelManager.inst.LoadLevel(new Level(fullPath));
        }

        public static bool SelectAllObjects()
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            if (EditorTimeline.inst.timelineObjects.Count == 1)
                EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.timelineObjects[0]);
            else if (EditorTimeline.inst.timelineObjects.Count > 1)
            {
                for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
                    EditorTimeline.inst.timelineObjects[i].Selected = true;

                EditorTimeline.inst.CurrentSelection = EditorTimeline.inst.timelineObjects.Last();

                EditorManager.inst.ClearPopups();
                MultiObjectEditor.inst.Dialog.Open();
            }

            Example.Current?.brain?.Interact(ExampleBrain.Interactions.SELECT_OBJECTS_COMMAND);

            return true;
        }

        public static bool SelectAllObjectsOnCurrentLayer()
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            var layer = EditorTimeline.inst.Layer;

            EditorTimeline.inst.DeselectAllObjects();

            if (EditorTimeline.inst.timelineObjects.Count == 1)
            {
                if (EditorTimeline.inst.timelineObjects[0].Layer == layer)
                    EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.timelineObjects[0]);
            }
            else if (EditorTimeline.inst.timelineObjects.Count > 1)
            {
                for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
                    if (EditorTimeline.inst.timelineObjects[i].Layer == layer)
                        EditorTimeline.inst.timelineObjects[i].Selected = true;

                EditorTimeline.inst.CurrentSelection = EditorTimeline.inst.timelineObjects.Last();

                EditorManager.inst.ClearPopups();
                MultiObjectEditor.inst.Dialog.Open();
            }

            return true;
        }

        public static bool SelectAllObjectsFromGroup(string editorGroup)
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            int selectedCount = 0;
            for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineObjects[i];
                timelineObject.Selected = timelineObject.EditorData && timelineObject.EditorData.editorGroup == editorGroup;
                if (!timelineObject.Selected)
                    continue;

                EditorTimeline.inst.CurrentSelection = timelineObject;
                selectedCount++;
            }
            EditorTimeline.inst.HandleSelection(EditorTimeline.inst.SelectedObjects);
            return true;
        }

        public static bool SelectAllObjectsFromPrefab(Prefab prefab)
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            int selectedCount = 0;
            for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineObjects[i];
                timelineObject.Selected = timelineObject.TryGetPrefabable(out IPrefabable prefabable) && prefabable.PrefabID == prefab.id;
                if (!timelineObject.Selected)
                    continue;

                EditorTimeline.inst.CurrentSelection = timelineObject;
                selectedCount++;
            }
            EditorTimeline.inst.HandleSelection(EditorTimeline.inst.SelectedObjects);
            return true;
        }
        
        public static bool SelectAllObjectsFromPrefabInstance(IPrefabable main)
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            int selectedCount = 0;
            for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineObjects[i];
                timelineObject.Selected = timelineObject.TryGetPrefabable(out IPrefabable prefabable) && main.SamePrefabInstance(prefabable);
                if (!timelineObject.Selected)
                    continue;

                EditorTimeline.inst.CurrentSelection = timelineObject;
                selectedCount++;
            }
            EditorTimeline.inst.HandleSelection(EditorTimeline.inst.SelectedObjects);
            return true;
        }

        public static bool MirrorSelectedObjects()
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            for (int i = 0; i < 3; i++)
                            {
                                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                                {
                                    beatmapObject.events[i][j].values[0] = -beatmapObject.events[i][j].values[0];
                                    beatmapObject.events[i][j].randomValues[0] = -beatmapObject.events[i][j].randomValues[0];
                                }
                            }

                            beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[0] = -prefabObject.events[0].values[0];
                            prefabObject.events[1].values[0] = -prefabObject.events[1].values[0];
                            prefabObject.events[2].values[0] = -prefabObject.events[2].values[0];
                            prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                            backgroundObject.pos.x = -backgroundObject.pos.x;
                            backgroundObject.scale.x = -backgroundObject.scale.x;
                            backgroundObject.rot = -backgroundObject.rot;
                            break;
                        }
                }
            }

            return true;
        }

        public static bool FlipSelectedObjects()
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();
                            for (int i = 0; i < 2; i++)
                            {
                                for (int j = 0; j < beatmapObject.events[i].Count; j++)
                                {
                                    beatmapObject.events[i][j].values[1] = -beatmapObject.events[i][j].values[1];
                                    beatmapObject.events[i][j].randomValues[1] = -beatmapObject.events[i][j].randomValues[1];
                                }
                            }

                            beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            var prefabObject = timelineObject.GetData<PrefabObject>();
                            prefabObject.events[0].values[1] = -prefabObject.events[0].values[1];
                            prefabObject.events[1].values[1] = -prefabObject.events[1].values[1];
                            prefabObject.GetParentRuntime()?.UpdatePrefab(prefabObject, PrefabObjectContext.TRANSFORM_OFFSET);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            var backgroundObject = timelineObject.GetData<BackgroundObject>();
                            backgroundObject.pos.y = -backgroundObject.pos.y;
                            backgroundObject.scale.y = -backgroundObject.scale.y;
                            break;
                        }
                }
            }

            return true;
        }

        public static bool RefreshKeyframesFromSelection()
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var beatmapObject in EditorTimeline.inst.SelectedObjects.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                beatmapObject.GetParentRuntime()?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);

            return true;
        }

        public static bool FixSmoothShakeKeyframes()
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            var gameData = GameData.Current;
            for (int i = 0; i < gameData.events[3].Count; i++)
                gameData.events[3][i].values[3] = 0f;

            return true;
        }

        public static void PasteKeyframes()
        {
            var kfs = KeyframeTimeline.copiedObjectKeyframes;

            if (kfs.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("No copied keyframes yet!", 2f, EditorManager.NotificationType.Warning);
                return;
            }
            foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
            {
                var ids = new List<string>();
                for (int i = 0; i < beatmapObject.events.Count; i++)
                {
                    beatmapObject.events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                    {
                        var kf = ObjectEditor.inst.Dialog.Timeline.PasteKF(beatmapObject, x);
                        ids.Add(kf.id);
                        return kf;
                    }));
                    beatmapObject.events[i].Sort((a, b) => a.time.CompareTo(b.time));
                }

                var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);
                if (EditorConfig.Instance.SelectPasted.Value)
                {
                    foreach (var kf in beatmapObject.TimelineKeyframes)
                        kf.Selected = ids.Contains(kf.ID);
                }

                EditorTimeline.inst.RenderTimelineObject(timelineObject);

                var runtime = beatmapObject.GetParentRuntime();
                runtime?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                runtime?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
            }
        }

        public static void RepeatPasteKeyframes(int repeatCount, float repeatOffsetTime)
        {
            var kfs = KeyframeTimeline.copiedObjectKeyframes;

            if (kfs.IsEmpty())
            {
                EditorManager.inst.DisplayNotification("No copied keyframes yet!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            foreach (var beatmapObject in EditorTimeline.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
            {
                float t = 0f;
                var ids = new List<string>();
                for (int repeat = 0; repeat < Mathf.Clamp(repeatCount + 1, 0, int.MaxValue); repeat++)
                {
                    for (int i = 0; i < beatmapObject.events.Count; i++)
                    {
                        beatmapObject.events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                        {
                            var kf = ObjectEditor.inst.Dialog.Timeline.PasteKF(beatmapObject, x);
                            kf.time += t;
                            ids.Add(kf.id);
                            return kf;
                        }));
                        beatmapObject.events[i].Sort((a, b) => a.time.CompareTo(b.time));
                    }

                    t += Mathf.Clamp(repeatOffsetTime, 0f, float.PositiveInfinity);
                }

                var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);
                if (EditorConfig.Instance.SelectPasted.Value)
                {
                    foreach (var kf in beatmapObject.TimelineKeyframes)
                        kf.Selected = ids.Contains(kf.ID);
                }

                EditorTimeline.inst.RenderTimelineObject(timelineObject);

                var runtime = beatmapObject.GetParentRuntime();
                runtime?.UpdateObject(beatmapObject, ObjectContext.KEYFRAMES);
                runtime?.UpdateObject(beatmapObject, ObjectContext.AUTOKILL);
            }
        }

        public static void AddSelectedObjectIndexes(int amount)
        {
            var selected = EditorTimeline.inst.SelectedBeatmapObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var index = GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.id);
                if (index < 0)
                    continue;

                GameData.Current.beatmapObjects.Move(index, Mathf.Clamp(index + amount, 0, GameData.Current.beatmapObjects.Count - 1));
            }

            selected = EditorTimeline.inst.SelectedPrefabObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var index = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
                if (index < 0)
                    continue;

                GameData.Current.prefabObjects.Move(index, Mathf.Clamp(index + amount, 0, GameData.Current.prefabObjects.Count - 1));
            }
            
            selected = EditorTimeline.inst.SelectedBackgroundObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var backgroundObject = timelineObject.GetData<BackgroundObject>();
                var index = GameData.Current.backgroundObjects.FindIndex(x => x.id == backgroundObject.id);
                if (index < 0)
                    continue;

                GameData.Current.backgroundObjects.Move(index, Mathf.Clamp(index + amount, 0, GameData.Current.backgroundObjects.Count - 1));
            }

            EditorTimeline.inst.UpdateTransformIndex();
        }

        public static void SetSelectedObjectIndexes(int amount)
        {
            var selected = EditorTimeline.inst.SelectedBeatmapObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var index = GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.id);
                if (index < 0)
                    continue;

                GameData.Current.beatmapObjects.Move(index, Mathf.Clamp(amount, 0, GameData.Current.beatmapObjects.Count - 1));
            }

            selected = EditorTimeline.inst.SelectedPrefabObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var index = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
                if (index < 0)
                    continue;

                GameData.Current.prefabObjects.Move(index, Mathf.Clamp(amount, 0, GameData.Current.prefabObjects.Count - 1));
            }
            
            selected = EditorTimeline.inst.SelectedBackgroundObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var backgroundObject = timelineObject.GetData<BackgroundObject>();
                var index = GameData.Current.backgroundObjects.FindIndex(x => x.id == backgroundObject.id);
                if (index < 0)
                    continue;

                GameData.Current.backgroundObjects.Move(index, Mathf.Clamp(amount, 0, GameData.Current.backgroundObjects.Count - 1));
            }

            EditorTimeline.inst.UpdateTransformIndex();
        }

        public static void ReverseSelectedObjectIndexes()
        {
            var selected = EditorTimeline.inst.SelectedBeatmapObjects.Order(x => x.Index, false);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var index = GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.id);
                if (index < 0)
                    continue;

                GameData.Current.beatmapObjects.Move(index, Mathf.Clamp(index - selected.Count, 0, GameData.Current.beatmapObjects.Count - 1));
            }

            selected = EditorTimeline.inst.SelectedPrefabObjects.Order(x => x.Index, false);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var index = GameData.Current.prefabObjects.FindIndex(x => x.id == prefabObject.id);
                if (index < 0)
                    continue;

                GameData.Current.prefabObjects.Move(index, Mathf.Clamp(index - selected.Count, 0, GameData.Current.prefabObjects.Count - 1));
            }

            selected = EditorTimeline.inst.SelectedBackgroundObjects.Order(x => x.Index, false);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var backgroundObject = timelineObject.GetData<BackgroundObject>();
                var index = GameData.Current.backgroundObjects.FindIndex(x => x.id == backgroundObject.id);
                if (index < 0)
                    continue;

                GameData.Current.backgroundObjects.Move(index, Mathf.Clamp(index - selected.Count, 0, GameData.Current.backgroundObjects.Count - 1));
            }

            EditorTimeline.inst.UpdateTransformIndex();
        }

        public static bool SetSelectedObjectPrefabGroupOnly(bool enabled)
        {
            if (!GameData.Current || !ProjectArrhythmia.State.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            timelineObject.GetData<BeatmapObject>().modifiers.ForLoop(modifier =>
                            {
                                var name = modifier.Name;
                                if (ModifiersHelper.IsGroupModifier(name))
                                    modifier.prefabInstanceOnly = enabled;
                            });
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            timelineObject.GetData<BackgroundObject>().modifiers.ForLoop(modifier =>
                            {
                                var name = modifier.Name;
                                if (ModifiersHelper.IsGroupModifier(name))
                                    modifier.prefabInstanceOnly = enabled;
                            });
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            timelineObject.GetData<PrefabObject>().modifiers.ForLoop(modifier =>
                            {
                                var name = modifier.Name;
                                if (ModifiersHelper.IsGroupModifier(name))
                                    modifier.prefabInstanceOnly = enabled;
                            });
                            break;
                        }
                }
            }

            return true;
        }

        public static void SaveDefaultPrefabTypes()
        {
            var jn = Parser.NewJSONObject();

            int index = 0;
            for (int i = 0; i < RTPrefabEditor.inst.prefabTypes.Count; i++)
            {
                var prefabType = RTPrefabEditor.inst.prefabTypes[i];
                if (!prefabType.isDefault)
                    continue;
                jn["prefab_types"][index] = prefabType.ToJSON();

                index++;
            }

            RTFile.WriteToFile(RTFile.GetAsset("builtin/default_prefabtypes" + FileFormat.LSPT.Dot()), jn.ToString(3));
        }

        public static void ModifyEditorThemes(Action<EditorTheme> action)
        {
            EditorThemeManager.EditorThemes.ForLoop(action);

            EditorThemeManager.SaveEditorThemes();
            EditorThemeManager.RenderElements();
        }

        public static void ModifyEditorTheme(EditorThemeType editorThemeType, ThemeGroup themeGroup, Color color) => ModifyEditorTheme(editorThemeType.Name.ToString(), themeGroup, color);

        public static void ModifyEditorTheme(string name, ThemeGroup themeGroup, Color color)
        {
            if (!EditorThemeManager.EditorThemes.TryFind(x => x.name == name, out EditorTheme editorTheme))
                return;

            editorTheme.ColorGroups[themeGroup] = color;

            EditorThemeManager.SaveEditorThemes();
            EditorThemeManager.RenderElements();
        }

        public static void ShuffleIDs()
        {
            for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
                ShuffleID(GameData.Current.beatmapObjects[i], false);
            for (int i = 0; i < GameData.Current.backgroundObjects.Count; i++)
                GameData.Current.backgroundObjects[i].id = PAObjectBase.GetStringID();
            for (int i = 0; i < GameData.Current.prefabObjects.Count; i++)
                ShuffleID(GameData.Current.prefabObjects[i]);
            RTLevel.Reinit();
        }

        public static void ShuffleSelectedIDs()
        {
            foreach (var timelineObject in EditorTimeline.inst.SelectedObjects)
            {
                switch (timelineObject.TimelineReference)
                {
                    case TimelineObject.TimelineReferenceType.BeatmapObject: {
                            ShuffleID(timelineObject.GetData<BeatmapObject>(), false);
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.BackgroundObject: {
                            timelineObject.GetData<BackgroundObject>().id = PAObjectBase.GetStringID();
                            break;
                        }
                    case TimelineObject.TimelineReferenceType.PrefabObject: {
                            ShuffleID(timelineObject.GetData<PrefabObject>());
                            break;
                        }
                }
            }

            RTLevel.Reinit();
        }

        /// <summary>
        /// Shuffles a Beatmap Objects' ID and updates all references to the old ID.
        /// </summary>
        /// <param name="beatmapObject">Beatmap Object to shuffle the ID of.</param>
        public static void ShuffleID(BeatmapObject beatmapObject, bool update = true)
        {
            var newID = PAObjectBase.GetStringID();
            var oldID = beatmapObject.id;
            beatmapObject.id = newID;

            ShuffleSetID(oldID, newID, GameData.Current.beatmapObjects, GameData.Current.prefabObjects);

            for (int i = 0; i < GameData.Current.prefabs.Count; i++)
            {
                var p = GameData.Current.prefabs[i];
                ShuffleSetID(oldID, newID, p.beatmapObjects, p.prefabObjects);
            }

            if (update)
                RTLevel.Reinit();
        }

        static void ShuffleSetID(string oldID, string newID, List<BeatmapObject> beatmapObjects, List<PrefabObject> prefabObjects)
        {
            for (int i = 0; i < beatmapObjects.Count; i++)
            {
                var b = beatmapObjects[i];
                if (b.parent == oldID)
                    b.Parent = newID;
            }

            for (int i = 0; i < prefabObjects.Count; i++)
            {
                var po = prefabObjects[i];
                if (po.parent == oldID)
                    po.Parent = newID;
            }
        }

        /// <summary>
        /// Shuffles a Prefab Objects' ID and updates all references to the old ID.
        /// </summary>
        /// <param name="prefabObject">Prefab Object to shuffle the ID of.</param>
        public static void ShuffleID(PrefabObject prefabObject)
        {
            var newID = PAObjectBase.GetStringID();
            var oldID = prefabObject.id;
            prefabObject.id = newID;

            foreach (var prefabable in GameData.Current.GetPrefabables())
            {
                if (prefabable.PrefabInstanceID == oldID)
                    prefabable.PrefabInstanceID = newID;
            }
        }

        /// <summary>
        /// Calculates the total progress of the loaded levels.
        /// </summary>
        /// <returns>Returns the total progress of all loaded levels.</returns>
        public static float TotalLevelProgress()
        {
            var p = 0f;
            int count = 0;
            foreach (var levelPanel in EditorLevelManager.inst.LevelPanels)
            {
                if (!levelPanel.EditorInfo)
                    continue;
                p += levelPanel.EditorInfo.Progress;
                count++;
            }
            return p / count;
        }

        public static void CreateMarkers(int count, float distance)
        {
            if (count <= 0 || distance <= 0f)
                return;

            var time = AudioManager.inst.CurrentAudioSource.time;
            for (int i = 0; i < count; i++)
            {
                var marker = new Marker();
                marker.time = time;
                GameData.Current.data.markers.Add(marker);
                time += distance;
            }
            RTMarkerEditor.inst?.CreateMarkers();
        }
    }
}

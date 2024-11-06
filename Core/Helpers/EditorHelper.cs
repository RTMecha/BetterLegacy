using BetterLegacy.Components;
using BetterLegacy.Configs;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    public static class EditorHelper
    {
        public static void SetComplexity(GameObject gameObject, Complexity complexity, bool onlySpecificComplexity = false)
        {
            if (complexity == Complexity.Simple || onlySpecificComplexity)
            {
                gameObject?.SetActive(complexity == EditorConfig.Instance.EditorComplexity.Value);
                EditorConfig.UpdateEditorComplexity += () => { gameObject?.SetActive(complexity == EditorConfig.Instance.EditorComplexity.Value); };
                return;
            }

            gameObject?.SetActive(complexity <= EditorConfig.Instance.EditorComplexity.Value);
            EditorConfig.UpdateEditorComplexity += () => { gameObject?.SetActive(complexity <= EditorConfig.Instance.EditorComplexity.Value); };
        }

        public static void AddEditorPopup(string _name, GameObject _go)
        {
            var editorPropertiesDialog = new EditorManager.EditorDialog
            {
                Dialog = _go.transform,
                Name = _name,
                Type = EditorManager.EditorDialog.DialogType.Popup
            };

            EditorManager.inst.EditorDialogs.Add(editorPropertiesDialog);
            EditorManager.inst.EditorDialogsDictionary.Add(_name, editorPropertiesDialog);
        }

        public static void AddEditorDialog(string _name, GameObject _go)
        {
            var editorPropertiesDialog = new EditorManager.EditorDialog
            {
                Dialog = _go.transform,
                Name = _name,
                Type = EditorManager.EditorDialog.DialogType.Object
            };

            EditorManager.inst.EditorDialogs.Add(editorPropertiesDialog);
            EditorManager.inst.EditorDialogsDictionary.Add(_name, editorPropertiesDialog);
        }

        public static GameObject AddEditorDropdown(string name, string key, string dropdown, Sprite sprite, UnityEngine.Events.UnityAction unityAction, int siblingIndex = -1)
        {
            if (!RTEditor.inst.titleBar.Find($"{dropdown}"))
                return null;

            var parent = RTEditor.inst.titleBar.Find($"{dropdown}/{dropdown} Dropdown");

            var gameObject = RTEditor.inst.titleBar.Find("Edit/Edit Dropdown/Cut").gameObject.Duplicate(parent, name, siblingIndex < 0 ? parent.childCount : siblingIndex);
            gameObject.transform.Find("Text").GetComponent<Text>().text = name;
            gameObject.transform.Find("Text").AsRT().sizeDelta = new Vector2(224f, 0f);
            gameObject.transform.Find("Text 1").GetComponent<Text>().text = key;

            var propWinButton = gameObject.GetComponent<Button>();
            propWinButton.onClick.ClearAll();
            propWinButton.onClick.AddListener(unityAction);

            gameObject.SetActive(true);

            var image = gameObject.transform.Find("Image").GetComponent<Image>();
            gameObject.transform.Find("Image").GetComponent<Image>().sprite = sprite;

            EditorThemeManager.AddSelectable(propWinButton, ThemeGroup.Title_Bar_Dropdown, false);
            EditorThemeManager.AddGraphic(gameObject.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Title_Bar_Text);
            EditorThemeManager.AddGraphic(image, ThemeGroup.Title_Bar_Text);

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

                    RTEditor.inst.ShowContextMenu(300f,
                        new RTEditor.ButtonFunction("Flip Left/Right", () => { name.text = CoreHelper.FlipLeftRight(name.text); }),
                        new RTEditor.ButtonFunction("Flip Up/Down", () => { name.text = CoreHelper.FlipUpDown(name.text); }),
                        new RTEditor.ButtonFunction("Flip Upper/Lower", () => { name.text = CoreHelper.FlipUpperLower(name.text); }),
                        new RTEditor.ButtonFunction(true),
                        new RTEditor.ButtonFunction("Flip Number", () =>
                        {
                            CoreHelper.RegexMatches(name.text, new Regex(@"([-0-9]+)"), match =>
                            {
                                int num = Parser.TryParse(match.Groups[1].ToString(), 0);
                                num = -num;
                                name.text = name.text.Replace(match.Groups[1].ToString(), num.ToString());
                            });
                        }),
                        new RTEditor.ButtonFunction("Increase Number", () =>
                        {
                            CoreHelper.RegexMatches(name.text, new Regex(@"([-0-9]+)"), match =>
                            {
                                int num = Parser.TryParse(match.Groups[1].ToString(), 0);
                                num++;
                                name.text = name.text.Replace(match.Groups[1].ToString(), num.ToString());
                            });
                        }),
                        new RTEditor.ButtonFunction("Decrease Number", () =>
                        {
                            CoreHelper.RegexMatches(name.text, new Regex(@"([-0-9]+)"), match =>
                            {
                                int num = Parser.TryParse(match.Groups[1].ToString(), 0);
                                num--;
                                name.text = name.text.Replace(match.Groups[1].ToString(), num.ToString());
                            });
                        })
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
        public static void LoadLevel(string fullPath, float delay = 2f) => CoreHelper.StartCoroutine(ILoadLevel(fullPath, delay));

        static IEnumerator ILoadLevel(string fullPath, float delay = 2f)
        {
            SceneManager.inst.LoadScene("Editor");
            while (!CoreHelper.InEditor || !RTEditor.inst || EditorManager.inst.loading)
                yield return null;

            yield return new WaitForSeconds(delay);

            CoreHelper.StartCoroutine(RTEditor.inst.LoadLevel(fullPath));
        }

        public static bool SelectAllObjects()
        {
            if (!GameData.IsValid || !CoreHelper.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            if (RTEditor.inst.timelineObjects.Count == 1)
            {
                ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelineObjects[0]);
            }
            else if (RTEditor.inst.timelineObjects.Count > 1)
            {
                for (int i = 0; i < RTEditor.inst.timelineObjects.Count; i++)
                    RTEditor.inst.timelineObjects[i].Selected = true;

                ObjectEditor.inst.CurrentSelection = RTEditor.inst.timelineObjects.Last();

                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            }

            return true;
        }

        public static bool SelectAllObjectsOnCurrentLayer()
        {
            if (!GameData.IsValid || !CoreHelper.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            var layer = RTEditor.inst.Layer;

            ObjectEditor.inst.DeselectAllObjects();

            if (RTEditor.inst.timelineObjects.Count == 1)
            {
                if (RTEditor.inst.timelineObjects[0].Layer == layer)
                    ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelineObjects[0]);
            }
            else if (RTEditor.inst.timelineObjects.Count > 1)
            {
                for (int i = 0; i < RTEditor.inst.timelineObjects.Count; i++)
                    if (RTEditor.inst.timelineObjects[i].Layer == layer)
                        RTEditor.inst.timelineObjects[i].Selected = true;

                ObjectEditor.inst.CurrentSelection = RTEditor.inst.timelineObjects.Last();

                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            }

            return true;
        }

        public static bool MirrorSelectedObjects()
        {
            if (!GameData.IsValid || !CoreHelper.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
            {
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < beatmapObject.events[i].Count; j++)
                    {
                        beatmapObject.events[i][j].eventValues[0] = -beatmapObject.events[i][j].eventValues[0];
                        beatmapObject.events[i][j].eventRandomValues[0] = -beatmapObject.events[i][j].eventRandomValues[0];
                    }
                }

                Updater.UpdateObject(beatmapObject, "Keyframes");
            }

            return true;
        }

        public static bool FlipSelectedObjects()
        {
            if (!GameData.IsValid || !CoreHelper.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
            {
                for (int i = 0; i < 2; i++)
                {
                    for (int j = 0; j < beatmapObject.events[i].Count; j++)
                    {
                        beatmapObject.events[i][j].eventValues[1] = -beatmapObject.events[i][j].eventValues[1];
                        beatmapObject.events[i][j].eventRandomValues[1] = -beatmapObject.events[i][j].eventRandomValues[1];
                    }
                }

                Updater.UpdateObject(beatmapObject, "Keyframes");
            }

            return true;
        }

        public static bool RefreshKeyframesFromSelection()
        {
            if (!GameData.IsValid || !CoreHelper.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                Updater.UpdateObject(beatmapObject, "Keyframes");

            return true;
        }

        public static bool FixSmoothShakeKeyframes()
        {
            if (!GameData.IsValid || !CoreHelper.InEditor || !EditorManager.inst.hasLoadedLevel)
                return false;

            var gameData = GameData.Current;
            for (int i = 0; i < gameData.eventObjects.allEvents[3].Count; i++)
                gameData.eventObjects.allEvents[3][i].eventValues[3] = 0f;

            return true;
        }

        public static void PasteKeyframes()
        {
            var kfs = ObjectEditor.inst.copiedObjectKeyframes;

            if (kfs.Count <= 0)
            {
                EditorManager.inst.DisplayNotification("No copied keyframes yet!", 2f, EditorManager.NotificationType.Warning);
                return;
            }
            foreach (var beatmapObject in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
            {
                var ids = new List<string>();
                for (int i = 0; i < beatmapObject.events.Count; i++)
                    beatmapObject.events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                    {
                        var kf = ObjectEditor.inst.PasteKF(beatmapObject, x);
                        ids.Add(kf.id);
                        return kf;
                    }));

                for (int i = 0; i < beatmapObject.events.Count; i++)
                {
                    beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                               orderby x.eventTime
                                               select x).ToList();
                }

                var timelineObject = ObjectEditor.inst.GetTimelineObject(beatmapObject);
                if (EditorConfig.Instance.SelectPasted.Value)
                {
                    foreach (var kf in timelineObject.InternalSelections)
                        kf.Selected = ids.Contains(kf.ID);
                }

                ObjectEditor.inst.RenderTimelineObject(timelineObject);

                if (ObjectEditor.UpdateObjects)
                {
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                    Updater.UpdateObject(beatmapObject, "Autokill");
                }
            }
        }

        public static void RepeatPasteKeyframes(int repeatCount, float repeatOffsetTime)
        {
            var kfs = ObjectEditor.inst.copiedObjectKeyframes;

            if (kfs.Count <= 0)
            {
                EditorManager.inst.DisplayNotification("No copied keyframes yet!", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            foreach (var beatmapObject in ObjectEditor.inst.SelectedBeatmapObjects.Select(x => x.GetData<BeatmapObject>()))
            {
                float t = 0f;
                var ids = new List<string>();
                for (int repeat = 0; repeat < Mathf.Clamp(repeatCount + 1, 0, int.MaxValue); repeat++)
                {
                    for (int i = 0; i < beatmapObject.events.Count; i++)
                        beatmapObject.events[i].AddRange(kfs.Where(x => x.Type == i).Select(x =>
                        {
                            var kf = ObjectEditor.inst.PasteKF(beatmapObject, x);
                            kf.eventTime += t;
                            ids.Add(kf.id);
                            return kf;
                        }));

                    t += Mathf.Clamp(repeatOffsetTime, 0f, float.PositiveInfinity);
                }

                for (int i = 0; i < beatmapObject.events.Count; i++)
                {
                    beatmapObject.events[i] = (from x in beatmapObject.events[i]
                                               orderby x.eventTime
                                               select x).ToList();
                }

                var timelineObject = ObjectEditor.inst.GetTimelineObject(beatmapObject);
                if (EditorConfig.Instance.SelectPasted.Value)
                {
                    foreach (var kf in timelineObject.InternalSelections)
                        kf.Selected = ids.Contains(kf.ID);
                }

                ObjectEditor.inst.RenderTimelineObject(timelineObject);

                if (ObjectEditor.UpdateObjects)
                {
                    Updater.UpdateObject(beatmapObject, "Keyframes");
                    Updater.UpdateObject(beatmapObject, "Autokill");
                }
            }
        }

        public static void AddSelectedObjectIndexes(int amount)
        {
            var selected = ObjectEditor.inst.SelectedBeatmapObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var index = GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.id);
                if (index < 0)
                    continue;

                GameData.Current.beatmapObjects.Move(index, Mathf.Clamp(index + amount, 0, GameData.Current.beatmapObjects.Count - 1));
            }

            selected = ObjectEditor.inst.SelectedPrefabObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var index = GameData.Current.prefabObjects.FindIndex(x => x.ID == prefabObject.ID);
                if (index < 0)
                    continue;

                GameData.Current.prefabObjects.Move(index, Mathf.Clamp(index + amount, 0, GameData.Current.beatmapObjects.Count - 1));
            }

            ObjectEditor.inst.UpdateTransformIndex();
        }

        public static void SetSelectedObjectIndexes(int amount)
        {
            var selected = ObjectEditor.inst.SelectedBeatmapObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var beatmapObject = timelineObject.GetData<BeatmapObject>();
                var index = GameData.Current.beatmapObjects.FindIndex(x => x.id == beatmapObject.id);
                if (index < 0)
                    continue;

                GameData.Current.beatmapObjects.Move(index, Mathf.Clamp(amount, 0, GameData.Current.beatmapObjects.Count - 1));
            }

            selected = ObjectEditor.inst.SelectedPrefabObjects.Order(x => x.Index, amount > 0);

            for (int i = 0; i < selected.Count; i++)
            {
                var timelineObject = selected[i];
                var prefabObject = timelineObject.GetData<PrefabObject>();
                var index = GameData.Current.prefabObjects.FindIndex(x => x.ID == prefabObject.ID);
                if (index < 0)
                    continue;

                GameData.Current.prefabObjects.Move(index, Mathf.Clamp(amount, 0, GameData.Current.prefabObjects.Count - 1));
            }

            ObjectEditor.inst.UpdateTransformIndex();
        }
    }
}

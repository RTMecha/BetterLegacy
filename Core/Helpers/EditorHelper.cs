﻿using BetterLegacy.Core.Data;
using BetterLegacy.Core.Optimization;
using BetterLegacy.Editor.Managers;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BetterLegacy.Core.Helpers
{
    public static class EditorHelper
    {
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

        public static void AddEditorDropdown(string name, string key, string dropdown, Sprite sprite, UnityEngine.Events.UnityAction unityAction, int siblingIndex = -1)
        {
            if (!RTEditor.inst.titleBar.Find($"{dropdown}"))
                return;

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
            while (!EditorManager.inst || !RTEditor.inst || EditorManager.inst.loading)
                yield return null;

            yield return new WaitForSeconds(delay);

            CoreHelper.StartCoroutine(RTEditor.inst.LoadLevel(fullPath));
        }

        public static bool SelectAllObjects()
        {
            if (!GameData.IsValid || !EditorManager.inst || !EditorManager.inst.hasLoadedLevel)
                return false;

            if (RTEditor.inst.timelineObjects.Count == 1)
            {
                ObjectEditor.inst.SetCurrentObject(RTEditor.inst.timelineObjects[0]);
            }
            else if (RTEditor.inst.timelineObjects.Count > 1)
            {
                for (int i = 0; i < RTEditor.inst.timelineObjects.Count; i++)
                    RTEditor.inst.timelineObjects[i].selected = true;

                ObjectEditor.inst.CurrentSelection = RTEditor.inst.timelineObjects.Last();

                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            }

            return true;
        }

        public static bool SelectAllObjectsOnCurrentLayer()
        {
            if (!GameData.IsValid || !EditorManager.inst || !EditorManager.inst.hasLoadedLevel)
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
                        RTEditor.inst.timelineObjects[i].selected = true;

                ObjectEditor.inst.CurrentSelection = RTEditor.inst.timelineObjects.Last();

                EditorManager.inst.ClearDialogs();
                EditorManager.inst.ShowDialog("Multi Object Editor", false);
            }

            return true;
        }

        public static bool MirrorSelectedObjects()
        {
            if (!GameData.IsValid || !EditorManager.inst || !EditorManager.inst.hasLoadedLevel)
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

                Updater.UpdateProcessor(beatmapObject, "Keyframes");
            }

            return true;
        }

        public static bool FlipSelectedObjects()
        {
            if (!GameData.IsValid || !EditorManager.inst || !EditorManager.inst.hasLoadedLevel)
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

                Updater.UpdateProcessor(beatmapObject, "Keyframes");
            }

            return true;
        }

        public static bool RefreshKeyframesFromSelection()
        {
            if (!GameData.IsValid || !EditorManager.inst || !EditorManager.inst.hasLoadedLevel)
                return false;

            foreach (var beatmapObject in ObjectEditor.inst.SelectedObjects.Where(x => x.IsBeatmapObject).Select(x => x.GetData<BeatmapObject>()))
                Updater.UpdateProcessor(beatmapObject, "Keyframes");

            return true;
        }
    }
}

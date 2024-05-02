using BetterLegacy.Editor.Managers;
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
    }
}

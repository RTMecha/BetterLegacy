
using UnityEngine;
using UnityEngine.UI;

using LSFunctions;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Editor.Data.Dialogs
{
    public class KeybindEditorDialog : EditorDialog
    {
        public KeybindEditorDialog() : base() { }

        #region Values

        public FunctionButtonStorage SelectActionButton { get; set; }

        public RectTransform KeysContent { get; set; }

        public RectTransform SettingsContent { get; set; }

        #endregion

        #region Methods

        public override void Init()
        {
            if (init)
                return;

            base.Init();

            var editorDialog = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "KeybindEditor");
            editorDialog.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialog.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialog.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor("D89356");
            dialogStorage.title.text = "- Keybind Editor -";

            EditorHelper.AddEditorDialog(KEYBIND_EDITOR, editorDialog);

            InitDialog(KEYBIND_EDITOR);

            #region Setup

            if (editorDialog.transform.TryFind("spacer", out Transform spacer))
                spacer.AsRT().sizeDelta = new Vector2(0f, 64f);
            CoreHelper.Delete(editorDialog.transform.Find("Text"));

            var data = Creator.NewUIObject("data", editorDialog.transform);
            data.transform.AsRT().sizeDelta = new Vector2(765f, 300f);
            var dataVLG = data.AddComponent<VerticalLayoutGroup>();
            dataVLG.childControlHeight = false;
            dataVLG.childForceExpandHeight = false;
            dataVLG.spacing = 4f;

            var action = Creator.NewUIObject("action", data.transform);
            action.transform.AsRT().sizeDelta = new Vector2(765f, 64f);
            var actionHLG = action.AddComponent<VerticalLayoutGroup>();
            actionHLG.childControlHeight = false;
            actionHLG.childForceExpandHeight = false;

            new Labels(Labels.InitSettings.Default.Parent(action.transform), "Action");

            SelectActionButton = EditorPrefabHolder.Instance.Function2Button.Duplicate(action.transform, "select").GetComponent<FunctionButtonStorage>();

            // Keys list
            var keysScrollRect = Creator.NewUIObject("ScrollRect", data.transform);
            keysScrollRect.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            keysScrollRect.transform.AsRT().sizeDelta = new Vector2(400f, 250f);
            var keysScrollRectSR = keysScrollRect.AddComponent<ScrollRect>();
            keysScrollRectSR.horizontal = false;

            var keysMaskGO = Creator.NewUIObject("Mask", keysScrollRect.transform);
            RectValues.FullAnchored.AssignToRectTransform(keysMaskGO.transform.AsRT());
            var keysMaskImage = keysMaskGO.AddComponent<Image>();
            keysMaskImage.color = new Color(1f, 1f, 1f, 0.04f);
            keysMaskGO.AddComponent<Mask>();

            var keysContentGO = Creator.NewUIObject("Content", keysMaskGO.transform);
            KeysContent = keysContentGO.transform.AsRT();
            new RectValues(new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(400f, 250f)).AssignToRectTransform(KeysContent);

            var keysContentCSF = keysContentGO.AddComponent<ContentSizeFitter>();
            keysContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            keysContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var keysContentVLG = keysContentGO.AddComponent<VerticalLayoutGroup>();
            keysContentVLG.childControlHeight = false;
            keysContentVLG.childForceExpandHeight = false;
            keysContentVLG.spacing = 4f;

            var keysContentLE = keysContentGO.AddComponent<LayoutElement>();
            keysContentLE.layoutPriority = 10000;
            keysContentLE.minWidth = 760;

            keysScrollRectSR.content = KeysContent;

            // Settings list
            var settingsScrollRect = Creator.NewUIObject("ScrollRect Settings", data.transform);
            settingsScrollRect.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            settingsScrollRect.transform.AsRT().sizeDelta = new Vector2(400f, 250f);
            var settingsScrollRectSR = settingsScrollRect.AddComponent<ScrollRect>();
            settingsScrollRectSR.horizontal = false;

            var settingsMaskGO = Creator.NewUIObject("Mask", settingsScrollRect.transform);
            RectValues.FullAnchored.AssignToRectTransform(settingsMaskGO.transform.AsRT());
            var settingsMaskImage = settingsMaskGO.AddComponent<Image>();
            settingsMaskImage.color = new Color(1f, 1f, 1f, 0.04f);
            settingsMaskGO.AddComponent<Mask>();

            var settingsContentGO = Creator.NewUIObject("Content", settingsMaskGO.transform);
            SettingsContent = settingsContentGO.transform.AsRT();
            new RectValues(new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(400f, 250f)).AssignToRectTransform(SettingsContent);

            var settingsContentCSF = settingsContentGO.AddComponent<ContentSizeFitter>();
            settingsContentCSF.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            settingsContentCSF.verticalFit = ContentSizeFitter.FitMode.MinSize;

            var settingsContentVLG = settingsContentGO.AddComponent<VerticalLayoutGroup>();
            settingsContentVLG.childControlHeight = false;
            settingsContentVLG.childForceExpandHeight = false;
            settingsContentVLG.spacing = 4f;

            var settingsContentLE = settingsContentGO.AddComponent<LayoutElement>();
            settingsContentLE.layoutPriority = 10000;
            settingsContentLE.minWidth = 760;

            settingsScrollRectSR.content = SettingsContent;

            EditorThemeManager.ApplyGraphic(editorDialog.GetComponent<Image>(), ThemeGroup.Background_1);
            EditorThemeManager.ApplySelectable(SelectActionButton.button, ThemeGroup.Function_2);
            EditorThemeManager.ApplyGraphic(SelectActionButton.label, ThemeGroup.Function_2_Text);

            #endregion
        }

        public void ClearKeys() => LSHelpers.DeleteChildren(KeysContent);

        public void ClearSettings() => LSHelpers.DeleteChildren(SettingsContent);

        #endregion
    }
}

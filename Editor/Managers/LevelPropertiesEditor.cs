using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Settings;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages level properties.
    /// </summary>
    public class LevelPropertiesEditor : BaseManager<LevelPropertiesEditor, EditorManagerSettings>
    {
        /* TODO:
        - Global modifier variables?
        - Global math variables & functions
         */

        #region Values

        /// <summary>
        /// Dialog of the editor.
        /// </summary>
        public LevelPropertiesEditorDialog Dialog { get; set; }

        /// <summary>
        /// List of copied modifier blocks.
        /// </summary>
        public List<ModifierBlock> copiedModifierBlocks = new List<ModifierBlock>();

        #endregion

        #region Functions

        public override void OnInit()
        {
            try
            {
                Dialog = new LevelPropertiesEditorDialog();
                Dialog.Init();

                EditorHelper.AddEditorDropdown("Edit Level Properties", string.Empty, EditorHelper.EDIT_DROPDOWN, EditorSprites.EditSprite, OpenDialog);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        /// <summary>
        /// Copies a modifier block.
        /// </summary>
        /// <param name="modifierBlock">Modifier block to copy.</param>
        public void CopyModifierBlock(ModifierBlock modifierBlock)
        {
            copiedModifierBlocks.Clear();
            copiedModifierBlocks.Add(modifierBlock.Copy());
            EditorManager.inst.DisplayNotification($"Copied modifier block!", 2f, EditorManager.NotificationType.Success);
            RenderDialog();
        }

        /// <summary>
        /// Copies a list of modifier blocks.
        /// </summary>
        /// <param name="modifierBlocks">List of modifier blocks to copy.</param>
        public void CopyModifierBlocks(List<ModifierBlock> modifierBlocks)
        {
            copiedModifierBlocks = new List<ModifierBlock>(modifierBlocks.Select(x => x.Copy()));
            EditorManager.inst.DisplayNotification($"Copied modifier blocks!", 2f, EditorManager.NotificationType.Success);
            RenderDialog();
        }

        /// <summary>
        /// Pastes all copied modifiers into a list of modifier blocks.
        /// </summary>
        public void PasteModifierBlocks() => PasteModifierBlocks(copiedModifierBlocks);

        /// <summary>
        /// Pastes all copied modifiers into a list of modifier blocks.
        /// </summary>
        /// <param name="copied">List of copied modifier blocks.</param>
        public void PasteModifierBlocks(List<ModifierBlock> copied) => PasteModifierBlocks(GameData.Current.modifierBlocks, copied);

        /// <summary>
        /// Pastes all copied modifiers into a list of modifier blocks.
        /// </summary>
        /// <param name="modifierBlocks">Modifier blocks to copy onto.</param>
        /// <param name="copied">List of copied modifier blocks.</param>
        public void PasteModifierBlocks(List<ModifierBlock> modifierBlocks, List<ModifierBlock> copied)
        {
            if (copied.IsEmpty())
            {
                EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            modifierBlocks.AddRange(copied.Select(x => x.Copy()));
            EditorManager.inst.DisplayNotification($"Pasted modifier blocks!", 2f, EditorManager.NotificationType.Success);
            RenderDialog();
        }

        /// <summary>
        /// Creates a new modifier block.
        /// </summary>
        public void CreateNewModifierBlock()
        {
            var modifierBlock = new ModifierBlock("newModifierBlock", ModifierReferenceType.ModifierBlock);
            GameData.Current.modifierBlocks.Add(modifierBlock);
            RenderDialog();
        }

        /// <summary>
        /// Opens the dialog.
        /// </summary>
        public void OpenDialog()
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification($"Open a level first before trying to edit level properties.", 2f, EditorManager.NotificationType.Warning);
                return;
            }

            Dialog.Open();
            RenderDialog();
        }

        /// <summary>
        /// Renders the dialog.
        /// </summary>
        public void RenderDialog()
        {
            Dialog.LevelStartOffsetField.SetTextWithoutNotify(GameData.Current.data.level.LevelStartOffset.ToString());
            Dialog.LevelStartOffsetField.OnValueChanged.NewListener(_val =>
            {
                if (!float.TryParse(_val, out float num))
                    return;

                GameData.Current.data.level.LevelStartOffset = num;
                EditorTimeline.inst.UpdateTimelineSizes();
                GameManager.inst.UpdateTimeline();
            });

            Dialog.LevelStartOffsetField.middleButton.onClick.NewListener(() => Dialog.LevelStartOffsetField.Text = AudioManager.inst.CurrentAudioSource.time.ToString());

            TriggerHelper.IncreaseDecreaseButtons(Dialog.LevelStartOffsetField, max: AudioManager.inst.CurrentAudioSource.clip.length);
            TriggerHelper.AddEventTriggers(Dialog.LevelStartOffsetField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.LevelStartOffsetField.inputField, max: AudioManager.inst.CurrentAudioSource.clip.length));

            Dialog.ReverseToggle.SetIsOnWithoutNotify(GameData.Current.data.level.reverse);
            Dialog.ReverseToggle.onValueChanged.NewListener(_val => GameData.Current.data.level.reverse = _val);

            Dialog.LevelEndOffsetField.SetTextWithoutNotify(GameData.Current.data.level.LevelEndOffset.ToString());
            Dialog.LevelEndOffsetField.OnValueChanged.NewListener(_val =>
            {
                if (!float.TryParse(_val, out float num))
                    return;

                GameData.Current.data.level.LevelEndOffset = num;
                EditorTimeline.inst.UpdateTimelineSizes();
                GameManager.inst.UpdateTimeline();
            });

            Dialog.LevelEndOffsetField.middleButton.onClick.NewListener(() => Dialog.LevelEndOffsetField.Text = (AudioManager.inst.CurrentAudioSource.clip.length - AudioManager.inst.CurrentAudioSource.time).ToString());

            TriggerHelper.IncreaseDecreaseButtons(Dialog.LevelEndOffsetField, min: 0.1f, max: AudioManager.inst.CurrentAudioSource.clip.length);
            TriggerHelper.AddEventTriggers(Dialog.LevelEndOffsetField.gameObject,
                TriggerHelper.ScrollDelta(Dialog.LevelEndOffsetField.inputField, min: 0.1f, max: AudioManager.inst.CurrentAudioSource.clip.length));

            Dialog.AutoEndLevelToggle.SetIsOnWithoutNotify(GameData.Current.data.level.autoEndLevel);
            Dialog.AutoEndLevelToggle.onValueChanged.NewListener(_val => GameData.Current.data.level.autoEndLevel = _val);

            Dialog.LevelEndFunctionDropdown.SetValueWithoutNotify((int)GameData.Current.data.level.endLevelFunc);
            Dialog.LevelEndFunctionDropdown.onValueChanged.NewListener(_val => GameData.Current.data.level.endLevelFunc = (EndLevelFunction)_val);

            Dialog.LevelEndDataField.SetTextWithoutNotify(GameData.Current.data.level.endLevelData);
            Dialog.LevelEndDataField.onValueChanged.NewListener(_val => GameData.Current.data.level.endLevelData = _val);

            CoroutineHelper.StartCoroutine(Dialog.LevelModifiers.RenderModifiers(GameData.Current));

            Dialog.ModifierBlocks.ForLoopReverse((modifierBlockDialog, index) =>
            {
                modifierBlockDialog.Clear();
                Dialog.ModifierBlocks.RemoveAt(index);
            });

            CoreHelper.DestroyChildren(Dialog.ModifierBlocksContent);

            for (int i = 0; i < GameData.Current.modifierBlocks.Count; i++)
            {
                var index = i;
                var modifierBlock = GameData.Current.modifierBlocks[i];

                var modifierBlockDialog = new ModifiersEditorDialog();
                modifierBlockDialog.Init(Dialog.ModifierBlocksContent, true, false, false, true);
                modifierBlockDialog.InitCopy(modifierBlockDialog.Label.transform.parent);
                new RectValues(new Vector2(700f, -14f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(26f, 26f)).AssignToRectTransform(modifierBlockDialog.CopyButton.transform.AsRT());
                modifierBlockDialog.CopyButton.onClick.ClearAll();
                EditorContextMenu.AddContextMenu(modifierBlockDialog.CopyButton.gameObject, leftClick: () => CopyModifierBlock(modifierBlock),
                    new ButtonElement("Copy", () => CopyModifierBlock(modifierBlock)),
                    new ButtonElement("Copy All", () => CopyModifierBlocks(GameData.Current.modifierBlocks)),
                    new SpacerElement(),
                    new ButtonElement("Copy to JSON", () =>
                    {
                        var jn = Parser.ModifierBlocksToJSON(new List<ModifierBlock> { modifierBlock });
                        LSText.CopyToClipboard(jn.ToString(3));
                    }),
                    new ButtonElement("Copy All to JSON", () =>
                    {
                        var jn = Parser.ModifierBlocksToJSON(GameData.Current.modifierBlocks);
                        LSText.CopyToClipboard(jn.ToString(3));
                    }),
                    new SpacerElement(),
                    new ButtonElement("Add to Prefab", () =>
                    {
                        RTPrefabEditor.inst.OpenPopup();
                        RTPrefabEditor.inst.onSelectPrefab = prefabPanel =>
                        {
                            if (!prefabPanel.Item.modifierBlocks.Has(x => x.Name == modifierBlock.Name))
                            {
                                prefabPanel.Item.modifierBlocks.Add(modifierBlock.Copy(false));
                                if (prefabPanel.IsExternal)
                                    RTPrefabEditor.inst.UpdatePrefabFile(prefabPanel);
                                EditorManager.inst.DisplayNotification($"Added modifier block {modifierBlock.Name} to the prefab.", 2f, EditorManager.NotificationType.Success);
                            }
                            else
                                EditorManager.inst.DisplayNotification($"Prefab already has a theme with the same ID!", 2f, EditorManager.NotificationType.Warning);
                        };
                    }));
                modifierBlockDialog.InitDelete(modifierBlockDialog.Label.transform.parent);
                new RectValues(new Vector2(730f, -14f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(26f, 26f)).AssignToRectTransform(modifierBlockDialog.DeleteButton.transform.AsRT());
                modifierBlockDialog.DeleteButton.onClick.NewListener(() =>
                {
                    GameData.Current.modifierBlocks.RemoveAt(index);
                    RenderDialog();
                });

                Dialog.ModifierBlocks.Add(modifierBlockDialog);

                CoroutineHelper.StartCoroutine(modifierBlockDialog.RenderModifiers(modifierBlock));
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(Dialog.ModifierBlocksContent);
            add.Text = "Add Modifier Block";
            add.OnClick.NewListener(CreateNewModifierBlock);

            var paste = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.ModifierBlocksContent, "Paste");
            var pasteStorage = paste.GetComponent<FunctionButtonStorage>();
            pasteStorage.Text = "Paste Modifier Blocks";
            pasteStorage.OnClick.ClearAll();
            EditorContextMenu.AddContextMenu(paste, leftClick: () => PasteModifierBlocks(),
                new ButtonElement("Paste (Additive)", PasteModifierBlocks),
                new ButtonElement("Paste (Overwrite)", () =>
                {
                    if (copiedModifierBlocks.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    for (int i = 0; i < copiedModifierBlocks.Count; i++)
                    {
                        var copy = copiedModifierBlocks[i];
                        var original = GameData.Current.modifierBlocks.Find(x => x.Name == copy.Name);
                        if (original)
                            original.CopyData(copy);
                        else
                            GameData.Current.modifierBlocks.Add(copy);
                    }

                    EditorManager.inst.DisplayNotification($"Pasted modifier blocks!", 2f, EditorManager.NotificationType.Success);
                    RenderDialog();
                }),
                new ButtonElement("Paste (Clear)", () =>
                {
                    if (copiedModifierBlocks.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    GameData.Current.modifierBlocks.Clear();
                    PasteModifierBlocks();
                }),
                new SpacerElement(),
                new ButtonElement("Paste from JSON (Additive)", () =>
                {
                    var text = RTString.GetClipboardText();
                    if (string.IsNullOrEmpty(text))
                    {
                        EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var jn = JSON.Parse(text);
                    var modifierBlocks = Parser.ParseModifierBlocks(jn, ModifierReferenceType.ModifierBlock);
                    PasteModifierBlocks(modifierBlocks);
                }),
                new ButtonElement("Paste from JSON (Overwrite)", () =>
                {
                    var text = RTString.GetClipboardText();
                    if (string.IsNullOrEmpty(text))
                    {
                        EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var jn = JSON.Parse(text);
                    var modifierBlocks = Parser.ParseModifierBlocks(jn, ModifierReferenceType.ModifierBlock);
                    if (modifierBlocks.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    for (int i = 0; i < modifierBlocks.Count; i++)
                    {
                        var copy = modifierBlocks[i];
                        var original = GameData.Current.modifierBlocks.Find(x => x.Name == copy.Name);
                        if (original)
                            original.CopyData(copy);
                        else
                            GameData.Current.modifierBlocks.Add(copy);
                    }

                    EditorManager.inst.DisplayNotification($"Pasted modifier blocks!", 2f, EditorManager.NotificationType.Success);
                    RenderDialog();
                }),
                new ButtonElement("Paste from JSON (Clear)", () =>
                {
                    var text = RTString.GetClipboardText();
                    if (string.IsNullOrEmpty(text))
                    {
                        EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    var jn = JSON.Parse(text);
                    var modifierBlocks = Parser.ParseModifierBlocks(jn, ModifierReferenceType.ModifierBlock);
                    if (modifierBlocks.IsEmpty())
                    {
                        EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                        return;
                    }

                    GameData.Current.modifierBlocks.Clear();
                    PasteModifierBlocks(modifierBlocks);
                }));

            EditorThemeManager.ApplyGraphic(pasteStorage.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(pasteStorage.label, ThemeGroup.Paste_Text);
        }

        #endregion
    }
}

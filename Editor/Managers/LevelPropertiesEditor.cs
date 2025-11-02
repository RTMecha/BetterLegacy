using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
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

        public LevelPropertiesEditorDialog Dialog { get; set; }

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

        public void CopyModifierBlock(ModifierBlock modifierBlock)
        {
            copiedModifierBlocks.Clear();
            copiedModifierBlocks.Add(modifierBlock.Copy());
            EditorManager.inst.DisplayNotification($"Copied modifier block!", 2f, EditorManager.NotificationType.Success);
            RenderDialog();
        }

        public void CopyModifierBlocks(List<ModifierBlock> modifierBlocks)
        {
            copiedModifierBlocks = new List<ModifierBlock>(modifierBlocks.Select(x => x.Copy()));
            EditorManager.inst.DisplayNotification($"Copied modifier blocks!", 2f, EditorManager.NotificationType.Success);
            RenderDialog();
        }

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

        public void CreateNewModifierBlock()
        {
            var modifierBlock = new ModifierBlock("newModifierBlock", ModifierReferenceType.ModifierBlock);
            GameData.Current.modifierBlocks.Add(modifierBlock);
            RenderDialog();
        }

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

        public void RenderDialog()
        {
            Dialog.LevelStartOffsetField.inputField.SetTextWithoutNotify(GameData.Current.data.level.LevelStartOffset.ToString());
            Dialog.LevelStartOffsetField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    GameData.Current.data.level.LevelStartOffset = num;
            });

            Dialog.ReverseToggle.SetIsOnWithoutNotify(GameData.Current.data.level.reverse);
            Dialog.ReverseToggle.onValueChanged.NewListener(_val => GameData.Current.data.level.reverse = _val);

            Dialog.LevelEndOffsetField.inputField.SetTextWithoutNotify(GameData.Current.data.level.LevelEndOffset.ToString());
            Dialog.LevelEndOffsetField.inputField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    GameData.Current.data.level.LevelEndOffset = num;
            });

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
                var copyContextClickable = modifierBlockDialog.CopyButton.gameObject.GetOrAddComponent<ContextClickable>();
                modifierBlockDialog.CopyButton.onClick.ClearAll();
                copyContextClickable.onClick = pointerEventData =>
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Copy", () => CopyModifierBlock(modifierBlock)),
                            new ButtonFunction("Copy All", () => CopyModifierBlocks(GameData.Current.modifierBlocks)),
                            new ButtonFunction(true),
                            new ButtonFunction("Copy to JSON", () =>
                            {
                                var jn = Parser.ModifierBlocksToJSON(new List<ModifierBlock> { modifierBlock });
                                LSText.CopyToClipboard(jn.ToString(3));
                            }),
                            new ButtonFunction("Copy All to JSON", () =>
                            {
                                var jn = Parser.ModifierBlocksToJSON(GameData.Current.modifierBlocks);
                                LSText.CopyToClipboard(jn.ToString(3));
                            }),
                            new ButtonFunction(true),
                            new ButtonFunction("Add to Prefab", () =>
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
                        return;
                    }

                    CopyModifierBlock(modifierBlock);
                };
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
            var pasteContextClickable = paste.GetOrAddComponent<ContextClickable>();
            pasteContextClickable.onClick = pointerEventData =>
            {
                if (pointerEventData.button == PointerEventData.InputButton.Right)
                {
                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Paste (Additive)", () =>
                        {
                            PasteModifierBlocks(GameData.Current.modifierBlocks, copiedModifierBlocks);
                        }),
                        new ButtonFunction("Paste (Overwrite)", () =>
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
                        new ButtonFunction("Paste (Clear)", () =>
                        {
                            if (copiedModifierBlocks.IsEmpty())
                            {
                                EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                                return;
                            }

                            GameData.Current.modifierBlocks.Clear();
                            PasteModifierBlocks(GameData.Current.modifierBlocks, copiedModifierBlocks);
                        }),
                        new ButtonFunction(true),
                        new ButtonFunction("Paste from JSON (Additive)", () =>
                        {
                            var text = RTString.GetClipboardText();
                            if (string.IsNullOrEmpty(text))
                            {
                                EditorManager.inst.DisplayNotification($"Nothing to paste!", 2f, EditorManager.NotificationType.Error);
                                return;
                            }

                            var jn = JSON.Parse(text);
                            var modifierBlocks = Parser.ParseModifierBlocks(jn, ModifierReferenceType.ModifierBlock);
                            PasteModifierBlocks(GameData.Current.modifierBlocks, modifierBlocks);
                        }),
                        new ButtonFunction("Paste from JSON (Overwrite)", () =>
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
                        new ButtonFunction("Paste from JSON (Clear)", () =>
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
                            PasteModifierBlocks(GameData.Current.modifierBlocks, modifierBlocks);
                        }));
                    return;
                }

                PasteModifierBlocks(GameData.Current.modifierBlocks, copiedModifierBlocks);
            };

            EditorThemeManager.ApplyGraphic(pasteStorage.button.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(pasteStorage.label, ThemeGroup.Paste_Text);
        }

        #endregion
    }
}

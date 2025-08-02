using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using SimpleJSON;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Editor class that manages level properties.
    /// </summary>
    public class LevelPropertiesEditor : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="LevelPropertiesEditor"/> global instance reference.
        /// </summary>
        public static LevelPropertiesEditor inst;

        /// <summary>
        /// Initializes <see cref="LevelPropertiesEditor"/>.
        /// </summary>
        public static void Init() => Creator.NewGameObject(nameof(LevelPropertiesEditor), EditorManager.inst.transform.parent).AddComponent<LevelPropertiesEditor>();

        void Awake()
        {
            inst = this;

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

        #endregion

        #region Values

        public LevelPropertiesEditorDialog Dialog { get; set; }

        public List<ModifierBlock<IModifierReference>> copiedModifierBlocks = new List<ModifierBlock<IModifierReference>>();

        #endregion

        #region Methods

        public void CopyModifierBlock(ModifierBlock<IModifierReference> modifierBlock)
        {
            copiedModifierBlocks.Clear();
            copiedModifierBlocks.Add(modifierBlock.Copy());
            EditorManager.inst.DisplayNotification($"Copied modifier block!", 2f, EditorManager.NotificationType.Success);
            RenderDialog();
        }

        public void CopyModifierBlocks(List<ModifierBlock<IModifierReference>> modifierBlocks)
        {
            copiedModifierBlocks = new List<ModifierBlock<IModifierReference>>(modifierBlocks.Select(x => x.Copy()));
            EditorManager.inst.DisplayNotification($"Copied modifier blocks!", 2f, EditorManager.NotificationType.Success);
            RenderDialog();
        }

        public void PasteModifierBlocks(List<ModifierBlock<IModifierReference>> modifierBlocks, List<ModifierBlock<IModifierReference>> copied)
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
            var modifierBlock = new ModifierBlock<IModifierReference>("newModifierBlock", ModifierReferenceType.ModifierBlock);
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
                                var jn = Parser.ModifierBlocksToJSON(new List<ModifierBlock<IModifierReference>> { modifierBlock });
                                LSText.CopyToClipboard(jn.ToString(3));
                            }),
                            new ButtonFunction("Copy All to JSON", () =>
                            {
                                var jn = Parser.ModifierBlocksToJSON(GameData.Current.modifierBlocks);
                                LSText.CopyToClipboard(jn.ToString(3));
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

            var add = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.ModifierBlocksContent, "Add");
            add.transform.AsRT().sizeDelta = new Vector2(763f, 32f);
            var addText = add.transform.Find("Text").GetComponent<Text>();
            addText.text = "Add Modifier Block";
            var addButton = add.GetComponent<Button>();
            addButton.onClick.ClearAll();
            var contextClickable = add.GetOrAddComponent<ContextClickable>();
            contextClickable.onClick = pointerEventData =>
            {
                CreateNewModifierBlock();
            };

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);

            var paste = PrefabEditor.inst.CreatePrefab.Duplicate(Dialog.ModifierBlocksContent, "Paste");
            paste.transform.AsRT().sizeDelta = new Vector2(763f, 32f);
            var pasteText = paste.transform.Find("Text").GetComponent<Text>();
            pasteText.text = "Paste Modifier Blocks";
            var pasteButton = paste.GetComponent<Button>();
            pasteButton.onClick.ClearAll();
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
                            var modifierBlocks = Parser.ParseModifierBlocks<IModifierReference>(jn, ModifierReferenceType.ModifierBlock);
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
                            var modifierBlocks = Parser.ParseModifierBlocks<IModifierReference>(jn, ModifierReferenceType.ModifierBlock);
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
                            var modifierBlocks = Parser.ParseModifierBlocks<IModifierReference>(jn, ModifierReferenceType.ModifierBlock);
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

            EditorThemeManager.ApplyGraphic(pasteButton.image, ThemeGroup.Paste, true);
            EditorThemeManager.ApplyGraphic(pasteText, ThemeGroup.Paste_Text);
        }

        #endregion
    }
}

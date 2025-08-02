using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
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

        #endregion

        #region Methods

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
                var modifierBlock = new ModifierBlock<IModifierReference>("newModifierBlock", ModifierReferenceType.ModifierBlock);
                GameData.Current.modifierBlocks.Add(modifierBlock);
                RenderDialog();
            };

            EditorThemeManager.ApplyGraphic(addButton.image, ThemeGroup.Add, true);
            EditorThemeManager.ApplyGraphic(addText, ThemeGroup.Add_Text);
        }

        #endregion
    }
}

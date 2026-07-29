using UnityEngine;

using BetterLegacy.Editor;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Region : ModifierActionBase
    {
        #region Constructors

        public Region(bool end)
        {
            Name = end ? "endregion" : "region";
            SetupModifier();
            Modifier.collapse = true;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Editor;

        public override Sprite Icon => EditorSprites.EditSprite;

        public override bool IsEditorModifier => true;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) { }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}

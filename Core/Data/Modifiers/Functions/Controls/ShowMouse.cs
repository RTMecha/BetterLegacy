using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ShowMouse : ModifierActionBase
    {
        #region Constructors

        public ShowMouse() => SetupModifier(false, "True");

        #endregion

        #region Values

        public override string Name => "showMouse";

        public override ModifierCategoryType Category => ModifierCategoryType.Controls;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.GetBool(0, true, modifierLoop.variables))
                CursorManager.inst.ShowCursor();
            else if (ProjectArrhythmia.State.InEditorPreview)
                CursorManager.inst.HideCursor();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.BoolGenerator(modifier, reference, "Enabled", 0, true);
        }

        #endregion
    }
}

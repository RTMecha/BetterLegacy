using UnityEngine;

using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetMousePosition : ModifierActionBase
    {
        #region Constructors

        public SetMousePosition() => SetupModifier("0", "0", "0");

        #endregion

        #region Values

        public override string Name => "setMousePosition";

        public override CategoryType Category => CategoryType.Controls;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (ProjectArrhythmia.State.IsEditing || !Application.isFocused)
                return;

            var screenScale = Display.main.systemWidth / 1920f;
            float windowCenterX = (Display.main.systemWidth) / 2;
            float windowCenterY = (Display.main.systemHeight) / 2;

            var x = modifier.GetFloat(1, 0f, modifierLoop.variables);
            var y = modifier.GetFloat(2, 0f, modifierLoop.variables);

            CursorManager.inst.SetCursorPosition(new Vector2((x * screenScale) + windowCenterX, (y * screenScale) + windowCenterY));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Position X", 1, 0);
            modifierCard.IntegerGenerator(modifier, reference, "Position Y", 1, 0);
        }

        #endregion
    }
}

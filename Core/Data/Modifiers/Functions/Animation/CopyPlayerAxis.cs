using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CopyPlayerAxis : ModifierActionBase
    {
        #region Constructors

        public CopyPlayerAxis() => SetupModifier("0", "0", "0", "0", "0", "1", "1", "0", "-99999", "99999");

        #endregion

        #region Values

        public override string Name => "copyPlayerAxis";

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var transformable = modifierLoop.reference.AsTransformable();
            if (transformable == null)
                return;

            var fromType = modifier.GetInt(1, 0, modifierLoop.variables);
            var fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);

            var toType = modifier.GetInt(3, 0, modifierLoop.variables);
            var toAxis = modifier.GetInt(4, 0, modifierLoop.variables);

            var delay = modifier.GetFloat(5, 0f, modifierLoop.variables);
            var multiply = modifier.GetFloat(6, 0f, modifierLoop.variables);
            var offset = modifier.GetFloat(7, 0f, modifierLoop.variables);
            var min = modifier.GetFloat(8, -9999f, modifierLoop.variables);
            var max = modifier.GetFloat(9, 9999f, modifierLoop.variables);

            var players = PlayerManager.Players;

            if (players.TryFind(x => x.RuntimePlayer && x.RuntimePlayer.rb, out PAPlayer player))
                transformable.SetTransform(toType, toAxis, RTMath.Clamp((player.RuntimePlayer.rb.transform.GetLocalVector(fromType).At(fromAxis) - offset) * multiply, min, max));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
            modifierCard.DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

            modifierCard.DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation", "Color"));
            modifierCard.DropdownGenerator(modifier, reference, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

            modifierCard.SingleGenerator(modifier, reference, "Multiply", 6, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Offset", 7, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Min", 8, -99999f);
            modifierCard.SingleGenerator(modifier, reference, "Max", 9, 99999f);
        }

        #endregion
    }
}

using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LerpTheme : ModifierActionBase
    {
        #region Constructors

        public LerpTheme() => SetupModifier("0", "1", "0.5");

        #endregion

        #region Values

        public override string Name => "lerpTheme";

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var firstID = modifier.GetValue(0, modifierLoop.variables);
            var secondID = modifier.GetValue(1, modifierLoop.variables);
            if (string.IsNullOrEmpty(firstID) || string.IsNullOrEmpty(secondID))
            {
                if (RTLevel.Current && RTLevel.Current.eventEngine)
                    RTLevel.Current.eventEngine.CustomTheme = null;
                return;
            }

            if (!RTLevel.Current || !RTLevel.Current.eventEngine)
                return;

            if (!RTLevel.Current.eventEngine.CustomTheme)
                RTLevel.Current.eventEngine.CustomTheme = ThemeManager.inst.Current.Copy();

            var first = ThemeManager.inst.GetTheme(firstID);
            var second = ThemeManager.inst.GetTheme(secondID);
            RTLevel.Current.eventEngine.CustomTheme.Lerp(first, second, modifier.GetFloat(2, 0f, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Previous ID", 0);
            modifierCard.StringGenerator(modifier, reference, "Next ID", 1);
            modifierCard.SingleGenerator(modifier, reference, "Interpolate", 2);
        }

        #endregion
    }
}

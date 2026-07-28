using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetTheme : ModifierActionBase
    {
        #region Constructors

        public SetTheme() => SetupModifier(string.Empty);

        #endregion

        #region Values

        public override string Name => "setTheme";

        public override CategoryType Category => CategoryType.Color;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (RTLevel.Current && RTLevel.Current.eventEngine)
                RTLevel.Current.eventEngine.CustomTheme = ThemeManager.inst.GetTheme(modifier.GetValue(0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "ID", 0);
        }

        #endregion
    }
}

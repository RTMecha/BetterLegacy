using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetColorSlotHexCode : ModifierVariableBase
    {
        #region Constructors

        public GetColorSlotHexCode() => SetupModifier("COLORHEX_VAR", "0", "1", "0", "0", "0", "4");

        #endregion

        #region Values

        public override string Name => "getColorSlotHexCode";

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var colorSource = (ThemeSource)modifier.GetInt(6, 4, modifierLoop.variables);
            var color = colorSource switch
            {
                ThemeSource.Background => ThemeManager.inst.bgColorToLerp,
                ThemeSource.GUI => ThemeManager.inst.timelineColorToLerp,
                ThemeSource.PlayerTail => ThemeManager.inst.tailColorToLerp,
                _ => ThemeManager.inst.Current.GetColor(colorSource, modifier.GetInt(1, 0, modifierLoop.variables)),
            };
            color = RTColors.FadeColor(color, modifier.GetFloat(2, 1f, modifierLoop.variables));
            color = RTColors.ChangeColorHSV(color, modifier.GetFloat(3, 0f, modifierLoop.variables), modifier.GetFloat(4, 0f, modifierLoop.variables), modifier.GetFloat(5, 0f, modifierLoop.variables));
            return RTColors.ColorToHexOptional(color);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.DropdownGenerator(modifier, reference, "Color Source", 6, CoreHelper.ToOptionData<ThemeSource>(), _val =>
            {
                modifier.SetValue(6, _val.ToString());
                modifierCard.RenderModifier(reference, modifyable);
            });
            modifierCard.ColorGenerator(modifier, reference, "Color", 1, (ThemeSource)modifier.GetInt(6, 4));
            modifierCard.SingleGenerator(modifier, reference, "Opacity", 2, 1f, max: 1f);
            modifierCard.SingleGenerator(modifier, reference, "Hue", 3);
            modifierCard.SingleGenerator(modifier, reference, "Saturation", 4);
            modifierCard.SingleGenerator(modifier, reference, "Value", 5);
        }

        #endregion
    }
}

using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetSample : ModifierActionBase
    {
        #region Constructors

        public GetSample() => SetupModifier("SAMPLE_VAR", "0", "0");

        #endregion

        #region Values

        public override string Name => "getSample";

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = RTLevel.Current.GetSample(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetFloat(2, 1f, modifierLoop.variables)).ToString();

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);
            modifierCard.SingleGenerator(modifier, reference, "Intensity", 2, 0f);
        }

        #endregion
    }
}

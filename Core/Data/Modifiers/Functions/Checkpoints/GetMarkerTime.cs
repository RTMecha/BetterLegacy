using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetMarkerTime : ModifierVariableBase
    {
        #region Constructors

        public GetMarkerTime() => SetupModifier("MARKER_TIME", "0");

        #endregion

        #region Values

        public override string Name => "getMarkerTime";

        public override ModifierCategoryType Category => ModifierCategoryType.Checkpoints;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (GameData.Current.data.markers.TryGetAt(modifier.GetInt(1, 0, modifierLoop.variables), out Marker checkpoint))
                return checkpoint.time.ToString();
            return null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.IntegerGenerator(modifier, reference, "Marker Index", 1);
        }

        #endregion
    }
}

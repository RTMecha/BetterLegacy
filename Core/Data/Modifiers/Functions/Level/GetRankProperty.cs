using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetRankProperty : ModifierVariableBase
    {
        #region Constructors

        public GetRankProperty(Property property)
        {
            this.property = property;
            Name = "get" + property.ToString() + "Count";
            SetupModifier($"{property.ToString().ToUpper()}_COUNT_VAR");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Level;

        readonly Property property;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => property switch
        {
            Property.Hit => RTBeatmap.Current.hits.Count.ToString(),
            Property.Death => RTBeatmap.Current.deaths.Count.ToString(),
            _ => null,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0);
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            Hit,
            Death,
        }

        #endregion
    }
}

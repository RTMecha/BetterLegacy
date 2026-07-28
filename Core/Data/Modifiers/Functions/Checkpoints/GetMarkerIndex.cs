using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetMarkerIndex : ModifierVariableBase
    {
        #region Constructors

        public GetMarkerIndex(Type type)
        {
            this.type = type;
            Name = "get" + type.ToString() + "MarkerIndex";
            SetupModifier($"{type.ToString().ToUpper()}_MARKER");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Checkpoints;

        readonly Type type;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => type switch
        {
            Type.Last => GameData.Current.data.GetLastMarkerIndex().ToString(),
            Type.Next => GameData.Current.data.GetNextMarkerIndex().ToString(),
            _ => null,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Last,
            Next,
        }

        #endregion
    }
}

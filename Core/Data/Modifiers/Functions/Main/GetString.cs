using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetString : ModifierVariableBase
    {
        #region Constructors

        public GetString(Type type)
        {
            this.type = type;
            Name = "getString";
            if (type != Type.Normal)
                Name += type.ToString();
            SetupModifier(type == Type.Length ? "STRINGLENGTH_VAR" : "STRING_VAR", "Text");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        readonly Type type;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var value = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);
            value = type switch
            {
                Type.Lower => value.ToLower(),
                Type.Upper => value.ToUpper(),
                Type.Length => value.Length.ToString(),
                _ => value,
            };
            return value;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Value", 1);
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Normal,
            Lower,
            Upper,
            Length,
        }

        #endregion
    }
}

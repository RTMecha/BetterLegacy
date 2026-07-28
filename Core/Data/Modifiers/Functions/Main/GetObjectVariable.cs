using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetObjectVariable : ModifierActionBase
    {
        #region Constructors

        public GetObjectVariable(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "getObjectVariable";
            if (isGroup)
                Name += "Other";
            SetupModifier("VAR");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Main;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (TryGetModifierReference(modifier, modifierLoop, isGroup, 1, out IModifierReference reference))
                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = reference.IntVariable.ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}

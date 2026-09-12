using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetObjectVariable : ModifierVariableBase
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

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => TryGetModifierReference(modifier, modifierLoop, isGroup, 1, out IModifierReference reference) ? reference.IntVariable.ToString() : null;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
            }
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion
    }
}

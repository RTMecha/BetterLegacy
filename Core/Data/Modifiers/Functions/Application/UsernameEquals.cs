using BetterLegacy.Configs;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class UsernameEquals : ModifierTriggerBase
    {
        #region Constructors

        public UsernameEquals() => SetupModifier("Player");

        #endregion

        #region Values

        public override string Name => "usernameEquals";

        public override CategoryType Category => CategoryType.Application;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => CoreConfig.Instance.DisplayName.Value == modifier.GetValue(0, modifierLoop.variables);

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Username", 0);
        }

        #endregion
    }
}

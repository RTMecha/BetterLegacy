using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LanguageEquals : ModifierTriggerBase
    {
        #region Constructors

        public LanguageEquals() => SetupModifier("0");

        #endregion

        #region Values

        public override string Name => "languageEquals";

        public override ModifierCategoryType Category => ModifierCategoryType.Application;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => CoreConfig.Instance.Language.Value == (Language)modifier.GetInt(0, 0, modifierLoop.variables);

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Language", 0, CoreHelper.ToOptionData<Language>());
        }

        #endregion
    }
}

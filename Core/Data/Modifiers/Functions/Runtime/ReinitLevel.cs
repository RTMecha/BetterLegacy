using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ReinitLevel : ModifierActionBase
    {
        #region Constructors

        public ReinitLevel() => SetupModifier(false);

        #endregion

        #region Values

        public override string Name => "updateObjects";

        public override CategoryType Category => CategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!modifier.constant)
                CoroutineHelper.StartCoroutine(RTLevel.IReinit());
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}

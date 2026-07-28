using BetterLegacy.Core.Runtime.Objects;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ObjectCustomActive : ModifierTriggerBase
    {
        #region Constructors

        public ObjectCustomActive() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "objectCustomActive";

        public override CategoryType Category => CategoryType.Enable;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifierLoop.reference.GetRuntimeObject() is ICustomActivatable customActivatable && customActivatable.CustomActive;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}

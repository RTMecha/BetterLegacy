using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ObjectActive : ModifierTriggerBase
    {
        #region Constructors

        public ObjectActive() => SetupModifier();

        #endregion

        #region Values

        public override string Name => "objectActive";

        public override CategoryType Category => CategoryType.Enable;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var runtimeObject = modifierLoop.reference.GetRuntimeObject();
            return runtimeObject != null ? runtimeObject.Active : modifierLoop.reference is Beatmap.ILifetime lifetime && lifetime.Alive;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}

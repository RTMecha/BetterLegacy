using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class MusicTimeInRange : ModifierTriggerBase
    {
        #region Constructors

        public MusicTimeInRange() => SetupModifier(string.Empty, "0", "0");

        #endregion

        #region Values

        public override string Name => "musicTimeInRange";

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        // this modifier is just for VG compatibility
        public override bool DisplayInEditor => false;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var time = modifierLoop.reference.GetParentRuntime().FixedTime;
            return modifier.values.Count > 2 && time >= modifier.GetFloat(1, 0f, modifierLoop.variables) - 0.01f && time <= modifier.GetFloat(2, 0f, modifierLoop.variables) + 0.1f;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetAudioProperty : ModifierVariableBase
    {
        #region Constructors

        public GetAudioProperty(Property property)
        {
            this.property = property;
            Name = "get" + property.ToString();
            SetupModifier(property == Property.MusicTime ? "MUSIC_TIME_VAR" : "PITCH_VAR");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Audio;

        readonly Property property;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => property switch
        {
            Property.MusicTime => AudioManager.inst.CurrentAudioSource.time.ToString(),
            _ => AudioManager.inst.CurrentAudioSource.pitch.ToString(),
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            Pitch,
            MusicTime,
        }

        #endregion
    }
}

using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GameState : ModifierTriggerBase
    {
        #region Constructors

        public GameState(Property property)
        {
            this.property = property;
            Name = property.ToString();
            SetupModifier();
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        readonly Property property;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => property switch
        {
            Property.inZenMode => RTBeatmap.Current.Invincible,
            Property.inNormal => RTBeatmap.Current.IsNormal,
            Property.in1Life => RTBeatmap.Current.Is1Life,
            Property.inNoHit => RTBeatmap.Current.IsNoHit,
            Property.inPractice => RTBeatmap.Current.IsPractice,
            Property.inEditor => ProjectArrhythmia.State.InEditor,
            Property.isEditing => ProjectArrhythmia.State.IsEditing,
            Property.inLobby => ProjectArrhythmia.State.IsInLobby,
            Property.isHosting => ProjectArrhythmia.State.IsHosting,
            _ => false,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion

        #region Sub Classes

        public enum Property
        {
            inZenMode,
            inNormal,
            in1Life,
            inNoHit,
            inPractice,
            inEditor,
            isEditing,
            inLobby,
            isHosting,
        }

        #endregion
    }
}

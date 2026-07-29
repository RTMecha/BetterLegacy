using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetCurrentLevelProperty : ModifierVariableBase
    {
        #region Constructors

        public GetCurrentLevelProperty(Property property)
        {
            this.property = property;
            Name = "getCurrent" + property.ToString();
            SetupModifier($"{RTString.SplitWords(property.ToString()).Replace(" ", "_").ToUpper()}_VAR");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        readonly Property property;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => property switch
        {
            Property.ID => LevelManager.CurrentLevel ? LevelManager.CurrentLevel.id : ProjectArrhythmia.State.InEditor && EditorLevelManager.inst.CurrentLevel ? EditorLevelManager.inst.CurrentLevel.id : null,
            Property.ArtistName => MetaData.Current?.artist?.name,
            Property.SongTitle => MetaData.Current?.song?.title,
            Property.LevelName => MetaData.Current?.beatmap?.name,
            Property.LevelRank => LevelManager.GetLevelRank(RTBeatmap.Current.hits).Ordinal.ToString(),
            _ => null,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            ID,
            ArtistName,
            SongTitle,
            LevelName,
            LevelRank,
        }

        #endregion
    }
}

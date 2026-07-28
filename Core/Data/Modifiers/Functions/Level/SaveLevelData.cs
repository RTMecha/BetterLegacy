using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SaveLevelData : ModifierActionBase
    {
        #region Constructors

        public SaveLevelData() => SetupModifier(false);

        #endregion

        #region Values

        public override string Name => "saveLevelData";

        public override CategoryType Category => CategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (ProjectArrhythmia.State.InEditor || modifier.constant || !LevelManager.CurrentLevel)
                return;

            LevelManager.UpdateCurrentLevelProgress();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}

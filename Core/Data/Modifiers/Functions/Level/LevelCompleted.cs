using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LevelCompleted : ModifierTriggerBase
    {
        #region Constructors

        public LevelCompleted(bool isOther)
        {
            this.isOther = isOther;
            Name = "levelCompleted";
            if (isOther)
                Name += "Other";
            SetupModifier();
            if (isOther)
                Modifier.values.Add("0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Level;

        readonly bool isOther;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isOther)
            {
                var id = modifier.GetValue(0, modifierLoop.variables);
                return ProjectArrhythmia.State.InEditor || LevelManager.Levels.TryFind(x => x.id == id, out Level.Level level) && level.saveData && level.saveData.Completed;
            }
            return ProjectArrhythmia.State.InEditor || LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.Completed;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isOther)
                modifierCard.StringGenerator(modifier, reference, "ID", 0);
        }

        #endregion
    }
}

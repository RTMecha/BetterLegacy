using BetterLegacy.Configs;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AchievementModifier : ModifierActionBase
    {
        #region Constructors

        public AchievementModifier(bool unlock)
        {
            this.unlock = unlock;
            Name = unlock ? "unlockAchievement" : "lockAchievement";
            SetupModifier(false, "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly bool unlock;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = modifier.GetValue(0, modifierLoop.variables);

            if (unlock && ProjectArrhythmia.State.InEditor)
            {
                if (!EditorConfig.Instance.ModifiersDisplayAchievements.Value)
                    return;

                var achievement = AchievementEditor.inst.achievements.Find(x => x.id == id);
                AchievementManager.inst.ShowAchievement(achievement);
                return;
            }

            if (!LevelManager.CurrentLevel)
                return;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);
            if (unlock)
                LevelManager.CurrentLevel.saveData.UnlockAchievement(id);
            else
                LevelManager.CurrentLevel.saveData.LockAchievement(modifier.GetValue(0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var id = modifierCard.StringGenerator(modifier, reference, "ID", 0);
            EditorContextMenu.AddContextMenu(id.transform.Find("Input").gameObject,
                new ButtonElement("Select Achievement", () => AchievementEditor.inst.OpenPopup(achievement => modifierCard.SetValue(0, achievement.id, reference))));
        }

        #endregion
    }
}

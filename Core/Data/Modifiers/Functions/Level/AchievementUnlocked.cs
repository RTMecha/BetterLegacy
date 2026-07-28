using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AchievementUnlocked : ModifierTriggerBase
    {
        #region Constructors

        public AchievementUnlocked() => SetupModifier("0", "False");

        #endregion

        #region Values

        public override string Name => "achievementUnlocked";

        public override CategoryType Category => CategoryType.Level;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => modifier.GetBool(1, false, modifierLoop.variables) ?
                AchievementManager.unlockedCustomAchievements.TryGetValue(modifier.GetValue(0, modifierLoop.variables), out bool global) && global :
                LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.AchievementUnlocked(modifier.GetValue(0, modifierLoop.variables));

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var id = modifierCard.StringGenerator(modifier, reference, "ID", 0);
            EditorContextMenu.AddContextMenu(id.transform.Find("Input").gameObject,
                new ButtonElement("Select Achievement", () => AchievementEditor.inst.OpenPopup(achievement => modifierCard.SetValue(0, achievement.id, reference))));
            modifierCard.BoolGenerator(modifier, reference, "Global", 1);
        }

        #endregion
    }
}

using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetAchievementUnlocked : ModifierVariableBase
    {
        #region Constructors

        public GetAchievementUnlocked() => SetupModifier("UNLOCKED_VAR", "0", "False");

        #endregion

        #region Values

        public override string Name => "getAchievementUnlocked";

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!LevelManager.CurrentLevel)
                return null;

            if (!LevelManager.CurrentLevel.saveData)
                LevelManager.AssignSaveData(LevelManager.CurrentLevel);

            // global or local
            var unlocked = modifier.GetBool(2, false, modifierLoop.variables) ?
                AchievementManager.unlockedCustomAchievements.TryGetValue(modifier.GetValue(1, modifierLoop.variables), out bool global) && global :
                LevelManager.CurrentLevel && LevelManager.CurrentLevel.saveData && LevelManager.CurrentLevel.saveData.AchievementUnlocked(modifier.GetValue(1, modifierLoop.variables));
            return unlocked.ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0);
            var id = modifierCard.StringGenerator(modifier, reference, "ID", 1);
            EditorContextMenu.AddContextMenu(id.transform.Find("Input").gameObject,
                new ButtonElement("Select Achievement", () => AchievementEditor.inst.OpenPopup(achievement => modifierCard.SetValue(1, achievement.id, reference))));
            modifierCard.BoolGenerator(modifier, reference, "Global", 2);
        }

        #endregion
    }
}

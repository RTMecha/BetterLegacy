using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Story;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadStoryLevel : ModifierActionBase
    {
        #region Constructors

        public LoadStoryLevel() => SetupModifier(false, "False", "0", "0", "False", "0");

        #endregion

        #region Values

        public override string Name => "loadStoryLevelDEVONLY";

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.StoryOnlyCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (ProjectArrhythmia.State.InStory)
                StoryManager.inst.Play(new StorySelection
                {
                    chapter = modifier.GetInt(1, 0, modifierLoop.variables),
                    level = modifier.GetInt(2, 0, modifierLoop.variables),
                    cutsceneIndex = modifier.GetInt(4, 0, modifierLoop.variables),
                    bonus = modifier.GetBool(0, false, modifierLoop.variables),
                    skipCutscenes = modifier.GetBool(3, false, modifierLoop.variables)
                });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.IntegerGenerator(modifier, reference, "Chapter", 1, 0);
            modifierCard.IntegerGenerator(modifier, reference, "Level", 2, 0);
            modifierCard.BoolGenerator(modifier, reference, "Bonus", 0, false);
            modifierCard.BoolGenerator(modifier, reference, "Skip Cutscene", 3, false);
        }

        #endregion
    }
}

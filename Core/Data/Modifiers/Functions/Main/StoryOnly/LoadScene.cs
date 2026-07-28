using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadScene : ModifierActionBase
    {
        #region Constructors

        public LoadScene() => SetupModifier(false, "Interface", "False");

        #endregion

        #region Values

        public override string Name => "loadSceneDEVONLY";

        public override CategoryType Category => CategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.StoryOnlyCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (ProjectArrhythmia.State.InStory)
                SceneManager.inst.LoadScene(modifier.GetValue(0, modifierLoop.variables), modifier.values.Count > 1 && modifier.GetBool(1, true, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Scene", 0);
            if (modifier.values.Count > 1)
                modifierCard.BoolGenerator(modifier, reference, "Show Loading", 1, true);
        }

        #endregion
    }
}

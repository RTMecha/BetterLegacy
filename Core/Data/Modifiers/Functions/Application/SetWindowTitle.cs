using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetWindowTitle : ModifierActionBase
    {
        #region Constructors

        public SetWindowTitle(bool isReset)
        {
            this.isReset = isReset;
            Name = isReset ? "resetWindowTitle" : "setWindowTitle";
            if (!isReset)
                SetupModifier("Project Arrhythmia");
            else
                SetupModifier();
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Application;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly bool isReset;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isReset)
                ProjectArrhythmia.Window.ResetTitle();
            else
                ProjectArrhythmia.Window.SetTitle(modifier.GetValue(0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (!isReset)
                modifierCard.StringGenerator(modifier, reference, "Title", 0);
        }

        #endregion
    }
}

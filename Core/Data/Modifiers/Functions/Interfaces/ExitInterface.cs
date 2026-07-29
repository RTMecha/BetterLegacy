using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Menus;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ExitInterface : ModifierActionBase
    {
        #region Constructors

        public ExitInterface() => SetupModifier(false);

        #endregion

        #region Values

        public override string Name => "exitInterface";

        public override ModifierCategoryType Category => ModifierCategoryType.Interfaces;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            InterfaceManager.inst.CloseMenus();
            if (ProjectArrhythmia.State.Paused)
                RTBeatmap.Current.Resume();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable) { }

        #endregion
    }
}

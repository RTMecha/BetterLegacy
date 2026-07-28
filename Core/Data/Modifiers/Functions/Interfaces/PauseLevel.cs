using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Menus.UI.Interfaces;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PauseLevel : ModifierActionBase
    {
        #region Constructors

        public PauseLevel() => SetupModifier(false);

        #endregion

        #region Values

        public override string Name => "pauseLevel";

        public override CategoryType Category => CategoryType.Interfaces;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            // only host can do this
            if (ProjectArrhythmia.State.IsClient)
                return;

            if (ProjectArrhythmia.State.InEditor)
            {
                EditorManager.inst.DisplayNotification("Cannot pause in the editor. This modifier only works in the Arcade.", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            PauseInterface.Pause();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}

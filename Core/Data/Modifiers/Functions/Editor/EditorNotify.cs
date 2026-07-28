using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EditorNotify : ModifierActionBase
    {
        #region Constructors

        public EditorNotify() => SetupModifier(false, "Modifier triggered!", "2", "1");

        #endregion

        #region Values

        public override string Name => "editorNotify";

        public override CategoryType Category => CategoryType.Editor;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var text = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);

            if (ProjectArrhythmia.State.InEditor)
                EditorManager.inst.DisplayNotification(
                    /*text: */ text,
                    /*time: */ modifier.GetFloat(1, 0.5f, modifierLoop.variables),
                    /*type: */ (EditorManager.NotificationType)modifier.GetInt(2, 0, modifierLoop.variables));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Text", 0);
            modifierCard.SingleGenerator(modifier, reference, "Time", 1, 0.5f);
            modifierCard.DropdownGenerator(modifier, reference, "Notify Type", 2, CoreHelper.StringToOptionData("Info", "Success", "Error", "Warning"));
        }

        #endregion
    }
}

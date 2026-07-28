using UnityEngine.UI;

using BetterLegacy.Configs;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EndLevel : ModifierActionBase
    {
        #region Constructors

        public EndLevel() => SetupModifier("0", string.Empty, "True");

        #endregion

        #region Values

        public override string Name => "endLevel";

        public override CategoryType Category => CategoryType.Level;

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
                if (!EditorManager.inst.isEditing && EditorConfig.Instance.ExitPreviewOnEnd.Value)
                    RTEditor.inst.ExitPreview();

                EditorManager.inst.DisplayNotification("End level func", 1f, EditorManager.NotificationType.Success);
                return;
            }

            var endLevelFunc = modifier.GetInt(0, 0, modifierLoop.variables);

            if (endLevelFunc > 0)
            {
                RTBeatmap.Current.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                RTBeatmap.Current.endLevelData = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);
            }
            RTBeatmap.Current.endLevelUpdateProgress = modifier.GetBool(2, true, modifierLoop.variables);

            LevelManager.EndLevel();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            var options = CoreHelper.ToOptionData<EndLevelFunction>();
            options.Insert(0, new Dropdown.OptionData("Default"));
            modifierCard.DropdownGenerator(modifier, reference, "End Level Function", 0, options);
            modifierCard.StringGenerator(modifier, reference, "End Level Data", 1);
            modifierCard.BoolGenerator(modifier, reference, "Save Player Data", 2, true);
        }

        #endregion
    }
}

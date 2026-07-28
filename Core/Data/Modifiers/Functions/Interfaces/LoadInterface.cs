using System.Collections.Generic;

using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Menus;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadInterface : ModifierActionBase
    {
        #region Constructors

        public LoadInterface() => SetupModifier(false, "interface_file_name", "True", "False");

        #endregion

        #region Values

        public override string Name => "loadInterface";

        public override CategoryType Category => CategoryType.Interfaces;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            // only host can do this
            if (ProjectArrhythmia.State.IsClient)
                return;

            if (ProjectArrhythmia.State.IsEditing) // don't want interfaces to load in editor
            {
                EditorManager.inst.DisplayNotification($"Cannot load interface in the editor!", 1f, EditorManager.NotificationType.Warning);
                return;
            }

            var value = modifier.GetValue(0, modifierLoop.variables);
            var path = RTFile.CombinePaths(RTFile.BasePath, value + FileFormat.LSI.Dot());

            if (!RTFile.FileExists(path))
            {
                CoreHelper.LogError($"Interface with file name: \"{value}\" does not exist.");
                return;
            }

            Dictionary<string, JSONNode> customVariables = null;
            if (modifier.GetBool(2, false, modifierLoop.variables))
            {
                customVariables = new Dictionary<string, JSONNode>();
                foreach (var variable in modifierLoop.variables)
                    customVariables[variable.Key] = variable.Value;
            }

            InterfaceManager.inst.ParseInterface(path, customVariables: customVariables);

            InterfaceManager.inst.MainDirectory = RTFile.BasePath;

            if (modifier.GetBool(1, true, modifierLoop.variables))
                RTBeatmap.Current.Pause();
            ArcadeHelper.endedLevel = false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Path", 0);
            modifierCard.BoolGenerator(modifier, reference, "Pause Level", 1);
            modifierCard.BoolGenerator(modifier, reference, "Pass Variables", 2);
        }

        #endregion
    }
}

using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadJSONExists : ModifierTriggerBase
    {
        #region Constructors

        public LoadJSONExists() => SetupModifier("save_file", "chapter/0/data");

        #endregion

        #region Values

        public override string Name => "loadJSONExists";

        public override ModifierCategoryType Category => ModifierCategoryType.JSON;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var path = modifier.GetValue(0, modifierLoop.variables);
            var jsonPath = modifier.GetValue(1, modifierLoop.variables);
            if (ProjectArrhythmia.State.IsClient)
                return LobbyInfo.HostJSONFileTriggers.TryGetValue(path + jsonPath, out bool hostTrigger) && hostTrigger;

            var active = RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(path), out string json) && JSON.Parse(json).GetPath(jsonPath) != null;
            if (ProjectArrhythmia.State.IsHosting)
                NetworkFunction.SendHostJSONTrigger(path + jsonPath, active);
            return active;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Path", 0);
            modifierCard.StringGenerator(modifier, reference, "JSON Path", 1);
        }

        #endregion
    }
}

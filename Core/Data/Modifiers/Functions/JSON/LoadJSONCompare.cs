using SimpleJSON;

using BetterLegacy.Core.Data.Network;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadJSONCompare : ModifierTriggerBase
    {
        #region Constructors

        public LoadJSONCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "loadJSON" + comparison.ToString();
            SetupModifier("save_file", "chapter/0/data", "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.JSON;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var path = modifier.GetValue(0, modifierLoop.variables);
            var jsonPath = modifier.GetValue(1, modifierLoop.variables);
            if (ProjectArrhythmia.State.IsClient)
                return LobbyInfo.HostJSONFileTriggers.TryGetValue(path + jsonPath, out bool hostTrigger) && hostTrigger;

            var value = modifier.GetValue(2, modifierLoop.variables);
            if (RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(path), out string json))
            {
                var jn = JSON.Parse(json);
                var j = jn.GetPath(jsonPath);
                var active = comparison == NumberComparison.Equals ? j == value : !string.IsNullOrEmpty(j) && comparison.Compare(j.AsFloat, Parser.TryParse(value, 0f));
                if (ProjectArrhythmia.State.IsHosting)
                    NetworkFunction.SendHostJSONTrigger(path + jsonPath, active);
                return active;
            }

            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Path", 0);
            modifierCard.StringGenerator(modifier, reference, "JSON Path", 1);
            if (comparison == NumberComparison.Equals)
                modifierCard.StringGenerator(modifier, reference, "Value", 2);
            else
                modifierCard.SingleGenerator(modifier, reference, "Value", 2);
        }

        #endregion
    }
}

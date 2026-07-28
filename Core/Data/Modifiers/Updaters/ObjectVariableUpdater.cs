using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class ObjectVariableUpdater : ModifierUpdaterBase
    {
        List<string> names = new List<string>
        {
            "addVariable",
            "subVariable",
            "setVariable",
            "setVariableMath",
            "addVariableOther",
            "subVariableOther",
            "setVariableOther",
            "setVariableMathOther",
            "variableEquals",
            "variableLesserEquals",
            "variableGreaterEquals",
            "variableLesser",
            "variableGreater",
            "variableOtherEquals",
            "variableOtherLesserEquals",
            "variableOtherGreaterEquals",
            "variableOtherLesser",
            "variableOtherGreater",
        };

        public override bool RequiresUpdate(Modifier modifier) => !string.IsNullOrEmpty(modifier.Name) && names.Contains(modifier.Name);

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            var mightBeOldModifiers = modifier.Name == "addVariable" || modifier.Name == "subVariable" || modifier.Name == "setVariable";
            modifier.name = modifier.Name.Replace("Variable", "ObjectVariable").Replace("variable", "objectVariable");
            if (mightBeOldModifiers && modifier.values.Count == 2)
                modifier.name += "Other";
        }
    }
}

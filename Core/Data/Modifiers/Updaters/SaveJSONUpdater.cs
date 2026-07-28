using BetterLegacy.Core.Helpers;

namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class SaveJSONUpdater : ModifierUpdaterBase
    {
        const string SAVE_JSON_VAR = "SAVE_JSON_VAR";

        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "saveFloat" || modifier.Name == "saveString" || modifier.Name == "saveVariable" || modifier.Name == "saveText";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            var path = modifier.GetValue(2) + "/" + modifier.GetValue(3);
            if (modifier.Name == "saveFloat" || modifier.Name == "saveVariable")
                path += "/float";
            if (modifier.Name == "saveString" || modifier.Name == "saveText")
                path += "/string";
            modifier.values.RemoveAt(2); // old
            modifier.values[1] = path;
            if (modifier.Name == "saveVariable")
            {
                var index = modifyable.Modifiers.IndexOf(modifier);
                var getObjectVariable = ModifierFunctions.getObjectVariable.Modifier.Copy();
                getObjectVariable.values[0] = SAVE_JSON_VAR;
                modifier.values[0] = SAVE_JSON_VAR;
                modifyable.Modifiers.Insert(index, getObjectVariable);
            }
            if (modifier.Name == "saveText")
            {
                var index = modifyable.Modifiers.IndexOf(modifier);
                var getText = ModifierFunctions.getText.Modifier.Copy();
                getText.values[0] = SAVE_JSON_VAR;
                modifier.values[0] = SAVE_JSON_VAR;
                modifyable.Modifiers.Insert(index, getText);
            }
            modifier.name = "saveJSON";
        }
    }
}

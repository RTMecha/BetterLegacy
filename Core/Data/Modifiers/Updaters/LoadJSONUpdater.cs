namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class LoadJSONUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "loadEquals" || modifier.Name == "loadLesserEquals" || modifier.Name == "loadGreaterEquals" || modifier.Name == "loadLesser" || modifier.Name == "loadGreater" || modifier.Name == "loadExists";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            var path = modifier.GetValue(2) + "/" + modifier.GetValue(3) + "/string";
            modifier.values.RemoveAt(2); // old
            modifier.values[2] = path;
            modifier.name = modifier.Name.Replace("load", "loadJSON");
        }
    }
}

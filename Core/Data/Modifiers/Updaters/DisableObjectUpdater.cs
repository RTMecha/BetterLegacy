namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class DisableObjectUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => !string.IsNullOrEmpty(modifier.Name) && modifier.Name.Contains("disableObject");

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.name = modifier.Name.Replace("disableObject", "enableObject");
            if (!modifier.values.IsEmpty())
                modifier.values[0] = "False";
        }
    }
}

namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class DisableObjectUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => !string.IsNullOrEmpty(modifier.Name) && modifier.Name.Contains("disableObject");

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.name = modifier.Name.Replace("disableObject", "enableObject");
            modifier.values.Add("False");
            if (modifier.Name == "enableObject")
            {
                modifier.SetValue(0, "False");
                modifier.version = 1;
            }
        }
    }
}

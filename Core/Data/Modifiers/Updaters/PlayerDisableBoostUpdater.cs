namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class PlayerDisableBoostUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => !string.IsNullOrEmpty(modifier.Name) && modifier.Name.Contains("playerDisableBoost");

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.name = modifier.Name.Replace("playerDisableBoost", "playerEnableBoost");
            if (modifier.values.Count == 1)
                modifier.values[0] = "False";
            else
                modifier.values.Add("False");
        }
    }
}

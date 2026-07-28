namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class SetPlayerVelocityUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => !string.IsNullOrEmpty(modifier.Name) && modifier.Name.Contains("playerVelocity");

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable) => modifier.name = modifier.name.Replace("playerVelocity", "setPlayerVelocity");
    }
}

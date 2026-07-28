namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class SetGlobalPlayerSpeedUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "playerSpeed";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable) => modifier.name = "setGlobalPlayerSpeed";
    }
}

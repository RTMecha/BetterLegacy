namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class ReinitLevelUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "updateObjects";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable) => modifier.name = "reinitLevel";
    }
}

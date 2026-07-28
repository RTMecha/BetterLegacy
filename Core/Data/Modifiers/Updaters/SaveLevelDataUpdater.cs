namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class SaveLevelDataUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "saveLevelRank";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.name = "saveLevelData";
            modifier.values.Clear(); // saveLevelData should have no values.
        }
    }
}

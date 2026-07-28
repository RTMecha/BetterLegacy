namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class ObjectActiveOtherUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "objectAlive";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable) => modifier.name = "objectActiveOther";
    }
}

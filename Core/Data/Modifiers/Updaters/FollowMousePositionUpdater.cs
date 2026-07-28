namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class FollowMousePositionUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "followMousePosition" && modifier.version == 0;

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.GetValue(0) == "0")
                modifier.SetValue(0, "1");
            modifier.version++;
        }
    }
}

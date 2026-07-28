namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class SetBGActiveUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "setBGActive";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.values.Add("False");
            modifier.values.Move(0, 1);
        }
    }
}

namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class SetActiveUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "setActive" || modifier.Name == "setActiveOther";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.name = modifier.Name.Replace("setActive", "enableObject");
            if (modifier.Name == "enableObjectOther")
            {
                var active = modifier.GetBool(0, true);
                var tag = modifier.GetValue(1);
                modifier.values.Clear();
                modifier.values.Add(tag);
                modifier.values.Add("False");
                modifier.values.Add(active.ToString());
            }
        }
    }
}

namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class HideMouseUpdater : ModifierUpdaterBase
    {
        public override bool RequiresUpdate(Modifier modifier) => modifier.Name == "hideMouse";

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.name = "showMouse";
            if (modifier.values.Count == 1)
                modifier.values[0] = "False";
            else
                modifier.values.Add("False");
        }
    }
}

namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class RealTimeUpdater : ModifierUpdaterBase
    {
        public RealTimeUpdater(int timeScaleToUpdate)
        {
            this.timeScaleToUpdate = timeScaleToUpdate;
            name = timeScaleToUpdate switch
            {
                1 => "realTimeSecond",
                2 => "realTimeMinute",
                3 => "realTime12Hour",
                4 => "realTime24Hour",
                5 => "realTimeDay",
                6 => "realTimeMonth",
                7 => "realTimeYear",
                8 => "realTimeDayWeek",
                _ => null,
            };
        }

        readonly int timeScaleToUpdate;
        readonly string name;

        public override bool RequiresUpdate(Modifier modifier) => !string.IsNullOrEmpty(modifier.Name) && modifier.Name.Contains(name);

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.values.Insert(0, timeScaleToUpdate.ToString());
            modifier.name = modifier.Name.Replace(name, "realTime");
        }
    }
}

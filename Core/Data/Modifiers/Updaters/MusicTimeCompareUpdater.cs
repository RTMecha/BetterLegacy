using System.Collections.Generic;

namespace BetterLegacy.Core.Data.Modifiers.Updaters
{
    public class MusicTimeCompareUpdater : ModifierUpdaterBase
    {
        List<string> names = new List<string>
        {
            "timeEquals",
            "timeLesserEquals",
            "timeGreaterEquals",
            "timeLesser",
            "timeGreater",
        };

        public override bool RequiresUpdate(Modifier modifier) => !string.IsNullOrEmpty(modifier.Name) && names.Contains(modifier.Name);

        public override void UpdateModifier(Modifier modifier, IModifyable modifyable)
        {
            modifier.name = modifier.Name.Replace("time", "musicTime");
        }
    }
}

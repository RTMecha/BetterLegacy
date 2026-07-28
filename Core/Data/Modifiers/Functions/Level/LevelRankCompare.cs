using System.Linq;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LevelRankCompare : ModifierTriggerBase
    {
        #region Constructors

        public LevelRankCompare(From from, NumberComparison comparison)
        {
            this.from = from;
            this.comparison = comparison;
            Name = "levelRank";
            if (from != From.CurrentLevel)
                Name += from.ToString();
            Name += comparison.ToString();
            SetupModifier("0");
            if (from == From.Other)
                Modifier.values.Add("0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Level;

        readonly From from;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (from)
            {
                case From.CurrentLevel: {
                        return ModifiersHelper.GetLevelRank(LevelManager.CurrentLevel, out int levelRankIndex) && levelRankIndex == modifier.GetInt(0, 0, modifierLoop.variables);
                    }
                case From.Other: {
                        var id = modifier.GetValue(1, modifierLoop.variables);
                        return LevelManager.Levels.TryFind(x => x.id == id, out Level.Level level) && ModifiersHelper.GetLevelRank(level, out int levelRankIndex) && comparison.Compare(levelRankIndex, modifier.GetInt(0, 0, modifierLoop.variables));
                    }
                case From.Current: {
                        return comparison.Compare(LevelManager.GetLevelRank(RTBeatmap.Current.hits), modifier.GetInt(0, 0, modifierLoop.variables));
                    }
            }
            return false;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (from == From.Other)
                modifierCard.StringGenerator(modifier, reference, "ID", 1);

            modifierCard.DropdownGenerator(modifier, reference, "Rank", 0, Rank.Null.GetNames().ToList());
        }

        #endregion

        #region Sub Classes

        public enum From
        {
            CurrentLevel,
            Other,
            Current,
        }

        #endregion
    }
}

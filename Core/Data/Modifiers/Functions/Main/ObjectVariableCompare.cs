using System.Linq;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ObjectVariableCompare : ModifierTriggerBase
    {
        #region Constructors

        public ObjectVariableCompare(NumberComparison comparison, bool isGroup)
        {
            this.comparison = comparison;
            this.isGroup = isGroup;
            Name = "objectVariable";
            if (isGroup)
                Name += "Other";
            Name += comparison.ToString();
            SetupModifier("1");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Main;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : base.Compatibility;

        readonly NumberComparison comparison;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var value = modifier.GetInt(0, 0, modifierLoop.variables);
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return false;
                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, modifierLoop.variables));
                return !list.IsEmpty() && list.Any(x => comparison.Compare(x.IntVariable, value));
            }
            return modifierLoop.reference is IModifyable modifyable && comparison.Compare(modifyable.IntVariable, value);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);
            }
            modifierCard.IntegerGenerator(modifier, reference, "Compare To", 0);
        }

        #endregion
    }
}

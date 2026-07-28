using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AxisCompare : ModifierTriggerBase
    {
        #region Constructors

        public AxisCompare(NumberComparison comparison)
        {
            this.comparison = comparison;
            Name = "axis" + comparison.ToString();
            SetupModifier(1, "Object Group", "0", "0", "0", "1", "0", "-99999", "99999", "1", "0", "99999", "True");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Animation;

        readonly NumberComparison comparison;

        #endregion

        #region Functions
        
        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.version != 0)
                return;

            if (modifier.version == 0)
            {
                if (modifier.GetInt(1, 0) == 2 && modifier.GetInt(2, 0) == 0 && !modifier.GetBool(9, false))
                    modifier.SetValue(2, "2");
                modifier.version++;
            }
            if (modifier.version == 1)
            {
                var axisSourceRaw = modifier.GetValue(9);
                if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                    modifier.SetValue(9, "0");
                if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                    modifier.SetValue(9, "1");
                modifier.version++;
            }
        }

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable == null)
                return false;

            var tag = modifier.GetValue(0, modifierLoop.variables);

            int fromType = modifier.GetInt(1, 0, modifierLoop.variables);
            int fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);

            float delay = modifier.GetFloat(3, 0f, modifierLoop.variables);
            float multiply = modifier.GetFloat(4, 0f, modifierLoop.variables);
            float offset = modifier.GetFloat(5, 0f, modifierLoop.variables);
            float min = modifier.GetFloat(6, -9999f, modifierLoop.variables);
            float max = modifier.GetFloat(7, 9999f, modifierLoop.variables);
            float equals = modifier.GetFloat(8, 0f, modifierLoop.variables);
            var axisSourceRaw = modifier.GetValue(9, modifierLoop.variables);
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                axisSourceRaw = "1";
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                axisSourceRaw = "0";
            var axisSource = Parser.TryParse(axisSourceRaw, true, AxisSource.Sequence);
            float loop = modifier.GetFloat(10, 9999f, modifierLoop.variables);
            var offsetAudio = modifier.GetBool(11, true, modifierLoop.variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }
            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return false;

            fromType = RTMath.Clamp(fromType, 0, beatmapObject.events.Count);

            var t = !offsetAudio ? delay : ModifiersHelper.GetTime(beatmapObject) - beatmapObject.StartTime - delay;

            return fromType >= 0 && fromType <= 2 && comparison.Compare(ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, t, loop, axisSource, modifier.version), equals);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);

            modifierCard.DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
            modifierCard.DropdownGenerator(modifier, reference, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

            modifierCard.SingleGenerator(modifier, reference, "Delay", 3, 0f);

            modifierCard.SingleGenerator(modifier, reference, "Multiply", 4, 1f);
            modifierCard.SingleGenerator(modifier, reference, "Offset", 5, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Min", 6, -99999f);
            modifierCard.SingleGenerator(modifier, reference, "Max", 7, 99999f);
            modifierCard.SingleGenerator(modifier, reference, "Loop", 10, 99999f);
            var axisSourceRaw = modifier.GetValue(9);
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                modifier.SetValue(9, "0");
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                modifier.SetValue(9, "1");
            modifierCard.DropdownGenerator(modifier, reference, "Axis Source", 9, CoreHelper.ToOptionData<AxisSource>());
            modifierCard.BoolGenerator(modifier, reference, "Offset Audio", 11, true);

            modifierCard.SingleGenerator(modifier, reference, "Equals", 8, 1f);
        }

        #endregion
    }
}

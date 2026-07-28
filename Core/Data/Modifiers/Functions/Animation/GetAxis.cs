using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetAxis : ModifierActionBase
    {
        #region Constructors

        public GetAxis(bool isMath)
        {
            this.isMath = isMath;
            Name = "getAxis";
            if (isMath)
            {
                Name += "Math";
                SetupModifier(1, "AXIS_VAR", "0", "0", "0", "0", "Object Group", "axis + 0", "True");
            }
            else
                SetupModifier(1, "AXIS_VAR", "0", "0", "0", "1", "0", "-99999", "99999", "0", "99999", "Object Group", "True");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Animation;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        readonly bool isMath;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.version != 0)
                return;

            var value = modifier.GetValue(isMath ? 4 : 8);
            if (!string.IsNullOrEmpty(value) && value.ToLower() == "true")
                modifier.SetValue(isMath ? 4 : 8, "1");
            if (!string.IsNullOrEmpty(value) && value.ToLower() == "false")
                modifier.SetValue(isMath ? 4 : 8, "0");
            if (modifier.GetInt(1, 0) == 2 && modifier.GetInt(2, 0) == 0 && !modifier.GetBool(isMath ? 4 : 8, false))
                modifier.SetValue(2, "2");
            modifier.version++;
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable == null)
                return;

            int fromType = modifier.GetInt(1, 0, modifierLoop.variables);
            int fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);

            float delay = modifier.GetFloat(3, 0f, modifierLoop.variables);
            var axisSourceRaw = modifier.GetValue(isMath ? 4 : 8, modifierLoop.variables);
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                axisSourceRaw = "1";
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                axisSourceRaw = "0";
            var axisSource = Parser.TryParse(axisSourceRaw, true, AxisSource.Sequence);
            var tag = FormatStringVariables(modifier.GetValue(isMath ? 5 : 10, modifierLoop.variables), modifierLoop.variables);
            var offsetAudio = modifier.GetBool(isMath ? 7 : 11, true, modifierLoop.variables);

            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
            if (cache.tag != tag)
            {
                cache.UpdateCache(modifier, prefabable, tag);
                modifier.Result = cache;
            }

            var beatmapObject = cache.obj;
            if (!beatmapObject)
                return;

            fromType = RTMath.Clamp(fromType, 0, beatmapObject.events.Count);

            if (fromType < 0 || fromType > 2)
                return;

            var t = !offsetAudio ? delay : ModifiersHelper.GetTime(beatmapObject) - beatmapObject.StartTime - delay;

            if (isMath)
            {
                if (modifierLoop.reference is not IEvaluatable evaluatable)
                    return;

                var evaluation = FormatStringVariables(modifier.GetValue(6, modifierLoop.variables), modifierLoop.variables);
                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                numberVariables["axis"] = ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, t, axisSource);
                beatmapObject.SetOtherObjectVariables(numberVariables);

                float value = RTMath.Parse(evaluation, RTLevel.Current?.evaluationContext, numberVariables);

                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = value.ToString();
                return;
            }

            float multiply = modifier.GetFloat(4, 0f, modifierLoop.variables);
            float offset = modifier.GetFloat(5, 0f, modifierLoop.variables);
            float min = modifier.GetFloat(6, -9999f, modifierLoop.variables);
            float max = modifier.GetFloat(7, 9999f, modifierLoop.variables);
            float loop = modifier.GetFloat(9, 9999f, modifierLoop.variables);
            modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, min, max, offset, multiply, t, loop, axisSource, modifier.version).ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);

            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", isMath ? 5 : 10);

            modifierCard.DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
            modifierCard.DropdownGenerator(modifier, reference, "Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

            modifierCard.SingleGenerator(modifier, reference, "Delay", 3, 0f);

            if (!isMath)
            {
                modifierCard.SingleGenerator(modifier, reference, "Multiply", 4, 1f);
                modifierCard.SingleGenerator(modifier, reference, "Offset", 5, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Min", 6, -99999f);
                modifierCard.SingleGenerator(modifier, reference, "Max", 7, 99999f);
                modifierCard.SingleGenerator(modifier, reference, "Loop", 9, 99999f);
            }
            else
                modifierCard.StringGenerator(modifier, reference, "Expression", 6);
            var axisSourceRaw = modifier.GetValue(isMath ? 4 : 8);
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                modifier.SetValue(isMath ? 4 : 8, "0");
            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                modifier.SetValue(isMath ? 4 : 8, "1");
            modifierCard.DropdownGenerator(modifier, reference, "Axis Source", isMath ? 4 : 8, CoreHelper.ToOptionData<AxisSource>());
            modifierCard.BoolGenerator(modifier, reference, "Offset Audio", isMath ? 7 : 11, true);

            if (modifier.version == 0)
                modifierCard.AddGenerator(modifier, reference, "Update", () =>
                {
                    if (modifier.GetInt(1, 0) == 2 && modifier.GetInt(2, 0) == 0 && !modifier.GetBool(isMath ? 4 : 8, false))
                        modifier.SetValue(2, "2");
                    modifier.version = 1;
                });
        }

        #endregion
    }
}

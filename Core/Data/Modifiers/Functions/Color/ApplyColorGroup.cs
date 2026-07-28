using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ApplyColorGroup : ModifierActionBase
    {
        #region Constructors

        public ApplyColorGroup()
        {
            SetupModifier("Object Group", "0", "True", "True");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name => "applyColorGroup";

        public override CategoryType Category => CategoryType.Color;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            //if (modifier.version == 1)
            //    return;

            //modifier.values.RemoveAt(1); // From Type
            //modifier.values.RemoveAt(1); // From Axis
            //modifier.values.Insert(1, "0");
            //modifier.version = 1;
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(0, modifierLoop.variables)));

            var cachedSequences = beatmapObject.cachedSequences;
            if (list.IsEmpty() || !cachedSequences)
                return;

            var t = 0f;
            if (modifier.version == 0)
            {
                var type = modifier.GetInt(1, 0, modifierLoop.variables);
                var axis = modifier.GetInt(2, 0, modifierLoop.variables);

                var isEmpty = beatmapObject.objectType == BeatmapObject.ObjectType.Empty;
                var time = beatmapObject.GetParentRuntime().CurrentTime - beatmapObject.StartTime;

                t = !isEmpty ? type switch
                {
                    0 => cachedSequences.PositionSequence.Value.At(axis),
                    1 => cachedSequences.ScaleSequence.Value.At(axis),
                    2 => cachedSequences.RotationSequence.Value.At(axis),
                    _ => 0f
                } : type switch
                {
                    0 => cachedSequences.PositionSequence.GetValue(time).At(axis),
                    1 => cachedSequences.ScaleSequence.GetValue(time).At(axis),
                    2 => cachedSequences.RotationSequence.GetValue(time).At(axis),
                    _ => 0f
                };
            }
            else
                t = modifier.GetFloat(1, 0f, modifierLoop.variables);

            var overrideStartOpacity = modifier.GetBool(modifier.version == 0 ? 3 : 2, true, modifierLoop.variables);
            var overrideEndOpacity = modifier.GetBool(modifier.version == 0 ? 4 : 3, true, modifierLoop.variables);

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var time = beatmapObject.GetParentRuntime().CurrentTime - beatmapObject.StartTime;
                var colors = beatmapObject.GetColors(time);
                var color = colors.startColor;
                var secondColor = colors.endColor;

                foreach (var other in list)
                {
                    var otherRuntimeObject = other.runtimeObject;
                    if (!otherRuntimeObject)
                        continue;

                    if (!otherRuntimeObject.visualObject.isGradient)
                    {
                        var startColor = otherRuntimeObject.visualObject.GetPrimaryColor();
                        var col = Color.Lerp(startColor, color, t);
                        if (!overrideStartOpacity)
                            col.a = startColor.a;
                        otherRuntimeObject.visualObject.SetColor(col);
                    }
                    else if (otherRuntimeObject.visualObject is SolidObject solidObject)
                    {
                        var otherColors = solidObject.GetColors();
                        var startColor = Color.Lerp(otherColors.startColor, color, t);
                        if (!overrideStartOpacity)
                            startColor.a = otherColors.startColor.a;
                        var endColor = Color.Lerp(otherColors.endColor, secondColor, t);
                        if (!overrideEndOpacity)
                            endColor.a = otherColors.endColor.a;
                        solidObject.SetColor(startColor, endColor);
                    }
                }
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            if (modifier.version == 0)
            {
                modifierCard.DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                modifierCard.DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));
            }
            else
                modifierCard.SingleGenerator(modifier, reference, "Lerp", 1, max: 1f);
            modifierCard.BoolGenerator(modifier, reference, "Override Start Opacity", modifier.version == 0 ? 3 : 2, true);
            modifierCard.BoolGenerator(modifier, reference, "Override End Opacity", modifier.version == 0 ? 4 : 3, true);

            if (modifier.version == 0)
                modifierCard.AddGenerator(modifier, reference, "Update", () =>
                {
                    modifier.values.RemoveAt(1); // From Type
                    modifier.values.RemoveAt(1); // From Axis
                    modifier.values.Insert(1, "0");
                    modifier.version = 1;
                });
        }

        #endregion
    }
}

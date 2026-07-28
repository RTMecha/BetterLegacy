using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AnimateColorKF : ModifierActionBase
    {
        #region Constructors

        public AnimateColorKF(Mode mode)
        {
            this.mode = mode;
            Name = "animateColorKF";
            switch (mode)
            {
                case Mode.Slot: {
                        SetupModifier("0", "0", "0", "1", "0", "0", "0", "0", "1", "0", "0", "0");
                        break;
                    }
                case Mode.Hex: {
                        Name += mode.ToString();
                        SetupModifier("0", "FFFFFF", "FFFFFF");
                        break;
                    }
            }
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Color;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible.WithBackgroundObject();

        readonly Mode mode;

        #endregion

        #region Functions

        (List<IKeyframe<Color>>, List<IKeyframe<Color>>) GetKeyframes(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (mode)
            {
                case Mode.Slot: {
                        var colorSource = (ThemeSource)modifier.GetInt(1, 0, modifierLoop.variables);
                        // custom start colors
                        var colorSlot1Start = modifier.GetInt(2, 0, modifierLoop.variables);
                        var opacity1Start = modifier.GetFloat(3, 1f, modifierLoop.variables);
                        var hue1Start = modifier.GetFloat(4, 0f, modifierLoop.variables);
                        var saturation1Start = modifier.GetFloat(5, 0f, modifierLoop.variables);
                        var value1Start = modifier.GetFloat(6, 0f, modifierLoop.variables);
                        var colorSlot2Start = modifier.GetInt(7, 0, modifierLoop.variables);
                        var opacity2Start = modifier.GetFloat(8, 1f, modifierLoop.variables);
                        var hue2Start = modifier.GetFloat(9, 0f, modifierLoop.variables);
                        var saturation2Start = modifier.GetFloat(10, 0f, modifierLoop.variables);
                        var value2Start = modifier.GetFloat(11, 0f, modifierLoop.variables);

                        var currentTime = 0f;

                        var keyframes1 = new List<IKeyframe<Color>>();
                        keyframes1.Add(new CustomThemeKeyframe(currentTime, colorSource, colorSlot1Start, opacity1Start, hue1Start, saturation1Start, value1Start, Ease.Linear, false));
                        var keyframes2 = new List<IKeyframe<Color>>();
                        keyframes2.Add(new CustomThemeKeyframe(currentTime, colorSource, colorSlot2Start, opacity2Start, hue2Start, saturation2Start, value2Start, Ease.Linear, false));
                        for (int i = 12; i < modifier.values.Count; i += 14)
                        {
                            var time = modifier.GetFloat(i + 1, 0f, modifierLoop.variables);
                            var relative = modifier.GetBool(i + 12, true, modifierLoop.variables);
                            if (relative ? time < 0f : time < currentTime)
                                continue;

                            var colorSlot1 = modifier.GetInt(i + 2, 0, modifierLoop.variables);
                            var opacity1 = modifier.GetFloat(i + 3, 1f, modifierLoop.variables);
                            var hue1 = modifier.GetFloat(i + 4, 0f, modifierLoop.variables);
                            var saturation1 = modifier.GetFloat(i + 5, 0f, modifierLoop.variables);
                            var value1 = modifier.GetFloat(i + 6, 0f, modifierLoop.variables);
                            var colorSlot2 = modifier.GetInt(i + 7, 0, modifierLoop.variables);
                            var opacity2 = modifier.GetFloat(i + 8, 1f, modifierLoop.variables);
                            var hue2 = modifier.GetFloat(i + 9, 0f, modifierLoop.variables);
                            var saturation2 = modifier.GetFloat(i + 10, 0f, modifierLoop.variables);
                            var value2 = modifier.GetFloat(i + 11, 0f, modifierLoop.variables);

                            var easing = Parser.TryParse(modifier.GetValue(i + 13, modifierLoop.variables), true, Easing.Linear);
                            var ease = Ease.GetEaseFunction(easing);
                            keyframes1.Add(new CustomThemeKeyframe(relative ? currentTime + time : time, colorSource, colorSlot1, opacity1, hue1, saturation1, value1, ease, false));
                            keyframes2.Add(new CustomThemeKeyframe(relative ? currentTime + time : time, colorSource, colorSlot2, opacity2, hue2, saturation2, value2, ease, false));

                            currentTime = time;
                        }
                        break;
                    }
                case Mode.Hex: {
                        // custom start colors
                        var color1Start = modifier.GetValue(1, modifierLoop.variables);
                        var color2Start = modifier.GetValue(2, modifierLoop.variables);

                        var currentTime = 0f;

                        var keyframes1 = new List<IKeyframe<Color>>();
                        keyframes1.Add(new ColorKeyframe(currentTime, RTColors.HexToColor(color1Start), Ease.Linear));
                        var keyframes2 = new List<IKeyframe<Color>>();
                        keyframes2.Add(new ColorKeyframe(currentTime, RTColors.HexToColor(color2Start), Ease.Linear));
                        for (int i = 3; i < modifier.values.Count; i += 6)
                        {
                            var time = modifier.GetFloat(i + 1, 0f, modifierLoop.variables);
                            var relative = modifier.GetBool(i + 4, true, modifierLoop.variables);
                            if (relative ? time < 0f : time < currentTime)
                                continue;

                            var color1 = modifier.GetValue(i + 2, modifierLoop.variables);
                            var color2 = modifier.GetValue(i + 3, modifierLoop.variables);

                            var easing = Parser.TryParse(modifier.GetValue(i + 5, modifierLoop.variables), true, Easing.Linear);
                            var ease = Ease.GetEaseFunction(easing);
                            keyframes1.Add(new ColorKeyframe(relative ? currentTime + time : time, RTColors.HexToColor(color1), ease));
                            keyframes2.Add(new ColorKeyframe(relative ? currentTime + time : time, RTColors.HexToColor(color2), ease));

                            currentTime = time;
                        }
                        break;
                    }
            }
            return (null, null);
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not ILifetime lifetime)
                return;

            Sequence<Color> sequence1;
            Sequence<Color> sequence2;

            var audioTime = modifier.GetFloat(0, 0f, modifierLoop.variables);

            if (modifier.TryGetResult(out KeyValuePair<Sequence<Color>, Sequence<Color>> sequences))
            {
                sequence1 = sequences.Key;
                sequence2 = sequences.Value;
            }
            else
            {
                var set = GetKeyframes(modifier, modifierLoop);
                if (set.Item1 == null)
                    return;

                sequence1 = new Sequence<Color>(set.Item1);
                sequence2 = new Sequence<Color>(set.Item2);

                modifier.Result = new KeyValuePair<Sequence<Color>, Sequence<Color>>(sequence1, sequence2);
            }

            var beatmapObject = modifierLoop.reference as BeatmapObject;
            var backgroundObject = modifierLoop.reference as BackgroundObject;

            var startTime = lifetime.StartTime;

            RTLevel.Current.postTick.Enqueue(() =>
            {
                var primaryColor = Color.white;
                var secondaryColor = Color.white;

                primaryColor = sequence1.GetValue(audioTime - startTime);
                secondaryColor = sequence2.GetValue(audioTime - startTime);

                if (beatmapObject && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject)
                {
                    if (solidObject.isGradient)
                        solidObject.SetColor(primaryColor, secondaryColor);
                    else
                        solidObject.SetColor(primaryColor);
                }

                if (backgroundObject && backgroundObject.runtimeObject)
                    backgroundObject.runtimeObject.SetColor(primaryColor, secondaryColor);
            });
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Time", 0);

            switch (mode)
            {
                case Mode.Slot: {
                        modifierCard.DropdownGenerator(modifier, reference, "Color Source", 1, CoreHelper.ToOptionData<ThemeSource>(), onSelect: _val =>
                        {
                            modifier.SetValue(1, _val.ToString());
                            modifierCard.RenderModifier(reference);
                        });
                        var source = (ThemeSource)modifier.GetInt(1, 0);

                        modifierCard.ColorGenerator(modifier, reference, "Color 1 Start", 2, source);
                        modifierCard.SingleGenerator(modifier, reference, "Opacity 1 Start", 3, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Hue 1 Start", 4, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation 1 Start", 5, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value 1 Start", 6, 0f);

                        modifierCard.ColorGenerator(modifier, reference, "Color 2 Start", 7, source);
                        modifierCard.SingleGenerator(modifier, reference, "Opacity 2 Start", 8, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Hue 2 Start", 9, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation 2 Start", 10, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value 2 Start", 11, 0f);
                        break;
                    }
                case Mode.Hex: {
                        modifierCard.StringGenerator(modifier, reference, "Color 1", 1);
                        modifierCard.StringGenerator(modifier, reference, "Color 2", 2);
                        break;
                    }
            }

            int a = 0;
            for (int i = mode == Mode.Hex ? 3 : 12; i < modifier.values.Count; i += mode == Mode.Hex ? 6 : 14)
            {
                int groupIndex = i;
                var label = modifierCard.LabelGenerator($"- Keyframe {a + 1}");

                modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                {
                    for (int j = 0; j < (mode == Mode.Hex ? 6 : 14); j++)
                        modifier.values.RemoveAt(groupIndex);
                });

                var collapseValue = modifier.GetBool(i, false);
                modifierCard.BoolGenerator("Collapse Keyframe Editor", collapseValue, _val =>
                {
                    modifier.SetValue(groupIndex, _val.ToString());
                    var value = modifierCard.DialogScrollbarValue;
                    modifierCard.RenderModifier(reference);
                    CoroutineHelper.PerformAtNextFrame(() => modifierCard.DialogScrollbarValue = value);
                });

                if (collapseValue)
                    continue;

                modifierCard.SingleGenerator(modifier, reference, "Keyframe Time", i + 1);
                switch (mode)
                {
                    case Mode.Slot: {
                            modifierCard.EaseGenerator(modifier, reference, i + 13);

                            modifierCard.BoolGenerator(modifier, reference, "Relative", i + 12, true);

                            var source = (ThemeSource)modifier.GetInt(1, 0);
                            modifierCard.ColorGenerator(modifier, reference, "Color 1", i + 2, source);
                            modifierCard.SingleGenerator(modifier, reference, "Opacity 1", i + 3, 1f);
                            modifierCard.SingleGenerator(modifier, reference, "Hue 1", i + 4, 0f);
                            modifierCard.SingleGenerator(modifier, reference, "Saturation 1", i + 5, 0f);
                            modifierCard.SingleGenerator(modifier, reference, "Value 1", i + 6, 0f);

                            modifierCard.ColorGenerator(modifier, reference, "Color 2", i + 7, source);
                            modifierCard.SingleGenerator(modifier, reference, "Opacity 2", i + 8, 1f);
                            modifierCard.SingleGenerator(modifier, reference, "Hue 2", i + 9, 0f);
                            modifierCard.SingleGenerator(modifier, reference, "Saturation 2", i + 10, 0f);
                            modifierCard.SingleGenerator(modifier, reference, "Value 2", i + 11, 0f);
                            break;
                        }
                    case Mode.Hex: {
                            modifierCard.EaseGenerator(modifier, reference, i + 5);

                            modifierCard.BoolGenerator(modifier, reference, "Relative", i + 4, true);

                            modifierCard.StringGenerator(modifier, reference, "Color 1", i + 2);
                            modifierCard.StringGenerator(modifier, reference, "Color 2", i + 3);
                            break;
                        }
                }

                a++;
            }

            modifierCard.AddGenerator(modifier, reference, "Add Keyframe", () =>
            {
                switch (mode)
                {
                    case Mode.Slot: {
                            modifier.values.Add("False"); // collapse keyframe
                            modifier.values.Add("0"); // keyframe time
                            modifier.values.Add("0"); // color slot 1
                            modifier.values.Add("1"); // opacity 1
                            modifier.values.Add("0"); // hue 1
                            modifier.values.Add("0"); // saturation 1
                            modifier.values.Add("0"); // value 1
                            modifier.values.Add("0"); // color slot 2
                            modifier.values.Add("1"); // opacity 2
                            modifier.values.Add("0"); // hue 2
                            modifier.values.Add("0"); // saturation 2
                            modifier.values.Add("0"); // value 2
                            modifier.values.Add("True"); // relative
                            modifier.values.Add("Linear"); // easing
                            break;
                        }
                    case Mode.Hex: {
                            modifier.values.Add("False"); // collapse keyframe
                            modifier.values.Add("0"); // keyframe time
                            modifier.values.Add("0"); // color 1
                            modifier.values.Add("0"); // color 2
                            modifier.values.Add("True"); // relative
                            modifier.values.Add("Linear"); // easing
                            break;
                        }
                }
            });
        }

        #endregion

        #region Sub Classes

        public enum Mode
        {
            Slot,
            Hex,
        }

        #endregion
    }
}

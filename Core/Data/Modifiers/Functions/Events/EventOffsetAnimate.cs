using System.Collections.Generic;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class EventOffsetAnimate : ModifierActionBase
    {
        #region Constructors

        public EventOffsetAnimate() => SetupModifier("1", "0", "0", "1", "0", "False", "0");

        #endregion

        #region Values

        public override string Name => "eventOffsetAnimate";

        public override ModifierCategoryType Category => ModifierCategoryType.Events;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant || !RTLevel.Current.eventEngine || RTLevel.Current.eventEngine.offsets == null)
                return;

            var easing = Parser.TryParse(modifier.GetValue(4, modifierLoop.variables), true, Easing.Linear);

            var list = RTLevel.Current.eventEngine.offsets;

            var eventType = modifier.GetInt(1, 0, modifierLoop.variables);
            var valueIndex = modifier.GetInt(2, 0, modifierLoop.variables);
            var operation = Parser.TryParse(modifier.GetValue(6, modifierLoop.variables), true, MathOperation.Addition);

            if (eventType < list.Count && valueIndex < list[eventType].Count)
            {
                var value = modifier.GetBool(5, false, modifierLoop.variables) ? list[eventType][valueIndex] + modifier.GetFloat(0, 0f, modifierLoop.variables) : modifier.GetFloat(0, 0f, modifierLoop.variables);

                var animation = new RTAnimation("Event Offset Animation");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, list[eventType][valueIndex], Ease.Linear),
                        new FloatKeyframe(modifier.GetFloat(3, 1f, modifierLoop.variables), value, Ease.GetEaseFunction(easing)),
                    },
                    x =>
                    {
                        RTLevel.Current.eventEngine.SetOffset(eventType, valueIndex, x);
                        RTLevel.Current.eventEngine.SetOffsetOperation(eventType, valueIndex, operation);
                    }, interpolateOnComplete: true)
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.DropdownGenerator(modifier, reference, "Event Type", 1, CoreHelper.StringToOptionData(EventLibrary.displayNames), _val =>
            {
                modifier.SetValue(1, _val.ToString());
                modifier.SetValue(2, "0");
                modifierCard.RenderModifier(reference);
                modifierCard.Update(modifier, reference);
            });
            modifierCard.DropdownGenerator(modifier, reference, "Value Index", 2, CoreHelper.StringToOptionData(EventLibrary.valueNames[RTMath.Clamp(modifier.GetInt(1, 0), 0, EventLibrary.valueNames.Length - 1)]));
            modifierCard.SingleGenerator(modifier, reference, "Offset Value", 0, 0f);

            modifierCard.SingleGenerator(modifier, reference, "Time", 3, 1f);
            modifierCard.EaseGenerator(modifier, reference, 4);
            modifierCard.BoolGenerator(modifier, reference, "Relative", 5, false);
            modifierCard.DropdownGenerator(modifier, reference, "Operation", 6, CoreHelper.ToOptionData<MathOperation>());
        }

        #endregion
    }
}

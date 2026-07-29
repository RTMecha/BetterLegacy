using System.Collections.Generic;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetAnimateVariable : ModifierActionBase
    {
        #region Constructors

        public GetAnimateVariable(bool isMath)
        {
            this.isMath = isMath;
            Name = "getAnimateVariable";
            if (isMath)
                Name += "Math";
            SetupModifier("1", "ANIMATE_VAR", "0", "True", "0", "True");
        }

        #endregion

        #region Values

        readonly bool isMath;

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isMath)
            {
                if (modifierLoop.reference is not IEvaluatable evaluatable)
                    return;

                var numberVariables = evaluatable.GetObjectVariables();
                ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                var functions = evaluatable.GetObjectFunctions();
                RTLevel.Current?.evaluationContext?.RegisterVariables(numberVariables);
                RTLevel.Current?.evaluationContext?.RegisterFunctions(functions);
            }

            var time = isMath ? (float)RTMath.Evaluate(modifier.GetValue(0, modifierLoop.variables), RTLevel.Current?.evaluationContext) : modifier.GetFloat(0, 0f, modifierLoop.variables);
            var name = FormatStringVariables(modifier.GetValue(1), modifierLoop.variables);
            var value = isMath ? (float)RTMath.Evaluate(modifier.GetValue(2, modifierLoop.variables), RTLevel.Current?.evaluationContext) : modifier.GetFloat(2, 0f, modifierLoop.variables);
            var relative = modifier.GetBool(3, true, modifierLoop.variables);

            if (string.IsNullOrEmpty(name))
                return;

            var easing = Parser.TryParse(modifier.GetValue(4, modifierLoop.variables), true, Easing.Linear);

            var applyDeltaTime = modifier.GetBool(5, true, modifierLoop.variables);

            var prevValue = modifierLoop.variables.TryGetValue(name, out string v) ? Parser.TryParse(v, 0f) : 0f;
            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    value *= CoreHelper.TimeFrame;

                value += prevValue;
            }

            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Variable");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, prevValue, Ease.Linear),
                        new FloatKeyframe(RTMath.Clamp(time, 0f, 9999f), value, Ease.GetEaseFunction(easing)),
                    }, x => modifierLoop.variables[name] = x.ToString(), interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                animation.onComplete += () => modifier.Result = default;
                AnimationManager.inst.Play(animation);
                modifier.Result = animation;
                return;
            }

            modifierLoop.variables[name] = value.ToString();
        }

        public override void OnRemoveCache(Modifier modifier)
        {
            if (modifier.TryGetResult(out RTAnimation animation))
                animation.Stop();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 1, renderVariables: false);
            if (isMath)
            {
                modifierCard.StringGenerator(modifier, reference, "Time", 0);
                modifierCard.StringGenerator(modifier, reference, "Value", 2);
            }
            else
            {
                modifierCard.SingleGenerator(modifier, reference, "Time", 0, 1f);
                modifierCard.SingleGenerator(modifier, reference, "Value", 2, 0f);
            }
            modifierCard.BoolGenerator(modifier, reference, "Relative", 3, true);
            modifierCard.EaseGenerator(modifier, reference, 4);
            modifierCard.BoolGenerator(modifier, reference, "Apply Delta Time", 5, true);
        }

        #endregion
    }
}

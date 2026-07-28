using System.Collections.Generic;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ApplyAnimation : ModifierActionBase
    {
        #region Constructors

        public ApplyAnimation(Type type, bool isMath)
        {
            this.type = type;
            this.isMath = isMath;
            isTo = type == Type.Both || type == Type.To;
            isFrom = type == Type.Both || type == Type.From;
            Name = "applyAnimation";
            if (type != Type.Both)
                Name += type.ToString();
            if (isMath)
                Name += "Math";
            SetupModifier("Object Group", "True", "True", "True", "0", "0", "0", "False", "1", "1");
            if (type == Type.Both)
                Modifier.values.Add("Object Group");
            if (isMath)
                Modifier.values.Add("audioTime");
            IsGroup = true;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Animation;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly Type type;

        readonly bool isTo;

        readonly bool isFrom;

        readonly bool isMath;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            var cache = modifier.GetResultOrDefault(() =>
            {
                var applyAnimationCache = new Cache();
                if (isFrom)
                    applyAnimationCache.from = GameData.Current.FindObjectWithTag(modifier, beatmapObject, modifier.GetValue(0, modifierLoop.variables));
                if (isTo)
                    applyAnimationCache.to = GameData.Current.FindObjectsWithTag(modifier, beatmapObject, modifier.GetValue(type == Type.Both ? 10 : 0, modifierLoop.variables));
                applyAnimationCache.startTime = modifierLoop.reference.GetParentRuntime()?.CurrentTime ?? 0f;
                return applyAnimationCache;
            });

            if (!cache.from)
                return;

            var from = cache.from;
            var list = cache.to;
            var time = cache.startTime;

            if (isMath)
            {
                var numberVariables = beatmapObject.GetObjectVariables();
                ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);
                var functions = beatmapObject.GetObjectFunctions();
                RTLevel.Current.evaluationContext.RegisterVariables(numberVariables);
                RTLevel.Current.evaluationContext.RegisterFunctions(functions);
            }

            var animatePos = modifier.GetBool(1, true, modifierLoop.variables);
            var animateSca = modifier.GetBool(2, true, modifierLoop.variables);
            var animateRot = modifier.GetBool(3, true, modifierLoop.variables);
            var delayPos = isMath ? RTMath.Evaluate(modifier.GetValue(4, modifierLoop.variables), RTLevel.Current.evaluationContext) : modifier.GetFloat(4, 0f, modifierLoop.variables);
            var delaySca = isMath ? RTMath.Evaluate(modifier.GetValue(5, modifierLoop.variables), RTLevel.Current.evaluationContext) : modifier.GetFloat(5, 0f, modifierLoop.variables);
            var delayRot = isMath ? RTMath.Evaluate(modifier.GetValue(6, modifierLoop.variables), RTLevel.Current.evaluationContext) : modifier.GetFloat(6, 0f, modifierLoop.variables);
            var useVisual = modifier.GetBool(7, false, modifierLoop.variables);
            var length = isMath ? RTMath.Evaluate(modifier.GetValue(8, modifierLoop.variables), RTLevel.Current.evaluationContext) : modifier.GetFloat(8, 1f, modifierLoop.variables);
            var speed = isMath ? RTMath.Evaluate(modifier.GetValue(9, modifierLoop.variables), RTLevel.Current.evaluationContext) : modifier.GetFloat(9, 1f, modifierLoop.variables);
            var timeOffset = isMath ? RTMath.Evaluate(modifier.GetValue(type == Type.Both ? 11 : 10, modifierLoop.variables), RTLevel.Current.evaluationContext) : modifierLoop.reference.GetParentRuntime().CurrentTime;

            if (!modifier.constant)
                AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

            switch (type)
            {
                case Type.Both: {
                        for (int i = 0; i < list.Count; i++)
                        {
                            var bm = list[i];

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                                animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 0f, Ease.Linear),
                                        new FloatKeyframe(RTMath.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                                    }, x => ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                                };
                                animation.onComplete = () =>
                                {
                                    AnimationManager.inst.Remove(animation.id);
                                    animation = null;
                                    modifier.Result = null;
                                };
                                AnimationManager.inst.Play(animation);
                                continue;
                            }

                            ModifiersHelper.ApplyAnimationTo(bm, from, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                        }
                        break;
                    }
                case Type.From: {
                        if (!modifier.constant)
                        {
                            AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

                            var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                            animation.animationHandlers = new List<AnimationHandlerBase>
                                {
                                    new AnimationHandler<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, 0f, Ease.Linear),
                                        new FloatKeyframe(RTMath.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                                    }, x => ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                                };
                            animation.onComplete = () =>
                            {
                                AnimationManager.inst.Remove(animation.id);
                                animation = null;
                                modifier.Result = null;
                            };
                            AnimationManager.inst.Play(animation);
                            return;
                        }

                        ModifiersHelper.ApplyAnimationTo(beatmapObject, from, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                        break;
                    }
                case Type.To: {
                        if (!modifier.constant)
                            AnimationManager.inst.RemoveName("Apply Object Animation " + beatmapObject.id);

                        for (int i = 0; i < list.Count; i++)
                        {
                            var bm = list[i];

                            if (!modifier.constant)
                            {
                                var animation = new RTAnimation("Apply Object Animation " + beatmapObject.id);
                                animation.animationHandlers = new List<AnimationHandlerBase>
                                    {
                                        new AnimationHandler<float>(new List<IKeyframe<float>>
                                        {
                                            new FloatKeyframe(0f, 0f, Ease.Linear),
                                            new FloatKeyframe(RTMath.Clamp(length / speed, 0f, 100f), length, Ease.Linear),
                                        }, x => ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, 0f, x, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot), interpolateOnComplete: true)
                                    };
                                animation.onComplete = () =>
                                {
                                    AnimationManager.inst.Remove(animation.id);
                                    animation = null;
                                    modifier.Result = null;
                                };
                                AnimationManager.inst.Play(animation);
                                continue;
                            }

                            ModifiersHelper.ApplyAnimationTo(bm, beatmapObject, useVisual, time, timeOffset, animatePos, animateSca, animateRot, delayPos, delaySca, delayRot);
                        }
                        break;
                    }
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            if (type != Type.Both)
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);
            else
            {
                modifierCard.GroupFieldGenerator(modifier, reference, "From Group", 0);
                modifierCard.GroupFieldGenerator(modifier, reference, "To Group", 10);
            }

            modifierCard.BoolGenerator(modifier, reference, "Animate Position", 1, true);
            modifierCard.BoolGenerator(modifier, reference, "Animate Scale", 2, true);
            modifierCard.BoolGenerator(modifier, reference, "Animate Rotation", 3, true);
            if (isMath)
            {
                modifierCard.StringGenerator(modifier, reference, "Delay Position", 4);
                modifierCard.StringGenerator(modifier, reference, "Delay Scale", 5);
                modifierCard.StringGenerator(modifier, reference, "Delay Rotation", 6);
            }
            else
            {
                modifierCard.SingleGenerator(modifier, reference, "Delay Position", 4, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Delay Scale", 5, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Delay Rotation", 6, 0f);
            }
            modifierCard.BoolGenerator(modifier, reference, "Use Visual", 7, false);
            if (isMath)
            {
                modifierCard.StringGenerator(modifier, reference, "Length", 8);
                modifierCard.StringGenerator(modifier, reference, "Speed", 9);
                modifierCard.StringGenerator(modifier, reference, "Time", type == Type.Both ? 11 : 10);
            }
            else
            {
                modifierCard.SingleGenerator(modifier, reference, "Length", 8, 1f);
                modifierCard.SingleGenerator(modifier, reference, "Speed", 9, 1f);
            }
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Both,
            From,
            To,
        }

        public class Cache
        {
            public BeatmapObject from;
            public List<BeatmapObject> to = new List<BeatmapObject>();
            public float startTime;
        }

        #endregion
    }
}

using System.Collections.Generic;

using BetterLegacy.Configs;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RunAnimation : ModifierActionBase
    {
        #region Constructors

        public RunAnimation() => SetupModifier(false, "Anim Name", "Object Group", "1", "0", "True", "True", "True", "False", "0");

        #endregion

        #region Values

        public override string Name => "runAnimation";

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var transformable = modifierLoop.reference.AsTransformable();
            if (transformable == null)
                return;
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable == null)
                return;

            var name = modifier.GetValue(0, modifierLoop.variables);
            if (string.IsNullOrEmpty(name))
                return;

            var tag = modifier.GetValue(1, modifierLoop.variables);
            var time = modifier.GetFloat(2, 0f, modifierLoop.variables);

            var easing = Parser.TryParse(modifier.GetValue(3, modifierLoop.variables), true, Easing.Linear);

            var disablePositionSequence = modifier.GetBool(4, true, modifierLoop.variables);
            var disableScaleSequence = modifier.GetBool(5, true, modifierLoop.variables);
            var disableRotationSequence = modifier.GetBool(6, true, modifierLoop.variables);

            var loop = modifier.GetBool(7, false, modifierLoop.variables);
            var endTime = modifier.GetFloat(8, 0f, modifierLoop.variables);

            if (GameData.Current.animationGroups.TryFind(x => x.name == name, out AnimationGroup animationGroup))
            {
                if (!modifier.constant)
                {
                    var animation = new RTAnimation("Animate Object Offset");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, 0f, Ease.Linear),
                            new FloatKeyframe(time == 0f ? animationGroup.AnimLength : RTMath.Clamp(time, 0f, 9999f), endTime == 0f ? animationGroup.AnimLength : RTMath.Clamp(endTime, 0f, 9999f), Ease.GetEaseFunction(easing)),
                        },
                        x =>
                        {
                            doAnimation(modifier, prefabable, tag, x, animationGroup.animations, disablePositionSequence, disableScaleSequence, disableRotationSequence);
                        }, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    if (loop)
                    {
                        animation.onComplete += () =>
                        {
                            animation.ResetTime();
                            for (int i = 0; i < animation.animationHandlers.Count; i++)
                                animation.animationHandlers[i].completed = false;
                            AnimationManager.inst.Play(animation);
                        };
                    }
                    modifier.Result = animation;
                    AnimationManager.inst.Play(animation);
                    return;
                }
                var t = loop ? time % (endTime != 0f ? endTime : animationGroup.AnimLength) : endTime != 0f ? RTMath.Clamp(time, 0f, endTime) : time;
                doAnimation(modifier, prefabable, tag, t, animationGroup.animations, disablePositionSequence, disableScaleSequence, disableRotationSequence);
                return;
            }
            if (!modifier.constant)
            {
                var animation = new RTAnimation("Animate Object Offset");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, 0f, Ease.Linear),
                        new FloatKeyframe(time == 0f ? animationGroup.AnimLength : RTMath.Clamp(time, 0f, 9999f), endTime == 0f ? animationGroup.AnimLength : RTMath.Clamp(endTime, 0f, 9999f), Ease.GetEaseFunction(easing)),
                    },
                    x =>
                    {
                        doAnimation(modifier, prefabable, tag, x, animationGroup.animations, disablePositionSequence, disableScaleSequence, disableRotationSequence);
                    }, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                if (loop)
                {
                    animation.onComplete += () =>
                    {
                        animation.ResetTime();
                        for (int i = 0; i < animation.animationHandlers.Count; i++)
                            animation.animationHandlers[i].completed = false;
                        AnimationManager.inst.Play(animation);
                    };
                }
                modifier.Result = animation;
                AnimationManager.inst.Play(animation);
                return;
            }
            doAnimation(modifier, prefabable, tag, time, GameData.Current.animations, disablePositionSequence, disableScaleSequence, disableRotationSequence);
        }

        static void doAnimation(Modifier modifier, IPrefabable prefabable, string tag, float time, List<PAAnimation> animations,
            bool disablePositionSequence, bool disableScaleSequence, bool disableRotationSequence)
        {
            for (int i = 0; i < animations.Count; i++)
            {
                var animation = animations[i];
                if (EditorConfig.Instance.RunAnimationsSetsCursorTime.Value)
                    animation.timeOffset = RTMath.Clamp(time, 0f, float.MaxValue);
                else if (AnimationEditor.inst && AnimationEditor.inst.Dialog && AnimationEditor.inst.Dialog.IsCurrent && AnimationEditor.inst.CurrentAnimation && AnimationEditor.inst.CurrentAnimation.id == animation.id)
                    time = animation.timeOffset;
                if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, tag, x => x.animID == animation.ReferenceID, out BeatmapObject beatmapObject))
                    continue;
                beatmapObject.disablePositionSequence = disablePositionSequence;
                beatmapObject.disableScaleSequence = disableScaleSequence;
                beatmapObject.disableRotationSequence = disableRotationSequence;
                var fullTransform = animation.InterpolateTransform(time);
                beatmapObject.fullTransform.position = fullTransform.position;
                beatmapObject.fullTransform.scale = fullTransform.scale;
                beatmapObject.fullTransform.rotation = fullTransform.rotation;
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.TryGetResult(out RTAnimation animation))
                AnimationManager.inst.Remove(animation.id);
            modifier.Result = null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.PrefabGroupOnly(modifier, reference);
            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);

            modifierCard.StringGenerator(modifier, reference, "Name", 0);
            modifierCard.SingleGenerator(modifier, reference, "Time", 2);
            modifierCard.SingleGenerator(modifier, reference, "End Time", 8);
            modifierCard.BoolGenerator(modifier, reference, "Loop", 7);

            modifierCard.EaseGenerator(modifier, reference, 3);

            modifierCard.BoolGenerator(modifier, reference, "Override Pos", 4);
            modifierCard.BoolGenerator(modifier, reference, "Override Sca", 5);
            modifierCard.BoolGenerator(modifier, reference, "Override Rot", 6);
        }

        #endregion
    }
}

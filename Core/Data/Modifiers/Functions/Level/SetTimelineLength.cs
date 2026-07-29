using System.Collections.Generic;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetTimelineLength : ModifierActionBase
    {
        #region Constructors

        public SetTimelineLength() => SetupModifier(false, "-1", "0.5", "24");

        #endregion

        #region Values

        public override string Name => "setTimelineLength";

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var length = modifier.GetFloat(0, -1f, modifierLoop.variables);
            var speed = modifier.GetFloat(1, 0.5f, modifierLoop.variables);
            var easing = Parser.TryParse(modifier.GetValue(2, modifierLoop.variables), true, Easing.OutCubic);

            if (speed == 0f || modifier.constant)
            {
                RTGameManager.inst.timelineLength = length;
                return;
            }

            var animation = new RTAnimation("Timeline Length");
            animation.animationHandlers = new List<AnimationHandlerBase>
            {
                new AnimationHandler<float>(new List<IKeyframe<float>>
                {
                    new FloatKeyframe(0f, RTGameManager.inst.timelineLength, Ease.Linear),
                    new FloatKeyframe(speed, length, Ease.GetEaseFunction(easing)),
                }, x => RTGameManager.inst.timelineLength = x, interpolateOnComplete: true),
            };
            animation.SetDefaultOnComplete();
            AnimationManager.inst.Play(animation);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Length", 0, -1f);
            modifierCard.SingleGenerator(modifier, reference, "Speed", 1, 0.5f);
            modifierCard.EaseGenerator(modifier, reference, 2);
        }

        #endregion
    }
}

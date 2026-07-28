using System;
using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class PlayerRotate : PlayerActionBase
    {
        #region Constructors

        public PlayerRotate(bool toObject, Selector selector) : base(selector)
        {
            this.toObject = toObject;
            Name = "playerRotate";
            if (selector != Selector.Nearest)
                Name += selector.ToString();
            if (toObject)
                Name += "ToObject";
            if (!toObject)
                SetupModifier("0", "1", "0", "False");
            else
                SetupModifier();
            if (selector == Selector.Index)
                Modifier.values.Insert(0, "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCompatibility Compatibility => toObject ? ModifierCompatibility.BeatmapObjectCompatible : base.Compatibility;

        readonly bool toObject;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!toObject)
            {
                base.Run(modifier, modifierLoop);
                return;
            }

            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;

            switch (selector)
            {
                case Selector.Nearest: {
                        // queue post tick so the position of the object is accurate.
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var pos = beatmapObject.GetFullPosition();
                            var player = PlayerManager.GetClosestPlayer(pos);
                            RotatePlayer(player, beatmapObject.GetFullRotation(true).z);
                        });
                        break;
                    }
                case Selector.Index: {
                        // queue post tick so the position of the object is accurate.
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var pos = beatmapObject.GetFullPosition();
                            if (PlayerManager.Players.TryGetAt(modifier.GetInt(0, 0, modifierLoop.variables), out PAPlayer player))
                                RotatePlayer(player, beatmapObject.GetFullRotation(true).z);
                        });
                        break;
                    }
                case Selector.All: {
                        // queue post tick so the position of the object is accurate.
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var value = beatmapObject.GetFullRotation(true).z;
                            foreach (var player in PlayerManager.Players)
                                RotatePlayer(player, value);
                        });
                        break;
                    }
            }
        }

        void RotatePlayer(PAPlayer player, float value)
        {
            if (player && player.RuntimePlayer && player.RuntimePlayer.rb)
                player.RuntimePlayer.rb.transform.SetLocalRotationEulerZ(value);
        }

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (toObject)
                return;

            var value = modifier.GetFloat(Index(0), 0f, modifierLoop.variables);
            var duration = modifier.GetFloat(Index(1), 0f, modifierLoop.variables);

            var easing = Parser.TryParse(FormatStringVariables(modifier.GetValue(Index(2), modifierLoop.variables), modifierLoop.variables), true, Easing.Linear);

            var relative = modifier.GetBool(Index(3), false, modifierLoop.variables);

            var tf = player.RuntimePlayer.rb.transform;
            if (modifier.constant)
            {
                var v = tf.localRotation.eulerAngles;
                if (relative)
                    v.z += value;
                else
                    v.z = value;
                tf.localRotation = Quaternion.Euler(v);
            }
            else
            {
                var animation = new RTAnimation("Player Move");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, tf.localRotation.eulerAngles.z, Ease.Linear),
                        new FloatKeyframe(duration, value + (relative ? tf.localRotation.eulerAngles.z : 0f), Ease.GetEaseFunction(easing)),
                    }, tf.SetLocalRotationEulerZ, interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                AnimationManager.inst.Play(animation);
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            if (toObject)
                return;

            modifierCard.SingleGenerator(modifier, reference, "Rotation", Index(0), 0f);
            modifierCard.SingleGenerator(modifier, reference, "Duration", Index(1), 1f);
            modifierCard.EaseGenerator(modifier, reference, Index(2));
            modifierCard.BoolGenerator(modifier, reference, "Relative", Index(3), false);
        }

        #endregion
    }
}

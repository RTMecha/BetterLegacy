using System.Collections.Generic;
using System.Linq;

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
    public class PlayerMove : PlayerActionBase
    {
        #region Constructors

        public PlayerMove(int axis, bool toObject, Selector selector) : base(selector)
        {
            this.toObject = toObject;
            this.axis = axis;
            Name = "playerMove" + (axis == 1 ? "Y" : axis == 0 ? "X" : string.Empty);
            if (selector != Selector.Nearest)
                Name += selector.ToString();
            if (toObject)
                Name += "ToObject";
            if (!toObject)
            {
                SetupModifier("0", "1", "0", "False");
                if (axis == -1)
                    Modifier.values.Insert(0, "0");
            }
            else
                SetupModifier();
            if (selector == Selector.Index)
                Modifier.values.Insert(0, "0");
            if (Name == "playerMove" || Name == "playerMoveAll")
                Modifier.version = 1;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCompatibility Compatibility => toObject ? ModifierCompatibility.BeatmapObjectCompatible : base.Compatibility;

        readonly int axis;

        readonly bool toObject;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if ((modifier.Name == "playerMove" || modifier.Name == "playerMoveAll") && modifier.version == 0)
            {
                var value = modifier.GetValue(0);

                if (value.Contains(','))
                {
                    var axis = value.Split(',');
                    modifier.SetValue(0, axis[0]);
                    modifier.values.RemoveAt(modifier.values.Count - 1);
                    modifier.values.Insert(1, axis[1]);
                }
                modifier.version++;
            }
        }

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
                            var player = PlayerManager.inst.GetClosestPlayer(pos);
                            MovePlayer(player, pos);
                        });
                        break;
                    }
                case Selector.Index: {
                        // queue post tick so the position of the object is accurate.
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var pos = beatmapObject.GetFullPosition();
                            if (PlayerManager.inst.players.TryGetAt(modifier.GetInt(0, 0, modifierLoop.variables), out PAPlayer player))
                                MovePlayer(player, pos);
                        });
                        break;
                    }
                case Selector.All: {
                        // queue post tick so the position of the object is accurate.
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            var pos = beatmapObject.GetFullPosition();
                            foreach (var player in PlayerManager.inst.players)
                                MovePlayer(player, pos);
                        });
                        break;
                    }
            }
        }

        void MovePlayer(PAPlayer player, Vector3 pos)
        {
            if (player && player.RuntimePlayer && player.RuntimePlayer.rb)
                player.RuntimePlayer.rb.position = axis switch
                {
                    0 => new Vector3(pos.x, player.RuntimePlayer.rb.position.y),
                    1 => new Vector3(player.RuntimePlayer.rb.position.x, pos.y),
                    _ => new Vector3(pos.x, pos.y, 0f),
                };
        }

        public override void RunOnPlayer(Modifier modifier, ModifierLoop modifierLoop, PAPlayer player)
        {
            if (toObject)
                return;

            if (axis == -1)
            {
                var vector = new Vector2(modifier.GetFloat(Index(0), 0f, modifierLoop.variables), modifier.GetFloat(Index(1), 0f, modifierLoop.variables));
                var duration = modifier.GetFloat(Index(2), 1f, modifierLoop.variables);
                bool relative = modifier.GetBool(Index(4), false, modifierLoop.variables);
                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
                if (duration == 0f || modifier.constant)
                {
                    if (relative)
                        tf.localPosition += (Vector3)vector;
                    else
                        tf.localPosition = vector;
                }
                else
                {
                    var easing = Parser.TryParse(FormatStringVariables(modifier.GetValue(Index(3), modifierLoop.variables), modifierLoop.variables), true, Easing.Linear);

                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<Vector2>(new List<IKeyframe<Vector2>>
                        {
                            new Vector2Keyframe(0f, tf.localPosition, Ease.Linear),
                            new Vector2Keyframe(duration, new Vector2(vector.x + (relative ? tf.localPosition.x : 0f), vector.y + (relative ? tf.localPosition.y : 0f)), Ease.GetEaseFunction(easing)),
                        }, vector2 => tf.localPosition = vector2, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            }
            else
            {
                var value = modifier.GetFloat(Index(0), 0f, modifierLoop.variables);
                var duration = modifier.GetFloat(Index(1), 1f, modifierLoop.variables);
                bool relative = modifier.GetBool(Index(3), false, modifierLoop.variables);
                if (!player || !player.RuntimePlayer)
                    return;

                var tf = player.RuntimePlayer.rb.transform;
                var currentValue = axis switch
                {
                    0 => tf.localPosition.x,
                    1 => tf.localPosition.y,
                    _ => 0f,
                };
                if (duration == 0f || modifier.constant)
                {
                    if (relative)
                        tf.localPosition = axis switch
                        {
                            0 => new Vector2(tf.localPosition.x + value, tf.localPosition.y),
                            1 => new Vector2(tf.localPosition.x, tf.localPosition.y + value),
                            _ => tf.localPosition,
                        };
                    else
                        tf.localPosition = axis switch
                        {
                            0 => new Vector2(value, tf.localPosition.y),
                            1 => new Vector2(tf.localPosition.x, value),
                            _ => tf.localPosition,
                        };
                }
                else
                {
                    var easing = Parser.TryParse(FormatStringVariables(modifier.GetValue(Index(2), modifierLoop.variables), modifierLoop.variables), true, Easing.Linear);

                    Helpers.CoreHelper.Log($"Running {Name} with value {value}");

                    var animation = new RTAnimation("Player Move");
                    animation.animationHandlers = new List<AnimationHandlerBase>
                    {
                        new AnimationHandler<float>(new List<IKeyframe<float>>
                        {
                            new FloatKeyframe(0f, currentValue, Ease.Linear),
                            new FloatKeyframe(duration, value + (relative ? currentValue : 0f), Ease.GetEaseFunction(easing)),
                        },
                        x =>  tf.localPosition = axis switch
                        {
                            0 => new Vector2(x, tf.localPosition.y),
                            1 => new Vector2(tf.localPosition.x, x),
                            _ => tf.localPosition,
                        }, interpolateOnComplete: true),
                    };
                    animation.SetDefaultOnComplete();
                    AnimationManager.inst.Play(animation);
                }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            base.RenderModifierCard(modifier, modifierCard, reference, modifyable);
            if (toObject)
                return;

            int index = 0;
            switch (axis)
            {
                case 0:
                case 1: {
                        modifierCard.SingleGenerator(modifier, reference, axis == 1 ? "Y" : "X", Index(0), 0f);
                        break;
                    }
                default: {
                        modifierCard.SingleGenerator(modifier, reference, "X", Index(0), 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Y", Index(1), 0f);
                        index++;
                        break;
                    }
            }
            modifierCard.SingleGenerator(modifier, reference, "Duration", Index(1 + index), 1f);
            modifierCard.EaseGenerator(modifier, reference, Index(2 + index));
            modifierCard.BoolGenerator(modifier, reference, "Relative", Index(3 + index), false);
        }

        #endregion
    }
}

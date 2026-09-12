using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class AnimateObject : ModifierActionBase
    {
        #region Constructors

        public AnimateObject(bool isSignal, bool isMath, bool isGroup)
        {
            this.isSignal = isSignal;
            this.isMath = isMath;
            this.isGroup = isGroup;
            Name = "animate" + (isSignal ? "Signal" : "Object");
            if (isMath)
                Name += "Math";
            if (isGroup)
                Name += "Other";
            SetupModifier(false, "1", "0", "0", "0", "0", "True", "0", "True");
            if (isSignal)
            {
                Modifier.values.Insert(7, "Object Group");
                Modifier.values.Insert(8, "0");
                Modifier.values.Add("True");
            }
            if (isGroup)
                Modifier.values.Insert(7, "Object Group");
            IsGroup = isGroup || isSignal;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        readonly bool isSignal;

        readonly bool isMath;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var values = Values.Get(modifier, modifierLoop, this);
            if (!values.accepted)
                return;

            var prefabable = isGroup || isSignal ? modifierLoop.reference.AsPrefabable() : null;
            if (isSignal && !values.signalDeactivate)
                foreach (var bm in GameData.Current.FindObjectsWithTag(modifier, prefabable, values.signalGroup))
                {
                    if (!bm.modifiers.IsEmpty() && !bm.modifiers.FindAll(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger).IsEmpty() &&
                        bm.modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                        m.Result = null;
                }

            if (isGroup)
            {
                if (prefabable == null)
                    return;

                foreach (var transformable in GameData.Current.FindTransformablesWithTag(modifier, prefabable, modifier.GetValue(7, modifierLoop.variables)))
                    Animate(modifier, transformable, prefabable, values.value, values.type, values.relative, values.applyDeltaTime, values.time, values.easing, values.signalGroup, values.signalDelay);
                return;
            }

            Animate(modifier, modifierLoop.reference.AsTransformable(), prefabable, values.value, values.type, values.relative, values.applyDeltaTime, values.time, values.easing, values.signalGroup, values.signalDelay);
        }

        void Animate(Modifier modifier, ITransformable transformable, IPrefabable prefabable, Vector3 setVector, int type, bool relative, bool applyDeltaTime, float time, Easing easing, string signalGroup, float signalDelay)
        {
            if (transformable == null)
                return;

            var vector = transformable.GetTransformOffset(type);

            if (relative)
            {
                if (modifier.constant && applyDeltaTime)
                    setVector *= CoreHelper.TimeFrame;

                setVector += vector;
            }

            if (!modifier.constant && time != 0f)
            {
                var animation = new RTAnimation("Animate Object Offset");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<Vector3>(new List<IKeyframe<Vector3>>
                    {
                        new Vector3Keyframe(0f, vector, Ease.Linear),
                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector, Ease.GetEaseFunction(easing)),
                    }, vector3 => transformable.SetTransform(type, vector3), interpolateOnComplete: true),
                };
                animation.SetDefaultOnComplete();
                if (isSignal)
                    animation.onComplete += () =>
                    {
                        var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, signalGroup);

                        foreach (var bm in list)
                            ModifiersHelper.SignalModifier(bm, signalDelay);
                    };
                AnimationManager.inst.Play(animation);
                return;
            }

            transformable.SetTransform(type, setVector);
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (modifier.constant || !modifier.GetBool(!isGroup ? 9 : 10, true, modifierLoop.variables))
                return;

            if (modifierLoop.reference is not IPrefabable prefabable)
                return;

            int groupIndex = !isGroup ? 7 : 8;
            var modifyables = GameData.Current.FindModifyables(modifier, prefabable, modifier.GetValue(groupIndex, modifierLoop.variables));

            foreach (var modifyable in modifyables)
            {
                if (!modifyable.Modifiers.IsEmpty() && modifyable.Modifiers.TryFind(x => x.Name == "requireSignal" && x.type == Modifier.Type.Trigger, out Modifier m))
                    m.Result = default;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 7);
            }

            if (isMath)
                modifierCard.StringGenerator(modifier, reference, "Time", 0);
            else
                modifierCard.SingleGenerator(modifier, reference, "Time", 0, 1f);

            modifierCard.DropdownGenerator(modifier, reference, "Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));

            if (isMath)
            {
                modifierCard.StringGenerator(modifier, reference, "X", 2);
                modifierCard.StringGenerator(modifier, reference, "Y", 3);
                modifierCard.StringGenerator(modifier, reference, "Z", 4);
            }
            else
            {
                modifierCard.SingleGenerator(modifier, reference, "X", 2, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Y", 3, 0f);
                modifierCard.SingleGenerator(modifier, reference, "Z", 4, 0f);
            }

            modifierCard.BoolGenerator(modifier, reference, "Relative", 5, true);

            modifierCard.EaseGenerator(modifier, reference, 6);

            modifierCard.BoolGenerator(modifier, reference, "Apply Delta Time", (isGroup ? 8 : 7) + (isSignal ? 3 : 0), true);

            if (isSignal)
            {
                modifierCard.GroupFieldGenerator(modifier, reference, "Signal Group", isGroup ? 8 : 7);
                if (isMath)
                    modifierCard.StringGenerator(modifier, reference, "Signal Delay", isGroup ? 9 : 8);
                else
                    modifierCard.SingleGenerator(modifier, reference, "Signal Delay", isGroup ? 9 : 8, 0f);
                modifierCard.BoolGenerator(modifier, reference, "Signal Deactivate", isGroup ? 10 : 9, true);
            }
        }

        #endregion

        #region Sub Classes

        public struct Values
        {
            public bool accepted;
            public float time;
            public int type;
            public Vector3 value;
            public bool relative;
            public Easing easing;
            public bool applyDeltaTime;
            public string signalGroup;
            public float signalDelay;
            public bool signalDeactivate;

            public static Values Get(Modifier modifier, ModifierLoop modifierLoop, AnimateObject animateObject)
            {
                var values = new Values();
                if (animateObject.isMath)
                {
                    if (modifierLoop.reference is not IEvaluatable evaluatable)
                        return values;

                    var numberVariables = evaluatable.GetObjectVariables();
                    ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                    var functions = evaluatable.GetObjectFunctions();
                    var evaluationContext = RTLevel.Current.evaluationContext;
                    evaluationContext.RegisterVariables(numberVariables);
                    evaluationContext.RegisterFunctions(functions);

                    values.time = RTMath.Evaluate(modifier.GetValue(0, modifierLoop.variables), RTLevel.Current?.evaluationContext);
                    values.type = modifier.GetInt(1, 0, modifierLoop.variables);
                    values.value = new Vector3(
                        x: (float)RTMath.Evaluate(modifier.GetValue(2, modifierLoop.variables), RTLevel.Current?.evaluationContext),
                        y: (float)RTMath.Evaluate(modifier.GetValue(3, modifierLoop.variables), RTLevel.Current?.evaluationContext),
                        z: (float)RTMath.Evaluate(modifier.GetValue(4, modifierLoop.variables), RTLevel.Current?.evaluationContext));
                    values.relative = modifier.GetBool(5, true, modifierLoop.variables);
                    values.easing = Parser.TryParse(modifier.GetValue(6, modifierLoop.variables), true, Easing.Linear);
                    values.applyDeltaTime = modifier.GetBool((animateObject.isGroup ? 8 : 7) + (animateObject.isSignal ? 3 : 0), true, modifierLoop.variables);
                    if (animateObject.isSignal)
                    {
                        values.signalGroup = modifier.GetValue(animateObject.isGroup ? 8 : 7, modifierLoop.variables);
                        values.signalDelay = RTMath.Parse(modifier.GetValue(animateObject.isGroup ? 9 : 8, modifierLoop.variables), RTLevel.Current?.evaluationContext, numberVariables, functions);
                        values.signalDeactivate = modifier.GetBool(animateObject.isGroup ? 10 : 9, true, modifierLoop.variables);
                    }
                    values.accepted = true;
                    return values;
                }
                values.time = modifier.GetFloat(0, 0f, modifierLoop.variables);
                values.type = modifier.GetInt(1, 0, modifierLoop.variables);
                values.value = new Vector3(
                    x: modifier.GetFloat(2, 0f, modifierLoop.variables),
                    y: modifier.GetFloat(3, 0f, modifierLoop.variables),
                    z: modifier.GetFloat(4, 0f, modifierLoop.variables));
                values.relative = modifier.GetBool(5, true, modifierLoop.variables);
                values.easing = Parser.TryParse(modifier.GetValue(6, modifierLoop.variables), true, Easing.Linear);
                values.applyDeltaTime = modifier.GetBool((animateObject.isGroup ? 8 : 7) + (animateObject.isSignal ? 3 : 0), true, modifierLoop.variables);
                if (animateObject.isSignal)
                {
                    values.signalGroup = modifier.GetValue(animateObject.isGroup ? 8 : 7, modifierLoop.variables);
                    values.signalDelay = modifier.GetFloat(animateObject.isGroup ? 9 : 8, 0f, modifierLoop.variables);
                    values.signalDeactivate = modifier.GetBool(animateObject.isGroup ? 10 : 9, true, modifierLoop.variables);
                }
                values.accepted = true;
                return values;
            }
        }

        #endregion
    }
}

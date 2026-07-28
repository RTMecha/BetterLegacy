using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ReactiveProperty : ModifierActionBase
    {
        #region Constructors

        public ReactiveProperty(Property property, bool isChain)
        {
            this.property = property;
            this.isChain = isChain;
            Name = "reactive" + property.ToString();
            if (isChain)
                Name += property == Property.Col ? "Lerp" : "Chain";
            Modifier = property switch
            {
                Property.Pos => CreateModifier(Name, "1", "0", "0", "0", "0"),
                Property.Sca => CreateModifier(Name, "1", "0", "0", "1", "1"),
                Property.Rot => CreateModifier(Name, "1", "0"),
                Property.Col => CreateModifier(Name, "1", "0", "0"),
                Property.Iterations => CreateModifier(Name, "100", "0", "0", "True"),
                _ => null,
            };
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Audio;

        public override ModifierCompatibility Compatibility => property == Property.Iterations ? ModifierCompatibility.BackgroundObjectCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly Property property;

        readonly bool isChain;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (property)
            {
                case Property.Pos: {
                        var val = modifier.GetFloat(0, 0f, modifierLoop.variables);
                        var sampleX = modifier.GetInt(1, 0, modifierLoop.variables);
                        var sampleY = modifier.GetInt(2, 0, modifierLoop.variables);
                        var intensityX = modifier.GetFloat(3, 0f, modifierLoop.variables);
                        var intensityY = modifier.GetFloat(4, 0f, modifierLoop.variables);

                        float reactivePositionX = RTLevel.Current.GetSample(sampleX, intensityX * val);
                        float reactivePositionY = RTLevel.Current.GetSample(sampleY, intensityY * val);

                        if (isChain)
                        {
                            if (modifierLoop.reference is IReactive reactive)
                                reactive.ReactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);
                            return;
                        }

                        if (modifierLoop.reference is not BeatmapObject beatmapObject)
                            return;

                        beatmapObject.runtimeObject?.visualObject?.SetOrigin(new Vector3(
                            beatmapObject.origin.x + reactivePositionX,
                            beatmapObject.origin.y + reactivePositionY,
                            beatmapObject.Depth * 0.1f));
                        break;
                    }
                case Property.Sca: {
                        var val = modifier.GetFloat(0, 0f, modifierLoop.variables);
                        var sampleX = modifier.GetInt(1, 0, modifierLoop.variables);
                        var sampleY = modifier.GetInt(2, 0, modifierLoop.variables);
                        var intensityX = modifier.GetFloat(3, 0f, modifierLoop.variables);
                        var intensityY = modifier.GetFloat(4, 0f, modifierLoop.variables);

                        float reactiveScaleX = RTLevel.Current.GetSample(sampleX, intensityX * val);
                        float reactiveScaleY = RTLevel.Current.GetSample(sampleY, intensityY * val);

                        if (isChain)
                        {
                            if (modifierLoop.reference is IReactive reactive)
                                reactive.ReactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY);
                            return;
                        }

                        if (modifierLoop.reference is not BeatmapObject beatmapObject)
                            return;

                        beatmapObject.runtimeObject?.visualObject?.SetScaleOffset(new Vector2(
                            1f + reactiveScaleX,
                            1f + reactiveScaleY));
                        break;
                    }
                case Property.Rot: {
                        if (isChain && modifierLoop.reference is IReactive reactive)
                            reactive.ReactiveRotationOffset = RTLevel.Current.GetSample(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetFloat(0, 0f, modifierLoop.variables));
                        if (!isChain && modifierLoop.reference is BeatmapObject beatmapObject)
                            beatmapObject.runtimeObject?.visualObject?.SetRotationOffset(RTLevel.Current.GetSample(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetFloat(0, 0f, modifierLoop.variables)));
                        break;
                    }
                case Property.Col: {
                        if (modifierLoop.reference is not BeatmapObject beatmapObject)
                            return;

                        var runtimeObject = beatmapObject.runtimeObject;
                        if (runtimeObject && runtimeObject.visualObject && runtimeObject.visualObject.renderer)
                            runtimeObject.visualObject.SetColor(
                                isChain ? RTMath.Lerp(runtimeObject.visualObject.GetPrimaryColor(), ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, modifierLoop.variables)), RTLevel.Current.GetSample(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetFloat(0, 0f, modifierLoop.variables))) :
                                runtimeObject.visualObject.GetPrimaryColor() + ThemeManager.inst.Current.GetObjColor(modifier.GetInt(2, 0, modifierLoop.variables)) * RTLevel.Current.GetSample(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetFloat(0, 0f, modifierLoop.variables)));
                        break;
                    }
                case Property.Iterations: {
                        if (modifierLoop.reference is BackgroundObject backgroundObject)
                            backgroundObject.runtimeObject?.ReactiveDepth(modifier.GetInt(1, 0, modifierLoop.variables), modifier.GetFloat(0, 100f, modifierLoop.variables), modifier.GetInt(2, 0, modifierLoop.variables), modifier.GetBool(3, true, modifierLoop.variables));
                        break;
                    }
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!isChain || property != Property.Iterations)
                return;

            switch (property)
            {
                case Property.Pos: {
                        if (modifierLoop.reference is IReactive reactive)
                            reactive.ReactivePositionOffset = Vector3.zero;
                        break;
                    }
                case Property.Sca: {
                        if (modifierLoop.reference is IReactive reactive)
                            reactive.ReactiveScaleOffset = Vector3.zero;
                        break;
                    }
                case Property.Rot: {
                        if (modifierLoop.reference is IReactive reactive)
                            reactive.ReactiveRotationOffset = 0f;
                        break;
                    }
                case Property.Iterations: {
                        if (modifierLoop.reference is BackgroundObject backgroundObject)
                            backgroundObject.runtimeObject?.SetDepthOffset(0);
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (property)
            {
                case Property.Rot: {
                        modifierCard.SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        modifierCard.IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);
                        break;
                    }
                case Property.Col: {
                        modifierCard.SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        modifierCard.IntegerGenerator(modifier, reference, "Sample", 1, 0);
                        modifierCard.ColorGenerator(modifier, reference, "Color", 2);
                        break;
                    }
                case Property.Iterations: {
                        modifierCard.SingleGenerator(modifier, reference, "Intensity", 0, 1f);
                        modifierCard.IntegerGenerator(modifier, reference, "Sample", 1, 0, max: RTLevel.MAX_SAMPLES);
                        modifierCard.IntegerGenerator(modifier, reference, "Offset", 2);
                        modifierCard.BoolGenerator(modifier, reference, "Inverse", 3);
                        break;
                    }
                default: {
                        modifierCard.SingleGenerator(modifier, reference, "Total Intensity", 0, 1f);
                        modifierCard.IntegerGenerator(modifier, reference, "Sample X", 1, 0, max: RTLevel.MAX_SAMPLES);
                        modifierCard.IntegerGenerator(modifier, reference, "Sample Y", 2, 0, max: RTLevel.MAX_SAMPLES);
                        modifierCard.SingleGenerator(modifier, reference, "Intensity X", 3, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Intensity Y", 4, 0f);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            Pos,
            Sca,
            Rot,
            Col,
            Iterations,
        }

        #endregion
    }
}

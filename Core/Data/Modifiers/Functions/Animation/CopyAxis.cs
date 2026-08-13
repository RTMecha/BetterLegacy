using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using ILMath;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class CopyAxis : ModifierActionBase
    {
        #region Constructors

        public CopyAxis(Type type)
        {
            this.type = type;
            Name = "copyAxis";
            if (type != Type.Normal)
                Name += type.ToString();
            Modifier = type switch
            {
                Type.Normal => CreateModifier(Name, 2, new string[] { "Object Group", "0", "0", "0", "0", "0", "1", "0", "-99999", "99999", "99999", "0", "True" }),
                Type.Math => CreateModifier(Name, 2, new string[] { "Object Group", "0", "0", "0", "0", "0", "-99999", "99999", "(axis - 0) * 1 % 9999", "0", "True" }),
                Type.Group => CreateModifier(Name, 2, new string[] { "var + 0", "0", "0", "var", "Object Group", "0", "0", "0", "-99999", "99999", "0" }),
                Type.Chain => CreateModifier(Name, 1, new string[] { "1", "0", "0", "0", "0", "0", "1", "0", "-99999", "99999", "99999", "0", "True", "0.1", "True", "", "False" }),
                _ => null,
            };
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Animation;

        public override ModifierCompatibility Compatibility => base.Compatibility;

        readonly Type type;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.version != 0)
                return;

            switch (type)
            {
                case Type.Normal: {
                        if (modifier.version == 0)
                        {
                            if (modifier.GetInt(1, 0) == 2 && modifier.GetInt(2, 0) == 0 && !modifier.GetBool(11, false))
                                modifier.SetValue(2, "2");
                            modifier.version++;
                        }
                        if (modifier.version == 1)
                        {
                            var axisSourceRaw = modifier.GetValue(11);
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                                modifier.SetValue(11, "0");
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                                modifier.SetValue(11, "1");
                            modifier.version++;
                        }
                        break;
                    }
                case Type.Math: {
                        if (modifier.version == 0)
                        {
                            if (modifier.GetInt(1, 0) == 2 && modifier.GetInt(2, 0) == 0 && !modifier.GetBool(9, false))
                                modifier.SetValue(2, "2");
                            modifier.version++;
                        }
                        if (modifier.version == 1)
                        {
                            var axisSourceRaw = modifier.GetValue(9);
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                                modifier.SetValue(9, "0");
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                                modifier.SetValue(9, "1");
                            modifier.version++;
                        }
                        break;
                    }
                case Type.Group: {
                        if (modifier.version == 0)
                        {
                            if (modifier.GetInt(1, 0) == 2 && modifier.GetInt(2, 0) == 0 && !modifier.GetBool(9, false))
                                modifier.SetValue(2, "2");
                            modifier.version++;
                        }
                        if (modifier.version == 1)
                        {
                            int a = 0;
                            for (int i = 3; i < modifier.values.Count; i += 8)
                            {
                                var axisSourceRaw = modifier.GetValue(i + 7);
                                if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                                    modifier.SetValue(i + 7, "0");
                                if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                                    modifier.SetValue(i + 7, "1");
                                a++;
                            }
                            modifier.version++;
                        }
                        break;
                    }
                case Type.Chain: {
                        if (modifier.version == 0)
                        {
                            var axisSourceRaw = modifier.GetValue(11);
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                                modifier.SetValue(11, "0");
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                                modifier.SetValue(11, "1");
                            modifier.version++;
                        }
                        break;
                    }
            }
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (type)
            {
                case Type.Normal: {
                        var transformable = modifierLoop.reference.AsTransformable();
                        if (transformable == null)
                            return;
                        var prefabable = modifierLoop.reference.AsPrefabable();
                        if (prefabable == null)
                            return;

                        var tag = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);

                        var fromType = modifier.GetInt(1, 0, modifierLoop.variables);
                        var fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);
                        var toType = modifier.GetInt(3, 0, modifierLoop.variables);
                        var toAxis = modifier.GetInt(4, 0, modifierLoop.variables);
                        var delay = modifier.GetFloat(5, 0f, modifierLoop.variables);
                        var multiply = modifier.GetFloat(6, 0f, modifierLoop.variables);
                        var offset = modifier.GetFloat(7, 0f, modifierLoop.variables);
                        var min = modifier.GetFloat(8, -9999f, modifierLoop.variables);
                        var max = modifier.GetFloat(9, 9999f, modifierLoop.variables);
                        var loop = modifier.GetFloat(10, 9999f, modifierLoop.variables);
                        var axisSourceRaw = modifier.GetValue(11, modifierLoop.variables);
                        if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                            axisSourceRaw = "1";
                        if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                            axisSourceRaw = "0";
                        var axisSource = Parser.TryParse(axisSourceRaw, true, AxisSource.Sequence);
                        var offsetAudio = modifier.GetBool(12, true, modifierLoop.variables);

                        var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
                        if (cache.tag != tag)
                        {
                            cache.UpdateCache(modifier, prefabable, tag);
                            modifier.Result = cache;
                        }

                        var bm = cache.obj;
                        if (!bm)
                            return;

                        var t = !offsetAudio ? delay : ModifiersHelper.GetTime(bm) - bm.StartTime - delay;

                        fromType = RTMath.Clamp(fromType, 0, bm.events.Count);

                        if (toType < 0 || toType > 3)
                            return;

                        transformable.SetTransform(toType, toAxis, ModifiersHelper.GetAnimation(bm, fromType, fromAxis, min, max, offset, multiply, t, loop, axisSource, modifier.version));
                        break;
                    }
                case Type.Math: {
                        var transformable = modifierLoop.reference.AsTransformable();
                        if (transformable == null)
                            return;
                        var prefabable = modifierLoop.reference.AsPrefabable();
                        if (prefabable == null)
                            return;

                        if (modifierLoop.reference is not IEvaluatable evaluatable)
                            return;

                        try
                        {
                            var tag = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);

                            var fromType = modifier.GetInt(1, 0, modifierLoop.variables);
                            var fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);
                            var toType = modifier.GetInt(3, 0, modifierLoop.variables);
                            var toAxis = modifier.GetInt(4, 0, modifierLoop.variables);
                            var delay = modifier.GetFloat(5, 0f, modifierLoop.variables);
                            var min = modifier.GetFloat(6, -9999f, modifierLoop.variables);
                            var max = modifier.GetFloat(7, 9999f, modifierLoop.variables);
                            var evaluation = FormatStringVariables(modifier.GetValue(8, modifierLoop.variables), modifierLoop.variables);
                            var axisSourceRaw = modifier.GetValue(9, modifierLoop.variables);
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                                axisSourceRaw = "1";
                            if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                                axisSourceRaw = "0";
                            var axisSource = Parser.TryParse(axisSourceRaw, true, AxisSource.Sequence);
                            var offsetAudio = modifier.GetBool(10, true, modifierLoop.variables);

                            var cache = modifier.GetResultOrDefault(() => GroupBeatmapObjectCache.Get(modifier, prefabable, tag));
                            if (cache.tag != tag)
                            {
                                cache.UpdateCache(modifier, prefabable, tag);
                                modifier.Result = cache;
                            }

                            var bm = cache.obj;
                            if (!bm)
                                return;

                            var t = !offsetAudio ? delay : ModifiersHelper.GetTime(bm) - bm.StartTime - delay;

                            fromType = RTMath.Clamp(fromType, 0, bm.events.Count);

                            if (toType < 0 || toType > 3)
                                return;

                            var numberVariables = evaluatable.GetObjectVariables();
                            ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);

                            numberVariables["axis"] = ModifiersHelper.GetAnimation(bm, fromType, fromAxis, t, axisSource);
                            bm.SetOtherObjectVariables(numberVariables);
                            transformable.SetTransform(toType, toAxis, RTMath.Clamp(RTMath.Parse(evaluation, RTLevel.Current?.evaluationContext, numberVariables), min, max));
                        }
                        catch
                        {

                        } // try catch for cases where the math is broken
                        break;
                    }
                case Type.Group: {
                        var transformable = modifierLoop.reference.AsTransformable();
                        if (transformable == null)
                            return;
                        var prefabable = modifierLoop.reference.AsPrefabable();
                        if (prefabable == null)
                            return;

                        if (modifierLoop.reference is not IEvaluatable evaluatable)
                            return;

                        var evaluation = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);

                        var toType = modifier.GetInt(1, 0, modifierLoop.variables);
                        var toAxis = modifier.GetInt(2, 0, modifierLoop.variables);

                        if (toType < 0 || toType > 4)
                            return;

                        try
                        {
                            var beatmapObjects = GameData.Current.beatmapObjects;
                            var prefabObjects = GameData.Current.prefabObjects;

                            var time = modifierLoop.reference.GetParentRuntime().CurrentTime;
                            var numberVariables = evaluatable.GetObjectVariables();
                            ModifiersHelper.SetVariables(modifierLoop.variables, numberVariables);
                            RTLevel.Current.evaluationContext.RegisterVariables(numberVariables);

                            var cache = modifier.GetResultOrDefault(() =>
                            {
                                var cache = new CopyAxisGroupCache();
                                cache.input = evaluation;
                                cache.evaluator = MathEvaluation.CompileExpression("ResultFunction", evaluation);

                                for (int i = 3; i < modifier.values.Count; i += 8)
                                {
                                    var group = FormatStringVariables(modifier.GetValue(i + 1, modifierLoop.variables), modifierLoop.variables);

                                    if (GameData.Current.TryFindObjectWithTag(modifier, prefabable, group, out BeatmapObject beatmapObject))
                                        cache.objs.Add(beatmapObject);
                                }

                                return cache;
                            });
                            if (cache.input != evaluation)
                            {
                                cache.input = evaluation;
                                cache.evaluator = MathEvaluation.CompileExpression("ResultFunction", evaluation);
                            }

                            int groupIndex = 0;
                            for (int i = 3; i < modifier.values.Count; i += 8)
                            {
                                var name = FormatStringVariables(modifier.GetValue(i, modifierLoop.variables), modifierLoop.variables);
                                var fromType = modifier.GetInt(i + 2, 0, modifierLoop.variables);
                                var fromAxis = modifier.GetInt(i + 3, 0, modifierLoop.variables);
                                var delay = modifier.GetFloat(i + 4, 0f, modifierLoop.variables);
                                var min = modifier.GetFloat(i + 5, 0f, modifierLoop.variables);
                                var max = modifier.GetFloat(i + 6, 0f, modifierLoop.variables);
                                var axisSourceRaw = modifier.GetValue(i + 7, modifierLoop.variables);
                                if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                                    axisSourceRaw = "1";
                                if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                                    axisSourceRaw = "0";
                                var axisSource = Parser.TryParse(axisSourceRaw, true, AxisSource.Sequence);

                                var beatmapObject = cache.objs.GetAtOrDefault(groupIndex, null);

                                if (!beatmapObject)
                                {
                                    groupIndex++;
                                    continue;
                                }

                                RTLevel.Current.evaluationContext.RegisterVariable(name, ModifiersHelper.GetAnimation(beatmapObject, fromType, fromAxis, time - beatmapObject.StartTime - delay, axisSource, modifier.version));

                                groupIndex++;
                            }

                            transformable.SetTransform(toType, toAxis, (float)cache.evaluator.Invoke(RTLevel.Current.evaluationContext));
                        }
                        catch (Exception ex)
                        {
                            CoreHelper.LogError($"{modifierLoop.reference} had an error. Exception: {ex}");
                        }
                        break;
                    }
                case Type.Chain: {
                        if (modifierLoop.reference is not BeatmapObject beatmapObject)
                            return;

                        var parentCount = modifier.GetInt(0, 1, modifierLoop.variables);

                        var fromType = modifier.GetInt(1, 0, modifierLoop.variables);
                        var fromAxis = modifier.GetInt(2, 0, modifierLoop.variables);
                        var toType = modifier.GetInt(3, 0, modifierLoop.variables);
                        var toAxis = modifier.GetInt(4, 0, modifierLoop.variables);
                        var delay = modifier.GetFloat(5, 0f, modifierLoop.variables);
                        var multiply = modifier.GetFloat(6, 0f, modifierLoop.variables);
                        var offset = modifier.GetFloat(7, 0f, modifierLoop.variables);
                        var min = modifier.GetFloat(8, -9999f, modifierLoop.variables);
                        var max = modifier.GetFloat(9, 9999f, modifierLoop.variables);
                        var loop = modifier.GetFloat(10, 9999f, modifierLoop.variables);
                        var axisSourceRaw = modifier.GetValue(11, modifierLoop.variables);
                        if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "true")
                            axisSourceRaw = "1";
                        if (!string.IsNullOrEmpty(axisSourceRaw) && axisSourceRaw.ToLower() == "false")
                            axisSourceRaw = "0";
                        var axisSource = Parser.TryParse(axisSourceRaw, true, AxisSource.Sequence);
                        var offsetAudio = modifier.GetBool(12, true, modifierLoop.variables);
                        var delayOffset = modifier.GetFloat(13, 0.1f, modifierLoop.variables);
                        var reverseChain = modifier.GetBool(14, true, modifierLoop.variables);
                        var requiredTag = FormatStringVariables(modifier.GetValue(15, modifierLoop.variables), modifierLoop.variables);
                        var searchChildren = modifier.GetBool(16, false, modifierLoop.variables);

                        var cache = modifier.GetResultOrDefault(() => new ChainCache(beatmapObject, parentCount, reverseChain, requiredTag, searchChildren));
                        if (cache.parentCount != parentCount || cache.reverseChain != reverseChain || cache.requiredTag != requiredTag || cache.searchChildren != searchChildren)
                            cache.Init(beatmapObject, parentCount, reverseChain, requiredTag, searchChildren);
                        for (int i = 0; i < parentCount; i++)
                        {
                            var currentParentCache = cache.parents.GetAtOrDefault(i, null);
                            if (!currentParentCache)
                                continue;

                            var currentParent = currentParentCache.beatmapObject;

                            if (i == 0)
                            {
                                delay += delayOffset;
                                if (cache.tickCount <= 0)
                                    continue;

                                for (int j = 0; j < cache.parents.Count; j++)
                                {
                                    var parentCache = cache.parents[j];
                                    // account for other modifiers that have affected transform offsets
                                    parentCache.currentValue = -(parentCache.GetTransformOffset(fromType, fromAxis) - parentCache.beatmapObject.GetTransformOffset(fromType, fromAxis));
                                }
                                continue;
                            }

                            var currentValue = 0f;
                            for (int j = 0; j < RTMath.Clamp(cache.parents.Count, 0, i + 1); j++)
                            {
                                var parentCache = cache.parents[j];
                                var parent = parentCache.beatmapObject;
                                var t = !offsetAudio ? delay : ModifiersHelper.GetTime(parent) - parent.StartTime - delay;

                                fromType = RTMath.Clamp(fromType, 0, parent.events.Count);

                                if (toType < 0 || toType > 3)
                                    continue;

                                currentValue += ModifiersHelper.GetAnimation(parent, fromType, fromAxis, min, max, offset, multiply, t, loop, axisSource, 1);
                                if (axisSource == AxisSource.Offset || axisSource == AxisSource.SequenceOffset)
                                {
                                    currentValue -= parent.GetTransformCache(fromType).At(fromAxis);
                                    currentValue += parentCache.currentValue;
                                }
                                //currentValue -= parent.GetTransformCache(fromType).At(fromAxis); // remove previous copy axis chain application
                            }
                            currentParent.SetTransformCache(toType, toAxis, currentValue);
                            currentParent.SetTransform(toType, toAxis, currentValue);
                            currentParentCache.positionOffset = currentParent.PositionOffset;
                            currentParentCache.scaleOffset = currentParent.ScaleOffset;
                            currentParentCache.rotationOffset = currentParent.RotationOffset;
                            delay += delayOffset;
                        }
                        cache.tickCount++;
                        break;
                    }
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop) => modifier.Result = default;

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (type)
            {
                case Type.Normal: {
                        modifierCard.PrefabGroupOnly(modifier, reference);
                        modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);

                        modifierCard.DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        modifierCard.DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        modifierCard.DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        modifierCard.DropdownGenerator(modifier, reference, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        modifierCard.SingleGenerator(modifier, reference, "Delay", 5, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Multiply", 6, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Offset", 7, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Min", 8, -99999f);
                        modifierCard.SingleGenerator(modifier, reference, "Max", 9, 99999f);

                        modifierCard.SingleGenerator(modifier, reference, "Loop", 10, 99999f);
                        modifierCard.DropdownGenerator(modifier, reference, "Axis Source", 11, CoreHelper.ToOptionData<AxisSource>());
                        modifierCard.BoolGenerator(modifier, reference, "Offset Audio", 12, true);
                        break;
                    }
                case Type.Math: {
                        modifierCard.PrefabGroupOnly(modifier, reference);
                        modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 0);

                        modifierCard.DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        modifierCard.DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        modifierCard.DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        modifierCard.DropdownGenerator(modifier, reference, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        modifierCard.SingleGenerator(modifier, reference, "Delay", 5, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Min", 6, -99999f);
                        modifierCard.SingleGenerator(modifier, reference, "Max", 7, 99999f);
                        modifierCard.DropdownGenerator(modifier, reference, "Axis Source", 9, CoreHelper.ToOptionData<AxisSource>());
                        modifierCard.StringGenerator(modifier, reference, "Expression", 8);
                        modifierCard.BoolGenerator(modifier, reference, "Offset Audio", 10, true);
                        break;
                    }
                case Type.Group: {
                        modifierCard.PrefabGroupOnly(modifier, reference);
                        modifierCard.StringGenerator(modifier, reference, "Expression", 0);

                        modifierCard.DropdownGenerator(modifier, reference, "To Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        modifierCard.DropdownGenerator(modifier, reference, "To Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        int a = 0;
                        for (int i = 3; i < modifier.values.Count; i += 8)
                        {
                            int groupIndex = i;
                            var label = modifierCard.LabelGenerator($"- Group {a + 1}");

                            modifierCard.DeleteGenerator(modifier, reference, label.transform, () =>
                            {
                                for (int j = 0; j < 8; j++)
                                    modifier.values.RemoveAt(groupIndex);
                            });

                            var groupName = modifierCard.StringGenerator(modifier, reference, "Name", i).transform.Find("Input").GetComponent<InputField>();
                            EditorContextMenu.AddContextMenu(groupName.gameObject, EditorContextMenu.GetNameFunctions(groupName));
                            modifierCard.StringGenerator(modifier, reference, "Object Group", i + 1).transform.Find("Input").GetComponent<InputField>();
                            modifierCard.DropdownGenerator(modifier, reference, "From Type", i + 2, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                            modifierCard.DropdownGenerator(modifier, reference, "From Axis", i + 3, CoreHelper.StringToOptionData("X", "Y", "Z"));
                            modifierCard.SingleGenerator(modifier, reference, "Delay", i + 4, 0f);
                            modifierCard.SingleGenerator(modifier, reference, "Min", i + 5, -9999f);
                            modifierCard.SingleGenerator(modifier, reference, "Max", i + 6, 9999f);
                            modifierCard.DropdownGenerator(modifier, reference, "Axis Source", i + 7, CoreHelper.ToOptionData<AxisSource>());
                            a++;
                        }

                        modifierCard.AddGenerator(modifier, reference, "Add Group", () =>
                        {
                            var lastIndex = modifier.values.Count - 1;

                            modifier.values.Add($"var_{a}");
                            modifier.values.Add("Object Group");
                            modifier.values.Add("0");
                            modifier.values.Add("0");
                            modifier.values.Add("0");
                            modifier.values.Add("-9999");
                            modifier.values.Add("9999");
                            modifier.values.Add("0");
                        });
                        break;
                    }
                case Type.Chain: {
                        modifierCard.IntegerGenerator(modifier, reference, "Parent Count", 0);
                        modifierCard.DropdownGenerator(modifier, reference, "From Type", 1, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        modifierCard.DropdownGenerator(modifier, reference, "From Axis", 2, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        modifierCard.DropdownGenerator(modifier, reference, "To Type", 3, CoreHelper.StringToOptionData("Position", "Scale", "Rotation"));
                        modifierCard.DropdownGenerator(modifier, reference, "To Axis (3D)", 4, CoreHelper.StringToOptionData("X", "Y", "Z"));

                        modifierCard.SingleGenerator(modifier, reference, "Delay", 5, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Multiply", 6, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Offset", 7, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Min", 8, -99999f);
                        modifierCard.SingleGenerator(modifier, reference, "Max", 9, 99999f);

                        modifierCard.SingleGenerator(modifier, reference, "Loop", 10, 99999f);
                        modifierCard.DropdownGenerator(modifier, reference, "Axis Source", 11, CoreHelper.ToOptionData<AxisSource>());
                        modifierCard.BoolGenerator(modifier, reference, "Offset Audio", 12, true);
                        modifierCard.SingleGenerator(modifier, reference, "Delay Offset Per Parent", 13, 0.1f);
                        modifierCard.BoolGenerator(modifier, reference, "Reverse Parent Chain", 14, true);

                        modifierCard.GroupFieldGenerator(modifier, reference, "Required Tag", 15);
                        modifierCard.BoolGenerator(modifier, reference, "Search Child Tree", 16);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Normal,
            Math,
            Group,
            Chain,
        }

        public class CopyAxisGroupCache : MathCache
        {
            public List<BeatmapObject> objs = new List<BeatmapObject>();
        }

        public class ChainCache
        {
            #region Constructors

            public ChainCache(BeatmapObject beatmapObject, int parentCount, bool reverseChain, string requiredTag, bool searchChildren) => Init(beatmapObject, parentCount, reverseChain, requiredTag, searchChildren);

            #endregion

            #region Values

            public int parentCount;
            public bool reverseChain;
            public string requiredTag;
            public bool searchChildren;
            public List<ParentCache> parents;
            public long tickCount;

            #endregion

            #region Functions

            public void Init(BeatmapObject beatmapObject, int parentCount, bool reverseChain, string requiredTag, bool searchChildren)
            {
                this.parentCount = parentCount;
                this.reverseChain = reverseChain;
                this.requiredTag = requiredTag;
                this.searchChildren = searchChildren;

                var self = beatmapObject;
                parents = new List<ParentCache>();
                parents.Add(new ParentCache(self));
                if (searchChildren)
                    parents.AddRange(GetChildTree(self, requiredTag, parentCount));
                else
                {
                    for (int i = 0; i < parentCount; i++)
                    {
                        var parent = self.GetParent();
                        if (!parent)
                            break;
                        if (!string.IsNullOrEmpty(requiredTag) && !parent.Tags.Contains(requiredTag))
                        {
                            self = parent;
                            continue;
                        }

                        parents.Add(new ParentCache(parent));
                        self = parent;
                    }
                }
                if (reverseChain)
                    parents.Reverse();
            }

            static List<ParentCache> GetChildTree(BeatmapObject self, string requiredTag, int parentCount, int subIndex = 0)
            {
                var list = new List<ParentCache>();
                if (subIndex >= parentCount)
                    return list;
                var children = self.GetChildren();
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!string.IsNullOrEmpty(requiredTag) && !child.Tags.Contains(requiredTag))
                        continue;
                    list.Add(new ParentCache(child));
                    var sub = GetChildTree(child, requiredTag, parentCount, subIndex + 1);
                    if (!sub.IsEmpty())
                        list.AddRange(sub);
                }
                return list;
            }

            #endregion

            #region Sub Classes

            public class ParentCache : Exists
            {
                public ParentCache(BeatmapObject beatmapObject)
                {
                    this.beatmapObject = beatmapObject;
                    positionOffset = beatmapObject.PositionOffset;
                    scaleOffset = beatmapObject.ScaleOffset;
                    rotationOffset = beatmapObject.RotationOffset;
                }

                public BeatmapObject beatmapObject;

                public Vector3 positionOffset;
                public Vector3 scaleOffset;
                public Vector3 rotationOffset;
                public float currentValue;

                public float GetTransformOffset(int fromType, int fromAxis) => fromType switch
                {
                    0 => positionOffset.At(fromAxis),
                    1 => scaleOffset.At(fromAxis),
                    2 => rotationOffset.At(fromAxis),
                    _ => 0f,
                };
            }

            #endregion
        }

        #endregion
    }
}

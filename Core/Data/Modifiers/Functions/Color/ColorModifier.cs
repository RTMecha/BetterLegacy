using System.Collections.Generic;

using UnityEngine;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ColorModifier : ModifierActionBase
    {
        #region Constructors

        public ColorModifier(MixType mixType, bool isGroup, bool isPlayerDistance)
        {
            this.mixType = mixType;
            this.isGroup = isGroup;
            this.isPlayerDistance = isPlayerDistance;
            Name = mixType.ToString().ToLower() + "Color";
            if (isPlayerDistance)
                Name += "PlayerDistance";
            if (isGroup)
                Name += "Other";
            SetupModifier("1", "0", "0", "0", "0", "4");
            if (mixType == MixType.Lerp && !isPlayerDistance)
                Modifier.values.Insert(5, "1");
            if (isGroup)
                Modifier.values.Insert(1, "Object Group");
            if (isPlayerDistance)
                Modifier.values.Insert(2, "1");
            IsGroup = isGroup;
        }

        // Value Map:
        // [addColor]
        // 0: multiply
        // 1: index
        // 2: hue
        // 3: sat
        // 4: val
        // 5: color source

        // [addColorOther]
        // 0: multiply
        // 1: tag
        // 2: index
        // 3: hue
        // 4: sat
        // 5: val
        // 6: color source

        // [lerpColor]
        // 0: multiply
        // 1: index
        // 2: hue
        // 3: sat
        // 4: val
        // 5: opacity
        // 6: color source

        // [lerpColorOther]
        // 0: multiply
        // 1: tag
        // 2: index
        // 3: hue
        // 4: sat
        // 5: val
        // 6: opacity
        // 7: color source

        // [addColorPlayerDistance]
        // 0: multiply
        // 1: index
        // 2: offset
        // 3: hue
        // 4: sat
        // 5: val
        // 6: color source

        // [lerpColorPlayerDistance]
        // 0: multiply
        // 1: index
        // 2: offset
        // 3: opacity
        // 4: hue
        // 5: sat
        // 6: val
        // 7: color source

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly MixType mixType;
        readonly bool isGroup;
        readonly bool isPlayerDistance;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var values = Values.Get(modifier, modifierLoop);

            if (isGroup)
            {
                var list = modifier.GetResultOrDefault(() =>
                {
                    var prefabable = modifierLoop.reference.AsPrefabable();
                    if (prefabable == null)
                        return new List<BeatmapObject>();

                    return GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1));
                });
                if (list.IsEmpty())
                    return;

                // queue post tick so the color overrides the sequence color
                RTLevel.Current.postTick.Enqueue(() =>
                {
                    var color = values.colorSource switch
                    {
                        ThemeSource.Background => ThemeManager.inst.bgColorToLerp,
                        ThemeSource.GUI => ThemeManager.inst.timelineColorToLerp,
                        ThemeSource.PlayerTail => ThemeManager.inst.tailColorToLerp,
                        _ => ThemeManager.inst.Current.GetColor(values.colorSource, values.index),
                    };
                    color = RTColors.ChangeColorHSV(color, values.hue, values.sat, values.val);
                    color.a *= values.opacity;
                    foreach (var bm in list)
                    {
                        if (!bm.runtimeObject || !bm.runtimeObject.visualObject)
                            continue;
                        bm.runtimeObject.visualObject.SetColor(mixType switch
                        {
                            MixType.Add => bm.runtimeObject.visualObject.GetPrimaryColor() + color * values.value,
                            MixType.Lerp => RTMath.Lerp(bm.runtimeObject.visualObject.GetPrimaryColor(), color, values.value),
                            _ => bm.runtimeObject.visualObject.GetPrimaryColor(),
                        });
                    }
                });
                return;
            }

            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return;
            var runtimeObject = beatmapObject.runtimeObject;
            if (!runtimeObject || !runtimeObject.visualObject)
                return;

            // queue post tick so the color overrides the sequence color
            RTLevel.Current.postTick.Enqueue(() =>
            {
                var color = values.colorSource switch
                {
                    ThemeSource.Background => ThemeManager.inst.bgColorToLerp,
                    ThemeSource.GUI => ThemeManager.inst.timelineColorToLerp,
                    ThemeSource.PlayerTail => ThemeManager.inst.tailColorToLerp,
                    _ => ThemeManager.inst.Current.GetColor(values.colorSource, values.index),
                };
                color = RTColors.ChangeColorHSV(color, values.hue, values.sat, values.val);
                color.a *= values.opacity;
                if (isPlayerDistance)
                {
                    var player = PlayerManager.GetClosestPlayer(runtimeObject.visualObject.gameObject.transform.position);
                    if (!player.RuntimePlayer || !player.RuntimePlayer.rb)
                        return;

                    var distance = Vector2.Distance(player.RuntimePlayer.rb.transform.position, runtimeObject.visualObject.gameObject.transform.position);
                    runtimeObject.visualObject.SetColor(mixType switch
                    {
                        MixType.Add => runtimeObject.visualObject.GetPrimaryColor() + color * -(distance * values.value - values.offset),
                        MixType.Lerp => Color.Lerp(runtimeObject.visualObject.GetPrimaryColor(), color, -(distance * values.value - values.offset)),
                        _ => runtimeObject.visualObject.GetPrimaryColor(),
                    });
                    return;
                }
                runtimeObject.visualObject.SetColor(mixType switch
                {
                    MixType.Add => runtimeObject.visualObject.GetPrimaryColor() + color * values.value,
                    MixType.Lerp => RTMath.Lerp(runtimeObject.visualObject.GetPrimaryColor(), color, values.value),
                    _ => runtimeObject.visualObject.GetPrimaryColor(),
                });
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (Name)
            {
                case "addColor": {
                        modifierCard.DropdownGenerator(modifier, reference, "Color Source", 5, CoreHelper.ToOptionData<ThemeSource>(), _val =>
                        {
                            modifier.SetValue(5, _val.ToString());
                            modifierCard.RenderModifier(reference, modifyable);
                        });
                        modifierCard.ColorGenerator(modifier, reference, "Color", 1, (ThemeSource)modifier.GetInt(5, 4));

                        modifierCard.SingleGenerator(modifier, reference, "Hue", 2, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation", 3, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value", 4, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Add Amount", 0, 1f);
                        break;
                    }
                case "addColorOther": {
                        modifierCard.PrefabGroupOnly(modifier, reference);
                        modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);

                        modifierCard.DropdownGenerator(modifier, reference, "Color Source", 6, CoreHelper.ToOptionData<ThemeSource>(), _val =>
                        {
                            modifier.SetValue(6, _val.ToString());
                            modifierCard.RenderModifier(reference, modifyable);
                        });
                        modifierCard.ColorGenerator(modifier, reference, "Color", 2, (ThemeSource)modifier.GetInt(6, 4));

                        modifierCard.SingleGenerator(modifier, reference, "Hue", 3, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation", 4, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value", 5, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Multiply", 0, 1f);
                        break;
                    }
                case "lerpColor": {
                        modifierCard.DropdownGenerator(modifier, reference, "Color Source", 6, CoreHelper.ToOptionData<ThemeSource>(), _val =>
                        {
                            modifier.SetValue(6, _val.ToString());
                            modifierCard.RenderModifier(reference, modifyable);
                        });
                        modifierCard.ColorGenerator(modifier, reference, "Color", 1, (ThemeSource)modifier.GetInt(6, 4));

                        modifierCard.SingleGenerator(modifier, reference, "Opacity", 5, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Hue", 2, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation", 3, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value", 4, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Interpolate", 0, 1f);
                        break;
                    }
                case "lerpColorOther": {
                        modifierCard.PrefabGroupOnly(modifier, reference);
                        modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 1);

                        modifierCard.DropdownGenerator(modifier, reference, "Color Source", 7, CoreHelper.ToOptionData<ThemeSource>(), _val =>
                        {
                            modifier.SetValue(7, _val.ToString());
                            modifierCard.RenderModifier(reference, modifyable);
                        });
                        modifierCard.ColorGenerator(modifier, reference, "Color", 2, (ThemeSource)modifier.GetInt(7, 4));

                        modifierCard.SingleGenerator(modifier, reference, "Opacity", 6, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Hue", 3, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation", 4, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value", 5, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Interpolate", 0, 1f);
                        break;
                    }
                case "addColorPlayerDistance:": {
                        modifierCard.DropdownGenerator(modifier, reference, "Color Source", 6, CoreHelper.ToOptionData<ThemeSource>(), _val =>
                        {
                            modifier.SetValue(6, _val.ToString());
                            modifierCard.RenderModifier(reference, modifyable);
                        });
                        modifierCard.ColorGenerator(modifier, reference, "Color", 1);

                        modifierCard.SingleGenerator(modifier, reference, "Hue", 4, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation", 5, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value", 6, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Interpolate", 0, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Offset", 2, 10f);
                        break;
                    }
                case "lerpColorPlayerDistance:": {
                        modifierCard.DropdownGenerator(modifier, reference, "Color Source", 7, CoreHelper.ToOptionData<ThemeSource>(), _val =>
                        {
                            modifier.SetValue(7, _val.ToString());
                            modifierCard.RenderModifier(reference, modifyable);
                        });
                        modifierCard.ColorGenerator(modifier, reference, "Color", 1);

                        modifierCard.SingleGenerator(modifier, reference, "Opacity", 3, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Hue", 4, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Saturation", 5, 0f);
                        modifierCard.SingleGenerator(modifier, reference, "Value", 6, 0f);

                        modifierCard.SingleGenerator(modifier, reference, "Interpolate", 0, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Offset", 2, 10f);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum MixType
        {
            Add,
            Lerp,
        }

        public struct Values
        {
            public string tag;
            public float value;
            public float offset;
            public int index;
            public float hue;
            public float sat;
            public float val;
            public float opacity;
            public ThemeSource colorSource;

            public static Values Get(Modifier modifier, ModifierLoop modifierLoop) => modifier.Name switch
            {
                "addColor" => new Values
                {
                    value = modifier.GetFloat(0, 1f, modifierLoop.variables),
                    index = modifier.GetInt(1, 0, modifierLoop.variables),
                    hue = modifier.GetFloat(2, 0f, modifierLoop.variables),
                    sat = modifier.GetFloat(3, 0f, modifierLoop.variables),
                    val = modifier.GetFloat(4, 0f, modifierLoop.variables),
                    colorSource = (ThemeSource)modifier.GetInt(5, 4, modifierLoop.variables),
                },
                "addColorOther" => new Values
                {
                    value = modifier.GetFloat(0, 1f, modifierLoop.variables),
                    tag = modifier.GetValue(1, modifierLoop.variables),
                    index = modifier.GetInt(2, 0, modifierLoop.variables),
                    hue = modifier.GetFloat(3, 0f, modifierLoop.variables),
                    sat = modifier.GetFloat(4, 0f, modifierLoop.variables),
                    val = modifier.GetFloat(5, 0f, modifierLoop.variables),
                    colorSource = (ThemeSource)modifier.GetInt(6, 4, modifierLoop.variables),
                },
                "lerpColor" => new Values
                {
                    value = modifier.GetFloat(0, 1f, modifierLoop.variables),
                    index = modifier.GetInt(1, 0, modifierLoop.variables),
                    hue = modifier.GetFloat(2, 0f, modifierLoop.variables),
                    sat = modifier.GetFloat(3, 0f, modifierLoop.variables),
                    val = modifier.GetFloat(4, 0f, modifierLoop.variables),
                    opacity = modifier.GetFloat(5, 0f, modifierLoop.variables),
                    colorSource = (ThemeSource)modifier.GetInt(6, 4, modifierLoop.variables),
                },
                "lerpColorOther" => new Values
                {
                    value = modifier.GetFloat(0, 1f, modifierLoop.variables),
                    tag = modifier.GetValue(1, modifierLoop.variables),
                    index = modifier.GetInt(2, 0, modifierLoop.variables),
                    hue = modifier.GetFloat(3, 0f, modifierLoop.variables),
                    sat = modifier.GetFloat(4, 0f, modifierLoop.variables),
                    val = modifier.GetFloat(5, 0f, modifierLoop.variables),
                    opacity = modifier.GetFloat(6, 0f, modifierLoop.variables),
                    colorSource = (ThemeSource)modifier.GetInt(7, 4, modifierLoop.variables),
                },
                "addColorPlayerDistance" => new Values
                {
                    value = modifier.GetFloat(0, 1f, modifierLoop.variables),
                    index = modifier.GetInt(1, 0, modifierLoop.variables),
                    offset = modifier.GetFloat(2, 10f, modifierLoop.variables),
                    hue = modifier.GetFloat(3, 0f, modifierLoop.variables),
                    sat = modifier.GetFloat(4, 0f, modifierLoop.variables),
                    val = modifier.GetFloat(5, 0f, modifierLoop.variables),
                    colorSource = (ThemeSource)modifier.GetInt(6, 4, modifierLoop.variables),
                },
                "lerpColorPlayerDistance" => new Values
                {
                    value = modifier.GetFloat(0, 1f, modifierLoop.variables),
                    index = modifier.GetInt(1, 0, modifierLoop.variables),
                    offset = modifier.GetFloat(2, 10f, modifierLoop.variables),
                    opacity = modifier.GetFloat(3, 0f, modifierLoop.variables),
                    hue = modifier.GetFloat(4, 0f, modifierLoop.variables),
                    sat = modifier.GetFloat(5, 0f, modifierLoop.variables),
                    val = modifier.GetFloat(6, 0f, modifierLoop.variables),
                    colorSource = (ThemeSource)modifier.GetInt(7, 4, modifierLoop.variables),
                },
                _ => default,
            };
        }

        #endregion
    }
}

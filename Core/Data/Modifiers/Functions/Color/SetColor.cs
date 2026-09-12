using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetColor : ModifierActionBase
    {
        #region Constructors

        public SetColor(Mode mode, bool isGroup)
        {
            this.mode = mode;
            this.isGroup = isGroup;
            Name = "setColor" + mode.ToString();
            if (isGroup)
                Name += "Other";
            switch (mode)
            {
                case Mode.Hex: {
                        SetupModifier("FFFFFFFF", "FFFFFFFF");
                        break;
                    }
                case Mode.RGBA: {
                        SetupModifier("1", "1", "1", "1", "1", "1", "1", "1");
                        break;
                    }
            }
            if (isGroup)
            {
                Modifier.values.Add("Object Group");
                Modifier.version = 1;
            }
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly Mode mode;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void ValidateModifier(Modifier modifier, IModifyable modifyable)
        {
            if (modifier.version != 0)
                return;

            if (modifier.Name == "setColorHexOther" && modifier.version == 0)
            {
                if (modifier.values.Count > 2)
                    modifier.values.Move(2, 1);
                modifier.version++;
            }
        }

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (mode)
            {
                case Mode.Hex: {
                        var color1 = modifier.GetValue(0, modifierLoop.variables);
                        var color2 = modifier.GetValue(1, modifierLoop.variables);

                        if (!isGroup)
                        {
                            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                                return;

                            var runtimeObject = beatmapObject.runtimeObject;
                            if (!runtimeObject)
                                return;

                            // queue post tick so the color overrides the sequence color
                            RTLevel.Current.postTick.Enqueue(() =>
                            {
                                if (!runtimeObject.visualObject.isGradient)
                                {
                                    var color = runtimeObject.visualObject.GetPrimaryColor();
                                    runtimeObject.visualObject.SetColor(string.IsNullOrEmpty(color1) ? color : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), color.a));
                                }
                                else if (runtimeObject.visualObject is SolidObject solidObject)
                                {
                                    var colors = solidObject.GetColors();
                                    solidObject.SetColor(
                                        string.IsNullOrEmpty(color1) ? colors.startColor : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), colors.startColor.a),
                                        string.IsNullOrEmpty(color2) ? colors.endColor : color2.Length == 8 ? RTColors.HexToColor(color2) : RTColors.FadeColor(RTColors.HexToColor(color2), colors.endColor.a));
                                }
                            });
                            return;
                        }

                        if (modifierLoop.reference is not IPrefabable prefabable)
                            return;

                        var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(2, modifierLoop.variables)));
                        if (list.IsEmpty())
                            return;

                        // queue post tick so the color overrides the sequence color
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            try
                            {
                                foreach (var bm in list)
                                {
                                    var runtimeObject = bm.runtimeObject;
                                    if (!runtimeObject)
                                        continue;

                                    if (!runtimeObject.visualObject.isGradient)
                                    {
                                        var color = runtimeObject.visualObject.GetPrimaryColor();
                                        runtimeObject.visualObject.SetColor(string.IsNullOrEmpty(color1) ? color : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), color.a));
                                    }
                                    else if (runtimeObject.visualObject is SolidObject solidObject)
                                    {
                                        var colors = solidObject.GetColors();
                                        solidObject.SetColor(
                                            string.IsNullOrEmpty(color1) ? colors.startColor : color1.Length == 8 ? RTColors.HexToColor(color1) : RTColors.FadeColor(RTColors.HexToColor(color1), colors.startColor.a),
                                            string.IsNullOrEmpty(color2) ? colors.endColor : color2.Length == 8 ? RTColors.HexToColor(color2) : RTColors.FadeColor(RTColors.HexToColor(color2), colors.endColor.a));
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Helpers.CoreHelper.LogError($"{Name} failed due to the exception: {ex}");
                            }
                        });
                        break;
                    }
                case Mode.RGBA: {
                        var color1 = new Color(modifier.GetFloat(0, 1f, modifierLoop.variables), modifier.GetFloat(1, 1f, modifierLoop.variables), modifier.GetFloat(2, 1f, modifierLoop.variables), modifier.GetFloat(3, 1f, modifierLoop.variables));
                        var color2 = new Color(modifier.GetFloat(4, 1f, modifierLoop.variables), modifier.GetFloat(5, 1f, modifierLoop.variables), modifier.GetFloat(6, 1f, modifierLoop.variables), modifier.GetFloat(7, 1f, modifierLoop.variables));

                        if (!isGroup)
                        {
                            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                                return;

                            var runtimeObject = beatmapObject.runtimeObject;
                            if (!runtimeObject)
                                return;

                            // queue post tick so the color overrides the sequence color
                            RTLevel.Current.postTick.Enqueue(() =>
                            {
                                if (!runtimeObject.visualObject.isGradient)
                                    runtimeObject.visualObject.SetColor(color1);
                                else if (runtimeObject.visualObject is SolidObject solidObject)
                                    solidObject.SetColor(color1, color2);
                            });
                            return;
                        }

                        if (modifierLoop.reference is not IPrefabable prefabable)
                            return;

                        var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(8, modifierLoop.variables));
                        if (list.IsEmpty())
                            return;

                        // queue post tick so the color overrides the sequence color
                        RTLevel.Current.postTick.Enqueue(() =>
                        {
                            foreach (var bm in list)
                            {
                                var runtimeObject = bm.runtimeObject;
                                if (!runtimeObject)
                                    continue;

                                if (!runtimeObject.visualObject.isGradient)
                                    runtimeObject.visualObject.SetColor(color1);
                                else if (runtimeObject.visualObject is SolidObject solidObject)
                                    solidObject.SetColor(color1, color2);
                            }
                        });
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (mode)
            {
                case Mode.Hex: {
                        if (isGroup)
                        {
                            modifierCard.PrefabGroupOnly(modifier, reference);
                            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 2);
                        }

                        var primaryHexCode = modifierCard.StringGenerator(modifier, reference, "Primary Hex Code", 0);
                        EditorContextMenu.AddContextMenu(primaryHexCode,
                            EditorContextMenu.GetEditorColorFunctions(primaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(0)));

                        var secondaryHexCode = modifierCard.StringGenerator(modifier, reference, "Secondary Hex Code", 1);
                        EditorContextMenu.AddContextMenu(secondaryHexCode,
                            EditorContextMenu.GetEditorColorFunctions(secondaryHexCode.transform.Find("Input").GetComponent<InputField>(), () => modifier.GetValue(1)));
                        break;
                    }
                case Mode.RGBA: {
                        if (isGroup)
                        {
                            modifierCard.PrefabGroupOnly(modifier, reference);
                            modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 8);
                        }

                        modifierCard.SingleGenerator(modifier, reference, "Red 1", 0, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Green 1", 1, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Blue 1", 2, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Opacity 1", 3, 1f);

                        modifierCard.SingleGenerator(modifier, reference, "Red 2", 4, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Green 2", 5, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Blue 2", 6, 1f);
                        modifierCard.SingleGenerator(modifier, reference, "Opacity 2", 7, 1f);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum Mode
        {
            Hex,
            RGBA,
        }

        #endregion
    }
}

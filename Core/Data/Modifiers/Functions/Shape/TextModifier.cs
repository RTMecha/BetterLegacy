using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class TextModifier : ModifierActionBase
    {
        #region Constructors

        public TextModifier(Operation operation, bool isGroup)
        {
            this.operation = operation;
            this.isGroup = isGroup;
            Name = operation.ToString().ToLower() + "Text";
            if (isGroup)
                Name += "Other";
            SetupModifier(operation == Operation.Remove || operation == Operation.RemoveAt ? "1" : "Text");
            if (operation == Operation.Replace)
                Modifier.values.Add("Replace");
            if (isGroup)
                Modifier.values.Add("Object Group");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly Operation operation;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(operation == Operation.Replace ? 2 : 1, modifierLoop.variables));
                if (list.IsEmpty())
                    return;

                foreach (var bm in list)
                {
                    if (bm.ShapeType != ShapeType.Text || !bm.runtimeObject || bm.runtimeObject.visualObject is not TextObject textObject)
                        continue;

                    if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                        textObject.SetText(operation switch
                        {
                            Operation.Add => textObject.textMeshPro.text + FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                            Operation.Remove => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - RTMath.Clamp(modifier.GetInt(0, 1, modifierLoop.variables), 0, textObject.textMeshPro.text.Length)),
                            Operation.RemoveAt => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.TryRemove(modifier.GetInt(0, 1, modifierLoop.variables), 1),
                            Operation.Replace => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Replace(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)),
                            _ => FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                        });
                    else
                        textObject.text = operation switch
                        {
                            Operation.Add => textObject.textMeshPro.text + FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                            Operation.Remove => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - RTMath.Clamp(modifier.GetInt(0, 1, modifierLoop.variables), 0, textObject.textMeshPro.text.Length)),
                            Operation.RemoveAt => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.TryRemove(modifier.GetInt(0, 1, modifierLoop.variables), 1),
                            Operation.Replace => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Replace(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)),
                            _ => FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                        };
                }
            }
            else
            {
                if (modifierLoop.reference is not BeatmapObject beatmapObject || beatmapObject.ShapeType != ShapeType.Text || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not TextObject textObject)
                    return;

                if (modifier.constant || !CoreConfig.Instance.AllowCustomTextFormatting.Value)
                    textObject.SetText(operation switch
                    {
                        Operation.Add => textObject.textMeshPro.text + FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                        Operation.Remove => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - RTMath.Clamp(modifier.GetInt(0, 1, modifierLoop.variables), 0, textObject.textMeshPro.text.Length)),
                        Operation.RemoveAt => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.TryRemove(modifier.GetInt(0, 1, modifierLoop.variables), 1),
                        Operation.Replace => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Replace(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)),
                        _ => FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                    });
                else
                    textObject.text = operation switch
                    {
                        Operation.Add => textObject.textMeshPro.text + FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                        Operation.Remove => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Substring(0, textObject.textMeshPro.text.Length - RTMath.Clamp(modifier.GetInt(0, 1, modifierLoop.variables), 0, textObject.textMeshPro.text.Length)),
                        Operation.RemoveAt => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.TryRemove(modifier.GetInt(0, 1, modifierLoop.variables), 1),
                        Operation.Replace => string.IsNullOrEmpty(textObject.textMeshPro.text) ? string.Empty : textObject.textMeshPro.text.Replace(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)),
                        _ => FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables),
                    };
            }
        }

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (operation != Operation.Set)
                return;

            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = GameData.Current.FindObjectsWithTag(modifier, prefabable, modifier.GetValue(1, modifierLoop.variables));

                if (modifier.constant && !list.IsEmpty())
                    foreach (var bm in list)
                        if (bm.ShapeType == ShapeType.Text && bm.runtimeObject && bm.runtimeObject.visualObject &&
                            bm.runtimeObject.visualObject is TextObject otherTextObject)
                            otherTextObject.text = bm.text;
                return;
            }

            if (modifier.constant && modifierLoop.reference is BeatmapObject beatmapObject && beatmapObject.ShapeType == ShapeType.Text && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject &&
                beatmapObject.runtimeObject.visualObject is TextObject textObject)
                textObject.text = beatmapObject.text;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", operation == Operation.Replace ? 2 : 1);
            }
            if (operation == Operation.Remove || operation == Operation.RemoveAt)
            {
                modifierCard.IntegerGenerator(modifier, reference, operation == Operation.Remove ? "Remove Amount" : "Remove At", 0, 0);
            }
            else
            {
                modifierCard.StringGenerator(modifier, reference, "Text", 0);
                if (operation == Operation.Replace)
                    modifierCard.StringGenerator(modifier, reference, "Replace", 1);
            }
        }

        #endregion

        #region Sub Classes

        public enum Operation
        {
            Set,
            Add,
            Remove,
            RemoveAt,
            Replace,
        }

        #endregion
    }
}

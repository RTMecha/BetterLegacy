using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetOutline : ModifierActionBase
    {
        #region Constructors

        public SetOutline(bool isHex, bool isGroup)
        {
            this.isHex = isHex;
            this.isGroup = isGroup;
            Name = "setOutline";
            if (isHex)
                Name += "Hex";
            if (isGroup)
                Name += "Other";
            SetupModifier("True", "0", "0.1");
            if (!isHex)
            {
                Modifier.values.Add("0"); // color slot
                Modifier.values.Add("1"); // opacity
                Modifier.values.Add("0"); // hue
                Modifier.values.Add("0"); // saturation
                Modifier.values.Add("0"); // value
            }
            else
                Modifier.values.Add(RTColors.WHITE_HEX_CODE);
            if (isGroup)
                Modifier.values.Insert(0, "Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Rendering;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        readonly bool isHex;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables)));
                if (list.IsEmpty())
                    return;

                var enabled = modifier.GetBool(1, true, modifierLoop.variables);
                var type = modifier.GetInt(2, 0, modifierLoop.variables);
                var width = modifier.GetFloat(3, 0.5f, modifierLoop.variables);
                var color = isHex ? RTColors.HexToColor(FormatStringVariables(modifier.GetValue(4, modifierLoop.variables), modifierLoop.variables)) : RTColors.FadeColor(RTColors.ChangeColorHSV(
                    CoreHelper.CurrentBeatmapTheme.GetObjColor(modifier.GetInt(4, 0, modifierLoop.variables)),
                    modifier.GetFloat(6, 0f, modifierLoop.variables),
                    modifier.GetFloat(7, 0f, modifierLoop.variables),
                    modifier.GetFloat(8, 0f, modifierLoop.variables)),
                    modifier.GetFloat(5, 0f, modifierLoop.variables));

                foreach (var beatmapObject in list)
                {
                    if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                        continue;

                    if (enabled)
                    {
                        solidObject.AddOutline(type);
                        solidObject.SetOutline(color, width);
                    }
                    else
                        solidObject.RemoveOutline();
                }
            }
            else
            {
                if (modifierLoop.reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                    return;

                var enabled = modifier.GetBool(0, true, modifierLoop.variables);
                var type = modifier.GetInt(1, 0, modifierLoop.variables);
                var width = modifier.GetFloat(2, 0.5f, modifierLoop.variables);
                var color = isHex ? RTColors.HexToColor(FormatStringVariables(modifier.GetValue(3, modifierLoop.variables), modifierLoop.variables)) : RTColors.FadeColor(RTColors.ChangeColorHSV(
                    CoreHelper.CurrentBeatmapTheme.GetObjColor(modifier.GetInt(3, 0, modifierLoop.variables)),
                    modifier.GetFloat(5, 0f, modifierLoop.variables),
                    modifier.GetFloat(6, 0f, modifierLoop.variables),
                    modifier.GetFloat(7, 0f, modifierLoop.variables)),
                    modifier.GetFloat(4, 0f, modifierLoop.variables));

                if (enabled)
                {
                    solidObject.AddOutline(type);
                    solidObject.SetOutline(color, width);
                }
                else
                    solidObject.RemoveOutline();
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            int index = 0;
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                var groupField = modifierCard.StringGenerator(modifier, reference, "Object Group", index).transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(groupField.gameObject, EditorContextMenu.GetNameFunctions(groupField));
                index++;
            }

            modifierCard.BoolGenerator(modifier, reference, "Enabled", 0 + index);
            modifierCard.DropdownGenerator(modifier, reference, "Type", 1 + index, CoreHelper.StringToOptionData("Behind Object", "Behind All"));
            modifierCard.SingleGenerator(modifier, reference, "Width", 2 + index, 0.1f);

            modifierCard.ColorGenerator(modifier, reference, "Color", 3 + index);
            modifierCard.SingleGenerator(modifier, reference, "Opacity", 4 + index, 0.5f);
            modifierCard.SingleGenerator(modifier, reference, "Hue", 5 + index, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Saturation", 6 + index, 0f);
            modifierCard.SingleGenerator(modifier, reference, "Value", 7 + index, 0f);
        }

        #endregion
    }
}

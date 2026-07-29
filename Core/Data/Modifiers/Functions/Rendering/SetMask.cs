using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetMask : ModifierActionBase
    {
        #region Constructors

        public SetMask(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "setMask";
            if (isGroup)
                Name += "Other";
            SetupModifier("8", "0", "0", "0", "0", "255", "255");
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

                var comparison = Parser.TryParse(modifier.GetValue(1, modifierLoop.variables), true, UnityEngine.Rendering.CompareFunction.Always);
                var pass = Parser.TryParse(modifier.GetValue(2, modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep);
                var fail = Parser.TryParse(modifier.GetValue(3, modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep);
                var zFail = Parser.TryParse(modifier.GetValue(4, modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep);
                var id = (byte)modifier.GetInt(5, 0, modifierLoop.variables);
                var writeMask = (byte)modifier.GetInt(6, 255, modifierLoop.variables);
                var readMask = (byte)modifier.GetInt(7, 255, modifierLoop.variables);

                foreach (var beatmapObject in list)
                {
                    if (!beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject || !solidObject.gameObject)
                        continue;

                    solidObject.SetStencil(comparison, pass, fail, zFail, id, writeMask, readMask);
                }
            }
            else
            {
                if (modifierLoop.reference is not BeatmapObject beatmapObject || !beatmapObject.runtimeObject || beatmapObject.runtimeObject.visualObject is not SolidObject solidObject)
                    return;
                solidObject.SetStencil(
                    Parser.TryParse(modifier.GetValue(0, modifierLoop.variables), true, UnityEngine.Rendering.CompareFunction.Always),
                    Parser.TryParse(modifier.GetValue(1, modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep),
                    Parser.TryParse(modifier.GetValue(2, modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep),
                    Parser.TryParse(modifier.GetValue(3, modifierLoop.variables), true, UnityEngine.Rendering.StencilOp.Keep),
                    (byte)modifier.GetInt(4, 0, modifierLoop.variables),
                    (byte)modifier.GetInt(5, 255, modifierLoop.variables),
                    (byte)modifier.GetInt(6, 255, modifierLoop.variables));
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

            modifierCard.DropdownGenerator(modifier, reference, "Comparison", index, CoreHelper.ToOptionData<UnityEngine.Rendering.CompareFunction>());
            index++;
            modifierCard.DropdownGenerator(modifier, reference, "Pass", index, CoreHelper.ToOptionData<UnityEngine.Rendering.StencilOp>());
            index++;
            modifierCard.DropdownGenerator(modifier, reference, "Fail", index, CoreHelper.ToOptionData<UnityEngine.Rendering.StencilOp>());
            index++;
            modifierCard.DropdownGenerator(modifier, reference, "ZFail", index, CoreHelper.ToOptionData<UnityEngine.Rendering.StencilOp>());
            index++;

            modifierCard.IntegerGenerator(modifier, reference, "ID", index, max: 255);
            index++;
            modifierCard.IntegerGenerator(modifier, reference, "Write Mask", index, max: 255);
            index++;
            modifierCard.IntegerGenerator(modifier, reference, "Read Mask", index, max: 255);
        }

        #endregion
    }
}

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class Blur : ModifierActionBase
    {
        #region Constructors

        public Blur(Type type, bool isGroup)
        {
            this.type = type;
            this.isGroup = isGroup;
            Name = "blur";
            if (type != Type.None)
                Name += type.ToString();
            if (isGroup)
                Name += "Other";
            SetupModifier("0.5", isGroup ? "Object Group" : "False", "False");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Rendering;

        public override ModifierCompatibility Compatibility => isGroup ? ModifierCompatibility.FullBeatmapCompatible : ModifierCompatibility.BeatmapObjectCompatible;

        readonly Type type;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return;

                var list = modifier.GetResultOrDefault(() => GameData.Current.FindObjectsWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)));
                if (list.IsEmpty())
                    return;

                var amount = modifier.GetFloat(0, 0f, modifierLoop.variables);

                foreach (var other in list)
                {
                    var runtimeObject = other.runtimeObject;
                    if (other.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || runtimeObject.visualObject is not SolidObject solidObject || !runtimeObject.visualObject.renderer)
                        continue;

                    var renderer = runtimeObject.visualObject.renderer;

                    var mat = GetMaterial();
                    if (renderer.material != mat)
                        solidObject.SetMaterial(mat);
                    renderer.material.SetFloat("_blurSizeXY", (type == Type.Variable ? other.IntVariable : -(other.Interpolate(3, 1) - 1f)) * amount);
                }
                return;
            }
            if (modifierLoop.reference is BeatmapObject beatmapObject)
            {
                if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty)
                    return;

                var runtimeObject = beatmapObject.runtimeObject;

                if (!runtimeObject || runtimeObject.visualObject is not SolidObject solidObject || !runtimeObject.visualObject.renderer)
                    return;

                var amount = modifier.GetFloat(0, 0f, modifierLoop.variables);
                var renderer = runtimeObject.visualObject.renderer;

                if (!modifier.HasResult())
                {
                    DestroyModifierResult.Init(runtimeObject.visualObject.gameObject, modifier);
                    modifier.Result = runtimeObject.visualObject.gameObject;
                    solidObject.SetMaterial(GetMaterial());
                }

                if (type != Type.Variable && modifier.GetBool(1, false, modifierLoop.variables))
                    renderer.material.SetFloat("_blurSizeXY", -(beatmapObject.Interpolate(3, 1) - 1f) * amount);
                else if (type == Type.Variable)
                    renderer.material.SetFloat("_blurSizeXY", beatmapObject.IntVariable * amount);
                else
                    renderer.material.SetFloat("_blurSizeXY", amount);
            }
        }

        Material GetMaterial() => type == Type.Colored ? LegacyResources.blurColoredMaterial : LegacyResources.blurMaterial;

        public override void Inactive(Modifier modifier, ModifierLoop modifierLoop)
        {
            // if not set back to normal when deactivated
            if (!modifier.GetBool(2, false, modifierLoop.variables))
                return;

            if (modifier.TryGetResult(out List<BeatmapObject> list))
            {
                foreach (var other in list)
                {
                    var runtimeObject = other.runtimeObject;
                    if (other.objectType == BeatmapObject.ObjectType.Empty || !runtimeObject || runtimeObject.visualObject is not SolidObject solidObject || !runtimeObject.visualObject.renderer)
                        continue;

                    solidObject.UpdateRendering(solidObject.gradientType, solidObject.renderType, solidObject.doubleSided, solidObject.gradientScale, solidObject.gradientRotation, solidObject.colorBlendMode);
                }
                return;
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.SingleGenerator(modifier, reference, "Amount", 0, 0.5f);
            if (type != Type.Variable && !isGroup)
                modifierCard.BoolGenerator(modifier, reference, "Use Opacity", 1, false);
            if (isGroup)
            {
                var groupField = modifierCard.StringGenerator(modifier, reference, "Object Group", 1).transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(groupField.gameObject, EditorContextMenu.GetNameFunctions(groupField));
            }
            modifierCard.BoolGenerator(modifier, reference, "Set Back to Normal", type == Type.Variable && !isGroup ? 1 : 2, false);
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            None,
            Variable,
            Colored,
        }

        #endregion
    }
}

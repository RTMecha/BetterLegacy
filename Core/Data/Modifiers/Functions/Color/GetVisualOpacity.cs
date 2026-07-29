using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetVisualOpacity : ModifierActionBase
    {
        #region Constructors

        public GetVisualOpacity(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "getVisualOpacity";
            if (isGroup)
                Name += "Other";
            SetupModifier("VISUALOPACITY1_VAR", "VISUALOPACITY2_VAR");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Color;

        public override Sprite Icon => EditorSprites.DownArrow;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            BeatmapObject beatmapObject = null;
            if (isGroup && !GameData.Current.TryFindObjectWithTag(modifier, modifierLoop.reference as IPrefabable, modifier.GetValue(2), out beatmapObject))
                return;
            if (!isGroup)
                beatmapObject = modifierLoop.reference as BeatmapObject;

            if (!beatmapObject)
                return;

            var colors = beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is SolidObject solidObject ? solidObject.GetColors() : beatmapObject.GetColors();
            var startOpacityName = FormatStringVariables(modifier.GetValue(0), modifierLoop.variables);
            var endOpacityName = FormatStringVariables(modifier.GetValue(1), modifierLoop.variables);
            if (!string.IsNullOrEmpty(startOpacityName))
                modifierLoop.variables[startOpacityName] = colors.startColor.a.ToString();
            if (!string.IsNullOrEmpty(endOpacityName))
                modifierLoop.variables[endOpacityName] = colors.endColor.a.ToString();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                var groupField = modifierCard.StringGenerator(modifier, reference, "Object Group", 2).transform.Find("Input").GetComponent<InputField>();
                EditorContextMenu.AddContextMenu(groupField.gameObject, EditorContextMenu.GetNameFunctions(groupField));
            }

            modifierCard.StringGenerator(modifier, reference, "Opacity 1 Var Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Opacity 2 Var Name", 1, renderVariables: false);
        }

        #endregion
    }
}

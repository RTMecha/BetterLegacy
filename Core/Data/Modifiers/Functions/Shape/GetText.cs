using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Runtime.Objects.Visual;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetText : ModifierVariableBase
    {
        #region Constructors

        public GetText(bool isGroup)
        {
            this.isGroup = isGroup;
            Name = "getText";
            if (isGroup)
                Name += "Other";
            SetupModifier("TEXT_VAR", "False");
            if (isGroup)
                Modifier.values.Add("Object Group");
            IsGroup = isGroup;
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Shape;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.BeatmapObjectCompatible;

        readonly bool isGroup;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var useVisual = modifier.GetBool(1, false, modifierLoop.variables);
            if (isGroup)
            {
                if (modifierLoop.reference is not IPrefabable prefabable)
                    return null;

                if (!GameData.Current.TryFindObjectWithTag(modifier, prefabable, FormatStringVariables(modifier.GetValue(2, modifierLoop.variables), modifierLoop.variables), out BeatmapObject otherBeatmapObject))
                    return null;

                if (useVisual && otherBeatmapObject.runtimeObject && otherBeatmapObject.runtimeObject.visualObject is TextObject otherTextObject)
                    return otherTextObject.GetText();
                else
                    return otherBeatmapObject.text;
            }

            if (modifierLoop.reference is not BeatmapObject beatmapObject)
                return null;

            if (useVisual && beatmapObject.runtimeObject && beatmapObject.runtimeObject.visualObject is TextObject textObject)
                return modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = textObject.GetText();
            else
                return modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = beatmapObject.text;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isGroup)
            {
                modifierCard.PrefabGroupOnly(modifier, reference);
                modifierCard.GroupFieldGenerator(modifier, reference, "Object Group", 2);
            }

            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.BoolGenerator(modifier, reference, "Use Visual", 1, false);
        }

        #endregion
    }
}

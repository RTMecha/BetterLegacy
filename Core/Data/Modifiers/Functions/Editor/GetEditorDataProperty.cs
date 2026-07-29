using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetEditorDataProperty : ModifierVariableBase
    {
        #region Constructors

        public GetEditorDataProperty(EditorDataProperty editorDataProperty)
        {
            this.editorDataProperty = editorDataProperty;
            Name = "getEditor" + editorDataProperty.ToString();
            SetupModifier($"EDITOR_{editorDataProperty.ToString().ToUpper()}_VAR", "False");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Editor;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.FullBeatmapCompatible;

        readonly EditorDataProperty editorDataProperty;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            ObjectEditorData editorData = null;
            var prefabable = modifierLoop.reference.AsPrefabable();
            if (prefabable != null && prefabable.FromPrefab && modifier.GetBool(1, false, modifierLoop.variables))
                editorData = prefabable.GetPrefabObject()?.EditorData;
            else if (modifierLoop.reference is IEditable editable)
                editorData = editable.EditorData;
            return editorDataProperty switch
            {
                EditorDataProperty.Bin => (editorData?.Bin ?? 0).ToString(),
                EditorDataProperty.Layer => (editorData?.Layer ?? 0).ToString(),
                _ => string.Empty,
            };
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.BoolGenerator(modifier, reference, $"Use Prefab Object {editorDataProperty}", 1);
        }

        #endregion

        #region Sub Classes

        public enum EditorDataProperty
        {
            Bin,
            Layer,
        }

        #endregion
    }
}

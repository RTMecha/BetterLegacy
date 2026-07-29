using SimpleJSON;

using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetJSON : ModifierActionBase
    {
        #region Constructors

        public GetJSON(Type type)
        {
            this.type = type;
            Name = "getJSON";
            if (type != Type.Object)
                Name += type.ToString();
            SetupModifier("JSON_VAR", "save_file", "chapter/0/data");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.JSON;

        readonly Type type;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (type != Type.Object)
            {
                if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables)), out string file))
                    return;

                var jn = JSON.Parse(file);

                var fjn = modifier.version == 1 ? jn.GetPath(modifier.GetValue(2, modifierLoop.variables)) : jn[FormatStringVariables(modifier.GetValue(2, modifierLoop.variables), modifierLoop.variables)][FormatStringVariables(modifier.GetValue(3, modifierLoop.variables), modifierLoop.variables)][type.ToString().ToLower()];

                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = fjn;
                return;
            }

            var key = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);
            string json;
            if (!RTFile.TryReadFromFile(ModifiersHelper.GetSaveFile(key), out json))
                json = key;

            try
            {
                var jn = JSON.Parse(json);
                var json1 = FormatStringVariables(modifier.GetValue(2, modifierLoop.variables), modifierLoop.variables);
                if (!string.IsNullOrEmpty(json1))
                    jn = modifier.version == 1 ? jn.GetPath(json1) : jn[json1];

                modifierLoop.variables[FormatStringVariables(modifier.GetValue(0), modifierLoop.variables)] = jn;
            }
            catch { }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Path", 1);
            modifierCard.StringGenerator(modifier, reference, "JSON Path", 2);
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Object,
            String,
            Float,
        }

        #endregion
    }
}

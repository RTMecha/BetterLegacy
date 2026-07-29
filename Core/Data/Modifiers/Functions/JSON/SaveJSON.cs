using SimpleJSON;

using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SaveJSON : ModifierActionBase
    {
        #region Constructors

        public SaveJSON() => SetupModifier(false, "save_file", "chapter/0/data", "0");

        #endregion

        #region Values

        public override string Name => "saveJSON";

        public override ModifierCategoryType Category => ModifierCategoryType.JSON;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            var path = modifier.GetValue(0, modifierLoop.variables);
            var jsonPath = modifier.GetValue(1, modifierLoop.variables);
            var value = modifier.GetValue(2, modifierLoop.variables);

            if (path.Contains("\\") || path.Contains("/") || path.Contains(".."))
                return;

            var profile = RTFile.CombinePaths(RTFile.ApplicationDirectory, "profile");
            RTFile.CreateDirectory(profile);

            var file = RTFile.CombinePaths(profile, $"{path}{FileFormat.SES.Dot()}");
            var jn = JSON.Parse(RTFile.FileExists(file) ? RTFile.ReadFromFile(file) : "{}");

            if (string.IsNullOrEmpty(value))
                jn.RemovePath(jsonPath, false);
            else
                jn.SetPath(jsonPath, value);

            RTFile.WriteToFile(file, jn.ToString(3));
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Path", 0);
            modifierCard.StringGenerator(modifier, reference, "JSON Path", 1);
            modifierCard.StringGenerator(modifier, reference, "Value", 2);
        }

        #endregion
    }
}

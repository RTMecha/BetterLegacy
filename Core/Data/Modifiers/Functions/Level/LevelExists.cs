using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LevelExists : ModifierTriggerBase
    {
        #region Constructors

        public LevelExists(bool isPath)
        {
            this.isPath = isPath;
            Name = "level";
            if (isPath)
                Name += "Path";
            Name += "Exists";
            SetupModifier(isPath ? "level name" : "0");
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Level;

        readonly bool isPath;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isPath)
            {
                var basePath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, modifier.GetValue(0, modifierLoop.variables));
                return
                    RTFile.FileExists(RTFile.CombinePaths(basePath, Level.Level.METADATA_LSB)) ||
                    RTFile.FileExists(RTFile.CombinePaths(basePath, Level.Level.METADATA_VGM)) ||
                    RTFile.FileExists(basePath + FileFormat.ASSET.Dot());
            }

            var id = modifier.GetValue(0, modifierLoop.variables);
            return LevelManager.Levels.Has(x => x.id == id);
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, isPath ? "Path" : "ID", 0);
        }

        #endregion
    }
}

using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class RemoveLevelVariable : ModifierActionBase
    {
        #region Constructors

        public RemoveLevelVariable(bool isCurrent, Type type)
        {
            this.isCurrent = isCurrent;
            this.type = type;
            Name = "get";
            if (isCurrent)
                Name += "Current";
            Name += type.ToString() + "Variable";
            SetupModifier("VAR");
            if (!isCurrent)
                Modifier.values.Insert(0, string.Empty);
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly bool isCurrent;

        readonly Type type;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (type)
            {
                case Type.Level: {
                        int index = 0;
                        Level.Level level = null;
                        if (!isCurrent)
                        {
                            var id = FormatStringVariables(modifier.GetValue(index, modifierLoop.variables), modifierLoop.variables);
                            level = LevelManager.Levels.Find(x => x.id == id);
                            index++;
                        }
                        else
                            level = LevelManager.CurrentLevel;
                        if (!level || !level.saveData || level.saveData.Variables == null)
                            return;

                        level.saveData.Variables.Remove(FormatStringVariables(modifier.GetValue(index, modifierLoop.variables), modifierLoop.variables));
                        LevelManager.SaveProgress();
                        break;
                    }
                case Type.Collection: {
                        int index = 0;
                        LevelCollection levelCollection = null;
                        if (!isCurrent)
                        {
                            var id = FormatStringVariables(modifier.GetValue(index, modifierLoop.variables), modifierLoop.variables);
                            levelCollection = LevelManager.LevelCollections.Find(x => x.id == id);
                            index++;
                        }
                        else
                            levelCollection = LevelManager.CurrentLevelCollection;
                        if (!levelCollection || !levelCollection.saveData || levelCollection.saveData.Variables == null)
                            return;

                        levelCollection.saveData.Variables.Remove(FormatStringVariables(modifier.GetValue(index, modifierLoop.variables), modifierLoop.variables));
                        LevelManager.SaveProgress();
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (!isCurrent)
                modifierCard.StringGenerator(modifier, reference, "ID", 0);
            modifierCard.StringGenerator(modifier, reference, $"{type} Variable Name", isCurrent ? 0 : 1);
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Level,
            Collection,
        }

        #endregion
    }
}

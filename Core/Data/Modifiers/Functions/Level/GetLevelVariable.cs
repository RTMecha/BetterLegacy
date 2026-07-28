using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetLevelVariable : ModifierVariableBase
    {
        #region Constructors

        public GetLevelVariable(bool isCurrent, Type type)
        {
            this.isCurrent = isCurrent;
            this.type = type;
            Name = "get";
            if (isCurrent)
                Name += "Current";
            Name += type.ToString() + "Variable";
            SetupModifier("VAR", string.Empty, $"{type.ToString().ToUpper()}_VAR");
            if (!isCurrent)
                Modifier.values.Insert(0, string.Empty);
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Level;

        readonly bool isCurrent;

        readonly Type type;

        #endregion

        #region Functions

        public override string GetKey(Modifier modifier, ModifierLoop modifierLoop) => FormatStringVariables(modifier.GetValue(!isCurrent ? 3 : 2), modifierLoop.variables);

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop)
        {
            var id = isCurrent ? null : FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            var levelVariableName = FormatStringVariables(modifier.GetValue(!isCurrent ? 1 : 0, modifierLoop.variables), modifierLoop.variables);
            var defaultValue = FormatStringVariables(modifier.GetValue(!isCurrent ? 2 : 1, modifierLoop.variables), modifierLoop.variables);
            if (string.IsNullOrEmpty(levelVariableName))
                return null;

            switch (type)
            {
                case Type.Level: {
                        var level = isCurrent ? ProjectArrhythmia.State.InEditor && EditorLevelManager.inst.CurrentLevel ? EditorLevelManager.inst.CurrentLevel : LevelManager.CurrentLevel : LevelManager.Levels.Find(x => x.id == id);

                        var val = level && level.saveData && level.saveData.Variables != null && level.saveData.Variables.TryGetValue(levelVariableName, out string value) ? value : defaultValue;
                        if (!string.IsNullOrEmpty(val))
                            return val;
                        break;
                    }
                case Type.Collection: {
                        var levelCollection = isCurrent ? ProjectArrhythmia.State.InEditor && EditorLevelManager.inst.CurrentLevelCollection ? EditorLevelManager.inst.CurrentLevelCollection : LevelManager.CurrentLevelCollection : LevelManager.LevelCollections.Find(x => x.id == id);

                        var val = levelCollection && levelCollection.saveData && levelCollection.saveData.Variables != null && levelCollection.saveData.Variables.TryGetValue(levelVariableName, out string value) ? value : defaultValue;
                        if (!string.IsNullOrEmpty(val))
                            return val;
                        break;
                    }
            }
            return null;
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            int index = 0;
            if (!isCurrent)
            {
                modifierCard.StringGenerator(modifier, reference, "ID", index);
                index++;
            }
            modifierCard.StringGenerator(modifier, reference, $"{type} Variable Name", index);
            index++;
            modifierCard.StringGenerator(modifier, reference, "Default Value", index);
            index++;
            modifierCard.StringGenerator(modifier, reference, "Variable Name", index, renderVariables: false);
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

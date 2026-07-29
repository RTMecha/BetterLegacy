using System.Collections.Generic;

using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ClearLevelVariables : ModifierActionBase
    {
        #region Constructors

        public ClearLevelVariables(bool isCurrent, Type type)
        {
            this.isCurrent = isCurrent;
            this.type = type;
            Name = "get";
            if (isCurrent)
                Name += "Current";
            Name += type.ToString() + "Variable";
            SetupModifier();
            if (!isCurrent)
                Modifier.values.Add(string.Empty);
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
            GetDictionary(modifier, modifierLoop)?.Clear();
            LevelManager.SaveProgress();
        }

        Dictionary<string, string> GetDictionary(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isCurrent)
                return type switch
                {
                    Type.Level => LevelManager.CurrentLevel?.saveData?.Variables,
                    Type.Collection => LevelManager.CurrentLevelCollection?.saveData?.Variables,
                    _ => null,
                };
            var id = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            return type switch
            {
                Type.Level => LevelManager.Levels.Find(x => x.id == id)?.saveData?.Variables,
                Type.Collection => LevelManager.LevelCollections.Find(x => x.id == id)?.saveData?.Variables,
                _ => null,
            };
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (!isCurrent)
                modifierCard.StringGenerator(modifier, reference, "ID", 0);
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

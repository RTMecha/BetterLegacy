using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Story;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class GetStorySaveProperty : ModifierVariableBase
    {
        #region Constructors

        public GetStorySaveProperty(Property property)
        {
            this.property = property;
            Name = "getStorySave" + property.ToString() + "DEVONLY";
            SetupModifier($"STORY_{property.ToString().ToUpper()}_VAR", $"{property}Variable", property switch
            {
                Property.Bool => "False",
                Property.Int => "0",
                Property.Float => "0",
                _ => string.Empty,
            });
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.StoryOnlyCompatible;

        readonly Property property;

        #endregion

        #region Functions

        public override string GetValue(Modifier modifier, ModifierLoop modifierLoop) => property switch
        {
            Property.Bool => !ProjectArrhythmia.State.InStory ? modifier.GetBool(2, false, modifierLoop.variables).ToString() : StoryManager.inst.CurrentSave.LoadBool(modifier.GetValue(1, modifierLoop.variables), modifier.GetBool(2, false, modifierLoop.variables)).ToString(),
            Property.Int => !ProjectArrhythmia.State.InStory ? modifier.GetInt(2, 0, modifierLoop.variables).ToString() : StoryManager.inst.CurrentSave.LoadInt(modifier.GetValue(1, modifierLoop.variables), modifier.GetInt(2, 0, modifierLoop.variables)).ToString(),
            Property.Float => !ProjectArrhythmia.State.InStory ? modifier.GetFloat(2, 0f, modifierLoop.variables).ToString() : StoryManager.inst.CurrentSave.LoadFloat(modifier.GetValue(1, modifierLoop.variables), modifier.GetFloat(2, 0f, modifierLoop.variables)).ToString(),
            Property.String => !ProjectArrhythmia.State.InStory ? modifier.GetValue(2, modifierLoop.variables).ToString() : StoryManager.inst.CurrentSave.LoadString(modifier.GetValue(1, modifierLoop.variables), modifier.GetValue(2, modifierLoop.variables)).ToString(),
            _ => null,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Variable Name", 0, renderVariables: false);
            modifierCard.StringGenerator(modifier, reference, "Value Name", 1);
            switch (property)
            {
                case Property.Bool: {
                        modifierCard.BoolGenerator(modifier, reference, "Default Value", 2);
                        break;
                    }
                case Property.Int: {
                        modifierCard.IntegerGenerator(modifier, reference, "Default Value", 2);
                        break;
                    }
                case Property.Float: {
                        modifierCard.SingleGenerator(modifier, reference, "Default Value", 2);
                        break;
                    }
                case Property.String: {
                        modifierCard.StringGenerator(modifier, reference, "Default Value", 2);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            Bool,
            Int,
            Float,
            String,
        }

        #endregion
    }
}

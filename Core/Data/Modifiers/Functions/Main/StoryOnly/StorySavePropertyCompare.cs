using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Story;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class StorySavePropertyCompare : ModifierTriggerBase
    {
        #region Constructors

        public StorySavePropertyCompare(Property property, NumberComparison comparison)
        {
            this.property = property;
            this.comparison = comparison;
            Name = "storyLoad" + property.ToString();
            if (property != Property.Bool && property != Property.String)
                Name += comparison.ToString();
            Name += "DEVONLY";
            SetupModifier($"{property}Variable", property switch
            {
                Property.Bool => "True",
                Property.Int => "0",
                Property.Float => "0",
                _ => string.Empty,
            });
            if (property != Property.Bool && property != Property.String)
                Modifier.values.Add("0");
            if (property == Property.String)
                Modifier.values.Add(string.Empty);
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Main;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.StoryOnlyCompatible;

        readonly Property property;

        readonly NumberComparison comparison;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => property switch
        {
            Property.Bool => StoryManager.inst && StoryManager.inst.CurrentSave && StoryManager.inst.CurrentSave.LoadBool(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), modifier.GetBool(1, false, modifierLoop.variables)),
            Property.Int => StoryManager.inst && StoryManager.inst.CurrentSave && comparison.Compare(StoryManager.inst.CurrentSave.LoadInt(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), modifier.GetInt(1, 0, modifierLoop.variables)), modifier.GetInt(2, 0, modifierLoop.variables)),
            Property.Float => StoryManager.inst && StoryManager.inst.CurrentSave && comparison.Compare(StoryManager.inst.CurrentSave.LoadFloat(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), modifier.GetFloat(1, 0, modifierLoop.variables)), modifier.GetFloat(2, 0, modifierLoop.variables)),
            Property.String => StoryManager.inst && StoryManager.inst.CurrentSave && StoryManager.inst.CurrentSave.LoadString(FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables), modifier.GetValue(1, modifierLoop.variables)) == modifier.GetValue(2, modifierLoop.variables),
            _ => false,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (property)
            {
                case Property.Bool: {
                        modifierCard.StringGenerator(modifier, reference, "Load", 0);
                        modifierCard.BoolGenerator(modifier, reference, "Default", 1, false);
                        break;
                    }
                case Property.Int: {
                        modifierCard.StringGenerator(modifier, reference, "Load", 0);
                        modifierCard.IntegerGenerator(modifier, reference, "Default", 1, 0);
                        modifierCard.IntegerGenerator(modifier, reference, "Equals", 2, 0);
                        break;
                    }
                case Property.Float: {
                        modifierCard.StringGenerator(modifier, reference, "Load", 0);
                        modifierCard.IntegerGenerator(modifier, reference, "Default", 1, 0);
                        modifierCard.IntegerGenerator(modifier, reference, "Equals", 2, 0);
                        break;
                    }
                case Property.String: {
                        modifierCard.StringGenerator(modifier, reference, "Load", 0);
                        modifierCard.StringGenerator(modifier, reference, "Default", 1);
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

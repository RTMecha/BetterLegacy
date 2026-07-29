using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Story;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class StorySaveProperty : ModifierActionBase
    {
        #region Constructors

        public StorySaveProperty(Property property)
        {
            this.property = property;
            Name = "storySave" + property.ToString() + "DEVONLY";
            if (property == Property.IntVariable)
            {
                SetupModifier(false, "IntVariable");
                return;
            }
            SetupModifier(false, $"{property}Variable", property switch
            {
                Property.Bool => "True",
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

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (!ProjectArrhythmia.State.InStory)
                return;

            switch (property)
            {
                case Property.Bool: {
                        StoryManager.inst.CurrentSave.SaveBool(modifier.GetValue(0, modifierLoop.variables), modifier.GetBool(1, false, modifierLoop.variables));
                        break;
                    }
                case Property.Int: {
                        StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, modifierLoop.variables), modifier.GetInt(1, 0, modifierLoop.variables));
                        break;
                    }
                case Property.Float: {
                        StoryManager.inst.CurrentSave.SaveFloat(modifier.GetValue(0, modifierLoop.variables), modifier.GetFloat(1, 0f, modifierLoop.variables));
                        break;
                    }
                case Property.String: {
                        StoryManager.inst.CurrentSave.SaveString(modifier.GetValue(0, modifierLoop.variables), modifier.GetValue(1, modifierLoop.variables));
                        break;
                    }
                case Property.IntVariable: {
                        if (modifierLoop.reference is IModifyable modifyable)
                            StoryManager.inst.CurrentSave.SaveInt(modifier.GetValue(0, modifierLoop.variables), modifyable.IntVariable);
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Save", 0);
            switch (property)
            {
                case Property.Bool: {
                        modifierCard.BoolGenerator(modifier, reference, "Value", 1);
                        break;
                    }
                case Property.Int: {
                        modifierCard.IntegerGenerator(modifier, reference, "Value", 1);
                        break;
                    }
                case Property.Float: {
                        modifierCard.SingleGenerator(modifier, reference, "Value", 1);
                        break;
                    }
                case Property.String: {
                        modifierCard.StringGenerator(modifier, reference, "Value", 1);
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
            IntVariable,
        }

        #endregion
    }
}

using UnityEngine.UI;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetLevelProperty : ModifierActionBase
    {
        #region Constructors

        public SetLevelProperty(Property property)
        {
            this.property = property;
            Name = "set" + property.ToString();
            Modifier = property switch
            {
                Property.AudioTransition => CreateModifier(Name, false, "0.5"),
                Property.IntroFade => CreateModifier(Name, false, "True"),
                Property.LevelEndFunc => CreateModifier(Name, false, "0", string.Empty, "True"),
                _ => null,
            };
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly Property property;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            switch (property)
            {
                case Property.AudioTransition: {
                        LevelManager.songFadeTransition = modifier.GetFloat(0, 0.5f, modifierLoop.variables);
                        break;
                    }
                case Property.IntroFade: {
                        RTGameManager.doIntroFade = modifier.GetBool(0, true, modifierLoop.variables);
                        break;
                    }
                case Property.LevelEndFunc: {
                        if (ProjectArrhythmia.State.InEditor)
                            return;

                        var endLevelFunc = modifier.GetInt(0, 0, modifierLoop.variables);

                        if (endLevelFunc > 0)
                        {
                            RTBeatmap.Current.endLevelFunc = (EndLevelFunction)(endLevelFunc - 1);
                            RTBeatmap.Current.endLevelData = modifier.GetValue(1, modifierLoop.variables);
                        }
                        RTBeatmap.Current.endLevelUpdateProgress = modifier.GetBool(2, true, modifierLoop.variables);
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            switch (property)
            {
                case Property.AudioTransition: {
                        modifierCard.SingleGenerator(modifier, reference, "Value", 0, 1f);
                        break;
                    }
                case Property.IntroFade: {
                        modifierCard.BoolGenerator(modifier, reference, "Should Fade", 0, true);
                        break;
                    }
                case Property.LevelEndFunc: {
                        var options = CoreHelper.ToOptionData<EndLevelFunction>();
                        options.Insert(0, new Dropdown.OptionData("Default"));
                        modifierCard.DropdownGenerator(modifier, reference, "End Level Function", 0, options);
                        modifierCard.StringGenerator(modifier, reference, "End Level Data", 1);
                        modifierCard.BoolGenerator(modifier, reference, "Save Player Data", 2, true);
                        break;
                    }
            }
        }

        #endregion

        #region Sub Classes

        public enum Property
        {
            AudioTransition,
            IntroFade,
            LevelEndFunc,
        }

        #endregion
    }
}

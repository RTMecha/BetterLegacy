using BetterLegacy.Configs;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class ConfigSetting : ModifierTriggerBase
    {
        #region Constructors

        public ConfigSetting(Setting setting)
        {
            this.setting = setting;
            Name = "config" + setting.ToString();
            SetupModifier();
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override ModifierCategoryType Category => ModifierCategoryType.Application;

        readonly Setting setting;

        #endregion

        #region Functions

        public override bool Run(Modifier modifier, ModifierLoop modifierLoop) => setting switch
        {
            Setting.LDM => CoreConfig.Instance.LDM.Value,
            Setting.ShowEffects => EventsConfig.Instance.ShowFX.Value,
            Setting.ShowPlayerGUI => EventsConfig.Instance.ShowGUI.Value,
            Setting.ShowIntro => EventsConfig.Instance.ShowIntro.Value,
            _ => false,
        };

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion

        #region Sub Classes

        public enum Setting
        {
            LDM,
            ShowEffects,
            ShowPlayerGUI,
            ShowIntro,
        }

        #endregion
    }
}

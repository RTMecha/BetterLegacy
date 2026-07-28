using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetDiscordStatus : ModifierActionBase
    {
        #region Constructors

        public SetDiscordStatus(bool isReset)
        {
            this.isReset = isReset;
            Name = isReset ? "resetDiscordStatus" : "setDiscordStatus";
            if (!isReset)
                SetupModifier("{1}: {0}", "In {3}", "0", "0");
            else
                SetupModifier();
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Application;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly bool isReset;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (isReset)
            {
                DiscordHelper.UpdateDiscordStatus(
                    state: (ProjectArrhythmia.State.InEditor ? "Editing: " : "Level: ") + MetaData.Current.beatmap.name,
                    details: ProjectArrhythmia.State.InEditor ? DiscordHelper.IN_EDITOR : DiscordHelper.IN_ARCADE,
                    icon: ProjectArrhythmia.State.InEditor ? DiscordHelper.EDITOR : DiscordHelper.ARCADE,
                    art: DiscordHelper.LOGO_LEGACY);
                return;
            }

            var discordSubIcons = DiscordHelper.subIcons;
            var discordIcons = DiscordHelper.icons;

            var state = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            var details = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);
            var discordSubIcon = modifier.GetValue(2, modifierLoop.variables);
            var discordIcon = modifier.GetValue(3, modifierLoop.variables);

            discordSubIcon = int.TryParse(discordSubIcon, out int discordSubIconIndex) ?
                discordSubIcons[RTMath.Clamp(discordSubIconIndex, 0, discordSubIcons.Length - 1)] : DiscordHelper.subIcons.Has(x => x == discordSubIcon) ? discordSubIcon : DiscordHelper.PLAY;
            discordIcon = int.TryParse(discordIcon, out int discordIconIndex) ?
                discordIcons[RTMath.Clamp(discordIconIndex, 0, discordIcons.Length - 1)] : DiscordHelper.icons.Has(x => x == discordIcon) ? discordIcon : DiscordHelper.LOGO_LEGACY;

            try
            {
                DiscordHelper.UpdateDiscordStatus(
                    string.Format(state, MetaData.Current.beatmap.name, $"{(!ProjectArrhythmia.State.InEditor ? "Game" : "Editor")}", $"{(!ProjectArrhythmia.State.InEditor ? "Level" : "Editing")}", $"{(!ProjectArrhythmia.State.InEditor ? "Arcade" : "Editor")}"),
                    string.Format(details, MetaData.Current.beatmap.name, $"{(!ProjectArrhythmia.State.InEditor ? "Game" : "Editor")}", $"{(!ProjectArrhythmia.State.InEditor ? "Level" : "Editing")}", $"{(!ProjectArrhythmia.State.InEditor ? "Arcade" : "Editor")}"),
                    discordSubIcon, discordIcon);
            }
            catch
            {
                DiscordHelper.UpdateDiscordStatus(
                    state: (ProjectArrhythmia.State.InEditor ? "Editing: " : "Level: ") + MetaData.Current.beatmap.name,
                    details: ProjectArrhythmia.State.InEditor ? DiscordHelper.IN_EDITOR : DiscordHelper.IN_ARCADE,
                    icon: ProjectArrhythmia.State.InEditor ? DiscordHelper.EDITOR : DiscordHelper.ARCADE,
                    art: DiscordHelper.LOGO_LEGACY);
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (isReset)
                return;
            modifierCard.StringGenerator(modifier, reference, "State", 0);
            modifierCard.StringGenerator(modifier, reference, "Details", 1);
            modifierCard.DropdownGenerator(modifier, reference, "Sub Icon", 2, CoreHelper.StringToOptionData(DiscordHelper.subIcons));
            modifierCard.DropdownGenerator(modifier, reference, "Icon", 3, CoreHelper.StringToOptionData(DiscordHelper.icons));
        }

        #endregion
    }
}

using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class QuitToArcade : ModifierActionBase
    {
        #region Constructors

        public QuitToArcade() => SetupModifier(false);

        #endregion

        #region Values

        public override string Name => "quitToArcade";

        public override CategoryType Category => CategoryType.Interfaces;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            // only host can do this
            if (ProjectArrhythmia.State.IsClient)
                return;

            if (ProjectArrhythmia.State.InEditor && !EditorManager.inst.isEditing && EditorConfig.Instance.ModifiersCanLoadLevels.Value)
            {
                string str = RTFile.BasePath;
                if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                {
                    GameData.Current.SaveData(RTFile.CombinePaths(str, $"level-modifier-backup{FileFormat.LSB.Dot()}"), () =>
                    {
                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                    });
                }

                GameManager.inst.QuitToArcade();

                return;
            }

            if (!ProjectArrhythmia.State.InEditor)
                ArcadeHelper.QuitToArcade();
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {

        }

        #endregion
    }
}

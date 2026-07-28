using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class DownloadLevel : ModifierActionBase
    {
        #region Constructors

        public DownloadLevel() => SetupModifier("0", string.Empty, string.Empty, string.Empty, string.Empty, "True");

        #endregion

        #region Values

        public override string Name => "downloadLevel";

        public override CategoryType Category => CategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            // only host can do this
            if (ProjectArrhythmia.State.IsClient)
                return;

            var id = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
            var levelInfo = new LevelInfo(
                id: id,
                arcadeID: id,
                serverID: FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables),
                workshopID: FormatStringVariables(modifier.GetValue(2, modifierLoop.variables), modifierLoop.variables),
                songTitle: FormatStringVariables(modifier.GetValue(3, modifierLoop.variables), modifierLoop.variables),
                name: FormatStringVariables(modifier.GetValue(4, modifierLoop.variables), modifierLoop.variables));

            if (!ProjectArrhythmia.State.InEditor)
            {
                if (LevelManager.Levels.TryFind(x => x.id == levelInfo.arcadeID, out Level.Level level))
                {
                    LevelManager.Play(level);
                    return;
                }
            }

            if (ProjectArrhythmia.State.IsEditing)
            {
                if (EditorLevelManager.inst.LevelPanels.TryFind(x => x.Item && x.Item.metadata is MetaData metaData && metaData.ID == levelInfo.arcadeID, out LevelPanel levelPanel))
                {
                    if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                        return;

                    var path = System.IO.Path.GetFileName(levelPanel.Path);

                    RTEditor.inst.ShowWarningPopup($"You are about to enter the level {path}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                    {
                        string str = RTFile.BasePath;
                        if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                        {
                            GameData.Current.SaveData(str + "level-modifier-backup.lsb", () =>
                            {
                                EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                            });
                        }

                        EditorLevelManager.inst.LoadLevel(levelPanel.Item);
                    });
                    return;
                }
                return;
            }

            LevelCollection.DownloadLevel(null, levelInfo, level =>
            {
                if (modifier.GetBool(5, true, modifierLoop.variables))
                    LevelManager.Play(level);
                else
                    RTBeatmap.Current.Resume(); // in case of softlock
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "Arcade ID", 0);
            modifierCard.StringGenerator(modifier, reference, "Server ID", 1);
            modifierCard.StringGenerator(modifier, reference, "Workshop ID", 2);
            modifierCard.StringGenerator(modifier, reference, "Song Title", 3);
            modifierCard.StringGenerator(modifier, reference, "Level Name", 4);
            modifierCard.BoolGenerator(modifier, reference, "Play Level", 5, true);
        }

        #endregion
    }
}

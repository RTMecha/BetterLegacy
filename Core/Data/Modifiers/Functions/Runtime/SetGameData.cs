using BetterLegacy.Configs;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class SetGameData : ModifierActionBase
    {
        #region Constructors

        public SetGameData() => SetupModifier(false, "level");

        #endregion

        #region Values

        public override string Name => "setGameData";

        public override ModifierCategoryType Category => ModifierCategoryType.Runtime;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            if (ProjectArrhythmia.State.InEditor && !EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                return;

            var currentLevel = ProjectArrhythmia.State.InEditor ? EditorLevelManager.inst.CurrentLevel : LevelManager.CurrentLevel;

            if (!MetaData.Current || !MetaData.Current.package || !currentLevel)
                return;

            if (MetaData.Current.package.GetLevel(modifier.GetValue(0, modifierLoop.variables)) is not PackageMetaData.File file)
                return;

            RTLevel.Current.postTick.Enqueue(() =>
            {
                GameManager.inst.gameState = GameManager.State.Parsing;
                GameData.Current = GameData.ReadFromFile(currentLevel.GetFile(file.fileName),
                    RTFile.FileIsFormat(file.fileName, FileFormat.VGD) ? ArrhythmiaType.VG : ArrhythmiaType.LS);
                if (GameData.Current && GameData.Current.data && GameData.Current.data.level)
                    RTBeatmap.Current.respawnImmediately = GameData.Current.data.level.respawnImmediately;
                ThemeManager.inst.UpdateAllThemes();

                GameManager.inst.UpdateTimeline();
                RTBeatmap.Current.ResetCheckpoint();

                RTPlayer.SetGameDataProperties();

                CoroutineHelper.StartCoroutine(RTLevel.IReinit());

                if (ProjectArrhythmia.State.InEditor)
                {
                    Editor.Data.Dialogs.EditorDialog.CurrentDialog.Close();

                    RTCheckpointEditor.inst.CreateGhostCheckpoints();

                    RTEventEditor.inst.CreateTimelineKeyframes();

                    RTMarkerEditor.inst.CreateMarkers();
                    RTMarkerEditor.inst.markerLooping = false;
                    RTMarkerEditor.inst.markerLoopBegin = null;
                    RTMarkerEditor.inst.markerLoopEnd = null;

                    RTThemeEditor.inst.LoadInternalThemes();

                    EditorTimeline.inst.InitTimelineObjects();

                    RTCheckpointEditor.inst.SetCurrentCheckpoint(0);

                    RTPrefabEditor.inst.currentQuickPrefab = null; // remove selected quick prefab as it probably doesn't exist anymore.

                    RTBeatmap.Current.ResetCheckpoint();

                    EditorTimeline.inst.RenderTimeline();
                    EditorTimeline.inst.RenderBins();
                }

                GameManager.inst.gameState = GameManager.State.Playing;
            });
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            modifierCard.StringGenerator(modifier, reference, "File ID", 0);
        }

        #endregion
    }
}

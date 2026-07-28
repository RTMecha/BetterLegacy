using BetterLegacy.Configs;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Managers;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Managers;

namespace BetterLegacy.Core.Data.Modifiers.Functions
{
    public class LoadLevel : ModifierActionBase
    {
        #region Constructors

        public LoadLevel(Type type)
        {
            Name = "loadLevel";
            if (type != Type.Path)
                Name += type.ToString();
            SetupModifier(false);
            if (type != Type.Previous && type != Type.Hub)
                Modifier.values.Add(type == Type.Path || type == Type.Internal ? "level name" : "0");
            if (type == Type.Collection)
                Modifier.values.Add(string.Empty);
        }

        #endregion

        #region Values

        public override string Name { get; }

        public override CategoryType Category => CategoryType.Level;

        public override ModifierCompatibility Compatibility => ModifierCompatibility.LevelControlCompatible;

        readonly Type type;

        #endregion

        #region Functions

        public override void Run(Modifier modifier, ModifierLoop modifierLoop)
        {
            // only host can do this
            if (ProjectArrhythmia.State.IsClient)
                return;

            switch (type)
            {
                case Type.Path: {
                        var path = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);

                        if (ProjectArrhythmia.State.IsEditing)
                        {
                            if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                                return;

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

                                EditorLevelManager.inst.LoadLevel(new Level.Level(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, path)));
                            });

                            return;
                        }

                        if (ProjectArrhythmia.State.InEditor)
                            return;

                        var levelPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListSlash, $"{path}");
                        if (RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.Level.LEVEL_LSB)) || RTFile.FileExists(RTFile.CombinePaths(levelPath, Level.Level.LEVEL_VGD)) || RTFile.FileExists(levelPath + FileFormat.ASSET.Dot()))
                            LevelManager.Load(levelPath);
                        else
                            SoundManager.inst.PlaySound(DefaultSounds.Block);
                        break;
                    }
                case Type.ID: {
                        var id = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
                        if (string.IsNullOrEmpty(id) || id == "0" || id == "-1")
                            return;

                        if (!ProjectArrhythmia.State.InEditor)
                        {
                            if (LevelManager.Levels.TryFind(x => x.id == id, out Level.Level level))
                                LevelManager.Play(level);
                            else
                                SoundManager.inst.PlaySound(DefaultSounds.Block);

                            return;
                        }

                        if (!ProjectArrhythmia.State.IsEditing)
                            return;

                        if (EditorLevelManager.inst.LevelPanels.TryFind(x => x.Item && x.Item.metadata is MetaData metaData && metaData.ID == id, out LevelPanel levelPanel))
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
                        }
                        else
                            SoundManager.inst.PlaySound(DefaultSounds.Block);
                        break;
                    }
                case Type.Internal: {
                        var path = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);

                        if (!ProjectArrhythmia.State.InEditor)
                        {
                            var filePath = RTFile.CombinePaths(RTFile.BasePath, path);
                            if (!ProjectArrhythmia.State.InEditor && (RTFile.FileExists(RTFile.CombinePaths(filePath, Level.Level.LEVEL_LSB)) || RTFile.FileIsFormat(RTFile.CombinePaths(filePath, Level.Level.LEVEL_VGD)) || RTFile.FileExists(filePath + FileFormat.ASSET.Dot())))
                                LevelManager.Load(filePath);
                            else
                                SoundManager.inst.PlaySound(DefaultSounds.Block);

                            return;
                        }

                        if (ProjectArrhythmia.State.IsEditing && RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, EditorManager.inst.currentLoadedLevel, path, Level.Level.LEVEL_LSB)))
                        {
                            if (!EditorConfig.Instance.ModifiersCanLoadLevels.Value)
                                return;

                            RTEditor.inst.ShowWarningPopup($"You are about to enter the level {RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, path)}, are you sure you want to continue? Any unsaved progress will be lost!", () =>
                            {
                                string str = RTFile.BasePath;
                                if (EditorConfig.Instance.ModifiersSavesBackup.Value)
                                {
                                    GameData.Current.SaveData(RTFile.CombinePaths(str, "level-modifier-backup.lsb"), () =>
                                    {
                                        EditorManager.inst.DisplayNotification($"Saved backup to {System.IO.Path.GetFileName(RTFile.RemoveEndSlash(str))}", 2f, EditorManager.NotificationType.Success);
                                    });
                                }

                                EditorLevelManager.inst.LoadLevel(new Level.Level(RTFile.CombinePaths(EditorManager.inst.currentLoadedLevel, path)));
                            });
                        }
                        break;
                    }
                case Type.Previous: {
                        if (ProjectArrhythmia.State.InEditor)
                            return;

                        LevelManager.Play(LevelManager.PreviousLevel);
                        break;
                    }
                case Type.Hub: {
                        if (ProjectArrhythmia.State.InEditor)
                            return;

                        LevelManager.Play(LevelManager.Hub);
                        break;
                    }
                case Type.InCollection: {
                        var id = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
                        if (!ProjectArrhythmia.State.InEditor && LevelManager.CurrentLevelCollection && LevelManager.CurrentLevelCollection.levels.TryFind(x => x.id == id, out Level.Level level))
                            LevelManager.Play(level);
                        break;
                    }
                case Type.Collection: {
                        var id = FormatStringVariables(modifier.GetValue(0, modifierLoop.variables), modifierLoop.variables);
                        if (ProjectArrhythmia.State.InEditor || !LevelManager.LevelCollections.TryFind(x => x.id == id, out LevelCollection levelCollection))
                            return;

                        var levelID = FormatStringVariables(modifier.GetValue(1, modifierLoop.variables), modifierLoop.variables);

                        var entryLevelIndex = levelCollection.EntryLevelIndex;
                        if (!string.IsNullOrEmpty(levelID) && LevelManager.Levels.TryFindIndex(x => x && x.id == levelID, out int arcadeLevelIndex))
                            entryLevelIndex = arcadeLevelIndex;
                        if (!string.IsNullOrEmpty(levelID) && RTSteamManager.inst.Levels.TryFindIndex(x => x && x.id == levelID, out int steamLevelIndex))
                            entryLevelIndex = steamLevelIndex;

                        if (entryLevelIndex < 0)
                            return;

                        levelCollection.DownloadLevel(levelCollection.levelInformation[entryLevelIndex], LevelManager.Play);
                        break;
                    }
            }
        }

        public override void RenderModifierCard(Modifier modifier, ModifierCard modifierCard, IModifierReference reference, IModifyable modifyable)
        {
            if (type == Type.Previous || type == Type.Hub)
                return;

            modifierCard.StringGenerator(modifier, reference, type switch
            {
                Type.Path => "Path",
                Type.ID => "ID",
                Type.Internal => "Inner Path",
                Type.InCollection => "ID",
                Type.Collection => "Collection ID",
                _ => string.Empty,
            }, 0);
            if (type == Type.Collection)
                modifierCard.StringGenerator(modifier, reference, "Level ID", 1);
        }

        #endregion

        #region Sub Classes

        public enum Type
        {
            Path,
            ID,
            Internal,
            Previous,
            Hub,
            InCollection,
            Collection,
        }

        #endregion
    }
}

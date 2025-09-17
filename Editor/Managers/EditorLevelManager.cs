using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using CielaSpike;
using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Components.Player;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Player;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Managers.Networking;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;
using BetterLegacy.Editor.Data.Elements;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Manages levels loaded in the editor and general functions to do with a level in the editor.
    /// </summary>
    public class EditorLevelManager : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="EditorLevelManager"/> global instance reference.
        /// </summary>
        public static EditorLevelManager inst;

        /// <summary>
        /// Initializes <see cref="EditorLevelManager"/>.
        /// </summary>
        public static void Init() => EditorManager.inst.gameObject.AddComponent<EditorLevelManager>();

        void Awake()
        {
            inst = this;
            CoroutineHelper.WaitUntil(
                () => RTEditor.inst,
                Setup);
        }

        void Setup()
        {
            LevelCollectionPopup = RTEditor.inst.GeneratePopup(EditorPopup.LEVEL_COLLECTION_POPUP, "Level Collections", Vector2.zero, new Vector2(600f, 400f),
                _val => RenderLevelCollections(),
                close: () => onLevelCollectionSelected = null,
                placeholderText: "Search for collection...");

            var levelPathGameObject = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(LevelCollectionPopup.GameObject.transform, "collection path");
            ((RectTransform)levelPathGameObject.transform).anchoredPosition = EditorConfig.Instance.OpenLevelEditorPathPos.Value;
            ((RectTransform)levelPathGameObject.transform).sizeDelta = new Vector2(EditorConfig.Instance.OpenLevelEditorPathLength.Value, 32f);

            TooltipHelper.AssignTooltip(levelPathGameObject, "Editor Path", 3f);

            RTEditor.inst.levelCollectionPathField = levelPathGameObject.GetComponent<InputField>();
            RTEditor.inst.levelCollectionPathField.characterValidation = InputField.CharacterValidation.None;
            RTEditor.inst.levelCollectionPathField.onValueChanged.ClearAll();
            RTEditor.inst.levelCollectionPathField.onEndEdit.ClearAll();
            RTEditor.inst.levelCollectionPathField.textComponent.alignment = TextAnchor.MiddleLeft;
            RTEditor.inst.levelCollectionPathField.textComponent.fontSize = 16;
            RTEditor.inst.levelCollectionPathField.text = RTEditor.inst.CollectionsPath;
            RTEditor.inst.levelCollectionPathField.onValueChanged.AddListener(_val => RTEditor.inst.CollectionsPath = _val);
            RTEditor.inst.levelCollectionPathField.onEndEdit.AddListener(_val => LoadLevelCollections());

            EditorThemeManager.AddInputField(RTEditor.inst.levelCollectionPathField);

            var levelClickable = levelPathGameObject.AddComponent<Clickable>();
            levelClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Level folder", () =>
                    {
                        RTEditor.inst.BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            RTEditor.inst.levelCollectionPathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Editor path to {RTEditor.inst.CollectionsPath}!", 2f, EditorManager.NotificationType.Success);
                            RTEditor.inst.BrowserPopup.Close();
                            LoadLevelCollections();
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", RTEditor.inst.OpenLevelCollectionListFolder));
            };
            EditorHelper.SetComplexity(levelPathGameObject, Complexity.Advanced);

            var levelListReloader = EditorPrefabHolder.Instance.SpriteButton.Duplicate(LevelCollectionPopup.GameObject.transform, "reload");
            levelListReloader.transform.AsRT().anchoredPosition = EditorConfig.Instance.OpenLevelListRefreshPosition.Value;
            levelListReloader.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            (levelListReloader.GetOrAddComponent<HoverTooltip>()).tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh level collection list",
                hint = "Clicking this will reload the level collection list."
            });

            var levelListReloaderButton = levelListReloader.GetComponent<Button>();
            levelListReloaderButton.onClick.NewListener(LoadLevelCollections);

            EditorThemeManager.AddSelectable(levelListReloaderButton, ThemeGroup.Function_2, false);

            levelListReloaderButton.image.sprite = EditorSprites.ReloadSprite;

            LoadLevelCollections();

            EditorHelper.AddEditorDropdown("Open Level Collection", string.Empty, EditorHelper.FILE_DROPDOWN, EditorSprites.OpenSprite, () =>
            {
                LevelCollectionPopup.Open();
                RenderLevelCollections();
            }, 2);

            LevelCollectionDialog = new LevelCollectionEditorDialog();
            LevelCollectionDialog.Init();

            LevelInfoDialog = new LevelInfoEditorDialog();
            LevelInfoDialog.Init();
        }

        void Update() => autosaveTimer.Update();

        #endregion

        #region Values

        /// <summary>
        /// The default level name.
        /// </summary>
        public const string DEFAULT_LEVEL_NAME = "New Awesome Beatmap";
        /// <summary>
        /// The default artist.
        /// </summary>
        public const string DEFAULT_ARTIST = "Kaixo";
        /// <summary>
        /// The default song title.
        /// </summary>
        public const string DEFAULT_SONG_TITLE = "Intertia";
        /// <summary>
        /// The default level difficulty.
        /// </summary>
        public const int DEFAULT_DIFFICULTY = 2;

        /// <summary>
        /// The currently loaded level.
        /// </summary>
        public Level CurrentLevel { get; set; }

        public LevelCollection CurrentLevelCollection { get; set; }

        public LevelCollection OpenLevelCollection { get; set; }

        /// <summary>
        /// Loaded editor levels.
        /// </summary>
        public List<LevelPanel> LevelPanels { get; set; } = new List<LevelPanel>();
        
        /// <summary>
        /// Selected editor levels.
        /// </summary>
        public List<LevelPanel> SelectedLevels => LevelPanels.FindAll(x => x.Selected);

        /// <summary>
        /// Loaded editor level collections.
        /// </summary>
        public List<LevelCollectionPanel> LevelCollectionPanels { get; set; } = new List<LevelCollectionPanel>();

        /// <summary>
        /// Selected editor level collections.
        /// </summary>
        public List<LevelCollectionPanel> SelectedLevelCollections => LevelCollectionPanels.FindAll(x => x.Selected);

        /// <summary>
        /// If the current level is a newly created one.
        /// </summary>
        public bool fromNewLevel;

        /// <summary>
        /// Settings used for creating new levels.
        /// </summary>
        public NewLevelSettings newLevelSettings = new NewLevelSettings(
            audioPath: string.Empty,
            levelName: DEFAULT_LEVEL_NAME,
            songArtist: DEFAULT_ARTIST,
            songTitle: DEFAULT_SONG_TITLE,
            difficulty: DEFAULT_DIFFICULTY
            );

        /// <summary>
        /// If the level should cut.
        /// </summary>
        public bool shouldCutLevel;

        /// <summary>
        /// Path to the copied level.
        /// </summary>
        public string copiedLevelPath;

        /// <summary>
        /// If the editor is currently loading a level.
        /// </summary>
        public bool loadingLevel;

        /// <summary>
        /// If the editor is currently autosaving.
        /// </summary>
        public bool autosaving;

        /// <summary>
        /// Amount of time that has passed since the last autosave.
        /// </summary>
        public RTTimer autosaveTimer;

        public NewLevelPopup NewLevelPopup { get; set; }

        public ContentPopup OpenLevelPopup { get; set; }

        public ContentPopup LevelCollectionPopup { get; set; }

        public LevelCollectionEditorDialog LevelCollectionDialog { get; set; }

        public LevelInfoEditorDialog LevelInfoDialog { get; set; }

        public Action<LevelCollectionPanel, PointerEventData> onLevelCollectionSelected;

        public bool LevelInfoCollapseIcon { get; set; } = true;
        public bool CollapseIcon { get; set; } = true;
        public bool CollapseBanner { get; set; } = true;

        #endregion

        #region Methods

        /// <summary>
        /// Pastes the copied / cut level.
        /// </summary>
        public void PasteLevel()
        {
            if (string.IsNullOrEmpty(copiedLevelPath))
            {
                EditorManager.inst.DisplayNotification("No level has been copied yet!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.DirectoryExists(copiedLevelPath))
            {
                EditorManager.inst.DisplayNotification("Copied level no longer exists.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var folderName = Path.GetFileName(copiedLevelPath);
            var directory = RTFile.GetDirectory(copiedLevelPath);
            var editorPath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath);

            if (shouldCutLevel)
            {
                if (RTFile.DirectoryExists(copiedLevelPath.Replace(directory, editorPath)))
                {
                    EditorManager.inst.DisplayNotification($"Level with the name \"{folderName}\" already exists in this location.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                Directory.Move(copiedLevelPath, copiedLevelPath.Replace(directory, editorPath));
                LoadLevels();
                EditorManager.inst.DisplayNotification($"Succesfully moved {folderName}!", 2f, EditorManager.NotificationType.Success);

                return;
            }

            var result = copiedLevelPath;
            int num = 0;
            while (Directory.Exists(result.Replace(directory, editorPath)))
            {
                result = $"{copiedLevelPath} [{num}]";
                num++;
            }

            var files = Directory.GetFiles(copiedLevelPath, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i].Replace("\\", "/").Replace(copiedLevelPath, result).Replace(directory, editorPath);
                var copyToDirectory = Path.GetDirectoryName(file);
                RTFile.CreateDirectory(copyToDirectory);

                RTFile.CopyFile(files[i], file);
            }

            LoadLevels();
            EditorManager.inst.DisplayNotification($"Succesfully pasted {folderName}!", 2f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Loads editor levels from the current editor folder.
        /// </summary>
        public void LoadLevels() => CoroutineHelper.StartCoroutine(ILoadLevels());

        /// <summary>
        /// Loads editor levels from the current editor folder.
        /// </summary>
        public IEnumerator ILoadLevels()
        {
            LevelPanels.Clear();
            OpenLevelPopup.ClearContent();

            var list = new List<Coroutine>();
            var fullPath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath);

            try
            {
                var collectionPath = RTFile.CombinePaths(fullPath, LevelCollection.COLLECTION_LSCO);
                if (!OpenLevelCollection && RTFile.FileExists(collectionPath))
                {
                    var jn = JSON.Parse(RTFile.ReadFromFile(collectionPath));
                    CurrentLevelCollection = LevelCollection.Parse(fullPath, jn, false);
                }
                else
                    CurrentLevelCollection = null;
            }
            catch (Exception ex)
            {
                CurrentLevelCollection = null;
                CoreHelper.LogError($"Had an exception with trying to read a collection file.\nException: {ex}");
            }

            // Back
            if (EditorConfig.Instance.ShowFoldersInLevelList.Value && RTFile.GetDirectory(fullPath) != RTEditor.inst.BeatmapsPath || OpenLevelCollection)
            {
                var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(OpenLevelPopup.Content, "back");
                var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.label.text = OpenLevelCollection ? "< Return" : "< Up a folder";

                folderButtonStorageFolder.label.horizontalOverflow = EditorConfig.Instance.OpenLevelTextHorizontalWrap.Value;
                folderButtonStorageFolder.label.verticalOverflow = EditorConfig.Instance.OpenLevelTextVerticalWrap.Value;
                folderButtonStorageFolder.label.fontSize = EditorConfig.Instance.OpenLevelTextFontSize.Value;

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath), () => { LoadLevels(); RTEditor.inst.HideNameEditor(); })),
                            new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                            new ButtonFunction("Paste", PasteLevel),
                            new ButtonFunction("Open List in File Explorer", RTEditor.inst.OpenLevelListFolder));

                        return;
                    }

                    if (OpenLevelCollection)
                    {
                        OpenLevelCollection = null;
                        LoadLevels();
                        return;
                    }

                    if (RTEditor.inst.editorPathField.text == RTEditor.inst.EditorPath)
                    {
                        RTEditor.inst.editorPathField.text = RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath)).Replace(RTEditor.inst.BeatmapsPath + "/", "");
                        RTEditor.inst.UpdateEditorPath(false);
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);
            }

            var currentLevelCollection = CurrentLevelCollection ?? OpenLevelCollection;
            if (currentLevelCollection)
            {
                var add = EditorPrefabHolder.Instance.CreateAddButton(OpenLevelPopup.Content);
                add.Text = "Add Level";
                add.OnClick.NewListener(() =>
                {
                    var levelInfo = new LevelInfo();
                    levelInfo.id = PAObjectBase.GetNumberID();
                    levelInfo.index = currentLevelCollection.levelInformation.Count;
                    levelInfo.collection = currentLevelCollection;
                    OpenLevelInfoEditor(levelInfo, () =>
                    {
                        currentLevelCollection.levelInformation.Add(levelInfo);
                        currentLevelCollection.Save();
                        LevelInfoDialog.Close();
                        LoadLevels();
                    });
                });

                foreach (var levelInfo in currentLevelCollection.levelInformation)
                {
                    var path = !string.IsNullOrEmpty(levelInfo.editorPath) ? levelInfo.editorPath : levelInfo.path;
                    Level level;
                    if (!(!string.IsNullOrEmpty(path) && Level.TryVerify(RTFile.CombinePaths(fullPath, path), false, out level) ||
                        Level.TryVerify(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, path), false, out level) ||
                        (SteamWorkshopManager.inst && SteamWorkshopManager.inst.Initialized && SteamWorkshopManager.inst.Levels.TryFind(x => x && x.id == levelInfo.workshopID, out level)) ||
                        LevelManager.Levels.TryFind(x => x && x.id == levelInfo.arcadeID, out level)))
                    {
                        var levelPanel = new LevelPanel();
                        levelPanel.Init(levelInfo);

                        if (levelInfo.icon)
                            levelPanel.SetIcon(levelInfo.icon);
                        else
                            levelPanel.SetDefaultIcon();
                        LevelPanels.Add(levelPanel);
                    }
                    else
                    {
                        levelInfo.level = level;
                        level.collectionInfo = levelInfo;
                        currentLevelCollection.levels.Add(level);

                        var levelPanel = new LevelPanel();
                        levelPanel.Init(levelInfo.level);

                        if (RTFile.FileExists(levelInfo.level.GetFile(Level.LEVEL_JPG)))
                            list.Add(levelPanel.LoadImageCoroutine(Level.LEVEL_JPG, LevelPanels.Add));
                        else if (RTFile.FileExists(levelInfo.level.GetFile(Level.COVER_JPG)))
                            list.Add(levelPanel.LoadImageCoroutine(Level.COVER_JPG, LevelPanels.Add));
                        else
                        {
                            if (levelInfo.icon)
                                levelPanel.SetIcon(levelInfo.icon);
                            else
                                levelPanel.SetDefaultIcon();
                            LevelPanels.Add(levelPanel);
                        }
                    }
                }
            }
            else
            {
                var files = Directory.GetDirectories(fullPath);
                foreach (var file in files)
                {
                    var path = RTFile.ReplaceSlash(file);

                    var levelPanel = new LevelPanel();

                    if (!Level.TryVerify(path, false, out Level level))
                    {
                        if (!EditorConfig.Instance.ShowFoldersInLevelList.Value)
                            continue;

                        levelPanel.Init(path);
                        LevelPanels.Add(levelPanel);

                        list.Add(levelPanel.LoadImageCoroutine($"folder_icon{FileFormat.PNG.Dot()}"));

                        continue;
                    }

                    levelPanel.Init(level);

                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_JPG)))
                        list.Add(levelPanel.LoadImageCoroutine(Level.LEVEL_JPG, LevelPanels.Add));
                    else if (RTFile.FileExists(RTFile.CombinePaths(path, Level.COVER_JPG)))
                        list.Add(levelPanel.LoadImageCoroutine(Level.COVER_JPG, LevelPanels.Add));
                    else
                    {
                        levelPanel.SetDefaultIcon();
                        LevelPanels.Add(levelPanel);
                    }
                }
            }

            if (list.Count >= 1)
                yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, OpenLevelPopupOnFinish));
            else
                OpenLevelPopupOnFinish();

            CoreHelper.Log($"Finished loading editor levels.");

            yield break;
        }

        public void LoadLevelCollections() => CoroutineHelper.StartCoroutine(ILoadLevelCollections());

        /// <summary>
        /// Loads editor level collections from the current editor folder.
        /// </summary>
        public IEnumerator ILoadLevelCollections()
        {
            LevelCollectionPanels.Clear();
            LevelCollectionPopup.ClearContent();

            var list = new List<Coroutine>();
            var fullPath = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.CollectionsPath);

            if (EditorConfig.Instance.ShowFoldersInLevelList.Value && RTFile.GetDirectory(fullPath) != RTEditor.inst.BeatmapsPath)
            {
                var gameObjectFolder = EditorManager.inst.folderButtonPrefab.Duplicate(LevelCollectionPopup.Content, "back");
                var folderButtonStorageFolder = gameObjectFolder.GetComponent<FunctionButtonStorage>();
                var folderButtonFunctionFolder = gameObjectFolder.AddComponent<FolderButtonFunction>();

                var hoverUIFolder = gameObjectFolder.AddComponent<HoverUI>();
                hoverUIFolder.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUIFolder.animatePos = false;
                hoverUIFolder.animateSca = true;

                folderButtonStorageFolder.label.text = "< Up a folder";

                folderButtonStorageFolder.label.horizontalOverflow = EditorConfig.Instance.OpenLevelTextHorizontalWrap.Value;
                folderButtonStorageFolder.label.verticalOverflow = EditorConfig.Instance.OpenLevelTextVerticalWrap.Value;
                folderButtonStorageFolder.label.fontSize = EditorConfig.Instance.OpenLevelTextFontSize.Value;

                folderButtonStorageFolder.button.onClick.ClearAll();
                folderButtonFunctionFolder.onClick = eventData =>
                {
                    if (eventData.button == PointerEventData.InputButton.Right)
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Create folder", () => RTEditor.inst.ShowFolderCreator(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.CollectionsPath), () => { LoadLevelCollections(); RTEditor.inst.HideNameEditor(); })),
                            //new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                            //new ButtonFunction("Paste", PasteLevel),
                            new ButtonFunction("Open List in File Explorer", RTEditor.inst.OpenLevelCollectionListFolder));

                        return;
                    }

                    if (RTEditor.inst.levelCollectionPathField.text == RTEditor.inst.CollectionsPath)
                    {
                        OpenLevelCollection = null;
                        RTEditor.inst.levelCollectionPathField.text = RTFile.GetDirectory(RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.CollectionsPath)).Replace(RTEditor.inst.BeatmapsPath + "/", "");
                        LoadLevelCollections();
                    }
                };

                EditorThemeManager.ApplySelectable(folderButtonStorageFolder.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyLightText(folderButtonStorageFolder.label);
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(LevelCollectionPopup.Content);
            add.Text = "Create Empty Collection";
            add.OnClick.NewListener(() =>
            {
                var collection = CreateNewLevelCollection();
                collection.Save();
                LoadLevelCollections();
            });

            if (!RTFile.DirectoryExists(fullPath))
            {
                if (list.Count >= 1)
                    yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, RenderLevelCollections));
                else
                    RenderLevelCollections();
                yield break;
            }

            var files = Directory.GetDirectories(fullPath);
            foreach (var file in files)
            {
                var path = RTFile.ReplaceSlash(file);

                var levelCollectionPanel = new LevelCollectionPanel();

                if (!LevelCollection.TryVerify(path, false, out LevelCollection levelCollection))
                {
                    if (!EditorConfig.Instance.ShowFoldersInLevelList.Value)
                        continue;

                    levelCollectionPanel.Init(path);
                    LevelCollectionPanels.Add(levelCollectionPanel);

                    list.Add(levelCollectionPanel.LoadImageCoroutine($"folder_icon{FileFormat.PNG.Dot()}"));

                    continue;
                }

                levelCollectionPanel.Init(levelCollection);

                if (RTFile.FileExists(RTFile.CombinePaths(path, LevelCollection.ICON_PNG)))
                    list.Add(levelCollectionPanel.LoadImageCoroutine(LevelCollection.ICON_PNG, LevelCollectionPanels.Add));
                else if (RTFile.FileExists(RTFile.CombinePaths(path, LevelCollection.ICON_JPG)))
                    list.Add(levelCollectionPanel.LoadImageCoroutine(LevelCollection.ICON_JPG, LevelCollectionPanels.Add));
                else
                {
                    levelCollectionPanel.SetDefaultIcon();
                    LevelCollectionPanels.Add(levelCollectionPanel);
                }

                if (RTFile.FileExists(RTFile.CombinePaths(path, LevelCollection.BANNER_PNG)))
                    list.Add(levelCollectionPanel.LoadBannerCoroutine(LevelCollection.BANNER_PNG));
                else if (RTFile.FileExists(RTFile.CombinePaths(path, LevelCollection.BANNER_JPG)))
                    list.Add(levelCollectionPanel.LoadBannerCoroutine(LevelCollection.BANNER_JPG));
            }

            if (list.Count >= 1)
                yield return StartCoroutine(LSHelpers.WaitForMultipleCoroutines(list, RenderLevelCollections));
            else
                RenderLevelCollections();

            yield break;
        }

        /// <summary>
        /// Loads a level in the editor.
        /// </summary>
        /// <param name="levelPanel">Level to load and edit.</param>
        public void LoadLevel(LevelPanel levelPanel) => CoroutineHelper.StartCoroutine(ILoadLevel(levelPanel.Item));

        /// <summary>
        /// Loads a level in the editor.
        /// </summary>
        /// <param name="level">Level to load and edit.</param>
        public void LoadLevel(Level level) => CoroutineHelper.StartCoroutine(ILoadLevel(level));

        /// <summary>
        /// Loads a level in the editor.
        /// </summary>
        /// <param name="level">Level to load and edit.</param>
        public IEnumerator ILoadLevel(Level level)
        {
            loadingLevel = true;

            CurrentLevel = level;
            var fullPath = RTFile.RemoveEndSlash(level.path);
            var currentFile = level.CurrentFile;
            level.currentFile = null; // reset since autosave loading should be temporary.

            var sw = CoreHelper.StartNewStopwatch();

            RTPlayer.GameMode = GameMode.Regular;

            EditorTimeline.inst.SetLayer(0, EditorTimeline.LayerType.Objects);

            ProjectArrhythmia.Window.ResetTitle();

            RandomHelper.UpdateSeed();

            CoreHelper.Log("Clearing data...");

            for (int i = 0; i < EditorTimeline.inst.timelineObjects.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineObjects[i];
                if (timelineObject.GameObject)
                    Destroy(timelineObject.GameObject);
                EditorTimeline.inst.timelineObjects[i] = null;
            }
            EditorTimeline.inst.timelineObjects.Clear();

            for (int i = 0; i < EditorTimeline.inst.timelineKeyframes.Count; i++)
            {
                var timelineObject = EditorTimeline.inst.timelineKeyframes[i];
                if (timelineObject.GameObject)
                    Destroy(timelineObject.GameObject);
                EditorTimeline.inst.timelineKeyframes[i] = null;
            }
            EditorTimeline.inst.timelineKeyframes.Clear();

            for (int i = 0; i < ObjEditor.inst.TimelineParents.Count; i++)
                LSHelpers.DeleteChildren(ObjEditor.inst.TimelineParents[i]);

            if (GameData.Current)
            {
                try
                {
                    for (int i = 0; i < GameData.Current.assets.sounds.Count; i++)
                    {
                        CoreHelper.Destroy(GameData.Current.assets.sounds[i].audio);
                        GameData.Current.assets.sounds[i].audio = null;
                    }
                    GameData.Current.assets.sounds.Clear();
                }
                catch (Exception ex)
                {
                    CoreHelper.LogError($"Failed to clear sound assets due to the exception: {ex}");
                }
            }

            RTLevel.Reinit(false);

            // We stop and play the doggo bop animation in case the user has looked at the settings dialog.
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");
            EditorManager.inst.InvokeRepeating("LoadingIconUpdate", 0f, 0.05f);

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            var name = Path.GetFileName(fullPath);

            EditorManager.inst.currentLoadedLevel = name;
            EditorManager.inst.SetPitch(1f);

            EditorManager.inst.timelineScrollRectBar.value = 0f;
            GameManager.inst.gameState = GameManager.State.Loading;

            EditorManager.inst.ClearPopups();
            EditorDialog.CurrentDialog?.Close();
            RTEditor.inst.InfoPopup.Open();

            if (EditorManager.inst.hasLoadedLevel && EditorConfig.Instance.BackupPreviousLoadedLevel.Value && RTFile.FileExists(GameManager.inst.path))
            {
                CoreHelper.Log("Backing up previous level...");

                RTEditor.inst.InfoPopup.SetInfo($"Backing up previous level [ {Path.GetFileName(RTFile.RemoveEndSlash(GameManager.inst.path))} ]");

                GameData.Current.SaveData(RTFile.CombinePaths(RTFile.BasePath, $"level-open-backup{FileFormat.LSB.Dot()}"));

                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Loading data...");

            RTEditor.inst.InfoPopup.SetInfo($"Loading Level Data for [ {name} ]");

            CoreHelper.Log($"Loading {currentFile}...");

            GameManager.inst.path = RTFile.CombinePaths(fullPath, Level.LEVEL_LSB);
            RTFile.BasePath = RTFile.AppendEndSlash(fullPath);
            GameManager.inst.levelName = name;
            RTEditor.inst.InfoPopup.SetInfo($"Loading Level Music for [ {name} ]\n\nIf this is taking more than a minute or two check if the song file (.ogg / .wav / .mp3) is corrupt. If not, then something went really wrong.");

            yield return CoroutineHelper.StartCoroutine(level.LoadAudioClipRoutine());

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Setting up Video...");
            yield return StartCoroutine(RTVideoManager.inst.Setup(fullPath));
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            GameManager.inst.gameState = GameManager.State.Parsing;
            CoreHelper.Log("Parsing data...");
            RTEditor.inst.InfoPopup.SetInfo($"Parsing Level Data for [ {name} ]");
            string rawJSON = RTFile.ReadFromFile(RTFile.CombinePaths(fullPath, currentFile));
            if (string.IsNullOrEmpty(rawJSON))
            {
                RTEditor.inst.InfoPopup.SetInfo($"{currentFile} is empty or corrupt.");
                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                yield break;
            }

            try
            {
                MetaData.Current = null;
                MetaData.Current = level.metadata;

                if (MetaData.Current.arcadeID == null || MetaData.Current.arcadeID == "0" || MetaData.Current.arcadeID == "-1")
                    MetaData.Current.arcadeID = LSText.randomNumString(16);

                if (ProjectArrhythmia.RequireUpdate(MetaData.Current.beatmap.gameVersion))
                    rawJSON = LevelManager.UpdateBeatmap(rawJSON, MetaData.Current.beatmap.gameVersion);

                GameData.Current = null;
                GameData.Current = level.IsVG ?
                    GameData.ParseVG(JSON.Parse(rawJSON), MetaData.Current.Version) :
                    GameData.Parse(JSON.Parse(rawJSON));
            }
            catch (Exception ex)
            {
                RTEditor.inst.InfoPopup.SetInfo($"Something went wrong when parsing the level data. Press the open log folder key ({CoreConfig.Instance.OpenPAPersistentFolder.Value}) and send the Player.log file to Mecha.");

                EditorManager.inst.DisplayNotification("Level could not load.", 3f, EditorManager.NotificationType.Error);

                CoreHelper.LogError($"Level loading caught an error: {ex}");

                yield break;
            }

            // preload audio clips
            if (GameData.Current && GameData.Current.assets)
                for (int i = 0; i < GameData.Current.assets.sounds.Count; i++)
                {
                    var soundAsset = GameData.Current.assets.sounds[i];
                    if (!soundAsset.audio && soundAsset.autoLoad)
                        yield return CoroutineHelper.StartCoroutine(soundAsset.LoadAudioClip());
                }

            yield return CoroutineHelper.StartCoroutine(AssetEditor.inst.ILoadAssets());

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            AchievementEditor.inst.LoadAchievements();

            if (RTEditor.inst.PreviewCover != null && RTEditor.inst.PreviewCover.gameObject)
                RTEditor.inst.PreviewCover.gameObject.SetActive(false);

            CoreHelper.Log("Playing level music...");
            RTEditor.inst.InfoPopup.SetInfo($"Playing Music for [ {name} ]\n\nIf it doesn't, then something went wrong!");
            SetCurrentAudio(level.music);
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            if (EditorConfig.Instance.WaveformGenerate.Value)
            {
                CoreHelper.Log("Assigning waveform textures...");
                RTEditor.inst.InfoPopup.SetInfo($"Assigning Waveform Textures for [ {name} ]");
                StartCoroutine(EditorTimeline.inst.AssignTimelineTexture(level.music));
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }
            else
            {
                CoreHelper.Log("Skipping waveform textures...");
                RTEditor.inst.InfoPopup.SetInfo($"Skipping Waveform Textures for [ {name} ]");
                EditorTimeline.inst.SetTimelineSprite(null);
                CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");
            }

            CoreHelper.Log("Updating timeline...");
            RTEditor.inst.InfoPopup.SetInfo($"Updating Timeline for [ {name} ]");
            EditorTimeline.inst.UpdateTimelineSizes();
            GameManager.inst.UpdateTimeline();
            RTMetaDataEditor.inst.RenderDialog();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            RTCheckpointEditor.inst.CreateGhostCheckpoints();

            RTEditor.inst.InfoPopup.SetInfo($"Updating states for [ {name} ]");
            if (CoreConfig.Instance.DiscordTimestampUpdatesPerLevel.Value)
                DiscordController.inst.presence.startTimestamp = SteamworksFacepunch.Epoch.Current;
            CoreHelper.UpdateDiscordStatus($"Editing: {MetaData.Current.beatmap.name}", "In Editor", "editor");

            CoreHelper.Log("Spawning players...");
            try
            {
                PlayersData.Load(level.GetFile(Level.PLAYERS_LSB));
                PlayerManager.SpawnPlayersOnStart();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            RTPlayer.SetGameDataProperties();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating objects...");
            yield return StartCoroutine(RTLevel.IReinit());
            RTLevel.Current.Tick();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Updating timeline...");
            RTEventEditor.inst.CreateEventObjects();

            RTMarkerEditor.inst.CreateMarkers();
            RTMarkerEditor.inst.markerLooping = false;
            RTMarkerEditor.inst.markerLoopBegin = null;
            RTMarkerEditor.inst.markerLoopEnd = null;

            RTThemeEditor.inst.LoadInternalThemes();

            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            CoreHelper.Log("Creating timeline objects...");
            RTEditor.inst.InfoPopup.SetInfo($"Setting first object of [ {name} ]");
            EditorTimeline.inst.ClearTimelineObjects();
            EditorTimeline.inst.timelineObjects = EditorTimeline.inst.ToTimelineObjects().ToList();
            CoreHelper.Log($"Done. Time taken: {sw.Elapsed}");

            RTCheckpointEditor.inst.SetCurrentCheckpoint(0);

            RTEditor.inst.InfoPopup.SetInfo("Done!");
            RTEditor.inst.InfoPopup.Close();
            EditorManager.inst.CancelInvoke("LoadingIconUpdate");

            RTBeatmap.Current.ResetCheckpoint();
            GameManager.inst.gameState = GameManager.State.Playing;

            EditorManager.inst.DisplayNotification($"{name} Level loaded", 2f, EditorManager.NotificationType.Success);
            EditorManager.inst.UpdatePlayButton();
            EditorManager.inst.hasLoadedLevel = true;

            SetAutosave();

            TriggerHelper.AddEventTriggers(RTEditor.inst.timeField.gameObject, TriggerHelper.ScrollDelta(RTEditor.inst.timeField, max: AudioManager.inst.CurrentAudioSource.clip.length));

            // Load Settings like timeline position, editor layer, bpm active, etc
            RTEditor.inst.LoadSettings();

            if (!EditorConfig.Instance.RetainCopiedPrefabInstanceData.Value)
                RTPrefabEditor.inst.copiedInstanceData = null;

            if (EditorConfig.Instance.AnalyzeBPMOnLevelLoad.Value && !RTEditor.inst.editorInfo.analyzedBPM)
            {
                yield return CoroutineHelper.StartCoroutineAsync(UniBpmAnalyzer.IAnalyzeBPM(AudioManager.inst.CurrentAudioSource.clip, bpm =>
                {
                    RTEditor.inst.editorInfo.analyzedBPM = true;
                    MetaData.Current.song.bpm = bpm;
                    RTEditor.inst.editorInfo.bpm = bpm;
                    EditorTimeline.inst.SetTimelineGridSize();
                }));
            }

            EditorTimeline.inst.RenderTimeline();
            EditorTimeline.inst.RenderBins();

            if (EditorConfig.Instance.LevelPausesOnStart.Value)
            {
                AudioManager.inst.CurrentAudioSource.Pause();
                EditorManager.inst.UpdatePlayButton();
            }

            Example.Current?.brain?.Notice(ExampleBrain.Notices.LOADED_LEVEL);

            loadingLevel = false;
            fromNewLevel = false;

            rawJSON = null;

            CoreHelper.StopAndLogStopwatch(sw, $"Finished loading {name}");
            sw = null;

            yield break;
        }

        public void LoadLevelCollection(LevelCollectionPanel levelCollectionPanel) => LoadLevelCollection(levelCollectionPanel.Item);
        
        public void LoadLevelCollection(LevelCollection levelCollection)
        {
            OpenLevelCollection = levelCollection;
            LoadLevels();
        }

        /// <summary>
        /// Saves the current level.
        /// </summary>
        public void SaveLevel()
        {
            var currentLevelCollection = CurrentLevelCollection ?? OpenLevelCollection;
            if (currentLevelCollection)
                currentLevelCollection.Save();

            if (!EditorManager.inst.hasLoadedLevel)
            {
                if (currentLevelCollection)
                    EditorManager.inst.DisplayNotification($"Saved level collection!", 2f, EditorManager.NotificationType.Success);
                else
                    EditorManager.inst.DisplayNotification("Beatmap can't be saved until you load a level.", 5f, EditorManager.NotificationType.Error);
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Attempting to save beatmap already, please wait!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            RTFile.CopyFile(RTFile.CombinePaths(RTFile.BasePath, Level.LEVEL_LSB), RTFile.CombinePaths(RTFile.BasePath, $"level-previous{FileFormat.LSB.Dot()}"));

            MetaData.Current.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.METADATA_LSB));
            CoroutineHelper.StartCoroutine(SaveData());
            CoroutineHelper.StartCoroutine(SavePlayers());
            RTEditor.inst.SaveSettings();

            return;
        }

        /// <summary>
        /// Saves the current level.
        /// </summary>
        public IEnumerator SaveData()
        {
            EditorManager.inst.DisplayNotification("Saving Beatmap!", 1f, EditorManager.NotificationType.Warning);
            EditorManager.inst.savingBeatmap = true;

            var gameData = GameData.Current;
            if (gameData.data && gameData.data.level)
                gameData.data.level.modVersion = LegacyPlugin.ModVersion.ToString();

            if (EditorConfig.Instance.SaveAsync.Value)
                yield return CoroutineHelper.StartCoroutineAsync(gameData.ISaveData(CurrentLevel.GetFile(Level.LEVEL_LSB)));
            else
                yield return CoroutineHelper.StartCoroutine(gameData.ISaveData(CurrentLevel.GetFile(Level.LEVEL_LSB)));

            AchievementEditor.inst.SaveAchievements();

            yield return CoroutineHelper.Seconds(0.5f);

            EditorManager.inst.DisplayNotification("Saved Beatmap!", 2f, EditorManager.NotificationType.Success);
            EditorManager.inst.savingBeatmap = false;

            Example.Current?.brain?.Notice(ExampleBrain.Notices.EDITOR_SAVED_LEVEL);

            yield break;
        }

        /// <summary>
        /// Saves the current players data.
        /// </summary>
        public IEnumerator SavePlayers()
        {
            EditorManager.inst.DisplayNotification("Saving Player Models...", 1f, EditorManager.NotificationType.Warning);

            if (EditorConfig.Instance.SaveAsync.Value)
                yield return CoroutineHelper.StartCoroutineAsync(CoroutineHelper.DoAction(() => RTFile.WriteToFile(CurrentLevel.GetFile(Level.PLAYERS_LSB), PlayersData.Current.ToJSON().ToString())));
            else
                RTFile.WriteToFile(CurrentLevel.GetFile(Level.PLAYERS_LSB), PlayersData.Current.ToJSON().ToString());

            PlayersData.Save();

            yield return CoroutineHelper.Seconds(0.5f);
            EditorManager.inst.DisplayNotification("Saved Player Models!", 1f, EditorManager.NotificationType.Success);
        }

        /// <summary>
        /// Sets the current audio the editor should use.
        /// </summary>
        /// <param name="audioClip">Audio to set.</param>
        public void SetCurrentAudio(AudioClip audioClip) => AudioManager.inst.PlayMusic(null, audioClip, true, 0f, true);

        /// <summary>
        /// Restarts the autosave loop.
        /// </summary>
        public void SetAutosave()
        {
            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            RTFile.CreateDirectory(autosavesDirectory);
            var files = Directory.GetFiles(autosavesDirectory, $"autosave_{FileFormat.LSB.ToPattern()}", SearchOption.TopDirectoryOnly);

            EditorManager.inst.autosaves.Clear();
            EditorManager.inst.autosaves.AddRange(files);

            EditorManager.inst.CancelInvoke(nameof(AutosaveLevel));
            CancelInvoke(nameof(AutosaveLevel));
            InvokeRepeating(nameof(AutosaveLevel), EditorConfig.Instance.AutosaveLoopTime.Value, EditorConfig.Instance.AutosaveLoopTime.Value);
        }

        /// <summary>
        /// Autosaves the level.
        /// </summary>
        public void AutosaveLevel()
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"autosave_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Autosaving backup...", 2f, EditorManager.NotificationType.Warning);

            EditorManager.inst.autosaves.Add(autosavePath);

            while (EditorManager.inst.autosaves.Count > EditorConfig.Instance.AutosaveLimit.Value)
            {
                var first = EditorManager.inst.autosaves[0];
                if (RTFile.FileExists(first))
                    File.Delete(first);

                EditorManager.inst.autosaves.RemoveAt(0);
            }

            GameData.Current?.SaveData(autosavePath);

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.EDITOR_AUTOSAVED);

            autosaveTimer.Reset();
            autosaving = false;
        }

        /// <summary>
        /// Saves a backup of the current level.
        /// </summary>
        public void SaveBackup()
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Beatmap can't autosave until you load a level.", 3f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(RTFile.BasePath, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"backup_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Saving backup...", 2f, EditorManager.NotificationType.Warning);

            GameData.Current?.SaveData(autosavePath);

            EditorManager.inst.DisplayNotification("Autosaved backup!", 2f, EditorManager.NotificationType.Success);

            autosaveTimer.Reset();
            autosaving = false;
        }

        /// <summary>
        /// Saves a backup of a level.
        /// </summary>
        /// <param name="levelPanel">Level to save the backup of.</param>
        public void SaveBackup(LevelPanel levelPanel)
        {
            if (EditorManager.inst.loading)
                return;

            autosaving = true;

            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Already attempting to save the beatmap!", 2f, EditorManager.NotificationType.Error);
                autosaving = false;
                return;
            }

            var autosavesDirectory = RTFile.CombinePaths(levelPanel.Path, "autosaves");
            var autosavePath = RTFile.CombinePaths(autosavesDirectory, $"backup_{DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss")}{FileFormat.LSB.Dot()}");

            RTFile.CreateDirectory(autosavesDirectory);

            EditorManager.inst.DisplayNotification("Saving backup...", 2f, EditorManager.NotificationType.Warning);

            RTFile.CopyFile(levelPanel.Item.GetFile(levelPanel.Item.CurrentFile), autosavePath);

            EditorManager.inst.DisplayNotification("Saved backup!", 2f, EditorManager.NotificationType.Success);

            autosaving = false;
        }

        /// <summary>
        /// Creates a new level and loads it.
        /// </summary>
        public void CreateNewLevel() => CreateNewLevel(() => LevelTemplateEditor.inst.CurrentTemplate.GetGameData(), newLevelSettings);

        /// <summary>
        /// Creates a new level and loads it.
        /// </summary>
        /// <param name="func">Custom GameData to create and assign to the new level.</param>
        /// <param name="newLevelSettings">Settings to apply to the new level.</param>
        public void CreateNewLevel(Func<GameData> func, NewLevelSettings newLevelSettings)
        {
            var newAudioPath = newLevelSettings.audioPath;
            var newLevelName = newLevelSettings.levelName;
            var newSongArtist = newLevelSettings.songArtist;
            var newSongTitle = newLevelSettings.songTitle;
            var newLevelDifficulty = newLevelSettings.difficulty;

            if (string.IsNullOrEmpty(newAudioPath))
            {
                EditorManager.inst.DisplayNotification("The file path is empty.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            if (!RTFile.FileExists(newAudioPath))
            {
                EditorManager.inst.DisplayNotification("The file you are trying to load doesn't appear to exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var audioFormat = RTFile.GetFileFormat(newAudioPath);
            if (!RTFile.ValidAudio(audioFormat))
            {
                EditorManager.inst.DisplayNotification($"The file you are trying to load doesn't appear to be a song file.\nDetected format: {audioFormat}", 6f, EditorManager.NotificationType.Error);
                return;
            }

            bool setNew = false;
            int num = 0;
            string p = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, newLevelName);
            while (RTFile.DirectoryExists(p))
            {
                p = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, newLevelName) + " - " + num.ToString();
                num += 1;
                setNew = true;

            }
            if (setNew)
                newLevelName += " - " + num.ToString();

            var path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, newLevelName);

            if (RTFile.DirectoryExists(path))
            {
                EditorManager.inst.DisplayNotification($"The level you are trying to create already exists.\nName: {Path.GetFileName(path)}", 4f, EditorManager.NotificationType.Error);
                return;
            }

            var gameData = func?.Invoke();

            if (!gameData)
                return;

            Directory.CreateDirectory(path);

            RTFile.CopyFile(newAudioPath, RTFile.CombinePaths(path, $"level{audioFormat.Dot()}"));

            gameData.SaveData(RTFile.CombinePaths(path, Level.LEVEL_LSB));
            var metaData = new MetaData();
            metaData.beatmap.gameVersion = ProjectArrhythmia.GameVersion.ToString();
            metaData.arcadeID = LSText.randomNumString(16);
            metaData.artist.name = newSongArtist;
            metaData.artist.link = newSongArtist;
            metaData.song.title = newSongTitle;
            metaData.song.difficulty = newLevelDifficulty;
            metaData.uploaderName = SteamWrapper.inst.user.displayName;
            metaData.creator.name = SteamWrapper.inst.user.displayName;
            metaData.creator.steamID = SteamWrapper.inst.user.id;
            metaData.beatmap.name = newLevelName;

            MetaData.Current = metaData;

            fromNewLevel = true;
            metaData.WriteToFile(RTFile.CombinePaths(path, Level.METADATA_LSB));

            var levelPanel = new LevelPanel();
            levelPanel.Init(new Level(path));
            LevelPanels.Add(levelPanel);
            LoadLevel(levelPanel);
            NewLevelPopup.Close();
        }

        /// <summary>
        /// Creates the default beatmap.
        /// </summary>
        /// <returns>Returns the base beatmap.</returns>
        public GameData CreateBaseBeatmap()
        {
            var gameData = new GameData();
            gameData.data = new BeatmapData();
            gameData.data.level = new LevelData()
            {
                limitPlayer = false,
            };
            gameData.data.checkpoints.Add(Checkpoint.Default);

            if (gameData.events == null)
                gameData.events = new List<List<EventKeyframe>>();
            gameData.events.Clear();
            GameData.ClampEventListValues(gameData.events);

            for (int i = 0; i < (Seasons.IsAprilFools ? 45 : 25); i++)
            {
                var backgroundObject = new BackgroundObject();
                backgroundObject.name = "bg - " + i;
                if (UnityEngine.Random.value > 0.5f)
                    backgroundObject.scale = new Vector2(UnityEngine.Random.Range(2, 8), UnityEngine.Random.Range(2, 8));
                else
                {
                    float num = UnityEngine.Random.Range(2, 6);
                    backgroundObject.scale = new Vector2(num, num);
                }
                backgroundObject.pos = new Vector2(UnityEngine.Random.Range(-48, 48), UnityEngine.Random.Range(-32, 32));
                backgroundObject.color = UnityEngine.Random.Range(1, 6);
                backgroundObject.depth = UnityEngine.Random.Range(0, 6);

                if (UnityEngine.Random.value > 0.5f)
                {
                    backgroundObject.reactiveType = (BackgroundObject.ReactiveType)UnityEngine.Random.Range(1, 5);
                    backgroundObject.reactiveScale = UnityEngine.Random.Range(0.01f, 0.04f);
                }

                if (Seasons.IsAprilFools)
                {
                    var randomShape = UnityEngine.Random.Range(0, ShapeManager.inst.Shapes3D.Count - 1);
                    if (randomShape != 4 && randomShape != 6)
                        backgroundObject.shape = randomShape;
                }

                gameData.backgroundObjects.Add(backgroundObject);
            }

            var beatmapObject = ObjectEditor.inst.CreateNewBeatmapObject(0.5f);
            beatmapObject.events[0].Add(new EventKeyframe(4f, new float[3] { 10f, 0f, 0f }, new float[3]));
            if (Seasons.IsAprilFools)
                beatmapObject.events[2].Add(new EventKeyframe(12f, new float[1] { 360000f }, new float[3]));

            beatmapObject.name = Seasons.IsAprilFools ? "trololololo" : RTEditor.DEFAULT_OBJECT_NAME;
            beatmapObject.autoKillType = AutoKillType.LastKeyframeOffset;
            beatmapObject.autoKillOffset = 4f;
            beatmapObject.editorData.Layer = 0;
            gameData.beatmapObjects.Add(beatmapObject);

            return gameData;
        }

        /// <summary>
        /// Creates a new level collection.
        /// </summary>
        /// <param name="levels">Levels to create from.</param>
        /// <returns>Returns a created level collection.</returns>
        public LevelCollection CreateNewLevelCollection(List<Level> levels = null)
        {
            var levelCollection = levels == null || levels.IsEmpty() ? new LevelCollection() : new LevelCollection(levels);
            levelCollection.name = "New Level Collection";
            levelCollection.dateCreated = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");
            levelCollection.path = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.CollectionsPath, "New Level Collection");
            return levelCollection;
        }

        void OpenLevelPopupOnFinish()
        {
            if (!EditorConfig.Instance.OpenNewLevelCreatorIfNoLevels.Value || !LevelPanels.IsEmpty())
            {
                OpenLevelPopup.Open();
                EditorManager.inst.RenderOpenBeatmapPopup();
            }
            else
                EditorManager.inst.OpenNewLevelPopup();
        }
        
        /// <summary>
        /// Refreshes the search and sort of the editor levels.
        /// </summary>
        public void RenderLevels() => CoroutineHelper.StartCoroutine(IRenderLevels());

        /// <summary>
        /// Refreshes the search and sort of the editor levels.
        /// </summary>
        public IEnumerator IRenderLevels()
        {
            CoreHelper.Log($"Level Search: {EditorManager.inst.openFileSearch}\nLevel Sort: { RTEditor.inst.levelAscend} - { RTEditor.inst.levelSort}");

            var levelPanels = LevelPanels;

            var currentLevelCollection = CurrentLevelCollection ?? OpenLevelCollection;
            if (!currentLevelCollection)
            {
                levelPanels = RTEditor.inst.levelSort switch
                {
                    LevelSort.Cover => LevelPanels.Order(x => x.Item && x.Item.icon != SteamWorkshop.inst.defaultSteamImageSprite, !RTEditor.inst.levelAscend),
                    LevelSort.Artist => LevelPanels.Order(x => x.Item?.metadata?.artist?.name ?? string.Empty, !RTEditor.inst.levelAscend),
                    LevelSort.Creator => LevelPanels.Order(x => x.Item?.metadata?.creator?.name ?? string.Empty, !RTEditor.inst.levelAscend),
                    LevelSort.File => LevelPanels.Order(x => x.Path, !RTEditor.inst.levelAscend),
                    LevelSort.Title => LevelPanels.Order(x => x.Item?.metadata?.song?.title ?? string.Empty, !RTEditor.inst.levelAscend),
                    LevelSort.Difficulty => LevelPanels.Order(x => x.Item?.metadata?.song?.difficulty ?? 0, !RTEditor.inst.levelAscend),
                    LevelSort.DateEdited => LevelPanels.Order(x => x.Item?.metadata?.beatmap?.dateEdited ?? string.Empty, !RTEditor.inst.levelAscend),
                    LevelSort.DateCreated => LevelPanels.Order(x => x.Item?.metadata?.beatmap?.dateCreated ?? string.Empty, !RTEditor.inst.levelAscend),
                    LevelSort.DatePublished => LevelPanels.Order(x => x.Item?.metadata?.beatmap?.datePublished ?? string.Empty, !RTEditor.inst.levelAscend),
                    _ => LevelPanels,
                };

                levelPanels = levelPanels.Order(x => x.isFolder, true); // folders should always be at the top.
            }
            else
            {
                levelPanels = levelPanels.Order(x => x.Item?.collectionInfo?.index ?? 0, true);
                levelPanels = levelPanels.Order(x => x.isFolder, true); // folders should always be at the top.
            }

            var content = OpenLevelPopup.Content;

            int num = 0;
            foreach (var levelPanel in levelPanels)
            {
                var folder = levelPanel.Path;
                var metadata = levelPanel.Item?.metadata;

                levelPanel.SetActive(levelPanel.isFolder ? RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) :
                    (RTString.SearchString(EditorManager.inst.openFileSearch, Path.GetFileName(folder)) ||
                        metadata == null || metadata != null &&
                        (RTString.SearchString(EditorManager.inst.openFileSearch,
                            metadata.song.title,
                            metadata.artist.name,
                            metadata.creator.name,
                            metadata.song.description,
                            LevelPanel.difficultyNames[Mathf.Clamp(metadata.song.difficulty, 0, LevelPanel.difficultyNames.Length - 1)]))));

                if (num >= 0 && num < content.childCount)
                    levelPanel.GameObject.transform.SetSiblingIndex(num);
                num++;
            }

            if (content.Find("add"))
            {
                yield return null;
                if (content.Find("add"))
                    content.Find("add").SetAsFirstSibling();
            }

            if (content.Find("back"))
            {
                yield return null;
                if (content.Find("back"))
                    content.Find("back").SetAsFirstSibling();
            }

            yield break;
        }

        public void RenderLevelCollections() => CoroutineHelper.StartCoroutine(IRenderLevelCollections());

        public IEnumerator IRenderLevelCollections()
        {
            var content = LevelCollectionPopup.Content;

            int num = 0;
            foreach (var levelCollectionPanel in LevelCollectionPanels)
            {
                var folder = levelCollectionPanel.Path;
                var levelCollection = levelCollectionPanel.Item;

                levelCollectionPanel.SetActive(levelCollectionPanel.isFolder ? RTString.SearchString(LevelCollectionPopup.SearchTerm, Path.GetFileName(folder)) :
                        RTString.SearchString(LevelCollectionPopup.SearchTerm,
                            Path.GetFileName(folder),
                            levelCollection.name,
                            levelCollection.creator,
                            levelCollection.description,
                            LevelPanel.difficultyNames[Mathf.Clamp(levelCollection.difficulty, 0, LevelPanel.difficultyNames.Length - 1)]));

                if (num >= 0 && num < content.childCount)
                    levelCollectionPanel.GameObject.transform.SetSiblingIndex(num);
                num++;
            }

            if (content.Find("add"))
            {
                yield return null;
                if (content.Find("add"))
                    content.Find("add").SetAsFirstSibling();
            }
            
            if (content.Find("back"))
            {
                yield return null;
                if (content.Find("back"))
                    content.Find("back").SetAsFirstSibling();
            }

            yield break;
        }

        /// <summary>
        /// Refreshes the editor file browsing.
        /// </summary>
        public void RefreshFileBrowserLevels() => RTFileBrowser.inst.UpdateBrowserFile(FileFormat.LSB.Dot(), "level", x => LoadLevel(new Level(RTFile.ReplaceSlash(x).Remove("/" + Level.LEVEL_LSB))));

        /// <summary>
        /// Refreshes the autosaves and backups of a level.
        /// </summary>
        /// <param name="levelPanel">Level to get autosaves from.</param>
        public void RefreshAutosaveList(LevelPanel levelPanel)
        {
            RTEditor.inst.AutosavePopup.SearchField.onValueChanged.NewListener(_val => RefreshAutosaveList(levelPanel));

            RTEditor.inst.AutosavePopup.ClearContent();

            if (levelPanel.isFolder)
            {
                EditorManager.inst.DisplayNotification("Folders can't have autosaves / backups.", 1.5f, EditorManager.NotificationType.Warning);
                return;
            }

            var files =
                Directory.GetFiles(levelPanel.Path, $"autosave_{FileFormat.LSB.ToPattern()}", SearchOption.AllDirectories)
                .Union(Directory.GetFiles(levelPanel.Path, $"backup_{FileFormat.LSB.ToPattern()}", SearchOption.AllDirectories));

            foreach (var file in files)
            {
                if (!RTString.SearchString(RTEditor.inst.AutosavePopup.SearchTerm, Path.GetFileName(file)))
                    continue;

                var path = RTFile.ReplaceSlash(file);
                var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.AutosavePopup.Content, $"Folder [{Path.GetFileName(file)}]");
                var folderButtonStorage = gameObject.GetComponent<FunctionButtonStorage>();
                folderButtonStorage.button.onClick.ClearAll();

                string tmpFile = path;

                var contextMenu = gameObject.AddComponent<FolderButtonFunction>();
                contextMenu.onClick = eventData =>
                {
                    switch (eventData.button)
                    {
                        // just realized I could collapse the switch case blocks like this
                        case PointerEventData.InputButton.Left: {
                                levelPanel.Item.currentFile = tmpFile.Remove(RTFile.AppendEndSlash(levelPanel.Path));

                                LoadLevel(levelPanel);
                                OpenLevelPopup.Close();
                                break;
                            }
                        case PointerEventData.InputButton.Right: {
                                EditorContextMenu.inst.ShowContextMenu(
                                    new ButtonFunction("Open", () =>
                                    {
                                        levelPanel.Item.currentFile = tmpFile.Remove(RTFile.AppendEndSlash(levelPanel.Path));

                                        LoadLevel(levelPanel);
                                        OpenLevelPopup.Close();
                                    }),
                                    new ButtonFunction("Toggle Backup State", () =>
                                    {
                                        var fi = new FileInfo(tmpFile);

                                        tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                                        if (fi.Exists)
                                            fi.MoveTo(tmpFile);

                                        RefreshAutosaveList(levelPanel);
                                    }, "Autosave Toggle Backup State"),
                                    new ButtonFunction("Delete", () =>
                                    {
                                        RTEditor.inst.ShowWarningPopup("Are you sure you want to delete this autosave? This is permanent!", () =>
                                        {
                                            RTFile.DeleteFile(tmpFile);
                                            RefreshAutosaveList(levelPanel);
                                        }, RTEditor.inst.HideWarningPopup);
                                    })
                                    );
                                break;
                            }
                    }
                };

                var hoverUI = gameObject.AddComponent<HoverUI>();
                hoverUI.size = EditorConfig.Instance.OpenLevelButtonHoverSize.Value;
                hoverUI.animatePos = false;
                hoverUI.animateSca = true;

                folderButtonStorage.label.text = Path.GetFileName(file);

                var backup = EditorPrefabHolder.Instance.Function1Button.Duplicate(gameObject.transform, "backup");
                var backupHolder = backup.GetComponent<FunctionButtonStorage>();
                backup.transform.localScale = Vector3.one;
                UIManager.SetRectTransform(backup.transform.AsRT(), new Vector2(450f, 0f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(80f, 28f));
                backupHolder.label.text = "Backup";
                backupHolder.button.onClick.NewListener(() =>
                {
                    var fi = new FileInfo(tmpFile);

                    tmpFile = tmpFile.Contains("autosave_") ? tmpFile.Replace("autosave_", "backup_") : tmpFile.Replace("backup_", "autosave_");

                    if (fi.Exists)
                        fi.MoveTo(tmpFile);

                    RefreshAutosaveList(levelPanel);
                });

                TooltipHelper.AssignTooltip(backup, "Autosave Toggle Backup State");

                EditorThemeManager.ApplySelectable(folderButtonStorage.button, ThemeGroup.List_Button_1);
                EditorThemeManager.ApplyGraphic(backupHolder.button.image, ThemeGroup.Function_1, true);
                EditorThemeManager.ApplyGraphic(backupHolder.label, ThemeGroup.Function_1_Text);
            }
        }

        /// <summary>
        /// Renders the new level difficulty.
        /// </summary>
        public void RenderNewLevelDifficulty()
        {
            CoreHelper.DestroyChildren(NewLevelPopup.DifficultyContent);

            var values = CustomEnumHelper.GetValues<DifficultyType>();
            var count = values.Length - 1;

            foreach (var difficulty in values)
            {
                if (difficulty.Ordinal < 0) // skip unknown difficulty
                    continue;

                var gameObject = RTMetaDataEditor.inst.difficultyToggle.Duplicate(NewLevelPopup.DifficultyContent, difficulty.DisplayName.ToLower(), difficulty == count - 1 ? 0 : difficulty + 1);
                gameObject.transform.localScale = Vector3.one;

                gameObject.transform.AsRT().sizeDelta = new Vector2(69f, 32f);

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.Color);
                text.text = difficulty == count - 1 ? "Anim" : difficulty.DisplayName;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.Color;
                toggle.group = null;
                toggle.SetIsOnWithoutNotify(newLevelSettings.difficulty == difficulty);
                toggle.onValueChanged.NewListener(_val =>
                {
                    newLevelSettings.difficulty = difficulty;
                    RenderNewLevelDifficulty();
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);
            }
        }

        public void OpenLevelCollectionEditor(LevelCollection levelCollection)
        {
            if (!levelCollection)
                return;

            LevelCollectionDialog.Open();
            RenderLevelCollectionEditor(levelCollection);
        }

        public void RenderLevelCollectionEditor(LevelCollection levelCollection)
        {
            LevelCollectionDialog.NameField.SetTextWithoutNotify(levelCollection.name);
            LevelCollectionDialog.NameField.onValueChanged.NewListener(_val =>
            {
                levelCollection.name = _val;
                levelCollection.editorPanel?.RenderLabel();
            });
            LevelCollectionDialog.NameField.onEndEdit.NewListener(_val =>
            {
                var oldPath = levelCollection.path;
                var path = levelCollection.path;
                path = RTFile.GetDirectory(RTFile.RemoveEndSlash(path));
                path = RTFile.CombinePaths(path, RTFile.ValidateDirectory(_val));

                RTFile.MoveDirectory(oldPath, path);
                levelCollection.path = path;
                levelCollection.Save();
            });

            LevelCollectionDialog.DescriptionField.SetTextWithoutNotify(levelCollection.description);
            LevelCollectionDialog.DescriptionField.onValueChanged.NewListener(_val => levelCollection.description = _val);

            LevelCollectionDialog.CreatorField.SetTextWithoutNotify(levelCollection.creator);
            LevelCollectionDialog.CreatorField.onValueChanged.NewListener(_val => levelCollection.creator = _val);

            LevelCollectionDialog.IconImage.sprite = levelCollection.icon;

            LevelCollectionDialog.CollapseIcon(CollapseIcon);
            LevelCollectionDialog.SelectIconButton.onClick.NewListener(() => OpenIconSelector(levelCollection));
            LevelCollectionDialog.CollapseIconToggle.SetIsOnWithoutNotify(CollapseIcon);
            LevelCollectionDialog.CollapseIconToggle.onValueChanged.NewListener(_val =>
            {
                LevelCollectionDialog.CollapseIcon(_val);
                CollapseIcon = _val;
            });

            LevelCollectionDialog.BannerImage.sprite = levelCollection.banner;

            LevelCollectionDialog.CollapseBanner(CollapseBanner);
            LevelCollectionDialog.SelectBannerButton.onClick.NewListener(() => OpenBannerSelector(levelCollection));
            LevelCollectionDialog.CollapseBannerToggle.SetIsOnWithoutNotify(CollapseBanner);
            LevelCollectionDialog.CollapseBannerToggle.onValueChanged.NewListener(_val =>
            {
                LevelCollectionDialog.CollapseBanner(_val);
                CollapseBanner = _val;
            });

            LevelCollectionDialog.VersionField.SetTextWithoutNotify(levelCollection.ObjectVersion);
            LevelCollectionDialog.VersionField.onValueChanged.NewListener(_val => levelCollection.ObjectVersion = _val);
            LevelCollectionDialog.VersionField.onEndEdit.NewListener(_val => RenderLevelCollectionEditor(levelCollection));
            EditorContextMenu.inst.AddContextMenu(LevelCollectionDialog.VersionField.gameObject, EditorContextMenu.GetObjectVersionFunctions(levelCollection, () => RenderLevelCollectionEditor(levelCollection)));

            LevelCollectionDialog.AllowZenProgressionToggle.SetIsOnWithoutNotify(levelCollection.allowZenProgression);
            LevelCollectionDialog.AllowZenProgressionToggle.onValueChanged.NewListener(_val => levelCollection.allowZenProgression = _val);

            LevelCollectionDialog.ViewLevelsButton.onClick.NewListener(() => LoadLevelCollection(levelCollection));
            LevelCollectionDialog.SaveButton.onClick.NewListener(() =>
            {
                levelCollection.Save();
                EditorManager.inst.DisplayNotification($"Saved level collection!", 2f, EditorManager.NotificationType.Success);
            });

            LevelCollectionDialog.ArcadeIDText.text = !string.IsNullOrEmpty(levelCollection.id) ? $"Arcade ID: {levelCollection.id} (Click to copy)" : "Arcade ID: No ID";
            LevelCollectionDialog.ArcadeIDContextMenu.onClick = eventData =>
            {
                if (string.IsNullOrEmpty(levelCollection.id))
                {
                    EditorManager.inst.DisplayNotification($"No ID assigned. This shouldn't happen. Did something break?", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                LSText.CopyToClipboard(levelCollection.id);
                EditorManager.inst.DisplayNotification($"Copied ID: {levelCollection.id} to your clipboard!", 1.5f, EditorManager.NotificationType.Success);
            };

            RenderLevelCollectionDifficulty(levelCollection);
            EditorServerManager.inst.RenderTagDialog(levelCollection, LevelCollectionDialog, EditorServerManager.DefaultTagRelation.Level, () => levelCollection.Save());
            EditorServerManager.inst.RenderServerDialog(
                uploadable: levelCollection,
                dialog: LevelCollectionDialog,
                upload: () => UploadLevelCollection(levelCollection),
                pull: () => PullLevelCollection(levelCollection),
                delete: () => DeleteLevelCollectionFromServer(levelCollection),
                verify: null);
        }

        static Vector2 difficultySize = new Vector2(100f, 32f);
        public void RenderLevelCollectionDifficulty(LevelCollection levelCollection)
        {
            LSHelpers.DeleteChildren(LevelCollectionDialog.DifficultyContent);

            var values = CustomEnumHelper.GetValues<DifficultyType>();
            var count = values.Length - 1;

            foreach (var difficulty in values)
            {
                if (difficulty.Ordinal < 0) // skip unknown difficulty
                    continue;

                var gameObject = RTMetaDataEditor.inst.difficultyToggle.Duplicate(LevelCollectionDialog.DifficultyContent, difficulty.DisplayName.ToLower(), difficulty == count - 1 ? 0 : difficulty + 1);
                gameObject.transform.localScale = Vector3.one;

                gameObject.transform.AsRT().sizeDelta = difficultySize;

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.Color);
                text.text = difficulty == count - 1 ? "Anim" : difficulty.DisplayName;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.Color;
                toggle.group = null;
                toggle.SetIsOnWithoutNotify(levelCollection.Difficulty == difficulty);
                toggle.onValueChanged.NewListener(_val =>
                {
                    levelCollection.Difficulty = difficulty;
                    levelCollection.Save();
                    RenderLevelCollectionDifficulty(levelCollection);
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);
            }
        }

        public void OpenIconSelector(LevelCollection levelCollection)
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            CoreHelper.Log("Selected file: " + jpgFile);
            if (string.IsNullOrEmpty(jpgFile))
                return;

            string jpgFileLocation = RTFile.CombinePaths(levelCollection.path, LevelCollection.ICON_JPG);
            CoroutineHelper.StartCoroutine(EditorManager.inst.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), cover =>
            {
                RTFile.CopyFile(jpgFile, jpgFileLocation);
                SetLevelCollectionIcon(levelCollection, cover);
            }, errorFile => EditorManager.inst.DisplayNotification("Please resize your image to be less than or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error)));
        }

        public void OpenIconSelector(LevelInfo levelInfo, Action onSubmit = null)
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            CoreHelper.Log("Selected file: " + jpgFile);
            if (string.IsNullOrEmpty(jpgFile))
                return;

            CoroutineHelper.StartCoroutine(EditorManager.inst.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(512f, 512f)), cover =>
            {
                levelInfo.icon = cover;
                RenderLevelInfoEditor(levelInfo, onSubmit);
            }, errorFile => EditorManager.inst.DisplayNotification("Please resize your image to be less than or equal to 512 x 512 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error)));
        }

        public void SetLevelCollectionIcon(LevelCollection levelCollection, Sprite sprite)
        {
            if (!levelCollection)
                return;

            levelCollection.icon = sprite;
            LevelCollectionDialog.IconImage.sprite = levelCollection.icon;
            levelCollection.Save();
        }

        public void OpenBannerSelector(LevelCollection levelCollection)
        {
            string jpgFile = FileBrowser.OpenSingleFile("jpg");
            CoreHelper.Log("Selected file: " + jpgFile);
            if (string.IsNullOrEmpty(jpgFile))
                return;

            string jpgFileLocation = RTFile.CombinePaths(levelCollection.path, LevelCollection.BANNER_JPG);
            CoroutineHelper.StartCoroutine(EditorManager.inst.GetSprite(jpgFile, new EditorManager.SpriteLimits(new Vector2(900f, 300f)), cover =>
            {
                RTFile.CopyFile(jpgFile, jpgFileLocation);
                SetLevelCollectionBanner(levelCollection, cover);
            }, errorFile => EditorManager.inst.DisplayNotification("Please resize your image to be less than or equal to 900 x 300 pixels. It must also be a jpg.", 2f, EditorManager.NotificationType.Error)));
        }

        public void SetLevelCollectionBanner(LevelCollection levelCollection, Sprite sprite)
        {
            if (levelCollection)
            {
                levelCollection.banner = sprite;
                LevelCollectionDialog.BannerImage.sprite = levelCollection.banner;
                levelCollection.Save();
            }
        }

        public void OpenLevelInfoEditor(LevelInfo levelInfo, Action onSubmit = null)
        {
            if (!levelInfo)
                return;

            LevelInfoDialog.Open();
            RenderLevelInfoEditor(levelInfo, onSubmit);
        }

        public void RenderLevelInfoEditor(LevelInfo levelInfo, Action onSubmit = null)
        {
            LevelInfoDialog.PathField.SetTextWithoutNotify(levelInfo.path);
            LevelInfoDialog.PathField.onValueChanged.NewListener(_val => levelInfo.path = _val);
            LevelInfoDialog.PathField.onEndEdit.NewListener(_val =>
            {
                levelInfo.collection?.Save();

                LoadLevelCollections();
                LoadLevels();
            });

            LevelInfoDialog.EditorPathField.SetTextWithoutNotify(levelInfo.editorPath);
            LevelInfoDialog.EditorPathField.onValueChanged.NewListener(_val => levelInfo.editorPath = _val);
            LevelInfoDialog.EditorPathField.onEndEdit.NewListener(_val =>
            {
                levelInfo.collection?.Save();

                LoadLevelCollections();
                LoadLevels();
            });

            LevelInfoDialog.SongArtistField.SetTextWithoutNotify(levelInfo.songArtist);
            LevelInfoDialog.SongArtistField.onValueChanged.NewListener(_val => levelInfo.songArtist = _val);
            LevelInfoDialog.SongArtistField.onEndEdit.NewListener(_val => levelInfo.collection?.Save());
            
            LevelInfoDialog.SongTitleField.SetTextWithoutNotify(levelInfo.songTitle);
            LevelInfoDialog.SongTitleField.onValueChanged.NewListener(_val => levelInfo.songTitle = _val);
            LevelInfoDialog.SongTitleField.onEndEdit.NewListener(_val => levelInfo.collection?.Save());

            LevelInfoDialog.NameField.SetTextWithoutNotify(levelInfo.name);
            LevelInfoDialog.NameField.onValueChanged.NewListener(_val => levelInfo.name = _val);
            LevelInfoDialog.NameField.onEndEdit.NewListener(_val => levelInfo.collection?.Save());

            LevelInfoDialog.CreatorField.SetTextWithoutNotify(levelInfo.creator);
            LevelInfoDialog.CreatorField.onValueChanged.NewListener(_val => levelInfo.creator = _val);
            LevelInfoDialog.CreatorField.onEndEdit.NewListener(_val => levelInfo.collection?.Save());

            RenderLevelInfoDifficulty(levelInfo);

            LevelInfoDialog.ArcadeIDField.SetTextWithoutNotify(levelInfo.arcadeID);
            LevelInfoDialog.ArcadeIDField.onValueChanged.NewListener(_val => levelInfo.arcadeID = _val);
            LevelInfoDialog.ArcadeIDField.onEndEdit.NewListener(_val => levelInfo.collection?.Save());

            LevelInfoDialog.ServerIDField.SetTextWithoutNotify(levelInfo.serverID);
            LevelInfoDialog.ServerIDField.onValueChanged.NewListener(_val => levelInfo.serverID = _val);
            LevelInfoDialog.ServerIDField.onEndEdit.NewListener(_val => levelInfo.collection?.Save());

            LevelInfoDialog.WorkshopIDField.SetTextWithoutNotify(levelInfo.workshopID);
            LevelInfoDialog.WorkshopIDField.onValueChanged.NewListener(_val => levelInfo.workshopID = _val);
            LevelInfoDialog.WorkshopIDField.onEndEdit.NewListener(_val => levelInfo.collection?.Save());

            LevelInfoDialog.IconImage.sprite = levelInfo.icon ?? LegacyPlugin.AtanPlaceholder;

            LevelInfoDialog.CollapseIcon(LevelInfoCollapseIcon);
            LevelInfoDialog.SelectIconButton.onClick.NewListener(() => OpenIconSelector(levelInfo, onSubmit));
            LevelInfoDialog.CollapseIconToggle.SetIsOnWithoutNotify(LevelInfoCollapseIcon);
            LevelInfoDialog.CollapseIconToggle.onValueChanged.NewListener(_val =>
            {
                LevelInfoDialog.CollapseIcon(_val);
                LevelInfoCollapseIcon = _val;
            });

            LevelInfoDialog.UnlockRequiredToggle.SetIsOnWithoutNotify(levelInfo.requireUnlock);
            LevelInfoDialog.UnlockRequiredToggle.onValueChanged.NewListener(_val =>
            {
                levelInfo.requireUnlock = _val;
                levelInfo.collection?.Save();
            });

            LevelInfoDialog.OverwriteUnlockRequiredToggle.SetIsOnWithoutNotify(levelInfo.overwriteRequireUnlock);
            LevelInfoDialog.OverwriteUnlockRequiredToggle.onValueChanged.NewListener(_val =>
            {
                levelInfo.overwriteRequireUnlock = _val;
                levelInfo.collection?.Save();
            });

            LevelInfoDialog.UnlockCompletionToggle.SetIsOnWithoutNotify(levelInfo.unlockAfterCompletion);
            LevelInfoDialog.UnlockCompletionToggle.onValueChanged.NewListener(_val =>
            {
                levelInfo.unlockAfterCompletion = _val;
                levelInfo.collection?.Save();
            });

            LevelInfoDialog.OverwriteUnlockCompletionToggle.SetIsOnWithoutNotify(levelInfo.overwriteUnlockAfterCompletion);
            LevelInfoDialog.OverwriteUnlockCompletionToggle.onValueChanged.NewListener(_val =>
            {
                levelInfo.overwriteUnlockAfterCompletion = _val;
                levelInfo.collection?.Save();
            });

            LevelInfoDialog.HiddenToggle.SetIsOnWithoutNotify(levelInfo.hidden);
            LevelInfoDialog.HiddenToggle.onValueChanged.NewListener(_val =>
            {
                levelInfo.hidden = _val;
                levelInfo.collection?.Save();
            });

            LevelInfoDialog.ShowAfterUnlockToggle.SetIsOnWithoutNotify(levelInfo.showAfterUnlock);
            LevelInfoDialog.ShowAfterUnlockToggle.onValueChanged.NewListener(_val =>
            {
                levelInfo.showAfterUnlock = _val;
                levelInfo.collection?.Save();
            });

            LevelInfoDialog.SkipToggle.SetIsOnWithoutNotify(levelInfo.skip);
            LevelInfoDialog.SkipToggle.onValueChanged.NewListener(_val =>
            {
                levelInfo.skip = _val;
                levelInfo.collection?.Save();
            });

            LevelInfoDialog.SubmitButton.gameObject.SetActive(onSubmit != null);
            LevelInfoDialog.SubmitButton.onClick.NewListener(() => onSubmit?.Invoke());
        }

        public void RenderLevelInfoDifficulty(LevelInfo levelInfo)
        {
            LSHelpers.DeleteChildren(LevelInfoDialog.DifficultyContent);

            var values = CustomEnumHelper.GetValues<DifficultyType>();
            var count = values.Length - 1;

            foreach (var difficulty in values)
            {
                if (difficulty.Ordinal < 0) // skip unknown difficulty
                    continue;

                var gameObject = RTMetaDataEditor.inst.difficultyToggle.Duplicate(LevelInfoDialog.DifficultyContent, difficulty.DisplayName.ToLower(), difficulty == count - 1 ? 0 : difficulty + 1);
                gameObject.transform.localScale = Vector3.one;

                gameObject.transform.AsRT().sizeDelta = difficultySize;

                var text = gameObject.transform.Find("Background/Text").GetComponent<Text>();
                text.color = LSColors.ContrastColor(difficulty.Color);
                text.text = difficulty == count - 1 ? "Anim" : difficulty.DisplayName;
                text.fontSize = 17;
                var toggle = gameObject.GetComponent<Toggle>();
                toggle.image.color = difficulty.Color;
                toggle.group = null;
                toggle.SetIsOnWithoutNotify(levelInfo.DifficultyType == difficulty);
                toggle.onValueChanged.NewListener(_val =>
                {
                    levelInfo.DifficultyType = difficulty;
                    RenderLevelInfoDifficulty(levelInfo);
                });

                EditorThemeManager.ApplyGraphic(toggle.image, ThemeGroup.Null, true);
                EditorThemeManager.ApplyGraphic(toggle.graphic, ThemeGroup.Background_1);
            }
        }

        #region Functions

        /// <summary>
        /// Converts a level to VG format and outputs it to the exports folder.
        /// </summary>
        /// <param name="levelPanel">Level to convert to VG.</param>
        public void ConvertLevel(LevelPanel levelPanel)
        {
            if (levelPanel.isFolder)
                return;

            var currentPath = levelPanel.Path;
            var fileName = levelPanel.Item.FolderName;

            var exportPath = EditorConfig.Instance.ConvertLevelLSToVGExportPath.Value;

            if (string.IsNullOrEmpty(exportPath))
            {
                var output = RTFile.CombinePaths(RTFile.ApplicationDirectory, RTEditor.DEFAULT_EXPORTS_PATH);
                RTFile.CreateDirectory(output);
                exportPath = output + "/";
            }

            exportPath = RTFile.AppendEndSlash(exportPath);

            if (!RTFile.DirectoryExists(Path.GetDirectoryName(exportPath)))
            {
                EditorManager.inst.DisplayNotification("Directory does not exist.", 2f, EditorManager.NotificationType.Error);
                return;
            }

            var gamedata = GameData.ReadFromFile(RTFile.CombinePaths(currentPath, Level.LEVEL_LSB), ArrhythmiaType.LS);
            var metadata = MetaData.ReadFromFile(RTFile.CombinePaths(currentPath, Level.METADATA_LSB), ArrhythmiaType.LS);

            var vgd = gamedata.ToJSONVG();

            var vgm = metadata.ToJSONVG();

            var path = exportPath + fileName;

            RTFile.CreateDirectory(path);

            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_OGG), RTFile.CombinePaths(path, Level.AUDIO_OGG));
            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_WAV), RTFile.CombinePaths(path, Level.AUDIO_WAV));
            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_MP3), RTFile.CombinePaths(path, Level.AUDIO_MP3));

            RTFile.CopyFile(RTFile.CombinePaths(currentPath, Level.LEVEL_JPG), RTFile.CombinePaths(path, Level.COVER_JPG));

            try
            {
                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.METADATA_VGM), vgm.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (MetaData) {ex}");
            }

            try
            {

                RTFile.WriteToFile(RTFile.CombinePaths(path, Level.LEVEL_VGD), vgd.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MetadataEditor.inst.className}Convert to VG error (GameData) {ex}");
            }

            EditorManager.inst.DisplayNotification($"Converted Level \"{fileName}\" from LS format to VG format and saved to {Path.GetFileName(path)}.", 4f,
                EditorManager.NotificationType.Success);

            AchievementManager.inst.UnlockAchievement("time_machine");
        }

        /// <summary>
        /// Zips a level folder.
        /// </summary>
        /// <param name="levelPanel">Level to zip.</param>
        public void ZipLevel(LevelPanel levelPanel)
        {
            EditorManager.inst.DisplayNotification($"Zipping {Path.GetFileName(RTFile.RemoveEndSlash(levelPanel.Path))}...", 2f, EditorManager.NotificationType.Warning);

            IZipLevel(levelPanel).StartAsync();
        }

        /// <summary>
        /// Zips a level folder.
        /// </summary>
        /// <param name="levelPanel">Level to zip.</param>
        public IEnumerator IZipLevel(LevelPanel levelPanel)
        {
            var currentPath = levelPanel.Path;
            bool failed;
            var zipPath = RTFile.RemoveEndSlash(currentPath) + FileFormat.ZIP.Dot();
            try
            {
                RTFile.DeleteFile(zipPath);

                System.IO.Compression.ZipFile.CreateFromDirectory(currentPath, zipPath);

                failed = false;
            }
            catch (Exception ex)
            {
                failed = true;
                CoreHelper.LogException(ex);
            }

            yield return Ninja.JumpToUnity;
            if (failed)
                EditorManager.inst.DisplayNotification($"Had an error with zipping the folder. Check the logs!", 2f, EditorManager.NotificationType.Error);
            else
                EditorManager.inst.DisplayNotification($"Successfully zipped the folder to {Path.GetFileName(zipPath)}!", 2f, EditorManager.NotificationType.Success);
            yield break;
        }

        /// <summary>
        /// Moves the folder to a recycling folder.
        /// </summary>
        /// <param name="level">Level to remove.</param>
        public void RecycleLevel(LevelPanel levelPanel)
        {
            var folderPath = levelPanel.Path;
            var recyclingPath = RTFile.CombinePaths(RTFile.ApplicationDirectory, "recycling");
            RTFile.CreateDirectory(recyclingPath);
            RTFile.MoveDirectory(folderPath, RTFile.CombinePaths(recyclingPath, Path.GetFileName(folderPath)));
            LoadLevels();
        }

        public GameData combinedGameData;

        /// <summary>
        /// Combines all selected editor levels into one.
        /// </summary>
        /// <param name="savePath">Path to save the level to.</param>
        public void Combine(string savePath, Action onCombined = null) => Combine(savePath, LevelPanels.Where(x => x.Selected && x.Item && RTFile.FileExists(x.Item.GetFile(x.Item.CurrentFile))), onCombined);

        /// <summary>
        /// Combines editor levels into one.
        /// </summary>
        /// <param name="savePath">Path to save the level to.</param>
        /// <param name="selected">Editor levels to combine.</param>
        public void Combine(string savePath, IEnumerable<LevelPanel> selected, Action onCombined = null)
        {
            var combineList = new List<GameData>();

            foreach (var levelPanel in selected)
            {
                Debug.Log($"{EditorManager.inst.className}Parsing GameData from {levelPanel.Item.FolderName}");
                combineList.Add(levelPanel.Item.LoadGameData());
            }

            Debug.Log($"{EditorManager.inst.className}Can Combine: {combineList.Count > 0 && !string.IsNullOrEmpty(savePath)}" +
                $"\nGameData Count: {combineList.Count}" +
                $"\nSavePath: {savePath}");

            if (combineList.Count < 2)
            {
                EditorManager.inst.DisplayNotification("More than one level needs to be selected.", 1f, EditorManager.NotificationType.Error);
                return;
            }

            if (string.IsNullOrEmpty(savePath))
            {
                EditorManager.inst.DisplayNotification("Cannot combine with an empty path!", 1f, EditorManager.NotificationType.Error);
                return;
            }

            var combinedGameData = GameData.Combiner.Combine(combineList.ToArray());
            this.combinedGameData = combinedGameData;

            var levelFile = EditorConfig.Instance.CombinerOutputFormat.Value switch
            {
                ArrhythmiaType.LS => Level.LEVEL_LSB,
                ArrhythmiaType.VG => Level.LEVEL_VGD,
                _ => "",
            };

            string save = savePath;
            if (!save.Contains(levelFile) && save.LastIndexOf('/') == save.Length - 1)
                save += levelFile;
            else if (!save.Contains("/" + levelFile))
                save += "/" + levelFile;

            if (!save.Contains(RTEditor.inst.BeatmapsPath) && !save.Contains(RTEditor.inst.EditorPath))
                save = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, RTEditor.inst.EditorPath, save);
            else if (!save.Contains(RTEditor.inst.BeatmapsPath))
                save = RTFile.CombinePaths(RTEditor.inst.BeatmapsPath, save);
            else if (!save.Contains(RTEditor.inst.BasePath))
                save = RTFile.CombinePaths(RTEditor.inst.BasePath, save);

            foreach (var levelPanel in selected)
            {
                var file = levelPanel.Item.GetFile(levelPanel.Item.CurrentFile);
                if (!RTFile.FileExists(file))
                    return;

                var directory = Path.GetDirectoryName(save);
                RTFile.CreateDirectory(directory);

                var files1 = Directory.GetFiles(Path.GetDirectoryName(file));

                foreach (var file2 in files1)
                {
                    string dir = Path.GetDirectoryName(file2);
                    RTFile.CreateDirectory(dir);

                    var copyTo = file2.Replace(Path.GetDirectoryName(file), directory);
                    if (EditorConfig.Instance.CombinerOutputFormat.Value == ArrhythmiaType.VG)
                        copyTo = copyTo
                            .Replace(Level.LEVEL_OGG, Level.AUDIO_OGG)
                            .Replace(Level.LEVEL_WAV, Level.AUDIO_WAV)
                            .Replace(Level.LEVEL_MP3, Level.AUDIO_MP3)
                            .Replace(Level.LEVEL_JPG, Level.COVER_JPG)
                            ;

                    var fileName = Path.GetFileName(file2);
                    if (fileName != Level.LEVEL_LSB && fileName != Level.LEVEL_VGD && fileName != Level.METADATA_LSB && fileName != Level.METADATA_VGM && !RTFile.FileExists(copyTo))
                        File.Copy(file2, copyTo);
                }
            }

            if (EditorConfig.Instance.CombinerOutputFormat.Value == ArrhythmiaType.LS)
            {
                selected.First().Item.metadata?.WriteToFile(save.Replace(Level.LEVEL_LSB, Level.METADATA_LSB));

                combinedGameData.SaveData(save, () =>
                {
                    EditorManager.inst.DisplayNotification($"Combined {RTString.ArrayToString(selected.Select(x => x.Name).ToArray())} to {savePath} in the LS format!", 3f, EditorManager.NotificationType.Success);
                    onCombined?.Invoke();
                });
            }
            else
            {
                selected.First().Item.metadata?.WriteToFileVG(save.Replace(Level.LEVEL_VGD, Level.METADATA_VGM).Replace(Level.LEVEL_LSB, Level.METADATA_VGM));

                combinedGameData.SaveDataVG(save.Replace(FileFormat.LSB.Dot(), FileFormat.VGD.Dot()), () =>
                {
                    EditorManager.inst.DisplayNotification($"Combined {RTString.ArrayToString(selected.Select(x => x.Name).ToArray())} to {savePath} in the VG format!", 3f, EditorManager.NotificationType.Success);
                    onCombined?.Invoke();
                });
            }
        }

        /// <summary>
        /// Checks if a level has been loaded. If the user is currently not in a level, a notification will display.
        /// </summary>
        /// <returns>Returns true if a level was loaded, otherwise returns false.</returns>
        public bool HasLoadedLevel()
        {
            if (!EditorManager.inst.hasLoadedLevel)
                EditorManager.inst.DisplayNotification($"Load a level first!", 2f, EditorManager.NotificationType.Warning);
            return EditorManager.inst.hasLoadedLevel;
        }
        
        public void UploadLevelCollection(LevelCollection levelCollection)
        {
            if (!levelCollection)
                return;

            EditorServerManager.inst.Upload(
                url: AlephNetwork.LevelCollectionURL,
                fileName: Path.GetFileName(levelCollection.path),
                uploadable: levelCollection,
                transfer: tempDirectory =>
                {
                    var directory = levelCollection.path;
                    var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        if (!EditorServerManager.inst.VerifyFile(Path.GetFileName(file)))
                            continue;

                        var copyTo = file.Replace(directory, tempDirectory);

                        var dir = RTFile.GetDirectory(copyTo);

                        RTFile.CreateDirectory(dir);
                        RTFile.CopyFile(file, copyTo);
                    }
                },
                saveFile: () =>
                {
                    levelCollection.Save();
                },
                onUpload: () =>
                {
                    RenderLevelCollectionEditor(levelCollection);
                });
        }

        public void DeleteLevelCollectionFromServer(LevelCollection levelCollection)
        {
            if (!levelCollection)
                return;

            EditorServerManager.inst.Delete(
                url: AlephNetwork.LevelCollectionURL,
                uploadable: levelCollection,
                saveFile: () =>
                {
                    levelCollection.Save();
                },
                onDelete: () =>
                {
                    RenderLevelCollectionEditor(levelCollection);
                });
        }

        // todo: implement
        public void PullLevelCollection(LevelCollection levelCollection)
        {
            if (EditorManager.inst.savingBeatmap)
            {
                EditorManager.inst.DisplayNotification("Cannot pull level collection from the Arcade server because the level is saving!", 3f, EditorManager.NotificationType.Warning);
                return;
            }

            EditorServerManager.inst.Pull(
                url: AlephNetwork.LevelCollectionURL,
                uploadable: levelCollection,
                pull: jn => EditorServerManager.inst.DownloadLevelCollection(jn["id"], RTFile.RemoveEndSlash(levelCollection.path), jn["name"]));
        }

        #endregion

        #region Story Development

        string storyLevelsCompilerPath = "C:/Users/Mecha/Documents/Project Arrhythmia/Unity/BetterLegacyEditor/Assets/Story Levels";
        string storyLevelsVersionControlPath = "C:/Users/Mecha/Documents/Project Arrhythmia/BetterLegacy.Story";

        public void ToStoryLevel() => ToStoryLevel(RTEditor.inst.editorInfo);

        public void ToStoryLevel(EditorInfo editorInfo)
        {
            try
            {
                if (!editorInfo.isStory)
                    return;

                string cutsceneDestination = string.Empty;
                if (editorInfo.cutsceneDestination != Story.CutsceneDestination.Level)
                    cutsceneDestination = editorInfo.cutsceneDestination.ToString().ToLower() + "_cutscene";
                int cutscene = 0;
                if (editorInfo.cutscene >= 0)
                    cutscene = editorInfo.cutscene;

                ToStoryLevel(editorInfo.storyChapter, editorInfo.storyLevel, cutsceneDestination, cutscene);
            }
            catch { }
        }

        public void ToStoryLevel(int chapter, int level, string type = "", int cutscene = 0)
        {
            var path = RTFile.BasePath;
            var doc = $"doc{RTString.ToStoryNumber(chapter)}";
            var saveTo = RTFile.CombinePaths(storyLevelsCompilerPath, doc, $"{doc}_{RTString.ToStoryNumber(level)}{(string.IsNullOrEmpty(type) ? string.Empty : "_" + type + RTString.ToStoryNumber(cutscene))}");

            RTFile.CreateDirectory(saveTo);
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_LSB), RTFile.CombinePaths(saveTo, $"level{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.METADATA_LSB), RTFile.CombinePaths(saveTo, $"metadata{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.PLAYERS_LSB), RTFile.CombinePaths(saveTo, $"players{FileFormat.JSON.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_OGG), RTFile.CombinePaths(saveTo, $"song{FileFormat.OGG.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_JPG), RTFile.CombinePaths(saveTo, Level.COVER_JPG));

            EditorManager.inst.DisplayNotification("Saved the level to the story level compiler.", 2f, EditorManager.NotificationType.Success);
        }

        public void ToStoryVersionControl() => ToStoryVersionControl(RTEditor.inst.editorInfo);

        public void ToStoryVersionControl(EditorInfo editorInfo)
        {
            try
            {
                if (!editorInfo.isStory)
                    return;

                string cutsceneDestination = string.Empty;
                if (editorInfo.cutsceneDestination != Story.CutsceneDestination.Level)
                    cutsceneDestination = editorInfo.cutsceneDestination.ToString().ToLower();
                int cutscene = 0;
                if (editorInfo.cutscene >= 0)
                    cutscene = editorInfo.cutscene;

                ToStoryVersionControl(editorInfo.storyChapter, editorInfo.storyLevel, cutsceneDestination, cutscene);
            }
            catch { }
        }

        public void ToStoryVersionControl(int chapter, int level, string type, int cutscene)
        {
            var path = RTFile.BasePath;
            var doc = $"doc{RTString.ToStoryNumber(chapter)}";
            var saveTo = RTFile.CombinePaths(storyLevelsVersionControlPath, doc, $"{doc}_{RTString.ToStoryNumber(level)}{(string.IsNullOrEmpty(type) ? "" : "_" + type + RTString.ToStoryNumber(cutscene))} - {EditorLevelManager.inst.CurrentLevel.FolderName}");

            RTFile.CreateDirectory(saveTo);
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_LSB), RTFile.CombinePaths(saveTo, Level.LEVEL_LSB));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.METADATA_LSB), RTFile.CombinePaths(saveTo, Level.METADATA_LSB));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.PLAYERS_LSB), RTFile.CombinePaths(saveTo, Level.PLAYERS_LSB));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.EDITOR_LSE), RTFile.CombinePaths(saveTo, Level.EDITOR_LSE));
            // don't copy audio files
            //RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_OGG), RTFile.CombinePaths(saveTo, $"song{FileFormat.OGG.Dot()}"));
            RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_JPG), RTFile.CombinePaths(saveTo, Level.LEVEL_JPG));

            EditorManager.inst.DisplayNotification("Saved the level to the story level version control.", 2f, EditorManager.NotificationType.Success);
        }

        #endregion

        #endregion
    }
}

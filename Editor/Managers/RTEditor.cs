using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LSFunctions;

using CielaSpike;
using TMPro;
using SimpleJSON;
using Crosstales.FB;

using BetterLegacy.Arcade.Managers;
using BetterLegacy.Companion;
using BetterLegacy.Companion.Data;
using BetterLegacy.Companion.Entity;
using BetterLegacy.Configs;
using BetterLegacy.Core;
using BetterLegacy.Core.Animation;
using BetterLegacy.Core.Animation.Keyframe;
using BetterLegacy.Core.Components;
using BetterLegacy.Core.Data;
using BetterLegacy.Core.Data.Beatmap;
using BetterLegacy.Core.Data.Level;
using BetterLegacy.Core.Data.Modifiers;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using BetterLegacy.Core.Runtime;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Components;
using BetterLegacy.Editor.Data;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Elements;
using BetterLegacy.Editor.Data.Popups;
using BetterLegacy.Editor.Data.Timeline;

namespace BetterLegacy.Editor.Managers
{
    /// <summary>
    /// Base modded editor manager. RT stands for RhythmTech.
    /// </summary>
    public class RTEditor : MonoBehaviour
    {
        #region Init

        /// <summary>
        /// The <see cref="RTEditor"/> global instance reference.
        /// </summary>
        public static RTEditor inst;

        /// <summary>
        /// Initializes <see cref="RTEditor"/>.
        /// </summary>
        public static void Init() => EditorManager.inst.gameObject.AddComponent<RTEditor>();

        void Awake()
        {
            inst = this;

            InitFileWatchers();

            CreateGlobalSettings();
            LoadGlobalSettings();

            try
            {
                if (AssetPack.TryReadFromFile("editor/data/complexity.json", out string complexityFile))
                    EditorHelper.complexityJSON = JSON.Parse(complexityFile);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"There was an error with loading the complexity setting in the editor: {ex}");
            }

            try
            {
                ShapeManager.inst.SetupShapes();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"There was an error with loading the shapes in the editor: {ex}");
            }

            // do this before everything else
            var openFilePopup = EditorManager.inst.GetDialog("Open File Popup").Dialog;
            var parentSelector = EditorManager.inst.GetDialog("Parent Selector").Dialog;
            var saveAsPopup = EditorManager.inst.GetDialog("Save As Popup").Dialog;
            var quickActionsPopup = EditorManager.inst.GetDialog("Quick Actions Popup").Dialog;

            var openFilePopupSelect = openFilePopup.gameObject.AddComponent<SelectGUI>();
            openFilePopupSelect.target = openFilePopup;
            openFilePopupSelect.ogPos = openFilePopup.position;

            var parentSelectorSelect = parentSelector.gameObject.AddComponent<SelectGUI>();
            parentSelectorSelect.target = parentSelector;
            parentSelectorSelect.ogPos = parentSelector.position;

            var saveAsPopupSelect = saveAsPopup.Find("New File Popup").gameObject.AddComponent<SelectGUI>();
            saveAsPopupSelect.target = saveAsPopup;
            saveAsPopupSelect.ogPos = saveAsPopup.position;

            var quickActionsPopupSelect = quickActionsPopup.gameObject.AddComponent<SelectGUI>();
            quickActionsPopupSelect.target = quickActionsPopup;
            quickActionsPopupSelect.ogPos = quickActionsPopup.position;

            CoreHelper.Log($"RTEDITOR INIT -> {nameof(CacheEditor)}");
            CacheEditor();
            CoreHelper.Log($"RTEDITOR INIT -> {nameof(CacheSprites)}");
            CacheSprites();
            CoreHelper.Log($"RTEDITOR INIT -> {nameof(RegisterPopups)}");
            RegisterPopups();
            CoreHelper.Log($"RTEDITOR INIT -> {nameof(InitPrefabs)}");
            InitPrefabs();
            CoreHelper.Log($"RTEDITOR INIT -> {nameof(InitUI)}");
            InitUI();
            CoreHelper.Log($"RTEDITOR INIT -> {nameof(InitEditors)}");
            InitEditors();
            CoreHelper.Log($"RTEDITOR INIT -> {nameof(FinalSetup)}");
            FinalSetup();

            CoreHelper.Log($"RTEDITOR INIT -> FILE DRAG DROP");
            var fileDragAndDrop = gameObject.AddComponent<FileDragAndDrop>();

            fileDragAndDrop.onFilesDropped = dropInfos =>
            {
                CoreHelper.Log($"Dropping files.\nCount: {dropInfos.Count}");
                if (dropInfos.All(x => RTFile.FileIsFormat(x.filePath, FileFormat.PNG, FileFormat.JPG)) && dropInfos.Count > 1)
                {
                    CoreHelper.Log($"Creating image sequence.");
                    ObjectEditor.inst.CreateImageSequence(dropInfos.Select(x => x.filePath).ToArray(), EditorConfig.Instance.ImageSequenceFPS.Value);
                    return;
                }

                for (int i = 0; i < dropInfos.Count; i++)
                {
                    var dropInfo = dropInfos[i];

                    CoreHelper.Log($"Dropped file: {dropInfo}");

                    dropInfo.filePath = RTFile.ReplaceSlash(dropInfo.filePath);

                    var attributes = File.GetAttributes(dropInfo.filePath);
                    if (attributes.HasFlag(FileAttributes.Directory))
                    {
                        if (Level.TryVerify(dropInfo.filePath, true, out Level level))
                            EditorLevelManager.inst.LoadLevel(level);
                        else if (EditorLevelManager.inst.OpenLevelPopup.IsOpen && dropInfo.filePath.Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                        {
                            EditorLevelManager.inst.OpenLevelPopup.PathField.text = dropInfo.filePath.Remove(RTFile.ApplicationDirectory + "beatmaps/");
                            UpdateEditorPath(false);
                        }
                        else
                        {
                            var files = Directory.GetFiles(dropInfo.filePath);
                            if (files.All(x => RTFile.FileIsFormat(x, FileFormat.PNG, FileFormat.JPG)))
                            {
                                CoreHelper.Log($"Creating image sequence.");
                                ObjectEditor.inst.CreateImageSequence(files, EditorConfig.Instance.ImageSequenceFPS.Value);
                            }
                        }
                        break;
                    }

                    if (dropInfo.filePath.EndsWith(Level.LEVEL_LSB) || dropInfo.filePath.EndsWith(Level.LEVEL_VGD))
                    {
                        if (Level.TryVerify(dropInfo.filePath.Remove("/" + Level.LEVEL_LSB).Remove("/" + Level.LEVEL_VGD), true, out Level level))
                            EditorLevelManager.inst.LoadLevel(level);
                        break;
                    }

                    if (EditorLevelManager.inst.NewLevelPopup.IsOpen)
                    {
                        if (RTFile.FileIsAudio(dropInfo.filePath) && EditorLevelManager.inst.NewLevelPopup.SongPath)
                            EditorLevelManager.inst.NewLevelPopup.SongPath.text = dropInfo.filePath;
                        break;
                    }

                    if (RTFile.FileIsAudio(dropInfo.filePath))
                    {
                        EditorContextMenu.inst.ShowContextMenu(
                            new ButtonFunction("Replace song", () =>
                            {
                                if (!EditorManager.inst.hasLoadedLevel)
                                {
                                    EditorManager.inst.DisplayNotification($"Load a level before trying to replace the song!", 2f, EditorManager.NotificationType.Warning);
                                    return;
                                }

                                var audioFormat = RTFile.GetFileFormat(dropInfo.filePath);
                                var level = EditorLevelManager.inst.CurrentLevel;
                                var waveforms = Enum.GetNames(typeof(WaveformType));
                                for (int i = 0; i < waveforms.Length; i++)
                                    RTFile.DeleteFile(RTFile.CombinePaths(RTFile.BasePath, $"waveform-{waveforms[i].ToLower()}{FileFormat.PNG.Dot()}"));

                                RTFile.CopyFile(dropInfo.filePath, RTFile.CombinePaths(RTFile.BasePath, $"level{audioFormat.Dot()}"));
                                var previousAudio = level.music;
                                var previousTime = RTLevel.Current.FixedTime;
                                var previousPlayState = SoundManager.inst.Playing;
                                level.music = null;
                                CoroutineHelper.StartCoroutine(level.LoadAudioClipRoutine(() =>
                                {
                                    EditorLevelManager.inst.SetCurrentAudio(level.music);
                                    AudioManager.inst.SetMusicTime(previousTime);
                                    SoundManager.inst.SetPlaying(previousPlayState);

                                    if (EditorConfig.Instance.WaveformGenerate.Value)
                                    {
                                        CoreHelper.Log("Assigning waveform textures...");
                                        StartCoroutine(EditorTimeline.inst.AssignTimelineTexture(level.music));
                                    }
                                    else
                                    {
                                        CoreHelper.Log("Skipping waveform textures...");
                                        EditorTimeline.inst.SetTimelineSprite(null);
                                    }

                                    EditorTimeline.inst.UpdateTimelineSizes();
                                    GameManager.inst.UpdateTimeline();

                                    TriggerHelper.AddEventTriggers(timeField.gameObject, TriggerHelper.ScrollDelta(timeField, max: AudioManager.inst.CurrentAudioSource.clip.length));
                                    CoreHelper.Destroy(previousAudio);
                                }));
                            }),
                            new ButtonFunction("Create audio object", () =>
                            {
                                if (!ModifiersManager.inst.modifiers.TryFind(x => x.Name == nameof(ModifierFunctions.playSound), out Modifier modifier))
                                    return;

                                var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
                                string jpgFileLocation = RTFile.CombinePaths(editorPath, Path.GetFileName(dropInfo.filePath));

                                var levelPath = dropInfo.filePath.Remove(editorPath + "/");

                                if (!RTFile.FileExists(jpgFileLocation) && !dropInfo.filePath.Contains(editorPath))
                                    RTFile.CopyFile(dropInfo.filePath, jpgFileLocation);
                                else
                                    jpgFileLocation = editorPath + "/" + levelPath;

                                ObjectEditor.inst.CreateNewObject(timelineObject =>
                                {
                                    var beatmapObject = timelineObject.GetData<BeatmapObject>();

                                    beatmapObject.objectType = BeatmapObject.ObjectType.Empty;
                                    modifier = modifier.Copy();
                                    modifier.SetValue(0, jpgFileLocation.Remove(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1)));
                                    beatmapObject.modifiers.Add(modifier);
                                });
                            }));
                    }

                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.LSP))
                    {
                        var jn = JSON.Parse(RTFile.ReadFromFile(dropInfo.filePath));
                        var prefab = Prefab.Parse(jn);

                        prefab = RTPrefabEditor.inst.ImportPrefabIntoLevel(prefab);

                        if (EditorTimeline.inst.isOverMainTimeline)
                            RTPrefabEditor.inst.AddPrefabObjectToLevel(prefab);

                        break;
                    }

                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.VGP))
                    {
                        var jn = JSON.Parse(RTFile.ReadFromFile(dropInfo.filePath));
                        var prefab = Prefab.ParseVG(jn);

                        RTPrefabEditor.inst.ImportPrefabIntoLevel(prefab);
                        break;
                    }

                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.LST))
                    {
                        var jn = JSON.Parse(RTFile.ReadFromFile(dropInfo.filePath));
                        var theme = BeatmapTheme.Parse(jn);
                        RTThemeEditor.inst.ImportTheme(theme);

                        break;
                    }
                    
                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.VGT))
                    {
                        var jn = JSON.Parse(RTFile.ReadFromFile(dropInfo.filePath));
                        var theme = BeatmapTheme.ParseVG(jn);
                        RTThemeEditor.inst.ImportTheme(theme);

                        break;
                    }

                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.PNG, FileFormat.JPG))
                    {
                        var editorPath = RTFile.RemoveEndSlash(EditorLevelManager.inst.CurrentLevel.path);
                        CoreHelper.Log($"Selected file: {dropInfo.filePath}");
                        if (string.IsNullOrEmpty(dropInfo.filePath))
                            break;

                        string jpgFileLocation = RTFile.CombinePaths(editorPath, Path.GetFileName(dropInfo.filePath));

                        var levelPath = dropInfo.filePath.Remove(editorPath + "/");

                        if ((EditorConfig.Instance.OverwriteImportedImages.Value || !RTFile.FileExists(jpgFileLocation)) && !dropInfo.filePath.Contains(editorPath))
                            RTFile.CopyFile(dropInfo.filePath, jpgFileLocation);
                        else
                            jpgFileLocation = editorPath + "/" + levelPath;

                        ObjectEditor.inst.CreateNewObject(timelineObject =>
                        {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();

                            beatmapObject.ShapeType = ShapeType.Image;
                            beatmapObject.text = jpgFileLocation.Remove(jpgFileLocation.Substring(0, jpgFileLocation.LastIndexOf('/') + 1));
                        });
                        break;
                    }

                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.TXT))
                    {
                        if (!RTFile.TryReadFromFile(dropInfo.filePath, out string text))
                            break;

                        ObjectEditor.inst.CreateNewObject(timelineObject =>
                        {
                            var beatmapObject = timelineObject.GetData<BeatmapObject>();

                            beatmapObject.ShapeType = ShapeType.Text;
                            beatmapObject.text = text;
                        });
                        break;
                    }

                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.MP4, FileFormat.MOV))
                    {
                        var copyTo = dropInfo.filePath.Replace(RTFile.AppendEndSlash(RTFile.GetDirectory(dropInfo.filePath)), RTFile.AppendEndSlash(RTFile.BasePath)).Replace(Path.GetFileName(dropInfo.filePath),
                            RTFile.FileIsFormat(dropInfo.filePath, FileFormat.MP4) ? $"bg{FileFormat.MP4.Dot()}" : $"bg{FileFormat.MOV.Dot()}");

                        if (RTFile.CopyFile(dropInfo.filePath, copyTo) && CoreConfig.Instance.EnableVideoBackground.Value)
                        {
                            RTVideoManager.inst.Play(copyTo, 1f);
                            EditorManager.inst.DisplayNotification($"Copied file {Path.GetFileName(dropInfo.filePath)} and started Video BG!", 2f, EditorManager.NotificationType.Success);
                        }
                        else
                            RTVideoManager.inst.Stop();
                        break;
                    }

                    if (RTFile.FileIsFormat(dropInfo.filePath, FileFormat.JSON))
                    {
                        if (!RTFile.TryReadFromFile(dropInfo.filePath, out string file))
                            break;

                        var jn = JSON.Parse(file);
                        if (jn["file_type"] == null)
                            break;

                        switch (jn["file_type"].Value)
                        {
                            case "modifier": {
                                    if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                                        break;

                                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                                    beatmapObject.modifiers.Add(Modifier.Parse(jn["data"]));

                                    if (ObjectEditor.inst.Dialog.IsCurrent)
                                        ObjectEditor.inst.RenderDialog(beatmapObject);

                                    break;
                                }
                            case "modifiers": {
                                    if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                                        break;

                                    var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();
                                    for (int j = 0; j < jn["data"].Count; j++)
                                        beatmapObject.modifiers.Add(Modifier.Parse(jn["data"][j]));

                                    if (ObjectEditor.inst.Dialog.IsCurrent)
                                        ObjectEditor.inst.RenderDialog(beatmapObject);

                                    break;
                                }
                            case "beatmap_object": {
                                    if (!EditorManager.inst.hasLoadedLevel)
                                    {
                                        EditorManager.inst.DisplayNotification("Can't add objects to level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                                        break;
                                    }

                                    var beatmapObject = BeatmapObject.Parse(jn["data"]);
                                    beatmapObject.StartTime = AudioManager.inst.CurrentAudioSource.time;

                                    if (EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events)
                                        EditorTimeline.inst.SetLayer(beatmapObject.editorData.Layer, EditorTimeline.LayerType.Objects);

                                    GameData.Current.beatmapObjects.Add(beatmapObject);

                                    var timelineObject = EditorTimeline.inst.GetTimelineObject(beatmapObject);

                                    AudioManager.inst.SetMusicTime(ObjectEditor.AllowTimeExactlyAtStart ? AudioManager.inst.CurrentAudioSource.time : AudioManager.inst.CurrentAudioSource.time + 0.001f);

                                    EditorTimeline.inst.SetCurrentObject(timelineObject);

                                    RTLevel.Current?.UpdateObject(beatmapObject);
                                    EditorTimeline.inst.RenderTimelineObject(timelineObject);
                                    ObjectEditor.inst.OpenDialog(beatmapObject);

                                    break;
                                }
                        }

                        break;
                    }
                }
            };

            CoreHelper.Log($"RTEDITOR INIT -> EDITOR THREAD");
            try
            {
                editorThread = new Core.Threading.TickRunner(true);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
            CoreHelper.Log($"RTEDITOR INIT -> DONE!");
        }

        void Start()
        {
            if (string.IsNullOrEmpty(LegacyPlugin.LevelStartupPath))
                return;

            // allows for opening a level directly using Open With
            CoreHelper.Log($"Level Startup Path: {LegacyPlugin.LevelStartupPath}");
            EditorManager.inst.loading = false;
            EditorLevelManager.inst.LoadLevel(new Level(LegacyPlugin.LevelStartupPath));
            LegacyPlugin.LevelStartupPath = null;
        }

        void OnDestroy()
        {
            CoreHelper.LogError($"RTEditor was destroyed!");
            EditorConfig.AdjustPositionInputsChanged = null;
        }

        #region Full Init

        // 1 - cache editor values
        void CacheEditor()
        {
            titleBar = GameObject.Find("Editor Systems/Editor GUI/sizer/main/TitleBar").transform;
            popups = GameObject.Find("Editor Systems/Editor GUI/sizer/main/Popups").transform;
            EditorTimeline.inst.wholeTimeline = EditorManager.inst.timelineSlider.transform.parent.parent;
            EditorTimeline.inst.bins = EditorTimeline.inst.wholeTimeline.Find("Bins");
            timelineBar = EditorManager.inst.playButton.transform.parent.gameObject;
            EditorTimeline.inst.timelineImage = EditorManager.inst.timeline.GetComponent<Image>();
            EditorTimeline.inst.timelineOverlayImage = EditorManager.inst.timelineWaveformOverlay.GetComponent<Image>();

            notificationsParent = EditorManager.inst.notification.transform.AsRT();
            tooltipText = EditorManager.inst.tooltip.GetComponent<TextMeshProUGUI>();

            // Here we fix the naming issues with unmodded Legacy. (e.g. naming a theme doesn't allow for symbols and naming a prefab doesn't allow for spaces)
            EditorManager.inst.GetDialog("Save As Popup").Dialog.Find("New File Popup/level-name").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
            GameObject.Find("Editor GUI/sizer/main/EditorDialogs/PrefabDialog/data/name/input").GetComponent<InputField>().characterValidation = InputField.CharacterValidation.None;
        }

        // 2 - cache sprites
        void CacheSprites()
        {
            EditorSprites.Init();

            EditorSprites.DottedLineSprite = ObjEditor.inst.KeyframeEndPrefab.GetComponent<Image>().sprite;
        }

        // 3 - register existing popups
        void RegisterPopups()
        {
            try
            {
                EditorLevelManager.inst.NewLevelPopup = new NewLevelPopup();
                EditorLevelManager.inst.NewLevelPopup.Assign(EditorLevelManager.inst.NewLevelPopup.GetLegacyDialog().Dialog.gameObject);
                EditorLevelManager.inst.NewLevelPopup.title = EditorLevelManager.inst.NewLevelPopup.TMPTitle.text;
                EditorLevelManager.inst.NewLevelPopup.size = EditorLevelManager.inst.NewLevelPopup.GameObject.transform.GetChild(0).AsRT().sizeDelta;
                EditorLevelManager.inst.NewLevelPopup.onRender = () =>
                {
                    if (AssetPack.TryReadFromFile("editor/ui/popups/new_file_popup.json", out string uiFile))
                    {
                        var jn = JSON.Parse(uiFile);
                        RectValues.TryParse(jn["base"]["rect"], RectValues.Default.SizeDelta(500f, 540f)).AssignToRectTransform(EditorLevelManager.inst.NewLevelPopup.GameObject.transform.GetChild(0).AsRT());
                        RectValues.TryParse(jn["top_panel"]["rect"], RectValues.FullAnchored.AnchoredPosition(0f, -32f).AnchorMin(0, 1).Pivot(0f, 0f).SizeDelta(0f, 32f)).AssignToRectTransform(EditorLevelManager.inst.NewLevelPopup.TopPanel);

                        if (jn["title"] != null)
                        {
                            EditorLevelManager.inst.NewLevelPopup.title = jn["title"]["text"] != null ? jn["title"]["text"] : "New Level Creator";

                            var title = EditorLevelManager.inst.NewLevelPopup.TMPTitle;
                            RectValues.TryParse(jn["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                        }
                    }
                };

                EditorLevelManager.inst.OpenLevelPopup = new ContentPopup(EditorPopup.OPEN_FILE_POPUP);
                EditorLevelManager.inst.OpenLevelPopup.Assign(EditorLevelManager.inst.OpenLevelPopup.GetLegacyDialog().Dialog.gameObject);
                EditorLevelManager.inst.OpenLevelPopup.title = EditorLevelManager.inst.OpenLevelPopup.Title.text;
                EditorLevelManager.inst.OpenLevelPopup.size = EditorLevelManager.inst.OpenLevelPopup.GameObject.transform.AsRT().sizeDelta;
                EditorLevelManager.inst.OpenLevelPopup.refreshSearch = EditorManager.inst.UpdateOpenBeatmapSearch;
                EditorLevelManager.inst.OpenLevelPopup.onRender = () =>
                {
                    if (AssetPack.TryReadFromFile("editor/ui/popups/open_file_popup.json", out string uiFile))
                    {
                        var jn = JSON.Parse(uiFile);
                        RectValues.TryParse(jn["base"]["rect"], RectValues.Default.SizeDelta(600f, 400f)).AssignToRectTransform(EditorLevelManager.inst.OpenLevelPopup.GameObject.transform.AsRT());
                        RectValues.TryParse(jn["top_panel"]["rect"], RectValues.FullAnchored.AnchorMin(0, 1).Pivot(0f, 0f).SizeDelta(32f, 32f)).AssignToRectTransform(EditorLevelManager.inst.OpenLevelPopup.TopPanel);

                        var layoutValues = LayoutValues.Parse(jn["layout"]);
                        if (layoutValues is GridLayoutValues gridLayoutValues)
                            gridLayoutValues.AssignToLayout(EditorLevelManager.inst.OpenLevelPopup.Grid ? EditorLevelManager.inst.OpenLevelPopup.Grid : EditorLevelManager.inst.OpenLevelPopup.GameObject.transform.Find("mask/content").GetComponent<GridLayoutGroup>());

                        if (jn["title"] != null)
                        {
                            EditorLevelManager.inst.OpenLevelPopup.title = jn["title"]["text"] != null ? jn["title"]["text"] : "Pick a Level to Open";

                            var title = EditorLevelManager.inst.OpenLevelPopup.Title;
                            RectValues.TryParse(jn["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                            title.alignment = jn["title"]["alignment"] != null ? (TextAnchor)jn["title"]["alignment"].AsInt : TextAnchor.MiddleLeft;
                            title.fontSize = jn["title"]["font_size"] != null ? jn["title"]["font_size"].AsInt : 20;
                            title.fontStyle = (FontStyle)jn["title"]["font_style"].AsInt;
                            title.horizontalOverflow = jn["title"]["horizontal_overflow"] != null ? (HorizontalWrapMode)jn["title"]["horizontal_overflow"].AsInt : HorizontalWrapMode.Wrap;
                            title.verticalOverflow = jn["title"]["vertical_overflow"] != null ? (VerticalWrapMode)jn["title"]["vertical_overflow"].AsInt : VerticalWrapMode.Overflow;
                        }
                    }
                };

                InfoPopup = new InfoPopup(EditorPopup.FILE_INFO_POPUP);
                InfoPopup.Assign(InfoPopup.GetLegacyDialog().Dialog.gameObject);

                ParentSelectorPopup = new ContentPopup(EditorPopup.PARENT_SELECTOR);
                ParentSelectorPopup.Assign(ParentSelectorPopup.GetLegacyDialog().Dialog.gameObject);
                ParentSelectorPopup.title = ParentSelectorPopup.Title.text;
                ParentSelectorPopup.size = ParentSelectorPopup.GameObject.transform.AsRT().sizeDelta;
                ParentSelectorPopup.refreshSearch = EditorManager.inst.UpdateParentSearch;

                SaveAsPopup = new EditorPopup(EditorPopup.SAVE_AS_POPUP);
                SaveAsPopup.Assign(SaveAsPopup.GetLegacyDialog().Dialog.gameObject);
                SaveAsPopup.title = SaveAsPopup.Title.text;
                SaveAsPopup.size = SaveAsPopup.GameObject.transform.AsRT().sizeDelta;

                PrefabPopups = new DoubleContentPopup(EditorPopup.PREFAB_POPUP);
                var prefabDialog = PrefabPopups.GetLegacyDialog().Dialog;
                PrefabPopups.GameObject = prefabDialog.gameObject;
                PrefabPopups.Internal = new ContentPopup("internal prefabs");
                PrefabPopups.Internal.Assign(prefabDialog.Find("internal prefabs").gameObject);
                var internalSelectGUI = PrefabPopups.Internal.GameObject.GetOrAddComponent<SelectGUI>();
                internalSelectGUI.ogPos = PrefabPopups.Internal.GameObject.transform.position;
                internalSelectGUI.target = PrefabPopups.Internal.GameObject.transform;
                PrefabPopups.External = new ContentPopup("external prefabs");
                PrefabPopups.External.Assign(prefabDialog.Find("external prefabs").gameObject);
                var externalSelectGUI = PrefabPopups.External.GameObject.GetOrAddComponent<SelectGUI>();
                externalSelectGUI.ogPos = PrefabPopups.External.GameObject.transform.position;
                externalSelectGUI.target = PrefabPopups.External.GameObject.transform;

                PrefabPopups.Internal.title = "Internal Prefabs";
                PrefabPopups.Internal.RenderTitle();
                PrefabPopups.External.title = "External Prefabs";
                PrefabPopups.External.RenderTitle();
                PrefabPopups.onRender = () =>
                {
                    if (AssetPack.TryReadFromFile("editor/ui/popups/prefab_popup.json", out string uiFile))
                    {
                        var jn = JSON.Parse(uiFile);
                        RectValues.TryParse(jn["base"]["rect"], RectValues.Default.SizeDelta(920f, 450f)).AssignToRectTransform(PrefabPopups.GameObject.transform.AsRT());

                        RectValues.TryParse(jn["internal"]["base"]["rect"], new RectValues(new Vector2(-80f, -16f), new Vector2(0f, 1f), Vector2.zero, new Vector2(0f, 0.5f), new Vector2(500f, -32f))).AssignToRectTransform(PrefabPopups.Internal.GameObject.transform.AsRT());
                        RectValues.TryParse(jn["external"]["base"]["rect"], new RectValues(new Vector2(60f, -16f), Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(500f, -32f))).AssignToRectTransform(PrefabPopups.External.GameObject.transform.AsRT());

                        var internalLayoutValues = LayoutValues.Parse(jn["internal"]["layout"]);
                        if (internalLayoutValues is GridLayoutValues internalGrid)
                            internalGrid.AssignToLayout(PrefabPopups.Internal.GameObject.transform.Find("mask/content").GetComponent<GridLayoutGroup>());
                        
                        var externalLayoutValues = LayoutValues.Parse(jn["external"]["layout"]);
                        if (externalLayoutValues is GridLayoutValues externalGrid)
                            externalGrid.AssignToLayout(PrefabPopups.External.GameObject.transform.Find("mask/content").GetComponent<GridLayoutGroup>());

                        PrefabPopups.Internal.GameObject.GetComponent<ScrollRect>().horizontal = jn["internal"]["scroll"]["horizontal"].AsBool;
                        PrefabPopups.External.GameObject.GetComponent<ScrollRect>().horizontal = jn["external"]["scroll"]["horizontal"].AsBool;

                        if (jn["internal"]["title"] != null)
                        {
                            PrefabPopups.Internal.title = jn["internal"]["title"]["text"];

                            var title = PrefabPopups.Internal.Title;
                            RectValues.TryParse(jn["internal"]["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                            title.alignment = (TextAnchor)jn["internal"]["title"]["alignment"].AsInt;
                            title.fontSize = jn["internal"]["title"]["font_size"].AsInt;
                            title.fontStyle = (FontStyle)jn["internal"]["title"]["font_style"].AsInt;
                            title.horizontalOverflow = (HorizontalWrapMode)jn["internal"]["title"]["horizontal_overflow"].AsInt;
                            title.verticalOverflow = (VerticalWrapMode)jn["internal"]["title"]["vertical_overflow"].AsInt;
                        }

                        if (jn["external"]["title"] != null)
                        {
                            PrefabPopups.External.title = jn["external"]["title"]["text"];

                            var title = PrefabPopups.External.Title;
                            RectValues.TryParse(jn["external"]["title"]["rect"], RectValues.FullAnchored.AnchoredPosition(2f, 0f).SizeDelta(-12f, -8f)).AssignToRectTransform(title.rectTransform);
                            title.alignment = (TextAnchor)jn["external"]["title"]["alignment"].AsInt;
                            title.fontSize = jn["external"]["title"]["font_size"].AsInt;
                            title.fontStyle = (FontStyle)jn["external"]["title"]["font_style"].AsInt;
                            title.horizontalOverflow = (HorizontalWrapMode)jn["external"]["title"]["horizontal_overflow"].AsInt;
                            title.verticalOverflow = (VerticalWrapMode)jn["external"]["title"]["vertical_overflow"].AsInt;
                        }
                    }
                };

                ObjectOptionsPopup = new EditorPopup(EditorPopup.OBJECT_OPTIONS_POPUP);
                ObjectOptionsPopup.GameObject = ObjectOptionsPopup.GetLegacyDialog().Dialog.gameObject;

                BGObjectOptionsPopup = new EditorPopup(EditorPopup.BG_OPTIONS_POPUP);
                BGObjectOptionsPopup.GameObject = BGObjectOptionsPopup.GetLegacyDialog().Dialog.gameObject;

                editorPopups.Add(EditorLevelManager.inst.NewLevelPopup);
                editorPopups.Add(EditorLevelManager.inst.OpenLevelPopup);
                editorPopups.Add(InfoPopup);
                editorPopups.Add(ParentSelectorPopup);
                editorPopups.Add(SaveAsPopup);
                editorPopups.Add(PrefabPopups);
                editorPopups.Add(ObjectOptionsPopup);
                editorPopups.Add(BGObjectOptionsPopup);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        // 4 - create editor prefabs
        void InitPrefabs()
        {
            var prefabParent = Creator.NewGameObject("prefabs", transform);
            var prefabHolder = EditorPrefabHolder.Instance;
            prefabHolder.PrefabParent = prefabParent.transform;

            timeDefault = timelineBar.transform.GetChild(0).gameObject;
            timeDefault.name = "Time Default";

            var defaultInputField = timelineBar.transform.Find("Time");
            prefabHolder.DefaultInputField = defaultInputField.gameObject;
            prefabHolder.DefaultInputField.SetActive(true);
            defaultInputField.SetParent(prefabHolder.PrefabParent);
            defaultInputField.localScale = Vector3.one;
            EditorManager.inst.speedText.transform.parent.SetParent(transform);

            if (prefabHolder.DefaultInputField.TryGetComponent(out InputField frick))
            {
                frick.textComponent.fontSize = 16;
                frick.GetPlaceholderText().fontSize = 16;
            }

            if (ObjEditor.inst)
            {
                prefabHolder.NumberInputField = ObjEditor.inst.ObjectView.transform.Find("time").gameObject.Duplicate(prefabHolder.PrefabParent, "float input");

                var floatInputFieldStorage = prefabHolder.NumberInputField.AddComponent<InputFieldStorage>();
                floatInputFieldStorage.Assign(prefabHolder.NumberInputField);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.addButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.subButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.leftGreaterButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.leftButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.middleButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.rightButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.rightGreaterButton);
                floatInputFieldStorage.inputField.characterLimit = 0;
                prefabHolder.NumberInputField.transform.Find("time").gameObject.name = "input";

                if (prefabHolder.NumberInputField.transform.Find("lock"))
                    DestroyImmediate(prefabHolder.NumberInputField.transform.Find("lock").gameObject);

                var middleButtonLayout = floatInputFieldStorage.middleButton.GetComponent<LayoutElement>();
                middleButtonLayout.minWidth = 8f;
                middleButtonLayout.preferredWidth = 8f;

                floatInputFieldStorage.subButton = floatInputFieldStorage.middleButton.gameObject.Duplicate(prefabHolder.NumberInputField.transform, "sub").GetComponent<Button>();
                floatInputFieldStorage.subButton.image = floatInputFieldStorage.subButton.GetComponent<Image>();
                floatInputFieldStorage.subButton.image.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/sub.png"));
                var subButtonLayout = floatInputFieldStorage.subButton.GetComponent<LayoutElement>();
                subButtonLayout.minWidth = 16f;
                subButtonLayout.preferredWidth = 16f;

                floatInputFieldStorage.subButton.gameObject.SetActive(false);
                floatInputFieldStorage.addButton = floatInputFieldStorage.middleButton.gameObject.Duplicate(prefabHolder.NumberInputField.transform, "add").GetComponent<Button>();
                floatInputFieldStorage.addButton.image = floatInputFieldStorage.addButton.GetComponent<Image>();
                floatInputFieldStorage.addButton.image.sprite = SpriteHelper.LoadSprite(AssetPack.GetFile("core/sprites/icons/operations/add.png"));
                var addButtonLayout = floatInputFieldStorage.addButton.GetComponent<LayoutElement>();
                addButtonLayout.minWidth = 32f;
                addButtonLayout.preferredWidth = 32f;
                floatInputFieldStorage.addButton.gameObject.SetActive(false);

                prefabHolder.StringInputField = floatInputFieldStorage.inputField.gameObject.Duplicate(prefabHolder.PrefabParent, "string input");
                var stringInputField = prefabHolder.StringInputField.GetComponent<InputField>();
                stringInputField.textComponent.fontSize = 16;
                stringInputField.GetPlaceholderText().fontSize = 16;

                prefabHolder.Function2Button = ObjEditor.inst.ObjectView.transform.Find("applyprefab").gameObject.Duplicate(prefabHolder.PrefabParent, "function 2 button");

                var functionButtonStorage = prefabHolder.Function2Button.AddComponent<FunctionButtonStorage>();
                functionButtonStorage.button = prefabHolder.Function2Button.GetComponent<Button>();
                functionButtonStorage.label = prefabHolder.Function2Button.transform.GetChild(0).GetComponent<Text>();
                Destroy(prefabHolder.Function2Button.GetComponent<Animator>());
                functionButtonStorage.button.transition = Selectable.Transition.ColorTint;

                prefabHolder.Dropdown = ObjEditor.inst.ObjectView.transform.Find("autokill/tod-dropdown").gameObject.Duplicate(prefabHolder.PrefabParent, "dropdown");
                var dropdownStorage = prefabHolder.Dropdown.AddComponent<DropdownStorage>();

                dropdownStorage.dropdown = prefabHolder.Dropdown.GetComponent<Dropdown>();
                dropdownStorage.templateGrid = dropdownStorage.dropdown.template.Find("Viewport/Content").gameObject.AddComponent<GridLayoutGroup>();
                dropdownStorage.templateGrid.cellSize = new Vector2(1000f, 32f);
                dropdownStorage.templateGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                dropdownStorage.templateGrid.constraintCount = 1;

                dropdownStorage.templateFitter = dropdownStorage.templateGrid.gameObject.AddComponent<ContentSizeFitter>();
                dropdownStorage.templateFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                dropdownStorage.dropdown.options.Clear();
                dropdownStorage.hideOptions = prefabHolder.Dropdown.GetComponent<HideDropdownOptions>();
                dropdownStorage.hideOptions.DisabledOptions.Clear();
                if (prefabHolder.Dropdown.TryGetComponent(out HoverTooltip dropdownHoverTooltip))
                    dropdownHoverTooltip.tooltipLangauges.Clear();

                dropdownStorage.arrow = prefabHolder.Dropdown.transform.Find("Arrow").GetComponent<Image>();

                prefabHolder.Labels = ObjEditor.inst.ObjectView.transform.ChildList().First(x => x.name == "label").gameObject.Duplicate(prefabHolder.PrefabParent, "label");
                if (prefabHolder.Labels.transform.childCount > 1)
                    CoreHelper.Destroy(prefabHolder.Labels.transform.GetChild(1).gameObject, true);

                prefabHolder.CollapseToggle = ObjEditor.inst.ObjectView.transform.Find("autokill/collapse").gameObject.Duplicate(prefabHolder.PrefabParent, "collapse");

                prefabHolder.ScrollView = ObjEditor.inst.ObjectView.transform.parent.parent.gameObject.Duplicate(prefabHolder.PrefabParent, "Scroll View");
                CoreHelper.DestroyChildren(prefabHolder.ScrollView.transform.Find("Viewport/Content"));

                prefabHolder.CurvesDropdown = ObjEditor.inst.KeyframeDialogs[0].transform.Find("curves").gameObject.Duplicate(prefabHolder.PrefabParent, "curves");
                var curvesDropdownStorage = prefabHolder.CurvesDropdown.AddComponent<DropdownStorage>();

                curvesDropdownStorage.dropdown = prefabHolder.CurvesDropdown.GetComponent<Dropdown>();
                curvesDropdownStorage.templateGrid = curvesDropdownStorage.dropdown.template.Find("Viewport/Content").gameObject.AddComponent<GridLayoutGroup>();
                curvesDropdownStorage.templateGrid.cellSize = new Vector2(1000f, 32f);
                curvesDropdownStorage.templateGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                curvesDropdownStorage.templateGrid.constraintCount = 1;

                curvesDropdownStorage.templateFitter = curvesDropdownStorage.templateGrid.gameObject.AddComponent<ContentSizeFitter>();
                curvesDropdownStorage.templateFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
                curvesDropdownStorage.dropdown.options.Clear();
                curvesDropdownStorage.hideOptions = prefabHolder.CurvesDropdown.AddComponent<HideDropdownOptions>();
                curvesDropdownStorage.hideOptions.DisabledOptions.Clear();
                if (prefabHolder.CurvesDropdown.TryGetComponent(out HoverTooltip curvesDropdownHoverTooltip))
                    curvesDropdownHoverTooltip.tooltipLangauges.Clear();

                curvesDropdownStorage.arrow = prefabHolder.CurvesDropdown.transform.Find("Arrow").GetComponent<Image>();

                prefabHolder.ColorsLayout = ObjEditor.inst.KeyframeDialogs[3].transform.Find("color").gameObject.Duplicate(prefabHolder.PrefabParent, "color");
                prefabHolder.Slider = ObjEditor.inst.ObjectView.transform.Find("depth/depth").gameObject.Duplicate(prefabHolder.PrefabParent, "Slider");

                prefabHolder.EditorLayerToggle = ObjEditor.inst.ObjectView.transform.Find("editor/layer").GetChild(0).gameObject.Duplicate(prefabHolder.PrefabParent, "editor layer");
                prefabHolder.EditorLayerToggle.GetComponent<Toggle>().group = null;
                CoreHelper.Destroy(prefabHolder.EditorLayerToggle.GetComponent<EventTrigger>(), true);
            }

            if (PrefabEditor.inst)
            {
                prefabHolder.DeleteButton = PrefabEditor.inst.AddPrefab.transform.Find("delete").gameObject.Duplicate(prefabHolder.PrefabParent, "delete");
                var deleteButtonStorage = prefabHolder.DeleteButton.AddComponent<DeleteButtonStorage>();
                deleteButtonStorage.Assign(prefabHolder.DeleteButton);

                prefabHolder.Tag = Creator.NewUIObject("Tag", transform);
                var tagPrefabImage = prefabHolder.Tag.AddComponent<Image>();
                tagPrefabImage.color = new Color(1f, 1f, 1f, 1f);
                var tagPrefabLayout = prefabHolder.Tag.AddComponent<HorizontalLayoutGroup>();
                tagPrefabLayout.childControlWidth = false;
                tagPrefabLayout.childForceExpandWidth = false;

                var input = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(prefabHolder.Tag.transform, "Input");
                input.transform.localScale = Vector3.one;
                input.transform.AsRT().sizeDelta = new Vector2(136f, 32f);
                var text = input.transform.Find("Text").GetComponent<Text>();
                text.alignment = TextAnchor.MiddleCenter;
                text.fontSize = 17;

                var delete = prefabHolder.DeleteButton.Duplicate(prefabHolder.Tag.transform, "Delete");
                new RectValues(Vector2.zero, Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f)).AssignToRectTransform(delete.transform.AsRT());
            }

            prefabHolder.Vector2InputFields = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/move/position").gameObject.Duplicate(prefabHolder.PrefabParent);
            var vector2InputFieldStorage = prefabHolder.Vector2InputFields.AddComponent<Vector2InputFieldStorage>();
            vector2InputFieldStorage.Assign();
            if (vector2InputFieldStorage.x)
            {
                var floatInputFieldStorage = vector2InputFieldStorage.x;
                CoreHelper.RemoveAnimator(floatInputFieldStorage.addButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.subButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.leftGreaterButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.leftButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.middleButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.rightButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.rightGreaterButton);
                floatInputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                floatInputFieldStorage.inputField.characterLimit = 0;
            }
            if (vector2InputFieldStorage.y)
            {
                var floatInputFieldStorage = vector2InputFieldStorage.y;
                CoreHelper.RemoveAnimator(floatInputFieldStorage.addButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.subButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.leftGreaterButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.leftButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.middleButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.rightButton);
                CoreHelper.RemoveAnimator(floatInputFieldStorage.rightGreaterButton);
                floatInputFieldStorage.inputField.characterValidation = InputField.CharacterValidation.None;
                floatInputFieldStorage.inputField.characterLimit = 0;
            }

            prefabHolder.Toggle = EditorManager.inst.GetDialog("Settings Editor").Dialog.Find("snap/toggle/toggle").gameObject.Duplicate(prefabHolder.PrefabParent, "toggle");

            prefabHolder.Function1Button = timelineBar.transform.Find("event").gameObject.Duplicate(prefabHolder.PrefabParent, "function 1 button");
            var functionButton1Storage = prefabHolder.Function1Button.AddComponent<FunctionButtonStorage>();
            functionButton1Storage.button = prefabHolder.Function1Button.GetComponent<Button>();
            functionButton1Storage.button.onClick.ClearAll();
            functionButton1Storage.label = prefabHolder.Function1Button.transform.GetChild(0).GetComponent<Text>();

            prefabHolder.ToggleButton = EditorManager.inst.GetDialog("Event Editor").Dialog.Find("data/right/grain/colored").gameObject.Duplicate(prefabHolder.PrefabParent, "toggle button");
            var toggleButtonStorage = prefabHolder.ToggleButton.AddComponent<ToggleButtonStorage>();
            toggleButtonStorage.Assign(prefabHolder.ToggleButton);

            prefabHolder.SpriteButton = timelineBar.transform.Find("play").gameObject.Duplicate(prefabHolder.PrefabParent, "Sprite Button");
            DestroyImmediate(prefabHolder.SpriteButton.GetComponent<Animator>());
            prefabHolder.SpriteButton.GetComponent<Button>().transition = Selectable.Transition.ColorTint;

            var openFilePopup = EditorManager.inst.GetDialog("Open File Popup").Dialog;
            prefabHolder.CloseButton = openFilePopup.Find("Panel/x").gameObject.Duplicate(prefabHolder.PrefabParent, "x");
            prefabHolder.Scrollbar = openFilePopup.Find("Scrollbar").gameObject.Duplicate(prefabHolder.PrefabParent, "Scrollbar");

            EditorTimeline.inst.binPrefab = EditorTimeline.inst.bins.GetChild(0).gameObject.Duplicate(prefabHolder.PrefabParent, "bin");

            prefabHolder.Dialog = EditorManager.inst.GetDialog("Multi Keyframe Editor (Object)").Dialog.gameObject.Duplicate(prefabHolder.PrefabParent, "Dialog");
            var dialogStorage = prefabHolder.Dialog.AddComponent<EditorDialogStorage>();
            dialogStorage.topPanel = prefabHolder.Dialog.transform.GetChild(0).GetComponent<Image>();
            dialogStorage.title = dialogStorage.topPanel.transform.GetChild(0).GetComponent<Text>();

            prefabHolder.ContentPopup = EditorManager.inst.GetDialog("Parent Selector").Dialog.gameObject.Duplicate(prefabHolder.PrefabParent, "Content Popup");
            prefabHolder.DoubleContentPopup = EditorManager.inst.GetDialog(EditorPopup.PREFAB_POPUP).Dialog.gameObject.Duplicate(prefabHolder.PrefabParent, "Double Content Popup");
            prefabHolder.DoubleContentPopup.transform.Find("internal prefabs").name = "internal";
            prefabHolder.DoubleContentPopup.transform.Find("external prefabs").name = "external";
        }

        // 5 - setup misc editor UI
        void InitUI()
        {
            LoadEditorAssetPack(false);

            SetupNotificationValues();
            SetupTimelineBar();
            SetupTimelineTriggers();
            SetupCreateObjects();
            SetupTitleBar();
            SetupDoggo();
            SetupPaths();
            SetupTimelinePreview();
            SetupTimelineElements();
            SetupGrid();
            SetupTimelineGrid();
            SetupNewFilePopup();
            CreatePreview();
            CreateDebug();
            CreateAutosavePopup();
            CreateScreenshotsView();
            CreateFontSelector();
            SetupEaseDropdowns();
            CreateCameraArea();

            ProgressPopup = new ProgressPopup(EditorPopup.PROGRESS_POPUP);
            ProgressPopup.Init();
        }

        // 6 - initialize editors
        void InitEditors()
        {
            RTColorPicker.Init();

            RTSettingEditor.Init();
            RTMarkerEditor.Init();
            RTMetaDataEditor.Init();
            RTEventEditor.Init();
            RTThemeEditor.Init();
            RTPrefabEditor.Init();
            ObjectEditor.Init();
            RTCheckpointEditor.Init();

            MultiObjectEditor.Init();
            RTTextEditor.Init();
            KeybindEditor.Init();
            PlayerEditor.Init();
            ModifiersEditor.Init();
            AchievementEditor.Init();
            LevelPropertiesEditor.Init();
            AssetEditor.Init();
            PinnedLayerEditor.Init();
            AnimationEditor.Init();
            ProjectPlanner.Init();

            RTBackgroundEditor.Init();

            EditorDocumentation.Init();

            CaptureArea.Init();
        }

        // 7 - finalization
        void FinalSetup()
        {
            mousePicker = Creator.NewUIObject("picker", EditorManager.inst.dialogs.parent);
            mousePicker.transform.localScale = Vector3.one;
            mousePickerRT = mousePicker.transform.AsRT();

            var img = Creator.NewUIObject("image", mousePickerRT);
            img.transform.localScale = Vector3.one;

            img.transform.AsRT().anchoredPosition = new Vector2(-930f, -520f);
            img.transform.AsRT().sizeDelta = new Vector2(32f, 32f);

            var image = img.AddComponent<Image>();

            image.sprite = EditorSprites.DropperSprite;

            timelineTime = EditorManager.inst.timelineTime.GetComponent<Text>();
            SetNotificationProperties();

            EditorTimeline.inst.timelineSlider = EditorManager.inst.timelineSlider.GetComponent<Slider>();
            TriggerHelper.AddEventTriggers(EditorTimeline.inst.timelineSlider.gameObject,
                TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
                {
                    if (!EditorConfig.Instance.DraggingMainCursorFix.Value)
                        return;

                    EditorTimeline.inst.changingTime = true;
                    EditorTimeline.inst.newTime = EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom;
                    AudioManager.inst.SetMusicTime(Mathf.Clamp(EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                }),
                TriggerHelper.CreateEntry(EventTriggerType.PointerUp, eventData =>
                {
                    if (!EditorConfig.Instance.DraggingMainCursorFix.Value)
                        return;

                    EditorTimeline.inst.newTime = EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom;
                    AudioManager.inst.SetMusicTime(Mathf.Clamp(EditorTimeline.inst.timelineSlider.value / EditorManager.inst.Zoom, 0f, AudioManager.inst.CurrentAudioSource.clip.length));
                    EditorTimeline.inst.changingTime = false;
                }));

            DestroyImmediate(EditorManager.inst.mouseTooltip);
            mouseTooltip = EditorManager.inst.notificationPrefabs[0].Duplicate(EditorManager.inst.notification.transform.parent, "tooltip");
            EditorManager.inst.mouseTooltip = mouseTooltip;
            mouseTooltipRT = mouseTooltip.transform.AsRT();
            UIManager.SetRectTransform(mouseTooltipRT, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(250f, 32f));
            mouseTooltipRT.localScale = new Vector3(0.9f, 0.9f, 1f);
            mouseTooltipText = mouseTooltip.transform.Find("text").GetComponent<TextMeshProUGUI>();

            EditorThemeManager.AddGraphic(mouseTooltip.GetComponent<Image>(), ThemeGroup.Notification_Background, true);
            EditorThemeManager.AddGraphic(mouseTooltipRT.Find("bg/bg").GetComponent<Image>(), ThemeGroup.Notification_Info, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddLightText(mouseTooltipText);
            EditorThemeManager.AddGraphic(mouseTooltipRT.Find("bg/Image").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(mouseTooltipRT.Find("bg/title").GetComponent<Text>());

            var timelineParent = Creator.NewUIObject("Timeline Objects", EditorManager.inst.timeline.transform, 1);
            EditorTimeline.inst.timelineObjectsParent = timelineParent.transform.AsRT();
            RectValues.FullAnchored.AssignToRectTransform(EditorTimeline.inst.timelineObjectsParent);

            SetupFileBrowser();
            CreateFolderCreator();
            CreateWarningPopup();
            EditorContextMenu.Init();

            SetupMiscEditorThemes();

            ParentSelectorPopup.InitPageField();
            ParentSelectorPopup.getMaxPageCount = () => GameData.Current.beatmapObjects.FindAll(x => !x.FromPrefab).Count / ObjectEditor.ParentObjectsPerPage; // temp
        }

        #endregion

        #endregion

        #region Tick Update

        void Update()
        {
            editorInfo?.timer.Update();

            UpdatePicker();
            UpdateTooltip();
            EditorTimeline.inst.UpdateBinControls();
            EditorTimeline.inst.UpdateTimeChange();
            UpdatePreview();
            UpdateKey();
            UpdateCameraArea();

            if (editorInfo.time > 36000f)
                AchievementManager.inst.UnlockAchievement("serious_dedication");
            if (editorInfo.time > 86400f)
                AchievementManager.inst.UnlockAchievement("true_dedication");

            // Only want this during April Fools.
            //if (CoreHelper.AprilFools && RandomHelper.PercentChanceSingle(0.001f))
            //{
            //    var array = new string[]
            //    {
            //        "BRO",
            //        "Go touch some grass.",
            //        "Hello, hello? I wanted to record this message for you to get you settled in your first night. The animatronic characters DO get a bit quirky at night",
            //        "",
            //        "L + Ratio",
            //        "Hi Diggy",
            //        "Hi KarasuTori",
            //        "Hi MoNsTeR",
            //        "Hi RTMecha",
            //        "Hi Example",
            //        $"Hi {CoreConfig.Instance.DisplayName.Value}!",
            //        "Kweeble kweeble kweeble",
            //        "Testing... is this thing on?",
            //        "When life gives you lemons, don't make lemonade.",
            //        "AMONGUS",
            //        "I fear no man, but THAT thing, it scares me.",
            //        "/summon minecraft:wither",
            //        "Autobots, transform and roll out.",
            //        "sands undertraveler",
            //    };

            //    EditorManager.inst.DisplayNotification(array[UnityEngine.Random.Range(0, array.Length)], 4f, EditorManager.NotificationType.Info);
            //}
        }

        void UpdatePicker()
        {
            var pickerActive = parentPickerEnabled || prefabPickerEnabled || EditorTimeline.inst.onSelectTimelineObject != null || RTPrefabEditor.inst.onSelectPrefab != null;
            if (Input.GetMouseButtonDown(1) && pickerActive)
            {
                parentPickerEnabled = false;
                prefabPickerEnabled = false;
                EditorTimeline.inst.onSelectTimelineObject = null;
                RTPrefabEditor.inst.onSelectPrefab = null;
                pickerActive = false;
            }

            mousePicker?.SetActive(pickerActive);

            if (mousePicker && mousePickerRT && pickerActive)
                mousePickerRT.anchoredPosition = Input.mousePosition * CoreHelper.ScreenScaleInverse;
        }

        void UpdateTooltip()
        {
            tooltipTime = Time.time - tooltipTimeOffset;

            if (!mouseTooltip)
                return;

            if (showTootip && tooltipTime >= EditorConfig.Instance.MouseTooltipHoverTime.Value)
            {
                showTootip = false;
                tooltipActive = true;

                mouseTooltip.SetActive(true);

                if (mouseTooltipText)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(mouseTooltipText.rectTransform);
                if (mouseTooltipRT)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(mouseTooltipRT);
            }

            if (tooltipActive)
            {
                float num = CoreHelper.ScreenScaleInverse;
                float x = mouseTooltipRT.sizeDelta.x;
                float y = mouseTooltipRT.sizeDelta.y;
                var tooltipOffset = Vector3.zero;

                // flips tooltip if mouse is close to the edge of the screen.
                if ((Input.mousePosition.x + x) * num >= 1920f)
                    tooltipOffset.x -= x + 8f;
                else
                    tooltipOffset.x = 8f;

                // flips tooltip if mouse is close to the edge of the screen.
                if ((Input.mousePosition.y + y) * num >= 1080f)
                    tooltipOffset.y -= y;

                var position = (Input.mousePosition + tooltipOffset) * num;
                position.x = Mathf.Clamp(position.x, 40f, 1880f);
                position.y = Mathf.Clamp(position.y, 40f, 1040f);
                mouseTooltipRT.anchoredPosition = position;
            }

            if (tooltipTime - EditorConfig.Instance.MouseTooltipHoverTime.Value > maxTooltipTime && tooltipActive)
            {
                tooltipActive = false;
                mouseTooltip.SetActive(false);
            }

            if (!EditorConfig.Instance.MouseTooltipDisplay.Value || !EditorManager.inst.showHelp && EditorConfig.Instance.MouseTooltipRequiresHelp.Value)
                mouseTooltip.SetActive(false);
        }

        void UpdatePreview()
        {
            if (RTLevel.Current && RTLevel.Current.eventEngine)
            {
                if (MouseOverPreview)
                {
                    if (Input.GetMouseButtonDown((int)PointerEventData.InputButton.Middle))
                    {
                        draggingCamera = true;
                        startDragPos = Input.mousePosition;
                        cachePos = RTLevel.Current.eventEngine.editorCamPosition;

                        if (EditorConfig.Instance.EnableEditorCameraOnDrag.Value)
                            Freecam = true;
                    }

                    if (Freecam)
                    {
                        var amount = Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 10f : Input.GetKey(EditorConfig.Instance.ScrollwheelSmallAmountKey.Value) ? 1f : 5f;

                        if (Input.mouseScrollDelta.y > 0f)
                            RTLevel.Current.eventEngine.editorCamZoom -= amount;
                        if (Input.mouseScrollDelta.y < 0f)
                            RTLevel.Current.eventEngine.editorCamZoom += amount;
                    }
                }

                if (!Input.GetMouseButton((int)PointerEventData.InputButton.Middle) || !Freecam)
                    draggingCamera = false;

                if (draggingCamera)
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                    {
                        var amount = Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 40f : Input.GetKey(EditorConfig.Instance.ScrollwheelSmallAmountKey.Value) ? 500f : 100f;

                        var val = RTLevel.Current.eventEngine.editorCamZoom / amount;
                        var mousePosition = (startDragPos - (Vector2)Input.mousePosition) * val;
                        RTLevel.Current.eventEngine.editorCamRotate += mousePosition.x;
                        startDragPos = Input.mousePosition;
                    }
                    else
                    {
                        var amount = Input.GetKey(EditorConfig.Instance.ScrollwheelLargeAmountKey.Value) ? 40f : Input.GetKey(EditorConfig.Instance.ScrollwheelSmallAmountKey.Value) ? 500f : 100f;

                        var val = RTLevel.Current.eventEngine.editorCamZoom / amount;
                        var mousePosition = (startDragPos - (Vector2)Input.mousePosition) * val;
                        RTLevel.Current.eventEngine.editorCamPosition += mousePosition;
                        startDragPos = Input.mousePosition;
                    }
                }
            }

            try
            {
                if (previewGrid)
                {
                    var enabled = EditorConfig.Instance.PreviewGridEnabled.Value && EditorManager.inst.isEditing;
                    previewGrid.enabled = enabled;

                    if (enabled)
                    {
                        var camPos = EventManager.inst.camPos;
                        previewGrid.rectTransform.anchoredPosition =
                            new Vector2(-40f / previewGrid.gridSize.x, -40f / previewGrid.gridSize.y) + new Vector2((int)(camPos.x / 40f) * 40f, (int)(camPos.y / 40f) * 40f);
                    }
                }
            }
            catch
            {

            }

            if (GameManager.inst.timeline && timelinePreview)
                timelinePreview.gameObject.SetActive(GameManager.inst.timeline.activeSelf);

            if (CoreHelper.Playing && timelinePreview && AudioManager.inst.CurrentAudioSource.clip != null && GameManager.inst.timeline && GameManager.inst.timeline.activeSelf)
            {
                float num = AudioManager.inst.CurrentAudioSource.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                if (timelinePosition)
                    timelinePosition.anchoredPosition = new Vector2(num, 0f);

                timelinePreview.localPosition = GameManager.inst.timeline.transform.localPosition;
                timelinePreview.localScale = GameManager.inst.timeline.transform.localScale;
                timelinePreview.localRotation = GameManager.inst.timeline.transform.localRotation;

                for (int i = 0; i < checkpointImages.Count; i++)
                {
                    if (RTGameManager.inst.checkpointImages.Count > i)
                        checkpointImages[i].color = RTGameManager.inst.checkpointImages[i].color;
                }

                timelinePreviewPlayer.color = RTGameManager.inst.timelinePlayer.color;
                timelinePreviewLeftCap.color = RTGameManager.inst.timelineLeftCap.color;
                timelinePreviewRightCap.color = RTGameManager.inst.timelineRightCap.color;
                timelinePreviewLine.color = RTGameManager.inst.timelineLine.color;
            }
        }

        void UpdateKey()
        {
            if (!selectingKey)
                return;

            var key = CoreHelper.GetKeyCodeDown();

            if (key == KeyCode.None)
                return;

            selectingKey = false;

            setKey?.Invoke(key);
        }

        void UpdateCameraArea()
        {
            var enabled = Freecam && EditorConfig.Instance.ShowCameraArea.Value && EditorManager.inst.hasLoadedLevel;
            if (editorCameraEnabled != enabled)
            {
                editorCameraEnabled = enabled;
                cameraArea.gameObject.SetActive(enabled);
            }

            if (!enabled || !EventManager.inst || !RTEventManager.inst || !RTEventManager.inst.delayTracker || !RTLevel.Current || !RTLevel.Current.eventEngine)
                return;

            cameraAreaCanvas.anchoredPosition = (Vector2)RTEventManager.inst.delayTracker.target + EventManager.inst.camPos + (Vector2)RTLevel.Current.eventEngine.GetShake() + new Vector2(RTLevel.Current.eventEngine.camOffsetX, RTLevel.Current.eventEngine.camOffsetY);
            cameraArea.sizeDelta = new Vector2(96f, 54f) * 12f * (EventManager.inst.camZoom / 20f);
            cameraArea.localEulerAngles = new Vector3(0f, 0f, EventManager.inst.camRot);
        }

        #endregion

        #region Constants

        /// <summary>
        /// Default object cameo
        /// </summary>
        public const string DEFAULT_OBJECT_NAME = "\"Default object cameo\" -Viral Mecha";

        /// <summary>
        /// Represents the local system browser (e.g. File Explorer)
        /// </summary>
        public const string SYSTEM_BROWSER = "System Browser";

        /// <summary>
        /// Represents the built-in file browser.
        /// </summary>
        public const string EDITOR_BROWSER = "Editor Browser";

        #endregion

        #region Variables

        #region Misc

        public bool Freecam { get; set; }

        /// <summary>
        /// Custom editor thread for performing larger tasks.
        /// </summary>
        public Core.Threading.TickRunner editorThread;

        /// <summary>
        /// If the mouse cursor is over the level preview.
        /// </summary>
        public static bool MouseOverPreview => RTLevel.Cameras.FG.rect.Contains(new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height));

        /// <summary>
        /// A list of easing dropdowns.
        /// </summary>
        public List<Dropdown> EasingDropdowns { get; set; } = new List<Dropdown>();

        public List<EasingOption> EasingOptions { get; set; } = new List<EasingOption>();

        public LayoutValues EasingDropdownLayoutGroupValues { get; set; }

        public RectValues EasingDropdownTemplateRectValues { get; set; } = new RectValues
        {
            anchoredPosition = new Vector2(0f, 2f),
            anchorMax = new Vector2(1f, 0f),
            anchorMin = Vector2.zero,
            pivot = new Vector2(0.5f, 1f),
            sizeDelta = new Vector2(0f, 140f),
        };

        /// <summary>
        /// If advanced features should display.
        /// </summary>
        public static bool ShowModdedUI { get; set; }

        /// <summary>
        /// If the editor shouldn't be simple.
        /// </summary>
        public static bool NotSimple => EditorConfig.Instance.EditorComplexity.Value != Complexity.Simple;

        /// <summary>
        /// Hides the preview area until a level is loaded.
        /// </summary>
        public EditorThemeManager.Element PreviewCover { get; set; }

        /// <summary>
        /// Helper component for object selection in preview.
        /// </summary>
        public SelectObjectHelper SelectObjectHelper { get; set; }

        /// <summary>
        /// Grid of the preview area.
        /// </summary>
        public GridRenderer previewGrid;

        public GameObject timeDefault;

        public bool ienumRunning;

        /// <summary>
        /// The top panel of the editor with the dropdowns.
        /// </summary>
        public Transform titleBar;

        public FunctionButtonStorage undoButton;
        public FunctionButtonStorage redoButton;

        public InputField folderCreatorName;
        Text folderCreatorTitle;
        Text folderCreatorNameLabel;
        Button folderCreatorSubmit;
        Text folderCreatorSubmitText;

        GameObject fontSelectionPrefab;

        RectTransform cameraAreaCanvas;
        RectTransform cameraArea;
        bool editorCameraEnabled;

        #endregion

        #region Popups

        public Transform popups;

        public List<EditorPopup> editorPopups = new List<EditorPopup>();

        public InfoPopup InfoPopup { get; set; }

        public ContentPopup ParentSelectorPopup { get; set; }

        public EditorPopup SaveAsPopup { get; set; }

        public DoubleContentPopup PrefabPopups { get; set; }

        public EditorPopup ObjectOptionsPopup { get; set; }
        public EditorPopup BGObjectOptionsPopup { get; set; }

        public ContentPopup ObjectTemplatePopup { get; set; }

        public ContentPopup DebuggerPopup { get; set; }

        public ContentPopup AutosavePopup { get; set; }

        public ContentPopup DocumentationPopup { get; set; }

        public EditorPopup WarningPopup { get; set; }

        public EditorPopup BrowserPopup { get; set; }

        public EditorPopup NamePopup { get; set; }

        public ContentPopup PrefabTypesPopup { get; set; }

        public ContentPopup FontSelectorPopup { get; set; }

        public ProgressPopup ProgressPopup { get; set; }

        #endregion

        #region Dragging

        public float dragOffset = -1f;
        public int dragBinOffset = -100;

        public static bool DraggingPlaysSound { get; set; }
        public static bool DraggingPlaysSoundBPM { get; set; }

        bool draggingCamera;
        Vector2 startDragPos;
        Vector2 cachePos;

        #endregion

        #region Loading & sorting

        public bool canUpdateThemes = true;
        public bool canUpdatePrefabs = true;

        #endregion

        #region Mouse Picker & Tooltip

        // tootlip

        public bool showTootip;

        public TextMeshProUGUI tooltipText;

        public bool tooltipActive;
        public float tooltipTime;
        public float tooltipTimeOffset;
        public float maxTooltipTime = 2f;

        public GameObject mouseTooltip;
        public RectTransform mouseTooltipRT;
        public TextMeshProUGUI mouseTooltipText;

        // picker

        public GameObject mousePicker;
        RectTransform mousePickerRT;

        public bool parentPickerEnabled;
        public bool prefabPickerEnabled;
        public bool selectingMultiple;

        #endregion

        #region Timeline Bar

        /// <summary>
        /// The main editor toolbar.
        /// </summary>
        public GameObject timelineBar;

        /// <summary>
        /// The modded song time field.
        /// </summary>
        public InputField timeField;

        /// <summary>
        /// The vanilla song time button text.
        /// </summary>
        public Text timelineTime;

        /// <summary>
        /// The modded audio pitch field.
        /// </summary>
        public InputField pitchField;

        /// <summary>
        /// The event layer toggle. If on, renders <see cref="LayerType.Events"/>, otherwise renders <see cref="LayerType.Objects"/>.
        /// </summary>
        public Toggle eventLayerToggle;

        #endregion

        #region Key selection

        /// <summary>
        /// If keyboard is currently being checked for any input.
        /// </summary>
        public bool selectingKey = false;

        /// <summary>
        /// Action to run when a key is selected
        /// </summary>
        public Action<KeyCode> setKey;

        #endregion

        #region Game Timeline

        public Image timelinePreviewPlayer;
        public Image timelinePreviewLine;
        public Image timelinePreviewLeftCap;
        public Image timelinePreviewRightCap;
        public List<Image> checkpointImages = new List<Image>();

        public Transform timelinePreview;
        public RectTransform timelinePosition;

        #endregion

        #region Debugger

        public List<string> debugs = new List<string>();
        public List<GameObject> customFunctions = new List<GameObject>();
        public string debugSearch;

        #endregion

        #region Screenshots

        public Transform screenshotContent;
        public InputField screenshotPageField;

        public int screenshotPage;
        public int screenshotsPerPage = 5;

        public int CurrentScreenshotPage => screenshotPage + 1;
        public int MinScreenshots => MaxScreenshots - screenshotsPerPage;
        public int MaxScreenshots => CurrentScreenshotPage * screenshotsPerPage;

        public int screenshotCount;

        #endregion

        #region Editor Layer

        public List<EditorLayer> EditorLayers { get; set; } = new List<EditorLayer>();

        public class EditorLayer : Exists
        {
            public EditorLayer() { }

            public EditorLayer(int layer, ThemeGroup themeGroup, string color)
            {
                this.layer = layer;
                this.themeGroup = themeGroup;
                this.color = color;
            }

            public int layer;
            public ThemeGroup themeGroup;
            public string color;
        }

        #endregion

        #endregion

        #region Settings

        public EditorInfo editorInfo = new EditorInfo();

        /// <summary>
        /// Saves the current editor settings.
        /// </summary>
        public void SaveSettings()
        {
            if (!editorInfo)
                editorInfo = new EditorInfo();

            editorInfo.ApplyFrom();
            editorInfo.WriteToFile(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE));
        }

        /// <summary>
        /// Loads the current levels' editor settings.
        /// </summary>
        public void LoadSettings()
        {
            if (!RTFile.FileExists(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE)))
            {
                editorInfo = new EditorInfo();
                editorInfo.timer.Reset();

                return;
            }

            try
            {
                editorInfo = RTFile.CreateFromFile<EditorInfo>(RTFile.CombinePaths(RTFile.BasePath, Level.EDITOR_LSE));
                editorInfo.ApplyTo();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }

            EditorTimeline.inst.prevLayer = EditorTimeline.inst.Layer;
            EditorTimeline.inst.prevLayerType = EditorTimeline.inst.layerType;

            EditorTimeline.inst.SetTimelineGridSize();
        }

        #endregion

        #region Notifications

        /// <summary>
        /// List of all current notifications.
        /// </summary>
        public List<string> notifications = new List<string>();

        /// <summary>
        /// The parent for the notifications.
        /// </summary>
        public RectTransform notificationsParent;

        /// <summary>
        /// Displays an editor notification.
        /// </summary>
        /// <param name="name">Name of the notification.</param>
        /// <param name="text">Text to display.</param>
        /// <param name="time">Time the notification should be on screen for.</param>
        /// <param name="type">Type of notification to spawn.</param>
        public void DisplayNotification(string name, string text, float time, EditorManager.NotificationType type) => StartCoroutine(DisplayNotificationLoop(name, text, time, type));

        /// <summary>
        /// Displays a custom notification.
        /// </summary>
        /// <param name="name">Name of the notification.</param>
        /// <param name="text">Text to display.</param>
        /// <param name="time">Time the notification should be on screen for.</param>
        /// <param name="baseColor">Color of the notification base.</param>
        /// <param name="panelColor">Color of the notification panel.</param>
        /// <param name="iconColor">Color of the notification icon.</param>
        /// <param name="title">Title of the notification.</param>
        /// <param name="icon">Icon of the notification.</param>
        public void DisplayCustomNotification(string name, string text, float time, Color baseColor, Color panelColor, Color iconColor, string title, Sprite icon = null) => StartCoroutine(DisplayCustomNotificationLoop(name, text, time, baseColor, panelColor, iconColor, title, icon));

        /// <summary>
        /// Rebuilds the notification layout in case it breaks.
        /// </summary>
        public void RebuildNotificationLayout()
        {
            try
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.AsRT());
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"There was some sort of error with rebuilding the layout. {ex}");
            }
        }

        /// <summary>
        /// Updates the notification settings.
        /// </summary>
        public void UpdateNotificationConfig()
        {
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notificationsParent.sizeDelta = new Vector2(EditorConfig.Instance.NotificationWidth.Value, 632f);
            EditorManager.inst.notification.transform.localScale = new Vector3(EditorConfig.Instance.NotificationSize.Value, EditorConfig.Instance.NotificationSize.Value, 1f);

            var direction = EditorConfig.Instance.NotificationDirection.Value;

            notificationsParent.anchoredPosition = new Vector2(8f, direction == VerticalDirection.Up ? 408f : 410f);
            notifyGroup.childAlignment = direction != VerticalDirection.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
        }

        #region Internal

        IEnumerator DisplayNotificationLoop(string name, string text, float time, EditorManager.NotificationType type)
        {
            Debug.Log("<color=#F6AC1A>Editor</color><color=#2FCBD6>Management</color>\nNotification: " + name + "\nText: " + text + "\nTime: " + time + "\nType: " + type);

            if (!notifications.Contains(name) && notifications.Count < 20 && EditorConfig.Instance.NotificationsDisplay.Value)
            {
                var notif = Instantiate(EditorManager.inst.notificationPrefabs[(int)type], Vector3.zero, Quaternion.identity);
                Destroy(notif, time * EditorConfig.Instance.NotificationDisplayTime.Value);

                Graphic textComponent = type == EditorManager.NotificationType.Info ? notif.transform.Find("text").GetComponent<TextMeshProUGUI>() : notif.transform.Find("text").GetComponent<Text>();

                if (type == EditorManager.NotificationType.Info)
                    ((TextMeshProUGUI)textComponent).text = text;
                else
                    ((Text)textComponent).text = text;

                notif.transform.SetParent(EditorManager.inst.notification.transform);
                if (EditorConfig.Instance.NotificationDirection.Value == VerticalDirection.Down)
                    notif.transform.SetAsFirstSibling();
                notif.transform.localScale = Vector3.one;

                EditorThemeManager.ApplyGraphic(notif.GetComponent<Image>(), ThemeGroup.Notification_Background, true);
                EditorThemeManager.ApplyGraphic(notif.transform.Find("bg/bg").GetComponent<Image>(), EditorTheme.GetGroup($"Notification {type}"), true, roundedSide: SpriteHelper.RoundedSide.Top);
                EditorThemeManager.ApplyGraphic(textComponent, ThemeGroup.Light_Text);
                EditorThemeManager.ApplyGraphic(notif.transform.Find("bg/Image").GetComponent<Image>(), ThemeGroup.Light_Text);
                EditorThemeManager.ApplyLightText(notif.transform.Find("bg/title").GetComponent<Text>());

                RebuildNotificationLayout();

                notifications.Add(name);

                yield return CoroutineHelper.Seconds(time * EditorConfig.Instance.NotificationDisplayTime.Value);
                notifications.Remove(name);
            }
            yield break;
        }

        IEnumerator DisplayCustomNotificationLoop(string name, string text, float time, Color baseColor, Color topColor, Color iconCOlor, string _title, Sprite _icon = null)
        {
            if (!notifications.Contains(name) && notifications.Count < 20 && EditorConfig.Instance.NotificationsDisplay.Value)
            {
                notifications.Add(name);
                var gameObject = Instantiate(EditorManager.inst.notificationPrefabs[0], Vector3.zero, Quaternion.identity);
                Destroy(gameObject, time * EditorConfig.Instance.NotificationDisplayTime.Value);
                gameObject.transform.Find("text").GetComponent<TextMeshProUGUI>().text = text;
                gameObject.transform.SetParent(EditorManager.inst.notification.transform);
                if (EditorConfig.Instance.NotificationDirection.Value == VerticalDirection.Down)
                    gameObject.transform.SetAsFirstSibling();
                gameObject.transform.localScale = Vector3.one;

                gameObject.GetComponent<Image>().color = baseColor;
                var bg = gameObject.transform.Find("bg");
                var img = bg.Find("Image").GetComponent<Image>();
                bg.Find("bg").GetComponent<Image>().color = topColor;
                if (_icon != null)
                    img.sprite = _icon;

                img.color = iconCOlor;
                bg.Find("title").GetComponent<Text>().text = _title;

                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info/text").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.Find("info").AsRT());
                LayoutRebuilder.ForceRebuildLayoutImmediate(EditorManager.inst.notification.transform.AsRT());

                yield return CoroutineHelper.Seconds(time * EditorConfig.Instance.NotificationDisplayTime.Value);
                notifications.Remove(name);
            }

            yield break;
        }

        void SetupNotificationValues()
        {
            var tooltip = EditorManager.inst.tooltip.transform.parent.gameObject;
            EditorThemeManager.AddGraphic(tooltip.GetComponent<Image>(), ThemeGroup.Notification_Background, true);
            EditorThemeManager.AddGraphic(tooltip.transform.Find("bg/bg").GetComponent<Image>(), ThemeGroup.Notification_Info, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddLightText(tooltipText);
            EditorThemeManager.AddGraphic(tooltip.transform.Find("bg/Image").GetComponent<Image>(), ThemeGroup.Light_Text);
            EditorThemeManager.AddLightText(tooltip.transform.Find("bg/title").GetComponent<Text>());

            UpdateNotificationConfig();
        }

        #endregion

        #endregion

        #region Paths

        #region Values

        public const string DEFAULT_EXPORTS_PATH = "beatmaps/exports";

        /// <summary>
        /// Base path to the game.
        /// </summary>
        public string BasePath { get; set; } = RTFile.ApplicationDirectory;

        /// <summary>
        /// The beatmaps folder that contains all PA directories and files.
        /// </summary>
        public string beatmapsFolder = "beatmaps";

        /// <summary>
        /// The beatmaps path that contains all PA directories and files.
        /// </summary>
        public string BeatmapsPath => RTFile.CombinePaths(BasePath, beatmapsFolder);

        /// <summary>
        /// Watches the current prefab folder for changes. If any changes are made, update the prefab list.
        /// </summary>
        public FileSystemWatcher PrefabWatcher { get; set; }

        /// <summary>
        /// Watches the current theme folder for changes. If any changes are made, update the theme list.
        /// </summary>
        public FileSystemWatcher ThemeWatcher { get; set; }

        /// <summary>
        /// The level sort.
        /// </summary>
        public LevelSort levelSort = 0;

        /// <summary>
        /// If the level sort should ascend.
        /// </summary>
        public bool levelAscend = true;

        public string EditorSettingsPath => RTFile.CombinePaths(RTFile.ApplicationDirectory, $"settings/editor{FileFormat.LSS.Dot()}");

        /// <summary>
        /// The path editor levels should load from.
        /// </summary>
        public string EditorPath
        {
            get => editorPath;
            set => editorPath = value;
        }
        string editorPath = "editor";

        /// <summary>
        /// The path themes should load from.
        /// </summary>
        public string ThemePath
        {
            get => themePath;
            set => themePath = value;
        }
        string themePath = "themes";

        /// <summary>
        /// The path prefabs should load from.
        /// </summary>
        public string PrefabPath
        {
            get => prefabPath;
            set => prefabPath = value;
        }
        string prefabPath = "prefabs";

        /// <summary>
        /// The path prefab types should load from.
        /// </summary>
        public string PrefabTypePath
        {
            get => prefabTypePath;
            set => prefabTypePath = value;
        }
        string prefabTypePath = "prefabtypes";

        /// <summary>
        /// The path player models should load from.
        /// </summary>
        public string PlayersPath
        {
            get => playersPath;
            set => playersPath = value;
        }
        string playersPath = "players";

        /// <summary>
        /// The path planners should load from.
        /// </summary>
        public string PlannersPath
        {
            get => plannersPath;
            set => plannersPath = value;
        }
        string plannersPath = "planners";

        /// <summary>
        /// The path level collections should load from.
        /// </summary>
        public string CollectionsPath
        {
            get => collectionsPath;
            set => collectionsPath = value;
        }
        string collectionsPath = "collections";

        #endregion

        #region Functions

        /// <summary>
        /// Resets the base path and reloads all files.
        /// </summary>
        public void ResetBasePath() => SetBasePath(RTFile.ApplicationDirectory);

        /// <summary>
        /// Sets the base path and reloads all files.
        /// </summary>
        /// <param name="basePath">Base path to load.</param>
        /// <param name="beatmapsFolder">Beatmaps folder that contains the editor, prefab, etc folders.</param>
        public void SetBasePath(string basePath, string beatmapsFolder = "beatmaps")
        {
            BasePath = RTFile.DirectoryExists(basePath) ? basePath : RTFile.ApplicationDirectory;
            this.beatmapsFolder = beatmapsFolder;

            EditorLevelManager.inst.OpenLevelPopup.PathField.text = "editor";
            UpdateEditorPath(false);
            RTThemeEditor.inst.Popup.PathField.text = "themes";
            UpdateThemePath(false);
            PrefabPopups.External.PathField.text = "prefabs";
            UpdatePrefabPath(false);

            CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.LoadPrefabTypes());
            if (EditorManager.inst.hasLoadedLevel)
                PlayerEditor.inst.Reload();
            ProjectPlanner.inst.Load();

            SaveGlobalSettings();
        }

        /// <summary>
        /// Updates the editor list and path.
        /// </summary>
        /// <param name="forceReload">If function should be forced to run.</param>
        public void UpdateEditorPath(bool forceReload)
        {
            if (!forceReload && !EditorConfig.Instance.SettingPathReloads.Value || EditorPath[EditorPath.Length - 1] == '/')
                return;

            var editorPath = RTFile.CombinePaths(BeatmapsPath, EditorPath);
            if (!RTFile.DirectoryExists(editorPath))
            {
                EditorLevelManager.inst.OpenLevelPopup.PathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    RTFile.CreateDirectory(editorPath);

                    SaveGlobalSettings();

                    EditorLevelManager.inst.LoadLevels();

                    HideWarningPopup();
                    EditorLevelManager.inst.OpenLevelPopup.PathField.interactable = true;
                },
                () =>
                {
                    HideWarningPopup();
                    EditorLevelManager.inst.OpenLevelPopup.PathField.interactable = true;
                });

                return;
            }

            SaveGlobalSettings();

            EditorLevelManager.inst.LoadLevels();
        }

        /// <summary>
        /// Updates the theme list and path.
        /// </summary>
        /// <param name="forceReload">If function should be forced to run.</param>
        public void UpdateThemePath(bool forceReload)
        {
            if (!forceReload && !EditorConfig.Instance.SettingPathReloads.Value || ThemePath[ThemePath.Length - 1] == '/')
                return;

            var themePath = RTFile.CombinePaths(BeatmapsPath, ThemePath);
            if (!RTFile.DirectoryExists(themePath))
            {
                RTThemeEditor.inst.Popup.PathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    RTFile.CreateDirectory(themePath);

                    SaveGlobalSettings();

                    CoroutineHelper.StartCoroutine(RTThemeEditor.inst.LoadThemes());
                    EventEditor.inst.RenderEventsDialog();

                    HideWarningPopup();
                    RTThemeEditor.inst.Popup.PathField.interactable = true;
                }, () =>
                {
                    HideWarningPopup();
                    RTThemeEditor.inst.Popup.PathField.interactable = true;
                });

                return;
            }

            SaveGlobalSettings();

            CoroutineHelper.StartCoroutine(RTThemeEditor.inst.LoadThemes());
            EventEditor.inst.RenderEventsDialog();
        }

        /// <summary>
        /// Updates the prefab list and path.
        /// </summary>
        /// <param name="forceReload">If function should be forced to run.</param>
        public void UpdatePrefabPath(bool forceReload)
        {
            if (!forceReload && !EditorConfig.Instance.SettingPathReloads.Value || PrefabPath[PrefabPath.Length - 1] == '/')
                return;

            var prefabPath = RTFile.CombinePaths(BeatmapsPath, PrefabPath);
            if (!RTFile.DirectoryExists(prefabPath))
            {
                PrefabPopups.External.PathField.interactable = false;
                ShowWarningPopup("No directory exists for this path. Do you want to create a new folder?", () =>
                {
                    RTFile.CreateDirectory(prefabPath);

                    SaveGlobalSettings();

                    RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs);

                    HideWarningPopup();
                    PrefabPopups.External.PathField.interactable = true;
                }, () =>
                {
                    HideWarningPopup();
                    PrefabPopups.External.PathField.interactable = true;
                });

                return;
            }

            SaveGlobalSettings();

            RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs);
        }

        /// <summary>
        /// Updates the level sort dropdown.
        /// </summary>
        public void UpdateOrderDropdown()
        {
            if (!EditorLevelManager.inst || !EditorLevelManager.inst.OpenLevelPopup || !EditorLevelManager.inst.OpenLevelPopup.SortDropdown)
                return;

            EditorLevelManager.inst.OpenLevelPopup.SortDropdown.SetValueWithoutNotify((int)levelSort);
            EditorLevelManager.inst.OpenLevelPopup.SortDropdown.onValueChanged.NewListener(_val =>
            {
                levelSort = (LevelSort)_val;
                EditorLevelManager.inst.RenderLevels();
                SaveGlobalSettings();
            });
        }

        /// <summary>
        /// Updates the level ascend toggle.
        /// </summary>
        public void UpdateAscendToggle()
        {
            if (!EditorLevelManager.inst || !EditorLevelManager.inst.OpenLevelPopup || !EditorLevelManager.inst.OpenLevelPopup.AscendToggle)
                return;

            EditorLevelManager.inst.OpenLevelPopup.AscendToggle.SetIsOnWithoutNotify(levelAscend);
            EditorLevelManager.inst.OpenLevelPopup.AscendToggle.onValueChanged.NewListener(_val =>
            {
                levelAscend = _val;
                EditorLevelManager.inst.RenderLevels();
                SaveGlobalSettings();
            });
        }

        /// <summary>
        /// Creates the global settings if it doesn't exist.
        /// </summary>
        public void CreateGlobalSettings()
        {
            if (RTFile.FileExists(EditorSettingsPath))
                return;

            var jn = JSON.Parse("{}");

            jn["sort"]["asc"] = "True";
            jn["sort"]["order"] = "0";

            EditorPath = "editor";
            jn["paths"]["editor"] = EditorPath;

            ThemePath = "themes";
            jn["paths"]["themes"] = ThemePath;

            PrefabPath = "prefabs";
            jn["paths"]["prefabs"] = PrefabPath;

            for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);

            EditorManager.inst.layerColors.RemoveAt(5);
            for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);

            if (EditorServerManager.inst)
            {
                jn["server"]["current_tab"] = (int)EditorServerManager.inst.tab;

                foreach (var tabSettingKeyValuePair in EditorServerManager.inst.tabSettings)
                {
                    var tabSettingJN = Parser.NewJSONObject();
                    var tabSetting = tabSettingKeyValuePair.Value;

                    tabSettingJN["sort"]["asc"] = tabSetting.ascend;
                    tabSettingJN["sort"]["sort"] = tabSetting.sort;
                    tabSettingJN["uploaded"] = tabSetting.uploaded;
                    tabSettingJN["page"] = tabSetting.page;

                    jn["server"]["tabs"][tabSettingKeyValuePair.Key.ToString().ToLower()] = tabSettingJN;
                }
            }

            RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
        }

        /// <summary>
        /// Loads the global editor settings.
        /// </summary>
        public void LoadGlobalSettings()
        {
            if (!RTFile.FileExists(EditorSettingsPath))
                return;

            var jn = JSON.Parse(RTFile.ReadFromFile(EditorSettingsPath));

            if (!string.IsNullOrEmpty(jn["sort"]["asc"]))
                levelAscend = jn["sort"]["asc"].AsBool;
            if (!string.IsNullOrEmpty(jn["sort"]["order"]))
                levelSort = (LevelSort)jn["sort"]["order"].AsInt;

            UpdateOrderDropdown();
            UpdateAscendToggle();

            BasePath = RTFile.DirectoryExists(jn["paths"]["base"]) ? jn["paths"]["base"] : RTFile.ApplicationDirectory;
            beatmapsFolder = !string.IsNullOrEmpty(jn["paths"]["beatmaps"]) ? jn["paths"]["beatmaps"] : "beatmaps";

            if (!string.IsNullOrEmpty(jn["paths"]["editor"]))
                EditorPath = jn["paths"]["editor"];
            if (!string.IsNullOrEmpty(jn["paths"]["themes"]))
                ThemePath = jn["paths"]["themes"];
            if (!string.IsNullOrEmpty(jn["paths"]["prefabs"]))
                PrefabPath = jn["paths"]["prefabs"];
            if (!string.IsNullOrEmpty(jn["paths"]["prefab_types"]))
                PrefabTypePath = jn["paths"]["prefab_types"];
            if (!string.IsNullOrEmpty(jn["paths"]["players"]))
                PlayersPath = jn["paths"]["players"];
            if (!string.IsNullOrEmpty(jn["paths"]["planners"]))
                PlannersPath = jn["paths"]["planners"];

            RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, EditorPath));
            RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, ThemePath));
            RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, PrefabPath));

            SetWatcherPaths();

            if (jn["marker_colors"] != null)
            {
                MarkerEditor.inst.markerColors.Clear();
                for (int i = 0; i < jn["marker_colors"].Count; i++)
                    MarkerEditor.inst.markerColors.Add(LSColors.HexToColor(jn["marker_colors"][i]));
            }
            else
            {
                for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                    jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }

            if (jn["layer_colors"] != null)
            {
                EditorManager.inst.layerColors.Clear();
                for (int i = 0; i < jn["layer_colors"].Count; i++)
                    EditorManager.inst.layerColors.Add(LSColors.HexToColor(jn["layer_colors"][i]));
            }
            else
            {
                for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                    jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);

                RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
            }

            if (jn["server"] != null && EditorServerManager.inst)
            {
                EditorServerManager.inst.tab = (EditorServerManager.Tab)jn["server"]["current_tab"].AsInt;

                foreach (var tabSettingKeyValuePair in EditorServerManager.inst.tabSettings)
                {
                    var tabSettingJN = jn["server"]["tabs"][tabSettingKeyValuePair.Key.ToString().ToLower()];
                    if (tabSettingJN == null)
                        continue;

                    var tabSetting = tabSettingKeyValuePair.Value;

                    if (tabSettingJN["sort"]["asc"] != null)
                        tabSetting.ascend = tabSettingJN["sort"]["asc"].AsBool;
                    if (tabSettingJN["sort"]["sort"] != null)
                        tabSetting.sort = tabSettingJN["sort"]["sort"].AsInt;
                    if (tabSettingJN["uploaded"] != null)
                        tabSetting.uploaded = tabSettingJN["uploaded"].AsBool;
                    if (tabSettingJN["page"] != null)
                        tabSetting.page = tabSettingJN["page"].AsInt;
                }
            }
        }

        /// <summary>
        /// Saves the global editor settings.
        /// </summary>
        public void SaveGlobalSettings()
        {
            var jn = Parser.NewJSONObject();

            jn["sort"]["asc"] = levelAscend.ToString();
            jn["sort"]["order"] = ((int)levelSort).ToString();

            if (!string.IsNullOrEmpty(BasePath) && BasePath != RTFile.ApplicationDirectory)
                jn["paths"]["base"] = BasePath;
            if (beatmapsFolder != "beatmaps")
                jn["paths"]["beatmaps"] = beatmapsFolder;

            jn["paths"]["editor"] = EditorPath;
            jn["paths"]["themes"] = ThemePath;
            jn["paths"]["prefabs"] = PrefabPath;
            jn["paths"]["prefab_types"] = PrefabTypePath;
            jn["paths"]["players"] = PlayersPath;
            jn["paths"]["planners"] = PlannersPath;

            SetWatcherPaths();

            for (int i = 0; i < MarkerEditor.inst.markerColors.Count; i++)
                jn["marker_colors"][i] = LSColors.ColorToHex(MarkerEditor.inst.markerColors[i]);

            for (int i = 0; i < EditorManager.inst.layerColors.Count; i++)
                jn["layer_colors"][i] = LSColors.ColorToHex(EditorManager.inst.layerColors[i]);

            if (EditorServerManager.inst)
            {
                jn["server"]["current_tab"] = (int)EditorServerManager.inst.tab;

                foreach (var tabSettingKeyValuePair in EditorServerManager.inst.tabSettings)
                {
                    var tabSettingJN = Parser.NewJSONObject();
                    var tabSetting = tabSettingKeyValuePair.Value;

                    tabSettingJN["sort"]["asc"] = tabSetting.ascend;
                    tabSettingJN["sort"]["sort"] = tabSetting.sort;
                    tabSettingJN["uploaded"] = tabSetting.uploaded;
                    tabSettingJN["page"] = tabSetting.page;

                    jn["server"]["tabs"][tabSettingKeyValuePair.Key.ToString().ToLower()] = tabSettingJN;
                }
            }

            RTFile.WriteToFile(EditorSettingsPath, jn.ToString(3));
        }

        /// <summary>
        /// Disables the prefab file watcher.
        /// </summary>
        public void DisablePrefabWatcher()
        {
            canUpdatePrefabs = false;
            PrefabWatcher.EnableRaisingEvents = false;
            try
            {
                PrefabWatcher.Changed -= OnPrefabPathChanged;
                PrefabWatcher.Created -= OnPrefabPathChanged;
                PrefabWatcher.Deleted -= OnPrefabPathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't remove OnPrefabPathChanged method.\nException: {ex}");
            }
        }

        /// <summary>
        /// Enables the prefab file watcher.
        /// </summary>
        public void EnablePrefabWatcher()
        {
            try
            {
                PrefabWatcher.Changed += OnPrefabPathChanged;
                PrefabWatcher.Created += OnPrefabPathChanged;
                PrefabWatcher.Deleted += OnPrefabPathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't add OnPrefabPathChanged method.\nException: {ex}");
            }
            PrefabWatcher.EnableRaisingEvents = true;
        }
        
        /// <summary>
        /// Disables the theme file watcher.
        /// </summary>
        public void DisableThemeWatcher()
        {
            canUpdateThemes = false;
            ThemeWatcher.EnableRaisingEvents = false;
            try
            {
                ThemeWatcher.Changed -= OnThemePathChanged;
                ThemeWatcher.Created -= OnThemePathChanged;
                ThemeWatcher.Deleted -= OnThemePathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't remove OnThemePathChanged method.\nException: {ex}");
            }
        }

        /// <summary>
        /// Enables the theme file watcher.
        /// </summary>
        public void EnableThemeWatcher()
        {
            try
            {
                ThemeWatcher.Changed += OnThemePathChanged;
                ThemeWatcher.Created += OnThemePathChanged;
                ThemeWatcher.Deleted += OnThemePathChanged;
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Couldn't add OnThemePathChanged method.\nException: {ex}");
            }
            ThemeWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Updates the file watcher paths.
        /// </summary>
        public void SetWatcherPaths()
        {
            DisablePrefabWatcher();
            if (RTFile.DirectoryExists(RTFile.CombinePaths(BeatmapsPath, PrefabPath)))
            {
                PrefabWatcher.Path = RTFile.CombinePaths(BeatmapsPath, PrefabPath);
                EnablePrefabWatcher();
            }
            DisableThemeWatcher();
            if (RTFile.DirectoryExists(RTFile.CombinePaths(BeatmapsPath, ThemePath)))
            {
                ThemeWatcher.Path = RTFile.CombinePaths(BeatmapsPath, ThemePath);
                EnableThemeWatcher();
            }
        }

        #region Internal

        void InitFileWatchers()
        {
            try
            {
                RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, EditorPath));
                RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, PrefabPath));
                RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, ThemePath));

                PrefabWatcher = new FileSystemWatcher
                {
                    Path = RTFile.CombinePaths(BeatmapsPath, PrefabPath),
                    Filter = FileFormat.LSP.ToPattern(),
                };
                EnablePrefabWatcher();

                ThemeWatcher = new FileSystemWatcher
                {
                    Path = RTFile.CombinePaths(BeatmapsPath, ThemePath),
                    Filter = FileFormat.LST.ToPattern()
                };
                EnableThemeWatcher();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        void OnPrefabPathChanged(object sender, FileSystemEventArgs e)
        {
            if (canUpdatePrefabs && EditorConfig.Instance.UpdatePrefabListOnFilesChanged.Value)
                CoroutineHelper.StartCoroutineAsync(UpdatePrefabPath());
            canUpdatePrefabs = true;
        }

        void OnThemePathChanged(object sender, FileSystemEventArgs e)
        {
            if (canUpdateThemes && EditorConfig.Instance.UpdateThemeListOnFilesChanged.Value)
                StartCoroutine(UpdateThemePath());
            canUpdateThemes = true;
        }

        IEnumerator UpdateThemePath()
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log($"------- [UPDATING THEME FILEWATCHER] -------");
            CoroutineHelper.StartCoroutine(RTThemeEditor.inst.LoadThemes());
            yield break;
        }

        IEnumerator UpdatePrefabPath()
        {
            yield return Ninja.JumpToUnity;
            CoreHelper.Log($"------- [UPDATING PREFAB FILEWATCHER] -------");
            RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs);
            yield break;
        }

        #endregion

        #endregion

        #endregion

        #region Objects

        /// <summary>
        /// Duplicates objects based on mouse position and current dialog.
        /// </summary>
        /// <param name="regen">If IDs should be regenerated.</param>
        public void Duplicate(bool regen = true) => Copy(false, true, regen);

        /// <summary>
        /// Cuts objects based on mouse position and current dialog.
        /// </summary>
        /// <param name="regen">If IDs should be regenerated.</param>
        public void Cut(bool regen = true) => Copy(true, regen: regen);

        /// <summary>
        /// Copies objects based on mouse position and current dialog.
        /// </summary>
        /// <param name="cut">If the objects should be removed.</param>
        /// <param name="dup">If the objects should be pasted.</param>
        /// <param name="regen">If IDs should be regenerated.</param>
        public void Copy(bool cut = false, bool dup = false, bool regen = true)
        {
            if (EditorTimeline.inst.isOverMainTimeline)
            {
                switch (EditorTimeline.inst.layerType)
                {
                    case EditorTimeline.LayerType.Objects: {
                            var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                            ObjectEditor.inst.CopyObjects();
                            if (!cut)
                                EditorManager.inst.DisplayNotification("Copied Beatmap Object", 1f, EditorManager.NotificationType.Success);
                            else
                            {
                                EditorTimeline.inst.DeleteObjects();
                                EditorManager.inst.DisplayNotification("Cut Beatmap Object", 1f, EditorManager.NotificationType.Success);
                            }

                            if (dup)
                                Paste(offsetTime, dup, regen);

                            break;
                        }
                    case EditorTimeline.LayerType.Events: {
                            if (dup)
                            {
                                EditorManager.inst.DisplayNotification("Can't duplicate Event Keyframe", 1f, EditorManager.NotificationType.Error);
                                break;
                            }

                            EventEditor.inst.CopyAllSelectedEvents();
                            if (!cut)
                                EditorManager.inst.DisplayNotification("Copied Event Keyframe", 1f, EditorManager.NotificationType.Success);
                            else
                            {
                                StartCoroutine(RTEventEditor.inst.DeleteKeyframes());
                                EditorManager.inst.DisplayNotification("Cut Event Keyframe", 1f, EditorManager.NotificationType.Success);
                            }

                            break;
                        }
                }
                return;
            }

            if (!EditorDialog.CurrentDialog)
                return;

            switch (EditorDialog.CurrentDialog.Name)
            {
                case EditorDialog.OBJECT_EDITOR: {
                        if (dup)
                        {
                            EditorManager.inst.DisplayNotification("Can't duplicate Object Keyframe", 1f, EditorManager.NotificationType.Error);
                            break;
                        }

                        ObjEditor.inst.CopyAllSelectedEvents();
                        if (!cut)
                            EditorManager.inst.DisplayNotification("Copied Object Keyframe", 1f, EditorManager.NotificationType.Success);
                        else
                        {
                            StartCoroutine(ObjectEditor.inst.DeleteKeyframes());
                            EditorManager.inst.DisplayNotification("Cut Object Keyframe", 1f, EditorManager.NotificationType.Success);
                        }
                        break;
                    }
                case EditorDialog.BACKGROUND_EDITOR: {
                        RTBackgroundEditor.inst.CopyBackground();
                        if (!cut)
                            EditorManager.inst.DisplayNotification("Copied Background Object", 1f, EditorManager.NotificationType.Success);
                        else
                        {
                            RTBackgroundEditor.inst.DeleteBackground();
                            EditorManager.inst.DisplayNotification("Cut Background Object", 1f, EditorManager.NotificationType.Success);
                        }

                        if (dup)
                            Paste();

                        break;
                    }
                case EditorDialog.CHECKPOINT_EDITOR: {
                        if (!RTCheckpointEditor.inst.CurrentCheckpoint)
                        {
                            EditorManager.inst.DisplayNotification("No Checkpoint.", 1f, EditorManager.NotificationType.Error);
                            break;
                        }

                        RTCheckpointEditor.inst.CopyCheckpoint();
                        if (!cut)
                            EditorManager.inst.DisplayNotification("Copied Checkpoint", 1f, EditorManager.NotificationType.Success);
                        else
                        {
                            RTCheckpointEditor.inst.DeleteCheckpoint(RTCheckpointEditor.inst.CurrentCheckpoint.Index);
                            EditorManager.inst.DisplayNotification("Cut Checkpoint", 1f, EditorManager.NotificationType.Success);
                        }

                        if (dup)
                            Paste();

                        break;
                    }
                case EditorDialog.ACHIEVEMENT_EDITOR_DIALOG: {
                        var currentAchievement = AchievementEditor.inst.CurrentAchievement;
                        if (!currentAchievement)
                            break;

                        if (cut)
                            AchievementEditor.inst.DeleteAchievement();
                        AchievementEditor.inst.CopyAchievement(currentAchievement);
                        if (dup)
                            AchievementEditor.inst.PasteAchievements();
                        break;
                    }
                case EditorDialog.ANIMATION_EDITOR_DIALOG: {
                        if (dup)
                        {
                            EditorManager.inst.DisplayNotification("Can't duplicate Object Keyframe", 1f, EditorManager.NotificationType.Error);
                            break;
                        }

                        if (!AnimationEditor.inst.CurrentAnimation)
                            return;

                        AnimationEditor.inst.Dialog.Timeline.CopyAllSelectedEvents(AnimationEditor.inst.CurrentAnimation);

                        if (!cut)
                            EditorManager.inst.DisplayNotification("Copied Object Keyframe", 1f, EditorManager.NotificationType.Success);
                        else
                        {
                            StartCoroutine(AnimationEditor.inst.Dialog.Timeline.DeleteKeyframes(AnimationEditor.inst.CurrentAnimation));
                            EditorManager.inst.DisplayNotification("Cut Object Keyframe", 1f, EditorManager.NotificationType.Success);
                        }
                        break;
                    }
            }
        }

        public void Paste() => Paste(0f, false);

        public void Paste(float offsetTime) => Paste(offsetTime, false);

        public void Paste(bool regen) => Paste(0f, regen);

        public void Paste(float offsetTime, bool regen) => Paste(offsetTime, false, regen);

        /// <summary>
        /// Pastes copied objects based on mouse position and current dialog.
        /// </summary>
        /// <param name="offsetTime">Time to offset the paste from.</param>
        /// <param name="regen">If IDs should be regenerated.</param>
        public void Paste(float offsetTime, bool dup, bool regen)
        {
            if (EditorTimeline.inst.isOverMainTimeline)
            {
                switch (EditorTimeline.inst.layerType)
                {
                    case EditorTimeline.LayerType.Objects: {
                            ObjectEditor.inst.PasteObject(offsetTime, dup, regen);
                            break;
                        }
                    case EditorTimeline.LayerType.Events: {
                            RTEventEditor.inst.PasteEvents();
                            EditorManager.inst.DisplayNotification($"Pasted Event Keyframe{(RTEventEditor.inst.copiedEventKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
                            break;
                        }
                }
                return;
            }

            if (!EditorDialog.CurrentDialog)
                return;

            switch (EditorDialog.CurrentDialog.Name)
            {
                case EditorDialog.OBJECT_EDITOR: {
                        ObjectEditor.inst.PasteKeyframes();
                        EditorManager.inst.DisplayNotification($"Pasted Object Keyframe{(KeyframeTimeline.copiedObjectKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
                        break;
                    }
                case EditorDialog.BACKGROUND_EDITOR: {
                        RTBackgroundEditor.inst.PasteBackground();
                        EditorManager.inst.DisplayNotification("Pasted Background Object", 1f, EditorManager.NotificationType.Success);
                        break;
                    }
                case EditorDialog.CHECKPOINT_EDITOR: {
                        RTCheckpointEditor.inst.PasteCheckpoint();
                        EditorManager.inst.DisplayNotification("Pasted Checkpoint", 1f, EditorManager.NotificationType.Success);
                        break;
                    }
                case EditorDialog.ACHIEVEMENT_EDITOR_DIALOG: {
                        AchievementEditor.inst.PasteAchievements();
                        break;
                    }
                case EditorDialog.ANIMATION_EDITOR_DIALOG: {
                        if (!AnimationEditor.inst.CurrentAnimation)
                            return;

                        AnimationEditor.inst.Dialog.Timeline.PasteKeyframes(AnimationEditor.inst.CurrentAnimation);
                        EditorManager.inst.DisplayNotification($"Pasted Object Keyframe{(KeyframeTimeline.copiedObjectKeyframes.Count > 1 ? "s" : "")}", 1f, EditorManager.NotificationType.Success);
                        break;
                    }
            }
        }

        /// <summary>
        /// Deletes an object based on mouse position and current dialog.
        /// </summary>
        public void Delete()
        {
            if (EditorTimeline.inst.isOverMainTimeline)
            {
                switch (EditorTimeline.inst.layerType)
                {
                    case EditorTimeline.LayerType.Objects: {
                            // prepare undo / redo
                            List<TimelineObject> list = EditorTimeline.inst.SelectedObjects;
                            Prefab prefab = null;
                            float t = 0f;
                            List<IDPair> children = null;

                            EditorManager.inst.history.Add(new History.Command("Delete Objects",
                                () =>
                                {
                                    EditorDialog.CurrentDialog?.Close();

                                    prefab = new Prefab("deleted objects", 0, 0f,
                                       list.Where(x => x.isBeatmapObject).Select(x => x.GetData<BeatmapObject>()).ToList(),
                                       list.Where(x => x.isPrefabObject).Select(x => x.GetData<PrefabObject>()).ToList(),
                                       null,
                                       list.Where(x => x.isBackgroundObject).Select(x => x.GetData<BackgroundObject>()).ToList());

                                    t = list.Min(x => x.Time);

                                    children = new List<IDPair>();
                                    foreach (var parent in prefab.beatmapObjects)
                                    {
                                        for (int i = 0; i < GameData.Current.beatmapObjects.Count; i++)
                                        {
                                            var beatmapObject = GameData.Current.beatmapObjects[i];
                                            if (beatmapObject.parent == parent.id)
                                                children.Add(new IDPair(parent.id, beatmapObject.id)); // old = parent new = id
                                        }
                                    }

                                    EditorTimeline.inst.DeleteObjects();
                                },
                                () =>
                                {
                                    EditorTimeline.inst.DeselectAllObjects();
                                    new PrefabExpander(prefab).Select().RetainID().Offset(t).Expand();

                                    if (children != null)
                                    {
                                        foreach (var pair in children)
                                        {
                                            if (GameData.Current.beatmapObjects.TryFind(x => x.id == pair.newID, out BeatmapObject beatmapObject))
                                            {
                                                beatmapObject.Parent = pair.oldID;
                                                RTLevel.Current.UpdateObject(beatmapObject, ObjectContext.PARENT_CHAIN);
                                            }
                                        }
                                    }

                                    list = EditorTimeline.inst.SelectedObjects;
                                }), true);
                            break;
                        }
                    case EditorTimeline.LayerType.Events: {
                            var selectedKeyframes = RTEventEditor.inst.SelectedKeyframes;
                            if (selectedKeyframes.IsEmpty() || selectedKeyframes.Has(x => x.Index == 0))
                            {
                                EditorManager.inst.DisplayNotification("Can't delete first Event Keyframe.", 1f, EditorManager.NotificationType.Error);
                                break;
                            }

                            var list = new List<TimelineKeyframe>();
                            foreach (var timelineObject in selectedKeyframes)
                                list.Add(timelineObject);

                            EditorManager.inst.history.Add(new History.Command("Delete Event Keyframes",
                                () =>
                                {
                                    EditorDialog.CurrentDialog?.Close();
                                    CoroutineHelper.StartCoroutine(RTEventEditor.inst.DeleteKeyframes(list));
                                },
                                () =>
                                {
                                    var pasted = RTEventEditor.inst.PasteEvents(list, false);
                                    if (pasted != null)
                                        list = pasted;
                                }), true);
                            break;
                        }
                }

                return;
            }

            if (!EditorDialog.CurrentDialog)
                return;

            switch (EditorDialog.CurrentDialog.Name)
            {
                case EditorDialog.OBJECT_EDITOR: {
                        if (!EditorTimeline.inst.CurrentSelection.isBeatmapObject)
                            break;

                        if (ObjectEditor.inst.Dialog.Timeline.currentKeyframeIndex == 0)
                        {
                            EditorManager.inst.DisplayNotification("Can't delete first keyframe.", 1f, EditorManager.NotificationType.Error);
                            break;
                        }

                        var list = new List<TimelineKeyframe>();
                        foreach (var timelineObject in EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>().TimelineKeyframes.Where(x => x.Selected))
                            list.Add(timelineObject);
                        var beatmapObject = EditorTimeline.inst.CurrentSelection.GetData<BeatmapObject>();

                        EditorManager.inst.history.Add(new History.Command("Delete Keyframes",
                            () =>
                            {
                                CoroutineHelper.StartCoroutine(ObjectEditor.inst.Dialog.Timeline.DeleteKeyframes(beatmapObject));
                            },
                            () =>
                            {
                                var pasted = ObjectEditor.inst.Dialog.Timeline.PasteKeyframes(beatmapObject, list, false);
                                if (pasted != null)
                                    list = pasted;
                            }), true);

                        break;
                    }
                case EditorDialog.BACKGROUND_EDITOR: {
                        var backgroundObject = RTBackgroundEditor.inst.CurrentSelectedBG;
                        if (!backgroundObject)
                        {
                            EditorManager.inst.DisplayNotification("No BG to delete.", 1f, EditorManager.NotificationType.Error);
                            return;
                        }

                        EditorManager.inst.history.Add(new History.Command("Delete Background",
                            () =>
                            {
                                RTBackgroundEditor.inst.DeleteBackground(backgroundObject);
                            },
                            () =>
                            {
                                if (!backgroundObject)
                                    return;

                                GameData.Current.backgroundObjects.Add(backgroundObject);
                                EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(backgroundObject));
                                RTLevel.Current.UpdateBackgroundObject(backgroundObject);
                            }), true);
                        break;
                    }
                case EditorDialog.CHECKPOINT_EDITOR: {
                        if (!RTCheckpointEditor.inst.CurrentCheckpoint)
                        {
                            EditorManager.inst.DisplayNotification("Can't delete first Checkpoint.", 1f, EditorManager.NotificationType.Error);
                            break;
                        }

                        TimelineCheckpoint deletedCheckpoint = null;

                        EditorManager.inst.history.Add(new History.Command("Delete Checkpoint",
                            () =>
                            {
                                deletedCheckpoint = RTCheckpointEditor.inst.CurrentCheckpoint;
                                RTCheckpointEditor.inst.DeleteCheckpoint(deletedCheckpoint.Index);
                                EditorManager.inst.DisplayNotification("Deleted Checkpoint.", 1f, EditorManager.NotificationType.Success);
                            },
                            () =>
                            {
                                if (!deletedCheckpoint)
                                    return;

                                var checkpoint = deletedCheckpoint.Checkpoint?.Copy();
                                if (!checkpoint)
                                    return;

                                GameData.Current.data.checkpoints.Add(checkpoint);
                                RTCheckpointEditor.inst.UpdateCheckpointTimeline();
                            }), true);
                        break;
                    }
                case EditorDialog.ANIMATION_EDITOR_DIALOG: {
                        var animation = AnimationEditor.inst.CurrentAnimation;
                        if (!animation)
                            break;

                        if (AnimationEditor.inst.Dialog.Timeline.currentKeyframeIndex == 0)
                        {
                            EditorManager.inst.DisplayNotification("Can't delete first keyframe.", 1f, EditorManager.NotificationType.Error);
                            break;
                        }

                        var list = new List<TimelineKeyframe>();
                        foreach (var timelineObject in animation.TimelineKeyframes.Where(x => x.Selected))
                            list.Add(timelineObject);

                        EditorManager.inst.history.Add(new History.Command("Delete Keyframes",
                            () =>
                            {
                                CoroutineHelper.StartCoroutine(AnimationEditor.inst.Dialog.Timeline.DeleteKeyframes(animation));
                            },
                            () =>
                            {
                                var pasted = AnimationEditor.inst.Dialog.Timeline.PasteKeyframes(animation, list, false);
                                if (pasted != null)
                                    list = pasted;
                            }), true);
                        break;
                    }
            }
        }

        #endregion

        #region Asset Packs

        public void LoadEditorAssetPack(bool update = true)
        {
            LoadLevelPanelUI(update);
            LoadInternalPrefabPanelUI(update);
            LoadExternalPrefabPanelUI(update);
            LoadEditorLayers(update);
        }

        public void LoadLevelPanelUI(bool update = true)
        {
            if (!AssetPack.TryReadFromFile("editor/ui/elements/level_panel.json", out string levelPanelFile))
                return;

            var jn = JSON.Parse(levelPanelFile);
            LevelPanel.labelRect = RectValues.TryParse(jn["label"]["rect"], RectValues.FullAnchored.AnchoredPosition(32f, 0f).SizeDelta(-12f, -8f));
            LevelPanel.labelFormat = jn["label"]["format"] != null ? jn["label"]["format"] : "/{0} : {1} by {2}";
            LevelPanel.labelAlignment = jn["label"]["alignment"] != null ? (TextAnchor)jn["label"]["alignment"].AsInt : TextAnchor.MiddleLeft;
            LevelPanel.labelHorizontalWrap = (HorizontalWrapMode)jn["label"]["horizontal_wrap"].AsInt;
            LevelPanel.labelVerticalWrap = (VerticalWrapMode)jn["label"]["vertical_wrap"].AsInt;
            LevelPanel.labelFontSize = jn["label"]["font_size"] != null ? jn["label"]["font_size"].AsInt : 20;
            LevelPanel.labelFolderNameMax = jn["label"]["folder_name_max"].AsInt;
            LevelPanel.labelSongTitleMax = jn["label"]["song_title_max"].AsInt;
            LevelPanel.labelArtistNameMax = jn["label"]["artist_name_max"].AsInt;
            LevelPanel.labelCreatorNameMax = jn["label"]["creator_name_max"].AsInt;
            LevelPanel.labelDescriptionMax = jn["label"]["description_max"].AsInt;
            LevelPanel.labelDateMax = jn["label"]["date_max"].AsInt;

            LevelPanel.iconRect = RectValues.TryParse(jn["icon"]["rect"], RectValues.Default.AnchoredPosition(-276f, 0f).SizeDelta(26f, 26f));
            LevelPanel.deleteRect = RectValues.TryParse(jn["delete"]["rect"], new RectValues(Vector2.zero, Vector2.one, new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(32f, 0f)));

            if (update)
                EditorLevelManager.inst.LoadLevels();
        }

        public void LoadInternalPrefabPanelUI(bool update = true)
        {
            if (!AssetPack.TryReadFromFile("editor/ui/elements/internal_prefab_panel.json", out string internalPrefabPanelFile))
                return;

            var jn = JSON.Parse(internalPrefabPanelFile);
            PrefabPanel.internalTypeBaseRect = RectValues.TryParse(jn["type"]["base"]["rect"], new RectValues(new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(32f, 32f)));
            PrefabPanel.internalTypeShadeRect = RectValues.TryParse(jn["type"]["shade"]["rect"], RectValues.FullAnchored.SizeDelta(-4f, -4f));
            PrefabPanel.internalTypeIconRect = RectValues.TryParse(jn["type"]["icon"]["rect"], RectValues.FullAnchored);

            PrefabPanel.internalNameLabelRect = RectValues.TryParse(jn["name_label"]["rect"], new RectValues(new Vector2(163.4375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(246.875f, 32f)));
            PrefabPanel.internalNameLabelAlignment = jn["name_label"]["alignment"] != null ? (TextAnchor)jn["name_label"]["alignment"].AsInt : TextAnchor.MiddleLeft;
            PrefabPanel.internalNameLabelHorizontalWrap = jn["name_label"]["horizontal_wrap"] != null ? (HorizontalWrapMode)jn["name_label"]["horizontal_wrap"].AsInt : HorizontalWrapMode.Overflow;
            PrefabPanel.internalNameLabelVerticalWrap = jn["name_label"]["vertical_wrap"] != null ? (VerticalWrapMode)jn["name_label"]["vertical_wrap"].AsInt : VerticalWrapMode.Overflow;
            PrefabPanel.internalNameLabelFontSize = jn["name_label"]["font_size"] != null ? jn["name_label"]["font_size"].AsInt : 20;

            PrefabPanel.internalTypeLabelRect = RectValues.TryParse(jn["type_label"]["rect"], new RectValues(new Vector2(368.9375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(148.125f, 32f)));
            PrefabPanel.internalTypeLabelAlignment = jn["type_label"]["alignment"] != null ? (TextAnchor)jn["type_label"]["alignment"].AsInt : TextAnchor.MiddleRight;
            PrefabPanel.internalTypeLabelHorizontalWrap = jn["type_label"]["horizontal_wrap"] != null ? (HorizontalWrapMode)jn["type_label"]["horizontal_wrap"].AsInt : HorizontalWrapMode.Overflow;
            PrefabPanel.internalTypeLabelVerticalWrap = jn["type_label"]["vertical_wrap"] != null ? (VerticalWrapMode)jn["type_label"]["vertical_wrap"].AsInt : VerticalWrapMode.Overflow;
            PrefabPanel.internalTypeLabelFontSize = jn["type_label"]["font_size"] != null ? jn["type_label"]["font_size"].AsInt : 20;

            PrefabPanel.internalDeleteRect = RectValues.TryParse(jn["delete"]["rect"], new RectValues(new Vector2(467f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(32f, 32f)));

            if (update)
                CoroutineHelper.StartCoroutine(RTPrefabEditor.inst.RefreshInternalPrefabs());
        }

        public void LoadExternalPrefabPanelUI(bool update = true)
        {
            if (!AssetPack.TryReadFromFile("editor/ui/elements/external_prefab_panel.json", out string externalPrefabPanelFile))
                return;

            var jn = JSON.Parse(externalPrefabPanelFile);
            PrefabPanel.externalTypeBaseRect = RectValues.TryParse(jn["type"]["base"]["rect"], new RectValues(new Vector2(0f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(32f, 32f)));
            PrefabPanel.externalTypeShadeRect = RectValues.TryParse(jn["type"]["shade"]["rect"], RectValues.FullAnchored.SizeDelta(-4f, -4f));
            PrefabPanel.externalTypeIconRect = RectValues.TryParse(jn["type"]["icon"]["rect"], RectValues.FullAnchored);

            PrefabPanel.externalNameLabelRect = RectValues.TryParse(jn["name_label"]["rect"], new RectValues(new Vector2(163.4375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(246.875f, 32f)));
            PrefabPanel.externalNameLabelAlignment = jn["name_label"]["alignment"] != null ? (TextAnchor)jn["name_label"]["alignment"].AsInt : TextAnchor.MiddleLeft;
            PrefabPanel.externalNameLabelHorizontalWrap = jn["name_label"]["horizontal_wrap"] != null ? (HorizontalWrapMode)jn["name_label"]["horizontal_wrap"].AsInt : HorizontalWrapMode.Overflow;
            PrefabPanel.externalNameLabelVerticalWrap = jn["name_label"]["vertical_wrap"] != null ? (VerticalWrapMode)jn["name_label"]["vertical_wrap"].AsInt : VerticalWrapMode.Overflow;
            PrefabPanel.externalNameLabelFontSize = jn["name_label"]["font_size"] != null ? jn["name_label"]["font_size"].AsInt : 20;

            PrefabPanel.externalTypeLabelRect = RectValues.TryParse(jn["type_label"]["rect"], new RectValues(new Vector2(368.9375f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(148.125f, 32f)));
            PrefabPanel.externalTypeLabelAlignment = jn["type_label"]["alignment"] != null ? (TextAnchor)jn["type_label"]["alignment"].AsInt : TextAnchor.MiddleRight;
            PrefabPanel.externalTypeLabelHorizontalWrap = jn["type_label"]["horizontal_wrap"] != null ? (HorizontalWrapMode)jn["type_label"]["horizontal_wrap"].AsInt : HorizontalWrapMode.Overflow;
            PrefabPanel.externalTypeLabelVerticalWrap = jn["type_label"]["vertical_wrap"] != null ? (VerticalWrapMode)jn["type_label"]["vertical_wrap"].AsInt : VerticalWrapMode.Overflow;
            PrefabPanel.externalTypeLabelFontSize = jn["type_label"]["font_size"] != null ? jn["type_label"]["font_size"].AsInt : 20;

            PrefabPanel.externalDeleteRect = RectValues.TryParse(jn["delete"]["rect"], new RectValues(new Vector2(467f, -16f), new Vector2(0f, 1f), new Vector2(0f, 1f), RectValues.CenterPivot, new Vector2(32f, 32f)));

            if (update)
                RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs);
        }

        public void LoadEditorLayers(bool update = true)
        {
            if (!AssetPack.TryReadFromFile("editor/data/editor_layers.json", out string editorLayersFile))
                return;

            var jn = JSON.Parse(editorLayersFile);
            EditorLayers.Clear();
            for (int i = 0; i < jn["layers"].Count; i++)
            {
                var jnLayer = jn["layers"][i];
                EditorLayers.Add(new EditorLayer(jnLayer["layer"], Parser.TryParse(jnLayer["theme_group"], ThemeGroup.Null), jnLayer["color"]));
            }

            if (!update)
                return;

            var allEditorLayerToggles = FindObjectsOfType<EditorLayerToggles>();
            for (int i = 0; i < allEditorLayerToggles.Length; i++)
            {
                var editorLayerToggles = allEditorLayerToggles[i];
                if (editorLayerToggles.editorLayerUI != null)
                    SetupEditorLayers(editorLayerToggles.editorLayerUI, editorLayerToggles.size);
            }
        }

        #endregion

        #region Generate UI

        public static GameObject GenerateSpacer(string name, Transform parent, Vector2 size)
        {
            var spacer = Creator.NewUIObject(name, parent);
            spacer.transform.AsRT().sizeDelta = size;

            return spacer;
        }

        public static GameObject GenerateLabels(string name, Transform parent, params Label[] labels) => GenerateLabels(name, parent, -1, labels);

        public static GameObject GenerateLabels(string name, Transform parent, int siblingIndex, params Label[] labels)
        {
            var label = EditorPrefabHolder.Instance.Labels.Duplicate(parent, name, siblingIndex);
            var first = label.transform.GetChild(0);

            for (int i = 0; i < labels.Length; i++)
            {
                var labelSetting = labels[i];
                if (i >= label.transform.childCount)
                    first.gameObject.Duplicate(label.transform, first.name);

                var child = label.transform.GetChild(i);
                var labelText = child.GetComponent<Text>();
                labelSetting.Apply(labelText);

                EditorThemeManager.AddLightText(labelText);
            }

            return label;
        }
        
        public static GameObject GenerateLabels(string name, Transform parent, int siblingIndex, bool applyThemes, params Label[] labels)
        {
            var label = EditorPrefabHolder.Instance.Labels.Duplicate(parent, name, siblingIndex);
            var first = label.transform.GetChild(0);

            for (int i = 0; i < labels.Length; i++)
            {
                var labelSetting = labels[i];
                if (i >= label.transform.childCount)
                    first.gameObject.Duplicate(label.transform, first.name);

                var child = label.transform.GetChild(i);
                var labelText = child.GetComponent<Text>();
                labelSetting.Apply(labelText);

                if (applyThemes)
                    EditorThemeManager.AddLightText(labelText);
            }

            return label;
        }

        /// <summary>
        /// Generates a content popup.
        /// </summary>
        /// <param name="name">Name of the editor popup.</param>
        /// <param name="title">Title to render.</param>
        /// <param name="defaultPosition">Default position to set.</param>
        /// <param name="size">Size of the editor popup.</param>
        /// <param name="refreshSearch">Function to run when the user types in the search field.</param>
        /// <param name="close">Function to run when the user closes the editor popup.</param>
        /// <param name="placeholderText">Search field placeholder text.</param>
        /// <returns>Returns a generated content popup.</returns>
        public ContentPopup GeneratePopup(string name, string title, Vector2? defaultPosition = null, Vector2? size = null, Action<string> refreshSearch = null, Action close = null, string placeholderText = "Search...")
        {
            var editorPopup = new ContentPopup(name, title, defaultPosition, size, refreshSearch, close, placeholderText);
            editorPopup.Init();
            editorPopups.Add(editorPopup);
            return editorPopup;
        }

        public void SetupIndexer(IIndexDialog dialog)
        {
            if (dialog == null || !dialog.Edit)
            {
                CoreHelper.LogError($"Failed to setup indexer.");
                return;
            }

            try
            {
                dialog.JumpToStartButton = dialog.Edit.Find("<<").GetComponent<Button>();
                dialog.JumpToPrevButton = dialog.Edit.Find("<").GetComponent<Button>();

                if (dialog.Edit.TryFind("|/Text", out Transform textTransform))
                    dialog.KeyframeIndexer = textTransform.GetComponent<Text>();
                else if (dialog.Edit.TryFind("|/text", out Transform textLowerTransform))
                    dialog.KeyframeIndexer = textLowerTransform.GetComponent<Text>();

                dialog.JumpToNextButton = dialog.Edit.Find(">").GetComponent<Button>();
                dialog.JumpToLastButton = dialog.Edit.Find(">>").GetComponent<Button>();

                if (dialog.Edit.TryFind("copy", out Transform copyTransform))
                    dialog.CopyButton = copyTransform.GetComponent<FunctionButtonStorage>();
                if (dialog.Edit.TryFind("paste", out Transform pasteTransform))
                    dialog.PasteButton = pasteTransform.GetComponent<FunctionButtonStorage>();
                dialog.DeleteButton = dialog.Edit.Find("del").gameObject.AddComponent<DeleteButtonStorage>();
                dialog.DeleteButton.Assign(dialog.DeleteButton.gameObject);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Failed to set edit: {ex}");
            }
        }

        public void SetupEditorLayers(IEditorLayerUI editorLayerUI, float size = 30.5f)
        {
            var editorLayerToggles = editorLayerUI.EditorLayerTogglesParent.gameObject.GetOrAddComponent<EditorLayerToggles>();
            editorLayerToggles.editorLayerUI = editorLayerUI;
            editorLayerToggles.size = size;

            CoreHelper.DestroyChildren(editorLayerUI.EditorLayerTogglesParent);
            editorLayerUI.EditorLayerToggles = new Toggle[EditorLayers.Count];
            for (int i = 0; i < EditorLayers.Count; i++)
            {
                var editorLayer = EditorLayers[i];
                var gameObject = EditorPrefabHolder.Instance.EditorLayerToggle.Duplicate(editorLayerUI.EditorLayerTogglesParent, (i + 1).ToString());
                gameObject.GetOrAddComponent<LayoutElement>().minWidth = size;
                var toggle = gameObject.GetComponent<Toggle>();
                editorLayerUI.EditorLayerToggles[i] = toggle;
                if (!string.IsNullOrEmpty(editorLayer.color))
                    toggle.image.color = RTColors.HexToColor(editorLayer.color);
                else
                    EditorThemeManager.AddGraphic(toggle.image, editorLayer.themeGroup);
                EditorThemeManager.AddGraphic(toggle.graphic, ThemeGroup.Timeline_Bar);

                var label = toggle.transform.Find("Background/Text").GetComponent<Text>();
                label.text = (i + 1).ToString();
                toggle.gameObject.AddComponent<ContrastColors>().Init(label, toggle.image);
            }
        }

        /// <summary>
        /// Sets up an easing dropdown.
        /// </summary>
        /// <param name="dropdown">Dropdown to setup.</param>
        public void SetupEaseDropdown(Dropdown dropdown)
        {
            //dropdown.options.Clear();
            //var easingNames = Enum.GetNames(typeof(Easing));
            //for (int i = 0; i < easingNames.Length; i++)
            //{
            //    var easingName = easingNames[i];
            //    var icon = SpriteHelper.LoadSprite(AssetPack.TryGetFile("core/sprites/ease/" + easingName + FileFormat.PNG.Dot(), out string iconFilePath) ? iconFilePath : AssetPack.GetFile("core/sprites/ease/Instant.png"));

            //    dropdown.options.Add(icon ? new Dropdown.OptionData(easingName, icon) : new Dropdown.OptionData(easingName));
            //}
            dropdown.options = GetEaseOptions();
            if (EasingDropdownLayoutGroupValues is GridLayoutValues gridLayoutValues)
                gridLayoutValues.AssignToLayout(dropdown.template.Find("Viewport/Content").gameObject.GetOrAddComponent<GridLayoutGroup>());
            else if (EasingDropdownLayoutGroupValues is HorizontalOrVerticalLayoutValues horizontalOrVerticalLayoutValues)
                horizontalOrVerticalLayoutValues.AssignToLayout(dropdown.template.Find("Viewport/Content").gameObject.GetOrAddComponent<HorizontalOrVerticalLayoutGroup>());
            EasingDropdownTemplateRectValues.AssignToRectTransform(dropdown.template);
            var contentSizeFitter = dropdown.template.Find("Viewport/Content").gameObject.GetOrAddComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.MinSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.MinSize;
        }

        public List<Dropdown.OptionData> GetEaseOptions()
        {
            //var options = EasingOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();
            //var easingNames = Enum.GetNames(typeof(Easing));
            //for (int i = 0; i < easingNames.Length; i++)
            //{
            //    var easingName = easingNames[i];
            //    var icon = SpriteHelper.LoadSprite(AssetPack.TryGetFile("core/sprites/ease/" + easingName + FileFormat.PNG.Dot(), out string iconFilePath) ? iconFilePath : AssetPack.GetFile("core/sprites/ease/Instant.png"));

            //    options.Add(icon ? new Dropdown.OptionData(easingName, icon) : new Dropdown.OptionData(easingName));
            //}
            return EasingOptions.Select(x => new Dropdown.OptionData(x.name, x.icon)).ToList();
        }

        #region Internal

        void SetupTimelineBar()
        {
            if (EditorManager.inst.markerTimeline)
                EditorManager.inst.markerTimeline.SetActive(EditorConfig.Instance.ShowMarkers.Value);

            var layers = Creator.NewUIObject("layer toggles", timelineBar.transform, 7);
            EditorTimeline.inst.EditorLayerTogglesParent = layers.transform.AsRT();
            var layersLayout = layers.AddComponent<HorizontalLayoutGroup>();
            layersLayout.childControlWidth = true;
            layersLayout.spacing = 8f;

            for (int i = 1; i <= 5; i++)
                timelineBar.transform.Find(i.ToString()).SetParent(null);
            SetupEditorLayers(EditorTimeline.inst, 34f);
            EditorHelper.SetComplexity(layers, "editor_layer_toggles", Complexity.Simple);

            eventLayerToggle = timelineBar.transform.Find("6").GetComponent<Toggle>();
            eventLayerToggle.group = null;
            Destroy(eventLayerToggle.GetComponent<EventTrigger>());

            var timeObj = EditorPrefabHolder.Instance.DefaultInputField.Duplicate(timelineBar.transform, "Time Input", 0);
            timeObj.transform.localScale = Vector3.one;

            timeField = timeObj.GetComponent<InputField>();

            TooltipHelper.AssignTooltip(timeObj, timeObj.name, 3f);

            timeObj.SetActive(true);
            timeField.textComponent.alignment = TextAnchor.MiddleLeft;
            timeField.textComponent.fontSize = 16;
            timeField.GetPlaceholderText().text = "Set time...";
            timeField.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
            timeField.GetPlaceholderText().fontSize = 16;
            timeField.GetPlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
            timeField.characterValidation = InputField.CharacterValidation.Decimal;

            timeField.onValueChanged.AddListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                    AudioManager.inst.SetMusicTime(num);
            });

            TriggerHelper.AddEventTriggers(timeObj, TriggerHelper.ScrollDelta(timeField));

            EditorHelper.SetComplexity(timeField.gameObject, "time_field", Complexity.Normal);

            var layersObj = timeObj.Duplicate(timelineBar.transform, "layers", 7);
            EditorHelper.SetComplexity(layersObj, "editor_layer_field", Complexity.Normal);
            layersObj.transform.localScale = Vector3.one;

            TooltipHelper.AssignTooltip(layersObj, "Editor Layer", 3f);

            EditorTimeline.inst.EditorLayerField = layersObj.GetComponent<InputField>();
            EditorTimeline.inst.EditorLayerField.textComponent.alignment = TextAnchor.MiddleCenter;

            EditorTimeline.inst.EditorLayerField.text = EditorTimeline.GetLayerString(EditorManager.inst.layer);

            layersObj.AddComponent<ContrastColors>().Init(EditorTimeline.inst.EditorLayerField.textComponent, EditorTimeline.inst.EditorLayerField.image);

            EditorTimeline.inst.EditorLayerField.textComponent.alignment = TextAnchor.MiddleCenter;
            EditorTimeline.inst.EditorLayerField.textComponent.fontSize = 16;
            EditorTimeline.inst.EditorLayerField.characterValidation = InputField.CharacterValidation.None;
            EditorTimeline.inst.EditorLayerField.contentType = InputField.ContentType.Standard;
            EditorTimeline.inst.EditorLayerField.GetPlaceholderText().text = "Set layer...";
            EditorTimeline.inst.EditorLayerField.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
            EditorTimeline.inst.EditorLayerField.GetPlaceholderText().fontSize = 16;
            EditorTimeline.inst.EditorLayerField.GetPlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
            EditorTimeline.inst.EditorLayerField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                    EditorTimeline.inst.SetLayer(Mathf.Clamp(num - 1, 0, int.MaxValue));
            });

            EditorTimeline.inst.EditorLayerField.image.color = EditorTimeline.GetLayerColor(EditorManager.inst.layer);

            TriggerHelper.AddEventTriggers(layersObj,
                TriggerHelper.ScrollDeltaInt(EditorTimeline.inst.EditorLayerField, 1, 1, int.MaxValue), TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
                {
                    if (((PointerEventData)eventData).button == PointerEventData.InputButton.Middle)
                        CoreHelper.ListObjectLayers();
                }));

            var pitchObj = timeObj.Duplicate(timelineBar.transform, "pitch", 5);
            pitchObj.SetActive(true);
            pitchObj.transform.localScale = Vector3.one;
            TooltipHelper.AssignTooltip(pitchObj, "Pitch", 3f);

            pitchField = pitchObj.GetComponent<InputField>();
            pitchField.textComponent.alignment = TextAnchor.MiddleCenter;
            pitchField.textComponent.fontSize = 16;
            pitchField.GetPlaceholderText().text = "Pitch";
            pitchField.GetPlaceholderText().alignment = TextAnchor.MiddleLeft;
            pitchField.GetPlaceholderText().fontSize = 16;
            pitchField.GetPlaceholderText().horizontalOverflow = HorizontalWrapMode.Overflow;
            pitchField.onValueChanged.NewListener(_val =>
            {
                if (float.TryParse(_val, out float num))
                {
                    AudioManager.inst.SetPitch(num);

                    if (num < 0f)
                        AchievementManager.inst.UnlockAchievement("editor_reverse_speed");
                }
            });

            TriggerHelper.AddEventTriggers(pitchObj, TriggerHelper.ScrollDelta(pitchField, 0.1f, 10f));

            pitchObj.GetComponent<LayoutElement>().minWidth = 64f;
            pitchField.textComponent.alignment = TextAnchor.MiddleCenter;

            pitchObj.AddComponent<InputFieldSwapper>();

            var timelineBarBase = timelineBar.transform.parent.gameObject;
            EditorThemeManager.AddGraphic(timelineBarBase.GetComponent<Image>(), ThemeGroup.Timeline_Bar);
            EditorThemeManager.AddSelectable(timeDefault.AddComponent<Button>(), ThemeGroup.List_Button_1);
            EditorThemeManager.AddLightText(timeDefault.transform.GetChild(0).GetComponent<Text>());

            EditorThemeManager.AddInputField(timeField);

            var play = timelineBar.transform.Find("play").gameObject;
            Destroy(play.GetComponent<Animator>());
            var playButton = play.GetComponent<Button>();
            playButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(playButton, ThemeGroup.Function_2, false);

            var leftPitch = timelineBar.transform.Find("<").gameObject;
            Destroy(leftPitch.GetComponent<Animator>());
            var leftPitchButton = leftPitch.GetComponent<Button>();
            leftPitchButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(leftPitchButton, ThemeGroup.Function_2, false);

            EditorThemeManager.AddInputField(pitchField);

            var rightPitch = timelineBar.transform.Find(">").gameObject;
            Destroy(rightPitch.GetComponent<Animator>());
            var rightPitchButton = rightPitch.GetComponent<Button>();
            rightPitchButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(rightPitchButton, ThemeGroup.Function_2, false);

            // Leave this group empty since the color is already handled via the custom layer colors. This is only here for the rounded corners.
            EditorThemeManager.AddGraphic(EditorTimeline.inst.EditorLayerField.image, ThemeGroup.Null, true);
            EditorThemeManager.AddGraphic(eventLayerToggle.image, ThemeGroup.Event_Check, true);
            EditorThemeManager.AddGraphic(eventLayerToggle.transform.Find("Background/Text").GetComponent<Text>(), ThemeGroup.Event_Check_Text);

            EditorThemeManager.AddGraphic(eventLayerToggle.graphic, ThemeGroup.Timeline_Bar);

            var prefabButton = timelineBar.transform.Find("prefab").gameObject;
            EditorThemeManager.AddGraphic(prefabButton.GetComponent<Image>(), ThemeGroup.Prefab, true);
            EditorThemeManager.AddGraphic(prefabButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Prefab_Text);

            var objectButton = timelineBar.transform.Find("object").gameObject;
            objectButton.GetComponent<Button>().onClick.ClearAll();
            EditorThemeManager.AddGraphic(objectButton.GetComponent<Image>(), ThemeGroup.Object, true);
            EditorThemeManager.AddGraphic(objectButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Object_Text);

            var markerButton = timelineBar.transform.Find("event").gameObject;
            EditorThemeManager.AddGraphic(markerButton.GetComponent<Image>(), ThemeGroup.Marker, true);
            EditorThemeManager.AddGraphic(markerButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Marker_Text);

            var checkpointButton = timelineBar.transform.Find("checkpoint").gameObject;
            EditorThemeManager.AddGraphic(checkpointButton.GetComponent<Image>(), ThemeGroup.Checkpoint, true);
            EditorThemeManager.AddGraphic(checkpointButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Checkpoint_Text);

            var backgroundButton = timelineBar.transform.Find("background").gameObject;
            EditorThemeManager.AddGraphic(backgroundButton.GetComponent<Image>(), ThemeGroup.Background_Object, true);
            EditorThemeManager.AddGraphic(backgroundButton.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Background_Object_Text);

            var playTest = timelineBar.transform.Find("playtest").gameObject;
            Destroy(playTest.GetComponent<Animator>());
            var playTestButton = playTest.GetComponent<Button>();
            playTestButton.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddSelectable(playTestButton, ThemeGroup.Function_2, false);

            var openBG = backgroundButton.transform.Find("BG Options Popup/open").GetComponent<Button>();
            openBG.onClick.NewListener(() => RTBackgroundEditor.inst.OpenDialog());
            var createBG = backgroundButton.transform.Find("BG Options Popup/create").GetComponent<Button>();
            createBG.onClick.NewListener(() => RTBackgroundEditor.inst.CreateNewBackground());

            var objectContextMenu = objectButton.AddComponent<ContextClickable>();
            objectContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                {
                    switch (EditorConfig.Instance.ObjectCreationClickBehavior.Value)
                    {
                        case ObjectCreationClickBehaviorType.OpenOptionsPopup: {
                                ObjectOptionsPopup.Open();
                                break;
                            }
                        case ObjectCreationClickBehaviorType.OpenMoreOptionsPopup: {
                                ObjectEditor.inst.ShowObjectTemplates();
                                break;
                            }
                        case ObjectCreationClickBehaviorType.CreateObject: {
                                ObjectEditor.inst.CreateNewNormalObject();
                                break;
                            }
                    }
                    return;
                }

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Create Object", () => ObjectEditor.inst.CreateNewNormalObject()),
                    new ButtonFunction("Options", ObjectOptionsPopup.Open),
                    new ButtonFunction("More Options", ObjectEditor.inst.ShowObjectTemplates),
                    new ButtonFunction(true),
                    new ButtonFunction($"{(EditorConfig.Instance.ObjectCreationClickBehavior.Value == ObjectCreationClickBehaviorType.OpenOptionsPopup ? "> " : string.Empty)}Behavior: Open Options Popup", () =>
                    {
                        EditorConfig.Instance.ObjectCreationClickBehavior.Value = ObjectCreationClickBehaviorType.OpenOptionsPopup;
                        EditorManager.inst.DisplayNotification($"Set the Object Creation button's click behavior to {EditorConfig.Instance.ObjectCreationClickBehavior.Value}!", 3f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction($"{(EditorConfig.Instance.ObjectCreationClickBehavior.Value == ObjectCreationClickBehaviorType.OpenMoreOptionsPopup ? "> " : string.Empty)}Behavior: Open More Options Popup", () =>
                    {
                        EditorConfig.Instance.ObjectCreationClickBehavior.Value = ObjectCreationClickBehaviorType.OpenMoreOptionsPopup;
                        EditorManager.inst.DisplayNotification($"Set the Object Creation button's click behavior to {EditorConfig.Instance.ObjectCreationClickBehavior.Value}!", 3f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction($"{(EditorConfig.Instance.ObjectCreationClickBehavior.Value == ObjectCreationClickBehaviorType.CreateObject ? "> " : string.Empty)}Behavior: Create Object", () =>
                    {
                        EditorConfig.Instance.ObjectCreationClickBehavior.Value = ObjectCreationClickBehaviorType.CreateObject;
                        EditorManager.inst.DisplayNotification($"Set the Object Creation button's click behavior to {EditorConfig.Instance.ObjectCreationClickBehavior.Value}!", 3f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Search Objects", ObjectEditor.inst.ShowObjectSearch),
                    new ButtonFunction(true),
                    new ButtonFunction("Reload Templates", ObjectEditor.inst.LoadObjectTemplates)
                    );
            };

            var prefabContextMenu = prefabButton.AddComponent<ContextClickable>();
            prefabContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Show Prefabs", PrefabEditor.inst.OpenPopup),
                    new ButtonFunction(true),
                    new ButtonFunction("Create Internal Prefab", () =>
                    {
                        PrefabEditor.inst.OpenDialog();
                        RTPrefabEditor.inst.createInternal = true;
                    }),
                    new ButtonFunction("Create External Prefab", () =>
                    {
                        PrefabEditor.inst.OpenDialog();
                        RTPrefabEditor.inst.createInternal = false;
                    })
                    );
            };

            var markerContextMenu = markerButton.AddComponent<ContextClickable>();
            markerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Open Marker Editor", () =>
                    {
                        if (!RTMarkerEditor.inst.CurrentMarker)
                        {
                            EditorManager.inst.DisplayNotification("Select / create a Marker first!", 1.5f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        RTMarkerEditor.inst.OpenDialog(RTMarkerEditor.inst.CurrentMarker);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Create Marker", MarkerEditor.inst.CreateNewMarker),
                    new ButtonFunction("Create Marker at Object", () =>
                    {
                        if (EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.TimelineReference != TimelineObject.TimelineReferenceType.Null)
                            RTMarkerEditor.inst.CreateNewMarker(EditorTimeline.inst.CurrentSelection.Time);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Show Markers", () =>
                    {
                        EditorConfig.Instance.ShowMarkers.Value = true;
                        EditorManager.inst.DisplayNotification("Markers will now display.", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction("Hide Markers", () =>
                    {
                        EditorConfig.Instance.ShowMarkers.Value = false;
                        EditorManager.inst.DisplayNotification("Markers will now be hidden.", 1.5f, EditorManager.NotificationType.Success);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Clear Markers", RTMarkerEditor.inst.ClearMarkers),
                    new ButtonFunction("Copy All Markers", RTMarkerEditor.inst.CopyAllMarkers),
                    new ButtonFunction("Paste Markers", RTMarkerEditor.inst.PasteMarkers)
                    );
            };

            var checkpointContextMenu = checkpointButton.AddComponent<ContextClickable>();
            checkpointContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Open Checkpoint Editor", () =>
                    {
                        if (!RTCheckpointEditor.inst.CurrentCheckpoint)
                        {
                            EditorManager.inst.DisplayNotification("Select / create a Checkpoint first!", 1.5f, EditorManager.NotificationType.Warning);
                            return;
                        }

                        RTCheckpointEditor.inst.OpenDialog(RTCheckpointEditor.inst.CurrentCheckpoint.Checkpoint);
                    }),
                    new ButtonFunction(true),
                    new ButtonFunction("Create Checkpoint", RTCheckpointEditor.inst.CreateNewCheckpoint),
                    new ButtonFunction("Create Checkpoint at Object", () =>
                    {
                        if (EditorTimeline.inst.CurrentSelection && EditorTimeline.inst.CurrentSelection.TimelineReference != TimelineObject.TimelineReferenceType.Null)
                            RTCheckpointEditor.inst.CreateNewCheckpoint(EditorTimeline.inst.CurrentSelection.Time, Vector2.zero);
                    })
                    );
            };

            var playTestContextMenu = playTest.AddComponent<ContextClickable>();
            playTestContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                var buttonFunctions = new List<ButtonFunction>() { new ButtonFunction("Playtest", TogglePreview), };

                var values = ChallengeMode.Zen.GetValues();
                for (int i = 0; i < values.Length; i++)
                {
                    var value = values[i];
                    buttonFunctions.Add(new ButtonFunction($"Playtest {value.DisplayName}", () =>
                    {
                        CoreConfig.Instance.ChallengeModeSetting.Value = value;
                        TogglePreview();
                    }));
                }

                EditorContextMenu.inst.ShowContextMenu(buttonFunctions);
            };

            var eventLayerContextMenu = eventLayerToggle.gameObject.AddComponent<ContextClickable>();
            eventLayerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Toggle Layer Type", () => EditorTimeline.inst.SetLayer(EditorTimeline.inst.Layer, EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events ? EditorTimeline.LayerType.Objects : EditorTimeline.LayerType.Events)),
                    new ButtonFunction("View Objects", () => EditorTimeline.inst.SetLayer(EditorTimeline.inst.Layer, EditorTimeline.LayerType.Objects)),
                    new ButtonFunction("View Events", () => EditorTimeline.inst.SetLayer(EditorTimeline.inst.Layer, EditorTimeline.LayerType.Events))
                    );
            };

            var layerContextMenu = layersObj.AddComponent<ContextClickable>();
            layerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("List Layers with Objects", CoreHelper.ListObjectLayers),
                    new ButtonFunction("Next Free Layer", () =>
                    {
                        var layer = 0;
                        while (GameData.Current.beatmapObjects.Has(x => x.editorData && x.editorData.Layer == layer))
                            layer++;
                        EditorTimeline.inst.SetLayer(layer, EditorTimeline.LayerType.Objects);
                    }),
                    new ButtonFunction("Toggle Object Preview Visibility", () =>
                    {
                        EditorConfig.Instance.OnlyObjectsOnCurrentLayerVisible.Value = !EditorConfig.Instance.OnlyObjectsOnCurrentLayerVisible.Value;
                    }),
                    new ButtonFunction("Pin Editor Layer", () =>
                    {
                        PinnedLayerEditor.inst.PinCurrentEditorLayer();
                        PinnedLayerEditor.inst.Popup.Open();
                        PinnedLayerEditor.inst.RenderPopup();
                    }),
                    new ButtonFunction("View Pinned Editor Layers", () =>
                    {
                        PinnedLayerEditor.inst.Popup.Open();
                        PinnedLayerEditor.inst.RenderPopup();
                    })
                    );
            };
        }

        void SetupTimelineTriggers()
        {
            #region Bin Controls

            var binScroll = EditorPrefabHolder.Instance.Slider.Duplicate(EditorTimeline.inst.wholeTimeline, "Bin Scrollbar");
            var binScrollImage = binScroll.transform.Find("Image").GetComponent<Image>();
            EditorTimeline.inst.binSlider = binScroll.GetComponent<Slider>();
            EditorTimeline.inst.binSlider.onValueChanged.ClearAll();
            EditorTimeline.inst.binSlider.wholeNumbers = false;
            EditorTimeline.inst.binSlider.direction = Slider.Direction.TopToBottom;
            EditorTimeline.inst.binSlider.minValue = 0f;
            EditorTimeline.inst.binSlider.maxValue = 1f;
            EditorTimeline.inst.binSlider.value = 0f;
            EditorTimeline.inst.binSlider.onValueChanged.AddListener(_val =>
            {
                EditorTimeline.inst.BinScroll = EditorTimeline.inst.layerType == EditorTimeline.LayerType.Events ? 0f : _val;
                EditorTimeline.inst.RenderBinPosition();
            });
            RectValues.Default.AnchoredPosition(960f, 134f).Pivot(1f, 1f).SizeDelta(32f, 268f).AssignToRectTransform(binScroll.transform.AsRT());
            RectValues.Default.AnchoredPosition(24f, 0f).AnchorMax(1f, 1f).AnchorMin(0f, 1f).Pivot(1f, 0.5f).SizeDelta(48f, 32f).AssignToRectTransform(EditorTimeline.inst.binSlider.handleRect);
            RectValues.FullAnchored.SizeDelta(0f, 32f).AssignToRectTransform(binScrollImage.rectTransform);

            EditorTimeline.inst.binSlider.colors = UIManager.SetColorBlock(EditorTimeline.inst.binSlider.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), Color.white, Color.white, Color.white);

            TriggerHelper.AddEventTriggers(binScroll, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData => EditorTimeline.inst.binSlider.value += ((PointerEventData)eventData).scrollDelta.y * -EditorConfig.Instance.BinControlScrollAmount.Value * 0.5f));

            #endregion

            TriggerHelper.AddEventTriggers(EditorManager.inst.timeline,
                TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData =>
                {
                    EditorTimeline.inst.isOverMainTimeline = true;
                    SetDialogStatus("Timeline", true);
                }),
                TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => EditorTimeline.inst.isOverMainTimeline = false),
                TriggerHelper.StartDragTrigger(), TriggerHelper.DragTrigger(), TriggerHelper.EndDragTrigger(),
                TriggerHelper.CreateEntry(EventTriggerType.PointerClick, eventData =>
                {
                    if (((PointerEventData)eventData).button != PointerEventData.InputButton.Right)
                        return;

                    EditorContextMenu.inst.ShowContextMenu(
                        new ButtonFunction("Create New", () => ObjectEditor.inst.CreateNewNormalObject()),
                        new ButtonFunction("Update Everything", () => RTLevel.Reinit()),
                        new ButtonFunction(true),
                        new ButtonFunction("Cut", () =>
                        {
                            ObjectEditor.inst.CopyObjects();
                            EditorTimeline.inst.DeleteObjects();
                        }),
                        new ButtonFunction("Copy", ObjectEditor.inst.CopyObjects),
                        new ButtonFunction("Paste", () => ObjectEditor.inst.PasteObject()),
                        new ButtonFunction("Duplicate", () =>
                        {
                            var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                            ObjectEditor.inst.CopyObjects();
                            ObjectEditor.inst.PasteObject(offsetTime);
                        }),
                        new ButtonFunction("Paste (Keep Prefab)", () => ObjectEditor.inst.PasteObject(0f, false)),
                        new ButtonFunction("Duplicate (Keep Prefab)", () =>
                        {
                            var offsetTime = EditorTimeline.inst.SelectedObjects.Min(x => x.Time);

                            ObjectEditor.inst.CopyObjects();
                            ObjectEditor.inst.PasteObject(offsetTime, false);
                        }),
                        new ButtonFunction("Delete", EditorTimeline.inst.DeleteObjects),
                        new ButtonFunction(true),
                        new ButtonFunction("Add Bin", EditorTimeline.inst.AddBin),
                        new ButtonFunction("Remove Bin", EditorTimeline.inst.RemoveBin),
                        new ButtonFunction("Set Max Bin Count", () => EditorTimeline.inst.SetBinCount(EditorTimeline.MAX_BINS)),
                        new ButtonFunction("Set Default Bin Count", () => EditorTimeline.inst.SetBinCount(EditorTimeline.DEFAULT_BIN_COUNT))
                        );
                }));

            for (int i = 0; i < EventEditor.inst.EventHolders.transform.childCount - 1; i++)
            {
                int type = i;
                var et = EventEditor.inst.EventHolders.transform.GetChild(i).GetComponent<EventTrigger>();
                et.triggers.Clear();
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData => EditorTimeline.inst.isOverMainTimeline = true));
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => EditorTimeline.inst.isOverMainTimeline = false));
                et.triggers.Add(TriggerHelper.StartDragTrigger());
                et.triggers.Add(TriggerHelper.DragTrigger());
                et.triggers.Add(TriggerHelper.EndDragTrigger());
                et.triggers.Add(TriggerHelper.CreateEntry(EventTriggerType.PointerDown, eventData =>
                {
                    var pointerEventData = (PointerEventData)eventData;

                    var currentEvent = (RTEventEditor.EVENT_LIMIT * (EditorTimeline.inst.Layer + 1)) - RTEventEditor.EVENT_LIMIT + type;

                    switch (pointerEventData.button)
                    {
                        case PointerEventData.InputButton.Right:
                                if (RTEventEditor.EventTypes.Length > currentEvent && (ShowModdedUI && GameData.Current.events.Count > currentEvent || 10 > currentEvent))
                                    RTEventEditor.inst.NewKeyframeFromTimeline(currentEvent);
                                break;
                        case PointerEventData.InputButton.Middle:
                            if (RTEventEditor.EventTypes.Length > currentEvent && (ShowModdedUI && GameData.Current.events.Count > currentEvent || 10 > currentEvent) && GameData.Current.events[currentEvent].TryFindLastIndex(x => x.time < EditorTimeline.inst.GetTimelineTime(false), out int index))
                                RTEventEditor.inst.SetCurrentEvent(currentEvent, index);
                            break;
                    }
                }));
            }

            TriggerHelper.AddEventTriggers(EditorManager.inst.timelineScrollbar, TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
            {
                var pointerEventData = (PointerEventData)eventData;

                var scrollBar = EditorManager.inst.timelineScrollRectBar;
                float multiply = Input.GetKey(KeyCode.LeftAlt) ? 0.1f : Input.GetKey(KeyCode.LeftControl) ? 10f : 1f;

                scrollBar.value = pointerEventData.scrollDelta.y > 0f ? scrollBar.value + (0.005f * multiply) : pointerEventData.scrollDelta.y < 0f ? scrollBar.value - (0.005f * multiply) : 0f;
            }));

            #region Editor Themes

            EditorThemeManager.AddGraphic(binScrollImage, ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorTimeline.inst.binSlider.image, ThemeGroup.Slider_2_Handle, true);

            EditorThemeManager.AddScrollbar(EditorManager.inst.timelineScrollbar.GetComponent<Scrollbar>(),
                scrollbarGroup: ThemeGroup.Timeline_Scrollbar_Base, handleGroup: ThemeGroup.Timeline_Scrollbar, canSetScrollbarRounded: false);

            EditorThemeManager.AddGraphic(EditorManager.inst.timelineSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);

            EditorThemeManager.AddGraphic(EditorTimeline.inst.wholeTimeline.GetComponent<Image>(), ThemeGroup.Timeline_Time_Scrollbar);

            var zoomSliderBase = EditorManager.inst.zoomSlider.transform.parent;
            EditorThemeManager.AddGraphic(zoomSliderBase.GetComponent<Image>(), ThemeGroup.Background_1, true);
            EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Slider_2);
            EditorThemeManager.AddGraphic(zoomSliderBase.transform.GetChild(2).GetComponent<Image>(), ThemeGroup.Slider_2);

            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.transform.Find("Background").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.transform.Find("Fill Area/Fill").GetComponent<Image>(), ThemeGroup.Slider_2, true);
            EditorThemeManager.AddGraphic(EditorManager.inst.zoomSlider.image, ThemeGroup.Slider_2_Handle, true);

            #endregion

            EditorTimeline.inst.ClampTimeline(false);

            TriggerHelper.AddEventTriggers(EditorTimeline.inst.wholeTimeline.gameObject,
                TriggerHelper.CreateEntry(EventTriggerType.PointerEnter, eventData =>
                {
                    EditorTimeline.inst.isOverMainTimeline = true;
                    SetDialogStatus("Timeline", true);
                }),
                TriggerHelper.CreateEntry(EventTriggerType.PointerExit, eventData => EditorTimeline.inst.isOverMainTimeline = false),
                TriggerHelper.StartDragTrigger(), TriggerHelper.DragTrigger(), TriggerHelper.EndDragTrigger());
        }

        void SetupCreateObjects()
        {
            //var dialog = EditorManager.inst.GetDialog("Object Options Popup").Dialog;

            //var persistent = dialog.Find("persistent").gameObject.GetComponent<Button>();
            //dialog.Find("persistent/text").gameObject.GetComponent<Text>().text = "No Autokill";
            //persistent.onClick.ClearAll();
            //persistent.onClick.AddListener(() => ObjectEditor.inst.CreateNewNoAutokillObject());

            //var empty = dialog.Find("empty").gameObject.GetComponent<Button>();
            //empty.onClick.ClearAll();
            //empty.onClick.AddListener(() => ObjectEditor.inst.CreateNewEmptyObject());

            //var decoration = dialog.Find("decoration").gameObject.GetComponent<Button>();
            //decoration.onClick.ClearAll();
            //decoration.onClick.AddListener(() => ObjectEditor.inst.CreateNewDecorationObject());

            //var helper = dialog.Find("helper").gameObject.GetComponent<Button>();
            //helper.onClick.ClearAll();
            //helper.onClick.AddListener(() => ObjectEditor.inst.CreateNewHelperObject());

            //var normal = dialog.Find("normal").gameObject.GetComponent<Button>();
            //normal.onClick.ClearAll();
            //normal.onClick.AddListener(() => ObjectEditor.inst.CreateNewNormalObject());

            //var circle = dialog.Find("shapes/circle").gameObject.GetComponent<Button>();
            //circle.onClick.ClearAll();
            //circle.onClick.AddListener(() => ObjectEditor.inst.CreateNewCircleObject());

            //var triangle = dialog.Find("shapes/triangle").gameObject.GetComponent<Button>();
            //triangle.onClick.ClearAll();
            //triangle.onClick.AddListener(() => ObjectEditor.inst.CreateNewTriangleObject());

            //var text = dialog.Find("shapes/text").gameObject.GetComponent<Button>();
            //text.onClick.ClearAll();
            //text.onClick.AddListener(() => ObjectEditor.inst.CreateNewTextObject());

            //var hexagon = dialog.Find("shapes/hexagon").gameObject.GetComponent<Button>();
            //hexagon.onClick.ClearAll();
            //hexagon.onClick.AddListener(() => ObjectEditor.inst.CreateNewHexagonObject());

            ObjectTemplatePopup = GeneratePopup(EditorPopup.OBJECT_TEMPLATES_POPUP, "Pick a template", Vector2.zero, new Vector2(600f, 400f), placeholderText: "Search for template...");
        }

        void SetupTitleBar()
        {
            var settingsButton = titleBar.Find("Settings").GetComponent<Button>();
            var settingsDropdown = titleBar.Find("Edit/Edit Dropdown").gameObject.Duplicate(settingsButton.transform, "Settings Dropdown");
            CoreHelper.DestroyChildren(settingsDropdown.transform);
            EditorManager.inst.DropdownMenus.Insert(3, settingsDropdown);
            settingsButton.onClick.NewListener(() =>
            {
                EditorManager.inst.ToggleDropdown(settingsDropdown);
                EditorManager.inst.ClearPopups();
            });

            EditorHelper.AddEditorDropdown("Editor Settings", string.Empty, EditorHelper.SETTINGS_DROPDOWN, EditorSprites.EditSprite, () => RTSettingEditor.inst.OpenDialog());

            var quitToArcade = EditorHelper.AddEditorDropdown("Quit to Arcade", string.Empty, EditorHelper.FILE_DROPDOWN, titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to quit to the arcade? Any unsaved progress will be lost!", ArcadeHelper.QuitToArcade, HideWarningPopup);
            }, 7);
            EditorHelper.SetComplexity(quitToArcade, Complexity.Normal);

            var copyLevelToArcade = EditorHelper.AddEditorDropdown("Copy Level to Arcade", string.Empty, EditorHelper.FILE_DROPDOWN, SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/operations/right_small{FileFormat.PNG.Dot()}")), () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Load a level before trying to copy a level to the arcade folder!", 2f, EditorManager.NotificationType.Error);
                    return;
                }

                ShowWarningPopup("Are you sure you want to copy the level to the arcade folder?", () =>
                {
                    var name = MetaData.Current.beatmap.name;
                    name = RTString.ReplaceFormatting(name); // for cases where a user has used symbols not allowed.
                    name = RTFile.ValidateDirectory(name);
                    var directory = RTFile.CombinePaths(RTFile.ApplicationDirectory, LevelManager.ListPath, $"{name} [{MetaData.Current.arcadeID}]");

                    if (RTFile.DirectoryExists(directory))
                    {
                        var backupDirectory = directory.Replace(LevelManager.ListPath, "beatmaps/arcade backups");
                        RTFile.DeleteDirectory(backupDirectory);
                        RTFile.MoveDirectory(directory, backupDirectory);
                    }

                    var files = Directory.GetFiles(RTFile.BasePath, "*", SearchOption.AllDirectories);
                    for (int i = 0; i < files.Length; i++)
                    {
                        var file = files[i];
                        if (!EditorServerManager.inst.VerifyFile(Path.GetFileName(file)))
                            continue;

                        var fileDirectory = RTFile.GetDirectory(file);
                        var fileDestination = file.Replace(fileDirectory, directory);
                        RTFile.CreateDirectory(RTFile.GetDirectory(fileDestination));
                        RTFile.CopyFile(file, fileDestination);
                    }

                    EditorManager.inst.DisplayNotification($"Successfully copied {name} to {LevelManager.Path}!", 2f, EditorManager.NotificationType.Success);
                    HideWarningPopup();
                }, HideWarningPopup);
            }, 7);
            EditorHelper.SetComplexity(copyLevelToArcade, Complexity.Normal);

            var restartEditor = EditorHelper.AddEditorDropdown("Restart Editor", string.Empty, EditorHelper.FILE_DROPDOWN, EditorSprites.ReloadSprite, () =>
            {
                DG.Tweening.DOTween.Clear();

                RTLevel.Reinit(false);
                GameData.Current = null;
                TooltipHelper.InitTooltips();
                SceneHelper.LoadEditorWithProgress();
            }, 7);
            EditorHelper.SetComplexity(restartEditor, Complexity.Normal);

            var openLevelBrowser = EditorHelper.AddEditorDropdown("Open Level Browser", string.Empty, EditorHelper.FILE_DROPDOWN, titleBar.Find("File/File Dropdown/Open/Image").GetComponent<Image>().sprite, () =>
            {
                BrowserPopup.Open();
                EditorLevelManager.inst.RefreshFileBrowserLevels();
            }, 3);
            EditorHelper.SetComplexity(openLevelBrowser, Complexity.Normal);

            var convertVGToLS = EditorHelper.AddEditorDropdown("Convert VG to LS", string.Empty, EditorHelper.FILE_DROPDOWN, EditorSprites.SearchSprite, ConvertVGToLS, 4);
            EditorHelper.SetComplexity(convertVGToLS, Complexity.Normal);

            var addFileToLevelFolder = EditorHelper.AddEditorDropdown("Add File to Level", string.Empty, EditorHelper.FILE_DROPDOWN, EditorSprites.SearchSprite, () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Cannot add a file to level until a level has been loaded.", 4f, EditorManager.NotificationType.Warning);
                    return;
                }

                BrowserPopup.Open();
                RTFileBrowser.inst.UpdateBrowserFile(RTFile.DotFormats(FileFormat.OGG, FileFormat.WAV, FileFormat.PNG, FileFormat.JPG, FileFormat.MP4, FileFormat.LSP, FileFormat.VGP), onSelectFile: _val =>
                {
                    var selectedFile = RTFile.ReplaceSlash(_val);
                    var fileFormat = RTFile.GetFileFormat(selectedFile);

                    switch (fileFormat)
                    {
                        case FileFormat.MP4:
                        case FileFormat.MOV: {
                                var copyTo = selectedFile.Replace(RTFile.AppendEndSlash(RTFile.GetDirectory(_val)), RTFile.AppendEndSlash(RTFile.BasePath)).Replace(Path.GetFileName(_val),
                                    RTFile.FileIsFormat(_val, FileFormat.MP4) ? $"bg{FileFormat.MP4.Dot()}" : $"bg{FileFormat.MOV.Dot()}");

                                if (RTFile.CopyFile(selectedFile, copyTo) && CoreConfig.Instance.EnableVideoBackground.Value)
                                {
                                    RTVideoManager.inst.Play(copyTo, 1f);
                                    EditorManager.inst.DisplayNotification($"Copied file {Path.GetFileName(selectedFile)} and started Video BG!", 2f, EditorManager.NotificationType.Success);
                                }
                                else
                                    RTVideoManager.inst.Stop();

                                return;
                            }
                        case FileFormat.LSP: {
                                var prefab = Prefab.Parse(JSON.Parse(RTFile.ReadFromFile(selectedFile)));

                                RTPrefabEditor.inst.OpenPopup();
                                RTPrefabEditor.inst.ImportPrefabIntoLevel(prefab);
                                EditorManager.inst.DisplayNotification($"Imported prefab {Path.GetFileName(selectedFile)} into level!", 2f, EditorManager.NotificationType.Success);

                                return;
                            }
                        case FileFormat.VGP: {
                                var prefab = Prefab.ParseVG(JSON.Parse(RTFile.ReadFromFile(selectedFile)));

                                RTPrefabEditor.inst.OpenPopup();
                                RTPrefabEditor.inst.ImportPrefabIntoLevel(prefab);
                                EditorManager.inst.DisplayNotification($"Converted & imported prefab {Path.GetFileName(selectedFile)} into level!", 2f, EditorManager.NotificationType.Success);

                                return;
                            }
                    }

                    var destination = selectedFile.Replace(RTFile.AppendEndSlash(RTFile.GetDirectory(selectedFile)), RTFile.AppendEndSlash(RTFile.BasePath));
                    if (RTFile.CopyFile(selectedFile, destination))
                        EditorManager.inst.DisplayNotification($"Copied file {Path.GetFileName(selectedFile)}!", 2f, EditorManager.NotificationType.Success);
                    else
                        EditorManager.inst.DisplayNotification($"Could not copy file {Path.GetFileName(selectedFile)}.", 2f, EditorManager.NotificationType.Error);
                });
            }, 5);
            EditorHelper.SetComplexity(addFileToLevelFolder, Complexity.Normal);

            var reloadLevel = EditorHelper.AddEditorDropdown("Reload Level", string.Empty, EditorHelper.FILE_DROPDOWN, EditorSprites.ReloadSprite, () =>
            {
                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Cannot reload a level without one already loaded.", 2f, EditorManager.NotificationType.Warning);
                    return;
                }

                ShowWarningPopup("Are you sure you want to reload the level?", () =>
                {
                    if (EditorLevelManager.inst.CurrentLevel)
                    {
                        if (GameData.Current)
                            GameData.Current.SaveData(RTFile.CombinePaths(EditorLevelManager.inst.CurrentLevel.path, "reload-level-backup.lsb"));
                        EditorLevelManager.inst.LoadLevel(EditorLevelManager.inst.CurrentLevel);
                    }
                    else
                        EditorManager.inst.DisplayNotification("Level does not exist.", 2f, EditorManager.NotificationType.Error);
                }, HideWarningPopup);
            }, 4);
            EditorHelper.SetComplexity(reloadLevel, Complexity.Normal);

            EditorHelper.AddEditorDropdown("Editor Config", string.Empty, EditorHelper.SETTINGS_DROPDOWN, SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/preferences{FileFormat.PNG.Dot()}")), () =>
            {
                ConfigManager.inst.SetTab(2);
                ConfigManager.inst.Show();
            });

            var clearSpriteData = EditorHelper.AddEditorDropdown("Clear Sprite Data", string.Empty, EditorHelper.EDIT_DROPDOWN, titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to clear sprite data? Any Image Shapes that use a stored image will have their images cleared and you will need to set them again.", () =>
                {
                    GameData.Current.assets.sprites.Clear();
                    HideWarningPopup();
                }, HideWarningPopup);
            });
            EditorHelper.SetComplexity(clearSpriteData, Complexity.Advanced);

            var clearModifierPrefabs = EditorHelper.AddEditorDropdown("Clear Modifier Prefabs", string.Empty, EditorHelper.EDIT_DROPDOWN, titleBar.Find("File/File Dropdown/Quit to Main Menu/Image").GetComponent<Image>().sprite, () =>
            {
                ShowWarningPopup("Are you sure you want to remove all Prefab Objects spawned from modifiers?", () =>
                {
                    var prefabObjects = GameData.Current.prefabObjects;
                    for (int i = prefabObjects.Count - 1; i >= 0; i--)
                    {
                        var prefabObject = prefabObjects[i];
                        if (prefabObject.fromModifier)
                        {
                            RTLevel.Current?.UpdatePrefab(prefabObject, false, false);
                            prefabObjects.RemoveAt(i);
                        }
                    }

                    RTLevel.Current?.RecalculateObjectStates();

                    HideWarningPopup();
                }, HideWarningPopup);
            });
            EditorHelper.SetComplexity(clearModifierPrefabs, Complexity.Advanced);

            var resetEventOffsets = EditorHelper.AddEditorDropdown("Reset Event Offsets", string.Empty, EditorHelper.EDIT_DROPDOWN, EditorSprites.CloseSprite, () =>
            {
                RTLevel.Current?.eventEngine?.ResetOffsets();

                EditorManager.inst.DisplayNotification("Event Offsets have been reset.", 1.4f, EditorManager.NotificationType.Success);
            });
            EditorHelper.SetComplexity(resetEventOffsets, Complexity.Advanced);

            var renderWaveform = EditorHelper.AddEditorDropdown("Render Waveform", string.Empty, EditorHelper.EDIT_DROPDOWN, EditorSprites.ReloadSprite, () =>
            {
                if (EditorConfig.Instance.WaveformGenerate.Value)
                    StartCoroutine(EditorTimeline.inst.AssignTimelineTexture(AudioManager.inst.CurrentAudioSource.clip, true));
                else
                    EditorTimeline.inst.SetTimelineSprite(null);
            });
            EditorHelper.SetComplexity(renderWaveform, Complexity.Normal);

            var deactivateModifiers = EditorHelper.AddEditorDropdown("Deactivate Modifiers", string.Empty, EditorHelper.EDIT_DROPDOWN, EditorSprites.CloseSprite, () =>
            {
                if (!GameData.Current)
                    return;

                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Load a level first!", 1f, EditorManager.NotificationType.Warning);
                    return;
                }

                var beatmapObjects = GameData.Current.beatmapObjects.FindAll(x => x.modifiers.Count > 0);
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var beatmapObject = beatmapObjects[i];

                    for (int j = 0; j < beatmapObject.modifiers.Count; j++)
                    {
                        var modifier = beatmapObject.modifiers[j];
                        modifier.active = false;
                        modifier.Inactive?.Invoke(modifier, beatmapObject, null);
                    }
                }

                EditorManager.inst.DisplayNotification("Modifiers have been deactivated.", 1.4f, EditorManager.NotificationType.Success);
            });
            EditorHelper.SetComplexity(deactivateModifiers, Complexity.Advanced);

            var resetObjectVariables = EditorHelper.AddEditorDropdown("Reset object variables", string.Empty, EditorHelper.EDIT_DROPDOWN, EditorSprites.CloseSprite, () =>
            {
                if (!GameData.Current)
                    return;

                if (!EditorManager.inst.hasLoadedLevel)
                {
                    EditorManager.inst.DisplayNotification("Load a level first!", 1f, EditorManager.NotificationType.Warning);
                    return;
                }

                var beatmapObjects = GameData.Current.beatmapObjects;
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var beatmapObject = beatmapObjects[i];

                    beatmapObject.integerVariable = 0;
                }

                EditorManager.inst.DisplayNotification("Reset all integer variables to 0.", 1.4f, EditorManager.NotificationType.Success);
            });
            EditorHelper.SetComplexity(resetObjectVariables, Complexity.Advanced);

            EditorHelper.AddEditorDropdown("Get Example", string.Empty, EditorHelper.VIEW_DROPDOWN, SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/example{FileFormat.PNG.Dot()}")), () =>
            {
                if (!Example.Current)
                    Example.Init();
                else
                    Example.Current.brain?.Notice(ExampleBrain.Notices.ALREADY_SPAWNED);
            });
            
            EditorHelper.AddEditorDropdown("Show Config Manager", string.Empty, EditorHelper.VIEW_DROPDOWN, SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/preferences{FileFormat.PNG.Dot()}")), ConfigManager.inst.Show);

            EditorHelper.AddEditorDropdown("Open Color Picker", string.Empty, EditorHelper.VIEW_DROPDOWN, EditorSprites.DropperSprite, () =>
            {
                RTColorPicker.inst.Show(Color.white,
                    (col, hex) => { },
                    (col, hex) =>
                    {
                        LSText.CopyToClipboard(hex);
                    });
            });

            titleBar.Find("Steam/Text").GetComponent<Text>().text = "Upload";
            var steamLayoutElement = titleBar.Find("Steam").GetComponent<LayoutElement>();
            steamLayoutElement.minWidth = 95f;
            steamLayoutElement.preferredWidth = 95f;

            EditorHelper.AddEditorDropdown("Login", string.Empty, EditorHelper.UPLOAD_DROPDOWN, SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/login{FileFormat.PNG.Dot()}")), () =>
            {
                if (LegacyPlugin.authData != null && LegacyPlugin.authData["access_token"] != null && LegacyPlugin.authData["refresh_token"] != null)
                {
                    CoroutineHelper.StartCoroutine(EditorServerManager.inst.RefreshTokens(null));
                    return;
                }
                EditorServerManager.inst.ShowLoginPopup(null);
            });

            titleBar.Find("Help/Help Dropdown/Join Discord/Text").GetComponent<Text>().text = "Modder's Discord";
            titleBar.Find("Help/Help Dropdown/Watch Tutorials/Text").AsRT().sizeDelta = new Vector2(200f, 0f);
            titleBar.Find("Help/Help Dropdown/Watch Tutorials/Text").GetComponent<Text>().text = "Watch Mod Showcases";
            titleBar.Find("Help/Help Dropdown/Community Guides/Text").GetComponent<Text>().text = "Open Source";
            titleBar.Find("Help/Help Dropdown/Which songs can I use?").gameObject.SetActive(false);
            var saveAsDropdown = titleBar.Find("File/File Dropdown/Save As").gameObject;
            saveAsDropdown.SetActive(true);
            EditorHelper.SetComplexity(saveAsDropdown, Complexity.Normal);

            undoButton = EditorManager.inst.undoButton.AddComponent<FunctionButtonStorage>();
            undoButton.Assign();
            redoButton = EditorManager.inst.redoButton.AddComponent<FunctionButtonStorage>();
            redoButton.Assign();
        }

        void SetupDoggo()
        {
            var doggoBase = Creator.NewUIObject("loading base", InfoPopup.GameObject.transform);
            var doggoLayout = doggoBase.AddComponent<LayoutElement>();
            doggoLayout.ignoreLayout = true;
            doggoBase.transform.AsRT().anchoredPosition = new Vector2(0f, -75f);
            doggoBase.transform.AsRT().sizeDelta = new Vector2(122f, 122f);

            var doggoBaseImage = doggoBase.AddComponent<Image>();
            var doggoMask = doggoBase.AddComponent<Mask>();
            doggoMask.showMaskGraphic = false;
            EditorThemeManager.AddGraphic(doggoBaseImage, ThemeGroup.Null, true);

            var doggoObject = Creator.NewUIObject("loading", doggoBase.transform);
            doggoObject.transform.localScale = Vector3.one;

            InfoPopup.Doggo = doggoObject.AddComponent<Image>();

            doggoObject.transform.AsRT().anchoredPosition = Vector2.zero;
            doggoObject.transform.AsRT().sizeDelta = new Vector2(122f, 122f);
            InfoPopup.Doggo.sprite = EditorManager.inst.loadingImage.sprite;

            InfoPopup.RenderSize(new Vector2(500f, 320f));
        }

        void SetupPaths()
        {
            EditorLevelManager.inst.OpenLevelPopup.InitTopElementsParent();
            EditorLevelManager.inst.OpenLevelPopup.InitPath(
                getValue: () => EditorPath,
                setValue: _val => EditorPath = _val,
                onEndEdit: _val => UpdateEditorPath(false));
            EditorLevelManager.inst.OpenLevelPopup.InitReload(EditorLevelManager.inst.LoadLevels);
            EditorLevelManager.inst.OpenLevelPopup.InitAscendToggle(
                getValue: () => levelAscend,
                setValue: _val =>
                {
                    levelAscend = _val;
                    EditorLevelManager.inst.RenderLevels();
                    SaveGlobalSettings();
                });
            EditorLevelManager.inst.OpenLevelPopup.InitSortDropdown(
                getValue: () => (int)levelSort,
                setValue: _val =>
                {
                    levelSort = (LevelSort)_val;
                    EditorLevelManager.inst.RenderLevels();
                    SaveGlobalSettings();
                },
                options: CoreHelper.StringToOptionData("Cover", "Artist", "Creator", "Folder", "Title", "Difficulty", "Date Edited", "Date Created"));
            EditorLevelManager.inst.OpenLevelPopup.TopElements.sizeDelta = new Vector2(90f, 32f);

            var panel = EditorLevelManager.inst.OpenLevelPopup.TopPanel;

            TooltipHelper.RemoveTooltip(EditorLevelManager.inst.OpenLevelPopup.SortDropdown.gameObject);
            TooltipHelper.AssignTooltip(EditorLevelManager.inst.OpenLevelPopup.SortDropdown.gameObject, "Level Sort Dropdown");

            EditorHelper.SetComplexity(EditorLevelManager.inst.OpenLevelPopup.SortDropdown.gameObject, Complexity.Normal);

            TooltipHelper.RemoveTooltip(EditorLevelManager.inst.OpenLevelPopup.AscendToggle.gameObject);
            TooltipHelper.AssignTooltip(EditorLevelManager.inst.OpenLevelPopup.AscendToggle.gameObject, "Level Ascend Toggle");

            EditorHelper.SetComplexity(EditorLevelManager.inst.OpenLevelPopup.AscendToggle.gameObject, Complexity.Normal);

            var contextClickable = EditorLevelManager.inst.OpenLevelPopup.GameObject.AddComponent<ContextClickable>();
            contextClickable.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Create folder", () =>
                    {
                        ShowFolderCreator(RTFile.CombinePaths(BeatmapsPath, EditorPath), () => { EditorLevelManager.inst.LoadLevels(); HideNameEditor(); });
                    }),
                    new ButtonFunction("Create level", EditorManager.inst.OpenNewLevelPopup),
                    new ButtonFunction("Paste", EditorLevelManager.inst.PasteLevel),
                    new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));
            };

            TooltipHelper.AssignTooltip(EditorLevelManager.inst.OpenLevelPopup.PathField.gameObject, "Editor Path", 3f);
            EditorHelper.SetComplexity(EditorLevelManager.inst.OpenLevelPopup.PathField.gameObject, Complexity.Advanced);

            var levelClickable = EditorLevelManager.inst.OpenLevelPopup.PathField.gameObject.AddComponent<Clickable>();
            levelClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Level folder", () =>
                    {
                        BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            EditorLevelManager.inst.OpenLevelPopup.PathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Editor path to {EditorPath}!", 2f, EditorManager.NotificationType.Success);
                            BrowserPopup.Close();
                            UpdateEditorPath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenLevelListFolder));
            };

            PrefabPopups.External.InitTopElementsParent();
            PrefabPopups.External.InitReload(() => RTPrefabEditor.inst.LoadPrefabs(RTPrefabEditor.inst.RenderExternalPrefabs));
            PrefabPopups.External.InitPath(
                getValue: () => PrefabPath,
                setValue: _val => PrefabPath = _val,
                onEndEdit: _val => UpdatePrefabPath(false));

            TooltipHelper.AssignTooltip(PrefabPopups.External.PathField.gameObject, "Prefab Path", 3f);

            var prefabPathClickable = PrefabPopups.External.PathField.gameObject.AddComponent<Clickable>();
            prefabPathClickable.onDown = pointerEventData =>
            {
                if (pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Set Prefab folder", () =>
                    {
                        BrowserPopup.Open();
                        RTFileBrowser.inst.UpdateBrowserFolder(_val =>
                        {
                            if (!_val.Replace("\\", "/").Contains(RTFile.ApplicationDirectory + "beatmaps/"))
                            {
                                EditorManager.inst.DisplayNotification($"Path does not contain the proper directory.", 2f, EditorManager.NotificationType.Warning);
                                return;
                            }

                            PrefabPopups.External.PathField.text = _val.Replace("\\", "/").Replace(RTFile.ApplicationDirectory.Replace("\\", "/") + "beatmaps/", "");
                            EditorManager.inst.DisplayNotification($"Set Prefab path to {PrefabPath}!", 2f, EditorManager.NotificationType.Success);
                            BrowserPopup.Close();
                            UpdatePrefabPath(false);
                        });
                    }),
                    new ButtonFunction("Open List in File Explorer", OpenPrefabListFolder),
                    new ButtonFunction("Set as Default for Level", () =>
                    {
                        editorInfo.prefabPath = prefabPath;
                        EditorManager.inst.DisplayNotification($"Set current prefab folder [ {prefabPath} ] as the default for the level!", 5f, EditorManager.NotificationType.Success);
                    }, "Prefab Default Path"),
                    new ButtonFunction("Remove Default", () =>
                    {
                        editorInfo.prefabPath = null;
                        EditorManager.inst.DisplayNotification($"Removed default prefab folder.", 5f, EditorManager.NotificationType.Success);
                    }, "Prefab Default Path"));
            };

            EditorHelper.SetComplexity(PrefabPopups.External.PathField.gameObject, Complexity.Advanced);
        }

        void SetupFileBrowser()
        {
            var fileBrowser = EditorLevelManager.inst.NewLevelPopup.GameObject.transform.Find("Browser Popup").gameObject.Duplicate(EditorManager.inst.GetDialog("New File Popup").Dialog.parent, "Browser Popup");
            fileBrowser.gameObject.SetActive(false);
            UIManager.SetRectTransform(fileBrowser.transform.AsRT(), Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(600f, 364f));
            var close = fileBrowser.transform.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(() => BrowserPopup.Close());
            fileBrowser.transform.Find("GameObject").gameObject.SetActive(false);

            var selectGUI = fileBrowser.AddComponent<SelectGUI>();
            selectGUI.target = fileBrowser.transform;

            var rtfb = fileBrowser.AddComponent<RTFileBrowser>();
            var fileBrowserBase = fileBrowser.GetComponent<FileBrowserTest>();
            rtfb.viewport = fileBrowserBase.viewport;
            rtfb.backPrefab = fileBrowserBase.backPrefab;

            rtfb.folderPrefab = fileBrowserBase.folderPrefab.Duplicate(EditorPrefabHolder.Instance.PrefabParent, fileBrowserBase.folderPrefab.name);
            var folderPrefabStorage = rtfb.folderPrefab.AddComponent<FunctionButtonStorage>();
            folderPrefabStorage.button = rtfb.folderPrefab.GetComponent<Button>();
            folderPrefabStorage.label = rtfb.folderPrefab.transform.GetChild(0).GetComponent<Text>();

            rtfb.folderBar = fileBrowserBase.folderBar;
            rtfb.oggFileInput = fileBrowserBase.oggFileInput;
            rtfb.filePrefab = fileBrowserBase.filePrefab.Duplicate(EditorPrefabHolder.Instance.PrefabParent, fileBrowserBase.filePrefab.name);
            var filePrefabStorage = rtfb.filePrefab.AddComponent<FunctionButtonStorage>();
            filePrefabStorage.button = rtfb.filePrefab.GetComponent<Button>();
            filePrefabStorage.label = rtfb.filePrefab.transform.GetChild(0).GetComponent<Text>();

            Destroy(fileBrowserBase);

            EditorHelper.AddEditorPopup(EditorPopup.BROWSER_POPUP, fileBrowser);

            EditorThemeManager.AddGraphic(fileBrowser.GetComponent<Image>(), ThemeGroup.Background_1, true);

            var panel = fileBrowser.transform.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(panel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);
            EditorThemeManager.AddSelectable(close, ThemeGroup.Close);

            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(panel.transform.Find("Text").GetComponent<TextMeshProUGUI>());

            EditorThemeManager.AddInputField(fileBrowser.transform.Find("folder-bar").GetComponent<InputField>());

            try
            {
                BrowserPopup = new EditorPopup(EditorPopup.BROWSER_POPUP);
                BrowserPopup.Assign(BrowserPopup.GetLegacyDialog().Dialog.gameObject);
                BrowserPopup.size = BrowserPopup.GameObject.transform.AsRT().sizeDelta;
                editorPopups.Add(BrowserPopup);

                if (BrowserPopup.Title)
                    BrowserPopup.title = BrowserPopup.Title.text;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        void SetupTimelinePreview()
        {
            if (GameManager.inst.playerGUI.transform.TryFind("Interface", out Transform ic))
                Destroy(ic.gameObject); // Destroys the Interface so the duplicate doesn't get in the way of the editor.

            var gui = GameManager.inst.playerGUI.Duplicate(EditorManager.inst.dialogs.parent);
            gui.transform.SetSiblingIndex(0);

            Destroy(gui.transform.Find("Health").gameObject);
            if (gui.transform.TryFind("Interface", out Transform dupIC))
                Destroy(dupIC.gameObject);

            gui.transform.localPosition = new Vector3(-382.5f, 184.05f, 0f);
            gui.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            gui.SetActive(true);
            timelinePreview = gui.transform.Find("Timeline");
            timelinePosition = timelinePreview.Find("Base/position").GetComponent<RectTransform>();

            timelinePreviewPlayer = timelinePreview.Find("Base/position").GetComponent<Image>();
            timelinePreviewLeftCap = timelinePreview.Find("Base/Image").GetComponent<Image>();
            timelinePreviewRightCap = timelinePreview.Find("Base/Image 1").GetComponent<Image>();
            timelinePreviewLine = timelinePreview.Find("Base").GetComponent<Image>();

            timelinePreviewPlayer.material = LegacyResources.canvasImageMask;
            timelinePreviewLeftCap.material = LegacyResources.canvasImageMask;
            timelinePreviewRightCap.material = LegacyResources.canvasImageMask;
            timelinePreviewLine.material = LegacyResources.canvasImageMask;
        }

        void SetupTimelineElements()
        {
            CoreHelper.Log($"Setting Timeline Cursor Colors");

            try
            {
                var top = Creator.NewUIObject("Top", EditorTimeline.inst.wholeTimeline, 3); // creates a cover so the bin scrolling doesn't overlay outside the regular slider range.
                RectValues.Default.AnchorMax(0f, 1f).AnchorMin(0f, 1f).Pivot(0f, 1f).SizeDelta(1920f, 25f).AssignToRectTransform(top.transform.AsRT());
                EditorThemeManager.AddGraphic(top.AddComponent<Image>(), ThemeGroup.Background_3);

                EditorTimeline.inst.timelineSliderHandle = EditorTimeline.inst.wholeTimeline.Find("Slider_Parent/Slider/Handle Slide Area/Image/Handle").GetComponent<Image>();
                EditorTimeline.inst.timelineSliderRuler = EditorTimeline.inst.wholeTimeline.Find("Slider_Parent/Slider/Handle Slide Area/Image").GetComponent<Image>();

                EditorTimeline.inst.UpdateTimelineColors();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"SetupTimelineElements Error {ex}");
            }
        }

        void SetupTimelineGrid()
        {
            var grid = Creator.NewUIObject("grid", EditorManager.inst.timeline.transform, 0);

            grid.AddComponent<CanvasRenderer>();

            var gridLayout = grid.AddComponent<LayoutElement>();
            gridLayout.ignoreLayout = true;

            UIManager.SetRectTransform(grid.transform.AsRT(), Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero, Vector2.zero);

            EditorTimeline.inst.timelineGridRenderer = grid.AddComponent<GridRenderer>();

            var config = EditorConfig.Instance;

            EditorTimeline.inst.timelineGridRenderer.color = config.TimelineGridColor.Value;
            EditorTimeline.inst.timelineGridRenderer.thickness = config.TimelineGridThickness.Value;

            EditorTimeline.inst.timelineGridRenderer.enabled = config.TimelineGridEnabled.Value;

            var gridCanvasGroup = grid.AddComponent<CanvasGroup>();
            gridCanvasGroup.blocksRaycasts = false;
            gridCanvasGroup.interactable = false;
        }

        void SetupGrid()
        {
            try
            {
                LSRenderManager.inst.FrontWorldCanvas.transform.localPosition = new Vector3(0f, 0f, -10000f);
                var grid = Creator.NewUIObject("Grid", LSRenderManager.inst.FrontWorldCanvas);
                
                previewGrid = grid.AddComponent<GridRenderer>();
                previewGrid.gridCellSize = new Vector2Int(80, 80);
                previewGrid.gridCellSpacing = new Vector2(10f, 10f);
                previewGrid.gridSize = new Vector2(1f, 1f);
                previewGrid.color = new Color(1f, 1f, 1f);
                previewGrid.thickness = 0.1f;

                grid.transform.AsRT().anchoredPosition = new Vector2(-40f, -40f);
                grid.transform.AsRT().sizeDelta = Vector2.one;

                UpdateGrid();
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"SetupGrid Exception {ex}");
            }
        }

        void SetupNewFilePopup()
        {
            var newFilePopupBase = EditorLevelManager.inst.NewLevelPopup.GameObject.transform;

            var newFilePopup = newFilePopupBase.Find("New File Popup");
            newFilePopup.transform.AsRT().sizeDelta = new Vector2(500f, 428f);

            var newFilePopupDetection = newFilePopup.gameObject.AddComponent<Clickable>();
            newFilePopupDetection.onEnable = _val =>
            {
                if (_val)
                    return;

                if (LevelTemplateEditor.inst.choosingLevelTemplate)
                    EditorLevelManager.inst.OpenLevelPopup.Close();

                LevelTemplateEditor.inst.choosingLevelTemplate = false;
                LevelTemplateEditor.inst.Dialog.Close();
            };

            EditorThemeManager.AddGraphic(newFilePopup.GetComponent<Image>(), ThemeGroup.Background_1, true);

            var newFilePopupPanel = newFilePopup.Find("Panel").gameObject;
            EditorThemeManager.AddGraphic(newFilePopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            var newFilePopupClose = newFilePopupPanel.transform.Find("x").gameObject;
            EditorThemeManager.AddSelectable(newFilePopupClose.GetComponent<Button>(), ThemeGroup.Close);

            var newFilePopupCloseX = newFilePopupClose.transform.GetChild(0).gameObject;
            EditorThemeManager.AddGraphic(newFilePopupClose.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            var newFilePopupTitle = newFilePopupPanel.transform.Find("Title").gameObject;
            EditorThemeManager.AddLightText(newFilePopupPanel.transform.Find("Title").GetComponent<TextMeshProUGUI>());

            var openFilePopupSelect = newFilePopup.gameObject.AddComponent<SelectGUI>();
            openFilePopupSelect.target = newFilePopup;
            openFilePopupSelect.ogPos = newFilePopup.position;

            var pather = newFilePopup.Find("GameObject");
            var spacer = newFilePopup.GetChild(6);

            var path = pather.Find("song-filename").GetComponent<InputField>();
            path.onEndEdit.ClearAll();
            path.onValueChanged.NewListener(_val => EditorLevelManager.inst.newLevelSettings.audioPath = _val);

            spacer.gameObject.Duplicate(newFilePopup, "spacer", 7);

            var browseBase = pather.gameObject.Duplicate(newFilePopup, "browse", 7);

            Destroy(browseBase.transform.GetChild(0).gameObject);
            Destroy(pather.GetChild(1).gameObject);

            var songPathLabel = newFilePopup.Find("Song Filename").GetComponent<Text>();
            songPathLabel.text = "Song Path";

            var browseLocal = browseBase.transform.Find("browse");
            var browseLocalText = browseLocal.Find("Text").GetComponent<Text>();
            browseLocalText.text = SYSTEM_BROWSER;
            var browseLocalButton = browseLocal.GetComponent<Button>();
            browseLocalButton.onClick.NewListener(() =>
            {
                string text = FileBrowser.OpenSingleFile("Select a song to use!", RTFile.ApplicationDirectory, "ogg", "wav", "mp3");
                if (string.IsNullOrEmpty(text))
                    return;

                path.text = text;
                Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 1);
            });

            var browseInternal = browseLocal.gameObject.Duplicate(browseBase.transform, "internal browse");
            var browseInternalText = browseInternal.transform.Find("Text").GetComponent<Text>();
            browseInternalText.text = EDITOR_BROWSER;
            var browseInternalButton = browseInternal.GetComponent<Button>();
            browseInternalButton.onClick.NewListener(() =>
            {
                BrowserPopup.Open();
                RTFileBrowser.inst.UpdateBrowserFile(RTFile.AudioDotFormats, onSelectFile: _val =>
                {
                    if (string.IsNullOrEmpty(_val))
                        return;

                    path.text = _val;
                    Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 1);
                    BrowserPopup.Close();
                });
            });

            var chooseTemplate = browseLocal.gameObject.Duplicate(newFilePopup, "choose template", 8);
            var chooseTemplateText = chooseTemplate.transform.Find("Text").GetComponent<Text>();
            chooseTemplateText.text = "Choose Template";
            var chooseTemplateButton = chooseTemplate.GetComponent<Button>();
            chooseTemplateButton.onClick.NewListener(() =>
            {
                LevelTemplateEditor.inst.RenderLevelTemplates();
                LevelTemplateEditor.inst.Dialog.Open();
            });
            chooseTemplate.transform.AsRT().sizeDelta = new Vector2(384f, 32f);

            spacer.gameObject.Duplicate(newFilePopup, "spacer", 8);

            var hlg = browseBase.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8f;

            pather.gameObject.AddComponent<HorizontalLayoutGroup>();

            var levelNameLabel = newFilePopup.Find("Level Name");
            var levelName = newFilePopup.Find("level-name");

            var levelNameField = levelName.GetComponent<InputField>();
            levelNameField.onEndEdit.ClearAll();
            levelNameField.onValueChanged.NewListener(_val =>
            {
                EditorLevelManager.inst.newLevelSettings.levelName = RTFile.ValidateDirectory(_val);
                levelNameField.text = EditorLevelManager.inst.newLevelSettings.levelName;
                Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 2);
            });

            var songTitleLabel = levelNameLabel.gameObject.Duplicate(newFilePopup, "Song Name", 4);
            var songTitleLabelText = songTitleLabel.GetComponent<Text>();
            songTitleLabelText.text = "Song Title";
            var songTitle = levelName.gameObject.Duplicate(newFilePopup, "song-title", 5);
            var songTitleInputField = songTitle.GetComponent<InputField>();
            songTitleInputField.onEndEdit.ClearAll();
            songTitleInputField.SetTextWithoutNotify(EditorLevelManager.DEFAULT_SONG_TITLE);
            songTitleInputField.onValueChanged.NewListener(_val =>
            {
                EditorLevelManager.inst.newLevelSettings.songTitle = _val;
                EditorLevelManager.inst.NewLevelPopup.RenderFormat();
                Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 3);
            });

            var songArtistLabel = levelNameLabel.gameObject.Duplicate(newFilePopup, "Song Artist", 4);
            var songArtistLabelText = songArtistLabel.GetComponent<Text>();
            songArtistLabelText.text = "Song Artist";
            var songArtist = levelName.gameObject.Duplicate(newFilePopup, "song-artist", 5);
            var songArtistInputField = songArtist.GetComponent<InputField>();
            songArtistInputField.onEndEdit.ClearAll();
            songArtistInputField.SetTextWithoutNotify(EditorLevelManager.DEFAULT_ARTIST);
            songArtistInputField.onValueChanged.NewListener(_val =>
            {
                EditorLevelManager.inst.newLevelSettings.songArtist = _val;
                EditorLevelManager.inst.NewLevelPopup.RenderFormat();
                // todo: add tutorial
            });

            var formatLabel = levelNameLabel.gameObject.Duplicate(newFilePopup, "Song Format", 4);
            var formatLabelText = formatLabel.GetComponent<Text>();
            formatLabelText.text = $"{EditorLevelManager.DEFAULT_ARTIST} - {EditorLevelManager.DEFAULT_SONG_TITLE}";
            EditorLevelManager.inst.NewLevelPopup.FormatLabel = formatLabelText;

            var songSpacer = Creator.NewUIObject("spacer", newFilePopup, 4);
            songSpacer.transform.AsRT().sizeDelta = new Vector2(0f, 16f);

            var difficultyLabel = levelNameLabel.gameObject.Duplicate(newFilePopup, "Song Name", 4);
            var difficultyLabelText = difficultyLabel.GetComponent<Text>();
            difficultyLabelText.text = "Difficulty";
            var difficultyToggles = Creator.NewUIObject("difficulty", newFilePopup, 5);
            difficultyToggles.transform.AsRT().sizeDelta = new Vector2(384f, 32f);
            var difficultyHorizontalLayoutGroup = difficultyToggles.AddComponent<HorizontalLayoutGroup>();
            difficultyHorizontalLayoutGroup.childForceExpandHeight = true;
            difficultyHorizontalLayoutGroup.childForceExpandWidth = true;
            difficultyHorizontalLayoutGroup.childScaleHeight = false;
            difficultyHorizontalLayoutGroup.childScaleWidth = false;
            EditorLevelManager.inst.NewLevelPopup.DifficultyContent = difficultyToggles.transform.AsRT();

            CoroutineHelper.WaitUntil(
                predicate: () => RTMetaDataEditor.inst && RTMetaDataEditor.inst.difficultyToggle,
                action: EditorLevelManager.inst.RenderNewLevelDifficulty);

            var create = newFilePopup.Find("submit").GetComponent<Button>();
            create.onClick.NewListener(() =>
            {
                EditorLevelManager.inst.CreateNewLevel();
                Example.Current?.tutorials?.AdvanceTutorial(ExampleTutorial.CREATE_LEVEL, 4);
            });

            EditorThemeManager.AddLightText(levelNameLabel.GetComponent<Text>());
            EditorThemeManager.AddInputField(levelNameField);

            EditorThemeManager.AddLightText(difficultyLabelText);

            EditorThemeManager.AddLightText(formatLabelText);

            EditorThemeManager.AddLightText(songArtistLabelText);
            EditorThemeManager.AddInputField(songArtistInputField);

            EditorThemeManager.AddLightText(songTitleLabelText);
            EditorThemeManager.AddInputField(songTitleInputField);

            EditorThemeManager.AddLightText(songPathLabel);
            EditorThemeManager.AddInputField(path);

            EditorThemeManager.AddGraphic(browseLocalButton.image, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(browseLocalText, ThemeGroup.Function_2_Text);

            EditorThemeManager.AddGraphic(browseInternalButton.image, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(browseInternalText, ThemeGroup.Function_2_Text);

            EditorThemeManager.AddGraphic(chooseTemplateButton.image, ThemeGroup.Function_2_Normal, true);
            EditorThemeManager.AddGraphic(chooseTemplateText, ThemeGroup.Function_2_Text);

            Destroy(create.GetComponent<Animator>());
            create.transition = Selectable.Transition.ColorTint;
            EditorThemeManager.AddGraphic(create.image, ThemeGroup.Add, true);

            EditorThemeManager.AddGraphic(create.transform.Find("text").GetComponent<Text>(), ThemeGroup.Add_Text);

            EditorLevelManager.inst.NewLevelPopup.SongPath = path;

            LevelTemplateEditor.Init();
        }

        void CreatePreview()
        {
            var gameObject = Creator.NewUIObject("Preview Cover", EditorManager.inst.dialogs.parent, 1);

            var rectTransform = gameObject.transform.AsRT();
            var image = gameObject.AddComponent<Image>();

            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(10000f, 10000f);

            PreviewCover = new EditorThemeManager.Element(ThemeGroup.Preview_Cover, gameObject, new List<Component> { image, });
            EditorThemeManager.AddElement(PreviewCover);

            gameObject.SetActive(!Seasons.IsAprilFools);

            var preview = Creator.NewUIObject("Preview", EditorManager.inst.dialogs.parent, 1);
            preview.transform.AsRT().anchoredPosition = new Vector2(577.5f, 724.05f);
            preview.transform.AsRT().anchorMax = Vector2.zero;
            preview.transform.AsRT().anchorMin = Vector2.zero;

            var basePreviewObject = Creator.NewUIObject("Base", preview.transform);
            var size = EditorConfig.Instance.ObjectDraggerHelperSize.Value;
            var outlineSize = EditorConfig.Instance.ObjectDraggerHelperOutlineSize.Value;
            basePreviewObject.transform.AsRT().sizeDelta = new Vector2(size + outlineSize, size + outlineSize);
            var basePreviewImage = basePreviewObject.AddComponent<Image>();
            basePreviewImage.sprite = EditorSprites.CircleSprite;
            basePreviewImage.color = Color.black;

            var previewObject = Creator.NewUIObject("Object", basePreviewObject.transform);
            previewObject.transform.AsRT().sizeDelta = new Vector2(size, size);
            var previewObjectImage = previewObject.AddComponent<Image>();
            previewObjectImage.sprite = EditorSprites.CircleSprite;

            SelectObjectHelper = previewObject.AddComponent<SelectObjectHelper>();
            SelectObjectHelper.baseImage = basePreviewImage;
            SelectObjectHelper.image = previewObjectImage;
            previewObject.AddComponent<EventTrigger>().triggers = new List<EventTrigger.Entry>
            {
                TriggerHelper.CreateEntry(EventTriggerType.PointerDown, SelectObjectHelper.PointerDown),
                TriggerHelper.CreateEntry(EventTriggerType.Scroll, SelectObjectHelper.Scroll),
                TriggerHelper.CreateEntry(EventTriggerType.BeginDrag, SelectObjectHelper.BeginDrag),
                TriggerHelper.CreateEntry(EventTriggerType.Drag, SelectObjectHelper.Drag),
                TriggerHelper.CreateEntry(EventTriggerType.EndDrag, SelectObjectHelper.EndDrag),
            };
        }

        void CreateWarningPopup()
        {
            var warningPopup = SaveAsPopup.GameObject.Duplicate(popups, "Warning Popup");
            warningPopup.transform.AsRT().anchoredPosition = Vector2.zero;

            var main = warningPopup.transform.GetChild(0);

            var spacer1 = Creator.NewUIObject("spacerL", main);
            spacer1.AddComponent<LayoutElement>();
            var horiz = spacer1.AddComponent<HorizontalLayoutGroup>();
            horiz.spacing = 22f;

            spacer1.transform.AsRT().sizeDelta = new Vector2(292f, 40f);

            var submit1 = main.Find("submit");
            submit1.SetParent(spacer1.transform);

            var submit2 = Instantiate(submit1);
            var submit2TF = submit2.transform;

            submit2TF.SetParent(spacer1.transform);
            submit2TF.localScale = Vector3.one;

            submit1.name = "submit1";
            submit2.name = "submit2";

            var submit1Image = submit1.GetComponent<Image>();
            submit1Image.color = new Color(1f, 0.2137f, 0.2745f, 1f);
            var submit2Image = submit2.GetComponent<Image>();
            submit2Image.color = new Color(0.302f, 0.7137f, 0.6745f, 1f);

            var submit1Button = submit1.GetComponent<Button>();
            var submit2Button = submit2.GetComponent<Button>();

            submit1Button.onClick.ClearAll();

            submit2Button.onClick.ClearAll();

            Destroy(main.Find("level-name").gameObject);

            var sizeFitter = main.GetComponent<ContentSizeFitter>();
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            main.transform.AsRT().sizeDelta = new Vector2(400f, 160f);

            main.Find("Level Name").AsRT().sizeDelta = new Vector2(292f, 64f);

            var panel = main.Find("Panel");

            var close = panel.Find("x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(HideWarningPopup);

            var title = panel.Find("Text").GetComponent<Text>();
            title.text = "Warning!";

            EditorHelper.AddEditorPopup(EditorPopup.WARNING_POPUP, warningPopup);

            EditorThemeManager.AddGraphic(main.GetComponent<Image>(), ThemeGroup.Background_1, true);
            EditorThemeManager.AddGraphic(panel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

            EditorThemeManager.AddSelectable(close, ThemeGroup.Close, true);
            EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

            EditorThemeManager.AddLightText(title);

            EditorThemeManager.AddLightText(main.Find("Level Name").GetComponent<Text>());
            EditorThemeManager.AddGraphic(submit1Image, ThemeGroup.Warning_Confirm, true);
            EditorThemeManager.AddGraphic(submit1Button.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Add_Text);
            EditorThemeManager.AddGraphic(submit2Image, ThemeGroup.Warning_Cancel, true);
            EditorThemeManager.AddGraphic(submit2Image.transform.GetChild(0).GetComponent<Text>(), ThemeGroup.Add_Text);

            try
            {
                WarningPopup = new EditorPopup(EditorPopup.WARNING_POPUP);
                WarningPopup.Assign(warningPopup);
                editorPopups.Add(WarningPopup);
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        GameObject GenerateDebugButton(string name, string hint, Action action)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(DebuggerPopup.Content, "Function");
            debugs.Add(name);

            gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = name,
                hint = hint
            });

            var button = gameObject.GetComponent<Button>();
            button.onClick.ClearAll();
            button.onClick.AddListener(() => action?.Invoke());

            EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            var text = gameObject.transform.GetChild(0).GetComponent<Text>();
            text.text = name;
            EditorThemeManager.ApplyLightText(text);
            return gameObject;
        }

        void CreateDebug()
        {
            if (!ModCompatibility.UnityExplorerInstalled)
                return;

            DebuggerPopup = GeneratePopup(EditorPopup.DEBUGGER_POPUP, "Debugger (Only use this if you know what you're doing)", Vector2.zero, new Vector2(600f, 450f), _val =>
            {
                debugSearch = _val;
                RefreshDebugger();
            }, placeholderText: "Search for function...");

            var reload = EditorPrefabHolder.Instance.SpriteButton.Duplicate(DebuggerPopup.TopPanel, "reload");
            UIManager.SetRectTransform(reload.transform.AsRT(), new Vector2(-42f, 0f), Vector2.one, Vector2.one, Vector2.one, new Vector2(32f, 32f));

            reload.GetOrAddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip
            {
                desc = "Refresh the function list",
                hint = "Clicking this will reload the function list."
            });

            var reloadButton = reload.GetComponent<Button>();
            reloadButton.onClick.ClearAll();
            reloadButton.onClick.AddListener(ReloadFunctions);

            EditorThemeManager.AddSelectable(reloadButton, ThemeGroup.Function_2, false);

            reloadButton.image.sprite = EditorSprites.ReloadSprite;

            EditorHelper.AddEditorDropdown("Debugger", "", "View", SpriteHelper.LoadSprite(AssetPack.GetFile($"core/sprites/icons/debugger{FileFormat.PNG.Dot()}")), () =>
            {
                DebuggerPopup.Open();
                RefreshDebugger();
            });

            EditorHelper.AddEditorDropdown("Show Explorer", "", "View", EditorSprites.SearchSprite, ModCompatibility.ShowExplorer);

            GenerateDebugButton(
                "Inspect DataManager",
                "DataManager is a pretty important storage component of Project Arrhythmia. It contains the GameData, all the external Beatmap Themes, etc.",
                () => ModCompatibility.Inspect(DataManager.inst));

            GenerateDebugButton(
                "Inspect EditorManager",
                "EditorManager handles the main unmodded editor related things.",
                () => ModCompatibility.Inspect(EditorManager.inst));

            GenerateDebugButton(
                "Inspect RTEditor",
                "EditorManager handles the main modded editor related things.",
                () => ModCompatibility.Inspect(inst));

            GenerateDebugButton(
                "Inspect ObjEditor",
                "ObjEditor is the component that handles regular object editor stuff.",
                () => ModCompatibility.Inspect(ObjEditor.inst));

            GenerateDebugButton(
                "Inspect ObjectEditor",
                "ObjectEditor is the component that handles modded object editor stuff.",
                () => ModCompatibility.Inspect(ObjectEditor.inst));

            GenerateDebugButton(
                "Inspect ObjectManager",
                "ObjectManager is the component that handles regular object stuff.",
                () => ModCompatibility.Inspect(ObjectManager.inst));

            GenerateDebugButton(
                "Inspect GameManager",
                "GameManager normally handles all the level loading, however now it's handled by LevelManager.",
                () => ModCompatibility.Inspect(GameManager.inst));

            GenerateDebugButton(
                "Inspect CompanionManager",
                "CompanionManager handles everything to do with Example, your little companion.",
                () => ModCompatibility.Inspect(CompanionManager.inst));
            
            GenerateDebugButton(
                "Inspect Example",
                "Example...",
                () => ModCompatibility.Inspect(Example.Current));

            GenerateDebugButton(
                "Inspect Object Editor UI",
                "Take a closer look at the Object Editor UI since the parent tree for it is pretty deep.",
                () => ModCompatibility.Inspect(ObjEditor.inst.ObjectView));

            GenerateDebugButton(
                "Inspect LevelProcessor",
                "LevelProcessor is the main handler for updating object animation and spawning / despawning objects.",
                () => ModCompatibility.Inspect(RTLevel.Current));

            GenerateDebugButton(
                "Inspect Current GameData",
                "GameData stores all the main level data.",
                () => ModCompatibility.Inspect(GameData.Current));

            GenerateDebugButton(
                "Inspect Current MetaData",
                "MetaData stores all the extra level info.",
                () => ModCompatibility.Inspect(MetaData.Current));

            GenerateDebugButton(
                "Current Event Keyframe",
                "The current selected Event Keyframe. Based on the type and index number.",
                () => ModCompatibility.Inspect(RTEventEditor.inst.CurrentSelectedKeyframe));

            ReloadFunctions();
        }

        void ReloadFunctions()
        {
            var functions = RTFile.ApplicationDirectory + "beatmaps/functions";
            if (!RTFile.DirectoryExists(functions))
                return;

            customFunctions.ForEach(x => Destroy(x));
            customFunctions.Clear();
            debugs.RemoveAll(x => x.Contains("Custom Code Function"));

            var files = Directory.GetFiles(functions, FileFormat.CS.ToPattern());
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];

                customFunctions.Add(GenerateDebugButton(
                    $"Custom Code Function: {Path.GetFileName(file)}",
                    "A custom code file. Make sure you know what you're doing before using this.",
                    () =>
                    {
                        var hadError = false;
                        Exception exception = null;
                        RTCode.Evaluate(RTFile.ReadFromFile(file), x => { hadError = true; exception = x; } );

                        if (hadError)
                            EditorManager.inst.DisplayNotification($"Couldn't evaluate {Path.GetFileName(file)}. Please verify your code and try again. Exception: {exception}", 2f, EditorManager.NotificationType.Error);
                        else
                            EditorManager.inst.DisplayNotification($"Evaluated {Path.GetFileName(file)}!", 2f, EditorManager.NotificationType.Success);
                    }));
            }

            RefreshDebugger();
        }

        void CreateAutosavePopup() => AutosavePopup = GeneratePopup(EditorPopup.AUTOSAVE_POPUP, "Open / Backup an Autosave", new Vector2(572f, 0f), new Vector2(460f, 350f), placeholderText: "Search autosaves...");

        void SetupMiscEditorThemes()
        {
            CoreHelper.Log($"Setting Object Options Popup");
            // Object Options
            {
                var options = ObjectOptionsPopup.GameObject.transform;

                EditorThemeManager.AddGraphic(options.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(options.Find("arrow").GetComponent<Image>(), ThemeGroup.Background_1);

                //for (int i = 1; i < options.childCount - 1; i++)
                //{
                //    var child = options.GetChild(i);

                //    EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Function_3, true);
                //    EditorThemeManager.AddGraphic(child.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_3_Text);
                //}

                //for (int i = 0; i < options.Find("shapes").childCount; i++)
                //{
                //    var child = options.Find("shapes").GetChild(i);

                //    EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Function_3, true);
                //    EditorThemeManager.AddGraphic(child.GetChild(0).GetComponent<Image>(), ThemeGroup.Function_3_Text);
                //}
            }

            // BG Options
            {
                var options = BGObjectOptionsPopup.GameObject.transform;

                EditorThemeManager.AddGraphic(options.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(options.Find("arrow").GetComponent<Image>(), ThemeGroup.Background_1);

                for (int i = 1; i < options.childCount; i++)
                {
                    var child = options.GetChild(i);

                    EditorThemeManager.AddGraphic(child.GetComponent<Image>(), ThemeGroup.Function_3, true);
                    EditorThemeManager.AddGraphic(child.GetChild(0).GetComponent<Text>(), ThemeGroup.Function_3_Text);
                }
            }
        }

        void CreateScreenshotsView()
        {
            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "ScreenshotDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            editorDialogObject.AddComponent<ActiveState>().onStateChanged = enabled => CaptureArea.inst.SetActive(enabled);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor("00FF8C");
            dialogStorage.title.text = "- Screenshots -";

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog(EditorDialog.SCREENSHOTS, editorDialogObject);

            var page = EditorPrefabHolder.Instance.NumberInputField.Duplicate(editorDialogObject.transform.Find("spacer"));
            var pageStorage = page.GetComponent<InputFieldStorage>();
            screenshotPageField = pageStorage.inputField;

            pageStorage.inputField.SetTextWithoutNotify(screenshotPage.ToString());
            pageStorage.inputField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int p))
                {
                    screenshotPage = Mathf.Clamp(p, 0, screenshotCount / screenshotsPerPage);
                    RefreshScreenshots();
                }
            });
            pageStorage.leftGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = "0";
            });
            pageStorage.leftButton.onClick.NewListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p - 1, 0, screenshotCount / screenshotsPerPage).ToString();
            });
            pageStorage.rightButton.onClick.NewListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = Mathf.Clamp(p + 1, 0, screenshotCount / screenshotsPerPage).ToString();
            });
            pageStorage.rightGreaterButton.onClick.NewListener(() =>
            {
                if (int.TryParse(pageStorage.inputField.text, out int p))
                    pageStorage.inputField.text = (screenshotCount / screenshotsPerPage).ToString();
            });

            Destroy(pageStorage.middleButton.gameObject);

            EditorThemeManager.AddInputField(pageStorage.inputField);
            EditorThemeManager.AddSelectable(pageStorage.leftGreaterButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.leftButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.rightButton, ThemeGroup.Function_2, false);
            EditorThemeManager.AddSelectable(pageStorage.rightGreaterButton, ThemeGroup.Function_2, false);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            screenshotContent = scrollView.transform.Find("Viewport/Content");
            scrollView.transform.localScale = Vector3.one;

            LSHelpers.DeleteChildren(screenshotContent);

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            EditorHelper.AddEditorDropdown("View Screenshots", "", "View", EditorSprites.SearchSprite, () =>
            {
                ScreenshotsDialog.Open();
                RefreshScreenshots();
            });

            try
            {
                ScreenshotsDialog = new EditorDialog(EditorDialog.SCREENSHOTS);
                ScreenshotsDialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        void CreateFolderCreator()
        {
            try
            {
                var folderCreator = SaveAsPopup.GameObject.Duplicate(popups, "Folder Creator Popup");
                folderCreator.transform.localPosition = Vector3.zero;

                var folderCreatorPopup = folderCreator.transform.GetChild(0);

                var folderCreatorPopupPanel = folderCreatorPopup.Find("Panel");
                folderCreatorTitle = folderCreatorPopupPanel.Find("Text").GetComponent<Text>();
                folderCreatorTitle.text = "Folder Creator";

                var close = folderCreatorPopupPanel.Find("x").GetComponent<Button>();
                close.onClick.ClearAll();
                close.onClick.AddListener(HideNameEditor);

                folderCreatorNameLabel = folderCreatorPopup.Find("Level Name").GetComponent<Text>();
                folderCreatorNameLabel.text = "New folder name";

                folderCreatorName = folderCreatorPopup.Find("level-name").GetComponent<InputField>();
                folderCreatorName.onValueChanged.ClearAll();
                folderCreatorName.text = "New Folder";
                folderCreatorName.characterLimit = 0;

                folderCreatorSubmit = folderCreatorPopup.Find("submit").GetComponent<Button>();
                var submitImage = folderCreatorPopup.Find("submit").GetComponent<Image>();
                folderCreatorSubmitText = folderCreatorPopup.Find("submit/text").GetComponent<Text>();
                folderCreatorSubmitText.text = "Create Folder";

                EditorThemeManager.AddGraphic(folderCreatorPopup.GetComponent<Image>(), ThemeGroup.Background_1, true);
                EditorThemeManager.AddGraphic(folderCreatorPopupPanel.GetComponent<Image>(), ThemeGroup.Background_1, true, roundedSide: SpriteHelper.RoundedSide.Top);

                EditorThemeManager.AddSelectable(close, ThemeGroup.Close, true);
                EditorThemeManager.AddGraphic(close.transform.GetChild(0).GetComponent<Image>(), ThemeGroup.Close_X);

                EditorThemeManager.AddLightText(folderCreatorTitle);
                EditorThemeManager.AddLightText(folderCreatorNameLabel);
                EditorThemeManager.AddInputField(folderCreatorName);

                EditorThemeManager.AddGraphic(submitImage, ThemeGroup.Function_1, true);
                EditorThemeManager.AddGraphic(folderCreatorSubmitText, ThemeGroup.Function_1_Text);

                EditorHelper.AddEditorPopup(EditorPopup.FOLDER_CREATOR_POPUP, folderCreator);

                NamePopup = new EditorPopup(EditorPopup.FOLDER_CREATOR_POPUP);
                NamePopup.Assign(NamePopup.GetLegacyDialog().Dialog.gameObject);
                NamePopup.size = NamePopup.GameObject.transform.AsRT().sizeDelta;
                editorPopups.Add(NamePopup);

                if (NamePopup.Title)
                    NamePopup.title = NamePopup.Title.text;
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            }
        }

        void CreateFontSelector()
        {
            FontSelectorPopup = GeneratePopup(EditorPopup.FONT_SELECTOR_POPUP, "Select a Font", Vector2.zero, new Vector2(600f, 400f), placeholderText: "Search fonts...");

            fontSelectionPrefab = Creator.NewUIObject("element", transform);
            RectValues.Default.SizeDelta(0f, 32f).AssignToRectTransform(fontSelectionPrefab.transform.AsRT());

            var horizontalLayoutGroup = fontSelectionPrefab.AddComponent<HorizontalLayoutGroup>();

            horizontalLayoutGroup.childControlHeight = false;
            horizontalLayoutGroup.childControlWidth = false;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.spacing = 8f;
            fontSelectionPrefab.AddComponent<Image>();
            fontSelectionPrefab.AddComponent<Button>();

            var labels = Creator.NewUIObject("label", fontSelectionPrefab.transform);
            RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(labels.transform.AsRT());

            var labelsHLG = labels.AddComponent<HorizontalLayoutGroup>();

            labelsHLG.childControlHeight = false;
            labelsHLG.childControlWidth = false;
            labelsHLG.spacing = 8f;

            var label = Creator.NewUIObject("text", labels.transform);
            RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(label.transform.AsRT());

            var text = label.AddComponent<TextMeshProUGUI>();
            text.font = FontManager.inst.allFontAssets["Inconsolata Variable"];
            text.fontSize = 20;
            text.enableWordWrapping = true;
            text.text = "font";
        }

        void SetupEaseDropdowns()
        {
            EasingOptions.Clear();
            if (AssetPack.TryReadFromFile("editor/data/ease.json", out string easeFile))
            {
                var jn = JSON.Parse(easeFile);

                if (jn["layout"] != null)
                    EasingDropdownLayoutGroupValues = LayoutValues.Parse(jn["layout"]);
                if (jn["template_rect"] != null)
                    EasingDropdownTemplateRectValues = RectValues.Parse(jn["template_rect"]);

                for (int i = 0; i < jn["options"].Count; i++)
                {
                    var jnOption = jn["options"][i];
                    var easingOption = new EasingOption();
                    easingOption.name = jnOption["name"];
                    easingOption.index = jnOption["index"].AsInt;
                    easingOption.icon = SpriteHelper.LoadSprite(AssetPack.TryGetFile(jnOption["icon_ref"], out string iconPath) ? iconPath : "core/sprites/ease/Linear.png");
                    EasingOptions.Add(easingOption);
                }
            }

            EasingDropdowns.Clear();

            var easingDropdowns = (from x in Resources.FindObjectsOfTypeAll<Dropdown>()
                                   where x.gameObject && x.gameObject.name == "curves"
                                   select x).ToList();

            foreach (var dropdown in easingDropdowns)
            {
                SetupEaseDropdown(dropdown);
                TriggerHelper.AddEventTriggers(dropdown.gameObject, TriggerHelper.ScrollDelta(dropdown));
            }

            EasingDropdowns = easingDropdowns;
        }

        void CreateCameraArea()
        {
            var canvas = UIManager.GenerateUICanvas("Camera Area", null);
            canvas.SetWorldSpace(RTLevel.UI_LAYER, RTLevel.Cameras.FG);
            cameraAreaCanvas = canvas.GameObject.transform.AsRT();
            var gameObject = Creator.NewUIObject("Camera", cameraAreaCanvas);
            cameraArea = gameObject.transform.AsRT();
            var image = gameObject.AddComponent<Image>();
            image.type = Image.Type.Tiled;
            image.sprite = EditorManager.inst.SelectionBoxImage.sprite;

            new Labels(Labels.InitSettings.Default.Parent(gameObject.transform).Rect(RectValues.BottomLeftAnchored.Pivot(0f, 1f).SizeDelta(0f, 20f)), "Camera Area");

            editorCameraEnabled = false;
            gameObject.SetActive(false);
        }

        #endregion

        #endregion

        #region Render UI

        public List<EditorDialog> editorDialogs = new List<EditorDialog>();

        public EditorDialog ScreenshotsDialog { get; set; }

        #region Folder Creator / Name Editor

        /// <summary>
        /// Shows the Folder Creator Popup.
        /// </summary>
        /// <param name="path">Path to create a folder in.</param>
        /// <param name="onSubmit">Function to run when Submit is clicked.</param>
        public void ShowFolderCreator(string path, Action onSubmit)
        {
            NamePopup.Open();
            RefreshFolderCreator(path, onSubmit);
        }

        /// <summary>
        /// Renders the Folder Creator Popup.
        /// </summary>
        /// <param name="path">Path to create a folder in.</param>
        /// <param name="onSubmit">Function to run when Submit is clicked.</param>
        public void RefreshFolderCreator(string path, Action onSubmit) => RefreshNameEditor("Folder Creator", "New folder name", "Create Folder", () =>
        {
            var directory = RTFile.CombinePaths(path, RTFile.ValidateDirectory(folderCreatorName.text));

            if (RTFile.CreateDirectory(directory))
                onSubmit?.Invoke();
        });

        /// <summary>
        /// Hides the name editor.
        /// </summary>
        public void HideNameEditor() => NamePopup.Close();

        /// <summary>
        /// Shows the Name Editor Popup.
        /// </summary>
        /// <param name="title">Name of the popup to render.</param>
        /// <param name="nameLabel">Label of the name field.</param>
        /// <param name="submitText">Submit button text.</param>
        /// <param name="onSubmit">Function to run when submit is clicked.</param>
        public void ShowNameEditor(string title, string nameLabel, string submitText, Action onSubmit)
        {
            NamePopup.Open();
            RefreshNameEditor(title, nameLabel, submitText, onSubmit);
        }

        /// <summary>
        /// Renders the Name Editor Popup.
        /// </summary>
        /// <param name="title">Name of the popup to render.</param>
        /// <param name="nameLabel">Label of the name field.</param>
        /// <param name="submitText">Submit button text.</param>
        /// <param name="onSubmit">Function to run when submit is clicked.</param>
        public void RefreshNameEditor(string title, string nameLabel, string submitText, Action onSubmit)
        {
            folderCreatorTitle.text = title;
            folderCreatorNameLabel.text = nameLabel;
            folderCreatorSubmitText.text = submitText;

            folderCreatorSubmit.onClick.NewListener(() => onSubmit?.Invoke());
        }

        /// <summary>
        /// Shows the Name Editor Popup.
        /// </summary>
        /// <param name="title">Name of the popup to render.</param>
        /// <param name="nameLabel">Label of the name field.</param>
        /// <param name="defaultText">Default text to set.</param>
        /// <param name="submitText">Submit button text.</param>
        /// <param name="onSubmit">Function to run when submit is clicked.</param>
        public void ShowNameEditor(string title, string nameLabel, string defaultText, string submitText, Action onSubmit)
        {
            NamePopup.Open();
            RefreshNameEditor(title, nameLabel, defaultText, submitText, onSubmit);
        }

        /// <summary>
        /// Renders the Name Editor Popup.
        /// </summary>
        /// <param name="title">Name of the popup to render.</param>
        /// <param name="nameLabel">Label of the name field.</param>
        /// <param name="defaultText">Default text to set.</param>
        /// <param name="submitText">Submit button text.</param>
        /// <param name="onSubmit">Function to run when submit is clicked.</param>
        public void RefreshNameEditor(string title, string nameLabel, string defaultText, string submitText, Action onSubmit)
        {
            folderCreatorTitle.text = title;
            folderCreatorNameLabel.text = nameLabel;
            folderCreatorSubmitText.text = submitText;

            folderCreatorName.SetTextWithoutNotify(defaultText);
            folderCreatorSubmit.onClick.NewListener(() => onSubmit?.Invoke());
        }

        #endregion

        #region Warning Popup

        /// <summary>
        /// Hides the warning popup.
        /// </summary>
        public void HideWarningPopup() => WarningPopup.Close();

        /// <summary>
        /// Shows the warning popup.
        /// </summary>
        /// <param name="warning">The warning message.</param>
        /// <param name="onConfirm">Function to run when the user confirms.</param>
        /// <param name="onCancel">Function to run when the user cancels.</param>
        /// <param name="confirm">Confirm button text.</param>
        /// <param name="cancel">Cancel button text.</param>
        /// <param name="onClose">Function to run when the user closes the popup.</param>
        public void ShowWarningPopup(string warning, UnityAction onConfirm, UnityAction onCancel, string confirm = "Yes", string cancel = "No", Action onClose = null)
        {
            WarningPopup.Open();
            RefreshWarningPopup(warning, onConfirm, onCancel, confirm, cancel, onClose);

            Example.Current?.brain?.Notice(ExampleBrain.Notices.WARNING_POPUP);
        }

        /// <summary>
        /// Renders the warning popup.
        /// </summary>
        /// <param name="warning">The warning message.</param>
        /// <param name="onConfirm">Function to run when the user confirms.</param>
        /// <param name="onCancel">Function to run when the user cancels.</param>
        /// <param name="confirm">Confirm button text.</param>
        /// <param name="cancel">Cancel button text.</param>
        /// <param name="onClose">Function to run when the user closes the popup.</param>
        public void RefreshWarningPopup(string warning, UnityAction onConfirm, UnityAction onCancel, string confirm = "Yes", string cancel = "No", Action onClose = null)
        {
            var warningPopup = WarningPopup.GameObject.transform.GetChild(0);

            var close = warningPopup.Find("Panel/x").GetComponent<Button>();
            close.onClick.ClearAll();
            close.onClick.AddListener(() =>
            {
                if (onClose != null)
                {
                    onClose();
                    return;
                }

                onCancel?.Invoke();
            });

            warningPopup.Find("Level Name").GetComponent<Text>().text = warning;

            var submit1 = warningPopup.Find("spacerL/submit1");
            var submit2 = warningPopup.Find("spacerL/submit2");

            var submit1Button = submit1.GetComponent<Button>();
            var submit2Button = submit2.GetComponent<Button>();

            submit1.Find("text").GetComponent<Text>().text = confirm;
            submit2.Find("text").GetComponent<Text>().text = cancel;

            submit1Button.onClick.ClearAll();
            submit2Button.onClick.ClearAll();

            submit1Button.onClick.AddListener(onConfirm);
            submit2Button.onClick.AddListener(onCancel);
        }

        #endregion

        #region Preview

        public void UpdateGrid()
        {
            if (!previewGrid)
                return;

            if (!EditorConfig.Instance.PreviewGridEnabled.Value)
                return;

            var gridSize = EditorConfig.Instance.PreviewGridSize.Value;
            previewGrid.gridSize = new Vector2(gridSize, gridSize);
            previewGrid.color = EditorConfig.Instance.PreviewGridColor.Value;
            previewGrid.thickness = EditorConfig.Instance.PreviewGridThickness.Value;
            previewGrid.SetVerticesDirty();
        }

        public void UpdateTimeline()
        {
            if (!timelinePreview || !AudioManager.inst.CurrentAudioSource.clip || !GameData.Current || !GameData.Current.data)
                return;

            for (int i = 0; i < checkpointImages.Count; i++)
            {
                if (checkpointImages[i] && checkpointImages[i].gameObject)
                    Destroy(checkpointImages[i].gameObject);
            }

            checkpointImages.Clear();
            LSHelpers.DeleteChildren(timelinePreview.Find("elements"));
            foreach (var checkpoint in GameData.Current.data.checkpoints)
            {
                if (checkpoint.time <= 0.5f)
                    continue;

                var gameObject = GameManager.inst.checkpointPrefab.Duplicate(timelinePreview.Find("elements"), $"Checkpoint [{checkpoint.name}] - [{checkpoint.time}]");
                float num = checkpoint.time * 400f / AudioManager.inst.CurrentAudioSource.clip.length;
                gameObject.transform.AsRT().anchoredPosition = new Vector2(num, 0f);

                var image = gameObject.GetComponent<Image>();
                image.material = LegacyResources.canvasImageMask;
                checkpointImages.Add(image);
            }
        }

        #endregion

        public void RenderTags(IModifyable modifyable, ITagDialog dialog)
        {
            var tagsScrollView = dialog.TagsScrollView;
            tagsScrollView.parent.GetChild(tagsScrollView.GetSiblingIndex() - 1).gameObject.SetActive(ShowModdedUI);
            tagsScrollView.gameObject.SetActive(ShowModdedUI);

            LSHelpers.DeleteChildren(dialog.TagsContent);

            if (!ShowModdedUI)
                return;

            int num = 0;
            foreach (var tag in modifyable.Tags)
            {
                int index = num;
                var gameObject = EditorPrefabHolder.Instance.Tag.Duplicate(dialog.TagsContent, index.ToString());
                gameObject.transform.localScale = Vector3.one;
                var input = gameObject.transform.Find("Input").GetComponent<InputField>();
                input.SetTextWithoutNotify(tag);
                input.onValueChanged.NewListener(_val => modifyable.Tags[index] = _val);

                var deleteStorage = gameObject.transform.Find("Delete").GetComponent<DeleteButtonStorage>();
                deleteStorage.button.onClick.NewListener(() =>
                {
                    modifyable.Tags.RemoveAt(index);
                    RenderTags(modifyable, dialog);
                });

                EditorHelper.AddInputFieldContextMenu(input);
                TriggerHelper.InversableField(input, InputFieldSwapper.Type.String);

                EditorThemeManager.ApplyGraphic(gameObject.GetComponent<Image>(), ThemeGroup.Input_Field, true);

                EditorThemeManager.ApplyInputField(input);

                EditorThemeManager.ApplyGraphic(deleteStorage.baseImage, ThemeGroup.Delete, true);
                EditorThemeManager.ApplyGraphic(deleteStorage.image, ThemeGroup.Delete_Text);

                num++;
            }

            var add = EditorPrefabHolder.Instance.CreateAddButton(dialog.TagsContent);
            add.Text = "Add Tag";
            add.OnClick.NewListener(() =>
            {
                modifyable.Tags.Add("New Tag");
                RenderTags(modifyable, dialog);
            });
        }

        public void RenderParent(IParentable parentable, IParentDialog dialog)
        {
            string parent = parentable.Parent;

            dialog.ParentButton.transform.AsRT().sizeDelta = new Vector2(!string.IsNullOrEmpty(parent) ? 201f : 241f, 32f);

            dialog.ParentSearchButton.onClick.NewListener(() => ObjectEditor.inst.ShowParentSearch(EditorTimeline.inst.GetTimelineObject(parentable as IEditable)));
            var parentSearchContextMenu = dialog.ParentSearchButton.gameObject.GetOrAddComponent<ContextClickable>();
            parentSearchContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Open Parent Popup", () => ObjectEditor.inst.ShowParentSearch(EditorTimeline.inst.GetTimelineObject(parentable as IEditable))),
                    new ButtonFunction("Parent to Camera", () =>
                    {
                        parentable.Parent = BeatmapObject.CAMERA_PARENT;
                        parentable.UpdateParentChain();
                        RenderParent(parentable, dialog);
                    })
                    );
            };

            dialog.ParentPickerButton.onClick.NewListener(() => parentPickerEnabled = true);

            dialog.ParentClearButton.gameObject.SetActive(!string.IsNullOrEmpty(parent));

            dialog.ParentSettingsParent.transform.AsRT().sizeDelta = new Vector2(351f, ShowModdedUI ? 152f : 112f);

            var parentContextMenu = dialog.ParentButton.gameObject.GetOrAddComponent<ContextClickable>();
            parentContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right || parentable is not BeatmapObject beatmapObject)
                    return;

                var list = new List<ButtonFunction>();

                if (!string.IsNullOrEmpty(parentable.Parent))
                {
                    var parentChain = beatmapObject.GetParentChain();
                    if (parentChain.Count > 0)
                        list.Add(new ButtonFunction("View Parent Chain", () =>
                        {
                            ObjectEditor.inst.ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)), beatmapObjects: parentChain);
                        }));
                }

                if (GameData.Current.beatmapObjects.TryFindAll(x => x.Parent == beatmapObject.id, out List<BeatmapObject> findAll))
                {
                    var childTree = beatmapObject.GetChildTree();
                    if (childTree.Count > 0)
                        list.Add(new ButtonFunction("View Child Tree", () =>
                        {
                            ObjectEditor.inst.ShowObjectSearch(x => EditorTimeline.inst.SetCurrentObject(EditorTimeline.inst.GetTimelineObject(x), Input.GetKey(KeyCode.LeftControl)), beatmapObjects: childTree);
                        }));
                }

                EditorContextMenu.inst.ShowContextMenu(list);
            };

            if (string.IsNullOrEmpty(parent))
            {
                dialog.ParentButton.button.interactable = false;
                dialog.ParentMoreButton.interactable = false;
                dialog.ParentSettingsParent.gameObject.SetActive(false);
                dialog.ParentButton.label.text = "No Parent Object";

                dialog.ParentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                dialog.ParentButton.button.onClick.ClearAll();
                dialog.ParentMoreButton.onClick.ClearAll();
                dialog.ParentClearButton.onClick.ClearAll();

                return;
            }

            string p = null;

            if (GameData.Current.beatmapObjects.TryFindIndex(x => x.id == parent, out int pa))
            {
                p = GameData.Current.beatmapObjects[pa].name;
                if (parentable is BeatmapObject beatmapObject)
                    dialog.ParentInfo.tooltipLangauges[0].hint = string.Format("Parent chain count: [{0}]\n(Inclusive)", beatmapObject.GetParentChain().Count);
            }
            else if (parent == BeatmapObject.CAMERA_PARENT)
            {
                p = "[CAMERA]";
                dialog.ParentInfo.tooltipLangauges[0].hint = "Object parented to the camera.";
            }

            dialog.ParentButton.button.interactable = p != null;
            dialog.ParentMoreButton.interactable = p != null;

            dialog.ParentSettingsParent.gameObject.SetActive(p != null && ObjEditor.inst.advancedParent);

            dialog.ParentClearButton.onClick.NewListener(() =>
            {
                if (parentable.CustomParent != null)
                {
                    parentable.CustomParent = null;
                    EditorManager.inst.DisplayNotification("Removed custom parent!", 1.5f, EditorManager.NotificationType.Success);
                }
                else
                    parentable.Parent = string.Empty;
                parentable.UpdateParentChain();
                RenderParent(parentable, dialog);
            });

            if (p == null)
            {
                dialog.ParentButton.label.text = "No Parent Object";
                dialog.ParentInfo.tooltipLangauges[0].hint = string.IsNullOrEmpty(parent) ? "Object not parented." : "No parent found.";
                dialog.ParentButton.button.onClick.ClearAll();
                dialog.ParentMoreButton.onClick.ClearAll();

                return;
            }

            dialog.ParentButton.label.text = p;

            dialog.ParentButton.button.onClick.NewListener(() =>
            {
                if (GameData.Current.beatmapObjects.Find(x => x.id == parent) != null &&
                    parent != BeatmapObject.CAMERA_PARENT &&
                    EditorTimeline.inst.timelineObjects.TryFind(x => x.ID == parent, out TimelineObject timelineObject))

                    EditorTimeline.inst.SetCurrentObject(timelineObject);
                else if (parent == BeatmapObject.CAMERA_PARENT)
                {
                    EditorTimeline.inst.SetLayer(EditorTimeline.LayerType.Events);
                    EventEditor.inst.SetCurrentEvent(0, GameData.Current.ClosestEventKeyframe(0));
                }
            });

            dialog.ParentMoreButton.onClick.NewListener(() =>
            {
                ObjEditor.inst.advancedParent = !ObjEditor.inst.advancedParent;
                dialog.ParentSettingsParent.gameObject.SetActive(ObjEditor.inst.advancedParent);
            });
            dialog.ParentSettingsParent.gameObject.SetActive(ObjEditor.inst.advancedParent);

            dialog.ParentDesyncToggle.gameObject.SetActive(ShowModdedUI);
            if (ShowModdedUI)
            {
                dialog.ParentDesyncToggle.SetIsOnWithoutNotify(parentable.ParentDesync);
                dialog.ParentDesyncToggle.onValueChanged.NewListener(_val =>
                {
                    parentable.ParentDesync = _val;
                    parentable.UpdateParentChain();
                });
            }

            for (int i = 0; i < dialog.ParentSettings.Count; i++)
            {
                var parentSetting = dialog.ParentSettings[i];

                var index = i;

                // Parent Type
                parentSetting.activeToggle.SetIsOnWithoutNotify(parentable.GetParentType(i));
                parentSetting.activeToggle.onValueChanged.NewListener(_val =>
                {
                    parentable.SetParentType(index, _val);
                    parentable.UpdateParentChain();
                });

                // Parent Offset
                var lel = parentSetting.offsetField.GetComponent<LayoutElement>();
                lel.minWidth = ShowModdedUI ? 64f : 128f;
                lel.preferredWidth = ShowModdedUI ? 64f : 128f;
                parentSetting.offsetField.SetTextWithoutNotify(parentable.GetParentOffset(i).ToString());
                parentSetting.offsetField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        parentable.SetParentOffset(index, num);
                        parentable.UpdateParentChain();
                    }
                });

                TriggerHelper.AddEventTriggers(parentSetting.offsetField.gameObject, TriggerHelper.ScrollDelta(parentSetting.offsetField));

                parentSetting.additiveToggle.onValueChanged.ClearAll();
                parentSetting.parallaxField.onValueChanged.ClearAll();
                parentSetting.additiveToggle.gameObject.SetActive(ShowModdedUI);
                parentSetting.parallaxField.gameObject.SetActive(ShowModdedUI);

                if (!ShowModdedUI)
                    continue;

                parentSetting.additiveToggle.SetIsOnWithoutNotify(parentable.GetParentAdditive(i));
                parentSetting.additiveToggle.onValueChanged.AddListener(_val =>
                {
                    parentable.SetParentAdditive(index, _val);
                    parentable.UpdateParentChain();
                });
                parentSetting.parallaxField.SetTextWithoutNotify(parentable.ParentParallax[index].ToString());
                parentSetting.parallaxField.onValueChanged.NewListener(_val =>
                {
                    if (float.TryParse(_val, out float num))
                    {
                        parentable.ParentParallax[index] = num;
                        parentable.UpdateParentChain();
                    }
                });

                TriggerHelper.AddEventTriggers(parentSetting.parallaxField.gameObject, TriggerHelper.ScrollDelta(parentSetting.parallaxField));
            }
        }

        public void RenderEditorLayer(IEditable editable, IEditorLayerUI editorLayerUI)
        {
            RenderEditorLayer(
                editorLayerUI: editorLayerUI,
                getLayer: () => editable.EditorData.Layer,
                setLayer: _val =>
                {
                    editable.EditorData.Layer = _val;
                    EditorTimeline.inst.RenderTimelineObject(EditorTimeline.inst.GetTimelineObject(editable));
                    RenderEditorLayer(editable, editorLayerUI);
                });
            var editorLayerContextMenu = editorLayerUI.EditorLayerField.gameObject.GetOrAddComponent<ContextClickable>();
            editorLayerContextMenu.onClick = eventData =>
            {
                if (eventData.button != PointerEventData.InputButton.Right)
                    return;

                EditorContextMenu.inst.ShowContextMenu(
                    new ButtonFunction("Go to Editor Layer", () => EditorTimeline.inst.SetLayer(editable.EditorData.Layer, EditorTimeline.LayerType.Objects))
                    );
            };
        }

        public void RenderEditorLayer(IEditorLayerUI editorLayerUI, Func<int> getLayer, Action<int> setLayer)
        {
            var layer = getLayer?.Invoke() ?? 0;
            editorLayerUI.EditorLayerField.image.color = EditorTimeline.GetLayerColor(layer);
            editorLayerUI.EditorLayerField.SetTextWithoutNotify((layer + 1).ToString());
            editorLayerUI.EditorLayerField.onValueChanged.NewListener(_val =>
            {
                if (int.TryParse(_val, out int num))
                {
                    num = Mathf.Clamp(num - 1, 0, int.MaxValue);
                    setLayer?.Invoke(num);
                }
            });

            if (editorLayerUI.EditorLayerField.gameObject)
                TriggerHelper.AddEventTriggers(editorLayerUI.EditorLayerField.gameObject, TriggerHelper.ScrollDeltaInt(editorLayerUI.EditorLayerField, 1, 1, int.MaxValue));

            if (editorLayerUI.EditorLayerToggles == null)
                return;

            for (int i = 0; i < editorLayerUI.EditorLayerToggles.Length; i++)
            {
                var index = EditorLayers.TryGetAt(i, out EditorLayer editorLayer) ? editorLayer.layer : 0;
                var toggle = editorLayerUI.EditorLayerToggles[i];
                toggle.SetIsOnWithoutNotify(index == layer);
                toggle.onValueChanged.NewListener(_val => setLayer?.Invoke(index));

                TriggerHelper.AddEventTriggers(toggle.gameObject,
                    TriggerHelper.CreateEntry(EventTriggerType.Scroll, eventData =>
                    {
                        var layer = getLayer?.Invoke() ?? 0;
                        var pointerEventData = (PointerEventData)eventData;
                        if (pointerEventData.scrollDelta.y > 0f)
                            setLayer?.Invoke((int)RTMath.Clamp((long)layer + 1, 0, int.MaxValue));
                        if (pointerEventData.scrollDelta.y < 0f)
                            setLayer?.Invoke(RTMath.Clamp(layer - 1, 0, int.MaxValue));
                    }));
            }
        }

        public void ShowFontSelector(Action<string> onFontSelected)
        {
            FontSelectorPopup.Open();
            RefreshFontSelector(onFontSelected);
        }

        public void RefreshFontSelector(Action<string> onFontSelected)
        {
            FontSelectorPopup.SearchField.onValueChanged.NewListener(_val => RefreshFontSelector(onFontSelected));
            FontSelectorPopup.ClearContent();

            foreach (var font in FontManager.inst.allFonts)
            {
                if (!RTString.SearchString(FontSelectorPopup.SearchTerm, font.Key))
                    continue;

                var gameObject = fontSelectionPrefab.Duplicate(FontSelectorPopup.Content, font.Key);
                RectValues.Default.SizeDelta(0f, 32f).AssignToRectTransform(gameObject.transform.AsRT());

                var image = gameObject.GetComponent<Image>();

                var text = gameObject.transform.Find("label/text").GetComponent<TextMeshProUGUI>();
                text.text = $"<font={font.Key}>ABCDEF abcdef 123</font> - {font.Key}";

                var button = gameObject.GetComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() =>
                {
                    onFontSelected?.Invoke($"<font={font.Key}>");
                    FontSelectorPopup.Close();
                });

                RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(gameObject.transform.Find("label").AsRT());
                RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(gameObject.transform.Find("label/text").AsRT());

                EditorThemeManager.ApplyGraphic(text, ThemeGroup.Light_Text);
                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
            }
        }

        /// <summary>
        /// Refreshes the debugger.
        /// </summary>
        public void RefreshDebugger()
        {
            for (int i = 0; i < debugs.Count; i++)
                DebuggerPopup.Content.GetChild(i).gameObject.SetActive(RTString.SearchString(debugSearch, debugs[i]));
        }

        /// <summary>
        /// Refreshes the screenshots in the screenshots folder.
        /// </summary>
        public void RefreshScreenshots()
        {
            var directory = RTFile.ApplicationDirectory + CoreConfig.Instance.ScreenshotsPath.Value;

            LSHelpers.DeleteChildren(screenshotContent);
            var files = Directory.GetFiles(directory, FileFormat.PNG.ToPattern(), SearchOption.TopDirectoryOnly);
            screenshotCount = files.Length;

            if (screenshotCount > screenshotsPerPage)
                TriggerHelper.AddEventTriggers(screenshotPageField.gameObject, TriggerHelper.ScrollDeltaInt(screenshotPageField, max: screenshotCount / screenshotsPerPage));
            else
                TriggerHelper.AddEventTriggers(screenshotPageField.gameObject);

            for (int i = 0; i < files.Length; i++)
            {
                if (!(i >= MinScreenshots && i < MaxScreenshots))
                    continue;

                var index = i;

                var gameObject = Creator.NewUIObject("screenshot", screenshotContent);
                gameObject.transform.localScale = Vector3.one;
                gameObject.transform.AsRT().sizeDelta = new Vector2(720f, 405f);

                var image = gameObject.AddComponent<Image>();
                image.enabled = false;

                var button = gameObject.AddComponent<Button>();
                button.onClick.ClearAll();
                button.onClick.AddListener(() => { System.Diagnostics.Process.Start(files[index]); });
                button.colors = UIManager.SetColorBlock(button.colors, Color.white, new Color(0.9f, 0.9f, 0.9f), new Color(0.7f, 0.7f, 0.7f), Color.white, Color.red);

                StartCoroutine(AlephNetwork.DownloadImageTexture($"file://{files[i]}", texture2D =>
                {
                    if (!image)
                        return;

                    image.enabled = true;
                    image.sprite = SpriteHelper.CreateSprite(texture2D);
                }));
            }
        }

        /// <summary>
        /// Renders the undo / redo edit buttons.
        /// </summary>
        public void RenderEditButtons()
        {
            var history = EditorManager.inst.history;
            string undoName = ((history.commands.Count > 1 && history.lastExecuted > 0) ? history.commands[history.lastExecuted].CommandName : null);
            undoButton.button.interactable = !string.IsNullOrEmpty(undoName);
            undoButton.label.text = string.IsNullOrEmpty(undoName) ? "Undo" : "Undo " + LSText.ClampString(undoName, 14);

            string redoName = ((history.commands.Count - 1 > history.lastExecuted) ? history.commands[history.lastExecuted + 1].CommandName : null);
            redoButton.button.interactable = !string.IsNullOrEmpty(redoName);
            redoButton.label.text = string.IsNullOrEmpty(redoName) ? "Redo" : "Redo " + LSText.ClampString(redoName, 14);
        }

        /// <summary>
        /// Plays an editor dialogs' animation.
        /// </summary>
        /// <param name="gameObject">Game object of the editor dialog.</param>
        /// <param name="dialogName">Name of the editor dialog.</param>
        /// <param name="active">Active state to apply to the editor dialog.</param>
        public void PlayDialogAnimation(GameObject gameObject, string dialogName, bool active)
        {
            var play = EditorConfig.Instance.PlayEditorAnimations.Value;

            EditorAnimation dialogAnimation = null;
            var hasAnimation = EditorConfig.Instance.editorAnimations.TryFind(x => x.name == dialogName, out dialogAnimation);

            if (play && hasAnimation && gameObject.activeSelf != active)
            {
                if (!dialogAnimation.Active)
                {
                    gameObject.SetActive(active);

                    return;
                }

                var dialog = gameObject.transform;

                var scrollbar = dialog.GetComponentsInChildren<Scrollbar>().ToList();
                var scrollAmounts = scrollbar.Select(x => x.value).ToList();

                var animation = new RTAnimation("Popup Open");
                animation.animationHandlers = new List<AnimationHandlerBase>
                {
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.PosStart.x : dialogAnimation.PosEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.PosXStartDuration : dialogAnimation.PosXEndDuration, active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, active ? Ease.GetEaseFunction(dialogAnimation.PosXStartEase) : Ease.GetEaseFunction(dialogAnimation.PosXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.PosXStartDuration : dialogAnimation.PosXEndDuration + 0.01f, active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.x = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.PosStart.y : dialogAnimation.PosEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.PosYStartDuration : dialogAnimation.PosYEndDuration, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, active ? Ease.GetEaseFunction(dialogAnimation.PosYStartEase) : Ease.GetEaseFunction(dialogAnimation.PosYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.PosYStartDuration : dialogAnimation.PosYEndDuration + 0.01f, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.PosActive)
                        {
                            var pos = dialog.localPosition;
                            pos.y = x;
                            dialog.localPosition = pos;
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.ScaStart.x : dialogAnimation.ScaEnd.x, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.ScaXStartDuration : dialogAnimation.ScaXEndDuration, active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, active ? Ease.GetEaseFunction(dialogAnimation.ScaXStartEase) : Ease.GetEaseFunction(dialogAnimation.ScaXEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.ScaXStartDuration : dialogAnimation.ScaXEndDuration + 0.01f, active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.x = x;
                            dialog.localScale = pos;

                            for (int i = 0; i < scrollbar.Count; i++)
                                scrollbar[i].value = scrollAmounts[i];
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.ScaStart.y : dialogAnimation.ScaEnd.y, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.ScaYStartDuration : dialogAnimation.ScaYEndDuration, active ? dialogAnimation.ScaEnd.y : dialogAnimation.ScaStart.y, active ? Ease.GetEaseFunction(dialogAnimation.ScaYStartEase) : Ease.GetEaseFunction(dialogAnimation.ScaYEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.ScaYStartDuration : dialogAnimation.ScaYEndDuration + 0.01f, active ? dialogAnimation.ScaEnd.y : dialogAnimation.ScaStart.y, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.ScaActive)
                        {
                            var pos = dialog.localScale;
                            pos.y = x;
                            dialog.localScale = pos;

                            for (int i = 0; i < scrollbar.Count; i++)
                                scrollbar[i].value = scrollAmounts[i];
                        }
                    }),
                    new AnimationHandler<float>(new List<IKeyframe<float>>
                    {
                        new FloatKeyframe(0f, active ? dialogAnimation.RotStart : dialogAnimation.RotEnd, Ease.Linear),
                        new FloatKeyframe(active ? dialogAnimation.RotStartDuration : dialogAnimation.RotEndDuration, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart, active ? Ease.GetEaseFunction(dialogAnimation.RotStartEase) : Ease.GetEaseFunction(dialogAnimation.RotEndEase)),
                        new FloatKeyframe(active ? dialogAnimation.RotStartDuration : dialogAnimation.RotEndDuration + 0.01f, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart, Ease.Linear),
                    }, x =>
                    {
                        if (dialogAnimation.RotActive)
                            dialog.localRotation = Quaternion.Euler(0f, 0f, x);
                    }),
                };
                animation.id = LSText.randomNumString(16);

                animation.onComplete = () =>
                {
                    dialog.gameObject.SetActive(active);

                    if (dialogAnimation.PosActive)
                        dialog.localPosition = new Vector3(active ? dialogAnimation.PosEnd.x : dialogAnimation.PosStart.x, active ? dialogAnimation.PosEnd.y : dialogAnimation.PosStart.y, 0f);
                    if (dialogAnimation.ScaActive)
                        dialog.localScale = new Vector3(active ? dialogAnimation.ScaEnd.x : dialogAnimation.ScaStart.x, dialogAnimation.ScaEnd.y, 1f);
                    if (dialogAnimation.RotActive)
                        dialog.localRotation = Quaternion.Euler(0f, 0f, active ? dialogAnimation.RotEnd : dialogAnimation.RotStart);

                    AnimationManager.inst.Remove(animation.id);
                };

                AnimationManager.inst.Play(animation);
            }

            if (!play || !hasAnimation || active)
                gameObject.SetActive(active);
        }

        public void ShowDialog(string name) => EditorManager.inst.ShowDialog(name);

        public void HideDialog(string name) => EditorManager.inst.HideDialog(name);

        /// <summary>
        /// Sets a dialogs' status.
        /// </summary>
        /// <param name="dialogName">The dialog to set the status of.</param>
        /// <param name="active">The active state.</param>
        /// <param name="focus">If the dialog should set as the current dialog.</param>
        public void SetDialogStatus(string dialogName, bool active, bool focus = true)
        {
            if (editorDialogs.TryFind(x => x.Name == dialogName, out EditorDialog dialog))
            {
                if (focus && active)
                {
                    EditorDialog.CurrentDialog?.Close();
                    EditorDialog.CurrentDialog = dialog;
                }

                dialog.SetActive(active);
                return;
            }

            if (!EditorManager.inst.EditorDialogsDictionary.TryGetValue(dialogName, out EditorManager.EditorDialog editorDialog))
            {
                Debug.LogError($"{EditorManager.inst.className}Can't load dialog [{dialogName}].");
                return;
            }

            PlayDialogAnimation(editorDialog.Dialog.gameObject, dialogName, active);

            if (active)
            {
                if (focus)
                    EditorManager.inst.currentDialog = editorDialog;
                if (!EditorManager.inst.ActiveDialogs.Contains(editorDialog))
                    EditorManager.inst.ActiveDialogs.Add(editorDialog);
            }
            else
            {
                EditorManager.inst.ActiveDialogs.Remove(editorDialog);
                if (EditorManager.inst.currentDialog == editorDialog && focus)
                    EditorManager.inst.currentDialog = EditorManager.inst.ActiveDialogs.Count > 0 ? EditorManager.inst.ActiveDialogs.Last() : new EditorManager.EditorDialog();
            }
        }

        #endregion

        #region Misc Functions

        /// <summary>
        /// Resets the editor undo / redo history.
        /// </summary>
        public void ResetHistory()
        {
            EditorManager.inst.history.Clear();
            EditorManager.inst.history.Add(new History.Command("Base", () => { }, () => { }));
        }

        /// <summary>
        /// Toggles the editor preview.
        /// </summary>
        public void TogglePreview()
        {
            if (!EditorManager.inst.hasLoadedLevel)
            {
                EditorManager.inst.DisplayNotification("Can't preview level until a level has been loaded!", 2f, EditorManager.NotificationType.Error);
                return;
            }

            EditorManager.inst.isEditing = !EditorManager.inst.isEditing;

            if (EditorManager.inst.isEditing)
            {
                ExitPreview();
                EditorManager.inst.UpdatePlayButton();
            }
            else
                EnterPreview();

            RTBeatmap.Current.ResetCheckpoint();

            Example.Current?.brain?.Notice(ExampleBrain.Notices.EDITOR_PREVIEW_TOGGLE);
            Example.Current?.model?.UpdateActive();
        }

        /// <summary>
        /// Starts the editor preview.
        /// </summary>
        public void EnterPreview()
        {
            if (!EditorManager.inst.hasLoadedLevel)
                return;

            RTBeatmap.Current?.Reset(EditorConfig.Instance.ApplyGameSettingsInPreviewMode.Value);

            if (RTBeatmap.Current && !EditorConfig.Instance.ApplyGameSettingsInPreviewMode.Value)
            {
                RTBeatmap.Current.challengeMode = ChallengeMode.Normal;
                RTBeatmap.Current.gameSpeed = GameSpeed.X1_0;
            }

            UpdatePlayers();

            GameManager.inst.playerGUI.SetActive(true);
            CursorManager.inst.HideCursor();
            EditorManager.inst.GUI.SetActive(false);
            if (!AudioManager.inst.CurrentAudioSource.isPlaying)
                AudioManager.inst.CurrentAudioSource.Play();
            EditorManager.inst.SetNormalRenderArea();
            GameManager.inst.UpdateTimeline();
            RTBeatmap.Current.ResetCheckpoint(true);

            EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Ends the editor preview.
        /// </summary>
        public void ExitPreview()
        {
            GameManager.inst.playerGUI.SetActive(false);
            CursorManager.inst.ShowCursor();
            EditorManager.inst.GUI.SetActive(true);
            EditorManager.inst.ShowGUI();
            EditorManager.inst.SetPlayersInvinsible(true);
            EditorManager.inst.SetEditRenderArea();
            GameManager.inst.UpdateTimeline();

            if (!EditorConfig.Instance.ResetHealthInEditor.Value || PlayerManager.Players.IsEmpty())
                return;

            if (!EditorManager.inst.hasLoadedLevel)
                return;

            try
            {
                UpdatePlayers(false);
                if (RTBeatmap.Current.ActiveCheckpoint)
                    PlayerManager.SpawnPlayers(RTBeatmap.Current.ActiveCheckpoint);
                else
                    PlayerManager.SpawnPlayers(EventManager.inst.cam.transform.position);
            }
            catch (Exception ex)
            {
                CoreHelper.LogError($"Resetting player health error.\n{ex}");
            }

            RTBeatmap.Current?.Reset();
        }

        /// <summary>
        /// Updates the players health and other settings when switching previews.
        /// </summary>
        /// <param name="considerChallengeMode">If challenge mode should be accounted for.</param>
        public void UpdatePlayers(bool considerChallengeMode = true)
        {
            if (PlayerManager.NoPlayers)
                return;

            foreach (var player in PlayerManager.Players)
                player.Health = considerChallengeMode && RTBeatmap.Current && RTBeatmap.Current.challengeMode.DefaultHealth > 0 ? RTBeatmap.Current.challengeMode.DefaultHealth : player.GetControl()?.Health ?? 3;
        }

        /// <summary>
        /// Sets the playing state of the current audio source.
        /// </summary>
        /// <param name="playing">If the audio should play.</param>
        public void SetPlaying(bool playing)
        {
            if (SoundManager.inst.Playing == playing)
                return;

            if (Seasons.IsAprilFools || EditorManager.inst.hasLoadedLevel)
                SoundManager.inst.SetPlaying(playing);
            else
                AudioManager.inst.CurrentAudioSource.Pause();

            EditorManager.inst.UpdatePlayButton();
        }

        public void OpenLevelListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(BeatmapsPath, EditorPath));

        public void OpenThemeListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(BeatmapsPath, ThemePath));

        public void OpenPrefabListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(BeatmapsPath, PrefabPath));

        public void OpenLevelCollectionListFolder() => RTFile.OpenInFileBrowser.Open(RTFile.CombinePaths(BeatmapsPath, CollectionsPath));

        public static void SetNotificationProperties()
        {
            CoreHelper.Log($"Setting Notification values");
            var notifyRT = EditorManager.inst.notification.transform.AsRT();
            var notifyGroup = EditorManager.inst.notification.GetComponent<VerticalLayoutGroup>();
            notifyRT.sizeDelta = new Vector2(EditorConfig.Instance.NotificationWidth.Value, 632f);
            EditorManager.inst.notification.transform.localScale =
                new Vector3(EditorConfig.Instance.NotificationSize.Value, EditorConfig.Instance.NotificationSize.Value, 1f);

            var direction = EditorConfig.Instance.NotificationDirection.Value;

            notifyRT.anchoredPosition = new Vector2(8f, direction == VerticalDirection.Up ? 408f : 410f);
            notifyGroup.childAlignment = direction != VerticalDirection.Up ? TextAnchor.LowerLeft : TextAnchor.UpperLeft;
        }

        public static Color GetObjectColor(BeatmapObject beatmapObject, bool ignoreTransparency)
        {
            var levelObject = beatmapObject.runtimeObject;
            if (beatmapObject.objectType == BeatmapObject.ObjectType.Empty || !levelObject || !levelObject.visualObject || !levelObject.visualObject.renderer)
                return Color.white;

            var color = AudioManager.inst.CurrentAudioSource.time < beatmapObject.StartTime ? CoreHelper.CurrentBeatmapTheme.GetObjColor((int)beatmapObject.events[3][0].values[0])
                : AudioManager.inst.CurrentAudioSource.time > beatmapObject.StartTime + beatmapObject.GetObjectLifeLength() && beatmapObject.autoKillType != AutoKillType.NoAutokill
                ? CoreHelper.CurrentBeatmapTheme.GetObjColor((int)beatmapObject.events[3][beatmapObject.events[3].Count - 1].values[0])
                : levelObject.visualObject.renderer.material.HasProperty("_Color") ? levelObject.visualObject.renderer.material.color : Color.white;

            if (ignoreTransparency)
                color.a = 1f;

            return color;
        }

        public static float SnapToBPM(float time)
        {
            var signature = RTSettingEditor.inst.BPMMulti / inst.editorInfo.timeSignature;
            return (Mathf.RoundToInt((time - inst.editorInfo.bpmOffset) / signature) * signature) + inst.editorInfo.bpmOffset;
        }

        public static float SnapToBPM(float time, float offset, float divisions, float bpm) => Mathf.RoundToInt((time + offset) / (60f / bpm / divisions)) * (60f / bpm / divisions) - offset;

        public static void SetActive(GameObject gameObject, bool active)
        {
            gameObject.SetActive(active);
            gameObject.transform.parent.GetChild(gameObject.transform.GetSiblingIndex() - 1).gameObject.SetActive(active);
        }

        /// <summary>
        /// Removes a package of objects from the level.
        /// </summary>
        /// <param name="beatmap">Package to remove.</param>
        public void RemoveBeatmap(IBeatmap beatmap)
        {
            var beatmapObjects = beatmap.BeatmapObjects;
            if (beatmapObjects != null)
                for (int i = 0; i < beatmapObjects.Count; i++)
                {
                    var beatmapObject = beatmapObjects[i];
                    RTLevel.Current.UpdateObject(beatmapObject, reinsert: false);
                    EditorTimeline.inst.RemoveTimelineObject(beatmapObject.timelineObject);
                    GameData.Current.beatmapObjects.Remove(x => x.id == beatmapObject.id);
                }

            var backgroundObjects = beatmap.BackgroundObjects;
            if (backgroundObjects != null)
                for (int i = 0; i < backgroundObjects.Count; i++)
                {
                    var backgroundObject = backgroundObjects[i];
                    RTLevel.Current.UpdateBackgroundObject(backgroundObject, false);
                    EditorTimeline.inst.RemoveTimelineObject(backgroundObject.timelineObject);
                    GameData.Current.backgroundObjects.Remove(x => x.id == backgroundObject.id);
                }

            var backgroundLayers = beatmap.BackgroundLayers;
            if (backgroundLayers != null)
                for (int i = 0; i < backgroundLayers.Count; i++)
                {
                    var backgroundLayer = backgroundLayers[i];
                    GameData.Current.backgroundLayers.Remove(x => x.id == backgroundLayer.id);
                }
            RTLevel.Current.UpdateBackgroundLayers();

            var prefabObjects = beatmap.PrefabObjects;
            if (prefabObjects != null)
                for (int i = 0; i < prefabObjects.Count; i++)
                {
                    var prefabObject = prefabObjects[i];
                    RTLevel.Current.UpdatePrefab(prefabObject, false);
                    EditorTimeline.inst.RemoveTimelineObject(prefabObject.timelineObject);
                    GameData.Current.prefabObjects.Remove(x => x.id == prefabObject.id);
                }

            var prefabs = beatmap.Prefabs;
            if (prefabs != null)
                for (int i = 0; i < prefabs.Count; i++)
                {
                    var prefab = prefabs[i];
                    GameData.Current.prefabs.Remove(x => x.id == prefab.id);
                }
        }

        public int GetEaseIndex(string name)
        {
            var index = EasingOptions.FindIndex(x => x.name == name);
            return index < 0 ? 0 : index;
        }

        public string GetEaseName(int index) => EasingOptions.TryFind(x => x.index == index, out EasingOption easingOption) ? easingOption.name : "Linear";

        public Easing GetEasing(int index) => EasingOptions.TryGetAt(index, out EasingOption easingOption) ? (Easing)easingOption.index : Easing.Linear;

        public int GetEditorLayer(int index) => EditorLayers.TryGetAt(index, out EditorLayer editorLayer) ? editorLayer.layer : 0;

        public void ConvertVGToLS()
        {
            BrowserPopup.Open();
            RTFileBrowser.inst.UpdateBrowserFile(RTFile.DotFormats(FileFormat.LSP, FileFormat.VGP, FileFormat.LST, FileFormat.VGT, FileFormat.LSB, FileFormat.VGD), onSelectFile: _val =>
            {
                bool failed = false;
                var selectedFile = _val.Replace("\\", "/");

                var fileFormat = RTFile.GetFileFormat(selectedFile);
                switch (fileFormat)
                {
                    case FileFormat.LSP: {
                            var file = RTFile.CombinePaths(BeatmapsPath, PrefabPath, Path.GetFileName(selectedFile));
                            if (RTFile.CopyFile(selectedFile, file))
                            {
                                EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(selectedFile)} to prefab ({PrefabPath}) folder.", 2f, EditorManager.NotificationType.Success);
                                StartCoroutine(UpdatePrefabPath());
                            }
                            else
                                EditorManager.inst.DisplayNotification($"Could not copy {Path.GetFileName(selectedFile)} as it already exists in the prefab ({PrefabPath}) folder.", 3f, EditorManager.NotificationType.Error);

                            break;
                        }
                    case FileFormat.VGP: {
                            try
                            {
                                var file = RTFile.ReadFromFile(selectedFile);

                                var vgjn = JSON.Parse(file);

                                var prefab = Prefab.ParseVG(vgjn);

                                var jn = prefab.ToJSON();

                                RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, PrefabPath));

                                string fileName = $"{RTFile.FormatLegacyFileName(prefab.name)}{FileFormat.LSP.Dot()}";
                                RTFile.WriteToFile(RTFile.CombinePaths(BeatmapsPath, PrefabPath, fileName), jn.ToString());

                                file = null;
                                vgjn = null;
                                prefab = null;
                                jn = null;

                                EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(selectedFile)} to {fileName} and added it to your prefab ({PrefabPath}) folder.", 2f,
                                    EditorManager.NotificationType.Success);

                                AchievementManager.inst.UnlockAchievement("time_machine");
                                StartCoroutine(UpdatePrefabPath());
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError(ex);
                                failed = true;
                            }

                            break;
                        }
                    case FileFormat.LST: {
                            var file = RTFile.CombinePaths(BeatmapsPath, ThemePath, Path.GetFileName(selectedFile));
                            if (RTFile.CopyFile(selectedFile, file))
                            {
                                EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(selectedFile)} to theme ({ThemePath}) folder.", 2f, EditorManager.NotificationType.Success);
                                StartCoroutine(UpdateThemePath());
                            }
                            else
                                EditorManager.inst.DisplayNotification($"Could not copy {Path.GetFileName(selectedFile)} as it already exists in the theme ({ThemePath}) folder.", 3f, EditorManager.NotificationType.Error);

                            break;
                        }
                    case FileFormat.VGT: {
                            try
                            {
                                var file = RTFile.ReadFromFile(selectedFile);

                                var vgjn = JSON.Parse(file);

                                var theme = BeatmapTheme.ParseVG(vgjn);

                                var jn = theme.ToJSON();

                                RTFile.CreateDirectory(RTFile.CombinePaths(BeatmapsPath, ThemePath));

                                var fileName = $"{RTFile.FormatLegacyFileName(theme.name)}{FileFormat.LST.Dot()}";
                                RTFile.WriteToFile(RTFile.CombinePaths(BeatmapsPath, ThemePath, fileName), jn.ToString());

                                file = null;
                                vgjn = null;
                                theme = null;
                                jn = null;

                                EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(selectedFile)} to {fileName} and added it to your theme ({ThemePath}) folder.", 2f,
                                    EditorManager.NotificationType.Success);

                                AchievementManager.inst.UnlockAchievement("time_machine");
                                StartCoroutine(UpdateThemePath());
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError(ex);
                                failed = true;
                            }

                            break;
                        }
                    case FileFormat.LSB: {
                            if (Path.GetFileName(selectedFile) != Level.LEVEL_LSB)
                            {
                                EditorManager.inst.DisplayNotification("Cannot select non-level.", 2f, EditorManager.NotificationType.Warning);
                                failed = true;
                                break;
                            }

                            ShowWarningPopup("Warning! Selecting a level will copy all of its contents to your editor, are you sure you want to do this?", () =>
                            {
                                var path = selectedFile.Replace("/" + Level.LEVEL_LSB, "");

                                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                                bool copied = false;
                                foreach (var file in files)
                                {
                                    var copyTo = file.Replace("\\", "/").Replace(Path.GetDirectoryName(path), RTFile.CombinePaths(BeatmapsPath, EditorPath));
                                    if (RTFile.CopyFile(file, copyTo))
                                        copied = true;
                                }

                                if (copied)
                                {
                                    EditorManager.inst.DisplayNotification($"Copied {Path.GetFileName(path)} to level ({EditorPath}) folder.", 2f, EditorManager.NotificationType.Success);
                                    EditorLevelManager.inst.LoadLevels();
                                }
                                else
                                    EditorManager.inst.DisplayNotification($"Could not copy {Path.GetFileName(path)}.", 3f, EditorManager.NotificationType.Error);

                                HideWarningPopup();
                            }, () =>
                            {
                                HideWarningPopup();
                                BrowserPopup.Open();
                            });

                            break;
                        }
                    case FileFormat.VGD: {
                            if (selectedFile.Contains("/autosave_"))
                            {
                                EditorManager.inst.DisplayNotification("Cannot select autosave.", 2f, EditorManager.NotificationType.Warning);
                                failed = true;
                                break;
                            }

                            try
                            {
                                var path = selectedFile.Replace("/level.vgd", "");

                                if (Level.Verify(path))
                                {
                                    var copyTo = path.Replace(Path.GetDirectoryName(path).Replace("\\", "/"), RTFile.CombinePaths(BeatmapsPath, EditorPath + "/")) + " Convert";

                                    RTFile.CreateDirectory(copyTo);

                                    #region Audio

                                    bool copiedAudioFile = false;
                                    bool audioFileExists = false;
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_OGG)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.AUDIO_OGG), RTFile.CombinePaths(copyTo, Level.LEVEL_OGG));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_WAV)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.AUDIO_WAV), RTFile.CombinePaths(copyTo, Level.LEVEL_WAV));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.AUDIO_MP3)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.AUDIO_MP3), RTFile.CombinePaths(copyTo, Level.LEVEL_MP3));
                                    }

                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_OGG)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_OGG), RTFile.CombinePaths(copyTo, Level.LEVEL_OGG));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_WAV)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_WAV), RTFile.CombinePaths(copyTo, Level.LEVEL_WAV));
                                    }
                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.LEVEL_MP3)))
                                    {
                                        audioFileExists = true;
                                        copiedAudioFile = RTFile.CopyFile(RTFile.CombinePaths(path, Level.LEVEL_MP3), RTFile.CombinePaths(copyTo, Level.LEVEL_MP3));
                                    }

                                    if (!copiedAudioFile)
                                    {
                                        if (!audioFileExists)
                                            EditorManager.inst.DisplayNotification("No audio file exists.", 3f, EditorManager.NotificationType.Error);
                                        else
                                            EditorManager.inst.DisplayNotification("Cannot overwrite the same file. Please rename the VG level folder you want to convert.", 6f, EditorManager.NotificationType.Error);

                                        return;
                                    }

                                    #endregion

                                    if (RTFile.FileExists(RTFile.CombinePaths(path, Level.COVER_JPG)))
                                        RTFile.CopyFile(RTFile.CombinePaths(path, Level.COVER_JPG), RTFile.CombinePaths(copyTo, Level.LEVEL_JPG));

                                    #region Data

                                    var metadataVGJSON = RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.METADATA_VGM));

                                    var metadataVGJN = JSON.Parse(metadataVGJSON);

                                    var metadata = MetaData.ParseVG(metadataVGJN);

                                    var metadataJN = metadata.ToJSON();

                                    RTFile.WriteToFile(RTFile.CombinePaths(copyTo, Level.METADATA_LSB), metadataJN.ToString());

                                    var levelVGJSON = RTFile.ReadFromFile(RTFile.CombinePaths(path, Level.LEVEL_VGD));

                                    var levelVGJN = JSON.Parse(levelVGJSON);

                                    var level = GameData.ParseVG(levelVGJN, metadata.Version);

                                    level.SaveData(RTFile.CombinePaths(copyTo, Level.LEVEL_LSB), () =>
                                    {
                                        EditorManager.inst.DisplayNotification($"Successfully converted {Path.GetFileName(path)} to {Path.GetFileName(copyTo)} and added it to your level ({EditorPath}) folder.", 2f,
                                            EditorManager.NotificationType.Success);

                                        metadataVGJSON = null;
                                        metadataVGJN = null;
                                        metadata = null;
                                        metadataJN = null;
                                        levelVGJSON = null;
                                        levelVGJN = null;
                                        level = null;

                                        AchievementManager.inst.UnlockAchievement("time_machine");
                                        EditorLevelManager.inst.LoadLevels();
                                    });

                                    #endregion
                                }
                                else
                                {
                                    EditorManager.inst.DisplayNotification("Could not convert since some needed files are missing!", 2f, EditorManager.NotificationType.Error);
                                    failed = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                EditorManager.inst.DisplayNotification($"There was an error in converting the VG level. Press {CoreConfig.Instance.OpenPAPersistentFolder.Value} to open the log folder and send the Player.log file to @rtmecha.", 5f, EditorManager.NotificationType.Error);
                                Debug.LogError(ex);
                                failed = true;
                            }

                            break;
                        }
                }

                if (!failed)
                    BrowserPopup.Close();
            });
        }
        
        public void SetCurrentPath(string path)
        {
            var fullPath = RTFile.RemoveEndSlash(path);
            var name = Path.GetFileName(fullPath);

            EditorManager.inst.currentLoadedLevel = name;
            GameManager.inst.path = RTFile.CombinePaths(fullPath, Level.LEVEL_LSB);
            RTFile.BasePath = RTFile.AppendEndSlash(fullPath);
            GameManager.inst.levelName = name;
        }

        #endregion
    }
}
